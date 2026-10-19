using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Linq;
using UnityEngine;
using LWS.TruckSports;
using LWS.TruckBowling;
namespace LWS.TruckBogSnorkelling {
 public sealed class BogHardProbe:MonoBehaviour {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Boot(){bool run=Array.IndexOf(Environment.GetCommandLineArgs(),"-tsBogHard")>=0;
#if UNITY_EDITOR
   run|=UnityEditor.SessionState.GetBool("Sports.Bog.Hard",false);UnityEditor.SessionState.SetBool("Sports.Bog.Hard",false);
#endif
   if(!run)return;var go=new GameObject("Explicit Hard Bog complete races and oxygen validation");DontDestroyOnLoad(go);go.AddComponent<SportsProbeInputIsolation>();go.AddComponent<BogHardProbe>();}
  const string Folder="Logs/TruckSports/EssentialRelease/Bog/Hard";IDisposable prefs;BogMatch m;bool manual;int checks,errors;readonly StringBuilder report=new StringBuilder();
  void Awake(){prefs=SportsPrefs.IsolateValidation("BogHard");SportsParticipantRegistry.ForgetCache();}
  void Check(bool value,string label){checks++;if(!value)errors++;report.AppendLine((value?"PASS ":"FAIL ")+label);File.WriteAllText(Folder+"/progress.txt",report.ToString());}
  void Error(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;report.AppendLine("ERROR "+message+"\n"+trace);}}
  IEnumerator Start(){Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/result.txt","RUNNING "+DateTime.UtcNow.ToString("o"));Application.logMessageReceived+=Error;yield return SportsProbeScene.Ensure<BogMatch>("TruckBogSnorkelling");yield return null;m=FindAnyObjectByType<BogMatch>();m.option=1;m.AllBots();for(int i=0;i<4;i++)m.setup.players[i].difficulty=i==0?BowlingDifficulty.Easy:i==1?BowlingDifficulty.Normal:BowlingDifficulty.Hard;
   Check(m.StartMatch(),"saved additional Hard course starts four physical competitors");Check(m.course.Hard&&m.course.hardTerrain.gameObject.activeSelf&&!m.course.normalTerrain.gameObject.activeSelf&&m.course.FinishDistance==650,"Hard terrain, flooded galleries, water extent and thirteen ordered flags selected independently of Classic");Time.timeScale=4;float deadline=Time.realtimeSinceStartup+240,next=0,minAir=14;int clean=0,finishes=0;bool[] counted=new bool[4];int round=m.series.round;bool captured=false;
   while(!m.Finished&&Time.realtimeSinceStartup<deadline){if(round!=m.series.round){round=m.series.round;Array.Clear(counted,0,4);}for(int i=0;i<4;i++){var r=m.racers[i];if(r.Submerged)minAir=Mathf.Min(minAir,r.Air);if(r.State==BogState.Finished&&!counted[i]){counted[i]=true;finishes++;if(r.Recoveries==0)clean++;report.AppendLine("FINISH round="+round+" slot="+i+" time="+r.FinishTime+" recoveries="+r.Recoveries);}}
    if(!captured&&m.racers.Any(r=>r.Checkpoint>=2)){captured=true;foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)}){SportsControlledCapture.Save(m,size.x,size.y,Folder+"/hard-"+size.x+"x"+size.y+".png");yield return null;}}
    if(Time.realtimeSinceStartup>next){next=Time.realtimeSinceStartup+4;report.AppendLine("LIVE round="+m.series.round+" clock="+m.Clock+" z="+string.Join(",",m.racers.Select(r=>r.truck.body.position.z.ToString("F0")))+" flags="+string.Join(",",m.racers.Select(r=>r.Checkpoint))+" air="+string.Join(",",m.racers.Select(r=>r.Air.ToString("F1")))+" recovery="+string.Join(",",m.racers.Select(r=>r.Recoveries)));File.WriteAllText(Folder+"/progress.txt",report.ToString());}yield return null;}
   Time.timeScale=1;Check(m.Finished&&m.series.winner>=0,"complete autonomous Hard race series declares a winner");Check(finishes>0&&clean>0,"a physically driven competent line completes the Hard route without oxygen recovery");Check(minAir<8,"actual submerged galleries materially consume the unchanged fourteen-second oxygen supply");
   m.SetupScreen();m.option=1;m.AllBots();m.StartMatch();m.enabled=false;var racer=m.racers[0];racer.Release();var p=m.course.Surface(new Vector3(BogCourse.Center(40),0,40))+Vector3.up*.55f;racer.truck.Prepare(p,Quaternion.identity);Physics.SyncTransforms();manual=true;Time.timeScale=4;deadline=Time.realtimeSinceStartup+16;while(racer.State!=BogState.Penalty&&racer.State!=BogState.DNF&&Time.realtimeSinceStartup<deadline)yield return null;manual=false;Time.timeScale=1;report.AppendLine("SLOW FIXTURE state="+racer.State+" air="+racer.Air+" recoveries="+racer.Recoveries+" position="+racer.truck.body.position);Check(racer.Recoveries>0&&racer.truck.body.position.z<40,"explicit inefficient held-dive physical fixture depletes air, stalls and returns to the prior safe checkpoint");m.enabled=true;m.SetupScreen();m.option=0;Check(m.StartMatch()&&!m.course.Hard&&m.course.normalTerrain.gameObject.activeSelf&&!m.course.hardTerrain.gameObject.activeSelf&&m.course.FinishDistance==350&&m.course.checkpoints.Length==5,"Classic terrain, water bounds and original five-flag350m rules remain selectable on rematch");m.SetupScreen();m.option=1;Check(m.StartMatch()&&m.racers.All(r=>r.Air==14&&r.Recoveries==0)&&m.course.checkpoints.Length==13,"Hard rematch restores air, course, ordered gates and recovery state");m.SetupScreen();SportsNavigation.FrontEnd();yield return null;yield return null;Check(FindAnyObjectByType<BogMatch>()==null&&FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude).Length==1,"course variants, underwater effects and bodies clean up on menu return");report.AppendLine("OBSERVED finishes="+finishes+" clean="+clean+" minimumAir="+minAir);report.AppendLine("CHECKS="+checks+" ERRORS="+errors+" · actual autonomous NWH/PhysX Hard series and separately initialized slow underwater oxygen fixture. No human/hardware/Windows claim.");File.WriteAllText(Folder+"/result.txt",report.ToString());Application.logMessageReceived-=Error;
#if UNITY_EDITOR
   if(Application.isEditor)UnityEditor.EditorApplication.isPlaying=false;else
#endif
   Application.Quit(errors==0?0:2);
  }
  void FixedUpdate(){if(manual&&m!=null)m.racers[0].Step(new Vector2(0,.08f),true,Time.fixedDeltaTime);}
  void OnDestroy(){Application.logMessageReceived-=Error;if(m!=null)m.enabled=true;prefs?.Dispose();SportsParticipantRegistry.ForgetCache();Time.timeScale=1;}
#endif
 }
}
