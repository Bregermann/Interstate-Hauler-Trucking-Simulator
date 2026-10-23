using UnityEngine;
using DigitalRuby.WeatherMaker;
namespace LWS.TruckCheeseRolling {
public enum CheeseWeatherKind {Clear,Blizzard,Hurricane}
// One vendor weather world, with all four cameras allowed. Only the sport owns gameplay gusts.
public sealed class CheeseWeather:MonoBehaviour {
 public CheeseMatch match;public WeatherMakerScript vendor;public WeatherMakerPrecipitationManagerScript precipitation;public Light sun;public Terrain terrain;public AudioSource ambience;
 public LWS.TruckSports.SportsMatchTime matchTime;
 [Range(0,1)]public float intensity=.7f;[Range(0,12)]public float windAcceleration=5;public CheeseWeatherKind Kind{get;private set;}public Vector3 Gust{get;private set;}
 Color clearAmbient;float phase;ParticleSystem[] particles;bool[] wasPlaying;
 public void Select(int seed){Kind=(CheeseWeatherKind)new System.Random(seed*7919+17).Next(3);phase=seed*.43f;precipitation.PrecipitationChangeDuration=0;precipitation.Precipitation=Kind==CheeseWeatherKind.Blizzard?WeatherMakerPrecipitationType.Snow:Kind==CheeseWeatherKind.Hurricane?WeatherMakerPrecipitationType.Rain:WeatherMakerPrecipitationType.None;precipitation.PrecipitationIntensity=Kind==CheeseWeatherKind.Clear?0:intensity;sun.intensity=Kind==CheeseWeatherKind.Clear?2:1.1f;RenderSettings.ambientLight=Kind==CheeseWeatherKind.Blizzard?new Color(.56f,.65f,.74f):new Color(.44f,.52f,.58f);Gust=Vector3.zero;matchTime?.Configure(seed,match.MatchId);}
 void Awake(){vendor.IsPermanent=false;clearAmbient=RenderSettings.ambientLight;particles=vendor.GetComponentsInChildren<ParticleSystem>(true);wasPlaying=new bool[particles.Length];}
 void FixedUpdate(){Gust=match.Live&&Kind==CheeseWeatherKind.Hurricane?new Vector3(Mathf.Sin(match.RaceTime*.8f+phase)*windAcceleration,0,Mathf.Sin(match.RaceTime*.37f+phase)*1.2f):Vector3.zero;}
 void Update(){float volume=match.InMatch&&!match.Paused&&Application.isFocused?match.settings.sfx:0;precipitation.RainScript.SetVolumeModifier(volume*.15f);precipitation.SnowScript.SetVolumeModifier(volume*.12f);if(ambience!=null){ambience.volume=volume*(Kind==CheeseWeatherKind.Clear?.015f:.07f);if(!ambience.isPlaying)ambience.Play();}}
 public void Pause(bool paused){for(int i=0;i<particles.Length;i++){if(paused){wasPlaying[i]=particles[i].isPlaying;particles[i].Pause();}else if(wasPlaying[i])particles[i].Play();}}
 void OnDestroy(){RenderSettings.ambientLight=clearAmbient;Gust=Vector3.zero;if(vendor!=null)Destroy(vendor.gameObject);}
}
}
