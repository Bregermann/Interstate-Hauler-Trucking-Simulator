using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace LWS.TruckRedLightGreenLight.Editor
{
    public static class RedLightJobs
    {
        const string Result = "Logs/TruckSports/EighteenSports/RedLight/result.txt";
        public static void Play()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Result)); File.WriteAllText(Result, "WAITING");
            EditorSceneManager.OpenScene(RedLightAuthoring.Scene); SessionState.SetBool("Sports.RedLight.Probe", true); SessionState.SetBool("Sports.RedLight.Exit", true);
            EditorApplication.playModeStateChanged += Changed; EditorApplication.EnterPlaymode();
        }
        [InitializeOnLoadMethod] static void Restore() { if (SessionState.GetBool("Sports.RedLight.Exit", false)) EditorApplication.playModeStateChanged += Changed; }
        static void Changed(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode) SessionState.SetBool("Sports.RedLight.Exit", true);
            if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool("Sports.RedLight.Exit", false)) return;
            SessionState.SetBool("Sports.RedLight.Exit", false); EditorApplication.playModeStateChanged -= Changed;
            string text = File.Exists(Result) ? File.ReadAllText(Result) : "";
            EditorApplication.Exit(text.Contains("ERRORS=0;") ? 0 : 2);
        }
    }
}
