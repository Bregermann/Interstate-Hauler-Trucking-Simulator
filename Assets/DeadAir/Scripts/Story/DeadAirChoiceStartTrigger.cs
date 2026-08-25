using UnityEngine;

namespace DeadAir
{
    public sealed class DeadAirChoiceStartTrigger : DeadAirTriggerZone
    {
        [SerializeField] private string choiceId = "CHOICE_00";

        public override string ChoiceId => choiceId;

        public void ConfigureChoice(string id)
        {
            choiceId = string.IsNullOrWhiteSpace(id) ? "CHOICE_00" : id;
        }

        protected override void Reset()
        {
            base.Reset();
            SetCategory(DeadAirTriggerCategory.ChoiceStart);
        }
    }
}
