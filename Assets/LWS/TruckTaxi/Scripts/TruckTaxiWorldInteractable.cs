using System.Collections.Generic;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // Scene-side interaction only. Stable IDs live on authored objects, never on instance IDs.
    public abstract class TruckTaxiWorldInteractable : MonoBehaviour
    {
        [SerializeField] private string stableId;
        [Min(1f)] public float interactionRange = 3f;
        [Tooltip("Use the controller's forgiving category radius as a minimum. Disable for an exact authored range.")]
        public bool useCategoryRange = true;
        public Transform interactionAnchor;
        private static readonly HashSet<TruckTaxiWorldInteractable> active = new HashSet<TruckTaxiWorldInteractable>();
        public static IReadOnlyCollection<TruckTaxiWorldInteractable> Active => active;
        private Collider interactionCollider;
        protected virtual void OnEnable()
        {
            interactionCollider = GetComponent<Collider>();
            active.Add(this);
        }
        protected virtual void OnDisable() => active.Remove(this);
        public Vector3 InteractionPosition(Vector3 observer) => interactionAnchor != null ? interactionAnchor.position :
            interactionCollider != null && interactionCollider.enabled ? interactionCollider.ClosestPoint(observer) : transform.position;
        public static int CompareStable(TruckTaxiWorldInteractable a, TruckTaxiWorldInteractable b)
        {
            int order = string.CompareOrdinal(a.StableId, b.StableId);
            // Instance identity only breaks malformed/missing authored-ID ties within this session.
            return order != 0 ? order : a.GetInstanceID().CompareTo(b.GetInstanceID());
        }
        public string StableId => stableId;
        public virtual string Prompt => name;
        public virtual bool HoldToUse => false;
        public virtual bool CanUse(TruckTaxiWorldLifeController actor) => actor != null && actor.IsOnFoot && isActiveAndEnabled;
        public abstract void Use(TruckTaxiWorldLifeController actor);
        public virtual void Hold(TruckTaxiWorldLifeController actor, float seconds) { }
        public void SetStableId(string value) => stableId = value;
    }
}
