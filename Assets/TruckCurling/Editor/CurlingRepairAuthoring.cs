using UnityEngine;
using UnityEditor;
namespace LWS.TruckCurling.Editor {
// Idempotent native migration; preserves the authored cab, setup, stone materials and shared UI.
public static class CurlingRepairAuthoring {
 public static void Install(CurlingMatch match){
  foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
   if(t.gameObject.scene!=match.gameObject.scene)continue;
   var p=t.position;var s=t.localScale;
   switch(t.name){
    case "Extra slippery curling sheet":p.z=CurlingRules.SheetCenter;s.z=CurlingRules.SheetLength;break;
    case "Sideline":p.z=CurlingRules.SheetCenter;s.z=CurlingRules.SheetLength-6;break;
    case "Outer arena boards":p.z=CurlingRules.SheetCenter;s.z=CurlingRules.SheetLength;break;
    case "Center line":p.z=81;s.z=210;break;
    case "Far hog line":p.z=CurlingRules.Hog;break;
    case "Tee line":p.z=CurlingRules.Tee;break;
    case "Back line":p.z=CurlingRules.Back;break;
    case "House title":p.z=189;break;
    case "Outer house":case "Middle house":case "Inner house":case "Button":
     var line=t.GetComponent<LineRenderer>();if(line!=null){var points=new Vector3[line.positionCount];line.GetPositions(points);float center=0;foreach(var v in points)center+=v.z;center/=points.Length; // duplicated closing vertex has zero sine, so the center is exact.
      for(int i=0;i<points.Length;i++)points[i].z+=CurlingRules.Tee-center;line.SetPositions(points);EditorUtility.SetDirty(line);}continue;
    default:continue;
   }t.position=p;t.localScale=s;EditorUtility.SetDirty(t);
  }
  foreach(var stone in match.stones){var p=stone.transform.position;p.z=CurlingRules.Start;stone.transform.position=p;EditorUtility.SetDirty(stone.transform);}
  var camera=match.houseCamera.transform;var cameraPosition=camera.position;cameraPosition.z=CurlingRules.Tee;camera.position=cameraPosition;
  match.cabCamera.view.farClipPlane=Mathf.Max(600,match.cabCamera.view.farClipPlane);EditorUtility.SetDirty(camera);EditorUtility.SetDirty(match);
 }
}
}
