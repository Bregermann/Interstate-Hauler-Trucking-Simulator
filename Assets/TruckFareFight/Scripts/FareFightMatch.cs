using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using LWS.TruckSports;
using LWS.TruckBowling;
using LWS.TruckTaxi;
namespace LWS.TruckFareFight {
 public sealed class FareFightMatch:SportsEventSession {
 public override void DescribeBroadcast(SportsBroadcastAction a,SportsBroadcastFraming f){int slot=f.Nearest(f.RequestedFocus,true);if(slot<0)slot=rules.Carrier>=0?rules.Carrier:f.Nearest(f.RequestedFocus);a.primary=slot;if(rules.Carrier==slot&&rules.Destination>=0)a.Objective(district.stops[rules.Destination].position,2);else if(rules.Carrier>=0)a.Objective(trucks[rules.Carrier].body.position,1);else if(rules.PickupLocation>=0)a.Objective(district.stops[rules.PickupLocation].position,1);a.reason=rules.Carrier==slot?"PASSENGER DELIVERY":rules.Carrier>=0?"MOVING PASSENGER CARRIER":"CONTESTED PICKUP";}
  public FareFightWorldReset worldReset;public FareFightDistrict district;public SportsEventTruck[] trucks;
  public SportsDrivingViews drivingViews;public SportsEventCamera broadcast;
  public SportsChaosWeather atmosphere;public SportsWeatherPresets presets;
  public TruckTaxiTrafficAdapter traffic;public TruckTaxiPedestrianPopulation pedestrians;
  public Transform pickupMarker,destinationMarker,carrierMarker;public GameObject passengerVisual;
  public FareFightHazards hazards;public FareFightActions interactions;public FareFightNavigation navigation;
  public FareFightReview review;public SportsMotionReplay replay;public int matchSeed;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
  [NonSerialized]public int validationSeed;
#endif
  public float triggerRadius=16;
  public readonly FareFightRules rules=new FareFightRules();
  public readonly FareFightRoute[] routes={new FareFightRoute(),new FareFightRoute(),new FareFightRoute(),new FareFightRoute()};
  public int PhysicalContacts{get;private set;}public int Recoveries{get;private set;}public float Intro{get;private set;}
  public override string Title=>"TRUCK FARE FIGHT";public override int MinimumPlayers=>1;public override int MaximumPlayers=>4;
  public override int OptionCount=>5;public override string OptionLabel=>new[]{"FIRST TO THREE","FIRST TO FIVE","FIRST TO TEN","FIVE MINUTES","SOLO PRACTICE"}[option];
  public override bool ReplayControls=>review!=null&&review.Playing;public override bool SupportsSplitAIViews=>true;public override bool MultiViewHUD=>!drivingViews.NeedsArena;
  public override bool BroadcastStandingsVisible=>!MultiViewHUD;public override bool UsesAction=>false;
  public override string Help=>"ONE PASSENGER. FOUR TAXIS. NO MANNERS.\n\nDrive through the blue roadside pickup marker. The first qualifying truck collects automatically. Drive through the gold destination marker to score. Stopping and interaction menus are never required.\n\nBump the carrier at a meaningful closing speed to steal the SAME passenger and destination. A visible 1.25-second lock prevents contact ping-pong. Ordinary traffic, pedestrians and explosions never steal. A destroyed or out-of-bounds carrier resets the passenger at a neutral roadside stop; the destination survives. Shallow surface water is safe.\n\nFirst to five by default. Choose another target, five minutes (tied leaders play a sudden-death delivery), or solo practice. Global names, colors and controller ownership are retained.\n\nUse the setup or pause VIEW button for shared arena, chase or the actual Taxi cab. Each whole controller may switch its own view with NORTH; shared sticks preserve independent steering and use the setup view. Keyboard clusters use C, End, F, J for camera. Hold and cue the jug using the displayed source bindings, then throw a filled jug using the displayed action. Its blast never awards possession. Flip-off is a separate action.\n\nThe playable district is the original authored Taxi town. Road barriers visibly close the later regional connections. GPS uses its actual connected road graph; it never routes through a different district. Offline only.";
  public override string Header=>Title+" · "+(rules.SuddenDeath?"SUDDEN DEATH":rules.TimeLimit>0?Mathf.CeilToInt(Mathf.Max(0,rules.TimeLimit-rules.Clock))+"s":option==4?"PRACTICE":"FIRST TO "+rules.Target)+" · "+(atmosphere!=null?atmosphere.Description:"");
  public override string Status=>Intro>0?"GET READY · "+Mathf.CeilToInt(Intro):noticeUntil>Time.time?notice:rules.Carrier>=0?setup.players[rules.Carrier].name+" HAS "+PassengerName+" · "+district.stops[rules.Destination].label+(rules.TransferCooldown>0?" · TRANSFER LOCK "+rules.TransferCooldown.ToString("F1")+"s":""):"PICKUP · "+PassengerName+" · "+(rules.PickupLocation>=0?district.stops[rules.PickupLocation].label:"");
  public string PassengerName=>new[]{"Margo","Eddie","Jules","Finley","Robin","Pat","Sasha","Cameron"}[(Mathf.Max(1,rules.Passenger)-1)%8];
  readonly StringBuilder text=new StringBuilder();readonly List<FareFightEvent> events=new List<FareFightEvent>(24);
  readonly Vector3[] previous=new Vector3[4];readonly float[] recoverAt=new float[4],badTime=new float[4];
  readonly Bounds[] recoveryBounds=new Bounds[4];bool recoveryBoundsReady;
  readonly FareFightStartingGrid startingGrid=new FareFightStartingGrid();
  readonly Vector3[] fairPositions=new Vector3[4],fairForwards=new Vector3[4];
  public readonly float[] InitialRouteDistances=new float[4];
  readonly Collider[] spawnContacts=new Collider[96];
  System.Random random;int lastRevision=-1,rematchSerial,lastPickup=-1,lastDestination=-1;float noticeUntil;string notice;
  public override string Standings{get{text.Clear();for(int i=0;i<setup.count;i++)text.Append(setup.players[i].name).Append("  ").Append(rules.score[i]).Append(" FARES").Append(i==rules.Carrier?" · CARRIER":"").Append('\n');return text.ToString();}}
  public override string Results{get{text.Clear();for(int i=0;i<setup.count;i++)if(rules.winners[i])text.Append(setup.players[i].name).Append("  ");return text+"WIN FARE FIGHT!\n\n"+Standings;}}
  public override string ViewText(int view){int slot=drivingViews.rally.owners[view];var body=trucks[slot].body;float distance=Vector3.Distance(body.position,Goal(slot));return setup.players[slot].name+" · "+rules.score[slot]+" FARES\n"+(body.linearVelocity.magnitude*2.236936f).ToString("F0")+" MPH · "+DistanceText(distance)+"\n"+(slot==rules.Carrier?"DELIVER ":rules.Carrier>=0?"CHASE ":"PICK UP ")+PassengerName+"\n"+(interactions!=null?interactions.HUDCaption(slot):"");}
  void CompactViewHUD(){
   for(int view=0;view<menus.viewTexts.Length;view++){
    var text=menus.viewTexts[view];text.rectTransform.sizeDelta=new Vector2(510,108);
    var backing=text.transform.parent.Find("View data backing") as RectTransform;
    if(backing!=null){backing.sizeDelta=new Vector2(530,120);if(backing.GetComponent<SportsBroadcastPlate>()==null)backing.gameObject.AddComponent<SportsBroadcastPlate>().cut=10;}
   }
  }
  public static string DistanceText(float metres)=>metres>1609.344f?(metres/1609.344f).ToString("F1")+" MI":Mathf.RoundToInt(metres*3.28084f)+" FT";
  public override bool TryReadScoreCard(int i,out string value,out string detail){value=rules.score[i].ToString();detail=(i==rules.Carrier?"CARRYING · ":"")+rules.steals[i]+" STEALS";return true;}
  public Vector3 Goal(int slot)=>rules.Carrier==slot&&rules.Destination>=0?district.stops[rules.Destination].position:rules.Carrier>=0?trucks[rules.Carrier].body.position:rules.PickupLocation>=0?district.stops[rules.PickupLocation].position:trucks[slot].body.position;
  public string ObjectiveState(int slot)=>recoverAt[slot]>0?"RECOVER TRUCK":routes[slot].Reversing?"RECOVER BLOCKED PATH":rules.Carrier==slot?"DELIVER PASSENGER":rules.Carrier>=0?"STEAL PASSENGER":"SEEK PASSENGER";
  protected override void BeginGame(){
   if(!recoveryBoundsReady){for(int i=0;i<trucks.Length;i++)recoveryBounds[i]=RecoveryBounds(trucks[i]);recoveryBoundsReady=true;}
   for(int slot=0;slot<routes.Length;slot++){routes[slot].useGoalProgressRecovery=true;routes[slot].useRoadConnectors=true;routes[slot].stallSeconds=2;routes[slot].reverseSeconds=1.4f;routes[slot].minimumReverseDistance=3;routes[slot].obstacleRadius=Mathf.Clamp(trucks[slot].shape.size.x*trucks[slot].transform.lossyScale.x*.45f,.8f,1.8f);}
   worldReset?.Restore();matchSeed=UnityEngine.Random.Range(1,int.MaxValue);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
   if(validationSeed>0)matchSeed=validationSeed;
#endif
   random=new System.Random(matchSeed);rematchSerial++;
   rules.Start(setup.count,option==0?3:option==2?10:5,option==3?300:0,option==4);PhysicalContacts=Recoveries=0;events.Clear();lastRevision=-1;lastPickup=lastDestination=-1;Intro=3;
   atmosphere.ConfigurePreset(presets.Resolve(matchSeed),matchSeed);
   AssignFare();Physics.SyncTransforms();float separation=12;foreach(var truck in trucks)separation=Mathf.Max(separation,truck.shape.size.z*truck.transform.lossyScale.z+3);
   if(!startingGrid.TryBuild(district.graph,district.stops[rules.PickupLocation].position,setup.count,separation,rematchSerial-1,fairPositions,fairForwards,InitialRouteDistances,ClearStartingRoad))throw new InvalidOperationException("Fare Fight cannot find separated, clear, equal-road-distance starts: "+startingGrid.Diagnostic);
   for(int i=0;i<4;i++){routes[i].Reset();recoverAt[i]=badTime[i]=0;if(i>=setup.count){trucks[i].Park(new Vector3(0,-100-i*12,0));trucks[i].gameObject.SetActive(false);continue;}trucks[i].tractorMotor.Configure();RepairTruck(trucks[i]);trucks[i].Prepare(fairPositions[i],Quaternion.LookRotation(fairForwards[i]));trucks[i].body.isKinematic=true;previous[i]=trucks[i].body.position;}
   if(traffic!=null){traffic.SetRegionAvailability(InitialPopulationAllowed);traffic.Initialize();if(rematchSerial>1)traffic.ResetTraffic();traffic.SetWeatherCaution(1-atmosphere.Grip);}
   if(pedestrians!=null){pedestrians.SetRegionAvailability(InitialPopulationAllowed);pedestrians.Initialize();if(rematchSerial>1)pedestrians.ResetPopulation();}
   hazards?.ResetMatch();interactions?.ResetMatch();navigation?.ResetMatch();drivingViews.Layout();CompactViewHUD();
  }
  bool InitialPopulationAllowed(Vector3 point){if(!district.Contains(point))return false;if(Intro>0)for(int i=0;i<setup.count;i++)if(Vector3.Distance(point,fairPositions[i])<20)return false;return true;}
  bool ClearStartingRoad(Vector3 point,Vector3 forward){if(!district.Contains(point))return false;var truck=trucks[0];var rotation=Quaternion.LookRotation(forward);var scale=truck.transform.lossyScale;var center=point+rotation*Vector3.Scale(truck.shape.center,scale);var half=Vector3.Scale(truck.shape.size,scale)*.55f;int count=Physics.OverlapBoxNonAlloc(center,half,spawnContacts,rotation,~0,QueryTriggerInteraction.Ignore);if(count==spawnContacts.Length)return false;for(int i=0;i<count;i++){var hit=spawnContacts[i];if(hit.bounds.max.y<=point.y+.25f||hit.GetComponentInParent<SportsEventTruck>()!=null||hit.GetComponentInParent<FareFightHazard>()!=null||traffic!=null&&hit.transform.IsChildOf(traffic.transform)||pedestrians!=null&&hit.transform.IsChildOf(pedestrians.transform))continue;return false;}return true;}
  void AssignFare(){if(!rules.NeedsPassenger)return;int pickup=random.Next(district.stops.Length);for(int n=0;n<district.stops.Length;n++){if(pickup!=lastPickup&&pickup!=lastDestination)break;pickup=(pickup+1)%district.stops.Length;}int destination=-1;for(int n=0;n<100;n++){int candidate=random.Next(district.stops.Length);if(candidate!=pickup&&candidate!=lastDestination&&Vector3.Distance(district.stops[pickup].position,district.stops[candidate].position)>230){destination=candidate;break;}}if(destination<0)throw new InvalidOperationException("Fare Fight needs separated reachable original-town stop pairs.");rules.AssignPassenger(pickup,destination);lastPickup=pickup;lastDestination=destination;}
  protected override void Simulate(float dt){
   atmosphere.Tick(Time.time);
   if(Intro>0){Intro-=dt;if(Intro<=0){if(review==null){review=gameObject.AddComponent<FareFightReview>();review.match=this;}review.ResetRecord();for(int i=0;i<setup.count;i++){trucks[i].body.isKinematic=false;trucks[i].body.WakeUp();}feedback.Play(5,0,true);}return;}
   AssignFare();hazards?.Tick();
   for(int i=0;i<setup.count;i++){
    var truck=trucks[i];var body=truck.body;
    if(recoverAt[i]>0){if(Time.time>=recoverAt[i])Recover(i);previous[i]=body.position;continue;}
    // NWH damage can destroy a powertrain while the chassis remains upright.
    // Use the same announced recovery/passenger reset for humans and AI;
    // repeatedly reversing a destroyed engine can never free a blocked truck.
    if(truck.tractorMotor.vehicle.powertrain.engine.Damage>=1||truck.tractorMotor.vehicle.powertrain.transmission.Damage>=1)DestroyTruck(i);
    else if(!district.Contains(body.position)){Queue(FareFightEventKind.CarrierReset,i,1);BeginRecovery(i,"OUT OF BOUNDS");}
    else if(body.position.y<-3){badTime[i]+=dt;if(badTime[i]>2.5f){Queue(FareFightEventKind.CarrierReset,i,1);BeginRecovery(i,"UNRECOVERABLE WATER");}}
    else if(Vector3.Dot(body.rotation*Vector3.up,Vector3.up)<.2f&&body.linearVelocity.magnitude<1){badTime[i]+=dt;if(badTime[i]>6){Queue(FareFightEventKind.CarrierReset,i,1);BeginRecovery(i,"OVERTURNED");}}
    else badTime[i]=0;
    if(recoverAt[i]>0)continue;
    if(rules.Phase==FareFightPhase.Waiting&&rules.PickupLocation>=0&&Crosses(previous[i],body.position,district.stops[rules.PickupLocation].position,triggerRadius,out float pickup))Queue(FareFightEventKind.Pickup,i,pickup);
    // Observe every slot: an earlier physical theft in this same step may make
    // it the carrier before a later destination crossing is resolved.
    if(rules.Destination>=0&&Crosses(previous[i],body.position,district.stops[rules.Destination].position,triggerRadius,out float drop))Queue(FareFightEventKind.Delivery,i,drop);
    routes[i].Update(district,body.position,Goal(i),rules.Revision,Time.time,false,truck.transform.forward);
    Vector2 drive=setup.players[i].source==BowlingSource.AI?routes[i].Drive(body,truck.transform,(int)setup.players[i].difficulty,dt,atmosphere.Grip,rules.Carrier>=0&&rules.Carrier!=i?trucks[rules.Carrier].body:null):commands[i];truck.TickDrive(drive);previous[i]=body.position;
   }
   int revision=rules.Revision;rules.Advance(dt,events);events.Clear();if(rules.Revision!=revision)Changed();
   interactions?.Tick(dt);navigation?.Tick(dt);review?.Capture(rules.Clock);
   if(rules.Phase==FareFightPhase.Complete)EndMatch();
  }
  void Queue(FareFightEventKind kind,int slot,float fraction,int victim=-1,float speed=0){events.Add(new FareFightEvent{kind=kind,participant=slot,victim=victim,fraction=fraction,normalSpeed=speed,passenger=rules.Passenger});}
  public void Contact(int first,int second,float speed,Vector3 point){if(!Live||Intro>0||rules.Carrier<0)return;PhysicalContacts++;int thief=rules.Carrier==first?second:rules.Carrier==second?first:-1;if(thief<0)return;int carrier=rules.Carrier;Vector3 path=trucks[carrier].body.position-previous[carrier];float fraction=path.sqrMagnitude>.0001f?Mathf.Clamp01(Vector3.Dot(point-previous[carrier],path)/path.sqrMagnitude):.5f;Queue(FareFightEventKind.Theft,thief,fraction,carrier,speed);}
  void Changed(){lastRevision=rules.Revision;notice=rules.LastEvent+(rules.Carrier>=0?" · "+setup.players[rules.Carrier].name:"");noticeUntil=Time.time+2;feedback.Play(rules.LastEvent=="DELIVERED"?5:2,0,true);menus.Dirty();}
  void BeginRecovery(int slot,string reason){if(recoverAt[slot]>0)return;recoverAt[slot]=Time.time+2;trucks[slot].Power(false);notice=setup.players[slot].name+" · "+reason;noticeUntil=Time.time+2;}
  // Repair at lifecycle boundaries through the vendor API. Do not change
  // damage tuning or silently heal impacts during active competition.
  static void RepairTruck(SportsEventTruck truck)=>truck.GetComponent<NWH.VehiclePhysics2.Damage.DamageHandler>()?.Repair();
  // Cache the complete authored assembly while upright, before the first race.
  // Include compound cab/chassis colliders, not just the event chassis box.
  static Bounds RecoveryBounds(SportsEventTruck truck){
   var bounds=new Bounds(truck.shape.center,truck.shape.size);
   foreach(var collider in truck.GetComponentsInChildren<Collider>(true)){
    if(collider.isTrigger)continue;
    Bounds local;var box=collider as BoxCollider;var mesh=collider as MeshCollider;
    if(box!=null)local=new Bounds(box.center,box.size);
    else if(mesh!=null&&mesh.sharedMesh!=null)local=mesh.sharedMesh.bounds;
    else{if(!collider.enabled||collider.bounds.size.sqrMagnitude<.001f)continue;var world=collider.bounds;for(int corner=0;corner<8;corner++)bounds.Encapsulate(truck.transform.InverseTransformPoint(world.center+Vector3.Scale(world.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1))));continue;}
    for(int corner=0;corner<8;corner++)bounds.Encapsulate(truck.transform.InverseTransformPoint(collider.transform.TransformPoint(local.center+Vector3.Scale(local.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1)))));
   }return bounds;
  }
  bool ClearRecoveryRoad(int slot,Vector3 point,Quaternion rotation){
   var truck=trucks[slot];var scale=truck.transform.lossyScale;var envelope=recoveryBounds[slot];var half=Vector3.Scale(envelope.extents,scale)+new Vector3(.35f,.15f,.5f);var center=point+rotation*Vector3.Scale(envelope.center,scale);
   for(int corner=0;corner<4;corner++)if(!district.Contains(center+rotation*new Vector3((corner&1)==0?-half.x:half.x,0,(corner&2)==0?-half.z:half.z)))return false;
   // A recovery never grants an immediate pickup/delivery by materializing in
   // its trigger. The passenger's assigned destination remains authoritative.
   if(rules.PickupLocation>=0&&Vector3.Distance(point,district.stops[rules.PickupLocation].position)<triggerRadius+half.z)return false;
   if(rules.Destination>=0&&Vector3.Distance(point,district.stops[rules.Destination].position)<triggerRadius+half.z)return false;
   int count=Physics.OverlapBoxNonAlloc(center,half,spawnContacts,rotation,~0,QueryTriggerInteraction.Ignore);if(count==spawnContacts.Length)return false;
   for(int i=0;i<count;i++){var hit=spawnContacts[i];if(hit.transform.IsChildOf(truck.transform)||hit.bounds.max.y<=point.y+.1f)continue;return false;}return true;
  }
  bool RecoveryPose(int slot,out Vector3 point,out Quaternion rotation){
   point=default;rotation=Quaternion.identity;float nearest=float.PositiveInfinity;var origin=trucks[slot].body.position;
   // Existing original-town authored road samples own placement and heading.
   // Opposing one-way edges supply both legal directions; no new road graph.
   foreach(var edge in district.graph.edges){bool allowed=true;if(edge.restrictions!=null)foreach(var restriction in edge.restrictions)if(restriction!=null&&!restriction.truckAllowed)allowed=false;if(!allowed||edge.samples==null)continue;
    for(int i=0;i<edge.samples.Count;i++){var sample=edge.samples[i];var forward=Vector3.ProjectOnPlane(sample.forward,Vector3.up).normalized;if(forward.sqrMagnitude<.9f&&i+1<edge.samples.Count)forward=Vector3.ProjectOnPlane(edge.samples[i+1].position-sample.position,Vector3.up).normalized;if(forward.sqrMagnitude<.9f)continue;
     var candidate=sample.position+Vector3.Cross(Vector3.up,forward)*2+Vector3.up*.2f;float distance=(candidate-origin).sqrMagnitude;if(distance>=nearest)continue;var facing=Quaternion.LookRotation(forward);if(!ClearRecoveryRoad(slot,candidate,facing))continue;nearest=distance;point=candidate;rotation=facing;
    }
   }return !float.IsPositiveInfinity(nearest);
  }
  void Recover(int slot){
   Physics.SyncTransforms();if(!RecoveryPose(slot,out var point,out var rotation)){recoverAt[slot]=Time.time+.5f;return;}
   RepairTruck(trucks[slot]);trucks[slot].Prepare(point,rotation);recoverAt[slot]=badTime[slot]=0;previous[slot]=trucks[slot].body.position;routes[slot].Reset();Recoveries++;
  }
  public void DestroyTruck(int slot){if(!Live||recoverAt[slot]>0)return;Queue(FareFightEventKind.CarrierReset,slot,1);BeginRecovery(slot,"WRECKED · PASSENGER RESET");}
  public override void ExtraAction(string key){if(key=="drivingview")drivingViews.Cycle();else review?.Action(key);}
  public override string ControlCaption(int slot)=>Controls(slot);
  bool helpShowing;
  void RefreshAssignedHelp(){bool showing=menus.Screen==SportsEventScreen.Help;if(showing&&!helpShowing){interactions?.RefreshBindings();var label=menus.pages[4].transform.Find("Rules and controls").GetComponent<TMPro.TMP_Text>();var details=new StringBuilder(Help).Append("\n\nASSIGNED CONTROLS");for(int slot=0;slot<setup.count;slot++)if(setup.players[slot].source!=BowlingSource.AI)details.Append("\n").Append(setup.players[slot].name).Append(" · ").Append(Controls(slot)).Append("\n").Append(interactions!=null?interactions.Caption(slot):"");label.text=details.ToString();label.fontSizeMin=18;label.fontSizeMax=24;}helpShowing=showing;}
  protected override void Present(float dt){RefreshAssignedHelp();if(drivingViews==null)return;if(review!=null&&review.Playing){review.Tick();return;}drivingViews.Tick(dt);if(drivingViews.NeedsArena){Vector3 focus=rules.Carrier>=0?trucks[rules.Carrier].body.position:rules.PickupLocation>=0?district.stops[rules.PickupLocation].position:Vector3.zero;broadcast.Pose(focus,new Vector3(0,145,-100),65);}bool pickup=Running&&rules.Phase==FareFightPhase.Waiting&&rules.PickupLocation>=0;pickupMarker.gameObject.SetActive(pickup);passengerVisual.SetActive(pickup);if(pickup){pickupMarker.position=district.stops[rules.PickupLocation].position;passengerVisual.transform.position=pickupMarker.position+Vector3.up*.15f;}bool destination=Running&&rules.Destination>=0;destinationMarker.gameObject.SetActive(destination);if(destination)destinationMarker.position=district.stops[rules.Destination].position;carrierMarker.gameObject.SetActive(Running&&rules.Carrier>=0);if(rules.Carrier>=0)carrierMarker.position=trucks[rules.Carrier].body.position+Vector3.up*5;}
  protected override void SetPaused(bool pause){atmosphere?.Pause(pause);interactions?.Pause(pause);}
  protected override void StopSounds(){interactions?.Pause(true);if(trucks!=null)foreach(var truck in trucks)truck?.StopSound();}
  protected override void StopGame(){review?.End(false);if(trucks!=null)foreach(var truck in trucks)if(truck!=null){truck.Power(false);truck.body.isKinematic=true;}interactions?.Pause(true);}
  public static bool Crosses(Vector3 from,Vector3 to,Vector3 center,float radius,out float fraction){from.y=to.y=center.y=0;Vector3 delta=to-from,relative=from-center;float c=relative.sqrMagnitude-radius*radius;if(c<=0){fraction=0;return true;}float a=delta.sqrMagnitude,b=Vector3.Dot(relative,delta),disc=b*b-a*c;if(a<.000001f||disc<0){fraction=0;return false;}fraction=(-b-Mathf.Sqrt(disc))/a;return fraction>=0&&fraction<=1;}
 }
}
