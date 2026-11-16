using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
namespace LWS.TruckSports
{
    // A game-night summary over the existing telemetry stream. It has no input,
    // scoring, physics or network authority and never transmits reports.
    public sealed class SportsGameNightReport : IDisposable
    {
        [Serializable] public sealed class Driver
        { public string id, name, color, avatar; public bool guest; public int points; }
        [Serializable] public sealed class Participant
        { public string id, name, source, difficulty, controller; public bool ai; public int slot; }
        [Serializable] public sealed class Match
        {
            public string id, sport, title, variant, startedUtc, endedUtc, status, interruption;
            public Settings settings;
            public double playedSeconds;
            public Participant[] participants;
            public SportsSessionTelemetry.Result[] results;
            public string[] humanPointAwards;
        }
        [Serializable] public sealed class Settings
        { public int option, weather, teamA, teamB, participants; public string configuration; public bool showAiCameras, legacyConfiguration; }
        [Serializable] public sealed class Event
        { public string utc, kind, matchId, sport, participantId, reason, message, level, stack; public double value; public int slot; }
        [Serializable] public sealed class Data
        {
            public int schema = 1;
            public bool automatedValidation;
            public string sessionId, version, startedUtc, endedUtc, savedUtc, status, telemetryFile;
            public double durationSeconds, sampledSeconds, averageFps;
            public int completedMatches, interruptedMatches, olderMatchesInTelemetry, omittedEvents, errors, warnings, performanceSamples;
            public long sampledFrames;
            public float worstFrameMs;
            public List<Driver> drivers = new List<Driver>();
            public List<Match> matches = new List<Match>();
            public List<Event> events = new List<Event>();
        }
        readonly SportsGameNightState state;
        readonly string folder;
        Match current;
        bool disposed;
        public Data Contents { get; } = new Data();
        public string JsonPath => Path.Combine(folder, "game-night-" + state.SessionId + ".json");
        public string SummaryPath => Path.Combine(folder, "game-night-" + state.SessionId + ".txt");
        public string SaveError { get; private set; }
        public SportsGameNightReport(SportsGameNightState state, string directory = null)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            folder = directory ?? Path.Combine(Application.persistentDataPath, "TruckSports",
                SportsPrefs.StorageScope == "Player" ? "Sessions" : "Validation/" + SportsPrefs.StorageScope + "/Sessions");
            Contents.sessionId = state.SessionId; Contents.version = Application.version; Contents.startedUtc = state.StartedUtc; Contents.status = "active";
            Contents.automatedValidation = Application.isBatchMode || SportsPrefs.StorageScope != "Player";
            SportsSessionTelemetry.Recorded += Observe;
        }
        public void Remember(SportsParticipant profile)
        {
            if (profile == null) return;
            var driver = Contents.drivers.FirstOrDefault(p => p.id == profile.id);
            if (driver == null) { driver = new Driver { id = profile.id }; Contents.drivers.Add(driver); }
            driver.name = profile.displayName; driver.color = "#" + ColorUtility.ToHtmlStringRGB(profile.color); driver.avatar = profile.avatarKey;
            driver.guest = SportsGameNightRuntime.IsGuest(profile.id);
        }
        public string NameFor(string id) => Contents.drivers.FirstOrDefault(p => p.id == id)?.name;
        public void BeginMatch(SportsPartyMatchBinding binding)
        {
            if (current != null) Interrupt("new-match-started");
            var session = binding.Session;
            current = new Match { id = binding.MatchId, sport = binding.SceneName, title = binding.Title, variant = binding.OptionLabel,
                settings = new Settings { option = session != null ? session.option : 0, weather = session != null ? session.weather : 0,
                    teamA = session != null ? session.teamA : binding.Legacy.TeamSplit, teamB = session != null ? session.teamB : binding.Legacy.TeamSplit > 0 ? binding.Count - binding.Legacy.TeamSplit : 0,
                    participants = binding.Count, configuration = binding.Fingerprint(), showAiCameras = SportsCameraOptions.ShowAI, legacyConfiguration = session == null },
                startedUtc = DateTime.UtcNow.ToString("O"), status = "playing",
                participants = binding.Players.Take(binding.Count).Select((p,i) => new Participant { id = p.ParticipantId, name = p.DisplayName,
                    source = binding.Source(i), difficulty = binding.Difficulty(i), controller = binding.Controller(i) != null ? binding.Controller(i).displayName : "", ai = p.IsBot, slot = i }).ToArray() };
            Contents.matches.Add(current);
            if (Contents.matches.Count > 512) { Contents.matches.RemoveAt(0); Contents.olderMatchesInTelemetry++; }
            SportsSessionTelemetry.GameNightEvent("game_night_match_started", current.id, current.sport, current.variant);
            Save();
        }
        public void Complete(SportsResultsSnapshot snapshot, IEnumerable<string> humans, double playedSeconds)
        {
            if (current == null) return;
            var allowed = new HashSet<string>(humans, StringComparer.Ordinal);
            current.results = snapshot.Rows.Select(r => new SportsSessionTelemetry.Result { participantId = r.ParticipantId, name = r.DisplayName,
                slot = r.Slot, place = r.Place, ai = r.IsAI, winner = r.Winner, unit = r.Unit, detail = r.Detail,
                hasMeasurement = r.Measurement.HasValue, measurement = r.Measurement ?? 0 }).ToArray();
            current.humanPointAwards = snapshot.Rows.Where(r => !r.IsAI && r.Winner && allowed.Contains(r.ParticipantId)).Select(r => r.ParticipantId).ToArray();
            current.playedSeconds = playedSeconds; current.endedUtc = DateTime.UtcNow.ToString("O"); current.status = "completed";
            Contents.completedMatches++; current = null; Save();
        }
        public void Interrupt(string reason)
        {
            if (current == null) return;
            current.endedUtc = DateTime.UtcNow.ToString("O"); current.status = "interrupted"; current.interruption = reason;
            current.humanPointAwards = Array.Empty<string>(); Contents.interruptedMatches++;
            SportsSessionTelemetry.GameNightEvent("match_interrupted", current.id, current.sport, reason); current = null;
        }
        public void Observe(SportsSessionTelemetry.Record record)
        {
            if (record == null || record.gameNightId != state.SessionId) return;
            if (record.kind == "telemetry_queue_overflow") Contents.omittedEvents += record.droppedEvents;
            if (record.kind == "performance_sample")
            {
                Contents.performanceSamples++; Contents.sampledFrames += record.sampleFrames;
                if (record.framesPerSecond > 0) Contents.sampledSeconds += record.sampleFrames / (double)record.framesPerSecond;
                Contents.averageFps = Contents.sampledSeconds > 0 ? Contents.sampledFrames / Contents.sampledSeconds : 0;
                Contents.worstFrameMs = Mathf.Max(Contents.worstFrameMs, record.maximumFrameMs);
                if (record.maximumFrameMs < 100) return;
            }
            if (record.kind == "application_log") { if (record.level == "Warning") Contents.warnings++; else Contents.errors++; }
            var entry = new Event { utc = record.utc, kind = record.kind, matchId = record.matchId, sport = record.sport,
                participantId = record.participantId, slot = record.slot, reason = record.reason ?? record.deviceChange,
                message = record.message ?? record.device, level = record.level, stack = record.stack,
                value = record.kind == "score_confirmed" ? record.delta : record.outcome };
            if (Contents.events.Count >= 512) { Contents.events.RemoveAt(0); Contents.omittedEvents++; }
            Contents.events.Add(entry);
        }
        public bool Save(bool finalize = false, bool interrupted = false)
        {
            SportsSessionTelemetry.FlushNow();
            if (finalize) { Interrupt(interrupted ? "application-closed" : "game-night-ended"); Contents.endedUtc ??= DateTime.UtcNow.ToString("O"); Contents.status = interrupted ? "interrupted" : "completed"; }
            Contents.savedUtc = DateTime.UtcNow.ToString("O"); Contents.telemetryFile = SportsSessionTelemetry.CurrentFile;
            if (DateTime.TryParse(Contents.startedUtc, out var start) && DateTime.TryParse(Contents.endedUtc ?? Contents.savedUtc, out var end)) Contents.durationSeconds = Math.Max(0, (end - start).TotalSeconds);
            foreach (var driver in Contents.drivers) driver.points = state.Points.TryGetValue(driver.id, out int points) ? points : 0;
            try { Directory.CreateDirectory(folder); AtomicWrite(JsonPath, JsonUtility.ToJson(Contents, true)); AtomicWrite(SummaryPath, Summary()); SaveError = null; return true; }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException) { SaveError = error.Message; return false; }
        }
        string Summary()
        {
            var text = new StringBuilder("TRUCK SPORTS: PARTY! — GAME NIGHT\n");
            text.AppendLine("Session: " + Contents.sessionId).AppendLine("Version: " + Contents.version).AppendLine("Started: " + Contents.startedUtc)
                .AppendLine("Ended: " + (Contents.endedUtc ?? "Still playing")).AppendLine("Status: " + Contents.status)
                .AppendLine("Completed matches: " + Contents.completedMatches + " · Interrupted: " + Contents.interruptedMatches).AppendLine().AppendLine("WINNER POINTS");
            foreach (var driver in Contents.drivers.OrderByDescending(d => d.points)) text.AppendLine(driver.name + " · " + driver.points + (driver.guest ? " · Guest" : ""));
            text.AppendLine().AppendLine("MATCHES");
            foreach (var match in Contents.matches)
            {
                text.AppendLine(match.title + " · " + match.variant + " · " + match.status);
                if (match.results != null) foreach (var row in match.results) text.AppendLine("  " + row.name + (row.ai ? " [AI]" : "") + " · " + (row.winner ? "WINNER · " : "") + (row.hasMeasurement ? row.measurement.ToString("0.###") + " " + row.unit : row.detail));
                if (!string.IsNullOrEmpty(match.interruption)) text.AppendLine("  Interrupted: " + match.interruption);
            }
            text.AppendLine().AppendLine("PERFORMANCE / DIAGNOSTICS").AppendLine("Sampled FPS: " + Contents.averageFps.ToString("0.0") + " · Worst sampled frame: " + Contents.worstFrameMs.ToString("0.0") + " ms")
                .AppendLine("Errors: " + Contents.errors + " · Warnings: " + Contents.warnings).AppendLine("Full event stream: " + (Contents.telemetryFile ?? "Unavailable"))
                .AppendLine("Older match details in stream: " + Contents.olderMatchesInTelemetry + " · Omitted summary events: " + Contents.omittedEvents);
            return text.ToString();
        }
        static void AtomicWrite(string path, string value)
        {
            string temporary = path + ".tmp", backup = path + ".bak";
            File.WriteAllText(temporary, value, new UTF8Encoding(false));
            if (File.Exists(path)) File.Replace(temporary, path, backup); else File.Move(temporary, path);
        }
        public void Dispose() { if (disposed) return; SportsSessionTelemetry.Recorded -= Observe; disposed = true; }
    }
}
