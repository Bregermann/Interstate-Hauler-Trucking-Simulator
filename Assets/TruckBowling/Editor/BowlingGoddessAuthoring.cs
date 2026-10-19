using System;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using LWS.TruckSports.Editor;
namespace LWS.TruckBowling.Editor {
public static class BowlingGoddessAuthoring {
 public static void Upgrade(){var scene=EditorSceneManager.OpenScene(BowlingAuthoring.Scene);Install(UnityEngine.Object.FindAnyObjectByType<BowlingMatch>());EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();}
 public static void Install(BowlingMatch m){var safe=SportsEventAuthoring.Contact("TruckBowling","Truck-safe spring wall contact",.12f,0);safe.bounceCombine=PhysicsMaterialCombine.Minimum;EditorUtility.SetDirty(safe);foreach(var lane in m.lanes){
  // The installed return policy is pin-only and replaces PhysX's return delta.
  // The solid wall itself deliberately has no special restitution for trucks.
  lane.rebound.GetComponent<Collider>().sharedMaterial=safe;
  var link=lane.cab.GetComponent<LWS.TruckSports.SportsLegacyCabLook>();if(link==null||link.cockpit==null)throw new InvalidOperationException("Keep the actual Taxi cab before removing obsolete visuals.");var old=lane.cab.transform.Find("Inside tiny cab");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);BowlingCabRepairAuthoring.AlignTruck(lane.truck);lane.cab.interior=link.cockpit.GetComponentsInChildren<Renderer>(true);lane.cab.exterior=lane.truck.GetComponentsInChildren<Renderer>(true);
  int slatIndex=0;foreach(var t in lane.GetComponentsInChildren<Transform>(true)){
   if(t.name=="Gutter channel"){float sign=Mathf.Sign(t.position.x-lane.center);t.position=new Vector3(lane.center+sign*2.775f,-.25f,t.position.z);var scale=t.localScale;scale.x=1.35f;t.localScale=scale;}
   else if(t.name=="Gutter outside rail"){float sign=Mathf.Sign(t.position.x-lane.center);t.position=new Vector3(lane.center+sign*3.51f,t.position.y,t.position.z);}
   else if(t.name=="Gutter curve slat"){float sign=Mathf.Sign(t.position.x-lane.center);float fraction=(slatIndex++%4)/3f;t.position=new Vector3(lane.center+sign*Mathf.Lerp(2.17f,2.60f,fraction),Mathf.Lerp(-.045f,-.185f,fraction),t.position.z);var scale=t.localScale;scale.x=.16f;t.localScale=scale;}
  }EditorUtility.SetDirty(lane.cab);EditorUtility.SetDirty(lane.rebound);
 }Debug.Log("BOWLING_GODDESS_GEOMETRY gutters=1.35m solid truck-safe wall, pin-only return preserved, post-shot camera enabled");}
}
}
