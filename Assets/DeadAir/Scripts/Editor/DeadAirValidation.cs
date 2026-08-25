using System.Collections.Generic;
using System.IO;
using System.Linq;
using LWS.InterstateHauler;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DeadAir.Editor
{
    public static class DeadAirValidation
    {
        private static readonly string[] RequiredRoots =
        {
            "DEAD_AIR",
            "START",
            "SYSTEMS",
            "STORY_TRIGGERS",
            "CHOICE_TRIGGERS",
            "ENVIRONMENT",
            "UI",
            "DEBUG"
        };

        [MenuItem("Dead Air/Validate Jam Mode")]
        public static void ValidateJamModeMenu()
        {
            ValidateJamMode();
        }

        public static bool ValidateJamMode()
        {
            List<string> errors = new List<string>();
            List<string> warnings = new List<string>();
            ValidateFolders(errors);
            ValidateScene(errors, warnings);
            ValidateWebGlSurface(warnings);

            foreach (string warning in warnings)
            {
                Debug.LogWarning($"[Dead Air Validation] {warning}");
            }

            foreach (string error in errors)
            {
                Debug.LogError($"[Dead Air Validation] {error}");
            }

            if (errors.Count == 0)
            {
                Debug.Log($"[Dead Air Validation] PASS with {warnings.Count} warning(s).");
                return true;
            }

            Debug.LogError($"[Dead Air Validation] FAIL with {errors.Count} error(s) and {warnings.Count} warning(s).");
            return false;
        }

        private static void ValidateFolders(List<string> errors)
        {
            string[] folders =
            {
                "Assets/DeadAir/Scenes",
                "Assets/DeadAir/Scripts/Core",
                "Assets/DeadAir/Scripts/Story",
                "Assets/DeadAir/Scripts/Vehicle",
                "Assets/DeadAir/Scripts/Audio",
                "Assets/DeadAir/Scripts/UI",
                "Assets/DeadAir/Scripts/Environment",
                "Assets/DeadAir/Scripts/Editor",
                "Assets/DeadAir/Prefabs",
                "Assets/DeadAir/Materials",
                "Assets/DeadAir/Audio",
                "Assets/DeadAir/Art",
                "Assets/DeadAir/Environment",
                "Assets/DeadAir/ScriptableObjects",
                "Assets/DeadAir/UI"
            };

            foreach (string folder in folders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    errors.Add($"Missing folder: {folder}");
                }
            }
        }

        private static void ValidateScene(List<string> errors, List<string> warnings)
        {
            string scenePath = DeadAirSceneBuilder.ScenePath;
            if (!File.Exists(scenePath))
            {
                errors.Add($"Missing Dead Air scene: {scenePath}");
                return;
            }

            bool openedScene = false;
            if (EditorSceneManager.GetActiveScene().path != scenePath)
            {
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                openedScene = true;
            }

            foreach (string root in RequiredRoots)
            {
                if (GameObject.Find(root) == null)
                {
                    errors.Add($"Missing scene root: {root}");
                }
            }

            if (Object.FindObjectsByType<DeadAirGameManager>(FindObjectsSortMode.None).Length > 1)
            {
                errors.Add("Duplicate DeadAirGameManager instances found.");
            }

            if (Object.FindFirstObjectByType<DeadAirSceneBootstrapper>() == null)
            {
                errors.Add("DeadAirSceneBootstrapper is missing.");
            }

            if (Object.FindFirstObjectByType<DeadAirDashboardMisinformationDirector>() == null)
            {
                warnings.Add("DeadAirDashboardMisinformationDirector is not serialized in the scene; the bootstrapper/builder should create it.");
            }

            if (Object.FindFirstObjectByType<DeadAirTrafficHorrorDirector>() == null)
            {
                warnings.Add("DeadAirTrafficHorrorDirector is not serialized in the scene; the bootstrapper/builder should create it.");
            }

            DeadAirTriggerZone[] triggers = Object.FindObjectsByType<DeadAirTriggerZone>(FindObjectsSortMode.None);
            HashSet<string> beatIds = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            foreach (DeadAirTriggerZone trigger in triggers)
            {
                if (trigger == null)
                {
                    continue;
                }

                bool constructionTemplate = DeadAirBeatLayoutUtility.IsUnderNamedRoot(trigger.transform, DeadAirBeatLayoutUtility.ConstructionKitRootName);
                if (string.IsNullOrWhiteSpace(trigger.BeatId))
                {
                    errors.Add($"Trigger {trigger.name} has no Beat ID.");
                    continue;
                }

                if (!constructionTemplate && !beatIds.Add(trigger.BeatId))
                {
                    errors.Add($"Duplicate Beat ID: {trigger.BeatId}");
                }

                BoxCollider box = trigger.GetComponent<BoxCollider>();
                if (box == null || !box.isTrigger)
                {
                    errors.Add($"Trigger {trigger.BeatId} needs a trigger BoxCollider.");
                }

                if (!constructionTemplate && trigger.TriggerOnce && trigger.Notes != null && trigger.Notes.Contains("UNPLACED"))
                {
                    warnings.Add($"Critical beat is still unplaced: {trigger.BeatId}");
                }
            }

            IReadOnlyList<DeadAirBeatDefinition> expected = DeadAirBeatLayoutUtility.CreateDefaultBeatDefinitions();
            foreach (DeadAirBeatDefinition beat in expected.Where(beat => beat.critical))
            {
                if (!beatIds.Contains(beat.beatId))
                {
                    warnings.Add($"Critical unplaced beat has not been instantiated yet: {beat.beatId}");
                }
            }

            ValidateChoiceGroups(warnings);
            ValidateConstructionKit(errors, warnings);
            ValidateBasicAutomaticSeam(errors);
            ValidateCockpitTrailerStartRig(errors, warnings);
            ValidateOffRoadFailure(errors, warnings);
            ValidatePlayableBlockout(errors, warnings);

            if (openedScene)
            {
                Debug.Log("[Dead Air Validation] DeadAir_Main was opened for validation.");
            }
        }

        private static void ValidateChoiceGroups(List<string> warnings)
        {
            string[] choices = { "CHOICE_01", "CHOICE_02", "CHOICE_03_EXIT17" };
            string[] suffixes = { "_START", "_LEFT_COMMIT", "_RIGHT_COMMIT", "_STRAIGHT_COMMIT" };
            GameObject root = GameObject.Find(DeadAirBeatLayoutUtility.UnplacedRootName);
            if (root == null)
            {
                warnings.Add("Dead Air unplaced gameplay root is missing, so choice group scaffolds could not be checked.");
                return;
            }

            foreach (string choice in choices)
            {
                Transform group = root.transform.Find(choice);
                if (group == null)
                {
                    warnings.Add($"Choice group root missing: {choice}");
                    continue;
                }

                foreach (string suffix in suffixes)
                {
                    if (group.Find(choice + suffix) == null)
                    {
                        warnings.Add($"Choice group {choice} is missing child {choice + suffix}.");
                    }
                }
            }
        }

        private static void ValidateConstructionKit(List<string> errors, List<string> warnings)
        {
            GameObject root = GameObject.Find(DeadAirBeatLayoutUtility.ConstructionKitRootName);
            if (root == null)
            {
                warnings.Add("Dead Air construction kit root is not serialized in the scene; run Dead Air/Build Or Refresh Main Scene or enter Play Mode to let the bootstrapper create it.");
                return;
            }

            string[] requiredChildren =
            {
                "ROAD_PIECES",
                "CHOICE_PIECES",
                "STORY_TRIGGERS",
                "SIGNS",
                "TRAFFIC_EVENTS",
                "ENVIRONMENT_EVENTS",
                "DASHBOARD_EVENTS",
                "GPS_EVENTS",
                "AUDIO_EVENTS",
                "ENDING_PIECES",
                "DEBUG_REFERENCE"
            };

            foreach (string child in requiredChildren)
            {
                if (root.transform.Find(child) == null)
                {
                    warnings.Add($"Construction kit child missing: {child}");
                }
            }

            if (Object.FindObjectsByType<DeadAirRoadPiece>(FindObjectsSortMode.None).Length == 0)
            {
                warnings.Add("Construction kit has no DeadAirRoadPiece templates.");
            }

            if (Object.FindObjectsByType<DeadAirSign>(FindObjectsSortMode.None).Length == 0)
            {
                warnings.Add("Construction kit has no DeadAirSign templates.");
            }

            string[] requiredTemplateNames =
            {
                "DA_ROAD_STRAIGHT_SHORT",
                "DA_ROAD_STRAIGHT_LONG",
                "DA_ROAD_CURVE_LEFT",
                "DA_ROAD_CURVE_RIGHT",
                "DA_ROAD_FORK",
                "DA_ROAD_EXIT_RIGHT",
                "DA_ROAD_MERGE",
                "DA_CHOICE_TEMPLATE",
                "DA_EXIT_CHOICE_TEMPLATE",
                "DA_TRIGGER_DISPATCH",
                "DA_TRIGGER_CB",
                "DA_TRIGGER_GPS",
                "DA_TRIGGER_ENVIRONMENT",
                "DA_TRIGGER_TRAFFIC",
                "DA_TRIGGER_DASHBOARD",
                "DA_TRIGGER_GENERIC_STORY",
                "DA_TRIGGER_ENDING",
                "DA_SIGN_DESTINATION",
                "DA_SIGN_MILEAGE",
                "DA_SIGN_EXIT",
                "DA_SIGN_WARNING",
                "DA_SIGN_ROUTE",
                "AUDIO_MULTI_CHANNEL_SEQUENCE",
                "GPS_RESTORE",
                "DASHBOARD_RESTORE",
                "DA_TRAFFIC_HEADLIGHTS",
                "DA_DEPOT_START_TEMPLATE",
                "DA_VALID_ROAD_ZONE",
                "DA_VALID_ROAD_DEPOT",
                "DA_VALID_ROAD_HIGHWAY",
                "DA_VALID_ROAD_FORK"
            };

            foreach (string templateName in requiredTemplateNames)
            {
                if (FindSceneObjectByName(root.transform, templateName) == null)
                {
                    warnings.Add($"Construction kit template missing: {templateName}");
                }
            }
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

        private static void ValidateBasicAutomaticSeam(List<string> errors)
        {
            const string sharedInputPath = "Assets/LWS/InterstateHauler/Input/LwsKeyboardGamepadTruckInputSource.cs";
            const string transmissionPath = "Assets/LWS/InterstateHauler/Vehicles/Transmission/Lws18SpeedTransmissionController.cs";
            string sharedInput = File.Exists(sharedInputPath) ? File.ReadAllText(sharedInputPath) : string.Empty;
            string transmission = File.Exists(transmissionPath) ? File.ReadAllText(transmissionPath) : string.Empty;
            bool sharedMode = sharedInput.Contains("LwsTruckInputMode") &&
                              sharedInput.Contains("BasicAutomatic") &&
                              sharedInput.Contains("TrySetInputMode");
            bool authority = transmission.Contains("TrySetAutomaticMode") &&
                             transmission.Contains("TrySetTransmissionMode") &&
                             !sharedInput.Contains("ShiftInto(");

            if (!sharedMode)
            {
                errors.Add("Shared LWS Basic Automatic input mode is missing.");
            }

            if (!authority)
            {
                errors.Add("Basic Automatic does not appear to route through Lws18SpeedTransmissionController authority.");
            }
        }

        private static void ValidateCockpitTrailerStartRig(List<string> errors, List<string> warnings)
        {
            const string sharedInputPath = "Assets/LWS/InterstateHauler/Input/LwsKeyboardGamepadTruckInputSource.cs";
            const string deadAirFallbackInputPath = "Assets/DeadAir/Scripts/Vehicle/DeadAirBasicAutomaticInputSource.cs";
            const string cockpitLockPath = "Assets/DeadAir/Scripts/Vehicle/DeadAirCockpitCameraLock.cs";
            const string startRigPath = "Assets/DeadAir/Scripts/Vehicle/DeadAirStartRigController.cs";
            const string startMarkerPath = "Assets/DeadAir/Scripts/Story/DeadAirStartMarker.cs";

            string sharedInput = File.Exists(sharedInputPath) ? File.ReadAllText(sharedInputPath) : string.Empty;
            string fallbackInput = File.Exists(deadAirFallbackInputPath) ? File.ReadAllText(deadAirFallbackInputPath) : string.Empty;
            string cockpitLock = File.Exists(cockpitLockPath) ? File.ReadAllText(cockpitLockPath) : string.Empty;
            string startRig = File.Exists(startRigPath) ? File.ReadAllText(startRigPath) : string.Empty;
            string startMarker = File.Exists(startMarkerPath) ? File.ReadAllText(startMarkerPath) : string.Empty;

            if (!sharedInput.Contains("SetCameraCycleSuppressed") || !sharedInput.Contains("SuppressCameraCycle"))
            {
                errors.Add("Shared LWS input source does not expose Dead Air camera-cycle suppression.");
            }

            if (fallbackInput.Contains("commands.cameraCycle = Edge("))
            {
                errors.Add("DeadAirBasicAutomaticInputSource still maps a direct camera-cycle command.");
            }

            if (!cockpitLock.Contains("LwsVehicleCameraMode.Cockpit") || !cockpitLock.Contains("CameraCycleBlocked"))
            {
                errors.Add("DeadAirCockpitCameraLock is missing or does not publish cockpit-only state.");
            }

            if (!startRig.Contains("deliveryTrailerPrefab") || !startRig.Contains("RequestTrailerAttachDetach"))
            {
                errors.Add("DeadAirStartRigController is missing trailer prefab/coupling setup.");
            }

            if (!startMarker.Contains("TRUCK_START_REFERENCE") || !startMarker.Contains("GetTruckPose"))
            {
                errors.Add("DeadAirStartMarker does not expose complete rig truck/trailer poses.");
            }

            DeadAirCockpitCameraLock cockpit = Object.FindFirstObjectByType<DeadAirCockpitCameraLock>();
            if (cockpit == null)
            {
                warnings.Add("DeadAirCockpitCameraLock is not serialized in the scene; run Dead Air/Build Or Refresh Main Scene or enter Play Mode to let the bootstrapper create it.");
            }

            DeadAirStartRigController rig = Object.FindFirstObjectByType<DeadAirStartRigController>();
            if (rig == null)
            {
                warnings.Add("DeadAirStartRigController is not serialized in the scene; run Dead Air/Build Or Refresh Main Scene or enter Play Mode to let the bootstrapper create it.");
                return;
            }

            if (rig.StartMarker == null)
            {
                warnings.Add("DeadAirStartRigController has no DeadAirStartMarker reference.");
            }

            if (!rig.TrailerReferenceAvailable)
            {
                warnings.Add("DeadAirStartRigController has no delivery trailer instance or prefab assigned.");
            }

            if (!rig.CouplingRequestEnabled)
            {
                warnings.Add("DeadAirStartRigController is not configured to request trailer coupling on run start.");
            }

            if (rig.PlayerTruck == null && rig.PlayerTruckPrefab == null)
            {
                warnings.Add("DeadAirStartRigController has no player truck instance or prefab assigned.");
            }
        }

        private static void ValidateOffRoadFailure(List<string> errors, List<string> warnings)
        {
            const string zonePath = "Assets/DeadAir/Scripts/Environment/DeadAirValidRoadZone.cs";
            const string controllerPath = "Assets/DeadAir/Scripts/Vehicle/DeadAirOffRoadFailureController.cs";
            const string managerPath = "Assets/DeadAir/Scripts/Core/DeadAirGameManager.cs";
            const string endingPath = "Assets/DeadAir/Scripts/Core/DeadAirEndingDirector.cs";

            string zone = File.Exists(zonePath) ? File.ReadAllText(zonePath) : string.Empty;
            string controller = File.Exists(controllerPath) ? File.ReadAllText(controllerPath) : string.Empty;
            string manager = File.Exists(managerPath) ? File.ReadAllText(managerPath) : string.Empty;
            string ending = File.Exists(endingPath) ? File.ReadAllText(endingPath) : string.Empty;

            if (!zone.Contains("ContainsWorldPoint") || !zone.Contains("ShowRoadBoundaryGizmos"))
            {
                errors.Add("DeadAirValidRoadZone is missing authorable valid-road volume behavior.");
            }

            if (!controller.Contains("EvaluateRigRoadState") ||
                !controller.Contains("UpdateGraceTimer") ||
                !controller.Contains("RequestVoidFailure"))
            {
                errors.Add("DeadAirOffRoadFailureController is missing complete-rig/grace/void failure behavior.");
            }

            if (!manager.Contains("RequestVoidFailure") || !manager.Contains("OffRoadFailureController"))
            {
                errors.Add("DeadAirGameManager is not wired to the off-road void failure controller.");
            }

            if (!ending.Contains("SuckedIntoVoid") || !ending.Contains("SUCKED INTO THE VOID"))
            {
                errors.Add("Dead Air void loss screen config is missing.");
            }

            DeadAirOffRoadFailureController failure = Object.FindFirstObjectByType<DeadAirOffRoadFailureController>();
            if (failure == null)
            {
                warnings.Add("DeadAirOffRoadFailureController is not serialized in the scene; run Dead Air/Build Or Refresh Main Scene or enter Play Mode to let the bootstrapper create it.");
            }

            DeadAirValidRoadZone[] zones = Object.FindObjectsByType<DeadAirValidRoadZone>(FindObjectsSortMode.None);
            DeadAirValidRoadZone[] runtimeZones = zones.Where(z => z != null && z.RuntimeCandidate).ToArray();
            if (runtimeZones.Length == 0)
            {
                warnings.Add("No runtime DeadAirValidRoadZone volumes exist in the scene yet. Duplicate DA_VALID_ROAD_* templates out of the construction kit before playable route validation.");
                return;
            }

            DeadAirStartMarker start = Object.FindFirstObjectByType<DeadAirStartMarker>();
            if (start != null)
            {
                start.GetTruckPose(out Vector3 truckPosition, out _);
                start.GetTrailerPose(out Vector3 trailerPosition, out _);
                bool truckCovered = runtimeZones.Any(z => z != null && z.ContainsWorldPoint(truckPosition));
                bool trailerCovered = runtimeZones.Any(z => z != null && z.ContainsWorldPoint(trailerPosition));
                if (!truckCovered || !trailerCovered)
                {
                    warnings.Add("DeadAirStartMarker truck/trailer start points are not both covered by valid-road zones.");
                }
            }
        }

        private static void ValidatePlayableBlockout(List<string> errors, List<string> warnings)
        {
            GameObject deadAirRoot = GameObject.Find("DEAD_AIR");
            Transform finalWorld = FindSceneObjectByName(deadAirRoot != null ? deadAirRoot.transform : null, DeadAirBeatLayoutUtility.FinalWorldRootName);
            if (finalWorld == null)
            {
                errors.Add("Dead Air FINAL_WORLD root is missing under DEAD_AIR/ENVIRONMENT.");
                return;
            }

            string[] finalWorldChildren =
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

            foreach (string child in finalWorldChildren)
            {
                if (finalWorld.Find(child) == null)
                {
                    errors.Add($"FINAL_WORLD child missing: {child}");
                }
            }

            Transform blockout = finalWorld.Find(DeadAirBeatLayoutUtility.PlayableBlockoutRootName);
            if (blockout == null)
            {
                errors.Add("DEAD_AIR_PLAYABLE_BLOCKOUT is missing under FINAL_WORLD.");
                return;
            }

            DeadAirStartMarker[] startMarkers = blockout.GetComponentsInChildren<DeadAirStartMarker>(true);
            DeadAirStartMarker blockoutStart = startMarkers.Length > 0 ? startMarkers[0] : null;
            if (blockoutStart == null)
            {
                errors.Add("Playable blockout is missing a DeadAirStartMarker depot start.");
            }
            else
            {
                if (blockoutStart.TruckStartReference == null)
                {
                    errors.Add("Playable blockout start marker is missing TRUCK_START_REFERENCE.");
                }

                if (blockoutStart.TrailerStartReference == null)
                {
                    errors.Add("Playable blockout start marker is missing TRAILER_START_REFERENCE.");
                }
            }

            DeadAirStartRigController rig = Object.FindFirstObjectByType<DeadAirStartRigController>();
            if (rig == null)
            {
                errors.Add("Playable blockout cannot initialize because DeadAirStartRigController is missing.");
            }
            else
            {
                if (blockoutStart != null && rig.StartMarker != blockoutStart)
                {
                    warnings.Add("DeadAirStartRigController is not serialized against the playable blockout start marker; Prepare Playable Blockout will repair this.");
                }

                if (rig.PlayerTruckPrefab == null)
                {
                    errors.Add("Playable blockout start rig is missing IH_PlayerTruck_NWH prefab reference.");
                }

                if (rig.DeliveryTrailerPrefab == null)
                {
                    errors.Add("Playable blockout start rig is missing IH_TestTrailer_DryVan prefab reference.");
                }
            }

            if (Object.FindFirstObjectByType<DeadAirCockpitCameraLock>() == null)
            {
                errors.Add("Playable blockout requires DeadAirCockpitCameraLock.");
            }

            if (Object.FindFirstObjectByType<DeadAirOffRoadFailureController>() == null)
            {
                errors.Add("Playable blockout requires DeadAirOffRoadFailureController.");
            }

            DeadAirRoadPiece[] roadPieces = blockout.GetComponentsInChildren<DeadAirRoadPiece>(true);
            if (roadPieces.Length < 8)
            {
                errors.Add("Playable blockout does not contain enough temporary road pieces for the requested route.");
            }

            DeadAirValidRoadZone[] zones = blockout.GetComponentsInChildren<DeadAirValidRoadZone>(true)
                .Where(z => z != null && z.RuntimeCandidate)
                .ToArray();
            if (zones.Length < 5)
            {
                errors.Add("Playable blockout requires runtime valid-road zones for depot, highway, fork, branches, and merge/end.");
            }

            string[] expectedZones =
            {
                "DA_RUNTIME_VALID_ROAD_DEPOT",
                "DA_RUNTIME_VALID_ROAD_HIGHWAY_00",
                "DA_RUNTIME_VALID_ROAD_HIGHWAY_CURVE",
                "DA_RUNTIME_VALID_ROAD_FORK_AND_BRANCHES",
                "DA_RUNTIME_VALID_ROAD_MERGE_TO_TEMP_END"
            };

            foreach (string zoneName in expectedZones)
            {
                if (FindSceneObjectByName(blockout, zoneName) == null)
                {
                    errors.Add($"Playable blockout valid-road zone missing: {zoneName}");
                }
            }

            ValidateBlockoutTrigger<DeadAirTriggerZone>(blockout, "TEST_STORY_TRIGGER", DeadAirTriggerCategory.Story, errors);
            ValidateBlockoutTrigger<DeadAirChoiceStartTrigger>(blockout, "TEST_CHOICE_START", DeadAirTriggerCategory.ChoiceStart, errors);
            ValidateBlockoutTrigger<DeadAirChoiceCommitTrigger>(blockout, "TEST_LEFT_COMMIT", DeadAirTriggerCategory.ChoiceCommit, errors);
            ValidateBlockoutTrigger<DeadAirChoiceCommitTrigger>(blockout, "TEST_RIGHT_COMMIT", DeadAirTriggerCategory.ChoiceCommit, errors);
            ValidateBlockoutTrigger<DeadAirTriggerZone>(blockout, "TEMP_END_TRIGGER", DeadAirTriggerCategory.Ending, errors);

            if (FindSceneObjectByName(blockout, "VOID_FAILURE_TEST_AREA") == null)
            {
                errors.Add("Playable blockout is missing VOID FAILURE TEST AREA marker/surface.");
            }
        }

        private static void ValidateBlockoutTrigger<T>(Transform blockout, string name, DeadAirTriggerCategory category, List<string> errors) where T : DeadAirTriggerZone
        {
            Transform transform = FindSceneObjectByName(blockout, name);
            T trigger = transform != null ? transform.GetComponent<T>() : null;
            if (trigger == null)
            {
                errors.Add($"Playable blockout trigger missing or wrong type: {name}");
                return;
            }

            if (trigger.Category != category)
            {
                errors.Add($"Playable blockout trigger {name} has category {trigger.Category}, expected {category}.");
            }

            if ((category == DeadAirTriggerCategory.ChoiceStart || category == DeadAirTriggerCategory.ChoiceCommit) &&
                trigger.ChoiceId != "TEST_CHOICE")
            {
                errors.Add($"Playable blockout choice trigger {name} uses choice ID '{trigger.ChoiceId}', expected TEST_CHOICE.");
            }

            BoxCollider collider = trigger.GetComponent<BoxCollider>();
            if (collider == null || !collider.isTrigger)
            {
                errors.Add($"Playable blockout trigger {name} needs a trigger BoxCollider.");
            }
        }

        private static void ValidateWebGlSurface(List<string> warnings)
        {
            string[] deadAirScripts = AssetDatabase.FindAssets("t:Script", new[] { "Assets/DeadAir" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => !path.Replace('\\', '/').Contains("/Scripts/Editor/"))
                .ToArray();

            string[] blockedTokens = { "DirectInput", "System.IO.File", "DllImport", "Windows", "Thread" };
            foreach (string script in deadAirScripts)
            {
                string text = File.ReadAllText(script);
                foreach (string token in blockedTokens)
                {
                    if (text.Contains(token))
                    {
                        warnings.Add($"Review WebGL compatibility token '{token}' in {script}.");
                    }
                }
            }
        }
    }
}
