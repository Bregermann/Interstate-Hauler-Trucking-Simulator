#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using LWS.TruckSports;
namespace LWS.TruckKingOfTheHill
{
    public sealed class HillProbe : MonoBehaviour
    {
        const string Folder = "Logs/TruckSports/EighteenSports/Hill";
        IDisposable preferences; Keyboard fixtureKeys; int checks, errors; readonly StringBuilder report = new StringBuilder(); InputSettings.BackgroundBehavior priorBackground;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode priorEditor;
#endif
        [RuntimeInitializeOnLoadMethod] static void Boot()
        {
            bool run = Array.IndexOf(Environment.GetCommandLineArgs(), "-tsHill") >= 0;
#if UNITY_EDITOR
            run |= UnityEditor.SessionState.GetBool("Sports.Hill.Probe", false); UnityEditor.SessionState.SetBool("Sports.Hill.Probe", false);
#endif
            if (!run) return;
            var root = new GameObject("Explicit hill physical-occupancy acceptance"); DontDestroyOnLoad(root); root.AddComponent<SportsProbeInputIsolation>(); root.AddComponent<HillProbe>();
        }
        void Awake()
        {
            preferences = SportsPrefs.IsolateValidation("Hill"); SportsParticipantRegistry.ForgetCache(); priorBackground = InputSystem.settings.backgroundBehavior; InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            priorEditor = InputSystem.settings.editorInputBehaviorInPlayMode; InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
        }
        void Check(bool ok, string message) { checks++; if (!ok) errors++; report.AppendLine((ok ? "PASS " : "FAIL ") + message); File.WriteAllText(Folder + "/progress.txt", report.ToString()); }
        void Error(string message, string stack, LogType type) { if (type == LogType.Error || type == LogType.Exception) { errors++; report.AppendLine("ERROR " + message + "\n" + stack); } }
        IEnumerator Start()
        {
            Directory.CreateDirectory(Folder); File.WriteAllText(Folder + "/result.txt", "RUNNING"); Application.logMessageReceived += Error;
            yield return SportsProbeScene.Ensure<HillMatch>("TruckKingOfTheHill"); yield return null;
            var m = FindAnyObjectByType<HillMatch>(); Check(m != null && m.sites.Length == 5 && m.trucks.Length == 8 && m.roads.Validate().IsValid, "saved five-plateau mountain and existing LWS road graph load with eight NWH tractors");
            if (m == null) { Complete(); yield break; }
            SportsCameraOptions.Set(false); m.drivingViews.Select(0); m.setup.count = 8; m.option = 1; m.AllBots(); for (int i = 0; i < 8; i++) m.setup.players[i].difficulty = i == 7 ? LWS.TruckBowling.BowlingDifficulty.Hard : i == 6 ? LWS.TruckBowling.BowlingDifficulty.Normal : LWS.TruckBowling.BowlingDifficulty.Easy;
            Check(m.StartMatch() && m.rules.Target == 60, "default cumulative60-second control match starts with actual mixed-difficulty AI");
            var starts = Array.ConvertAll(m.trucks, t => t.body.position); yield return new WaitForSeconds(1.5f); bool grounded = true; float drift = 0;
            for (int i = 0; i < 8; i++) { drift = Mathf.Max(drift, Vector3.ProjectOnPlane(m.trucks[i].body.position - starts[i], Vector3.up).magnitude); grounded &= m.trucks[i].tractorMotor.Grounded && Vector3.Dot(m.trucks[i].transform.up, Vector3.up) > .95f; }
            Check(grounded && drift < .3f && m.rules.Clock == 0, "all eight countdown tractors are grounded/level on the central flat platform; drift=" + drift.ToString("F3"));
            Time.timeScale = 4; float deadline = Time.realtimeSinceStartup + 260; bool captured = false, paused = false, warning = false, contested = false; int lastMove = -1; float nextTrace = 0;
            while (!m.Finished && Time.realtimeSinceStartup < deadline)
            {
                contested |= m.rules.Occupants > 1;
                if (!paused && m.rules.Clock > 2) { paused = true; float clock = m.rules.Clock; var p = m.trucks[0].body.position; m.Pause(); yield return new WaitForSecondsRealtime(.15f); Check(m.rules.Clock == clock && p == m.trucks[0].body.position, "pause freezes control clock and vehicle physics"); m.Resume(); Time.timeScale = 4; }
                if (m.rules.Warning && m.nextRing.localScale.sqrMagnitude > 0 && Vector3.Distance(m.nextRing.position, m.sites[m.rules.NextZone]) < 1) warning = true;
                if (!captured && m.rules.Clock > 15) { captured = true; yield return SportsWindowCapture.Save(1280, 720, Folder + "/gameplay-720.png"); yield return SportsWindowCapture.Save(1920, 1080, Folder + "/gameplay-1080.png"); }
                if (m.rules.Moves != lastMove || m.rules.Clock >= nextTrace) { lastMove = m.rules.Moves; nextTrace = m.rules.Clock + 20; report.AppendLine("ACTUAL_HILL clock=" + m.rules.Clock.ToString("F1") + " zone=" + m.rules.Zone + " occupants=" + m.rules.Occupants + " controller=" + m.rules.Controller + " control=" + string.Join(",", Array.ConvertAll(m.rules.control, value => value.ToString("F1"))) + " recoveries=" + m.Recoveries); File.WriteAllText(Folder + "/progress.txt", report.ToString()); }
                yield return null;
            }
            Time.timeScale = 1; Check(m.Finished && m.rules.Winner >= 0 && m.rules.control[m.rules.Winner] == 60, "actual AI navigation/control/ramming completes the full default match without awarding fake points");
            Check(contested && warning && m.rules.Moves >= 1 && captured, "actual competition contests the ring, previews the next accessible site and relocates");
            report.AppendLine("ACTUAL_AI_RESULTS\n" + m.Standings);
            if (m.Finished) { string result = m.Results; m.ExtraAction("watchhill"); yield return new WaitForSecondsRealtime(.4f); Check(m.review.Playing, "recorded actual tractor and control-ring replay starts"); yield return SportsWindowCapture.Save(1280, 720, Folder + "/replay-720.png"); m.ExtraAction("replayskip"); Check(result == m.Results && !m.review.Playing, "replay preserves control totals and locked winner"); }
            m.SetupScreen(); m.setup.count = 2; m.AllBots(); fixtureKeys = InputSystem.AddDevice<Keyboard>("Hill controlled physical occupancy keyboard"); fixtureKeys.MakeCurrent(); for (int i = 0; i < 2; i++) m.setup.players[i].source = LWS.TruckBowling.BowlingSource.Keyboard;
            Check(m.StartMatch() && m.rules.control[0] == 0 && m.rules.Moves == 0, "two-human rematch restores locations, totals and safe tractors");
            deadline = Time.realtimeSinceStartup + 10; while (m.Countdown > 0 && Time.realtimeSinceStartup < deadline) yield return null;
            if (m.Countdown > 0 || m.Paused) { Check(false, "controlled devices must remain connected through countdown"); Complete(); yield break; }
            var center = m.sites[0]; m.trucks[0].Prepare(center + new Vector3(-4, .65f, 0), Quaternion.identity); m.trucks[1].Prepare(center + new Vector3(4, .65f, 0), Quaternion.identity); Physics.SyncTransforms(); yield return new WaitForSeconds(1);
            Check(m.rules.Occupants == 2 && m.rules.control[0] == 0 && m.rules.control[1] == 0, "two physically grounded opposing tractors contest without earning control");
            m.trucks[1].Prepare(center + new Vector3(0, .65f, 28), Quaternion.identity); yield return new WaitForSeconds(1);
            Check(m.rules.Controller == 0 && m.rules.control[0] > .6f && m.rules.control[1] == 0, "leaving the physical ring gives only its sole grounded occupant elapsed control");
            var proxy = new GameObject("Controlled distant competitor compound-trigger proxy"); proxy.transform.SetParent(m.trucks[1].transform, false); proxy.transform.position = center + Vector3.up; var proxyBody = proxy.AddComponent<Rigidbody>(); proxyBody.isKinematic = true; proxy.AddComponent<SphereCollider>().isTrigger = true;
            float before = m.rules.control[0]; yield return new WaitForSeconds(.6f); Check(m.rules.Controller == 0 && m.rules.control[0] > before && m.rules.control[1] == 0, "a distant competitor's overlapping child collider cannot contest or score"); Destroy(proxy);
            m.trucks[1].Prepare(center + Vector3.up * 15, Quaternion.identity); m.trucks[1].Power(false); m.trucks[1].body.isKinematic = true; yield return new WaitForSeconds(.6f); Check(m.rules.Controller == 0 && m.rules.control[1] == 0, "an airborne tractor above the ring does not earn control");
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
