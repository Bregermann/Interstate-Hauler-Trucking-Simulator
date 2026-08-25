using UnityEngine;

namespace DeadAir
{
    public sealed class DeadAirGpsEventTrigger : DeadAirTriggerZone
    {
        [SerializeField] private string instruction = "Recalculating.";
        [SerializeField] private string destination = "Exit 17";
        [SerializeField] private DeadAirGpsArrow arrow = DeadAirGpsArrow.Straight;
        [SerializeField] private float distanceMeters = 805f;
        [SerializeField] private bool enabledState = true;
        [SerializeField] private bool routeVisible = true;
        [SerializeField] private bool intentionallyWrong;
        [SerializeField] private bool recalculate;
        [SerializeField] private bool signalLost;
        [SerializeField] private bool corrupted;
        [SerializeField] private bool restoreNormal;

        protected override void Reset()
        {
            base.Reset();
            SetCategory(DeadAirTriggerCategory.GPS);
        }

        protected override void OnActivated(DeadAirTriggerEvent triggerEvent, DeadAirStoryDirector director)
        {
            DeadAirGPSDirector gps = DeadAirGameManager.Instance != null
                ? DeadAirGameManager.Instance.GpsDirector
                : FindFirstObjectByType<DeadAirGPSDirector>();
            if (gps == null)
            {
                return;
            }

            if (restoreNormal)
            {
                gps.ResetGps();
                return;
            }

            gps.SetEnabled(enabledState);
            gps.SetDestination(destination);
            gps.SetRouteVisible(routeVisible);
            gps.SetGpsInstruction(instruction, arrow, distanceMeters, intentionallyWrong);
            if (recalculate)
            {
                gps.SetRecalculating(true);
            }

            if (signalLost)
            {
                gps.SetSignalLost(true);
            }

            if (corrupted)
            {
                gps.SetCorrupted(true);
            }
        }
    }
}
