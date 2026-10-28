using System;using System.Text;using LWS.TruckSports;using LWS.TruckTeaParty;using LWS.TruckFareFight;using LWS.TruckBowling;using LWS.TruckFieldGoal;using UnityEngine;using UnityEngine.InputSystem;
namespace LWS.TruckBakeOff {
 public sealed class BakeMatch:SportsEventSession,IServingEvents {
  public ServingArm[] arms;public ServingVessel[] ingredients,nozzles;public BakeBowl[] bowls;public BakePan[] pans;public BakeOven[] ovens;public BakeJudge[] judges;public BakeWhisk[] whisks;public SportsDrivingViews drivingViews;public SportsEventCamera broadcast;public FieldGoalCrowd crowd;public BakeRules rules=new BakeRules();public Key[] toolKeys={Key.Q,Key.PageUp,Key.Y,Key.O},doorKeys={Key.E,Key.PageDown,Key.B,Key.P};public int NativeMixSteps,NativeHeatSteps,NativeDecorationHits,NativeEggCracks;public int JudgedCakes{get;private set;}public int BrokenIngredients{get;private set;}
  readonly FareFightRoute[] routes=new FareFightRoute[4];readonly int[] task=new int[4],ingredientType=new int[4],stockId=new int[4],quadrant=new int[4];readonly bool[] extraReleased=new bool[4],doorHeld=new bool[4],returnAligned=new bool[4],pourAligned=new bool[4],sourceRaised=new bool[4];readonly Vector3[] supplyLift=new Vector3[4],supplyWithdraw=new Vector3[4];readonly float[] panSupportTop=new float[4];readonly Collider[] decorationSupports=new Collider[4];readonly int[] worktopParking=new int[4];readonly Vector3[] worktopGoals=new Vector3[4];readonly bool[] worktopGoalSet=new bool[4];readonly Vector3[] ovenRetreatGoals=new Vector3[4];ServingVessel[] vessels;readonly StringBuilder standings=new StringBuilder(300);
  public override string Title=>"TRUCK BAKE-OFF";public override int MinimumPlayers=>1;public override int MaximumPlayers=>4;public override int WeatherCount=>1;public override bool SupportsSplitAIViews=>true;public override bool CompactBroadcastStatus=>true;public override bool CompactViewportHud=>true;public override int CompactScoreColumns=>4;public override bool MultiViewHUD=>drivingViews!=null&&!drivingViews.NeedsArena;public override string OptionLabel=>option==0?"QUICK SHOW · 5 MIN":option==1?"STUDIO CHALLENGE · 7 MIN":"SHOWSTOPPER · 9 MIN";
  public override string Header=>Title+" · "+(rules.Clock<3?"READY "+Mathf.CeilToInt(3-rules.Clock):Mathf.CeilToInt(rules.Remaining)+"s");public override string Status=>rules.Clock<3?"KITCHEN CHECK":rules.Complete?"":JudgedCakes+" / "+setup.count+" CAKES PRESENTED";
  public override string Help=>"Bake a real cake in your own NWH truck using a physical articulated ingredient scoop/cradle, whisk, tray and decorating nozzle. Original drive/steer, profiles, shared/chase/cab, pause, input recovery and rematch remain. ACTION grabs a vessel actually touching the wrist sensor; a fresh press drops it. Hold ACTION with drive/steer to adjust arm height/yaw while NWH handbrake holds. TOOL: keyboard Q/PageUp/Y/O, whole RB, own half shoulder. It tilts a held ingredient/batter container; with an empty hand at the mixing bowl it spins the actual whisk; after cooking it opens the gimballed cream nozzle. OVEN DOOR: keyboard E/PageDown/B/P, whole West, own half stick-click; actual presence at your oven controls is required. Recipe: flour0.8, milk0.5, sugar0.3, one egg. Pour measured contents into the actual bowl opening. Drop the physical egg into the bowl so native contact cracks it; a hard miss can break it on the floor. Fragile supplies are finite with one spare egg. Mix using the rotating whisk physically inside the bowl for8seconds. The stand then releases the real bowl. Pour at least2.1 batter into your physical cake pan. Carry/release the pan onto the actual oven shelf, close the moving door and heat around200degrees until cooked. Open/remove it; excessive heating reduces quality. Set it on the decorating counter and direct the real cream stream across all four cake quadrants. At least0.04 cream per quadrant forms a rosette. Finally place an intact decorated cake on your own judging table, upright and still for0.7seconds. Only this actual presentation scores recipe precision, cooking timing, retained cake and decoration once. No pickup/mixing/heat timer grants fake points. AI physically performs the same tasks and contact gates. A broken mixing bowl or cake pan fails that batch; setup/rematch resets the full recipe. General instructions remain here, not across the play view.";
  public override bool TryReadScoreCard(int slot,out string score,out string detail){bool valid=slot>=0&&slot<setup.count;score=valid?rules.points[slot]+" PTS":"";detail=valid?rules.stages[slot].ToString().ToUpperInvariant():"";return valid;}
  public override bool TryReadViewportCard(int view,out int participant,out string score,out string detail)=>TryReadDrivingViewportCard(drivingViews,view,out participant,out score,out detail);public override string ViewportTelemetry(int view)=>Readout(drivingViews.rally.owners[view]);public override string ViewText(int view){int s=drivingViews.rally.owners[view];return setup.players[s].name+" · "+rules.points[s]+" PTS\n"+Readout(s);}public string Readout(int s){var stage=rules.stages[s];if(stage==BakeStage.Ingredients||stage==BakeStage.Mixing)return "F "+rules.measured[s,0].ToString("F2")+" M "+rules.measured[s,1].ToString("F2")+" S "+rules.measured[s,2].ToString("F2")+" E "+rules.measured[s,3].ToString("F0")+" · MIX "+rules.mixSeconds[s].ToString("F1")+" / 8";if(stage==BakeStage.Loaded||stage==BakeStage.Baking)return Mathf.RoundToInt(ovens[s].Temperature)+"° · HEAT "+rules.heatSeconds[s].ToString("F1")+" / 10";if(stage==BakeStage.Cooked||stage==BakeStage.Decorated){int bits=rules.decorated[s],n=0;for(int i=0;i<4;i++)if((bits&(1<<i))!=0)n++;return "ICING "+n+" / 4 · "+pans[s].vessel.Volume.ToString("F2")+" CAKE";}return stage.ToString().ToUpperInvariant();}
  public override string Standings{get{standings.Clear();for(int p=1;p<=setup.count;p++)for(int s=0;s<setup.count;s++)if(rules.Place(s)==p)standings.Append('#').Append(p).Append(' ').Append(setup.players[s].name).Append(" · ").Append(rules.points[s]).Append(" PTS · ").Append(rules.stages[s]).Append('\n');return standings.ToString();}}public override string Results=>"BAKE-OFF JUDGING COMPLETE\n\n"+Standings;
  public override bool ValidateSources(out string reason){if(!base.ValidateSources(out reason))return false;for(int i=0;i<setup.count;i++){if(setup.players[i].source!=BowlingSource.Keyboard)continue;foreach(var k in new[]{toolKeys[i],doorKeys[i]}){if(k==Key.None||k==Key.Escape||toolKeys[i]==doorKeys[i]){reason="Assign distinct baking tool/door keys; Escape remains pause.";return false;}for(int j=0;j<setup.count;j++){var p=setup.players[j];if(p.source!=BowlingSource.Keyboard)continue;if(k==p.forward||k==p.back||k==p.left||k==p.right||k==actionKeys[j]||j!=i&&(k==toolKeys[j]||k==doorKeys[j])){reason="Baking key conflicts with driver "+(j+1);return false;}}}}return true;}
  protected override void BeginGame(){if(arms.Length!=4||ingredients.Length!=20||bowls.Length!=4||pans.Length!=4||ovens.Length!=4||judges.Length!=4||whisks.Length!=4||nozzles.Length!=4)throw new InvalidOperationException("Author all four complete television baking workstations.");rules.Reset(setup.count,option);NativeMixSteps=NativeHeatSteps=NativeDecorationHits=NativeEggCracks=JudgedCakes=BrokenIngredients=0;vessels=new ServingVessel[32];Array.Copy(ingredients,vessels,20);for(int i=0;i<4;i++){vessels[20+i]=bowls[i].vessel;vessels[24+i]=pans[i].vessel;vessels[28+i]=nozzles[i];task[i]=ingredientType[i]=quadrant[i]=0;stockId[i]=i*5;panSupportTop[i]=0;worktopParking[i]=0;worktopGoalSet[i]=false;ovenRetreatGoals[i]=Vector3.zero;sourceRaised[i]=pourAligned[i]=returnAligned[i]=extraReleased[i]=doorHeld[i]=false;routes[i]??=new FareFightRoute();routes[i].Reset();routes[i].useGoalProgressRecovery=true;routes[i].stallSeconds=2.5f;routes[i].minimumReverseDistance=6;arms[i].gameObject.SetActive(i<setup.count);if(i<setup.count)arms[i].Prepare(new Vector3(X(i),.65f,-58),Quaternion.identity);}foreach(var v in vessels)v.Prepare();foreach(var ingredient in ingredients){var egg=ingredient.GetComponent<BakeEgg>();if(egg!=null)egg.ResetEgg();}for(int i=0;i<4;i++){bowls[i].Prepare();pans[i].vessel.contents=ServingContents.Batter;pans[i].Tick();ovens[i].Prepare();judges[i].Prepare();nozzles[i].body.isKinematic=true;nozzles[i].PickupAllowed=false;nozzles[i].pourable=false;whisks[i].Running=false;}Physics.SyncTransforms();BindDecorationSupports();drivingViews.Layout();crowd?.ResetAttempt();}
  protected override void Simulate(float dt){rules.Tick(dt);if(rules.Complete){for(int s=0;s<setup.count;s++)if(rules.Place(s)==1)ConfirmOutcome("bake-champion:"+s,s,rules.points[s],"STAR BAKER","PTS",SportsScoreTier.MatchChampion,matchEnded:true);EndMatch();return;}foreach(var v in vessels){v.InteractionEnabled=rules.Active;v.TickLiquid(dt);}if(!rules.Active){for(int s=0;s<setup.count;s++)Hold(arms[s].truck);return;}
   for(int s=0;s<setup.count;s++){var arm=arms[s];arm.clock=rules.Clock;nozzles[s].pourable=false;whisks[s].Running=false;if(rules.stages[s]==BakeStage.Failed||rules.stages[s]==BakeStage.Presented){Hold(arm.truck);continue;}if(setup.players[s].source==BowlingSource.AI)Bot(s,dt);else{bool tool=Extra(s,true),door=Extra(s,false);if(door&&!doorHeld[s])ovens[s].Toggle();doorHeld[s]=door;bool adjust=ActionHeld(s);if(actions[s]){if(arm.Held!=null)arm.Drop();else arm.Grab();}if(adjust)Hold(arm.truck);else arm.truck.TickDrive(commands[s]);arm.Manual(commands[s],adjust,tool&&arm.Held!=null,dt);if(tool&&arm.Held==null){if(rules.stages[s]==BakeStage.Cooked||rules.stages[s]==BakeStage.Decorated)nozzles[s].pourable=true;else whisks[s].Running=true;}}
    nozzles[s].body.MovePosition(arm.grip.position+arm.grip.rotation*new Vector3(1.7f,-.8f,0));nozzles[s].body.MoveRotation(Quaternion.identity);whisks[s].Tick(dt);bowls[s].Tick(dt);ovens[s].Tick(dt);pans[s].Tick();
   }
  }
  bool Extra(int slot,bool tool){var p=setup.players[slot];if(!Ready(slot)||!p.released){extraReleased[slot]=false;return false;}bool raw(bool t){if(p.source==BowlingSource.Keyboard)return SportsDeviceInput.KeyHeld(t?toolKeys[slot]:doorKeys[slot]);if(!p.Controller)return false;if(p.source==BowlingSource.WholeController)return t?p.pad.rightShoulder.isPressed:p.pad.buttonWest.isPressed;bool right=p.source==BowlingSource.RightStick;return t?(right?p.pad.rightShoulder:p.pad.leftShoulder).isPressed:(right?p.pad.rightStickButton:p.pad.leftStickButton).isPressed;}if(!extraReleased[slot]){if(raw(true)||raw(false))return false;extraReleased[slot]=true;}return raw(tool);}
  public static float X(int slot)=>(slot-1.5f)*34;Vector3 Source(int slot)=>new Vector3(X(slot)-2.2f,0,ingredients[stockId[slot]].spawn.z-2.5f);static Vector3 Mixer(int slot)=>new Vector3(X(slot)+2.2f,0,-27.5f);static void Hold(SportsEventTruck t){t.TickDrive(Vector2.zero);var input=t.tractorMotor.vehicle.input;input.states.handbrake=input.states.handbrakeRaw=1;}
  bool Approach(int slot,Vector3 goal,float dt,float stopRadius=2,float driveScale=1){var truck=arms[slot].truck;if(Vector3.ProjectOnPlane(goal-truck.body.position,Vector3.up).magnitude<stopRadius){Hold(truck);return truck.body.linearVelocity.magnitude<.6f;}// Workstations flank an open aisle. A short return along that same
   // aisle uses the original NWH reverse input, instead of asking a full
   // tractor to circle through a solid counter to face a goal behind it.
   var delta=Vector3.ProjectOnPlane(goal-truck.body.position,Vector3.up);float angle=Vector3.SignedAngle(Vector3.ProjectOnPlane(truck.transform.forward,Vector3.up),delta,Vector3.up);
   if(Mathf.Abs(angle)>110&&Mathf.Abs(Vector3.Dot(delta,truck.transform.right))<10){float reverseAngle=Vector3.SignedAngle(Vector3.ProjectOnPlane(-truck.transform.forward,Vector3.up),delta,Vector3.up);truck.TickDrive(new Vector2(-Mathf.Clamp(reverseAngle/42,-1,1),-.45f*driveScale));return false;}
   routes[slot].DirectGoal(truck.body.position,goal);var drive=routes[slot].Drive(truck.body,truck.transform,(int)setup.players[slot].difficulty,dt,1);drive.y=Mathf.Clamp(drive.y,-.4f,.6f)*driveScale;truck.TickDrive(drive);return false;}
  bool ApproachWorktop(int slot,Vector3 goal,float dt,bool workspaceFits)
  {
   // R49: releasing the handbrake immediately after inertia crosses a tiny
   // arrival radius made the original route circle through workstations.
   // These measured worktop/oven arrivals latch the ORIGINAL NWH brake until the
   // tractor actually stops. The measured cargo/nozzle workspace, not a fake
   // position or forced velocity, then admits physical machinery work.
   var truck=arms[slot].truck;if(!worktopGoalSet[slot]||(worktopGoals[slot]-goal).sqrMagnitude>.000001f){worktopGoals[slot]=goal;worktopGoalSet[slot]=true;worktopParking[slot]=0;}
   if(worktopParking[slot]==1){Hold(truck);if(truck.body.linearVelocity.magnitude>=.6f)return false;if(workspaceFits)return true;worktopParking[slot]=2;}
   float distance=Vector3.ProjectOnPlane(goal-truck.body.position,Vector3.up).magnitude;float radius=worktopParking[slot]==2?(workspaceFits?.35f:.05f):2;
   if(distance<radius){worktopParking[slot]=1;Hold(truck);return truck.body.linearVelocity.magnitude<.6f&&workspaceFits;}
   Approach(slot,goal,dt,.05f,worktopParking[slot]==2?.25f:1);return false;
  }
  void AimHeld(int slot,Vector3 bodyGoal,float tilt,float dt){var arm=arms[slot];if(arm.Held!=null)arm.Move(bodyGoal-(arm.Held.body.position-arm.grip.position),tilt,false,dt);}
  bool AtBody(ServingVessel v,Vector3 goal)=>Vector3.Distance(v.body.position,goal)<.45f&&v.body.linearVelocity.magnitude<.7f;
  static Vector3 SourcePickupPoint(ServingVessel stock)
  {
   // V27 measured the ORIGINAL scoop .7182m BELOW the wrist, lower than
   // its plate. Clear that complete solid tool, not just the plate.
   // V28 reached the1m hover, but its3.0806m body-origin distance
   // exceeded the unchanged native Grab radius3m. The.85m hover leaves
   // .1318m scoop clearance and2.9347m body-origin distance for the actual
   // sacks; rear.7m retains native sensor contact. Raise in the aisle first.
   // Grab still requires the same real trigger candidate/pose/velocity gates.
   float top=stock.body.position.y;foreach(var shape in stock.physical)if(shape!=null&&shape.enabled)top=Mathf.Max(top,shape.bounds.max.y);
   return new Vector3(stock.body.position.x,top+.85f,stock.body.position.z-.7f);
  }
  static Vector3 PourBodyTarget(ServingVessel receiver)
  {
   // Align the ORIGINAL carried body upright above the real receiver rim,
   // before tilting it. The35-degree AI pour keeps sacks/pots clear of that
   // rim and inside the existing8m machinery reach at the aisle-side stand.
   // ServingVessel's unchanged physical ballistic stream alone accepts liquid.
   float rim=receiver.body.position.y;foreach(var shape in receiver.physical)if(shape!=null&&shape.enabled)rim=Mathf.Max(rim,shape.bounds.max.y);
   return new Vector3(receiver.body.position.x,rim+.75f,receiver.body.position.z-2.1f);
  }
  static float LowestPhysicalY(ServingVessel vessel)
  {
   float lowest=float.PositiveInfinity;foreach(var shape in vessel.physical)if(shape!=null&&shape.enabled)lowest=Mathf.Min(lowest,shape.bounds.min.y);return float.IsPositiveInfinity(lowest)?vessel.body.position.y:lowest;
  }
  static Bounds VesselPhysicalBounds(ServingVessel vessel)
  {
   var bounds=new Bounds(vessel.body.position,Vector3.zero);bool first=true;foreach(var shape in vessel.physical)if(shape!=null&&shape.enabled){if(first){bounds=shape.bounds;first=false;}else bounds.Encapsulate(shape.bounds);}return bounds;
  }
  void BindDecorationSupports()
  {
   // The original saved worktop has no separate gameplay adapter. Bind its
   // unique authored name AND original pose/size, never a generic first child.
   for(int s=0;s<4;s++){BoxCollider found=null;var expected=new Vector3(X(s)+8,2.9f,-5);foreach(var c in pans[s].vessel.transform.root.GetComponentsInChildren<BoxCollider>(true))if(c.name=="Studio worktop polished stainless"&&(c.bounds.center-expected).sqrMagnitude<.000001f&&(c.bounds.size-new Vector3(6,.2f,9)).sqrMagnitude<.000001f){if(found!=null)throw new InvalidOperationException("Ambiguous original decoration worktop.");found=c;}decorationSupports[s]=found!=null?found:throw new InvalidOperationException("Exact original decoration worktop binding required.");}
  }
  static bool SideWorktopPoses(Collider surface,ServingVessel pan,Vector3 center,out Vector3 seat,out Vector3 above)
  {
   var top=surface.bounds;var physical=VesselPhysicalBounds(pan);var low=physical.min-pan.body.position;var high=physical.max-pan.body.position;float left=top.min.x-low.x+.2f,right=top.max.x-high.x-.2f,near=top.min.z-low.z+.2f,far=top.max.z-high.z-.2f;seat=center;above=seat;if(left>right||near>far)return false;
   // The aisle is LEFT of both original6m-wide tables. An actual supported
   // left inset preserves the full pan footprint and all nozzle workspace.
   seat.x=left;seat.z=Mathf.Clamp(center.z,near,far);seat.y=top.max.y+.2f-low.y;above=seat;above.y=top.max.y+.35f-low.y;return true;
  }
  static bool PanFootprintOn(Collider surface,ServingVessel pan)
  {
   var top=surface.bounds;var p=VesselPhysicalBounds(pan);return p.min.x>=top.min.x+.05f&&p.max.x<=top.max.x-.05f&&p.min.z>=top.min.z+.05f&&p.max.z<=top.max.z-.05f;
  }
  bool NozzleWorkspaceFits(int slot,Vector3 panGoal)
  {
   var arm=arms[slot];var basePoint=arm.truck.transform.TransformPoint(arm.mount);var offset=nozzles[slot].spout.position-arm.grip.position;for(int q=0;q<4;q++){var target=panGoal+new Vector3(q%2==0?-.7f:.7f,2.2f,q<2?-.7f:.7f);if((target-offset-basePoint).sqrMagnitude>64)return false;}return true;
  }
  static Vector3 LiftAboveWorktop(Collider top,ServingVessel pan)
  {
   var goal=pan.body.position;goal.y+=Mathf.Max(0,top.bounds.max.y+.35f-LowestPhysicalY(pan));return goal;
  }
  static Bounds WristSolidBounds(ServingArm arm)
  {
   var result=new Bounds(arm.grip.position,Vector3.zero);bool first=true;foreach(var shape in arm.grip.GetComponentsInChildren<Collider>(true))if(shape.enabled&&shape.gameObject.activeInHierarchy&&!shape.isTrigger){if(first){result=shape.bounds;first=false;}else result.Encapsulate(shape.bounds);}return result;
  }
  static bool AtWrist(ServingArm arm,Vector3 goal)=>(arm.grip.position-goal).sqrMagnitude<.0225f&&arm.grip.linearVelocity.magnitude<.7f;
  static Vector3 StowedWrist(ServingArm arm)=>arm.truck.transform.TransformPoint(new Vector3(0,4,2));
  static float DoorSweepNear(BakeOven oven)
  {
   float radius=0;var hinge=oven.door.position;foreach(var shape in oven.door.GetComponentsInChildren<Collider>(true))if(shape.enabled&&!shape.isTrigger)
   {
    // Original leaf is a BoxCollider. Its actual transformed corners give
    // a rotation-invariant sweep radius; world AABB corners artificially grow
    // while the same leaf rotates and must not move the retirement target.
    if(shape is BoxCollider box){for(int x=0;x<2;x++)for(int y=0;y<2;y++)for(int z=0;z<2;z++){var local=box.center+Vector3.Scale(box.size*.5f,new Vector3(x==0?-1:1,y==0?-1:1,z==0?-1:1));var point=box.transform.TransformPoint(local);radius=Mathf.Max(radius,Vector2.Distance(new Vector2(hinge.x,hinge.z),new Vector2(point.x,point.z)));}}
    else{var b=shape.bounds;for(int x=0;x<2;x++)for(int z=0;z<2;z++)radius=Mathf.Max(radius,Vector2.Distance(new Vector2(hinge.x,hinge.z),new Vector2(x==0?b.min.x:b.max.x,z==0?b.min.z:b.max.z)));}
   }return hinge.z-radius;
  }
  static float CrewFront(ServingArm arm)
  {
   float front=arm.truck.body.position.z;foreach(var shape in arm.contacts)if(shape!=null&&shape.enabled&&shape.gameObject.activeInHierarchy&&!shape.isTrigger)front=Mathf.Max(front,shape.bounds.max.z);return front;
  }
  void PrepareOvenRetreat(int slot,BakeOven oven)
  {
   var arm=arms[slot];ovenRetreatGoals[slot]=new Vector3(X(slot),0,DoorSweepNear(oven)-(CrewFront(arm)-arm.truck.body.position.z)-.7f);
  }
  bool RetreatForOvenMotion(int slot,BakeOven oven,float dt)
  {
   // R52: re-solving a moving overhang while briefly releasing brakes near
   // a tiny radius made retirement circle and leave the pickup facing away.
   // Capture the same real clear target at the accepted control press, then
   // finish a latched NWH stop; actual ALL-assembly sweep clearance still gates.
   var arm=arms[slot];arm.Move(StowedWrist(arm),0,false,dt);float near=DoorSweepNear(oven);bool clear=CrewFront(arm)<near-.2f;return ApproachWorktop(slot,ovenRetreatGoals[slot],dt,clear)&&clear;
  }
  static bool OvenInsertionPoses(BakeOven oven,ServingVessel pan,out Vector3 seat,out Vector3 above)
  {
   // R38: the FRONT native pickup retains the pan behind its wrist. The
   // original tray CENTER asks for8.71m even at the exact original parking
   // waypoint. The SAME real shelf supports a front-side placement instead;
   // its actual corners and the actual pan solids determine every inset.
   var shelf=oven.shelf.bounds;var physical=VesselPhysicalBounds(pan);var body=pan.body.position;var low=physical.min-body;var high=physical.max-body;
   seat=oven.trayPoint.position+Vector3.up*.2f;above=seat;float left=shelf.min.x-low.x+.2f,right=shelf.max.x-high.x-.2f,front=shelf.min.z-low.z+.2f,back=shelf.max.z-high.z-.2f;
   if(left>right||front>back)return false;seat.x=Mathf.Clamp(seat.x,left,right);seat.z=front;above=seat;above.y=Mathf.Max(seat.y,shelf.max.y+.35f-low.y);return true;
  }
  static bool HeldBodyPoseFits(ServingArm arm,ServingVessel pan,Vector3 bodyGoal)
  {
   var requested=bodyGoal-(pan.body.position-arm.grip.position);return (requested-arm.truck.transform.TransformPoint(arm.mount)).sqrMagnitude<=64;
  }
  static bool PanFootprintOnShelf(BakeOven oven,ServingVessel pan)
  {
   var shelf=oven.shelf.bounds;var physical=VesselPhysicalBounds(pan);return physical.min.x>=shelf.min.x+.05f&&physical.max.x<=shelf.max.x-.05f&&physical.min.z>=shelf.min.z+.05f&&physical.max.z<=shelf.max.z-.05f;
  }
  static Vector3 LoadedPanPickupPoint(ServingVessel pan)
  {
   // R35's diagonal root-target descent struck the returned bowl first,
   // then the pan. Approach ABOVE the pan from its free FRONT side instead.
   return new Vector3(pan.body.position.x,VesselPhysicalBounds(pan).max.y+.85f,pan.body.position.z+1.1f);
  }
  static bool BowlPourWorkspaceFits(ServingArm arm,ServingVessel bowl,ServingVessel pan)
  {
   // Actual retained attachment offset, not body-origin distance alone: the
   // tilted bowl needs a different wrist pose. The original8m cap is unchanged.
   if(arm.Tilt>=1)return false;var basePoint=arm.truck.transform.TransformPoint(arm.mount);var target=PourBodyTarget(pan);var offset=bowl.body.position-arm.grip.position;var forward=Vector3.ProjectOnPlane(bowl.spout.forward,Vector3.up).normalized;var tilted=Quaternion.AngleAxis(35,Vector3.Cross(Vector3.up,forward))*offset;return (target-offset-basePoint).sqrMagnitude<=64&&(target-tilted-basePoint).sqrMagnitude<=64;
  }
  static bool BowlBehindPan(ServingVessel bowl,ServingVessel pan)
  {
   float front=float.NegativeInfinity,back=float.PositiveInfinity;foreach(var shape in bowl.physical)if(shape!=null&&shape.enabled)front=Mathf.Max(front,shape.bounds.max.z);foreach(var shape in pan.physical)if(shape!=null&&shape.enabled)back=Mathf.Min(back,shape.bounds.min.z);return float.IsFinite(front)&&float.IsFinite(back)&&front+.1f<back;
  }
  void Bot(int s,float dt){var arm=arms[s];var bowl=bowls[s];var pan=pans[s];var oven=ovens[s];var stock=ingredients[stockId[s]];bool ready;switch(task[s]){
   case 0:if(rules.stages[s]>=BakeStage.Mixing){task[s]=5;break;}int need=-1;for(int k=0;k<4;k++)if(rules.measured[s,k]<BakeRules.Target((BakeIngredient)k)){need=k;break;}if(need<0){task[s]=5;break;}sourceRaised[s]=pourAligned[s]=false;ingredientType[s]=need;stockId[s]=s*5+need;if(ingredients[stockId[s]].Broken){if(need==3&&!ingredients[s*5+4].Broken)stockId[s]=s*5+4;else{rules.Fail(s);break;}}task[s]=1;break;
   case 1:if(stock.Broken){task[s]=0;break;}var pickup=SourcePickupPoint(stock);var raised=arm.truck.transform.TransformPoint(new Vector3(0,6.2f,4));raised.y=Mathf.Max(raised.y,pickup.y+.25f);ready=Approach(s,Source(s),dt);if(!ready){sourceRaised[s]=false;arm.Move(raised,0,false,dt);break;}if(!sourceRaised[s]){arm.Move(raised,0,false,dt);if(Vector3.Distance(arm.grip.position,raised)<.15f&&arm.grip.linearVelocity.magnitude<.7f)sourceRaised[s]=true;break;}arm.Move(pickup,0,false,dt);if(arm.Held!=null&&arm.Held!=stock)arm.Drop();if(Vector3.Distance(arm.grip.position,pickup)<.15f&&arm.grip.linearVelocity.magnitude<.7f&&arm.Grab()){if(arm.Held==stock){supplyLift[s]=stock.body.position+new Vector3(3,2.6f,0);task[s]=23;}else arm.Drop();}break;
   // Stop before reaching into the finite supply row. The original2.8m tray
   // grips from behind each vessel, then lifts TOWARDS the aisle before the
   // truck departs; neither wrist nor cargo sweeps the next untouched pot.
   case 23:Hold(arm.truck);AimHeld(s,supplyLift[s],0,dt);if(AtBody(stock,supplyLift[s]))task[s]=2;break;
   case 2:ready=Approach(s,Mixer(s),dt);AimHeld(s,arm.truck.transform.TransformPoint(new Vector3(0,4.5f,6)),0,dt);if(ready)task[s]=3;break;
   case 3:Hold(arm.truck);if(ingredientType[s]==3){var point=bowl.vessel.body.position+Vector3.up*2.8f;AimHeld(s,point,0,dt);if(arm.Held!=null&&AtBody(stock,point)){arm.Drop();task[s]=20;}break;}if(arm.Held!=stock){task[s]=0;break;}var pourBody=PourBodyTarget(bowl.vessel);if(!pourAligned[s]){AimHeld(s,pourBody,0,dt);if(AtBody(stock,pourBody)&&arm.Tilt<1)pourAligned[s]=true;break;}AimHeld(s,pourBody,35,dt);if(rules.measured[s,ingredientType[s]]>=BakeRules.Target((BakeIngredient)ingredientType[s])){task[s]=4;returnAligned[s]=false;}else if(stock.Volume<=.001f)rules.Fail(s);break;
   case 4:if(!returnAligned[s]){Hold(arm.truck);AimHeld(s,PourBodyTarget(bowl.vessel),0,dt);if(arm.Tilt<1)returnAligned[s]=true;break;}ready=Approach(s,Source(s),dt);if(ready){var aboveSupply=stock.spawn+new Vector3(3,2.6f,0);AimHeld(s,aboveSupply,0,dt);if(AtBody(stock,aboveSupply))task[s]=24;}else AimHeld(s,arm.truck.transform.TransformPoint(new Vector3(0,4.5f,6)),0,dt);break;
   case 24:Hold(arm.truck);AimHeld(s,stock.spawn+Vector3.up*.2f,0,dt);if(AtBody(stock,stock.spawn+Vector3.up*.2f)){supplyWithdraw[s]=arm.grip.position+Vector3.right*3;arm.Drop();task[s]=25;}break;
   case 25:Hold(arm.truck);arm.Move(supplyWithdraw[s],0,false,dt);if(Vector3.Distance(arm.grip.position,supplyWithdraw[s])<.15f)task[s]=0;break;
   case 20:Hold(arm.truck);arm.Move(bowl.vessel.body.position+Vector3.up*3,0,false,dt);if(rules.measured[s,3]>=.85f)task[s]=5;else if(stock.Broken){task[s]=0;}break;
   case 5:ready=Approach(s,Mixer(s),dt);arm.Move(bowl.vessel.body.position+Vector3.up*2.6f,0,false,dt);whisks[s].Running=ready;if(rules.stages[s]>=BakeStage.Mixed)task[s]=6;break;
   // R33's loose2m stop left a real8.597m tilted wrist target. Finish
   // parking at the SAME waypoint using original NWH controls before native
   // pickup. If still outside the actual workspace, continue towards that
   // waypoint instead of accepting a clamped pose or changing the arm limit.
   case 6:bool fits=BowlPourWorkspaceFits(arm,bowl.vessel,pan.vessel);ready=Approach(s,Mixer(s),dt,fits?.35f:.05f);if(!ready||!fits){arm.Move(bowl.vessel.body.position+Vector3.up*2.6f,0,false,dt);break;}arm.Move(bowl.vessel.body.position,0,false,dt);if(arm.Grab()){if(arm.Held==bowl.vessel){var lift=bowl.vessel.body.position;float panRim=PourBodyTarget(pan.vessel).y-.75f;lift.y+=Mathf.Max(0,panRim+.25f-LowestPhysicalY(bowl.vessel));supplyLift[s]=lift;task[s]=26;}else arm.Drop();}break;
   // V30b: diagonal travel from the stand struck the actual pan while the
   // bowl bottom was still below its rim, displacing the target beyond8m.
   // Lift the real held bowl vertically first; only actual collider clearance
   // and ordinary physical settlement permit the original forward alignment.
   case 26:Hold(arm.truck);AimHeld(s,supplyLift[s],0,dt);if(LowestPhysicalY(bowl.vessel)>PourBodyTarget(pan.vessel).y-.75f+.1f&&AtBody(bowl.vessel,supplyLift[s]))task[s]=7;break;
   case 7:Hold(arm.truck);var neutral=PourBodyTarget(pan.vessel);AimHeld(s,neutral,0,dt);if(AtBody(bowl.vessel,neutral)&&arm.Tilt<1)task[s]=8;break;
   case 8:Hold(arm.truck);AimHeld(s,PourBodyTarget(pan.vessel),35,dt);if(rules.stages[s]>=BakeStage.Loaded)task[s]=9;else if(bowl.vessel.Volume<=.001f)rules.Fail(s);break;
   // R32 measured native pan impulses when the tilted bowl was lowered
   // through the loaded pan. Untilting, withdrawal and lowering are distinct
   // physical moves; the original held joint and8m machinery cap remain.
   case 9:Hold(arm.truck);var upright=PourBodyTarget(pan.vessel);AimHeld(s,upright,0,dt);if(arm.Tilt<1&&AtBody(bowl.vessel,upright)&&LowestPhysicalY(bowl.vessel)>upright.y-.75f+.1f){supplyWithdraw[s]=new Vector3(bowl.vessel.spawn.x,bowl.vessel.body.position.y,bowl.vessel.spawn.z);task[s]=27;}break;
   case 27:Hold(arm.truck);AimHeld(s,supplyWithdraw[s],0,dt);if(AtBody(bowl.vessel,supplyWithdraw[s])&&BowlBehindPan(bowl.vessel,pan.vessel))task[s]=28;break;
   case 28:Hold(arm.truck);AimHeld(s,bowl.vessel.spawn+Vector3.up*.2f,0,dt);if(AtBody(bowl.vessel,bowl.vessel.spawn+Vector3.up*.2f)){arm.Drop();task[s]=10;}break;
   case 10:Hold(arm.truck);var panPickup=LoadedPanPickupPoint(pan.vessel);arm.Move(panPickup,0,false,dt);if(Vector3.Distance(arm.grip.position,panPickup)<.15f&&arm.grip.linearVelocity.magnitude<.7f&&arm.Grab()){if(arm.Held==pan.vessel){panSupportTop[s]=pan.vessel.LastSupport!=null?pan.vessel.LastSupport.bounds.max.y:pan.vessel.spawn.y;var panLift=pan.vessel.body.position;panLift.y+=Mathf.Max(0,panSupportTop[s]+.35f-LowestPhysicalY(pan.vessel));supplyLift[s]=panLift;task[s]=29;}else arm.Drop();}break;
   case 29:Hold(arm.truck);AimHeld(s,supplyLift[s],0,dt);if(LowestPhysicalY(pan.vessel)>panSupportTop[s]+.2f&&AtBody(pan.vessel,supplyLift[s])){var panBounds=VesselPhysicalBounds(pan.vessel);var bowlBounds=VesselPhysicalBounds(bowl.vessel);var aisle=pan.vessel.body.position;aisle.x=Mathf.Min(aisle.x,bowlBounds.min.x-.2f-(panBounds.max.x-pan.vessel.body.position.x));supplyWithdraw[s]=aisle;task[s]=30;}break;
   case 30:Hold(arm.truck);AimHeld(s,supplyWithdraw[s],0,dt);if(AtBody(pan.vessel,supplyWithdraw[s])&&VesselPhysicalBounds(pan.vessel).max.x+.1f<VesselPhysicalBounds(bowl.vessel).min.x)task[s]=11;break;
   case 11:bool ovenFits=OvenInsertionPoses(oven,pan.vessel,out var ovenSeat,out var ovenAbove)&&HeldBodyPoseFits(arm,pan.vessel,ovenSeat)&&HeldBodyPoseFits(arm,pan.vessel,ovenAbove);ready=ApproachWorktop(s,new Vector3(X(s),0,5),dt,ovenFits);var carryBody=arm.truck.transform.TransformPoint(new Vector3(0,3,6))+(pan.vessel.body.position-arm.grip.position);var carriedBounds=VesselPhysicalBounds(pan.vessel);carryBody.y=Mathf.Max(carryBody.y,panSupportTop[s]+.35f-(carriedBounds.min.y-pan.vessel.body.position.y));carryBody.x=Mathf.Min(carryBody.x,VesselPhysicalBounds(bowl.vessel).min.x-.2f-(carriedBounds.max.x-pan.vessel.body.position.x));AimHeld(s,carryBody,0,dt);if(ready&&ovenFits){if(oven.WantsClosed)oven.Toggle();if(oven.Open)task[s]=31;}break;
   // Align the complete pan footprint ABOVE the unchanged shelf before
   // lowering. A diagonal insertion previously drove its bottom into the
   // real shelf edge while the requested center pose was still clamped.
   case 31:Hold(arm.truck);if(OvenInsertionPoses(oven,pan.vessel,out var insertionSeat,out var insertionAbove)&&HeldBodyPoseFits(arm,pan.vessel,insertionSeat)&&HeldBodyPoseFits(arm,pan.vessel,insertionAbove)){AimHeld(s,insertionAbove,0,dt);if(AtBody(pan.vessel,insertionAbove)&&PanFootprintOnShelf(oven,pan.vessel)&&LowestPhysicalY(pan.vessel)>oven.shelf.bounds.max.y+.2f)task[s]=12;}break;
   case 12:Hold(arm.truck);if(OvenInsertionPoses(oven,pan.vessel,out var supportedSeat,out _)&&HeldBodyPoseFits(arm,pan.vessel,supportedSeat)){AimHeld(s,supportedSeat,0,dt);if(AtBody(pan.vessel,supportedSeat)&&PanFootprintOnShelf(oven,pan.vessel)&&LowestPhysicalY(pan.vessel)>oven.shelf.bounds.max.y+.02f){arm.Drop();var wristBounds=WristSolidBounds(arm);supplyLift[s]=arm.grip.position+Vector3.up*Mathf.Max(0,VesselPhysicalBounds(pan.vessel).max.y+.2f-wristBounds.min.y);task[s]=32;}}break;
   // R43 native contacts identify the inward door as the FIRST harmful
   // pair. Its outward motion is requested at the SAME real controls, then
   // waits for actual tool and original NWH assembly clearance.
   case 32:Hold(arm.truck);arm.Move(supplyLift[s],0,false,dt);if(AtWrist(arm,supplyLift[s])&&WristSolidBounds(arm).min.y>VesselPhysicalBounds(pan.vessel).max.y+.15f){var back=arm.grip.position;back.z=Mathf.Min(back.z,VesselPhysicalBounds(pan.vessel).min.z-.2f-(WristSolidBounds(arm).max.z-arm.grip.position.z));supplyWithdraw[s]=back;task[s]=33;}break;
   case 33:Hold(arm.truck);arm.Move(supplyWithdraw[s],0,false,dt);if(AtWrist(arm,supplyWithdraw[s])&&WristSolidBounds(arm).max.z<VesselPhysicalBounds(pan.vessel).min.z-.1f)task[s]=34;break;
   case 34:Hold(arm.truck);arm.Move(StowedWrist(arm),0,false,dt);if(AtWrist(arm,StowedWrist(arm))&&oven.RequestToggleAfterCrewClear()){PrepareOvenRetreat(s,oven);task[s]=35;}break;
   case 35:if(RetreatForOvenMotion(s,oven,dt)){oven.CrewClearedMotion();task[s]=13;}break;
   case 13:Hold(arm.truck);arm.Move(StowedWrist(arm),0,false,dt);if(rules.stages[s]>=BakeStage.Cooked)task[s]=36;break;
   // Retrieval uses the same actual control visit and physical retreat before
   // opening; the original tractor then returns only after the leaf is open.
   case 36:ready=ApproachWorktop(s,new Vector3(X(s),0,5),dt,rules.Clock-oven.LastVisit<=.15f&&oven.Closed);arm.Move(StowedWrist(arm),0,false,dt);if(ready&&oven.RequestToggleAfterCrewClear()){PrepareOvenRetreat(s,oven);task[s]=37;}break;
   case 37:if(RetreatForOvenMotion(s,oven,dt)){oven.CrewClearedMotion();task[s]=38;}break;
   case 38:Hold(arm.truck);arm.Move(StowedWrist(arm),0,false,dt);if(oven.Open)task[s]=14;break;
   case 14:var cookedPickup=LoadedPanPickupPoint(pan.vessel);bool cookedPickupFits=(cookedPickup-arm.truck.transform.TransformPoint(arm.mount)).sqrMagnitude<=64;ready=ApproachWorktop(s,new Vector3(X(s),0,5),dt,cookedPickupFits&&oven.Open);if(!ready||!oven.Open){arm.Move(StowedWrist(arm),0,false,dt);break;}arm.Move(cookedPickup,0,false,dt);if(AtWrist(arm,cookedPickup)&&arm.Grab()){if(arm.Held==pan.vessel){var liftedPan=pan.vessel.body.position;liftedPan.y+=Mathf.Max(0,oven.shelf.bounds.max.y+.35f-LowestPhysicalY(pan.vessel));supplyLift[s]=liftedPan;task[s]=39;}else arm.Drop();}break;
   case 39:Hold(arm.truck);AimHeld(s,supplyLift[s],0,dt);if(AtBody(pan.vessel,supplyLift[s])&&LowestPhysicalY(pan.vessel)>oven.shelf.bounds.max.y+.2f){var withdraw=pan.vessel.body.position;withdraw.z=oven.shelf.bounds.min.z-.2f-(VesselPhysicalBounds(pan.vessel).max.z-pan.vessel.body.position.z);supplyWithdraw[s]=withdraw;task[s]=40;}break;
   case 40:Hold(arm.truck);AimHeld(s,supplyWithdraw[s],0,dt);if(AtBody(pan.vessel,supplyWithdraw[s])&&VesselPhysicalBounds(pan.vessel).max.z<oven.shelf.bounds.min.z-.1f)task[s]=15;break;
   // R47 first actual contact is the counter SIDE, while the cooked pan
   // bottom is still~2.16m below its3m worktop. Lift in the aisle FIRST.
   case 15:bool counterFits=SideWorktopPoses(decorationSupports[s],pan.vessel,new Vector3(X(s)+8,3,-5),out var counterSeat,out var counterAbove)&&HeldBodyPoseFits(arm,pan.vessel,counterSeat)&&HeldBodyPoseFits(arm,pan.vessel,counterAbove)&&NozzleWorkspaceFits(s,counterSeat);ready=ApproachWorktop(s,new Vector3(X(s)+.8f,0,-7.5f),dt,counterFits);arm.Move(arm.truck.transform.TransformPoint(new Vector3(0,3,6)),0,false,dt);if(ready&&counterFits){supplyLift[s]=LiftAboveWorktop(decorationSupports[s],pan.vessel);task[s]=41;}break;
   case 41:Hold(arm.truck);AimHeld(s,supplyLift[s],0,dt);if(AtBody(pan.vessel,supplyLift[s])&&LowestPhysicalY(pan.vessel)>decorationSupports[s].bounds.max.y+.2f)task[s]=42;break;
   case 42:Hold(arm.truck);if(SideWorktopPoses(decorationSupports[s],pan.vessel,new Vector3(X(s)+8,3,-5),out _,out var aboveCounter)){AimHeld(s,aboveCounter,0,dt);if(AtBody(pan.vessel,aboveCounter)&&PanFootprintOn(decorationSupports[s],pan.vessel)&&LowestPhysicalY(pan.vessel)>decorationSupports[s].bounds.max.y+.2f)task[s]=16;}break;
   case 16:Hold(arm.truck);if(SideWorktopPoses(decorationSupports[s],pan.vessel,new Vector3(X(s)+8,3,-5),out var supportedCounter,out _)&&HeldBodyPoseFits(arm,pan.vessel,supportedCounter)&&NozzleWorkspaceFits(s,supportedCounter)){AimHeld(s,supportedCounter,0,dt);if(AtBody(pan.vessel,supportedCounter)&&PanFootprintOn(decorationSupports[s],pan.vessel)&&LowestPhysicalY(pan.vessel)>decorationSupports[s].bounds.max.y+.02f){arm.Drop();task[s]=17;}}break;
   case 17:Hold(arm.truck);int q=0;while(q<4&&(rules.decorated[s]&(1<<q))!=0)q++;if(q==4){task[s]=18;break;}quadrant[s]=q;var target=pan.vessel.body.position+new Vector3((q%2==0?-.7f:.7f),2.2f,(q<2?-.7f:.7f));var nozzle=nozzles[s];arm.Move(target-(nozzle.spout.position-arm.grip.position),0,false,dt);nozzle.pourable=Vector3.Distance(nozzle.spout.position,target)<.35f;break;
   case 18:Hold(arm.truck);var decoratedPickup=LoadedPanPickupPoint(pan.vessel);arm.Move(decoratedPickup,0,false,dt);if(AtWrist(arm,decoratedPickup)&&arm.Grab()){if(arm.Held==pan.vessel){supplyLift[s]=LiftAboveWorktop(decorationSupports[s],pan.vessel);task[s]=44;}else arm.Drop();}break;
   case 44:Hold(arm.truck);AimHeld(s,supplyLift[s],0,dt);if(AtBody(pan.vessel,supplyLift[s])&&LowestPhysicalY(pan.vessel)>decorationSupports[s].bounds.max.y+.2f){var aisleExit=pan.vessel.body.position;aisleExit.x=decorationSupports[s].bounds.min.x-.2f-(VesselPhysicalBounds(pan.vessel).max.x-pan.vessel.body.position.x);supplyWithdraw[s]=aisleExit;task[s]=45;}break;
   case 45:Hold(arm.truck);AimHeld(s,supplyWithdraw[s],0,dt);if(AtBody(pan.vessel,supplyWithdraw[s])&&VesselPhysicalBounds(pan.vessel).max.x<decorationSupports[s].bounds.min.x-.1f)task[s]=19;break;
   // Same original side-worktop geometry at the judging table: do not
   // repeat the measured undersurface diagonal after successful decoration.
   case 19:bool judgeFits=SideWorktopPoses(judges[s].table,pan.vessel,judges[s].placement.position,out var judgeSeat,out var judgeAbove)&&HeldBodyPoseFits(arm,pan.vessel,judgeSeat)&&HeldBodyPoseFits(arm,pan.vessel,judgeAbove);ready=ApproachWorktop(s,new Vector3(X(s)+.8f,0,-60.5f),dt,judgeFits);arm.Move(arm.truck.transform.TransformPoint(new Vector3(0,3,6)),0,false,dt);if(ready&&judgeFits){supplyLift[s]=LiftAboveWorktop(judges[s].table,pan.vessel);task[s]=46;}break;
   case 46:Hold(arm.truck);AimHeld(s,supplyLift[s],0,dt);if(AtBody(pan.vessel,supplyLift[s])&&LowestPhysicalY(pan.vessel)>judges[s].table.bounds.max.y+.2f)task[s]=47;break;
   case 47:Hold(arm.truck);if(SideWorktopPoses(judges[s].table,pan.vessel,judges[s].placement.position,out _,out var aboveJudge)){AimHeld(s,aboveJudge,0,dt);if(AtBody(pan.vessel,aboveJudge)&&PanFootprintOn(judges[s].table,pan.vessel)&&LowestPhysicalY(pan.vessel)>judges[s].table.bounds.max.y+.2f)task[s]=21;}break;
   case 21:Hold(arm.truck);if(SideWorktopPoses(judges[s].table,pan.vessel,judges[s].placement.position,out var presentation,out _)&&HeldBodyPoseFits(arm,pan.vessel,presentation)){AimHeld(s,presentation,0,dt);if(AtBody(pan.vessel,presentation)&&PanFootprintOn(judges[s].table,pan.vessel)&&LowestPhysicalY(pan.vessel)>judges[s].table.bounds.max.y+.02f){arm.Drop();supplyLift[s]=arm.grip.position+Vector3.up*Mathf.Max(0,VesselPhysicalBounds(pan.vessel).max.y+.2f-WristSolidBounds(arm).min.y);task[s]=48;}}break;
   case 48:Hold(arm.truck);arm.Move(supplyLift[s],0,false,dt);if(AtWrist(arm,supplyLift[s])){var finalExit=arm.grip.position;finalExit.x=judges[s].table.bounds.min.x-.2f-(WristSolidBounds(arm).max.x-arm.grip.position.x);supplyWithdraw[s]=finalExit;task[s]=49;}break;
   case 49:Hold(arm.truck);arm.Move(supplyWithdraw[s],0,false,dt);if(AtWrist(arm,supplyWithdraw[s]))task[s]=22;break;
   default:Hold(arm.truck);arm.Move(arm.truck.transform.TransformPoint(new Vector3(0,3,4)),0,false,dt);break;
  }}
  public bool Present(BakePan pan,BakeJudge judge){int s=pan.slot;var v=pan.vessel;if(s<0||s>=setup.count||judge.slot!=s||v.Holder!=null||v.Broken||v.Banked||v.LastSupport!=judge.table||v.SupportAge>.1f)return false;int score=rules.Judge(s,true,true,v.Volume);if(score<=0)return false;v.MarkBanked();JudgedCakes++;ConfirmScore("judged-cake:"+s,s,score,rules.points[s],"CAKE PRESENTED",SportsScoreTier.Major);feedback.PlayAt(3,v.body.position,0,true);judge.judge?.CrossFade("Cheer",.2f);crowd?.React(true,"CAKE JUDGED",v.body.position);return true;}
  public void Spilled(ServingVessel source,float amount,Vector3 point){}public void DishBroken(ServingVessel source,Vector3 point){BrokenIngredients++;for(int s=0;s<setup.count;s++)if(source==bowls[s].vessel||source==pans[s].vessel)rules.Fail(s);feedback.PlayAt(4,point,0,true);crowd?.React(false,"KITCHEN MISHAP",point);}
  public override void DescribeBroadcast(SportsBroadcastAction action,SportsBroadcastFraming source){int s=source.Nearest(Vector3.zero,true);if(s<0)s=0;action.primary=s;Vector3 point=arms[s].Held!=null?arms[s].Held.body.position:rules.stages[s]>=BakeStage.Loaded&&rules.stages[s]<BakeStage.Cooked?ovens[s].trayPoint.position:rules.stages[s]>=BakeStage.Cooked?pans[s].vessel.body.position:bowls[s].vessel.body.position;action.Objective(point,3);action.reason="ACTUAL BATCH / PHYSICAL TOOL / OVEN / CAKE";}
  protected override void Present(float dt){if(!Running)return;drivingViews.Tick(dt);if(drivingViews.NeedsArena&&broadcast.LegacyPoseAllowed)broadcast.Pose(arms[0].truck.body.position,new Vector3(0,23,-32),60);}public override void ExtraAction(string key){if(key=="drivingview")drivingViews.Cycle();}protected override void StopSounds(){if(arms!=null)foreach(var a in arms)if(a!=null)a.truck.StopSound();}protected override void StopGame(){Array.Clear(worktopParking,0,worktopParking.Length);Array.Clear(worktopGoalSet,0,worktopGoalSet.Length);Array.Clear(ovenRetreatGoals,0,ovenRetreatGoals.Length);if(ovens!=null)foreach(var oven in ovens)if(oven!=null)oven.CancelCrewRequest();if(arms!=null)foreach(var a in arms)if(a!=null)a.Freeze();if(vessels!=null)foreach(var v in vessels)if(v!=null)v.Freeze();StopSounds();}
 }
}
