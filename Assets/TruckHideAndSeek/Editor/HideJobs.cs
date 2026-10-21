using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace LWS.TruckHideAndSeek.Editor
{
 public static class HideJobs
 {
  public static void SnowRoads(){SessionState.SetString("Sports.ExpansionSnow.OnlyScene",HideAuthoring.Folder);LWS.TruckSports.Editor.ExpansionSnowJobs.Play();}
  public static void Scenic(){SessionState.SetBool("Sports.Hide.Scenic",true);Play();}
  public static void Adapters(){Directory.CreateDirectory("Logs/TruckSports/EighteenSports/HideAdapters");File.WriteAllText("Logs/TruckSports/EighteenSports/HideAdapters/result.txt","WAITING");EditorSceneManager.OpenScene(LWS.TruckHungryHaulers.Editor.HaulerAuthoring.Scene);SessionState.SetBool("Sports.Hide.Adapters",true);SessionState.SetBool("Sports.Hide.AdapterExit",true);EditorApplication.playModeStateChanged+=AdapterChanged;EditorApplication.EnterPlaymode();}
  public static void Play(){Directory.CreateDirectory("Logs/TruckSports/EighteenSports/Hide");File.WriteAllText("Logs/TruckSports/EighteenSports/Hide/result.txt","WAITING");EditorSceneManager.OpenScene(HideAuthoring.Scene);SessionState.SetBool("Sports.Hide.Probe",true);SessionState.SetBool("Sports.Hide.Exit",true);EditorApplication.playModeStateChanged+=Changed;EditorApplication.EnterPlaymode();}
  [InitializeOnLoadMethod]static void Restore(){if(SessionState.GetBool("Sports.Hide.Exit",false))EditorApplication.playModeStateChanged+=Changed;if(SessionState.GetBool("Sports.Hide.AdapterExit",false))EditorApplication.playModeStateChanged+=AdapterChanged;}
  static void AdapterChanged(PlayModeStateChange state){if(state!=PlayModeStateChange.EnteredEditMode||!SessionState.GetBool("Sports.Hide.AdapterExit",false))return;SessionState.SetBool("Sports.Hide.AdapterExit",false);EditorApplication.playModeStateChanged-=AdapterChanged;EditorApplication.Exit(File.ReadAllText("Logs/TruckSports/EighteenSports/HideAdapters/result.txt").Contains("ERRORS=0;")?0:2);}
  static void Changed(PlayModeStateChange state){if(state!=PlayModeStateChange.EnteredEditMode||!SessionState.GetBool("Sports.Hide.Exit",false))return;SessionState.SetBool("Sports.Hide.Exit",false);EditorApplication.playModeStateChanged-=Changed;EditorApplication.Exit(File.ReadAllText("Logs/TruckSports/EighteenSports/Hide/result.txt").Contains("ERRORS=0;")?0:2);}
 }
}
