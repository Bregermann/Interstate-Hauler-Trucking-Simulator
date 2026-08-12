using System;
using System.IO;
using UnityEngine;

namespace LWS.InterstateHauler
{
    [Serializable]
    public sealed class LwsWheelCalibrationProfile
    {
        public int schemaVersion = 1;
        public string profileId = "directinput.logitech.g29.global";
        public string hardwareProfileId = "directinput.logitech.g29.driving-force";
        public string displayName = "DirectInput Logitech G29 Global Calibration";
        public string selectedDevicePath;
        public string selectedDeviceLayout;
        public string selectedDeviceDisplayName;
        public bool validationGearMappingEnabled = false;
        public LwsWheelAxisCalibration steering = LwsWheelAxisCalibration.SteeringDefault();
        public LwsWheelPedalCalibration throttle = LwsWheelPedalCalibration.Default();
        public LwsWheelPedalCalibration brake = LwsWheelPedalCalibration.Default();
        public LwsWheelPedalCalibration clutch = LwsWheelPedalCalibration.Default();
        public LwsWheelControlBinding steeringBinding = LwsWheelControlBinding.Unbound(LwsWheelLogicalControl.Steering, LwsWheelControlKind.Axis);
        public LwsWheelControlBinding throttleBinding = LwsWheelControlBinding.Unbound(LwsWheelLogicalControl.Throttle, LwsWheelControlKind.Axis);
        public LwsWheelControlBinding brakeBinding = LwsWheelControlBinding.Unbound(LwsWheelLogicalControl.Brake, LwsWheelControlKind.Axis);
        public LwsWheelControlBinding clutchBinding = LwsWheelControlBinding.Unbound(LwsWheelLogicalControl.Clutch, LwsWheelControlKind.Axis);
        public LwsWheelControlBinding gate1Binding = LwsWheelControlBinding.Unbound(LwsWheelLogicalControl.ShifterGate1, LwsWheelControlKind.Button);
        public LwsWheelControlBinding gate2Binding = LwsWheelControlBinding.Unbound(LwsWheelLogicalControl.ShifterGate2, LwsWheelControlKind.Button);
        public LwsWheelControlBinding gate3Binding = LwsWheelControlBinding.Unbound(LwsWheelLogicalControl.ShifterGate3, LwsWheelControlKind.Button);
        public LwsWheelControlBinding gate4Binding = LwsWheelControlBinding.Unbound(LwsWheelLogicalControl.ShifterGate4, LwsWheelControlKind.Button);
        public LwsWheelControlBinding gate5Binding = LwsWheelControlBinding.Unbound(LwsWheelLogicalControl.ShifterGate5, LwsWheelControlKind.Button);
        public LwsWheelControlBinding gate6Binding = LwsWheelControlBinding.Unbound(LwsWheelLogicalControl.ShifterGate6, LwsWheelControlKind.Button);
        public LwsWheelControlBinding reverseBinding = LwsWheelControlBinding.Unbound(LwsWheelLogicalControl.ShifterReverse, LwsWheelControlKind.Button);
        public LwsWheelControlBinding rangeBinding = LwsWheelControlBinding.Unbound(LwsWheelLogicalControl.RangeToggle, LwsWheelControlKind.Button);
        public LwsWheelControlBinding splitterBinding = LwsWheelControlBinding.Unbound(LwsWheelLogicalControl.SplitterToggle, LwsWheelControlKind.Button);
        public LwsWheelControlBinding trailerAttachDetachBinding = LwsWheelControlBinding.Unbound(LwsWheelLogicalControl.TrailerAttachDetach, LwsWheelControlKind.Button);
        public LwsWheelControlBinding parkingBrakeBinding = LwsWheelControlBinding.Unbound(LwsWheelLogicalControl.ParkingBrake, LwsWheelControlKind.Button);
        public LwsWheelControlBinding ignitionBinding = LwsWheelControlBinding.Unbound(LwsWheelLogicalControl.Ignition, LwsWheelControlKind.Button);
        public LwsWheelControlBinding hornBinding = LwsWheelControlBinding.Unbound(LwsWheelLogicalControl.Horn, LwsWheelControlKind.Button);
        public LwsWheelControlBinding cameraCycleBinding = LwsWheelControlBinding.Unbound(LwsWheelLogicalControl.CameraCycle, LwsWheelControlKind.Button);
        public LwsWheelControlBinding menuSubmitBinding = LwsWheelControlBinding.Unbound(LwsWheelLogicalControl.MenuSubmit, LwsWheelControlKind.Button);
        public LwsWheelControlBinding menuCancelBinding = LwsWheelControlBinding.Unbound(LwsWheelLogicalControl.MenuCancel, LwsWheelControlKind.Button);
        public LwsWheelControlBinding pauseBinding = LwsWheelControlBinding.Unbound(LwsWheelLogicalControl.Pause, LwsWheelControlKind.Button);
        public LwsWheelControlBinding dpadUpBinding = LwsWheelControlBinding.Unbound(LwsWheelLogicalControl.DPadUp, LwsWheelControlKind.Button);
        public LwsWheelControlBinding dpadDownBinding = LwsWheelControlBinding.Unbound(LwsWheelLogicalControl.DPadDown, LwsWheelControlKind.Button);
        public LwsWheelControlBinding dpadLeftBinding = LwsWheelControlBinding.Unbound(LwsWheelLogicalControl.DPadLeft, LwsWheelControlKind.Button);
        public LwsWheelControlBinding dpadRightBinding = LwsWheelControlBinding.Unbound(LwsWheelLogicalControl.DPadRight, LwsWheelControlKind.Button);
        public LwsForceFeedbackSettings forceFeedback = LwsForceFeedbackSettings.SafeDefault();

        public static LwsWheelCalibrationProfile CreateDefaultLogitechG29()
        {
            return new LwsWheelCalibrationProfile();
        }

        public bool ValidateRanges(out string message)
        {
            if (string.IsNullOrWhiteSpace(profileId))
            {
                message = "Wheel calibration profile ID is required.";
                return false;
            }

            if (!LwsWheelCalibrationUtility.ValidateSteeringCalibration(steering, out message))
            {
                return false;
            }

            if (!LwsWheelCalibrationUtility.ValidatePedalCalibration(throttle, "Throttle", out message) ||
                !LwsWheelCalibrationUtility.ValidatePedalCalibration(brake, "Brake", out message) ||
                !LwsWheelCalibrationUtility.ValidatePedalCalibration(clutch, "Clutch", out message))
            {
                return false;
            }

            message = "Wheel calibration ranges are valid.";
            return true;
        }

        public bool HasRequiredDrivingBindings()
        {
            return steeringBinding.IsBound &&
                   throttleBinding.IsBound &&
                   brakeBinding.IsBound &&
                   clutchBinding.IsBound &&
                   gate1Binding.IsBound &&
                   gate2Binding.IsBound &&
                   gate3Binding.IsBound &&
                   gate4Binding.IsBound &&
                   gate5Binding.IsBound &&
                   gate6Binding.IsBound &&
                   reverseBinding.IsBound &&
                   rangeBinding.IsBound &&
                   splitterBinding.IsBound;
        }

        public LwsWheelControlBinding GetBinding(LwsWheelLogicalControl logicalControl)
        {
            switch (logicalControl)
            {
                case LwsWheelLogicalControl.Steering: return steeringBinding;
                case LwsWheelLogicalControl.Throttle: return throttleBinding;
                case LwsWheelLogicalControl.Brake: return brakeBinding;
                case LwsWheelLogicalControl.Clutch: return clutchBinding;
                case LwsWheelLogicalControl.ShifterGate1: return gate1Binding;
                case LwsWheelLogicalControl.ShifterGate2: return gate2Binding;
                case LwsWheelLogicalControl.ShifterGate3: return gate3Binding;
                case LwsWheelLogicalControl.ShifterGate4: return gate4Binding;
                case LwsWheelLogicalControl.ShifterGate5: return gate5Binding;
                case LwsWheelLogicalControl.ShifterGate6: return gate6Binding;
                case LwsWheelLogicalControl.ShifterReverse: return reverseBinding;
                case LwsWheelLogicalControl.RangeToggle: return rangeBinding;
                case LwsWheelLogicalControl.SplitterToggle: return splitterBinding;
                case LwsWheelLogicalControl.TrailerAttachDetach: return trailerAttachDetachBinding;
                case LwsWheelLogicalControl.ParkingBrake: return parkingBrakeBinding;
                case LwsWheelLogicalControl.Ignition: return ignitionBinding;
                case LwsWheelLogicalControl.Horn: return hornBinding;
                case LwsWheelLogicalControl.CameraCycle: return cameraCycleBinding;
                case LwsWheelLogicalControl.MenuSubmit: return menuSubmitBinding;
                case LwsWheelLogicalControl.MenuCancel: return menuCancelBinding;
                case LwsWheelLogicalControl.Pause: return pauseBinding;
                case LwsWheelLogicalControl.DPadUp: return dpadUpBinding;
                case LwsWheelLogicalControl.DPadDown: return dpadDownBinding;
                case LwsWheelLogicalControl.DPadLeft: return dpadLeftBinding;
                case LwsWheelLogicalControl.DPadRight: return dpadRightBinding;
                default: return LwsWheelControlBinding.Unbound(logicalControl, LwsWheelControlKind.Unknown);
            }
        }

        public void SetBinding(LwsWheelControlBinding binding)
        {
            switch (binding.logicalControl)
            {
                case LwsWheelLogicalControl.Steering: steeringBinding = binding; break;
                case LwsWheelLogicalControl.Throttle: throttleBinding = binding; break;
                case LwsWheelLogicalControl.Brake: brakeBinding = binding; break;
                case LwsWheelLogicalControl.Clutch: clutchBinding = binding; break;
                case LwsWheelLogicalControl.ShifterGate1: gate1Binding = binding; break;
                case LwsWheelLogicalControl.ShifterGate2: gate2Binding = binding; break;
                case LwsWheelLogicalControl.ShifterGate3: gate3Binding = binding; break;
                case LwsWheelLogicalControl.ShifterGate4: gate4Binding = binding; break;
                case LwsWheelLogicalControl.ShifterGate5: gate5Binding = binding; break;
                case LwsWheelLogicalControl.ShifterGate6: gate6Binding = binding; break;
                case LwsWheelLogicalControl.ShifterReverse: reverseBinding = binding; break;
                case LwsWheelLogicalControl.RangeToggle: rangeBinding = binding; break;
                case LwsWheelLogicalControl.SplitterToggle: splitterBinding = binding; break;
                case LwsWheelLogicalControl.TrailerAttachDetach: trailerAttachDetachBinding = binding; break;
                case LwsWheelLogicalControl.ParkingBrake: parkingBrakeBinding = binding; break;
                case LwsWheelLogicalControl.Ignition: ignitionBinding = binding; break;
                case LwsWheelLogicalControl.Horn: hornBinding = binding; break;
                case LwsWheelLogicalControl.CameraCycle: cameraCycleBinding = binding; break;
                case LwsWheelLogicalControl.MenuSubmit: menuSubmitBinding = binding; break;
                case LwsWheelLogicalControl.MenuCancel: menuCancelBinding = binding; break;
                case LwsWheelLogicalControl.Pause: pauseBinding = binding; break;
                case LwsWheelLogicalControl.DPadUp: dpadUpBinding = binding; break;
                case LwsWheelLogicalControl.DPadDown: dpadDownBinding = binding; break;
                case LwsWheelLogicalControl.DPadLeft: dpadLeftBinding = binding; break;
                case LwsWheelLogicalControl.DPadRight: dpadRightBinding = binding; break;
            }
        }

        public string ToJson(bool prettyPrint = false)
        {
            return JsonUtility.ToJson(this, prettyPrint);
        }

        public static LwsWheelCalibrationProfile FromJson(string json)
        {
            return string.IsNullOrWhiteSpace(json)
                ? CreateDefaultLogitechG29()
                : JsonUtility.FromJson<LwsWheelCalibrationProfile>(json);
        }
    }

    public static class LwsWheelCalibrationUtility
    {
        public static float NormalizeSteering(float rawValue, LwsWheelAxisCalibration calibration)
        {
            float center = calibration.center;
            float denominator = rawValue >= center
                ? calibration.maximum - center
                : center - calibration.minimum;
            if (Mathf.Abs(denominator) < 0.0001f)
            {
                return 0f;
            }

            float value = Mathf.Clamp(rawValue >= center
                ? (rawValue - center) / denominator
                : (rawValue - center) / denominator, -1f, 1f);
            if (calibration.inverted)
            {
                value = -value;
            }

            value = ApplyBipolarDeadzoneAndSaturation(value, calibration.deadzone, calibration.saturation);
            float sensitivity = Mathf.Max(0.25f, calibration.sensitivity);
            return Mathf.Sign(value) * Mathf.Pow(Mathf.Abs(value), sensitivity);
        }

        public static float NormalizePedal(float rawValue, LwsWheelPedalCalibration calibration)
        {
            float denominator = calibration.pressedValue - calibration.releasedValue;
            if (Mathf.Abs(denominator) < 0.0001f)
            {
                return 0f;
            }

            float value = Mathf.Clamp01((rawValue - calibration.releasedValue) / denominator);
            if (calibration.inverted)
            {
                value = 1f - value;
            }

            return ApplyUnipolarDeadzoneAndSaturation(value, calibration.deadzone, calibration.saturation);
        }

        public static float ApplyBipolarDeadzoneAndSaturation(float value, float deadzone, float saturation)
        {
            float sign = Mathf.Sign(value);
            float magnitude = Mathf.Abs(value);
            float dz = Mathf.Clamp01(deadzone);
            float sat = Mathf.Clamp01(saturation);
            if (magnitude <= dz)
            {
                return 0f;
            }

            float usable = Mathf.Max(0.0001f, 1f - dz - sat);
            return sign * Mathf.Clamp01((magnitude - dz) / usable);
        }

        public static float ApplyUnipolarDeadzoneAndSaturation(float value, float deadzone, float saturation)
        {
            float dz = Mathf.Clamp01(deadzone);
            float sat = Mathf.Clamp01(saturation);
            if (value <= dz)
            {
                return 0f;
            }

            float usable = Mathf.Max(0.0001f, 1f - dz - sat);
            return Mathf.Clamp01((value - dz) / usable);
        }

        public static int ValidationNwhGearForGate(LwsTruckShifterGate gate)
        {
            switch (gate)
            {
                case LwsTruckShifterGate.Reverse: return -1;
                case LwsTruckShifterGate.Neutral: return 0;
                case LwsTruckShifterGate.Gate1: return 1;
                case LwsTruckShifterGate.Gate2: return 2;
                case LwsTruckShifterGate.Gate3: return 3;
                case LwsTruckShifterGate.Gate4: return 4;
                case LwsTruckShifterGate.Gate5: return 5;
                case LwsTruckShifterGate.Gate6: return 6;
                default: return -999;
            }
        }

        public static LwsTruckGearIntent ToValidationGearIntent(LwsHPatternShifterState state, bool validationMappingEnabled)
        {
            if (!validationMappingEnabled)
            {
                return new LwsTruckGearIntent
                {
                    physicalGate = state.activeGate,
                    range = state.range,
                    splitter = state.splitter,
                    requestedLogicalGear = 0,
                    neutralRequested = state.neutral || state.activeGate == LwsTruckShifterGate.Neutral,
                    reverseRequested = state.reverse || state.activeGate == LwsTruckShifterGate.Reverse
                };
            }

            int nwhGear = ValidationNwhGearForGate(state.activeGate);
            return new LwsTruckGearIntent
            {
                physicalGate = state.activeGate,
                range = state.range,
                splitter = state.splitter,
                requestedLogicalGear = nwhGear > 0 ? nwhGear : 0,
                neutralRequested = state.neutral || nwhGear == 0,
                reverseRequested = state.reverse || nwhGear < 0
            };
        }

        public static float ClampForce(float force)
        {
            return Mathf.Clamp(force, -1f, 1f);
        }

        public static bool ValidateSteeringCalibration(LwsWheelAxisCalibration calibration, out string message)
        {
            if (!IsFinite(calibration.center) || !IsFinite(calibration.minimum) || !IsFinite(calibration.maximum))
            {
                message = "Steering calibration contains a non-finite value.";
                return false;
            }

            if (Mathf.Abs(calibration.maximum - calibration.minimum) < 0.01f)
            {
                message = "Steering calibration minimum and maximum are too close together.";
                return false;
            }

            if (calibration.center <= Mathf.Min(calibration.minimum, calibration.maximum) ||
                calibration.center >= Mathf.Max(calibration.minimum, calibration.maximum))
            {
                message = "Steering center must be between minimum and maximum.";
                return false;
            }

            message = "Steering calibration is valid.";
            return true;
        }

        public static bool ValidatePedalCalibration(LwsWheelPedalCalibration calibration, string pedalName, out string message)
        {
            if (!IsFinite(calibration.releasedValue) || !IsFinite(calibration.pressedValue))
            {
                message = $"{pedalName} calibration contains a non-finite value.";
                return false;
            }

            if (Mathf.Abs(calibration.pressedValue - calibration.releasedValue) < 0.01f)
            {
                message = $"{pedalName} released and pressed values are too close together.";
                return false;
            }

            message = $"{pedalName} calibration is valid.";
            return true;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }

    [CreateAssetMenu(menuName = "Interstate Hauler/Input/Wheel Calibration Template", fileName = "IH_WheelCalibrationTemplate")]
    public sealed class LwsWheelCalibrationAsset : ScriptableObject
    {
        [SerializeField] private LwsWheelCalibrationProfile profile = LwsWheelCalibrationProfile.CreateDefaultLogitechG29();

        public LwsWheelCalibrationProfile Profile => profile;

        public bool Validate(out string message)
        {
            if (profile == null)
            {
                message = $"{name} has no wheel calibration profile.";
                return false;
            }

            return profile.ValidateRanges(out message);
        }
    }

    public interface ILwsWheelCalibrationService : ILwsService
    {
        LwsWheelCalibrationProfile CurrentProfile { get; }
        string GlobalProfilePath { get; }
        LwsServiceResult LoadGlobalProfile();
        LwsServiceResult SaveGlobalProfile();
        void SetProfile(LwsWheelCalibrationProfile profile);
    }

    public sealed class LwsWheelCalibrationService : ILwsWheelCalibrationService
    {
        private const string RelativeDirectory = "InterstateHauler";
        private const string Filename = "WheelCalibration_Global.json";

        public string ServiceId => "lws.input.wheel.calibration";
        public LwsWheelCalibrationProfile CurrentProfile { get; private set; } = LwsWheelCalibrationProfile.CreateDefaultLogitechG29();
        public string GlobalProfilePath => Path.Combine(Application.persistentDataPath, RelativeDirectory, Filename);

        public LwsServiceResult Initialize(LwsServiceContext context)
        {
            return LoadGlobalProfile();
        }

        public LwsServiceResult Shutdown(LwsServiceContext context)
        {
            return LwsServiceResult.Success("LWS wheel calibration service shut down.");
        }

        public LwsServiceResult LoadGlobalProfile()
        {
            try
            {
                string path = GlobalProfilePath;
                if (!File.Exists(path))
                {
                    CurrentProfile = LwsWheelCalibrationProfile.CreateDefaultLogitechG29();
                    return LwsServiceResult.Success("Using default Unity-DirectInput Logitech G29 calibration profile.");
                }

                CurrentProfile = LwsWheelCalibrationProfile.FromJson(File.ReadAllText(path)) ??
                                 LwsWheelCalibrationProfile.CreateDefaultLogitechG29();
                return CurrentProfile.ValidateRanges(out string message)
                    ? LwsServiceResult.Success($"Loaded wheel calibration profile. {message}")
                    : LwsServiceResult.Failure(message);
            }
            catch (Exception ex)
            {
                CurrentProfile = LwsWheelCalibrationProfile.CreateDefaultLogitechG29();
                return LwsServiceResult.Failure($"Failed to load wheel calibration profile: {ex.Message}");
            }
        }

        public LwsServiceResult SaveGlobalProfile()
        {
            try
            {
                if (CurrentProfile == null)
                {
                    return LwsServiceResult.Failure("Cannot save a null wheel calibration profile.");
                }

                if (!CurrentProfile.ValidateRanges(out string message))
                {
                    return LwsServiceResult.Failure(message);
                }

                string path = GlobalProfilePath;
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, CurrentProfile.ToJson(true));
                return LwsServiceResult.Success($"Saved wheel calibration profile: {path}");
            }
            catch (Exception ex)
            {
                return LwsServiceResult.Failure($"Failed to save wheel calibration profile: {ex.Message}");
            }
        }

        public void SetProfile(LwsWheelCalibrationProfile profile)
        {
            CurrentProfile = profile ?? LwsWheelCalibrationProfile.CreateDefaultLogitechG29();
        }
    }
}
