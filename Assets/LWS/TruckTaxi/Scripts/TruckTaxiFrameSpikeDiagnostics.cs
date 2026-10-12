using System;
using LWS.InterstateHauler;
using Unity.Profiling;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // Bounded development telemetry, separate from the opt-in driving workload harness.
    public sealed class TruckTaxiFrameSpikeDiagnostics : MonoBehaviour
    {
        [Min(1)] public float warningMilliseconds=33,severeMilliseconds=50;
        public bool logSpikes;
        private TruckTaxiBootstrap host;
        private TruckTaxiRegionalWorld world;
        private LwsSceneStreamerAdapter streamer;
        private TruckTaxiSnowSurface snow;
        private ProfilerRecorder main,render,physics,gc,draws;
        public const int Capacity=128;
        private readonly Sample[] history=new Sample[Capacity];
        private int count,next,collections;
        private float nextLog;
        public int WarningCount { get; private set; }
        public int SevereCount { get; private set; }
        public int SampleCount => count;
        public float PeakMilliseconds { get; private set; }
        public struct Sample
        {
            public int frame,gcCollections,traffic,pedestrians,rivals,regions,events,drawCalls;
            public int pendingStreamOperations,snowCacheTargets,snowUniqueTargets,snowCacheRefreshes;
            public double realtimeSeconds;
            public float milliseconds,mainMs,renderMs,physicsMs;
            public long gcBytes;
            public string weatherId,regionId;
            public bool paused,racing,saving,loading,saveAvailable,navigationAvailable;
            public bool hasNavigationTarget,routeReady,guidanceSuppressed,hudMapEnabled,fullMapOpen;
        }
        public void Initialize(TruckTaxiBootstrap owner)
        {
            if(owner==null) throw new ArgumentNullException(nameof(owner));
            DisposeRecorders();
            host=owner;world=owner.GetComponent<TruckTaxiRegionalWorld>();
            streamer=owner.GetComponent<LwsSceneStreamerAdapter>();
            snow=owner.GetComponent<TruckTaxiSnowSurface>();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            main=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Main Thread",1);
            render=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Render Thread",1);
            physics=ProfilerRecorder.StartNew(ProfilerCategory.Physics,"Physics.Simulate",1);
            gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame",1);
            draws=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count",1);
            enabled=true;
#else
            enabled=false;
#endif
            Clear();
        }
        private void Update()
        {
            int currentCollections=GC.CollectionCount(0),delta=Math.Max(0,currentCollections-collections);collections=currentCollections;
            if(host==null || !Application.isFocused) return;
            // Persistence is created after this component; read the owner's cached reference, not GetComponent.
            var persistence=host.Persistence;
            bool loading=persistence?.IsBusy==true || persistence?.SaveService?.IsLoading==true;
            if(!host.Ready && !loading) return;
            float ms=Time.unscaledDeltaTime*1000;
            if(!float.IsFinite(ms) || ms<WarningThreshold) return;
            var sample=Observe(ms,delta);
            if(!Ingest(sample)) return;
            if(logSpikes && Time.unscaledTime>=nextLog)
            { nextLog=Time.unscaledTime+2;Debug.Log("Truck Taxi frame spike: "+Describe(sample),this); }
        }
        private Sample Observe(float ms,int delta)
        {
            var persistence=host.Persistence;
            var gps=host.GPS;
            return new Sample{frame=Time.frameCount,milliseconds=ms,mainMs=Milliseconds(main),renderMs=Milliseconds(render),
                physicsMs=Milliseconds(physics),gcBytes=ReadCounter(gc),gcCollections=delta,
                realtimeSeconds=Time.realtimeSinceStartupAsDouble,drawCalls=(int)ReadCounter(draws),traffic=host.traffic?.ActiveCount ?? 0,
                pedestrians=host.pedestrians?.ActiveCount ?? 0,rivals=host.Rivals?.LiveCount ?? 0,
                regions=world!=null ? world.LoadedCount : -1,events=host.Extremes?.ActiveSnapshots.Count ?? 0,
                paused=host.Paused,racing=host.Races?.IsActive==true,
                loading=persistence?.IsBusy==true || persistence?.SaveService?.IsLoading==true,
                saveAvailable=persistence?.SaveService!=null,saving=persistence?.SaveService?.IsSaving==true,
                weatherId=host.Environment?.CurrentTaxiWeatherId,regionId=world!=null ? world.CurrentRegion : null,
                pendingStreamOperations=streamer!=null ? streamer.PendingOperationCount : -1,
                navigationAvailable=gps!=null,hasNavigationTarget=gps!=null && gps.HasNavigationTarget,
                routeReady=gps!=null && gps.RouteReady,guidanceSuppressed=gps!=null && gps.GuidanceSuppressed,
                hudMapEnabled=gps!=null && gps.HudVisible,fullMapOpen=gps!=null && gps.FullMapOpen,
                snowCacheTargets=snow!=null ? snow.CacheTargetCount : -1,
                snowUniqueTargets=snow!=null ? snow.UniqueCacheTargetCount : -1,
                snowCacheRefreshes=snow!=null ? snow.CacheRefreshCount : -1};
        }
        private float WarningThreshold => float.IsFinite(warningMilliseconds) ? Mathf.Max(1,warningMilliseconds) : 33;
        private float SevereThreshold => Mathf.Max(WarningThreshold,float.IsFinite(severeMilliseconds) ? severeMilliseconds : 50);

        // Main-thread deterministic ingestion; values are observations, not causal attribution.
        public bool Ingest(Sample sample)
        {
            if(!float.IsFinite(sample.milliseconds) || sample.milliseconds<WarningThreshold) return false;
            WarningCount=Math.Min(int.MaxValue-1,WarningCount)+1;
            if(sample.milliseconds>=SevereThreshold) SevereCount=Math.Min(int.MaxValue-1,SevereCount)+1;
            PeakMilliseconds=Mathf.Max(PeakMilliseconds,sample.milliseconds);
            history[next]=sample;next=(next+1)%history.Length;count=Mathf.Min(count+1,history.Length);
            return true;
        }

        // Copies oldest retained first, up to destination capacity. Never exposes internal storage or allocates.
        public int CopyChronological(Sample[] destination,int destinationIndex=0)
        {
            if(destination==null) throw new ArgumentNullException(nameof(destination));
            if(destinationIndex<0 || destinationIndex>destination.Length) throw new ArgumentOutOfRangeException(nameof(destinationIndex));
            int length=Math.Min(count,destination.Length-destinationIndex);
            int start=(next-count+Capacity)%Capacity,first=Math.Min(length,Capacity-start);
            Array.Copy(history,start,destination,destinationIndex,first);
            Array.Copy(history,0,destination,destinationIndex+first,length-first);
            return length;
        }
        public static long ReadCounter(ProfilerRecorder recorder) =>
            recorder.Valid && recorder.Count>0 ? recorder.LastValue : -1;
        private static float Milliseconds(ProfilerRecorder recorder)
        { long value=ReadCounter(recorder);return value<0 ? -1 : value*.000001f; }
        public void Clear()
        {
            count=next=WarningCount=SevereCount=0;PeakMilliseconds=0;
            Array.Clear(history,0,history.Length);
            collections=GC.CollectionCount(0);nextLog=0;
        }
        public string Summary => "FRAMES >= "+WarningThreshold+"ms: "+WarningCount+" / >= "+SevereThreshold+"ms: "+SevereCount+
            "   PEAK "+PeakMilliseconds.ToString("0.0")+"ms\n"+(count==0 ? "NO CAPTURED SPIKES" : Describe(history[(next+history.Length-1)%history.Length]))+
            "\nLast 128 spikes retained. Negative counters mean unavailable. State observed at capture; recorder timing may refer to the preceding frame. Correlation is not a measured cause.";
        private static string Describe(Sample s) => "FRAME "+s.frame+"  "+s.milliseconds.ToString("0.0")+"ms  MAIN "+s.mainMs.ToString("0.0")+
            "  RENDER "+s.renderMs.ToString("0.0")+"  PHYSICS "+s.physicsMs.ToString("0.0")+
            "\nGC BYTES "+s.gcBytes+"  COLLECTIONS "+s.gcCollections+"  DRAWS "+s.drawCalls+
            "\nTRAFFIC "+s.traffic+"  PEOPLE "+s.pedestrians+"  RIVALS "+s.rivals+"  REGIONS "+s.regions+"  EVENTS "+s.events+
            "\nAT "+s.realtimeSeconds.ToString("0.000")+"s  PAUSED "+s.paused+"  RACING "+s.racing+
            "  SAVING "+(s.saveAvailable ? s.saving.ToString() : "UNAVAILABLE")+"  LOADING "+s.loading+
            "\nWEATHER "+(s.weatherId ?? "UNAVAILABLE")+"  REGION "+(s.regionId ?? "UNAVAILABLE")+"  STREAM PENDING "+s.pendingStreamOperations+
            "\nNAV "+(s.navigationAvailable ? "TARGET "+s.hasNavigationTarget+" / READY "+s.routeReady+" / SUPPRESSED "+s.guidanceSuppressed+
                " / HUD ENABLED "+s.hudMapEnabled+" / FULL MAP "+s.fullMapOpen : "UNAVAILABLE")+
            "\nSNOW GROUND CACHE "+s.snowCacheTargets+" / UNIQUE "+s.snowUniqueTargets+" / SCANS "+s.snowCacheRefreshes;
        private void DisposeRecorders() { main.Dispose();render.Dispose();physics.Dispose();gc.Dispose();draws.Dispose(); }
        private void OnDestroy() => DisposeRecorders();
    }
}
