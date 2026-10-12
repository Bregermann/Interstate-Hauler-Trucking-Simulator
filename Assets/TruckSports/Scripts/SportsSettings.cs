using UnityEngine;
namespace LWS.TruckSports {
// Compatibility adapter: the completed Billiards settings keys remain the shared source of truth.
public sealed class SportsSettings:MonoBehaviour {
 const string Prefix="TruckBilliards.Settings.";
 public float master=1,sfx=.65f,sensitivity=1;public bool vibration=true,fullscreen;public int resolutionIndex;public Vector2Int[] resolutions;
 float previous;
 void Awake(){previous=AudioListener.volume;Load();}
 public void Load(){master=PlayerPrefs.GetFloat(Prefix+"Master",1);sfx=PlayerPrefs.GetFloat(Prefix+"SFX",.65f);sensitivity=PlayerPrefs.GetFloat(Prefix+"Steering",1);vibration=PlayerPrefs.GetInt(Prefix+"Vibration",1)==1;fullscreen=PlayerPrefs.GetInt(Prefix+"Fullscreen",Screen.fullScreen?1:0)==1;
 var list=new System.Collections.Generic.List<Vector2Int>();foreach(var r in Screen.resolutions){var s=new Vector2Int(r.width,r.height);if(s.x>=1024&&s.y>=600&&!list.Contains(s))list.Add(s);}if(list.Count==0)list.Add(new Vector2Int(Screen.width,Screen.height));resolutions=list.ToArray();int w=PlayerPrefs.GetInt(Prefix+"Width",Screen.width),h=PlayerPrefs.GetInt(Prefix+"Height",Screen.height);resolutionIndex=System.Array.FindIndex(resolutions,r=>r.x==w&&r.y==h);if(resolutionIndex<0)resolutionIndex=resolutions.Length-1;ApplyAudio();
 if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-screen-width")<0)ApplyDisplay();}
 public void ApplyAudio(){AudioListener.volume=Mathf.Clamp01(master);}
 public void ApplyDisplay(){var r=resolutions[resolutionIndex];Screen.SetResolution(r.x,r.y,fullscreen?FullScreenMode.FullScreenWindow:FullScreenMode.Windowed);}
 public void Save(bool display=true){PlayerPrefs.SetFloat(Prefix+"Master",master);PlayerPrefs.SetFloat(Prefix+"SFX",sfx);PlayerPrefs.SetFloat(Prefix+"Steering",sensitivity);PlayerPrefs.SetInt(Prefix+"Vibration",vibration?1:0);PlayerPrefs.SetInt(Prefix+"Fullscreen",fullscreen?1:0);var r=resolutions[resolutionIndex];PlayerPrefs.SetInt(Prefix+"Width",r.x);PlayerPrefs.SetInt(Prefix+"Height",r.y);PlayerPrefs.Save();ApplyAudio();if(display)ApplyDisplay();}
 void OnDestroy(){AudioListener.volume=previous;}
}
}
