using System;
using UnityEngine;

namespace LWS.InterstateHauler
{
    [DefaultExecutionOrder(135)]
    [DisallowMultipleComponent]
    public sealed class Lws18SpeedTransmissionController : MonoBehaviour, ILwsTruckTransmission, ILwsSaveParticipant
    {
        public const LwsTransmissionMode DevelopmentDefaultMode = LwsTransmissionMode.Automatic;

        [SerializeField] private Lws18SpeedTransmissionDefinition definition;
        [SerializeField] private LwsNwh18SpeedTransmissionAdapter nwhAdapter;
        [SerializeField] private MonoBehaviour fallbackInputSourceBehaviour;
        [SerializeField] private LwsTransmissionMode mode = DevelopmentDefaultMode;
        [SerializeField] private LwsManualShiftAssistMode assistMode = LwsManualShiftAssistMode.AssistedManual;
        [SerializeField] private bool configureNwhOnStart = true;
        [SerializeField] private bool registerSaveParticipant = true;
        [SerializeField] private bool logRejectedShifts;
        [SerializeField] private int automaticStartingForwardGear = 3;
        [SerializeField] private int automaticMaximumForwardGear = 18;
        [SerializeField] private float automaticUpshiftRpm = 1850f;
        [SerializeField] private float automaticDownshiftRpm = 1050f;
        [SerializeField] private float automaticShiftCooldownSeconds = 0.65f;
        [SerializeField] private float automaticStoppedNeutralSpeedMetersPerSecond = 0.35f;
        [SerializeField, Range(0f, 1f)] private float automaticThrottleThreshold = 0.05f;
        [SerializeField, Range(0f, 1f)] private float automaticBrakeThreshold = 0.1f;

        private ILwsVehicleInputService _inputService;
        private ILwsVehicleInputSource _fallbackInputSource;
        private ILwsSaveService _saveService;
        private LwsTransmissionState _state;
        private LwsTransmissionDisplayState _displayState;
        private LwsTransmissionAbuseEvent _lastAbuseEvent;
        private bool _nwhConfigured;
        private bool _nwhConfigureWarningLogged;
        private bool _saveRegistered;
        private int _lastCommandedNwhGear = int.MinValue;
        private float _lastClutchInput;
        private LwsTruckShifterGate _lastPhysicalGate = LwsTruckShifterGate.Neutral;
        private Lws18SpeedTransmissionDefinition _transientDefinition;
        private int _automaticTargetNwhGear;
        private string _automaticTargetLabel = "N";
        private float _nextAutomaticShiftTime;
        private string _lastModeSwitchMessage = string.Empty;

        public event Action<LwsTransmissionAbuseEvent> AbuseDetected;

        public LwsTransmissionState CurrentState => CaptureState();
        public LwsTransmissionDisplayState DisplayState => _displayState;
        public LwsTransmissionAbuseEvent LastAbuseEvent => _lastAbuseEvent;
        public Lws18SpeedTransmissionDefinition Definition => ActiveDefinition;
        public bool DevelopmentAutomaticModeActive => mode == LwsTransmissionMode.Automatic;
        public int AutomaticTargetNwhGear => _automaticTargetNwhGear;
        public string AutomaticTargetLabel => string.IsNullOrWhiteSpace(_automaticTargetLabel) ? "N" : _automaticTargetLabel;
        public string LastModeSwitchMessage => _lastModeSwitchMessage;
        public string ParticipantId => "vehicle.transmission.player";
        public int PayloadVersion => 1;

        private Lws18SpeedTransmissionDefinition ActiveDefinition
        {
            get
            {
                if (definition != null)
                {
                    return definition;
                }

                if (_transientDefinition == null)
                {
                    _transientDefinition = Lws18SpeedTransmissionDefinition.CreateTransientG29DevelopmentPreset();
                }

                return _transientDefinition;
            }
        }

        private void Reset()
        {
            ResolveLocalReferences();
        }

        private void Awake()
        {
            ResolveLocalReferences();
            _state = new LwsTransmissionState
            {
                mode = mode,
                logicalGear = Lws18SpeedGearId.Neutral,
                displayLabel = "N",
                physicalGate = LwsTruckShifterGate.Neutral,
                requestedRange = LwsTruckRange.Low,
                engagedRange = LwsTruckRange.Low,
                requestedSplitter = LwsTruckSplitter.Low,
                engagedSplitter = LwsTruckSplitter.Low,
                neutral = true,
                nwhGear = 0,
                shiftState = LwsTransmissionShiftState.Idle
            };
            _displayState = BuildDisplayState(ReadNwhState());
        }

        private void Start()
        {
            ResolveServices();
            if (definition != null)
            {
                assistMode = definition.DefaultAssistMode;
            }

            if (configureNwhOnStart)
            {
                ConfigureNwhIfNeeded();
            }

            RegisterSaveParticipantIfNeeded();
        }

        private void OnDestroy()
        {
            if (_saveRegistered && _saveService != null)
            {
                _saveService.UnregisterParticipant(ParticipantId);
            }
        }

        private void Update()
        {
            ResolveServices();
            ConfigureNwhIfNeeded();

            ILwsVehicleInputSource source = ResolveInputSource();
            if (mode == LwsTransmissionMode.Automatic)
            {
                LwsVehicleContinuousInput automaticInput = source != null ? source.ReadContinuousInput() : default;
                ProcessAutomaticTransmission(ActiveDefinition, automaticInput);
                return;
            }

            if (mode != LwsTransmissionMode.Truck18Speed)
            {
                _state.shiftState = LwsTransmissionShiftState.Idle;
                _state.lastRejectionReason = LwsShiftRejectionReason.TransmissionModeConflict;
                _displayState = BuildDisplayState(ReadNwhState());
                return;
            }

            if (source == null)
            {
                PreserveCurrentGearForMissingInput();
                return;
            }

            LwsVehicleContinuousInput continuousInput = source.ReadContinuousInput();
            _lastClutchInput = Mathf.Clamp01(continuousInput.clutch);
            ProcessGearIntent(source.ReadGearIntent(), _lastClutchInput);
        }

        public void SetDefinition(Lws18SpeedTransmissionDefinition transmissionDefinition)
        {
            definition = transmissionDefinition;
            _nwhConfigured = false;
            _nwhConfigureWarningLogged = false;
            if (definition != null)
            {
                assistMode = definition.DefaultAssistMode;
            }
        }

        public void SetMode(LwsTransmissionMode transmissionMode)
        {
            mode = transmissionMode;
            _state.mode = transmissionMode;
        }

        public bool TrySetDevelopmentAutomaticTestMode(bool enabled, out string message)
        {
            ResolveLocalReferences();
            ConfigureNwhIfNeeded();

            LwsNwhTransmissionRuntimeState nwhState = ReadNwhState();
            if (nwhState.available && Mathf.Abs(nwhState.signedSpeedMetersPerSecond) > 1.5f)
            {
                message = "Stop the truck before switching transmission test modes.";
                _lastModeSwitchMessage = message;
                return false;
            }

            SetNeutralTransmissionState();
            EngageNeutral(ActiveDefinition);

            if (enabled)
            {
                mode = LwsTransmissionMode.Automatic;
                _state.mode = LwsTransmissionMode.Automatic;
                _state.requiresShifterSynchronization = false;
                _automaticTargetNwhGear = 0;
                _automaticTargetLabel = "N";
                _nextAutomaticShiftTime = Time.time + 0.25f;
                message = "Development automatic transmission test mode enabled.";
            }
            else
            {
                mode = LwsTransmissionMode.Truck18Speed;
                _state.mode = LwsTransmissionMode.Truck18Speed;
                _state.requiresShifterSynchronization = true;
                _automaticTargetNwhGear = 0;
                _automaticTargetLabel = "N";
                message = "Returned to 18-speed manual mode; shifter synchronization is required.";
            }

            _lastModeSwitchMessage = message;
            _displayState = BuildDisplayState(ReadNwhState());
            return true;
        }

        public void SetAssistMode(LwsManualShiftAssistMode manualAssistMode)
        {
            assistMode = manualAssistMode;
        }

        public void ApplyGearIntent(LwsTruckGearIntent gearIntent)
        {
            ProcessGearIntent(gearIntent, _lastClutchInput);
        }

        public LwsTransmissionState CaptureState()
        {
            LwsNwhTransmissionRuntimeState nwhState = ReadNwhState();
            _state.nwhGear = nwhState.nwhGear;
            _state.engineStalled = nwhState.engineStalled;
            _state.clutchInput = _lastClutchInput;
            return _state;
        }

        public void RestoreState(LwsTransmissionState state)
        {
            _state = state;
            _state.requiresShifterSynchronization = true;
            _lastCommandedNwhGear = state.nwhGear;
            if (nwhAdapter != null && state.nwhGear > -100)
            {
                nwhAdapter.TryShiftInto(state.nwhGear, true, out _);
            }

            _displayState = BuildDisplayState(ReadNwhState());
        }

        LwsSaveParticipantState ILwsSaveParticipant.CaptureState()
        {
            LwsTransmissionState current = CaptureState();
            var payload = new Lws18SpeedTransmissionSavePayload
            {
                schemaVersion = 1,
                mode = current.mode,
                logicalGear = current.logicalGear,
                logicalRatioIndex = current.logicalRatioIndex,
                nwhGear = current.nwhGear,
                physicalGate = current.physicalGate,
                requestedRange = current.requestedRange,
                engagedRange = current.engagedRange,
                requestedSplitter = current.requestedSplitter,
                engagedSplitter = current.engagedSplitter,
                shiftState = current.shiftState,
                requiresShifterSynchronization = current.requiresShifterSynchronization
            };

            return new LwsSaveParticipantState
            {
                participantId = ParticipantId,
                payloadVersion = PayloadVersion,
                payloadJson = JsonUtility.ToJson(payload)
            };
        }

        public LwsSaveOperationResult RestoreState(LwsSaveParticipantState state)
        {
            if (state == null || state.participantId != ParticipantId)
            {
                return LwsSaveOperationResult.Failure($"Payload does not belong to {ParticipantId}.");
            }

            Lws18SpeedTransmissionSavePayload payload = JsonUtility.FromJson<Lws18SpeedTransmissionSavePayload>(state.payloadJson);
            if (payload == null)
            {
                return LwsSaveOperationResult.Failure("18-speed transmission payload is empty.");
            }

            RestoreState(new LwsTransmissionState
            {
                mode = payload.mode,
                logicalGear = payload.logicalGear,
                logicalRatioIndex = payload.logicalRatioIndex,
                nwhGear = payload.nwhGear,
                physicalGate = payload.physicalGate,
                requestedRange = payload.requestedRange,
                engagedRange = payload.engagedRange,
                requestedSplitter = payload.requestedSplitter,
                engagedSplitter = payload.engagedSplitter,
                neutral = payload.logicalGear == Lws18SpeedGearId.Neutral,
                reverse = payload.logicalGear == Lws18SpeedGearId.Reverse1,
                shiftState = LwsTransmissionShiftState.ShifterMismatch,
                requiresShifterSynchronization = true
            });
            return LwsSaveOperationResult.Success("18-speed transmission state restored and awaiting shifter synchronization.");
        }

        public LwsSaveOperationResult ClearState()
        {
            RestoreState(new LwsTransmissionState
            {
                mode = LwsTransmissionMode.Truck18Speed,
                logicalGear = Lws18SpeedGearId.Neutral,
                displayLabel = "N",
                physicalGate = LwsTruckShifterGate.Neutral,
                requestedRange = LwsTruckRange.Low,
                engagedRange = LwsTruckRange.Low,
                requestedSplitter = LwsTruckSplitter.Low,
                engagedSplitter = LwsTruckSplitter.Low,
                neutral = true,
                nwhGear = 0
            });
            _state.requiresShifterSynchronization = false;
            return LwsSaveOperationResult.Success();
        }

        public LwsSaveOperationResult ValidateParticipant()
        {
            if (ActiveDefinition == null)
            {
                return LwsSaveOperationResult.Failure("18-speed transmission definition is not available.");
            }

            return ActiveDefinition.ValidateDefinition(out string message)
                ? LwsSaveOperationResult.Success(message)
                : LwsSaveOperationResult.Failure(message);
        }

        private void ProcessGearIntent(LwsTruckGearIntent gearIntent, float clutchInput)
        {
            Lws18SpeedTransmissionDefinition activeDefinition = ActiveDefinition;
            _state.mode = mode;
            _state.physicalGate = gearIntent.physicalGate == LwsTruckShifterGate.None
                ? LwsTruckShifterGate.Neutral
                : gearIntent.physicalGate;
            _state.requestedRange = gearIntent.range;
            _state.requestedSplitter = gearIntent.splitter;
            _state.clutchInput = clutchInput;

            if (_state.requiresShifterSynchronization && !CurrentInputMatchesRestoredState(gearIntent))
            {
                Reject(LwsShiftRejectionReason.ShifterPositionMismatch, LwsTransmissionShiftState.ShifterMismatch);
                _displayState = BuildDisplayState(ReadNwhState());
                return;
            }

            _state.requiresShifterSynchronization = false;

            if (gearIntent.neutralRequested || _state.physicalGate == LwsTruckShifterGate.Neutral)
            {
                EngageNeutral(activeDefinition);
                return;
            }

            if (gearIntent.reverseRequested || _state.physicalGate == activeDefinition.ReverseGate)
            {
                TryEngageReverse(activeDefinition, clutchInput);
                return;
            }

            if (!activeDefinition.IsKnownForwardGate(_state.physicalGate))
            {
                Reject(LwsShiftRejectionReason.GearUnavailable, LwsTransmissionShiftState.Rejected);
                return;
            }

            if (_state.requestedRange != _state.engagedRange)
            {
                _state.shiftState = LwsTransmissionShiftState.WaitingForNeutral;
                _state.lastRejectionReason = LwsShiftRejectionReason.None;
            }

            if (_state.requestedSplitter != _state.engagedSplitter)
            {
                if (CanEngageSplitter(clutchInput, activeDefinition, out bool waitingForSynchronization))
                {
                    _state.engagedSplitter = _state.requestedSplitter;
                }
                else
                {
                    _state.shiftState = waitingForSynchronization
                        ? LwsTransmissionShiftState.WaitingForSynchronization
                        : LwsTransmissionShiftState.WaitingForClutch;
                    _displayState = BuildDisplayState(ReadNwhState());
                    return;
                }
            }

            if (!activeDefinition.TryResolveForward(_state.physicalGate, _state.engagedRange, _state.engagedSplitter, out Lws18SpeedResolvedGear target))
            {
                Reject(target.invalidReason, LwsTransmissionShiftState.Rejected);
                return;
            }

            TryEngageForwardGear(activeDefinition, target, clutchInput);
            _lastPhysicalGate = _state.physicalGate;
        }

        private void ProcessAutomaticTransmission(Lws18SpeedTransmissionDefinition activeDefinition, LwsVehicleContinuousInput continuousInput)
        {
            LwsNwhTransmissionRuntimeState nwhState = ReadNwhState();
            _state.mode = LwsTransmissionMode.Automatic;
            _state.physicalGate = LwsTruckShifterGate.Neutral;
            _state.requestedRange = LwsTruckRange.Low;
            _state.engagedRange = LwsTruckRange.Low;
            _state.requestedSplitter = LwsTruckSplitter.Low;
            _state.engagedSplitter = LwsTruckSplitter.Low;
            _lastClutchInput = 1f;
            _state.clutchInput = _lastClutchInput;

            int targetNwhGear = ChooseAutomaticTargetGear(activeDefinition, nwhState, continuousInput);
            _automaticTargetNwhGear = targetNwhGear;
            _automaticTargetLabel = GetAutomaticTargetLabel(activeDefinition, targetNwhGear);

            if (targetNwhGear == 0)
            {
                EngageNeutral(activeDefinition);
                return;
            }

            int currentNwhGear = nwhState.available ? nwhState.nwhGear : _state.nwhGear;
            if (targetNwhGear == currentNwhGear && targetNwhGear == _lastCommandedNwhGear)
            {
                _state.shiftState = LwsTransmissionShiftState.Engaged;
                _state.lastRejectionReason = LwsShiftRejectionReason.None;
                _displayState = BuildDisplayState(nwhState);
                return;
            }

            if (Time.time < _nextAutomaticShiftTime)
            {
                _state.shiftState = LwsTransmissionShiftState.Preselected;
                _state.lastRejectionReason = LwsShiftRejectionReason.None;
                _displayState = BuildDisplayState(nwhState);
                return;
            }

            if (!activeDefinition.TryGetMappingForNwhGear(targetNwhGear, out Lws18SpeedRatioMapping mapping))
            {
                Reject(LwsShiftRejectionReason.GearUnavailable, LwsTransmissionShiftState.Rejected);
                return;
            }

            Lws18SpeedResolvedGear target = ResolveMapping(mapping);
            float predictedRpm = PredictTargetRpm(nwhState, target);
            float rpmError = Mathf.Abs(predictedRpm - nwhState.engineRpm);
            ApplyAcceptedGear(target, predictedRpm, rpmError);
            _nextAutomaticShiftTime = Time.time + Mathf.Max(0.1f, automaticShiftCooldownSeconds);
        }

        private int ChooseAutomaticTargetGear(
            Lws18SpeedTransmissionDefinition activeDefinition,
            LwsNwhTransmissionRuntimeState nwhState,
            LwsVehicleContinuousInput continuousInput)
        {
            int minimumForwardGear = Mathf.Clamp(automaticStartingForwardGear, 1, 18);
            int maximumForwardGear = Mathf.Clamp(Mathf.Max(automaticMaximumForwardGear, minimumForwardGear), minimumForwardGear, 18);
            int currentNwhGear = nwhState.available ? nwhState.nwhGear : _state.nwhGear;
            float speed = nwhState.available ? Mathf.Abs(nwhState.signedSpeedMetersPerSecond) : 0f;
            bool throttleRequested = continuousInput.throttle > automaticThrottleThreshold;
            bool brakeRequested = continuousInput.brake > automaticBrakeThreshold;

            if (!throttleRequested && speed <= automaticStoppedNeutralSpeedMetersPerSecond)
            {
                return 0;
            }

            if (brakeRequested && !throttleRequested && speed <= automaticStoppedNeutralSpeedMetersPerSecond)
            {
                return 0;
            }

            if (currentNwhGear <= 0)
            {
                return throttleRequested ? minimumForwardGear : 0;
            }

            if (speed <= automaticStoppedNeutralSpeedMetersPerSecond)
            {
                return throttleRequested ? minimumForwardGear : 0;
            }

            if (!nwhState.available || Time.time < _nextAutomaticShiftTime)
            {
                return Mathf.Clamp(currentNwhGear, minimumForwardGear, maximumForwardGear);
            }

            if (nwhState.engineRpm > automaticUpshiftRpm && currentNwhGear < maximumForwardGear)
            {
                return currentNwhGear + 1;
            }

            if (nwhState.engineRpm > 0f && nwhState.engineRpm < automaticDownshiftRpm && currentNwhGear > minimumForwardGear)
            {
                return currentNwhGear - 1;
            }

            return Mathf.Clamp(currentNwhGear, minimumForwardGear, maximumForwardGear);
        }

        private string GetAutomaticTargetLabel(Lws18SpeedTransmissionDefinition activeDefinition, int targetNwhGear)
        {
            if (targetNwhGear == 0)
            {
                return "N";
            }

            return activeDefinition.TryGetMappingForNwhGear(targetNwhGear, out Lws18SpeedRatioMapping mapping)
                ? mapping.displayLabel
                : targetNwhGear.ToString();
        }

        private static Lws18SpeedResolvedGear ResolveMapping(Lws18SpeedRatioMapping mapping)
        {
            return new Lws18SpeedResolvedGear
            {
                valid = mapping.valid,
                gearId = mapping.gearId,
                physicalGate = mapping.physicalGate,
                range = mapping.range,
                splitter = mapping.splitter,
                logicalRatioIndex = mapping.logicalRatioIndex,
                nwhGearIndex = mapping.nwhGearIndex,
                gearRatio = mapping.gearRatio,
                displayLabel = mapping.displayLabel
            };
        }

        private void SetNeutralTransmissionState()
        {
            _state.physicalGate = LwsTruckShifterGate.Neutral;
            _state.requestedRange = LwsTruckRange.Low;
            _state.engagedRange = LwsTruckRange.Low;
            _state.requestedSplitter = LwsTruckSplitter.Low;
            _state.engagedSplitter = LwsTruckSplitter.Low;
            _state.logicalGear = Lws18SpeedGearId.Neutral;
            _state.logicalRatioIndex = 0;
            _state.displayLabel = "N";
            _state.neutral = true;
            _state.reverse = false;
            _state.nwhGear = 0;
            _state.gearRatio = 0f;
            _state.predictedRpm = 0f;
            _state.rpmError = 0f;
            _state.lastRejectionReason = LwsShiftRejectionReason.None;
            _state.shiftState = LwsTransmissionShiftState.Idle;
        }

        private void EngageNeutral(Lws18SpeedTransmissionDefinition activeDefinition)
        {
            _state.engagedRange = _state.requestedRange;
            _state.engagedSplitter = _state.requestedSplitter;
            _state.logicalGear = Lws18SpeedGearId.Neutral;
            _state.logicalRatioIndex = 0;
            _state.displayLabel = "N";
            _state.neutral = true;
            _state.reverse = false;
            _state.gearRatio = 0f;
            _state.predictedRpm = 0f;
            _state.rpmError = 0f;
            _state.lastRejectionReason = LwsShiftRejectionReason.None;
            _state.shiftState = LwsTransmissionShiftState.Engaged;

            if (nwhAdapter != null)
            {
                nwhAdapter.TryShiftInto(0, true, out _);
            }

            _lastCommandedNwhGear = 0;
            _displayState = BuildDisplayState(ReadNwhState());
        }

        private void TryEngageReverse(Lws18SpeedTransmissionDefinition activeDefinition, float clutchInput)
        {
            LwsNwhTransmissionRuntimeState nwhState = ReadNwhState();
            Lws18SpeedResolvedGear reverse = activeDefinition.ResolveReverse();
            if (nwhState.signedSpeedMetersPerSecond > activeDefinition.ReverseSpeedLimitMetersPerSecond)
            {
                EmitAbuse(new LwsTransmissionAbuseEvent
                {
                    severity = LwsTransmissionAbuseSeverity.Severe,
                    cause = LwsTransmissionAbuseCause.ReverseWhileMovingForward,
                    attemptedGear = Lws18SpeedGearId.Reverse1,
                    attemptedNwhGear = activeDefinition.ReverseNwhGearIndex,
                    currentRpm = nwhState.engineRpm,
                    speedMetersPerSecond = nwhState.signedSpeedMetersPerSecond,
                    message = "Reverse requested while moving forward."
                });
                Reject(LwsShiftRejectionReason.InvalidReverseRequest, LwsTransmissionShiftState.Rejected);
                return;
            }

            if (!CanEngageShift(clutchInput, reverse, nwhState, activeDefinition, out _, out _))
            {
                Reject(LwsShiftRejectionReason.ClutchNotDepressed, LwsTransmissionShiftState.WaitingForClutch);
                return;
            }

            ApplyAcceptedGear(reverse, 0f, 0f);
        }

        private void TryEngageForwardGear(
            Lws18SpeedTransmissionDefinition activeDefinition,
            Lws18SpeedResolvedGear target,
            float clutchInput)
        {
            LwsNwhTransmissionRuntimeState nwhState = ReadNwhState();
            float predictedRpm = PredictTargetRpm(nwhState, target);
            float rpmError = Mathf.Abs(predictedRpm - nwhState.engineRpm);
            LwsTransmissionAbuseEvent abuse = EvaluateAbuse(activeDefinition, target, nwhState, predictedRpm, rpmError, clutchInput);
            if (abuse.HasAbuse)
            {
                EmitAbuse(abuse);
            }

            if (abuse.severity == LwsTransmissionAbuseSeverity.CatastrophicRisk ||
                (abuse.severity == LwsTransmissionAbuseSeverity.Severe && assistMode == LwsManualShiftAssistMode.AssistedManual))
            {
                Reject(LwsShiftRejectionReason.PredictedOverspeed, LwsTransmissionShiftState.Rejected);
                _state.predictedRpm = predictedRpm;
                _state.rpmError = rpmError;
                return;
            }

            if (!CanEngageShift(clutchInput, target, nwhState, activeDefinition, out LwsShiftRejectionReason reason, out LwsTransmissionShiftState waitState))
            {
                Reject(reason, waitState);
                _state.predictedRpm = predictedRpm;
                _state.rpmError = rpmError;
                return;
            }

            ApplyAcceptedGear(target, predictedRpm, rpmError);
        }

        private bool CanEngageSplitter(float clutchInput, Lws18SpeedTransmissionDefinition activeDefinition, out bool waitingForSynchronization)
        {
            waitingForSynchronization = false;
            if (assistMode == LwsManualShiftAssistMode.AssistedManual)
            {
                return true;
            }

            if (Lws18SpeedShiftEvaluation.IsClutchDepressed(clutchInput, activeDefinition.ClutchDepressedThreshold))
            {
                return true;
            }

            waitingForSynchronization = true;
            return false;
        }

        private bool CanEngageShift(
            float clutchInput,
            Lws18SpeedResolvedGear target,
            LwsNwhTransmissionRuntimeState nwhState,
            Lws18SpeedTransmissionDefinition activeDefinition,
            out LwsShiftRejectionReason reason,
            out LwsTransmissionShiftState waitState)
        {
            reason = LwsShiftRejectionReason.None;
            waitState = LwsTransmissionShiftState.Engaging;

            if (assistMode == LwsManualShiftAssistMode.AssistedManual)
            {
                return true;
            }

            if (Lws18SpeedShiftEvaluation.IsClutchDepressed(clutchInput, activeDefinition.ClutchDepressedThreshold))
            {
                return true;
            }

            float predictedRpm = PredictTargetRpm(nwhState, target);
            float rpmError = Mathf.Abs(predictedRpm - nwhState.engineRpm);
            if (nwhState.nwhGear != 0 &&
                Lws18SpeedShiftEvaluation.IsRpmSynchronized(nwhState.engineRpm, predictedRpm, activeDefinition.FloatShiftRpmTolerance))
            {
                return true;
            }

            reason = nwhState.nwhGear == 0 ? LwsShiftRejectionReason.ClutchNotDepressed : LwsShiftRejectionReason.RpmMismatch;
            waitState = nwhState.nwhGear == 0 ? LwsTransmissionShiftState.WaitingForClutch : LwsTransmissionShiftState.Grinding;
            if (nwhState.nwhGear != 0)
            {
                EmitAbuse(new LwsTransmissionAbuseEvent
                {
                    severity = rpmError > activeDefinition.AssistedRpmTolerance ? LwsTransmissionAbuseSeverity.Moderate : LwsTransmissionAbuseSeverity.Minor,
                    cause = LwsTransmissionAbuseCause.ClutchlessPoorSynchronization,
                    attemptedGear = target.gearId,
                    attemptedNwhGear = target.nwhGearIndex,
                    currentRpm = nwhState.engineRpm,
                    predictedRpm = predictedRpm,
                    rpmError = rpmError,
                    speedMetersPerSecond = nwhState.signedSpeedMetersPerSecond,
                    message = "Hardcore clutchless shift attempted outside RPM synchronization tolerance."
                });
            }

            return false;
        }

        private void ApplyAcceptedGear(Lws18SpeedResolvedGear target, float predictedRpm, float rpmError)
        {
            LwsNwhTransmissionRuntimeState nwhState = ReadNwhState();
            if (target.nwhGearIndex == _lastCommandedNwhGear && nwhState.nwhGear == target.nwhGearIndex)
            {
                _state.lastRejectionReason = LwsShiftRejectionReason.DuplicateRequestSuppressed;
                _state.shiftState = LwsTransmissionShiftState.Engaged;
                _displayState = BuildDisplayState(nwhState);
                return;
            }

            string message = "NWH transmission adapter is not available.";
            if (nwhAdapter != null && nwhAdapter.TryShiftInto(target.nwhGearIndex, true, out message))
            {
                _lastCommandedNwhGear = target.nwhGearIndex;
                _state.logicalGear = target.gearId;
                _state.logicalRatioIndex = target.logicalRatioIndex;
                _state.displayLabel = target.displayLabel;
                _state.neutral = target.neutral;
                _state.reverse = target.reverse;
                _state.nwhGear = target.nwhGearIndex;
                _state.gearRatio = target.gearRatio;
                _state.predictedRpm = predictedRpm;
                _state.rpmError = rpmError;
                _state.lastRejectionReason = LwsShiftRejectionReason.None;
                _state.shiftState = LwsTransmissionShiftState.Engaged;
            }
            else
            {
                Reject(LwsShiftRejectionReason.GearUnavailable, LwsTransmissionShiftState.Rejected);
                if (logRejectedShifts)
                {
                    Debug.LogWarning(message, this);
                }
            }

            _displayState = BuildDisplayState(ReadNwhState());
        }

        private LwsTransmissionAbuseEvent EvaluateAbuse(
            Lws18SpeedTransmissionDefinition activeDefinition,
            Lws18SpeedResolvedGear target,
            LwsNwhTransmissionRuntimeState nwhState,
            float predictedRpm,
            float rpmError,
            float clutchInput)
        {
            if (nwhState.revLimiterRpm <= 0f || predictedRpm <= 0f)
            {
                return default;
            }

            LwsTransmissionAbuseSeverity overspeed = Lws18SpeedShiftEvaluation.ClassifyOverspeed(
                predictedRpm,
                nwhState.revLimiterRpm,
                activeDefinition.SevereOverspeedMultiplier,
                activeDefinition.CatastrophicOverspeedMultiplier);
            if (overspeed == LwsTransmissionAbuseSeverity.CatastrophicRisk)
            {
                return Abuse(target, nwhState, predictedRpm, rpmError, LwsTransmissionAbuseSeverity.CatastrophicRisk, LwsTransmissionAbuseCause.ExtremeDownshift);
            }

            if (overspeed == LwsTransmissionAbuseSeverity.Severe)
            {
                return Abuse(target, nwhState, predictedRpm, rpmError, LwsTransmissionAbuseSeverity.Severe, LwsTransmissionAbuseCause.HighSpeedLowGearSelection);
            }

            if (!Lws18SpeedShiftEvaluation.IsClutchDepressed(clutchInput, activeDefinition.ClutchDepressedThreshold) &&
                rpmError > activeDefinition.FloatShiftRpmTolerance &&
                assistMode == LwsManualShiftAssistMode.HardcoreManual)
            {
                return Abuse(target, nwhState, predictedRpm, rpmError, LwsTransmissionAbuseSeverity.Minor, LwsTransmissionAbuseCause.ExcessiveRpmMismatch);
            }

            return default;
        }

        private static LwsTransmissionAbuseEvent Abuse(
            Lws18SpeedResolvedGear target,
            LwsNwhTransmissionRuntimeState nwhState,
            float predictedRpm,
            float rpmError,
            LwsTransmissionAbuseSeverity severity,
            LwsTransmissionAbuseCause cause)
        {
            return new LwsTransmissionAbuseEvent
            {
                severity = severity,
                cause = cause,
                attemptedGear = target.gearId,
                attemptedNwhGear = target.nwhGearIndex,
                currentRpm = nwhState.engineRpm,
                predictedRpm = predictedRpm,
                rpmError = rpmError,
                speedMetersPerSecond = nwhState.signedSpeedMetersPerSecond,
                message = $"{cause} detected while attempting {target.displayLabel}."
            };
        }

        private float PredictTargetRpm(LwsNwhTransmissionRuntimeState nwhState, Lws18SpeedResolvedGear target)
        {
            if (target.neutral || target.reverse || target.nwhGearIndex == 0)
            {
                return 0f;
            }

            float targetRatio = nwhAdapter != null
                ? Mathf.Abs(nwhAdapter.GetConfiguredTotalRatio(target.nwhGearIndex))
                : Mathf.Abs(target.gearRatio * ActiveDefinition.NwhFinalDriveRatio);
            float currentRatio = nwhAdapter != null
                ? Mathf.Abs(nwhAdapter.GetConfiguredTotalRatio(nwhState.nwhGear))
                : Mathf.Abs(nwhState.currentTotalGearRatio);

            return Lws18SpeedShiftEvaluation.PredictRpmFromRatioChange(
                nwhState.engineRpm,
                currentRatio,
                targetRatio,
                nwhState.nwhGear);
        }

        private void Reject(LwsShiftRejectionReason reason, LwsTransmissionShiftState shiftState)
        {
            _state.lastRejectionReason = reason;
            _state.shiftState = shiftState;
            if (logRejectedShifts && reason != LwsShiftRejectionReason.None)
            {
                Debug.LogWarning($"18-speed shift rejected: {reason}", this);
            }

            _displayState = BuildDisplayState(ReadNwhState());
        }

        private void EmitAbuse(LwsTransmissionAbuseEvent abuseEvent)
        {
            _lastAbuseEvent = abuseEvent;
            _state.lastAbuseSeverity = abuseEvent.severity;
            AbuseDetected?.Invoke(abuseEvent);
        }

        private void PreserveCurrentGearForMissingInput()
        {
            _state.lastRejectionReason = LwsShiftRejectionReason.InputUnavailable;
            _state.shiftState = LwsTransmissionShiftState.Idle;
            _state.predictedRpm = 0f;
            _state.rpmError = 0f;
            _displayState = BuildDisplayState(ReadNwhState());
        }

        private bool CurrentInputMatchesRestoredState(LwsTruckGearIntent gearIntent)
        {
            if (_state.neutral || _state.logicalGear == Lws18SpeedGearId.Neutral)
            {
                return gearIntent.neutralRequested || gearIntent.physicalGate == LwsTruckShifterGate.Neutral;
            }

            if (_state.reverse || _state.logicalGear == Lws18SpeedGearId.Reverse1)
            {
                return gearIntent.reverseRequested || gearIntent.physicalGate == ActiveDefinition.ReverseGate;
            }

            if (!ActiveDefinition.TryGetMapping(_state.logicalGear, out Lws18SpeedRatioMapping mapping))
            {
                return false;
            }

            return gearIntent.physicalGate == mapping.physicalGate &&
                   gearIntent.range == mapping.range &&
                   gearIntent.splitter == mapping.splitter;
        }

        private LwsTransmissionDisplayState BuildDisplayState(LwsNwhTransmissionRuntimeState nwhState)
        {
            return new LwsTransmissionDisplayState
            {
                mode = mode,
                physicalGate = _state.physicalGate,
                requestedRange = _state.requestedRange,
                engagedRange = _state.engagedRange,
                requestedSplitter = _state.requestedSplitter,
                engagedSplitter = _state.engagedSplitter,
                logicalGear = _state.logicalGear,
                displayLabel = string.IsNullOrWhiteSpace(_state.displayLabel) ? "N" : _state.displayLabel,
                logicalRatioIndex = _state.logicalRatioIndex,
                nwhGear = nwhState.available ? nwhState.nwhGear : _state.nwhGear,
                gearRatio = _state.gearRatio,
                clutch = _lastClutchInput,
                engineRpm = nwhState.engineRpm,
                predictedTargetRpm = _state.predictedRpm,
                rpmError = _state.rpmError,
                automaticTargetNwhGear = _automaticTargetNwhGear,
                automaticTargetLabel = AutomaticTargetLabel,
                shiftState = _state.shiftState,
                lastRejectionReason = _state.lastRejectionReason,
                lastAbuseSeverity = _state.lastAbuseSeverity,
                requiresShifterSynchronization = _state.requiresShifterSynchronization
            };
        }

        private LwsNwhTransmissionRuntimeState ReadNwhState()
        {
            return nwhAdapter != null ? nwhAdapter.ReadRuntimeState() : default;
        }

        private ILwsVehicleInputSource ResolveInputSource()
        {
            if (_inputService != null && _inputService.HasActiveSource)
            {
                return _inputService;
            }

            return _fallbackInputSource;
        }

        private void ConfigureNwhIfNeeded()
        {
            if (_nwhConfigured || nwhAdapter == null || !configureNwhOnStart)
            {
                return;
            }

            _nwhConfigured = nwhAdapter.ConfigureForDefinition(ActiveDefinition, out string message);
            if (!_nwhConfigured && !_nwhConfigureWarningLogged)
            {
                Debug.LogWarning(message, this);
                _nwhConfigureWarningLogged = true;
            }
        }

        private void ResolveLocalReferences()
        {
            if (nwhAdapter == null)
            {
                nwhAdapter = GetComponent<LwsNwh18SpeedTransmissionAdapter>();
            }

            if (nwhAdapter == null)
            {
                nwhAdapter = gameObject.AddComponent<LwsNwh18SpeedTransmissionAdapter>();
            }

            if (_fallbackInputSource == null && fallbackInputSourceBehaviour != null)
            {
                _fallbackInputSource = fallbackInputSourceBehaviour as ILwsVehicleInputSource;
            }
        }

        private void ResolveServices()
        {
            if (LwsApplicationBootstrap.Instance == null || LwsApplicationBootstrap.Instance.Registry == null)
            {
                return;
            }

            if (_inputService == null)
            {
                LwsApplicationBootstrap.Instance.Registry.TryGet(out _inputService);
            }

            if (_saveService == null)
            {
                LwsApplicationBootstrap.Instance.Registry.TryGet(out _saveService);
            }
        }

        private void RegisterSaveParticipantIfNeeded()
        {
            if (!registerSaveParticipant || _saveRegistered)
            {
                return;
            }

            ResolveServices();
            LwsSaveOperationResult result = _saveService != null
                ? _saveService.RegisterParticipant(this)
                : LwsSaveOperationResult.Failure("LWS save service is not available.");
            _saveRegistered = result.Succeeded;
        }
    }
}
