using UnityEngine;

namespace DeadAir
{
    public sealed class DeadAirTrafficHorrorEventTrigger : DeadAirTriggerZone
    {
        [SerializeField] private DeadAirTrafficHorrorDirector.TrafficEvent trafficEvent;
        [SerializeField] private bool cleanupInsteadOfSpawn;

        protected override void Reset()
        {
            base.Reset();
            SetCategory(DeadAirTriggerCategory.Traffic);
        }

        protected override void OnActivated(DeadAirTriggerEvent triggerEvent, DeadAirStoryDirector director)
        {
            DeadAirTrafficHorrorDirector traffic = FindFirstObjectByType<DeadAirTrafficHorrorDirector>();
            if (cleanupInsteadOfSpawn)
            {
                traffic?.Cleanup();
            }
            else
            {
                traffic?.Play(trafficEvent);
            }
        }
    }
}
