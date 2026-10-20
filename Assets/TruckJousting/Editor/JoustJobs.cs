using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace LWS.TruckJousting.Editor
{
    public static class JoustJobs
    {
        public static void Play(){Directory.CreateDirectory("Logs/TruckSports/EighteenSports/Joust");File.WriteAllText("Logs/TruckSports/EighteenSports/Joust/result.txt","WAITING");EditorSceneManager.OpenScene(JoustAuthoring.Scene);SessionState.SetBool("Sports.Joust.Probe",true);SessionState.SetBool("Sports.Joust.Exit",true);EditorApplication.playModeStateChanged+=Changed;EditorApplication.EnterPlaymode();}
        [InitializeOnLoadMethod]static void Restore(){if(SessionState.GetBool("Sports.Joust.Exit",false))EditorApplication.playModeStateChanged+=Changed;}
        static void Changed(PlayModeStateChange state){if(state!=PlayModeStateChange.EnteredEditMode||!SessionState.GetBool("Sports.Joust.Exit",false))return;SessionState.SetBool("Sports.Joust.Exit",false);EditorApplication.playModeStateChanged-=Changed;EditorApplication.Exit(File.ReadAllText("Logs/TruckSports/EighteenSports/Joust/result.txt").Contains("ERRORS=0;")?0:2);}
    }
}
