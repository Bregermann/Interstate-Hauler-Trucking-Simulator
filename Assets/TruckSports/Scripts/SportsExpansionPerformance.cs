#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Unity.Profiling;
namespace LWS.TruckSports
{
 // Explicit acceptance instrumentation over Unity's own profiler/timing APIs.
 // It has no simulation, input, weather or camera authority.
 public sealed class SportsExpansionPerformance:MonoBehaviour
 {
  [Serializable]public sealed class Receipt{public string platform,scene,scope="Normal-speed live gameplay after five-second warmup; snapshot pauses excluded. Raw development-build telemetry, not human performance certification. Zero counter samples means unavailable, not zero cost.";public int samples,cpuSamples,physicsSamples,gpuSamples;public double frameMeanMs,frameP95Ms,cpuMeanMs,cpuP95Ms,physicsMeanMs,physicsP95Ms,gpuMeanMs,gpuP95Ms;public long peakAllocatedBytes,peakManagedBytes,peakDriverGraphicsBytes;}
  readonly List<double> frames=new List<double>(12000),cpu=new List<double>(12000),physics=new List<double>(12000),gpu=new List<double>(12000);ProfilerRecorder mainThread,simulation;readonly FrameTiming[] timings=new FrameTiming[1];SportsEventSession match;string folder;long allocated,managed,graphics;float nextMemory;bool closed;
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Boot(){var args=Environment.GetCommandLineArgs();string mode=Array.IndexOf(args,"-tsFrog")>=0?"Frog":Array.IndexOf(args,"-tsAvalanche")>=0?"Avalanche":Array.IndexOf(args,"-tsNinja")>=0?"Ninja":null;if(mode==null)return;var go=new GameObject("Explicit Unity expansion performance evidence");DontDestroyOnLoad(go);go.AddComponent<SportsExpansionPerformance>().folder="Logs/TruckSports/EighteenSports/"+mode;}
  void Start(){try{mainThread=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Main Thread",1);}catch{}try{simulation=ProfilerRecorder.StartNew(ProfilerCategory.Physics,"Physics.Simulate",1);}catch{}}
  void Update()
  {
   if(match==null)match=FindAnyObjectByType<SportsEventSession>();if(match==null||!match.Live||match.RaceStartSeconds<5||Time.timeScale<.99f||Time.timeScale>1.01f||frames.Count>=12000)return;
   if(Time.unscaledDeltaTime>0)frames.Add(Time.unscaledDeltaTime*1000d);if(mainThread.Valid&&mainThread.LastValue>0)cpu.Add(mainThread.LastValue/1000000d);if(simulation.Valid&&simulation.LastValue>0)physics.Add(simulation.LastValue/1000000d);
   FrameTimingManager.CaptureFrameTimings();if(FrameTimingManager.GetLatestTimings(1,timings)>0&&timings[0].gpuFrameTime>0)gpu.Add(timings[0].gpuFrameTime);
   if(Time.unscaledTime>=nextMemory){nextMemory=Time.unscaledTime+1;allocated=Math.Max(allocated,UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong());managed=Math.Max(managed,UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong());graphics=Math.Max(graphics,UnityEngine.Profiling.Profiler.GetAllocatedMemoryForGraphicsDriver());}
  }
  static double Mean(List<double> values){double total=0;foreach(var v in values)total+=v;return values.Count==0?0:total/values.Count;}static double P95(List<double> values){if(values.Count==0)return 0;var copy=values.ToArray();Array.Sort(copy);return copy[Math.Min(copy.Length-1,(int)Math.Ceiling(copy.Length*.95)-1)];}
  void Save(){if(closed||string.IsNullOrEmpty(folder))return;closed=true;Directory.CreateDirectory(folder);var receipt=new Receipt{platform=Application.platform.ToString(),scene=match==null?"unknown":match.gameObject.scene.name,samples=frames.Count,cpuSamples=cpu.Count,physicsSamples=physics.Count,gpuSamples=gpu.Count,frameMeanMs=Mean(frames),frameP95Ms=P95(frames),cpuMeanMs=Mean(cpu),cpuP95Ms=P95(cpu),physicsMeanMs=Mean(physics),physicsP95Ms=P95(physics),gpuMeanMs=Mean(gpu),gpuP95Ms=P95(gpu),peakAllocatedBytes=allocated,peakManagedBytes=managed,peakDriverGraphicsBytes=graphics};File.WriteAllText(folder+"/performance.json",JsonUtility.ToJson(receipt,true));mainThread.Dispose();simulation.Dispose();}
  void OnApplicationQuit(){Save();}void OnDestroy(){Save();}
 }
}
#endif
