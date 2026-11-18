using UnityEngine;
using LWS.TruckBowling;
using LWS.TruckSports;
namespace LWS.TruckWaterSports {
public sealed class BlackwaterMatch:WaterMatch {
 public BlackwaterWake wake;public Rigidbody entity;public Rigidbody[] supplies;public Transform dock;
 [Min(0)]public float safeContactSpeed=3.5f,minimumContactImpulsePerMass=.75f,contactDamageCooldown=1.2f;
 [Range(0,50)]public float maximumContactDamage=20;
 public readonly int[] HullImpactHits=new int[4];public readonly float[] ContactDamageTaken=new float[4];readonly float[] nextContactDamage=new float[4];
 [Min(.5f)]public float aiQuietHoldSeconds=5,aiCommittedEscapeSeconds=12;
 [Min(.1f)]public float engineAcceleration=7.5f,maximumPoweredSpeed=5.5f;
 public float[] Integrity={100,100,100,100};public int[] Deliveries=new int[4],Rescues=new int[4];public float entitySpeed=9;public AudioSource warning;
 public int BankedSupplies{get;private set;}public bool CooperativeEscaped{get;private set;}
 public int SharedSuppliesNeeded=>Mathf.Max(1,(setup.count+1)/2);
 readonly FixedJoint[] cargoJoints=new FixedJoint[4];readonly int[] boatCargo={-1,-1,-1,-1},cargoOwner={-1,-1,-1,-1};
 readonly bool[] banked=new bool[4];readonly Vector3[] cargoStarts=new Vector3[4];
 readonly Light[] healingAt=new Light[4];readonly float[] rescueProgress=new float[4],quietSeconds=new float[4];Light[] shelterLights=System.Array.Empty<Light>();bool capturedCargoStarts;float nextStrike,targetSwitch;int hunted;
 readonly float[] propulsionInput=new float[4],escapeUntil=new float[4],quietAttemptUntil=new float[4];
 readonly int[] cargoTargets={-1,-1,-1,-1};
 readonly Collider[][] vesselHulls=new Collider[4][];float creatureCrown,creatureTurnRadius;BoxCollider creatureHull;
 readonly RaycastHit[] creatureSweepHits=new RaycastHit[24];
 protected override bool Canoe=>false;public override string Title=>"BLACKWATER: SOMETHING BELOW";public override bool Cooperative=>option==3;
 public override string OptionLabel=>new[]{"MIDNIGHT CROSSING","SUPPLY RUN","SURVIVAL","COOPERATIVE BLACKWATER ESCAPE"}[option];
 public override string ResultUnit=>Cooperative?"DELIVERIES":"SEC";public override bool ResultHigherIsBetter=>option==2||Cooperative;
 public override double ResultScore(int slot)=>Cooperative?Deliveries[slot]:Stranded[slot]?StrandedAt[slot]:FinishTimes[slot]>=0?FinishTimes[slot]:Clock;
 public override bool ResultHasOutcome(int slot)=>slot>=0&&slot<setup.count&&(option==0?FinishTimes[slot]>=0&&Integrity[slot]>0:option==1?Deliveries[slot]>0:Cooperative?CooperativeEscaped||Deliveries[slot]>0||Rescues[slot]>0:Clock>=3);
 public override string Help=>"Drive and steer your physical barge; reverse thrust brakes. Hard normal collisions with sufficient impulse damage the hull; gentle docking and glancing scrapes are protected. Entity strikes remain a separate danger. ACTION rights a capsize. Crossing follows lit buoys to the far shore. Supply Run: touch a real unclaimed crate, then return it slowly inside the dock. Survival ranks actual time afloat; slowing quietly can shake pursuit, but lingering silence eventually draws it back. In crossing/delivery modes the green navigation lights repel direct strikes and slowly restore hull; wakes still move you. Cooperative Escape needs shared supplies, every boat upright and moving slowly back inside the dock, and no stranded teammates. Any boat can carry supplies. Hold ACTION while gently touching a stranded teammate for two seconds to repair them. AI can collect, brake, seek shelter and physically rescue. Cooperative success awards no individual winner point.";
 public override string Status{
  get{
   if(Cooperative){if(BankedSupplies>=SharedSuppliesNeeded)return "SUPPLIES READY · RESCUE TEAMMATES · REGROUP AT DOCK";for(int i=0;i<setup.count;i++)if(Integrity[i]<=0)return "TEAMMATE STRANDED · GENTLE CONTACT + HOLD ACTION";return "SHARED SUPPLIES "+BankedSupplies+" / "+SharedSuppliesNeeded+" · GREEN LIGHTS OFFER SHELTER";}
   return Clock<20?"FOLLOW THE NAVIGATION LIGHTS":Clock<45?"THAT WAKE IS MOVING AGAINST THE WIND":option==2?"STAY ALIVE · QUIET BUYS TIME, NOT PERMANENT SAFETY":"SHELTER AT GREEN LIGHTS · WATCH THE WAKE";
  }
 }
 public override string Results=>Cooperative?(CooperativeEscaped?"CONVOY ESCAPED":"COOPERATIVE EXTRACTION FAILED")+"\nShared supplies "+BankedSupplies+" / "+SharedSuppliesNeeded+"\n"+Standings:base.Results;
 // Capture the saved positions before the setup screen can leave gravity-only
 // crates falling while their match-gated flotation is inactive.
 protected override void Awake(){base.Awake();CachePhysicalHulls();if(vessels!=null)foreach(var vessel in vessels)if(vessel!=null)vessel.HullImpact+=ContactImpact;var lights=new System.Collections.Generic.List<Light>();foreach(var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))if(light.type==LightType.Point&&light.color.g>light.color.r*1.3f&&light.color.g>light.color.b*1.1f)lights.Add(light);shelterLights=lights.ToArray();if(supplies==null||supplies.Length<4)return;for(int cargo=0;cargo<4;cargo++)if(supplies[cargo]!=null){cargoStarts[cargo]=supplies[cargo].position;supplies[cargo].isKinematic=true;}capturedCargoStarts=true;}
 void CachePhysicalHulls(){
  creatureHull=entity!=null?entity.GetComponent<BoxCollider>():null;
  if(creatureHull!=null){creatureCrown=(creatureHull.center.y+creatureHull.size.y*.5f)*Mathf.Abs(creatureHull.transform.lossyScale.y);Vector3 extent=Vector3.Scale(creatureHull.size*.5f,creatureHull.transform.lossyScale);creatureTurnRadius=Mathf.Max(.1f,new Vector2(extent.x,extent.z).magnitude);}
  if(vessels==null)return;
  for(int i=0;i<Mathf.Min(4,vessels.Length);i++)if(vessels[i]!=null){
   var hulls=new System.Collections.Generic.List<Collider>();
   foreach(var hull in vessels[i].GetComponentsInChildren<Collider>(true))if(!hull.isTrigger&&hull.GetComponentInParent<Rigidbody>(true)==vessels[i].body)hulls.Add(hull);
   vesselHulls[i]=hulls.ToArray();
  }
 }
 float HullBottom(int slot){float bottom=vessels[slot].body.position.y;if(vesselHulls[slot]!=null)foreach(var hull in vesselHulls[slot])if(hull!=null&&hull.enabled)bottom=Mathf.Min(bottom,hull.bounds.min.y);return bottom;}
 // The leviathan is a real kinematic obstacle, not a collision-free visual.
 // Its long hull cannot rotate instantaneously through other barges. Limit the
 // outer corner's turn speed to the authored swimming speed, sweep translation,
 // and reject rotations that introduce/deepen an actual vessel penetration.
 void MoveCreature(Vector3 desired,float dt){
  Vector3 delta=desired-entity.position,step=Vector3.ClampMagnitude(delta,entitySpeed*dt);
  Vector3 heading=Vector3.ProjectOnPlane(delta,Vector3.up);Quaternion rotation=entity.rotation;
  if(heading.sqrMagnitude>1)rotation=Quaternion.RotateTowards(rotation,Quaternion.LookRotation(heading.normalized,Vector3.up),entitySpeed/Mathf.Max(.1f,creatureTurnRadius)*Mathf.Rad2Deg*dt);
  if(creatureHull!=null){
   float distance=step.magnitude;
   if(distance>.0001f){
    Vector3 center=entity.position+entity.rotation*Vector3.Scale(creatureHull.center,creatureHull.transform.lossyScale);
    Vector3 half=Vector3.Scale(creatureHull.size*.5f,creatureHull.transform.lossyScale);half=new Vector3(Mathf.Abs(half.x),Mathf.Abs(half.y),Mathf.Abs(half.z));
    int count=Physics.BoxCastNonAlloc(center,half,step/distance,creatureSweepHits,entity.rotation,distance,~0,QueryTriggerInteraction.Ignore);
    for(int hit=0;hit<count;hit++)if(creatureSweepHits[hit].rigidbody!=entity)distance=Mathf.Min(distance,Mathf.Max(0,creatureSweepHits[hit].distance-.02f));
    step=step.normalized*distance;
   }
   Vector3 proposed=entity.position+step;
   for(int slot=0;slot<setup.count;slot++)if(vessels[slot].gameObject.activeInHierarchy&&vesselHulls[slot]!=null)foreach(var hull in vesselHulls[slot]){
    if(hull==null||!hull.enabled||!hull.gameObject.activeInHierarchy)continue;
    if(!Physics.ComputePenetration(creatureHull,proposed,rotation,hull,hull.transform.position,hull.transform.rotation,out _,out float nextDepth))continue;
    bool alreadyOverlapping=Physics.ComputePenetration(creatureHull,entity.position,entity.rotation,hull,hull.transform.position,hull.transform.rotation,out _,out float currentDepth);
    if(!alreadyOverlapping||nextDepth>=currentDepth){rotation=entity.rotation;step=Vector3.zero;break;}
   }
  }
  entity.MovePosition(entity.position+step);entity.MoveRotation(rotation);
 }
 protected override void BeginWater(){
  nextStrike=8;targetSwitch=0;hunted=0;BankedSupplies=0;CooperativeEscaped=false;entity.position=new Vector3(0,-5,90);
  for(int i=0;i<4;i++){
   Integrity[i]=100;HullImpactHits[i]=0;ContactDamageTaken[i]=0;nextContactDamage[i]=0;healingAt[i]=null;vessels[i].flotation.waveAmplitude=.1f;Deliveries[i]=Rescues[i]=0;rescueProgress[i]=quietSeconds[i]=propulsionInput[i]=escapeUntil[i]=quietAttemptUntil[i]=0;boatCargo[i]=cargoOwner[i]=cargoTargets[i]=-1;banked[i]=false;
   if(cargoJoints[i]!=null)Destroy(cargoJoints[i]);cargoJoints[i]=null;
   if(!capturedCargoStarts)cargoStarts[i]=supplies[i].position;
   supplies[i].gameObject.SetActive(i<setup.count);supplies[i].isKinematic=false;supplies[i].position=cargoStarts[i];supplies[i].rotation=Quaternion.identity;supplies[i].linearVelocity=supplies[i].angularVelocity=Vector3.zero;
   supplies[i].GetComponent<BlackwaterSupply>().ResetContacts();
  }
  capturedCargoStarts=true;
 }
 // Native contacts provide both closing speed and the resolved impulse. A
 // fast glancing scrape or gentle docking touch alone cannot damage the hull.
 public static float ContactDamageFor(float closingSpeed,float impulsePerMass,float safeSpeed=3.5f,float minimumImpulse=.75f,float cap=20){
  if(float.IsNaN(closingSpeed)||float.IsInfinity(closingSpeed)||float.IsNaN(impulsePerMass)||float.IsInfinity(impulsePerMass)||closingSpeed<=safeSpeed||impulsePerMass<=minimumImpulse)return 0;
  return Mathf.Clamp((closingSpeed-safeSpeed)*2+(impulsePerMass-minimumImpulse)*2.5f,0,Mathf.Max(0,cap));
 }
 void ContactImpact(WaterVessel vessel,Collision collision){
  int index=System.Array.IndexOf(vessels,vessel);
  if(!Live||index<0||index>=setup.count||Integrity[index]<=0||Stranded[index]||Clock<nextContactDamage[index]||collision.rigidbody==entity||collision.contactCount==0)return;
  float closing=0;for(int contact=0;contact<collision.contactCount;contact++)closing=Mathf.Max(closing,Mathf.Abs(Vector3.Dot(collision.relativeVelocity,collision.GetContact(contact).normal)));
  float impulse=collision.impulse.magnitude/Mathf.Max(1,vessel.body.mass);
  float damage=ContactDamageFor(closing,impulse,safeContactSpeed,minimumContactImpulsePerMass,maximumContactDamage);
  if(damage<=0)return;
  nextContactDamage[index]=Clock+contactDamageCooldown;Integrity[index]=Mathf.Max(0,Integrity[index]-damage);HullImpactHits[index]++;ContactDamageTaken[index]+=damage;
  SportsSessionTelemetry.GameplayEvent(gameObject.scene.name,"blackwater-hull-contact-damage",setup.players[index].participantId,"normal_speed="+closing.ToString("F2")+"; impulse_per_mass="+impulse.ToString("F2")+"; collider="+collision.collider.name,damage);
 }
 protected override void OnDestroy(){if(vessels!=null)foreach(var vessel in vessels)if(vessel!=null)vessel.HullImpact-=ContactImpact;base.OnDestroy();}
 int NearestFreeCargo(Vector3 position){int found=-1;float nearest=float.MaxValue;for(int cargo=0;cargo<setup.count;cargo++)if(!banked[cargo]&&cargoOwner[cargo]<0){float distance=(supplies[cargo].position-position).sqrMagnitude;if(distance<nearest){nearest=distance;found=cargo;}}return found;}
 bool AvailableCargoTarget(int index,int cargo){
  if(cargo<0||cargo>=setup.count||banked[cargo]||cargoOwner[cargo]>=0||!supplies[cargo].gameObject.activeInHierarchy)return false;
  for(int other=0;other<setup.count;other++)if(other!=index&&Integrity[other]>0&&!Stranded[other]&&FinishTimes[other]<0&&setup.players[other].source==BowlingSource.AI&&cargoTargets[other]==cargo)return false;
  return true;
 }
 int CargoTarget(int index){
  // This is only an AI navigation intention. Ownership still requires the
  // original physical crate contact and real native FixedJoint below.
  if(AvailableCargoTarget(index,cargoTargets[index]))return cargoTargets[index];
  cargoTargets[index]=-1;float nearest=float.MaxValue;
  for(int cargo=0;cargo<setup.count;cargo++)if(AvailableCargoTarget(index,cargo)){float distance=(supplies[cargo].position-vessels[index].body.position).sqrMagnitude;if(distance<nearest){nearest=distance;cargoTargets[index]=cargo;}}
  return cargoTargets[index];
 }
 Vector3 CrossingApproach(int index){
  int gate=Gates[index];Vector3 incoming=Vector3.ProjectOnPlane(course.Point(gate)-course.Point(gate-1),Vector3.up).normalized;
  // The existing 18m gate remains unchanged. Seven-metre lanes give each
  // 5.5m hull/outboard paddle assembly room to pass the same physical buoy pair.
  return course.Point(gate)+Vector3.Cross(Vector3.up,incoming)*(index-(setup.count-1)*.5f)*7;
 }
 int NearestStranded(int rescuer){int found=-1;float nearest=float.MaxValue;for(int other=0;other<setup.count;other++)if(other!=rescuer&&Integrity[other]<=0){float distance=(vessels[other].body.position-vessels[rescuer].body.position).sqrMagnitude;if(distance<nearest){nearest=distance;found=other;}}return found;}
 bool LightShelter(Vector3 position){if(option==2)return false;if(Vector3.Distance(position,dock.position)<15)return true;foreach(var light in shelterLights)if(light!=null&&light.isActiveAndEnabled&&light.intensity>0&&Vector3.Distance(position,light.transform.position)<light.range)return true;return false;}
 Vector2 Bot(int index,WaterVessel vessel){
  int rescue=Cooperative?NearestStranded(index):-1;Vector3 goal;Rigidbody contact=null;
  if(rescue>=0){goal=vessels[rescue].body.position;contact=vessels[rescue].body;}
  else if(Cooperative&&BankedSupplies>=SharedSuppliesNeeded||boatCargo[index]>=0){goal=dock.position;contact=boatCargo[index]>=0?supplies[boatCargo[index]]:null;cargoTargets[index]=-1;}
  else if(option==1||Cooperative){int cargo=CargoTarget(index);goal=cargo>=0?supplies[cargo].position:dock.position;contact=cargo>=0?supplies[cargo]:null;}
  else goal=option==0?CrossingApproach(index):course.Point(Gates[index]);
  if(healingAt[index]!=null&&(!healingAt[index].isActiveAndEnabled||Integrity[index]>=90))healingAt[index]=null;if(rescue<0&&option!=2&&Integrity[index]<65&&healingAt[index]==null){float nearest=float.MaxValue;foreach(var light in shelterLights)if(light!=null&&light.isActiveAndEnabled&&light.intensity>0){float distanceToLight=(light.transform.position-vessel.body.position).sqrMagnitude;if(distanceToLight<nearest){nearest=distanceToLight;healingAt[index]=light;}}}if(healingAt[index]!=null&&rescue<0){goal=healingAt[index].transform.position;contact=boatCargo[index]>=0?supplies[boatCargo[index]]:null;}
  float distance=Vector3.Distance(vessel.body.position,goal);
  bool approachingSupply=rescue<0&&boatCargo[index]<0&&cargoTargets[index]>=0&&contact==supplies[cargoTargets[index]];
  float requestedSpeed=approachingSupply?Mathf.Lerp(1.2f,maximumPoweredSpeed,Mathf.InverseLerp(6,20,distance)):maximumPoweredSpeed;
  var drive=NavigateWatercraft(index,vessel,goal,requestedSpeed,contact);
  if(rescue>=0&&distance<9){float speed=Vector3.Dot(vessel.body.linearVelocity,vessel.transform.forward);drive.y=Mathf.Clamp((.45f-speed)*.8f,-.8f,.4f);}
  else if((healingAt[index]!=null||boatCargo[index]>=0||Cooperative&&BankedSupplies>=SharedSuppliesNeeded)&&distance<22){float desired=Mathf.Clamp((distance-5)*.28f,0,3);float speed=Vector3.Dot(vessel.body.linearVelocity,vessel.transform.forward);drive.y=Mathf.Clamp((desired-speed)*.8f,-.9f,.8f);}
  float reaction=8+(int)setup.players[index].difficulty*5;
  if(Clock>=25&&Clock>=escapeUntil[index]&&Vector3.Distance(vessel.body.position,entity.position)<reaction&&!LightShelter(vessel.body.position)){
   // A quiet ATTEMPT has a deadline independent of noise success. Real wakes
   // or braking pulses may interrupt quiet protection, but cannot make the AI
   // wait forever for five perfectly silent seconds before navigating again.
   if(quietAttemptUntil[index]<=0)quietAttemptUntil[index]=Clock+aiQuietHoldSeconds;
   if(Clock>=quietAttemptUntil[index]){escapeUntil[index]=Clock+aiCommittedEscapeSeconds;quietAttemptUntil[index]=0;}
   else{var planarForward=Vector3.ProjectOnPlane(vessel.transform.forward,Vector3.up).normalized;float signed=Vector3.Dot(vessel.body.linearVelocity,planarForward);drive.y=Mathf.Abs(signed)>.35f?-Mathf.Sign(signed)*.65f:0;}
  }
  else quietAttemptUntil[index]=0;
  return drive;
 }
 protected override void TickBoat(WaterVessel vessel,int index,float dt){
  propulsionInput[index]=0;
  if(FinishTimes[index]>=0||Stranded[index])return;
  if(Integrity[index]<=0){DropCargo(index);if(!Cooperative){FinishTimes[index]=Clock;Scores[index]=option==2?Clock:-1000;vessel.Freeze();}return;}
  bool ai=setup.players[index].source==BowlingSource.AI;var drive=ai?Bot(index,vessel):commands[index];propulsionInput[index]=drive.y;
  var forward=Vector3.ProjectOnPlane(vessel.transform.forward,Vector3.up).normalized;
  if(vessel.WaterSupported&&vessel.body.position.y<vessel.flotation.waterHeight+1.5f){
   // Heavy barges need enough thrust against native water drag to cross
   // the actual lake. Ease applied propulsion near top speed and preserve
   // full braking thrust in the opposite direction.
   float speed=Vector3.Dot(vessel.body.linearVelocity,forward);
   float taper=Mathf.Clamp01((maximumPoweredSpeed-speed*Mathf.Sign(drive.y))/1.5f);
   vessel.body.AddForce(forward*(drive.y*engineAcceleration*taper),ForceMode.Acceleration);
  }
  vessel.SteerWatercraft(drive.x);
  if(Action(index)||vessel.Capsized&&ai)vessel.Recover();
  if(option==1||Cooperative)Cargo(index,vessel);
  else if(option==0)CheckGate(index,vessel,18);
  else{Scores[index]=Clock;if(Vector3.Distance(vessel.body.position,course.Point(Gates[index]))<18)Gates[index]=(Gates[index]+1)%course.stations.Length;}
  if(LightShelter(vessel.body.position))Integrity[index]=Mathf.Min(100,Integrity[index]+dt*3);
 }
 void Cargo(int index,WaterVessel vessel){
  if(boatCargo[index]>=0&&cargoJoints[index]==null){cargoOwner[boatCargo[index]]=-1;boatCargo[index]=-1;}
  if(boatCargo[index]<0)for(int cargo=0;cargo<setup.count;cargo++){
   if(banked[cargo]||cargoOwner[cargo]>=0||!supplies[cargo].GetComponent<BlackwaterSupply>().Touching(vessel))continue;
   var joint=supplies[cargo].gameObject.AddComponent<FixedJoint>();joint.connectedBody=vessel.body;joint.breakForce=30000;joint.enableCollision=false;
   cargoJoints[index]=joint;boatCargo[index]=cargo;cargoOwner[cargo]=index;cargoTargets[index]=-1;
   SportsSessionTelemetry.GameplayEvent(Title,"supply-coupled",setup.players[index].participantId,"native crate contact",cargo);break;
  }
  int carried=boatCargo[index];if(carried<0)return;
  if(Vector3.Distance(vessel.body.position,dock.position)>=13||Vector3.Distance(supplies[carried].position,dock.position)>=15||vessel.body.linearVelocity.magnitude>=2.5f)return;
  Destroy(cargoJoints[index]);cargoJoints[index]=null;supplies[carried].linearVelocity=supplies[carried].angularVelocity=Vector3.zero;supplies[carried].isKinematic=true;
  banked[carried]=true;cargoOwner[carried]=-1;boatCargo[index]=-1;Deliveries[index]++;BankedSupplies++;
  Scores[index]=Cooperative?Deliveries[index]*1000+Rescues[index]*200:10000-Clock;
  if(!Cooperative)FinishTimes[index]=Clock;
  ConfirmOutcome("supply:"+carried,index,1,"SUPPLY DELIVERED","DELIVERY");
 }
 void DropCargo(int index){int cargo=boatCargo[index];if(cargo<0)return;if(cargoJoints[index]!=null)Destroy(cargoJoints[index]);cargoJoints[index]=null;cargoOwner[cargo]=-1;boatCargo[index]=-1;}
 void Rescue(float dt){
  for(int victim=0;victim<setup.count;victim++){
   if(Integrity[victim]>0){rescueProgress[victim]=0;continue;}
   int rescuer=-1;
   for(int i=0;i<setup.count;i++)if(i!=victim&&Integrity[i]>0&&vessels[i].Touching(vessels[victim])&&(vessels[i].body.linearVelocity-vessels[victim].body.linearVelocity).magnitude<1.8f&&(setup.players[i].source==BowlingSource.AI||ActionHeld(i))){rescuer=i;break;}
   if(rescuer<0){rescueProgress[victim]=0;continue;}
   rescueProgress[victim]+=dt;if(rescueProgress[victim]<2)continue;
   // An off-lake teammate must first be physically returned to supported water.
   // Clearing the common stranded state is valid only after that real recovery;
   // otherwise the next shared tick would immediately strand the same boat again.
   if(Stranded[victim]&&!vessels[victim].WaterSupported){rescueProgress[victim]=0;continue;}
   Integrity[victim]=40;Stranded[victim]=false;StrandedAt[victim]=-1;rescueProgress[victim]=0;Rescues[rescuer]++;Scores[rescuer]=Deliveries[rescuer]*1000+Rescues[rescuer]*200;vessels[victim].body.WakeUp();
   ConfirmOutcome("rescue:"+rescuer+":"+victim+":"+Rescues[rescuer],rescuer,1,"TEAMMATE RECOVERED","RESCUE");
  }
 }
 bool QuietMovement(int slot)=>Vector3.ProjectOnPlane(vessels[slot].body.linearVelocity,Vector3.up).sqrMagnitude<.8f*.8f&&Mathf.Abs(propulsionInput[slot])<.1f;
 protected override void AfterBoats(float dt){
  // Passive vertical buoyancy/wakes are not engine or travel noise. An actual
  // throttle request still reveals a stationary player pressing into a wall.
  for(int i=0;i<setup.count;i++){if(Stranded[i])Integrity[i]=0;quietSeconds[i]=QuietMovement(i)?quietSeconds[i]+dt:0;vessels[i].flotation.waveAmplitude=Mathf.MoveTowards(vessels[i].flotation.waveAmplitude,.1f,dt*.15f);}
  if(Cooperative){
   Rescue(dt);bool anyAlive=false,allReady=BankedSupplies>=SharedSuppliesNeeded;
   for(int i=0;i<setup.count;i++){anyAlive|=Integrity[i]>0;allReady&=Integrity[i]>0&&!Stranded[i]&&!vessels[i].Capsized&&vessels[i].body.linearVelocity.magnitude<2.5f&&Vector3.Distance(vessels[i].body.position,dock.position)<22;}
   if(allReady){CooperativeEscaped=true;for(int i=0;i<setup.count;i++)FinishTimes[i]=Clock;}
   else if(!anyAlive)for(int i=0;i<setup.count;i++){FinishTimes[i]=Clock;Scores[i]=-1000;}
  }
  Hunt(dt);if(wake!=null)for(int i=0;i<setup.count;i++)vessels[i].flotation.waterHeight=wake.Height(vessels[i].body.position,Clock);
 }
 void Hunt(float dt){
  if(Clock>=targetSwitch){targetSwitch=Clock+12;float loudest=-1;for(int i=0;i<setup.count;i++){float sound=Vector3.ProjectOnPlane(vessels[i].body.linearVelocity,Vector3.up).sqrMagnitude+Mathf.Abs(propulsionInput[i]);if(!Stranded[i]&&Integrity[i]>0&&FinishTimes[i]<0&&sound>loudest){loudest=sound;hunted=i;}}}
  var victim=vessels[hunted];if(Integrity[hunted]<=0||FinishTimes[hunted]>=0)return;
  bool quiet=QuietMovement(hunted)&&quietSeconds[hunted]<7,sheltered=LightShelter(victim.body.position);
  // Keep the crown below every nearby real hull, not just the current target:
  // a target switch must not sweep the 24m body through a lower neighbouring
  // barge or paddle. The physical sweep below still retains all contacts.
  Vector3 desired=victim.body.position;
  float lowestHull=Mathf.Min(course.Point(0).y,HullBottom(hunted));
  for(int i=0;i<setup.count;i++)if(vessels[i].gameObject.activeInHierarchy&&Vector3.ProjectOnPlane(vessels[i].body.position-entity.position,Vector3.up).sqrMagnitude<Mathf.Pow(creatureTurnRadius+entitySpeed+6,2))lowestHull=Mathf.Min(lowestHull,HullBottom(i));
  desired.y=lowestHull-creatureCrown-.6f;
  if(Clock<35||quiet||sheltered)desired+=Vector3.ProjectOnPlane(victim.transform.forward,Vector3.up).normalized*30;
  MoveCreature(desired,dt);
  float proximity=Vector3.Distance(entity.position,victim.body.position);
  if(proximity<18){victim.body.AddForce((victim.body.position-entity.position).normalized*Mathf.Sin(Clock*2)*2,ForceMode.Acceleration);victim.flotation.waveAmplitude=.1f+(18-proximity)*.035f;}else victim.flotation.waveAmplitude=.1f;
  if(proximity>=7||Clock<nextStrike||Clock<35||quiet||sheltered)return;
  nextStrike=Clock+11;victim.body.AddForce(Vector3.up*5+entity.transform.right*3,ForceMode.VelocityChange);victim.body.AddTorque(entity.transform.forward*1.5f,ForceMode.VelocityChange);Integrity[hunted]-=25;
  if(warning!=null&&warning.clip!=null){warning.transform.position=entity.position;warning.volume=settings.sfx*.6f;warning.Play();}
  SportsSessionTelemetry.GameplayEvent(Title,"entity-strike",setup.players[hunted].participantId,"physical lift",Integrity[hunted]);
 }
 public override bool TryReadScoreCard(int slot,out string score,out string detail){score=Scores[slot].ToString("F0");detail=Stranded[slot]?"STRANDED · LEFT THE LAKE":Mathf.CeilToInt(Integrity[slot])+"% HULL · "+Deliveries[slot]+" SUPPLIES"+(Cooperative?" · "+Rescues[slot]+" RESCUES":"");return slot<setup.count;}
 protected override void RecordModeFinalMetrics(int slot){FinalMetric(slot,"supplies_delivered",Deliveries[slot],"individual");FinalMetric(slot,"hull_condition_percent",Integrity[slot],"individual");FinalMetric(slot,"hull_contact_damage",ContactDamageTaken[slot],"individual");FinalMetric(slot,"hull_damaging_contacts",HullImpactHits[slot],"individual");if(option==2)FinalMetric(slot,"survival_seconds",Stranded[slot]?StrandedAt[slot]:FinishTimes[slot]>=0?FinishTimes[slot]:Clock,"individual");if(Cooperative){FinalMetric(slot,"teammates_rescued",Rescues[slot],"individual");FinalMetric(slot,"cooperative_escape",CooperativeEscaped?1:0,"shared_convoy");}}
 protected override void StopGame(){base.StopGame();if(supplies!=null)foreach(var cargo in supplies)if(cargo!=null){if(!cargo.isKinematic)cargo.linearVelocity=cargo.angularVelocity=Vector3.zero;cargo.isKinematic=true;}}
 protected override void StopSounds(){if(warning!=null)warning.Stop();}
}
}
