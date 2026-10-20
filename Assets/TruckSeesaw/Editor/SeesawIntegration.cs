using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using LWS.TruckSports.Editor;
namespace LWS.TruckSeesaw.Editor
{
    public static class SeesawIntegration
    {
        public static void Menu()
        {
            string source="Logs/TruckSports/EighteenSports/Seesaw/gameplay-1080.png";
            if(!File.Exists(source))throw new InvalidOperationException("Capture actual physical quarry gameplay before installing its card.");
            string preview="Assets/TruckSports/Art/Previews/TruckSeesaw.png";File.Copy(source,preview,true);AssetDatabase.ImportAsset(preview,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(preview);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.mipmapEnabled=false;importer.maxTextureSize=1024;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
            var catalogue=JsonUtility.FromJson<SportsSportLogoAuthoring.Catalogue>(File.ReadAllText(SportsSportLogoAuthoring.InfoPath));
            if(!catalogue.entries.Any(e=>e.scene==SeesawAuthoring.Folder))catalogue.entries=catalogue.entries.Concat(new[]{new SportsSportLogoAuthoring.Information{scene=SeesawAuthoring.Folder,title="TRUCK SEESAW",players="1–4 LOCAL HUMANS / AI · QUARRY WOBBLE BOARD"}}).ToArray();
            File.WriteAllText(SportsSportLogoAuthoring.InfoPath,JsonUtility.ToJson(catalogue,true));AssetDatabase.ImportAsset(SportsSportLogoAuthoring.InfoPath);SportsNewEntriesAuthoring.Install();Debug.Log("SEESAW_MENU_INTEGRATED actual physical quarry capture and original grungy PNG");
        }
    }
}
