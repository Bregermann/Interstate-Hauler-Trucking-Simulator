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
    // One-time conversion of the existing authored town, not a replacement demo generator.
    public static class TruckTaxiRegionalAuthoring
    {
        public const string Root = "Assets/LWS/TruckTaxi/Regional";
        public static readonly string[] Names = { "Town01", "Town01Outskirts", "Highway", "RiverBridge", "ServiceArea", "Town02Outskirts", "Town02", "RailCorridor" };
        public static string[] RegionScenePaths => Names.Select(n => Root + "/Scenes/Taxi_" + n + ".unity").ToArray();
        private static readonly float[] Starts = { -550, 360, 960, 1460, 2500, 3060, 3600, -450 };
        private static readonly float[] Ends = { 400, 1000, 1500, 2540, 3100, 3640, 4550, 4450 };
        private static Material asphalt, concrete, grass, steel, stripe;

        [MenuItem("Truck Taxi/Regional/Convert Existing City To Streamed Region")]
        public static void Build()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save scene changes before regional authoring.");
            var persistent = EditorSceneManager.OpenScene(TruckTaxiDemoBuilder.ScenePath);
            var host = Object.FindFirstObjectByType<TruckTaxiBootstrap>();
            if (host.GetComponent<TruckTaxiRegionalWorld>() != null)
            { Debug.Log("TRUCK TAXI REGIONAL: already authored. Existing designer content preserved."); return; }
            Folder(Root); Folder(Root + "/Scenes"); Folder(Root + "/Meshes"); Folder(Root + "/Materials");
            asphalt = AssetDatabase.LoadAssetAtPath<Material>(TruckTaxiDemoBuilder.Root + "/Materials/Asphalt.mat") ?? Material("Regional Asphalt", new Color(.16f,.19f,.2f));
            concrete = Material("Regional Concrete", new Color(.59f,.65f,.66f));
            grass = Material("Regional Ground", new Color(.22f,.39f,.19f));
            steel = Material("Regional Steel", new Color(.35f,.43f,.47f));
            stripe = Material("Regional Markings", new Color(.96f,.95f,.82f));
            var town01 = GameObject.Find("Truck Taxi Demo City");
            if (town01 == null) throw new InvalidOperationException("Existing town scenery was not found; refusing to regenerate it.");
            var town02 = Object.Instantiate(town01); town02.name = "Town 02 Scenery";
            town02.transform.position += Vector3.right * 4000;
            string[] townNames = { "Riverside Offices", "Eastbank Diner", "Bridge End Cafe", "Eastbank Station", "Willow Gardens", "Mill Apartments", "Orchard House", "Eastbank Market", "River View Hotel", "Eastbank Bus Terminal", "Harbor Arcade", "Sawmill Warehouse", "Canal Coffee", "Foundry Annex", "Eastbank Clinic", "Canal Inn", "River Overlook", "Railway Tavern", "Eastbank Service", "Station Records" };
            var stops = town02.GetComponentsInChildren<TruckTaxiRideLocation>();
            Array.Sort(stops, (a,b) => string.CompareOrdinal(a.locationId,b.locationId));
            for (int i = 0; i < stops.Length; i++)
            {
                stops[i].locationId = "taxi.town02.stop." + i.ToString("00");
                stops[i].locationName = i < townNames.Length ? townNames[i] : "Eastbank Stop " + i;
                // District category is authored passenger eligibility; town identity is regional metadata.
                stops[i].name = stops[i].locationName;
                foreach (var label in stops[i].GetComponentsInChildren<TMP_Text>()) label.text = stops[i].locationName + "\nTAXI";
            }
            foreach (var target in town02.GetComponentsInChildren<TruckTaxiImpactTarget>()) target.targetId = "town02." + target.targetId;
            foreach (var shortcut in town02.GetComponentsInChildren<ShortcutTrigger>()) shortcut.shortcutId = "town02." + shortcut.shortcutId;
            foreach (var marker in town02.GetComponentsInChildren<TruckTaxiMapMarker>()) marker.stableId = "town02." + marker.stableId;
            OpenBarrier(town01.transform, "East barrier", 395);
            OpenBarrier(town02.transform, "West barrier", 3605);
            var metadata = new GameObject("Truck Taxi Regional Metadata");
            ExtractStops(town01.transform, metadata.transform);
            ExtractStops(town02.transform, metadata.transform);
            AddServices(stops);
            var region = host.gameObject.AddComponent<TruckTaxiRegionalWorld>();
            region.regions = Names.Select((name,i) => new TruckTaxiRegion {
                id = "taxi." + name.ToLowerInvariant(), displayName = name, sceneName = "Taxi_" + name,
                bounds = new Bounds(new Vector3((Starts[i]+Ends[i])*.5f, 0, i==7 ? 510 : 0), new Vector3(Ends[i]-Starts[i],200,i==7 ? 100 : 1000)), population = i != 7 }).ToArray();
            ConfigureStreaming(region);
            var roots = new GameObject[Names.Length]; roots[0] = town01; roots[6] = town02;
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i] == null) roots[i] = new GameObject("Taxi_" + Names[i]);
                roots[i].name = "Taxi_" + Names[i];
                if (i != 0 && i != 6 && i != 7) BuildCorridor(roots[i].transform, i);
            }
            BuildRiver(roots[3].transform);
            BuildRegionalStops(metadata.transform, roots);
            var graph = host.roadGraph.Graph;
            ExtendGraph(graph);
            host.roadGraph.SetGraph(graph, false);
            ExtendTraffic(host.traffic);
            ExtendPedestrianPaths(host.pedestrians);
            // Metadata remains in the persistent scene. Only scenery/colliders live in chunks.
            for (int i = 0; i < roots.Length; i++)
            {
                var chunk = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                SceneManager.MoveGameObjectToScene(roots[i], chunk);
                EditorSceneManager.SaveScene(chunk, RegionScenePaths[i]);
                EditorSceneManager.CloseScene(chunk, true);
            }
            SceneManager.SetActiveScene(persistent);
            var profile = host.traffic.densityProfile;
            profile.trafficDensityMultiplier = 12.5f; profile.maximumActiveTraffic = 150;
            profile.maximumActivePedestrians = 720; profile.spawnRadius = profile.despawnRadius = 0;
            EditorUtility.SetDirty(profile);
            EditorSceneManager.MarkSceneDirty(persistent); EditorSceneManager.SaveScene(persistent);
            var builds = EditorBuildSettings.scenes.ToList();
            foreach (string path in RegionScenePaths) if (!builds.Any(s => s.path == path)) builds.Add(new EditorBuildSettingsScene(path,true));
            EditorBuildSettings.scenes = builds.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("TRUCK TAXI REGIONAL: authored 8 streamed regions, persistent cross-region routing/stops, Town 02 and EasyRoads bridge corridor.");
        }
        [MenuItem("Truck Taxi/Regional/Configure Existing Streaming Manifest")]
        public static void ConfigureExistingStreaming()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save scene changes first.");
            var region = Object.FindFirstObjectByType<TruckTaxiRegionalWorld>();
            if (region == null) throw new InvalidOperationException("Open the persistent Truck Taxi scene first.");
            ConfigureStreaming(region);
            EditorSceneManager.MarkSceneDirty(region.gameObject.scene);
            EditorSceneManager.SaveScene(region.gameObject.scene);
            AssetDatabase.SaveAssets();
        }
        private static void ConfigureStreaming(TruckTaxiRegionalWorld region)
        {
            string policyPath = Root + "/Taxi_StreamingPolicy.asset", manifestPath = Root + "/Taxi_StreamingManifest.asset";
            var policy = AssetDatabase.LoadAssetAtPath<LwsWorldStreamingPolicy>(policyPath);
            if (policy == null)
            {
                policy = ScriptableObject.CreateInstance<LwsWorldStreamingPolicy>();
                policy.loadAheadDistanceMeters = 450; policy.keepBehindDistanceMeters = 250; policy.preloadMarginMeters = 150;
                policy.unloadDistanceMeters = 850; policy.unloadHysteresisMeters = 200; policy.minimumLoadedNeighborCount = 1;
                policy.maximumConcurrentLoads = 2; policy.evaluationIntervalSeconds = .5f;
                AssetDatabase.CreateAsset(policy, policyPath);
            }
            var manifest = AssetDatabase.LoadAssetAtPath<LwsWorldStreamingManifest>(manifestPath);
            if (manifest == null)
            {
                manifest = ScriptableObject.CreateInstance<LwsWorldStreamingManifest>();
                manifest.worldId = "taxi.regional"; manifest.policy = policy;
                for (int i = 0; i < region.regions.Length; i++)
                {
                    var r = region.regions[i];
                    var chunk = new LwsWorldChunkDefinition { chunkId = r.id, displayName = r.displayName, sceneName = r.sceneName,
                        scenePath = RegionScenePaths[i], boundsCenter = r.bounds.center, boundsSize = r.bounds.size,
                        containsTrafficPresentation = r.population, containsRoadGeometry = r.population };
                    if (i > 0 && i < 7) chunk.neighborChunkIds.Add(region.regions[i-1].id);
                    if (i < 6) chunk.neighborChunkIds.Add(region.regions[i+1].id);
                    if (i == 0 || i == 6) chunk.neighborChunkIds.Add(region.regions[7].id);
                    manifest.chunks.Add(chunk);
                }
                AssetDatabase.CreateAsset(manifest, manifestPath);
            }
            region.streamingManifest = manifest;
            if (!manifest.ValidateManifest().IsValid) throw new InvalidOperationException(manifest.ValidateManifest().Summary);
            EditorUtility.SetDirty(region);
        }
        private static void ExtractStops(Transform world, Transform metadata)
        {
            foreach (var stop in world.GetComponentsInChildren<TruckTaxiRideLocation>())
            {
                var art = new GameObject(stop.name + " scenery").transform; art.SetParent(world, false);
                foreach (var child in stop.transform.Cast<Transform>().ToArray())
                    if (child != stop.truckStopPoint && child != stop.passengerSpawnPoint) child.SetParent(art, true);
                stop.transform.SetParent(metadata, true);
            }
        }
        private static void AddServices(TruckTaxiRideLocation[] stops)
        {
            foreach (int i in new[] { 1, 7, 8, 18 })
            {
                if (i >= stops.Length) continue;
                var stop = stops[i]; var go = stop.gameObject;
                var service = go.GetComponent<TruckTaxiServicePoint>() ?? go.AddComponent<TruckTaxiServicePoint>();
                service.stableId = stop.locationId + ".service"; service.displayName = stop.locationName; service.location = stop;
                service.capabilities = TruckTaxiServiceCapability.Restroom | TruckTaxiServiceCapability.DisposeWaste | TruckTaxiServiceCapability.CleanCab;
                var bathroom = go.GetComponent<TruckTaxiBathroomPoint>() ?? go.AddComponent<TruckTaxiBathroomPoint>();
                bathroom.stableId = service.stableId + ".restroom"; bathroom.displayName = stop.locationName; bathroom.location = stop;
                if (i != 18)
                {
                    var store = go.GetComponent<TruckTaxiStorePoint>() ?? go.AddComponent<TruckTaxiStorePoint>();
                    store.stableId = service.stableId + ".store"; store.location = stop; store.displayName = stop.locationName;
                    service.capabilities |= TruckTaxiServiceCapability.Store | TruckTaxiServiceCapability.Food | TruckTaxiServiceCapability.Drink | TruckTaxiServiceCapability.PissJugs;
                }
                else
                {
                    var gas = go.GetComponent<TruckTaxiGasStationPoint>() ?? go.AddComponent<TruckTaxiGasStationPoint>();
                    gas.stableId = service.stableId + ".gas"; gas.displayName = stop.locationName; gas.stoppingRadius = 11;
                    service.capabilities |= TruckTaxiServiceCapability.Refuel | TruckTaxiServiceCapability.RecoverVehicle | TruckTaxiServiceCapability.RepairEngine | TruckTaxiServiceCapability.RepairTransmission | TruckTaxiServiceCapability.RepairGeneralDamage | TruckTaxiServiceCapability.RepairTires;
                }
            }
        }
        public static float RoadHeight(float x) => x < 1460 || x > 2540 ? .14f : x < 1780 ? Mathf.Lerp(.14f,8,(x-1460)/320) : x > 2220 ? Mathf.Lerp(8,.14f,(x-2220)/320) : 8;
        private static void BuildCorridor(Transform root, int index)
        {
            float from = Starts[index], to = Ends[index];
            if (index != 3) Box("Regional ground",root,new Vector3((from+to)*.5f,-.55f,0),new Vector3(to-from,1,1000),grass);
            else
            {
                Box("West river bank",root,new Vector3(1620,-.55f,0),new Vector3(320,1,1000),grass);
                Box("East river bank",root,new Vector3(2380,-.55f,0),new Vector3(320,1,1000),grass);
            }
            var line = new List<Vector3>();
            for (float x=from; x<to; x+=40) line.Add(new Vector3(x,RoadHeight(x),0));
            line.Add(new Vector3(to,RoadHeight(to),0));
            TruckTaxiDemoBuilder.BakeRegionalRoads(root,new List<Vector3[]> { line.ToArray() },28,"Regional_"+Names[index],asphalt,Root+"/Meshes");
            for (float x=from; x<to-8; x+=18)
                foreach(float z in new[] {-7f,0f,7f}) Box("Highway lane marking",root,new Vector3(x,RoadHeight(x)+.05f,z),new Vector3(8,.02f,.15f),stripe,false);
            if (index == 1 || index == 5)
            {
                float center=(from+to)*.5f;
                Vector3[] ramp={new Vector3(from,.16f,0),new Vector3(center-100,.16f,-65),new Vector3(center+100,.16f,-65),new Vector3(to,.16f,0)};
                TruckTaxiDemoBuilder.BakeRegionalRoads(root,new List<Vector3[]> { ramp },10,"Ramp_"+index,asphalt,Root+"/Meshes");
                Box("Interchange overpass",root,new Vector3(center,7,0),new Vector3(18,1,185),concrete);
                foreach(float z in new[] {-42f,42f}) Box("Overpass support",root,new Vector3(center,3,z),new Vector3(8,6,3),concrete);
            }
            if (index == 3)
            {
                for(float x=from; x<to-20; x+=20)
                    foreach(float z in new[] {-14.5f,14.5f})
                    {
                        var rail=Box("Bridge guardrail",root,new Vector3(x+10,RoadHeight(x+10)+.85f,z),new Vector3(20,.9f,.35f),steel);
                        rail.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(RoadHeight(x+20)-RoadHeight(x),20)*Mathf.Rad2Deg);
                    }
                for(float x=1800; x<=2200; x+=100) Box("Bridge pier",root,new Vector3(x,1,0),new Vector3(5,14,24),concrete);
                Box("Bridge deck",root,new Vector3(2000,7.4f,0),new Vector3(440,1,28),concrete);
            }
        }
        private static void BuildRiver(Transform root)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/WeatherMaker/Prefab/WeatherMakerWaterPrefab.prefab");
            if(prefab==null) throw new InvalidOperationException("Installed Weather Maker water prefab was not found.");
            var water=(GameObject)PrefabUtility.InstantiatePrefab(prefab); water.name="Regional River - Weather Maker"; water.transform.SetParent(root,false);
            ConfigureRiverReflections(water);
            water.transform.position=new Vector3(2000,-1.5f,0);
            var renderer=water.GetComponentInChildren<Renderer>();
            if(renderer!=null)
            {
                var size=renderer.bounds.size;
                water.transform.localScale=Vector3.Scale(water.transform.localScale,new Vector3(440/Mathf.Max(.01f,size.x),1,1200/Mathf.Max(.01f,size.z)));
            }
            foreach(var collider in water.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(collider);
            foreach(float x in new[] {1770f,2230f}) Box("River shoreline",root,new Vector3(x,-.9f,0),new Vector3(20,1,1200),concrete);
        }
        public static void ConfigureRiverReflections(GameObject water)
        {
            // The installed planar renderer issues destination-less URP RenderRequests. Its public
            // mask=0 option disables only optional reflections, retaining vendor water presentation.
            foreach(var component in water.GetComponentsInChildren<MonoBehaviour>(true))
                if(component!=null && component.GetType().FullName=="DigitalRuby.WeatherMaker.WeatherMakerPlanarReflectionScript")
                {
                    component.GetType().GetField("ReflectionMask").SetValue(component,(LayerMask)0);
                    component.enabled=false;
                    EditorUtility.SetDirty(component);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                }
        }
        private static void BuildRegionalStops(Transform metadata, GameObject[] roots)
        {
            AddStop(metadata, roots[4].transform,"taxi.service.river","River Bridge Services",new Vector3(2800,.15f,-45),true);
            AddStop(metadata, roots[7].transform,"taxi.station.west","Town 01 Rail Station",new Vector3(0,.15f,355),false);
            AddStop(metadata, roots[7].transform,"taxi.station.east","Town 02 Rail Station",new Vector3(4000,.15f,355),false);
        }
        private static void AddStop(Transform metadata,Transform art,string id,string title,Vector3 position,bool service)
        {
            var go=new GameObject(title); go.transform.SetParent(metadata,false); go.transform.position=position;
            var stop=go.AddComponent<TruckTaxiRideLocation>(); stop.locationId=id; stop.locationName=title; stop.locationType=service?TaxiLocationType.GasStation:TaxiLocationType.Transit;
            stop.district=service ? "Industrial" : "Downtown";
            var bay=new GameObject("Truck Stop").transform; bay.SetParent(go.transform,false); stop.truckStopPoint=bay;
            var person=new GameObject("Passenger Spawn").transform; person.SetParent(go.transform,false); person.localPosition=Vector3.right*7; stop.passengerSpawnPoint=person;
            Box("Paved access apron",art,position+Vector3.down*.1f,new Vector3(80,.2f,110),asphalt);
            Box(title+" building",art,position+new Vector3(30,4,service ? -35 : 35),new Vector3(24,8,25),concrete);
            var sign=new GameObject(title+" sign").AddComponent<TextMeshPro>(); sign.transform.SetParent(art,false); sign.transform.position=position+new Vector3(15,5,0);
            sign.text=title; sign.fontSize=5; sign.rectTransform.sizeDelta=new Vector2(30,5); sign.alignment=TextAlignmentOptions.Center;
            if(service)
            {
                var s=go.AddComponent<TruckTaxiServicePoint>(); s.stableId=id; s.displayName=title; s.location=stop;
                s.capabilities=TruckTaxiServiceCapability.Refuel | TruckTaxiServiceCapability.RecoverVehicle | TruckTaxiServiceCapability.RepairEngine |
                    TruckTaxiServiceCapability.RepairTransmission | TruckTaxiServiceCapability.RepairGeneralDamage | TruckTaxiServiceCapability.RepairTires |
                    TruckTaxiServiceCapability.Restroom | TruckTaxiServiceCapability.DisposeWaste | TruckTaxiServiceCapability.CleanCab |
                    TruckTaxiServiceCapability.Store | TruckTaxiServiceCapability.Food | TruckTaxiServiceCapability.Drink | TruckTaxiServiceCapability.PissJugs;
                var gas=go.AddComponent<TruckTaxiGasStationPoint>(); gas.stableId=id; gas.displayName=title; gas.stoppingRadius=11;
                var store=go.AddComponent<TruckTaxiStorePoint>(); store.stableId=id; store.displayName=title; store.location=stop;
                var bathroom=go.AddComponent<TruckTaxiBathroomPoint>(); bathroom.stableId=id; bathroom.displayName=title; bathroom.location=stop;
            }
        }
        private static void ExtendGraph(LwsRoadGraph graph)
        {
            var oldNodes=graph.nodes.ToArray(); var oldEdges=graph.edges.ToArray();
            foreach(var node in oldNodes) graph.nodes.Add(new LwsRoadNode { nodeId="town02."+node.nodeId,position=node.position+Vector3.right*4000,regionId="taxi.town02",sceneChunkId="Taxi_Town02" });
            foreach(var edge in oldEdges)
            {
                var copy=JsonUtility.FromJson<LwsRoadEdge>(JsonUtility.ToJson(edge));
                copy.edgeId="town02."+edge.edgeId; copy.roadId="town02."+edge.roadId; copy.segmentId="town02."+edge.segmentId;
                copy.fromNodeId="town02."+edge.fromNodeId; copy.toNodeId="town02."+edge.toNodeId; copy.stateOrRegionId="taxi.town02"; copy.sceneChunkId="Taxi_Town02";
                foreach(var sample in copy.samples) { sample.position+=Vector3.right*4000; sample.roadId=copy.roadId; sample.segmentId=copy.segmentId; }
                graph.edges.Add(copy);
            }
            var west=oldNodes.Where(n=>n.position.x<325).OrderBy(n=>(n.position-new Vector3(320,.15f,0)).sqrMagnitude).First();
            var east=graph.nodes.Where(n=>n.nodeId.StartsWith("town02.")).OrderBy(n=>(n.position-new Vector3(3680,.15f,0)).sqrMagnitude).First();
            var previous=west;
            for(int x=360; x<=3640; x+=40)
            {
                var node=new LwsRoadNode { nodeId="taxi.regional."+x,position=new Vector3(x,RoadHeight(x),0),regionId="taxi.regional",sceneChunkId="regional" };
                graph.nodes.Add(node); Link(graph,previous,node,LwsRoadClass.Interstate,55); previous=node;
            }
            Link(graph,previous,east,LwsRoadClass.LocalRoad,30);
            foreach (int regionIndex in new[] {1,5})
            {
                float from=Starts[regionIndex], to=Ends[regionIndex], center=(from+to)*.5f;
                var a=graph.nodes.OrderBy(n=>(n.position-new Vector3(from,.14f,0)).sqrMagnitude).First();
                var b=graph.nodes.OrderBy(n=>(n.position-new Vector3(to,.14f,0)).sqrMagnitude).First();
                var first=new LwsRoadNode { nodeId="taxi.ramp."+regionIndex+".entry",position=new Vector3(center-100,.16f,-65),regionId="taxi.regional",sceneChunkId="Taxi_"+Names[regionIndex] };
                var last=new LwsRoadNode { nodeId="taxi.ramp."+regionIndex+".exit",position=new Vector3(center+100,.16f,-65),regionId="taxi.regional",sceneChunkId=first.sceneChunkId };
                graph.nodes.Add(first); graph.nodes.Add(last);
                Link(graph,a,first,LwsRoadClass.Ramp,25); Link(graph,first,last,LwsRoadClass.Ramp,25); Link(graph,last,b,LwsRoadClass.Ramp,25);
            }
            foreach(var stop in Object.FindObjectsByType<TruckTaxiRideLocation>(FindObjectsSortMode.None).Where(s=>s.locationId.StartsWith("taxi.station.") || s.locationId=="taxi.service.river"))
            {
                var near=graph.nodes.OrderBy(n=>(n.position-stop.StopPosition).sqrMagnitude).First();
                var node=new LwsRoadNode { nodeId=stop.locationId,position=stop.StopPosition,regionId=stop.district,sceneChunkId="regional" };
                graph.nodes.Add(node); Link(graph,near,node,LwsRoadClass.LocalRoad,15);
            }
            graph.graphId="truck-taxi-regional-v1";
        }
        private static void Link(LwsRoadGraph graph,LwsRoadNode from,LwsRoadNode to,LwsRoadClass roadClass,float speed)
        {
            foreach(bool reverse in new[] {false,true})
            {
                var a=reverse?to:from; var b=reverse?from:to; float distance=Vector3.Distance(a.position,b.position);
                string id=a.nodeId+">"+b.nodeId;
                graph.edges.Add(new LwsRoadEdge { edgeId=id,roadId=id,segmentId=id,fromNodeId=a.nodeId,toNodeId=b.nodeId,
                    roadClass=roadClass,direction=reverse?LwsRoadDirection.Westbound:LwsRoadDirection.Eastbound,surfaceType=LwsRoadSurfaceType.AsphaltInterstate,
                    oneWay=true,distanceMeters=distance,travelCost=distance,speedLimitMph=speed,laneCount=2,laneWidthMeters=3.5f,stateOrRegionId="taxi.regional",sceneChunkId="regional",
                    samples=new List<LwsRoadSample> { new LwsRoadSample { position=a.position,forward=(b.position-a.position).normalized,roadWidthMeters=28,laneWidthMeters=3.5f,speedLimitMph=speed },new LwsRoadSample { position=b.position,forward=(b.position-a.position).normalized,roadWidthMeters=28,laneWidthMeters=3.5f,speedLimitMph=speed,distanceFromStartMeters=distance } } });
            }
        }
        private static void ExtendTraffic(TruckTaxiTrafficAdapter traffic)
        {
            var lanes=traffic.cityLanes.ToList();
            foreach(var lane in traffic.cityLanes)
            {
                var copy=JsonUtility.FromJson<LwsTrafficLaneDefinition>(JsonUtility.ToJson(lane));
                copy.laneId="town02."+lane.laneId; copy.roadId="taxi.city.loop.town02."+lane.laneId; copy.edgeId="town02."+lane.edgeId; copy.segmentId="town02."+lane.segmentId;
                copy.centerline=copy.centerline.Select(p=>p+Vector3.right*4000).ToArray(); lanes.Add(copy);
            }
            // A physical return loop uses both carriageways and the towns' existing end streets.
            for(int side=0;side<2;side++)
            {
                var points=new List<Vector3>(); float z=-3.5f-side*3.5f;
                for(float x=320;x<=3680;x+=20) points.Add(new Vector3(x,RoadHeight(x),z));
                points.Add(new Vector3(3840,.14f,z)); points.Add(new Vector3(3840,.14f,160));
                points.Add(new Vector3(3680,.14f,160)); points.Add(new Vector3(3680,.14f,-z));
                for(float x=3660;x>=320;x-=20) points.Add(new Vector3(x,RoadHeight(x),-z));
                points.Add(new Vector3(160,.14f,-z)); points.Add(new Vector3(160,.14f,-160));
                points.Add(new Vector3(320,.14f,-160));
                lanes.Add(new LwsTrafficLaneDefinition { laneId="taxi.regional.lane."+side,roadId="taxi.regional.highway",segmentId="taxi.regional.highway",edgeId="taxi.regional.highway",laneIndex=side,
                    roadClass=LwsRoadClass.Interstate,direction=LwsRoadDirection.Bidirectional,laneWidthMeters=3.5f,speedLimitMph=50,lengthMeters=6800,centerline=points.ToArray() });
            }
            traffic.cityLanes=lanes.ToArray();
        }
        private static void ExtendPedestrianPaths(TruckTaxiPedestrianPopulation population)
        {
            var paths=population.peoplePaths.ToList();
            foreach(var path in population.peoplePaths)
            {
                var clone=Object.Instantiate(path.gameObject,population.transform); clone.name="Town 02 "+path.name; clone.transform.position+=Vector3.right*4000;
                var component=clone.GetComponent(path.GetType());
                var points=(List<Vector3>)path.GetType().GetField("pathPoint").GetValue(component);
                for(int i=0;i<points.Count;i++) points[i]+=Vector3.right*4000;
                paths.Add(component);
            }
            population.peoplePaths=paths.ToArray();
        }
        private static void OpenBarrier(Transform parent,string name,float x)
        {
            var old=parent.Find(name); if(old==null) return;
            Object.DestroyImmediate(old.gameObject);
            foreach(float z in new[] {-215f,215f}) Box(name+" regional opening",parent,new Vector3(x,1.5f,z),new Vector3(2,3,350),concrete);
        }
        private static GameObject Box(string name,Transform parent,Vector3 position,Vector3 size,Material material,bool collision=true)
        { var go=GameObject.CreatePrimitive(PrimitiveType.Cube); go.name=name; go.transform.SetParent(parent,false); go.transform.position=position; go.transform.localScale=size; go.GetComponent<Renderer>().sharedMaterial=material; if(!collision) Object.DestroyImmediate(go.GetComponent<Collider>()); if(name=="Regional ground" || name.EndsWith("river bank")) go.AddComponent<TruckTaxiSurface>(); return go; }
        private static Material Material(string name,Color color)
        { string path=Root+"/Materials/"+name+".mat"; var m=AssetDatabase.LoadAssetAtPath<Material>(path); if(m==null) { m=new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m,path); } m.color=color; EditorUtility.SetDirty(m); return m; }
        private static void Folder(string path)
        { if(AssetDatabase.IsValidFolder(path)) return; int split=path.LastIndexOf('/'); Folder(path.Substring(0,split)); AssetDatabase.CreateFolder(path.Substring(0,split),path.Substring(split+1)); }
    }
}
