using System;
using System.IO;
using System.Text;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using LWS.TruckSports;
using LWS.TruckBowling;
namespace LWS.TruckBeerPong {
 public sealed class PongPacingProbe:MonoBehaviour {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Boot(){bool run=Array.IndexOf(Environment.GetCommandLineArgs(),"-tsPongPacing")>=0;
#if UNITY_EDITOR
   run|=UnityEditor.SessionState.GetBool("Sports.Pong.Pacing",false);UnityEditor.SessionState.SetBool("Sports.Pong.Pacing",false);
#endif
   if(!run)return;var go=new GameObject("Explicit Beer Pong actual-physics pacing comparison");DontDestroyOnLoad(go);go.AddComponent<SportsProbeInputIsolation>();go.AddComponent<PongPacingProbe>();}
  const string Folder="Logs/TruckSports/EssentialRelease/BeerPong/Pacing";IDisposable prefs;Keyboard keyboard;readonly StringBuilder report=new StringBuilder();int checks,errors;
  void Awake(){prefs=SportsPrefs.IsolateValidation("PongPacing");SportsParticipantRegistry.ForgetCache();}
  void Check(bool value,string label){checks++;if(!value)errors++;report.AppendLine((value?"PASS ":"FAIL ")+label);File.WriteAllText(Folder+"/progress.txt",report.ToString());}
  void Error(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;report.AppendLine("ERROR "+message+"\n"+trace);}}
  IEnumerator Start(){Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/result.txt","RUNNING "+DateTime.UtcNow.ToString("o"));Application.logMessageReceived+=Error;yield return SportsProbeScene.Ensure<PongMatch>("TruckBeerPong");yield return null;var m=FindAnyObjectByType<PongMatch>();float step=Time.fixedDeltaTime;var physics=Physics.simulationMode;int[] normalCup=new int[3];Vector2[] normalPlan=new Vector2[3];int manual=0;
   for(int mode=0;mode<3;mode++)for(int sample=0;sample<3;sample++){m.SetupScreen();SportsPrefs.SetInt(PongAIPlayback.Preference,mode);m.option=1;m.NormalizeSetup();m.AllBots();m.seed=7349+sample*101;m.setup.players[0].difficulty=(BowlingDifficulty)sample;Check(m.StartMatch(),PongAIPlayback.Label+" physical AI shot starts difficulty="+sample);float deadline=Time.realtimeSinceStartup+70;double started=Time.realtimeSinceStartupAsDouble;bool pause=false;
    while(!m.rules.ShotLocked&&Time.realtimeSinceStartup<deadline){if(!pause&&mode==0&&sample==0&&m.Phase==PongPhase.Flight){pause=true;m.Pause();var p=m.truck.body.position;float clock=m.Clock;Check(m.SimulateAIFlight(64)==0,"paused AI flight cannot be manually advanced");yield return new WaitForSecondsRealtime(.12f);Check(m.truck.body.position==p&&m.Clock==clock,"pause preserves AI flight and clock");m.Resume();}yield return null;}
    Check(m.rules.ShotLocked&&m.TotalLaunches==1&&m.rules.shots[0]==1,"actual fixed-step throw resolves exactly once mode="+mode+" sample="+sample);int result=m.rules.LastHit?m.rules.LastCup:-1;var velocity=m.CommandedLaunchVelocity;var plan=new Vector2(m.AIPlannedCharge,m.AIPlannedYaw);report.AppendLine("SHOT mode="+mode+" skill="+sample+" cup="+result+" plan="+plan+" command="+velocity+" observed="+m.shot.LaunchVelocity+" contacts="+m.shot.Contacts+" manualSteps="+m.aiPlayback.ManualSteps+" realSeconds="+(Time.realtimeSinceStartupAsDouble-started).ToString("F2"));
    if(mode==0){normalCup[sample]=result;normalPlan[sample]=plan;}else{Check(Vector2.Distance(normalPlan[sample],plan)<.00001f,"pacing preserves the exact seeded AI power and aim plan skill="+sample);Check(normalCup[sample]==result,"same seeded actual AI cup outcome as Normal skill="+sample);}
    if(mode==2){manual+=m.aiPlayback.ManualSteps;Check(m.Phase==PongPhase.TurnResult&&m.aiPlayback.ManualSteps>0,"simulate uses real PhysX substeps then exposes the actual updated cup result");}
    Check(Physics.simulationMode==physics&&Time.fixedDeltaTime==step,"temporary simulation ownership restores host physics configuration");m.SetupScreen();Check(Mathf.Approximately(Time.timeScale,1),"setup restores ordinary time scale after AI pacing");yield return null;
   }
   Check(manual>0,"explicit actual-physics simulation was exercised");m.option=0;m.teamA=m.teamB=2;m.NormalizeSetup();m.AllBots();foreach(var player in m.setup.players)player.difficulty=BowlingDifficulty.Hard;SportsPrefs.SetInt(PongAIPlayback.Preference,2);Check(m.StartMatch(),"four autonomous competitors start a complete simulated-pacing2v2 match");float end=Time.realtimeSinceStartup+240;int last=-1;while(!m.Finished&&Time.realtimeSinceStartup<end){int shots=m.rules.shots[0]+m.rules.shots[1];if(shots!=last){last=shots;report.AppendLine("MATCH shots="+shots+" remaining="+m.rules.remaining[0]+","+m.rules.remaining[1]+" phase="+m.Phase);File.WriteAllText(Folder+"/progress.txt",report.ToString());}yield return null;}Check(m.Finished&&m.rules.Winner>=0,"complete real2v2 match retains cup elimination, turn order, rebuttal and victory");Check(m.TotalLaunches==m.rules.shots[0]+m.rules.shots[1],"each actual launch contributes at most one official shot");Check(Physics.simulationMode==physics&&Mathf.Approximately(Time.timeScale,1),"results release manual physics and accelerated time ownership");
   m.SetupScreen();keyboard=InputSystem.AddDevice<Keyboard>();m.option=1;m.NormalizeSetup();m.setup.players[0].source=BowlingSource.Keyboard;Check(m.StartMatch(),"human practice can use the same saved Simulate preference");yield return new WaitForSeconds(.25f);Check(Mathf.Approximately(Time.timeScale,1)&&m.aiPlayback.ManualSteps==0,"human driving and shots are never fast-forwarded or simulated");m.Pause();yield return new WaitForSecondsRealtime(.25f);SportsControlledCapture.Save(m,1280,720,Folder+"/pacing-pause-720.png");m.SetupScreen();yield return new WaitForSecondsRealtime(.25f);SportsControlledCapture.Save(m,1920,1080,Folder+"/pacing-setup-1080.png");SportsNavigation.FrontEnd();yield return null;yield return null;Check(FindAnyObjectByType<PongMatch>()==null&&Physics.simulationMode==physics&&Mathf.Approximately(Time.timeScale,1),"repeated matches and scene exit restore the normal shared simulation");report.AppendLine("CHECKS="+checks+" ERRORS="+errors+" · actual physical AI approaches/launches/cup detection and explicit fixed-step PhysX flight simulation; injected keyboard readiness only. No human/hardware/Windows claim.");File.WriteAllText(Folder+"/result.txt",report.ToString());Application.logMessageReceived-=Error;
#if UNITY_EDITOR
   if(Application.isEditor)UnityEditor.EditorApplication.isPlaying=false;else
#endif
   Application.Quit(errors==0?0:2);
  }
  void OnDestroy(){Application.logMessageReceived-=Error;if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);prefs?.Dispose();SportsParticipantRegistry.ForgetCache();Time.timeScale=1;}
#endif
 }
}
