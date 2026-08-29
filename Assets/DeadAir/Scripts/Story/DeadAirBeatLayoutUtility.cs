using System.Collections.Generic;
using UnityEngine;

namespace DeadAir
{
    public static class DeadAirBeatLayoutUtility
    {
        public const string UnplacedRootName = "DEAD_AIR_UNPLACED_GAMEPLAY";
        public const string ConstructionKitRootName = "DEAD_AIR_CONSTRUCTION_KIT";
        public const string FinalWorldRootName = "FINAL_WORLD";
        public const string PlayableBlockoutRootName = "DEAD_AIR_PLAYABLE_BLOCKOUT";

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
                Beat("DA_018", "The Choice Narrows", DeadAirTriggerCategory.Story, 49.2f, "Final setup before ending trigger.")
            };
        }

        public static Transform EnsureUnplacedBeatLayout()
        {
            Transform root = EnsureRoot(UnplacedRootName);
            AddComponentIfMissing<DeadAirRouteFlowGizmo>(root.gameObject);
            IReadOnlyList<DeadAirBeatDefinition> beats = CreateDefaultBeatDefinitions();
            for (int i = 0; i < beats.Count; i++)
            {
                DeadAirBeatDefinition beat = beats[i];
                if (root.Find(beat.beatId) != null)
                {
                    continue;
                }

                Vector3 localPosition = new Vector3((i % 6) * 24f, 0f, (i / 6) * 24f);
                DeadAirTriggerZone trigger = EnsureTrigger<DeadAirTriggerZone>(root, beat.beatId, beat.category, beat.routeDistanceMiles, localPosition);
                trigger.Configure(beat, i);
            }

            EnsureChoiceGroup(root, "CHOICE_01", "Choice 01 - Dispatch Or GPS", 18.5f, DeadAirChoiceOutcome.TrustDispatch, DeadAirChoiceOutcome.TrustGPS, DeadAirChoiceOutcome.TrustNoOne);
            EnsureChoiceGroup(root, "CHOICE_02", "Choice 02 - Keep Going Or Turn Back", 33.0f, DeadAirChoiceOutcome.TrustGPS, DeadAirChoiceOutcome.TurnBack, DeadAirChoiceOutcome.TrustNoOne);
            EnsureChoiceGroup(root, "CHOICE_03_EXIT17", "Choice 03 - Exit 17", 48.5f, DeadAirChoiceOutcome.Exit17, DeadAirChoiceOutcome.Lost, DeadAirChoiceOutcome.TrustNoOne);
            EnsureMarker(root, "Traffic Spawn Marker");
            EnsureMarker(root, "Sign Marker");
            EnsureMarker(root, "Headlights Marker");
            EnsureSceneStartMarker();
            return root;
        }

        public static DeadAirStartMarker FindPreferredRuntimeStartMarker()
        {
            DeadAirStartMarker fallback = null;
            DeadAirStartMarker[] markers = Object.FindObjectsByType<DeadAirStartMarker>(FindObjectsSortMode.None);
            foreach (DeadAirStartMarker marker in markers)
            {
                if (marker == null ||
                    IsUnderNamedRoot(marker.transform, ConstructionKitRootName) ||
                    IsUnderNamedRoot(marker.transform, UnplacedRootName))
                {
                    continue;
                }

                if (IsUnderNamedRoot(marker.transform, PlayableBlockoutRootName))
                {
                    return marker;
                }

                if (fallback == null)
                {
                    fallback = marker;
                }
            }

            return fallback;
        }

        public static bool IsUnderNamedRoot(Transform transform, string rootName)
        {
            Transform current = transform;
            while (current != null)
            {
                if (current.name == rootName)
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        public static Transform EnsureConstructionKit()
        {
            Transform root = EnsureRoot(ConstructionKitRootName);
            Transform roads = EnsureChild(root, "ROAD_PIECES");
            EnsureRoadPiece(roads, "DA_ROAD_STRAIGHT_SHORT", DeadAirRoadPieceKind.Straight, new Vector3(0f, 0f, 0f));
            EnsureRoadPiece(roads, "DA_ROAD_STRAIGHT_LONG", DeadAirRoadPieceKind.Straight, new Vector3(18f, 0f, 0f));
            EnsureRoadPiece(roads, "DA_ROAD_CURVE_LEFT", DeadAirRoadPieceKind.Curve, new Vector3(36f, 0f, 0f));
            EnsureRoadPiece(roads, "DA_ROAD_CURVE_RIGHT", DeadAirRoadPieceKind.Curve, new Vector3(54f, 0f, 0f));
            EnsureRoadPiece(roads, "DA_ROAD_FORK", DeadAirRoadPieceKind.Fork, new Vector3(72f, 0f, 0f));
            EnsureRoadPiece(roads, "DA_ROAD_EXIT_RIGHT", DeadAirRoadPieceKind.ExitRamp, new Vector3(90f, 0f, 0f));
            EnsureRoadPiece(roads, "DA_ROAD_MERGE", DeadAirRoadPieceKind.Merge, new Vector3(108f, 0f, 0f));
            EnsureValidRoadZone(roads, "DA_VALID_ROAD_ZONE", new Vector3(0f, 0f, -28f), new Vector3(24f, 10f, 90f));
            EnsureValidRoadZone(roads, "DA_VALID_ROAD_DEPOT", new Vector3(30f, 0f, -28f), new Vector3(58f, 10f, 58f));
            EnsureValidRoadZone(roads, "DA_VALID_ROAD_HIGHWAY", new Vector3(66f, 0f, -28f), new Vector3(30f, 10f, 160f));
            EnsureValidRoadZone(roads, "DA_VALID_ROAD_FORK", new Vector3(104f, 0f, -28f), new Vector3(64f, 10f, 92f));

            Transform choices = EnsureChild(root, "CHOICE_PIECES");
            EnsureChoiceTemplate(choices, "DA_CHOICE_TEMPLATE", "CHOICE_TEMPLATE", false, new Vector3(0f, 0f, 24f));
            EnsureChoiceTemplate(choices, "DA_EXIT_CHOICE_TEMPLATE", "EXIT_CHOICE_TEMPLATE", true, new Vector3(36f, 0f, 24f));

            Transform story = EnsureChild(root, "STORY_TRIGGERS");
            EnsureTemplateTrigger<DeadAirTriggerZone>(story, "DA_TRIGGER_DISPATCH", DeadAirTriggerCategory.Dispatch);
            EnsureTemplateTrigger<DeadAirTriggerZone>(story, "DA_TRIGGER_CB", DeadAirTriggerCategory.CBRadio);
            EnsureTemplateTrigger<DeadAirGpsEventTrigger>(story, "DA_TRIGGER_GPS", DeadAirTriggerCategory.GPS);
            EnsureTemplateTrigger<DeadAirEnvironmentEventTrigger>(story, "DA_TRIGGER_ENVIRONMENT", DeadAirTriggerCategory.Environment);
            EnsureTemplateTrigger<DeadAirTrafficHorrorEventTrigger>(story, "DA_TRIGGER_TRAFFIC", DeadAirTriggerCategory.Traffic);
            EnsureTemplateTrigger<DeadAirDashboardEventTrigger>(story, "DA_TRIGGER_DASHBOARD", DeadAirTriggerCategory.Dashboard);
            EnsureTemplateTrigger<DeadAirTriggerZone>(story, "DA_TRIGGER_GENERIC_STORY", DeadAirTriggerCategory.Story);

            Transform signs = EnsureChild(root, "SIGNS");
            EnsureSign(signs, "DA_SIGN_DESTINATION", DeadAirSignKind.DestinationSign, "DESTINATION");
            EnsureSign(signs, "DA_SIGN_MILEAGE", DeadAirSignKind.RoadSign, "MILES");
            EnsureSign(signs, "DA_SIGN_EXIT", DeadAirSignKind.ExitSign, "EXIT");
            EnsureSign(signs, "DA_SIGN_WARNING", DeadAirSignKind.WarningSign, "WARNING");
            EnsureSign(signs, "DA_SIGN_ROUTE", DeadAirSignKind.RoadSign, "ROUTE");

            Transform traffic = EnsureChild(root, "TRAFFIC_EVENTS");
            EnsureTemplateTrigger<DeadAirTrafficHorrorEventTrigger>(traffic, "DA_TRAFFIC_SPAWN", DeadAirTriggerCategory.Traffic);
            EnsureTemplateTrigger<DeadAirTrafficHorrorEventTrigger>(traffic, "DA_TRAFFIC_DESPAWN", DeadAirTriggerCategory.Traffic);
            EnsureTemplateTrigger<DeadAirTrafficHorrorEventTrigger>(traffic, "DA_TRAFFIC_REPEATING_CAR", DeadAirTriggerCategory.Traffic);
            EnsureTemplateTrigger<DeadAirTrafficHorrorEventTrigger>(traffic, "DA_TRAFFIC_FOLLOWER", DeadAirTriggerCategory.Traffic);
            EnsureTemplateTrigger<DeadAirTrafficHorrorEventTrigger>(traffic, "DA_TRAFFIC_HEADLIGHTS", DeadAirTriggerCategory.Traffic);

            Transform environment = EnsureChild(root, "ENVIRONMENT_EVENTS");
            EnsureTemplateTrigger<DeadAirEnvironmentEventTrigger>(environment, "FOG_START", DeadAirTriggerCategory.Environment);
            EnsureTemplateTrigger<DeadAirEnvironmentEventTrigger>(environment, "FOG_STOP", DeadAirTriggerCategory.Environment);
            EnsureTemplateTrigger<DeadAirEnvironmentEventTrigger>(environment, "RAIN_START", DeadAirTriggerCategory.Environment);
            EnsureTemplateTrigger<DeadAirEnvironmentEventTrigger>(environment, "RAIN_STOP", DeadAirTriggerCategory.Environment);
            EnsureTemplateTrigger<DeadAirEnvironmentEventTrigger>(environment, "LIGHTNING_EVENT", DeadAirTriggerCategory.Environment);
            EnsureTemplateTrigger<DeadAirEnvironmentEventTrigger>(environment, "LIGHTING_CHANGE", DeadAirTriggerCategory.Environment);
            EnsureTemplateTrigger<DeadAirEnvironmentEventTrigger>(environment, "AMBIENT_SILENCE", DeadAirTriggerCategory.Environment);
            EnsureTemplateTrigger<DeadAirEnvironmentEventTrigger>(environment, "AMBIENT_RESTORE", DeadAirTriggerCategory.Environment);

            Transform dashboard = EnsureChild(root, "DASHBOARD_EVENTS");
            EnsureTemplateTrigger<DeadAirDashboardEventTrigger>(dashboard, "FUEL_DISPLAY_LIE", DeadAirTriggerCategory.Dashboard);
            EnsureTemplateTrigger<DeadAirDashboardEventTrigger>(dashboard, "CLOCK_LIE", DeadAirTriggerCategory.Dashboard);
            EnsureTemplateTrigger<DeadAirDashboardEventTrigger>(dashboard, "SPEED_DISPLAY_GLITCH", DeadAirTriggerCategory.Dashboard);
            EnsureTemplateTrigger<DeadAirDashboardEventTrigger>(dashboard, "WARNING_LIGHT_EVENT", DeadAirTriggerCategory.Dashboard);
            EnsureTemplateTrigger<DeadAirDashboardEventTrigger>(dashboard, "DASHBOARD_RESTORE", DeadAirTriggerCategory.Dashboard);

            Transform gps = EnsureChild(root, "GPS_EVENTS");
            EnsureTemplateTrigger<DeadAirGpsEventTrigger>(gps, "GPS_NORMAL", DeadAirTriggerCategory.GPS);
            EnsureTemplateTrigger<DeadAirGpsEventTrigger>(gps, "GPS_TURN", DeadAirTriggerCategory.GPS);
            EnsureTemplateTrigger<DeadAirGpsEventTrigger>(gps, "GPS_EXIT", DeadAirTriggerCategory.GPS);
            EnsureTemplateTrigger<DeadAirGpsEventTrigger>(gps, "GPS_RECALCULATING", DeadAirTriggerCategory.GPS);
            EnsureTemplateTrigger<DeadAirGpsEventTrigger>(gps, "GPS_NO_SIGNAL", DeadAirTriggerCategory.GPS);
            EnsureTemplateTrigger<DeadAirGpsEventTrigger>(gps, "GPS_CORRUPTION", DeadAirTriggerCategory.GPS);
            EnsureTemplateTrigger<DeadAirGpsEventTrigger>(gps, "GPS_RESTORE", DeadAirTriggerCategory.GPS);

            Transform audio = EnsureChild(root, "AUDIO_EVENTS");
            EnsureTemplateTrigger<DeadAirAudioEventTrigger>(audio, "AUDIO_DISPATCH", DeadAirTriggerCategory.Dispatch);
            EnsureTemplateTrigger<DeadAirAudioEventTrigger>(audio, "AUDIO_CB", DeadAirTriggerCategory.CBRadio);
            EnsureTemplateTrigger<DeadAirAudioEventTrigger>(audio, "AUDIO_GPS", DeadAirTriggerCategory.GPS);
            EnsureTemplateTrigger<DeadAirAudioEventTrigger>(audio, "AUDIO_MULTI_CHANNEL_SEQUENCE", DeadAirTriggerCategory.CBRadio);

            EnsureChild(root, "ENDING_PIECES");
            Transform debug = EnsureChild(root, "DEBUG_REFERENCE");
            EnsureDepotStartTemplate(debug);
            return root;
        }

        private static void EnsureSceneStartMarker()
        {
            Transform startRoot = EnsureRoot("START");
            Transform marker = startRoot.Find("DeadAirStartMarker");
            if (marker == null)
            {
                marker = new GameObject("DeadAirStartMarker").transform;
                marker.SetParent(startRoot, false);
            }

            AddComponentIfMissing<DeadAirStartMarker>(marker.gameObject);
        }

        private static void EnsureChoiceTemplate(Transform parent, string templateName, string choiceId, bool exitTemplate, Vector3 localPosition)
        {
            Transform template = EnsureChild(parent, templateName);
            template.localPosition = localPosition;
            DeadAirChoiceStartTrigger start = EnsureTrigger<DeadAirChoiceStartTrigger>(template, "CHOICE_START", DeadAirTriggerCategory.ChoiceStart, 0f, Vector3.zero);
            start.ConfigureChoice(choiceId);

            if (exitTemplate)
            {
                DeadAirChoiceCommitTrigger exit = EnsureTrigger<DeadAirChoiceCommitTrigger>(template, "EXIT_COMMIT", DeadAirTriggerCategory.ChoiceCommit, 0.1f, new Vector3(-10f, 0f, 18f));
                exit.ConfigureChoice(choiceId, DeadAirChoiceOutcome.Exit17);
                DeadAirChoiceCommitTrigger straight = EnsureTrigger<DeadAirChoiceCommitTrigger>(template, "STRAIGHT_COMMIT", DeadAirTriggerCategory.ChoiceCommit, 0.1f, new Vector3(10f, 0f, 18f));
                straight.ConfigureChoice(choiceId, DeadAirChoiceOutcome.Lost);
                return;
            }

            DeadAirChoiceCommitTrigger left = EnsureTrigger<DeadAirChoiceCommitTrigger>(template, "LEFT_COMMIT", DeadAirTriggerCategory.ChoiceCommit, 0.1f, new Vector3(-10f, 0f, 18f));
            left.ConfigureChoice(choiceId, DeadAirChoiceOutcome.TrustDispatch);
            DeadAirChoiceCommitTrigger right = EnsureTrigger<DeadAirChoiceCommitTrigger>(template, "RIGHT_COMMIT", DeadAirTriggerCategory.ChoiceCommit, 0.1f, new Vector3(10f, 0f, 18f));
            right.ConfigureChoice(choiceId, DeadAirChoiceOutcome.TrustGPS);
        }

        private static void EnsureDepotStartTemplate(Transform parent)
        {
            Transform template = EnsureChild(parent, "DA_DEPOT_START_TEMPLATE");
            template.localPosition = new Vector3(0f, 0f, 18f);
            AddPlacementNote(template);

            DeadAirStartMarker marker = AddComponentIfMissing<DeadAirStartMarker>(template.gameObject);
            Transform truck = EnsureChild(template, "TRUCK_START_REFERENCE");
            truck.localPosition = Vector3.zero;
            truck.localRotation = Quaternion.identity;

            Transform trailer = EnsureChild(template, "TRAILER_START_REFERENCE");
            trailer.localPosition = new Vector3(0f, 0f, -13.5f);
            trailer.localRotation = Quaternion.identity;

            Transform forward = EnsureChild(template, "FORWARD_DIRECTION");
            forward.localPosition = new Vector3(0f, 0f, 8f);
            marker.ConfigureRigReferences(truck, trailer);

            DeadAirTriggerZone start = EnsureTrigger<DeadAirTriggerZone>(
                template,
                "DA_START_000_INITIAL_TRIGGER",
                DeadAirTriggerCategory.Dispatch,
                0f,
                new Vector3(0f, 0f, 16f));
            start.Configure(
                Beat("DA_START_000", "Start - Dead Channel", DeadAirTriggerCategory.Dispatch, 0f, "Initial run setup trigger; first spoken line can be placed later."),
                0);
        }

        private static void EnsureChoiceGroup(
            Transform root,
            string choiceId,
            string displayName,
            float miles,
            DeadAirChoiceOutcome left,
            DeadAirChoiceOutcome right,
            DeadAirChoiceOutcome straight)
        {
            Transform group = EnsureChild(root, choiceId);
            AddPlacementNote(group);
            DeadAirChoiceStartTrigger start = EnsureTrigger<DeadAirChoiceStartTrigger>(group, $"{choiceId}_START", DeadAirTriggerCategory.ChoiceStart, miles, Vector3.zero);
            start.ConfigureChoice(choiceId);
            start.Configure(Beat($"{choiceId}_START", displayName, DeadAirTriggerCategory.ChoiceStart, miles, "Choice fork start."), 100);
            ConfigureCommit(group, $"{choiceId}_LEFT_COMMIT", choiceId, left, miles + 0.1f, new Vector3(-12f, 0f, 20f));
            ConfigureCommit(group, $"{choiceId}_RIGHT_COMMIT", choiceId, right, miles + 0.1f, new Vector3(12f, 0f, 20f));
            ConfigureCommit(group, $"{choiceId}_STRAIGHT_COMMIT", choiceId, straight, miles + 0.1f, new Vector3(0f, 0f, 32f));
        }

        private static void ConfigureCommit(Transform parent, string name, string choiceId, DeadAirChoiceOutcome outcome, float miles, Vector3 localPosition)
        {
            DeadAirChoiceCommitTrigger commit = EnsureTrigger<DeadAirChoiceCommitTrigger>(parent, name, DeadAirTriggerCategory.ChoiceCommit, miles, localPosition);
            commit.ConfigureChoice(choiceId, outcome);
            commit.Configure(Beat(name, $"{choiceId} - {outcome}", DeadAirTriggerCategory.ChoiceCommit, miles, $"Commit lane for {outcome}."), 101);
        }

        private static GameObject EnsureTemplateTrigger<T>(Transform parent, string name, DeadAirTriggerCategory category) where T : DeadAirTriggerZone
        {
            T trigger = EnsureTrigger<T>(parent, name, category, 0f, Vector3.zero);
            trigger.Configure(Beat(name, name, category, 0f, "Construction-kit template. Duplicate before editing."), 0);
            return trigger.gameObject;
        }

        private static T EnsureTrigger<T>(Transform parent, string name, DeadAirTriggerCategory category, float miles, Vector3 localPosition) where T : DeadAirTriggerZone
        {
            GameObject existing = FindChildGameObject(parent, name);
            T trigger = existing != null ? existing.GetComponent<T>() : null;
            if (trigger == null)
            {
                GameObject go = existing != null ? existing : new GameObject(name);
                go.transform.SetParent(parent, false);
                go.transform.localPosition = localPosition;
                trigger = go.AddComponent<T>();
            }

            trigger.Configure(Beat(name, name, category, miles, "Dead Air trigger placeholder."), 0);
            BoxCollider collider = trigger.GetComponent<BoxCollider>();
            if (collider != null)
            {
                collider.size = new Vector3(14f, 8f, 14f);
            }

            AddPlacementNote(trigger.transform);
            return trigger;
        }

        private static void EnsureRoadPiece(Transform parent, string name, DeadAirRoadPieceKind kind, Vector3 localPosition)
        {
            GameObject go = FindChildGameObject(parent, name) ?? new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            DeadAirRoadPiece piece = go.GetComponent<DeadAirRoadPiece>() ?? go.AddComponent<DeadAirRoadPiece>();
            piece.Configure(kind, "Construction-kit template. Duplicate before editing.");
        }

        private static void EnsureValidRoadZone(Transform parent, string name, Vector3 localPosition, Vector3 size)
        {
            GameObject go = FindChildGameObject(parent, name) ?? new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            DeadAirValidRoadZone zone = go.GetComponent<DeadAirValidRoadZone>() ?? go.AddComponent<DeadAirValidRoadZone>();
            zone.Configure(name, size);
        }

        private static void EnsureSign(Transform parent, string name, DeadAirSignKind kind, string text)
        {
            GameObject go = FindChildGameObject(parent, name) ?? new GameObject(name);
            go.transform.SetParent(parent, false);
            DeadAirSign sign = go.GetComponent<DeadAirSign>() ?? go.AddComponent<DeadAirSign>();
            sign.Configure(kind, text);
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
                placementStatus = DeadAirPlacementStatus.Unplaced,
                choiceOutcome = DeadAirChoiceOutcome.None,
                critical = true
            };
        }

        private static Transform EnsureRoot(string name)
        {
            GameObject existing = GameObject.Find(name);
            return existing != null ? existing.transform : new GameObject(name).transform;
        }

        private static Transform EnsureChild(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                return existing;
            }

            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child.transform;
        }

        private static void EnsureMarker(Transform root, string name)
        {
            if (root.Find(name) != null)
            {
                return;
            }

            GameObject marker = new GameObject(name);
            marker.transform.SetParent(root, false);
            marker.AddComponent<DeadAirAuthoringMarker>();
        }

        private static void AddPlacementNote(Transform parent)
        {
            if (parent.Find("PLACEMENT STATUS: UNPLACED") != null)
            {
                return;
            }

            GameObject note = new GameObject("PLACEMENT STATUS: UNPLACED");
            note.transform.SetParent(parent, false);
        }

        private static GameObject FindChildGameObject(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            return child != null ? child.gameObject : null;
        }

        private static T AddComponentIfMissing<T>(GameObject go) where T : Component
        {
            return go.GetComponent<T>() ?? go.AddComponent<T>();
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
