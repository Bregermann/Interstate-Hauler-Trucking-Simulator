using UnityEngine;using LWS.TruckSports;
namespace LWS.TruckMattressRacing {
// Bounded, scene-owned contact effects. No extra physics or gameplay authority.
public sealed class MattressPresentation:MonoBehaviour {
 public MattressMatch match;public ParticleSystem[] dust;public TrailRenderer[] tracks;
 void LateUpdate(){for(int i=0;i<match.racers.Length;i++){var r=match.racers[i];float speed=r.truck.body.linearVelocity.magnitude;bool sliding=match.Live&&match.Phase==MattressPhase.Race&&r.Phase==MattressAttempt.Racing&&r.Grounded&&speed>3;dust[i].transform.position=r.transform.TransformPoint(new Vector3(0,-.25f,-2));var emission=dust[i].emission;emission.enabled=sliding;var main=dust[i].main;bool mud=match.course.ContactFactor(r.transform.position)>1.13f;main.startColor=mud?new Color(.20f,.13f,.065f,.65f):new Color(.61f,.54f,.34f,.4f);for(int k=0;k<2;k++){var t=tracks[i*2+k];t.transform.position=r.transform.TransformPoint(new Vector3(k==0?-1.3f:1.3f,-.32f,-1.8f));t.emitting=sliding&&mud;t.startColor=new Color(.18f,.11f,.05f,.5f);t.endColor=new Color(.18f,.11f,.05f,0);}}}
 void OnDisable(){if(dust!=null)foreach(var p in dust)if(p!=null)p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);if(tracks!=null)foreach(var t in tracks)if(t!=null){t.emitting=false;t.Clear();}}
}
}
