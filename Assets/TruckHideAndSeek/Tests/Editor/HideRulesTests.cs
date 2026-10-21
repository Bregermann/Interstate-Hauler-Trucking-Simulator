using System;using NUnit.Framework;
public sealed class HideRulesTests
{
 Rules r;[SetUp]public void Setup(){r=new Rules();r.Reset(8,8);r.Begin();}
 [Test]public void ThirtySecondHidingPhasePrecedesSeeking(){r.Tick(29.99f);Assert.AreEqual("Hiding",r.Phase);r.Tick(.01f);Assert.AreEqual("Seeking",r.Phase);}
 [Test]public void SeekerCannotTagDuringHiding(){Assert.False(r.Tag(0,1));Assert.AreEqual(0,r.points[0]);}
 [Test]public void ActualSeekerTagScoresOnce(){r.Tick(30);Assert.True(r.Tag(0,1));Assert.False(r.Tag(0,1));Assert.AreEqual(1,r.points[0]);}
 [Test]public void CaughtHiderDoesNotBecomeASeeker(){r.Tick(30);r.Tag(0,1);Assert.False(r.Tag(1,2));Assert.AreEqual(0,r.Seeker);}
 [Test]public void OriginalSeekerCannotTagItself(){r.Tick(30);Assert.False(r.Tag(0,0));}
 [Test]public void EveryHiderMustBePhysicallyTaggedForSweep(){r.Tick(30);for(int i=1;i<7;i++)r.Tag(0,i);Assert.AreEqual("Seeking",r.Phase);Assert.AreEqual(1,r.Remaining);r.Tag(0,7);Assert.AreEqual("RoundEnd",r.Phase);Assert.AreEqual(10,r.points[0]);}
 [Test]public void TimeoutRewardsOnlySurvivingHiders(){r.Tick(30);r.Tag(0,1);r.Tick(180);Assert.AreEqual(0,r.points[1]);for(int i=2;i<8;i++)Assert.AreEqual(2,r.points[i]);}
 [Test]public void LastSurvivorGetsVisibleOptionalBonus(){r.Tick(30);for(int i=1;i<7;i++)r.Tag(0,i);r.Tick(180);Assert.AreEqual(3,r.points[7]);}
 [Test]public void EndedRoundRejectsLateTags(){r.Tick(210);Assert.False(r.Tag(0,1));}
 [Test]public void RotationKeepsOneSeekerAndClearsOnlyRoundState(){r.Tick(30);r.Tag(0,1);r.Tick(182.5f);Assert.AreEqual("Hiding",r.Phase);Assert.AreEqual(1,r.Seeker);Assert.False(r.found[1]);Assert.AreEqual(1,r.points[0]);}
 [Test]public void FullRotationGivesEachSlotOneSeekerTurn(){for(int i=0;i<8;i++){Assert.AreEqual(i,r.Seeker);r.Tick(212.5f);}Assert.AreEqual("Complete",r.Phase);}
 [Test]public void FinalResultsAreLocked(){r.Reset(2,1);r.Begin();r.Tick(30);r.Tag(0,1);r.Tick(2.5f);Assert.AreEqual("Complete",r.Phase);int p=r.points[0];Assert.False(r.Tag(0,1));r.Tick(500);Assert.AreEqual(p,r.points[0]);}
 [Test]public void EqualPointsSharePlacing(){Assert.AreEqual(1,r.Place(0));Assert.AreEqual(1,r.Place(7));}
 [Test]public void OneRoundModeCompletesWithoutExtraSeeker(){r.Reset(8,1);r.Begin();r.Tick(212.5f);Assert.AreEqual("Complete",r.Phase);}
 [Test]public void InvalidTagReferencesAreRejected(){r.Tick(30);Assert.False(r.Tag(0,-1));Assert.False(r.Tag(0,8));Assert.False(r.Tag(7,1));}
 [Test]public void RematchClearsTotalsAndFoundState(){r.Tick(30);r.Tag(0,1);r.Reset(8,8);Assert.AreEqual(0,r.points[0]);Assert.False(r.found[1]);Assert.AreEqual("Setup",r.Phase);}
 [Test]public void DeltaCanCrossPhaseBoundaryWithoutLosingTime(){r.Tick(31);Assert.AreEqual("Seeking",r.Phase);Assert.AreEqual(1,r.Clock,.001f);}
 static object Signal(string method,params object[] args)=>Type.GetType("LWS.TruckHideAndSeek.HideSignalRules, Assembly-CSharp",true).GetMethod(method).Invoke(null,args);
 [Test]public void QuietDarkTruckHasShorterVisualRange(){float dark=(float)Signal("VisualRange",false,false,false,false,1f),lit=(float)Signal("VisualRange",true,true,true,true,1f);Assert.Less(dark,lit);Assert.AreEqual(45,dark);}
 [Test]public void ActualSolidOcclusionAlwaysBlocksVisualDiscovery(){Assert.False((bool)Signal("CanSee",10f,1f,true,true,true,true,true,1f));Assert.True((bool)Signal("CanSee",10f,1f,false,true,true,true,true,1f));}
 [Test]public void EngineNoiseIsAttenuatedThroughWalls(){Assert.Less((float)Signal("AudibleRange",true,0f,true),(float)Signal("AudibleRange",true,0f,false));}
 [Test]public void StoppedEngineHasNoStationarySoundHint(){Assert.AreEqual(0,(float)Signal("AudibleRange",false,0f,false));Assert.Greater((float)Signal("AudibleRange",false,4f,false),0);}
 [Test]public void SectorHintsAreQuantizedRatherThanExactBearings(){Assert.AreEqual(0,(int)Signal("Sector",12f));Assert.AreEqual(0,(int)Signal("Sector",-12f));Assert.AreEqual(1,(int)Signal("Sector",23f));Assert.AreEqual(1,(int)Signal("Sector",60f));Assert.AreEqual(2,(int)Signal("Sector",90f));}
 [Test]public void DistanceHintPublishesOnlyBroadBands(){Assert.AreEqual("NEAR",Signal("Band",40f));Assert.AreEqual("MID-RANGE",Signal("Band",100f));Assert.AreEqual("DISTANT",Signal("Band",400f));}
 [Test]public void KnowledgeRemembersOnlyTheLastActualObservation(){var type=Type.GetType("LWS.TruckHideAndSeek.HideKnowledge, Assembly-CSharp",true);var k=Activator.CreateInstance(type);type.GetMethod("Reset").Invoke(k,null);var observed=new UnityEngine.Vector3(15,2,30);type.GetMethod("See").Invoke(k,new object[]{2,observed,10f});Assert.True((bool)type.GetMethod("Recent").Invoke(k,new object[]{2,12f,5f}));Assert.False((bool)type.GetMethod("Recent").Invoke(k,new object[]{2,16f,5f}));Assert.AreEqual(observed,((UnityEngine.Vector3[])type.GetField("lastSeen").GetValue(k))[2]);}
 [Test]public void AudioHintDoesNotStoreAnExactObservedPosition(){var type=Type.GetType("LWS.TruckHideAndSeek.HideKnowledge, Assembly-CSharp",true);var k=Activator.CreateInstance(type);type.GetMethod("Reset").Invoke(k,null);var origin=UnityEngine.Vector3.zero;type.GetMethod("Hear").Invoke(k,new object[]{origin,new UnityEngine.Vector3(8,0,100),5f});Assert.AreEqual(0,type.GetProperty("SoundSector").GetValue(k));Assert.AreEqual("MID-RANGE",type.GetProperty("SoundBand").GetValue(k));Assert.AreEqual(new UnityEngine.Vector3(0,0,100),type.GetMethod("SectorGoal").Invoke(k,new object[]{origin}));Assert.True(Array.TrueForAll((float[])type.GetField("seenAt").GetValue(k),t=>t<0));}
 [Test]public void RoundResetErasesAllExactKnowledgeAndSoundHints(){var type=Type.GetType("LWS.TruckHideAndSeek.HideKnowledge, Assembly-CSharp",true);var k=Activator.CreateInstance(type);type.GetMethod("See").Invoke(k,new object[]{1,UnityEngine.Vector3.one,2f});type.GetMethod("Hear").Invoke(k,new object[]{UnityEngine.Vector3.zero,UnityEngine.Vector3.one,3f});type.GetMethod("Reset").Invoke(k,null);Assert.AreEqual(-1,type.GetProperty("SoundSector").GetValue(k));Assert.True(Array.TrueForAll((float[])type.GetField("seenAt").GetValue(k),t=>t<0));}
 sealed class Rules
 {
  readonly Type type=Type.GetType("LWS.TruckHideAndSeek.HideRules, Assembly-CSharp",true);readonly object value;public Rules(){value=Activator.CreateInstance(type);}object Call(string n,params object[] a)=>type.GetMethod(n).Invoke(value,a);T Read<T>(string n)=>(T)type.GetProperty(n).GetValue(value);T Field<T>(string n)=>(T)type.GetField(n).GetValue(value);public string Phase=>Read<object>("Phase").ToString();public float Clock=>Read<float>("Clock");public int Seeker=>Read<int>("Seeker");public int Remaining=>Read<int>("Remaining");public int[] points=>Field<int[]>("points");public bool[] found=>Field<bool[]>("found");public void Reset(int p,int rounds)=>Call("Reset",p,rounds,30f,180f);public void Begin()=>Call("Begin");public void Tick(float dt)=>Call("Tick",dt);public bool Tag(int a,int b)=>(bool)Call("Tag",a,b);public int Place(int i)=>(int)Call("Place",i);
 }
}
