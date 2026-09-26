using System;
using System.Collections.Generic;
using LWS.InterstateHauler;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LWS.TruckTaxi.Editor
{
    public static class TruckTaxiEditorSnowSetup
    {
        [MenuItem("Truck Taxi/Migrate Snow Rates")]
        public static void MigrateSnowSettingsAsset()
        {
            var settings = AssetDatabase.LoadAssetAtPath<TruckTaxiEnvironmentSettings>(TruckTaxiEnvironmentSetup.SettingsPath);
            if (settings == null) throw new InvalidOperationException("Taxi environment settings asset is missing: " + TruckTaxiEnvironmentSetup.SettingsPath);
            if (MigrateSnowRates(settings))
            {
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
            }
            Debug.Log($"TAXI SNOW: settings now target {settings.maximumSnowDepthMeters:0.00}m in roughly 90s light, 35s heavy, 25s blizzard when using sandbox defaults.", settings);
        }

        public static bool MigrateSnowRates(TruckTaxiEnvironmentSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            bool changed = false;
            float maximum = Mathf.Max(.1f, settings.maximumSnowDepthMeters);
            if (Mathf.Approximately(settings.lightSnowMetersPerSecond, .0005f))
            { settings.lightSnowMetersPerSecond = maximum / 90f; changed = true; }
            if (Mathf.Approximately(settings.heavySnowMetersPerSecond, .002f))
            { settings.heavySnowMetersPerSecond = maximum / 35f; changed = true; }
            if (Mathf.Approximately(settings.blizzardMetersPerSecond, .005f))
            { settings.blizzardMetersPerSecond = maximum / 25f; changed = true; }
            if (settings.snowMeltMetersPerSecond <= 0)
            { settings.snowMeltMetersPerSecond = .01f; changed = true; }
            if (settings.tireCompressionMetersPerMeter <= 0)
            { settings.tireCompressionMetersPerMeter = .002f; changed = true; }
            return changed;
        }

        [MenuItem("Truck Taxi/Configure Snow")]
        public static void ConfigureDemoScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode before editing Taxi snow setup.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(TruckTaxiDemoBuilder.ScenePath);
            var host = Object.FindFirstObjectByType<TruckTaxiBootstrap>();
            ConfigureScene(host);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        public static void ConfigureScene(TruckTaxiBootstrap host)
        {
            if (host == null) throw new InvalidOperationException("Taxi bootstrap is required.");
            var environment = host.GetComponent<TruckTaxiEnvironmentCoordinator>();
            if (environment == null) throw new InvalidOperationException("Configure the existing Taxi environment before adding snow.");
            var settings = environment.settings;
            if (settings == null)
            {
                settings = AssetDatabase.LoadAssetAtPath<TruckTaxiEnvironmentSettings>(TruckTaxiEnvironmentSetup.SettingsPath);
                if (settings == null)
                {
                    settings = ScriptableObject.CreateInstance<TruckTaxiEnvironmentSettings>();
                    AssetDatabase.CreateAsset(settings, TruckTaxiEnvironmentSetup.SettingsPath);
                }
                Undo.RecordObject(environment, "Assign Taxi snow settings");
                environment.settings = settings;
                EditorUtility.SetDirty(environment);
            }
            Undo.RecordObject(settings, "Enable Taxi snow weights");
            var weights = new List<TruckTaxiWeatherWeight>(settings.weatherWeights ?? Array.Empty<TruckTaxiWeatherWeight>());
            EnsureWeight(weights, LwsWeatherPresetCatalog.LightSnowId, 1);
            EnsureWeight(weights, LwsWeatherPresetCatalog.HeavySnowId, .5f);
            EnsureWeight(weights, TruckTaxiSnow.BlizzardId, .2f);
            settings.weatherWeights = weights.ToArray();
            if (settings.maximumSnowDepthMeters <= 0) settings.maximumSnowDepthMeters = .6f;
            if (settings.maximumSnowCells <= 0) settings.maximumSnowCells = 4096;
            MigrateSnowRates(settings);
            EditorUtility.SetDirty(settings);

            EnsureSingle<LwsRoadConditionRuntimeController>(host);
            EnsureSingle<LwsWeatheradeAdapter>(host);
            int markedRoads = 0;
            foreach (var road in Object.FindObjectsByType<TruckTaxiSurface>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (road.gameObject.scene != host.gameObject.scene || !road.isRoad || road.GetComponent<Renderer>() == null) continue;
                if (road.GetComponent<LwsRoadSurface>() == null)
                {
                    var marker = Undo.AddComponent<LwsRoadSurface>(road.gameObject);
                    marker.Configure("taxi.city", "taxi.snow." + road.gameObject.name, LwsRoadSurfaceType.AsphaltInterstate, road.gameObject.name);
                    EditorUtility.SetDirty(marker);
                }
                markedRoads++;
            }
            if (host.GetComponent<TruckTaxiSnow>() == null) Undo.AddComponent<TruckTaxiSnow>(host.gameObject);
            // Weatherade creates these materials by Shader.Find; retain explicit player-build dependencies.
            var snow = new SerializedObject(host.GetComponent<TruckTaxiSnow>());
            var shaders = snow.FindProperty("runtimeCoverageShaders");
            string[] names = { "NOT_Lonely/Weatherade/Snow Coverage",
                "Hidden/NOT_Lonely/NL_GaussianBlur", "Hidden/NOT_Lonely/Weatherade/NL_TexturePacking",
                "Hidden/NOT_Lonely/Weatherade/NL_TraceMaskGen", "Hidden/NOT_Lonely/Weatherade/DepthRenderer",
                "NOT_Lonely/Weatherade/Extra/NL_DepthOccluder" };
            shaders.arraySize = names.Length;
            for (int i = 0; i < names.Length; i++)
            {
                var shader = Shader.Find(names[i]);
                if (shader == null) throw new InvalidOperationException("Required Weatherade shader not installed: " + names[i]);
                shaders.GetArrayElementAtIndex(i).objectReferenceValue = shader;
            }
            AssignVendorTexture(snow, "weatheradeSnowTexture", "Snow_01_n_h_sm.tif");
            AssignVendorTexture(snow, "weatheradeSnowDetailTexture", "SnowDetail.tif");
            AssignVendorTexture(snow, "weatheradeSnowSparkleTexture", "SparkleMask.tif");
            snow.FindProperty("weatheradeDepthRendererIndex").intValue = ResolveDepthRendererIndex(settings);
            snow.ApplyModifiedProperties();
            Debug.Log($"TAXI SNOW: weights, Weatherade and hidden gameplay depth configured; {markedRoads} existing road renderers marked without snow mesh generation.", host);
        }

        private static void AssignVendorTexture(SerializedObject snow, string propertyName, string fileName)
        {
            string path = "Assets/NOT_Lonely/Weatherade SRS/Textures/" + fileName;
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null) throw new InvalidOperationException("Required Weatherade snow texture not installed: " + path);
            snow.FindProperty(propertyName).objectReferenceValue = texture;
        }

        private static int ResolveDepthRendererIndex(TruckTaxiEnvironmentSettings settings)
        {
            if (settings.weatherRenderPipeline == null) throw new InvalidOperationException("Taxi weather render pipeline is not configured.");
            var depthRenderer = AssetDatabase.LoadMainAssetAtPath("Assets/NOT_Lonely/Weatherade SRS/Resources/SRS_DepthRenderer.asset");
            if (depthRenderer == null) throw new InvalidOperationException("Weatherade SRS depth renderer asset is missing.");
            var pipeline = new SerializedObject(settings.weatherRenderPipeline);
            var renderers = pipeline.FindProperty("m_RendererDataList");
            if (renderers == null) throw new InvalidOperationException("Taxi render pipeline has no renderer list.");
            for (int i = 0; i < renderers.arraySize; i++)
                if (renderers.GetArrayElementAtIndex(i).objectReferenceValue == depthRenderer) return i;
            throw new InvalidOperationException("Taxi render pipeline does not include the installed Weatherade SRS depth renderer.");
        }

        private static void EnsureWeight(List<TruckTaxiWeatherWeight> weights, string id, float weight)
        {
            foreach (var existing in weights) if (existing != null && existing.presetId == id) return;
            weights.Add(new TruckTaxiWeatherWeight(id, weight));
        }

        private static void EnsureSingle<T>(TruckTaxiBootstrap host) where T : Component
        {
            T found = null;
            foreach (var component in Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (component.gameObject.scene != host.gameObject.scene) continue;
                if (found != null) throw new InvalidOperationException("More than one Taxi scene " + typeof(T).Name + " exists.");
                found = component;
            }
            if (found == null) Undo.AddComponent<T>(host.gameObject);
        }
    }
}
