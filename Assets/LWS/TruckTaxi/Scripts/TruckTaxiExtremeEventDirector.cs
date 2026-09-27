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
        [SerializeField] private TruckTaxiHazardZone[] authoredZones = Array.Empty<TruckTaxiHazardZone>();
        [SerializeField, Min(50)] private float visualDistanceMeters = 350;
        [SerializeField, Min(0)] private float maximumPlayerForceNewtons = 3500;
        [SerializeField, Min(0)] private float maximumQuakeAcceleration = 1.5f;
        [SerializeField, Range(0, 8)] private int maximumVisualActors = 3;
        [SerializeField, Range(0, 4)] private int maximumDebrisPiles = 2;

        private sealed class ActiveEvent
        {
            public TruckTaxiHazardZone Zone;
            public DateTime Started;
            public DateTime MovingEnds;
            public DateTime Ends;
            public GameObject Visual;
            public GameObject RoadDebris;
            public bool Nearby;
            public bool PhysicsRelevant;
            public Vector3 Position;
            public Vector3[] PreviousPropOffsets;
        }

        private readonly Dictionary<string, TruckTaxiHazardZone> zones = new Dictionary<string, TruckTaxiHazardZone>();
        private readonly List<ActiveEvent> active = new List<ActiveEvent>();
        private readonly List<TruckTaxiExtremeSnapshot> snapshots = new List<TruckTaxiExtremeSnapshot>();
        private readonly List<TruckTaxiExtremeWarning> warnings = new List<TruckTaxiExtremeWarning>();
        private readonly Dictionary<string, DateTime> lastNaturalAttempt = new Dictionary<string, DateTime>();
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
        public int LivePhysicsActorCount { get; private set; }
        public int LiveVisualActorCount { get; private set; }
        public TruckTaxiExtremeWeatherProfile? ActiveSevereProfile { get; private set; }
        public bool RoutingClosureSupported => false;
        public event Action<TruckTaxiExtremeWarning> WarningRaised;
        public event Action HazardChanged;
        public event Action<TruckTaxiExtremeSnapshot> LocalReactionRequested;
        public float CameraShakeIntensity01 { get; private set; }

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
            if (!initialized || !TryZone(kind, zoneId, out var zone)) return false;
            for (int i = 0; i < active.Count; i++)
                if (active[i].Zone == zone) return false;
            DateTime now = gameNow();
            var moving = now + TruckTaxiExtremeRules.Duration(kind);
            var item = new ActiveEvent { Zone = zone, Started = now, MovingEnds = moving,
                Ends = moving + TruckTaxiExtremeRules.Cleanup(kind), Position = zone.PositionAt(0),
                PreviousPropOffsets = new Vector3[zone.shakeProps != null ? zone.shakeProps.Length : 0] };
            active.Add(item);
            if (kind == TruckTaxiExtremeKind.Hurricane)
            {
                ActiveSevereProfile = new TruckTaxiExtremeWeatherProfile("thunderstorm", 28, 160, 1);
                setWeather?.Invoke(ActiveSevereProfile, TruckTaxiExtremeRules.Duration(kind));
            }
            RaiseWarning(zone, now, item.Ends);
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
            if (startsAt - now <= lead) RaiseWarning(zone, now, startsAt + TruckTaxiExtremeRules.Duration(kind));
            return true;
        }

        public bool TryStartNatural(TruckTaxiExtremeKind kind, string zoneId, bool severeStorm,
            bool heavySnow, float continuousHeavyRainHours, float dailyRoll)
        {
            if (!initialized || !TryZone(kind, zoneId, out _) ||
                !TruckTaxiExtremeRules.NaturalEligible(kind, severeStorm, heavySnow, continuousHeavyRainHours) ||
                !float.IsFinite(dailyRoll)) return false;
            DateTime today = gameNow().Date;
            if (lastNaturalAttempt.TryGetValue(zoneId, out var attempted) && attempted == today) return false;
            lastNaturalAttempt[zoneId] = today;
            return Mathf.Clamp01(dailyRoll) < TruckTaxiExtremeRules.DailyChance(kind) && StartEvent(kind, zoneId);
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
                        RaiseWarning(zone, now, item.StartsAt + TruckTaxiExtremeRules.Duration(item.Kind));
                }
                if (now < item.StartsAt) continue;
                scheduled.RemoveAt(i);
                StartEvent(item.Kind, item.ZoneId);
            }
            bool eventExpired = false;
            for (int i = active.Count - 1; i >= 0; i--)
                if (now >= active[i].Ends)
                {
                    RemoveAt(i);
                    eventExpired = true;
                }
            for (int i = warnings.Count - 1; i >= 0; i--)
                if (now >= warnings[i].ExpiresAt) warnings.RemoveAt(i);
            if (host != null && !host.Paused && Time.unscaledTime >= nextNaturalTick)
            {
                nextNaturalTick = Time.unscaledTime + 5;
                foreach (var pair in zones)
                {
                    var zone = pair.Value;
                    if (zone != null)
                        TryStartNatural(zone.kind, zone.zoneId, severeStorm, heavySnow,
                            continuousHeavyRainHours, (float)naturalRandom.NextDouble());
                }
            }
            if (Time.unscaledTime < nextVisualTick)
            {
                if (eventExpired) Rebuild(now);
                return;
            }
            nextVisualTick = Time.unscaledTime + .1f;
            Rebuild(now);
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
                    (.035f * CameraShakeIntensity01);
                shakeCamera.localPosition += previousShakeOffset;
            }
            foreach (var item in active)
            {
                if (item.Zone.kind != TruckTaxiExtremeKind.Earthquake || !item.PhysicsRelevant ||
                    gameNow() >= item.MovingEnds) continue;
                var props = item.Zone.shakeProps;
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
            var props = item.Zone != null ? item.Zone.shakeProps : null;
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
                if (!item.PhysicsRelevant || now >= item.MovingEnds) continue;
                Vector3 delta = playerBody.position - item.Position;
                delta.y = 0;
                float distance = delta.magnitude;
                float radius = item.Zone.radiusMeters;
                if (distance > radius) continue;
                float exposure = 1 - distance / radius;
                Vector3 direction;
                switch (item.Zone.kind)
                {
                    case TruckTaxiExtremeKind.Tornado:
                        direction = distance > .1f ? -delta.normalized + Vector3.Cross(Vector3.up, delta.normalized) * .4f : Vector3.right;
                        playerBody.AddForce(direction.normalized * TruckTaxiExtremeRules.CappedForce(
                            maximumPlayerForceNewtons * exposure, maximumPlayerForceNewtons), ForceMode.Force);
                        break;
                    case TruckTaxiExtremeKind.Hurricane:
                        playerBody.AddForce(item.Zone.transform.right * TruckTaxiExtremeRules.CappedForce(
                            maximumPlayerForceNewtons * .55f * exposure, maximumPlayerForceNewtons), ForceMode.Force);
                        break;
                    case TruckTaxiExtremeKind.Earthquake:
                        float acceleration = Mathf.Clamp(maximumQuakeAcceleration * exposure, 0, 2);
                        playerBody.AddForce(Vector3.up * acceleration * Mathf.Max(0, Mathf.Sin(Time.fixedTime * 17)), ForceMode.Acceleration);
                        break;
                    default:
                        direction = item.Zone.path[item.Zone.path.Length - 1] - item.Zone.path[0];
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
                bool cleanup = now >= item.MovingEnds;
                double duration = (item.MovingEnds - item.Started).TotalSeconds;
                float progress = duration > 0 ? Mathf.Clamp01((float)((now - item.Started).TotalSeconds / duration)) : 1;
                item.Position = item.Zone.PositionAt(cleanup ? 1 : progress);
                bool roadHazard = item.Zone.kind == TruckTaxiExtremeKind.Avalanche ||
                    item.Zone.kind == TruckTaxiExtremeKind.Mudslide;
                Vector3 relevantPosition = roadHazard && cleanup ? RoadCrossing(item.Zone) : item.Position;
                bool loaded = regional == null || regional.IsPositionAvailable(relevantPosition);
                item.Nearby = loaded && (player - relevantPosition).sqrMagnitude <= visualDistanceMeters * visualDistanceMeters;
                item.PhysicsRelevant = loaded &&
                    (player - item.Position).sqrMagnitude <= item.Zone.radiusMeters * item.Zone.radiusMeters;
                bool show = item.Nearby && LiveVisualActorCount < maximumVisualActors;
                if (show)
                {
                    if (item.Visual == null) item.Visual = CreateVisual(item);
                    if (item.Visual != null)
                    {
                        item.Visual.transform.position = item.Position;
                        if (item.Zone.kind == TruckTaxiExtremeKind.Tornado)
                            item.Visual.transform.Rotate(Vector3.up, 35 * .1f, Space.World);
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
                var snapshot = new TruckTaxiExtremeSnapshot(item.Zone.kind, item.Zone.zoneId,
                    item.Zone.regionId, cleanup ? TruckTaxiExtremeStage.Cleanup : TruckTaxiExtremeStage.Moving,
                    item.Ends, relevantPosition,
                    item.Zone.radiusMeters, item.Nearby, roadHazard);
                snapshots.Add(snapshot);
                if (item.PhysicsRelevant && !cleanup)
                {
                    LocalReactionRequested?.Invoke(snapshot);
                    if (item.Zone.kind == TruckTaxiExtremeKind.Earthquake)
                        CameraShakeIntensity01 = Mathf.Max(CameraShakeIntensity01, .55f);
                }
            }
        }

        private GameObject CreateVisual(ActiveEvent item)
        {
            GameObject visual = item.Zone.nearbyVisualPrefab != null
                ? Instantiate(item.Zone.nearbyVisualPrefab, item.Position, Quaternion.identity, transform)
                : CreateFallbackParticles(item.Zone.kind, item.Position);
            if (item.Zone.nearbyVisualPrefab == null &&
                (item.Zone.kind == TruckTaxiExtremeKind.Avalanche || item.Zone.kind == TruckTaxiExtremeKind.Mudslide))
            {
                Vector3 slope = item.Zone.path[item.Zone.path.Length - 1] - item.Zone.path[0];
                visual.transform.rotation = Quaternion.FromToRotation(Vector3.up, slope.normalized);
            }
            if (item.Zone.nearbyVisualPrefab == null && item.Zone.kind == TruckTaxiExtremeKind.Hurricane)
                visual.transform.rotation = Quaternion.FromToRotation(Vector3.up, item.Zone.transform.right);
            if (item.Zone.startClip != null)
            {
                var source = visual.AddComponent<AudioSource>();
                source.spatialBlend = 1;
                source.playOnAwake = false;
                source.clip = item.Zone.startClip;
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
            main.maxParticles = kind == TruckTaxiExtremeKind.Tornado ? 96 : 48;
            main.startLifetime = kind == TruckTaxiExtremeKind.Tornado ? 3 : 1.8f;
            main.startSpeed = kind == TruckTaxiExtremeKind.Tornado ? 8 :
                kind == TruckTaxiExtremeKind.Hurricane ? 11 : 2.5f;
            main.startSize = kind == TruckTaxiExtremeKind.Tornado ? 2 : .7f;
            main.startColor = kind == TruckTaxiExtremeKind.Hurricane
                ? new Color(.65f, .74f, .78f, .55f) : kind == TruckTaxiExtremeKind.Avalanche
                ? new Color(.85f, .9f, .94f, .7f) : kind == TruckTaxiExtremeKind.Mudslide
                    ? new Color(.34f, .27f, .2f, .8f) : new Color(.56f, .55f, .5f, .65f);
            var emission = particles.emission;
            emission.rateOverTime = kind == TruckTaxiExtremeKind.Tornado ? 32 : 18;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.radius = kind == TruckTaxiExtremeKind.Tornado ? 3 : 5;
            shape.angle = kind == TruckTaxiExtremeKind.Tornado ? 18 : 10;
            shape.rotation = new Vector3(-90, 0, 0);
            particles.Play();
            if (kind == TruckTaxiExtremeKind.Tornado)
            {
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
            return visual;
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

        private static Vector3 RoadCrossing(TruckTaxiHazardZone zone) => zone.path[zone.path.Length / 2];

        private GameObject CreateRoadDebris(ActiveEvent item)
        {
            // Two visible shoulder rocks leave the center navigable; no unrepresented road closure.
            var root = new GameObject("Taxi visible partial road debris");
            root.transform.SetParent(transform, false);
            root.transform.position = RoadCrossing(item.Zone);
            Color color = item.Zone.kind == TruckTaxiExtremeKind.Avalanche
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

        private void RaiseWarning(TruckTaxiHazardZone zone, DateTime now, DateTime expires)
        {
            string place = string.IsNullOrWhiteSpace(zone.displayName) ? zone.regionId : zone.displayName;
            string text = zone.kind == TruckTaxiExtremeKind.Earthquake ? "EARTHQUAKE - " + place + " - TAKE CARE" :
                zone.kind == TruckTaxiExtremeKind.Hurricane ? "HURRICANE WARNING - " + place + " - HEAVY WIND AND RAIN" :
                zone.kind == TruckTaxiExtremeKind.Tornado ? "TORNADO WARNING - " + place + " - DRIVE CAREFULLY" :
                zone.kind == TruckTaxiExtremeKind.Avalanche ? "AVALANCHE HAZARD - " + place + " - ROUTE MAY BE UNSAFE" :
                "MUDSLIDE HAZARD - " + place + " - ROUTE MAY BE UNSAFE";
            var warning = new TruckTaxiExtremeWarning(zone.kind, zone.zoneId, text, now, expires);
            warnings.Add(warning);
            WarningRaised?.Invoke(warning);
        }

        private void ClearMatching(TruckTaxiExtremeKind? kind, string zoneId)
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                if (kind.HasValue && active[i].Zone.kind != kind.Value) continue;
                if (zoneId != null && active[i].Zone.zoneId != zoneId) continue;
                RemoveAt(i);
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
                if (item.Zone.kind == TruckTaxiExtremeKind.Hurricane) return true;
            return false;
        }

        private void RemoveAt(int index)
        {
            bool hurricane = active[index].Zone.kind == TruckTaxiExtremeKind.Hurricane;
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
    }
}
