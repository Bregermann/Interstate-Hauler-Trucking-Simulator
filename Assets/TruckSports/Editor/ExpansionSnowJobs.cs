using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace LWS.TruckSports.Editor
{
    public static class ExpansionSnowJobs
    {
        public static void Play() { Directory.CreateDirectory("Logs/TruckSports/EighteenSports/Snow"); File.WriteAllText("Logs/TruckSports/EighteenSports/Snow/result.txt", "WAITING"); EditorSceneManager.OpenScene("Assets/TruckRedLightGreenLight/Scenes/TruckRedLightGreenLight.unity"); SessionState.SetBool("Sports.ExpansionSnow.Probe", true); SessionState.SetBool("Sports.ExpansionSnow.Exit", true); EditorApplication.playModeStateChanged += Changed; EditorApplication.EnterPlaymode(); }
        [InitializeOnLoadMethod] static void Restore() { if (SessionState.GetBool("Sports.ExpansionSnow.Exit", false)) EditorApplication.playModeStateChanged += Changed; }
        static void Changed(PlayModeStateChange state) { if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool("Sports.ExpansionSnow.Exit", false)) return; SessionState.SetBool("Sports.ExpansionSnow.Exit", false); EditorApplication.playModeStateChanged -= Changed; EditorApplication.Exit(File.ReadAllText("Logs/TruckSports/EighteenSports/Snow/result.txt").Contains("ERRORS=0;") ? 0 : 2); }
    }
}
