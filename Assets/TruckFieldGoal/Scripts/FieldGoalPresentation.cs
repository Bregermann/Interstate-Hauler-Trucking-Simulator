using TMPro;
using UnityEngine;

namespace LWS.TruckFieldGoal {
// Read-only feedback from the one match-owned Weather Maker wind vector.
public sealed class FieldGoalPresentation:MonoBehaviour {
 public FieldGoalMatch match;
 public Transform sock;
 public RectTransform windArrow;
 public TMP_Text forecast,speed,direction;
 float next;
 void LateUpdate(){
  var flow=match.CurrentWind;float magnitude=flow.magnitude;
  if(sock!=null){if(magnitude>.05f)sock.rotation=Quaternion.Slerp(sock.rotation,Quaternion.LookRotation(flow.normalized,Vector3.up),1-Mathf.Exp(-5*Time.deltaTime));sock.localScale=new Vector3(1,1,Mathf.Lerp(.3f,1,Mathf.Clamp01(magnitude/15)));}
  if(Time.unscaledTime<next)return;next=Time.unscaledTime+.12f;
  if(forecast!=null)forecast.text=match.Forecast!=null?match.atmosphere.Description:"MATCH FORECAST";
  if(speed!=null)speed.text=(magnitude*2.236936f).ToString("F1")+" MPH";
  if(windArrow!=null){windArrow.gameObject.SetActive(magnitude>.1f);var forward=Vector3.ProjectOnPlane(match.cameraRig.view.transform.forward,Vector3.up).normalized;windArrow.localRotation=Quaternion.Euler(0,0,-Vector3.SignedAngle(forward,flow,Vector3.up));}
  if(direction!=null)direction.text=magnitude<.1f?"CALM":Mathf.Abs(flow.x)>Mathf.Abs(flow.z)?flow.x>0?"CROSSWIND RIGHT":"CROSSWIND LEFT":flow.z>0?"TAILWIND":"HEADWIND";
 }
}
}
