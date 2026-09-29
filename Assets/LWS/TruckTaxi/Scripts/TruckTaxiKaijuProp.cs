using UnityEngine;

namespace LWS.TruckTaxi
{
    public enum TruckTaxiKaijuPropKind { Vehicle, BuildingChunk }

    // Only project-authored scenery may be consumed. Never attach this to a UTS pooled car.
    [DisallowMultipleComponent]
    public sealed class TruckTaxiKaijuProp : MonoBehaviour
    {
        public TruckTaxiKaijuPropKind kind;
        public GameObject intactRoot;
        public GameObject damagedRoot;
        [Tooltip("Visual-only prefab for grab/throw. Do not assign a live traffic prefab.")]
        public GameObject presentationPrefab;
        public bool restoreAfterEvent = true;
        public bool Taken { get; private set; }
        private GameObject proxy;
        public GameObject Visual => proxy;

        public bool IsEligible => isActiveAndEnabled && !Taken && intactRoot != null && intactRoot.activeSelf &&
            GetComponentInParent<TruckTaxiTrafficPooledActor>() == null &&
            (presentationPrefab == null || (presentationPrefab.activeSelf &&
                presentationPrefab.GetComponentsInChildren<MonoBehaviour>(true).Length == 0));

        public GameObject Grab(Transform hand)
        {
            if (!IsEligible || hand == null) return null;
            proxy = TruckTaxiKaijuVisual.Snapshot(presentationPrefab != null ? presentationPrefab.transform : intactRoot.transform, true);
            if (proxy == null) return null;
            proxy.transform.SetPositionAndRotation(intactRoot.transform.position, intactRoot.transform.rotation);
            proxy.transform.SetParent(hand, true);
            intactRoot.SetActive(false);
            if (damagedRoot != null) damagedRoot.SetActive(true);
            Taken = true;
            return proxy;
        }

        public GameObject Throw(Vector3 velocity)
        {
            if (!Taken || proxy == null) return null;
            TruckTaxiKaijuVisual.Throw(proxy, velocity);
            var released = proxy;
            proxy = null;
            return released;
        }

        public void Eat()
        {
            if (proxy != null) Destroy(proxy);
            proxy = null;
        }

        public bool Crush()
        {
            if (!IsEligible) return false;
            intactRoot.SetActive(false);
            if (damagedRoot != null) damagedRoot.SetActive(true);
            Taken = true;
            return true;
        }

        public void Restore()
        {
            if (proxy != null) Destroy(proxy);
            proxy = null;
            if (restoreAfterEvent)
            {
                if (intactRoot != null) intactRoot.SetActive(true);
                if (damagedRoot != null) damagedRoot.SetActive(false);
                Taken = false;
            }
        }

        private void OnDestroy() { if (proxy != null) Destroy(proxy); }
    }
}
