using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace LWS.TruckWhackAMole.Editor
{
    public static class WhackJobs
    {
        public static void Play() { Directory.CreateDirectory("Logs/TruckSports/EighteenSports/Whack"); File.WriteAllText("Logs/TruckSports/EighteenSports/Whack/result.txt", "WAITING"); EditorSceneManager.OpenScene(WhackAuthoring.Scene); SessionState.SetBool("Sports.Whack.Probe", true); SessionState.SetBool("Sports.Whack.Exit", true); EditorApplication.playModeStateChanged += Changed; EditorApplication.EnterPlaymode(); }
        [InitializeOnLoadMethod] static void Restore() { if (SessionState.GetBool("Sports.Whack.Exit", false)) EditorApplication.playModeStateChanged += Changed; }
        static void Changed(PlayModeStateChange state) { if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool("Sports.Whack.Exit", false)) return; SessionState.SetBool("Sports.Whack.Exit", false); EditorApplication.playModeStateChanged -= Changed; EditorApplication.Exit(File.ReadAllText("Logs/TruckSports/EighteenSports/Whack/result.txt").Contains("ERRORS=0;") ? 0 : 2); }
    }
}
