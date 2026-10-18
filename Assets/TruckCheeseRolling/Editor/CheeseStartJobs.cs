using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace LWS.TruckCheeseRolling.Editor {
[InitializeOnLoad]public static class CheeseStartJobs {
 const string Folder="Logs/TruckSports/Party/Cheese/";
 static CheeseStartJobs(){EditorApplication.update+=Tick;}
 static void Tick(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlaying||!File.Exists(Folder+"job.request"))return;string job;try{job=File.ReadAllText(Folder+"job.request").Trim();File.Delete(Folder+"job.request");}catch(IOException){return;}
  try{if(job=="baseline"||job=="verify"){EditorSceneManager.OpenScene(CheeseAuthoring.Scene);SessionState.SetBool("Sports.CheeseStartProbe",true);SessionState.SetString("Sports.CheeseStartMode",job);EditorApplication.isPlaying=true;}else if(job=="author"){var scene=EditorSceneManager.OpenScene(CheeseAuthoring.Scene);CheeseRepairAuthoring.Install(UnityEngine.Object.FindAnyObjectByType<CheeseMatch>());EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();CheeseAuthoring.Validate();}else if(job=="build")LWS.TruckSports.Editor.SportsBuilder.Build();else throw new ArgumentException("Unknown explicit Cheese job "+job);File.WriteAllText(Folder+"job-result.txt","PASS "+job+" "+DateTime.UtcNow.ToString("O"));}catch(Exception e){Debug.LogException(e);File.WriteAllText(Folder+"job-result.txt","FAIL "+job+" "+e);}
 }
}
}
