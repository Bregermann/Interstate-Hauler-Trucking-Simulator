using System.Collections.Generic;
using UnityEngine;
namespace LWS.TruckHockey {
// Articulated blade is a separate kinematic PhysX body. It only collides with the puck.
// During side changes it tucks with contact disabled and rearms only after clear separation.
[DefaultExecutionOrder(50)]public sealed class HockeyStick:MonoBehaviour {
 public HockeyTruck owner;public Rigidbody bladeBody;public BoxCollider blade;public Transform bladeVisual;public LineRenderer shaft;public Collider[] obstacles;
 public float sideOffset=.65f,forwardOffset=.85f,lookAhead=.23f,sideHold=.65f,moveSpeed=5.5f;public int Side{get;private set;}=1;public bool Tucked{get;private set;}public int Switches{get;private set;}
 readonly Collider[] found=new Collider[64];readonly HashSet<Collider> solids=new HashSet<Collider>();Vector3 local;float hold,lag,contactKick;bool switching;int stage;Collider[] ignored;public string Diagnostic=>"side="+Side+" tucked="+Tucked+" switching="+switching+" stage="+stage+" local="+local+" puck="+owner.match.puck.position;
 void Awake(){foreach(var c in obstacles)if(c!=null)solids.Add(c);ResetRig();}
 void Start(){Bind();}
 void Bind(){if(ignored==null)ignored=FindObjectsByType<Collider>(FindObjectsInactive.Include);var puck=owner.match.puck.GetComponent<Collider>();foreach(var c in ignored)if(c!=null&&c!=blade&&c!=puck)Physics.IgnoreCollision(blade,c);}
 public void ResetRig(){Side=1;hold=0;lag=contactKick=0;switching=true;stage=0;local=new Vector3(0,.65f,0);if(blade!=null)blade.enabled=false;if(bladeBody!=null&&owner!=null){bladeBody.position=owner.body.position+owner.body.rotation*local;bladeBody.rotation=owner.body.rotation;}}
 bool SolidBox(Vector3 center,Quaternion rotation){int n=Physics.OverlapBoxNonAlloc(center,new Vector3(.36f,.12f,.2f),found,rotation,~0,QueryTriggerInteraction.Ignore);if(n==found.Length)return true;for(int i=0;i<n;i++)if(solids.Contains(found[i]))return true;return false;}
 bool Clear(int side){for(int step=0;step<3;step++){float t=step*lookAhead*.5f;var q=owner.body.rotation*Quaternion.Euler(0,owner.steer*owner.turnDegrees*t*.65f,0);var p=owner.body.position+owner.body.linearVelocity*t;var tip=p+q*new Vector3(side*sideOffset,-.14f,forwardOffset);if(SolidBox(tip,q))return false;var start=p+q*new Vector3(side*.3f,.32f,.1f);for(int k=0;k<4;k++){var point=Vector3.Lerp(start,tip,k/3f);int n=Physics.OverlapSphereNonAlloc(point,.08f,found,~0,QueryTriggerInteraction.Ignore);if(n==found.Length)return false;for(int i=0;i<n;i++)if(solids.Contains(found[i]))return false;}}return true;}
 void FixedUpdate(){if(owner==null||owner.match==null||bladeBody==null)return;if(!owner.match.rules.Live||owner.match.MenuOpen){blade.enabled=false;return;}hold-=Time.fixedDeltaTime;bool safe=Clear(Side),other=Clear(-Side);Tucked=!safe;
  if(!safe&&other&&hold<=0&&(!switching||stage==0)){Side=-Side;Switches++;hold=sideHold;switching=true;stage=0;}
  if(!safe&&!switching){switching=true;stage=0;}
  contactKick=Mathf.MoveTowards(contactKick,0,18*Time.fixedDeltaTime);var relative=owner.match.puck.position-owner.body.position;bool preparing=relative.sqrMagnitude<4&&Vector3.Dot(relative,owner.match.puck.linearVelocity-owner.body.linearVelocity)<-.4f;float wantedLag=Mathf.Clamp(-owner.steer*10-owner.throttle*3+(preparing?-Side*2:0)+contactKick,-14,14);lag=Mathf.Lerp(lag,wantedLag,1-Mathf.Exp(-9*Time.fixedDeltaTime));var rotation=owner.body.rotation*Quaternion.Euler(0,lag,0);
  var target=new Vector3(Side*sideOffset,-.14f,forwardOffset);if(switching){blade.enabled=false;if(stage==0){target=new Vector3(0,.65f,0);if(Vector3.Distance(local,target)<.025f)stage=1;}else if(Tucked)target=new Vector3(0,.65f,0);else if(Vector3.Distance(local,target)<.025f){switching=false;hold=sideHold;}}
  if(Tucked)target=new Vector3(0,.65f,0);local=Vector3.MoveTowards(local,target,moveSpeed*Time.fixedDeltaTime);var world=owner.body.position+rotation*local;
  if(!switching&&!Tucked&&SolidBox(world,rotation)){switching=true;stage=0;blade.enabled=false;return;}
  bladeBody.MovePosition(world);bladeBody.MoveRotation(rotation);var puck=owner.match.puck;var puckLocal=Quaternion.Inverse(rotation)*(puck.position-world)-blade.center;var half=blade.size*.5f;var closest=new Vector3(Mathf.Clamp(puckLocal.x,-half.x,half.x),Mathf.Clamp(puckLocal.y,-half.y,half.y),Mathf.Clamp(puckLocal.z,-half.z,half.z));float distance=(puckLocal-closest).magnitude;if(!switching&&!Tucked&&!blade.enabled&&distance>owner.match.puckContact.radius+.1f){blade.enabled=true;Bind();}
 }
 void LateUpdate(){if(owner==null)return;var mount=owner.transform.TransformPoint(new Vector3(Side*.3f,.32f,.1f));shaft.SetPosition(0,mount);shaft.SetPosition(1,bladeBody.transform.position);}
 void OnCollisionEnter(Collision c){if(c.rigidbody==owner.match.puck&&blade.enabled)contactKick=Mathf.Clamp(Vector3.Dot(c.relativeVelocity,owner.transform.right)*.5f,-5,5);}
 void OnDisable(){if(blade!=null)blade.enabled=false;}
}
}
