#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace LWS.TruckDentist.Tests
{
 public sealed class DentistAiSafeRouteTests
 {
  const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
  static object Get(object x,string n)=>x.GetType().GetField(n,Any) is FieldInfo f?f.GetValue(x):x.GetType().GetProperty(n,Any).GetValue(x);
  static void Set(object x,string n,object v)=>x.GetType().GetField(n,Any).SetValue(x,v);
  static object Call(object x,string n,params object[] a)=>x.GetType().GetMethods(Any).Single(m=>m.Name==n&&m.GetParameters().Length==a.Length&&m.GetParameters().Select((p,i)=>a[i]==null||p.ParameterType.IsInstanceOfType(a[i])).All(v=>v)).Invoke(x,a);
  sealed class Original:IDisposable
  {
   readonly object f=Activator.CreateInstance(typeof(DentistLifecycleTests).GetNestedType("Fixture",BindingFlags.NonPublic),true);public Behaviour Match=>(Behaviour)Get(f,"match");public object Rules=>Get(f,"rules");public object[] Arms=>(object[])Get(f,"arms");public object[] Tools=>(object[])Get(f,"tools");public object[] Teeth=>(object[])Get(f,"teeth");public object[] Trucks=>(object[])Get(f,"trucks");
   public IEnumerator Load()=>(IEnumerator)Call(f,"Load");public void Start(int count)=>Call(f,"Start",count);public void Observe()=>Call(f,"Observe",-1,true);
   public void AttachPrecisionProbes(int count){for(int slot=0;slot<count;slot++){var p=((Component)Teeth[slot*4]).gameObject.AddComponent<DentistPrecisionContactProbe>();p.match=Match;p.tooth=(Component)Teeth[slot*4];p.tool=(Component)Tools[slot];p.arm=(Component)Arms[slot];}}
   public void Dispose()=>((IDisposable)f).Dispose();
  }
  static IEnumerator Active(Original f){float end=Time.realtimeSinceStartup+8;while(!(bool)Get(f.Rules,"Active")&&Time.realtimeSinceStartup<end)yield return null;Assert.That((bool)Get(f.Rules,"Active"),Is.True,"Original actual countdown");}
  static bool Connected(object tooth)=>(Joint)Get(tooth,"Socket")!=null;
  static void Log(Original f,string path,string context)
  {
   var receipt=Get(Get(f.Match,"ScoreJournal"),"Latest");string receiptText=receipt==null?"none":Get(receipt,"EventId")+"/"+Get(receipt,"Reason")+"/"+Get(receipt,"Delta");string line="DENTIST_SAFE_ROUTE receipt="+receiptText+" maxForce="+Get(f.Match,"MaximumObservedForce")+" stages="+string.Join(",",(int[])Get(f.Rules,"stage"))+" event="+context+" fixed="+Time.fixedTime+" clock="+Get(f.Rules,"Clock")+" points="+string.Join(",",(int[])Get(f.Rules,"points"))+" mistakes="+string.Join(",",(int[])Get(f.Rules,"mistakes"))+" routeTooth="+string.Join(",",(int[])Get(f.Match,"aiRouteTooth"))+" phase="+string.Join(",",(int[])Get(f.Match,"aiRoutePhase"))+" contacts="+Get(f.Match,"NativeToolContacts")+" extractions="+Get(f.Match,"NativeExtractions")+" correctExtractions="+Get(f.Match,"CorrectExtractions");for(int s=0;s<(int)Get(Get(f.Match,"setup"),"count");s++)line+=" | owner="+s+" root="+((Rigidbody)Get(f.Trucks[s],"body")).position.ToString("F5")+" wrist="+((Rigidbody)Get(f.Arms[s],"grip")).position.ToString("F5")+" goal="+Get(f.Arms[s],"WorldGoal")+" tip="+Get(f.Tools[s],"Tip")+" physicalTip="+Get(f.Tools[s],"PhysicalTip")+" speed="+Get(f.Tools[s],"TipSpeed")+" tool="+Get(f.Tools[s],"Kind")+" running="+Get(f.Tools[s],"Running");File.AppendAllText(path,line+Environment.NewLine);Debug.Log(line);
  }
  static string Trace(string name){string folder=Path.GetFullPath("Logs/TruckSports/PlaytestRepair/DentistSafeRouteNative/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));Directory.CreateDirectory(folder);return Path.Combine(folder,name+".txt");}
  [UnityTest,Timeout(110000)]public IEnumerator ActualSameAiWaypointsDriveFrontBackAndFrontAgainWithRealWaterAndIntactSockets()
  {
   using(var f=new Original())
   {
    yield return f.Load();f.Start(1);yield return Active(f);f.Match.enabled=false;object tool=f.Tools[0];Call(tool,"Select",Enum.Parse(Get(tool,"Kind").GetType(),"Water"));string path=Trace("controlled-actual-AI-waypoints-front-back-front");var bodies=f.Teeth.Take(4).Select(t=>(Rigidbody)Get(t,"body")).ToArray();var masses=bodies.Select(b=>b.mass).ToArray();
    // Controlled target requests exercise the exact production AI waypoint
    // function. No rules stage, truck/body/grip pose, velocity or score writes.
    foreach(int id in new[]{1,3,1})
    {
     int contacts=(int)Get(f.Match,"NativeToolContacts");float end=Time.realtimeSinceStartup+30,next=0;bool complete=false;
     while(!complete&&Time.realtimeSinceStartup<end)
     {
      Set(tool,"Running",(bool)Call(f.Match,"GuideTooth",0,id,Time.fixedDeltaTime));Call(tool,"Tick",Time.fixedDeltaTime);yield return new WaitForFixedUpdate();foreach(var tooth in f.Teeth.Take(4))Call(tooth,"Tick");
      if(Time.realtimeSinceStartup>=next){next=Time.realtimeSinceStartup+.5f;Log(f,path,"CONTROLLED_TARGET_"+id);}
      Assert.That((int[])Get(f.Rules,"mistakes"),Is.All.Zero,"Safe actual route cannot hide tooth or force penalties");Assert.That(f.Teeth.Take(4).All(Connected),Is.True,"All actual sockets survive tool approach/transit/treatment");
      complete=(int)Get(f.Match,"NativeToolContacts")>contacts+15&&((int[])Get(f.Rules,"stage"))[1]>=1;
     }
     Assert.That(complete,Is.True,"Actual original NWH drive, physical arm route and native Water contact to target"+id);Log(f,path,"TARGET_COMPLETE_"+id);
    }
    Assert.That((int)Get(f.Match,"NativeExtractions"),Is.Zero);Assert.That(((int[])Get(f.Rules,"points"))[0],Is.EqualTo(10),"Only the genuine first cavity rinse awards its original10 points; healthy rinses/transit award none");Assert.That(f.Teeth.Take(4).All(t=>Mathf.Abs((float)Get(t,"Health")-100)<.001f),Is.True);CollectionAssert.AreEqual(bodies,f.Teeth.Take(4).Select(t=>(Rigidbody)Get(t,"body")).ToArray());CollectionAssert.AreEqual(masses,bodies.Select(b=>b.mass).ToArray());Set(tool,"Running",false);Call(tool,"Tick",Time.fixedDeltaTime);
   }
  }
  [UnityTest,Timeout(65000)]public IEnumerator OriginalAiWithdrawsBeforeRealWaterToBrushHandoffAndCleansAtActualGentleContact()
  {
   using(var f=new Original())
   {
    yield return f.Load();f.Start(1);f.AttachPrecisionProbes(1);yield return Active(f);f.Match.enabled=false;string path=Trace("actual-AI-rinse-withdraw-brush");var tool=f.Tools[0];bool switched=false;float end=Time.realtimeSinceStartup+45,next=0;
    while(((int[])Get(f.Rules,"stage"))[0]<2&&Time.realtimeSinceStartup<end)
    {
     string before=Get(tool,"Kind").ToString();Set(tool,"Running",false);Call(f.Match,"Bot",0,Time.fixedDeltaTime);string after=Get(tool,"Kind").ToString();if(before=="Water"&&after=="Brush"){switched=true;Vector3 tip=(Vector3)Get(tool,"Tip");var shape=((Collider[])Get(f.Teeth[0],"physical"))[0];Assert.That(Vector3.Distance(shape.ClosestPoint(tip),tip),Is.GreaterThan(1),"New solid brush must be selected only after actual physical withdrawal clear of enamel");Log(f,path,"ACTUAL_RETRACTED_SOLID_TOOL_SELECTION");}
     Call(tool,"Tick",Time.fixedDeltaTime);yield return new WaitForFixedUpdate();foreach(var tooth in f.Teeth.Take(4))Call(tooth,"Tick");if(Time.realtimeSinceStartup>=next||((int[])Get(f.Rules,"mistakes")).Any(v=>v!=0)){next=Time.realtimeSinceStartup+.5f;Log(f,path,"ACTUAL_RINSE_TO_BRUSH");}
     Assert.That((int[])Get(f.Rules,"mistakes"),Is.All.Zero);Assert.That(f.Teeth.Take(4).All(Connected),Is.True,"Actual correct gentle treatment preserves every original socket");
    }
    Assert.That(switched,Is.True);Assert.That(((int[])Get(f.Rules,"stage"))[0],Is.EqualTo(2),"Original Water then Brush stages must physically complete");Assert.That(((int[])Get(f.Rules,"points"))[0],Is.EqualTo(30));var counts=(int[])Get(f.Match,"ToolContactCounts");Assert.That(counts[0],Is.GreaterThan(0));Assert.That(counts[4],Is.GreaterThan(0));Assert.That((int)Get(f.Match,"NativeExtractions"),Is.Zero);Assert.That((float)Get(f.Teeth[0],"Health"),Is.EqualTo(100));Log(f,path,"ACTUAL_TWO_CORRECT_STAGES");Set(tool,"Running",false);Call(tool,"Tick",Time.fixedDeltaTime);
   }
  }
  [UnityTest,Timeout(100000)]public IEnumerator OriginalFourAiPhysicallyTreatBothRowsAndExtractOnlyCorrectLooseTeethWithoutTrayDamage()
  {
   using(var f=new Original())
   {
    yield return f.Load();f.Start(4);f.AttachPrecisionProbes(4);string path=Trace("natural-original-four-AI-first-checkup");Time.timeScale=2;float end=Time.realtimeSinceStartup+80,next=0;
    while((int)Get(f.Match,"CorrectExtractions")<4&&Time.realtimeSinceStartup<end)
    {
     if((float)Get(f.Rules,"Clock")>=next){next=(float)Get(f.Rules,"Clock")+1;Log(f,path,"NATURAL_FOUR_AI");f.Observe();}
     if(((int[])Get(f.Rules,"mistakes")).Any(v=>v!=0))Log(f,path,"FIRST_ACTUAL_PENALTY_BEFORE_ASSERT");Assert.That((int[])Get(f.Rules,"mistakes"),Is.All.Zero,"Genuine original AI must avoid physical tray damage/rough tool work");Assert.That((int)Get(f.Match,"NativeExtractions"),Is.EqualTo((int)Get(f.Match,"CorrectExtractions")),"No wrong socket breaks may be hidden by positive totals");Assert.That(Enumerable.Range(0,4).All(s=>Connected(f.Teeth[s*4+3])),Is.True,"All healthy back-right sockets remain connected");yield return null;
    }
    Log(f,path,"NATURAL_FIRST_CHECKUP_COMPLETE");Assert.That((int)Get(f.Match,"CorrectExtractions"),Is.EqualTo(4),"All4 original AI must reach real clamped loose-tooth extraction through actual front/back treatments");Assert.That((int[])Get(f.Match,"ToolContactCounts"),Is.All.GreaterThan(0));Assert.That((int[])Get(f.Rules,"points"),Is.All.GreaterThan(0));Assert.That(Enumerable.Range(0,4).All(s=>((bool[])Get(f.Rules,"done"))[s*4]&&((bool[])Get(f.Rules,"done"))[s*4+1]),Is.True,"Real front-row treatment completes before back extraction");
    Call(f.Match,"SetupScreen");f.Start(4);Assert.That((int[])Get(f.Match,"aiRouteTooth"),Is.All.EqualTo(-1));Assert.That((int[])Get(f.Match,"aiRoutePhase"),Is.All.Zero);Assert.That((int[])Get(f.Rules,"points"),Is.All.Zero);Assert.That((int)Get(f.Match,"NativeExtractions"),Is.Zero);
   }
  }
 }
}
#endif
