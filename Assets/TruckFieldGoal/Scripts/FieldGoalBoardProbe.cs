#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using LWS.TruckSports;
using LWS.TruckBowling;

namespace LWS.TruckFieldGoal {
public sealed class FieldGoalBoardProbe:MonoBehaviour {
 [RuntimeInitializeOnLoadMethod]static void Boot(){bool run=Array.IndexOf(Environment.GetCommandLineArgs(),"-tsFieldGoalBoard")>=0;
#if UNITY_EDITOR
 run|=UnityEditor.SessionState.GetBool("Sports.FieldGoal.Board",false);UnityEditor.SessionState.SetBool("Sports.FieldGoal.Board",false);
#endif
 if(run){var go=new GameObject("Explicit stadium board acceptance");DontDestroyOnLoad(go);go.AddComponent<SportsProbeInputIsolation>();go.AddComponent<FieldGoalBoardProbe>();}}
 readonly StringBuilder log=new StringBuilder();int checks,errors;string folder;IDisposable prefs;int previousFrameRate;
 void Error(string value,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;log.AppendLine("ERROR "+value+"\n"+stack);}}
 void Check(bool ok,string label){checks++;if(!ok)errors++;log.AppendLine((ok?"PASS ":"FAIL ")+label);File.WriteAllText(folder+"/progress.txt",log.ToString());}
 IEnumerator Start(){previousFrameRate=Application.targetFrameRate;Application.targetFrameRate=60;prefs=SportsPrefs.IsolateValidation("FieldGoalBoard");SportsParticipantRegistry.ForgetCache();Application.logMessageReceived+=Error;folder=Path.GetFullPath(Path.Combine(Application.dataPath,Application.isEditor?"..":"../../..","Logs/TruckSports/FieldGoalBoard",Application.isEditor?"Native":"Windows"));Directory.CreateDirectory(folder);File.WriteAllText(folder+"/result.txt","RUNNING "+DateTime.UtcNow.ToString("o"));
  yield return SportsProbeScene.Ensure<FieldGoalMatch>("TruckFieldGoal");yield return null;var m=FindAnyObjectByType<FieldGoalMatch>();var b=m.stadiumBoard;
  Check(b!=null&&b.screen.renderMode==RenderMode.WorldSpace&&b.screen.transform.position.z>m.posts.position.z,"saved physical scoreboard behind the actual uprights");
  Check(m.GetComponent<FieldGoalPresentation>().sock.IsChildOf(m.posts)&&m.GetComponent<FieldGoalPresentation>().sock.position.y>=19f,"windsock is attached above an upper upright corner and moves with the posts");
  Check(b.screen.GetComponentsInChildren<Collider>(true).Length==0,"screen and prompts cannot interfere with football collisions");
  Check(!m.BroadcastHeaderVisible&&!m.BroadcastStatusVisible&&!m.BroadcastStandingsVisible,"gameplay stats use stadium board without duplicate broadcast strips");
  var art=b.fieldAssembly.Find("Original Party logo · fifty-yard midfield");var end=m.endZone.Find("Original Field Goal full-width end-zone artwork");Check(art!=null&&Mathf.Abs(art.localPosition.z+54.864f)<.001f,"existing Party wordmark is on the actual fifty-yard midfield line");Check(Vector3.Dot(art.TransformDirection(Vector3.up),Vector3.left)>.99f,"midfield wordmark faces the right-hand stadium stands");Check(b.runoff.Length>=1&&b.runoff.All(c=>c!=null&&c.enabled&&!c.isTrigger),"saved green stadium runoff and concourses have solid collision");Check(end!=null&&end.localScale.x>=50&&Mathf.Abs(m.endZone.Find("Ten-yard end-zone paint").localScale.z-9.144f)<.001f,"original full-width Field Goal art occupies a regulation ten-yard end zone");var numbers=b.fieldAssembly.Find("Traditional sideline yard numbers").GetComponentsInChildren<TMPro.TextMeshPro>();Check(numbers.Length==18&&numbers.Count(t=>t.text=="50")==2&&numbers.Count(t=>t.text=="10")==4,"traditional10/20/30/40/50 yard numbers exist on both sidelines");var hashes=b.fieldAssembly.GetComponentsInChildren<Transform>().Where(t=>t.name=="Inlaid single-yard hash"&&t.localPosition.x<0).Select(t=>t.localPosition.z).OrderBy(z=>z).ToArray();Check(hashes.Length==90&&Mathf.Abs(hashes[1]-hashes[0]-.9144f)<.001f,"single-yard hashes use0.9144m spacing with ten-yard stripes between groups");
  m.option=0;m.setup.count=4;m.NormalizeSetup();m.AllBots();for(int i=0;i<4;i++){m.setup.players[i].name="Long driver "+(i+1)+" name";m.setup.players[i].difficulty=BowlingDifficulty.Hard;}
  m.weatherPresets.Select(m.weatherPresets.entries.First(p=>p.displayName=="Clear").id);Check(m.StartMatch(),"four-AI match starts on saved upgraded scene");
  float deadline=Time.realtimeSinceStartup+150;int seen=0;bool captured=false,replayChecked=false;Time.timeScale=5;
  while(!m.Finished&&Time.realtimeSinceStartup<deadline){
   if(m.Phase==FieldGoalPhase.Approach){int round=m.rules.Round;int bit=1<<Mathf.Clamp(round-1,0,15);if((seen&bit)==0){seen|=bit;Check(Mathf.Abs(m.ground.bounds.max.z-m.GoalZ)<.02f&&Mathf.Abs(m.endZone.position.z+4.572f-m.GoalZ)<.02f,"field turf and ten-yard end line terminate at goal plane distance="+m.GoalZ);Check(Mathf.Abs(b.fieldAssembly.position.z-m.posts.position.z)<.01f&&b.screen.transform.position.z>m.GoalZ,"board stays behind uprights across attempt "+round);
     if(round==5){m.Pause();m.menus.Show(SportsEventScreen.Play);yield return new WaitForSecondsRealtime(.8f);b.Refresh();foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)}){if(Application.isEditor)SportsControlledCapture.Save(m,size.x,size.y,folder+"/longest-kick-"+size.x+"x"+size.y+".png");else{Screen.SetResolution(size.x,size.y,false);yield return new WaitForSecondsRealtime(.4f);ScreenCapture.CaptureScreenshot(folder+"/longest-kick-"+size.x+"x"+size.y+".png");yield return new WaitForSecondsRealtime(.2f);}}var corners=new Vector3[4];b.screen.GetComponent<RectTransform>().GetWorldCorners(corners);Check(corners.All(p=>{var v=m.cameraRig.view.WorldToViewportPoint(p);return v.z>0&&v.x>=0&&v.x<=1&&v.y>=0&&v.y<=1;}),"entire physical scoreboard remains in view at longest kick");var low=m.cameraRig.view.WorldToViewportPoint(corners[0]);var high=m.cameraRig.view.WorldToViewportPoint(corners[1]);Check((high.y-low.y)*720>=180,"longest-kick board has at least180pixels of vertical space at720p · height="+((high.y-low.y)*720));m.menus.Show(SportsEventScreen.Pause);m.Resume();Time.timeScale=5;}
    }
    if(!captured){captured=true;Time.timeScale=1;yield return new WaitForSeconds(.2f);m.Pause();m.menus.Show(SportsEventScreen.Play);b.Refresh();yield return null;
     foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(1024,768),new Vector2Int(2560,1080)}){if(Application.isEditor)SportsControlledCapture.Save(m,size.x,size.y,folder+"/approach-"+size.x+"x"+size.y+".png");else{Screen.SetResolution(size.x,size.y,false);yield return new WaitForSecondsRealtime(.4f);ScreenCapture.CaptureScreenshot(folder+"/approach-"+size.x+"x"+size.y+".png");yield return new WaitForSecondsRealtime(.2f);}}
     Check(b.names.All(t=>!t.isTextOverflowing)&&b.scores.All(t=>!t.isTextOverflowing),"all four long names and scores fit their embedded cards · "+string.Join(" | ",b.names.Select(t=>t.text+" size="+t.fontSize+" overflow="+t.isTextOverflowing)));m.menus.Show(SportsEventScreen.Pause);m.Resume();Time.timeScale=5;
    }
   }
   if(m.rules.attempts.Sum()>0){b.Refresh();CheckScoreOnce(m,b);}
   if(m.ReplayControls&&!replayChecked){replayChecked=true;yield return ReplayLayouts(m,b);var goals=(int[])m.rules.goals.Clone();var attempts=(int[])m.rules.attempts.Clone();float speed=m.replay.Speed;int angle=m.replay.Angle;b.buttons[1].onClick.Invoke();Check(m.replay.Speed!=speed,"embedded Heat speed button controls the actual replay");b.buttons[0].onClick.Invoke();Check(m.replay.Angle==(angle+1)%3,"embedded Heat angle button controls the actual replay");float cursor=m.replay.Cursor;b.buttons[2].onClick.Invoke();Check(m.replay.Cursor<=cursor,"embedded rewind button rewinds recorded presentation");b.buttons[3].onClick.Invoke();Check(m.PhaseAge>=19&&goals.SequenceEqual(m.rules.goals)&&attempts.SequenceEqual(m.rules.attempts),"embedded continue button exits replay without scoring twice");}
   yield return null;
  }
  Time.timeScale=1;Check(m.Finished&&m.rules.Round>=5&&m.rules.attempts.All(n=>n>=5),"complete four-AI physical bumper-kick match gives all players five attempts");Check(m.rules.goals.Sum()>0,"actual physical kick and swept legal goal still score");
  b.Refresh();Check(b.status.text=="MATCH COMPLETE"&&b.scores.Select(t=>t.text).SequenceEqual(m.rules.goals.Select(n=>n.ToString())),"final stadium scores agree exactly with authoritative results");
  foreach(var offset in new[]{new Vector3(40,5,10),new Vector3(0,5,40)}){
   m.SetupScreen();Check(m.StartMatch(),"injected missed-kick/runoff collision fixture starts");m.football.body.position=offset+Vector3.forward*m.GoalZ;m.football.body.isKinematic=false;m.football.body.detectCollisions=true;m.football.body.linearVelocity=Vector3.down*30;m.football.Arm();Physics.SyncTransforms();float until=Time.realtimeSinceStartup+3;while(m.football.GroundContacts==0&&Time.realtimeSinceStartup<until)yield return null;yield return new WaitForFixedUpdate();Check(m.football.GroundContacts>0&&m.football.body.position.y>-.5f&&m.rules.AttemptLocked&&m.rules.goals.Sum()==0,"actual football CCD contacts green runoff, resolves one miss and remains above ground · "+offset);
  }
  for(int repeat=0;repeat<2;repeat++){m.SetupScreen();Check(m.StartMatch(),"rematch starts "+repeat);yield return null;Check(FindObjectsByType<FieldGoalStadiumBoard>().Length==1&&Mathf.Abs(m.ground.bounds.max.z-m.GoalZ)<.02f,"rematch retains one correctly aligned board "+repeat);}
  m.Pause();var point=m.football.body.position;yield return new WaitForSecondsRealtime(.2f);Check(m.football.body.position==point,"pause preserves the authoritative football");m.Resume();SportsNavigation.FrontEnd();yield return null;yield return null;Check(FindAnyObjectByType<FieldGoalStadiumBoard>()==null&&FindObjectsByType<AudioListener>().Length==1,"menu exit clears stadium and retains one listener");
  log.AppendLine("CHECKS="+checks+" ERRORS="+errors+" "+DateTime.UtcNow.ToString("o"));log.AppendLine("Actual NWH/PhysX AI match. Editor images are controlled RT captures; Windows images are actual player screenshots. No physical-controller or human playtest claim.");File.WriteAllText(folder+"/result.txt",log.ToString());
#if UNITY_EDITOR
 if(Application.isEditor)UnityEditor.EditorApplication.isPlaying=false;else
#endif
 Application.Quit(errors==0?0:2);
 }
 IEnumerator ReplayLayouts(FieldGoalMatch m,FieldGoalStadiumBoard b){
  Time.timeScale=0;
  float savedSpeed=m.replay.Speed;int savedAngle=m.replay.Angle;m.replay.Speed=0;
  for(int angle=0;angle<3;angle++){
   m.replay.Angle=angle;yield return new WaitForSecondsRealtime(.3f);
   b.PositionReplayTransport(true);Canvas.ForceUpdateCanvases();
   foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(1280,800),new Vector2Int(1024,768),new Vector2Int(2560,1080)}){
    if(Application.isEditor){
     SportsControlledCapture.Save(m,size.x,size.y,folder+"/replay-angle-"+angle+"-"+size.x+"x"+size.y+".png",camera=>Check(TransportVisible(b,camera,size.x,size.y),"all four Heat replay actions stay fully inside "+size+" at recorded angle "+angle));
    }else{
     Screen.SetResolution(size.x,size.y,false);yield return new WaitForSecondsRealtime(.4f);b.PositionReplayTransport(true);Canvas.ForceUpdateCanvases();
     Check(TransportVisible(b,null,Screen.width,Screen.height),"all four Heat replay actions stay fully inside actual player "+size+" at recorded angle "+angle);
     ScreenCapture.CaptureScreenshot(Path.GetFullPath(folder+"/replay-angle-"+angle+"-"+size.x+"x"+size.y+".png"));yield return new WaitForSecondsRealtime(.15f);
    }
   }
  }
  m.replay.Angle=savedAngle;m.replay.Speed=savedSpeed;Time.timeScale=5;
 }
 static bool TransportVisible(FieldGoalStadiumBoard b,Camera camera,int width,int height){
  var corners=new Vector3[4];
  for(int i=0;i<4;i++){
   var rect=b.buttons[i].GetComponent<RectTransform>();if(rect.parent!=b.match.menus.hud.transform||!rect.gameObject.activeInHierarchy)return false;
   rect.GetWorldCorners(corners);foreach(var corner in corners){var pixel=RectTransformUtility.WorldToScreenPoint(camera,corner);if(pixel.x<8||pixel.x>width-8||pixel.y<8||pixel.y>height-8)return false;}
  }
  return true;
 }

 bool checkedScores;
 void CheckScoreOnce(FieldGoalMatch m,FieldGoalStadiumBoard b){if(checkedScores)return;checkedScores=true;Check(b.scores.Select(t=>t.text).SequenceEqual(m.rules.goals.Select(n=>n.ToString())),"live board reads actual per-slot goal totals");}
 void OnDestroy(){Application.targetFrameRate=previousFrameRate;Application.logMessageReceived-=Error;prefs?.Dispose();SportsParticipantRegistry.ForgetCache();Time.timeScale=1;}
}
}
#endif
