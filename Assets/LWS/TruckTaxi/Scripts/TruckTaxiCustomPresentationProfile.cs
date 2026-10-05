using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace LWS.TruckTaxi
{
    [CreateAssetMenu(menuName = "Truck Taxi/Custom Content/Presentation")]
    public sealed class TruckTaxiCustomPresentationProfile : ScriptableObject
    {
        public string stableId, displayName;
        public AnimationClip driverAnimation, passengerAnimation;
        [Tooltip("Timeline outputs named Driver and Passenger bind only to visual proxy Animators.")]
        public PlayableAsset timeline;
        [Tooltip("Presentation-only prefab. Physics and custom behaviour components are rejected.")]
        public GameObject presentationPrefab;
        public TruckTaxiCustomReactionAudioProfile audio;
        [Min(.05f)] public float duration = 3;
        [Min(0)] public float weight = 1;
        [Tooltip("Optional TruckTaxiCabAnchorId name. Empty preserves the current visual pose.")]
        public string driverAnchorId, passengerAnchorId;
        [Min(0)] public float fadeIn = .15f, fadeOut = .2f;
        public bool allowMovement;
        public bool lockInput = true;
        [Tooltip("Requests the existing renderer-only event motion; never animates vehicle physics.")]
        public bool useTruckRocking;

        public IEnumerable<string> Validate()
        {
            if (string.IsNullOrWhiteSpace(stableId)) yield return "Missing stable ID.";
            if (!float.IsFinite(duration) || duration <= 0) yield return "Duration must be positive.";
            if (!float.IsFinite(weight) || weight < 0) yield return "Weight must be finite and nonnegative.";
            if (!float.IsFinite(fadeIn) || !float.IsFinite(fadeOut) || fadeIn < 0 || fadeOut < 0) yield return "Fade times must be finite and nonnegative.";
            if (driverAnimation != null && driverAnimation.legacy) yield return "Driver clip must not be Legacy.";
            if (passengerAnimation != null && passengerAnimation.legacy) yield return "Passenger clip must not be Legacy.";
            if (!PrefabIsSafe(presentationPrefab)) yield return "Invalid prefab: presentation must contain only transforms, renderers, mesh filters and Animators.";
            if (!ValidAnchor(driverAnchorId)) yield return "Unknown driver anchor ID.";
            if (!ValidAnchor(passengerAnchorId)) yield return "Unknown passenger anchor ID.";
            if (timeline != null)
                foreach (var output in timeline.outputs)
                    if (output.outputTargetType != typeof(Animator) || output.streamName != "Driver" && output.streamName != "Passenger")
                        yield return "Timeline supports Animation tracks named Driver or Passenger only; other tracks could escape proxy isolation.";
        }

        private static bool ValidAnchor(string id) => string.IsNullOrEmpty(id) ||
            System.Enum.TryParse<TruckTaxiCabAnchorId>(id, out var value) && System.Enum.IsDefined(typeof(TruckTaxiCabAnchorId), value);

        public static bool PrefabIsSafe(GameObject prefab)
        {
            if (prefab == null) return true;
            foreach (var component in prefab.GetComponentsInChildren<Component>(true))
                if (component == null || !(component is Transform || component is Renderer || component is MeshFilter || component is Animator)) return false;
            return true;
        }
    }
}
