using UnityEngine;

namespace DeadAir
{
    public sealed class DeadAirChoiceCommitTrigger : DeadAirTriggerZone
    {
        [SerializeField] private string choiceId = "CHOICE_00";
        [SerializeField] private DeadAirChoiceOutcome outcome = DeadAirChoiceOutcome.None;

        public override string ChoiceId => choiceId;
        protected override DeadAirChoiceOutcome ChoiceOutcome => outcome;

        public void ConfigureChoice(string id, DeadAirChoiceOutcome choiceOutcome)
        {
            choiceId = string.IsNullOrWhiteSpace(id) ? "CHOICE_00" : id;
            outcome = choiceOutcome;
        }

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
