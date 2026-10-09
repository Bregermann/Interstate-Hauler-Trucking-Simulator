using System;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public enum TruckTaxiOccupantReactionCategory
    {
        Amused, Embarrassed, Annoyed, Curious, Enthusiastic, Jealous, Shocked, Disapproving, Entertained
    }

    [Serializable]
    public sealed class TruckTaxiOccupantReaction
    {
        public PassengerProfile passenger;
        public TruckTaxiOccupantReactionCategory category;
        [TextArea] public string subtitle;

        // Explicit adult authoring only; ordinary pickups do not acquire private-event eligibility.
        public bool Eligible(PassengerProfile speaker, bool companionIsAdult) =>
            speaker != null && speaker == passenger && speaker.explicitlyAdult && speaker.minimumAdultAge >= 18 &&
            companionIsAdult && !string.IsNullOrWhiteSpace(subtitle);
    }
}
