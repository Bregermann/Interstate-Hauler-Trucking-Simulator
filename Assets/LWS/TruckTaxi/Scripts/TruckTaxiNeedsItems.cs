using System;

namespace LWS.TruckTaxi
{
    public enum TruckTaxiNeedsItem { EmptyBottle, EmptyPissJug, FilledBottle, FilledJug, WaterBottle, Soda, EnergyDrink, Snack, Burger, Sandwich, Meal, HighOctaneSuppository, MysteryMushrooms, AirFreshener, HulaFigure, NoveltyDuck, RoadPep }
    public enum TruckTaxiCabSlot { DashboardLeft, DashboardRight, Mirror }
    public enum TruckTaxiItemCategory { All, Food, Drinks, Cab, Supplies, Specials }
    [Flags]
    public enum TruckTaxiItemFlags { None = 0, NonInventory = 1, DebugOnly = 2, NotPlayerOwnable = 4 }

    public sealed class TruckTaxiNeedsItemDefinition
    {
        public readonly TruckTaxiNeedsItem Item;
        public readonly string StableId;
        public readonly TruckTaxiItemFlags Flags;
        public readonly bool Purchasable;
        public bool PlayerOwnable => (Flags & (TruckTaxiItemFlags.NonInventory | TruckTaxiItemFlags.DebugOnly | TruckTaxiItemFlags.NotPlayerOwnable)) == 0;
        public bool IsDrink => ThirstRelief > 0;
        public readonly string Name;
        public readonly long PriceCents;
        public readonly float HungerRelief, ThirstRelief, BowelLoad;
        public readonly bool CreatesBottle;
        public readonly TruckTaxiCabSlot? Slot;
        public TruckTaxiItemCategory Category => Slot.HasValue ? TruckTaxiItemCategory.Cab :
            HungerRelief>0 ? TruckTaxiItemCategory.Food : IsDrink ? TruckTaxiItemCategory.Drinks :
            Item==TruckTaxiNeedsItem.HighOctaneSuppository || Item==TruckTaxiNeedsItem.MysteryMushrooms || Item==TruckTaxiNeedsItem.RoadPep ?
                TruckTaxiItemCategory.Specials : TruckTaxiItemCategory.Supplies;
        public string UseSummary => HungerRelief>0 ? "Hunger relief: "+(HungerRelief*100).ToString("0")+"%" :
            IsDrink ? "Thirst relief: "+(ThirstRelief*100).ToString("0")+"%" :
            Slot.HasValue ? "Cab decoration" : Item==TruckTaxiNeedsItem.FilledBottle || Item==TruckTaxiNeedsItem.FilledJug ?
                "Filled container" : Category==TruckTaxiItemCategory.Specials ? "Temporary special effect" : "Container";
        public TruckTaxiNeedsItemDefinition(TruckTaxiNeedsItem item, string name, long priceCents,
            float hungerRelief = 0, float thirstRelief = 0, bool createsBottle = false, TruckTaxiCabSlot? slot = null,
            string stableId = null, bool purchasable = true, TruckTaxiItemFlags flags = TruckTaxiItemFlags.None, float bowelLoad = 0)
        { Item = item; Name = name; PriceCents = priceCents; HungerRelief = hungerRelief; ThirstRelief = thirstRelief; CreatesBottle = createsBottle; Slot = slot;
            StableId = stableId; Purchasable = purchasable; Flags = flags;
            BowelLoad = float.IsFinite(bowelLoad) ? UnityEngine.Mathf.Clamp01(bowelLoad) : 0; }
    }

    // One registry; store availability is derived from ownership and purchase metadata.
    public static class TruckTaxiNeedsItems
    {
        public const float DrinkThirstRelief = .05f;
        public static readonly TruckTaxiNeedsItemDefinition[] All =
        {
            new TruckTaxiNeedsItemDefinition(TruckTaxiNeedsItem.WaterBottle, "Water bottle", 225, thirstRelief: DrinkThirstRelief, createsBottle: true, stableId: "water-bottle", bowelLoad: .015f),
            new TruckTaxiNeedsItemDefinition(TruckTaxiNeedsItem.Soda, "Soda", 275, thirstRelief: DrinkThirstRelief, createsBottle: true, stableId: "soda", bowelLoad: .04f),
            new TruckTaxiNeedsItemDefinition(TruckTaxiNeedsItem.EnergyDrink, "Energy drink", 425, thirstRelief: DrinkThirstRelief, createsBottle: true, stableId: "energy-drink", bowelLoad: .08f),
            new TruckTaxiNeedsItemDefinition(TruckTaxiNeedsItem.Snack, "Snack", 250, hungerRelief: .24f, stableId: "snack", bowelLoad: .08f),
            new TruckTaxiNeedsItemDefinition(TruckTaxiNeedsItem.Burger, "Burger", 700, hungerRelief: .55f, stableId: "burger", bowelLoad: .18f),
            new TruckTaxiNeedsItemDefinition(TruckTaxiNeedsItem.Sandwich, "Sandwich", 550, hungerRelief: .43f, stableId: "sandwich", bowelLoad: .12f),
            new TruckTaxiNeedsItemDefinition(TruckTaxiNeedsItem.Meal, "Hot meal", 1100, hungerRelief: .78f, stableId: "hot-meal", bowelLoad: .24f),
            new TruckTaxiNeedsItemDefinition(TruckTaxiNeedsItem.EmptyPissJug, "Dedicated piss jug", 450, stableId: "empty-jug"),
            new TruckTaxiNeedsItemDefinition(TruckTaxiNeedsItem.EmptyBottle, "Empty bottle", 100, stableId: "empty-bottle"),
            new TruckTaxiNeedsItemDefinition(TruckTaxiNeedsItem.AirFreshener, "Air freshener", 350, slot: TruckTaxiCabSlot.Mirror, stableId: "air-freshener"),
            new TruckTaxiNeedsItemDefinition(TruckTaxiNeedsItem.HulaFigure, "Dashboard dancer", 1250, slot: TruckTaxiCabSlot.DashboardLeft, stableId: "dashboard-dancer"),
            new TruckTaxiNeedsItemDefinition(TruckTaxiNeedsItem.NoveltyDuck, "Novelty duck", 800, slot: TruckTaxiCabSlot.DashboardRight, stableId: "novelty-duck"),
            new TruckTaxiNeedsItemDefinition(TruckTaxiNeedsItem.HighOctaneSuppository, "High-octane energy suppository", 999, stableId: "high-octane"),
            new TruckTaxiNeedsItemDefinition(TruckTaxiNeedsItem.MysteryMushrooms, "Mystery mushrooms", 666, stableId: "mystery-mushrooms"),
            new TruckTaxiNeedsItemDefinition(TruckTaxiNeedsItem.RoadPep, "Road Pep", 1800, stableId: "road-pep"),
            new TruckTaxiNeedsItemDefinition(TruckTaxiNeedsItem.FilledBottle, "Filled bottle", 0, stableId: "filled-bottle", purchasable: false),
            new TruckTaxiNeedsItemDefinition(TruckTaxiNeedsItem.FilledJug, "Filled jug", 0, stableId: "filled-jug", purchasable: false)
        };
        public static readonly TruckTaxiNeedsItemDefinition[] Store = Array.FindAll(All, entry => entry.PlayerOwnable && entry.Purchasable);
        public static TruckTaxiNeedsItemDefinition Find(TruckTaxiNeedsItem item)
        { foreach (var entry in All) if (entry.Item == item) return entry; return null; }
        public static TruckTaxiNeedsItemDefinition Find(string stableId)
        { foreach (var entry in All) if (string.Equals(entry.StableId, stableId, StringComparison.Ordinal)) return entry; return null; }
        public static TruckTaxiNeedsItemDefinition FindByStableId(string stableId) => Find(stableId);
    }
}
