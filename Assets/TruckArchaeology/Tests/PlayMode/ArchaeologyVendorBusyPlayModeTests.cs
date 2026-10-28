#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Digger.Modules.Core.Sources;
using Digger.Modules.Runtime.Sources;
public sealed class ArchaeologyVendorBusyPlayModeTests
{
 const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 static object Get(object x,string n)=>x.GetType().GetField(n,Any) is FieldInfo f?f.GetValue(x):x.GetType().GetProperty(n,Any).GetValue(x);
 static object Call(object x,string n,params object[] args)=>x.GetType().GetMethods(Any).Single(m=>m.Name==n&&m.GetParameters().Length==args.Length).Invoke(x,args);
 [UnityTest,Timeout(90000)]public IEnumerator RealVendorAsyncBusyRefusesAdapterCutWithoutPhantomStrokeOwnerCounterOrReceipt()
 {
  var type=typeof(ArchaeologyPlayModeTests).GetNestedType("Fixture",BindingFlags.NonPublic);var fixture=(IDisposable)Activator.CreateInstance(type,true);
  using(fixture)
  {
   yield return (IEnumerator)Call(fixture,"Load");var match=(Behaviour)Get(fixture,"match");match.enabled=false;var earth=Get(match,"earth");var runtime=(DiggerMasterRuntime)Get(earth,"runtime");Assert.That((bool)Get(earth,"Ready"),Is.True);int edits=(int)Get(earth,"ActualEdits"),strokes=(int)Get(match,"ToolStrokes");var counter=(System.Collections.Generic.IReadOnlyList<int>)Get(match,"AcceptedCuts");int[] before=counter.ToArray();int receipts=0;Action<int,int,int,Vector3,float,float,float,int> listener=(owner,target,tool,point,radius,opacity,clock,ordinal)=>receipts++;var acceptedEvent=match.GetType().GetEvent("CutAccepted");acceptedEvent.AddEventHandler(match,listener);Vector3 marked=(Vector3)Call(match,"Plot",0)+Vector3.up*6.8f;
   // An actual supported vendor operation owns async. Never set a private
   // vendor busy flag or fabricate an accepted mode receipt/counter.
   var operation=runtime.ModifyAsync(new ModificationParameters{Position=marked,Brush=BrushType.Sphere,Action=ActionType.Dig,Size=.6f,Opacity=.5f});var awaiter=operation.GetAwaiter();bool actualBusy=runtime.IsRunningAsync;bool accepted=(bool)Call(earth,"Cut",marked,.6f,.5f);int afterEdits=(int)Get(earth,"ActualEdits"),afterStrokes=(int)Get(match,"ToolStrokes");int[] after=counter.ToArray();int afterReceipts=receipts;acceptedEvent.RemoveEventHandler(match,listener);float end=Time.realtimeSinceStartup+20;while(!awaiter.IsCompleted&&Time.realtimeSinceStartup<end)yield return null;Assert.That(awaiter.IsCompleted,Is.True,"Actual vendor async must finish normally before fixture cleanup");awaiter.GetResult();yield return null;yield return null;
   Assert.That(actualBusy,Is.True,"Real vendor asynchronous operation, not an invented busy condition");Assert.That(accepted,Is.False);Assert.That(afterEdits,Is.EqualTo(edits));Assert.That(afterStrokes,Is.EqualTo(strokes));CollectionAssert.AreEqual(before,after);Assert.That(afterReceipts,Is.Zero,"Refused vendor cut must not publish a successful owner receipt");Assert.That(runtime.IsRunningAsync,Is.False);Assert.That((bool)Get(earth,"Ready"),Is.True);TestContext.WriteLine("ARCHAEOLOGY_ACTUAL_VENDOR_BUSY refused adapter cut with unchanged actual mode edits/strokes/owner counters; external supported vendor async completed; no full-session outcome inference.");
  }
 }
}
#endif
