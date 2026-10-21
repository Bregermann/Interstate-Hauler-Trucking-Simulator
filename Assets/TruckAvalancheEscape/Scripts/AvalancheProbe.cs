#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using LWS.TruckSports;
using LWS.TruckBowling;
namespace LWS.TruckAvalancheEscape
{
 public sealed class AvalancheProbe:MonoBehaviour
 {
  const string Folder="Logs/TruckSports/EighteenSports/Avalanche";readonly StringBuilder report=new StringBuilder();IDisposable preferences;AvalancheMatch match;int checks,errors;float deadline;Gamepad[] pads;Keyboard stationaryKeyboard;InputSettings.BackgroundBehavior background;bool intentionalPause;
#if UNITY_EDITOR
 InputSettings.EditorInputBehaviorInPlayMode editorInput;
#endif
  [RuntimeInitializeOnLoadMethod]static void Boot(){bool run=Array.IndexOf(Environment.GetCommandLineArgs(),"-tsAvalanche")>=0;
#if UNITY_EDITOR
   run|=UnityEditor.SessionState.GetBool("Sports.Avalanche.Probe",false);UnityEditor.SessionState.SetBool("Sports.Avalanche.Probe",false);
#endif
   if(run){var g=new GameObject("Explicit physical avalanche acceptance");DontDestroyOnLoad(g);g.AddComponent<SportsProbeInputIsolation>();g.AddComponent<AvalancheProbe>();}}
  void Check(bool yes,string message){checks++;if(!yes)errors++;report.AppendLine((yes?"PASS ":"FAIL ")+message);File.WriteAllText(Folder+"/progress.txt",report.ToString());}
  void Error(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;report.AppendLine("ERROR "+message+"\n"+stack);}}
  void Update(){if(deadline>0&&Time.realtimeSinceStartup>deadline){deadline=0;Check(false,"bounded avalanche runtime probe deadline");StopAllCoroutines();match?.SetupScreen();Complete();}if(match!=null&&match.Paused&&match.DevicesReady&&!intentionalPause){match.Resume();Time.timeScale=1;}}
  IEnumerator Start()
  {
   Directory.CreateDirectory(Folder);preferences=SportsPrefs.IsolateValidation("Avalanche");background=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
 editorInput=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
Application.logMessageReceived+=Error;deadline=Time.realtimeSinceStartup+650;yield return SportsProbeScene.Ensure<AvalancheMatch>("TruckAvalancheEscape");yield return null;match=FindAnyObjectByType<AvalancheMatch>();var m=match;Check(m!=null&&m.trucks.Length==12&&m.world.routes.Length>=3&&m.world.terrain!=null,"saved twelve-truck Arctic terrain and three physical road routes");if(m==null){Complete();yield break;}
   m.atmosphere.presets.Select(Array.Find(m.atmosphere.presets.entries,p=>p.legacyKind==SportsWeatherKind.Clear).id);m.setup.count=12;m.option=0;m.AllBots();for(int i=0;i<12;i++)m.setup.players[i].difficulty=BowlingDifficulty.Hard;Check(m.StartMatch(),"complete twelve-AI race starts through setup");Time.timeScale=1;float until=Time.realtimeSinceStartup+360,next=0;bool captured=false;
   bool startsChecked=false,progressChecked=false;
   while(!m.Finished&&Time.realtimeSinceStartup<until){if(!startsChecked&&m.rules.Phase==AvalanchePhase.Countdown&&m.rules.Countdown<.6f){startsChecked=true;bool settled=true;foreach(var truck in m.trucks)settled&=truck.transform.up.y>.95f&&Array.TrueForAll(truck.wheels,w=>w.IsGrounded)&&truck.body.linearVelocity.magnitude<.3f;Check(settled,"all twelve actual trucks settle upright with grounded wheels before GO");}if(!progressChecked&&m.rules.Clock>10){progressChecked=true;bool forward=true;for(int i=0;i<12;i++)forward&=m.trucks[i].body.position.z>m.world.starts[i].z+5;Check(forward,"all twelve AI physically leave their starting positions in the downhill direction");}if(Time.realtimeSinceStartup>next){next=Time.realtimeSinceStartup+5;report.AppendLine("PHYSICAL_RACE time="+m.rules.Clock+" front="+m.front.CurrentZ+" contacts="+m.PhysicalSnowContacts+" burials="+m.ActualBurials+" escapes="+m.ActualEscapes);for(int i=0;i<12;i++)report.AppendLine(" truck="+i+" position="+m.trucks[i].body.position+" distance="+m.rules.distance[i]+" finish="+m.rules.finished[i]+" alive="+m.rules.alive[i]);File.WriteAllText(Folder+"/progress.txt",report.ToString());}if(!captured&&m.rules.Clock>55){captured=true;yield return SportsWindowCapture.Save(1280,720,Folder+"/mountain-720.png");yield return SportsWindowCapture.Save(1920,1080,Folder+"/mountain-1080.png");}yield return null;}
   Time.timeScale=1;Check(m.Finished,"full physical mountain race resolves every participant");int finishers=0;float furthest=0;for(int i=0;i<12;i++){if(m.rules.finished[i])finishers++;furthest=Mathf.Max(furthest,m.rules.distance[i]);}Check(finishers>0,"actual AI follows a complete route and escapes the mountain");int[] routeFinishes=new int[3];for(int i=0;i<12;i++)if(m.rules.finished[i])routeFinishes[i%3]++;report.AppendLine("REAL_ROUTE_FINISHERS broad="+routeFinishes[0]+" cliff="+routeFinishes[1]+" river="+routeFinishes[2]);Check(Array.TrueForAll(routeFinishes,n=>n>0),"real AI completes the broad road, cliff jump and frozen-river route");Check(m.front.CurrentZ>500&&furthest>1000,"avalanche genuinely pursues racing trucks down the mountain");Check(m.PhysicalSnowContacts>0||finishers==m.setup.count,"competing trucks either physically encounter snow or all genuinely escape it");yield return SportsWindowCapture.Save(1280,720,Folder+"/results-720.png");string resultBefore=m.Results;m.ExtraAction("watchavalanche");yield return new WaitForSecondsRealtime(.2f);Check(m.review.Playing,"actual mountain race and snow/debris highlight replay starts");yield return SportsWindowCapture.Save(1280,720,Folder+"/replay-720.png");m.ExtraAction("replayskip");Check(m.Results==resultBefore,"replay preserves finish times, burial and survival rankings");m.SetupScreen();Check(m.StartMatch()&&m.PhysicalSnowContacts==0&&m.ActualBurials==0&&m.rules.Clock==0,"rematch restores trucks, snow exposure, rolling debris and avalanche origin");m.SetupScreen();
   stationaryKeyboard=InputSystem.AddDevice<Keyboard>("Neutral physical avalanche burial driver");InputSystem.QueueStateEvent(stationaryKeyboard,new KeyboardState());m.setup.count=1;m.AllBots();m.setup.players[0].source=BowlingSource.Keyboard;Check(m.StartMatch(),"stationary real-truck snow contact fixture starts through setup");while(m.rules.Phase==AvalanchePhase.Countdown)yield return null;Check(!m.Status.Contains("BURIED")&&!m.Status.Contains("ESCAPED"),"new match clears prior burial and finishing announcements");until=Time.realtimeSinceStartup+60;while(m.PhysicalSnowContacts==0&&!m.Finished&&Time.realtimeSinceStartup<until)yield return null;Check(m.PhysicalSnowContacts>0&&m.ActualBurials==0,"initial actual snow contact leaves a real escape grace period");while(!m.Finished&&Time.realtimeSinceStartup<until)yield return null;Check(m.Finished&&m.ActualBurials==1&&!m.rules.finished[0],"sustained physical snow contact buries the truck exactly once without awarding a finish");m.SetupScreen();
   pads=new Gamepad[4];for(int i=0;i<4;i++)pads[i]=InputSystem.AddDevice<Gamepad>("Avalanche software owner "+i);
   for(int humans=1;humans<=4;humans++){m.setup.count=12;m.AllBots();for(int i=0;i<humans;i++){m.setup.players[i].source=BowlingSource.WholeController;m.setup.players[i].pad=pads[i];InputSystem.QueueStateEvent(pads[i],new GamepadState());}SportsCameraOptions.Set(false);Check(m.StartMatch(),humans+" human twelve-truck match starts");m.drivingViews.Select(2);yield return new WaitForSeconds(.2f);Check(m.cameras.Length==humans,"AI cameras OFF gives every viewport to a human");bool ownership=true;for(int i=0;i<humans;i++)ownership&=m.drivingViews.rally.owners[i]==i&&m.cameras[i].enabled;Check(ownership,"cab cameras preserve participant ownership");if(humans>1)Check(m.MultiViewHUD&&m.menus.viewTexts[0].text.Contains(m.setup.players[0].name),"split-screen HUD uses the correct participant data");else Check(m.cameras[0].rect==new Rect(0,0,1,1),"one human receives the entire screen");InputSystem.QueueStateEvent(pads[0],new GamepadState{rightTrigger=.8f});yield return new WaitForSeconds(.12f);bool independent=m.commands[0].y>.5f;for(int i=1;i<humans;i++)independent&=m.commands[i]==Vector2.zero;Check(independent,"human controller inputs remain isolated");InputSystem.QueueStateEvent(pads[0],new GamepadState());SportsCameraOptions.Set(true);m.drivingViews.Layout();yield return new WaitForSeconds(.2f);Check(m.cameras.Length==4,"AI cameras ON uses the documented four-viewport maximum");if(humans==4){yield return SportsWindowCapture.Save(1280,720,Folder+"/four-cabs-720.png");yield return SportsWindowCapture.Save(1920,1080,Folder+"/four-cabs-1080.png");intentionalPause=true;float before=m.rules.Clock;m.Pause();yield return new WaitForSecondsRealtime(.15f);Check(m.rules.Clock==before,"pause freezes the shared race clock");m.Resume();intentionalPause=false;}m.SetupScreen();}
   SportsCameraOptions.Set(false);Complete();
  }
  void Complete(){report.AppendLine("CHECKS="+checks+" ERRORS="+errors+"; platform="+Application.platform+"; actual NWH physics and moving snow contacts. No hardware or human-playtest certification.");File.WriteAllText(Folder+"/result.txt",report.ToString());Application.logMessageReceived-=Error;
#if UNITY_EDITOR
   if(Application.isEditor)UnityEditor.EditorApplication.isPlaying=false;else
#endif
   Application.Quit(errors==0?0:2);}
  void OnDestroy(){Application.logMessageReceived-=Error;if(stationaryKeyboard!=null&&stationaryKeyboard.added)InputSystem.RemoveDevice(stationaryKeyboard);if(pads!=null)foreach(var pad in pads)if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);InputSystem.settings.backgroundBehavior=background;
#if UNITY_EDITOR
 InputSystem.settings.editorInputBehaviorInPlayMode=editorInput;
#endif
 preferences?.Dispose();SportsParticipantRegistry.ForgetCache();Time.timeScale=1;}
 }
}
#endif
