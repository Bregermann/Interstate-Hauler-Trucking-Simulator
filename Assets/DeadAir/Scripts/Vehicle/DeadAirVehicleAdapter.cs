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
        [SerializeField] private LwsKeyboardGamepadTruckInputSource sharedKeyboardGamepadInputSource;
        [SerializeField] private LwsNwhTrailerCouplingAdapter trailerCouplingAdapter;
        [SerializeField] private DeadAirBasicAutomaticInputSource basicAutomaticInputSource;
        [SerializeField] private Rigidbody fallbackRigidbody;
        [SerializeField] private float movingThresholdMph = 1f;

        private bool _hornActive;
        private bool _wasHornActive;
        private bool _deadAirTrailerConsideredConnected;
        private bool _deadAirDrivingInputLocked;
        private GameObject _deadAirDeliveryTrailer;

        public event Action HornStarted;
        public event Action HornStopped;
        public DeadAirVehicleSnapshot CurrentSnapshot { get; private set; }
        public bool CameraCycleSuppressed => sharedKeyboardGamepadInputSource != null && sharedKeyboardGamepadInputSource.CameraCycleSuppressed ||
                                             basicAutomaticInputSource != null && !basicAutomaticInputSource.CameraCycleAllowed;
        public bool TrailerAttachedForDeadAir => trailerCouplingAdapter != null && trailerCouplingAdapter.IsTrailerAttached;
        public bool TrailerConsideredConnectedForDeadAir => TrailerAttachedForDeadAir || _deadAirTrailerConsideredConnected;
        public bool DeadAirDrivingInputLocked => _deadAirDrivingInputLocked;
        public string CurrentTrailerId => trailerCouplingAdapter != null && !string.IsNullOrWhiteSpace(trailerCouplingAdapter.CurrentTrailerId)
            ? trailerCouplingAdapter.CurrentTrailerId
            : _deadAirDeliveryTrailer != null ? _deadAirDeliveryTrailer.name : string.Empty;

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
            if (sharedKeyboardGamepadInputSource != null)
            {
                sharedKeyboardGamepadInputSource.enabled = true;
                sharedKeyboardGamepadInputSource.SetDrivingInputSuppressed(false);
                sharedKeyboardGamepadInputSource.ConfigureTransmissionController(transmissionController);
                sharedKeyboardGamepadInputSource.SetCameraCycleSuppressed(true);
                if (!sharedKeyboardGamepadInputSource.TrySetInputMode(LwsTruckInputMode.BasicAutomatic, out string message))
                {
                    automaticSet = false;
                    Debug.LogWarning(message, this);
                }
            }
            else if (transmissionController != null &&
                !transmissionController.TrySetAutomaticMode(out string message))
            {
                automaticSet = false;
                Debug.LogWarning(message, this);
            }

            if (basicAutomaticInputSource != null)
            {
                basicAutomaticInputSource.enabled = false;
                basicAutomaticInputSource.SetDrivingInputSuppressed(false);
                basicAutomaticInputSource.SetCameraCycleAllowed(false);
                basicAutomaticInputSource.SetTransmissionController(transmissionController);
            }

            _deadAirDrivingInputLocked = false;
            return automaticSet;
        }

        public void SetDeadAirCameraCycleSuppressed(bool suppressed)
        {
            ResolveReferences();
            if (sharedKeyboardGamepadInputSource != null)
            {
                sharedKeyboardGamepadInputSource.SetCameraCycleSuppressed(suppressed);
            }

            if (basicAutomaticInputSource != null)
            {
                basicAutomaticInputSource.SetCameraCycleAllowed(!suppressed);
            }
        }

        public void ConfigureDeadAirTrailerState(bool consideredConnected, GameObject deliveryTrailer)
        {
            _deadAirTrailerConsideredConnected = consideredConnected;
            if (deliveryTrailer != null)
            {
                _deadAirDeliveryTrailer = deliveryTrailer;
            }
        }

        public void RequestTrailerAttachDetach()
        {
            ResolveReferences();
            trailerCouplingAdapter?.RequestAttachDetach();
        }

        public void SetDeadAirDrivingInputLocked(bool locked)
        {
            ResolveReferences();
            _deadAirDrivingInputLocked = locked;
            if (sharedKeyboardGamepadInputSource != null)
            {
                sharedKeyboardGamepadInputSource.SetDrivingInputSuppressed(locked);
            }

            if (basicAutomaticInputSource != null)
            {
                basicAutomaticInputSource.SetDrivingInputSuppressed(locked);
            }
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
                    transmissionMode = transmission.mode.ToString(),
                    trailerConnected = TrailerConsideredConnectedForDeadAir
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
                transmissionMode = transmissionController != null ? transmissionController.DisplayState.mode.ToString() : "Unknown",
                trailerConnected = TrailerConsideredConnectedForDeadAir
            };
        }

        private bool ResolveHornActive()
        {
            if (sharedKeyboardGamepadInputSource != null)
            {
                LwsVehicleCommandFrame commands = sharedKeyboardGamepadInputSource.LastCommands;
                return LwsVehicleCommandFrameUtility.IsActive(commands.horn) ||
                       LwsVehicleCommandFrameUtility.IsActive(commands.airHorn);
            }

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
            if (sharedKeyboardGamepadInputSource == null) sharedKeyboardGamepadInputSource = GetComponent<LwsKeyboardGamepadTruckInputSource>();
            if (sharedKeyboardGamepadInputSource == null) sharedKeyboardGamepadInputSource = gameObject.AddComponent<LwsKeyboardGamepadTruckInputSource>();
            if (trailerCouplingAdapter == null) trailerCouplingAdapter = GetComponent<LwsNwhTrailerCouplingAdapter>();
            if (trailerCouplingAdapter == null) trailerCouplingAdapter = gameObject.AddComponent<LwsNwhTrailerCouplingAdapter>();
            if (basicAutomaticInputSource == null) basicAutomaticInputSource = GetComponent<DeadAirBasicAutomaticInputSource>();
            if (fallbackRigidbody == null) fallbackRigidbody = GetComponent<Rigidbody>();
        }
    }
}
