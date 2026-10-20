using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace LWS.TruckFreezeTag.Editor
{
    public static class FreezeJobs
    {
        public static void Play(){Directory.CreateDirectory("Logs/TruckSports/EighteenSports/Freeze");File.WriteAllText("Logs/TruckSports/EighteenSports/Freeze/result.txt","WAITING");EditorSceneManager.OpenScene(FreezeAuthoring.Scene);SessionState.SetBool("Sports.Freeze.Probe",true);SessionState.SetBool("Sports.Freeze.Exit",true);EditorApplication.playModeStateChanged+=Changed;EditorApplication.EnterPlaymode();}
        [InitializeOnLoadMethod]static void Restore(){if(SessionState.GetBool("Sports.Freeze.Exit",false))EditorApplication.playModeStateChanged+=Changed;}
        static void Changed(PlayModeStateChange state){if(state!=PlayModeStateChange.EnteredEditMode||!SessionState.GetBool("Sports.Freeze.Exit",false))return;SessionState.SetBool("Sports.Freeze.Exit",false);EditorApplication.playModeStateChanged-=Changed;EditorApplication.Exit(File.ReadAllText("Logs/TruckSports/EighteenSports/Freeze/result.txt").Contains("ERRORS=0;")?0:2);}
    }
}
