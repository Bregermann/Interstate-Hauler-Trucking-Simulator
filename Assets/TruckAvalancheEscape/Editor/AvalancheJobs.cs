using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace LWS.TruckAvalancheEscape.Editor
{
 public static class AvalancheJobs
 {
  public static void Play(){Directory.CreateDirectory("Logs/TruckSports/EighteenSports/Avalanche");File.WriteAllText("Logs/TruckSports/EighteenSports/Avalanche/result.txt","WAITING");EditorSceneManager.OpenScene(AvalancheAuthoring.Scene);SessionState.SetBool("Sports.Avalanche.Probe",true);SessionState.SetBool("Sports.Avalanche.Exit",true);EditorApplication.playModeStateChanged+=Changed;EditorApplication.EnterPlaymode();}
  [InitializeOnLoadMethod]static void Restore(){if(SessionState.GetBool("Sports.Avalanche.Exit",false))EditorApplication.playModeStateChanged+=Changed;}
  static void Changed(PlayModeStateChange state){if(state!=PlayModeStateChange.EnteredEditMode||!SessionState.GetBool("Sports.Avalanche.Exit",false))return;SessionState.SetBool("Sports.Avalanche.Exit",false);EditorApplication.playModeStateChanged-=Changed;EditorApplication.Exit(File.ReadAllText("Logs/TruckSports/EighteenSports/Avalanche/result.txt").Contains("ERRORS=0;")?0:2);}
 }
}
