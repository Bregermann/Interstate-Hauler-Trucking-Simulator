using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using LWS.TruckSports.Editor;
namespace LWS.TruckWhackAMole.Editor
{
    public static class WhackIntegration
    {
        public static void Menu()
        {
            string source = "Logs/TruckSports/EighteenSports/Whack/gameplay-1080.png";
            if (!File.Exists(source)) throw new InvalidOperationException("Capture actual Whack-a-Mole gameplay before installing its card.");
            string preview = "Assets/TruckSports/Art/Previews/TruckWhackAMole.png"; File.Copy(source, preview, true); AssetDatabase.ImportAsset(preview, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(preview); importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single; importer.mipmapEnabled = false; importer.maxTextureSize = 1024; importer.wrapMode = TextureWrapMode.Clamp; importer.SaveAndReimport();
            var catalogue = JsonUtility.FromJson<SportsSportLogoAuthoring.Catalogue>(File.ReadAllText(SportsSportLogoAuthoring.InfoPath));
            if (!catalogue.entries.Any(e => e.scene == WhackAuthoring.Folder)) catalogue.entries = catalogue.entries.Concat(new[] { new SportsSportLogoAuthoring.Information { scene = WhackAuthoring.Folder, title = "TRUCK WHACK-A-MOLE", players = "1–8 COMPETITORS · UP TO 4 LOCAL HUMANS · AI" } }).ToArray();
            File.WriteAllText(SportsSportLogoAuthoring.InfoPath, JsonUtility.ToJson(catalogue, true)); AssetDatabase.ImportAsset(SportsSportLogoAuthoring.InfoPath); SportsNewEntriesAuthoring.Install(); Debug.Log("WHACK_MENU_INTEGRATED actual fairground capture and original PNG");
        }
    }
}
