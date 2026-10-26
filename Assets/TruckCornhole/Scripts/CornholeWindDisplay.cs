using UnityEngine;using TMPro;using LWS.TruckSports;
namespace LWS.TruckCornhole {
// Read-only viewport presentation of the same Weather Maker vector used by flight.
public sealed class CornholeWindDisplay:MonoBehaviour {
 public CornholeMatch match;public RectTransform[] arrows;public TMP_Text[] speeds;readonly Vector3[] aiming={Vector3.forward,Vector3.forward,Vector3.forward,Vector3.forward};int cameraMask=-1;float next;
 void LateUpdate(){if(match.atmosphere==null)return;int mask=0;for(int i=0;i<4;i++)if(match.courses[i].chase.view.enabled)mask|=1<<i;if(mask!=cameraMask){cameraMask=mask;match.atmosphere.vendor.AllowCameras.Clear();for(int i=0;i<4;i++)if((mask&(1<<i))!=0)match.atmosphere.vendor.AllowCameras.Add(match.courses[i].chase.view);}if(Time.unscaledTime<next)return;next=Time.unscaledTime+.1f;for(int i=0;i<4;i++){var truck=match.courses[i].Current;if(truck!=null&&!truck.Launched){var forward=Vector3.ProjectOnPlane(truck.transform.forward,Vector3.up);if(forward.sqrMagnitude>.01f)aiming[i]=forward.normalized;}if(truck!=null&&truck.Launched)aiming[i]=truck.LaunchHeading;var flow=match.EffectiveWind(aiming[i]);arrows[i].localRotation=Quaternion.Euler(0,0,-Vector3.SignedAngle(aiming[i],flow,Vector3.up));arrows[i].gameObject.SetActive(flow.sqrMagnitude>.01f);speeds[i].text=flow.magnitude<.1f?"WIND · CALM":"WIND\n"+(flow.magnitude*2.236936f).ToString("F1")+" MPH";}}
}
}
