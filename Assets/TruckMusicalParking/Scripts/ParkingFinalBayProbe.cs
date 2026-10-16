using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using LWS.TruckSports;
using LWS.TruckBowling;
namespace LWS.TruckMusicalParking {
public sealed class ParkingFinalBayProbe:MonoBehaviour {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
 [RuntimeInitializeOnLoadMethod]static void Boot(){bool run=Array.IndexOf(Environment.GetCommandLineArgs(),"-tsParkingFinal")>=0;bool before=false;
#if UNITY_EDITOR
 run|=UnityEditor.SessionState.GetBool("LWS.ParkingFinal",false);before=UnityEditor.SessionState.GetBool("LWS.ParkingFinalBefore",false);UnityEditor.SessionState.SetBool("LWS.ParkingFinal",false);
#endif
 if(run){var g=new GameObject("Owned final-bay actual driving reproduction");DontDestroyOnLoad(g);if(!SportsProbeInputIsolation.Active)g.AddComponent<SportsProbeInputIsolation>();g.AddComponent<ParkingFinalBayProbe>().before=before;}}
 bool before,had,restored;Keyboard keyboard;ParkingMatch m;string folder,key,saved;InputSettings.BackgroundBehavior background;readonly StringBuilder log=new StringBuilder();int errors;
 void Check(bool ok,string label){log.AppendLine((ok?"PASS ":"FAIL ")+label);if(!ok)errors++;File.WriteAllText(folder+"/progress.txt",log.ToString());}
 void Error(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;log.AppendLine("ERROR "+message);File.WriteAllText(folder+"/progress.txt",log.ToString());}}
 IEnumerator Start(){folder=SportsEvidencePaths.Folder(before?"ParkingFinalBefore":"ParkingFinalAfter");background=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;keyboard=InputSystem.AddDevice<Keyboard>();InputSystem.EnableDevice(keyboard);yield return SportsProbeScene.Ensure<ParkingMatch>("TruckMusicalParking");m=FindAnyObjectByType<ParkingMatch>();yield return null;key=m.PreferenceKey;had=SportsPrefs.HasKey(key);saved=SportsPrefs.GetString(key);Application.logMessageReceived+=Error;m.AllBots();m.setup.players[1].source=BowlingSource.Keyboard;Check(m.StartMatch(),"controlled final2/onebay reproduction starts");for(int i=0;i<8;i++)m.active[i]=i<2;typeof(ParkingMatch).GetProperty("Remaining").SetValue(m,2);typeof(ParkingMatch).GetMethod("BeginRound",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(m,null);Time.timeScale=4;while(m.Phase==ParkingPhase.Countdown)yield return null;var recent=(float[])typeof(ParkingMatch).GetField("lastLegal",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(m);for(int i=0;i<2;i++){m.routeProgress[i]=20;recent[i]=m.Age;}m.Horn();m.debt[0]=m.debt[1]=0;
  var truck=m.trucks[0];var bay=m.bays[0];float y=bay.transform.position.y+truck.shape.size.y*.5f-truck.shape.center.y+.015f;var start=bay.transform.position+Vector3.right*1.65f;start.y=y;truck.Prepare(start,Quaternion.Euler(0,90,0));var selected=(int[])typeof(ParkingMatch).GetField("selected",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(m);selected[0]=0;((float[])typeof(ParkingMatch).GetField("reaction",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(m))[0]=0;var error=(Vector3[])typeof(ParkingMatch).GetField("executionOffset",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(m);error[0]=Vector3.zero;Physics.SyncTransforms();Check(!ParkingRules.Contains(truck.shape,truck.body,bay.transform.position,bay.transform.rotation,bay.size),"angled near-center authoritative body actually protrudes beyond painted bay");float end=Time.time+8,motorTime=0;while(Time.time<end&&m.secured[0]<0&&!m.Finished){if(Mathf.Abs(truck.wheels[0].MotorTorque)>.05f)motorTime+=Time.fixedDeltaTime;yield return new WaitForFixedUpdate();}float moved=Vector3.Distance(truck.body.position,start);bool contained=ParkingRules.Contains(truck.shape,truck.body,bay.transform.position,bay.transform.rotation,bay.size);log.AppendLine("MEASURE initial="+start.ToString("F5")+" final="+truck.body.position.ToString("F5")+" shapeCenter="+truck.shape.center.ToString("F5")+" shapeSize="+truck.shape.size.ToString("F5")+" contained="+contained+" capture="+m.capture[0]+" owner="+bay.Owner+" motorSeconds="+motorTime+" moved="+moved+" finalMotor="+truck.wheels[0].MotorTorque+" finalBrake="+truck.wheels[0].BrakeTorque);
  if(before)Check(!contained&&m.secured[0]<0&&m.capture[0]==0&&Mathf.Abs(truck.wheels[0].MotorTorque)<.01f&&truck.wheels[0].BrakeTorque>0&&moved<.3f,"REPRODUCED radial brake stalls with zero capture and a protruding body");else Check(contained&&m.secured[0]==0&&bay.Owner==0&&motorTime>.05f,"same physical near-bay AI now centers, slows and captures authoritative final space");m.SetupScreen();Check(m.StartMatch()&&m.Remaining==8&&m.secured[0]<0,"rematch resets controlled final-round state");m.SetupScreen();Restore();log.AppendLine("ERRORS="+errors+". Deliberately initialized two-player final-round state/pose; actual NWH drive, chassis footprint, PhysX and capture timer. No human or hardware claim.");File.WriteAllText(folder+"/result.txt",log.ToString());if(!Application.isEditor)Application.Quit(errors==0?0:2);
 }
 void Restore(){if(restored)return;restored=true;Application.logMessageReceived-=Error;if(m!=null)m.SetupScreen();if(key!=null){if(had)SportsPrefs.SetString(key,saved);else SportsPrefs.DeleteKey(key);SportsPrefs.Save();}if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);keyboard=null;InputSystem.settings.backgroundBehavior=background;Time.timeScale=1;}
 void OnDestroy(){Restore();}
#endif
}
}
