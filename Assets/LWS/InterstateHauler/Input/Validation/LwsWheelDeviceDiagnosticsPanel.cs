using System.Collections.Generic;
using UnityEngine;

namespace LWS.InterstateHauler
{
    [DisallowMultipleComponent]
    public sealed class LwsWheelDeviceDiagnosticsPanel : MonoBehaviour
    {
        [SerializeField] private bool visible = true;
        [SerializeField] private int maxControlsPerDevice = 6;
        [SerializeField] private bool autoRefresh;
        [SerializeField, Min(0.25f)] private float refreshIntervalSeconds = 2f;

        private readonly List<LwsWheelDeviceDescriptor> _cachedCandidates = new List<LwsWheelDeviceDescriptor>();
        private float _nextRefreshTime;
        private string _lastRefreshMessage = "Device diagnostics have not refreshed yet.";

        private void OnEnable()
        {
            RefreshCandidates();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void OnGUI()
        {
            if (!visible)
            {
                return;
            }

            if (autoRefresh && Time.unscaledTime >= _nextRefreshTime)
            {
                RefreshCandidates();
            }

            GUILayout.BeginArea(new Rect(444f, 244f, 520f, 360f), GUI.skin.box);
            GUILayout.Label("Wheel Device Discovery");
            GUILayout.Label(_lastRefreshMessage);
            if (GUILayout.Button("Refresh Devices"))
            {
                RefreshCandidates();
            }

            GUILayout.Label($"Candidates: {_cachedCandidates.Count}");
            for (int i = 0; i < _cachedCandidates.Count; i++)
            {
                LwsWheelDeviceDescriptor device = _cachedCandidates[i];
                GUILayout.Label($"{i + 1}. {device.displayName} | {device.manufacturer} | {device.product}");
                GUILayout.Label($"Class/Layout/Path: {device.deviceClass} / {device.layout} / {device.path}");
                GUILayout.Label($"DirectInput GUID: {device.directInputGuid}");
                GUILayout.Label($"DirectInput FFB: {device.directInputForceFeedbackCapable}  Effects: {Truncate(device.directInputSupportedEffects, 96)}");
                GUILayout.Label($"Axes: {device.axes.Count}  Buttons: {device.buttons.Count}  D-pads: {device.dpads.Count}");
                GUILayout.Label($"Capabilities: {Truncate(device.capabilities, 96)}");
                for (int axis = 0; axis < Mathf.Min(maxControlsPerDevice, device.axes.Count); axis++)
                {
                    GUILayout.Label($"A{axis}: {device.axes[axis]}");
                }
                for (int button = 0; button < Mathf.Min(maxControlsPerDevice, device.buttons.Count); button++)
                {
                    GUILayout.Label($"B{button}: {device.buttons[button]}");
                }
            }

            GUILayout.EndArea();
        }
#endif

        private void RefreshCandidates()
        {
            _cachedCandidates.Clear();
            _cachedCandidates.AddRange(LwsWheelDeviceDiscovery.GetConnectedWheelCandidates());
            _nextRefreshTime = Time.unscaledTime + Mathf.Max(0.25f, refreshIntervalSeconds);
            _lastRefreshMessage = $"Last refresh: {Time.realtimeSinceStartup:0.0}s";
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            {
                return value ?? string.Empty;
            }

            return value.Substring(0, maxLength);
        }
    }
}
