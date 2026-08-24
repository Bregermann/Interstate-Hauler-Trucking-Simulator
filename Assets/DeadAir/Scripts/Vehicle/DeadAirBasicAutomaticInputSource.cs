using System.Collections.Generic;
using LWS.InterstateHauler;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DeadAir
{
    [DefaultExecutionOrder(-125)]
    [DisallowMultipleComponent]
    public sealed class DeadAirBasicAutomaticInputSource : MonoBehaviour, ILwsVehicleInputSource
    {
        [SerializeField] private bool registerWithLwsInputService = true;
        [SerializeField] private bool forceInputOwnership = true;
        [SerializeField] private Lws18SpeedTransmissionController transmissionController;
        [SerializeField] private float steeringResponse = 1f;

        private readonly Dictionary<string, bool> _previous = new Dictionary<string, bool>();
        private ILwsVehicleInputService _inputService;
        private LwsVehicleContinuousInput _continuous;
        private LwsVehicleCommandFrame _commands;

        public string SourceId => "dead-air.input.basic-automatic";
        public bool HornHeld => LwsVehicleCommandFrameUtility.IsActive(_commands.horn) || LwsVehicleCommandFrameUtility.IsActive(_commands.airHorn);
        public LwsVehicleContinuousInput LastContinuousInput => _continuous;
        public LwsVehicleCommandFrame LastCommandFrame => _commands;

        public void SetTransmissionController(Lws18SpeedTransmissionController controller)
        {
            transmissionController = controller;
        }

        public void Activate()
        {
            ResolveServices();
            if (_inputService != null)
            {
                _inputService.SetInputSource(this, LwsVehicleInputOwner.KeyboardMouse, forceInputOwnership);
            }
        }

        private void Start()
        {
            ResolveServices();
            if (registerWithLwsInputService)
            {
                Activate();
            }
        }

        private void Update()
        {
            ResolveServices();
            _continuous = ReadContinuousNow();
            _commands = ReadCommandsNow();
        }

        public LwsVehicleContinuousInput ReadContinuousInput()
        {
            return _continuous;
        }

        public LwsVehicleCommandFrame ReadCommandFrame()
        {
            return _commands;
        }

        public LwsTruckGearIntent ReadGearIntent()
        {
            return new LwsTruckGearIntent
            {
                physicalGate = LwsTruckShifterGate.Neutral,
                neutralRequested = true
            };
        }

        public void SetSimulatedFrameForTests(LwsVehicleContinuousInput continuous, LwsVehicleCommandFrame commands)
        {
            _continuous = continuous;
            _commands = commands;
            enabled = false;
        }

        private LwsVehicleContinuousInput ReadContinuousNow()
        {
            var continuous = new LwsVehicleContinuousInput();
            bool forwardHeld = false;
            bool reverseHeld = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) continuous.steering -= steeringResponse;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) continuous.steering += steeringResponse;
                forwardHeld = Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed;
                reverseHeld = Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed;
            }

            if (Gamepad.current != null)
            {
                continuous.steering = Mathf.Abs(Gamepad.current.leftStick.x.ReadValue()) > Mathf.Abs(continuous.steering)
                    ? Gamepad.current.leftStick.x.ReadValue()
                    : continuous.steering;
                continuous.throttle = Mathf.Max(continuous.throttle, Gamepad.current.rightTrigger.ReadValue());
                continuous.brake = Mathf.Max(continuous.brake, Gamepad.current.leftTrigger.ReadValue());
            }
#else
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) continuous.steering -= steeringResponse;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) continuous.steering += steeringResponse;
            forwardHeld = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
            reverseHeld = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
#endif

            ApplyBasicAutomaticDirection(ref continuous, forwardHeld, reverseHeld);
            continuous.steering = Mathf.Clamp(continuous.steering, -1f, 1f);
            return continuous;
        }

        private void ApplyBasicAutomaticDirection(ref LwsVehicleContinuousInput continuous, bool forwardHeld, bool reverseHeld)
        {
            if (transmissionController == null)
            {
                if (forwardHeld) continuous.throttle = 1f;
                if (reverseHeld) continuous.brake = 1f;
                return;
            }

            LwsTransmissionDisplayState state = transmissionController.DisplayState;
            LwsAutomaticKeyboardDirectionDecision decision = LwsKeyboardGamepadTruckInputSource.ResolveAutomaticKeyboardDirection(
                forwardHeld,
                reverseHeld,
                LwsTransmissionMode.Automatic,
                state.automaticSelector,
                state.signedSpeedMetersPerSecond,
                transmissionController.AutomaticDirectionChangeSpeedThresholdMetersPerSecond);

            if (!decision.handled)
            {
                return;
            }

            if (decision.requestSelectorChange &&
                !transmissionController.TrySetAutomaticSelector(decision.requestedSelector, out string message))
            {
                Debug.LogWarning(message, this);
                continuous.throttle = 0f;
                continuous.brake = 1f;
                return;
            }

            continuous.throttle = Mathf.Max(continuous.throttle, decision.throttle);
            continuous.brake = Mathf.Max(continuous.brake, decision.brake);
        }

        private LwsVehicleCommandFrame ReadCommandsNow()
        {
            var commands = new LwsVehicleCommandFrame();
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                commands.horn = Edge("kb.horn", keyboard.hKey.isPressed);
                commands.airHorn = commands.horn;
                commands.pause = Edge("kb.pause", keyboard.escapeKey.isPressed);
                commands.menuCancel = commands.pause;
                commands.interact = Edge("kb.interact", keyboard.enterKey.isPressed);
                commands.cameraCycle = Edge("kb.camera", keyboard.tabKey.isPressed);
                commands.flipOffDriver = Edge("kb.flipOff", keyboard.fKey.isPressed);
            }

            Gamepad gamepad = Gamepad.current;
            if (gamepad != null)
            {
                LwsVehicleCommandFrame gamepadCommands = new LwsVehicleCommandFrame
                {
                    horn = Edge("gp.horn", gamepad.buttonSouth.isPressed),
                    airHorn = Edge("gp.airHorn", gamepad.buttonSouth.isPressed),
                    pause = Edge("gp.pause", gamepad.startButton.isPressed),
                    menuCancel = Edge("gp.cancel", gamepad.buttonEast.isPressed),
                    interact = Edge("gp.interact", gamepad.buttonSouth.isPressed),
                    cameraCycle = Edge("gp.camera", gamepad.selectButton.isPressed),
                    flipOffDriver = Edge("gp.flipOff", gamepad.leftStickButton.isPressed && gamepad.rightStickButton.isPressed)
                };
                commands = LwsVehicleCommandFrameUtility.Combine(commands, gamepadCommands);
            }
#else
            commands.horn = Edge("kb.horn", Input.GetKey(KeyCode.H));
            commands.airHorn = commands.horn;
            commands.pause = Edge("kb.pause", Input.GetKey(KeyCode.Escape));
            commands.menuCancel = commands.pause;
            commands.interact = Edge("kb.interact", Input.GetKey(KeyCode.Return));
            commands.cameraCycle = Edge("kb.camera", Input.GetKey(KeyCode.Tab));
            commands.flipOffDriver = Edge("kb.flipOff", Input.GetKey(KeyCode.F));
#endif
            return commands;
        }

        private LwsMomentaryIntent Edge(string key, bool current)
        {
            _previous.TryGetValue(key, out bool previous);
            _previous[key] = current;
            if (current && !previous) return LwsMomentaryIntent.Pressed;
            if (!current && previous) return LwsMomentaryIntent.Released;
            return current ? LwsMomentaryIntent.Held : LwsMomentaryIntent.None;
        }

        private void ResolveServices()
        {
            if (transmissionController == null)
            {
                transmissionController = GetComponent<Lws18SpeedTransmissionController>();
            }

            if (_inputService != null ||
                LwsApplicationBootstrap.Instance == null ||
                LwsApplicationBootstrap.Instance.Registry == null)
            {
                return;
            }

            LwsApplicationBootstrap.Instance.Registry.TryGet(out _inputService);
        }
    }
}
