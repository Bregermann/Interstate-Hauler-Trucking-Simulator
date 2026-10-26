using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LWS.TruckFieldGoal;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace LWS.TruckBeerPong.Editor
{
 public static class PongTailgateAuthoring
 {
  const string Folder="Assets/TruckBeerPong/Carnival",Library=Folder+"/Tailgate stadium and render library.prefab";
  static readonly string Cars="Assets/UTS_FullPack/Models/Cars/Car_Prefabs/Day Cars/";
  static readonly string Food="Assets/Modern Resaturant - building and interior/Models/Prefabs/";
  [MenuItem("Truck Sports/Beer Pong/Install football tailgate and event crowd in open scene")]
  public static void InstallOpenScene()
  {
   var match=UnityEngine.Object.FindAnyObjectByType<PongMatch>();if(match==null||match.gameObject.scene.path!="Assets/TruckBeerPong/Scenes/TruckBeerPong.unity")throw new InvalidOperationException("Open the existing Beer Pong scene first.");
   BuildRenderLibrary(match);Install(match,true);EditorSceneManager.MarkSceneDirty(match.gameObject.scene);
  }
  // Copy native render components from asset data. Never instantiate live NWH,
  // UTS, ragdoll or passenger scripts just to destroy them after Awake ran.
  static GameObject RenderCopy(Transform source,Transform parent,bool animate=false)
  {
   var transforms=new Dictionary<Transform,Transform>();var renderers=new Dictionary<Renderer,Renderer>();
   Transform Copy(Transform original,Transform owner)
   {
    var node=new GameObject(original.name).transform;node.SetParent(owner,false);node.localPosition=original.localPosition;node.localRotation=original.localRotation;node.localScale=original.localScale;node.gameObject.layer=original.gameObject.layer;node.gameObject.SetActive(original.gameObject.activeSelf);transforms.Add(original,node);foreach(Transform child in original)Copy(child,node);return node;
   }
   var root=Copy(source,parent);root.gameObject.SetActive(false);
   foreach(var pair in transforms)
   {
    var filter=pair.Key.GetComponent<MeshFilter>();if(filter!=null)pair.Value.gameObject.AddComponent<MeshFilter>().sharedMesh=filter.sharedMesh;
    Renderer replacement=null;var renderer=pair.Key.GetComponent<Renderer>();if(renderer!=null&&renderer is SkinnedMeshRenderer skin){var target=pair.Value.gameObject.AddComponent<SkinnedMeshRenderer>();target.sharedMesh=skin.sharedMesh;target.bones=skin.bones.Select(bone=>bone!=null&&transforms.TryGetValue(bone,out var mapped)?mapped:null).ToArray();target.rootBone=skin.rootBone!=null&&transforms.TryGetValue(skin.rootBone,out var mappedRoot)?mappedRoot:null;target.localBounds=skin.localBounds;target.quality=skin.quality;target.updateWhenOffscreen=true;replacement=target;}else if(renderer!=null&&renderer is MeshRenderer)replacement=pair.Value.gameObject.AddComponent<MeshRenderer>();
    if(replacement!=null){replacement.sharedMaterials=renderer.sharedMaterials;replacement.enabled=renderer.enabled;replacement.shadowCastingMode=ShadowCastingMode.Off;replacement.receiveShadows=renderer.receiveShadows;renderers.Add(renderer,replacement);}
    var animator=pair.Key.GetComponent<Animator>();if(animate&&animator!=null){var target=pair.Value.gameObject.AddComponent<Animator>();target.avatar=animator.avatar;target.applyRootMotion=false;target.cullingMode=AnimatorCullingMode.AlwaysAnimate;}
   }
   foreach(var pair in transforms){var original=pair.Key.GetComponent<LODGroup>();if(original!=null){var group=pair.Value.gameObject.AddComponent<LODGroup>();group.SetLODs(original.GetLODs().Select(lod=>new LOD(lod.screenRelativeTransitionHeight,lod.renderers.Where(renderer=>renderer!=null&&renderers.ContainsKey(renderer)).Select(renderer=>renderers[renderer]).ToArray()){fadeTransitionWidth=lod.fadeTransitionWidth}).ToArray());group.fadeMode=original.fadeMode;group.RecalculateBounds();}}
   return root.gameObject;
  }
  static GameObject Source(string path,Transform parent,string name)
  {
   var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(source==null)throw new InvalidOperationException("Existing tailgate art required: "+path);var copy=RenderCopy(source.transform,parent);copy.name=name;return copy;
  }
  const string Human="Assets/LWS/TruckTaxi/Passengers/Animations/Humanoid/",Controller=Folder+"/Tailgate humanoid reactions.controller";
  static Avatar MappedAvatar(string path){var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(model==null)throw new InvalidOperationException("Verified shared humanoid model missing: "+path);var animator=model.GetComponent<Animator>();if(animator==null)throw new InvalidOperationException("Verified shared humanoid model has no Animator: "+path);var avatar=animator.avatar;if(avatar==null||!avatar.isValid||!avatar.isHuman)throw new InvalidOperationException("Verified shared humanoid avatar is invalid: "+path);return avatar;}
  static void RepairCachedTemplates(GameObject existing)
  {
   var originalTemplates=existing.transform.Find("Inactive render-only tailgate templates");if(originalTemplates==null)throw new InvalidOperationException("Cached tailgate render templates are missing.");
   if(originalTemplates.GetComponentsInChildren<MonoBehaviour>(true).All(component=>component==null)&&originalTemplates.GetComponentsInChildren<Rigidbody>(true).Length==0&&originalTemplates.GetComponentsInChildren<Collider>(true).Length==0)return;
   // The first failed author left a required NWH controller/body in its OWN
   // cached template. Replace only that template branch with clean native
   // render data; retain the valid stadium, crowd batches and material assets.
   var contents=PrefabUtility.LoadPrefabContents(Library);try{var prior=contents.transform.Find("Inactive render-only tailgate templates");var clean=RenderCopy(prior,contents.transform);clean.name="Inactive render-only tailgate templates";clean.SetActive(true);UnityEngine.Object.DestroyImmediate(prior.gameObject);PrefabUtility.SaveAsPrefabAsset(contents,Library);Debug.Log("PONG_TAILGATE_CACHED_TEMPLATES_REPAIRED render-only; existing stadium retained.");}finally{PrefabUtility.UnloadPrefabContents(contents);}
  }
  static AnimationClip HumanClip(string name)=>AssetDatabase.LoadAllAssetsAtPath(Human+name+".FBX").OfType<AnimationClip>().First(clip=>!clip.name.StartsWith("__")&&clip.humanMotion);
  static bool HumanControllerReady(AnimatorController controller)
  {
   if(controller==null||controller.layers.Length==0)return false;var machine=controller.layers[0].stateMachine;if(machine==null||machine.defaultState==null)return false;
   foreach(string name in new[]{"Idle","Cheer","Disappointed","Throw"}){var state=machine.states.FirstOrDefault(item=>item.state!=null&&item.state.name==name).state;if(state==null)return false;var clip=state.motion as AnimationClip;if(clip==null||!clip.humanMotion)return false;}return true;
  }
  static void PrepareHumanoids()
  {
   var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Controller);if(HumanControllerReady(controller))return;
   const string throwing=Folder+"/Tailgate humanoid throw.FBX";
   if(!File.Exists(throwing)&&!AssetDatabase.CopyAsset("Assets/UTS_FullPack/Models/People/Animations/Girl no Heel/work_assembly_f.FBX",throwing))throw new InvalidOperationException("Owned copy of existing arcade throw clip could not be prepared.");
   var importer=(ModelImporter)AssetImporter.GetAtPath(throwing);importer.animationType=ModelImporterAnimationType.Human;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;importer.SaveAndReimport();
   var toss=AssetDatabase.LoadAllAssetsAtPath(throwing).OfType<AnimationClip>().First(clip=>!clip.name.StartsWith("__")&&clip.humanMotion);
   if(controller==null)controller=AnimatorController.CreateAnimatorControllerAtPath(Controller);if(controller.layers.Length==0)controller.AddLayer("Base Layer");var layers=controller.layers;if(layers[0].stateMachine==null){var machine=new AnimatorStateMachine{name="Base Layer"};AssetDatabase.AddObjectToAsset(machine,controller);layers[0].stateMachine=machine;}if(layers[0].stateMachine.states.Length==0)layers[0].defaultWeight=1;controller.layers=layers;var stateMachine=controller.layers[0].stateMachine;
   foreach(var pair in new[]{("Idle",HumanClip("idle1")),("Cheer",HumanClip("cheer")),("Disappointed",HumanClip("shout")),("Throw",toss)}){var state=stateMachine.states.FirstOrDefault(item=>item.state!=null&&item.state.name==pair.Item1).state;if(state==null){state=controller.AddMotion(pair.Item2);state.name=pair.Item1;}else if(state.motion==null)state.motion=pair.Item2;if(stateMachine.defaultState==null&&pair.Item1=="Idle")stateMachine.defaultState=state;EditorUtility.SetDirty(state);}EditorUtility.SetDirty(stateMachine);EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();
   if(!HumanControllerReady(controller))throw new InvalidOperationException("Tailgate controller needs four genuine Human reaction states; existing nonempty custom motions were retained.");Debug.Log("PONG_TAILGATE_HUMAN_CONTROLLER_READY layers="+controller.layers.Length+" states="+string.Join(",",stateMachine.states.Select(item=>item.state.name+":"+(item.state.motion!=null?item.state.motion.name:"MISSING")))+" savedBeforeVenue=true");
  }
  static void BuildRenderLibrary(PongMatch match)
  {
   Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
   PrepareHumanoids();
   if(AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Tailgate parking asphalt.mat")==null){var sourceMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckFareFight/Materials/Original town textured asphalt.mat");if(sourceMaterial==null)throw new InvalidOperationException("Owned Taxi/FareFight textured parking asphalt required.");var asphalt=new Material(sourceMaterial){name="Beer Pong tailgate parking asphalt",mainTextureScale=new Vector2(30,30)};AssetDatabase.CreateAsset(asphalt,Folder+"/Tailgate parking asphalt.mat");}
   var existing=AssetDatabase.LoadAssetAtPath<GameObject>(Library);if(existing!=null){RepairCachedTemplates(existing);return;}
   var destination=SceneManager.GetActiveScene();var source=EditorSceneManager.OpenScene("Assets/TruckFieldGoal/Scenes/TruckFieldGoal.unity",OpenSceneMode.Additive);GameObject library=null;
   try
   {
    SceneManager.SetActiveScene(destination);library=new GameObject("Football tailgate - shared Union Field render foundation");var crowd=library.AddComponent<FieldGoalCrowd>();crowd.nearFans=Array.Empty<Animator>();
    var original=source.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<FieldGoalCrowd>(true)).First(item=>item.batches!=null&&item.batches.Length>0);crowd.batches=original.batches.Select(batch=>new FieldGoalCrowd.Batch{idle=batch.idle,cheer=batch.cheer,materials=batch.materials,seats=Array.Empty<Matrix4x4>(),fans=Array.Empty<MeshFilter>()}).ToArray();crowd.cheer=original.cheer;crowd.boo=original.boo;
    var stadium=new GameObject("Huge reused Union Field football stadium render geometry").transform;stadium.SetParent(library.transform,false);int count=0;
    foreach(var renderer in source.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<MeshRenderer>(true)))
    {
     if(renderer.name!="Union Field Grandstand tier"&&renderer.name!="Union Field End-zone concourse"&&renderer.name!="Union Field Press box")continue;
     var filter=renderer.GetComponent<MeshFilter>();if(filter==null||filter.sharedMesh==null)continue;var go=new GameObject(renderer.name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(stadium,false);go.transform.SetPositionAndRotation(renderer.transform.position,renderer.transform.rotation);go.transform.localScale=renderer.transform.lossyScale;go.GetComponent<MeshFilter>().sharedMesh=filter.sharedMesh;go.GetComponent<MeshRenderer>().sharedMaterials=renderer.sharedMaterials;count++;
    }
    if(count<10)throw new InvalidOperationException("Existing football stadium render foundation incomplete.");stadium.localPosition=new Vector3(0,-2,-45);stadium.localScale=Vector3.one*1.5f;
    var templates=new GameObject("Inactive render-only tailgate templates").transform;templates.SetParent(library.transform,false);
    Source(Cars+"Car_1.prefab",templates,"Parked car A");Source(Cars+"Car_5.prefab",templates,"Parked car B");Source("Assets/NWH/Vehicle Physics 2/Vehicles/MonsterTruck/MonsterTruck.fbx",templates,"Pickup-bodied NWH party truck");Source(Cars+"Truck_2.prefab",templates,"Parked UTS cargo truck");
    var semi=RenderCopy(match.truck.visuals,templates);semi.name="Parked owned semi tractor";
    foreach(string prop in new[]{"Restaurant_Table_Rectangular_1","Restaurant_chair","Pizza","Burger","Wine_Bottel","kitchen_module_oven"})Source(Food+prop+".prefab",templates,prop);
    Source("Assets/NWH/WheelController/DemoContent/_Scenes/WC3D Main Demo/Props/Banner/BannerStart.prefab",templates,"Installed event banner arch");
    var jug=Source("Assets/RoadPropsPack/URP/Prefabs/Canister_URP.prefab",templates,"Existing sealed amber arcade jug");var amber=AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckFareFight/Materials/Reusable Taxi jug translucent amber.mat");if(amber==null)throw new InvalidOperationException("Existing Taxi arcade jug material required.");foreach(var renderer in jug.GetComponentsInChildren<Renderer>(true))renderer.sharedMaterial=amber;
    PrefabUtility.SaveAsPrefabAsset(library,Library);Debug.Log("PONG_TAILGATE_RENDER_LIBRARY stadium="+count+" - only existing mesh/material assets copied, source scene closes unsaved.");
   }
   finally{if(library!=null)UnityEngine.Object.DestroyImmediate(library);SceneManager.SetActiveScene(destination);EditorSceneManager.CloseScene(source,true);}
  }
  static Transform Decor(Transform templates,string source,Transform parent,string name,Vector3 position,float length,float yaw)
  {
   var copy=UnityEngine.Object.Instantiate(templates.Find(source),parent,false);copy.name=name;copy.gameObject.SetActive(true);copy.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));var renderers=copy.GetComponentsInChildren<Renderer>();Bounds bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);copy.localScale*=length/Mathf.Max(.001f,bounds.size.x,bounds.size.y,bounds.size.z);bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);copy.position+=Vector3.up*(position.y-bounds.min.y);return copy;
  }
  static Transform ActorRoot(Animator animator,Transform root){var item=animator.transform;while(item.parent!=null&&item.parent!=root)item=item.parent;return item;}
  public static PongTailgateVenue Install(PongMatch match,bool undo)
  {
   var old=match.GetComponentInChildren<PongTailgateVenue>(true);if(old!=null&&old.crowd!=null&&old.crowd.nearFans?.Length==64&&old.jugs?.Length==4&&old.partyAreas?.Length==6)return old;var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Library);if(prefab==null)throw new InvalidOperationException("Run PongTailgateAuthoring.InstallOpenScene once before native tests; it creates the read-only reused render library.");
   var root=old!=null?old.gameObject:UnityEngine.Object.Instantiate(prefab);
   if(old!=null)
   {
    // Resume only our incomplete installation. Preserve its valid stadium and
    // cached render templates; no original table/cup/vehicle/camera is rebuilt.
    var incomplete=new List<GameObject>();foreach(Transform child in root.transform)if(child.name!="Huge reused Union Field football stadium render geometry"&&child.name!="Inactive render-only tailgate templates")incomplete.Add(child.gameObject);foreach(var child in incomplete)UnityEngine.Object.DestroyImmediate(child);
    foreach(var batch in old.crowd.batches){batch.fans=Array.Empty<MeshFilter>();batch.seats=Array.Empty<Matrix4x4>();}old.crowd.nearFans=Array.Empty<Animator>();
   }
   root.name="Beer Pong football tailgate party";if(old==null)SceneManager.MoveGameObjectToScene(root,match.gameObject.scene);root.transform.SetParent(match.transform,false);var crowd=root.GetComponent<FieldGoalCrowd>();crowd.eventSession=match;crowd.match=null;crowd.runnerChance=0;var venue=old!=null?old:root.AddComponent<PongTailgateVenue>();venue.match=match;venue.crowd=crowd;var templates=root.transform.Find("Inactive render-only tailgate templates");var vehicles=new List<Transform>();var areas=new List<Transform>();var actors=new List<Animator>();
   // Reuse the room's existing physical floor/table and hide only superseded
   // indoor visuals. No physical contact, launch pad or cup is moved/disabled.
   var floorRenderer=match.floor.GetComponent<Renderer>();if(undo)Undo.RecordObject(floorRenderer,"Dress existing floor as tailgate parking");floorRenderer.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Tailgate parking asphalt.mat");if(floorRenderer.sharedMaterial==null)throw new InvalidOperationException("Run explicit tailgate author to prepare the owned parking material.");
   foreach(var renderer in match.gameObject.scene.GetRootGameObjects().SelectMany(go=>go.GetComponentsInChildren<Renderer>(true)))if(renderer.name=="Architectural party room sidewall"||renderer.name=="Party-room back wall"||renderer.name=="Pendant metal shade"){if(undo)Undo.RecordObject(renderer,"Reveal football tailgate");renderer.enabled=false;}
   foreach(var light in match.gameObject.scene.GetRootGameObjects().SelectMany(go=>go.GetComponentsInChildren<Light>(true)))if(light.name.StartsWith("Warm pendant light")){if(undo)Undo.RecordObject(light,"Use outdoor tailgate light");light.enabled=false;}
   for(int side=-1;side<=1;side+=2)for(int n=0;n<10;n++){string asset=n%5==0?"Parked owned semi tractor":n%5==1?"Pickup-bodied NWH party truck":n%5==2?"Parked UTS cargo truck":n%2==0?"Parked car A":"Parked car B";vehicles.Add(Decor(templates,asset,root.transform,asset+" tailgate "+side+"-"+n,new Vector3(side*(n%5==0?68:53),-2,-80+n*17),asset.Contains("semi")?13:asset.Contains("truck")?9:6,side>0?-90:90));}
   var controller=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Controller);if(controller==null)throw new InvalidOperationException("Run explicit tailgate author to prepare genuine humanoid reaction clips.");
   var people=new[]{AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UTS_FullPack/Models/People/Prefabs/Mans/business01_m.prefab"),AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UTS_FullPack/Models/People/Prefabs/Womans/business01_f_highpoly.prefab")};var avatars=new[]{MappedAvatar(Human+"business01_m.FBX"),MappedAvatar(Human+"business01_f.FBX")};if(people.Any(person=>person==null))throw new InvalidOperationException("Existing verified Taxi male/female humanoid render rigs required.");
   for(int side=-1;side<=1;side+=2)for(int row=0;row<2;row++)for(int n=0;n<16;n++){int variant=actors.Count%people.Length;var position=new Vector3(side*(25+row*10),-2,-46+n*6.1f);var person=RenderCopy(people[variant].transform,root.transform,true);person.name="Actual shared Taxi humanoid tailgate spectator "+actors.Count;person.transform.SetPositionAndRotation(position,Quaternion.LookRotation(Vector3.ProjectOnPlane(-position,Vector3.up)));person.transform.localScale=Vector3.one*4;var actor=person.GetComponentInChildren<Animator>(true);if(actor==null)throw new InvalidOperationException("Copied shared humanoid rig has no Animator.");actor.avatar=avatars[variant];actor.runtimeAnimatorController=controller;person.SetActive(true);if(!actor.isHuman)throw new InvalidOperationException("Mapped tailgate spectator is not genuinely humanoid.");actors.Add(actor);}
   var stands=root.GetComponentsInChildren<MeshRenderer>().Where(renderer=>renderer.name=="Union Field Grandstand tier").ToArray();if(stands.Length==0)throw new InvalidOperationException("Actual football grandstands required.");
   crowd.nearFans=actors.ToArray();for(int batch=0;batch<crowd.batches.Length;batch++){var group=crowd.batches[batch];var seats=new List<Matrix4x4>();var fans=new List<MeshFilter>();for(int n=batch;n<320;n+=crowd.batches.Length){var stand=stands[n%stands.Length];int seat=n/stands.Length;var position=stand.transform.TransformPoint(new Vector3(Mathf.Lerp(-.46f,.46f,(seat%20)/19f),.51f,seat<20?-.22f:.22f));var go=new GameObject("Shared posed football party fan "+batch+"-"+n,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root.transform,false);go.transform.SetPositionAndRotation(position,Quaternion.LookRotation(Vector3.ProjectOnPlane(new Vector3(0,0,-45)-position,Vector3.up)));go.transform.localScale=Vector3.one*3.8f;go.GetComponent<MeshFilter>().sharedMesh=group.idle;go.GetComponent<MeshRenderer>().sharedMaterials=group.materials;go.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;seats.Add(go.transform.localToWorldMatrix);fans.Add(go.GetComponent<MeshFilter>());}group.seats=seats.ToArray();group.fans=fans.ToArray();}
   for(int side=-1;side<=1;side+=2)for(int zone=0;zone<3;zone++)
   {
    var area=new GameObject("Football food and drinks party area "+side+"-"+zone).transform;area.SetParent(root.transform,false);area.position=new Vector3(side*70,-2,-55+zone*55);areas.Add(area);var table=Decor(templates,"Restaurant_Table_Rectangular_1",area,"Actual catered tailgate table",area.position,8,0);var top=table.GetComponentsInChildren<Renderer>().Max(renderer=>renderer.bounds.max.y);Decor(templates,"Pizza",area,"Shared tailgate pizza",new Vector3(area.position.x,top,area.position.z),1.3f,0);Decor(templates,"Burger",area,"Shared tailgate burger",new Vector3(area.position.x+2,top,area.position.z),.7f,0);Decor(templates,"Wine_Bottel",area,"Shared party drink",new Vector3(area.position.x-2,top,area.position.z),1.3f,0);Decor(templates,"kitchen_module_oven",area,"Installed outdoor catering station",area.position+Vector3.forward*8,4,180);for(int seat=0;seat<4;seat++)Decor(templates,"Restaurant_chair",area,"Actual tailgate party chair",area.position+new Vector3((seat%2==0?-1:1)*5,0,(seat<2?-1:1)*3),3,seat%2==0?90:-90);
    Decor(templates,"Installed event banner arch",area,"Installed football party entry decoration",area.position+Vector3.forward*13,14,side>0?-90:90);var banner=new GameObject("Football tailgate team banner",typeof(TextMeshPro)).GetComponent<TextMeshPro>();banner.transform.SetParent(area,false);banner.transform.localPosition=new Vector3(0,10,0);banner.transform.localRotation=Quaternion.Euler(0,side>0?-90:90,0);banner.font=match.menus.header.font;banner.text=(side<0?"BLUE CREW":"ORANGE CREW")+"\nFOOTBALL TAILGATE";banner.fontSize=6;banner.rectTransform.sizeDelta=new Vector2(15,5);banner.alignment=TextAlignmentOptions.Center;banner.color=side<0?new Color(.12f,.55f,1):new Color(1,.38f,.12f);
   }
   venue.parkedVehicles=vehicles.ToArray();venue.partyAreas=areas.ToArray();venue.jugs=new Transform[4];for(int i=0;i<4;i++){venue.jugs[i]=Decor(templates,"Existing sealed amber arcade jug",root.transform,"Pooled existing arcade jug "+i,Vector3.zero,.8f,0);venue.jugs[i].gameObject.SetActive(false);}
   var response=new GameObject("Actual shot crowd response",typeof(AudioSource)).GetComponent<AudioSource>();response.transform.SetParent(root.transform,false);response.playOnAwake=false;response.spatialBlend=0;crowd.response=response;
   var atmosphere=new GameObject("Quiet tailgate crowd atmosphere",typeof(AudioSource)).GetComponent<AudioSource>();atmosphere.transform.SetParent(root.transform,false);atmosphere.playOnAwake=false;atmosphere.loop=true;atmosphere.spatialBlend=0;atmosphere.clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/TruckDockDiving/Audio/Ordinary applause.wav");venue.atmosphere=atmosphere;
   if(undo)Undo.RegisterCreatedObjectUndo(root,"Install football tailgate and event crowd");return venue;
  }
 }
}
