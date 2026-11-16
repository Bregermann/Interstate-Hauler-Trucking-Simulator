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

// Controlled encounter, not full-journey acceptance. Only the unsaved initial
// spawn markers change; the original start, NWH, collisions, cranes and awards run.
public sealed class DragonEggBayEncounterNativeTests
{
    const string ScenePath="Assets/TruckDragonEggDelivery/Scenes/TruckDragonEggDelivery.unity";
    object match;
    IDisposable preferences;
    GameObject isolation;
    string output;
    readonly StringBuilder trace=new StringBuilder();
    readonly Vector3[] requested=new Vector3[4];
    readonly Collider[] nearby=new Collider[64];
    readonly RaycastHit[] ahead=new RaycastHit[32];
    static Type Runtime(string name)=>Type.GetType(name+", Assembly-CSharp",true);
    static object Get(object item,string name)=>item.GetType().GetField(name) is FieldInfo field?field.GetValue(item):item.GetType().GetProperty(name).GetValue(item);
    static object Call(object item,string name,params object[] args)=>item.GetType().GetMethod(name).Invoke(item,args);
    object Carrier(int slot)=>((Array)Get(match,"carriers")).GetValue(slot);
    object Truck(int slot)=>Get(Carrier(slot),"truck");
    Rigidbody Body(int slot)=>(Rigidbody)Get(Truck(slot),"body");
    BoxCollider Shape(int slot)=>(BoxCollider)Get(Truck(slot),"shape");
    DragonEggDeliveryRules Rules=>(DragonEggDeliveryRules)Get(match,"Rules");
    bool OpposedTruckActuallyBlocks(int slot)
    {
        var body=Body(slot);var shape=Shape(slot);var truck=((Component)Truck(slot)).transform;var other=(Component)Truck(slot==0?2:0);
        Vector3 center=truck.TransformPoint(shape.center),half=Vector3.Scale(shape.size*.5f,truck.lossyScale)+new Vector3(.2f,.05f,.3f);
        var truckType=Runtime("LWS.TruckSports.SportsEventTruck");
        int count=Physics.OverlapBoxNonAlloc(center,half,nearby,body.rotation,~0,QueryTriggerInteraction.Ignore);
        Assert.That(count,Is.LessThan(nearby.Length));for(int i=0;i<count;i++)if(nearby[i].GetComponentInParent(truckType)==other)return true;
        var reverse=(bool[])match.GetType().GetField("bayReverse",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(match);
        Vector3 direction=Vector3.ProjectOnPlane(truck.forward,Vector3.up).normalized*(reverse[slot]?-1:1);
        count=Physics.BoxCastNonAlloc(center,half,direction,ahead,body.rotation,Mathf.Clamp(body.linearVelocity.magnitude*.75f+1.5f,1.5f,7),~0,QueryTriggerInteraction.Ignore);
        Assert.That(count,Is.LessThan(ahead.Length));for(int i=0;i<count;i++)if(ahead[i].collider.GetComponentInParent(truckType)==other)return true;
        return false;
    }

    void ConfigureClearEncounter()
    {
        Assert.That(Get(match,"Running"),Is.EqualTo(false));
        var world=Get(match,"world");var starts=(Transform[])Get(Call(world,"Region","castle"),"truckStarts");
        var graph=Get(Get(world,"definition"),"graph");var nests=(Array)Get(match,"nests");
        var goals=Enumerable.Range(0,4).Select(slot=>((Transform)Get(nests.GetValue(slot),"approach")).position).ToArray();
        Vector3 midpoint=(goals[0]+goals[2])*.5f,axis=Vector3.ProjectOnPlane(goals[0]-goals[2],Vector3.up).normalized;
        var nearest=Type.GetType("LWS.InterstateHauler.LwsRoadGraphQuery, LWS.InterstateHauler.Runtime",true).GetMethod("TryFindNearestRoad");
        for(int slot=0;slot<4;slot++)
        {
            Vector3 point;
            if(slot==0||slot==2)point=midpoint+axis*(slot==0?-3.4f:3.4f);
            else {object[] args={graph,goals[slot],150f,null};Assert.That(nearest.Invoke(null,args),Is.EqualTo(true));point=Vector3.Lerp((Vector3)Get(args[3],"NearestPosition"),goals[slot],.45f);}
            Vector3 toward=Vector3.ProjectOnPlane(goals[slot]-point,Vector3.up).normalized;
            Assert.That(Vector3.Distance(point,goals[slot]),Is.InRange(8,65));requested[slot]=point;
            starts[slot].SetPositionAndRotation(point,Quaternion.LookRotation(toward));
            trace.AppendLine("INITIAL_MARKER slot="+slot+" position="+point.ToString("F4")+" toward="+toward.ToString("F4")+" realBay="+goals[slot].ToString("F4"));
        }
        Physics.SyncTransforms();
    }
    void Observe(string phase)
    {
        var a=Shape(0);var b=Shape(2);bool penetrating=Physics.ComputePenetration(a,a.transform.position,a.transform.rotation,b,b.transform.position,b.transform.rotation,out var separation,out float depth);
        if(!penetrating){separation=Vector3.zero;depth=0;} // Native out values are undefined when there is no penetration.
        trace.AppendLine("ENCOUNTER phase="+phase+" clock="+Rules.Clock+" physicalDeliveries="+Get(match,"PhysicalDeliveries")+" bodyCenterGap="+Vector3.Distance(a.bounds.center,b.bounds.center)+" boundsGap="+Mathf.Sqrt(a.bounds.SqrDistance(b.bounds.ClosestPoint(a.bounds.center)))+" actualShapePenetration="+penetrating+" depth="+depth+" separate0Direction="+separation);
        for(int slot=0;slot<4;slot++)
        {
            var body=Body(slot);var truck=Truck(slot);var carrier=Carrier(slot);var cargo=Get(carrier,"cargo");var motor=Get(truck,"tractorMotor");var state=Get(Get(Get(motor,"vehicle"),"input"),"states");
            trace.AppendLine(" slot="+slot+" body="+body.GetEntityId()+" position="+body.position.ToString("F4")+" forward="+body.transform.forward.ToString("F4")+" velocity="+body.linearVelocity.ToString("F4")+" command="+Get(truck,"command")+" actualNwhThrottle="+Get(state,"throttle")+" actualNwhBrakes="+Get(state,"brakes")+" actualNwhGrounded="+Get(motor,"Grounded")+" finalBay="+Call(match,"UsingBayParking",slot)+" status="+Call(match,"BayParkingStatus",slot)+" stage="+Rules.Cargo[slot].Stage+" score="+Rules.Cargo[slot].Score+" crane="+Get(Get(carrier,"crane"),"Stage")+" originalCargo="+((Rigidbody)Get(cargo,"body")).GetEntityId()+" restraint="+(Get(cargo,"Restraint") is Joint));
            DragonEggPhysicsObservation.Append(match,slot,trace);
            trace.AppendLine(" YIELD slot="+slot+" starts="+Call(match,"BayYieldStarts",slot)+" active="+Call(match,"BayYieldActive",slot)+" separatingSteps="+Call(match,"BayYieldSeparatingSteps",slot));
        }
        File.WriteAllText(Path.Combine(output,"autonomous-clear-start-encounter.txt"),trace.ToString());
    }
    void Capture(string name)
    {
        Runtime("LWS.TruckSports.SportsControlledCapture").GetMethods().Single(method=>method.Name=="Save"&&method.GetParameters().Length==5).Invoke(null,new object[]{match,1280,720,Path.Combine(output,name+".png"),null});
    }
    [UnityTest,Timeout(150000)]
    public IEnumerator AutonomousOpposedBayApproachesFromClearStartsMustPhysicallySeparateAndUnloadBothOriginalCargo()
    {
        Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath),Is.Not.Null);
        var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");output=Path.GetFullPath(Path.Combine(at>=0&&at+1<args.Length?args[at+1]:Application.persistentDataPath,"EggBayEncounter",DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff")));Directory.CreateDirectory(output);
        preferences=(IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"EggBayEncounter"});Runtime("LWS.TruckSports.SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);
        isolation=new GameObject("Egg encounter physical input isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("LWS.TruckSports.SportsProbeInputIsolation"));
        Time.timeScale=1;EditorSceneManager.LoadSceneInPlayMode(ScenePath,new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
        match=UnityEngine.Object.FindAnyObjectByType(Runtime("LWS.TruckDragonEggDelivery.DragonEggDeliveryMatch"));Assert.That(match,Is.Not.Null);
        isolation.AddComponent<DragonEggJointTransitionObservation>().Configure(match,Path.Combine(output,"actual-joint-transitions-before-physx.txt"));
        Get(match,"setup").GetType().GetField("count").SetValue(Get(match,"setup"),4);match.GetType().GetField("option").SetValue(match,2);match.GetType().GetField("fixedSeed").SetValue(match,812);Call(match,"AllBots");ConfigureClearEncounter();
        Assert.That(Call(match,"StartMatch"),Is.EqualTo(true));Assert.That(Get(match,"AllAI"),Is.EqualTo(true));
        for(int slot=0;slot<4;slot++)Assert.That(Vector3.ProjectOnPlane(Body(slot).position-requested[slot],Vector3.up).magnitude,Is.LessThan(.75f),"Original full-body-clear start must accept each actual marker; no fallback location or start-gate bypass.");
        Assert.That(Physics.ComputePenetration(Shape(0),Shape(0).transform.position,Shape(0).transform.rotation,Shape(2),Shape(2).transform.position,Shape(2).transform.rotation,out _,out _),Is.False,"Legitimately clear initial NWH shapes; no injected contact/overlap.");
        var original=Enumerable.Range(0,4).Select(slot=>(Rigidbody)Get(Get(Carrier(slot),"cargo"),"body")).ToArray();Observe("ordinary-original-start");
        float support=Time.realtimeSinceStartup+35;while((float)Get(match,"Intro")>0&&Time.realtimeSinceStartup<support)yield return null;
        Assert.That((float)Get(match,"Intro"),Is.Zero);Observe("normal-speed-grounded-go");Capture("clear-supported-opposed-start");
        Time.timeScale=4;TestContext.WriteLine("CONTROLLED AUTONOMOUS ENCOUNTER: four actual NWH crews; clear initial markers0/2 are6.8m apart facing opposed real bays.4xsim after ordinary supported countdown. No pose, velocity, force, score, callback or contact injection. This is not full15minute journey acceptance.");
        int blockedFrames=0;bool captured=false,yieldCaptured=false,actualYieldReverse=false;float deadline=Time.realtimeSinceStartup+95,next=Time.realtimeSinceStartup;
        while(Time.realtimeSinceStartup<deadline&&!(Rules.Cargo[0].Stage==DragonEggStage.Delivered&&Rules.Cargo[2].Stage==DragonEggStage.Delivered)&&!(bool)Get(match,"Finished"))
        {
            bool blocked=new[]{0,2}.Any(slot=>(bool)Call(match,"UsingBayParking",slot)&&((string)Call(match,"BayParkingStatus",slot)??"").Contains("ACTUAL BODY")&&OpposedTruckActuallyBlocks(slot));
            if(blocked){blockedFrames++;if(!captured){captured=true;Observe("first-genuine-body-block");Capture("actual-blocked-encounter");}}
            foreach(int slot in new[]{0,2})actualYieldReverse|=(bool)Call(match,"BayYieldActive",slot)&&Vector3.Dot(Body(slot).linearVelocity,Body(slot).transform.forward)<-.35f;
            if(!yieldCaptured&&new[]{0,2}.Any(slot=>(int)Call(match,"BayYieldStarts",slot)>0)){yieldCaptured=true;Observe("first-actual-yield");Capture("actual-supported-yield");}
            if(Time.realtimeSinceStartup>=next){Observe("ordinary-ai-progress");next=Time.realtimeSinceStartup+1;}yield return null;
        }
        Observe("result-or-deadlock");Capture("actual-encounter-result");TestContext.WriteLine("Actual body-blocked frames="+blockedFrames+"; evidence="+output);
        Assert.That(blockedFrames,Is.GreaterThan(0),"The hold must have the actual opposed NWH crew0/2 in its real footprint/cast; a shell fragment or unrelated prop cannot satisfy this encounter.");
        Assert.That(yieldCaptured,Is.True);Assert.That(actualYieldReverse,Is.True,"The designated yielding original NWH body must genuinely reverse; a timer or bookkeeping transition cannot clear the encounter.");
        foreach(int slot in new[]{0,2}){Assert.That(Rules.Cargo[slot].Stage,Is.EqualTo(DragonEggStage.Delivered),"Opposed original crew must physically clear the encounter and unload through its own crane at its correct nest.");Assert.That(Rules.Cargo[slot].Score,Is.GreaterThan(0));Assert.That(Get(Get(Carrier(slot),"cargo"),"body"),Is.SameAs(original[slot]));}
        var jointWitness=isolation.GetComponent<DragonEggJointTransitionObservation>();Assert.That(jointWitness.UnsafeDisconnectedDriveSteps,Is.Zero);Assert.That(jointWitness.InertReleasedJointSteps,Is.GreaterThan(0));Assert.That(jointWitness.ActualGripHandovers,Is.GreaterThanOrEqualTo(2));Assert.That(jointWitness.MaximumHandoverAnchorError,Is.LessThan(.02f));
        Assert.That(jointWitness.ActualPrematureGripLosses,Is.Zero);
    }
    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        if(match is UnityEngine.Object item&&item!=null)Call(match,"SetupScreen");Time.timeScale=1;
        var previous=SceneManager.GetActiveScene();var empty=SceneManager.CreateScene("Egg encounter cleanup "+Guid.NewGuid().ToString("N"));SceneManager.SetActiveScene(empty);if(previous.IsValid()&&previous.isLoaded)yield return SceneManager.UnloadSceneAsync(previous);
        if(isolation!=null)UnityEngine.Object.DestroyImmediate(isolation);preferences?.Dispose();preferences=null;Runtime("LWS.TruckSports.SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);match=null;
    }
}
#endif
