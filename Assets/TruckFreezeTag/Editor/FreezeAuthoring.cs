using System;using System.IO;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;using TMPro;
using LWS.TruckSports;using LWS.TruckSports.Editor;using LWS.TruckFareFight.Editor;
namespace LWS.TruckFreezeTag.Editor
{
 public static class FreezeAuthoring
 {
  public const string Folder="TruckFreezeTag",Scene="Assets/TruckFreezeTag/Scenes/TruckFreezeTag.unity";
  public static void Create()
  {
   if(File.Exists(Scene))throw new InvalidOperationException("Preserve the saved winter stadium; use targeted upgrades.");var m=SportsEventAuthoring.Begin<FreezeMatch>(Folder);m.teamA=m.teamB=4;m.option=1;m.NormalizeSetup();m.feedback.clips=(AudioClip[])LWS.TruckHockey.Editor.HockeyAudioAuthoring.Create().Clone();m.feedback.clips[0]=m.feedback.clips[1]=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/NOT_Lonely/Weatherade SRS/Samples/SharedContent/SnowHit.wav");
   var ground=SportsEventAuthoring.Mat(Folder,"Existing textured winter snow field",new Color(.48f,.68f,.83f),.12f);ground.mainTexture=AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckKeepaway/Materials/Snowfield terrain.mat").mainTexture;ground.mainTextureScale=Vector2.one*28;var physics=SportsEventAuthoring.Contact(Folder,"Grippy packed winter snow",.8f,.02f);var floor=SportsEventAuthoring.Box("Continuous packed-snow freeze arena",new Vector3(0,-.5f,0),new Vector3(500,1,600),ground,physics);SportsDrivingSurfaceAuthoring.MarkHierarchy(floor.transform);
   var blue=SportsEventAuthoring.Mat(Folder,"Cobalt winter event paint",new Color(.03f,.3f,.68f),.35f);var pink=SportsEventAuthoring.Mat(Folder,"Magenta winter rescue paint",new Color(.87f,.035f,.45f),.35f);var steel=SportsEventAuthoring.Mat(Folder,"Cannon brushed steel",new Color(.22f,.28f,.34f),.55f);var cream=SportsEventAuthoring.Mat(Folder,"Frost rim white",new Color(.85f,.96f,1),.25f);
   foreach(int side in new[]{-1,1}){SportsEventAuthoring.Box("Continuous visible freeze stadium end barrier",new Vector3(0,2,side*82),new Vector3(176,4,1.2f),side<0?blue:pink,physics);SportsEventAuthoring.Box("Continuous visible freeze stadium side barrier",new Vector3(side*88,2,0),new Vector3(1.2f,4,165),side<0?blue:pink,physics);}
   var root=new GameObject("Installed winter cover, trees and spectator venue").transform;
   for(int i=0;i<6;i++){float x=i%2==0?-28:28,z=(i/2-1)*33;var cover=new GameObject("Visible solid snow-covered cargo cover");cover.transform.SetParent(root);cover.transform.position=new Vector3(x,0,z);Cover(cover,cream,physics);}

   for(int i=0;i<24;i++){var tree=new GameObject("Installed winter pine "+i);tree.transform.SetParent(root);float a=i*Mathf.PI*2/24;tree.transform.position=new Vector3(Mathf.Sin(a)*120,0,Mathf.Cos(a)*115);Fit(tree.transform,"Assets/TruckSnake/Prefabs/Original Vista pine.prefab",new Vector3(12,22,12));}
   var ice=SportsEventAuthoring.Mat(Folder,"Translucent cyan ice shell",new Color(.3f,.78f,1,.42f),.7f);ice.SetFloat("_Surface",1);ice.SetFloat("_SrcBlend",5);ice.SetFloat("_DstBlend",10);ice.SetFloat("_ZWrite",0);ice.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");ice.renderQueue=3000;
   var spray=SportsEventAuthoring.Mat(Folder,"Pooled ice spray beam",new Color(.5f,.9f,1,.8f));spray.shader=Shader.Find("Universal Render Pipeline/Particles/Unlit");spray.SetFloat("_Surface",1);spray.SetFloat("_SrcBlend",5);spray.SetFloat("_DstBlend",10);spray.SetFloat("_ZWrite",0);spray.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");spray.renderQueue=3000;
   m.trucks=new SportsEventTruck[8];m.fragments=new SportsFragmentRig[8];m.muzzles=new Transform[8];m.cannons=new GameObject[8];m.shells=new GameObject[8];m.sprays=new LineRenderer[8];m.shatter=new ParticleSystem[8];
   for(int i=0;i<8;i++)
   {
    var truck=FareFightVehicleAuthoring.Spawn(m,i,new Vector3((i%4-1.5f)*17,.65f,i<4?-42:42),Quaternion.Euler(0,i<4?0:180,0));m.trucks[i]=truck;m.fragments[i]=SportsFragmentAuthoring.InstallRigid(truck.body,truck.visuals.GetComponentsInChildren<MeshFilter>(true),Folder);var contacts=truck.gameObject.AddComponent<FreezeContacts>();contacts.match=m;contacts.truck=truck;
    var cannon=new GameObject("Visible role-specific industrial ice cannon");cannon.transform.SetParent(truck.transform,false);cannon.transform.localPosition=new Vector3(0,4.2f,0);m.cannons[i]=cannon;SportsEventAuthoring.Box("Cannon reinforced rotating pedestal",new Vector3(0,-.4f,0),new Vector3(1.4f,.8f,2.4f),steel,null,cannon.transform);
    for(int ring=0;ring<4;ring++){var barrel=GameObject.CreatePrimitive(PrimitiveType.Cylinder);barrel.name="Modeled ribbed ice cannon barrel";barrel.transform.SetParent(cannon.transform,false);barrel.transform.localPosition=new Vector3(0,0,ring*.7f+.7f);barrel.transform.localRotation=Quaternion.Euler(90,0,0);barrel.transform.localScale=new Vector3(ring==3?1.3f:1.05f,.32f,ring==3?1.3f:1.05f);barrel.GetComponent<Renderer>().sharedMaterial=ring%2==0?blue:steel;UnityEngine.Object.DestroyImmediate(barrel.GetComponent<Collider>());}
    var muzzle=new GameObject("Actual unobstructed ice spray muzzle").transform;muzzle.SetParent(truck.transform,false);muzzle.localPosition=new Vector3(0,4.2f,5.3f);m.muzzles[i]=muzzle;
    var shell=SportsEventAuthoring.Box("Persistent physical ice shell",new Vector3(0,2.5f,0),new Vector3(4.8f,5.3f,12),ice,physics,truck.transform);shell.SetActive(false);m.shells[i]=shell;
    var line=new GameObject("Pooled actual wall-blocked cannon spray").AddComponent<LineRenderer>();line.positionCount=2;line.useWorldSpace=true;line.widthMultiplier=.45f;line.sharedMaterial=spray;line.enabled=false;m.sprays[i]=line;
    var fx=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<ParticleSystem>("Assets/NOT_Lonely/Weatherade SRS/Samples/SharedContent/Particles/SnowHitFX.prefab"),truck.transform);fx.name="Pooled existing Weatherade ice shatter";fx.transform.localPosition=Vector3.up*2;ConfigurePool(fx,spray);fx.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);fx.GetComponent<ParticleSystemRenderer>().sharedMaterial=spray;var main=fx.main;main.playOnAwake=false;main.loop=false;main.duration=.5f;main.startSpeed=new ParticleSystem.MinMaxCurve(5,13);main.startLifetime=new ParticleSystem.MinMaxCurve(.5f,1.5f);main.maxParticles=150;main.simulationSpace=ParticleSystemSimulationSpace.World;var emission=fx.emission;emission.rateOverTime=0;emission.SetBursts(new[]{new ParticleSystem.Burst(0,80)});fx.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);m.shatter[i]=fx;
   }
   Bleachers(root,steel);var panel=SportsEventAuthoring.Box("Winter festival actual match scoreboard",new Vector3(0,15,82),new Vector3(80,12,1),steel);var label=new GameObject("Readably lit winter event board").AddComponent<TextMeshPro>();label.font=Resources.Load<SportsBroadcastTheme>("SportsBroadcastTheme").headingFont;label.transform.position=new Vector3(0,15,81.4f);label.rectTransform.sizeDelta=new Vector2(76,10);label.fontSize=4.5f;label.alignment=TextAlignmentOptions.Center;label.color=cream.color;var board=panel.AddComponent<FreezeBoard>();board.match=m;board.text=label;
   m.broadcast=SportsEventAuthoring.Camera("Winter stadium freeze and rescue broadcast");m.broadcast.view.farClipPlane=2400;m.broadcast.view.orthographic=true;m.framing=m.broadcast.gameObject.AddComponent<SportsCameraFit>();m.framing.cameraTarget=m.broadcast.view;m.framing.arenaFit=true;m.framing.topInset=.1f;m.framing.bottomInset=.2f;m.cameras=new[]{m.broadcast.view};SportsEventAuthoring.UI(m);SportsRallyAuthoring.CompactUI(m);m.drivingViews=SportsDrivingViewsAuthoring.Install(m,m.trucks,m.broadcast,Folder);m.drivingViews.independentViewModes=true;m.atmosphere=SportsChaosAuthoring.Weather(m,Folder);m.atmosphere.presets=SportsWeatherPresetsAuthoring.EnsureCatalog();m.drivingViews.rally.weather=m.atmosphere;SportsWeatherPickerAuthoring.Install(m,m.atmosphere.presets,new Vector2(350,-180));var hud=m.gameObject.AddComponent<SportsExpansionViewHud>();hud.match=m;hud.views=m.drivingViews;SportsUIAuthoring.Button(m.menus.pages[6].transform,"WATCH THE ICE BREAK","watchfreeze",new Vector2(0,-245),new Vector2(520,48));Artwork(m);SportsBroadcastAuthoring.ApplyScene();SportsScenicGradeAuthoring.Install(m.gameObject,new[]{m.broadcast.view}.Concat(m.drivingViews.rally.views.Select(v=>v.view)).ToArray(),Folder,-.3f);SportsEventAuthoring.Save(Folder);
  }
  static Bounds VisibleBounds(Transform root)
  {
   bool first=true;Bounds result=default;foreach(var renderer in root.GetComponentsInChildren<Renderer>())if(renderer.enabled){var box=renderer.localBounds;for(int i=0;i<8;i++){var p=root.InverseTransformPoint(renderer.transform.TransformPoint(box.center+Vector3.Scale(box.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1))));if(first){result=new Bounds(p,Vector3.zero);first=false;}else result.Encapsulate(p);}}return result;
  }
  static void Cover(GameObject cover,Material snow,PhysicsMaterial physics)
  {
   var visual=Fit(cover.transform,"Assets/ShipContainerPack/URP/Prefabs/ShipContainerRed_URP.prefab",new Vector3(13,3,3),true);var baseBounds=VisibleBounds(cover.transform);var top=UnityEngine.Object.Instantiate(visual,cover.transform,false);top.name="Second actual stacked cover container";top.transform.localPosition+=Vector3.up*baseBounds.size.y;
   var bounds=VisibleBounds(cover.transform);var collider=cover.GetComponent<BoxCollider>();if(collider==null)collider=cover.AddComponent<BoxCollider>();collider.center=bounds.center;collider.size=bounds.size;collider.sharedMaterial=physics;
   SportsEventAuthoring.Box("Accumulated snow cap on real cargo cover",new Vector3(bounds.center.x,bounds.max.y+.08f,bounds.center.z),new Vector3(bounds.size.x+.2f,.16f,bounds.size.z+.2f),snow,null,cover.transform);Debug.Log("FREEZE_COVER_VISIBLE_AND_SOLID "+cover.name+" size="+bounds.size+" collider="+collider.size);
  }
  public static void UpgradeCoverAndSnow()
  {
   EditorSceneManager.OpenScene(Scene);var scene=SceneManager.GetActiveScene();var m=UnityEngine.Object.FindAnyObjectByType<FreezeMatch>();var snow=AssetDatabase.LoadAssetAtPath<Material>("Assets/"+Folder+"/Materials/Frost rim white.mat");var physics=AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/"+Folder+"/Materials/Grippy packed winter snow.asset");var original=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ShipContainerPack/URP/Prefabs/ShipContainerRed_URP.prefab");
   foreach(var shape in UnityEngine.Object.FindObjectsByType<BoxCollider>(FindObjectsInactive.Include))if(shape.gameObject.scene==scene&&shape.name=="Visible solid snow-covered cargo cover"){
    var cover=shape.gameObject;foreach(Transform child in cover.transform.Cast<Transform>().ToArray())if(child.name==original.name||child.name=="Accumulated snow cap on real cargo cover"||child.name=="Second actual stacked cover container")UnityEngine.Object.DestroyImmediate(child.gameObject);Cover(cover,snow,physics);
   }
   var ground=AssetDatabase.LoadAssetAtPath<Material>("Assets/"+Folder+"/Materials/Existing textured winter snow field.mat");var source=AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckKeepaway/Materials/Snowfield terrain.mat");ground.color=new Color(.48f,.68f,.83f);ground.mainTextureScale=Vector2.one*65;if(source.HasProperty("_BumpMap")){ground.SetTexture("_BumpMap",source.GetTexture("_BumpMap"));ground.EnableKeyword("_NORMALMAP");ground.SetFloat("_BumpScale",1.2f);}EditorUtility.SetDirty(ground);
   var board=GameObject.Find("Readably lit winter event board").GetComponent<TMP_Text>();board.fontSize=24;board.enableAutoSizing=true;board.fontSizeMax=26;board.fontSizeMin=14;EditorUtility.SetDirty(board);
   if(GameObject.Find("Original freeze festival center-ice emblem")==null){var logo=GameObject.CreatePrimitive(PrimitiveType.Quad);logo.name="Original freeze festival center-ice emblem";UnityEngine.Object.DestroyImmediate(logo.GetComponent<Collider>());logo.transform.position=new Vector3(0,.06f,0);logo.transform.rotation=Quaternion.Euler(90,0,0);logo.transform.localScale=new Vector3(30,16,1);logo.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/"+Folder+"/Materials/Original grungy ice festival signage.mat");}
   EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
  }
  static void ConfigurePool(ParticleSystem effect,Material material)
  {
   foreach(var system in effect.GetComponentsInChildren<ParticleSystem>(true)){var main=system.main;main.stopAction=ParticleSystemStopAction.None;main.playOnAwake=false;main.loop=false;main.maxParticles=150;var emission=system.emission;emission.rateOverTime=0;emission.SetBursts(new[]{new ParticleSystem.Burst(0,80)});var renderer=system.GetComponent<ParticleSystemRenderer>();if(renderer!=null)renderer.sharedMaterial=material;system.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);EditorUtility.SetDirty(system);}
  }
  public static void UpgradeEffectPool()
  {
   EditorSceneManager.OpenScene(Scene);var m=UnityEngine.Object.FindAnyObjectByType<FreezeMatch>();var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/"+Folder+"/Materials/Pooled ice spray beam.mat");foreach(var effect in m.shatter)ConfigurePool(effect,material);EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
  }
  static GameObject Fit(Transform parent,string path,Vector3 requested,bool longAxisX=false)
  {
   var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(source==null)throw new InvalidOperationException("Installed visual model required: "+path);var copy=SportsReplayVisuals.Copy(source.transform);copy.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
   foreach(var renderer in copy.GetComponentsInChildren<Renderer>())if(renderer.name.IndexOf("shadow",StringComparison.OrdinalIgnoreCase)>=0)renderer.enabled=false;
   var all=copy.GetComponentsInChildren<Renderer>().Where(x=>x.enabled).ToArray();if(all.Length==0)throw new InvalidOperationException("No authored visual geometry in "+path);var bounds=all[0].bounds;foreach(var renderer in all)bounds.Encapsulate(renderer.bounds);if(longAxisX&&bounds.size.z>bounds.size.x){copy.transform.Rotate(0,90,0);bounds=all[0].bounds;foreach(var renderer in all)bounds.Encapsulate(renderer.bounds);}float scale=Mathf.Min(requested.x/bounds.size.x,requested.y/bounds.size.y,requested.z/bounds.size.z);copy.transform.localScale*=scale;copy.transform.position=-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z)*scale;copy.transform.SetParent(parent,false);
   foreach(var renderer in all){var materials=renderer.sharedMaterials;for(int i=0;i<materials.Length;i++)if(materials[i]!=null&&!materials[i].shader.name.Contains("Universal")&&!materials[i].shader.name.Contains("URP")){string ownedPath="Assets/"+Folder+"/Materials/Imported "+materials[i].name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(ownedPath);if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.color=materials[i].HasProperty("_Color")?materials[i].color:Color.white;material.mainTexture=materials[i].mainTexture;material.enableInstancing=true;AssetDatabase.CreateAsset(material,ownedPath);}materials[i]=material;}renderer.sharedMaterials=materials;}
   return copy;
  }
  static void Bleachers(Transform root,Material steel)
  {
   var destination=SceneManager.GetActiveScene();var source=EditorSceneManager.OpenScene(LWS.TruckHotPotato.Editor.HotPotatoAuthoring.Scene,OpenSceneMode.Additive);
   try
   {
    SceneManager.SetActiveScene(destination);var crowd=source.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<LWS.TruckHotPotato.HotPotatoMatch>(true)).First().crowd;
    for(int side=-1;side<=1;side+=2)for(int row=0;row<4;row++)
    {
     SportsEventAuthoring.Box("Protected winter tiered steel bleacher",new Vector3(side*(95+row*2),1+row,0),new Vector3(2.5f,1.5f,90),steel,null,root);
     for(int i=0;i<36;i++){var batch=crowd.batches[(i+row)%crowd.batches.Length];var fan=new GameObject("Reused posed winter spectator");fan.transform.SetParent(root);fan.transform.SetPositionAndRotation(new Vector3(side*(95+row*2),1.75f+row,-43+i*2.4f),Quaternion.Euler(0,side<0?90:-90,0));fan.AddComponent<MeshFilter>().sharedMesh=batch.idle;fan.AddComponent<MeshRenderer>().sharedMaterials=batch.materials;}
    }
   }finally{EditorSceneManager.CloseScene(source,true);SceneManager.SetActiveScene(destination);}
  }
  static void Artwork(FreezeMatch m)
  {
   var logo=SportsRetroMenuAuthoring.LoadLogo(Folder);foreach(int index in new[]{0,4}){var page=m.menus.pages[index];var art=SportsUIAuthoring.Rect("Original sport title artwork",page.transform,new Vector2(260,160),new Vector2(-560,365)).AddComponent<UnityEngine.UI.Image>();art.sprite=logo;art.preserveAspect=true;art.raycastTarget=false;var title=page.transform.Find("Title").GetComponent<TMP_Text>();title.rectTransform.anchoredPosition+=Vector2.right*140;title.rectTransform.sizeDelta=new Vector2(1000,title.rectTransform.sizeDelta.y);}
   var material=SportsEventAuthoring.Mat(Folder,"Original grungy ice festival signage",Color.white);material.shader=Shader.Find("Universal Render Pipeline/Unlit");material.mainTexture=logo.texture;material.SetFloat("_Surface",1);material.SetFloat("_SrcBlend",5);material.SetFloat("_DstBlend",10);material.SetFloat("_ZWrite",0);material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.renderQueue=3000;
   foreach(int side in new[]{-1,1}){var sign=GameObject.CreatePrimitive(PrimitiveType.Quad);sign.name="Original winter freeze arena venue artwork";UnityEngine.Object.DestroyImmediate(sign.GetComponent<Collider>());sign.transform.position=new Vector3(side*52,12,81);sign.transform.localScale=new Vector3(35,19,1);sign.GetComponent<Renderer>().sharedMaterial=material;}
  }
 }
}
