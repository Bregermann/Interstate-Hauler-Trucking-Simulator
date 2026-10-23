#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;using System.Collections;using System.IO;using System.Text;
using UnityEngine;using UnityEngine.InputSystem;
using LWS.TruckSports;using LWS.TruckBowling;
namespace LWS.TruckMagnetMayhem
{
 public sealed class MagnetProbe:MonoBehaviour
 {
  const string Folder="Logs/TruckSports/EighteenSports/Magnet";
  IDisposable preferences;int checks,errors;readonly StringBuilder report=new StringBuilder();InputSettings.BackgroundBehavior background;Gamepad fixturePad;MagnetMatch watched;bool intentionalPause;float nextHeartbeat,deadline,requestedScale=1;
  void Update()
  {
   if(deadline>0&&Time.realtimeSinceStartup>deadline){deadline=0;Check(false,"acceptance watchdog expired instead of hanging a scaled wait");watched?.SetupScreen();StopAllCoroutines();Complete();return;}
   if(watched==null||!watched.Running||watched.Finished||intentionalPause)return;
   if(Time.realtimeSinceStartup>=nextHeartbeat){nextHeartbeat=Time.realtimeSinceStartup+15;report.AppendLine("HARNESS_STATE focused="+Application.isFocused+" paused="+watched.Paused+" ready="+watched.DevicesReady+" scale="+Time.timeScale+" clock="+watched.rules.Clock);File.WriteAllText(Folder+"/progress.txt",report.ToString());}
   // The explicit software-input fixture acknowledges external focus pauses
   // through the normal Resume path. Production focus/reconnect policy stays active.
   if(watched.Paused&&watched.DevicesReady){report.AppendLine("HARNESS_ACKNOWLEDGED_EXTERNAL_PAUSE focused="+Application.isFocused);watched.Resume();Time.timeScale=requestedScale;}
  }
#if UNITY_EDITOR
  InputSettings.EditorInputBehaviorInPlayMode editorInput;
#endif
  [RuntimeInitializeOnLoadMethod]static void Boot()
  {
   bool run=Array.IndexOf(Environment.GetCommandLineArgs(),"-tsMagnet")>=0;
#if UNITY_EDITOR
   run|=UnityEditor.SessionState.GetBool("Sports.Magnet.Probe",false);UnityEditor.SessionState.SetBool("Sports.Magnet.Probe",false);
#endif
   if(!run)return;var go=new GameObject("Explicit physical scrapyard acceptance");DontDestroyOnLoad(go);go.AddComponent<SportsProbeInputIsolation>();go.AddComponent<MagnetProbe>();
  }
  void Awake(){preferences=SportsPrefs.IsolateValidation("Magnet");SportsParticipantRegistry.ForgetCache();background=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
   editorInput=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
  }
  void Check(bool ok,string message){checks++;if(!ok)errors++;report.AppendLine((ok?"PASS ":"FAIL ")+message);File.WriteAllText(Folder+"/progress.txt",report.ToString());}
  void Error(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;report.AppendLine("ERROR "+message+"\n"+stack);}}
  IEnumerator Start()
  {
   deadline=Time.realtimeSinceStartup+360;Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/result.txt","RUNNING");Application.logMessageReceived+=Error;
   yield return SportsProbeScene.Ensure<MagnetMatch>("TruckMagnetMayhem");yield return null;var m=FindAnyObjectByType<MagnetMatch>();watched=m;
   Check(m!=null&&m.trucks.Length==8&&m.scrap.Length==40&&m.bays.Length==8,"saved scrapyard has eight NWH trucks and forty actual owned scrap models with matching colliders");if(m==null){Complete();yield break;}
   var clear=Array.Find(m.atmosphere.presets.entries,p=>p.legacyKind==SportsWeatherKind.Clear);if(clear!=null)m.atmosphere.presets.Select(clear.id);
   SportsCameraOptions.Set(false);m.drivingViews.Select(0);m.setup.count=8;m.option=1;m.AllBots();for(int i=0;i<8;i++)m.setup.players[i].difficulty=(BowlingDifficulty)(i%3);Check(m.StartMatch(),"eight AI competitors begin a normal three-minute physical scrapyard match");
   var start=Array.ConvertAll(m.trucks,t=>t.body.position);yield return new WaitForSeconds(1.5f);bool stable=m.fields.ForceApplications==0&&!m.rules.Live;
   for(int i=0;i<8;i++)stable&=m.trucks[i].tractorMotor.Grounded&&Vector3.Dot(m.trucks[i].transform.up,Vector3.up)>.95f&&Vector3.ProjectOnPlane(m.trucks[i].body.position-start[i],Vector3.up).magnitude<.35f;
   Check(stable,"all eight settle grounded with fields and sorting hazards disarmed during countdown");
   requestedScale=Time.timeScale=4;float matchDeadline=Time.realtimeSinceStartup+180,nextTrace=0;bool scoreCaptured=false,paused=false,captured=false;
   while(!m.Finished&&Time.realtimeSinceStartup<matchDeadline)
   {
                if(!scoreCaptured&&m.ScoreJournal.Count>0){scoreCaptured=true;yield return SportsScoreAcceptance.CaptureAward(m,Check,Folder);if(SportsScoreAcceptance.CaptureOnly){SportsScoreAcceptance.Journal(m,m.ActualDeposits,Check);Check(true,"focused first-award capture; full-match evidence is recorded separately");m.SetupScreen();Check(m.StartMatch()&&m.ScoreJournal.Count==0,"focused scoring rematch starts with an empty journal");yield return null;m.SetupScreen();Complete();yield break;}}

    if(!paused&&m.rules.Clock>2){paused=true;intentionalPause=true;float clock=m.rules.Clock;var pose=m.trucks[0].body.position;m.Pause();yield return new WaitForSecondsRealtime(.15f);Check(clock==m.rules.Clock&&pose==m.trucks[0].body.position,"pause freezes clock, fields, trucks and metal");m.Resume();intentionalPause=false;Time.timeScale=4;}
    if(!captured&&m.rules.Clock>25){captured=true;yield return SportsWindowCapture.Save(1280,720,Folder+"/gameplay-720.png");yield return SportsWindowCapture.Save(1920,1080,Folder+"/gameplay-1080.png");}
    if(m.rules.Clock>=nextTrace){nextTrace=m.rules.Clock+15;report.AppendLine("ACTUAL_MAGNET t="+m.rules.Clock+" deposits="+m.ActualDeposits+" forces="+m.fields.ForceApplications+" maxN="+m.fields.MaximumApplied+" points="+string.Join(",",m.rules.points));for(int i=0;i<8;i++)report.AppendLine("TRUCK "+i+" p="+m.trucks[i].body.position+" speed="+m.trucks[i].body.linearVelocity.magnitude+" pole="+m.truckMetal[i].polarity);File.WriteAllText(Folder+"/progress.txt",report.ToString());}yield return null;
   }
   requestedScale=Time.timeScale=1;SportsScoreAcceptance.Journal(m,m.ActualDeposits,Check);Check(scoreCaptured,"actual award animation captured at720p/1080p");Check(m.Finished&&m.rules.Complete,"actual three-minute AI match reaches final standings without a softlock");Check(m.ActualDeposits>0&&m.fields.ForceApplications>100,"AI physically hauls and settles metal; every point comes from an actual bay deposit");Check(m.fields.MaximumApplied<=m.fields.maximumNewtons+.1f,"actual summed magnetic forces stay within configured bounds");int deposits=0;for(int i=0;i<8;i++)deposits+=m.rules.deliveries[i];Check(deposits==m.ActualDeposits&&deposits==40-m.rules.Remaining,"each real scrap object credits only one participant once");
   if(m.Finished){string result=m.Results;m.ExtraAction("watchmagnet");yield return new WaitForSecondsRealtime(.25f);Check(m.review.Playing,"recorded truck and physical scrap replay starts");yield return SportsWindowCapture.Save(1280,720,Folder+"/replay-720.png");m.ExtraAction("replayskip");Check(result==m.Results&&!m.review.Playing,"replay cannot alter scores or ownership");}
   yield return Fields(m);yield return SportsExpansionCameraAcceptance.Run(m,m.drivingViews,Check,Folder);
   m.SetupScreen();m.setup.count=8;m.AllBots();Check(m.StartMatch()&&m.ActualDeposits==0&&m.rules.Remaining==40,"rematch restores all forty objects, original points and field state");m.SetupScreen();Complete();
  }
  IEnumerator Fields(MagnetMatch m)
  {
   fixturePad=InputSystem.AddDevice<Gamepad>("Isolated magnetic field controls");m.SetupScreen();m.setup.count=1;m.AllBots();m.setup.players[0].source=BowlingSource.WholeController;m.setup.players[0].pad=fixturePad;Check(m.StartMatch(),"isolated physical polarity and occlusion fixture starts through setup");while(m.Countdown>0)yield return null;
   var item=m.scrap[2];for(int i=0;i<m.scrap.Length;i++)if(i!=2)m.scrap[i].gameObject.SetActive(false);
   var truck=m.trucks[0];truck.Prepare(new Vector3(-5,.65f,0),Quaternion.LookRotation(Vector3.right));Set(item,new Vector3(5,.05f,0));yield return new WaitForSeconds(.4f);
   m.SetPole(0,-item.polarity);float before=item.body.position.x;yield return new WaitForSeconds(.5f);Check(item.body.position.x<before-.15f,"opposite polarity physically attracts actual container without a grab teleport");
   m.SetPole(0,0);yield return new WaitForSeconds(.3f);truck.Prepare(new Vector3(-5,.65f,0),Quaternion.LookRotation(Vector3.right));Set(item,new Vector3(5,.05f,0));m.SetPole(0,item.polarity);before=item.body.position.x;yield return new WaitForSeconds(.5f);Check(item.body.position.x>before+.15f,"matching polarity physically repels the same container");
   m.SetPole(0,0);yield return new WaitForSeconds(.3f);int applications=m.fields.ForceApplications;yield return new WaitForSeconds(.25f);Check(applications==m.fields.ForceApplications,"OFF ceases this truck's field instead of leaving a stale force");
   truck.Prepare(new Vector3(-5,.65f,0),Quaternion.LookRotation(Vector3.right));Set(item,new Vector3(5,.05f,0));var wall=new GameObject("Acceptance solid magnetic obstruction");wall.transform.position=new Vector3(0,3,0);wall.AddComponent<BoxCollider>().size=new Vector3(.6f,6,14);Physics.SyncTransforms();m.SetPole(0,-item.polarity);applications=m.fields.ForceApplications;yield return new WaitForSeconds(.3f);Check(applications==m.fields.ForceApplications,"actual solid wall occludes attraction");Destroy(wall);m.SetPole(0,0);yield return new WaitForSeconds(.3f);
   Set(item,m.bays[0].transform.position+Vector3.up*.05f);yield return new WaitForSeconds(3);Check(m.rules.owners[2]==0&&m.rules.points[0]==item.points,"whole actual container settles in bay and awards its configured value");int score=m.rules.points[0];yield return new WaitForSeconds(2);Check(m.rules.points[0]==score&&item.body.isKinematic,"secured scrap cannot repeatedly score or be magnetically stolen after acceptance");m.SetupScreen();InputSystem.RemoveDevice(fixturePad);fixturePad=null;
  }
  static void Set(MagnetMetal item,Vector3 position){item.body.interpolation=RigidbodyInterpolation.None;item.body.linearVelocity=item.body.angularVelocity=Vector3.zero;item.body.position=position;item.body.rotation=Quaternion.identity;item.transform.SetPositionAndRotation(position,Quaternion.identity);item.body.interpolation=RigidbodyInterpolation.Interpolate;Physics.SyncTransforms();}
  void Complete(){report.AppendLine("CHECKS="+checks+" ERRORS="+errors+"; platform="+Application.platform+"; capture="+SportsWindowCapture.EvidenceKind+"; actual NWH/PhysX scrap hauling, field and software input tests; no human/device/listening certification.");File.WriteAllText(Folder+"/result.txt",report.ToString());Application.logMessageReceived-=Error;
#if UNITY_EDITOR
   if(Application.isEditor)UnityEditor.EditorApplication.isPlaying=false;else
#endif
   Application.Quit(errors==0?0:2);}
  void OnDestroy(){Application.logMessageReceived-=Error;if(fixturePad!=null&&fixturePad.added)InputSystem.RemoveDevice(fixturePad);InputSystem.settings.backgroundBehavior=background;
#if UNITY_EDITOR
   InputSystem.settings.editorInputBehaviorInPlayMode=editorInput;
#endif
   preferences?.Dispose();SportsParticipantRegistry.ForgetCache();Time.timeScale=1;}
 }
}
#endif
