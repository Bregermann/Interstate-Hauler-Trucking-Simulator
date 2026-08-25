using UnityEngine;

namespace LWS.InterstateHauler
{
    [DisallowMultipleComponent]
    public sealed class Lws18SpeedTransmissionDebugPanel : MonoBehaviour
    {
        [SerializeField] private Lws18SpeedTransmissionController transmission;
        [SerializeField] private bool visible;
        [SerializeField] private Vector2 position = new Vector2(12f, 238f);

        private void Reset()
        {
            transmission = GetComponent<Lws18SpeedTransmissionController>();
        }

        private void Awake()
        {
            if (transmission == null)
            {
                transmission = GetComponent<Lws18SpeedTransmissionController>();
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void OnGUI()
        {
            if (!visible || transmission == null)
            {
                return;
            }

            LwsTransmissionDisplayState state = transmission.DisplayState;
            GUILayout.BeginArea(new Rect(position.x, position.y, 380f, 450f), GUI.skin.box);
            GUILayout.Label("Interstate Hauler Transmission");
            GUILayout.Label($"Transmission Mode: {state.mode.ToString().ToUpperInvariant()}");
            string buttonLabel = transmission.AutomaticModeActive
                ? "SWITCH TO 18-SPEED MANUAL"
                : "SWITCH TO AUTOMATIC";
            if (GUILayout.Button(buttonLabel, GUILayout.Height(32f)))
            {
                LwsTransmissionMode nextMode = transmission.AutomaticModeActive
                    ? LwsTransmissionMode.Truck18Speed
                    : LwsTransmissionMode.Automatic;
                if (!transmission.TrySetTransmissionMode(nextMode, out string message))
                {
                    Debug.LogWarning(message, this);
                }
            }

            if (transmission.AutomaticModeActive)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("D", GUILayout.Height(28f)) &&
                    !transmission.TrySetAutomaticSelector(LwsAutomaticTransmissionSelector.Drive, out string driveMessage))
                {
                    Debug.LogWarning(driveMessage, this);
                }

                if (GUILayout.Button("N", GUILayout.Height(28f)) &&
                    !transmission.TrySetAutomaticSelector(LwsAutomaticTransmissionSelector.Neutral, out string neutralMessage))
                {
                    Debug.LogWarning(neutralMessage, this);
                }

                if (GUILayout.Button("R", GUILayout.Height(28f)) &&
                    !transmission.TrySetAutomaticSelector(LwsAutomaticTransmissionSelector.Reverse, out string reverseMessage))
                {
                    Debug.LogWarning(reverseMessage, this);
                }

                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("SHIFT -", GUILayout.Height(28f)) &&
                    !transmission.TryShiftManualByStep(-1, out string downMessage))
                {
                    Debug.LogWarning(downMessage, this);
                }

                if (GUILayout.Button("SHIFT +", GUILayout.Height(28f)) &&
                    !transmission.TryShiftManualByStep(1, out string upMessage))
                {
                    Debug.LogWarning(upMessage, this);
                }

                GUILayout.EndHorizontal();
            }

            if (!string.IsNullOrWhiteSpace(transmission.LastModeSwitchMessage))
            {
                GUILayout.Label(transmission.LastModeSwitchMessage);
            }

            GUILayout.Label($"Physical Gate: {state.physicalGate}");
            GUILayout.Label($"Requested Range: {state.requestedRange}");
            GUILayout.Label($"Engaged Range: {state.engagedRange}");
            GUILayout.Label($"Requested Splitter: {state.requestedSplitter}");
            GUILayout.Label($"Engaged Splitter: {state.engagedSplitter}");
            GUILayout.Label($"Logical Gear: {state.displayLabel}");
            GUILayout.Label($"Logical Ratio: {state.logicalRatioIndex}/18");
            GUILayout.Label($"NWH Gear: {state.nwhGear}");
            GUILayout.Label($"Automatic Target Gear: {state.automaticTargetLabel} ({state.automaticTargetNwhGear})");
            GUILayout.Label($"Gear Ratio: {state.gearRatio:0.00}");
            GUILayout.Label($"Clutch: {state.clutch:0.00}");
            GUILayout.Label($"Engine RPM: {state.engineRpm:0}");
            GUILayout.Label($"Predicted RPM: {state.predictedTargetRpm:0}");
            GUILayout.Label($"RPM Error: {state.rpmError:0}");
            GUILayout.Label($"Shift State: {state.shiftState}");
            GUILayout.Label($"Last Rejection: {state.lastRejectionReason}");
            GUILayout.Label($"Last Abuse: {state.lastAbuseSeverity}");
            GUILayout.Label($"Shifter Sync Required: {state.requiresShifterSynchronization}");
            GUILayout.EndArea();
        }
#endif
    }
}
