using System.Collections.Generic;

namespace LWS.TruckTaxi
{
    // A draft never changes ownership, the wallet, or the saved paint until Apply.
    public sealed class TruckTaxiGarageDraft
    {
        private string runId, originalTruckId;
        private TruckTaxiCareerProfileData snapshot;
        public string TruckId { get; private set; }
        public TruckTaxiGaragePaintData Paint { get; private set; }
        public bool IsActive => snapshot != null;

        public static List<TruckTaxiCareerTruckDefinition> OwnedTrucks(TruckTaxiCareerProfileData profile,
            TruckTaxiCareerCatalog catalog)
        {
            var result = new List<TruckTaxiCareerTruckDefinition>();
            if (profile == null || !profile.Validate(out _) || catalog == null || !catalog.Validate(out _)) return result;
            foreach (var entry in catalog.trucks)
                if (entry.HasContent && profile.trucks.Exists(t => t.truckId == entry.StableId)) result.Add(entry);
            return result;
        }

        public bool Begin(TruckTaxiCareerProfileData profile, TruckTaxiCareerCatalog catalog)
        {
            Cancel();
            if (!OwnedTrucks(profile, catalog).Exists(t => t.StableId == profile.equippedTruckId)) return false;
            snapshot = profile.Copy(); runId = profile.runProfileId; originalTruckId = profile.equippedTruckId;
            return Select(catalog, originalTruckId);
        }
        public bool Select(TruckTaxiCareerCatalog catalog, string id)
        {
            if (!IsActive || !OwnedTrucks(snapshot, catalog).Exists(t => t.StableId == id)) return false;
            TruckId = id;
            var truck = snapshot.trucks.Find(t => t.truckId == id);
            Paint = truck.hasCustomPaint ? truck.paint?.Copy() : null;
            if (Paint == null)
                foreach (var selection in truck.cosmetics)
                {
                    var cosmetic = catalog.FindCosmetic(selection.itemId);
                    if (selection.slotId != "paint" || cosmetic?.category != TruckTaxiCosmeticCategory.Paint ||
                        !cosmetic.HasContent || !TruckTaxiCareerCatalog.Compatible(cosmetic.compatibleTruckIds, id)) continue;
                    var value = new TruckTaxiGaragePaintData { color = cosmetic.paintColor };
                    if (value.Validate()) Paint = value;
                }
            return true;
        }
        public bool SetPaint(TruckTaxiGaragePaintData paint)
        {
            if (!IsActive || paint != null && !paint.Validate()) return false;
            Paint = paint?.Copy(); return true;
        }
        public bool Apply(TruckTaxiSession session, TruckTaxiCareerCatalog catalog, string canonicalTruckId, out string reason)
        {
            reason = "Garage draft no longer belongs to the active Career truck.";
            if (!IsActive || session == null || session.CareerRunProfileId != runId || session.EquippedTruckId != originalTruckId)
                return false;
            // Callers without the factory must still prove the selected actor is canonical.
            if (TruckId != canonicalTruckId)
            { reason = "This truck requires a canonical factory swap before it can be equipped."; return false; }
            if (!session.TryApplyGarage(catalog, TruckId, Paint, out reason)) return false;
            Cancel(); return true;
        }
        public bool Apply(TruckTaxiSession session, TruckTaxiCareerCatalog catalog, TruckTaxiTruckFactory factory, out string reason)
        {
            reason = "Garage draft no longer belongs to the active Career truck.";
            if (!IsActive || session == null || session.CareerRunProfileId != runId ||
                session.EquippedTruckId != originalTruckId || catalog == null || !catalog.Validate(out reason) ||
                !OwnedTrucks(session.CaptureCareerProfile(), catalog).Exists(t => t.StableId == TruckId) ||
                Paint != null && !Paint.Validate()) return false;
            if (factory == null) { reason = "Truck factory is unavailable."; return false; }
            if (TruckId != originalTruckId && !factory.TryEquip(TruckId, out reason, true)) return false;
            if (session.EquippedTruckId != TruckId)
            { reason = "Factory did not equip the selected Career truck."; return false; }
            // Keep a failed paint transaction retryable if the factory already changed the actor.
            originalTruckId = TruckId;
            return Apply(session, catalog, TruckId, out reason);
        }
        public void Cancel() { snapshot = null; Paint = null; TruckId = runId = originalTruckId = null; }
    }
}
