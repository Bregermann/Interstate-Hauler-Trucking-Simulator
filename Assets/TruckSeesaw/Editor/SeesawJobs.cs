using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace LWS.TruckSeesaw.Editor
{
    public static class SeesawJobs
    {
        public static void Play(){Directory.CreateDirectory("Logs/TruckSports/EighteenSports/Seesaw");File.WriteAllText("Logs/TruckSports/EighteenSports/Seesaw/result.txt","WAITING");EditorSceneManager.OpenScene(SeesawAuthoring.Scene);SessionState.SetBool("Sports.Seesaw.Probe",true);SessionState.SetBool("Sports.Seesaw.Exit",true);EditorApplication.playModeStateChanged+=Changed;EditorApplication.EnterPlaymode();}
        [InitializeOnLoadMethod]static void Restore(){if(SessionState.GetBool("Sports.Seesaw.Exit",false))EditorApplication.playModeStateChanged+=Changed;}
        static void Changed(PlayModeStateChange state){if(state!=PlayModeStateChange.EnteredEditMode||!SessionState.GetBool("Sports.Seesaw.Exit",false))return;SessionState.SetBool("Sports.Seesaw.Exit",false);EditorApplication.playModeStateChanged-=Changed;EditorApplication.Exit(File.ReadAllText("Logs/TruckSports/EighteenSports/Seesaw/result.txt").Contains("ERRORS=0;")?0:2);}
    }
}
