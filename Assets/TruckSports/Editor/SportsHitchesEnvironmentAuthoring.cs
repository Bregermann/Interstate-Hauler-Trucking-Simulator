using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using DigitalRuby.WeatherMaker;

namespace LWS.TruckSports.Editor
{
    // Explicit migration of the 22 new saved venues only. No runtime/build regeneration.
    public static class SportsHitchesEnvironmentAuthoring
    {
        public static void RepairNewVenues()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stopped saved scenes required.");
            var catalog = SportsWeatherPresetsAuthoring.EnsureCatalog();
            foreach (var entry in SportsHitchesGames.All)
            {
                var scene = EditorSceneManager.OpenScene(entry.ScenePath);
                var match = UnityEngine.Object.FindAnyObjectByType<SportsEventSession>();
                if (match == null) throw new InvalidOperationException(entry.scene + " has no match authority.");
                string folder = entry.ScenePath.Substring(7, entry.ScenePath.IndexOf("/Scenes/", StringComparison.Ordinal) - 7);
                if (match is LWS.TruckWaterSports.WaterMatch)
                    LWS.TruckWaterSports.Editor.WaterSportsAuthoring.RepairActiveVesselPayloads();
                if (entry.scene == "Trailerblazers")
                    LWS.TruckHitchSports.Editor.CoreHitchSportsAuthoring.RepairActiveRocketBoosterClearance();
                if (entry.scene == "ZeroGHitchUp") { EditorSceneManager.SaveScene(scene); continue; }
                if (match is LWS.TruckWaterSports.BlackwaterMatch && match.presentationWeather == null)
                {
                    match.presentationWeather = SportsChaosAuthoring.Weather(match, folder);
                    SportsWeatherRenderingAuthoring.Install(match, folder);
                    var views = match.GetComponent<SportsRallyViews>(); if (views != null) views.weather = match.presentationWeather;
                    SportsSnowAuthoring.InstallScene();
                }
                var horror = match as LWS.TruckHorrorSports.HorrorMatch;
                var weather = horror != null ? horror.atmosphere : match.presentationWeather;
                if (weather == null) throw new InvalidOperationException(entry.scene + " requires its shared weather authority.");
                weather.presets = catalog; EditorUtility.SetDirty(weather);
                bool narrativeNight = horror != null || match is LWS.TruckWaterSports.BlackwaterMatch;
                if (narrativeNight)
                {
                    var cycle = weather.vendor.GetComponentInChildren<WeatherMakerDayNightCycleManagerScript>(true);
                    string clockPath = "Assets/" + folder + "/Materials/Authored midnight clock.asset";
                    var night = AssetDatabase.LoadAssetAtPath<WeatherMakerDayNightCycleProfileScript>(clockPath);
                    if (night == null) { night = UnityEngine.Object.Instantiate(cycle.DayNightProfile); night.TimeOfDay = 0; night.Speed = night.NightSpeed = 0; AssetDatabase.CreateAsset(night, clockPath); }
                    cycle.DayNightProfile = night; EditorUtility.SetDirty(cycle);
                    SportsWeatherPickerAuthoring.Install(match, catalog, new Vector2(0, 336));
                    SportsWeatherRenderingAuthoring.UseViewportSkybox(weather.gameObject,
                        scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Camera>(true)).Where(c => c.cameraType == CameraType.Game).ToArray(), folder);
                }
                else
                    SportsEnvironmentAuthoring.BindAuthoredOutdoorSession(match, scene.name,
                        scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Light>(true)).Where(l => l.type != LightType.Directional && l.GetComponentInParent<WeatherMakerScript>() == null).ToArray(), folder);
                EnsureDepthRenderer(match);
                EditorUtility.SetDirty(match); EditorSceneManager.SaveScene(scene);
                Debug.Log("HH10_ENVIRONMENT_BOUND " + entry.id + " " + entry.scene);
            }
            AssetDatabase.SaveAssets();
        }
        static void EnsureDepthRenderer(SportsEventSession match)
        {
            var snow = match.GetComponent<SportsSnowSurface>(); if (snow == null) return;
            var rendering = match.GetComponent<LWS.TruckBaseJumping.BaseJumpRendering>();
            if (rendering == null || rendering.pipeline == null) throw new InvalidOperationException("Owned rendering pipeline required.");
            string path = AssetDatabase.GetAssetPath(rendering.pipeline);
            if (!(path.StartsWith("Assets/TruckWaterSports/") || path.StartsWith("Assets/TruckHorrorSports/") || path.StartsWith("Assets/TruckHitchSports/") || path.StartsWith("Assets/TruckExtremeHauling/") || path == "Assets/TruckSports/Rendering/" + match.gameObject.scene.name + " snow pipeline.asset"))
                throw new InvalidOperationException("Refusing to alter a shared/vendor pipeline: " + path);
            var depth = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>("Assets/NOT_Lonely/Weatherade SRS/Resources/SRS_DepthRenderer.asset");
            if (depth == null) throw new InvalidOperationException("Installed Weatherade depth renderer required.");
            var settings = new SerializedObject(rendering.pipeline); var list = settings.FindProperty("m_RendererDataList");
            int index = -1; for (int i = 0; i < list.arraySize; i++) if (list.GetArrayElementAtIndex(i).objectReferenceValue == depth) index = i;
            if (index < 0) { index = list.arraySize++; list.GetArrayElementAtIndex(index).objectReferenceValue = depth; }
            settings.ApplyModifiedPropertiesWithoutUndo(); snow.depthRenderer = index;
            EditorUtility.SetDirty(rendering.pipeline); EditorUtility.SetDirty(snow);
        }
    }
}
