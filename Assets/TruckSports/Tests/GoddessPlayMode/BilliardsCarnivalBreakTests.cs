#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
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

public sealed class BilliardsCarnivalBreakTests
{
 const BindingFlags Any=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
 static Type Runtime(string name)=>Type.GetType(name+", Assembly-CSharp",true);
 static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
 static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Any).SetValue(owner,value);
 static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethods(Any).Single(method=>method.Name==name&&method.GetParameters().Length==args.Length).Invoke(owner,args);
 sealed class Fixture:IDisposable
 {
  readonly IDisposable prefs;readonly InputSettings original,temporary;readonly GameObject isolation;public readonly Gamepad pad;public Behaviour match;
  public Fixture(string suffix)
  {
   prefs=(IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"BilliardsCarnival"+suffix});original=InputSystem.settings;temporary=UnityEngine.Object.Instantiate(original);temporary.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;temporary.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=temporary;
   isolation=new GameObject("Billiards initial-break source isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("LWS.TruckSports.SportsProbeInputIsolation"));pad=InputSystem.AddDevice<Gamepad>();InputSystem.EnableDevice(pad);
  }
  public object session=>Get(match,"session");public object rules=>Get(match,"rules");public object Ball(int number)=>((Array)Get(match,"balls")).GetValue(number);public Rigidbody Body(int number)=>(Rigidbody)Get(Ball(number),"body");public object truck=>Get(match,"player");
  public Vector3[] Poses()=>Enumerable.Range(0,16).Select(number=>Body(number).position).ToArray();
  public Vector3[] Authored()=>Enumerable.Range(0,16).Select(number=>(Vector3)Get(Ball(number),"initial")).ToArray();
  public IEnumerator Load()
  {
   EditorSceneManager.LoadSceneInPlayMode("Assets/TruckBilliards/Scenes/TruckBilliards.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
   match=UnityEngine.Object.FindObjectsByType(Runtime("LWS.TruckBilliards.BilliardsMatch")).Cast<Behaviour>().Single();Set(session,"hotSeatGamepad",true);Call(session,"SelectMode",Enum.Parse(Runtime("LWS.TruckBilliards.BilliardsMode"),"Practice"));Assert.That((bool)Call(session,"Assign",0,pad),Is.True);
  }
  public IEnumerator Fresh(int seed,float amount=.006f)
  {
   InputSystem.QueueStateEvent(pad,new GamepadState());Set(match,"rackSeed",seed);Set(match,"rackImperfection",amount);Assert.That((bool)Call(session,"StartMatch"),Is.True);float deadline=Time.realtimeSinceStartup+5;while(!(bool)Get(match,"AreSettled")&&Time.realtimeSinceStartup<deadline)yield return null;Assert.That((bool)Get(match,"AreSettled"),Is.True);yield return null;
  }
  public void Dispose(){Time.timeScale=1;Behaviour router=null;bool enabled=false;if(match!=null){Call(session,"Show",Enum.Parse(Runtime("LWS.TruckBilliards.BilliardsScreen"),"Setup"));router=(Behaviour)Get(session,"input");enabled=router.enabled;router.enabled=false;}InputSystem.settings=original;if(pad.added)InputSystem.RemoveDevice(pad);if(router!=null)router.enabled=enabled;UnityEngine.Object.DestroyImmediate(temporary);UnityEngine.Object.DestroyImmediate(isolation);prefs.Dispose();Time.timeScale=1;}
 }
 static void Geometry(Fixture f,Vector3[] authored)
 {
  var actual=f.Poses();Assert.That(actual[0],Is.EqualTo(authored[0]));for(int i=1;i<16;i++){Assert.That(Vector3.Distance(actual[i],authored[i]),Is.LessThan(.00801f));Assert.That(actual[i].y,Is.EqualTo(authored[i].y));for(int j=i+1;j<16;j++){var first=(SphereCollider)Get(f.Ball(i),"sphere");var second=(SphereCollider)Get(f.Ball(j),"sphere");Assert.That(Physics.ComputePenetration(first,f.Body(i).position,f.Body(i).rotation,second,f.Body(j).position,f.Body(j).rotation,out _,out float depth)&&depth>.00001f,Is.False);}}Assert.That(Enumerable.Range(0,16).All(number=>f.Body(number).linearVelocity.sqrMagnitude<.000001f),Is.True);
 }
 static void Capture(Fixture f,string label)
 {
  string folder=Path.GetFullPath("Logs/TruckSports/PlaytestRepair/BilliardsCarnivalNative");Directory.CreateDirectory(folder);var camera=(Camera)Get(f.match,"tableCamera");var canvas=((Component)Get(f.session,"menus")).GetComponentInParent<Canvas>();Assert.That(canvas,Is.Not.Null);var save=Runtime("LWS.TruckSports.SportsControlledCapture").GetMethods().Single(method=>method.Name=="Save"&&method.GetParameters().Length==6);
  foreach(int width in new[]{1280,1920})save.Invoke(null,new object[]{canvas,new[]{camera},width,width==1280?720:1080,Path.Combine(folder,label+"-"+width+"-controlled.png"),null});
 }
 [UnityTest,Timeout(240000)]
 public IEnumerator RepeatedIdenticalHeadOnBumperBreaksProduceMeasuredPhysicalCornerScatter()
 {
  using(var f=new Fixture("Scatter"))
  {
   yield return f.Load();var authored=f.Authored();var corners=new List<Vector3[]>();float firstSpeed=-1;string folder=Path.GetFullPath("Logs/TruckSports/PlaytestRepair/BilliardsCarnivalNative");Directory.CreateDirectory(folder);var rows=new List<string>{"seed\tvariance\tstrikeSpeed\tleftX\tleftZ\trightX\trightZ\tmaxSpeed\tfirstContact"};
   for(int trial=0;trial<5;trial++)
   {
    int seed=new[]{101,202,101,202,303}[trial];float amount=trial<2?0:.006f;yield return f.Fresh(seed,amount);Geometry(f,authored);var initial=f.Poses();if(amount>0)Assert.That(initial.Zip(authored,(a,b)=>Vector3.Distance(a,b)>.00001f).Count(value=>value),Is.GreaterThan(10));
    InputSystem.QueueStateEvent(f.pad,new GamepadState{rightTrigger=1});float deadline=Time.realtimeSinceStartup+5;while((int)Get(f.match,"strikeCount")==0&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();Assert.That((int)Get(f.match,"strikeCount"),Is.EqualTo(1),"Original swept bumper must produce the opening strike.");Assert.That((bool)Get(f.match,"LastStrikeOpening"),Is.True);float speed=(float)Get(f.match,"LastStrikeSpeed");if(firstSpeed<0)firstSpeed=speed;else Assert.That(speed,Is.EqualTo(firstSpeed).Within(.001f),"Keep physical bumper launch speed identical across trials.");InputSystem.QueueStateEvent(f.pad,new GamepadState());
    float maximum=0;for(int step=0;step<300;step++){yield return new WaitForFixedUpdate();for(int number=0;number<16;number++)maximum=Mathf.Max(maximum,f.Body(number).linearVelocity.magnitude);}
    Assert.That((int)Get(f.rules,"FirstContact"),Is.GreaterThan(0));Assert.That((int)Get(f.match,"strikeCount"),Is.EqualTo(1));Assert.That(maximum,Is.LessThanOrEqualTo((float)Get(f.match,"maxBreakShotSpeed")+.5f));var corner=new[]{f.Body(13).position,f.Body(15).position};corners.Add(corner);for(int i=0;i<2;i++)Assert.That(Vector3.Distance(corner[i],initial[i==0?13:15]),Is.GreaterThan(1),"Corner movement must come from actual ball collisions, not its6mm initial offset.");
    rows.Add(seed+"\t"+amount+"\t"+speed+"\t"+corner[0].x+"\t"+corner[0].z+"\t"+corner[1].x+"\t"+corner[1].z+"\t"+maximum+"\t"+Get(f.rules,"FirstContact"));File.WriteAllLines(Path.Combine(folder,"head-on-corner-scatter.tsv"),rows);if(trial>=2)Capture(f,"seed-"+seed+"-six-second-scatter");CollectionAssert.AreEqual(authored,f.Authored());
   }
   float baselineNoise=Mathf.Max(Vector3.Distance(corners[0][0],corners[1][0]),Vector3.Distance(corners[0][1],corners[1][1])),variation=0;for(int i=2;i<5;i++)for(int j=i+1;j<5;j++)for(int corner=0;corner<2;corner++)variation=Mathf.Max(variation,Vector3.Distance(corners[i][corner],corners[j][corner]));Assert.That(variation,Is.GreaterThan(Mathf.Max(.02f,baselineNoise*3)),"Seeded physical scatter must exceed both6mm placement and repeat-trial timing noise.");
   TestContext.WriteLine("Five actual software-RT/head-on swept-bumper breaks: two zero-variance controls plus seeds101/202/303 at0.006m. Fixed physical launch speed="+firstSpeed+", control corner noise="+baselineNoise+", physical seeded corner difference="+variation+"m after300steps. Native Camera.Render images/TSV retained. No injected shot velocity/contact/pocket or postimpact force; not a full-match or hardware acceptance claim.");
  }
 }
 [UnityTest,Timeout(90000)]
 public IEnumerator SavedRackReferencesSeedsAndOrdinaryShotPhysicsRemainSeparate()
 {
  using(var f=new Fixture("Boundary"))
  {
   yield return f.Load();var authored=f.Authored();yield return f.Fresh(717);var first=f.Poses();Geometry(f,authored);yield return f.Fresh(717);CollectionAssert.AreEqual(first,f.Poses());yield return f.Fresh(818);Assert.That(first.Zip(f.Poses(),(a,b)=>Vector3.Distance(a,b)>.00001f).Any(value=>value),Is.True);
   yield return f.Fresh(0);int automatic=(int)Get(f.match,"LastRackSeed");yield return f.Fresh(0);Assert.That((int)Get(f.match,"LastRackSeed"),Is.Not.EqualTo(automatic));Geometry(f,authored);
   // Controlled zero-motion rule transition isolates a later ordinary shot.
   // The preceding test supplies the real opening-break physics evidence.
   Assert.That((bool)Call(f.rules,"BeginShot"),Is.True);Call(f.rules,"Contact",1);Call(f.rules,"Resolve");Assert.That((bool)Get(f.rules,"Opening"),Is.False);var before=f.Poses();int seed=(int)Get(f.match,"LastRackSeed");Call(f.match,"Strike",f.truck,Vector3.forward*30);
   Assert.That((bool)Get(f.match,"LastStrikeOpening"),Is.False);Assert.That((float)Get(f.match,"LastStrikeSpeed"),Is.EqualTo((float)Get(f.truck,"maxShotSpeed")));for(int i=1;i<16;i++)Assert.That(f.Body(i).position,Is.EqualTo(before[i]));yield return new WaitForSeconds(.25f);for(int i=1;i<16;i++)Assert.That(Vector3.Distance(f.Body(i).position,before[i]),Is.LessThan(.00001f),"Before cue arrival, ordinary-shot updates cannot rerandomize a rack.");Assert.That((int)Get(f.match,"LastRackSeed"),Is.EqualTo(seed));CollectionAssert.AreEqual(authored,f.Authored());
   TestContext.WriteLine("Actual saved-rack seeded placement, no sphere penetration/prebreak velocity, unchanged author references, fixed/automatic seed behavior and ordinary cappedStrike without reracking checked. Normal-shot phase is an explicit controlled zero-motion rules fixture, not a complete played break.");
  }
 }
}
#endif
