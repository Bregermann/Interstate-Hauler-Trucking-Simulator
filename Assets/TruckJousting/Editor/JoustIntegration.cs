using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using LWS.TruckSports.Editor;
namespace LWS.TruckJousting.Editor
{
    public static class JoustIntegration
    {
        public static void Menu()
        {
            const string folder="Logs/TruckSports/EighteenSports/Joust";
            if(!File.Exists(folder+"/result.txt")||!File.ReadAllText(folder+"/result.txt").Contains("ERRORS=0;")||!File.ReadAllText(folder+"/result.txt").Contains("PASS actual AI driving and lance aiming complete all three tournament passes")||!File.ReadAllText(folder+"/result.txt").Contains("PASS real physical lance-tip contacts earn all hit points exactly once"))throw new InvalidOperationException("Pass the actual full physical jousting tournament before installing its menu card.");
            string source=folder+"/gameplay-1080.png";if(!File.Exists(source))throw new InvalidOperationException("Capture actual jousting gameplay first.");
            string preview="Assets/TruckSports/Art/Previews/TruckJousting.png";File.Copy(source,preview,true);AssetDatabase.ImportAsset(preview,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(preview);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.mipmapEnabled=false;importer.maxTextureSize=1024;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
            var catalogue=JsonUtility.FromJson<SportsSportLogoAuthoring.Catalogue>(File.ReadAllText(SportsSportLogoAuthoring.InfoPath));
            if(!catalogue.entries.Any(e=>e.scene==JoustAuthoring.Folder))catalogue.entries=catalogue.entries.Concat(new[]{new SportsSportLogoAuthoring.Information{scene=JoustAuthoring.Folder,title="TRUCK JOUSTING",players="1–2 LOCAL HUMANS OR AI · PHYSICAL LANCE TOURNAMENT"}}).ToArray();
            File.WriteAllText(SportsSportLogoAuthoring.InfoPath,JsonUtility.ToJson(catalogue,true));AssetDatabase.ImportAsset(SportsSportLogoAuthoring.InfoPath);SportsNewEntriesAuthoring.Install();Debug.Log("JOUST_MENU_INTEGRATED actual tournament capture and original grungy PNG");
        }
    }
}
