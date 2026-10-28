#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace LWS.TruckDentist.Tests
{
 // Independent diagnostics reuse the original controlled/native fixture verbatim.
 // No source scoring/physics, tooth, input or collision-filter authority changes.
 public sealed class DentistPenaltyDiagnostics
 {
  const BindingFlags Any=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
  static object Get(object x,string n)=>x.GetType().GetField(n,Any) is FieldInfo f?f.GetValue(x):x.GetType().GetProperty(n,Any).GetValue(x);
  static void Set(object x,string n,object v)=>x.GetType().GetField(n,Any).SetValue(x,v);
  static object Call(object x,string n,params object[] a)=>x.GetType().GetMethods(Any).Single(m=>m.Name==n&&m.GetParameters().Length==a.Length&&m.GetParameters().Select((p,i)=>a[i]==null||p.ParameterType.IsInstanceOfType(a[i])).All(v=>v)).Invoke(x,a);
  sealed class Original:IDisposable
  {
   public readonly object fixture=Activator.CreateInstance(typeof(DentistLifecycleTests).GetNestedType("Fixture",BindingFlags.NonPublic),true);
   public Behaviour Match=>(Behaviour)Get(fixture,"match");public object Rules=>Get(fixture,"rules");public object[] Arms=>(object[])Get(fixture,"arms");public object[] Tools=>(object[])Get(fixture,"tools");public object[] Teeth=>(object[])Get(fixture,"teeth");public object[] Trucks=>(object[])Get(fixture,"trucks");
   public IEnumerator Load()=>(IEnumerator)Call(fixture,"Load");public void Start(int n)=>Call(fixture,"Start",n);
   public IEnumerator Work(int id,string kind,Func<bool> done,float seconds)=>(IEnumerator)typeof(DentistLifecycleTests).GetMethod("ActualToolWork",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{fixture,id,kind,done,seconds,false});
   public void Observe(int id=-1)=>Call(fixture,"Observe",id,true);
   public DentistPenaltyCauseProbe[] Attach(string scenario,int count)
   {
    return Teeth.Take(count*4).Select((t,id)=>{int s=id/4;var p=((Component)t).gameObject.AddComponent<DentistPenaltyCauseProbe>();p.match=Match;p.tooth=(Component)t;p.tool=(Component)Tools[s];p.arm=(Component)Arms[s];p.scenario=scenario;return p;}).ToArray();
   }
   public void Dispose()=>((IDisposable)fixture).Dispose();
  }
  static IEnumerator Active(Original f)
  {
   float end=Time.realtimeSinceStartup+8;while(!(bool)Get(f.Rules,"Active")&&Time.realtimeSinceStartup<end)yield return null;
   Assert.That((bool)Get(f.Rules,"Active"),Is.True,"Actual original countdown must complete; no Ready override.");
  }
  static void OriginalControlledPose(Original f)
  {
   f.Match.enabled=false;Call(f.Arms[0],"Prepare",new Vector3(-51,.65f,10),Quaternion.identity);Call(f.Trucks[0],"Power",false);Call(f.Tools[0],"Prepare");
  }
  static IEnumerator MoveWrist(Original f,Vector3 goal,string stage)
  {
   object arm=f.Arms[0],tool=f.Tools[0];var grip=(Rigidbody)Get(arm,"grip");Set(tool,"Running",false);float end=Time.realtimeSinceStartup+6;
   while(Vector3.Distance(grip.position,goal)>.025f&&Time.realtimeSinceStartup<end)
   {
    Call(arm,"Move",goal,0f,false,Time.fixedDeltaTime);Call(tool,"Tick",Time.fixedDeltaTime);yield return new WaitForFixedUpdate();foreach(var t in f.Teeth.Take(4))Call(t,"Tick");
   }
   TestContext.WriteLine("DENTIST_SAFE_PHYSICAL_WAYPOINT "+stage+" requested="+goal.ToString("F6")+" actual="+grip.position.ToString("F6"));
   Assert.That(Vector3.Distance(grip.position,goal),Is.LessThan(.025f),"Actual bounded original articulated arm reaches "+stage+" without pose injection.");
   Assert.That((int[])Get(f.Rules,"points"),Is.All.Zero,"Retreat/alignment must not clobber another tooth.");
   Assert.That(f.Teeth.Take(4).All(t=>(Joint)Get(t,"Socket")!=null),Is.True,"Every original physical socket survives the safe approach.");
  }
  [UnityTest,Timeout(35000)]
  public IEnumerator OriginalDiagonalWaterApproachReportsActualToothCollisionAndPenaltyWithoutWaiver()
  {
   using(var f=new Original())
   {
    yield return f.Load();f.Start(1);yield return Active(f);OriginalControlledPose(f);var probes=f.Attach("ORIGINAL_DIAGONAL",1);int start=(int)Get(f.Match,"NativeToolContacts");
    yield return f.Work(3,"Water",()=>(int)Get(f.Match,"NativeToolContacts")>start+15,10);
    foreach(var p in probes)p.Report("ORIGINAL_WATER_COMPLETE");
    Assert.That((int[])Get(f.Rules,"points"),Is.All.Zero,"Original Water-only requirement stays strict; actual collider/socket/score-event diagnostics explain any failure.");
    Assert.That((float)Get(f.Teeth[3],"Health"),Is.EqualTo(100));Assert.That((Joint)Get(f.Teeth[3],"Socket"),Is.Not.Null);
   }
  }
  [UnityTest,Timeout(45000)]
  public IEnumerator SameOriginalRootAndToolPhysicallyRetreatThenAlignBeforeHarmlessWater()
  {
   using(var f=new Original())
   {
    yield return f.Load();f.Start(1);yield return Active(f);OriginalControlledPose(f);var probes=f.Attach("PHYSICAL_RETREAT_ALIGN",1);var grip=(Rigidbody)Get(f.Arms[0],"grip");var root=(Rigidbody)Get(f.Trucks[0],"body");Vector3 rootStart=root.position;float rootMass=root.mass;int bodyId=root.GetInstanceID();var initial=grip.position;
    yield return MoveWrist(f,new Vector3(initial.x,initial.y,13),"RETRACT_IN_CENTRAL_CORRIDOR");
    yield return MoveWrist(f,new Vector3(((Rigidbody)Get(f.Teeth[3],"body")).position.x,initial.y,13),"LATERAL_ALIGN_BEFORE_TOOTH_ROW");
    int start=(int)Get(f.Match,"NativeToolContacts");yield return f.Work(3,"Water",()=>(int)Get(f.Match,"NativeToolContacts")>start+15,10);
    foreach(var p in probes)p.Report("ALIGNED_WATER_COMPLETE");
    Assert.That((int[])Get(f.Rules,"points"),Is.All.Zero,"Same root/tool/contact authority; only physical wrist path differs.");
    Assert.That(f.Teeth.Take(4).All(t=>(Joint)Get(t,"Socket")!=null),Is.True,"All real sockets remain connected after actual Water contact.");
    Assert.That((float)Get(f.Teeth[3],"Health"),Is.EqualTo(100));Assert.That((int)Get(f.Match,"NativeExtractions"),Is.Zero);
    TestContext.WriteLine("DENTIST_UNALTERED_NWH_ROOT start="+rootStart.ToString("F6")+" actual="+root.position.ToString("F6")+" velocity="+root.linearVelocity.ToString("F6"));Assert.That(root.GetInstanceID(),Is.EqualTo(bodyId));Assert.That(root.mass,Is.EqualTo(rootMass));
   }
  }
  [UnityTest,Timeout(90000)]
  public IEnumerator ReadOnlyOriginalFourAiFrontToBackApproachAndActualCollisionTrace()
  {
   using(var f=new Original())
   {
    yield return f.Load();f.Start(4);var probes=f.Attach("ORIGINAL_FOUR_AI",4);Time.timeScale=2;float end=Time.realtimeSinceStartup+75,next=0;
    while((float)Get(f.Rules,"Clock")<65&&!(bool)Get(f.Match,"Finished")&&Time.realtimeSinceStartup<end)
    {
     if((float)Get(f.Rules,"Clock")>=next)
     {
      next=(float)Get(f.Rules,"Clock")+1;f.Observe();for(int s=0;s<4;s++)
      {
       int id=((int[])Get(f.Match,"targets"))[s];var arm=f.Arms[s];var truck=(Component)f.Trucks[s];var root=(Rigidbody)Get(f.Trucks[s],"body");var wrist=(Rigidbody)Get(arm,"grip");var tip=(Vector3)Get(f.Tools[s],"Tip");var body=(Rigidbody)Get(f.Teeth[id],"body");var mount=truck.transform.TransformPoint((Vector3)Get(arm,"mount"));Vector3 treatment=body.position+new Vector3(0,1.6f,-1.15f),desired=treatment-(tip-wrist.position);
       TestContext.WriteLine("DENTIST_AI_ROUTE owner="+s+" clock="+Get(f.Rules,"Clock")+" target="+id+" root="+root.position.ToString("F5")+" rootV="+root.linearVelocity.ToString("F5")+" rootForward="+truck.transform.forward.ToString("F5")+" wrist="+wrist.position.ToString("F5")+" requiredGrip="+desired.ToString("F5")+" requiredReach="+Vector3.Distance(mount,desired).ToString("R")+" armGoal="+Get(arm,"WorldGoal")+" points="+string.Join(",",(int[])Get(f.Rules,"points")));
      }
     }
     yield return null;
    }
    foreach(var p in probes)p.Report("ORIGINAL_AI_DIAGNOSTIC_COMPLETE");f.Observe();Assert.That((float)Get(f.Rules,"Clock"),Is.GreaterThan(12),"Read-only original AI must actually advance; no clinical completion claim from this bounded diagnostic.");
   }
  }
 }
}
#endif
