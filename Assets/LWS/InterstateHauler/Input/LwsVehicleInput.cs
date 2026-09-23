using System;

namespace LWS.InterstateHauler
{
    public enum LwsMomentaryIntent
    {
        None,
        Pressed,
        Released,
        Held
    }

    public enum LwsTruckShifterGate
    {
        None,
        Reverse,
        Neutral,
        Gate1,
        Gate2,
        Gate3,
        Gate4,
        Gate5,
        Gate6,
        Gate7,
        Gate8
    }

    public enum LwsTruckRange
    {
        Low,
        High
    }

    public enum LwsTruckSplitter
    {
        Low,
        High
    }

    public enum LwsVehicleInputOwner
    {
        None,
        KeyboardMouse,
        Gamepad,
        Wheel,
        AutomatedTest
    }

    [Serializable]
    public struct LwsVehicleContinuousInput
    {
        public float steering;
        public float throttle;
        public float brake;
        public float clutch;
        public float parkingBrake;
    }

    [Serializable]
    public struct LwsTruckGearIntent
    {
        public LwsTruckShifterGate physicalGate;
        public LwsTruckRange range;
        public LwsTruckSplitter splitter;
        public int requestedLogicalGear;
        public bool neutralRequested;
        public bool reverseRequested;
    }

    [Serializable]
    public struct LwsVehicleCommandFrame
    {
        public LwsMomentaryIntent ignitionToggle;
        public LwsMomentaryIntent ignition;
        public LwsMomentaryIntent engineStart;
        public LwsMomentaryIntent engineStop;
        public LwsMomentaryIntent parkingBrakeToggle;
        public LwsMomentaryIntent horn;
        public LwsMomentaryIntent airHorn;
        public LwsMomentaryIntent lowBeamLights;
        public LwsMomentaryIntent highBeamLights;
        public LwsMomentaryIntent hazardLights;
        public LwsMomentaryIntent leftIndicator;
        public LwsMomentaryIntent rightIndicator;
        public LwsMomentaryIntent wipers;
        public LwsMomentaryIntent wiperIncrease;
        public LwsMomentaryIntent wiperDecrease;
        public LwsMomentaryIntent cruiseControl;
        public LwsMomentaryIntent cruiseSet;
        public LwsMomentaryIntent cruiseResume;
        public LwsMomentaryIntent cruiseCancel;
        public LwsMomentaryIntent cruiseIncrease;
        public LwsMomentaryIntent cruiseDecrease;
        public LwsMomentaryIntent engineBrake;
        public LwsMomentaryIntent engineBrakeIncrease;
        public LwsMomentaryIntent engineBrakeDecrease;
        public LwsMomentaryIntent retarder;
        public LwsMomentaryIntent retarderIncrease;
        public LwsMomentaryIntent retarderDecrease;
        public LwsMomentaryIntent differentialLock;
        public LwsMomentaryIntent transmissionShiftUp;
        public LwsMomentaryIntent transmissionShiftDown;
        public LwsMomentaryIntent trailerAttachDetach;
        public LwsMomentaryIntent trailerBrake;
        public LwsMomentaryIntent cameraCycle;
        public LwsMomentaryIntent lookReset;
        public LwsMomentaryIntent resetTruckUpright;
        public LwsMomentaryIntent flipOffDriver;
        public LwsMomentaryIntent interact;
        public LwsMomentaryIntent menuSubmit;
        public LwsMomentaryIntent menuCancel;
        public LwsMomentaryIntent pause;
        public LwsMomentaryIntent navigateUp;
        public LwsMomentaryIntent navigateDown;
        public LwsMomentaryIntent navigateLeft;
        public LwsMomentaryIntent navigateRight;
    }

    public interface ILwsVehicleInputSource
    {
        string SourceId { get; }
        LwsVehicleContinuousInput ReadContinuousInput();
        LwsVehicleCommandFrame ReadCommandFrame();
        LwsTruckGearIntent ReadGearIntent();
    }

    public interface ILwsVehicleInputService : ILwsService, ILwsVehicleInputSource
    {
        LwsVehicleInputOwner ActiveOwner { get; }
        string ActiveSourceId { get; }
        bool HasActiveSource { get; }
        event Action<LwsVehicleInputOwner, LwsVehicleInputOwner> InputOwnerChanged;
        void SetInputSource(ILwsVehicleInputSource source);
        LwsServiceResult SetInputSource(ILwsVehicleInputSource source, LwsVehicleInputOwner owner, bool force = false);
        LwsServiceResult ReleaseInputSource(ILwsVehicleInputSource source);
        void NeutralizeInput();
    }

    public sealed class LwsVehicleInputService : ILwsVehicleInputService
    {
        private static readonly ILwsVehicleInputSource NeutralSource = new LwsNeutralVehicleInputSource();

        private ILwsVehicleInputSource _source;
        private LwsVehicleInputOwner _activeOwner;

        public string ServiceId => "lws.input.vehicle";
        public string SourceId => _source?.SourceId ?? "lws.input.none";
        public LwsVehicleInputOwner ActiveOwner => _activeOwner;
        public string ActiveSourceId => _source?.SourceId ?? string.Empty;
        public bool HasActiveSource => _source != null && _source != NeutralSource;

        public event Action<LwsVehicleInputOwner, LwsVehicleInputOwner> InputOwnerChanged;

        public LwsServiceResult Initialize(LwsServiceContext context)
        {
            NeutralizeInput();
            return LwsServiceResult.Success("LWS vehicle input service initialized.");
        }

        public LwsServiceResult Shutdown(LwsServiceContext context)
        {
            NeutralizeInput();
            return LwsServiceResult.Success("LWS vehicle input service shut down.");
        }

        public void SetInputSource(ILwsVehicleInputSource source)
        {
            SetInputSource(source, source == null ? LwsVehicleInputOwner.None : LwsVehicleInputOwner.KeyboardMouse, true);
        }

        public LwsServiceResult SetInputSource(ILwsVehicleInputSource source, LwsVehicleInputOwner owner, bool force = false)
        {
            if (source == null || owner == LwsVehicleInputOwner.None)
            {
                NeutralizeInput();
                return LwsServiceResult.Success("Vehicle input source cleared.");
            }

            if (_source != null && _source != NeutralSource && _source != source && _activeOwner == owner && !force)
            {
                return LwsServiceResult.Failure($"Vehicle input owner {owner} already has an active source: {_source.SourceId}");
            }

            LwsVehicleInputOwner previousOwner = _activeOwner;
            _source = source;
            _activeOwner = owner;
            if (previousOwner != _activeOwner)
            {
                InputOwnerChanged?.Invoke(previousOwner, _activeOwner);
            }

            return LwsServiceResult.Success($"Vehicle input source set to {source.SourceId} with owner {owner}.");
        }

        public LwsServiceResult ReleaseInputSource(ILwsVehicleInputSource source)
        {
            if (source == null || _source == null || _source == NeutralSource)
            {
                NeutralizeInput();
                return LwsServiceResult.Success("No active vehicle input source to release.");
            }

            if (_source != source)
            {
                return LwsServiceResult.Failure($"Cannot release non-active vehicle input source: {source.SourceId}");
            }

            NeutralizeInput();
            return LwsServiceResult.Success("Vehicle input source released.");
        }

        public void NeutralizeInput()
        {
            LwsVehicleInputOwner previousOwner = _activeOwner;
            _source = NeutralSource;
            _activeOwner = LwsVehicleInputOwner.None;
            if (previousOwner != _activeOwner)
            {
                InputOwnerChanged?.Invoke(previousOwner, _activeOwner);
            }
        }

        public LwsVehicleContinuousInput ReadContinuousInput()
        {
            return _source?.ReadContinuousInput() ?? default;
        }

        public LwsVehicleCommandFrame ReadCommandFrame()
        {
            return _source?.ReadCommandFrame() ?? default;
        }

        public LwsTruckGearIntent ReadGearIntent()
        {
            return _source?.ReadGearIntent() ?? default;
        }

        private sealed class LwsNeutralVehicleInputSource : ILwsVehicleInputSource
        {
            public string SourceId => "lws.input.neutral";

            public LwsVehicleContinuousInput ReadContinuousInput()
            {
                return default;
            }

            public LwsVehicleCommandFrame ReadCommandFrame()
            {
                return default;
            }

            public LwsTruckGearIntent ReadGearIntent()
            {
                return new LwsTruckGearIntent
                {
                    physicalGate = LwsTruckShifterGate.Neutral,
                    neutralRequested = true
                };
            }
        }
    }
}
