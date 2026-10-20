using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace LWS.TruckTetherball.Editor
{
    public static class TetherJobs
    {
        public static void Play(){Directory.CreateDirectory("Logs/TruckSports/EighteenSports/Tether");File.WriteAllText("Logs/TruckSports/EighteenSports/Tether/result.txt","WAITING");EditorSceneManager.OpenScene(TetherAuthoring.Scene);SessionState.SetBool("Sports.Tether.Probe",true);SessionState.SetBool("Sports.Tether.Exit",true);EditorApplication.playModeStateChanged+=Changed;EditorApplication.EnterPlaymode();}
        [InitializeOnLoadMethod]static void Restore(){if(SessionState.GetBool("Sports.Tether.Exit",false))EditorApplication.playModeStateChanged+=Changed;}
        static void Changed(PlayModeStateChange state){if(state!=PlayModeStateChange.EnteredEditMode||!SessionState.GetBool("Sports.Tether.Exit",false))return;SessionState.SetBool("Sports.Tether.Exit",false);EditorApplication.playModeStateChanged-=Changed;EditorApplication.Exit(File.ReadAllText("Logs/TruckSports/EighteenSports/Tether/result.txt").Contains("ERRORS=0;")?0:2);}
    }
}
