using System;
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

        private ILwsRoadConditionService _roadConditionService;
        private ILwsPlayerVehicleService _playerVehicleService;
        private Type _coverageBaseType;
        private Type _rainCoverageType;
        private Type _snowCoverageType;
        private Component _coverageComponent;
        private CoverageMode _coverageMode = CoverageMode.None;
        private bool _createdCoverageObject;
        private LwsRoadConditionSnapshot _lastApplied;

        public string AdapterId => "weatherade.visuals";
        public bool IsAvailable => _rainCoverageType != null && _snowCoverageType != null;
        public string Status { get; private set; } = "Weatherade adapter not initialized.";

        private enum CoverageMode
        {
            None,
            Rain,
            Snow
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
        }

        public void ApplyRoadCondition(LwsRoadConditionSnapshot snapshot)
        {
            if (!IsAvailable)
            {
                Status = "Weatherade runtime classes were not found.";
                return;
            }

            CoverageMode desiredMode = ResolveCoverageMode(snapshot);
            if (!EnsureCoverage(desiredMode))
            {
                return;
            }

            ConfigureCoverageBase();

            if (_coverageComponent == null)
            {
                Status = "No Weatherade coverage component is active.";
                return;
            }

            if (desiredMode == CoverageMode.Snow)
            {
                float coverage = Mathf.Max(snapshot.snowDepth01, snapshot.packedSnow01, snapshot.ice01 * 0.35f);
                SetMember(_coverageComponent, "coverageAmount", Mathf.Clamp01(coverage));
            }
            else
            {
                SetMember(_coverageComponent, "wetnessAmount", Mathf.Clamp01(snapshot.wetness01));
                SetMember(_coverageComponent, "puddlesAmount", Mathf.Clamp01(snapshot.standingWater01));
                SetMember(_coverageComponent, "ripplesAmount", Mathf.RoundToInt(Mathf.Lerp(0f, 12f, snapshot.standingWater01)));
                SetMember(_coverageComponent, "ripplesIntensity", Mathf.Lerp(0f, 1f, snapshot.standingWater01));
                SetMember(_coverageComponent, "spotsIntensity", Mathf.Lerp(0f, 4f, snapshot.wetness01));
                SetMember(_coverageComponent, "dripsIntensity", Mathf.Lerp(0f, 2.5f, snapshot.wetness01));
            }

            InvokeOptional(_coverageComponent, "UpdateCoverageMaterials");
            _lastApplied = snapshot;
            Status = $"Weatherade {desiredMode} coverage applied for {snapshot.condition}.";
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
                desiredMode = _coverageMode == CoverageMode.Snow ? CoverageMode.Snow : CoverageMode.Rain;
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
