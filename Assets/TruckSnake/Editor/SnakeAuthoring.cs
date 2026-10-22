using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using LWS.TruckSports;
using LWS.TruckSports.Editor;
using LWS.InterstateHauler;
using LWS.TruckTaxi;
namespace LWS.TruckSnake.Editor {
 public static class SnakeAuthoring {
  public const string Folder="TruckSnake",Scene="Assets/TruckSnake/Scenes/TruckSnake.unity";
  sealed class Road{public string name;public Vector3[] points;public float elevation;public bool hard;}
  static readonly List<Road> roads=new List<Road>();static SnakeMatch active;static bool repair;
#if VISTA
  static Pinwheel.Vista.ProgressiveTask terrainTask;
#endif
  [MenuItem("Truck Sports/Truck Snake/Create Vista and EasyRoads world")]
  public static void RepairRoads(){
#if VISTA
   EditorSceneManager.OpenScene(Scene);active=UnityEngine.Object.FindAnyObjectByType<SnakeMatch>();repair=true;PlanRoads();var manager=UnityEngine.Object.FindAnyObjectByType<Pinwheel.Vista.VistaManager>();terrainTask=manager.GenerateAll();EditorApplication.update+=Continue;
#endif
  }
  public static void Create(){repair=false;
   if(File.Exists(Scene))throw new InvalidOperationException("Preserve the authored Snake world; apply targeted upgrades.");
   foreach(var dir in new[]{"Terrain","Meshes","Prefabs","Configuration","Art"})Directory.CreateDirectory("Assets/"+Folder+"/"+dir);
   var m=SportsEventAuthoring.Begin<SnakeMatch>(Folder);active=m;m.option=0;m.setup.count=4;m.NormalizeSetup();for(int i=1;i<4;i++)m.setup.players[i].source=LWS.TruckBowling.BowlingSource.AI;
   m.world=new GameObject("Seven-square-kilometre connected road-train playground").AddComponent<SnakeWorld>();m.world.playableBounds=new Bounds(Vector3.zero,new Vector3(2980,300,2380));m.world.regions=new[]{"Original Downtown","East Industrial Yards","Northern Multilevel Interchanges","Western Countryside","Southern Stunt Park"};PlanRoads();
#if VISTA
   terrainTask=SnakeTerrainAuthoring.Generate(m,roads.Select(r=>r.points).ToList());EditorApplication.update+=Continue;
   File.WriteAllText("Logs/TruckSports/EssentialRelease/snake-authoring.txt","GENERATING_VISTA "+DateTime.UtcNow.ToString("o"));
#else
   throw new InvalidOperationException("The installed Vista runtime is required for the authored Snake world.");
#endif
  }
  static void Continue(){
#if VISTA
   if(terrainTask==null||!terrainTask.isCompleted)return;EditorApplication.update-=Continue;terrainTask=null;
   try{if(repair){SnakeTerrainAuthoring.Finish(active);var old=GameObject.Find("EasyRoads connected regional lanes and flyovers");if(old!=null)UnityEngine.Object.DestroyImmediate(old);var source=EditorSceneManager.OpenPreviewScene(LWS.TruckFareFight.Editor.FareFightAuthoring.Scene);try{var original=source.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<LWS.TruckFareFight.FareFightMatch>(true)).Single();active.world.graph=JsonUtility.FromJson<LwsRoadGraph>(JsonUtility.ToJson(original.district.graph));}finally{EditorSceneManager.ClosePreviewScene(source);}Roads(active);active.minimumSpeed=7;EditorSceneManager.MarkSceneDirty(active.gameObject.scene);EditorSceneManager.SaveScene(active.gameObject.scene);}else{SnakeTerrainAuthoring.Finish(active);Finish(active);}File.WriteAllText("Logs/TruckSports/EssentialRelease/snake-authoring.txt","PASS saved "+DateTime.UtcNow.ToString("o"));File.WriteAllText("Logs/TruckSports/EssentialRelease/snake-job.txt","PASS saved "+DateTime.UtcNow.ToString("o"));}
   catch(Exception error){File.WriteAllText("Logs/TruckSports/EssentialRelease/snake-authoring.txt","FAIL "+error);Debug.LogException(error);}finally{active=null;}
#endif
  }
  static void PlanRoads(){roads.Clear();roads.Add(new Road{name="Outer scenic circuit",points=Loop(-1300,-1000,1300,1000,120)});roads.Add(new Road{name="Inner freight circuit",points=Loop(-700,-600,700,600,110)});roads.Add(new Road{name="Industrial cargo circuit",points=Loop(700,-400,1300,400,85)});roads.Add(new Road{name="Southern stunt circuit",points=Loop(-550,-1000,550,-600,85)});
   roads.Add(new Road{name="North town artery",points=new[]{new Vector3(0,0,320),new Vector3(0,0,600),new Vector3(0,0,1000)}});roads.Add(new Road{name="South town artery",points=new[]{new Vector3(0,0,-320),new Vector3(0,0,-600),new Vector3(0,0,-1000)}});roads.Add(new Road{name="West town artery",points=new[]{new Vector3(-320,0,0),new Vector3(-700,0,0),new Vector3(-1300,0,0)}});roads.Add(new Road{name="East town artery",points=new[]{new Vector3(320,0,0),new Vector3(700,0,0),new Vector3(1300,0,0)}});
   roads.Add(new Road{name="Western winding farm road",points=new[]{new Vector3(-1300,0,0),new Vector3(-1150,0,170),new Vector3(-950,0,260),new Vector3(-800,0,160),new Vector3(-700,0,0)}});
   roads.Add(new Road{name="Northern aerial interchange",points=Loop(-1200,400,1200,1000,160),elevation=22,hard=true});
   roads.Add(new Road{name="West sweeping flyover ramp",points=new[]{new Vector3(0,0,600),new Vector3(-250,4,620),new Vector3(-600,12,650),new Vector3(-900,18,680),new Vector3(-1200,22,700)}});roads.Add(new Road{name="East sweeping flyover ramp",points=new[]{new Vector3(0,0,600),new Vector3(250,4,620),new Vector3(600,12,650),new Vector3(900,18,680),new Vector3(1200,22,700)}});
   roads.Add(new Road{name="Stunt gap approach",points=new[]{new Vector3(-550,0,-800),new Vector3(-420,1,-800),new Vector3(-320,5,-800),new Vector3(-240,10,-800),new Vector3(-205,12,-800)},hard=true});
   roads.Add(new Road{name="Stunt gap landing",points=new[]{new Vector3(-175,10,-800),new Vector3(-100,6,-800),new Vector3(0,0,-800),new Vector3(300,0,-800),new Vector3(550,0,-800)},hard=true});
  }
  static Vector3[] Loop(float minX,float minZ,float maxX,float maxZ,float radius){var p=new List<Vector3>();float midX=(minX+maxX)/2,midZ=(minZ+maxZ)/2;
   p.Add(new Vector3(midX,0,minZ));Arc(p,maxX-radius,minZ+radius,radius,-90,0);p.Add(new Vector3(maxX,0,midZ));Arc(p,maxX-radius,maxZ-radius,radius,0,90);p.Add(new Vector3(midX,0,maxZ));Arc(p,minX+radius,maxZ-radius,radius,90,180);p.Add(new Vector3(minX,0,midZ));Arc(p,minX+radius,minZ+radius,radius,180,270);p.Add(p[0]);return p.ToArray();}
  static void Arc(List<Vector3> points,float x,float z,float radius,float first,float last){for(int i=0;i<=5;i++){float a=Mathf.Lerp(first,last,i/5f)*Mathf.Deg2Rad;points.Add(new Vector3(x+Mathf.Cos(a)*radius,0,z+Mathf.Sin(a)*radius));}}
  static void Finish(SnakeMatch m){
   var source=EditorSceneManager.OpenPreviewScene(LWS.TruckFareFight.Editor.FareFightAuthoring.Scene);
   try{var roots=source.GetRootGameObjects();var original=roots.SelectMany(r=>r.GetComponentsInChildren<LWS.TruckFareFight.FareFightMatch>(true)).Single();m.world.graph=JsonUtility.FromJson<LwsRoadGraph>(JsonUtility.ToJson(original.district.graph));m.world.graph.graphId="truck-snake-connected-five-regions";
    foreach(var root in roots.Where(r=>r.name.StartsWith("Original Taxi town ·")||r.name=="Owned roadside district dressing")){var copy=UnityEngine.Object.Instantiate(root);copy.name="Snake Downtown · "+root.name;SceneManager.MoveGameObjectToScene(copy,m.gameObject.scene);}
    var traffic=UnityEngine.Object.Instantiate(original.traffic);traffic.name="Existing UTS world traffic";m.traffic=traffic;traffic.cityLanes=Array.Empty<LwsTrafficLaneDefinition>();traffic.maximumVehicles=36;traffic.maxFullTraffic=traffic.maxVisibleTraffic=48;traffic.fullTrafficRadius=traffic.visibleTrafficRadius=2200;
    var profile=UnityEngine.Object.Instantiate(original.traffic.densityProfile);profile.name="Snake world population budget";profile.maximumActiveTraffic=48;profile.measuredTrafficBaseline=36;profile.trafficDensityMultiplier=1.3f;profile.maximumActivePedestrians=150;profile.spawnRadius=2200;profile.despawnRadius=2400;AssetDatabase.CreateAsset(profile,"Assets/TruckSnake/Configuration/World population.asset");traffic.densityProfile=profile;
    m.pedestrians=UnityEngine.Object.Instantiate(original.pedestrians);m.pedestrians.name="Original downtown UTS pedestrian ecosystem";m.pedestrians.densityProfile=profile;
   }finally{EditorSceneManager.ClosePreviewScene(source);}
   foreach(var renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include))if(renderer.name=="Ground"&&renderer.GetComponentInParent<SnakeWorld>()==null&&renderer.transform.root.name.StartsWith("Snake Downtown")){renderer.enabled=false;var collider=renderer.GetComponent<Collider>();if(collider!=null)collider.enabled=false;}
   Roads(m);Scenery(m);Vehicles(m);Pickups(m);SnakeGrowthAuthoring.Install(m);
   m.broadcast=SportsEventAuthoring.Camera("Snake world broadcast");m.broadcast.Pose(m.world.starts[0],new Vector3(0,30,-40),67,true);m.cameras=new[]{m.broadcast.view};SportsEventAuthoring.UI(m);SportsRallyAuthoring.CompactUI(m);
   m.drivingViews=SportsDrivingViewsAuthoring.Install(m,m.trucks,m.broadcast,Folder);m.drivingViews.defaultMode=1;m.drivingViews.independentViewModes=true;m.drivingViews.localElevated=true;m.drivingViews.cabMounts=m.trucks.Select(t=>t.GetComponentsInChildren<Camera>(true).First(c=>c.name=="Vehicle Camera Driver").transform).ToArray();m.drivingViews.rally.groundOffset=new Vector3(0,8,-21);
   m.feedback.clips=LWS.TruckHockey.Editor.HockeyAudioAuthoring.Create();m.atmosphere=SportsChaosAuthoring.Weather(m,Folder);m.presets=SportsWeatherPresetsAuthoring.EnsureCatalog();m.drivingViews.rally.weather=m.atmosphere;SportsWeatherRenderingAuthoring.Install(m,Folder);SportsWeatherPickerAuthoring.Install(m,m.presets,new Vector2(0,355));SportsScenicGradeAuthoring.Install(m.gameObject,m.cameras,Folder,-.35f);
   SnakePresentationAuthoring.Install(m);ValidateUsableArea(m);SportsEventAuthoring.Save(Folder);Validate(m);
  }
  public static void EnsureGradeMarkers(SnakeMatch m){if(m.world.groundRoads!=null&&m.world.groundRoads.Length>0)return;PlanRoads();m.world.groundRoads=roads.Where(r=>r.elevation==0&&r.points.All(p=>p.y==0)).Select(r=>new SnakeWorld.GroundRoad{name=r.name,width=30,markers=r.points.Select(p=>new Vector3(p.x,m.world.Height(p)+.18f,p.z)).ToArray()}).ToArray();EditorUtility.SetDirty(m.world);}
  static void Roads(SnakeMatch m){
   var heights=m.world.terrain.Select(t=>t.terrainData.GetHeights(0,0,t.terrainData.heightmapResolution,t.terrainData.heightmapResolution)).ToArray();
   var planned=roads.ToDictionary(r=>r,r=>r.points.Select(p=>new Vector3(p.x,m.world.Height(p)+p.y+r.elevation+.18f,p.z)).ToArray());
   var material=SportsEventAuthoring.Mat(Folder,"Open-road textured asphalt",new Color(.6f,.64f,.68f),.2f);var layer=AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/NWH/Common/TerrainLayers/AsphaltLayer.terrainlayer");material.mainTexture=layer.diffuseTexture;material.mainTextureScale=new Vector2(3,6);
   var root=new GameObject("EasyRoads connected regional lanes and flyovers").transform;var graph=m.world.graph;var pickupPoints=new List<SnakeWorld.RoadPoint>();
   foreach(var road in roads){var points=planned[road];
    LWS.TruckTaxi.Editor.TruckTaxiDemoBuilder.BakeRegionalRoads(root,new List<Vector3[]>{points},road.hard?26:30,road.name,material,"Assets/TruckSnake/Meshes");
    for(int t=0;t<heights.Length;t++)m.world.terrain[t].terrainData.SetHeights(0,0,heights[t]);
    for(int i=1;i<points.Length;i++){LwsRoadNode a=Node(graph,points[i-1]),b=Node(graph,points[i]);if(a.nodeId==b.nodeId)continue;var edge=new LwsRoadEdge{roadId="snake.road."+graph.edges.Count,segmentId="snake.segment."+graph.edges.Count,edgeId="snake.edge."+graph.edges.Count,fromNodeId=a.nodeId,toNodeId=b.nodeId,roadClass=road.hard?LwsRoadClass.Ramp:LwsRoadClass.LocalRoad,direction=LwsRoadDirection.Bidirectional,surfaceType=LwsRoadSurfaceType.AsphaltInterstate,oneWay=false,speedLimitMph=35,laneCount=2,laneWidthMeters=4,distanceMeters=Vector3.Distance(a.position,b.position),travelCost=Vector3.Distance(a.position,b.position),stateOrRegionId="snake.world",sceneChunkId="snake.world"};int samples=Mathf.Max(2,Mathf.CeilToInt(edge.distanceMeters/10));for(int k=0;k<=samples;k++){var p=Vector3.Lerp(a.position,b.position,k/(float)samples);edge.samples.Add(new LwsRoadSample{position=p,forward=(b.position-a.position).normalized,up=Vector3.up,distanceFromStartMeters=edge.distanceMeters*k/samples,roadWidthMeters=30,laneCount=2,laneWidthMeters=4,speedLimitMph=35});if(k%5==0&&k>0&&k<samples)pickupPoints.Add(new SnakeWorld.RoadPoint{position=p,hard=road.hard});}graph.edges.Add(edge);}
   }
   var launch=graph.nodes.OrderBy(n=>Vector3.Distance(n.position,new Vector3(-205,m.world.Height(new Vector3(-205,0,-800))+12.18f,-800))).First();var landing=graph.nodes.OrderBy(n=>Vector3.Distance(n.position,new Vector3(-175,m.world.Height(new Vector3(-175,0,-800))+10.18f,-800))).First();var jump=new LwsRoadEdge{roadId="snake.stunt.jump",segmentId="snake.stunt.jump",edgeId="snake.stunt.jump",fromNodeId=launch.nodeId,toNodeId=landing.nodeId,roadClass=LwsRoadClass.Ramp,direction=LwsRoadDirection.Eastbound,surfaceType=LwsRoadSurfaceType.AsphaltInterstate,oneWay=true,distanceMeters=Vector3.Distance(launch.position,landing.position),travelCost=36,speedLimitMph=35,laneCount=1,laneWidthMeters=6,stateOrRegionId="snake.world",sceneChunkId="snake.world"};jump.samples.Add(new LwsRoadSample{position=launch.position,forward=Vector3.right});jump.samples.Add(new LwsRoadSample{position=landing.position,forward=Vector3.right,distanceFromStartMeters=jump.distanceMeters});graph.edges.Add(jump);
   m.world.groundRoads=roads.Where(r=>r.elevation==0&&r.points.All(p=>p.y==0)).Select(r=>new SnakeWorld.GroundRoad{name=r.name,width=30,markers=planned[r]}).ToArray();SnakeRoadGradeAuthoring.Grade(m);m.world.pickupPoints=pickupPoints.ToArray();m.world.jumpLaunch=launch.position;m.world.jumpDirection=Vector3.right;
   var policy=new LwsTrafficSpawnPolicy{includeRampTraffic=false};m.traffic.cityLanes=LwsTrafficLaneBuilder.BuildTrafficLanes(graph,policy).Where(l=>l.centerline.Length>=5).ToArray();if(m.traffic.cityLanes.Length==0)SnakePlaytestRepairAuthoring.InstallTrafficLanes(m);m.pedestrians.roadLanes=m.traffic.cityLanes;
   var validation=graph.Validate();if(!validation.IsValid)throw new InvalidOperationException("Snake connected road graph invalid: "+validation.Summary);
  }
  static LwsRoadNode Node(LwsRoadGraph graph,Vector3 point){var existing=graph.nodes.FirstOrDefault(n=>Vector3.Distance(n.position,point)<.7f);if(existing!=null)return existing;var node=new LwsRoadNode{nodeId="snake.node."+graph.nodes.Count,position=point,regionId="snake.world",sceneChunkId="snake.world"};graph.nodes.Add(node);return node;}
  static void Scenery(SnakeMatch m){SnakeSceneryAuthoring.Install(m);}
  static void Vehicles(SnakeMatch m){
   m.world.starts=new[]{new Vector3(-6,.35f,-120),new Vector3(6,.35f,-90),new Vector3(-6,.35f,-60),new Vector3(6,.35f,-30)};m.world.rotations=new[]{Quaternion.identity,Quaternion.identity,Quaternion.identity,Quaternion.identity};m.trucks=new SportsEventTruck[4];m.convoys=new SnakeConvoy[4];m.horns=new AudioSource[4];m.horn=AssetDatabase.FindAssets("Horn t:AudioClip",new[]{"Assets/NWH"}).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<AudioClip>).FirstOrDefault();m.couplerMaterial=SportsEventAuthoring.Mat(Folder,"Articulated coupling rubber",new Color(.04f,.045f,.05f),.1f);
   for(int i=0;i<4;i++){var truck=LWS.TruckFareFight.Editor.FareFightVehicleAuthoring.Spawn(m,i,m.world.starts[i],Quaternion.identity);m.trucks[i]=truck;var convoy=new GameObject("Independent pooled snake train "+i).AddComponent<SnakeConvoy>();convoy.tractor=truck;convoy.slot=i;convoy.match=m;convoy.trailerPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TruckTrailerPlug/Prefabs/Cargo Trailer Render Model.prefab");m.convoys[i]=convoy;foreach(var body in truck.GetComponentsInChildren<Rigidbody>(true)){var contact=body.gameObject.AddComponent<SnakeTruckContact>();contact.match=m;contact.truck=truck;}m.horns[i]=truck.gameObject.AddComponent<AudioSource>();m.horns[i].playOnAwake=false;m.horns[i].spatialBlend=.8f;m.horns[i].minDistance=8;m.horns[i].maxDistance=120;}
  }
  static void Pickups(SnakeMatch m){m.pickups=new SnakePickup[SnakeGrowthAuthoring.PickupCount];var cargo=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TruckTrailerPlug/Prefabs/Cargo Trailer Render Model.prefab");for(int i=0;i<m.pickups.Length;i++){var root=new GameObject(i>=SnakeGrowthAuthoring.StandardCount?"Golden three-trailer route gate":"Green one-trailer route gate");var pickup=root.AddComponent<SnakePickup>();pickup.match=m;pickup.Golden=i>=SnakeGrowthAuthoring.StandardCount;m.pickups[i]=pickup;var shape=root.AddComponent<BoxCollider>();shape.isTrigger=true;shape.center=Vector3.up*2;shape.size=new Vector3(11,6,11);var line=root.AddComponent<LineRenderer>();line.loop=true;line.useWorldSpace=false;line.positionCount=64;line.widthMultiplier=.3f;var color=pickup.Golden?new Color(1,.7f,.025f):new Color(.1f,1,.3f);var material=SportsEventAuthoring.Mat(Folder,pickup.Golden?"Gold pickup glow":"Green pickup glow",color,.4f);material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",color*2);line.sharedMaterial=material;for(int k=0;k<64;k++){float a=k*Mathf.PI*2/64;line.SetPosition(k,new Vector3(Mathf.Cos(a)*5,.25f,Mathf.Sin(a)*5));}pickup.cargoIcon=UnityEngine.Object.Instantiate(cargo,root.transform).transform;pickup.cargoIcon.localScale=Vector3.one*.12f;pickup.cargoIcon.localPosition=Vector3.up*2;foreach(var renderer in pickup.cargoIcon.GetComponentsInChildren<Renderer>())renderer.sharedMaterial=material;root.SetActive(false);}}
  static void ValidateUsableArea(SnakeMatch m){Physics.SyncTransforms();double area=0;const float grid=20;for(float z=-1150;z<=1150;z+=grid)for(float x=-1450;x<=1450;x+=grid){var p=new Vector3(x,0,z);float h=m.world.Height(p);var terrain=m.world.terrain.First(t=>x>=t.transform.position.x&&x<=t.transform.position.x+t.terrainData.size.x&&z>=t.transform.position.z&&z<=t.transform.position.z+t.terrainData.size.z);float slope=terrain.terrainData.GetSteepness((x-terrain.transform.position.x)/terrain.terrainData.size.x,(z-terrain.transform.position.z)/terrain.terrainData.size.z);if(slope>25)continue;bool blocked=false;foreach(var c in Physics.OverlapBox(new Vector3(x,h+1.8f,z),new Vector3(10,1.3f,10),Quaternion.identity,1,QueryTriggerInteraction.Ignore))if(!(c is TerrainCollider)&&c.bounds.max.y>h+2&&c.bounds.min.y<h+3){blocked=true;break;}if(!blocked)area+=grid*grid;}m.world.verifiedUsableSquareMetres=(float)area;File.WriteAllText("Logs/TruckSports/EssentialRelease/snake-playable-area.txt","Raster20m · real terrain slope<=25deg and full-cell low-obstacle clearance · usable="+area+"m2 / "+(area/2589988.110336)+" square miles. Buildings/tree trunks and low obstructions excluded.");if(area<2*2589988.110336)throw new InvalidOperationException("Snake usable ground area is below two square miles: "+area);}
  public static void Validate(SnakeMatch m){if(m==null||m.trucks.Length!=4||m.pickups.Length!=SnakeGrowthAuthoring.PickupCount||m.world.verifiedUsableSquareMetres<5179976)throw new InvalidOperationException("Snake scene reference/area validation failed.");if(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include).Length!=1)throw new InvalidOperationException("Snake requires one shared listener.");}
 }
}
