using UnityEngine;using UnityEngine.EventSystems;
namespace LWS.TruckSports {
    public sealed class SportsCardSelection : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        public SportsCardMotion card;
        public void OnSelect(BaseEventData data) { card?.Select(true); }
        public void OnDeselect(BaseEventData data) { card?.Select(false); }
    }
}
