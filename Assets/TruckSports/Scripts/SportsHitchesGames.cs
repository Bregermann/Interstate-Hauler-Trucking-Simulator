using System;
using System.Collections.Generic;

namespace LWS.TruckSports
{
    // Stable catalog identity only. Registration is not a completion certificate.
    // The acceptance ledger independently records actual gameplay verification.
    public static class SportsHitchesGames
    {
        public enum Family { Water, Horror, Trailer }
        public sealed class Entry
        {
            public readonly string id, key, scene, title, description, ScenePath;
            public readonly Family family;
            public Entry(int number, string key, string scene, string title, string description, Family family, string path)
            { id = "HH10-" + number.ToString("00"); this.key = key; this.scene = scene; this.title = title; this.description = description; this.family = family; ScenePath = path; }
        }
        static Entry Water(int n, string key, string scene, string title, string text,Family family=Family.Water) => new Entry(n, key, scene, title, text, family, "Assets/TruckWaterSports/Scenes/" + scene + ".unity");
        static Entry Horror(int n, string key, string scene, string title, string text,Family family=Family.Horror) => new Entry(n, key, scene, title, text, family, "Assets/TruckHorrorSports/" + scene + "/Scenes/" + scene + ".unity");
        static Entry Hitch(int n, string key, string scene, string title, string text) => new Entry(n, key, scene, title, text, Family.Trailer, "Assets/TruckHitchSports/Scenes/" + scene + ".unity");
        static Entry Extreme(int n, string key, string scene, string title, string text) => new Entry(n, key, scene, title, text, Family.Trailer, "Assets/TruckExtremeHauling/Scenes/" + scene + ".unity");
        static readonly Entry[] entries =
        {
            Water(1,"paddlepals","PaddlePals","TRUCK CANOEING: PADDLE PALS","Two paddlers. One enormous canoe. Coordinate your strokes through the rapids."),
            Water(2,"whitewaterwipeout","WhitewaterWipeout","TRUCK KAYAKING: WHITEWATER WIPEOUT","Brace, roll and carve a truck-sized kayak through wild whitewater."),
            Horror(3,"graveyardshift","GraveyardShift","GRAVEYARD SHIFT: THE LAST STOP","Finish the overnight truck-stop shift. Something has arrived without a driver."),
            Horror(4,"scarecrowseason","ScarecrowSeason","TRUCK HARVEST: SCARECROW SEASON","Find the ritual objects. Watch your headlights. Escape the corn maze."),
            Horror(5,"deadend","DeadEnd","DEAD END: THE LAST HEADLIGHT","An impossible highway. A driverless semi. Find the road that leads home."),
            Water(6,"blackwater","Blackwater","BLACKWATER: SOMETHING BELOW","Carry supplies across a midnight lake while something moves beneath your boat.",Family.Horror),
            Hitch(7,"trailerblazers","Trailerblazers","TRAILERBLAZERS: ROCKET LAUNCH","Ignite trailer-mounted boosters. Separate the stage. Bring the rig home."),
            Hitch(8,"jackknifehighwire","JackknifeHighwire","JACKKNIFE HIGHWIRE","Control an articulated trailer across an unforgiving elevated freight route."),
            Hitch(9,"hitchandrun","HitchAndRun","HITCH & RUN","Line up the kingpin, lock the fifth wheel and deliver the contested cargo."),
            Hitch(10,"trailertrebuchet","TrailerTrebuchet","TRAILER TREBUCHET","Load a real siege engine and launch a full-size trailer at the castle."),
            Hitch(11,"hitchhandoff","HitchHandoff","HITCH HANDOFF","The trailer is the baton. Brake, uncouple and make the relay exchange."),
            Hitch(12,"unhitched","Unhitched","UNHITCHED!","Balance a tractor on a moving trailer platform. Land it. Keep the ride alive."),
            Extreme(13,"cargocrisis","CargoCrisis","CARGO CRISIS","Winch stranded freight upright, reconnect and complete the rescue delivery."),
            Horror(14,"cursedcargo","CursedCargo","CURSED CARGO","Your trailer is fighting back. Contain the load and survive the delivery.",Family.Trailer),
            Extreme(15,"dominoderby","DominoDerby","DOMINO DERBY","Arrange upright trailers and unleash a genuine freight-sized chain reaction."),
            Extreme(16,"trailerplinko","TrailerPlinko","TRAILER PLINKO","Choose the drop. Set the spin. Let the pegs decide where the trailer lands."),
            Extreme(17,"jackknifeonice","JackknifeOnIce","JACKKNIFE ON ICE","Turn trailer sway into a controlled, scoring slide across polished ice."),
            Extreme(18,"roadtrainwreck","RoadTrainWreck","ROAD TRAIN WRECK","Three articulated trailers, tight turns and very real consequences."),
            Extreme(19,"hitchringtoss","HitchRingToss","HITCH RING TOSS","Build swing momentum and release a trailer through a towering target ring."),
            Extreme(20,"zeroghitchup","ZeroGHitchUp","ZERO-G HITCH-UP","Match velocity, align the kingpin and dock orbital freight with thrusters."),
            Extreme(21,"skyhooksprint","SkyhookSprint","SKYHOOK SPRINT","Control a suspended cargo swing, release the cable and land the delivery."),
            Extreme(22,"iceroadreckoning","IceRoadReckoning","ICE ROAD RECKONING: THIN ICE","Read the cracking ice. Manage a heavy load. Get the freight to shore.")
        };
        public static IReadOnlyList<Entry> All => entries;
        public static bool TryFindKey(string key, out Entry found) { foreach (var e in entries) if (string.Equals(e.key,key,StringComparison.Ordinal)) { found=e; return true; } found=null; return false; }
        public static bool TryFindScene(string scene, out Entry found) { foreach (var e in entries) if (string.Equals(e.scene,scene,StringComparison.Ordinal)) { found=e; return true; } found=null; return false; }
    }
}
