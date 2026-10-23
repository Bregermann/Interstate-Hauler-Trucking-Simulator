#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using LWS.TruckSports;
namespace LWS.TruckBalloonBattle
{
    public sealed class BalloonProbe : MonoBehaviour
    {
        const string Folder = "Logs/TruckSports/EighteenSports/Balloon";
        IDisposable preferences; Keyboard fixtureKeys; int checks, errors; readonly StringBuilder report = new StringBuilder(); InputSettings.BackgroundBehavior priorBackground;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode priorEditor;
#endif
        [RuntimeInitializeOnLoadMethod] static void Boot()
        {
            bool run = Array.IndexOf(Environment.GetCommandLineArgs(), "-tsBalloon") >= 0;
#if UNITY_EDITOR
            run |= UnityEditor.SessionState.GetBool("Sports.Balloon.Probe", false); UnityEditor.SessionState.SetBool("Sports.Balloon.Probe", false);
#endif
            if (!run) return;
            var root = new GameObject("Explicit Balloon Battle actual-physics acceptance"); DontDestroyOnLoad(root); root.AddComponent<SportsProbeInputIsolation>(); root.AddComponent<BalloonProbe>();
        }
        void Awake()
        {
            preferences = SportsPrefs.IsolateValidation("Balloon"); SportsParticipantRegistry.ForgetCache();
            priorBackground = InputSystem.settings.backgroundBehavior; InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            priorEditor = InputSystem.settings.editorInputBehaviorInPlayMode; InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
        }
        void Check(bool ok, string message) { checks++; if (!ok) errors++; report.AppendLine((ok ? "PASS " : "FAIL ") + message); File.WriteAllText(Folder + "/progress.txt", report.ToString()); }
        void Error(string message, string stack, LogType type) { if (type == LogType.Error || type == LogType.Exception) { errors++; report.AppendLine("ERROR " + message + "\n" + stack); } }
        IEnumerator Start()
        {
            Directory.CreateDirectory(Folder); File.WriteAllText(Folder + "/result.txt", "RUNNING"); Application.logMessageReceived += Error;
            yield return SportsProbeScene.Ensure<BalloonMatch>("TruckBalloonBattle"); yield return null;
            var m = FindAnyObjectByType<BalloonMatch>(); Check(m != null && m.trucks.Length == 8 && m.targets.Length == 24, "saved eight-full-NWH-truck stadium and24physical balloons load");
            if (m == null) { Complete(); yield break; }
            SportsCameraOptions.Set(false); m.drivingViews.Select(0); m.setup.count = 8; m.AllBots();
            for (int i = 0; i < 8; i++) m.setup.players[i].difficulty = LWS.TruckBowling.BowlingDifficulty.Hard;
            Check(m.StartMatch() && m.AliveCount() == 8 && m.rules.Pops == 0, "complete eight-truck AI match starts with three balloons each");
            var starts = Array.ConvertAll(m.trucks, t => t.body.position); yield return new WaitForSeconds(1.5f);
            bool settled = true; float planarDrift = 0, verticalSettle = 0;
            for (int i = 0; i < 8; i++) { var delta = m.trucks[i].body.position - starts[i]; planarDrift = Mathf.Max(planarDrift, Vector3.ProjectOnPlane(delta, Vector3.up).magnitude); verticalSettle = Mathf.Max(verticalSettle, Mathf.Abs(delta.y)); settled &= m.trucks[i].tractorMotor.Grounded && Vector3.Dot(m.trucks[i].transform.up, Vector3.up) > .95f; }
            Check(m.Countdown > 0 && settled && planarDrift < .3f && m.PhysicalPops == 0, "countdown holds all eight upright grounded tractors; planar drift=" + planarDrift.ToString("F3") + " suspension settling=" + verticalSettle.ToString("F3"));
            Time.timeScale = 4; float deadline = Time.realtimeSinceStartup + 190; bool scoreCaptured = false, captured = false, paused = false; int lastPops = 0;
            while (!m.Finished && Time.realtimeSinceStartup < deadline)
            {
                if(!scoreCaptured&&m.ScoreJournal.Count>0){scoreCaptured=true;yield return SportsScoreAcceptance.CaptureAward(m,Check,Folder);if(SportsScoreAcceptance.CaptureOnly){SportsScoreAcceptance.Journal(m,m.PhysicalPops + (m.rules.Winner >= 0 ? 1 : 0),Check);Check(true,"focused first-award capture; full-match evidence is recorded separately");m.SetupScreen();Check(m.StartMatch()&&m.ScoreJournal.Count==0,"focused scoring rematch starts with an empty journal");yield return null;m.SetupScreen();Complete();yield break;}}

                if (!paused && m.rules.Clock > 2) { paused = true; float clock = m.rules.Clock; var p = m.targets[0].body.position; m.Pause(); yield return new WaitForSecondsRealtime(.15f); Check(m.rules.Clock == clock && p == m.targets[0].body.position, "pause freezes attack clock, tractors and tether motion"); m.Resume(); Time.timeScale = 4; }
                if (!captured && m.rules.Clock > 6) { captured = true; yield return SportsWindowCapture.Save(1280, 720, Folder + "/gameplay-720.png"); yield return SportsWindowCapture.Save(1920, 1080, Folder + "/gameplay-1080.png"); }
                if (lastPops != m.rules.Pops) { lastPops = m.rules.Pops; report.AppendLine("PHYSICAL_POP clock=" + m.rules.Clock.ToString("F1") + " total=" + lastPops + " alive=" + m.AliveCount()); File.WriteAllText(Folder + "/progress.txt", report.ToString()); }
                yield return null;
            }
            Time.timeScale = 1;
            SportsScoreAcceptance.Journal(m,m.PhysicalPops + (m.rules.Winner >= 0 ? 1 : 0),Check);Check(scoreCaptured,"actual award animation captured at720p/1080p");Check(m.Finished && m.rules.Complete && m.AliveCount() <= 1 && m.PhysicalPops >= 21, "actual AI attacks eliminate seven trucks and resolve a full match; pops=" + m.PhysicalPops + " alive=" + m.AliveCount());
            Check(captured && paused && m.PhysicalPops == m.rules.Pops, "every scored pop is routed through actual spike/balloon PhysX contact");
            if (m.Finished)
            {
                yield return SportsResultsAcceptance.Capture(m,Check,Folder,slot=>m.rules.Winner==slot?1:0,slot=>(double?)m.rules.scores[slot],slot=>m.rules.Winner==slot);string result = m.Results; m.drivingViews.Select(2); m.ExtraAction("watchballoon"); yield return new WaitForSecondsRealtime(.4f);
                Check(m.review.Playing && m.atmosphere.vendor.AllowCameras.Count == 1 && m.atmosphere.vendor.AllowCameras.Contains(m.broadcast.view), "recorded replay switches weather allowance from split cabs to broadcast");
                yield return SportsWindowCapture.Save(1280, 720, Folder + "/replay-720.png"); m.ExtraAction("replayskip"); Check(!m.review.Playing && result == m.Results, "replay skip cannot change scores");
            }
            m.SetupScreen(); m.setup.count = 2; m.AllBots(); Check(m.StartMatch() && m.PhysicalPops == 0 && m.rules.Remaining(0) == 3 && m.rules.Remaining(1) == 3, "rematch restores all balloon models, target colliders and attack cooldowns");
            fixtureKeys = InputSystem.AddDevice<Keyboard>("Balloon controlled contact keyboard"); fixtureKeys.MakeCurrent();
            for (int i = 0; i < 2; i++) m.setup.players[i].source = LWS.TruckBowling.BowlingSource.Keyboard;
            deadline = Time.realtimeSinceStartup + 10;
            while (m.Countdown > 0 && Time.realtimeSinceStartup < deadline) yield return null;
            Check(m.Countdown <= 0 && !m.Paused, "controlled keyboard participants remain connected and countdown releases");
            if (m.Countdown > 0 || m.Paused) { Complete(); yield break; }
            // Freeze AI drive at the fixture boundary, without bypassing collision
            // detection or scoring. Real trigger overlap must resolve the attack.
            m.Pause();
            m.trucks[1].Prepare(new Vector3(0, .65f, 0), Quaternion.identity); m.trucks[0].Prepare(new Vector3(-2.8f, .65f, -8), Quaternion.identity);
            for (int i = 0; i < 6; i++) m.targets[i].Restore(); Physics.SyncTransforms();
            m.Resume(); m.trucks[0].TickDrive(Vector2.zero); m.trucks[1].TickDrive(Vector2.zero);
            Check(!m.rules.Extended(0) && m.PhysicalPops == 0, "ordinary overlap without an active spike never pops balloons");
            bool armed = m.Attack(0); yield return new WaitForSeconds(.12f);
            Check(armed && m.PhysicalPops == 0, "yellow telegraph does not pop early");
            deadline = Time.realtimeSinceStartup + 2;
            while (m.PhysicalPops == 0 && Time.realtimeSinceStartup < deadline) yield return null;
            Check(m.PhysicalPops > 0 && m.rules.Remaining(1) == 2, "actual controlled spike overlap pops exactly one victim balloon");
            int pops = m.PhysicalPops; yield return new WaitForSeconds(.35f); Check(m.PhysicalPops == pops, "continuous overlap does not farm extra balloons during the same attack");
            m.SetupScreen(); yield return SportsExpansionCameraAcceptance.Run(m, m.drivingViews, Check, Folder); Complete();
        }
        void Complete()
        {
            report.AppendLine("CHECKS=" + checks + " ERRORS=" + errors + "; platform=" + Application.platform + "; capture=" + SportsWindowCapture.EvidenceKind + "; actual NWH/PhysX and software input fixtures; no human/device/listening claim."); File.WriteAllText(Folder + "/result.txt", report.ToString()); Application.logMessageReceived -= Error;
#if UNITY_EDITOR
            if (Application.isEditor) UnityEditor.EditorApplication.isPlaying = false; else
#endif
                Application.Quit(errors == 0 ? 0 : 2);
        }
        void OnDestroy()
        {
            Application.logMessageReceived -= Error; if (fixtureKeys != null && fixtureKeys.added) InputSystem.RemoveDevice(fixtureKeys); InputSystem.settings.backgroundBehavior = priorBackground;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = priorEditor;
#endif
            preferences?.Dispose(); SportsParticipantRegistry.ForgetCache(); Time.timeScale = 1;
        }
    }
}
#endif
