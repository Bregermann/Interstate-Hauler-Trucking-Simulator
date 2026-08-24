using System.Collections.Generic;
using UnityEngine;

namespace DeadAir
{
    public static class DeadAirBeatLayoutUtility
    {
        public const string UnplacedRootName = "DEAD_AIR_UNPLACED_GAMEPLAY";

        public static IReadOnlyList<DeadAirBeatDefinition> CreateDefaultBeatDefinitions()
        {
            return new[]
            {
                Beat("DA_START_000", "Start - Dead Channel", DeadAirTriggerCategory.Dispatch, 0f, "The route begins with a silent radio check."),
                Beat("DA_001", "Dispatch Finds You", DeadAirTriggerCategory.Dispatch, 1.2f, "First dispatch contact."),
                Beat("DA_002", "GPS Names The Wrong Exit", DeadAirTriggerCategory.GPS, 3.1f, "GPS contradicts the route."),
                Beat("DA_003", "CB Bleed", DeadAirTriggerCategory.CBRadio, 5.4f, "Unknown driver breaks through."),
                Beat("DA_004", "Fog Bank", DeadAirTriggerCategory.Environment, 7.6f, "Visibility closes down."),
                Beat("DA_005", "Dash Flicker", DeadAirTriggerCategory.Dashboard, 9.8f, "Instruments lie briefly."),
                Beat("DA_006", "Headlights Behind", DeadAirTriggerCategory.Traffic, 12.2f, "Distant lights appear in mirrors."),
                Beat("DA_007", "Dispatcher Denies GPS", DeadAirTriggerCategory.Dispatch, 14.7f, "Human voice contradicts machine voice."),
                Beat("DA_008", "GPS Recalculating Loop", DeadAirTriggerCategory.GPS, 17.3f, "Recalculating becomes a threat."),
                Beat("DA_009", "Roadside Sign Wrong Name", DeadAirTriggerCategory.Environment, 20.1f, "The sign knows the player."),
                Beat("DA_010", "CB Asks For Horn", DeadAirTriggerCategory.CBRadio, 22.6f, "A voice asks for proof."),
                Beat("DA_011", "No Service", DeadAirTriggerCategory.GPS, 25.4f, "Navigation signal drops."),
                Beat("DA_012", "Truck Feels Heavy", DeadAirTriggerCategory.Environment, 28.0f, "Anomaly hook for handling/audio."),
                Beat("DA_013", "Dispatcher Is Afraid", DeadAirTriggerCategory.Dispatch, 31.2f, "Tone turns human."),
                Beat("DA_014", "Exit 17 Foreshadow", DeadAirTriggerCategory.Story, 35.5f, "First hard mention of Exit 17."),
                Beat("DA_015", "Split Horizon", DeadAirTriggerCategory.Environment, 39.0f, "Route choice pressure builds."),
                Beat("DA_016", "Radio Cutout", DeadAirTriggerCategory.CBRadio, 43.3f, "Dead air becomes explicit."),
                Beat("DA_017", "Last Correct Instruction", DeadAirTriggerCategory.GPS, 47.1f, "One instruction may be true."),
                Beat("DA_018", "The Choice Narrows", DeadAirTriggerCategory.Story, 49.2f, "Final setup before ending trigger."),
                Beat("CHOICE_01", "Choice 01 - Dispatch Or GPS", DeadAirTriggerCategory.ChoiceStart, 18.5f, "Choice group: trust dispatch or GPS."),
                Beat("CHOICE_02", "Choice 02 - Keep Going Or Turn Back", DeadAirTriggerCategory.ChoiceStart, 33.0f, "Choice group: continue or retreat."),
                Beat("CHOICE_03_EXIT17", "Choice 03 - Exit 17", DeadAirTriggerCategory.ChoiceStart, 48.5f, "Choice group: commit to Exit 17."),
                Beat("ENDING_TRIGGER", "Ending Trigger", DeadAirTriggerCategory.Ending, 50.0f, "Resolve ending from committed choices.")
            };
        }

        public static Transform EnsureUnplacedBeatLayout()
        {
            GameObject rootObject = GameObject.Find(UnplacedRootName);
            Transform root = rootObject != null ? rootObject.transform : new GameObject(UnplacedRootName).transform;
            IReadOnlyList<DeadAirBeatDefinition> beats = CreateDefaultBeatDefinitions();
            for (int i = 0; i < beats.Count; i++)
            {
                DeadAirBeatDefinition beat = beats[i];
                if (root.Find(beat.beatId) != null)
                {
                    continue;
                }

                GameObject go = new GameObject(beat.beatId);
                go.transform.SetParent(root, false);
                go.transform.localPosition = new Vector3((i % 6) * 24f, 0f, (i / 6) * 24f);
                DeadAirTriggerZone trigger = go.AddComponent<DeadAirTriggerZone>();
                trigger.Configure(beat, i);
                BoxCollider collider = go.GetComponent<BoxCollider>();
                collider.size = new Vector3(14f, 8f, 14f);
                GameObject note = new GameObject("PLACEMENT STATUS: UNPLACED");
                note.transform.SetParent(go.transform, false);
            }

            EnsureMarker(root, "Traffic Spawn Marker");
            EnsureMarker(root, "Sign Marker");
            EnsureMarker(root, "Headlights Marker");
            return root;
        }

        private static DeadAirBeatDefinition Beat(string id, string name, DeadAirTriggerCategory category, float miles, string description)
        {
            return new DeadAirBeatDefinition
            {
                beatId = id,
                displayName = name,
                category = category,
                routeDistanceMiles = miles,
                description = description,
                notes = "PLACEMENT STATUS: UNPLACED. Pacing metadata only; do not trigger by timer.",
                estimatedPlaybackSeconds = 8f,
                debugColor = CategoryColor(category),
                choiceOutcome = DeadAirChoiceOutcome.None,
                critical = true
            };
        }

        private static void EnsureMarker(Transform root, string name)
        {
            if (root.Find(name) != null)
            {
                return;
            }

            GameObject marker = new GameObject(name);
            marker.transform.SetParent(root, false);
        }

        private static Color CategoryColor(DeadAirTriggerCategory category)
        {
            switch (category)
            {
                case DeadAirTriggerCategory.Dispatch: return new Color(0.1f, 0.6f, 1f, 0.35f);
                case DeadAirTriggerCategory.GPS: return new Color(0.1f, 1f, 0.45f, 0.35f);
                case DeadAirTriggerCategory.CBRadio: return new Color(1f, 0.75f, 0.15f, 0.35f);
                case DeadAirTriggerCategory.Environment: return new Color(0.55f, 0.9f, 0.95f, 0.35f);
                case DeadAirTriggerCategory.Traffic: return new Color(1f, 0.25f, 0.2f, 0.35f);
                case DeadAirTriggerCategory.ChoiceStart:
                case DeadAirTriggerCategory.ChoiceCommit: return new Color(1f, 0.1f, 0.9f, 0.35f);
                case DeadAirTriggerCategory.Ending: return new Color(0.95f, 0.95f, 0.95f, 0.35f);
                default: return new Color(0.2f, 0.85f, 1f, 0.35f);
            }
        }
    }
}
