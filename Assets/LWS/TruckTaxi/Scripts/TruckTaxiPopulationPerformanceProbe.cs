#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Profiling;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public static class TruckTaxiPopulationPerformanceProbe
    {
        [Serializable] private sealed class Report
        {
            public bool baseline;
            public int pedestrians,traffic,pedestrianTarget,trafficTarget,frames;
            public float meanFrameMilliseconds,p95FrameMilliseconds,meanMainThreadMilliseconds,meanPhysicsMilliseconds,framesPerSecond;
            public bool mainThreadRecorderValid,physicsRecorderValid;
            public int rejectedPedestrianSpawns,rejectedTrafficSpawns;
            public int trackedTraffic,movingTraffic;
            public float maximumTrafficDisplacementMeters;
        }
        public static IEnumerator Run(TruckTaxiBootstrap host,Action<bool,string> check,Action<string> capture)
        {
            float oldFrequency=host.Configuration.rideFrequency;
            host.Configuration.rideFrequency=10000;
            host.Session.EndShift(); host.StartShift();
            float deadline=Time.realtimeSinceStartup+40;
            while((host.pedestrians.ActiveCount<host.pedestrians.TargetCount || host.traffic.ActiveCount<host.traffic.TargetCount) && Time.realtimeSinceStartup<deadline)
                yield return null;
            yield return new WaitForSecondsRealtime(3);
            bool baseline=host.pedestrians.IsBaselineValidation;
            capture?.Invoke(baseline ? "Population_Baseline" : "Population_Dense");
            var data=new Report {baseline=baseline,pedestrians=host.pedestrians.ActiveCount,traffic=host.traffic.ActiveCount,
                pedestrianTarget=host.pedestrians.TargetCount,trafficTarget=host.traffic.TargetCount};
            var frameTimes=new List<float>(); double main=0,physics=0;
            var trafficStart=new Dictionary<Transform,Vector3>();
            foreach(var vehicle in host.traffic.Vehicles)
                if(vehicle!=null) trafficStart[vehicle.transform]=vehicle.transform.position;
            using(var mainRecorder=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Main Thread",1))
            using(var physicsRecorder=ProfilerRecorder.StartNew(ProfilerCategory.Physics,"Physics.Simulate",1))
            {
                data.mainThreadRecorderValid=mainRecorder.Valid; data.physicsRecorderValid=physicsRecorder.Valid;
                deadline=Time.realtimeSinceStartup+20;
                while(Time.realtimeSinceStartup<deadline)
                {
                    yield return null;
                    frameTimes.Add(Time.unscaledDeltaTime*1000);
                    if(mainRecorder.Valid) main+=mainRecorder.LastValue*1e-6;
                    if(physicsRecorder.Valid) physics+=physicsRecorder.LastValue*1e-6;
                }
            }
            data.frames=frameTimes.Count;
            float total=0; foreach(float t in frameTimes) total+=t;
            data.meanFrameMilliseconds=total/Mathf.Max(1,data.frames); data.framesPerSecond=1000/Mathf.Max(.001f,data.meanFrameMilliseconds);
            frameTimes.Sort(); data.p95FrameMilliseconds=frameTimes.Count>0 ? frameTimes[Mathf.Min(frameTimes.Count-1,Mathf.FloorToInt(frameTimes.Count*.95f))] : 0;
            data.meanMainThreadMilliseconds=(float)(main/Math.Max(1,data.frames)); data.meanPhysicsMilliseconds=(float)(physics/Math.Max(1,data.frames));
            data.rejectedPedestrianSpawns=host.pedestrians.RejectedSpawnAttempts; data.rejectedTrafficSpawns=host.traffic.RejectedSpawnAttempts;
            foreach(var pair in trafficStart)
            {
                if(pair.Key==null) continue;
                data.trackedTraffic++;
                float distance=Vector3.Distance(pair.Value,pair.Key.position);
                data.maximumTrafficDisplacementMeters=Mathf.Max(data.maximumTrafficDisplacementMeters,distance);
                if(distance>2) data.movingTraffic++;
            }
            string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../Validation")); Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output,baseline ? "Population_Baseline.json" : "Population_Dense.json"),JsonUtility.ToJson(data,true));
            Debug.Log("TAXI POPULATION PERFORMANCE "+JsonUtility.ToJson(data));
            check(data.pedestrians==data.pedestrianTarget,"Pedestrians reached requested target: "+data.pedestrians+"/"+data.pedestrianTarget);
            check(data.traffic==data.trafficTarget,"Traffic reached requested target: "+data.traffic+"/"+data.trafficTarget);
            check(data.movingTraffic>0,"Existing UTS traffic moved over the sample: "+data.movingTraffic+"/"+data.trackedTraffic+" tracked cars moved >2m");
            host.Configuration.rideFrequency=oldFrequency;
        }
    }
}
#endif
