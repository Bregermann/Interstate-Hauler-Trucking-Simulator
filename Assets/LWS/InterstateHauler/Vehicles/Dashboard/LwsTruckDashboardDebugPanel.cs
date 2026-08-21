using UnityEngine;

namespace LWS.InterstateHauler
{
    [DisallowMultipleComponent]
    public sealed class LwsTruckDashboardDebugPanel : MonoBehaviour
    {
        [SerializeField] private bool visible;
        [SerializeField] private LwsTruckDashboardController dashboard;
        [SerializeField] private LwsTruckMirrorController mirrors;
        [SerializeField] private LwsCabAccessoryAnchorRegistry anchors;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnGUI()
        {
            if (!visible)
            {
                return;
            }

            ResolveReferences();
            GUILayout.BeginArea(new Rect(12f, 540f, 360f, 430f), "IH Dashboard / Mirrors / Cab", GUI.skin.window);
            if (dashboard != null)
            {
                LwsTruckDashboardSnapshot state = dashboard.CurrentSnapshot;
                GUILayout.Label($"Speed: {state.rawSpeedMetersPerSecond:0.0} m/s | {state.displaySpeed:0} {state.speedUnit}");
                GUILayout.Label($"RPM: {state.engineRpm:0}");
                GUILayout.Label($"Gear: {state.transmissionLabel} | {state.shiftState}");
                GUILayout.Label($"Range: req {state.requestedRange} / eng {state.engagedRange}");
                GUILayout.Label($"Split: req {state.requestedSplitter} / eng {state.engagedSplitter}");
                GUILayout.Label($"Ignition: {state.ignitionState} | Running: {state.engineRunning} | Stalled: {state.engineStalled}");
                GUILayout.Label($"Brake: park {state.parkingBrakeOn} service {state.serviceBrakeInput:0.00} trailer {state.trailerBrakeHeld}");
                GUILayout.Label($"Lights: low {state.headlightsOn} high {state.highBeamsOn} signal {state.turnSignal} hazards {state.hazardsOn}");
                GUILayout.Label($"Drivetrain: EB {state.engineBrakeLevel} ret {state.retarderLevel} diff {state.differentialLocked}");
                GUILayout.Label($"Cruise: {state.cruiseEnabled} target {state.cruiseTargetSpeedMetersPerSecond * 2.23693629f:0} mph");
                GUILayout.Label($"Trailer: {state.trailerAttached} {state.trailerId}");
            }

            if (mirrors != null)
            {
                LwsTruckMirrorRuntimeState mirrorState = mirrors.CurrentState;
                GUILayout.Space(4f);
                GUILayout.Label($"Mirror quality: {mirrorState.quality}");
                GUILayout.Label($"Mirrors: L {mirrorState.leftActive} R {mirrorState.rightActive}");
                GUILayout.Label($"RT: {mirrorState.resolutionPixels}px | every {mirrorState.updateIntervalFrames} frame(s)");
                if (GUILayout.Button("Cycle Mirror Quality"))
                {
                    mirrors.CycleQuality();
                }
            }

            if (anchors != null)
            {
                GUILayout.Space(4f);
                GUILayout.Label($"Anchors: {anchors.Anchors.Count}");
                GUILayout.Label($"Dashboard: {anchors.CountByType(LwsCabAccessoryAnchorType.DashboardAccessory)}");
                GUILayout.Label($"Hanging: {anchors.CountByType(LwsCabAccessoryAnchorType.HangingAccessory)}");
                GUILayout.Label($"Passenger: {anchors.CountByType(LwsCabAccessoryAnchorType.PassengerSeat)}");
                GUILayout.Label($"Sleeper: {anchors.CountByType(LwsCabAccessoryAnchorType.Sleeper)}");
                GUILayout.Label($"Mementos: {anchors.CountByType(LwsCabAccessoryAnchorType.PersonalMemento)}");
                GUILayout.Label($"Hula: {anchors.HulaPlaceholderAttached} @ {anchors.HulaPlaceholderAnchorId}");
            }

            GUILayout.EndArea();
        }

        private void ResolveReferences()
        {
            if (dashboard == null) dashboard = GetComponent<LwsTruckDashboardController>();
            if (mirrors == null) mirrors = GetComponent<LwsTruckMirrorController>();
            if (anchors == null) anchors = GetComponent<LwsCabAccessoryAnchorRegistry>();
        }
    }
}
