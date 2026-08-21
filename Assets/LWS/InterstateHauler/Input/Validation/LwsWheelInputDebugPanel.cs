using UnityEngine;

namespace LWS.InterstateHauler
{
    [DisallowMultipleComponent]
    public sealed class LwsWheelInputDebugPanel : MonoBehaviour
    {
        [SerializeField] private LwsWheelInputSource wheelInputSource;
        [SerializeField] private LwsWheelInputBootstrap wheelBootstrap;
        [SerializeField] private LwsPlayerTruck playerTruck;
        [SerializeField] private bool visible;
        [SerializeField, Min(0.02f)] private float telemetryRefreshIntervalSeconds = 0.1f;

        private LwsVehicleTelemetry _cachedTelemetry;
        private float _nextTelemetryRefreshTime;
        private float _nextTruckLookupTime;

        private void Reset()
        {
            wheelInputSource = FindFirstObjectByType<LwsWheelInputSource>();
            wheelBootstrap = FindFirstObjectByType<LwsWheelInputBootstrap>();
            playerTruck = FindFirstObjectByType<LwsPlayerTruck>();
        }

        private void Awake()
        {
            ResolveLocalReferences();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void OnGUI()
        {
            if (!visible || wheelInputSource == null)
            {
                return;
            }

            LwsWheelInputFrame frame = wheelInputSource.LastFrame;
            RefreshTelemetryIfNeeded();

            GUILayout.BeginArea(new Rect(Screen.width - 420f, 12f, 408f, 360f), GUI.skin.box);
            GUILayout.Label("DirectInput Wheel Diagnostics");
            GUILayout.Label($"Device: {frame.deviceDisplayName}");
            GUILayout.Label($"Device Path: {frame.devicePath}");
            GUILayout.Label($"Connection: {frame.connectionState}");
            GUILayout.Label($"Profile: {wheelInputSource.CalibrationProfile?.hardwareProfileId}");
            GUILayout.Label($"Wheel Owner: {(wheelBootstrap != null && wheelBootstrap.WheelOwnsInput ? "Wheel" : "Fallback")}");
            GUILayout.Label($"Raw steering/throttle/brake/clutch: {frame.analog.rawSteering:0.000} / {frame.analog.rawThrottle:0.000} / {frame.analog.rawBrake:0.000} / {frame.analog.rawClutch:0.000}");
            GUILayout.Label($"Cal steering/throttle/brake/clutch: {frame.analog.steering:0.000} / {frame.analog.throttle:0.000} / {frame.analog.brake:0.000} / {frame.analog.clutch:0.000}");
            GUILayout.Label($"Shifter: {frame.shifter.activeGate}  Neutral: {frame.shifter.neutral}  Reverse: {frame.shifter.reverse}");
            GUILayout.Label($"Range: {frame.shifter.range} ({frame.shifter.rangePressed})  Splitter: {frame.shifter.splitter} ({frame.shifter.splitterPressed})");
            GUILayout.Label($"NWH steering/throttle/brake/clutch: {_cachedTelemetry.steeringInput:0.000} / {_cachedTelemetry.throttleInput:0.000} / {_cachedTelemetry.brakeInput:0.000} / {_cachedTelemetry.clutchInput:0.000}");
            GUILayout.Label($"NWH gear: {_cachedTelemetry.currentGearName} ({_cachedTelemetry.currentGear})");
            GUILayout.Label($"FFB Backend: {frame.forceFeedback.backendId}");
            GUILayout.Label($"FFB Device: {frame.forceFeedback.deviceDisplayName}");
            GUILayout.Label($"FFB: {(frame.forceFeedback.available ? "Available" : "Unavailable")} / {(frame.forceFeedback.deviceConnected ? "Connected" : "No Device")} / {(frame.forceFeedback.enabled ? "Enabled" : "Disabled")}");
            GUILayout.Label($"FFB strength: {frame.forceFeedback.masterStrength:0.00}");
            GUILayout.Label($"FFB forces A/D/R/I: {frame.forceFeedback.alignmentForce:0.00} / {frame.forceFeedback.dampingForce:0.00} / {frame.forceFeedback.roadForce:0.00} / {frame.forceFeedback.impactForce:0.00}");
            GUILayout.Label($"FFB Status: {frame.forceFeedback.statusMessage}");
            GUILayout.EndArea();
        }
#endif

        private void ResolveLocalReferences()
        {
            if (wheelInputSource == null)
            {
                wheelInputSource = GetComponent<LwsWheelInputSource>();
            }

            if (wheelBootstrap == null)
            {
                wheelBootstrap = GetComponent<LwsWheelInputBootstrap>();
            }
        }

        private void RefreshTelemetryIfNeeded()
        {
            float now = Time.unscaledTime;
            if (playerTruck == null && now >= _nextTruckLookupTime)
            {
                playerTruck = FindFirstObjectByType<LwsPlayerTruck>();
                _nextTruckLookupTime = now + 1f;
            }

            if (now < _nextTelemetryRefreshTime)
            {
                return;
            }

            _nextTelemetryRefreshTime = now + Mathf.Max(0.02f, telemetryRefreshIntervalSeconds);
            _cachedTelemetry = playerTruck != null && playerTruck.NwhAdapter != null
                ? playerTruck.NwhAdapter.ReadTelemetry()
                : default;
        }
    }
}
