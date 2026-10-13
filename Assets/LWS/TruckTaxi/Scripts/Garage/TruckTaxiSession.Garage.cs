namespace LWS.TruckTaxi
{
    public sealed partial class TruckTaxiSession
    {
        // Selection and paint publish together through the existing Career/vendor save payload.
        public bool TryApplyGarage(TruckTaxiCareerCatalog catalog, string truckId,
            TruckTaxiGaragePaintData paint, out string reason)
        {
            if (!CatalogReady(catalog, out reason)) return false;
            if (!ownedProfile.trucks.Exists(t => t.truckId == truckId) || catalog.FindTruck(truckId)?.HasContent != true)
            { reason = "Truck is not owned or its real prefab is unavailable."; return false; }
            if (paint != null && !paint.Validate())
            { reason = "Paint must be a finite, opaque RGB color."; return false; }
            var next = ownedProfile.Copy();
            next.equippedTruckId = truckId;
            var truck = SelectedTruck(next);
            truck.hasCustomPaint = paint != null;
            truck.paint = paint?.Copy();
            truck.cosmetics.RemoveAll(s => s.slotId == "paint");
            return CommitCareer(next, 0, out reason);
        }
    }
}
