using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace LWS.TruckCamping.Editor
{
    // Explicit forensic entry only. Reuses persisted failed-author inputs without
    // changing their bytes, importers, trees, GUIDs or the shared prefab.
    public static class CampingNavigationDiagnostics
    {
        [Serializable] sealed class PointReport { public int landmark; public Vector3 desired; public float terrainHeight; public bool sampled; public Vector3 hit; public float distance; }
        [Serializable] sealed class SourceReport { public string shape, asset; public Vector3 translation, size, scale; public int trees; }
        [Serializable] sealed class PassReport { public string name; public int sourceCount, triangleVertices; public Vector3 min, max; public PointReport[] points; public SourceReport[] sources; }
        [Serializable] sealed class Report { public string status = "DIAGNOSTIC_ONLY_NOT_ACCEPTANCE"; public PassReport[] passes; }

        public static void DiagnosePersistedPartialNavigation()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling) throw new InvalidOperationException("Stopped compiled Editor required.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save current scene edits before explicit navigation diagnosis.");
            var setup = EditorSceneManager.GetSceneManagerSetup();
            var transient = new List<UnityEngine.Object>();
            try
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var passes = new List<PassReport>();
                var prior = AssetDatabase.LoadAssetAtPath<NavMeshData>("Assets/TruckCamping/Models/Camp Haul connected woodland navigation.asset");
                if (prior == null) throw new InvalidOperationException("Exact persisted partial navigation asset required.");
                passes.Add(Measure("persisted failed-author navigation", prior, null, null));
                var root = new GameObject("Transient existing terrain navigation diagnosis"); transient.Add(root);
                var terrains = new Terrain[4];
                for (int z = 0; z < 2; z++) for (int x = 0; x < 2; x++)
                {
                    string path = "Assets/TruckCamping/Models/Camp Haul Vista terrain " + x + " " + z + ".asset";
                    var data = AssetDatabase.LoadAssetAtPath<TerrainData>(path);
                    if (data == null) throw new InvalidOperationException("Exact persisted terrain required: " + path);
                    var go = Terrain.CreateTerrainGameObject(data); go.transform.SetParent(root.transform, false);
                    go.transform.position = new Vector3(-1000 + x * 1000, 0, -1000 + z * 1000); terrains[z * 2 + x] = go.GetComponent<Terrain>();
                }
                passes.Add(Build("persisted terrains with original tree instances", root.transform, terrains, transient));
                foreach (var terrain in terrains)
                {
                    var copy = UnityEngine.Object.Instantiate(terrain.terrainData); copy.name = "Transient tree-free diagnostic copy"; transient.Add(copy);
                    copy.treeInstances = Array.Empty<TreeInstance>(); terrain.terrainData = copy; terrain.GetComponent<TerrainCollider>().terrainData = copy;
                }
                passes.Add(Build("transient copies of same terrains without tree instances", root.transform, terrains, transient));
                string directory = "Logs/TruckSports/Impossible07/CampingNavigationDiagnosis"; Directory.CreateDirectory(directory);
                string json = JsonUtility.ToJson(new Report { passes = passes.ToArray() }, true);
                File.WriteAllText(directory + "/persisted-partial-navigation.json", json);
                Debug.Log("CAMPING_NAVIGATION_DIAGNOSTIC " + json);
            }
            finally
            {
                foreach (var item in transient) if (item != null) UnityEngine.Object.DestroyImmediate(item);
                if (setup.Length > 0 && setup.All(s => !string.IsNullOrEmpty(s.path))) EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
        }

        static PassReport Build(string name, Transform root, Terrain[] terrains, List<UnityEngine.Object> transient)
        {
            Physics.SyncTransforms();
            var sources = new List<NavMeshBuildSource>();
            NavMeshBuilder.CollectSources(root, 1, NavMeshCollectGeometry.PhysicsColliders, 0, new List<NavMeshBuildMarkup>(), sources);
            var settings = NavMesh.GetSettingsByID(0); settings.agentRadius = 1; settings.agentHeight = 2.2f; settings.agentClimb = .45f;
            settings.agentSlope = 35; settings.overrideVoxelSize = true; settings.voxelSize = .65f;
            var data = NavMeshBuilder.BuildNavMeshData(settings, sources, new Bounds(new Vector3(0, 125, 0), new Vector3(2000, 300, 2000)), Vector3.zero, Quaternion.identity);
            if (data == null) throw new InvalidOperationException("Actual diagnostic navigation bake failed."); transient.Add(data);
            return Measure(name, data, sources, terrains);
        }

        static PassReport Measure(string name, NavMeshData data, List<NavMeshBuildSource> sources, Terrain[] terrains)
        {
            var instance = NavMesh.AddNavMeshData(data);
            try
            {
                var triangles = NavMesh.CalculateTriangulation(); var bounds = triangles.vertices.Length > 0 ? new Bounds(triangles.vertices[0], Vector3.zero) : new Bounds();
                foreach (var point in triangles.vertices) bounds.Encapsulate(point);
                var points = new List<PointReport>();
                for (int i = 0; i < CampingWildernessAuthoring.Stops.Length; i++)
                {
                    var stop = CampingWildernessAuthoring.Stops[i]; var desired = new Vector3(stop.x, CampingWildernessAuthoring.HeightAt(stop), stop.y - 7);
                    bool found = NavMesh.SamplePosition(desired, out var hit, 8, NavMesh.AllAreas); float height = float.NaN;
                    if (terrains != null) foreach (var terrain in terrains)
                    {
                        var local = desired - terrain.transform.position; var size = terrain.terrainData.size;
                        if (local.x >= 0 && local.z >= 0 && local.x <= size.x && local.z <= size.z) height = terrain.SampleHeight(desired) + terrain.transform.position.y;
                    }
                    points.Add(new PointReport { landmark = i, desired = desired, terrainHeight = height, sampled = found, hit = found ? hit.position : Vector3.zero, distance = found ? Vector3.Distance(desired, hit.position) : -1 });
                }
                return new PassReport { name = name, sourceCount = sources == null ? -1 : sources.Count, triangleVertices = triangles.vertices.Length, min = bounds.min, max = bounds.max, points = points.ToArray(), sources = sources == null ? Array.Empty<SourceReport>() : sources.Select(s => new SourceReport { shape = s.shape.ToString(), asset = s.sourceObject == null ? "" : AssetDatabase.GetAssetPath(s.sourceObject), translation = s.transform.GetColumn(3), scale = s.transform.lossyScale, size = s.size, trees = s.sourceObject is TerrainData terrain ? terrain.treeInstanceCount : 0 }).ToArray() };
            }
            finally { instance.Remove(); }
        }
    }
}
