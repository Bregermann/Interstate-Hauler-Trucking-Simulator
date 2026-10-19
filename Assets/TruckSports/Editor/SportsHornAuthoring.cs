using System;using System.IO;using UnityEditor;using UnityEngine;
namespace LWS.TruckSports.Editor {
public static class SportsHornAuthoring {
 public const string Path="Assets/TruckSports/Resources/SportsHornAssets.asset";
 public static void Ensure(){var asset=AssetDatabase.LoadAssetAtPath<SportsHornAssets>(Path);if(asset!=null){if(asset.defaultHorn==null)throw new InvalidOperationException("Configure the shared Sports horn recording.");return;}Directory.CreateDirectory("Assets/TruckSports/Resources");var clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/NWH/Vehicle Physics 2/Resources/NWH Vehicle Physics 2/Defaults/Sound/Horn.wav");if(clip==null)throw new InvalidOperationException("Installed NWH horn recording missing.");asset=ScriptableObject.CreateInstance<SportsHornAssets>();asset.defaultHorn=clip;AssetDatabase.CreateAsset(asset,Path);AssetDatabase.SaveAssets();}
}
}
