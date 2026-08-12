using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.InterstateHauler
{
    public enum LwsWheelConnectionState
    {
        Unknown,
        Disconnected,
        Connected
    }

    public enum LwsWheelControlKind
    {
        Unknown,
        Axis,
        Button,
        Dpad
    }

    public enum LwsWheelLogicalControl
    {
        None,
        Steering,
        Throttle,
        Brake,
        Clutch,
        ShifterGate1,
        ShifterGate2,
        ShifterGate3,
        ShifterGate4,
        ShifterGate5,
        ShifterGate6,
        ShifterReverse,
        RangeToggle,
        SplitterToggle,
        TrailerAttachDetach,
        ParkingBrake,
        Ignition,
        Horn,
        CameraCycle,
        MenuSubmit,
        MenuCancel,
        Pause,
        DPadUp,
        DPadDown,
        DPadLeft,
        DPadRight
    }

    public enum LwsWheelCompatibilityStatus
    {
        SupportedPhysicallyVerified,
        PluginVerified,
        SupportedNotPhysicallyVerified,
        PotentiallySupported,
        Unsupported,
        Unknown
    }

    [Serializable]
    public struct LwsWheelControlBinding
    {
        public LwsWheelLogicalControl logicalControl;
        public LwsWheelControlKind controlKind;
        public string controlPath;
        public string displayName;
        public float pressThreshold;

        public bool IsBound => !string.IsNullOrWhiteSpace(controlPath);

        public static LwsWheelControlBinding Create(
            LwsWheelLogicalControl logicalControl,
            LwsWheelControlKind controlKind,
            string controlPath,
            string displayName = "",
            float pressThreshold = 0.5f)
        {
            return new LwsWheelControlBinding
            {
                logicalControl = logicalControl,
                controlKind = controlKind,
                controlPath = controlPath ?? string.Empty,
                displayName = displayName ?? string.Empty,
                pressThreshold = Mathf.Clamp01(pressThreshold)
            };
        }

        public static LwsWheelControlBinding Unbound(LwsWheelLogicalControl logicalControl, LwsWheelControlKind controlKind)
        {
            return Create(logicalControl, controlKind, string.Empty);
        }
    }

    [Serializable]
    public struct LwsWheelAxisCalibration
    {
        public float center;
        public float minimum;
        public float maximum;
        [Range(0f, 0.5f)] public float deadzone;
        [Range(0f, 0.5f)] public float saturation;
        public bool inverted;
        [Range(0.25f, 4f)] public float sensitivity;
        public int steeringRotationDegrees;

        public static LwsWheelAxisCalibration SteeringDefault()
        {
            return new LwsWheelAxisCalibration
            {
                center = 0f,
                minimum = -1f,
                maximum = 1f,
                deadzone = 0.02f,
                saturation = 0f,
                inverted = false,
                sensitivity = 1f,
                steeringRotationDegrees = 900
            };
        }
    }

    [Serializable]
    public struct LwsWheelPedalCalibration
    {
        public float releasedValue;
        public float pressedValue;
        [Range(0f, 0.5f)] public float deadzone;
        [Range(0f, 0.5f)] public float saturation;
        public bool inverted;

        public static LwsWheelPedalCalibration Default()
        {
            return new LwsWheelPedalCalibration
            {
                releasedValue = 0f,
                pressedValue = 1f,
                deadzone = 0.02f,
                saturation = 0f,
                inverted = false
            };
        }
    }

    [Serializable]
    public struct LwsHPatternShifterState
    {
        public LwsTruckShifterGate activeGate;
        public bool neutral;
        public bool reverse;
        public LwsTruckRange range;
        public LwsTruckSplitter splitter;
        public bool rangePressed;
        public bool splitterPressed;

        public static LwsHPatternShifterState Neutral(LwsTruckRange range = LwsTruckRange.Low, LwsTruckSplitter splitter = LwsTruckSplitter.Low)
        {
            return new LwsHPatternShifterState
            {
                activeGate = LwsTruckShifterGate.Neutral,
                neutral = true,
                reverse = false,
                range = range,
                splitter = splitter
            };
        }
    }

    [Serializable]
    public struct LwsWheelAnalogState
    {
        public float rawSteering;
        public float rawThrottle;
        public float rawBrake;
        public float rawClutch;
        public float steering;
        public float throttle;
        public float brake;
        public float clutch;
        public float parkingBrake;
    }

    [Serializable]
    public struct LwsForceFeedbackSettings
    {
        public bool enabled;
        [Range(0f, 1f)] public float masterStrength;
        [Range(0f, 1f)] public float alignmentStrength;
        [Range(0f, 1f)] public float dampingStrength;
        [Range(0f, 1f)] public float roadStrength;
        [Range(0f, 1f)] public float impactStrength;
        public bool directInputBackendRequired;
        public string preferredDeviceSearchTerm;

        public static LwsForceFeedbackSettings SafeDefault()
        {
            return new LwsForceFeedbackSettings
            {
                enabled = false,
                masterStrength = 0.5f,
                alignmentStrength = 0.5f,
                dampingStrength = 0.25f,
                roadStrength = 0.25f,
                impactStrength = 0.25f,
                directInputBackendRequired = true,
                preferredDeviceSearchTerm = "g29"
            };
        }
    }

    [Serializable]
    public struct LwsForceFeedbackStatus
    {
        public bool available;
        public bool enabled;
        public bool deviceConnected;
        public string backendId;
        public string statusMessage;
        public string deviceDisplayName;
        public string deviceGuid;
        public string supportedEffects;
        public float masterStrength;
        public float alignmentForce;
        public float dampingForce;
        public float roadForce;
        public float impactForce;
    }

    [Serializable]
    public struct LwsWheelInputFrame
    {
        public LwsWheelConnectionState connectionState;
        public string deviceDisplayName;
        public string devicePath;
        public LwsWheelAnalogState analog;
        public LwsHPatternShifterState shifter;
        public LwsVehicleCommandFrame commands;
        public LwsForceFeedbackStatus forceFeedback;
    }

    [Serializable]
    public sealed class LwsWheelDeviceDescriptor
    {
        public string displayName;
        public string manufacturer;
        public string product;
        public string deviceClass;
        public string layout;
        public string path;
        public string interfaceName;
        public string capabilities;
        public string directInputGuid;
        public bool directInputForceFeedbackCapable;
        public string directInputSupportedEffects;
        public List<string> axes = new List<string>();
        public List<string> buttons = new List<string>();
        public List<string> dpads = new List<string>();

        public bool LooksLikeLogitechG29()
        {
            string combined = $"{displayName} {manufacturer} {product} {layout}".ToLowerInvariant();
            return combined.Contains("g29") ||
                   combined.Contains("logitech g29") ||
                   (combined.Contains("logitech") && combined.Contains("driving force"));
        }
    }
}
