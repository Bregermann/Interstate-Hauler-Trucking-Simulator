using UnityEngine;

namespace LWS.TruckTaxi
{
    // Opt-in inspector probe. Does not mutate state or claim to exercise physics/audio.
    public sealed class TruckTaxiNeedsRuntimeProbe : MonoBehaviour
    {
        [SerializeField] private TruckTaxiDriverNeedsCoordinator needs;
        [ContextMenu("Report Driver Needs Wiring")]
        public void Report()
        {
            if (!Application.isPlaying || needs == null || needs.State == null)
            { Debug.LogWarning("Needs probe requires Play Mode and an initialized coordinator.", this); return; }
            var state = needs.State;
            Debug.Log("NEEDS PROBE: hunger=" + state.Hunger.ToString("0.00") +
                " thirst=" + state.Thirst.ToString("0.00") +
                " empty bottles=" + state.Count(TruckTaxiNeedsItem.EmptyBottle) +
                " empty jugs=" + state.Count(TruckTaxiNeedsItem.EmptyPissJug) +
                " filled bottles=" + state.Count(TruckTaxiNeedsItem.FilledBottle) +
                " filled jugs=" + state.Count(TruckTaxiNeedsItem.FilledJug) +
                " stores=" + needs.Stores.Count +
                " nearby store=" + (needs.NearbyStore != null ? needs.NearbyStore.stableId : "none"), this);
        }
    }
}
