using UnityEngine;
using DigitalRuby.WeatherMaker;
namespace LWS.TruckBaseJumping {
// One shared gameplay wind field; Weather Maker owns the sky/precipitation presentation.
public sealed class BaseJumpWeather:MonoBehaviour {
 public WeatherMakerFullScreenCloudsScript clouds;public WeatherMakerCloudProfileScript[] cloudProfiles;public BaseJumpMatch match;public WeatherMakerScript vendor;public WeatherMakerPrecipitationManagerScript precipitation;public WeatherMakerWindScript wind;public Light sun;public AudioSource ambience;public Vector3 Flow{get;private set;}
 public LWS.TruckSports.SportsMatchTime matchTime;
 public LWS.TruckSports.SportsChaosWeather presentation;
 float phase;int preset;ParticleSystem[] particles;bool[] playing;
 void Awake(){vendor.IsPermanent=false;particles=vendor.GetComponentsInChildren<ParticleSystem>(true);playing=new bool[particles.Length];}
 public void Select(int option,int seed){preset=option;phase=seed*.173f;Flow=Vector3.zero;if(presentation!=null){presentation.Configure(seed,0,0);return;}if(clouds!=null&&cloudProfiles!=null&&cloudProfiles.Length==3){clouds.CloudSeed=seed;clouds.ShowCloudsAnimated(cloudProfiles[option],0,.6f);}precipitation.PrecipitationChangeDuration=0;precipitation.Precipitation=option==0?WeatherMakerPrecipitationType.None:option==1?WeatherMakerPrecipitationType.Rain:WeatherMakerPrecipitationType.Snow;precipitation.PrecipitationIntensity=option==0?0:option==1?.68f:.8f;sun.intensity=option==0?1.7f:option==1?.85f:1.1f;matchTime?.Configure(seed,match.MatchId);}
 public void Tick(float age){float gust=Mathf.Sin(age*.7f+phase)+Mathf.Sin(age*1.31f+phase*.41f)*.45f;Flow=preset==0?new Vector3(7.5f+gust*1.6f,0,Mathf.Sin(age*.33f+phase)*2):preset==1?new Vector3(9+gust*2.2f,0,Mathf.Sin(age*.46f+phase)*3.2f):new Vector3(-8.5f+gust*1.9f,0,Mathf.Cos(age*.38f+phase)*3);if(wind.WindZone!=null){wind.WindZone.transform.rotation=Quaternion.LookRotation(Flow,Vector3.up);wind.WindZone.windMain=Flow.magnitude*.12f;}wind.WindIntensity=Mathf.Clamp01(Flow.magnitude/14);}
 void Update(){if(presentation!=null)return;float gain=match!=null&&match.Live&&Application.isFocused?match.settings.sfx:0;precipitation.RainScript.SetVolumeModifier(gain*.1f);precipitation.SnowScript.SetVolumeModifier(gain*.09f);if(ambience!=null){ambience.volume=gain*.065f;if(gain>0&&!ambience.isPlaying)ambience.Play();else if(gain<=0)ambience.Stop();}}
 public void PauseVisuals(bool pause){if(presentation!=null){presentation.Pause(pause);return;}for(int i=0;i<particles.Length;i++){if(pause){playing[i]=particles[i].isPlaying;particles[i].Pause();}else if(playing[i])particles[i].Play();}}
 void OnDestroy(){Flow=Vector3.zero;if(object.ReferenceEquals(presentation,null)&&vendor!=null)Destroy(vendor.gameObject);}
}
}
