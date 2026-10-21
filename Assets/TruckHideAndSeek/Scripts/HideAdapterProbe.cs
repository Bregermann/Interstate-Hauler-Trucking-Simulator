#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using LWS.TruckSports;
namespace LWS.TruckHideAndSeek
{
 // Focused regressions for the three optional shared presentation fields.
 // These are actual existing saved scenes, not copies of their camera logic.
 public sealed class HideAdapterProbe:MonoBehaviour
 {
  const string Folder="Logs/TruckSports/EighteenSports/HideAdapters";readonly StringBuilder report=new StringBuilder();IDisposable preferences;int checks,errors;SportsEventSession current;float deadline;InputSettings.BackgroundBehavior background;
#if UNITY_EDITOR
  InputSettings.EditorInputBehaviorInPlayMode editorInput;
#endif
  [RuntimeInitializeOnLoadMethod]static void Boot(){bool run=Array.IndexOf(Environment.GetCommandLineArgs(),"-tsHideAdapters")>=0;
#if UNITY_EDITOR
   run|=UnityEditor.SessionState.GetBool("Sports.Hide.Adapters",false);UnityEditor.SessionState.SetBool("Sports.Hide.Adapters",false);
#endif
   if(run){var go=new GameObject("Explicit optional Hide presentation regression");DontDestroyOnLoad(go);go.AddComponent<SportsProbeInputIsolation>();go.AddComponent<HideAdapterProbe>();}}
  void Awake(){preferences=SportsPrefs.IsolateValidation("HideAdapters");SportsParticipantRegistry.ForgetCache();background=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
   editorInput=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
  }
  bool Defaults(SportsDrivingViews views,SportsEventTruck[] trucks,SportsMatchAudioAnchor audio){var groups=views.rally.presentationGroups;report.AppendLine("DEFAULTS groups="+(groups==null?"null":groups.Length.ToString())+" audio="+(audio.explicitFocus==null?"null":audio.explicitFocus.name)+" forced="+Array.Exists(trucks,t=>t.tractorMotor.forceSpatialAudio));return (groups==null||groups.Length==0)&&audio.explicitFocus==null&&Array.TrueForAll(trucks,t=>!t.tractorMotor.forceSpatialAudio);}
  void Check(bool okay,string message){checks++;if(!okay)errors++;report.AppendLine((okay?"PASS ":"FAIL ")+message);File.WriteAllText(Folder+"/progress.txt",report.ToString());}
  void Error(string m,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;report.AppendLine("ERROR "+m+"\n"+stack);}}
  void Update(){if(deadline>0&&Time.realtimeSinceStartup>deadline){deadline=0;Check(false,"shared presentation regression watchdog expired");current?.SetupScreen();StopAllCoroutines();Complete();return;}if(current!=null&&current.Running&&!current.Finished&&current.Paused&&current.DevicesReady)current.Resume();}
  IEnumerator Start()
  {
   Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/result.txt","RUNNING");Application.logMessageReceived+=Error;deadline=Time.realtimeSinceStartup+240;
   yield return SportsProbeScene.Ensure<LWS.TruckHungryHaulers.HaulerMatch>("TruckHungryHaulers");yield return null;var hauler=FindAnyObjectByType<LWS.TruckHungryHaulers.HaulerMatch>();current=hauler;Check(Defaults(hauler.drivingViews,hauler.trucks,hauler.GetComponent<SportsMatchAudioAnchor>()),"Haulers retains inactive defaults for all three optional Hide adapters");Directory.CreateDirectory(Folder+"/Hauler");yield return SportsExpansionCameraAcceptance.Run(hauler,hauler.drivingViews,Check,Folder+"/Hauler");hauler.SetupScreen();current=null;
   yield return SportsProbeScene.Ensure<LWS.TruckSheepHerding.HerdMatch>("TruckSheepHerding");yield return null;var herd=FindAnyObjectByType<LWS.TruckSheepHerding.HerdMatch>();current=herd;Check(Defaults(herd.drivingViews,herd.trucks,herd.GetComponent<SportsMatchAudioAnchor>()),"Herding retains inactive defaults for all three optional Hide adapters");Directory.CreateDirectory(Folder+"/Herd");yield return SportsExpansionCameraAcceptance.Run(herd,herd.drivingViews,Check,Folder+"/Herd");herd.SetupScreen();Complete();
  }
  void Complete(){report.AppendLine("CHECKS="+checks+" ERRORS="+errors+"; platform="+Application.platform+"; actual existing saved scenes, shared view allocation and software controllers. Focused presentation regression, not new full gameplay or hardware/listening acceptance.");File.WriteAllText(Folder+"/result.txt",report.ToString());Application.logMessageReceived-=Error;
#if UNITY_EDITOR
   if(Application.isEditor)UnityEditor.EditorApplication.isPlaying=false;else
#endif
   Application.Quit(errors==0?0:2);}
  void OnDestroy(){Application.logMessageReceived-=Error;InputSystem.settings.backgroundBehavior=background;
#if UNITY_EDITOR
   InputSystem.settings.editorInputBehaviorInPlayMode=editorInput;
#endif
   preferences?.Dispose();SportsParticipantRegistry.ForgetCache();Time.timeScale=1;}
 }
}
#endif
