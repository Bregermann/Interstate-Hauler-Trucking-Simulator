using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LWS.InterstateHauler;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace LWS.TruckTaxi.Editor
{
    // Adds two short spurs to the existing worlds; never invokes either world generator.
    public static class TruckTaxiWorldConnectionAuthoring
    {
        private const string Folder = "Assets/LWS/TruckTaxi/WorldConnections";
        private const string RootName = "Truck Taxi Regional Connection";

        [MenuItem("Truck Taxi/Regional/Add Or Refresh World Connections")]
        public static void Build()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
            foreach (string path in new[] { TruckTaxiDemoBuilder.ScenePath, TruckTaxiEdenboroughLayout.ScenePath })
            {
                Scene scene = SceneManager.GetSceneByPath(path);
                bool opened = !scene.IsValid() || !scene.isLoaded;
                if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                SceneManager.SetActiveScene(scene);
                AddConnection(scene);
                EditorSceneManager.SaveScene(scene);
                if (opened) EditorSceneManager.CloseScene(scene, true);
            }
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Truck Taxi/Regional/Bake World Connection Map Tiles")]
        public static void BakeMaps()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            foreach (string path in new[] { TruckTaxiDemoBuilder.ScenePath, TruckTaxiEdenboroughLayout.ScenePath })
            {
                Scene scene = SceneManager.GetSceneByPath(path);
                bool opened = !scene.IsValid() || !scene.isLoaded;
                if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    var root = scene.GetRootGameObjects().Single(g => g.name == RootName);
                    var bounds = new Bounds(root.GetComponentInChildren<TruckTaxiWorldExit>().transform.position, Vector3.zero);
                    foreach (var road in root.GetComponentsInChildren<MeshCollider>()) bounds.Encapsulate(road.bounds);
                    bounds.Expand(new Vector3(120,400,120));
                    TruckTaxiMapTileBaker.BakeConnectionSupplement(scene,bounds);
                }
                finally { if (opened) EditorSceneManager.CloseScene(scene,true); }
            }
        }

        private static void AddConnection(Scene scene)
        {
            var provider = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<LwsRoadGraphProvider>(true)).Single();
            bool eden = scene.name == TruckTaxiWorldCatalog.Edenborough;
            string nodeId = eden ? "bypass.west" : "taxi.regional.1200";
            string region = eden ? "taxi.edenborough.hillend" : "taxi.region.highway";
            string chunk = eden ? scene.name : "Taxi_Highway";
            var node = provider.Graph.nodes.Single(n => n.nodeId == nodeId);
            var points = eden ? new[] { node.position, new Vector3(-4900, 0, -2430), new Vector3(-5050, 0, -2490), new Vector3(-5250, 0, -2550) } :
                new[] { node.position, new Vector3(1260, .14f, -40), new Vector3(1340, .14f, -130), new Vector3(1400, .14f, -240) };
            if (eden)
                for (int i = 1; i < points.Length; i++) points[i].y = TruckTaxiEdenboroughLayout.At(points[i].x, points[i].z).y;
            var old = scene.GetRootGameObjects().FirstOrDefault(g => g.name == RootName);
            if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject(RootName);
            Material asphalt = AssetDatabase.LoadAssetAtPath<Material>(TruckTaxiDemoBuilder.Root + "/Materials/Asphalt.mat");
            if (asphalt == null) throw new InvalidOperationException("Existing Truck Taxi asphalt material is missing.");
            TruckTaxiDemoBuilder.BakeRegionalRoads(root.transform, new List<Vector3[]> { points }, 7.4f,
                eden ? "Edenborough_Connection" : "DemoCity_Connection", asphalt, Folder);
            if (eden) GradeConnection(scene, root, points);

            string exitId = "taxi.world-exit." + (eden ? "demo-city" : "edenborough");
            provider.Graph.nodes.RemoveAll(n => n.nodeId == exitId);
            provider.Graph.edges.RemoveAll(e => e.fromNodeId == exitId || e.toNodeId == exitId);
            provider.Graph.nodes.Add(new LwsRoadNode { nodeId = exitId, position = points[points.Length - 1], regionId = region, sceneChunkId = chunk });
            int first = provider.Graph.edges.Count;
            TruckTaxiDemoBuilder.BayEdge(provider.Graph, nodeId, exitId, points);
            TruckTaxiDemoBuilder.BayEdge(provider.Graph, exitId, nodeId, points.Reverse().ToArray());
            foreach (var edge in provider.Graph.edges.Skip(first))
            {
                edge.laneWidthMeters = 3.45f; edge.speedLimitMph = 30; edge.stateOrRegionId = region; edge.sceneChunkId = chunk;
                foreach (var sample in edge.samples) { sample.roadWidthMeters = 7.4f; sample.laneWidthMeters = 3.45f; sample.speedLimitMph = 30; }
            }
            EditorUtility.SetDirty(provider);
            Vector3 end = points[points.Length - 1], forward = (end - points[points.Length - 2]).normalized;
            forward.y = 0; forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            var portal = new GameObject("Outbound regional boundary", typeof(BoxCollider), typeof(TruckTaxiWorldExit));
            portal.transform.SetParent(root.transform, false);
            portal.transform.SetPositionAndRotation(end - forward * 12 + right * 1.725f + Vector3.up * 3, Quaternion.LookRotation(forward));
            var trigger = portal.GetComponent<BoxCollider>(); trigger.isTrigger = true; trigger.size = new Vector3(3.4f, 8, 10);
            var exit = portal.GetComponent<TruckTaxiWorldExit>(); exit.destinationScene = TruckTaxiWorldCatalog.Other(scene.name);
            var arrival = new GameObject("Safe inbound arrival").transform; arrival.SetParent(root.transform, false);
            arrival.SetPositionAndRotation(end - forward * 65 - right * 1.725f + Vector3.up * 1.3f, Quaternion.LookRotation(-forward));
            exit.arrival = arrival;
            var marker = portal.AddComponent<TruckTaxiMapMarker>(); marker.stableId = exitId;
            marker.label = "Regional Travel - " + TruckTaxiWorldCatalog.Label(exit.destinationScene); marker.markerType = TruckTaxiMapMarkerType.Place;
            Sign(root.transform, end - forward * 100 + right * 8, forward, TruckTaxiWorldCatalog.Label(exit.destinationScene));
            // Short centerline dashes make the two narrow right-hand lanes legible.
            for (int i = 1; i < points.Length; i++)
            {
                Vector3 a = points[i - 1], b = points[i];
                for (float d = 5; d < Vector3.Distance(a,b) - 5; d += 14)
                {
                    Vector3 p = Vector3.MoveTowards(a,b,d);
                    Cube(root.transform, "Connection centerline", p + Vector3.up * .025f, new Vector3(.14f,.015f,5),
                        Quaternion.LookRotation(b-a), MaterialAsset("Connection road markings", new Color(.96f,.82f,.25f)), false);
                }
            }
            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static void GradeConnection(Scene scene, GameObject root, Vector3[] points)
        {
            var road = new TruckTaxiEdenboroughLayout.Road("world-connection", "Regional Connection", "bypass.west", "world-exit", "Avenue", points);
            var field = new TruckTaxiEdenboroughTerrainAuthoring.RoadHeightfield(new[] { road }, null, root.GetComponentsInChildren<Collider>());
            Bounds area = new Bounds(points[0], Vector3.zero); foreach (var p in points) area.Encapsulate(p); area.Expand(120);
            foreach (Terrain terrain in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Terrain>()))
            {
                var data = terrain.terrainData; var origin = terrain.transform.position;
                var bounds = new Bounds(origin + data.size*.5f, data.size);
                if (!bounds.Intersects(area)) continue;
                int n = data.heightmapResolution - 1;
                int x0 = Mathf.Clamp(Mathf.FloorToInt((area.min.x-origin.x)/data.size.x*n),0,n);
                int z0 = Mathf.Clamp(Mathf.FloorToInt((area.min.z-origin.z)/data.size.z*n),0,n);
                int x1 = Mathf.Clamp(Mathf.CeilToInt((area.max.x-origin.x)/data.size.x*n),0,n);
                int z1 = Mathf.Clamp(Mathf.CeilToInt((area.max.z-origin.z)/data.size.z*n),0,n);
                var heights = data.GetHeights(x0,z0,x1-x0+1,z1-z0+1);
                for (int z = z0; z <= z1; z++) for (int x = x0; x <= x1; x++)
                {
                    var sample = field.Sample(origin.x+x/(float)n*data.size.x, origin.z+z/(float)n*data.size.z);
                    if (sample.RoadBlend > 0) heights[z-z0,x-x0] = Mathf.Min(heights[z-z0,x-x0], Mathf.Clamp01((sample.Height-origin.y)/data.size.y));
                }
                data.SetHeights(x0,z0,heights); EditorUtility.SetDirty(data);
            }
        }

        private static void Sign(Transform parent, Vector3 position, Vector3 forward, string destination)
        {
            var sign = new GameObject("Regional highway sign").transform; sign.SetParent(parent,false);
            sign.SetPositionAndRotation(position,Quaternion.LookRotation(forward));
            Cube(sign,"Sign support",position+Vector3.up*2,new Vector3(.18f,4,.18f),sign.rotation,MaterialAsset("Connection steel",Color.gray),true);
            Cube(sign,"Destination sign",position+Vector3.up*4.8f,new Vector3(8,2.8f,.12f),sign.rotation,MaterialAsset("Connection sign green",new Color(.035f,.23f,.16f)),true);
            var text = new GameObject("Destination lettering",typeof(TextMeshPro)).GetComponent<TextMeshPro>(); text.transform.SetParent(sign,false);
            text.transform.localPosition = new Vector3(0,4.8f,-.08f); text.text = destination.ToUpperInvariant()+"\nREGIONAL CONNECTION";
            text.fontSize = 2.1f; text.alignment = TextAlignmentOptions.Center; text.rectTransform.sizeDelta = new Vector2(7.5f,2.5f);
        }

        private static Material MaterialAsset(string name, Color color)
        {
            string path = Folder+"/"+name+".mat"; var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material,path); }
            material.color=color; return material;
        }
        private static void Cube(Transform parent,string name,Vector3 position,Vector3 size,Quaternion rotation,Material material,bool solid)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube); go.name=name; go.transform.SetParent(parent,false);
            go.transform.SetPositionAndRotation(position,rotation); go.transform.localScale=size; go.GetComponent<Renderer>().sharedMaterial=material;
            if (!solid) Object.DestroyImmediate(go.GetComponent<Collider>());
        }
    }
}
