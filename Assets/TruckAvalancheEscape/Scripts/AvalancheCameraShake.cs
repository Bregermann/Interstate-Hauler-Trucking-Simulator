using UnityEngine;
using Cinemachine;
namespace LWS.TruckAvalancheEscape
{
 // Cinemachine supplies the noise signal; LWS supplies participant/proximity semantics.
 [DefaultExecutionOrder(500)]public sealed class AvalancheCameraShake:MonoBehaviour
 {
  public AvalancheMatch match;public NoiseSettings profile;
  void LateUpdate()
  {
   if(profile==null||!match.Live||match.ReplayControls||match.rules.Phase!=AvalanchePhase.Racing)return;
   for(int i=0;i<match.cameras.Length;i++){var camera=match.cameras[i];if(camera==null||!camera.enabled)continue;int owner=-1;for(int v=0;v<match.drivingViews.rally.Humans;v++)if(match.drivingViews.rally.views[v].view==camera)owner=match.drivingViews.rally.owners[v];if(owner<0){float best=float.NegativeInfinity;for(int j=0;j<match.setup.count;j++)if(match.rules.alive[j]&&!match.rules.finished[j]&&match.rules.distance[j]>best){best=match.rules.distance[j];owner=j;}}if(owner<0)continue;float behind=match.trucks[owner].body.position.z-match.front.CurrentZ;float amount=Mathf.Clamp01(1-Mathf.Abs(behind)/220)*.65f;profile.GetSignal(match.rules.Clock+i*7,out var position,out var rotation);camera.transform.position+=camera.transform.rotation*position*amount;camera.transform.rotation*=Quaternion.Slerp(Quaternion.identity,rotation,amount);}
  }
 }
}
