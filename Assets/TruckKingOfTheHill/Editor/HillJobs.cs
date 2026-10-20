using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace LWS.TruckKingOfTheHill.Editor
{
    public static class HillJobs
    {
        public static void Hud() => Begin(true);
        public static void Play() => Begin(false);
        static string Folder => "Logs/TruckSports/EighteenSports/" + (SessionState.GetBool("Sports.Hill.ExitHud", false) ? "HillHud" : "Hill");
        static void Begin(bool hud)
        {
            SessionState.SetBool("Sports.Hill.ExitHud", hud);
            SessionState.SetBool("Sports.Hill.HudProbe", hud);
            Directory.CreateDirectory(Folder); File.WriteAllText(Folder + "/result.txt", "WAITING");
            EditorSceneManager.OpenScene(HillAuthoring.Scene);
            SessionState.SetBool("Sports.Hill.Probe", true); SessionState.SetBool("Sports.Hill.Exit", true);
            EditorApplication.playModeStateChanged += Changed; EditorApplication.EnterPlaymode();
        }
        [InitializeOnLoadMethod] static void Restore() { if (SessionState.GetBool("Sports.Hill.Exit", false)) EditorApplication.playModeStateChanged += Changed; }
        static void Changed(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool("Sports.Hill.Exit", false)) return;
            SessionState.SetBool("Sports.Hill.Exit", false); EditorApplication.playModeStateChanged -= Changed;
            EditorApplication.Exit(File.ReadAllText(Folder + "/result.txt").Contains("ERRORS=0;") ? 0 : 2);
        }
    }
}
