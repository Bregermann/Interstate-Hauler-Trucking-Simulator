using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace LWS.TruckPedestrianPlowing.Editor {
 [InitializeOnLoad]public static class PlowEditorJobs {
  const string Request="Logs/TruckSports/Plowing/author.request";
  static PlowEditorJobs(){EditorApplication.update+=Tick;}
  static void Tick(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlaying||!File.Exists(Request))return;string job;try{job=File.ReadAllText(Request).Trim();File.Delete(Request);}catch(IOException){return;}try{if(job.StartsWith("play")){UnityEditor.SceneManagement.EditorSceneManager.OpenScene(PlowAuthoring.Scene);SessionState.SetBool("LWS.PlowProbe",true);SessionState.SetString("LWS.PlowMode",job.Length>5?job.Substring(5):"smoke");EditorApplication.isPlaying=true;}else if(job=="front"){LWS.TruckSports.Editor.SportsExpansionFront.Upgrade();LWS.TruckSports.Editor.SportsExpansionFront.Validate();}else if(job=="polish")PlowPolishAuthoring.Install();else if(job=="prepare"){PlowBadgesAuthoring.Install();PlowCabAuthoring.Create();LWS.TruckSports.Editor.SportsExpansionFront.Upgrade();PlowFixtures.Run();}else if(job=="badges")PlowBadgesAuthoring.Install();else if(job=="cab")PlowCabAuthoring.Create();else if(job=="build")LWS.TruckSports.Editor.SportsBuilder.Build();else if(job=="validate"){PlowFixtures.Run();PlowAuthoring.Validate();}else PlowAuthoring.Create();File.WriteAllText("Logs/TruckSports/Plowing/author-result.txt","PASS "+job+" "+DateTime.UtcNow.ToString("O"));}catch(Exception e){Debug.LogException(e);File.WriteAllText("Logs/TruckSports/Plowing/author-result.txt","FAIL "+job+" "+e);}}
 }
}
