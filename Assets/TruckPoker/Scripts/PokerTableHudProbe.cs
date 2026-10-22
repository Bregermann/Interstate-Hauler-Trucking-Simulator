#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using LWS.TruckBowling;
using LWS.TruckSports;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace LWS.TruckPoker
{
    public sealed class PokerTableHudProbe : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Boot()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-tsPokerTableHud") < 0) return;
            var root = new GameObject("Explicit Poker table UI Windows acceptance"); DontDestroyOnLoad(root); root.AddComponent<SportsProbeInputIsolation>(); root.AddComponent<PokerTableHudProbe>();
        }
        PokerMatch match; string folder; int checks, errors; IDisposable prefs; Keyboard keys; Gamepad whole, shared; InputSettings original, injected; readonly StringBuilder log = new StringBuilder();
        void Check(bool ok, string message) { checks++; if (!ok) errors++; log.AppendLine((ok ? "PASS " : "FAIL ") + message); File.WriteAllText(Path.Combine(folder, "progress.txt"), log.ToString()); }
        void Error(string message, string stack, LogType type) { if (type == LogType.Error || type == LogType.Exception) { errors++; log.AppendLine("ERROR " + message + "\n" + stack); } }
        void Finish() { log.AppendLine("CHECKS=" + checks + " ERRORS=" + errors + "; Windows composed frames and actual original Poker simulation; software devices, no human/controller/listening acceptance."); File.WriteAllText(Path.Combine(folder, "result.txt"), log.ToString()); Application.Quit(errors == 0 ? 0 : 1); }
        IEnumerator Capture(string name) { yield return new WaitForSecondsRealtime(.65f); yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(folder, name + ".png")); yield return new WaitForSecondsRealtime(.15f); }
        bool Inside(RectTransform rect, Rect viewport)
        { var corners = new Vector3[4]; rect.GetWorldCorners(corners); return corners.All(p => p.x >= viewport.xMin * Screen.width - 1 && p.x <= viewport.xMax * Screen.width + 1 && p.y >= viewport.yMin * Screen.height - 1 && p.y <= viewport.yMax * Screen.height + 1); }
        void Neutral() { InputSystem.QueueStateEvent(keys, new KeyboardState()); InputSystem.QueueStateEvent(whole, new GamepadState()); InputSystem.QueueStateEvent(shared, new GamepadState()); }
        void Configure(int humans)
        {
            match.SetupScreen(); match.option = 0; match.setup.count = 8; match.AllBots(); SportsCameraOptions.Set(false);
            for (int i = 0; i < humans; i++) { match.setup.players[i].source = i == 0 ? BowlingSource.Keyboard : i == 1 ? BowlingSource.WholeController : i == 2 ? BowlingSource.LeftStick : BowlingSource.RightStick; if (i > 0) match.Assign(i, i == 1 ? whole : shared); }
        }
        IEnumerator Start()
        {
            var args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args, "-tsPokerTableHudOutput"); folder = index >= 0 ? args[index + 1] : SportsEvidencePaths.Folder("PokerTableHud"); Directory.CreateDirectory(folder);
            prefs = SportsPrefs.IsolateValidation("PokerTableHudWindows"); original = InputSystem.settings; injected = Instantiate(original); injected.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; InputSystem.settings = injected; keys = InputSystem.AddDevice<Keyboard>(); whole = InputSystem.AddDevice<Gamepad>(); shared = InputSystem.AddDevice<Gamepad>(); Application.logMessageReceived += Error;
            yield return SportsProbeScene.Ensure<PokerMatch>("TruckPoker"); yield return null; match = FindAnyObjectByType<PokerMatch>(); var ui = match.presentation.tableHud; Check(ui != null && match.presentation.presence != null, "saved scene has table UI and isolated public AI presence");
            foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080), new Vector2Int(2560, 1080), new Vector2Int(1024, 768) })
            {
                Screen.SetResolution(size.x, size.y, false); yield return new WaitForSecondsRealtime(.2f);
                foreach (int humans in new[] { 0, 1, 2, 3, 4 })
                {
                    Configure(humans); Check(match.StartMatch() && match.HumanViews == humans, "existing mixed sources start " + humans + " human panes at " + size);
                    match.dealerDrive.Skip(); Time.timeScale = 0; Neutral(); yield return null; ui.Refresh(); yield return Capture("layout-" + humans + "humans-" + size.x + "x" + size.y); Canvas.ForceUpdateCanvases();
                    if (humans == 0)
                    {
                        Check(ui.broadcastSeats.All(s => Inside(s.root, new Rect(0, 0, 1, 1))), "eight public stack panels fit broadcast " + size);
                        Check(ui.broadcastSeats.Select((s, i) => s.stack.text == match.Table.Stack(i).ToString("N0") && s.stack.textInfo.characterInfo.Any(c => c.isVisible) && s.stack.mesh.vertexCount > 0).All(visible => visible), "all eight authoritative chip totals have rendered glyph geometry " + size);
                        var bounds = ui.venueLogo.bounds; float logoBottom = float.PositiveInfinity;
                        for (int corner = 0; corner < 8; corner++) { var p = bounds.center + Vector3.Scale(bounds.extents, new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1)); logoBottom = Mathf.Min(logoBottom, match.broadcast.view.WorldToScreenPoint(p).y); }
                        var rail = new Vector3[4]; ui.header.GetWorldCorners(rail); Check(rail.Max(p => p.y) < logoBottom - 4, "new hand rail clears the actual projected original venue logo " + size);
                    }
                    for (int v = 0; v < humans; v++)
                    {
                        var view = ui.views[v]; var region = match.seats[v].view.rect;
                        Check(view.design.GetComponentsInChildren<UnityEngine.UI.Graphic>().All(g => Inside(g.rectTransform, region)) && Inside(view.seats, region) && view.publicSeats.All(s => Inside(s.root, region)), "all visible table panels isolated in viewport " + v + " at " + size);
                        Check(view.owner.text == match.setup.players[match.ViewOwners[v]].name && match.presentation.panes[v].own.All(c => !c.HasPrivateDisplayData), "owned name and hidden private cards agree view " + v);
                    }
                }
            }
            Configure(1); Screen.SetResolution(1920, 1080, false); match.StartMatch(); match.dealerDrive.Skip(); Neutral(); yield return new WaitForSeconds(.9f);
            // Preserve the current real actor while peeking; this fixture never
            // submits a bet or changes an authoritative hand/card.
            Time.timeScale = 0; InputSystem.QueueStateEvent(keys, new KeyboardState(match.PeekKey(0))); yield return null; yield return null; yield return null;
            Check(match.presentation.panes[0].own.All(c => c.HasPrivateDisplayData) && match.presentation.seatCards.All(c => !c.HasPrivateDisplayData), "real keyboard hold displays only owner's original card renderer"); yield return Capture("owned-private-peek");
            Neutral(); yield return null; yield return null; Check(match.presentation.panes.All(p => p.own.All(c => !c.HasPrivateDisplayData)), "release clears original private card render state"); match.Pause(); yield return Capture("pause"); match.Resume();
            Configure(0); match.startingStack = 200; match.smallBlind = 25; match.levelHands = 3; SportsPrefs.SetInt("TruckPoker.DealerPacing.v1", 1); match.StartMatch(); Time.timeScale = 4;
            var positions = match.avatars.Select(t => t.position).ToArray(); var rotations = match.avatars.Select(t => t.rotation).ToArray(); var old = match.presentation.presence.pivots.Select(t => t.localRotation).ToArray();
            yield return new WaitForSeconds(.9f); Check(match.presentation.presence.pivots.Where((t, i) => Quaternion.Angle(old[i], t.localRotation) > .1f).Count() > 3, "actual AI idle/gaze pivots animate during the original deal");
            Check(match.avatars.Where((t, i) => t.position != positions[i] || t.rotation != rotations[i]).Count() == 0, "parked truck roots remain unchanged while reacting"); yield return Capture("actual-table-dealing");
            bool award = false; float until = Time.realtimeSinceStartup + 240; int lastHand = 0;
            while (!match.Finished && Time.realtimeSinceStartup < until)
            {
                match.Table.CheckChips();
                if (match.Table.HandNumber != lastHand) { lastHand = match.Table.HandNumber; log.AppendLine("HAND " + lastHand + " actions=" + match.ActionsTaken); }
                if (!award && match.Table.HandDone) { award = true; Time.timeScale = 0; yield return Capture("actual-awarded-hand"); Check(!string.IsNullOrEmpty(ui.PublicAwardCaption()) && ui.broadcastStack.text == ui.PublicAwardCaption(), "actual hand award caption uses only legitimately public cards or a private fold-win label"); Time.timeScale = 4; }
                yield return null;
            }
            Check(match.Finished && match.Table.Winner >= 0 && match.ActionsTaken > 0, "complete original eight-AI tournament without injected cards/actions");
            Check(Enumerable.Range(0, 8).Sum(match.Table.Stack) == 1600 && Enumerable.Range(0, 8).Count(i => match.Table.Stack(i) > 0) == 1, "all 1600 chips conserved at actual tournament result");
            if (!match.Finished) { Finish(); yield break; }
            log.AppendLine("TOURNAMENT hands=" + match.Table.HandNumber + " actions=" + match.ActionsTaken + " winner=" + match.Table.Winner); yield return Capture("actual-tournament-results");
            Check(match.StartMatch() && match.Table.HandNumber == 1 && match.ActionsTaken == 0, "rematch resets original tournament and public presentation"); match.SetupScreen(); yield return null; Check(match.presentation.presence.pivots.All(p => p.localPosition == Vector3.zero && Quaternion.Angle(p.localRotation, Quaternion.identity) < .001f), "menu return restores every public visual pivot");
            SportsNavigation.FrontEnd(); yield return null; yield return null; Check(FindAnyObjectByType<SportsFrontEnd>() != null, "collection return cleans up Poker scene"); Finish();
        }
        void OnDestroy()
        { Application.logMessageReceived -= Error; if (match != null) match.SetupScreen(); prefs?.Dispose(); if (keys != null) InputSystem.RemoveDevice(keys); if (whole != null) InputSystem.RemoveDevice(whole); if (shared != null) InputSystem.RemoveDevice(shared); if (original != null) InputSystem.settings = original; if (injected != null) Destroy(injected); Time.timeScale = 1; }
    }
}
#endif
