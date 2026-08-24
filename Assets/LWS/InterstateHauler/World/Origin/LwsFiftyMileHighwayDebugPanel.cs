using System.Linq;
using UnityEngine;

namespace LWS.InterstateHauler
{
    [DisallowMultipleComponent]
    public sealed class LwsFiftyMileHighwayDebugPanel : MonoBehaviour
    {
        [SerializeField] private bool visible;
        [SerializeField] private Rect windowRect = new Rect(460f, 12f, 470f, 620f);
        [SerializeField] private float refreshIntervalSeconds = 0.35f;

        private LwsFiftyMileHighwayValidationController _controller;
        private string _summary = "50-mile validation debug panel waiting for controller.";
        private float _nextRefreshTime;

        private void Awake()
        {
            _controller = GetComponent<LwsFiftyMileHighwayValidationController>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F10))
            {
                visible = !visible;
            }

            if (Time.unscaledTime >= _nextRefreshTime)
            {
                _nextRefreshTime = Time.unscaledTime + Mathf.Max(0.05f, refreshIntervalSeconds);
                RefreshSummary();
            }
        }

        private void OnGUI()
        {
            if (!visible)
            {
                return;
            }

            windowRect = GUILayout.Window(GetInstanceID(), windowRect, DrawWindow, "IH 50-Mile Floating-Origin Test");
        }

        private void DrawWindow(int id)
        {
            GUILayout.Label(_summary);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Mile 0"))
            {
                _controller?.TeleportToMile(0d);
                RefreshSummary();
            }

            if (GUILayout.Button("Mile 10"))
            {
                _controller?.TeleportToMile(10d);
                RefreshSummary();
            }

            if (GUILayout.Button("Mile 25"))
            {
                _controller?.TeleportToMile(25d);
                RefreshSummary();
            }

            if (GUILayout.Button("Mile 40"))
            {
                _controller?.TeleportToMile(40d);
                RefreshSummary();
            }

            if (GUILayout.Button("Mile 49"))
            {
                _controller?.TeleportToMile(49d);
                RefreshSummary();
            }

            GUILayout.EndHorizontal();
            GUI.DragWindow();
        }

        private void RefreshSummary()
        {
            if (_controller == null)
            {
                _controller = GetComponent<LwsFiftyMileHighwayValidationController>();
            }

            if (_controller == null)
            {
                _summary = "50-mile validation controller is missing.";
                return;
            }

            ILwsWorldOriginService origin = _controller.OriginService;
            ILwsWorldStreamingService streaming = _controller.StreamingService;
            ILwsNavigationService navigation = _controller.NavigationService;
            ILwsWeatherService weather = _controller.WeatherService;
            ILwsRoadConditionService road = _controller.RoadConditionService;
            LwsUtsHighwayTrafficController trafficController = _controller.TrafficController;
            LwsTrafficRuntimeStats traffic = trafficController != null ? trafficController.Stats : default;
            int loadedChunks = streaming != null ? streaming.ChunkStates.Count(s => s.IsLoaded) : 0;
            LwsWorldPositionD global = origin != null ? origin.PlayerGlobalPosition : LwsWorldPositionD.Zero;
            Vector3 local = origin != null ? origin.PlayerLocalPosition : Vector3.zero;
            LwsRoadConditionSnapshot roadSnapshot = road != null ? road.CurrentSnapshot : default;
            LwsNavigationRuntimeState navState = navigation != null ? navigation.RuntimeState : null;
            LwsWeatherSnapshot weatherSnapshot = weather != null ? weather.CurrentSnapshot : LwsWeatherSnapshot.Clear;

            _summary =
                "50-MILE TEST\n" +
                $"Distance Driven: {_controller.CurrentMile:0.00} mi\n" +
                $"Distance Remaining: {_controller.DistanceRemainingMeters / LwsFiftyMileHighwayModel.MetersPerMile:0.00} mi\n" +
                $"Current Mile: {_controller.CurrentMile:0.00}\n" +
                $"Current Chunk: {_controller.CurrentChunkId}\n" +
                $"Loaded Chunks: {loadedChunks}\n\n" +
                "GLOBAL POSITION\n" +
                $"X: {global.x:0.0}  Y: {global.y:0.0}  Z: {global.z:0.0}\n\n" +
                "LOCAL POSITION\n" +
                $"X: {local.x:0.0}  Y: {local.y:0.0}  Z: {local.z:0.0}\n\n" +
                "FLOATING ORIGIN\n" +
                $"Origin Offset: {(origin != null ? origin.CurrentOriginOffset.ToString() : "n/a")}\n" +
                $"Shift Count: {(origin != null ? origin.ShiftCount : 0)}\n" +
                $"Last Shift: {(origin != null ? origin.LastShiftEvent.GlobalShiftDelta.ToString() : "n/a")}\n" +
                $"Origin Version: {(origin != null ? origin.OriginVersion : 0)}\n\n" +
                "STREAMING\n" +
                $"Current Chunk: {(streaming != null ? streaming.ActiveWorldChunkId : "n/a")}\n" +
                $"Meters Road Ahead: {_controller.MetersRoadAheadAvailable:0.0} m\n" +
                $"Chunk Loads: {_controller.ChunkLoadCount}  Unloads: {_controller.ChunkUnloadCount}\n" +
                $"Streaming Failures: {_controller.StreamingFailureCount}\n" +
                $"Streaming Error: {(streaming != null && !string.IsNullOrWhiteSpace(streaming.LastError) ? streaming.LastError : "None")}\n\n" +
                "TRAFFIC\n" +
                $"Active Vehicles: {traffic.ActiveVehicles}\n" +
                $"Total Spawned: {traffic.TotalSpawned}\n" +
                $"Lanes: {traffic.Lanes}\n" +
                $"Last: {traffic.LastMessage}\n\n" +
                "WEATHER\n" +
                $"Preset: {weatherSnapshot.weatherPresetId}\n" +
                $"Weather Maker Active: {(weather != null && weather.WeatherMakerAvailable)}\n" +
                $"Precipitation: {weatherSnapshot.precipitationType} {weatherSnapshot.precipitationIntensity01:0.00}\n\n" +
                "ROAD CONDITION\n" +
                $"Condition: {roadSnapshot.condition}\n" +
                $"Wetness: {roadSnapshot.wetness01:0.00}  Snow: {roadSnapshot.snowDepth01:0.00}  Ice: {roadSnapshot.ice01:0.00}\n\n" +
                "GPS\n" +
                $"Route Active: {(navState != null && navState.routeActive)}\n" +
                $"Remaining Miles: {(navState != null ? navState.distanceRemainingMeters / (float)LwsFiftyMileHighwayModel.MetersPerMile : 0f):0.00}\n" +
                $"Current Road: {(navState != null ? navState.currentRoadId : "n/a")}\n\n" +
                "PERFORMANCE\n" +
                $"Last Chunk Load: {(streaming != null ? streaming.LastLoadDurationSeconds : 0f):0.000}s\n" +
                $"Last Origin Shift Duration: {(origin != null ? origin.LastShiftDurationMilliseconds : 0d):0.000} ms\n" +
                $"Max Local Distance: {_controller.MaxLocalDistanceMeters:0.0} m\n" +
                $"Last Error: {(string.IsNullOrWhiteSpace(_controller.LastError) ? "None" : _controller.LastError)}\n\n" +
                _controller.LastReport;
        }
    }
}
