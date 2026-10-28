#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using LWS.TruckMoonMadness;

public sealed class MoonPlayModeTests
{
    const BindingFlags Any=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    static Type Runtime(string n)=>Type.GetType(n+", Assembly-CSharp",true);
    static object Get(object o,string n)=>o.GetType().GetField(n,Any) is FieldInfo f?f.GetValue(o):o.GetType().GetProperty(n,Any).GetValue(o);
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethods(Any).Single(m=>m.Name==n&&m.GetParameters().Length==a.Length).Invoke(o,a);
    sealed class Fixture:IDisposable
    {
        readonly IDisposable prefs;readonly InputSettings originalSettings,temporary;readonly GameObject isolation;
        public Behaviour match;public MoonRules Rules=>(MoonRules)Get(match,"Rules");public Array Trucks=>(Array)Get(match,"trucks");public Array Pilots=>(Array)Get(match,"pilots");public Array Flags=>(Array)Get(match,"flags");
        public Fixture(){prefs=(IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"Moon07Native"});originalSettings=InputSystem.settings;temporary=UnityEngine.Object.Instantiate(originalSettings);temporary.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;temporary.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=temporary;isolation=new GameObject("Moon original input isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("LWS.TruckSports.SportsProbeInputIsolation"));}
        public IEnumerator Load(){Assert.That(File.Exists("Assets/TruckMoonMadness/Scenes/TruckMoonMadness.unity"),Is.True,"Run guarded new-scene MoonAuthoring first.");EditorSceneManager.LoadSceneInPlayMode("Assets/TruckMoonMadness/Scenes/TruckMoonMadness.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;match=UnityEngine.Object.FindObjectsByType(Runtime("LWS.TruckMoonMadness.MoonMatch")).Cast<Behaviour>().Single();Call(match,"AllBots");Assert.That((bool)Call(match,"StartMatch"),Is.True);yield return null;}
        public Rigidbody Body(int i)=>(Rigidbody)Get(Trucks.GetValue(i),"body");
        public void Dispose(){Time.timeScale=1;if(match!=null)Call(match,"SetupScreen");UnityEngine.Object.DestroyImmediate(isolation);InputSystem.settings=originalSettings;UnityEngine.Object.DestroyImmediate(temporary);prefs.Dispose();}
    }
    static IEnumerator Simulated(Fixture f,float seconds,string label){float target=(float)Get(f.match,"Clock")+seconds,deadline=Time.realtimeSinceStartup+Mathf.Max(6,seconds*3);while((float)Get(f.match,"Clock")<target&&Time.realtimeSinceStartup<deadline)yield return null;Assert.That((float)Get(f.match,"Clock"),Is.GreaterThanOrEqualTo(target),label+" requires actual fixed-step progression; paused="+Get(f.match,"Paused"));}
    static void Capture(Fixture f,string name){string folder=Path.GetFullPath("Logs/TruckSports/PlaytestRepair/Impossible07Moon");Directory.CreateDirectory(folder);var canvas=((Component)Get(f.match,"menus")).GetComponent<Canvas>();var cameras=(Camera[])Get(f.match,"cameras");var save=Runtime("LWS.TruckSports.SportsControlledCapture").GetMethods().Single(m=>m.Name=="Save"&&m.GetParameters().Length==6);foreach(int width in new[]{1280,1920})save.Invoke(null,new object[]{canvas,cameras,width,width==1280?720:1080,Path.Combine(folder,name+"-"+width+"-controlled.png"),null});}
    static void Source(object match,int i,string source){var p=((Array)Get(Get(match,"setup"),"players")).GetValue(i);var f=p.GetType().GetField("source");f.SetValue(p,Enum.Parse(f.FieldType,source));}
    [UnityTest,Timeout(420000)]public IEnumerator NaturalEightAiPhysicallyCollectDeliverContestedFlagsAndFinishActualMatch()
    {
        using(var f=new Fixture())
        {
            yield return f.Load();var roots=f.Trucks.Cast<Component>().Select(t=>t.gameObject.GetInstanceID()).ToArray();float deadline=Time.realtimeSinceStartup+340,nextLog=0;bool airborne=false,carried=false,warning=false,impact=false;float travel=0;Vector3 first=f.Body(0).position;bool captured=false;
            while(!(bool)Get(f.match,"Finished")&&Time.realtimeSinceStartup<deadline)
            {
                airborne|=f.Pilots.Cast<object>().Any(p=>(int)Get(p,"Hops")>0);carried|=f.Rules.Owners.Any(i=>i>=0);warning|=((Array)Get(f.match,"meteors")).Cast<object>().Any(m=>(float)Get(m,"WarningSeconds")>0);impact|=(int)Get(f.match,"PhysicalMeteorImpacts")>0;travel=Mathf.Max(travel,Vector3.Distance(first,f.Body(0).position));
                if(!captured&&f.Rules.Deliveries>0){Capture(f,"natural-first-real-delivery");captured=true;}
                if(Time.realtimeSinceStartup>nextLog){Debug.Log("MOON_NATIVE clock="+Get(f.match,"Clock")+" scores="+string.Join(",",f.Rules.Scores)+" owners="+string.Join(",",f.Rules.Owners)+" collections="+Get(f.match,"TotalCollections")+" impacts="+Get(f.match,"PhysicalMeteorImpacts"));nextLog=Time.realtimeSinceStartup+15;}
                yield return null;
            }
            Assert.That((bool)Get(f.match,"Finished"),Is.True,"Original AI must finish without injected poses, points or outcomes.");Assert.That(travel,Is.GreaterThan(35));Assert.That(carried&&warning&&impact,Is.True);
            Assert.That((int)Get(f.match,"TotalCollections"),Is.GreaterThanOrEqualTo(f.Rules.Deliveries));Assert.That(f.Rules.Deliveries,Is.GreaterThan(1));Assert.That(f.Rules.Scores.Sum(),Is.EqualTo(f.Rules.Deliveries));
            Assert.That(((Array)Get(f.match,"meteors")).Length,Is.EqualTo(4));CollectionAssert.AreEqual(roots,f.Trucks.Cast<Component>().Select(t=>t.gameObject.GetInstanceID()).ToArray());Capture(f,"natural-real-match-result");
            TestContext.WriteLine("Natural lunar match: bounded-hop use observed="+airborne+"; no forced hop/flag/meteor/scoring state. Controlled ability/meteor cases separately require actual physical effect.");
        }
    }
    [UnityTest,Timeout(100000)]public IEnumerator NaturalEightAiBoundedPhysicalDeliveryDiagnostic()
    {
        using(var f=new Fixture())
        {
            yield return f.Load();var roots=f.Trucks.Cast<Component>().Select(t=>t.gameObject.GetInstanceID()).ToArray();Vector3 gravity=Physics.gravity;
            yield return UntilDiagnostic(f,65);Assert.That(f.Rules.Scores.Sum(),Is.EqualTo(f.Rules.Deliveries));CollectionAssert.AreEqual(roots,f.Trucks.Cast<Component>().Select(t=>t.gameObject.GetInstanceID()).ToArray());Assert.That(Physics.gravity,Is.EqualTo(gravity));
            Debug.Log("MOON_DIAGNOSTIC_ONLY clock="+Get(f.match,"Clock")+" collections="+Get(f.match,"TotalCollections")+" deliveries="+f.Rules.Deliveries+" owners="+string.Join(",",f.Rules.Owners)+" scores="+string.Join(",",f.Rules.Scores)+"; no pose/points injection; this bounded telemetry case is not full-match acceptance.");
        }
    }
    [UnityTest,Timeout(65000)]public IEnumerator NaturalEightAiLiveCargoAndNativeContactEnergyDiagnostic()
    {
        using(var f=new Fixture())
        {
            yield return f.Load();string folder=Path.GetFullPath("Logs/TruckSports/PlaytestRepair/MoonNaturalFlagEnergyNativeV8/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));Directory.CreateDirectory(folder);
            var observers=f.Flags.Cast<Component>().Select(flag=>{var observer=flag.gameObject.AddComponent<MoonFlagEnergyDiagnosticObserver>();observer.Configure(flag,f.match,f.Rules,f.Trucks);return observer;}).ToArray();
            Vector3 originalGravity=Physics.gravity;float originalStep=Time.fixedDeltaTime,originalScale=Time.timeScale;var roots=f.Trucks.Cast<Component>().Select(t=>t.GetInstanceID()).ToArray();float deadline=Time.realtimeSinceStartup+55;
            try
            {
                while((float)Get(f.match,"Clock")<50&&!(bool)Get(f.match,"Finished")&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That((float)Get(f.match,"Clock"),Is.GreaterThanOrEqualTo(50),"Bounded diagnostic must observe fifty seconds of genuine natural eight-AI physics; no setup/pose/flag/contact/score/force writes.");
                Assert.That(Physics.gravity,Is.EqualTo(originalGravity));Assert.That(Time.fixedDeltaTime,Is.EqualTo(originalStep));Assert.That(Time.timeScale,Is.EqualTo(originalScale));CollectionAssert.AreEqual(roots,f.Trucks.Cast<Component>().Select(t=>t.GetInstanceID()).ToArray());Assert.That((int)Get(f.match,"TotalCollections"),Is.GreaterThan(0));Assert.That(f.Rules.Scores.Sum(),Is.EqualTo(f.Rules.Deliveries));
            }
            finally
            {
                foreach(var observer in observers)
                {
                    File.WriteAllText(Path.Combine(folder,"flag-"+observer.report.flagId+"-original-natural-physics.json"),JsonUtility.ToJson(observer.report,true));
                    TestContext.WriteLine("MOON_ENERGY_DIAGNOSTIC_ONLY flag="+observer.report.flagId+" entries="+observer.report.samples.Count+" firstSpeedAbove30="+observer.report.firstSpeedBreachClock+" maximumSpeed="+observer.report.maximumSpeed+" maximumFixedStepDeltaV="+observer.report.maximumStepDeltaV+" file="+folder+"; every actual SpringJoint/current owner pointer/callback retained. No full-match acceptance or root-cause inference from diagnostic completion.");
                    UnityEngine.Object.DestroyImmediate(observer);
                }
            }
        }
    }
    [UnityTest,Timeout(125000)]public IEnumerator NaturalHullContactCarriesOriginalFlagIntoActualOwnStationWithoutWheelGroundEnergy()
    {
        using(var f=new Fixture())
        {
            yield return f.Load();var flags=f.Flags.Cast<Component>().ToArray();var bodies=flags.Select(x=>(Rigidbody)Get(x,"body")).ToArray();var shapes=flags.Select(x=>(Collider)Get(x,"shape")).ToArray();var wheels=f.Trucks.Cast<object>().SelectMany(x=>((Array)Get(x,"wheels")).Cast<object>()).ToArray();Assert.That(wheels.Length,Is.EqualTo(32));
            var stations=(Array)Get(f.match,"stations");bool carried=false,pausedWithCargo=false;var eligibleOwners=new HashSet<int>();float maximumSpeed=0,deadline=Time.realtimeSinceStartup+110;
            while(f.Rules.Deliveries==0&&(float)Get(f.match,"Clock")<100&&Time.realtimeSinceStartup<deadline)
            {
                foreach(var wheel in wheels)
                {
                    var set=(HashSet<Collider>)Get(wheel,"vehicleColliders");foreach(var shape in shapes)Assert.That(set.Contains(shape),Is.True,"All five physical flags are excluded only from all32 original NWH ground queries, equally for every owner.");
                    if((bool)Get(wheel,"IsGrounded")){var hit=(Collider)Get(wheel,"HitCollider");Assert.That(hit==null||!bodies.Contains(hit.attachedRigidbody),Is.True,"The existing NWH ground cast must not apply suspension/friction reaction to lightweight cargo.");}
                }
                for(int id=0;id<flags.Length;id++)
                {
                    maximumSpeed=Mathf.Max(maximumSpeed,bodies[id].linearVelocity.magnitude);Assert.That(bodies[id].linearVelocity.magnitude,Is.LessThan(30),"Natural flag energy stays below the diagnostic's30m/s spike boundary; there is no production velocity clamp.");int owner=f.Rules.Owners[id];if(owner<0)continue;
                    var joint=(SpringJoint)Get(flags[id],"Joint");Assert.That(joint,Is.Not.Null);Assert.That(joint.connectedBody,Is.SameAs(f.Body(owner)));Assert.That((int)Get(flags[id],"PhysicalCollections"),Is.GreaterThan(0),"The unchanged Collect API can qualify only a genuine original native hull/tire contact.");
                    float rack=Vector3.Distance(bodies[id].position,((Component)f.Trucks.GetValue(owner)).transform.TransformPoint(new Vector3(0,2.5f,.3f)));
                    if((float)Get(flags[id],"HeldSeconds")>=1&&rack<1.3f)
                    {
                        carried=true;
                        if((bool)Call(stations.GetValue(owner),"Contains",id)&&Vector3.Distance(f.Body(owner).position,((Component)stations.GetValue(owner)).transform.position)<9&&f.Body(owner).linearVelocity.magnitude<3)eligibleOwners.Add(owner);
                        if(!pausedWithCargo)
                        {
                            pausedWithCargo=true;Call(f.match,"Pause",0,false);float clock=(float)Get(f.match,"Clock");Vector3 position=bodies[id].position,velocity=bodies[id].linearVelocity;yield return new WaitForSecondsRealtime(.12f);Assert.That((float)Get(f.match,"Clock"),Is.EqualTo(clock));Assert.That(bodies[id].position,Is.EqualTo(position));Assert.That(bodies[id].linearVelocity,Is.EqualTo(velocity));Assert.That(Get(flags[id],"Joint"),Is.SameAs(joint));Call(f.match,"Resume");
                        }
                    }
                }
                yield return null;
            }
            Assert.That(carried&&pausedWithCargo,Is.True,"A real original spring must physically carry and pause with genuine native-contact cargo.");Assert.That(f.Rules.Deliveries,Is.GreaterThan(0),"A focused first genuine delivery is required before the unchanged300s/>1-delivery full-match gate.");Assert.That(f.Rules.Scores.Sum(),Is.EqualTo(f.Rules.Deliveries));foreach(int owner in Enumerable.Range(0,8).Where(i=>f.Rules.Scores[i]>0))Assert.That(eligibleOwners.Contains(owner),Is.True,"Actual same-owner station entry, rack rest and slow original truck were observed before official scoring.");
            TestContext.WriteLine("MOON_NATURAL_QUERY_REPAIR firstDeliveries="+f.Rules.Deliveries+" physicalCollections="+Get(f.match,"TotalCollections")+" maximumFlagSpeed="+maximumSpeed+" clock="+Get(f.match,"Clock")+"; original5flags/8NWH/nativepickup/spring/station/half-secondrest/points criteria unchanged, no forced poses or scoring.");Capture(f,"natural-query-repair-actual-first-station-delivery");
        }
    }
    [UnityTest,Timeout(35000)]public IEnumerator OriginalGroundQueryMembershipReinitializationPauseAndRematchRestoreExactly()
    {
        using(var f=new Fixture())
        {
            yield return f.Load();Call(f.match,"SetupScreen");var wheels=f.Trucks.Cast<object>().SelectMany(x=>((Array)Get(x,"wheels")).Cast<object>()).ToArray();var shapes=f.Flags.Cast<object>().Select(x=>(Collider)Get(x,"shape")).ToArray();var first=(HashSet<Collider>)Get(wheels[0],"vehicleColliders");var second=(HashSet<Collider>)Get(wheels[1],"vehicleColliders");var unrelated=(Collider)Get(((Array)Get(f.match,"stations")).GetValue(0),"volume");bool hadFlag=first.Contains(shapes[0]),hadOther=second.Contains(unrelated);first.Add(shapes[0]);second.Add(unrelated);
            var baseline=wheels.Select(w=>((HashSet<Collider>)Get(w,"vehicleColliders")).ToArray()).ToArray();var masks=wheels.Select(w=>(LayerMask)Get(w,"layerMask")).ToArray();var scales=wheels.Select(w=>(float)Get(w,"otherBodyForceScale")).ToArray();var matrix=new bool[32,32];for(int a=0;a<32;a++)for(int b=0;b<32;b++)matrix[a,b]=Physics.GetIgnoreLayerCollision(a,b);
            try
            {
                for(int round=0;round<2;round++)
                {
                    Call(f.match,"AllBots");Assert.That((bool)Call(f.match,"StartMatch"),Is.True);yield return Simulated(f,.08f,"Actual ground-filter rematch initialization");
                    if(round==0)
                    {
                        // Controlled collection replacement models NWH Initialize's
                        // documented HashSet rebuild, without regenerating any collider.
                        var replacement=new HashSet<Collider>(baseline[0]);wheels[0].GetType().GetField("vehicleColliders").SetValue(wheels[0],replacement);yield return Simulated(f,.06f,"Original vendor ground-filter collection replacement");CollectionAssert.AreEquivalent(baseline[0],first,"Only this mode's additions are removed from the superseded vendor set.");
                    }
                    for(int i=0;i<32;i++){var actual=(HashSet<Collider>)Get(wheels[i],"vehicleColliders");foreach(var shape in shapes)Assert.That(actual.Contains(shape),Is.True);Assert.That((LayerMask)Get(wheels[i],"layerMask"),Is.EqualTo(masks[i]));Assert.That((float)Get(wheels[i],"otherBodyForceScale"),Is.EqualTo(scales[i]));}
                    Call(f.match,"Pause",0,false);yield return new WaitForSecondsRealtime(.1f);foreach(var w in wheels)foreach(var shape in shapes)Assert.That(((HashSet<Collider>)Get(w,"vehicleColliders")).Contains(shape),Is.True);Call(f.match,"Resume");Call(f.match,"SetupScreen");
                    for(int i=0;i<32;i++)CollectionAssert.AreEquivalent(baseline[i],(HashSet<Collider>)Get(wheels[i],"vehicleColliders"),"Mode exit restores exact preexisting flag/unrelated/native membership.");
                    for(int a=0;a<32;a++)for(int b=0;b<32;b++)Assert.That(Physics.GetIgnoreLayerCollision(a,b),Is.EqualTo(matrix[a,b]));Assert.That(f.Rules.Scores.Sum(),Is.Zero);
                }
            }
            finally
            {
                Call(f.match,"SetupScreen");var current=(HashSet<Collider>)Get(wheels[0],"vehicleColliders");if(!hadFlag){first.Remove(shapes[0]);current.Remove(shapes[0]);}if(!hadOther)second.Remove(unrelated);wheels[0].GetType().GetField("vehicleColliders").SetValue(wheels[0],first);
            }
        }
    }
    static IEnumerator UntilDiagnostic(Fixture f,float seconds){float deadline=Time.realtimeSinceStartup+seconds+15;while((float)Get(f.match,"Clock")<seconds&&!(bool)Get(f.match,"Finished")&&Time.realtimeSinceStartup<deadline)yield return null;Assert.That((float)Get(f.match,"Clock")>=seconds||(bool)Get(f.match,"Finished"),Is.True,"Diagnostic requires actual original simulation progression, never a frozen timer.");}
    [UnityTest,Timeout(60000)]public IEnumerator ActualAiObservesAuthoredCraterDropHopsAndUsesFiniteLandingJets()
    {
        using(var f=new Fixture())
        {
            yield return f.Load();yield return Simulated(f,3.2f,"Countdown");var truck=f.Trucks.GetValue(0);var pilot=f.Pilots.GetValue(0);
            Call(truck,"Prepare",new Vector3(18,.8f,62),Quaternion.LookRotation(Vector3.back),true);f.Body(0).AddForce(Vector3.back*5,ForceMode.VelocityChange);
            TestContext.WriteLine("Controlled AI encounter: original full assembly at the actual authored north crater edge, original AI and objective selection retained; only initial physical pose/impulse controlled.");
            float deadline=Time.realtimeSinceStartup+8;while((int)Get(pilot,"Hops")==0&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That((int)Get(pilot,"Hops"),Is.GreaterThan(0),"Actual AI must observe the real ground-height drop and request the original bounded hop.");
            deadline=Time.realtimeSinceStartup+3;while((float)Get(pilot,"ThrustSeconds")<=.01f&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That((float)Get(pilot,"ThrustSeconds"),Is.GreaterThan(.01f));Assert.That((float)Get(pilot,"Fuel"),Is.LessThan(1));Assert.That(f.Body(0).position.y,Is.GreaterThan(.8f));Assert.That(f.Rules.Scores.Sum(),Is.Zero);Capture(f,"controlled-authored-crater-real-AI-hop");
        }
    }
    [UnityTest,Timeout(60000)]public IEnumerator ActualOriginalAssemblyFallsAtOneSixthGravityAndFiniteJetsCannotHover()
    {
        using(var f=new Fixture())
        {
            yield return f.Load();Call(f.match,"Pause",0,false);yield return null;Vector3 global=Physics.gravity;var mode=Physics.simulationMode;
            var truck=f.Trucks.GetValue(0);var pilot=f.Pilots.GetValue(0);Call(truck,"Prepare",new Vector3(0,45,0),Quaternion.identity,false);
            var assembly=(Rigidbody[])Get(pilot,"Assembly");Assert.That(assembly.Contains(f.Body(0)),Is.True);float damping=f.Body(0).linearDamping;
            try
            {
                Physics.simulationMode=SimulationMode.Script;Physics.SyncTransforms();
                var moving=assembly.Where(b=>!b.isKinematic).ToArray();Assert.That(moving.Length,Is.EqualTo(2),"The real original chassis AND suspended Cab must participate.");
                Assert.That(moving.All(b=>b.useGravity&&b.linearDamping==0),Is.True,"Keep actual undamped source bodies; no fixture coefficient edits.");
                foreach(var joint in moving.SelectMany(b=>b.GetComponents<Joint>()))TestContext.WriteLine("MOON_ORIGINAL_JOINT type="+joint.GetType().Name+" owner="+joint.name+" connected="+(joint.connectedBody!=null?joint.connectedBody.name:"WORLD")+" anchor="+joint.anchor.ToString("F7")+" connectedAnchor="+joint.connectedAnchor.ToString("F7")+" worldAnchor="+joint.transform.TransformPoint(joint.anchor).ToString("F7")+" connectedWorld="+(joint.connectedBody!=null?joint.connectedBody.transform.TransformPoint(joint.connectedAnchor):joint.connectedAnchor).ToString("F7"));
                var before=moving.Select(b=>b.linearVelocity).ToArray();float mass=moving.Sum(b=>b.mass),beforeCom=moving.Sum(b=>b.mass*b.linearVelocity.y)/mass;
                for(int i=0;i<10;i++){Call(pilot,"GravityStep");Physics.Simulate(.02f);}
                float actual=(moving.Sum(b=>b.mass*b.linearVelocity.y)/mass-beforeCom)/.2f;
                TestContext.WriteLine("MOON_ASSEMBLY gravity="+global+" actualCOMAccelerationY="+actual+" totalMass="+mass+" assembly="+assembly.Length+" rootDamping="+damping+" bodies="+string.Join(";",moving.Select((b,i)=>b.name+" mass="+b.mass+" before="+before[i].ToString("F7")+" after="+b.linearVelocity.ToString("F7")+" aY="+((b.linearVelocity.y-before[i].y)/.2f).ToString("R"))));
                Assert.That(actual,Is.EqualTo(global.y/6).Within(.05f),"Whole-assembly COM removes original internal suspended-Cab joint forces; same strict physical acceleration bound.");Assert.That(Physics.gravity,Is.EqualTo(global));
                Vector3 right=f.Body(0).rotation*Vector3.right;float oldPitch=Vector3.Dot(f.Body(0).angularVelocity,right);for(int i=0;i<2;i++){Call(pilot,"GravityStep");Call(pilot,"Step",new Vector2(0,.8f),true,false,.02f,i*.02f);Physics.Simulate(.02f);}Assert.That(Vector3.Dot(f.Body(0).angularVelocity,right)-oldPitch,Is.GreaterThan(.002f),"Actual finite fore/aft jet torque must physically change the original chassis pitch.");
                int steps=0;
                while((float)Get(pilot,"Fuel")>0&&steps++<260){Call(pilot,"GravityStep");Call(pilot,"Step",new Vector2(.7f,.5f),true,false,.02f,steps*.02f);Physics.Simulate(.02f);}
                Assert.That((float)Get(pilot,"Fuel"),Is.Zero);Assert.That(steps,Is.LessThan(260));Assert.That((float)Get(pilot,"ThrustSeconds"),Is.GreaterThan(3));Assert.That(f.Body(0).linearVelocity.y,Is.LessThan(-1),"Correction thrust stays below lunar weight and cannot hover.");
                float spent=(float)Get(pilot,"ThrustSeconds");for(int i=0;i<10;i++){Call(pilot,"GravityStep");Call(pilot,"Step",Vector2.one,true,false,.02f,6+i*.02f);Physics.Simulate(.02f);}Assert.That((float)Get(pilot,"ThrustSeconds"),Is.EqualTo(spent));
            }
            finally{Physics.simulationMode=mode;Assert.That(Physics.gravity,Is.EqualTo(global));}
        }
    }
    [UnityTest,Timeout(90000)]public IEnumerator RealCargoCollisionCanDetachOnceAndMeteorWarningPrecedesActualPhysicalImpact()
    {
        using(var f=new Fixture())
        {
            yield return f.Load();var keyboard=InputSystem.AddDevice<Keyboard>();InputSystem.EnableDevice(keyboard);keyboard.MakeCurrent();try
            {
                Source(f.match,0,"Keyboard");InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();yield return Simulated(f,3.2f,"Countdown");
                var flag=f.Flags.GetValue(0);var flagBody=(Rigidbody)Get(flag,"body");Vector3 p=flagBody.position;
                var truck=f.Trucks.GetValue(0);var motor=Get(truck,"tractorMotor");
                var ground=Physics.RaycastAll(p+Vector3.up*20,Vector3.down,40,1<<0,QueryTriggerInteraction.Ignore).Where(h=>h.collider.attachedRigidbody==null).OrderBy(h=>h.distance).ToArray();Assert.That(ground.Length,Is.GreaterThan(0),"Controlled contact uses actual lunar terrain height.");
                Call(truck,"Prepare",new Vector3(p.x,ground[0].point.y+.7f,p.z-12),Quaternion.identity,true);Physics.SyncTransforms();
                var physical=((Component)truck).GetComponentsInChildren<Collider>(true).Where(c=>c.enabled&&!c.isTrigger).ToArray();var envelope=physical[0].bounds;foreach(var c in physical)envelope.Encapsulate(c.bounds);var flagShape=(Collider)Get(flag,"shape");
                float front=envelope.max.z-f.Body(0).position.z;Vector3 start=new Vector3(flagShape.bounds.center.x,ground[0].point.y+.7f,flagShape.bounds.min.z-front-1);Call(truck,"Prepare",start,Quaternion.identity,true);Physics.SyncTransforms();
                TestContext.WriteLine("MOON_CONTROLLED_APPROACH original bodies="+((Rigidbody[])Get(f.Pilots.GetValue(0),"Assembly")).Length+" originalNwhGround="+ground[0].point+" physicalFrontOffset="+front+" initialGap="+(flagShape.bounds.min.z-physical.Max(c=>c.bounds.max.z))+" flag="+flagBody.position+" owner="+f.Rules.Owners[0]);Call(motor,"AddAssemblyVelocity",Vector3.forward*2);
                float deadline=Time.realtimeSinceStartup+8,nextContact=0;while(!(bool)Call(flag,"Touching",0)&&Time.realtimeSinceStartup<deadline){if(Time.realtimeSinceStartup>=nextContact){Debug.Log("MOON_CONTROLLED_CONTACT clock="+Get(f.match,"Clock")+" root="+f.Body(0).position.ToString("F5")+" v="+f.Body(0).linearVelocity.ToString("F5")+" flag="+flagBody.position.ToString("F5")+" flagV="+flagBody.linearVelocity.ToString("F5")+" owner="+f.Rules.Owners[0]);nextContact=Time.realtimeSinceStartup+.5f;}yield return null;}
                Assert.That((bool)Call(flag,"Touching",0),Is.True,"Controlled physical approach must produce a genuine contact callback.");Assert.That((bool)Call(flag,"Collect",0),Is.True);Assert.That(Get(flag,"Joint"),Is.Not.Null);Assert.That(f.Rules.Scores.Sum(),Is.Zero);
                yield return Simulated(f,1.2f,"Physical spring acquisition");
                var other=f.Trucks.GetValue(1);Source(f.match,1,"Keyboard");Call(other,"Prepare",f.Body(0).position+Vector3.right*10,Quaternion.LookRotation(Vector3.left),true);Call(Get(other,"tractorMotor"),"AddAssemblyVelocity",Vector3.left*10);
                deadline=Time.realtimeSinceStartup+5;while(f.Rules.Carried(0)>=0&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(f.Rules.Carried(0),Is.EqualTo(-1),"A real truck collision must dislodge the physical flag joint.");Assert.That((int)Get(f.match,"CollisionDrops"),Is.EqualTo(1));Assert.That(f.Rules.Scores.Sum(),Is.Zero);
                var meteors=(Array)Get(f.match,"meteors");deadline=Time.realtimeSinceStartup+20;bool warning=false,falling=false,placed=false;Vector3 preImpactVelocity=Vector3.zero;Rigidbody hazardBody=null;object chosenMeteor=null;
                while((chosenMeteor==null||(int)Get(chosenMeteor,"Impacts")==0)&&Time.realtimeSinceStartup<deadline)
                {
                    var warned=meteors.Cast<object>().FirstOrDefault(m=>(float)Get(m,"WarningSeconds")>2);warning|=meteors.Cast<object>().Any(m=>(float)Get(m,"WarningSeconds")>0);falling|=meteors.Cast<object>().Any(m=>(bool)Get(m,"Falling"));
                    if(!placed&&warned!=null){Vector3 impactPoint=(Vector3)Get(warned,"ImpactPoint");var actor=f.Trucks.GetValue(2);Source(f.match,2,"Keyboard");Call(actor,"Prepare",impactPoint+new Vector3(4,.8f,0),Quaternion.identity,false);hazardBody=f.Body(2);chosenMeteor=warned;placed=true;TestContext.WriteLine("Controlled physical hazard fixture only: existing actor placed beside the actual originally scheduled warning; meteor state/outcomes are untouched. point="+impactPoint);}
                    if(hazardBody!=null)preImpactVelocity=hazardBody.linearVelocity;yield return null;
                }
                Assert.That(warning&&falling&&placed,Is.True);Assert.That((int)Get(chosenMeteor,"Impacts"),Is.EqualTo(1));yield return new WaitForFixedUpdate();Assert.That(Vector3.ProjectOnPlane(hazardBody.linearVelocity-preImpactVelocity,Vector3.up).magnitude,Is.GreaterThan(.05f),"The actual warned meteor collision/explosion must physically change the original nearby chassis velocity.");Capture(f,"controlled-real-contact-and-original-meteor-flow");
            }finally{InputSystem.RemoveDevice(keyboard);}
        }
    }
    [UnityTest,Timeout(70000)]public IEnumerator GenuineContactAcquiredFlagReleaseCannotGainExplosiveEnergyBeforeDeferredDestroy()
    {
        using(var f=new Fixture()){yield return f.Load();var keyboard=InputSystem.AddDevice<Keyboard>();InputSystem.EnableDevice(keyboard);keyboard.MakeCurrent();var mode=Physics.simulationMode;try
        {
            Source(f.match,0,"Keyboard");InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();yield return Simulated(f,3.2f,"Original countdown before controlled real contact");
            var flag=f.Flags.GetValue(0);var body=(Rigidbody)Get(flag,"body");var shape=(Collider)Get(flag,"shape");var truck=f.Trucks.GetValue(0);var motor=Get(truck,"tractorMotor");Vector3 p=body.position;
            var ground=Physics.RaycastAll(p+Vector3.up*20,Vector3.down,40,1,QueryTriggerInteraction.Ignore).Where(h=>h.collider.attachedRigidbody==null).OrderBy(h=>h.distance).First();
            // Controlled initial geometry only: the same original flag and whole
            // original NWH assembly approach through a genuine one-metre gap,
            // raised together before contact. Ground depenetration is not joint energy.
            body.position=p+Vector3.up*2;body.transform.SetPositionAndRotation(body.position,body.rotation);
            Call(truck,"Prepare",new Vector3(p.x,ground.point.y+2.7f,p.z-12),Quaternion.identity,true);Physics.SyncTransforms();var physical=((Component)truck).GetComponentsInChildren<Collider>(true).Where(c=>c.enabled&&!c.isTrigger).ToArray();float front=physical.Max(c=>c.bounds.max.z)-f.Body(0).position.z;Call(truck,"Prepare",new Vector3(shape.bounds.center.x,ground.point.y+2.7f,shape.bounds.min.z-front-1),Quaternion.identity,true);Physics.SyncTransforms();
            float Clearance(){var bounds=shape.bounds;var hits=Physics.BoxCastAll(bounds.center+Vector3.up*20,new Vector3(bounds.extents.x,.01f,bounds.extents.z),Vector3.down,Quaternion.identity,60,1,QueryTriggerInteraction.Ignore).Where(h=>h.collider.attachedRigidbody==null).OrderBy(h=>h.distance).ToArray();Assert.That(hits.Length,Is.GreaterThan(0),"Original real lunar terrain remains present under the complete flag footprint.");return bounds.min.y-hits[0].point.y;}
            string evidence=Path.GetFullPath("Logs/TruckSports/PlaytestRepair/Impossible07MoonReleaseAirborneNative/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")+".txt");Directory.CreateDirectory(Path.GetDirectoryName(evidence));var trace=new System.Text.StringBuilder();void Trace(string label){trace.AppendLine(label+" clock="+Get(f.match,"Clock")+" flag="+body.position.ToString("F7")+" velocity="+body.linearVelocity.ToString("F7")+" flagBounds="+shape.bounds.ToString("F7")+" clearance="+Clearance().ToString("R")+" truck="+f.Body(0).position.ToString("F7"));File.WriteAllText(evidence,trace.ToString());}
            float gap=shape.bounds.min.z-physical.Max(c=>c.bounds.max.z);Assert.That(gap,Is.EqualTo(1).Within(.001f));Assert.That(Clearance(),Is.GreaterThan(1));Assert.That(physical.Where(c=>c.gameObject.activeInHierarchy).Min(c=>c.bounds.min.y)-ground.point.y,Is.GreaterThan(1),"The whole original NWH contact setup is airborne too.");Trace("PRECONTACT original physical gap="+gap+" translated2m above unchanged real terrain");Call(motor,"AddAssemblyVelocity",Vector3.forward*2);
            float deadline=Time.realtimeSinceStartup+8;while(!(bool)Call(flag,"Touching",0)&&Time.realtimeSinceStartup<deadline)yield return null;Assert.That((bool)Call(flag,"Touching",0),Is.True,"Original physical callback acquires the source flag; no contact state injection.");Assert.That((bool)Call(flag,"Collect",0),Is.True);var retiring=(SpringJoint)Get(flag,"Joint");Assert.That(retiring,Is.Not.Null);Vector3 before=body.linearVelocity;var connected=retiring.connectedBody;Trace("GENUINE CONTACT AND ACQUISITION");Assert.That(Clearance(),Is.GreaterThan(1),"Real flag contact/acquisition occurs while its full collider stays clear of original terrain.");
            Call(f.match,"DropFlag",0,"CONTROLLED ACTUAL RELEASE API");Call(f.match,"Pause",0,false);Assert.That(f.Rules.Owners[0],Is.EqualTo(-1));Assert.That(retiring.connectedBody,Is.SameAs(connected),"A retired local anchor must never become a world tether.");Assert.That(retiring.spring,Is.Zero);Assert.That(retiring.damper,Is.Zero);Assert.That(retiring.minDistance,Is.Zero);Assert.That(retiring.maxDistance,Is.GreaterThan(100000));
            Physics.simulationMode=SimulationMode.Script;for(int step=0;step<3;step++){Trace("BEFORE RELEASE SUBSTEP "+step);Assert.That(Clearance(),Is.GreaterThan(1),"Release interval isolates joint retirement from real terrain contact without disabling any collider.");foreach(var pilot in f.Pilots)Call(pilot,"GravityStep");foreach(var item in f.Flags)Call(item,"Step",.02f);Physics.Simulate(.02f);Trace("AFTER RELEASE SUBSTEP "+step);}Assert.That(Clearance(),Is.GreaterThan(1));
            TestContext.WriteLine("MOON_RELEASE_ENERGY before="+before.ToString("F7")+" after="+body.linearVelocity.ToString("F7")+" delta="+(body.linearVelocity-before).magnitude+" p="+body.position.ToString("F7")+" oldConnected="+retiring.connectedBody.name+"; three real substeps BEFORE deferred component destruction, no velocity clamp or counterforce.");
            Assert.That((body.linearVelocity-before).magnitude,Is.LessThan(.3f),"The genuinely contact-acquired flag stays airborne; original lunar gravity/drag must keep its unchanged absolute delta-v below0.3 while the retiring spring cannot add explosive energy.");Assert.That(f.Rules.Scores.Sum(),Is.Zero);Assert.That(Get(flag,"Joint"),Is.Null);
        }finally{Physics.simulationMode=mode;InputSystem.RemoveDevice(keyboard);}}
    }
    [UnityTest,Timeout(65000)]public IEnumerator OriginalNwhAiStraightReverseUsesSignedHeadingAndSpeedWithoutFullLockOrbit()
    {
        using(var f=new Fixture())
        {
            yield return f.Load();yield return Simulated(f,3.2f,"Actual lunar countdown");var truck=f.Trucks.GetValue(0);Call(truck,"Prepare",new Vector3(0,.8f,40),Quaternion.identity,true);Vector3 initial=f.Body(0).position;
            var signed=f.match.GetType().GetMethod("SignedDrive",BindingFlags.Static|BindingFlags.NonPublic);Assert.That(signed,Is.Not.Null);var direct=(Vector2)signed.Invoke(null,new object[]{180f,0f,3f});Assert.That(Mathf.Abs(direct.x),Is.LessThan(.0001f),"A goal exactly behind must not command full-lock reverse orbit.");Assert.That(direct.y,Is.LessThan(0));var overspeed=(Vector2)signed.Invoke(null,new object[]{180f,-4f,3f});Assert.That(overspeed.y,Is.GreaterThan(0),"Original NWH reverse input swaps positive intent to braking when reversing too fast.");
            yield return Simulated(f,3,"Actual original AI/NWH reverse on authored flat corridor");var command=(Vector2)Get(truck,"command");var vehicle=Get(Get(truck,"tractorMotor"),"vehicle");int gear=(int)Get(Get(Get(vehicle,"powertrain"),"transmission"),"Gear");
            TestContext.WriteLine("MOON_REAL_STRAIGHT_REVERSE start="+initial.ToString("F5")+" end="+f.Body(0).position.ToString("F5")+" v="+f.Body(0).linearVelocity.ToString("F5")+" command="+command+" gear="+gear+" yaw="+Quaternion.Angle(f.Body(0).rotation,Quaternion.identity));
            Assert.That(gear,Is.LessThan(0));Assert.That(f.Body(0).position.z,Is.LessThan(initial.z-.5f));Assert.That(Mathf.Abs(f.Body(0).position.x-initial.x),Is.LessThan(.5f));Assert.That(Quaternion.Angle(f.Body(0).rotation,Quaternion.identity),Is.LessThan(5));Assert.That(Mathf.Abs(command.x),Is.LessThan(.05f));Assert.That(f.Rules.Scores.Sum(),Is.Zero,"Controlled initial physical encounter is not a delivery/result injection.");
        }
    }
    static IEnumerator PresentationSettled(){yield return null;yield return null;yield return new WaitForSecondsRealtime(.18f);}
    static void CapturePresentation(Fixture f,string name){string dir=Path.GetFullPath("Logs/TruckSports/PlaytestRepair/Impossible07MoonPresentationV5");Directory.CreateDirectory(dir);var save=Runtime("LWS.TruckSports.SportsControlledCapture").GetMethods().Single(m=>m.Name=="Save"&&m.GetParameters().Length==6);foreach(int w in new[]{1280,1920})save.Invoke(null,new object[]{((Component)Get(f.match,"menus")).GetComponent<Canvas>(),(Camera[])Get(f.match,"cameras"),w,w==1280?720:1080,Path.Combine(dir,name+"-"+w+"-controlled.png"),null});}
    [UnityTest,Timeout(70000)]public IEnumerator OriginalCountdownShowsThreeTwoOneGoThenHidesAndRepeatsAfterRematch()
    {
        using(var f=new Fixture())
        {
            yield return f.Load();var countdown=((Component)f.match).GetComponent(Runtime("LWS.TruckTerrierRacing.TerrierCountdown"));Assert.That(countdown,Is.Not.Null);yield return null;Assert.That((int)Get(countdown,"Beat"),Is.EqualTo(3));
            yield return UntilDiagnostic(f,1.1f);yield return null;Assert.That((int)Get(countdown,"Beat"),Is.EqualTo(2));yield return UntilDiagnostic(f,2.1f);yield return null;Assert.That((int)Get(countdown,"Beat"),Is.EqualTo(1));Call(f.match,"Pause",0,false);yield return PresentationSettled();float clock=(float)Get(f.match,"Clock");yield return new WaitForSecondsRealtime(.2f);Assert.That((float)Get(f.match,"Clock"),Is.EqualTo(clock));Assert.That((int)Get(countdown,"Beat"),Is.EqualTo(-1),"Original countdown hides during actual pause.");
            Call(f.match,"Resume");yield return UntilDiagnostic(f,3.1f);yield return null;Assert.That((int)Get(countdown,"Beat"),Is.EqualTo(0),"Existing shared display receives genuine GO elapsed time.");Assert.That((float)Get(f.match,"RaceCountdownSeconds"),Is.LessThan(0));CapturePresentation(f,"actual-GO");
            yield return UntilDiagnostic(f,4.2f);yield return PresentationSettled();Assert.That((int)Get(countdown,"Beat"),Is.EqualTo(-1));Assert.That(((RectTransform[])Get(countdown,"panels")).All(t=>!t.gameObject.activeSelf),Is.True,"No permanent1medallion obscures live lunar play.");CapturePresentation(f,"live-no-countdown-overlay");Call(f.match,"SetupScreen");Call(f.match,"AllBots");Assert.That((bool)Call(f.match,"StartMatch"),Is.True);yield return null;yield return null;Assert.That((int)Get(countdown,"Beat"),Is.EqualTo(3),"Fresh actual rematch receives a fresh3 without changing the mode clock/physics.");
        }
    }
    [UnityTest,Timeout(180000)]public IEnumerator ActualOneThroughFourChaseSignsStayBoundedAndSettledHudRetainsItsOwner()
    {
        using(var f=new Fixture()){yield return f.Load();var keyboard=InputSystem.AddDevice<Keyboard>();InputSystem.EnableDevice(keyboard);keyboard.MakeCurrent();try
        {
            for(int humans=1;humans<=4;humans++)
            {
                Call(f.match,"SetupScreen");Owners(f.match,humans);Assert.That((bool)Call(f.match,"StartMatch"),Is.True);Call(Get(f.match,"drivingViews"),"Select",1);yield return UntilDiagnostic(f,4.2f);yield return PresentationSettled();
                var labels=((Component)f.match).GetComponentsInChildren<TextMesh>(true).Where(t=>t.name=="Habitat identity").ToArray();Assert.That(labels,Has.Length.EqualTo(8));Assert.That(labels.All(l=>l.GetComponent(Runtime("LWS.TruckSports.SportsPerCameraBillboard"))!=null),Is.True,"Explicit owned8sign author repair must precede this native render proof.");
                var cameras=(Camera[])Get(f.match,"cameras");var compact=((Component)Get(f.match,"menus")).GetComponent(Runtime("LWS.TruckSports.SportsCompactViewportHud"));Assert.That(compact,Is.Not.Null,"Original shared compact viewport adapter must remain integrated.");var cards=(Array)Get(compact,"cards");
                for(int v=0;v<humans;v++){var card=cards.GetValue(v);Assert.That(card,Is.Not.Null);Assert.That(((RectTransform)Get(card,"root")).gameObject.activeInHierarchy,Is.True,"Settled actual owner card must exist after live UI updates.");Assert.That((string)Get(Get(card,"name"),"text"),Is.EqualTo((string)Get(((Array)Get(Get(f.match,"setup"),"players")).GetValue(v),"name")));}
                foreach(int width in new[]{1280,1920})
                {
                    int height=width==1280?720:1080;var rt=RenderTexture.GetTemporary(width,height,24);
                    try{for(int v=0;v<humans;v++)
                    {
                        var c=cameras[v];var old=c.targetTexture;float aspect=c.aspect;try
                        {
                            c.targetTexture=rt;c.aspect=width*c.rect.width/(height*c.rect.height);c.Render();int visible=0;
                            foreach(var label in labels){if(label.transform.localScale.sqrMagnitude<.0000001f)continue;var bounds=label.GetComponent<Renderer>().bounds;Vector2 min=Vector2.one*float.MaxValue,max=Vector2.one*float.MinValue;bool front=true;for(int k=0;k<8;k++){var q=c.WorldToViewportPoint(bounds.center+Vector3.Scale(bounds.extents,new Vector3((k&1)==0?-1:1,(k&2)==0?-1:1,(k&4)==0?-1:1)));front&=q.z>0;min=Vector2.Min(min,q);max=Vector2.Max(max,q);}if(!front||max.x<0||min.x>1||max.y<0||min.y>1)continue;visible++;Vector2 pixels=Vector2.Scale(max-min,new Vector2(width*c.rect.width,height*c.rect.height));Assert.That(pixels.x,Is.LessThanOrEqualTo(144.1f));Assert.That(pixels.y,Is.LessThanOrEqualTo(24.1f));Assert.That(Vector3.Dot(label.transform.forward,c.transform.forward),Is.GreaterThan(.999f),"Existing camera-local facing prevents mirrored roof lettering.");}
                            var nearby=Physics.OverlapSphere(c.transform.position,.35f,1,QueryTriggerInteraction.Ignore).Where(o=>o.attachedRigidbody==null).Select(o=>o.name).ToArray();Debug.Log("MOON_PRESENTATION_RENDER humans="+humans+" view="+v+" output="+width+"x"+height+" eye="+c.transform.position.ToString("F5")+" boundedVisibleSigns="+visible+" staticCameraNear="+string.Join(",",nearby));
                        }finally{c.targetTexture=old;c.aspect=aspect;}
                    }}finally{RenderTexture.ReleaseTemporary(rt);}
                }
                CapturePresentation(f,humans+"-settled-owned-chase-and-signs");
            }
        }finally{InputSystem.RemoveDevice(keyboard);}}
    }
    static void Owners(object match,int humans)
    {
        Key[] forward={Key.W,Key.UpArrow,Key.I,Key.T},left={Key.A,Key.LeftArrow,Key.J,Key.F},back={Key.S,Key.DownArrow,Key.K,Key.G},right={Key.D,Key.RightArrow,Key.L,Key.H};
        var players=(Array)Get(Get(match,"setup"),"players");for(int i=0;i<8;i++){Source(match,i,i<humans?"Keyboard":"AI");if(i<humans){var p=players.GetValue(i);p.GetType().GetField("forward").SetValue(p,forward[i]);p.GetType().GetField("left").SetValue(p,left[i]);p.GetType().GetField("back").SetValue(p,back[i]);p.GetType().GetField("right").SetValue(p,right[i]);}}
        match.GetType().GetField("actionKeys").SetValue(match,new[]{Key.Space,Key.RightShift,Key.R,Key.U,Key.B,Key.Numpad0,Key.Digit0,Key.PageUp});
    }
    [UnityTest,Timeout(150000)]public IEnumerator OneThroughFourOwnersActualNwhMovementPauseAndExactNoncompoundingTuningRestoration()
    {
        using(var f=new Fixture())
        {
            yield return f.Load();var originalRoots=f.Trucks.Cast<Component>().Select(t=>t.gameObject.GetInstanceID()).ToArray();var keyboard=InputSystem.AddDevice<Keyboard>();InputSystem.EnableDevice(keyboard);keyboard.MakeCurrent();try
            {
                for(int humans=1;humans<=4;humans++)
                {
                    InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();Call(f.match,"SetupScreen");var motor=Get(f.Trucks.GetValue(0),"tractorMotor");float baselineGrip=(float)Get(motor,"gripScale");var wheels=(Array)Get(f.Trucks.GetValue(0),"wheels");float[] spring=wheels.Cast<object>().Select(w=>(float)Get(Get(w,"spring"),"maxForce")).ToArray();float[] bump=wheels.Cast<object>().Select(w=>(float)Get(Get(w,"damper"),"bumpRate")).ToArray(),rebound=wheels.Cast<object>().Select(w=>(float)Get(Get(w,"damper"),"reboundRate")).ToArray();var components=((Component)f.Trucks.GetValue(0)).GetComponents<Component>();var aero=components.Single(c=>c.GetType().Name=="AerodynamicsModuleWrapper");var arcade=components.Single(c=>c.GetType().Name=="ArcadeModuleWrapper");bool drag=(bool)Get(Get(aero,"module"),"simulateDrag"),downforce=(bool)Get(Get(aero,"module"),"simulateDownforce");float assist=(float)Get(Get(arcade,"module"),"artificialSteerStrength"),drift=(float)Get(Get(arcade,"module"),"maxDriftAssistForce");
                    Owners(f.match,humans);Assert.That((bool)Call(f.match,"StartMatch"),Is.True);Call(Get(f.match,"drivingViews"),"Select",1);yield return null;yield return null;
                    var rally=Get(Get(f.match,"drivingViews"),"rally");Assert.That((int)Get(rally,"Humans"),Is.EqualTo(humans));CollectionAssert.AreEqual(Enumerable.Range(0,humans).ToArray(),((int[])Get(rally,"owners")).Take(humans).ToArray());
                    for(int w=0;w<wheels.Length;w++)Assert.That((float)Get(Get(wheels.GetValue(w),"spring"),"maxForce"),Is.EqualTo(spring[w]/6));Assert.That((float)Get(motor,"gripScale"),Is.EqualTo(baselineGrip*1.8f));Assert.That((bool)Get(Get(aero,"module"),"simulateDrag"),Is.False);Assert.That((bool)Get(Get(aero,"module"),"simulateDownforce"),Is.False);Assert.That((float)Get(Get(arcade,"module"),"artificialSteerStrength"),Is.EqualTo(assist/6));
                    yield return Simulated(f,3.3f,"Lunar countdown");Vector3 before=f.Body(0).position;InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));InputSystem.Update();yield return Simulated(f,2.5f,"Assigned actual NWH drive");Assert.That(((Vector2[])Get(f.match,"commands"))[0].y,Is.GreaterThan(.9f));Assert.That(Vector3.Distance(f.Body(0).position,before),Is.GreaterThan(.5f));
                    InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();Call(f.match,"Pause",0,false);yield return null;Vector3 position=f.Body(0).position,velocity=f.Body(0).linearVelocity;float clock=(float)Get(f.match,"Clock"),fuel=(float)Get(f.Pilots.GetValue(0),"Fuel");yield return new WaitForSecondsRealtime(.2f);Assert.That(f.Body(0).position,Is.EqualTo(position));Assert.That(f.Body(0).linearVelocity,Is.EqualTo(velocity));Assert.That((float)Get(f.match,"Clock"),Is.EqualTo(clock));Assert.That((float)Get(f.Pilots.GetValue(0),"Fuel"),Is.EqualTo(fuel));Call(f.match,"Resume");yield return null;yield return null;yield return new WaitForSecondsRealtime(.18f);Capture(f,humans+"-assigned-split-original-NWH");Call(f.match,"SetupScreen");
                    Assert.That((float)Get(motor,"gripScale"),Is.EqualTo(baselineGrip));for(int w=0;w<wheels.Length;w++){Assert.That((float)Get(Get(wheels.GetValue(w),"spring"),"maxForce"),Is.EqualTo(spring[w]));Assert.That((float)Get(Get(wheels.GetValue(w),"damper"),"bumpRate"),Is.EqualTo(bump[w]));Assert.That((float)Get(Get(wheels.GetValue(w),"damper"),"reboundRate"),Is.EqualTo(rebound[w]));}Assert.That((bool)Get(Get(aero,"module"),"simulateDrag"),Is.EqualTo(drag));Assert.That((bool)Get(Get(aero,"module"),"simulateDownforce"),Is.EqualTo(downforce));Assert.That((float)Get(Get(arcade,"module"),"artificialSteerStrength"),Is.EqualTo(assist));Assert.That((float)Get(Get(arcade,"module"),"maxDriftAssistForce"),Is.EqualTo(drift));CollectionAssert.AreEqual(originalRoots,f.Trucks.Cast<Component>().Select(t=>t.gameObject.GetInstanceID()).ToArray());
                }
            }finally{InputSystem.RemoveDevice(keyboard);}
        }
    }
}
#endif
