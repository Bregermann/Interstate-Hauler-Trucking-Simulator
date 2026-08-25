using UnityEngine;

namespace DeadAir
{
    public sealed class DeadAirAudioEventTrigger : DeadAirTriggerZone
    {
        [SerializeField] private DeadAirAudioDirector.AudioSequence sequence;

        protected override void Reset()
        {
            base.Reset();
            SetCategory(DeadAirTriggerCategory.CBRadio);
        }

        protected override void OnActivated(DeadAirTriggerEvent triggerEvent, DeadAirStoryDirector director)
        {
            DeadAirAudioDirector audio = DeadAirGameManager.Instance != null
                ? DeadAirGameManager.Instance.AudioDirector
                : FindFirstObjectByType<DeadAirAudioDirector>();
            audio?.Play(sequence);
        }
    }
}
