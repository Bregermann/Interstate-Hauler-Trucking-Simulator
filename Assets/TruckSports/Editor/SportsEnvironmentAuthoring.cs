using System;
using System.IO;
using System.Linq;
using DigitalRuby.WeatherMaker;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckSports.Editor
{
    public static partial class SportsEnvironmentAuthoring
    {
        static readonly string[] Representative = { "TruckKeepaway", "TruckFieldGoal", "TruckSkyTow", "TruckTerrierRacing", "TruckSnake", "TruckPoker" };
        // Explicit repair of the diagnosed coplanar staging surface. Keep every
        // collider and the authored EasyRoads meshes; never rebuild the course.
        public static void RepairAvalancheStagingSurface()
        {
            var scene = EditorSceneManager.OpenScene("Assets/TruckAvalancheEscape/Scenes/TruckAvalancheEscape.unity", OpenSceneMode.Single);
            var platform = All<MeshRenderer>(scene).Single(item => item.name == "Broad flat safe mountain staging platform");
            if (Vector3.Distance(platform.bounds.center, new Vector3(0, 429, -65)) > .01f ||
                Vector3.Distance(platform.bounds.size, new Vector3(66, 2, 90)) > .01f || platform.GetComponent<Collider>() == null)
                throw new InvalidOperationException("Staging platform was customized; inspect before applying the original surface repair.");
            if (!platform.enabled) return;
            platform.enabled = false;
            EditorUtility.SetDirty(platform);
            EditorSceneManager.SaveScene(scene);
        }
        public static void AuditRemainingVenueSupports()
        {
            Directory.CreateDirectory("Logs/TruckSports/Environment");
            using (var output = new StreamWriter("Logs/TruckSports/Environment/remaining-venue-supports.txt"))
            foreach (string name in new[] { "TruckAvalancheEscape", "TruckBogSnorkelling", "TruckFloorIsLava", "TruckMattressRacing", "TruckMinefield", "TruckSkyTow", "TruckTargetLaunch", "TruckTerrierRacing", "TruckTugOfWar", "TruckWifeCarrying", "TruckCornhole", "TruckCheeseRolling", "TruckBaseJumping" })
            {
                var scene = EditorSceneManager.OpenScene(SportsBuilder.SceneList().Single(path => Path.GetFileNameWithoutExtension(path) == name), OpenSceneMode.Single);
                output.WriteLine("SCENE " + name);
                foreach (var terrain in All<Terrain>(scene))
                    output.WriteLine("TERRAIN " + terrain.name + " origin=" + terrain.transform.position + " size=" + terrain.terrainData.size);
                foreach (var item in All<MeshRenderer>(scene).Where(item => new[] { "floor", "ground", "platform", "rail", "deck", "wall", "pole", "tower", "road", "ramp", "board", "helicopter", "gantry", "crane", "bridge", "shore", "bank" }.Any(word => item.name.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0)).Take(100))
                    output.WriteLine("SUPPORT " + item.name + " bounds=" + item.bounds + " parent=" + item.transform.parent?.name);
                foreach (var component in All<MonoBehaviour>(scene).Where(component => component != null && component.GetType().Namespace != null && component.GetType().Namespace.StartsWith("LWS.") && new[] { "Course", "World", "Arena", "Slope", "Mountain", "Range" }.Any(word => component.GetType().Name.Contains(word))))
                    output.WriteLine("LAYOUT " + component.GetType().FullName + " " + JsonUtility.ToJson(component));
                foreach (var clock in All<SportsMatchTime>(scene)) output.WriteLine("CLOCK " + clock.name + " lamps=" + clock.nightLights.Length);
            }
        }
        static T[] All<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
        [MenuItem("Truck Sports/Environment/Install Mountain and Meadow Course Lights")]
        public static void InstallMountainCourseVenueLighting()
        {
            // Explicit additive authoring, never invoked by loading, rematching or building.
            foreach (string name in new[] { "TruckAvalancheEscape", "TruckBogSnorkelling", "TruckMattressRacing", "TruckMinefield", "TruckWifeCarrying", "TruckCheeseRolling" })
            {
                var scene = EditorSceneManager.OpenScene(SportsBuilder.SceneList().Single(path => Path.GetFileNameWithoutExtension(path) == name), OpenSceneMode.Single);
                var clock = All<SportsMatchTime>(scene).Single();
                if (All<Transform>(scene).Any(item => item.name == "Authored mountain course night fixtures")) continue;
                if (clock.nightLights.Length != 0) throw new InvalidOperationException("Inspect independently customized course lamps before migrating " + name);
                var lamps = new System.Collections.Generic.List<Light>();
                var mounts = new System.Collections.Generic.List<Vector3>();
                var root = Root(clock.transform);
                if (name == "TruckAvalancheEscape")
                {
                    var world = All<LWS.TruckAvalancheEscape.AvalancheWorld>(scene).Single();
                    foreach (var route in world.routes)
                    for (int segment = 1; segment < route.points.Length; segment++)
                    {
                        var a = route.points[segment - 1]; var b = route.points[segment];
                        int steps = Mathf.CeilToInt(Vector3.Distance(a, b) / 75);
                        for (int step = 0; step < steps; step++)
                        {
                            var center = Vector3.Lerp(a, b, step / (float)steps);
                            var right = Vector3.Cross(Vector3.up, (b - a).normalized).normalized;
                            foreach (int side in new[] { -1, 1 })
                            {
                                var mount = Ground(world.terrain, center + right * side * 48);
                                // Shared route sections get one pair, not a duplicated set for each AI route.
                                if (mounts.Any(previous => Vector3.ProjectOnPlane(previous - mount, Vector3.up).sqrMagnitude < 30 * 30)) continue;
                                mounts.Add(mount); Pole(root, mount, center, 27, 180, 1870, new Color(.86f, .94f, 1));
                            }
                        }
                    }
                }
                else if (name == "TruckBogSnorkelling")
                {
                    var course = All<LWS.TruckBogSnorkelling.BogCourse>(scene).Single();
                    foreach (bool hard in new[] { false, true })
                    {
                        var terrain = hard ? course.hardTerrain : course.normalTerrain;
                        if (terrain == null) throw new InvalidOperationException("Retain both authored Bog courses.");
                        var courseRoot = Root(terrain.transform); // Original course activation owns these lamps too.
                        for (float z = -5; z <= (hard ? 650 : 350); z += 60)
                        foreach (int side in new[] { -1, 1 })
                            Pole(courseRoot, Ground(terrain, new Vector3(side * 40, 0, z)), new Vector3(0, course.water.position.y, z), 22, 125, 1040, new Color(1, .94f, .82f));
                    }
                }
                else if (name == "TruckMattressRacing")
                {
                    var course = All<LWS.TruckMattressRacing.MattressCourse>(scene).Single();
                    for (float z = 0; z <= 660; z += 55) foreach (int side in new[] { -1, 1 })
                    {
                        var center = Ground(course.terrain, new Vector3(course.Center(z), 0, z));
                        // The randomized height variation is bounded. Bury the footing slightly
                        // so a different seed cannot leave a detached pole floating above the meadow.
                        // At82m the existing bank formula is capped for every possible
                        // randomized center. Only the bounded surface noise can change.
                        var mount = Ground(course.terrain, new Vector3(side * 82, 0, z)) - Vector3.up * 2.7f;
                        Pole(root, mount, center, 29, 190, 2000, new Color(1, .94f, .82f));
                    }
                }
                else if (name == "TruckMinefield")
                {
                    var match = All<LWS.TruckMinefield.MinefieldMatch>(scene).Single();
                    var west = match.grounds.Single(terrain => terrain.name == "West meadow");
                    var east = match.grounds.Single(terrain => terrain.name == "East meadow");
                    var runout = match.grounds.Single(terrain => terrain.name == "Mile landing runout");
                    float end = runout.transform.position.z + runout.terrainData.size.z - 25;
                    for (float z = 0; z <= end; z += 90) foreach (int side in new[] { -1, 1 })
                    {
                        var center = Ground(z < runout.transform.position.z ? match.terrain : runout, new Vector3(0, 0, z));
                        Pole(root, Ground(side < 0 ? west : east, new Vector3(side * 74, 0, z)), center, 31, 190, 3000, new Color(1, .94f, .83f));
                    }
                }
                else if (name == "TruckWifeCarrying")
                {
                    var course = All<LWS.TruckWifeCarrying.WifeCourse>(scene).Single();
                    for (float progress = 0; progress < LWS.TruckWifeCarrying.WifeCourse.Finish; progress += 31)
                    foreach (int side in new[] { -1, 1 })
                    {
                        var center = course.loopCircuit ? course.Point(progress) : Ground(course.terrain, new Vector3(course.Center(progress), 0, progress));
                        var mount = course.loopCircuit ? course.Point(progress, side * 34) : Ground(course.terrain, center + Vector3.right * side * 40);
                        Pole(root, mount, center, 24, 125, 936, new Color(1, .93f, .8f));
                    }
                }
                else
                {
                    for (float z = -5; z <= 310; z += 45) foreach (int side in new[] { -1, 1 })
                        Pole(root, LWS.TruckCheeseRolling.CheeseRules.Surface(side * 64, z), LWS.TruckCheeseRolling.CheeseRules.Surface(0, z), 27, 190, 1200, new Color(1, .94f, .82f));
                }
                clock.nightLights = lamps.ToArray(); clock.morningLampMultiplier = .65f;
                clock.lampSnowSurface = All<SportsSnowSurface>(scene).Single(); clock.coveredLampMultiplier = .3f;
                EditorUtility.SetDirty(clock); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                CompleteCourseLightBudget(scene);
                Transform Root(Transform parent)
                {
                    var holder = new GameObject("Authored mountain course night fixtures").transform; holder.SetParent(parent, false); return holder;
                }
                void Pole(Transform parent, Vector3 position, Vector3 target, float height, float range, float power, Color color)
                {
                    var pole = new GameObject("Supported course fixture " + lamps.Count).transform; pole.SetParent(parent, false); pole.position = position;
                    pole.rotation = Quaternion.Euler(0, target.x < position.x ? 180 : 0, 0);
                    LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(pole, "Assets/Toon Gas Station/Prefabs/Streetlight_1C.prefab", new Vector3(100, height, 100), 0, false, "TruckSports");
                    var renderers = pole.GetComponentsInChildren<Renderer>(); Bounds bounds = renderers[0].bounds; foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
                    var lamp = new GameObject("Budgeted mountain course night light").AddComponent<Light>(); lamp.transform.SetParent(pole, false); lamp.transform.position = new Vector3(bounds.center.x, bounds.max.y - .4f, bounds.center.z);
                    lamp.transform.rotation = Quaternion.LookRotation(target - lamp.transform.position); lamp.type = LightType.Spot; lamp.range = range; lamp.spotAngle = 112; lamp.innerSpotAngle = 76; lamp.intensity = power; lamp.color = color; lamp.shadows = LightShadows.None; lamps.Add(lamp);
                }
            }
            AssetDatabase.SaveAssets();
        }
        static Vector3 Ground(Terrain terrain, Vector3 point)
        {
            var origin = terrain.transform.position; var size = terrain.terrainData.size;
            if (point.x < origin.x || point.x > origin.x + size.x || point.z < origin.z || point.z > origin.z + size.z) throw new InvalidOperationException("A venue fixture must have actual terrain support: " + terrain.name + " " + point);
            point.y = terrain.SampleHeight(point) + origin.y; return point;
        }
        public static void CompleteMountainCourseLightBudgets()
        {
            foreach (string name in new[] { "TruckAvalancheEscape", "TruckBogSnorkelling", "TruckMattressRacing", "TruckMinefield", "TruckWifeCarrying", "TruckCheeseRolling" })
                CompleteCourseLightBudget(EditorSceneManager.OpenScene(SportsBuilder.SceneList().Single(path => Path.GetFileNameWithoutExtension(path) == name), OpenSceneMode.Single));
            AssetDatabase.SaveAssets();
        }
        public static void RetuneReviewedMountainNightExposure()
        {
            string[] names = { "TruckAvalancheEscape", "TruckBogSnorkelling", "TruckMattressRacing", "TruckMinefield", "TruckWifeCarrying", "TruckCheeseRolling" };
            float[] before = { 8500, 5200, 12500, 12500, 5200, 10000 }, after = { 1870, 1040, 2000, 3000, 936, 1200 };
            int[] counts = { 140, 34, 26, 186, 20, 16 };
            for (int index = 0; index < names.Length; index++)
            {
                var scene = EditorSceneManager.OpenScene(SportsBuilder.SceneList().Single(path => Path.GetFileNameWithoutExtension(path) == names[index]), OpenSceneMode.Single);
                var lamps = All<SportsMatchTime>(scene).Single().nightLights;
                if (lamps.Length != counts[index] || lamps.Any(light => light == null || light.name != "Budgeted mountain course night light" || !Mathf.Approximately(light.intensity, before[index])))
                    throw new InvalidOperationException("Preserve independently edited course lights; this correction requires the reviewed initial layout.");
                foreach (var lamp in lamps) { lamp.intensity = after[index]; EditorUtility.SetDirty(lamp); }
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
        }
        static void CompleteCourseLightBudget(Scene scene)
        {
            string name = scene.name;
            // Follow the actual saved lifetime owner. Some older modes retain their
            // Weather pipeline rather than the otherwise valid unused snow copy.
            var owner = All<LWS.TruckBaseJumping.BaseJumpRendering>(scene).SingleOrDefault();
            var pipeline = owner != null ? owner.pipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset : All<LWS.TruckCheeseRolling.CheeseLook>(scene).Single().pipeline;
            if (pipeline == null) throw new InvalidOperationException("Retain the existing mode-local weather/snow pipeline.");
            var settings = new SerializedObject(pipeline); var first = settings.FindProperty("m_RendererDataList").GetArrayElementAtIndex(0);
            bool legacy = name == "TruckMinefield" || name == "TruckWifeCarrying";
            string source = AssetDatabase.GetAssetPath(first.objectReferenceValue), target = "Assets/TruckSports/Rendering/" + name + (legacy ? " active course clustered renderer.asset" : " local clustered renderer.asset");
            if (source != target)
            {
                if (AssetDatabase.LoadMainAssetAtPath(target) != null || !AssetDatabase.CopyAsset(source, target)) throw new InvalidOperationException("Inspect existing local renderer before replacing it.");
                var renderer = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRendererData>(target);
                renderer.renderingMode = UnityEngine.Rendering.Universal.RenderingMode.ForwardPlus; EditorUtility.SetDirty(renderer);
                first.objectReferenceValue = renderer; settings.ApplyModifiedPropertiesWithoutUndo();
            }
            pipeline.maxAdditionalLightsCount = 8; EditorUtility.SetDirty(pipeline);
        }
        [Serializable] sealed class VenueAudit
        {
            public string scene, session, skyMaterial, skyShader, skyProfile, nightTexture;
            public int weatherAdapters, legacyWeatherAdapters, clocks, perspectiveViews, orthographicViews, fixedDirectionalLights, pointOrSpotLights, weatherPickers, timePickers;
            public float starIntensity;
            public bool hasMatchTime;
        }
        [Serializable] sealed class EnvironmentAudit
        {
            public VenueAudit[] venues;
            public string[] weatherProfiles;
        }
        public static void AuditCollection()
        {
            var venues = new System.Collections.Generic.List<VenueAudit>();
            foreach (string path in SportsBuilder.SceneList())
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var weather = All<SportsChaosWeather>(scene);
                var sky = All<WeatherMakerSkySphereScript>(scene).FirstOrDefault();
                var views = All<Camera>(scene).Where(camera => camera.cameraType == CameraType.Game).ToArray();
                var fixedLights = All<Light>(scene).Where(light => light.GetComponentInParent<WeatherMakerScript>() == null).ToArray();
                var pickers = All<SportsWeatherPicker>(scene);
                venues.Add(new VenueAudit
                {
                    scene = path, session = All<SportsEventSession>(scene).FirstOrDefault()?.GetType().FullName,
                    skyMaterial = AssetDatabase.GetAssetPath(RenderSettings.skybox), skyShader = RenderSettings.skybox != null ? RenderSettings.skybox.shader.name : null,
                    skyProfile = sky != null ? AssetDatabase.GetAssetPath(sky.SkySphereProfile) : null,
                    nightTexture = sky != null && sky.SkySphereProfile != null ? AssetDatabase.GetAssetPath(sky.SkySphereProfile.NightTexture) : null,
                    starIntensity = sky != null && sky.SkySphereProfile != null ? sky.SkySphereProfile.NightIntensity : 0,
                    weatherAdapters = weather.Length,
                    legacyWeatherAdapters = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true)).Count(component => component != null && component.GetType().Name.EndsWith("Weather") && !(component is SportsChaosWeather)),
                    clocks = All<WeatherMakerDayNightCycleManagerScript>(scene).Length,
                    perspectiveViews = views.Count(camera => !camera.orthographic), orthographicViews = views.Count(camera => camera.orthographic),
                    fixedDirectionalLights = fixedLights.Count(light => light.type == LightType.Directional), pointOrSpotLights = fixedLights.Count(light => light.type == LightType.Point || light.type == LightType.Spot),
                    weatherPickers = pickers.Count(picker => !picker.timeOfDay), timePickers = pickers.Count(picker => picker.timeOfDay), hasMatchTime = All<SportsMatchTime>(scene).Length != 0
                });
            }
            var registry = AssetDatabase.LoadAssetAtPath<SportsWeatherPresets>(SportsWeatherPresetsAuthoring.CatalogPath);
            Directory.CreateDirectory("Logs/TruckSports/Environment");
            File.WriteAllText("Logs/TruckSports/Environment/collection-audit.json", JsonUtility.ToJson(new EnvironmentAudit { venues = venues.ToArray(), weatherProfiles = registry.entries.Select(entry => entry.displayName + " | " + AssetDatabase.GetAssetPath(entry.profile) + " | sky=" + AssetDatabase.GetAssetPath(entry.profile.SkyProfile)).ToArray() }, true));
        }
        public static void AuditLegacy()
        {
            Directory.CreateDirectory("Logs/TruckSports/Environment");
            using (var output = new StreamWriter("Logs/TruckSports/Environment/legacy-layout-audit.txt"))
            foreach (string name in new[] { "TruckCornhole", "TruckCheeseRolling", "TruckBaseJumping" })
            {
                var scene = EditorSceneManager.OpenScene(SportsBuilder.SceneList().Single(path => Path.GetFileNameWithoutExtension(path) == name), OpenSceneMode.Single);
                var menus = All<SportsMenuView>(scene).Single(); var parent = menus.pages[0].transform;
                output.WriteLine("SCENE " + name);
                foreach (var rect in parent.GetComponentsInChildren<RectTransform>(true).Where(rect => rect.parent == parent))
                    output.WriteLine(rect.name + " active=" + rect.gameObject.activeSelf + " pos=" + rect.anchoredPosition + " size=" + rect.sizeDelta);
                foreach (var light in All<Light>(scene).Where(light => light.GetComponentInParent<WeatherMakerScript>() == null))
                    output.WriteLine("LIGHT " + light.name + " type=" + light.type + " intensity=" + light.intensity + " at=" + light.transform.position);
            }
        }
        static readonly string[] NewOutdoors = { "TruckDockDiving", "TruckBuzkashi", "TruckBungeeDemolition", "TruckFishing", "TruckSumo", "TruckLogRolling", "TruckTrampolineDodgeball", "TruckDemolitionDerby" };
        static readonly string[] RemainingSharedOutdoors = { "TruckCurling", "TruckSkeeBall" };
        // Explicit additive authoring after the venue audit. Never called by a
        // build, scene load or Play Mode; existing designers' choices survive.
        public static void InstallNewOutdoors() => InstallWeatherlessOutdoors(NewOutdoors);
        public static void InstallRemainingSharedOutdoors() => InstallWeatherlessOutdoors(RemainingSharedOutdoors);
        [MenuItem("Truck Sports/Environment/Complete Musical Parking Weather and Time")]
        public static void InstallMusicalParkingOutdoorEnvironment()
        {
            // Explicit additive migration only; never called by play/build/load.
            InstallWeatherlessOutdoors(new[] { "TruckMusicalParking" });
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.name != "TruckMusicalParking") throw new InvalidOperationException("Open the registered saved Parking scene.");
            var session = All<LWS.TruckMusicalParking.ParkingMatch>(scene).Single();
            var weather = session.presentationWeather;
            var binding = weather.GetComponent<SportsMatchTime>();
            if (binding.nightLights.Length == 0)
            {
                // Reuse the six real authored lamp pools before adding/tuning
                // any further light. Native night captures decide visibility.
                var lamps = All<Light>(scene).Where(light => light.name == "Parking lamp warm pool").ToArray();
                if (lamps.Length != 6) throw new InvalidOperationException("Preserve the six authored Parking lamp fixtures.");
                binding.nightLights = lamps;
                EditorUtility.SetDirty(binding);
            }
            var lot = All<MeshRenderer>(scene).Single(renderer => renderer.name == "Full 3D asphalt lot");
            var marker = lot.GetComponent<LWS.TruckTaxi.TruckTaxiSurface>();
            if (marker == null) marker = lot.gameObject.AddComponent<LWS.TruckTaxi.TruckTaxiSurface>();
            marker.isRoad = false; EditorUtility.SetDirty(marker);
            // Existing Weatherade/Taxi accumulation and actual NWH contact
            // tracing; no independent snow implementation or physics changes.
            SportsSnowAuthoring.InstallScene();
            var snow = session.GetComponent<SportsSnowSurface>();
            if (snow == null || snow.adapter == null) throw new InvalidOperationException("Bind the existing Sports snow adapter.");
            var settings = new SerializedObject(snow.adapter);
            settings.FindProperty("areaSizeMeters").floatValue = 104;
            settings.FindProperty("areaDepthMeters").floatValue = 90;
            settings.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(snow.adapter);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        public static void InstallRemainingOutdoorsAndGroundFixtures()
        {
            InstallRemainingSharedOutdoors();
            GroundAuthoredVenueFixtures();
            RefineRemainingOutdoorLamps();
            AssetDatabase.SaveAssets();
        }
        public static void RefineRemainingOutdoorLamps()
        {
            foreach (string name in new[] { "TruckSkeeBall", "TruckFishing" })
            {
                var scene = EditorSceneManager.OpenScene(SportsBuilder.SceneList().Single(path => Path.GetFileNameWithoutExtension(path) == name), OpenSceneMode.Single);
                var binding = All<SportsMatchTime>(scene).Single();
                if (name == "TruckSkeeBall")
                {
                    var poles = binding.transform.Find("Authored night venue lamps").Cast<Transform>().ToArray();
                    if (poles.Length != 16) throw new InvalidOperationException("Retain four authored fixtures per Skee lane.");
                    int index = 0;
                    foreach (var course in All<LWS.TruckSkeeBall.SkeeRunner>(scene).OrderBy(course => course.lane))
                    foreach (bool board in new[] { false, true }) foreach (int side in new[] { -1, 1 })
                    {
                        var pole = poles[index++];
                        var floor = course.target.parent.Find("Course miss floor").GetComponent<MeshRenderer>().bounds;
                        pole.position = new Vector3(floor.center.x + side * (floor.extents.x - 1.5f), floor.max.y, board ? course.target.position.z - 16 : 8);
                        var light = pole.GetComponentInChildren<Light>();
                        light.transform.localPosition = Vector3.up * 29;
                        var target = board ? course.target.TransformPoint(new Vector3(0, 17.5f, 0)) : new Vector3(course.target.position.x, 0, 24);
                        light.transform.rotation = Quaternion.LookRotation(target - light.transform.position);
                        light.range = board ? 80 : 100; light.intensity = board ? 800 : 1200;
                        EditorUtility.SetDirty(pole); EditorUtility.SetDirty(light); EditorUtility.SetDirty(light.transform);
                    }
                }
                else foreach (var light in binding.nightLights) { light.intensity = 1200; EditorUtility.SetDirty(light); }
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
        }
        static void InstallWeatherlessOutdoors(string[] names)
        {
            var paths = SportsBuilder.SceneList().Where(path => names.Contains(Path.GetFileNameWithoutExtension(path))).ToArray();
            if (paths.Length != names.Length) throw new InvalidOperationException("Missing audited outdoor scene.");
            foreach (string path in paths)
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var session = All<SportsEventSession>(scene).Single();
                var existing = All<SportsChaosWeather>(scene);
                if (existing.Length != 0)
                {
                    if (existing.Length != 1 || session.presentationWeather != existing[0]) throw new InvalidOperationException("Do not replace an existing weather authority: " + path);
                    continue;
                }
                if (All<WeatherMakerScript>(scene).Length != 0) throw new InvalidOperationException("Unexpected vendor weather owner: " + path);
                string folder = Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(path)));
                var weather = SportsChaosAuthoring.Weather(session, folder);
                weather.presets = SportsWeatherPresetsAuthoring.EnsureCatalog();
                var rendering = All<LWS.TruckBaseJumping.BaseJumpRendering>(scene).Single();
                // Clone the venue's existing renderer, retaining water and other
                // effects. Never add a second pipeline lifetime owner.
                SportsWeatherRenderingAuthoring.Install(session.gameObject, session.cameras, folder, rendering);
                session.presentationWeather = weather;
                EditorUtility.SetDirty(session); EditorUtility.SetDirty(weather);
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            InstallPaths(paths, true);
        }
        public static void AuditOutdoorCandidates()
        {
            Directory.CreateDirectory("Logs/TruckSports/Environment");
            using (var output = new StreamWriter("Logs/TruckSports/Environment/outdoor-candidates.txt"))
            foreach (string name in new[] { "TruckDockDiving", "TruckCurling", "TruckBuzkashi", "TruckBungeeDemolition", "TruckFishing", "TruckSumo", "TruckSkeeBall", "TruckLogRolling", "TruckTrampolineDodgeball", "TruckDemolitionDerby", "TruckHockey", "TruckWallball" })
            {
                var scene = EditorSceneManager.OpenScene(SportsBuilder.SceneList().Single(path => Path.GetFileNameWithoutExtension(path) == name), OpenSceneMode.Single);
                output.WriteLine("SCENE " + name);
                output.WriteLine("SESSION " + All<SportsEventSession>(scene).FirstOrDefault()?.GetType().FullName);
                foreach (var root in scene.GetRootGameObjects()) output.WriteLine("ROOT " + root.name);
                foreach (var item in All<MeshRenderer>(scene).Where(item => new[] { "roof", "ceiling", "stadium", "arena", "mountain", "water", "grass", "ground", "wall", "pool", "forest" }.Any(word => item.name.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0)).Take(65))
                    output.WriteLine("VENUE " + item.name + " bounds=" + item.bounds + " material=" + string.Join(",", item.sharedMaterials.Where(material => material != null).Select(material => material.name)));
                foreach (var light in All<Light>(scene)) output.WriteLine("LIGHT " + light.name + " kind=" + light.type + " intensity=" + light.intensity);
                foreach (var rendering in All<LWS.TruckBaseJumping.BaseJumpRendering>(scene)) output.WriteLine("PIPELINE " + AssetDatabase.GetAssetPath(rendering.pipeline));
            }
        }
        public static void InstallLegacyCourtWeather()
        {
            foreach (string name in new[] { "TruckHockey", "TruckWallball" })
            {
                var scene = EditorSceneManager.OpenScene(SportsBuilder.SceneList().Single(path => Path.GetFileNameWithoutExtension(path) == name), OpenSceneMode.Single);
                var hockey = All<LWS.TruckHockey.HockeyMatch>(scene).SingleOrDefault();
                var wall = All<LWS.TruckWallball.WallballMatch>(scene).SingleOrDefault();
                MonoBehaviour match = hockey != null ? (MonoBehaviour)hockey : wall;
                if (match == null) throw new InvalidOperationException("Missing existing court match authority.");
                var menus = hockey != null ? (SportsMenuView)hockey.menus : wall.menus;
                var cameras = All<Camera>(scene).Where(camera => camera.cameraType == CameraType.Game).ToArray();
                var existing = All<SportsChaosWeather>(scene);
                if (existing.Length != 0)
                {
                    if (existing.Length != 1 || existing[0].alternateSession != match) throw new InvalidOperationException("Do not replace an existing court weather authority.");
                    continue; // Do not overwrite subsequent designer setup/lighting edits.
                }
                if (All<WeatherMakerScript>(scene).Length != 0) throw new InvalidOperationException("Unexpected existing vendor weather owner.");
                var previousPipeline = UnityEngine.QualitySettings.renderPipeline != null ? UnityEngine.QualitySettings.renderPipeline : UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline;
                var weather = SportsChaosAuthoring.Weather(match.gameObject, cameras, name);
                weather.alternateSession = match; weather.presets = SportsWeatherPresetsAuthoring.EnsureCatalog();
                var rendering = All<LWS.TruckBaseJumping.BaseJumpRendering>(scene).Single();
                if (previousPipeline != null) rendering.pipeline = previousPipeline;
                SportsWeatherRenderingAuthoring.Install(match.gameObject, cameras, name, rendering);
                var binding = match.gameObject.AddComponent<SportsMatchTime>();
                binding.cycle = weather.vendor.GetComponentInChildren<WeatherMakerDayNightCycleManagerScript>(true); binding.clouds = weather.clouds;
                binding.daylightFill = All<Light>(scene).Single(light => light.type == LightType.Directional && light.GetComponentInParent<WeatherMakerScript>() == null);
                BindThreeDimensionalSky(weather, binding); BindSkyProfile(weather, binding);
                SportsWeatherRenderingAuthoring.UseViewportSkybox(weather.gameObject, cameras, name);
                InstallAuditedOutdoorLamps(scene, binding);
                var headlights = match.gameObject.AddComponent<SportsNightHeadlights>(); headlights.time = binding; headlights.alternateSession = match;
                var root = new GameObject("Night vehicle light presentation").transform; root.SetParent(binding.transform, false);
                var trucks = hockey != null ? hockey.trucks.Select(truck => truck.transform).ToArray() : wall.trucks.Select(truck => truck.transform).ToArray();
                headlights.beams = trucks.Select(truck =>
                {
                    var shape = truck.GetComponent<BoxCollider>();
                    if (shape == null) throw new InvalidOperationException("Retain the existing court truck collider.");
                    var light = new GameObject("Authored court night low-beam").AddComponent<Light>(); light.transform.SetParent(root, false);
                    light.type = LightType.Spot; light.range = 28; light.spotAngle = 80; light.innerSpotAngle = 60;
                    light.intensity = 90; light.color = new Color(1, .98f, .93f); light.shadows = LightShadows.None; light.enabled = false;
                    return new SportsNightHeadlights.Beam { target = truck, light = light, position = shape.center + new Vector3(0, shape.size.y * .2f, shape.size.z * .49f), rotation = Quaternion.identity };
                }).ToArray();
                weather.matchTime = binding;
                var page = menus.pages[0].transform; var picker = SportsWeatherPickerAuthoring.Install(page, weather.presets, new Vector2(0, 337)); InstallTimePicker(picker);
                foreach (var selector in page.GetComponentsInChildren<SportsWeatherPicker>(true))
                {
                    var rect = selector.GetComponent<RectTransform>(); rect.anchoredPosition = new Vector2(selector.timeOfDay ? 165 : -165, 337); rect.sizeDelta = new Vector2(300, 54);
                    selector.dropdown.panelSize = 220; selector.dropdown.enableScrollbar = true;
                    var scroll = selector.GetComponentInChildren<UnityEngine.UI.ScrollRect>(true);
                    if (scroll != null) scroll.verticalScrollbarVisibility = UnityEngine.UI.ScrollRect.ScrollbarVisibility.AutoHide;
                    EditorUtility.SetDirty(selector.dropdown); EditorUtility.SetDirty(rect);
                }
                var title = page.Find("Title").GetComponent<TMPro.TMP_Text>(); title.rectTransform.anchoredPosition = new Vector2(0, 430); title.rectTransform.sizeDelta = new Vector2(1350, 58); title.fontSizeMax = 44;
                page.Find("Subtitle").gameObject.SetActive(false); page.Find("Weather selection explanation").gameObject.SetActive(false);
                binding.setupLayoutVersion = 6;
                RefineCourtFixtureLayout(scene, binding);
                EditorUtility.SetDirty(title); EditorUtility.SetDirty(title.rectTransform); EditorUtility.SetDirty(binding); EditorUtility.SetDirty(weather); EditorUtility.SetDirty(headlights);
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
        }
        public static void InstallLegacyForecastCatalog()
        {
            foreach (string name in new[] { "TruckCheeseRolling", "TruckBaseJumping" })
            {
                var scene = EditorSceneManager.OpenScene(SportsBuilder.SceneList().Single(path => Path.GetFileNameWithoutExtension(path) == name), OpenSceneMode.Single);
                var cheese = All<LWS.TruckCheeseRolling.CheeseWeather>(scene).SingleOrDefault();
                var jumper = All<LWS.TruckBaseJumping.BaseJumpWeather>(scene).SingleOrDefault();
                var owner = cheese != null ? cheese.gameObject : jumper.gameObject;
                var vendor = cheese != null ? cheese.vendor : jumper.vendor;
                // The original Cheese adapter intentionally disabled full-screen
                // clouds when it only controlled precipitation. Installed profiles
                // transition through the vendor cloud manager and require it.
                var profileClouds = vendor.GetComponentInChildren<WeatherMakerFullScreenCloudsScript>(true);
                if (profileClouds != null && !profileClouds.enabled) { profileClouds.enabled = true; EditorUtility.SetDirty(profileClouds); }
                var existing = owner.GetComponent<SportsChaosWeather>();
                if (existing != null)
                {
                    if (existing.vendor != vendor || (cheese != null ? cheese.presentation : jumper.presentation) != existing) throw new InvalidOperationException("Do not replace an existing legacy visual authority.");
                    var retainedSnow = All<SportsSnowSurface>(scene).SingleOrDefault();
                    if (retainedSnow != null && retainedSnow.weather != existing) { retainedSnow.weather = existing; EditorUtility.SetDirty(retainedSnow); }
                    EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                    continue; // Preserve later designer changes to rosters, UI and physics.
                }
                var presentation = owner.AddComponent<SportsChaosWeather>();
                presentation.vendor = vendor; presentation.presets = SportsWeatherPresetsAuthoring.EnsureCatalog();
                presentation.session = jumper != null ? jumper.match : null; presentation.alternateSession = cheese != null ? cheese.match : null;
                presentation.precipitation = cheese != null ? cheese.precipitation : jumper.precipitation;
                presentation.clouds = vendor.GetComponentInChildren<WeatherMakerFullScreenCloudsScript>(true);
                presentation.wind = vendor.GetComponentInChildren<WeatherMakerWindScript>(true);
                presentation.sun = cheese != null ? cheese.sun : jumper.sun;
                presentation.ambience = cheese != null ? cheese.ambience : jumper.ambience;
                presentation.matchTime = cheese != null ? cheese.matchTime : jumper.matchTime;
                var snow = All<SportsSnowSurface>(scene).SingleOrDefault();
                if (snow != null) { snow.weather = presentation; EditorUtility.SetDirty(snow); }
                SportsMenuView menus;
                if (cheese != null) { cheese.presentation = presentation; menus = cheese.match.menus; EditorUtility.SetDirty(cheese); }
                else { jumper.presentation = presentation; menus = jumper.match.menus; InstallHeadlights(scene, presentation.matchTime); EditorUtility.SetDirty(jumper); }
                var page = menus.pages[0].transform;
                var oldExplanation = page.Find("Weather selection explanation"); if (oldExplanation != null) { oldExplanation.name = "Retained legacy time explanation"; EditorUtility.SetDirty(oldExplanation.gameObject); }
                var forecast = SportsWeatherPickerAuthoring.Install(page, presentation.presets, new Vector2(-165, 337));
                foreach (var picker in page.GetComponentsInChildren<SportsWeatherPicker>(true))
                {
                    var rect = picker.GetComponent<RectTransform>(); rect.anchoredPosition = new Vector2(picker.timeOfDay ? 165 : -165, 337); rect.sizeDelta = new Vector2(300, 54);
                    picker.dropdown.panelSize = 220; picker.dropdown.enableScrollbar = true;
                    var scroll = picker.GetComponentInChildren<UnityEngine.UI.ScrollRect>(true); if (scroll != null) scroll.verticalScrollbarVisibility = UnityEngine.UI.ScrollRect.ScrollbarVisibility.AutoHide;
                    EditorUtility.SetDirty(picker.dropdown); EditorUtility.SetDirty(rect);
                }
                foreach (string label in new[] { "Weather selection label", "Time of day selection label" })
                {
                    var rect = page.Find(label).GetComponent<RectTransform>(); rect.anchoredPosition = new Vector2(label.StartsWith("Time", StringComparison.Ordinal) ? 165 : -165, 381); rect.sizeDelta = new Vector2(300, 30); EditorUtility.SetDirty(rect);
                }
                page.Find("Weather selection explanation").gameObject.SetActive(false);
                presentation.matchTime.setupLayoutVersion = 101;
                EditorUtility.SetDirty(presentation); EditorUtility.SetDirty(presentation.matchTime);
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
        }
        public static void RefineLegacyCourtLights()
        {
            foreach (string name in new[] { "TruckHockey", "TruckWallball" })
            {
                var scene = EditorSceneManager.OpenScene(SportsBuilder.SceneList().Single(path => Path.GetFileNameWithoutExtension(path) == name), OpenSceneMode.Single);
                var binding = All<SportsMatchTime>(scene).Single();
                RefineCourtFixtureLayout(scene, binding);
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
        }
        static void RefineCourtFixtureLayout(Scene scene, SportsMatchTime binding)
        {
                string name = scene.name;
                var floor = All<MeshRenderer>(scene).Single(renderer => renderer.name == (name == "TruckHockey" ? "Arena floor" : "Court floor")).bounds;
                var poles = binding.transform.Find("Authored night venue lamps").Cast<Transform>().ToArray();
                if (poles.Length != 4) throw new InvalidOperationException("Retain four owned court fixtures.");
                foreach (var pole in poles)
                {
                    pole.position = new Vector3(floor.center.x + Mathf.Sign(pole.position.x - floor.center.x) * (floor.extents.x - .25f), floor.max.y, floor.center.z + Mathf.Sign(pole.position.z - floor.center.z) * (floor.extents.z - .25f));
                    var model = pole.Cast<Transform>().Single(child => child.name.StartsWith("Installed ", StringComparison.Ordinal));
                    // One explicit correction; a marker makes this safe to rerun.
                    if (!model.name.EndsWith(" · court scale", StringComparison.Ordinal)) { model.localScale *= .35f; model.localPosition *= .35f; model.name += " · court scale"; }
                    float top = model.GetComponentsInChildren<Renderer>(true).Max(renderer => renderer.bounds.max.y);
                    var light = pole.GetComponentInChildren<Light>(); light.transform.position = new Vector3(pole.position.x, top - .15f, pole.position.z);
                    light.transform.rotation = Quaternion.LookRotation(new Vector3(floor.center.x, floor.max.y, floor.center.z) - light.transform.position);
                    light.intensity = 50; light.range = 45;
                    EditorUtility.SetDirty(pole); EditorUtility.SetDirty(model); EditorUtility.SetDirty(model.gameObject); EditorUtility.SetDirty(light); EditorUtility.SetDirty(light.transform);
                }
                foreach (var beam in binding.GetComponent<SportsNightHeadlights>().beams) { beam.light.intensity = 8; beam.light.range = 14; EditorUtility.SetDirty(beam.light); }
        }
        public static void RefineNewOutdoorLamps()
        {
            foreach (string name in new[] { "TruckDockDiving", "TruckDemolitionDerby" })
            {
                var scene = EditorSceneManager.OpenScene(SportsBuilder.SceneList().Single(path => Path.GetFileNameWithoutExtension(path) == name), OpenSceneMode.Single);
                var binding = All<SportsMatchTime>(scene).Single();
                foreach (var light in binding.nightLights)
                {
                    if (name == "TruckDockDiving") light.intensity = 1800;
                    else { light.intensity = 4500; light.range = 110; light.spotAngle = 115; light.innerSpotAngle = 90; }
                    EditorUtility.SetDirty(light);
                }
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
        }
        public static void RefineBuzSetup()
        {
            var scene = EditorSceneManager.OpenScene("Assets/TruckBuzkashi/Scenes/TruckBuzkashi.unity", OpenSceneMode.Single);
            var session = All<SportsEventSession>(scene).Single();
            var button = session.menus.pages[0].GetComponentsInChildren<SportsMenuAction>(true).Single(action => action.action == "teamsetup").GetComponent<RectTransform>();
            button.anchoredPosition = new Vector2(0, -225); EditorUtility.SetDirty(button);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }
        public static void GroundAuthoredVenueFixtures()
        {
            foreach (string name in new[] { "TruckBungeeDemolition", "TruckFishing" })
            {
                var scene = EditorSceneManager.OpenScene(SportsBuilder.SceneList().Single(path => Path.GetFileNameWithoutExtension(path) == name), OpenSceneMode.Single);
                var binding = All<SportsMatchTime>(scene).Single();
                string anchor = name == "TruckBungeeDemolition" ? "Independent demolition bay" : "Equivalent independent river";
                float ground = scene.GetRootGameObjects().Where(item => item.name.StartsWith(anchor, StringComparison.Ordinal)).Select(item => item.transform.position.y).Distinct().Single();
                var fixtures = binding.transform.Find("Authored night venue lamps");
                foreach (Transform pole in fixtures)
                {
                    if (!pole.name.StartsWith("Venue night fixture ", StringComparison.Ordinal)) throw new InvalidOperationException("Unexpected owned fixture layout.");
                    var position = pole.position; position.y = ground; pole.position = position; EditorUtility.SetDirty(pole);
                }
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
        }
        public static void InstallLegacyTime()
        {
            Directory.CreateDirectory("Logs/TruckSports/Environment");
            using (var output = new StreamWriter("Logs/TruckSports/Environment/legacy-time-install.txt"))
            foreach (string name in new[] { "TruckCornhole", "TruckCheeseRolling", "TruckBaseJumping" })
            {
                var scene = EditorSceneManager.OpenScene(SportsBuilder.SceneList().Single(path => Path.GetFileNameWithoutExtension(path) == name), OpenSceneMode.Single);
                var vendor = All<WeatherMakerScript>(scene).Single();
                var menus = All<SportsMenuView>(scene).Single(); var parent = menus.pages[0].transform;
                var corn = All<LWS.TruckCornhole.CornholeMatch>(scene).SingleOrDefault();
                var cheese = All<LWS.TruckCheeseRolling.CheeseWeather>(scene).SingleOrDefault();
                var jumper = All<LWS.TruckBaseJumping.BaseJumpWeather>(scene).SingleOrDefault();
                var owner = corn != null ? corn.atmosphere.gameObject : cheese != null ? cheese.gameObject : jumper.gameObject;
                var binding = owner.GetComponent<SportsMatchTime>() ?? owner.AddComponent<SportsMatchTime>();
                binding.cycle = vendor.GetComponentInChildren<WeatherMakerDayNightCycleManagerScript>(true);
                binding.clouds = vendor.GetComponentInChildren<WeatherMakerFullScreenCloudsScript>(true);
                binding.daylightFill = All<Light>(scene).Single(light => light.type == LightType.Directional && light.GetComponentInParent<WeatherMakerScript>() == null);
                BindThreeDimensionalSky(vendor, binding); BindSkyProfile(vendor, owner, binding);
                BindElevatedHorizon(vendor);
                binding.skySphere = vendor.GetComponentInChildren<WeatherMakerSkySphereScript>(true); binding.coordinateGroundColor = true;
                var cameras = corn != null ? corn.courses.Select(course => course.chase.view).ToArray() : cheese != null ? cheese.match.cameras.Select(rig => rig.view).ToArray() : jumper.match.views.Select(rig => rig.view).ToArray();
                SportsWeatherRenderingAuthoring.UseViewportSkybox(binding.clouds, cameras, name);
                if (cheese != null)
                {
                    // Cheese's intentional mushroom look owns a runtime sky clone.
                    // Give that existing owner the vendor-backed template as well.
                    var look = All<LWS.TruckCheeseRolling.CheeseLook>(scene).Single();
                    if (look.skyTemplate.shader.name == "Skybox/Procedural") { look.skyTemplate = RenderSettings.skybox; EditorUtility.SetDirty(look); }
                }
                if (cheese != null || jumper != null)
                {
                    // Their original renderer contained only the mushroom pass
                    // (Cheese), or no feature (Base). Install the same vendor
                    // weather pass used by the other outdoor sports.
                    UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset pipeline = cheese != null ? All<LWS.TruckCheeseRolling.CheeseLook>(scene).Single().pipeline : (UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset)All<LWS.TruckBaseJumping.BaseJumpRendering>(scene).Single().pipeline;
                    var settings = new SerializedObject(pipeline);
                    int index = settings.FindProperty("m_DefaultRendererIndex").intValue;
                    var data = (UnityEngine.Rendering.Universal.UniversalRendererData)settings.FindProperty("m_RendererDataList").GetArrayElementAtIndex(index).objectReferenceValue;
                    if (!AssetDatabase.GetAssetPath(data).StartsWith("Assets/" + name + "/", StringComparison.Ordinal)) throw new InvalidOperationException("Preserve shared/vendor renderer assets; require the sport-owned renderer.");
                    SportsWeatherRenderingAuthoring.EnableVendorWeather(data);
                }
                if (corn != null) { corn.atmosphere.matchTime = binding; EditorUtility.SetDirty(corn.atmosphere); }
                if (cheese != null) { cheese.matchTime = binding; EditorUtility.SetDirty(cheese); }
                if (jumper != null) { jumper.matchTime = binding; EditorUtility.SetDirty(jumper); }
                if (binding.setupLayoutVersion < 100)
                {
                    var title = parent.Find("Title").GetComponent<TMPro.TMP_Text>();
                    title.rectTransform.anchoredPosition = new Vector2(0, 430); title.rectTransform.sizeDelta = new Vector2(1350, 58); title.fontSizeMax = 44;
                    parent.Find("Subtitle").gameObject.SetActive(false);
                    SportsWeatherPicker clock;
                    if (corn != null)
                    {
                        var forecast = parent.GetComponentsInChildren<SportsWeatherPicker>(true).Single(picker => !picker.timeOfDay);
                        forecast.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 300);
                        InstallTimePicker(forecast); clock = parent.GetComponentsInChildren<SportsWeatherPicker>(true).Single(picker => picker.timeOfDay);
                        foreach (string label in new[] { "Weather selection label", "Time of day selection label" }) parent.Find(label).gameObject.SetActive(true);
                    }
                    else
                    {
                        // These two sports retain their existing forecast/physics owner.
                        // Reuse the same installed Heat widget for the independent clock.
                        clock = SportsWeatherPickerAuthoring.Install(parent, SportsWeatherPresetsAuthoring.EnsureCatalog(), new Vector2(0, 337));
                        clock.timeOfDay = true; clock.name = "Time of day · Weather Maker selector";
                        var label = parent.Find("Weather selection label"); label.name = "Time of day selection label";
                        label.GetComponent<TMPro.TMP_Text>().text = "TIME OF DAY";
                        EditorUtility.SetDirty(label.GetComponent<TMPro.TMP_Text>());
                    }
                    foreach (var picker in parent.GetComponentsInChildren<SportsWeatherPicker>(true))
                    {
                        picker.dropdown.panelSize = 220; picker.dropdown.enableScrollbar = true;
                        var scroll = picker.GetComponentInChildren<UnityEngine.UI.ScrollRect>(true);
                        if (scroll != null) scroll.verticalScrollbarVisibility = UnityEngine.UI.ScrollRect.ScrollbarVisibility.AutoHide;
                        EditorUtility.SetDirty(picker); EditorUtility.SetDirty(picker.dropdown);
                    }
                    var explanation = parent.Find("Weather selection explanation"); if (explanation != null) explanation.gameObject.SetActive(false);
                    binding.setupLayoutVersion = 100;
                }
                EditorUtility.SetDirty(binding); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                output.WriteLine(name + ": existing match identity owns frozen time; vendor sky and shared Heat clock saved; physics, forecast choices and vehicle hierarchy retained.");
            }
            AssetDatabase.SaveAssets();
        }
        static void BindElevatedHorizon(WeatherMakerScript vendor)
        {
            const string path = "Assets/TruckSports/Settings/Sports elevated horizon sky.asset";
            var profile = AssetDatabase.LoadAssetAtPath<WeatherMakerSkyProfileScript>(path);
            if (profile == null)
            {
                profile = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<WeatherMakerSkyProfileScript>("Assets/TruckSports/Settings/Sports atmospheric sky.asset"));
                profile.name = "Sports elevated horizon sky";
                // Vendor ground alpha was zero: stars appeared below the horizon
                // around the three elevated courses. Use its own ground control.
                profile.SkyGroundGradient = new Gradient();
                profile.SkyGroundGradient.SetKeys(new[] {
                    new GradientColorKey(new Color(.008f, .014f, .025f), 0),
                    new GradientColorKey(new Color(.03f, .045f, .065f), .5f),
                    new GradientColorKey(new Color(.07f, .095f, .13f), .6f),
                    new GradientColorKey(new Color(.11f, .14f, .17f), 1)
                }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });
                AssetDatabase.CreateAsset(profile, path);
            }
            var sphere = vendor.GetComponentInChildren<WeatherMakerSkySphereScript>(true);
            if (AssetDatabase.GetAssetPath(sphere.SkySphereProfile) == "Assets/TruckSports/Settings/Sports atmospheric sky.asset") { sphere.SkySphereProfile = profile; EditorUtility.SetDirty(sphere); }
        }
        // Focused opt-in bindings over existing lights/recorders only. Never
        // called by builds, Play Mode, or any destructive scene generator.
        public static void AuditSwingVenueLighting()
        {
            var scene = EditorSceneManager.OpenScene("Assets/TruckGiantSwing/Scenes/TruckGiantSwing.unity", OpenSceneMode.Single);
            Directory.CreateDirectory("Logs/TruckSports/PlaytestRepair");
            using (var output = new StreamWriter("Logs/TruckSports/PlaytestRepair/swing-venue-lighting-audit.txt"))
            {
                var match = All<LWS.TruckGiantSwing.SwingMatch>(scene).Single();
                var clock = All<SportsMatchTime>(scene).Single();
                output.WriteLine("CLOCK lamps=" + clock.nightLights.Length + " position=" + clock.transform.position);
                output.WriteLine("PIVOT " + match.pivot.position + " truck=" + match.truck.transform.position);
                foreach (var root in scene.GetRootGameObjects()) output.WriteLine("ROOT " + root.name + " at=" + root.transform.position);
                foreach (var ground in All<LWS.TruckGiantSwing.SwingGround>(scene)) output.WriteLine("GROUND " + ground.name + " bounds=" + ground.GetComponent<Collider>().bounds);
                foreach (var renderer in All<MeshRenderer>(scene).Where(item => item.name.Contains("target") || item.name.Contains("Gantry") || item.name.Contains("foot"))) output.WriteLine("MESH " + renderer.name + " bounds=" + renderer.bounds);
                foreach (var light in All<Light>(scene).Where(item => item.GetComponentInParent<WeatherMakerScript>() == null)) output.WriteLine("LIGHT " + light.name + " at=" + light.transform.position + " type=" + light.type + " power=" + light.intensity + " range=" + light.range);
            }
        }
        [MenuItem("Truck Sports/Environment/Install Giant Swing Night Fixtures")]
        public static void InstallSwingVenueLighting()
        {
            // Explicit additive scene authoring only. Preserve later designer edits;
            // never run this migration during a build, scene load or Play Mode.
            var scene = EditorSceneManager.OpenScene("Assets/TruckGiantSwing/Scenes/TruckGiantSwing.unity", OpenSceneMode.Single);
            var clock = All<SportsMatchTime>(scene).Single();
            var existing = clock.transform.Find("Authored Swing night venue lamps");
            if (existing != null) return;
            if (clock.nightLights.Length != 0) throw new InvalidOperationException("Retain the designer's existing Swing venue lights; do not replace their bindings.");
            var ground = All<LWS.TruckGiantSwing.SwingGround>(scene).Single().GetComponent<Collider>();
            var root = new GameObject("Authored Swing night venue lamps").transform; root.SetParent(clock.transform, false);
            var lights = new System.Collections.Generic.List<Light>();
            foreach (float z in new[] { -16f, 50f, 112f }) foreach (int side in new[] { -1, 1 })
            {
                float height = 27;
                var pole = new GameObject("Swing venue fixture " + lights.Count).transform; pole.SetParent(root, false);
                pole.position = new Vector3(side * 40, ground.bounds.max.y, z);
                pole.rotation = Quaternion.Euler(0, side < 0 ? 0 : 180, 0);
                // Fit the purchased model by its actual height, without imposing
                // an unrelated width cap that would leave the beam floating above it.
                LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(pole, "Assets/Toon Gas Station/Prefabs/Streetlight_1C.prefab", new Vector3(100, height, 100), 0, false, scene.name);
                var renderers = pole.GetComponentsInChildren<Renderer>();
                Bounds bounds = renderers[0].bounds; foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
                var head = new GameObject("Budgeted Swing venue night light").AddComponent<Light>(); head.transform.SetParent(pole, false);
                head.transform.position = new Vector3(bounds.center.x, bounds.max.y - .4f, bounds.center.z);
                var target = new Vector3(0, z < 0 ? 10 : ground.bounds.max.y, z < 0 ? 0 : z - 8);
                head.transform.rotation = Quaternion.LookRotation(target - head.transform.position);
                head.type = LightType.Spot; head.range = 110; head.spotAngle = 108; head.innerSpotAngle = 76;
                head.color = new Color(1, .94f, .82f); head.intensity = z < 0 ? 3000 : 2600;
                head.shadows = LightShadows.None; lights.Add(head);
            }
            clock.nightLights = lights.ToArray(); EditorUtility.SetDirty(clock);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        }
        public static void AuditStadiumCabVenueLighting()
        {
            Directory.CreateDirectory("Logs/TruckSports/PlaytestRepair");
            using (var output = new StreamWriter("Logs/TruckSports/PlaytestRepair/stadium-cab-lighting-layout-audit.txt"))
            foreach (string name in new[] { "TruckKickball", "TruckKeepawayFirstPerson", "TruckKeepaway" })
            {
                var scene = EditorSceneManager.OpenScene(SportsBuilder.SceneList().Single(path => Path.GetFileNameWithoutExtension(path) == name), OpenSceneMode.Single);
                var clock = All<SportsMatchTime>(scene).Single(); output.WriteLine("SCENE " + name + " clock=" + clock.transform.position + " lamps=" + clock.nightLights.Length);
                foreach (var renderer in All<MeshRenderer>(scene).Where(item => item.name.Contains("Grandstand") || item.name == "Connected stadium ground turf" || item.name == "Rounded arena surface")) output.WriteLine("MESH " + renderer.name + " bounds=" + renderer.bounds);
                foreach (var light in clock.nightLights) output.WriteLine("AUTHORED LAMP " + light.name + " at=" + light.transform.position + " type=" + light.type + " power=" + light.intensity + " range=" + light.range);
                foreach (var root in scene.GetRootGameObjects()) if (root.name.Contains("stadium") || root.name.Contains("arena")) output.WriteLine("ROOT " + root.name + " at=" + root.transform.position);
            }
        }
        [MenuItem("Truck Sports/Environment/Complete Existing Show and Quarry Lighting")]
        public static void CompleteExistingShowVenueLighting()
        {
            foreach (string name in new[] { "TruckNinjaWarrior", "TruckTrailerPlug" })
            {
                var scene = EditorSceneManager.OpenScene("Assets/" + name + "/Scenes/" + name + ".unity", OpenSceneMode.Single);
                var clock = All<SportsMatchTime>(scene).Single();
                const string rootName = "Supported original show venue lighting";
                if (clock.transform.Find(rootName) != null) continue; // Explicit migration never replaces later designer edits.
                if (clock.nightLights.Length != 0) throw new InvalidOperationException("Retain independently authored show-light bindings.");
                bool ninja = name == "TruckNinjaWarrior";
                var lamps = All<Light>(scene).Where(item => item.name == (ninja ? "Actual broad show floodlight" : "Quarry event floodlight")).ToArray();
                if (lamps.Length != (ninja ? 24 : 6) || lamps.Any(item => item.type != LightType.Spot || Mathf.Abs(item.intensity - (ninja ? 70 : 8)) > .001f))
                    throw new InvalidOperationException("Inspect customized show fixtures before applying this one-time migration.");
                var root = new GameObject(rootName).transform; root.SetParent(clock.transform, false);
                foreach (var lamp in lamps)
                {
                    // Original colored show lights stay active in every time selection.
                    // Aim at the real elevated decks, not the empty water below them.
                    if (ninja) lamp.transform.LookAt(new Vector3(0, 12, lamp.transform.position.z));
                    lamp.range = ninja ? 145 : 120; lamp.spotAngle = 112; lamp.innerSpotAngle = 76;
                    lamp.intensity = ninja ? 9500 : 2200; lamp.shadows = LightShadows.None;
                    EditorUtility.SetDirty(lamp); EditorUtility.SetDirty(lamp.transform);
                }
                if (!ninja)
                {
                    var rig = All<LWS.TruckTrailerPlug.TrailerPlugRig>(scene).Single();
                    var crane = All<MeshRenderer>(scene).Single(item => item.name == "Existing modeled bungee support crane");
                    var vertices = crane.GetComponent<MeshFilter>().sharedMesh.vertices.Select(point => crane.transform.TransformPoint(point)).ToArray();
                    foreach (int side in new[] { 1 })
                    {
                        var desired = new Vector3(crane.bounds.center.x + side * 3, crane.bounds.max.y, crane.bounds.center.z);
                        var mount = vertices.OrderBy(point => (point - desired).sqrMagnitude).First();
                        var fixture = new GameObject("Crane-mounted upper swing flood fixture " + side).transform; fixture.SetParent(root, false); fixture.position = mount;
                        fixture.rotation = Quaternion.LookRotation(rig.TrailerCenter - mount);
                        InstallMountedLampModel(fixture);
                        var light = new GameObject("Budgeted upper swing show floodlight").AddComponent<Light>(); light.transform.SetParent(fixture, false);
                        light.type = LightType.Spot; light.range = 170; light.spotAngle = 108; light.innerSpotAngle = 76; light.intensity = 700; light.color = new Color(.86f, .94f, 1); light.shadows = LightShadows.None;
                    }
                    var upperFill = lamps.Where(item => item.transform.position.x > 0).OrderBy(item => item.transform.position.z).First();
                    upperFill.transform.LookAt(rig.truckStart + new Vector3(0, 2, -12)); upperFill.range = 170; upperFill.intensity = 18000;
                    EditorUtility.SetDirty(upperFill); EditorUtility.SetDirty(upperFill.transform);
                }
                string pipelinePath = "Assets/TruckSports/Rendering/" + name + " snow pipeline.asset";
                var pipeline = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>(pipelinePath);
                var settings = new SerializedObject(pipeline); var first = settings.FindProperty("m_RendererDataList").GetArrayElementAtIndex(0);
                string source = AssetDatabase.GetAssetPath(first.objectReferenceValue);
                string expected = ninja ? "Assets/TruckBaseJumping/Rendering/Base renderer.asset" : "Assets/TruckTrailerPlug/Rendering/Weather renderer.asset";
                string target = "Assets/TruckSports/Rendering/" + name + " local clustered renderer.asset";
                if (source != expected) throw new InvalidOperationException("Retain independently customized renderer: " + source);
                if (AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRendererData>(target) != null || !AssetDatabase.CopyAsset(source, target)) throw new InvalidOperationException("Preserve existing local renderer assets.");
                var renderer = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRendererData>(target); renderer.renderingMode = UnityEngine.Rendering.Universal.RenderingMode.ForwardPlus;
                first.objectReferenceValue = renderer; settings.ApplyModifiedPropertiesWithoutUndo(); pipeline.maxAdditionalLightsCount = 8;
                EditorUtility.SetDirty(renderer); EditorUtility.SetDirty(pipeline); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
        }
        [MenuItem("Truck Sports/Environment/Correct Reviewed Quarry Upper Fixture")]
        public static void CorrectReviewedQuarryUpperFixture()
        {
            var scene = EditorSceneManager.OpenScene("Assets/TruckTrailerPlug/Scenes/TruckTrailerPlug.unity", OpenSceneMode.Single);
            var root = All<SportsMatchTime>(scene).Single().transform.Find("Supported original show venue lighting");
            if (root == null) throw new InvalidOperationException("Review the existing quarry lighting migration first.");
            var fixtures = root.Cast<Transform>().Where(item => item.name.StartsWith("Crane-mounted upper swing flood fixture", StringComparison.Ordinal)).ToArray();
            var upperFill = All<Light>(scene).Where(item => item.name == "Quarry event floodlight" && item.transform.position.x > 0).OrderBy(item => item.transform.position.z).First();
            if (fixtures.Length == 1 && Mathf.Approximately(fixtures[0].GetComponentInChildren<Light>().intensity, 700) && Mathf.Approximately(upperFill.intensity, 18000)) return;
            if (fixtures.Length != 2 || Vector3.Distance(fixtures[0].position, fixtures[1].position) > .01f || fixtures.Any(item => !Mathf.Approximately(item.GetComponentInChildren<Light>().intensity, 7000)) || !Mathf.Approximately(upperFill.intensity, 2200))
                throw new InvalidOperationException("Preserve independently customized fixtures; expected the exact reviewed duplicate layout.");
            if (fixtures[1].GetComponentsInChildren<Collider>(true).Length != 0 || fixtures[1].GetComponentsInChildren<MonoBehaviour>(true).Length != 0) throw new InvalidOperationException("Only the owned render-only duplicate may be removed.");
            UnityEngine.Object.DestroyImmediate(fixtures[1].gameObject);
            var rig = All<LWS.TruckTrailerPlug.TrailerPlugRig>(scene).Single();
            fixtures[0].rotation = Quaternion.LookRotation(rig.TrailerCenter - fixtures[0].position); fixtures[0].GetComponentInChildren<Light>().intensity = 700;
            upperFill.transform.LookAt(rig.truckStart + new Vector3(0, 2, -12)); upperFill.range = 170; upperFill.intensity = 18000;
            EditorUtility.SetDirty(fixtures[0]); EditorUtility.SetDirty(fixtures[0].GetComponentInChildren<Light>()); EditorUtility.SetDirty(upperFill); EditorUtility.SetDirty(upperFill.transform);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        }
        public static void AuditPartyVenueLighting()
        {
            WriteVenueLightingAudit("party-venue-lighting-layout-audit.txt", new[] { "TruckBalloonBattle", "TruckHotPotato", "TruckWhackAMole", "TruckKingOfTheHill", "TruckTetherball", "TruckMagnetMayhem", "TruckVolleyball", "TruckSeesaw" });
            InspectMountedLampModel();
        }
        public static void AuditFieldVenueLighting() => WriteVenueLightingAudit("field-venue-lighting-layout-audit.txt", new[] { "TruckFreezeTag", "TruckFrogger", "TruckHideAndSeek", "TruckHungryHaulers", "TruckJousting", "TruckLimbo", "TruckRedLightGreenLight", "TruckSheepHerding" });
        public static void AuditUrbanVenueLighting() => WriteVenueLightingAudit("urban-venue-lighting-layout-audit.txt", new[] { "TruckFareFight", "TruckSnake", "TruckHideAndSeek" });
        public static void AuditUrbanRoadGeometry()
        {
            Directory.CreateDirectory("Logs/TruckSports/PlaytestRepair");
            using(var output=new StreamWriter("Logs/TruckSports/PlaytestRepair/urban-road-geometry-audit.txt"))
            foreach(string name in new[]{"TruckFareFight","TruckSnake"})
            {
                var scene=EditorSceneManager.OpenScene(SportsBuilder.SceneList().Single(path=>Path.GetFileNameWithoutExtension(path)==name),OpenSceneMode.Single);
                var snake=All<LWS.TruckSnake.SnakeWorld>(scene).FirstOrDefault();
                var fare=All<LWS.TruckFareFight.FareFightMatch>(scene).FirstOrDefault();
                var graph=snake!=null?snake.graph:fare.district.graph;
                output.WriteLine("SCENE "+name+" nodes="+graph.nodes.Count+" edges="+graph.edges.Count);
                foreach(var edge in graph.edges)
                    output.WriteLine("EDGE "+edge.edgeId+" road="+edge.roadId+" class="+edge.roadClass+" lanes="+edge.laneCount+" laneWidth="+edge.laneWidthMeters+" samples="+edge.samples.Count+" first="+edge.samples[0].position+" last="+edge.samples[edge.samples.Count-1].position+" widths="+string.Join(",",edge.samples.Select(point=>point.roadWidthMeters).Distinct()));
                if(snake!=null)foreach(var road in snake.groundRoads)
                    output.WriteLine("GRADED GROUND ROAD "+road.name+" width="+road.width+" markers="+string.Join(";",road.markers.Select(point=>point.ToString("F2"))));
                foreach(var renderer in All<MeshRenderer>(scene).Where(item=>item.name.Contains("Sidewalk")||item.name.Contains("sidewalk")||item.name.Contains("curb")))
                    output.WriteLine("WALKWAY "+renderer.name+" bounds="+renderer.bounds);
            }
        }
        [MenuItem("Truck Sports/Environment/Bind Existing Hide And Seek Loading Lights")]
        public static void BindExistingHideLoadingLights()
        {
            var scene=EditorSceneManager.OpenScene("Assets/TruckHideAndSeek/Scenes/TruckHideAndSeek.unity",OpenSceneMode.Single);
            var clock=All<SportsMatchTime>(scene).Single();if(clock.nightLights.Length!=0)return;
            var lights=All<Light>(scene).Where(light=>light.name=="Supported warm night-shift floodlight" && light.transform.parent!=null && light.transform.parent.name=="Grounded night-shift lighting and sign supports").ToArray();
            if(lights.Length!=30)throw new InvalidOperationException("Inspect and retain the actual30 supported saved loading/staging lights; do not regenerate the obsolete authoring layout.");
            clock.nightLights=lights;EditorUtility.SetDirty(clock);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        [MenuItem("Truck Sports/Environment/Install Urban Road Night Fixtures")]
        public static void InstallUrbanRoadLighting()
        {
            foreach (string name in new[] { "TruckFareFight", "TruckSnake" })
            {
                var scene = EditorSceneManager.OpenScene(SportsBuilder.SceneList().Single(path => Path.GetFileNameWithoutExtension(path) == name), OpenSceneMode.Single);
                var clock = All<SportsMatchTime>(scene).Single();
                if (clock.nightLights.Length != 0 || All<Transform>(scene).Any(item => item.name == "Authored urban roadside night fixtures")) continue;
                var snake = All<LWS.TruckSnake.SnakeWorld>(scene).FirstOrDefault();
                var graph = snake != null ? snake.graph : All<LWS.TruckFareFight.FareFightMatch>(scene).Single().district.graph;
                var holder = new GameObject("Authored urban roadside night fixtures").transform; holder.SetParent(clock.transform, false);
                var lights = new System.Collections.Generic.List<Light>(); var mounts = new System.Collections.Generic.List<Vector3>();
                Physics.SyncTransforms();
                foreach (var edge in graph.edges.Where(item => item.samples.Count >= 6 && item.samples[0].roadWidthMeters >= 18))
                {
                    float length = 0; for (int i = 1; i < edge.samples.Count; i++) length += Vector3.Distance(edge.samples[i - 1].position, edge.samples[i].position);
                    float next = 35, traveled = 0;
                    for (int i = 1; i < edge.samples.Count; i++)
                    {
                        var a = edge.samples[i - 1].position; var b = edge.samples[i].position; float segment = Vector3.Distance(a, b);
                        while (next <= traveled + segment && next < length - 25)
                        {
                            var center = Vector3.Lerp(a, b, (next - traveled) / Mathf.Max(.001f, segment)); next += 75;
                            var forward = Vector3.ProjectOnPlane(b - a, Vector3.up).normalized; var side = Vector3.Cross(Vector3.up, forward);
                            bool elevated = snake != null && center.y - snake.Height(center) > 4;
                            float offset = edge.samples[i].roadWidthMeters * .5f + (elevated ? -.8f : 2.5f);
                            foreach (int sign in new[] { -1, 1 })
                            {
                                var point = center + sign * offset * side;
                                if (mounts.Any(prior => Vector3.Distance(prior, point) < 45)) continue;
                                // Exclude crossing carriageways, including ramps at the same elevation.
                                bool crossing = graph.edges.Any(other => other.samples.Zip(other.samples.Skip(1), (u, v) =>
                                {
                                    if (Mathf.Abs(Vector3.Dot(Vector3.ProjectOnPlane(v.position - u.position, Vector3.up).normalized, forward)) > .85f) return false;
                                    var delta = v.position - u.position; float t = Mathf.Clamp01(Vector3.Dot(point - u.position, delta) / Mathf.Max(.001f, delta.sqrMagnitude));
                                    var nearest = u.position + t * delta;
                                    return Mathf.Abs(nearest.y - center.y) < 4 && Vector3.Distance(nearest, point) < u.roadWidthMeters * .5f + 2;
                                }).Any(value => value));
                                if (crossing) continue;
                                // Mount only on an actual saved static road/sidewalk/terrain surface.
                                // A missing shoulder is skipped rather than fabricating a floating pole.
                                var hits = Physics.RaycastAll(new Ray(point + Vector3.up * 2, Vector3.down), 6, ~0, QueryTriggerInteraction.Ignore)
                                    .Where(hit => hit.rigidbody == null && Vector3.Dot(hit.normal, Vector3.up) > .9f && Mathf.Abs(hit.point.y - center.y) < 2)
                                    .OrderBy(hit => Mathf.Abs(hit.point.y - center.y)).ToArray();
                                if (hits.Length == 0) continue;
                                point.y = hits[0].point.y;
                                if (Physics.OverlapBox(point + Vector3.up * 6, new Vector3(.6f, 5.5f, .6f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore).Any(item => item.attachedRigidbody == null)) continue;
                                var pole = new GameObject("Supported roadside fixture " + lights.Count).transform; pole.SetParent(holder, false); pole.position = point;
                                pole.rotation = Quaternion.Euler(0, Quaternion.LookRotation(center - point).eulerAngles.y - 90, 0);
                                LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(pole, "Assets/Toon Gas Station/Prefabs/Streetlight_1C.prefab", new Vector3(100, 18, 100), 0, false, "TruckSports");
                                var renderers = pole.GetComponentsInChildren<Renderer>(); Bounds bounds = renderers[0].bounds; foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
                                var light = new GameObject("Budgeted urban road night light").AddComponent<Light>(); light.transform.SetParent(pole, false); light.transform.position = new Vector3(bounds.center.x, bounds.max.y - .4f, bounds.center.z);
                                light.transform.rotation = Quaternion.LookRotation(center - light.transform.position); light.type = LightType.Spot; light.range = 95; light.spotAngle = 112; light.innerSpotAngle = 80; light.intensity = 2400; light.color = elevated ? new Color(.83f, .92f, 1) : new Color(1, .91f, .75f); light.shadows = LightShadows.None;
                                lights.Add(light); mounts.Add(point);
                            }
                        }
                        traveled += segment;
                    }
                }
                if (lights.Count < 20) throw new InvalidOperationException("Inspect actual saved road surfaces before accepting a sparse urban light layout: " + name + " " + lights.Count);
                clock.morningLampMultiplier = .65f; clock.lampSnowSurface = All<SportsSnowSurface>(scene).Single(); clock.coveredLampMultiplier = .3f;
                clock.nightLights = lights.ToArray(); EditorUtility.SetDirty(clock); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                Debug.Log(name + " supported road fixtures: " + lights.Count + "; original roads, colliders and navigation unchanged.");
            }
            AssetDatabase.SaveAssets();
        }
        [MenuItem("Truck Sports/Environment/Complete Urban Road Light Budgets")]
        public static void CompleteUrbanRoadLightBudgets()
        {
            foreach (string name in new[] { "TruckFareFight", "TruckSnake" })
            {
                string source = "Assets/" + name + "/Rendering/Weather renderer.asset";
                string target = "Assets/TruckSports/Rendering/" + name + " local clustered renderer.asset";
                var pipeline = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>(name == "TruckSnake" ? "Assets/TruckSnake/Rendering/Weather pipeline.asset" : "Assets/TruckSports/Rendering/" + name + " snow pipeline.asset");
                var settings = new SerializedObject(pipeline); var first = settings.FindProperty("m_RendererDataList").GetArrayElementAtIndex(0);
                string current = AssetDatabase.GetAssetPath(first.objectReferenceValue);
                if (current != source && current != target) throw new InvalidOperationException("Inspect an independently customized road renderer before migrating: " + current);
                if (AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRendererData>(target) == null && !AssetDatabase.CopyAsset(source, target)) throw new InvalidOperationException("Could not preserve the original mode renderer features.");
                var renderer = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRendererData>(target);
                renderer.renderingMode = UnityEngine.Rendering.Universal.RenderingMode.ForwardPlus;
                first.objectReferenceValue = renderer; settings.ApplyModifiedPropertiesWithoutUndo(); pipeline.maxAdditionalLightsCount = 8;
                EditorUtility.SetDirty(renderer); EditorUtility.SetDirty(pipeline);
            }
            AssetDatabase.SaveAssets();
        }
        static void WriteVenueLightingAudit(string filename, string[] names)
        {
            Directory.CreateDirectory("Logs/TruckSports/PlaytestRepair");
            using (var output = new StreamWriter("Logs/TruckSports/PlaytestRepair/" + filename))
            {
                foreach (string name in names)
                {
                    var scene = EditorSceneManager.OpenScene(SportsBuilder.SceneList().Single(path => Path.GetFileNameWithoutExtension(path) == name), OpenSceneMode.Single);
                    var clock = All<SportsMatchTime>(scene).Single(); output.WriteLine("SCENE " + name + " lamps=" + clock.nightLights.Length);
                    var truckBeams = new System.Collections.Generic.HashSet<Light>(All<SportsNightHeadlights>(scene).SelectMany(item=>item.beams.Select(beam=>beam.light).Concat(item.wheelOnlyBeams)).Where(item=>item!=null));
                    foreach(var light in All<Light>(scene).Where(item=>item.GetComponentInParent<WeatherMakerScript>()==null && item.type!=LightType.Directional))
                        output.WriteLine((truckBeams.Contains(light)||light.GetComponentInParent<Rigidbody>()!=null ? "VEHICLE LIGHT " : "SAVED NONVEHICLE LIGHT ")+light.name+" at="+light.transform.position+" power="+light.intensity+" range="+light.range+" enabled="+light.enabled+" active="+light.gameObject.activeInHierarchy+" parent="+(light.transform.parent!=null?light.transform.parent.name:"ROOT"));
                    foreach (var renderer in All<MeshRenderer>(scene).Where(item => item.GetComponentInParent<WeatherMakerScript>() == null && item.bounds.size.x * item.bounds.size.z > 100).OrderByDescending(item => item.bounds.size.x * item.bounds.size.z).Take(16)) output.WriteLine("MESH " + renderer.name + " bounds=" + renderer.bounds);
                    foreach (var lamp in All<Light>(scene).Where(item => item.GetComponentInParent<WeatherMakerScript>() == null && item.GetComponentInParent<Rigidbody>() == null && item.type != LightType.Directional)) output.WriteLine("EXISTING STATIC LIGHT " + lamp.name + " at=" + lamp.transform.position + " power=" + lamp.intensity + " range=" + lamp.range + " enabled=" + lamp.enabled);
                    foreach (var root in scene.GetRootGameObjects().Where(item => item.name.Contains("gantry") || item.name.Contains("plateau") || item.name.Contains("platform"))) output.WriteLine("ROOT " + root.name + " at=" + root.transform.position);
                    var hill = All<LWS.TruckKingOfTheHill.HillMatch>(scene).FirstOrDefault();
                    if (hill != null) for (int i = 0; i < hill.sites.Length; i++) output.WriteLine("PLATEAU " + hill.siteNames[i] + " at=" + hill.sites[i]);
                }
            }
        }
        public static void InspectMountedLampModel()
        {
            Directory.CreateDirectory("Logs/TruckSports/PlaytestRepair");
            using (var output = new StreamWriter("Logs/TruckSports/PlaytestRepair/installed-wall-lamp-details.txt"))
            {
                var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Plugins/Pixel Crushers/Dialogue System/Demo/Art/Scifi Environment/Models/WallLamp.FBX");
                if (source == null) throw new InvalidOperationException("Inspect the installed owned wall-lamp model before mounted-fixture authoring.");
                var authored = AssetDatabase.LoadAssetAtPath<Material>("Assets/Plugins/Pixel Crushers/Dialogue System/Demo/Art/Scifi Environment/Materials/WallLamp-16 - Default.mat");
                if (authored == null) throw new InvalidOperationException("Use the material actually assigned by the vendor Room prefab.");
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var model = UnityEngine.Object.Instantiate(source); model.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                var renderers = model.GetComponentsInChildren<Renderer>(); Bounds bounds = renderers[0].bounds;
                foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
                output.WriteLine("INSTALLED WALL LAMP bounds=" + bounds);
                var owned = new System.Collections.Generic.List<Material>();
                foreach (var renderer in renderers)
                {
                    var materials = renderer.sharedMaterials;
                    for (int i = 0; i < materials.Length; i++)
                    {
                        output.WriteLine("VENDOR ROOM LAMP MATERIAL " + AssetDatabase.GetAssetPath(authored) + " shader=" + authored.shader.name);
                        var material = new Material(authored); material.hideFlags = HideFlags.DontSave;
                        materials[i] = material; owned.Add(material);
                    }
                    renderer.sharedMaterials = materials;
                }
                RenderSettings.skybox = null; RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = new Color(.25f, .25f, .25f);
                var lamp = new GameObject("Transient asset inspection light").AddComponent<Light>(); lamp.type = LightType.Directional; lamp.intensity = 1.5f; lamp.transform.rotation = Quaternion.Euler(35, -25, 0);
                var camera = new GameObject("Transient installed-model inspection").AddComponent<Camera>(); camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.08f, .1f, .14f); camera.fieldOfView = 35;
                camera.nearClipPlane = .01f;
                camera.transform.position = bounds.center + new Vector3(1, .45f, 1.3f).normalized * bounds.size.magnitude * 2.2f; camera.transform.LookAt(bounds.center);
                var target = RenderTexture.GetTemporary(960, 640, 24); var previous = RenderTexture.active; var image = new Texture2D(960, 640, TextureFormat.RGB24, false);
                try { camera.targetTexture = target; camera.Render(); RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, 960, 640), 0, 0); image.Apply(); File.WriteAllBytes("Logs/TruckSports/PlaytestRepair/installed-wall-lamp-inspection.png", image.EncodeToPNG()); }
                finally { camera.targetTexture = null; RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target); UnityEngine.Object.DestroyImmediate(image); foreach (var material in owned) UnityEngine.Object.DestroyImmediate(material); }
                // Inspection is a temporary unsaved scene. No source model,
                // material, vendor code or authored venue is saved or modified.
            }
        }
        [MenuItem("Truck Sports/Environment/Install Party Venue Night Fixtures")]
        public static void InstallPartyVenueLighting()
        {
            foreach (string name in new[] { "TruckBalloonBattle", "TruckHotPotato", "TruckWhackAMole", "TruckKingOfTheHill", "TruckTetherball", "TruckMagnetMayhem", "TruckVolleyball", "TruckSeesaw" })
            {
                var scene = EditorSceneManager.OpenScene(SportsBuilder.SceneList().Single(path => Path.GetFileNameWithoutExtension(path) == name), OpenSceneMode.Single);
                var clock = All<SportsMatchTime>(scene).Single();
                if (clock.nightLights.Length != 0 || clock.transform.Find("Authored party venue night fixtures") != null) continue;
                var root = new GameObject("Authored party venue night fixtures").transform; root.SetParent(clock.transform, false);
                var lights = new System.Collections.Generic.List<Light>();
                if (name == "TruckKingOfTheHill")
                {
                    var hill = All<LWS.TruckKingOfTheHill.HillMatch>(scene).Single();
                    var rails = All<MeshRenderer>(scene).Where(item => item.name == "Visible continuous bridge edge rail").ToArray();
                    if (rails.Length != 16) throw new InvalidOperationException("Preserve and audit the eight existing paired bridge rails before mounting lights.");
                    foreach (var rail in rails)
                    {
                        var local = rail.GetComponent<MeshFilter>().sharedMesh.bounds;
                        var mount = rail.transform.TransformPoint(new Vector3(local.center.x, local.max.y, local.center.z));
                        var axis = Vector3.ProjectOnPlane(rail.transform.forward, Vector3.up).normalized;
                        var endpoints = hill.sites.Where(site => Vector3.Cross(Vector3.ProjectOnPlane(site - mount, Vector3.up), axis).magnitude < 16).OrderBy(site => Vector3.ProjectOnPlane(site - mount, Vector3.up).sqrMagnitude).Take(2).ToArray();
                        if (endpoints.Length != 2) throw new InvalidOperationException("Bind lights to the actual supported bridge and adjacent plateaus.");
                        bool right = Vector3.Dot(mount - (endpoints[0] + endpoints[1]) * .5f, rail.transform.right) > 0;
                        AddPole(mount, endpoints[right ? 0 : 1], 22, 145, 6500, new Color(1, .94f, .8f));
                    }
                }
                else if (name == "TruckSeesaw")
                {
                    var beam = All<MeshRenderer>(scene).Single(item => item.name == "Suspension gantry crossbeam").bounds;
                    var deck = All<MeshRenderer>(scene).Single(item => item.name == "Physical weight-responsive suspended wobble deck").bounds;
                    foreach (float x in new[] { -40f, 0f, 40f }) foreach (int side in new[] { -1, 1 })
                    {
                        var fixture = new GameObject("Gantry-mounted quarry flood fixture " + lights.Count).transform; fixture.SetParent(root, false);
                        fixture.position = new Vector3(beam.center.x + x, beam.min.y - .4f, beam.center.z + side * .8f);
                        fixture.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
                        InstallMountedLampModel(fixture);
                        var body = fixture.GetComponentsInChildren<MeshRenderer>().Where(item => item.name != "Original nine-LED emitter").ToArray();
                        Bounds bounds = body[0].bounds; foreach (var renderer in body.Skip(1)) bounds.Encapsulate(renderer.bounds);
                        fixture.position += Vector3.up * (beam.min.y - bounds.max.y);
                        var position = fixture.position + Vector3.down * .7f;
                        AddLight(fixture, position, new Vector3(fixture.position.x, deck.max.y, side * 25), 160, 3000, new Color(.9f, .95f, 1));
                    }
                }
                else if (name == "TruckMagnetMayhem" || name == "TruckVolleyball")
                {
                    bool sand = name == "TruckVolleyball";
                    var floor = All<MeshRenderer>(scene).Single(item => item.name == (sand ? "Continuous volleyball sand court" : "Continuous detailed industrial scrapyard floor")).bounds;
                    foreach (float z in sand ? new[] { -45f, 45f } : new[] { -70f, 0f, 70f }) foreach (int side in new[] { -1, 1 })
                        AddPole(new Vector3(side * (sand ? 58 : 98), floor.max.y, z), new Vector3(0, floor.max.y, z * .6f), sand ? 24 : 28, sand ? 150 : 180, sand ? 4200 : 12000, sand ? new Color(1, .94f, .82f) : new Color(.86f, .95f, 1));
                }
                else
                {
                    string floorName = name == "TruckBalloonBattle" ? "Continuous party stadium collision base" : name == "TruckHotPotato" ? "Continuous modeled stadium arena base" : name == "TruckWhackAMole" ? "Continuous navigable dirt arena" : "Continuous detailed oversized tether court";
                    var floor = All<MeshRenderer>(scene).Single(item => item.name == floorName).bounds;
                    var stands = All<MeshRenderer>(scene).Where(item => item.name == "Union Field Grandstand tier").ToArray();
                    foreach (int side in new[] { -1, 1 })
                    {
                        var seating = stands.Where(item => Mathf.Sign(item.bounds.center.x) == side).OrderByDescending(item => Mathf.Abs(item.bounds.center.x)).First();
                        var tier = seating.bounds;
                        // The reused Union Field tiers tilt eight degrees.
                        // Fit to their actual top plane, not the high corner of
                        // the world AABB, which would leave an inset pole floating.
                        var mesh = seating.GetComponent<MeshFilter>().sharedMesh.bounds;
                        var top = seating.transform.TransformPoint(new Vector3(mesh.center.x, mesh.max.y, mesh.center.z));
                        var normal = seating.transform.TransformDirection(Vector3.up);
                        foreach (float fraction in new[] { -.55f, 0f, .55f })
                        {
                            var mount = new Vector3(side < 0 ? tier.min.x + 1 : tier.max.x - 1, tier.max.y, floor.center.z + fraction * floor.extents.z);
                            mount.y = top.y - (normal.x * (mount.x - top.x) + normal.z * (mount.z - top.z)) / normal.y;
                            float distance = Vector3.Distance(mount + Vector3.up * 24, floor.center);
                            Color color = name == "TruckBalloonBattle" ? new Color(1, .88f, .96f) : name == "TruckTetherball" ? new Color(.86f, .94f, 1) : new Color(1, .93f, .81f);
                            float surfaceGain = name == "TruckBalloonBattle" ? 1 : name == "TruckTetherball" ? 1.15f / .65f : 1.6f / .65f;
                            AddPole(mount, new Vector3(floor.center.x, floor.max.y, floor.center.z + fraction * floor.extents.z * .5f), 24, distance + floor.extents.magnitude * .6f + 30, Mathf.Clamp(distance * distance * .65f, 8000, 28000) * surfaceGain, color);
                        }
                    }
                }
                clock.nightLights = lights.ToArray(); EditorUtility.SetDirty(clock); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                void AddPole(Vector3 position, Vector3 target, float height, float range, float power, Color color)
                {
                    var pole = new GameObject("Supported party venue fixture " + lights.Count).transform; pole.SetParent(root, false); pole.position = position;
                    pole.rotation = Quaternion.Euler(0, target.x < position.x ? 180 : 0, 0);
                    LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(pole, "Assets/Toon Gas Station/Prefabs/Streetlight_1C.prefab", new Vector3(100, height, 100), 0, false, "TruckSports");
                    var renderers = pole.GetComponentsInChildren<Renderer>(); Bounds bounds = renderers[0].bounds; foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
                    AddLight(pole, new Vector3(bounds.center.x, bounds.max.y - .4f, bounds.center.z), target, range, power, color);
                }
                void AddLight(Transform fixture, Vector3 position, Vector3 target, float range, float power, Color color)
                {
                    var light = new GameObject("Budgeted party venue night light").AddComponent<Light>(); light.transform.SetParent(fixture, false); light.transform.position = position; light.transform.rotation = Quaternion.LookRotation(target - position);
                    light.type = LightType.Spot; light.range = range; light.spotAngle = 108; light.innerSpotAngle = 76; light.intensity = power; light.color = color; light.shadows = LightShadows.None; lights.Add(light);
                }
            }
            AssetDatabase.SaveAssets();
        }
        [MenuItem("Truck Sports/Environment/Install Field Venue Night Fixtures")]
        public static void InstallFieldVenueLighting()
        {
            foreach (string name in new[] { "TruckFreezeTag", "TruckFrogger", "TruckHungryHaulers", "TruckJousting", "TruckLimbo", "TruckRedLightGreenLight", "TruckSheepHerding" })
            {
                var scene = EditorSceneManager.OpenScene(SportsBuilder.SceneList().Single(path => Path.GetFileNameWithoutExtension(path) == name), OpenSceneMode.Single);
                var clock = All<SportsMatchTime>(scene).Single();
                if (clock.nightLights.Length != 0 || All<Transform>(scene).Any(item => item.name == "Authored field venue night fixtures")) continue;
                var lights = new System.Collections.Generic.List<Light>();
                if (name == "TruckFrogger")
                {
                    var world = All<LWS.TruckFrogger.FrogWorld>(scene).Single();
                    foreach (var course in world.courses)
                    {
                        // The original course root already owns activation.
                        // Inactive courses therefore cost no active lamp objects.
                        var root = Root(course.root);
                        var banks = course.root.GetComponentsInChildren<MeshRenderer>(true).Where(item => item.name == "Continuous dressed riverbank driving terrain").ToArray();
                        var positions = new System.Collections.Generic.SortedSet<float> { course.start.z - 10, course.finish.z + 10 };
                        for (float z = course.start.z - 10; z <= course.finish.z + 10; z += 20) positions.Add(z);
                        foreach (var water in course.water) { positions.Add(water.x - 14); positions.Add(water.y + 14); }
                        float previous = float.NegativeInfinity;
                        foreach (float z in positions)
                        {
                            if (z - previous < 35) continue;
                            if (course.water.Any(water => z > water.x - 8 && z < water.y + 8)) continue;
                            // Keep modeled poles out of transverse traffic and
                            // train corridors, including the return carriageway.
                            if (course.gates.Any(gate => Mathf.Abs(z - gate.z) < 12 || Mathf.Abs(z - gate.z - 28) < 12)) continue;
                            var bank = banks.FirstOrDefault(item => z > item.bounds.min.z + 1 && z < item.bounds.max.z - 1);
                            if (bank == null) continue;
                            previous = z;
                            foreach (int side in new[] { -1, 1 })
                                Pole(root, new Vector3(course.start.x + side * 36, bank.bounds.max.y, z), new Vector3(course.start.x, .05f, z), 24, 145, 4200, new Color(.9f, .95f, 1));
                        }
                        if (root.childCount < 8) throw new InvalidOperationException("Each of the five actual Frogger courses needs supported roadside illumination.");
                    }
                    clock.morningLampMultiplier = .65f;
                    clock.lampSnowSurface = All<SportsSnowSurface>(scene).Single();
                    clock.coveredLampMultiplier = .15f;
                }
                else
                {
                    var root = Root(clock.transform);
                    if (name == "TruckSheepHerding")
                    {
                        var ground = All<MeshCollider>(scene).Single(item => item.name == "Continuous rolling collision-matched farm landscape");
                        Physics.SyncTransforms(); var center = Ground(0, 0);
                        for (int i = 0; i < 8; i++)
                        {
                            float angle = i * Mathf.PI / 4;
                            Pole(root, Ground(Mathf.Sin(angle) * 116, Mathf.Cos(angle) * 116), center, 30, 175, 7000, new Color(1, .94f, .81f));
                        }
                        Vector3 Ground(float x, float z)
                        {
                            if (!ground.Raycast(new Ray(new Vector3(x, ground.bounds.max.y + 2, z), Vector3.down), out var hit, ground.bounds.size.y + 10)) throw new InvalidOperationException("Mount farm fixtures on the actual saved rolling collider, never an approximate height.");
                            return hit.point;
                        }
                    }
                    else if (name == "TruckLimbo")
                    {
                        var floor = All<MeshRenderer>(scene).Single(item => item.name == "Textured tropical festival sand").bounds;
                        foreach (int side in new[] { -1, 1 }) foreach (float z in new[] { -22f, 68f })
                            Pole(root, new Vector3(side * 36, floor.max.y, z), new Vector3(0, floor.max.y, z < 0 ? 0 : 42), 18, 100, 2000, new Color(1, .9f, .72f));
                    }
                    else
                    {
                        bool freeze = name == "TruckFreezeTag", red = name == "TruckRedLightGreenLight", joust = name == "TruckJousting";
                        string floorName = freeze ? "Continuous packed-snow freeze arena" : red ? "Substantial surrounding park" : joust ? "Continuous festival meadow surround" : "Finished carnival surrounding landscape";
                        var floor = All<MeshRenderer>(scene).Single(item => item.name == floorName).bounds;
                        float x = freeze ? 110 : red ? 75 : joust ? 60 : 114;
                        var rows = freeze ? new[] { -74f, 0f, 74f } : red ? new[] { -15f, 95f, 205f, 315f } : joust ? new[] { -110f, 0f, 110f } : new[] { -55f, 0f, 55f };
                        foreach (float z in rows) foreach (int side in new[] { -1, 1 })
                            Pole(root, new Vector3(side * x, floor.max.y, z), new Vector3(0, 0, freeze ? z * .6f : z), red ? 29 : 30, red ? 165 : joust ? 155 : 180, freeze ? 6500 : red || joust ? 8500 : 9500, freeze ? new Color(.8f, .91f, 1) : red ? new Color(.9f, .95f, 1) : new Color(1, .93f, .81f));
                    }
                }
                clock.nightLights = lights.ToArray(); EditorUtility.SetDirty(clock); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                Transform Root(Transform parent)
                {
                    var holder = new GameObject("Authored field venue night fixtures").transform; holder.SetParent(parent, false); return holder;
                }
                void Pole(Transform parent, Vector3 position, Vector3 target, float height, float range, float power, Color color)
                {
                    var pole = new GameObject("Supported field fixture " + lights.Count).transform; pole.SetParent(parent, false); pole.position = position;
                    pole.rotation = Quaternion.Euler(0, target.x < position.x ? 180 : 0, 0);
                    LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(pole, "Assets/Toon Gas Station/Prefabs/Streetlight_1C.prefab", new Vector3(100, height, 100), 0, false, "TruckSports");
                    var renderers = pole.GetComponentsInChildren<Renderer>(); Bounds bounds = renderers[0].bounds; foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
                    var lamp = new GameObject("Budgeted field venue night light").AddComponent<Light>(); lamp.transform.SetParent(pole, false); lamp.transform.position = new Vector3(bounds.center.x, bounds.max.y - .4f, bounds.center.z);
                    lamp.transform.rotation = Quaternion.LookRotation(target - lamp.transform.position); lamp.type = LightType.Spot; lamp.range = range; lamp.spotAngle = 108; lamp.innerSpotAngle = 76; lamp.intensity = power; lamp.color = color; lamp.shadows = LightShadows.None; lights.Add(lamp);
                }
            }
            AssetDatabase.SaveAssets();
        }
        [MenuItem("Truck Sports/Environment/Complete Field Venue Light Budgets")]
        public static void CompleteFieldVenueLightBudgets()
        {
            const string target = "Assets/TruckSports/Rendering/TruckFrogger local clustered renderer.asset";
            foreach (string name in new[] { "TruckFrogger", "TruckRedLightGreenLight" })
            {
                var pipeline = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>("Assets/TruckSports/Rendering/" + name + " snow pipeline.asset");
                if (pipeline == null) throw new InvalidOperationException("Retain each sport's existing scoped weather/snow pipeline.");
                pipeline.maxAdditionalLightsCount = 8;
                if (name == "TruckFrogger")
                {
                    var settings = new SerializedObject(pipeline); var first = settings.FindProperty("m_RendererDataList").GetArrayElementAtIndex(0);
                    var current = first.objectReferenceValue; string source = AssetDatabase.GetAssetPath(current);
                    if (source != target && source != "Assets/TruckBaseJumping/Rendering/Base renderer.asset") throw new InvalidOperationException("Preserve an independently customized Frogger renderer; inspect it before migrating.");
                    var renderer = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRendererData>(target);
                    if (renderer == null)
                    {
                        // Copy the existing owned configuration and its embedded
                        // Weather Maker feature, not vendor source or a new pass.
                        if (!AssetDatabase.CopyAsset(source, target)) throw new InvalidOperationException("Retain the original renderer and every authored weather feature.");
                        renderer = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRendererData>(target);
                        renderer.renderingMode = UnityEngine.Rendering.Universal.RenderingMode.ForwardPlus; EditorUtility.SetDirty(renderer);
                    }
                    if (renderer.renderingMode != UnityEngine.Rendering.Universal.RenderingMode.ForwardPlus) throw new InvalidOperationException("Do not overwrite a designer-modified local renderer.");
                    first.objectReferenceValue = renderer; settings.ApplyModifiedPropertiesWithoutUndo();
                }
                EditorUtility.SetDirty(pipeline);
            }
            AssetDatabase.SaveAssets();
        }
        static void InstallMountedLampModel(Transform fixture)
        {
            const string sourcePath = "Assets/Plugins/Pixel Crushers/Dialogue System/Demo/Art/Scifi Environment/Models/WallLamp.FBX";
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Plugins/Pixel Crushers/Dialogue System/Demo/Art/Scifi Environment/Materials/WallLamp-16 - Default.mat");
            if (source == null || material == null) throw new InvalidOperationException("Reuse the installed model and its actual vendor Room material.");
            var body = SportsReplayVisuals.Copy(source.transform); body.name = "Installed modeled floodlight housing";
            var renderers = body.GetComponentsInChildren<Renderer>(); Bounds bounds = renderers[0].bounds; foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            body.transform.localScale *= 1.5f / bounds.size.y;
            bounds = renderers[0].bounds; foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            body.transform.position -= bounds.center; body.transform.SetParent(fixture, false);
            foreach (var renderer in renderers) renderer.sharedMaterial = material;
            const string meshPath = "Assets/TruckSports/Models/Original mounted venue LED panel.asset", matPath = "Assets/TruckSports/Materials/Mounted venue LED panel.mat";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (mesh == null)
            {
                var vertices = new System.Collections.Generic.List<Vector3>(); var triangles = new System.Collections.Generic.List<int>();
                for (int y = -1; y <= 1; y++) for (int x = -1; x <= 1; x++)
                {
                    int start = vertices.Count; float cx = x * .3f, cy = y * .28f;
                    vertices.Add(new Vector3(cx - .115f, cy - .1f, 0)); vertices.Add(new Vector3(cx + .115f, cy - .1f, 0)); vertices.Add(new Vector3(cx + .115f, cy + .1f, 0)); vertices.Add(new Vector3(cx - .115f, cy + .1f, 0));
                    triangles.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
                }
                mesh = new Mesh { name = "Original bounded nine-LED fixture panel" }; mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); Directory.CreateDirectory("Assets/TruckSports/Models"); AssetDatabase.CreateAsset(mesh, meshPath);
            }
            var panelMaterial = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (panelMaterial == null) { panelMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit")); panelMaterial.color = new Color(1, .96f, .84f); panelMaterial.enableInstancing = true; AssetDatabase.CreateAsset(panelMaterial, matPath); }
            var panel = new GameObject("Original nine-LED emitter"); panel.transform.SetParent(fixture, false); panel.transform.localPosition = new Vector3(0, 0, bounds.extents.z + .015f);
            panel.AddComponent<MeshFilter>().sharedMesh = mesh; panel.AddComponent<MeshRenderer>().sharedMaterial = panelMaterial;
        }
        [MenuItem("Truck Sports/Environment/Install Kickball and Keepaway Cab Night Fixtures")]
        public static void InstallStadiumCabVenueLighting()
        {
            foreach (string name in new[] { "TruckKickball", "TruckKeepawayFirstPerson" })
            {
                var scene = EditorSceneManager.OpenScene(SportsBuilder.SceneList().Single(path => Path.GetFileNameWithoutExtension(path) == name), OpenSceneMode.Single);
                var clock = All<SportsMatchTime>(scene).Single();
                if (clock.nightLights.Length != 0 || clock.transform.Find("Authored night venue lamps") != null) continue;
                if (name == "TruckKeepawayFirstPerson")
                {
                    // Reuse the already-authored companion venue's purchased
                    // fixtures, asset references and tuned illumination exactly.
                    var preview = EditorSceneManager.OpenPreviewScene("Assets/TruckKeepaway/Scenes/TruckKeepaway.unity");
                    try
                    {
                        var sourceClock = All<SportsMatchTime>(preview).Single();
                        var source = sourceClock.transform.Find("Authored night venue lamps");
                        if (source == null || sourceClock.nightLights.Length != 4) throw new InvalidOperationException("Keep the existing reviewed four-fixture Keepaway venue as the source.");
                        var copy = UnityEngine.Object.Instantiate(source.gameObject); copy.name = source.name;
                        SceneManager.MoveGameObjectToScene(copy, scene); copy.transform.SetParent(clock.transform, false);
                        clock.nightLights = copy.GetComponentsInChildren<Light>(true);
                    }
                    finally { EditorSceneManager.ClosePreviewScene(preview); }
                }
                else
                {
                    var turf = All<MeshRenderer>(scene).Single(item => item.name == "Connected stadium ground turf").bounds;
                    var root = new GameObject("Authored night venue lamps").transform; root.SetParent(clock.transform, false);
                    var lights = new System.Collections.Generic.List<Light>();
                    // Saved fence radius78; first seating tier starts at88.5.
                    // Fixtures occupy the clear perimeter band, not the diamond
                    // or the purchased stands. Original collision stays intact.
                    foreach (float z in new[] { -36f, 34f, 104f }) foreach (int side in new[] { -1, 1 })
                    {
                        var pole = new GameObject("Kickball stadium fixture " + lights.Count).transform; pole.SetParent(root, false);
                        pole.position = new Vector3(side * 83, turf.max.y, z); pole.rotation = Quaternion.Euler(0, side < 0 ? 0 : 180, 0);
                        LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(pole, "Assets/Toon Gas Station/Prefabs/Streetlight_1C.prefab", new Vector3(100, 34, 100), 0, false, "TruckKickball");
                        var renderers = pole.GetComponentsInChildren<Renderer>(); Bounds bounds = renderers[0].bounds;
                        foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
                        var head = new GameObject("Budgeted Kickball stadium night light").AddComponent<Light>(); head.transform.SetParent(pole, false);
                        head.transform.position = new Vector3(bounds.center.x, bounds.max.y - .4f, bounds.center.z);
                        head.transform.rotation = Quaternion.LookRotation(new Vector3(0, turf.max.y, Mathf.Clamp(z, -16, 84)) - head.transform.position);
                        head.type = LightType.Spot; head.range = 170; head.spotAngle = 108; head.innerSpotAngle = 76;
                        head.color = new Color(.9f, .94f, 1); head.intensity = 6000; head.shadows = LightShadows.None; lights.Add(head);
                    }
                    clock.nightLights = lights.ToArray();
                }
                if (clock.nightLights.Any(light => light.shadows != LightShadows.None)) throw new InvalidOperationException("Venue lamps must not add shadow maps.");
                EditorUtility.SetDirty(clock); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
        }
        public static void InstallRecordedVehicleLightBindings()
        {
            foreach (string name in new[] { "TruckDockDiving", "TruckGiantSwing" })
            {
                var scene = EditorSceneManager.OpenScene(SportsBuilder.SceneList().Single(path => Path.GetFileNameWithoutExtension(path) == name), OpenSceneMode.Single);
                var lights = All<SportsNightHeadlights>(scene).Single();
                Transform vehicle;
                MonoBehaviour recorder;
                if (name == "TruckDockDiving")
                {
                    var match = All<LWS.TruckDockDiving.DockMatch>(scene).Single(); vehicle = match.truck.transform; recorder = match.replay;
                }
                else
                {
                    var match = All<LWS.TruckGiantSwing.SwingMatch>(scene).Single(); vehicle = match.truck.transform; recorder = match.replay;
                }
                if (!(recorder is ISportsRecordedVehicleView)) throw new InvalidOperationException("Bind the existing read-only recorder interface.");
                var beams = lights.beams.Where(beam => beam.target == vehicle).ToArray();
                if (beams.Length == 0) throw new InvalidOperationException("Preserve and bind the existing vehicle beams: " + name);
                foreach (var beam in beams)
                {
                    if (beam.replaySource != null && beam.replaySource != recorder) throw new InvalidOperationException("Do not overwrite a designer's replay binding.");
                    beam.replaySource = recorder;
                }
                EditorUtility.SetDirty(lights); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
        }
        public static void Audit()
        {
            Directory.CreateDirectory("Logs/TruckSports/Environment");
            using (var output = new StreamWriter("Logs/TruckSports/Environment/representative-audit.txt"))
            foreach (string path in SportsBuilder.SceneList().Where(path => Representative.Contains(Path.GetFileNameWithoutExtension(path))))
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                output.WriteLine("SCENE " + path);
                foreach (var weather in All<SportsChaosWeather>(scene)) output.WriteLine("WEATHER " + weather.name + " presets=" + (weather.presets != null) + " session=" + weather.session);
                foreach (var cycle in All<WeatherMakerDayNightCycleManagerScript>(scene)) output.WriteLine("DAY PROFILE " + AssetDatabase.GetAssetPath(cycle.DayNightProfile) + " seconds=" + cycle.TimeOfDay + " ambient=" + cycle.DayNightProfile.AmbientColorMode);
                foreach (var picker in All<SportsWeatherPicker>(scene)) output.WriteLine("PICKER " + picker.name + " parent=" + picker.transform.parent.name + " rect=" + picker.GetComponent<RectTransform>().anchoredPosition + " size=" + picker.GetComponent<RectTransform>().sizeDelta);
                foreach (var light in All<Light>(scene).Where(light => light.GetComponentInParent<WeatherMakerScript>() == null)) output.WriteLine("LIGHT " + light.name + " type=" + light.type + " intensity=" + light.intensity + " active=" + light.gameObject.activeInHierarchy + " enabled=" + light.enabled + " at=" + light.transform.position);
                output.WriteLine("SKY " + (RenderSettings.skybox != null ? AssetDatabase.GetAssetPath(RenderSettings.skybox) : "none"));
            }
        }
        // Explicit migration only; normal builds and Play Mode never rewrite setup choices.
        public static void InstallRepresentative() => InstallPaths(SportsBuilder.SceneList().Where(path => Representative.Contains(Path.GetFileNameWithoutExtension(path))), false);
        public static void InstallApplicable()
        {
            // Preserve the two specifically protected Plowing moods and the legacy
            // lifecycle adapters. Their integration requires a separate review.
            InstallPaths(SportsBuilder.SceneList().Where(path => !Representative.Contains(Path.GetFileNameWithoutExtension(path)) && !Path.GetFileName(path).StartsWith("TruckPedestrianPlowing", StringComparison.Ordinal)), true);
        }
        static void InstallPaths(System.Collections.Generic.IEnumerable<string> paths, bool collection)
        {
            Directory.CreateDirectory("Logs/TruckSports/Environment");
            using (var output = new StreamWriter("Logs/TruckSports/Environment/" + (collection ? "collection-install.txt" : "representative-install.txt")))
            foreach (string path in paths)
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var weather = All<SportsChaosWeather>(scene).SingleOrDefault();
                if (weather == null || weather.session == null) { output.WriteLine(path + ": no shared outdoor session/weather lifecycle; retained for separate indoor/legacy review."); continue; }
                var binding = weather.GetComponent<SportsMatchTime>() ?? weather.gameObject.AddComponent<SportsMatchTime>();
                if (weather.presets == null) weather.presets = SportsWeatherPresetsAuthoring.EnsureCatalog();
                binding.cycle = weather.vendor.GetComponentInChildren<WeatherMakerDayNightCycleManagerScript>(true);
                binding.clouds = weather.clouds;
                BindThreeDimensionalSky(weather, binding);
                BindSkyProfile(weather, binding);
                binding.daylightFill = All<Light>(scene).SingleOrDefault(light => light.type == LightType.Directional && light.GetComponentInParent<WeatherMakerScript>() == null);
                var cameras = All<SportsEventCamera>(scene).Select(rig => rig.view).Where(camera => camera != null).Distinct().ToArray();
                if (weather.session.presentationWeather == weather)
                {
                    // These modes replace session.cameras during spectator,
                    // split-screen and replay transitions. Bind the authored
                    // venue camera roster, not only the initial setup array.
                    weather.vendor.AllowCameras.Clear(); weather.vendor.AllowCameras.AddRange(cameras); EditorUtility.SetDirty(weather.vendor);
                }
                string folder = Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(path)));
                SportsWeatherRenderingAuthoring.UseViewportSkybox(weather.gameObject, cameras, folder);
                InstallVenueLamps(scene, binding);
                InstallHeadlights(scene, binding);
                weather.matchTime = binding;
                var picker = All<SportsWeatherPicker>(scene).FirstOrDefault(p => !p.timeOfDay);
                if (picker == null) picker = SportsWeatherPickerAuthoring.Install(weather.session, weather.presets, new Vector2(0, 336));
                InstallTimePicker(picker);
                if (collection && !scene.name.StartsWith("TruckKeepaway", StringComparison.Ordinal)) PolishCollectionSetup(binding, picker);
                else PolishSetup(scene, binding, picker);
                EditorUtility.SetDirty(binding); EditorUtility.SetDirty(weather);
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                output.WriteLine(path + ": explicit Weather Maker time binding; saved Heat weather/time pair; fixed directional fill superseded by vendor sun/moon.");
            }
            AssetDatabase.SaveAssets();
        }
        static void InstallHeadlights(Scene scene, SportsMatchTime binding)
        {
            // Hide & Seek owns tactical manual headlight/engine state. Its existing
            // NWH adapter and lighting input remain the only authority there.
            if (scene.name == "TruckHideAndSeek") return;
            var coordinator = binding.GetComponent<SportsNightHeadlights>();
            if (coordinator != null) { if (coordinator.layoutVersion < 1) DetachRecordedHierarchyLights(binding, coordinator); TuneOwnedHeadlights(coordinator); return; }
            var trucks = All<SportsEventTruck>(scene);
            coordinator = binding.gameObject.AddComponent<SportsNightHeadlights>(); coordinator.time = binding;
            coordinator.trucks = trucks; coordinator.wheelOnlyBeams = new Light[trucks.Length];
            for (int i = 0; i < trucks.Length; i++)
            {
                var truck = trucks[i];
                var manager = truck.tractorMotor != null && truck.tractorMotor.vehicle != null ? truck.tractorMotor.vehicle.effectsManager.lightsManager : null;
                if (manager != null && manager.lowBeamLights.lightSources.Any(source => source.light != null)) continue;
                int[] sides = manager != null ? new[] { -1, 1 } : new[] { 0 };
                foreach (int side in sides)
                {
                    var light = new GameObject("Authored night low-beam" + side).AddComponent<Light>(); light.transform.SetParent(truck.transform, false);
                    var bounds = AuthoredTruckBounds(truck); light.transform.localPosition = bounds.center + new Vector3(side * bounds.size.x * .3f, bounds.size.y * .2f, bounds.size.z * .49f);
                    light.type = LightType.Spot; light.range = Mathf.Clamp(bounds.size.z * 12, 25, 100); light.spotAngle = 80; light.innerSpotAngle = 60;
                    light.intensity = manager != null ? 1000 : 750; light.color = new Color(1, .93f, .76f); light.shadows = LightShadows.None; light.enabled = false;
                    if (manager != null) manager.lowBeamLights.lightSources.Add(new NWH.VehiclePhysics2.Effects.LightSource { type = NWH.VehiclePhysics2.Effects.LightSource.LightType.Light, light = light, emissionColor = light.color });
                    else coordinator.wheelOnlyBeams[i] = light;
                }
                if (manager != null) EditorUtility.SetDirty(truck.tractorMotor.vehicle);
            }
            DetachRecordedHierarchyLights(binding, coordinator);
            TuneOwnedHeadlights(coordinator);
            EditorUtility.SetDirty(coordinator);
        }
        static Bounds AuthoredTruckBounds(SportsEventTruck truck)
        {
            if (truck.shape != null) return new Bounds(truck.shape.center, truck.shape.size);
            // Curling deliberately uses a circular MeshCollider. Locate visual
            // lamps without replacing that sport's authored collision footprint.
            var renderers = truck.visuals.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException("Missing authored truck visuals: " + truck.name);
            var bounds = new Bounds(); bool initialized = false;
            foreach (var renderer in renderers)
            {
                var local = renderer.localBounds;
                foreach (int x in new[] { -1, 1 }) foreach (int y in new[] { -1, 1 }) foreach (int z in new[] { -1, 1 })
                {
                    var corner = local.center + Vector3.Scale(local.extents, new Vector3(x, y, z));
                    var point = truck.transform.InverseTransformPoint(renderer.transform.TransformPoint(corner));
                    if (!initialized) { bounds = new Bounds(point, Vector3.zero); initialized = true; }
                    else bounds.Encapsulate(point);
                }
            }
            return bounds;
        }
        static void TuneOwnedHeadlights(SportsNightHeadlights coordinator)
        {
            if (coordinator.layoutVersion >= 2) return;
            foreach (var beam in coordinator.beams)
            {
                beam.light.color = new Color(1, .98f, .93f);
                var truck = beam.target.GetComponent<SportsEventTruck>();
                beam.light.intensity = truck.tractorMotor != null ? 180 : 90;
                EditorUtility.SetDirty(beam.light);
            }
            coordinator.layoutVersion = 2; EditorUtility.SetDirty(coordinator);
        }
        static void DetachRecordedHierarchyLights(SportsMatchTime binding, SportsNightHeadlights coordinator)
        {
            var root = binding.transform.Find("Night vehicle light presentation");
            if (root == null) { root = new GameObject("Night vehicle light presentation").transform; root.SetParent(binding.transform, false); }
            var beams = new System.Collections.Generic.List<SportsNightHeadlights.Beam>();
            foreach (var truck in coordinator.trucks)
            foreach (var light in truck.GetComponentsInChildren<Light>(true).Where(light => light.name.StartsWith("Authored night low-beam", StringComparison.Ordinal)).ToArray())
            {
                beams.Add(new SportsNightHeadlights.Beam { target = truck.transform, light = light, position = truck.transform.InverseTransformPoint(light.transform.position), rotation = Quaternion.Inverse(truck.transform.rotation) * light.transform.rotation });
                light.transform.SetParent(root, true); EditorUtility.SetDirty(light.transform);
            }
            coordinator.beams = beams.ToArray(); coordinator.layoutVersion = 1; EditorUtility.SetDirty(coordinator);
        }
        static void PolishCollectionSetup(SportsMatchTime binding, SportsWeatherPicker weather)
        {
            if (binding.setupLayoutVersion >= 6) return;
            var parent = weather.transform.parent;
            var session = binding.GetComponent<SportsChaosWeather>().session;
            foreach (var action in parent.GetComponentsInChildren<SportsMenuAction>(true))
                if (action.action == "weather" || action.action == "chaosweather") { action.gameObject.SetActive(false); EditorUtility.SetDirty(action.gameObject); }
            if (session.TeamSport)
            {
                foreach (var action in parent.GetComponentsInChildren<SportsMenuAction>(true))
                {
                    var rect = action.GetComponent<RectTransform>();
                    if (action.action == "option") { rect.anchoredPosition = new Vector2(0, 285); rect.sizeDelta = new Vector2(1100, 34); session.menus.optionText.rectTransform.sizeDelta = new Vector2(1082, 26); EditorUtility.SetDirty(session.menus.optionText.rectTransform); EditorUtility.SetDirty(rect); }
                    if (action.action == "teamA" || action.action == "teamB") { rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, 235); EditorUtility.SetDirty(rect); }
                }
            }
            if (binding.gameObject.scene.name == "TruckBogSnorkelling")
            {
                // Original Bog authoring moved the caption instead of its button,
                // leaving a blank choice and putting its text at the screen edge.
                var caption = session.menus.optionText.rectTransform; caption.anchoredPosition = Vector2.zero;
                caption.sizeDelta = caption.parent.GetComponent<RectTransform>().sizeDelta - new Vector2(18, 8); EditorUtility.SetDirty(caption);
            }
            if (binding.setupLayoutVersion >= 4) { binding.setupLayoutVersion = 6; return; }
            var title = parent.Find("Title").GetComponent<TMPro.TMP_Text>();
            title.rectTransform.anchoredPosition = new Vector2(0, 430); title.rectTransform.sizeDelta = new Vector2(1350, 58);
            title.fontSizeMax = 44; EditorUtility.SetDirty(title); EditorUtility.SetDirty(title.rectTransform);
            var time = parent.GetComponentsInChildren<SportsWeatherPicker>(true).Single(picker => picker.timeOfDay);
            foreach (var picker in new[] { weather, time })
            {
                var rect = picker.GetComponent<RectTransform>(); rect.anchoredPosition = new Vector2(picker.timeOfDay ? 165 : -165, 337); rect.sizeDelta = new Vector2(300, 54); EditorUtility.SetDirty(rect);
            }
            foreach (string name in new[] { "Weather selection label", "Time of day selection label" })
            {
                var label = parent.Find(name).GetComponent<RectTransform>(); label.gameObject.SetActive(true); label.anchoredPosition = new Vector2(name.StartsWith("Time") ? 165 : -165, 381); label.sizeDelta = new Vector2(300, 30); EditorUtility.SetDirty(label); EditorUtility.SetDirty(label.gameObject);
            }
            var explanation = parent.Find("Weather selection explanation");
            if (explanation != null) { explanation.gameObject.SetActive(false); EditorUtility.SetDirty(explanation.gameObject); }
            foreach (var action in parent.GetComponentsInChildren<SportsMenuAction>(true))
            {
                if (action.action == "drivingview") { var rect = action.GetComponent<RectTransform>(); rect.anchoredPosition = new Vector2(0, -245); EditorUtility.SetDirty(rect); }
                if (action.action == "teamsetup") { var rect = action.GetComponent<RectTransform>(); rect.anchoredPosition = new Vector2(0, -225); EditorUtility.SetDirty(rect); }
            }
            time.dropdown.panelSize = 220; time.dropdown.enableScrollbar = true;
            var scroll = time.GetComponentInChildren<UnityEngine.UI.ScrollRect>(true);
            if (scroll != null) { scroll.verticalScrollbarVisibility = UnityEngine.UI.ScrollRect.ScrollbarVisibility.AutoHide; EditorUtility.SetDirty(scroll); }
            EditorUtility.SetDirty(time.dropdown); binding.setupLayoutVersion = 6;
        }
        static void BindSkyProfile(SportsChaosWeather weather, SportsMatchTime binding) => BindSkyProfile(weather.vendor, weather.gameObject, binding);
        static void BindSkyProfile(WeatherMakerScript vendor, GameObject owner, SportsMatchTime binding)
        {
            const string path = "Assets/TruckSports/Settings/Sports atmospheric sky.asset";
            var profile = AssetDatabase.LoadAssetAtPath<WeatherMakerSkyProfileScript>(path);
            if (profile == null)
            {
                var source = AssetDatabase.LoadAssetAtPath<WeatherMakerSkyProfileScript>("Assets/WeatherMaker/Prefab/Profiles/Sky/WeatherMakerSkyProfile_ProceduralMilkyWayPhysicallyBased.asset");
                profile = UnityEngine.Object.Instantiate(source);
                profile.name = "Sports atmospheric sky";
                var stars = AssetDatabase.LoadAssetAtPath<WeatherMakerSkyProfileScript>("Assets/WeatherMaker/Prefab/Profiles/Sky/WeatherMakerSkyProfile_ProceduralMilkyWay.asset");
                profile.NightTexture = stars.NightTexture;
                profile.NightIntensity = .65f;
                profile.NightTwinkleSpeed = 1.1f;
                profile.NightTwinkleVariance = .12f;
                profile.NightTwinkleRandomness = .25f;
                AssetDatabase.CreateAsset(profile, path);
            }
            var sphere = vendor.GetComponentInChildren<WeatherMakerSkySphereScript>(true);
            sphere.gameObject.SetActive(true); sphere.enabled = true;
            var mesh = sphere.GetComponent<MeshRenderer>(); if (mesh != null) { mesh.enabled = false; EditorUtility.SetDirty(mesh); }
            // Do not replace a designer's later sport-specific profile on rerun.
            string original = AssetDatabase.GetAssetPath(sphere.SkySphereProfile);
            if (original.StartsWith("Assets/WeatherMaker/", StringComparison.Ordinal)) sphere.SkySphereProfile = profile;
            var adapter = owner.GetComponent<SportsOrthographicSky>() ?? owner.AddComponent<SportsOrthographicSky>();
            adapter.weather = vendor; adapter.commands = vendor.GetComponentInChildren<WeatherMakerCommandBufferManagerScript>(true);
            adapter.sphere = sphere; adapter.sun = binding.perspectiveSun;
            EditorUtility.SetDirty(sphere); EditorUtility.SetDirty(adapter);
        }
        static void InstallVenueLamps(Scene scene, SportsMatchTime binding)
        {
            if (NewOutdoors.Contains(scene.name) || RemainingSharedOutdoors.Contains(scene.name)) { InstallAuditedOutdoorLamps(scene, binding); return; }
            bool stadium = scene.name == "TruckFieldGoal", arena = scene.name == "TruckKeepaway";
            if (!stadium && !arena) return;
            Transform parent = stadium ? All<LWS.TruckFieldGoal.FieldGoalStadiumBoard>(scene).Single().fieldAssembly : binding.transform;
            var existing = parent.Find("Authored night venue lamps");
            if (existing != null) return; // Preserve designer positions/intensities on subsequent explicit migrations.
            var root = new GameObject("Authored night venue lamps").transform;
            root.SetParent(parent, false);
            var lights = new System.Collections.Generic.List<Light>();
            foreach (int side in new[] { -1, 1 }) foreach (float z in stadium ? new[] { -90f, -20f } : new[] { -34f, 34f })
            {
                var pole = new GameObject("Venue lamp · " + side + " · " + z).transform; pole.SetParent(root, false);
                pole.localPosition = new Vector3(side * (stadium ? 60 : 61), 0, z); pole.localRotation = Quaternion.Euler(0, side < 0 ? 0 : 180, 0);
                LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(pole, "Assets/Toon Gas Station/Prefabs/Streetlight_1C.prefab", new Vector3(12, 26, 12), 0, false, scene.name);
                var head = new GameObject("Budgeted night floodlight").AddComponent<Light>(); head.transform.SetParent(pole, false); head.transform.localPosition = new Vector3(0, 24, 0);
                var target = root.TransformPoint(new Vector3(0, 0, z)); head.transform.rotation = Quaternion.LookRotation(target - head.transform.position);
                head.type = LightType.Spot; head.range = 140; head.spotAngle = 105; head.innerSpotAngle = 78;
                head.color = new Color(.86f, .92f, 1); head.intensity = stadium ? 6500 : 4800; head.shadows = LightShadows.None;
                lights.Add(head);
            }
            binding.nightLights = lights.ToArray();
        }
        static void InstallAuditedOutdoorLamps(Scene scene, SportsMatchTime binding)
        {
            if (binding.transform.Find("Authored night venue lamps") != null) return;
            if (scene.name == "TruckDemolitionDerby")
            {
                // Retain the existing stadium fixtures, positions and intensities.
                binding.nightLights = All<Light>(scene).Where(light => light.GetComponentInParent<WeatherMakerScript>() == null && (light.type == LightType.Spot || light.type == LightType.Point)).ToArray();
                return;
            }
            var root = new GameObject("Authored night venue lamps").transform; root.SetParent(binding.transform, false);
            var lights = new System.Collections.Generic.List<Light>();
            if (scene.name == "TruckDockDiving")
            {
                foreach (float z in new[] { -25f, 45f, 115f }) foreach (int side in new[] { -1, 1 }) AddLamp(new Vector3(side * 29, 0, z), new Vector3(0, 0, z), 22, 110, 1800);
            }
            else if (scene.name == "TruckCurling")
            {
                // Side fixtures illuminate the full extended sheet instead of
                // leaving its middle beyond four corner spotlights' coverage.
                var sheet = scene.GetRootGameObjects().Single(item => item.name == "Extra slippery curling sheet").GetComponent<MeshRenderer>().bounds;
                foreach (float fraction in new[] { .12f, .5f, .88f }) foreach (int side in new[] { -1, 1 })
                {
                    float z = Mathf.Lerp(sheet.min.z, sheet.max.z, fraction);
                    AddLamp(new Vector3(side * 16, sheet.max.y, z), new Vector3(0, sheet.max.y, z), 22, 115, 2000);
                }
            }
            else if (scene.name == "TruckHockey" || scene.name == "TruckWallball")
            {
                string floorName = scene.name == "TruckHockey" ? "Arena floor" : "Court floor";
                var floor = All<MeshRenderer>(scene).Single(renderer => renderer.name == floorName).bounds;
                foreach (int side in new[] { -1, 1 }) foreach (int end in new[] { -1, 1 })
                    AddLamp(new Vector3(floor.center.x + side * (floor.extents.x - 1), floor.max.y, floor.center.z + end * (floor.extents.z - 1)), new Vector3(floor.center.x, floor.max.y, floor.center.z), 12, 60, 1000);
            }
            else
            {
                string anchor = scene.name == "TruckBuzkashi" ? "Broad physical grass field" : scene.name == "TruckBungeeDemolition" ? "Independent demolition bay" : scene.name == "TruckFishing" ? "Equivalent independent river" : scene.name == "TruckSumo" ? "Surrounding arena floor" : scene.name == "TruckLogRolling" ? "Installed NWH pond mesh" : scene.name == "TruckCurling" ? "Extra slippery curling sheet" : scene.name == "TruckSkeeBall" ? "Equivalent giant arcade Skee course" : "Structural arena rubber floor";
                var anchors = scene.GetRootGameObjects().Where(item => item.name.StartsWith(anchor, StringComparison.Ordinal)).ToArray();
                if (anchors.Length == 0) throw new InvalidOperationException("Missing audited lamp boundary: " + scene.name);
                foreach (var item in anchors)
                {
                    var renderers = item.GetComponentsInChildren<MeshRenderer>(true).Where(renderer => renderer.bounds.size.sqrMagnitude > 1).ToArray();
                    if (renderers.Length == 0) throw new InvalidOperationException("Missing physical venue bounds: " + item.name);
                    Bounds bounds = renderers[0].bounds; foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
                    float height = Mathf.Clamp(Mathf.Max(bounds.extents.x, bounds.extents.z) * .55f, 14, 30);
                    float range = Mathf.Clamp(bounds.extents.magnitude * 2.2f + height, 50, 240);
                    float power = Mathf.Clamp(height * height * 6, 1500, 5400);
                    // Assembly bounds include towers and trees. Their top is
                    // not the ground. These audited venue roots author the
                    // floor elevation; flat sheet/pit anchors use their surface.
                    float ground = scene.name == "TruckSkeeBall" ? item.transform.Find("Course miss floor").GetComponent<MeshRenderer>().bounds.max.y : scene.name == "TruckBungeeDemolition" || scene.name == "TruckFishing" ? item.transform.position.y : bounds.max.y;
                    // Fixtures sit outside the authored footprint. No new physics
                    // geometry, supports, or colliders enter the playing routes.
                    foreach (int x in new[] { -1, 1 }) foreach (int z in new[] { -1, 1 })
                        AddLamp(new Vector3(bounds.center.x + x * (bounds.extents.x + 3), ground, bounds.center.z + z * (bounds.extents.z + 3)), new Vector3(bounds.center.x, ground, bounds.center.z), height, range, power);
                }
            }
            binding.nightLights = lights.ToArray();
            void AddLamp(Vector3 position, Vector3 target, float height, float range, float power)
            {
                var pole = new GameObject("Venue night fixture " + lights.Count).transform; pole.SetParent(root, false); pole.position = position;
                LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(pole, "Assets/Toon Gas Station/Prefabs/Streetlight_1C.prefab", new Vector3(5, height, 5), 0, false, scene.name);
                var head = new GameObject("Budgeted venue night light").AddComponent<Light>(); head.transform.SetParent(pole, false); head.transform.localPosition = Vector3.up * (height - 1);
                head.transform.rotation = Quaternion.LookRotation(target - head.transform.position); head.type = LightType.Spot; head.range = range; head.spotAngle = 108; head.innerSpotAngle = 76;
                head.color = new Color(1, .94f, .82f); head.intensity = power; head.shadows = LightShadows.None; lights.Add(head);
            }
        }
        static void PolishSetup(Scene scene, SportsMatchTime binding, SportsWeatherPicker weather)
        {
            if (binding.setupLayoutVersion >= 3) return;
            var parent = weather.transform.parent;
            var time = parent.GetComponentsInChildren<SportsWeatherPicker>(true).Single(picker => picker.timeOfDay);
            // Heat's disable-scrollbar branch destroys its Scrollbar component,
            // while the installed SmoothScrollbar requires that component. Keep
            // both vendor components and use uGUI's supported visibility policy.
            if (binding.setupLayoutVersion < 2)
            {
                time.dropdown.panelSize = 220; time.dropdown.enableScrollbar = true;
                var scroll = time.GetComponentInChildren<UnityEngine.UI.ScrollRect>(true);
                if (scroll != null) { scroll.verticalScrollbarVisibility = UnityEngine.UI.ScrollRect.ScrollbarVisibility.AutoHide; EditorUtility.SetDirty(scroll); }
                EditorUtility.SetDirty(time.dropdown);
            }
            if (scene.name.StartsWith("TruckKeepaway", StringComparison.Ordinal) && binding.setupLayoutVersion < 1)
            {
                // The preserved Keepaway setup has a custom title/options layout.
                // Use its spare header row instead of the ordinary setup coordinates.
                foreach (var action in parent.GetComponentsInChildren<SportsMenuAction>(true))
                {
                    if (action.action == "chaosweather") { action.gameObject.SetActive(false); EditorUtility.SetDirty(action.gameObject); }
                    if (action.action != "chaos" && action.action != "seed") continue;
                    var rect = action.GetComponent<RectTransform>(); rect.anchoredPosition = new Vector2(action.action == "chaos" ? -510 : 510, 200); rect.sizeDelta = new Vector2(310, 46); EditorUtility.SetDirty(rect);
                }
                foreach (var picker in new[] { weather, time })
                {
                    var rect = picker.GetComponent<RectTransform>(); rect.anchoredPosition = new Vector2(picker.timeOfDay ? 170 : -170, 200); rect.sizeDelta = new Vector2(310, 54); EditorUtility.SetDirty(rect);
                }
                foreach (string name in new[] { "Weather selection label", "Time of day selection label" })
                {
                    var label = parent.Find(name).GetComponent<RectTransform>(); label.anchoredPosition = new Vector2(name.StartsWith("Time") ? 170 : -170, 244); label.sizeDelta = new Vector2(310, 30); EditorUtility.SetDirty(label);
                }
                var explanation = parent.Find("Weather selection explanation");
                if (explanation != null) { explanation.gameObject.SetActive(false); EditorUtility.SetDirty(explanation.gameObject); }
            }
            if ((scene.name == "TruckSnake" || scene.name == "TruckSkyTow") && binding.setupLayoutVersion < 3)
            {
                // These four-participant layouts reserve the left column for the crew;
                // their upper row already contains the title and/or camera selector.
                foreach (var picker in new[] { weather, time })
                {
                    var rect = picker.GetComponent<RectTransform>(); rect.anchoredPosition = new Vector2(picker.timeOfDay ? 525 : 195, 145); rect.sizeDelta = new Vector2(300, 54); EditorUtility.SetDirty(rect);
                }
                foreach (string name in new[] { "Weather selection label", "Time of day selection label" })
                {
                    var label = parent.Find(name).GetComponent<RectTransform>(); label.gameObject.SetActive(true); label.anchoredPosition = new Vector2(name.StartsWith("Time") ? 525 : 195, 189); label.sizeDelta = new Vector2(300, 30); EditorUtility.SetDirty(label); EditorUtility.SetDirty(label.gameObject);
                }
                var explanation = parent.Find("Weather selection explanation");
                if (explanation != null) { explanation.gameObject.SetActive(false); EditorUtility.SetDirty(explanation.gameObject); }
            }
            binding.setupLayoutVersion = 3;
        }
        static void BindThreeDimensionalSky(SportsChaosWeather weather, SportsMatchTime binding) => BindThreeDimensionalSky(weather.vendor, binding);
        static void BindThreeDimensionalSky(WeatherMakerScript vendor, SportsMatchTime binding)
        {
            // These are 3D worlds even when the sport uses an orthographic arena lens.
            // The installed 2D prefab documents separate perspective/orthographic sun entries.
            // Retain one world-lighting sun; the second entry is lookup-only (culling mask 0).
            vendor.CameraType = CameraMode.Perspective;
            binding.perspectiveSun = vendor.GetComponentsInChildren<WeatherMakerCelestialObjectScript>(true).First(sun => sun.IsSun && sun.OrbitTypeIsPerspective);
            var proxy = vendor.transform.Find("Sports orthographic sun lookup");
            if (proxy == null)
            {
                var root = new GameObject("Sports orthographic sun lookup", typeof(Light)); root.transform.SetParent(vendor.transform, false);
                var light = root.GetComponent<Light>(); light.type = LightType.Directional; light.cullingMask = 0; light.shadows = LightShadows.None;
                var sun = root.AddComponent<WeatherMakerCelestialObjectScript>(); sun.IsSun = true; sun.OrbitType = WeatherMakerOrbitType.CustomOrthographic; sun.LightMode = WeatherMakerCelestialObjectLightMode.None;
                sun.LightColor = binding.perspectiveSun.LightColor; sun.LightPower = binding.perspectiveSun.LightPower; sun.LightMultiplier = binding.perspectiveSun.LightMultiplier;
                proxy = root.transform;
            }
            binding.orthographicSun = proxy.GetComponent<WeatherMakerCelestialObjectScript>();
            EditorUtility.SetDirty(vendor);
        }
        static void InstallTimePicker(SportsWeatherPicker weather)
        {
            var parent = weather.transform.parent;
            if (parent.GetComponentsInChildren<SportsWeatherPicker>(true).Any(p => p.timeOfDay)) return;
            var rect = weather.GetComponent<RectTransform>(); var original = rect.anchoredPosition;
            rect.sizeDelta = new Vector2(300, 54); rect.anchoredPosition = original + new Vector2(-165, 0);
            var clone = UnityEngine.Object.Instantiate(weather.gameObject, parent, false); clone.name = "Time of day · Weather Maker selector";
            var time = clone.GetComponent<SportsWeatherPicker>(); time.timeOfDay = true;
            var timeRect = clone.GetComponent<RectTransform>(); timeRect.anchoredPosition = original + new Vector2(165, 0);
            time.dropdown.items.Clear(); foreach (string name in Enum.GetNames(typeof(SportsTimeChoice))) time.dropdown.items.Add(new Michsky.UI.Heat.Dropdown.Item { itemName = name });
            time.dropdown.selectedItemIndex = (int)SportsTimeChoice.Day;
            var label = parent.Find("Weather selection label");
            if (label != null)
            {
                var text = label.GetComponent<TMPro.TMP_Text>(); text.rectTransform.sizeDelta = new Vector2(300, 30); text.rectTransform.anchoredPosition = original + new Vector2(-165, 44);
                var title = UnityEngine.Object.Instantiate(label.gameObject, parent, false); title.name = "Time of day selection label";
                var titleText = title.GetComponent<TMPro.TMP_Text>(); titleText.text = "TIME OF DAY"; titleText.rectTransform.anchoredPosition = original + new Vector2(165, 44);
                EditorUtility.SetDirty(text); EditorUtility.SetDirty(text.rectTransform);
            }
            var explanation = parent.Find("Weather selection explanation");
            if (explanation != null) { var text = explanation.GetComponent<TMPro.TMP_Text>(); text.text = "Independent forecast and time. Random rolls once per match."; text.fontSize = 20; EditorUtility.SetDirty(text); }
            EditorUtility.SetDirty(weather); EditorUtility.SetDirty(rect); EditorUtility.SetDirty(time); EditorUtility.SetDirty(time.dropdown);
        }
    }
}
