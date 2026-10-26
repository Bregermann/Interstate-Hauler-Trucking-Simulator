#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class VolleyCarnivalPlayModeTests
{
 const BindingFlags Any=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
 static Type Runtime(string name)=>Type.GetType(name+", Assembly-CSharp",true);
 static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
 static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Any).SetValue(owner,value);
 static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethods(Any).Single(method=>method.Name==name&&method.GetParameters().Length==args.Length).Invoke(owner,args);
 sealed class Fixture:IDisposable
 {
  readonly IDisposable prefs;readonly InputSettings original,temporary;readonly GameObject isolation;
  public readonly Gamepad[] pads=new Gamepad[2];public Behaviour match;
  public Fixture(string suffix)
  {
   prefs=(IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"VolleyCarnival"+suffix});
   original=InputSystem.settings;temporary=UnityEngine.Object.Instantiate(original);temporary.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;temporary.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=temporary;
   isolation=new GameObject("Volleyball Carnival software-source isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("LWS.TruckSports.SportsProbeInputIsolation"));
   for(int i=0;i<2;i++){pads[i]=InputSystem.AddDevice<Gamepad>();InputSystem.EnableDevice(pads[i]);}
  }
  public object rules=>Get(match,"rules");
  public object Truck(int slot=0)=>((Array)Get(match,"trucks")).GetValue(slot);
  public Rigidbody Body(int slot=0)=>(Rigidbody)Get(Truck(slot),"body");
  public object Ability(int slot=0)=>((Array)Get(match,"abilities")).GetValue(slot);
  public object Handling=>Get(Get(Truck(),"tractorMotor"),"handling");
  public object Player(int slot=0)=>((Array)Get(Get(match,"setup"),"players")).GetValue(slot);
  public void Source(int slot,string source)=>Set(Player(slot),"source",Enum.Parse(Player(slot).GetType().GetField("source").FieldType,source));
  public IEnumerator Load()
  {
   EditorSceneManager.LoadSceneInPlayMode("Assets/TruckVolleyball/Scenes/TruckVolleyball.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
   match=UnityEngine.Object.FindObjectsByType(Runtime("LWS.TruckVolleyball.VolleyMatch")).Cast<Behaviour>().Single();Set(match,"teamA",1);Set(match,"teamB",1);Call(match,"NormalizeSetup");Call(match,"AllBots");
   for(int i=0;i<2;i++){Source(i,"WholeController");Set(Player(i),"pad",pads[i]);}
   var catalog=Get(Get(match,"atmosphere"),"presets");var clear=((Array)Get(catalog,"entries")).Cast<object>().Single(entry=>(string)Get(entry,"displayName")=="Clear");Call(catalog,"Select",Get(clear,"id"));
   Runtime("LWS.TruckSports.SportsCameraOptions").GetMethod("Set").Invoke(null,new object[]{false});
  }
  public IEnumerator Fresh(bool tuning=true)
  {
   Call(match,"SetupScreen");foreach(var pad in pads)InputSystem.QueueStateEvent(pad,new GamepadState());Assert.That((bool)Call(match,"StartMatch"),Is.True);if(!tuning)Call(Ability(),"Restore");
   float deadline=Time.realtimeSinceStartup+12;while((float)Get(match,"Countdown")>0&&Time.realtimeSinceStartup<deadline)yield return null;
   Assert.That((float)Get(match,"Countdown"),Is.Zero);Assert.That((bool)Get(rules,"Live"),Is.True);
   // Isolate vehicle acceptance from the independent ball/rally workload. This
   // controlled ball fixture is explicitly not a full physical-match claim.
   var ball=(Rigidbody)Get(match,"ball");ball.useGravity=false;ball.position=new Vector3(0,30,0);ball.linearVelocity=Vector3.zero;ball.angularVelocity=Vector3.zero;Physics.SyncTransforms();
   yield return new WaitForSeconds(.15f);
  }
  public void Dispose()
  {
   Time.timeScale=1;if(match!=null)Call(match,"SetupScreen");InputSystem.settings=original;foreach(var pad in pads)if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);UnityEngine.Object.DestroyImmediate(temporary);UnityEngine.Object.DestroyImmediate(isolation);prefs.Dispose();
  }
 }
 static void Capture(Fixture f,string label)
 {
  string folder=Path.GetFullPath("Logs/TruckSports/PlaytestRepair/VolleyCarnivalNative");Directory.CreateDirectory(folder);
  var canvas=((Component)Get(f.match,"menus")).GetComponent<Canvas>();var cameras=(Camera[])Get(f.match,"cameras");var save=Runtime("LWS.TruckSports.SportsControlledCapture").GetMethods().Single(method=>method.Name=="Save"&&method.GetParameters().Length==6);
  foreach(int width in new[]{1280,1920})save.Invoke(null,new object[]{canvas,cameras,width,width==1280?720:1080,Path.Combine(folder,label+"-"+width+"-controlled.png"),null});
 }
 static IEnumerator SettledSide(Fixture f)
 {
  // Controlled initial failure pose; subsequent motion is only NWH/PhysX.
  Call(f.Truck(),"Prepare",new Vector3(-18,5,-30),Quaternion.Euler(0,0,95),true);Physics.SyncTransforms();
  float deadline=Time.realtimeSinceStartup+4;do{yield return new WaitForFixedUpdate();}while((Mathf.Abs(f.Body().linearVelocity.y)>.3f||f.Body().position.y>4)&&Time.realtimeSinceStartup<deadline);
  Assert.That(Vector3.Dot(f.Body().transform.up,Vector3.up),Is.LessThan(.5f),"The actual compound tractor must remain tipped for the recovery fixture.");
 }
 static IEnumerator Drive(Fixture f,GamepadState input,float duration,Action<float,float,float> measured)
 {
  InputSystem.QueueStateEvent(f.pads[0],input);float deadline=Time.realtimeSinceStartup+3;
  while((((Vector2[])Get(f.match,"commands"))[0].y<.99f||input.leftStick.x>.1f&&((Vector2[])Get(f.match,"commands"))[0].x<.6f)&&Time.realtimeSinceStartup<deadline)yield return null;
  var delivered=((Vector2[])Get(f.match,"commands"))[0];Assert.That(delivered.y,Is.GreaterThan(.99f));if(input.leftStick.x>.1f)Assert.That(delivered.x,Is.GreaterThan(.6f));
  var origin=f.Body().position;var forward=f.Body().transform.forward;var velocity=f.Body().linearVelocity;float fixedStart=Time.fixedTime;string startPowertrain=Powertrain(f);int steps=Mathf.RoundToInt(duration/Time.fixedDeltaTime);
  for(int step=0;step<steps;step++){yield return new WaitForFixedUpdate();Assert.That(((Vector2)Get(f.Truck(),"command")).y,Is.GreaterThan(.99f),"Every measured physics step must receive the original full-throttle command.");}
  float speed=Vector3.ProjectOnPlane(f.Body().linearVelocity,Vector3.up).magnitude,travel=Vector3.ProjectOnPlane(f.Body().position-origin,Vector3.up).magnitude,turn=Vector3.Angle(forward,Vector3.ProjectOnPlane(f.Body().transform.forward,Vector3.up));
  TestContext.WriteLine("VOLLEY_EQUAL_STEP modeApplied="+Get(f.Ability(),"Applied")+" delivered="+delivered+" fixedSteps="+steps+" fixedSeconds="+(Time.fixedTime-fixedStart)+" initialVelocity="+velocity+" initialPowertrain="+startPowertrain+" finalPowertrain="+Powertrain(f)+" speed="+speed+" travel="+travel+" yaw="+turn);measured(speed,travel,turn);
  InputSystem.QueueStateEvent(f.pads[0],new GamepadState());
 }
 static string Powertrain(Fixture f)
 {
  var vehicle=Get(Get(f.Truck(),"tractorMotor"),"vehicle");var powertrain=Get(vehicle,"powertrain");var engine=Get(powertrain,"engine");var modifiers=(System.Collections.ICollection)Get(engine,"powerModifiers");var field=engine.GetType().GetField("_powerModifierSum",Any);
  var wheels=((Array)Get(f.Truck(),"wheels")).Cast<object>().ToArray();return "gear="+Get(Get(powertrain,"transmission"),"Gear")+",rpm="+Get(engine,"OutputRPM")+",powerSum="+(field!=null?field.GetValue(engine):"not-exposed")+",modifierCount="+modifiers.Count+",gripScale="+Get(Get(f.Truck(),"tractorMotor"),"gripScale")+",longSlip="+wheels.Average(wheel=>Mathf.Abs((float)Get(wheel,"LongitudinalSlip")))+",sideSlip="+wheels.Average(wheel=>Mathf.Abs((float)Get(wheel,"LateralSlip")))+",longGrip="+string.Join(",",wheels.Select(wheel=>(float)Get(wheel,"LongitudinalFrictionGrip")))+",sideGrip="+string.Join(",",wheels.Select(wheel=>(float)Get(wheel,"LateralFrictionGrip")));
 }

 [UnityTest,Timeout(180000)]
 public IEnumerator ModeMobilityImprovesActualAccelerationTravelAndTurningWithoutCompounding()
 {
  using(var f=new Fixture("Mobility"))
  {
   yield return f.Load();var originalHandling=f.Handling;float city=(float)Get(originalHandling,"citySteeringLock"),response=(float)Get(originalHandling,"steeringResponse"),smoothing=(float)Get(originalHandling,"lowSpeedSmoothing"),limit=f.Body().maxLinearVelocity,gripScale=(float)Get(Get(f.Truck(),"tractorMotor"),"gripScale");var wheels=((Array)Get(f.Truck(),"wheels")).Cast<object>().ToArray();var originalLongGrip=wheels.Select(wheel=>(float)Get(wheel,"LongitudinalFrictionGrip")).ToArray();var originalSideGrip=wheels.Select(wheel=>(float)Get(wheel,"LateralFrictionGrip")).ToArray();
   var baselineSpeed=new float[2];var baselineTravel=new float[2];var enhancedSpeed=new float[2];var enhancedTravel=new float[2];var baselineTurn=new float[2];var enhancedTurn=new float[2];
   for(int trial=0;trial<4;trial++){bool enhanced=trial==1||trial==2;int index=enhanced?trial-1:trial==0?0:1;yield return f.Fresh(enhanced);yield return Drive(f,new GamepadState{rightTrigger=1},1,(speed,travel,turn)=>{if(enhanced){enhancedSpeed[index]=speed;enhancedTravel[index]=travel;}else{baselineSpeed[index]=speed;baselineTravel[index]=travel;}});}
   for(int trial=0;trial<4;trial++){bool enhanced=trial==1||trial==2;int index=enhanced?trial-1:trial==0?0:1;yield return f.Fresh(enhanced);yield return Drive(f,new GamepadState{rightTrigger=1,leftStick=new Vector2(.65f,0)},1.2f,(speed,travel,turn)=>{if(enhanced)enhancedTurn[index]=turn;else baselineTurn[index]=turn;});Assert.That(Vector3.Dot(f.Body().transform.up,Vector3.up),Is.GreaterThan(.7f));}
   float baseSpeed=baselineSpeed.Average(),baseTravel=baselineTravel.Average(),boostedSpeed=enhancedSpeed.Average(),boostedTravel=enhancedTravel.Average(),baseTurn=baselineTurn.Average(),boostedTurn=enhancedTurn.Average();
   TestContext.WriteLine("VOLLEY_ABBA baselineSpeed="+string.Join(",",baselineSpeed)+" enhancedSpeed="+string.Join(",",enhancedSpeed)+" baselineTravel="+string.Join(",",baselineTravel)+" enhancedTravel="+string.Join(",",enhancedTravel)+" baselineTurn="+string.Join(",",baselineTurn)+" enhancedTurn="+string.Join(",",enhancedTurn));
   Assert.That(baselineSpeed.Max()-baselineSpeed.Min(),Is.LessThan(baseSpeed*.05f),"Baseline speed repeat noise must be below half the required10% improvement.");Assert.That(baselineTravel.Max()-baselineTravel.Min(),Is.LessThan(baseTravel*.05f),"Baseline travel repeat noise must be below half the required10% improvement.");Assert.That(baselineTurn.Max()-baselineTurn.Min(),Is.LessThan(1),"Baseline turn noise must be below half the required2degree improvement.");
   Assert.That(boostedSpeed,Is.GreaterThan(baseSpeed*1.1f));Assert.That(boostedTravel,Is.GreaterThan(baseTravel*1.1f));Assert.That(boostedTurn,Is.GreaterThan(baseTurn+2));
   yield return f.Fresh();
   float appliedLimit=f.Body().maxLinearVelocity,appliedCity=(float)Get(f.Handling,"citySteeringLock");Assert.That(appliedLimit,Is.EqualTo(40).Within(.001f));
   // Explicit above-cap fixture, separate from the ordinary-drive ABBA runs:
   // PhysX must actually enforce the configured bound, not merely expose it.
   foreach(float cap in new[]{36f,40f})
   {
    Call(f.Ability(),"Restore");f.Ability().GetType().GetProperty("MovementSpeedLimit").SetValue(f.Ability(),cap);Call(f.Ability(),"Apply");
    var simulation=Physics.simulationMode;var probe=new GameObject("Independent PhysX cap fixture - no NWH joints or forces").AddComponent<Rigidbody>();probe.position=new Vector3(0,100,0);probe.maxLinearVelocity=f.Body().maxLinearVelocity;probe.useGravity=false;probe.linearDamping=probe.angularDamping=0;
    try
    {
     // The independent zero-drag/gravity body uses the actual mode cap. No
     // yield/FixedUpdate can submit NWH forces, and no joint solver transfers
     // momentum from uncapped wheel bodies. Existing vehicle settings stay
     // untouched; ordinary ABBA above is the actual NWH mobility evidence.
     Physics.simulationMode=SimulationMode.Script;probe.linearVelocity=new Vector3(80,0,0);Physics.SyncTransforms();Assert.That(probe.linearVelocity.magnitude,Is.GreaterThan(cap));
     Physics.Simulate(Time.fixedDeltaTime);float first=probe.linearVelocity.magnitude;Assert.That(first,Is.LessThanOrEqualTo(cap+.05f));Physics.Simulate(Time.fixedDeltaTime);float second=probe.linearVelocity.magnitude;
     TestContext.WriteLine("VOLLEY_PHYSX_CAP configured="+cap+" property="+f.Body().maxLinearVelocity+" injected=80 manualSteps=2 first="+first+" second="+second+" nwhFixedUpdates=0 gravityAndDrag=0");
     Assert.That(f.Body().maxLinearVelocity,Is.EqualTo(cap));Assert.That(second,Is.LessThanOrEqualTo(cap+.05f),"The actual configured public PhysX property must enforce an upper bound independently of NWH forces.");
    }
    finally{Physics.simulationMode=simulation;UnityEngine.Object.DestroyImmediate(probe.gameObject);}
   }
   for(int i=0;i<2;i++){yield return f.Fresh();Assert.That(f.Body().maxLinearVelocity,Is.EqualTo(appliedLimit));Assert.That((float)Get(f.Handling,"citySteeringLock"),Is.EqualTo(appliedCity));Assert.That((float)Get(Get(f.Truck(),"tractorMotor"),"gripScale"),Is.EqualTo((float)Get(Get(f.match,"atmosphere"),"Grip")*1.35f).Within(.0001f),"Each rematch retains one mode multiplier on the actual weather grip.");}
   Call(f.match,"SetupScreen");Assert.That(f.Body().maxLinearVelocity,Is.EqualTo(limit));Assert.That((float)Get(f.Handling,"citySteeringLock"),Is.EqualTo(city));Assert.That((float)Get(f.Handling,"steeringResponse"),Is.EqualTo(response));Assert.That((float)Get(f.Handling,"lowSpeedSmoothing"),Is.EqualTo(smoothing));Assert.That((float)Get(Get(f.Truck(),"tractorMotor"),"gripScale"),Is.EqualTo(gripScale));CollectionAssert.AreEqual(originalLongGrip,wheels.Select(wheel=>(float)Get(wheel,"LongitudinalFrictionGrip")).ToArray());CollectionAssert.AreEqual(originalSideGrip,wheels.Select(wheel=>(float)Get(wheel,"LateralFrictionGrip")).ToArray());
   TestContext.WriteLine("Physical matched one-second straight runs: baseline speed/travel="+baseSpeed+"/"+baseTravel+", Volley="+boostedSpeed+"/"+boostedTravel+"; matched1.2sec turn="+baseTurn+"→"+boostedTurn+"deg. Not a terminal-speed claim. Temporary mode speed cap40 and exact handling restoration/rematch idempotence checked.");
  }
 }

 [UnityTest,Timeout(90000)]
 public IEnumerator AssignedJumpUsesNinePointTwoMetresAndSharesRecoveryCooldown()
 {
  using(var f=new Fixture("Jump"))
  {
   yield return f.Load();yield return f.Fresh();Assert.That((float)Get(f.match,"jumpHeight"),Is.EqualTo(8));Assert.That((float)Get(f.match,"EffectiveJumpHeight"),Is.EqualTo(9.2f).Within(.001f));
   float origin=f.Body().position.y;InputSystem.QueueStateEvent(f.pads[0],new GamepadState().WithButton(GamepadButton.South));float deadline=Time.realtimeSinceStartup+2;
   while((int)Get(f.match,"ActualJumps")==0&&Time.realtimeSinceStartup<deadline)yield return null;
   Assert.That((int)Get(f.match,"ActualJumps"),Is.EqualTo(1));Assert.That(f.Body().linearVelocity.y,Is.InRange(12f,14f));Assert.That((bool)Call(f.match,"Jump",0),Is.False);InputSystem.QueueStateEvent(f.pads[0],new GamepadState());
   float peak=f.Body().position.y;deadline=Time.realtimeSinceStartup+3;while(f.Body().linearVelocity.y>0&&Time.realtimeSinceStartup<deadline){yield return null;peak=Mathf.Max(peak,f.Body().position.y);}
   Assert.That(peak-origin,Is.InRange(7.5f,10.3f),"Actual NWH/PhysX apex remains near the9.2m target rather than an arbitrary large boost.");Capture(f,"jump-apex");Assert.That((int)Get(f.match,"ActualJumps"),Is.EqualTo(1));Assert.That((int)Get(Get(f.match,"ScoreJournal"),"Count"),Is.Zero);
   yield return f.Fresh();Assert.That((int)Get(f.match,"ActualJumps"),Is.Zero);Assert.That((bool)Call(f.match,"Jump",0),Is.True,"Rematch clears the actual shared cooldown.");
   TestContext.WriteLine("Assigned software JUMP caused one actual assembly flight, measured rise="+(peak-origin)+"m, saved8m×1.15 tuning, cooldown and no vehicle-action score. Ball was explicitly isolated; fullVolleyProbe remains the physical rally acceptance.");
  }
 }

 [UnityTest,Timeout(120000)]
 public IEnumerator TippedAssignedJumpPhysicallyRightsInPlaceAndAIUsesTheSameAbility()
 {
  using(var f=new Fixture("Recovery"))
  {
   yield return f.Load();yield return f.Fresh();yield return SettledSide(f);var fallen=f.Body().position;float largestStep=0;var previous=fallen;float deadline=Time.realtimeSinceStartup+5;
   while(Time.realtimeSinceStartup<deadline){yield return new WaitForFixedUpdate();largestStep=Mathf.Max(largestStep,Vector3.Distance(f.Body().position,previous));previous=f.Body().position;}
   Assert.That((int)Get(f.match,"ActualJumps"),Is.Zero);Assert.That(largestStep,Is.LessThan(1.5f),"No old four-second automatic reset discontinuity.");Assert.That(Vector3.ProjectOnPlane(f.Body().position-fallen,Vector3.up).magnitude,Is.LessThan(4));Assert.That(Vector3.Dot(f.Body().transform.up,Vector3.up),Is.LessThan(.5f));
   fallen=f.Body().position;InputSystem.QueueStateEvent(f.pads[0],new GamepadState().WithButton(GamepadButton.South));deadline=Time.realtimeSinceStartup+2;while((int)Get(f.match,"ActualJumps")==0&&Time.realtimeSinceStartup<deadline)yield return null;
   Assert.That((int)Get(f.match,"ActualJumps"),Is.EqualTo(1));Assert.That(f.Body().linearVelocity.y,Is.GreaterThan(3));Assert.That(f.Body().angularVelocity.magnitude,Is.InRange(.5f,4.1f));Assert.That(Vector3.ProjectOnPlane(f.Body().position-fallen,Vector3.up).magnitude,Is.LessThan(1));Assert.That((bool)Call(f.match,"Jump",0),Is.False);InputSystem.QueueStateEvent(f.pads[0],new GamepadState());
   deadline=Time.realtimeSinceStartup+4;while((Vector3.Dot(f.Body().transform.up,Vector3.up)<.85f||!(bool)Get(Get(f.Truck(),"tractorMotor"),"Grounded"))&&Time.realtimeSinceStartup<deadline)yield return null;
   Assert.That(Vector3.Dot(f.Body().transform.up,Vector3.up),Is.GreaterThan(.85f));Assert.That((bool)Get(Get(f.Truck(),"tractorMotor"),"Grounded"),Is.True);Capture(f,"recovered-in-place");
   var restored=f.Body().position;InputSystem.QueueStateEvent(f.pads[0],new GamepadState{rightTrigger=1});yield return new WaitForSeconds(.6f);Assert.That(Vector3.ProjectOnPlane(f.Body().position-restored,Vector3.up).magnitude,Is.GreaterThan(.2f),"Actual NWH propulsion resumes after righting.");InputSystem.QueueStateEvent(f.pads[0],new GamepadState());
   yield return f.Fresh();yield return SettledSide(f);Assert.That((bool)Call(f.match,"Jump",0),Is.True);Assert.That((bool)Get(f.Ability(),"Recovering"),Is.True);Call(f.match,"Pause",0,false);Assert.That((bool)Get(f.Ability(),"Recovering"),Is.False);Call(f.match,"Resume");
   yield return f.Fresh();yield return SettledSide(f);f.Source(0,"AI");deadline=Time.realtimeSinceStartup+4;while((int)Get(f.match,"ActualJumps")==0&&Time.realtimeSinceStartup<deadline)yield return null;Assert.That((int)Get(f.match,"ActualJumps"),Is.EqualTo(1));
   deadline=Time.realtimeSinceStartup+4;while(Vector3.Dot(f.Body().transform.up,Vector3.up)<.85f&&Time.realtimeSinceStartup<deadline)yield return null;Assert.That(Vector3.Dot(f.Body().transform.up,Vector3.up),Is.GreaterThan(.85f));Assert.That((int)Get(Get(f.match,"ScoreJournal"),"Count"),Is.Zero);
   yield return f.Fresh();Assert.That((int)Get(f.match,"ActualJumps"),Is.Zero);Assert.That((bool)Get(f.Ability(),"Recovering"),Is.False);Assert.That(((int[])Get(f.rules,"points")).Sum(),Is.Zero);
   TestContext.WriteLine("Controlled side-drop fixture: five seconds without Jump had no automatic reset; assigned Jump used upward velocity+bounded torque to regain actual wheel support and propulsion. Same cooldown/ability for AI, pause cancellation, rematch and no score checked. No pose correction after initial drop fixture.");
  }
 }
}
#endif
