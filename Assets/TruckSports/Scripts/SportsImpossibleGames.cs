using System;
using System.Collections.Generic;

namespace LWS.TruckSports
{
    // One definition per new sport for the existing navigation, build and menu
    // adapters. Definitions alone never create a scene or a selectable card.
    public static class SportsImpossibleGames
    {
        public sealed class Entry
        {
            public readonly string key, scene, title, description;
            public string ScenePath => "Assets/" + scene + "/Scenes/" + scene + ".unity";
            public Entry(string key, string scene, string title, string description)
            { this.key = key; this.scene = scene; this.title = title; this.description = description; }
        }
        static readonly Entry[] entries =
        {
            new Entry("goose", "TruckTruckGoose", "TRUCK TRUCK GOOSE", "Pick a goose. Race opposite ways. Claim the empty parking bay."),
            new Entry("submarine", "TruckSubmarine", "TRUCK SUBMARINE", "Dive, discover real salvage and return your cargo to the surface."),
            new Entry("moonmadness", "TruckMoonMadness", "TRUCK MOON MADNESS", "Low gravity, limited landing jets and physical lunar flag deliveries."),
            new Entry("pinatapanic", "TruckPinataPanic", "TRUCK PIÑATA PANIC", "Strike a swinging piñata. Chase sweets and dodge explosive candy."),
            new Entry("eggspoon", "TruckEggSpoonRace", "TRUCK EGG & SPOON RACE", "Carry a real fragile egg across a countryside obstacle race."),
            new Entry("tornadorodeo", "TruckTornadoRodeo", "TRUCK TORNADO RODEO", "Ride the funnel, pass aerial gates and earn a controlled landing."),
            new Entry("mechanicalbull", "TruckMechanicalBull", "TRUCK MECHANICAL BULL", "Counterlean a bucking longhorn. Stay on its moving saddle."),
            new Entry("guessthat", "TruckGuessThat", "GUESS THAT TRUCK!", "Ask truthful questions and fold away the wrong truck identities."),
            new Entry("jenga", "TruckJenga", "TRUCK JENGA", "Winch real containers from the tower, then crane them onto the top."),
            new Entry("camping", "TruckCamping", "TRUCK CAMPING", "Explore a shared mountain wilderness and make a campsite your own."),
            new Entry("teaparty", "TruckTeaParty", "TRUCK TEA PARTY", "Pour carefully, carry porcelain and serve your guests with courtesy."),
            new Entry("dentist", "TruckDentist", "TRUCK DENTIST", "Clean, repair and carefully treat an enormous cartoon smile."),
            new Entry("ballet", "TruckBallet", "TRUCK BALLET", "Hold elegant truck poses and perform deliberate stage choreography."),
            new Entry("bakeoff", "TruckBakeOff", "TRUCK BAKE-OFF", "Mix, bake, decorate and present your own ridiculous creation."),
            new Entry("fashionshow", "TruckFashionShow", "TRUCK FASHION SHOW", "Style your named truck and make an expressive runway entrance."),
            new Entry("yoga", "TruckYoga", "TRUCK YOGA", "Balance seven truck poses or relax in an untimed meadow class."),
            new Entry("nestbuilding", "TruckNestBuilding", "TRUCK NEST BUILDING", "Carry branches, build real supports and welcome a tiny truck family."),
            new Entry("snowmanbuilding", "TruckSnowmanBuilding", "TRUCK SNOWMAN BUILDING", "Roll real snowballs. Stack, dress and save your winter creation."),
            new Entry("archaeology", "TruckArchaeology", "TRUCK ARCHAEOLOGY", "Excavate actual terrain and carefully uncover buried artifacts."),
            new Entry("orchestra", "TruckOrchestra", "TRUCK ORCHESTRA", "Make an absurd truck ensemble and perform together."),
            new Entry("petpals", "TruckPetPals", "TRUCK PET PALS", "Care for, train and explore with a companion in the shared wilderness.")
        };
        public static IReadOnlyList<Entry> All => entries;
        public static bool TryFindKey(string key, out Entry found)
        { foreach (var entry in entries) if (string.Equals(entry.key, key, StringComparison.Ordinal)) { found = entry; return true; } found = null; return false; }
        public static bool TryFindScene(string scene, out Entry found)
        { foreach (var entry in entries) if (string.Equals(entry.scene, scene, StringComparison.Ordinal)) { found = entry; return true; } found = null; return false; }
    }
}
