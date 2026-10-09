using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckTaxi.Editor
{
    public static class TruckTaxiRaceAuthoring
    {
        private const string TruckOne = "Assets/UTS_FullPack/Models/Cars/Car_Prefabs/Day Cars/Truck_1.prefab";
        private const string TruckTwo = "Assets/UTS_FullPack/Models/Cars/Car_Prefabs/Day Cars/Truck_2.prefab";
        private const string OfficeName = "Thunder Bowl Race Registration";

        [MenuItem("Truck Taxi/Regional/Validate Flagship Road Race")]
        public static void ValidateFlagship()
        {
            var host = SelectedHost();
            if (!TruckTaxiFlagshipRoute.TryCreate(host.roadGraph?.Graph,
                host.traffic?.cityLanes, out var route, out string reason))
                throw new InvalidOperationException(reason);
            Debug.Log($"Flagship geometry validated: {route.SourceLaneId}, {route.LengthMeters:0} m, {route.Gates.Length} gates. " +
                "Grounded grid, streaming, UTS performance and a full drive still require Play Mode validation.", host);
        }

        [MenuItem("Truck Taxi/Regional/Configure Flagship Road Race On Selected Host")]
        public static void ConfigureFlagship()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode before race authoring.");
            var host = SelectedHost();
            ValidateFlagship();
            var first = AssetDatabase.LoadAssetAtPath<GameObject>(TruckOne);
            var second = AssetDatabase.LoadAssetAtPath<GameObject>(TruckTwo);
            var api = new LWS.InterstateHauler.LwsUtsTrafficApi();
            if (!api.PrefabLooksLikeUtsVehicle(first) || !api.PrefabLooksLikeUtsVehicle(second))
                throw new InvalidOperationException("Installed UTS truck prefabs are absent or incompatible.");
            var race = host.GetComponent<TruckTaxiRaceRuntime>() ?? Undo.AddComponent<TruckTaxiRaceRuntime>(host.gameObject);
            Undo.RecordObject(race, "Configure flagship road race");
            race.utsTruckPrefabs = new[] { first, second };
            EditorUtility.SetDirty(race);
            EditorSceneManager.MarkSceneDirty(host.gameObject.scene);
            Debug.Log("Flagship UTS references configured. No road/venue geometry changed and no scene saved. " +
                "Bootstrap must retain its existing RaceRuntime.Initialize(host) call.", race);
        }

        private static TruckTaxiBootstrap SelectedHost()
        {
            var host = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponent<TruckTaxiBootstrap>() : null;
            if (host == null || !host.gameObject.scene.IsValid())
                throw new InvalidOperationException("Select the existing Truck Taxi bootstrap scene object.");
            return host;
        }

        [MenuItem("Truck Taxi/Regional/Install Thunder Bowl Race Office")]
        public static void Install()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode before race authoring.");
            for (int i=0;i<SceneManager.sceneCount;i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save open scene changes before race authoring.");
            GameObject first = AssetDatabase.LoadAssetAtPath<GameObject>(TruckOne);
            GameObject second = AssetDatabase.LoadAssetAtPath<GameObject>(TruckTwo);
            var api = new LWS.InterstateHauler.LwsUtsTrafficApi();
            if (!api.PrefabLooksLikeUtsVehicle(first) || !api.PrefabLooksLikeUtsVehicle(second))
                throw new InvalidOperationException("The installed UTS truck prefabs are absent or incompatible.");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TruckTaxiSpeedwayAuthoring.ScenePath) == null)
                throw new InvalidOperationException("Author the existing Thunder Bowl Speedway first.");

            var persistent = EditorSceneManager.OpenScene(TruckTaxiDemoBuilder.ScenePath);
            var host = UnityEngine.Object.FindFirstObjectByType<TruckTaxiBootstrap>();
            if (host == null) throw new InvalidOperationException("Truck Taxi bootstrap is missing from the persistent scene.");
            var race = host.GetComponent<TruckTaxiRaceRuntime>() ?? Undo.AddComponent<TruckTaxiRaceRuntime>(host.gameObject);
            Undo.RecordObject(race,"Configure Thunder Bowl racing");
            race.utsTruckPrefabs = new[] { first, second };
            EditorUtility.SetDirty(race);
            EditorSceneManager.MarkSceneDirty(persistent);
            EditorSceneManager.SaveScene(persistent);

            var speedway = EditorSceneManager.OpenScene(TruckTaxiSpeedwayAuthoring.ScenePath);
            GameObject office = null;
            foreach (var root in speedway.GetRootGameObjects())
                if (root.name == OfficeName) { office = root; break; }
            if (office == null)
            {
                office = new GameObject(OfficeName);
                Undo.RegisterCreatedObjectUndo(office,"Create Thunder Bowl race office sign");
                var text = office.AddComponent<TextMeshPro>();
                text.text = "RACE REGISTRATION\nPIT OFFICE";
                text.fontSize = 3.4f;
                text.alignment = TextAlignmentOptions.Center;
                text.rectTransform.sizeDelta = new Vector2(34,8);
                office.transform.position = TruckTaxiSpeedwayLayout.Stops[1].Position + Vector3.up*5;
                EditorSceneManager.MarkSceneDirty(speedway);
                EditorSceneManager.SaveScene(speedway);
            }
            Debug.Log("Thunder Bowl race sidecar and UTS truck prefabs configured; pit office sign " +
                (office != null ? "present" : "missing") + ". Bootstrap still needs to call Initialize(this).");
        }
    }
}
