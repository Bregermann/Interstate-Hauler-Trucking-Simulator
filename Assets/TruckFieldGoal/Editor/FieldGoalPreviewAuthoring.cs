using System.IO;
using UnityEngine;
using UnityEditor;
namespace LWS.TruckFieldGoal.Editor {
public static class FieldGoalPreviewAuthoring {
 // Preserve the verified natural-gameplay capture during normal scene authoring.
 public static void Create(FieldGoalMatch match){
  const string path="Assets/TruckFieldGoal/Art/Menu Preview.png";
  if(!File.Exists(path))throw new System.InvalidOperationException("Restore or capture the actual bumper-kick gameplay preview before authoring Field Goal.");
  var importer=AssetImporter.GetAtPath(path) as TextureImporter;
  if(importer==null)throw new System.InvalidOperationException("Import the saved Field Goal gameplay preview first.");
  if(importer.textureType!=TextureImporterType.Sprite){importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.mipmapEnabled=false;importer.SaveAndReimport();}
 }
}
}
