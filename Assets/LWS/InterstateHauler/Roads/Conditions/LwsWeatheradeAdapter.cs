using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEngine;

namespace LWS.InterstateHauler
{
    [DisallowMultipleComponent]
    public sealed class LwsWeatheradeAdapter : MonoBehaviour, ILwsRoadConditionVisualAdapter
    {
        private const string CoverageBaseTypeName = "NOT_Lonely.Weatherade.CoverageBase";
        private const string RainCoverageTypeName = "NOT_Lonely.Weatherade.RainCoverage";
        private const string SnowCoverageTypeName = "NOT_Lonely.Weatherade.SnowCoverage";
        private const string GeneratedCoverageObjectName = "IH Weatherade Road Coverage";

        [SerializeField] private bool attachOnEnable = true;
        [SerializeField] private float areaSizeMeters = 180f;
        [SerializeField] private float areaDepthMeters = 70f;
        [SerializeField] private LayerMask depthLayerMask = -1;
        [SerializeField] private bool autoBindWeatheradeRoadMaterials = true;
        [SerializeField] private float roadMaterialRefreshIntervalSeconds = 1f;

        private readonly Dictionary<Renderer, SurfaceMaterialBinding> _surfaceMaterialBindings = new Dictionary<Renderer, SurfaceMaterialBinding>();
        private readonly HashSet<Renderer> _scannedRenderers = new HashSet<Renderer>();
        private ILwsRoadConditionService _roadConditionService;
        private ILwsPlayerVehicleService _playerVehicleService;
        private ILwsWorldOriginService _originService;
        private Type _coverageBaseType;
        private Type _rainCoverageType;
        private Type _snowCoverageType;
        private Component _coverageComponent;
        private CoverageMode _coverageMode = CoverageMode.None;
        private LwsWeatheradeSurfaceMaterialMode _lastSurfaceMaterialMode = LwsWeatheradeSurfaceMaterialMode.Rain;
        private float _nextRoadMaterialRefreshTime;
        private bool _createdCoverageObject;
        private bool _originEventsSubscribed;
        private LwsRoadConditionSnapshot _lastApplied;

        public string AdapterId => "weatherade.visuals";
        public bool IsAvailable => _rainCoverageType != null && _snowCoverageType != null;
        public string Status { get; private set; } = "Weatherade adapter not initialized.";
        public int BoundWeatheradeSurfaceCount { get; private set; }
        public int IncompatibleSurfaceMaterialCount { get; private set; }
        public string RoadMaterialDiagnostic { get; private set; } = "Weatherade road materials not scanned.";
        public string RainCoverageDiagnostic { get; private set; } = "RainCoverage not applied.";
        public string SnowCoverageDiagnostic { get; private set; } = "SnowCoverage not applied.";
        public string DepthRendererDiagnostic { get; private set; } = "Weatherade depth renderer owned by CoverageBase.";
        public string LastVendorApplyDiagnostic { get; private set; } = "No Weatherade apply yet.";
        public bool RoadMaterialCompatible => BoundWeatheradeSurfaceCount > 0 && IncompatibleSurfaceMaterialCount == 0;

        private enum CoverageMode
        {
            None,
            Rain,
            Snow
        }

        private sealed class SurfaceMaterialBinding
        {
            public Material OriginalMaterial;
            public Material RuntimeMaterial;
            public LwsWeatheradeSurfaceMaterialMode Mode;
        }

        private void OnEnable()
        {
            ResolveTypes();
            ResolveServices();
            if (attachOnEnable)
            {
                _roadConditionService?.AttachVisualAdapter(this);
            }
        }

        private void OnDisable()
        {
            _roadConditionService?.DetachVisualAdapter(this);
            if (_originService != null && _originEventsSubscribed)
            {
                _originService.OriginShiftCompleted -= HandleOriginShiftCompleted;
                _originEventsSubscribed = false;
            }

            RestoreOriginalRoadMaterials();
        }

        public void ApplyRoadCondition(LwsRoadConditionSnapshot snapshot)
        {
            ResolveTypes();
            if (!IsAvailable)
            {
                Status = "Weatherade runtime classes were not found.";
                return;
            }

            CoverageMode requestedMode = ResolveCoverageMode(snapshot);
            CoverageMode activeCoverageMode = requestedMode == CoverageMode.Snow ? CoverageMode.Snow : CoverageMode.Rain;
            if (!EnsureCoverage(activeCoverageMode))
            {
                return;
            }

            ConfigureCoverageBase();

            if (_coverageComponent == null)
            {
                Status = "No Weatherade coverage component is active.";
                return;
            }

            ApplyWeatheradeRoadMaterials(activeCoverageMode, false);

            if (activeCoverageMode == CoverageMode.Snow)
            {
                float coverage = Mathf.Max(snapshot.snowDepth01, snapshot.packedSnow01, snapshot.ice01 * 0.35f);
                SetMember(_coverageComponent, "coverageAmount", Mathf.Clamp01(coverage));
                SnowCoverageDiagnostic = $"{_coverageComponent.GetType().Name} coverage {ReadFloatMember(_coverageComponent, "coverageAmount"):0.00}.";
                RainCoverageDiagnostic = "RainCoverage inactive while SnowCoverage owns the active Weatherade instance.";
            }
            else
            {
                float wetness = requestedMode == CoverageMode.None ? 0f : snapshot.wetness01;
                float puddles = requestedMode == CoverageMode.None ? 0f : snapshot.standingWater01;
                SetMember(_coverageComponent, "wetnessAmount", Mathf.Clamp01(wetness));
                SetMember(_coverageComponent, "puddlesAmount", Mathf.Clamp01(puddles));
                SetMember(_coverageComponent, "ripplesAmount", Mathf.RoundToInt(Mathf.Lerp(0f, 12f, puddles)));
                SetMember(_coverageComponent, "ripplesIntensity", Mathf.Lerp(0f, 1f, puddles));
                SetMember(_coverageComponent, "spotsIntensity", Mathf.Lerp(0f, 4f, wetness));
                SetMember(_coverageComponent, "dripsIntensity", Mathf.Lerp(0f, 2.5f, wetness));
                RainCoverageDiagnostic = $"{_coverageComponent.GetType().Name} wetness {ReadFloatMember(_coverageComponent, "wetnessAmount"):0.00}, puddles {ReadFloatMember(_coverageComponent, "puddlesAmount"):0.00}.";
                SnowCoverageDiagnostic = "SnowCoverage inactive while RainCoverage owns the active Weatherade instance.";
            }

            bool updated = InvokeOptional(_coverageComponent, "UpdateCoverageMaterials");
            _lastApplied = snapshot;
            string requestedLabel = requestedMode == CoverageMode.None ? "dry baseline" : requestedMode.ToString();
            LastVendorApplyDiagnostic = updated
                ? $"Weatherade UpdateCoverageMaterials invoked for {requestedLabel}."
                : "Weatherade UpdateCoverageMaterials API was not found.";
            Status = $"Weatherade {requestedLabel} coverage applied for {snapshot.condition}. {RoadMaterialDiagnostic}";
        }

        public void ApplyQualityTier(LwsRenderQualityTier tier)
        {
            switch (tier)
            {
                case LwsRenderQualityTier.Ultra:
                    areaSizeMeters = 240f;
                    areaDepthMeters = 90f;
                    break;
                case LwsRenderQualityTier.High:
                    areaSizeMeters = 200f;
                    areaDepthMeters = 80f;
                    break;
                case LwsRenderQualityTier.Medium:
                    areaSizeMeters = 160f;
                    areaDepthMeters = 70f;
                    break;
                case LwsRenderQualityTier.Low:
                case LwsRenderQualityTier.SteamDeck:
                    areaSizeMeters = 110f;
                    areaDepthMeters = 55f;
                    break;
            }

            ConfigureCoverageBase();
            if (_coverageComponent != null)
            {
                InvokeOptional(_coverageComponent, "UpdateCoverageMaterials");
            }
        }

        private void ResolveServices()
        {
            if (LwsApplicationBootstrap.Instance == null || LwsApplicationBootstrap.Instance.Registry == null)
            {
                return;
            }

            LwsApplicationBootstrap.Instance.Registry.TryGet(out _roadConditionService);
            LwsApplicationBootstrap.Instance.Registry.TryGet(out _playerVehicleService);
            LwsApplicationBootstrap.Instance.Registry.TryGet(out _originService);
            if (_originService != null && !_originEventsSubscribed)
            {
                _originService.OriginShiftCompleted += HandleOriginShiftCompleted;
                _originEventsSubscribed = true;
            }
        }

        private void ResolveTypes()
        {
            _coverageBaseType ??= ResolveType(CoverageBaseTypeName);
            _rainCoverageType ??= ResolveType(RainCoverageTypeName);
            _snowCoverageType ??= ResolveType(SnowCoverageTypeName);
            Status = IsAvailable
                ? "Weatherade RainCoverage and SnowCoverage APIs are available."
                : "Weatherade RainCoverage/SnowCoverage APIs are not available.";
        }

        private bool EnsureCoverage(CoverageMode desiredMode)
        {
            if (desiredMode == CoverageMode.None)
            {
                desiredMode = CoverageMode.Rain;
            }

            Type desiredType = desiredMode == CoverageMode.Snow ? _snowCoverageType : _rainCoverageType;
            if (_coverageComponent != null && desiredType.IsInstanceOfType(_coverageComponent))
            {
                _coverageMode = desiredMode;
                return true;
            }

            Component existing = GetWeatheradeSingleton();
            if (existing != null && desiredType.IsInstanceOfType(existing))
            {
                _coverageComponent = existing;
                _coverageMode = desiredMode;
                _createdCoverageObject = existing.gameObject.name == GeneratedCoverageObjectName;
                return true;
            }

            if (existing != null && !_createdCoverageObject)
            {
                Status = "A Weatherade coverage instance already exists with a different mode; leaving it untouched.";
                return false;
            }

            DestroyOwnedCoverage();

            GameObject coverageObject = new GameObject(GeneratedCoverageObjectName);
            coverageObject.transform.SetParent(transform, false);
            coverageObject.transform.localPosition = Vector3.zero;
            coverageObject.transform.localRotation = Quaternion.identity;
            _coverageComponent = coverageObject.AddComponent(desiredType);
            _createdCoverageObject = true;
            _coverageMode = desiredMode;
            return _coverageComponent != null;
        }

        private void ConfigureCoverageBase()
        {
            if (_coverageComponent == null)
            {
                return;
            }

            SetMember(_coverageComponent, "areaSize", Mathf.Max(20f, areaSizeMeters));
            SetMember(_coverageComponent, "areaDepth", Mathf.Max(5f, areaDepthMeters));
            SetMember(_coverageComponent, "depthLayerMask", depthLayerMask);
            SetMember(_coverageComponent, "useFollowTarget", true);
            SetMember(_coverageComponent, "followTarget", ResolveFollowTarget());
            DepthRendererDiagnostic = $"Coverage area {areaSizeMeters:0}m, depth {areaDepthMeters:0}m, layer mask {depthLayerMask.value}.";
        }

        private Transform ResolveFollowTarget()
        {
            LwsPlayerTruck truck = _playerVehicleService != null ? _playerVehicleService.ActiveTruck : null;
            if (truck != null)
            {
                return truck.transform;
            }

            Camera mainCamera = Camera.main;
            return mainCamera != null ? mainCamera.transform : transform;
        }

        private void HandleOriginShiftCompleted(LwsOriginShiftEvent shiftEvent)
        {
            ConfigureCoverageBase();
            if (_coverageComponent != null)
            {
                ApplyWeatheradeRoadMaterials(_coverageMode == CoverageMode.Snow ? CoverageMode.Snow : CoverageMode.Rain, true);
                InvokeOptional(_coverageComponent, "UpdateCoverageMaterials");
                Status = $"Weatherade coverage rebound after origin shift {shiftEvent.NewOriginVersion}. {RoadMaterialDiagnostic}";
            }
        }

        private void ApplyWeatheradeRoadMaterials(CoverageMode coverageMode, bool force)
        {
            if (!autoBindWeatheradeRoadMaterials)
            {
                RoadMaterialDiagnostic = "Automatic Weatherade road material binding is disabled.";
                return;
            }

            LwsWeatheradeSurfaceMaterialMode materialMode = coverageMode == CoverageMode.Snow
                ? LwsWeatheradeSurfaceMaterialMode.Snow
                : LwsWeatheradeSurfaceMaterialMode.Rain;
            float now = Application.isPlaying ? Time.unscaledTime : 0f;
            bool refreshDue = force || materialMode != _lastSurfaceMaterialMode || BoundWeatheradeSurfaceCount == 0 || now >= _nextRoadMaterialRefreshTime;
            if (!refreshDue)
            {
                return;
            }

            _lastSurfaceMaterialMode = materialMode;
            _nextRoadMaterialRefreshTime = now + Mathf.Max(0.1f, roadMaterialRefreshIntervalSeconds);
            RefreshRoadSurfaceMaterials(materialMode);
        }

        private void RefreshRoadSurfaceMaterials(LwsWeatheradeSurfaceMaterialMode materialMode)
        {
            PruneDeadMaterialBindings();
            _scannedRenderers.Clear();
            BoundWeatheradeSurfaceCount = 0;
            IncompatibleSurfaceMaterialCount = 0;
            int surfaceCount = 0;

            LwsRoadSurface[] roadSurfaces = FindObjectsByType<LwsRoadSurface>(FindObjectsSortMode.None);
            for (int i = 0; i < roadSurfaces.Length; i++)
            {
                LwsRoadSurface surface = roadSurfaces[i];
                if (surface == null)
                {
                    continue;
                }

                surfaceCount++;
                Renderer renderer = surface.GetComponent<Renderer>();
                if (renderer != null)
                {
                    BindRoadRenderer(renderer, materialMode);
                    continue;
                }

                MeshRenderer[] childRenderers = surface.GetComponentsInChildren<MeshRenderer>(true);
                for (int rendererIndex = 0; rendererIndex < childRenderers.Length; rendererIndex++)
                {
                    BindRoadRenderer(childRenderers[rendererIndex], materialMode);
                }
            }

            RoadMaterialDiagnostic = $"{BoundWeatheradeSurfaceCount} Weatherade-compatible road renderers, {IncompatibleSurfaceMaterialCount} incompatible, {surfaceCount} LwsRoadSurface objects scanned in {materialMode} mode.";
        }

        private void BindRoadRenderer(Renderer renderer, LwsWeatheradeSurfaceMaterialMode materialMode)
        {
            if (renderer == null || !_scannedRenderers.Add(renderer))
            {
                return;
            }

            Material current = renderer.sharedMaterial;
            if (current == null)
            {
                IncompatibleSurfaceMaterialCount++;
                return;
            }

            if (_surfaceMaterialBindings.TryGetValue(renderer, out SurfaceMaterialBinding existingBinding))
            {
                Material original = existingBinding.OriginalMaterial;
                if (LwsWeatheradeMaterialFactory.IsWeatheradeMaterialForMode(original, materialMode))
                {
                    renderer.sharedMaterial = original;
                    DestroyRuntimeMaterial(existingBinding.RuntimeMaterial);
                    _surfaceMaterialBindings.Remove(renderer);
                    BoundWeatheradeSurfaceCount++;
                    return;
                }

                if (existingBinding.RuntimeMaterial != null && existingBinding.Mode == materialMode && renderer.sharedMaterial == existingBinding.RuntimeMaterial)
                {
                    BoundWeatheradeSurfaceCount++;
                    return;
                }
            }
            else if (LwsWeatheradeMaterialFactory.IsWeatheradeMaterialForMode(current, materialMode))
            {
                BoundWeatheradeSurfaceCount++;
                return;
            }

            Material source = existingBinding != null && existingBinding.OriginalMaterial != null ? existingBinding.OriginalMaterial : current;
            if (!LwsWeatheradeMaterialFactory.TryCreateWeatheradeRoadSurfaceMaterial($"IH Weatherade {materialMode} {renderer.gameObject.name}", source, ResolveFallbackRoadColor(materialMode), materialMode, out Material runtimeMaterial))
            {
                IncompatibleSurfaceMaterialCount++;
                return;
            }

            if (existingBinding != null)
            {
                DestroyRuntimeMaterial(existingBinding.RuntimeMaterial);
            }

            renderer.sharedMaterial = runtimeMaterial;
            _surfaceMaterialBindings[renderer] = new SurfaceMaterialBinding
            {
                OriginalMaterial = source,
                RuntimeMaterial = runtimeMaterial,
                Mode = materialMode
            };
            BoundWeatheradeSurfaceCount++;
        }

        private static Color ResolveFallbackRoadColor(LwsWeatheradeSurfaceMaterialMode mode)
        {
            return mode == LwsWeatheradeSurfaceMaterialMode.Snow
                ? new Color(0.22f, 0.23f, 0.24f, 1f)
                : new Color(0.065f, 0.065f, 0.06f, 1f);
        }

        private void PruneDeadMaterialBindings()
        {
            if (_surfaceMaterialBindings.Count == 0)
            {
                return;
            }

            List<Renderer> dead = null;
            foreach (KeyValuePair<Renderer, SurfaceMaterialBinding> pair in _surfaceMaterialBindings)
            {
                if (pair.Key == null)
                {
                    dead ??= new List<Renderer>();
                    dead.Add(pair.Key);
                    DestroyRuntimeMaterial(pair.Value.RuntimeMaterial);
                }
            }

            if (dead == null)
            {
                return;
            }

            for (int i = 0; i < dead.Count; i++)
            {
                _surfaceMaterialBindings.Remove(dead[i]);
            }
        }

        private void RestoreOriginalRoadMaterials()
        {
            if (_surfaceMaterialBindings.Count == 0)
            {
                return;
            }

            foreach (KeyValuePair<Renderer, SurfaceMaterialBinding> pair in _surfaceMaterialBindings)
            {
                Renderer renderer = pair.Key;
                SurfaceMaterialBinding binding = pair.Value;
                if (renderer != null && renderer.sharedMaterial == binding.RuntimeMaterial)
                {
                    renderer.sharedMaterial = binding.OriginalMaterial;
                }

                DestroyRuntimeMaterial(binding.RuntimeMaterial);
            }

            _surfaceMaterialBindings.Clear();
        }

        private static void DestroyRuntimeMaterial(Material material)
        {
            if (material == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(material);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(material);
            }
        }

        private Component GetWeatheradeSingleton()
        {
            if (_coverageBaseType == null)
            {
                return null;
            }

            FieldInfo field = _coverageBaseType.GetField("instance", BindingFlags.Static | BindingFlags.Public);
            return field?.GetValue(null) as Component;
        }

        private CoverageMode ResolveCoverageMode(LwsRoadConditionSnapshot snapshot)
        {
            float snowVisual = Mathf.Max(snapshot.snowDepth01, snapshot.packedSnow01, snapshot.ice01 * 0.35f);
            if (snowVisual > 0.02f)
            {
                return CoverageMode.Snow;
            }

            if (snapshot.wetness01 > 0.02f || snapshot.standingWater01 > 0.02f)
            {
                return CoverageMode.Rain;
            }

            return CoverageMode.None;
        }

        private void DestroyOwnedCoverage()
        {
            if (!_createdCoverageObject || _coverageComponent == null)
            {
                _coverageComponent = null;
                return;
            }

            GameObject coverageObject = _coverageComponent.gameObject;
            ClearWeatheradeSingletonIfOwned(_coverageComponent);
            _coverageComponent = null;
            _createdCoverageObject = false;
            if (coverageObject == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(coverageObject);
            }
            else
            {
                DestroyImmediate(coverageObject);
            }
        }

        private void ClearWeatheradeSingletonIfOwned(Component component)
        {
            if (_coverageBaseType == null || component == null)
            {
                return;
            }

            FieldInfo field = _coverageBaseType.GetField("instance", BindingFlags.Static | BindingFlags.Public);
            if (field != null && ReferenceEquals(field.GetValue(null), component))
            {
                field.SetValue(null, null);
            }
        }

        private static Type ResolveType(string typeName)
        {
            Type type = Type.GetType(typeName) ?? Type.GetType($"{typeName}, Assembly-CSharp");
            if (type != null)
            {
                return type;
            }

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                type = assemblies[i].GetType(typeName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static bool SetMember(object target, string memberName, object value)
        {
            if (target == null)
            {
                return false;
            }

            Type type = target.GetType();
            FieldInfo field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
            {
                field.SetValue(target, value);
                return true;
            }

            PropertyInfo property = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null && property.CanWrite)
            {
                property.SetValue(target, value);
                return true;
            }

            return false;
        }

        private static object GetMember(object target, string memberName)
        {
            if (target == null)
            {
                return null;
            }

            Type type = target.GetType();
            FieldInfo field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
            {
                return field.GetValue(target);
            }

            PropertyInfo property = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return property != null && property.CanRead ? property.GetValue(target) : null;
        }

        private static float ReadFloatMember(object target, string memberName)
        {
            object value = GetMember(target, memberName);
            if (value == null)
            {
                return 0f;
            }

            if (value is float floatValue)
            {
                return floatValue;
            }

            if (value is int intValue)
            {
                return intValue;
            }

            if (value is IConvertible convertible)
            {
                try
                {
                    return Convert.ToSingle(convertible, CultureInfo.InvariantCulture);
                }
                catch (FormatException)
                {
                    return 0f;
                }
                catch (InvalidCastException)
                {
                    return 0f;
                }
            }

            return 0f;
        }

        private static bool InvokeOptional(object target, string methodName)
        {
            if (target == null)
            {
                return false;
            }

            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (method == null || method.GetParameters().Length != 0)
            {
                return false;
            }

            method.Invoke(target, null);
            return true;
        }
    }
}