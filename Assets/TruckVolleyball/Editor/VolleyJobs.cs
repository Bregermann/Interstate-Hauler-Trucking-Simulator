using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace LWS.TruckVolleyball.Editor
{
    public static class VolleyJobs
    {
        public static void Play(){Directory.CreateDirectory("Logs/TruckSports/EighteenSports/Volley");File.WriteAllText("Logs/TruckSports/EighteenSports/Volley/result.txt","WAITING");EditorSceneManager.OpenScene(VolleyAuthoring.Scene);SessionState.SetBool("Sports.Volley.Probe",true);SessionState.SetBool("Sports.Volley.Exit",true);EditorApplication.playModeStateChanged+=Changed;EditorApplication.EnterPlaymode();}
        [InitializeOnLoadMethod]static void Restore(){if(SessionState.GetBool("Sports.Volley.Exit",false))EditorApplication.playModeStateChanged+=Changed;}
        static void Changed(PlayModeStateChange state){if(state!=PlayModeStateChange.EnteredEditMode||!SessionState.GetBool("Sports.Volley.Exit",false))return;SessionState.SetBool("Sports.Volley.Exit",false);EditorApplication.playModeStateChanged-=Changed;EditorApplication.Exit(File.ReadAllText("Logs/TruckSports/EighteenSports/Volley/result.txt").Contains("ERRORS=0;")?0:2);}
    }
}
