using UnityEngine;

namespace LWS.TruckTaxi
{
    // Scene-side interaction only. Stable IDs live on authored objects, never on instance IDs.
    public abstract class TruckTaxiWorldInteractable : MonoBehaviour
    {
        [SerializeField] private string stableId;
        [Min(1f)] public float interactionRange = 3f;
        public string StableId => stableId;
        public virtual string Prompt => name;
        public virtual bool HoldToUse => false;
        public virtual bool CanUse(TruckTaxiWorldLifeController actor) => actor != null && actor.IsOnFoot && isActiveAndEnabled;
        public abstract void Use(TruckTaxiWorldLifeController actor);
        public virtual void Hold(TruckTaxiWorldLifeController actor, float seconds) { }
        public void SetStableId(string value) => stableId = value;
    }
}
