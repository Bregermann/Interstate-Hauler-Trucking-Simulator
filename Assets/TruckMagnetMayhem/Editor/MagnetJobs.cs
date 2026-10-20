using System.IO;using UnityEditor;using UnityEditor.SceneManagement;
namespace LWS.TruckMagnetMayhem.Editor
{
 public static class MagnetJobs
 {
  public static void Play(){Directory.CreateDirectory("Logs/TruckSports/EighteenSports/Magnet");File.WriteAllText("Logs/TruckSports/EighteenSports/Magnet/result.txt","WAITING");EditorSceneManager.OpenScene(MagnetAuthoring.Scene);SessionState.SetBool("Sports.Magnet.Probe",true);SessionState.SetBool("Sports.Magnet.Exit",true);EditorApplication.playModeStateChanged+=Changed;EditorApplication.EnterPlaymode();}
  [InitializeOnLoadMethod]static void Restore(){if(SessionState.GetBool("Sports.Magnet.Exit",false))EditorApplication.playModeStateChanged+=Changed;}
  static void Changed(PlayModeStateChange state){if(state!=PlayModeStateChange.EnteredEditMode||!SessionState.GetBool("Sports.Magnet.Exit",false))return;SessionState.SetBool("Sports.Magnet.Exit",false);EditorApplication.playModeStateChanged-=Changed;EditorApplication.Exit(File.ReadAllText("Logs/TruckSports/EighteenSports/Magnet/result.txt").Contains("ERRORS=0;")?0:2);}
 }
}
