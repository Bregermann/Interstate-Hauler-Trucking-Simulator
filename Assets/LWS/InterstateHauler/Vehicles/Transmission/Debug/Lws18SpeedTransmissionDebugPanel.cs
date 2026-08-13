using UnityEngine;

namespace LWS.InterstateHauler
{
    [DisallowMultipleComponent]
    public sealed class Lws18SpeedTransmissionDebugPanel : MonoBehaviour
    {
        [SerializeField] private Lws18SpeedTransmissionController transmission;
        [SerializeField] private bool visible = true;
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
            GUILayout.Label("Interstate Hauler 18-Speed");
            GUILayout.Label($"Transmission Mode: {state.mode}");
            GUILayout.Label(transmission.DevelopmentAutomaticModeActive ? "TEST AUTOMATIC: ON" : "TEST AUTOMATIC: OFF");
            string buttonLabel = transmission.DevelopmentAutomaticModeActive
                ? "RETURN TO 18-SPEED MANUAL"
                : "ENABLE TEST AUTOMATIC";
            if (GUILayout.Button(buttonLabel, GUILayout.Height(32f)))
            {
                bool enableAutomatic = !transmission.DevelopmentAutomaticModeActive;
                if (!transmission.TrySetDevelopmentAutomaticTestMode(enableAutomatic, out string message))
                {
                    Debug.LogWarning(message, this);
                }
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
