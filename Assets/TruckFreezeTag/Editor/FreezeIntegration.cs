using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using LWS.TruckSports.Editor;
namespace LWS.TruckFreezeTag.Editor
{
    public static class FreezeIntegration
    {
        public static void Menu(){Prepare();SportsNewEntriesAuthoring.Install();}
        public static void Prepare()
        {
            const string folder="Logs/TruckSports/EighteenSports/Freeze";
            if(!File.Exists(folder+"/result.txt")||!File.ReadAllText(folder+"/result.txt").Contains("ERRORS=0;")||!File.ReadAllText(folder+"/result.txt").Contains("PASS actual default four-round AI match reaches final team standings")||!File.ReadAllText(folder+"/result.txt").Contains("PASS teammate physically drives into ice, shatters it once and restores actual NWH control"))throw new InvalidOperationException("Pass the actual full physical freeze tag tournament before installing its menu card.");
            string source=folder+"/gameplay-1080.png";if(!File.Exists(source))throw new InvalidOperationException("Capture actual freeze tag gameplay first.");
            string preview="Assets/TruckSports/Art/Previews/TruckFreezeTag.png";File.Copy(source,preview,true);AssetDatabase.ImportAsset(preview,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(preview);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.mipmapEnabled=false;importer.maxTextureSize=1024;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
            var catalogue=JsonUtility.FromJson<SportsSportLogoAuthoring.Catalogue>(File.ReadAllText(SportsSportLogoAuthoring.InfoPath));
            if(!catalogue.entries.Any(e=>e.scene==FreezeAuthoring.Folder))catalogue.entries=catalogue.entries.Concat(new[]{new SportsSportLogoAuthoring.Information{scene=FreezeAuthoring.Folder,title="TRUCK FREEZE TAG",players="1–4 LOCAL HUMANS · ASYMMETRIC4v4 · ALTERNATING ROLES"}}).ToArray();
            File.WriteAllText(SportsSportLogoAuthoring.InfoPath,JsonUtility.ToJson(catalogue,true));AssetDatabase.ImportAsset(SportsSportLogoAuthoring.InfoPath);Debug.Log("FREEZE_MENU_INTEGRATED actual tournament capture and original grungy PNG");
        }
    }
}
