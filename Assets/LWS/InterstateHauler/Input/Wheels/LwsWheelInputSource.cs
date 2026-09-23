using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LWS.InterstateHauler
{
    [DefaultExecutionOrder(-120)]
    [DisallowMultipleComponent]
    public sealed class LwsWheelInputSource : MonoBehaviour, ILwsVehicleInputSource
    {
        [SerializeField] private LwsWheelDeviceProfile deviceProfile;
        [SerializeField] private LwsWheelCalibrationAsset calibrationTemplate;
        [SerializeField] private bool preferWheelAtStart = true;
        [SerializeField] private bool useGlobalCalibrationService = true;
        [SerializeField] private bool neutralizeOnDisconnect = true;
        [SerializeField] private bool toggleRangeOnPress = true;
        [SerializeField] private bool toggleSplitterOnPress = true;
        [SerializeField] private bool useSimulatedInputForTests;
        [SerializeField, Min(0.25f)] private float disconnectedDeviceScanIntervalSeconds = 2f;
        [SerializeField] private LwsWheelInputFrame simulatedFrame;

        private readonly Dictionary<LwsWheelLogicalControl, bool> _previousButtonStates = new Dictionary<LwsWheelLogicalControl, bool>();
        private ILwsWheelCalibrationService _calibrationService;
        private ILwsForceFeedbackService _forceFeedbackService;
        private InputDevice _selectedDevice;
        private LwsWheelCalibrationProfile _calibrationProfile;
        private LwsWheelInputFrame _lastFrame;
        private LwsTruckRange _rangeState;
        private LwsTruckSplitter _splitterState;
        private bool _disconnectNoticeLogged;
        private float _nextDisconnectedDeviceScanTime;

        public string SourceId => "lws.input.wheel.directinput";
        public LwsWheelDeviceProfile DeviceProfile => deviceProfile;
        public LwsWheelCalibrationProfile CalibrationProfile => _calibrationProfile;
        public InputDevice SelectedDevice => _selectedDevice;
        public LwsWheelInputFrame LastFrame => _lastFrame;
        public bool HasConnectedDevice => useSimulatedInputForTests
            ? simulatedFrame.connectionState == LwsWheelConnectionState.Connected
            : _selectedDevice != null && _selectedDevice.added;

        public event Action<LwsWheelInputFrame> FrameUpdated;

        private void Awake()
        {
            _calibrationProfile = CreateTemplateProfileCopy();
            _lastFrame = CreateNeutralFrame(LwsWheelConnectionState.Disconnected);
        }

        private void OnEnable()
        {
            InputSystem.onDeviceChange += OnDeviceChange;
        }

        private void OnDisable()
        {
            InputSystem.onDeviceChange -= OnDeviceChange;
            NeutralizeForDisconnect("Wheel input source disabled.");
        }

        private void Start()
        {
            ResolveServices();
            if (useGlobalCalibrationService && _calibrationService?.CurrentProfile != null)
            {
                _calibrationProfile = _calibrationService.CurrentProfile;
            }

            if (preferWheelAtStart)
            {
                SelectPreferredDevice();
            }

            _forceFeedbackService?.ApplySettings(_calibrationProfile.forceFeedback);
        }

        private void Update()
        {
            if (useSimulatedInputForTests)
            {
                _lastFrame = simulatedFrame;
                FrameUpdated?.Invoke(_lastFrame);
                return;
            }

            if (_selectedDevice == null && Time.unscaledTime >= _nextDisconnectedDeviceScanTime)
            {
                _nextDisconnectedDeviceScanTime = Time.unscaledTime + Mathf.Max(0.25f, disconnectedDeviceScanIntervalSeconds);
                SelectPreferredDevice();
            }

            if (_selectedDevice == null || !_selectedDevice.added)
            {
                if (neutralizeOnDisconnect && _lastFrame.connectionState != LwsWheelConnectionState.Disconnected)
                {
                    NeutralizeForDisconnect("Wheel disconnected or unavailable.");
                }

                return;
            }

            _disconnectNoticeLogged = false;
            _lastFrame = ReadHardwareFrame();
            FrameUpdated?.Invoke(_lastFrame);
        }

        public LwsVehicleContinuousInput ReadContinuousInput()
        {
            return new LwsVehicleContinuousInput
            {
                steering = _lastFrame.analog.steering,
                throttle = _lastFrame.analog.throttle,
                brake = _lastFrame.analog.brake,
                clutch = _lastFrame.analog.clutch,
                parkingBrake = _lastFrame.analog.parkingBrake
            };
        }

        public LwsVehicleCommandFrame ReadCommandFrame()
        {
            return _lastFrame.commands;
        }

        public LwsTruckGearIntent ReadGearIntent()
        {
            return LwsWheelCalibrationUtility.ToValidationGearIntent(
                _lastFrame.shifter,
                _calibrationProfile != null && _calibrationProfile.validationGearMappingEnabled);
        }

        public bool SelectPreferredDevice()
        {
            InputDevice savedDevice = _calibrationProfile != null
                ? LwsWheelDeviceDiscovery.FindDeviceByPath(_calibrationProfile.selectedDevicePath)
                : null;
            _selectedDevice = savedDevice != null
                ? savedDevice
                : LwsWheelDeviceDiscovery.FindFirstMatchingDevice(deviceProfile);

            if (_selectedDevice == null)
            {
                _lastFrame = CreateNeutralFrame(LwsWheelConnectionState.Disconnected);
                _nextDisconnectedDeviceScanTime = Time.unscaledTime + Mathf.Max(0.25f, disconnectedDeviceScanIntervalSeconds);
                return false;
            }

            if (_calibrationProfile != null)
            {
                _calibrationProfile.selectedDevicePath = _selectedDevice.path;
                _calibrationProfile.selectedDeviceLayout = _selectedDevice.layout;
                _calibrationProfile.selectedDeviceDisplayName = _selectedDevice.displayName ?? string.Empty;
            }

            _lastFrame = ReadHardwareFrame();
            _forceFeedbackService?.ApplySettings(_calibrationProfile.forceFeedback);
            return true;
        }

        public void SetCalibrationProfile(LwsWheelCalibrationProfile profile)
        {
            _calibrationProfile = profile ?? LwsWheelCalibrationProfile.CreateDefaultLogitechG29();
            _calibrationProfile.NormalizeBindingLogicalControls();
            _calibrationService?.SetProfile(_calibrationProfile);
            _forceFeedbackService?.ApplySettings(_calibrationProfile.forceFeedback);
            _nextDisconnectedDeviceScanTime = 0f;
        }

        public LwsServiceResult SaveCalibration()
        {
            if (_calibrationService == null)
            {
                return LwsServiceResult.Failure("Wheel calibration service is not available.");
            }

            _calibrationService.SetProfile(_calibrationProfile);
            return _calibrationService.SaveGlobalProfile();
        }

        public void NeutralizeForDisconnect(string reason)
        {
            _lastFrame = CreateNeutralFrame(LwsWheelConnectionState.Disconnected);
            _forceFeedbackService?.DisableNow(reason);
            if (!_disconnectNoticeLogged && !useSimulatedInputForTests && !string.IsNullOrWhiteSpace(reason))
            {
                Debug.LogWarning(reason, this);
                _disconnectNoticeLogged = true;
            }

            FrameUpdated?.Invoke(_lastFrame);
        }

        public void SetSimulatedFrameForTests(LwsWheelInputFrame frame)
        {
            useSimulatedInputForTests = true;
            simulatedFrame = frame;
            _lastFrame = frame;
        }

        public bool TryAssignDominantControl(LwsWheelLogicalControl logicalControl, LwsWheelControlKind kind)
        {
            if (_selectedDevice == null)
            {
                SelectPreferredDevice();
            }

            if (!LwsWheelDeviceDiscovery.TryFindDominantControl(_selectedDevice, kind, out LwsWheelControlBinding binding))
            {
                return false;
            }

            binding.logicalControl = logicalControl;
            _calibrationProfile.SetBinding(binding);
            return true;
        }

        public bool TryReadRawBinding(LwsWheelControlBinding binding, out float rawValue)
        {
            return LwsWheelDeviceDiscovery.TryReadAxis(_selectedDevice, binding, out rawValue);
        }

        private void ResolveServices()
        {
            if (LwsApplicationBootstrap.Instance == null || LwsApplicationBootstrap.Instance.Registry == null)
            {
                return;
            }

            LwsApplicationBootstrap.Instance.Registry.TryGet(out _calibrationService);
            LwsApplicationBootstrap.Instance.Registry.TryGet(out _forceFeedbackService);
        }

        private LwsWheelInputFrame ReadHardwareFrame()
        {
            var analog = new LwsWheelAnalogState();
            if (_calibrationProfile != null)
            {
                if (LwsWheelDeviceDiscovery.TryReadAxis(_selectedDevice, _calibrationProfile.steeringBinding, out analog.rawSteering))
                {
                    analog.steering = LwsWheelCalibrationUtility.NormalizeSteering(analog.rawSteering, _calibrationProfile.steering);
                }

                if (LwsWheelDeviceDiscovery.TryReadAxis(_selectedDevice, _calibrationProfile.throttleBinding, out analog.rawThrottle))
                {
                    analog.throttle = LwsWheelCalibrationUtility.NormalizePedal(analog.rawThrottle, _calibrationProfile.throttle);
                }

                if (LwsWheelDeviceDiscovery.TryReadAxis(_selectedDevice, _calibrationProfile.brakeBinding, out analog.rawBrake))
                {
                    analog.brake = LwsWheelCalibrationUtility.NormalizePedal(analog.rawBrake, _calibrationProfile.brake);
                }

                if (LwsWheelDeviceDiscovery.TryReadAxis(_selectedDevice, _calibrationProfile.clutchBinding, out analog.rawClutch))
                {
                    analog.clutch = LwsWheelCalibrationUtility.NormalizePedal(analog.rawClutch, _calibrationProfile.clutch);
                }

                analog.parkingBrake = ReadBoundButton(_calibrationProfile.parkingBrakeBinding) ? 1f : 0f;
            }

            LwsHPatternShifterState shifter = ReadShifterState();
            LwsVehicleCommandFrame commands = ReadCommands();
            LwsForceFeedbackStatus ffbStatus = _forceFeedbackService?.Status ?? default;

            return new LwsWheelInputFrame
            {
                connectionState = LwsWheelConnectionState.Connected,
                deviceDisplayName = _selectedDevice != null ? _selectedDevice.displayName : string.Empty,
                devicePath = _selectedDevice != null ? _selectedDevice.path : string.Empty,
                analog = analog,
                shifter = shifter,
                commands = commands,
                forceFeedback = ffbStatus
            };
        }

        private LwsHPatternShifterState ReadShifterState()
        {
            if (_calibrationProfile == null)
            {
                return LwsHPatternShifterState.Neutral(_rangeState, _splitterState);
            }

            bool rangePressed = ReadButtonIntent(_calibrationProfile.rangeBinding) == LwsMomentaryIntent.Pressed;
            bool splitterPressed = ReadButtonIntent(_calibrationProfile.splitterBinding) == LwsMomentaryIntent.Pressed;
            if (toggleRangeOnPress && rangePressed)
            {
                _rangeState = _rangeState == LwsTruckRange.Low ? LwsTruckRange.High : LwsTruckRange.Low;
            }

            if (toggleSplitterOnPress && splitterPressed)
            {
                _splitterState = _splitterState == LwsTruckSplitter.Low ? LwsTruckSplitter.High : LwsTruckSplitter.Low;
            }

            LwsTruckShifterGate gate = LwsTruckShifterGate.Neutral;
            if (ReadBoundButton(_calibrationProfile.reverseBinding)) gate = LwsTruckShifterGate.Reverse;
            else if (ReadBoundButton(_calibrationProfile.gate1Binding)) gate = LwsTruckShifterGate.Gate1;
            else if (ReadBoundButton(_calibrationProfile.gate2Binding)) gate = LwsTruckShifterGate.Gate2;
            else if (ReadBoundButton(_calibrationProfile.gate3Binding)) gate = LwsTruckShifterGate.Gate3;
            else if (ReadBoundButton(_calibrationProfile.gate4Binding)) gate = LwsTruckShifterGate.Gate4;
            else if (ReadBoundButton(_calibrationProfile.gate5Binding)) gate = LwsTruckShifterGate.Gate5;
            else if (ReadBoundButton(_calibrationProfile.gate6Binding)) gate = LwsTruckShifterGate.Gate6;

            return new LwsHPatternShifterState
            {
                activeGate = gate,
                neutral = gate == LwsTruckShifterGate.Neutral,
                reverse = gate == LwsTruckShifterGate.Reverse,
                range = _rangeState,
                splitter = _splitterState,
                rangePressed = rangePressed,
                splitterPressed = splitterPressed
            };
        }

        private LwsVehicleCommandFrame ReadCommands()
        {
            if (_calibrationProfile == null)
            {
                return default;
            }

            LwsMomentaryIntent ignition = ReadButtonIntent(_calibrationProfile.ignitionBinding);
            return new LwsVehicleCommandFrame
            {
                ignitionToggle = ignition,
                ignition = ignition,
                engineStart = ReadButtonIntent(_calibrationProfile.engineStartBinding),
                engineStop = ReadButtonIntent(_calibrationProfile.engineStopBinding),
                parkingBrakeToggle = ReadButtonIntent(_calibrationProfile.parkingBrakeBinding),
                lowBeamLights = ReadButtonIntent(_calibrationProfile.headlightsBinding),
                highBeamLights = ReadButtonIntent(_calibrationProfile.highBeamsBinding),
                leftIndicator = ReadButtonIntent(_calibrationProfile.leftSignalBinding),
                rightIndicator = ReadButtonIntent(_calibrationProfile.rightSignalBinding),
                hazardLights = ReadButtonIntent(_calibrationProfile.hazardsBinding),
                wipers = ReadButtonIntent(_calibrationProfile.wipersBinding),
                horn = ReadButtonIntent(_calibrationProfile.hornBinding),
                airHorn = ReadButtonIntent(_calibrationProfile.airHornBinding),
                engineBrake = ReadButtonIntent(_calibrationProfile.engineBrakeBinding),
                retarderIncrease = ReadButtonIntent(_calibrationProfile.retarderIncreaseBinding),
                retarderDecrease = ReadButtonIntent(_calibrationProfile.retarderDecreaseBinding),
                differentialLock = ReadButtonIntent(_calibrationProfile.differentialLockBinding),
                trailerAttachDetach = ReadButtonIntent(_calibrationProfile.trailerAttachDetachBinding),
                trailerBrake = ReadButtonIntent(_calibrationProfile.trailerBrakeBinding),
                cameraCycle = ReadButtonIntent(_calibrationProfile.cameraCycleBinding),
                lookReset = ReadButtonIntent(_calibrationProfile.lookResetBinding),
                resetTruckUpright = ReadButtonIntent(_calibrationProfile.resetTruckUprightBinding),
                flipOffDriver = ReadButtonIntent(_calibrationProfile.flipOffDriverBinding),
                interact = ReadButtonIntent(_calibrationProfile.interactBinding),
                menuSubmit = ReadButtonIntent(_calibrationProfile.menuSubmitBinding),
                menuCancel = ReadButtonIntent(_calibrationProfile.menuCancelBinding),
                pause = ReadButtonIntent(_calibrationProfile.pauseBinding),
                navigateUp = ReadButtonIntent(_calibrationProfile.dpadUpBinding),
                navigateDown = ReadButtonIntent(_calibrationProfile.dpadDownBinding),
                navigateLeft = ReadButtonIntent(_calibrationProfile.dpadLeftBinding),
                navigateRight = ReadButtonIntent(_calibrationProfile.dpadRightBinding)
            };
        }

        private LwsMomentaryIntent ReadButtonIntent(LwsWheelControlBinding binding)
        {
            bool current = ReadBoundButton(binding);
            _previousButtonStates.TryGetValue(binding.logicalControl, out bool previous);
            _previousButtonStates[binding.logicalControl] = current;

            if (current && !previous)
            {
                return LwsMomentaryIntent.Pressed;
            }

            if (!current && previous)
            {
                return LwsMomentaryIntent.Released;
            }

            return current ? LwsMomentaryIntent.Held : LwsMomentaryIntent.None;
        }

        private bool ReadBoundButton(LwsWheelControlBinding binding)
        {
            return LwsWheelDeviceDiscovery.TryReadButton(_selectedDevice, binding, out bool pressed) && pressed;
        }

        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (device == null)
            {
                return;
            }

            if (_selectedDevice != null && device.deviceId == _selectedDevice.deviceId &&
                (change == InputDeviceChange.Disconnected || change == InputDeviceChange.Removed))
            {
                _selectedDevice = null;
                NeutralizeForDisconnect("Wheel disconnected; vehicle input neutralized.");
                return;
            }

            if (_selectedDevice == null &&
                (change == InputDeviceChange.Added || change == InputDeviceChange.Reconnected))
            {
                _nextDisconnectedDeviceScanTime = 0f;
                SelectPreferredDevice();
            }
        }

        private LwsWheelCalibrationProfile CreateTemplateProfileCopy()
        {
            if (calibrationTemplate != null && calibrationTemplate.Profile != null)
            {
                return LwsWheelCalibrationProfile.FromJson(calibrationTemplate.Profile.ToJson());
            }

            return LwsWheelCalibrationProfile.CreateDefaultLogitechG29();
        }

        private static LwsWheelInputFrame CreateNeutralFrame(LwsWheelConnectionState connectionState)
        {
            return new LwsWheelInputFrame
            {
                connectionState = connectionState,
                shifter = LwsHPatternShifterState.Neutral()
            };
        }
    }
}
