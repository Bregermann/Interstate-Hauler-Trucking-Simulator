#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using LWS.TruckSports;
using LWS.TruckBowling;
namespace LWS.TruckHotPotato
{
    public sealed class HotPotatoProbe : MonoBehaviour
    {
        const string Folder = "Logs/TruckSports/EighteenSports/HotPotato";
        [RuntimeInitializeOnLoadMethod] static void Boot()
        {
            bool run = Array.IndexOf(Environment.GetCommandLineArgs(), "-tsHotPotato") >= 0;
#if UNITY_EDITOR
            run |= UnityEditor.SessionState.GetBool("Sports.HotPotato.Probe", false); UnityEditor.SessionState.SetBool("Sports.HotPotato.Probe", false);
#endif
            if (!run) return;
            var root = new GameObject("Explicit Hot Potato actual-physics acceptance"); DontDestroyOnLoad(root); root.AddComponent<SportsProbeInputIsolation>(); root.AddComponent<HotPotatoProbe>();
        }
        IDisposable preferences; Keyboard keys; int checks, errors; readonly StringBuilder report = new StringBuilder(); InputSettings.BackgroundBehavior priorBackground;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode priorEditor;
#endif
        void Awake()
        {
            preferences = SportsPrefs.IsolateValidation("HotPotato"); SportsParticipantRegistry.ForgetCache(); priorBackground = InputSystem.settings.backgroundBehavior; InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            priorEditor = InputSystem.settings.editorInputBehaviorInPlayMode; InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
        }
        void Check(bool ok, string message) { checks++; if (!ok) errors++; report.AppendLine((ok ? "PASS " : "FAIL ") + message); File.WriteAllText(Folder + "/progress.txt", report.ToString()); }
        void Error(string message, string stack, LogType type) { if (type == LogType.Error || type == LogType.Exception) { errors++; report.AppendLine("ERROR " + message + "\n" + stack); } }
        IEnumerator Start()
        {
            Directory.CreateDirectory(Folder); File.WriteAllText(Folder + "/result.txt", "RUNNING"); Application.logMessageReceived += Error;
            yield return SportsProbeScene.Ensure<HotPotatoMatch>("TruckHotPotato");
            yield return null;
            var m = FindAnyObjectByType<HotPotatoMatch>(); Check(m != null && m.trucks.Length == 8 && m.breakables.Length == 4, "authored eight-full-NWH-truck scene and existing Core-Collapse breakables load");
            if (m == null) { Complete(); yield break; }
            SportsCameraOptions.Set(false); m.drivingViews.Select(0);
            m.setup.count = 8; m.AllBots(); for (int i = 0; i < 8; i++) m.setup.players[i].difficulty = BowlingDifficulty.Hard;
            Check(m.StartMatch(), "complete autonomous eight-truck match starts through real setup"); Time.timeScale = 3;
            float deadline = Time.realtimeSinceStartup + 180; bool captured = false, paused = false; float traveled = 0; int lastTransfers = 0;
            while (!m.Finished && Time.realtimeSinceStartup < deadline)
            {
                for (int i = 0; i < 8; i++) traveled = Mathf.Max(traveled, Vector3.Distance(m.trucks[i].body.position, m.Grid(i)));
                if (!paused && m.rules.Clock > 2)
                { paused = true; float clock = m.rules.Clock; var p = m.trucks[0].body.position; m.Pause(); yield return new WaitForSecondsRealtime(.15f); Check(m.rules.Clock == clock && m.trucks[0].body.position == p, "pause freezes bomb clock and vehicle physics"); m.Resume(); Time.timeScale = 3; }
                if (!captured && m.rules.Clock > 6)
                { captured = true; yield return SportsWindowCapture.Save(1280, 720, Folder + "/gameplay-720.png"); yield return SportsWindowCapture.Save(1920, 1080, Folder + "/gameplay-1080.png"); }
                if (m.rules.Transfers != lastTransfers) { lastTransfers = m.rules.Transfers; report.AppendLine("CONTACT_PASS clock=" + m.rules.Clock.ToString("F2") + " count=" + lastTransfers + " carrier=" + m.rules.Carrier); }
                yield return null;
            }
            Time.timeScale = 1;
            Check(m.Finished && m.rules.Complete && m.rules.Winner >= 0 && m.Explosions == 7 && m.rules.AliveCount() == 1, "actual full match resolves seven bomb eliminations and one survivor");
            Check(traveled > 20 && m.rules.Transfers > 0 && captured && paused, "AI genuinely drives, flees and passes through physical truck contact; passes=" + m.rules.Transfers + " traveled=" + traveled.ToString("F1"));
            string result = m.Results; m.ExtraAction("watchpotato"); yield return new WaitForSecondsRealtime(.4f);
            Check(m.review.Playing && Time.timeScale == 0, "recorded match-end replay runs with frozen live physics");
            if (m.review.Playing)
            {
                Check(Array.TrueForAll(m.fragments, rig => Array.TrueForAll(rig.pieces, p => p.renderer.forceRenderingOff)), "recorded replay hides live debris instead of drawing duplicate fragments");
                report.AppendLine("REPLAY camera=" + m.broadcast.view.transform.position + " forward=" + m.broadcast.view.transform.forward + " ghost=" + m.review.record.Position(m.rules.Winner));
                yield return SportsWindowCapture.Save(1280, 720, Folder + "/replay-720.png"); m.ExtraAction("replayskip"); Check(m.Results == result && !m.review.Playing && Time.timeScale == 1, "skip preserves authoritative scores and returns to results");
            }
            m.SetupScreen(); m.setup.count = 2; m.NormalizeSetup(); m.AllBots();
            keys = InputSystem.AddDevice<Keyboard>("Injected Hot Potato local keyboard"); keys.MakeCurrent(); InputSystem.QueueStateEvent(keys, new KeyboardState()); yield return null;
            for (int i = 0; i < 2; i++) m.setup.players[i].source = BowlingSource.Keyboard;
            m.setup.players[0].forward = Key.W; m.setup.players[0].back = Key.S; m.setup.players[0].left = Key.A; m.setup.players[0].right = Key.D;
            m.setup.players[1].forward = Key.UpArrow; m.setup.players[1].back = Key.DownArrow; m.setup.players[1].left = Key.LeftArrow; m.setup.players[1].right = Key.RightArrow;
            bool started = m.StartMatch(); Check(started && m.Explosions == 0 && m.rules.AliveCount() == 2, "two-human rematch restores full tractors, props, bomb and scores");
            if (!started) { Complete(); yield break; }
            while (m.Countdown > 0 || m.rules.Clock < 1) yield return null;
            m.trucks[0].Prepare(new Vector3(0, .65f, -6), Quaternion.identity); m.trucks[1].Prepare(new Vector3(0, .65f, 6), Quaternion.Euler(0, 180, 0)); Physics.SyncTransforms();
            float gap = m.trucks[1].shape.bounds.min.z - m.trucks[0].shape.bounds.max.z;
            m.trucks[1].body.position -= Vector3.forward * (gap - .015f); Physics.SyncTransforms();
            int owner = m.rules.Carrier; m.trucks[0].tractorMotor.AddAssemblyVelocity(Vector3.forward * .65f);
            deadline = Time.realtimeSinceStartup + 4;
            while (m.rules.Transfers == 0 && Time.realtimeSinceStartup < deadline) yield return null;
            Check(m.rules.Transfers == 1 && m.rules.Carrier != owner, "controlled gentle 0.65m/s actual tractor contact transfers without a ram threshold");
            int transfers = m.rules.Transfers; yield return new WaitForSeconds(1);
            Check(m.rules.Transfers == transfers, "continuous resting compound contact does not ping-pong after cooldown");
            InputSystem.QueueStateEvent(keys, new KeyboardState(Key.W)); yield return new WaitForSeconds(.2f);
            Check(m.commands[0].y > .5f && m.commands[1] == Vector2.zero, "injected local keyboard input belongs only to its assigned participant");
            InputSystem.QueueStateEvent(keys, new KeyboardState()); m.SetupScreen();
            yield return SportsExpansionCameraAcceptance.Run(m, m.drivingViews, Check, Folder);
            Complete();
        }
        void Complete()
        {
            report.AppendLine("CHECKS=" + checks + " ERRORS=" + errors + "; platform=" + Application.platform + "; capture=" + SportsWindowCapture.EvidenceKind + "; genuine NWH/PhysX, controlled contact fixture and software input; no human/device/listening claim."); File.WriteAllText(Folder + "/result.txt", report.ToString()); Application.logMessageReceived -= Error;
#if UNITY_EDITOR
            if (Application.isEditor) UnityEditor.EditorApplication.isPlaying = false; else
#endif
                Application.Quit(errors == 0 ? 0 : 2);
        }
        void OnDestroy()
        {
            Application.logMessageReceived -= Error; if (keys != null && keys.added) InputSystem.RemoveDevice(keys); InputSystem.settings.backgroundBehavior = priorBackground;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = priorEditor;
#endif
            preferences?.Dispose(); SportsParticipantRegistry.ForgetCache(); Time.timeScale = 1;
        }
    }
}
#endif
