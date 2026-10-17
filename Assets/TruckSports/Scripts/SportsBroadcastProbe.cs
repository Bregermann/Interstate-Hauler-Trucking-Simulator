using System;
using System.IO;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LWS.TruckBowling;
namespace LWS.TruckSports {
public sealed class SportsBroadcastProbe:MonoBehaviour {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
 [RuntimeInitializeOnLoadMethod]static void Boot(){bool run=Array.IndexOf(Environment.GetCommandLineArgs(),"-tsBroadcast")>=0;bool before=false;
#if UNITY_EDITOR
 run|=UnityEditor.SessionState.GetBool("LWS.BroadcastProbe",false);before=UnityEditor.SessionState.GetBool("LWS.BroadcastBefore",false);UnityEditor.SessionState.SetBool("LWS.BroadcastProbe",false);
#endif
 if(run){var g=new GameObject("Owned broadcast layout acceptance");DontDestroyOnLoad(g);if(!SportsProbeInputIsolation.Active)g.AddComponent<SportsProbeInputIsolation>();g.AddComponent<SportsBroadcastProbe>().before=before;}}
 bool before;string folder;int errors;System.Text.StringBuilder log=new System.Text.StringBuilder();
 void Check(bool ok,string label){log.AppendLine((ok?"PASS ":"FAIL ")+label);if(!ok)errors++;File.WriteAllText(folder+"/progress.txt",log.ToString());}
 void Error(string text,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;log.AppendLine("ERROR "+text);}}
 IEnumerator Start(){folder=SportsEvidencePaths.Folder(before?"BroadcastBefore":"BroadcastAfter");Application.logMessageReceived+=Error;foreach(var name in new[]{SportsNavigation.Minefield,SportsNavigation.Swing,"TruckWifeCarrying","TruckTugOfWar"}){SportsNavigation.LoadSport(name);yield return null;yield return null;var match=FindAnyObjectByType<SportsEventSession>();match.setup.count=8;if(match.TeamSport)match.teamA=match.teamB=4;for(int i=0;i<8;i++){match.setup.players[i].source=BowlingSource.AI;match.setup.players[i].name="Long Racer Name "+(i+1);}Check(match.StartMatch(),name+" maximum AI roster starts");for(int f=0;f<12;f++)yield return null;yield return new WaitForSecondsRealtime(.35f);Time.timeScale=0;if(match is LWS.TruckWifeCarrying.WifeMatch){var cards=FindAnyObjectByType<SportsBroadcastScores>();Check(cards!=null&&cards.OwnsStandings&&match.ScoreCardCount==4&&!match.Standings.Contains("DNF"),"paired team identities use four readable score cards and opening status is READY");}foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(1440,900),new Vector2Int(1280,960),new Vector2Int(2560,1080)}){if(!Application.isEditor){Screen.SetResolution(size.x,size.y,false);yield return new WaitForSecondsRealtime(.35f);ScreenCapture.CaptureScreenshot(folder+"/"+name+"-"+size.x+"x"+size.y+"-window.png");yield return new WaitForSecondsRealtime(.15f);}Capture(match,size.x,size.y);Check(FindObjectsByType<AudioListener>(FindObjectsInactive.Include).Length==1,"one listener "+name+" "+size);}
 match.SetupScreen();Time.timeScale=1;}SportsNavigation.FrontEnd();yield return null;yield return null;Check(FindAnyObjectByType<SportsFrontEnd>()!=null,"clean collection return");log.AppendLine("ERRORS="+errors+". Native camera+converted-canvas captures are controlled render fixtures; only -window captures from an actual Windows player include normal overlay presentation. No hardware or human acceptance.");File.WriteAllText(folder+"/result.txt",log.ToString());if(!Application.isEditor)Application.Quit(errors==0?0:2);}
 void Capture(SportsEventSession match,int width,int height){var camera=match.cameras[0];var canvas=match.menus.GetComponent<Canvas>();var originalMode=canvas.renderMode;var originalCamera=canvas.worldCamera;float originalDistance=canvas.planeDistance;var originalTarget=camera.targetTexture;var originalRect=camera.rect;var active=RenderTexture.active;var rt=RenderTexture.GetTemporary(width,height,24);camera.targetTexture=rt;camera.rect=new Rect(0,0,1,1);canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=Mathf.Max(.5f,camera.nearClipPlane+.2f);Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;var image=new Texture2D(width,height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();File.WriteAllBytes(folder+"/"+match.GetType().Name+"-"+width+"x"+height+"-controlled-ui.png",image.EncodeToPNG());Destroy(image);if(!before){var points=new Vector3[4];foreach(var label in new[]{match.menus.header,match.menus.standings}){if(!label.gameObject.activeInHierarchy)continue;label.rectTransform.GetWorldCorners(points);bool inside=true;foreach(var p in points){var screen=camera.WorldToViewportPoint(p);inside&=screen.x>=-.002f&&screen.x<=1.002f&&screen.y>=-.002f&&screen.y<=1.002f;}Check(inside,"anchored critical label stays inside viewport "+match.GetType().Name+" "+label.name+" "+width+"x"+height);}}
 canvas.renderMode=originalMode;canvas.worldCamera=originalCamera;canvas.planeDistance=originalDistance;camera.targetTexture=originalTarget;camera.rect=originalRect;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);}
 void OnDestroy(){Application.logMessageReceived-=Error;Time.timeScale=1;}
#endif
}
}
