#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LWS.TruckBakeOff.Tests
{
    public sealed class BakePouringLifecycleTests
    {
        const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        static Type Sports(string name)=>Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
        static object Read(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
        static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethods(Any).Single(method=>method.Name==name&&method.GetParameters().Length==args.Length).Invoke(owner,args);
        static Rigidbody Body(object owner)=>(Rigidbody)Read(owner,"body");
        sealed class Natural:IDisposable
        {
            readonly IDisposable preferences;
            readonly InputSettings previous,settings;
            readonly GameObject isolation;
            readonly StringBuilder trace=new StringBuilder();
            readonly string path;
            float next;
            public Behaviour match;
            public object rules;
            public object[] arms,ingredients,bowls,pans;
            public float Clock=>(float)Read(rules,"Clock");
            public float[,] Measured=>(float[,])Read(rules,"measured");
            public Natural()
            {
                preferences=(IDisposable)Sports("SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"BakeRealPour"+Guid.NewGuid().ToString("N")});Sports("SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);
                previous=InputSystem.settings;settings=UnityEngine.Object.Instantiate(previous);settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=settings;
                isolation=new GameObject("Bake genuine finite-pour native input isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Sports("SportsProbeInputIsolation"));
                path=Path.GetFullPath("Logs/TruckSports/PlaytestRepair/BakeSafeLoadedPanCarryNativeV21/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")+".txt");Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            public IEnumerator Load(int? optionOverride=null)
            {
                const string scene="Assets/TruckBakeOff/Scenes/TruckBakeOff.unity";Assert.That(File.Exists(scene),Is.True);EditorSceneManager.LoadSceneInPlayMode(scene,new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
                match=UnityEngine.Object.FindObjectsByType(Type.GetType("LWS.TruckBakeOff.BakeMatch, Assembly-CSharp",true)).Cast<Behaviour>().Single();Call(match,"SetupScreen");if(optionOverride.HasValue)match.GetType().GetField("option",Any).SetValue(match,optionOverride.Value);var setup=Read(match,"setup");setup.GetType().GetField("count").SetValue(setup,4);Call(match,"NormalizeSetup");Call(match,"AllBots");Assert.That((bool)Call(match,"StartMatch"),Is.True);
                rules=Read(match,"rules");arms=((Array)Read(match,"arms")).Cast<object>().ToArray();ingredients=((Array)Read(match,"ingredients")).Cast<object>().ToArray();bowls=((Array)Read(match,"bowls")).Cast<object>().ToArray();pans=((Array)Read(match,"pans")).Cast<object>().ToArray();Assert.That(arms.Length,Is.EqualTo(4));Assert.That(ingredients.Length,Is.EqualTo(20));
            }
            public void Observe(bool force=false)
            {
                if(!force&&Clock<next)return;next=Clock+.5f;var tasks=(int[])Read(match,"task");var stocks=(int[])Read(match,"stockId");
                for(int owner=0;owner<4;owner++)
                {
                    var arm=arms[owner];var truck=Read(arm,"truck");var grip=(Rigidbody)Read(arm,"grip");var stock=ingredients[stocks[owner]];var spout=(Transform)Read(stock,"spout");var stream=(LineRenderer)Read(stock,"stream");var opening=BowlOpening(this,owner);var measured=Measured;Assert.That(spout!=null||!(bool)Read(stock,"pourable")&&Read(stock,"contents").ToString()=="Egg",Is.True,"Only the original non-pourable egg may intentionally have no spout.");
                    var basePoint=((Component)truck).transform.TransformPoint((Vector3)Read(arm,"mount"));var heldOffset=Body(stock).position-grip.position;var originalSpawn=(Vector3)Read(stock,"spawn");float top=((Collider[])Read(stock,"physical")).Where(shape=>shape!=null&&shape.enabled).Select(shape=>shape.bounds.max.y).DefaultIfEmpty(Body(stock).position.y).Max();
                    var requestedPickup=(Vector3)match.GetType().GetMethod("SourcePickupPoint",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new[]{stock});var requestedReturnGrip=originalSpawn+Vector3.up*.2f-heldOffset;
                    var raised=((Component)truck).transform.TransformPoint(new Vector3(0,6.2f,4));raised.y=Mathf.Max(raised.y,requestedPickup.y+.25f);var nativeCandidates=(System.Collections.IDictionary)Read(arm,"candidates");float candidateAge=nativeCandidates.Contains(stock)?(float)Read(arm,"clock")-(float)nativeCandidates[stock]:float.PositiveInfinity;
                    var vehicle=Read(Read(truck,"tractorMotor"),"vehicle");var states=Read(Read(vehicle,"input"),"states");
                    trace.AppendLine("BAKE_ACTUAL_PICKUP_GATE clock="+Clock.ToString("R")+" owner="+owner+" sourceRaised="+((bool[])Read(match,"sourceRaised"))[owner]+" raisedTarget="+raised.ToString("F5")+" raisedError="+Vector3.Distance(grip.position,raised).ToString("R")+" pickupError="+Vector3.Distance(grip.position,requestedPickup).ToString("R")+" actualGripSpeed="+grip.linearVelocity.magnitude.ToString("R")+" nativeGrabSquaredDistance="+(Body(stock).position-grip.position).sqrMagnitude.ToString("R")+" nativeGrabExclusiveSquaredLimit=9 candidateAge="+candidateAge.ToString("R")+" nativeSensorOverlap="+ActualSensorContact(arm,stock)+" pickupAllowed="+Read(stock,"PickupAllowed")+" sessionLive="+Read(match,"Live")+" truckSpeed="+Body(truck).linearVelocity.magnitude.ToString("R")+" nativeHandbrake="+Read(states,"handbrake"));
                    trace.AppendLine("BAKE_ACTUAL_SUPPLY_REACH clock="+Clock.ToString("R")+" owner="+owner+" task="+tasks[owner]+" pickupRequestedGrip="+requestedPickup.ToString("F5")+" pickupRequestedReach="+Vector3.Distance(basePoint,requestedPickup).ToString("R")+" returnRequestedGrip="+requestedReturnGrip.ToString("F5")+" returnRequestedReach="+Vector3.Distance(basePoint,requestedReturnGrip).ToString("R")+" actualReturnBodyError="+Vector3.Distance(Body(stock).position,originalSpawn+Vector3.up*.2f).ToString("R")+" actualOriginalTruckEnvelope="+((Collider)Read(truck,"shape")).bounds.ToString("F5"));
                    trace.AppendLine("BAKE_ACTUAL_FINITE_POUR clock="+Clock.ToString("R")+" owner="+owner+" task="+tasks[owner]+" stage="+((Array)Read(rules,"stages")).GetValue(owner)+" stock="+Read(stock,"id")+" held="+ReferenceEquals(Read(arm,"Held"),stock)+" originalBody="+Body(truck).position.ToString("F5")+" armBase="+((Component)truck).transform.TransformPoint((Vector3)Read(arm,"mount")).ToString("F5")+" grip="+grip.position.ToString("F5")+" worldGoal="+((Vector3)Read(arm,"WorldGoal")).ToString("F5")+" nativeCandidates="+Read(arm,"NativeCandidates")+" actualSourceColliderTop="+((Collider[])Read(stock,"physical")).Where(shape=>shape!=null&&shape.enabled).Select(shape=>shape.bounds.max.y).DefaultIfEmpty(Body(stock).position.y).Max()+" tilt="+Read(arm,"Tilt")+" stockBody="+Body(stock).position.ToString("F5")+" stockVelocity="+Body(stock).linearVelocity.ToString("F5")+" stockRotation="+Body(stock).rotation.eulerAngles.ToString("F4")+" actualSpout="+(spout!=null?spout.position.ToString("F5"):"NON_POURABLE_EGG")+" actualForward="+(spout!=null?spout.forward.ToString("F5"):"NON_POURABLE_EGG")+" sourceVolume="+Read(stock,"Volume")+" sourceSpilled="+Read(stock,"SpilledVolume")+" sourceNativePours="+Read(stock,"NativePours")+" bowlBody="+Body(Read(bowls[owner],"vessel")).position.ToString("F5")+" aperture="+opening.bounds.ToString("F5")+" actualStreamEnd="+(stream.enabled&&stream.positionCount>0?stream.GetPosition(stream.positionCount-1).ToString("F5"):"NONE")+" measured="+string.Join(",",Enumerable.Range(0,4).Select(kind=>measured[owner,kind].ToString("R")))+" mix="+((float[])Read(rules,"mixSeconds"))[owner]+" pan="+Read(Read(pans[owner],"vessel"),"Volume"));
                    var bowlVessel=Read(bowls[owner],"vessel");var panVessel=Read(pans[owner],"vessel");var bowlPhysical=((Collider[])Read(bowlVessel,"physical")).Where(shape=>shape!=null&&shape.enabled).ToArray();var panPhysical=((Collider[])Read(panVessel,"physical")).Where(shape=>shape!=null&&shape.enabled).ToArray();var desiredBowl=(Vector3)match.GetType().GetMethod("PourBodyTarget",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new[]{panVessel});var desiredGrip=desiredBowl-(Body(bowlVessel).position-grip.position);
                    var bowlForward=Vector3.ProjectOnPlane(((Transform)Read(bowlVessel,"spout")).forward,Vector3.up).normalized;var pourAxis=Vector3.Cross(Vector3.up,bowlForward);var neutralBowlOffset=Quaternion.AngleAxis(-(float)Read(arm,"Tilt"),pourAxis)*(Body(bowlVessel).position-grip.position);var planned35Grip=desiredBowl-Quaternion.AngleAxis(35,pourAxis)*neutralBowlOffset;var mixer=(Vector3)match.GetType().GetMethod("Mixer",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{owner});
                    trace.AppendLine("BAKE_ACTUAL_BOWL_WORKSPACE clock="+Clock.ToString("R")+" owner="+owner+" task="+tasks[owner]+" originalMixer="+mixer.ToString("F5")+" actualHorizontalParkingError="+Vector3.ProjectOnPlane(Body(truck).position-mixer,Vector3.up).magnitude.ToString("R")+" neutralRetainedOffset="+neutralBowlOffset.ToString("F5")+" planned35Grip="+planned35Grip.ToString("F5")+" planned35Reach="+Vector3.Distance(basePoint,planned35Grip).ToString("R")+" fixedNativeArmCap=8");
                    trace.AppendLine("BAKE_ACTUAL_BATTER_TRANSFER clock="+Clock.ToString("R")+" owner="+owner+" task="+tasks[owner]+" actualHeldBowl="+ReferenceEquals(Read(arm,"Held"),bowlVessel)+" bowlBody="+Body(bowlVessel).position.ToString("F5")+" bowlVelocity="+Body(bowlVessel).linearVelocity.ToString("F5")+" bowlBottom="+bowlPhysical.Select(shape=>shape.bounds.min.y).DefaultIfEmpty(Body(bowlVessel).position.y).Min().ToString("R")+" bowlVolume="+Read(bowlVessel,"Volume")+" bowlSpilled="+Read(bowlVessel,"SpilledVolume")+" bowlNativePours="+Read(bowlVessel,"NativePours")+" panBody="+Body(panVessel).position.ToString("F5")+" panVelocity="+Body(panVessel).linearVelocity.ToString("F5")+" panEuler="+Body(panVessel).rotation.eulerAngles.ToString("F4")+" panDisplacement="+Vector3.Distance(Body(panVessel).position,(Vector3)Read(panVessel,"spawn")).ToString("R")+" panTop="+panPhysical.Select(shape=>shape.bounds.max.y).DefaultIfEmpty(Body(panVessel).position.y).Max().ToString("R")+" desiredBowl="+desiredBowl.ToString("F5")+" desiredGrip="+desiredGrip.ToString("F5")+" desiredReach="+Vector3.Distance(basePoint,desiredGrip).ToString("R"));
                    for(int offset=0;offset<5;offset++)
                    {
                        var finite=ingredients[owner*5+offset];
                        trace.AppendLine("BAKE_ORIGINAL_STOCK clock="+Clock.ToString("R")+" owner="+owner+" id="+Read(finite,"id")+" originalSpawn="+((Vector3)Read(finite,"spawn")).ToString("F5")+" body="+Body(finite).position.ToString("F5")+" velocity="+Body(finite).linearVelocity.ToString("F5")+" rotation="+Body(finite).rotation.eulerAngles.ToString("F4")+" nativeContacts="+Read(finite,"NativeContacts")+" carrier="+Read(finite,"LastCarrier")+" held="+(Read(finite,"Holder")!=null)+" broken="+Read(finite,"Broken")+" volume="+Read(finite,"Volume")+" spilled="+Read(finite,"SpilledVolume"));
                    }
                }
                File.WriteAllText(path,trace.ToString());
            }
            public void Dispose()
            {
                try{if(match!=null)Observe(true);}finally{try{if(match!=null)Call(match,"SetupScreen");}finally{Time.timeScale=1;UnityEngine.Object.DestroyImmediate(isolation);InputSystem.settings=previous;UnityEngine.Object.DestroyImmediate(settings);preferences.Dispose();Sports("SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);}}
            }
        }
        static Collider BowlOpening(Natural f,int owner)
        {
            var root=((Component)f.bowls[owner]).transform.Find("Exact physical mixing aperture");Assert.That(root,Is.Not.Null,"Use the actual serialized gameplay opening, never a guessed renderer or placeholder.");return root.GetComponent<Collider>();
        }
        static bool StreamEndsAt(object source,Collider aperture)
        {
            var line=(LineRenderer)Read(source,"stream");if(!line.enabled||line.positionCount<2)return false;var endpoint=line.GetPosition(line.positionCount-1);return Vector3.Distance(aperture.ClosestPoint(endpoint),endpoint)<=.065f;
        }
        static void AssertActualBowlWorkspace(Natural f,int owner)
        {
            var arm=f.arms[owner];var bowl=Read(f.bowls[owner],"vessel");var pan=Read(f.pans[owner],"vessel");var grip=(Rigidbody)Read(arm,"grip");var truck=(Component)Read(arm,"truck");var basePoint=truck.transform.TransformPoint((Vector3)Read(arm,"mount"));var target=(Vector3)f.match.GetType().GetMethod("PourBodyTarget",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new[]{pan});var offset=Body(bowl).position-grip.position;var forward=Vector3.ProjectOnPlane(((Transform)Read(bowl,"spout")).forward,Vector3.up).normalized;var tilted=Quaternion.AngleAxis(35,Vector3.Cross(Vector3.up,forward))*offset;
            Assert.That((float)Read(arm,"Tilt"),Is.LessThan(1));Assert.That(Vector3.Distance(basePoint,target-offset),Is.LessThanOrEqualTo(8),"Actual retained upright-bowl pose must fit the original arm, without a clamped target.");Assert.That(Vector3.Distance(basePoint,target-tilted),Is.LessThanOrEqualTo(8),"Actual retained35degree pour must also fit before the original physical bowl is acquired; body-origin distance alone is insufficient.");
        }
        static void WristSolidsRemainClear(object arm,object source)
        {
            var grip=(Rigidbody)Read(arm,"grip");var original=(Collider[])Read(source,"physical");
            foreach(var solid in grip.GetComponentsInChildren<Collider>(true).Where(shape=>shape.enabled&&shape.gameObject.activeInHierarchy&&!shape.isTrigger))
                foreach(var physical in original.Where(shape=>shape.enabled))
                    Assert.That(Physics.ComputePenetration(physical,physical.transform.position,physical.transform.rotation,solid,solid.transform.position,solid.transform.rotation,out _,out _),Is.False,"Original "+solid.name+" cannot tip the source before genuine sensor pickup; all wrist solids, including the LOWER scoop, retain their colliders.");
        }
        static bool ActualSensorContact(object arm,object source)
        {
            var grip=(Rigidbody)Read(arm,"grip");var sensor=grip.GetComponents<SphereCollider>().Single(shape=>shape.isTrigger);return ((Collider[])Read(source,"physical")).Any(shape=>shape.enabled&&Physics.ComputePenetration(sensor,sensor.transform.position,sensor.transform.rotation,shape,shape.transform.position,shape.transform.rotation,out _,out _));
        }
        [UnityTest,Timeout(220000)]
        public IEnumerator FourOriginalNwhWorktopArrivalsLatchNativeBrakingUntilActualCargoWorkspaceAdmission()
        {
            using(var f=new Natural())
            {
                yield return f.Load(0);Assert.That((int)Read(f.match,"option"),Is.Zero,"Use the same original QuickShow setup option as the unchanged full fixture.");Assert.That((float)Read(f.rules,"Remaining"),Is.EqualTo(300));Time.timeScale=3;var brakeSeen=new bool[4,2];var arrived=new bool[4,2];var ovenBrakeSeen=new bool[4,2];var actualCookedPickup=new bool[4];var ovens=((Array)Read(f.match,"ovens")).Cast<object>().ToArray();var trucks=f.arms.Select(a=>Read(a,"truck")).ToArray();var masses=trucks.Select(Body).Select(b=>b.mass).ToArray();var physicsStep=new WaitForFixedUpdate();float deadline=Time.realtimeSinceStartup+175;string path="Logs/TruckSports/PlaytestRepair/BakeWorktopBrakeNativeV29/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")+".txt";Directory.CreateDirectory(Path.GetDirectoryName(path));var trace=new StringBuilder();float next=0;
                bool Done()=>Enumerable.Range(0,4).All(owner=>arrived[owner,0]&&arrived[owner,1]);
                try
                {
                    while(!Done()&&!(bool)Read(f.match,"Finished")&&Time.realtimeSinceStartup<deadline)
                    {
                        f.Observe();for(int owner=0;owner<4;owner++)
                        {
                            int task=((int[])Read(f.match,"task"))[owner],phase=((int[])Read(f.match,"worktopParking"))[owner];var truck=trucks[owner];var body=Body(truck);var state=Read(Read(Read(Read(truck,"tractorMotor"),"vehicle"),"input"),"states");int place=task==15?0:task==19?1:-1;
                            if(place>=0&&phase==1)
                            {
                                Assert.That((float)Read(state,"throttle"),Is.Zero);Assert.That((float)Read(state,"handbrake"),Is.EqualTo(1),"Original NWH handbrake must stay engaged throughout actual coasting, including after crossing the first arrival radius.");if(body.linearVelocity.magnitude>=.6f)brakeSeen[owner,place]=true;
                            }
                            if((task==35||task==37)&&phase==1)
                            {
                                Assert.That((float)Read(state,"throttle"),Is.Zero);Assert.That((float)Read(state,"handbrake"),Is.EqualTo(1),"Oven retirement must finish actual native braking instead of circling out of its original clear pose.");if(body.linearVelocity.magnitude>=.6f)ovenBrakeSeen[owner,task==35?0:1]=true;
                            }
                            if(task==13||task==38)
                            {
                                float front=(float)f.match.GetType().GetMethod("CrewFront",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new[]{f.arms[owner]});float near=(float)f.match.GetType().GetMethod("DoorSweepNear",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new[]{ovens[owner]});Assert.That(body.linearVelocity.magnitude,Is.LessThan(.6f));Assert.That(front,Is.LessThan(near-.2f));
                            }
                            if(task==39){Assert.That(Read(f.arms[owner],"Held"),Is.SameAs(Read(f.pans[owner],"vessel")));Assert.That(((float[])Read(f.rules,"heatSeconds"))[owner],Is.GreaterThanOrEqualTo(10));actualCookedPickup[owner]=true;}
                            if(task==41||task==46)
                            {
                                int admittedPlace=task==41?0:1;Assert.That(brakeSeen[owner,admittedPlace],Is.True);Assert.That(body.linearVelocity.magnitude,Is.LessThan(.6f));var pan=Read(f.pans[owner],"vessel");Assert.That(Read(f.arms[owner],"Held"),Is.SameAs(pan));Assert.That((bool)Read(pan,"Broken"),Is.False);Assert.That((float)Read(pan,"Volume"),Is.GreaterThanOrEqualTo(2.1f));var goal=((Vector3[])Read(f.match,"worktopGoals"))[owner];Assert.That(goal,Is.EqualTo(new Vector3((owner-1.5f)*34+.8f,0,admittedPlace==0?-7.5f:-60.5f)),"Keep original physical route waypoint; arrival must not be a rewritten target or forced pose.");arrived[owner,admittedPlace]=true;
                            }
                            Assert.That(body.mass,Is.EqualTo(masses[owner]));if(f.Clock>=next)trace.AppendLine("BAKE_NATIVE_WORKTOP_BRAKE clock="+f.Clock.ToString("R")+" owner="+owner+" task="+task+" phase="+phase+" goal="+((Vector3[])Read(f.match,"worktopGoals"))[owner].ToString("F5")+" body="+body.position.ToString("F5")+" velocity="+body.linearVelocity.ToString("F5")+" euler="+body.rotation.eulerAngles.ToString("F5")+" throttle="+Read(state,"throttle")+" handbrake="+Read(state,"handbrake"));
                        }
                        if(f.Clock>=next){next=f.Clock+.1f;File.WriteAllText(path,trace.ToString());}yield return physicsStep;
                    }
                    Assert.That(Done(),Is.True,"All4 ordinary original NWH trucks must physically brake and admit BOTH actual intact cargo workspaces before the unchanged show/175real limit; no forced winner or score.");Assert.That(actualCookedPickup,Is.All.True);for(int owner=0;owner<4;owner++)for(int trip=0;trip<2;trip++)Assert.That(ovenBrakeSeen[owner,trip],Is.True);float completedClock=f.Clock;Call(f.match,"SetupScreen");Assert.That((int[])Read(f.match,"worktopParking"),Is.All.Zero);Assert.That((bool[])Read(f.match,"worktopGoalSet"),Is.All.False);Assert.That((Vector3[])Read(f.match,"ovenRetreatGoals"),Is.All.EqualTo(Vector3.zero));Assert.That((bool)Call(f.match,"StartMatch"),Is.True);Assert.That((int[])Read(f.match,"worktopParking"),Is.All.Zero);Assert.That((bool[])Read(f.match,"worktopGoalSet"),Is.All.False);Assert.That((int[])Read(f.rules,"points"),Is.All.Zero);TestContext.WriteLine("BAKE_NATIVE_WORKTOP_BRAKE all4 actualNWHbraking/coast/2ovenretirements/realcookedpickup/stoppedpose/BOTHoriginalwaypoints/reset ORIGINAL_QUICK_SHOW_300 clock="+completedClock+"; no bodyposition/velocity/force/score/recipe/deadline injection. Trace="+path);
                }
                finally{File.WriteAllText(path,trace.ToString());}
            }
        }
        [UnityTest,Timeout(220000)]
        public IEnumerator FourNaturalCookedPansClearOriginalCounterSidesAndReceiveAllFourActualCreamQuadrants()
        {
            using(var f=new Natural())
            {
                yield return f.Load();Time.timeScale=3;var seenLift=new bool[4];var aligned=new bool[4];var released=new bool[4];var decorated=new bool[4];var quadrantHit=new bool[4,4];var supports=(Collider[])Read(f.match,"decorationSupports");var original=supports.Select(c=>c.bounds).ToArray();var observers=new BakeBatterContactDiagnosticObserver[4];var nozzles=((Array)Read(f.match,"nozzles")).Cast<object>().ToArray();var previous=Enumerable.Repeat(-1,4).ToArray();var physicsStep=new WaitForFixedUpdate();string folder="Logs/TruckSports/PlaytestRepair/BakeSafeCounterCreamNativeV28/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
                for(int owner=0;owner<4;owner++){var bowl=(Component)Read(f.bowls[owner],"vessel");var pan=(Component)Read(f.pans[owner],"vessel");observers[owner]=pan.gameObject.AddComponent<BakeBatterContactDiagnosticObserver>();observers[owner].Bind(f.match,(Component)f.arms[owner],bowl,pan,owner,"safe-counter-cream",180);}
                float deadline=Time.realtimeSinceStartup+175;
                try
                {
                    while(decorated.Any(value=>!value)&&!(bool)Read(f.match,"Finished")&&Time.realtimeSinceStartup<deadline)
                    {
                        f.Observe();for(int owner=0;owner<4;owner++)
                        {
                            var pan=Read(f.pans[owner],"vessel");var arm=f.arms[owner];int task=((int[])Read(f.match,"task"))[owner];var physical=((Collider[])Read(pan,"physical")).Where(c=>c!=null&&c.enabled).ToArray();Assert.That(supports[owner].bounds,Is.EqualTo(original[owner]));Assert.That(observers[owner].CounterBaseContacts,Is.Zero,"A genuinely carried pan cannot contact the original workstation SIDE before clearing its top; no collision isolation.");Assert.That(observers[owner].DoorPanContacts,Is.Zero);
                            if(task==41){seenLift[owner]=true;if(physical.Min(c=>c.bounds.min.y)<=supports[owner].bounds.max.y+.2f)Assert.That(physical.Max(c=>c.bounds.max.x),Is.LessThan(supports[owner].bounds.min.x-.1f),"Raise original pan while still completely in aisle before any lateral advance.");}
                            if(task==42||task==16)
                            {
                                Assert.That(seenLift[owner],Is.True);Assert.That((bool)Read(pan,"Broken"),Is.False);Assert.That(Read(arm,"Held"),Is.SameAs(pan));Assert.That((float)Read(pan,"Volume"),Is.GreaterThanOrEqualTo(2.1f));Assert.That(((float[])Read(f.rules,"heatSeconds"))[owner],Is.GreaterThanOrEqualTo(10));var args=new object[]{supports[owner],pan,new Vector3((owner-1.5f)*34+8,3,-5),Vector3.zero,Vector3.zero};Assert.That((bool)f.match.GetType().GetMethod("SideWorktopPoses",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,args),Is.True);var basePoint=((Component)Read(arm,"truck")).transform.TransformPoint((Vector3)Read(arm,"mount"));var offset=Body(pan).position-((Rigidbody)Read(arm,"grip")).position;foreach(var target in args.Skip(3).Cast<Vector3>())Assert.That(Vector3.Distance(basePoint,target-offset),Is.LessThanOrEqualTo(8));
                                if(task==16&&previous[owner]!=16){Assert.That(physical.Min(c=>c.bounds.min.y),Is.GreaterThan(supports[owner].bounds.max.y+.2f));Assert.That(physical.Min(c=>c.bounds.min.x),Is.GreaterThanOrEqualTo(supports[owner].bounds.min.x+.049f));Assert.That(physical.Max(c=>c.bounds.max.x),Is.LessThanOrEqualTo(supports[owner].bounds.max.x-.049f));aligned[owner]=true;}
                            }
                            if(aligned[owner]&&Read(pan,"Holder")==null)released[owner]=true;
                            if(task==17)
                            {
                                Assert.That(released[owner],Is.True);Assert.That((bool)Read(pan,"Broken"),Is.False);if((int)Read(nozzles[owner],"NativePours")>0){Assert.That(Read(pan,"LastSupport"),Is.SameAs(supports[owner]));Assert.That((float)Read(pan,"SupportAge"),Is.LessThan(.15f));}var basePoint=((Component)Read(arm,"truck")).transform.TransformPoint((Vector3)Read(arm,"mount"));var offset=((Transform)Read(nozzles[owner],"spout")).position-((Rigidbody)Read(arm,"grip")).position;for(int q=0;q<4;q++){var target=Body(pan).position+new Vector3(q%2==0?-.7f:.7f,2.2f,q<2?-.7f:.7f);Assert.That(Vector3.Distance(basePoint,target-offset),Is.LessThanOrEqualTo(8),"All4 actual nozzle corners fit native reach; not just the near side.");}
                            }
                            var opening=(Collider)Read(f.pans[owner],"creamOpening");if((int)Read(nozzles[owner],"NativePours")>0&&StreamEndsAt(nozzles[owner],opening)){var line=(LineRenderer)Read(nozzles[owner],"stream");var local=((Component)pan).transform.InverseTransformPoint(line.GetPosition(line.positionCount-1));int q=(local.x>=0?1:0)+(local.z>=0?2:0);quadrantHit[owner,q]=true;}
                            if(((int[])Read(f.rules,"decorated"))[owner]==15&&!decorated[owner]){Assert.That(released[owner],Is.True);Assert.That((bool)Read(pan,"Broken"),Is.False);Assert.That(Read(pan,"Holder"),Is.Null);Assert.That(Read(pan,"LastSupport"),Is.SameAs(supports[owner]));Assert.That((float)Read(pan,"Volume"),Is.GreaterThanOrEqualTo(2.1f));Assert.That((int)Read(nozzles[owner],"NativePours"),Is.GreaterThan(0));for(int q=0;q<4;q++)Assert.That(quadrantHit[owner,q],Is.True,"Each owner quadrant needs observed real ballistic stream contact.");decorated[owner]=true;}
                            previous[owner]=task;
                        }
                        yield return physicsStep;
                    }
                    f.Observe(true);Assert.That(seenLift,Is.All.True);Assert.That(aligned,Is.All.True);Assert.That(released,Is.All.True);Assert.That(decorated,Is.All.True,"All4 original finite cooked pans must physically settle and receive4 real cream quadrants within original5minute show/175real budget.");Assert.That((int)Read(f.match,"NativeDecorationHits"),Is.GreaterThan(0));TestContext.WriteLine("BAKE_SAFE_NATIVE_COUNTER_CREAM all4 original cookedpan/aislelift/horizontalclearance/verticalseat/actualsupport/ZEROsidecontacts/4nativecreamquadrants clock="+f.Clock+"; no stock,force,rule,score,deadline or geometry injection.");
                }
                finally{foreach(var observer in observers)if(observer!=null){observer.Save(folder);UnityEngine.Object.DestroyImmediate(observer);}}
            }
        }
        [UnityTest,Timeout(105000)]
        public IEnumerator NaturalCookedPanDecorationCounterFirstContactAndActualNozzleQuadrantReachAreRecorded()
        {
            using(var f=new Natural())
            {
                yield return f.Load();Time.timeScale=3;string folder="Logs/TruckSports/PlaytestRepair/BakeCookedCounterContactNativeV27/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");var observers=new BakeBatterContactDiagnosticObserver[4];var reached=new bool[4];var physicsStep=new WaitForFixedUpdate();
                for(int owner=0;owner<4;owner++){var bowl=(Component)Read(f.bowls[owner],"vessel");var pan=(Component)Read(f.pans[owner],"vessel");observers[owner]=pan.gameObject.AddComponent<BakeBatterContactDiagnosticObserver>();observers[owner].Bind(f.match,(Component)f.arms[owner],bowl,pan,owner,"cooked-counter",180);}
                float deadline=Time.realtimeSinceStartup+85;
                try
                {
                    while(f.Clock<210&&!(bool)Read(f.rules,"Complete")&&Time.realtimeSinceStartup<deadline){f.Observe();for(int owner=0;owner<4;owner++)reached[owner]|=((int[])Read(f.match,"task"))[owner]==16;yield return physicsStep;}
                    Assert.That(f.Clock>=210||(bool)Read(f.rules,"Complete"),Is.True,"Complete requested natural counter measurement or genuine original terminal outcome; realtime truncation is not evidence.");Assert.That(f.Clock,Is.GreaterThan(180));Assert.That(reached,Is.All.True,"All4 actual cooked finite pans must reach original decoration insertion before diagnosing its first physical obstruction.");Assert.That((float[])Read(f.rules,"heatSeconds"),Is.All.GreaterThanOrEqualTo(10));
                }
                finally{foreach(var observer in observers)if(observer!=null){observer.Save(folder);UnityEngine.Object.DestroyImmediate(observer);}}
                TestContext.WriteLine("BAKE_COOKED_COUNTER_NATIVE_DIAGNOSTIC measurementONLY actual4recipes/cookedpan/retainedjoint/counterfirstpair/8mrequest/all4nozzlequadrants clock="+f.Clock+" folder="+folder+"; no pose/body/force/score/stock/recipe/deadline mutation or gameplay/Windows acceptance claim.");
            }
        }
        [UnityTest,Timeout(70000)]
        public IEnumerator NaturalLoadedPanOvenReachBoundsAndNativeContactsAreRecorded()
        {
            using(var f=new Natural())
            {
                yield return f.Load();Time.timeScale=3;string folder="Logs/TruckSports/PlaytestRepair/BakePostReleaseDoorToolContactNativeV25/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");var observers=new BakeBatterContactDiagnosticObserver[4];var reached=new bool[4];
                for(int owner=0;owner<4;owner++){var bowl=(Component)Read(f.bowls[owner],"vessel");var pan=(Component)Read(f.pans[owner],"vessel");var listener=pan.gameObject.AddComponent<BakeBatterContactDiagnosticObserver>();observers[owner]=listener;listener.Bind(f.match,(Component)f.arms[owner],bowl,pan,owner,"pan-door-tool",132);}
                float deadline=Time.realtimeSinceStartup+60;var physicsStep=new WaitForFixedUpdate();
                try
                {
                    while(f.Clock<148&&!(bool)Read(f.rules,"Complete")&&Time.realtimeSinceStartup<deadline){f.Observe();for(int owner=0;owner<4;owner++)reached[owner]|=((int[])Read(f.match,"task"))[owner]==12;yield return physicsStep;}
                    Assert.That(f.Clock>=148||(bool)Read(f.rules,"Complete"),Is.True,"Finish requested ordinary window or genuine original terminal outcome; realtime truncation is not evidence.");Assert.That(f.Clock,Is.GreaterThan(130),"Actual natural finite recipe must reach the requested post-carry measurement stage.");Assert.That(reached,Is.All.True,"All4 actual finite loaded pans must reach original oven insertion before diagnosing that gate.");
                }
                finally{foreach(var observer in observers)if(observer!=null){observer.Save(folder);UnityEngine.Object.DestroyImmediate(observer);}}
                TestContext.WriteLine("BAKE_OVEN_REACH_NATIVE_DIAGNOSTIC measurementONLY ordinary4AI originalpan/oven/shelf/retainedjoint/8mcap/nativefirstDoorVsToolPair/actualWristSolid/hypotheticalLiftAndWithdrawReach clock="+f.Clock+" folder="+folder+"; no transform/body/force/stock/recipe/rules mutation or gameplay/Windows acceptance claim.");
            }
        }
        [UnityTest,Timeout(70000)]
        public IEnumerator NaturalLoadedPanPickupAndDepartureOriginalNativeContactsAreRecorded()
        {
            using(var f=new Natural())
            {
                yield return f.Load();Time.timeScale=3;string folder="Logs/TruckSports/PlaytestRepair/BakePanDepartureContactNativeV20/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");var observers=new BakeBatterContactDiagnosticObserver[8];
                for(int owner=0;owner<4;owner++)
                {
                    var bowl=(Component)Read(f.bowls[owner],"vessel");var pan=(Component)Read(f.pans[owner],"vessel");
                    for(int role=0;role<2;role++){var observed=role==0?bowl:pan;var listener=observed.gameObject.AddComponent<BakeBatterContactDiagnosticObserver>();observers[owner*2+role]=listener;listener.Bind(f.match,(Component)f.arms[owner],bowl,pan,owner,role==0?"bowl":"pan",120);}
                }
                float deadline=Time.realtimeSinceStartup+60;
                try{while(f.Clock<140&&!(bool)Read(f.rules,"Complete")&&Time.realtimeSinceStartup<deadline){f.Observe();yield return null;}Assert.That(f.Clock>=140||(bool)Read(f.rules,"Complete"),Is.True,"Complete the requested ordinary physical window or original terminal outcome; realtime truncation is not measurement success.");Assert.That(f.Clock,Is.GreaterThan(120),"Actual finite recipe must reach the post-transfer observation window.");}
                finally{foreach(var observer in observers)if(observer!=null){observer.Save(folder);UnityEngine.Object.DestroyImmediate(observer);}}
                TestContext.WriteLine("BAKE_PAN_DEPARTURE_NATIVE_CONTACT_DIAGNOSTIC measurementONLY120-140sim/freshbuffers ordinary4AI clock="+f.Clock+" folder="+folder+"; original loaded-pan pickup, original returned bowls, native contacts/velocities/impulses and NWH; no physics/body/rule injection or gameplay/Windows pass claim.");
            }
        }
        [UnityTest,Timeout(60000)]
        public IEnumerator NaturalMeasuredEggAndWhiskThenOriginalBowlPanContactsAndRequestedReachAreRecorded()
        {
            using(var f=new Natural())
            {
                yield return f.Load();Time.timeScale=3;string folder="Logs/TruckSports/PlaytestRepair/BakeBatterContactNativeV16/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");var observers=new BakeBatterContactDiagnosticObserver[8];
                for(int owner=0;owner<4;owner++)
                {
                    var bowl=(Component)Read(f.bowls[owner],"vessel");var pan=(Component)Read(f.pans[owner],"vessel");
                    for(int role=0;role<2;role++){var observed=role==0?bowl:pan;var listener=observed.gameObject.AddComponent<BakeBatterContactDiagnosticObserver>();observers[owner*2+role]=listener;listener.Bind(f.match,(Component)f.arms[owner],bowl,pan,owner,role==0?"bowl":"pan");}
                }
                float deadline=Time.realtimeSinceStartup+52;
                try{while(f.Clock<125&&Time.realtimeSinceStartup<deadline){f.Observe();yield return null;}Assert.That(f.Clock,Is.GreaterThanOrEqualTo(125),"Complete requested ordinary natural simulation window; incomplete data is not a diagnostic pass.");}
                finally{foreach(var observer in observers)if(observer!=null){observer.Save(folder);UnityEngine.Object.DestroyImmediate(observer);}}
                TestContext.WriteLine("BAKE_BATTER_NATIVE_CONTACT_DIAGNOSTIC measurementONLY ordinary4AI clock="+f.Clock+" folder="+folder+"; actual unmodified finite recipe/egg/whisk/bowl/pan/native8m arm authority; no gameplay or Windows acceptance claim.");
            }
        }
        [UnityTest,Timeout(35000)]
        public IEnumerator ThreeOrdinaryStartsAcquireAllFourUntippedFiniteSacksWithOnlyRealSensorContacts()
        {
            using(var f=new Natural())
            {
                yield return f.Load();Time.timeScale=3;float deadline=Time.realtimeSinceStartup+23;
                for(int round=0;round<3;round++)
                {
                    if(round>0){Call(f.match,"SetupScreen");Assert.That((bool)Call(f.match,"StartMatch"),Is.True);Time.timeScale=3;}
                    var acquired=new bool[4];
                    while(acquired.Any(value=>!value)&&f.Clock<20&&Time.realtimeSinceStartup<deadline)
                    {
                        for(int owner=0;owner<4;owner++)
                        {
                            if(acquired[owner])continue;var source=f.ingredients[owner*5];WristSolidsRemainClear(f.arms[owner],source);
                            Assert.That((float)Read(source,"Volume"),Is.EqualTo((float)Read(source,"initialVolume")).Within(.005f),"Finite original source cannot be spilled/tipped during acquisition.");Assert.That(Vector3.Dot(Body(source).transform.up,Vector3.up),Is.GreaterThan(.995f),"No badly tilted attachment may pass the native sensor gate.");
                            if((int)Read(source,"LastCarrier")==owner){Assert.That(ActualSensorContact(f.arms[owner],source),Is.True);Assert.That((int)Read(f.arms[owner],"NativeCandidates"),Is.GreaterThan(0));acquired[owner]=true;}
                        }
                        f.Observe();yield return null;
                    }
                    f.Observe(true);Assert.That(acquired.All(value=>value),Is.True,"All four ordinary NWH owners must reach real source contact in every actual rematch without a cap/gate waiver.");TestContext.WriteLine("BAKE_ALL_SOLID_CLEAR_NATIVE_PICKUP round="+round+" all4clock="+f.Clock);
                }
            }
        }
        [UnityTest,Timeout(35000)]
        public IEnumerator ThreeNaturalStartsMeasureOriginalSourceCollisionPairsAllWristShapesBrakesAndActualReach()
        {
            using(var f=new Natural())
            {
                yield return f.Load();Time.timeScale=3;string folder="Logs/TruckSports/PlaytestRepair/BakeSourceContactDiagnosticNativeV13/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");var observers=new BakeSourceContactDiagnosticObserver[4];
                for(int owner=0;owner<4;owner++){observers[owner]=((Component)f.ingredients[owner*5]).gameObject.AddComponent<BakeSourceContactDiagnosticObserver>();observers[owner].Bind(f.match,(Component)f.arms[owner],(Component)f.ingredients[owner*5],owner);}
                float deadline=Time.realtimeSinceStartup+23;int completed=0;
                try
                {
                    for(int round=0;round<3;round++)
                    {
                        if(round>0){Call(f.match,"SetupScreen");foreach(var observer in observers)observer.ResetMeasurement();Assert.That((bool)Call(f.match,"StartMatch"),Is.True);Time.timeScale=3;}
                        while(f.Clock<16&&Time.realtimeSinceStartup<deadline)yield return null;
                        foreach(var observer in observers){observer.Save(folder,round);Assert.That(observer.Count,Is.GreaterThan(0));}
                        Assert.That(f.Clock,Is.GreaterThanOrEqualTo(16),"Bounded three-start diagnostic must finish its requested ordinary physics window; incomplete data is not a pass.");completed++;
                    }
                }
                finally{foreach(var observer in observers)if(observer!=null)UnityEngine.Object.DestroyImmediate(observer);}
                Assert.That(completed,Is.EqualTo(3));TestContext.WriteLine("BAKE_ORIGINAL_CONTACT_DIAGNOSTIC measurementONLY rounds3/owners4 folder="+folder+"; original source state, collision callback pairs, solid scoop/tray/sensor geometry and NWHbrakes/reach; no acceptance/physics mutations.");
            }
        }
        [UnityTest,Timeout(30000)]
        public IEnumerator FourOriginalNativeSourceSensorsAcquireFromAboveBeforeFiniteFlourCanBePushedOffItsCounter()
        {
            using(var f=new Natural())
            {
                yield return f.Load();Time.timeScale=3;float deadline=Time.realtimeSinceStartup+20;var actualSensorContact=new bool[4];
                while(actualSensorContact.Any(seen=>!seen)&&f.Clock<30&&Time.realtimeSinceStartup<deadline)
                {
                    f.Observe();for(int owner=0;owner<4;owner++)
                    {
                        var flour=f.ingredients[owner*5];
                        WristSolidsRemainClear(f.arms[owner],flour);
                        if((int)Read(flour,"LastCarrier")==-1)
                        {
                            Assert.That(Vector3.Distance(Body(flour).position,(Vector3)Read(flour,"spawn")),Is.LessThan(.5f),"Original flour must remain on the physical counter until genuine sensor pickup; the tray cannot push it away while chasing a pose.");
                            Assert.That((float)Read(flour,"Volume"),Is.EqualTo((float)Read(flour,"initialVolume")).Within(.005f));
                        }
                        else if(!actualSensorContact[owner])
                        {
                            var grip=(Rigidbody)Read(f.arms[owner],"grip");var sensor=grip.GetComponents<SphereCollider>().Single(shape=>shape.isTrigger);var physical=(Collider[])Read(flour,"physical");
                            actualSensorContact[owner]=physical.Any(shape=>shape.enabled&&Physics.ComputePenetration(sensor,sensor.transform.position,sensor.transform.rotation,shape,shape.transform.position,shape.transform.rotation,out _,out _));
                            Assert.That(actualSensorContact[owner],Is.True,"First genuine pickup is still inside the original physical sensor geometry, not a proximity/Attach bypass.");
                            float top=physical.Where(shape=>shape.enabled).Max(shape=>shape.bounds.max.y);var solids=grip.GetComponentsInChildren<Collider>(true).Where(shape=>shape.enabled&&!shape.isTrigger).ToArray();float lowest=solids.Min(shape=>shape.bounds.min.y);
                            Assert.That(lowest-top,Is.GreaterThan(.05f),"First native contact comes from the sensor above the actual supply; ALL solid wrist tools, including the lower scoop, stay clear.");
                            TestContext.WriteLine("BAKE_FIRST_NATIVE_SENSOR owner="+owner+" clock="+f.Clock+" sensor="+sensor.bounds+" sourceTop="+top+" lowestWristSolid="+lowest+" candidates="+Read(f.arms[owner],"NativeCandidates"));
                        }
                    }
                    yield return null;
                }
                for(int owner=0;owner<4;owner++){var flour=f.ingredients[owner*5];Assert.That((int)Read(flour,"LastCarrier"),Is.EqualTo(owner),"Actual native sensor/Grab must acquire every original flour sack.");Assert.That(actualSensorContact[owner],Is.True);Assert.That((int)Read(f.arms[owner],"NativeCandidates"),Is.GreaterThan(0));Assert.That((bool)Read(flour,"Broken"),Is.False);}
                TestContext.WriteLine("BAKE_ABOVE_NATIVE_ACQUISITION all4actualsensors/finiteoriginalflour atclock="+f.Clock+"; no directAttach/pose/volume writes.");
            }
        }
        [UnityTest,Timeout(70000)]
        public IEnumerator FourNaturalAiAlignFiniteFlourThenActualBallisticStreamsEnterOriginalBowlApertures()
        {
            using(var f=new Natural())
            {
                yield return f.Load();Time.timeScale=2;var receivedAtOpening=new bool[4];float deadline=Time.realtimeSinceStartup+50;
                while(Enumerable.Range(0,4).Any(owner=>f.Measured[owner,0]<.8f)&&f.Clock<85&&Time.realtimeSinceStartup<deadline)
                {
                    f.Observe();for(int owner=0;owner<4;owner++)
                    {
                        var flour=f.ingredients[owner*5];if((int)Read(flour,"NativePours")>0&&StreamEndsAt(flour,BowlOpening(f,owner)))receivedAtOpening[owner]=true;
                        Assert.That((float)Read(flour,"Volume")+(float)Read(flour,"SpilledVolume")+f.Measured[owner,0],Is.EqualTo((float)Read(flour,"initialVolume")).Within(.01f),"Finite original flour is either still held, physically spilled or genuinely measured; no refill, guessed acceptance or fabricated amount.");
                    }
                    yield return null;
                }
                for(int owner=0;owner<4;owner++)
                {
                    var flour=f.ingredients[owner*5];Assert.That(f.Measured[owner,0],Is.GreaterThanOrEqualTo(.8f),"Each actual owner must complete its first real ingredient rather than emptying stock outside the opening.");Assert.That(receivedAtOpening[owner],Is.True,"Observed original ballistic stream endpoint must intersect its actual authored receiving aperture.");Assert.That((int)Read(flour,"NativePours"),Is.GreaterThan(0));Assert.That((int)Read(f.arms[owner],"NativeCandidates"),Is.GreaterThan(0));Assert.That((int)Read(flour,"LastCarrier"),Is.EqualTo(owner));
                }
                TestContext.WriteLine("BAKE_ACTUAL_FLOUR_APERTURES all4finite/nativecontact/ballisticreceipt/accounting pass atclock="+f.Clock+"; no body/volume/rule/score state injection.");
            }
        }
        [UnityTest,Timeout(110000)]
        public IEnumerator FourActualFiniteLoadedPansEnterOriginalShelvesAndHeatOnlyAfterNativeSupportedRelease()
        {
            using(var f=new Natural())
            {
                yield return f.Load();Time.timeScale=3;var aligned=new bool[4];var admitted=new bool[4];var released=new bool[4];var heated=new bool[4];var previous=Enumerable.Repeat(-1,4).ToArray();var ovens=((Array)Read(f.match,"ovens")).Cast<object>().ToArray();var shelves=ovens.Select(o=>(Collider)Read(o,"shelf")).ToArray();var originalBounds=shelves.Select(c=>c.bounds).ToArray();var trays=ovens.Select(o=>((Transform)Read(o,"trayPoint")).position).ToArray();float deadline=Time.realtimeSinceStartup+80;var physicsStep=new WaitForFixedUpdate();var observers=new BakeBatterContactDiagnosticObserver[4];bool manualCancel=false;string contactFolder="Logs/TruckSports/PlaytestRepair/BakeOutwardOvenNativeV26/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
                for(int owner=0;owner<4;owner++){var bowl=(Component)Read(f.bowls[owner],"vessel");var pan=(Component)Read(f.pans[owner],"vessel");observers[owner]=pan.gameObject.AddComponent<BakeBatterContactDiagnosticObserver>();observers[owner].Bind(f.match,(Component)f.arms[owner],bowl,pan,owner,"safe-outward-oven",132);}
                bool Footprint(Collider[] shapes,Bounds shelf)=>shapes.Min(c=>c.bounds.min.x)>=shelf.min.x+.049f&&shapes.Max(c=>c.bounds.max.x)<=shelf.max.x-.049f&&shapes.Min(c=>c.bounds.min.z)>=shelf.min.z+.049f&&shapes.Max(c=>c.bounds.max.z)<=shelf.max.z-.049f;
                try
                {
                while(heated.Any(value=>!value)&&f.Clock<180&&Time.realtimeSinceStartup<deadline)
                {
                    f.Observe();for(int owner=0;owner<4;owner++)
                    {
                        Assert.That(shelves[owner].bounds,Is.EqualTo(originalBounds[owner]));Assert.That(((Transform)Read(ovens[owner],"trayPoint")).position,Is.EqualTo(trays[owner]));var pan=Read(f.pans[owner],"vessel");var bowl=Read(f.bowls[owner],"vessel");var arm=f.arms[owner];int task=((int[])Read(f.match,"task"))[owner];var physical=((Collider[])Read(pan,"physical")).Where(c=>c!=null&&c.enabled).ToArray();
                        Assert.That(observers[owner].DoorPanContacts,Is.Zero,"Actual outward moving door must never touch the original loaded pan; no IgnoreCollision or force bypass.");
                        if((bool)Read(ovens[owner],"CrewMotionPending"))
                        {
                            Assert.That(Quaternion.Angle(((Rigidbody)Read(ovens[owner],"door")).rotation,Quaternion.Euler(0,105,0)),Is.LessThan(.1f),"Accepted close request remains physically open until original full assembly clears its actual sweep.");Assert.That(((float[])Read(f.rules,"heatSeconds"))[owner],Is.Zero,"No heat before native closure and real shelf support.");
                            if(owner==0&&!manualCancel)
                            {
                                Assert.That((bool)Read(ovens[owner],"WantsClosed"),Is.True);Assert.That((bool)Call(ovens[owner],"Toggle"),Is.True,"Fresh manual cancel uses the same actual control visit.");Assert.That((bool)Read(ovens[owner],"CrewMotionPending"),Is.False);Assert.That((bool)Read(ovens[owner],"WantsClosed"),Is.False);Assert.That((bool)Call(ovens[owner],"RequestToggleAfterCrewClear"),Is.True);Assert.That((bool)Read(ovens[owner],"CrewMotionPending"),Is.True);manualCancel=true;
                            }
                        }
                        if((bool)Read(ovens[owner],"WantsClosed")&&!(bool)Read(ovens[owner],"CrewMotionPending"))
                        {
                            float actualFront=(float)f.match.GetType().GetMethod("CrewFront",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new[]{arm});float swingNear=(float)f.match.GetType().GetMethod("DoorSweepNear",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new[]{ovens[owner]});Assert.That(actualFront,Is.LessThan(swingNear-.2f),"Original NWH hull, Cab, wheels and solid gear must physically clear the real leaf sweep BEFORE closing motion.");
                        }
                        if(task==31||task==12)
                        {
                            Assert.That(Read(arm,"Held"),Is.SameAs(pan));Assert.That((float)Read(pan,"Volume"),Is.GreaterThanOrEqualTo(2.1f));Assert.That((bool)Read(pan,"Broken"),Is.False);Assert.That((bool)Read(bowl,"Broken"),Is.False);var args=new object[]{ovens[owner],pan,Vector3.zero,Vector3.zero};Assert.That((bool)f.match.GetType().GetMethod("OvenInsertionPoses",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,args),Is.True);
                            var basePoint=((Component)Read(arm,"truck")).transform.TransformPoint((Vector3)Read(arm,"mount"));var offset=Body(pan).position-((Rigidbody)Read(arm,"grip")).position;foreach(var target in args.Skip(2).Cast<Vector3>())Assert.That(Vector3.Distance(basePoint,target-offset),Is.LessThanOrEqualTo(8),"Both actual supported/lifted pan poses must fit the unchanged native machinery cap before admission.");admitted[owner]=true;
                            if(task==12&&previous[owner]!=12){Assert.That(Footprint(physical,shelves[owner].bounds),Is.True);Assert.That(physical.Min(c=>c.bounds.min.y),Is.GreaterThan(shelves[owner].bounds.max.y+.2f),"Complete original pan must align ABOVE original shelf before vertical lowering.");aligned[owner]=true;}
                        }
                        if(admitted[owner]&&Read(pan,"Holder")==null&&!released[owner]){Assert.That(aligned[owner],Is.True);Assert.That((bool)Read(pan,"Broken"),Is.False);Assert.That(Footprint(physical,shelves[owner].bounds),Is.True);released[owner]=true;}
                        if(((float[])Read(f.rules,"heatSeconds"))[owner]>0&&!heated[owner])
                        {
                            Assert.That(released[owner],Is.True);Assert.That(Read(pan,"Holder"),Is.Null);Assert.That(Read(pan,"LastSupport"),Is.SameAs(shelves[owner]));Assert.That((float)Read(pan,"SupportAge"),Is.LessThan(.15f));Assert.That(((BoxCollider)Read(ovens[owner],"chamber")).bounds.Contains(Body(pan).worldCenterOfMass),Is.True);Assert.That((bool)Read(ovens[owner],"Closed"),Is.True);Assert.That((bool)Read(ovens[owner],"WantsClosed"),Is.True);Assert.That((bool)Read(pan,"Broken"),Is.False);Assert.That((bool)Read(bowl,"Broken"),Is.False);Assert.That((float)Read(pan,"Volume"),Is.GreaterThanOrEqualTo(2.1f));heated[owner]=true;
                        }
                        previous[owner]=task;
                    }
                    yield return physicsStep;
                }
                f.Observe(true);Assert.That(manualCancel,Is.True);Assert.That(admitted,Is.All.True);Assert.That(aligned,Is.All.True);Assert.That(released,Is.All.True);Assert.That(heated,Is.All.True,"All4 original finite pans must actually settle on their original shelves and start native closed-oven heat within unchanged180sim/80real gates.");Assert.That((int)Read(f.match,"NativeHeatSteps"),Is.GreaterThan(0));Assert.That(((int[])Read(f.rules,"points")).Sum(),Is.Zero);TestContext.WriteLine("BAKE_NATIVE_ORIGINAL_SHELF_INSERTION all4 actual8m admitted poses/above-shelf alignment/vertical lower/drop/actualcontrolcancel/outwardswingzeroPANcontacts/fullcrewreverse/native shelf support/closedovenheat clock="+f.Clock+"; originalgeometry,forces,rules,stock,deadlines unchanged.");
                Call(f.match,"SetupScreen");foreach(var oven in ovens)Assert.That((bool)Read(oven,"CrewMotionPending"),Is.False,"Actual StopGame clears pending crew requests.");Assert.That((bool)Call(f.match,"StartMatch"),Is.True);foreach(var oven in ovens){Assert.That((bool)Read(oven,"CrewMotionPending"),Is.False);Assert.That((bool)Read(oven,"WantsClosed"),Is.False);Assert.That((bool)Read(oven,"Open"),Is.True);}Assert.That(((int[])Read(f.rules,"points")),Is.All.Zero);
                }
                finally{foreach(var observer in observers)if(observer!=null){observer.Save(contactFolder);UnityEngine.Object.DestroyImmediate(observer);}}
            }
        }
        [UnityTest,Timeout(110000)]
        public IEnumerator FourOriginalFiniteLoadedPansAreSensorPickedLiftedAndPhysicallyCarriedClearOfReturnedBowls()
        {
            using(var f=new Natural())
            {
                yield return f.Load();Time.timeScale=3;var picked=new bool[4];var lifted=new bool[4];var carried=new bool[4];var carryStarts=new Vector3[4];var panStarts=new Vector3[4];var driving=new bool[4];float deadline=Time.realtimeSinceStartup+80;
                while(carried.Any(value=>!value)&&f.Clock<180&&Time.realtimeSinceStartup<deadline)
                {
                    f.Observe();for(int owner=0;owner<4;owner++)
                    {
                        if(carried[owner])continue;var arm=f.arms[owner];var truck=Read(arm,"truck");var pan=Read(f.pans[owner],"vessel");var bowl=Read(f.bowls[owner],"vessel");int task=((int[])Read(f.match,"task"))[owner];
                        if(task==10){WristSolidsRemainClear(arm,bowl);WristSolidsRemainClear(arm,pan);}
                        if(task!=10&&task!=29&&task!=30&&task!=11)continue;
                        Assert.That((bool)Read(bowl,"Broken"),Is.False,"Pan pickup/departure cannot destroy the genuinely returned original bowl.");Assert.That((bool)Read(pan,"Broken"),Is.False);Assert.That((float)Read(pan,"Volume"),Is.GreaterThanOrEqualTo(2.1f),"Original finite batter remains in its actual pan.");
                        if(!ReferenceEquals(Read(arm,"Held"),pan))continue;
                        if(!picked[owner]){Assert.That(ActualSensorContact(arm,pan),Is.True,"Only original native wrist sensor contact may acquire the loaded pan.");Assert.That((int)Read(pan,"LastCarrier"),Is.EqualTo(owner));Assert.That(Read(pan,"Holder"),Is.SameAs(arm));picked[owner]=true;}
                        var joint=(ConfigurableJoint)Read(pan,"attachment");Assert.That(joint,Is.Not.Null);Assert.That(joint.connectedBody,Is.SameAs((Rigidbody)Read(arm,"grip")));Assert.That(joint.xMotion,Is.EqualTo(ConfigurableJointMotion.Locked));
                        var physical=((Collider[])Read(pan,"physical")).Where(shape=>shape!=null&&shape.enabled).ToArray();var originalBowl=((Collider[])Read(bowl,"physical")).Where(shape=>shape!=null&&shape.enabled).ToArray();float supportTop=((float[])Read(f.match,"panSupportTop"))[owner];
                        if(task==30){Assert.That(physical.Min(shape=>shape.bounds.min.y),Is.GreaterThan(supportTop+.2f),"Lift actual pan clear of its original support before aisle withdrawal.");Assert.That(Body(truck).linearVelocity.magnitude,Is.LessThan(.7f),"Native handbrake holds original truck throughout physical pan clearance.");lifted[owner]=true;}
                        if(task==11)
                        {
                            Assert.That(lifted[owner],Is.True);Assert.That(physical.Max(shape=>shape.bounds.max.x)+.1f,Is.LessThan(originalBowl.Min(shape=>shape.bounds.min.x)),"Real pan/wrist departure stays on aisle side of the returned bowl.");Assert.That(physical.Min(shape=>shape.bounds.min.y),Is.GreaterThan(supportTop+.2f),"Native carry keeps the real pan above worktop collision height.");
                            if(!driving[owner]){driving[owner]=true;carryStarts[owner]=Body(truck).position;panStarts[owner]=Body(pan).position;}
                            if(Vector3.ProjectOnPlane(Body(truck).position-carryStarts[owner],Vector3.up).magnitude>=5&&Vector3.ProjectOnPlane(Body(pan).position-panStarts[owner],Vector3.up).magnitude>=3)carried[owner]=true;
                        }
                    }
                    yield return null;
                }
                f.Observe(true);Assert.That(picked,Is.All.True);Assert.That(lifted,Is.All.True);Assert.That(carried,Is.All.True,"All4 original NWH trucks must physically carry their actual native-grabbed finite loaded pans five metres without destroying either pan or returned bowl.");Assert.That(((int[])Read(f.rules,"points")).Sum(),Is.Zero);TestContext.WriteLine("BAKE_NATIVE_LOADED_PAN_CARRY all4 actualsensor/pan2.1/nativejoint/verticalsupportclearance/aisleclearance/originalNWH5m clock="+f.Clock+"; no body/stock/force/recipe/score/deadline injection.");
            }
        }
        [UnityTest,Timeout(110000)]
        public IEnumerator AllFourLoadedPansSurviveActualUprightBowlWithdrawalAndStandReturn()
        {
            using(var f=new Natural())
            {
                yield return f.Load();Time.timeScale=3;var loaded=new bool[4];var admitted=new bool[4];var withdrew=new bool[4];var lowered=new bool[4];var returned=new bool[4];float deadline=Time.realtimeSinceStartup+80;var physicsStep=new WaitForFixedUpdate();
                while(returned.Any(value=>!value)&&f.Clock<180&&Time.realtimeSinceStartup<deadline)
                {
                    f.Observe();for(int owner=0;owner<4;owner++)
                    {
                        if(returned[owner])continue;var bowl=Read(f.bowls[owner],"vessel");var pan=Read(f.pans[owner],"vessel");int task=((int[])Read(f.match,"task"))[owner];var arm=f.arms[owner];
                        if(task==26&&!admitted[owner]){AssertActualBowlWorkspace(f,owner);admitted[owner]=true;}
                        if((float)Read(pan,"Volume")>=2.1f){Assert.That((int)Read(bowl,"NativePours"),Is.GreaterThan(0));loaded[owner]=true;}
                        if(!loaded[owner])continue;
                        Assert.That((bool)Read(pan,"Broken"),Is.False,"Actual loaded pan must survive the original bowl's physical return.");Assert.That((bool)Read(bowl,"Broken"),Is.False);Assert.That((float)Read(pan,"Volume"),Is.GreaterThanOrEqualTo(2.1f),"Finite measured batter stays inside the original loaded pan.");
                        var bowlPhysical=((Collider[])Read(bowl,"physical")).Where(shape=>shape!=null&&shape.enabled).ToArray();var panPhysical=((Collider[])Read(pan,"physical")).Where(shape=>shape!=null&&shape.enabled).ToArray();
                        if(task==27)
                        {
                            withdrew[owner]=true;Assert.That(ReferenceEquals(Read(arm,"Held"),bowl),Is.True);Assert.That((float)Read(arm,"Tilt"),Is.LessThan(1));Assert.That(Vector3.Dot(Body(bowl).transform.up,Vector3.up),Is.GreaterThan(.995f));Assert.That(bowlPhysical.Min(shape=>shape.bounds.min.y)-panPhysical.Max(shape=>shape.bounds.max.y),Is.GreaterThan(.1f),"Upright bowl withdraws above the actual loaded-pan rim before lowering.");
                        }
                        if(task==28)
                        {
                            Assert.That(withdrew[owner],Is.True);lowered[owner]=true;Assert.That(bowlPhysical.Max(shape=>shape.bounds.max.z)+.1f,Is.LessThan(panPhysical.Min(shape=>shape.bounds.min.z)),"Original bowl footprint clears original pan before vertical lowering.");
                        }
                        if(lowered[owner]&&!ReferenceEquals(Read(arm,"Held"),bowl))
                        {
                            Assert.That(Read(bowl,"Holder"),Is.Null);Assert.That(Vector3.Distance(Body(bowl).position,(Vector3)Read(bowl,"spawn")+Vector3.up*.2f),Is.LessThan(.45f));Assert.That(Vector3.Dot(Body(bowl).transform.up,Vector3.up),Is.GreaterThan(.995f));returned[owner]=true;
                        }
                    }
                    // Bot transitions happen in SportsEventSession.FixedUpdate. At 3x,
                    // a rendered frame may contain several native steps and miss the
                    // short task28 lowering phase. Observe each real step instead.
                    yield return physicsStep;
                }
                f.Observe(true);TestContext.WriteLine("BAKE_RETURN_ACTUAL_FIXED_STEPS admitted="+string.Join(",",admitted)+" loaded="+string.Join(",",loaded)+" withdrew="+string.Join(",",withdrew)+" lowered="+string.Join(",",lowered)+" returned="+string.Join(",",returned)+" clock="+f.Clock);Assert.That(admitted,Is.All.True);Assert.That(loaded,Is.All.True);Assert.That(withdrew,Is.All.True);Assert.That(lowered,Is.All.True);Assert.That(returned,Is.All.True,"All four actual finite recipes must pour, upright, withdraw and return their original bowls without breaking their loaded pans before the unchanged full-show deadline.");Assert.That(((int[])Read(f.rules,"points")).Sum(),Is.Zero,"Returning utensils alone must not fabricate a judged cake.");TestContext.WriteLine("BAKE_NATIVE_STAGED_BOWL_RETURN all4 originalfinite2.1pan/nativebatter/uprightclearance/horizontalwithdrawal/realstanddrop clock="+f.Clock+"; no body,force,volume,recipe,score or time injection.");
            }
        }
        [UnityTest,Timeout(110000)]
        public IEnumerator ActualMeasuredIngredientsEggContactAndNativeWhiskFeedRealBatterIntoOriginalPanOpening()
        {
            using(var f=new Natural())
            {
                yield return f.Load();Time.timeScale=3;var batterAtOpening=new bool[4];var sawVerticalLift=new bool[4];var clearedBeforeAdvance=new bool[4];var liftOrigins=new Vector3[4];float deadline=Time.realtimeSinceStartup+80;
                bool Loaded()=>Enumerable.Range(0,4).All(owner=>batterAtOpening[owner]&&(float)Read(Read(f.pans[owner],"vessel"),"Volume")>=2.1f);
                while(!Loaded()&&f.Clock<180&&Time.realtimeSinceStartup<deadline)
                {
                    f.Observe();for(int owner=0;owner<4;owner++)
                    {
                        var bowl=Read(f.bowls[owner],"vessel");var pan=Read(f.pans[owner],"vessel");int task=((int[])Read(f.match,"task"))[owner];
                        if(task==26)
                        {
                            if(!sawVerticalLift[owner]){AssertActualBowlWorkspace(f,owner);sawVerticalLift[owner]=true;liftOrigins[owner]=Body(bowl).position;}
                            Assert.That(ReferenceEquals(Read(f.arms[owner],"Held"),bowl),Is.True,"Only the actual native acquired bowl is lifted.");Assert.That(Vector3.ProjectOnPlane(Body(bowl).position-liftOrigins[owner],Vector3.up).magnitude,Is.LessThan(.05f),"Bowl first rises at its original actual X/Z; diagonal advance must not shove the pan.");
                        }
                        if(task==26||task==7)Assert.That(Vector3.Distance(Body(pan).position,(Vector3)Read(pan,"spawn")),Is.LessThan(.1f),"Vertical lift/alignment preserves the original physical pan before pouring; no displaced-target chase.");
                        if(task==7&&!clearedBeforeAdvance[owner])
                        {
                            Assert.That(sawVerticalLift[owner],Is.True,"The native complete recipe must physically lift the acquired bowl before advancing.");float bottom=((Collider[])Read(bowl,"physical")).Where(shape=>shape!=null&&shape.enabled).Min(shape=>shape.bounds.min.y);float panTop=((Collider[])Read(pan,"physical")).Where(shape=>shape!=null&&shape.enabled).Max(shape=>shape.bounds.max.y);Assert.That(bottom-panTop,Is.GreaterThan(.1f),"Actual bowl solids clear the actual original pan rim before forward advance.");clearedBeforeAdvance[owner]=true;
                        }
                        var opening=(Collider)Read(f.pans[owner],"batterOpening");if((int)Read(bowl,"NativePours")>0&&StreamEndsAt(bowl,opening)){Assert.That(clearedBeforeAdvance[owner],Is.True);batterAtOpening[owner]=true;}
                    }
                    yield return null;
                }
                Assert.That(Loaded(),Is.True,"Actual ingredient measure/egg impact/whisk mixing must physically transfer finite batter through a real pan aperture before the unchanged full-show completion gate.");Assert.That((int)Read(f.match,"NativeEggCracks"),Is.GreaterThan(0));Assert.That((int)Read(f.match,"NativeMixSteps"),Is.GreaterThan(0));Assert.That(Enumerable.Range(0,4).Any(owner=>Enumerable.Range(0,3).All(kind=>f.Measured[owner,kind]>=new[]{.8f,.5f,.3f}[kind]*.85f)&&f.Measured[owner,3]>=.85f),Is.True);Assert.That(((int[])Read(f.rules,"points")).Sum(),Is.Zero,"Mixing and pan loading cannot fabricate the later physically presented cake's points.");
                TestContext.WriteLine("BAKE_REAL_BATTER_APERTURE nativeegg="+Read(f.match,"NativeEggCracks")+" nativeMixSteps="+Read(f.match,"NativeMixSteps")+" clock="+f.Clock+"; cooking/decorating/presentation/fullresults remain the original separate full-match test.");
            }
        }
        [UnityTest,Timeout(70000)]
        public IEnumerator FourUntouchedFiniteMilkPotsSurviveFlourHandlingThenNativePickupAndStreamMeasureEachOwner()
        {
            using(var f=new Natural())
            {
                yield return f.Load();Time.timeScale=3;var receivedAtOpening=new bool[4];var pickupContact=new bool[4];float deadline=Time.realtimeSinceStartup+50;
                while(Enumerable.Range(0,4).Any(owner=>f.Measured[owner,1]<.5f)&&f.Clock<120&&Time.realtimeSinceStartup<deadline)
                {
                    f.Observe();for(int owner=0;owner<4;owner++)
                    {
                        var milk=f.ingredients[owner*5+1];
                        if((int)Read(milk,"LastCarrier")==-1)
                        {
                            Assert.That((bool)Read(milk,"Broken"),Is.False,"Handling the previous ingredient must preserve the untouched finite milk pot.");
                            Assert.That((float)Read(milk,"Volume"),Is.EqualTo((float)Read(milk,"initialVolume")).Within(.005f),"Unheld milk cannot be emptied by the AI's wrist sweeping the supply row.");
                            Assert.That(Vector3.Distance(Body(milk).position,(Vector3)Read(milk,"spawn")),Is.LessThan(.75f),"Unused stock stays on its actual original supply counter until physical acquisition.");
                        }
                        else
                        {
                            Assert.That((int)Read(milk,"LastCarrier"),Is.EqualTo(owner));
                            if(!pickupContact[owner])
                            {
                                var grip=(Rigidbody)Read(f.arms[owner],"grip");var sensor=grip.GetComponents<SphereCollider>().Single(shape=>shape.isTrigger);var physical=(Collider[])Read(milk,"physical");
                                pickupContact[owner]=ReferenceEquals(Read(milk,"Holder"),f.arms[owner])&&physical.Any(shape=>shape.enabled&&Physics.ComputePenetration(sensor,sensor.transform.position,sensor.transform.rotation,shape,shape.transform.position,shape.transform.rotation,out _,out _));
                                Assert.That(pickupContact[owner],Is.True,"Milk acquisition requires its actual original native trigger contact and correct Holder, rather than an unrelated solid CollisionEnter counter.");
                            }
                        }
                        if((int)Read(milk,"NativePours")>0&&StreamEndsAt(milk,BowlOpening(f,owner)))receivedAtOpening[owner]=true;
                    }
                    yield return null;
                }
                for(int owner=0;owner<4;owner++)
                {
                    var milk=f.ingredients[owner*5+1];Assert.That(f.Measured[owner,0],Is.GreaterThanOrEqualTo(.8f));Assert.That(f.Measured[owner,1],Is.GreaterThanOrEqualTo(.5f),"Every owner must proceed beyond flour through actual milk pickup/finite ballistic measurement.");Assert.That(receivedAtOpening[owner],Is.True);Assert.That((int)Read(milk,"LastCarrier"),Is.EqualTo(owner));Assert.That(pickupContact[owner],Is.True);Assert.That((int)Read(f.arms[owner],"NativeCandidates"),Is.GreaterThanOrEqualTo(2));
                    Assert.That((float)Read(milk,"Volume")+(float)Read(milk,"SpilledVolume")+f.Measured[owner,1],Is.EqualTo((float)Read(milk,"initialVolume")).Within(.01f));
                }
                TestContext.WriteLine("BAKE_REAL_MILK_STAGE all4untouchedfinitepots/nativepickup/actualaperture/measure/accounting atclock="+f.Clock+"; no stock replacement/reset/refill/contact bypass.");
            }
        }
    }
}
#endif
