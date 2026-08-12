using UnityEngine;

namespace LWS.InterstateHauler
{
    [DisallowMultipleComponent]
    public sealed class LwsPlayerTruckDebugPanel : MonoBehaviour
    {
        [SerializeField] private LwsPlayerTruck playerTruck;
        [SerializeField] private bool visible = true;

        private void Reset()
        {
            playerTruck = GetComponent<LwsPlayerTruck>();
        }

        private void Awake()
        {
            if (playerTruck == null)
            {
                playerTruck = GetComponent<LwsPlayerTruck>();
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void OnGUI()
        {
            if (!visible || playerTruck == null)
            {
                return;
            }

            LwsVehicleTelemetry telemetry = playerTruck.NwhAdapter != null
                ? playerTruck.NwhAdapter.ReadTelemetry()
                : playerTruck.LastTelemetry;

            GUILayout.BeginArea(new Rect(12f, 12f, 340f, 220f), GUI.skin.box);
            GUILayout.Label("Interstate Hauler Truck Validation");
            GUILayout.Label($"Truck: {telemetry.vehicleId}");
            GUILayout.Label($"Speed: {telemetry.signedSpeedMetersPerSecond:0.0} m/s");
            GUILayout.Label($"RPM: {telemetry.engineRpm:0}");
            GUILayout.Label($"Gear: {telemetry.currentGearName} ({telemetry.currentGear})");
            GUILayout.Label($"Engine: {(telemetry.engineRunning ? "Running" : "Stopped")}");
            GUILayout.Label($"Steer: {telemetry.steeringInput:0.00}");
            GUILayout.Label($"Throttle: {telemetry.throttleInput:0.00}");
            GUILayout.Label($"Brake: {telemetry.brakeInput:0.00}");
            GUILayout.Label($"Clutch: {telemetry.clutchInput:0.00}");
            GUILayout.Label($"Trailer: {(telemetry.trailerAttached ? telemetry.trailerId : "Detached")}");
            GUILayout.EndArea();
        }
#endif
    }
}
