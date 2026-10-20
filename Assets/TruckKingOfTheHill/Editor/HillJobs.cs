using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace LWS.TruckKingOfTheHill.Editor
{
    public static class HillJobs
    {
        public static void Play() { Directory.CreateDirectory("Logs/TruckSports/EighteenSports/Hill"); File.WriteAllText("Logs/TruckSports/EighteenSports/Hill/result.txt", "WAITING"); EditorSceneManager.OpenScene(HillAuthoring.Scene); SessionState.SetBool("Sports.Hill.Probe", true); SessionState.SetBool("Sports.Hill.Exit", true); EditorApplication.playModeStateChanged += Changed; EditorApplication.EnterPlaymode(); }
        [InitializeOnLoadMethod] static void Restore() { if (SessionState.GetBool("Sports.Hill.Exit", false)) EditorApplication.playModeStateChanged += Changed; }
        static void Changed(PlayModeStateChange state) { if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool("Sports.Hill.Exit", false)) return; SessionState.SetBool("Sports.Hill.Exit", false); EditorApplication.playModeStateChanged -= Changed; EditorApplication.Exit(File.ReadAllText("Logs/TruckSports/EighteenSports/Hill/result.txt").Contains("ERRORS=0;") ? 0 : 2); }
    }
}
