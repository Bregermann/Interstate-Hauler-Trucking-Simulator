#if UNITY_EDITOR
using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
public sealed class ArchaeologyAiWellTests
{
 const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 static object Plan()=>Activator.CreateInstance(Type.GetType("LWS.TruckArchaeology.ArchaeologyAiWell, Assembly-CSharp",true));
 static object Get(object p,string n)=>p.GetType().GetProperty(n,Any).GetValue(p);
 static object Call(object p,string n,params object[] args)=>p.GetType().GetMethod(n,Any).Invoke(p,args);
 [Test]public void ActualV30FinePointCommitsOneBrushRadiusOfRealLocalSurfaceProgressBeforeReacquisition()
 {
  var plan=Plan();Call(plan,"ChangeArtifact",1);Assert.That((bool)Call(plan,"BeginSample",20f),Is.True);Vector3 surface=new Vector3(-53.25f,3.58f,6.5f);Assert.That((bool)Call(plan,"Acquire",surface,.6f,1.15f),Is.True);Assert.That((float)Get(plan,"TargetSurfaceY"),Is.EqualTo(2.98f).Within(1e-6f));
  foreach(float y in new[]{3.5f,3.3f,3.05f})
  {
   Assert.That((bool)Call(plan,"Observe",new Vector3(surface.x,y,surface.z),1.15f),Is.True);Assert.That((Vector3)Get(plan,"SamplePosition"),Is.EqualTo(surface),"Well XZ remains at its actual acquisition sample, independent of another distant highest crest");Assert.That((Vector3)Get(plan,"Point"),Is.EqualTo(new Vector3(surface.x,y-.2f,surface.z)),"Y is the actual supplied surface minus original .2m offset, never an invented excavated height");
  }
  Assert.That((bool)Call(plan,"Observe",new Vector3(surface.x,2.97f,surface.z),1.15f),Is.False);Assert.That((bool)Get(plan,"Active"),Is.False);Assert.That((int)Get(plan,"CompletedSteps"),Is.EqualTo(1));
 }
 [Test]public void SampleCadenceIsOneQuarterSecondAcrossCompletionAndArtifactChanges()
 {
  var plan=Plan();Assert.That((bool)Call(plan,"BeginSample",0f),Is.True);for(int i=1;i<25;i++)Assert.That((bool)Call(plan,"BeginSample",i*.01f),Is.False);Call(plan,"ChangeArtifact",4);Assert.That((bool)Call(plan,"BeginSample",.249f),Is.False,"Changing targets cannot create extra ray queries");Assert.That((bool)Call(plan,"BeginSample",.25f),Is.True);Assert.That((int)Get(plan,"Queries"),Is.EqualTo(2));
 }
 [Test]public void ActualBottomAndMissingOrInvalidSampleCancelOnlyPlanAndNeverInventExposure()
 {
  var plan=Plan();Vector3 surface=new Vector3(-54,1.4f,5);Call(plan,"Acquire",surface,.6f,1.15f);Assert.That((float)Get(plan,"TargetSurfaceY"),Is.EqualTo(1.15f));Assert.That((bool)Call(plan,"Observe",new Vector3(-54,1.15f,5),1.15f),Is.False);Assert.That((bool)Get(plan,"Active"),Is.False);
  Assert.That((bool)Call(plan,"Acquire",Vector3.positiveInfinity,.6f,1.15f),Is.False);Call(plan,"Acquire",surface,.6f,1.15f);Call(plan,"Invalidate");Assert.That((bool)Get(plan,"Active"),Is.False);Call(plan,"Acquire",surface,.6f,1.15f);Assert.That((bool)Call(plan,"Observe",new Vector3(-50,1.3f,5),1.15f),Is.False,"A different real sample XZ must reacquire, not shift a committed well silently");Assert.That((bool)Get(plan,"Active"),Is.False);Call(plan,"ChangeArtifact",7);Assert.That((int)Get(plan,"Artifact"),Is.EqualTo(7));
 }
}
#endif
