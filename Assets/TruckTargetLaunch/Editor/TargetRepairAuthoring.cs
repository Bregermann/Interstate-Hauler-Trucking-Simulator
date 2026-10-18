using System;
using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using LWS.TruckSports;
using LWS.TruckSports.Editor;
using LWS.TruckTerrierRacing;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
namespace LWS.TruckTargetLaunch.Editor {
public static class TargetRepairAuthoring {
 const string EnvironmentPath="Assets/TruckTargetLaunch/Prefabs/Volcanic launch crater.prefab";
 public static void Upgrade(){
  if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode before Target authoring.");
  for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save the open scene first.");
  var previous=EditorSceneManager.GetSceneManagerSetup();try{EnsureEnvironmentPrefab();var scene=EditorSceneManager.OpenScene(TargetAuthoring.Scene);Install(UnityEngine.Object.FindAnyObjectByType<TargetMatch>());EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();}
  finally{if(previous.Any(x=>x.isLoaded&&!string.IsNullOrEmpty(x.path)))EditorSceneManager.RestoreSceneManagerSetup(previous);else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);}
 }
 public static void Install(TargetMatch match){
  if(match==null)throw new InvalidOperationException("No Target Launch authority.");
  if(match.volcano==null)InstallVolcano(match);else if(PrefabUtility.GetCorrespondingObjectFromSource(match.volcano.gameObject)!=AssetDatabase.LoadAssetAtPath<GameObject>(EnvironmentPath)){var old=match.volcano;var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(EnvironmentPath));root.transform.position=old.lava.position;var replacement=root.GetComponent<TargetVolcano>();replacement.match=match;replacement.wrecks=old.wrecks;replacement.debrisSources=old.debrisSources;root.SetActive(true);match.volcano=replacement;match.missGround=replacement.lava.GetComponent<Collider>();UnityEngine.Object.DestroyImmediate(old.gameObject);}
  if(match.scoreFeedback==null){var feedback=match.gameObject.AddComponent<TargetScoreFeedback>();feedback.match=match;feedback.rings=new LineRenderer[4];var material=SportsEventAuthoring.Mat(TargetAuthoring.Folder,"Final scoring ring highlight",Color.white,.1f);for(int i=0;i<4;i++){var line=new GameObject("Final ring celebration "+i).AddComponent<LineRenderer>();line.transform.SetParent(feedback.transform,false);line.useWorldSpace=true;line.sharedMaterial=material;line.positionCount=97;line.startWidth=line.endWidth=.22f;line.gameObject.SetActive(false);feedback.rings[i]=line;}match.scoreFeedback=feedback;}
  match.volcano.wrecks.minimumUpwardVelocity=9;EditorUtility.SetDirty(match.volcano.wrecks);
  InstallLook(match);
  if(match.director.impactGroups==null||match.director.impactGroups.Length!=4){
   match.director.impactGroups=new CinemachineTargetGroup[4];match.volcano.impactFocus=new Transform[4];
   for(int i=0;i<4;i++){
    var focus=new GameObject("Recorded lava impact focus "+i).transform;focus.SetParent(match.volcano.transform,false);match.volcano.impactFocus[i]=focus;
    var group=new GameObject("Owned Cinemachine impact group "+i).AddComponent<CinemachineTargetGroup>();group.transform.SetParent(match.director.transform,false);group.m_PositionMode=CinemachineTargetGroup.PositionMode.GroupCenter;group.m_RotationMode=CinemachineTargetGroup.RotationMode.Manual;group.m_Targets=new[]{new CinemachineTargetGroup.Target{target=focus,weight=1,radius=8}};match.director.impactGroups[i]=group;
   }
  }
  foreach(var marker in match.landingMarkers){string prior=marker.name;marker.name=marker.name.Replace("first-contact","final-rest");var ghost=match.replay.ghosts[4].Find(prior);if(ghost!=null)ghost.name=marker.name;}
  // Match-owned replay keeps target at index4; four pooled fragment groups follow it.
  foreach(var camera in match.playerCameras.Concat(new[]{match.broadcast})){
   camera.view.farClipPlane=5000;camera.view.allowHDR=true;var data=camera.view.GetUniversalAdditionalCameraData();data.requiresColorTexture=true;data.renderPostProcessing=true;data.volumeLayerMask|=1;
   var listener=camera.rig.GetComponent<CinemachineImpulseListener>()??camera.rig.gameObject.AddComponent<CinemachineImpulseListener>();listener.m_ChannelMask=1;listener.m_Gain=.35f;listener.m_Use2DDistance=false;
  }
  foreach(var panel in match.menus.viewPanels){var backing=panel.Find("View data backing");if(backing!=null&&backing.GetComponent<SportsBroadcastPlate>()==null)backing.gameObject.AddComponent<SportsBroadcastPlate>().cut=12;}
  foreach(var label in match.menus.pages[4].GetComponentsInChildren<TMPro.TMP_Text>(true))if(label.name=="Rules and controls")label.text=match.Help;
  // Native setters on prefab instances do not reliably serialize overrides by themselves.
  // Record the owned environment's changed references, including the terrain collider.
  foreach(var component in match.volcano.GetComponentsInChildren<Component>(true))if(component!=null){EditorUtility.SetDirty(component);if(PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);}
  foreach(var child in match.volcano.GetComponentsInChildren<Transform>(true))if(PrefabUtility.IsPartOfPrefabInstance(child.gameObject))PrefabUtility.RecordPrefabInstancePropertyModifications(child.gameObject);
  EditorUtility.SetDirty(match);Debug.Log("TARGET_REPAIR_SAVED final physical rest, shared volcano and wreck pool");
 }
 static void InstallLook(TargetMatch match){
  var pipeline=match.GetComponent<LWS.TruckBaseJumping.BaseJumpRendering>().pipeline as UniversalRenderPipelineAsset;
  if(pipeline==null||!pipeline.supportsHDR)throw new InvalidOperationException("Target Launch requires its existing HDR URP pipeline for the volcanic grade.");
  InstallTerrainLook(match);
  var source=AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckTugOfWar/Materials/Glowing distant lava.mat");
  const string lavaPath="Assets/TruckTargetLaunch/Materials/Target molten channels.mat";var lava=AssetDatabase.LoadAssetAtPath<Material>(lavaPath);
  if(lava==null){lava=new Material(source);lava.name="Target molten channels";lava.SetFloat("_ChannelWidth",.065f);lava.SetFloat("_PatternScale",.045f);AssetDatabase.CreateAsset(lava,lavaPath);}
  match.volcano.lava.GetComponent<Renderer>().sharedMaterial=lava;
  const string profilePath="Assets/TruckTargetLaunch/Materials/Volcanic HDR grade.asset";var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
  if(profile==null){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,profilePath);var tone=profile.Add<Tonemapping>(true);tone.mode.value=TonemappingMode.ACES;AssetDatabase.AddObjectToAsset(tone,profile);var bloom=profile.Add<Bloom>(true);bloom.intensity.value=.3f;bloom.threshold.value=1.2f;bloom.scatter.value=.6f;bloom.downscale.value=BloomDownscaleMode.Quarter;bloom.highQualityFiltering.value=false;AssetDatabase.AddObjectToAsset(bloom,profile);var color=profile.Add<ColorAdjustments>(true);color.postExposure.value=-.25f;color.contrast.value=12;color.saturation.value=-5;AssetDatabase.AddObjectToAsset(color,profile);EditorUtility.SetDirty(profile);}
  var grade=match.GetComponentInChildren<Volume>(true);if(grade==null){var go=new GameObject("Target volcanic HDR grade");go.transform.SetParent(match.transform,false);go.layer=0;grade=go.AddComponent<Volume>();}grade.isGlobal=true;grade.priority=20;grade.weight=1;grade.sharedProfile=profile;
  var blast=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/WeatherMaker/Prefab/Sound/Weather/WeatherMakerThunderIntense1.mp3");if(blast==null)throw new InvalidOperationException("Existing Cheese/Weather Maker blast sound missing.");int index=Array.IndexOf(match.feedback.clips,blast);if(index<0){index=match.feedback.clips.Length;match.feedback.clips=match.feedback.clips.Concat(new[]{blast}).ToArray();}match.volcano.blastSoundIndex=index;
 }
 static void InstallTerrainLook(TargetMatch match){
  const string dataPath="Assets/TruckTargetLaunch/Models/Volcanic launch terrain.asset";var data=AssetDatabase.LoadAssetAtPath<TerrainData>(dataPath);
  if(data==null){
   data=UnityEngine.Object.Instantiate(match.volcano.terrain.terrainData);data.name="Target authored volcanic terrain";AssetDatabase.CreateAsset(data,dataPath);
   var cliff=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/RiverModeler/Demo/Viking Village/Terrain/Black_Sand_Rocks_B.terrainlayer"));cliff.name="Target basalt cliffs";cliff.diffuseTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/NWH/Common/Textures/Ground/CliffAlbedo.png");cliff.normalMapTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/NWH/Common/Textures/Ground/CliffNormal.png");cliff.tileSize=Vector2.one*38;cliff.normalScale=.8f;cliff.metallic=0;cliff.smoothness=.05f;cliff.diffuseRemapMax=new Vector4(.44f,.47f,.51f,1);AssetDatabase.CreateAsset(cliff,"Assets/TruckTargetLaunch/Materials/Basalt cliff layer.terrainlayer");
   var ash=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/RiverModeler/Demo/Viking Village/Terrain/Black_Sand_Rocks_B.terrainlayer"));ash.name="Target black ash and stones";ash.tileSize=Vector2.one*12;ash.normalScale=.8f;ash.metallic=0;ash.smoothness=.05f;AssetDatabase.CreateAsset(ash,"Assets/TruckTargetLaunch/Materials/Black ash layer.terrainlayer");
   var coarse=UnityEngine.Object.Instantiate(ash);coarse.name="Target coarse volcanic rock";coarse.tileSize=Vector2.one*24;coarse.diffuseRemapMax=new Vector4(.65f,.68f,.72f,1);AssetDatabase.CreateAsset(coarse,"Assets/TruckTargetLaunch/Materials/Coarse volcanic layer.terrainlayer");
   data.terrainLayers=new[]{cliff,ash,coarse};EditorUtility.SetDirty(data);
  }
  match.volcano.terrain.terrainData=data;match.volcano.terrain.GetComponent<TerrainCollider>().terrainData=data;
  var obsolete=GameObject.Find("Installed quarry cliffs and distant scale references");if(obsolete!=null)obsolete.SetActive(false);
  const string rockPath="Assets/TruckTargetLaunch/Materials/Weathered basalt formations.mat";var rock=AssetDatabase.LoadAssetAtPath<Material>(rockPath);
  if(rock==null){rock=SportsEventAuthoring.Mat(TargetAuthoring.Folder,"Weathered basalt formations",new Color(.28f,.3f,.33f),.12f);rock.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/NWH/Common/Textures/Ground/CliffAlbedo.png");rock.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/NWH/Common/Textures/Ground/CliffNormal.png"));rock.SetFloat("_BumpScale",.6f);rock.EnableKeyword("_NORMALMAP");EditorUtility.SetDirty(rock);}
  // The reused flat quarry clusters read as repetitive floating tiles on this steep crater.
  // Keep its collision terrain, and dress the distant rim with installed Vista rock models.
  var formations=match.volcano.transform.Find("Installed volcanic rock formations");if(formations!=null)formations.gameObject.SetActive(false);
  if(match.volcano.transform.Find("Target basalt outcrops")==null){
   var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PinwheelStudio/Vista/Personal/Samples/Prefabs/VistaSample_DesertRock.prefab");if(source==null)throw new InvalidOperationException("Installed Vista rock model required.");
   var root=new GameObject("Target basalt outcrops").transform;root.SetParent(match.volcano.transform,false);
   for(int i=0;i<28;i++){
    float angle=i*2.399963f,radius=365+(i%5)*24;var p=match.volcano.lava.position+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius);var terrain=match.volcano.terrain;var uv=p-terrain.transform.position;var normal=data.GetInterpolatedNormal(Mathf.Clamp01(uv.x/data.size.x),Mathf.Clamp01(uv.z/data.size.z));p.y=terrain.SampleHeight(p)+terrain.transform.position.y;
    var go=(GameObject)PrefabUtility.InstantiatePrefab(source);go.name="Installed Vista basalt outcrop "+i;go.transform.SetParent(root,true);go.transform.position=Vector3.zero;go.transform.rotation=Quaternion.FromToRotation(Vector3.up,Vector3.Slerp(Vector3.up,normal,.65f))*Quaternion.Euler(0,i*137.5f,0)*source.transform.rotation;
    var renderer=go.GetComponentInChildren<MeshRenderer>();float size=20+(i%4)*6;go.transform.localScale*=size/Mathf.Max(renderer.bounds.size.x,renderer.bounds.size.z);var bounds=renderer.bounds;go.transform.position=p-new Vector3(bounds.center.x,bounds.min.y+bounds.size.y*.22f,bounds.center.z);renderer.sharedMaterials=Enumerable.Repeat(rock,renderer.sharedMaterials.Length).ToArray();
   }
  }
 }
 public static void EnsureEnvironmentPrefab(){
  if(AssetDatabase.LoadAssetAtPath<GameObject>(EnvironmentPath)!=null)return;
  for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save open scenes before preparing the shared volcanic environment.");
  var previous=EditorSceneManager.GetSceneManagerSetup();GameObject clone=null;
  try{
   // Single-scene extraction: never run two Weather Maker authorities together.
   EditorSceneManager.OpenScene(LWS.TruckTerrierRacing.Editor.TerrierAuthoring.Scene);
   var source=UnityEngine.Object.FindAnyObjectByType<TerrierVolcano>();if(source==null)throw new InvalidOperationException("The saved Terrier volcanic environment is required.");
   clone=UnityEngine.Object.Instantiate(source.gameObject);clone.SetActive(false);clone.name="Volcanic launch crater";
   var original=clone.GetComponent<TerrierVolcano>();clone.transform.position-=original.lava.position;
   var environment=clone.AddComponent<TargetVolcano>();environment.terrain=original.terrain;environment.lava=original.lava;environment.splashes=original.splashes;
   var priorSurface=environment.lava.GetComponent<TerrierLavaSurface>();if(priorSurface!=null)UnityEngine.Object.DestroyImmediate(priorSurface);
   environment.lava.gameObject.AddComponent<TargetLavaSurface>().volcano=environment;
   var scaffolds=clone.transform.Find("Installed scaffold bridge pylons");if(scaffolds!=null)UnityEngine.Object.DestroyImmediate(scaffolds.gameObject);
   UnityEngine.Object.DestroyImmediate(original);
   environment.impulse=clone.AddComponent<CinemachineImpulseSource>();environment.impulse.m_ImpulseDefinition.m_ImpulseType=CinemachineImpulseDefinition.ImpulseTypes.Uniform;environment.impulse.m_ImpulseDefinition.m_ImpulseShape=CinemachineImpulseDefinition.ImpulseShapes.Explosion;environment.impulse.m_ImpulseDefinition.m_ImpulseDuration=.3f;
   System.IO.Directory.CreateDirectory("Assets/TruckTargetLaunch/Prefabs");AssetDatabase.Refresh();PrefabUtility.SaveAsPrefabAsset(clone,EnvironmentPath);
  }finally{if(clone!=null)UnityEngine.Object.DestroyImmediate(clone);if(previous.Any(x=>x.isLoaded&&!string.IsNullOrEmpty(x.path)))EditorSceneManager.RestoreSceneManagerSetup(previous);else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);}
 }
 static void InstallVolcano(TargetMatch match){
  var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(EnvironmentPath);if(prefab==null)throw new InvalidOperationException("Prepare the shared volcanic environment prefab from a saved scene before authoring Target Launch.");
  var root=(GameObject)PrefabUtility.InstantiatePrefab(prefab);root.transform.position=new Vector3(0,-15.5f,130);root.SetActive(true);var volcano=root.GetComponent<TargetVolcano>();volcano.match=match;
  match.missGround.gameObject.SetActive(false);match.missGround=volcano.lava.GetComponent<Collider>();match.volcano=volcano;InstallFragments(match,volcano);
 }
 static void InstallFragments(TargetMatch match,TargetVolcano volcano){
  var pool=match.gameObject.AddComponent<TerrierWreckPool>();pool.match=null;pool.sourceTrucks=match.racers.Select(r=>r.truck).ToArray();pool.externalReplay=true;pool.lifetime=6;volcano.wrecks=pool;
  var partsRoot=new GameObject("Pooled actual Target truck fragments").transform;volcano.debrisSources=new Transform[4];
  var ghostGroups=new Transform[4];var parts=new List<TerrierWreckPool.Piece>();
  var names=new[]{"Chassis","Cab","door rt","doors lft","Whl HD FL","Whl HD FR","Whl HD RL","Whl HD RR","Exhaust","interior","mirror lft.001","mirror rt.001"};
  var contact=AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/TruckTerrierRacing/Materials/Wreck fragments contact.asset");if(contact==null)throw new InvalidOperationException("Existing truck-fragment contact material required.");
  for(int owner=0;owner<4;owner++){
   var group=new GameObject("Target fragments participant "+owner).transform;group.SetParent(partsRoot,false);volcano.debrisSources[owner]=group;
   var ghosts=new GameObject("Recorded Target fragments "+owner).transform;ghostGroups[owner]=ghosts;
   foreach(var source in pool.sourceTrucks[owner].visuals.GetComponentsInChildren<MeshFilter>(true).Where(f=>names.Contains(f.name))){
    var g=new GameObject("Actual wreck part "+owner+" · "+source.name);g.layer=2;g.transform.SetParent(group,false);g.transform.localScale=source.transform.lossyScale;g.AddComponent<MeshFilter>().sharedMesh=source.sharedMesh;var renderer=g.AddComponent<MeshRenderer>();renderer.sharedMaterials=source.GetComponent<Renderer>().sharedMaterials;
    var shape=g.AddComponent<BoxCollider>();shape.center=source.sharedMesh.bounds.center;shape.size=source.sharedMesh.bounds.size;shape.sharedMaterial=contact;
    var body=g.AddComponent<Rigidbody>();body.mass=source.name=="Cab"?2:source.name=="Chassis"?3:.5f;body.centerOfMass=shape.center;body.maxLinearVelocity=45;body.maxAngularVelocity=16;body.maxDepenetrationVelocity=2;body.linearDamping=.1f;body.angularDamping=.3f;body.interpolation=RigidbodyInterpolation.Interpolate;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;body.isKinematic=true;
    var ghost=new GameObject("Recorded wreck part "+owner+" · "+source.name);ghost.transform.SetParent(ghosts,false);ghost.AddComponent<MeshFilter>().sharedMesh=source.sharedMesh;var ghostRenderer=ghost.AddComponent<MeshRenderer>();ghostRenderer.sharedMaterials=renderer.sharedMaterials;
    parts.Add(new TerrierWreckPool.Piece{owner=owner,source=source.transform,body=body,shape=shape,renderer=renderer,ghost=ghost.transform,ghostRenderer=ghostRenderer});g.SetActive(false);
   }
   ghosts.gameObject.SetActive(false);
  }
  pool.pieces=parts.ToArray();if(parts.Count!=48)throw new InvalidOperationException("Four trucks must each have twelve actual mesh fragments, got "+parts.Count);
  match.replay.sources=match.replay.sources.Concat(volcano.debrisSources).ToArray();match.replay.ghosts=match.replay.ghosts.Concat(ghostGroups).ToArray();EditorUtility.SetDirty(pool);
 }
}
}
