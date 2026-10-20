#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using LWS.TruckSports;
using LWS.TruckBowling;
namespace LWS.TruckSnake {
// Explicit deterministic obstruction fixture. Normal scene loading never runs it.
public sealed class SnakeAvoidanceProbe:MonoBehaviour {
 [RuntimeInitializeOnLoadMethod]static void Boot(){bool run=Array.IndexOf(Environment.GetCommandLineArgs(),"-tsSnakeAvoidance")>=0;
#if UNITY_EDITOR
  run|=UnityEditor.SessionState.GetBool("Sports.Snake.Avoidance",false);UnityEditor.SessionState.SetBool("Sports.Snake.Avoidance",false);
#endif
  if(!run)return;var go=new GameObject("Explicit Snake physical obstruction acceptance");DontDestroyOnLoad(go);go.AddComponent<SportsProbeInputIsolation>();go.AddComponent<SnakeAvoidanceProbe>();
 }
 string folder;IDisposable prefs;int checks,errors;readonly StringBuilder log=new StringBuilder();
 void Check(bool value,string label){checks++;if(!value)errors++;log.AppendLine((value?"PASS ":"FAIL ")+label);File.WriteAllText(folder+"/progress.txt",log.ToString());}
 void Error(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;log.AppendLine("ERROR "+message+"\n"+stack);}}
 IEnumerator Start(){
  folder=Path.GetFullPath("Logs/TruckSports/EssentialRelease/Snake/"+(Application.isEditor?"Native":"Windows")+"/Avoidance");Directory.CreateDirectory(folder);File.WriteAllText(folder+"/result.txt","RUNNING "+DateTime.UtcNow.ToString("o"));
  prefs=SportsPrefs.IsolateValidation("SnakeAvoidance");SportsParticipantRegistry.ForgetCache();Application.logMessageReceived+=Error;
  yield return SportsProbeScene.Ensure<SnakeMatch>("TruckSnake");yield return null;var m=FindAnyObjectByType<SnakeMatch>();
  foreach(var entry in m.presets.entries)if(entry.displayName=="Clear"){m.presets.Select(entry.id);break;}
  m.setup.count=1;m.AllBots();m.option=2;m.pickupRespawnInterval=10000;
  var start=new Vector3(-1120,m.world.Height(new Vector3(-1120,0,-1000))+.5f,-1000);m.world.starts[0]=start;m.world.rotations[0]=Quaternion.Euler(0,90,0);
  var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="Explicit test-only solid road obstruction";wall.transform.position=new Vector3(-1010,m.world.Height(new Vector3(-1010,0,-1000))+3.5f,-1000);wall.transform.localScale=new Vector3(4,7,10);
  for(int difficulty=0;difficulty<3;difficulty++){
   m.SetupScreen();m.setup.players[0].difficulty=(BowlingDifficulty)difficulty;UnityEngine.Random.InitState(62071);Check(m.StartMatch(),"single "+(BowlingDifficulty)difficulty+" NWH competitor starts with one trailer");
   foreach(var gate in m.pickups)gate.Dormant();var destination=new Vector3(-850,m.world.Height(new Vector3(-850,0,-1000))+.18f,-1000);m.pickups[0].Golden=false;m.pickups[0].Spawn(destination);
   typeof(SnakeMatch).GetField("nextPickup",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(m,10000f);Physics.SyncTransforms();
   float deadline=Time.realtimeSinceStartup+38,side=0,next=0;Time.timeScale=2;bool passed=false;
   while(m.rules.Playing(0)&&Time.realtimeSinceStartup<deadline&&m.rules.Clock<45){
    side=Mathf.Max(side,Mathf.Abs(m.trucks[0].body.position.z+1000));
    if(m.trucks[0].body.position.x>wall.transform.position.x+25){passed=true;break;}
    if(Time.realtimeSinceStartup>=next){next=Time.realtimeSinceStartup+2;log.AppendLine("LIVE "+(BowlingDifficulty)difficulty+" p="+m.trucks[0].body.position+" v="+m.trucks[0].body.linearVelocity+" "+m.drivers[0].RouteDiagnostic);File.WriteAllText(folder+"/progress.txt",log.ToString());}
    yield return null;
   }
   Time.timeScale=1;Check(passed&&m.rules.Playing(0),"actual "+(BowlingDifficulty)difficulty+" tractor AND starter trailer pass the solid obstruction without privileged movement · "+m.Standings);
   Check(side<14,"avoidance stays within the existing thirty-metre road width · maximum lateral="+side.ToString("F2"));
   SportsControlledCapture.Save(m,1280,720,folder+"/obstruction-"+difficulty+".png");m.SetupScreen();yield return null;
  }
  Destroy(wall);SportsNavigation.FrontEnd();yield return null;yield return null;Check(FindAnyObjectByType<SnakeMatch>()==null,"menu exit cleans independent convoy, fixture and AI state");
  log.AppendLine("CHECKS="+checks+" ERRORS="+errors+" "+DateTime.UtcNow.ToString("o")+". Real NWH driving/PhysX; explicitly placed static fixture and pickup. No human or hardware testing.");File.WriteAllText(folder+"/result.txt",log.ToString());Application.logMessageReceived-=Error;
#if UNITY_EDITOR
  if(Application.isEditor)UnityEditor.EditorApplication.isPlaying=false;else
#endif
  Application.Quit(errors==0?0:2);
 }
 void OnDestroy(){Application.logMessageReceived-=Error;prefs?.Dispose();SportsParticipantRegistry.ForgetCache();Time.timeScale=1;}
}
}
#endif
