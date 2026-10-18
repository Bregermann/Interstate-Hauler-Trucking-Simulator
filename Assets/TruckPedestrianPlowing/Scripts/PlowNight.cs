using UnityEngine;
using LWS.TruckSports;
namespace LWS.TruckPedestrianPlowing {
 // Scene-owned coordination only. Weather Maker renders the actual night sky, moon and clouds.
 public sealed class PlowNight:MonoBehaviour {
  public PlowMatch match;public SportsChaosWeather weather;
  Camera[] views;
  void Start(){weather.Configure(match.crowdSettings.randomSeed,1,0);}
  void Update(){
   if(views!=match.cameras){views=match.cameras;weather.vendor.AllowCameras.Clear();foreach(var camera in views)if(camera!=null)weather.vendor.AllowCameras.Add(camera);DigitalRuby.WeatherMaker.WeatherMakerScript.ClearShouldIgnoreCameraCache();}
   if(match.Live)weather.Tick(match.Round!=null?match.Round.Elapsed:0);
  }
 }
}
