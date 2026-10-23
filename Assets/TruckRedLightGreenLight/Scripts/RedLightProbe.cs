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

namespace LWS.TruckRedLightGreenLight
{
    public sealed class RedLightProbe : MonoBehaviour
    {
        const string Folder = "Logs/TruckSports/EighteenSports/RedLight";
        [RuntimeInitializeOnLoadMethod]
        static void Boot()
        {
            bool run = Array.IndexOf(Environment.GetCommandLineArgs(), "-tsRedLight") >= 0;
#if UNITY_EDITOR
            run |= UnityEditor.SessionState.GetBool("Sports.RedLight.Probe", false);
            UnityEditor.SessionState.SetBool("Sports.RedLight.Probe", false);
#endif
            if (!run) return;
            var root = new GameObject("Explicit Red Light actual-physics acceptance"); DontDestroyOnLoad(root);
            root.AddComponent<SportsProbeInputIsolation>(); root.AddComponent<RedLightProbe>();
        }
        IDisposable prefs; Keyboard keyboard; InputSettings.BackgroundBehavior priorBackground; int checks, errors; readonly StringBuilder report = new StringBuilder();
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode priorEditorInput;
#endif
        void Awake()
        {
            prefs = SportsPrefs.IsolateValidation("RedLight"); SportsParticipantRegistry.ForgetCache();
            priorBackground = InputSystem.settings.backgroundBehavior; InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            priorEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
        }
        void Check(bool ok, string message) { checks++; if (!ok) errors++; report.AppendLine((ok ? "PASS " : "FAIL ") + message); Flush(); }
        void Flush() { File.WriteAllText(Folder + "/progress.txt", report.ToString()); }
        void Error(string message, string stack, LogType type) { if (type == LogType.Exception || type == LogType.Error) { errors++; report.AppendLine("ERROR " + message + "\n" + stack); Flush(); } }
        IEnumerator Start()
        {
            Directory.CreateDirectory(Folder); File.WriteAllText(Folder + "/result.txt", "RUNNING " + DateTime.UtcNow.ToString("o")); Application.logMessageReceived += Error;
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "TruckRedLightGreenLight")
            {
#if UNITY_EDITOR
                UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode("Assets/TruckRedLightGreenLight/Scenes/TruckRedLightGreenLight.unity", new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
#else
                UnityEngine.SceneManagement.SceneManager.LoadScene("TruckRedLightGreenLight");
#endif
            }
            yield return null; yield return null;
            var m = FindAnyObjectByType<RedLightMatch>();
            Check(m != null && m.trucks.Length == 12 && m.fragments.Length == 12, "saved twelve-full-NWH-truck scene and reusable fragment rigs exist");
            if (m == null) { Complete(); yield break; }
            var venue = m.GetComponent<RedLightPresentation>();
            Check(venue != null && venue.boards.Length == 2 && venue.listeners.Length == 5 && Array.TrueForAll(m.explosions, p => p.GetComponentsInChildren<ParticleSystem>(true).Length >= 3), "saved digital standings, PA towers, Cinemachine camera impulses and pooled smoke/sparks integrate with actual fireballs");
            SportsCameraOptions.Set(false); m.drivingViews.Select(0);
            bool humanOnly = Array.IndexOf(Environment.GetCommandLineArgs(), "-tsRedLightHuman") >= 0;
            if (!humanOnly)
            {
            using var outcomeTally = new SportsScoreAcceptance.OutcomeTally(m);
            bool outcomeCaptured = false;
            m.setup.count = 12; m.NormalizeSetup(); m.AllBots();
            for (int i = 0; i < 12; i++) m.setup.players[i].difficulty = BowlingDifficulty.Hard;
            Check(m.StartMatch(), "complete twelve-AI match starts through normal setup lifecycle");
            Time.timeScale = 3; float deadline = Time.realtimeSinceStartup + 160; bool checkedStart = false, paused = false, captured = false; float furthest = 0; int frameCount = 0; float frameTotal = 0;
            var previousPhase = m.rules.Phase;
            while (!m.Finished && Time.realtimeSinceStartup < deadline)
            {
                if (!outcomeCaptured && m.ScoreJournal.Count > 0) { outcomeCaptured = true; yield return SportsScoreAcceptance.CaptureAward(m, Check, Folder); }
                frameCount++; frameTotal += Time.unscaledDeltaTime;
                for (int i = 0; i < 12; i++) furthest = Mathf.Max(furthest, m.trucks[i].body.position.z);
                if (m.rules.Phase != previousPhase)
                { report.AppendLine("PHASE " + m.rules.Phase + " clock=" + m.rules.Elapsed.ToString("F2") + " explosions=" + m.Explosions + " leaderZ=" + furthest.ToString("F1")); for (int i = 0; i < 12; i++) report.AppendLine("TRUCK " + i + " speed=" + m.trucks[i].body.linearVelocity.magnitude.ToString("F3") + " state=" + m.rules.outcomes[i] + " reason=" + m.rules.reasons[i]); previousPhase = m.rules.Phase; Flush(); }
                if (!checkedStart && m.rules.Elapsed >= 2 && m.rules.Phase == RedLightPhase.Countdown)
                {
                    checkedStart = true; bool flat = true, grounded = true, separated = true;
                    for (int i = 0; i < 12; i++)
                    { flat &= Vector3.Dot(m.trucks[i].transform.up, Vector3.up) > .99f && m.trucks[i].body.linearVelocity.magnitude < .2f; grounded &= m.trucks[i].tractorMotor.Grounded; for (int j = 0; j < i; j++) separated &= !m.trucks[i].shape.bounds.Intersects(m.trucks[j].shape.bounds); }
                    Check(flat && grounded && separated && m.Explosions == 0, "all twelve tractors settle flat, grounded, separated and stationary before GO");
                }
                if (!paused && m.rules.Phase == RedLightPhase.Green && furthest > 3)
                {
                    paused = true; var position = m.trucks[0].body.position; float clock = m.rules.Elapsed; m.Pause(); yield return new WaitForSecondsRealtime(.2f);
                    Check(m.rules.Elapsed == clock && m.trucks[0].body.position == position, "pause preserves actual vehicle/signal simulation"); m.Resume(); Time.timeScale = 3;
                }
                if (!captured && m.rules.Phase == RedLightPhase.LaughWarning)
                { captured = true; yield return SportsWindowCapture.Save(1280, 720, Folder + "/warning-720.png"); yield return SportsWindowCapture.Save(1920, 1080, Folder + "/warning-1080.png"); }
                yield return null;
            }
            Time.timeScale = 1;
            if (!outcomeCaptured && m.ScoreJournal.Count > 0) { outcomeCaptured = true; yield return SportsScoreAcceptance.CaptureAward(m, Check, Folder); }
            outcomeTally.Verify(slot => m.rules.outcomes[slot] == RedLightOutcome.Finished, slot => m.rules.finishTimes[slot], Check);
            Check(outcomeCaptured, "actual legal-finish measurement captured at720p/1080p without invented points");
            report.AppendLine("FINAL phase=" + m.rules.Phase + " elapsed=" + m.rules.Elapsed.ToString("F3") + " realDeadline=" + (Time.realtimeSinceStartup >= deadline));
            for (int i = 0; i < 12; i++) report.AppendLine("FINAL_TRUCK " + i + " position=" + m.trucks[i].body.position + " outcome=" + m.rules.outcomes[i] + " reason=" + m.rules.reasons[i]);
            Check(checkedStart && paused && captured && furthest > 15, "actual trucks accelerate and genuine warning/pause/render scenarios execute");
            Check(m.Finished && m.LegalFinishes > 0 && m.rules.Winner >= 0, "real Hard AI brake, survive and legally finish a complete match");
            yield return SportsResultsAcceptance.Capture(m,Check,Folder,m.ResultPlace,slot=>m.rules.outcomes[slot]==RedLightOutcome.Finished?(double?)m.rules.finishTimes[slot]:null,slot=>m.rules.Winner==slot);string resultBeforeReplay = m.Results;
            m.ExtraAction("watchredlight");
            Check(m.review != null && m.review.Playing && m.review.record.Playing && Time.timeScale == 0, "recorded match-end replay starts without running live physics");
            if (m.review.Playing)
            {
                Check(Array.TrueForAll(m.fragments, rig => Array.TrueForAll(rig.pieces, p => p.renderer.forceRenderingOff)), "recorded replay hides live debris instead of drawing duplicate fragments");
                m.ExtraAction("replayangle"); m.ExtraAction("replayspeed");
                m.review.record.Seek(m.review.record.Duration - 2);
                yield return new WaitForSecondsRealtime(.4f);
                var camera = m.broadcast.view;
                report.AppendLine("REPLAY camera=" + camera.transform.position.ToString("F3") + " forward=" + camera.transform.forward.ToString("F3") + " ghost=" + m.review.record.Position(m.rules.Winner).ToString("F3") + " clipping=" + camera.nearClipPlane + "/" + camera.farClipPlane);
                yield return SportsWindowCapture.Save(1280, 720, Folder + "/replay-720.png");
                m.ExtraAction("replayskip");
                Check(!m.review.Playing && m.Results == resultBeforeReplay && Time.timeScale == 1, "angle/speed/seek/skip restore unchanged authoritative results");
            }
            report.AppendLine("EDITOR/PLAYER FRAME SAMPLE frames=" + frameCount + " meanUnscaledMs=" + (1000 * frameTotal / Mathf.Max(1, frameCount)).ToString("F2") + "; not a hardware or full-player performance certification"); Flush();
            }
            m.SetupScreen(); m.setup.count = 2; m.NormalizeSetup(); m.AllBots();
            keyboard = InputSystem.AddDevice<Keyboard>("Injected deliberate red-light violator");
            InputSystem.EnableDevice(keyboard); keyboard.MakeCurrent(); InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
            m.setup.players[0].source = BowlingSource.Keyboard; m.setup.players[0].forward = Key.W; m.setup.players[0].back = Key.S; m.setup.players[0].left = Key.A; m.setup.players[0].right = Key.D;
            bool started = m.StartMatch(); m.ValidateSources(out string sourceReason);
            Check(started && m.Explosions == 0 && !m.fragments[0].Bursting, "rematch restores full tractor/parts, scores and countdown; source=" + sourceReason + " keyboard=" + keyboard.enabled + " ready=" + m.DevicesReady);
            if (!started) { Complete(); yield break; }
            Time.timeScale = 3; float humanDeadline = Time.realtimeSinceStartup + 50;
            while (m.rules.Phase == RedLightPhase.Countdown && Time.realtimeSinceStartup < humanDeadline) yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
            bool inputLogged = false;
            while (!m.Finished && m.rules.outcomes[0] == RedLightOutcome.Racing && Time.realtimeSinceStartup < humanDeadline)
            {
                keyboard.MakeCurrent(); InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
                yield return null;
                if (!inputLogged && m.rules.Elapsed > 4)
                {
                    inputLogged = true;
                    report.AppendLine("INPUT current=" + Keyboard.current.deviceId + " injected=" + keyboard.deviceId + " enabled=" + keyboard.enabled + " keyW=" + keyboard.wKey.isPressed + " source=" + m.setup.players[0].source + " forward=" + m.setup.players[0].forward + " back=" + m.setup.players[0].back + " read=" + m.ReadDrive(0) + " command=" + m.commands[0]); Flush();
                }
            }
            report.AppendLine("HUMAN phase=" + m.rules.Phase + " elapsed=" + m.rules.Elapsed + " position=" + m.trucks[0].body.position + " command=" + m.commands[0] + " released=" + m.setup.players[0].released + " paused=" + m.Paused + " outcome=" + m.rules.outcomes[0] + " reason=" + m.rules.reasons[0]);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            Check(m.rules.outcomes[0] == RedLightOutcome.Eliminated && m.fragments[0].Bursting && m.Explosions > 0, "injected human deliberately driving on red triggers an actual one-time mesh-shattering explosion");
            yield return null;
            bool extraParticles = Array.Exists(m.explosions[0].GetComponentsInChildren<ParticleSystem>(true), p => p != m.explosions[0] && p.isPlaying && p.particleCount > 0);
            bool shake = Cinemachine.CinemachineImpulseManager.Instance.GetImpulseAt(m.broadcast.view.transform.position, false, 8, out var shakePosition, out var shakeRotation);
            Check(venue.boards[0].text.Contains("ELIMINATED") && extraParticles && shake, "actual violation updates live statistics, emits pooled smoke/sparks and generates a real Cinemachine shake signal");
            if(m.Paused){Check(false,"unexpected pause aborts bounded human acceptance instead of stalling scaled waits");Complete();yield break;}
            yield return new WaitForSeconds(.22f);
            yield return null;
            var blastPoint = m.broadcast.view.WorldToViewportPoint(m.trucks[0].body.worldCenterOfMass);
            Check(blastPoint.z > 0 && blastPoint.x > 0 && blastPoint.x < 1 && blastPoint.y > 0 && blastPoint.y < 1, "shared broadcast keeps the actual eliminated truck/fireball position in frame");
            yield return SportsWindowCapture.Save(1280, 720, Folder + "/violation-720.png");
            m.drivingViews.Select(2); yield return new WaitForSecondsRealtime(.2f);
            Check(!m.drivingViews.cabs[0].gameObject.activeSelf && m.drivingViews.rally.views[0].view.enabled, "eliminated local human receives a live spectator camera instead of a frozen cab");
            yield return SportsWindowCapture.Save(1280, 720, Folder + "/eliminated-spectator-720.png");
            Time.timeScale = 1; m.SetupScreen();
            Check(!m.laugh.isPlaying, "setup cancels pending master laugh audio");
            yield return SportsExpansionCameraAcceptance.Run(m, m.drivingViews, Check, Folder);
            Complete();
        }
        void Complete()
        {
            var match=FindAnyObjectByType<RedLightMatch>();if(match!=null)match.SetupScreen();
            report.AppendLine("CHECKS=" + checks + " ERRORS=" + errors + "; platform=" + Application.platform + "; capture=" + SportsWindowCapture.EvidenceKind + "; actual NWH/PhysX and software input; no human/device/listening acceptance.");
            File.WriteAllText(Folder + "/result.txt", report.ToString()); Application.logMessageReceived -= Error;
#if UNITY_EDITOR
            if (Application.isEditor) UnityEditor.EditorApplication.isPlaying = false; else
#endif
                Application.Quit(errors == 0 ? 0 : 2);
        }
        void OnDestroy()
        {
            Application.logMessageReceived -= Error; if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            InputSystem.settings.backgroundBehavior = priorBackground;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = priorEditorInput;
#endif
            prefs?.Dispose(); SportsParticipantRegistry.ForgetCache(); Time.timeScale = 1;
        }
    }
}
#endif
