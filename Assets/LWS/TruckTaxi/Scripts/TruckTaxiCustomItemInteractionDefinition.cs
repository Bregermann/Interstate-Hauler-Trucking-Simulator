using System;
using UnityEngine;

namespace LWS.TruckTaxi
{
    [CreateAssetMenu(menuName = "Truck Taxi/Custom Content/Item Interaction")]
    public sealed class TruckTaxiCustomItemInteractionDefinition : ScriptableObject
    {
        public string stableId, displayName;
        [Tooltip("Exact stable inventory ID. No display-name matching.")]
        public string requiredItemStableId;
        public string[] requiredPassengerTags = Array.Empty<string>();
        public TruckTaxiPassengerGender requiredGender;
        [Min(0)] public int minimumAge;
        [Min(0)] public int consumeQuantity = 1;
        [TextArea] public string optionalPrompt, optionalAcceptText, optionalDeclineText;
        [Min(0)] public long rewardCashCents;
        [Range(-4, 4)] public float satisfactionDelta;
        public int chaosDelta;
        public TruckTaxiCustomPresentationProfile presentationProfile;
        public TruckTaxiCustomReactionAudioProfile audioProfile;
        public string customEventId;
        [Tooltip("Prevents one accepted interaction being executed twice in the same ride.")]
        public bool oncePerRide = true;
    }
}
