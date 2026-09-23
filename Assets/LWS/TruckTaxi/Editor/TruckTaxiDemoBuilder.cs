using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using LWS.InterstateHauler;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace LWS.TruckTaxi.Editor
{
    public static class TruckTaxiDemoBuilder
    {
        public const string Root = "Assets/LWS/TruckTaxi";
        public const string ScenePath = Root+"/Scenes/TruckTaxi_DemoCity.unity";
        private static Material asphalt, grass, concrete, yellow, teal, coral;
        [MenuItem("Truck Taxi/Create Or Refresh Demo City")]
        public static void CreateDemo()
        {
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            foreach(string folder in new[]{"Scenes","Prefabs","ScriptableObjects/Passengers","ScriptableObjects/Requests","UI","Audio","Materials","DemoContent/Meshes","Documentation"})
                Directory.CreateDirectory(Root+"/"+folder);
            AssetDatabase.Refresh();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            asphalt=Material("Asphalt",new Color(.12f,.14f,.15f));
            grass=Material("Park green",new Color(.25f,.4f,.22f));
            concrete=Material("Concrete",new Color(.49f,.52f,.51f));
            yellow=Material("Taxi yellow",new Color(1,.78f,.12f));
            teal=Material("Harbor teal",new Color(.17f,.47f,.5f));
            coral=Material("Brick red",new Color(.65f,.25f,.2f));
            var config=CreateContent();
            var city=new GameObject("Truck Taxi Demo City");
            Box("Ground",city.transform,new Vector3(0,-.55f,0),new Vector3(1100,1,1100),grass);
            BakeEasyRoads(city.transform);
            var graphObject=new GameObject("LWS Demo City Road Graph");
            var graph=graphObject.AddComponent<LwsRoadGraphProvider>(); graph.SetGraph(CreateGraph(),false);
            BuildDistricts(city.transform);
            BuildStops(city.transform);
            BuildShortcutsAndProps(city.transform);
            BuildLighting();

            var services=new GameObject("Interstate Driving Services").AddComponent<LwsApplicationBootstrap>();
            Set(services,"drivingSandbox",true); Set(services,"dontDestroyOnLoad",false);
            var spawn=new GameObject("Tractor Spawn").transform;
            spawn.position=new Vector3(-315,1.6f,-280);
            var spawner=new GameObject("Existing LWS Tractor Spawner").AddComponent<LwsPlayerTruckSpawner>();
            Set(spawner,"fallbackPlayerTruckPrefab",CreateTaxiTractor());
            Set(spawner,"truckSpawnPoint",spawn);
            Set(spawner,"transmissionDefinition",AssetDatabase.LoadAssetAtPath<Lws18SpeedTransmissionDefinition>("Assets/LWS/InterstateHauler/Vehicles/Transmission/Data/IH_18SpeedTransmission_G29_EatonDevelopment.asset"));
            Set(spawner,"addDebugPanel",false); Set(spawner,"buildValidationRoadside",false); Set(spawner,"spawnOnStart",false);
            // No TruckDefinition or trailerPrefab: canonical spawner cannot pull in the freight validation trailer.
            var host=new GameObject("Truck Taxi Bootstrap").AddComponent<TruckTaxiBootstrap>();
            host.configuration=config; host.spawner=spawner; host.roadGraph=graph; host.playerSpawn=spawn;
            host.traffic=new GameObject("UTS Taxi Traffic").AddComponent<TruckTaxiTrafficAdapter>();
            host.traffic.trafficPrefabs=new[]{"Car_1","Car_3","Taxi"}.Select(n=>AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UTS_FullPack/Models/Cars/Car_Prefabs/Day Cars/"+n+".prefab")).ToArray();
            if(host.traffic.trafficPrefabs.Any(p=>p==null)) throw new InvalidOperationException("UTS vehicle prefab missing.");
            host.traffic.cityLanes=CreateTrafficLanes();
            host.pedestrians=BuildPedestrianPaths();
            host.hud=host.gameObject.AddComponent<TruckTaxiHud>();
            host.hud.heatButtonPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Heat - Complete Modern UI/Prefabs/UI Elements/Button/Button.prefab");
            host.hud.font=TMP_Settings.defaultFontAsset;
            if(host.hud.heatButtonPrefab==null) throw new InvalidOperationException("Heat button prefab missing.");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene,ScenePath);
            var scenes=EditorBuildSettings.scenes.ToList();
            if(!scenes.Any(s=>s.path==ScenePath)) scenes.Add(new EditorBuildSettingsScene(ScenePath,true));
            EditorBuildSettings.scenes=scenes.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("TRUCK TAXI GENERATED: 20 locations, 10 passengers, 11 requests; existing NWH/Compass/UTS reused.");
        }

        private static TruckTaxiConfiguration CreateContent()
        {
            var config=Asset<TruckTaxiConfiguration>(Root+"/ScriptableObjects/TruckTaxi_DemoConfiguration.asset");
            var descriptions=new[]{"Beat the clock","Use a shortcut","Ram one traffic car","Bump one fictional pedestrian","Knock over roadside clutter",
                "Spend 5 seconds offroad","Keep the ride smooth","Arrive without a collision","Make 400 chaos points","Visit a scenic overlook","Thread a near miss"};
            var defs=new List<PassengerRequestDefinition>();
            for(int i=0;i<descriptions.Length;i++)
            {
                string path=Root+"/ScriptableObjects/Requests/"+((TaxiRequestType)i)+".asset";
                bool exists=AssetDatabase.LoadAssetAtPath<PassengerRequestDefinition>(path)!=null;
                var r=Asset<PassengerRequestDefinition>(path);
                if(!exists || string.IsNullOrWhiteSpace(r.description))
                {
                    r.requestType=(TaxiRequestType)i; r.description=descriptions[i]; r.timer=i==0?75:180;
                    r.target=i==5?5:i==8?400:1; r.bonusMoneyCents=300+i*50; r.bonusScore=100+i*10;
                    r.dialogue=descriptions[i]+". I will put it in the review."; r.cooldown=35;
                    EditorUtility.SetDirty(r);
                }
                defs.Add(r);
            }
            string[] names={"Pat Pending","Max Volume","Joy Ride","Bill Rush","Dot Matrix","Nora Brake","Al Lee","Ray Rage","Cam Era","Robin Roundabout"};
            string[] types={"Normal Commuter","Adrenaline Junkie","Chaos Tourist","Impatient Executive","Conspiracy Nut","Extremely Nervous Passenger","Shortcut Fanatic","Road Rage Passenger","Tourist","Weird Local"};
            int[][] choices={new[]{0,6,7},new[]{0,1,8,10},new[]{2,3,4,8},new[]{0,1},new[]{1,5,9},new[]{6,7},new[]{1,5},new[]{2,4,8},new[]{6,9},new[]{3,5,10}};
            var profiles=new List<PassengerProfile>();
            for(int i=0;i<names.Length;i++)
            {
                string path=Root+"/ScriptableObjects/Passengers/"+names[i].Replace(" ","_")+".asset";
                bool exists=AssetDatabase.LoadAssetAtPath<PassengerProfile>(path)!=null;
                var p=Asset<PassengerProfile>(path);
                if(!exists || string.IsNullOrWhiteSpace(p.passengerName))
                {
                    p.passengerName=names[i]; p.personality=types[i]; p.preferredDrivingStyle=i==0||i==5||i==8?"Calm":"Chaotic";
                    p.chaosAffinity=i==0||i==5||i==8?-1:1; p.basePatience=i==3?140:300;
                    p.possibleRequests=choices[i].Select(x=>defs[x]).ToArray();
                    p.dialogueSet=new[]{"I ordered the XL. This feels accurate.","Five stars for the turning radius. Eventually."};
                    p.collisionReaction=p.chaosAffinity>0?"That is going in my highlight reel!":"My insurance agent just felt a disturbance.";
                    p.shortcutReaction="A bold interpretation of a road."; EditorUtility.SetDirty(p);
                }
                profiles.Add(p);
            }
            config.passengers=profiles.ToArray(); config.requests=defs.ToArray(); EditorUtility.SetDirty(config);
            return config;
        }
        private static GameObject CreateTaxiTractor()
        {
            const string sourcePath="Assets/LWS/InterstateHauler/Vehicles/Prefabs/IH_PlayerTruck_NWH.prefab";
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            var instance=Object.Instantiate(source);
            instance.name="Truck Taxi Interstate Tractor";
            if(PrefabUtility.IsPartOfPrefabInstance(instance))
                PrefabUtility.UnpackPrefabInstance(instance,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            var result=PrefabUtility.SaveAsPrefabAsset(instance,Root+"/Prefabs/TruckTaxi_InterstateTractor.prefab");
            Object.DestroyImmediate(instance);
            return result;
        }
        public static LwsRoadGraph CreateGraph()
        {
            var graph=new LwsRoadGraph{graphId="truck-taxi.demo-city"};
            for(int x=0;x<5;x++) for(int z=0;z<5;z++)
                graph.nodes.Add(new LwsRoadNode{nodeId=$"taxi.n{x}.{z}",position=new Vector3(-320+x*160,.15f,-320+z*160),regionId="taxi.demo",sceneChunkId="taxi.city"});
            for(int x=0;x<5;x++) for(int z=0;z<5;z++)
            {
                if(x<4) { Edge(graph,x,z,x+1,z); Edge(graph,x+1,z,x,z); }
                if(z<4) { Edge(graph,x,z,x,z+1); Edge(graph,x,z+1,x,z); }
            }
            // Each taxi bay is a real graph endpoint, connected along the street then into its apron.
            for(int i=0;i<20;i++)
            {
                int x=i/4,z=i%4;
                string stopId="taxi.stop."+i.ToString("00");
                Vector3 bay=new Vector3(-320+x*160+13,.15f,-260+z*160);
                Vector3 approach=bay-Vector3.right*13;
                graph.nodes.Add(new LwsRoadNode{nodeId=stopId,position=bay,regionId="taxi.demo",sceneChunkId="taxi.city"});
                foreach(int junction in new[]{z,z+1})
                {
                    string roadId=$"taxi.n{x}.{junction}";
                    Vector3 road=new Vector3(approach.x,.15f,-320+junction*160);
                    BayEdge(graph,roadId,stopId,new[]{road,approach,bay});
                    BayEdge(graph,stopId,roadId,new[]{bay,approach,road});
                }
            }
            return graph;
        }
        private static void BayEdge(LwsRoadGraph graph,string from,string to,Vector3[] points)
        {
            string id=from+"-"+to;
            var edge=new LwsRoadEdge{edgeId=id,roadId=id,segmentId=id,fromNodeId=from,toNodeId=to,
                roadClass=LwsRoadClass.LocalRoad,direction=LwsRoadDirection.Bidirectional,oneWay=true,
                surfaceType=LwsRoadSurfaceType.AsphaltInterstate,speedLimitMph=15,laneCount=1,laneWidthMeters=5,
                stateOrRegionId="taxi.demo",sceneChunkId="taxi.city"};
            float distance=0;
            for(int segment=1;segment<points.Length;segment++)
            {
                Vector3 a=points[segment-1],b=points[segment];
                float length=Vector3.Distance(a,b); int steps=Mathf.CeilToInt(length/5);
                for(int n=segment==1?0:1;n<=steps;n++)
                    edge.samples.Add(new LwsRoadSample{roadId=id,segmentId=id,position=Vector3.Lerp(a,b,n/(float)steps),
                        forward=(b-a).normalized,up=Vector3.up,distanceFromStartMeters=distance+length*n/steps,
                        roadWidthMeters=9,laneWidthMeters=5,laneCount=1,speedLimitMph=15,direction=edge.direction});
                distance+=length;
            }
            edge.distanceMeters=edge.travelCost=distance; graph.edges.Add(edge);
        }
        private static void Edge(LwsRoadGraph graph,int x,int z,int dx,int dz)
        {
            string id=$"taxi.{x}.{z}-{dx}.{dz}";
            Vector3 a=new Vector3(-320+x*160,.15f,-320+z*160), b=new Vector3(-320+dx*160,.15f,-320+dz*160);
            var edge=new LwsRoadEdge{edgeId=id,roadId=id+".road",segmentId=id+".segment",
                fromNodeId=$"taxi.n{x}.{z}",toNodeId=$"taxi.n{dx}.{dz}",roadClass=LwsRoadClass.LocalRoad,
                direction=dx>x?LwsRoadDirection.Eastbound:dx<x?LwsRoadDirection.Westbound:dz>z?LwsRoadDirection.Northbound:LwsRoadDirection.Southbound,
                surfaceType=LwsRoadSurfaceType.AsphaltInterstate,oneWay=true,distanceMeters=160,travelCost=160,speedLimitMph=30,laneCount=1,laneWidthMeters=5,
                laneCenterOffsetsMeters=new List<float>{4.5f},stateOrRegionId="taxi.demo",sceneChunkId="taxi.city"};
            for(int i=0;i<=16;i++) edge.samples.Add(new LwsRoadSample{roadId=edge.roadId,segmentId=edge.segmentId,position=Vector3.Lerp(a,b,i/16f),forward=(b-a).normalized,up=Vector3.up,distanceFromStartMeters=i*10,roadWidthMeters=20,laneWidthMeters=5,laneCount=1,speedLimitMph=30,direction=edge.direction});
            graph.edges.Add(edge);
        }
        private static LwsTrafficLaneDefinition[] CreateTrafficLanes()
        {
            var result=new List<LwsTrafficLaneDefinition>();
            for(int i=0;i<4;i++)
            {
                float minX=i%2==0?-315:5, minZ=i/2==0?-315:5;
                float maxX=minX+310,maxZ=minZ+310;
                Vector3[] corners={new Vector3(minX,.2f,minZ),new Vector3(minX,.2f,maxZ),new Vector3(maxX,.2f,maxZ),new Vector3(maxX,.2f,minZ)};
                var points=new List<Vector3>();
                for(int c=0;c<4;c++)
                {
                    Vector3 previous=corners[(c+3)%4],corner=corners[c],next=corners[(c+1)%4];
                    Vector3 enter=Vector3.MoveTowards(corner,previous,12), exit=Vector3.MoveTowards(corner,next,12);
                    Vector3 priorExit=Vector3.MoveTowards(previous,corner,12);
                    int steps=Mathf.CeilToInt(Vector3.Distance(priorExit,enter)/8);
                    for(int s=0;s<steps;s++) points.Add(Vector3.Lerp(priorExit,enter,s/(float)steps));
                    for(int s=0;s<6;s++) { float t=s/6f; points.Add((1-t)*(1-t)*enter+2*(1-t)*t*corner+t*t*exit); }
                }
                float length=0; for(int n=1;n<points.Count;n++) length+=Vector3.Distance(points[n-1],points[n]);
                result.Add(new LwsTrafficLaneDefinition{laneId="taxi.loop."+i,roadId="taxi.city.loop."+i,segmentId="taxi.loop."+i,edgeId="taxi.loop."+i,roadClass=LwsRoadClass.LocalRoad,direction=LwsRoadDirection.Bidirectional,laneWidthMeters=5,speedLimitMph=25,lengthMeters=length,centerline=points.ToArray()});
            }
            return result.ToArray();
        }

        private static void BakeEasyRoads(Transform parent)
        {
            var before=new HashSet<int>(Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).Select(m=>m.GetInstanceID()));
            var rootsBefore=new HashSet<int>(SceneManager.GetActiveScene().GetRootGameObjects().Select(o=>o.GetInstanceID()));
            Type networkType=Resolve("EasyRoads3Dv3.ERRoadNetwork"), roadType=Resolve("EasyRoads3Dv3.ERRoadType");
            object network=Construct(networkType), type=Construct(roadType);
            Member(type,"roadTypeName","Truck Taxi Streets"); Member(type,"roadWidth",20f); Member(type,"roadMaterial",asphalt);
            Member(type,"hasMeshCollider",true); Member(type,"layer",0); Member(type,"tag","Untagged");
            for(int i=0;i<5;i++)
            {
                float coordinate=-320+i*160;
                foreach(bool vertical in new[]{false,true})
                {
                    Vector3[] markers=Enumerable.Range(0,5).Select(n=>vertical?new Vector3(coordinate,.12f,-360+n*180):new Vector3(-360+n*180,.13f,coordinate)).ToArray();
                    object road=Call(network,"CreateRoad","Taxi Street "+i+(vertical?" N":" E"),type,markers);
                    Call(road,"SetMeshCollider",true);
                    Call(road,"SetTerrainDeformation",false);
                    Call(road,"SnapToTerrain",false);
                }
            }
            Call(network,"BuildRoadNetwork",false,false,false);
            var baked=new GameObject("EasyRoads Baked City Streets"); baked.transform.SetParent(parent,false);
            int index=0;
            foreach(var source in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            {
                if(before.Contains(source.GetInstanceID()) || source.sharedMesh==null || source.GetComponent<MeshCollider>()==null) continue;
                var mesh=Object.Instantiate(source.sharedMesh); mesh.name="Taxi Road "+index;
                string path=Root+"/DemoContent/Meshes/Road_"+index+".asset";
                var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(existing!=null) { EditorUtility.CopySerialized(mesh,existing); Object.DestroyImmediate(mesh); mesh=existing; }
                else AssetDatabase.CreateAsset(mesh,path);
                var go=new GameObject(mesh.name,typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));
                go.transform.SetParent(baked.transform,false); go.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation); go.transform.localScale=source.transform.lossyScale;
                go.GetComponent<MeshFilter>().sharedMesh=mesh; go.GetComponent<MeshRenderer>().sharedMaterial=asphalt; go.GetComponent<MeshCollider>().sharedMesh=mesh; index++;
            }
            foreach(var go in SceneManager.GetActiveScene().GetRootGameObjects()) if(!rootsBefore.Contains(go.GetInstanceID()) && go!=baked) Object.DestroyImmediate(go);
            if(index==0) throw new InvalidOperationException("EasyRoads generated no collidable road meshes.");
            Debug.Log("Truck Taxi baked "+index+" EasyRoads meshes.");
        }
        private static void BuildDistricts(Transform parent)
        {
            var model=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NWH/Vehicle Physics 2/Vehicles/Euro Truck by GR3D/GR3D 010416TRUK/Demo Scene/Mesh/Building01.fbx");
            for(int x=0;x<4;x++) for(int z=0;z<4;z++)
            {
                Vector3 center=new Vector3(-240+x*160,0,-240+z*160);
                bool park=x==1&&z==1;
                if(park) { Box("Central Park Plaza",parent,center+Vector3.up*.02f,new Vector3(105,.05f,105),concrete); continue; }
                bool industrial=x>=2&&z>=2;
                for(int b=0;b<3;b++)
                {
                    Vector3 p=center+new Vector3(b==0?-38:38,0,b==2?38:-38);
                    float height=industrial?12:8+(x+z+b)%4*7;
                    if(model!=null && b==0)
                    {
                        var go=(GameObject)PrefabUtility.InstantiatePrefab(model); go.name="Imported city building"; go.transform.SetParent(parent,false);
                        var renderers=go.GetComponentsInChildren<Renderer>();
                        Bounds bounds=renderers[0].bounds; foreach(var r in renderers) bounds.Encapsulate(r.bounds);
                        float scale=32/Mathf.Max(bounds.size.x,bounds.size.z); go.transform.localScale=Vector3.one*scale;
                        go.transform.position=p-Vector3.Scale(bounds.center-new Vector3(0,bounds.extents.y,0),go.transform.localScale);
                        foreach(var r in renderers) r.sharedMaterial=industrial?teal:concrete;
                        var col=go.AddComponent<BoxCollider>(); col.center=bounds.center; col.size=bounds.size;
                    }
                    else Box(industrial?"Industrial warehouse":"City block",parent,p+Vector3.up*height*.5f,new Vector3(34,height,34),industrial?teal:(b==1?coral:concrete));
                }
                Box("Parking court",parent,center+new Vector3(-35,.025f,36),new Vector3(58,.05f,55),asphalt);
            }
            for(int i=0;i<5;i++)
            {
                float c=-320+i*160;
                for(int j=-340;j<350;j+=18)
                {
                    Box("Lane stripe",parent,new Vector3(c,.18f,j),new Vector3(.2f,.015f,6),yellow,false);
                    Box("Lane stripe",parent,new Vector3(j,.18f,c),new Vector3(6,.015f,.2f),yellow,false);
                }
            }
            // Soft world boundary: visible barriers, no failure/restart mechanic.
            Box("North barrier",parent,new Vector3(0,1.5f,395),new Vector3(800,3,2),concrete);
            Box("South barrier",parent,new Vector3(0,1.5f,-395),new Vector3(800,3,2),concrete);
            Box("East barrier",parent,new Vector3(395,1.5f,0),new Vector3(2,3,800),concrete);
            Box("West barrier",parent,new Vector3(-395,1.5f,0),new Vector3(2,3,800),concrete);
        }
        private static void BuildStops(Transform parent)
        {
            string[] names={"Clockout Offices","Wrong Turn Diner","Brakefast Cafe","Central Transit","Park Pavilion","Sidequest Apartments","Tiny Mansion","Corner Market","The Long Stay Hotel","Commuter Square","Dispatch Arcade","Warehouse Nine","Cargo Cult Coffee","Overtime Foundry","Southside Clinic","No Vacancy Inn","Scenic Notch","The Last Stop Bar","Union Workshop","Roundabout Records"};
            for(int i=0;i<20;i++)
            {
                int x=i/4,z=i%4;
                var go=new GameObject(names[i]); go.transform.SetParent(parent,false);
                go.transform.position=new Vector3(-320+x*160+13,.16f,-260+z*160);
                var location=go.AddComponent<TruckTaxiRideLocation>(); location.locationName=names[i]; location.locationId="taxi.stop."+i.ToString("00");
                location.locationType=(TaxiLocationType)(i%12); location.district=x<2?"Residential":x==2?"Downtown":"Industrial";
                var stop=new GameObject("Truck Stop").transform; stop.SetParent(go.transform,false); location.truckStopPoint=stop;
                var person=new GameObject("Passenger Spawn Point").transform; person.SetParent(go.transform,false); person.localPosition=new Vector3(5,0,0); location.passengerSpawnPoint=person;
                Box("Taxi bay",go.transform,go.transform.position+Vector3.down*.07f,new Vector3(9,.08f,22),asphalt);
                Box("Taxi stop marker",go.transform,go.transform.position+new Vector3(6,1.5f,5),new Vector3(.25f,3,.25f),yellow);
                var sign=new GameObject("Stop sign").AddComponent<TextMeshPro>(); sign.transform.SetParent(go.transform,false);
                sign.transform.localPosition=new Vector3(6,3.3f,5); sign.transform.localRotation=Quaternion.Euler(0,270,0); sign.text=names[i]+"\nTAXI"; sign.fontSize=4; sign.rectTransform.sizeDelta=new Vector2(8,3);
                sign.alignment=TextAlignmentOptions.Center;
            }
        }
        private static void BuildShortcutsAndProps(Transform parent)
        {
            for(int i=0;i<6;i++)
            {
                Vector3 center=new Vector3(-240+(i%3)*160,.2f,-240+(i/3)*320);
                var go=new GameObject("Shortcut "+i); go.transform.SetParent(parent,false); go.transform.position=center;
                var box=go.AddComponent<BoxCollider>(); box.isTrigger=true; box.size=new Vector3(20,5,35);
                var trigger=go.AddComponent<ShortcutTrigger>(); trigger.shortcutId="taxi.shortcut."+i; trigger.displayName=i%2==0?"Loading yard cut":"Parking lot cut"; trigger.minimumSpeed=2;
                for(int j=0;j<4;j++)
                {
                    var prop=Box("Movable construction barrier",parent,center+new Vector3(-8+j*5,1,17),new Vector3(3,1.6f,.6f),j%2==0?yellow:coral);
                    var body=prop.AddComponent<Rigidbody>(); body.mass=35;
                    var target=prop.AddComponent<TruckTaxiImpactTarget>(); target.kind=TaxiImpactKind.Property; target.targetId="taxi.prop."+i+"."+j;
                }
            }
            for(int i=0;i<3;i++)
            {
                var go=new GameObject("Scenic park point "+i); go.transform.SetParent(parent,false); go.transform.position=new Vector3(-100+i*60,2,-80);
                var col=go.AddComponent<BoxCollider>(); col.isTrigger=true; col.size=new Vector3(24,6,24);
                var marker=go.AddComponent<ShortcutTrigger>(); marker.scenicPoint=true; marker.shortcutId="taxi.scenic."+i; marker.displayName="Park viewpoint"; marker.bonusScore=0;
                Box("Park sculpture",parent,go.transform.position+new Vector3(15,2,0),new Vector3(3,8,3),teal);
            }
        }
        private static TruckTaxiPedestrianPopulation BuildPedestrianPaths()
        {
            var population=new GameObject("UTS Taxi Pedestrians").AddComponent<TruckTaxiPedestrianPopulation>();
            Type pathType=Resolve("PeopleWalkPath");
            var prefabs=AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/UTS_FullPack/Models/People/Prefabs/Womans"}).Take(2).Select(g=>AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
            if(prefabs.Length==0) throw new InvalidOperationException("UTS adult pedestrian prefabs missing.");
            var paths=new List<Component>();
            for(int i=0;i<4;i++)
            {
                var go=new GameObject("UTS Pedestrian Loop "+i); go.transform.SetParent(population.transform,false);
                var path=go.AddComponent(pathType);
                Member(path,"walkingPrefabs",prefabs); Member(path,"numberOfWays",1); Member(path,"loopPath",true);
                Member(path,"Density",.05f); Member(path,"_minimalObjectLength",3f); Member(path,"disableLineDraw",true);
                Set(path,"_ignorePeople",false); Set(path,"_ignoreCar",false); Set(path,"_ignoreBicycle",false);
                float x=-300+i*160;
                Vector3[] points={new Vector3(x,.2f,-260),new Vector3(x,.2f,-190),new Vector3(x+20,.2f,-190),new Vector3(x+20,.2f,-260)};
                var transforms=(List<GameObject>)pathType.GetField("pathPointTransform").GetValue(path);
                var positions=(List<Vector3>)pathType.GetField("pathPoint").GetValue(path);
                foreach(var p in points) { var point=new GameObject("point"); point.transform.SetParent(go.transform,false); point.transform.position=p; transforms.Add(point); positions.Add(p); }
                paths.Add(path);
            }
            population.peoplePaths=paths.ToArray(); return population;
        }
        private static void BuildLighting()
        {
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.65f,.75f,.9f); RenderSettings.ambientEquatorColor=new Color(.5f,.52f,.5f); RenderSettings.ambientGroundColor=new Color(.22f,.22f,.2f);
            var light=new GameObject("Demo daylight").AddComponent<Light>(); light.type=LightType.Directional; light.intensity=1.7f; light.shadows=LightShadows.Soft; light.transform.rotation=Quaternion.Euler(48,-35,0);
            RenderSettings.sun=light;
        }
        private static GameObject Box(string name,Transform parent,Vector3 position,Vector3 size,Material material,bool collider=true)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube); go.name=name; go.transform.SetParent(parent,false); go.transform.position=position; go.transform.localScale=size;
            go.GetComponent<Renderer>().sharedMaterial=material; if(!collider) Object.DestroyImmediate(go.GetComponent<Collider>()); return go;
        }
        private static Material Material(string name,Color color)
        {
            string path=Root+"/Materials/"+name+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null) { m=new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m,path); }
            m.color=color; m.SetFloat("_Smoothness",.15f); EditorUtility.SetDirty(m); return m;
        }
        private static T Asset<T>(string path) where T:ScriptableObject
        { var value=AssetDatabase.LoadAssetAtPath<T>(path); if(value==null) { value=ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(value,path); } return value; }
        private static Type Resolve(string name)
        { foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies()) { var t=assembly.GetType(name); if(t!=null) return t; } throw new TypeLoadException(name); }
        private static object Construct(Type type)
        {
            var constructor=type.GetConstructors().FirstOrDefault(c=>c.GetParameters().All(p=>p.IsOptional));
            if(constructor==null) throw new MissingMethodException(type.FullName+" has no default/optional constructor.");
            return constructor.Invoke(constructor.GetParameters().Select(p=>p.DefaultValue).ToArray());
        }
        private static object Call(object target,string name,params object[] args)
        {
            foreach(var method in target.GetType().GetMethods().Where(m=>m.Name==name))
            {
                var parameters=method.GetParameters();
                if(parameters.Length<args.Length) continue;
                if(parameters.Take(args.Length).Where((p,i)=>args[i]!=null && !p.ParameterType.IsInstanceOfType(args[i])).Any()) continue;
                if(parameters.Skip(args.Length).Any(p=>!p.IsOptional)) continue;
                return method.Invoke(target,args.Concat(parameters.Skip(args.Length).Select(p=>p.DefaultValue)).ToArray());
            }
            throw new MissingMethodException(target.GetType().Name,name);
        }
        private static void Member(object target,string name,object value)
        { var t=target.GetType(); var f=t.GetField(name); if(f!=null) f.SetValue(target,value); else t.GetProperty(name).SetValue(target,value); }
        private static void Set(Object target,string name,object value)
        {
            var so=new SerializedObject(target); var p=so.FindProperty(name);
            if(p==null) throw new MissingFieldException(target.GetType().Name,name);
            if(value is bool flag) p.boolValue=flag; else if(value is Object obj) p.objectReferenceValue=obj;
            else if(value is float number) p.floatValue=number; else if(value is int n) p.intValue=n;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [MenuItem("Truck Taxi/Build Windows Demo")]
        public static void BuildWindows()
        {
            if(!File.Exists(ScenePath)) throw new InvalidOperationException("Create the Truck Taxi scene first.");
            Directory.CreateDirectory("Builds/TruckTaxiDemo");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes=new[]{ScenePath}, locationPathName="Builds/TruckTaxiDemo/TruckTaxi.exe",
                target=BuildTarget.StandaloneWindows64, options=BuildOptions.Development
            });
            Debug.Log("TRUCK TAXI BUILD: "+report.summary.result+" / "+report.summary.totalSize+" bytes / "+report.summary.totalErrors+" errors");
            if(report.summary.result!=BuildResult.Succeeded) throw new Exception("Truck Taxi Windows build failed.");
        }
    }
}
