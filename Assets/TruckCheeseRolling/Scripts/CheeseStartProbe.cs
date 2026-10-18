using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using LWS.TruckSports;
using LWS.TruckBowling;
namespace LWS.TruckCheeseRolling {
// Explicit native/Windows development reproduction. Never runs during normal play.
public sealed class CheeseStartProbe:MonoBehaviour {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
 [RuntimeInitializeOnLoadMethod]static void Boot(){bool run=Array.IndexOf(Environment.GetCommandLineArgs(),"-tsCheeseStart")>=0;
#if UNITY_EDITOR
 run|=UnityEditor.SessionState.GetBool("Sports.CheeseStartProbe",false);UnityEditor.SessionState.SetBool("Sports.CheeseStartProbe",false);
#endif
 if(!run)return;var g=new GameObject("Cheese start explicit reproduction");DontDestroyOnLoad(g);if(!SportsProbeInputIsolation.Active)g.AddComponent<SportsProbeInputIsolation>();g.AddComponent<CheeseStartProbe>();}
 readonly StringBuilder log=new StringBuilder();int errors;string folder;bool done;Keyboard keyboard;
 void Check(bool ok,string what){log.AppendLine((ok?"PASS ":"FAIL ")+what);if(!ok)errors++;}
 void Error(string message,string stack,LogType type){if(type==LogType.Exception||type==LogType.Error){errors++;log.AppendLine("RUNTIME ERROR "+message+"\n"+stack);}}
 IEnumerator Start(){folder=Path.GetFullPath(Path.Combine(Application.dataPath,Application.isEditor?"..":"../../..","Logs","TruckSports","Party","Cheese",Application.isEditor?"Native":"Windows"));Directory.CreateDirectory(folder);File.WriteAllText(folder+"/result.txt","RUNNING");Application.logMessageReceived+=Error;
  string mode=Mode();log.AppendLine("CHEESE_START_RUN mode="+mode+" editor="+Application.isEditor+" pid="+System.Diagnostics.Process.GetCurrentProcess().Id+" build="+Application.buildGUID);
  keyboard=InputSystem.AddDevice<Keyboard>();InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return SportsProbeScene.Ensure<CheeseMatch>(SportsNavigation.Cheese);yield return null;yield return null;
  var m=FindAnyObjectByType<CheeseMatch>();m.fixedSeed=true;var defaults=BowlingSetup.Defaults();int reproduced=0;
  // Disable only the deliberately earned jump in this reproduction so normal suspension release is isolated.
  m.startJump=m.startForward=0;
  for(int repeat=0;repeat<3;repeat++)for(int count=1;count<=4;count++){
   m.ChangeSetup();m.setup.count=count;m.seed=8137+repeat*91+count;while(new System.Random(m.seed*7919+17).Next(3)!=0)m.seed++;
   for(int i=0;i<4;i++){var p=m.setup.players[i];p.source=BowlingSource.Keyboard;p.pad=null;p.forward=defaults[i].forward;p.left=defaults[i].left;p.back=defaults[i].back;p.right=defaults[i].right;}
   if(!m.StartMatch()){errors++;log.AppendLine("FAIL cannot start independent-keyboard fixture");break;}
   InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
   var trace=m.trucks[0].GetComponent<CheeseStartContactTrace>();if(trace==null)trace=m.trucks[0].gameObject.AddComponent<CheeseStartContactTrace>();trace.events.Clear();trace.recording=true;
   float gap=0;int grounded=0,active=0,overlaps=0;foreach(var t in m.trucks)if(t.gameObject.activeSelf){active++;foreach(var w in t.wheels){Vector3 anchor=t.body.position+t.body.rotation*w.transform.localPosition;float terrain=m.weather.terrain.SampleHeight(anchor)+m.weather.terrain.transform.position.y;gap=Mathf.Max(gap,anchor.y-w.wheel.radius-w.spring.maxLength-terrain);if(w.IsGrounded)grounded++;}var box=t.body.GetComponent<BoxCollider>();if(Physics.ComputePenetration(box,box.transform.position,box.transform.rotation,m.weather.terrain.GetComponent<TerrainCollider>(),m.weather.terrain.transform.position,m.weather.terrain.transform.rotation,out _,out float depth)&&depth>.003f)overlaps++;}
   for(int i=0;i<m.trucks.Length;i++)if(m.trucks[i].gameObject.activeSelf)for(int j=0;j<i;j++)if(m.trucks[j].gameObject.activeSelf&&m.trucks[i].body.GetComponent<BoxCollider>().bounds.Intersects(m.trucks[j].body.GetComponent<BoxCollider>().bounds))overlaps++;
   log.AppendLine("START repeat="+repeat+" humans="+count+" active="+active+" maxWheelGap="+gap.ToString("F4")+" grounded="+grounded+" overlaps="+overlaps+" explosions="+m.ExplosionCount);
   if(mode=="verify"){Check(active==count+12,"all participants and twelve background trucks active");Check(gap<.08f&&overlaps==0,"start is near support with no solid overlaps");}
   float deadline=Time.realtimeSinceStartup+12;bool sampled=false;while(!m.Live&&Time.realtimeSinceStartup<deadline){if(!sampled&&m.Starting&&m.StartElapsed>2.6f){sampled=true;int groundedAtGo=0;bool held=true;float heightError=0;foreach(var t in m.trucks)if(t.gameObject.activeSelf){foreach(var w in t.wheels)if(w.IsGrounded)groundedAtGo++;held&=t.GridPrepared&&t.body.isKinematic&&t.body.linearVelocity.sqrMagnitude<.0001f&&t.body.angularVelocity.sqrMagnitude<.0001f&&Quaternion.Angle(t.body.rotation,Quaternion.identity)<.01f;foreach(var w in t.wheels){Vector3 anchor=t.body.position+t.body.rotation*w.transform.localPosition;float ground=m.weather.terrain.SampleHeight(anchor)+m.weather.terrain.transform.position.y;heightError=Mathf.Max(heightError,Mathf.Abs(anchor.y-w.wheel.radius-w.spring.length-ground));}}if(!Application.isEditor&&repeat==0)ScreenCapture.CaptureScreenshot(folder+"/grid-"+count+"-"+Screen.width+".png");log.AppendLine("BEFORE_GO grounded="+groundedAtGo+" rootHeight="+(m.trucks[0].body.position.y-CheeseRules.Height(m.trucks[0].body.position.x,m.trucks[0].body.position.z)).ToString("F4")+" velocity="+m.trucks[0].body.linearVelocity+" angular="+m.trucks[0].body.angularVelocity+" wheelContactError="+heightError.ToString("F4"));if(mode=="verify"){Check(groundedAtGo==active*4&&heightError<.025f,"every real NWH wheel supported before countdown release");Check(held,"every starter upright stationary and prepared at GO");}}yield return null;}
   if(!m.Live){errors++;log.AppendLine("FAIL no GO");break;}
   float minY=0,maxY=0,maxSpin=0,maxDamper=0,maxAngle=0;float end=Time.realtimeSinceStartup+.7f;
   while(Time.realtimeSinceStartup<end){var t=m.trucks[0];minY=Mathf.Min(minY,t.body.linearVelocity.y);maxY=Mathf.Max(maxY,t.body.linearVelocity.y);maxSpin=Mathf.Max(maxSpin,t.body.angularVelocity.magnitude);maxAngle=Mathf.Max(maxAngle,Vector3.Angle(t.transform.up,Vector3.up));foreach(var w in t.wheels)maxDamper=Mathf.Max(maxDamper,w.damper.force);yield return null;}
   trace.recording=false;foreach(var contact in trace.events)log.AppendLine("CONTACT "+contact);
   if(gap>.4f&&minY<-1)reproduced++;log.AppendLine("RELEASE minVy="+minY.ToString("F4")+" maxVy="+maxY.ToString("F4")+" maxSpin="+maxSpin.ToString("F4")+" maxTilt="+maxAngle.ToString("F3")+" maxDamperN="+maxDamper.ToString("F2")+" explosions="+m.ExplosionCount+" boostForces=0");
   if(mode=="verify"){Check(sampled&&minY>-.35f&&maxY<.35f&&maxAngle<.3f,"neutral release has no unsupported drop or suspension kick");Check(m.ExplosionCount==0,"no countdown or release explosion");}
  }
  m.ChangeSetup();log.AppendLine("REPRODUCED_FLOATING_DROP="+reproduced+"/12");
  if(mode=="verify")yield return Driving(m);
  done=true;log.AppendLine("ERRORS="+errors);File.WriteAllText(folder+"/result.txt",log.ToString());if(!Application.isEditor)Application.Quit(errors==0?0:2);
 }
 IEnumerator Driving(CheeseMatch m){
  m.startJump=5;m.startForward=4;
  for(int timing=0;timing<3;timing++){
   m.ChangeSetup();m.setup.count=1;m.setup.players[0].source=BowlingSource.Keyboard;InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
   Check(m.StartMatch(),"start timing fixture "+timing);float deadline=Time.realtimeSinceStartup+12;
   float pressAt=timing==0?.3f:timing==1?1.35f:2.3f;
   while((!m.Starting||m.StartElapsed<pressAt)&&Time.realtimeSinceStartup<deadline)yield return null;
   InputSystem.QueueStateEvent(keyboard,new KeyboardState(m.setup.players[0].forward));
   while(!m.Live&&Time.realtimeSinceStartup<deadline)yield return null;
   Check(m.Live&&m.trucks[0].StartBoosted==(timing==1),"fresh-input boost window preserved "+timing);
   Check(m.trucks[0].command.y>.5f,"held acceleration reaches GO "+timing);
   if(timing==1)Check(m.trucks[0].body.linearVelocity.y>3,"earned jump remains a real upward launch");
   float end=Time.realtimeSinceStartup+.4f;while(Time.realtimeSinceStartup<end)yield return null;
   if(timing==0)Check(m.trucks[0].body.linearVelocity.z>.1f,"ordinary first acceleration moves down-course without a launch");
   InputSystem.QueueStateEvent(keyboard,new KeyboardState());
  }
  m.ChangeSetup();m.setup.count=4;for(int i=0;i<4;i++){m.setup.players[i].source=BowlingSource.AI;m.setup.players[i].difficulty=(BowlingDifficulty)(i%3);}
  Check(m.StartMatch(),"four-participant autonomous physical match starts");Time.timeScale=3;float limit=Time.realtimeSinceStartup+300,next=Time.realtimeSinceStartup+10;float progress=0;
  while(m.InMatch&&!m.Finished&&Time.realtimeSinceStartup<limit){foreach(var t in m.trucks)if(t.Scoring)progress=Mathf.Max(progress,t.Progress);if(Time.realtimeSinceStartup>=next){next+=10;log.AppendLine("AI_PROGRESS round="+m.Round+" phase="+m.Phase+" catches="+m.Catches+" totals="+string.Join(",",m.totals)+" cheese="+m.cheese.body.position+" maxProgress="+progress.ToString("F1"));File.WriteAllText(folder+"/result.txt",log+"\nRUNNING");}yield return null;}
  Check(progress>200,"AI descends the varied enormous hill into runout");Check(m.Finished&&m.Catches>=5,"full autonomous five-catch match completes through real contacts");
  log.AppendLine("AI_RESULT catches="+m.Catches+" totals="+string.Join(",",m.totals)+" explosions="+m.ExplosionCount+" neutral="+m.NeutralReplays);Time.timeScale=1;m.ChangeSetup();
 }
 string Mode(){
#if UNITY_EDITOR
 return UnityEditor.SessionState.GetString("Sports.CheeseStartMode","baseline");
#else
 var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-tsCheeseStart");return i>=0&&i+1<args.Length?args[i+1]:"baseline";
#endif
 }
 void OnDestroy(){Application.logMessageReceived-=Error;if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);if(!done&&folder!=null)File.WriteAllText(folder+"/result.txt",log+"\nINCOMPLETE");}
#endif
}
#if UNITY_EDITOR || DEVELOPMENT_BUILD
public sealed class CheeseStartContactTrace:MonoBehaviour {
 public bool recording;public readonly System.Collections.Generic.List<string> events=new System.Collections.Generic.List<string>();
 void OnCollisionEnter(Collision c){if(!recording||events.Count>=6||c.contactCount==0)return;var hit=c.GetContact(0);events.Add("other="+c.collider.name+" normal="+hit.normal+" relativeNormalSpeed="+Mathf.Abs(Vector3.Dot(c.relativeVelocity,hit.normal)).ToString("F3")+" impulse="+c.impulse+" phase="+GetComponent<CheeseTruck>().match.Phase);}
}
#endif
}
