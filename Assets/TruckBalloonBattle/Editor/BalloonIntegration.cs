using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using LWS.TruckSports.Editor;
namespace LWS.TruckBalloonBattle.Editor
{
    public static class BalloonIntegration
    {
        public static void Menu()
        {
            string source = "Logs/TruckSports/EighteenSports/Balloon/gameplay-1080.png";
            if (!File.Exists(source)) throw new InvalidOperationException("Capture actual Balloon Battle gameplay before installing its selection card.");
            string preview = "Assets/TruckSports/Art/Previews/TruckBalloonBattle.png"; File.Copy(source, preview, true); AssetDatabase.ImportAsset(preview, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(preview); importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single; importer.mipmapEnabled = false; importer.maxTextureSize = 1024; importer.wrapMode = TextureWrapMode.Clamp; importer.SaveAndReimport();
            var catalogue = JsonUtility.FromJson<SportsSportLogoAuthoring.Catalogue>(File.ReadAllText(SportsSportLogoAuthoring.InfoPath));
            if (!catalogue.entries.Any(e => e.scene == BalloonAuthoring.Folder)) catalogue.entries = catalogue.entries.Concat(new[] { new SportsSportLogoAuthoring.Information { scene = BalloonAuthoring.Folder, title = "TRUCK BALLOON BATTLE", players = "2–8 COMPETITORS · UP TO 4 LOCAL HUMANS · AI" } }).ToArray();
            File.WriteAllText(SportsSportLogoAuthoring.InfoPath, JsonUtility.ToJson(catalogue, true)); AssetDatabase.ImportAsset(SportsSportLogoAuthoring.InfoPath); SportsNewEntriesAuthoring.Install();
            Debug.Log("BALLOON_MENU_INTEGRATED actual captured gameplay and original grungy PNG");
        }
    }
}
