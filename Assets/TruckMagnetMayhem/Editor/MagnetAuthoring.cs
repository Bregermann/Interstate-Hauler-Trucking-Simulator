using System;using System.IO;using System.Linq;
using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;using TMPro;
using LWS.TruckSports;using LWS.TruckSports.Editor;using LWS.TruckFareFight.Editor;
namespace LWS.TruckMagnetMayhem.Editor
{
 public static class MagnetAuthoring
 {
  public const string Folder="TruckMagnetMayhem",Scene="Assets/TruckMagnetMayhem/Scenes/TruckMagnetMayhem.unity";
  public static void Create()
  {
   if(File.Exists(Scene))throw new InvalidOperationException("Preserve the authored scrapyard; use explicit targeted upgrades.");
   var m=SportsEventAuthoring.Begin<MagnetMatch>(Folder);m.setup.count=8;m.option=1;m.NormalizeSetup();m.feedback.clips=LWS.TruckHockey.Editor.HockeyAudioAuthoring.Create();
   var ground=SportsEventAuthoring.Mat(Folder,"Weathered quarry steel-yard floor",new Color(.28f,.29f,.27f),.12f);ground.mainTexture=AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckTrailerPlug/Materials/Rocky quarry floor.mat").mainTexture;ground.mainTextureScale=Vector2.one*28;
   var contact=SportsEventAuthoring.Contact(Folder,"NWH scrapyard driving contact",.8f,.02f);var floor=SportsEventAuthoring.Box("Continuous detailed industrial scrapyard floor",new Vector3(0,-.5f,0),new Vector3(230,1,230),ground,contact);SportsDrivingSurfaceAuthoring.MarkHierarchy(floor.transform);
   var steel=SportsEventAuthoring.Mat(Folder,"Brushed dark industrial steel",new Color(.19f,.22f,.25f),.55f);var yellow=SportsEventAuthoring.Mat(Folder,"Worn hazard yellow",new Color(.88f,.58f,.045f),.2f);var white=SportsEventAuthoring.Mat(Folder,"Scuffed collection-bay paint",Color.white,.1f);
   for(int side=-1;side<=1;side+=2){SportsEventAuthoring.Box("Continuous solid scrapyard perimeter",new Vector3(side*98,4,0),new Vector3(1,8,197),steel,contact);SportsEventAuthoring.Box("Continuous solid scrapyard perimeter",new Vector3(0,4,side*98),new Vector3(197,8,1),steel,contact);}
   Backdrop(steel,yellow);m.fields=m.gameObject.AddComponent<MagnetFields>();m.scrapGroup=new GameObject("Finite replay-recordable physical scrap population").transform;
   m.scrap=new MagnetMetal[40];var sliding=SportsEventAuthoring.Contact(Folder,"Bounded sliding magnetic scrap",.05f,.04f);sliding.frictionCombine=PhysicsMaterialCombine.Minimum;
   for(int i=0;i<m.scrap.Length;i++)
   {
    float angle=(i%20+(i>=20?.5f:0))*Mathf.PI*2/20,radius=i<20?38:52;var root=new GameObject("Physical finite magnetic scrap "+i);root.transform.SetParent(m.scrapGroup);root.transform.SetPositionAndRotation(new Vector3(Mathf.Sin(angle)*radius,.05f,Mathf.Cos(angle)*radius),Quaternion.Euler(0,i*137.5f,0));
    int kind=i%5;string source=kind<2?"Assets/RoadPropsPack/URP/Prefabs/Canister_URP.prefab":kind<4?"Assets/ShipContainerPack/URP/Prefabs/ShipContainerRed_URP.prefab":"Assets/UTS_FullPack/Models/Cars/Models/Car_1.FBX";
    Vector3 size=kind<2?new Vector3(1.1f,1.5f,1.1f):kind<4?new Vector3(2.6f,2.6f,5.6f):new Vector3(2.5f,1.7f,5);
    var visual=Fit(root.transform,source,size);var renderers=visual.GetComponentsInChildren<Renderer>().Where(x=>x.enabled).ToArray();var bounds=LocalBounds(root.transform,renderers);
    var shape=root.AddComponent<BoxCollider>();shape.center=bounds.center;shape.size=bounds.size;shape.sharedMaterial=sliding;var body=root.AddComponent<Rigidbody>();body.mass=kind<2?100:kind<4?800:1000;body.centerOfMass=shape.center;body.isKinematic=true;body.interpolation=RigidbodyInterpolation.Interpolate;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;body.maxLinearVelocity=32;body.maxAngularVelocity=12;body.linearDamping=.08f;body.angularDamping=.25f;
    var actor=root.AddComponent<MagnetMetal>();m.scrap[i]=actor;actor.index=i;actor.points=kind<2?1:kind<4?3:5;actor.polarity=kind<2?0:i%2==0?1:-1;actor.body=body;actor.shape=shape;actor.tint=renderers;
    var badge=SportsEventAuthoring.Label("Engraved scrap value",actor.points+"P"+(actor.polarity>0?" +":actor.polarity<0?" -":""),root.transform.position+Vector3.up*(bounds.size.y+.4f),.3f,actor.polarity>0?Color.red:actor.polarity<0?Color.cyan:Color.white);badge.transform.SetParent(root.transform,true);badge.gameObject.AddComponent<SportsPerCameraBillboard>();
   }
   m.trucks=new SportsEventTruck[8];m.fragments=new SportsFragmentRig[8];m.truckMetal=new MagnetMetal[8];m.bays=new MagnetBay[8];
   var fieldMaterial=SportsEventAuthoring.Mat(Folder,"Pooled industrial magnetic field rings",Color.white);fieldMaterial.shader=Shader.Find("Universal Render Pipeline/Particles/Unlit");fieldMaterial.SetFloat("_Surface",1);fieldMaterial.SetFloat("_Blend",0);fieldMaterial.SetFloat("_ZWrite",0);fieldMaterial.SetFloat("_SrcBlend",5);fieldMaterial.SetFloat("_DstBlend",10);fieldMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");fieldMaterial.renderQueue=3000;
   for(int i=0;i<8;i++)
   {
    float angle=i*2*Mathf.PI/8;var radial=new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle));m.bays[i]=Bay(i,radial*65,Quaternion.LookRotation(radial),white,steel,contact);
    var truck=FareFightVehicleAuthoring.Spawn(m,i,radial*77+Vector3.up*.65f,Quaternion.LookRotation(-radial));m.trucks[i]=truck;truck.body.maxLinearVelocity=35;m.fragments[i]=SportsFragmentAuthoring.InstallRigid(truck.body,truck.visuals.GetComponentsInChildren<MeshFilter>(true),Folder);
    var actor=truck.gameObject.AddComponent<MagnetMetal>();m.truckMetal[i]=actor;actor.body=truck.body;actor.shape=truck.shape;actor.truckSlot=i;actor.tint=Array.Empty<Renderer>();
    var mount=new GameObject("Obvious roof-mounted industrial electromagnet").transform;mount.SetParent(truck.transform,false);mount.localPosition=new Vector3(0,4.5f,2.5f);actor.fieldPoint=mount;
    var crown=SportsEventAuthoring.Box("Giant U-magnet forged yoke",new Vector3(0,1.3f,0),new Vector3(3.6f,.65f,.85f),steel,null,mount);var poles=new Renderer[2];
    for(int side=0;side<2;side++){var pole=SportsEventAuthoring.Box("Color-changing positive/negative magnetic pole",new Vector3(side==0?-1.45f:1.45f,.2f,0),new Vector3(.7f,2.4f,.85f),white,null,mount);poles[side]=pole.GetComponent<Renderer>();SportsEventAuthoring.Box("Bolted magnet safety bracket",new Vector3(side==0?-1.45f:1.45f,-1.5f,-.3f),new Vector3(.25f,1.5f,.25f),yellow,null,mount);}
    var display=mount.gameObject.AddComponent<MagnetDisplay>();display.metal=actor;display.poles=poles;display.rings=new LineRenderer[3];
    for(int ring=0;ring<3;ring++){var line=new GameObject("Pooled visible polarity field "+ring).AddComponent<LineRenderer>();line.transform.SetParent(mount,false);line.transform.localRotation=Quaternion.Euler(0,ring*60,0);line.useWorldSpace=false;line.positionCount=33;line.widthMultiplier=.08f;line.sharedMaterial=fieldMaterial;line.enabled=false;display.rings[ring]=line;}
   }
   m.sortingArms=new MagnetSortingArm[2];
   for(int side=-1;side<=1;side+=2)
   {
    var arm=SportsEventAuthoring.Box("Physical rotating scrap sorting arm",new Vector3(side*24,2,0),new Vector3(2.2f,.8f,14),yellow,contact);var body=arm.AddComponent<Rigidbody>();body.isKinematic=true;body.interpolation=RigidbodyInterpolation.Interpolate;var motor=arm.AddComponent<MagnetSortingArm>();motor.body=body;motor.match=m;motor.degreesPerSecond=side*12;m.sortingArms[(side+1)/2]=motor;
    SportsEventAuthoring.Box("Sorting arm fixed machinery base",new Vector3(side*24,.7f,0),new Vector3(3,1.4f,3),steel,contact);
   }
   m.broadcast=SportsEventAuthoring.Camera("Scrapyard magnetic collection broadcast");m.broadcast.view.farClipPlane=2500;m.broadcast.view.orthographic=true;m.framing=m.broadcast.gameObject.AddComponent<SportsCameraFit>();m.framing.cameraTarget=m.broadcast.view;m.framing.arenaFit=true;m.framing.topInset=.1f;m.framing.bottomInset=.2f;m.cameras=new[]{m.broadcast.view};
   SportsEventAuthoring.UI(m);SportsRallyAuthoring.CompactUI(m);m.drivingViews=SportsDrivingViewsAuthoring.Install(m,m.trucks,m.broadcast,Folder);m.drivingViews.independentViewModes=true;
   m.atmosphere=SportsChaosAuthoring.Weather(m,Folder);m.atmosphere.presets=SportsWeatherPresetsAuthoring.EnsureCatalog();m.drivingViews.rally.weather=m.atmosphere;SportsWeatherPickerAuthoring.Install(m,m.atmosphere.presets,new Vector2(350,-180));var hud=m.gameObject.AddComponent<SportsExpansionViewHud>();hud.match=m;hud.views=m.drivingViews;
   SportsUIAuthoring.Button(m.menus.pages[6].transform,"WATCH THE SCRAP HEIST","watchmagnet",new Vector2(0,-245),new Vector2(520,48));Artwork(m);SportsBroadcastAuthoring.ApplyScene();SportsScenicGradeAuthoring.Install(m.gameObject,new[]{m.broadcast.view}.Concat(m.drivingViews.rally.views.Select(v=>v.view)).ToArray(),Folder,-.4f);SportsEventAuthoring.Save(Folder);Debug.Log("MAGNET_AUTHORED40physical installed scrap objects, eight NWH electromagnet tractors and actual settled-scoring bays");
  }
  static Bounds LocalBounds(Transform root,Renderer[] renderers)
  {
   Bounds result=default;bool first=true;foreach(var renderer in renderers){var local=renderer.localBounds;for(int i=0;i<8;i++){var corner=local.center+Vector3.Scale(local.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));var p=root.InverseTransformPoint(renderer.transform.TransformPoint(corner));if(first){result=new Bounds(p,Vector3.zero);first=false;}else result.Encapsulate(p);}}return result;
  }
  static GameObject Fit(Transform parent,string path,Vector3 requested)
  {
   var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(source==null)throw new InvalidOperationException("Installed visual model required: "+path);var copy=SportsReplayVisuals.Copy(source.transform);copy.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
   foreach(var renderer in copy.GetComponentsInChildren<Renderer>())if(renderer.name.IndexOf("shadow",StringComparison.OrdinalIgnoreCase)>=0)renderer.enabled=false;
   var all=copy.GetComponentsInChildren<Renderer>().Where(x=>x.enabled).ToArray();if(all.Length==0)throw new InvalidOperationException("No authored visual geometry in "+path);var bounds=all[0].bounds;foreach(var renderer in all)bounds.Encapsulate(renderer.bounds);float scale=Mathf.Min(requested.x/bounds.size.x,requested.y/bounds.size.y,requested.z/bounds.size.z);copy.transform.localScale*=scale;copy.transform.position=-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z)*scale;copy.transform.SetParent(parent,false);
   foreach(var renderer in all){var materials=renderer.sharedMaterials;for(int i=0;i<materials.Length;i++)if(materials[i]!=null&&!materials[i].shader.name.Contains("Universal")&&!materials[i].shader.name.Contains("URP")){string ownedPath="Assets/"+Folder+"/Materials/Imported "+materials[i].name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(ownedPath);if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.color=materials[i].HasProperty("_Color")?materials[i].color:Color.white;material.mainTexture=materials[i].mainTexture;material.enableInstancing=true;AssetDatabase.CreateAsset(material,ownedPath);}materials[i]=material;}renderer.sharedMaterials=materials;}
   return copy;
  }
  static MagnetBay Bay(int owner,Vector3 p,Quaternion rotation,Material paint,Material steel,PhysicsMaterial contact)
  {
   var root=new GameObject("Participant-colored real settled collection bay "+owner);root.transform.SetPositionAndRotation(p,rotation);var bay=root.AddComponent<MagnetBay>();bay.owner=owner;bay.zone=root.AddComponent<BoxCollider>();bay.zone.center=new Vector3(0,3,0);bay.zone.size=new Vector3(22,6,26);bay.zone.isTrigger=true;var parts=new System.Collections.Generic.List<Renderer>();
   foreach(int side in new[]{-1,1}){parts.Add(SportsEventAuthoring.Box("Collection-bay painted side",new Vector3(side*11,.025f,0),new Vector3(.3f,.035f,26),paint,null,root.transform).GetComponent<Renderer>());parts.Add(SportsEventAuthoring.Box("Collection-bay painted threshold",new Vector3(0,.025f,side*13),new Vector3(22,.035f,.3f),paint,null,root.transform).GetComponent<Renderer>());}
   for(int stripe=0;stripe<8;stripe++)parts.Add(SportsEventAuthoring.Box("Painted secured scrap parking stripes",new Vector3(0,.022f,stripe*3-10.5f),new Vector3(21,.03f,.12f),paint,null,root.transform).GetComponent<Renderer>());bay.paint=parts.ToArray();return bay;
  }
  static void Placed(Transform parent,string path,Vector3 position,Vector3 size){var root=new GameObject("Installed scrapyard visual model").transform;root.SetParent(parent,false);root.localPosition=position;Fit(root,path,size);}
  static void Backdrop(Material steel,Material yellow)
  {
   var root=new GameObject("Installed finished industrial scrapyard environment").transform;
   var terrain=Terrain.CreateTerrainGameObject(AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/TruckTrailerPlug/Models/Industrial Quarry Terrain.asset"));terrain.name="Reused substantial quarry landscape";terrain.transform.position=new Vector3(-300,-terrain.GetComponent<Terrain>().terrainData.bounds.max.y-8,-300);
   var craneMesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/TruckBungeeDemolition/Models/Lattice Crane.asset");
   for(int i=0;i<4;i++){float side=i%2==0?-1:1;var crane=new GameObject("Installed detailed magnetic crane "+i);crane.transform.SetParent(root);crane.AddComponent<MeshFilter>().sharedMesh=craneMesh;crane.AddComponent<MeshRenderer>().sharedMaterial=yellow;float scale=80/craneMesh.bounds.size.y;crane.transform.localScale=Vector3.one*scale;crane.transform.position=new Vector3(side*112,-craneMesh.bounds.min.y*scale,i<2?-72:72);crane.transform.rotation=Quaternion.Euler(0,i*90,0);}
   for(int side=-1;side<=1;side+=2)for(int row=-1;row<=1;row++){var p=new Vector3(side*119,0,row*55);Placed(root,"Assets/JC_LP_MegaCity/Prefabs/Buildings/SM_Buildings_Warehouse_01.prefab",p,new Vector3(20,18,28));Placed(root,"Assets/ShipContainerPack/URP/Prefabs/ShipContainerRed_URP.prefab",p+new Vector3(-side*12,0,12),new Vector3(12,3,3));}
   for(int side=-1;side<=1;side+=2)for(int roller=0;roller<16;roller++){var drum=GameObject.CreatePrimitive(PrimitiveType.Cylinder);drum.name="Modeled sorting conveyor steel roller";drum.transform.SetParent(root);drum.transform.position=new Vector3(side*90,1,roller*1.2f-9);drum.transform.rotation=Quaternion.Euler(0,0,90);drum.transform.localScale=new Vector3(.7f,2,.7f);drum.GetComponent<Renderer>().sharedMaterial=steel;UnityEngine.Object.DestroyImmediate(drum.GetComponent<Collider>());}
   Bleachers(root,steel);
  }
  static void Bleachers(Transform root,Material steel)
  {
   var destination=SceneManager.GetActiveScene();var source=EditorSceneManager.OpenScene(LWS.TruckHotPotato.Editor.HotPotatoAuthoring.Scene,OpenSceneMode.Additive);
   try
   {
    SceneManager.SetActiveScene(destination);var crowd=source.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<LWS.TruckHotPotato.HotPotatoMatch>(true)).First().crowd;
    for(int side=-1;side<=1;side+=2)for(int row=0;row<4;row++)
    {
     SportsEventAuthoring.Box("Protected scrapyard tiered steel bleacher",new Vector3(side*(104+row*2),1+row,0),new Vector3(2.5f,1.5f,90),steel,null,root);
     for(int i=0;i<36;i++){var batch=crowd.batches[(i+row)%crowd.batches.Length];var fan=new GameObject("Reused posed scrapyard spectator");fan.transform.SetParent(root);fan.transform.SetPositionAndRotation(new Vector3(side*(104+row*2),1.75f+row,-43+i*2.4f),Quaternion.Euler(0,side<0?90:-90,0));fan.AddComponent<MeshFilter>().sharedMesh=batch.idle;fan.AddComponent<MeshRenderer>().sharedMaterials=batch.materials;}
    }
   }finally{EditorSceneManager.CloseScene(source,true);SceneManager.SetActiveScene(destination);}
  }
  public static void UpgradeVenue()
  {
   EditorSceneManager.OpenScene(Scene);var terrain=GameObject.Find("Reused substantial quarry landscape").GetComponent<Terrain>();terrain.transform.position=new Vector3(-300,-terrain.terrainData.bounds.max.y-8,-300);
   var scene=SceneManager.GetActiveScene();foreach(var go in scene.GetRootGameObjects())if(go.name.StartsWith("Reused scrapyard spectator stand"))UnityEngine.Object.DestroyImmediate(go);
   var root=GameObject.Find("Installed finished industrial scrapyard environment").transform;
   if(root.Find("Protected scrapyard tiered steel bleacher")==null)Bleachers(root,AssetDatabase.LoadAssetAtPath<Material>("Assets/"+Folder+"/Materials/Brushed dark industrial steel.mat"));
   EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
  }

  static void Artwork(MagnetMatch m)
  {
   var logo=SportsRetroMenuAuthoring.LoadLogo(Folder);foreach(int index in new[]{0,4}){var page=m.menus.pages[index];var art=SportsUIAuthoring.Rect("Original sport title artwork",page.transform,new Vector2(260,160),new Vector2(-560,365)).AddComponent<UnityEngine.UI.Image>();art.sprite=logo;art.preserveAspect=true;art.raycastTarget=false;var title=page.transform.Find("Title").GetComponent<TMP_Text>();title.rectTransform.anchoredPosition+=Vector2.right*140;title.rectTransform.sizeDelta=new Vector2(1000,title.rectTransform.sizeDelta.y);}
   var material=SportsEventAuthoring.Mat(Folder,"Original grungy scrapyard signage",Color.white);material.shader=Shader.Find("Universal Render Pipeline/Unlit");material.mainTexture=logo.texture;material.SetFloat("_Surface",1);material.SetFloat("_SrcBlend",5);material.SetFloat("_DstBlend",10);material.SetFloat("_ZWrite",0);material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.renderQueue=3000;
   foreach(int side in new[]{-1,1}){var sign=GameObject.CreatePrimitive(PrimitiveType.Quad);sign.name="Original magnetic scrapyard venue artwork";UnityEngine.Object.DestroyImmediate(sign.GetComponent<Collider>());sign.transform.position=new Vector3(side*63,16,94);sign.transform.localScale=new Vector3(35,19,1);sign.GetComponent<Renderer>().sharedMaterial=material;}
  }
 }
}
