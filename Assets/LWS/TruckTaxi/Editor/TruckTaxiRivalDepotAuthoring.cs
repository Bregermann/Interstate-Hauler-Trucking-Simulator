using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.TruckTaxi.Editor
{
    public static class TruckTaxiRivalDepotAuthoring
    {
        private const string TruckPath = "Assets/UTS_FullPack/Models/Cars/Car_Prefabs/Day Cars/Truck_1.prefab";

        [MenuItem("Truck Taxi/Regional/Author Rival Truck Taxi Depots")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before authoring depots.");
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save open scene edits before depot authoring.");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TruckPath);
            if (prefab == null) throw new InvalidOperationException("UTS Truck_1 cab prefab is missing.");
            var scene = EditorSceneManager.OpenScene(TruckTaxiDemoBuilder.ScenePath);
            var traffic = UnityEngine.Object.FindFirstObjectByType<TruckTaxiTrafficAdapter>();
            if (traffic == null) throw new InvalidOperationException("Truck Taxi traffic adapter is required.");
            var rivals = traffic.GetComponent<TruckTaxiRivalPopulation>() ?? Undo.AddComponent<TruckTaxiRivalPopulation>(traffic.gameObject);
            Undo.RecordObject(traffic, "Assign UTS rival truck");
            traffic.rivalTruckPrefab = prefab;
            EditorUtility.SetDirty(traffic);
            var depots = new[] {
                EnsureDepot(rivals.transform, prefab, "taxi.depot.town01", "Town 01 Truck Taxi Depot", "taxi.town01", new Vector3(240, .2f, 240), true),
                EnsureDepot(rivals.transform, prefab, "taxi.depot.town02", "Town 02 Truck Taxi Depot", "taxi.town02", new Vector3(4240, .2f, 240), true),
                EnsureDepot(rivals.transform, prefab, "taxi.depot.stadium", "Stadium Staging Lot", "taxi.stadium", new Vector3(1210, .2f, -620), false),
                EnsureDepot(rivals.transform, prefab, "taxi.depot.neonyard", "Neon Yard Event Lot", "taxi.concert", new Vector3(3860, .2f, -675), false)
            };
            Undo.RecordObject(rivals, "Assign rival depot metadata");
            rivals.depots = depots;
            EditorUtility.SetDirty(rivals);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Rival cab-only UTS truck and four home/staging locations authored. Inspect Town 01/02 lot placement before acceptance.");
        }

        private static TruckTaxiRivalDepot EnsureDepot(Transform parent, GameObject prefab, string id,
            string label, string town, Vector3 position, bool visible)
        {
            Transform existing = parent.Find(id);
            GameObject root = existing != null ? existing.gameObject : new GameObject(id);
            if (existing == null) { Undo.RegisterCreatedObjectUndo(root, "Create rival depot"); root.transform.SetParent(parent, false); }
            root.transform.position = position;
            var depot = root.GetComponent<TruckTaxiRivalDepot>() ?? Undo.AddComponent<TruckTaxiRivalDepot>(root);
            Undo.RecordObject(depot, "Configure rival depot");
            depot.depotId = id;
            depot.preferredTown = town;
            depot.venueBias = visible ? .2f : .85f;
            depot.shiftStartHour = visible ? 6 : 16;
            depot.shiftEndHour = visible ? 22 : 2;
            EditorUtility.SetDirty(depot);
            if (visible && root.transform.Find("Lot") == null)
            {
                var lot = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Undo.RegisterCreatedObjectUndo(lot, "Create truck taxi lot");
                lot.name = "Lot";
                lot.transform.SetParent(root.transform, false);
                lot.transform.localPosition = new Vector3(0, -.05f, 0);
                lot.transform.localScale = new Vector3(25, .12f, 22);
                var sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Undo.RegisterCreatedObjectUndo(sign, "Create depot sign");
                sign.name = "Fleet sign";
                sign.transform.SetParent(root.transform, false);
                sign.transform.localPosition = new Vector3(0, 2.1f, 10);
                sign.transform.localScale = new Vector3(7, 2.2f, .35f);
                var text = new GameObject("Depot lettering");
                Undo.RegisterCreatedObjectUndo(text, "Create depot lettering");
                text.transform.SetParent(sign.transform, false);
                text.transform.localPosition = new Vector3(0, 0, -.55f);
                text.transform.localRotation = Quaternion.Euler(0, 180, 0);
                var mesh = text.AddComponent<TextMesh>();
                mesh.text = label.ToUpperInvariant();
                mesh.anchor = TextAnchor.MiddleCenter;
                mesh.alignment = TextAlignment.Center;
                mesh.characterSize = .12f;
                mesh.fontSize = 48;
                for (int i = 0; i < 2; i++)
                {
                    var parked = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
                    Undo.RegisterCreatedObjectUndo(parked, "Park rival truck");
                    parked.name = "Parked UTS taxi truck " + (i + 1);
                    parked.transform.localPosition = new Vector3(i == 0 ? -6 : 6, 0, -3);
                    parked.transform.localRotation = Quaternion.Euler(0, i == 0 ? 0 : 180, 0);
                    foreach (var behaviour in parked.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
                    foreach (var collider in parked.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
                    foreach (var body in parked.GetComponentsInChildren<Rigidbody>(true))
                    { body.isKinematic = true; body.useGravity = false; }
                }
            }
            return depot;
        }
    }
}
