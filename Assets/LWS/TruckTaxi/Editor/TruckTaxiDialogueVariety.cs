using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckTaxi.Editor
{
    public static class TruckTaxiDialogueVariety
    {
        public const string ContentPath = "Tools/TruckTaxiPassengerFactory/Content/IntegratedDialogueVariety.tsv";
        public const string EventContentPath = "Tools/TruckTaxiPassengerFactory/Content/IntegratedDialogueEvents.tsv";
        public const string ReactionContentPath = "Tools/TruckTaxiPassengerFactory/Content/IntegratedDialogueReactions.tsv";
        private static readonly string[] EventCategories = {
            "Boarding", "PickupCancelled", "RepeatPickup", "ScenicStopComplete", "IllicitStopArrival", "IllicitStopComplete"
        };
        private static readonly string[] EventIds = {
            "boarding", "cancel", "repeat", "scenic-complete", "illicit-arrival", "illicit-complete"
        };
        private static readonly string[] ReactionCategories = {
            "RideFailure", "EjectionReaction", "JugSpilled", "RainReaction", "SnowReaction", "BlizzardReaction"
        };
        private static readonly string[] ReactionIds = {
            "ride-failure", "ejection", "jug-spill", "rain", "snow", "blizzard"
        };
        private static readonly string[] Required = {
            "PickupGreeting", "Boarding", "GeneralChatter", "Arrival", "RideFailure", "PickupCancelled",
            "RepeatPickup", "CollisionNegative", "TrafficRamReaction", "PedestrianHitReaction",
            "NearMissReaction", "ShortcutReaction", "OffroadReaction", "PropertyDamageReaction",
            "ScenicView", "ScenicStopComplete", "IllicitStopArrival", "IllicitStopComplete",
            "RainReaction", "StormReaction", "FogReaction", "SunsetReaction",
            "NightReaction", "SnowReaction", "BlizzardReaction",
            "JugStarted", "JugSucceeded", "JugSpilled", "JugThrownFromWindow",
            "EjectionReaction"
        };

        public sealed class Entry
        {
            public string passengerId, lineId, category, text;
        }

        public static Entry[] ReadEntries(string path)
        {
            var entries = new List<Entry>();
            int number = 0;
            foreach (string raw in File.ReadAllLines(path))
            {
                number++;
                if (string.IsNullOrWhiteSpace(raw) || raw.StartsWith("#", StringComparison.Ordinal)) continue;
                string[] fields = raw.Split(new[] { '\t' }, 4);
                if (fields.Length != 4 || fields.Any(string.IsNullOrWhiteSpace))
                    throw new InvalidDataException("Invalid dialogue row at line " + number);
                entries.Add(new Entry { passengerId = fields[0], lineId = fields[1], category = fields[2], text = fields[3] });
            }
            return entries.ToArray();
        }

        public static Entry[] ReadEventEntries(string path) => ReadWideEntries(path, EventCategories, EventIds);
        public static Entry[] ReadReactionEntries(string path) => ReadWideEntries(path, ReactionCategories, ReactionIds);

        private static Entry[] ReadWideEntries(string path, string[] categories, string[] ids)
        {
            var entries = new List<Entry>();
            int number = 0;
            foreach (string raw in File.ReadAllLines(path))
            {
                number++;
                if (string.IsNullOrWhiteSpace(raw) || raw.StartsWith("#", StringComparison.Ordinal)) continue;
                string[] fields = raw.Split('\t');
                if (fields.Length != categories.Length + 1 || fields.Any(string.IsNullOrWhiteSpace))
                    throw new InvalidDataException("Invalid event dialogue row at line " + number);
                for (int i = 0; i < categories.Length; i++)
                    entries.Add(new Entry {
                        passengerId = fields[0], lineId = "variety-event-" + ids[i] + "-01",
                        category = categories[i], text = fields[i + 1]
                    });
            }
            return entries.ToArray();
        }

        [MenuItem("Tools/Truck Taxi/Dialogue/Apply Integrated Variety")]
        public static void ApplyMenu()
        {
            int added = ApplyIntegrated();
            Debug.Log("Truck Taxi dialogue variety: " + added + " new lines. Existing lines and audio preserved.");
        }

        public static int ApplyIntegrated() => ApplyEntries(ReadEntries(ContentPath)
            .Concat(ReadEventEntries(EventContentPath)).Concat(ReadReactionEntries(ReactionContentPath)).ToArray());
        public static int Apply(string path) => ApplyEntries(ReadEntries(path));

        private static int ApplyEntries(Entry[] entries)
        {
            var passengers = TruckTaxiPassengerDialogueAuthoring.AllPassengers()
                .ToDictionary(p => p.passengerId, StringComparer.Ordinal);
            if (entries.GroupBy(e => e.passengerId + "/" + e.lineId, StringComparer.Ordinal).Any(g => g.Count() > 1))
                throw new InvalidDataException("Duplicate authored line IDs.");
            foreach (Entry entry in entries)
            {
                if (!passengers.TryGetValue(entry.passengerId, out var passenger) || passenger.authoredDialogue == null)
                    throw new InvalidDataException("Missing passenger or dialogue set: " + entry.passengerId);
                if (!TruckTaxiChatterboxQueue.IsSafeId(entry.lineId) ||
                    !Enum.TryParse(entry.category, false, out TruckTaxiDialogueCategory _))
                    throw new InvalidDataException("Invalid line ID or unavailable category: " + entry.passengerId + "/" + entry.lineId + "/" + entry.category);
                var existing = (passenger.authoredDialogue.lines ?? Array.Empty<TruckTaxiDialogueLine>())
                    .FirstOrDefault(line => line != null && line.lineId == entry.lineId);
                if (existing != null && (existing.text != entry.text || existing.category.ToString() != entry.category))
                    throw new InvalidDataException("Authored ID has different content; preserve manually: " + entry.passengerId + "/" + entry.lineId);
            }
            int added = 0;
            foreach (var group in entries.GroupBy(e => e.passengerId, StringComparer.Ordinal))
            {
                var dialogue = passengers[group.Key].authoredDialogue;
                var lines = (dialogue.lines ?? Array.Empty<TruckTaxiDialogueLine>()).ToList();
                var fresh = group.Where(e => lines.All(line => line == null || line.lineId != e.lineId)).ToArray();
                if (fresh.Length == 0) continue;
                Undo.RecordObject(dialogue, "Apply Truck Taxi authored dialogue variety");
                foreach (Entry entry in fresh)
                    lines.Add(new TruckTaxiDialogueLine {
                        passengerId = entry.passengerId, lineId = entry.lineId,
                        category = (TruckTaxiDialogueCategory)Enum.Parse(typeof(TruckTaxiDialogueCategory), entry.category),
                        text = entry.text, subtitle = entry.text
                    });
                dialogue.lines = lines.ToArray();
                EditorUtility.SetDirty(dialogue);
                added += fresh.Length;
            }
            if (added > 0) AssetDatabase.SaveAssets();
            return added;
        }

        [MenuItem("Tools/Truck Taxi/Dialogue/Validate Roster Variety")]
        public static void ValidateMenu()
        {
            string[] issues = ValidateRoster(TruckTaxiPassengerDialogueAuthoring.AllPassengers());
            Debug.Log("Truck Taxi dialogue variety: " + issues.Length + " issues.\n" + string.Join("\n", issues));
        }

        public static string[] ValidateRoster(IEnumerable<PassengerProfile> passengers)
        {
            var issues = new List<string>();
            var repeated = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            var scenic = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            foreach (string category in Required)
                if (!Enum.TryParse(category, false, out TruckTaxiDialogueCategory _))
                    issues.Add("runtime category unavailable: " + category);
            foreach (var passenger in passengers.Where(p => p != null))
            {
                var lines = (passenger.authoredDialogue?.lines ?? Array.Empty<TruckTaxiDialogueLine>())
                    .Where(line => line != null && !string.IsNullOrWhiteSpace(line.text)).ToArray();
                int thin = 0;
                foreach (string category in Required)
                {
                    if (!Enum.TryParse(category, false, out TruckTaxiDialogueCategory parsed)) continue;
                    int count = lines.Count(line => line.category == parsed);
                    if (count == 0) issues.Add(passenger.passengerId + ": missing " + category);
                    if (count < 2) thin++;
                }
                if (thin > Required.Length / 2)
                    issues.Add(passenger.passengerId + ": fallback-heavy; " + thin + " common categories have fewer than two lines");
                foreach (var line in lines)
                {
                    string normalized = Normalize(line.text);
                    if (normalized.Length >= 45) Add(repeated, normalized, passenger.passengerId);
                    if (line.category.ToString() == "ScenicView") Add(scenic, normalized, passenger.passengerId);
                }
            }
            foreach (var item in repeated.Where(pair => pair.Value.Count > 1))
                issues.Add("duplicate long line across " + string.Join(", ", item.Value.OrderBy(id => id, StringComparer.Ordinal)) + ": " + item.Key);
            foreach (var item in scenic.Where(pair => pair.Value.Count > 1))
                issues.Add("identical scenic summary across " + string.Join(", ", item.Value.OrderBy(id => id, StringComparer.Ordinal)) + ": " + item.Key);
            return issues.ToArray();
        }

        private static void Add(Dictionary<string, HashSet<string>> index, string text, string passengerId)
        {
            if (!index.TryGetValue(text, out var ids)) index[text] = ids = new HashSet<string>(StringComparer.Ordinal);
            ids.Add(passengerId);
        }

        private static string Normalize(string text) => Regex.Replace(text.ToLowerInvariant(), "[^a-z0-9]+", " ").Trim();
    }
}
