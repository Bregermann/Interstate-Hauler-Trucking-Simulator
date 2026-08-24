using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace LWS.InterstateHauler
{
    [DefaultExecutionOrder(-110)]
    [DisallowMultipleComponent]
    public sealed class LwsKeyboardGamepadTruckInputSource : MonoBehaviour, ILwsVehicleInputSource
    {
        [SerializeField] private bool registerAsFallbackDrivingSource = true;
        [SerializeField] private bool readKeyboard = true;
        [SerializeField] private bool readGamepad = true;
        [SerializeField] private bool automaticKeyboardDirectionPolicy = true;
        [SerializeField] private Lws18SpeedTransmissionController transmissionController;

        private readonly Dictionary<string, bool> _previous = new Dictionary<string, bool>();
        private ILwsVehicleInputService _inputService;
        private LwsVehicleCommandFrame _lastCommands;
        private LwsVehicleContinuousInput _lastContinuous;

        public string SourceId => "lws.input.keyboard-gamepad.truck";
        public LwsVehicleCommandFrame LastCommands => _lastCommands;

        public void ConfigureTransmissionController(Lws18SpeedTransmissionController controller)
        {
            transmissionController = controller;
        }

        private void Start()
        {
            ResolveServices();
            if (registerAsFallbackDrivingSource &&
                _inputService != null &&
                !_inputService.HasActiveSource)
            {
                _inputService.SetInputSource(this, LwsVehicleInputOwner.KeyboardMouse, true);
            }
        }

        private void Update()
        {
            _lastContinuous = ReadContinuousNow();
            _lastCommands = ReadCommandsNow();
        }

        public LwsVehicleContinuousInput ReadContinuousInput()
        {
            return _lastContinuous;
        }

        public LwsVehicleCommandFrame ReadCommandFrame()
        {
            return _lastCommands;
        }

        public LwsTruckGearIntent ReadGearIntent()
        {
            return new LwsTruckGearIntent
            {
                physicalGate = LwsTruckShifterGate.Neutral,
                neutralRequested = true
            };
        }

        public void SetSimulatedFrameForTests(LwsVehicleCommandFrame commands, LwsVehicleContinuousInput continuous = default)
        {
            _lastCommands = commands;
            _lastContinuous = continuous;
            enabled = false;
        }

        private LwsVehicleContinuousInput ReadContinuousNow()
        {
            var continuous = new LwsVehicleContinuousInput();
            bool forwardHeld = false;
            bool reverseHeld = false;
            if (readKeyboard && Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) continuous.steering -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) continuous.steering += 1f;
                forwardHeld = Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed;
                reverseHeld = Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed;
                if (!ApplyAutomaticKeyboardDirectionPolicy(ref continuous, forwardHeld, reverseHeld))
                {
                    if (forwardHeld) continuous.throttle = 1f;
                    if (reverseHeld) continuous.brake = 1f;
                }

                if (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed) continuous.clutch = 1f;
            }

            if (readGamepad && Gamepad.current != null)
            {
                continuous.steering = Mathf.Abs(Gamepad.current.leftStick.x.ReadValue()) > Mathf.Abs(continuous.steering)
                    ? Gamepad.current.leftStick.x.ReadValue()
                    : continuous.steering;
                continuous.throttle = Mathf.Max(continuous.throttle, Gamepad.current.rightTrigger.ReadValue());
                continuous.brake = Mathf.Max(continuous.brake, Gamepad.current.leftTrigger.ReadValue());
            }

            return continuous;
        }

        private bool ApplyAutomaticKeyboardDirectionPolicy(ref LwsVehicleContinuousInput continuous, bool forwardHeld, bool reverseHeld)
        {
            if (!automaticKeyboardDirectionPolicy || transmissionController == null)
            {
                return false;
            }

            LwsTransmissionDisplayState state = transmissionController.DisplayState;
            LwsAutomaticKeyboardDirectionDecision decision = ResolveAutomaticKeyboardDirection(
                forwardHeld,
                reverseHeld,
                state.mode,
                state.automaticSelector,
                state.signedSpeedMetersPerSecond,
                transmissionController.AutomaticDirectionChangeSpeedThresholdMetersPerSecond);

            if (!decision.handled)
            {
                return false;
            }

            if (decision.requestSelectorChange &&
                !transmissionController.TrySetAutomaticSelector(decision.requestedSelector, out string message))
            {
                Debug.LogWarning(message, this);
                continuous.throttle = 0f;
                continuous.brake = 1f;
                return true;
            }

            continuous.throttle = decision.throttle;
            continuous.brake = decision.brake;
            return true;
        }

        public static LwsAutomaticKeyboardDirectionDecision ResolveAutomaticKeyboardDirection(
            bool forwardHeld,
            bool reverseHeld,
            LwsTransmissionMode mode,
            LwsAutomaticTransmissionSelector currentSelector,
            float signedSpeedMetersPerSecond,
            float stoppedThresholdMetersPerSecond)
        {
            if (mode != LwsTransmissionMode.Automatic || !forwardHeld && !reverseHeld)
            {
                return LwsAutomaticKeyboardDirectionDecision.NotHandled(currentSelector);
            }

            if (forwardHeld && reverseHeld)
            {
                return LwsAutomaticKeyboardDirectionDecision.Handled(currentSelector, false, 0f, 1f);
            }

            float threshold = Mathf.Max(0f, stoppedThresholdMetersPerSecond);
            if (forwardHeld)
            {
                if (currentSelector == LwsAutomaticTransmissionSelector.Reverse && signedSpeedMetersPerSecond < -threshold)
                {
                    return LwsAutomaticKeyboardDirectionDecision.Handled(currentSelector, false, 0f, 1f);
                }

                return LwsAutomaticKeyboardDirectionDecision.Handled(LwsAutomaticTransmissionSelector.Drive, currentSelector != LwsAutomaticTransmissionSelector.Drive, 1f, 0f);
            }

            if (currentSelector == LwsAutomaticTransmissionSelector.Drive && signedSpeedMetersPerSecond > threshold)
            {
                return LwsAutomaticKeyboardDirectionDecision.Handled(currentSelector, false, 0f, 1f);
            }

            return LwsAutomaticKeyboardDirectionDecision.Handled(LwsAutomaticTransmissionSelector.Reverse, currentSelector != LwsAutomaticTransmissionSelector.Reverse, 1f, 0f);
        }

        private LwsVehicleCommandFrame ReadCommandsNow()
        {
            LwsVehicleCommandFrame keyboard = readKeyboard ? ReadKeyboardCommands() : default;
            LwsVehicleCommandFrame gamepad = readGamepad ? ReadGamepadCommands() : default;
            return LwsVehicleCommandFrameUtility.Combine(keyboard, gamepad);
        }

        private LwsVehicleCommandFrame ReadKeyboardCommands()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return default;
            }

            bool shift = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
            return new LwsVehicleCommandFrame
            {
                ignitionToggle = Edge("kb.ignition", keyboard.iKey.isPressed),
                engineStart = Edge("kb.engineStart", keyboard.eKey.isPressed && !shift),
                engineStop = Edge("kb.engineStop", keyboard.eKey.isPressed && shift),
                parkingBrakeToggle = Edge("kb.parkingBrake", keyboard.pKey.isPressed),
                lowBeamLights = Edge("kb.lowBeam", keyboard.lKey.isPressed),
                highBeamLights = Edge("kb.highBeam", keyboard.kKey.isPressed),
                leftIndicator = Edge("kb.leftSignal", keyboard.zKey.isPressed),
                rightIndicator = Edge("kb.rightSignal", keyboard.xKey.isPressed),
                hazardLights = Edge("kb.hazards", keyboard.jKey.isPressed),
                wipers = Edge("kb.wipers", keyboard.vKey.isPressed),
                horn = Edge("kb.horn", keyboard.hKey.isPressed),
                airHorn = Edge("kb.airHorn", keyboard.bKey.isPressed),
                engineBrake = Edge("kb.engineBrake", keyboard.mKey.isPressed),
                engineBrakeIncrease = Edge("kb.engineBrakeUp", keyboard.pageUpKey.isPressed),
                engineBrakeDecrease = Edge("kb.engineBrakeDown", keyboard.pageDownKey.isPressed),
                retarderIncrease = Edge("kb.retarderUp", keyboard.homeKey.isPressed),
                retarderDecrease = Edge("kb.retarderDown", keyboard.endKey.isPressed),
                differentialLock = Edge("kb.diffLock", keyboard.oKey.isPressed),
                cruiseControl = Edge("kb.cruiseToggle", keyboard.cKey.isPressed),
                cruiseSet = Edge("kb.cruiseSet", keyboard.rKey.isPressed && !shift),
                cruiseResume = Edge("kb.cruiseResume", keyboard.rKey.isPressed && shift),
                cruiseCancel = Edge("kb.cruiseCancel", keyboard.backspaceKey.isPressed),
                cruiseIncrease = Edge("kb.cruiseIncrease", keyboard.equalsKey.isPressed),
                cruiseDecrease = Edge("kb.cruiseDecrease", keyboard.minusKey.isPressed),
                trailerAttachDetach = Edge("kb.trailerAttach", keyboard.tKey.isPressed),
                trailerBrake = Edge("kb.trailerBrake", keyboard.spaceKey.isPressed),
                cameraCycle = Edge("kb.cameraCycle", keyboard.tabKey.isPressed),
                lookReset = Edge("kb.lookReset", keyboard.backquoteKey.isPressed),
                flipOffDriver = Edge("kb.flipOff", keyboard.fKey.isPressed),
                interact = Edge("kb.interact", keyboard.enterKey.isPressed),
                menuSubmit = Edge("kb.submit", keyboard.enterKey.isPressed),
                menuCancel = Edge("kb.cancel", keyboard.escapeKey.isPressed),
                pause = Edge("kb.pause", keyboard.escapeKey.isPressed),
                navigateUp = Edge("kb.navUp", keyboard.upArrowKey.isPressed),
                navigateDown = Edge("kb.navDown", keyboard.downArrowKey.isPressed),
                navigateLeft = Edge("kb.navLeft", keyboard.leftArrowKey.isPressed),
                navigateRight = Edge("kb.navRight", keyboard.rightArrowKey.isPressed)
            };
        }

        private LwsVehicleCommandFrame ReadGamepadCommands()
        {
            Gamepad gamepad = Gamepad.current;
            if (gamepad == null)
            {
                return default;
            }

            return new LwsVehicleCommandFrame
            {
                parkingBrakeToggle = Edge("gp.parkingBrake", gamepad.buttonEast.isPressed),
                lowBeamLights = Edge("gp.lowBeam", gamepad.buttonNorth.isPressed),
                highBeamLights = Edge("gp.highBeam", gamepad.buttonWest.isPressed),
                leftIndicator = Edge("gp.leftSignal", gamepad.leftShoulder.isPressed),
                rightIndicator = Edge("gp.rightSignal", gamepad.rightShoulder.isPressed),
                hazardLights = Edge("gp.hazards", gamepad.leftStickButton.isPressed),
                wipers = Edge("gp.wipers", gamepad.dpad.up.isPressed),
                horn = Edge("gp.horn", gamepad.buttonSouth.isPressed),
                airHorn = Edge("gp.airHorn", gamepad.rightStickButton.isPressed),
                cruiseControl = Edge("gp.cruiseToggle", gamepad.dpad.right.isPressed),
                cruiseCancel = Edge("gp.cruiseCancel", gamepad.dpad.left.isPressed),
                trailerAttachDetach = Edge("gp.trailerAttach", gamepad.dpad.down.isPressed),
                cameraCycle = Edge("gp.cameraCycle", gamepad.selectButton.isPressed),
                flipOffDriver = Edge("gp.flipOff", gamepad.leftStickButton.isPressed && gamepad.rightStickButton.isPressed),
                pause = Edge("gp.pause", gamepad.startButton.isPressed),
                menuSubmit = Edge("gp.submit", gamepad.buttonSouth.isPressed),
                menuCancel = Edge("gp.cancel", gamepad.buttonEast.isPressed),
                navigateUp = Edge("gp.navUp", gamepad.dpad.up.isPressed),
                navigateDown = Edge("gp.navDown", gamepad.dpad.down.isPressed),
                navigateLeft = Edge("gp.navLeft", gamepad.dpad.left.isPressed),
                navigateRight = Edge("gp.navRight", gamepad.dpad.right.isPressed)
            };
        }

        private LwsMomentaryIntent Edge(string key, bool current)
        {
            _previous.TryGetValue(key, out bool previous);
            _previous[key] = current;
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

        private void ResolveServices()
        {
            if (LwsApplicationBootstrap.Instance == null || LwsApplicationBootstrap.Instance.Registry == null)
            {
                return;
            }

            LwsApplicationBootstrap.Instance.Registry.TryGet(out _inputService);
            if (transmissionController == null)
            {
                transmissionController = GetComponent<Lws18SpeedTransmissionController>();
            }
        }
    }

    public readonly struct LwsAutomaticKeyboardDirectionDecision
    {
        private LwsAutomaticKeyboardDirectionDecision(
            bool handled,
            LwsAutomaticTransmissionSelector requestedSelector,
            bool requestSelectorChange,
            float throttle,
            float brake)
        {
            this.handled = handled;
            this.requestedSelector = requestedSelector;
            this.requestSelectorChange = requestSelectorChange;
            this.throttle = Mathf.Clamp01(throttle);
            this.brake = Mathf.Clamp01(brake);
        }

        public readonly bool handled;
        public readonly LwsAutomaticTransmissionSelector requestedSelector;
        public readonly bool requestSelectorChange;
        public readonly float throttle;
        public readonly float brake;

        public static LwsAutomaticKeyboardDirectionDecision NotHandled(LwsAutomaticTransmissionSelector selector)
        {
            return new LwsAutomaticKeyboardDirectionDecision(false, selector, false, 0f, 0f);
        }

        public static LwsAutomaticKeyboardDirectionDecision Handled(
            LwsAutomaticTransmissionSelector selector,
            bool requestSelectorChange,
            float throttle,
            float brake)
        {
            return new LwsAutomaticKeyboardDirectionDecision(true, selector, requestSelectorChange, throttle, brake);
        }
    }
}
