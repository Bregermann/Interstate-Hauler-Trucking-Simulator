using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using LWS.TruckSports.Editor;
namespace LWS.TruckSheepHerding.Editor
{
    public static class HerdIntegration
    {
        public static void Menu(){Prepare();SportsNewEntriesAuthoring.Install();}
        public static void MenuWithHaulers(){LWS.TruckHungryHaulers.Editor.HaulerIntegration.Prepare();Prepare();SportsNewEntriesAuthoring.Install();}
        public static void Prepare()
        {
            const string folder="Logs/TruckSports/EighteenSports/Herd";
            string proof=File.Exists(folder+"/result.txt")?File.ReadAllText(folder+"/result.txt"):"";
            if(!proof.Contains("ERRORS=0;")||!proof.Contains("PASS actual full timed farm competition reaches final standings")||!proof.Contains("PASS contested AI match genuinely herds multiple sheep through physical pen entries")||!Enumerable.Range(0,4).All(slot=>proof.Contains("PASS this AI slot genuinely drives and herds the whole animal into its assigned pen slot="+slot)))throw new InvalidOperationException("Pass the full actual farm competition AND all four physical AI capability cases before installing this card.");
            string source=folder+"/gameplay-1080.png";if(!File.Exists(source))throw new InvalidOperationException("Capture actual sheep herding gameplay first.");
            string preview="Assets/TruckSports/Art/Previews/TruckSheepHerding.png";File.Copy(source,preview,true);AssetDatabase.ImportAsset(preview,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(preview);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.mipmapEnabled=false;importer.maxTextureSize=1024;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
            var catalogue=JsonUtility.FromJson<SportsSportLogoAuthoring.Catalogue>(File.ReadAllText(SportsSportLogoAuthoring.InfoPath));
            if(!catalogue.entries.Any(e=>e.scene==HerdAuthoring.Folder))catalogue.entries=catalogue.entries.Concat(new[]{new SportsSportLogoAuthoring.Information{scene=HerdAuthoring.Folder,title="TRUCK SHEEP HERDING",players="1–4 LOCAL HUMANS · GENTLE HERDING · FOUR COLOR PENS"}}).ToArray();
            File.WriteAllText(SportsSportLogoAuthoring.InfoPath,JsonUtility.ToJson(catalogue,true));AssetDatabase.ImportAsset(SportsSportLogoAuthoring.InfoPath);Debug.Log("HERD_MENU_INTEGRATED actual farm competition capture and original grungy PNG");
        }
    }
}
