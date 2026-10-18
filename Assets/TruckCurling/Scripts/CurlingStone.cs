using UnityEngine;
using LWS.TruckSports;
namespace LWS.TruckCurling {
public sealed class CurlingStone:MonoBehaviour {
 public Renderer footprint;public LineRenderer protectedRing;public CurlingMatch match;public SportsEventTruck truck;public int delivery,team;public bool InPlay{get;private set;}public bool HitStone{get;private set;}public bool HitGuard{get;private set;}public float quiet;
 public bool Guard=>InPlay&&CurlingRules.Guard(truck.body.position,HitGuard);public bool Moving=>InPlay&&!truck.body.IsSleeping()&&(truck.body.linearVelocity.sqrMagnitude>.004f||truck.body.angularVelocity.sqrMagnitude>.01f);
 public void Prepare(int number,int ownerTeam,int slot){delivery=number;team=ownerTeam;InPlay=true;HitStone=HitGuard=false;quiet=0;truck.slot=slot;truck.Prepare(new Vector3(0,.38f,CurlingRules.Start),Quaternion.identity);var paint=new MaterialPropertyBlock();paint.SetColor("_BaseColor",team==0?new Color(.08f,.6f,.93f):new Color(1,.4f,.08f));footprint.SetPropertyBlock(paint);}
 public void Remove(){InPlay=false;truck.Power(false);if(!truck.body.isKinematic)truck.body.linearVelocity=truck.body.angularVelocity=Vector3.zero;truck.body.isKinematic=true;truck.body.detectCollisions=false;gameObject.SetActive(false);}
 public void Restore(Vector3 p,Quaternion q,bool hitStone,bool hitGuard){gameObject.SetActive(true);InPlay=true;HitStone=hitStone;HitGuard=hitGuard;truck.Power(false);truck.body.isKinematic=false;truck.body.detectCollisions=true;truck.body.position=p;truck.body.rotation=q;truck.body.linearVelocity=truck.body.angularVelocity=Vector3.zero;truck.body.Sleep();quiet=1;}
 void OnCollisionEnter(Collision c){if(!match.Live||!InPlay)return;var other=c.rigidbody!=null?c.rigidbody.GetComponent<CurlingStone>():null;if(other!=null&&other.InPlay){bool guarded=Guard||other.Guard;HitStone=true;HitGuard|=guarded;match.PhysicalContacts++;match.feedback.Play(1,c.relativeVelocity.magnitude);match.Pulse(truck.slot,Mathf.Clamp(c.relativeVelocity.magnitude*.025f,.03f,.22f),.08f);}}
}
}
