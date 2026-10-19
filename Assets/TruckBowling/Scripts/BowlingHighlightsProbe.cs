using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using LWS.TruckSports;
namespace LWS.TruckBowling {
 public sealed class BowlingHighlightsProbe:MonoBehaviour {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Boot(){bool run=Array.IndexOf(Environment.GetCommandLineArgs(),"-tsBowlingHighlights")>=0;
#if UNITY_EDITOR
   run|=UnityEditor.SessionState.GetBool("Sports.Bowling.Highlights",false);UnityEditor.SessionState.SetBool("Sports.Bowling.Highlights",false);
#endif
   if(!run)return;var go=new GameObject("Explicit Bowling actual-shot highlight validation");DontDestroyOnLoad(go);go.AddComponent<SportsProbeInputIsolation>();go.AddComponent<BowlingHighlightsProbe>();}
  const string Folder="Logs/TruckSports/EssentialRelease/Bowling/Highlights";IDisposable prefs;Keyboard keyboard;readonly StringBuilder report=new StringBuilder();int checks,errors;
  void Awake(){prefs=SportsPrefs.IsolateValidation("BowlingHighlights");SportsParticipantRegistry.ForgetCache();}
  void Check(bool value,string label){checks++;if(!value)errors++;report.AppendLine((value?"PASS ":"FAIL ")+label);File.WriteAllText(Folder+"/progress.txt",report.ToString());}
  void Error(string msg,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;report.AppendLine("ERROR "+msg+"\n"+trace);}}
  IEnumerator Start(){Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/result.txt","RUNNING "+DateTime.UtcNow.ToString("o"));Application.logMessageReceived+=Error;yield return SportsProbeScene.Ensure<BowlingMatch>("TruckBowling");yield return null;var m=FindAnyObjectByType<BowlingMatch>();SportsPrefs.SetInt(BowlingHighlights.Preference,2);m.PresetAI(2,BowlingMode.Simultaneous);SportsCameraOptions.Set(true);Check(m.StartMatch(),"two real independent AI lanes start with every-shot replay enabled");Time.timeScale=4;float deadline=Time.realtimeSinceStartup+90;var lane=m.lanes[0];while(lane.Phase!=BowlingPhase.Replay&&Time.realtimeSinceStartup<deadline)yield return null;Time.timeScale=1;Check(lane.Phase==BowlingPhase.Replay&&lane.highlights.Replays>0,"actual physical approach, impact/settling and score lock enter recorded replay");
   if(lane.highlights.Playing){int deliveries=lane.Owner.score.Deliveries;var poses=lane.pins.Select(p=>p.body.position).ToArray();int otherLocks=m.lanes[1].ScoreLocks;float otherElapsed=m.lanes[1].Elapsed;var h=lane.highlights;Check(h.replay.ghosts.All(g=>g.GetComponentsInChildren<Collider>(true).Length==0&&g.GetComponentsInChildren<Rigidbody>(true).Length==0),"replay geometry has no authoritative physics or scripts");
    for(int angle=0;angle<3;angle++){h.replay.Angle=angle;h.replay.Speed=angle==0?.35f:angle==1?1:2;h.replay.Seek(Mathf.Max(0,h.replay.Duration-3));yield return new WaitForSeconds(.15f);Check(lane.cab.transform.position.y>1&&lane.cab.transform.position.y<8,"recorded camera angle "+angle+" stays above the floor and below the ceiling");SportsControlledCapture.Save(m.menus.GetComponent<Canvas>(),m.lanes.Select(l=>l.cab.view).ToArray(),1280,720,Folder+"/replay-angle-"+angle+".png");yield return null;}
    Check(lane.Owner.score.Deliveries==deliveries&&lane.pins.Select((p,i)=>p.body.position==poses[i]).All(x=>x),"replay changes neither sealed scoring nor live pin poses");Check(m.lanes[1].ScoreLocks>otherLocks||m.lanes[1].Elapsed>otherElapsed||m.lanes[1].Phase!=BowlingPhase.Idle,"the other physical lane remains independently active");
    keyboard=InputSystem.AddDevice<Keyboard>();lane.Owner.source=BowlingSource.Keyboard;InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return new WaitForSeconds(.1f);InputSystem.QueueStateEvent(keyboard,new KeyboardState(lane.Owner.forward,lane.Owner.back));yield return new WaitForSeconds(.1f);Check(!h.Playing,"fresh owning keyboard skip exits replay without a second score event");InputSystem.QueueStateEvent(keyboard,new KeyboardState());lane.Owner.source=BowlingSource.AI;Check(lane.Owner.score.Deliveries==deliveries,"skip cannot record the same delivery twice");
    deadline=Time.realtimeSinceStartup+20;Time.timeScale=4;while(lane.Phase!=BowlingPhase.Approach&&Time.realtimeSinceStartup<deadline)yield return null;Time.timeScale=1;Check(lane.Phase==BowlingPhase.Approach&&!lane.cab.ShotSpectator&&!h.Playing,"pinsetter completes and next delivery restores the driving camera");
   }
   // Explicit visual-only event fixtures; no score or outcome is injected.
   foreach(string kind in new[]{"STRIKE","SPARE","GUTTER"}){lane.highlights.Complete(kind);yield return new WaitForSeconds(.4f);Canvas.ForceUpdateCanvases();foreach(var mascot in lane.highlights.mascots){var mesh=mascot.canvasRenderer.GetMesh();report.AppendLine("MASCOT mesh="+(mesh!=null?mesh.vertexCount:0)+" color="+mascot.color+" renderer="+mascot.canvasRenderer.GetColor()+" culled="+mascot.canvasRenderer.cull+" canvas="+mascot.canvas.name+" rect="+mascot.rectTransform.rect+" p="+mascot.transform.position);Check(mesh!=null&&mesh.vertexCount>100,"pin mascot geometry rendered "+kind);}Check(lane.highlights.group.alpha>.9f&&lane.highlights.title.text.Length>0,"original animated pin-card fixture "+kind);foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)}){SportsControlledCapture.Save(m.menus.GetComponent<Canvas>(),m.lanes.Select(l=>l.cab.view).ToArray(),size.x,size.y,Folder+"/celebration-"+kind+"-"+size.x+".png");yield return null;}}
   m.Pause();float scale=lane.highlights.mascots[0].rectTransform.eulerAngles.z;yield return new WaitForSecondsRealtime(.15f);Check(Mathf.Abs(Mathf.DeltaAngle(scale,lane.highlights.mascots[0].rectTransform.eulerAngles.z))<.01f,"pause also preserves presentation animation state");m.Resume();m.ChangeSetup();Check(m.StartMatch()&&lane.Owner.score.Deliveries==0&&!lane.highlights.Playing,"rematch clears replay, previous results and score");m.ChangeSetup();SportsNavigation.FrontEnd();yield return null;yield return null;Check(FindAnyObjectByType<BowlingMatch>()==null&&FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude).Length==1,"menu cleanup removes per-lane recordings and leaves one listener");report.AppendLine("CHECKS="+checks+" ERRORS="+errors+" · actual AI/PhysX delivery and independent replay; separate STRIKE/SPARE/GUTTER visual event fixtures. Injected keyboard, no physical hardware/human/Windows claim.");File.WriteAllText(Folder+"/result.txt",report.ToString());Application.logMessageReceived-=Error;
#if UNITY_EDITOR
   if(Application.isEditor)UnityEditor.EditorApplication.isPlaying=false;else
#endif
   Application.Quit(errors==0?0:2);
  }
  void OnDestroy(){Application.logMessageReceived-=Error;if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);prefs?.Dispose();SportsParticipantRegistry.ForgetCache();Time.timeScale=1;}
#endif
 }
}
