using System;

namespace LWS.TruckTaxi
{
    public enum TruckTaxiNeedsItem { EmptyBottle, EmptyPissJug, FilledBottle, FilledJug, WaterBottle, Soda, EnergyDrink, Snack, Burger, Sandwich, Meal, HighOctaneSuppository, MysteryMushrooms, AirFreshener, HulaFigure, NoveltyDuck }
    public enum TruckTaxiCabSlot { DashboardLeft, DashboardRight, Mirror }

    public sealed class TruckTaxiNeedsItemDefinition
    {
        public readonly TruckTaxiNeedsItem Item;
        public readonly string Name;
        public readonly long PriceCents;
        public readonly float HungerRelief, ThirstRelief;
        public readonly bool CreatesBottle;
        public readonly TruckTaxiCabSlot? Slot;
        public TruckTaxiNeedsItemDefinition(TruckTaxiNeedsItem item, string name, long priceCents,
            float hungerRelief = 0, float thirstRelief = 0, bool createsBottle = false, TruckTaxiCabSlot? slot = null)
        { Item = item; Name = name; PriceCents = priceCents; HungerRelief = hungerRelief; ThirstRelief = thirstRelief; CreatesBottle = createsBottle; Slot = slot; }
    }

    // This fixed catalog is the entire store vocabulary, not a general inventory system.
    public static class TruckTaxiNeedsItems
    {
        public static readonly TruckTaxiNeedsItemDefinition[] Store =
        {
            new TruckTaxiNeedsItemDefinition(TruckTaxiNeedsItem.WaterBottle, "Water bottle", 225, thirstRelief: .36f, createsBottle: true),
            new TruckTaxiNeedsItemDefinition(TruckTaxiNeedsItem.Soda, "Soda", 275, thirstRelief: .27f, createsBottle: true),
            new TruckTaxiNeedsItemDefinition(TruckTaxiNeedsItem.EnergyDrink, "Energy drink", 425, thirstRelief: .31f, createsBottle: true),
            new TruckTaxiNeedsItemDefinition(TruckTaxiNeedsItem.Snack, "Snack", 250, hungerRelief: .24f),
            new TruckTaxiNeedsItemDefinition(TruckTaxiNeedsItem.Burger, "Burger", 700, hungerRelief: .55f),
            new TruckTaxiNeedsItemDefinition(TruckTaxiNeedsItem.Sandwich, "Sandwich", 550, hungerRelief: .43f),
            new TruckTaxiNeedsItemDefinition(TruckTaxiNeedsItem.Meal, "Hot meal", 1100, hungerRelief: .78f),
            new TruckTaxiNeedsItemDefinition(TruckTaxiNeedsItem.EmptyPissJug, "Dedicated piss jug", 450),
            new TruckTaxiNeedsItemDefinition(TruckTaxiNeedsItem.EmptyBottle, "Empty bottle", 100),
            new TruckTaxiNeedsItemDefinition(TruckTaxiNeedsItem.AirFreshener, "Air freshener", 350, slot: TruckTaxiCabSlot.Mirror),
            new TruckTaxiNeedsItemDefinition(TruckTaxiNeedsItem.HulaFigure, "Dashboard dancer", 1250, slot: TruckTaxiCabSlot.DashboardLeft),
            new TruckTaxiNeedsItemDefinition(TruckTaxiNeedsItem.NoveltyDuck, "Novelty duck", 800, slot: TruckTaxiCabSlot.DashboardRight),
            new TruckTaxiNeedsItemDefinition(TruckTaxiNeedsItem.HighOctaneSuppository, "High-octane energy suppository", 999),
            new TruckTaxiNeedsItemDefinition(TruckTaxiNeedsItem.MysteryMushrooms, "Mystery mushrooms", 666)
        };
        public static TruckTaxiNeedsItemDefinition Find(TruckTaxiNeedsItem item)
        { foreach (var entry in Store) if (entry.Item == item) return entry; return null; }
    }
}
