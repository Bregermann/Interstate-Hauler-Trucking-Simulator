using System;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public enum TruckTaxiDialogueCategory
    {
        PickupGreeting, PickupComplaint, Boarding, DestinationReveal, GeneralChatter,
        RequestIntroduction, RequestReminder, RequestSuccess, RequestFailure,
        SpeedPositive, SpeedNegative, CollisionPositive, CollisionNegative, TrafficRamReaction,
        PedestrianHitReaction, ShortcutReaction, OffroadReaction, NearMissReaction, PropertyDamageReaction,
        Arrival, RideFailure, Ejection, PairExchange, SpecialMechanic
    }

    [Serializable]
    public sealed class TruckTaxiDialogueLine
    {
        public string lineId;
        public string passengerId;
        public TruckTaxiDialogueCategory category;
        [TextArea(2, 6)] public string text;
        [TextArea(2, 6)] public string subtitle;
        public TruckTaxiVoiceEmotion emotion;
        [Tooltip("Authored performance metadata. WorkHere uses the selected emotion reference and preset; no unsupported Turbo intensity parameter is invented.")]
        [Range(0, 2)] public float intensity = 1;
        public TruckTaxiVoiceProfile voiceProfile;
        public AudioClip generatedAudio;
        [Min(0)] public float cooldown = 8;
        [Min(0)] public float weight = 1;
        public int interruptPriority;
        public bool oneShot;
        public TruckTaxiState[] rideStateRequirements = Array.Empty<TruckTaxiState>();
        public TaxiRequestType[] requestRequirements = Array.Empty<TaxiRequestType>();
        public string[] gameplayTags = Array.Empty<string>();
        public string Subtitle => string.IsNullOrWhiteSpace(subtitle) ? text : subtitle;
#if UNITY_EDITOR
        [Tooltip("Optional external WAV override for this line. Never imported into the build.")]
        public string preferredReference;
        [HideInInspector] public string generationRequestHash;
        [HideInInspector] public string generationHash;
#endif
    }

    [CreateAssetMenu(menuName = "Truck Taxi/Passengers/Dialogue Set")]
    public sealed class TruckTaxiDialogueSet : ScriptableObject
    {
        public TruckTaxiDialogueLine[] lines = Array.Empty<TruckTaxiDialogueLine>();
    }
}
