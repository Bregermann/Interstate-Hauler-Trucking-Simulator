using UnityEngine;

namespace LWS.InterstateHauler
{
    [DisallowMultipleComponent]
    public sealed class LwsTruckControlDebugPanel : MonoBehaviour
    {
        [SerializeField] private LwsTruckControlController controller;
        [SerializeField] private bool visible;
        [SerializeField] private Rect panelRect = new Rect(12f, 468f, 380f, 420f);

        private void Reset()
        {
            controller = GetComponent<LwsTruckControlController>();
        }

        private void Awake()
        {
            if (controller == null)
            {
                controller = GetComponent<LwsTruckControlController>();
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void OnGUI()
        {
            if (!visible || controller == null)
            {
                return;
            }

            LwsTruckControlState state = controller.CurrentState;
            GUILayout.BeginArea(panelRect, GUI.skin.box);
            GUILayout.Label("Interstate Hauler Truck Controls");
            GUILayout.Label($"Input: {state.inputOwner} / {state.inputSourceId}");
            GUILayout.Label($"Ignition: {state.ignitionState}  Engine: {(state.engineRunning ? "Running" : "Stopped")}");
            GUILayout.Label($"Parking: {state.parkingBrakeOn}  Service Brake: {state.serviceBrakeInput:0.00}");
            GUILayout.Label($"Trailer Brake: {state.trailerBrakeHeld}  Engine Brake: {state.engineBrakeLevel}  Retarder: {state.retarderLevel}");
            GUILayout.Label($"Lights: Low {state.headlightsOn}  High {state.highBeamsOn}");
            GUILayout.Label($"Signals: {state.turnSignal}  Hazards: {state.hazardsOn}");
            GUILayout.Label($"Wipers: {state.wiperState}  Horn: {state.hornActive}  Air Horn: {state.airHornActive}");
            GUILayout.Label($"Diff Lock: {state.differentialLocked}");
            GUILayout.Label($"Cruise: {state.cruiseEnabled}  Target: {state.cruiseTargetSpeedMetersPerSecond:0.0} m/s");
            GUILayout.Label($"Cruise Output: T {state.cruiseThrottleOutput:0.00}  B {state.cruiseBrakeOutput:0.00}");
            GUILayout.Label($"Trailer: {(state.trailerAttached ? state.trailerId : "Detached")}");
            GUILayout.Label($"Transmission: {state.transmission.displayLabel}  {state.transmission.shiftState}");
            GUILayout.Label($"Gesture: {state.lastGesture}  Target: {(state.lastGestureHadTarget ? state.lastGestureTargetId : "None")}");
            GUILayout.Label($"Gesture Distance: {state.lastGestureTargetDistance:0.0} m  Cooldown: {state.gestureCooldownRemaining:0.0}s");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Horn"))
            {
                controller.ApplyCommandFrame(new LwsVehicleCommandFrame { horn = LwsMomentaryIntent.Pressed }, default);
            }

            if (GUILayout.Button("Flip Off"))
            {
                controller.ApplyCommandFrame(new LwsVehicleCommandFrame { flipOffDriver = LwsMomentaryIntent.Pressed }, default);
            }

            if (GUILayout.Button("Camera"))
            {
                controller.ApplyCommandFrame(new LwsVehicleCommandFrame { cameraCycle = LwsMomentaryIntent.Pressed }, default);
            }
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }
#endif
    }
}
