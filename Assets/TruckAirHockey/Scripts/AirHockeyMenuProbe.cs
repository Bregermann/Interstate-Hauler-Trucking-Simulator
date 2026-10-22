#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using LWS.TruckSports;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace LWS.TruckAirHockey
{
    public sealed class AirHockeyMenuProbe : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-tsAirMenus") < 0) return;
            var root = new GameObject("Explicit Air Hockey and Scrap menu Windows acceptance"); DontDestroyOnLoad(root); root.AddComponent<SportsProbeInputIsolation>(); root.AddComponent<AirHockeyMenuProbe>();
        }
        string folder; int checks, errors; IDisposable prefs; Keyboard keyboard; Gamepad first, second; InputSettings original, injected; readonly StringBuilder log = new StringBuilder();
        void Check(bool ok, string text) { checks++; if (!ok) errors++; log.AppendLine((ok ? "PASS " : "FAIL ") + text); File.WriteAllText(Path.Combine(folder, "progress.txt"), log.ToString()); }
        void Error(string message, string stack, LogType type) { if (type == LogType.Error || type == LogType.Exception) { errors++; log.AppendLine("ERROR " + message + "\n" + stack); } }
        void Finish() { log.AppendLine("CHECKS=" + checks + " ERRORS=" + errors + "; actual Windows AI match, menu launches and software-device fixtures; no physical-controller/human/listening acceptance"); File.WriteAllText(Path.Combine(folder, "result.txt"), log.ToString()); Application.Quit(errors == 0 ? 0 : 1); }
        IEnumerator Capture(string name) { yield return new WaitForSecondsRealtime(.65f); yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(folder, name + ".png")); yield return new WaitForSecondsRealtime(.15f); }
        static bool Inside(RectTransform rect) { var corners = new Vector3[4]; rect.GetWorldCorners(corners); return corners.All(p => p.x >= -1 && p.x <= Screen.width + 1 && p.y >= -1 && p.y <= Screen.height + 1); }
        IEnumerator Start()
        {
            var args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args, "-tsAirMenusOutput"); folder = index >= 0 ? args[index + 1] : SportsEvidencePaths.Folder("AirHockeyMenus"); Directory.CreateDirectory(folder);
            prefs = SportsPrefs.IsolateValidation("AirHockeyMenuWindows"); original = InputSystem.settings; injected = Instantiate(original); injected.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; InputSystem.settings = injected; keyboard = InputSystem.AddDevice<Keyboard>(); first = InputSystem.AddDevice<Gamepad>(); second = InputSystem.AddDevice<Gamepad>(); Application.logMessageReceived += Error;
            yield return SportsProbeScene.Ensure<AirHockeyMatch>("TruckAirHockey"); yield return null; var match = FindAnyObjectByType<AirHockeyMatch>(); var ui = match.menus.presentation; Check(ui != null, "saved scene has assigned new presentation");
            foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080), new Vector2Int(2560, 1080), new Vector2Int(1024, 768) })
            {
                Screen.SetResolution(size.x, size.y, false); yield return new WaitForSecondsRealtime(.2f); match.ChangeSetup(); match.Configure(AirHockeyMode.SharedKeyboard); yield return Capture("setup-" + size.x + "x" + size.y);
                foreach (var status in ui.setupStatus) Check(Inside((RectTransform)status.transform.parent), "both driver cards fit " + size.x + "x" + size.y);
                var setupLogo = match.menus.pages[0].transform.Find("Original sport title artwork").GetComponent<UnityEngine.UI.Image>();
                Check(match.menus.pages[0].GetComponentsInChildren<UnityEngine.UI.Image>().Count(i => i.sprite == setupLogo.sprite) == 1, "setup shows exactly one original sport logo");
                Check(match.StartMatch(), "two-keyboard fixture starts"); Time.timeScale = 0; ui.Refresh(); Canvas.ForceUpdateCanvases(); Check(Inside(ui.scoreboard) && Inside(ui.controls.rectTransform), "score/clock and bottom controls stay screen-safe"); yield return Capture("live-layout-" + size.x + "x" + size.y);
                var legacy = match.menus.hud.GetComponent<SportsArenaOverlay>(); Check(ui.controls.gameObject.activeInHierarchy && ui.scoreboard.gameObject.activeInHierarchy && (legacy == null || !legacy.enabled), "visible controls and one HUD layout authority");
                match.Pause(); ui.Refresh(); yield return Capture("pause-" + size.x + "x" + size.y); Check(!match.menus.hud.activeSelf, "pause hides gameplay score and retains paired scores"); match.Resume();
            }
            foreach (var mode in new[] { AirHockeyMode.PlayerAI, AirHockeyMode.SharedKeyboard, AirHockeyMode.SeparateControllers, AirHockeyMode.KeyboardController, AirHockeyMode.SharedController, AirHockeyMode.AIVsAI })
            {
                match.ChangeSetup(); match.Configure(mode); for (int i = 0; i < 2; i++) if (match.players[i].controller) Check(match.Assign(i, mode == AirHockeyMode.SharedController || i == 0 ? first : second), "existing software-pad ownership accepted " + mode + " slot " + i);
                Check(match.StartMatch(), "existing input configuration starts " + mode); Time.timeScale = 0; ui.Refresh(); Check(ui.names[0].text == match.players[0].name && ui.names[1].text == match.players[1].name, "UI reads current participant identities " + mode);
            }
            match.ChangeSetup(); match.Configure(AirHockeyMode.AIVsAI); Screen.SetResolution(1920, 1080, false); match.StartMatch(); Time.timeScale = 4; bool paused = false, goal = false; float until = Time.realtimeSinceStartup + 240;
            while (match.rules.Phase != AirHockeyPhase.Finished && Time.realtimeSinceStartup < until)
            {
                if (!paused && match.MatchSeconds > 1) { paused = true; match.Pause(); float clock = match.MatchSeconds; yield return new WaitForSecondsRealtime(.2f); Check(match.MatchSeconds == clock, "live-play clock and simulation stop during pause"); match.Resume(); Time.timeScale = 4; }
                if (!goal && match.rules.Goals > 0) { goal = true; Time.timeScale = 0; ui.Refresh(); Check(ui.points[0].text == match.rules.Score[0].ToString() && ui.points[1].text == match.rules.Score[1].ToString(), "actual physical goal updates both graphical scores"); yield return Capture("actual-goal-feedback"); Time.timeScale = 4; }
                yield return null;
            }
            Check(match.rules.Phase == AirHockeyPhase.Finished && match.rules.Score[match.rules.Winner] == 7, "complete ordinary two-AI physical first-to-seven match"); if (match.rules.Phase != AirHockeyPhase.Finished) { Finish(); yield break; } ui.Refresh(); Check(ui.resultRanks[match.rules.Winner].text == "WINNER", "graphical result identifies actual winner"); log.AppendLine("MATCH score=" + string.Join(",", match.rules.Score) + " goals=" + match.rules.Goals + " playSeconds=" + match.MatchSeconds); yield return Capture("actual-match-results");
            Check(match.StartMatch(true) && match.rules.Server == 1 && match.MatchSeconds == 0, "rematch clears scores/clock and preserves alternating opening serve"); match.ChangeSetup(); SportsNavigation.FrontEnd(); yield return null; yield return null; var front = FindAnyObjectByType<SportsFrontEnd>(); front.Action("scrapheap");
            var selected = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;
            Check(selected != null && new[] { "wallball", "logrolling", "dodgeball" }.Contains(selected.GetComponent<SportsMenuAction>()?.action), "Scrap focus starts on an accessible sport");
            foreach (int width in new[] { 1280, 1920 })
            { Screen.SetResolution(width, width == 1280 ? 720 : 1080, false); yield return Capture("scrap-heap-" + width); Check(front.pages[4].GetComponentsInChildren<SportsMenuAction>().Where(a => a.action != "scrapback").All(a => Inside(a.transform.parent as RectTransform)), "Scrap cards fit " + width); }
            foreach (string key in new[] { "wallball", "logrolling", "dodgeball" })
            { Check(!front.catalogue.cards.Any(c => c.GetComponentsInChildren<SportsMenuAction>(true).Any(a => a.action == key)), "archived key absent from active catalogue " + key); var action = front.GetComponentsInChildren<SportsMenuAction>(true).Single(a => a.action == key); Check(action.transform.IsChildOf(front.pages[4].transform) && action.gameObject.activeInHierarchy && Inside(action.transform.parent as RectTransform), "unique reachable Scrap card fits screen " + key); }
            foreach (var pair in new[] { new[] { "logrolling", SportsNavigation.LogRolling }, new[] { "dodgeball", SportsNavigation.Dodgeball } })
            { front.Action(pair[0]); float wait = Time.realtimeSinceStartup + 20; while (SceneManager.GetActiveScene().name != pair[1] && Time.realtimeSinceStartup < wait) yield return null; yield return null; Check(SceneManager.GetActiveScene().name == pair[1], "preserved Scrap game launches " + pair[0]); yield return Capture("preserved-" + pair[0] + "-setup"); SportsNavigation.FrontEnd(); yield return null; yield return null; front = FindAnyObjectByType<SportsFrontEnd>(); front.Action("scrapheap"); }
            front.Action("scrapback"); Check(front.Page == 1, "Scrap back returns to active sports and preserves menu flow");
            Finish();
        }
        void OnDestroy() { Application.logMessageReceived -= Error; prefs?.Dispose(); if (keyboard != null) InputSystem.RemoveDevice(keyboard); if (first != null) InputSystem.RemoveDevice(first); if (second != null) InputSystem.RemoveDevice(second); if (original != null) InputSystem.settings = original; if (injected != null) Destroy(injected); Time.timeScale = 1; }
    }
}
#endif
