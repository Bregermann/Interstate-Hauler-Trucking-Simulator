using UnityEngine;
namespace LWS.TruckSports {
// Read-only PhysX contact observation. Weatherade remains the trace renderer; no force or grip changes.
public sealed class SportsSnowBodyContact:MonoBehaviour {
 public Collider Surface{get;private set;}public Vector3 Point{get;private set;}float stamp=-100;
 public bool Supported=>Surface!=null&&Time.fixedTime-stamp<.07f;
 void Observe(Collision collision){if(!isActiveAndEnabled)return;Vector3 sum=Vector3.zero;int count=0;for(int i=0;i<collision.contactCount;i++){var c=collision.GetContact(i);if(c.normal.y<.3f)continue;sum+=c.point;count++;}if(count==0)return;Surface=collision.collider;Point=sum/count;stamp=Time.fixedTime;}
 void OnCollisionEnter(Collision c){Observe(c);}void OnCollisionStay(Collision c){Observe(c);}void OnCollisionExit(Collision c){if(c.collider==Surface){Surface=null;stamp=-100;}}void OnDisable(){Surface=null;stamp=-100;}
}}
