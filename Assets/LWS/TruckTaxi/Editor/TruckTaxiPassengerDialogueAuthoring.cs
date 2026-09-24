using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckTaxi.Editor
{
    public static class TruckTaxiPassengerDialogueAuthoring
    {
        public const string Root = "Assets/LWS/TruckTaxi/Passengers";
        public static PassengerProfile[] AllPassengers() => AssetDatabase.FindAssets("t:PassengerProfile", new[] { "Assets/LWS/TruckTaxi" })
            .Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<PassengerProfile>)
            .Where(p => p != null).OrderBy(p => p.passengerName, StringComparer.Ordinal).ToArray();

        public static void BuildMissingDialogueAssets(PassengerProfile passenger)
        {
            if (passenger == null) return;
            Undo.RecordObject(passenger, "Create missing passenger dialogue assets");
            if (string.IsNullOrWhiteSpace(passenger.passengerId))
                passenger.passengerId = "passenger-" + AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(passenger)).Substring(0, 12);
            if (!TruckTaxiChatterboxQueue.IsSafeId(passenger.passengerId)) throw new InvalidOperationException("Fix invalid Passenger ID before building.");
            if (AllPassengers().Any(other => other != passenger && string.Equals(other.passengerId, passenger.passengerId, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Duplicate Passenger ID: " + passenger.passengerId);
            EnsureFolder(Root + "/VoiceProfiles"); EnsureFolder(Root + "/Dialogue");
            if (passenger.voiceProfile == null)
            {
                string path = Root + "/VoiceProfiles/" + passenger.passengerId + ".asset";
                var voice = AssetDatabase.LoadAssetAtPath<TruckTaxiVoiceProfile>(path);
                if (voice == null)
                {
                    voice = ScriptableObject.CreateInstance<TruckTaxiVoiceProfile>();
                    voice.voiceProfileId = "voice-" + passenger.passengerId;
                    voice.displayName = passenger.passengerName;
                    voice.description = "Reference audio not assigned. Use explicitly cast source recordings.";
                    AssetDatabase.CreateAsset(voice, path);
                }
                passenger.voiceProfile = voice;
            }
            if (passenger.authoredDialogue == null)
            {
                string path = Root + "/Dialogue/" + passenger.passengerId + ".asset";
                var dialogue = AssetDatabase.LoadAssetAtPath<TruckTaxiDialogueSet>(path);
                if (dialogue == null)
                {
                    dialogue = ScriptableObject.CreateInstance<TruckTaxiDialogueSet>();
                    var source = passenger.dialogueSet?.Where(t => !string.IsNullOrWhiteSpace(t)).ToArray() ?? Array.Empty<string>();
                    var lines = source.Select((text, index) => Line(passenger, "legacy-" + index, index == 0
                        ? TruckTaxiDialogueCategory.PickupGreeting : TruckTaxiDialogueCategory.GeneralChatter, text)).ToList();
                    if (lines.Count == 0) lines.Add(Line(passenger, "greeting", TruckTaxiDialogueCategory.PickupGreeting, "Thanks for the pickup."));
                    lines.Add(Line(passenger, "arrival", TruckTaxiDialogueCategory.Arrival, "Thanks for the ride."));
                    lines.Add(Line(passenger, "collision", passenger.chaosAffinity > 0 ? TruckTaxiDialogueCategory.CollisionPositive
                        : TruckTaxiDialogueCategory.CollisionNegative, passenger.collisionReaction));
                    lines.Add(Line(passenger, "shortcut", TruckTaxiDialogueCategory.ShortcutReaction, passenger.shortcutReaction));
                    lines.Add(Line(passenger, "speed", passenger.chaosAffinity > 0 ? TruckTaxiDialogueCategory.SpeedPositive
                        : TruckTaxiDialogueCategory.SpeedNegative, passenger.speedReaction));
                    dialogue.lines = lines.ToArray();
                    AssetDatabase.CreateAsset(dialogue, path);
                }
                passenger.authoredDialogue = dialogue;
            }
            EditorUtility.SetDirty(passenger); AssetDatabase.SaveAssets();
        }

        private static TruckTaxiDialogueLine Line(PassengerProfile passenger, string id, TruckTaxiDialogueCategory category, string text) =>
            new TruckTaxiDialogueLine { lineId = id, passengerId = passenger.passengerId, category = category, text = text };

        public static void BuildAllMissingDialogueAssets()
        {
            foreach (var passenger in AllPassengers())
                try { BuildMissingDialogueAssets(passenger); }
                catch (Exception ex) { Debug.LogWarning(passenger.name + ": " + ex.Message, passenger); }
            WriteMissingVoiceReport();
        }

        public static string WriteMissingVoiceReport()
        {
            var passengers = AllPassengers();
            var report = new StringBuilder("# Truck Taxi Missing Voice Samples\n\nSource WAVs stay outside Assets and are never overwritten. No voice is inferred from a passenger's identity.\n\n");
            var voices = passengers.Select(p => p.voiceProfile)
                .Concat(passengers.Where(p => p.authoredDialogue != null).SelectMany(p => p.authoredDialogue.lines ?? Array.Empty<TruckTaxiDialogueLine>())
                    .Where(l => l != null).Select(l => l.voiceProfile)).Where(v => v != null).Distinct().OrderBy(v => v.voiceProfileId);
            foreach (var voice in voices)
            {
                report.Append("## ").AppendLine(voice.voiceProfileId);
                report.Append("Used by: ").AppendLine(string.Join(", ", passengers.Where(p => p.voiceProfile == voice ||
                    (p.authoredDialogue != null && p.authoredDialogue.lines != null && p.authoredDialogue.lines.Any(l => l != null && l.voiceProfile == voice))).Select(p => p.passengerId)));
                foreach (TruckTaxiVoiceEmotion emotion in Enum.GetValues(typeof(TruckTaxiVoiceEmotion)))
                {
                    string path = voice.ResolveReference(emotion, out string resolution);
                    report.Append("- ").Append(emotion).Append(": ").AppendLine(File.Exists(path) ? "AVAILABLE (" + resolution + ")" : "MISSING");
                }
                report.AppendLine();
            }
            foreach (var passenger in passengers.Where(p => p.voiceProfile == null)) report.AppendLine("- Missing voice profile: " + passenger.passengerName);
            const string pathReport = "Assets/LWS/TruckTaxi/Documentation/TruckTaxi_MissingVoiceSamples.md";
            File.WriteAllText(pathReport, report.ToString()); AssetDatabase.ImportAsset(pathReport); return pathReport;
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent); AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
