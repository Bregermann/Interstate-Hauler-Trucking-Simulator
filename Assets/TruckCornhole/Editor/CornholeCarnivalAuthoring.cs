using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
namespace LWS.TruckCornhole.Editor
{
 // Explicit additive author. The caller owns opening/saving the original scene.
 public static class CornholeCarnivalAuthoring
 {
  const string Folder="Assets/TruckCornhole/Carnival",Human="Assets/LWS/TruckTaxi/Passengers/Animations/Humanoid/",Controller=Folder+"/Yard humanoid reactions.controller";
  const int Shared=CornholeSharedTarget.SharedLayer,Live=CornholeSharedTarget.TruckLayer;
  [MenuItem("Truck Sports/Cornhole/Install shared board and miniature garden in open scene")]
  public static void InstallOpenScene()
  {
   var match=UnityEngine.Object.FindAnyObjectByType<CornholeMatch>();if(match==null||match.gameObject.scene.path!=CornholeAuthoring.Scene)throw new InvalidOperationException("Open the existing Cornhole scene; this entry never opens or saves scenes.");
   Directory.CreateDirectory(Folder);AssetDatabase.Refresh();PrepareController();Install(match);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(match.gameObject.scene);
   Debug.Log("CORNHOLE_CARNIVAL_READY one original open board/catch; four fan approaches; 20 art units per fictional metre; physics/wind units unchanged; explicit caller save required.");
  }
  static AnimationClip HumanClip(string name)
  {
   var source=AssetDatabase.LoadAllAssetsAtPath(Human+name+".FBX").OfType<AnimationClip>().FirstOrDefault(clip=>!clip.name.StartsWith("__")&&clip.humanMotion&&clip.length>.1f);if(source==null)throw new InvalidOperationException("Existing verified Taxi Human clip required: "+name);
   string path=Folder+"/Yard "+name+".anim";var result=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);if(result==null){result=UnityEngine.Object.Instantiate(source);result.name="Yard "+name;var settings=AnimationUtility.GetAnimationClipSettings(result);settings.loopTime=true;AnimationUtility.SetAnimationClipSettings(result,settings);AssetDatabase.CreateAsset(result,path);}if(!result.humanMotion)throw new InvalidOperationException("Owned yard clip lost its Human curves: "+path);return result;
  }
  static void PrepareController()
  {
   var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Controller);if(controller==null)controller=AnimatorController.CreateAnimatorControllerAtPath(Controller);if(controller.layers.Length==0)controller.AddLayer("Base Layer");var layers=controller.layers;if(layers[0].stateMachine==null){layers[0].stateMachine=new AnimatorStateMachine{name="Base Layer"};AssetDatabase.AddObjectToAsset(layers[0].stateMachine,controller);}if(layers[0].stateMachine.states.Length==0)layers[0].defaultWeight=1;controller.layers=layers;var machine=controller.layers[0].stateMachine;
   foreach(var pair in new[]{("Idle","idle1"),("Walk","walk"),("Cheer","cheer"),("React","shout")}){var state=machine.states.FirstOrDefault(item=>item.state!=null&&item.state.name==pair.Item1).state;if(state==null){state=controller.AddMotion(HumanClip(pair.Item2));state.name=pair.Item1;}else if(state.motion==null)state.motion=HumanClip(pair.Item2);if(!(state.motion is AnimationClip clip)||!clip.humanMotion)throw new InvalidOperationException("Preserved nonempty yard state is not a Human clip: "+pair.Item1);if(machine.defaultState==null&&pair.Item1=="Idle")machine.defaultState=state;EditorUtility.SetDirty(state);}
   EditorUtility.SetDirty(machine);EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();Debug.Log("CORNHOLE_YARD_HUMAN_CONTROLLER_READY states="+machine.states.Length+" persistedBeforeVenue=true");
  }
  static Avatar AvatarAt(string path){var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(model==null)throw new InvalidOperationException(path);var animator=model.GetComponent<Animator>();if(animator==null||animator.avatar==null||!animator.avatar.isValid||!animator.avatar.isHuman)throw new InvalidOperationException("Verified Taxi Human avatar required: "+path);return animator.avatar;}
  static Material MaterialCopy(string name,string source,Vector2 tile)
  {
   string path=Folder+"/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material!=null)return material;var original=AssetDatabase.LoadAssetAtPath<Material>(source);if(original==null)throw new InvalidOperationException("Owned textured material required: "+source);material=new Material(original){name=name,mainTextureScale=tile,enableInstancing=true};AssetDatabase.CreateAsset(material,path);return material;
  }
  // Build render data directly; never instantiate UTS/NWH live scripts, colliders
  // or ragdolls and hope to remove them after their Awake side effects.
  static GameObject RenderCopy(Transform source,Transform parent,bool animate=false)
  {
   var map=new Dictionary<Transform,Transform>();
   Transform Copy(Transform original,Transform owner){var copy=new GameObject(original.name).transform;copy.SetParent(owner,false);copy.localPosition=original.localPosition;copy.localRotation=original.localRotation;copy.localScale=original.localScale;copy.gameObject.layer=Shared;copy.gameObject.SetActive(original.gameObject.activeSelf);map.Add(original,copy);foreach(Transform child in original)Copy(child,copy);return copy;}
   var root=Copy(source,parent);root.gameObject.SetActive(false);
   foreach(var pair in map){var filter=pair.Key.GetComponent<MeshFilter>();if(filter!=null)pair.Value.gameObject.AddComponent<MeshFilter>().sharedMesh=filter.sharedMesh;var renderer=pair.Key.GetComponent<Renderer>();Renderer target=null;
    if(renderer!=null&&renderer is SkinnedMeshRenderer skin){var replacement=pair.Value.gameObject.AddComponent<SkinnedMeshRenderer>();replacement.sharedMesh=skin.sharedMesh;replacement.bones=skin.bones.Select(bone=>bone!=null&&map.TryGetValue(bone,out var copy)?copy:null).ToArray();replacement.rootBone=skin.rootBone!=null&&map.TryGetValue(skin.rootBone,out var mapped)?mapped:null;replacement.localBounds=skin.localBounds;replacement.updateWhenOffscreen=true;target=replacement;}else if(renderer!=null&&renderer is MeshRenderer)target=pair.Value.gameObject.AddComponent<MeshRenderer>();
    if(target!=null){target.sharedMaterials=renderer.sharedMaterials;target.enabled=renderer.enabled;target.shadowCastingMode=ShadowCastingMode.On;target.receiveShadows=true;}
    var actor=pair.Key.GetComponent<Animator>();if(animate&&actor!=null){var replacement=pair.Value.gameObject.AddComponent<Animator>();replacement.avatar=actor.avatar;replacement.applyRootMotion=false;replacement.cullingMode=AnimatorCullingMode.AlwaysAnimate;}
   }return root.gameObject;
  }
  static GameObject Prop(string path,Transform owner,Vector3 position,float yaw,float scale=20)
  {
   var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(source==null)throw new InvalidOperationException("Installed garden art required: "+path);var result=RenderCopy(source.transform,owner);result.transform.position=position;result.transform.rotation=Quaternion.Euler(0,yaw,0);result.transform.localScale*=scale;result.SetActive(true);return result;
  }
  static void VisualLayer(Transform root,int layer)
  {
   foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))
   {
    // Preserve physics layers even if a future art upgrade puts a render mesh
    // directly on a chassis or physical surface.
    var filter=renderer.GetComponent<MeshFilter>();if(renderer.GetComponent<Collider>()!=null&&renderer is MeshRenderer&&filter!=null){var visual=new GameObject(renderer.name+" shared visual",typeof(MeshFilter),typeof(MeshRenderer));visual.transform.SetParent(renderer.transform,false);visual.layer=layer;visual.GetComponent<MeshFilter>().sharedMesh=filter.sharedMesh;var copy=visual.GetComponent<MeshRenderer>();copy.sharedMaterials=renderer.sharedMaterials;copy.enabled=renderer.enabled;copy.shadowCastingMode=renderer.shadowCastingMode;UnityEngine.Object.DestroyImmediate(renderer);UnityEngine.Object.DestroyImmediate(filter);}else renderer.gameObject.layer=layer;
   }
  }
  public static CornholeYardPresentation Install(CornholeMatch match)
  {
   if(match==null||match.courses.Length!=4)throw new InvalidOperationException("Four original course owners required.");
   var controller=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Controller);if(controller==null||controller.animationClips.Length<4)throw new InvalidOperationException("Run explicit author asset preparation before integration.");
   var target=match.GetComponentInChildren<CornholeSharedTarget>(true);
   if(target==null)
   {
    var root=new GameObject("One original shared opposing board and catch");root.transform.SetParent(match.transform,false);target=root.AddComponent<CornholeSharedTarget>();target.match=match;
    var original=match.courses[0];target.boardSurface=original.board.GetComponent<CornholeSurface>();target.groundSurface=original.GetComponentsInChildren<CornholeSurface>(true).Single(surface=>surface.kind==CornholeSurfaceKind.Ground);
    original.board.SetParent(root.transform,true);target.groundSurface.transform.SetParent(root.transform,true);target.groundSurface.transform.localScale=new Vector3(96,1,170);
    foreach(Transform child in original.transform.Cast<Transform>().Where(child=>child.name=="Board leg").ToArray())child.SetParent(root.transform,true);
   }
   var board=target.boardSurface.transform;var retired=match.transform.Find("Superseded separate target art · inactive");if(retired==null){retired=new GameObject("Superseded separate target art · inactive").transform;retired.SetParent(match.transform,false);retired.gameObject.SetActive(false);}
   for(int i=0;i<4;i++)
   {
    var course=match.courses[i];if(course.board!=board){course.board.SetParent(retired,true);foreach(var surface in course.GetComponentsInChildren<CornholeSurface>(true).Where(surface=>surface.kind==CornholeSurfaceKind.Ground).ToArray())surface.transform.SetParent(retired,true);foreach(Transform child in course.transform.Cast<Transform>().Where(child=>child.name=="Board leg").ToArray())child.SetParent(retired,true);}
    foreach(var surface in course.GetComponentsInChildren<CornholeSurface>(true).Where(surface=>surface.kind==CornholeSurfaceKind.Barrier).ToArray())surface.gameObject.SetActive(false);
    course.sharedTarget=target;course.board=board;course.approachFrame=course.transform;var rotation=Quaternion.Euler(0,-18+12*i,0);course.transform.SetPositionAndRotation(new Vector3(board.position.x,0,board.position.z)-rotation*new Vector3(0,0,64),rotation);course.center=course.transform.position.x;
    course.holeEffect.transform.SetParent(target.transform,true);course.holeEffect.transform.position=board.TransformPoint(new Vector3(0,2,course.holeZ));
    VisualLayer(course.transform,Shared);foreach(var truck in course.trucks)VisualLayer(truck.transform,Live);
    // Private cab + that owner's replay layer; all live opponents and garden
    // are shared. Keep any existing weather layers outside the reserved range.
    foreach(var renderer in course.chase.interior)if(renderer!=null)renderer.gameObject.layer=CornholeSharedTarget.PrivateLayerStart+i;
    int privateMask=15<<CornholeSharedTarget.PrivateLayerStart;course.chase.view.cullingMask=(course.chase.view.cullingMask&~privateMask)|(1<<(CornholeSharedTarget.PrivateLayerStart+i))|(1<<Shared)|(1<<Live);course.chase.view.farClipPlane=Mathf.Max(420,course.chase.view.farClipPlane);
    EditorUtility.SetDirty(course);EditorUtility.SetDirty(course.chase.view);
   }
   VisualLayer(target.transform,Shared);
   var wood=MaterialCopy("Miniature garden warm timber","Assets/TruckFishing/Materials/Pier warm hardwood.mat",new Vector2(1,2));foreach(var renderer in board.GetComponentsInChildren<MeshRenderer>(true))if(renderer.name.Contains("visible surface")||renderer.gameObject==board.gameObject)renderer.sharedMaterial=wood;
   foreach(var label in board.GetComponentsInChildren<TMP_Text>(true))label.text="TRUCK CORNHOLE\nTHE SHARED GARDEN BOARD";
   var venue=match.GetComponentInChildren<CornholeYardPresentation>(true);if(venue==null)venue=BuildYard(match,controller);var grass=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Miniature garden textured lawn.mat");foreach(var renderer in target.groundSurface.GetComponentsInChildren<MeshRenderer>(true))renderer.sharedMaterial=grass;
   foreach(var text in match.menus.pages[0].GetComponentsInChildren<TMP_Text>(true))if(text.text.Contains("Separate simultaneous courses"))text.text=text.text.Replace("Separate simultaneous courses","Four approaches · One shared opposing board");
   EditorUtility.SetDirty(target);EditorUtility.SetDirty(venue);return venue;
  }
  static CornholeYardPresentation BuildYard(CornholeMatch match,RuntimeAnimatorController controller)
  {
   var root=new GameObject("Miniature garden · 20 art units per fictional metre");root.transform.SetParent(match.transform,false);try {var venue=root.AddComponent<CornholeYardPresentation>();venue.match=match;
   var turf=MaterialCopy("Miniature garden textured lawn","Assets/TruckBuzkashi/Materials/Buzkashi grass.mat",new Vector2(28,24));
   var lawn=GameObject.CreatePrimitive(PrimitiveType.Plane);lawn.name="Textured garden lawn · visual only";lawn.transform.SetParent(root.transform,false);lawn.layer=Shared;lawn.transform.position=new Vector3(0,-10.015f,35);lawn.transform.localScale=new Vector3(40,1,34);UnityEngine.Object.DestroyImmediate(lawn.GetComponent<Collider>());lawn.GetComponent<Renderer>().sharedMaterial=turf;
   var props=new List<Transform>();
   for(int side=-1;side<=1;side+=2)
   {
    for(int n=0;n<5;n++)props.Add(Prop("Assets/LWS/TruckTaxi/Authoring/Dressing/Prefabs/Taxi_FenceA.prefab",root.transform,new Vector3(side*185,-10,-105+n*68),90).transform);
    for(int n=0;n<3;n++){var table=Prop("Assets/Modern Resaturant - building and interior/Models/Prefabs/Restaurant_Table_Rectangular_1.prefab",root.transform,new Vector3(side*132,-10,-38+n*75),side*90);props.Add(table.transform);for(int seat=-1;seat<=1;seat+=2)props.Add(Prop("Assets/Modern Resaturant - building and interior/Models/Prefabs/Restaurant_chair.prefab",root.transform,new Vector3(side*132+seat*22,-10,-38+n*75),seat*90).transform);}
    props.Add(Prop("Assets/RoadPropsPack/URP/Prefabs/WoodBox_URP.prefab",root.transform,new Vector3(side*155,-10,148),side*15).transform);
    props.Add(Prop("Assets/Modern Resaturant - building and interior/Models/Prefabs/Street_Lamp.prefab",root.transform,new Vector3(side*165,-10,25),0).transform);
   }
   for(int n=0;n<28;n++){float angle=n*Mathf.PI*2/28;props.Add(Prop("Assets/JC_LP_MegaCity/Prefabs/Nature/SM_Nature_Grass_01.prefab",root.transform,new Vector3(Mathf.Cos(angle)*174,-10,35+Mathf.Sin(angle)*145),n*47,20).transform);}
   var banner=Prop("Assets/NWH/WheelController/DemoContent/_Scenes/WC3D Main Demo/Props/Banner/BannerStart.prefab",root.transform,new Vector3(0,-10,-105),0,20);props.Add(banner.transform);
   var title=new GameObject("Garden match signage").AddComponent<TextMeshPro>();title.transform.SetParent(root.transform,false);title.gameObject.layer=Shared;title.transform.position=new Vector3(0,45,-107);title.transform.rotation=Quaternion.Euler(0,180,0);title.font=TMP_Settings.defaultFontAsset;title.fontSize=13;title.alignment=TextAlignmentOptions.Center;title.rectTransform.sizeDelta=new Vector2(180,24);title.text="THE MINIATURE HAULERS\nGARDEN CORNHOLE";
   var actors=new List<Animator>();var avatars=new[]{AvatarAt(Human+"business01_m.FBX"),AvatarAt(Human+"business01_f.FBX")};var models=new[]{"Assets/UTS_FullPack/Models/People/Prefabs/Mans/business01_m.prefab","Assets/UTS_FullPack/Models/People/Prefabs/Womans/business01_f_highpoly.prefab"};
   for(int n=0;n<32;n++)
   {
    int variant=n%2;var source=AssetDatabase.LoadAssetAtPath<GameObject>(models[variant]);if(source==null)throw new InvalidOperationException(models[variant]);var person=RenderCopy(source.transform,root.transform,true);person.name="Normal-proportion garden person "+n+" · 1.8 fictional metres";person.transform.SetPositionAndRotation(new Vector3((n%2==0?-1:1)*(n<8?69:92+(n/16)*18),-10,-70+(n/2)*14),Quaternion.Euler(0,n%2==0?90:-90,0));
    var animator=person.GetComponentInChildren<Animator>(true);if(animator==null)throw new InvalidOperationException("Genuine humanoid rig required.");animator.avatar=avatars[variant];animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;person.SetActive(true);
    var skins=person.GetComponentsInChildren<SkinnedMeshRenderer>(true);if(skins.Length==0||skins.Any(skin=>skin.bones.Length<10))throw new InvalidOperationException("Actual skinned Human spectator required.");var bounds=skins[0].bounds;foreach(var skin in skins)bounds.Encapsulate(skin.bounds);person.transform.localScale*=CornholeYardPresentation.HumanHeightMetres*CornholeYardPresentation.ArtUnitsPerMetre/Mathf.Max(.001f,bounds.size.y);if(!animator.isHuman)throw new InvalidOperationException("Mapped garden spectator is not Human.");actors.Add(animator);
   }
   var lights=new List<Light>();for(int side=-1;side<=1;side+=2){var light=new GameObject("Warm garden party light "+side).AddComponent<Light>();light.transform.SetParent(root.transform,false);light.transform.position=new Vector3(side*140,70,30);light.type=LightType.Point;light.color=new Color(1,.78f,.48f);light.intensity=20;light.range=165;light.shadows=LightShadows.None;lights.Add(light);}
   venue.spectators=actors.ToArray();venue.yardProps=props.ToArray();venue.yardLights=lights.ToArray();return venue;
   }catch{UnityEngine.Object.DestroyImmediate(root);throw;}
  }
 }
}
