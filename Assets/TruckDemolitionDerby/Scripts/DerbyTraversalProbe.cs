using System;using System.IO;using System.Text;using System.Linq;using System.Collections;using UnityEngine;using LWS.TruckSports;
namespace LWS.TruckDerby {
public sealed class DerbyTraversalContact:MonoBehaviour {
 public DerbyFighter fighter;public readonly StringBuilder events=new StringBuilder();public int Contacts;
 void OnCollisionEnter(Collision collision){Contacts++;var renderers=collision.collider.GetComponentsInChildren<Renderer>(true);int visible=renderers.Count(r=>r.enabled&&r.gameObject.activeInHierarchy&&!r.forceRenderingOff);if(visible==0&&collision.collider.transform.parent!=null)visible=collision.collider.transform.parent.GetComponentsInChildren<Renderer>(true).Count(r=>r.enabled&&r.gameObject.activeInHierarchy&&!r.forceRenderingOff&&r.bounds.Intersects(collision.collider.bounds));for(int i=0;i<Mathf.Min(4,collision.contactCount);i++){var p=collision.GetContact(i);events.AppendLine("CONTACT object="+collision.collider.name+" normal="+p.normal.ToString("F3")+" incomingNormal="+Mathf.Max(0,-Vector3.Dot(fighter.Incoming,p.normal)).ToString("F3")+" p="+p.point.ToString("F3")+" visibleModels="+visible+" dynamic="+(collision.rigidbody!=null)+" bounds="+collision.collider.bounds);}}
}
public sealed class DerbyTraversalProbe:MonoBehaviour {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
 [RuntimeInitializeOnLoadMethod]static void Boot(){bool run=Array.IndexOf(Environment.GetCommandLineArgs(),"-tsDerbyTraverse")>=0;string mode="baseline";
#if UNITY_EDITOR
 run|=UnityEditor.SessionState.GetBool("Sports.Derby.Traverse",false);UnityEditor.SessionState.SetBool("Sports.Derby.Traverse",false);mode=UnityEditor.SessionState.GetString("Sports.Derby.TraverseMode","baseline");
#endif
 if(run){var go=new GameObject("Explicit controlled NWH central-course traversal");DontDestroyOnLoad(go);go.AddComponent<SportsProbeInputIsolation>();go.AddComponent<DerbyTraversalProbe>().mode=mode;}}
 string mode="baseline",folder;readonly StringBuilder log=new StringBuilder();IDisposable prefs;DerbyMatch match;DerbyFighter subject;bool driving;int errors,checks;
 void Awake(){prefs=SportsPrefs.IsolateValidation("DerbyTraverse");SportsParticipantRegistry.ForgetCache();}
 void Check(bool ok,string message){checks++;if(!ok)errors++;log.AppendLine((ok?"PASS ":"FAIL ")+message);Write();}void Write()=>File.WriteAllText(folder+"/progress.txt",log.ToString());
 void Error(string s,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;log.AppendLine("ERROR "+s);Write();}}
 IEnumerator Start(){folder="Logs/TruckSports/EssentialRelease/Derby/Traversal/"+mode;Directory.CreateDirectory(folder);File.WriteAllText(folder+"/result.txt","RUNNING "+DateTime.UtcNow.ToString("o"));Application.logMessageReceived+=Error;yield return SportsProbeScene.Ensure<DerbyMatch>("TruckDemolitionDerby");yield return null;match=FindAnyObjectByType<DerbyMatch>();match.option=1;match.setup.count=2;match.AllBots();
  var starts=new[]{new Vector3(0,1.3f,-30),new Vector3(-12,1.3f,-30),new Vector3(12,1.3f,-30),new Vector3(-32,1.3f,-6),new Vector3(32,1.3f,6),new Vector3(-32,1.3f,6),new Vector3(32,1.3f,-6)};for(int trial=0;trial<starts.Length;trial++){var start=starts[trial];var direction=trial<3?Vector3.forward:trial%2==1?Vector3.right:Vector3.left;var rotation=Quaternion.LookRotation(direction);string lane=trial+" "+start+" -> "+direction;match.enabled=true;match.SetupScreen();Check(match.StartMatch(),"original two-truck lifecycle starts controlled lane="+lane);Time.timeScale=2;float deadline=Time.realtimeSinceStartup+15;while(match.Stage==DerbyStage.Countdown&&Time.realtimeSinceStartup<deadline)yield return null;Check(match.Stage==DerbyStage.Playing,"original countdown releases NWH before fixture control");match.enabled=false;subject=match.fighters[0];for(int i=1;i<match.fighters.Length;i++)match.fighters[i].gameObject.SetActive(false);subject.ResetFighter(start,rotation);subject.Wake();var trace=subject.GetComponent<DerbyTraversalContact>()??subject.gameObject.AddComponent<DerbyTraversalContact>();trace.fighter=subject;trace.events.Clear();trace.Contacts=0;driving=true;float peak=0,stopped=0,peakY=subject.body.position.y;deadline=Time.realtimeSinceStartup+18;float next=0;
   while(Vector3.Dot(subject.body.position-start,direction)<59&&Time.realtimeSinceStartup<deadline){peakY=Mathf.Max(peakY,subject.body.position.y);peak=Mathf.Max(peak,subject.body.linearVelocity.magnitude);stopped=subject.body.linearVelocity.magnitude<.5f?stopped+Time.deltaTime:0;if(Time.realtimeSinceStartup>=next){next=Time.realtimeSinceStartup+1;log.AppendLine("LIVE lane="+lane+" p="+subject.body.position.ToString("F3")+" v="+subject.body.linearVelocity.ToString("F2")+" damage="+subject.damage.Structure.ToString("F3")+" engine="+subject.damage.Engine.ToString("F3")+" contact="+trace.Contacts+" stopped="+stopped.ToString("F1"));Write();}yield return null;}
   driving=false;log.Append(trace.events);Check(Vector3.Dot(subject.body.position-start,direction)>=59,"actual NWH tractor traverses the central course with saved props retained · lane="+lane+" final="+subject.body.position.ToString("F2")+" peak="+peak.ToString("F2")+" peakChassisY="+peakY.ToString("F3"));Write();subject.vehicle.input.states.Reset();subject.Freeze();match.enabled=true;match.SetupScreen();}
  Time.timeScale=1;SportsNavigation.FrontEnd();yield return null;yield return null;Check(FindAnyObjectByType<DerbyMatch>()==null,"controlled traversal exits without retaining disabled match authority");log.AppendLine("CHECKS="+checks+" ERRORS="+errors+" · actual NWH/PhysX, controlled forward commands and relocated test starts; normal match AI/timers intentionally disabled after countdown to isolate geometry. Not a human/full-match/Windows test.");File.WriteAllText(folder+"/result.txt",log.ToString());Application.logMessageReceived-=Error;
#if UNITY_EDITOR
 if(Application.isEditor)UnityEditor.EditorApplication.isPlaying=false;else
#endif
 Application.Quit(errors==0?0:2);
 }
 void FixedUpdate(){if(driving&&subject!=null)subject.Drive(Vector2.up,Time.fixedDeltaTime);}
 void OnDestroy(){driving=false;if(match!=null){match.enabled=true;match.SetupScreen();}Application.logMessageReceived-=Error;prefs?.Dispose();SportsParticipantRegistry.ForgetCache();Time.timeScale=1;}
#endif
}
}
