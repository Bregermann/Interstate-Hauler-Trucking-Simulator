using System;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // Art anchors bind existing interaction types; service, needs and economy ownership is unchanged.
    [DisallowMultipleComponent]
    public sealed class TruckTaxiEdenboroughFacility : MonoBehaviour
    {
        public enum Role { DieselPump, Toilet, DinerSeat, MotelDesk, MotelBed, ProductShelf, Checkout }
        [Serializable] public sealed class Binding
        {
            public GameObject art;
            public Role role;
            public TruckTaxiNeedsItem product;
        }
        public TruckTaxiServicePoint service;
        public TruckTaxiGasStationPoint station;
        public TruckTaxiStorePoint store;
        public Binding[] interactions = Array.Empty<Binding>();
        private void Awake()
        {
            if (service == null) { Debug.LogError("Edenborough facility has no service identity.", this); return; }
            for (int i = 0; i < interactions.Length; i++)
            {
                var binding = interactions[i];
                if (binding?.art == null) { Debug.LogWarning("Missing reviewed facility interaction art: " + service.stableId, this); continue; }
                TruckTaxiWorldInteractable action;
                switch (binding.role)
                {
                    case Role.DieselPump: var pump = GetOrAdd<TruckTaxiDieselPump>(binding.art); pump.station = station; action = pump; break;
                    case Role.Toilet: var toilet = GetOrAdd<TruckTaxiRestroomFixture>(binding.art); toilet.bowel = true; action = toilet; break;
                    case Role.DinerSeat: action = GetOrAdd<TruckTaxiDinerSeat>(binding.art); break;
                    case Role.MotelDesk: var desk = GetOrAdd<TruckTaxiMotelDesk>(binding.art); desk.service = service; action = desk; break;
                    case Role.MotelBed: var bed = GetOrAdd<TruckTaxiMotelBed>(binding.art); bed.service = service; action = bed; break;
                    case Role.ProductShelf: var shelf = GetOrAdd<TruckTaxiProductShelf>(binding.art); shelf.store = store; shelf.item = binding.product; action = shelf; break;
                    default: action = GetOrAdd<TruckTaxiCheckoutRegister>(binding.art); break;
                }
                action.SetStableId(service.stableId + ".interaction." + i);
                action.interactionAnchor = binding.art.transform;
                action.interactionRange = 3;
                action.useCategoryRange = false;
            }
        }
        private static T GetOrAdd<T>(GameObject go) where T : Component => go.GetComponent<T>() ?? go.AddComponent<T>();
    }
}
