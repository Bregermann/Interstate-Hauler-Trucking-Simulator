using UnityEngine;

namespace LWS.TruckTaxi
{
    [CreateAssetMenu(menuName = "Truck Taxi/Passenger Profile")]
    public sealed class PassengerProfile : ScriptableObject
    {
        public string passengerName;
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
