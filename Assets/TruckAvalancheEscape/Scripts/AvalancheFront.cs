using UnityEngine;
using LWS.TruckSports;
namespace LWS.TruckAvalancheEscape
{
 public sealed class AvalancheFront:MonoBehaviour
 {
  public AvalancheMatch match;public Rigidbody body;public BoxCollider contact;public BoxCollider[] settledSnow;public ParticleSystem cloud;public Transform debrisGroup;public Rigidbody[] debris;public Vector3[] debrisOffsets;public AudioSource rumble;public float initialZ=-240,startDelay=12,speed=12,acceleration=.01f;public float CurrentZ{get;private set;}
  public void ResetFront(){CurrentZ=initialZ;Pose(initialZ,true);if(settledSnow!=null)foreach(var shape in settledSnow)shape.enabled=false;cloud.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);for(int i=0;i<debris.Length;i++){var rb=debris[i];if(!rb.isKinematic)rb.linearVelocity=rb.angularVelocity=Vector3.zero;rb.isKinematic=true;rb.transform.SetPositionAndRotation(DebrisPosition(i),Quaternion.Euler(i*31,i*67,i*43));rb.position=rb.transform.position;rb.rotation=rb.transform.rotation;}rumble.Stop();}
  Vector3 DebrisPosition(int i){var p=body.position+debrisOffsets[i];p.y=match.world.Height(p)+8+i%4*2;return p;}
  void Pose(float z,bool immediate){var p=new Vector3(0,match.world.FrontHeight(z)+8,z);if(immediate){body.position=p;transform.position=p;}else body.MovePosition(p);}
  public void Step(float age,bool racing)
  {
   if(!racing){if(cloud.isPlaying)cloud.Stop();rumble.volume=0;return;}float t=Mathf.Max(0,age-startDelay);CurrentZ=initialZ+speed*t+.5f*acceleration*t*t;Pose(CurrentZ,false);if(settledSnow!=null)for(int i=0;i<settledSnow.Length;i++){float z=initialZ+40+i*80;var shape=settledSnow[i];shape.enabled=CurrentZ>z+40;shape.center=new Vector3(0,match.world.FrontHeight(z)+20,z)-body.position;}
   if(t>0&&!cloud.isPlaying)cloud.Play();float nearest=500;for(int i=0;i<match.setup.count;i++)if(match.rules.alive[i]&&!match.rules.finished[i])nearest=Mathf.Min(nearest,Mathf.Abs(match.trucks[i].body.position.z-CurrentZ));rumble.volume=match.settings.sfx*Mathf.Clamp01(1-nearest/260)*.6f;if(rumble.volume>0&&!rumble.isPlaying)rumble.Play();
   for(int i=0;i<debris.Length;i++){var rb=debris[i];if(t<=0)continue;if(rb.isKinematic){rb.isKinematic=false;rb.linearVelocity=new Vector3(Mathf.Sin(i)*2,0,speed);rb.angularVelocity=new Vector3(1,0,Mathf.Cos(i))*2;}if(rb.position.z<CurrentZ-110||rb.position.y<match.world.Height(rb.position)-10){rb.linearVelocity=rb.angularVelocity=Vector3.zero;var p=DebrisPosition(i);rb.position=p;rb.transform.position=p;rb.linearVelocity=Vector3.forward*speed+Vector3.up*2;}}
  }
  void OnTriggerStay(Collider shape){if(shape.isTrigger)return;var truck=shape.GetComponentInParent<SportsEventTruck>();if(truck!=null&&shape.attachedRigidbody==truck.body)match.ContactSnow(truck.slot);}
  void OnDisable(){if(rumble!=null)rumble.Stop();if(cloud!=null)cloud.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}
 }
}
