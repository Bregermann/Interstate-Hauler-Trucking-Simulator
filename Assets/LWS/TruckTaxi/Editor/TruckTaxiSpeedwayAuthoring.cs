using System;
using System.Collections.Generic;
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
    public static class TruckTaxiSpeedwayAuthoring
    {
        public const string ScenePath = TruckTaxiRegionalAuthoring.Root + "/Scenes/" + TruckTaxiSpeedwayLayout.SceneName + ".unity";
        private const string MeshFolder = TruckTaxiRegionalAuthoring.Root + "/Meshes/Speedway";
        private const string MaterialFolder = TruckTaxiRegionalAuthoring.Root + "/Materials";

        [MenuItem("Truck Taxi/Regional/Hide Prototype Town Perimeter Walls")]
        public static void HidePrototypeTownWalls()
        {
            RequireCleanScenes();
            foreach (string town in new[] { "Town01", "Town02" })
            {
                var scene = EditorSceneManager.OpenScene(TruckTaxiRegionalAuthoring.Root + "/Scenes/Taxi_" + town + ".unity");
                int hidden = 0;
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        if (!BelongsToPrototypePerimeter(renderer.transform) || !renderer.enabled) continue;
                        // Keep existing collision as an invisible safety boundary until terrain/outskirts replace it.
                        renderer.enabled = false;
                        EditorUtility.SetDirty(renderer);
                        hidden++;
                    }
                if (hidden > 0) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
                Debug.Log("Truck Taxi " + town + ": hidden " + hidden + " prototype perimeter renderers; safety colliders retained.");
            }
        }

        public static bool IsPrototypePerimeter(string name)
        {
            return name == "North barrier" || name == "South barrier" || name == "East barrier" || name == "West barrier" ||
                   name == "East barrier regional opening" || name == "West barrier regional opening" ||
                   string.Equals(name, "Townperimeterwall", StringComparison.OrdinalIgnoreCase);
        }
        private static bool BelongsToPrototypePerimeter(Transform transform)
        {
            for (var current=transform; current!=null; current=current.parent)
                if (IsPrototypePerimeter(current.name)) return true;
            return false;
        }

        [MenuItem("Truck Taxi/Regional/Author Thunder Bowl Speedway")]
        public static void AuthorSpeedway()
        {
            RequireCleanScenes();
            var persistent = EditorSceneManager.OpenScene(TruckTaxiDemoBuilder.ScenePath);
            var host = Object.FindFirstObjectByType<TruckTaxiBootstrap>();
            var world = host != null ? host.GetComponent<TruckTaxiRegionalWorld>() : null;
            if (world == null || host.roadGraph == null || world.streamingManifest == null)
                throw new InvalidOperationException("Open the authored regional Truck Taxi scene first.");
            bool hasRegion = world.regions.Any(r => r.id == TruckTaxiSpeedwayLayout.RegionId);
            bool hasScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null;
            if (hasRegion && hasScene)
            {
                if (!world.streamingManifest.chunks.Any(c => c.chunkId == TruckTaxiSpeedwayLayout.RegionId) ||
                    !host.roadGraph.Graph.nodes.Exists(n => n.nodeId == "taxi.speedway.track.00"))
                    throw new InvalidOperationException("Speedway scene exists but its persistent graph or stream metadata is incomplete.");
                Debug.Log("Thunder Bowl Speedway is already authored; no changes made."); return;
            }
            if (hasRegion || hasScene) throw new InvalidOperationException("Partial speedway authoring detected. Inspect the persistent metadata and scene before retrying.");
            var graph = host.roadGraph.Graph;
            if (graph == null || !graph.Validate().IsValid) throw new InvalidOperationException("Regional road graph is missing or invalid.");
            if (!graph.nodes.Exists(n => n.nodeId == "taxi.regional.2800")) throw new InvalidOperationException("Required regional highway junction is absent.");
            var manifest = world.streamingManifest;
            if (manifest.chunks.Any(c => c.chunkId == TruckTaxiSpeedwayLayout.RegionId))
                throw new InvalidOperationException("Speedway chunk already exists without a complete authored region.");
            var service = manifest.chunks.Find(c => c.chunkId == "taxi.servicearea");
            if (service == null) throw new InvalidOperationException("Service Area streaming neighbor is absent.");
            foreach (string profileName in new[] { "Max_Volume", "Joy_Ride" })
                if (AssetDatabase.LoadAssetAtPath<PassengerProfile>("Assets/LWS/TruckTaxi/ScriptableObjects/Passengers/" + profileName + ".asset") == null)
                    throw new InvalidOperationException("Racing passenger profile missing: " + profileName);
            Folder(MeshFolder);
            Material asphalt = Material("Speedway Asphalt", new Color(.16f,.17f,.19f));
            Material concrete = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/Regional Concrete.mat") ?? Material("Speedway Concrete", new Color(.56f,.61f,.63f));
            Material seating = Material("Speedway Seating", new Color(.24f,.36f,.42f));
            Material accent = Material("Speedway Accent", new Color(.87f,.28f,.19f));
            Material ground = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/Regional Ground.mat") ?? Material("Speedway Ground", new Color(.24f,.38f,.2f));

            var metadata = new GameObject("Thunder Bowl Speedway Metadata");
            SceneManager.MoveGameObjectToScene(metadata, persistent);
            foreach (var spec in TruckTaxiSpeedwayLayout.Stops) AddStop(metadata.transform, spec);
            var lap = new GameObject("Take a Lap Start Line");
            lap.transform.SetParent(metadata.transform,false);
            lap.transform.position = TruckTaxiSpeedwayLayout.LapCheckpoints()[0];
            var lapStop = lap.AddComponent<TruckTaxiStopObjectivePoint>();
            lapStop.stableId = TruckTaxiSpeedwayLayout.LapStopId;
            lapStop.displayName = "Thunder Bowl Start Line";
            lapStop.district = "Speedway"; lapStop.category = TruckTaxiStopCategory.Racetrack;
            lapStop.radius = 25; lapStop.durationSeconds = 1; lapStop.maximumSpeed = 99;
            lapStop.arrivalDialogue = "Let's take a lap.";
            lapStop.completionDialogue = "That was a proper lap!";
            var poi = new GameObject(TruckTaxiSpeedwayLayout.DisplayName);
            poi.transform.SetParent(metadata.transform, false); poi.transform.position = TruckTaxiSpeedwayLayout.Stops[0].Position;
            var marker = poi.AddComponent<TruckTaxiMapMarker>();
            marker.stableId = TruckTaxiSpeedwayLayout.PoiId; marker.label = TruckTaxiSpeedwayLayout.DisplayName;
            marker.markerType = RacetrackIcon(); marker.state = TruckTaxiMapMarkerState.Known;

            var chunk = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(chunk);
            var root = new GameObject(TruckTaxiSpeedwayLayout.SceneName).transform;
            Box("Speedway outskirts", root, new Vector3(2800,-.55f,-605), new Vector3(750,1,1130), ground);
            Box("Infield apron", root, new Vector3(2800,.05f,-780), new Vector3(200,.2f,155), concrete);
            Box("Truck parking", root, new Vector3(2800,.06f,-525), new Vector3(330,.2f,150), asphalt);
            var track = TruckTaxiSpeedwayLayout.TrackCenterline();
            var closed = new List<Vector3>(track) { track[0] };
            TruckTaxiDemoBuilder.BakeRegionalRoads(root, new List<Vector3[]> { closed.ToArray() },
                TruckTaxiSpeedwayLayout.TrackWidth, "ThunderBowl_Track", asphalt, MeshFolder);
            var bakedTrack = root.GetChild(root.childCount - 1);
            BankEasyRoadsMeshes(bakedTrack, track);
            PaintTrackLanes(root, track, accent);
            var roads = new List<Vector3[]> {
                new[] { new Vector3(2800,.15f,0), new Vector3(2800,.2f,-250), TruckTaxiSpeedwayLayout.Stops[0].Position, TruckTaxiSpeedwayLayout.Stops[4].Position, TruckTaxiSpeedwayLayout.Stops[1].Position, TruckTaxiSpeedwayLayout.Stops[3].Position },
                new[] { TruckTaxiSpeedwayLayout.Stops[0].Position, TruckTaxiSpeedwayLayout.Stops[2].Position },
                new[] { TruckTaxiSpeedwayLayout.Stops[4].Position, TruckTaxiSpeedwayLayout.Stops[5].Position },
                new[] { TruckTaxiSpeedwayLayout.Stops[5].Position, track[43] }
            };
            TruckTaxiDemoBuilder.BakeRegionalRoads(root, roads, 10, "ThunderBowl_Access", asphalt, MeshFolder);
            BuildBowl(root, track, concrete, seating, accent);
            Box("Pit garage", root, new Vector3(2630,6,-555), new Vector3(54,12,30), concrete);
            Box("VIP gate", root, new Vector3(2980,5,-470), new Vector3(34,10,12), accent);
            var sign = new GameObject("Thunder Bowl Speedway sign").AddComponent<TextMeshPro>();
            sign.transform.SetParent(root,false); sign.transform.position = new Vector3(2800,17,-450);
            sign.text = "THUNDER BOWL\nSPEEDWAY"; sign.fontSize = 8;
            sign.rectTransform.sizeDelta = new Vector2(85,20); sign.alignment = TextAlignmentOptions.Center;
            EditorSceneManager.SaveScene(chunk, ScenePath);
            EditorSceneManager.CloseScene(chunk, true);
            SceneManager.SetActiveScene(persistent);

            TruckTaxiSpeedwayLayout.AddToGraph(graph);
            host.roadGraph.SetGraph(graph, false);
            var speedwayRegion = new TruckTaxiRegion {
                id = TruckTaxiSpeedwayLayout.RegionId, displayName = TruckTaxiSpeedwayLayout.DisplayName,
                sceneName = TruckTaxiSpeedwayLayout.SceneName, bounds = TruckTaxiSpeedwayLayout.RegionBounds,
                population = true };
            var regions = world.regions.ToList();
            int serviceIndex = regions.FindIndex(r => r.id == "taxi.servicearea");
            regions.Insert(serviceIndex >= 0 ? serviceIndex : regions.Count, speedwayRegion);
            world.regions = regions.ToArray();
            var definition = new LwsWorldChunkDefinition {
                chunkId = TruckTaxiSpeedwayLayout.RegionId, displayName = TruckTaxiSpeedwayLayout.DisplayName,
                sceneName = TruckTaxiSpeedwayLayout.SceneName, scenePath = ScenePath,
                boundsCenter = TruckTaxiSpeedwayLayout.RegionBounds.center,
                boundsSize = TruckTaxiSpeedwayLayout.RegionBounds.size,
                containsRoadGeometry = true, containsTrafficPresentation = false,
                neighborChunkIds = new List<string> { service.chunkId }
            };
            manifest.chunks.Insert(manifest.chunks.IndexOf(service), definition);
            if (!service.neighborChunkIds.Contains(definition.chunkId)) service.neighborChunkIds.Add(definition.chunkId);
            var validation = manifest.ValidateManifest();
            if (!validation.IsValid) throw new InvalidOperationException(validation.Summary);
            EnsureLapRequest();
            EditorUtility.SetDirty(host.roadGraph); EditorUtility.SetDirty(world); EditorUtility.SetDirty(manifest);
            EditorSceneManager.MarkSceneDirty(persistent); EditorSceneManager.SaveScene(persistent);
            AssetDatabase.SaveAssets();
            Debug.Log("Thunder Bowl Speedway authored. Add " + ScenePath + " to Build Settings before running.");
        }

        private static void AddStop(Transform metadata, TruckTaxiSpeedwayLayout.Stop spec)
        {
            var go = new GameObject(spec.Name); go.transform.SetParent(metadata,false); go.transform.position = spec.Position;
            var stop = go.AddComponent<TruckTaxiRideLocation>();
            stop.locationId = spec.Id; stop.locationName = spec.Name; stop.district = "Speedway";
            stop.locationType = TaxiLocationType.Transit; stop.detectionRadius = 17;
            var bay = new GameObject("Truck Stop").transform; bay.SetParent(go.transform,false); stop.truckStopPoint = bay;
            var passenger = new GameObject("Passenger Spawn").transform; passenger.SetParent(go.transform,false);
            passenger.localPosition = new Vector3(7,0,0); stop.passengerSpawnPoint = passenger;
            var marker = go.AddComponent<TruckTaxiMapMarker>(); marker.stableId = spec.Id;
            marker.label = spec.Name; marker.markerType = RacetrackIcon();
        }
        private static TruckTaxiMapMarkerType RacetrackIcon()
        {
            return Enum.TryParse("Racetrack", out TruckTaxiMapMarkerType icon)
                ? icon : TruckTaxiMapMarkerType.SpecialEvent;
        }

        private static void EnsureLapRequest()
        {
            const string path = "Assets/LWS/TruckTaxi/ScriptableObjects/Requests/TakeALap.asset";
            var request = AssetDatabase.LoadAssetAtPath<PassengerRequestDefinition>(path);
            if (request == null)
            {
                request = ScriptableObject.CreateInstance<PassengerRequestDefinition>();
                request.requestType = TaxiRequestType.TakeALap;
                request.objectiveId = "taxi.objective.take-a-lap";
                request.description = "Take a lap at Thunder Bowl Speedway";
                request.targetId = TruckTaxiSpeedwayLayout.LapStopId;
                request.target = 1; request.timer = 360;
                request.bonusMoneyCents = 2500; request.bonusScore = 350;
                request.rewardTipMultiplier = 1.15f;
                request.dialogue = "One full lap. Keep it on the banking!";
                AssetDatabase.CreateAsset(request,path);
            }
            foreach (string name in new[] { "Max_Volume", "Joy_Ride" })
            {
                string profilePath = "Assets/LWS/TruckTaxi/ScriptableObjects/Passengers/" + name + ".asset";
                var profile = AssetDatabase.LoadAssetAtPath<PassengerProfile>(profilePath);
                if (profile == null) throw new InvalidOperationException("Racing passenger profile missing: " + profilePath);
                if (profile.possibleRequests != null && profile.possibleRequests.Contains(request)) continue;
                profile.possibleRequests = (profile.possibleRequests ?? Array.Empty<PassengerRequestDefinition>()).Concat(new[] { request }).ToArray();
                EditorUtility.SetDirty(profile);
            }
        }

        private static void BankEasyRoadsMeshes(Transform baked, IReadOnlyList<Vector3> centerline)
        {
            foreach (var filter in baked.GetComponentsInChildren<MeshFilter>())
            {
                Mesh mesh = filter.sharedMesh;
                if (mesh == null) continue;
                var vertices = mesh.vertices;
                for (int i=0;i<vertices.Length;i++)
                {
                    Vector3 world = filter.transform.TransformPoint(vertices[i]);
                    world.y += TruckTaxiSpeedwayLayout.BankOffset(world,centerline);
                    vertices[i] = filter.transform.InverseTransformPoint(world);
                }
                mesh.vertices = vertices; mesh.RecalculateNormals(); mesh.RecalculateBounds();
                var collider = filter.GetComponent<MeshCollider>();
                if (collider != null) { collider.sharedMesh = null; collider.sharedMesh = mesh; }
                EditorUtility.SetDirty(mesh);
            }
        }

        private static void BuildBowl(Transform root, IReadOnlyList<Vector3> track, Material concrete, Material seating, Material accent)
        {
            for (int i=0;i<track.Count;i+=2)
            {
                Vector3 a=track[i], b=track[(i+2)%track.Count];
                Vector3 tangent=(b-a).normalized, outward=new Vector3(-tangent.z,0,tangent.x);
                Vector3 middle=(a+b)*.5f;
                for (int tier=0;tier<3;tier++)
                {
                    var stand=Box("Grandstand tier " + tier,root,middle+outward*(29+tier*15)+Vector3.up*(8+tier*4),
                        new Vector3(15,2,Vector3.Distance(a,b)+2),tier==2 ? accent : seating,false);
                    stand.transform.rotation=Quaternion.LookRotation(tangent,Vector3.up);
                }
                var rail=Box("Oval safety rail",root,middle+outward*17+Vector3.up*8,
                    new Vector3(.5f,3,Vector3.Distance(a,b)+2),concrete);
                rail.transform.rotation=Quaternion.LookRotation(tangent,Vector3.up);
            }
        }
        private static void PaintTrackLanes(Transform root, IReadOnlyList<Vector3> track, Material paint)
        {
            for (int i=0;i<track.Count;i++)
            {
                Vector3 a=track[i], b=track[(i+1)%track.Count];
                Vector3 tangent=(b-a).normalized, outward=new Vector3(-tangent.z,0,tangent.x);
                foreach (float offset in new[] { -7f, 0f, 7f })
                {
                    Vector3 at=(a+b)*.5f+outward*offset;
                    at.y += TruckTaxiSpeedwayLayout.BankOffset(at,track)+.06f;
                    var stripe=Box("Lane stripe",root,at,new Vector3(.14f,.025f,Vector3.Distance(a,b)*.55f),paint,false);
                    stripe.transform.rotation=Quaternion.LookRotation(tangent,Vector3.up);
                }
            }
        }
        private static GameObject Box(string name, Transform parent, Vector3 position, Vector3 size, Material material, bool collision=true)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube); go.name=name;
            go.transform.SetParent(parent,false); go.transform.position=position; go.transform.localScale=size;
            go.GetComponent<Renderer>().sharedMaterial=material;
            if (!collision) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }
        private static Material Material(string name, Color color)
        {
            string path=MaterialFolder+"/"+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material!=null) return material;
            material=new Material(Shader.Find("Universal Render Pipeline/Lit")) { color=color };
            AssetDatabase.CreateAsset(material,path); return material;
        }
        private static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash=path.LastIndexOf('/'); Folder(path.Substring(0,slash));
            AssetDatabase.CreateFolder(path.Substring(0,slash),path.Substring(slash+1));
        }
        private static void RequireCleanScenes()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode before regional authoring.");
            for (int i=0;i<SceneManager.sceneCount;i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save open scene changes before regional authoring.");
        }
    }
}
