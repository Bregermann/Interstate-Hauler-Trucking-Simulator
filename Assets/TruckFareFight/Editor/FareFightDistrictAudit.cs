using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using LWS.TruckTaxi;
using LWS.InterstateHauler;
namespace LWS.TruckFareFight.Editor
{
    // Read actual authored districts and graph metadata before composing a
    // separate sport. Never saves or reauthors the original Taxi scenes.
    public static class FareFightDistrictAudit
    {
        public static void Details()
        {
            var scene=EditorSceneManager.OpenPreviewScene("Assets/LWS/TruckTaxi/Scenes/TruckTaxi_DemoCity.unity");
            try {
                var text=new System.Text.StringBuilder();
                foreach(var root in scene.GetRootGameObjects()){
                    text.AppendLine(root.name+" components="+string.Join(",",root.GetComponents<Component>().Where(c=>c!=null).Select(c=>c.GetType().Name)));
                    foreach(Transform child in root.transform){text.AppendLine("  "+child.name+" @ "+child.position+" children="+child.childCount);if(root.name=="Road Network")foreach(Transform nested in child)text.AppendLine("    "+nested.name+" @ "+nested.position);}
                }
                File.WriteAllText("Logs/TruckSports/EssentialRelease/FareFight/scene-composition.txt",text.ToString());
            } finally{EditorSceneManager.ClosePreviewScene(scene);}
        }
        [Serializable] public sealed class Stop
        { public string id, name, district; public Vector3 position; public bool pickup, destination; }
        [Serializable] public sealed class RoadNode
        { public string id, region, chunk; public Vector3 position; }
        [Serializable] public sealed class District
        { public string name; public int stops; public Vector3 minimum, maximum; }
        [Serializable] public sealed class Audit
        { public string source, graphId; public string[] roots; public int edges; public Stop[] stops; public RoadNode[] nodes; public District[] districts; }
        [MenuItem("Truck Sports/Fare Fight/Audit existing Taxi districts (read only)")]
        public static void Run()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("District audit requires the exclusive idle Editor.");
            Directory.CreateDirectory("Logs/TruckSports/EssentialRelease/FareFight");
            foreach (var path in new[] { "Assets/LWS/TruckTaxi/Scenes/TruckTaxi_DemoCity.unity", "Assets/LWS/TruckTaxi/Scenes/TruckTaxi_Edenborough.unity" })
            {
                var scene = EditorSceneManager.OpenPreviewScene(path);
                try
                {
                    var roots = scene.GetRootGameObjects();
                    var stops = roots.SelectMany(r => r.GetComponentsInChildren<TruckTaxiRideLocation>(true))
                        .Select(s => new Stop { id = s.locationId, name = s.locationName, district = s.district ?? "", position = s.StopPosition, pickup = s.pickupAllowed, destination = s.dropoffAllowed })
                        .OrderBy(s => s.id).ToArray();
                    var graph = roots.SelectMany(r => r.GetComponentsInChildren<LwsRoadGraphProvider>(true)).Select(p => p.Graph).FirstOrDefault(g => g != null && g.nodes.Count > 0);
                    var districts = new List<District>();
                    foreach (var group in stops.GroupBy(s => s.district))
                    {
                        var min = Vector3.one * float.MaxValue; var max = Vector3.one * float.MinValue;
                        foreach (var stop in group) { min = Vector3.Min(min, stop.position); max = Vector3.Max(max, stop.position); }
                        districts.Add(new District { name = group.Key, stops = group.Count(), minimum = min, maximum = max });
                    }
                    var audit = new Audit { source = path, roots = roots.Select(r => r.name).ToArray(), stops = stops,
                        graphId = graph?.graphId, edges = graph?.edges.Count ?? 0, districts = districts.ToArray(),
                        nodes = graph == null ? Array.Empty<RoadNode>() : graph.nodes.Select(n => new RoadNode { id = n.nodeId, region = n.regionId, chunk = n.sceneChunkId, position = n.position }).ToArray() };
                    File.WriteAllText("Logs/TruckSports/EssentialRelease/FareFight/" + Path.GetFileNameWithoutExtension(path) + "-districts.json", JsonUtility.ToJson(audit, true));
                }
                finally { EditorSceneManager.ClosePreviewScene(scene); }
            }
        }
    }
}
