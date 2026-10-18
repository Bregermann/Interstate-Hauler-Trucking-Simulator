using System;
using UnityEngine;
using DigitalRuby.WeatherMaker;
namespace LWS.TruckSports {
// Selection metadata and sport semantics over installed Weather Maker profiles/groups.
// Weather Maker remains responsible for random selection and weather presentation.
[CreateAssetMenu(menuName="Truck Sports/Weather preset catalog")]
public sealed class SportsWeatherPresets:ScriptableObject {
 public const string PreferenceKey="TruckSports.Weather.Preset.v1";
 public const string RandomId="random";
 [Serializable]public sealed class Entry {
  public string id,displayName;
  public WeatherMakerProfileScript profile;
  [Min(0)]public int randomWeight=100;
  [Range(.2f,1)]public float grip=1;
  [Min(0)]public float metresPerSecondPerWindUnit=.44704f;
  public SportsWeatherKind legacyKind;
  public bool Snow=>profile!=null&&profile.PrecipitationProfile!=null&&profile.PrecipitationProfile.PrecipitationType==WeatherMakerPrecipitationType.Snow;
 }
 public Entry[] entries=Array.Empty<Entry>();
 [Range(0,1)]public float minefieldSnowWeight=.25f;
 public int Find(string id){for(int i=0;i<entries.Length;i++)if(entries[i]!=null&&entries[i].id==id)return i;return -1;}
 public string Selection=>SportsPrefs.GetString(PreferenceKey,RandomId);
 public void Select(string id){if(id!=RandomId&&Find(id)<0)throw new ArgumentException("Weather preset is not in the installed catalog: "+id);SportsPrefs.SetString(PreferenceKey,id);SportsPrefs.Save();}
 public Entry Resolve(int seed,bool reduceSnow=false){
  ValidateCatalog();
  string choice=Selection;if(choice!=RandomId){int found=Find(choice);if(found<0)throw new InvalidOperationException("Selected weather preset is missing: "+choice);ValidateEntry(entries[found]);return entries[found];}
  var group=ScriptableObject.CreateInstance<WeatherMakerProfileGroupScript>();group.hideFlags=HideFlags.HideAndDontSave;
  var state=UnityEngine.Random.state;
  try{
   group.Profiles=new WeatherMakerProfileAndWeight[entries.Length];long total=0;
   for(int i=0;i<entries.Length;i++){
    var item=entries[i];ValidateEntry(item);int weight=item.profile.Disabled?0:Mathf.RoundToInt(item.randomWeight*(reduceSnow&&item.Snow?minefieldSnowWeight:1));
    if(weight<0)throw new InvalidOperationException("Weather random weight must be nonnegative: "+item.displayName);
    total+=weight;group.Profiles[i]=new WeatherMakerProfileAndWeight{Name=item.displayName,Profile=item.profile,Weight=weight};
   }
   if(total<=0||total>int.MaxValue)throw new InvalidOperationException("Weather Random requires at least one enabled positive weight and a bounded total.");
   UnityEngine.Random.InitState(seed);var selected=group.PickWeightedProfile();
   foreach(var item in entries)if(item.profile==selected)return item;
   throw new InvalidOperationException("Weather Maker returned a profile outside the configured catalog.");
  }finally{UnityEngine.Random.state=state;if(Application.isPlaying)Destroy(group);else DestroyImmediate(group);}
 }
 static void ValidateEntry(Entry item){if(item==null||string.IsNullOrEmpty(item.id)||item.id==RandomId||string.IsNullOrEmpty(item.displayName)||item.profile==null)throw new InvalidOperationException("Weather catalog entries require a unique ID, readable name and installed Weather Maker profile.");}
 public void ValidateCatalog(){
  if(entries==null||entries.Length==0)throw new InvalidOperationException("Weather preset catalog is empty.");
  var ids=new System.Collections.Generic.HashSet<string>();var profiles=new System.Collections.Generic.HashSet<WeatherMakerProfileScript>();
  foreach(var item in entries){ValidateEntry(item);if(!ids.Add(item.id)||!profiles.Add(item.profile))throw new InvalidOperationException("Duplicate weather preset ID/profile: "+item.displayName);if(item.randomWeight<0)throw new InvalidOperationException("Weather weights cannot be negative.");}
 }
}
}
