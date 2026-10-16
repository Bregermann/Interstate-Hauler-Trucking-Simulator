using UnityEngine;
namespace LWS.TruckSports {
// Compatibility adapter: the completed Billiards settings keys remain the shared source of truth.
public sealed class SportsSettings:MonoBehaviour {
 const string Prefix="TruckBilliards.Settings.";
 public float master=1,sfx=.65f,music=.55f,sensitivity=1;public bool vibration=true,fullscreen;public int resolutionIndex;public Vector2Int[] resolutions;
 float previous;
 void Awake(){previous=AudioListener.volume;Load();}
 public void Load(){master=LWS.TruckSports.SportsPrefs.GetFloat(Prefix+"Master",1);sfx=LWS.TruckSports.SportsPrefs.GetFloat(Prefix+"SFX",.65f);music=LWS.TruckSports.SportsPrefs.GetFloat(Prefix+"Music",.55f);sensitivity=LWS.TruckSports.SportsPrefs.GetFloat(Prefix+"Steering",1);vibration=LWS.TruckSports.SportsPrefs.GetInt(Prefix+"Vibration",1)==1;fullscreen=LWS.TruckSports.SportsPrefs.GetInt(Prefix+"Fullscreen",Screen.fullScreen?1:0)==1;
 var list=new System.Collections.Generic.List<Vector2Int>();foreach(var r in Screen.resolutions){var s=new Vector2Int(r.width,r.height);if(s.x>=1024&&s.y>=600&&!list.Contains(s))list.Add(s);}if(list.Count==0)list.Add(new Vector2Int(Screen.width,Screen.height));resolutions=list.ToArray();int w=LWS.TruckSports.SportsPrefs.GetInt(Prefix+"Width",Screen.width),h=LWS.TruckSports.SportsPrefs.GetInt(Prefix+"Height",Screen.height);resolutionIndex=System.Array.FindIndex(resolutions,r=>r.x==w&&r.y==h);if(resolutionIndex<0)resolutionIndex=resolutions.Length-1;ApplyAudio();
 if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-screen-width")<0)ApplyDisplay();}
 public void ApplyAudio(){AudioListener.volume=Mathf.Clamp01(master);}
 public void ApplyDisplay(){var r=resolutions[resolutionIndex];Screen.SetResolution(r.x,r.y,fullscreen?FullScreenMode.FullScreenWindow:FullScreenMode.Windowed);}
 public void Save(bool display=true){LWS.TruckSports.SportsPrefs.SetFloat(Prefix+"Master",master);LWS.TruckSports.SportsPrefs.SetFloat(Prefix+"SFX",sfx);LWS.TruckSports.SportsPrefs.SetFloat(Prefix+"Music",music);LWS.TruckSports.SportsPrefs.SetFloat(Prefix+"Steering",sensitivity);LWS.TruckSports.SportsPrefs.SetInt(Prefix+"Vibration",vibration?1:0);LWS.TruckSports.SportsPrefs.SetInt(Prefix+"Fullscreen",fullscreen?1:0);var r=resolutions[resolutionIndex];LWS.TruckSports.SportsPrefs.SetInt(Prefix+"Width",r.x);LWS.TruckSports.SportsPrefs.SetInt(Prefix+"Height",r.y);LWS.TruckSports.SportsPrefs.Save();ApplyAudio();if(display)ApplyDisplay();}
 void OnDestroy(){AudioListener.volume=previous;}
}
}
