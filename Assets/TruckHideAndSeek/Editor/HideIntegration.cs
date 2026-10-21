using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using LWS.TruckSports.Editor;
namespace LWS.TruckHideAndSeek.Editor
{
 public static class HideIntegration
 {
  public static void Menu()
  {
   const string log="Logs/TruckSports/EighteenSports/Hide/";string proof=File.Exists(log+"result.txt")?File.ReadAllText(log+"result.txt"):"";if(!proof.Contains("ERRORS=0;")||!proof.Contains("PASS full normal hiding and seeking round completes")||!proof.Contains("PASS actual body contact tags exactly once"))throw new InvalidOperationException("Pass real Hide & Seek runtime acceptance before installing its menu card.");
   string preview="Assets/TruckSports/Art/Previews/TruckHideAndSeek.png";File.Copy(log+"gameplay-1080.png",preview,true);AssetDatabase.ImportAsset(preview,ImportAssetOptions.ForceSynchronousImport);var importer=(TextureImporter)AssetImporter.GetAtPath(preview);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.mipmapEnabled=false;importer.maxTextureSize=1024;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
   var data=JsonUtility.FromJson<SportsSportLogoAuthoring.Catalogue>(File.ReadAllText(SportsSportLogoAuthoring.InfoPath));if(!data.entries.Any(e=>e.scene==HideAuthoring.Folder))data.entries=data.entries.Concat(new[]{new SportsSportLogoAuthoring.Information{scene=HideAuthoring.Folder,title="TRUCK HIDE & SEEK",players="1–4 LOCAL HUMANS · EIGHT TRUCKS · ONE ORIGINAL SEEKER"}}).ToArray();File.WriteAllText(SportsSportLogoAuthoring.InfoPath,JsonUtility.ToJson(data,true));AssetDatabase.ImportAsset(SportsSportLogoAuthoring.InfoPath);SportsNewEntriesAuthoring.Install();
  }
 }
}
