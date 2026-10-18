#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;using System.IO;using System.Collections;using System.Text;using UnityEngine;using UnityEngine.InputSystem;using LWS.TruckSports;using LWS.TruckBowling;
namespace LWS.TruckCornhole {
public sealed class CornholeHoleProbe:MonoBehaviour {
 [RuntimeInitializeOnLoadMethod]static void Boot(){bool run=Array.IndexOf(Environment.GetCommandLineArgs(),"-tsCornholeHole")>=0;
#if UNITY_EDITOR
 run|=UnityEditor.SessionState.GetBool("Sports.CornholeHoleProbe",false);UnityEditor.SessionState.SetBool("Sports.CornholeHoleProbe",false);
#endif
 if(!run)return;var g=new GameObject("Explicit physical clean-hole acceptance");DontDestroyOnLoad(g);if(!SportsProbeInputIsolation.Active)g.AddComponent<SportsProbeInputIsolation>();g.AddComponent<CornholeHoleProbe>();}
 string folder;int errors,checks;float began;bool done,had;string oldSetup;readonly StringBuilder log=new StringBuilder();
 void Check(bool ok,string label){checks++;if(!ok)errors++;log.AppendLine((ok?"PASS ":"FAIL ")+label);File.WriteAllText(folder+"/progress.txt",log.ToString());}
 void Error(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;log.AppendLine("ERROR "+message);}}
 IEnumerator Start(){began=Time.realtimeSinceStartup;folder=SportsEvidencePaths.Folder("Party/CornholeHole");File.WriteAllText(folder+"/result.txt","RUNNING "+DateTime.UtcNow.ToString("O"));Application.logMessageReceived+=Error;had=SportsPrefs.HasKey(CornholeMatch.SetupKey);oldSetup=SportsPrefs.GetString(CornholeMatch.SetupKey);SportsNavigation.LoadSport("TruckCornhole");yield return null;yield return null;yield return new WaitForSecondsRealtime(.2f);var m=FindAnyObjectByType<CornholeMatch>();Check(m!=null,"actual saved Cornhole scene");
 foreach(float speed in new[]{8f,30f,70f,110f,160f})for(int repeat=0;repeat<3;repeat++){
 m.ChangeSetup();m.setup.count=1;m.AllAI();Check(m.StartMatch(),"fresh physical drop fixture");yield return new WaitForSeconds(.05f);var c=m.courses[0];c.enabled=false;var t=c.Current;t.body.linearVelocity=Vector3.forward*3;t.Launch();t.body.angularVelocity=Vector3.zero;var point=c.board.TransformPoint(new Vector3(0,4,c.holeZ));t.body.position=point;t.body.rotation=c.board.rotation;t.transform.SetPositionAndRotation(point,c.board.rotation);t.body.linearVelocity=Vector3.zero;t.command=Vector2.zero;Physics.SyncTransforms();yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();t.body.linearVelocity=c.board.TransformDirection(Vector3.down*speed);float end=Time.time+2;while(!t.Holed&&Time.time<end&&c.board.InverseTransformPoint(t.body.position).y> -3)yield return new WaitForFixedUpdate();
 log.AppendLine("DROP speed="+speed+" repeat="+repeat+" holed="+t.Holed+" points="+t.Points+" boardContact="+t.BoardContactTime+" ground="+t.Grounded+" local="+c.board.InverseTransformPoint(t.body.position));Check(t.Holed&&t.Points==3,"center clean full passage scores once speed="+speed+" repeat="+repeat);Check(t.BoardContactTime<0,"center passage requires no board contact");if(t.Holed){int score=c.Provisional;yield return new WaitForSeconds(.05f);Check(c.Provisional==score&&score==3,"later updates cannot duplicate or revoke hole score");}c.enabled=true;
 }
 foreach(int kind in new[]{0,1,2}){
 m.ChangeSetup();m.setup.count=1;m.AllAI();Check(m.StartMatch(),"negative passage fixture starts");yield return new WaitForSeconds(.05f);var c=m.courses[0];c.enabled=false;var t=c.Current;t.body.linearVelocity=Vector3.forward*3;t.Launch();var local=kind==0?new Vector3(2.8f,4,c.holeZ):kind==1?new Vector3(0,-3,c.holeZ):new Vector3(0,.2f,c.holeZ);var position=c.board.TransformPoint(local);t.body.position=position;t.body.rotation=c.board.rotation;t.transform.SetPositionAndRotation(position,c.board.rotation);t.body.linearVelocity=t.body.angularVelocity=Vector3.zero;Physics.SyncTransforms();yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();if(kind==2)t.body.isKinematic=true;else t.body.linearVelocity=c.board.TransformDirection(Vector3.up*(kind==1?80:-80));yield return new WaitForSeconds(.12f);Check(!t.Holed,kind==0?"outside the real opening does not score three":kind==1?"upward passage does not score":"overlap or hanging without full downward clearance does not score");c.enabled=true;
 }
 m.ChangeSetup();Restore();done=true;log.AppendLine("ERRORS="+errors+" CHECKS="+checks+" PID="+System.Diagnostics.Process.GetCurrentProcess().Id+". Real PhysX drops through unchanged opening; injected launch setup, not human throws.");File.WriteAllText(folder+"/result.txt",log.ToString());Debug.Log("TS_CORNHOLE_HOLE_FINISHED code="+(errors==0?0:2));if(!Application.isEditor)Application.Quit(errors==0?0:2);
 }
 void Restore(){Application.logMessageReceived-=Error;if(had)SportsPrefs.SetString(CornholeMatch.SetupKey,oldSetup);else SportsPrefs.DeleteKey(CornholeMatch.SetupKey);SportsPrefs.Save();Time.timeScale=1;}
 void Update(){if(!done&&folder!=null&&Time.realtimeSinceStartup-began>180){StopAllCoroutines();errors++;Restore();done=true;log.AppendLine("ERRORS="+errors+" INCOMPLETE timeout");File.WriteAllText(folder+"/result.txt",log.ToString());if(!Application.isEditor)Application.Quit(2);}}
 void OnDestroy(){if(!done)Restore();}
}
}
#endif
