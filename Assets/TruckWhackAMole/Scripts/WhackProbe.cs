#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using LWS.TruckSports;
namespace LWS.TruckWhackAMole
{
    public sealed class WhackProbe : MonoBehaviour
    {
        const string Folder = "Logs/TruckSports/EighteenSports/Whack";
        IDisposable preferences; Keyboard fixtureKeys; int checks, errors; readonly StringBuilder report = new StringBuilder(); InputSettings.BackgroundBehavior priorBackground;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode priorEditor;
#endif
        [RuntimeInitializeOnLoadMethod] static void Boot()
        {
            bool run = Array.IndexOf(Environment.GetCommandLineArgs(), "-tsWhack") >= 0;
#if UNITY_EDITOR
            run |= UnityEditor.SessionState.GetBool("Sports.Whack.Probe", false); UnityEditor.SessionState.SetBool("Sports.Whack.Probe", false);
#endif
            if (!run) return;
            var root = new GameObject("Explicit Whack-a-Mole actual-physics acceptance"); DontDestroyOnLoad(root); root.AddComponent<SportsProbeInputIsolation>(); root.AddComponent<WhackProbe>();
        }
        void Awake()
        {
            preferences = SportsPrefs.IsolateValidation("Whack"); SportsParticipantRegistry.ForgetCache(); priorBackground = InputSystem.settings.backgroundBehavior; InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            priorEditor = InputSystem.settings.editorInputBehaviorInPlayMode; InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
        }
        void Check(bool ok, string message) { checks++; if (!ok) errors++; report.AppendLine((ok ? "PASS " : "FAIL ") + message); File.WriteAllText(Folder + "/progress.txt", report.ToString()); }
        void Error(string message, string stack, LogType type) { if (type == LogType.Error || type == LogType.Exception) { errors++; report.AppendLine("ERROR " + message + "\n" + stack); } }
        IEnumerator Start()
        {
            Directory.CreateDirectory(Folder); File.WriteAllText(Folder + "/result.txt", "RUNNING"); Application.logMessageReceived += Error;
            yield return SportsProbeScene.Ensure<WhackMatch>("TruckWhackAMole"); yield return null;
            var m = FindAnyObjectByType<WhackMatch>(); Check(m != null && m.trucks.Length == 8 && m.moles.Length == 36, "saved fairground has eight full NWH tractors and36pooled holes");
            if (m == null) { Complete(); yield break; }
            bool authored = true; foreach (var mole in m.moles) foreach (var animator in mole.animators) authored &= animator != null && animator.HasState(0, Animator.StringToHash("Emerge")) && animator.HasState(0, Animator.StringToHash("Whack"));
            Check(authored, "every pooled mole has actual authored emergence/look/dive/hit animation controllers");
            SportsCameraOptions.Set(false); m.drivingViews.Select(0); m.setup.count = 8; m.option = 0; m.AllBots(); for (int i = 0; i < 8; i++) m.setup.players[i].difficulty = LWS.TruckBowling.BowlingDifficulty.Hard;
            Check(m.StartMatch(), "complete eight-truck timed AI match starts"); Time.timeScale = 4;
            float deadline = Time.realtimeSinceStartup + 190; bool captured = false, paused = false, movingMole = false; int lastHits = 0;
            while (!m.Finished && Time.realtimeSinceStartup < deadline)
            {
                foreach (var mole in m.moles) if (m.rules.Phase(mole.hole) == MolePhase.Emerging) { var rig = mole.models[(int)m.rules.kinds[mole.hole]].Find("Rig"); movingMole |= rig.localPosition.y > -4.9f && rig.localPosition.y < -.1f; }
                if (!paused && m.rules.Clock > 2) { paused = true; float clock = m.rules.Clock; var p = m.trucks[0].body.position; m.Pause(); yield return new WaitForSecondsRealtime(.15f); Check(m.rules.Clock == clock && p == m.trucks[0].body.position, "pause freezes match clock and physical truck movement"); m.Resume(); Time.timeScale = 4; }
                if (!captured && m.PhysicalHits > 2) { captured = true; yield return SportsWindowCapture.Save(1280, 720, Folder + "/gameplay-720.png"); yield return SportsWindowCapture.Save(1920, 1080, Folder + "/gameplay-1080.png"); }
                if (lastHits != m.PhysicalHits) { lastHits = m.PhysicalHits; report.AppendLine("PHYSICAL_WHACK clock=" + m.rules.Clock.ToString("F1") + " hits=" + lastHits); File.WriteAllText(Folder + "/progress.txt", report.ToString()); }
                yield return null;
            }
            Time.timeScale = 1;
            report.AppendLine("ACTUAL_AI_RESULTS\n" + m.Standings);
            Check(m.Finished && m.rules.Complete && m.rules.Clock == 120, "actual timed match reaches results without softlock");
            Check(m.PhysicalHits > 8 && m.PhysicalHits == m.rules.Contacts && captured, "AI genuinely drives into emerged targets; all scoring comes from physical bumper contacts; hits=" + m.PhysicalHits);
            Check(movingMole, "actual Animator moves model through intermediate emergence heights rather than teleporting");
            if (m.Finished) { string result = m.Results; m.ExtraAction("watchwhack"); yield return new WaitForSecondsRealtime(.4f); Check(m.review.Playing, "recorded tractor and actual animated-mole replay starts"); yield return SportsWindowCapture.Save(1280, 720, Folder + "/replay-720.png"); m.ExtraAction("replayskip"); Check(!m.review.Playing && result == m.Results, "replay skip preserves points and shared placings"); }
            m.SetupScreen(); m.setup.count = 1; m.option = 0; m.AllBots(); fixtureKeys = InputSystem.AddDevice<Keyboard>("Whack controlled contact keyboard"); fixtureKeys.MakeCurrent(); m.setup.players[0].source = LWS.TruckBowling.BowlingSource.Keyboard;
            Check(m.StartMatch() && m.PhysicalHits == 0 && m.DecoyExplosions == 0, "solo rematch restores all pooled exposures and score statistics");
            deadline = Time.realtimeSinceStartup + 12; int holeIndex = -1;
            while (holeIndex < 0 && Time.realtimeSinceStartup < deadline) { for (int i = 0; i < m.moles.Length; i++) if (m.rules.Phase(i) == MolePhase.Exposed) { holeIndex = i; break; } yield return null; }
            Check(holeIndex >= 0, "solo match exposes a real timed target");
            if (holeIndex >= 0)
            {
                var mole = m.moles[holeIndex]; m.Pause();
                // Fixture selects a decoy before contact, not points. Real PhysX
                // must still detect a moving front bumper against the exposure.
                m.rules.kinds[holeIndex] = MoleKind.Decoy; mole.Tick();
                // Inspect the authored character through the actual selected cab,
                // freezing the simulation briefly for an unaltered render capture.
                var face = mole.transform.position + mole.transform.forward * 9 + Vector3.up * .65f;
                m.trucks[0].Prepare(face, Quaternion.LookRotation(-mole.transform.forward)); m.Resume(); Time.timeScale = 0; m.drivingViews.Select(2);
                m.drivingViews.Tick(1); yield return new WaitForSecondsRealtime(.3f);
                yield return SportsWindowCapture.Save(1280, 720, Folder + "/decoy-cab-720.png"); Time.timeScale = 1; m.Pause();
                var bumper = m.trucks[0].GetComponentInChildren<WhackBumper>().GetComponent<BoxCollider>();
                var origin = mole.transform.position - Vector3.forward * bumper.center.z + Vector3.up * .63f;
                m.trucks[0].Prepare(origin, Quaternion.identity); Physics.SyncTransforms(); m.Resume(); m.trucks[0].tractorMotor.AddAssemblyVelocity(Vector3.forward * 2);
                deadline = Time.realtimeSinceStartup + 2; while (m.DecoyExplosions == 0 && Time.realtimeSinceStartup < deadline) yield return null;
                Check(m.DecoyExplosions == 1 && m.rules.scores[0] == -25 && m.trucks[0].body.linearVelocity.y > 2, "real decoy bumper contact deducts25and physically launches the actual tractor");
                int hits = m.PhysicalHits; yield return new WaitForSeconds(.2f); Check(m.PhysicalHits == hits, "repeated overlap with a claimed exposure cannot duplicate points");
            }
            m.SetupScreen(); yield return SportsExpansionCameraAcceptance.Run(m, m.drivingViews, Check, Folder); Complete();
        }
        void Complete()
        {
            report.AppendLine("CHECKS=" + checks + " ERRORS=" + errors + "; platform=" + Application.platform + "; capture=" + SportsWindowCapture.EvidenceKind + "; actual NWH/PhysX, authored animation and software-input fixtures; no human/device/listening claim."); File.WriteAllText(Folder + "/result.txt", report.ToString()); Application.logMessageReceived -= Error;
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
