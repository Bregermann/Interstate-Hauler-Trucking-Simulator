using System;using System.IO;using System.Linq;
using UnityEngine;using UnityEngine.SceneManagement;using UnityEditor;using UnityEditor.SceneManagement;using TMPro;
using LWS.TruckSports;using LWS.TruckSports.Editor;using LWS.TruckFareFight.Editor;
namespace LWS.TruckJousting.Editor
{
 public static class JoustAuthoring
 {
  public const string Folder="TruckJousting",Scene="Assets/TruckJousting/Scenes/TruckJousting.unity";
  public static void Create()
  {
   if(File.Exists(Scene))throw new InvalidOperationException("Preserve the authored tournament scene; use explicit targeted upgrades.");
   var m=SportsEventAuthoring.Begin<JoustMatch>(Folder);m.setup.count=2;m.option=0;m.NormalizeSetup();m.feedback.clips=LWS.TruckHockey.Editor.HockeyAudioAuthoring.Create();
   var contact=SportsEventAuthoring.Contact(Folder,"Tournament turf driving contact",.85f,.02f);
   var grass=SportsEventAuthoring.Mat(Folder,"Vista tournament meadow",new Color(.5f,.61f,.3f),.08f);grass.mainTexture=AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/PinwheelStudio/Vista/Personal/Samples/TerrainLayers/Grass Light/Grass_Light_VistaSample.terrainlayer").diffuseTexture;grass.mainTextureScale=Vector2.one*45;
   var meadow=SportsEventAuthoring.Box("Continuous festival meadow surround",new Vector3(0,-1.1f,0),new Vector3(600,1,600),grass,contact);SportsDrivingSurfaceAuthoring.MarkHierarchy(meadow.transform);
   var earth=SportsEventAuthoring.Mat(Folder,"Trampled tournament lanes",new Color(.52f,.4f,.23f),.1f);earth.mainTexture=AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckHotPotato/Materials/Festival scorched clay.mat").mainTexture;earth.mainTextureScale=new Vector2(5,38);
   var lanes=SportsEventAuthoring.Box("Two generous physical jousting lanes",new Vector3(0,-.5f,0),new Vector3(38,1,340),earth,contact);SportsDrivingSurfaceAuthoring.MarkHierarchy(lanes.transform);
   var wood=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<Material>("Assets/RoadPropsPack/URP/Materials/M_WoodBox.mat"));wood.name="Tournament stained oak";wood.color=new Color(.54f,.32f,.14f);AssetDatabase.CreateAsset(wood,"Assets/"+Folder+"/Materials/Tournament stained oak.mat");
   var red=SportsEventAuthoring.Mat(Folder,"Weathered crimson tournament cloth",new Color(.65f,.035f,.055f),.2f);var blue=SportsEventAuthoring.Mat(Folder,"Faded royal blue tournament cloth",new Color(.035f,.14f,.58f),.2f);var gold=SportsEventAuthoring.Mat(Folder,"Tarnished heraldic gold",new Color(.95f,.68f,.16f),.45f);
   var steel=SportsEventAuthoring.Mat(Folder,"Tournament lance collar steel",new Color(.33f,.36f,.39f),.6f);
   for(int z=-155;z<=155;z+=10)
   {
    foreach(int side in new[]{-1,0,1})
    {
     float x=side*15;SportsEventAuthoring.Box("Modeled tournament oak rail",new Vector3(x,.65f,z),new Vector3(.3f,.24f,10),wood,contact);
     SportsEventAuthoring.Box("Oak rail support post",new Vector3(x,.5f,z-5),new Vector3(.45f,1,.45f),wood,contact);
    }
   }
   Flags(red,blue,steel);Venue();m.debrisGroup=new GameObject("Pooled Core Collapse oak splinters").transform;
   m.trucks=new SportsEventTruck[2];m.fragments=new SportsFragmentRig[2];m.lances=new JoustLance[2];m.mounts=new Transform[2];m.shields=new JoustShield[2];
   for(int i=0;i<2;i++)
   {
    var truck=FareFightVehicleAuthoring.Spawn(m,i,new Vector3(i==0?-4:4,.65f,i==0?-115:115),Quaternion.Euler(0,i==0?0:180,0));m.trucks[i]=truck;m.fragments[i]=SportsFragmentAuthoring.InstallRigid(truck.body,truck.visuals.GetComponentsInChildren<MeshFilter>(true),Folder);
    var shieldRoot=new GameObject("Actual opponent shield contact zone");shieldRoot.transform.SetParent(truck.transform,false);shieldRoot.transform.localPosition=new Vector3(1.5f,2.3f,.7f);
    var shape=shieldRoot.AddComponent<BoxCollider>();shape.size=new Vector3(2,2,.3f);shape.sharedMaterial=contact;
    var shield=shieldRoot.AddComponent<JoustShield>();shield.match=m;shield.owner=i;m.shields[i]=shield;
    Disc(shieldRoot.transform,"Painted armored shield",Vector3.zero,2,i==0?red:blue);Disc(shieldRoot.transform,"Raised gold central scoring boss",new Vector3(0,0,.18f),.8f,gold);
    var lanceRoot=new GameObject("Side-mounted adjustable tournament lance");lanceRoot.transform.SetParent(truck.transform,false);lanceRoot.transform.localPosition=new Vector3(1.6f,2.3f,3);lanceRoot.transform.localRotation=Quaternion.Euler(0,24,0);m.mounts[i]=lanceRoot.transform;lanceRoot.AddComponent<JoustLanceSupport>();var lance=lanceRoot.AddComponent<JoustLance>();lance.owner=truck.body;m.lances[i]=lance;
    var model=new GameObject("Actual intact oak lance and heraldic ribbons");model.transform.SetParent(lanceRoot.transform,false);lance.model=model;lance.splinters=new Rigidbody[8];lance.originals=new Transform[8];
    for(int piece=0;piece<8;piece++)
    {
     var shaft=GameObject.CreatePrimitive(PrimitiveType.Cylinder);shaft.name="Authored oak lance segment "+piece;shaft.transform.SetParent(model.transform,false);shaft.transform.localPosition=Vector3.forward*((piece+.5f)*1.5f);shaft.transform.localRotation=Quaternion.Euler(90,0,0);shaft.transform.localScale=new Vector3(.27f,.75f,.27f);shaft.GetComponent<Renderer>().sharedMaterial=piece%3==0?(i==0?red:blue):wood;UnityEngine.Object.DestroyImmediate(shaft.GetComponent<Collider>());lance.originals[piece]=shaft.transform;
     var fragment=UnityEngine.Object.Instantiate(shaft,m.debrisGroup);fragment.name="Pooled physical oak splinter "+i+"-"+piece;fragment.layer=2;var collider=fragment.AddComponent<BoxCollider>();collider.size=new Vector3(1,2,1);collider.sharedMaterial=contact;var body=fragment.AddComponent<Rigidbody>();body.mass=5;body.linearDamping=.2f;body.angularDamping=.5f;body.maxLinearVelocity=35;body.maxAngularVelocity=15;body.isKinematic=true;fragment.SetActive(false);lance.splinters[piece]=body;
    }
    var tip=GameObject.CreatePrimitive(PrimitiveType.Sphere);tip.name="Real solid rounded lance impact tip";tip.transform.SetParent(model.transform,false);tip.transform.localPosition=Vector3.forward*12;tip.transform.localScale=Vector3.one*.7f;tip.GetComponent<Renderer>().sharedMaterial=gold;lance.tip=tip.GetComponent<SphereCollider>();lance.tip.sharedMaterial=contact;
    var contacts=truck.gameObject.AddComponent<JoustContacts>();contacts.match=m;contacts.owner=i;
   }
   m.broadcast=SportsEventAuthoring.Camera("Tournament side-on physical-impact broadcast");m.broadcast.view.farClipPlane=1800;m.broadcast.view.orthographic=true;m.framing=m.broadcast.gameObject.AddComponent<SportsCameraFit>();m.framing.cameraTarget=m.broadcast.view;m.framing.arenaFit=true;m.framing.topInset=.1f;m.framing.bottomInset=.2f;m.cameras=new[]{m.broadcast.view};
   SportsEventAuthoring.UI(m);SportsRallyAuthoring.CompactUI(m);for(int i=0;i<2;i++)((RectTransform)m.menus.rows[i].transform).anchoredPosition=new Vector2(i==0?-350:350,100);
   m.drivingViews=SportsDrivingViewsAuthoring.Install(m,m.trucks,m.broadcast,Folder);m.drivingViews.independentViewModes=true;m.atmosphere=SportsChaosAuthoring.Weather(m,Folder);m.atmosphere.presets=SportsWeatherPresetsAuthoring.EnsureCatalog();m.drivingViews.rally.weather=m.atmosphere;SportsWeatherPickerAuthoring.Install(m,m.atmosphere.presets,new Vector2(350,-180));
   var hud=m.gameObject.AddComponent<SportsExpansionViewHud>();hud.match=m;hud.views=m.drivingViews;SportsUIAuthoring.Button(m.menus.pages[6].transform,"WATCH THE ACTUAL STRIKES","watchjoust",new Vector2(0,-245),new Vector2(520,48));Artwork(m);SportsBroadcastAuthoring.ApplyScene();SportsScenicGradeAuthoring.Install(m.gameObject,new[]{m.broadcast.view}.Concat(m.drivingViews.rally.views.Select(v=>v.view)).ToArray(),Folder,-.4f);SportsEventAuthoring.Save(Folder);Debug.Log("JOUST_AUTHORED two real NWH tractors, compound solid lance tips/shields and resettable Core Collapse lances");
  }
  public static void UpgradePresentation()
  {
   var scene=EditorSceneManager.OpenScene(Scene);var m=UnityEngine.Object.FindAnyObjectByType<JoustMatch>();
   SportsScenicGradeAuthoring.Install(m.gameObject,new[]{m.broadcast.view}.Concat(m.drivingViews.rally.views.Select(v=>v.view)).ToArray(),Folder,-.4f);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
  }
  static void Disc(Transform parent,string name,Vector3 p,float diameter,Material material){var disc=GameObject.CreatePrimitive(PrimitiveType.Cylinder);disc.name=name;disc.transform.SetParent(parent,false);disc.transform.localPosition=p;disc.transform.localRotation=Quaternion.Euler(90,0,0);disc.transform.localScale=new Vector3(diameter,.08f,diameter);disc.GetComponent<Renderer>().sharedMaterial=material;UnityEngine.Object.DestroyImmediate(disc.GetComponent<Collider>());}
  static void Flags(Material red,Material blue,Material steel)
  {
   var model=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TruckMinefield/Art/Imported/flag.fbx");if(model==null)throw new InvalidOperationException("Existing owned flag model required.");
   foreach(int side in new[]{-1,1})for(int z=-140;z<=140;z+=35)
   {
    var art=UnityEngine.Object.Instantiate(model);art.name="Installed heraldic festival flag";var renderers=art.GetComponentsInChildren<Renderer>(true);var bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);float scale=10/Mathf.Max(.1f,bounds.size.y);art.transform.localScale*=scale;art.transform.position=new Vector3(side*22,0,z)-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z)*scale;foreach(var renderer in renderers)renderer.sharedMaterial=renderer is SkinnedMeshRenderer?(side<0?red:blue):steel;
   }
  }
  static void Venue()
  {
   var destination=SceneManager.GetActiveScene();var source=EditorSceneManager.OpenScene(LWS.TruckHotPotato.Editor.HotPotatoAuthoring.Scene,OpenSceneMode.Additive);
   try{SceneManager.SetActiveScene(destination);foreach(var root in source.GetRootGameObjects())if(root.name.Contains("Union Field grandstands")||root.name.Contains("spectators")){var copy=SportsReplayVisuals.Copy(root.transform);copy.name="Reused tournament "+root.name;SceneManager.MoveGameObjectToScene(copy,destination);}}
   finally{EditorSceneManager.CloseScene(source,true);SceneManager.SetActiveScene(destination);}
  }
  static void Artwork(JoustMatch m)
  {
   var logo=SportsRetroMenuAuthoring.LoadLogo(Folder);
   foreach(int index in new[]{0,4}){var page=m.menus.pages[index];var art=SportsUIAuthoring.Rect("Original sport title artwork",page.transform,new Vector2(260,160),new Vector2(-560,365)).AddComponent<UnityEngine.UI.Image>();art.sprite=logo;art.preserveAspect=true;art.raycastTarget=false;var title=page.transform.Find("Title").GetComponent<TMP_Text>();title.rectTransform.anchoredPosition+=Vector2.right*140;title.rectTransform.sizeDelta=new Vector2(1000,title.rectTransform.sizeDelta.y);}
   var material=SportsEventAuthoring.Mat(Folder,"Original grungy heraldic joust signage",Color.white);material.shader=Shader.Find("Universal Render Pipeline/Unlit");material.mainTexture=logo.texture;material.SetFloat("_Surface",1);material.SetFloat("_SrcBlend",5);material.SetFloat("_DstBlend",10);material.SetFloat("_ZWrite",0);material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.renderQueue=3000;
   foreach(int side in new[]{-1,1}){var sign=GameObject.CreatePrimitive(PrimitiveType.Quad);sign.name="Original tournament venue artwork";UnityEngine.Object.DestroyImmediate(sign.GetComponent<Collider>());sign.transform.position=new Vector3(side*32,11,0);sign.transform.rotation=Quaternion.Euler(0,side<0?90:-90,0);sign.transform.localScale=new Vector3(30,17,1);sign.GetComponent<Renderer>().sharedMaterial=material;}
  }
 }
}
