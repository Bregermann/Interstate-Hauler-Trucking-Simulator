using UnityEngine;

namespace LWS.TruckTaxi
{
    [CreateAssetMenu(menuName = "Truck Taxi/Passenger Profile")]
    public sealed class PassengerProfile : ScriptableObject
    {
        public string passengerId;
        public string passengerName;
        [Header("Identity and casting")]
        public string developmentReference, archetype;
        [TextArea] public string description;
        public TruckTaxiCastingProfile casting;
        public TruckTaxiAppearanceProfile appearance;
        public TruckTaxiAnimatorProfile animatorProfile;
        public TruckTaxiSeatProfile seatProfile;
        public GameObject modelPrefab, runtimePrefab;
        public PassengerProfile pairPassenger, companion;
        public bool available = true;
        public TruckTaxiRarity rarity;
        [Min(0)] public float spawnWeight = 1;
        public string[] districts = System.Array.Empty<string>();
        public TruckTaxiMechanic[] uniqueMechanics = System.Array.Empty<TruckTaxiMechanic>();
        [Header("Driving preferences (independent of casting)")]
        [Range(1,5)] public float baseSatisfaction = 4;
        [Min(0)] public float tipMultiplier = 1;
        [Range(-1,1)] public float speedPreference, collisionPreference, shortcutPreference, offroadPreference;
        [Header("Player ejection")]
        public bool canBeEjected = true;
        [Min(0)] public float ejectionImpulse = 6;
        public int ejectionChaosReward = 75;
        [Min(0)] public int ejectionFarePenaltyCents = 500;
        [Range(0,4)] public float ejectionRatingPenalty = 1;
        public TruckTaxiDialogueSet ejectionReactions;
        [TextArea] public string ejectionReaction = "I requested a ride, not a launch!";
        public TruckTaxiVoiceProfile voiceProfile;
        public TruckTaxiDialogueSet authoredDialogue;
        public Sprite portrait;
        [Range(1,5)] public float passengerRating = 4.5f;
        public string personality;
        [Min(30)] public float basePatience = 240;
        [Range(0,1)] public float baseTipChance = 0.5f;
        [TextArea] public string[] dialogueSet;
        public PassengerRequestDefinition[] possibleRequests;
        [Min(5)] public float requestFrequency = 20;
        public Vector2 requestDifficultyRange = new Vector2(0.8f, 1.2f);
        public string preferredDrivingStyle;
        public string[] specialTraits;
        [Tooltip("Positive values reward chaos; negative values penalize it.")] public float chaosAffinity = -1;
        public float smoothAffinity = 1;
        [TextArea] public string collisionReaction = "My coffee has become airborne.";
        [TextArea] public string shortcutReaction = "Is this on the map?";
        [TextArea] public string speedReaction = "My calendar just skipped a day.";
    }
}
