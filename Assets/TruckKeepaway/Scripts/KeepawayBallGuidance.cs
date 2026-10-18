using CompassNavigatorPro;
using UnityEngine;
using LWS.TruckSports;
using LWS.TruckBowling;
using LWS.SportsPresentation;
namespace LWS.TruckKeepaway {
// Read-only ball proxy. Never owns input, ball physics or possession.
[DefaultExecutionOrder(500)]
public sealed class KeepawayBallGuidance : MonoBehaviour {
 public KeepawayMatch match;public CompassProPOI ballPOI;public KeepawayBallCompass[] views;
 void LateUpdate(){
  bool valid=match!=null&&match.ball!=null&&match.Running&&!match.Finished&&match.menus.Screen==SportsEventScreen.Play;
  var position=valid?match.ball.position:Vector3.zero;
  valid=valid&&float.IsFinite(position.x)&&float.IsFinite(position.y)&&float.IsFinite(position.z);
  // Keep the POI registered during menus/rematches; hiding a compass preserves its
  // per-group state and avoids destroying other views' vendor-owned indicators.
  if(valid)ballPOI.transform.position=position;
  for(int i=0;i<views.Length;i++){
   bool active=valid&&match.cabVariant&&i<match.HumanViews&&match.setup.players[match.CabSlot(i)].source!=BowlingSource.AI;
   var view=views[i];
   if(active&&!view.gameObject.activeSelf)view.gameObject.SetActive(true);
   if(active){view.cameraMain=match.cabViews[i].view;view.follow=view.cameraMain.transform;view.viewportRect=view.cameraMain.rect;view.ballPOI=ballPOI;}
   if(view.gameObject.activeSelf!=active){if(!active)view.ResetPresentation();view.gameObject.SetActive(active);}
  }
 }
}
}
