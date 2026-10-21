using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;
using CompassNavigatorPro;
using LWS.TruckSports;
using LWS.TruckSports.Editor;
using LWS.TruckFareFight.Editor;
using LWS.TruckSnake;
using DigitalRuby.WeatherMaker;
namespace LWS.TruckHideAndSeek.Editor
{
 public static class HideAuthoring
 {
  public const string Folder="TruckHideAndSeek",Scene="Assets/TruckHideAndSeek/Scenes/TruckHideAndSeek.unity";
  public static void Create()
  {
   if(File.Exists(Scene))throw new InvalidOperationException("Preserve the existing Hide & Seek scene; use targeted repairs.");var m=SportsEventAuthoring.Begin<HideMatch>(Folder);m.setup.count=8;m.option=2;m.NormalizeSetup();for(int i=1;i<8;i++)m.setup.players[i].source=LWS.TruckBowling.BowlingSource.AI;m.feedback.clips=LWS.TruckHockey.Editor.HockeyAudioAuthoring.Create();
   m.world=new GameObject("Hide & Seek industrial world metadata").AddComponent<HideWorld>();ReuseWorld(m);var spots=new List<Vector3>();OpenWarehouses(m,spots);ContainerHides(m,spots);
   m.world.hidingSpots=spots.ToArray();m.world.searchSpots=spots.SelectMany(p=>new[]{p+Vector3.back*24,p}).ToArray();ConnectServiceYards(m);AuditSpots(m);m.world.starts=new Vector3[8];m.world.rotations=new Quaternion[8];m.trucks=new SportsEventTruck[8];m.engines=new HideEngine[8];m.fragments=new SportsFragmentRig[8];
   for(int i=0;i<8;i++)
   {
    var p=new Vector3(i%2==0?-6:6,0,-120+i/2*30);p.y=m.world.Height(p)+.65f;m.world.starts[i]=p;m.world.rotations[i]=Quaternion.identity;var t=FareFightVehicleAuthoring.Spawn(m,i,p,Quaternion.identity);m.trucks[i]=t;m.fragments[i]=SportsFragmentAuthoring.InstallRigid(t.body,t.visuals.GetComponentsInChildren<MeshFilter>(true),Folder);var engine=t.gameObject.AddComponent<HideEngine>();engine.truck=t;m.engines[i]=engine;t.tractorMotor.forceSpatialAudio=true;var contact=t.gameObject.AddComponent<HideContacts>();contact.match=m;contact.truck=t;Headlights(t);
   }
   m.perception=m.gameObject.AddComponent<HidePerception>();m.perception.match=m;m.broadcast=SportsEventAuthoring.Camera("Industrial original-seeker spectator camera");m.broadcast.view.farClipPlane=3200;m.framing=m.broadcast.gameObject.AddComponent<SportsCameraFit>();m.framing.cameraTarget=m.broadcast.view;m.cameras=new[]{m.broadcast.view};SportsEventAuthoring.UI(m);SportsRallyAuthoring.CompactUI(m);m.drivingViews=SportsDrivingViewsAuthoring.Install(m,m.trucks,m.broadcast,Folder);m.drivingViews.independentViewModes=true;m.drivingViews.localElevated=true;m.drivingViews.defaultMode=1;foreach(var view in m.drivingViews.rally.views)view.view.farClipPlane=3200;
   m.atmosphere=SportsChaosAuthoring.Weather(m,Folder);m.atmosphere.presets=SportsWeatherPresetsAuthoring.EnsureCatalog();m.drivingViews.rally.weather=m.atmosphere;SportsWeatherPickerAuthoring.Install(m,m.atmosphere.presets,new Vector2(350,-180));Night(m);CompassHints(m);var hud=m.gameObject.AddComponent<SportsExpansionViewHud>();hud.match=m;hud.views=m.drivingViews;SportsUIAuthoring.Button(m.menus.pages[6].transform,"WATCH THE SEARCH","watchhide",new Vector2(0,-245),new Vector2(520,48));Artwork(m);HidePresentation.Install(m);SportsBroadcastAuthoring.ApplyScene();SportsScenicGradeAuthoring.Install(m.gameObject,new[]{m.broadcast.view}.Concat(m.drivingViews.rally.views.Select(v=>v.view)).ToArray(),Folder,-.15f);SportsMatchAudioAuthoring.Install(m,m.trucks);ValidateArea(m);SportsEventAuthoring.Save(Folder);HidePresentation.BindRoadSnow(m);EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
  }
  public static void InspectWarehouse()
  {
   EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);var root=new GameObject("Owned warehouse fit diagnosis").transform;LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(root,"Assets/JC_LP_MegaCity/Prefabs/Buildings/SM_Buildings_Warehouse_01.prefab",new Vector3(78,38,72),0,false,Folder);Debug.Log("HIDE_WAREHOUSE_BOUNDS "+BoundsOf(root));foreach(var t in root.GetComponentsInChildren<Transform>())if(t.name.Contains("Gate_")||t.name.EndsWith("_Door"))Debug.Log("HIDE_WAREHOUSE_OPENING "+t.name+" position="+t.position+" rotation="+t.rotation.eulerAngles+" visible="+BoundsOf(t));
  }
  static void ReuseWorld(HideMatch m)
  {
   var dest=SceneManager.GetActiveScene();var source=EditorSceneManager.OpenPreviewScene("Assets/TruckSnake/Scenes/TruckSnake.unity");
   try
   {
    var original=source.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SnakeWorld>(true)).Single();m.world.graph=JsonUtility.FromJson<LWS.InterstateHauler.LwsRoadGraph>(JsonUtility.ToJson(original.graph));m.world.graph.graphId="hide-seek-connected-industrial-world";m.world.playableBounds=original.playableBounds;
    foreach(var root in source.GetRootGameObjects())
    {
     if(!(root.name=="Composed five-region owned environment"||root.name=="EasyRoads connected regional lanes and flyovers"||root.name.StartsWith("Snake Downtown")||original.terrain.Any(t=>t.transform.root==root.transform)))continue;
     var copy=UnityEngine.Object.Instantiate(root);copy.name="Hide & Seek reused "+root.name;SceneManager.MoveGameObjectToScene(copy,dest);
     foreach(var behavior in copy.GetComponentsInChildren<MonoBehaviour>(true))if(behavior!=null&&!(behavior is LWS.InterstateHauler.LwsRoadSurface)&&!(behavior is LWS.TruckTaxi.TruckTaxiSurface))UnityEngine.Object.DestroyImmediate(behavior);
     // Replace only this sport's cloned cargo layout with the deliberate
     // truck-clear courtyards below. Source Snake scenery is never modified.
     foreach(var cargo in copy.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Physical cargo container ")||t.name.StartsWith("Industrial warehouse ")).ToArray())UnityEngine.Object.DestroyImmediate(cargo.gameObject);
    }
   }
   finally{EditorSceneManager.ClosePreviewScene(source);SceneManager.SetActiveScene(dest);}
   m.world.terrain=UnityEngine.Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include).Where(t=>t.gameObject.scene==dest).ToArray();if(m.world.terrain.Length==0)throw new InvalidOperationException("The existing Vista terrain must be composed, not replaced with a flat test floor.");
  }
  static Bounds BoundsOf(Transform root){var all=root.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();if(all.Length==0)throw new InvalidOperationException("No visible model geometry.");var b=all[0].bounds;foreach(var r in all)b.Encapsulate(r.bounds);return b;}
  static void OpenWarehouses(HideMatch m,List<Vector3> spots)
  {
   const string kit="Assets/Hivemind/ModularWarehouse/URP/Art/Prefabs/";const string props="Assets/LeartesStudios/Garage Warehouse Environment/Art/Prefabs/";
   var root=new GameObject("Owned open warehouses and truck-sized industrial hiding places").transform;
   for(int i=0;i<9;i++)
   {
    var p=new Vector3(760+i%3*240,0,-260+i/3*300);float floor=-100;for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++)floor=Mathf.Max(floor,m.world.Height(p+new Vector3(x*30,0,z*26)));p.y=floor+.12f;var hangar=new GameObject("Actual open modular industrial hangar "+i).transform;hangar.SetParent(root);hangar.position=p;
    for(int x=0;x<9;x++)for(int z=0;z<7;z++){Module(hangar,kit+"SM_Floor_Concrete_600_A.prefab",new Vector3((x-4)*6,0,(z-3)*6),new Vector3(6,.18f,6));Module(hangar,kit+"SM_Ceiling_Hangar_600_A.prefab",new Vector3((x-4)*6,12,(z-3)*6),new Vector3(6,1.5f,6));}
    for(int tier=0;tier<4;tier++)
    {
     for(int col=0;col<9;col++){Module(hangar,kit+"SM_Wall_Hangar_600_300_A.prefab",new Vector3((col-4)*6,tier*3,21),new Vector3(6,3,.45f));if(col<3||col>5)Module(hangar,kit+"SM_Wall_Hangar_600_300_A.prefab",new Vector3((col-4)*6,tier*3,-21),new Vector3(6,3,.45f),180);}
     foreach(int side in new[]{-1,1})for(int col=0;col<7;col++)Module(hangar,kit+"SM_Wall_Hangar_600_300_A.prefab",new Vector3(side*27,tier*3,(col-3)*6),new Vector3(6,3,.45f),side*90);
    }
    // Real six-metre purchased panels continue through the eighteen-metre
    // entrance. No raised loading-door mesh, hidden front wall or fake gap.
    for(int x=-1;x<=1;x++)for(int z=0;z<2;z++)Module(hangar,kit+"SM_Floor_Concrete_600_A.prefab",new Vector3(x*6,0,-24-z*6),new Vector3(6,.18f,6));
    for(int j=0;j<4;j++){Module(hangar,props+"SM_MetalShelve_01.prefab",new Vector3((j-1.5f)*11,0,17),new Vector3(5,5,2));Module(hangar,props+"SM_PK04_WoodenCrate_01.prefab",new Vector3((j-1.5f)*11,0,13),new Vector3(2,2,2));}
    Module(hangar,props+"SM_ToolCabinet_01.prefab",new Vector3(23,0,3),new Vector3(2,2.5f,1.5f),-90);spots.Add(p+new Vector3(0,.2f,-8));
   }
   foreach(var p in new[]{new Vector3(-600,0,650),new Vector3(600,0,650),new Vector3(-900,0,680),new Vector3(900,0,680),new Vector3(-750,0,160),new Vector3(-1030,0,240)}){var point=p;point.y=m.world.Height(point);spots.Add(point);}
  }
  static void Module(Transform parent,string path,Vector3 local,Vector3 size,float yaw=0)
  {
   var holder=new GameObject("Installed interior module · "+Path.GetFileNameWithoutExtension(path)).transform;holder.SetParent(parent,false);holder.localPosition=local;holder.localRotation=Quaternion.Euler(0,yaw,0);LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(holder,path,size,0,true,Folder);BindModuleMaterials(holder,path);foreach(var filter in holder.GetComponentsInChildren<MeshFilter>())if(filter.sharedMesh!=null)filter.gameObject.AddComponent<MeshCollider>().sharedMesh=filter.sharedMesh;if(path.Contains("Floor"))SportsDrivingSurfaceAuthoring.MarkHierarchy(holder);
  }
  static void BindModuleMaterials(Transform holder,string prefab)
  {
   const string kit="Assets/Hivemind/ModularWarehouse/URP/Art/ENV/WorldBuilding/Architecture/TEX/";const string props="Assets/LeartesStudios/Garage Warehouse Environment/Art/Textures/";
   foreach(var renderer in holder.GetComponentsInChildren<Renderer>())
   {
    var materials=renderer.sharedMaterials;for(int i=0;i<materials.Length;i++)
    {
     string old=materials[i]==null?"":materials[i].name;string stem,baseMap,normal;float metal=0,smooth=.2f;
     if(prefab.Contains("ToolCabinet")){stem="Actual painted tool cabinet atlas";baseMap=props+"T_Toolcabinet_D.PNG";normal=props+"T_Toolcabinet_N.png";metal=.2f;}
     else if(prefab.Contains("WoodenCrate")){stem="Actual battered wooden cargo crate atlas";baseMap=props+"T_PropsKit04_D.PNG";normal=props+"T_PropsKit04_N.png";}
     else if(prefab.Contains("MetalShelve")){stem="Actual weathered warehouse shelving atlas";baseMap=props+"T_BigShelve_D.PNG";normal=props+"T_BigShelve_N.png";metal=.35f;}
     else if(old.Contains("Metal")||old.Contains("Trim")){stem="Actual gunmetal architectural trim";baseMap=kit+"T_Metal_Trim_C_ALB.png";normal=kit+"T_Metal_Trim_C_N.png";metal=.55f;smooth=.35f;}
     else if(old.Contains("Plastic")||old.Contains("Sheet")){stem="Actual weathered industrial sheet wall";baseMap=kit+"T_Wall_Plastic_Tile_A_ALB.png";normal=kit+"T_Wall_Plastic_Tile_A_N.png";}
     else{stem="Actual textured concrete warehouse panels";baseMap=kit+"T_Concrete_Panels_Tile_A_ALB.png";normal=kit+"T_Concrete_Panels_Tile_A_N.png";}
     var material=SportsEventAuthoring.Mat(Folder,stem,Color.white,smooth);var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(baseMap);if(texture==null)throw new InvalidOperationException("Existing module texture required: "+baseMap);material.mainTexture=texture;material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(normal));material.SetFloat("_BumpScale",.75f);material.SetFloat("_Metallic",metal);material.EnableKeyword("_NORMALMAP");materials[i]=material;
    }
    renderer.sharedMaterials=materials;
   }
  }
  public static void UpgradeLightingAndMaterials()
  {
   EditorSceneManager.OpenScene(Scene);var m=UnityEngine.Object.FindAnyObjectByType<HideMatch>();var sun=m.atmosphere.vendor.GetComponentsInChildren<WeatherMakerCelestialObjectScript>(true).First(x=>x.IsSun);foreach(var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include))if(light.type==LightType.Directional&&light.GetComponent<WeatherMakerCelestialObjectScript>()==null)light.enabled=false;m.atmosphere.sun=sun.GetComponent<Light>();RenderSettings.sun=m.atmosphere.sun;
   foreach(var holder in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))if(holder.name.StartsWith("Installed interior module · "))BindModuleMaterials(holder,holder.name);
   foreach(var renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude)){string name=renderer.name.ToLowerInvariant();if(renderer.GetComponentInParent<Rigidbody>()==null&&(name.Contains("lamp")||name.Contains("streetlight")||name.Contains("lightpole")))Debug.Log("HIDE_STATIC_LIGHT_PROP "+renderer.name+" parent="+(renderer.transform.parent==null?"ROOT":renderer.transform.parent.name)+" base="+renderer.bounds.min+" ground="+m.world.Height(renderer.bounds.center));}
   EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();Debug.Log("HIDE_VENDOR_NIGHT_SUN_AND_TEXTURES_BOUND");
  }
  public static void UpgradeWarehouses()
  {
   EditorSceneManager.OpenScene(Scene);var m=UnityEngine.Object.FindAnyObjectByType<HideMatch>();foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects())if(root.name=="Owned open warehouses and truck-sized industrial hiding places"||root.name=="Truck-sized cargo courtyards and layered container cover")UnityEngine.Object.DestroyImmediate(root);m.world.graph.nodes.RemoveAll(n=>n.nodeId.StartsWith("hide.service.node."));m.world.graph.edges.RemoveAll(e=>e.edgeId.StartsWith("hide.service."));var spots=new List<Vector3>();OpenWarehouses(m,spots);ContainerHides(m,spots);m.world.hidingSpots=spots.ToArray();m.world.searchSpots=spots.SelectMany(point=>new[]{point+Vector3.back*24,point}).ToArray();ConnectServiceYards(m);AuditSpots(m);for(int i=0;i<8;i++){var start=new Vector3(i%2==0?-6:6,0,-120+i/2*30);start.y=m.world.Height(start)+.65f;m.world.starts[i]=start;}Physics.SyncTransforms();ValidateArea(m);EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();Debug.Log("HIDE_MODULAR_INTERIORS_SAVED count="+spots.Count);
  }
  static void ContainerHides(HideMatch m,List<Vector3> spots)
  {
   var parent=new GameObject("Truck-sized cargo courtyards and layered container cover").transform;
   for(int i=0;i<20;i++)
   {
    var p=new Vector3(-580+i%5*230,0,-470-i/5*130);p.y=m.world.Height(p);foreach(int side in new[]{-1,1})
    {
     var holder=new GameObject("Visible solid cargo courtyard wall "+i+" "+side).transform;holder.SetParent(parent);holder.position=p+Vector3.right*side*12;var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ShipContainerPack/URP/Prefabs/ShipContainerRed_URP.prefab");var visual=SportsReplayVisuals.Copy(source.transform);visual.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);var b=BoundsOf(visual.transform);if(b.size.x>b.size.z)visual.transform.Rotate(0,90,0);b=BoundsOf(visual.transform);float scale=Mathf.Min(5/b.size.x,6/b.size.y,24/b.size.z);visual.transform.localScale*=scale;visual.transform.position=-new Vector3(b.center.x,b.min.y,b.center.z)*scale;visual.transform.SetParent(holder,false);b=BoundsOf(holder);
     var shape=holder.gameObject.AddComponent<BoxCollider>();shape.center=holder.InverseTransformPoint(b.center);shape.size=Quaternion.Inverse(holder.rotation)*b.size;shape.size=new Vector3(Mathf.Abs(shape.size.x),Mathf.Abs(shape.size.y),Mathf.Abs(shape.size.z));
    }
    spots.Add(p);
   }
  }
  static void ConnectServiceYards(HideMatch m)
  {
   // Mode-owned drivable service-yard metadata extends the existing LWS graph.
   // The existing planner remains the only route authority. Actual PhysX
   // clearance, rather than a direct line through a warehouse, admits a link.
   var graph=m.world.graph;var added=new List<LWS.InterstateHauler.LwsRoadNode>();foreach(float x in new[]{700f,830,1010,1190,1300})foreach(float z in new[]{-400f,-264,-132,0,132,264,396}){var p=new Vector3(x,0,z);p.y=m.world.Height(p)+.1f;added.Add(Node(p));}foreach(var p in m.world.searchSpots)added.Add(Node(p));Physics.SyncTransforms();
   foreach(var a in added){int links=0;foreach(var b in graph.nodes.Where(n=>n!=a).OrderBy(n=>(n.position-a.position).sqrMagnitude).Take(32)){var delta=b.position-a.position;float distance=delta.magnitude;if(distance>220)break;if(distance<1)continue;bool blocked=false;foreach(var hit in Physics.BoxCastAll(a.position+Vector3.up*2.2f,new Vector3(2.6f,1.8f,5.8f),delta.normalized,Quaternion.LookRotation(Vector3.ProjectOnPlane(delta,Vector3.up)),distance,1,QueryTriggerInteraction.Ignore))if(hit.normal.y<.65f){blocked=true;break;}if(blocked)continue;var edge=new LWS.InterstateHauler.LwsRoadEdge{edgeId="hide.service."+graph.edges.Count,roadId="hide.service.road."+graph.edges.Count,segmentId="hide.service."+graph.edges.Count,fromNodeId=a.nodeId,toNodeId=b.nodeId,roadClass=LWS.InterstateHauler.LwsRoadClass.LocalRoad,direction=LWS.InterstateHauler.LwsRoadDirection.Bidirectional,surfaceType=LWS.InterstateHauler.LwsRoadSurfaceType.AsphaltInterstate,oneWay=false,speedLimitMph=15,laneCount=1,laneWidthMeters=8,distanceMeters=distance,travelCost=distance,stateOrRegionId="hide.service",sceneChunkId="hide.world"};edge.samples.Add(new LWS.InterstateHauler.LwsRoadSample{position=a.position,forward=delta.normalized,up=Vector3.up,roadWidthMeters=8,laneCount=1,laneWidthMeters=8,speedLimitMph=15});edge.samples.Add(new LWS.InterstateHauler.LwsRoadSample{position=b.position,forward=delta.normalized,up=Vector3.up,distanceFromStartMeters=distance,roadWidthMeters=8,laneCount=1,laneWidthMeters=8,speedLimitMph=15});graph.edges.Add(edge);if(++links>=4)break;}if(links==0)Debug.LogWarning("HIDE_SERVICE_POINT_NOT_CONNECTED "+a.position);}
   if(!graph.Validate().IsValid)throw new InvalidOperationException("Hide service graph metadata invalid: "+graph.Validate().Summary);
   LWS.InterstateHauler.LwsRoadNode Node(Vector3 p){var node=new LWS.InterstateHauler.LwsRoadNode{nodeId="hide.service.node."+graph.nodes.Count,position=p,regionId="hide.service",sceneChunkId="hide.world"};graph.nodes.Add(node);return node;}
  }
  static void AuditSpots(HideMatch m)
  {
   var valid=new List<Vector3>();var origin=new Vector3(-6,0,-120);origin.y=m.world.Height(origin)+.2f;foreach(var p in m.world.hidingSpots){bool clear=true;foreach(var c in Physics.OverlapBox(p+Vector3.up*2.2f,new Vector3(2.6f,1.8f,5.8f),Quaternion.identity,1,QueryTriggerInteraction.Ignore))if(!(c is TerrainCollider)){clear=false;break;}var route=new LWS.TruckFareFight.FareFightRoute();route.Update(m.world.graph,origin,p,0,0,true,Vector3.forward);if(clear&&route.points.Count>0)valid.Add(p);else Debug.LogWarning("HIDE_CANDIDATE_REJECTED clear="+clear+" route="+route.points.Count+" position="+p);}m.world.hidingSpots=valid.ToArray();m.world.searchSpots=valid.SelectMany(p=>new[]{p+Vector3.back*24,p}).ToArray();if(valid.Count<24)throw new InvalidOperationException("Fewer than24actual truck-clear route-connected hiding places: "+valid.Count);Debug.Log("HIDE_CLEAR_CONNECTED_PLACES "+valid.Count);
  }
  static void Headlights(SportsEventTruck t)
  {
   var sources=t.tractorMotor.vehicle.effectsManager.lightsManager.lowBeamLights.lightSources;
   if(sources.Any(s=>s.light!=null))return;foreach(int side in new[]{-1,1}){var light=new GameObject("NWH-controlled stealth low-beam headlamp").AddComponent<Light>();light.transform.SetParent(t.transform,false);light.transform.localPosition=new Vector3(side*1.1f,1.15f,5.1f);light.type=LightType.Spot;light.range=85;light.spotAngle=70;light.intensity=7;light.color=new Color(1,.93f,.7f);light.shadows=LightShadows.None;light.enabled=false;sources.Add(new NWH.VehiclePhysics2.Effects.LightSource{type=NWH.VehiclePhysics2.Effects.LightSource.LightType.Light,light=light,emissionColor=light.color});}
  }
  static void Night(HideMatch m)
  {
   var manager=m.atmosphere.vendor.GetComponentInChildren<WeatherMakerDayNightCycleManagerScript>(true);var profile=UnityEngine.Object.Instantiate(manager.DayNightProfile);profile.name="Hide and seek fixed moonlit industrial evening";profile.TimeOfDay=21*3600;profile.Speed=profile.NightSpeed=0;AssetDatabase.CreateAsset(profile,"Assets/"+Folder+"/Materials/Fixed moonlit industrial evening.asset");manager.DayNightProfile=profile;var actualSun=m.atmosphere.vendor.GetComponentsInChildren<WeatherMakerCelestialObjectScript>(true).First(x=>x.IsSun);foreach(var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include))if(light.type==LightType.Directional&&light.GetComponent<WeatherMakerCelestialObjectScript>()==null)light.enabled=false;m.atmosphere.sun=actualSun.GetComponent<Light>();RenderSettings.sun=m.atmosphere.sun;RenderSettings.ambientLight=new Color(.18f,.23f,.32f);
   var material=SportsEventAuthoring.Mat(Folder,"Industrial warm loading light housing",new Color(.32f,.3f,.22f),.4f);for(int i=0;i<18;i++){var p=m.world.searchSpots[i*2];var lamp=new GameObject("Warm industrial loading-area light "+i).AddComponent<Light>();lamp.transform.position=p+Vector3.up*10;lamp.type=LightType.Spot;lamp.transform.rotation=Quaternion.Euler(90,0,0);lamp.range=34;lamp.spotAngle=110;lamp.intensity=22;lamp.color=new Color(1,.73f,.38f);lamp.shadows=LightShadows.None;SportsEventAuthoring.Box("Loading lamp visible steel mount",lamp.transform.position+Vector3.up*.25f,new Vector3(1,.3f,.8f),material);}
  }
  static void CompassHints(HideMatch m)
  {
   var hints=m.gameObject.AddComponent<HideHints>();hints.match=m;m.hints=hints;hints.compasses=new CompassPro[4];hints.clues=new CompassProPOI[4];var icon=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Plugins/Kronnect/CompassNavigatorPro/Resources/CNPro/Sprites/icon-arrow-white.png");
   for(int i=0;i<4;i++){var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Plugins/Kronnect/CompassNavigatorPro/Resources/CNPro/Prefabs/CompassNavigatorPro.prefab"));root.name="CNP broad seeker clue viewport "+i;var compass=root.GetComponentInChildren<CompassPro>(true);hints.compasses[i]=compass;compass.compassGroup=i+1;compass.showMiniMap=false;compass.showCompassBar=false;compass.follow=m.trucks[0].transform;compass.cameraMain=m.drivingViews.rally.views[i].view;var canvas=compass.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=compass.cameraMain;canvas.planeDistance=.5f;foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=26+i;var poi=new GameObject("Artificial broad-sector hint; never attached to a hider").AddComponent<CompassProPOI>();poi.compassGroupMask=1<<i;poi.iconNonVisited=poi.iconVisited=icon;poi.iconShowDistance=poi.onScreenIndicatorShowDistance=poi.offScreenIndicatorShowDistance=false;poi.onScreenIndicatorShowTitle=true;poi.showOnScreenIndicator=poi.showOffScreenIndicator=true;poi.tintColor=new Color(1,.8f,.13f);poi.title="BROAD CLUE";poi.iconScale=.7f;hints.clues[i]=poi;poi.gameObject.SetActive(false);root.SetActive(false);}
  }
  static void Artwork(HideMatch m)
  {
   var logo=SportsRetroMenuAuthoring.LoadLogo(Folder);foreach(int i in new[]{0,4}){var page=m.menus.pages[i];var art=SportsUIAuthoring.Rect("Original sport title artwork",page.transform,new Vector2(260,160),new Vector2(-560,365)).AddComponent<UnityEngine.UI.Image>();art.sprite=logo;art.preserveAspect=true;art.raycastTarget=false;var title=page.transform.Find("Title").GetComponent<TMP_Text>();title.rectTransform.anchoredPosition+=Vector2.right*140;title.rectTransform.sizeDelta=new Vector2(1000,title.rectTransform.sizeDelta.y);}
   var material=SportsEventAuthoring.Mat(Folder,"Original industrial hide and seek venue sign",Color.white);material.shader=Shader.Find("Universal Render Pipeline/Unlit");material.mainTexture=logo.texture;material.SetFloat("_Surface",1);material.SetFloat("_SrcBlend",5);material.SetFloat("_DstBlend",10);material.SetFloat("_ZWrite",0);material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.renderQueue=3000;foreach(var p in new[]{new Vector3(0,14,-200),new Vector3(755,16,0)}){var sign=GameObject.CreatePrimitive(PrimitiveType.Quad);sign.name="Original Hide & Seek industrial event artwork";UnityEngine.Object.DestroyImmediate(sign.GetComponent<Collider>());sign.transform.position=p;sign.transform.localScale=new Vector3(30,20,1);sign.GetComponent<Renderer>().sharedMaterial=material;}
  }
  static void ValidateArea(HideMatch m)
  {
   Physics.SyncTransforms();double area=0;const int step=25;for(float z=m.world.playableBounds.min.z+25;z<m.world.playableBounds.max.z-25;z+=step)for(float x=m.world.playableBounds.min.x+25;x<m.world.playableBounds.max.x-25;x+=step){var p=new Vector3(x,0,z);float h=m.world.Height(p);var tile=Array.Find(m.world.terrain,t=>x>=t.transform.position.x&&x<=t.transform.position.x+t.terrainData.size.x&&z>=t.transform.position.z&&z<=t.transform.position.z+t.terrainData.size.z);if(tile==null)continue;var size=tile.terrainData.size;if(tile.terrainData.GetSteepness((x-tile.transform.position.x)/size.x,(z-tile.transform.position.z)/size.z)>24)continue;bool blocked=false;foreach(var c in Physics.OverlapBox(new Vector3(x,h+2,z),new Vector3(5,1.7f,7),Quaternion.identity,1,QueryTriggerInteraction.Ignore))if(!(c is TerrainCollider)&&c.bounds.max.y>h+1&&c.bounds.min.y<h+4){blocked=true;break;}if(!blocked)area+=step*step;}m.world.verifiedUsableSquareMetres=(float)area;Directory.CreateDirectory("Logs/TruckSports/EighteenSports/Hide");File.WriteAllText("Logs/TruckSports/EighteenSports/Hide/area.txt","Actual existing terrain and cloned physical world; 25m raster slope <=24deg, truck clearance sample; usable="+area+"m2 / "+area/2589988.110336+" sq miles. This area check alone does not prove route connectivity or hiding quality.");if(area<2589988.110336)throw new InvalidOperationException("At least one square mile of actual truck-clear usable terrain is required.");
  }
 }
}
