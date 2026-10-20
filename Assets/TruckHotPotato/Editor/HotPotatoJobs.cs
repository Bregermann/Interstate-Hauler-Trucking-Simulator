using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace LWS.TruckHotPotato.Editor
{
    public static class HotPotatoJobs
    {
        public static void Play() { Directory.CreateDirectory("Logs/TruckSports/EighteenSports/HotPotato"); File.WriteAllText("Logs/TruckSports/EighteenSports/HotPotato/result.txt", "WAITING"); EditorSceneManager.OpenScene(HotPotatoAuthoring.Scene); SessionState.SetBool("Sports.HotPotato.Probe", true); SessionState.SetBool("Sports.HotPotato.Exit", true); EditorApplication.playModeStateChanged += Changed; EditorApplication.EnterPlaymode(); }
        [InitializeOnLoadMethod] static void Restore() { if (SessionState.GetBool("Sports.HotPotato.Exit", false)) EditorApplication.playModeStateChanged += Changed; }
        static void Changed(PlayModeStateChange state)
        { if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool("Sports.HotPotato.Exit", false)) return; SessionState.SetBool("Sports.HotPotato.Exit", false); EditorApplication.playModeStateChanged -= Changed; var text = File.ReadAllText("Logs/TruckSports/EighteenSports/HotPotato/result.txt"); EditorApplication.Exit(text.Contains("ERRORS=0;") ? 0 : 2); }
    }
}
