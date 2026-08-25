using UnityEngine;

namespace DeadAir
{
    public sealed class DeadAirDashboardEventTrigger : DeadAirTriggerZone
    {
        [SerializeField] private DeadAirDashboardMisinformationDirector.DashboardState dashboardState;

        protected override void Reset()
        {
            base.Reset();
            SetCategory(DeadAirTriggerCategory.Dashboard);
        }

        protected override void OnActivated(DeadAirTriggerEvent triggerEvent, DeadAirStoryDirector director)
        {
            DeadAirDashboardMisinformationDirector dashboard = FindFirstObjectByType<DeadAirDashboardMisinformationDirector>();
            dashboard?.Apply(dashboardState);
        }
    }
}
