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
using LWS.TruckJenga;
public sealed class JengaPlayModeTests
{
    const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static Type Runtime(string name)=>Type.GetType(name+", Assembly-CSharp",true);
    static object Get(object o,string n)=>o.GetType().GetField(n,Any) is FieldInfo f?f.GetValue(o):o.GetType().GetProperty(n,Any).GetValue(o);
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethods(Any).Single(m=>m.Name==n&&m.GetParameters().Length==a.Length).Invoke(o,a);
    sealed class Fixture:IDisposable
    {
        readonly IDisposable prefs;readonly InputSettings originalSettings,temporarySettings;readonly GameObject isolate;public Behaviour match;public JengaRules Rules=>(JengaRules)Get(match,"Rules");public Rigidbody[] Bodies=>(Rigidbody[])Get(match,"containers");
        public Fixture(){prefs=(IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"Jenga07Native"});originalSettings=InputSystem.settings;temporarySettings=UnityEngine.Object.Instantiate(originalSettings);temporarySettings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;temporarySettings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=temporarySettings;isolate=new GameObject("Jenga original input isolation");UnityEngine.Object.DontDestroyOnLoad(isolate);isolate.AddComponent(Runtime("LWS.TruckSports.SportsProbeInputIsolation"));}
        public IEnumerator Load(){Assert.That(File.Exists("Assets/TruckJenga/Scenes/TruckJenga.unity"),Is.True,"Run the explicit NEW scene author first.");EditorSceneManager.LoadSceneInPlayMode("Assets/TruckJenga/Scenes/TruckJenga.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;match=UnityEngine.Object.FindObjectsByType(Runtime("LWS.TruckJenga.JengaMatch")).Cast<Behaviour>().Single();AssertAuthoredResetPoses(this);Call(match,"AllBots");Assert.That((bool)Call(match,"StartMatch"),Is.True);yield return null;}
        public void Dispose(){Time.timeScale=1;if(match!=null)Call(match,"SetupScreen");UnityEngine.Object.DestroyImmediate(isolate);InputSystem.settings=originalSettings;UnityEngine.Object.DestroyImmediate(temporarySettings);prefs.Dispose();}
    }
    static void AssertAuthoredResetPoses(Fixture f)
    {
        var positions=(Vector3[])Get(f.match,"initialPositions");var rotations=(Quaternion[])Get(f.match,"initialRotations");
        Assert.That(positions.Length,Is.EqualTo(18),"All original reset positions must exist.");Assert.That(rotations.Length,Is.EqualTo(18));
        var shapes=(BoxCollider[])Get(f.match,"shapes");
        for(int i=0;i<18;i++)
        {
            Vector3 expected=(Vector3)Call(f.match,"Slot",i/3,i%3);Quaternion rotation=(Quaternion)Call(f.match,"Rotation",i/3);
            Assert.That(Vector3.Distance(positions[i],expected),Is.LessThanOrEqualTo(.00001f),"Serialized reset position piece "+i+" must match its actual alternating tower slot, not a stale Rigidbody cache.");
            Assert.That(Quaternion.Angle(rotations[i],rotation),Is.LessThanOrEqualTo(.001f),"Serialized reset rotation piece "+i);
            for(int j=0;j<i;j++)if(Physics.ComputePenetration(shapes[i],positions[i],rotations[i],shapes[j],positions[j],rotations[j],out _,out float depth))
                Assert.That(depth,Is.LessThanOrEqualTo(.0001f),"Original authored tower must have no positive-volume overlap: "+i+"/"+j);
        }
    }
    [UnityTest,Timeout(60000)]public IEnumerator AuthoredResetMetadataPhysicallySettlesBeforeSelectionAndRematchUsesSameTower()
    {
        using(var f=new Fixture())
        {
            yield return f.Load();var bodies=f.Bodies.ToArray();float deadline=Time.realtimeSinceStartup+15;
            while(f.Rules.Phase==JengaRules.Stage.Preparing&&Time.realtimeSinceStartup<deadline){Assert.That((string)Get(f.match,"Fault"),Is.Empty,"Actual foundation must settle without collapse or support changes.");yield return null;}
            Assert.That(f.Rules.Phase,Is.EqualTo(JengaRules.Stage.Selecting),"Unmodified gravity/contact solver must settle the original dynamic tower.");
            var positions=(Vector3[])Get(f.match,"initialPositions");var rotations=(Quaternion[])Get(f.match,"initialRotations");
            for(int i=0;i<18;i++)
            {
                TestContext.WriteLine("JENGA_V3_SETTLED piece="+i+" p="+bodies[i].position.ToString("F6")+" v="+bodies[i].linearVelocity.ToString("F6")+" w="+bodies[i].angularVelocity.ToString("F6"));
                Assert.That(bodies[i].isKinematic,Is.False);Assert.That(Vector3.Distance(bodies[i].position,positions[i]),Is.LessThan(.05f),"No tower reconstruction or hidden settling teleport: "+i);
                Assert.That(Quaternion.Angle(bodies[i].rotation,rotations[i]),Is.LessThan(1));Assert.That(bodies[i].linearVelocity.magnitude,Is.LessThanOrEqualTo(.25f));Assert.That(bodies[i].angularVelocity.magnitude,Is.LessThanOrEqualTo(.18f));
            }
            Call(f.match,"SetupScreen");Call(f.match,"AllBots");Assert.That((bool)Call(f.match,"StartMatch"),Is.True);CollectionAssert.AreEqual(bodies,f.Bodies);
            for(int i=0;i<18;i++){Assert.That(Vector3.Distance(bodies[i].position,positions[i]),Is.LessThanOrEqualTo(.00001f),"Rematch must restore the exact original slot "+i);Assert.That(Quaternion.Angle(bodies[i].rotation,rotations[i]),Is.LessThanOrEqualTo(.001f));Assert.That(bodies[i].linearVelocity,Is.EqualTo(Vector3.zero));}
            Assert.That(f.Rules.Stacks.Sum(),Is.Zero);Assert.That((int)Get(Get(f.match,"ScoreJournal"),"Count"),Is.Zero);
        }
    }
    static IEnumerator Simulated(Fixture f,float seconds,string label){float target=(float)Get(f.match,"Clock")+seconds,deadline=Time.realtimeSinceStartup+Mathf.Max(5,seconds*3);while((float)Get(f.match,"Clock")<target&&Time.realtimeSinceStartup<deadline)yield return null;Assert.That((float)Get(f.match,"Clock"),Is.GreaterThanOrEqualTo(target),label+" must complete genuine simulation time; paused="+Get(f.match,"Paused")+" scale="+Time.timeScale+" keyboard="+(Keyboard.current!=null?Keyboard.current.enabled.ToString():"none"));}
    static void Capture(Fixture f,string name){string folder=Path.GetFullPath("Logs/TruckSports/PlaytestRepair/Impossible07Jenga");Directory.CreateDirectory(folder);var canvas=((Component)Get(f.match,"menus")).GetComponent<Canvas>();var cameras=(Camera[])Get(f.match,"cameras");var save=Runtime("LWS.TruckSports.SportsControlledCapture").GetMethods().Single(m=>m.Name=="Save"&&m.GetParameters().Length==6);foreach(int w in new[]{1280,1920})save.Invoke(null,new object[]{canvas,cameras,w,w==1280?720:1080,Path.Combine(folder,name+"-"+w+"-controlled.png"),null});}
    static void Source(object match,int seat,string value){var players=(Array)Get(Get(match,"setup"),"players");var player=players.GetValue(seat);var f=player.GetType().GetField("source");f.SetValue(player,Enum.Parse(f.FieldType,value));}
    [UnityTest,Timeout(600000)]public IEnumerator NaturalAiPhysicallyExtractsCraneRestacksAndEventuallyCausesActualCollapse()
    {
        using(var f=new Fixture()){yield return f.Load();var original=f.Bodies.ToArray();Assert.That(original.Length,Is.EqualTo(18));Assert.That(original.All(b=>!b.isKinematic&&b.mass>0),Is.True);float deadline=Time.realtimeSinceStartup+450;float peakForce=0;bool restackCaptured=false,pulling=false,craneMoved=false,dynamicLoadLifted=false;Vector3 oldSpreader=((Rigidbody)Get(Get(f.match,"crane"),"spreader")).position;while(!(bool)Get(f.match,"Finished")&&Time.realtimeSinceStartup<deadline){Assert.That((string)Get(f.match,"Fault"),Is.Empty);var winch=Get(f.match,"winch");peakForce=Mathf.Max(peakForce,(float)Get(winch,"Tension"));pulling|=f.Rules.Phase==JengaRules.Stage.Pulling;craneMoved|=Vector3.Distance(((Rigidbody)Get(Get(f.match,"crane"),"spreader")).position,oldSpreader)>3;var load=Get(Get(f.match,"crane"),"Load") as Rigidbody;if(load!=null&&f.Rules.Selected>=0)dynamicLoadLifted|=!load.isKinematic&&load.position.y>((Vector3[])Get(f.match,"initialPositions"))[f.Rules.Selected].y+1;if(!restackCaptured&&(int)Get(f.match,"PhysicalRestacks")>0){yield return null;yield return null;yield return new WaitForSecondsRealtime(.18f);Capture(f,"natural-first-restack");restackCaptured=true;}yield return null;}
            TestContext.WriteLine("JENGA_NATURAL turns="+f.Rules.Turn+" stacks="+string.Join(",",f.Rules.Stacks)+" extracted="+Get(f.match,"PhysicalExtractions")+" restacked="+Get(f.match,"PhysicalRestacks")+" peakN="+peakForce+" loser="+f.Rules.Loser+" fallen="+Get(f.match,"CollapsedPiece"));Assert.That((bool)Get(f.match,"Finished"),Is.True);Assert.That(pulling,Is.True,"Original AI must actually enter physical pulling.");Assert.That(craneMoved,Is.True,"Original spreader must actually travel >3m; early physical collapse cannot substitute for crane progress. extracted="+Get(f.match,"PhysicalExtractions")+" pickups="+Get(Get(f.match,"crane"),"Pickups")+" fallen="+Get(f.match,"CollapsedPiece"));Assert.That(dynamicLoadLifted,Is.True,"The actual still-dynamic container must rise >1m under the physical spreader joint.");Assert.That(peakForce,Is.GreaterThan(10));Assert.That((int)Get(f.match,"PhysicalRestacks"),Is.GreaterThan(0));Assert.That((int)Get(Get(f.match,"crane"),"Pickups"),Is.GreaterThan(0));Assert.That((int)Get(Get(f.match,"crane"),"Releases"),Is.GreaterThanOrEqualTo((int)Get(f.match,"PhysicalRestacks")));Assert.That((int)Get(Get(f.match,"crane"),"Pickups"),Is.GreaterThanOrEqualTo((int)Get(Get(f.match,"crane"),"Releases")));Assert.That(f.Rules.Loser,Is.InRange(0,f.Rules.Stacks.Length-1));Assert.That((int)Get(Get(f.match,"ScoreJournal"),"Count"),Is.EqualTo(f.Rules.Stacks.Sum()+1));CollectionAssert.AreEqual(original,f.Bodies);yield return null;yield return null;yield return new WaitForSecondsRealtime(.18f);Capture(f,"natural-real-collapse-result");}
    }
    [UnityTest,Timeout(150000)]public IEnumerator ActualAiExtractionParksWithNwhHandbrakeWithoutReversingLoadBackIntoTower()
    {
        using(var f=new Fixture())
        {
            yield return f.Load();float deadline=Time.realtimeSinceStartup+35;while((int)Get(f.match,"PhysicalExtractions")==0&&!(bool)Get(f.match,"Finished")&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That((int)Get(f.match,"PhysicalExtractions"),Is.EqualTo(1),"Original AI must physically extract the first legal piece without pose/outcome injection.");
            var shapes=(BoxCollider[])Get(f.match,"shapes");int selected=f.Rules.Selected;Bounds footprint=shapes[selected].bounds;
            for(int i=0;i<shapes.Length;i++)if(i!=selected){Bounds other=shapes[i].bounds;other.Expand(.08f);bool overlap=footprint.min.x<other.max.x&&footprint.max.x>other.min.x&&footprint.min.z<other.max.z&&footprint.max.z>other.min.z;TestContext.WriteLine("JENGA_HANDOFF_COLUMN selected="+selected+" other="+i+" candidate="+footprint+" actualOther="+other+" overlap="+overlap);Assert.That(overlap,Is.False,"Actual AI must clear every real tower column before lifting, including displaced overhangs with different Y.");}
            int owner=f.Rules.Owner;var truck=((Array)Get(f.match,"trucks")).GetValue(owner);var body=(Rigidbody)Get(truck,"body");Vector3 direction=(Vector3)Get(f.match,"pullDirection"),start=body.position;float toward=0;deadline=Time.realtimeSinceStartup+45;
            while((int)Get(Get(f.match,"crane"),"Pickups")==0&&f.Rules.Phase==JengaRules.Stage.Crane&&Time.realtimeSinceStartup<deadline)
            {
                toward=Mathf.Min(toward,Vector3.Dot(body.linearVelocity,direction));Assert.That((Vector2)Get(truck,"command"),Is.EqualTo(Vector2.zero),"Crane hold sends zero throttle/service-brake input, preserving Auto-DNR intent.");
                var states=Get(Get(Get(truck,"tractorMotor"),"vehicle"),"input");var input=Get(states,"states");Assert.That((float)Get(input,"handbrake"),Is.EqualTo(1));Assert.That((float)Get(input,"brakes"),Is.Zero);Assert.That((float)Get(input,"throttle"),Is.Zero);
                Assert.That(body.isKinematic,Is.False,"Real parking wheel forces hold the original dynamic chassis.");yield return null;
            }
            TestContext.WriteLine("JENGA_V5_HOLD original extraction start="+start+" final="+body.position+" towardTowerWorst="+toward+" pickups="+Get(Get(f.match,"crane"),"Pickups"));
            Assert.That(toward,Is.GreaterThan(-.2f),"Parking must never accelerate the original operator backwards into the real load/tower.");Assert.That(Vector3.Dot(body.position-start,direction),Is.GreaterThan(-.1f));Assert.That((int)Get(Get(f.match,"crane"),"Pickups"),Is.GreaterThan(0),"A real dynamic load joint must now be acquired; no crane timing/velocity threshold relaxation.");Assert.That(f.Rules.Loser,Is.EqualTo(-1));
        }
    }
    [UnityTest,Timeout(150000)]public IEnumerator PausePreservesPhysicalCableLoadAndCraneStateThenRematchRestoresOriginalBodies()
    {
        using(var f=new Fixture()){yield return f.Load();var original=f.Bodies.ToArray();float deadline=Time.realtimeSinceStartup+20;while(f.Rules.Phase!=JengaRules.Stage.Pulling&&Time.realtimeSinceStartup<deadline)yield return null;Assert.That(f.Rules.Phase,Is.EqualTo(JengaRules.Stage.Pulling));yield return new WaitForSeconds(.3f);Call(f.match,"Pause",0,false);yield return null;var positions=f.Bodies.Select(b=>b.position).ToArray();var velocities=f.Bodies.Select(b=>b.linearVelocity).ToArray();float clock=(float)Get(f.match,"Clock"),length=(float)Get(Get(f.match,"winch"),"Length");var joint=Get(Get(f.match,"winch"),"Joint");yield return new WaitForSecondsRealtime(.2f);CollectionAssert.AreEqual(positions,f.Bodies.Select(b=>b.position).ToArray());CollectionAssert.AreEqual(velocities,f.Bodies.Select(b=>b.linearVelocity).ToArray());Assert.That((float)Get(f.match,"Clock"),Is.EqualTo(clock));Assert.That((float)Get(Get(f.match,"winch"),"Length"),Is.EqualTo(length));Assert.That(Get(Get(f.match,"winch"),"Joint"),Is.SameAs(joint));Call(f.match,"Resume");yield return new WaitForSeconds(.2f);Assert.That((float)Get(f.match,"Clock"),Is.GreaterThan(clock));var crane=Get(f.match,"crane");deadline=Time.realtimeSinceStartup+90;while(Get(crane,"Stage").ToString()!="Lift"&&Get(crane,"Stage").ToString()!="Traverse"&&!(bool)Get(f.match,"Finished")&&Time.realtimeSinceStartup<deadline)yield return null;Assert.That(Get(crane,"Stage").ToString(),Is.EqualTo("Lift").Or.EqualTo("Traverse"),"Actual loaded-crane pause coverage requires live pickup before collapse. phase="+f.Rules.Phase+" finished="+Get(f.match,"Finished")+" extracted="+Get(f.match,"PhysicalExtractions")+" pickups="+Get(crane,"Pickups")+" fallen="+Get(f.match,"CollapsedPiece"));var load=(Rigidbody)Get(crane,"Load");Assert.That(load,Is.Not.Null);Assert.That(load.isKinematic,Is.False);Assert.That(load.GetComponent<FixedJoint>(),Is.Not.Null);Call(f.match,"Pause",0,false);yield return null;Vector3 loadPosition=load.position,loadVelocity=load.linearVelocity,spreaderPosition=((Rigidbody)Get(crane,"spreader")).position;string stage=Get(crane,"Stage").ToString();yield return new WaitForSecondsRealtime(.2f);Assert.That(load.position,Is.EqualTo(loadPosition));Assert.That(load.linearVelocity,Is.EqualTo(loadVelocity));Assert.That(((Rigidbody)Get(crane,"spreader")).position,Is.EqualTo(spreaderPosition));Assert.That(Get(crane,"Stage").ToString(),Is.EqualTo(stage));Call(f.match,"Resume");yield return new WaitForFixedUpdate();Call(f.match,"SetupScreen");Call(f.match,"AllBots");Assert.That((bool)Call(f.match,"StartMatch"),Is.True);CollectionAssert.AreEqual(original,f.Bodies);Assert.That(f.Rules.Stacks.Sum(),Is.Zero);Assert.That((int)Get(f.match,"PhysicalRestacks"),Is.Zero);Assert.That(Get(Get(f.match,"winch"),"Joint"),Is.Null);Assert.That(Get(Get(f.match,"crane"),"Load"),Is.Null);Assert.That((int)Get(Get(f.match,"ScoreJournal"),"Count"),Is.Zero);}
    }
    [UnityTest,Timeout(60000)]public IEnumerator ControlledRealImpulseCollapseIsAttributedOnceWithoutInjectingOutcome()
    {
        using(var f=new Fixture()){yield return f.Load();float deadline=Time.realtimeSinceStartup+15;while(f.Rules.Phase!=JengaRules.Stage.Selecting&&Time.realtimeSinceStartup<deadline)yield return null;Assert.That(f.Rules.Phase,Is.EqualTo(JengaRules.Stage.Selecting));int owner=f.Rules.Owner;Assert.That((bool)Call(f.match,"SelectPiece",4,1),Is.True);Assert.That((bool)Call(f.match,"BeginExtraction"),Is.True);Call(Get(f.match,"winch"),"Disconnect");var keyboard=InputSystem.AddDevice<Keyboard>();InputSystem.EnableDevice(keyboard);keyboard.MakeCurrent();try{Source(f.match,owner,"Keyboard");InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();f.Bodies[16].AddForce(Vector3.right*1600,ForceMode.Impulse);deadline=Time.realtimeSinceStartup+8;while(f.Rules.Loser<0&&Time.realtimeSinceStartup<deadline)yield return null;Assert.That(f.Rules.Loser,Is.EqualTo(owner));Assert.That((int)Get(f.match,"CollapsedPiece"),Is.GreaterThanOrEqualTo(0));Assert.That(f.Rules.Stacks.Sum(),Is.Zero);Assert.That((int)Get(Get(f.match,"ScoreJournal"),"Count"),Is.EqualTo(1));yield return new WaitForSeconds(.3f);Assert.That((int)Get(Get(f.match,"ScoreJournal"),"Count"),Is.EqualTo(1));TestContext.WriteLine("Controlled impulse fixture only: real dynamic body push, original physical collapse observer and attribution; no Rule.Collapse/score/result injection.");}finally{InputSystem.RemoveDevice(keyboard);}}
    }
    [UnityTest,Timeout(65000)]public IEnumerator OnlyActualOperatingOwnerReceivesCableTelemetryAndActions()
    {
        using(var f=new Fixture()){yield return f.Load();var keyboard=InputSystem.AddDevice<Keyboard>();InputSystem.EnableDevice(keyboard);keyboard.MakeCurrent();try
        {
            Call(f.match,"SetupScreen");KeyboardOwners(f.match,4,4);Assert.That((bool)Call(f.match,"StartMatch"),Is.True);Call(Get(f.match,"drivingViews"),"Select",1);float deadline=Time.realtimeSinceStartup+15;while(f.Rules.Phase!=JengaRules.Stage.Selecting&&Time.realtimeSinceStartup<deadline)yield return null;Assert.That(f.Rules.Phase,Is.EqualTo(JengaRules.Stage.Selecting));Assert.That((bool)Call(f.match,"BeginExtraction"),Is.True,"Original legal selection/attach interaction owns this real cable.");yield return null;yield return null;yield return new WaitForSecondsRealtime(.18f);
            for(int v=0;v<4;v++){string context=(string)Call(f.match,"ViewportContextPrompt",v),telemetry=(string)Call(f.match,"ViewportTelemetry",v);if(v==f.Rules.Owner){Assert.That(context,Does.Contain("WINCH"));Assert.That(telemetry,Does.Contain("TENSION"));}else{Assert.That(string.IsNullOrEmpty(context),Is.True,"Waiting participant must not be instructed to operate another owner's cable.");Assert.That(string.IsNullOrEmpty(telemetry),Is.True,"Waiting participant has no private winch tension.");}}
            Capture(f,"settled-four-owner-only-active-cable-context");Assert.That((int)Get(Get(f.match,"ScoreJournal"),"Count"),Is.Zero,"Presentation change cannot award a restack.");
        }finally{InputSystem.RemoveDevice(keyboard);}}
    }
    static void KeyboardOwners(object match,int humans,int total){var setup=Get(match,"setup");setup.GetType().GetField("count").SetValue(setup,total);var players=(Array)Get(setup,"players");Key[] forward={Key.W,Key.UpArrow,Key.I,Key.T},left={Key.A,Key.LeftArrow,Key.J,Key.F},back={Key.S,Key.DownArrow,Key.K,Key.G},right={Key.D,Key.RightArrow,Key.L,Key.H};for(int i=0;i<total;i++){Source(match,i,i<humans?"Keyboard":"AI");if(i<humans){var player=players.GetValue(i);player.GetType().GetField("forward").SetValue(player,forward[i]);player.GetType().GetField("left").SetValue(player,left[i]);player.GetType().GetField("back").SetValue(player,back[i]);player.GetType().GetField("right").SetValue(player,right[i]);}}match.GetType().GetField("actionKeys").SetValue(match,new[]{Key.Space,Key.RightShift,Key.R,Key.U,Key.B,Key.Numpad0,Key.Digit0,Key.PageUp});}
    static void Keys(Keyboard keyboard,params Key[] keys){InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));InputSystem.Update();}
    static IEnumerator NavigateOnce(Fixture f,Keyboard keyboard,Key key,int direction)
    {
        Keys(keyboard);yield return Simulated(f,.28f,"Neutral selection repeat cooldown");int before=(int)Get(f.match,"Selected");int[] legal=f.Rules.LegalPieces();int expected=legal[(Array.IndexOf(legal,before)+direction+legal.Length)%legal.Length];float deadline=Time.realtimeSinceStartup+3;
        Keys(keyboard,key);while((int)Get(f.match,"Selected")==before&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();Keys(keyboard);
        while(Mathf.Abs(((Vector2[])Get(f.match,"commands"))[0].x)>.01f&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(((Vector2[])Get(f.match,"commands"))[0].x,Is.EqualTo(0).Within(.01f),"Original assigned input receipt must release to neutral.");Assert.That((int)Get(f.match,"Selected"),Is.EqualTo(expected),"One actual navigation pulse must move exactly one legal piece. direction="+direction+" before="+before+" clock="+Get(f.match,"Clock"));
    }
    [UnityTest,Timeout(120000)]public IEnumerator OneThroughFourAssignedOwnersUseRealSelectionWinchAndNwhDrivingWithOriginalViews()
    {
        using(var f=new Fixture()){yield return f.Load();var keyboard=InputSystem.AddDevice<Keyboard>();InputSystem.EnableDevice(keyboard);keyboard.MakeCurrent();try{var bodies=f.Bodies.ToArray();for(int humans=1;humans<=4;humans++){Keys(keyboard);Call(f.match,"SetupScreen");KeyboardOwners(f.match,humans,Mathf.Max(2,humans));Assert.That((bool)Call(f.match,"StartMatch"),Is.True);Call(Get(f.match,"drivingViews"),"Select",1);yield return null;yield return null;var rally=Get(Get(f.match,"drivingViews"),"rally");Assert.That((int)Get(rally,"Humans"),Is.EqualTo(humans));CollectionAssert.AreEqual(Enumerable.Range(0,humans).ToArray(),((int[])Get(rally,"owners")).Take(humans).ToArray());CollectionAssert.AreEqual(bodies,f.Bodies);if(humans==4){float deadline=Time.realtimeSinceStartup+15;while(f.Rules.Phase!=JengaRules.Stage.Selecting&&Time.realtimeSinceStartup<deadline)yield return null;Assert.That(f.Rules.Phase,Is.EqualTo(JengaRules.Stage.Selecting));int selected=(int)Get(f.match,"Selected");yield return NavigateOnce(f,keyboard,Key.D,1);yield return NavigateOnce(f,keyboard,Key.A,-1);Assert.That((int)Get(f.match,"Selected"),Is.EqualTo(selected),"A single opposite input pulse returns to the original legal piece; no held-key repeat duration assumption.");Keys(keyboard,Key.Space);yield return null;yield return new WaitForFixedUpdate();Assert.That(f.Rules.Phase,Is.EqualTo(JengaRules.Stage.Pulling));Assert.That((bool)Get(Get(f.match,"winch"),"Attached"),Is.True);Keys(keyboard);yield return null;var trucks=(Array)Get(f.match,"trucks");var truck=trucks.GetValue(0);var body=(Rigidbody)Get(truck,"body");Vector3 position=body.position;Keys(keyboard,Key.W);yield return Simulated(f,2,"Assigned NWH physical driving");Assert.That(((Vector2[])Get(f.match,"commands"))[0].y,Is.GreaterThan(.9f));Assert.That(Vector3.Distance(body.position,position),Is.GreaterThan(.1f));Assert.That((int)Get(Get(f.match,"winch"),"Connections"),Is.GreaterThan(0));Capture(f,"four-assigned-owner-real-winch-driving");}}}finally{InputSystem.RemoveDevice(keyboard);}}
    }
}
#endif
