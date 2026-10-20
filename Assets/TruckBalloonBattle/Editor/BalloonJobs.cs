using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace LWS.TruckBalloonBattle.Editor
{
    public static class BalloonJobs
    {
        public static void Play() { Directory.CreateDirectory("Logs/TruckSports/EighteenSports/Balloon"); File.WriteAllText("Logs/TruckSports/EighteenSports/Balloon/result.txt", "WAITING"); EditorSceneManager.OpenScene(BalloonAuthoring.Scene); SessionState.SetBool("Sports.Balloon.Probe", true); SessionState.SetBool("Sports.Balloon.Exit", true); EditorApplication.playModeStateChanged += Changed; EditorApplication.EnterPlaymode(); }
        [InitializeOnLoadMethod] static void Restore() { if (SessionState.GetBool("Sports.Balloon.Exit", false)) EditorApplication.playModeStateChanged += Changed; }
        static void Changed(PlayModeStateChange state) { if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool("Sports.Balloon.Exit", false)) return; SessionState.SetBool("Sports.Balloon.Exit", false); EditorApplication.playModeStateChanged -= Changed; EditorApplication.Exit(File.ReadAllText("Logs/TruckSports/EighteenSports/Balloon/result.txt").Contains("ERRORS=0;") ? 0 : 2); }
    }
}
