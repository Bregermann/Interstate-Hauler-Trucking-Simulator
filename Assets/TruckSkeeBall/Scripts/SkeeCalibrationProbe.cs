using System;using System.IO;using System.Text;using System.Collections;using UnityEngine;using UnityEngine.InputSystem;using UnityEngine.InputSystem.LowLevel;using LWS.TruckSports;using LWS.TruckBowling;
namespace LWS.TruckSkeeBall {public sealed class SkeeCalibrationProbe:MonoBehaviour {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
 [RuntimeInitializeOnLoadMethod]static void Boot(){bool run=Array.IndexOf(Environment.GetCommandLineArgs(),"-tsSkeeCalibration")>=0;
#if UNITY_EDITOR
 run|=UnityEditor.SessionState.GetBool("LWS.SkeeCalibration",false);UnityEditor.SessionState.SetBool("LWS.SkeeCalibration",false);
#endif
 if(run){var g=new GameObject("Owned actual-ramp velocity calibration through injected keyboard controls");DontDestroyOnLoad(g);if(!SportsProbeInputIsolation.Active)g.AddComponent<SportsProbeInputIsolation>();g.AddComponent<SkeeCalibrationProbe>();}}
 SkeeMatch m;Keyboard keys;string folder,key,saved;bool had,restored;int errors;InputSettings.BackgroundBehavior background;readonly StringBuilder log=new StringBuilder();
#if UNITY_EDITOR
 InputSettings.EditorInputBehaviorInPlayMode editorInput;
#endif
 void Error(string text,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;log.AppendLine("ERROR "+text);}}
 IEnumerator Start(){folder=SportsEvidencePaths.Folder("SkeeCalibrationEvidence");background=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
 editorInput=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
 keys=InputSystem.AddDevice<Keyboard>();yield return SportsProbeScene.Ensure<SkeeMatch>("TruckSkeeBall");m=FindAnyObjectByType<SkeeMatch>();yield return null;key=m.PreferenceKey;had=SportsPrefs.HasKey(key);saved=SportsPrefs.GetString(key);Application.logMessageReceived+=Error;foreach(float desired in new[]{18f,20,22,24,26,28,30,32,34}){m.SetupScreen();m.option=0;m.setup.count=1;m.NormalizeSetup();m.AllBots();m.setup.players[0].source=BowlingSource.Keyboard;m.StartMatch();InputSystem.QueueStateEvent(keys,new KeyboardState());Time.timeScale=3;var r=m.courses[0];float deadline=Time.realtimeSinceStartup+45,entry=0;bool launch=false;while(Time.realtimeSinceStartup<deadline&&r.Phase!=SkeePhase.Result&&r.Phase!=SkeePhase.Replay){if(r.Phase==SkeePhase.Approach){float speed=Vector3.Dot(r.truck.body.linearVelocity,r.truck.body.rotation*Vector3.forward);InputSystem.QueueStateEvent(keys,speed<desired-.25f?new KeyboardState(Key.W):speed>desired+.6f?new KeyboardState(Key.S):new KeyboardState());if(r.truck.body.position.z<32)entry=speed;}else InputSystem.QueueStateEvent(keys,new KeyboardState());if(r.Phase==SkeePhase.Flight&&!launch){launch=true;var p=r.LaunchPosition;var v=r.LaunchVelocity;var n=r.target.forward;float d=Vector3.Dot(p-r.target.position,n),b=Vector3.Dot(v,n),a=.5f*Vector3.Dot(Physics.gravity,n);float disc=b*b-4*a*d;float t=disc>=0?(-b+Mathf.Sqrt(disc))/(2*a):-1;var point=p+v*t+Physics.gravity*(.5f*t*t);var local=r.target.InverseTransformPoint(point);log.AppendLine("MEASURE desired="+desired+" entry="+entry+" launchPosition="+p.ToString("F4")+" launchVelocity="+v.ToString("F4")+" angle="+(Mathf.Atan2(v.y,new Vector2(v.x,v.z).magnitude)*Mathf.Rad2Deg)+" planeTime="+t+" boardLocal="+local.ToString("F4"));File.WriteAllText(folder+"/progress.txt",log.ToString());}yield return null;}if(!launch){errors++;log.AppendLine("FAIL no natural ramp launch desired="+desired+" phase="+r.Phase);}log.AppendLine("RESULT desired="+desired+" score="+r.Award+" aftertouch="+r.touch.Spent);Time.timeScale=1;}Restore();log.AppendLine("ERRORS="+errors+". Injected keyboard throttle/brake feedback, real NWH/EasyRoads flight; analytic plane intersections only diagnose the recorded release and do not steer/score the truck.");File.WriteAllText(folder+"/result.txt",log.ToString());if(!Application.isEditor)Application.Quit(errors==0?0:2);}
 void Restore(){if(restored)return;restored=true;Application.logMessageReceived-=Error;if(m!=null)m.SetupScreen();if(key!=null){if(had)SportsPrefs.SetString(key,saved);else SportsPrefs.DeleteKey(key);SportsPrefs.Save();}InputSystem.settings.backgroundBehavior=background;
#if UNITY_EDITOR
 InputSystem.settings.editorInputBehaviorInPlayMode=editorInput;
#endif
 if(keys!=null&&keys.added)InputSystem.RemoveDevice(keys);Time.timeScale=1;}void OnDestroy(){Restore();}
#endif
}}
