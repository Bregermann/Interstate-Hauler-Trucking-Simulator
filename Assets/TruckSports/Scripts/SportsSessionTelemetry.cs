using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

namespace LWS.TruckSports
{
    // Local release analytics. Gameplay/result authorities supply events; this
    // observer never awards points, changes simulation or sends data anywhere.
    public sealed class SportsSessionTelemetry : MonoBehaviour
    {
        [Serializable] public sealed class Result
        {
            public string participantId, name, unit, detail;
            public int slot, place;
            public bool ai, winner, hasMeasurement;
            public double measurement;
        }
        [Serializable] public sealed class Record
        {
            public int schema = 1;
            public long sequence;
            public string sessionId, gameNightId, utc, kind, scene, version, matchId, sport, eventId, participantId, teamId;
            public string message, stack, level, unit, reason, device, deviceChange;
            public int frame, slot, deviceId, sampleFrames, droppedEvents;
            public float realtime, framesPerSecond, minimumFrameMs, maximumFrameMs;
            public long managedBytes, unityAllocatedBytes;
            public double delta, total, outcome;
            public bool ai, team, roundEnded, matchEnded, truncated;
            public Result[] results;
        }

        const int QueueCapacity = 512;
        static readonly ConcurrentQueue<Record> pending = new ConcurrentQueue<Record>();
        static SportsSessionTelemetry instance;
        static int queued, dropped, accepting;
        static string activeSession;
        static string activeGameNight;
        public static event Action<Record> Recorded;
        StreamWriter writer;
        string scene, version;
        int frame, sampleFrames;
        float realtime, sampleSeconds, minFrame = float.MaxValue, maxFrame, nextFlush;
        long sequence;
        bool closed;
        public static string CurrentFile { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession()
        {
            instance = null; activeSession = activeGameNight = null; CurrentFile = null; Recorded = null;
            queued = dropped = accepting = 0;
            while (pending.TryDequeue(out _)) { }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (instance != null || Application.isBatchMode || SceneManager.GetActiveScene().name != SportsNavigation.Front) return;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (SportsProbeInputIsolation.Active) return;
#endif
            var owner = new GameObject("Truck Sports session telemetry");
            DontDestroyOnLoad(owner); owner.AddComponent<SportsSessionTelemetry>();
        }

        void Awake()
        {
            if (instance != null) { Destroy(gameObject); return; }
            instance = this; activeSession = Guid.NewGuid().ToString("N");
            scene = SceneManager.GetActiveScene().name; version = Application.version;
            try
            {
                string folder = Path.Combine(Application.persistentDataPath, "TruckSports", SportsPrefs.StorageScope == "Player" ? "Telemetry" : "Validation/" + SportsPrefs.StorageScope + "/Telemetry");
                Directory.CreateDirectory(folder);
                CurrentFile = Path.Combine(folder, DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ") + "-" + activeSession + ".jsonl");
                writer = new StreamWriter(new FileStream(CurrentFile, FileMode.CreateNew, FileAccess.Write, FileShare.Read));
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
            {
                closed = true; instance = null;
                Debug.LogWarning("Truck Sports telemetry unavailable: " + error.Message);
                Destroy(gameObject); return;
            }
            Volatile.Write(ref accepting, 1);
            Application.logMessageReceivedThreaded += Error;
            SceneManager.activeSceneChanged += SceneChanged;
            InputSystem.onDeviceChange += DeviceChanged;
            Enqueue(new Record { kind = "session_started", message = SystemInfo.operatingSystem + " | " + SystemInfo.graphicsDeviceName });
            Drain();
        }

        static string Limit(string value, int maximum, out bool truncated)
        {
            truncated = value != null && value.Length > maximum;
            return truncated ? value.Substring(0, maximum) : value;
        }
        static void Enqueue(Record value)
        {
            if (Volatile.Read(ref accepting) == 0) return;
            value.utc = DateTime.UtcNow.ToString("O");
            value.gameNightId = activeGameNight;
            if (Interlocked.Increment(ref queued) > QueueCapacity)
            {
                Interlocked.Decrement(ref queued); Interlocked.Increment(ref dropped); return;
            }
            pending.Enqueue(value);
        }

        public static void MatchStarted(string matchId, string sport)
            => Enqueue(new Record { kind = "match_started", matchId = matchId, sport = sport });
        public static void BeginGameNight(string id) { activeGameNight = id; Enqueue(new Record { kind = "game_night_started" }); }
        public static void EndGameNight(string id)
        {
            if (activeGameNight != id) return;
            Enqueue(new Record { kind = "game_night_ended" }); FlushNow(); activeGameNight = null;
        }
        public static void GameNightEvent(string kind, string matchId = null, string sport = null, string message = null)
            => Enqueue(new Record { kind = kind, matchId = matchId, sport = sport, message = message });
        public static void GameplayEvent(string sport, string eventId, string participantId, string reason, double value = 0)
        {
            if (!double.IsFinite(value)) return;
            Enqueue(new Record { kind = "gameplay_event", sport = sport, eventId = eventId, participantId = participantId, reason = reason, outcome = value });
        }
        public static void FlushNow() { if (instance != null && !instance.closed) { instance.Drain(); instance.Flush(); } }
        public static void ConfirmedScore(SportsScoreEvent score)
        {
            if (score == null || instance == null) return;
            Enqueue(new Record { kind = score.Kind == SportsScoreKind.Outcome ? "outcome_confirmed" : "score_confirmed",
                matchId = score.MatchId, sport = score.SportId, eventId = score.EventId,
                participantId = score.ParticipantId, teamId = score.TeamId, slot = score.Slot,
                ai = score.IsAI, team = score.IsTeam, delta = score.Delta, total = score.Total,
                outcome = score.OutcomeValue, unit = score.Unit, reason = score.Reason,
                roundEnded = score.RoundEnded, matchEnded = score.MatchEnded });
        }
        public static void MatchCompleted(string matchId, string sport, SportsResultsSnapshot snapshot)
        {
            if (snapshot == null || instance == null) return;
            Enqueue(new Record { kind = "match_completed", matchId = matchId, sport = sport,
                message = snapshot.Summary, results = snapshot.Rows.Select(row => new Result {
                    participantId = row.ParticipantId, name = row.DisplayName, slot = row.Slot,
                    place = row.Place, winner = row.Winner, ai = row.IsAI, unit = row.Unit,
                    detail = row.Detail, hasMeasurement = row.Measurement.HasValue,
                    measurement = row.Measurement ?? 0 }).ToArray() });
        }

        void Error(string text, string trace, LogType type)
        {
            if (type != LogType.Warning && type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            string message = Limit(text, 8192, out bool first), stack = Limit(trace, 16384, out bool second);
            Enqueue(new Record { kind = "application_log", level = type.ToString(), message = message, stack = stack, truncated = first || second });
        }
        void SceneChanged(Scene previous, Scene next)
        {
            scene = next.name;
            Enqueue(new Record { kind = "scene_changed", scene = next.name, message = previous.name });
        }
        void DeviceChanged(InputDevice device, InputDeviceChange change)
            => Enqueue(new Record { kind = "device_changed", deviceId = device.deviceId,
                device = device.displayName + " / " + device.layout, deviceChange = change.ToString() });

        void Update()
        {
            if (closed) return;
            frame = Time.frameCount; realtime = Time.realtimeSinceStartup;
            float dt = Time.unscaledDeltaTime;
            if (dt > 0) { sampleFrames++; sampleSeconds += dt; minFrame = Mathf.Min(minFrame, dt); maxFrame = Mathf.Max(maxFrame, dt); }
            if (sampleSeconds >= 10)
            {
                Enqueue(new Record { kind = "performance_sample", sampleFrames = sampleFrames,
                    framesPerSecond = sampleFrames / sampleSeconds, minimumFrameMs = minFrame * 1000,
                    maximumFrameMs = maxFrame * 1000, managedBytes = GC.GetTotalMemory(false),
                    unityAllocatedBytes = Profiler.GetTotalAllocatedMemoryLong() });
                sampleFrames = 0; sampleSeconds = 0; minFrame = float.MaxValue; maxFrame = 0;
            }
            Drain(64);
            if (realtime >= nextFlush) { Flush(); nextFlush = realtime + 2; }
        }
        void Drain(int maximum = int.MaxValue)
        {
            try
            {
                int lost = Interlocked.Exchange(ref dropped, 0);
                if (lost > 0) Write(new Record { kind = "telemetry_queue_overflow", gameNightId = activeGameNight, droppedEvents = lost, utc = DateTime.UtcNow.ToString("O") });
                while (maximum-- > 0 && pending.TryDequeue(out var value)) { Interlocked.Decrement(ref queued); Write(value); }
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException) { StopAfterIoFailure(); }
        }
        void Write(Record value)
        {
            value.sessionId = activeSession; value.sequence = ++sequence;
            value.scene = value.scene ?? scene; value.version = version; value.frame = frame; value.realtime = realtime;
            writer.WriteLine(JsonUtility.ToJson(value));
            Recorded?.Invoke(value);
        }
        void Flush()
        {
            try { writer?.Flush(); }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException) { StopAfterIoFailure(); }
        }
        void StopAfterIoFailure()
        {
            // Disk failure must never break match completion or recurse through logging.
            closed = true; Volatile.Write(ref accepting, 0); Unsubscribe();
            try { writer?.Dispose(); } catch (IOException) { }
            writer = null;
        }
        void Unsubscribe()
        {
            Application.logMessageReceivedThreaded -= Error;
            SceneManager.activeSceneChanged -= SceneChanged;
            InputSystem.onDeviceChange -= DeviceChanged;
        }
        void OnApplicationQuit() => Close();
        void OnDestroy() { if (instance == this) { Close(); instance = null; } }
        void Close()
        {
            if (closed) return;
            Enqueue(new Record { kind = "session_ended" }); Volatile.Write(ref accepting, 0); Unsubscribe(); Drain(); Flush();
            try { writer?.Dispose(); } catch (IOException) { }
            writer = null; closed = true;
        }
    }
}
