using System;
using System.Collections.Generic;
using LWS.InterstateHauler;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public enum TruckTaxiUpgradeCategory { Engine, Transmission, Brakes, Fuel, Suspension, Tires, Durability }
    public enum TruckTaxiCosmeticCategory { Paint, ExteriorAccessory, InteriorAccessory, Wheels, CabDecoration, PlayerHeadwear }

    [Serializable]
    public sealed class TruckTaxiCareerTruckDefinition
    {
        public string stableId, displayName;
        [Tooltip("Reuse the existing definition ID, display name and real player prefab.")]
        public LwsTruckDefinition truck;
        [TextArea] public string description;
        public Sprite icon;
        [Min(0)] public long priceCents;
        [Tooltip("Leave false for future/unimplemented content.")]
        public bool availableForPurchase;
        public string StableId => !string.IsNullOrEmpty(stableId) ? stableId : truck!=null ? truck.StableId : null;
        public string DisplayName => !string.IsNullOrEmpty(displayName) ? displayName : truck!=null ? truck.DisplayName : StableId;
        public bool HasContent => truck!=null && truck.PlayerPrefab!=null && truck.StableId==StableId;
    }

    [Serializable]
    public sealed class TruckTaxiCareerUpgradeDefinition
    {
        public string stableId, displayName;
        [TextArea] public string description;
        public Sprite icon;
        [Min(0)] public long priceCents;
        public TruckTaxiUpgradeCategory category;
        public string[] compatibleTruckIds=Array.Empty<string>();
        [Tooltip("Explicit LWS installer key; not a type name for reflection.")]
        public string adapterId;
        [Tooltip("Existing authored adapter definition, e.g. Lws18SpeedTransmissionDefinition. No automatic NWH mutation.")]
        public ScriptableObject adapterConfiguration;
        [Tooltip("Enable only after the parent installer supports this definition.")]
        public bool availableForPurchase;
        public string SlotId => TruckTaxiCareerCatalog.UpgradeSlot(category);
    }

    [Serializable]
    public sealed class TruckTaxiCareerCosmeticDefinition
    {
        public string stableId, displayName;
        [TextArea] public string description;
        public Sprite icon;
        [Min(0)] public long priceCents;
        public TruckTaxiCosmeticCategory category;
        public string[] compatibleTruckIds=Array.Empty<string>();
        public string adapterId;
        [Tooltip("Use an existing cab anchor ID for cab content; headwear belongs to the player.")]
        public string mountId;
        public GameObject prefab;
        public Color paintColor=Color.white;
        [Tooltip("Leave false until real content and a parent presentation installer are available.")]
        public bool availableForPurchase;
        public bool IsHeadwear => category==TruckTaxiCosmeticCategory.PlayerHeadwear;
        public string SlotId => TruckTaxiCareerCatalog.CosmeticSlot(category);
        public bool HasContent => category==TruckTaxiCosmeticCategory.Paint ?
            float.IsFinite(paintColor.r) && float.IsFinite(paintColor.g) && float.IsFinite(paintColor.b) &&
            float.IsFinite(paintColor.a) : prefab!=null && TruckTaxiCareerProfileData.ValidId(mountId);
    }

    [CreateAssetMenu(menuName="Truck Taxi/Career/Catalog")]
    public sealed class TruckTaxiCareerCatalog : ScriptableObject
    {
        public TruckTaxiCareerTruckDefinition[] trucks=Array.Empty<TruckTaxiCareerTruckDefinition>();
        public TruckTaxiCareerUpgradeDefinition[] upgrades=Array.Empty<TruckTaxiCareerUpgradeDefinition>();
        public TruckTaxiCareerCosmeticDefinition[] cosmetics=Array.Empty<TruckTaxiCareerCosmeticDefinition>();

        public bool Validate(out string reason)
        {
            reason="Invalid or duplicate Career catalog identity, price or category.";
            if(trucks==null || upgrades==null || cosmetics==null) return false;
            var ids=new HashSet<string>(StringComparer.Ordinal);
            foreach(var item in trucks)
                if(item==null || !TruckTaxiCareerProfileData.ValidId(item.StableId) || !ids.Add(item.StableId) || item.priceCents<0 ||
                    (item.truck!=null && item.truck.StableId!=item.StableId)) return false;
            foreach(var item in upgrades)
                if(item==null || !TruckTaxiCareerProfileData.ValidId(item.stableId) || !ids.Add(item.stableId) ||
                    item.priceCents<0 || item.SlotId==null || !ValidCompatibility(item.compatibleTruckIds)) return false;
            foreach(var item in cosmetics)
                if(item==null || !TruckTaxiCareerProfileData.ValidId(item.stableId) || !ids.Add(item.stableId) ||
                    item.priceCents<0 || item.SlotId==null || !ValidCompatibility(item.compatibleTruckIds)) return false;
            reason=null; return true;
        }
        private static bool ValidCompatibility(string[] ids) =>
            ids!=null && TruckTaxiCareerProfileData.UniqueIds(new List<string>(ids));
        public static bool Compatible(string[] ids,string truckId) => ids!=null && Array.IndexOf(ids,truckId)>=0;
        public TruckTaxiCareerTruckDefinition FindTruck(string id) => trucks==null ? null : Array.Find(trucks,t=>t!=null && t.StableId==id);
        public TruckTaxiCareerUpgradeDefinition FindUpgrade(string id) => upgrades==null ? null : Array.Find(upgrades,t=>t!=null && t.stableId==id);
        public TruckTaxiCareerCosmeticDefinition FindCosmetic(string id) => cosmetics==null ? null : Array.Find(cosmetics,t=>t!=null && t.stableId==id);
        public static string UpgradeSlot(TruckTaxiUpgradeCategory category)
        {
            switch(category)
            {
                case TruckTaxiUpgradeCategory.Engine: return "engine";
                case TruckTaxiUpgradeCategory.Transmission: return "transmission";
                case TruckTaxiUpgradeCategory.Brakes: return "brakes";
                case TruckTaxiUpgradeCategory.Fuel: return "fuel";
                case TruckTaxiUpgradeCategory.Suspension: return "suspension";
                case TruckTaxiUpgradeCategory.Tires: return "tires";
                case TruckTaxiUpgradeCategory.Durability: return "durability";
                default: return null;
            }
        }
        public static string CosmeticSlot(TruckTaxiCosmeticCategory category)
        {
            switch(category)
            {
                case TruckTaxiCosmeticCategory.Paint: return "paint";
                case TruckTaxiCosmeticCategory.ExteriorAccessory: return "exterior-accessory";
                case TruckTaxiCosmeticCategory.InteriorAccessory: return "interior-accessory";
                case TruckTaxiCosmeticCategory.Wheels: return "wheels";
                case TruckTaxiCosmeticCategory.CabDecoration: return "cab-decoration";
                case TruckTaxiCosmeticCategory.PlayerHeadwear: return "player-headwear";
                default: return null;
            }
        }
        internal static bool IsUpgradeSlot(string id) => id=="engine" || id=="transmission" || id=="brakes" ||
            id=="fuel" || id=="suspension" || id=="tires" || id=="durability";
        internal static bool IsTruckCosmeticSlot(string id) => id=="paint" || id=="exterior-accessory" ||
            id=="interior-accessory" || id=="wheels" || id=="cab-decoration";
    }
}
