using UnityEngine;

namespace LWS.InterstateHauler
{
    [DisallowMultipleComponent]
    public sealed class LwsWheelCalibrationPanel : MonoBehaviour
    {
        private enum StepKind
        {
            SteeringCenter,
            SteeringLeft,
            SteeringRight,
            PedalReleased,
            PedalPressed,
            Button,
            Neutral
        }

        private struct Step
        {
            public string label;
            public LwsWheelLogicalControl control;
            public StepKind kind;

            public Step(string label, LwsWheelLogicalControl control, StepKind kind)
            {
                this.label = label;
                this.control = control;
                this.kind = kind;
            }
        }

        private static readonly Step[] Steps =
        {
            new Step("Center steering wheel", LwsWheelLogicalControl.Steering, StepKind.SteeringCenter),
            new Step("Rotate full left", LwsWheelLogicalControl.Steering, StepKind.SteeringLeft),
            new Step("Rotate full right", LwsWheelLogicalControl.Steering, StepKind.SteeringRight),
            new Step("Release throttle", LwsWheelLogicalControl.Throttle, StepKind.PedalReleased),
            new Step("Fully depress throttle", LwsWheelLogicalControl.Throttle, StepKind.PedalPressed),
            new Step("Release brake", LwsWheelLogicalControl.Brake, StepKind.PedalReleased),
            new Step("Fully depress brake", LwsWheelLogicalControl.Brake, StepKind.PedalPressed),
            new Step("Release clutch", LwsWheelLogicalControl.Clutch, StepKind.PedalReleased),
            new Step("Fully depress clutch", LwsWheelLogicalControl.Clutch, StepKind.PedalPressed),
            new Step("Shift into physical gate 1", LwsWheelLogicalControl.ShifterGate1, StepKind.Button),
            new Step("Shift into physical gate 2", LwsWheelLogicalControl.ShifterGate2, StepKind.Button),
            new Step("Shift into physical gate 3", LwsWheelLogicalControl.ShifterGate3, StepKind.Button),
            new Step("Shift into physical gate 4", LwsWheelLogicalControl.ShifterGate4, StepKind.Button),
            new Step("Shift into physical gate 5", LwsWheelLogicalControl.ShifterGate5, StepKind.Button),
            new Step("Shift into physical gate 6", LwsWheelLogicalControl.ShifterGate6, StepKind.Button),
            new Step("Shift into reverse", LwsWheelLogicalControl.ShifterReverse, StepKind.Button),
            new Step("Return to neutral", LwsWheelLogicalControl.None, StepKind.Neutral),
            new Step("Press intended RANGE button", LwsWheelLogicalControl.RangeToggle, StepKind.Button),
            new Step("Press intended SPLITTER button", LwsWheelLogicalControl.SplitterToggle, StepKind.Button)
        };

        [SerializeField] private LwsWheelInputSource wheelInputSource;
        [SerializeField] private bool visible = true;
        [SerializeField] private int stepIndex;

        private string _lastMessage = string.Empty;

        private void Reset()
        {
            wheelInputSource = FindFirstObjectByType<LwsWheelInputSource>();
        }

        private void Awake()
        {
            if (wheelInputSource == null)
            {
                wheelInputSource = GetComponent<LwsWheelInputSource>();
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void OnGUI()
        {
            if (!visible)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(12f, 244f, 420f, 360f), GUI.skin.box);
            GUILayout.Label("G29 Calibration");
            if (wheelInputSource == null)
            {
                GUILayout.Label("Wheel input source missing.");
                GUILayout.EndArea();
                return;
            }

            LwsWheelInputFrame frame = wheelInputSource.LastFrame;
            GUILayout.Label($"Device: {frame.deviceDisplayName}");
            GUILayout.Label($"Connection: {frame.connectionState}");
            GUILayout.Label($"Step {stepIndex + 1}/{Steps.Length}: {Steps[stepIndex].label}");
            GUILayout.Label($"Raw steering/throttle/brake/clutch: {frame.analog.rawSteering:0.000} / {frame.analog.rawThrottle:0.000} / {frame.analog.rawBrake:0.000} / {frame.analog.rawClutch:0.000}");
            GUILayout.Label($"Gate: {frame.shifter.activeGate}  Range: {frame.shifter.range}  Splitter: {frame.shifter.splitter}");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Select Wheel"))
            {
                _lastMessage = wheelInputSource.SelectPreferredDevice()
                    ? "Selected preferred wheel candidate."
                    : "No wheel candidate detected by Unity Input System.";
            }

            if (GUILayout.Button("Record Step"))
            {
                RecordCurrentStep();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Back"))
            {
                stepIndex = Mathf.Max(0, stepIndex - 1);
            }

            if (GUILayout.Button("Next"))
            {
                stepIndex = Mathf.Min(Steps.Length - 1, stepIndex + 1);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Invert Steering"))
            {
                wheelInputSource.CalibrationProfile.steering.inverted = !wheelInputSource.CalibrationProfile.steering.inverted;
            }

            if (GUILayout.Button("Save Global"))
            {
                _lastMessage = wheelInputSource.SaveCalibration().Message;
            }
            GUILayout.EndHorizontal();

            GUILayout.Label(_lastMessage);
            GUILayout.EndArea();
        }
#endif

        private void RecordCurrentStep()
        {
            Step step = Steps[stepIndex];
            LwsWheelCalibrationProfile profile = wheelInputSource.CalibrationProfile;
            if (profile == null)
            {
                _lastMessage = "No calibration profile is active.";
                return;
            }

            if (step.kind == StepKind.Button)
            {
                bool assigned = wheelInputSource.TryAssignDominantControl(step.control, LwsWheelControlKind.Button);
                _lastMessage = assigned ? $"Bound {step.control}." : $"No pressed button found for {step.control}.";
                return;
            }

            if (step.kind == StepKind.Neutral)
            {
                _lastMessage = wheelInputSource.LastFrame.shifter.neutral
                    ? "Neutral observed."
                    : "Neutral not observed; move the shifter between gates.";
                return;
            }

            LwsWheelControlBinding binding = profile.GetBinding(step.control);
            if (!binding.IsBound)
            {
                bool assigned = wheelInputSource.TryAssignDominantControl(step.control, LwsWheelControlKind.Axis);
                if (!assigned)
                {
                    _lastMessage = $"No active axis found for {step.control}.";
                    return;
                }

                binding = profile.GetBinding(step.control);
            }

            if (!wheelInputSource.TryReadRawBinding(binding, out float rawValue))
            {
                _lastMessage = $"Could not read raw value for {step.control}.";
                return;
            }

            ApplyRawSample(profile, step, rawValue);
            _lastMessage = $"Recorded {step.label}: {rawValue:0.000}";
        }

        private static void ApplyRawSample(LwsWheelCalibrationProfile profile, Step step, float rawValue)
        {
            if (step.kind == StepKind.SteeringCenter)
            {
                profile.steering.center = rawValue;
                profile.steering.minimum = rawValue;
                profile.steering.maximum = rawValue;
                return;
            }

            if (step.kind == StepKind.SteeringLeft)
            {
                profile.steering.minimum = Mathf.Min(rawValue, profile.steering.minimum);
                profile.steering.maximum = Mathf.Max(rawValue, profile.steering.maximum);
                return;
            }

            if (step.kind == StepKind.SteeringRight)
            {
                profile.steering.minimum = Mathf.Min(rawValue, profile.steering.minimum);
                profile.steering.maximum = Mathf.Max(rawValue, profile.steering.maximum);
                return;
            }

            if (step.control == LwsWheelLogicalControl.Throttle)
            {
                ApplyPedalSample(ref profile.throttle, step.kind, rawValue);
            }
            else if (step.control == LwsWheelLogicalControl.Brake)
            {
                ApplyPedalSample(ref profile.brake, step.kind, rawValue);
            }
            else if (step.control == LwsWheelLogicalControl.Clutch)
            {
                ApplyPedalSample(ref profile.clutch, step.kind, rawValue);
            }
        }

        private static void ApplyPedalSample(ref LwsWheelPedalCalibration calibration, StepKind kind, float rawValue)
        {
            if (kind == StepKind.PedalReleased)
            {
                calibration.releasedValue = rawValue;
            }
            else if (kind == StepKind.PedalPressed)
            {
                calibration.pressedValue = rawValue;
            }
        }
    }
}
