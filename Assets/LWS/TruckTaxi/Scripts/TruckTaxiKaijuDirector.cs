using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public readonly struct TruckTaxiKaijuSnapshot
    {
        public TruckTaxiKaijuSnapshot(TruckTaxiKaijuKind kind, string regionId, Vector3 position, float dangerRadius)
        { Kind = kind; RegionId = regionId; Position = position; DangerRadius = dangerRadius; }
        public TruckTaxiKaijuKind Kind { get; }
        public string RegionId { get; }
        public Vector3 Position { get; }
        public float DangerRadius { get; }
    }

    public readonly struct TruckTaxiKaijuCaptureContext
    {
        public TruckTaxiKaijuCaptureContext(Transform hand, Transform mouth, Vector3 origin)
        { Hand = hand; Mouth = mouth; Origin = origin; }
        public Transform Hand { get; }
        public Transform Mouth { get; }
        public Vector3 Origin { get; }
    }

    [DisallowMultipleComponent]
    public sealed class TruckTaxiKaijuDirector : MonoBehaviour
    {
        [SerializeField] private TruckTaxiKaijuConfig configuration;
        private TruckTaxiKaijuConfig runtimeDefaults;
        private TruckTaxiBootstrap host;
        private TruckTaxiRegionalWorld regional;
        private TruckTaxiKaijuSchedule schedule;
        private TruckTaxiKaijuActor actor;
        private TruckTaxiKaijuPlayerRecovery playerRecovery;
        private TruckTaxiMapMarker marker;
        private TruckTaxiKaijuKind kind;
        private string regionId;
        private Vector3 travelHeading;
        private Vector3 fireDirection;
        private float endsAt, nextInteraction, nextReaction, nextStomp, holdUntil, fireUntil, warningUntil, nextPlayerWarning;
        private float fireDamageAt;
        private bool holdingThrow, firing, telegraphing, playerWarned;
        private TruckTaxiKaijuProp heldProp;
        private GameObject heldVisual;
        private Vector3 heldOrigin, heldScale;
        private float heldStarted;
        private int trafficVictims;
        private GameObject captureVisual;
        private bool captureActive;
        private float captureStarted;
        private Vector3 captureOrigin;
        private Vector3 captureScale;
        private Action<bool> captureCompletion;
        private readonly List<TruckTaxiKaijuProp> usedProps = new List<TruckTaxiKaijuProp>();
        private readonly List<GameObject> debris = new List<GameObject>();
        private readonly List<float> debrisExpiry = new List<float>();
        private readonly System.Random random = new System.Random(50417);
        private bool initialized;

        public TruckTaxiKaijuKind Kind => kind;
        public TruckTaxiKaijuSnapshot State => new TruckTaxiKaijuSnapshot(kind, regionId,
            actor != null ? actor.transform.position : Vector3.zero, kind == TruckTaxiKaijuKind.Attack ? 180 : 0);
        public DateTime NextNaturalSighting => schedule?.NextSighting ?? default;
        public DateTime NextNaturalAttack => schedule?.NextAttack ?? default;
        public bool IsActive => actor != null;
        public bool IsCapturingPlayer => captureActive;
        public bool PlayerCaptureBound => TryBeginPlayerCapture != null && CompletePlayerCapture != null;
        public TruckTaxiKaijuConfig Configuration => configuration != null ? configuration : runtimeDefaults;

        // Initialize binds the existing-roadside adapter. Optional parent replacements must provide
        // both callbacks and restore the rig on false (interruption without a capture penalty).
        public Func<TruckTaxiKaijuCaptureContext, bool> TryBeginPlayerCapture;
        public Action<bool> CompletePlayerCapture;
        public event Action<TruckTaxiKaijuSnapshot> StateChanged;
        public event Action<string> WarningRaised;
        public event Action<TruckTaxiKaijuSnapshot> RegionalThreatRaised;
        public event Action<Vector3, float> PlayerDamageRequested;
        public event Action<Vector3, float> TrafficPanicRequested;
        public event Action<string> TrafficVictimTaken;

        public bool Initialize(TruckTaxiBootstrap boot)
        {
            if (initialized) return host == boot;
            if (boot == null || !boot.Ready || boot.Player == null || boot.CalendarWeather == null) return false;
            host = boot;
            regional = boot.GetComponent<TruckTaxiRegionalWorld>();
            if (configuration == null) runtimeDefaults = ScriptableObject.CreateInstance<TruckTaxiKaijuConfig>();
            schedule = new TruckTaxiKaijuSchedule(Configuration, 50417);
            schedule.Start(boot.CalendarWeather.CurrentDateTime);
            playerRecovery = GetComponent<TruckTaxiKaijuPlayerRecovery>() ?? gameObject.AddComponent<TruckTaxiKaijuPlayerRecovery>();
            if (playerRecovery.Initialize(boot))
            {
                TryBeginPlayerCapture = playerRecovery.TryBegin;
                CompletePlayerCapture = playerRecovery.Complete;
                PlayerDamageRequested += playerRecovery.ApplyFireDamage;
            }
            else Debug.LogWarning("Kaiju capture is unavailable: player/roadside recovery is not initialized.", this);
            initialized = true;
            return true;
        }

        private void Update()
        {
            if (!initialized || host == null || host.Player == null) return;
            if (captureActive) { TickCapture(Time.unscaledTime); return; }
            if (host.Paused) return;
            float now = Time.time;
            if (actor == null)
            {
                var due = schedule.Advance(host.CalendarWeather.CurrentDateTime);
                if (due != TruckTaxiKaijuKind.None) StartEvent(due, false);
                return;
            }
            if (now >= endsAt) { EndKaiju(); return; }
            actor.Tick(Time.deltaTime, travelHeading, Camera.main != null ? Camera.main.transform : null);
            if (now >= nextStomp)
            {
                nextStomp = now + 2.2f;
                actor.Stomp(Configuration.stomp);
                if (kind == TruckTaxiKaijuKind.Attack) CrushAuthoredVehicle();
            }
            if (marker != null) marker.transform.position = actor.transform.position;
            TickHeld(now);
            TickFire(now);
            TickDebris(now);
            TickPlayerWarning(now);
            if (now >= nextReaction) { nextReaction = now + 12; PanicNearby(); }
            if (now >= nextInteraction) { nextInteraction = now + (kind == TruckTaxiKaijuKind.Attack ? 15 : 32); NaturalInteraction(); }
        }

        public bool ForceSightingHere() => StartEvent(TruckTaxiKaijuKind.Sighting, true);
        public bool ForceAttackHere() => StartEvent(TruckTaxiKaijuKind.Attack, true);

        public bool StartEvent(TruckTaxiKaijuKind requested, bool recordSchedule = true)
        {
            if (!initialized || !isActiveAndEnabled || actor != null || requested == TruckTaxiKaijuKind.None) return false;
            Vector3 player = host.Player.transform.position;
            Vector3 position = SelectSpawn(player, requested);
            var go = new GameObject("Cinder Spindle - " + requested);
            go.transform.SetParent(transform, true);
            go.transform.position = position;
            actor = go.AddComponent<TruckTaxiKaijuActor>();
            actor.Initialize(Configuration);
            travelHeading = Vector3.ProjectOnPlane(player - position, Vector3.up).normalized;
            if (requested == TruckTaxiKaijuKind.Sighting)
                travelHeading = Quaternion.Euler(0, 65, 0) * travelHeading;
            actor.transform.rotation = Quaternion.LookRotation(travelHeading, Vector3.up);
            kind = requested;
            regionId = regional != null ? regional.ResolveRegion(position) : string.Empty;
            if (string.IsNullOrEmpty(regionId)) regionId = regional != null ? regional.ResolveRegion(player) : "current region";
            if (string.IsNullOrEmpty(regionId)) regionId = "current region";
            endsAt = Time.time + (requested == TruckTaxiKaijuKind.Attack ? Configuration.attackSeconds : Configuration.sightingSeconds);
            nextReaction = Time.time;
            nextStomp = Time.time + 1;
            nextInteraction = Time.time + 15;
            playerWarned = false;
            nextPlayerWarning = 0;
            if (recordSchedule) schedule.Record(requested, host.CalendarWeather.CurrentDateTime);
            EnsureMarker();
            Publish(requested == TruckTaxiKaijuKind.Attack ? "EMERGENCY ALERT\nKAIJU ATTACK - " + regionId :
                "GIANT CREATURE SPOTTED - " + regionId);
            actor.Roar(Configuration.roar);
            StateChanged?.Invoke(State);
            RegionalThreatRaised?.Invoke(State);
            return true;
        }

        public Vector3? TeleportTarget => actor != null ? actor.transform.position : (Vector3?)null;

        public bool ForceGrabCar() => TryGrabTraffic() || TryGrab(TruckTaxiKaijuPropKind.Vehicle);
        public bool ForceThrowCar()
        {
            if (heldVisual == null || (heldProp != null && heldProp.kind != TruckTaxiKaijuPropKind.Vehicle)) return false;
            holdingThrow = true; holdUntil = Time.time; TickHeld(Time.time); return true;
        }
        public bool ForceFire()
        {
            if (actor == null || telegraphing || firing) return false;
            Vector3 target = actor.transform.position + actor.transform.forward * 120;
            if (kind == TruckTaxiKaijuKind.Attack && (host.Player.transform.position - actor.transform.position).sqrMagnitude < 150 * 150)
                target = host.Player.transform.position + Vector3.up * 2;
            fireDirection = (target - actor.FireOrigin).normalized;
            telegraphing = true; fireUntil = Time.time + 2;
            Publish("KAIJU FIRE BREATH - GET CLEAR");
            return true;
        }
        public bool ForceGrabPlayer()
        {
            if (actor == null || kind != TruckTaxiKaijuKind.Attack ||
                TryBeginPlayerCapture == null || CompletePlayerCapture == null) return false;
            playerWarned = true; warningUntil = Time.time + Configuration.CaptureWarningSeconds;
            Publish("KAIJU IS REACHING FOR YOUR TRUCK - ESCAPE NOW");
            return true;
        }

        public void EndKaiju()
        {
            if (actor == null && kind == TruckTaxiKaijuKind.None) return;
            if (captureActive) FinishCapture(false);
            if (heldProp != null) heldProp.Eat();
            else if (heldVisual != null) Destroy(heldVisual);
            heldProp = null;
            heldVisual = null;
            trafficVictims = 0;
            foreach (var prop in usedProps) if (prop != null) prop.Restore();
            usedProps.Clear();
            for (int i = 0; i < debris.Count; i++) if (debris[i] != null) Destroy(debris[i]);
            debris.Clear(); debrisExpiry.Clear();
            if (actor != null) Destroy(actor.gameObject);
            actor = null;
            kind = TruckTaxiKaijuKind.None;
            firing = telegraphing = playerWarned = false;
            if (marker != null) marker.state = TruckTaxiMapMarkerState.Hidden;
            host?.GPS?.MapMarkers?.RequestRefresh();
            StateChanged?.Invoke(State);
        }

        private Vector3 SelectSpawn(Vector3 player, TruckTaxiKaijuKind requested)
        {
            string currentRegion = regional != null ? regional.ResolveRegion(player) : string.Empty;
            TruckTaxiKaijuSpawnAnchor selected = null;
            float bestScore = float.MaxValue;
            foreach (var anchor in FindObjectsByType<TruckTaxiKaijuSpawnAnchor>(FindObjectsSortMode.None))
            {
                if (!anchor.isActiveAndEnabled || (requested == TruckTaxiKaijuKind.Attack ? !anchor.allowAttack : !anchor.allowSighting)) continue;
                if (!string.IsNullOrEmpty(anchor.regionId) && anchor.regionId != currentRegion) continue;
                float distance = Vector3.Distance(player, anchor.transform.position);
                if (distance < Configuration.minimumSpawnMeters || distance > Configuration.maximumSpawnMeters) continue;
                float score = Mathf.Abs(distance - (Configuration.minimumSpawnMeters + Configuration.maximumSpawnMeters) * .5f);
                if (score < bestScore) { bestScore = score; selected = anchor; }
            }
            if (selected != null) return selected.transform.position;
            Vector3 fallback = player + Vector3.forward * Configuration.minimumSpawnMeters;
            for (int i = 0; i < 24; i++)
            {
                float angle = (float)(random.NextDouble() * Math.PI * 2);
                float distance = Mathf.Lerp(Configuration.minimumSpawnMeters, Configuration.maximumSpawnMeters,
                    (float)random.NextDouble());
                var point = player + new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)) * distance;
                string region = regional != null ? regional.ResolveRegion(point) : currentRegion;
                if (regional != null && string.IsNullOrEmpty(region)) continue;
                if (i < 12 && !string.IsNullOrEmpty(currentRegion) && region != currentRegion) continue;
                if (Physics.Raycast(point + Vector3.up * 300, Vector3.down, out var hit, 600,
                    ~0, QueryTriggerInteraction.Ignore)) point.y = hit.point.y;
                return point;
            }
            return fallback;
        }

        private void EnsureMarker()
        {
            if (host.GPS?.MapMarkers == null) return;
            if (marker == null)
            {
                var go = new GameObject("Kaiju map marker"); go.transform.SetParent(transform, false);
                marker = go.AddComponent<TruckTaxiMapMarker>(); marker.stableId = "taxi.kaiju.active";
                host.GPS.MapMarkers.Register(marker);
            }
            marker.label = kind == TruckTaxiKaijuKind.Attack ? "KAIJU ATTACK" : "KAIJU SIGHTING";
            marker.markerType = kind == TruckTaxiKaijuKind.Attack ? TruckTaxiMapMarkerType.Danger : TruckTaxiMapMarkerType.Hazard;
            marker.state = TruckTaxiMapMarkerState.Active;
            marker.transform.position = actor.transform.position;
            host.GPS.MapMarkers.RequestRefresh();
        }

        private void Publish(string text)
        {
            host?.hud?.Alerts?.Publish("kaiju:" + text, text, TruckTaxiAlertPriority.Critical, 12);
            WarningRaised?.Invoke(text);
        }

        private void PanicNearby()
        {
            Vector3 center = actor.transform.position;
            host.pedestrians?.ReactToHazard(center, 220, 24);
            int reacted = 0;
            if (host.traffic != null)
                foreach (var car in host.traffic.Vehicles)
                {
                    if (car == null || (car.transform.position - center).sqrMagnitude > 220 * 220) continue;
                    car.GetComponent<TruckTaxiTrafficBehaviour>()?.ReactToHazard(3);
                    if (++reacted >= 6) break;
                }
            TrafficPanicRequested?.Invoke(center, 220);
        }

        private void NaturalInteraction()
        {
            if (actor == null) return;
            if (kind == TruckTaxiKaijuKind.Attack && random.NextDouble() < .35) { ForceFire(); return; }
            if (!ForceGrabCar() && kind == TruckTaxiKaijuKind.Attack)
                TryGrab(TruckTaxiKaijuPropKind.BuildingChunk);
            else actor.Roar(Configuration.roar);
        }

        private bool TryGrab(TruckTaxiKaijuPropKind propKind)
        {
            if (!CanGrab) return false;
            TruckTaxiKaijuProp nearest = null;
            float best = Configuration.interactionRadiusMeters * Configuration.interactionRadiusMeters;
            foreach (var prop in FindObjectsByType<TruckTaxiKaijuProp>(FindObjectsSortMode.None))
            {
                if (prop.kind != propKind || !prop.IsEligible) continue;
                float distance = (prop.transform.position - actor.transform.position).sqrMagnitude;
                if (distance < best) { nearest = prop; best = distance; }
            }
            if (nearest == null || nearest.Grab(actor.Hand) == null) return false;
            usedProps.Add(nearest);
            heldProp = nearest;
            HoldVisual(nearest.Visual);
            return true;
        }

        private bool CanGrab => actor != null && heldVisual == null &&
            usedProps.Count + trafficVictims < Mathf.Clamp(Configuration.maximumPropsPerEvent, 0, 12);

        private bool TryGrabTraffic()
        {
            if (!CanGrab || host.traffic == null) return false;
            var candidates = new List<GameObject>();
            float radius = Mathf.Clamp(Configuration.interactionRadiusMeters, 5, 100);
            foreach (var vehicle in host.traffic.Vehicles)
                if (vehicle != null && vehicle.activeInHierarchy &&
                    (vehicle.transform.position - actor.transform.position).sqrMagnitude <= radius * radius)
                    candidates.Add(vehicle);
            candidates.Sort((a, b) => (a.transform.position - actor.transform.position).sqrMagnitude.CompareTo(
                (b.transform.position - actor.transform.position).sqrMagnitude));
            foreach (var vehicle in candidates)
            {
                var pooled = vehicle.GetComponent<TruckTaxiTrafficPooledActor>();
                if (pooled == null || pooled.MissionReserved ||
                    vehicle.GetComponentInChildren<NWH.VehiclePhysics2.VehicleController>(true) != null) continue;
                int generation = pooled.Generation;
                string id = vehicle.GetComponent<TruckTaxiImpactTarget>()?.targetId;
                var visual = TruckTaxiKaijuVisual.Snapshot(vehicle.transform);
                if (visual == null) continue;
                if (!host.traffic.TryReleaseKaijuVictim(vehicle, generation)) { Destroy(visual); continue; }
                trafficVictims++;
                HoldVisual(visual);
                TrafficVictimTaken?.Invoke(id);
                return true;
            }
            return false;
        }

        private void HoldVisual(GameObject visual)
        {
            heldVisual = visual;
            heldOrigin = visual.transform.position;
            visual.transform.SetParent(actor.Hand, true);
            heldScale = visual.transform.localScale;
            heldStarted = Time.time;
            holdingThrow = kind == TruckTaxiKaijuKind.Attack && random.NextDouble() < .55;
            holdUntil = Time.time + 3;
        }

        private void CrushAuthoredVehicle()
        {
            if (usedProps.Count + trafficVictims >= Configuration.maximumPropsPerEvent) return;
            foreach (var prop in FindObjectsByType<TruckTaxiKaijuProp>(FindObjectsSortMode.None))
            {
                if (prop.kind != TruckTaxiKaijuPropKind.Vehicle || !prop.IsEligible ||
                    (prop.transform.position - actor.transform.position).sqrMagnitude > 20 * 20) continue;
                if (prop.Crush()) usedProps.Add(prop);
                return;
            }
        }

        private void TickHeld(float now)
        {
            if (heldVisual == null) { heldProp = null; return; }
            float reach = Mathf.Clamp01(now - heldStarted);
            float swallow = holdingThrow ? 0 : Mathf.Clamp01((now - heldStarted - 1) / 2);
            heldVisual.transform.position = Vector3.Lerp(Vector3.Lerp(heldOrigin, actor.Hand.position, reach),
                actor.Mouth.position, swallow);
            heldVisual.transform.localScale = heldScale * Mathf.Lerp(1, .08f, swallow);
            if (now < holdUntil) return;
            if (holdingThrow && debris.Count < Configuration.maximumDebris)
            {
                Vector3 away = Vector3.ProjectOnPlane(actor.transform.position - host.Player.transform.position, Vector3.up).normalized;
                if (away.sqrMagnitude < .01f) away = actor.transform.forward;
                Vector3 velocity = away * 22 + Vector3.up * 13;
                var thrown = heldProp != null ? heldProp.Throw(velocity) : heldVisual;
                if (heldProp == null) TruckTaxiKaijuVisual.Throw(thrown, velocity);
                if (thrown != null) { debris.Add(thrown); debrisExpiry.Add(now + Configuration.debrisLifetimeSeconds); }
            }
            else if (heldProp != null) heldProp.Eat();
            else Destroy(heldVisual);
            heldProp = null;
            heldVisual = null;
        }

        private void TickDebris(float now)
        {
            for (int i = debris.Count - 1; i >= 0; i--)
            {
                if (now < debrisExpiry[i]) continue;
                if (debris[i] != null) Destroy(debris[i]);
                debris.RemoveAt(i); debrisExpiry.RemoveAt(i);
            }
        }

        private void TickFire(float now)
        {
            if (telegraphing && now >= fireUntil)
            {
                telegraphing = false; firing = true; fireUntil = now + 3; fireDamageAt = now;
                actor.Fire(Configuration.fire);
            }
            if (firing && now >= fireUntil)
            {
                firing = false;
                actor.PulseExplosion(actor.FireOrigin + fireDirection * 120);
            }
            actor.SetFire(firing || telegraphing, telegraphing, 150, fireDirection);
            if (!firing || now < fireDamageAt) return;
            fireDamageAt = now + .5f;
            Vector3 toPlayer = host.Player.transform.position + Vector3.up * 2 - actor.FireOrigin;
            bool blocked = Physics.Raycast(actor.FireOrigin, toPlayer.normalized, out var obstruction, toPlayer.magnitude,
                ~0, QueryTriggerInteraction.Ignore) && !obstruction.transform.IsChildOf(host.Player.transform);
            if (!blocked && toPlayer.magnitude < 150 && Vector3.Angle(fireDirection, toPlayer) < 22)
                PlayerDamageRequested?.Invoke(host.Player.transform.position, .12f);
            TrafficPanicRequested?.Invoke(actor.FireOrigin + fireDirection * 100, 90);
        }

        private void TickPlayerWarning(float now)
        {
            if (kind != TruckTaxiKaijuKind.Attack || host.Player == null || captureActive) return;
            float distance = Vector3.Distance(host.Player.transform.position, actor.transform.position);
            if (!playerWarned)
            {
                if (distance >= 95 || now < nextPlayerWarning) return;
                playerWarned = true;
                warningUntil = now + Configuration.CaptureWarningSeconds;
                Publish("KAIJU NEARBY - DRIVE CLEAR OF THE DANGER AREA");
                return;
            }
            if (now < warningUntil) return;
            playerWarned = false;
            nextPlayerWarning = now + 20;
            Vector3 reach = host.Player.transform.position + Vector3.up * 2 - actor.Hand.position;
            bool blocked = Physics.Raycast(actor.Hand.position, reach.normalized, out var hit, reach.magnitude,
                ~0, QueryTriggerInteraction.Ignore) && !hit.transform.IsChildOf(host.Player.transform);
            if (!blocked && distance <= Configuration.CaptureRadiusMeters && PlayerCaptureBound) BeginCapture();
        }

        private void BeginCapture()
        {
            var visual = CaptureTruckVisual();
            var context = new TruckTaxiKaijuCaptureContext(actor.Hand, actor.Mouth, actor.transform.position);
            var completion = CompletePlayerCapture;
            bool accepted;
            try { accepted = TryBeginPlayerCapture(context); }
            catch (Exception error)
            {
                Debug.LogException(error, this);
                Destroy(visual);
                try { completion?.Invoke(false); }
                catch (Exception cleanupError) { Debug.LogException(cleanupError, this); }
                return;
            }
            if (!accepted) { Destroy(visual); return; }
            captureVisual = visual;
            captureActive = true;
            captureStarted = Time.unscaledTime;
            captureCompletion = completion;
            actor.Roar(Configuration.roar);
            Publish("KAIJU HAS YOUR TRUCK");
        }

        private GameObject CaptureTruckVisual()
        {
            var root = new GameObject("Captured truck visual only");
            captureOrigin = host.Player.transform.position;
            root.transform.SetPositionAndRotation(host.Player.transform.position, host.Player.transform.rotation);
            int meshes = 0;
            var renderers = playerRecovery != null ? playerRecovery.GetRigRenderers() : host.Player.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                if (!(renderer is MeshRenderer) || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                var source = renderer.GetComponent<MeshFilter>();
                if (source == null || source.sharedMesh == null) continue;
                if (++meshes > 128) break;
                var part = new GameObject(source.name);
                part.transform.SetParent(root.transform, false);
                part.transform.localPosition = root.transform.InverseTransformPoint(source.transform.position);
                part.transform.localRotation = Quaternion.Inverse(root.transform.rotation) * source.transform.rotation;
                part.transform.localScale = source.transform.lossyScale;
                part.AddComponent<MeshFilter>().sharedMesh = source.sharedMesh;
                part.AddComponent<MeshRenderer>().sharedMaterials = renderer.sharedMaterials;
            }
            if (root.transform.childCount == 0)
            {
                var fallback = GameObject.CreatePrimitive(PrimitiveType.Cube);
                fallback.name = "Truck cab fallback";
                fallback.transform.SetParent(root.transform, false);
                fallback.transform.localScale = new Vector3(3, 3, 7);
                var collider = fallback.GetComponent<Collider>(); collider.enabled = false; Destroy(collider);
            }
            root.transform.SetParent(actor.Hand, true);
            captureScale = root.transform.localScale;
            return root;
        }

        private void TickCapture(float now)
        {
            if (!captureActive) return;
            if (captureVisual == null || actor == null) { FinishCapture(false); return; }
            float progress = Mathf.Clamp01((now - captureStarted) / 4);
            captureVisual.transform.position = progress < .25f ?
                Vector3.Lerp(captureOrigin, actor.Hand.position, progress * 4) :
                Vector3.Lerp(actor.Hand.position, actor.Mouth.position, (progress - .25f) / .75f);
            captureVisual.transform.localScale = captureScale * Mathf.Lerp(1, .08f, Mathf.Clamp01((progress - .25f) / .75f));
            if (progress >= 1) FinishCapture(true);
        }

        private void FinishCapture(bool swallowed)
        {
            captureActive = false;
            if (captureVisual != null) Destroy(captureVisual);
            captureVisual = null;
            var completion = captureCompletion;
            captureCompletion = null;
            try { completion?.Invoke(swallowed); }
            catch (Exception error) { Debug.LogException(error, this); }
        }

        private void OnDisable() { EndKaiju(); }
        private void OnDestroy()
        {
            EndKaiju();
            if (playerRecovery != null) PlayerDamageRequested -= playerRecovery.ApplyFireDamage;
            if (runtimeDefaults != null) Destroy(runtimeDefaults);
        }
    }
}
