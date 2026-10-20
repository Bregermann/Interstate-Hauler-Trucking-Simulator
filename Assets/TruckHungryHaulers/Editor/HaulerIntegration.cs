using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using LWS.TruckSports.Editor;
namespace LWS.TruckHungryHaulers.Editor
{
    public static class HaulerIntegration
    {
        public static void Menu(){Prepare();SportsNewEntriesAuthoring.Install();}
        public static void Prepare()
        {
            const string folder="Logs/TruckSports/EighteenSports/Hauler";
            if(!File.Exists(folder+"/result.txt")||!File.ReadAllText(folder+"/result.txt").Contains("ERRORS=0;")||!File.ReadAllText(folder+"/result.txt").Contains("PASS actual full AI cargo match reaches final shared/winning standings")||!File.ReadAllText(folder+"/result.txt").Contains("PASS all four AI physically scoop and bank their own cargo without automatic points"))throw new InvalidOperationException("Pass the actual full physical cargo match before installing its menu card.");
            string source=folder+"/gameplay-1080.png";if(!File.Exists(source))throw new InvalidOperationException("Capture actual cargo gameplay first.");
            string preview="Assets/TruckSports/Art/Previews/TruckHungryHaulers.png";File.Copy(source,preview,true);AssetDatabase.ImportAsset(preview,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(preview);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.mipmapEnabled=false;importer.maxTextureSize=1024;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
            var catalogue=JsonUtility.FromJson<SportsSportLogoAuthoring.Catalogue>(File.ReadAllText(SportsSportLogoAuthoring.InfoPath));
            if(!catalogue.entries.Any(e=>e.scene==HaulerAuthoring.Folder))catalogue.entries=catalogue.entries.Concat(new[]{new SportsSportLogoAuthoring.Information{scene=HaulerAuthoring.Folder,title="HUNGRY HUNGRY HAULERS",players="1–4 LOCAL HUMANS · SCOOP, HAUL AND BANK · FOUR TRUCKS"}}).ToArray();
            File.WriteAllText(SportsSportLogoAuthoring.InfoPath,JsonUtility.ToJson(catalogue,true));AssetDatabase.ImportAsset(SportsSportLogoAuthoring.InfoPath);Debug.Log("HAULER_MENU_INTEGRATED actual physical cargo capture and original grungy PNG");
        }
    }
}
