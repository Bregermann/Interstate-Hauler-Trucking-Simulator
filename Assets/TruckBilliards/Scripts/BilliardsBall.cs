using UnityEngine;
namespace LWS.TruckBilliards {
public sealed class BilliardsBall:MonoBehaviour {
 public int number; public BilliardsMatch match; public Rigidbody body; public SphereCollider sphere; public Vector3 initial;public BilliardsBallPresentation presentation;
 Vector3 previous;bool havePrevious;
 public bool Pocketed {get;private set;}
 public void SetPocketed(bool value){Pocketed=value;if(!body.isKinematic){body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;}body.isKinematic=value;if(!value){body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;}sphere.enabled=!value;if(presentation!=null)presentation.Pocket(value);else foreach(var r in GetComponentsInChildren<Renderer>())r.enabled=!value;}
 public void Restore(Vector3 p){SetPocketed(false);body.position=p;body.rotation=Quaternion.identity;body.Sleep();previous=p;havePrevious=true;presentation?.Restore();}
 void FixedUpdate(){
  if(Pocketed)return;var v=body.linearVelocity;v.y=0;body.linearVelocity=Vector3.MoveTowards(v,Vector3.zero,match.rollingResistance*Time.fixedDeltaTime);
  if(v.magnitude<match.settledSpeed*.6f){body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;}
  var current=new Vector3(body.position.x,0,body.position.z);var last=new Vector3(previous.x,0,previous.z);var segment=current-last;foreach(var p in match.pockets){float t=havePrevious&&segment.sqrMagnitude>.000001f?Mathf.Clamp01(Vector3.Dot(p-last,segment)/segment.sqrMagnitude):1;if(Vector3.Distance(havePrevious?last+segment*t:current,p)<match.pocketRadius){match.Pocket(this);break;}}previous=body.position;havePrevious=true;
 }
 void OnCollisionEnter(Collision c){var other=c.gameObject.GetComponent<BilliardsBall>();if(number==0&&other!=null)match.rules.Contact(other.number);
  if(match.session==null||!match.session.GameplayEnabled||match.session.feedback==null)return;
  if(other!=null){if(number<other.number)match.session.feedback.Impact(BilliardsSound.Ball,c.relativeVelocity.magnitude);}
  else if(c.contactCount>0&&Mathf.Abs(c.GetContact(0).normal.y)<.5f)match.session.feedback.Impact(BilliardsSound.Cushion,c.relativeVelocity.magnitude);}
}}

