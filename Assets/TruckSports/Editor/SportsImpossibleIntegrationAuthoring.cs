using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.TruckSports.Editor
{
    // Explicit first-wave integration. No registry entries are created for an
    // unfinished scene, and none of the original Sports scenes are rebuilt.
    public static class SportsImpossibleIntegrationAuthoring
    {
        static readonly string[,] FirstWave =
        {
            {"TruckTruckGoose", "LWS.TruckTruckGoose.Editor.GooseAuthoring", "CreateNewScene"},
            {"TruckGuessThat", "LWS.TruckGuessThat.Editor.GuessAuthoring", "CreateNewScene"},
            {"TruckPinataPanic", "LWS.TruckPinataPanic.Editor.PinataAuthoring", "Create"},
            {"TruckMechanicalBull", "LWS.TruckMechanicalBull.Editor.MechanicalBullAuthoring", "Create"},
            {"TruckYoga", "LWS.TruckYoga.Editor.YogaAuthoring", "Create"}
        };
        static readonly string[,] PhysicsWave =
        {
            {"TruckJenga", "LWS.TruckJenga.Editor.JengaAuthoring", "CreateNewScene"},
            {"TruckEggSpoonRace", "LWS.TruckEggSpoonRace.Editor.EggSpoonAuthoring", "Create"},
            {"TruckTornadoRodeo", "LWS.TruckTornadoRodeo.Editor.TornadoAuthoring", "Create"}
        };
        static readonly string[,] BuilderWave =
        {
            {"TruckNestBuilding", "LWS.TruckNestBuilding.Editor.NestAuthoring", "Create"},
            {"TruckSnowmanBuilding", "LWS.TruckSnowmanBuilding.Editor.SnowmanAuthoring", "CreateNewScene"}
        };

        static readonly string[,] ExpeditionWave =
        {
            {"TruckSubmarine", "LWS.TruckSubmarine.Editor.SubmarineAuthoring", "Create"},
            {"TruckMoonMadness", "LWS.TruckMoonMadness.Editor.MoonAuthoring", "CreateNewScene"},
            {"TruckTeaParty", "LWS.TruckTeaParty.Editor.TeaAuthoring", "Create"}
        };

        public static void CreateMoonExpedition()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling || File.Exists(PathFor("TruckMoonMadness")))
                throw new InvalidOperationException("Stopped compiled Editor and a new Moon scene required.");
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save pending edits before Moon authoring.");
            var type = Type.GetType("LWS.TruckMoonMadness.Editor.MoonAuthoring, Assembly-CSharp-Editor", true);
            type.GetMethod("CreateNewScene", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
            FinishOpenFirstWave("TruckMoonMadness");
            Debug.Log("IMPOSSIBLE_MOON_AUTHORED airless sky retained; actual gravity/flags/AI/rendered acceptance pending.");
        }

        public static void CreateDentistScene()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling || File.Exists(PathFor("TruckDentist")))
                throw new InvalidOperationException("Stopped compiled Editor and a new Dentist scene required.");
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save pending edits before Dentist authoring.");
            var type = Type.GetType("LWS.TruckDentist.Editor.DentistAuthoring, Assembly-CSharp-Editor", true);
            type.GetMethod("Create", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
            FinishOpenFirstWave("TruckDentist");
        }

        public static void CreateExhibitionAndWilderness()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling)
                throw new InvalidOperationException("Stopped compiled Editor required.");
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save pending edits before explicit exhibition/wilderness authoring.");
            string[,] wave =
            {
                { "TruckOrchestra", "OrchestraAuthoring", "Create" },
                { "TruckFashionShow", "FashionAuthoring", "CreateNewScene" },
                { "TruckBallet", "BalletAuthoring", "Create" },
                { "TruckCamping", "CampingAuthoring", "Create" },
                { "TruckPetPals", "PetPalsAuthoring", "Create" }
            };
            for (int i = 0; i < wave.GetLength(0); i++)
                if (File.Exists(PathFor(wave[i, 0]))) throw new InvalidOperationException("Existing exhibition/wilderness scene retained: " + wave[i, 0]);
            for (int i = 0; i < wave.GetLength(0); i++)
            {
                var type = Type.GetType("LWS." + wave[i, 0] + ".Editor." + wave[i, 1] + ", Assembly-CSharp-Editor", true);
                type.GetMethod(wave[i, 2], BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
                FinishOpenFirstWave(wave[i, 0]);
            }
            SportsEnvironmentAuthoring.InstallWildernessEnvironments();
            SportsImpossibleArtworkAuthoring.InstallAuthoredImpossibleHeaders();
            Debug.Log("IMPOSSIBLE_EXHIBITION_AND_WILDERNESS_FIVE_AUTHORED; actual native, menu, asset export and Windows acceptance remain pending.");
        }

        public static void CreateServingSports()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling)
                throw new InvalidOperationException("Stopped compiled Editor required.");
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save pending edits before new serving scenes.");
            foreach (string name in new[] { "TruckTeaParty", "TruckBakeOff" })
                if (File.Exists(PathFor(name))) throw new InvalidOperationException("Existing serving scene retained: " + name);
            foreach (string mode in new[] { "Tea", "Bake" })
            {
                string name = mode == "Tea" ? "TruckTeaParty" : "TruckBakeOff";
                var type = Type.GetType("LWS." + name + ".Editor." + mode + "Authoring, Assembly-CSharp-Editor", true);
                type.GetMethod("Create", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
                FinishOpenFirstWave(name);
            }
            SportsEnvironmentAuthoring.InstallTeaGardenEnvironment();
            Debug.Log("IMPOSSIBLE_SERVING_SPORTS_AUTHORED; original physical serving equipment, independent rules and existing shared presentation; native and Windows acceptance pending.");
        }

        public static void CreateExpeditionsAndTea()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling)
                throw new InvalidOperationException("Stopped compiled Editor required.");
            for (int index = 0; index < UnityEngine.SceneManagement.SceneManager.sceneCount; index++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(index).isDirty)
                    throw new InvalidOperationException("Save pending scene edits before explicit new-scene authoring.");
            for (int i = 0; i < ExpeditionWave.GetLength(0); i++)
                if (File.Exists(PathFor(ExpeditionWave[i, 0])))
                    throw new InvalidOperationException("Existing expedition or tea scene retained: " + ExpeditionWave[i, 0]);
            for (int i = 0; i < ExpeditionWave.GetLength(0); i++)
            {
                var type = Type.GetType(ExpeditionWave[i, 1] + ", Assembly-CSharp-Editor", true);
                type.GetMethod(ExpeditionWave[i, 2], BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
                FinishOpenFirstWave(ExpeditionWave[i, 0]);
            }
            // The airless Moon keeps its authored space sky; underwater optics
            // remain Submarine's existing camera-local Bog rendering adapter.
            SportsEnvironmentAuthoring.InstallTeaGardenEnvironment();
            Debug.Log("IMPOSSIBLE_EXPEDITIONS_AND_TEA_AUTHORED; native mechanics, underwater optics, art, menus and Windows acceptance remain pending.");
        }

        public static void RepairJengaAndCreateBuildersTwo()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling)
                throw new InvalidOperationException("Stopped compiled validation Editor required.");
            for (int index = 0; index < UnityEngine.SceneManagement.SceneManager.sceneCount; index++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(index).isDirty)
                    throw new InvalidOperationException("Save pending scene edits before explicit new-scene authoring.");
            for (int i = 0; i < BuilderWave.GetLength(0); i++)
                if (File.Exists(PathFor(BuilderWave[i, 0])))
                    throw new InvalidOperationException("Existing construction scene retained: " + BuilderWave[i, 0]);
            var jenga = EditorSceneManager.OpenScene(PathFor("TruckJenga"), OpenSceneMode.Single);
            var repairType = Type.GetType("LWS.TruckJenga.Editor.JengaResetPoseRepair, Assembly-CSharp-Editor", true);
            repairType.GetMethod("RepairKnownZeroResetPoseMetadata", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
            if (!EditorSceneManager.SaveScene(jenga)) throw new IOException("Could not save guarded Jenga reset metadata repair.");
            for (int i = 0; i < BuilderWave.GetLength(0); i++)
            {
                var type = Type.GetType(BuilderWave[i, 1] + ", Assembly-CSharp-Editor", true);
                type.GetMethod(BuilderWave[i, 2], BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
                FinishOpenFirstWave(BuilderWave[i, 0]);
                SportsImpossibleWorkshopAuthoring.BindOpenWorkshop();
                if (!EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene()))
                    throw new IOException("Could not save new workshop integration.");
            }
            SportsEnvironmentAuthoring.InstallConstructionEnvironments();
            var winter = EditorSceneManager.OpenScene(PathFor("TruckSnowmanBuilding"), OpenSceneMode.Single);
            var snow = winter.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<SportsSnowSurface>(true)).Single();
            snow.permanentCoverage = 1; EditorUtility.SetDirty(snow);
            if (!EditorSceneManager.SaveScene(winter)) throw new IOException("Could not save permanent workshop snow coverage.");
            Debug.Log("IMPOSSIBLE_CONSTRUCTION_TWO_AUTHORED_AND_BOUND; native save/reload, gameplay and rendered acceptance remain pending.");
        }

        public static void ApplyFirstEightIntegration()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling)
                throw new InvalidOperationException("Stopped compiled validation Editor required.");
            for (int index = 0; index < UnityEngine.SceneManagement.SceneManager.sceneCount; index++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(index).isDirty)
                    throw new InvalidOperationException("Save pending scene edits before explicit integration.");
            var author = Type.GetType("LWS.TruckPinataPanic.Editor.PinataAuthoring, Assembly-CSharp-Editor", true);
            var repair = author.GetMethod("RepairSavedSuspension", BindingFlags.Public | BindingFlags.Static);
            if (repair == null) throw new InvalidOperationException("Frozen, guarded Pinata suspension repair required.");
            repair.Invoke(null, null);
            SportsEnvironmentAuthoring.InstallFirstEightEnvironments();
            SportsImpossibleArtworkAuthoring.InstallFirstFiveHeaders();
            Debug.Log("IMPOSSIBLE_FIRST_EIGHT_INTEGRATED; native and actual Windows acceptance are still required.");
        }

        public static void CreatePhysicsThree()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling)
                throw new InvalidOperationException("Stopped compiled validation Editor required.");
            for (int index = 0; index < UnityEngine.SceneManagement.SceneManager.sceneCount; index++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(index).isDirty)
                    throw new InvalidOperationException("Save pending scene edits before explicit new-scene authoring.");
            for (int i = 0; i < PhysicsWave.GetLength(0); i++)
                if (File.Exists(PathFor(PhysicsWave[i, 0])))
                    throw new InvalidOperationException("Existing scene retained; explicit additive repair required: " + PhysicsWave[i, 0]);
            for (int i = 0; i < PhysicsWave.GetLength(0); i++)
            {
                var type = Type.GetType(PhysicsWave[i, 1] + ", Assembly-CSharp-Editor", true);
                var method = type.GetMethod(PhysicsWave[i, 2], BindingFlags.Public | BindingFlags.Static);
                if (method == null) throw new InvalidOperationException("Missing frozen author entry point: " + type.FullName);
                method.Invoke(null, null);
                FinishOpenFirstWave(PhysicsWave[i, 0]);
            }
            Debug.Log("IMPOSSIBLE_PHYSICS_THREE_AUTHORED; actual physics, lifecycle, environment, art and Windows acceptance remain separate.");
        }

        public static void CreateFirstFive()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling)
                throw new InvalidOperationException("Stopped compiled validation Editor required.");
            for (int index = 0; index < UnityEngine.SceneManagement.SceneManager.sceneCount; index++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(index).isDirty)
                    throw new InvalidOperationException("Save pending scene edits before explicit new-scene authoring.");
            // Check the whole intended batch before any scene mutation.
            for (int i = 0; i < FirstWave.GetLength(0); i++)
                if (File.Exists(PathFor(FirstWave[i, 0])))
                    throw new InvalidOperationException("Existing scene retained; explicit additive repair required: " + FirstWave[i, 0]);
            for (int i = 0; i < FirstWave.GetLength(0); i++)
            {
                var type = Type.GetType(FirstWave[i, 1] + ", Assembly-CSharp-Editor", true);
                var method = type.GetMethod(FirstWave[i, 2], BindingFlags.Public | BindingFlags.Static);
                if (method == null) throw new InvalidOperationException("Missing frozen author entry point: " + type.FullName);
                method.Invoke(null, null);
                FinishOpenFirstWave(FirstWave[i, 0]);
            }
            Debug.Log("IMPOSSIBLE_FIRST_FIVE_AUTHORED; native play, art, menu and Windows acceptance remain separate.");
        }

        public static void FinishOpenFirstWave(string expectedName)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            bool allowed = Enumerable.Range(0, FirstWave.GetLength(0)).Any(i => FirstWave[i, 0] == expectedName) ||
                Enumerable.Range(0, PhysicsWave.GetLength(0)).Any(i => PhysicsWave[i, 0] == expectedName) ||
                Enumerable.Range(0, BuilderWave.GetLength(0)).Any(i => BuilderWave[i, 0] == expectedName) ||
                Enumerable.Range(0, ExpeditionWave.GetLength(0)).Any(i => ExpeditionWave[i, 0] == expectedName) ||
                new[] { "TruckBakeOff", "TruckDentist", "TruckOrchestra", "TruckCamping", "TruckPetPals", "TruckArchaeology", "TruckBallet", "TruckFashionShow" }.Contains(expectedName);
            if (!allowed ||
                scene.path != PathFor(expectedName))
                throw new InvalidOperationException("Explicit saved first-wave scene required.");
            var roots = scene.GetRootGameObjects();
            var match = roots.SelectMany(root => root.GetComponentsInChildren<SportsEventSession>(true)).Single();
            var trucks = roots.SelectMany(root => root.GetComponentsInChildren<SportsEventTruck>(true))
                .Where(truck => truck.session == match).OrderBy(truck => truck.slot).ToArray();
            if (trucks.Length > 0)
                SportsTruckAppearanceHostAuthoring.BindNewImpossibleScene(match, trucks);
            // Private Guess boards are deliberately not reframed as a tractor arena.
            if (expectedName != "TruckGuessThat")
            {
                var field = match.GetType().GetField("broadcast", BindingFlags.Public | BindingFlags.Instance);
                var camera = field != null ? field.GetValue(match) as SportsEventCamera : null;
                if (camera == null) throw new InvalidOperationException("Explicit original shared camera required for " + expectedName);
                var framing = camera.GetComponent<SportsBroadcastFraming>();
                if (framing == null)
                {
                    framing = camera.gameObject.AddComponent<SportsBroadcastFraming>();
                    framing.session = match; framing.cameraRig = camera; framing.trucks = trucks;
                    framing.sharedDefaults = AssetDatabase.LoadAssetAtPath<SportsBroadcastProfile>("Assets/TruckSports/Settings/Keepaway broadcast framing.asset");
                    if (framing.sharedDefaults == null) throw new InvalidOperationException("Existing broadcast profile required.");
                    framing.useSharedDefaults = true; framing.useOrthographic = camera.view.orthographic;
                    camera.broadcastFraming = framing; EditorUtility.SetDirty(framing); EditorUtility.SetDirty(camera);
                }
                match.appearanceFraming = roots.SelectMany(root => root.GetComponentsInChildren<SportsBroadcastFraming>(true))
                    .Where(f => f.session == match).ToArray();
            }
            EditorUtility.SetDirty(match); EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save new scene " + scene.path);
        }

        public static void BindSavedArchaeologyVendorProvider()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling)
                throw new InvalidOperationException("Stopped compiled Editor required.");
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Preserve dirty scenes before explicit Digger provider binding.");
            var scene = EditorSceneManager.OpenScene(PathFor("TruckArchaeology"), OpenSceneMode.Single);
            LWS.TruckArchaeology.Editor.ArchaeologyDiggerProviderAuthoring.InstallOnOpenOriginalScene();
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save owned Digger provider binding.");
        }

        public static void RepairSavedOrchestraPresentation()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling)
                throw new InvalidOperationException("Stopped compiled Editor required.");
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Preserve dirty scenes before explicit concert presentation repair.");
            var scene = EditorSceneManager.OpenScene(PathFor("TruckOrchestra"), OpenSceneMode.Single);
            LWS.TruckOrchestra.Editor.OrchestraPresentationAuthoring.RepairOpenOriginalScene();
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save owned concert presentation.");
        }

        public static void RaiseSavedOrchestraPodiumClearance()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling)
                throw new InvalidOperationException("Stopped compiled Editor required.");
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Preserve dirty scenes before explicit podium clearance repair.");
            var scene = EditorSceneManager.OpenScene(PathFor("TruckOrchestra"), OpenSceneMode.Single);
            LWS.TruckOrchestra.Editor.OrchestraPodiumClearanceAuthoring.InstallOnOpenOriginalScene();
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save owned podium clearance repair.");
        }

        public static void PositionSavedOrchestraVenueTitle()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling)
                throw new InvalidOperationException("Stopped compiled Editor required.");
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Preserve dirty scenes before explicit venue title repair.");
            var scene = EditorSceneManager.OpenScene(PathFor("TruckOrchestra"), OpenSceneMode.Single);
            LWS.TruckOrchestra.Editor.OrchestraVenueTitleAuthoring.InstallOnOpenOriginalScene();
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save owned venue title position.");
        }

        public static void CreateRecoveredWildernessScenes()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling)
                throw new InvalidOperationException("Stopped compiled Editor required.");
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Preserve dirty scenes before explicit new wilderness scenes.");
            foreach (string name in new[] { "TruckCamping", "TruckPetPals" })
                if (File.Exists(PathFor(name))) throw new InvalidOperationException("Existing wilderness scene retained: " + name);
            foreach (var name in new[] { "TruckCamping", "TruckPetPals" })
            {
                string author = name == "TruckCamping" ? "CampingAuthoring" : "PetPalsAuthoring";
                var type = Type.GetType("LWS." + name + ".Editor." + author + ", Assembly-CSharp-Editor", true);
                type.GetMethod("Create", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
                FinishOpenFirstWave(name);
            }
            SportsEnvironmentAuthoring.InstallWildernessEnvironments();
            SportsImpossibleArtworkAuthoring.InstallAuthoredImpossibleHeaders();
        }

        public static void CreateArchaeologyScene()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling || File.Exists(PathFor("TruckArchaeology")))
                throw new InvalidOperationException("Stopped compiled Editor and a new Archaeology scene required.");
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Preserve dirty scenes before new Archaeology authoring.");
            var type = Type.GetType("LWS.TruckArchaeology.Editor.ArchaeologyAuthoring, Assembly-CSharp-Editor", true);
            type.GetMethod("CreateNewScene", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
            FinishOpenFirstWave("TruckArchaeology");
            SportsImpossibleWorkshopAuthoring.BindOpenWorkshop();
            if (!EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene()))
                throw new IOException("Could not save Archaeology workshop binding.");
            SportsEnvironmentAuthoring.InstallArchaeologyEnvironment();
            SportsImpossibleArtworkAuthoring.InstallAuthoredImpossibleHeaders();
        }

        public static void InstallSavedSnowmanCollectionRack()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling)
                throw new InvalidOperationException("Stopped compiled Editor required.");
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Preserve dirty scenes before explicit Snowman collection repair.");
            var scene = EditorSceneManager.OpenScene(PathFor("TruckSnowmanBuilding"), OpenSceneMode.Single);
            LWS.TruckSnowmanBuilding.Editor.SnowmanCollectionRackAuthoring.InstallOnOpenOriginalScene();
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save owned Snowman collection rack.");
        }

        public static void RepairSavedMoonLabels()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling)
                throw new InvalidOperationException("Stopped compiled Editor required.");
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Preserve dirty scenes before explicit Moon label repair.");
            var scene = EditorSceneManager.OpenScene(PathFor("TruckMoonMadness"), OpenSceneMode.Single);
            LWS.TruckMoonMadness.Editor.MoonPresentationAuthoring.RepairOpenOwnedHabitatLabels();
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save owned Moon labels.");
        }

        public static void InstallSavedArchaeologySpreaderGeometry()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling)
                throw new InvalidOperationException("Stopped compiled Editor required.");
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Preserve dirty scenes before explicit Archaeology geometry repair.");
            var scene = EditorSceneManager.OpenScene(PathFor("TruckArchaeology"), OpenSceneMode.Single);
            LWS.TruckArchaeology.Editor.ArchaeologySpreaderColliderAuthoring.InstallOnOpenOriginalScene();
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save owned Archaeology pickup geometry.");
        }

        static string PathFor(string mode) => "Assets/" + mode + "/Scenes/" + mode + ".unity";
    }
}
