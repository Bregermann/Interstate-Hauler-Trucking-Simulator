using UnityEngine;
using UnityEngine.Splines;

namespace LWS.TruckBaseJumping
{
 // One mode-owned flock adapter over actual artist animation, Unity Splines
 // and PhysX. It never changes a truck velocity, landing, score or input.
 [DefaultExecutionOrder(60)]
 public sealed class BaseBirdFlock:MonoBehaviour
 {
  public BaseJumpMatch match;public BaseBirdActor[] birds;public SplineContainer[] paths;
  [Range(6,12)]public float flightSpeed=10;
  [Range(.002f,.01f)]public float massFraction=.0075f;
  public Vector2 crossingInterval=new Vector2(10,18);
  [Range(0,.2f)]public float physicalCrossingChance=.12f;
  [Min(35)]public float minimumContactAltitude=45;
  public int Contacts{get;private set;}public int Crossings{get;private set;}public bool Active{get;private set;}
  public float LastImpulse{get;private set;}public int LastParticipant{get;private set;}=-1;
  readonly bool[] contacted=new bool[4];readonly float[] lengths=new float[3];
  readonly bool[] colliding=new bool[12];readonly Vector3[] pausedVelocity=new Vector3[12],pausedAngular=new Vector3[12];
  System.Random random;string matchId;int round=-1,crossingGroup=-1,crossingParticipant=-1;float nextCrossing,crossingStart,crossingDuration=5;bool crossingPhysical,paused;
  Vector3 crossA,crossB,crossC,crossD;
  Collider[] contactScope;bool[] scopeActive;
  bool Eligible(int slot)=>slot>=0&&slot<match.jumpers.Length&&match.jumpers[slot].Released&&!match.jumpers[slot].Landed&&match.jumpers[slot].Deployed&&match.jumpers[slot].Opening>.9f&&match.jumpers[slot].FlightTime>4&&match.jumpers[slot].truck.body.position.y>Mathf.Max(35,minimumContactAltitude)&&!contacted[slot];
  float Interval(){float minimum=Mathf.Max(6,Mathf.Min(crossingInterval.x,crossingInterval.y)),maximum=Mathf.Max(minimum,Mathf.Max(crossingInterval.x,crossingInterval.y));return Mathf.Lerp(minimum,maximum,(float)random.NextDouble());}
  void StartRound()
  {
   Clear();if(matchId!=match.MatchId){matchId=match.MatchId;Contacts=Crossings=0;LastParticipant=-1;LastImpulse=0;}round=match.Round;System.Array.Clear(contacted,0,contacted.Length);random=new System.Random(match.seed+round*1879);nextCrossing=match.AirTime+Interval();crossingGroup=crossingParticipant=-1;
   for(int i=0;i<3;i++)lengths[i]=paths[i].CalculateLength();float mass=float.MaxValue;foreach(var jumper in match.jumpers)mass=Mathf.Min(mass,jumper.truck.body.mass);Active=true;
   foreach(var bird in birds)
   {
    Path(bird.index,match.AirTime,out var point,out var tangent);bird.gameObject.SetActive(true);bird.shape.enabled=false;bird.body.mass=Mathf.Max(.001f,mass*Mathf.Clamp(massFraction,.002f,.01f));bird.body.isKinematic=false;bird.body.position=point;bird.body.rotation=Quaternion.LookRotation(tangent,Vector3.up);bird.transform.SetPositionAndRotation(point,bird.body.rotation);bird.body.linearVelocity=tangent*Mathf.Clamp(flightSpeed,6,12);bird.body.angularVelocity=Vector3.zero;bird.animator.speed=bird.animationSpeed;bird.animator.Play("Fly",0,bird.index*.137f%1);
    // Only the authoritative chassis may ever be enabled for an encounter.
    foreach(var jumper in match.jumpers)foreach(var collider in jumper.truck.GetComponentsInChildren<Collider>(true))if(collider.enabled&&collider.gameObject.activeInHierarchy)Physics.IgnoreCollision(bird.shape,collider,true);
    foreach(var other in birds)if(other!=bird&&other.gameObject.activeInHierarchy)Physics.IgnoreCollision(bird.shape,other.shape,true);
   }
  }
  void Path(int index,float age,out Vector3 position,out Vector3 direction)
  {
   int group=index/4;float distance=Mathf.Repeat(age*Mathf.Clamp(flightSpeed,6,12)+(index%4)*6,lengths[group]);float t=SplineUtility.GetNormalizedInterpolation(paths[group].Spline,distance,PathIndexUnit.Distance);paths[group].Evaluate(t,out var point,out var tangent,out var up);direction=((Vector3)tangent).normalized;if(direction.sqrMagnitude<.01f)direction=Vector3.forward;position=(Vector3)point+Vector3.Cross(Vector3.up,direction)*((index%4%2==0?-1:1)*(index%4)*.8f);
  }
  public bool TryCrossing(int participant,bool permitContact)
  {
   if(!Active||!match.Live||crossingGroup>=0||!Eligible(participant))return false;
   var target=match.jumpers[participant].truck.body;crossingParticipant=participant;crossingGroup=Crossings%3;crossingStart=match.AirTime;crossingDuration=5;
   var leader=birds[crossingGroup*4].body;var lateral=Vector3.ProjectOnPlane(target.position-leader.position,Vector3.up).normalized;if(lateral.sqrMagnitude<.01f)lateral=Vector3.right;
   var point=target.position+target.linearVelocity*3;point.y=Mathf.Max(minimumContactAltitude+5,point.y);crossA=leader.position;crossB=crossA+leader.linearVelocity*1.5f;crossC=point-lateral*15;crossD=point+lateral*25;crossingPhysical=permitContact;if(permitContact){contactScope=Object.FindObjectsByType<Collider>(FindObjectsInactive.Include,FindObjectsSortMode.None);scopeActive=new bool[contactScope.Length];for(int i=0;i<contactScope.Length;i++)scopeActive[i]=contactScope[i]!=null&&contactScope[i].enabled&&contactScope[i].gameObject.activeInHierarchy;}Crossings++;return true;
  }
  static Vector3 Curve(Vector3 a,Vector3 b,Vector3 c,Vector3 d,float t){float u=1-t;return a*(u*u*u)+b*(3*u*u*t)+c*(3*u*t*t)+d*(t*t*t);}
  void Permissions(BaseBirdActor bird)
  {
   bool permit=crossingGroup==bird.index/4&&crossingPhysical&&Eligible(crossingParticipant);if(permit==colliding[bird.index])return;colliding[bird.index]=permit;bird.shape.enabled=permit;
   if(permit)
   {
    // Per-collider overrides allow the actual chassis layer even when the
    // collection's layer matrix excludes decorative scenery. No project/global
    // collision matrix is changed; every non-target wheel/body is ignored.
    int layer=match.jumpers[crossingParticipant].truck.shape.gameObject.layer;bird.shape.includeLayers=1<<layer;bird.shape.excludeLayers=~(1<<layer);
    if(contactScope!=null)foreach(var shape in contactScope)if(shape!=null&&shape!=bird.shape&&shape.enabled&&shape.gameObject.activeInHierarchy)Physics.IgnoreCollision(bird.shape,shape,shape!=match.jumpers[crossingParticipant].truck.shape);
    for(int i=0;i<match.jumpers.Length;i++)foreach(var shape in match.jumpers[i].truck.GetComponentsInChildren<Collider>(true))if(shape.enabled&&shape.gameObject.activeInHierarchy)Physics.IgnoreCollision(bird.shape,shape,i!=crossingParticipant||shape!=match.jumpers[i].truck.shape||contacted[i]);
    foreach(var other in birds)if(other!=bird)Physics.IgnoreCollision(bird.shape,other.shape,true);
   }
  }
  void FixedUpdate()
  {
   if(match==null||!match.Live||match.Phase!=BaseJumpPhase.Flight)return;if(!Active||matchId!=match.MatchId||round!=match.Round)StartRound();if(paused)PauseBodies(false);
   if(crossingGroup>=0&&match.AirTime-crossingStart>crossingDuration){crossingGroup=crossingParticipant=-1;crossingPhysical=false;}
   if(match.AirTime>=nextCrossing){nextCrossing=match.AirTime+Interval();int candidate=random.Next(match.jumpers.Length);for(int i=0;i<match.jumpers.Length;i++){int slot=(candidate+i)%match.jumpers.Length;if(Eligible(slot)&&TryCrossing(slot,random.NextDouble()<physicalCrossingChance))break;}}
   // Existing pooled sky props can activate during a crossing. Keep those new
   // bird-only pairs ignored; no vehicle/traffic-to-vehicle pair is changed.
   if(crossingPhysical&&contactScope!=null)for(int i=0;i<contactScope.Length;i++){var shape=contactScope[i];bool active=shape!=null&&shape.enabled&&shape.gameObject.activeInHierarchy;if(active&&!scopeActive[i])foreach(var bird in birds)if(colliding[bird.index]&&shape!=bird.shape)Physics.IgnoreCollision(bird.shape,shape,shape!=match.jumpers[crossingParticipant].truck.shape);scopeActive[i]=active;}
   foreach(var bird in birds)
   {
    Path(bird.index,match.AirTime,out var goal,out var direction);
    if(crossingGroup==bird.index/4){float t=Mathf.Clamp01((match.AirTime-crossingStart)/crossingDuration);goal=Curve(crossA,crossB,crossC,crossD,t);direction=(Curve(crossA,crossB,crossC,crossD,Mathf.Min(1,t+.01f))-goal).normalized;if(direction.sqrMagnitude<.01f)direction=bird.transform.forward;goal+=Vector3.Cross(Vector3.up,direction)*((bird.index%4-1.5f)*2.2f)+Vector3.up*(bird.index%2);}
    var wind=match.weatherField.SampleWind(bird.body.worldCenterOfMass);var velocity=Vector3.ClampMagnitude((goal-bird.body.position)*2+direction*flightSpeed,14);bird.body.AddForce(Vector3.ClampMagnitude((velocity-bird.body.linearVelocity)*4+wind*.06f,25),ForceMode.Acceleration);
    var rotation=Quaternion.LookRotation(direction,Vector3.up)*Quaternion.Inverse(bird.body.rotation);rotation.ToAngleAxis(out float angle,out var axis);if(angle>180)angle-=360;if(float.IsFinite(axis.x))bird.body.AddTorque(Vector3.ClampMagnitude(axis*angle*.06f-bird.body.angularVelocity*2,8),ForceMode.Acceleration);Permissions(bird);
   }
  }
  public void Contact(BaseBirdActor bird,Collision collision)
  {
   if(!Active||!match.Live||!colliding[bird.index]||collision.rigidbody==null)return;var jumper=collision.rigidbody.GetComponentInParent<BaseJumper>();if(jumper==null||jumper.match!=match||jumper.slot!=crossingParticipant||!Eligible(jumper.slot))return;
   contacted[jumper.slot]=true;Contacts++;LastParticipant=jumper.slot;LastImpulse=collision.impulse.magnitude;foreach(var other in birds)if(other.index/4==crossingGroup){colliding[other.index]=false;other.shape.enabled=false;}crossingPhysical=false;
  }
  void Update()
  {
   if(match==null)return;if(match.Running&&matchId!=match.MatchId){matchId=match.MatchId;Contacts=Crossings=0;LastParticipant=-1;LastImpulse=0;}if(!match.Running||match.Finished||match.Phase!=BaseJumpPhase.Flight){if(Active)Clear();return;}
   if(paused!=match.Paused)PauseBodies(match.Paused);
  }
  void PauseBodies(bool value){paused=value;for(int i=0;i<birds.Length;i++){var bird=birds[i];bird.animator.speed=paused?0:bird.animationSpeed;if(paused){pausedVelocity[i]=bird.body.linearVelocity;pausedAngular[i]=bird.body.angularVelocity;bird.body.isKinematic=true;}else{bird.body.isKinematic=false;bird.body.linearVelocity=pausedVelocity[i];bird.body.angularVelocity=pausedAngular[i];}}}
  public void Clear(){Active=false;crossingGroup=crossingParticipant=-1;crossingPhysical=false;paused=false;System.Array.Clear(colliding,0,colliding.Length);if(birds!=null)foreach(var bird in birds)if(bird!=null){bird.shape.enabled=false;bird.body.isKinematic=true;bird.gameObject.SetActive(false);}}
  void OnDisable()=>Clear();
 }
}
