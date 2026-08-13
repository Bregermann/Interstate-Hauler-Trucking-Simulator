using UnityEngine;

namespace LWS.InterstateHauler
{
    [DisallowMultipleComponent]
    public sealed class LwsNavigationDebugPanel : MonoBehaviour
    {
        [SerializeField] private LwsRoadGraphProvider roadGraphProvider;
        [SerializeField] private bool visible = true;
        [SerializeField] private Rect panelRect = new Rect(12f, 520f, 360f, 300f);

        private ILwsNavigationService _navigationService;
        private ILwsPlayerSettingsService _settingsService;
        private ILwsGpsVoiceGuidanceService _voiceService;
        private float _nextServiceResolveTime;

        private void Awake()
        {
            if (roadGraphProvider == null)
            {
                roadGraphProvider = GetComponent<LwsRoadGraphProvider>();
            }
        }

        private void Update()
        {
            if (Time.unscaledTime >= _nextServiceResolveTime)
            {
                _nextServiceResolveTime = Time.unscaledTime + 1f;
                ResolveServices();
            }
        }

        private void OnGUI()
        {
            if (!visible)
            {
                return;
            }

            panelRect = GUILayout.Window(GetInstanceID(), panelRect, DrawWindow, "LWS GPS / Navigation");
        }

        private void DrawWindow(int id)
        {
            ResolveServices();
            LwsNavigationRuntimeState state = _navigationService != null ? _navigationService.RuntimeState : null;

            GUILayout.Label($"Route Active: {state?.routeActive ?? false}");
            GUILayout.Label($"Status: {state?.status.ToString() ?? "Unavailable"}");
            GUILayout.Label($"Current Road: {state?.currentRoadDisplayName ?? "Unknown"}");
            GUILayout.Label($"Current Edge: {state?.currentEdgeId ?? "-"}");
            GUILayout.Label($"Step: {(state != null ? state.currentStepIndex + 1 : 0)}/{(state != null ? state.totalSteps : 0)}");
            GUILayout.Label($"Next: {state?.nextManeuver.ToString() ?? "-"}");
            GUILayout.Label($"Instruction: {state?.nextInstructionText ?? "-"}");
            GUILayout.Label($"To Maneuver: {FormatMeters(state?.distanceToNextManeuverMeters ?? 0f)}");
            GUILayout.Label($"Remaining: {FormatMeters(state?.distanceRemainingMeters ?? 0f)}");
            GUILayout.Label($"ETA: {FormatEta(state?.estimatedTimeRemainingSeconds ?? 0f)}");
            GUILayout.Label($"Off Route: {state?.offRoute ?? false}  Recalc: {state?.recalculating ?? false}");
            GUILayout.Space(4f);
            GUILayout.Label($"Voice Enabled: {_settingsService?.GpsVoiceGuidanceEnabled ?? true}");
            GUILayout.Label($"Voice Pack: {_voiceService?.VoicePack?.DisplayName ?? "None assigned"}");
            GUILayout.Label($"Last Voice: {_voiceService?.LastInstruction ?? "-"}");
            GUILayout.Label($"Last Clip: {_voiceService?.LastClipName ?? "-"}");
            GUILayout.Label($"Voice Playing: {_voiceService?.VoicePlaying ?? false}");
            GUILayout.Space(4f);

            if (GUILayout.Button("START TEST ROUTE"))
            {
                StartTestRoute();
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("CLEAR ROUTE"))
            {
                _navigationService?.ClearRoute();
            }

            if (GUILayout.Button("RECALCULATE"))
            {
                Vector3 origin = ResolvePlayerPosition();
                _navigationService?.RecalculateRoute(origin);
            }
            GUILayout.EndHorizontal();

            bool voiceEnabled = _settingsService?.GpsVoiceGuidanceEnabled ?? true;
            if (GUILayout.Button(voiceEnabled ? "GPS VOICE: ON" : "GPS VOICE: OFF"))
            {
                _settingsService?.SetGpsVoiceGuidanceEnabled(!voiceEnabled);
            }

            GUI.DragWindow();
        }

        private void StartTestRoute()
        {
            ResolveServices();
            LwsRoadGraph graph = roadGraphProvider != null ? roadGraphProvider.Graph : null;
            if (graph == null && LwsApplicationBootstrap.Instance != null &&
                LwsApplicationBootstrap.Instance.Registry != null &&
                LwsApplicationBootstrap.Instance.Registry.TryGet(out ILwsRoadGraphService roadGraphService))
            {
                graph = roadGraphService.ActiveGraph;
            }

            if (_navigationService == null || graph == null)
            {
                Debug.LogWarning("Cannot start GPS test route because navigation service or road graph is missing.", this);
                return;
            }

            var request = new LwsRouteRequest
            {
                requestId = "IH_TEST_ROUTE_011_RAMP_TO_RETURN",
                originNodeId = "IH_TEST_I000_NB_ENTRY_RAMP_START",
                destinationNodeId = "IH_TEST_I000_SB_MAIN_END",
                destinationId = "IH_TEST_DEST_SOUTHBOUND_RETURN",
                truckRouteRequired = true
            };

            LwsRouteResult result = _navigationService.RequestRoute(request, graph);
            if (!result.succeeded)
            {
                Debug.LogWarning(result.message, this);
            }
        }

        private Vector3 ResolvePlayerPosition()
        {
            if (LwsApplicationBootstrap.Instance != null &&
                LwsApplicationBootstrap.Instance.Registry != null &&
                LwsApplicationBootstrap.Instance.Registry.TryGet(out ILwsPlayerVehicleService playerVehicleService) &&
                playerVehicleService.ActiveTruck != null)
            {
                return playerVehicleService.ActiveTruck.transform.position;
            }

            return transform.position;
        }

        private void ResolveServices()
        {
            if (LwsApplicationBootstrap.Instance == null || LwsApplicationBootstrap.Instance.Registry == null)
            {
                return;
            }

            LwsApplicationBootstrap.Instance.Registry.TryGet(out _navigationService);
            LwsApplicationBootstrap.Instance.Registry.TryGet(out _settingsService);
            LwsApplicationBootstrap.Instance.Registry.TryGet(out _voiceService);
        }

        private static string FormatMeters(float meters)
        {
            return meters >= 1609.344f ? $"{meters / 1609.344f:0.0} mi" : $"{meters:0} m";
        }

        private static string FormatEta(float seconds)
        {
            if (seconds <= 0f)
            {
                return "--";
            }

            int minutes = Mathf.CeilToInt(seconds / 60f);
            return $"{minutes} min";
        }
    }
}
