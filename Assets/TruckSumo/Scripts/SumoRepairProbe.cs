#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using LWS.TruckSports;

namespace LWS.TruckSumo {
// Explicit development-only evidence. No fixture runs in ordinary matches.
public sealed class SumoRepairProbe:MonoBehaviour {
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Boot(){
  var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsSumoRepair");bool run=at>=0;string mode=at>=0&&at+1<args.Length?args[at+1]:"contact";
#if UNITY_EDITOR
  if(UnityEditor.SessionState.GetBool("Sumo.RepairProbe",false)){run=true;mode=UnityEditor.SessionState.GetString("Sumo.RepairMode","contact");}UnityEditor.SessionState.SetBool("Sumo.RepairProbe",false);
#endif
  if(!run)return;var root=new GameObject("Sumo explicit scoring acceptance");DontDestroyOnLoad(root);root.AddComponent<SportsProbeInputIsolation>();root.AddComponent<SumoRepairProbe>().mode=mode;
 }
 string mode,folder;int checks,errors;IDisposable preferences;readonly StringBuilder log=new StringBuilder();SumoMatch match;Keyboard keyboard;InputSettings originalInput,fixtureInput;
 void Awake(){preferences=SportsPrefs.IsolateValidation("SumoRepair");SportsParticipantRegistry.ForgetCache();Application.logMessageReceived+=Error;}
 void Error(string value,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;log.AppendLine("ERROR "+value+"\n"+stack);Write();}}
 void Write(){if(folder!=null)File.WriteAllText(folder+"/progress.txt",log.ToString());}
 void Check(bool ok,string value){checks++;if(!ok)errors++;log.AppendLine((ok?"PASS ":"FAIL ")+value);Write();}
 IEnumerator Start(){
  folder=Path.GetFullPath(Path.Combine(Application.dataPath,Application.isEditor?"..":"../../..","Logs/TruckSports/SumoRepair",Application.isEditor?"Native":"Windows",mode));Directory.CreateDirectory(folder);File.WriteAllText(folder+"/result.txt","RUNNING");log.AppendLine("SUMO_REPAIR mode="+mode+" PID="+System.Diagnostics.Process.GetCurrentProcess().Id+" editor="+Application.isEditor);
  var stack=new System.Collections.Generic.Stack<IEnumerator>();stack.Push(Run());while(stack.Count>0){object next=null;try{var current=stack.Peek();if(!current.MoveNext()){stack.Pop();continue;}next=current.Current;if(next is IEnumerator child){stack.Push(child);continue;}}catch(Exception e){Check(false,e.ToString());break;}yield return next;}
  log.AppendLine("CHECKS="+checks);log.AppendLine("ERRORS="+errors);log.AppendLine(errors==0?"PASS":"FAIL");File.WriteAllText(folder+"/result.txt",log.ToString());
#if UNITY_EDITOR
  if(Application.isEditor)UnityEditor.EditorApplication.isPlaying=false;else
#endif
  Application.Quit(errors==0?0:2);
 }
 IEnumerator Run(){if(mode=="contact")yield return ContactCalibration();else if(mode=="physical")yield return PhysicalScoring();else if(mode=="shove")yield return StandingShove();else throw new ArgumentException("Unknown Sumo repair probe "+mode);}
 IEnumerator StandingShove(){
  originalInput=InputSystem.settings;fixtureInput=Instantiate(originalInput);fixtureInput.updateMode=InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;InputSystem.settings=fixtureInput;keyboard=InputSystem.AddDevice<Keyboard>();
  yield return SportsProbeScene.Ensure<SumoMatch>("TruckSumo");match=FindAnyObjectByType<SumoMatch>();yield return null;match.setup.count=4;match.NormalizeSetup();for(int i=0;i<4;i++)match.setup.players[i].source=LWS.TruckBowling.BowlingSource.Keyboard;
  yield return FreshFight();Place(0,new Vector3(0,.7f,7),0);Place(1,new Vector3(0,.7f,match.Radius-.1f),0);Place(2,new Vector3(-6,.7f,-5),0);Place(3,new Vector3(-6,.7f,5),0);
  var victim=match.fighters[1].truck;var attacker=match.fighters[0].truck;victim.body.position+=Vector3.forward*(match.Radius-.1f-victim.body.worldCenterOfMass.z);Physics.SyncTransforms();float gap=victim.shape.bounds.min.z-attacker.shape.bounds.max.z;attacker.body.position+=Vector3.forward*(gap+.002f);Physics.SyncTransforms();var trace=victim.gameObject.AddComponent<SumoImpulseTrace>();trace.body=victim.body;trace.only=attacker.body;bool gravity=victim.body.useGravity;victim.body.useGravity=false;victim.Power(false);yield return new WaitForSeconds(.3f);
  Check(trace.contacts>0,"actual resting truck contact exists before the stationary shove");
  Check(attacker.Grounded&&Vector3.ProjectOnPlane(attacker.body.linearVelocity,Vector3.up).magnitude<.35f,"grounded stationary human truck is ready for its ordinary shove action");
  InputSystem.QueueStateEvent(keyboard,new KeyboardState(match.actionKeys[0]));yield return Until(()=>match.fighters[0].Shoves==1,2,"fresh assigned action produces one ordinary bounded shove");InputSystem.QueueStateEvent(keyboard,new KeyboardState());
  yield return Until(()=>!match.rules.alive[1],4,"standing human shove physically pushes the opponent over the rim");victim.body.useGravity=gravity;
  log.AppendLine("STANDING_SHOVE contacts="+match.QualifyingContacts+" incoming="+match.fighters[0].IncomingVelocity+" shoves="+match.fighters[0].Shoves+" points="+string.Join(",",match.rules.points)+" ringouts="+string.Join(",",match.rules.ringouts));log.AppendLine(trace.record.ToString());
  Check(match.rules.points[0]==1&&match.rules.ringouts[0]==1,"standing shove's actual collision receives exactly one ring-out point");match.SetupScreen();SportsNavigation.FrontEnd();yield return null;yield return null;Check(FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude).Length==1,"standing shove fixture cleans up to one audio owner");
 }
 IEnumerator Until(Func<bool> condition,float seconds,string label){float end=Time.realtimeSinceStartup+seconds;while(!condition()&&Time.realtimeSinceStartup<end)yield return null;Check(condition(),label);}
 IEnumerator FreshFight(){InputSystem.QueueStateEvent(keyboard,new KeyboardState());match.SetupScreen();Check(match.StartMatch(),"four human keyboard-source fixture starts");Time.timeScale=2;yield return Until(()=>match.Stage==SumoStage.Fight,8,"normal countdown enters physical fight");yield return new WaitForSeconds(.2f);}
 void Place(int slot,Vector3 position,float yaw){match.fighters[slot].ResetFighter(position,Quaternion.Euler(0,yaw,0));Physics.SyncTransforms();}
 IEnumerator Capture(int width,int height,string label){var prior=match.menus.Screen;match.menus.Show(SportsEventScreen.Play);yield return new WaitForSecondsRealtime(.15f);if(Application.isEditor)SportsControlledCapture.Save(match,width,height,folder+"/"+label+"-"+width+"-controlled.png");else{Screen.SetResolution(width,height,false);yield return new WaitForSecondsRealtime(.35f);ScreenCapture.CaptureScreenshot(folder+"/"+label+"-"+width+"-window.png");yield return new WaitForSecondsRealtime(.2f);Check(Screen.width==width&&Screen.height==height,"actual Windows viewport "+width+"x"+height);}match.menus.Show(prior);}
 IEnumerator PhysicalScoring(){
  originalInput=InputSystem.settings;fixtureInput=Instantiate(originalInput);fixtureInput.updateMode=InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;InputSystem.settings=fixtureInput;keyboard=InputSystem.AddDevice<Keyboard>();
  yield return SportsProbeScene.Ensure<SumoMatch>("TruckSumo");match=FindAnyObjectByType<SumoMatch>();yield return null;match.setup.count=4;match.NormalizeSetup();for(int i=0;i<4;i++){match.setup.players[i].source=LWS.TruckBowling.BowlingSource.Keyboard;match.setup.players[i].name=i<2?"Sumo Twin":"Long Sumo Contender "+(i+1);}
  yield return FreshFight();Place(0,new Vector3(0,.7f,5.8f),0);Place(1,new Vector3(0,.7f,9.6f),0);Place(2,new Vector3(-6,.7f,-5),0);Place(3,new Vector3(-6,.7f,5),0);
  InputSystem.QueueStateEvent(keyboard,new KeyboardState(match.setup.players[0].forward));yield return Until(()=>!match.rules.alive[1],10,"actual keyboard-driven bumper push produces a real opponent ring-out");InputSystem.QueueStateEvent(keyboard,new KeyboardState());
  log.AppendLine("REAL_RINGOUT contacts="+match.QualifyingContacts+" lastVictim="+match.LastImpactVictim+" lastAttacker="+match.LastImpactAttacker+" normal="+match.LastReceivingNormal+" normalSpeed="+match.LastContactSpeed+" points="+string.Join(",",match.rules.points)+" ringouts="+string.Join(",",match.rules.ringouts));
  Check(match.rules.ringouts[0]==1&&match.rules.points[0]==1&&match.rules.points[1]==0,"actual physical aggressor earns one point; victim receives none");yield return new WaitForSeconds(.25f);Check(match.rules.ringouts[0]==1&&match.rules.points[0]==1,"continued contacts and eliminated body cannot duplicate the award");
  var score=match.menus.GetComponentInChildren<SportsScorePresentation>(true);Check(score!=null,"saved shared Heat presentation is integrated in the real scene");
  var receipt=match.ScoreJournal.Latest;Check(match.ScoreJournal.Count==1&&receipt!=null&&receipt.ParticipantId==match.setup.players[0].participantId&&receipt.ParticipantId!=match.setup.players[1].participantId&&receipt.Delta==1&&receipt.Total==match.rules.points[0],"real bumper award has one immutable stable-ID receipt despite duplicate names");
  if(score!=null){yield return Until(()=>score.Presented==1,4,"actual Heat notification consumes the confirmed ring-out once");Check(score.LastPresented==receipt&&match.rules.points[0]==1,"score animation cannot create points or change attribution");float scoreCaptureScale=Time.timeScale;yield return new WaitForSeconds(Mathf.Max(.1f,Michsky.UI.Heat.HeatUIInternalTools.GetAnimatorClipLength(score.notificationAnimator,"Notification_In"))+.03f);Time.timeScale=0;log.AppendLine("CONTROLLED_SCORE_CAPTURE actual committed award and completed Heat In animation held only for 720p/1080p backbuffers; original game clock restored afterward.");yield return Capture(1280,720,"confirmed-score-banner");yield return Capture(1920,1080,"confirmed-score-banner");Time.timeScale=scoreCaptureScale;}
  match.TryReadScoreCard(0,out var first,out var firstDetail);match.TryReadScoreCard(1,out var second,out _);Check(match.setup.players[0].name==match.setup.players[1].name&&first=="1"&&second=="0"&&firstDetail.Contains("1 OUT"),"duplicate names retain independent typed point/ring-out cards");match.Pause();int kept=match.rules.points[0];yield return new WaitForSecondsRealtime(.2f);Check(match.rules.points[0]==kept,"pause preserves actual awarded score");yield return Capture(1280,720,"credited-ringout");yield return Capture(1920,1080,"credited-ringout");match.Resume();
  yield return FreshFight();Place(0,new Vector3(0,.7f,7),0);Place(1,new Vector3(0,.7f,match.Radius-.04f),0);Place(2,new Vector3(-6,.7f,-5),0);Place(3,new Vector3(-6,.7f,5),0);
  var edgeVictim=match.fighters[1].truck;var edgeAttacker=match.fighters[0].truck;
  edgeVictim.body.position+=Vector3.forward*(match.Radius-.04f-edgeVictim.body.worldCenterOfMass.z);Physics.SyncTransforms();float gap=edgeVictim.shape.bounds.min.z-edgeAttacker.shape.bounds.max.z;edgeAttacker.body.position+=Vector3.forward*(gap-.02f);Physics.SyncTransforms();
  bool victimGravity=edgeVictim.body.useGravity,attackerGravity=edgeAttacker.body.useGravity;edgeVictim.body.useGravity=edgeAttacker.body.useGravity=false;edgeVictim.Power(false);edgeAttacker.Power(false);edgeAttacker.body.linearVelocity=Vector3.forward*8;
  yield return Until(()=>!match.rules.alive[1],3,"initialized actual truck contact crosses the rim in its first receiving physics step");edgeVictim.body.useGravity=victimGravity;edgeAttacker.body.useGravity=attackerGravity;
  log.AppendLine("IMMEDIATE_RIM pre="+match.fighters[1].IncomingPosition+" after="+edgeVictim.body.worldCenterOfMass+" contacts="+match.QualifyingContacts+" points="+string.Join(",",match.rules.points));Check(match.rules.points[0]==1&&match.rules.ringouts[0]==1,"immediate physical rim crossing uses pre-step contact evidence and scores once");
  yield return FreshFight();Place(0,new Vector3(0,.7f,10),0);Place(1,new Vector3(-6,.7f,-5),0);Place(2,new Vector3(-6,.7f,5),0);Place(3,new Vector3(5,.7f,-5),0);InputSystem.QueueStateEvent(keyboard,new KeyboardState(match.setup.players[0].forward));yield return Until(()=>!match.rules.alive[0],8,"actual unopposed human truck drives itself over the visible edge");InputSystem.QueueStateEvent(keyboard,new KeyboardState());Check(Array.TrueForAll(match.rules.points,n=>n==0),"self-inflicted fall grants nobody a ring-out point");
  yield return FreshFight();Place(0,new Vector3(0,.7f,0),0);Place(1,new Vector3(0,.7f,10),0);Place(2,new Vector3(10,.7f,0),90);Place(3,new Vector3(0,.7f,-10),180);InputSystem.QueueStateEvent(keyboard,new KeyboardState(match.setup.players[1].forward,match.setup.players[2].forward,match.setup.players[3].forward));yield return Until(()=>match.Stage==SumoStage.Replay,10,"three physical departures leave a sole survivor and start actual replay");InputSystem.QueueStateEvent(keyboard,new KeyboardState());Check(match.rules.HeatWinner==0&&match.rules.points[0]==3&&match.rules.wins[0]==1&&match.rules.ringouts[0]==0,"passive sole survivor earns exactly three points and one heat win");Check(match.ScoreJournal.Count==1&&match.ScoreJournal.Latest.Delta==3&&match.ScoreJournal.Latest.RoundEnded&&match.ScoreJournal.Latest.Tier==SportsScoreTier.RoundWinner,"survival is one confirmed +3 round-winner receipt, separate from ring-out points");int receiptsBeforeReplay=match.ScoreJournal.Count;int[] sealedPoints=(int[])match.rules.points.Clone();match.replay.Seek(0);match.ExtraAction("replayangle");match.ExtraAction("replayspeed");match.replay.Seek(match.replay.Duration);Check(string.Join(",",sealedPoints)==string.Join(",",match.rules.points),"actual replay seek/angle/slow motion cannot change awarded points");Check(match.ScoreJournal.Count==receiptsBeforeReplay,"replay never republishes confirmed score events");match.ExtraAction("replayskip");yield return Until(()=>match.rules.Heat==2,8,"normal replay/result progression advances to next heat");Check(match.rules.points[0]==3&&match.rules.wins[0]==1,"next heat retains cumulative survival reward");
  match.SetupScreen();Check(match.StartMatch()&&Array.TrueForAll(match.rules.points,n=>n==0)&&Array.TrueForAll(match.rules.ringouts,n=>n==0),"rematch clears all points, ring-outs, heat wins and contact state");yield return null;Check(match.ScoreJournal.Count==0&&score.Presented==0,"rematch clears journal and presentation state");match.SetupScreen();SportsNavigation.FrontEnd();yield return null;yield return null;Check(FindAnyObjectByType<SportsFrontEnd>()!=null&&FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude).Length==1,"physical scoring exits cleanly to one collection/audio owner");
 }
 IEnumerator ContactCalibration(){
  var scene=SceneManager.CreateScene("Sumo impulse direction fixture",new CreateSceneParameters(LocalPhysicsMode.Physics3D));var simulation=scene.GetPhysicsScene();
  var attacker=TraceBody(scene,"Attacker",new Vector3(0,0,-2),Vector3.forward*6);var victim=TraceBody(scene,"Victim",Vector3.zero,Vector3.zero);
  for(int step=0;step<40;step++)simulation.Simulate(.02f);
  Check(attacker.contacts==1&&victim.contacts==1,"real dynamic bodies produce one first-contact callback each");
  log.AppendLine(attacker.record.ToString());log.AppendLine(victim.record.ToString());
  Check(victim.normal.z>.9f&&attacker.normal.z<-.9f,"receiving contact normals identify the opposite physical response directions");
  Check(Vector3.Dot(victim.normal,victim.body.linearVelocity-victim.initial)>0&&Vector3.Dot(attacker.normal,attacker.body.linearVelocity-attacker.initial)>0,"normal-oriented impulse agrees with each body's actual momentum change");
  Check(victim.body.linearVelocity.z>1&&attacker.body.linearVelocity.z<6,"actual contact transfers forward momentum without a custom impulse");
  yield return SceneManager.UnloadSceneAsync(scene);
 }
 static SumoImpulseTrace TraceBody(Scene scene,string name,Vector3 position,Vector3 velocity){var g=new GameObject(name);SceneManager.MoveGameObjectToScene(g,scene);g.transform.position=position;g.AddComponent<BoxCollider>();var body=g.AddComponent<Rigidbody>();body.mass=12;body.useGravity=false;body.linearDamping=0;body.angularDamping=0;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;body.linearVelocity=velocity;var trace=g.AddComponent<SumoImpulseTrace>();trace.body=body;trace.initial=velocity;return trace;}
 void OnDestroy(){Application.logMessageReceived-=Error;if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);if(originalInput!=null)InputSystem.settings=originalInput;if(fixtureInput!=null)Destroy(fixtureInput);preferences?.Dispose();SportsParticipantRegistry.ForgetCache();Time.timeScale=1;}
}
public sealed class SumoImpulseTrace:MonoBehaviour {
 public Rigidbody body,only;public int contacts;public Vector3 impulse,normal,initial;public readonly StringBuilder record=new StringBuilder();
 void OnCollisionEnter(Collision collision){if(only!=null&&collision.rigidbody!=only)return;contacts++;impulse=collision.impulse;normal=collision.GetContact(0).normal;record.AppendLine("ACTUAL_CONTACT body="+name+" other="+collision.collider.name+" impulse="+impulse.ToString("F4")+" normal="+normal.ToString("F4")+" relativeVelocity="+collision.relativeVelocity.ToString("F4")+" postVelocity="+body.linearVelocity.ToString("F4"));}
 int forceStays;
 void OnCollisionStay(Collision collision){if(only==null||collision.rigidbody!=only||collision.impulse.magnitude<.5f||forceStays++>=8)return;var fighter=only.GetComponent<SumoFighter>();record.AppendLine("ACTUAL_PRESSURE impulse="+collision.impulse.ToString("F4")+" normal="+collision.GetContact(0).normal.ToString("F4")+" relativeVelocity="+collision.relativeVelocity.ToString("F4")+" otherIncoming="+(fighter!=null?fighter.IncomingVelocity.ToString("F4"):"none"));}
}
}
#endif
