using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using LWS.TruckSports.Editor;
namespace LWS.TruckFloorIsLava.Editor
{
    public static class LavaIntegration
    {
        public static void Menu()
        {
            const string folder="Logs/TruckSports/EighteenSports/Lava";
            if(!File.Exists(folder+"/result.txt")||!File.ReadAllText(folder+"/result.txt").Contains("ERRORS=0;")||!File.ReadAllText(folder+"/result.txt").Contains("PASS physical eight-truck AI survival reaches complete results")||!File.ReadAllText(folder+"/result.txt").Contains("PASS actual accelerator-driven solid-truck impact earns one recent lava knockoff"))throw new InvalidOperationException("Pass actual finite-platform gameplay before installing its menu card.");
            string source=folder+"/gameplay-1080.png";if(!File.Exists(source))throw new InvalidOperationException("Capture actual volcanic-platform gameplay first.");
            string preview="Assets/TruckSports/Art/Previews/TruckFloorIsLava.png";File.Copy(source,preview,true);AssetDatabase.ImportAsset(preview,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(preview);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.mipmapEnabled=false;importer.maxTextureSize=1024;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
            var catalogue=JsonUtility.FromJson<SportsSportLogoAuthoring.Catalogue>(File.ReadAllText(SportsSportLogoAuthoring.InfoPath));
            if(!catalogue.entries.Any(e=>e.scene==LavaAuthoring.Folder))catalogue.entries=catalogue.entries.Concat(new[]{new SportsSportLogoAuthoring.Information{scene=LavaAuthoring.Folder,title="TRUCK FLOOR IS LAVA",players="1–4 LOCAL HUMANS + AI / 8 TRUCKS · COLLAPSING PLATFORMS"}}).ToArray();
            File.WriteAllText(SportsSportLogoAuthoring.InfoPath,JsonUtility.ToJson(catalogue,true));AssetDatabase.ImportAsset(SportsSportLogoAuthoring.InfoPath);SportsNewEntriesAuthoring.Install();Debug.Log("LAVA_MENU_INTEGRATED actual physical-platform capture and original grungy PNG");
        }
    }
}
