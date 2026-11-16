#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using LWS.TruckDragonEggDelivery;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed partial class DragonEggDeliveryNativeTests
{
    const string Scene = "Assets/TruckDragonEggDelivery/Scenes/TruckDragonEggDelivery.unity";
    object match;
    IDisposable preferences;
    GameObject inputIsolation;
    string output;
    readonly StringBuilder evidence=new StringBuilder();
    readonly bool[] bayCaptured=new bool[4];
    static Type Runtime(string name)=>Type.GetType(name+", Assembly-CSharp",true);
    static object Get(object value, string name) => value.GetType().GetField(name) is FieldInfo field ? field.GetValue(value) : value.GetType().GetProperty(name).GetValue(value);
    static object Call(object value, string name, params object[] args) => value.GetType().GetMethod(name).Invoke(value, args);
    IEnumerator StartActualSavedJourney(bool controlledNearBay=false)
    {
        evidence.Clear();Array.Clear(bayCaptured,0,bayCaptured.Length);
        if(controlledNearBay)RememberResultsSourceBytes();
        Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(Scene), Is.Not.Null, "Explicitly author the actual saved shared-world Egg Delivery scene first.");
        string[] args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");output=Path.GetFullPath(Path.Combine(at>=0&&at+1<args.Length?args[at+1]:Application.persistentDataPath,"EggNative"));Directory.CreateDirectory(output);
        preferences=(IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"EggNative"+TestContext.CurrentContext.Test.MethodName});Runtime("LWS.TruckSports.SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);
        inputIsolation=new GameObject("Egg native journey physical-device isolation");UnityEngine.Object.DontDestroyOnLoad(inputIsolation);inputIsolation.AddComponent(Runtime("LWS.TruckSports.SportsProbeInputIsolation"));
        EditorSceneManager.LoadSceneInPlayMode(Scene, new LoadSceneParameters(LoadSceneMode.Single)); yield return null;yield return null;
        var type = Type.GetType("LWS.TruckDragonEggDelivery.DragonEggDeliveryMatch, Assembly-CSharp", true);
        match = UnityEngine.Object.FindAnyObjectByType(type); Assert.That(match, Is.Not.Null);
        if(controlledNearBay)inputIsolation.AddComponent<DragonEggJointTransitionObservation>().Configure(match,Path.Combine(output,"controlled-nearbay-actual-joint-transitions-before-physx.txt"));
        var setup=Get(match,"setup");setup.GetType().GetField("count").SetValue(setup,4);
        type.GetField("option").SetValue(match, 2); type.GetField("fixedSeed").SetValue(match, 812);
        Call(match, "AllBots");if(controlledNearBay)ConfigureControlledBayStarts(false); Assert.That(Call(match, "StartMatch"), Is.EqualTo(true));
        Assert.That(Get(match,"AllAI"),Is.EqualTo(true));Assert.That(((DragonEggDeliveryRules)Get(match,"Rules")).Count,Is.EqualTo(4));
        Snapshot("ordinary-start-returned");
        float supportDeadline=Time.realtimeSinceStartup+35;while((float)Get(match,"Intro")>0&&Time.realtimeSinceStartup<supportDeadline)yield return null;
        Snapshot("normal-speed-actual-supported-start");Assert.That((float)Get(match,"Intro"),Is.Zero,"Original NWH wheel support/countdown must complete before accelerated journey observation.");
        Capture("actual-supported-start");
        Time.timeScale = 4;
        TestContext.WriteLine("Actual saved-world AI/NWH Egg Delivery fixture at4xsimulation; no human input or normal-speed performance claim.");
    }
    void ConfigureControlledBayStarts(bool quarterTurn)
    {
        Assert.That(Get(match,"Running"),Is.EqualTo(false),"Controlled initial spawn configuration only, never move a running truck/cargo.");
        var world=Get(match,"world");var graph=Get(Get(world,"definition"),"graph");var starts=(Transform[])Get(Call(world,"Region","castle"),"truckStarts");var nests=(Array)Get(match,"nests");var query=Type.GetType("LWS.InterstateHauler.LwsRoadGraphQuery, LWS.InterstateHauler.Runtime",true).GetMethod("TryFindNearestRoad");
        for(int slot=0;slot<4;slot++)
        {
            var approach=(Transform)Get(nests.GetValue(slot),"approach");object[] args={graph,approach.position,150f,null};Assert.That((bool)query.Invoke(null,args),Is.True);
            Vector3 projection=(Vector3)Get(args[3],"NearestPosition"),point=Vector3.Lerp(projection,approach.position,.45f),toward=Vector3.ProjectOnPlane(approach.position-point,Vector3.up).normalized;
            Assert.That(Vector3.Distance(point,approach.position),Is.InRange(8,65),"A genuine finite approach, not an already parked/delivered load.");
            starts[slot].SetPositionAndRotation(point,Quaternion.LookRotation(quarterTurn?Quaternion.AngleAxis(90,Vector3.up)*toward:-toward));
            evidence.AppendLine("CONTROLLED_INITIAL_BAY_START slot="+slot+" marker="+point+" heading="+starts[slot].forward+" actualGoal="+approach.position+" · unsaved scene start-marker configuration before ordinary StartMatch, not full journey acceptance.");
        }
        Physics.SyncTransforms();
    }
    void Snapshot(string phase)
    {
        if(match==null)return;var rules=(DragonEggDeliveryRules)Get(match,"Rules");var carriers=(Array)Get(match,"carriers");
        string text="ACTUAL_EGG_JOURNEY phase="+phase+" intro="+Get(match,"Intro")+" clock="+rules.Clock+" finished="+Get(match,"Finished")+" physicalDeliveries="+Get(match,"PhysicalDeliveries")+" match="+Get(match,"MatchId");
        for(int slot=0;slot<rules.Count;slot++)
        {
            var carrier=carriers.GetValue(slot);var cargo=Get(carrier,"cargo");var body=(Rigidbody)Get(cargo,"body");var truck=Get(carrier,"truck");var truckBody=(Rigidbody)Get(truck,"body");var record=rules.Cargo[slot];
            var nest=((Array)Get(match,"nests")).GetValue(slot);var center=(Transform)Get(nest,"center");var approach=(Transform)Get(nest,"approach");var route=Call(match,"NavigationRoute",slot);
            var restraint=Get(cargo,"Restraint") as Joint;var motor=Get(truck,"tractorMotor");var states=Get(Get(Get(motor,"vehicle"),"input"),"states");
            var crane=Get(carrier,"crane");var hoistHead=(Rigidbody)Get(crane,"spreader");var socket=(Transform)Get(carrier,"socket");
            var points=(IList)Get(route,"points");int cursor=(int)Get(route,"Cursor");var current=cursor>=0&&cursor<points.Count?(Vector3)points[cursor]:truckBody.position;
            text+=" | slot="+slot+" id="+record.Id+" stage="+record.Stage+" condition="+record.Condition+" hatchAt="+record.HatchAt+" score="+record.Score+" body="+body.GetEntityId()+" position="+body.position.ToString("F3")+" velocity="+body.linearVelocity.ToString("F3")+" truck="+truckBody.position.ToString("F3")+" truckSpeed="+truckBody.linearVelocity.magnitude+" powered="+Get(truck,"Powered")+" grounded="+Get(motor,"Grounded")+" throttle="+Get(states,"throttle")+" brakes="+Get(states,"brakes")+" restraint="+(restraint!=null)+" restraintBody="+(restraint!=null&&restraint.connectedBody!=null?restraint.connectedBody.name:"none")+" craneBusy="+Get(carrier,"Busy")+" crane="+Get(Get(carrier,"crane"),"Stage")+" hatches="+Get(cargo,"Hatches")+" babyVisible="+Get(cargo,"IsBabyVisible")+
                " nest="+center.position.ToString("F3")+" approach="+approach.position.ToString("F3")+" physicalReach="+Vector3.Distance(truckBody.position,center.position)+" approachDistance="+Vector3.Distance(truckBody.position,approach.position)+" goal="+Call(match,"NavigationGoal",slot)+" path="+cursor+"/"+points.Count+" currentWaypoint="+current.ToString("F3")+" forward="+truckBody.transform.forward.ToString("F3")+" command="+Get(truck,"command")+" desiredSpeed="+Get(route,"DesiredSpeed")+" reversing="+Get(route,"Reversing")+" obstacle="+Get(route,"BlockingObstacle")+" stalled="+Get(route,"SecondsWithoutProgress")+" routeRecoveries="+Get(route,"RecoveryStarts")+" finalBay="+Call(match,"UsingBayParking",slot)+" bayReverseObserved="+Call(match,"BayParkingReverseObserved",slot)+" bayChecks="+Call(match,"BayCorridorChecks",slot)+" bayStatus="+Call(match,"BayParkingStatus",slot)+" actualHoistHead="+hoistHead.position.ToString("F3")+" carrySocket="+socket.position.ToString("F3")+" headSocketDistance="+Vector3.Distance(hoistHead.position,socket.position)+" timeScale="+Time.timeScale+" actualNestContainsSupports="+Call(nest,"ContainsAndSupports",cargo)+" actualSettled="+Get(cargo,"Settled")+" bayYieldStarts="+Call(match,"BayYieldStarts",slot)+" separatingMoves="+Call(match,"BayYieldSeparatingSteps",slot);
        }
        evidence.AppendLine(text);TestContext.WriteLine(text);File.WriteAllText(Path.Combine(output,TestContext.CurrentContext.Test.MethodName+"-progress.txt"),evidence.ToString());
    }
    void Capture(string phase)
    {
        try{using(var hash=System.Security.Cryptography.SHA256.Create()){string test=BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(TestContext.CurrentContext.Test.MethodName))).Replace("-","").Substring(0,12).ToLowerInvariant();Runtime("LWS.TruckSports.SportsControlledCapture").GetMethods().Single(method=>method.Name=="Save"&&method.GetParameters().Length==5).Invoke(null,new object[]{match,1280,720,Path.Combine(output,"egg-"+test+"-"+phase+".png"),null});}}
        catch(Exception exception){TestContext.WriteLine("ACTUAL_EGG_CAPTURE_FAILED "+exception);}
    }
    [UnityTest, Timeout(180000)]
    public IEnumerator ActualNwhTransportAndPrematureHatchKeepTheSameDynamicCargoIdentity()
    {
        yield return StartActualSavedJourney();
        var carriers = (Array)Get(match, "carriers"); var cargo = Get(carriers.GetValue(0), "cargo"); var body = (Rigidbody)Get(cargo, "body");
        var rules = (DragonEggDeliveryRules)Get(match, "Rules"); string identity = rules.Cargo[0].Id;
        var truck = Get(carriers.GetValue(0), "truck"); var truckBody = (Rigidbody)Get(truck, "body"); Vector3 start = truckBody.position;
        float deadline = Time.realtimeSinceStartup + 150,nextSnapshot=Time.realtimeSinceStartup+10; bool actualMovement = false;
        while (!(bool)Get(match, "Finished") && rules.Cargo[0].Stage != DragonEggStage.Baby && Time.realtimeSinceStartup < deadline)
        {
            var restraint=(ConfigurableJoint)Get(cargo,"Restraint");actualMovement |= Vector3.Distance(start, truckBody.position) > 20 && body.linearVelocity.magnitude > 1&&restraint!=null&&restraint.connectedBody==truckBody;
            if(Time.realtimeSinceStartup>=nextSnapshot){Snapshot("transport-progress");nextSnapshot=Time.realtimeSinceStartup+10;}
            yield return null;
        }
        Snapshot("hatch-or-timeout");
        Capture("actual-hatch-or-timeout");
        Assert.That(actualMovement, Is.True, "An AI truck must drive its actual physical load, not receive a synthetic delivery.");
        Assert.That(rules.Cargo[0].Stage, Is.EqualTo(DragonEggStage.Baby)); Assert.That(rules.Cargo[0].Id, Is.EqualTo(identity));
        Assert.That(Get(cargo, "body"), Is.SameAs(body)); Assert.That(body.isKinematic, Is.False); Assert.That(Get(cargo, "IsBabyVisible"), Is.EqualTo(true));
        var animator = (Animator)Get(cargo, "babyAnimator"); Assert.That(animator.runtimeAnimatorController, Is.Not.Null);
        Call(match, "Pause", 0, false); Assert.That(Get(match, "Paused"), Is.EqualTo(true)); Call(match, "Resume");
        Assert.That(Call(match, "StartMatch"), Is.EqualTo(true));
        var fresh = (DragonEggDeliveryRules)Get(match, "Rules"); Assert.That(fresh.Cargo[0].Stage, Is.EqualTo(DragonEggStage.Egg));
        Assert.That(Get(cargo, "Hatches"), Is.EqualTo(0)); Assert.That(Get(cargo, "IsBabyVisible"), Is.EqualTo(false));
        Snapshot("ordinary-rematch");Capture("ordinary-rematch");
    }
    [UnityTest, Timeout(720000)]
    public IEnumerator ActualAiMustUnloadRealCargoAtCorrectSupportedNestAndProduceOrdinaryResults()
    {
        RememberResultsSourceBytes();
        yield return StartActualSavedJourney();
        var carriers=(Array)Get(match,"carriers");
        var bodies=Enumerable.Range(0,4).Select(slot=>(Rigidbody)Get(Get(carriers.GetValue(slot),"cargo"),"body")).ToArray();
        var trucks=Enumerable.Range(0,4).Select(slot=>(Rigidbody)Get(Get(carriers.GetValue(slot),"truck"),"body")).ToArray();
        var chips=Enumerable.Range(0,4).Select(slot=>(Rigidbody[])Get(Get(carriers.GetValue(slot),"cargo"),"shellChips")).Select(items=>items.ToArray()).ToArray();
        var starts=(Transform[])Get(Call(Get(match,"world"),"Region","castle"),"truckStarts");
        var startPositions=starts.Select(item=>item.position).ToArray();var startRotations=starts.Select(item=>item.rotation).ToArray();
        string previousMatch=null;
        for(int run=0;run<2;run++)
        {
            if(run>0)
            {
                Call(match,"SetupScreen");Assert.That(Get(match,"Running"),Is.EqualTo(false));AssertEggResultsReset();
                Assert.That(Call(match,"StartMatch"),Is.EqualTo(true));AssertEggResultsReset();
                Assert.That(Get(match,"AllAI"),Is.EqualTo(true));Assert.That(Get(match,"Finished"),Is.EqualTo(false));
                Assert.That(Get(match,"PhysicalDeliveries"),Is.EqualTo(0));Assert.That(Get(Get(match,"ScoreJournal"),"Count"),Is.EqualTo(0));
                var fresh=(DragonEggDeliveryRules)Get(match,"Rules");Assert.That(fresh.Count,Is.EqualTo(4));Assert.That(fresh.Clock,Is.Zero);
                for(int slot=0;slot<4;slot++)
                {
                    var cargo=Get(carriers.GetValue(slot),"cargo");Assert.That(fresh.Cargo[slot].Stage,Is.EqualTo(DragonEggStage.Egg));Assert.That(fresh.Cargo[slot].Score,Is.Zero);
                    Assert.That(Get(cargo,"Hatches"),Is.EqualTo(0));Assert.That(Get(cargo,"IsBabyVisible"),Is.EqualTo(false));Assert.That(bodies[slot].isKinematic,Is.False);
                    CollectionAssert.AreEqual(chips[slot],(Rigidbody[])Get(cargo,"shellChips"));
                    foreach(var chip in chips[slot]){Assert.That(chip.gameObject.activeSelf,Is.False);Assert.That(chip.isKinematic,Is.True);Assert.That(chip.detectCollisions,Is.False);}
                }
                Array.Clear(bayCaptured,0,bayCaptured.Length);Snapshot("ordinary-rematch-returned");
                float supportDeadline=Time.realtimeSinceStartup+35;while((float)Get(match,"Intro")>0&&Time.realtimeSinceStartup<supportDeadline)yield return null;
                Assert.That((float)Get(match,"Intro"),Is.Zero,"The second saved-course journey requires ordinary NWH support/countdown at normal speed too.");
                Snapshot("ordinary-rematch-supported-start");Capture("ordinary-rematch-supported-start");Time.timeScale=4;
            }
            string identity=(string)Get(match,"MatchId");Assert.That(identity,Is.Not.Null.And.Not.Empty);Assert.That(identity,Is.Not.EqualTo(previousMatch));previousMatch=identity;
            var rules=(DragonEggDeliveryRules)Get(match,"Rules");var witnessed=new bool[4];
            float deadline=Time.realtimeSinceStartup+300,nextSnapshot=Time.realtimeSinceStartup+10;
            while(!(bool)Get(match,"Finished")&&Time.realtimeSinceStartup<deadline)
            {
                bool nearBay=false;var nests=(Array)Get(match,"nests");
                for(int slot=0;slot<4;slot++)
                {
                    var cargo=Get(carriers.GetValue(slot),"cargo");Assert.That(Get(cargo,"body"),Is.SameAs(bodies[slot]));Assert.That(Get(Get(carriers.GetValue(slot),"truck"),"body"),Is.SameAs(trucks[slot]));
                    if(rules.Cargo[slot].Stage==DragonEggStage.Delivered&&!witnessed[slot])
                    {
                        Assert.That(rules.Cargo[slot].Score,Is.GreaterThan(0));Assert.That(Get(cargo,"PhysicallyPresent"),Is.EqualTo(true));Assert.That(Call(nests.GetValue(slot),"ContainsAndSupports",cargo),Is.EqualTo(true));Assert.That(Get(cargo,"Settled"),Is.EqualTo(true));
                        witnessed[slot]=true;Snapshot("actual-supported-scored-delivery-run"+run+"-slot"+slot);
                    }
                    var approach=(Transform)Get(nests.GetValue(slot),"approach");if(Vector3.Distance(trucks[slot].position,approach.position)>=50)continue;
                    nearBay=true;if(!bayCaptured[slot]){bayCaptured[slot]=true;Snapshot("actual-first-bay-approach-run"+run+"-slot"+slot);Capture("actual-first-bay-approach-run"+run+"-slot"+slot);}
                }
                if(Time.realtimeSinceStartup>=nextSnapshot){Snapshot("complete-journey-run"+run+"-progress");nextSnapshot=Time.realtimeSinceStartup+(nearBay?2:10);}
                yield return null;
            }
            Snapshot("result-or-timeout-run"+run);Capture("actual-result-or-timeout-run"+run);
            Assert.That(Get(match,"Finished"),Is.EqualTo(true));Assert.That((int)Get(match,"PhysicalDeliveries"),Is.GreaterThan(0));Assert.That(rules.Complete,Is.True);
            foreach(var cargo in rules.Cargo)if(cargo.Score>0)Assert.That(cargo.Stage,Is.EqualTo(DragonEggStage.Delivered));
            Assert.That(rules.Cargo.Count(cargo=>cargo.Score>0),Is.EqualTo((int)Get(match,"PhysicalDeliveries")));
            Assert.That(witnessed.Count(value=>value),Is.EqualTo((int)Get(match,"PhysicalDeliveries")),"Results require the same original cargo observed supported and settled at its correct nest in this round; timer expiry alone cannot pass.");
            Assert.That(Get(Get(match,"ScoreJournal"),"Count"),Is.EqualTo(2*(int)Get(match,"PhysicalDeliveries")),"Each actual delivery owns its ordinary score and time receipt, without duplicates or stale rematch awards.");
            Assert.That(Get(match,"Results").ToString(),Does.Contain("PTS"));
            for(int slot=0;slot<starts.Length;slot++){Assert.That(starts[slot].position,Is.EqualTo(startPositions[slot]));Assert.That(starts[slot].rotation,Is.EqualTo(startRotations[slot]),"No near-bay marker placement is permitted in either full journey.");}
            yield return ObserveActualEggResultsBoard(rules,run,requireAllDeliveries:false);
            AssertResultsSourceBytes();evidence.AppendLine("FULL_SAVED_EGG_JOURNEY run="+run+" match="+identity+" physicalDeliveries="+Get(match,"PhysicalDeliveries")+" actualSupportedScoreWitnesses="+witnessed.Count(value=>value)+" publicRematch="+(run>0)+" sourceExact=true");
        }
        Call(match,"SetupScreen");Assert.That(Get(match,"Running"),Is.EqualTo(false));AssertEggResultsReset();AssertResultsSourceBytes();
    }
    [UnityTest, Timeout(240000)]
    public IEnumerator ControlledNearBayOriginalNwhReverseAndQuarterTurnParkingPhysicallyUnloadAcrossTwoStarts()
    {
        yield return StartActualSavedJourney(true);
        TestContext.WriteLine("CONTROLLED NEAR-BAY CONTRACT ONLY: initial scene markers on actual road-to-bay connectors, original NWH/cranes/cargo/nest awards. Does not replace the untouched full15minute journey fixture.");
        var carriers=(Array)Get(match,"carriers");var bodies=Enumerable.Range(0,4).Select(slot=>(Rigidbody)Get(Get(carriers.GetValue(slot),"cargo"),"body")).ToArray();
        var configuredTravel=Enumerable.Range(0,4).Select(slot=>(float)Get(Get(carriers.GetValue(slot),"crane"),"travelSpeed")).ToArray();var configuredLift=Enumerable.Range(0,4).Select(slot=>(float)Get(Get(carriers.GetValue(slot),"crane"),"liftSpeed")).ToArray();
        for(int run=0;run<2;run++)
        {
            if(run>0)
            {
                Call(match,"SetupScreen");AssertEggResultsReset();Time.timeScale=1;ConfigureControlledBayStarts(true);Assert.That(Call(match,"StartMatch"),Is.EqualTo(true));AssertEggResultsReset();for(int slot=0;slot<4;slot++)Assert.That(Call(match,"BayCorridorChecks",slot),Is.EqualTo(0),"Ordinary Prepare resets parking history.");
                float support=Time.realtimeSinceStartup+35;while((float)Get(match,"Intro")>0&&Time.realtimeSinceStartup<support)yield return null;Assert.That((float)Get(match,"Intro"),Is.Zero);Time.timeScale=4;
            }
            bool paused=false;var reverseMotion=new bool[4];float deadline=Time.realtimeSinceStartup+85,next=Time.realtimeSinceStartup;
            while(!(bool)Get(match,"Finished")&&Time.realtimeSinceStartup<deadline)
            {
                for(int slot=0;slot<4;slot++){var body=(Rigidbody)Get(Get(carriers.GetValue(slot),"truck"),"body");reverseMotion[slot]|=(bool)Call(match,"UsingBayParking",slot)&&Vector3.Dot(body.linearVelocity,body.transform.forward)<-.35f;}
                if(!paused&&Enumerable.Range(0,4).Any(slot=>(bool)Call(match,"UsingBayParking",slot)))
                {
                    var positions=Enumerable.Range(0,4).Select(slot=>((Rigidbody)Get(Get(carriers.GetValue(slot),"truck"),"body")).position).ToArray();var checks=Enumerable.Range(0,4).Select(slot=>(int)Call(match,"BayCorridorChecks",slot)).ToArray();Call(match,"Pause",0,false);yield return new WaitForSecondsRealtime(.2f);
                    for(int slot=0;slot<4;slot++){Assert.That(Vector3.Distance(positions[slot],((Rigidbody)Get(Get(carriers.GetValue(slot),"truck"),"body")).position),Is.LessThan(.01f));Assert.That(Call(match,"BayCorridorChecks",slot),Is.EqualTo(checks[slot]));}Call(match,"Resume");Time.timeScale=4;paused=true;
                }
                if(Time.realtimeSinceStartup>=next){Snapshot("controlled-nearbay-run"+run);next=Time.realtimeSinceStartup+2;}
                yield return null;
            }
            Snapshot("controlled-nearbay-result-run"+run);Capture("controlled-nearbay-result-run"+run);Assert.That(paused,Is.True);Assert.That(Get(match,"Finished"),Is.EqualTo(true));Assert.That(Get(match,"PhysicalDeliveries"),Is.EqualTo(4),"All four original crews must genuinely crane/unload; no staged delivery or callback.");
            var jointWitness=inputIsolation.GetComponent<DragonEggJointTransitionObservation>();Assert.That(jointWitness,Is.Not.Null);Assert.That(jointWitness.UnsafeDisconnectedDriveSteps,Is.Zero,"Deferred restraint destruction must never leave an active driven world joint for a real physics step.");Assert.That(jointWitness.InertReleasedJointSteps,Is.GreaterThan(0));Assert.That(jointWitness.ActualGripHandovers,Is.GreaterThanOrEqualTo((run+1)*4),"Every real unload must acquire the original Jenga FixedJoint before releasing its cradle.");Assert.That(jointWitness.MaximumHandoverAnchorError,Is.LessThan(.02f),"Deferred inert restraint preserves its actual world anchor during acquired-grip handover.");
            Assert.That(jointWitness.ActualPrematureGripLosses,Is.Zero,"Real finite Jenga grip must survive Lift/Traverse/LowerStack until ordinary Release; no force/torque gate is increased.");for(int slot=0;slot<4;slot++){Assert.That(Get(Get(carriers.GetValue(slot),"crane"),"travelSpeed"),Is.EqualTo(configuredTravel[slot]));Assert.That(Get(Get(carriers.GetValue(slot),"crane"),"liftSpeed"),Is.EqualTo(configuredLift[slot]));}
            var rules=(DragonEggDeliveryRules)Get(match,"Rules");for(int slot=0;slot<4;slot++){Assert.That(rules.Cargo[slot].Stage,Is.EqualTo(DragonEggStage.Delivered));Assert.That(rules.Cargo[slot].Score,Is.GreaterThan(0));Assert.That(Get(Get(carriers.GetValue(slot),"cargo"),"body"),Is.SameAs(bodies[slot]));if(run==0){Assert.That(Call(match,"BayParkingReverseObserved",slot),Is.EqualTo(true),"An initially away-facing tractor must use actual reverse hysteresis.");Assert.That(reverseMotion[slot],Is.True,"The original NWH body must actually travel in reverse, not merely request the parking mode.");}}
            yield return ObserveActualEggResultsBoard(rules,run);
        }
    }
    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        if(match is UnityEngine.Object item&&item!=null)Call(match,"SetupScreen");Time.timeScale=1;
        if(output!=null)File.WriteAllText(Path.Combine(output,TestContext.CurrentContext.Test.MethodName+".txt"),evidence.ToString());
        var current=SceneManager.GetActiveScene();var empty=SceneManager.CreateScene("Egg native cleanup "+Guid.NewGuid().ToString("N"));SceneManager.SetActiveScene(empty);if(current.IsValid()&&current.isLoaded)yield return SceneManager.UnloadSceneAsync(current);match=null;
        if(inputIsolation!=null)UnityEngine.Object.DestroyImmediate(inputIsolation);preferences?.Dispose();preferences=null;Runtime("LWS.TruckSports.SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);
        AssertResultsSourceBytes();
    }
}
#endif
