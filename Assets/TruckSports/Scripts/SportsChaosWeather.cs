using UnityEngine;
using DigitalRuby.WeatherMaker;
namespace LWS.TruckSports {
public enum SportsWeatherKind {Clear,Rain,Snow,Blizzard,Hurricane}
// Read-only bridge for existing sports whose match authority predates
// SportsEventSession. Their rules, inputs and physics remain with that owner.
public interface ISportsWeatherLifecycle {
 string WeatherMatchId {get;} bool WeatherMatchActive {get;} bool WeatherPaused {get;} bool WeatherAudible {get;} float WeatherSfx {get;}
}
// Owned arcade wind semantics. Weather Maker remains the presentation authority.
public sealed class SportsChaosWeather:MonoBehaviour {
 public LWS.TruckCornhole.CornholeMatch cornhole;public SportsWeatherPresets presets;public bool reduceSnowRandom;
 public WeatherMakerFullScreenCloudsScript clouds;public WeatherMakerCloudProfileScript[] cloudProfiles;public SportsEventSession session;public WeatherMakerScript vendor;public WeatherMakerPrecipitationManagerScript precipitation;public WeatherMakerWindScript wind;public Light sun;public AudioSource ambience;
 public SportsWeatherKind Kind{get;private set;}public Vector3 Flow{get;private set;}public float Grip{get;private set;}=1;public int Seed{get;private set;}public string Description{get;private set;}="CLEAR";
 public bool PresetDriven{get;private set;}public string PresetId{get;private set;}
 public SportsMatchTime matchTime;
 public MonoBehaviour alternateSession;
 string alternateMatchId,configuredMatchId;bool alternatePaused;
 string CurrentMatchId=>session!=null?session.MatchId:cornhole!=null?cornhole.MatchId:(alternateSession as ISportsWeatherLifecycle)?.WeatherMatchId;
 WeatherMakerProfileScript activeProfile;float windUnitScale=.44704f;WeatherMakerLightningBoltScript[] lightning;WeatherMakerThunderAndLightningScript thunder;
 public void ConfigurePreset(SportsWeatherPresets.Entry preset,int seed){
  if(preset==null||preset.profile==null||preset.profile.Disabled)throw new System.ArgumentException("Select an enabled installed weather profile.");
  matchTime?.Configure(seed,CurrentMatchId);
  BindLightningCamera();var previous=activeProfile;activeProfile=Instantiate(preset.profile);activeProfile.name=preset.profile.name+" (Sports runtime clone)";
  Seed=seed;PresetId=preset.id;Kind=preset.legacyKind;Grip=preset.grip;windUnitScale=preset.metresPerSecondPerWindUnit;Description=preset.displayName.ToUpperInvariant();PresetDriven=true;
  matchTime?.ApplySnowPalette(preset.Snow);
  if(clouds!=null)clouds.CloudSeed=seed;vendor.LastLocalProfile=activeProfile;vendor.RaiseWeatherProfileChanged(previous,activeProfile,.2f,float.MaxValue,true,null);
  if(previous!=null)Destroy(previous);Tick(0);
 }
 float phase,strength;ParticleSystem[] systems;bool[] playing;
 void Awake(){if(vendor==null)return;vendor.IsPermanent=false;lightning=vendor.GetComponentsInChildren<WeatherMakerLightningBoltScript>(true);foreach(var bolt in lightning)bolt.MultiThreaded=false;thunder=vendor.GetComponentInChildren<WeatherMakerThunderAndLightningScript>(true);BindLightningCamera();systems=vendor.GetComponentsInChildren<ParticleSystem>(true);playing=new bool[systems.Length];}
 public void Configure(int seed,int choice,int chaos){if(presets!=null){
  string id=CurrentMatchId;
  // A heat/attempt can refresh its own wind semantics; the selected match
  // forecast must not silently reroll. Direct ConfigurePreset stays explicit.
  if(id!=null&&id==configuredMatchId&&PresetDriven&&activeProfile!=null){matchTime?.Configure(seed,id);return;}
  ConfigurePreset(presets.Resolve(seed,reduceSnowRandom),seed);configuredMatchId=id;return;
 }PresetDriven=false;PresetId=null;Seed=seed;var random=new System.Random(seed);Kind=(SportsWeatherKind)(choice==0?random.Next(5):Mathf.Clamp(choice-1,0,4));phase=(float)random.NextDouble()*40;strength=Mathf.Lerp(.65f,1.25f,Mathf.Clamp01(chaos*.5f));Description=Kind==SportsWeatherKind.Hurricane?"HURRICANE GUSTS":Kind==SportsWeatherKind.Blizzard?"BLIZZARD":Kind.ToString().ToUpperInvariant();Grip=Kind==SportsWeatherKind.Clear?1:Kind==SportsWeatherKind.Rain?.78f:Kind==SportsWeatherKind.Snow?.72f:Kind==SportsWeatherKind.Blizzard?.64f:.82f;
  precipitation.PrecipitationChangeDuration=0;precipitation.Precipitation=Kind==SportsWeatherKind.Clear?WeatherMakerPrecipitationType.None:Kind==SportsWeatherKind.Snow||Kind==SportsWeatherKind.Blizzard?WeatherMakerPrecipitationType.Snow:WeatherMakerPrecipitationType.Rain;precipitation.PrecipitationIntensity=Kind==SportsWeatherKind.Clear?0:Mathf.Clamp01((Kind==SportsWeatherKind.Blizzard||Kind==SportsWeatherKind.Hurricane?.85f:.65f)*strength);if(clouds!=null&&cloudProfiles!=null&&cloudProfiles.Length==3){clouds.CloudSeed=seed;clouds.ShowCloudsAnimated(cloudProfiles[Kind==SportsWeatherKind.Clear?0:Kind==SportsWeatherKind.Snow?2:1],0,.6f);}if(sun!=null)sun.intensity=Kind==SportsWeatherKind.Clear?1.8f:Kind==SportsWeatherKind.Hurricane?.7f:1.1f;Tick(0);matchTime?.Configure(seed,CurrentMatchId);matchTime?.ApplySnowPalette(Kind==SportsWeatherKind.Snow||Kind==SportsWeatherKind.Blizzard);
 }
 public void Tick(float age){if(PresetDriven){float scale=wind!=null&&wind.WindProfile!=null?wind.WindProfile.WindMainMultiplier:0;Flow=scale>.00001f?Vector3.ProjectOnPlane(wind.CurrentWindVelocity,Vector3.up)*(windUnitScale/scale):Vector3.zero;return;}float gust=Mathf.Sin(age*.61f+phase)+.4f*Mathf.Sin(age*1.27f+phase*.17f);float speed=Kind==SportsWeatherKind.Hurricane?10:Kind==SportsWeatherKind.Blizzard?6:Kind==SportsWeatherKind.Clear?1.5f:3;Flow=new Vector3(Mathf.Sin(phase)*speed+gust*speed*.35f,0,Mathf.Cos(phase)*speed*.5f+Mathf.Sin(age*.3f+phase)*speed*.2f)*strength;if(wind!=null){wind.WindIntensity=Mathf.Clamp01(Flow.magnitude/14);if(wind.WindZone!=null){wind.WindZone.transform.rotation=Quaternion.LookRotation(Flow.sqrMagnitude>.01f?Flow:Vector3.forward);wind.WindZone.windMain=Flow.magnitude*.12f;}}}
 public void Pause(bool pause){if(systems==null)return;for(int i=0;i<systems.Length;i++){if(pause){playing[i]=systems[i].isPlaying;systems[i].Pause();}else if(playing[i])systems[i].Play();}}
 // Split-screen cameras are intentionally untagged. The vendor bolt fallback
 // cannot discover them through Camera.main, so bind its supported Camera field.
 // Use the supported synchronous bolt generation option before vendor Start.
 // Its worker can otherwise enqueue light creation while a scene is destroyed.
 void BindLightningCamera(){if(vendor==null||lightning==null)return;Camera selected=null;foreach(var camera in vendor.AllowCameras){if(camera==null)continue;if(selected==null)selected=camera;if(camera.isActiveAndEnabled){selected=camera;break;}}foreach(var bolt in lightning)if(bolt!=null)bolt.Camera=selected;}
 void Update(){
  var alternate=alternateSession as ISportsWeatherLifecycle;
  if(alternate!=null){
   if(alternate.WeatherMatchActive&&alternate.WeatherMatchId!=alternateMatchId){Pause(false);Configure(alternate.WeatherMatchId.GetHashCode(),0,0);alternateMatchId=alternate.WeatherMatchId;}
   if(alternatePaused!=alternate.WeatherPaused){alternatePaused=alternate.WeatherPaused;Pause(alternatePaused);}
   if(alternate.WeatherMatchActive&&!alternatePaused)Tick(0);
  }
  BindLightningCamera();bool live=session!=null?session.Live:cornhole!=null?cornhole.InMatch&&!cornhole.Paused&&!cornhole.Finished:alternate!=null&&alternate.WeatherAudible;
  float gain=live&&Application.isFocused?(session!=null?session.settings.sfx:cornhole!=null?cornhole.settings.sfx:alternate.WeatherSfx):0;
  if(thunder!=null)thunder.VolumeModifier=gain;precipitation.RainScript.SetVolumeModifier(gain*.12f);precipitation.SnowScript.SetVolumeModifier(gain*.1f);if(ambience!=null){ambience.volume=gain*(Kind==SportsWeatherKind.Clear?.015f:.07f);if(ambience.volume>0&&!ambience.isPlaying)ambience.Play();else if(ambience.volume<=0)ambience.Stop();}
 }
 void OnDestroy(){if(activeProfile!=null)Destroy(activeProfile);Flow=Vector3.zero;if(vendor!=null)Destroy(vendor.gameObject);}
}
}
