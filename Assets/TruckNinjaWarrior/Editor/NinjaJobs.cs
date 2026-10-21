using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace LWS.TruckNinjaWarrior.Editor
{
 public static class NinjaJobs
 {
  public static void Wall(){SessionState.SetBool("Sports.Ninja.WallDiagnostic",true);Play();}
  public static void Play(){Directory.CreateDirectory("Logs/TruckSports/EighteenSports/Ninja");File.WriteAllText("Logs/TruckSports/EighteenSports/Ninja/result.txt","WAITING");EditorSceneManager.OpenScene(NinjaAuthoring.Scene);SessionState.SetBool("Sports.Ninja.Probe",true);SessionState.SetBool("Sports.Ninja.Exit",true);EditorApplication.playModeStateChanged+=Changed;EditorApplication.EnterPlaymode();}
  [InitializeOnLoadMethod]static void Restore(){if(SessionState.GetBool("Sports.Ninja.Exit",false))EditorApplication.playModeStateChanged+=Changed;}
  static void Changed(PlayModeStateChange state){if(state!=PlayModeStateChange.EnteredEditMode||!SessionState.GetBool("Sports.Ninja.Exit",false))return;SessionState.SetBool("Sports.Ninja.Exit",false);EditorApplication.playModeStateChanged-=Changed;EditorApplication.Exit(File.ReadAllText("Logs/TruckSports/EighteenSports/Ninja/result.txt").Contains("ERRORS=0;")?0:2);}
 }
}
