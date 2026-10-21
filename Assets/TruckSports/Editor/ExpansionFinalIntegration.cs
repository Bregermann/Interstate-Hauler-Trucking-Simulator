using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
namespace LWS.TruckSports.Editor
{
 public static class ExpansionFinalIntegration
 {
  public static void Dependencies(){string[] scenes={"Assets/TruckFrogger/Scenes/TruckFrogger.unity","Assets/TruckAvalancheEscape/Scenes/TruckAvalancheEscape.unity","Assets/TruckNinjaWarrior/Scenes/TruckNinjaWarrior.unity"};var paths=AssetDatabase.GetDependencies(scenes,true).Where(p=>p.StartsWith("Assets/",StringComparison.Ordinal)).Distinct().OrderBy(p=>p).ToArray();Directory.CreateDirectory("Logs/TruckSports/EighteenSports");File.WriteAllLines("Logs/TruckSports/EighteenSports/final-three-asset-dependencies.txt",paths);UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenes[1]);var world=UnityEngine.Object.FindAnyObjectByType<LWS.TruckAvalancheEscape.AvalancheWorld>();var lines=new System.Collections.Generic.List<string>();foreach(var start in world.starts)lines.Add("start="+start+" actual terrain="+world.Height(start));for(int z=-105;z<=20;z+=5)foreach(float x in new[]{-20f,-6.5f,6.5f,20f})lines.Add("surface="+new Vector3(x,0,z)+" terrain="+world.Height(new Vector3(x,0,z)));File.WriteAllLines("Logs/TruckSports/EighteenSports/avalanche-staging-surface-audit.txt",lines);}
  public static void Menu()
  {
   Add("TruckFrogger","Frog","TRUCK FROGGER","1–4 LOCAL HUMANS · EIGHT ALTERNATING DRIVERS · FIVE CHALLENGES","challenge-1-1080.png","PASS real AI driver physically completes Ultimate Crossing");
   Add("TruckAvalancheEscape","Avalanche","TRUCK AVALANCHE ESCAPE","1–4 LOCAL HUMANS · TWELVE TRUCKS · THREE MOUNTAIN ROUTES","mountain-1080.png","PASS actual AI follows a complete route and escapes the mountain");
   Add("TruckNinjaWarrior","Ninja","TRUCK NINJA WARRIOR","1–4 LOCAL HUMANS · EIGHT ALTERNATING ATHLETES · SIX OBSTACLES","course-1080.png","PASS AI physically clears all six obstacles and strikes the actual bell");
   SportsNewEntriesAuthoring.Install();
  }
  static void Add(string folder,string probe,string title,string players,string frame,string required)
  {
   string evidence="Logs/TruckSports/EighteenSports/"+probe+"/";string proof=File.ReadAllText(evidence+"result.txt");if(!proof.Contains("ERRORS=0;")||!proof.Contains(required))throw new InvalidOperationException("Real physical acceptance must pass before installing "+title);
   string preview="Assets/TruckSports/Art/Previews/"+folder+".png";File.Copy(evidence+frame,preview,true);AssetDatabase.ImportAsset(preview,ImportAssetOptions.ForceSynchronousImport);var importer=(TextureImporter)AssetImporter.GetAtPath(preview);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.mipmapEnabled=false;importer.maxTextureSize=1024;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
   var catalogue=JsonUtility.FromJson<SportsSportLogoAuthoring.Catalogue>(File.ReadAllText(SportsSportLogoAuthoring.InfoPath));if(!catalogue.entries.Any(e=>e.scene==folder))catalogue.entries=catalogue.entries.Concat(new[]{new SportsSportLogoAuthoring.Information{scene=folder,title=title,players=players}}).ToArray();File.WriteAllText(SportsSportLogoAuthoring.InfoPath,JsonUtility.ToJson(catalogue,true));AssetDatabase.ImportAsset(SportsSportLogoAuthoring.InfoPath);
  }
 }
}
