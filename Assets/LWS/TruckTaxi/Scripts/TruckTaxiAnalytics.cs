using System;
using System.Collections.Generic;
using LWS.InterstateHauler;
using Unity.Profiling;
using UnityEngine;

namespace LWS.TruckTaxi
{
    [DisallowMultipleComponent]
    public sealed class TruckTaxiAnalytics : MonoBehaviour
    {
        private TruckTaxiBootstrap host;
        private TruckTaxiSession session;
        private TruckTaxiRegionalWorld regional;
        private LwsSceneStreamerAdapter streamer;
        private TruckTaxiDriverNeedsCoordinator needs;
        private TruckTaxiRadio radio;
        private TruckTaxiCustomContentRunner customContent;
        private readonly TruckTaxiAnalyticsContentIds contentIds = new TruckTaxiAnalyticsContentIds();
        private TruckTaxiAnalyticsStorage storage;
        private TruckTaxiAnalyticsSessionObserver observer;
        private readonly TruckTaxiAnalyticsMetrics metrics = new TruckTaxiAnalyticsMetrics();
        private readonly HashSet<string> regions = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> approvedRegions = new HashSet<string>(StringComparer.Ordinal);
        private readonly double[] nextError = new double[5];
        private string sessionId, gameVersion, buildId, currentRegion = "";
        private double nextSample, frameSeconds, mainMs, physicsMs;
        private int frames, mainSamples, physicsSamples, lowWindows, weather = -1;
        private int rateCount, rateDropped;
        private double rateWindow;
        private bool initialized, ended, applicationPaused;
        private ProfilerRecorder mainThread, physics;
        public TruckTaxiAnalyticsMetrics Metrics => metrics;
        public bool IsInitialized => initialized && !ended;
        public int UniquePassengers => observer?.UniquePassengers ?? 0;
        public int RosterCount => observer?.RosterCount ?? 0;
        public double RosterCoveragePercent => observer?.CoveragePercent ?? 0;
        public int Rides => observer?.CompletedRides ?? 0;
        public int Crashes => observer?.Crashes ?? 0;
        public int RegionsVisited => regions.Count;
        public double Fps { get; private set; } = -1;
        public int PendingUploadEvents => storage?.PendingUploadEvents ?? 0;
        public int DroppedEvents => (storage?.DroppedEvents ?? 0) + rateDropped;
        public TruckTaxiAnalyticsUploadResult LastUploadResult => storage?.LastUploadResult ?? TruckTaxiAnalyticsUploadResult.NotConfigured;
        public TruckTaxiAnalyticsLocalResult LastLocalResult => storage?.LastLocalResult ?? TruckTaxiAnalyticsLocalResult.Closed;

        // Parent calls this after Session/Player/services are initialized, before StartShift.
        public void Initialize(TruckTaxiBootstrap value)
        {
            if (initialized || value == null || value.Session == null || (!Application.isEditor && !Debug.isDebugBuild)) return;
            initialized = true; host = value; session = value.Session;
            sessionId = Guid.NewGuid().ToString("N");
            gameVersion = TruckTaxiAnalyticsPrivacy.Token(Application.version, 48);
            if (gameVersion == "") gameVersion = "unspecified";
            buildId = TruckTaxiAnalyticsPrivacy.Token(Application.buildGUID, 64);
            if (buildId == "") buildId = Application.isEditor ? "editor" : "unspecified";
            storage = new TruckTaxiAnalyticsStorage(Application.persistentDataPath);
            Record(new TruckTaxiAnalyticsEvent { kind = TruckTaxiAnalyticsKind.SessionStarted });
            var config = host.Configuration;
            var profiles = config != null ? (config.passengerDatabase != null ? config.passengerDatabase.passengers : config.passengers) : null;
            observer = new TruckTaxiAnalyticsSessionObserver(session, profiles, Record, contentIds);
            customContent = host.CustomContent;
            if (customContent != null) customContent.PhaseChanged += RecordCustomContent;
            regional = host.GetComponent<TruckTaxiRegionalWorld>();
            if (regional != null)
                foreach (var region in regional.regions)
                {
                    string id = TruckTaxiAnalyticsPrivacy.Token(region?.id);
                    if (id != "" && approvedRegions.Count < 128) approvedRegions.Add(id);
                }
            streamer = host.GetComponent<LwsSceneStreamerAdapter>();
            if (streamer != null) streamer.SceneOperationFailed += StreamFailed;
            needs = host.DriverNeeds; if (needs != null) needs.DriverNeedsEvent += RecordDriverNeed;
            radio = host.Radio; if (radio != null) radio.Changed += RadioChanged;
            Application.logMessageReceived += LogReceived;
            try { mainThread = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 1); }
            catch (Exception) { }
            try { physics = ProfilerRecorder.StartNew(ProfilerCategory.Physics, "Physics.Simulate", 1); }
            catch (Exception) { }
            nextSample = Time.unscaledTimeAsDouble + 20;
        }
        private void Update()
        {
            if (!IsInitialized || host == null) return;
            bool active = session.State != TruckTaxiState.Inactive;
            bool paused = host.Paused || applicationPaused || Time.timeScale <= 0;
            double dt = Time.unscaledDeltaTime;
            bool recovering = host.Fuel?.IsRescuing == true || host.Roadside?.IsRecovering == true || host.FastTravel?.IsTravelling == true;
            double speed = !recovering && host.Player != null ? host.Player.LastTelemetry.speedMetersPerSecond : 0;
            metrics.Tick(dt, Time.deltaTime, speed, active, paused);
            if (active && !paused && dt > 0)
            {
                frameSeconds += dt; frames++;
                if (mainThread.Valid && mainThread.Count > 0) { mainMs += mainThread.LastValue / 1000000d; mainSamples++; }
                if (physics.Valid && physics.Count > 0) { physicsMs += physics.LastValue / 1000000d; physicsSamples++; }
            }
            double now = Time.unscaledTimeAsDouble;
            if (now < nextSample) return;
            nextSample = now + storage.PerformanceSeconds;
            ObserveWorld();
            Fps = frameSeconds > 0 ? frames / frameSeconds : -1;
            var sample = new TruckTaxiAnalyticsEvent { kind = TruckTaxiAnalyticsKind.Performance,
                fps = Fps, frameMs = frames > 0 ? frameSeconds * 1000 / frames : -1,
                mainThreadMs = mainSamples > 0 ? mainMs / mainSamples : -1,
                physicsMs = physicsSamples > 0 ? physicsMs / physicsSamples : -1,
                liveCars = host.traffic != null ? host.traffic.ActiveCount : -1,
                livePedestrians = host.pedestrians != null ? host.pedestrians.ActiveCount : -1,
                liveRivals = host.Rivals != null ? host.Rivals.LiveCount : -1,
                loadedRegions = regional != null ? regional.LoadedCount : -1,
                paused = paused || !active, regionId = currentRegion };
            Record(sample);
            if (!sample.paused && Fps >= 0 && Fps < 30) lowWindows++; else lowWindows = 0;
            if (lowWindows == 2)
            {
                sample.kind = TruckTaxiAnalyticsKind.LowPerformance; Record(sample);
            }
            Record(new TruckTaxiAnalyticsEvent { kind = TruckTaxiAnalyticsKind.SessionSnapshot });
            frameSeconds = mainMs = physicsMs = 0; frames = mainSamples = physicsSamples = 0;
            storage.RequestBatch();
        }
        private void ObserveWorld()
        {
            if (regional != null && host.Player != null)
            {
                string region = regional.ResolveRegion(host.Player.transform.position);
                currentRegion = region != null && approvedRegions.Contains(region) ? region : "";
                if (currentRegion != "" && regions.Add(currentRegion))
                    Record(new TruckTaxiAnalyticsEvent { kind = TruckTaxiAnalyticsKind.RegionVisited, regionId = currentRegion });
            }
            if (host.CalendarWeather != null && weather != (int)host.CalendarWeather.CurrentCondition)
            {
                weather = (int)host.CalendarWeather.CurrentCondition;
                Record(new TruckTaxiAnalyticsEvent { kind = TruckTaxiAnalyticsKind.WeatherChanged, code = weather });
            }
        }
        private void Record(TruckTaxiAnalyticsEvent e)
        {
            if (storage == null || ended) return;
            double now = Time.unscaledTimeAsDouble;
            if (now >= rateWindow + 1) { rateWindow = now; rateCount = 0; }
            // Roster discovery is once per session; runtime event storms have a hard ceiling.
            if (e.kind != TruckTaxiAnalyticsKind.RosterMember && ++rateCount > 128) { rateDropped++; return; }
            e.gameVersion = gameVersion; e.buildId = buildId; e.sessionId = sessionId; e.eventId = Guid.NewGuid().ToString("N");
            e.realSeconds = metrics.RealSeconds; e.gameMinutes = session.GameMinutes;
            if (e.kind == TruckTaxiAnalyticsKind.SessionStarted || e.kind == TruckTaxiAnalyticsKind.SessionSnapshot || e.kind == TruckTaxiAnalyticsKind.SessionEnded)
                metrics.CopyTo(e);
            storage.TryRecord(e);
            if (e.kind == TruckTaxiAnalyticsKind.RideFinished || e.kind == TruckTaxiAnalyticsKind.WorldEvent || e.kind == TruckTaxiAnalyticsKind.Error)
                storage.RequestBatch();
        }
        public void RecordDriverNeed(TruckTaxiDriverNeedEvent value) => RecordEnum(TruckTaxiAnalyticsKind.DriverNeed, value);
        public void RecordItemUsed(TruckTaxiNeedsItem value) => RecordEnum(TruckTaxiAnalyticsKind.ItemUsed, value);
        public void RecordServiceUsed(TruckTaxiAnalyticsService value) => RecordEnum(TruckTaxiAnalyticsKind.ServiceUsed, value);
        public void RecordNavigation(TruckTaxiAnalyticsNavigation value) => RecordEnum(TruckTaxiAnalyticsKind.NavigationUsed, value);
        public void RecordWorldEvent(TruckTaxiAnalyticsWorldEvent value) => RecordEnum(TruckTaxiAnalyticsKind.WorldEvent, value);
        public void RecordCustomContent(string stableId, TruckTaxiCustomPhase phase)
        {
            if (!IsInitialized || !Enum.IsDefined(typeof(TruckTaxiCustomPhase), phase)) return;
            string alias = contentIds.Alias(stableId); if (alias == "") return;
            Record(new TruckTaxiAnalyticsEvent { kind = TruckTaxiAnalyticsKind.CustomContentPhase,
                customContentId = alias, code = (int)phase });
        }
        private void RecordEnum<T>(TruckTaxiAnalyticsKind kind, T value) where T : struct, Enum
        {
            if (!IsInitialized || !Enum.IsDefined(typeof(T), value)) return;
            Record(new TruckTaxiAnalyticsEvent { kind = kind, code = Convert.ToInt32(value), regionId = currentRegion });
        }
        public void RecordError(TruckTaxiAnalyticsError error)
        {
            if (!IsInitialized || !Enum.IsDefined(typeof(TruckTaxiAnalyticsError), error)) return;
            int index = (int)error; double now = Time.unscaledTimeAsDouble;
            if (now < nextError[index]) return; nextError[index] = now + 30;
            RecordEnum(TruckTaxiAnalyticsKind.Error, error);
        }
        public void RecordFairness(string selectedStableId, int candidateCount, double baseWeight, double finalWeight,
            bool unseen, double recentPenalty, double archetypeModifier, double eventModifier)
        {
            if (!IsInitialized) return;
            string id = observer?.ApprovedPassenger(selectedStableId) ?? "";
            if (id == "") { RecordError(TruckTaxiAnalyticsError.InvalidPassenger); return; }
            Record(new TruckTaxiAnalyticsEvent { kind = TruckTaxiAnalyticsKind.Fairness, passengerId = id,
                candidateCount = candidateCount, baseWeight = baseWeight, finalWeight = finalWeight, unseen = unseen,
                recentPenalty = recentPenalty, archetypeModifier = archetypeModifier, eventModifier = eventModifier });
        }
        // Optional caller seam: only use when the caller knows why the offer closed.
        public void RecordOfferDisposition(string stablePassengerId, bool expired)
        {
            if (!IsInitialized) return;
            string id = observer?.ApprovedPassenger(stablePassengerId) ?? "";
            if (id != "") Record(new TruckTaxiAnalyticsEvent { kind = expired ? TruckTaxiAnalyticsKind.OfferExpired : TruckTaxiAnalyticsKind.OfferDeclined, passengerId = id });
        }
        public void ExportCsv() { if (IsInitialized) storage.RequestCsvExport(); }
        private void RadioChanged() => Record(new TruckTaxiAnalyticsEvent { kind = TruckTaxiAnalyticsKind.RadioChanged,
            code = (radio.IsOn ? 1 : 0) + (radio.Muted ? 2 : 0) });
        private void StreamFailed(string ignoredScene, string ignoredMessage) => RecordError(TruckTaxiAnalyticsError.StreamFailure);
        private void LogReceived(string ignoredMessage, string ignoredStackTrace, LogType type)
        { if (type == LogType.Exception) RecordError(TruckTaxiAnalyticsError.UnhandledException); }
        private void OnApplicationPause(bool paused)
        {
            applicationPaused = paused;
            if (paused && IsInitialized) { Record(new TruckTaxiAnalyticsEvent { kind = TruckTaxiAnalyticsKind.SessionSnapshot }); storage.RequestBatch(); }
        }
        public void EndSession()
        {
            if (!IsInitialized) return;
            observer?.Dispose();
            if (needs != null) needs.DriverNeedsEvent -= RecordDriverNeed;
            if (radio != null) radio.Changed -= RadioChanged;
            if (customContent != null) customContent.PhaseChanged -= RecordCustomContent;
            if (streamer != null) streamer.SceneOperationFailed -= StreamFailed;
            Application.logMessageReceived -= LogReceived;
            Record(new TruckTaxiAnalyticsEvent { kind = TruckTaxiAnalyticsKind.SessionEnded });
            storage.RequestBatch(); storage.Dispose(); ended = true;
            mainThread.Dispose(); physics.Dispose();
        }
        private void OnDisable() => EndSession();
        private void OnDestroy() => EndSession();
        private void OnApplicationQuit() => EndSession();
    }
}
