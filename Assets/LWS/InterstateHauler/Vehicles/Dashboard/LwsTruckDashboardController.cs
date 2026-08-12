using System;
using System.Collections.Generic;
using NWH.VehiclePhysics2.VehicleGUI;
using UnityEngine;

namespace LWS.InterstateHauler
{
    [DefaultExecutionOrder(170)]
    [DisallowMultipleComponent]
    public sealed class LwsTruckDashboardController : MonoBehaviour
    {
        [SerializeField] private LwsTruckDashboardDefinition dashboardDefinition;
        [SerializeField] private LwsVehicleIdentity identity;
        [SerializeField] private LwsNwhVehicleAdapter vehicleAdapter;
        [SerializeField] private LwsTruckControlController truckControlController;
        [SerializeField] private Lws18SpeedTransmissionController transmissionController;
        [SerializeField] private AnalogGauge speedometer;
        [SerializeField] private AnalogGauge tachometer;
        [SerializeField] private DigitalGauge gearDisplay;
        [SerializeField] private DashLight leftSignalIndicator;
        [SerializeField] private DashLight rightSignalIndicator;
        [SerializeField] private DashLight highBeamIndicator;
        [SerializeField] private DashLight checkEngineIndicator;
        [SerializeField] private Transform steeringWheelVisual;
        [SerializeField] private float steeringWheelMaximumAngle = 450f;
        [SerializeField] private bool disableNwhDashGuiController = true;
        [SerializeField] private bool createAuxiliaryStatusPanel = true;
        [SerializeField] private bool registerWithService = true;

        private readonly Dictionary<LwsDashboardIndicatorId, TextMesh> _auxIndicators = new Dictionary<LwsDashboardIndicatorId, TextMesh>();
        private ILwsTruckDashboardService _dashboardService;
        private Quaternion _steeringWheelBaseRotation;
        private bool _steeringWheelBaseCaptured;
        private bool _serviceRegistered;
        private LwsTruckDashboardSnapshot _snapshot;

        public LwsTruckDashboardSnapshot CurrentSnapshot => _snapshot;
        public LwsTruckDashboardDefinition DashboardDefinition => dashboardDefinition;
        public bool HasSpeedometer => speedometer != null;
        public bool HasTachometer => tachometer != null;
        public bool HasGearDisplay => gearDisplay != null;
        public bool HasSteeringWheelVisual => steeringWheelVisual != null;

        private void Reset()
        {
            ResolveReferences();
        }

        private void Awake()
        {
            ResolveReferences();
            if (disableNwhDashGuiController)
            {
                DisableNativeDashGuiControllers();
            }

            CaptureSteeringWheelBaseRotation();
        }

        private void Start()
        {
            ResolveServices();
            RegisterWithServiceIfNeeded();
            EnsureAuxiliaryStatusPanel();
            ConfigureNativeGauges();
        }

        private void OnDisable()
        {
            if (_serviceRegistered && _dashboardService != null)
            {
                _dashboardService.ClearActiveController(this);
                _serviceRegistered = false;
            }
        }

        private void Update()
        {
            ResolveServices();
            RegisterWithServiceIfNeeded();

            _snapshot = BuildSnapshot();
            ApplyDashboardBindings(_snapshot);
            _dashboardService?.PublishSnapshot(this, _snapshot);
        }

        public void Configure(LwsTruckDashboardDefinition definition)
        {
            dashboardDefinition = definition;
            ConfigureNativeGauges();
        }

        public bool ValidateBindings(out string message)
        {
            ResolveReferences();
            if (dashboardDefinition == null)
            {
                message = "Dashboard definition is missing.";
                return false;
            }

            if (speedometer == null || tachometer == null || gearDisplay == null)
            {
                message = "Required speedometer, tachometer, or gear display binding is missing.";
                return false;
            }

            if (leftSignalIndicator == null || rightSignalIndicator == null || highBeamIndicator == null)
            {
                message = "Required native signal/high-beam indicator binding is missing.";
                return false;
            }

            if (steeringWheelVisual == null)
            {
                message = "Steering-wheel visual binding is missing.";
                return false;
            }

            message = "Truck dashboard bindings are valid.";
            return true;
        }

        public LwsTruckDashboardSnapshot BuildSnapshot()
        {
            LwsVehicleTelemetry telemetry = vehicleAdapter != null ? vehicleAdapter.ReadTelemetry() : default;
            LwsTruckControlState controlState = truckControlController != null ? truckControlController.CurrentState : default;
            LwsTransmissionDisplayState transmission = transmissionController != null
                ? transmissionController.DisplayState
                : controlState.transmission;

            LwsDashboardSpeedUnit speedUnit = dashboardDefinition != null
                ? dashboardDefinition.SpeedUnit
                : LwsDashboardSpeedUnit.MilesPerHour;
            float displaySpeed = dashboardDefinition != null
                ? dashboardDefinition.MetersPerSecondToDisplaySpeed(telemetry.signedSpeedMetersPerSecond)
                : Mathf.Abs(telemetry.signedSpeedMetersPerSecond) * 2.23693629f;

            return new LwsTruckDashboardSnapshot
            {
                vehicleId = identity != null ? identity.VehicleId : telemetry.vehicleId,
                rawSpeedMetersPerSecond = telemetry.signedSpeedMetersPerSecond,
                displaySpeed = displaySpeed,
                speedUnit = speedUnit,
                engineRpm = telemetry.engineRpm,
                ignitionState = controlState.ignitionState,
                engineRunning = controlState.engineRunning || telemetry.engineRunning,
                engineStalled = controlState.engineStalled || telemetry.engineStalled,
                parkingBrakeOn = controlState.parkingBrakeOn || telemetry.parkingBrakeActive,
                serviceBrakeInput = Mathf.Max(controlState.serviceBrakeInput, telemetry.brakeInput),
                trailerBrakeHeld = controlState.trailerBrakeHeld,
                headlightsOn = controlState.headlightsOn,
                highBeamsOn = controlState.highBeamsOn,
                turnSignal = controlState.turnSignal,
                hazardsOn = controlState.hazardsOn,
                wiperState = controlState.wiperState,
                engineBrakeLevel = controlState.engineBrakeLevel,
                retarderLevel = controlState.retarderLevel,
                differentialLocked = controlState.differentialLocked,
                cruiseEnabled = controlState.cruiseEnabled,
                cruiseTargetSpeedMetersPerSecond = controlState.cruiseTargetSpeedMetersPerSecond,
                trailerAttached = controlState.trailerAttached || telemetry.trailerAttached,
                trailerId = !string.IsNullOrEmpty(controlState.trailerId) ? controlState.trailerId : telemetry.trailerId,
                transmissionLabel = !string.IsNullOrEmpty(transmission.displayLabel) ? transmission.displayLabel : "N",
                requestedRange = transmission.requestedRange,
                engagedRange = transmission.engagedRange,
                requestedSplitter = transmission.requestedSplitter,
                engagedSplitter = transmission.engagedSplitter,
                shiftState = transmission.shiftState,
                rejectionReason = transmission.lastRejectionReason,
                abuseSeverity = transmission.lastAbuseSeverity,
                requiresShifterSynchronization = transmission.requiresShifterSynchronization,
                steeringInput = telemetry.steeringInput,
                throttleInput = telemetry.throttleInput,
                brakeInput = telemetry.brakeInput,
                clutchInput = telemetry.clutchInput
            };
        }

        private void ApplyDashboardBindings(LwsTruckDashboardSnapshot snapshot)
        {
            if (speedometer != null)
            {
                speedometer.Value = Mathf.Max(0f, snapshot.displaySpeed);
            }

            if (tachometer != null)
            {
                tachometer.Value = Mathf.Max(0f, snapshot.engineRpm);
            }

            if (gearDisplay != null)
            {
                gearDisplay.gaugeType = DigitalGauge.GaugeType.Textual;
                gearDisplay.unit = string.Empty;
                gearDisplay.stringValue = snapshot.transmissionLabel;
            }

            float blinkTime = Time.time;
            SetDashLight(leftSignalIndicator, snapshot.ShowLeftSignal(blinkTime));
            SetDashLight(rightSignalIndicator, snapshot.ShowRightSignal(blinkTime));
            SetDashLight(highBeamIndicator, snapshot.highBeamsOn);
            SetDashLight(checkEngineIndicator, snapshot.ShowCheckEngine());
            ApplyAuxiliaryIndicator(LwsDashboardIndicatorId.Ignition, snapshot.ignitionState != LwsIgnitionState.Off, $"IGN {snapshot.ignitionState}");
            ApplyAuxiliaryIndicator(LwsDashboardIndicatorId.EngineRunning, snapshot.engineRunning, snapshot.engineRunning ? "ENG RUN" : "ENG OFF");
            ApplyAuxiliaryIndicator(LwsDashboardIndicatorId.ParkingBrake, snapshot.parkingBrakeOn, "PARK");
            ApplyAuxiliaryIndicator(LwsDashboardIndicatorId.EngineBrake, snapshot.engineBrakeLevel > 0, $"ENG BRK {snapshot.engineBrakeLevel}");
            ApplyAuxiliaryIndicator(LwsDashboardIndicatorId.Retarder, snapshot.retarderLevel > 0, $"RET {snapshot.retarderLevel}");
            ApplyAuxiliaryIndicator(LwsDashboardIndicatorId.DifferentialLock, snapshot.differentialLocked, "DIFF");
            ApplyAuxiliaryIndicator(LwsDashboardIndicatorId.Cruise, snapshot.cruiseEnabled, $"CRUISE {MetersPerSecondToMph(snapshot.cruiseTargetSpeedMetersPerSecond):0}");
            ApplyAuxiliaryIndicator(LwsDashboardIndicatorId.TrailerAttached, snapshot.trailerAttached, "TRAILER");
            AnimateSteeringWheel(snapshot.steeringInput);
        }

        private void ConfigureNativeGauges()
        {
            if (dashboardDefinition == null)
            {
                return;
            }

            if (speedometer != null)
            {
                LwsDashboardGaugeMapping mapping = dashboardDefinition.Speedometer;
                speedometer.maxValue = mapping.maximumValue;
                speedometer.startAngle = mapping.startAngle;
                speedometer.endAngle = mapping.endAngle;
                speedometer.needleSmoothing = mapping.smoothing;
            }

            if (tachometer != null)
            {
                LwsDashboardGaugeMapping mapping = dashboardDefinition.Tachometer;
                tachometer.maxValue = mapping.maximumValue;
                tachometer.startAngle = mapping.startAngle;
                tachometer.endAngle = mapping.endAngle;
                tachometer.needleSmoothing = mapping.smoothing;
            }
        }

        private void AnimateSteeringWheel(float steeringInput)
        {
            if (steeringWheelVisual == null)
            {
                return;
            }

            CaptureSteeringWheelBaseRotation();
            float angle = Mathf.Clamp(steeringInput, -1f, 1f) * steeringWheelMaximumAngle;
            steeringWheelVisual.localRotation = _steeringWheelBaseRotation * Quaternion.Euler(0f, 0f, -angle);
        }

        private void EnsureAuxiliaryStatusPanel()
        {
            if (!createAuxiliaryStatusPanel || _auxIndicators.Count > 0)
            {
                return;
            }

            Transform dashboardRoot = FindChildRecursive(transform, "DashInstruments") ?? FindChildRecursive(transform, "Cab") ?? transform;
            var panel = new GameObject("IH_DashboardAuxStatusPanel");
            panel.transform.SetParent(dashboardRoot, false);
            panel.transform.localPosition = new Vector3(0f, 0.04f, 0.02f);
            panel.transform.localRotation = Quaternion.identity;
            panel.transform.localScale = Vector3.one;

            LwsDashboardIndicatorId[] rows =
            {
                LwsDashboardIndicatorId.Ignition,
                LwsDashboardIndicatorId.EngineRunning,
                LwsDashboardIndicatorId.ParkingBrake,
                LwsDashboardIndicatorId.EngineBrake,
                LwsDashboardIndicatorId.Retarder,
                LwsDashboardIndicatorId.DifferentialLock,
                LwsDashboardIndicatorId.Cruise,
                LwsDashboardIndicatorId.TrailerAttached
            };

            for (int i = 0; i < rows.Length; i++)
            {
                var row = new GameObject($"IH_DashIndicator_{rows[i]}");
                row.transform.SetParent(panel.transform, false);
                row.transform.localPosition = new Vector3(0f, -0.045f * i, 0f);
                row.transform.localRotation = Quaternion.identity;
                row.transform.localScale = Vector3.one;
                TextMesh text = row.AddComponent<TextMesh>();
                text.text = rows[i].ToString();
                text.characterSize = 0.035f;
                text.anchor = TextAnchor.MiddleLeft;
                text.alignment = TextAlignment.Left;
                text.color = new Color(0.18f, 0.18f, 0.18f);
                _auxIndicators[rows[i]] = text;
            }
        }

        private void ApplyAuxiliaryIndicator(LwsDashboardIndicatorId indicator, bool active, string label)
        {
            if (!_auxIndicators.TryGetValue(indicator, out TextMesh text) || text == null)
            {
                return;
            }

            text.text = label;
            text.color = active ? new Color(0.12f, 0.95f, 0.42f) : new Color(0.08f, 0.08f, 0.08f);
        }

        private void ResolveReferences()
        {
            LwsPlayerTruck truck = GetComponent<LwsPlayerTruck>();
            if (dashboardDefinition == null && truck != null && truck.Definition != null)
            {
                dashboardDefinition = truck.Definition.DashboardDefinition;
            }

            if (identity == null) identity = GetComponent<LwsVehicleIdentity>();
            if (vehicleAdapter == null) vehicleAdapter = GetComponent<LwsNwhVehicleAdapter>();
            if (truckControlController == null) truckControlController = GetComponent<LwsTruckControlController>();
            if (transmissionController == null) transmissionController = GetComponent<Lws18SpeedTransmissionController>();
            if (speedometer == null) speedometer = FindGauge<AnalogGauge>("SpeedGaugeAnalog", "Speed");
            if (tachometer == null) tachometer = FindGauge<AnalogGauge>("RPMGaugeAnalog", "RPM");
            if (gearDisplay == null) gearDisplay = FindGauge<DigitalGauge>("GearGaugeDigital", "Gear");
            if (leftSignalIndicator == null) leftSignalIndicator = FindDashLight("Left Blinker", "LeftBlinker");
            if (rightSignalIndicator == null) rightSignalIndicator = FindDashLight("Right Blinker", "RightBlinker");
            if (highBeamIndicator == null) highBeamIndicator = FindDashLight("High Beam", "HighBeam");
            if (checkEngineIndicator == null) checkEngineIndicator = FindDashLight("Check Engine", "CheckEngine");
            if (steeringWheelVisual == null) steeringWheelVisual = FindChildRecursive(transform, "steering wheel");
        }

        private void ResolveServices()
        {
            if (_dashboardService != null || LwsApplicationBootstrap.Instance == null || LwsApplicationBootstrap.Instance.Registry == null)
            {
                return;
            }

            LwsApplicationBootstrap.Instance.Registry.TryGet(out _dashboardService);
        }

        private void RegisterWithServiceIfNeeded()
        {
            if (!registerWithService || _serviceRegistered || _dashboardService == null)
            {
                return;
            }

            LwsServiceResult result = _dashboardService.RegisterActiveController(this);
            _serviceRegistered = result.Succeeded;
            if (!result.Succeeded)
            {
                Debug.LogError(result.Message, this);
            }
        }

        private void DisableNativeDashGuiControllers()
        {
            DashGUIController[] nativeControllers = GetComponentsInChildren<DashGUIController>(true);
            for (int i = 0; i < nativeControllers.Length; i++)
            {
                nativeControllers[i].enabled = false;
            }
        }

        private void CaptureSteeringWheelBaseRotation()
        {
            if (_steeringWheelBaseCaptured || steeringWheelVisual == null)
            {
                return;
            }

            _steeringWheelBaseRotation = steeringWheelVisual.localRotation;
            _steeringWheelBaseCaptured = true;
        }

        private T FindGauge<T>(params string[] candidates) where T : Component
        {
            T[] components = GetComponentsInChildren<T>(true);
            return FindByName(components, candidates);
        }

        private DashLight FindDashLight(params string[] candidates)
        {
            DashLight[] lights = GetComponentsInChildren<DashLight>(true);
            return FindByName(lights, candidates);
        }

        private static T FindByName<T>(T[] components, params string[] candidates) where T : Component
        {
            foreach (string candidate in candidates)
            {
                for (int i = 0; i < components.Length; i++)
                {
                    if (components[i].name.IndexOf(candidate, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return components[i];
                    }
                }
            }

            return null;
        }

        private static Transform FindChildRecursive(Transform parent, string childName)
        {
            if (parent == null)
            {
                return null;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name.IndexOf(childName, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return child;
                }

                Transform nested = FindChildRecursive(child, childName);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        private static void SetDashLight(DashLight light, bool active)
        {
            if (light != null)
            {
                light.Active = active;
            }
        }

        private static float MetersPerSecondToMph(float metersPerSecond)
        {
            return Mathf.Max(0f, metersPerSecond) * 2.23693629f;
        }
    }
}
