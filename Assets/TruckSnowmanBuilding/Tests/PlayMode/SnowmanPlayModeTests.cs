#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using LWS.TruckSnowmanBuilding;
public sealed class SnowmanPlayModeTests
{
    const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static Type Runtime(string name)=>Type.GetType(name+", Assembly-CSharp",true);
    static object Get(object o,string n)=>o.GetType().GetField(n,Any) is FieldInfo f?f.GetValue(o):o.GetType().GetProperty(n,Any).GetValue(o);
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethods(Any).Single(m=>m.Name==n&&m.GetParameters().Length==a.Length).Invoke(o,a);
    sealed class Fixture:IDisposable
    {
        readonly IDisposable prefs;readonly InputSettings original,temporary;readonly GameObject isolate;public Behaviour match;
        public Component[] Balls=>((Array)Get(match,"balls")).Cast<Component>().ToArray();public SnowmanRules Rules=>(SnowmanRules)Get(match,"Rules");
        public Fixture(){prefs=(IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"Snowman07Native"});original=InputSystem.settings;temporary=UnityEngine.Object.Instantiate(original);temporary.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;temporary.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=temporary;isolate=new GameObject("Snowman original input isolation");UnityEngine.Object.DontDestroyOnLoad(isolate);isolate.AddComponent(Runtime("LWS.TruckSports.SportsProbeInputIsolation"));}
        public IEnumerator Load(){Assert.That(File.Exists("Assets/TruckSnowmanBuilding/Scenes/TruckSnowmanBuilding.unity"),Is.True,"Explicit new-scene author must run first.");EditorSceneManager.LoadSceneInPlayMode("Assets/TruckSnowmanBuilding/Scenes/TruckSnowmanBuilding.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;match=UnityEngine.Object.FindObjectsByType(Runtime("LWS.TruckSnowmanBuilding.SnowmanMatch")).Cast<Behaviour>().Single();Call(match,"AllBots");Assert.That((bool)Call(match,"StartMatch"),Is.True,"Original all-AI session starts.");yield return null;}
        public void Dispose(){Time.timeScale=1;if(match!=null){match.enabled=true;Call(match,"SetupScreen");}UnityEngine.Object.DestroyImmediate(isolate);InputSystem.settings=original;UnityEngine.Object.DestroyImmediate(temporary);prefs.Dispose();}
    }
    static IEnumerator Until(Fixture f,Func<bool> predicate,float seconds,string label){float end=Time.realtimeSinceStartup+seconds;while(!predicate()&&Time.realtimeSinceStartup<end)yield return null;Assert.That(predicate(),Is.True,label+" paused="+Get(f.match,"Paused")+" scale="+Time.timeScale+" phase="+Get(f.match,"LiftRole"));}
    static void Capture(Fixture f,string name){string dir=Path.GetFullPath("Logs/TruckSports/PlaytestRepair/Impossible07Snowman");Directory.CreateDirectory(dir);var canvas=((Component)Get(f.match,"menus")).GetComponent<Canvas>();var cameras=(Camera[])Get(f.match,"cameras");var method=Runtime("LWS.TruckSports.SportsControlledCapture").GetMethods().Single(m=>m.Name=="Save"&&m.GetParameters().Length==6);foreach(int w in new[]{1280,1920})method.Invoke(null,new object[]{canvas,cameras,w,w==1280?720:1080,Path.Combine(dir,name+"-"+w+"-controlled.png"),null});}
    [UnityTest,Timeout(660000)]public IEnumerator NaturalAiCrewActuallyRollsGrowsStacksDecoratesAndCompletesOnce()
    {
        using(var f=new Fixture()){yield return f.Load();var balls=f.Balls;var roots=balls.Select(b=>(Rigidbody)Get(b,"body")).ToArray();float deadline=Time.realtimeSinceStartup+540,next=0;bool captured=false;while(!(bool)Get(f.match,"Finished")&&Time.realtimeSinceStartup<deadline){if(Time.realtimeSinceStartup>=next){Debug.Log("SNOWMAN_NATURAL clock="+Get(f.match,"Clock")+" placed="+f.Rules.Placed+" decor="+f.Rules.Decorations+" lift="+Get(f.match,"LiftRole")+" interceptions="+Get(f.match,"PhysicalInterceptions")+" AIphases="+string.Join(",",(int[])Get(f.match,"AiDeliveryPhase"))+" balls="+string.Join(";",balls.Select(b=>Get(b,"radius")+"/"+Get(b,"Rolled")+"@"+((Rigidbody)Get(b,"body")).position)));next=Time.realtimeSinceStartup+15;}if(!captured&&f.Rules.Placed>=1){Capture(f,"natural-first-physical-pack");captured=true;}yield return null;}Assert.That((bool)Get(f.match,"Finished"),Is.True,"Original AI must complete the actual whole workshop.");Assert.That(f.Rules.Placed,Is.EqualTo(3));Assert.That(f.Rules.Decorations,Is.EqualTo(63));Assert.That(f.Rules.Score,Is.GreaterThan(200));Assert.That(balls.All(b=>(bool)Get(b,"Packed")&&(float)Get(b,"Rolled")>=8),Is.True);Assert.That((int)Get(Get(f.match,"hoist"),"Pickups"),Is.GreaterThanOrEqualTo(3));Assert.That(((GameObject[])Get(f.match,"installed")).All(g=>g!=null&&g.activeInHierarchy&&g.GetComponentsInChildren<Renderer>().Length>0),Is.True,"Every actual decoration family must be visibly installed.");Assert.That((int)Get(Get(f.match,"ScoreJournal"),"Count"),Is.EqualTo(1));CollectionAssert.AreEqual(roots,f.Balls.Select(b=>(Rigidbody)Get(b,"body")).ToArray());Capture(f,"natural-completed-team-grade");}
    }
    [UnityTest,Timeout(110000)]public IEnumerator OriginalNaturalDecoratingRouteObservationWithoutProgressInjection()
    {
        using(var f=new Fixture())
        {
            yield return f.Load();
            string folder=Path.GetFullPath("Logs/TruckSports/PlaytestRepair/SnowmanDecoratingRoute/"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
            string path=Path.Combine(folder,"actual-route.txt");
            var actors=((Array)Get(f.match,"trucks")).Cast<Component>().ToArray();float originalStep=Time.fixedDeltaTime;
            float end=Time.realtimeSinceStartup+75,next=0;bool captured=false;Time.timeScale=8;
            try
            {
                while(Time.realtimeSinceStartup<end&&(float)Get(f.match,"Clock")<535&&!(bool)Get(f.match,"Finished"))
                {
                    float clock=(float)Get(f.match,"Clock");
                    if(clock>=next)
                    {
                        next=clock+4;
                        string state="clock="+clock+" placed="+f.Rules.Placed+" decor="+f.Rules.Decorations+" stable="+Get(f.match,"Stable")+" families="+string.Join(",",(int[])Get(f.match,"family"))+" carried="+string.Join(",",(int[])Get(f.match,"carried"));
                        foreach(var actor in actors)
                        {
                            var body=(Rigidbody)Get(actor,"body");var motor=Get(actor,"tractorMotor");var vehicle=Get(motor,"vehicle");
                            state+="\n owner="+Get(actor,"slot")+" position="+body.position.ToString("F4")+" velocity="+body.linearVelocity.ToString("F4")+" forward="+actor.transform.forward.ToString("F4")+" command="+Get(actor,"command")+" gear="+Get(Get(Get(vehicle,"powertrain"),"transmission"),"Gear");
                        }
                        File.AppendAllText(path,state+"\n");
                    }
                    if(f.Rules.Placed==3&&!captured){captured=true;Capture(f,"natural-three-packed-decoration-route-observation");}
                    yield return null;
                }
                Assert.That(Time.fixedDeltaTime,Is.EqualTo(originalStep),"Observation preserves the original physics step.");
                TestContext.WriteLine("Measurement only; accelerated wall-clock observation, original AI/physics/progress, no completion acceptance. "+path+" finalClock="+Get(f.match,"Clock")+" placed="+f.Rules.Placed+" decor="+f.Rules.Decorations);
            }
            finally{Time.timeScale=1;}
        }
    }
    [UnityTest,Timeout(120000)]public IEnumerator NaturalAiPhysicallyDeliversBaseIntoRealCollectionRackInsideOriginalHoistZone()
    {
        using(var f=new Fixture())
        {
            yield return f.Load();var body=(Rigidbody)Get(f.Balls[0],"body");var rack=(Component)Get(f.match,"collectionRack");
            Assert.That(rack,Is.Not.Null,"Explicit additive collection author must run before this native test.");
            yield return Until(f,()=>(int)Get(f.match,"LiftRole")==0,45,"Original AI must physically push its base into the visible catch, settle and satisfy the unchanged lift gate");
            Assert.That(((int[])Get(rack,"RealContacts"))[0],Is.GreaterThan(0),"The actual base must contact an actual rack collider; another worker's contact is insufficient.");
            Assert.That(((float[])Get(rack,"FirstContactClock"))[0],Is.GreaterThanOrEqualTo(0));
            Assert.That(body.isKinematic,Is.False);Assert.That(body.position.z,Is.InRange(-11f,5f));Assert.That(Mathf.Abs(body.position.x),Is.LessThanOrEqualTo(17));Assert.That(body.linearVelocity.magnitude,Is.LessThanOrEqualTo(.7f));Assert.That((float)Get(f.Balls[0],"radius"),Is.GreaterThanOrEqualTo(1.8f));Assert.That((float)Get(f.Balls[0],"Rolled"),Is.GreaterThanOrEqualTo(8));
            TestContext.WriteLine("SNOWMAN_V4_REAL_RACK_DELIVERY clock="+Get(f.match,"Clock")+" position="+body.position.ToString("F6")+" velocity="+body.linearVelocity.ToString("F6")+" phase="+string.Join(",",(int[])Get(f.match,"AiDeliveryPhase"))+" contacts="+string.Join(",",(int[])Get(rack,"RealContacts"))+" firstContact="+((float[])Get(rack,"FirstContactClock"))[0]);
            float deliveredY=body.position.y;yield return Until(f,()=>(int)Get(Get(f.match,"hoist"),"Pickups")>0,45,"Original physical hoist must acquire the still-dynamic delivered base");Assert.That(body.GetComponent<FixedJoint>(),Is.Not.Null);Assert.That(body.isKinematic,Is.False);
            yield return Until(f,()=>body.position.y>deliveredY+1,15,"Actual dynamic snowball must rise more than one metre under its original physical spreader joint");Assert.That(body.isKinematic,Is.False);Assert.That(body.GetComponent<FixedJoint>(),Is.Not.Null);
            yield return null;yield return null;yield return new WaitForSecondsRealtime(.18f);Capture(f,"natural-real-collection-rack-and-hoist-load");
        }
    }
    [UnityTest,Timeout(150000)]public IEnumerator VisibleCollectionRackActuallyStopsAllValidSphereSizesWithoutScoreAndResetsOnRematch()
    {
        using(var f=new Fixture())
        {
            yield return f.Load();f.match.enabled=false;var rack=(Component)Get(f.match,"collectionRack");var planks=(BoxCollider[])Get(rack,"planks");
            Assert.That(planks.Length,Is.EqualTo(19));Assert.That(rack.GetComponentsInChildren<Rigidbody>(true),Is.Empty,"Static physical timber only; no alternate physics authority.");
            foreach(var plank in planks){Assert.That(plank.isTrigger,Is.False);Assert.That(plank.enabled,Is.True);Assert.That(plank.GetComponent<Renderer>().enabled,Is.True);Assert.That(plank.GetComponent<Renderer>().sharedMaterial,Is.Not.Null);Assert.That(plank.sharedMaterial,Is.SameAs(((SphereCollider)Get(f.Balls[0],"sphere")).sharedMaterial));Assert.That(plank.bounds.max.y,Is.LessThanOrEqualTo(2.5001f));Assert.That(plank.bounds.max.z,Is.LessThan(5));}
            foreach(var truck in ((Array)Get(f.match,"trucks")).Cast<Component>()){Call(truck,"Power",false);((Rigidbody)Get(truck,"body")).isKinematic=true;}
            var balls=f.Balls;foreach(var ball in balls)((Rigidbody)Get(ball,"body")).isKinematic=true;
            // Controlled physical geometry fixture, distinct from the uninjected AI test:
            // original snow bodies, radius/mass API, genuine rolling impulse and callbacks.
            float[] minimum={1.8f,1.2f,.85f},maximum={3,2.4f,1.8f};
            for(int role=0;role<3;role++)foreach(float radius in new[]{minimum[role],maximum[role]})
            {
                var body=(Rigidbody)Get(balls[role],"body");int before=((int[])Get(rack,"RealContacts"))[role];float lane=(role-1)*10;
                // Entire mouth and vertical crane route are physically open at max radius.
                foreach(var plank in planks){Assert.That(plank.bounds.Intersects(new Bounds(new Vector3(lane,3.1f,-10),new Vector3(6,6,.2f))),Is.False,"No hidden front blocker");Assert.That(plank.bounds.Intersects(new Bounds(new Vector3(lane,8,0),new Vector3(6,6,10))),Is.False,"Original hoist has real open-top clearance");}
                Call(balls[role],"Restore",radius,0f,-1,new Vector3(lane,radius+.06f,-4),Quaternion.identity,Vector3.forward*4,Vector3.right*(4/radius));
                Physics.SyncTransforms();float end=Time.realtimeSinceStartup+10;
                while(Time.realtimeSinceStartup<end&&(((int[])Get(rack,"RealContacts"))[role]<=before||body.linearVelocity.magnitude>.7f))yield return new WaitForFixedUpdate();
                Assert.That(((int[])Get(rack,"RealContacts"))[role],Is.GreaterThan(before),"Actual radius "+radius+" in lane "+role+" must strike physical timber");Assert.That(body.position.z,Is.InRange(-11f,5f));Assert.That(Mathf.Abs(body.position.x-lane),Is.LessThan(1));Assert.That(body.linearVelocity.magnitude,Is.LessThanOrEqualTo(.7f));Assert.That(body.isKinematic,Is.False);Assert.That(f.Rules.Placed,Is.Zero);Assert.That((int)Get(f.match,"LiftRole"),Is.EqualTo(-1));Assert.That((bool)Call(f.match,"RequestLift"),Is.False,"Controlled geometry has no actual rolling-growth history and cannot earn pickup/points");
                TestContext.WriteLine("SNOWMAN_RACK_REAL_STOP role="+role+" radius="+radius+" position="+body.position.ToString("F6")+" velocity="+body.linearVelocity.ToString("F6")+" contacts="+((int[])Get(rack,"RealContacts"))[role]);body.isKinematic=true;
            }
            Assert.That((int)Get(Get(f.match,"ScoreJournal"),"Count"),Is.Zero);f.match.enabled=true;Call(f.match,"SetupScreen");Call(f.match,"AllBots");Assert.That((bool)Call(f.match,"StartMatch"),Is.True);Assert.That(((int[])Get(rack,"RealContacts")).Sum(),Is.Zero);Assert.That(((float[])Get(rack,"FirstContactClock")).All(x=>x<0),Is.True);Assert.That(((int[])Get(f.match,"AiDeliveryPhase")).Sum(),Is.Zero);Assert.That(f.Rules.Placed,Is.Zero);
        }
    }
    [UnityTest,Timeout(90000)]public IEnumerator ControlledActualGroundRollingConsumesSnowWhileAirborneMotionCannotGrow()
    {
        using(var f=new Fixture()){yield return f.Load();f.match.enabled=false;foreach(var t in ((Array)Get(f.match,"trucks")).Cast<Component>()){Call(t,"Power",false);((Rigidbody)Get(t,"body")).isKinematic=true;}var ball=f.Balls[0];var body=(Rigidbody)Get(ball,"body");var patch=((Array)Get(f.match,"patches")).GetValue(0);yield return new WaitForSeconds(.2f);Call(ball,"Track");float initial=(float)Get(ball,"radius"),snow=((float[])Get(patch,"Remaining")).Sum();body.AddForce(Vector3.forward*body.mass,ForceMode.Impulse);float until=Time.realtimeSinceStartup+2;while(Time.realtimeSinceStartup<until){yield return new WaitForFixedUpdate();Call(ball,"Step");}Assert.That((float)Get(ball,"radius"),Is.GreaterThan(initial+.005f));Assert.That(((float[])Get(patch,"Remaining")).Sum(),Is.LessThan(snow));Assert.That((float)Get(ball,"Rolled"),Is.GreaterThan(.05f));body.AddForce(Vector3.up*body.mass*8,ForceMode.Impulse);until=Time.realtimeSinceStartup+2;while((bool)Get(Get(ball,"contact"),"Supported")&&Time.realtimeSinceStartup<until){yield return new WaitForFixedUpdate();Call(ball,"Step");}Assert.That((bool)Get(Get(ball,"contact"),"Supported"),Is.False);float airborne=(float)Get(ball,"radius");until=Time.realtimeSinceStartup+.5f;while(Time.realtimeSinceStartup<until){yield return new WaitForFixedUpdate();Call(ball,"Step");}Assert.That((float)Get(ball,"radius"),Is.EqualTo(airborne),"Real airborne travel/rotation cannot collect ground snow.");Assert.That(f.Rules.Placed,Is.Zero);Assert.That((bool)Call(f.match,"RequestLift"),Is.False,"An ungrown/undelivered ball cannot request pickup.");Assert.That((bool)Call(f.match,"FinishBuild"),Is.False);Assert.That((int)Get(Get(f.match,"ScoreJournal"),"Count"),Is.Zero);TestContext.WriteLine("Controlled real impulse fixture; original growth adapter, actual contacts, no scoring/placement injection.");}
    }
    [UnityTest,Timeout(120000)]public IEnumerator RealProgressSnapshotRejectsUnknownIdsWithoutWritesAndRestoresPausedContext()
    {
        using(var f=new Fixture()){yield return f.Load();yield return Until(f,()=>(float)Get(f.Balls[0],"radius")>.8f,40,"Original AI must create actual growth first");Call(f.match,"Pause",0,false);yield return null;Assert.That((bool)Get(f.match,"WorkshopSaveReady"),Is.True);string json=(string)Call(f.match,"CaptureWorkshopJson");var before=f.Balls.Select(b=>((Rigidbody)Get(b,"body")).position).ToArray();var args=new object[]{json.Replace("snowman.ball.base","snowman.ball.UNKNOWN"),null};Assert.That((bool)f.match.GetType().GetMethod("TryRestoreWorkshopJson").Invoke(f.match,args),Is.False);Assert.That((string)args[1],Is.Not.Empty);CollectionAssert.AreEqual(before,f.Balls.Select(b=>((Rigidbody)Get(b,"body")).position).ToArray());string original=(string)Get(f.match,"MatchId");Call(f.match,"SetupScreen");Call(f.match,"AllBots");Assert.That((bool)Call(f.match,"StartMatch"),Is.True);Call(f.match,"Pause",0,false);args=new object[]{json,null};Assert.That((bool)f.match.GetType().GetMethod("TryRestoreWorkshopJson").Invoke(f.match,args),Is.True,(string)args[1]);Assert.That((string)Get(f.match,"RestoredSourceMatchId"),Is.EqualTo(original));for(int i=0;i<3;i++)Assert.That(Vector3.Distance(before[i],((Rigidbody)Get(f.Balls[i],"body")).position),Is.LessThan(1e-5f));Assert.That((float)Get(f.Balls[0],"radius"),Is.GreaterThan(.8f));Assert.That((int)Get(Get(f.match,"ScoreJournal"),"Count"),Is.Zero,"Restore itself cannot create another result receipt.");var frozen=f.Balls.Select(b=>((Rigidbody)Get(b,"body")).position).ToArray();yield return new WaitForSecondsRealtime(.2f);CollectionAssert.AreEqual(frozen,f.Balls.Select(b=>((Rigidbody)Get(b,"body")).position).ToArray());Call(f.match,"SetupScreen");args=new object[]{json,null};Assert.That((bool)f.match.GetType().GetMethod("TryRestoreWorkshopJson").Invoke(f.match,args),Is.True,"A stopped, validated workshop can be queued for original StartMatch.");Assert.That((bool)Call(f.match,"StartMatch"),Is.True);Call(f.match,"Pause",0,false);Assert.That((string)Get(f.match,"RestoredSourceMatchId"),Is.EqualTo(original),"Original StartMatch must consume the stopped snapshot once instead of discarding it.");for(int i=0;i<3;i++)Assert.That(Vector3.Distance(before[i],((Rigidbody)Get(f.Balls[i],"body")).position),Is.LessThan(1e-5f));Assert.That((int)Get(Get(f.match,"ScoreJournal"),"Count"),Is.Zero);}
    }
    static void Keys(Keyboard keyboard,params Key[] keys){InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));InputSystem.Update();}
    static void Owners(object match,int count){var setup=Get(match,"setup");setup.GetType().GetField("count").SetValue(setup,count);var players=(Array)Get(setup,"players");Key[] forward={Key.W,Key.UpArrow,Key.T,Key.I},left={Key.A,Key.LeftArrow,Key.F,Key.J},back={Key.S,Key.DownArrow,Key.G,Key.K},right={Key.D,Key.RightArrow,Key.H,Key.L};for(int i=0;i<count;i++){var p=players.GetValue(i);var source=p.GetType().GetField("source");source.SetValue(p,Enum.Parse(source.FieldType,"Keyboard"));p.GetType().GetField("forward").SetValue(p,forward[i]);p.GetType().GetField("left").SetValue(p,left[i]);p.GetType().GetField("back").SetValue(p,back[i]);p.GetType().GetField("right").SetValue(p,right[i]);}match.GetType().GetField("actionKeys").SetValue(match,new[]{Key.Space,Key.RightShift,Key.R,Key.U,Key.B,Key.Numpad0,Key.Digit0,Key.PageUp});}
    [UnityTest,Timeout(90000)]public IEnumerator OneThroughFourAssignedOwnersKeepOriginalNwhPhysicsAndPrivateViews()
    {
        using(var f=new Fixture()){yield return f.Load();var keyboard=InputSystem.AddDevice<Keyboard>();InputSystem.EnableDevice(keyboard);keyboard.MakeCurrent();try{var trucks=((Array)Get(f.match,"trucks")).Cast<Component>().ToArray();var bodies=trucks.Select(t=>(Rigidbody)Get(t,"body")).ToArray();for(int n=1;n<=4;n++){Keys(keyboard);Call(f.match,"SetupScreen");Owners(f.match,n);Assert.That((bool)Call(f.match,"StartMatch"),Is.True,"Configured disjoint keyboard owners must validate.");Call(Get(f.match,"drivingViews"),"Select",1);yield return null;yield return null;var rally=Get(Get(f.match,"drivingViews"),"rally");Assert.That((int)Get(rally,"Humans"),Is.EqualTo(n));CollectionAssert.AreEqual(Enumerable.Range(0,n).ToArray(),((int[])Get(rally,"owners")).Take(n).ToArray());CollectionAssert.AreEqual(bodies,trucks.Select(t=>(Rigidbody)Get(t,"body")).ToArray());if(n==4){Vector3 start=bodies[0].position;float target=(float)Get(f.match,"Clock")+2;Keys(keyboard,Key.W);yield return Until(f,()=>(float)Get(f.match,"Clock")>=target,6,"Two genuine NWH simulation seconds");Assert.That(((Vector2[])Get(f.match,"commands"))[0].y,Is.GreaterThan(.9f));Assert.That(Vector3.Distance(start,bodies[0].position),Is.GreaterThan(.1f));Assert.That((int)Get(Get(f.match,"ScoreJournal"),"Count"),Is.Zero);Capture(f,"four-assigned-owners-original-nwh");}}}finally{InputSystem.RemoveDevice(keyboard);}}
    }
}
#endif
