using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace LWS.TruckArchaeology.Editor
{
 // Original Jenga pickup targets the load's local top with spreader ORIGIN.
 // Keep that original anchor/joint/force semantics. The owned solid sphere's
 // bottom must meet the origin, rather than its center penetrating the load.
 // This fixes pickup geometry only; first approach/physical load remains a
 // separate actual native gate. Never disables world/artifact contacts.
 public static class ArchaeologySpreaderColliderAuthoring
 {
  static void Require(bool value,string message){if(!value)throw new InvalidOperationException("Archaeology spreader geometry: "+message);}
  static void ValidateUnmodifiedSphere(SphereCollider sphere)
  {
   Require(sphere!=null&&sphere.center.Equals(Vector3.zero)&&Mathf.Abs(sphere.radius-.25f)<1e-6f&&!sphere.isTrigger&&sphere.enabled&&sphere.sharedMaterial==null,"exact old owned solid radius.25/center0/defaultmaterial sphere required; custom/already-corrected refused");
   Require(sphere.attachedRigidbody!=null&&sphere.attachedRigidbody.isKinematic&&!sphere.attachedRigidbody.useGravity&&sphere.gameObject.GetComponents<Collider>().Length==1&&(sphere.transform.lossyScale-Vector3.one).sqrMagnitude<1e-8f,"original unit-scale kinematic single-sphere head required");
  }
  internal static void ConfigureNewHead(SphereCollider sphere){sphere.center=Vector3.up*sphere.radius;}
  [MenuItem("Truck Sports/Alpha 0.7/Repair OPEN Archaeology/Owned sphere bottom at original spreader anchor")]
  public static void InstallOnOpenOriginalScene()
  {
   Require(!EditorApplication.isPlaying&&!EditorApplication.isCompiling,"stopped compiled Editor required");var matches=UnityEngine.Object.FindObjectsByType<ArchaeologyMatch>(FindObjectsInactive.Include,FindObjectsSortMode.None);Require(matches.Length==1&&matches[0].gameObject.scene.path==ArchaeologyAuthoring.Scene,"exact original saved Archaeology scene required");var match=matches[0];Require(match.equipment!=null&&match.equipment.Length==4,"original fixed4 physical equipment required");var spheres=new SphereCollider[4];
   for(int i=0;i<4;i++)
   {
    var eq=match.equipment[i];Require(eq!=null&&eq.match==match&&eq.head!=null&&eq.hoist!=null&&eq.hoist.spreader==eq.head&&eq.head.name=="Actual kinematic excavation head / crane spreader "+i,"original owner/head/crane identity required "+i);var sphere=eq.headShape as SphereCollider;Require(sphere!=null&&sphere.gameObject==eq.head.gameObject,"actual same-body head sphere required");ValidateUnmodifiedSphere(sphere);spheres[i]=sphere;
   }
   // Every known-original check precedes all four center writes. No asset,
   // body, origin, joint, collision filter, scene open/save or other tuning.
   foreach(var sphere in spheres){ConfigureNewHead(sphere);EditorUtility.SetDirty(sphere);}EditorSceneManager.MarkSceneDirty(match.gameObject.scene);Debug.Log("ARCHAEOLOGY_SPREADER_SPHERE_V9 only4 owned collider centers0->up*.25; sphere bottom now matches unchanged originalJenga load-top pickup origin. Allcontacts/forces/radii/bodyposes unchanged. Caller owns save; first-release/physical-load native gates required.");
  }
 }
}
