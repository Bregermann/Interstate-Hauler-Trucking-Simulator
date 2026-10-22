#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using LWS.TruckSports;

namespace LWS.TruckKickball
{
    public sealed class KickballPacingRestoreProbe : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-tsKickballPacingRestore") < 0) return;
            var root = new GameObject("Explicit saved Kickball pacing restart"); DontDestroyOnLoad(root);
            root.AddComponent<SportsProbeInputIsolation>(); root.AddComponent<KickballPacingRestoreProbe>();
        }
        IDisposable preferences; int checks, errors; string folder; readonly StringBuilder log = new StringBuilder();
        void Check(bool value, string text) { checks++; if (!value) errors++; log.AppendLine((value ? "PASS " : "FAIL ") + text); }
        IEnumerator Start()
        {
            var args = Environment.GetCommandLineArgs(); int at = Array.IndexOf(args, "-tsKickballOutput"); folder = args[at + 1]; Directory.CreateDirectory(folder);
            preferences = SportsPrefs.IsolateValidation("KickballWaitingRepair");
            Check(KickballAIPlayback.Enabled, "new Windows process restores the preceding run's saved fast-forward preference");
            yield return SportsProbeScene.Ensure<KickballMatch>(SportsNavigation.Kickball); yield return null;
            var match = FindAnyObjectByType<KickballMatch>(); match.AllBots(); match.option = 3;
            match.RefreshExtraUI(); Check(match.paceChoice.text.Contains("2×") && match.pausePaceChoice.text.Contains("2×"), "both real menus restore the selected pacing caption after restart");
            Check(match.StartMatch(), "AI-only match launches after application restart"); yield return null;
            Check(match.aiPlayback.Accelerating && Time.timeScale == 2, "saved option drives actual AI-only simulation after restart");
            match.Pause(); yield return null; Check(Time.timeScale == 0, "saved acceleration respects pause after restart");
            match.Resume(); yield return null; Check(match.aiPlayback.Accelerating && Time.timeScale == 2, "saved acceleration resumes through the ordinary pause flow");
            match.SetupScreen(); match.ExtraAction("aipace"); yield return null;
            Check(Time.timeScale == 1 && !KickballAIPlayback.Enabled, "normal selection is saved and releases time ownership");
            log.AppendLine("CHECKS=" + checks + " ERRORS=" + errors); File.WriteAllText(Path.Combine(folder, "result.txt"), log.ToString()); Application.Quit(errors == 0 ? 0 : 1);
        }
        void OnDestroy() { preferences?.Dispose(); Time.timeScale = 1; }
    }
}
#endif
