using UnityEngine;

namespace LWS.InterstateHauler
{
    [DisallowMultipleComponent]
    public sealed class LwsGpsSettingsPanel : MonoBehaviour
    {
        [SerializeField] private bool visible;
        [SerializeField] private Rect panelRect = new Rect(390f, 520f, 260f, 120f);

        private ILwsPlayerSettingsService _settingsService;

        private void OnGUI()
        {
            if (!visible)
            {
                return;
            }

            panelRect = GUILayout.Window(GetInstanceID(), panelRect, DrawWindow, "Settings - Navigation");
        }

        private void DrawWindow(int id)
        {
            ResolveService();
            bool enabled = _settingsService == null || _settingsService.GpsVoiceGuidanceEnabled;
            GUILayout.Label("GPS Voice Guidance");
            if (GUILayout.Button(enabled ? "ON" : "OFF", GUILayout.Height(30f)))
            {
                _settingsService?.SetGpsVoiceGuidanceEnabled(!enabled);
            }

            GUILayout.Label("Visual GPS remains active.");
            GUI.DragWindow();
        }

        private void ResolveService()
        {
            if (_settingsService != null ||
                LwsApplicationBootstrap.Instance == null ||
                LwsApplicationBootstrap.Instance.Registry == null)
            {
                return;
            }

            LwsApplicationBootstrap.Instance.Registry.TryGet(out _settingsService);
        }
    }
}
