using UnityEngine;
using CompassNavigatorPro;
namespace LWS.TruckHideAndSeek
{
 // Compass Navigator Pro owns screen projection and edge indicators. These POIs
 // sit at an artificial broad-sector anchor; they never follow a hider.
 [DefaultExecutionOrder(600)]public sealed class HideHints:MonoBehaviour
 {
  public HideMatch match;public CompassPro[] compasses;public CompassProPOI[] clues;readonly System.Collections.Generic.List<Transform> surfaces=new System.Collections.Generic.List<Transform>(64);readonly int[] layers={26,27,28,29};
  public void Refresh()
  {
   var rally=match.drivingViews.rally;bool seeking=match.Live&&match.rules.Phase==HidePhase.Seeking;bool fresh=match.Age-match.perception.knowledge.SoundAt<6;
   for(int v=0;v<compasses.Length;v++)
   {
    bool broadcast=rally.Humans==0&&v==0;bool enabled=seeking&&fresh&&(broadcast||v<rally.Humans&&rally.owners[v]==match.rules.Seeker);
    var gps=compasses[v];gps.gameObject.SetActive(enabled);clues[v].gameObject.SetActive(enabled);if(!enabled)continue;
    layers[v]=broadcast?30:26+v;var camera=broadcast?match.broadcast.view:rally.views[v].view;gps.onScreenIndicatorScale=.24f;gps.offScreenIndicatorScale=.3f;gps.cameraMain=camera;gps.GetComponent<Canvas>().worldCamera=camera;gps.follow=match.trucks[match.rules.Seeker].transform;
    clues[v].transform.position=match.perception.knowledge.SectorGoal(gps.follow.position)+Vector3.up*3;clues[v].title="BROAD CLUE · "+match.perception.knowledge.SoundBand;
   }
  }
  // CNP legitimately rebuilds its indicator RectTransforms after a camera or
  // enable transition. Retag the current hierarchy, never cached destroyed UI.
  void LateUpdate(){for(int v=0;v<compasses.Length;v++)if(compasses[v].gameObject.activeInHierarchy){surfaces.Clear();compasses[v].GetComponentsInChildren(true,surfaces);foreach(var t in surfaces)t.gameObject.layer=layers[v];}}
  void OnDisable(){if(compasses!=null)foreach(var c in compasses)if(c!=null)c.gameObject.SetActive(false);if(clues!=null)foreach(var p in clues)if(p!=null)p.gameObject.SetActive(false);}
 }
}
