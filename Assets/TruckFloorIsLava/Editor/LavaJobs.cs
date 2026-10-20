using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace LWS.TruckFloorIsLava.Editor
{
    public static class LavaJobs
    {
        public static void Ram(){SessionState.SetBool("Sports.Lava.RamOnly",true);Play();}
        public static void Blizzard(){SessionState.SetBool("Sports.Lava.Blizzard",true);Play();}
        public static void Play(){Directory.CreateDirectory("Logs/TruckSports/EighteenSports/Lava");File.WriteAllText("Logs/TruckSports/EighteenSports/Lava/result.txt","WAITING");EditorSceneManager.OpenScene(LavaAuthoring.Scene);SessionState.SetBool("Sports.Lava.Probe",true);SessionState.SetBool("Sports.Lava.Exit",true);EditorApplication.playModeStateChanged+=Changed;EditorApplication.EnterPlaymode();}
        [InitializeOnLoadMethod]static void Restore(){if(SessionState.GetBool("Sports.Lava.Exit",false))EditorApplication.playModeStateChanged+=Changed;}
        static void Changed(PlayModeStateChange state){if(state!=PlayModeStateChange.EnteredEditMode||!SessionState.GetBool("Sports.Lava.Exit",false))return;SessionState.SetBool("Sports.Lava.Exit",false);EditorApplication.playModeStateChanged-=Changed;EditorApplication.Exit(File.ReadAllText("Logs/TruckSports/EighteenSports/Lava/result.txt").Contains("ERRORS=0;")?0:2);}
    }
}
