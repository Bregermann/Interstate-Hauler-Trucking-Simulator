using System;
using System.Collections.Generic;
using System.Reflection;
using LWS.InterstateHauler;
using UnityEngine;
using UnityEngine.Rendering;

namespace LWS.TruckTaxi
{
    // Weatherade owns every visible snow pixel. These temporary, invisible volumes only feed its trace camera.
    public sealed class TruckTaxiSnowSurface : MonoBehaviour
    {
        private const int SurfaceLayer = 30;
        private const int TraceLayer = 29;
        private const string SnowCoverageType = "NOT_Lonely.Weatherade.SnowCoverage";
        private const string CoverageBaseType = "NOT_Lonely.Weatherade.CoverageBase";
        private const string TraceGeneratorType = "NOT_Lonely.Weatherade.SRS_TraceMaskGenerator";
        private static Type coverageBaseType;
        private static Type traceGeneratorType;
        private static FieldInfo coverageSingleton;
        private readonly Dictionary<int, MeshRenderer> wheelTracers = new Dictionary<int, MeshRenderer>();
        private readonly Dictionary<MeshRenderer, MeshRenderer> roadDepthProxies = new Dictionary<MeshRenderer, MeshRenderer>();
        private readonly List<MeshRenderer> roadTargets = new List<MeshRenderer>();
        private readonly List<MeshRenderer> groundTargets = new List<MeshRenderer>();
        private readonly Dictionary<MeshRenderer, Material> tracedMaterials = new Dictionary<MeshRenderer, Material>();
        private readonly HashSet<Material> updatedSnowMaterials = new HashSet<Material>();
        private readonly Dictionary<MeshRenderer, GroundBinding> groundBindings = new Dictionary<MeshRenderer, GroundBinding>();
        private readonly Dictionary<Camera, CameraMask> cameraMasks = new Dictionary<Camera, CameraMask>();
        private readonly HashSet<Camera> boundDepthCameras = new HashSet<Camera>();
        private bool targetsCached;
        private LwsWeatheradeAdapter weatherade;
        private Component coverage;
        private Component generator;
        private MeshRenderer plowTracer;
        private Material traceMaterial;
        private float coverage01;
        private float nextMaterialCheck;
        private bool createdGenerator;
        private Texture2D snowTexture;
        private Texture2D snowDetailTexture;
        private Texture2D snowSparkleTexture;
        private float lastAppliedCoverage = -1;
        private int depthRendererIndex = -1;

        private struct GroundBinding
        {
            public Material original;
            public Material snow;
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
        public int GroundSnowSurfaceCount => groundBindings.Count;
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

        public void SetCoverage(float value) { coverage01 = Mathf.Clamp01(value); }
        public void ConfigureVendorTextures(Texture2D main, Texture2D detail, Texture2D sparkle)
        {
            snowTexture = main;
            snowDetailTexture = detail;
            snowSparkleTexture = sparkle;
            lastAppliedCoverage = -1;
        }
        public void ConfigureDepthRenderer(int rendererIndex) { depthRendererIndex = rendererIndex; }
        public void MarkDirty() { }

        public void RefreshStreamedSurfaces()
        {
            targetsCached = false;
            roadTargets.Clear(); groundTargets.Clear();
            foreach (var proxy in roadDepthProxies.Values) if (proxy != null) Destroy(proxy.gameObject);
            roadDepthProxies.Clear(); tracedMaterials.Clear();
            RestoreGround(); lastAppliedCoverage = -1;
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
            if (current == null || current.GetType().FullName != SnowCoverageType)
            {
                coverage = null;
                generator = null;
                StopAllTraces();
                RestoreGround();
                RestoreCameraMasks();
                return;
            }
            if (coverage != current)
            {
                coverage = current;
                generator = null;
                createdGenerator = false;
                nextMaterialCheck = 0;
                lastAppliedCoverage = -1;
                updatedSnowMaterials.Clear();
                boundDepthCameras.Clear();
            }
            BindCoverageDepthCamera(current);
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
            UpdateVisualDiagnostic();
            Diagnostic = $"Weatherade SnowCoverage {coverage01:0.00}; SRS renderer={depthRendererIndex}, bound cameras={BoundDepthCameraCount}; snow texture ready={SnowTextureReady}; updated snow materials={BoundSnowMaterialCount}; trace mask ready={TraceMaskReady}; road depth sources={RoadDepthSourceCount}; ground surfaces={GroundSnowSurfaceCount}.";
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
            if (boundDepthCameras.Contains(camera)) return false;
            Type dataType = ResolveType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData");
            Component data = dataType == null ? null : camera.GetComponent(dataType);
            MethodInfo setRenderer = dataType?.GetMethod("SetRenderer", BindingFlags.Instance | BindingFlags.Public);
            if (data == null || setRenderer == null) return false;
            setRenderer.Invoke(data, new object[] { depthRendererIndex });
            boundDepthCameras.Add(camera);
            return true;
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
                if (!LwsWeatheradeMaterialFactory.IsWeatheradeMaterialForMode(material, LwsWeatheradeSurfaceMaterialMode.Snow)) continue;
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

        public void TraceWheel(int index, Vector3 from, Vector3 to, float width)
        {
            if (generator == null || coverage01 <= .001f) return;
            if (!wheelTracers.TryGetValue(index, out MeshRenderer tracer) || tracer == null)
            {
                tracer = CreateTracer("Taxi wheel Weatherade trace " + index);
                if (tracer == null) return;
                wheelTracers[index] = tracer;
            }
            PlaceTrace(tracer, from, to, width, .35f, .03f);
        }

        public void StopWheelTrace(int index)
        {
            if (wheelTracers.TryGetValue(index, out MeshRenderer tracer) && tracer != null) tracer.enabled = false;
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
            SetField(generator, "decaySpeed", .025f);
            type.GetMethod("Init", BindingFlags.Instance | BindingFlags.Public)?.Invoke(generator, null);
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
                    groundTargets.Add(renderer);
                    if (!roadTargets.Contains(renderer)) roadTargets.Add(renderer);
                }
            }
        }

        private void BindGround()
        {
            CacheTargets();
            foreach (MeshRenderer renderer in groundTargets)
            {
                if (renderer == null || groundBindings.ContainsKey(renderer)) continue;
                Material original = renderer.sharedMaterial;
                if (original == null || LwsWeatheradeMaterialFactory.IsWeatheradeMaterialForMode(original, LwsWeatheradeSurfaceMaterialMode.Snow)) continue;
                if (!LwsWeatheradeMaterialFactory.TryCreateWeatheradeRoadSurfaceMaterial(
                    "Taxi Weatherade ground " + renderer.name, original, Color.white,
                    LwsWeatheradeSurfaceMaterialMode.Snow, out Material snow)) continue;
                renderer.sharedMaterial = snow;
                groundBindings[renderer] = new GroundBinding { original = original, snow = snow };
            }
        }

        private void RestoreGround()
        {
            foreach (var pair in groundBindings)
            {
                if (pair.Key != null && pair.Key.sharedMaterial == pair.Value.snow) pair.Key.sharedMaterial = pair.Value.original;
                if (pair.Value.snow != null) Destroy(pair.Value.snow);
            }
            groundBindings.Clear();
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
            direction.y = 0;
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

        private void StopAllTraces()
        {
            StopPlowTrace();
            foreach (MeshRenderer tracer in wheelTracers.Values) if (tracer != null) tracer.enabled = false;
            foreach (MeshRenderer proxy in roadDepthProxies.Values) if (proxy != null) proxy.enabled = false;
        }

        private void OnDisable() { StopAllTraces(); RestoreGround(); RestoreCameraMasks(); }
        private void OnDestroy()
        {
            RestoreGround();
            RestoreCameraMasks();
            if (createdGenerator && generator != null) Destroy(generator);
            if (traceMaterial != null) Destroy(traceMaterial);
        }
    }
}
