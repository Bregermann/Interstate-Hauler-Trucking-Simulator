using UnityEngine;
namespace LWS.TruckBilliards
{
 public sealed class BilliardsSettings:MonoBehaviour
 {
  const string Prefix="TruckBilliards.Settings.";public BilliardsSession session;
  public float master=1,sfx=.65f,sensitivity=1;public bool fullscreen;public bool vibration=true;public int resolutionIndex;
  public Vector2Int[] resolutions;float previousListenerVolume;
  void Awake(){previousListenerVolume=AudioListener.volume;}
  void OnDestroy(){AudioListener.volume=previousListenerVolume;}
  public void Load()
  {
   vibration=LWS.TruckSports.SportsPrefs.GetInt(Prefix+"Vibration",1)==1;session.difficulty=(BilliardsDifficulty)Mathf.Clamp(LWS.TruckSports.SportsPrefs.GetInt(Prefix+"Difficulty",1),0,2);
   master=LWS.TruckSports.SportsPrefs.GetFloat(Prefix+"Master",1);sfx=LWS.TruckSports.SportsPrefs.GetFloat(Prefix+"SFX",.65f);sensitivity=LWS.TruckSports.SportsPrefs.GetFloat(Prefix+"Steering",1);
   fullscreen=LWS.TruckSports.SportsPrefs.GetInt(Prefix+"Fullscreen",Screen.fullScreen?1:0)==1;
   var list=new System.Collections.Generic.List<Vector2Int>();
   foreach(var r in Screen.resolutions){var size=new Vector2Int(r.width,r.height);if(size.x>=1024&&size.y>=600&&!list.Contains(size))list.Add(size);}
   if(list.Count==0){list.Add(new Vector2Int(Screen.width,Screen.height));}
   resolutions=list.ToArray();int w=LWS.TruckSports.SportsPrefs.GetInt(Prefix+"Width",Screen.width),h=LWS.TruckSports.SportsPrefs.GetInt(Prefix+"Height",Screen.height);
   resolutionIndex=System.Array.FindIndex(resolutions,r=>r.x==w&&r.y==h);if(resolutionIndex<0)resolutionIndex=resolutions.Length-1;
   ApplyAudio();
   // Preserve test/command-line requested window sizes; saved display selection is applied on normal launches.
   if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-tbLocalSmoke")<0&&System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-screen-width")<0)ApplyDisplay();
  }
  public void ApplyAudio(){AudioListener.volume=master;if(session!=null)session.match.audioSource.volume=sfx;if(session!=null&&session.feedback!=null)session.feedback.ApplyVolume();}
  public void ApplyDisplay(){var r=resolutions[resolutionIndex];Screen.SetResolution(r.x,r.y,fullscreen?FullScreenMode.FullScreenWindow:FullScreenMode.Windowed);}
  public void Save()
  {
   LWS.TruckSports.SportsPrefs.SetInt(Prefix+"Vibration",vibration?1:0);LWS.TruckSports.SportsPrefs.SetInt(Prefix+"Difficulty",(int)session.difficulty);
   LWS.TruckSports.SportsPrefs.SetFloat(Prefix+"Master",master);LWS.TruckSports.SportsPrefs.SetFloat(Prefix+"SFX",sfx);LWS.TruckSports.SportsPrefs.SetFloat(Prefix+"Steering",sensitivity);
   LWS.TruckSports.SportsPrefs.SetInt(Prefix+"Fullscreen",fullscreen?1:0);var r=resolutions[resolutionIndex];LWS.TruckSports.SportsPrefs.SetInt(Prefix+"Width",r.x);LWS.TruckSports.SportsPrefs.SetInt(Prefix+"Height",r.y);LWS.TruckSports.SportsPrefs.Save();
   ApplyAudio();ApplyDisplay();
  }
 }
}


