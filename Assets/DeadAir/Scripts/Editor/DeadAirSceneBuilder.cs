using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeadAir.Editor
{
    public static class DeadAirSceneBuilder
    {
        public const string ScenePath = "Assets/DeadAir/Scenes/DeadAir_Main.unity";
        private const string PlayerTruckPrefabPath = "Assets/LWS/InterstateHauler/Vehicles/Prefabs/IH_PlayerTruck_NWH.prefab";
        private const string DeliveryTrailerPrefabPath = "Assets/LWS/InterstateHauler/Vehicles/Prefabs/IH_TestTrailer_DryVan.prefab";
        private const string DeadAirRootName = "DEAD_AIR";
        private const string EnvironmentRootName = "ENVIRONMENT";

        [MenuItem("Dead Air/Build Or Refresh Main Scene")]
        public static void BuildOrRefreshMainScene()
        {
            Directory.CreateDirectory("Assets/DeadAir/Scenes");
            Scene scene = File.Exists(ScenePath)
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            EnsureRoot(DeadAirRootName);
            EnsureRoot("START");
            Transform systems = EnsureRoot("SYSTEMS");
            EnsureRoot("STORY_TRIGGERS");
            EnsureRoot("CHOICE_TRIGGERS");
            EnsureRoot("ENVIRONMENT");
            Transform ui = EnsureRoot("UI");
            EnsureRoot("DEBUG");
            Transform start = EnsureRoot("START");
            DeadAirStartMarker startMarker = EnsureStartMarker(start);
            GameObject truckPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerTruckPrefabPath);
            GameObject trailerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DeliveryTrailerPrefabPath);

            DeadAirSceneBootstrapper bootstrapper = EnsureComponent<DeadAirSceneBootstrapper>(GameObject.Find("DEAD_AIR").transform, "Dead Air Scene Bootstrapper");
            bootstrapper.ConfigurePrefabs(truckPrefab, trailerPrefab);
            EnsureComponent<DeadAirGameManager>(systems, "Dead Air Game Manager");
            EnsureComponent<DeadAirStoryDirector>(systems, "Dead Air Story Director");
            EnsureComponent<DeadAirAudioDirector>(systems, "Dead Air Audio Director");
            EnsureComponent<DeadAirGPSDirector>(systems, "Dead Air GPS Director");
            EnsureComponent<DeadAirGPSController>(systems, "Dead Air GPS Controller");
            EnsureComponent<DeadAirEndingDirector>(systems, "Dead Air Ending Director");
            EnsureComponent<DeadAirAnomalyDirector>(systems, "Dead Air Anomaly Director");
            EnsureComponent<DeadAirDashboardMisinformationDirector>(systems, "Dead Air Dashboard Director");
            EnsureComponent<DeadAirTrafficHorrorDirector>(systems, "Dead Air Traffic Horror Director");
            DeadAirStartRigController startRig = EnsureComponent<DeadAirStartRigController>(start, "Dead Air Start Rig Controller");
            startRig.Configure(truckPrefab, trailerPrefab, startMarker);
            EnsureComponent<DeadAirCockpitCameraLock>(systems, "Dead Air Cockpit Camera Lock");
            EnsureComponent<DeadAirHud>(ui, "Dead Air HUD");
            EnsurePlayerRig(startRig, startMarker, truckPrefab, trailerPrefab);
            DeadAirBeatLayoutUtility.EnsureUnplacedBeatLayout();
            DeadAirBeatLayoutUtility.EnsureConstructionKit();
            EnsureFinalWorldScaffold();

            EnsurePreviewCamera();
            EnsureLight();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Dead Air] Built/refreshed {ScenePath}.");
        }

        [MenuItem("Dead Air/Prepare Playable Blockout")]
        public static void PreparePlayableBlockout()
        {
            BuildOrRefreshMainScene();
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            DeadAirStartMarker blockoutStart = EnsurePlayableBlockout();

            GameObject truckPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerTruckPrefabPath);
            GameObject trailerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DeliveryTrailerPrefabPath);
            DeadAirStartRigController startRig = Object.FindFirstObjectByType<DeadAirStartRigController>();
            if (startRig != null)
            {
                startRig.Configure(truckPrefab, trailerPrefab, blockoutStart);
                startRig.InitializeRig();
            }

            DeadAirSceneBootstrapper bootstrapper = Object.FindFirstObjectByType<DeadAirSceneBootstrapper>();
            if (bootstrapper != null)
            {
                bootstrapper.ConfigurePrefabs(truckPrefab, trailerPrefab);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[Dead Air] Prepared disposable playable blockout in DeadAir_Main.");
        }

        [MenuItem("Dead Air/Create Unplaced Beat Layout")]
        public static void CreateUnplacedBeatLayout()
        {
            DeadAirBeatLayoutUtility.EnsureUnplacedBeatLayout();
            DeadAirBeatLayoutUtility.EnsureConstructionKit();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[Dead Air] Unplaced beat layout created/refreshed.");
        }

        [MenuItem("Dead Air/Toggle Authoring Gizmos")]
        public static void ToggleAuthoringGizmos()
        {
            DeadAirTriggerZone.ShowDeadAirGizmos = !DeadAirTriggerZone.ShowDeadAirGizmos;
            SceneView.RepaintAll();
            Debug.Log($"[Dead Air] Authoring gizmos {(DeadAirTriggerZone.ShowDeadAirGizmos ? "shown" : "hidden")}.");
        }

        private static Transform EnsureRoot(string name)
        {
            GameObject existing = GameObject.Find(name);
            return existing != null ? existing.transform : new GameObject(name).transform;
        }

        private static Transform EnsureFinalWorldScaffold()
        {
            Transform deadAir = EnsureRoot(DeadAirRootName);
            Transform environment = EnsureChild(deadAir, EnvironmentRootName);
            Transform finalWorld = EnsureChild(environment, DeadAirBeatLayoutUtility.FinalWorldRootName);
            string[] children =
            {
                "DEPOT",
                "ROADS",
                "ROAD_PROPS",
                "SIGNS",
                "VEGETATION",
                "LANDMARKS",
                "LIGHTING",
                "VALID_ROAD_ZONES",
                "GAMEPLAY_PLACEMENT"
            };

            foreach (string child in children)
            {
                EnsureChild(finalWorld, child);
            }

            return finalWorld;
        }

        private static DeadAirStartMarker EnsurePlayableBlockout()
        {
            DeadAirBeatLayoutUtility.EnsureConstructionKit();
            Transform finalWorld = EnsureFinalWorldScaffold();
            Transform blockout = EnsureChild(finalWorld, DeadAirBeatLayoutUtility.PlayableBlockoutRootName, out bool blockoutCreated);
            if (blockoutCreated)
            {
                blockout.localPosition = Vector3.zero;
                blockout.localRotation = Quaternion.identity;
            }

            ConfigureMarker(
                blockout,
                "DISPOSABLE PLAYABLE BLOCKOUT",
                "Temporary 1-2 minute route for truck, trailer, trigger, choice, restart, and void-failure validation. Designer may replace this later.",
                new Color(1f, 0.9f, 0.2f, 0.95f),
                6f);

            Transform depot = EnsureChild(blockout, "DEPOT_START");
            Transform roads = EnsureChild(blockout, "ROADS");
            Transform roadProps = EnsureChild(blockout, "ROAD_PROPS");
            EnsureChild(blockout, "SIGNS");
            EnsureChild(blockout, "VEGETATION");
            EnsureChild(blockout, "LANDMARKS");
            EnsureChild(blockout, "LIGHTING");
            Transform validRoadZones = EnsureChild(blockout, "VALID_ROAD_ZONES");
            Transform gameplay = EnsureChild(blockout, "GAMEPLAY_PLACEMENT");

            DeadAirStartMarker startMarker = EnsureBlockoutDepotStart(depot);
            EnsureBlockoutRoads(roads);
            EnsureBlockoutValidRoadZones(validRoadZones);
            EnsureVoidTestArea(roadProps);
            EnsureBlockoutGameplay(gameplay);
            return startMarker;
        }

        private static DeadAirStartMarker EnsureBlockoutDepotStart(Transform depot)
        {
            Transform package = EnsureChild(depot, "DA_DEPOT_START_TEMPLATE", out bool packageCreated);
            if (packageCreated)
            {
                package.localPosition = Vector3.zero;
                package.localRotation = Quaternion.identity;
            }

            ConfigureMarker(
                package,
                "DEPOT START - BLOCKOUT COPY",
                "Runtime depot start copied from the construction-kit concept. Truck and trailer begin hitched and aligned with the exit road.",
                new Color(0.25f, 1f, 0.55f, 0.95f),
                4f);

            DeadAirStartMarker marker = package.GetComponent<DeadAirStartMarker>() ?? package.gameObject.AddComponent<DeadAirStartMarker>();
            Transform truck = EnsureChild(package, DeadAirStartMarker.TruckStartReferenceName, out bool truckCreated);
            if (truckCreated)
            {
                truck.localPosition = Vector3.zero;
                truck.localRotation = Quaternion.identity;
            }

            Transform trailer = EnsureChild(package, DeadAirStartMarker.TrailerStartReferenceName, out bool trailerCreated);
            if (trailerCreated)
            {
                trailer.localPosition = new Vector3(0f, 0f, -13.5f);
                trailer.localRotation = Quaternion.identity;
            }

            Transform forward = EnsureChild(package, DeadAirStartMarker.ForwardDirectionName, out bool forwardCreated);
            if (forwardCreated)
            {
                forward.localPosition = new Vector3(0f, 0f, 10f);
            }

            marker.ConfigureRigReferences(truck, trailer);
            EnsureBlockoutSurface(package, "DEPOT_COLLIDER_SURFACE", new Vector3(0f, -0.1f, 4f), Vector3.zero, new Vector3(70f, 0.25f, 92f));
            return marker;
        }

        private static void EnsureBlockoutRoads(Transform roads)
        {
            EnsureRoadPieceInstance(roads, "BLOCKOUT_00_DEPOT_EXIT_STRAIGHT", "DA_ROAD_STRAIGHT_LONG", DeadAirRoadPieceKind.Straight, new Vector3(0f, 0f, 78f), Vector3.zero, new Vector3(14f, 0.25f, 110f));
            EnsureRoadPieceInstance(roads, "BLOCKOUT_01_STRAIGHT_TO_CURVE", "DA_ROAD_STRAIGHT_LONG", DeadAirRoadPieceKind.Straight, new Vector3(0f, 0f, 160f), Vector3.zero, new Vector3(14f, 0.25f, 80f));
            EnsureRoadPieceInstance(roads, "BLOCKOUT_02_GENTLE_CURVE_A", "DA_ROAD_CURVE_RIGHT", DeadAirRoadPieceKind.Curve, new Vector3(18f, 0f, 225f), new Vector3(0f, 18f, 0f), new Vector3(14f, 0.25f, 86f));
            EnsureRoadPieceInstance(roads, "BLOCKOUT_03_GENTLE_CURVE_B", "DA_ROAD_CURVE_RIGHT", DeadAirRoadPieceKind.Curve, new Vector3(48f, 0f, 288f), new Vector3(0f, 28f, 0f), new Vector3(14f, 0.25f, 86f));
            EnsureRoadPieceInstance(roads, "BLOCKOUT_04_STRAIGHT_TO_FORK", "DA_ROAD_STRAIGHT_LONG", DeadAirRoadPieceKind.Straight, new Vector3(74f, 0f, 365f), Vector3.zero, new Vector3(14f, 0.25f, 100f));
            EnsureRoadPieceInstance(roads, "BLOCKOUT_05_SIMPLE_FORK", "DA_ROAD_FORK", DeadAirRoadPieceKind.Fork, new Vector3(74f, 0f, 430f), Vector3.zero, new Vector3(54f, 0.25f, 58f));
            EnsureRoadPieceInstance(roads, "BLOCKOUT_06_LEFT_BRANCH", "DA_ROAD_STRAIGHT_LONG", DeadAirRoadPieceKind.Straight, new Vector3(40f, 0f, 508f), new Vector3(0f, -24f, 0f), new Vector3(14f, 0.25f, 120f));
            EnsureRoadPieceInstance(roads, "BLOCKOUT_07_RIGHT_BRANCH", "DA_ROAD_STRAIGHT_LONG", DeadAirRoadPieceKind.Straight, new Vector3(108f, 0f, 508f), new Vector3(0f, 24f, 0f), new Vector3(14f, 0.25f, 120f));
            EnsureRoadPieceInstance(roads, "BLOCKOUT_08_MERGE", "DA_ROAD_MERGE", DeadAirRoadPieceKind.Merge, new Vector3(74f, 0f, 590f), Vector3.zero, new Vector3(80f, 0.25f, 66f));
            EnsureRoadPieceInstance(roads, "BLOCKOUT_09_TEMP_END_STRAIGHT", "DA_ROAD_STRAIGHT_LONG", DeadAirRoadPieceKind.Straight, new Vector3(74f, 0f, 685f), Vector3.zero, new Vector3(14f, 0.25f, 150f));
        }

        private static void EnsureBlockoutValidRoadZones(Transform zones)
        {
            EnsureValidRoadZoneInstance(zones, "DA_RUNTIME_VALID_ROAD_DEPOT", "DA_VALID_ROAD_DEPOT", new Vector3(0f, 1f, 4f), Vector3.zero, new Vector3(88f, 12f, 112f));
            EnsureValidRoadZoneInstance(zones, "DA_RUNTIME_VALID_ROAD_HIGHWAY_00", "DA_VALID_ROAD_HIGHWAY", new Vector3(0f, 1f, 130f), Vector3.zero, new Vector3(34f, 12f, 190f));
            EnsureValidRoadZoneInstance(zones, "DA_RUNTIME_VALID_ROAD_HIGHWAY_CURVE", "DA_VALID_ROAD_HIGHWAY", new Vector3(35f, 1f, 260f), new Vector3(0f, 22f, 0f), new Vector3(40f, 12f, 180f));
            EnsureValidRoadZoneInstance(zones, "DA_RUNTIME_VALID_ROAD_FORK_AND_BRANCHES", "DA_VALID_ROAD_FORK", new Vector3(74f, 1f, 510f), Vector3.zero, new Vector3(170f, 12f, 230f));
            EnsureValidRoadZoneInstance(zones, "DA_RUNTIME_VALID_ROAD_MERGE_TO_TEMP_END", "DA_VALID_ROAD_HIGHWAY", new Vector3(74f, 1f, 675f), Vector3.zero, new Vector3(38f, 12f, 190f));
        }

        private static void EnsureVoidTestArea(Transform roadProps)
        {
            Transform voidRoot = EnsureChild(roadProps, "VOID_FAILURE_TEST_AREA", out bool created);
            if (created)
            {
                voidRoot.localPosition = new Vector3(150f, 0f, 235f);
                voidRoot.localRotation = Quaternion.identity;
            }

            ConfigureMarker(
                voidRoot,
                "VOID FAILURE TEST AREA",
                "Drive the entire truck and trailer here, outside valid-road zones, to validate the Sucked Into the Void flow.",
                new Color(1f, 0.2f, 0.2f, 0.95f),
                8f);
            EnsureBlockoutSurface(voidRoot, "VOID_TEST_DRIVEABLE_SURFACE_NO_VALID_ZONE", Vector3.zero, Vector3.zero, new Vector3(76f, 0.2f, 120f));
        }

        private static void EnsureBlockoutGameplay(Transform gameplay)
        {
            DeadAirTriggerZone story = EnsureBlockoutTrigger<DeadAirTriggerZone>(
                gameplay,
                "TEST_STORY_TRIGGER",
                BlockoutBeat("TEST_STORY_TRIGGER", "TEST STORY TRIGGER FIRED", DeadAirTriggerCategory.Story, 0.08f, "TEST STORY TRIGGER FIRED"),
                900,
                new Vector3(0f, 2f, 118f),
                new Vector3(20f, 8f, 18f));
            DeadAirTriggerZone gpsFlash = EnsureBlockoutTrigger<DeadAirTriggerZone>(
                gameplay,
                "TEST_GPS_FLASH_TRIGGER",
                BlockoutBeat("TEST_GPS_FLASH_TRIGGER", "GPS FLASH TEST", DeadAirTriggerCategory.GPS, 0.16f, "Dead Air GPS flashes and changes direction."),
                901,
                new Vector3(0f, 2f, 205f),
                new Vector3(20f, 8f, 18f));
            gpsFlash.ConfigureGpsEvent(new DeadAirGpsNarrativeEvent
            {
                enableGpsEvent = true,
                eventType = DeadAirGpsNarrativeEventType.FlashAndChangeDirection,
                primaryText = "EXIT 12",
                secondaryText = "1 MI",
                arrow = DeadAirGpsArrow.ExitRight,
                distanceMeters = 1609.344f,
                destination = "Exit 12",
                routeVisible = true,
                intentionallyWrong = true,
                flash = true
            });

            DeadAirTriggerZone gpsRecalculating = EnsureBlockoutTrigger<DeadAirTriggerZone>(
                gameplay,
                "TEST_GPS_RECALCULATING_TRIGGER",
                BlockoutBeat("TEST_GPS_RECALCULATING_TRIGGER", "GPS RECALCULATING TEST", DeadAirTriggerCategory.GPS, 0.22f, "Dead Air GPS glitches, recalculates, then lies."),
                902,
                new Vector3(18f, 2f, 288f),
                new Vector3(20f, 8f, 18f));
            gpsRecalculating.ConfigureGpsEvent(new DeadAirGpsNarrativeEvent
            {
                enableGpsEvent = true,
                eventType = DeadAirGpsNarrativeEventType.RecalculatingThenDirection,
                primaryText = "RECALCULATING...",
                secondaryText = string.Empty,
                arrow = DeadAirGpsArrow.None,
                distanceMeters = 1609.344f,
                destination = "Exit 12",
                routeVisible = true,
                intentionallyWrong = true,
                flash = true,
                glitch = true,
                glitchDuration = 0.75f,
                glitchIntensity = 0.75f,
                recalculating = true,
                recalculatingMessage = "RECALCULATING...",
                recalculatingDuration = 1.4f,
                finalPrimaryText = "TAKE EXIT 12",
                finalSecondaryText = "1 MI",
                finalArrow = DeadAirGpsArrow.ExitRight
            });

            DeadAirChoiceStartTrigger choiceStart = EnsureBlockoutTrigger<DeadAirChoiceStartTrigger>(
                gameplay,
                "TEST_CHOICE_START",
                BlockoutBeat("TEST_CHOICE_START", "TEST CHOICE START", DeadAirTriggerCategory.ChoiceStart, 0.28f, "Temporary fork choice begins."),
                910,
                new Vector3(74f, 2f, 412f),
                new Vector3(50f, 8f, 24f));
            choiceStart.ConfigureChoice("TEST_CHOICE");

            DeadAirChoiceCommitTrigger leftCommit = EnsureBlockoutTrigger<DeadAirChoiceCommitTrigger>(
                gameplay,
                "TEST_LEFT_COMMIT",
                BlockoutBeat("TEST_LEFT_COMMIT", "TEST LEFT COMMIT", DeadAirTriggerCategory.ChoiceCommit, 0.36f, "Temporary left fork commit."),
                911,
                new Vector3(34f, 2f, 530f),
                new Vector3(26f, 8f, 28f));
            leftCommit.ConfigureChoice("TEST_CHOICE", DeadAirChoiceOutcome.TrustDispatch);

            DeadAirChoiceCommitTrigger rightCommit = EnsureBlockoutTrigger<DeadAirChoiceCommitTrigger>(
                gameplay,
                "TEST_RIGHT_COMMIT",
                BlockoutBeat("TEST_RIGHT_COMMIT", "TEST RIGHT COMMIT", DeadAirTriggerCategory.ChoiceCommit, 0.36f, "Temporary right fork commit."),
                912,
                new Vector3(114f, 2f, 530f),
                new Vector3(26f, 8f, 28f));
            rightCommit.ConfigureChoice("TEST_CHOICE", DeadAirChoiceOutcome.TrustGPS);

            ConfigureMarker(
                story.transform,
                "TEST STORY TRIGGER",
                "Temporary physical story trigger. Expected overlay text: TEST STORY TRIGGER FIRED.",
                new Color(0.2f, 0.85f, 1f, 0.95f),
                3f);
        }

        private static void EnsureRoadPieceInstance(Transform parent, string name, string templateName, DeadAirRoadPieceKind kind, Vector3 localPosition, Vector3 localEuler, Vector3 surfaceScale)
        {
            GameObject go = FindChildGameObject(parent, name);
            bool created = go == null;
            if (go == null)
            {
                GameObject template = FindConstructionTemplate(templateName);
                go = template != null ? Object.Instantiate(template, parent) : new GameObject(name);
                go.name = name;
                go.transform.SetParent(parent, false);
            }

            if (created)
            {
                go.transform.localPosition = localPosition;
                go.transform.localRotation = Quaternion.Euler(localEuler);
            }

            DeadAirRoadPiece piece = go.GetComponent<DeadAirRoadPiece>() ?? go.AddComponent<DeadAirRoadPiece>();
            piece.Configure(kind, "DISPOSABLE PLAYABLE BLOCKOUT ROAD PIECE. Replace during final Dead Air map construction.");
            EnsureBlockoutSurface(go.transform, "COLLIDER_SURFACE", Vector3.zero, Vector3.zero, surfaceScale);
        }

        private static void EnsureValidRoadZoneInstance(Transform parent, string name, string templateName, Vector3 localPosition, Vector3 localEuler, Vector3 size)
        {
            GameObject go = FindChildGameObject(parent, name);
            bool created = go == null;
            if (go == null)
            {
                GameObject template = FindConstructionTemplate(templateName);
                go = template != null ? Object.Instantiate(template, parent) : new GameObject(name);
                go.name = name;
                go.transform.SetParent(parent, false);
            }

            if (created)
            {
                go.transform.localPosition = localPosition;
                go.transform.localRotation = Quaternion.Euler(localEuler);
            }

            DeadAirValidRoadZone zone = go.GetComponent<DeadAirValidRoadZone>() ?? go.AddComponent<DeadAirValidRoadZone>();
            if (created)
            {
                zone.Configure(name, size);
            }
        }

        private static T EnsureBlockoutTrigger<T>(Transform parent, string name, DeadAirBeatDefinition beat, int sequenceIndex, Vector3 localPosition, Vector3 colliderSize) where T : DeadAirTriggerZone
        {
            GameObject go = FindChildGameObject(parent, name);
            bool created = go == null;
            if (go == null)
            {
                go = new GameObject(name);
                go.transform.SetParent(parent, false);
                go.transform.localPosition = localPosition;
            }

            T trigger = go.GetComponent<T>();
            bool componentCreated = trigger == null;
            if (trigger == null)
            {
                trigger = go.AddComponent<T>();
            }

            if (created || componentCreated)
            {
                trigger.Configure(beat, sequenceIndex);
                BoxCollider collider = go.GetComponent<BoxCollider>();
                if (collider != null)
                {
                    collider.isTrigger = true;
                    collider.size = colliderSize;
                }
            }

            return trigger;
        }

        private static GameObject EnsureBlockoutSurface(Transform parent, string name, Vector3 localPosition, Vector3 localEuler, Vector3 localScale)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                BoxCollider existingCollider = existing.GetComponent<BoxCollider>();
                if (existingCollider != null)
                {
                    existingCollider.isTrigger = false;
                }

                return existing.gameObject;
            }

            GameObject surface = GameObject.CreatePrimitive(PrimitiveType.Cube);
            surface.name = name;
            surface.transform.SetParent(parent, false);
            surface.transform.localPosition = localPosition;
            surface.transform.localRotation = Quaternion.Euler(localEuler);
            surface.transform.localScale = localScale;
            BoxCollider collider = surface.GetComponent<BoxCollider>();
            if (collider != null)
            {
                collider.isTrigger = false;
            }

            return surface;
        }

        private static GameObject FindConstructionTemplate(string templateName)
        {
            GameObject kit = GameObject.Find(DeadAirBeatLayoutUtility.ConstructionKitRootName);
            Transform found = FindSceneObjectByName(kit != null ? kit.transform : null, templateName);
            return found != null ? found.gameObject : null;
        }

        private static Transform FindSceneObjectByName(Transform root, string objectName)
        {
            if (root == null)
            {
                return null;
            }

            if (root.name == objectName)
            {
                return root;
            }

            foreach (Transform child in root)
            {
                Transform found = FindSceneObjectByName(child, objectName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static GameObject FindChildGameObject(Transform parent, string name)
        {
            if (parent == null)
            {
                return null;
            }

            Transform child = parent.Find(name);
            return child != null ? child.gameObject : null;
        }

        private static void ConfigureMarker(Transform target, string markerId, string notes, Color color, float radius)
        {
            DeadAirAuthoringMarker marker = target.GetComponent<DeadAirAuthoringMarker>() ?? target.gameObject.AddComponent<DeadAirAuthoringMarker>();
            marker.Configure(markerId, notes, color, radius);
        }

        private static DeadAirBeatDefinition BlockoutBeat(string id, string displayName, DeadAirTriggerCategory category, float routeMiles, string description)
        {
            return new DeadAirBeatDefinition
            {
                beatId = id,
                displayName = displayName,
                category = category,
                routeDistanceMiles = routeMiles,
                description = description,
                notes = "TEMPORARY DISPOSABLE PLAYABLE BLOCKOUT. Not final Dead Air pacing, dialogue, or route content.",
                estimatedPlaybackSeconds = 4f,
                debugColor = BlockoutColor(category),
                placementStatus = DeadAirPlacementStatus.Draft,
                critical = false
            };
        }

        private static Color BlockoutColor(DeadAirTriggerCategory category)
        {
            switch (category)
            {
                case DeadAirTriggerCategory.ChoiceStart:
                case DeadAirTriggerCategory.ChoiceCommit:
                    return new Color(1f, 0.15f, 0.9f, 0.35f);
                case DeadAirTriggerCategory.Ending:
                    return new Color(1f, 1f, 1f, 0.35f);
                default:
                    return new Color(0.2f, 0.85f, 1f, 0.35f);
            }
        }

        private static T EnsureComponent<T>(Transform parent, string name) where T : Component
        {
            T existing = Object.FindFirstObjectByType<T>();
            if (existing != null)
            {
                if (existing.transform.parent == null && parent != null)
                {
                    existing.transform.SetParent(parent, false);
                }

                return existing;
            }

            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<T>();
        }

        private static void EnsurePreviewCamera()
        {
            if (Camera.main != null)
            {
                return;
            }

            GameObject cameraObject = new GameObject("Dead Air Preview Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 18f, -38f);
            cameraObject.transform.rotation = Quaternion.Euler(22f, 0f, 0f);
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }

        private static void EnsureLight()
        {
            if (Object.FindFirstObjectByType<Light>() != null)
            {
                return;
            }

            GameObject lightObject = new GameObject("Dead Air Directional Light");
            lightObject.transform.rotation = Quaternion.Euler(55f, -50f, 0f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.65f;
            light.color = new Color(0.55f, 0.7f, 0.78f, 1f);
        }

        private static DeadAirStartMarker EnsureStartMarker(Transform start)
        {
            Transform marker = start.Find("DeadAirStartMarker");
            if (marker == null)
            {
                marker = new GameObject("DeadAirStartMarker").transform;
                marker.SetParent(start, false);
            }

            DeadAirStartMarker startMarker = marker.GetComponent<DeadAirStartMarker>();
            if (startMarker == null)
            {
                startMarker = marker.gameObject.AddComponent<DeadAirStartMarker>();
            }

            Transform truck = EnsureChild(marker, "TRUCK_START_REFERENCE");
            Transform trailer = EnsureChild(marker, "TRAILER_START_REFERENCE");
            if (trailer.localPosition == Vector3.zero)
            {
                trailer.localPosition = new Vector3(0f, 0f, -13.5f);
            }

            EnsureChild(marker, "FORWARD_DIRECTION").localPosition = new Vector3(0f, 0f, 8f);
            startMarker.ConfigureRigReferences(truck, trailer);
            return startMarker;
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

        private static Transform EnsureChild(Transform parent, string name, out bool created)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                created = false;
                return existing;
            }

            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            created = true;
            return child.transform;
        }

        private static void EnsurePlayerRig(DeadAirStartRigController startRig, DeadAirStartMarker startMarker, GameObject truckPrefab, GameObject trailerPrefab)
        {
            if (Object.FindFirstObjectByType<DeadAirVehicleAdapter>() != null)
            {
                startRig?.InitializeRig();
                return;
            }

            if (truckPrefab == null)
            {
                Debug.LogWarning($"[Dead Air] Player truck prefab missing: {PlayerTruckPrefabPath}");
                return;
            }

            if (startRig != null)
            {
                startRig.Configure(truckPrefab, trailerPrefab, startMarker);
                startRig.InitializeRig();
                return;
            }

            GameObject truck = PrefabUtility.InstantiatePrefab(truckPrefab) as GameObject;
            if (truck == null)
            {
                return;
            }

            truck.name = "Dead Air Player Truck";
            if (startMarker != null)
            {
                startMarker.GetTruckPose(out Vector3 position, out Quaternion rotation);
                truck.transform.SetPositionAndRotation(position, rotation);
            }

            if (truck.GetComponent<DeadAirVehicleAdapter>() == null)
            {
                truck.AddComponent<DeadAirVehicleAdapter>();
            }

            if (truck.GetComponent<LWS.InterstateHauler.LwsKeyboardGamepadTruckInputSource>() == null)
            {
                truck.AddComponent<LWS.InterstateHauler.LwsKeyboardGamepadTruckInputSource>();
            }
        }
    }
}
