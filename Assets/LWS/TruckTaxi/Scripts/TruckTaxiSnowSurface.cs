using System;
using System.Collections.Generic;
using System.Reflection;
using LWS.InterstateHauler;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace LWS.TruckTaxi
{
    // Invisible contact volumes feed Weatherade's bounded snow coverage and indented tire-track mask.
    public sealed class TruckTaxiSnowSurface : MonoBehaviour
    {
        private const float MinimumTrackDepthMeters = .04f;
        private const float MinimumTrackCoverage = .06f;
        private const int SurfaceLayer = 30;
        private const int TraceLayer = 29;
        private const string SnowCoverageType = "NOT_Lonely.Weatherade.SnowCoverage";
        private const string CoverageBaseType = "NOT_Lonely.Weatherade.CoverageBase";
        private const string TraceGeneratorType = "NOT_Lonely.Weatherade.SRS_TraceMaskGenerator";
        private static Type coverageBaseType;
        private static Type traceGeneratorType;
        private static FieldInfo coverageSingleton;
        private static FieldInfo depthTargetsField;
        private readonly Dictionary<int, MeshRenderer> wheelTracers = new Dictionary<int, MeshRenderer>();
        private readonly Dictionary<MeshRenderer, MeshRenderer> roadDepthProxies = new Dictionary<MeshRenderer, MeshRenderer>();
        private readonly List<MeshRenderer> roadTargets = new List<MeshRenderer>();
        private readonly List<MeshRenderer> groundTargets = new List<MeshRenderer>();
        private readonly HashSet<int> cachedGroundTargetIds = new HashSet<int>();
        private readonly Dictionary<MeshRenderer, Material> tracedMaterials = new Dictionary<MeshRenderer, Material>();
        private readonly HashSet<Material> updatedSnowMaterials = new HashSet<Material>();
        private readonly HashSet<int> warnedIncompatibleRoads = new HashSet<int>();
        private readonly Dictionary<MeshRenderer, GroundBinding> groundBindings = new Dictionary<MeshRenderer, GroundBinding>();
        private readonly Dictionary<Material, Material> environmentMaterials = new Dictionary<Material, Material>();
        private readonly Dictionary<Material, Material> exteriorMaterials = new Dictionary<Material, Material>();
        private readonly Dictionary<Material, Material> trackMaterials = new Dictionary<Material, Material>();
        private readonly Dictionary<Terrain, TerrainBinding> terrainBindings = new Dictionary<Terrain, TerrainBinding>();
        private readonly List<MeshRenderer> environmentTargets = new List<MeshRenderer>();
        private readonly HashSet<int> environmentTargetIds = new HashSet<int>();
        private readonly Dictionary<int, MeshRenderer> wheelDepthProxies = new Dictionary<int, MeshRenderer>();
        private readonly RaycastHit[] exposureHits = new RaycastHit[32];
        private int nextEnvironmentTarget;
        private static readonly int EnvironmentCoverageId = Shader.PropertyToID("_TaxiSnowCoverage");
        private static readonly int EnvironmentTextureId = Shader.PropertyToID("_TaxiSnowTexture");
        private static readonly int EnvironmentExposureId = Shader.PropertyToID("_TaxiSnowExposure");
        private static readonly int EnvironmentTracesId = Shader.PropertyToID("_TaxiSnowTracesReady");
        private readonly Dictionary<Camera, CameraMask> cameraMasks = new Dictionary<Camera, CameraMask>();
        private readonly HashSet<Camera> boundDepthCameras = new HashSet<Camera>();
        private readonly Dictionary<Camera, RTHandle> ownedDepthTargets = new Dictionary<Camera, RTHandle>();
        private bool targetsCached;
        private LwsWeatheradeAdapter weatherade;
        private Component coverage;
        private Component generator;
        private MeshRenderer plowTracer;
        private Material traceMaterial;
        private float coverage01;
        private float nextMaterialCheck;
        private float nextRoadRegistration;
        private bool createdGenerator;
        private Texture2D snowTexture;
        private Texture2D snowDetailTexture;
        private Texture2D snowSparkleTexture;
        private float lastAppliedCoverage = -1;
        private int depthRendererIndex = -1;

        private struct GroundBinding
        {
            public Material[] original;
            public Material[] snow;
            public bool trackable;
        }
        private struct TerrainBinding
        {
            public Material original, snow;
            public float detailDensity;
        }
        private struct CameraMask
        {
            public int original;
            public int applied;
        }

        // Kept for existing probes; there is deliberately no Taxi snow mesh or collider.
        public int RenderedCells => 0;
        public Collider DepthCollider => null;
        public string Diagnostic { get; private set; } = "Weatherade snow not bound.";
        public int RoadDepthSourceCount => roadDepthProxies.Count;
        public int WheelTraceSourceCount => wheelTracers.Count;
        public int WheelTraceStampCount { get; private set; }
        public int ActiveWheelTraceSourceCount
        {
            get
            {
                int count = 0;
                foreach (MeshRenderer tracer in wheelTracers.Values) if (tracer != null && tracer.enabled) count++;
                return count;
            }
        }
        public int GroundSnowSurfaceCount => groundBindings.Count;
        public int TerrainSnowSurfaceCount => terrainBindings.Count;
        public int SharedEnvironmentMaterialCount => environmentMaterials.Count + exteriorMaterials.Count + trackMaterials.Count;
        public int PendingEnvironmentBindingCount => coverage01 <= .001f ? 0 : Mathf.Max(0, environmentTargets.Count - nextEnvironmentTarget);
        public bool EnvironmentBindingsReady => coverage01 <= .001f || targetsCached && nextEnvironmentTarget >= environmentTargets.Count;
        // Discovery-time ground-cache counts, not live renderer counts.
        public int CacheTargetCount => groundTargets.Count;
        public int UniqueCacheTargetCount => cachedGroundTargetIds.Count;
        public int CacheRefreshCount { get; private set; }
        public float Coverage01 => coverage01;
        public int BoundSnowMaterialCount => updatedSnowMaterials.Count;
        public bool SnowTextureReady => snowTexture != null;
        public int WeatheradeDepthRendererIndex => depthRendererIndex;
        public int BoundDepthCameraCount => boundDepthCameras.Count;
        public string VisualDiagnostic { get; private set; } = "Weatherade snow visual not sampled.";
        public string TracePixelDiagnostic { get; private set; } = "Trace pixels not sampled.";
        public int TraceActivePixelCount { get; private set; }
        public byte TracePeakMaskByte { get; private set; }
        public int TraceIndentPixelCount { get; private set; }
        public byte TracePeakIndentDeltaByte { get; private set; }
        public bool TraceMaskReady
        {
            get
            {
                if (!(generator is Behaviour behaviour) || !behaviour.isActiveAndEnabled) return false;
                RenderTexture texture = generator.GetType().GetField("traceTex", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(generator) as RenderTexture;
                return texture != null && texture.IsCreated();
            }
        }

        public void Initialize(TruckTaxiSnowRegion snow) { Initialize(snow, GetComponent<LwsWeatheradeAdapter>()); }
        public void Initialize(TruckTaxiSnowRegion snow, LwsWeatheradeAdapter adapter)
        {
            weatherade = adapter;
            Shader shader = Shader.Find("NOT_Lonely/Weatherade/Extra/NL_DepthOccluder");
            if (shader != null)
            {
                traceMaterial = new Material(shader) { name = "Taxi Weatherade trace-only volume" };
                traceMaterial.SetFloat("_Render", 0);
                // Weatherade's URP depth feature filters opaque queues before applying its depth override shader.
                traceMaterial.renderQueue = (int)RenderQueue.Geometry;
            }
            else Diagnostic = "Weatherade depth-occluder shader is missing; traces disabled.";
        }

        public void SetCoverage(float value)
        {
            coverage01 = float.IsFinite(value) ? Mathf.Clamp01(value) : 0;
            Shader.SetGlobalFloat(EnvironmentCoverageId, coverage01);
            weatherade?.SetExternalCoverageControl(coverage01 > .001f);
        }
        public void ConfigureVendorTextures(Texture2D main, Texture2D detail, Texture2D sparkle)
        {
            snowTexture = main;
            snowDetailTexture = detail;
            snowSparkleTexture = sparkle;
            Shader.SetGlobalTexture(EnvironmentTextureId, main);
            lastAppliedCoverage = -1;
        }
        public void ConfigureDepthRenderer(int rendererIndex) { depthRendererIndex = rendererIndex; }
        public void MarkDirty() { targetsCached = false; nextRoadRegistration = 0; }

        public void RefreshStreamedSurfaces()
        {
            targetsCached = false;
            roadTargets.Clear(); groundTargets.Clear();
            cachedGroundTargetIds.Clear();
            environmentTargets.Clear(); environmentTargetIds.Clear(); nextEnvironmentTarget = 0;
            StopAllTraces();
            ClearTraceHistory();
            foreach (var proxy in roadDepthProxies.Values) if (proxy != null) Destroy(proxy.gameObject);
            roadDepthProxies.Clear(); tracedMaterials.Clear();
            RestoreGround(); lastAppliedCoverage = -1;
            nextRoadRegistration = 0;
        }

        private bool IsTaxiScene(UnityEngine.SceneManagement.Scene scene)
        {
            if (scene == gameObject.scene) return true;
            var regions = TruckTaxiBootstrap.Instance?.GetComponent<TruckTaxiRegionalWorld>()?.regions;
            if (regions != null) foreach (var region in regions) if (region.sceneName == scene.name) return true;
            return false;
        }

        public void Refresh()
        {
            Component current = GetCoverageSingleton();
            if (coverage01 > .001f && (current == null || current.GetType().FullName != SnowCoverageType))
            {
                if (weatherade == null)
                {
                    Diagnostic = "LwsWeatheradeAdapter is missing; Taxi cannot bind snow road materials.";
                    return;
                }
                weatherade.ApplyRoadCondition(new LwsRoadConditionSnapshot
                {
                    condition = LwsRoadConditionType.LightSnow,
                    snowDepth01 = Mathf.Max(.021f, coverage01)
                });
                current = GetCoverageSingleton();
            }
            if (coverage01 > .001f && weatherade != null && Time.unscaledTime >= nextRoadRegistration)
            {
                weatherade.ApplyRoadCondition(new LwsRoadConditionSnapshot
                {
                    condition = LwsRoadConditionType.LightSnow,
                    snowDepth01 = Mathf.Max(.021f, coverage01)
                });
                nextRoadRegistration = Time.unscaledTime + 3f;
            }
            if (current == null || current.GetType().FullName != SnowCoverageType)
            {
                Shader.SetGlobalFloat(EnvironmentCoverageId, 0);
                Shader.SetGlobalFloat(EnvironmentTracesId, 0);
                coverage = null;
                generator = null;
                ReleaseDepthTargets();
                StopAllTraces();
                RestoreGround();
                RestoreCameraMasks();
                return;
            }
            if (coverage != current)
            {
                ReleaseDepthTargets();
                coverage = current;
                generator = null;
                createdGenerator = false;
                nextMaterialCheck = 0;
                lastAppliedCoverage = -1;
                updatedSnowMaterials.Clear();
                boundDepthCameras.Clear();
                ConfigureTrackAppearance(current);
            }
            Shader.SetGlobalFloat(EnvironmentCoverageId, coverage01);
            BindCoverageDepthCamera(current);
            Shader.SetGlobalVector(EnvironmentExposureId, new Vector4(
                ReadFloat(current, "coverageAreaBias", .025f), ReadFloat(current, "coverageAreaMaskRange", 1),
                ReadFloat(current, "coverageLeakReduction", 0), 1));
            BindVendorTextures(current);
            FieldInfo amount = current.GetType().GetField("coverageAmount", BindingFlags.Instance | BindingFlags.Public);
            MethodInfo update = current.GetType().GetMethod("UpdateCoverageMaterials", BindingFlags.Instance | BindingFlags.Public);
            if (amount == null || update == null)
            {
                Diagnostic = "Installed SnowCoverage lacks coverageAmount or UpdateCoverageMaterials.";
                return;
            }
            amount.SetValue(current, coverage01);
            BindGround();
            update.Invoke(current, null);
            UpdateRuntimeSnowMaterials(current);
            if (Time.time >= nextMaterialCheck)
            {
                EnableRoadTraces();
                HideTraceLayersFromGameCameras();
                nextMaterialCheck = Time.time + 1f;
            }
            EnsureTraceGenerator();
            BindTraceDepthCameras();
            Shader.SetGlobalFloat(EnvironmentTracesId, TraceMaskReady ? 1 : 0);
            UpdateVisualDiagnostic();
            Diagnostic = $"Weatherade SnowCoverage {coverage01:0.00}; SRS renderer={depthRendererIndex}, bound cameras={BoundDepthCameraCount}; snow texture ready={SnowTextureReady}; updated snow materials={BoundSnowMaterialCount}; trace mask ready={TraceMaskReady}; road depth sources={RoadDepthSourceCount}; environment surfaces={GroundSnowSurfaceCount}, terrains={TerrainSnowSurfaceCount}, shared materials={SharedEnvironmentMaterialCount}.";
        }

        private void BindCoverageDepthCamera(Component current)
        {
            if (depthRendererIndex < 0) return;
            const int traceLayers = (1 << SurfaceLayer) | (1 << TraceLayer);
            Type type = current.GetType();
            type.GetField("srsRendererId", BindingFlags.Instance | BindingFlags.Public)?.SetValue(current, depthRendererIndex);
            FieldInfo maskField = type.GetField("depthLayerMask", BindingFlags.Instance | BindingFlags.Public);
            if (maskField != null)
            {
                LayerMask mask = (LayerMask)maskField.GetValue(current);
                mask.value &= ~traceLayers;
                maskField.SetValue(current, mask);
            }
            Camera camera = type.GetProperty("sceneDepthCam", BindingFlags.Instance | BindingFlags.Public)?.GetValue(current) as Camera;
            if (camera == null) return;
            bool maskChanged = (camera.cullingMask & traceLayers) != 0;
            camera.cullingMask &= ~traceLayers;
            bool rendererChanged = BindDepthCameraRenderer(camera);
            if (maskChanged || rendererChanged)
                type.GetMethod("UpdateDepth", BindingFlags.Instance | BindingFlags.Public)?.Invoke(current, null);
        }

        private void BindTraceDepthCameras()
        {
            if (generator == null || depthRendererIndex < 0) return;
            Type type = generator.GetType();
            foreach (string name in new[] { "traceSurfCam", "traceObjsCam" })
            {
                Camera camera = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(generator) as Camera;
                if (camera != null) BindDepthCameraRenderer(camera);
            }
        }

        private bool BindDepthCameraRenderer(Camera camera)
        {
            BindDepthTarget(camera);
            if (boundDepthCameras.Contains(camera)) return false;
            Type dataType = ResolveType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData");
            Component data = dataType == null ? null : camera.GetComponent(dataType);
            MethodInfo setRenderer = dataType?.GetMethod("SetRenderer", BindingFlags.Instance | BindingFlags.Public);
            if (data == null || setRenderer == null) return false;
            setRenderer.Invoke(data, new object[] { depthRendererIndex });
            boundDepthCameras.Add(camera);
            return true;
        }

        private static IDictionary<Camera, RTHandle> DepthTargets()
        {
            depthTargetsField ??= ResolveType("SRS_RenderDepthWithReplacement")?.GetField("rtHandles", BindingFlags.Static | BindingFlags.Public);
            return depthTargetsField?.GetValue(null) as IDictionary<Camera, RTHandle>;
        }

        private void BindDepthTarget(Camera camera)
        {
            RenderTexture texture = camera.targetTexture;
            var targets = DepthTargets();
            if (texture == null || targets == null) return;
            if (targets.TryGetValue(camera, out RTHandle registered) && registered != null && registered.rt == texture) return;
            // Weatherade reuses named cameras, but AddCameraRTHandlePair cannot replace their old textures.
            // Repair the public renderer input, not the vendor's private registration lists or trace pixels.
            if (ownedDepthTargets.TryGetValue(camera, out RTHandle handle) && handle.rt != texture)
            {
                ReleaseDepthTarget(camera);
                handle = null;
            }
            if (handle == null)
            {
                handle = RTHandles.Alloc(texture, transferOwnership: false);
                ownedDepthTargets[camera] = handle;
            }
            targets[camera] = handle;
        }

        private void ReleaseDepthTarget(Camera camera)
        {
            RTHandle handle = ownedDepthTargets[camera];
            var targets = DepthTargets();
            if (targets != null && targets.TryGetValue(camera, out RTHandle registered) && ReferenceEquals(registered, handle))
                targets.Remove(camera);
            handle.Release(); // Non-owning wrapper: the vendor still owns the RenderTexture.
            ownedDepthTargets.Remove(camera);
        }

        private void ReleaseDepthTargets()
        {
            foreach (Camera camera in new List<Camera>(ownedDepthTargets.Keys)) ReleaseDepthTarget(camera);
        }

        public void RequestTracePixelDiagnostic()
        {
            RenderTexture texture = generator?.GetType().GetField("traceTex", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(generator) as RenderTexture;
            if (texture == null || !texture.IsCreated() || !SystemInfo.supportsAsyncGPUReadback)
            {
                TracePixelDiagnostic = "Trace texture unavailable or GPU readback unsupported.";
                return;
            }
            TracePixelDiagnostic = "Trace pixel readback pending.";
            AsyncGPUReadback.Request(texture, 0, request =>
            {
                if (this == null) return;
                if (request.hasError)
                {
                    TracePixelDiagnostic = "Trace pixel readback failed.";
                    return;
                }
                var pixels = request.GetData<Color32>();
                int active = 0;
                int indented = 0;
                byte peak = 0;
                byte peakIndent = 0;
                for (int i = 0; i < pixels.Length; i++)
                {
                    byte mask = pixels[i].b;
                    byte indent = (byte)Mathf.Abs(pixels[i].a - 128);
                    if (mask > 8) active++;
                    if (mask > 8 && indent > 8) indented++;
                    if (mask > peak) peak = mask;
                    if (indent > peakIndent) peakIndent = indent;
                }
                TraceActivePixelCount = active;
                TracePeakMaskByte = peak;
                TraceIndentPixelCount = indented;
                TracePeakIndentDeltaByte = peakIndent;
                TracePixelDiagnostic = $"Weatherade trace: B>8 {active}/{pixels.Length}; B>8 and |A-128|>8 {indented}; peak B {peak}, peak |A-128| {peakIndent}.";
            });
        }

        private void UpdateVisualDiagnostic()
        {
            Camera depth = coverage?.GetType().GetProperty("sceneDepthCam", BindingFlags.Instance | BindingFlags.Public)?.GetValue(coverage) as Camera;
            Material first = null;
            foreach (MeshRenderer renderer in roadTargets)
            {
                if (renderer == null || !LwsWeatheradeMaterialFactory.IsWeatheradeMaterialForMode(renderer.sharedMaterial, LwsWeatheradeSurfaceMaterialMode.Snow)) continue;
                first = renderer.sharedMaterial;
                break;
            }
            string material = first == null ? "no snow material" :
                $"amount={first.GetFloat("_CoverageAmount"):0.00}, texture={first.GetTexture("_CoverageTex0") != null}, keyword={first.IsKeywordEnabled("_COVERAGE_ON")}";
            string camera = depth == null ? "no depth camera" :
                $"depth cam size={depth.orthographicSize * 2:0}m/far={depth.farClipPlane:0}m/layers={depth.cullingMask}";
            VisualDiagnostic = $"{updatedSnowMaterials.Count} updated materials; {material}; {camera}.";
        }

        private void BindVendorTextures(Component current)
        {
            if (snowTexture == null) return;
            SetTextureIfMissing(current, "coverageTex0", snowTexture);
            SetTextureIfMissing(current, "coverageDetailTex", snowDetailTexture);
            SetTextureIfMissing(current, "sparkleTex", snowSparkleTexture);
        }

        private static void SetTextureIfMissing(Component current, string name, Texture2D texture)
        {
            if (texture == null) return;
            FieldInfo field = current.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null && field.GetValue(current) == null) field.SetValue(current, texture);
        }

        private void UpdateRuntimeSnowMaterials(Component current)
        {
            CacheTargets();
            MethodInfo updateMaterial = current.GetType().GetMethod("UpdateCoverageMaterial", BindingFlags.Instance | BindingFlags.Public);
            if (updateMaterial == null) return;
            bool amountChanged = !Mathf.Approximately(lastAppliedCoverage, coverage01);
            foreach (MeshRenderer renderer in roadTargets)
            {
                if (renderer == null) continue;
                Material material = renderer.sharedMaterial;
                if (!LwsWeatheradeMaterialFactory.IsWeatheradeMaterialForMode(material, LwsWeatheradeSurfaceMaterialMode.Snow))
                {
                    if (coverage01 > .001f && warnedIncompatibleRoads.Add(renderer.GetInstanceID()))
                        Debug.LogWarning("Truck Taxi snow: road renderer is not Weatherade snow-compatible: " + renderer.name, renderer);
                    continue;
                }
                if (amountChanged || !updatedSnowMaterials.Contains(material)) updateMaterial.Invoke(current, new object[] { material });
                updatedSnowMaterials.Add(material);
            }
            lastAppliedCoverage = coverage01;
        }

        public void TracePlow(Vector3 from, Vector3 to, float width)
        {
            if (generator == null || coverage01 <= .001f) return;
            if (plowTracer == null) plowTracer = CreateTracer("Taxi plow Weatherade trace");
            PlaceTrace(plowTracer, from, to, width, 1.2f, -.3f);
        }

        public void StopPlowTrace()
        {
            if (plowTracer != null) plowTracer.enabled = false;
        }

        public static float TrackStrength(float depthMeters, float coverage)
        {
            if (depthMeters < MinimumTrackDepthMeters || coverage < MinimumTrackCoverage) return 0;
            return .22f + .78f * Mathf.InverseLerp(MinimumTrackDepthMeters, .4f, depthMeters);
        }

        public void TraceWheel(int index, Vector3 from, Vector3 to, float width, float depthMeters)
        {
            TraceWheel(index, from, to, width, depthMeters, false);
        }

        public void TraceWheel(int index, Vector3 from, Vector3 to, float width, float depthMeters, bool offRoad)
        {
            float strength = TrackStrength(depthMeters, coverage01);
            if (generator == null || strength <= 0) { StopWheelTrace(index); return; }
            if (!wheelTracers.TryGetValue(index, out MeshRenderer tracer) || tracer == null)
            {
                tracer = CreateTracer("Taxi wheel Weatherade trace " + index);
                if (tracer == null) return;
                wheelTracers[index] = tracer;
            }
            width = Mathf.Max(.15f, width);
            PlaceTrace(tracer, from, to, width, .18f + .36f * strength, .03f);
            if (offRoad)
            {
                if (!wheelDepthProxies.TryGetValue(index, out MeshRenderer depth) || depth == null)
                {
                    depth = CreateTracer("Taxi wheel contact depth " + index);
                    if (depth != null) depth.gameObject.layer = SurfaceLayer;
                    wheelDepthProxies[index] = depth;
                }
                // A reusable contact-sized depth volume, pitched between real NWH contacts.
                // No terrain tessellation/rebuild, raycast grid, or duplicate terrain is required.
                PlaceTrace(depth, from, to, width * 1.6f, .04f, -.02f);
            }
            else if (wheelDepthProxies.TryGetValue(index, out MeshRenderer depth) && depth != null) depth.enabled = false;
            WheelTraceStampCount++;
        }

        public void StopWheelTrace(int index)
        {
            if (wheelTracers.TryGetValue(index, out MeshRenderer tracer) && tracer != null) tracer.enabled = false;
            if (wheelDepthProxies.TryGetValue(index, out MeshRenderer depth) && depth != null) depth.enabled = false;
        }

        public bool IsSnowCoveredOffRoad(Collider collider, Vector3 point, Transform vehicle)
        {
            if (collider == null || collider.isTrigger || !IsTaxiScene(collider.gameObject.scene)) return false;
            TruckTaxiSurface marked = collider.GetComponentInParent<TruckTaxiSurface>();
            if (marked != null && marked.isRoad || collider.GetComponentInParent<LwsRoadSurface>() != null) return false;
            bool supported = collider is TerrainCollider terrainCollider &&
                terrainCollider.GetComponent<Terrain>() is Terrain terrain && terrainBindings.ContainsKey(terrain);
            if (!supported)
            {
                MeshRenderer renderer = collider.GetComponent<MeshRenderer>() ?? collider.GetComponentInParent<MeshRenderer>();
                supported = renderer != null && groundBindings.TryGetValue(renderer, out GroundBinding binding) && binding.trackable;
            }
            if (!supported) return false;
            int count = Physics.RaycastNonAlloc(point + Vector3.up * .15f, Vector3.up, exposureHits, 128,
                ~((1 << SurfaceLayer) | (1 << TraceLayer)), QueryTriggerInteraction.Ignore);
            if (count == exposureHits.Length) return false;
            for (int i = 0; i < count; i++)
            {
                Transform hit = exposureHits[i].transform;
                if (vehicle != null && hit.IsChildOf(vehicle)) continue;
                return false;
            }
            return true;
        }

        private void EnsureTraceGenerator()
        {
            if (traceMaterial == null || coverage == null || generator != null) return;
            Type type = traceGeneratorType ??= ResolveType(TraceGeneratorType);
            if (type == null) { Diagnostic = "Installed SRS_TraceMaskGenerator type not found."; return; }
            Camera depthCamera = coverage.GetType().GetProperty("sceneDepthCam", BindingFlags.Instance | BindingFlags.Public)?.GetValue(coverage) as Camera;
            if (depthCamera == null) return;
            generator = coverage.GetComponent(type);
            createdGenerator = generator == null;
            if (createdGenerator) generator = coverage.gameObject.AddComponent(type);
            if (generator == null) return;
            SetField(generator, "traceSurfLayermask", new LayerMask { value = 1 << SurfaceLayer });
            SetField(generator, "traceObjsLayermask", new LayerMask { value = 1 << TraceLayer });
            SetField(generator, "decaySpeed", .003f);
            SetField(generator, "maskRes", 1024);
            SetField(generator, "blurKernelSize", 2);
            SetField(generator, "borderIntensity", .7f);
            SetField(generator, "normalSpread", 1.3f);
            type.GetMethod("Init", BindingFlags.Instance | BindingFlags.Public)?.Invoke(generator, null);
        }

        private static void ConfigureTrackAppearance(Component snowCoverage)
        {
            SetField(snowCoverage, "traces", true);
            Color compressedSnow = new Color(.40f, .46f, .52f, .65f);
            foreach (string suffix in new[] { "", "1", "2" })
            {
                SetField(snowCoverage, "tracesBaseBlend" + (suffix == "" ? "0" : suffix), .45f);
                SetField(snowCoverage, "tracesNormalScale" + suffix, 1.35f);
                SetField(snowCoverage, "tracesColor" + suffix, compressedSnow);
            }
        }

        private void EnableRoadTraces()
        {
            CacheTargets();
            foreach (MeshRenderer renderer in roadTargets)
            {
                if (renderer == null) continue;
                Material material = renderer.sharedMaterial;
                if (material == null || !material.HasProperty("_TracesOverride")) continue;
                if (!tracedMaterials.TryGetValue(renderer, out Material previous) || previous != material)
                {
                    material.SetFloat("_TracesOverride", 1);
                    material.SetFloat("_Traces", 1);
                    material.EnableKeyword("_TRACES_ON");
                    tracedMaterials[renderer] = material;
                }
                MeshFilter source = renderer.GetComponent<MeshFilter>();
                if (source == null || source.sharedMesh == null || traceMaterial == null) continue;
                if (!roadDepthProxies.TryGetValue(renderer, out MeshRenderer proxy) || proxy == null)
                {
                    GameObject obj = new GameObject("Taxi Weatherade road depth source");
                    obj.layer = SurfaceLayer;
                    obj.transform.SetParent(transform, true);
                    obj.AddComponent<MeshFilter>().sharedMesh = source.sharedMesh;
                    proxy = obj.AddComponent<MeshRenderer>();
                    proxy.sharedMaterial = traceMaterial;
                    proxy.shadowCastingMode = ShadowCastingMode.Off;
                    proxy.receiveShadows = false;
                    roadDepthProxies[renderer] = proxy;
                }
                proxy.transform.SetPositionAndRotation(renderer.transform.position, renderer.transform.rotation);
                proxy.transform.localScale = renderer.transform.lossyScale;
                proxy.enabled = true;
            }
        }

        private void CacheTargets()
        {
            if (targetsCached) return;
            targetsCached = true;
            CacheRefreshCount = Math.Min(int.MaxValue - 1, CacheRefreshCount) + 1;
            foreach (LwsRoadSurface road in FindObjectsByType<LwsRoadSurface>(FindObjectsSortMode.None))
            {
                if (road == null || !IsTaxiScene(road.gameObject.scene)) continue;
                foreach (MeshRenderer renderer in road.GetComponentsInChildren<MeshRenderer>(true))
                    if (renderer.gameObject.layer != SurfaceLayer && renderer.gameObject.layer != TraceLayer && !roadTargets.Contains(renderer))
                        roadTargets.Add(renderer);
            }
            foreach (TruckTaxiSurface surface in FindObjectsByType<TruckTaxiSurface>(FindObjectsSortMode.None))
            {
                if (surface == null || surface.isRoad || !IsTaxiScene(surface.gameObject.scene)) continue;
                MeshRenderer renderer = surface.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    if (!groundTargets.Contains(renderer)) groundTargets.Add(renderer);
                    cachedGroundTargetIds.Add(renderer.GetInstanceID());
                }
            }
            foreach (MeshRenderer renderer in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if (renderer == null || !IsTaxiScene(renderer.gameObject.scene) ||
                    renderer.gameObject.layer == SurfaceLayer || renderer.gameObject.layer == TraceLayer ||
                    renderer.GetComponentInParent<LwsRoadSurface>() != null || IsExcludedEnvironment(renderer.transform)) continue;
                if (environmentTargetIds.Add(renderer.GetInstanceID())) environmentTargets.Add(renderer);
            }
        }

        private void BindGround()
        {
            CacheTargets();
            // Streamed surfaces are discovered once, then adapted in bounded batches.
            int end = Mathf.Min(nextEnvironmentTarget + 64, environmentTargets.Count);
            while (nextEnvironmentTarget < end)
            {
                MeshRenderer renderer = environmentTargets[nextEnvironmentTarget++];
                if (renderer == null || groundBindings.ContainsKey(renderer)) continue;
                Material[] original = renderer.sharedMaterials;
                Material[] snow = (Material[])original.Clone();
                bool changed = false;
                bool exterior = IsExteriorSurface(renderer);
                bool trackable = IsTrackSurface(renderer);
                for (int i = 0; i < snow.Length; i++)
                {
                    Material adapted = AdaptEnvironmentMaterial(original[i], exterior, trackable);
                    if (adapted == null) continue;
                    snow[i] = adapted;
                    changed = true;
                }
                if (!changed) continue;
                renderer.sharedMaterials = snow;
                groundBindings[renderer] = new GroundBinding { original = original, snow = snow, trackable = trackable };
            }
            BindTerrains();
        }

        private void RestoreGround()
        {
            foreach (var pair in groundBindings)
            {
                if (pair.Key == null) continue;
                Material[] current = pair.Key.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < current.Length && i < pair.Value.snow.Length; i++)
                    if (current[i] == pair.Value.snow[i]) { current[i] = pair.Value.original[i]; changed = true; }
                if (changed) pair.Key.sharedMaterials = current;
            }
            groundBindings.Clear();
            foreach (var pair in terrainBindings)
            {
                if (pair.Key == null) continue;
                if (pair.Key.materialTemplate == pair.Value.snow) pair.Key.materialTemplate = pair.Value.original;
                pair.Key.detailObjectDensity = pair.Value.detailDensity;
            }
            terrainBindings.Clear();
            foreach (Material material in environmentMaterials.Values) if (material != null) DestroyOwned(material);
            foreach (Material material in exteriorMaterials.Values) if (material != null) DestroyOwned(material);
            foreach (Material material in trackMaterials.Values) if (material != null) DestroyOwned(material);
            environmentMaterials.Clear();
            exteriorMaterials.Clear();
            trackMaterials.Clear();
            nextEnvironmentTarget = 0;
        }

        public static bool IsSnowEligibleMaterial(Material material)
        {
            if (material == null || material.shader == null || material.renderQueue >= (int)RenderQueue.Transparent ||
                material.GetTag("RenderType", false) == "Transparent" ||
                material.HasProperty("_Surface") && material.GetFloat("_Surface") > .5f) return false;
            string name = material.name.ToLowerInvariant();
            return !name.Contains("glass") && !name.Contains("window") && !name.Contains("water") &&
                !name.Contains("interior") && !name.Contains("screen");
        }

        private static bool IsExcludedEnvironment(Transform target)
        {
            for (Transform current = target; current != null; current = current.parent)
            {
                string name = current.name.ToLowerInvariant();
                if (name.Contains("interior") || name.Contains("indoor") || name.Contains("passenger") ||
                    name.Contains("player") || name.Contains("cockpit") || name.Contains("hud")) return true;
            }
            return false;
        }

        private static bool IsExteriorSurface(MeshRenderer renderer)
        {
            // Conservative distant fallback. Composite buildings/floors require the depth volume;
            // explicit outdoor geometry can remain snowy beyond its bounded shelter map.
            string name = renderer.name.ToLowerInvariant();
            foreach (string token in new[] { "roof", "awning", "grass", "ground", "terrain", "dirt", "rock", "tree",
                "bush", "shrub", "sidewalk", "pavement", "fence", "barrier", "bench", "dumpster", "container" })
                if (name.Contains(token)) return true;
            return false;
        }

        private static bool IsTrackSurface(MeshRenderer renderer)
        {
            TruckTaxiSurface marker = renderer.GetComponentInParent<TruckTaxiSurface>();
            if (marker != null) return !marker.isRoad;
            string name = renderer.name.ToLowerInvariant();
            foreach (string token in new[] { "grass", "ground", "terrain", "dirt", "field", "earth", "shoulder", "parking", "sidewalk", "pavement" })
                if (name.Contains(token)) return true;
            return false;
        }

        private Material AdaptEnvironmentMaterial(Material source, bool exterior, bool trackable)
        {
            if (!IsSnowEligibleMaterial(source)) return null;
            var cache = trackable ? trackMaterials : exterior ? exteriorMaterials : environmentMaterials;
            if (cache.TryGetValue(source, out Material cached)) return cached;
            string resource;
            switch (source.shader.name)
            {
                case "Universal Render Pipeline/Lit": resource = trackable ? "EnvironmentLitGround" : exterior ? "EnvironmentLitExterior" : "EnvironmentLit"; break;
                case "NatureManufacture/URP/Foliage/Foliage": resource = "EnvironmentFoliage"; break;
                case "Toby Fredson/The Toby Foliage Engine/(TTFE) Tree Foliage": resource = "EnvironmentTreeFoliage"; break;
                case "Toby Fredson/The Toby Foliage Engine/(TTFE) Tree Bark": resource = "EnvironmentTreeBark"; break;
                case "Toby Fredson/The Toby Foliage Engine/(TTFE) Tree Billboard": resource = "EnvironmentTreeBillboard"; break;
                default: return null; // Unknown shaders retain their authored presentation.
            }
            Shader shader = Resources.Load<Shader>("TruckTaxi/Snow/" + resource);
            if (shader == null || !shader.isSupported) return null;
            var material = new Material(source) { name = source.name + " Taxi snow", shader = shader, hideFlags = HideFlags.DontSave };
            material.enableInstancing = source.enableInstancing;
            material.shaderKeywords = source.shaderKeywords;
            cache.Add(source, material);
            return material;
        }

        private void BindTerrains()
        {
            foreach (Terrain terrain in Terrain.activeTerrains)
            {
                if (terrain == null || !IsTaxiScene(terrain.gameObject.scene) || terrain.terrainData == null) continue;
                if (!terrainBindings.TryGetValue(terrain, out TerrainBinding binding))
                {
                    Material source = terrain.materialTemplate;
                    if (source == null || source.shader == null || source.shader.name != "Universal Render Pipeline/Terrain/Lit") continue;
                    if (!environmentMaterials.TryGetValue(source, out Material snow))
                    {
                        Material template = Resources.Load<Material>("TruckTaxi/Snow/EnvironmentTerrain");
                        if (template == null || !template.shader.isSupported) continue;
                        snow = new Material(template) { name = source.name + " Taxi snow", hideFlags = HideFlags.DontSave, enableInstancing = true };
                        foreach (string property in new[] { "_EnableHeightBlend", "_HeightTransition", "_EnableInstancedPerPixelNormal" })
                            if (source.HasProperty(property)) snow.SetFloat(property, source.GetFloat(property));
                        if (source.IsKeywordEnabled("_TERRAIN_BLEND_HEIGHT")) snow.EnableKeyword("_TERRAIN_BLEND_HEIGHT");
                        if (source.IsKeywordEnabled("_NORMALMAP")) snow.EnableKeyword("_NORMALMAP");
                        environmentMaterials.Add(source, snow);
                    }
                    binding = new TerrainBinding { original = source, snow = snow, detailDensity = terrain.detailObjectDensity };
                    terrainBindings.Add(terrain, binding);
                    terrain.materialTemplate = snow;
                }
                if (coverage != null)
                    coverage.GetType().GetMethod("UpdateCoverageMaterial", BindingFlags.Instance | BindingFlags.Public)?.Invoke(coverage, new object[] { binding.snow });
                // Only the live instance changes; TerrainData/detail prototypes remain untouched.
                terrain.detailObjectDensity = binding.detailDensity * Mathf.Lerp(1, .18f, coverage01 * coverage01);
            }
        }

        private static float ReadFloat(Component component, string field, float fallback)
        {
            for (Type type = component.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo member = type.GetField(field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (member?.GetValue(component) is float value) return value;
            }
            return fallback;
        }

        private static void DestroyOwned(UnityEngine.Object value)
        {
            if (Application.isPlaying) Destroy(value);
            else DestroyImmediate(value);
        }

        private void HideTraceLayersFromGameCameras()
        {
            const int traceMask = (1 << SurfaceLayer) | (1 << TraceLayer);
            foreach (Camera camera in Camera.allCameras)
            {
                if (camera == null || camera.name.StartsWith("SRS_", StringComparison.Ordinal)) continue;
                if (!cameraMasks.TryGetValue(camera, out CameraMask saved) || camera.cullingMask != saved.applied)
                    saved.original = camera.cullingMask;
                saved.applied = saved.original & ~traceMask;
                camera.cullingMask = saved.applied;
                cameraMasks[camera] = saved;
            }
        }

        private void RestoreCameraMasks()
        {
            foreach (var pair in cameraMasks)
                if (pair.Key != null && pair.Key.cullingMask == pair.Value.applied)
                    pair.Key.cullingMask = pair.Value.original;
            cameraMasks.Clear();
        }

        private MeshRenderer CreateTracer(string name)
        {
            if (traceMaterial == null) return null;
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.layer = TraceLayer;
            obj.transform.SetParent(transform, true);
            Collider collider = obj.GetComponent<Collider>();
            if (collider != null) { collider.enabled = false; Destroy(collider); }
            MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = traceMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.enabled = false;
            return renderer;
        }

        private static void PlaceTrace(MeshRenderer tracer, Vector3 from, Vector3 to, float width, float height, float yOffset)
        {
            if (tracer == null) return;
            Vector3 direction = to - from;
            if (direction.sqrMagnitude < .0025f) { tracer.enabled = false; return; }
            Transform shape = tracer.transform;
            shape.SetPositionAndRotation((from + to) * .5f + Vector3.up * yOffset, Quaternion.LookRotation(direction.normalized, Vector3.up));
            shape.localScale = new Vector3(width, height, direction.magnitude + width * .25f);
            tracer.enabled = true;
        }

        private static Component GetCoverageSingleton()
        {
            coverageBaseType ??= ResolveType(CoverageBaseType);
            coverageSingleton ??= coverageBaseType?.GetField("instance", BindingFlags.Static | BindingFlags.Public);
            return coverageSingleton?.GetValue(null) as Component;
        }

        private static Type ResolveType(string name)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(name);
                if (type != null) return type;
            }
            return null;
        }

        private static void SetField(Component component, string name, object value)
        {
            component.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(component, value);
        }

        private void ClearTraceHistory()
        {
            if (generator == null) return;
            RenderTexture previous = RenderTexture.active;
            try
            {
                foreach (string field in new[] { "traceMaskRT", "finalTraceMask", "bluredRT", "traceTex" })
                {
                    RenderTexture texture = generator.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(generator) as RenderTexture;
                    if (texture == null || !texture.IsCreated()) continue;
                    RenderTexture.active = texture;
                    GL.Clear(false, true, field == "traceTex" ? new Color(.5f, .5f, 0, .5f) : Color.clear);
                }
            }
            finally { RenderTexture.active = previous; }
        }

        private void StopAllTraces()
        {
            StopPlowTrace();
            foreach (MeshRenderer tracer in wheelTracers.Values) if (tracer != null) tracer.enabled = false;
            foreach (MeshRenderer proxy in roadDepthProxies.Values) if (proxy != null) proxy.enabled = false;
            foreach (MeshRenderer proxy in wheelDepthProxies.Values) if (proxy != null) proxy.enabled = false;
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            MarkDirty();
        }
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) { MarkDirty(); }
        private void OnSceneUnloaded(Scene scene)
        {
            // A streamed chunk is not a world restore: keep surviving adapters and the bounded trace mask.
            nextEnvironmentTarget = PruneSceneTargets(environmentTargets, scene, nextEnvironmentTarget);
            PruneSceneTargets(groundTargets, scene);
            PruneSceneTargets(roadTargets, scene);
            environmentTargetIds.Clear();
            foreach (MeshRenderer renderer in environmentTargets) environmentTargetIds.Add(renderer.GetInstanceID());
            cachedGroundTargetIds.Clear();
            foreach (MeshRenderer renderer in groundTargets) cachedGroundTargetIds.Add(renderer.GetInstanceID());

            foreach (MeshRenderer renderer in new List<MeshRenderer>(groundBindings.Keys))
                if (HasDepartedScene(renderer, scene)) groundBindings.Remove(renderer);
            foreach (Terrain terrain in new List<Terrain>(terrainBindings.Keys))
                if (HasDepartedScene(terrain, scene)) terrainBindings.Remove(terrain);
            foreach (MeshRenderer renderer in new List<MeshRenderer>(tracedMaterials.Keys))
                if (HasDepartedScene(renderer, scene)) tracedMaterials.Remove(renderer);
            foreach (MeshRenderer renderer in new List<MeshRenderer>(roadDepthProxies.Keys))
            {
                if (!HasDepartedScene(renderer, scene)) continue;
                MeshRenderer proxy = roadDepthProxies[renderer];
                if (proxy != null) DestroyOwned(proxy.gameObject);
                roadDepthProxies.Remove(renderer);
            }

            var liveMaterials = new HashSet<Material>();
            foreach (GroundBinding binding in groundBindings.Values)
                foreach (Material material in binding.snow) liveMaterials.Add(material);
            foreach (TerrainBinding binding in terrainBindings.Values) liveMaterials.Add(binding.snow);
            PruneUnusedAdapters(environmentMaterials, liveMaterials);
            PruneUnusedAdapters(exteriorMaterials, liveMaterials);
            PruneUnusedAdapters(trackMaterials, liveMaterials);

            liveMaterials.Clear();
            var liveRoadIds = new HashSet<int>();
            foreach (MeshRenderer renderer in roadTargets)
            {
                liveMaterials.Add(renderer.sharedMaterial);
                liveRoadIds.Add(renderer.GetInstanceID());
            }
            updatedSnowMaterials.IntersectWith(liveMaterials);
            warnedIncompatibleRoads.IntersectWith(liveRoadIds);
            foreach (Camera camera in new List<Camera>(ownedDepthTargets.Keys))
                if (HasDepartedScene(camera, scene)) ReleaseDepthTarget(camera);
            boundDepthCameras.RemoveWhere(camera => HasDepartedScene(camera, scene));
            MarkDirty();
        }

        private static bool HasDepartedScene(Component target, Scene scene)
        {
            return target == null || target.gameObject.scene == scene || !target.gameObject.scene.isLoaded;
        }

        private static int PruneSceneTargets(List<MeshRenderer> targets, Scene scene, int nextTarget = 0)
        {
            int count = 0, next = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                MeshRenderer renderer = targets[i];
                if (HasDepartedScene(renderer, scene)) continue;
                targets[count++] = renderer;
                // Preserve the processed/pending boundary when entries on either side disappear.
                if (i < nextTarget) next++;
            }
            targets.RemoveRange(count, targets.Count - count);
            return next;
        }

        private static void PruneUnusedAdapters(Dictionary<Material, Material> adapters, HashSet<Material> liveMaterials)
        {
            foreach (Material source in new List<Material>(adapters.Keys))
            {
                Material adapted = adapters[source];
                if (liveMaterials.Contains(adapted)) continue;
                adapters.Remove(source);
                if (adapted != null) DestroyOwned(adapted);
            }
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            Shader.SetGlobalFloat(EnvironmentCoverageId, 0);
            Shader.SetGlobalFloat(EnvironmentTracesId, 0);
            StopAllTraces(); RestoreGround(); RestoreCameraMasks();
            ReleaseDepthTargets();
            weatherade?.SetExternalCoverageControl(false);
        }
        private void OnDestroy()
        {
            weatherade?.SetExternalCoverageControl(false);
            RestoreGround();
            RestoreCameraMasks();
            ReleaseDepthTargets();
            if (createdGenerator && generator != null) Destroy(generator);
            if (traceMaterial != null) DestroyOwned(traceMaterial);
        }
    }
}
