using System.Collections;
using System.Collections.Generic;
using LWS.InterstateHauler;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

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
            public int variant;
        }

        private readonly List<Companion> companions = new List<Companion>();
        private readonly List<Material> materials = new List<Material>();
        private TruckTaxiBootstrap host;
        private TruckTaxiPrivateEventVehicleMotion motion;
        private TruckTaxiSeatProfile seat;
        private Rigidbody truckBody;
        private LwsTruckControlController controls;
        private LwsWheelInputSource wheel;
        private TruckTaxiControlsCatalog.Entry hornBinding, parkingBinding;
        private TextMeshPro identifier;
        private bool hornWasActive;
        private bool boarding;
        private Coroutine boardingRoutine;
        private Companion nearby, active;
        private Phase phase;
        private bool offerLeaseHeld;
        private float eventTime, nextNearbyCheck;
        private const float MaximumSpeed = .447f;
        private const float InteractionRadius = 12f;
        private const float EventSeconds = 7f;
        private static readonly RaycastHit[] groundHits = new RaycastHit[24];
        private static readonly Collider[] clearanceHits = new Collider[32];
        private static readonly float[] roadsideOffsets = { 0f, 6f, -6f, 9f, -9f, 12f, -12f };
        private static readonly string[] ejectionReactions = {
            "Wow. Next time try a goodbye.", "That is the worst date exit I've ever had.",
            "Your truck's manners need work.", "I am leaving a very specific review.",
            "I hope your GPS judges you.", "Keep the ride. I wanted the scenery anyway.",
            "Rude, but admittedly dramatic.", "Fine. The private stop was overrated."
        };

        public bool Ready => host != null && motion != null && motion.Ready && host.DriverNeeds?.State != null &&
            companions.Count > 0;
        public int CompanionCount => companions.Count;
        public bool IsBusy => phase != Phase.Waiting;
        public bool HasOnboardCompanion => active?.actor != null && phase != Phase.Waiting && !boarding;
        public bool CanEject => HasOnboardCompanion && host != null && !host.Paused;
        public TruckTaxiStopObjectivePoint ActivePrivateStop => phase == Phase.ToPrivateStop ? active?.destination : null;
        public float PrivateStopParkingRadius => ActivePrivateStop != null ? ActivePrivateStop.radius : 0f;
        public string HornBinding => BindingLabel(ref hornBinding,"Horn / air horn","H / B");
        public string ParkingBrakeBinding => BindingLabel(ref parkingBinding,"Parking brake","P");
        public float FadeAlpha { get; private set; }
        public string Feedback { get; private set; } = "";
        public string Prompt => phase == Phase.PrivateEvent ? "" : phase == Phase.ToPrivateStop ?
            boarding ? "BOARDING" :
            (ActivePrivateStop == null ? "" : CanInteract ? "PARK" :
                FlatDistance(truckBody.position,ActivePrivateStop.Position)<=ActivePrivateStop.radius ?
                    "PARK & SET BRAKE" : "DRIVE TO PRIVATE STOP") : CanInteract ? "HONK TO PICK UP" : "";
        public bool CanInteract => Ready && !host.Paused && host.Session?.State == TruckTaxiState.Available &&
            !host.Session.HasPassenger && host.Roadside?.IsRecovering != true && host.Fuel?.IsRescuing != true &&
            truckBody != null && truckBody.linearVelocity.magnitude <= MaximumSpeed &&
            (phase == Phase.Waiting ? nearby?.actor != null && Time.time >= nearby.availableAt &&
                FlatDistance(truckBody.position, nearby.actor.transform.position) <= InteractionRadius :
                phase == Phase.ToPrivateStop && !boarding && active?.destination != null &&
                active.destination.IsValidStop(truckBody.position, truckBody.linearVelocity.magnitude) &&
                controls != null && controls.CurrentState.parkingBrakeOn &&
                TruckTaxiSurface.TrySample(truckBody.position, host.Player.transform, out _));

        // Parent adds this after DriverNeeds.Initialize; the motion snapshots renderers on demand.
        public bool Initialize(TruckTaxiBootstrap owner, Transform playerTruckRoot, TruckTaxiSeatProfile companionSeat = null)
        {
            if (host != null) return Ready;
            if (owner?.Player == null || owner.GPS == null || owner.Session == null || owner.DriverNeeds?.State == null)
                return false;
            truckBody = owner.Player.GetComponent<Rigidbody>();
            if (truckBody == null) return false;
            controls=owner.Player.GetComponentInChildren<LwsTruckControlController>(true);
            wheel=owner.Player.GetComponentInChildren<LwsWheelInputSource>(true);
            motion = gameObject.AddComponent<TruckTaxiPrivateEventVehicleMotion>();
            if (!motion.Initialize(playerTruckRoot)) { Destroy(motion); motion = null; return false; }
            host = owner;
            seat = companionSeat;
            identifier=new GameObject("Companion pickup identifier",typeof(TextMeshPro)).GetComponent<TextMeshPro>();
            identifier.transform.SetParent(transform,false);
            identifier.font=owner.hud.font;
            identifier.fontSize=7; identifier.alignment=TextAlignmentOptions.Center;
            identifier.rectTransform.sizeDelta=new Vector2(20,5);
            identifier.color=new Color(1f,.25f,.4f);
            identifier.gameObject.SetActive(false);
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
                var destination = NearbyPrivateStop(pickup.Position, pickup, stops);
                if (destination == null || !TryRoadsidePosition(pickup.Position, pickup.transform.right, out Vector3 standing))
                    continue;
                var actor = new GameObject("Roadside companion (21+ consenting adult)");
                actor.transform.SetPositionAndRotation(standing, Quaternion.Euler(0, variant * 71f, 0));
                var red = CreateMaterial(new Color(variant % 2 == 0 ? .82f : .64f, .06f, .11f));
                var dark = CreateMaterial(new Color(.13f, .10f, .14f));
                var accent = CreateMaterial(new Color(.96f, .35f, .27f));
                var visual=TruckTaxiWobbleVisual.CreateThemed(actor.transform, new TruckTaxiWobbleVisual.Design {
                    silhouette = TruckTaxiWobbleVisual.Silhouette.Human,
                    headwear = variant % 3 == 0 ? TruckTaxiWobbleVisual.Headwear.WideHat :
                        variant % 3 == 1 ? TruckTaxiWobbleVisual.Headwear.Curls : TruckTaxiWobbleVisual.Headwear.Braid,
                    accessory = variant % 2 == 0 ? TruckTaxiWobbleVisual.Accessory.Scarf : TruckTaxiWobbleVisual.Accessory.Flower,
                    height = 1.55f + .08f * (variant % 3), width = 1.1f + .12f * (variant % 3),
                    headScale = 1f, upperBodyScale = 1.35f, clothedAdult = true,
                    body = red, clothes = dark, accent = accent, skin = accent
                });
                visual.AddComponent<TruckTaxiWobbleWalkSway>();
                // CreateThemed schedules primitive collider destruction in play mode.
                foreach (var collider in actor.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
                actor.AddComponent<TruckTaxiPassengerActor>().Bind(null);
                companions.Add(new Companion { actor = actor, pickup = pickup, destination = destination, variant=variant });
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
            return TruckTaxiSurface.TrySample(point.Position, host.Player.transform, out _);
        }

        private TruckTaxiStopObjectivePoint NearbyPrivateStop(Vector3 origin, TruckTaxiStopObjectivePoint exclude,
            TruckTaxiStopObjectivePoint[] stops)
        {
            TruckTaxiStopObjectivePoint best = null;
            float bestDistance = float.PositiveInfinity;
            foreach (var candidate in stops)
            {
                if (candidate == exclude || candidate.category != TruckTaxiStopCategory.PrivateMeeting || !Eligible(candidate)) continue;
                float direct = FlatDistance(origin, candidate.Position);
                if (direct < 25 || direct > 1200 || direct >= bestDistance) continue;
                var route = host.RouteDistances?.Measure(origin, candidate.Position);
                if (route?.Navigable != true || route.Meters > 1800) continue;
                best = candidate;
                bestDistance = direct;
            }
            return best;
        }

        private TruckTaxiStopObjectivePoint ClaimPrivateStop(Vector3 origin,
            TruckTaxiStopObjectivePoint exclude,TruckTaxiStopObjectivePoint[] stops)
        {
            var candidates=new List<TruckTaxiStopObjectivePoint>();
            foreach(var point in stops)
            {
                if(point==exclude || point.category!=TruckTaxiStopCategory.PrivateMeeting || !Eligible(point)) continue;
                float direct=FlatDistance(origin,point.Position);
                if(direct<25f || direct>1200f) continue;
                candidates.Add(point);
            }
            candidates.Sort((a,b)=>FlatDistance(origin,a.Position).CompareTo(FlatDistance(origin,b.Position)));
            foreach(var point in candidates)
            {
                var route=host.RouteDistances?.Measure(origin,point.Position);
                if(route?.Navigable!=true || route.Meters>1800f) continue;
                if(host.GPS.SetPrivateStopDestination(point)) return point;
            }
            return null;
        }

        private static float FlatDistance(Vector3 a, Vector3 b) =>
            Vector3.ProjectOnPlane(a - b, Vector3.up).magnitude;

        private bool TryRoadsidePosition(Vector3 origin, Vector3 right, out Vector3 position,
            Vector3? interactionOrigin = null)
        {
            right.y = 0;
            right = right.sqrMagnitude > .01f ? right.normalized : Vector3.right;
            foreach (float distance in roadsideOffsets)
            {
                Vector3 candidate = origin + right * distance;
                int count = Physics.RaycastNonAlloc(candidate + Vector3.up * 5f, Vector3.down,
                    groundHits, 16f, ~0, QueryTriggerInteraction.Ignore);
                float nearest = float.MaxValue;
                bool offroad = false;
                Vector3 ground = candidate;
                for (int i = 0; i < count; i++)
                {
                    var hit = groundHits[i];
                    if (hit.transform.IsChildOf(host.Player.transform) || hit.distance >= nearest) continue;
                    var surface = hit.collider.GetComponentInParent<TruckTaxiSurface>();
                    if (surface == null) continue;
                    nearest = hit.distance;
                    offroad = !surface.isRoad;
                    ground = hit.point;
                }
                if (!offroad || !ClearOfTruck(ground) ||
                    interactionOrigin.HasValue &&
                    FlatDistance(interactionOrigin.Value, ground) > InteractionRadius) continue;
                position = ground;
                return true;
            }
            position = default;
            return false;
        }

        private bool ClearOfTruck(Vector3 ground)
        {
            int count = Physics.OverlapCapsuleNonAlloc(ground + Vector3.up * .35f,
                ground + Vector3.up * 1.8f, .55f, clearanceHits, ~0, QueryTriggerInteraction.Ignore);
            if (count == clearanceHits.Length) return false;
            for (int i = 0; i < count; i++)
            {
                var collider = clearanceHits[i];
                if (collider.transform.IsChildOf(host.Player.transform) ||
                    collider.GetComponentInParent<TruckTaxiSurface>() == null) return false;
            }
            return true;
        }

        public bool DebugSpawnNearPlayer()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!Ready || IsBusy || truckBody == null) return false;
            foreach (var companion in companions)
            {
                if (companion.actor == null || companion == active) continue;
                Vector3 origin = truckBody.position + truckBody.rotation * Vector3.forward * 7f;
                if (!TryRoadsidePosition(origin, truckBody.rotation * Vector3.right, out Vector3 standing,
                    truckBody.position)) return false;
                companion.actor.transform.SetPositionAndRotation(standing, Quaternion.identity);
                companion.actor.SetActive(true);
                companion.availableAt = 0;
                companion.returnPending = false;
                nearby = companion;
                nextNearbyCheck = 0;
                return true;
            }
#endif
            return false;
        }

        public bool DebugForceNearbyPrivateStop()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!Ready || phase != Phase.ToPrivateStop || active == null) return false;
            var stops = FindObjectsByType<TruckTaxiStopObjectivePoint>(FindObjectsSortMode.None);
            var destination = NearbyPrivateStop(truckBody.position, active.pickup, stops);
            if (destination == null) return false;
            if (host.GPS.TargetId == active.destination.stableId)
                host.GPS.ClearPrivateStopDestination(active.destination.stableId);
            else if (!string.IsNullOrEmpty(host.GPS.TargetId)) return false;
            if(!host.GPS.SetPrivateStopDestination(destination)) return false;
            active.destination = destination;
            host.PickupZone?.SetPrivateStop(destination);
            return true;
#else
            return false;
#endif
        }

        public bool Interact()
        {
            if (!CanInteract) return false;
            if (phase == Phase.Waiting)
            {
                return false;
            }
            active.actor.SetActive(false);
            if (!motion.Begin()) { active.actor.SetActive(true); return false; }
            phase = Phase.PrivateEvent;
            eventTime = 0;
            Feedback = "";
            host.PickupZone?.SetPrivateStop(null);
            return true;
        }

        public bool TryHornPickup()
        {
            if (phase != Phase.Waiting || !CanInteract || nearby?.actor == null || controls==null ||
                !(controls.CurrentState.hornActive || controls.CurrentState.airHornActive)) return false;
            {
                if (!string.IsNullOrEmpty(host.GPS.TargetId)) return false;
                active = nearby; nearby = null;
                var stops=FindObjectsByType<TruckTaxiStopObjectivePoint>(FindObjectsSortMode.None);
                active.destination=ClaimPrivateStop(truckBody.position,active.pickup,stops);
                if(active.destination==null)
                { active=null; return false; }
                phase = Phase.ToPrivateStop;
                host.Session.AcquireOfferSuppression(this);
                offerLeaseHeld = true;
                boarding=true;
                boardingRoutine=StartCoroutine(BoardCompanion(active));
                host.PickupZone?.SetPrivateStop(active.destination);
                Feedback = "Companion is boarding.";
                return true;
            }
        }

        private IEnumerator BoardCompanion(Companion companion)
        {
            float deadline=Time.time+1.5f;
            while(companion?.actor!=null && Time.time<deadline)
            {
                if(truckBody.linearVelocity.magnitude>MaximumSpeed*2f) { Cancel(); yield break; }
                Vector3 door=host.Player.transform.position+host.Player.transform.right*2.3f;
                Vector3 delta=Vector3.ProjectOnPlane(door-companion.actor.transform.position,Vector3.up);
                if(delta.sqrMagnitude>.02f)
                {
                    companion.actor.transform.rotation=Quaternion.LookRotation(delta.normalized);
                    companion.actor.transform.position=Vector3.MoveTowards(companion.actor.transform.position,
                        new Vector3(door.x,companion.actor.transform.position.y,door.z),3f*Time.deltaTime);
                }
                if(delta.sqrMagnitude<.4f) break;
                yield return null;
            }
            if(companion==active && companion.actor!=null)
            {
                TruckTaxiSeatProfile.PlaceActor(companion.actor.transform,host.Player.transform,seat,false,
                    companion.actor.GetComponent<TruckTaxiPassengerActor>().StandingWorldScale);
                var seatedSway=companion.actor.GetComponentInChildren<TruckTaxiWobbleWalkSway>();
                if(seatedSway!=null) seatedSway.enabled=false;
                boarding=false;
                boardingRoutine=null;
                Feedback="Park at the nearby private stop.";
            }
        }

        public bool Eject()
        {
            if (!CanEject) return false;
            var departing=active;
            var actor=departing.actor.GetComponent<TruckTaxiPassengerActor>();
            departing.actor=null;
            motion?.StopMotion();
            actor.gameObject.SetActive(true);
            actor.transform.SetParent(null,true);
            actor.transform.localScale=actor.StandingWorldScale;
            actor.transform.SetPositionAndRotation(host.Player.transform.position+host.Player.transform.right*2.8f+
                Vector3.up*2f,Quaternion.LookRotation(host.Player.transform.forward));
            var ejectedSway=actor.GetComponentInChildren<TruckTaxiWobbleWalkSway>();
            if(ejectedSway!=null) ejectedSway.enabled=true;
            actor.Eject(truckBody.linearVelocity+host.Player.transform.right*6f+Vector3.up*3f,host.Player.transform);
            Finish(false);
            Feedback=ejectionReactions[departing.variant % ejectionReactions.Length];
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
                bool horn=controls != null && (controls.CurrentState.hornActive || controls.CurrentState.airHornActive);
                bool hornPressed=horn && !hornWasActive;
                hornWasActive=horn;
                if (Time.time < nextNearbyCheck && !hornPressed) return;
                nextNearbyCheck = Time.time + .2f;
                nearby = null;
                float best = InteractionRadius;
                foreach (var companion in companions)
                {
                    if (companion.returnPending && Time.time >= companion.returnAt && CanReturnOffscreen(companion))
                    {
                        if (!TryRoadsidePosition(companion.pickup.Position, companion.pickup.transform.right,
                            out Vector3 returnPosition)) continue;
                        companion.actor.transform.SetPositionAndRotation(returnPosition, Quaternion.identity);
                        companion.returnAt = 0;
                        companion.returnPending = false;
                        companion.availableAt = Time.time + 15f;
                    }
                    if (companion.actor == null || !companion.actor.activeSelf || Time.time < companion.availableAt) continue;
                    float distance = FlatDistance(truckBody.position, companion.actor.transform.position);
                    if (distance < best) { nearby = companion; best = distance; }
                }
                if(hornPressed) TryHornPickup();
                if(identifier!=null)
                {
                    identifier.gameObject.SetActive(nearby?.actor!=null && CanInteract);
                    if(identifier.gameObject.activeSelf)
                    {
                        identifier.text="THIRST  /  HONK TO PICK UP ("+HornBinding+")";
                        identifier.transform.position=nearby.actor.transform.position+Vector3.up*2.7f;
                        if(Camera.main!=null) identifier.transform.rotation=Quaternion.LookRotation(
                            identifier.transform.position-Camera.main.transform.position);
                    }
                }
                return;
            }
            if(identifier!=null) identifier.gameObject.SetActive(false);
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
            if(boardingRoutine!=null) { StopCoroutine(boardingRoutine); boardingRoutine=null; }
            boarding=false;
            motion?.StopMotion();
            FadeAlpha = 0;
            host?.PickupZone?.SetPrivateStop(null);
            if (host?.GPS != null && active?.destination != null)
                host.GPS.ClearPrivateStopDestination(active.destination.stableId);
            if (host?.Session != null && offerLeaseHeld) host.Session.ReleaseOfferSuppression(this);
            offerLeaseHeld = false;
            if (active?.actor != null)
            {
                active.actor.transform.SetParent(null, true);
                Vector3 exitOrigin = completed && active.destination != null ?
                    active.destination.Position : active.pickup.Position;
                Vector3 exitRight = completed && active.destination != null ?
                    active.destination.transform.right : active.pickup.transform.right;
                bool safeExit = TryRoadsidePosition(exitOrigin, exitRight, out Vector3 exit);
                if (!safeExit) exit = active.pickup.Position;
                active.actor.transform.SetPositionAndRotation(exit, Quaternion.identity);
                active.actor.transform.localScale = active.actor.GetComponent<TruckTaxiPassengerActor>().StandingWorldScale;
                var exitSway=active.actor.GetComponentInChildren<TruckTaxiWobbleWalkSway>(true);
                if(exitSway!=null) exitSway.enabled=true;
                active.actor.SetActive(safeExit);
                active.availableAt = completed ? float.PositiveInfinity : Time.time + 30f;
                active.returnAt = completed ? Time.time + 15f : 0;
                active.returnPending = completed;
            }
            active = null;
            phase = Phase.Waiting;
            nearby = null;
        }

        private void OnDisable() => Cancel();

        private string BindingLabel(ref TruckTaxiControlsCatalog.Entry entry,string name,string fallback)
        {
            if(entry==null && host?.hud?.UIInput!=null)
                entry=TruckTaxiControlsCatalog.Build(host.hud.UIInput,host.DriverNeeds,host.Passengers)
                    .Find(row=>row.Name==name);
            if(entry==null) return fallback;
            string value=entry.Binding(host.hud.UIInput.UsingGamepad);
            if(wheel!=null && wheel.HasConnectedDevice)
            {
                string wheelValue=entry.WheelBinding(wheel.CalibrationProfile);
                if(wheelValue!="UNBOUND") value+=" / "+wheelValue;
            }
            return value;
        }

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
