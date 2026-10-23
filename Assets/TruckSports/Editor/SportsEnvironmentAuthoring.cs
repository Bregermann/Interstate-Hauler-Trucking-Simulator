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
    public static class SportsEnvironmentAuthoring
    {
        static readonly string[] Representative = { "TruckKeepaway", "TruckFieldGoal", "TruckSkyTow", "TruckTerrierRacing", "TruckSnake", "TruckPoker" };
        static T[] All<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
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
