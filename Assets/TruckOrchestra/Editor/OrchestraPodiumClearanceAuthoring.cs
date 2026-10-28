using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace LWS.TruckOrchestra.Editor
{
 // V11 actual conductor body overlapped the timing-panel safety region by2.23px
 // at1080. Raise its feet and the actual podium top together by.35m; preserve
 // the podium bottom on the floor rather than floating the original plinth.
 public static class OrchestraPodiumClearanceAuthoring
 {
  const float Lift=.35f;
  static void Require(bool value,string reason){if(!value)throw new InvalidOperationException("Orchestra podium clearance: "+reason);}
  static bool Near(Vector3 a,Vector3 b)=>Vector3.Distance(a,b)<.0001f;
  static void ValidateInstalledPose(Transform actor,Transform podium)
  {
   Require(actor!=null&&podium!=null,"original installed conductor/podium required");
   Require(Near(actor.position,new Vector3(0,1.05f,-3))&&Near(actor.localScale,Vector3.one*2)&&Quaternion.Angle(actor.rotation,Quaternion.identity)<.001f,"custom/already-raised actor pose refused");
   Require(Near(podium.position,new Vector3(0,.5f,-3))&&Near(podium.localScale,new Vector3(5,1,4))&&Quaternion.Angle(podium.rotation,Quaternion.identity)<.001f,"custom/already-raised podium pose refused");
   Require(Near(podium.parent.lossyScale,Vector3.one)&&Quaternion.Angle(podium.parent.rotation,Quaternion.identity)<.001f,"unscaled original podium parent required");
   Require(actor.GetComponentsInChildren<Collider>(true).Length==0&&actor.GetComponentsInChildren<Rigidbody>(true).Length==0&&podium.GetComponentsInChildren<Collider>(true).Length==0&&podium.GetComponentsInChildren<Rigidbody>(true).Length==0,"only original render-only objects may move");
  }
  // Called only after the original complete new-presentation author preflight.
  internal static void ApplyNewPresentation(Transform actor,Transform podium)
  {
   actor.position=new Vector3(0,1.05f+Lift,-3);podium.position=new Vector3(0,.5f+Lift*.5f,-3);podium.localScale=new Vector3(5,1+Lift,4);
  }
  [MenuItem("Truck Sports/Impossible Games/Repair OPEN Orchestra/Installed V2 podium HUD clearance")]
  public static void InstallOnOpenOriginalScene()
  {
   Require(!EditorApplication.isPlaying&&!EditorApplication.isCompiling,"stopped compiled Editor required");
   var matches=UnityEngine.Object.FindObjectsByType<OrchestraMatch>(FindObjectsInactive.Include,FindObjectsSortMode.None);
   Require(matches.Length==1&&matches[0].gameObject.scene.path==OrchestraAuthoring.ScenePath,"exact original saved Orchestra scene required");
   var match=matches[0];var c=match.conductor;Require(c!=null&&c.actorSpace!=null,"original conductor binding required");
   var podium=match.gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Single(t=>t.name=="Ornate conductor podium");
   ValidateInstalledPose(c.actorSpace,podium);
   Require(c.idleFoundation!=null&&AssetDatabase.GetAssetPath(c.idleFoundation)=="Assets/UTS_FullPack/Models/People/Animations/Girl with Heel/idle1_f.FBX"&&!c.idleFoundation.humanMotion,"known installed V2 original Generic idle required");
   var animator=c.actorSpace.GetComponent<Animator>();Require(animator!=null&&!animator.enabled&&animator.runtimeAnimatorController==null&&animator.avatar!=null&&!animator.avatar.isHuman&&AssetDatabase.GetAssetPath(animator.avatar)=="Assets/UTS_FullPack/Models/People/Models/Girl/Girl_44.FBX","exact disabled original Generic rig required");
   Require(c.leftForearm!=null&&c.rightForearm!=null&&c.leftHand!=null&&c.rightHand!=null&&c.leftForearm.IsChildOf(c.leftArm)&&c.rightForearm.IsChildOf(c.rightArm)&&c.leftHand.IsChildOf(c.leftForearm)&&c.rightHand.IsChildOf(c.rightForearm),"known V2 actual arm-chain references required");
   var filter=podium.GetComponent<MeshFilter>();var renderer=podium.GetComponent<MeshRenderer>();Require(filter!=null&&renderer!=null&&filter.sharedMesh!=null&&AssetDatabase.GetAssetPath(filter.sharedMesh)=="Assets/TruckOrchestra/Models/Bevelled acoustic instrument cabinet.asset"&&renderer.sharedMaterials.Length==1&&AssetDatabase.GetAssetPath(renderer.sharedMaterial)=="Assets/TruckBowling/Materials/Maple plank.mat","original owned podium mesh/material required");
   Require(match.instruments.Length==4&&match.instruments.All(i=>i!=null&&i.transform.Find("Owned connected instrument supports V2")!=null),"all four installed V2 connected instrument stations required");
   var voices=match.instruments.SelectMany(i=>i.voices).ToArray();Require(voices.Length==16&&voices.All(v=>v!=null&&!v.transform.IsChildOf(c.actorSpace)&&!v.transform.IsChildOf(podium)),"all16 original note sources must remain outside changed render roots");
   // Every read-only condition precedes the only three transform writes.
   ApplyNewPresentation(c.actorSpace,podium);EditorUtility.SetDirty(c.actorSpace);EditorUtility.SetDirty(podium);EditorSceneManager.MarkSceneDirty(match.gameObject.scene);
   Debug.Log("ORCHESTRA_PODIUM_CLEARANCE_V4 actor y1.05->1.40; original podium bottom0/top1->1.35; same mesh/material, audio poses/physics/notes/scoring/camera/HUD unchanged. Caller owns save; original strict rendered regression still required.");
  }
 }
}
