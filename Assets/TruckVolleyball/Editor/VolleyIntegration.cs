using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using LWS.TruckSports.Editor;
namespace LWS.TruckVolleyball.Editor
{
    public static class VolleyIntegration
    {
        public static void Menu(){Prepare();SportsNewEntriesAuthoring.Install();}
        public static void MenuWithFreeze(){Prepare();LWS.TruckFreezeTag.Editor.FreezeIntegration.Prepare();SportsNewEntriesAuthoring.Install();}
        public static void Prepare()
        {
            const string folder="Logs/TruckSports/EighteenSports/Volley";
            if(!File.Exists(folder+"/result.txt")||!File.ReadAllText(folder+"/result.txt").Contains("ERRORS=0;")||!File.ReadAllText(folder+"/result.txt").Contains("PASS actual default first-to-fifteen AI volleyball match completes under win-by-two/cap rules")||!File.ReadAllText(folder+"/result.txt").Contains("PASS AI physically returns and jumps for the actual inflatable ball"))throw new InvalidOperationException("Pass the actual full physical beach volleyball tournament before installing its menu card.");
            string source=folder+"/gameplay-1080.png";if(!File.Exists(source))throw new InvalidOperationException("Capture actual beach volleyball gameplay first.");
            string preview="Assets/TruckSports/Art/Previews/TruckVolleyball.png";File.Copy(source,preview,true);AssetDatabase.ImportAsset(preview,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(preview);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.mipmapEnabled=false;importer.maxTextureSize=1024;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
            var catalogue=JsonUtility.FromJson<SportsSportLogoAuthoring.Catalogue>(File.ReadAllText(SportsSportLogoAuthoring.InfoPath));
            if(!catalogue.entries.Any(e=>e.scene==VolleyAuthoring.Folder))catalogue.entries=catalogue.entries.Concat(new[]{new SportsSportLogoAuthoring.Information{scene=VolleyAuthoring.Folder,title="TRUCK VOLLEYBALL",players="1–4 LOCAL HUMANS · 2v2 / 3v3 / 4v4 · FIRST TO15"}}).ToArray();
            File.WriteAllText(SportsSportLogoAuthoring.InfoPath,JsonUtility.ToJson(catalogue,true));AssetDatabase.ImportAsset(SportsSportLogoAuthoring.InfoPath);Debug.Log("VOLLEY_MENU_INTEGRATED actual rally capture and original grungy PNG");
        }
    }
}
