using System;
using System.IO;
using UnityEditor;
namespace LWS.TruckSports.Editor {
[InitializeOnLoad]public static class SportsPresentationJobs {
 const string Folder="Logs/TruckSports/PresentationSprint";
 public static void ApplyPokerPresentation(){LWS.TruckPoker.Editor.PokerRestaurantDressing.Upgrade();LWS.TruckPoker.Editor.PokerTabletopDealerAuthoring.Upgrade();System.IO.Directory.CreateDirectory(Folder);System.IO.File.WriteAllText(Folder+"/poker-authoring.txt","PASS "+DateTime.UtcNow.ToString("o"));}
 static SportsPresentationJobs(){EditorApplication.update+=Tick;}
 static void Tick(){string request=Folder+"/request.txt";if(EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(request))return;string command=File.ReadAllText(request).Trim();File.Delete(request);Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/job.txt","RUNNING "+command+" "+DateTime.UtcNow.ToString("o"));try{
  if(command=="integrate"){LWS.TruckDockDiving.Editor.DockSlangAuthoring.Upgrade();LWS.TruckPoker.Editor.PokerRestaurantDressing.Upgrade();SportsSportLogoAuthoring.UpgradeAll();}
  else if(command=="menu-integrate"){SportsRetroMenuAuthoring.Upgrade();}
  else if(command=="menu-info"){SportsSportLogoAuthoring.RefreshInformation();}
  else if(command=="dock-play"){UnityEditor.SessionState.SetBool("Sports.Dock.Slang",true);EditorApplication.isPlaying=true;}
  else if(command=="poker-play"){UnityEditor.SessionState.SetBool("Sports.Poker.Dealer",true);EditorApplication.isPlaying=true;}
  else if(command=="menu-play"){UnityEditor.SceneManagement.EditorSceneManager.OpenScene(SportsBuilder.FrontScene);UnityEditor.SessionState.SetBool("Sports.MenuPresentation.Probe",true);EditorApplication.isPlaying=true;}
  else throw new InvalidOperationException("Unknown presentation job "+command);
  File.WriteAllText(Folder+"/job.txt","PASS "+command+" "+DateTime.UtcNow.ToString("o"));
 }catch(Exception e){File.WriteAllText(Folder+"/job.txt","FAIL "+command+"\n"+e);UnityEngine.Debug.LogException(e);}}
}
}
