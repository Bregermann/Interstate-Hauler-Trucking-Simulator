using System;
using System.Reflection;
using LWS.InterstateHauler;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiFrameSpikeDiagnosticsTests
    {
        private GameObject root;
        private TruckTaxiFrameSpikeDiagnostics capture;
        [SetUp] public void SetUp()
        { root=new GameObject("Frame capture contract");capture=root.AddComponent<TruckTaxiFrameSpikeDiagnostics>(); }
        [TearDown] public void TearDown() { if(root!=null) Object.DestroyImmediate(root); }

        [Test] public void FreshCaptureHasBoundedStorageAndNoSamples()
        {
            Assert.AreEqual(33,capture.warningMilliseconds);Assert.AreEqual(50,capture.severeMilliseconds);
            Assert.IsFalse(capture.logSpikes);Assert.AreEqual(128,TruckTaxiFrameSpikeDiagnostics.Capacity);
            Assert.Zero(capture.CopyChronological(Array.Empty<TruckTaxiFrameSpikeDiagnostics.Sample>()));
            AssertEmpty();
        }

        [TestCase(1)]
        [TestCase(128)]
        [TestCase(129)]
        [TestCase(300)]
        public void IngestAndCopyRetainNewest128InChronologicalOrder(int total)
        {
            for(int i=1;i<=total;i++) Assert.IsTrue(capture.Ingest(Sample(i)));
            int count=Math.Min(total,TruckTaxiFrameSpikeDiagnostics.Capacity);
            var copy=new TruckTaxiFrameSpikeDiagnostics.Sample[128];
            Assert.AreEqual(count,capture.CopyChronological(copy));
            for(int i=0;i<count;i++) Assert.AreEqual(total-count+i+1,copy[i].frame);
            Assert.AreEqual(count,capture.SampleCount);
            Assert.AreEqual(total,capture.WarningCount);Assert.AreEqual(total,capture.SevereCount);
            string summary=capture.Summary;
            Assert.That(summary,Does.Contain("FRAME "+total+" "));
            Assert.That(summary,Does.Contain("GC BYTES -1  COLLECTIONS 2  DRAWS -1"));
            Assert.That(summary,Does.Contain("TRAFFIC 12  PEOPLE 7  RIVALS 20  REGIONS 3  EVENTS 1"));
            Assert.That(summary,Does.Contain("PAUSED True  RACING True  SAVING True  LOADING False"));
            Assert.That(summary,Does.Contain("WEATHER snow  REGION town  STREAM PENDING 2"));
            Assert.That(summary,Does.Contain("NAV TARGET True / READY True / SUPPRESSED False / HUD ENABLED True / FULL MAP False"));
            Assert.That(summary,Does.Contain("Correlation is not a measured cause"));
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        [TestCase(-1f)]
        [TestCase(0f)]
        [TestCase(32.99f)]
        public void InvalidOrBelowThresholdSamplesAreRejected(float milliseconds)
        {
            var sample=Sample(1);sample.milliseconds=milliseconds;
            Assert.IsFalse(capture.Ingest(sample));AssertEmpty();
        }

        [Test] public void WarningAndSevereThresholdsAreInclusive()
        {
            var sample=Sample(1);sample.milliseconds=33;Assert.IsTrue(capture.Ingest(sample));
            Assert.Zero(capture.SevereCount);
            sample.milliseconds=49.99f;capture.Ingest(sample);Assert.Zero(capture.SevereCount);
            sample.milliseconds=50;capture.Ingest(sample);
            Assert.AreEqual(3,capture.WarningCount);Assert.AreEqual(1,capture.SevereCount);
            Assert.AreEqual(50,capture.PeakMilliseconds);
        }

        [Test] public void InvalidConfigurationUsesFiniteOrderedThresholds()
        {
            capture.warningMilliseconds=float.NaN;capture.severeMilliseconds=float.NaN;
            var sample=Sample(1);sample.milliseconds=32;Assert.IsFalse(capture.Ingest(sample));
            sample.milliseconds=33;Assert.IsTrue(capture.Ingest(sample));Assert.Zero(capture.SevereCount);
            capture.warningMilliseconds=60;capture.severeMilliseconds=10;
            sample.milliseconds=59;Assert.IsFalse(capture.Ingest(sample));
            sample.milliseconds=60;Assert.IsTrue(capture.Ingest(sample));Assert.AreEqual(1,capture.SevereCount);
        }

        [Test] public void CopyIsBoundedSupportsOffsetsAndCannotMutateStoredSamples()
        {
            for(int i=1;i<=130;i++) capture.Ingest(Sample(i));
            var copy=new TruckTaxiFrameSpikeDiagnostics.Sample[4];copy[0]=Sample(999);
            Assert.AreEqual(3,capture.CopyChronological(copy,1));
            Assert.AreEqual(999,copy[0].frame);Assert.AreEqual(3,copy[1].frame);Assert.AreEqual(5,copy[3].frame);
            copy[1].frame=-1;capture.CopyChronological(copy,1);Assert.AreEqual(3,copy[1].frame);
            Assert.Zero(capture.CopyChronological(copy,copy.Length));
            Assert.Throws<ArgumentNullException>(()=>capture.CopyChronological(null));
            Assert.Throws<ArgumentOutOfRangeException>(()=>capture.CopyChronological(copy,-1));
            Assert.Throws<ArgumentOutOfRangeException>(()=>capture.CopyChronological(copy,5));
        }

        [Test] public void ClearResetsGcBaselineCountersAndLogCooldownWithoutReplacingBuffer()
        {
            var buffer=Field("history").GetValue(capture);
            for(int i=0;i<300;i++) capture.Ingest(Sample(9123));
            Field("collections").SetValue(capture,-100);Field("nextLog").SetValue(capture,float.MaxValue);
            capture.warningMilliseconds=40;capture.severeMilliseconds=80;capture.logSpikes=true;
            int before=GC.CollectionCount(0);capture.Clear();capture.Clear();
            AssertEmpty();
            Assert.That(Field("history").GetValue(capture),Is.SameAs(buffer));
            Assert.That(Field("collections").GetValue(capture),Is.InRange(before,GC.CollectionCount(0)));
            Assert.AreEqual(0f,Field("nextLog").GetValue(capture));
            Assert.AreEqual(40,capture.warningMilliseconds);Assert.AreEqual(80,capture.severeMilliseconds);
            Assert.IsTrue(capture.logSpikes);
            Assert.Zero(capture.CopyChronological(new TruckTaxiFrameSpikeDiagnostics.Sample[128]));
            Assert.IsTrue(capture.Ingest(Sample(4321)));
            Assert.That(capture.Summary,Does.Contain("FRAME 4321 "));
            Assert.That(capture.Summary,Does.Not.Contain("FRAME 9123 "));
        }

        [Test] public void UnboundUpdateDoesNotCaptureOrRequireRuntimeHost()
        { Method("Update").Invoke(capture,null);AssertEmpty(); }

        [Test] public void UnavailableRecorderReportsNegativeTimeNotFalseZero()
        {
            Assert.AreEqual(-1,TruckTaxiFrameSpikeDiagnostics.ReadCounter(default));
            Assert.AreEqual(-1f,Method("Milliseconds").Invoke(null,new object[]{default(ProfilerRecorder)}));
        }

        [Test] public void ValidButEmptyRecorderIsUnavailable()
        {
            // Markers require an aggregation flag; omit StartImmediately so the valid recorder stays empty.
            using(var recorder=new ProfilerRecorder(ProfilerCategory.Internal,"Main Thread",1,ProfilerRecorderOptions.SumAllSamplesInFrame))
            {
                Assert.IsTrue(recorder.Valid);Assert.Zero(recorder.Count);
                Assert.AreEqual(-1,TruckTaxiFrameSpikeDiagnostics.ReadCounter(recorder));
                Assert.AreEqual(-1f,Method("Milliseconds").Invoke(null,new object[]{recorder}));
            }
        }

        [TestCase(true,false,false)]
        [TestCase(false,true,false)]
        [TestCase(false,false,true)]
        public void ObservedSavingComesFromSaveServiceNotLoadPreparation(bool saving,bool busy,bool loading)
        {
            var ownerRoot=new GameObject("Inactive diagnostics host");ownerRoot.SetActive(false);
            try
            {
                var owner=ownerRoot.AddComponent<TruckTaxiBootstrap>();
                var persistence=ownerRoot.AddComponent<TruckTaxiVibinPersistence>();
                var saves=new LwsSaveService(includeDefaultParticipants:false);
                SetProperty(saves,"IsSaving",saving);SetProperty(saves,"IsLoading",loading);
                SetProperty(persistence,"IsBusy",busy);SetProperty(persistence,"SaveService",saves);
                SetProperty(owner,"Persistence",persistence);Field("host").SetValue(capture,owner);
                var sample=(TruckTaxiFrameSpikeDiagnostics.Sample)Method("Observe").Invoke(capture,new object[]{65f,0});
                Assert.IsTrue(sample.saveAvailable);Assert.AreEqual(saving,sample.saving);
                Assert.AreEqual(busy || loading,sample.loading);
                Assert.IsFalse(sample.navigationAvailable);Assert.AreEqual(-1,sample.pendingStreamOperations);
                Assert.AreEqual(-1,sample.snowCacheTargets);Assert.AreEqual(-1,sample.gcBytes);
                capture.Ingest(sample);Assert.That(capture.Summary,Does.Contain("NAV UNAVAILABLE"));
            }
            finally { Object.DestroyImmediate(ownerRoot); }
        }

        [Test] public void UninitializedCaptureCanBeDestroyed()
        { Assert.DoesNotThrow(()=>Object.DestroyImmediate(root));root=null; }

        private void AssertEmpty()
        {
            Assert.Zero(capture.SampleCount);Assert.Zero(capture.WarningCount);
            Assert.Zero(capture.SevereCount);Assert.Zero(capture.PeakMilliseconds);
            Assert.That(capture.Summary,Does.Contain("NO CAPTURED SPIKES"));
        }
        private static FieldInfo Field(string name) => typeof(TruckTaxiFrameSpikeDiagnostics)
            .GetField(name,BindingFlags.Instance|BindingFlags.NonPublic);
        private static MethodInfo Method(string name) => typeof(TruckTaxiFrameSpikeDiagnostics)
            .GetMethod(name,BindingFlags.Instance|BindingFlags.Static|BindingFlags.NonPublic);
        private static void SetProperty(object target,string name,object value) => target.GetType()
            .GetProperty(name).GetSetMethod(true).Invoke(target,new[]{value});
        private static TruckTaxiFrameSpikeDiagnostics.Sample Sample(int frame) =>
            new TruckTaxiFrameSpikeDiagnostics.Sample {
                frame=frame,milliseconds=65,mainMs=-1,renderMs=-1,physicsMs=-1,gcBytes=-1,drawCalls=-1,
                gcCollections=2,traffic=12,pedestrians=7,rivals=20,regions=3,events=1,paused=true,racing=true,
                saving=true,saveAvailable=true,realtimeSeconds=frame,weatherId="snow",regionId="town",
                pendingStreamOperations=2,navigationAvailable=true,hasNavigationTarget=true,routeReady=true,hudMapEnabled=true
            };
    }
}
