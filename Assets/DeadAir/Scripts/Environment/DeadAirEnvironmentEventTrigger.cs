using UnityEngine;

namespace DeadAir
{
    public sealed class DeadAirEnvironmentEventTrigger : DeadAirTriggerZone
    {
        [SerializeField] private DeadAirAnomalyDirector.AnomalyEffect anomaly;
        [SerializeField] private bool clearAllAnomalies;

        protected override void Reset()
        {
            base.Reset();
            SetCategory(DeadAirTriggerCategory.Environment);
        }

        protected override void OnActivated(DeadAirTriggerEvent triggerEvent, DeadAirStoryDirector director)
        {
            DeadAirAnomalyDirector anomalyDirector = DeadAirGameManager.Instance != null
                ? DeadAirGameManager.Instance.AnomalyDirector
                : FindFirstObjectByType<DeadAirAnomalyDirector>();
            if (clearAllAnomalies)
            {
                anomalyDirector?.ClearAllAnomalies();
            }
            else
            {
                anomalyDirector?.ApplyEffect(anomaly);
            }
        }
    }
}
