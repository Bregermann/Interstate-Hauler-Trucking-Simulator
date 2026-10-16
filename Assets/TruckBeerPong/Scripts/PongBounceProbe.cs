using System;using System.IO;using System.Text;using System.Collections;using UnityEngine;using LWS.TruckSports;using LWS.TruckBowling;
namespace LWS.TruckBeerPong {
public sealed class PongBounceProbe:MonoBehaviour {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
 [RuntimeInitializeOnLoadMethod]static void Boot(){bool run=Array.IndexOf(Environment.GetCommandLineArgs(),"-tsPongBounce")>=0;
#if UNITY_EDITOR
 run|=UnityEditor.SessionState.GetBool("LWS.PongBounce",false);UnityEditor.SessionState.SetBool("LWS.PongBounce",false);
#endif
 if(run){var g=new GameObject("Pong physical table rebound calibration");DontDestroyOnLoad(g);g.AddComponent<SportsProbeInputIsolation>();g.AddComponent<PongBounceProbe>();}}
 IEnumerator Start(){var folder=SportsEvidencePaths.Folder("PongBounceEvidence");var report=new StringBuilder();yield return SportsProbeScene.Ensure<PongMatch>("TruckBeerPong");var m=FindAnyObjectByType<PongMatch>();yield return null;bool good=false;foreach(float speed in new[]{2f,3f,4f,5f,6f,7f,8f,9f,10f,11f,12f}){m.SetupScreen();m.option=1;m.NormalizeSetup();m.AllBots();m.StartMatch();Time.timeScale=8;m.truck.Power(false);var b=m.truck.body;var p=new Vector3(0,8,14);b.position=p;b.rotation=Quaternion.identity;m.truck.transform.SetPositionAndRotation(p,Quaternion.identity);b.linearVelocity=new Vector3(0,-15,speed);b.angularVelocity=Vector3.zero;Physics.SyncTransforms();m.shot.ResetShot();m.shot.Arm();typeof(PongMatch).GetProperty("Phase").SetValue(m,PongPhase.Flight);float max=0;Vector3 bounce=Vector3.zero;bool seen=false;float end=Time.realtimeSinceStartup+12;while(!m.rules.ShotLocked&&Time.realtimeSinceStartup<end){if(m.shot.Bounced){if(!seen){seen=true;bounce=b.linearVelocity;}max=Mathf.Max(max,b.worldCenterOfMass.y);}yield return null;}report.AppendLine("BOUNCE speed="+speed+" firstReturn="+bounce+" peak="+max+" contacts="+m.shot.Contacts+" cup="+m.rules.LastCup+" reason="+m.Outcome+" end="+b.worldCenterOfMass);File.WriteAllText(folder+"/progress.txt",report.ToString());if(m.rules.LastHit&&m.shot.Bounced){good=true;break;}}m.SetupScreen();Time.timeScale=1;report.AppendLine("PHYSICAL_BOUNCE_SUCCESS="+good);report.AppendLine("ERRORS="+(good?0:1));File.WriteAllText(folder+"/result.txt",report.ToString());if(!Application.isEditor)Application.Quit(good?0:2);}
#endif
}
}
