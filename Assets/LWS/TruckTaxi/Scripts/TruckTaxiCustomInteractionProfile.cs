using System;
using UnityEngine;

namespace LWS.TruckTaxi
{
    [CreateAssetMenu(menuName = "Truck Taxi/Custom Content/Interaction Phases")]
    public sealed class TruckTaxiCustomInteractionProfile : ScriptableObject
    {
        public string stableId;
        public TruckTaxiCustomReactionAudioProfile startAudioProfile, loopAudioProfile, completionAudioProfile;
        public TruckTaxiCustomPresentationProfile startPresentation, loopPresentation, completionPresentation;
        [Tooltip("Optional loop variants; empty uses Loop Presentation. Only positive weights participate.")]
        public TruckTaxiCustomPresentationProfile[] variants = Array.Empty<TruckTaxiCustomPresentationProfile>();
        [Tooltip("Optional start presentation/audio fallback from an authored interaction. This does not consume items or award rewards; those require explicit player acceptance.")]
        public TruckTaxiCustomItemInteractionDefinition optionalCustomInteractionProfile;
    }
}
