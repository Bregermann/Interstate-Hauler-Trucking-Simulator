using System.IO;using UnityEditor;using UnityEditor.SceneManagement;
namespace LWS.TruckSheepHerding.Editor
{
 public static class HerdJobs
 {
  public static void Skill(){SessionState.SetBool("Sports.Herd.Skill",true);Play();}
  public static void Play(){Directory.CreateDirectory("Logs/TruckSports/EighteenSports/Herd");File.WriteAllText("Logs/TruckSports/EighteenSports/Herd/result.txt","WAITING");EditorSceneManager.OpenScene(HerdAuthoring.Scene);SessionState.SetBool("Sports.Herd.Probe",true);SessionState.SetBool("Sports.Herd.Exit",true);EditorApplication.playModeStateChanged+=Changed;EditorApplication.EnterPlaymode();}
  [InitializeOnLoadMethod]static void Restore(){if(SessionState.GetBool("Sports.Herd.Exit",false))EditorApplication.playModeStateChanged+=Changed;}
  static void Changed(PlayModeStateChange state){if(state!=PlayModeStateChange.EnteredEditMode||!SessionState.GetBool("Sports.Herd.Exit",false))return;SessionState.SetBool("Sports.Herd.Exit",false);EditorApplication.playModeStateChanged-=Changed;EditorApplication.Exit(File.ReadAllText("Logs/TruckSports/EighteenSports/Herd/result.txt").Contains("ERRORS=0;")?0:2);}
 }
}
