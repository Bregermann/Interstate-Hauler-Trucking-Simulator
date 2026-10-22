#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using LWS.TruckSports;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LWS.TruckBilliards
{
    public sealed class BilliardsHudProbe : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-tsBilliardsHud") < 0) return;
            var root = new GameObject("Explicit Billiards HUD Windows acceptance"); DontDestroyOnLoad(root); root.AddComponent<SportsProbeInputIsolation>(); root.AddComponent<BilliardsHudProbe>();
        }
        int checks, errors; string folder; IDisposable preferences; Keyboard keyboard; readonly StringBuilder log = new StringBuilder();
        void Check(bool value, string text) { checks++; if (!value) errors++; log.AppendLine((value ? "PASS " : "FAIL ") + text); File.WriteAllText(Path.Combine(folder, "progress.txt"), log.ToString()); }
        void Error(string message, string stack, LogType type) { if (type == LogType.Error || type == LogType.Exception) { errors++; log.AppendLine("ERROR " + message + "\n" + stack); } }
        IEnumerator Capture(string name) { yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(folder, name + ".png")); yield return new WaitForSecondsRealtime(.15f); }
        static string State(BilliardsMatch m) => m.rules.Turn + ":" + m.rules.Phase + ":" + m.rules.ShotId + ":" + string.Join(",", m.rules.Pocketed.OrderBy(n => n)) + ":" + string.Join(",", m.session.series.Wins);
        IEnumerator Start()
        {
            var args = Environment.GetCommandLineArgs(); int output = Array.IndexOf(args, "-tsBilliardsHudOutput"); folder = output >= 0 ? args[output + 1] : SportsEvidencePaths.Folder("BilliardsHud"); Directory.CreateDirectory(folder);
            preferences = SportsPrefs.IsolateValidation("BilliardsHudWindows"); keyboard = InputSystem.AddDevice<Keyboard>(); Application.logMessageReceived += Error;
            yield return SportsProbeScene.Ensure<BilliardsMatch>("TruckBilliards"); yield return null;
            var match = FindAnyObjectByType<BilliardsMatch>(); var session = match.session; var hud = session.hud; var ui = hud.presentation;
            Check(ui != null && ui.ballFaces.Length == 30 && ui.ballNumbers.Length == 30, "saved scene has complete independent ball-objective cards");
            session.SelectMode(BilliardsMode.AIVsAI); session.StartMatch(); Time.timeScale = 0; hud.Refresh();
            string before = State(match); for (int i = 0; i < 100; i++) hud.Refresh(); Check(before == State(match), "repeated HUD refresh cannot change turn, shots, balls or series score");
            foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080), new Vector2Int(2560, 1080), new Vector2Int(1024, 768) })
            {
                Screen.SetResolution(size.x, size.y, false); yield return new WaitForSecondsRealtime(.25f); hud.Refresh(); Canvas.ForceUpdateCanvases();
                var corners = new Vector3[4]; bool inside = true;
                foreach (var rect in ui.cards.Concat(new[] { ui.rack.rectTransform.parent as RectTransform, ui.turn.rectTransform.parent as RectTransform, ui.power.transform.parent as RectTransform, ui.bindings.rectTransform }))
                { rect.GetWorldCorners(corners); inside &= corners.All(p => p.x >= -1 && p.x <= Screen.width + 1 && p.y >= -1 && p.y <= Screen.height + 1); }
                Check(inside, "all compact cards stay inside " + size.x + "x" + size.y); yield return Capture("open-table-" + size.x + "x" + size.y);
            }
            match.rules.BeginShot(); match.rules.Contact(1); match.rules.Pocket(1, 1); match.rules.Resolve(); hud.Refresh();
            Check(ui.groups[0].text == "SOLIDS" && ui.groups[1].text == "STRIPES" && ui.remaining[0].text == "6 REMAINING" && ui.remaining[1].text == "7 REMAINING", "legal assignment displays independent solids/stripes and actual remaining counts");
            Check(ui.ballFaces[0].transform.parent.GetComponent<CanvasGroup>().alpha < .3f && ui.ballNumbers[15].text == "9", "pocketed objective dims and opponent card shows stripe numbers");
            foreach (int n in Enumerable.Range(2, 6)) match.rules.Pocketed.Add(n); hud.Refresh();
            Check(ui.groups[0].text == "8-BALL NEXT" && ui.ballNumbers[0].text == "8" && ui.remaining[0].text == "1 TO WIN", "cleared group highlights the authoritative eight-ball objective");
            yield return Capture("eight-ball-objective-camera-fixture");
            session.Pause(); hud.Refresh(); Check(!ui.content.activeSelf, "pause hides gameplay cards behind existing menu"); session.Resume(); hud.Refresh(); Check(ui.content.activeSelf, "resume restores gameplay presentation");
            session.SelectMode(BilliardsMode.Practice); session.StartMatch(); Time.timeScale = 0; hud.Refresh();
            Check(!ui.cards[1].gameObject.activeSelf && ui.remaining[0].text == "15 REMAINING", "practice shows one identity and all fifteen target balls");
            Check(ui.power.barImage.sprite != null, "Heat power fill retains its actual sprite"); ui.power.currentValue = 37; ui.power.UpdateUI(); Check(Mathf.Abs(ui.power.barImage.fillAmount - .37f) < .001f, "installed Heat bar reflects actual fractional shot power");
            session.SelectMode(BilliardsMode.AIVsAI); session.StartMatch(); Time.timeScale = 4; int maximumShots = 0; float until = Time.realtimeSinceStartup + 300;
            while (!session.series.Finished && Time.realtimeSinceStartup < until) { maximumShots = Mathf.Max(maximumShots, match.strikeCount); yield return null; }
            Check(session.series.Finished && session.Screen == BilliardsScreen.Result, "ordinary physical AI rack reaches existing results"); Check(maximumShots > 0 && match.resolutionCount > 0, "AI uses actual truck strikes and physical ball resolution");
            log.AppendLine("MATCH winner=" + session.series.Winner + " shots=" + maximumShots + " resolutions=" + match.resolutionCount + " score=" + string.Join(",", session.series.Wins)); yield return Capture("actual-ai-result");
            session.RestartSeries(); yield return null; hud.Refresh(); Check(!session.series.Finished && session.series.Wins.All(n => n == 0) && ui.content.activeSelf, "rematch clears series and restores fresh gameplay cards");
            session.MainMenu(); yield return null; yield return null; Check(FindAnyObjectByType<BilliardsMatch>() == null, "collection return cleans match and owned HUD");
            log.AppendLine("CHECKS=" + checks + " ERRORS=" + errors + "; actual Windows AI match and labelled rules/UI fixtures; no human or physical-controller acceptance"); File.WriteAllText(Path.Combine(folder, "result.txt"), log.ToString()); Application.Quit(errors == 0 ? 0 : 1);
        }
        void OnDestroy() { Application.logMessageReceived -= Error; preferences?.Dispose(); if (keyboard != null) InputSystem.RemoveDevice(keyboard); Time.timeScale = 1; }
    }
}
#endif
