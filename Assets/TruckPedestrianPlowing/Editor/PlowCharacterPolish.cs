using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace LWS.TruckPedestrianPlowing.Editor {
 public static class PlowCharacterPolish {
  public static void Install(){string path=PlowAuthoring.Root+"/Configuration/Parade Actions.overrideController";var controller=AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(path);if(controller==null){var source=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/UTS_FullPack/Models/People/Animations/Girl no Heel controller.controller");controller=new AnimatorOverrideController(source);var dance=AssetDatabase.LoadAllAssetsAtPath("Assets/UTS_FullPack/Models/People/Animations/Girl no Heel/dance_f.FBX").OfType<AnimationClip>().First(a=>!a.name.StartsWith("__"));var pairs=new List<KeyValuePair<AnimationClip,AnimationClip>>();controller.GetOverrides(pairs);for(int i=0;i<pairs.Count;i++)if(pairs[i].Key.name=="cheer_f")pairs[i]=new KeyValuePair<AnimationClip,AnimationClip>(pairs[i].Key,dance);controller.ApplyOverrides(pairs);AssetDatabase.CreateAsset(controller,path);}
   foreach(var id in AssetDatabase.FindAssets("t:Prefab",new[]{PlowAuthoring.Root+"/Prefabs"})){string prefab=AssetDatabase.GUIDToAssetPath(id);var root=PrefabUtility.LoadPrefabContents(prefab);try{var ped=root.GetComponent<PlowPedestrian>();if(ped==null)continue;ped.animator.runtimeAnimatorController=controller;ped.animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;foreach(var r in root.GetComponentsInChildren<MeshRenderer>(true)){string n=r.name.ToLowerInvariant();r.shadowCastingMode=n=="body"||n=="head"?UnityEngine.Rendering.ShadowCastingMode.On:UnityEngine.Rendering.ShadowCastingMode.Off;}PlowCrowdLodAuthoring.Apply(ped);PrefabUtility.SaveAsPrefabAsset(root,prefab);}finally{PrefabUtility.UnloadPrefabContents(root);}}AssetDatabase.SaveAssets();Debug.Log("PLOW_CHARACTER_ACTIONS_SAVED vendor dance/idle/talk/walk/run, owned overrides only");}
 }
}
