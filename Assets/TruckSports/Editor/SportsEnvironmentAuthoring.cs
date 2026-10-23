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
        public static void InstallRepresentative()
        {
            Directory.CreateDirectory("Logs/TruckSports/Environment");
            using (var output = new StreamWriter("Logs/TruckSports/Environment/representative-install.txt"))
            foreach (string path in SportsBuilder.SceneList().Where(path => Representative.Contains(Path.GetFileNameWithoutExtension(path))))
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var weather = All<SportsChaosWeather>(scene).SingleOrDefault();
                if (weather == null) { output.WriteLine(path + ": indoor venue, no Weather Maker world; original lighting retained without an inert selector."); continue; }
                var binding = weather.GetComponent<SportsMatchTime>() ?? weather.gameObject.AddComponent<SportsMatchTime>();
                if (weather.presets == null) weather.presets = SportsWeatherPresetsAuthoring.EnsureCatalog();
                binding.cycle = weather.vendor.GetComponentInChildren<WeatherMakerDayNightCycleManagerScript>(true);
                BindThreeDimensionalSky(weather, binding);
                binding.daylightFill = All<Light>(scene).SingleOrDefault(light => light.type == LightType.Directional && light.GetComponentInParent<WeatherMakerScript>() == null);
                var cameras = All<SportsEventCamera>(scene).Select(rig => rig.view).Where(camera => camera != null).Distinct().ToArray();
                SportsWeatherRenderingAuthoring.UseViewportSkybox(weather.gameObject, cameras, Path.GetFileNameWithoutExtension(path));
                InstallVenueLamps(scene, binding);
                weather.matchTime = binding;
                var picker = All<SportsWeatherPicker>(scene).FirstOrDefault(p => !p.timeOfDay);
                if (picker == null) picker = SportsWeatherPickerAuthoring.Install(weather.session, SportsWeatherPresetsAuthoring.EnsureCatalog(), new Vector2(0, 336));
                InstallTimePicker(picker);
                PolishSetup(scene, binding, picker);
                EditorUtility.SetDirty(binding); EditorUtility.SetDirty(weather);
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                output.WriteLine(path + ": explicit Weather Maker time binding; saved Heat weather/time pair; fixed directional fill superseded by vendor sun/moon.");
            }
            AssetDatabase.SaveAssets();
        }
        static void InstallVenueLamps(Scene scene, SportsMatchTime binding)
        {
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
            if (scene.name == "TruckKeepaway" && binding.setupLayoutVersion < 1)
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
        static void BindThreeDimensionalSky(SportsChaosWeather weather, SportsMatchTime binding)
        {
            // These are 3D worlds even when the sport uses an orthographic arena lens.
            // The installed 2D prefab documents separate perspective/orthographic sun entries.
            // Retain one world-lighting sun; the second entry is lookup-only (culling mask 0).
            weather.vendor.CameraType = CameraMode.Perspective;
            binding.perspectiveSun = weather.vendor.GetComponentsInChildren<WeatherMakerCelestialObjectScript>(true).First(sun => sun.IsSun && sun.OrbitTypeIsPerspective);
            var proxy = weather.vendor.transform.Find("Sports orthographic sun lookup");
            if (proxy == null)
            {
                var root = new GameObject("Sports orthographic sun lookup", typeof(Light)); root.transform.SetParent(weather.vendor.transform, false);
                var light = root.GetComponent<Light>(); light.type = LightType.Directional; light.cullingMask = 0; light.shadows = LightShadows.None;
                var sun = root.AddComponent<WeatherMakerCelestialObjectScript>(); sun.IsSun = true; sun.OrbitType = WeatherMakerOrbitType.CustomOrthographic; sun.LightMode = WeatherMakerCelestialObjectLightMode.None;
                sun.LightColor = binding.perspectiveSun.LightColor; sun.LightPower = binding.perspectiveSun.LightPower; sun.LightMultiplier = binding.perspectiveSun.LightMultiplier;
                proxy = root.transform;
            }
            binding.orthographicSun = proxy.GetComponent<WeatherMakerCelestialObjectScript>();
            EditorUtility.SetDirty(weather.vendor);
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
