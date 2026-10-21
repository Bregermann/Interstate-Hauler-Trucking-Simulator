using System;
using System.IO;
using System.Text;
using System.Collections;
using UnityEngine;
using LWS.TruckSports;
using LWS.TruckBowling;
using UnityEngine.InputSystem;using UnityEngine.InputSystem.LowLevel;
namespace LWS.TruckTrailerPlug
{
    public sealed class TrailerPlugProbe : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            var args = Environment.GetCommandLineArgs(); int at = Array.IndexOf(args, "-tsTrailerPlug");
            bool run = at >= 0; string mode = at >= 0 && at + 1 < args.Length ? args[at + 1] : "full";
#if UNITY_EDITOR
            run |= UnityEditor.SessionState.GetBool("Sports.TrailerPlug.Probe", false);
            UnityEditor.SessionState.SetBool("Sports.TrailerPlug.Probe", false);
            if (Application.isEditor) mode = UnityEditor.SessionState.GetString("Sports.TrailerPlug.Mode", "full");
#endif
            if (!run) return;
            var root = new GameObject("Explicit Trailer Plug physical acceptance"); DontDestroyOnLoad(root);
            root.AddComponent<SportsProbeInputIsolation>(); root.AddComponent<TrailerPlugProbe>().mode = mode;
        }
        string mode, folder; IDisposable preferences; int checks, errors;
        readonly StringBuilder report = new StringBuilder(); TrailerPlugMatch match;Keyboard keyboard;bool checkedReplay;
        void Awake() { preferences = SportsPrefs.IsolateValidation("TrailerPlug"); SportsParticipantRegistry.ForgetCache(); }
        void Check(bool value, string label)
        { checks++; if (!value) errors++; report.AppendLine((value ? "PASS " : "FAIL ") + label); Flush(); }
        void Flush() { if (folder != null) File.WriteAllText(folder + "/progress.txt", report.ToString()); }
        void Error(string message, string stack, LogType type)
        { if (type == LogType.Exception || type == LogType.Error) { errors++; report.AppendLine("ERROR " + message + "\n" + stack); Flush(); } }
        IEnumerator Start()
        {
            folder = Path.GetFullPath("Logs/TruckSports/EssentialRelease/TrailerPlug/" + (Application.isEditor ? "Native/" : "Windows/") + mode);
            Directory.CreateDirectory(folder); File.WriteAllText(folder + "/result.txt", "RUNNING " + DateTime.UtcNow.ToString("o"));
            Application.logMessageReceived += Error;
            yield return SportsProbeScene.Ensure<TrailerPlugMatch>("TruckTrailerPlug"); yield return null;
            match = FindAnyObjectByType<TrailerPlugMatch>();
            Check(match != null && match.rig.trailer != null && match.rig.spring != null && match.crowdSettings.characters.Length > 0,
                "saved scene has an actual suspended tractor, cargo trailer, open hole and shared Taxi pedestrian roster");
            foreach (var entry in match.presets.entries) if (entry.displayName == "Clear") { match.presets.Select(entry.id); break; }
            match.setup.count = 4; match.option = 0; match.NormalizeSetup(); match.AllBots();
            for (int i = 0; i < 4; i++) match.setup.players[i].difficulty = (BowlingDifficulty)Mathf.Min(i, 2);
            if(mode=="mixed"){keyboard=InputSystem.AddDevice<Keyboard>("Trailer Plug injected keyboard");match.setup.players[0].source=BowlingSource.Keyboard;}
            Check(match.StartMatch(), mode=="mixed"?"injected human and three AI competitors start with independent source ownership":"four alternating AI competitors start without controllers");
            Time.timeScale = 4; float deadline = Time.realtimeSinceStartup + 400, next = 0;
            bool captured = false, paused = false; int priorRelease = 0, hitchSamples = 0; float maxCable = 0, maximumHitchSeparation = 0;
            Check(match.rig.tractorHitch != null && match.rig.trailerModule != null && match.rig.trailer.GetComponentsInChildren<Rigidbody>(true).Length == 1,
                "matched native models retain NWH hitch/trailer modules and one authoritative trailer Rigidbody");
            Check(match.rig.truck.transform.localScale == Vector3.one && match.rig.trailer.transform.localScale == Vector3.one,
                "both NWH physics roots keep their supported unit scale");
            Check(match.openingRadii.x > 2.4f && match.openingRadii.y > 6, "actual opening is enlarged for the matched articulated vehicle dimensions");
            while (!match.Finished && Time.realtimeSinceStartup < deadline && errors == 0)
            {
                if (match.Phase == TrailerPlugPhase.Swing && match.rig.Attached)
                { hitchSamples++; maximumHitchSeparation = Mathf.Max(maximumHitchSeparation, match.rig.HitchSeparation); }
                if(mode=="mixed"){
                    if(match.Phase==TrailerPlugPhase.Swing&&match.rules.Participant==0){InputSystem.QueueStateEvent(keyboard,match.Age>8?new KeyboardState(Key.W,Key.Space):match.Age>1?new KeyboardState(Key.W):new KeyboardState());}
                    else InputSystem.QueueStateEvent(keyboard,new KeyboardState());
                    if(match.Phase==TrailerPlugPhase.Replay){if(!checkedReplay){checkedReplay=true;Check(match.replay.Playing,"injected participant receives actual post-delivery recorded replay");match.ExtraAction("replayangle");match.ExtraAction("replayspeed");Check(match.replay.Angle==1&&match.replay.Speed<1,"trailer review supports alternate angles and slow motion");match.ExtraAction("replayrewind");}match.ExtraAction("replayskip");}
                }
                maxCable = Mathf.Max(maxCable, Vector3.Distance(match.rig.anchor.position, match.rig.truck.body.position));
                if (match.PhysicalReleases > priorRelease)
                {
                    priorRelease = match.PhysicalReleases;
                    report.AppendLine("RELEASE " + priorRelease + " v=" + match.rig.ReleasedVelocity + " angular=" + match.rig.ReleasedAngular);
                }
                if (!paused && match.Phase == TrailerPlugPhase.Flight)
                {
                    paused = true; var position = match.rig.trailer.position; float age = match.Age;
                    match.Pause(); yield return new WaitForSecondsRealtime(.3f);
                    Check(match.rig.trailer.position == position && match.Age == age, "pause preserves actual detached flight and attempt state");
                    match.Resume(); Time.timeScale = 4;
                }
                if (!captured && match.Phase == TrailerPlugPhase.Swing && match.Age > 6)
                {
                    captured = true;
                    foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080) })
                    { SportsControlledCapture.Save(match, size.x, size.y, folder + "/quarry-" + size.x + "x" + size.y + ".png"); yield return null; }
                }
                if (Time.realtimeSinceStartup >= next)
                {
                    next = Time.realtimeSinceStartup + 3;
                    report.AppendLine("LIVE round=" + match.rules.Round + " slot=" + match.rules.Participant + " phase=" + match.Phase + " age=" + match.Age +
                        " trailer=" + match.rig.trailer.position + " v=" + match.rig.trailer.linearVelocity + " hits=" + match.PhysicalHits + " crowd=" + match.Crowd.Count + " score=" + string.Join(",", match.rules.score)); Flush();
                }
                yield return null;
            }
            Check(match.Finished && match.rules.Complete, "complete four-participant competition resolves through actual delivery physics");
            if(mode=="mixed")Check(checkedReplay,"mixed match exercised replay and fresh-input turn transitions");
            Check(match.PhysicalReleases == 12, "exactly twelve physical releases, three per participant");
            for (int i = 0; i < 4; i++) Check(match.rules.attempts[i] == 3, "participant " + i + " receives exactly three scored drops");
            Check(match.PhysicalHits > 0, "falling trailers physically activate shared Taxi ragdolls and once-only impact scores");
            Check(hitchSamples > 100 && maximumHitchSeparation < .5f, "real NWH attachment points remain physically aligned throughout all swings, max=" + maximumHitchSeparation);
            Check(maxCable < 72, "actual bungee remains bounded through all deliveries");
            report.AppendLine("PHYSICAL releases=" + match.PhysicalReleases + " hits=" + match.PhysicalHits + " settled=" + match.SettledAttempts + " maxCable=" + maxCable + " hitchSamples=" + hitchSamples + " maxHitchSeparation=" + maximumHitchSeparation);
            match.SetupScreen(); Check(match.StartMatch() && match.rules.Round == 1 && match.rules.score[0] == 0 && match.PhysicalReleases == 0,
                "rematch restores hitch, crowd pool, turn counts and scores");
            yield return null; match.SetupScreen(); SportsNavigation.FrontEnd(); yield return null; yield return null;
            Check(FindAnyObjectByType<TrailerPlugMatch>() == null && FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude).Length == 1,
                "scene exit removes rig, crowds, replay, navigation and extra cameras/listeners");
            report.AppendLine("CHECKS=" + checks + " ERRORS=" + errors + " · actual Unity/NWH/PhysX, automated AI; no human or physical controller validation.");
            File.WriteAllText(folder + "/result.txt", report.ToString()); Application.logMessageReceived -= Error; Time.timeScale = 1;
#if UNITY_EDITOR
            if (Application.isEditor) UnityEditor.EditorApplication.isPlaying = false; else
#endif
            Application.Quit(errors == 0 ? 0 : 2);
        }
        void OnDestroy() { if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);Application.logMessageReceived -= Error; preferences?.Dispose(); SportsParticipantRegistry.ForgetCache(); Time.timeScale = 1; }
#endif
    }
}
