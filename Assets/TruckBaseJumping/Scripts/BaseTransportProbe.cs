using System;using System.IO;using System.Collections;using System.Text;using UnityEngine;using LWS.TruckBowling;
namespace LWS.TruckBaseJumping {
// Opt-in native integration evidence; software AI and actual PhysX, no physical-controller claim.
public sealed class BaseTransportProbe:MonoBehaviour {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){bool run=Array.IndexOf(Environment.GetCommandLineArgs(),"-tsTransport")>=0;
#if UNITY_EDITOR
 run|=UnityEditor.SessionState.GetBool("LWS.TransportProbe",false);UnityEditor.SessionState.SetBool("LWS.TransportProbe",false);
#endif
 if(run){var g=new GameObject("Transport and sky physical validation");DontDestroyOnLoad(g);g.AddComponent<BaseTransportProbe>();}}
 BaseJumpMatch match;string saved;bool existed,restored;readonly StringBuilder report=new StringBuilder();readonly bool[] released=new bool[4];readonly float[] exitTimes=new float[4];string folder;int errors,clips;Quaternion firstRotor;
 IEnumerator Start(){folder=LWS.TruckSports.SportsEvidencePaths.Folder("TransportAudit");Directory.CreateDirectory(folder);match=FindAnyObjectByType<BaseJumpMatch>();if(match==null){LWS.TruckSports.SportsNavigation.LoadSport(LWS.TruckSports.SportsNavigation.BaseJump);yield return null;match=FindAnyObjectByType<BaseJumpMatch>();}yield return null;existed=LWS.TruckSports.SportsPrefs.HasKey(match.PreferenceKey);saved=LWS.TruckSports.SportsPrefs.GetString(match.PreferenceKey);Application.logMessageReceived+=Log;for(int i=0;i<4;i++){match.setup.players[i].source=BowlingSource.AI;match.setup.players[i].difficulty=(BowlingDifficulty)(i%3);}match.StartMatch();firstRotor=match.transport.model.transform.Find("Front_Rotor").localRotation;float end=Time.realtimeSinceStartup+240;float next=0;bool paused=false;int activePeak=0;while(!match.Finished&&Time.realtimeSinceStartup<end){
  activePeak=Mathf.Max(activePeak,System.Array.FindAll(match.skyField.bodies,b=>b.gameObject.activeSelf).Length);if(match.Phase==BaseJumpPhase.Flight&&match.AirTime>4&&!paused){paused=true;match.Pause();var position=match.jumpers[0].truck.body.position;float clock=match.AirTime;yield return new WaitForSecondsRealtime(.25f);bool frozen=match.Paused&&position==match.jumpers[0].truck.body.position&&clock==match.AirTime;report.AppendLine("PAUSE_FLIGHT preserved="+frozen);if(!frozen)errors++;match.Resume();}
  if(match.Phase==BaseJumpPhase.Flight){for(int i=0;i<4;i++){var j=match.jumpers[i];if(!j.Released){var shape=j.truck.shape;foreach(var c in match.transport.model.GetComponentsInChildren<MeshCollider>())if(Physics.ComputePenetration(shape,j.truck.body.position,j.truck.body.rotation,c,c.transform.position,c.transform.rotation,out _,out float distance)&&distance>.03f){if(clips<10)report.AppendLine("CARGO_INTERSECTION slot="+i+" p="+j.truck.body.position+" depth="+distance+" body="+c.name);clips++;}}if(j.Released&&!released[i]){released[i]=true;exitTimes[i]=Time.time;report.AppendLine("EXIT slot="+i+" p="+j.truck.body.position+" personalTime="+j.FlightTime+" velocity="+j.truck.body.linearVelocity);}}
   if(match.AirTime>12&&next==0){next=1;if(!Application.isEditor)ScreenCapture.CaptureScreenshot(folder+"/native-flight.png");}}
  yield return null;
 }
 report.AppendLine("SKY activePeak="+activePeak+" pool="+match.skyField.bodies.Length+" explosionsFinalRound="+match.skyField.Explosions);report.AppendLine("Finished="+match.Finished+" rounds="+match.RoundLocks+" landingLocks="+match.LandingLocks+" scores="+string.Join(",",match.totals)+" errors="+errors+" cargoPenetrationSamples="+clips+" rotorMoved="+(Quaternion.Angle(firstRotor,match.transport.model.transform.Find("Front_Rotor").localRotation)>.1f));for(int i=0;i<4;i++)report.AppendLine("RESULT "+i+" "+match.jumpers[i].Outcome+" "+match.jumpers[i].Landing.valid+" errorMM="+match.jumpers[i].Landing.errorMillimetres);File.WriteAllText(folder+"/native-probe.txt",report.ToString());Debug.Log("TRANSPORT_NATIVE_PROBE_COMPLETE "+report);bool complete=match.Finished&&match.LandingLocks>=12;Restore();if(!Application.isEditor)Application.Quit(errors==0&&complete&&clips==0?0:2);
 }
 void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;report.AppendLine("ERROR "+message);}}
 void Restore(){if(restored)return;restored=true;if(match!=null){match.SetupScreen();if(existed)LWS.TruckSports.SportsPrefs.SetString(match.PreferenceKey,saved);else LWS.TruckSports.SportsPrefs.DeleteKey(match.PreferenceKey);LWS.TruckSports.SportsPrefs.Save();}Application.logMessageReceived-=Log;Time.timeScale=1;}
 void OnDestroy(){Restore();}
#endif
}
}
