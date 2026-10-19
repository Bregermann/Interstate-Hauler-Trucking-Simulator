using System;using System.Linq;using System.Text;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;
namespace LWS.TruckDerby.Editor {
public static class DerbySceneAudit {
 public static void Run(){EditorSceneManager.OpenScene(DerbyAuthoring.Scene);Physics.SyncTransforms();var text=new StringBuilder();text.AppendLine("Actual enabled saved-scene static contacts inside playable ring; read-only audit.");foreach(var collider in UnityEngine.Object.FindObjectsByType<Collider>().Where(c=>c.enabled&&!c.isTrigger&&c.attachedRigidbody==null&&c.bounds.min.y<4&&c.bounds.max.y>.1f&&new Vector2(c.bounds.center.x,c.bounds.center.z).magnitude<44)){var rendered=collider.GetComponentsInChildren<Renderer>(true).Concat(collider.transform.parent!=null?collider.transform.parent.GetComponentsInChildren<Renderer>(true):Array.Empty<Renderer>()).Where(r=>r.enabled&&!r.forceRenderingOff&&r.gameObject.activeInHierarchy&&r.bounds.Intersects(collider.bounds)).ToArray();text.AppendLine("SOLID "+collider.name+" bounds="+collider.bounds+" nearbyVisibleRenderers="+rendered.Length+" parents="+Path(collider.transform));}
  foreach(float z in new[]{-16f,-8f,0f,8f,16f})foreach(float height in new[]{.65f,1.5f}){foreach(var hit in Physics.RaycastAll(new Vector3(-40,height,z),Vector3.right,80,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance)){if(hit.rigidbody!=null)continue;text.AppendLine("RAY z="+z+" y="+height+" object="+hit.collider.name+" p="+hit.point+" normal="+hit.normal+" bounds="+hit.collider.bounds);}}
  System.IO.File.WriteAllText("Logs/TruckSports/EssentialRelease/derby-solid-baseline.txt",text.ToString());
 }
 static string Path(Transform t){string value=t.name;while(t.parent!=null){t=t.parent;value=t.name+"/"+value;}return value;}
}
}
