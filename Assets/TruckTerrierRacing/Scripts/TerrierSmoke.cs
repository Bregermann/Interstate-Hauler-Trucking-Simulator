using System;
using System.IO;
using System.Text;
using System.Collections;
using UnityEngine;
using LWS.TruckBowling;
namespace LWS.TruckTerrierRacing {
public sealed class TerrierSmoke:MonoBehaviour {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Launch(){bool run=Array.IndexOf(Environment.GetCommandLineArgs(),"-tsTerrier")>=0;
#if UNITY_EDITOR
 run|=UnityEditor.SessionState.GetBool("LWS.TerrierNativeProbe",false);UnityEditor.SessionState.SetBool("LWS.TerrierNativeProbe",false);
#endif
 if(run){var g=new GameObject("Terrier native validation");DontDestroyOnLoad(g);g.AddComponent<TerrierSmoke>();}}
 TerrierMatch m;string saved,folder;bool existed,restored;int errors,oldLocks,lastHeat,replayLocks;float lastWrite,firstFinishAt=-1;bool windowChecked;readonly StringBuilder report=new StringBuilder(12000);readonly System.Collections.Generic.List<float> frames=new System.Collections.Generic.List<float>(8000);
 void Check(bool good,string label){report.AppendLine((good?"PASS ":"FAIL ")+label);if(!good)errors++;}
 void Log(string s,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;report.AppendLine("ERROR "+s);}}
 IEnumerator Start(){m=FindAnyObjectByType<TerrierMatch>();if(m==null){LWS.TruckSports.SportsNavigation.LoadSport(LWS.TruckSports.SportsNavigation.Terrier);yield return null;m=FindAnyObjectByType<TerrierMatch>();}folder=LWS.TruckSports.SportsEvidencePaths.Folder("TerrierEvidence");Directory.CreateDirectory(folder);existed=PlayerPrefs.HasKey(m.PreferenceKey);saved=PlayerPrefs.GetString(m.PreferenceKey);Application.logMessageReceived+=Log;yield return null;
 for(int humans=1;humans<=4;humans++){m.SetupScreen();for(int i=0;i<12;i++)m.setup.players[i].source=i<humans?BowlingSource.Keyboard:BowlingSource.AI;m.option=0;Check(m.StartMatch(),"software keyboard layout starts "+humans);Check(m.HumanViews==humans&&m.cameras.Length==humans,"human-only viewports "+humans);Screen.SetResolution(humans%2==0?1920:1280,humans%2==0?1080:720,false);yield return new WaitForSecondsRealtime(.25f);if(!Application.isEditor)ScreenCapture.CaptureScreenshot(folder+"/cab-layout-"+humans+".png");yield return null;}
 m.SetupScreen();for(int i=0;i<12;i++){m.setup.players[i].source=BowlingSource.AI;m.setup.players[i].difficulty=(BowlingDifficulty)(i%3);}m.option=0;Check(m.StartMatch(),"twelve-AI autonomous match starts");Check(m.HumanViews==0&&m.cameras.Length==1,"AI uses one broadcast viewport");bool paused=false,replayChecked=false;float end=Time.realtimeSinceStartup+700;int firstReplayWins=-1;
 while(!m.Finished&&Time.realtimeSinceStartup<end){
 if(m.Phase==TerrierPhase.Race&&m.HeatTime>2&&!paused){m.Pause();var p=m.racers[0].truck.body.position;float time=m.HeatTime;yield return new WaitForSecondsRealtime(.2f);Check(m.Paused&&(p-m.racers[0].truck.body.position).sqrMagnitude<.00001f&&m.HeatTime==time,"pause preserves actual race bodies and clock");m.Resume();paused=true;}
 if(m.Heat!=lastHeat){lastHeat=m.Heat;firstFinishAt=-1;report.AppendLine("HEAT "+lastHeat+" seed="+m.course.Seed+" layout="+m.course.LayoutHash+" weather="+m.course.weather.Description);}
 if(m.Phase==TerrierPhase.Race&&m.FinishWindowRemaining>0&&firstFinishAt<0)firstFinishAt=m.HeatTime-(TerrierMatch.FinishWindow-m.FinishWindowRemaining);if(firstFinishAt>=0&&m.Phase==TerrierPhase.Race&&m.HeatTime-firstFinishAt<7.9f)CheckWindowStillRacing();if(m.Locks!=oldLocks){if(firstFinishAt>=0){Check(m.HeatTime-firstFinishAt>=7.95f&&m.HeatTime-firstFinishAt<8.1f,"automatic eight-second finish window heat "+m.Heat);windowChecked=true;}oldLocks=m.Locks;report.AppendLine("LOCK "+oldLocks+" winner="+m.HeatWinner+" time="+m.HeatTime+" wins="+string.Join(",",m.wins));}
 if(m.Phase==TerrierPhase.Replay&&!replayChecked){replayChecked=true;replayLocks=m.Locks;firstReplayWins=0;foreach(int v in m.wins)firstReplayWins+=v;var before=m.replay.TimeCursor;m.ExtraAction("replayangle");m.ExtraAction("replayspeed");m.ExtraAction("replayrewind");Check(m.replay.Playing&&m.replay.Angle==1,"recorded replay angle and rewind active");Check(m.replay.Duration>2,"actual heat poses recorded");m.ExtraAction("replayskip");Check(m.Phase==TerrierPhase.Replay,"early skip does not bypass minimum replay");if(!Application.isEditor)ScreenCapture.CaptureScreenshot(folder+"/recorded-replay.png");}
 if(replayChecked&&m.Locks==replayLocks){int sum=0;foreach(int v in m.wins)sum+=v;if(sum!=firstReplayWins){Check(false,"replay must not change authoritative points");firstReplayWins=sum;}}
 if(m.Heat==1&&m.Phase==TerrierPhase.Race)frames.Add(Time.unscaledDeltaTime*1000);if(m.Heat>1)Time.timeScale=4;
 if(Time.realtimeSinceStartup-lastWrite>2){File.WriteAllText(folder+"/progress.txt",report+"CURRENT phase="+m.Phase+" heat="+m.Heat+" time="+m.HeatTime+" errors="+errors);lastWrite=Time.realtimeSinceStartup;}
 yield return null;
 }
 Check(windowChecked,"finish window observed during physical match");Check(m.Finished&&m.MatchWinner>=0&&m.wins[m.MatchWinner]==5,"complete first-to-five match through actual controls/PhysX");report.AppendLine("RESULT winner="+m.MatchWinner+" heats="+m.Heat+" locks="+m.Locks+" wins="+string.Join(",",m.wins));if(frames.Count>0){frames.Sort();report.AppendLine("First heat real-time frames="+frames.Count+" medianMS="+frames[frames.Count/2]+" p95MS="+frames[(int)((frames.Count-1)*.95f)]+" GPU="+SystemInfo.graphicsDeviceName);}report.AppendLine("ERRORS="+errors+"; software keyboard layout/AI/native physics only; no physical controller or human playtest");File.WriteAllText(folder+"/native-result.txt",report.ToString());Debug.Log("TERRIER_NATIVE_SMOKE_COMPLETE "+report);Restore();if(!Application.isEditor)Application.Quit(errors==0?0:2);}
 void CheckWindowStillRacing(){if(m.replay.Playing)Check(false,"replay must not interrupt the finish window");}
 void Restore(){if(restored)return;restored=true;Application.logMessageReceived-=Log;if(m!=null){m.SetupScreen();if(existed)PlayerPrefs.SetString(m.PreferenceKey,saved);else PlayerPrefs.DeleteKey(m.PreferenceKey);PlayerPrefs.Save();}Time.timeScale=1;}
 void OnDestroy(){Restore();}
#endif
}
}
