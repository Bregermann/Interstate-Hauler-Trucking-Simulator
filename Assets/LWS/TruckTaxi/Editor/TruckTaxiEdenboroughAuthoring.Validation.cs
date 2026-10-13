using System;
using System.Collections.Generic;
using System.Linq;
using LWS.InterstateHauler;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckTaxi.Editor
{
    public static partial class TruckTaxiEdenboroughAuthoring
    {
        [MenuItem("Truck Taxi/Edenborough/Validate Loaded Dedicated Scene")]
        public static void ValidateLoadedScene()
        {
            EnsureEditMode();
            var scene = SceneManager.GetSceneByPath(TruckTaxiEdenboroughLayout.ScenePath);
            if (!scene.IsValid() || !scene.isLoaded) throw new InvalidOperationException("Open the dedicated Edenborough scene first.");
            var host = Find<TruckTaxiBootstrap>(scene);
            if (!host.roadGraph.ValidateGraph().IsValid || host.mapTiles == null || host.mapTiles.tiles.Length == 0)
                throw new InvalidOperationException("Valid LWS road graph and baked Compass map tiles are required.");
            var spawner = new SerializedObject(host.spawner);
            var definition = spawner.FindProperty("truckDefinition").objectReferenceValue as LwsTruckDefinition;
            var prefab = definition != null ? definition.PlayerPrefab : spawner.FindProperty("fallbackPlayerTruckPrefab").objectReferenceValue as GameObject;
            if (prefab == null) throw new InvalidOperationException("Canonical player truck prefab missing.");
            float width = MeasureTractorWidth(prefab);
            if (width > TruckTaxiEdenboroughLayout.LocalLaneWidth)
                throw new InvalidOperationException("Actual tractor collider width " + width.ToString("F2") + " m exceeds the " + TruckTaxiEdenboroughLayout.LocalLaneWidth + " m local lane.");
            Physics.SyncTransforms(); int misses = 0, probes = 0;
            foreach (var road in TruckTaxiEdenboroughLayout.Roads)
                for (int i = 1; i < road.Points.Length - 1; i += 4)
                {
                    probes++;
                    if (!Physics.Raycast(road.Points[i] + Vector3.up * 3, Vector3.down, out var hit, 6, ~0, QueryTriggerInteraction.Ignore) ||
                        hit.collider.GetComponent<TruckTaxiSurface>()?.isRoad != true || Mathf.Abs(hit.point.y - road.Points[i].y) > .35f) misses++;
                }
            if (misses != 0) throw new InvalidOperationException("EasyRoads surface validation missed " + misses + "/" + probes + " sampled points. Inspect terrain/road collision before driving.");
            var binding = host.GetComponent<TruckTaxiEdenboroughSceneBinding>();
            var bay = binding?.garageVolume;
            if (bay == null) throw new InvalidOperationException("Drive-in garage volume missing.");
            var obstructions = Physics.OverlapBox(bay.transform.position, new Vector3(1.55f,2.15f,9), bay.transform.rotation, ~0, QueryTriggerInteraction.Ignore);
            if (obstructions.Length != 0) throw new InvalidOperationException("Garage truck clearance blocked by: " + string.Join(", ", obstructions.Select(c => c.name)));
            foreach (var stop in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<TruckTaxiRideLocation>(true)))
                if (!Physics.Raycast(stop.StopPosition + Vector3.up * 3, Vector3.down, out var hit, 6, ~0, QueryTriggerInteraction.Ignore) ||
                    hit.collider.GetComponent<TruckTaxiSurface>()?.isRoad != true)
                    throw new InvalidOperationException("Service bay has no road collider: " + stop.locationId);
            Debug.Log("EDENBOROUGH GEOMETRY CHECK: tractor width=" + width.ToString("F2") + " m; narrow lane=" +
                TruckTaxiEdenboroughLayout.LocalLaneWidth + " m; road probes=" + probes + ". Editor geometry only; Windows driving/visual validation remains required.");
        }
        public static float MeasureTractorWidth(GameObject prefab)
        {
            bool found = false; Bounds union = default;
            foreach (var collider in prefab.GetComponentsInChildren<Collider>(true))
            {
                if (collider.isTrigger) continue;
                Bounds local;
                if (collider is BoxCollider box) local = new Bounds(box.center, box.size);
                else if (collider is SphereCollider sphere) local = new Bounds(sphere.center, Vector3.one * sphere.radius * 2);
                else if (collider is CapsuleCollider capsule)
                { var size = Vector3.one * capsule.radius * 2; size[capsule.direction] = Mathf.Max(capsule.height,size[capsule.direction]); local = new Bounds(capsule.center,size); }
                else if (collider is MeshCollider mesh && mesh.sharedMesh != null) local = mesh.sharedMesh.bounds;
                else continue;
                var relative = TruckTaxiWorldColliderReview.TransformBounds(local,prefab.transform.worldToLocalMatrix * collider.transform.localToWorldMatrix);
                if (!found) { union = relative; found = true; } else union.Encapsulate(relative);
            }
            if (!found) throw new InvalidOperationException("Canonical truck has no inspectable physical collider bounds.");
            return union.size.x;
        }
        [MenuItem("Truck Taxi/Edenborough/Use Validated Edenborough As Primary World")]
        public static void SetPrimaryWorld()
        {
            ValidateLoadedScene();
            string path = EditorTruckTaxiMainMenuSetup.ScenePath;
            var menuScene = SceneManager.GetSceneByPath(path); bool opened = !menuScene.IsValid() || !menuScene.isLoaded;
            if (opened) menuScene = EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                var menu = Find<TruckTaxiMainMenu>(menuScene); menu.ConfigurePrimaryWorld(TruckTaxiEdenboroughLayout.SceneName);
                EditorUtility.SetDirty(menu); EditorSceneManager.MarkSceneDirty(menuScene); EditorSceneManager.SaveScene(menuScene);
            }
            finally { if (opened) EditorSceneManager.CloseScene(menuScene,true); }
        }
    }
}
