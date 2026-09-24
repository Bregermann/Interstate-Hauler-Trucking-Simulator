using System;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public enum TruckTaxiVoiceEmotion { Neutral, Excited, Angry, Afraid, Sad, Annoyed }

    [CreateAssetMenu(menuName = "Truck Taxi/Passengers/Voice Profile")]
    public sealed class TruckTaxiVoiceProfile : ScriptableObject
    {
        public string voiceProfileId;
        public string displayName;
        [TextArea] public string description;
        public string voicePresentation;
        public string approximateAge;
        public string regionalAccent;
        public string pitchDescription;
        public string energy;
        public string speakingSpeed;
        public string deliveryStyle;

#if UNITY_EDITOR
        // Authoring paths only. Source recordings and generation tools are never player dependencies.
        [Header("External WorkHere generation (Editor only)")]
        public string model = "turbo";
        public string preset = "Lecture Neutral";
        public string device = "auto";
        public int seed = 42;
        [Tooltip("Absolute WAV path outside Assets. Source recordings are read, never rewritten.")]
        public string neutralReference;
        public string excitedReference;
        public string angryReference;
        public string afraidReference;
        public string sadReference;
        public string annoyedReference;
        public VoiceReference[] additionalReferences = Array.Empty<VoiceReference>();

        [Serializable]
        public sealed class VoiceReference
        {
            public TruckTaxiVoiceEmotion emotion;
            public string path;
        }

        public string ResolveReference(TruckTaxiVoiceEmotion emotion, out string resolution)
        {
            var exact = FindReference(emotion);
            if (!string.IsNullOrWhiteSpace(exact)) { resolution = emotion.ToString(); return exact; }
            TruckTaxiVoiceEmotion closest;
            switch (emotion)
            {
                case TruckTaxiVoiceEmotion.Angry: closest = TruckTaxiVoiceEmotion.Annoyed; break;
                case TruckTaxiVoiceEmotion.Annoyed: closest = TruckTaxiVoiceEmotion.Angry; break;
                case TruckTaxiVoiceEmotion.Afraid: closest = TruckTaxiVoiceEmotion.Sad; break;
                case TruckTaxiVoiceEmotion.Sad: closest = TruckTaxiVoiceEmotion.Afraid; break;
                default: closest = TruckTaxiVoiceEmotion.Neutral; break;
            }
            var fallback = FindReference(closest);
            if (!string.IsNullOrWhiteSpace(fallback)) { resolution = emotion + " -> " + closest; return fallback; }
            resolution = emotion + " -> Neutral";
            return FindReference(TruckTaxiVoiceEmotion.Neutral);
        }

        private string FindReference(TruckTaxiVoiceEmotion emotion)
        {
            string path;
            switch (emotion)
            {
                case TruckTaxiVoiceEmotion.Excited: path = excitedReference; break;
                case TruckTaxiVoiceEmotion.Angry: path = angryReference; break;
                case TruckTaxiVoiceEmotion.Afraid: path = afraidReference; break;
                case TruckTaxiVoiceEmotion.Sad: path = sadReference; break;
                case TruckTaxiVoiceEmotion.Annoyed: path = annoyedReference; break;
                default: path = neutralReference; break;
            }
            if (!string.IsNullOrWhiteSpace(path)) return path;
            if (additionalReferences != null)
                foreach (var reference in additionalReferences)
                    if (reference != null && reference.emotion == emotion && !string.IsNullOrWhiteSpace(reference.path)) return reference.path;
            return "";
        }
#endif
    }
}
