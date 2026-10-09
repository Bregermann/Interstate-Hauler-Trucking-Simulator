using System;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public sealed partial class TruckTaxiSession
    {
        private TruckTaxiMoralityState morality=new TruckTaxiMoralityState();
        private TruckTaxiMoralChoicePresentation presentedMoralChoice;
        public event Action MoralityChanged;
        public ITruckTaxiMoralEventContextSource MoralEventContextSource { get; set; }
        // Existing world metadata owner maps pickup position to town. Missing mapping is neutral.
        public Func<Vector3,string> TownIdAtPosition { get; set; }
        public float AcceptedTownFareMultiplier { get; private set; }=1;
        public TruckTaxiMoralityState CaptureMoralityState() => morality.Copy();
        public TruckTaxiTownState CaptureTownState(string townId) => morality.towns.Find(t=>t.townId==townId)?.Copy();
        public TruckTaxiAcceptedMoralChoice CaptureActiveMoralChoice() => morality.choices.Find(c=>!c.completed)?.Copy();
        public float TownFareMultiplier(string townId) => morality.towns.Find(t=>t.townId==townId)?.fareMultiplier ?? 1;

        public bool TryRegisterTown(TruckTaxiTownDefinition definition,out string reason)
        {
            if(!CanChangeCareer(out reason)) return false;
            if(definition==null || definition.initialState==null || !definition.initialState.IsValid)
            { reason="Invalid authored town."; return false; }
            if(morality.towns.Exists(t=>t.townId==definition.initialState.townId)) return true;
            var next=morality.Copy(); next.towns.Add(definition.initialState.Copy());
            if(!next.Validate(ShiftEarnings,out reason)) return false;
            careerMutationInProgress=true;
            try { morality=next; MoralityChanged?.Invoke(); Changed?.Invoke(); }
            finally { careerMutationInProgress=false; }
            return true;
        }

        public bool TryPresentMoralChoice(TruckTaxiMoralChoiceDefinition definition,out TruckTaxiMoralChoicePresentation presentation,out string reason)
        {
            presentation=null;
            if(!CanChangeCareer(out reason)) return false;
            if(definition==null) { reason="No authored moral choice."; return false; }
            if(!definition.Validate(out reason)) return false;
            if(morality.choices.Exists(c=>c.eventId==definition.eventId || !c.completed))
            { reason="Event already accepted/completed, or another choice is active."; return false; }
            var prepared=new TruckTaxiMoralChoicePresentation {
                eventId=definition.eventId,trailerId=definition.trailerId,description=definition.description,
                initialTown=definition.town.initialState.Copy(),options=Array.ConvertAll(definition.options,o=>o.Copy())
            };
            careerMutationInProgress=true;
            try
            {
                if(!MatchingMoralContext(prepared.eventId,prepared.initialTown.townId,prepared.trailerId,null,out reason)) return false;
                presentedMoralChoice=prepared; presentation=prepared.Copy(); return true;
            }
            finally { careerMutationInProgress=false; }
        }

        public bool TryAcceptMoralChoice(string eventId,string choiceId,out string reason)
        {
            if(!CanChangeCareer(out reason)) return false;
            var presented=presentedMoralChoice;
            if(presented==null || presented.eventId!=eventId || morality.choices.Exists(c=>c.eventId==eventId || !c.completed))
            { reason="No matching presented choice, or event already accepted/completed."; return false; }
            var option=Array.Find(presented.options,o=>o.choiceId==choiceId);
            if(option==null) { reason="Unknown authored choice."; return false; }
            careerMutationInProgress=true;
            try
            {
                if(!MatchingMoralContext(eventId,presented.initialTown.townId,presented.trailerId,null,out reason)) return false;
                var next=morality.Copy();
                if(!next.towns.Exists(t=>t.townId==presented.initialTown.townId)) next.towns.Add(presented.initialTown.Copy());
                next.choices.Add(new TruckTaxiAcceptedMoralChoice {
                    eventId=eventId,townId=presented.initialTown.townId,trailerId=presented.trailerId,
                    choiceId=choiceId,completionObjectiveId=option.completionObjectiveId,effects=option.effects.Copy()
                });
                if(!next.Validate(ShiftEarnings,out reason)) return false;
                morality=next; presentedMoralChoice=null; MoralityChanged?.Invoke(); Changed?.Invoke(); return true;
            }
            finally { careerMutationInProgress=false; }
        }

        public bool TryCompleteMoralChoice(string eventId,out string reason)
        {
            if(!CanChangeCareer(out reason)) return false;
            var accepted=morality.choices.Find(c=>c.eventId==eventId && !c.completed);
            if(accepted==null) { reason="No matching accepted moral choice."; return false; }
            careerMutationInProgress=true;
            try
            {
                if(!MatchingMoralContext(eventId,accepted.townId,accepted.trailerId,accepted.completionObjectiveId,out reason)) return false;
                long earnings;
                try { earnings=checked(ShiftEarnings+accepted.effects.payoutCents); }
                catch(OverflowException) { reason="Moral-choice payout would overflow the wallet."; return false; }
                var next=morality.Copy();
                var completed=next.choices.Find(c=>c.eventId==eventId);
                next.towns.Find(t=>t.townId==accepted.townId).Apply(completed.effects);
                completed.completed=true;
                if(!next.Validate(earnings,out reason)) return false;
                // Same earnings authority as fares/races. Publish effects, completion and money together.
                morality=next; ShiftEarnings=earnings; MoralityChanged?.Invoke(); Changed?.Invoke(); return true;
            }
            finally { careerMutationInProgress=false; }
        }

        private bool MatchingMoralContext(string eventId,string townId,string trailerId,string objectiveId,out string reason)
        {
            reason="Matching live authored trailer/event context is unavailable.";
            if(MoralEventContextSource==null || !MoralEventContextSource.TryGetContext(eventId,out var context) ||
                context.EventId!=eventId || context.TownId!=townId || context.Trailer==null || !context.Trailer.isActiveAndEnabled ||
                !context.Trailer.Validate(out _) || context.Trailer.StableTrailerId!=trailerId) return false;
            if(objectiveId!=null && context.CompletedObjectiveId!=objectiveId)
            { reason="The accepted authored completion objective has not completed."; return false; }
            reason=null; return true;
        }
    }
}
