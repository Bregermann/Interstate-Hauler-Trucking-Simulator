using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using LWS.TruckSports.Editor;
namespace LWS.TruckTetherball.Editor
{
    public static class TetherIntegration
    {
        public static void Menu()
        {
            const string folder="Logs/TruckSports/EighteenSports/Tether";
            if(!File.Exists(folder+"/result.txt")||!File.ReadAllText(folder+"/result.txt").Contains("ERRORS=0;")||!File.ReadAllText(folder+"/result.txt").Contains("PASS actual AI strikes complete the full best-of-three match")||!File.ReadAllText(folder+"/result.txt").Contains("PASS real constrained rope remains bounded"))throw new InvalidOperationException("Pass the actual full physical tetherball match before installing its menu card.");
            string source=folder+"/gameplay-1080.png";if(!File.Exists(source))throw new InvalidOperationException("Capture actual tetherball gameplay first.");
            string preview="Assets/TruckSports/Art/Previews/TruckTetherball.png";File.Copy(source,preview,true);AssetDatabase.ImportAsset(preview,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(preview);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.mipmapEnabled=false;importer.maxTextureSize=1024;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
            var catalogue=JsonUtility.FromJson<SportsSportLogoAuthoring.Catalogue>(File.ReadAllText(SportsSportLogoAuthoring.InfoPath));
            if(!catalogue.entries.Any(e=>e.scene==TetherAuthoring.Folder))catalogue.entries=catalogue.entries.Concat(new[]{new SportsSportLogoAuthoring.Information{scene=TetherAuthoring.Folder,title="TRUCK TETHERBALL",players="1–2 LOCAL HUMANS OR AI · PHYSICAL CABLE WINDING"}}).ToArray();
            File.WriteAllText(SportsSportLogoAuthoring.InfoPath,JsonUtility.ToJson(catalogue,true));AssetDatabase.ImportAsset(SportsSportLogoAuthoring.InfoPath);SportsNewEntriesAuthoring.Install();Debug.Log("TETHER_MENU_INTEGRATED actual winding match capture and original grungy PNG");
        }
    }
}
