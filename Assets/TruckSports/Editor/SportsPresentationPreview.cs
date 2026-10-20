using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace LWS.TruckSports.Editor {
// Explicit runtime screenshot authoring only. No scene regeneration or normal
// build hook. Captures are labeled controlled Editor fixtures in provenance.
[InitializeOnLoad]public static class SportsPresentationPreview {
 const string Key="Sports.Presentation.Preview";static int stage;static float deadline;static IDisposable preferences;
 static SportsPresentationPreview(){EditorApplication.update+=Tick;}
 public static void Dock(){Begin("Dock",LWS.TruckDockDiving.Editor.DockAuthoring.Scene);}
 public static void Poker(){Begin("Poker",LWS.TruckPoker.Editor.PokerAuthoring.Scene);}
 static void Begin(string kind,string scene){EditorSceneManager.OpenScene(scene);SessionState.SetBool(Key+".captured",false);SessionState.SetString(Key,kind);EditorApplication.isPlaying=true;}
 static void Tick(){string kind=SessionState.GetString(Key,"");if(kind.Length==0)return;
  if(SessionState.GetBool(Key+".captured",false)&&!EditorApplication.isPlayingOrWillChangePlaymode){SessionState.EraseString(Key);SessionState.EraseBool(Key+".captured");preferences?.Dispose();EditorApplication.Exit(0);return;}
  if(!EditorApplication.isPlaying)return;
  try{
   var match=UnityEngine.Object.FindAnyObjectByType<SportsEventSession>();if(match==null)return;
   if(stage==0){new GameObject("Preview input isolation").AddComponent<SportsProbeInputIsolation>();preferences=SportsPrefs.IsolateValidation("PresentationPreview");SportsParticipantRegistry.ForgetCache();match.setup.count=kind=="Poker"?8:2;match.AllBots();if(!match.StartMatch())throw new InvalidOperationException("Preview match did not start");deadline=Time.realtimeSinceStartup+45;stage=1;return;}
   bool ready=kind=="Poker"?((LWS.TruckPoker.PokerMatch)match).dealerDrive.DeliveredCards>=3:match.GetComponent<LWS.TruckDockDiving.DockSlang>().group.alpha>.9f;
   if(!ready){if(Time.realtimeSinceStartup>deadline)throw new TimeoutException(kind+" runtime preview");return;}
   string path="Logs/TruckSports/PresentationSprint/PreviewSources/Truck"+(kind=="Poker"?"Poker":"DockDiving")+".png";
   SportsControlledCapture.Save(match,1280,720,path);File.WriteAllText(path+".txt","Actual saved-scene runtime; controlled Editor camera/UI fixture. No player-frame/performance claim.");match.SetupScreen();stage=2;SessionState.SetBool(Key+".captured",true);EditorApplication.isPlaying=false;
  }catch(Exception e){SessionState.EraseString(Key);Debug.LogException(e);preferences?.Dispose();EditorApplication.Exit(2);}
 }
}
}
