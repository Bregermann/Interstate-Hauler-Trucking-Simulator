using System;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckTaxi.Editor
{
    public static class TruckTaxiRoadPepAuthoring
    {
        // Parent world authoring selects only exceptional stores; ordinary stock remains unchanged.
        public static void EnableRareStock(TruckTaxiStorePoint store)
        {
            if (store == null) throw new ArgumentNullException(nameof(store));
            if (store.sellsRoadPep) return;
            Undo.RecordObject(store, "Enable rare Road Pep stock");
            store.sellsRoadPep = true;
            EditorUtility.SetDirty(store);
        }

        public static ScriptableObject EnsureRuntimeMushroomWave(TruckTaxiEnvironmentSettings settings,
            Camera gameplayCamera = null)
        {
            if (settings == null || settings.weatherRenderPipeline == null)
                throw new ArgumentException("The active Taxi environment must have a weather render pipeline.", nameof(settings));
            var pipeline = new SerializedObject(settings.weatherRenderPipeline);
            var renderers = pipeline.FindProperty("m_RendererDataList");
            var defaultIndex = pipeline.FindProperty("m_DefaultRendererIndex");
            if (renderers == null || defaultIndex == null)
                throw new InvalidOperationException("The configured Taxi pipeline is not compatible URP renderer data.");
            int index = defaultIndex.intValue;
            if (gameplayCamera != null)
            {
                foreach (var component in gameplayCamera.GetComponents<Component>())
                {
                    if (component == null || component.GetType().FullName !=
                        "UnityEngine.Rendering.Universal.UniversalAdditionalCameraData") continue;
                    var cameraData = new SerializedObject(component);
                    var overrideIndex = cameraData.FindProperty("m_RendererIndex");
                    if (overrideIndex != null && overrideIndex.intValue >= 0) index = overrideIndex.intValue;
                    break;
                }
            }
            if (index < 0 || index >= renderers.arraySize)
                throw new InvalidOperationException("The gameplay camera's URP renderer index is invalid: " + index);
            var renderer = renderers.GetArrayElementAtIndex(index).objectReferenceValue;
            var feature = EnsureMushroomWave(renderer);
            Debug.Log("Mushroom wave configured: " + AssetDatabase.GetAssetPath(settings.weatherRenderPipeline) +
                " renderer[" + index + "] -> " + AssetDatabase.GetAssetPath(renderer), renderer);
            return feature;
        }

        // Match the existing weather authoring bridge without adding URP references to the shared editor assembly.
        public static ScriptableObject EnsureMushroomWave(UnityEngine.Object renderer)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Configure the renderer before entering Play Mode.");
            if (renderer == null || !AssetDatabase.Contains(renderer))
                throw new ArgumentException("Select a persistent project-owned URP renderer data asset.", nameof(renderer));
            if (!AssetDatabase.GetAssetPath(renderer).StartsWith("Assets/LWS/TruckTaxi/Rendering/", StringComparison.Ordinal))
                throw new ArgumentException("Select a Truck Taxi owned renderer; shared/vendor assets are not modified.", nameof(renderer));
            var serializedRenderer = new SerializedObject(renderer);
            var features = serializedRenderer.FindProperty("m_RendererFeatures");
            var featureMap = serializedRenderer.FindProperty("m_RendererFeatureMap");
            if (features == null || featureMap == null)
                throw new ArgumentException("Selected asset is not URP renderer data.", nameof(renderer));
            const string shaderPath = "Assets/LWS/TruckTaxi/Rendering/TruckTaxiMushroomWorldWave.shader";
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
            if (shader == null) throw new InvalidOperationException("Mushroom wave shader has not been imported: " + shaderPath);
            var featureType = typeof(TruckTaxiTemporaryEffects).Assembly.GetType(
                "LWS.TruckTaxi.TruckTaxiMushroomWaveFeature", true);
            ScriptableObject configured = null;
            for (int i = 0; i < features.arraySize; i++)
            {
                var feature = features.GetArrayElementAtIndex(i).objectReferenceValue;
                if (feature != null && featureType.IsInstanceOfType(feature))
                { configured = (ScriptableObject)feature; break; }
            }
            Undo.RecordObject(renderer, "Add Mushroom World Wave");
            if (configured == null)
            {
                configured = ScriptableObject.CreateInstance(featureType);
                configured.name = "Taxi Mushroom World Wave";
                Undo.RegisterCreatedObjectUndo(configured, "Add Mushroom World Wave");
                AssetDatabase.AddObjectToAsset(configured, renderer);
                features.arraySize++;
                features.GetArrayElementAtIndex(features.arraySize - 1).objectReferenceValue = configured;
            }
            Undo.RecordObject(configured, "Configure Mushroom World Wave");
            var serializedFeature = new SerializedObject(configured);
            serializedFeature.FindProperty("waveShader").objectReferenceValue = shader;
            serializedFeature.FindProperty("m_Active").boolValue = true;
            serializedFeature.ApplyModifiedProperties();
            featureMap.arraySize = features.arraySize;
            for (int i = 0; i < features.arraySize; i++)
            {
                var feature = features.GetArrayElementAtIndex(i).objectReferenceValue;
                long localId = 0;
                if (feature != null) AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out string guid, out localId);
                featureMap.GetArrayElementAtIndex(i).longValue = localId;
            }
            serializedRenderer.ApplyModifiedProperties();
            renderer.GetType().GetMethod("SetDirty")?.Invoke(renderer, null);
            EditorUtility.SetDirty(configured);
            EditorUtility.SetDirty(renderer);
            AssetDatabase.SaveAssetIfDirty(renderer);
            return configured;
        }

        [MenuItem("Truck Taxi/Configure Mushroom Wave For Runtime Pipeline")]
        public static void ConfigureRuntimeMushroomWave()
        {
            var settings = Selection.activeObject as TruckTaxiEnvironmentSettings;
            if (settings == null)
            {
                foreach (var environment in UnityEngine.Object.FindObjectsByType<TruckTaxiEnvironmentCoordinator>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (environment.settings == null) continue;
                    if (settings != null && settings != environment.settings)
                        throw new InvalidOperationException("Multiple Taxi settings are loaded. Select the active settings asset first.");
                    settings = environment.settings;
                }
            }
            EnsureRuntimeMushroomWave(settings);
        }

        [MenuItem("Truck Taxi/Enable Road Pep At Selected Store")]
        private static void EnableSelectedStore()
        {
            var store = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponent<TruckTaxiStorePoint>() : null;
            EnableRareStock(store);
        }

        [MenuItem("Truck Taxi/Add Mushroom Wave To Selected Renderer")]
        private static void AddToSelectedRenderer() => EnsureMushroomWave(Selection.activeObject);
    }
}
