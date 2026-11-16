#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Actual saved scenes + original session sampling. Software devices only;
// no session command, body pose/velocity, AI goal or score injection.
public sealed partial class NativeDragonInputTests
{
    const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    Component match;GameObject isolation;IDisposable preferences;InputSettings original,temporary;Keyboard keyboard;Mouse riderMouse;readonly Gamepad[] pads=new Gamepad[4];
    readonly StringBuilder report=new StringBuilder();string mode,reportName,output;Array players,trucks;object views;bool riders;
    int observedStaleNativeModeResets;readonly HashSet<string> observedPrepareModeReceipts=new HashSet<string>();
    string riderCaptureFolder="RidersFocused";bool transientCabCandidate;float transientSourceEnvelopeFlap;bool mixedRiderEnvelope,safeMountedEnvelope;readonly List<UnityEngine.Object> transientCabObjects=new List<UnityEngine.Object>();
    bool transientFireSize,transientFireNormalFamily,transientFireHeadLever;IDisposable transientFireSizeLease;Component transientFireGeometryObserver;
    [Serializable]sealed class SizedFireLiveGeometry{public int owner,frame,finalAttachmentFrame,triangles,boxes,satPairs,wheels;public float actualNormalized,maximumActualWheelGap;public string action;public bool physicallyMounted,allActualWheelsFound;}
    [Serializable]sealed class CabTransientViews{public CabTransientView[] views=Array.Empty<CabTransientView>();}
    [Serializable]sealed class CabTransientView{public int owner;public bool exactOwnedStatePreserved;public string actualBaseClips="",actualWheelDeckSeating="";public CabTransientGeometry[] pureConnectedNeckCandidates=Array.Empty<CabTransientGeometry>();}
    [Serializable]sealed class CabWheelDeckSeating{public string scope="",context="";public CabWheelContact[] wheels=Array.Empty<CabWheelContact>();}
    [Serializable]sealed class CabWheelContact{public string name="",deckFaceSource="";public bool actualVisualFound=false,deckFaceFound=false;public float actualGap=0;}
    [Serializable]sealed class CabTransientGeometry{public int actualTrueBakeTriangleBoxIntersections,triangleBoxIntersections,actualOriginalBoxes,completeSkinTriangles;public CabTransientRays[] sameFrameCabRays=Array.Empty<CabTransientRays>();}
    [Serializable]sealed class CabTransientRays{public string name="";public int ownHeadNeckDominantHits;public CabTransientRay[] centralWindscreenRays=Array.Empty<CabTransientRay>();}
    [Serializable]sealed class CabTransientRay{public Vector2 viewport;public bool ownDragon;public float headNeckWeight;}
    static Type Runtime(string name)=>Type.GetType(name+", Assembly-CSharp",true);
    static object Read(object value,string name)=>value.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(value):value.GetType().GetProperty(name,Any).GetValue(value);
    static void Set(object value,string name,object data)=>value.GetType().GetField(name,Any).SetValue(value,data);
    static object Call(object value,string name,params object[] args)=>value.GetType().GetMethods(Any).Single(method=>method.Name==name&&method.GetParameters().Length==args.Length&&method.GetParameters().Select((parameter,index)=>args[index]==null||parameter.ParameterType.IsInstanceOfType(args[index])).All(ok=>ok)).Invoke(value,args);
    void Log(string text){report.AppendLine(text);TestContext.WriteLine(text);if(output!=null)File.WriteAllText(Path.Combine(output,reportName+".txt"),report.ToString());}
    static string Diagnostic(object value,string name)
    {
        if(value==null)return "none";
        try{return Read(value,name)?.ToString()??"null";}catch(MissingMemberException){return "unavailable";}catch(NullReferenceException){return "unavailable";}
    }
    void ObserveDrive(int slot,string phase)
    {
        var body=Body(slot);string text="ACTUAL_INPUT_DRIVE phase="+phase+" slot="+slot+" source="+Source(slot)+" clock="+Read(match,"Clock")+" live="+Read(match,"Live")+" released="+Read(Player(slot),"released")+" raw="+Call(match,"ReadDrive",slot)+" sampled="+((Vector2[])Read(match,"commands"))[slot]+" position="+Position(slot)+" velocity="+body.linearVelocity+" command="+Read(Truck(slot),"command");
        if(riders)
        {
            var racer=((Array)Read(match,"racers")).GetValue(slot);var motor=FlightMotor(slot);
            var animal=Read(motor,"animal");var state=Read(animal,"ActiveState");var animator=(Animator)Read(motor,"animator");var animation=animator.GetCurrentAnimatorStateInfo(0);var dragon=Read(racer,"Dragon");text+=" actorClock="+Read(dragon,"clock")+" actorReadyAt="+Read(dragon,"attackReadyAt")+" actorWarning="+Read(dragon,"WarningSecondsRemaining")+" riderReadyIn="+Read(racer,"AttackReadyIn");
            text+=" lastRealActionAttemptFrame="+Diagnostic(motor,"LastActionRequestFrame")+" lastRealActionAttemptTime="+Diagnostic(motor,"LastActionRequestTime")+" lastRealActionAttempt="+Diagnostic(motor,"LastActionRequestDiagnosis");
            var actualBody=(Rigidbody)Read(motor,"body");var hip=(RaycastHit)Read(animal,"hit_Hip");var chest=(RaycastHit)Read(animal,"hit_Chest");text+=" motorPaused="+Read(motor,"Paused")+" actorPaused="+Read(dragon,"Paused")+" dragonKinematic="+actualBody.isKinematic+" dragonSleeping="+actualBody.IsSleeping()+" disablePosition="+Diagnostic(animal,"DisablePosition")+" disableRotation="+Diagnostic(animal,"DisableRotation")+" lockMovement="+Diagnostic(animal,"LockMovement")+" lockInput="+Diagnostic(animal,"LockInput")+" animalSleep="+Diagnostic(animal,"Sleep")+" stagedHeld="+Diagnostic(motor,"StagedAnimationHeld")+" hipCollider="+(hip.collider!=null?hip.collider.name+"/"+hip.collider.transform.root.name+"/layer"+hip.collider.gameObject.layer:"none")+" hipPoint="+hip.point+" chestCollider="+(chest.collider!=null?chest.collider.name+"/"+chest.collider.transform.root.name+"/layer"+chest.collider.gameObject.layer:"none")+" chestPoint="+chest.point;
            text+=" nativePlayingMode="+Diagnostic(animal,"IsPlayingMode")+" nativePreparingMode="+Diagnostic(animal,"IsPreparingMode")+" nativeStateNoModes="+Diagnostic(state,"NoModes")+" action="+Read(motor,"CurrentAction")+" actionPlaying="+Read(motor,"ActionPlaying")+" actionNormalized="+Read(motor,"ActionNormalizedTime");
            foreach(var nativeMode in ((IEnumerable)Read(animal,"modes")).Cast<object>()){var ability=Read(nativeMode,"ActiveAbility");text+=" nativeMode=["+Diagnostic(nativeMode,"Name")+",active="+Diagnostic(nativeMode,"Active")+",temporal="+Diagnostic(nativeMode,"TemporalActivation")+",playing="+Diagnostic(nativeMode,"PlayingMode")+",inCooldown="+Diagnostic(nativeMode,"InCoolDown")+",hasCooldown="+Diagnostic(nativeMode,"HasCoolDown")+",activation="+Diagnostic(nativeMode,"ActivationTime")+",cooldown="+Diagnostic(nativeMode,"CoolDown")+",ability="+Diagnostic(ability,"Name")+"]";}
            text+=" flight="+Diagnostic(motor,"IsFlying")+" grounded="+Diagnostic(motor,"IsGrounded")+" motorVelocity="+Diagnostic(motor,"Velocity")+" riderPhase="+Diagnostic(racer,"Phase")+" dislodgement="+Diagnostic(racer,"LastDislodgementReason")+" vendorState="+(state?.GetType().Name??"none")+" vendorCore="+Diagnostic(state,"InCoreAnimation")+" baseState="+animation.fullPathHash+" baseNormalized="+animation.normalizedTime+" baseLength="+animation.length+" transition="+animator.IsInTransition(0)+" baseClips=["+string.Join(";",animator.GetCurrentAnimatorClipInfo(0).Select(value=>value.clip.name+"@"+value.weight))+"] nextClips=["+string.Join(";",animator.GetNextAnimatorClipInfo(0).Select(value=>value.clip.name+"@"+value.weight))+"]";
        }
        else
        {
            var motor=Read(Truck(slot),"tractorMotor");var vehicle=Read(motor,"vehicle");var power=Read(vehicle,"powertrain");var engine=Read(power,"engine");var transmission=Read(power,"transmission");var state=Read(Read(vehicle,"input"),"states");
            text+=" powered="+Read(Truck(slot),"Powered")+" enabled="+Diagnostic(vehicle,"enabled")+" grounded="+Read(motor,"Grounded")+" throttle="+Diagnostic(state,"throttle")+" brake="+Diagnostic(state,"brakes")+" handbrake="+Diagnostic(state,"handbrake")+" ignition="+Diagnostic(engine,"ignition")+" engineRunning="+Diagnostic(engine,"IsRunning")+" stalled="+Diagnostic(engine,"IsStalled")+" starterActive="+Diagnostic(engine,"starterActive")+" flyingStart="+Diagnostic(engine,"flyingStartEnabled")+" idleRPM="+Diagnostic(engine,"idleRPM")+" rpmPercent="+Diagnostic(engine,"RPMPercent")+" startDuration="+Diagnostic(engine,"startDuration")+" gear="+Diagnostic(transmission,"Gear")+" ratio="+Diagnostic(transmission,"currentGearRatio")+" shifting="+Diagnostic(transmission,"IsShifting");
            var clutch=Read(power,"clutch");var handling=Read(motor,"handling");
            text+=" swappedThrottle="+Diagnostic(Read(vehicle,"input"),"InputSwappedThrottle")+" engineThrottle="+Diagnostic(engine,"_throttlePosition")+" userThrottle="+Diagnostic(engine,"_userThrottleInput")+" generatedPower="+Diagnostic(engine,"generatedPower")+" powerModifier="+Diagnostic(engine,"_powerModifierSum")+" maxPower="+Diagnostic(engine,"maxPower")+" lossPercent="+Diagnostic(engine,"engineLossPercent")+" damage="+Diagnostic(engine,"Damage")+" engineRPM="+Diagnostic(engine,"OutputRPM")+" stallRPM="+Diagnostic(engine,"stallRPM");
            text+=" revLimiterActive="+Diagnostic(engine,"revLimiterActive")+" revLimiterDuration="+Diagnostic(engine,"revLimiterCutoffDuration");
            text+=" clutchEngagement="+Diagnostic(clutch,"Engagement")+" clutchInput="+Diagnostic(clutch,"clutchInput")+" clutchControl="+Diagnostic(clutch,"controlType")+" clutchEngagementRPM="+Diagnostic(clutch,"engagementRPM")+" clutchSlipTorque="+Diagnostic(clutch,"slipTorque")+" engineInputTorque="+Diagnostic(engine,"inputTorque")+" engineOutputTorque="+Diagnostic(engine,"outputTorque")+" handlingApplied="+Diagnostic(handling,"Applied")+" launchModifier="+Diagnostic(handling,"CurrentPowerMultiplier");
            var tcsWrapper=((Component)vehicle).GetComponents<Component>().FirstOrDefault(value=>value!=null&&value.GetType().Name=="TCSModuleWrapper");var tcs=tcsWrapper!=null?Read(tcsWrapper,"module"):null;
            // Read cached values and delegate identities only. Calling a power
            // modifier to inspect it can itself fire TCS events/change state.
            text+=" localForwardVelocity="+Diagnostic(vehicle,"LocalForwardVelocity")+" tcsActive="+Diagnostic(tcs,"active")+" tcsEnabled="+Diagnostic(tcs,"IsActive")+" tcsLowSpeed="+Diagnostic(tcs,"lowerSpeedThreshold")+" tcsSlipThreshold="+Diagnostic(tcs,"slipThreshold");
            text+=" modifiers=["+string.Join(";",((IEnumerable)Read(engine,"powerModifiers")).Cast<Delegate>().Select(value=>(value.Target?.GetType().FullName??"static")+"."+value.Method.Name))+"] wheels=["+string.Join(";",((IEnumerable)Read(power,"wheels")).Cast<object>().Select((value,index)=>{var wheel=Read(value,"wheelUAPI");return index+":grounded="+Diagnostic(wheel,"IsGrounded")+",longSlip="+Diagnostic(wheel,"LongitudinalSlip")+",rpm="+Diagnostic(wheel,"RPM");}))+ "]";
        }
        Log(text);
    }
    IEnumerator AwaitActualRiderCoreFlight(int slot)
    {
        var motor=FlightMotor(slot);if((bool)Read(motor,"IsFlying"))yield break;
        var animator=(Animator)Read(motor,"animator");bool actualTakeoff=animator.GetCurrentAnimatorClipInfo(0).Concat(animator.GetNextAnimatorClipInfo(0)).Any(value=>value.weight>0&&value.clip.name.IndexOf("Takeoff",StringComparison.OrdinalIgnoreCase)>=0);
        ObserveDrive(slot,"before-natural-core-flight-wait");
        Assert.That(Read(motor,"IsGrounded"),Is.EqualTo(false),"Only an observed airborne native Takeoff may wait beyond the measured assigned-input response window.");
        Assert.That(actualTakeoff,Is.True,"Do not hide a missing or unrelated flight state behind a generic delay: record actual native Takeoff playback first.");
        // V225 observed genuine movement/yaw at1.18s, while V223 records this
        // same original1.8s Takeoff at.578. Core Fly follows its normal.92 exit.
        // Keep the input held; never force a Malbers state or Animator time.
        float deadline=Time.realtimeSinceStartup+6,next=Time.realtimeSinceStartup+.25f;
        while(!(bool)Read(motor,"IsFlying")&&Time.realtimeSinceStartup<deadline)
        {
            if(Time.realtimeSinceStartup>=next){ObserveDrive(slot,"natural-takeoff-transition");next=Time.realtimeSinceStartup+.25f;}
            yield return new WaitForFixedUpdate();
        }
        ObserveDrive(slot,"after-natural-core-flight-wait");AssertExclusiveDrive(slot);
        Assert.That(Read(motor,"IsFlying"),Is.EqualTo(true),"Actual native Takeoff must naturally enter the real Malbers core Fly within the bounded observation window.");
    }
    object Player(int slot)=>players.GetValue(slot);
    string Source(int slot)=>Read(Player(slot),"source").ToString();
    Component Truck(int slot)=>(Component)trucks.GetValue(slot);
    Rigidbody Body(int slot)=>(Rigidbody)Read(Truck(slot),"body");
    Component FlightMotor(int slot)=>(Component)Read(Read(((Array)Read(match,"racers")).GetValue(slot),"Dragon"),"motor");
    Vector3 Position(int slot)=>riders?((Rigidbody)Read(FlightMotor(slot),"body")).position:Body(slot).position;
    Rigidbody ObjectiveHoist(int slot)
    {
        if(mode!="TruckHoardHeist")return null;
        return (Rigidbody)Read(((Array)Read(match,"hoists")).GetValue(slot),"spreader");
    }
    string Identity()=>string.Join(";",Enumerable.Range(0,4).Select(slot=>slot+":"+Read(Player(slot),"participantId")+":"+Read(Player(slot),"name")+":"+Read(Player(slot),"color")+":"+Source(slot)+":"+((Gamepad)Read(Player(slot),"pad"))?.deviceId+":"+Body(slot).GetEntityId()));
    void Neutral(){InputSystem.QueueStateEvent(keyboard,new KeyboardState());foreach(var pad in pads)InputSystem.QueueStateEvent(pad,new GamepadState());if(riderMouse!=null)InputSystem.QueueStateEvent(riderMouse,new MouseState());}
    void AssignSource(int slot,string source,int pad=-1){var player=Player(slot);Set(player,"source",Enum.Parse(player.GetType().GetField("source").FieldType,source));Set(player,"pad",pad<0?null:pads[pad]);}
    void Configure(int humans)
    {
        Call(match,"SetupScreen");Set(Read(match,"setup"),"count",InputParticipantCount);Call(match,"AllBots");if(coldInfernoFixture)for(int inactive=1;inactive<4;inactive++)AssignSource(inactive,"AI");
        // Deliberately noncontiguous owners and nonzero pad mapping detect
        // accidental slot-zero/first-device assumptions.
        if(humans==1)AssignSource(coldInfernoFixture?0:2,"Keyboard");
        if(humans==2){AssignSource(0,"WholeController",2);AssignSource(3,"Keyboard");}
        if(humans==3){AssignSource(0,"LeftStick",1);AssignSource(1,"RightStick",1);AssignSource(3,"Keyboard");}
        if(humans==4){AssignSource(0,"Keyboard");AssignSource(1,"WholeController",2);AssignSource(2,"LeftStick",0);AssignSource(3,"RightStick",0);}
        for(int slot=0;slot<4;slot++)if((bool)Read(Player(slot),"Controller"))Assert.That(Call(match,"Assign",slot,Read(Player(slot),"pad")),Is.EqualTo(true));
        Assert.That(Read(match,"DevicesReady"),Is.EqualTo(true));Assert.That(Enumerable.Range(0,4).Count(slot=>Source(slot)!="AI"),Is.EqualTo(humans));
    }
    void PulseDrive(int slot,bool action=false)
    {
        var source=Source(slot);if(source=="Keyboard")
        {InputSystem.QueueStateEvent(keyboard,action?new KeyboardState(((Key[])Read(match,"actionKeys"))[slot]):new KeyboardState((Key)Read(Player(slot),"forward"),(Key)Read(Player(slot),"right")));return;}
        var state=new GamepadState();if(source=="WholeController"){state.leftStick=new Vector2(.75f,.8f);state.rightTrigger=.7f;if(action)state=state.WithButton(GamepadButton.South);}
        else if(source=="LeftStick"){state.leftStick=new Vector2(.75f,.8f);state.leftTrigger=action?1:0;}
        else{state.rightStick=new Vector2(.75f,.8f);state.rightTrigger=action?1:0;}
        InputSystem.QueueStateEvent((Gamepad)Read(Player(slot),"pad"),state);
    }
    void AssertExclusiveDrive(int owner)
    {
        foreach(int slot in Enumerable.Range(0,4).Where(slot=>Source(slot)!="AI"))
        {
            var raw=(Vector2)Call(match,"ReadDrive",slot);var sampled=((Vector2[])Read(match,"commands"))[slot];
            if(slot==owner){Assert.That(raw.x,Is.GreaterThan(.2f));Assert.That(raw.y,Is.GreaterThan(.2f));Assert.That(sampled.x,Is.GreaterThan(.2f));Assert.That(sampled.y,Is.GreaterThan(.2f));}
            else{Assert.That(raw.sqrMagnitude,Is.LessThan(.0001f),"Other human consumed the owner's device: "+slot);Assert.That(sampled.sqrMagnitude,Is.LessThan(.0001f));}
        }
    }
    void AssertLayout(int humans,bool showAI,string identity)
    {
        var rally=Read(views,"rally");var owners=((int[])Read(rally,"owners")).Take((int)Read(rally,"Humans")).ToArray();
        int[] expected=Enumerable.Range(0,InputParticipantCount).Where(slot=>Source(slot)!="AI").Concat(showAI?Enumerable.Range(0,InputParticipantCount).Where(slot=>Source(slot)=="AI"):Enumerable.Empty<int>()).ToArray();
        Assert.That(owners,Is.EqualTo(expected));Assert.That(owners.Distinct().Count(),Is.EqualTo(owners.Length));Assert.That(owners.Length,Is.EqualTo(showAI?InputParticipantCount:humans));
        var cameras=(Camera[])Read(match,"cameras");Assert.That(cameras.Length,Is.EqualTo(owners.Length));Assert.That(cameras.Distinct().Count(),Is.EqualTo(cameras.Length));float area=0;
        for(int i=0;i<cameras.Length;i++)
        {
            var camera=cameras[i];Assert.That(camera.enabled&&camera.gameObject.activeInHierarchy,Is.True);var rect=camera.rect;Assert.That(rect.xMin,Is.GreaterThanOrEqualTo(0));Assert.That(rect.yMin,Is.GreaterThanOrEqualTo(0));Assert.That(rect.xMax,Is.LessThanOrEqualTo(1));Assert.That(rect.yMax,Is.LessThanOrEqualTo(1));area+=rect.width*rect.height;
            for(int j=0;j<i;j++){var other=cameras[j].rect;float overlap=Mathf.Max(0,Mathf.Min(rect.xMax,other.xMax)-Mathf.Max(rect.xMin,other.xMin))*Mathf.Max(0,Mathf.Min(rect.yMax,other.yMax)-Mathf.Max(rect.yMin,other.yMin));Assert.That(overlap,Is.LessThan(.0001f));}
        }
        Assert.That(area,Is.EqualTo(1).Within(.0001f));Assert.That(Identity(),Is.EqualTo(identity));Assert.That(Enumerable.Range(0,4).All(slot=>slot<InputParticipantCount?Truck(slot).gameObject.activeInHierarchy&&Body(slot).detectCollisions:!Truck(slot).gameObject.activeInHierarchy),Is.True,"Camera choices must preserve every actual competitor.");
        Log("ACTUAL_VIEW_OWNERS mode="+mode+" humans="+humans+" showAI="+showAI+" viewMode="+Read(views,"Mode")+" owners=["+string.Join(",",owners)+"] rects=["+string.Join(";",cameras.Select(camera=>camera.rect.ToString()))+"] identity="+identity);
    }
    IEnumerator CameraOwnership(int humans,string identity)
    {
        Neutral();yield return null;yield return null;Call(match,"Pause",0,false);yield return null;
        // The existing camera fixtures use the public Play presentation while
        // simulation remains paused. SOUTH is then an ownership sample rather
        // than a focused Pause-menu submit; no gameplay action is consumed.
        Call(Read(match,"menus"),"Show",Enum.Parse(Runtime("LWS.TruckSports.SportsEventScreen"),"Play"));yield return null;
        var poses=Enumerable.Range(0,4).Select(slot=>new Pose(Body(slot).position,Body(slot).rotation)).ToArray();float clock=(float)Read(match,"Clock");
        foreach(bool ai in new[]{false,true})foreach(int cameraMode in new[]{2,1})
        {
            Runtime("LWS.TruckSports.SportsCameraOptions").GetMethod("Set").Invoke(null,new object[]{ai});Call(views,"Select",cameraMode);yield return null;yield return null;AssertLayout(humans,ai,identity);
            var cabs=(Transform[])Read(views,"cabs");for(int view=0;view<((Camera[])Read(match,"cameras")).Length;view++)Assert.That(cabs[view].gameObject.activeInHierarchy,Is.EqualTo(cameraMode==2));
            for(int slot=0;slot<4;slot++){Assert.That(Vector3.Distance(Body(slot).position,poses[slot].position),Is.LessThan(.0001f));Assert.That(Quaternion.Angle(Body(slot).rotation,poses[slot].rotation),Is.LessThan(.001f));}Assert.That((float)Read(match,"Clock"),Is.EqualTo(clock));
        }
        foreach(int slot in Enumerable.Range(0,4).Where(slot=>Source(slot)!="AI"))
        {
            Neutral();yield return null;PulseDrive(slot,true);yield return null;yield return null;
            foreach(int other in Enumerable.Range(0,4).Where(other=>Source(other)!="AI"))Assert.That(Call(match,"ActionHeld",other),Is.EqualTo(slot==other),"Assigned action leaked to another crew.");
        }
        Neutral();yield return null;yield return null;
        InputSystem.QueueStateEvent(pads[3],new GamepadState{leftStick=Vector2.one,rightStick=Vector2.one,rightTrigger=1,leftTrigger=1}.WithButton(GamepadButton.South));yield return null;yield return null;
        foreach(int slot in Enumerable.Range(0,4).Where(slot=>Source(slot)!="AI")){Assert.That(((Vector2)Call(match,"ReadDrive",slot)).sqrMagnitude,Is.LessThan(.0001f));Assert.That(Call(match,"ActionHeld",slot),Is.EqualTo(false),"Unassigned fourth software pad acquired a crew.");}
        Neutral();yield return null;Call(match,"Resume");yield return null;yield return null;
    }
    void ObserveActualPrepareModeResets(string phase)
    {
        for(int slot=0;slot<InputParticipantCount;slot++)
        {
            var motor=FlightMotor(slot);int frame=(int)Read(motor,"LastPrepareModeResetFrame");if(observedPrepareModeReceipts.Add(slot+":"+frame))observedStaleNativeModeResets+=(int)Read(motor,"LastPrepareStalePlayingModesReset");
            Log("ACTUAL_PREPARE_MODE_RESET phase="+phase+" slot="+slot+" frame="+frame+" resetPlaying="+Read(motor,"LastPreparePlayingModesReset")+" resetStale="+Read(motor,"LastPrepareStalePlayingModesReset")+" facts="+Read(motor,"LastPrepareModeResetDiagnosis"));
        }
    }
    string ActualRiderModeConfiguration()
    {
        return string.Join(" | ",Enumerable.Range(0,InputParticipantCount).Select(slot=>slot+":"+string.Join(";",((IEnumerable)Read(Read(FlightMotor(slot),"animal"),"modes")).Cast<object>().Select(mode=>"id="+((UnityEngine.Object)Read(mode,"ID")).GetEntityId()+",active="+Read(mode,"Active")+",temporal="+Read(mode,"TemporalActivation")+",abilities=["+string.Join("/",((IEnumerable)Read(mode,"Abilities")).Cast<object>().Select(ability=>Read(ability,"Name")+":"+Read(Read(ability,"Index"),"Value")+":"+Read(ability,"Status")+":"+Read(ability,"Persistent")+":"+Read(Read(ability,"CoolDown"),"Value")+":limits="+System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(Read(ability,"Limits"))))+"]"))));
    }
    void AssertActualRiderReadiness(int slot,string phase)
    {
        var racer=((Array)Read(match,"racers")).GetValue(slot);var dragon=Read(racer,"Dragon");float actual=Mathf.Max(0,(float)Read(dragon,"attackReadyAt")-(float)Read(dragon,"clock"));float actor=(float)Read(dragon,"AttackReadyIn"),shown=(float)Read(racer,"AttackReadyIn");
        Assert.That(actor,Is.EqualTo(actual).Within(.00001f),"The public actor readiness must report its genuine existing grace/cooldown clock.");Assert.That(shown,Is.GreaterThanOrEqualTo(actual-.00001f),"The rider's HUD readiness cannot understate the shared attack authority's actual grace.");
        if(phase!=null)Log("ACTUAL_RIDER_READINESS phase="+phase+" slot="+slot+" sharedAuthoritySeconds="+actual+" riderHudSeconds="+shown+" noTimerMutation=true");
    }
    void RiderActionOnly(int slot)
    {
        if(Source(slot)=="Keyboard"){InputSystem.QueueStateEvent(keyboard,new KeyboardState(((Key[])Read(match,"actionKeys"))[slot]));return;}var state=new GamepadState();if(Source(slot)=="WholeController")state=state.WithButton(GamepadButton.South);else if(Source(slot)=="LeftStick")state.leftTrigger=1;else state.rightTrigger=1;InputSystem.QueueStateEvent((Gamepad)Read(Player(slot),"pad"),state);
    }
    void InspectRiderViewportHud(Camera ui,int width,int height)
    {
        var cameras=(Camera[])Read(match,"cameras");var rally=Read(views,"rally");var owners=(int[])Read(rally,"owners");var panels=(RectTransform[])Read(Read(match,"menus"),"viewPanels");var textType=Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro",true);
        for(int view=0;view<cameras.Length;view++)
        {
            var plate=panels[view].Find("Compact owned viewport score") as RectTransform;Assert.That(plate!=null&&plate.gameObject.activeInHierarchy,Is.True,"The actual compact owned HUD must render in each enabled viewport.");var label=plate.Find("Participant name").GetComponent(textType);Assert.That(Read(label,"text"),Is.EqualTo(Read(Player(owners[view]),"name")));var telemetry=plate.Find("Driving telemetry").GetComponent(textType);string actual=(string)Read(telemetry,"text");Assert.That(actual,Does.Contain("STAMINA").And.Contain("BREATH").And.Contain("MPH"));Assert.That((bool)Read(telemetry,"isTextTruncated"),Is.False,"Actual rendered stamina/Breath telemetry must fit; a hidden second source-string line is not readable HUD acceptance.");var telemetryRect=(RectTransform)((Component)telemetry).transform;var textBounds=(Bounds)Read(telemetry,"textBounds");Assert.That(textBounds.size.y,Is.LessThanOrEqualTo(telemetryRect.rect.height+.5f));Assert.That(textBounds.size.x,Is.LessThanOrEqualTo(telemetryRect.rect.width+.5f));var corners=new Vector3[4];plate.GetWorldCorners(corners);var rect=cameras[view].rect;
            foreach(var corner in corners){var point=RectTransformUtility.WorldToScreenPoint(ui,corner);Assert.That(point.x,Is.InRange(rect.xMin*width-1,rect.xMax*width+1));Assert.That(point.y,Is.InRange(rect.yMin*height-1,rect.yMax*height+1));}
            string expectedPrompt=(string)Call(match,"ViewportContextPrompt",view);var action=plate.Find("Contextual action") as RectTransform;
            if(string.IsNullOrEmpty(expectedPrompt))Assert.That(action==null||!action.gameObject.activeInHierarchy,Is.True,"An empty owner-specific provider must hide the actual contextual Canvas element.");
            else
            {
                Assert.That(action!=null&&action.gameObject.activeInHierarchy,Is.True);var text=action.Find("Assigned action").GetComponent(textType);Assert.That(Read(text,"text"),Is.EqualTo(expectedPrompt));Assert.That((bool)Read(text,"isTextTruncated"),Is.False,"The actual owner-specific control instruction must fit.");
                action.GetWorldCorners(corners);foreach(var corner in corners){var point=RectTransformUtility.WorldToScreenPoint(ui,corner);Assert.That(point.x,Is.InRange(rect.xMin*width-1,rect.xMax*width+1));Assert.That(point.y,Is.InRange(rect.yMin*height-1,rect.yMax*height+1));}
            }
            var racer=((Array)Read(match,"racers")).GetValue(owners[view]);string phase=Read(racer,"Phase").ToString();string eventOwner=(string)Read(match,"announcementParticipantId"),participant=(string)Read(Player(owners[view]),"participantId");
            if(phase=="Mounted"&&!string.Equals(eventOwner,participant,StringComparison.Ordinal))Assert.That(expectedPrompt,Is.Null.Or.Empty,"Another participant's event cannot become this mounted player's control prompt.");
            Log("ACTUAL_VIEWPORT_PROMPT width="+width+" height="+height+" view="+view+" owner="+participant+" eventOwner="+eventOwner+" phase="+phase+" actual="+(expectedPrompt??"NONE")+" canvasVerified=true");
        }
    }
    IEnumerator FocusedRiderPresentation(int humans,string identity)
    {
        Assert.That(riders,Is.True);var racers=(Array)Read(match,"racers");int[] humanSlots=Enumerable.Range(0,4).Where(slot=>Source(slot)!="AI").ToArray();
        foreach(int slot in humanSlots)
        {
            Neutral();yield return null;yield return null;var racer=racers.GetValue(slot);var motor=FlightMotor(slot);var dragon=Read(racer,"Dragon");float deadline=Time.realtimeSinceStartup+12;
            ObserveDrive(slot,"before-natural-attack-readiness-wait");AssertActualRiderReadiness(slot,"before-one-assigned-press");
            // Shared DragonCharacter has a genuine two-second initial grace
            // independent of the rider's later match-local attack cooldown.
            // Read both actual clocks; do not press/retry during that grace or
            // manufacture readiness by writing either gameplay authority.
            bool Ready()=> (float)Read(racer,"AttackReadyIn")<=.05f&&(float)Read(dragon,"clock")>=(float)Read(dragon,"attackReadyAt")&&!(bool)Read(dragon,"warningPending")&&!(bool)Read(motor,"ActionPlaying");
            while(!Ready()&&Time.realtimeSinceStartup<deadline){AssertActualRiderReadiness(slot,null);yield return new WaitForFixedUpdate();}AssertActualRiderReadiness(slot,"after-natural-readiness-wait");ObserveDrive(slot,"before-assigned-breath");Assert.That(Ready(),Is.True,"Wait only for genuine actor warning/cooldown readiness before the single assigned press.");var otherCooldowns=humanSlots.ToDictionary(other=>other,other=>(float)Read(racers.GetValue(other),"AttackReadyIn"));
            RiderActionOnly(slot);yield return null;yield return null;Assert.That(Call(match,"ActionHeld",slot),Is.EqualTo(true));foreach(int other in humanSlots.Where(other=>other!=slot))Assert.That(Call(match,"ActionHeld",other),Is.EqualTo(false));yield return new WaitForFixedUpdate();Neutral();deadline=Time.realtimeSinceStartup+5;
            float nextAttackObservation=Time.realtimeSinceStartup;
            while((Read(motor,"CurrentAction").ToString()!="Breath"||!(bool)Read(motor,"ActionPlaying"))&&Time.realtimeSinceStartup<deadline){if(Time.realtimeSinceStartup>=nextAttackObservation){ObserveDrive(slot,"actual-warning-to-native-mode");nextAttackObservation=Time.realtimeSinceStartup+.12f;}yield return new WaitForFixedUpdate();}ObserveDrive(slot,"after-assigned-breath-wait");Assert.That(Read(motor,"CurrentAction").ToString()=="Breath"&&(bool)Read(motor,"ActionPlaying"),Is.True,"The assigned software action must enter real mapped Breath playback after its genuine warning.");Assert.That((float)Read(racer,"AttackReadyIn"),Is.GreaterThan(1));
            foreach(int other in humanSlots.Where(other=>other!=slot))Assert.That((float)Read(racers.GetValue(other),"AttackReadyIn"),Is.LessThanOrEqualTo(otherCooldowns[other]+.05f),"Assigned action cannot start another human's attack cooldown.");Log("ACTUAL_RIDER_ASSIGNED_BREATH humans="+humans+" slot="+slot+" source="+Source(slot)+" action="+Read(motor,"CurrentAction")+" normalized="+Read(motor,"ActionNormalizedTime")+" NO_TARGET_HIT_OR_LISTENING_CLAIM");
            if(transientFireSize||coldInfernoFixture)yield return ObserveLiveMountedBreath(slot,coldInfernoFixture);
        }
        Neutral();Call(views,"Select",2);yield return null;yield return null;var rally=Read(views,"rally");int count=(int)Read(rally,"Humans");var owners=(int[])Read(rally,"owners");var looks=(Component[])((Array)Read(views,"looks")).Cast<Component>().ToArray();
        for(int view=0;view<count;view++)
        {
            int owner=owners[view];var cabRoots=(Transform[])Read(views,"cabs");int mouseView=Enumerable.Range(0,count).Where(candidate=>Source(owners[candidate])=="Keyboard"&&cabRoots[candidate].gameObject.activeInHierarchy).DefaultIfEmpty(-1).First();if(Source(owner)!="WholeController"&&!(Source(owner)=="Keyboard"&&view==mouseView)){if(Source(owner)!="AI")Log("RIDER_LOOK_LIMIT owner="+owner+" source="+Source(owner)+" view="+view+" shared-stick_or_secondary-keyboard_singlemouse_restriction");continue;}
            Neutral();Call(views,"Select",2);yield return null;yield return null;var initialHeading=(Vector3)Read(racers.GetValue(owner),"FlightHeading");float end=Time.time+.35f;
            while(Time.time<end){if(Source(owner)=="WholeController")InputSystem.QueueStateEvent((Gamepad)Read(Player(owner),"pad"),new GamepadState{rightStick=new Vector2(.8f,.3f)});else InputSystem.QueueStateEvent(riderMouse,new MouseState{buttons=2,delta=new Vector2(14,3)});yield return null;}
            Assert.That(Mathf.Abs((float)Read(looks[view],"Yaw")),Is.GreaterThan(2),"Compatible actual assigned cab-look input must rotate its own Cinemachine axes.");for(int other=0;other<count;other++)if(other!=view)Assert.That(Mathf.Abs((float)Read(looks[other],"Yaw")),Is.LessThan(.01f));foreach(int other in humanSlots)Assert.That(((Vector2)Call(match,"ReadDrive",other)).sqrMagnitude,Is.LessThan(.0001f),"Free look cannot borrow drive input from another crew.");Assert.That(Vector3.Angle(initialHeading,(Vector3)Read(racers.GetValue(owner),"FlightHeading")),Is.LessThan(.01f));Log("ACTUAL_RIDER_CAB_LOOK owner="+owner+" view="+view+" source="+Source(owner)+" yaw="+Read(looks[view],"Yaw")+" pitch="+Read(looks[view],"Pitch"));
            Neutral();yield return null;yield return null;
            if(Source(owner)=="WholeController")InputSystem.QueueStateEvent((Gamepad)Read(Player(owner),"pad"),new GamepadState().WithButton(GamepadButton.RightStick));else InputSystem.QueueStateEvent(riderMouse,new MouseState{buttons=4});
            yield return null;yield return null;Neutral();float recenterDeadline=Time.realtimeSinceStartup+3;
            while((Mathf.Abs((float)Read(looks[view],"Yaw"))>=.1f||Mathf.Abs((float)Read(looks[view],"Pitch"))>=.1f)&&Time.realtimeSinceStartup<recenterDeadline)yield return null;
            Assert.That(Mathf.Abs((float)Read(looks[view],"Yaw")),Is.LessThan(.1f));Assert.That(Mathf.Abs((float)Read(looks[view],"Pitch")),Is.LessThan(.1f));Log("ACTUAL_RIDER_ASSIGNED_RECENTER owner="+owner+" view="+view+" source="+Source(owner)+" yaw="+Read(looks[view],"Yaw")+" pitch="+Read(looks[view],"Pitch")+" existingCinemachineRecentering=true noManualPoseOrReset=true");
        }
        Neutral();yield return null;yield return null;Call(match,"Pause",humanSlots[0],false);Call(Read(match,"menus"),"Show",Enum.Parse(Runtime("LWS.TruckSports.SportsEventScreen"),"Play"));
        foreach(bool showAI in new[]{false,true})foreach(int cameraMode in new[]{1,2})
        {
            Runtime("LWS.TruckSports.SportsCameraOptions").GetMethod("Set").Invoke(null,new object[]{showAI});Call(views,"Select",cameraMode);yield return new WaitForSecondsRealtime(.16f);AssertLayout(humans,showAI,identity);
            foreach(int height in new[]{720,1080})
            {
                int width=height==720?1280:1920;string file=Path.Combine(output,riderCaptureFolder,"humans"+humans+"-view"+cameraMode+"-ai"+showAI+"-"+width+"x"+height+".png");Action<Camera> inspect=ui=>InspectRiderViewportHud(ui,width,height);IDisposable sightline=null;
                try{if((humans==2||coldInfernoFixture)&&cameraMode==2&&!showAI&&(height==720||transientSourceEnvelopeFlap>0||transientFireSize||coldInfernoFixture))sightline=(IDisposable)Activator.CreateInstance(Type.GetType("DragonRiderCabSightlineObservation, LWS.TruckDragonRiders.NativeTests",true),match,Path.ChangeExtension(file,".actual-skin-sightlines.json"));Runtime("LWS.TruckSports.SportsControlledCapture").GetMethods().Single(method=>method.Name=="Save"&&method.GetParameters().Length==5).Invoke(null,new object[]{match,width,height,file,inspect});}finally{sightline?.Dispose();}
                if(transientFireSize&&humans==2&&cameraMode==2&&!showAI)ValidateTransientFireSizeCab(file,height);if(coldInfernoFixture&&cameraMode==2&&!showAI)ValidatePristineInfernoCab(file,height);
                if(transientCabCandidate&&humans==2&&cameraMode==2&&!showAI&&(height==720||transientSourceEnvelopeFlap>0))
                {
                    var actual=JsonUtility.FromJson<CabTransientViews>(File.ReadAllText(Path.ChangeExtension(file,".actual-skin-sightlines.json")));foreach(var candidateView in actual.views.Where(value=>transientSourceEnvelopeFlap>0||value.owner==0)){Assert.That(candidateView.exactOwnedStatePreserved,Is.True);Assert.That(candidateView.actualBaseClips,Does.Contain("TRANSIENT_CAB_"),"The original native Animator must actually play the authored source-curve variant after ordinary takeoff/resume.");if(transientSourceEnvelopeFlap>0){Assert.That(candidateView.actualWheelDeckSeating,Is.Not.Empty,"Actual same-render wheel/wood contact must be observed using existing seating geometry authority.");var seating=JsonUtility.FromJson<CabWheelDeckSeating>(candidateView.actualWheelDeckSeating);Assert.That(seating.context,Is.EqualTo("ACTUAL_NATIVE_PLAYMODE_ORIGINAL_PARTICIPANT"));Assert.That(seating.wheels.Length,Is.EqualTo(4));foreach(var wheel in seating.wheels){Assert.That(wheel.actualVisualFound&&wheel.deckFaceFound,Is.True,wheel.name+" must have a real composed wheel and visible vendor timber face.");Assert.That(Mathf.Abs(wheel.actualGap),Is.LessThanOrEqualTo(.03f),wheel.name+" original wheel must meet its actual wood face within existing3cm seating guard.");Log("ACTUAL_NATIVE_WHEEL_TIMBER_CONTACT owner="+candidateView.owner+" height="+height+" wheel="+wheel.name+" actualGap="+wheel.actualGap.ToString("R")+" source="+wheel.deckFaceSource);}}var geometry=candidateView.pureConnectedNeckCandidates[0];Assert.That(geometry.actualTrueBakeTriangleBoxIntersections,Is.Zero,"Actual native baked full skin must clear both complete original NWH boxes.");Assert.That(geometry.triangleBoxIntersections,Is.Zero);var cab=geometry.sameFrameCabRays.Single(value=>value.name=="ACTUAL_CAMERA_ROUND_TRIP_CONTROL");var center=cab.centralWindscreenRays.Single(ray=>(ray.viewport-new Vector2(.5f,.5f)).sqrMagnitude<.000001f);Assert.That(center.ownDragon&&center.headNeckWeight>=.5f,Is.False,"Actual forward cab center must be clear of the owner head.");Assert.That(cab.ownHeadNeckDominantHits,Is.LessThanOrEqualTo(5),"At least80percent of the central windscreen grid must clear the owner head; native images still require separate visual review.");Log("ACTUAL_TRANSIENT_NATIVE_CLIP_AND_FULL_SKIN owner="+candidateView.owner+" sourceRootFlap="+transientSourceEnvelopeFlap+" height="+height+" headHits="+cab.ownHeadNeckDominantHits+"/25 centerClear=true actualTrueBakeSAT=0 sourceAndOtherBodyRulesUnchanged=true NOT_PRODUCTION_OR_HUMAN_ACCEPTANCE");}
                }
                Assert.That(new FileInfo(file).Length,Is.GreaterThan(2000));Log("ACTUAL_RIDER_VIEWPORT_CAPTURE "+file);
            }
        }
        string modeConfiguration=ActualRiderModeConfiguration();Neutral();Call(match,"SetupScreen");yield return null;Assert.That(Identity(),Is.EqualTo(identity));Assert.That(Call(match,"StartMatch"),Is.EqualTo(true));float countdownDeadline=Time.realtimeSinceStartup+35;while((float)Read(match,"Countdown")>0&&Time.realtimeSinceStartup<countdownDeadline)yield return null;Assert.That((float)Read(match,"Countdown"),Is.Zero);Neutral();yield return null;yield return null;AssertLayout(humans,true,identity);Assert.That(ActualRiderModeConfiguration(),Is.EqualTo(modeConfiguration),"Natural rematch mode reset must preserve actual authored active/temporal/ability configuration and identities.");ObserveActualPrepareModeResets("inner-same-configuration-rematch");foreach(int slot in humanSlots){Assert.That(((Vector2)Call(match,"ReadDrive",slot)).sqrMagnitude,Is.LessThan(.0001f));Assert.That(Call(match,"ActionHeld",slot),Is.EqualTo(false));}Log("ACTUAL_RIDER_SAME_CONFIGURATION_REMATCH humans="+humans+" identityExact=true neutralInputs=true noInjectedFlightOrGates=true");
    }
    void ObserveTransientFireSize()
    {
        if(!transientFireSize)return;
        var author=Type.GetType("LWS.TruckDragonRiders.Editor.DragonRiderSaddleAuthoring, Assembly-CSharp-Editor",true);
        Log("ACTUAL_TRANSIENT_FIRE_SIZE_LEASE "+author.GetMethod("ObserveTransientFireMountedSize").Invoke(null,new object[]{transientFireSizeLease}));
    }
    IEnumerator ObserveLiveMountedBreath(int owner,bool cold=false)
    {
        if(transientFireGeometryObserver==null)transientFireGeometryObserver=new GameObject("Existing final-attachment observer for bounded Fire Breath geometry").AddComponent(Type.GetType("DragonRiderSaddleFrameObserver, LWS.TruckDragonRiders.NativeTests",true));
        var author=Type.GetType("LWS.TruckDragonRiders.Editor.DragonRiderSaddleAuthoring, Assembly-CSharp-Editor",true);var motor=FlightMotor(owner);
        foreach(float threshold in new[]{0f,.25f,.45f,.75f})
        {
            bool sampled=false;Exception failure=null;SizedFireLiveGeometry actual=null;float requested=threshold;
            Action observe=()=>
            {
                if(sampled||failure!=null)return;
                try
                {
                    Assert.That(Read(motor,"CurrentAction").ToString(),Is.EqualTo("Breath"));Assert.That((bool)Read(motor,"ActionPlaying"),Is.True,"The real assigned Breath must remain active until this bounded native observation.");
                    if((float)Read(motor,"ActionNormalizedTime")<requested)return;
                    string json=(string)author.GetMethod(cold?"ObservePristineInfernoMotor17Geometry":"ObserveTransientFireMountedGeometry").Invoke(null,new object[]{cold?coldInfernoFixtureLease:transientFireSizeLease,owner,"actual-assigned-Breath-normalized-at-least-"+requested});actual=JsonUtility.FromJson<SizedFireLiveGeometry>(json);Log("ACTUAL_FINAL_ATTACHMENT_FIRE_BREATH_GEOMETRY "+json);sampled=true;
                }
                catch(Exception error){failure=error;}
            };
            Set(transientFireGeometryObserver,"Observe",observe);float deadline=Time.realtimeSinceStartup+5;
            while(!sampled&&failure==null&&Time.realtimeSinceStartup<deadline)yield return null;
            Set(transientFireGeometryObserver,"Observe",null);if(failure!=null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
            Assert.That(sampled,Is.True);Assert.That(actual.owner,Is.EqualTo(owner));Assert.That(actual.action,Is.EqualTo("Breath"));Assert.That(actual.actualNormalized,Is.GreaterThanOrEqualTo(threshold));Assert.That(actual.physicallyMounted,Is.True);Assert.That(actual.finalAttachmentFrame,Is.EqualTo(actual.frame));Assert.That(actual.triangles,Is.GreaterThan(2500));Assert.That(actual.boxes,Is.EqualTo(2));Assert.That(actual.satPairs,Is.Zero);Assert.That(actual.wheels,Is.EqualTo(4));Assert.That(actual.allActualWheelsFound,Is.True);Assert.That(actual.maximumActualWheelGap,Is.LessThanOrEqualTo(.03f));
        }
        Log((cold?"ACTUAL_PRISTINE_INFERNO_BREATH_NATIVE_GEOMETRY owner=":"ACTUAL_FIRE_BREATH_NATIVE_GEOMETRY owner=")+owner+" fourRealPlaybackPosesIncludingContactPhaseMeasured=true NO_EXTERNAL_TARGET_DAMAGE_OR_AUDIO_LISTENING_ACCEPTANCE");
    }
    void ValidateTransientFireSizeCab(string file,int height)
    {
        ObserveTransientFireSize();var actual=JsonUtility.FromJson<CabTransientViews>(File.ReadAllText(Path.ChangeExtension(file,".actual-skin-sightlines.json")));
        Assert.That(actual.views.Length,Is.EqualTo(2));
        foreach(var view in actual.views)
        {
            Assert.That(view.exactOwnedStatePreserved,Is.True);Assert.That(Read(Read(((Array)Read(match,"racers")).GetValue(view.owner),"Species"),"id"),Is.EqualTo("dragon-fire"));
            if(transientFireNormalFamily)Assert.That(view.actualBaseClips,Does.Contain("TRANSIENT_CAB_"),"The real native Animator must play the positively proven owned normal-flight family, not the rejected original raised-head controller.");
            var seating=JsonUtility.FromJson<CabWheelDeckSeating>(view.actualWheelDeckSeating);Assert.That(seating.context,Is.EqualTo("ACTUAL_NATIVE_PLAYMODE_ORIGINAL_PARTICIPANT"));Assert.That(seating.wheels.Length,Is.EqualTo(4));
            foreach(var wheel in seating.wheels){Assert.That(wheel.actualVisualFound&&wheel.deckFaceFound,Is.True);Assert.That(Mathf.Abs(wheel.actualGap),Is.LessThanOrEqualTo(.03f));}
            var geometry=view.pureConnectedNeckCandidates[0];Assert.That(geometry.completeSkinTriangles,Is.GreaterThan(2500));Assert.That(geometry.actualOriginalBoxes,Is.EqualTo(2));Assert.That(geometry.actualTrueBakeTriangleBoxIntersections,Is.Zero);Assert.That(geometry.triangleBoxIntersections,Is.Zero);
            var cab=geometry.sameFrameCabRays.Single(value=>value.name=="ACTUAL_CAMERA_ROUND_TRIP_CONTROL");var center=cab.centralWindscreenRays.Single(ray=>(ray.viewport-new Vector2(.5f,.5f)).sqrMagnitude<.000001f);
            Assert.That(center.ownDragon&&center.headNeckWeight>=.5f,Is.False);Assert.That(cab.ownHeadNeckDominantHits,Is.LessThanOrEqualTo(5));
            Log((transientFireHeadLever?"ACTUAL_TRANSIENT_FIRE_PROVEN_HEAD_LEVER_60_MINUS15_167_CAB":transientFireNormalFamily?"ACTUAL_TRANSIENT_FIRE_NORMAL_FAMILY_STRICT_TORSO_1_POINT67_CAB":"ACTUAL_TRANSIENT_FIRE_1_POINT87_CAB")+" owner="+view.owner+" height="+height+" ownHeadGridHits="+cab.ownHeadNeckDominantHits+" actualFullSkinSAT=0 originalBoxes=2 actualFourWheelTimberGapWithin3cm=true originalSavedControllerUnchanged="+(!transientFireNormalFamily)+" NO_COURSE_WINDOWS_OR_HUMAN_ACCEPTANCE");
        }
    }
    IEnumerator ObserveSingleBusyAssignedBreath(int slot,string phase)
    {
        var motor=FlightMotor(slot);var racer=((Array)Read(match,"racers")).GetValue(slot);var dragon=Read(racer,"Dragon");var animal=Read(motor,"animal");var animator=(Animator)Read(motor,"animator");Neutral();yield return null;yield return null;float deadline=Time.realtimeSinceStartup+12;
        bool Ready()=> (float)Read(racer,"AttackReadyIn")<=.05f&&(float)Read(dragon,"clock")>=(float)Read(dragon,"attackReadyAt")&&!(bool)Read(dragon,"warningPending")&&!(bool)Read(motor,"ActionPlaying");
        while(!Ready()&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();Assert.That(Ready(),Is.True);AssertActualRiderReadiness(slot,phase+"-ready");
        var mapping=((Array)Read(Read(motor,"Species"),"animations")).Cast<object>().Single(value=>value!=null&&Read(value,"action").ToString()=="Breath");var actualMapped=(AnimationClip)Call(motor,"ResolveMappedClip",Read(mapping,"clip"));Assert.That(actualMapped,Is.Not.Null);
        RiderActionOnly(slot);yield return null;yield return null;Assert.That(Call(match,"ActionHeld",slot),Is.EqualTo(true));yield return new WaitForFixedUpdate();Neutral();deadline=Time.realtimeSinceStartup+6;
        bool ActuallyPlaying()=> Read(motor,"CurrentAction").ToString()=="Breath"&&(bool)Read(motor,"ActionPlaying")&&(bool)Read(animal,"IsPlayingMode")&&animator.GetCurrentAnimatorClipInfo(1).Concat(animator.GetNextAnimatorClipInfo(1)).Any(value=>value.clip==actualMapped&&value.weight>0);
        while(!ActuallyPlaying()&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();ObserveDrive(slot,phase+"-actual-busy-mapped-clip");Assert.That(ActuallyPlaying(),Is.True,"One real assigned press must visibly play its actual configured Breath through the native mode before interruption.");var nativeMode=Read(animal,"ActiveMode");Assert.That(Read(nativeMode,"PlayingMode"),Is.EqualTo(true));Log("ACTUAL_BUSY_BREATH phase="+phase+" slot="+slot+" mappedClip="+actualMapped.name+" mode="+Read(nativeMode,"Name")+" nativePlaying=true noForcedModeOrState=true");
    }
    IEnumerator BusyRiderPublicRematch(int slot,string identity)
    {
        yield return ObserveSingleBusyAssignedBreath(slot,"first-round");var motor=FlightMotor(slot);var animal=Read(motor,"animal");string configuration=ActualRiderModeConfiguration();
        // Deliberately interrupt a REAL busy animation using only the existing
        // public menu/new-round flow. Do not wait for its native completion.
        Call(match,"SetupScreen");Assert.That(Call(match,"StartMatch"),Is.EqualTo(true));ObserveActualPrepareModeResets("deterministic-public-busy-rematch");
        Assert.That((int)Read(motor,"LastPreparePlayingModesReset"),Is.GreaterThan(0),"This dedicated lifecycle case must actually clean a genuinely playing mode at its real Prepare boundary.");Assert.That(Read(animal,"IsPlayingMode"),Is.EqualTo(false));Assert.That((bool)Read(motor,"ActionPlaying"),Is.False);Assert.That((string)Read(motor,"LastPrepareModeResetDiagnosis"),Does.Contain("nativePointerCleared=True").And.Contain("sameModeId=True").And.Contain("sameAbilityList=True"));Assert.That(ActualRiderModeConfiguration(),Is.EqualTo(configuration));Assert.That(Identity(),Is.EqualTo(identity));
        float deadline=Time.realtimeSinceStartup+35;while((float)Read(match,"Countdown")>0&&Time.realtimeSinceStartup<deadline)yield return null;Assert.That((float)Read(match,"Countdown"),Is.Zero);Neutral();yield return null;yield return null;Assert.That(Identity(),Is.EqualTo(identity));
        yield return ObserveSingleBusyAssignedBreath(slot,"new-round");Log("ACTUAL_PUBLIC_BUSY_REMATCH slot="+slot+" playingCleanup="+Read(motor,"LastPreparePlayingModesReset")+" staleDiagnostic="+Read(motor,"LastPrepareStalePlayingModesReset")+" exactConfiguration=true actualNextAssignedBreath=true noPrivateFlagTimerPoseOrActivationInjection=true");
    }
    static string ActualOriginalSaddleBindingSignature(Component actor)
    {
        var frame=actor.GetComponents<Component>().Single(value=>value.GetType().Name=="DragonRiderSaddle");var skin=(SkinnedMeshRenderer)Read(frame,"surfaceRenderer");return JsonUtility.ToJson(frame)+"|actualSharedMesh="+skin.sharedMesh.GetEntityId();
    }
    void InstallTransientInfernoCabProfile()
    {
        var all=((Array)Read(match,"racers")).Cast<object>().SelectMany(racer=>((Array)Read(racer,"choices")).Cast<Component>()).ToArray();var author=Type.GetType("LWS.TruckDragonRiders.Editor.DragonRiderSaddleAuthoring, Assembly-CSharp-Editor",true);
        foreach(string speciesId in transientSourceEnvelopeFlap>0?new[]{"dragon-inferno","dragon-fire"}:new[]{"dragon-inferno"})
        {
            var actors=all.Where(actor=>(string)Read(Read(actor,"motor"),"configuredSpeciesId")==speciesId).ToArray();Assert.That(actors.Length,Is.EqualTo(4));var first=actors[0];var prior=((Animator)Read(Read(first,"motor"),"animator")).runtimeAnimatorController;var bindingSignatures=actors.ToDictionary(actor=>actor,actor=>ActualOriginalSaddleBindingSignature(actor));foreach(var actor in actors)Assert.That(((Animator)Read(Read(actor,"motor"),"animator")).runtimeAnimatorController,Is.SameAs(prior),"Preserve alternate designer controllers rather than overwriting unknown configurations.");
            float speciesFlap=mixedRiderEnvelope&&speciesId=="dragon-fire"?.5f:transientSourceEnvelopeFlap;var objects=(UnityEngine.Object[])(safeMountedEnvelope?author.GetMethod("CreateTransientSafeMountedController").Invoke(null,new object[]{first,speciesId}):transientSourceEnvelopeFlap>0?author.GetMethod("CreateTransientCabFlightEnvelope").Invoke(null,new object[]{first,speciesId,speciesFlap}):author.GetMethod("CreateTransientCabFlightController").Invoke(null,new object[]{first,speciesId}));transientCabObjects.AddRange(objects);var controller=objects.OfType<AnimatorOverrideController>().Single();foreach(var actor in actors)
            {
                ((Animator)Read(Read(actor,"motor"),"animator")).runtimeAnimatorController=controller;Assert.That(ActualOriginalSaddleBindingSignature(actor),Is.EqualTo(bindingSignatures[actor]),"Source-curve envelope creation and installation must preserve every original actual skin binding; only the existing saddle owns mounting.");
                if(transientSourceEnvelopeFlap>0){var saddle=actor.GetComponents<Component>().Single(value=>value.GetType().Name=="DragonRiderSaddle");var anchors=(Array)Read(saddle,"surfacePatchAnchors");Assert.That(anchors.Length,Is.EqualTo(5));Log("ORIGINAL_CENTERED_SKIN_BINDINGS_UNCHANGED actor="+actor.name+" anchors="+string.Join(";",anchors.Cast<object>().Select(point=>Read(point,"a")+"/"+Read(point,"b")+"/"+Read(point,"c")+"="+Read(point,"barycentric")))+" ordinaryPublicStartUsesSingleSaddleAuthority=true noPatchPoseColliderOrOffsetInjection=true");}
            }
            Log("TRANSIENT_AUTHORED_PROFILE_INSTALLED species="+speciesId+" sourceRootFlap="+speciesFlap+" actualAOC="+controller.name+" objects="+objects.Length+" ordinaryNextPublicStart=true noSavedAssetOrPoseWriter=true");
        }
    }
    IEnumerator Exercise(string scene,string type,bool interruptRealLimiter=false,bool focusedRiderPresentation=false,bool busyRiderRematch=false,int? onlyRiderHumanCount=null,bool transientCabProfile=false,float? sourceEnvelopeFlap=null,bool mixedSpeciesEnvelope=false,bool sourceSafeMounted=false,bool sizedFire=false,bool sizedFireFamily=false,bool sizedFireHeadLever=false,bool pristineInferno=false)
    {
        coldInfernoFixture=pristineInferno;
        transientFireSize=sizedFire||sizedFireFamily||sizedFireHeadLever;transientFireNormalFamily=sizedFireFamily||sizedFireHeadLever;transientFireHeadLever=sizedFireHeadLever;transientSourceEnvelopeFlap=sourceEnvelopeFlap??0;mixedRiderEnvelope=mixedSpeciesEnvelope;safeMountedEnvelope=sourceSafeMounted;mode=scene;reportName=scene+(pristineInferno?"-PristineMotor17Solo":sizedFireHeadLever?"-TransientFireHeadLever60Minus15StrictTorso167":sizedFireFamily?"-TransientFireNormalFamily167StrictTorso":sizedFire?"-TransientFire187OriginalController":transientCabProfile?"-TransientAuthoredCabProfile"+(transientSourceEnvelopeFlap>0?"-Envelope"+(mixedRiderEnvelope?"MixedQuarterHalf":transientSourceEnvelopeFlap.ToString()):""):onlyRiderHumanCount.HasValue?"-TwoHumanCabGeometryScreen":focusedRiderPresentation?"-FocusedRiderPresentation":busyRiderRematch?"-BusyBreathRematch":"");riders=scene=="TruckDragonRiders";report.Clear();riderCaptureFolder="RidersFocused";transientCabCandidate=false;Assert.That(Application.isPlaying,Is.True);if(onlyRiderHumanCount.HasValue)Assert.That(riders&&focusedRiderPresentation&&onlyRiderHumanCount.Value==2,Is.True,"The bounded geometry diagnostic is only the established real two-human cab configuration, never a reduced broad acceptance claim.");
        string path="Assets/"+scene+"/Scenes/"+scene+".unity";Assert.That(File.Exists(path),Is.True,"Explicitly author and accept the shared world and real saved scene first; this fixture does not generate venues.");
        var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");output=Path.GetFullPath(Path.Combine(at>=0&&at+1<args.Length?args[at+1]:Application.persistentDataPath,"InputOwnership"));Directory.CreateDirectory(output);
        preferences=(IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"DragonInput"+scene});Runtime("LWS.TruckSports.SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);
        isolation=new GameObject("Dragon saved-scene software input isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("LWS.TruckSports.SportsProbeInputIsolation"));
        original=InputSystem.settings;temporary=UnityEngine.Object.Instantiate(original);temporary.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;temporary.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=temporary;
        keyboard=InputSystem.AddDevice<Keyboard>();InputSystem.EnableDevice(keyboard);keyboard.MakeCurrent();for(int i=0;i<4;i++){pads[i]=InputSystem.AddDevice<Gamepad>();InputSystem.EnableDevice(pads[i]);}
        if(focusedRiderPresentation){riderMouse=InputSystem.AddDevice<Mouse>();InputSystem.EnableDevice(riderMouse);riderMouse.MakeCurrent();}
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(path,new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
        match=(Component)UnityEngine.Object.FindAnyObjectByType(Runtime(type));Assert.That(match,Is.Not.Null);players=(Array)Read(Read(match,"setup"),"players");trucks=(Array)Read(match,"trucks");views=Read(match,"drivingViews");
        if(transientFireSize)
        {
            Assert.That(riders&&focusedRiderPresentation&&!transientCabProfile,Is.True);int proofAt=Array.IndexOf(args,"-tsRiderMountedSizeEvidence");Assert.That(proofAt>=0&&proofAt+1<args.Length,Is.True,"Explicit positive native geometry receipt required.");
            var selected=(string[])Read(match,"selectedSpeciesIds");for(int slot=0;slot<selected.Length;slot++)selected[slot]="dragon-fire";
            var author=Type.GetType("LWS.TruckDragonRiders.Editor.DragonRiderSaddleAuthoring, Assembly-CSharp-Editor",true);transientFireSizeLease=(IDisposable)author.GetMethod(transientFireHeadLever?"BeginTransientFireHeadLeverSizeFromNativeEvidence":transientFireNormalFamily?"BeginTransientFireNormalFamilySizeFromNativeEvidence":"BeginTransientFireMountedSizeFromNativeEvidence").Invoke(null,new object[]{match,args[proofAt+1]});ObserveTransientFireSize();
        }
        if(coldInfernoFixture)BeginPristineInfernoFixture(args);
        Log("SOFTWARE_INPUT_ONLY savedScene="+path+" timeScale=1; actual InputSystem virtual devices, original session sampling, real NWH/vendor movement; no physical hardware, Windows, listening, full-match or human acceptance.");
        bool observedInterruptedLimiter=false;observedStaleNativeModeResets=0;observedPrepareModeReceipts.Clear();
        int actualConfigurationPass=0;foreach(int humans in coldInfernoFixture?new[]{1,1}:transientCabProfile||transientFireSize?new[]{2,2}:onlyRiderHumanCount.HasValue?new[]{onlyRiderHumanCount.Value}:new[]{1,2,3,4})
        {
            transientCabCandidate=transientCabProfile&&actualConfigurationPass>0;riderCaptureFolder=transientCabProfile?(transientSourceEnvelopeFlap>0?Path.Combine(safeMountedEnvelope?"RidersMountedSafe":mixedRiderEnvelope?"RidersMixedEnvelope":"RidersEnvelope"+(int)(transientSourceEnvelopeFlap*100),transientCabCandidate?"Candidate":"Baseline"):Path.Combine("RidersTransientCab",transientCabCandidate?"ActualAuthoredCandidate":"ActualOriginalBaseline")):"RidersFocused";if(transientCabCandidate)InstallTransientInfernoCabProfile();actualConfigurationPass++;
            if(coldInfernoFixture)riderCaptureFolder=Path.Combine("RidersPristineMotor17Solo","public-start-"+actualConfigurationPass);if(transientFireSize)riderCaptureFolder=Path.Combine(transientFireHeadLever?"RidersFireHeadLever60Minus15StrictTorso167":transientFireNormalFamily?"RidersFireNormalFamily167StrictTorso":"RidersFireSize187","public-start-"+actualConfigurationPass);
            Neutral();yield return null;Configure(humans);if(busyRiderRematch){AssignSource(2,"AI");AssignSource(1,"RightStick",1);Assert.That(Call(match,"Assign",1,pads[1]),Is.EqualTo(true));Assert.That(Read(match,"DevicesReady"),Is.EqualTo(true));}Runtime("LWS.TruckSports.SportsCameraOptions").GetMethod("Set").Invoke(null,new object[]{false});Call(views,"Select",1);
            if(!riders)for(int slot=0;slot<4;slot++){var engine=Read(Read(Read(Truck(slot),"tractorMotor"),"vehicle"),"powertrain");engine=Read(engine,"engine");Log("ACTUAL_NWH_BEFORE_NEW_ATTEMPT humans="+humans+" slot="+slot+" revLimiterActive="+Diagnostic(engine,"revLimiterActive")+" engineRPM="+Diagnostic(engine,"OutputRPM"));}
            Assert.That(Call(match,"StartMatch"),Is.EqualTo(true));
            float deadline=Time.realtimeSinceStartup+35;while((float)Read(match,riders?"Countdown":"Intro")>0&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That((float)Read(match,riders?"Countdown":"Intro"),Is.Zero,"Actual initial support/countdown must complete.");yield return null;yield return null;
            ObserveTransientFireSize();if(coldInfernoFixture)ObservePristineInfernoFixture();
            if(riders&&focusedRiderPresentation){ObserveActualPrepareModeResets("outer-layout-start");foreach(int slot in Enumerable.Range(0,4).Where(slot=>Source(slot)!="AI"))AssertActualRiderReadiness(slot,"initial-native-grace-after-countdown");}
            string identity=Identity();var ids=Enumerable.Range(0,4).Select(slot=>(string)Read(Player(slot),"participantId")).ToArray();Assert.That(ids.All(value=>!string.IsNullOrWhiteSpace(value)),Is.True);Assert.That(ids.Distinct().Count(),Is.EqualTo(4));var initial=Enumerable.Range(0,InputParticipantCount).Select(Position).ToArray();var initialHoist=Enumerable.Range(0,InputParticipantCount).Select(slot=>ObjectiveHoist(slot)!=null?ObjectiveHoist(slot).position:Vector3.zero).ToArray();AssertLayout(humans,false,identity);
            foreach(int slot in Enumerable.Range(0,4).Where(slot=>Source(slot)!="AI"))
            {
                Neutral();yield return null;yield return null;Vector3 start=Position(slot);Vector3 heading=riders?(Vector3)Read(((Array)Read(match,"racers")).GetValue(slot),"heading"):Vector3.zero;
                if(!riders){var engine=Read(Read(Read(Read(Truck(slot),"tractorMotor"),"vehicle"),"powertrain"),"engine");ObserveDrive(slot,"ready-after-countdown");Assert.That(Read(engine,"revLimiterActive"),Is.EqualTo(false),"An idle human's previous-attempt cutoff must not remain active after the held countdown.");}
                PulseDrive(slot);yield return null;yield return null;AssertExclusiveDrive(slot);ObserveDrive(slot,"held-start");float next=Time.time+.1f,end=Time.time+1.1f;while(Time.time<end){Assert.That(Read(match,"Paused"),Is.EqualTo(false));if(Time.time>=next){ObserveDrive(slot,"held-fixed-step");next=Time.time+.1f;}yield return new WaitForFixedUpdate();}ObserveDrive(slot,"held-end");
                AssertExclusiveDrive(slot);float distance=Vector3.Distance(Position(slot),start);Assert.That(distance,Is.GreaterThan(.15f),"Real assigned input must move the actual vendor-driven participant.");
                if(riders){var racer=((Array)Read(match,"racers")).GetValue(slot);Assert.That(Vector3.Angle(heading,(Vector3)Read(racer,"heading")),Is.GreaterThan(.5f));yield return AwaitActualRiderCoreFlight(slot);Assert.That(Read(FlightMotor(slot),"IsFlying"),Is.EqualTo(true));if(coldInfernoFixture)yield return MeasurePristineInfernoWorldSpeed(slot);}
                else{var drive=(Vector2)Read(Truck(slot),"command");Assert.That(drive.x,Is.GreaterThan(.1f));Assert.That(drive.y,Is.GreaterThan(.1f));Assert.That(Read(Truck(slot),"Powered"),Is.EqualTo(true));}
                Log("ACTUAL_SOFTWARE_OWNER slot="+slot+" source="+Source(slot)+" pad="+((Gamepad)Read(Player(slot),"pad"))?.deviceId+" displacement="+distance+" position="+Position(slot));
            }
            Neutral();yield return null;foreach(int ai in Enumerable.Range(0,InputParticipantCount).Where(slot=>Source(slot)=="AI"))
            {
                float travel=Vector3.Distance(Position(ai),initial[ai]);var hoist=ObjectiveHoist(ai);float hoistTravel=hoist!=null?Vector3.Distance(hoist.position,initialHoist[ai]):0;
                // A Heist crew must stop its real NWH truck to operate the
                // physical hoist. Requiring chassis travel during that valid
                // pickup job incorrectly rejected V151's mixed human setup.
                // Actual hoist-body travel is an alternate physical witness;
                // a mere AI flag, timer, or changed camera is not sufficient.
                Log("ACTUAL_HIDDEN_AI slot="+ai+" chassisTravel="+travel+" originalHoistTravel="+hoistTravel+" powered="+Read(Truck(ai),"Powered"));
                if(!riders){ObserveDrive(ai,"hidden-ai-objective");Log("ACTUAL_HIDDEN_AI_OBJECTIVE slot="+ai+" goal="+Call(match,"DrivingGoal",ai)+" holding="+Call(match,"ShouldHoldPosition",ai));}
                Assert.That(travel>.05f||hoist!=null&&hoistTravel>.05f,Is.True,"Hidden AI must drive or perform its genuine physical hoist job.");if(!riders){Assert.That(Read(Truck(ai),"Powered"),Is.EqualTo(true));Assert.That(((IList)Read(Call(match,"NavigationRoute",ai),"points")).Count,Is.GreaterThan(1),"AI still uses its actual shared route.");}
            }
            if(riders&&busyRiderRematch){yield return BusyRiderPublicRematch(1,identity);Call(match,"SetupScreen");yield return null;break;}
            if(riders&&focusedRiderPresentation)yield return FocusedRiderPresentation(humans,identity);
            ObserveTransientFireSize();
            yield return CameraOwnership(humans,identity);Assert.That(Identity(),Is.EqualTo(identity));
            if(interruptRealLimiter&&humans==1)
            {
                // Reproduce the real lifecycle defect without setting a flag,
                // RPM, command or body pose: the assigned keyboard owner drives
                // the actual NWH engine into its normal rev-limiter cutoff.
                int owner=Enumerable.Range(0,4).Single(slot=>Source(slot)=="Keyboard");
                var engine=Read(Read(Read(Read(Truck(owner),"tractorMotor"),"vehicle"),"powertrain"),"engine");
                Neutral();yield return null;InputSystem.QueueStateEvent(keyboard,new KeyboardState((Key)Read(Player(owner),"forward")));
                float limiterDeadline=Time.realtimeSinceStartup+8;
                while(!(bool)Read(engine,"revLimiterActive")&&Time.realtimeSinceStartup<limiterDeadline)yield return new WaitForFixedUpdate();
                Assert.That(Read(engine,"revLimiterActive"),Is.EqualTo(true),"Actual assigned driving must reach the vendor's real cutoff; no synthetic engine state is permitted.");
                ObserveDrive(owner,"real-cutoff-before-disable");
                Call(match,"SetupScreen");
                Assert.That(Read(engine,"revLimiterActive"),Is.EqualTo(true),"Record the original vendor transient left behind when disabling cancels its coroutine.");
                Log("ACTUAL_INTERRUPTED_VENDOR_CUTOFF owner="+owner+" flagAfterDisable="+Read(engine,"revLimiterActive")+" engineRPM="+Read(engine,"OutputRPM"));
                observedInterruptedLimiter=true;Neutral();
            }
            Call(match,"SetupScreen");yield return null;
        }
        if(interruptRealLimiter)Assert.That(observedInterruptedLimiter,Is.True);
        if(riders&&focusedRiderPresentation)Log("ACTUAL_BROAD_LAYOUT_RESET_DIAGNOSTIC observedStale="+observedStaleNativeModeResets+"; deliberate positive busy-mode cleanup belongs to its dedicated public-rematch case.");
    }
    [UnityTest,Timeout(180000)] public IEnumerator RidersRealBusyBreathInterruptedByPublicRematchClearsNativeModeAndAllowsNextAssignedBreath(){yield return Exercise("TruckDragonRiders","LWS.TruckDragonRiders.DragonRidersMatch",false,false,true);}
    [UnityTest,Timeout(600000)] public IEnumerator RidersRealSoftwareSourcesAndCamerasKeepFourParticipantOwners(){yield return Exercise("TruckDragonRiders","LWS.TruckDragonRiders.DragonRidersMatch");}
    [UnityTest,Timeout(600000)] public IEnumerator RidersActualAssignedActionsCabLookViewportHudAndSameConfigurationRematch(){yield return Exercise("TruckDragonRiders","LWS.TruckDragonRiders.DragonRidersMatch",false,true);}
    [UnityTest,Timeout(180000)] public IEnumerator RidersNeutralCabPureConnectedNeckGeometryScreen(){yield return Exercise("TruckDragonRiders","LWS.TruckDragonRiders.DragonRidersMatch",false,true,false,2);}
    [UnityTest,Timeout(240000)] public IEnumerator RidersTransientSafeMountedProfilesActuallyAnimateBothCabsAfterPublicRematch(){yield return Exercise("TruckDragonRiders","LWS.TruckDragonRiders.DragonRidersMatch",false,true,false,2,true,.25f,true,true);}
    [UnityTest,Timeout(240000)] public IEnumerator RidersTransientMixedEnvelopeActuallyAnimatesBothCabsAfterPublicRematch(){yield return Exercise("TruckDragonRiders","LWS.TruckDragonRiders.DragonRidersMatch",false,true,false,2,true,.25f,true);}
    [UnityTest,Timeout(300000)] public IEnumerator RidersTransientDerivedFire187KeepsSavedControllerAndOriginalNwhThroughActualCabActionsAndRematches(){yield return Exercise("TruckDragonRiders","LWS.TruckDragonRiders.DragonRidersMatch",false,true,false,2,false,null,false,false,true);}
    [UnityTest,Timeout(300000)] public IEnumerator RidersQualifiedFireHeadLever60Minus15StrictTorso167ClearsActualCabsAndKeepsOriginalNwhThroughBreathAndRematches(){yield return Exercise("TruckDragonRiders","LWS.TruckDragonRiders.DragonRidersMatch",false,true,false,2,sizedFireHeadLever:true);}
    [UnityTest,Timeout(300000)] public IEnumerator RidersQualifiedFireNormalFamily167StrictTorsoClearsActualCabsAndKeepsOriginalNwhThroughBreathAndRematches(){yield return Exercise("TruckDragonRiders","LWS.TruckDragonRiders.DragonRidersMatch",false,true,false,2,false,null,false,false,false,true);}
    [UnityTest,Timeout(240000)] public IEnumerator RidersTransientQuarterRootFlapEnvelopeActuallyAnimatesBothCabsAfterPublicRematch(){yield return Exercise("TruckDragonRiders","LWS.TruckDragonRiders.DragonRidersMatch",false,true,false,2,true,.25f);}
    [UnityTest,Timeout(240000)] public IEnumerator RidersTransientHalfRootFlapEnvelopeActuallyAnimatesBothCabsAfterPublicRematch(){yield return Exercise("TruckDragonRiders","LWS.TruckDragonRiders.DragonRidersMatch",false,true,false,2,true,.5f);}
    [UnityTest,Timeout(240000)] public IEnumerator RidersTransientAuthoredFlightClipsActuallyAnimateAndClearInfernoCabAfterPublicRematch(){yield return Exercise("TruckDragonRiders","LWS.TruckDragonRiders.DragonRidersMatch",false,true,false,2,true);}
    [UnityTest,Timeout(600000)] public IEnumerator HeistRealSoftwareSourcesAndCamerasKeepFourParticipantOwners(){yield return Exercise("TruckHoardHeist","LWS.TruckHoardHeist.HoardHeistMatch");}
    [UnityTest,Timeout(600000)] public IEnumerator HeistActualRevLimiterInterruptionThenRematchRestoresRealAssignedDrive(){yield return Exercise("TruckHoardHeist","LWS.TruckHoardHeist.HoardHeistMatch",true);}
    [UnityTest,Timeout(600000)] public IEnumerator EggRealSoftwareSourcesAndCamerasKeepFourParticipantOwners(){yield return Exercise("TruckDragonEggDelivery","LWS.TruckDragonEggDelivery.DragonEggDeliveryMatch");}
    [UnityTest,Timeout(600000)] public IEnumerator EscapeRealSoftwareSourcesAndCamerasKeepFourParticipantOwners(){yield return Exercise("TruckDragonEscape","LWS.TruckDragonEscape.DragonEscapeMatch");}
    [UnityTearDown] public IEnumerator Cleanup()
    {
        Exception cleanupFailure=null;void Release(System.Action action){try{action();}catch(Exception error){if(cleanupFailure==null)cleanupFailure=error;TestContext.WriteLine("NATIVE_INPUT_CLEANUP_FAILURE "+error);}}
        Release(()=>{if(match!=null)Call(match,"SetupScreen");});Release(()=>{if(!string.IsNullOrEmpty(output))File.WriteAllText(Path.Combine(output,reportName+".txt"),report.ToString());});
        Release(()=>{if(transientFireGeometryObserver!=null){Set(transientFireGeometryObserver,"Observe",null);UnityEngine.Object.DestroyImmediate(transientFireGeometryObserver.gameObject);}});transientFireGeometryObserver=null;
        Release(()=>transientFireSizeLease?.Dispose());transientFireSizeLease=null;Release(()=>coldInfernoFixtureLease?.Dispose());coldInfernoFixtureLease=null;
        try
        {
            var current=SceneManager.GetActiveScene();var empty=SceneManager.CreateScene("Dragon input cleanup "+Guid.NewGuid().ToString("N"));SceneManager.SetActiveScene(empty);if(current.IsValid()&&current.isLoaded)yield return SceneManager.UnloadSceneAsync(current);match=null;
        }
        finally
        {
            foreach(var item in transientCabObjects)Release(()=>{if(item!=null)UnityEngine.Object.DestroyImmediate(item);});transientCabObjects.Clear();
            Release(()=>{if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);});for(int i=0;i<pads.Length;i++){var pad=pads[i];Release(()=>{if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);});pads[i]=null;}
            Release(()=>{if(riderMouse!=null&&riderMouse.added)InputSystem.RemoveDevice(riderMouse);});riderMouse=null;
            Release(()=>{if(original!=null)InputSystem.settings=original;});Release(()=>{if(temporary!=null)UnityEngine.Object.DestroyImmediate(temporary);});Release(()=>{if(isolation!=null)UnityEngine.Object.DestroyImmediate(isolation);});Release(()=>preferences?.Dispose());preferences=null;Time.timeScale=1;Release(()=>Runtime("LWS.TruckSports.SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null));
        }
        if(cleanupFailure!=null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(cleanupFailure).Throw();
    }
}
#endif
