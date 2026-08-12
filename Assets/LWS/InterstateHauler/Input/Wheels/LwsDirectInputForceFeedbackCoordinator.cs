using UnityEngine;

namespace LWS.InterstateHauler
{
    [DisallowMultipleComponent]
    public sealed class LwsDirectInputForceFeedbackCoordinator : MonoBehaviour
    {
        [SerializeField] private LwsNwhVehicleAdapter vehicleAdapter;
        [SerializeField] private LwsWheelInputSource wheelInputSource;
        [SerializeField] private bool applyForces = true;
        [SerializeField, Range(1f, 45f)] private float fullRoadSpeedMetersPerSecond = 25f;
        [SerializeField, Range(0f, 1f)] private float baselineDamping = 0.2f;
        [SerializeField, Range(0f, 1f)] private float movingDamping = 0.45f;
        [SerializeField, Range(0f, 1f)] private float roadEffect = 0.15f;

        private ILwsForceFeedbackService _forceFeedbackService;

        private void Reset()
        {
            vehicleAdapter = FindFirstObjectByType<LwsNwhVehicleAdapter>();
            wheelInputSource = FindFirstObjectByType<LwsWheelInputSource>();
        }

        private void Awake()
        {
            if (vehicleAdapter == null)
            {
                vehicleAdapter = FindFirstObjectByType<LwsNwhVehicleAdapter>();
            }

            if (wheelInputSource == null)
            {
                wheelInputSource = GetComponent<LwsWheelInputSource>();
            }
        }

        private void Start()
        {
            ResolveServices();
        }

        private void Update()
        {
            ResolveServices();
            if (!applyForces || _forceFeedbackService == null || wheelInputSource == null || !wheelInputSource.HasConnectedDevice)
            {
                _forceFeedbackService?.DisableNow("DirectInput FFB coordinator inactive or wheel unavailable.");
                return;
            }

            if (vehicleAdapter == null || !vehicleAdapter.IsReady)
            {
                _forceFeedbackService.DisableNow("DirectInput FFB coordinator has no ready NWH vehicle adapter.");
                return;
            }

            LwsVehicleTelemetry telemetry = vehicleAdapter.ReadTelemetry();
            float speed01 = Mathf.Clamp01(Mathf.Abs(telemetry.signedSpeedMetersPerSecond) / Mathf.Max(1f, fullRoadSpeedMetersPerSecond));
            float alignment = -telemetry.steeringInput * speed01;
            float damping = Mathf.Lerp(baselineDamping, movingDamping, speed01);
            float road = telemetry.grounded ? speed01 * roadEffect : 0f;

            _forceFeedbackService.SetForces(alignment, damping, road, 0f);
        }

        private void OnDisable()
        {
            _forceFeedbackService?.DisableNow("DirectInput FFB coordinator disabled.");
        }

        private void OnApplicationQuit()
        {
            _forceFeedbackService?.DisableNow("Application quitting; DirectInput FFB disabled.");
        }

        private void ResolveServices()
        {
            if (_forceFeedbackService != null ||
                LwsApplicationBootstrap.Instance == null ||
                LwsApplicationBootstrap.Instance.Registry == null)
            {
                return;
            }

            LwsApplicationBootstrap.Instance.Registry.TryGet(out _forceFeedbackService);
        }
    }
}
