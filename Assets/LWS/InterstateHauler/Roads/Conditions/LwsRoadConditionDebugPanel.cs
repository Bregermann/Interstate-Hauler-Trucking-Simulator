using UnityEngine;

namespace LWS.InterstateHauler
{
    [DisallowMultipleComponent]
    public sealed class LwsRoadConditionDebugPanel : MonoBehaviour
    {
        [SerializeField] private bool visible;
        [SerializeField] private Rect panelRect = new Rect(12f, 670f, 390f, 430f);

        private ILwsRoadConditionService _roadConditionService;
        private ILwsPlayerVehicleService _playerVehicleService;
        private float _nextRefreshTime;
        private bool _measuringBrakeDistance;
        private Vector3 _brakeStartPosition;
        private float _brakeStartSpeed;
        private float _lastBrakeDistanceMeters;
        private string _measurementStatus = "Not measured.";

        private void Update()
        {
            if (Time.time >= _nextRefreshTime)
            {
                RefreshCache();
            }

            UpdateBrakingMeasurement();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void OnGUI()
        {
            if (!visible)
            {
                return;
            }

            RefreshCache();
            panelRect = GUILayout.Window(GetInstanceID(), panelRect, DrawWindow, "IH Road Conditions");
        }
#endif

        private void DrawWindow(int windowId)
        {
            if (_roadConditionService == null)
            {
                GUILayout.Label("Road condition service: missing");
                GUI.DragWindow();
                return;
            }

            LwsRoadConditionSnapshot snapshot = _roadConditionService.CurrentSnapshot;
            LwsVehicleTelemetry telemetry = ReadTelemetry();

            GUILayout.Label($"Mode: {_roadConditionService.Mode}");
            GUILayout.Label($"Road: {snapshot.roadId} / {snapshot.edgeId}");
            GUILayout.Label($"Condition: {snapshot.condition} ({snapshot.MajorGameplayState})");
            GUILayout.Label($"Temp: {snapshot.surfaceTemperatureC:0.0} C  Speed: {telemetry.speedMetersPerSecond * 2.236936f:0.0} MPH");
            GUILayout.Label($"Wet {snapshot.wetness01:0.00}  Water {snapshot.standingWater01:0.00}  Snow {snapshot.snowDepth01:0.00}  Packed {snapshot.packedSnow01:0.00}  Ice {snapshot.ice01:0.00}");
            GUILayout.Label($"Grip Lng {snapshot.longitudinalGripMultiplier01:0.00}  Lat {snapshot.lateralGripMultiplier01:0.00}  Brake {snapshot.brakingGripMultiplier01:0.00}  Hydro {snapshot.hydroplaningRisk01:0.00}");

            GUILayout.Space(4f);
            if (GUILayout.Button("AUTO FROM WEATHER"))
            {
                _roadConditionService.SetAutoFromWeather();
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("FORCE DRY")) _roadConditionService.ForceCondition(LwsRoadConditionOverrideMode.ForceDry);
            if (GUILayout.Button("FORCE WET")) _roadConditionService.ForceCondition(LwsRoadConditionOverrideMode.ForceWet);
            if (GUILayout.Button("FORCE WATER")) _roadConditionService.ForceCondition(LwsRoadConditionOverrideMode.ForceStandingWater);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("FORCE SNOW")) _roadConditionService.ForceCondition(LwsRoadConditionOverrideMode.ForceSnow);
            if (GUILayout.Button("FORCE PACKED")) _roadConditionService.ForceCondition(LwsRoadConditionOverrideMode.ForcePackedSnow);
            if (GUILayout.Button("FORCE ICE")) _roadConditionService.ForceCondition(LwsRoadConditionOverrideMode.ForceIce);
            GUILayout.EndHorizontal();

            if (GUILayout.Button("RESET ROAD CONDITION"))
            {
                _roadConditionService.ResetCurrentRoad();
            }

            GUILayout.Space(4f);
            GUILayout.Label($"Accumulation speed: {_roadConditionService.AccumulationSpeedMultiplier:0.##}x");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("1x")) _roadConditionService.SetAccumulationSpeedMultiplier(1f);
            if (GUILayout.Button("10x")) _roadConditionService.SetAccumulationSpeedMultiplier(10f);
            if (GUILayout.Button("60x")) _roadConditionService.SetAccumulationSpeedMultiplier(60f);
            GUILayout.EndHorizontal();

            GUILayout.Label(_roadConditionService.TemperatureOverrideEnabled
                ? $"Temp override: {_roadConditionService.SurfaceTemperatureOverrideC:0.0} C"
                : "Temp override: off");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+10 C")) _roadConditionService.SetSurfaceTemperatureOverride(true, 10f);
            if (GUILayout.Button("0 C")) _roadConditionService.SetSurfaceTemperatureOverride(true, 0f);
            if (GUILayout.Button("-5 C")) _roadConditionService.SetSurfaceTemperatureOverride(true, -5f);
            if (GUILayout.Button("OFF")) _roadConditionService.SetSurfaceTemperatureOverride(false, 0f);
            GUILayout.EndHorizontal();

            GUILayout.Space(4f);
            GUILayout.Label($"Brake input: {telemetry.brakeInput:0.00}");
            GUILayout.Label($"Stopping distance: {_measurementStatus}");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(_measuringBrakeDistance ? "MEASURING" : "START BRAKE MEASURE"))
            {
                StartBrakingMeasurement(telemetry);
            }

            if (GUILayout.Button("CLEAR MEASURE"))
            {
                _measuringBrakeDistance = false;
                _lastBrakeDistanceMeters = 0f;
                _measurementStatus = "Not measured.";
            }

            GUILayout.EndHorizontal();

            GUI.DragWindow();
        }

        private void RefreshCache()
        {
            _nextRefreshTime = Time.time + 0.5f;
            if (LwsApplicationBootstrap.Instance == null || LwsApplicationBootstrap.Instance.Registry == null)
            {
                return;
            }

            LwsApplicationBootstrap.Instance.Registry.TryGet(out _roadConditionService);
            LwsApplicationBootstrap.Instance.Registry.TryGet(out _playerVehicleService);
        }

        private LwsVehicleTelemetry ReadTelemetry()
        {
            LwsPlayerTruck truck = _playerVehicleService != null ? _playerVehicleService.ActiveTruck : null;
            if (truck == null)
            {
                return default;
            }

            return truck.NwhAdapter != null ? truck.NwhAdapter.ReadTelemetry() : truck.LastTelemetry;
        }

        private void StartBrakingMeasurement(LwsVehicleTelemetry telemetry)
        {
            LwsPlayerTruck truck = _playerVehicleService != null ? _playerVehicleService.ActiveTruck : null;
            if (truck == null)
            {
                _measurementStatus = "No player truck.";
                return;
            }

            _measuringBrakeDistance = true;
            _brakeStartPosition = truck.transform.position;
            _brakeStartSpeed = telemetry.speedMetersPerSecond;
            _lastBrakeDistanceMeters = 0f;
            _measurementStatus = $"Started at {_brakeStartSpeed * 2.236936f:0.0} MPH.";
        }

        private void UpdateBrakingMeasurement()
        {
            if (!_measuringBrakeDistance)
            {
                return;
            }

            LwsPlayerTruck truck = _playerVehicleService != null ? _playerVehicleService.ActiveTruck : null;
            if (truck == null)
            {
                _measuringBrakeDistance = false;
                _measurementStatus = "Cancelled: no player truck.";
                return;
            }

            LwsVehicleTelemetry telemetry = ReadTelemetry();
            _lastBrakeDistanceMeters = Vector3.Distance(_brakeStartPosition, truck.transform.position);
            _measurementStatus = $"{_lastBrakeDistanceMeters:0.0} m from {_brakeStartSpeed * 2.236936f:0.0} MPH.";

            if (telemetry.speedMetersPerSecond <= 0.4f && _brakeStartSpeed > 2f)
            {
                _measuringBrakeDistance = false;
                _measurementStatus = $"Stopped in {_lastBrakeDistanceMeters:0.0} m from {_brakeStartSpeed * 2.236936f:0.0} MPH.";
            }
        }
    }
}
