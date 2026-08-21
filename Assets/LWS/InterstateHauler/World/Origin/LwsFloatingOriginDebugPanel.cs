using UnityEngine;

namespace LWS.InterstateHauler
{
    [DisallowMultipleComponent]
    public sealed class LwsFloatingOriginDebugPanel : MonoBehaviour
    {
        [SerializeField] private bool visible;
        [SerializeField] private Rect windowRect = new Rect(454f, 520f, 430f, 420f);
        [SerializeField] private float refreshIntervalSeconds = 0.25f;

        private ILwsWorldOriginService _originService;
        private ILwsWorldStreamingService _streamingService;
        private ILwsRoadGraphService _roadGraphService;
        private ILwsNavigationService _navigationService;
        private ILwsTrafficService _trafficService;
        private ILwsWeatherService _weatherService;
        private ILwsRoadConditionService _roadConditionService;
        private float _nextRefreshTime;
        private string _snapshot = "Floating-origin service not resolved.";

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F9))
            {
                visible = !visible;
            }

            if (Time.unscaledTime >= _nextRefreshTime)
            {
                RefreshSnapshot();
            }
        }

        private void OnGUI()
        {
            if (!visible || !Debug.isDebugBuild)
            {
                return;
            }

            windowRect = GUILayout.Window(GetInstanceID(), windowRect, DrawWindow, "LWS Floating Origin");
        }

        private void DrawWindow(int id)
        {
            GUILayout.Label(_snapshot);
            if (_originService != null)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(_originService.Enabled ? "Floating Origin Off" : "Floating Origin On"))
                {
                    _originService.SetEnabled(!_originService.Enabled);
                    RefreshSnapshot();
                }

                if (GUILayout.Button("Force Shift Now"))
                {
                    _originService.ForceShiftNow(_originService.PlayerLocalPosition, "Developer forced origin shift.", out _);
                    RefreshSnapshot();
                }

                if (GUILayout.Button("Reset Validation Origin"))
                {
                    _originService.ResetValidationOrigin();
                    RefreshSnapshot();
                }

                GUILayout.EndHorizontal();
            }

            GUI.DragWindow();
        }

        private void RefreshSnapshot()
        {
            _nextRefreshTime = Time.unscaledTime + Mathf.Max(0.1f, refreshIntervalSeconds);
            ResolveServices();
            if (_originService == null)
            {
                _snapshot = "Floating-origin service not resolved.";
                return;
            }

            Vector3 local = _originService.PlayerLocalPosition;
            LwsWorldPositionD global = _originService.PlayerGlobalPosition;
            LwsWorldPositionD offset = _originService.CurrentOriginOffset;
            LwsOriginShiftEvent last = _originService.LastShiftEvent;
            _snapshot =
                "ORIGIN\n" +
                $"Enabled: {_originService.Enabled}  Shift In Progress: {_originService.ShiftInProgress}\n" +
                $"Origin Version: {_originService.OriginVersion}  Shift Count: {_originService.ShiftCount}\n\n" +
                "LOCAL\n" +
                $"Player Local: {local.x:0.0}, {local.y:0.0}, {local.z:0.0}  Dist: {new Vector2(local.x, local.z).magnitude:0.0}m\n\n" +
                "GLOBAL\n" +
                $"Player Global: {global.x:0.###}, {global.y:0.###}, {global.z:0.###}\n\n" +
                "OFFSET\n" +
                $"Origin Offset: {offset.x:0.###}, {offset.y:0.###}, {offset.z:0.###}\n\n" +
                "LAST SHIFT\n" +
                $"Delta: {last.GlobalShiftDelta.x:0.0}, {last.GlobalShiftDelta.y:0.0}, {last.GlobalShiftDelta.z:0.0}\n" +
                $"Physics Preserved: True  Participants: {_originService.LastParticipantsShifted}\n" +
                $"Chunk Roots: {_originService.LastChunkRootsShifted}  NPCs: {_originService.LastTrafficParticipantsShifted}\n" +
                $"Duration: {_originService.LastShiftDurationMilliseconds:0.000} ms  Last Error: {_originService.LastError}\n\n" +
                "SERVICES\n" +
                $"Streaming: {_streamingService != null}  Road Graph: {_roadGraphService != null}  Navigation: {_navigationService != null}\n" +
                $"Traffic: {_trafficService != null}  Weather: {_weatherService != null}  Road Conditions: {_roadConditionService != null}";
        }

        private void ResolveServices()
        {
            if (LwsApplicationBootstrap.Instance == null || LwsApplicationBootstrap.Instance.Registry == null)
            {
                return;
            }

            LwsServiceRegistry registry = LwsApplicationBootstrap.Instance.Registry;
            registry.TryGet(out _originService);
            registry.TryGet(out _streamingService);
            registry.TryGet(out _roadGraphService);
            registry.TryGet(out _navigationService);
            registry.TryGet(out _trafficService);
            registry.TryGet(out _weatherService);
            registry.TryGet(out _roadConditionService);
        }
    }
}
