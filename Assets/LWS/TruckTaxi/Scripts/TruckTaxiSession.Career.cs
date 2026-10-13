using System;
using System.Collections.Generic;

namespace LWS.TruckTaxi
{
    public sealed partial class TruckTaxiSession
    {
        private TruckTaxiCareerProfileData ownedProfile;
        private string boundCareerRunProfileId;
        private bool restoredWithoutCareerIdentity, careerMutationInProgress;
        public event Action CareerEquipmentChanged;
        public bool HasCareerIdentity => ownedProfile!=null;
        public string CareerRunProfileId => ownedProfile?.runProfileId;
        public string DriverName => ownedProfile?.driverName;
        public string EquippedTruckId => ownedProfile?.equippedTruckId;
        public TruckTaxiCareerProfileData CaptureCareerProfile() => ownedProfile?.Copy();
        public TruckTaxiDriverAppearanceData CaptureDriverAppearance() =>
            ownedProfile==null ? null : ownedProfile.driverAppearance?.Copy() ?? new TruckTaxiDriverAppearanceData();
        public bool TrySetDriverAppearance(TruckTaxiDriverAppearanceData appearance,out string reason)
        {
            if(!CanChangeCareer(out reason)) return false;
            if(appearance==null) { reason="Driver appearance is required."; return false; }
            if(!appearance.Validate(out reason)) return false;
            var next=ownedProfile.Copy(); next.driverAppearance=appearance.Copy();
            return CommitCareer(next,0,out reason);
        }

        // Parent passes the existing persistence RunProfileId. Never generate a second profile identity.
        public bool BindCareerRun(string runProfileId,out string reason)
        {
            reason="Career run/profile mismatch.";
            if(careerMutationInProgress || !TruckTaxiCareerProfileData.ValidId(runProfileId) ||
                boundCareerRunProfileId!=null && boundCareerRunProfileId!=runProfileId ||
                ownedProfile!=null && ownedProfile.runProfileId!=runProfileId) return false;
            boundCareerRunProfileId=runProfileId; reason=null; return true;
        }

        // currentTruckId must come from the actual spawned truck definition, not a fabricated catalog model.
        public bool TryInitializeCareer(string driverName,string currentTruckId,out string reason)
        {
            reason="Bind the active run before initializing its Career.";
            if(careerMutationInProgress || boundCareerRunProfileId==null || ownedProfile!=null ||
                !TruckTaxiCareerProfileData.ValidId(currentTruckId)) return false;
            if(!TruckTaxiCareerProfileData.TryNormalizeDriverName(driverName,out var normalized,out reason)) return false;
            var next=new TruckTaxiCareerProfileData {
                runProfileId=boundCareerRunProfileId,driverName=normalized,equippedTruckId=currentTruckId,
                firstGuideEligible=!restoredWithoutCareerIdentity
            };
            next.trucks.Add(new TruckTaxiOwnedTruck { truckId=currentTruckId });
            return CommitCareer(next,0,out reason);
        }

        public bool TrySetDriverName(string driverName,out string reason)
        {
            if(!CanChangeCareer(out reason) ||
                !TruckTaxiCareerProfileData.TryNormalizeDriverName(driverName,out var normalized,out reason)) return false;
            var next=ownedProfile.Copy(); next.driverName=normalized;
            return CommitCareer(next,0,out reason);
        }

        public bool TryClaimFirstDayGuide(out TruckTaxiFirstDayGuideLine line)
        {
            line=default;
            if(!CanChangeCareer(out _) || !ownedProfile.firstGuideEligible || ownedProfile.firstGuideClaimed) return false;
            var next=ownedProfile.Copy(); next.firstGuideClaimed=true;
            return CommitCareer(next,0,out _);
        }
        // Read-only replay seam: UI decides whether to expose this in development.
        public static TruckTaxiFirstDayGuideLine FirstDayGuidePreview => default;

        private bool CanChangeCareer(out string reason)
        {
            reason="Career is not bound or another transaction is in progress.";
            if(careerMutationInProgress || ownedProfile==null || boundCareerRunProfileId!=ownedProfile.runProfileId) return false;
            if(State!=TruckTaxiState.Available && State!=TruckTaxiState.Inactive || RaceEventActive || HasStolenPassenger)
            { reason="Finish the active fare or race before changing equipment."; return false; }
            reason=null; return true;
        }

        private bool CatalogReady(TruckTaxiCareerCatalog catalog,out string reason)
        {
            if(!CanChangeCareer(out reason)) return false;
            if(catalog==null) { reason="No authored Career catalog."; return false; }
            return catalog.Validate(out reason);
        }

        public bool TryPurchaseTruck(TruckTaxiCareerCatalog catalog,string truckId,out string reason)
        {
            if(!CatalogReady(catalog,out reason)) return false;
            var definition=catalog.FindTruck(truckId);
            if(definition==null || !definition.availableForPurchase || !definition.HasContent)
            { reason="Truck content is not available for purchase."; return false; }
            if(ownedProfile.trucks.Exists(t=>t.truckId==truckId))
            { reason="Truck is already owned."; return false; }
            var next=ownedProfile.Copy();
            next.trucks.Add(new TruckTaxiOwnedTruck { truckId=truckId });
            return CommitCareer(next,definition.priceCents,out reason);
        }

        // Selection only. Parent must validate the swap location and apply through its existing spawner.
        public bool TryEquipTruck(TruckTaxiCareerCatalog catalog,string truckId,out string reason)
        {
            if(!CatalogReady(catalog,out reason)) return false;
            if(!ownedProfile.trucks.Exists(t=>t.truckId==truckId) || catalog.FindTruck(truckId)?.HasContent!=true)
            { reason="Truck is not owned or its real prefab is unavailable."; return false; }
            var next=ownedProfile.Copy(); next.equippedTruckId=truckId;
            return CommitCareer(next,0,out reason);
        }

        public bool TryPurchaseUpgrade(TruckTaxiCareerCatalog catalog,string itemId,out string reason)
        {
            if(!CatalogReady(catalog,out reason)) return false;
            var definition=catalog.FindUpgrade(itemId);
            if(!UpgradeAvailable(definition) || !definition.availableForPurchase)
            { reason="Upgrade is unavailable or incompatible with this truck."; return false; }
            var next=ownedProfile.Copy(); var truck=SelectedTruck(next);
            if(truck.ownedUpgradeIds.Contains(itemId)) { reason="Upgrade is already owned for this truck."; return false; }
            truck.ownedUpgradeIds.Add(itemId);
            return CommitCareer(next,definition.priceCents,out reason);
        }
        public bool TryEquipUpgrade(TruckTaxiCareerCatalog catalog,string itemId,out string reason)
        {
            if(!CatalogReady(catalog,out reason)) return false;
            var definition=catalog.FindUpgrade(itemId);
            if(!UpgradeAvailable(definition) || !SelectedTruck(ownedProfile).ownedUpgradeIds.Contains(itemId))
            { reason="Upgrade is not owned, configured or compatible."; return false; }
            var next=ownedProfile.Copy();
            Select(SelectedTruck(next).upgrades,definition.SlotId,itemId);
            return CommitCareer(next,0,out reason);
        }
        private bool UpgradeAvailable(TruckTaxiCareerUpgradeDefinition definition) => definition!=null &&
            definition.adapterConfiguration!=null && TruckTaxiCareerProfileData.ValidId(definition.adapterId) &&
            TruckTaxiCareerCatalog.Compatible(definition.compatibleTruckIds,ownedProfile.equippedTruckId);

        public bool TryPurchaseCosmetic(TruckTaxiCareerCatalog catalog,string itemId,out string reason)
        {
            if(!CatalogReady(catalog,out reason)) return false;
            var definition=catalog.FindCosmetic(itemId);
            if(!CosmeticAvailable(definition) || !definition.availableForPurchase)
            { reason="Cosmetic content is unavailable or incompatible."; return false; }
            var next=ownedProfile.Copy();
            var owned=definition.IsHeadwear ? next.ownedHeadwearIds : SelectedTruck(next).ownedCosmeticIds;
            if(owned.Contains(itemId)) { reason="Cosmetic is already owned."; return false; }
            owned.Add(itemId);
            return CommitCareer(next,definition.priceCents,out reason);
        }
        public bool TryEquipCosmetic(TruckTaxiCareerCatalog catalog,string itemId,out string reason)
        {
            if(!CatalogReady(catalog,out reason)) return false;
            var definition=catalog.FindCosmetic(itemId);
            if(!CosmeticAvailable(definition)) { reason="Cosmetic content is unavailable or incompatible."; return false; }
            var next=ownedProfile.Copy();
            var owned=definition.IsHeadwear ? next.ownedHeadwearIds : SelectedTruck(next).ownedCosmeticIds;
            if(!owned.Contains(itemId)) { reason="Cosmetic is not owned."; return false; }
            if(definition.IsHeadwear) next.equippedHeadwearId=itemId;
            else
            {
                Select(SelectedTruck(next).cosmetics,definition.SlotId,itemId);
                if(definition.category==TruckTaxiCosmeticCategory.Paint)
                { SelectedTruck(next).hasCustomPaint=false; SelectedTruck(next).paint=null; }
            }
            return CommitCareer(next,0,out reason);
        }
        private bool CosmeticAvailable(TruckTaxiCareerCosmeticDefinition definition) => definition!=null &&
            definition.HasContent && TruckTaxiCareerProfileData.ValidId(definition.adapterId) &&
            (definition.IsHeadwear || TruckTaxiCareerCatalog.Compatible(definition.compatibleTruckIds,ownedProfile.equippedTruckId));

        public bool TryUnequipUpgrade(TruckTaxiUpgradeCategory category,out string reason) =>
            Unequip(TruckTaxiCareerCatalog.UpgradeSlot(category),true,out reason);
        public bool TryUnequipCosmetic(TruckTaxiCosmeticCategory category,out string reason) =>
            Unequip(TruckTaxiCareerCatalog.CosmeticSlot(category),false,out reason);
        private bool Unequip(string slot,bool upgrade,out string reason)
        {
            if(!CanChangeCareer(out reason)) return false;
            if(slot==null) { reason="Unknown equipment category."; return false; }
            var next=ownedProfile.Copy();
            if(!upgrade && slot=="paint") { SelectedTruck(next).hasCustomPaint=false; SelectedTruck(next).paint=null; }
            if(!upgrade && slot=="player-headwear") next.equippedHeadwearId=null;
            else (upgrade ? SelectedTruck(next).upgrades : SelectedTruck(next).cosmetics).RemoveAll(s=>s.slotId==slot);
            return CommitCareer(next,0,out reason);
        }
        private static TruckTaxiOwnedTruck SelectedTruck(TruckTaxiCareerProfileData data) =>
            data.trucks.Find(t=>t.truckId==data.equippedTruckId);
        private static void Select(List<TruckTaxiCareerSelection> selections,string slot,string item)
        {
            selections.RemoveAll(s=>s.slotId==slot);
            selections.Add(new TruckTaxiCareerSelection { slotId=slot,itemId=item });
        }

        private bool CommitCareer(TruckTaxiCareerProfileData next,long price,out string reason)
        {
            reason="Career transaction is not available.";
            if(careerMutationInProgress || next==null || next.runProfileId!=boundCareerRunProfileId || price<0) return false;
            if(!next.Validate(out reason)) return false;
            if(price>0 && price>WalletBalanceCents) { reason="Insufficient funds."; return false; }
            long charges;
            try { charges=checked(purchaseCharges+price); }
            catch(OverflowException) { reason="Purchase total would overflow."; return false; }
            // Same Session wallet fields as TrySpend. Publish only after ownership AND debit commit,
            // so a Changed listener cannot observe half a purchase or buy the same item reentrantly.
            careerMutationInProgress=true;
            try { ownedProfile=next; purchaseCharges=charges; CareerEquipmentChanged?.Invoke(); Changed?.Invoke(); }
            finally { careerMutationInProgress=false; }
            reason=null; return true;
        }
    }
}
