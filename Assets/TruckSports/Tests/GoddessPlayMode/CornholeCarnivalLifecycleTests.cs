#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class CornholeCarnivalLifecycleTests
{
    const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static Type Runtime(string name)=>Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
    static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
    static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Any).SetValue(owner,value);
    static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethods(Any).Single(method=>method.Name==name&&method.GetParameters().Length==args.Length&&method.GetParameters().Select((p,i)=>args[i]==null||p.ParameterType.IsInstanceOfType(args[i])).All(v=>v)).Invoke(owner,args);
    static object At(object owner,string field,int index)=>((Array)Get(owner,field)).GetValue(index);
    static Rigidbody Body(object truck)=>(Rigidbody)Get(truck,"body");
    static void Seed(object truck,Vector3 position,Quaternion rotation,Vector3 velocity)
    {
        var body=Body(truck);body.position=position;body.rotation=rotation;((Component)truck).transform.SetPositionAndRotation(position,rotation);body.linearVelocity=velocity;body.angularVelocity=Vector3.zero;Physics.SyncTransforms();
    }
    static IDisposable Preferences(string key)
    {
        var prefs=(IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{key});Runtime("SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);return prefs;
    }
    static GameObject Isolation(){var go=new GameObject("Explicit bounded Cornhole carnival native fixtures");UnityEngine.Object.DontDestroyOnLoad(go);go.AddComponent(Runtime("SportsProbeInputIsolation"));return go;}
    static IEnumerator Load(){EditorSceneManager.LoadSceneInPlayMode("Assets/TruckCornhole/Scenes/TruckCornhole.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;}
    static Component Match()=>(Component)UnityEngine.Object.FindAnyObjectByType(Type.GetType("LWS.TruckCornhole.CornholeMatch, Assembly-CSharp",true));
    static void Start(Component match,int count)
    {
        Set(Get(match,"setup"),"count",count);Call(match,"AllAI");var presets=Get(match,"weatherPresets");var clear=((Array)Get(presets,"entries")).Cast<object>().FirstOrDefault(entry=>(string)Get(entry,"displayName")=="Clear");if(clear!=null)Call(presets,"Select",Get(clear,"id"));Assert.That(Call(match,"StartMatch"),Is.True);Time.timeScale=1;
    }
    static IEnumerator Launched(object truck,float seconds=20)
    {
        float until=Time.realtimeSinceStartup+seconds;while(!(bool)Get(truck,"Launched")&&Time.realtimeSinceStartup<until)yield return new WaitForFixedUpdate();Assert.That(Get(truck,"Launched"),Is.True,"Original native ramp/trampoline must produce a launch; no synthetic Launched flag.");Assert.That(Get(truck,"LaunchCount"),Is.EqualTo(1));
    }

    [UnityTest,Timeout(180000)]
    public IEnumerator OriginalConcurrentLaunchesIgnoreAirbornePairsThenDeferOverlapAndKeepNativeLandingImpactsEnabled()
    {
        var prefs=Preferences("CarnivalCornholeCollision");var isolation=Isolation();Component match=null;SimulationMode prior=Physics.simulationMode;object first=null,second=null;
        try
        {
            yield return Load();match=Match();Start(match,4);var courses=((Array)Get(match,"courses")).Cast<object>().ToArray();var initial=courses.Select(course=>Get(course,"Current")).ToArray();
            Assert.That(initial.All(truck=>((Component)truck).gameObject.activeSelf),Is.True);Assert.That(courses.All(course=>(int)Get(course,"Attempts")==1),Is.True);var policy=Get(match,"landingCollisions");
            for(int i=0;i<initial.Length;i++)for(int j=i+1;j<initial.Length;j++)Assert.That(Physics.GetIgnoreCollision((Collider)Get(initial[i],"shape"),(Collider)Get(initial[j],"shape")),Is.True);
            foreach(var truck in initial)yield return Launched(truck);Assert.That(initial.All(truck=>(int)Get(truck,"LaunchCount")==1),Is.True);
            // Controlled same-course physical contact fixture after observed
            // original launches. No score/holed/landed flags are supplied.
            Call(match,"ChangeSetup");Start(match,1);var own=courses[0];first=Get(own,"Current");yield return Launched(first);((Behaviour)match).enabled=false;foreach(var course in courses)((Behaviour)course).enabled=false;var board=(Transform)Get(own,"board");Set(first,"command",Vector2.zero);
            Seed(first,board.TransformPoint(new Vector3(3,.8f,0)),board.rotation,Vector3.zero);float until=Time.realtimeSinceStartup+8;
            while(!(bool)Get(first,"Landed")&&Time.realtimeSinceStartup<until){yield return new WaitForFixedUpdate();Call(policy,"Tick");}
            Assert.That(Get(first,"Landed"),Is.True);Assert.That(Get(first,"Supported"),Is.True);
            // Explicit next-delivery fixture uses the existing pool's Spawn;
            // actual native trampoline contact must still launch that body.
            Call(own,"Spawn");second=Get(own,"Current");Set(second,"command",Vector2.zero);Seed(second,(Vector3)Call(own,"ApproachPoint",new Vector3(0,.55f,1.1f)),(Quaternion)Get(own,"ApproachRotation"),(Vector3)Call(own,"ApproachDirection",Vector3.forward*13));Call(policy,"Tick");yield return Launched(second,4);
            Assert.That(Physics.GetIgnoreCollision((Collider)Get(first,"shape"),(Collider)Get(second,"shape")),Is.True);
            Time.timeScale=0;Physics.simulationMode=SimulationMode.Script;Body(first).useGravity=Body(second).useGravity=false;Set(match,"windDrag",0f);Set(first,"command",Vector2.zero);Set(second,"command",Vector2.zero);
            var high=board.TransformPoint(new Vector3(3,12,0));Seed(first,high,board.rotation,Vector3.zero);Seed(second,high,board.rotation,Vector3.zero);Call(policy,"Tick");
            int impacts=(int)Get(first,"TruckImpacts")+(int)Get(second,"TruckImpacts");for(int i=0;i<5;i++){Call(policy,"Tick");Physics.Simulate(.02f);}
            Assert.That(Physics.GetIgnoreCollision((Collider)Get(first,"shape"),(Collider)Get(second,"shape")),Is.True);Assert.That((int)Get(first,"TruckImpacts")+(int)Get(second,"TruckImpacts"),Is.EqualTo(impacts));Assert.That(Body(first).linearVelocity.magnitude+Body(second).linearVelocity.magnitude,Is.LessThan(.001f));
            var near=board.TransformPoint(new Vector3(3,1.5f,0));var down=board.TransformDirection(Vector3.down*.1f);Seed(first,near,board.rotation,down);Seed(second,near,board.rotation,down);var p=Body(second).position;var v=Body(second).linearVelocity;Call(policy,"Tick");
            Assert.That(Get(second,"LandingCollisionReady"),Is.True);Assert.That(Get(policy,"PendingOverlaps"),Is.GreaterThan(0));Assert.That(Physics.GetIgnoreCollision((Collider)Get(first,"shape"),(Collider)Get(second,"shape")),Is.True);Assert.That(Body(second).position,Is.EqualTo(p));Assert.That(Body(second).linearVelocity,Is.EqualTo(v),"Collision eligibility cannot teleport or force bodies apart.");
            var right=board.TransformDirection(Vector3.right);Body(first).linearVelocity=-right*1.5f;Body(second).linearVelocity=right*1.5f;
            for(int i=0;i<60&&Physics.GetIgnoreCollision((Collider)Get(first,"shape"),(Collider)Get(second,"shape"));i++){Call(policy,"Tick");Physics.Simulate(.02f);}
            Assert.That(Physics.GetIgnoreCollision((Collider)Get(first,"shape"),(Collider)Get(second,"shape")),Is.False,"Only actual separation may activate the pending pair.");Assert.That(Get(policy,"ActivePairs"),Is.GreaterThan(0));
            Body(first).linearVelocity=right*2;Body(second).linearVelocity=-right*2;impacts=(int)Get(first,"TruckImpacts")+(int)Get(second,"TruckImpacts");
            for(int i=0;i<80&&(int)Get(first,"TruckImpacts")+(int)Get(second,"TruckImpacts")==impacts;i++){Call(policy,"Tick");Physics.Simulate(.02f);}
            Assert.That((int)Get(first,"TruckImpacts")+(int)Get(second,"TruckImpacts"),Is.GreaterThan(impacts));Call(policy,"Tick");Assert.That(Physics.GetIgnoreCollision((Collider)Get(first,"shape"),(Collider)Get(second,"shape")),Is.False,"Activated pairs remain physical during native contacts and pileups.");Assert.That(Body(first).linearVelocity.magnitude,Is.LessThan(8));Assert.That(Body(second).linearVelocity.magnitude,Is.LessThan(8));Assert.That(Get(Get(match,"ScoreJournal"),"Count"),Is.Zero);
            Body(first).useGravity=Body(second).useGravity=true;Physics.simulationMode=prior;Time.timeScale=1;string id=(string)Get(match,"MatchId");Call(match,"ChangeSetup");Assert.That(Call(match,"StartMatch"),Is.True);Time.timeScale=0;Assert.That(Get(match,"MatchId"),Is.Not.EqualTo(id));Assert.That(Get(Get(match,"ScoreJournal"),"Count"),Is.Zero);Assert.That(Get(Get(own,"Current"),"LandingCollisionReady"),Is.False);
            TestContext.WriteLine("CONTROLLED_CORNHOLE_COLLISION original four concurrent native launches, original same-course board landing and second native trampoline contact; supplied overlap/velocities with gravity disabled for isolated pair integration, no policy pose/force changes, ignored initial air contact, pending board overlap, native separation then real impact, enabled contact retained and fresh rematch. Saved courses remain isolated; no shared-board/full-match/human acceptance.");
        }
        finally
        {
            Physics.simulationMode=prior;Time.timeScale=1;if(first!=null)Body(first).useGravity=true;if(second!=null)Body(second).useGravity=true;if(match!=null){((Behaviour)match).enabled=true;foreach(var course in ((Array)Get(match,"courses")).Cast<Behaviour>())course.enabled=true;Call(match,"ChangeSetup");}UnityEngine.Object.DestroyImmediate(isolation);prefs.Dispose();Runtime("SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);
        }
    }

    [UnityTest,Timeout(180000)]
    public IEnumerator NativeWholeHoleAndLowerGroundMissStayAtTheirActualFinalPosesUntilRoundClear()
    {
        var prefs=Preferences("CarnivalCornholeRetainedBodies");var isolation=Isolation();Component match=null;
        try
        {
            yield return Load();match=Match();Start(match,2);var course0=At(match,"courses",0);var course1=At(match,"courses",1);var holed=Get(course0,"Current");var missed=Get(course1,"Current");yield return Launched(holed);yield return Launched(missed);((Behaviour)match).enabled=false;Set(holed,"command",Vector2.zero);Set(missed,"command",Vector2.zero);
            var board0=(Transform)Get(course0,"board");var board1=(Transform)Get(course1,"board");
            Seed(holed,board0.TransformPoint(new Vector3(0,3,(float)Get(course0,"holeZ"))),board0.rotation,board0.TransformDirection(Vector3.down*8));Call(holed,"ObserveHole");Seed(missed,board1.TransformPoint(new Vector3(10,1,0)),board1.rotation,Vector3.zero);Call(missed,"ObserveHole");
            var policy=Get(match,"landingCollisions");float until=Time.realtimeSinceStartup+15;
            while((!(bool)Get(holed,"Holed")||!(bool)Get(missed,"Excluded")||(float)Get(missed,"GroundDuration")<1.3f)&&Time.realtimeSinceStartup<until){yield return new WaitForFixedUpdate();Call(policy,"Tick");}
            Assert.That(Get(holed,"Holed"),Is.True);Assert.That(Get(holed,"Points"),Is.EqualTo(3));Assert.That(((Component)holed).gameObject.activeSelf,Is.True);Assert.That(Body(holed).isKinematic,Is.True);Assert.That(Body(holed).detectCollisions,Is.False);Assert.That(((Collider)Get(holed,"shape")).enabled,Is.False);var finalHolePose=Body(holed).position;
            Assert.That(Get(missed,"Grounded"),Is.True);Assert.That(Get(missed,"Excluded"),Is.True);Assert.That(Get(missed,"Points"),Is.Zero);Assert.That(((Component)missed).gameObject.activeSelf,Is.True);Assert.That(Body(missed).isKinematic,Is.False);Assert.That(((Collider)Get(missed,"shape")).enabled,Is.True);Assert.That(Body(missed).position.y,Is.LessThan(-7));
            float stop=Time.time+4;while(Time.time<stop){yield return new WaitForFixedUpdate();Call(policy,"Tick");Call(holed,"ObserveHole");}
            Assert.That(((Component)holed).gameObject.activeSelf&&((Component)missed).gameObject.activeSelf,Is.True);Assert.That(Body(holed).position,Is.EqualTo(finalHolePose));Assert.That(Get(holed,"Points"),Is.EqualTo(3));Assert.That(Get(missed,"Points"),Is.Zero);Assert.That(((Renderer[])Get(holed,"Visuals")).Any(renderer=>renderer.enabled),Is.True);Assert.That(Get(Get(match,"ScoreJournal"),"Count"),Is.Zero,"Provisional retained bodies cannot bank points before the original whole-round finalizer.");
            Call(course0,"Clear");Call(course1,"Clear");Assert.That(((Array)Get(course0,"trucks")).Cast<Component>().All(truck=>!truck.gameObject.activeSelf),Is.True);Assert.That(((Array)Get(course1,"trucks")).Cast<Component>().All(truck=>!truck.gameObject.activeSelf),Is.True);Call(course0,"BeginRound");Assert.That(Get(Get(course0,"Current"),"Holed"),Is.False);Assert.That(Get(Get(course0,"Current"),"Points"),Is.Zero);
            var resetPose=Body(Get(course0,"Current")).position;var startPose=(Vector3)Get(course0,"StartPosition");var resetDelta=resetPose-startPose;
            TestContext.WriteLine("CORNHOLE_RESET_POSE actual=("+resetPose.x.ToString("R")+","+resetPose.y.ToString("R")+","+resetPose.z.ToString("R")+") expected=("+startPose.x.ToString("R")+","+startPose.y.ToString("R")+","+startPose.z.ToString("R")+") delta=("+resetDelta.x.ToString("R")+","+resetDelta.y.ToString("R")+","+resetDelta.z.ToString("R")+") magnitude="+resetDelta.magnitude.ToString("R"));
            Assert.That(resetDelta.magnitude,Is.LessThanOrEqualTo(.0001f),"Only the new round's native Rigidbody/Transform spawn roundtrip permits sub-millimetre storage precision; retained holed pose above remains exact.");
            TestContext.WriteLine("CONTROLLED_CORNHOLE_RETENTION original native trampoline launches then explicit above-hole/off-board drop initial poses; real whole-body downward passage scores3 once, real catch-ground miss scores0, both remain active until explicit proper round Clear, holed actual pose frozen and missed body physical; original provisional bank boundary unchanged. Not full round/result or human/visibility acceptance.");
        }
        finally{Time.timeScale=1;if(match!=null){((Behaviour)match).enabled=true;Call(match,"ChangeSetup");}UnityEngine.Object.DestroyImmediate(isolation);prefs.Dispose();Runtime("SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);}
    }

    [UnityTest,Timeout(120000)]
    public IEnumerator EffectiveHeadwindIsTenMphForNativeFlightAiPredictionAndExistingWindDisplayOnly()
    {
        var prefs=Preferences("CarnivalCornholeEffectiveWind");var isolation=Isolation();Component match=null;SimulationMode prior=Physics.simulationMode;object truck=null;
        try
        {
            yield return Load();match=Match();Start(match,1);var course=At(match,"courses",0);truck=Get(course,"Current");yield return Launched(truck);((Behaviour)match).enabled=false;((Behaviour)course).enabled=false;Time.timeScale=0;Physics.simulationMode=SimulationMode.Script;Body(truck).useGravity=false;Set(truck,"command",Vector2.zero);var atmosphere=Get(match,"atmosphere");var flowField=atmosphere.GetType().GetField("<Flow>k__BackingField",Any);var heading=(Vector3)Get(truck,"LaunchHeading");var raw=-heading*30;flowField.SetValue(atmosphere,raw);
            var effective=(Vector3)Call(match,"EffectiveWind",heading);Assert.That(-Vector3.Dot(effective,heading),Is.EqualTo(4.4704f).Within(.00002f));Assert.That((Vector3)Get(atmosphere,"Flow"),Is.EqualTo(raw),"Mode cap must not modify Weather Maker/shared weather authority.");
            Seed(truck,new Vector3((float)Get(course,"center"),50,30),Quaternion.identity,Vector3.zero);var acceleration=(Vector3)Call(match,"FlightWindFor",truck,Vector3.zero);Call(truck,"FixedUpdate");Physics.Simulate(.02f);Assert.That(Vector3.Distance(Body(truck).linearVelocity,acceleration*.02f),Is.LessThan(.0002f),"The actual original Rigidbody must integrate the capped acceleration.");var strong=Body(truck).linearVelocity;
            flowField.SetValue(atmosphere,effective);Seed(truck,new Vector3((float)Get(course,"center"),50,30),Quaternion.identity,Vector3.zero);Call(truck,"FixedUpdate");Physics.Simulate(.02f);Assert.That(Vector3.Distance(Body(truck).linearVelocity,strong),Is.LessThan(.0002f));
            Set(course,"elapsed",1f);var board=(Transform)Get(course,"board");Seed(truck,new Vector3((float)Get(course,"center"),10,board.position.z),Quaternion.identity,Vector3.zero);flowField.SetValue(atmosphere,raw);var strongPlan=(Vector2)Call(course,"Plan");flowField.SetValue(atmosphere,effective);Assert.That((Vector2)Call(course,"Plan"),Is.EqualTo(strongPlan));
            var cross=Vector3.Cross(Vector3.up,heading);var mixed=cross*17-heading*30;flowField.SetValue(atmosphere,mixed);var mixedEffective=(Vector3)Call(match,"EffectiveWind",heading);Assert.That(Vector3.Dot(mixedEffective,cross),Is.EqualTo(17).Within(.0001f));Assert.That(Vector3.Dot(mixedEffective,heading),Is.EqualTo(-4.4704f).Within(.0001f));flowField.SetValue(atmosphere,heading*30);Assert.That((Vector3)Call(match,"EffectiveWind",heading),Is.EqualTo(heading*30));flowField.SetValue(atmosphere,Vector3.left*30);Assert.That(Vector3.Distance((Vector3)Call(match,"EffectiveWind",Vector3.right),Vector3.left*4.4704f),Is.LessThan(.0001f));
            flowField.SetValue(atmosphere,raw);var display=((Component)Get(match,"menus")).GetComponent(Type.GetType("LWS.TruckCornhole.CornholeWindDisplay, Assembly-CSharp",true));Set(display,"next",0f);Call(display,"LateUpdate");var speed=At(display,"speeds",0);StringAssert.Contains("10.0 MPH",(string)Get(speed,"text"));Assert.That((Vector3)Get(atmosphere,"Flow"),Is.EqualTo(raw));Assert.That(Get(Get(match,"ScoreJournal"),"Count"),Is.Zero);
            Body(truck).useGravity=true;TestContext.WriteLine("CONTROLLED_CORNHOLE_WIND actual native trampoline launch then supplied constant head/cross/tail wind vectors, direct source-force/native Rigidbody integration at50m, same capped AI prediction at supplied clock/pose, existing bottom-right display reads10.0MPH; cap only opposing component, unchanged raw shared Flow. Not natural forecast/full-match/human acceptance.");
        }
        finally{Physics.simulationMode=prior;Time.timeScale=1;if(truck!=null)Body(truck).useGravity=true;if(match!=null){((Behaviour)match).enabled=true;foreach(var course in ((Array)Get(match,"courses")).Cast<Behaviour>())course.enabled=true;Call(match,"ChangeSetup");}UnityEngine.Object.DestroyImmediate(isolation);prefs.Dispose();Runtime("SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);}
    }
}
#endif
