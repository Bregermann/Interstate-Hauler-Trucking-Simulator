using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeadAir.Editor
{
    public static class DeadAirSceneBuilder
    {
        public const string ScenePath = "Assets/DeadAir/Scenes/DeadAir_Main.unity";

        [MenuItem("Dead Air/Build Or Refresh Main Scene")]
        public static void BuildOrRefreshMainScene()
        {
            Directory.CreateDirectory("Assets/DeadAir/Scenes");
            Scene scene = File.Exists(ScenePath)
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            EnsureRoot("DEAD_AIR");
            EnsureRoot("START");
            Transform systems = EnsureRoot("SYSTEMS");
            EnsureRoot("STORY_TRIGGERS");
            EnsureRoot("CHOICE_TRIGGERS");
            EnsureRoot("ENVIRONMENT");
            Transform ui = EnsureRoot("UI");
            EnsureRoot("DEBUG");

            EnsureComponent<DeadAirSceneBootstrapper>(GameObject.Find("DEAD_AIR").transform, "Dead Air Scene Bootstrapper");
            EnsureComponent<DeadAirGameManager>(systems, "Dead Air Game Manager");
            EnsureComponent<DeadAirStoryDirector>(systems, "Dead Air Story Director");
            EnsureComponent<DeadAirAudioDirector>(systems, "Dead Air Audio Director");
            EnsureComponent<DeadAirGPSDirector>(systems, "Dead Air GPS Director");
            EnsureComponent<DeadAirEndingDirector>(systems, "Dead Air Ending Director");
            EnsureComponent<DeadAirAnomalyDirector>(systems, "Dead Air Anomaly Director");
            EnsureComponent<DeadAirHud>(ui, "Dead Air HUD");
            EnsurePlayerTruck(EnsureRoot("START"));
            DeadAirBeatLayoutUtility.EnsureUnplacedBeatLayout();

            EnsurePreviewCamera();
            EnsureLight();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Dead Air] Built/refreshed {ScenePath}.");
        }

        [MenuItem("Dead Air/Create Unplaced Beat Layout")]
        public static void CreateUnplacedBeatLayout()
        {
            DeadAirBeatLayoutUtility.EnsureUnplacedBeatLayout();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[Dead Air] Unplaced beat layout created/refreshed.");
        }

        private static Transform EnsureRoot(string name)
        {
            GameObject existing = GameObject.Find(name);
            return existing != null ? existing.transform : new GameObject(name).transform;
        }

        private static T EnsureComponent<T>(Transform parent, string name) where T : Component
        {
            T existing = Object.FindFirstObjectByType<T>();
            if (existing != null)
            {
                if (existing.transform.parent == null && parent != null)
                {
                    existing.transform.SetParent(parent, false);
                }

                return existing;
            }

            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<T>();
        }

        private static void EnsurePreviewCamera()
        {
            if (Camera.main != null)
            {
                return;
            }

            GameObject cameraObject = new GameObject("Dead Air Preview Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 18f, -38f);
            cameraObject.transform.rotation = Quaternion.Euler(22f, 0f, 0f);
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }

        private static void EnsureLight()
        {
            if (Object.FindFirstObjectByType<Light>() != null)
            {
                return;
            }

            GameObject lightObject = new GameObject("Dead Air Directional Light");
            lightObject.transform.rotation = Quaternion.Euler(55f, -50f, 0f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.65f;
            light.color = new Color(0.55f, 0.7f, 0.78f, 1f);
        }

        private static void EnsurePlayerTruck(Transform start)
        {
            if (Object.FindFirstObjectByType<DeadAirVehicleAdapter>() != null)
            {
                return;
            }

            const string prefabPath = "Assets/LWS/InterstateHauler/Vehicles/Prefabs/IH_PlayerTruck_NWH.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[Dead Air] Player truck prefab missing: {prefabPath}");
                return;
            }

            GameObject truck = PrefabUtility.InstantiatePrefab(prefab, start) as GameObject;
            if (truck == null)
            {
                return;
            }

            truck.name = "Dead Air Player Truck";
            truck.transform.localPosition = Vector3.zero;
            truck.transform.localRotation = Quaternion.identity;
            if (truck.GetComponent<DeadAirVehicleAdapter>() == null)
            {
                truck.AddComponent<DeadAirVehicleAdapter>();
            }

            if (truck.GetComponent<DeadAirBasicAutomaticInputSource>() == null)
            {
                truck.AddComponent<DeadAirBasicAutomaticInputSource>();
            }
        }
    }
}
