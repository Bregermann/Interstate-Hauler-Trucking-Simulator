using System;
using LWS.InterstateHauler;
using UnityEngine;

namespace LWS.TruckTaxi
{
    [Serializable]
    public sealed class TruckTaxiMoralChoiceOption
    {
        public string choiceId, label, completionObjectiveId;
        [TextArea] public string description;
        public TruckTaxiMoralChoiceEffects effects=new TruckTaxiMoralChoiceEffects();
        public TruckTaxiMoralChoiceOption Copy()
        { var copy=(TruckTaxiMoralChoiceOption)MemberwiseClone(); copy.effects=effects?.Copy(); return copy; }
    }

    [CreateAssetMenu(menuName="Truck Taxi/Morality/People In Trailer Choice")]
    public sealed class TruckTaxiMoralChoiceDefinition : ScriptableObject
    {
        public const string TurnIn="turn-in", Deliver="deliver-to-destination";
        [Tooltip("Unique one-time event ID, not a randomly generated encounter ID.")]
        public string eventId;
        public TruckTaxiTownDefinition town;
        [Tooltip("Exact LwsTrailerGameplayIdentity.StableTrailerId of the authored event trailer.")]
        public string trailerId;
        [TextArea] public string description;
        [Tooltip("Fictional abstract choices only. No voice or appearance is generated.")]
        public TruckTaxiMoralChoiceOption[] options=Array.Empty<TruckTaxiMoralChoiceOption>();
        internal static bool ValidChoiceId(string id) => id==TurnIn || id==Deliver;
        public bool Validate(out string reason)
        {
            reason="Missing or invalid authored town, trailer, choice or completion objective.";
            if(!TruckTaxiCareerProfileData.ValidId(eventId) || !TruckTaxiCareerProfileData.ValidId(trailerId) ||
                trailerId=="trailer.unassigned" || town==null || town.initialState==null || !town.initialState.IsValid ||
                options==null || options.Length!=2) return false;
            foreach(var option in options)
                if(option==null || !ValidChoiceId(option.choiceId) || string.IsNullOrWhiteSpace(option.label) ||
                    !TruckTaxiCareerProfileData.ValidId(option.completionObjectiveId) || option.effects==null || !option.effects.IsValid) return false;
            if(options[0].choiceId==options[1].choiceId || options[0].completionObjectiveId==options[1].completionObjectiveId) return false;
            reason=null; return true;
        }
    }

    public sealed class TruckTaxiMoralChoicePresentation
    {
        public string eventId, trailerId, description;
        public TruckTaxiTownState initialTown;
        public TruckTaxiMoralChoiceOption[] options;
        internal TruckTaxiMoralChoicePresentation Copy() => new TruckTaxiMoralChoicePresentation {
            eventId=eventId,trailerId=trailerId,description=description,initialTown=initialTown.Copy(),
            options=Array.ConvertAll(options,o=>o.Copy())
        };
    }

    // Runtime evidence from the existing world/event owner, never serialized or accepted from UI.
    public readonly struct TruckTaxiMoralEventContext
    {
        public readonly string EventId, TownId, CompletedObjectiveId;
        public readonly LwsTrailerGameplayIdentity Trailer;
        public TruckTaxiMoralEventContext(string eventId,string townId,LwsTrailerGameplayIdentity trailer,string completedObjectiveId=null)
        { EventId=eventId; TownId=townId; Trailer=trailer; CompletedObjectiveId=completedObjectiveId; }
    }
    public interface ITruckTaxiMoralEventContextSource
    {
        // Return true only for an active authored event. Supply an objective ID only after genuine completion.
        bool TryGetContext(string eventId,out TruckTaxiMoralEventContext context);
    }
}
