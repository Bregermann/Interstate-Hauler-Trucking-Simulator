#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using LWS.TruckSports;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LWS.TruckBowling
{
    public sealed class BowlingHudProbe : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-tsBowlingHud") < 0) return;
            var root = new GameObject("Explicit Bowling HUD Windows acceptance"); DontDestroyOnLoad(root); root.AddComponent<SportsProbeInputIsolation>(); root.AddComponent<BowlingHudProbe>();
        }
        string folder; int checks, errors; Keyboard keyboard; IDisposable preferences; InputSettings originalInput, probeInput; readonly StringBuilder log = new StringBuilder();
        void Check(bool ok, string label) { checks++; if (!ok) errors++; log.AppendLine((ok ? "PASS " : "FAIL ") + label); File.WriteAllText(Path.Combine(folder, "progress.txt"), log.ToString()); }
        void Error(string message, string stack, LogType type) { if (type == LogType.Error || type == LogType.Exception) { errors++; log.AppendLine("ERROR " + message + "\n" + stack); } }
        IEnumerator Capture(string name) { yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(folder, name + ".png")); yield return new WaitForSecondsRealtime(.15f); }
        IEnumerator Start()
        {
            var args = Environment.GetCommandLineArgs(); int output = Array.IndexOf(args, "-tsBowlingHudOutput"); folder = output >= 0 ? args[output + 1] : SportsEvidencePaths.Folder("BowlingHud"); Directory.CreateDirectory(folder); preferences = SportsPrefs.IsolateValidation("BowlingHudWindows"); originalInput = InputSystem.settings; probeInput = Instantiate(originalInput); probeInput.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; InputSystem.settings = probeInput; keyboard = InputSystem.AddDevice<Keyboard>(); Application.logMessageReceived += Error;
            yield return SportsProbeScene.Ensure<BowlingMatch>("TruckBowling"); yield return null; var match = FindAnyObjectByType<BowlingMatch>(); var ui = match.menus.presentation;
            Check(ui != null && ui.portraits.Length == 4 && ui.progress.All(p => p.barImage.sprite != null), "saved four-lane HUD has assigned portraits and actual Heat progress graphics");
            foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080) })
            {
                Screen.SetResolution(size.x, size.y, false); yield return new WaitForSecondsRealtime(.2f);
                for (int humans = 0; humans <= 4; humans++) foreach (bool showAi in new[] { false, true })
                {
                    match.ChangeSetup(); match.PresetAI(4, BowlingMode.Simultaneous); for (int i = 0; i < humans; i++) match.setup.players[i].source = BowlingSource.Keyboard;
                    SportsCameraOptions.Set(showAi); Check(match.StartMatch(), "start " + humans + " software-human views / AI cameras " + showAi + " at " + size.x); Time.timeScale = 0; yield return new WaitForSecondsRealtime(.2f); Canvas.ForceUpdateCanvases();
                    int expected = showAi ? 4 : Mathf.Max(1, humans); Check(match.lanes.Count(l => l.cab.view.enabled) == expected, "existing viewport policy preserved");
                    bool inside = true, owners = true; var corners = new Vector3[4];
                    for (int i = 0; i < 4; i++) if (match.lanes[i].cab.view.enabled)
                    {
                        ui.RefreshLane(i); owners &= match.menus.headers[i].text == match.lanes[i].Owner.name && ui.totals[i].text == match.lanes[i].Owner.score.ResolvedTotal.ToString();
                        var view = match.lanes[i].cab.view.pixelRect;
                        foreach (var rect in new[] { match.menus.headers[i].rectTransform.parent as RectTransform, match.menus.guidance[i].rectTransform.parent as RectTransform })
                        { rect.GetWorldCorners(corners); inside &= corners.All(p => p.x >= view.xMin - 1 && p.x <= view.xMax + 1 && p.y >= view.yMin - 1 && p.y <= view.yMax + 1); }
                    }
                    Check(inside && owners, "each visible HUD belongs to its lane and stays within its actual viewport");
                    if (!showAi && (humans == 1 || humans == 2 || humans == 4)) yield return Capture("saved-layout-" + humans + "-human-" + size.x);
                }
            }
            match.ChangeSetup(); match.PresetAI(4, BowlingMode.Simultaneous); SportsCameraOptions.Set(true); SportsPrefs.SetInt(BowlingHighlights.Preference, 0); match.StartMatch(); Time.timeScale = 4;
            bool driving = false, result = false; float until = Time.realtimeSinceStartup + 280;
            while (!match.Finished && Time.realtimeSinceStartup < until)
            {
                if (!driving && match.lanes.Any(l => l.Phase == BowlingPhase.Rolling)) { driving = true; yield return Capture("actual-four-AI-driving"); }
                if (!result && match.lanes.Any(l => l.ScoreLocks > 0)) { result = true; yield return Capture("actual-physical-pinfall"); }
                yield return null;
            }
            Check(match.Finished && match.Winners.Length > 0, "complete ordinary four-AI ten-frame physical match reaches results");
            Check(match.setup.players.Take(4).All(p => p.score.Finished) && match.lanes.Any(l => l.LegalPinContact || l.truck.PinImpacts > 0), "all independent scorecards finish through real truck/pin contacts");
            log.AppendLine("MATCH totals=" + string.Join(",", match.setup.players.Take(4).Select(p => p.score.ResolvedTotal)) + " deliveries=" + string.Join(",", match.setup.players.Take(4).Select(p => p.score.Deliveries)));
            yield return Capture("actual-four-AI-results"); match.ChangeSetup(); Check(match.StartMatch(), "rematch starts"); Time.timeScale = 0; yield return new WaitForSecondsRealtime(.2f); Check(match.setup.players.Take(4).All(p => p.score.Deliveries == 0) && ui.totals.All(t => t.text == "0"), "rematch resets score cards without changing lane ownership");
            match.Pause(); Check(match.menus.viewRoots.All(r => !r.gameObject.activeSelf), "existing pause hides all gameplay HUDs"); match.Resume();
            match.ChangeSetup(); SportsNavigation.FrontEnd(); yield return null; yield return null; Check(FindAnyObjectByType<BowlingMatch>() == null, "menu return cleans Bowling scene and added graphics");
            log.AppendLine("CHECKS=" + checks + " ERRORS=" + errors + "; actual Windows four-AI match and labelled software-input layout fixtures; no human/hardware/listening acceptance"); File.WriteAllText(Path.Combine(folder, "result.txt"), log.ToString()); Application.Quit(errors == 0 ? 0 : 1);
        }
        void OnDestroy() { Application.logMessageReceived -= Error; preferences?.Dispose(); if (keyboard != null) InputSystem.RemoveDevice(keyboard); if (originalInput != null) InputSystem.settings = originalInput; if (probeInput != null) Destroy(probeInput); Time.timeScale = 1; }
    }
}
#endif
