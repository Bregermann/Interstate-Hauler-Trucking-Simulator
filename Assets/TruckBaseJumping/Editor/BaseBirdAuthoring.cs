using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

namespace LWS.TruckBaseJumping.Editor
{
 // Explicit only: no AssetPostprocessor, startup initializer, scene rebuild or
 // automatic build regeneration. Original CC0 FBX bytes remain unchanged.
 public static class BaseBirdAuthoring
 {
  const string Folder="Assets/TruckBaseJumping/ThirdParty/QuaterniusBirds";
  [Serializable]sealed class ClipRecord{public string name;public float seconds;public int transformCurves;}
  [Serializable]sealed class ModelRecord{public string source,sha256,selectedClip;public bool genericAvatar;public int skinnedRenderers,bones;public ClipRecord[] clips;}
  [Serializable]sealed class ImportRecord{public string artist="Quaternius",license="CC0-1.0; original upload URLs and archive hashes in SOURCE_AND_LICENSE.md";public ModelRecord[] models;}
  [MenuItem("Truck Sports/BASE Jumping/Import CC0 animated birds and install bounded flock in open scene")]
  public static void InstallOpenScene()
  {
   var match=UnityEngine.Object.FindAnyObjectByType<BaseJumpMatch>();if(match==null||match.gameObject.scene.path!="Assets/TruckBaseJumping/Scenes/TruckBaseJumping.unity")throw new InvalidOperationException("Open the existing Base Jumping scene first.");
   var records=new List<ModelRecord>();Prepare("Eagle.fbx","Eagle","Flying",2.8f,records);Prepare("bird.fbx","Small bird","ArmatureAction",1.6f,records);File.WriteAllText(Folder+"/ImportReport.json",JsonUtility.ToJson(new ImportRecord{models=records.ToArray()},true));Install(match,true);EditorSceneManager.MarkSceneDirty(match.gameObject.scene);
  }
  static Material Palette(string label,string original)
  {
   string name=label+" "+original.Replace('/','_').Replace('|','_');string path=Folder+"/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material!=null)return material;
   Color color=label=="Eagle"?(original.IndexOf("Beak",StringComparison.OrdinalIgnoreCase)>=0?new Color(.94f,.62f,.12f):original.IndexOf("Head",StringComparison.OrdinalIgnoreCase)>=0?new Color(.94f,.91f,.83f):original.IndexOf("Claw",StringComparison.OrdinalIgnoreCase)>=0?new Color(.1f,.08f,.06f):new Color(.25f,.15f,.08f)):(original.Contains("002")?new Color(.18f,.37f,.54f):new Color(.83f,.78f,.64f));
   material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};material.SetColor("_BaseColor",color);material.SetFloat("_Smoothness",.18f);material.enableInstancing=true;AssetDatabase.CreateAsset(material,path);return material;
  }
  static AnimatorController FlightController(string path,AnimationClip flight)
  {
   var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);if(controller==null)controller=AnimatorController.CreateAnimatorControllerAtPath(path);if(controller.layers.Length==0)controller.AddLayer("Base Layer");var layers=controller.layers;if(layers[0].stateMachine==null){var machine=new AnimatorStateMachine{name="Base Layer"};AssetDatabase.AddObjectToAsset(machine,controller);layers[0].stateMachine=machine;}if(layers[0].stateMachine.states.Length==0)layers[0].defaultWeight=1;controller.layers=layers;
   var states=controller.layers[0].stateMachine;var state=states.states.FirstOrDefault(item=>item.state!=null&&item.state.name=="Fly").state;if(state==null){state=controller.AddMotion(flight);state.name="Fly";}else if(state.motion==null)state.motion=flight;if(states.defaultState==null)states.defaultState=state;
   var motion=state.motion as AnimationClip;if(motion==null||motion.humanMotion||motion.length<=.2f||AnimationUtility.GetCurveBindings(motion).Count(binding=>binding.type==typeof(Transform))<3)throw new InvalidOperationException("Original Generic bird Fly motion required; existing nonempty custom motion was retained for review.");
   // Persist the layer AND its state-machine/state subassets before a later
   // model/prefab step can fail. File existence alone never proves readiness.
   EditorUtility.SetDirty(state);EditorUtility.SetDirty(states);EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();return controller;
  }
  static void Prepare(string filename,string label,string take,float span,List<ModelRecord> records)
  {
   string path=Folder+"/"+filename;AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var importer=AssetImporter.GetAtPath(path) as ModelImporter;if(importer==null)throw new InvalidOperationException("Reviewed original bird FBX required: "+path);
   importer.importAnimation=true;importer.animationType=ModelImporterAnimationType.Generic;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;importer.importCameras=false;importer.importLights=false;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
   var definitions=importer.defaultClipAnimations;foreach(var definition in definitions){definition.loopTime=true;definition.lockRootRotation=true;definition.lockRootHeightY=true;definition.lockRootPositionXZ=true;}importer.clipAnimations=definitions;importer.SaveAndReimport();
   var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(model==null)throw new InvalidOperationException("Bird model failed to import: "+path);var animator=model.GetComponentInChildren<Animator>(true);if(animator==null||animator.avatar==null||!animator.avatar.isValid||animator.avatar.isHuman)throw new InvalidOperationException("A real Generic bird avatar is required: "+path);
   var skins=model.GetComponentsInChildren<SkinnedMeshRenderer>(true);if(skins.Length==0||skins.Sum(skin=>skin.bones.Length)<5)throw new InvalidOperationException("Original skinned bird bones are missing: "+path);
   var clips=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(clip=>!clip.name.StartsWith("__")).ToArray();var flight=clips.FirstOrDefault(clip=>clip.name.IndexOf(take,StringComparison.OrdinalIgnoreCase)>=0&&clip.length>.2f);if(flight==null)throw new InvalidOperationException("Original flight take unavailable. Actual imported clips: "+string.Join(",",clips.Select(clip=>clip.name)));
   int curves=AnimationUtility.GetCurveBindings(flight).Count(binding=>binding.type==typeof(Transform));if(curves<3)throw new InvalidOperationException("Selected bird take has no actual transform animation: "+flight.name);
   string hash;using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();records.Add(new ModelRecord{source=path,sha256=hash,selectedClip=flight.name,genericAvatar=true,skinnedRenderers=skins.Length,bones=skins.Sum(skin=>skin.bones.Length),clips=clips.Select(clip=>new ClipRecord{name=clip.name,seconds=clip.length,transformCurves=AnimationUtility.GetCurveBindings(clip).Count(binding=>binding.type==typeof(Transform))}).ToArray()});Debug.Log("BASE_BIRD_IMPORTED "+label+" selected="+flight.name+" generic=true bones="+skins.Sum(skin=>skin.bones.Length)+" clips="+string.Join(",",clips.Select(clip=>clip.name+"("+clip.length+")")));
   string controllerPath=Folder+"/"+label+" flight.controller";var controller=FlightController(controllerPath,flight);
   string prefabPath=Folder+"/"+label+" flight.prefab";if(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath)!=null)return;
   var root=new GameObject(label+" - original animated Quaternius bird");try
   {
    var actor=root.AddComponent<BaseBirdActor>();actor.body=root.AddComponent<Rigidbody>();actor.shape=root.AddComponent<SphereCollider>();actor.shape.radius=label=="Eagle"?.22f:.16f;actor.shape.enabled=false;actor.shape.layerOverridePriority=100;var contact=AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(Folder+"/Soft bird contact.asset");if(contact==null){contact=new PhysicsMaterial("Soft bird contact"){staticFriction=0,dynamicFriction=0,bounciness=0,frictionCombine=PhysicsMaterialCombine.Minimum,bounceCombine=PhysicsMaterialCombine.Minimum};AssetDatabase.CreateAsset(contact,Folder+"/Soft bird contact.asset");}actor.shape.sharedMaterial=contact;
    var body=actor.body;body.mass=.04f;body.useGravity=false;body.linearDamping=.15f;body.angularDamping=1.8f;body.maxLinearVelocity=16;body.maxAngularVelocity=5;body.maxDepenetrationVelocity=.3f;body.interpolation=RigidbodyInterpolation.Interpolate;body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;body.isKinematic=true;
    var visual=(GameObject)PrefabUtility.InstantiatePrefab(model,root.transform);visual.name="Original Generic flight rig";var renderers=visual.GetComponentsInChildren<Renderer>(true);Bounds bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);visual.transform.localScale*=span/Mathf.Max(.001f,bounds.size.x,bounds.size.y,bounds.size.z);bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);visual.transform.localPosition-=bounds.center;
    foreach(var renderer in renderers){renderer.sharedMaterials=renderer.sharedMaterials.Select(material=>Palette(label,material!=null?material.name:"Body")).ToArray();renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;if(renderer is SkinnedMeshRenderer skin)skin.updateWhenOffscreen=true;}
    actor.animator=visual.GetComponentInChildren<Animator>(true);if(actor.animator==null)throw new InvalidOperationException("Imported bird has no Animator.");actor.animator.runtimeAnimatorController=controller;actor.animator.applyRootMotion=false;actor.animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;actor.animator.updateMode=AnimatorUpdateMode.Normal;PrefabUtility.SaveAsPrefabAsset(root,prefabPath);
   }
   finally{UnityEngine.Object.DestroyImmediate(root);}
  }
  public static BaseBirdFlock Install(BaseJumpMatch match,bool undo)
  {
   var existing=match.GetComponentInChildren<BaseBirdFlock>(true);if(existing!=null)return existing;var prefabs=new[]{AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Eagle flight.prefab"),AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Small bird flight.prefab")};if(prefabs.Any(prefab=>prefab==null))throw new InvalidOperationException("Run explicit BaseBirdAuthoring.InstallOpenScene to import original animation first.");
   var root=new GameObject("Bounded twelve original animated Quaternius birds");root.transform.SetParent(match.transform,false);var flock=root.AddComponent<BaseBirdFlock>();flock.match=match;flock.paths=new SplineContainer[3];var centers=new[]{new Vector3(-45,135,-30),new Vector3(50,190,20),new Vector3(-5,85,60)};
   for(int group=0;group<3;group++){var path=new GameObject("Closed real bird flight spline "+group).AddComponent<SplineContainer>();path.transform.SetParent(root.transform,false);var points=new Vector3[8];for(int i=0;i<points.Length;i++){float angle=i*Mathf.PI*2/points.Length;points[i]=centers[group]+new Vector3(Mathf.Cos(angle)*(35+group*7),Mathf.Sin(angle*2)*8,Mathf.Sin(angle)*(30+group*5));}path.Spline=new Spline(points.Select(point=>new BezierKnot((float3)point)),true);path.Spline.SetTangentMode(TangentMode.AutoSmooth);flock.paths[group]=path;}
   flock.birds=new BaseBirdActor[12];for(int i=0;i<12;i++){var bird=UnityEngine.Object.Instantiate(prefabs[i%2],root.transform,false).GetComponent<BaseBirdActor>();if(bird==null)throw new InvalidOperationException("Owned animated bird prefab has no actor.");bird.index=i;bird.flock=flock;bird.animationSpeed=.85f+i%4*.1f;flock.birds[i]=bird;}flock.Clear();if(undo)Undo.RegisterCreatedObjectUndo(root,"Install bounded animated CC0 birds");return flock;
  }
 }
}
