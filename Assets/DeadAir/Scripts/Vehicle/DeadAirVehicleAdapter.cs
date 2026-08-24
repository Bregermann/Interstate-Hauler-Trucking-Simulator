using System;
using LWS.InterstateHauler;
using UnityEngine;

namespace DeadAir
{
    [DefaultExecutionOrder(-40)]
    [DisallowMultipleComponent]
    public sealed class DeadAirVehicleAdapter : MonoBehaviour
    {
        [SerializeField] private LwsNwhVehicleAdapter lwsVehicleAdapter;
        [SerializeField] private Lws18SpeedTransmissionController transmissionController;
        [SerializeField] private DeadAirBasicAutomaticInputSource basicAutomaticInputSource;
        [SerializeField] private Rigidbody fallbackRigidbody;
        [SerializeField] private float movingThresholdMph = 1f;

        private bool _hornActive;
        private bool _wasHornActive;

        public event Action HornStarted;
        public event Action HornStopped;
        public DeadAirVehicleSnapshot CurrentSnapshot { get; private set; }

        private void Reset()
        {
            ResolveReferences();
        }

        private void Awake()
        {
            ResolveReferences();
        }

        private void Update()
        {
            CurrentSnapshot = CaptureSnapshot();
            if (CurrentSnapshot.hornActive != _wasHornActive)
            {
                if (CurrentSnapshot.hornActive)
                {
                    HornStarted?.Invoke();
                }
                else
                {
                    HornStopped?.Invoke();
                }

                _wasHornActive = CurrentSnapshot.hornActive;
            }
        }

        public bool EnableBasicAutomatic()
        {
            ResolveReferences();
            bool automaticSet = true;
            if (transmissionController != null &&
                !transmissionController.TrySetDevelopmentAutomaticTestMode(true, out string message))
            {
                automaticSet = false;
                Debug.LogWarning(message, this);
            }

            if (basicAutomaticInputSource != null)
            {
                basicAutomaticInputSource.SetTransmissionController(transmissionController);
                basicAutomaticInputSource.Activate();
            }

            return automaticSet;
        }

        public DeadAirVehicleSnapshot CaptureSnapshot()
        {
            ResolveReferences();
            if (lwsVehicleAdapter != null && lwsVehicleAdapter.IsReady)
            {
                LwsVehicleTelemetry telemetry = lwsVehicleAdapter.ReadTelemetry();
                LwsTransmissionDisplayState transmission = transmissionController != null ? transmissionController.DisplayState : default;
                _hornActive = ResolveHornActive();
                return new DeadAirVehicleSnapshot
                {
                    available = true,
                    speedMph = Mathf.Abs(telemetry.speedMetersPerSecond) * 2.23693629f,
                    signedSpeedMph = telemetry.signedSpeedMetersPerSecond * 2.23693629f,
                    position = telemetry.worldPosition,
                    forward = transform.forward,
                    moving = Mathf.Abs(telemetry.signedSpeedMetersPerSecond) * 2.23693629f >= movingThresholdMph,
                    reverse = telemetry.reverse || telemetry.signedSpeedMetersPerSecond < -0.25f,
                    hornActive = _hornActive,
                    transmissionMode = transmission.mode.ToString()
                };
            }

            Vector3 velocity = fallbackRigidbody != null ? fallbackRigidbody.linearVelocity : Vector3.zero;
            float signed = Vector3.Dot(velocity, transform.forward);
            _hornActive = ResolveHornActive();
            return new DeadAirVehicleSnapshot
            {
                available = fallbackRigidbody != null,
                speedMph = velocity.magnitude * 2.23693629f,
                signedSpeedMph = signed * 2.23693629f,
                position = transform.position,
                forward = transform.forward,
                moving = velocity.magnitude * 2.23693629f >= movingThresholdMph,
                reverse = signed < -0.25f,
                hornActive = _hornActive,
                transmissionMode = transmissionController != null ? transmissionController.DisplayState.mode.ToString() : "Unknown"
            };
        }

        private bool ResolveHornActive()
        {
            if (basicAutomaticInputSource != null)
            {
                return basicAutomaticInputSource.HornHeld;
            }

            return false;
        }

        private void ResolveReferences()
        {
            if (lwsVehicleAdapter == null) lwsVehicleAdapter = GetComponent<LwsNwhVehicleAdapter>();
            if (transmissionController == null) transmissionController = GetComponent<Lws18SpeedTransmissionController>();
            if (basicAutomaticInputSource == null) basicAutomaticInputSource = GetComponent<DeadAirBasicAutomaticInputSource>();
            if (basicAutomaticInputSource == null) basicAutomaticInputSource = gameObject.AddComponent<DeadAirBasicAutomaticInputSource>();
            if (fallbackRigidbody == null) fallbackRigidbody = GetComponent<Rigidbody>();
        }
    }
}
