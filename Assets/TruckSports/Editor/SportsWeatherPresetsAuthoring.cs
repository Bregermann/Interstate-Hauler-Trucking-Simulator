using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using DigitalRuby.WeatherMaker;
namespace LWS.TruckSports.Editor {
public static class SportsWeatherPresetsAuthoring {
 public const string CatalogPath="Assets/TruckSports/Settings/SportsWeatherPresets.asset";
 const string Folder="Assets/TruckSports/Settings";
 // Explicit creation only. Normal builds and Play Mode never repopulate customized lists.
 public static SportsWeatherPresets EnsureCatalog(){
  var existing=AssetDatabase.LoadAssetAtPath<SportsWeatherPresets>(CatalogPath);if(existing!=null){existing.ValidateCatalog();return existing;}
  Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
  var catalog=ScriptableObject.CreateInstance<SportsWeatherPresets>();
  var list=AssetDatabase.FindAssets("t:WeatherMakerProfileScript",new[]{"Assets/WeatherMaker/Prefab/Profiles/Weather/Individual"}).OrderBy(AssetDatabase.GUIDToAssetPath).Select(guid=>{
   var profile=AssetDatabase.LoadAssetAtPath<WeatherMakerProfileScript>(AssetDatabase.GUIDToAssetPath(guid));string raw=profile.name.Replace("WeatherMakerProfile_","");
   string label=Regex.Replace(raw,"([a-z])([A-Z])","$1 $2");if(raw=="Storm")label="Thunderstorm";else if(raw=="MediumRain")label="Rain";else if(raw=="MediumSnow")label="Snow";else if(raw=="None")label="No Weather";
   var type=profile.PrecipitationProfile!=null?profile.PrecipitationProfile.PrecipitationType:WeatherMakerPrecipitationType.None;float intensity=profile.PrecipitationProfile!=null?profile.PrecipitationProfile.IntensityRange.Maximum:0;
   return new SportsWeatherPresets.Entry{id=guid,displayName=label,profile=profile,randomWeight=100,grip=type==WeatherMakerPrecipitationType.Snow?1-.36f*intensity:type==WeatherMakerPrecipitationType.None?1:1-.24f*intensity,legacyKind=raw=="Blizzard"?SportsWeatherKind.Blizzard:type==WeatherMakerPrecipitationType.Snow?SportsWeatherKind.Snow:type==WeatherMakerPrecipitationType.None?SportsWeatherKind.Clear:SportsWeatherKind.Rain};
  }).ToList();
  if(list.Count==0)throw new InvalidOperationException("No installed Weather Maker individual profiles found.");
  // The collection already supports Hurricane. Express it through owned vendor profiles.
  var storm=AssetDatabase.LoadAssetAtPath<WeatherMakerProfileScript>("Assets/WeatherMaker/Prefab/Profiles/Weather/Individual/Rain/WeatherMakerProfile_Storm.asset");
  var hurricane=UnityEngine.Object.Instantiate(storm);hurricane.name="Truck Sports Hurricane";
  var wind=UnityEngine.Object.Instantiate(storm.WindProfile);wind.name="Truck Sports Hurricane wind";wind.WindSpeedRange=new RangeOfFloats{Minimum=65,Maximum=90};wind.WindChangeInterval=new RangeOfFloats{Minimum=2,Maximum=5};wind.WindChangeDuration=new RangeOfFloats{Minimum=.8f,Maximum=2};wind.AllowBlowUp=false;
  AssetDatabase.CreateAsset(wind,Folder+"/Hurricane wind.asset");hurricane.WindProfile=wind;
  AssetDatabase.CreateAsset(hurricane,Folder+"/Hurricane weather.asset");
  list.Add(new SportsWeatherPresets.Entry{id=AssetDatabase.AssetPathToGUID(Folder+"/Hurricane weather.asset"),displayName="Hurricane",profile=hurricane,randomWeight=100,grip=.74f,legacyKind=SportsWeatherKind.Hurricane});
  catalog.entries=list.ToArray();catalog.ValidateCatalog();AssetDatabase.CreateAsset(catalog,CatalogPath);AssetDatabase.SaveAssets();return catalog;
 }
}
}
