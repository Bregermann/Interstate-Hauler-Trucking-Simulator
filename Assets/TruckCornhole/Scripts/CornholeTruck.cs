using UnityEngine;
using NWH.WheelController3D;
namespace LWS.TruckCornhole {
// NWH alone owns approach tire/suspension forces. PhysX owns all landing contacts.
public sealed class CornholeTruck:MonoBehaviour {
 public CornholeCourse course;public Rigidbody body;public BoxCollider shape;public WheelController[] wheels;public AudioSource engine,wind,scrape;public Renderer paint;
 Renderer[] visuals;public Renderer[] Visuals=>visuals??(visuals=GetComponentsInChildren<Renderer>(true));public int delivery;public bool Launched{get;private set;}public bool Landed{get;private set;}public bool Holed{get;private set;}public bool Excluded{get;private set;}public int LaunchCount{get;private set;}
 public float GroundDuration{get;private set;}public float UsedAftertouch{get;private set;}public float StableTime{get;private set;}public float BoardContactTime{get;private set;}=-100;public float GroundContactTime{get;private set;}=-100;public int TruckImpacts{get;private set;}
 public Vector2 command;float flightTime,rangeGate,quiet;bool rangeReleased,braking,wasAbove,holeEntered,boardTouch,groundTouch;float lastImpact;Vector3 previousPosition;Quaternion previousRotation;
 public bool Grounded=>groundTouch;
 public bool Supported=>boardTouch;
 public bool Quiet=>Holed||body.IsSleeping()||body.linearVelocity.sqrMagnitude<.06f&&body.angularVelocity.sqrMagnitude<.09f;
 public int Points=>CornholeRules.Points(Holed,Grounded,Supported,StableTime>=1.2f,Excluded);
 public void Spawn(int number){gameObject.SetActive(true);delivery=number;Launched=Landed=Holed=Excluded=false;LaunchCount=0;GroundDuration=UsedAftertouch=StableTime=quiet=flightTime=rangeGate=0;rangeReleased=wasAbove=holeEntered=braking=boardTouch=groundTouch=false;BoardContactTime=GroundContactTime=-100;TruckImpacts=0;command=Vector2.zero;body.isKinematic=false;body.detectCollisions=true;shape.enabled=true;body.position=course.StartPosition;body.rotation=Quaternion.Euler(13.5f,0,0);body.linearVelocity=body.angularVelocity=Vector3.zero;foreach(var w in wheels){w.wheel.angularVelocity=0;w.enabled=true;if(w.wheel.meshCollider!=null)w.wheel.meshCollider.enabled=true;w.WakeFromSleep();}previousPosition=body.position;previousRotation=body.rotation;body.WakeUp();}
 public void Park(){StopSound();foreach(var w in wheels){w.MotorTorque=w.BrakeTorque=0;w.enabled=false;if(w.wheel.meshCollider!=null)w.wheel.meshCollider.enabled=false;}body.isKinematic=true;gameObject.SetActive(false);}
 public void Exclude(){ObserveHole();if(Holed)return;Excluded=true;command=Vector2.zero;foreach(var w in wheels){w.MotorTorque=w.BrakeTorque=0;w.enabled=false;if(w.wheel.meshCollider!=null)w.wheel.meshCollider.enabled=false;}}
 public void Launch(){if(Launched||Landed||Holed||course.Current!=this||course.Phase!=CornholePhase.Approach)return;var v=body.linearVelocity;if(v.z<1)return;Launched=true;LaunchCount++;flightTime=0;rangeReleased=false;rangeGate=0;foreach(var w in wheels){w.MotorTorque=w.BrakeTorque=0;w.enabled=false;if(w.wheel.meshCollider!=null)w.wheel.meshCollider.enabled=false;}
  // Preserve the entry's lateral line and heading. No target point or score is consulted.
  var heading=Vector3.ProjectOnPlane(body.rotation*Vector3.forward,Vector3.up).normalized;float speed=Mathf.Clamp(v.z,1,20);float forward=Mathf.Clamp(speed*course.launchForwardScale+course.launchForwardAdd,3,15);body.linearVelocity=new Vector3(Mathf.Clamp(v.x*.75f+heading.x*speed*.35f,-8,8),Mathf.Clamp(course.launchUp+speed*.06f,28,32),forward);body.angularVelocity+=new Vector3(-.35f,0,command.x*-.3f);body.WakeUp();course.Launched();}
 void Land(){if(!Launched||Landed)return;Landed=true;command=Vector2.zero;if(course.Current==this)course.Landed();}
 void FixedUpdate(){if(course==null||!course.match.InMatch||course.match.Paused||Holed||body.isKinematic)return;float dt=Time.fixedDeltaTime;bool current=course.Current==this;Vector2 input=current?command:Vector2.zero;
  if(current&&!Launched&&!Landed&&!Excluded&&course.Phase==CornholePhase.Approach){float speed=Vector3.Dot(body.linearVelocity,transform.forward);if(!braking&&input.y*speed<-.12f&&Mathf.Abs(speed)>.25f)braking=true;if(braking&&(Mathf.Abs(speed)<.12f||input.y*speed>=0))braking=false;foreach(var w in wheels){w.MotorTorque=braking||body.linearVelocity.magnitude>=21?0:input.y*2.3f;w.BrakeTorque=braking?9*Mathf.Abs(input.y):0;w.SteerAngle=w.transform.localPosition.z>0?input.x*22:0;if(w.IsGrounded&&w.HitCollider!=null){var surface=w.HitCollider.GetComponent<CornholeSurface>();if(surface!=null&&surface.course==course&&surface.kind==CornholeSurfaceKind.Trampoline)Launch();}}
  }
  if(Launched&&!Landed&&!Excluded){body.AddForce(course.match.FlightWind(body.linearVelocity),ForceMode.Acceleration);flightTime+=dt;if(Mathf.Abs(input.y)<.15f)rangeReleased=true;rangeGate=Mathf.MoveTowards(rangeGate,flightTime>.25f?1:0,dt*2);Vector2 touch=new Vector2(input.x,rangeReleased?input.y:0);float cost=touch.magnitude*course.aftertouchAcceleration*dt;float allowed=Mathf.Min(cost,Mathf.Max(0,course.aftertouchBudget-UsedAftertouch));if(cost>0){float factor=allowed/cost*rangeGate;body.AddForce(new Vector3(touch.x,0,touch.y*.65f)*course.aftertouchAcceleration*factor,ForceMode.Acceleration);body.AddTorque(new Vector3(touch.y*.22f,0,-touch.x*.35f)*factor,ForceMode.Acceleration);UsedAftertouch+=allowed*rangeGate;}
  }
  ObserveHole();
  GroundDuration=Grounded?GroundDuration+dt:0;bool stable=Supported&&!Grounded&&Quiet;StableTime=stable?StableTime+dt:0;quiet=Quiet?quiet+dt:0;
 }
 void PlaneBounds(Vector3 position,Quaternion rotation,out float min,out float max){min=float.MaxValue;max=float.MinValue;for(int i=0;i<8;i++){var p=Corner(i,position,rotation);min=Mathf.Min(min,p.y);max=Mathf.Max(max,p.y);}}
 Vector3 Corner(int i,Vector3 position,Quaternion rotation){var sign=new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1);var point=Vector3.Scale(shape.center+Vector3.Scale(shape.size*.5f,sign),transform.lossyScale);return course.board.InverseTransformPoint(position+rotation*point);}
 bool InsideOpening(Vector3 position,Quaternion rotation){float radius=course.holeRadius-.025f;for(int i=0;i<8;i++){var p=Corner(i,position,rotation);if(new Vector2(p.x,p.z-course.holeZ).sqrMagnitude>radius*radius)return false;}return true;}
 bool FitsAtCrossing(Vector3 now,Quaternion rotation,float plane,bool rear){float low=0,high=1;for(int i=0;i<12;i++){float t=(low+high)*.5f;PlaneBounds(Vector3.Lerp(previousPosition,now,t),Quaternion.Slerp(previousRotation,rotation,t),out var min,out var max);if((rear?max:min)>plane)low=t;else high=t;}float crossing=(low+high)*.5f;return InsideOpening(Vector3.Lerp(previousPosition,now,crossing),Quaternion.Slerp(previousRotation,rotation,crossing));}
 // Observe completed PhysX motion before miss/ground resolution. Entry and whole-body
 // exit may occur in the same step; neither board contact nor trigger callbacks are required.
 public void ObserveHole(){if(!Launched||Holed||Excluded||course.match.Paused||!gameObject.activeSelf)return;var now=body.position;var rotation=body.rotation;
  PlaneBounds(previousPosition,previousRotation,out var beforeMin,out var beforeMax);PlaneBounds(now,rotation,out var min,out var max);
  bool descending=course.board.InverseTransformPoint(now).y<course.board.InverseTransformPoint(previousPosition).y;
  wasAbove|=beforeMin>.02f||min>.02f;
  if(wasAbove&&descending&&beforeMin>.05f&&min<=.05f)holeEntered=FitsAtCrossing(now,rotation,.05f,false);
  // A resting truck may later be physically pushed into the opening by another delivery.
  if(wasAbove&&descending&&min<=.05f&&max>=-.4f)holeEntered=InsideOpening(now,rotation);
  bool cleared=holeEntered&&descending&&beforeMax>=-.4f&&max<-.4f&&FitsAtCrossing(now,rotation,-.4f,true);
  if(min>.05f)holeEntered=false;previousPosition=now;previousRotation=rotation;
  if(cleared)Collect();
 }
 void Collect(){if(Holed)return;if(Excluded){Park();return;}Holed=true;Landed=true;StableTime=0;command=Vector2.zero;foreach(var w in wheels)w.enabled=false;body.linearVelocity=body.angularVelocity=Vector3.zero;body.isKinematic=true;body.detectCollisions=false;shape.enabled=false;course.Hole(this);gameObject.SetActive(false);}
 void Contact(Collision c){var s=c.collider.GetComponent<CornholeSurface>();if(s!=null&&s.course==course){if(s.kind==CornholeSurfaceKind.Trampoline)Launch();if(s.kind==CornholeSurfaceKind.Board){Land();boardTouch=false;for(int i=0;i<c.contactCount;i++){var p=c.GetContact(i);var local=course.board.InverseTransformPoint(p.point);if(Vector3.Dot(p.normal,course.board.up)>.65f&&local.y>-.08f){BoardContactTime=Time.time;boardTouch=true;}}}if(s.kind==CornholeSurfaceKind.Ground){GroundContactTime=Time.time;groundTouch=true;}}var other=c.collider.GetComponent<CornholeTruck>();if(other!=null&&other.course==course){TruckImpacts++;if(other.Landed||other.Supported)Land();}}
 void OnCollisionExit(Collision c){var surface=c.collider.GetComponent<CornholeSurface>();if(surface==null||surface.course!=course)return;if(surface.kind==CornholeSurfaceKind.Board)boardTouch=false;if(surface.kind==CornholeSurfaceKind.Ground)groundTouch=false;}
 void OnCollisionStay(Collision c){Contact(c);}void OnCollisionEnter(Collision c){Contact(c);if(Launched&&Time.time>lastImpact+.12f){lastImpact=Time.time;course.match.feedback.Play(1,c.relativeVelocity.magnitude);course.Pulse(Mathf.Min(.28f,c.relativeVelocity.magnitude*.015f),.12f);}}
 void Update(){if(course==null)return;bool audible=course.match.InMatch&&!course.match.Paused&&Application.isFocused;float gain=audible?course.match.settings.sfx:0;SetLoop(engine,currentApproach?gain*.07f:0,.75f+body.linearVelocity.magnitude*.045f);SetLoop(wind,Launched&&!Landed?gain*.06f:0,.8f+body.linearVelocity.magnitude*.02f);SetLoop(scrape,Supported&&!Quiet?gain*.06f:0,1);}
 bool currentApproach=>course.Current==this&&course.Phase==CornholePhase.Approach;
 static void SetLoop(AudioSource s,float volume,float pitch){if(s==null)return;s.volume=volume;s.pitch=pitch;if(volume>.001f){if(!s.isPlaying)s.Play();}else if(s.isPlaying)s.Stop();}
 public void StopSound(){if(engine!=null)engine.Stop();if(wind!=null)wind.Stop();if(scrape!=null)scrape.Stop();}
 void OnDisable(){StopSound();}
}
}
