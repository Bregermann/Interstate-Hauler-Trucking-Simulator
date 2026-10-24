using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using LWS.TruckSports;
using LWS.TruckBowling;

namespace LWS.TruckKickball
{
    public sealed class KickballProbe : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            string[] args = Environment.GetCommandLineArgs(); int at = Array.IndexOf(args, "-tsKickball");
            bool run = at >= 0; string mode = run && at + 1 < args.Length ? args[at + 1] : "smoke";
#if UNITY_EDITOR
            run |= UnityEditor.SessionState.GetBool("Sports.Kickball.Probe", false);
            UnityEditor.SessionState.SetBool("Sports.Kickball.Probe", false);
            if (Application.isEditor) mode = UnityEditor.SessionState.GetString("Sports.Kickball.Mode", "smoke");
#endif
            if (!run) return;
            var root = new GameObject("Explicit Kickball acceptance"); DontDestroyOnLoad(root);
            root.AddComponent<SportsProbeInputIsolation>(); var probe = root.AddComponent<KickballProbe>(); probe.mode = mode;
        }
        string mode, folder;
        IDisposable preferences;
        int errors, checks;
        readonly StringBuilder log = new StringBuilder(2000);
        void Awake() { preferences = SportsPrefs.IsolateValidation("Kickball"); SportsParticipantRegistry.ForgetCache(); }
        void Check(bool pass, string message)
        { checks++; if (!pass) errors++; log.AppendLine((pass ? "PASS " : "FAIL ") + message); File.WriteAllText(folder + "/progress.txt", log.ToString()); }
        void Error(string message, string stack, LogType type)
        { if (type == LogType.Error || type == LogType.Exception) { errors++; log.AppendLine("ERROR " + message); } }
        IEnumerator Start()
        {
            folder = Path.GetFullPath("Logs/TruckSports/EssentialRelease/Kickball/" + (Application.isEditor ? "Native/" : "Windows/") + mode);
            Directory.CreateDirectory(folder); File.WriteAllText(folder + "/result.txt", "RUNNING " + DateTime.UtcNow.ToString("o"));
            Application.logMessageReceived += Error;
            yield return SportsProbeScene.Ensure<KickballMatch>("TruckKickball"); yield return null;
            var match = FindAnyObjectByType<KickballMatch>();
            Check(match != null && match.agents.Length == 12, "saved scene owns twelve separate physical NWH trucks");
            match.option = mode.StartsWith("full") ? 7 : 3;
            match.NormalizeSetup(); match.AllBots();
            foreach (var preset in match.weatherPresets.entries)
                if (preset.displayName == "Clear") { match.weatherPresets.Select(preset.id); break; }
            int difficulty = mode.EndsWith("easy") ? 0 : mode.EndsWith("hard") ? 2 : 1;
            for (int i = 0; i < match.setup.count; i++) match.setup.players[i].difficulty = (BowlingDifficulty)difficulty;
            Check(match.StartMatch(), "twelve-AI match starts without a controller or human confirmation");
            Check(match.rules.TeamSize == 6 && match.rules.BattingTeam == 0, "six-a-side and correct starting half");
            while (match.ViewMode != 0) match.ExtraAction("view");
            yield return new WaitForSecondsRealtime(.4f);
            if (!Application.isEditor) yield return CaptureField(match, "startup", true);
            Time.timeScale = 8;
            float began = Time.realtimeSinceStartup, next = 0;
            float deadline = began + (mode.StartsWith("full") ? 900 : 100);
            bool moved = false, pause = false, sawReplay = false, restoredBroadcast = false;
            while (!match.Finished && Time.realtimeSinceStartup < deadline)
            {
                foreach (var agent in match.agents) moved |= agent.truck.body.linearVelocity.magnitude > 2;
                if (match.ReplayControls && !sawReplay)
                {
                    sawReplay = true;
                    Check(!match.views.broadcast.view.orthographic, "recorded replay retains the weather-compatible perspective projection");
                    if (!Application.isEditor) yield return CaptureField(match, "first-replay", false);
                }
                else if (sawReplay && !match.ReplayControls && !restoredBroadcast)
                {
                    restoredBroadcast = true;
                    if (!Application.isEditor) yield return CaptureField(match, "broadcast-after-replay", true);
                }
                if (!pause && match.PhysicalKicks > 0)
                {
                    pause = true; Time.timeScale = 1; match.Pause();
                    var pose = match.agents[0].truck.body.position; float clock = match.Clock;
                    yield return new WaitForSecondsRealtime(.2f);
                    Check(match.Paused && pose == match.agents[0].truck.body.position && clock == match.Clock, "pause preserves actual truck pose, ball state and match clock");
                    match.Resume(); Time.timeScale = 8;
                }
                if (!mode.StartsWith("full") && match.rules.CompletedPlays >= 5 && match.PhysicalKicks > 0 && match.PhysicalBaseTouches > 0) break;
                if (Time.realtimeSinceStartup >= next)
                {
                    File.WriteAllText(folder + "/progress.txt", log + "LIVE phase=" + match.rules.Phase + " inning=" + match.rules.Inning + " side=" + match.rules.BattingTeam + " plays=" + match.rules.CompletedPlays + " pitches=" + match.rules.Pitches + " kicks=" + match.PhysicalKicks + " catches=" + match.PhysicalCatches + " throws=" + match.PhysicalThrows + " bases=" + match.PhysicalBaseTouches + " scores=" + string.Join(",", match.rules.Scores) + " carrier=" + match.Carrier + " playtime=" + match.PlayTime + " ball=" + match.ball.body.position + " batter=" + match.agents[match.rules.Batter].truck.body.position);
                    next = Time.realtimeSinceStartup + 3;
                }
                yield return null;
            }
            Time.timeScale = 1;
            Check(moved && match.rules.Pitches >= 3, "AI physically moves and rolls real pitches");
            Check(match.PhysicalKicks > 0, "actual bumper contact produces a real kick");
            Check(match.PhysicalBaseTouches > 0, "physical truck motion produces ordered base-pad crossings");
            Check(match.PhysicalCatches > 0, "actual front contact secures the visible ball");
if(match.Finished)yield return SportsResultsAcceptance.Capture(match,Check,folder,slot=>match.rules.Winner<0?0:match.rules.Team(slot)==match.rules.Winner?1:2,slot=>(double?)match.rules.Scores[match.rules.Team(slot)],slot=>match.rules.Team(slot)==match.rules.Winner);
             if (mode.StartsWith("full")) Check(match.Finished && match.rules.Complete, "complete actual three-inning match including necessary extra innings");
            else Check(match.rules.CompletedPlays >= 5, "five actual pitches/plays resolve without injected outs or scores");
            var audience=match.GetComponent<LWS.TruckFieldGoal.FieldGoalCrowd>();Check(audience!=null&&audience.Spectators>=2000&&audience.Reactions>0,"reused stadium audience responds to actual runs or outs without scoring authority");
            log.AppendLine("MATCH difficulty=" + difficulty + " pitches=" + match.rules.Pitches + " kicks=" + match.PhysicalKicks + " catches=" + match.PhysicalCatches + " throws=" + match.PhysicalThrows + " bases=" + match.PhysicalBaseTouches + " plays=" + match.rules.CompletedPlays + " innings=" + match.rules.Inning + " scores=" + string.Join(",", match.rules.Scores));
            match.SetupScreen(); Check(match.StartMatch() && match.rules.Scores[0] == 0 && match.rules.Scores[1] == 0 && match.rules.Outs == 0, "rematch clears scores, runners, outs, possession and replay");
            yield return null; match.Pause();
            if (mode.StartsWith("full")) Check(sawReplay && restoredBroadcast, "real match enters a recorded highlight and restores broadcast rendering");
            var sizes = new[] { new Vector2Int(1280,720), new Vector2Int(1920,1080), new Vector2Int(1440,900), new Vector2Int(1280,960), new Vector2Int(2560,1080) };
            for (int i = 0; i < sizes.Length; i++)
            {
                int w = sizes[i].x, h = sizes[i].y;
                if (!Application.isEditor) Screen.SetResolution(w, h, false);
                yield return new WaitForSecondsRealtime(.3f);
                if (!Application.isEditor)
                { match.menus.Show(SportsEventScreen.Play); yield return new WaitForSecondsRealtime(.15f); yield return CaptureField(match, "rematch-" + w + "x" + h, true); }
            }
            SportsNavigation.FrontEnd(); yield return null; yield return null;
            Check(FindAnyObjectByType<SportsFrontEnd>() != null && FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude).Length == 1, "menu return removes trucks, cameras, replay and extra listeners");
            log.AppendLine("PID=" + System.Diagnostics.Process.GetCurrentProcess().Id + " CHECKS=" + checks + " ERRORS=" + errors + ". Real NWH/PhysX simulation and software test orchestration, not human or controller hardware validation.");
            File.WriteAllText(folder + "/result.txt", log.ToString());
            Application.logMessageReceived -= Error;
#if UNITY_EDITOR
            if (Application.isEditor) UnityEditor.EditorApplication.isPlaying = false; else
#endif
                Application.Quit(errors == 0 ? 0 : 2);
        }
        IEnumerator CaptureField(KickballMatch match, string label, bool requireTurf)
        {
            yield return new WaitForEndOfFrame();
            var picture = ScreenCapture.CaptureScreenshotAsTexture();
            try
            {
                File.WriteAllBytes(folder + "/" + label + "-window.png", picture.EncodeToPNG());
                int green = 0, samples = 0;
                for (int y = picture.height / 8; y < picture.height * 7 / 8; y += 8)
                    for (int x = picture.width / 8; x < picture.width * 7 / 8; x += 8)
                    { var color = picture.GetPixel(x,y); samples++; if (color.g > color.r * 1.15f && color.g > color.b * 1.15f && color.g > .07f) green++; }
                log.AppendLine("RENDER " + label + " " + picture.width + "x" + picture.height + " turfFraction=" + ((float)green / samples).ToString("F3") + " position=" + match.cameras[0].transform.position);
                if (requireTurf) Check((float)green / samples > .08f && !match.cameras[0].orthographic, label + " actual player framebuffer contains the stadium, not a fullscreen sky pass");
            }
            finally { Destroy(picture); }
        }
        void OnDestroy()
        { Application.logMessageReceived -= Error; preferences?.Dispose(); SportsParticipantRegistry.ForgetCache(); Time.timeScale = 1; }
#endif
    }
}
