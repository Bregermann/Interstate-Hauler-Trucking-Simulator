using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.TruckSports.Editor
{
    public static partial class SportsEnvironmentAuthoring
    {
        public static void InstallFirstImpossibleOutdoors()
        {
            InstallImpossibleOutdoors(new[] { "TruckTruckGoose", "TruckPinataPanic", "TruckMechanicalBull", "TruckYoga" });
        }

        public static void InstallFirstEightEnvironments()
        {
            InstallImpossibleOutdoors(new[] { "TruckTruckGoose", "TruckPinataPanic", "TruckMechanicalBull", "TruckYoga",
                "TruckJenga", "TruckEggSpoonRace", "TruckTornadoRodeo" });
        }

        public static void InstallConstructionEnvironments()
        {
            InstallImpossibleOutdoors(new[] { "TruckNestBuilding", "TruckSnowmanBuilding" });
        }

        public static void InstallTeaGardenEnvironment()
        {
            InstallImpossibleOutdoors(new[] { "TruckTeaParty" });
        }

        public static void InstallWildernessEnvironments()
        {
            InstallImpossibleOutdoors(new[] { "TruckCamping", "TruckPetPals" });
        }

        public static void InstallArchaeologyEnvironment()
        {
            // The existing snow adapter deliberately retains unknown terrain
            // and mesh shaders. Digger owns both representations of excavation.
            // Never substitute an ordinary snow terrain shader for its cutter.
            var path = "Assets/TruckArchaeology/Scenes/TruckArchaeology.unity";
            if (!File.Exists(path)) throw new InvalidOperationException("Saved real Digger scene required.");
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var terrain = All<Terrain>(scene).Single();
            var material = terrain.materialTemplate;
            if (material == null || material.shader == null || material.shader.name != "Digger/Terrain/URP/Lit")
                throw new InvalidOperationException("Preserve the real Digger terrain material before environment binding.");
            string originalPath = AssetDatabase.GetAssetPath(material);
            InstallImpossibleOutdoors(new[] { "TruckArchaeology" });
            scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            terrain = All<Terrain>(scene).Single();
            if (terrain.materialTemplate != AssetDatabase.LoadAssetAtPath<Material>(originalPath))
                throw new InvalidOperationException("Environment binding must preserve the authored Digger cutter material.");
        }

        static void InstallImpossibleOutdoors(string[] names)
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling)
                throw new InvalidOperationException("Stopped compiled Editor required.");
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save pending scene edits before explicit environment integration.");
            // Guess That Truck is an indoor private-information board game.
            // Only these explicitly authored first-wave outdoor venues participate.
            var paths = names.Select(name => "Assets/" + name + "/Scenes/" + name + ".unity").ToArray();
            if (paths.Any(path => !File.Exists(path)))
                throw new InvalidOperationException("Create the explicit new outdoor scenes before their environment pass.");
            var catalog = AssetDatabase.LoadAssetAtPath<SportsWeatherPresets>(SportsWeatherPresetsAuthoring.CatalogPath);
            if (catalog == null) throw new InvalidOperationException("Existing installed-profile weather catalog required.");
            catalog.ValidateCatalog(); // Never regenerate designer-selected entries.
            foreach (var path in paths)
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var match = All<SportsEventSession>(scene).Single();
                var cameras = All<SportsEventCamera>(scene).Select(rig => rig.view).Where(view => view != null).Distinct().ToArray();
                if (cameras.Length == 0) throw new InvalidOperationException("Explicit existing camera roster required: " + path);
                var existing = All<SportsChaosWeather>(scene);
                if (existing.Length > 1 || existing.Length == 1 && match.presentationWeather != existing[0])
                    throw new InvalidOperationException("Existing weather authority retained for review: " + path);
                var weather = existing.SingleOrDefault();
                if (weather == null)
                {
                    if (All<DigitalRuby.WeatherMaker.WeatherMakerScript>(scene).Length != 0)
                        throw new InvalidOperationException("Unexpected vendor weather authority: " + path);
                    weather = SportsChaosAuthoring.Weather(match.gameObject, cameras, scene.name);
                    weather.presets = catalog; match.presentationWeather = weather;
                    var rendering = All<LWS.TruckBaseJumping.BaseJumpRendering>(scene).Single();
                    SportsWeatherRenderingAuthoring.Install(match.gameObject, cameras, scene.name, rendering);
                    SportsSnowAuthoring.InstallScene();
                }
                weather.vendor.AutoFindMainCamera = false;
                weather.vendor.AllowCameras.Clear(); weather.vendor.AllowCameras.AddRange(cameras);
                EditorUtility.SetDirty(match); EditorUtility.SetDirty(weather); EditorUtility.SetDirty(weather.vendor);
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            // Reuse the existing sun/moon, 3D viewport sky, Heat weather/time,
            // headlight and setup integration. Preserve its earlier evidence file.
            string previous = "Logs/TruckSports/Environment/collection-install.txt";
            if (File.Exists(previous))
            {
                string backup = "Logs/TruckSports/Impossible07/Environment/collection-install.before.txt";
                Directory.CreateDirectory(Path.GetDirectoryName(backup));
                if (!File.Exists(backup)) File.Copy(previous, backup);
            }
            InstallPaths(paths, true);
            Debug.Log("IMPOSSIBLE_OUTDOOR_ENVIRONMENTS installed existing Weather Maker/time/SRS adapters in " + paths.Length + " NEW scenes only; native/rendered verification pending.");
        }
    }
}
