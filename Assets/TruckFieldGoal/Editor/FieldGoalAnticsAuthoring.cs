using System.Linq;
using UnityEditor;
using UnityEngine;
using LWS.TruckSports.Editor;
using LWS.TruckTaxi;
using LWS.TruckPedestrianPlowing;

namespace LWS.TruckFieldGoal.Editor {
public static class FieldGoalAnticsAuthoring {
 public static void Install(FieldGoalCrowd crowd,Transform root,RuntimeAnimatorController controller){
  var old=crowd.GetComponent<FieldGoalStadiumAntics>();if(old!=null)Object.DestroyImmediate(old);
  var antics=crowd.gameObject.AddComponent<FieldGoalStadiumAntics>();antics.match=crowd.match;antics.crowd=crowd;
  antics.jugs=new Transform[8];var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RoadPropsPack/URP/Prefabs/Canister_URP.prefab");if(source==null)throw new System.InvalidOperationException("Owned RoadProps canister asset is required.");
  var amber=AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckFareFight/Materials/Reusable Taxi jug translucent amber.mat");
  for(int i=0;i<antics.jugs.Length;i++){
   var jug=(GameObject)PrefabUtility.InstantiatePrefab(source,root);jug.name="Pooled sealed stadium jug "+i;foreach(var c in jug.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(c);foreach(var b in jug.GetComponentsInChildren<Rigidbody>(true))Object.DestroyImmediate(b);
   foreach(var r in jug.GetComponentsInChildren<Renderer>(true)){r.sharedMaterial=amber;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;}
   var bounds=new Bounds(jug.transform.position,Vector3.zero);foreach(var r in jug.GetComponentsInChildren<Renderer>())bounds.Encapsulate(r.bounds);jug.transform.localScale*=.8f/Mathf.Max(.01f,bounds.size.y);antics.jugs[i]=jug.transform;jug.SetActive(false);
  }
  var character=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TruckPedestrianPlowing/Prefabs/glam-fire-fighter Parade.prefab");
  var animator=SportsAudienceAuthoring.Spawn(character,root,new Vector3(35,0,24),Vector3.zero,controller,1.25f,true);
  animator.runtimeAnimatorController=controller;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;antics.invader=animator.GetComponentInParent<TruckTaxiPedestrian>();if(antics.invader==null||antics.invader.GetComponentsInChildren<Joint>(true).Length==0)throw new System.InvalidOperationException("The actual shared Taxi jointed ragdoll is required.");
  antics.invaderAnimator=animator;antics.invader.name="Pooled pitch invader · existing Taxi jointed ragdoll";antics.invader.settings.maximumRagdollImpulse=360;antics.invader.settings.ragdollLifetime=30;antics.invader.gameObject.SetActive(false);
  var truck=new GameObject("Referee truck · staged non-scoring stadium vehicle").transform;truck.SetParent(root,false);var model=Object.Instantiate(crowd.match.truck.visuals,truck,false);model.localPosition=crowd.match.truck.visuals.localPosition;model.localRotation=crowd.match.truck.visuals.localRotation;model.gameObject.SetActive(true);
  foreach(var script in model.GetComponentsInChildren<MonoBehaviour>(true))Object.DestroyImmediate(script);foreach(var c in model.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(c);foreach(var b in model.GetComponentsInChildren<Rigidbody>(true))Object.DestroyImmediate(b);
  string stripePath="Assets/TruckFieldGoal/Crowd/Referee cab stripes.asset";var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(stripePath);if(texture==null){texture=new Texture2D(64,8,TextureFormat.RGBA32,false);for(int x=0;x<64;x++)for(int y=0;y<8;y++)texture.SetPixel(x,y,x/8%2==0?new Color(.94f,.94f,.88f):new Color(.018f,.022f,.027f));texture.Apply();texture.wrapMode=TextureWrapMode.Repeat;AssetDatabase.CreateAsset(texture,stripePath);}
  var stripes=SportsEventAuthoring.Mat("TruckFieldGoal","Referee black and white cab stripes",Color.white,.18f);stripes.mainTexture=texture;stripes.mainTextureScale=new Vector2(2,1);EditorUtility.SetDirty(stripes);
  // Dress the same production truck cab, preserving wheels, lamps and glass.
  foreach(var r in model.GetComponentsInChildren<MeshRenderer>(true)){var materials=r.sharedMaterials;bool changed=false;for(int i=0;i<materials.Length;i++)if(materials[i]!=null&&(materials[i].name.ToLowerInvariant().Contains("paint")||materials[i].name.ToLowerInvariant().Contains("body")||materials[i].name.ToLowerInvariant().Contains("cab"))){materials[i]=stripes;changed=true;}if(changed)r.sharedMaterials=materials;}
  var badge=new GameObject("Roof referee stripe jersey",typeof(MeshFilter),typeof(MeshRenderer));badge.transform.SetParent(truck,false);var boundsTruck=new Bounds();bool first=true;foreach(var r in model.GetComponentsInChildren<Renderer>(true)){if(first){boundsTruck=r.bounds;first=false;}else boundsTruck.Encapsulate(r.bounds);}
  var cap=GameObject.CreatePrimitive(PrimitiveType.Cylinder);cap.name="Oversized referee cap";cap.transform.SetParent(truck,false);cap.transform.position=new Vector3(boundsTruck.center.x,boundsTruck.max.y+.12f,boundsTruck.max.z-1.5f);cap.transform.localScale=new Vector3(2.6f,.1f,2.6f);Object.DestroyImmediate(cap.GetComponent<Collider>());cap.GetComponent<Renderer>().sharedMaterial=stripes;Object.DestroyImmediate(badge);
  antics.referee=truck;truck.gameObject.SetActive(false);
  var burst=Object.Instantiate(crowd.match.kickBurst,root);burst.name="Pooled referee tackle dust · no damage";burst.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var main=burst.main;main.loop=false;main.startColor=new Color(.94f,.9f,.62f,.7f);antics.tackleBurst=burst;
  EditorUtility.SetDirty(antics);
 }
}
}
