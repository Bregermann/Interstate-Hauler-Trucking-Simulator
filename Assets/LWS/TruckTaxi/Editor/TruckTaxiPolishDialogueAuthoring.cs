using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace LWS.TruckTaxi.Editor
{
    public static class TruckTaxiPolishDialogueAuthoring
    {
        private sealed class Entry
        {
            public readonly string passengerId, lineId, text;
            public readonly TruckTaxiDialogueCategory category;
            public readonly TruckTaxiVoiceEmotion emotion;
            public Entry(string id, string category, string text, TruckTaxiVoiceEmotion emotion = TruckTaxiVoiceEmotion.Neutral)
            {
                passengerId = id;
                lineId = "regional-polish-" + category.ToLowerInvariant();
                this.category = (TruckTaxiDialogueCategory)Enum.Parse(typeof(TruckTaxiDialogueCategory), category);
                this.text = text;
                this.emotion = emotion;
            }
        }

        private static readonly Entry[] Entries = {
            E("analytical-robot", "WrongWayReaction", "The route is moving away from the destination. Recalculating my confidence in that turn.", TruckTaxiVoiceEmotion.Annoyed),
            E("quiet-hero", "WrongWayReaction", "I think we just left the destination behind. Could we take the road that goes toward it?", TruckTaxiVoiceEmotion.Annoyed),
            E("angry-power-warrior", "WrongWayReaction", "Wrong way! Even my power level cannot make the destination come to us!", TruckTaxiVoiceEmotion.Angry),
            E("veteran-trucker", "StoppedTooLong", "If this is not a light or a queue, let's get rolling before the clock wins.", TruckTaxiVoiceEmotion.Annoyed),
            E("time-scientist", "StoppedTooLong", "Our elapsed time is rising while distance stays constant. That is not the experiment I requested.", TruckTaxiVoiceEmotion.Annoyed),
            E("food-obsessed-dad", "GoodProgress", "Nice steady pace. We will make that diner while the pies are still warm."),
            E("analytical-robot", "GoodProgress", "Route distance is falling at a stable rate. Your driving is within my preferred parameters."),
            E("card-game-hero", "DiversionOffered", "I have a side quest: one quick stop, a little reward, then the original destination. Deal?", TruckTaxiVoiceEmotion.Excited),
            E("bird-loving-android", "DiversionOffered", "There is a quiet birding pull-off nearby. May I trade a few minutes for a better view?", TruckTaxiVoiceEmotion.Excited),
            E("card-game-hero", "DiversionAccepted", "Quest accepted! We make the stop, collect the reward, then return to the main route.", TruckTaxiVoiceEmotion.Excited),
            E("bird-loving-android", "DiversionAccepted", "Thank you. I will keep the stop brief; the original destination remains our next marker.", TruckTaxiVoiceEmotion.Excited),
            E("card-game-hero", "DiversionDeclined", "No side quest, then. The main objective stays active; no penalty from me."),
            E("bird-loving-android", "DiversionDeclined", "Of course. We will keep to the agreed destination; the birds can wait."),
            E("smuggler", "SketchyExit", "This is my stop. I will handle a very ordinary, completely unspecified exchange and be right back."),
            E("ominous-attendant", "SketchyExit", "Wait here. I have a brief appointment with someone who prefers not to be introduced."),
            E("smuggler", "SketchyDance", "The secret handshake was fine. The double spin needs work. Bag exchanged; dignity still missing.", TruckTaxiVoiceEmotion.Excited),
            E("ominous-attendant", "SketchyDance", "A side-eye, a shuffle, and one unnecessary spin. The ritual is complete.", TruckTaxiVoiceEmotion.Excited),
            E("smuggler", "SketchyReturn", "Back aboard. The mysterious bag is not a conversation topic; let's resume the original route."),
            E("ominous-attendant", "SketchyReturn", "All settled. Our brief and unremarkable errand is finished; proceed to the drop-off."),
            E("food-obsessed-dad", "RestaurantReaction", "That place has the kind of pie my family would call a responsible detour."),
            E("glam-brawler-bartender", "RestaurantReaction", "A diner stop sounds good. I know the difference between a proper meal and bar peanuts."),
            E("mechanic-inventor", "RepairReaction", "That rattle has a very specific mechanical rhythm. Let's find a shop before it adds a verse."),
            E("unstable-engineer", "RepairReaction", "The truck is making a noise my calculations did not include. Tow or repair, please."),
            E("grumpy-green-warrior", "RoadRageReaction", "That driver has earned my anger. Keep moving; I would rather arrive than trade dents.", TruckTaxiVoiceEmotion.Angry),
            E("short-anxious-fighter", "RoadRageReaction", "They cut us off! Deep breath. Please keep us safe and let them go.", TruckTaxiVoiceEmotion.Afraid),
            E("smuggler", "PickupRevenge", "You hit me before the ride even started? I have a very memorable complaint ready!", TruckTaxiVoiceEmotion.Angry),
            E("clueless-adult", "PickupRevenge", "Was that the pickup signal? I thought it meant duck! I am definitely telling you off now.", TruckTaxiVoiceEmotion.Angry),
            E("racing-blackstripe-veteran", "RacetrackReaction", "At the speedway, smooth lines beat loud engines. Show me a clean lap.", TruckTaxiVoiceEmotion.Excited),
            E("racing-tennessee-veteran", "RacetrackReaction", "Take me by the track. I want to hear how that big rig sounds beside the grandstands.", TruckTaxiVoiceEmotion.Excited),
            E("racing-southern-commentator", "RacetrackReaction", "Folks, we are approaching the speedway! This could be the highlight of the whole route.", TruckTaxiVoiceEmotion.Excited),
            E("racing-shorttrack-deadpan", "RacetrackReaction", "The track is over there. I have opinions about every corner. Briefly.", TruckTaxiVoiceEmotion.Excited),
            E("racing-excitement-hothead", "RacetrackReaction", "Speedway! One lap, clean driving, and I promise to only yell at the scoreboard.", TruckTaxiVoiceEmotion.Excited),
            E("veteran-trucker", "TransitReaction", "There is the bus terminal. Good place to make a connection without unhooking anything."),
            E("time-scientist", "TransitReaction", "The rail station is our landmark. The timetable appears to be winning this round."),
            E("merchant-bear", "TransitReaction", "Bus stop there, rail station beyond. A traveler should always know the next connection."),
            E("glam-dual-personality", "CompanionNearby", "If you are stopping anyway, a friendly wave would be nicer than another hurried pass."),
            E("glam-dual-personality", "PrivateStopReaction", "That private stop works for me. Park safely and I will tell you when I am ready."),
            E("glam-dark-sorceress", "CompanionNearby", "A little courtesy on the roadside can turn a long day around."),
            E("glam-dark-sorceress", "PrivateStopReaction", "This quiet stop will do. Bring the truck to a safe rest and we can take a moment."),
            E("glam-brawler-bartender", "CompanionNearby", "You look like you could use a friendly passenger. Stop and say hello sometime."),
            E("glam-brawler-bartender", "PrivateStopReaction", "Pull in safely here, and leave the parking brake on. No rush."),
            E("glam-genius-heiress", "CompanionNearby", "A quick hello beats shouting over traffic. Stop nearby if you would like company."),
            E("glam-genius-heiress", "PrivateStopReaction", "This is a suitable private stop. Park securely and we will continue when ready.")
        };

        [MenuItem("Tools/Truck Taxi/Dialogue/Apply Curated Regional Polish")]
        public static void ApplyMenu()
        {
            int added = ApplyCuratedCoverage(TruckTaxiPassengerDialogueAuthoring.AllPassengers());
            UnityEngine.Debug.Log("Truck Taxi curated dialogue polish: " + added + " authored lines added; existing lines and audio preserved.");
        }

        public static int ApplyCuratedCoverage(IEnumerable<PassengerProfile> profiles)
        {
            var byId = profiles.Where(profile => profile != null && Entries.Any(entry => entry.passengerId == profile.passengerId))
                .GroupBy(profile => profile.passengerId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            int added = 0;
            bool changedPersistentAsset = false;
            foreach (var group in Entries.GroupBy(entry => entry.passengerId, StringComparer.Ordinal))
            {
                if (!byId.TryGetValue(group.Key, out PassengerProfile profile) || profile.authoredDialogue == null) continue;
                var lines = (profile.authoredDialogue.lines ?? Array.Empty<TruckTaxiDialogueLine>()).ToList();
                var ids = new HashSet<string>(lines.Where(line => line != null).Select(line => line.lineId), StringComparer.Ordinal);
                Entry[] fresh = group.Where(entry => ids.Add(entry.lineId)).ToArray();
                if (fresh.Length == 0) continue;
                bool persistent = AssetDatabase.Contains(profile.authoredDialogue);
                if (persistent) Undo.RecordObject(profile.authoredDialogue, "Apply curated Truck Taxi dialogue polish");
                foreach (Entry entry in fresh)
                {
                    lines.Add(new TruckTaxiDialogueLine {
                        lineId = entry.lineId, passengerId = profile.passengerId, category = entry.category,
                        text = entry.text, subtitle = entry.text, emotion = entry.emotion, cooldown = 18, weight = 1
                    });
                }
                profile.authoredDialogue.lines = lines.ToArray();
                if (persistent)
                {
                    EditorUtility.SetDirty(profile.authoredDialogue);
                    changedPersistentAsset = true;
                }
                added += fresh.Length;
            }
            if (changedPersistentAsset) AssetDatabase.SaveAssets();
            return added;
        }

        private static Entry E(string id, string category, string text, TruckTaxiVoiceEmotion emotion = TruckTaxiVoiceEmotion.Neutral) =>
            new Entry(id, category, text, emotion);
    }
}
