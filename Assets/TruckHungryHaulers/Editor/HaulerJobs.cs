using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace LWS.TruckHungryHaulers.Editor
{
    public static class HaulerJobs
    {
        public static void Intake(){SessionState.SetBool("Sports.Hauler.Intake",true);Play();}
        public static void Play(){Directory.CreateDirectory("Logs/TruckSports/EighteenSports/Hauler");File.WriteAllText("Logs/TruckSports/EighteenSports/Hauler/result.txt","WAITING");EditorSceneManager.OpenScene(HaulerAuthoring.Scene);SessionState.SetBool("Sports.Hauler.Probe",true);SessionState.SetBool("Sports.Hauler.Exit",true);EditorApplication.playModeStateChanged+=Changed;EditorApplication.EnterPlaymode();}
        [InitializeOnLoadMethod]static void Restore(){if(SessionState.GetBool("Sports.Hauler.Exit",false))EditorApplication.playModeStateChanged+=Changed;}
        static void Changed(PlayModeStateChange state){if(state!=PlayModeStateChange.EnteredEditMode||!SessionState.GetBool("Sports.Hauler.Exit",false))return;SessionState.SetBool("Sports.Hauler.Exit",false);EditorApplication.playModeStateChanged-=Changed;EditorApplication.Exit(File.ReadAllText("Logs/TruckSports/EighteenSports/Hauler/result.txt").Contains("ERRORS=0;")?0:2);}
    }
}
