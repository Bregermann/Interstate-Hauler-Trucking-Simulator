using UnityEngine;
using NWH.WheelController3D;
namespace LWS.TruckWallball {
// NWH alone owns tire/suspension forces. LWS owns sport inputs, upright jump and limits.
[DefaultExecutionOrder(-20)]public sealed class WallballTruck:MonoBehaviour {
 public WallballMatch match;public Rigidbody body;public BoxCollider shape;public int slot;public AudioSource engine;public WheelController[] wheels;public Collider[] CollisionShapes{get;private set;}
 public float motorTorque=3.6f,brakeTorque=9,maxSpeed=9,reverseSpeed=6,steerAngle=36,grip=1.25f,turnDegrees=200,gravity=24,jumpSpeed=9,jumpCooldown=.45f;
 public float throttle,steer;public bool Grounded{get;private set;}public bool BrakingOpposite{get;private set;}public int Jumps{get;private set;}public int Landings{get;private set;}public int Hits{get;private set;}
 bool opposing,jumpRequested,wasGrounded;float nextJump,airborneUntil;
 public void CacheShapes(){CollisionShapes=GetComponentsInChildren<Collider>(true);foreach(var c in CollisionShapes)c.gameObject.layer=2;}
 // Collider.ClosestPoint uses the interpolated Transform. Eligibility needs the current physics pose.
 public Vector3 PhysicalClosestPoint(Collider c,Vector3 point){var offset=transform.InverseTransformPoint(c.transform.position);var rotation=Quaternion.Inverse(transform.rotation)*c.transform.rotation;return Physics.ClosestPoint(point,c,body.position+body.rotation*offset,body.rotation*rotation);}
 public void Control(Vector2 drive,bool jump=false){steer=Mathf.Clamp(drive.x,-1,1);throttle=Mathf.Clamp(drive.y,-1,1);jumpRequested|=jump;}
 public void Stop(){throttle=steer=0;opposing=jumpRequested=false;if(wheels!=null)foreach(var w in wheels)if(w!=null){w.MotorTorque=w.BrakeTorque=0;w.SteerAngle=0;}}
 public void ResetBody(Vector3 p){body.position=p;body.rotation=Quaternion.identity;if(!body.isKinematic)body.linearVelocity=body.angularVelocity=Vector3.zero;Stop();nextJump=airborneUntil=0;wasGrounded=false;foreach(var w in wheels)w.wheel.angularVelocity=0;}
 public void RecordHit(){Hits++;}
 void FixedUpdate(){if(!match.rules.Active||match.MenuOpen){Stop();return;}int grounded=0;if(Time.time>=airborneUntil)foreach(var w in wheels){if(!w.enabled){w.enabled=true;w.WakeFromSleep();CacheShapes();match.BindTruckPairs();}if(w.IsGrounded)grounded++;}Grounded=grounded>=2&&body.linearVelocity.y<1;
  if(Grounded&&!wasGrounded){Landings++;match.feedback.Play(3,Mathf.Max(1,Mathf.Abs(body.linearVelocity.y)));}wasGrounded=Grounded;
  float speed=Vector3.Dot(body.linearVelocity,transform.forward);bool requested=Mathf.Abs(throttle)>.08f&&speed*throttle<0;if(!requested||Mathf.Abs(speed)<.12f)opposing=false;else if(Mathf.Abs(speed)>.3f)opposing=true;BrakingOpposite=opposing;
  float motor=opposing||speed*(throttle<0?-1:1)>=(throttle<0?reverseSpeed:maxSpeed)?0:throttle*motorTorque;
  for(int i=0;i<wheels.Length;i++){var w=wheels[i];w.MotorTorque=motor;w.BrakeTorque=opposing?Mathf.Abs(throttle)*brakeTorque:0;w.SteerAngle=i<2?steer*steerAngle:0;w.LongitudinalFrictionGrip=w.LateralFrictionGrip=grip;}
  var velocity=body.linearVelocity;var flat=new Vector3(velocity.x,0,velocity.z);flat=Vector3.ClampMagnitude(flat,11);body.linearVelocity=new Vector3(flat.x,velocity.y,flat.z);body.AddForce(Vector3.down*gravity,ForceMode.Acceleration);
  if(jumpRequested&&Grounded&&Time.time>=nextJump){velocity=body.linearVelocity;velocity.y=jumpSpeed;body.linearVelocity=velocity;nextJump=Time.time+jumpCooldown;airborneUntil=Time.time+.12f;foreach(var w in wheels)w.enabled=false;Grounded=wasGrounded=false;Jumps++;match.feedback.Play(4,0,true);match.Pulse(slot,.06f,.06f);}jumpRequested=false;
 }
 void Update(){if(engine==null)return;bool play=match.rules.Active&&!match.MenuOpen&&Application.isFocused&&match.settings.master>0&&match.settings.sfx>0;engine.volume=play?match.settings.sfx*.065f:0;engine.pitch=.75f+new Vector2(body.linearVelocity.x,body.linearVelocity.z).magnitude*.04f+Mathf.Abs(throttle)*.08f;if(play&&!engine.isPlaying)engine.Play();if(!play&&engine.isPlaying)engine.Stop();}
 void OnDisable(){if(engine!=null)engine.Stop();}
}
}
