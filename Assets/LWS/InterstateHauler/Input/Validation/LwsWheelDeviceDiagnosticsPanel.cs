using System.Collections.Generic;
using UnityEngine;

namespace LWS.InterstateHauler
{
    [DisallowMultipleComponent]
    public sealed class LwsWheelDeviceDiagnosticsPanel : MonoBehaviour
    {
        [SerializeField] private bool visible = true;
        [SerializeField] private int maxControlsPerDevice = 6;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void OnGUI()
        {
            if (!visible)
            {
                return;
            }

            List<LwsWheelDeviceDescriptor> candidates = LwsWheelDeviceDiscovery.GetConnectedWheelCandidates();
            GUILayout.BeginArea(new Rect(444f, 244f, 520f, 360f), GUI.skin.box);
            GUILayout.Label("Wheel Device Discovery");
            GUILayout.Label($"Candidates: {candidates.Count}");
            for (int i = 0; i < candidates.Count; i++)
            {
                LwsWheelDeviceDescriptor device = candidates[i];
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
