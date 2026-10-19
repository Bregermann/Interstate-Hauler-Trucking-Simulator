using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using CompassNavigatorPro;
using LWS.TruckSports;
using LWS.TruckSports.Editor;
using LWS.TruckTaxi;
using LWS.InterstateHauler;
namespace LWS.TruckFareFight.Editor {
 public static class FareFightAuthoring {
  public const string Folder="TruckFareFight",Scene="Assets/TruckFareFight/Scenes/TruckFareFight.unity";
  const string OriginalCity="Assets/LWS/TruckTaxi/Scenes/TruckTaxi_DemoCity.unity",OriginalTown="Assets/LWS/TruckTaxi/Regional/Scenes/Taxi_Town01.unity";
  [MenuItem("Truck Sports/Fare Fight/Create original-town competition")]
  public static void Create(){
   if(File.Exists(Scene))throw new InvalidOperationException("Preserve the authored Fare Fight scene; use targeted upgrades.");
   foreach(string directory in new[]{"Configuration","Prefabs","Rendering"})Directory.CreateDirectory("Assets/"+Folder+"/"+directory);
   var m=SportsEventAuthoring.Begin<FareFightMatch>(Folder);m.setup.count=4;m.option=1;m.NormalizeSetup();
   for(int i=1;i<4;i++)m.setup.players[i].source=LWS.TruckBowling.BowlingSource.AI;
   var course=ScriptableObject.CreateInstance<FareFightDistrict>();course.name="Original Taxi town competition";
   var source=EditorSceneManager.OpenPreviewScene(OriginalCity);
   try{
    var roots=source.GetRootGameObjects();var provider=roots.SelectMany(r=>r.GetComponentsInChildren<LwsRoadGraphProvider>(true)).First();
    course.graph=JsonUtility.FromJson<LwsRoadGraph>(JsonUtility.ToJson(provider.Graph));course.graph.graphId="fare-fight-original-taxi-town";
    course.graph.nodes=course.graph.nodes.Where(n=>n.regionId=="taxi.demo").ToList();var ids=new HashSet<string>(course.graph.nodes.Select(n=>n.nodeId));
    course.graph.edges=course.graph.edges.Where(e=>ids.Contains(e.fromNodeId)&&ids.Contains(e.toNodeId)).ToList();
    if(course.graph.nodes.Count<25||!course.graph.Validate().IsValid)throw new InvalidOperationException("Original-town graph must remain valid and connected.");
    course.stops=roots.SelectMany(r=>r.GetComponentsInChildren<TruckTaxiRideLocation>(true)).Where(s=>System.Text.RegularExpressions.Regex.IsMatch(s.locationId??"","^taxi\\.stop\\.[0-9]+$"))
     .OrderBy(s=>s.locationId,StringComparer.Ordinal).Select(s=>new FareFightDistrict.Stop{id=s.locationId,label=s.locationName,position=s.StopPosition}).ToArray();
    if(course.stops.Length!=20)throw new InvalidOperationException("Expected the original twenty authored town stops, not regional or optional private locations.");
    Vector3 min=course.graph.nodes[0].position,max=min;foreach(var n in course.graph.nodes){min=Vector3.Min(min,n.position);max=Vector3.Max(max,n.position);}course.playableBounds=new Bounds((min+max)*.5f,new Vector3(max.x-min.x+65,100,max.z-min.z+65));
    course.startingPositions=new[]{new Vector3(-160,1.7f,-280),new Vector3(160,1.7f,280),new Vector3(-280,1.7f,160),new Vector3(280,1.7f,-160)};
    course.startingForwards=new[]{Vector3.forward,Vector3.back,Vector3.right,Vector3.left};
    m.traffic=CloneRoot<TruckTaxiTrafficAdapter>(roots,"UTS Taxi Traffic");
    foreach(var rival in m.traffic.GetComponentsInChildren<TruckTaxiRivalPopulation>(true))UnityEngine.Object.DestroyImmediate(rival);
    m.traffic.rivalTruckPrefab=null;m.traffic.maximumDedicatedVehicles=0;
    m.traffic.cityLanes=m.traffic.cityLanes.Where(l=>l.centerline.All(course.Contains)).ToArray();m.traffic.maximumVehicles=28;
    m.traffic.maxFullTraffic=m.traffic.maxVisibleTraffic=48;m.traffic.fullTrafficRadius=m.traffic.visibleTrafficRadius=650;
    m.pedestrians=CloneRoot<TruckTaxiPedestrianPopulation>(roots,"UTS Taxi Pedestrians");
    foreach(Transform child in m.pedestrians.transform.Cast<Transform>().ToArray())if(child.name.StartsWith("Town 02"))UnityEngine.Object.DestroyImmediate(child.gameObject);
    m.pedestrians.peoplePaths=m.pedestrians.peoplePaths.Where(p=>p!=null).ToArray();m.pedestrians.pedestrianAreas=m.pedestrians.pedestrianAreas.Where(p=>p!=null&&course.Contains(p.transform.position)).ToArray();
    m.pedestrians.signalIntersections=m.pedestrians.signalIntersections.Where(p=>p!=null&&course.Contains(p.transform.position)).ToArray();m.pedestrians.roadLanes=m.traffic.cityLanes;
    m.pedestrians.fullPhysicsRadius=m.pedestrians.visibleRadius=650;m.pedestrians.maxFullPedestrians=100;m.pedestrians.maxVisiblePedestrians=180;
    var density=ScriptableObject.CreateInstance<TruckTaxiPopulationProfile>();density.name="Fare Fight shared-district population";density.measuredTrafficBaseline=28;density.trafficDensityMultiplier=1.5f;density.maximumActiveTraffic=42;density.measuredPedestrianBaseline=120;density.pedestrianDensityMultiplier=1.25f;density.maximumActivePedestrians=150;density.maximumActiveRagdolls=24;density.spawnRadius=560;density.despawnRadius=650;
    AssetDatabase.CreateAsset(density,"Assets/"+Folder+"/Configuration/City population.asset");m.traffic.densityProfile=m.pedestrians.densityProfile=density;
   }finally{EditorSceneManager.ClosePreviewScene(source);}
   AssetDatabase.CreateAsset(course,"Assets/"+Folder+"/Configuration/Original Taxi District.asset");m.district=course;
   var town=EditorSceneManager.OpenPreviewScene(OriginalTown);
   try{foreach(var root in town.GetRootGameObjects()){var clone=UnityEngine.Object.Instantiate(root);clone.name="Original Taxi town · "+root.name;SceneManager.MoveGameObjectToScene(clone,m.gameObject.scene);foreach(var camera in clone.GetComponentsInChildren<Camera>(true))UnityEngine.Object.DestroyImmediate(camera);foreach(var listener in clone.GetComponentsInChildren<AudioListener>(true))UnityEngine.Object.DestroyImmediate(listener);}}
   finally{EditorSceneManager.ClosePreviewScene(town);}
   // Explicit sporting closures at the town's regional road exits. They do not
   // cut the original connected grid or obstruct any chosen stop.
   Barriers(m);
   m.trucks=new SportsEventTruck[4];
   for(int i=0;i<4;i++){var truck=FareFightVehicleAuthoring.Spawn(m,i,course.startingPositions[i],Quaternion.LookRotation(course.startingForwards[i]));m.trucks[i]=truck;var contact=truck.gameObject.AddComponent<FareFightContact>();contact.truck=truck;contact.match=m;}
   m.broadcast=SportsEventAuthoring.Camera("Original town shared broadcast");m.broadcast.Pose(Vector3.zero,new Vector3(0,460,-260),65,true);m.cameras=new[]{m.broadcast.view};
   SportsEventAuthoring.UI(m);SportsRallyAuthoring.CompactUI(m);
   m.drivingViews=SportsDrivingViewsAuthoring.Install(m,m.trucks,m.broadcast,Folder);m.drivingViews.independentViewModes=true;m.drivingViews.defaultMode=2;
   m.drivingViews.cabMounts=m.trucks.Select(t=>t.GetComponentsInChildren<Camera>(true).First(c=>c.name=="Vehicle Camera Driver").transform).ToArray();
   m.drivingViews.rally.groundOffset=new Vector3(0,7,-17);m.drivingViews.rally.groundLookAhead=7;
   Markers(m);Actions(m);GPS(m);
   m.feedback.clips=LWS.TruckHockey.Editor.HockeyAudioAuthoring.Create();m.atmosphere=SportsChaosAuthoring.Weather(m,Folder);m.presets=SportsWeatherPresetsAuthoring.EnsureCatalog();m.drivingViews.rally.weather=m.atmosphere;
   SportsWeatherRenderingAuthoring.Install(m,Folder);SportsWeatherPickerAuthoring.Install(m,m.presets,new Vector2(0,355));SportsScenicGradeAuthoring.Install(m.gameObject,m.cameras,Folder,-.4f);
   FareFightPresentationAuthoring.Install(m);SportsEventAuthoring.Save(Folder);Validate();
  }
  static T CloneRoot<T>(GameObject[] roots,string name) where T:Component {var root=UnityEngine.Object.Instantiate(roots.First(r=>r.name==name));root.name="Fare Fight · "+name;return root.GetComponent<T>();}
  static void Barriers(FareFightMatch m){var paint=SportsEventAuthoring.Mat(Folder,"Regional closure reflective orange",new Color(1,.33f,.035f));var physics=SportsEventAuthoring.Contact(Folder,"District barrier",.5f,.03f);var parent=new GameObject("Deliberate excluded-district road closures").transform;foreach(var p in new[]{new Vector3(364,.9f,0),new Vector3(-364,.9f,0),new Vector3(0,.9f,364),new Vector3(0,.9f,-364)}){SportsEventAuthoring.Box("Closed regional route",p,new Vector3(p.x==0?38:2,1.8f,p.x==0?2:38),paint,physics,parent);var sign=SportsEventAuthoring.Label("Sport district boundary","FARE FIGHT\nROAD CLOSED",p+Vector3.up*2,.22f,Color.white);sign.transform.rotation=Quaternion.Euler(0,p.x==0?0:90,0);}}
  static void Markers(FareFightMatch m){m.pickupMarker=Marker("Blue passenger pickup",new Color(.05f,.65f,1));m.destinationMarker=Marker("Gold passenger destination",new Color(1,.7f,.04f));m.carrierMarker=Marker("Contested passenger carrier",new Color(.1f,1,.45f));m.carrierMarker.localScale=Vector3.one*.35f;
   var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TruckPedestrianPlowing/Prefabs/Characters/Standard/Standard Taxi Character.prefab");
   // Reuse the actual authored Taxi pedestrian asset, without any Plowing score role.
   var settings=AssetDatabase.LoadAssetAtPath<LWS.TruckPedestrianPlowing.PlowSettings>(LWS.TruckPedestrianPlowing.Editor.PlowAuthoring.SettingsPath);
   if(settings==null||settings.characters.Length==0)throw new InvalidOperationException("An existing Taxi pedestrian profile is required.");
   var entry=settings.characters.First(e=>e!=null&&e.prefab!=null);m.passengerVisual=SportsReplayVisuals.Copy(entry.prefab.transform);m.passengerVisual.name="Contested passenger · original authored character";m.passengerVisual.transform.SetPositionAndRotation(m.district.stops[0].position,Quaternion.identity);
  }
  static Transform Marker(string name,Color color){var root=new GameObject(name).transform;var line=root.gameObject.AddComponent<LineRenderer>();line.useWorldSpace=false;line.loop=true;line.positionCount=64;line.widthMultiplier=.35f;var mat=SportsEventAuthoring.Mat(Folder,name,color,.5f);mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",color*2);line.sharedMaterial=mat;for(int i=0;i<64;i++){float angle=i*Mathf.PI*2/64;line.SetPosition(i,new Vector3(Mathf.Sin(angle)*12,.35f,Mathf.Cos(angle)*12));}return root;}
  static void Actions(FareFightMatch m){
   var a=m.gameObject.AddComponent<FareFightActions>();a.match=m;m.interactions=a;a.needsSettings=AssetDatabase.LoadAssetAtPath<TruckTaxiEnvironmentSettings>("Assets/LWS/TruckTaxi/ScriptableObjects/TruckTaxi_EnvironmentSettings.asset");a.jugs=new FareFightJug[4];a.blasts=new ParticleSystem[4];a.cabJugs=new Transform[4];a.gestures=new LwsPlayerGestureController[4];
   a.horn=AssetDatabase.FindAssets("Horn t:AudioClip",new[]{"Assets/NWH"}).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<AudioClip>).FirstOrDefault();
   var sounds=LWS.TruckHockey.Editor.HockeyAudioAuthoring.Create();a.blastSound=sounds[0];
   var material=SportsEventAuthoring.Mat(Folder,"Reusable Taxi jug translucent amber",new Color(.85f,.65f,.12f),.65f);
   for(int i=0;i<4;i++){
    var truck=m.trucks[i];a.gestures[i]=truck.GetComponent<LwsPlayerGestureController>()??truck.gameObject.AddComponent<LwsPlayerGestureController>();var driver=truck.GetComponent<TruckTaxiDriverPresentation>()??truck.gameObject.AddComponent<TruckTaxiDriverPresentation>();
    var init=truck.gameObject.AddComponent<FareFightDriver>();init.truck=truck;init.presentation=driver;
    var jug=GameObject.CreatePrimitive(PrimitiveType.Cylinder);jug.name="Pooled thrown Taxi jug "+i;jug.transform.localScale=new Vector3(.3f,.35f,.3f);jug.GetComponent<Renderer>().sharedMaterial=material;var body=jug.AddComponent<Rigidbody>();body.mass=2;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;body.interpolation=RigidbodyInterpolation.Interpolate;var j=jug.AddComponent<FareFightJug>();j.body=body;j.shape=jug.GetComponent<Collider>();j.actions=a;j.owner=i;a.jugs[i]=j;body.isKinematic=true;body.detectCollisions=true;j.shape.enabled=false;jug.SetActive(false);
    foreach(var collider in truck.GetComponentsInChildren<Collider>(true))Physics.IgnoreCollision(j.shape,collider,true);
    var visual=new GameObject("Visible prepared jug").transform;visual.SetParent(m.drivingViews.cabs[i],false);visual.localPosition=new Vector3(.25f,-.27f,.3f);visual.localScale=new Vector3(.09f,.1f,.09f);visual.gameObject.AddComponent<MeshFilter>().sharedMesh=jug.GetComponent<MeshFilter>().sharedMesh;visual.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;visual.gameObject.layer=26+i;a.cabJugs[i]=visual;
    var effect=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TruckKeepaway/Prefabs/ComicBlast.prefab"));a.blasts[i]=effect.GetComponent<ParticleSystem>();SportsChaosAuthoring.FixBlast(a.blasts[i],SportsChaosAuthoring.Flare(Folder));var main=a.blasts[i].main;main.startSize=new ParticleSystem.MinMaxCurve(4,9);effect.SetActive(true);
   }
  }
  static void GPS(FareFightMatch m){var n=m.gameObject.AddComponent<FareFightNavigation>();n.match=m;m.navigation=n;n.gps=new CompassPro[4];
   for(int i=0;i<4;i++){var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Plugins/Kronnect/CompassNavigatorPro/Resources/CNPro/Prefabs/CompassNavigatorPro.prefab"));root.name="Independent Fare Fight GPS "+i;var compass=root.GetComponentInChildren<CompassPro>(true);n.gps[i]=compass;var canvas=compass.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=m.drivingViews.rally.views[i].view;canvas.planeDistance=.5f;
    compass.compassGroup=40+i;compass.cameraMain=canvas.worldCamera;compass.follow=m.trucks[i].transform;compass.showCompassBar=false;compass.showMiniMap=true;compass.miniMapPositionAndSize=MiniMapPositionAndScaleMode.UserDefined;compass.miniMapStyle=MiniMapStyle.SolidBox;compass.miniMapOrientation=MiniMapOrientation.Follow;compass.miniMapContents=MiniMapContents.TopDownWorldView;compass.miniMapResolution=MiniMapResolution._256;compass.miniMapCameraSnapshotFrequency=MiniMapCameraSnapshotFrequency.TimeInterval;compass.miniMapSnapshotInterval=.35f;compass.miniMapLayerMask=1;compass.miniMapCaptureSize=100;compass.miniMapCameraHeightVSFollow=180;compass.miniMapCameraDepth=240;compass.miniMapShowMaximizeButton=false;compass.miniMapShowZoomInOutButtons=false;compass.miniMapShowPlayerIcon=true;compass.routeShowOnMiniMap=true;compass.routeShowInWorld=false;compass.routeShowOnCompassBar=false;compass.routeWidth=3;compass.routeColor=new Color(.15f,1,.55f);
    foreach(var rect in root.GetComponentsInChildren<RectTransform>(true))if(rect.name=="MiniMap Root"){rect.anchorMin=rect.anchorMax=new Vector2(1,0);rect.pivot=new Vector2(1,0);rect.anchoredPosition=new Vector2(-18,18);rect.sizeDelta=new Vector2(200,155);}
    foreach(var child in root.GetComponentsInChildren<Transform>(true))child.gameObject.layer=26+i;root.SetActive(false);
   }
  }
  public static void Validate(){var m=UnityEngine.Object.FindAnyObjectByType<FareFightMatch>();if(m==null||m.district==null||m.trucks.Length!=4||m.district.stops.Length!=20||m.traffic==null||m.pedestrians==null||m.navigation.gps.Length!=4)throw new InvalidOperationException("Fare Fight scene dependencies incomplete.");if(UnityEngine.Object.FindObjectsByType<TruckTaxiBootstrap>(FindObjectsInactive.Include).Length!=0||UnityEngine.Object.FindObjectsByType<TruckTaxiRivalPopulation>(FindObjectsInactive.Include).Length!=0)throw new InvalidOperationException("Original Taxi bootstrap/rival system must not run in this sport.");if(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include).Length!=1)throw new InvalidOperationException("One shared listener required.");}
 }
}
