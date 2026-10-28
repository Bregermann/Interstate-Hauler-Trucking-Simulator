#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
public sealed class ArchaeologyFourAiObservationTests
{
 const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 static object Get(object x,string n)=>x.GetType().GetField(n,Any) is FieldInfo f?f.GetValue(x):x.GetType().GetProperty(n,Any).GetValue(x);
 static object Call(object x,string n,params object[] args)=>x.GetType().GetMethods(Any).Single(m=>m.Name==n&&m.GetParameters().Length==args.Length).Invoke(x,args);
 [UnityTest,Timeout(180000)]public IEnumerator ActualFourAiNinetyRealSecondsReportsOriginalToolsWorkpointsClearanceAndAcceptedVendorCuts()
 {
  var fixtureType=typeof(ArchaeologyPlayModeTests).GetNestedType("Fixture",BindingFlags.NonPublic);var fixture=(IDisposable)Activator.CreateInstance(fixtureType,true);
  using(fixture)
  {
   yield return (IEnumerator)Call(fixture,"Load");var match=(Behaviour)Get(fixture,"match");
   // Exact V13 setup boundary. Persistent earlier SOLO preferences are not a
   // substitute for four real AI owners and twelve active original finds.
   Call(match,"SetupScreen");var actualSetup=Get(match,"setup");actualSetup.GetType().GetField("count").SetValue(actualSetup,4);Call(match,"NormalizeSetup");Call(match,"AllBots");Assert.That((bool)Call(match,"StartMatch"),Is.True,"Four actual AI crews must start through the original setup boundary");
   var rules=Get(match,"Rules");var finds=((Array)Get(match,"artifacts")).Cast<Component>().ToArray();Assert.That((int)Get(actualSetup,"count"),Is.EqualTo(4));Assert.That((int)Get(rules,"Players"),Is.EqualTo(4));Assert.That((bool)Get(match,"AllAI"),Is.True);Assert.That(finds.Count(a=>a.gameObject.activeInHierarchy),Is.EqualTo(12));
   var trucks=((Array)Get(match,"trucks")).Cast<Component>().ToArray();Assert.That(trucks.Count(t=>t.gameObject.activeInHierarchy),Is.EqualTo(4));var originalBodies=finds.Select(a=>(Rigidbody)Get(a,"body")).ToArray();float fixedStep=Time.fixedDeltaTime;Assert.That(Time.timeScale,Is.EqualTo(1),"Live original simulation speed, no accelerated completion claim");
   string directory=Path.GetFullPath("Logs/TruckSports/PlaytestRepair/ArchaeologyFourAi90Native/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")+"-"+Guid.NewGuid().ToString("N").Substring(0,8));Directory.CreateDirectory(directory);string path=Path.Combine(directory,"original-four-AI-90-real-seconds-owner-tool-earth.txt");var utf8=new UTF8Encoding(false);var snapshot=typeof(ArchaeologyExcavationDiagnostics).GetMethod("Snapshot",BindingFlags.Static|BindingFlags.NonPublic);var observer=match.gameObject.AddComponent<ArchaeologyFourAiCutProbe>();observer.match=match;observer.path=Path.Combine(directory,"actual-accepted-vendor-cuts.txt");File.WriteAllText(observer.path,"READ ONLY actual accepted Digger edits. Sphere-envelope volume is requested brush geometry; no net soil-volume claim.\n",utf8);observer.StartObservation();
   File.WriteAllText(path,"MEASUREMENT ONLY actual4AI/original fixedstep/live1x/90real seconds; not full600/full12 completion acceptance.\nsetupCount="+Get(actualSetup,"count")+" option="+Get(match,"option")+" rulePlayers="+Get(rules,"Players")+" sources="+string.Join(",",((Array)Get(actualSetup,"players")).Cast<object>().Take(4).Select(p=>Get(p,"source")))+" activeOriginalFinds="+finds.Count(a=>a.gameObject.activeInHierarchy)+"\n",utf8);
   object earth=Get(match,"earth");int editsBefore=(int)Get(earth,"ActualEdits"),strokesBefore=(int)Get(match,"ToolStrokes");float start=Time.realtimeSinceStartup,deadline=start+90,next=start;int samples=0;
   while(Time.realtimeSinceStartup<deadline&&!(bool)Get(match,"Finished"))
   {
    if(Time.realtimeSinceStartup>=next){next=Time.realtimeSinceStartup+10;string state="REAL_SECONDS="+(Time.realtimeSinceStartup-start).ToString("R")+" actualEditDelta="+((int)Get(earth,"ActualEdits")-editsBefore)+" originalStrokeDelta="+((int)Get(match,"ToolStrokes")-strokesBefore)+"\n"+(string)snapshot.Invoke(null,new object[]{match})+observer.Summary();File.AppendAllText(path,state+"\n",utf8);Debug.Log(state);samples++;}yield return null;
   }
   string final="FINAL_REAL_SECONDS="+(Time.realtimeSinceStartup-start).ToString("R")+"\n"+(string)snapshot.Invoke(null,new object[]{match})+observer.Summary();File.AppendAllText(path,final+"\n",utf8);Debug.Log(final);
   Assert.That((int)Get(actualSetup,"count"),Is.EqualTo(4));Assert.That((int)Get(rules,"Players"),Is.EqualTo(4));Assert.That(Time.fixedDeltaTime,Is.EqualTo(fixedStep));Assert.That(Time.timeScale,Is.EqualTo(1));Assert.That(samples,Is.GreaterThanOrEqualTo(2),"Bounded original simulation must be observed over time");Assert.That((float)Get(match,"Clock"),Is.GreaterThan(20));Assert.That((int)Get(earth,"ActualEdits")-editsBefore,Is.GreaterThan(12),"Real originalAI must issue genuine supported vendor edits; no manual cuts/poses/tools/outcomes supplied");Assert.That(observer.ObservedEdits,Is.GreaterThan(0));Assert.That(observer.UnattributedEdits,Is.Zero,"Every accepted cut must be measured after its actual fixed-step authority, never guessed across lost edits");CollectionAssert.AreEqual(originalBodies,finds.Select(a=>(Rigidbody)Get(a,"body")).ToArray());
   Assert.That(observer.DuplicateOwnersInPass,Is.Zero,"At most one accepted cut per owner in each original .2s pass");Assert.That(observer.MaximumCutsInPass,Is.InRange(2,4),"Multiple original working crews must receive serial cuts, bounded4, instead of old global5Hz starvation");Assert.That(observer.FullCrewPasses,Is.GreaterThanOrEqualTo(3),"Actual natural4AI must demonstrate repeated same-pass service of every owner");Assert.That(observer.MinimumOwnerInterval,Is.GreaterThanOrEqualTo(.199f),"Each crew retains its original .2s cooldown; fixed-clock float subtraction tolerance1ms only");Assert.That((observer.StartingOwnerMask&(observer.StartingOwnerMask-1))!=0,Is.True,"First serial access must rotate across actual owners");
   var authoritative=(System.Collections.Generic.IReadOnlyList<int>)Get(match,"AcceptedCuts");Assert.That(authoritative.Sum(),Is.EqualTo(observer.ObservedEdits));for(int owner=0;owner<4;owner++){int measured=0;for(int tool=0;tool<6;tool++)measured+=observer.Cuts[owner,tool];Assert.That(authoritative[owner],Is.EqualTo(measured),"Exact accepted owner receipts match original runtime counters");Assert.That(measured,Is.GreaterThan(3),"No holding naturalAI crew may be starved by other owners");}
   var actualPlans=(Array)Get(match,"aiWells");for(int owner=0;owner<4;owner++){object plan=actualPlans.GetValue(owner);Assert.That((int)Get(plan,"Queries"),Is.LessThanOrEqualTo(Mathf.CeilToInt((float)Get(match,"Clock")/.25f)+1),"Actual extra local surface queries stay at existing quarter-second cadence, never per50Hz call");Assert.That((int)Get(plan,"Acquisitions"),Is.GreaterThan(0));Assert.That((int)Get(plan,"CompletedSteps"),Is.GreaterThan(0),"Original AI must complete real brush-radius progress and reacquire actual cached soil; no permanent lock on a failed well");}
   TestContext.WriteLine("ARCHAEOLOGY_FOUR_AI_90_MEASUREMENT "+path+"; actual accepted vendor cut receipts="+observer.path+". No full completion, net voxel volume, Windows disk persistence or human/device acceptance inferred.");UnityEngine.Object.Destroy(observer);
  }
 }
}
#endif
