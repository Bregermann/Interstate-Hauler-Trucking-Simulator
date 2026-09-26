using System;
using System.Linq;
using LWS.InterstateHauler;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LWS.TruckTaxi.Editor
{
    public static class TruckTaxiEnvironmentSetup
    {
        public const string SettingsPath = "Assets/LWS/TruckTaxi/ScriptableObjects/TruckTaxi_EnvironmentSettings.asset";
        public const string WeatherPrefabPath = "Assets/LWS/TruckTaxi/Prefabs/TruckTaxi_WeatherMaker.prefab";
        public const string WeatherResourcesPath = "Assets/LWS/TruckTaxi/ScriptableObjects/TruckTaxi_WeatherResources.asset";

        // Narrow build repair: updates only Taxi-owned weather assets, without opening a scene.
        public static void ConfigureWeatherResources()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode before changing weather assets.");
            EnsureTaxiWeatherPrefab(); AssetDatabase.SaveAssets();
            var waterShaders = AssetDatabase.GetDependencies(WeatherPrefabPath, true)
                .Where(path => path.EndsWith(".shader", StringComparison.OrdinalIgnoreCase) &&
                    path.IndexOf("Water", StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
            if (waterShaders.Length > 0) throw new InvalidOperationException("Taxi weather still depends on unused water shaders: " + string.Join(", ", waterShaders));
            Debug.Log("TAXI WEATHER: project-owned resource container excludes unused water profiles and water shaders.");
        }

        [MenuItem("Truck Taxi/Update Time Weather And Driver Needs")]
        public static void UpdateScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode before changing authored Taxi content.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(TruckTaxiDemoBuilder.ScenePath);
            var host = Object.FindFirstObjectByType<TruckTaxiBootstrap>();
            ConfigureScene(host);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("TAXI ENVIRONMENT: LWS clock + Weather Maker, two authored restroom bays, scenic bonus, driver-needs configuration. No city regeneration.");
        }
        // The main builder can call this on its already-open scene; this never opens or saves scenes itself.
        public static void ConfigureScene(TruckTaxiBootstrap host)
        {
            if (host == null) throw new InvalidOperationException("Truck Taxi bootstrap is required.");
            // Weather Maker deliberately skips its generated sky when a camera uses an
            // assigned Unity skybox. Taxi's inherited default skybox stays bright at night.
            var previousScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!UnityEngine.SceneManagement.SceneManager.SetActiveScene(host.gameObject.scene))
                throw new InvalidOperationException("Taxi scene must be loaded before configuring its weather sky.");
            try { RenderSettings.skybox = null; }
            finally { UnityEngine.SceneManagement.SceneManager.SetActiveScene(previousScene); }
            var settings = AssetDatabase.LoadAssetAtPath<TruckTaxiEnvironmentSettings>(SettingsPath);
            if (settings == null) { settings = ScriptableObject.CreateInstance<TruckTaxiEnvironmentSettings>(); AssetDatabase.CreateAsset(settings, SettingsPath); }
            if (settings.weatherRenderPipeline == null) settings.weatherRenderPipeline = EnsureWeatherRenderer();
            EditorUtility.SetDirty(settings);
            var environment = host.GetComponent<TruckTaxiEnvironmentCoordinator>() ?? host.gameObject.AddComponent<TruckTaxiEnvironmentCoordinator>();
            environment.settings = settings;
            var existing = Object.FindObjectsByType<LwsWeatherMakerAdapter>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(a => a.gameObject.scene == host.gameObject.scene).ToArray();
            if (existing.Length > 1) throw new InvalidOperationException("More than one existing weather adapter in Taxi scene; resolve this before adding environment integration.");
            if (existing.Length == 1) environment.weatherAdapter = existing[0];
            else
            {
                var go = new GameObject("Taxi Weather Maker Adapter"); go.transform.SetParent(host.transform, false);
                environment.weatherAdapter = go.AddComponent<LwsWeatherMakerAdapter>();
            }
            environment.weatherAdapter.ConfigureWeatherMakerPrefab(EnsureTaxiWeatherPrefab());
            var data = new SerializedObject(environment.weatherAdapter);
            data.FindProperty("runtimeInstanceName").stringValue = "Taxi Weather Maker Runtime";
            data.FindProperty("defaultQualityTier").enumValueIndex = (int)LwsRenderQualityTier.Medium;
            data.ApplyModifiedPropertiesWithoutUndo();
            // Also migrate a scene configured by the earlier setup revision. Bootstrap enables
            // the adapter only after the scene-local pipeline and current player camera exist.
            environment.weatherAdapter.enabled = false;
            EditorUtility.SetDirty(environment.weatherAdapter);
            if (host.GetComponent<TruckTaxiDriverNeedsCoordinator>() == null) host.gameObject.AddComponent<TruckTaxiDriverNeedsCoordinator>();
            var locations = Object.FindObjectsByType<TruckTaxiRideLocation>(FindObjectsSortMode.None)
                .Where(l => l.gameObject.scene == host.gameObject.scene).ToArray();
            foreach (string id in new[] { "taxi.stop.01", "taxi.stop.07" })
            {
                var matches = locations.Where(l => l.locationId == id).ToArray();
                if (matches.Length != 1) throw new InvalidOperationException("Expected one authored restroom host location: " + id);
                var location = matches[0];
                var bathroom = location.GetComponent<TruckTaxiBathroomPoint>();
                if (bathroom == null)
                {
                    bathroom = location.gameObject.AddComponent<TruckTaxiBathroomPoint>();
                    bathroom.stableId = id + ".restroom"; bathroom.displayName = location.locationName + " Restroom";
                    bathroom.location = location; bathroom.stopRadius = location.detectionRadius;
                    var sign = new GameObject("Restroom service sign").AddComponent<TextMeshPro>();
                    sign.transform.SetParent(location.transform, false); sign.transform.localPosition = new Vector3(6, 5.2f, 5);
                    sign.transform.localRotation = Quaternion.Euler(0, 270, 0); sign.font = TMP_Settings.defaultFontAsset;
                    sign.fontSize = 3.4f; sign.alignment = TextAlignmentOptions.Center; sign.rectTransform.sizeDelta = new Vector2(9, 3.5f);
                    sign.text = "RESTROOM\nDISPOSAL / CAB CLEANUP";
                }
                if (!Physics.Raycast(bathroom.Position + Vector3.up * 4, Vector3.down, out _, 8, ~0, QueryTriggerInteraction.Ignore))
                    throw new InvalidOperationException("Restroom bay has no ground: " + bathroom.stableId);
                EditorUtility.SetDirty(bathroom);
            }
            var scenic = Object.FindObjectsByType<TruckTaxiStopObjectivePoint>(FindObjectsSortMode.None)
                .Where(p => p.gameObject.scene == host.gameObject.scene && p.category == TruckTaxiStopCategory.Scenic)
                .OrderBy(p => p.stableId, StringComparer.Ordinal).FirstOrDefault();
            if (scenic != null && scenic.GetComponent<TruckTaxiScenicEnvironmentBonus>() == null)
                scenic.gameObject.AddComponent<TruckTaxiScenicEnvironmentBonus>();
            EditorUtility.SetDirty(environment);
        }
        private static GameObject EnsureTaxiWeatherPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(WeatherPrefabPath);
            var resources = EnsureTaxiWeatherResources();
            var root = PrefabUtility.LoadPrefabContents(existing != null ? WeatherPrefabPath : LwsWeatherMakerAdapter.DefaultWeatherMakerPrefabPath);
            try
            {
                // The vendor default is permanent. Taxi weather must not leak into subsequent Interstate scenes.
                bool found = false;
                foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (component == null || component.GetType().FullName != "DigitalRuby.WeatherMaker.WeatherMakerScript") continue;
                    var data = new SerializedObject(component); var permanent = data.FindProperty("IsPermanent");
                    if (permanent == null) throw new InvalidOperationException("Installed Weather Maker does not expose expected IsPermanent setting.");
                    var container = data.FindProperty("ResourceContainer");
                    if (container == null) throw new InvalidOperationException("Installed Weather Maker has no ResourceContainer setting.");
                    permanent.boolValue = false; container.objectReferenceValue = resources;
                    data.ApplyModifiedPropertiesWithoutUndo(); found = true;
                }
                if (!found) throw new InvalidOperationException("Weather Maker prefab has no WeatherMakerScript.");
                root.name = "Truck Taxi Weather Maker";
                return PrefabUtility.SaveAsPrefabAsset(root, WeatherPrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        private static ScriptableObject EnsureTaxiWeatherResources()
        {
            const string source = "Assets/WeatherMaker/Prefab/Profiles/ResourceContainer/WeatherMakerResourceContainerScript_NoWater.asset";
            if (AssetDatabase.LoadMainAssetAtPath(WeatherResourcesPath) == null && !AssetDatabase.CopyAsset(source, WeatherResourcesPath))
                throw new InvalidOperationException("Could not copy installed NoWater resource container for Taxi.");
            var resources = AssetDatabase.LoadAssetAtPath<ScriptableObject>(WeatherResourcesPath);
            var data = new SerializedObject(resources);
            var water = data.FindProperty("ProfilesWater"); var shaders = data.FindProperty("Shaders");
            if (water == null || shaders == null || !shaders.isArray)
                throw new InvalidOperationException("Installed resource container does not expose water profiles and shaders.");
            water.ClearArray();
            // Vendor NoWater omits water profiles, but still explicitly bundles its water shaders.
            for (int i = shaders.arraySize - 1; i >= 0; i--)
            {
                var entry = shaders.GetArrayElementAtIndex(i); var shader = entry.objectReferenceValue as Shader;
                if (shader == null || shader.name.IndexOf("Water", StringComparison.OrdinalIgnoreCase) < 0) continue;
                entry.objectReferenceValue = null; shaders.DeleteArrayElementAtIndex(i);
            }
            data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(resources);
            return resources;
        }
        private static UnityEngine.Rendering.RenderPipelineAsset EnsureWeatherRenderer()
        {
            const string folder = "Assets/LWS/TruckTaxi/Rendering";
            const string rendererPath = folder + "/TruckTaxi_Weather_Renderer.asset";
            const string pipelinePath = folder + "/TruckTaxi_Weather_RPAsset.asset";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/LWS/TruckTaxi", "Rendering");
            if (AssetDatabase.LoadMainAssetAtPath(rendererPath) == null && !AssetDatabase.CopyAsset("Assets/Settings/PC_Renderer.asset", rendererPath))
                throw new InvalidOperationException("Could not copy existing PC renderer for Taxi weather.");
            var renderer = AssetDatabase.LoadMainAssetAtPath(rendererPath);
            const string featureName = "DigitalRuby.WeatherMaker.WeatherMakerURPRenderFeatureScript";
            var featureType = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(featureName, false)).FirstOrDefault(t => t != null);
            if (featureType == null) throw new InvalidOperationException("Installed Weather Maker URP render feature is not compiled. Enable vendor URP support before configuring Taxi weather.");
            // Use the renderer's public feature collection; all copies and subassets remain project-owned.
            var features = renderer.GetType().GetProperty("rendererFeatures")?.GetValue(renderer) as System.Collections.IList;
            if (features == null) throw new InvalidOperationException("Current renderer does not expose its public rendererFeatures collection.");
            bool found = false; foreach (var feature in features) if (feature != null && featureType.IsInstanceOfType(feature)) found = true;
            if (!found)
            {
                var feature = ScriptableObject.CreateInstance(featureType); feature.name = "Taxi Weather Maker URP";
                AssetDatabase.AddObjectToAsset(feature, renderer); features.Add(feature);
                EditorUtility.SetDirty(feature); EditorUtility.SetDirty(renderer);
            }
            if (AssetDatabase.LoadMainAssetAtPath(pipelinePath) == null &&
                !AssetDatabase.CopyAsset("Assets/LWS/InterstateHauler/Rendering/Quality/IH_Medium_RPAsset.asset", pipelinePath))
                throw new InvalidOperationException("Could not copy existing medium pipeline for Taxi weather.");
            var pipeline = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.RenderPipelineAsset>(pipelinePath);
            var serialized = new SerializedObject(pipeline); var renderers = serialized.FindProperty("m_RendererDataList");
            if (renderers == null || renderers.arraySize == 0) throw new InvalidOperationException("Current URP pipeline has no serialized renderer list.");
            renderers.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
            serialized.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(pipeline);
            return pipeline;
        }
    }
}
