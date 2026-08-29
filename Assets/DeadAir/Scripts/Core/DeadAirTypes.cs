using System;
using UnityEngine;

namespace DeadAir
{
    public enum DeadAirGameState
    {
        Boot,
        Playing,
        Ending,
        Restarting,
        Paused
    }

    public enum DeadAirControlMode
    {
        BasicAutomatic
    }

    public enum DeadAirPlacementStatus
    {
        Unplaced,
        Draft,
        Placed,
        Approved
    }

    public enum DeadAirTriggerCategory
    {
        Dispatch,
        GPS,
        CBRadio,
        Environment,
        Traffic,
        Dashboard,
        Story,
        ChoiceStart,
        ChoiceCommit,
        Ending
    }

    public enum DeadAirChoiceOutcome
    {
        None,
        TrustDispatch,
        TrustGPS,
        TurnBack,
        Exit17,
        TrustNoOne,
        Lost
    }

    public enum DeadAirEndingId
    {
        None,
        Exit17,
        TrustDispatch,
        Lost,
        TrustNoOne,
        SuckedIntoVoid
    }

    public enum DeadAirGpsArrow
    {
        None,
        Straight,
        SlightLeft,
        SlightRight,
        Left,
        Right,
        Exit,
        UTurn,
        ExitLeft,
        ExitRight
    }

    public enum DeadAirGpsPresentationMode
    {
        Normal,
        Recalculating,
        SignalLost,
        Corrupted,
        Hidden
    }


    public enum DeadAirGpsNarrativeEventType
    {
        None,
        ChangeDirection,
        FlashAndChangeDirection,
        Glitch,
        GlitchThenDirection,
        Recalculating,
        RecalculatingThenDirection,
        ClearOverlay,
        CustomCombined
    }
    public enum DeadAirAudioChannel
    {
        Dispatcher,
        CBRadio,
        GPS,
        Ambient,
        World,
        SFX
    }

    public enum DeadAirAudioCollisionBehavior
    {
        Queue,
        Interrupt,
        Ignore,
        Wait
    }

    public enum DeadAirAnomalyKind
    {
        None,
        WeatherPreset,
        Fog,
        Lightning,
        TimeOfDay,
        DashboardOverride,
        AudioStatic,
        GpsCorruption,
        TrafficHint,
        AmbientSilence,
        AmbientRestore,
        WeatherRestore
    }

    public enum DeadAirRoadPieceKind
    {
        Straight,
        Curve,
        Fork,
        ExitRamp,
        Merge,
        DeadEnd
    }

    public enum DeadAirSignKind
    {
        RoadSign,
        ExitSign,
        WarningSign,
        DestinationSign,
        AnomalySign
    }

    public enum DeadAirDashboardEventKind
    {
        None,
        Flicker,
        WrongSpeed,
        WrongGear,
        WarningLamp,
        Blackout,
        Reset
    }

    public enum DeadAirTrafficHorrorEventKind
    {
        None,
        HeadlightsBehind,
        PassingTruck,
        StoppedVehicle,
        PhantomConvoy
    }

    [Serializable]
    public struct DeadAirTriggerEvent
    {
        public string beatId;
        public string displayName;
        public string description;
        public string notes;
        public DeadAirTriggerCategory category;
        public float estimatedPlaybackSeconds;
        public float routeDistanceMiles;
        public Vector3 position;
        public GameObject triggerObject;
        public string choiceId;
        public DeadAirChoiceOutcome choiceOutcome;
        public float timestamp;

        public bool IsChoice => category == DeadAirTriggerCategory.ChoiceStart || category == DeadAirTriggerCategory.ChoiceCommit;
    }

    [Serializable]
    public struct DeadAirGpsState
    {
        public bool enabled;
        public DeadAirGpsPresentationMode presentationMode;
        public string instructionText;
        public DeadAirGpsArrow arrow;
        public float distanceToInstructionMeters;
        public string destination;
        public bool routeVisible;
        public bool intentionallyWrong;
        public string currentRoad;
        public float speedLimitMph;
        public float remainingRouteMeters;

        public static DeadAirGpsState Default()
        {
            return new DeadAirGpsState
            {
                enabled = true,
                presentationMode = DeadAirGpsPresentationMode.Normal,
                instructionText = "Continue north.",
                arrow = DeadAirGpsArrow.Straight,
                distanceToInstructionMeters = 805f,
                destination = "Exit 17",
                routeVisible = true,
                currentRoad = "Unknown Road",
                speedLimitMph = 55f,
                remainingRouteMeters = 80467f
            };
        }
    }


    [Serializable]
    public sealed class DeadAirGpsNarrativeEvent
    {
        [Header("DEAD AIR GPS EVENT")]
        public bool enableGpsEvent;

        public DeadAirGpsNarrativeEventType eventType = DeadAirGpsNarrativeEventType.None;
        [Min(0f)] public float delayBeforeGpsEvent;

        [Header("Direction")]
        public string primaryText = "CONTINUE STRAIGHT";
        public string secondaryText = "8.0 MI";
        public DeadAirGpsArrow arrow = DeadAirGpsArrow.Straight;
        [Min(0f)] public float distanceMeters = 12874.752f;
        public string destination;
        public bool routeVisible = true;
        public bool intentionallyWrong;

        [Header("Effects")]
        public bool flash = true;
        public bool glitch;
        [Min(0.05f)] public float glitchDuration = 0.75f;
        [Range(0f, 1f)] public float glitchIntensity = 0.65f;

        [Header("Recalculating")]
        public bool recalculating;
        public string recalculatingMessage = "RECALCULATING...";
        [Min(0f)] public float recalculatingDuration = 1.4f;
        public string finalPrimaryText = "TAKE NEXT EXIT";
        public string finalSecondaryText = "1 MI";
        public DeadAirGpsArrow finalArrow = DeadAirGpsArrow.ExitRight;

        [Header("Audio")]
        public AudioClip gpsAudioClip;
        [Range(0f, 1f)] public float gpsAudioVolume = 0.75f;

        public bool ShouldRun => enableGpsEvent && eventType != DeadAirGpsNarrativeEventType.None;

        public DeadAirGpsNarrativeEvent Clone()
        {
            return (DeadAirGpsNarrativeEvent)MemberwiseClone();
        }
    }
    [Serializable]
    public struct DeadAirVehicleSnapshot
    {
        public bool available;
        public float speedMph;
        public float signedSpeedMph;
        public Vector3 position;
        public Vector3 forward;
        public bool moving;
        public bool reverse;
        public bool hornActive;
        public string transmissionMode;
        public bool trailerConnected;
    }

    [Serializable]
    public struct DeadAirRoadBoundaryEvaluation
    {
        public bool tractorFrontValid;
        public bool tractorRearValid;
        public bool trailerFrontValid;
        public bool trailerRearValid;
        public bool tractorValid;
        public bool trailerValid;
        public bool anyRigPointValid;
        public bool entireRigOffRoad;
        public int validZoneCount;
        public float graceTimerSeconds;
        public float graceDurationSeconds;

        public bool GraceActive => entireRigOffRoad && graceTimerSeconds > 0f && graceTimerSeconds < graceDurationSeconds;
    }

    [Serializable]
    public sealed class DeadAirBeatDefinition
    {
        public string beatId;
        public string displayName;
        [TextArea] public string description;
        [TextArea] public string notes;
        public DeadAirTriggerCategory category = DeadAirTriggerCategory.Story;
        public float estimatedPlaybackSeconds = 8f;
        public float routeDistanceMiles;
        public Color debugColor = new Color(0.2f, 0.85f, 1f, 0.35f);
        public DeadAirPlacementStatus placementStatus = DeadAirPlacementStatus.Unplaced;
        public DeadAirChoiceOutcome choiceOutcome = DeadAirChoiceOutcome.None;
        public bool critical = true;
    }
}
