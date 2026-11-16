using System;
using System.Collections.Generic;

namespace LWS.TruckSports
{
    // Expansion-specific definitions use the established collection entry
    // contract without changing Alpha 0.7's original twenty-one definitions.
    public static class SportsDragonGames
    {
        static readonly SportsImpossibleGames.Entry[] entries =
        {
            new SportsImpossibleGames.Entry("dragonriders", "TruckDragonRiders", "TRUCK DRAGON RIDERS", "Saddle up. Fly the Skyfire Grand Prix. Rescue your falling truck."),
            new SportsImpossibleGames.Entry("hoardheist", "TruckHoardHeist", "TRUCK HOARD HEIST", "Lift real treasure. Mind the noise. Escape a waking dragon."),
            new SportsImpossibleGames.Entry("dragoneggdelivery", "TruckDragonEggDelivery", "TRUCK DRAGON EGG DELIVERY", "Carry precious dragon eggs home. Handle with care when they hatch."),
            new SportsImpossibleGames.Entry("dragonescape", "TruckDragonEscape", "TRUCK DRAGON ESCAPE", "Escape the kingdom by fortress, mountain refuge or departing boat.")
        };
        public static IReadOnlyList<SportsImpossibleGames.Entry> All => entries;
        public static bool TryFindKey(string key, out SportsImpossibleGames.Entry found)
        { foreach (var entry in entries) if (string.Equals(entry.key, key, StringComparison.Ordinal)) { found = entry; return true; } found = null; return false; }
        public static bool TryFindScene(string scene, out SportsImpossibleGames.Entry found)
        { foreach (var entry in entries) if (string.Equals(entry.scene, scene, StringComparison.Ordinal)) { found = entry; return true; } found = null; return false; }
    }
}
