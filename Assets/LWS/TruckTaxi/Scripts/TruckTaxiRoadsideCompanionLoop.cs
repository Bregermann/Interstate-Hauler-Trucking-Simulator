using System.Collections.Generic;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // Separate from fare passengers: this component never assigns a ride or fare objective.
    [DisallowMultipleComponent]
    public sealed class TruckTaxiRoadsideCompanionLoop : MonoBehaviour
    {
        private enum Phase { Waiting, ToPrivateStop, PrivateEvent }
        private sealed class Companion
        {
            public GameObject actor;
            public TruckTaxiStopObjectivePoint pickup;
            public TruckTaxiStopObjectivePoint destination;
            public float availableAt;
            public float returnAt;
            public bool returnPending;
        }

        private readonly List<Companion> companions = new List<Companion>();
        private readonly List<Material> materials = new List<Material>();
        private TruckTaxiBootstrap host;
        private TruckTaxiPrivateEventVehicleMotion motion;
        private TruckTaxiSeatProfile seat;
        private Rigidbody truckBody;
        private Companion nearby, active;
        private Phase phase;
        private bool offerLeaseHeld;
        private float eventTime, nextNearbyCheck;
        private const float MaximumSpeed = .447f;
        private const float InteractionRadius = 12f;
        private const float EventSeconds = 7f;

        public bool Ready => host != null && motion != null && motion.Ready && host.DriverNeeds?.State != null &&
            companions.Count > 0;
        public int CompanionCount => companions.Count;
        public bool IsBusy => phase != Phase.Waiting;
        public float FadeAlpha { get; private set; }
        public string Feedback { get; private set; } = "";
        public string Prompt => phase == Phase.PrivateEvent ? "" : phase == Phase.ToPrivateStop ?
            (CanInteract ? "PARK" : "") : CanInteract ? "PICK UP" : "";
        public bool CanInteract => Ready && !host.Paused && host.Session?.State == TruckTaxiState.Available &&
            !host.Session.HasPassenger && host.Roadside?.IsRecovering != true && host.Fuel?.IsRescuing != true &&
            truckBody != null && truckBody.linearVelocity.magnitude <= MaximumSpeed &&
            (phase == Phase.Waiting ? nearby?.actor != null && Time.time >= nearby.availableAt &&
                FlatDistance(truckBody.position, nearby.actor.transform.position) <= InteractionRadius :
                phase == Phase.ToPrivateStop && active?.destination != null &&
                active.destination.IsValidStop(truckBody.position, truckBody.linearVelocity.magnitude));

        // Parent adds this after DriverNeeds.Initialize; the motion snapshots renderers on demand.
        public bool Initialize(TruckTaxiBootstrap owner, Transform playerTruckRoot, TruckTaxiSeatProfile companionSeat = null)
        {
            if (host != null) return Ready;
            if (owner?.Player == null || owner.GPS == null || owner.Session == null || owner.DriverNeeds?.State == null)
                return false;
            truckBody = owner.Player.GetComponent<Rigidbody>();
            if (truckBody == null) return false;
            motion = gameObject.AddComponent<TruckTaxiPrivateEventVehicleMotion>();
            if (!motion.Initialize(playerTruckRoot)) { Destroy(motion); motion = null; return false; }
            host = owner;
            seat = companionSeat;
            SpawnAtAuthoredStops();
            return Ready;
        }

        private void SpawnAtAuthoredStops()
        {
            var stops = FindObjectsByType<TruckTaxiStopObjectivePoint>(FindObjectsSortMode.None);
            System.Array.Sort(stops, (a, b) => string.CompareOrdinal(a.stableId, b.stableId));
            int variant = 0;
            foreach (var pickup in stops)
            {
                if (companions.Count >= 8) break;
                if (!Eligible(pickup) || pickup.category != TruckTaxiStopCategory.IllicitPickup &&
                    pickup.category != TruckTaxiStopCategory.PrivateMeeting) continue;
                var destination = NearbyPrivateStop(pickup, stops);
                if (destination == null) continue;
                var actor = new GameObject("Roadside companion (21+ consenting adult)");
                actor.transform.SetPositionAndRotation(pickup.Position, Quaternion.Euler(0, variant * 71f, 0));
                var red = CreateMaterial(new Color(variant % 2 == 0 ? .82f : .64f, .06f, .11f));
                var dark = CreateMaterial(new Color(.13f, .10f, .14f));
                var accent = CreateMaterial(new Color(.96f, .35f, .27f));
                TruckTaxiWobbleVisual.CreateThemed(actor.transform, new TruckTaxiWobbleVisual.Design {
                    silhouette = TruckTaxiWobbleVisual.Silhouette.Human,
                    headwear = variant % 3 == 0 ? TruckTaxiWobbleVisual.Headwear.WideHat :
                        variant % 3 == 1 ? TruckTaxiWobbleVisual.Headwear.Curls : TruckTaxiWobbleVisual.Headwear.Braid,
                    accessory = variant % 2 == 0 ? TruckTaxiWobbleVisual.Accessory.Scarf : TruckTaxiWobbleVisual.Accessory.Flower,
                    height = 1.55f + .08f * (variant % 3), width = 1.1f + .12f * (variant % 3),
                    headScale = 1f, upperBodyScale = 1.35f, clothedAdult = true,
                    body = red, clothes = dark, accent = accent, skin = accent
                });
                // CreateThemed schedules primitive collider destruction in play mode.
                foreach (var collider in actor.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
                companions.Add(new Companion { actor = actor, pickup = pickup, destination = destination });
                variant++;
            }
        }

        private Material CreateMaterial(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader) { color = color, hideFlags = HideFlags.DontSave };
            materials.Add(material);
            return material;
        }

        private bool Eligible(TruckTaxiStopObjectivePoint point)
        {
            if (point == null || !point.isActiveAndEnabled || point.gameObject.scene != host.gameObject.scene ||
                string.IsNullOrWhiteSpace(point.stableId)) return false;
            return TruckTaxiSurface.TrySample(point.Position, host.Player.transform, out bool onRoad) && !onRoad;
        }

        private TruckTaxiStopObjectivePoint NearbyPrivateStop(TruckTaxiStopObjectivePoint pickup,
            TruckTaxiStopObjectivePoint[] stops)
        {
            TruckTaxiStopObjectivePoint best = null;
            float bestDistance = float.PositiveInfinity;
            foreach (var candidate in stops)
            {
                if (candidate == pickup || candidate.category != TruckTaxiStopCategory.PrivateMeeting || !Eligible(candidate)) continue;
                float direct = FlatDistance(pickup.Position, candidate.Position);
                if (direct < 25 || direct > 220 || direct >= bestDistance) continue;
                var route = host.RouteDistances?.Measure(pickup.Position, candidate.Position);
                if (route?.Navigable != true || route.Meters > 300) continue;
                best = candidate;
                bestDistance = direct;
            }
            return best;
        }

        private static float FlatDistance(Vector3 a, Vector3 b) =>
            Vector3.ProjectOnPlane(a - b, Vector3.up).magnitude;

        public bool Interact()
        {
            if (!CanInteract) return false;
            if (phase == Phase.Waiting)
            {
                if (!string.IsNullOrEmpty(host.GPS.TargetId)) return false;
                active = nearby; nearby = null;
                phase = Phase.ToPrivateStop;
                host.Session.AcquireOfferSuppression(this);
                offerLeaseHeld = true;
                Transform mount = seat != null && !string.IsNullOrWhiteSpace(seat.mountPath) ?
                    host.Player.transform.Find(seat.mountPath) : null;
                active.actor.transform.SetParent(mount != null ? mount : host.Player.transform, false);
                active.actor.transform.localPosition = seat != null ? seat.localPosition : new Vector3(.5f, 1f, 0);
                active.actor.transform.localRotation = Quaternion.Euler(seat != null ? seat.localEulerAngles : Vector3.zero);
                active.actor.transform.localScale = seat != null ? seat.localScale : Vector3.one;
                host.GPS.SetServiceDestination(active.destination.stableId, active.destination.displayName,
                    active.destination.Position);
                Feedback = "Park at the nearby private stop.";
                return true;
            }
            active.actor.SetActive(false);
            if (!motion.Begin()) { active.actor.SetActive(true); return false; }
            phase = Phase.PrivateEvent;
            eventTime = 0;
            Feedback = "";
            return true;
        }

        private void Update() => Tick(Time.deltaTime);

        public void Tick(float deltaTime)
        {
            if (!Ready) return;
            if (phase != Phase.Waiting && (host.Session.State != TruckTaxiState.Available ||
                host.Session.HasPassenger || host.Roadside?.IsRecovering == true || host.Fuel?.IsRescuing == true))
            { Cancel(); return; }
            if (host.Paused) return;
            if (phase == Phase.Waiting)
            {
                if (Time.time < nextNearbyCheck) return;
                nextNearbyCheck = Time.time + .2f;
                nearby = null;
                float best = InteractionRadius;
                foreach (var companion in companions)
                {
                    if (companion.returnPending && Time.time >= companion.returnAt && CanReturnOffscreen(companion))
                    {
                        companion.actor.transform.SetPositionAndRotation(companion.pickup.Position, Quaternion.identity);
                        companion.returnAt = 0;
                        companion.returnPending = false;
                        companion.availableAt = Time.time + 15f;
                    }
                    if (companion.actor == null || !companion.actor.activeSelf || Time.time < companion.availableAt) continue;
                    float distance = FlatDistance(truckBody.position, companion.actor.transform.position);
                    if (distance < best) { nearby = companion; best = distance; }
                }
                return;
            }
            if (phase == Phase.ToPrivateStop)
            {
                // GPS may auto-complete inside 3 m; another claimed destination is not ours to clear.
                if (!string.IsNullOrEmpty(host.GPS.TargetId) && host.GPS.TargetId != active.destination.stableId)
                    Cancel();
                return;
            }
            if (truckBody.linearVelocity.magnitude > MaximumSpeed) { Cancel(); return; }
            eventTime += Mathf.Max(0, deltaTime);
            FadeAlpha = .65f * Mathf.Min(Mathf.Clamp01(eventTime / .6f),
                Mathf.Clamp01((EventSeconds - eventTime) / .8f));
            if (eventTime < EventSeconds) return;
            host.DriverNeeds.State.SatisfyThirst();
            Feedback = "THIRST SATISFIED";
            Finish(true);
        }

        public void Cancel()
        {
            if (phase == Phase.Waiting) return;
            Feedback = "Private stop cancelled.";
            Finish(false);
        }

        private void Finish(bool completed)
        {
            motion?.StopMotion();
            FadeAlpha = 0;
            if (host?.GPS != null && active?.destination != null)
                host.GPS.CompleteServiceDestination(active.destination.stableId);
            if (host?.Session != null && offerLeaseHeld) host.Session.ReleaseOfferSuppression(this);
            offerLeaseHeld = false;
            if (active?.actor != null)
            {
                active.actor.transform.SetParent(null, true);
                Vector3 exit = completed && active.destination != null ?
                    active.destination.Position + active.destination.transform.right * 4f : active.pickup.Position;
                if (completed && active.destination != null &&
                    (!TruckTaxiSurface.TrySample(exit, host.Player.transform, out bool onRoad) || onRoad))
                    exit = active.destination.Position;
                active.actor.transform.SetPositionAndRotation(exit, Quaternion.identity);
                active.actor.transform.localScale = Vector3.one;
                active.actor.SetActive(true);
                active.availableAt = completed ? float.PositiveInfinity : Time.time + 30f;
                active.returnAt = completed ? Time.time + 15f : 0;
                active.returnPending = completed;
            }
            active = null;
            phase = Phase.Waiting;
            nearby = null;
        }

        private void OnDisable() => Cancel();

        private bool CanReturnOffscreen(Companion companion)
        {
            if (companion.actor == null || companion.pickup == null ||
                FlatDistance(truckBody.position, companion.actor.transform.position) < 150f ||
                FlatDistance(truckBody.position, companion.pickup.Position) < 150f) return false;
            var camera = Camera.main;
            return camera != null ? Offscreen(camera, companion.actor.transform.position) &&
                Offscreen(camera, companion.pickup.Position) :
                FlatDistance(truckBody.position, companion.actor.transform.position) > 300f &&
                FlatDistance(truckBody.position, companion.pickup.Position) > 300f;
        }

        private static bool Offscreen(Camera camera, Vector3 position)
        {
            Vector3 viewport = camera.WorldToViewportPoint(position);
            return viewport.z <= 0 || viewport.x < -.1f || viewport.x > 1.1f ||
                viewport.y < -.1f || viewport.y > 1.1f;
        }

        private void OnDestroy()
        {
            Cancel();
            foreach (var companion in companions) if (companion.actor != null) Destroy(companion.actor);
            foreach (var material in materials) if (material != null) Destroy(material);
            companions.Clear(); materials.Clear();
        }
    }
}
