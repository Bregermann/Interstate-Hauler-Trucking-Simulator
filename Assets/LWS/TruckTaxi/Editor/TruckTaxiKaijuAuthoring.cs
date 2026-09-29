using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace LWS.TruckTaxi.Editor
{
    public static class TruckTaxiKaijuAuthoring
    {
        public const string SetPieceRoot = "Kaiju Town02 set pieces";
        private const string TownScene = "Assets/LWS/TruckTaxi/Regional/Scenes/Taxi_Town02.unity";
        private const string CarAsset = "Assets/UTS_FullPack/Models/Cars/Car_Prefabs/Day Cars/Car_1.prefab";
        private const string BuildingAsset = "Assets/NWH/Vehicle Physics 2/Vehicles/Euro Truck by GR3D/GR3D 010416TRUK/Demo Scene/Mesh/Building01.fbx";

        // Parent authoring calls this after the regional world exists. Does not save scenes or touch Bootstrap.
        [MenuItem("Tools/Truck Taxi/Kaiju/Apply Town02 Set Pieces")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Apply kaiju authoring outside Play Mode.");
            var car = AssetDatabase.LoadAssetAtPath<GameObject>(CarAsset);
            var building = AssetDatabase.LoadAssetAtPath<GameObject>(BuildingAsset);
            if (car == null || building == null || AssetDatabase.LoadAssetAtPath<SceneAsset>(TownScene) == null)
                throw new InvalidOperationException("Kaiju authoring requires the regional Town02 scene, installed UTS car and NWH building model.");
            var scene = SceneManager.GetSceneByPath(TownScene);
            if (!scene.isLoaded) scene = EditorSceneManager.OpenScene(TownScene, OpenSceneMode.Additive);
            foreach (var existing in scene.GetRootGameObjects())
                if (existing.name == SetPieceRoot) { Selection.activeGameObject = existing; return; }
            int undo = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Apply Kaiju Town02 Set Pieces");
            var root = new GameObject(SetPieceRoot);
            SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Create Kaiju set pieces");
            try
            {
                // Existing block parking court, between the authored roads at x4160/4320 and z160/320.
                for (int i = 0; i < 3; i++)
                    AddVisualTarget(root.transform, car, TruckTaxiKaijuPropKind.Vehicle,
                        new Vector3(4195 + i * 9, .2f, 278), new Vector3(2.4f, 2, 5));
                for (int i = 0; i < 2; i++)
                    AddVisualTarget(root.transform, building, TruckTaxiKaijuPropKind.BuildingChunk,
                        new Vector3(4238 + i * 12, .1f, 240), new Vector3(8, 6, 5));
                var anchor = new GameObject("Town02 kaiju approach");
                anchor.transform.SetParent(root.transform, false);
                anchor.transform.position = new Vector3(4335, 0, 300);
                anchor.AddComponent<TruckTaxiKaijuSpawnAnchor>().regionId = "taxi.town02";
                EditorSceneManager.MarkSceneDirty(scene);
                Selection.activeGameObject = root;
                Undo.CollapseUndoOperations(undo);
                Debug.Log("Kaiju Town02: 3 UTS-mesh parked vehicles, 2 imported-model building chunks and 1 approach anchor authored. Scene left open and dirty for parent validation/save.");
            }
            catch { Undo.RevertAllDownToGroup(undo); throw; }
        }

        private static void AddVisualTarget(Transform parent, GameObject asset, TruckTaxiKaijuPropKind kind,
            Vector3 position, Vector3 size)
        {
            var root = new GameObject(kind == TruckTaxiKaijuPropKind.Vehicle ? "Kaiju parked UTS car" : "Kaiju building chunk");
            root.transform.SetParent(parent, false);
            root.transform.position = position;
            var intact = TruckTaxiKaijuVisual.Snapshot(asset.transform, true);
            if (intact == null) throw new InvalidOperationException("Asset has no supported visible mesh: " + asset.name);
            intact.name = "Intact imported visual";
            intact.transform.SetParent(root.transform, false);
            intact.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            var bounds = TruckTaxiKaijuVisual.LocalBounds(intact);
            Vector3 scale = new Vector3(size.x / Mathf.Max(.01f, bounds.size.x),
                size.y / Mathf.Max(.01f, bounds.size.y), size.z / Mathf.Max(.01f, bounds.size.z));
            intact.transform.localScale = scale;
            intact.transform.localPosition = -Vector3.Scale(new Vector3(bounds.center.x, bounds.min.y, bounds.center.z), scale);
            var collider = intact.AddComponent<BoxCollider>();
            collider.center = bounds.center; collider.size = bounds.size;
            var damaged = Object.Instantiate(intact, root.transform);
            damaged.name = "Crushed imported visual";
            damaged.transform.localScale = Vector3.Scale(scale, new Vector3(1.1f, .15f, 1.05f));
            damaged.transform.localPosition = -Vector3.Scale(new Vector3(bounds.center.x, bounds.min.y, bounds.center.z), damaged.transform.localScale);
            damaged.SetActive(false);
            var prop = root.AddComponent<TruckTaxiKaijuProp>();
            prop.kind = kind; prop.intactRoot = intact; prop.damagedRoot = damaged;
        }

        [MenuItem("Tools/Truck Taxi/Kaiju/Create Configuration")]
        private static void CreateConfiguration()
        {
            var asset = ScriptableObject.CreateInstance<TruckTaxiKaijuConfig>();
            string path = AssetDatabase.GenerateUniqueAssetPath("Assets/LWS/TruckTaxi/TruckTaxiKaijuConfig.asset");
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        [MenuItem("GameObject/Truck Taxi/Kaiju/Attach Director To Bootstrap", false, 19)]
        private static void AttachDirector()
        {
            var boot = Selection.activeGameObject.GetComponent<TruckTaxiBootstrap>();
            if (boot.GetComponent<TruckTaxiKaijuDirector>() == null)
                Undo.AddComponent<TruckTaxiKaijuDirector>(boot.gameObject);
        }

        [MenuItem("GameObject/Truck Taxi/Kaiju/Attach Director To Bootstrap", true)]
        private static bool CanAttachDirector() => Selection.activeGameObject != null &&
            Selection.activeGameObject.GetComponent<TruckTaxiBootstrap>() != null;

        [MenuItem("GameObject/Truck Taxi/Kaiju/Vehicle Prop", false, 20)]
        private static void VehicleProp() => CreateTarget(TruckTaxiKaijuPropKind.Vehicle);

        [MenuItem("GameObject/Truck Taxi/Kaiju/Building Chunk", false, 21)]
        private static void BuildingChunk() => CreateTarget(TruckTaxiKaijuPropKind.BuildingChunk);

        [MenuItem("GameObject/Truck Taxi/Kaiju/Spawn Anchor", false, 22)]
        private static void SpawnAnchor()
        {
            var go = new GameObject("Kaiju emergence anchor");
            if (Selection.activeTransform != null)
            {
                go.transform.SetParent(Selection.activeTransform, false);
                go.transform.localPosition = Vector3.zero;
            }
            Undo.RegisterCreatedObjectUndo(go, "Create Kaiju spawn anchor");
            Undo.AddComponent<TruckTaxiKaijuSpawnAnchor>(go);
            Selection.activeGameObject = go;
        }

        private static void CreateTarget(TruckTaxiKaijuPropKind kind)
        {
            var root = new GameObject(kind == TruckTaxiKaijuPropKind.Vehicle ? "Kaiju authored vehicle" : "Kaiju authored building chunk");
            if (Selection.activeTransform != null)
            {
                root.transform.SetParent(Selection.activeTransform, false);
                root.transform.localPosition = Vector3.zero;
            }
            Undo.RegisterCreatedObjectUndo(root, "Create Kaiju target");
            var intact = GameObject.CreatePrimitive(PrimitiveType.Cube);
            intact.name = "Intact visible prop";
            intact.transform.SetParent(root.transform, false);
            intact.transform.localScale = kind == TruckTaxiKaijuPropKind.Vehicle ? new Vector3(3, 2, 6) : new Vector3(8, 12, 6);
            Undo.RegisterCreatedObjectUndo(intact, "Create Kaiju target visual");
            var damaged = GameObject.CreatePrimitive(PrimitiveType.Cube);
            damaged.name = "Damaged visible prop";
            damaged.transform.SetParent(root.transform, false);
            damaged.transform.localScale = intact.transform.localScale * .55f;
            damaged.SetActive(false);
            Undo.RegisterCreatedObjectUndo(damaged, "Create Kaiju damaged visual");
            var prop = Undo.AddComponent<TruckTaxiKaijuProp>(root);
            Undo.RecordObject(prop, "Configure Kaiju target");
            prop.kind = kind;
            prop.intactRoot = intact;
            prop.damagedRoot = damaged;
            EditorUtility.SetDirty(prop);
            Selection.activeGameObject = root;
        }

        [MenuItem("Tools/Truck Taxi/Kaiju/Validate Active Scene")]
        private static void Validate()
        {
            int valid = 0, invalid = 0;
            foreach (var prop in Object.FindObjectsByType<TruckTaxiKaijuProp>(FindObjectsSortMode.None))
            {
                bool okay = prop.intactRoot != null && prop.intactRoot != prop.gameObject &&
                    prop.GetComponentInParent<TruckTaxiTrafficPooledActor>() == null &&
                    (prop.presentationPrefab == null || prop.presentationPrefab.GetComponentsInChildren<MonoBehaviour>(true).Length == 0);
                if (okay) valid++;
                else { invalid++; Debug.LogError("Kaiju target needs an isolated visible child and a visual-only prefab; UTS cars cannot be targets.", prop); }
            }
            Debug.Log($"Kaiju authored targets: {valid} valid, {invalid} invalid. No traffic or scene objects changed.");
        }
    }
}
