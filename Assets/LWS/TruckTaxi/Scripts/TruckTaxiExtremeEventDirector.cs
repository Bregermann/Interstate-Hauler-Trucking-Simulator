using System;
using System.Collections.Generic;
using NWH.VehiclePhysics2.GroundDetection;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // Taxi event semantics only. The forecast owner applies Weather Maker profiles through LWS.
    [DisallowMultipleComponent]
    public sealed class TruckTaxiExtremeEventDirector : MonoBehaviour
    {
        private static readonly TruckTaxiExtremeKind[] naturallyScheduledKinds =
        { TruckTaxiExtremeKind.Tornado, TruckTaxiExtremeKind.Earthquake,
          TruckTaxiExtremeKind.Avalanche, TruckTaxiExtremeKind.Mudslide };
        [SerializeField] private TruckTaxiHazardZone[] authoredZones = Array.Empty<TruckTaxiHazardZone>();
        [SerializeField, Min(50)] private float visualDistanceMeters = 350;
        [SerializeField, Min(0)] private float maximumPlayerForceNewtons = 3500;
        [SerializeField, Min(0)] private float maximumQuakeAcceleration = 1.5f;
        [SerializeField, Range(0, 8)] private int maximumVisualActors = 3;
        [SerializeField, Range(0, 4)] private int maximumDebrisPiles = 2;
        [SerializeField] private TruckTaxiExtremeFootprintSettings footprintSettings = new TruckTaxiExtremeFootprintSettings();
        [SerializeField] private TruckTaxiExtremeScheduleSettings scheduleSettings = new TruckTaxiExtremeScheduleSettings();
        [SerializeField] private AudioClip tornadoClip;
        [SerializeField] private AudioClip earthquakeClip;
        [SerializeField] private AudioClip slideClip;

        private sealed class ActiveEvent
        {
            public TruckTaxiExtremeFootprint Footprint;
            public DateTime Started;
            public DateTime MovingEnds;
            public DateTime Ends;
            public GameObject Visual;
            public GameObject RoadDebris;
            public bool Nearby;
            public bool PhysicsRelevant;
            public Vector3 Position;
            public Vector3[] PreviousPropOffsets;
            public float RealStarted;
        }

        private readonly Dictionary<string, TruckTaxiHazardZone> zones = new Dictionary<string, TruckTaxiHazardZone>();
        private readonly List<ActiveEvent> active = new List<ActiveEvent>();
        private readonly List<TruckTaxiExtremeSnapshot> snapshots = new List<TruckTaxiExtremeSnapshot>();
        private readonly List<TruckTaxiExtremeWarning> warnings = new List<TruckTaxiExtremeWarning>();
        private readonly Dictionary<string, DateTime> lastNaturalAttempt = new Dictionary<string, DateTime>();
        private readonly List<TruckTaxiExtremeAttempt> attempts = new List<TruckTaxiExtremeAttempt>();
        private readonly string[] lastRejection = new string[5];
        private TruckTaxiExtremeDaySchedule daySchedule;
        private DateTime lastScheduledDay;
        private int proceduralSequence;
        private readonly List<ScheduledEvent> scheduled = new List<ScheduledEvent>();
        private readonly struct ScheduledEvent
        {
            public ScheduledEvent(TruckTaxiExtremeKind kind, string zoneId, DateTime startsAt)
            { Kind = kind; ZoneId = zoneId; StartsAt = startsAt; }
            public TruckTaxiExtremeKind Kind { get; }
            public string ZoneId { get; }
            public DateTime StartsAt { get; }
        }

        private TruckTaxiBootstrap host;
        private TruckTaxiRegionalWorld regional;
        private Rigidbody playerBody;
        private Func<DateTime> gameNow;
        private Action<TruckTaxiExtremeWeatherProfile?, TimeSpan> setWeather;
        private readonly System.Random naturalRandom = new System.Random(71629);
        private bool severeStorm;
        private bool heavySnow;
        private float continuousHeavyRainHours;
        private float nextNaturalTick;
        private Transform shakeCamera;
        private Vector3 previousShakeOffset;
        private Vector3 cameraPositionBeforeShake;
        private Material fallbackParticleMaterial;
        private bool fallbackMaterialResolved;
        private float nextVisualTick;
        private DateTime lastGameNow;
        private bool initialized;

        public IReadOnlyList<TruckTaxiExtremeSnapshot> ActiveSnapshots => snapshots;
        public IReadOnlyList<TruckTaxiExtremeWarning> Warnings => warnings;
        public IReadOnlyList<TruckTaxiExtremeAttempt> Attempts => attempts;
        public int LivePhysicsActorCount { get; private set; }
        public int LiveVisualActorCount { get; private set; }
        public TruckTaxiExtremeWeatherProfile? ActiveSevereProfile { get; private set; }
        public bool RoutingClosureSupported => false;
        public event Action<TruckTaxiExtremeWarning> WarningRaised;
        public event Action HazardChanged;
        public event Action<TruckTaxiExtremeSnapshot> LocalReactionRequested;
        public float CameraShakeIntensity01 { get; private set; }
        public DateTime LastNaturalEvent { get; private set; }
        public TruckTaxiExtremeKind? LastNaturalKind { get; private set; }
        public TruckTaxiExtremeSnapshot? CurrentEvent => snapshots.Count > 0 ? snapshots[0] : (TruckTaxiExtremeSnapshot?)null;

        public bool ForceTornadoHere() => StartEventHere(TruckTaxiExtremeKind.Tornado);
        public bool ForceEarthquakeHere() => StartEventHere(TruckTaxiExtremeKind.Earthquake);
        public bool ForceAvalancheHere() => StartEventHere(TruckTaxiExtremeKind.Avalanche);
        public bool ForceMudslideHere() => StartEventHere(TruckTaxiExtremeKind.Mudslide);

        public TruckTaxiExtremeScheduleStatus GetScheduleStatus(TruckTaxiExtremeKind kind)
        {
            bool eligible = TruckTaxiExtremeRules.NaturalEligible(kind, severeStorm, heavySnow, continuousHeavyRainHours);
            DateTime last = daySchedule != null ? daySchedule.LastOccurrence(kind) : default;
            return new TruckTaxiExtremeScheduleStatus(kind,
                last,
                daySchedule != null ? daySchedule.DryDays(kind) : 0,
                last == default ? -1 : Math.Max(0, (int)(gameNow().Date - last.Date).TotalDays),
                daySchedule != null ? daySchedule.CurrentProbability(kind, eligible) : 0,
                eligible, lastRejection[(int)kind]);
        }

        public void BindCamera(Transform camera)
        {
            RestoreCamera();
            shakeCamera = camera;
        }

        public bool Initialize(TruckTaxiBootstrap owner, Func<DateTime> now,
            Action<TruckTaxiExtremeWeatherProfile?, TimeSpan> weatherCallback = null)
        {
            if (initialized) return owner == host;
            if (owner == null || now == null) return false;
            host = owner;
            regional = owner.GetComponent<TruckTaxiRegionalWorld>();
            gameNow = now;
            lastGameNow = now();
            lastScheduledDay = lastGameNow.Date.AddDays(-1);
            daySchedule = new TruckTaxiExtremeDaySchedule(scheduleSettings);
            setWeather = weatherCallback;
            if (authoredZones != null)
                foreach (var zone in authoredZones) RegisterZone(zone);
            initialized = true;
            return true;
        }

        public void SetWeatherObservation(bool storm, bool snow, float rainHours)
        {
            float boundedRainHours = float.IsFinite(rainHours) ? Mathf.Max(0, rainHours) : 0;
            bool eligibilityChanged = severeStorm != storm || heavySnow != snow ||
                (continuousHeavyRainHours >= 2) != (boundedRainHours >= 2);
            severeStorm = storm;
            heavySnow = snow;
            continuousHeavyRainHours = boundedRainHours;
            if (eligibilityChanged) nextNaturalTick = 0;
        }

        public bool RegisterZone(TruckTaxiHazardZone zone)
        {
            if (zone == null || !zone.IsValid(out _)) return false;
            if (zones.TryGetValue(zone.zoneId, out var existing) && existing != zone) return false;
            zones[zone.zoneId] = zone;
            return true;
        }

        public bool StartEvent(TruckTaxiExtremeKind kind, string zoneId)
        {
            if (!initialized) return false;
            if (!TryZone(kind, zoneId, out var zone)) return StartEventHere(kind);
            return StartFootprint(TruckTaxiExtremeFootprint.FromZone(zone), false);
        }

        public bool StartEventHere(TruckTaxiExtremeKind kind)
        {
            if (!initialized) return false;
            if (!TryPlayer(out var player))
            {
                lastRejection[(int)kind] = "PLAYER_UNAVAILABLE";
                RecordAttempt(kind, gameNow(), true, "PLAYER_UNAVAILABLE", "none", null, null, 0, false);
                return false;
            }
            TruckTaxiExtremeFootprint footprint = null;
            float closest = float.PositiveInfinity;
            foreach (var pair in zones)
            {
                var zone = pair.Value;
                if (zone == null || zone.kind != kind || !zone.IsValid(out _)) continue;
                float distance = Vector3.Distance(player, zone.PositionAt(0));
                float authoredRange = kind == TruckTaxiExtremeKind.Tornado ? 1000 :
                    kind == TruckTaxiExtremeKind.Hurricane ? 1500 :
                    kind == TruckTaxiExtremeKind.Earthquake ? 350 : 600;
                if (distance >= closest || distance > authoredRange ||
                    (regional != null && !regional.IsPositionAvailable(zone.PositionAt(0)))) continue;
                closest = distance;
                footprint = TruckTaxiExtremeFootprint.FromZone(zone);
            }
            if (footprint == null)
            {
                string regionId = regional != null ? regional.ResolveRegion(player) : "taxi.current";
                Vector3 forward = host.Player != null ? host.Player.transform.forward : Vector3.forward;
                string id = "dynamic:" + kind + ":" + ++proceduralSequence;
                if (!TruckTaxiExtremeFootprint.TryCreate(kind, id, regionId, player, forward,
                    footprintSettings, p => regional == null || regional.IsPositionAvailable(p), GroundAt,
                    out footprint, out string reason))
                {
                    lastRejection[(int)kind] = reason;
                    RecordAttempt(kind, gameNow(), true, reason, "procedural", id, regionId, 0, false);
                    return false;
                }
            }
            return StartFootprint(footprint, true);
        }

        private bool StartFootprint(TruckTaxiExtremeFootprint footprint, bool natural)
        {
            if (footprint == null) return false;
            for (int i = 0; i < active.Count; i++)
                if (active[i].Footprint.Id == footprint.Id ||
                    (natural && active[i].Footprint.Kind == footprint.Kind)) return false;
            DateTime now = gameNow();
            var moving = now + TruckTaxiExtremeRules.Duration(footprint.Kind);
            var props = footprint.ShakeProps;
            var item = new ActiveEvent { Footprint = footprint, Started = now, MovingEnds = moving,
                Ends = moving + TruckTaxiExtremeRules.Cleanup(footprint.Kind), Position = footprint.PositionAt(0),
                PreviousPropOffsets = new Vector3[props != null ? props.Length : 0], RealStarted = Time.unscaledTime };
            active.Add(item);
            if (footprint.Kind == TruckTaxiExtremeKind.Hurricane)
            {
                ActiveSevereProfile = new TruckTaxiExtremeWeatherProfile("thunderstorm", 28, 160, 1);
                setWeather?.Invoke(ActiveSevereProfile, TruckTaxiExtremeRules.Duration(footprint.Kind));
            }
            RaiseWarning(footprint, now, item.Ends);
            Vector3 player = playerBody != null ? playerBody.position : footprint.Path[0];
            RecordAttempt(footprint.Kind, now, true, null,
                footprint.IsProcedural ? "procedural" : "authored", footprint.Id, footprint.RegionId,
                Vector3.Distance(player, footprint.Path[0]), true);
            Rebuild(now);
            HazardChanged?.Invoke();
            return true;
        }

        // Calendar/forecast owner schedules known storms. Quakes only alert at onset.
        public bool Schedule(TruckTaxiExtremeKind kind, string zoneId, DateTime startsAt)
        {
            if (!initialized || !TryZone(kind, zoneId, out var zone) || startsAt <= gameNow()) return false;
            if (kind == TruckTaxiExtremeKind.Earthquake) return false;
            for (int i = 0; i < scheduled.Count; i++)
                if (scheduled[i].ZoneId == zoneId) return false;
            scheduled.Add(new ScheduledEvent(kind, zoneId, startsAt));
            DateTime now = gameNow();
            TimeSpan lead = kind == TruckTaxiExtremeKind.Hurricane ? TimeSpan.FromHours(3) :
                kind == TruckTaxiExtremeKind.Tornado ? TimeSpan.FromMinutes(10) : TimeSpan.FromHours(1);
            if (startsAt - now <= lead) RaiseWarning(TruckTaxiExtremeFootprint.FromZone(zone), now,
                startsAt + TruckTaxiExtremeRules.Duration(kind));
            return true;
        }

        public bool TryStartNatural(TruckTaxiExtremeKind kind, string zoneId, bool severeStorm,
            bool heavySnow, float continuousHeavyRainHours, float dailyRoll)
        {
            if (!initialized || !TryZone(kind, zoneId, out _)) return false;
            if (!TruckTaxiExtremeRules.NaturalEligible(kind, severeStorm, heavySnow, continuousHeavyRainHours))
            { RecordAttempt(kind, gameNow(), false, "WRONG_WEATHER", "authored", zoneId, null, 0, false); return false; }
            if (!float.IsFinite(dailyRoll)) return false;
            DateTime today = gameNow().Date;
            if (lastNaturalAttempt.TryGetValue(zoneId, out var attempted) && attempted == today)
            { RecordAttempt(kind, gameNow(), true, "COOLDOWN", "authored", zoneId, null, 0, false); return false; }
            lastNaturalAttempt[zoneId] = today;
            if (Mathf.Clamp01(dailyRoll) >= TruckTaxiExtremeRules.DailyChance(kind))
            { RecordAttempt(kind, gameNow(), true, "ROLL_FAILED", "authored", zoneId, null, 0, false); return false; }
            bool started = StartEvent(kind, zoneId);
            if (!started) RecordAttempt(kind, gameNow(), true, "CONFLICT", "authored", zoneId, null, 0, false);
            else
            {
                daySchedule.RecordStart(kind, today);
                LastNaturalEvent = today;
                LastNaturalKind = kind;
            }
            return started;
        }

        public void Clear() => ClearMatching(null, null);
        public void Clear(TruckTaxiExtremeKind kind, string zoneId) => ClearMatching(kind, zoneId);

        private bool TryZone(TruckTaxiExtremeKind kind, string id, out TruckTaxiHazardZone zone)
        {
            if (!zones.TryGetValue(id ?? string.Empty, out zone) || zone == null || zone.kind != kind ||
                !zone.IsValid(out _)) return false;
            if (regional == null || regional.regions == null) return true;
            foreach (var region in regional.regions)
                if (region != null && region.id == zone.regionId) return true;
            return false;
        }

        private void Update() => Tick();

        public void Tick()
        {
            if (!initialized) return;
            DateTime now = gameNow();
            if (now < lastGameNow)
            {
                ClearMatching(null, null);
                lastNaturalAttempt.Clear();
                daySchedule.Reset();
                lastScheduledDay = now.Date.AddDays(-1);
                LastNaturalEvent = default;
                LastNaturalKind = null;
            }
            lastGameNow = now;
            for (int i = scheduled.Count - 1; i >= 0; i--)
            {
                var item = scheduled[i];
                if (now < item.StartsAt && !HasWarning(item.ZoneId))
                {
                    TimeSpan lead = item.Kind == TruckTaxiExtremeKind.Hurricane ? TimeSpan.FromHours(3) :
                        item.Kind == TruckTaxiExtremeKind.Tornado ? TimeSpan.FromMinutes(10) : TimeSpan.FromHours(1);
                    if (item.StartsAt - now <= lead && TryZone(item.Kind, item.ZoneId, out var zone))
                        RaiseWarning(TruckTaxiExtremeFootprint.FromZone(zone), now,
                            item.StartsAt + TruckTaxiExtremeRules.Duration(item.Kind));
                }
                if (now < item.StartsAt) continue;
                scheduled.RemoveAt(i);
                StartEvent(item.Kind, item.ZoneId);
            }
            bool eventExpired = false;
            for (int i = active.Count - 1; i >= 0; i--)
                if (now >= active[i].Ends && !IsMoving(active[i], now))
                {
                    RemoveAt(i, "EXPIRED");
                    eventExpired = true;
                }
            for (int i = warnings.Count - 1; i >= 0; i--)
                if (now >= warnings[i].ExpiresAt) warnings.RemoveAt(i);
            if (host != null && !host.Paused && Time.unscaledTime >= nextNaturalTick)
            {
                nextNaturalTick = Time.unscaledTime + 5;
                ProcessNaturalDays(now);
                foreach (var pair in zones)
                    if (pair.Value != null && pair.Value.kind == TruckTaxiExtremeKind.Hurricane)
                        TryStartNatural(pair.Value.kind, pair.Value.zoneId, severeStorm, heavySnow,
                            continuousHeavyRainHours, (float)naturalRandom.NextDouble());
            }
            if (Time.unscaledTime < nextVisualTick)
            {
                if (eventExpired) Rebuild(now);
                return;
            }
            nextVisualTick = Time.unscaledTime + .1f;
            Rebuild(now);
        }

        private void ProcessNaturalDays(DateTime now)
        {
            DateTime first = lastScheduledDay.AddDays(1);
            if (first > now.Date) first = now.Date;
            if (first < now.Date.AddDays(-31)) first = now.Date.AddDays(-31);
            for (DateTime day = first; day <= now.Date; day = day.AddDays(1))
            {
                foreach (TruckTaxiExtremeKind kind in naturallyScheduledKinds)
                {
                    bool eligible = TruckTaxiExtremeRules.NaturalEligible(kind, severeStorm,
                        heavySnow, continuousHeavyRainHours);
                    if (!daySchedule.TryDay(kind, day, eligible, (float)naturalRandom.NextDouble(), out string reason))
                    {
                        bool changed = lastRejection[(int)kind] != reason;
                        if (reason != "ALREADY_ATTEMPTED") lastRejection[(int)kind] = reason;
                        if (reason != "ALREADY_ATTEMPTED" &&
                            (reason != "WRONG_WEATHER" || changed))
                            RecordAttempt(kind, day, eligible, reason, "none", null,
                                regional != null ? regional.CurrentRegion : null, 0, false);
                        continue;
                    }
                    if (HasActiveKind(kind))
                    {
                        lastRejection[(int)kind] = "CONFLICT";
                        RecordAttempt(kind, day, true, "CONFLICT", "none", null,
                            regional != null ? regional.CurrentRegion : null, 0, false);
                        continue;
                    }
                    if (StartEventHere(kind))
                    {
                        daySchedule.RecordStart(kind, day);
                        LastNaturalEvent = day;
                        LastNaturalKind = kind;
                        lastRejection[(int)kind] = null;
                    }
                    else if (string.IsNullOrEmpty(lastRejection[(int)kind]))
                        lastRejection[(int)kind] = "NO_PROCEDURAL_CANDIDATE";
                }
            }
            lastScheduledDay = now.Date;
        }

        private bool HasActiveKind(TruckTaxiExtremeKind kind)
        {
            foreach (var item in active) if (item.Footprint.Kind == kind) return true;
            return false;
        }

        private bool HasWarning(string zoneId)
        {
            for (int i = 0; i < warnings.Count; i++)
                if (warnings[i].ZoneId == zoneId) return true;
            return false;
        }

        private void LateUpdate()
        {
            RestoreCamera();
            for (int i = 0; i < active.Count; i++) RestoreProps(active[i]);
            if (CameraShakeIntensity01 <= 0 || host == null || host.Paused) return;
            float phase = Time.unscaledTime * 31;
            if (shakeCamera != null)
            {
                cameraPositionBeforeShake = shakeCamera.localPosition;
                previousShakeOffset = new Vector3(Mathf.Sin(phase), Mathf.Sin(phase * 1.37f), 0) *
                    (.12f * CameraShakeIntensity01);
                shakeCamera.localPosition += previousShakeOffset;
            }
            foreach (var item in active)
            {
                if (item.Footprint.Kind != TruckTaxiExtremeKind.Earthquake || !item.PhysicsRelevant ||
                    !IsMoving(item, gameNow())) continue;
                var props = item.Footprint.ShakeProps;
                if (props == null) continue;
                for (int i = 0; i < props.Length; i++)
                {
                    if (props[i] == null) continue;
                    Vector3 offset = new Vector3(Mathf.Sin(phase + i), 0, Mathf.Cos(phase * 1.2f + i)) * .08f;
                    props[i].localPosition += offset;
                    item.PreviousPropOffsets[i] = offset;
                }
            }
        }

        private static void RestoreProps(ActiveEvent item)
        {
            var props = item.Footprint.ShakeProps;
            if (props == null) return;
            for (int i = 0; i < props.Length && i < item.PreviousPropOffsets.Length; i++)
            {
                if (props[i] != null) props[i].localPosition -= item.PreviousPropOffsets[i];
                item.PreviousPropOffsets[i] = Vector3.zero;
            }
        }

        private void RestoreCamera()
        {
            if (shakeCamera != null && previousShakeOffset != Vector3.zero &&
                (shakeCamera.localPosition - cameraPositionBeforeShake - previousShakeOffset).sqrMagnitude < .000001f)
                shakeCamera.localPosition = cameraPositionBeforeShake;
            previousShakeOffset = Vector3.zero;
        }

        private void FixedUpdate()
        {
            LivePhysicsActorCount = 0;
            bool playerAffected = false;
            for (int i = 0; i < active.Count; i++)
                if (active[i].RoadDebris != null) LivePhysicsActorCount += 2;
            if (!initialized || host == null || host.Paused || playerBody == null) return;
            DateTime now = gameNow();
            for (int i = 0; i < active.Count; i++)
            {
                var item = active[i];
                if (!item.PhysicsRelevant || !IsMoving(item, now)) continue;
                Vector3 delta = playerBody.position - item.Position;
                delta.y = 0;
                float distance = delta.magnitude;
                float radius = item.Footprint.RadiusMeters;
                if (distance > radius) continue;
                float exposure = 1 - distance / radius;
                Vector3 direction;
                switch (item.Footprint.Kind)
                {
                    case TruckTaxiExtremeKind.Tornado:
                        direction = distance > .1f ? -delta.normalized + Vector3.Cross(Vector3.up, delta.normalized) * .4f : Vector3.right;
                        playerBody.AddForce(direction.normalized * TruckTaxiExtremeRules.CappedForce(
                            maximumPlayerForceNewtons * exposure, maximumPlayerForceNewtons), ForceMode.Force);
                        break;
                    case TruckTaxiExtremeKind.Hurricane:
                        playerBody.AddForce(item.Footprint.WindDirection * TruckTaxiExtremeRules.CappedForce(
                            maximumPlayerForceNewtons * .55f * exposure, maximumPlayerForceNewtons), ForceMode.Force);
                        break;
                    case TruckTaxiExtremeKind.Earthquake:
                        float acceleration = Mathf.Clamp(maximumQuakeAcceleration * exposure, 0, 2);
                        playerBody.AddForce(Vector3.up * acceleration * Mathf.Max(0, Mathf.Sin(Time.fixedTime * 17)), ForceMode.Acceleration);
                        break;
                    default:
                        direction = item.Footprint.Path[item.Footprint.Path.Length - 1] - item.Footprint.Path[0];
                        direction.y = 0;
                        if (direction.sqrMagnitude > .01f)
                            playerBody.AddForce(direction.normalized * TruckTaxiExtremeRules.CappedForce(
                                maximumPlayerForceNewtons * .4f * exposure, maximumPlayerForceNewtons), ForceMode.Force);
                        break;
                }
                playerAffected = true; // NWH retains vehicle authority.
            }
            if (playerAffected) LivePhysicsActorCount++;
        }

        private void Rebuild(DateTime now)
        {
            if (playerBody == null && host != null && host.Player != null)
                playerBody = host.Player.GetComponent<Rigidbody>();
            Vector3 player = playerBody != null ? playerBody.position : Vector3.positiveInfinity;
            snapshots.Clear();
            LiveVisualActorCount = 0;
            CameraShakeIntensity01 = 0;
            int debrisPiles = 0;
            for (int i = 0; i < active.Count; i++)
            {
                var item = active[i];
                bool cleanup = !IsMoving(item, now);
                double duration = (item.MovingEnds - item.Started).TotalSeconds;
                float progress = duration > 0 ? Mathf.Clamp01((float)((now - item.Started).TotalSeconds / duration)) : 1;
                item.Position = item.Footprint.PositionAt(cleanup ? 1 : progress);
                bool roadHazard = item.Footprint.Kind == TruckTaxiExtremeKind.Avalanche ||
                    item.Footprint.Kind == TruckTaxiExtremeKind.Mudslide;
                Vector3 relevantPosition = roadHazard && cleanup ? RoadCrossing(item.Footprint) : item.Position;
                bool loaded = regional == null || regional.IsPositionAvailable(relevantPosition);
                float visibleRange = item.Footprint.Kind == TruckTaxiExtremeKind.Tornado ?
                    Mathf.Max(1000, visualDistanceMeters) : roadHazard ? Mathf.Max(600, visualDistanceMeters) :
                    visualDistanceMeters;
                item.Nearby = loaded && (player - relevantPosition).sqrMagnitude <= visibleRange * visibleRange;
                item.PhysicsRelevant = loaded &&
                    (player - item.Position).sqrMagnitude <= item.Footprint.RadiusMeters * item.Footprint.RadiusMeters;
                bool show = item.Nearby && LiveVisualActorCount < maximumVisualActors;
                if (show)
                {
                    if (item.Visual == null) item.Visual = CreateVisual(item);
                    if (item.Visual != null)
                    {
                        item.Visual.transform.position = item.Position;
                        if (item.Footprint.Kind == TruckTaxiExtremeKind.Tornado)
                            item.Visual.transform.Rotate(Vector3.up, 35 * .1f, Space.World);
                        MarkAttempt(item, true, null);
                        LiveVisualActorCount++;
                    }
                }
                else DestroyVisual(item);
                if (roadHazard && cleanup && item.Nearby && debrisPiles < maximumDebrisPiles)
                {
                    if (item.RoadDebris == null) item.RoadDebris = CreateRoadDebris(item);
                    debrisPiles++;
                }
                else DestroyRoadDebris(item);
                var snapshot = new TruckTaxiExtremeSnapshot(item.Footprint.Kind, item.Footprint.Id,
                    item.Footprint.RegionId, cleanup ? TruckTaxiExtremeStage.Cleanup : TruckTaxiExtremeStage.Moving,
                    item.Ends, relevantPosition,
                    item.Footprint.RadiusMeters, item.Nearby, roadHazard);
                snapshots.Add(snapshot);
                if (item.PhysicsRelevant && !cleanup)
                {
                    LocalReactionRequested?.Invoke(snapshot);
                    if (item.Footprint.Kind == TruckTaxiExtremeKind.Earthquake &&
                        Time.unscaledTime - item.RealStarted <= 20)
                        CameraShakeIntensity01 = Mathf.Max(CameraShakeIntensity01, .85f);
                }
            }
        }

        private GameObject CreateVisual(ActiveEvent item)
        {
            var footprint = item.Footprint;
            GameObject visual = footprint.VisualPrefab != null
                ? Instantiate(footprint.VisualPrefab, item.Position, Quaternion.identity, transform)
                : CreateFallbackParticles(footprint.Kind, item.Position);
            if (footprint.VisualPrefab == null &&
                (footprint.Kind == TruckTaxiExtremeKind.Avalanche || footprint.Kind == TruckTaxiExtremeKind.Mudslide))
            {
                Vector3 flow = footprint.Path[footprint.Path.Length - 1] - footprint.Path[0];
                visual.transform.rotation = Quaternion.LookRotation(new Vector3(flow.x, 0, flow.z));
            }
            if (footprint.VisualPrefab == null && footprint.Kind == TruckTaxiExtremeKind.Hurricane)
                visual.transform.rotation = Quaternion.FromToRotation(Vector3.up, footprint.WindDirection);
            AudioClip clip = footprint.StartClip != null ? footprint.StartClip :
                footprint.Kind == TruckTaxiExtremeKind.Tornado ? tornadoClip :
                footprint.Kind == TruckTaxiExtremeKind.Earthquake ? earthquakeClip :
                footprint.Kind == TruckTaxiExtremeKind.Avalanche || footprint.Kind == TruckTaxiExtremeKind.Mudslide
                    ? slideClip : null;
            if (clip != null)
            {
                var source = visual.AddComponent<AudioSource>();
                source.spatialBlend = 1;
                source.playOnAwake = false;
                source.rolloffMode = AudioRolloffMode.Linear;
                source.maxDistance = footprint.Kind == TruckTaxiExtremeKind.Tornado ? 1100 : 600;
                source.clip = clip;
                source.Play();
            }
            return visual;
        }

        private GameObject CreateFallbackParticles(TruckTaxiExtremeKind kind, Vector3 position)
        {
            var visual = new GameObject("Taxi " + kind + " temporary VFX");
            visual.transform.SetParent(transform, false);
            visual.transform.position = position;
            var particles = visual.AddComponent<ParticleSystem>();
            Material material = GetFallbackParticleMaterial();
            if (material != null) visual.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = kind == TruckTaxiExtremeKind.Tornado ? 256 :
                kind == TruckTaxiExtremeKind.Hurricane ? 48 : 160;
            main.startLifetime = kind == TruckTaxiExtremeKind.Tornado ? 3 : 1.8f;
            main.startSpeed = kind == TruckTaxiExtremeKind.Tornado ? 8 :
                kind == TruckTaxiExtremeKind.Hurricane ? 11 : 2.5f;
            main.startSize = kind == TruckTaxiExtremeKind.Tornado ? 4 :
                kind == TruckTaxiExtremeKind.Hurricane ? .7f :
                kind == TruckTaxiExtremeKind.Earthquake ? 3 : 2.5f;
            main.startColor = kind == TruckTaxiExtremeKind.Hurricane
                ? new Color(.65f, .74f, .78f, .55f) : kind == TruckTaxiExtremeKind.Avalanche
                ? new Color(.85f, .9f, .94f, .7f) : kind == TruckTaxiExtremeKind.Mudslide
                    ? new Color(.34f, .27f, .2f, .8f) : new Color(.56f, .55f, .5f, .65f);
            var emission = particles.emission;
            emission.rateOverTime = kind == TruckTaxiExtremeKind.Tornado ? 90 :
                kind == TruckTaxiExtremeKind.Hurricane ? 18 : 55;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.radius = kind == TruckTaxiExtremeKind.Tornado ? 12 :
                kind == TruckTaxiExtremeKind.Hurricane ? 5 :
                kind == TruckTaxiExtremeKind.Earthquake ? 35 : 15;
            shape.angle = kind == TruckTaxiExtremeKind.Tornado ? 18 : 10;
            shape.rotation = new Vector3(-90, 0, 0);
            particles.Play();
            if (kind == TruckTaxiExtremeKind.Tornado)
            {
                AddFunnel(visual.transform);
                var debris = new GameObject("orbiting visible debris");
                debris.transform.SetParent(visual.transform, false);
                var flecks = debris.AddComponent<ParticleSystem>();
                if (material != null) debris.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
                flecks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var fleckMain = flecks.main;
                fleckMain.loop = true;
                fleckMain.simulationSpace = ParticleSystemSimulationSpace.Local;
                fleckMain.maxParticles = 24;
                fleckMain.startLifetime = 2;
                fleckMain.startSpeed = 3;
                fleckMain.startSize = .5f;
                fleckMain.startColor = new Color(.23f, .21f, .18f, .9f);
                var fleckEmission = flecks.emission;
                fleckEmission.rateOverTime = 10;
                var fleckShape = flecks.shape;
                fleckShape.shapeType = ParticleSystemShapeType.Sphere;
                fleckShape.radius = 5;
                flecks.Play();
            }
            else if (kind == TruckTaxiExtremeKind.Avalanche || kind == TruckTaxiExtremeKind.Mudslide)
                AddSlideMass(visual.transform, kind);
            return visual;
        }

        private static void AddFunnel(Transform parent)
        {
            for (int i = 0; i < 9; i++)
            {
                float t = i / 8f;
                var section = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                section.name = "Visible funnel section";
                section.transform.SetParent(parent, false);
                section.transform.localPosition = new Vector3(Mathf.Sin(t * 5) * 2, 4 + t * 56, 0);
                float width = Mathf.Lerp(4, 42, t * t);
                section.transform.localScale = new Vector3(width, 4.1f, width);
                var collider = section.GetComponent<Collider>();
                collider.enabled = false;
                Destroy(collider);
                var block = new MaterialPropertyBlock();
                Color color = Color.Lerp(new Color(.34f, .36f, .38f), new Color(.48f, .5f, .52f), t);
                block.SetColor("_BaseColor", color);
                block.SetColor("_Color", color);
                section.GetComponent<Renderer>().SetPropertyBlock(block);
            }
        }

        private static void AddSlideMass(Transform parent, TruckTaxiExtremeKind kind)
        {
            var block = new MaterialPropertyBlock();
            Color color = kind == TruckTaxiExtremeKind.Avalanche
                ? new Color(.82f, .87f, .9f) : new Color(.32f, .23f, .16f);
            block.SetColor("_BaseColor", color);
            block.SetColor("_Color", color);
            for (int i = 0; i < 7; i++)
            {
                var chunk = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                chunk.name = "Moving slide mass";
                chunk.transform.SetParent(parent, false);
                chunk.transform.localPosition = new Vector3((i - 3) * 5, 2.5f + i % 2, -(i % 3) * 5);
                chunk.transform.localScale = new Vector3(9, 5 + i % 2 * 2, 12);
                var collider = chunk.GetComponent<Collider>();
                collider.enabled = false;
                Destroy(collider);
                chunk.GetComponent<Renderer>().SetPropertyBlock(block);
            }
        }

        private Material GetFallbackParticleMaterial()
        {
            if (fallbackMaterialResolved) return fallbackParticleMaterial;
            fallbackMaterialResolved = true;
            var preset = Resources.Load<GroundDetectionPreset>(
                "NWH Vehicle Physics 2/Defaults/DefaultGroundDetectionPreset");
            var renderer = preset != null && preset.particlePrefab != null
                ? preset.particlePrefab.GetComponent<ParticleSystemRenderer>() : null;
            fallbackParticleMaterial = renderer != null ? renderer.sharedMaterial : null;
            if (fallbackParticleMaterial == null)
                Debug.LogWarning("Taxi extreme fallback particles have no included NWH URP material; Unity default particle material will be used.", this);
            return fallbackParticleMaterial;
        }

        private static Vector3 RoadCrossing(TruckTaxiExtremeFootprint footprint) =>
            footprint.Path[footprint.Path.Length / 2];

        private static bool IsMoving(ActiveEvent item, DateTime now) =>
            now < item.MovingEnds ||
            (item.Footprint.Kind == TruckTaxiExtremeKind.Earthquake &&
             Time.unscaledTime - item.RealStarted < 20);

        private GameObject CreateRoadDebris(ActiveEvent item)
        {
            // Two visible shoulder rocks leave the center navigable; no unrepresented road closure.
            var root = new GameObject("Taxi visible partial road debris");
            root.transform.SetParent(transform, false);
            root.transform.position = RoadCrossing(item.Footprint);
            Color color = item.Footprint.Kind == TruckTaxiExtremeKind.Avalanche
                ? new Color(.78f, .83f, .85f) : new Color(.38f, .3f, .23f);
            var block = new MaterialPropertyBlock();
            block.SetColor("_Color", color);
            block.SetColor("_BaseColor", color);
            for (int i = 0; i < 2; i++)
            {
                var rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rock.name = "Visible debris " + i;
                rock.transform.SetParent(root.transform, false);
                rock.transform.localPosition = new Vector3(i == 0 ? -3.4f : 3.4f, .6f, i == 0 ? -1 : 1);
                rock.transform.localScale = new Vector3(1.5f, 1.2f, 1.8f);
                rock.transform.localRotation = Quaternion.Euler(0, i == 0 ? 15 : -11, 0);
                rock.GetComponent<Renderer>().SetPropertyBlock(block);
            }
            return root;
        }

        private void RaiseWarning(TruckTaxiExtremeFootprint zone, DateTime now, DateTime expires)
        {
            string place = string.IsNullOrWhiteSpace(zone.DisplayName) ? zone.RegionId : zone.DisplayName;
            string text = zone.Kind == TruckTaxiExtremeKind.Earthquake ? "EARTHQUAKE - " + place + " - TAKE CARE" :
                zone.Kind == TruckTaxiExtremeKind.Hurricane ? "HURRICANE WARNING - " + place + " - HEAVY WIND AND RAIN" :
                zone.Kind == TruckTaxiExtremeKind.Tornado ? "TORNADO WARNING - " + place + " - DRIVE CAREFULLY" :
                zone.Kind == TruckTaxiExtremeKind.Avalanche ? "AVALANCHE HAZARD - " + place + " - ROUTE MAY BE UNSAFE" :
                "MUDSLIDE HAZARD - " + place + " - ROUTE MAY BE UNSAFE";
            var warning = new TruckTaxiExtremeWarning(zone.Kind, zone.Id, text, now, expires);
            warnings.Add(warning);
            WarningRaised?.Invoke(warning);
        }

        private void ClearMatching(TruckTaxiExtremeKind? kind, string zoneId)
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                if (kind.HasValue && active[i].Footprint.Kind != kind.Value) continue;
                if (zoneId != null && active[i].Footprint.Id != zoneId) continue;
                RemoveAt(i, "CLEARED");
            }
            for (int i = scheduled.Count - 1; i >= 0; i--)
                if ((!kind.HasValue || scheduled[i].Kind == kind.Value) &&
                    (zoneId == null || scheduled[i].ZoneId == zoneId)) scheduled.RemoveAt(i);
            for (int i = warnings.Count - 1; i >= 0; i--)
                if ((!kind.HasValue || warnings[i].Kind == kind.Value) &&
                    (zoneId == null || warnings[i].ZoneId == zoneId)) warnings.RemoveAt(i);
            snapshots.Clear();
            LivePhysicsActorCount = 0;
            LiveVisualActorCount = 0;
            if (initialized) Rebuild(gameNow());
            HazardChanged?.Invoke();
        }

        private bool HasHurricane()
        {
            foreach (var item in active)
                if (item.Footprint.Kind == TruckTaxiExtremeKind.Hurricane) return true;
            return false;
        }

        private void RemoveAt(int index, string endReason)
        {
            bool hurricane = active[index].Footprint.Kind == TruckTaxiExtremeKind.Hurricane;
            MarkAttempt(active[index], active[index].Visual != null, endReason);
            RestoreProps(active[index]);
            DestroyVisual(active[index]);
            DestroyRoadDebris(active[index]);
            active.RemoveAt(index);
            if (hurricane && !HasHurricane())
            {
                ActiveSevereProfile = null;
                setWeather?.Invoke(null, TimeSpan.Zero);
            }
            HazardChanged?.Invoke();
        }

        private static void DestroyVisual(ActiveEvent item)
        {
            if (item.Visual == null) return;
            Destroy(item.Visual);
            item.Visual = null;
        }

        private static void DestroyRoadDebris(ActiveEvent item)
        {
            if (item.RoadDebris == null) return;
            Destroy(item.RoadDebris);
            item.RoadDebris = null;
        }

        private void OnDisable()
        {
            RestoreCamera();
            ClearMatching(null, null);
        }

        private void OnDestroy()
        {
            RestoreCamera();
            ClearMatching(null, null);
            initialized = false;
        }

        private bool TryPlayer(out Vector3 position)
        {
            if (playerBody == null && host != null && host.Player != null)
                playerBody = host.Player.GetComponent<Rigidbody>();
            if (playerBody != null) { position = playerBody.position; return true; }
            if (host != null && host.Player != null)
            { position = host.Player.transform.position; return true; }
            position = default;
            return false;
        }

        private static Vector3? GroundAt(Vector3 position)
        {
            if (Physics.Raycast(position + Vector3.up * 300, Vector3.down, out var hit, 650,
                ~0, QueryTriggerInteraction.Ignore)) return hit.point;
            return position;
        }

        private void RecordAttempt(TruckTaxiExtremeKind kind, DateTime time, bool eligible,
            string reason, string source, string zoneId, string regionId, float distance, bool started)
        {
            if (attempts.Count == 64) attempts.RemoveAt(0);
            attempts.Add(new TruckTaxiExtremeAttempt(kind, time, eligible, reason, source, zoneId,
                regionId, distance, started, false, started, null));
        }

        private void MarkAttempt(ActiveEvent item, bool visual, string endReason)
        {
            for (int i = attempts.Count - 1; i >= 0; i--)
            {
                var attempt = attempts[i];
                if (!attempt.Started || attempt.Kind != item.Footprint.Kind ||
                    attempt.ZoneId != item.Footprint.Id || attempt.Time != item.Started) continue;
                if (attempt.VisualMaterialized == visual && endReason == null) return;
                attempts[i] = new TruckTaxiExtremeAttempt(attempt.Kind, attempt.Time,
                    attempt.Eligible, attempt.Reason, attempt.Source, attempt.ZoneId,
                    attempt.RegionId, attempt.PlayerDistanceMeters, true,
                    attempt.VisualMaterialized || visual, attempt.AlertSent,
                    endReason ?? attempt.EndReason);
                return;
            }
        }
    }
}
