using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Michsky.UI.Heat;
namespace LWS.TruckBilliards.Editor {
public static class BilliardsBuilder {
 public const string Root="Assets/TruckBilliards",ScenePath=Root+"/Scenes/TruckBilliards.unity";
 static Material Mat(string name,Color color){
  string path=Root+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
  if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
  m.color=color;EditorUtility.SetDirty(m);return m;
 }
 static GameObject Shape(string name,PrimitiveType type,Vector3 p,Vector3 s,Material m,bool collider=true){
  var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.position=p;g.transform.localScale=s;g.GetComponent<Renderer>().sharedMaterial=m;
  if(!collider)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;
 }
 [MenuItem("Truck Billiards/Create or Rebuild Demo")]
 public static void Create(){
  Directory.CreateDirectory(Root+"/Materials");Directory.CreateDirectory(Root+"/Prefabs");Directory.CreateDirectory(Root+"/Scenes");
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  var felt=Mat("Felt",new Color(.02f,.27f,.21f));var wood=Mat("Walnut",new Color(.17f,.07f,.03f));var rail=Mat("Cushions",new Color(.01f,.13f,.1f));var black=Mat("Pocket",new Color(.008f,.01f,.015f));
  var root=new GameObject("Truck Billiards — Match Authority");var match=root.AddComponent<BilliardsMatch>();match.audioSource=root.AddComponent<AudioSource>();
  match.impact=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Heat - Complete Modern UI/Audio/Click.wav");match.pocketSound=match.impact;
  Shape("Table body",PrimitiveType.Cube,new Vector3(0,-.55f,0),new Vector3(13.6f,1,25.6f),wood,false);
  Shape("Felt playing surface",PrimitiveType.Cube,new Vector3(0,-.1f,0),new Vector3(12,.2f,24),felt);
  var physics=new PhysicsMaterial("Billiards cushions"){bounciness=.82f,dynamicFriction=0,staticFriction=0,bounceCombine=PhysicsMaterialCombine.Maximum};
  physics=PersistPhysics(physics,Root+"/Materials/Cushion.physicsMaterial");
  match.pockets=new[]{new Vector3(-6,0,-12),new Vector3(6,0,-12),new Vector3(-6,0,12),new Vector3(6,0,12),new Vector3(-6,0,0),new Vector3(6,0,0)};
  foreach(var p in match.pockets)Shape("Pocket capture",PrimitiveType.Cylinder,p+Vector3.up*.015f,new Vector3(1.8f,.02f,1.8f),black,false);
  foreach(float x in new[]{-6.25f,6.25f})foreach(float z in new[]{-6.2f,6.2f}){
   var g=Shape("Side cushion",PrimitiveType.Cube,new Vector3(x,.4f,z),new Vector3(.5f,.8f,10.5f),rail);g.GetComponent<Collider>().sharedMaterial=physics;}
  foreach(float z in new[]{-12.25f,12.25f}){var g=Shape("End cushion",PrimitiveType.Cube,new Vector3(0,.4f,z),new Vector3(10.5f,.8f,.5f),rail);g.GetComponent<Collider>().sharedMaterial=physics;}
  // Backstop segments beyond capture zones keep balls on table without sealing pocket mouths.
  foreach(float x in new[]{-6.95f,6.95f})Shape("Outer ball backstop",PrimitiveType.Cube,new Vector3(x,.5f,0),new Vector3(.2f,1,26),wood);
  foreach(float z in new[]{-12.95f,12.95f})Shape("Outer ball backstop",PrimitiveType.Cube,new Vector3(0,.5f,z),new Vector3(14,1,.2f),wood);
  var ballPhysics=new PhysicsMaterial("Ball restitution"){bounciness=.94f,dynamicFriction=0,staticFriction=0,bounceCombine=PhysicsMaterialCombine.Minimum};
  ballPhysics=PersistPhysics(ballPhysics,Root+"/Materials/Ball.physicsMaterial");
  match.balls=new BilliardsBall[16];int[] rack={1,9,2,10,8,3,4,11,5,12,13,6,14,7,15};int idx=0;
  Vector3[] locations=new Vector3[16];locations[0]=new Vector3(0,.5f,-6);
  for(int row=0;row<5;row++)for(int col=0;col<=row;col++)locations[rack[idx++]]=new Vector3((col-row*.5f)*1.025f,.5f,4.5f+row*.89f);
  Color[] colors={Color.white,new Color(1,.8f,.05f),new Color(.05f,.18f,.95f),new Color(.9f,.05f,.08f),new Color(.42f,.08f,.65f),new Color(1,.35f,.03f),new Color(.05f,.45f,.15f),new Color(.5f,.04f,.04f),Color.black};
  for(int n=0;n<16;n++){
   Color c=colors[n<9?n:n-8];var material=Mat("Ball"+n,c);
   if(n>8){var tex=new Texture2D(64,32);for(int y=0;y<32;y++)for(int x=0;x<64;x++)tex.SetPixel(x,y,y>9&&y<23?c:Color.white);tex.Apply();string texturePath=Root+"/Materials/Stripe"+n+".asset";var existing=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);if(existing!=null){EditorUtility.CopySerialized(tex,existing);UnityEngine.Object.DestroyImmediate(tex);tex=existing;}else AssetDatabase.CreateAsset(tex,texturePath);material.mainTexture=tex;material.color=Color.white;}
   var g=Shape("Ball "+n,PrimitiveType.Sphere,locations[n],Vector3.one,material);
   var b=g.AddComponent<BilliardsBall>();b.number=n;b.match=match;b.initial=locations[n];b.sphere=g.GetComponent<SphereCollider>();b.sphere.sharedMaterial=ballPhysics;
   b.body=g.AddComponent<Rigidbody>();b.body.mass=1;b.body.useGravity=false;b.body.constraints=RigidbodyConstraints.FreezePositionY|RigidbodyConstraints.FreezeRotation;
   b.body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;b.body.interpolation=RigidbodyInterpolation.Interpolate;match.balls[n]=b;
  }
  var camera=new GameObject("Table Camera").AddComponent<Camera>();camera.tag="MainCamera";camera.transform.position=new Vector3(19,29,0);camera.transform.LookAt(Vector3.zero);camera.orthographic=true;camera.orthographicSize=9.2f;camera.backgroundColor=new Color(.015f,.024f,.04f);camera.clearFlags=CameraClearFlags.SolidColor;camera.gameObject.AddComponent<AudioListener>();match.tableCamera=camera;
  // Number decals fixed above the ball, readable from the full-table camera.
  var labelMat=Mat("Number white",Color.white);
  foreach(var b in match.balls){if(b.number==0)continue;
   var disk=Shape("Number patch",PrimitiveType.Cylinder,b.initial+Vector3.up*.49f,new Vector3(.52f,.008f,.52f),labelMat,false);disk.transform.SetParent(b.transform,true);
   var text=new GameObject("Number "+b.number).AddComponent<TextMesh>();text.transform.SetParent(b.transform);text.transform.localPosition=new Vector3(0,.59f,0);text.transform.rotation=camera.transform.rotation;text.text=b.number.ToString();text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.fontSize=64;text.characterSize=.075f;text.color=Color.black;
  }
  var light=new GameObject("Table Light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.5f;light.transform.rotation=Quaternion.Euler(55,-25,0);RenderSettings.ambientLight=new Color(.65f,.65f,.65f);
  match.player=Truck(match,0,new Vector3(0,0,-8.3f),Color.cyan);match.ai=Truck(match,1,new Vector3(4.4f,0,9.5f),new Color(1,.36f,.03f));
  match.ai.transform.rotation=Quaternion.Euler(0,180,0);match.opponent=root.AddComponent<BilliardsAI>();match.opponent.match=match;match.opponent.truck=match.ai;
  var previewMat=Mat("Placement preview",new Color(.2f,1,.3f,.45f));previewMat.SetFloat("_Surface",1);previewMat.SetFloat("_SrcBlend",5);previewMat.SetFloat("_DstBlend",10);previewMat.SetFloat("_ZWrite",0);previewMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");previewMat.renderQueue=3000;
  match.preview=Shape("Cue placement preview",PrimitiveType.Sphere,new Vector3(0,.5f,-6),Vector3.one,previewMat,false).transform;match.preview.gameObject.SetActive(false);
  UI(match);
  EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
  BilliardsMenuBuilder.Upgrade();LWS.TruckSports.Editor.SportsPolishAuthoring.UpgradeScene(ScenePath);ValidateSavedScene();Debug.Log("TB_SCENE_CREATED "+ScenePath);
 }
 static PhysicsMaterial PersistPhysics(PhysicsMaterial value,string path){
  var existing=AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
  if(existing==null){AssetDatabase.CreateAsset(value,path);return value;}
  EditorUtility.CopySerialized(value,existing);UnityEngine.Object.DestroyImmediate(value);return existing;
 }
 static BilliardsTruck Truck(BilliardsMatch match,int side,Vector3 p,Color color){
  var root=new GameObject(side==0?"Player Truck / Blue":"AI Truck / Orange");root.transform.position=Vector3.zero;
  // Copy only render geometry from the commercial prefab, no vehicle stack or collider.
  var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NWH/Vehicle Physics 2/Vehicles/Euro Truck by GR3D/SemiTruck.prefab");
  if(source==null)throw new Exception("NWH truck prefab missing");
  var copy=(GameObject)PrefabUtility.InstantiatePrefab(source);var visual=new GameObject("NWH GR3D visual only");visual.transform.SetParent(root.transform,false);
  foreach(var mf in copy.GetComponentsInChildren<MeshFilter>(true)){
   var mr=mf.GetComponent<MeshRenderer>();if(mr==null||mf.sharedMesh==null)continue;
   var part=new GameObject(mf.name);part.transform.SetParent(visual.transform);part.transform.position=mf.transform.position;part.transform.rotation=mf.transform.rotation;part.transform.localScale=mf.transform.lossyScale;
   part.AddComponent<MeshFilter>().sharedMesh=mf.sharedMesh;part.AddComponent<MeshRenderer>().sharedMaterials=mr.sharedMaterials;
  }
  UnityEngine.Object.DestroyImmediate(copy);
  var renderers=visual.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
  float scale=1/Mathf.Max(bounds.size.x,bounds.size.z);visual.transform.localScale=Vector3.one*scale;
  visual.transform.localPosition=new Vector3(-bounds.center.x*scale,-bounds.min.y*scale,-bounds.center.z*scale);
  var bumper=Shape("Colored front bumper",PrimitiveType.Cube,new Vector3(0,.19f,.48f),new Vector3(.47f,.2f,.1f),Mat(side==0?"Player Blue":"AI Orange",color),false);bumper.transform.SetParent(root.transform,true);
  var label=new GameObject("Truck label").AddComponent<TextMesh>();label.transform.SetParent(root.transform);label.transform.localPosition=new Vector3(0,.75f,0);label.transform.rotation=match.tableCamera.transform.rotation;label.text=side==0?"YOU":"AI";label.anchor=TextAnchor.MiddleCenter;label.fontSize=48;label.characterSize=.055f;label.color=color;label.gameObject.AddComponent<BilliardsBillboard>().cameraTarget=match.tableCamera;
  root.transform.position=p;var truck=root.AddComponent<BilliardsTruck>();truck.match=match;truck.side=side;
  PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/"+(side==0?"PlayerTruck":"AITruck")+".prefab");return truck;
 }
 static GameObject Rect(string name,Transform parent,Vector2 anchor,Vector2 size,Vector2 offset){
  var g=new GameObject(name,typeof(RectTransform));g.transform.SetParent(parent,false);var r=g.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=anchor;r.pivot=anchor;r.sizeDelta=size;r.anchoredPosition=offset;return g;
 }
 static TMPro.TextMeshProUGUI Text(string name,Transform parent,Vector2 a,Vector2 size,Vector2 pos,int font=24){
  var g=Rect(name,parent,a,size,pos);var t=g.AddComponent<TMPro.TextMeshProUGUI>();t.font=TMPro.TMP_Settings.defaultFontAsset;t.fontSize=font;t.color=Color.white;t.alignment=TMPro.TextAlignmentOptions.Center;t.overflowMode=TMPro.TextOverflowModes.Overflow;t.raycastTarget=false;return t;
 }
 static void UI(BilliardsMatch match){
  var canvas=new GameObject("Truck Billiards HUD",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
  var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
  var hud=canvas.AddComponent<BilliardsHUD>();hud.match=match;
  hud.banner=Text("Turn",canvas.transform,new Vector2(.5f,1),new Vector2(650,65),new Vector2(0,-5),38);
  hud.status=Text("Shot status",canvas.transform,new Vector2(.5f,1),new Vector2(940,48),new Vector2(0,-68),24);
  hud.playerPanel=Text("Player balls",canvas.transform,new Vector2(0,1),new Vector2(460,125),new Vector2(15,-10),23);
  hud.aiPanel=Text("AI balls",canvas.transform,new Vector2(1,1),new Vector2(460,125),new Vector2(-15,-10),23);
  hud.notice=Text("Notice",canvas.transform,new Vector2(.5f,0),new Vector2(1350,48),new Vector2(0,102),24);
  hud.speed=Text("Power",canvas.transform,new Vector2(0,0),new Vector2(470,80),new Vector2(15,12),23);
  var controls=Text("Controls",canvas.transform,new Vector2(1,0),new Vector2(1000,80),new Vector2(-15,12),23);controls.text="W/S or ↑/↓ accelerate / reverse  •  A/D or ←/→ steer  •  SPACE brake\nR reset truck  •  ESC restart match  •  Placement: mouse + LEFT CLICK";
  hud.overlay=Rect("Menu and result",canvas.transform,new Vector2(.5f,.5f),new Vector2(860,510),Vector2.zero);hud.overlay.AddComponent<Image>().color=new Color(.02f,.035f,.055f,.98f);
  hud.overlayTitle=Text("Title",hud.overlay.transform,new Vector2(.5f,1),new Vector2(810,100),new Vector2(0,-25),48);
  hud.overlayBody=Text("Description",hud.overlay.transform,new Vector2(.5f,.5f),new Vector2(800,170),new Vector2(0,20),26);
  var button=Rect("Heat Play Again",hud.overlay.transform,new Vector2(.5f,0),new Vector2(420,85),new Vector2(0,40));button.AddComponent<Image>().color=new Color(.04f,.35f,.33f);
  var caption=Rect("Caption",button.transform,new Vector2(.5f,.5f),new Vector2(410,80),Vector2.zero).AddComponent<TMPro.TextMeshProUGUI>();caption.text="PLAY — YOU BREAK";caption.fontSize=30;caption.alignment=TMPro.TextAlignmentOptions.Center;caption.raycastTarget=false;
  hud.playButton=button.AddComponent<ButtonManager>();hud.playButton.useLocalization=false;hud.playButton.useSounds=false;hud.playButton.autoFitContent=false;hud.playButton.checkForDoubleClick=false;hud.playButton.normalTextObj=caption;hud.playButton.textSize=30;
  var eventSystem=new GameObject("Billiards Event System",typeof(EventSystem),typeof(StandaloneInputModule)).GetComponent<EventSystem>();match.gameObject.AddComponent<BilliardsHostIsolation>().modeEventSystem=eventSystem;
 }
 public static void ValidateSavedScene(){
  var scene=EditorSceneManager.OpenScene(ScenePath);
  var match=UnityEngine.Object.FindAnyObjectByType<BilliardsMatch>();
  if(match==null||match.balls.Length!=16||match.player==null||match.ai==null||match.opponent==null||match.tableCamera==null||match.pockets.Length!=6)throw new Exception("Saved scene wiring invalid");
  var session=match.GetComponent<BilliardsSession>();
  if(session==null||session.input==null||session.menus==null||session.settings==null||session.players.Length!=2||session.menus.pages[(int)BilliardsScreen.Result]==null)throw new Exception("Local multiplayer session/menu wiring invalid");
  if(session.feedback==null||session.feedback.clips==null||session.feedback.clips.Length!=8||session.feedback.clips.Any(c=>c==null)||session.celebration==null||session.menus.formatText==null||session.menus.nextRackButton==null)throw new Exception("Series/practice/feedback scene wiring invalid");
  foreach(var t in new[]{match.player,match.ai})if(t.GetComponentsInChildren<Collider>(true).Length!=0)throw new Exception("Truck collider violates ball isolation");
  foreach(var root in scene.GetRootGameObjects())foreach(var tr in root.GetComponentsInChildren<Transform>(true))if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(tr.gameObject)>0)throw new Exception("Missing script: "+tr.name);
  Debug.Log("TB_SAVED_SCENE_VALIDATED 16 balls, 6 pockets, 2 isolated trucks, AI, HUD, camera");
 }
 [MenuItem("Truck Billiards/Build Windows x64 Development")]
 public static void Build(){
  ValidateSavedScene();BilliardsRuleFixtures.Run();BilliardsFeatureFixtures.Run();
  string path="Builds/TruckBilliards/TruckBilliards.exe";Directory.CreateDirectory(Path.GetDirectoryName(path));
  string before=File.ReadAllText("ProjectSettings/EditorBuildSettings.asset");
  var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName=path,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
  File.WriteAllText("Logs/TruckBilliards/build-summary.txt",report.summary.result+"\nPlatform: "+report.summary.platform+"\nOptions: "+report.summary.options+"\nSize: "+report.summary.totalSize+"\nErrors: "+report.summary.totalErrors+"\nWarnings: "+report.summary.totalWarnings+"\nScene: "+ScenePath);
  if(File.ReadAllText("ProjectSettings/EditorBuildSettings.asset")!=before)throw new Exception("Host build scene configuration changed");
  if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Truck Billiards build failed: "+report.summary.result);
  Debug.Log("TB_BUILD_SUCCEEDED "+Path.GetFullPath(path));
 }
 public static void CreateValidateBuild(){Create();Build();}
}}







