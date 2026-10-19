using System;
using System.IO;
using System.Text;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using LWS.TruckSports;
using LWS.TruckBowling;
namespace LWS.TruckFareFight {
 public sealed class FareFightDeviceProbe:MonoBehaviour {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Boot(){bool run=Array.IndexOf(Environment.GetCommandLineArgs(),"-tsFareDevices")>=0;
#if UNITY_EDITOR
   run|=UnityEditor.SessionState.GetBool("Sports.FareDevices.Probe",false);UnityEditor.SessionState.SetBool("Sports.FareDevices.Probe",false);
#endif
   if(!run)return;var root=new GameObject("Explicit Fare Fight injected-device acceptance");DontDestroyOnLoad(root);root.AddComponent<SportsProbeInputIsolation>();root.AddComponent<FareFightDeviceProbe>();}
  IDisposable prefs;Keyboard keys;Gamepad first,shared;int checks,errors;readonly StringBuilder report=new StringBuilder();const string Folder="Logs/TruckSports/EssentialRelease/FareFight/Native/Devices";
  void Awake(){prefs=SportsPrefs.IsolateValidation("FareDevices");SportsParticipantRegistry.ForgetCache();}
  void Check(bool value,string message){checks++;if(!value)errors++;report.AppendLine((value?"PASS ":"FAIL ")+message);File.WriteAllText(Folder+"/progress.txt",report.ToString());}
  void Error(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;report.AppendLine("ERROR "+message);}}
  IEnumerator Start(){Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/result.txt","RUNNING "+DateTime.UtcNow.ToString("o"));Application.logMessageReceived+=Error;yield return SportsProbeScene.Ensure<FareFightMatch>("TruckFareFight");yield return null;var m=FindAnyObjectByType<FareFightMatch>();
   keys=InputSystem.AddDevice<Keyboard>();first=InputSystem.AddDevice<Gamepad>();shared=InputSystem.AddDevice<Gamepad>();m.setup.count=4;
   m.setup.players[0].source=BowlingSource.Keyboard;m.setup.players[1].source=BowlingSource.WholeController;m.setup.players[1].pad=first;m.setup.players[2].source=BowlingSource.LeftStick;m.setup.players[2].pad=shared;m.setup.players[3].source=BowlingSource.RightStick;m.setup.players[3].pad=shared;
   foreach(var preset in m.presets.entries)if(preset.displayName=="Clear"){m.presets.Select(preset.id);break;}
   Check(m.ValidateSources(out _),"exclusive keyboard, whole pad and two independent shared-stick sources accepted");m.setup.players[3].source=BowlingSource.WholeController;Check(!m.ValidateSources(out _),"whole controller cannot overlap its assigned left stick");m.setup.players[3].source=BowlingSource.RightStick;
   SportsCameraOptions.Set(false);m.drivingViews.Select(2);Check(m.StartMatch(),"four injected human sources start a simultaneous match");Time.timeScale=3;while(m.Intro>0)yield return null;Time.timeScale=1;yield return new WaitForSeconds(.3f);
   Check(m.cameras.Length==4,"four human camera viewports remain when AI views are disabled");for(int i=0;i<4;i++){Check(m.drivingViews.rally.owners[i]==i&&m.drivingViews.cabs[i].gameObject.activeInHierarchy,"correct actual Taxi cab owner "+i);Check(m.navigation.gps[i].follow==m.trucks[i].transform&&m.navigation.gps[i].cameraMain==m.cameras[i],"independent CNP route/camera binding "+i);}
   var start=new Vector3[4];for(int i=0;i<4;i++)start[i]=m.trucks[i].body.position;
   InputSystem.QueueStateEvent(keys,new KeyboardState(Key.W));InputSystem.QueueStateEvent(first,new GamepadState{rightTrigger=1,leftStick=new Vector2(.12f,0)});InputSystem.QueueStateEvent(shared,new GamepadState{leftStick=Vector2.up,rightStick=new Vector2(-.08f,1)});yield return new WaitForSeconds(5);
   for(int i=0;i<4;i++)Check(Vector3.Distance(start[i],m.trucks[i].body.position)>2,"source "+i+" actually drives its own full NWH tractor");
   InputSystem.QueueStateEvent(keys,new KeyboardState());InputSystem.QueueStateEvent(first,new GamepadState());InputSystem.QueueStateEvent(shared,new GamepadState());yield return new WaitForSeconds(.3f);
   m.drivingViews.CycleParticipant(1);Check(m.drivingViews.ViewMode(1)==1&&m.drivingViews.ViewMode(0)==2&&m.drivingViews.ViewMode(2)==2&&m.drivingViews.ViewMode(3)==2,"one participant changes camera without replacing the other three views");
   foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)}){SportsControlledCapture.Save(m,size.x,size.y,Folder+"/four-human-"+size.x+"x"+size.y+".png");yield return null;}
   InputSystem.DisableDevice(first);yield return null;Check(m.Paused&&Time.timeScale==0,"disconnect of one whole pad pauses the shared simulation");float clock=m.rules.Clock;var pose=m.trucks[2].body.position;yield return new WaitForSecondsRealtime(.25f);Check(m.rules.Clock==clock&&m.trucks[2].body.position==pose,"disconnect preserves clock, passenger, attempt and physical bodies");InputSystem.EnableDevice(first);InputSystem.QueueStateEvent(first,new GamepadState{rightTrigger=1});yield return null;m.Resume();yield return new WaitForSeconds(.25f);Check(!m.Paused&&m.commands[1]==Vector2.zero,"held reconnect input cannot leak through confirmation");InputSystem.QueueStateEvent(first,new GamepadState());yield return new WaitForSeconds(.15f);InputSystem.QueueStateEvent(first,new GamepadState{rightTrigger=1});yield return new WaitForSeconds(.2f);Check(m.commands[1].y>.5f,"neutral then fresh controller driving is restored");InputSystem.QueueStateEvent(first,new GamepadState());
   m.SetupScreen();Check(m.StartMatch()&&m.setup.players[2].pad==shared&&m.setup.players[3].pad==shared,"rematch preserves the shared-stick assignment and independent crew slots");yield return null;m.SetupScreen();SportsNavigation.FrontEnd();yield return null;yield return null;Check(FindAnyObjectByType<FareFightMatch>()==null,"scene exit removes all participant drivers, GPS and action handlers");report.AppendLine("CHECKS="+checks+" ERRORS="+errors+" · injected Input System devices and real NWH simulation; no physical controllers or human playtesting.");File.WriteAllText(Folder+"/result.txt",report.ToString());Application.logMessageReceived-=Error;
#if UNITY_EDITOR
   if(Application.isEditor)UnityEditor.EditorApplication.isPlaying=false;else
#endif
   Application.Quit(errors==0?0:2);
  }
  void OnDestroy(){Application.logMessageReceived-=Error;if(keys!=null&&keys.added)InputSystem.RemoveDevice(keys);if(first!=null&&first.added)InputSystem.RemoveDevice(first);if(shared!=null&&shared.added)InputSystem.RemoveDevice(shared);prefs?.Dispose();SportsParticipantRegistry.ForgetCache();Time.timeScale=1;}
#endif
 }
}
