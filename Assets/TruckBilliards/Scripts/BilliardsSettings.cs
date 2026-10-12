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
   vibration=PlayerPrefs.GetInt(Prefix+"Vibration",1)==1;session.difficulty=(BilliardsDifficulty)Mathf.Clamp(PlayerPrefs.GetInt(Prefix+"Difficulty",1),0,2);
   master=PlayerPrefs.GetFloat(Prefix+"Master",1);sfx=PlayerPrefs.GetFloat(Prefix+"SFX",.65f);sensitivity=PlayerPrefs.GetFloat(Prefix+"Steering",1);
   fullscreen=PlayerPrefs.GetInt(Prefix+"Fullscreen",Screen.fullScreen?1:0)==1;
   var list=new System.Collections.Generic.List<Vector2Int>();
   foreach(var r in Screen.resolutions){var size=new Vector2Int(r.width,r.height);if(size.x>=1024&&size.y>=600&&!list.Contains(size))list.Add(size);}
   if(list.Count==0){list.Add(new Vector2Int(Screen.width,Screen.height));}
   resolutions=list.ToArray();int w=PlayerPrefs.GetInt(Prefix+"Width",Screen.width),h=PlayerPrefs.GetInt(Prefix+"Height",Screen.height);
   resolutionIndex=System.Array.FindIndex(resolutions,r=>r.x==w&&r.y==h);if(resolutionIndex<0)resolutionIndex=resolutions.Length-1;
   ApplyAudio();
   // Preserve test/command-line requested window sizes; saved display selection is applied on normal launches.
   if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-tbLocalSmoke")<0&&System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-screen-width")<0)ApplyDisplay();
  }
  public void ApplyAudio(){AudioListener.volume=master;if(session!=null)session.match.audioSource.volume=sfx;if(session!=null&&session.feedback!=null)session.feedback.ApplyVolume();}
  public void ApplyDisplay(){var r=resolutions[resolutionIndex];Screen.SetResolution(r.x,r.y,fullscreen?FullScreenMode.FullScreenWindow:FullScreenMode.Windowed);}
  public void Save()
  {
   PlayerPrefs.SetInt(Prefix+"Vibration",vibration?1:0);PlayerPrefs.SetInt(Prefix+"Difficulty",(int)session.difficulty);
   PlayerPrefs.SetFloat(Prefix+"Master",master);PlayerPrefs.SetFloat(Prefix+"SFX",sfx);PlayerPrefs.SetFloat(Prefix+"Steering",sensitivity);
   PlayerPrefs.SetInt(Prefix+"Fullscreen",fullscreen?1:0);var r=resolutions[resolutionIndex];PlayerPrefs.SetInt(Prefix+"Width",r.x);PlayerPrefs.SetInt(Prefix+"Height",r.y);PlayerPrefs.Save();
   ApplyAudio();ApplyDisplay();
  }
 }
}


