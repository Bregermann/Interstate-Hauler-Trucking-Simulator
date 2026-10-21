using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace LWS.TruckFrogger.Editor
{
 public static class FrogAudit
 {
  public static void Inspect(){EditorSceneManager.OpenScene(FrogAuthoring.Scene);var m=Object.FindAnyObjectByType<FrogMatch>();for(int i=0;i<5;i++){foreach(var c in m.world.courses)c.root.gameObject.SetActive(false);var course=m.world.courses[i];course.root.gameObject.SetActive(true);Physics.SyncTransforms();var ray=new Ray(course.start+Vector3.up*30,Vector3.down);foreach(var h in Physics.RaycastAll(ray,60,1,QueryTriggerInteraction.Ignore))Debug.Log("FROG_START_SURFACE course="+i+" hit="+h.collider.name+" point="+h.point);foreach(var c in Physics.OverlapBox(course.start+Vector3.up*2,new Vector3(3,1.4f,7),Quaternion.identity,1,QueryTriggerInteraction.Ignore))if(c.bounds.max.y>course.start.y+.3f)Debug.Log("FROG_START_OVERLAP course="+i+" collider="+c.name+" bounds="+c.bounds);foreach(var mover in course.movers){var shapes=mover.GetComponentsInChildren<Collider>();var b=shapes[0].bounds;foreach(var s in shapes)b.Encapsulate(s.bounds);Debug.Log("FROG_ACTUAL_MOVER_BOUNDS course="+i+" name="+mover.name+" origin="+mover.origin+" actual="+b+" halfWidth="+mover.halfWidth);}}}
  public static void Signs(){EditorSceneManager.OpenScene(FrogAuthoring.Scene);foreach(var text in Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(text.name=="Readable crossing warning"||text.name=="Unmistakable crossing finish")text.characterSize=.12f;EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());AssetDatabase.SaveAssets();}
 }
}

