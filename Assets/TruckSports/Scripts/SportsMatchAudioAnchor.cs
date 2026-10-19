using UnityEngine;
namespace LWS.TruckSports {
// One scene listener follows the leading display. Never move the match authority.
[DefaultExecutionOrder(1000)]public sealed class SportsMatchAudioAnchor:MonoBehaviour {
 public SportsEventSession match;public AudioListener listener;public SportsEventTruck[] trucks;
 void LateUpdate(){if(listener==null||match==null)return;Camera camera=null;if(match.cameras!=null)foreach(var c in match.cameras)if(c!=null&&c.enabled){camera=c;break;}if(camera==null)return;var p=camera.transform.position;if(trucks!=null&&trucks.Length>0){float best=float.PositiveInfinity;SportsEventTruck closest=null;foreach(var truck in trucks){if(truck==null||!truck.gameObject.activeInHierarchy)continue;float d=(truck.body.position-p).sqrMagnitude;if(d<best){best=d;closest=truck;}}if(closest!=null&&best>10000)p=closest.body.position+Vector3.up*1.5f;}listener.transform.SetPositionAndRotation(p,camera.transform.rotation);}
}
}
