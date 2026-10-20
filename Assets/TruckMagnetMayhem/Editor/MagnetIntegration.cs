using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using LWS.TruckSports.Editor;
namespace LWS.TruckMagnetMayhem.Editor
{
    public static class MagnetIntegration
    {
        public static void Menu()
        {
            const string folder="Logs/TruckSports/EighteenSports/Magnet";
            if(!File.Exists(folder+"/result.txt")||!File.ReadAllText(folder+"/result.txt").Contains("ERRORS=0;")||!File.ReadAllText(folder+"/result.txt").Contains("PASS actual three-minute AI match reaches final standings without a softlock")||!File.ReadAllText(folder+"/result.txt").Contains("PASS AI physically hauls and settles metal; every point comes from an actual bay deposit"))throw new InvalidOperationException("Pass the actual full physical magnetic scrapyard tournament before installing its menu card.");
            string source=folder+"/gameplay-1080.png";if(!File.Exists(source))throw new InvalidOperationException("Capture actual magnetic scrapyard gameplay first.");
            string preview="Assets/TruckSports/Art/Previews/TruckMagnetMayhem.png";File.Copy(source,preview,true);AssetDatabase.ImportAsset(preview,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(preview);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.mipmapEnabled=false;importer.maxTextureSize=1024;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
            var catalogue=JsonUtility.FromJson<SportsSportLogoAuthoring.Catalogue>(File.ReadAllText(SportsSportLogoAuthoring.InfoPath));
            if(!catalogue.entries.Any(e=>e.scene==MagnetAuthoring.Folder))catalogue.entries=catalogue.entries.Concat(new[]{new SportsSportLogoAuthoring.Information{scene=MagnetAuthoring.Folder,title="TRUCK MAGNET MAYHEM",players="1–4 LOCAL HUMANS · UP TO8TRUCKS · MAGNETIC SCRAP HEIST"}}).ToArray();
            File.WriteAllText(SportsSportLogoAuthoring.InfoPath,JsonUtility.ToJson(catalogue,true));AssetDatabase.ImportAsset(SportsSportLogoAuthoring.InfoPath);SportsNewEntriesAuthoring.Install();Debug.Log("JOUST_MENU_INTEGRATED actual tournament capture and original grungy PNG");
        }
    }
}
