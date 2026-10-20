using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using LWS.TruckSports.Editor;

namespace LWS.TruckRedLightGreenLight.Editor
{
    public static class RedLightIntegration
    {
        public static void Menu()
        {
            string capture = "Logs/TruckSports/EighteenSports/RedLight/warning-1080.png";
            if (!File.Exists(capture)) throw new InvalidOperationException("Capture actual playable Red Light gameplay before installing its card.");
            string preview = "Assets/TruckSports/Art/Previews/TruckRedLightGreenLight.png";
            File.Copy(capture, preview, true); AssetDatabase.ImportAsset(preview, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(preview);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false; importer.maxTextureSize = 1024; importer.wrapMode = TextureWrapMode.Clamp; importer.SaveAndReimport();
            var catalogue = JsonUtility.FromJson<SportsSportLogoAuthoring.Catalogue>(File.ReadAllText(SportsSportLogoAuthoring.InfoPath));
            if (!catalogue.entries.Any(e => e.scene == RedLightAuthoring.Folder))
                catalogue.entries = catalogue.entries.Concat(new[] { new SportsSportLogoAuthoring.Information { scene = RedLightAuthoring.Folder, title = "TRUCK RED LIGHT, GREEN LIGHT", players = "1–12 COMPETITORS · UP TO 4 LOCAL HUMANS · AI" } }).ToArray();
            File.WriteAllText(SportsSportLogoAuthoring.InfoPath, JsonUtility.ToJson(catalogue, true));
            AssetDatabase.ImportAsset(SportsSportLogoAuthoring.InfoPath);
            if (File.Exists("Logs/TruckSports/EighteenSports/HotPotato/gameplay-1080.png"))
            {
                string hotPreview = "Assets/TruckSports/Art/Previews/TruckHotPotato.png";
                File.Copy("Logs/TruckSports/EighteenSports/HotPotato/gameplay-1080.png", hotPreview, true); AssetDatabase.ImportAsset(hotPreview, ImportAssetOptions.ForceSynchronousImport);
                var hotImporter = (TextureImporter)AssetImporter.GetAtPath(hotPreview); hotImporter.textureType = TextureImporterType.Sprite; hotImporter.spriteImportMode = SpriteImportMode.Single; hotImporter.mipmapEnabled = false; hotImporter.maxTextureSize = 1024; hotImporter.wrapMode = TextureWrapMode.Clamp; hotImporter.SaveAndReimport();
                if (!catalogue.entries.Any(e => e.scene == "TruckHotPotato"))
                    catalogue.entries = catalogue.entries.Concat(new[] { new SportsSportLogoAuthoring.Information { scene = "TruckHotPotato", title = "TRUCK HOT POTATO", players = "2–8 COMPETITORS · UP TO 4 LOCAL HUMANS · AI" } }).ToArray();
                File.WriteAllText(SportsSportLogoAuthoring.InfoPath, JsonUtility.ToJson(catalogue, true)); AssetDatabase.ImportAsset(SportsSportLogoAuthoring.InfoPath);
            }
            SportsNewEntriesAuthoring.Install();
            Debug.Log("RLGL_MENU_INTEGRATED with approved original PNG and actual gameplay capture");
        }
    }
}
