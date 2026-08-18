using UnityEngine;

namespace LWS.InterstateHauler
{
    [DisallowMultipleComponent]
    public sealed class LwsRoadConditionRuntimeController : MonoBehaviour
    {
        [SerializeField] private LwsRoadConditionPhysicsProfile physicsProfile;
        [SerializeField] private float updateIntervalSeconds = 0.25f;
        [SerializeField] private float fallbackRoadSearchDistanceMeters = 75f;

        private ILwsRoadConditionService _roadConditionService;
        private ILwsPlayerVehicleService _playerVehicleService;
        private ILwsWorldOriginService _originService;
        private float _elapsed;

        public bool IsReady => _roadConditionService != null;
        public LwsRoadConditionSnapshot CurrentSnapshot => _roadConditionService != null
            ? _roadConditionService.CurrentSnapshot
            : default;

        private void OnEnable()
        {
            ResolveServices();
            if (_roadConditionService != null && physicsProfile != null)
            {
                _roadConditionService.SetProfile(physicsProfile.Profile);
            }
        }

        private void Update()
        {
            if (_roadConditionService == null)
            {
                ResolveServices();
                if (_roadConditionService == null)
                {
                    return;
                }
            }

            _elapsed += Time.deltaTime;
            float interval = Mathf.Max(0.05f, updateIntervalSeconds);
            if (_elapsed < interval)
            {
                return;
            }

            float delta = _elapsed;
            _elapsed = 0f;
            UpdatePlayerRoadContext();
            _roadConditionService.Tick(delta);
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
        }

        private void UpdatePlayerRoadContext()
        {
            LwsPlayerTruck truck = _playerVehicleService != null ? _playerVehicleService.ActiveTruck : null;
            if (truck == null)
            {
                _roadConditionService.UpdatePlayerRoad(ResolveGlobalPosition(transform.position), transform.forward);
                _roadConditionService.SetVehicleSpeedMetersPerSecond(0f);
                return;
            }

            LwsVehicleTelemetry telemetry = truck.LastTelemetry;
            if (truck.NwhAdapter != null)
            {
                telemetry = truck.NwhAdapter.ReadTelemetry();
            }

            _roadConditionService.SetVehicleSpeedMetersPerSecond(Mathf.Abs(telemetry.signedSpeedMetersPerSecond));
            _roadConditionService.UpdatePlayerRoad(ResolveGlobalPosition(truck.transform.position), truck.transform.forward);
        }

        private Vector3 ResolveGlobalPosition(Vector3 localPosition)
        {
            return _originService != null
                ? _originService.LocalToGlobal(localPosition).ToVector3()
                : localPosition;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            updateIntervalSeconds = Mathf.Clamp(updateIntervalSeconds, 0.05f, 2f);
            fallbackRoadSearchDistanceMeters = Mathf.Clamp(fallbackRoadSearchDistanceMeters, 10f, 500f);
        }
#endif
    }
}
