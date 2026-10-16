using System;
using System.IO;
using System.Text;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using LWS.TruckSports;
using LWS.TruckBowling;
namespace LWS.TruckKeepaway {
// Diagnostic intentionally compares a fixed-coroutine pause with the actual Update input path.
public sealed class KeepawayPauseProbe:MonoBehaviour {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
 [RuntimeInitializeOnLoadMethod]static void Boot(){bool run=Array.IndexOf(Environment.GetCommandLineArgs(),"-tsKeepPause")>=0;
#if UNITY_EDITOR
 run|=UnityEditor.SessionState.GetBool("LWS.KeepPause",false);UnityEditor.SessionState.SetBool("LWS.KeepPause",false);
#endif
 if(run){var g=new GameObject("Owned Keepaway pause timing diagnostic");DontDestroyOnLoad(g);g.AddComponent<SportsProbeInputIsolation>();g.AddComponent<KeepawayPauseProbe>();}}
 KeepawayMatch match;Keyboard keyboard;InputSettings.BackgroundBehavior background;readonly StringBuilder log=new StringBuilder();string folder;int errors;bool pauseInUpdate,pausedFromUpdate;Vector3 before,velocity;float score;int pauseFrame;float pauseTime;bool priorPaused;
 void Sample(){var b=match.trucks[3].body;before=b.position;velocity=b.linearVelocity;score=match.possession[3];pauseFrame=Time.frameCount;pauseTime=Time.time;priorPaused=match.Paused;match.Pause();}
 void Update(){if(pauseInUpdate){pauseInUpdate=false;Sample();pausedFromUpdate=true;}}
 IEnumerator Start(){folder=SportsEvidencePaths.Folder("KeepawayPauseEvidence");background=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;keyboard=InputSystem.AddDevice<Keyboard>();InputSystem.EnableDevice(keyboard);
  for(int mode=0;mode<2;mode++)for(int view=0;view<2;view++)for(int humans=0;humans<3;humans++){
   if(match!=null)match.SetupScreen();SportsNavigation.LoadSport(view==0?SportsNavigation.Keepaway:SportsNavigation.KeepawayCab);yield return null;yield return null;match=FindAnyObjectByType<KeepawayMatch>();match.setup.count=4;match.option=1;match.chaosLevel=1;match.weatherChoice=1;for(int i=0;i<4;i++){match.setup.players[i].source=i<humans?BowlingSource.Keyboard:BowlingSource.AI;match.setup.players[i].pad=null;match.setup.players[i].difficulty=(BowlingDifficulty)(i%3);}match.StartMatch();Time.timeScale=4;float end=Time.time+18;while(Time.time<end&&!match.Finished)yield return new WaitForFixedUpdate();
   if(mode==0)Sample();else{pausedFromUpdate=false;pauseInUpdate=true;while(!pausedFromUpdate)yield return null;}
   yield return new WaitForSecondsRealtime(.12f);var after=match.trucks[3].body.position;var afterVelocity=match.trucks[3].body.linearVelocity;bool same=before==after&&score==match.possession[3];if(!same&&mode==1)errors++;log.AppendLine((same?"PASS ":mode==0?"FIXED_PHASE_DIFFERENCE ":"FAIL ")+"mode="+mode+" view="+view+" humans="+humans+" before="+before.ToString("F9")+" after="+after.ToString("F9")+" delta="+(after-before).ToString("F9")+" velocity="+velocity.ToString("F9")+" afterVelocity="+afterVelocity.ToString("F9")+" score="+score.ToString("R")+" afterScore="+match.possession[3].ToString("R")+" oldPaused="+priorPaused+" paused="+match.Paused+" startFrame="+pauseFrame+" endFrame="+Time.frameCount+" time="+pauseTime.ToString("R")+" afterTime="+Time.time.ToString("R"));File.WriteAllText(folder+"/progress.txt",log.ToString());match.SetupScreen();Time.timeScale=1;
  }
  log.AppendLine("ERRORS="+errors+". Controlled fixed-coroutine versus real Update pause timing; no hardware claim.");File.WriteAllText(folder+"/result.txt",log.ToString());Restore();if(!Application.isEditor)Application.Quit(errors==0?0:2);
 }
 void Restore(){if(match!=null)match.SetupScreen();if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);keyboard=null;InputSystem.settings.backgroundBehavior=background;Time.timeScale=1;}
 void OnDestroy(){Restore();}
#endif
}
}
