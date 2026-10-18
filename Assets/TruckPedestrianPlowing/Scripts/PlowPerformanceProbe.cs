#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using DigitalRuby.WeatherMaker;
using LWS.TruckSports;
namespace LWS.TruckPedestrianPlowing {
 public sealed partial class PlowValidationProbe {
  IEnumerator PerformanceCases(string mode){
   bool previousAIViews=SportsCameraOptions.ShowAI;m.SetupScreen();m.AllBots();SportsCameraOptions.Set(true);m.option=m.cabExperience?2:1;UnityEngine.Random.InitState(m.crowdSettings.randomSeed);
   Check(m.StartMatch()&&m.cameras.Length==4&&m.AllAI,"performance starts eight competitors and four actual independent AI views");
   var restore=PerformanceVariant(mode);log.AppendLine("PERFORMANCE_VARIANT "+mode);
   var counters=new PerformanceCounters(log);Time.timeScale=1;int liveFrames=0,focusedFrames=0,rematches=0,peakRagdolls=0;float nextCount=0,end=Time.realtimeSinceStartup+60;var times=new List<float>();
   try{
    while(Time.realtimeSinceStartup<end){
     if(m.Finished){restore();m.SetupScreen();Check(m.StartMatch()&&m.cameras.Length==4,"performance round completed and rematch restored four views");restore=PerformanceVariant(mode);rematches++;}
     times.Add(Time.unscaledDeltaTime*1000);counters.Sample();if(m.Live&&m.menus.Screen==SportsEventScreen.Play)liveFrames++;if(Application.isFocused)focusedFrames++;
     if(Time.realtimeSinceStartup>=nextCount){nextCount=Time.realtimeSinceStartup+.5f;int active=0;foreach(var ped in m.Crowd)if(ped.Claimed&&!ped.Resting)active++;peakRagdolls=Mathf.Max(peakRagdolls,active);}
     yield return null;
    }
   }finally{counters.Dispose();restore();SportsCameraOptions.Set(previousAIViews);}
   times.Sort();float sum=0;foreach(var t in times)sum+=t;
   log.AppendLine("PERFORMANCE 60 real seconds / four AI views / eight competitors / "+m.Crowd.Length+" crowd / frames="+times.Count+" meanMs="+sum/times.Count+" p95Ms="+times[(int)(times.Count*.95f)]+" p99Ms="+times[(int)(times.Count*.99f)]);
   log.AppendLine("PERFORMANCE_LIFECYCLE liveFrames="+liveFrames+" focusedFrames="+focusedFrames+" total="+times.Count+" actualViews="+m.cameras.Length+" rematches="+rematches+" peakActiveRagdolls="+peakRagdolls+" physicalHits="+m.PhysicalHits);
   Check(times.Count>100&&liveFrames>=times.Count*.95f&&m.cameras.Length==4,"sustained live rendered four-view performance sample");
  }
  Action PerformanceVariant(string mode){
   var restore=new List<Action>();var night=m.GetComponent<PlowNight>();
   if(mode.EndsWith("-nosky")){bool enabled=night.weather.clouds.enabled;night.weather.clouds.enabled=false;restore.Add(()=>night.weather.clouds.enabled=enabled);}
   if(mode.EndsWith("-nofeature"))foreach(var feature in Resources.FindObjectsOfTypeAll<WeatherMakerURPRenderFeatureScript>()){bool active=feature.isActive;feature.SetActive(false);restore.Add(()=>feature.SetActive(active));}
   if(mode.EndsWith("-nolights")){var lights=m.transform.Find("Festival night fill lights").gameObject;bool active=lights.activeSelf;lights.SetActive(false);restore.Add(()=>lights.SetActive(active));}
   if(mode.EndsWith("-notint"))foreach(var ped in m.Crowd)foreach(var renderer in ped.GetComponent<PlowRoleCostume>().clothing){var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);renderer.SetPropertyBlock(null);restore.Add(()=>renderer.SetPropertyBlock(block));}
   if(mode.EndsWith("-nocostumes")||mode.EndsWith("-noshadows"))foreach(var ped in m.Crowd){
    var wardrobe=ped.GetComponent<PlowRoleCostume>();var roots=new[]{wardrobe.standard,wardrobe.bonus,wardrobe.penalty,wardrobe.pitchfork}.Concat(wardrobe.bonusAttachments).Concat(wardrobe.penaltyAttachments).Distinct();
    foreach(var root in roots){if(mode.EndsWith("-nocostumes")){bool active=root.activeSelf;root.SetActive(false);restore.Add(()=>root.SetActive(active));}else foreach(var renderer in root.GetComponentsInChildren<Renderer>(true)){var shadows=renderer.shadowCastingMode;renderer.shadowCastingMode=ShadowCastingMode.Off;restore.Add(()=>renderer.shadowCastingMode=shadows);}}
   }
   return ()=>{foreach(var action in restore)action();};
  }
  sealed class PerformanceCounters:IDisposable {
   sealed class Counter {public string name,unit;public ProfilerRecorder recorder;public double sum,max;public int samples;}
   readonly List<Counter> counters=new List<Counter>();readonly System.Text.StringBuilder log;
   public PerformanceCounters(System.Text.StringBuilder output){
    log=output;var available=new List<ProfilerRecorderHandle>();ProfilerRecorderHandle.GetAvailable(available);
    string[] wanted={"Main Thread","Render Thread","GPU Frame Time","Gfx.WaitForPresentOnGfxThread","Physics.Simulate","Triangles Count","SetPass Calls Count","Draw Calls Count","GC Allocated In Frame"};
    foreach(string name in wanted){bool found=false;foreach(var handle in available){var description=ProfilerRecorderHandle.GetDescription(handle);if(description.Name!=name)continue;
      var recorder=ProfilerRecorder.StartNew(description.Category,name,1,ProfilerRecorderOptions.StartImmediately|ProfilerRecorderOptions.WrapAroundWhenCapacityReached|ProfilerRecorderOptions.SumAllSamplesInFrame);
      if(recorder.Valid){counters.Add(new Counter{name=name,unit=description.UnitType.ToString(),recorder=recorder});found=true;}else recorder.Dispose();break;
     }if(!found)log.AppendLine("PROFILE_UNAVAILABLE "+name);
    }
   }
   public void Sample(){foreach(var counter in counters){if(counter.recorder.Count==0)continue;long value=counter.recorder.LastValue;counter.sum+=value;counter.max=Math.Max(counter.max,value);counter.samples++;}}
   public void Dispose(){foreach(var counter in counters){double scale=counter.unit=="TimeNanoseconds"?1e-6:1;log.AppendLine("PROFILE "+counter.name+" mean="+(counter.sum/Math.Max(1,counter.samples)*scale).ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+" max="+(counter.max*scale).ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+" unit="+(scale==1?counter.unit:"ms")+" samples="+counter.samples);counter.recorder.Dispose();}}
  }
 }
}
#endif
