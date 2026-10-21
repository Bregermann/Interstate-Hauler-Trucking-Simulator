using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace LWS.TruckFrogger.Editor
{
 public static class FrogJobs
 {
  public static void RailRiver(){SessionState.SetBool("Sports.Frog.RailRiver",true);Play();}
  public static void Play(){Directory.CreateDirectory("Logs/TruckSports/EighteenSports/Frog");File.WriteAllText("Logs/TruckSports/EighteenSports/Frog/result.txt","WAITING");EditorSceneManager.OpenScene(FrogAuthoring.Scene);SessionState.SetBool("Sports.Frog.Probe",true);SessionState.SetBool("Sports.Frog.Exit",true);EditorApplication.playModeStateChanged+=Changed;EditorApplication.EnterPlaymode();}
  [InitializeOnLoadMethod]static void Restore(){if(SessionState.GetBool("Sports.Frog.Exit",false))EditorApplication.playModeStateChanged+=Changed;}
  static void Changed(PlayModeStateChange state){if(state!=PlayModeStateChange.EnteredEditMode||!SessionState.GetBool("Sports.Frog.Exit",false))return;SessionState.SetBool("Sports.Frog.Exit",false);EditorApplication.playModeStateChanged-=Changed;EditorApplication.Exit(File.ReadAllText("Logs/TruckSports/EighteenSports/Frog/result.txt").Contains("ERRORS=0;")?0:2);}
 }
}
