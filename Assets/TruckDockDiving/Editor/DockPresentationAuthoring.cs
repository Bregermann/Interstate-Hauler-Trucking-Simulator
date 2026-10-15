using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using LWS.TruckSports.Editor;
namespace LWS.TruckDockDiving.Editor {
public static class DockPresentationAuthoring {
 public static void Install(DockMatch match){
  var water=GameObject.Find("NWH water plane · URP pool surface").GetComponent<Renderer>();var material=water.sharedMaterial;var bump=AssetDatabase.LoadAssetAtPath<Texture>("Assets/WeatherMaker/Prefab/Textures/Water/WeatherMakerWaterBump2.jpg");material.SetTexture("_BumpMap",bump);material.SetFloat("_BumpScale",.32f);material.EnableKeyword("_NORMALMAP");material.SetTextureScale("_BumpMap",new Vector2(8,35));EditorUtility.SetDirty(material);
  var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UTS_FullPack/Models/People/RagDoll Prefabs/Man_11.prefab");var clip=AssetDatabase.LoadAllAssetsAtPath("Assets/UTS_FullPack/Models/People/Animations/man/man_claphands.FBX").OfType<AnimationClip>().First(c=>!c.name.StartsWith("__"));string path="Assets/TruckDockDiving/Materials/Spectator applause.controller";var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);if(controller==null){controller=AnimatorController.CreateAnimatorControllerAtPath(path);controller.AddMotion(clip);}
  var crowd=new GameObject("UTS spectator crowd · render and animation only");for(int side=-1;side<=1;side+=2)for(int row=0;row<2;row++)for(int seat=0;seat<12;seat++){
   var person=UnityEngine.Object.Instantiate(source,crowd.transform);person.name="Applauding spectator";person.transform.SetPositionAndRotation(new Vector3(side*(19.8f+row*1.8f),.7f+row*.7f,5+seat*6),Quaternion.Euler(0,side<0?90:-90,0));person.transform.localScale=Vector3.one*1.1f;
   foreach(var script in person.GetComponentsInChildren<MonoBehaviour>(true))UnityEngine.Object.DestroyImmediate(script);foreach(var joint in person.GetComponentsInChildren<Joint>(true))UnityEngine.Object.DestroyImmediate(joint);foreach(var c in person.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);foreach(var body in person.GetComponentsInChildren<Rigidbody>(true))UnityEngine.Object.DestroyImmediate(body);
   var animator=person.GetComponentInChildren<Animator>(true);if(animator!=null){animator.enabled=true;animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.CullCompletely;}
   foreach(var renderer in person.GetComponentsInChildren<Renderer>(true)){var materials=renderer.sharedMaterials;for(int i=0;i<materials.Length;i++){var original=materials[i];if(original==null)continue;var owned=SportsEventAuthoring.Mat("TruckDockDiving","Spectator "+original.name,Color.white);if(original.HasProperty("_MainTex"))owned.mainTexture=original.mainTexture;materials[i]=owned;}renderer.sharedMaterials=materials;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;}
  }
 }
}
}
