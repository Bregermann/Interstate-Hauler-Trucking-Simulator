using System.Collections.Generic;
using LWS.InterstateHauler;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // LWS cab anchors remain the sole attachment authority.
    public sealed class TruckTaxiCabDecorations
    {
        private readonly LwsCabAccessoryAnchorRegistry registry;
        private readonly Dictionary<TruckTaxiCabSlot, TruckTaxiNeedsItem> mounted = new Dictionary<TruckTaxiCabSlot, TruckTaxiNeedsItem>();
        private static readonly string[] AnchorIds =
            { "IH_CabAnchor_Dashboard01", "IH_CabAnchor_Dashboard02", "IH_CabAnchor_Hanging01" };
        public TruckTaxiCabDecorations(LwsCabAccessoryAnchorRegistry value) { registry = value; registry?.EnsureInitialized(); }
        public bool Mount(TruckTaxiNeedsItem item, TruckTaxiCabSlot slot, GameObject accessory)
        {
            var definition = TruckTaxiNeedsItems.Find(item);
            bool dashboard = slot == TruckTaxiCabSlot.DashboardLeft || slot == TruckTaxiCabSlot.DashboardRight;
            bool compatible = item == TruckTaxiNeedsItem.AirFreshener ? slot == TruckTaxiCabSlot.Mirror :
                item == TruckTaxiNeedsItem.HulaFigure || item == TruckTaxiNeedsItem.NoveltyDuck ? dashboard : false;
            if (definition == null || !compatible || accessory == null || registry == null ||
                !registry.TryGetAnchor(AnchorIds[(int)slot], out var anchor) || anchor.Occupied) return false;
            var result = anchor.Attach(accessory);
            if (!result.Succeeded) return false;
            mounted[slot] = item;
            return true;
        }
        public bool IsMounted(TruckTaxiCabSlot slot) => mounted.ContainsKey(slot);
        public int MountedCount(TruckTaxiNeedsItem item)
        { int count = 0; foreach (var value in mounted.Values) if (value == item) count++; return count; }
    }
}
