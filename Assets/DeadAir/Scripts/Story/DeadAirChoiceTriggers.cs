using UnityEngine;

namespace DeadAir
{
    public sealed class DeadAirChoiceStartTrigger : DeadAirTriggerZone
    {
        [SerializeField] private string choiceId = "CHOICE_00";

        public string ChoiceId => choiceId;

        protected override void Reset()
        {
            base.Reset();
            SetCategory(DeadAirTriggerCategory.ChoiceStart);
        }
    }

    public sealed class DeadAirChoiceCommitTrigger : DeadAirTriggerZone
    {
        [SerializeField] private string choiceId = "CHOICE_00";
        [SerializeField] private DeadAirChoiceOutcome outcome = DeadAirChoiceOutcome.None;

        public string ChoiceId => choiceId;
        protected override DeadAirChoiceOutcome ChoiceOutcome => outcome;

        protected override void Reset()
        {
            base.Reset();
            SetCategory(DeadAirTriggerCategory.ChoiceCommit);
        }

        protected override void OnActivated(DeadAirTriggerEvent triggerEvent, DeadAirStoryDirector director)
        {
            director?.CommitChoice(choiceId, outcome);
        }
    }
}
