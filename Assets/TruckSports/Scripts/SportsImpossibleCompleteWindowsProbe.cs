#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Profiling;

namespace LWS.TruckSports
{
    // Opt-in extension of the existing saved-scene tour. Existing Sports modes,
    // NWH, PhysX, weather and score authorities perform every game operation.
    public sealed partial class SportsImpossibleWindowsProbe
    {
        const string CompleteFlag = "-tsImpossibleComplete", ScenesFlag = "-tsImpossibleCompleteScenes";
        const string RepeatFlag = "-tsImpossibleCompleteRepeat", ViewsFlag = "-tsImpossibleCompleteViews";
        const string ScaleFlag = "-tsImpossibleCompleteScale", ForecastFlag = "-tsImpossibleCompleteWeather";
        const BindingFlags StateAccess = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        bool extended, splitViews;
        int repetitions = 1, completionScale = 4;
        string forcedForecast;
        float extendedLimit;
        readonly List<CompleteFact> completeFacts = new List<CompleteFact>();
        readonly List<MemoryFact> memoryFacts = new List<MemoryFact>();
        readonly List<PerformanceFact> performanceFacts = new List<PerformanceFact>();
        readonly List<ObservedScore> observedScores = new List<ObservedScore>();

        [Serializable] sealed class ObservedScore
        {
            public string scene, matchId, eventId, participantId, reason, kind, tier;
            public int owner;
            public double delta, total, outcome;
        }
        void ObserveCompleteScore(SportsScoreEvent value)
        {
            observedScores.Add(new ObservedScore { scene = current, matchId = value.MatchId, eventId = value.EventId, participantId = value.ParticipantId,
                owner = value.Slot, delta = value.Delta, total = value.Total, outcome = value.OutcomeValue, kind = value.Kind.ToString(), tier = value.Tier.ToString(), reason = value.Reason });
        }

        [Serializable] sealed class CompleteFact
        {
            public string scene, matchId, stopReason, header, status, results, ruleState, weatherPresetId, weather, timeOfDay;
            public bool finished, untimed, paused, rematched, pauseVerified, currentlyPaused;
            public float realSeconds, clock, completionTimeScale, grip, windMetresPerSecond;
            public int journalEntries, option, players, truthfulGuessTurns, identificationWinner = -1;
            public int packedSnowballs, decorationMask, measuredSnowmanGrade, audibleNotes;
            public RhythmFact[] rhythm;
        }
        [Serializable] sealed class RhythmFact
        {
            public int owner, chartNotes, resolved, successfulJudgments, ruleScore, concertTotal, maximumCombo;
        }
        [Serializable] sealed class MemoryFact
        {
            public string scene, phase;
            public int repeat, objects, bodies, colliders, materials, textures, meshes;
            public long allocated, managed, graphics;
            public ResourceFamily[] meshFamilies, materialFamilies;
        }
        [Serializable] sealed class ResourceFamily { public string name; public int count; }
        static ResourceFamily[] ResourceFamilies<T>() where T : UnityEngine.Object
            => Resources.FindObjectsOfTypeAll<T>().GroupBy(item => item.name).Select(group => new ResourceFamily { name = group.Key, count = group.Count() }).OrderBy(item => item.name, StringComparer.Ordinal).ToArray();
        [Serializable] sealed class PerformanceFact
        {
            public string scene, path, scope, weatherPresetId, weather, timeOfDay;
            public int width, height, views;
            public SportsExpansionPerformance.Receipt sample;
        }
        [Serializable] sealed class CompleteSummary
        {
            public string scope = "ACTUAL_WINDOWS_ORIGINAL_AI_COMPLETE_SESSIONS_AND_UNITY_TELEMETRY_NOT_HUMAN_OR_DEVICE_CERTIFICATION";
            public string cpu, gpu, os, backend, version, buildGuid;
            public int cpuThreads, systemMemoryMB, graphicsMemoryMB, targetFrameRate, vSync, repetitions;
            public bool interrupted, splitAIViews;
            public float fixedDelta;
            public CompleteFact[] sessions;
            public MemoryFact[] memory;
            public PerformanceFact[] performance;
            public ObservedScore[] originalScoreReceipts;
        }

        public static string[] CompleteScenes(string[] args)
        {
            int at = Array.IndexOf(args, ScenesFlag);
            if (at < 0) return SportsImpossibleGames.All.Select(e => e.scene).ToArray();
            if (at + 1 >= args.Length) throw new ArgumentException("Explicit complete-scene list required.");
            var selected = args[at + 1].Split(',');
            if (selected.Length == 0 || selected.Distinct().Count() != selected.Length || selected.Any(s => !SportsImpossibleGames.TryFindScene(s, out _)))
                throw new ArgumentException("Use distinct registered Impossible scene names only.");
            return selected;
        }

        public static int CompleteSimulationScale(string[] args)
        {
            int at = Array.IndexOf(args, ScaleFlag);
            if (at < 0) return 4;
            if (at + 1 >= args.Length || !int.TryParse(args[at + 1], out int value) || value != 1 && value != 4)
                throw new ArgumentException("Complete-session observation supports explicit normal1x or accelerated4x only; fixedDelta is unchanged.");
            return value;
        }

        IEnumerable<SportsImpossibleGames.Entry> ConfigureExtended(string[] args)
        {
            extended = Array.IndexOf(args, CompleteFlag) >= 0;
            if (!extended) return SportsImpossibleGames.All;
            string[] selected = CompleteScenes(args);
            completionScale = CompleteSimulationScale(args);
            int forecast = Array.IndexOf(args, ForecastFlag);
            if (forecast >= 0)
            {
                if (forecast + 1 >= args.Length || string.IsNullOrWhiteSpace(args[forecast + 1])) throw new ArgumentException("Explicit existing forecast display name required.");
                forcedForecast = args[forecast + 1];
            }
            int at = Array.IndexOf(args, RepeatFlag);
            if (at >= 0 && (at + 1 >= args.Length || !int.TryParse(args[at + 1], out repetitions) || repetitions < 1 || repetitions > 5))
                throw new ArgumentException("Use one to five explicit ordinary reset observations.");
            at = Array.IndexOf(args, ViewsFlag);
            if (at >= 0)
            {
                if (at + 1 >= args.Length || args[at + 1] != "shared" && args[at + 1] != "split") throw new ArgumentException("Use shared or split camera presentation.");
                splitViews = args[at + 1] == "split";
            }
            SportsCameraOptions.Set(splitViews);
            extendedLimit = selected.Sum(s => RealBudget(s) + 100 + repetitions * 25);
            report.AppendLine("EXTENDED_CONFIG scenes=" + string.Join(",", selected) + " option=0 ALL_AI=requested4_CLAMPED_TO_ORIGINAL_MODE_MIN_MAX actualCountInEachSessionFact completionScale=" + completionScale + "_EXCEPT_ARCHAEOLOGY_AND_UNTIMED_1 profilerScale=1 fixedDelta=" + Time.fixedDeltaTime + " repeats=" + repetitions + " requestedViews=" + (splitViews ? "split" : "shared") + " requestedForecast=" + (forcedForecast ?? "Random from original registry"));
            return selected.Select(s => { SportsImpossibleGames.TryFindScene(s, out var entry); return entry; }).ToArray();
        }
        void ApplyExtendedForecast(SportsEventSession match)
        {
            if (forcedForecast == null) return;
            var catalog = match.presentationWeather == null ? null : match.presentationWeather.presets;
            if (catalog == null) { report.AppendLine("SCOPE " + current + " no exterior weather adapter; original indoor presentation retained."); return; }
            var entry = catalog.entries.SingleOrDefault(e => e.displayName == forcedForecast);
            if (entry == null) throw new ArgumentException("Requested forecast is absent from this original scene registry: " + forcedForecast);
            catalog.Select(entry.id); // Same supported pre-match choice as the actual weather UI.
        }

        static float RealBudget(string scene) => scene == "TruckArchaeology" ? 2100 : scene == "TruckSnowmanBuilding" || scene == "TruckNestBuilding" ? 650 : scene == "TruckCamping" || scene == "TruckPetPals" ? 150 : 400;
        // These modes deliberately do not emit intermediate action points. Verify
        // their real construction/rhythm authorities instead of inventing awards.
        public static bool CompleteSnowmanEvidence(int placed, int decorations, bool complete, int grade, double award, double total)
            => placed == 3 && decorations == 63 && complete && grade > 0 && award == grade && total == grade;
        public static bool CompleteRhythmEvidence(int chartNotes, int resolved, int successes, int ruleScore, int total, bool singleSong)
            => chartNotes > 0 && resolved == chartNotes && successes > 0 && successes <= resolved && ruleScore > 0 && (singleSong ? total == ruleScore : total >= ruleScore);
        static RhythmFact[] RhythmFacts(SportsEventSession match)
        {
            if (!(match is LWS.TruckDanceOff.DanceMatch dance) || dance.Rules == null) return Array.Empty<RhythmFact>();
            return dance.Rules.Take(match.setup.count).Select((r, i) => new RhythmFact { owner = i, chartNotes = r.Count, resolved = r.Resolved,
                successfulJudgments = r.Perfect + r.Great + r.Good, ruleScore = r.Score, concertTotal = dance.total[i], maximumCombo = r.MaximumCombo }).ToArray();
        }
        static object StateValue(object value, string name)
        {
            if (value == null) return null;
            var type = value.GetType(); var field = type.GetField(name, StateAccess);
            return field != null ? field.GetValue(value) : type.GetProperty(name, StateAccess)?.GetValue(value);
        }
        static float SessionClock(SportsEventSession match)
        {
            foreach (string name in new[] { "Clock", "WildernessSeconds", "RoundTime", "Elapsed", "SongTime" })
            { var v = StateValue(match, name); if (v is float f) return f; if (v is double d) return (float)d; }
            var rules = StateValue(match, "rules") ?? StateValue(match, "Rules");
            return StateValue(rules, "Clock") is float clock ? clock : -1;
        }
        string RuleState(SportsEventSession match)
        {
            var rules = StateValue(match, "rules") ?? StateValue(match, "Rules");
            if (rules == null) return "No separate rules object; original session owns state.";
            try { return JsonUtility.ToJson(rules); } catch (Exception e) { return "Unavailable rule serialization: " + e.GetType().Name; }
        }
        CompleteFact Fact(SportsEventSession match, string reason, float entered, bool untimed, bool paused)
        {
            return new CompleteFact { scene = current, matchId = match.MatchId, stopReason = reason, header = match.Header, status = match.Status,
                results = match.Finished ? match.Results : "", ruleState = RuleState(match), finished = match.Finished, untimed = untimed,
                paused = paused, pauseVerified = paused, currentlyPaused = match.Paused, // Retain the earlier verification field; explicitly distinguish live state.
                weatherPresetId = match.presentationWeather == null ? null : match.presentationWeather.PresetId,
                weather = match.presentationWeather == null ? "NO WEATHER ADAPTER" : match.presentationWeather.Description,
                timeOfDay = match.presentationWeather == null || match.presentationWeather.matchTime == null ? "NO TIME ADAPTER" : match.presentationWeather.matchTime.Applied.ToString(),
                grip = match.presentationWeather == null ? 1 : match.presentationWeather.Grip,
                windMetresPerSecond = match.presentationWeather == null ? 0 : match.presentationWeather.Flow.magnitude,
                realSeconds = Time.realtimeSinceStartup - entered, clock = SessionClock(match), completionTimeScale = Time.timeScale,
                journalEntries = match.ScoreJournal.Count, option = match.option, players = match.setup.count,
                truthfulGuessTurns = match is LWS.TruckGuessThat.GuessMatch guess ? guess.Rules.Turns : 0,
                identificationWinner = match is LWS.TruckGuessThat.GuessMatch solved ? solved.Rules.Winner : -1,
                packedSnowballs = match is LWS.TruckSnowmanBuilding.SnowmanMatch snow ? snow.Rules.Placed : 0,
                decorationMask = match is LWS.TruckSnowmanBuilding.SnowmanMatch decorated ? decorated.Rules.Decorations : 0,
                measuredSnowmanGrade = match is LWS.TruckSnowmanBuilding.SnowmanMatch graded ? graded.Rules.Score : 0,
                audibleNotes = match is LWS.TruckOrchestra.OrchestraMatch orchestra ? orchestra.AudibleNotes : 0, rhythm = RhythmFacts(match) };
        }

        IEnumerator CompleteVisit(SportsEventSession match)
        {
            float entered = Time.realtimeSinceStartup;
            int initialJournal = match.ScoreJournal.Count;
            match.ScoreConfirmed += ObserveCompleteScore;
            bool untimed = current == "TruckCamping" || current == "TruckPetPals";
            var camp = match as LWS.TruckCamping.CampingMatch;
            var campStart = camp == null ? Array.Empty<Vector3>() : camp.explorers.Take(match.setup.count).Select(e => e.truck.body.position).ToArray();
            int discoveryStart = camp == null ? 0 : camp.explorers.Take(match.setup.count).Sum(e => e.Journal.discovered);
            Time.timeScale = 1;
            Memory("initial-live", 0);
            foreach (int width in new[] { 1280, 1920 })
            {
                if (!match.Live) break;
                Screen.SetResolution(width, width == 1280 ? 720 : 1080, FullScreenMode.Windowed);
                yield return new WaitForSecondsRealtime(.6f);
                if (!match.Live) break; // A short original match may finish during the real resize wait.
                string output = Path.Combine(folder, current + "-live-profiler-" + width);
                var root = new GameObject("Explicit original Unity live profiler window");
                var observer = root.AddComponent<SportsExpansionPerformance>(); observer.BeginWindow(match, output);
                float until = Time.realtimeSinceStartup + 16;
                while (match.Live && Time.realtimeSinceStartup < until) yield return null;
                var receipt = observer.EndWindow(); Destroy(root);
                performanceFacts.Add(new PerformanceFact { scene = current, path = Path.GetFileName(output) + "/performance.json", width = width, height = width == 1280 ? 720 : 1080,
                    views = (match.cameras ?? Array.Empty<Camera>()).Count(c => c != null && c.isActiveAndEnabled && c.targetTexture == null), sample = receipt,
                    weatherPresetId = match.presentationWeather == null ? null : match.presentationWeather.PresetId,
                    weather = match.presentationWeather == null ? "NO WEATHER ADAPTER" : match.presentationWeather.Description,
                    timeOfDay = match.presentationWeather == null || match.presentationWeather.matchTime == null ? "NO TIME ADAPTER" : match.presentationWeather.matchTime.Applied.ToString(),
                    scope = "Original live normal-speed play; five-live-second warmup. Short naturally finished modes may have no eligible samples. GPU/physics zero sample count means unavailable, not zero cost." });
                report.AppendLine("PERFORMANCE_WINDOW " + current + " " + JsonUtility.ToJson(performanceFacts.Last()));
            }
            bool paused = false;
            if (match.Live)
            {
                match.Pause(0, false);
                float pauseDeadline = Time.realtimeSinceStartup + 5;
                while (!match.Paused && Time.realtimeSinceStartup < pauseDeadline) yield return null;
                yield return null; yield return null;
                var before = FindObjectsByType<Rigidbody>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(b => b.gameObject.scene == match.gameObject.scene).Select(b => (body: b, position: b.position, rotation: b.rotation)).ToArray();
                float clock = SessionClock(match); int journal = match.ScoreJournal.Count; string rules = RuleState(match);
                yield return new WaitForSecondsRealtime(.2f);
                paused = match.Paused && before.All(b => b.body != null && Vector3.Distance(b.position, b.body.position) < .0001f && b.rotation.Equals(b.body.rotation)) && clock == SessionClock(match) && rules == RuleState(match) && journal == match.ScoreJournal.Count;
                Check(paused, "complete-session original pause preserves actual body poses, available original clock/serialized rule state and score journal");
                match.Resume(); yield return null;
            }
            if (!match.Finished) Time.timeScale = untimed || current == "TruckArchaeology" ? 1 : completionScale;
            float next = 0;
            while (!match.Finished && Time.realtimeSinceStartup - entered < RealBudget(current))
            {
                if (untimed && SessionClock(match) >= 95)
                {
                    // The advertised cooperative session has no forced winner.
                    // This is the same existing FINISH control as the human HUD.
                    match.ExtraAction("camp-finish");
                }
                if (Time.realtimeSinceStartup >= next)
                {
                    next = Time.realtimeSinceStartup + 15;
                    report.AppendLine("COMPLETE_LIVE " + JsonUtility.ToJson(Fact(match, "ORIGINAL_AI_PLAYING", entered, untimed, paused)));
                    File.WriteAllText(Path.Combine(folder, "progress.txt"), report.ToString());
                }
                yield return null;
            }
            Check(match.Finished, "original complete session reaches its own results within disclosed real watchdog");
            Check(!match.Finished || match.menus.Screen == SportsEventScreen.Result && !string.IsNullOrWhiteSpace(match.Results), "finished original mode presents real results");
            match.ScoreConfirmed -= ObserveCompleteScore;
            var scores = observedScores.Where(s => s.matchId == match.MatchId).ToArray();
            Check(scores.Select(s => s.eventId).Distinct().Count() == scores.Length && initialJournal + scores.Length == match.ScoreJournal.Count,
                "observed original immutable score events match the authoritative journal with no duplicate delivery/action credit");
            if (match is LWS.TruckGuessThat.GuessMatch guessing)
                Check(guessing.Rules.Winner >= 0 && guessing.Rules.Winner < match.setup.count && guessing.Rules.Turns > 0 && guessing.LastAnswer.StartsWith("CORRECT", StringComparison.Ordinal)
                    && scores.Any(s => s.eventId == "guess.solved" && s.owner == guessing.Rules.Winner && s.delta == 1 && s.total == 1 && s.tier == SportsScoreTier.MatchChampion.ToString()),
                    "original legal identification wins after actual truthful turns and emits its sole champion point; no intermediate points required");
            else if (match is LWS.TruckSnowmanBuilding.SnowmanMatch snowman)
                Check(scores.Length == 1 && scores[0].eventId == "snowman.completed" && scores[0].tier == SportsScoreTier.MatchChampion.ToString()
                    && CompleteSnowmanEvidence(snowman.Rules.Placed, snowman.Rules.Decorations, snowman.Rules.Complete, snowman.Rules.Score, scores[0].delta, scores[0].total),
                    "all three real packed balls and six delivered decorations produce the original measured grade exactly once");
            else if (match is LWS.TruckOrchestra.OrchestraMatch orchestra)
                Check(orchestra.Phase == LWS.TruckDanceOff.DancePhase.Complete && orchestra.AudibleNotes > 0
                    && RhythmFacts(match).Length == match.setup.count && RhythmFacts(match).All(r => CompleteRhythmEvidence(r.chartNotes, r.resolved, r.successfulJudgments, r.ruleScore, r.concertTotal, orchestra.Format == 0)),
                    "original DSP rhythm rules resolve every chart with successful notes and conserved participant totals; no synthetic score-journal awards required");
            else if (!untimed) Check(scores.Any(s => s.tier != SportsScoreTier.MatchChampion.ToString() && (s.kind == SportsScoreKind.Points.ToString() ? s.delta > 0 : s.outcome > 0)),
                "at least one positive original gameplay award occurs before champion feedback; timeout alone is not gameplay acceptance");
            if (untimed)
            {
                Check(match.ScoreJournal.Count == 0, "untimed cooperative activity never fabricates competitive points");
                Check(camp != null && camp.explorers.Take(match.setup.count).Select((e, i) => Vector3.Distance(e.truck.body.position, campStart[i])).Max() > 10,
                    "original cooperative AI actually travels through the physical wilderness");
                if (match is LWS.TruckPetPals.PetPalsMatch pets)
                {
                    Check(pets.companions.Take(match.setup.count).All(p => p.Record != null && p.Record.walked > 8), "actual selected companions genuinely walk with their owners");
                    Check(pets.companions.Take(match.setup.count).Any(p => p.NaturalPees > 0 || p.Record != null && p.Record.baths > 0) || pets.petToys.Any(t => t.PhysicalReturns > 0),
                        "at least one real original autonomous care/grass/fetch interaction completes");
                }
                else Check(camp.explorers.Take(match.setup.count).Sum(e => e.Journal.discovered) != discoveryStart || camp.explorers.Take(match.setup.count).Any(e => e.Journal.mealsCooked > 0 || e.Journal.fishCaught > 0 || e.Journal.naps > 0),
                    "original exploration discovers a landmark or completes a real camp activity");
            }
            completeFacts.Add(Fact(match, match.Finished ? "ORIGINAL_SESSION_FINISHED" : "REAL_WATCHDOG_UNFINISHED", entered, untimed, paused));
            if (match.Finished) yield return Capture("actual-complete-results");
            Time.timeScale = 1; Memory("complete-or-deadline", 0);
        }

        IEnumerator RepeatedVisit(SportsEventSession match)
        {
            for (int repeat = 1; repeat < repetitions; repeat++)
            {
                string prior = match.MatchId; Check(match.StartMatch() && match.MatchId != prior, "repeated original StartMatch creates fresh identity cycle=" + repeat);
                Time.timeScale = 1;
                int journal = match.ScoreJournal.Count;
                Check(journal == 0, "repeated original match starts with an empty score journal cycle=" + repeat);
                float until = Time.realtimeSinceStartup + 18;
                while (match.Live && Time.realtimeSinceStartup < until) yield return null;
                Memory("repeat-live", repeat); match.SetupScreen(); yield return null; yield return null;
                Memory("repeat-stopped", repeat);
            }
        }
        MemoryFact Memory(string phase, int repeat)
        {
            var fact = new MemoryFact { scene = current, phase = phase, repeat = repeat, objects = Resources.FindObjectsOfTypeAll<GameObject>().Length,
                bodies = FindObjectsByType<Rigidbody>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length,
                colliders = FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length,
                materials = Resources.FindObjectsOfTypeAll<Material>().Length, textures = Resources.FindObjectsOfTypeAll<Texture>().Length, meshes = Resources.FindObjectsOfTypeAll<Mesh>().Length,
                allocated = Profiler.GetTotalAllocatedMemoryLong(), managed = Profiler.GetMonoUsedSizeLong(), graphics = Profiler.GetAllocatedMemoryForGraphicsDriver(),
                meshFamilies = ResourceFamilies<Mesh>(), materialFamilies = ResourceFamilies<Material>() };
            memoryFacts.Add(fact); return fact;
        }
        IEnumerator RecordCollectionMemory()
        {
            Memory("collection-natural-before-explicit-cleanup", 0);
            yield return Resources.UnloadUnusedAssets(); GC.Collect(); yield return null;
            Memory("collection-after-explicit-unload-and-GC", 0);
        }
        void WriteExtendedSummary(bool interrupted)
        {
            var summary = new CompleteSummary { cpu = SystemInfo.processorType, gpu = SystemInfo.graphicsDeviceName, os = SystemInfo.operatingSystem,
                backend = SystemInfo.graphicsDeviceType.ToString(), version = Application.version, buildGuid = Application.buildGUID,
                cpuThreads = SystemInfo.processorCount, systemMemoryMB = SystemInfo.systemMemorySize, graphicsMemoryMB = SystemInfo.graphicsMemorySize,
                targetFrameRate = Application.targetFrameRate, vSync = QualitySettings.vSyncCount, repetitions = repetitions, interrupted = interrupted,
                splitAIViews = splitViews, fixedDelta = Time.fixedDeltaTime, sessions = completeFacts.ToArray(), memory = memoryFacts.ToArray(), performance = performanceFacts.ToArray(), originalScoreReceipts = observedScores.ToArray() };
            File.WriteAllText(Path.Combine(folder, "complete-summary.json"), JsonUtility.ToJson(summary, true));
        }
    }
}
#endif
