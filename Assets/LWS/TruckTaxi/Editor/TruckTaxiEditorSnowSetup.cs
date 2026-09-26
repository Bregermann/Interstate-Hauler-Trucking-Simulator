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
            string[] names = { "Hidden/NOT_Lonely/NL_GaussianBlur", "Hidden/NOT_Lonely/Weatherade/NL_TexturePacking",
                "Hidden/NOT_Lonely/Weatherade/NL_TraceMaskGen", "Hidden/NOT_Lonely/Weatherade/DepthRenderer" };
            shaders.arraySize = names.Length;
            for (int i = 0; i < names.Length; i++)
            {
                var shader = Shader.Find(names[i]);
                if (shader == null) throw new InvalidOperationException("Required Weatherade shader not installed: " + names[i]);
                shaders.GetArrayElementAtIndex(i).objectReferenceValue = shader;
            }
            snow.ApplyModifiedProperties();
            Debug.Log($"TAXI SNOW: weights, Weatherade and bounded runtime snow configured; {markedRoads} existing road renderers marked without mesh regeneration.", host);
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
