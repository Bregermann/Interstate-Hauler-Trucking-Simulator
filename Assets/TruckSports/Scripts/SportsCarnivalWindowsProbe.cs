#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using LWS.TruckBeerPong;
using LWS.TruckHungryHaulers;
using LWS.TruckHotPotato;
using LWS.TruckSkeeBall;

namespace LWS.TruckSports
{
    // Opt-in real player-window acceptance. No pose, score or outcome injection.
    public sealed class SportsCarnivalWindowsProbe : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod] static void Boot()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-tsCarnival") < 0) return;
            var root = new GameObject("Explicit Carnival Windows acceptance"); DontDestroyOnLoad(root);
            root.AddComponent<SportsProbeInputIsolation>(); root.AddComponent<SportsCarnivalWindowsProbe>();
        }
        readonly StringBuilder log = new StringBuilder(); readonly List<SportsScoreEvent> receipts = new List<SportsScoreEvent>();
        string folder; IDisposable preferences; int checks, errors; bool finished;
        void Check(bool value, string reason) { checks++; if (!value) errors++; log.AppendLine((value ? "PASS " : "FAIL ") + reason); File.WriteAllText(Path.Combine(folder, "progress.txt"), log.ToString()); }
        void Error(string message, string stack, LogType type) { if (type != LogType.Error && type != LogType.Exception) return; errors++; log.AppendLine("ERROR " + message); }
        void Record(SportsScoreEvent receipt) => receipts.Add(receipt);
        IEnumerator Until(Func<bool> condition, float seconds, string label)
        {
            float deadline = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
            Check(condition(), label);
        }
        IEnumerator Capture(string label)
        {
            foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080) })
            {
                Screen.SetResolution(size.x, size.y, false); yield return new WaitForSecondsRealtime(.35f);
                string path = Path.Combine(folder, label + "-" + size.x + "x" + size.y + ".png"); bool correct = false;
                for (int attempt = 0; attempt < 10 && !correct; attempt++)
                {
                    yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(path); yield return new WaitForSecondsRealtime(.2f);
                    if (File.Exists(path))
                    {
                        var bytes = File.ReadAllBytes(path);
                        if (bytes.Length >= 24) correct = ((bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19]) == size.x && ((bytes[20] << 24) | (bytes[21] << 16) | (bytes[22] << 8) | bytes[23]) == size.y;
                    }
                    if (!correct) { Screen.SetResolution(size.x, size.y, false); yield return new WaitForSecondsRealtime(.3f); }
                }
                Check(correct, label + " actual Windows backbuffer " + size.x + "x" + size.y + "; no rescale/substitution");
            }
        }
        IEnumerator Start()
        {
            var args = Environment.GetCommandLineArgs(); int output = Array.IndexOf(args, "-tsCarnivalOutput");
            if (output < 0 || output + 1 >= args.Length) throw new ArgumentException("Provide -tsCarnivalOutput.");
            folder = Path.GetFullPath(args[output + 1]); Directory.CreateDirectory(folder); preferences = SportsPrefs.IsolateValidation("CarnivalActualWindows");
            SportsParticipantRegistry.ForgetCache(); Application.logMessageReceived += Error; SportsTimeOfDay.Select(SportsTimeChoice.Day);
            var catalog = Resources.Load<SportsTruckAppearanceCatalog>("SportsTruckAppearanceCatalog");
            Check(catalog != null && catalog.Validate(out _), "real built seven-body/accessory catalog resolves");
            yield return Pong(); if (errors > 5) { Finish(); yield break; }
            yield return Haulers(); if (errors > 5) { Finish(); yield break; }
            yield return Potato(); if (errors > 5) { Finish(); yield break; }
            yield return Skee();
            SportsNavigation.FrontEnd(); yield return null; yield return null;
            Check(FindAnyObjectByType<SportsFrontEnd>() != null, "return to real original selection menu after four sports"); yield return Capture("collection-return"); Finish();
        }
        IEnumerator Pong()
        {
            yield return SportsProbeScene.Ensure<PongMatch>("TruckBeerPong"); var match = FindAnyObjectByType<PongMatch>();
            match.SetupScreen(); match.AllBots(); foreach (var player in match.setup.players) player.difficulty = LWS.TruckBowling.BowlingDifficulty.Hard;
            Check(match.StartMatch(), "Pong original physical AI shot starts"); Time.timeScale = 3;
            yield return Until(() => match.Phase == PongPhase.Replay, 100, "Pong actual charge/release/contact enters recorded replay");
            if (match.Phase != PongPhase.Replay) { match.SetupScreen(); yield break; }
            int shots = match.rules.shots.Sum(), hits = match.rules.hits.Sum(); Check(shots == 1, "Pong first original shot locked once");
            Time.timeScale = 0;
            for (int angle = 0; angle < 3; angle++)
            {
                if (angle > 0) match.ExtraAction("replayangle");
                match.replay.Seek(match.replay.Duration * .65f); yield return null; yield return null;
                Check(Vector3.Distance(match.cameraRig.aim.position, match.RecordedTruckCenter) < .15f, "Pong angle" + angle + " follows recorded physical center, not offset mesh pivot");
                Check(match.cameraRig.view.transform.position.y >= match.table.bounds.max.y + .6f && !match.table.bounds.Contains(match.cameraRig.view.transform.position), "Pong angle" + angle + " stays above/outside actual table");
                yield return Capture("Pong-real-recorded-angle" + angle);
            }
            Check(match.rules.shots.Sum() == shots && match.rules.hits.Sum() == hits, "Pong replay/capture cannot change authoritative shots/hits");
            string prior = match.MatchId; match.SetupScreen(); Check(match.StartMatch() && match.MatchId != prior && match.rules.shots.Sum() == 0 && !match.replay.Playing, "Pong rematch resets original recording/shot state"); match.SetupScreen();
        }
        IEnumerator Haulers()
        {
            yield return SportsProbeScene.Ensure<HaulerMatch>("TruckHungryHaulers"); var match = FindAnyObjectByType<HaulerMatch>(); match.SetupScreen(); match.AllBots(); match.option = 0;
            match.drivingViews.Select(0); SportsCameraOptions.Set(false); receipts.Clear(); match.ScoreConfirmed += Record;
            Check(match.StartMatch(), "Hauler four real AI trucks and physical banking holes start"); Time.timeScale = 3;
            yield return Until(() => match.ActualDeposits > 0 || match.Finished, 95, "Hauler physical AI cargo reaches an actual bank or match concludes");
            Check(match.ActualDeposits > 0, "Hauler actual ball-only bank contact produced a deposit");
            var hud = match.menus.GetComponent<HaulerCargoHud>(); Check(hud != null && hud.cards.Length == 4 && hud.clocks.Length == 4, "Hauler saved compact score/cargo/clock bindings exist in player");
            Time.timeScale = 0; yield return null; yield return Capture("Hauler-physical-banked-shared"); Time.timeScale = 4;
            yield return Until(() => match.Finished, 100, "Hauler original two-minute AI match reaches real results");
            Check(receipts.Select(item => item.EventId).Distinct().Count() == receipts.Count, "Hauler score receipts remain unique");
            Time.timeScale = 0; yield return Capture("Hauler-real-results"); string prior = match.MatchId;
            match.SetupScreen(); Check(match.StartMatch() && match.MatchId != prior && match.ActualDeposits == 0 && match.ScoreJournal.Count == 0, "Hauler rematch clears actual deposits and score journal");
            match.ScoreConfirmed -= Record; match.SetupScreen();
        }
        IEnumerator Potato()
        {
            yield return SportsProbeScene.Ensure<HotPotatoMatch>("TruckHotPotato"); var match = FindAnyObjectByType<HotPotatoMatch>(); match.SetupScreen(); match.AllBots();
            receipts.Clear(); match.ScoreConfirmed += Record; Check(match.StartMatch(), "Hot Potato original AI match starts"); Time.timeScale = 3;
            yield return Until(() => match.rules.Transfers > 0 || match.Finished, 70, "Hot Potato real competitors make contact or heat concludes");
            Check(match.rules.Transfers > 0 && match.rules.TransferPoints == 3, "Hot Potato genuine contact transfer uses configured3 points");
            Check(receipts.Any(item => item.Delta == 3), "Hot Potato actual transfer publishes a3-point receipt");
            Time.timeScale = 0; yield return Capture("HotPotato-real-transfer-approved-arena"); Time.timeScale = 4;
            yield return Until(() => match.Finished, 110, "Hot Potato original AI elimination match finishes");
            Check(receipts.Select(item => item.EventId).Distinct().Count() == receipts.Count, "Hot Potato original pass/survival/champion awards stay distinct");
            Time.timeScale = 0; yield return Capture("HotPotato-real-results"); string prior = match.MatchId;
            match.SetupScreen(); Check(match.StartMatch() && match.MatchId != prior && match.rules.Transfers == 0 && match.ScoreJournal.Count == 0, "Hot Potato rematch clears contacts, passes and score receipts"); match.ScoreConfirmed -= Record; match.SetupScreen();
        }
        IEnumerator Skee()
        {
            yield return SportsProbeScene.Ensure<SkeeMatch>("TruckSkeeBall"); var match = FindAnyObjectByType<SkeeMatch>(); match.SetupScreen(); match.AllBots(); match.setup.count = 1;
            Check(match.StartMatch(), "Skee original solo AI course starts"); Time.timeScale = 3;
            var pixels = match.GetComponent<SkeeArcadePixels>();
            yield return Until(() => match.Finished, 160, "Skee real ramp/flight/quiet-rest attempts finish");
            Check(match.GetComponentInChildren<SkeeCarnivalVenue>(true) != null && pixels != null, "Skee actual built cabinet/zero return/net/pixel components resolve");
            Time.timeScale = 0; yield return Capture("Skee-real-results"); string prior = match.MatchId;
            match.SetupScreen(); Check(match.StartMatch() && match.MatchId != prior, "Skee original rematch starts fresh"); yield return null; yield return null;
            Check(pixels.Active && pixels.FeedCount == 1 && pixels.TextureFor(0) != null, "Skee rematch recreates one real low-resolution world feed");
            yield return Capture("Skee-real-arcade-rematch"); match.SetupScreen(); yield return null; Check(!pixels.Active && pixels.FeedCount == 0, "Skee setup cleans render feeds");
        }
        void Finish()
        {
            finished = true; Application.logMessageReceived -= Error; log.AppendLine("CHECKS=" + checks + " ERRORS=" + errors);
            log.AppendLine("Actual Windows NWH/PhysX AI gameplay and unscaled backbuffers; no injected poses/scores/outcomes, human/controller/listening or long-soak performance acceptance.");
            File.WriteAllText(Path.Combine(folder, "result.txt"), log.ToString()); Time.timeScale = 1; preferences?.Dispose(); preferences = null; SportsParticipantRegistry.ForgetCache(); Application.Quit(errors == 0 ? 0 : 2);
        }
        void OnDestroy() { Application.logMessageReceived -= Error; if (!finished && folder != null) { log.AppendLine("INTERRUPTED"); File.WriteAllText(Path.Combine(folder, "result.txt"), log.ToString()); } preferences?.Dispose(); Time.timeScale = 1; }
    }
}
#endif
