#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Linq;
using UnityEngine;
namespace LWS.TruckSports
{
    public sealed class SportsExpansionSnowProbe : MonoBehaviour
    {
        const string Folder = "Logs/TruckSports/EighteenSports/Snow";
        IDisposable preferences; int checks, errors; readonly StringBuilder report = new StringBuilder();
        [RuntimeInitializeOnLoadMethod] static void Boot()
        {
            bool run = Array.IndexOf(Environment.GetCommandLineArgs(), "-tsExpansionSnow") >= 0;
#if UNITY_EDITOR
            run |= UnityEditor.SessionState.GetBool("Sports.ExpansionSnow.Probe", false); UnityEditor.SessionState.SetBool("Sports.ExpansionSnow.Probe", false);
#endif
            if (!run) return; var root = new GameObject("Explicit actual Weatherade six-sport snow acceptance"); DontDestroyOnLoad(root); root.AddComponent<SportsProbeInputIsolation>(); root.AddComponent<SportsExpansionSnowProbe>();
        }
        void Awake() { preferences = SportsPrefs.IsolateValidation("ExpansionSnow"); SportsParticipantRegistry.ForgetCache(); }
        void Check(bool ok, string message) { checks++; if (!ok) errors++; report.AppendLine((ok ? "PASS " : "FAIL ") + message); File.WriteAllText(Folder + "/progress.txt", report.ToString()); }
        void Error(string message, string stack, LogType type) { if (type == LogType.Error || type == LogType.Exception) { errors++; report.AppendLine("ERROR " + message + "\n" + stack); } }
        IEnumerator Start()
        {
            Directory.CreateDirectory(Folder); File.WriteAllText(Folder + "/result.txt", "RUNNING"); Application.logMessageReceived += Error;
            foreach (var scene in new[] { "TruckRedLightGreenLight", "TruckHotPotato", "TruckBalloonBattle", "TruckWhackAMole", "TruckKingOfTheHill", "TruckSeesaw" })
            {
                if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != scene) { SportsNavigation.LoadSport(scene); yield return null; yield return null; } yield return null; var match = FindAnyObjectByType<SportsEventSession>(); var weather = match.GetComponent<SportsChaosWeather>(); var snow = match.GetComponent<SportsSnowSurface>();
                var preset = weather.presets.entries.First(p => p.Snow && p.legacyKind == SportsWeatherKind.Snow); weather.presets.Select(preset.id);
                var views = match.GetComponent<SportsDrivingViews>(); SportsCameraOptions.Set(false); views.Select(0); match.setup.count = 2; match.AllBots(); for (int i = 0; i < 2; i++) match.setup.players[i].difficulty = LWS.TruckBowling.BowlingDifficulty.Hard;
                Check(match.gameObject.scene.name == scene && match.StartMatch() && snow != null, scene + " starts through shared explicit installed snow selection"); if (snow == null) continue;
                float until = Time.realtimeSinceStartup + 14; int contacts = 0; float coverage = 0;
                while (Time.realtimeSinceStartup < until && !match.Finished)
                { contacts = Mathf.Max(contacts, snow.Contacts); coverage = Mathf.Max(coverage, snow.Coverage); if (snow.surface.WheelTraceStampCount > 30 && coverage > .72f) break; yield return null; }
                Check(weather.PresetId == preset.id && snow.Falling && coverage >= .68f && snow.surface.GroundSnowSurfaceCount > 0, scene + " has actual accumulating Weatherade surfaces; coverage=" + coverage.ToString("F2") + " grounds=" + snow.surface.GroundSnowSurfaceCount);
                Check(contacts > 0 && snow.surface.WheelTraceStampCount > 0, scene + " moving real NWH tires produce actual Weatherade tracks; contacts=" + contacts + " stamps=" + snow.surface.WheelTraceStampCount);
                yield return SportsWindowCapture.Save(1280, 720, Folder + "/" + scene + "-snow-720.png"); match.SetupScreen();
            }
            report.AppendLine("CHECKS=" + checks + " ERRORS=" + errors + "; platform=" + Application.platform + "; capture=" + SportsWindowCapture.EvidenceKind + "; actual Weatherade coverage and NWH contact-generated tire tracks; no human/device/performance certification."); File.WriteAllText(Folder + "/result.txt", report.ToString()); Application.logMessageReceived -= Error;
#if UNITY_EDITOR
            if (Application.isEditor) UnityEditor.EditorApplication.isPlaying = false; else
#endif
                Application.Quit(errors == 0 ? 0 : 2);
        }
        void OnDestroy() { Application.logMessageReceived -= Error; preferences?.Dispose(); SportsParticipantRegistry.ForgetCache(); Time.timeScale = 1; }
    }
}
#endif
