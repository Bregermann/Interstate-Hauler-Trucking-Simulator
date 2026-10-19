using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AI;
using LWS.TruckSports;
using LWS.TruckBowling;
namespace LWS.TruckPedestrianPlowing {
 public class PlowMatch:SportsEventSession,IPlowCrowdContext {
  PlowSettings IPlowCrowdContext.CrowdSettings=>crowdSettings;
  int IPlowCrowdContext.ThreatCount=>setup.count;
  Rigidbody IPlowCrowdContext.ThreatBody(int index)=>trucks[index].body;
  Collider[] IPlowCrowdContext.ClimbSurfaces=>climbSurfaces;

  public bool cabExperience;public PlowCabExtras cabExtras;public PlowSettings crowdSettings;public SportsEventTruck[] trucks;public Transform[] truckStarts;
  public PlowSpawnZone[] spawnZones;public Transform crowdRoot;public PlowViews views;public PlowHUD hud;
  public PlowImpactPresentation impactPresentation;public ParticleSystem[] hitDust;int nextDust;public PlowProp[] props;public Bounds arenaBounds=new Bounds(Vector3.zero,new Vector3(142,20,142));
  public Collider[] climbSurfaces=Array.Empty<Collider>();
  public PlowRound Round{get;private set;}public PlowPedestrian[] Crowd=>crowd;
  public bool ProbeHandsOff;public float Intro{get;private set;}public int PhysicalHits{get;private set;}
  public override string Title=>"TRUCK PEDESTRIAN PLOWING";
  public override int MinimumPlayers=>1;public override int MaximumPlayers=>8;public override bool UsesAction=>false;
  public override int WeatherCount=>1;public override string OptionLabel=>new[]{"SHARED ARENA","SPLIT CHASE","SPLIT DRIVER CAB"}[option];
  public override bool MultiViewHUD=>true;public override bool ReplayControls=>replay!=null&&replay.Playing;
  public override string Help=>"PARADE PANDEMONIUM\nDrive into the marked cartoon targets. Bright costumes and blue costume noses: 10 points. Black masked runners with gold stars: 100 points. Red STOP uniforms and blonde ponytails: −100 points. Point values follow costume roles, independently of the original character identity.\n\nOne score per pedestrian, credited to the first meaningful physical truck impact. Fallen bodies never score again. Scores can be negative. Three minutes, or clear every positive target. Surviving red targets do not prevent completion. Ties share placing.\n\nChoose shared arena, split chase or driver cab. Up to four humans and eight total competitors. Movement alone plays the sport. Reverse brakes before reversing. Cab: whole pad right stick; primary keyboard hold right mouse to look, middle click recenters. Shared sticks retain independent driving.\n\nEsc / Start pauses. Reconnect or explicitly replace a missing device, release inputs, then freshly resume. Rematch retains your names, colors and sources. Results offer optional multi-angle slow/fast impact highlights.";
  public override string Header=>Title;public override string Status=>announcement;public override string Standings=>Scoreboard(false);public override string Results=>Scoreboard(true);
  public override string ViewText(int index)=>"";
  PlowPedestrian[] crowd=Array.Empty<PlowPedestrian>();PlowTargetKind[] roles;System.Random random;float liveStart,uiNext,announceUntil,lastImpactTime=-100;
  Vector3 lastImpact;string announcement="";readonly List<Pending> pending=new List<Pending>(128);
  readonly int[] multiHits=new int[8];readonly float[] multiUntil=new float[8];readonly int[] target=new int[8];readonly float[] rethink=new float[8],stuck=new float[8],reverseUntil=new float[8],avoidUntil=new float[8],avoidSide=new float[8];readonly Vector3[] prior=new Vector3[8];readonly NavMeshPath[] paths=new NavMeshPath[8];readonly int[] corner=new int[8];
  readonly PlowTruckContact[] contacts=new PlowTruckContact[8];readonly Vector3[] corners=new Vector3[128];
  Renderer[] replayLive=Array.Empty<Renderer>();bool[] replayEnabled=Array.Empty<bool>();
  SportsMotionReplay replay;float recordStart,recordUntil,bestImpact;Transform replayHip;GameObject ghostTruck,ghostPed;
  AudioSource[] voice;int voiceIndex;float nextVoice;
  struct Pending {public PlowPedestrian ped;public int slot;public Vector3 point,relative;public float normalSpeed,truckSpeed,time;}
  protected override void Awake(){base.Awake();routeScratch=new NavMeshPath();for(int i=0;i<8;i++){contacts[i]=trucks[i].GetComponent<PlowTruckContact>();paths[i]=new NavMeshPath();}voice=new AudioSource[3];for(int i=0;i<3;i++){voice[i]=gameObject.AddComponent<AudioSource>();voice[i].playOnAwake=false;}}
  public override bool ValidateSources(out string reason){if(!base.ValidateSources(out reason))return false;if(crowdSettings==null||crowdSettings.characters==null||crowdSettings.characters.Length==0){reason="Crowd Settings needs at least one valid weighted pedestrian prefab.";return false;}float available=0;foreach(var c in crowdSettings.characters)if(c!=null&&c.prefab!=null&&PlowRound.Finite(c.weight))available+=Mathf.Max(0,c.weight);if(available<=0){reason="Enable a pedestrian prefab with a positive spawn weight in Crowd Settings.";return false;}int humans=0;for(int i=0;i<setup.count;i++)if(setup.players[i].source!=BowlingSource.AI)humans++;if(cabExtras!=null&&!cabExtras.ValidBindings(out reason))return false;if(humans>4){reason="At most four local humans; use AI for the other slots.";return false;}return true;}
  bool ProbeActive {get{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
   return SportsProbeInputIsolation.Active;
#else
   return false;
#endif
  }}
  protected override void BeginGame(){
   cabExtras?.ResetNeeds();impactPresentation?.ResetEffects();
   EndReplay();ReleaseReplay();random=new System.Random(crowdSettings.randomSeed+(crowdSettings.varySeedEachMatch?Environment.TickCount:0));
   int count=Mathf.Clamp(crowdSettings.crowdSize,16,384);EnsureCrowd(count);roles=new PlowTargetKind[count];int bonuses=Mathf.RoundToInt(count*crowdSettings.bonusFraction),penalties=Mathf.RoundToInt(count*crowdSettings.penaltyFraction);
   for(int i=0;i<count;i++)roles[i]=i<bonuses?PlowTargetKind.BonusRunner:i<bonuses+penalties?PlowTargetKind.PenaltyMarshal:PlowTargetKind.Standard;
   for(int i=count-1;i>0;i--){int j=random.Next(i+1);var k=roles[i];roles[i]=roles[j];roles[j]=k;}
   Round=new PlowRound(roles,setup.count,crowdSettings.roundSeconds);pending.Clear();PhysicalHits=0;bestImpact=0;Intro=3.5f;liveStart=Time.fixedTime+Intro;
   for(int i=0;i<trucks.Length;i++){if(i<setup.count){trucks[i].Prepare(truckStarts[i].position,truckStarts[i].rotation);prior[i]=trucks[i].body.position;}else trucks[i].gameObject.SetActive(false);multiHits[i]=0;multiUntil[i]=0;target[i]=-1;rethink[i]=stuck[i]=reverseUntil[i]=avoidUntil[i]=avoidSide[i]=0;}
   for(int i=0;i<crowd.Length;i++){if(i>=count){crowd[i].ragdoll.PrepareForPool();continue;}var character=crowd[i].characterName;crowd[i].ResetTarget(this,i,roles[i],Spawn(i),Quaternion.Euler(0,random.Next(360),0),character,crowd[i].panicLine);}
   foreach(var p in props)p.ResetProp();views.Layout();hud.ResetHUD();announcement="";announceUntil=0;
  }
  void EnsureCrowd(int count){if(crowd.Length>=count)return;var list=new List<PlowPedestrian>(crowd);for(int i=list.Count;i<count;i++)list.Add(PlowCrowdFactory.Create(PlowCrowdFactory.Choose(crowdSettings.characters,random),crowdRoot,crowdSettings));crowd=list.ToArray();}
  Vector3 Spawn(int id){for(int attempt=0;attempt<100;attempt++){float total=0;foreach(var z in spawnZones)if(z.enabledForCrowd)total+=Mathf.Max(0,z.weight);float pick=(float)random.NextDouble()*total;PlowSpawnZone zone=null;foreach(var z in spawnZones)if(z.enabledForCrowd&&(pick-=Mathf.Max(0,z.weight))<=0){zone=z;break;}if(zone==null)throw new InvalidOperationException("Enable a positive-weight crowd spawn zone.");var p=zone.transform.TransformPoint(new Vector3(((float)random.NextDouble()-.5f)*zone.size.x,0,((float)random.NextDouble()-.5f)*zone.size.y));if(!NavMesh.SamplePosition(p,out var hit,2,NavMesh.AllAreas))continue;bool clear=true;for(int j=0;j<setup.count;j++)if((truckStarts[j].position-hit.position).sqrMagnitude<30)clear=false;for(int j=0;j<id&&clear;j++)if((crowd[j].transform.position-hit.position).sqrMagnitude<1.2f)clear=false;if(clear&&TryRoute(truckStarts[0].position,hit.position,out var reachable))return reachable;}throw new InvalidOperationException("No reachable crowd spawn after 100 attempts; check zone size/navigation/density.");}
  public bool TryRoute(Vector3 from,Vector3 desired,out Vector3 destination){destination=from;if(!NavMesh.SamplePosition(from,out var a,8,NavMesh.AllAreas)||!NavMesh.SamplePosition(desired,out var b,5,NavMesh.AllAreas))return false;var path=routeScratch;bool ok=NavMesh.CalculatePath(a.position,b.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;if(ok)destination=b.position;return ok;}
  NavMeshPath routeScratch;
  public Vector3 RandomDestination(Vector3 from,float distance){return from+new Vector3(((float)random.NextDouble()-.5f)*distance,0,((float)random.NextDouble()-.5f)*distance);}
  public bool NearImpact(Vector3 p)=>Time.time-lastImpactTime<1&&(p-lastImpact).sqrMagnitude<100;
  public bool NearView(Vector3 p,float distance){foreach(var c in cameras)if(c!=null&&c.enabled){var v=c.WorldToViewportPoint(p);if(v.z>0&&v.x>-.1f&&v.x<1.1f&&v.y>-.1f&&v.y<1.1f&&(c.orthographic||(c.transform.position-p).sqrMagnitude<distance*distance))return true;}return false;}
  public bool SafeToRetire(Vector3 p){foreach(var c in cameras)if(c!=null&&c.enabled){var v=c.WorldToViewportPoint(p);if(v.z>0&&v.x>-.1f&&v.x<1.1f&&v.y>-.1f&&v.y<1.1f)return false;}return true;}
  public void Reaction(PlowPedestrian ped){if(Time.time<nextVoice)return;nextVoice=Time.time+2.5f;var line=ped.reactions.Length>0?ped.reactions[random.Next(ped.reactions.Length)]:null;string caption=line!=null?line.Subtitle:ped.panicLine;if(Time.time-lastImpactTime>1.2f){announcement=ped.characterName+": “"+caption+"”";announceUntil=Time.time+2;}if(line!=null){ped.ReactionCooldown(line.cooldown);if(line.generatedAudio!=null){var s=voice[voiceIndex++%voice.Length];s.volume=settings.sfx*.2f;s.clip=line.generatedAudio;s.Play();}}}
  public void Contact(PlowPedestrian ped,Collision c){if(!Live||Intro>0||Round==null||Round.Sealed||ped.Claimed||c.collider.isTrigger)return;var truck=c.rigidbody!=null?c.rigidbody.GetComponent<SportsEventTruck>():null;if(truck==null||truck.session!=this||truck.slot>=setup.count)return;var snapshot=contacts[truck.slot];for(int k=0;k<c.contactCount;k++){var cp=c.GetContact(k);Vector3 incoming=snapshot.velocity+Vector3.Cross(snapshot.angular,cp.point-snapshot.position)-ped.Velocity;float closing=Mathf.Max(0,Vector3.Dot(incoming,cp.normal));if(closing<2)continue;var shape=truck.shape;Vector3 extent=shape.size*.5f;float support=Mathf.Abs(Vector3.Dot(truck.transform.right*extent.x,cp.normal))+Mathf.Abs(Vector3.Dot(truck.transform.up*extent.y,cp.normal))+Mathf.Abs(Vector3.Dot(truck.transform.forward*extent.z,cp.normal));float gap=Mathf.Max(0,Vector3.Dot(cp.point-snapshot.position,cp.normal)-support);float fraction=Mathf.Clamp(gap/closing,0,Time.fixedDeltaTime);float when=Mathf.Max(0,Time.fixedTime-Time.fixedDeltaTime-liveStart+fraction);pending.Add(new Pending{ped=ped,slot=truck.slot,point=cp.point,relative=incoming,normalSpeed=closing,truckSpeed=snapshot.velocity.magnitude,time=when});break;}}
  void ResolveContacts(){pending.Sort((a,b)=>a.time!=b.time?a.time.CompareTo(b.time):a.slot.CompareTo(b.slot));foreach(var hit in pending){if(!Round.TryClaim(hit.ped.id,hit.slot,hit.normalSpeed,hit.truckSpeed,hit.time))continue;PhysicalHits++;multiHits[hit.slot]=Time.time<multiUntil[hit.slot]?multiHits[hit.slot]+1:1;multiUntil[hit.slot]=Time.time+1.25f;hit.ped.Strike(hit.relative,hit.point);impactPresentation?.Impact(hit.point);lastImpact=hit.point;lastImpactTime=Time.time;int points=PlowRound.Points(hit.ped.role);announcement=setup.players[hit.slot].name+"  "+(points>0?"+":"")+points+" · "+(hit.ped.role==PlowTargetKind.PenaltyMarshal?"PENALTY":hit.ped.role==PlowTargetKind.BonusRunner?"GOLD STAR":"HIT");if(multiHits[hit.slot]>1)announcement+=" · "+multiHits[hit.slot]+"-HIT PILEUP!";announceUntil=Time.time+2.5f;hud.Popup(hit.point,points,setup.players[hit.slot].color,hit.slot);if(hitDust!=null&&hitDust.Length>0){var dust=hitDust[nextDust++%hitDust.Length];dust.transform.position=hit.point;dust.Play();}feedback.Play(points<0?2:points==100?4:1,hit.normalSpeed,true);Pulse(hit.slot,.25f,.12f);TryRecord(hit);}pending.Clear();}
  protected override void Simulate(float dt){
   if(Intro>0){Intro=Mathf.Max(0,Intro-dt);liveStart=Time.fixedTime;for(int i=0;i<setup.count;i++)trucks[i].TickDrive(Vector2.zero);return;}
   Round.Advance(dt);ResolveContacts();if(Round.ShouldEnd){Round.Finish();EndMatch();return;}
   for(int i=0;i<setup.count;i++){var drive=ProbeHandsOff&&ProbeActive?Vector2.zero:setup.players[i].source==BowlingSource.AI?Bot(i):commands[i];trucks[i].TickDrive(drive);Recover(i,dt);}
   int activeRagdolls=0;foreach(var p in crowd)if(p.gameObject.activeSelf&&p.Claimed&&!p.Resting)activeRagdolls++;
   foreach(var p in crowd){if(!p.gameObject.activeSelf)continue;p.Tick(dt);if(p.Claimed&&!p.Resting&&activeRagdolls>crowdSettings.maximumActiveRagdolls&&Time.time-p.ClaimedAt>8&&p.Settled){if(crowdSettings.keepRagdolls){if(p.Rest())activeRagdolls--;}else if(SafeToRetire(p.DisplayPosition)){p.ragdoll.PrepareForPool();activeRagdolls--;}else if(p.Collect())activeRagdolls--;}}
   if(replay!=null&&!replay.Playing&&Time.time<=recordUntil)replay.Record(Time.time-recordStart,3);
  }
  Vector2 Bot(int slot){var t=trucks[slot];int difficulty=(int)setup.players[slot].difficulty;
   if(Time.time>=rethink[slot]||target[slot]<0||crowd[target[slot]].Claimed){rethink[slot]=Time.time+(difficulty==0?.9f:difficulty==1?.5f:.25f);float best=float.PositiveInfinity;int chosen=-1;
    for(int i=0;i<Round.TargetCount;i++){var p=crowd[i];if(!p.gameObject.activeInHierarchy||p.Claimed||p.role==PlowTargetKind.PenaltyMarshal)continue;float cost=(p.transform.position-t.body.position).sqrMagnitude/(p.role==PlowTargetKind.BonusRunner?1.5f:1);if(cost<best){best=cost;chosen=i;}}target[slot]=chosen;
    paths[slot].ClearCorners();corner[slot]=1;
    if(chosen>=0&&NavMesh.SamplePosition(t.body.position,out var a,8,-1)&&NavMesh.CalculatePath(a.position,crowd[chosen].transform.position,-1,paths[slot])&&paths[slot].status!=NavMeshPathStatus.PathComplete)target[slot]=-1;
   }
   if(Time.time<reverseUntil[slot])return new Vector2(-avoidSide[slot]*.85f,-.85f);
   if(target[slot]<0)return Vector2.zero;var targetPed=crowd[target[slot]];Vector3 aim=targetPed.transform.position+targetPed.Velocity*(difficulty==0?.08f:difficulty==1?.3f:.45f);
   int n=paths[slot].GetCornersNonAlloc(corners);if(n>1&&corner[slot]<n-1){if((corners[corner[slot]]-t.body.position).sqrMagnitude<14)corner[slot]++;aim=corners[Mathf.Min(corner[slot],n-1)];}
   Vector3 delta=aim-t.body.position;delta.y=0;
   if(difficulty>0)foreach(var p in crowd)if(!p.Claimed&&p.role==PlowTargetKind.PenaltyMarshal){var near=p.transform.position-t.body.position;near.y=0;if(near.sqrMagnitude<36&&Vector3.Dot(near,t.transform.forward)>0)delta+=Vector3.Cross(Vector3.up,near.normalized)*((slot%2==0?1:-1)*Mathf.Max(0,6-near.magnitude)*difficulty);}
   float angle=Vector3.SignedAngle(t.transform.forward,delta,Vector3.up);float steer=Mathf.Clamp(angle/30,-1,1);float throttle=Mathf.Abs(angle)>85?.3f:Mathf.Abs(angle)>40?.6f:1;
   if(Physics.SphereCast(t.body.position+Vector3.up*.5f,.65f,t.transform.forward,out var obstacle,4,1,QueryTriggerInteraction.Ignore)){
    if(Time.time>=avoidUntil[slot]){float side=Vector3.Dot(obstacle.normal,t.transform.right);avoidSide[slot]=Mathf.Abs(side)>.15f?Mathf.Sign(side):(slot%2==0?1:-1);avoidUntil[slot]=Time.time+.8f;}
    steer=avoidSide[slot]*.95f;throttle=.3f;
   }
   return new Vector2(steer,throttle);
  }
  void Recover(int slot,float dt){var t=trucks[slot];Vector3 displacement=t.body.position-prior[slot];displacement.y=0;prior[slot]=t.body.position;
   // Obstacle throttle is .3: it must qualify too. Recovery remains ordinary NWH braking/reverse.
   if(setup.players[slot].source==BowlingSource.AI){
    bool reversing=Time.time<reverseUntil[slot];stuck[slot]=!reversing&&displacement.sqrMagnitude<dt*dt*.64f&&Mathf.Abs(t.command.y)>.15f?stuck[slot]+dt:0;
    if(stuck[slot]>.7f){if(Mathf.Abs(avoidSide[slot])<.1f)avoidSide[slot]=slot%2==0?1:-1;reverseUntil[slot]=Time.time+1.1f;avoidUntil[slot]=reverseUntil[slot]+.5f;rethink[slot]=reverseUntil[slot];stuck[slot]=0;}
   }
   if(t.body.position.y< -8||Vector3.Dot(t.transform.up,Vector3.up)<.15f){if((Time.time%4)<dt){var start=truckStarts[slot];t.Prepare(start.position,start.rotation);}}
  }
  protected override void Present(float dt){cabExtras?.Tick(dt);if(replay!=null&&replay.Playing){var focus=replayHip!=null?replayHip.position:replay.Position(1);views.ReplayView(focus,replay.Angle);if(replay.Step(dt))EndReplay();}if(Time.unscaledTime>=uiNext){uiNext=Time.unscaledTime+.1f;hud?.Refresh();}if(Time.time>announceUntil)announcement="";}
  public string Countdown=>Intro>1?Mathf.CeilToInt(Intro-.5f).ToString():"GO!";
  public string LastTargets(int slot){if(Round==null||Round.PositiveRemaining>5||Round.PositiveRemaining==0)return "";float best=float.PositiveInfinity;Vector3 delta=Vector3.zero;foreach(var p in crowd)if(!p.Claimed&&p.role!=PlowTargetKind.PenaltyMarshal){var d=p.transform.position-trucks[slot].body.position;if(d.sqrMagnitude<best){best=d.sqrMagnitude;delta=d;}}float angle=Vector3.SignedAngle(trucks[slot].transform.forward,delta,Vector3.up);return "LAST TARGETS: "+Mathf.RoundToInt(Mathf.Sqrt(best))+"m "+(Mathf.Abs(angle)<30?"AHEAD":Mathf.Abs(angle)>140?"BEHIND":angle<0?"LEFT":"RIGHT");}
  string Scoreboard(bool detailed){if(Round==null)return "";var order=new int[setup.count];for(int i=0;i<order.Length;i++)order[i]=i;Array.Sort(order,(a,b)=>Round.Score(b).CompareTo(Round.Score(a)));var s=new StringBuilder();if(detailed)s.AppendLine("<b>"+(setup.count==1?"SCORE ATTACK COMPLETE":Round.Score(order[0])==Round.Score(order[1])?"SHARED FIRST PLACE":"WINNER: "+setup.players[order[0]].name)+"</b>");foreach(int i in order){s.Append(Round.Place(i)).Append(". ").Append(setup.players[i].name).Append("   ").Append(Round.Score(i)).Append(" pts");if(detailed)s.Append("    ").Append(Round.StandardHits(i)).Append(" standard · ").Append(Round.BonusHits(i)).Append(" bonus · ").Append(Round.Penalties(i)).Append(" penalties");s.AppendLine();}return s.ToString();}
  protected override void StopGame(){foreach(var t in trucks)if(t!=null&&t.gameObject.activeSelf)t.TickDrive(Vector2.zero);foreach(var p in crowd)if(p!=null&&p.agent.enabled&&p.agent.isOnNavMesh)p.agent.isStopped=true;}
  protected override void StopSounds(){cabExtras?.StopAudio();if(voice!=null)foreach(var s in voice)if(s!=null)s.Stop();if(trucks!=null)foreach(var t in trucks)if(t!=null)t.StopSound();}
  void TryRecord(Pending hit){if(hit.normalSpeed<=bestImpact||replay!=null&&Time.time<recordUntil)return;ReleaseReplay();bestImpact=hit.normalSpeed;ghostTruck=Ghost(trucks[hit.slot].transform);ghostPed=Ghost(hit.ped.transform);replay=gameObject.AddComponent<SportsMotionReplay>();replay.capacity=80;replay.interval=.04f;replay.sources=new[]{trucks[hit.slot].transform,hit.ped.transform};replay.ghosts=new[]{ghostTruck.transform,ghostPed.transform};replay.ResetRecord();recordStart=Time.time;recordUntil=Time.time+2.7f;replayHip=ghostPed.transform.Find("Bip01/Bip01 Pelvis");replay.Record(0,3);}
  GameObject Ghost(Transform source){var copy=SportsReplayVisuals.Copy(source);copy.name="Plowing render-only highlight";copy.SetActive(false);return copy;}

  void ShowLiveReplay(bool visible){if(visible){for(int i=0;i<replayLive.Length;i++)if(replayLive[i]!=null)replayLive[i].enabled=replayEnabled[i];replayLive=Array.Empty<Renderer>();replayEnabled=Array.Empty<bool>();return;}var list=new List<Renderer>();foreach(var t in trucks)list.AddRange(t.GetComponentsInChildren<Renderer>(true));foreach(var p in crowd)list.AddRange(p.GetComponentsInChildren<Renderer>(true));replayLive=list.ToArray();replayEnabled=new bool[replayLive.Length];for(int i=0;i<replayLive.Length;i++){replayEnabled[i]=replayLive[i].enabled;replayLive[i].enabled=false;}}
  void ReleaseReplay(){if(replay!=null)Destroy(replay);if(ghostTruck!=null)Destroy(ghostTruck);if(ghostPed!=null)Destroy(ghostPed);replay=null;ghostTruck=ghostPed=null;}
  void EndReplay(){ShowLiveReplay(true);if(replay!=null)replay.End();if(views!=null){views.rally.Replay=false;if(Finished)menus.Show(SportsEventScreen.Result);views.Layout();}}
  public override void ExtraAction(string key){if(key=="highlights"&&Finished&&replay!=null&&replay.Duration>.1f){replay.Begin();if(replay.Playing)ShowLiveReplay(false);menus.Show(SportsEventScreen.Play);}else if(key=="replayskip")EndReplay();else if(replay!=null&&replay.Playing){if(key=="replayangle")replay.Angle++;if(key=="replayspeed")replay.Speed=replay.Speed<.5f?.5f:replay.Speed<1?1:replay.Speed<2?2:.25f;if(key=="replayrewind")replay.Seek(0);}}
  protected override void OnDestroy(){ReleaseReplay();base.OnDestroy();}
 }
}
