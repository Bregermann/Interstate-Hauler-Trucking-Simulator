using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using LWS.TruckSports.Editor;
namespace LWS.TruckPoker.Editor {
// Explicit additive upgrade using already-owned/public restaurant assets.
// The central table and dealer's x16/z13 circuit remain unobstructed.
public static class PokerRestaurantDressing {
 const string Models="Assets/Modern Resaturant - building and interior/Models/Prefabs/";
 public static void Upgrade(){var scene=EditorSceneManager.OpenScene(PokerAuthoring.Scene);var match=Object.FindAnyObjectByType<PokerMatch>();Install(match);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();}
 public static void Install(PokerMatch match){
  var venue=match.transform.Find("Modeled midnight casino interior");if(venue==null)throw new System.InvalidOperationException("Upgrade the saved restaurant shell first.");
  if(venue.Find("Restaurant dining and service")!=null){PokerRestaurantSurfaceAuthoring.Install(match);return;}
  var root=new GameObject("Restaurant dining and service").transform;root.SetParent(venue,false);
  var clip=AssetDatabase.LoadAllAssetsAtPath("Assets/UTS_FullPack/Models/People/Animations/man/eatsitting.FBX").OfType<AnimationClip>().First(c=>!c.name.StartsWith("__"));
  const string controllerPath="Assets/TruckPoker/Materials/Restaurant diners.controller";
  var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);if(controller==null){controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);controller.AddMotion(clip);}
  foreach(int side in new[]{-1,1})foreach(int row in new[]{-1,1}){
   var center=new Vector3(side*21,0,row*6.5f);
   Model(root,"Owned restaurant dining table","Restaurant_Table_Rectangular_1.prefab",center,new Vector3(3.2f,1.2f,2.2f));
   Model(root,"Pizza on dining table","Pizza.prefab",center+new Vector3(.5f,1.22f,0),new Vector3(.65f,.13f,.65f));
   Model(root,"Burger on dining table","Burger.prefab",center+new Vector3(-.5f,1.22f,0),new Vector3(.38f,.3f,.38f));
   Model(root,"Dining wine bottle","Wine_Bottel.prefab",center+new Vector3(0,1.22f,.45f),new Vector3(.18f,.55f,.18f));
   foreach(int seat in new[]{-1,1}){
    var position=center+new Vector3(0,0,seat*1.8f);float yaw=seat>0?180:0;
    Model(root,"Owned dining chair","Restaurant_chair.prefab",position,new Vector3(.9f,1.2f,.9f),yaw);
    var diner=new GameObject("Animated restaurant guest").transform;diner.SetParent(root,false);diner.SetPositionAndRotation(position+Vector3.down*.3f,Quaternion.Euler(0,yaw,0));
    LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(diner,"Assets/UTS_FullPack/Models/People/RagDoll Prefabs/Man_11.prefab",new Vector3(.7f,1.8f,.7f),0,false,"TruckPoker");
    var animator=diner.GetComponentInChildren<Animator>(true);if(animator!=null){animator.enabled=true;animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.CullCompletely;}
   }
  }
  for(int i=-1;i<=1;i++)Model(root,"Owned restaurant service oven","kitchen_module_oven.prefab",new Vector3(i*3.2f,0,21.4f),new Vector3(2.8f,2.1f,1.4f),180);
  PokerRestaurantSurfaceAuthoring.Install(match);EditorUtility.SetDirty(match);
 }
 static void Model(Transform parent,string name,string file,Vector3 position,Vector3 size,float yaw=0){var root=new GameObject(name).transform;root.SetParent(parent,false);root.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(root,Models+file,size,0,false,"TruckPoker");}
}
}
