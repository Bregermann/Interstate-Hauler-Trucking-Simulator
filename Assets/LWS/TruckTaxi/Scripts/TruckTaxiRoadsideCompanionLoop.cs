using System.Collections;
using System.Collections.Generic;
using LWS.InterstateHauler;
using NWH.Common.Cameras;
using NWH.VehiclePhysics2;
using NWH.VehiclePhysics2.Input;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LWS.TruckTaxi
{
    // Separate from fare passengers: this component never assigns a ride or fare objective.
    [DisallowMultipleComponent]
    public sealed class TruckTaxiRoadsideCompanionLoop : MonoBehaviour
    {
        private enum Phase { Waiting, AuxiliaryRide, ToPrivateStop, MovingToSleeper, PrivateEvent, CompletingContent }
        private sealed class Companion
        {
            public GameObject actor;
            public TruckTaxiStopObjectivePoint pickup;
            public TruckTaxiStopObjectivePoint destination;
            public float availableAt;
            public float returnAt;
            public bool returnPending;
            public int variant;
            public float nextImpactAt;
            public Coroutine impactRoutine;
            public TruckTaxiCompanionPopulationPolicy.Record population;
            public bool materializedBefore;
            public float nextMaterializationAttempt;
        }

        private readonly List<Companion> companions = new List<Companion>();
        private readonly List<Companion> departingCompanions = new List<Companion>();
        private readonly List<Material> materials = new List<Material>();
        private readonly TruckTaxiCompanionPopulationPolicy population = new TruckTaxiCompanionPopulationPolicy();
        private readonly List<Companion> populationOrder = new List<Companion>();
        private readonly Material[][] variantMaterials = new Material[TruckTaxiCompanionPopulationPolicy.VariantCount][];
        private System.Func<Vector3, bool> regionAvailable;
        private float nextPopulationRefresh, nextMaterialization;
        public TruckTaxiCustomInteractionProfile[] variantInteractionProfiles = System.Array.Empty<TruckTaxiCustomInteractionProfile>();
        private bool ownsCustomContent, contentCompleting;
        private float nextContentBeat;
        private readonly HashSet<Renderer> eventVehicleRenderers = new HashSet<Renderer>();
        public TruckTaxiCustomInteractionProfile ActiveInteractionProfile
        {
            get
            {
                if (active != null && active.variant >= 0)
                {
                    if (variantInteractionProfiles != null && active.variant < variantInteractionProfiles.Length && variantInteractionProfiles[active.variant] != null)
                        return variantInteractionProfiles[active.variant];
                    var authored = host?.Configuration?.customCompanionVariantInteractions;
                    if (authored != null && active.variant < authored.Length && authored[active.variant] != null)
                        return authored[active.variant];
                }
                return host?.Configuration?.customCompanionInteraction;
            }
        }
        [Min(8)] public int maximumMaterialized = 48;
        [Min(40)] public float materializationRadius = 180;
        public int LogicalPopulationCount => population.Count;
        public int MaterializedCount
        {
            get
            {
                int count = 0;
                foreach (var c in companions) if (c.actor != null) count++;
                foreach (var c in departingCompanions) if (c.actor != null) count++;
                if (active?.actor != null && !companions.Contains(active)) count++;
                return count;
            }
        }
        public int RejectedPlacementCount { get; private set; }
        public int MaterializationBudget => Mathf.Clamp(maximumMaterialized, 8, 128);
        public int NearbyPopulationDeficit
        {
            get
            {
                if (truckBody == null) return 0;
                int count = 0;
                foreach (var c in companions)
                    if (FlatDistance(truckBody.position, c.population.Origin) <= materializationRadius &&
                        (regionAvailable == null || regionAvailable(c.population.Origin)) &&
                        (c == active || c.actor == null || !c.actor.activeInHierarchy || Time.time < c.availableAt)) count++;
                return count;
            }
        }
        public Transform ActiveCompanionVisual => active?.actor != null ? active.actor.transform : null;
        public TruckTaxiCabOccupantRig OccupantRig => cabRig;
        public Transform PlayerTruckRoot => host?.Player != null ? host.Player.transform : null;
        public void SetRegionAvailability(System.Func<Vector3, bool> availability) => regionAvailable = availability;
        public void RegisterBlock(string id, Bounds bounds) { population.RegisterBlock(id, bounds); SyncPopulationRecords(); }
        public void RegisterService(string id, Vector3 position, float radius, bool largeSite = false)
        { population.RegisterService(id, position, radius, largeSite); SyncPopulationRecords(); }
        private TruckTaxiBootstrap host;
        private TruckTaxiPrivateEventVehicleMotion motion;
        private TruckTaxiCabOccupantRig cabRig;
        private TruckTaxiDriverPresentation driverPresentation;
        private VehicleController eventVehicle;
        private CameraChanger eventCameraChanger;
        private GameObject previousCamera, exteriorCamera;
        private Lws18SpeedTransmissionController eventTransmission;
        private VehicleInputStates previousInputStates;
        private RigidbodyConstraints previousConstraints;
        private bool sleeperHold, previousAutoInput, previousSwapInput, previousControlsEnabled;
        private bool previousTransmissionEnabled, previousCameraChangerEnabled, previousCameraInside;
        private TruckTaxiSeatProfile seat;
        private Rigidbody truckBody;
        private LwsTruckControlController controls;
        private LwsWheelInputSource wheel;
        private TruckTaxiControlsCatalog.Entry hornBinding, parkingBinding;
        private TextMeshPro identifier;
        private bool hornWasActive;
        private bool boarding;
        private long privateRouteToken;
        private Coroutine boardingRoutine;
        private Companion nearby, active;
        public readonly struct PickupTarget
        {
            public readonly string Id;
            public readonly Transform Actor;
            public readonly int Variant;
            internal PickupTarget(string id, Transform actor, int variant)
            { Id = id; Actor = actor; Variant = variant; }
        }
        private object pickupReservationOwner;
        private PickupTarget reservedPickup;
        private System.Func<bool> reservedPickupCanBoard;
        public event System.Action<PickupTarget> CompanionBoarded;
        private Phase phase;
        private bool offerLeaseHeld;
        private float eventTime, nextNearbyCheck;
        private const float MaximumSpeed = .447f;
        private const float InteractionRadius = 12f;
        private const float EventSeconds = 7f;
        private static readonly float[] roadsideOffsets = { 0f, 6f, -6f, 9f, -9f, 12f, -12f };
        private static readonly string[] ejectionReactions = {
            "Wow. Next time try a goodbye.", "That is the worst date exit I've ever had.",
            "Your truck's manners need work.", "I am leaving a very specific review.",
            "I hope your GPS judges you.", "Keep the ride. I wanted the scenery anyway.",
            "Rude, but admittedly dramatic.", "Fine. The private stop was overrated."
        };

        public bool Ready => host != null && host.Player != null && host.Session != null && host.GPS != null &&
            truckBody != null && motion != null && motion.Ready && host.DriverNeeds?.State != null;
        public int CompanionCount => companions.Count;
        public bool IsBusy => phase != Phase.Waiting;
        public bool HasOnboardCompanion => active?.actor != null && phase != Phase.Waiting && !boarding;
        public bool CanEject => HasOnboardCompanion && host != null && !host.Paused;
        public bool SleeperSequenceActive => sleeperHold;
        public bool FrontSeatsVacated => sleeperHold && cabRig != null && cabRig.PrivateTransitionComplete;
        public TruckTaxiStopObjectivePoint ActivePrivateStop => phase == Phase.ToPrivateStop ? active?.destination : null;
        public float PrivateStopParkingRadius => ActivePrivateStop != null ? ActivePrivateStop.radius : 0f;
        public string HornBinding => BindingLabel(ref hornBinding,"Horn / air horn","H");
        public string ParkingBrakeBinding => BindingLabel(ref parkingBinding,"Parking brake","P");
        public float FadeAlpha { get; private set; }
        public string Feedback { get; private set; } = "";
        public string Prompt => phase == Phase.PrivateEvent || phase == Phase.MovingToSleeper || phase == Phase.CompletingContent ? "" :
            phase == Phase.AuxiliaryRide ? (boarding ? "BOARDING" : "COMPANION ONBOARD") : phase == Phase.ToPrivateStop ?
            boarding ? "BOARDING" :
            (ActivePrivateStop == null ? "" : CanInteract ? "PARK" :
                FlatDistance(truckBody.position,ActivePrivateStop.Position)<=ActivePrivateStop.radius ?
                    "PARK & SET BRAKE" : "DRIVE TO PRIVATE STOP") : PickupInRange ?
                        (HasRoom ? "HONK TO PICK UP" : "NO ROOM FOR ANOTHER PASSENGER") : "";
        private TruckTaxiSeatType CompanionSeatType => seat != null ? seat.seatType : TruckTaxiSeatType.CompanionPassenger;
        public bool HasRoom
        {
            get
            {
                if (active != null || boarding) return false;
                if (cabRig == null && host?.Player != null) cabRig = host.Player.GetComponent<TruckTaxiCabOccupantRig>();
                return cabRig != null && cabRig.HasSecondarySeat(CompanionSeatType, nearby?.actor?.transform);
            }
        }

        public bool TryFindPickupTarget(float maximumDistance, out PickupTarget target)
        {
            target = default;
            if (!Ready || IsBusy || pickupReservationOwner != null || !float.IsFinite(maximumDistance)) return false;
            float best = Mathf.Min(Mathf.Max(0, maximumDistance), materializationRadius);
            bool found = false;
            foreach (var companion in companions)
            {
                if (!IsPickupAvailable(companion, best)) continue;
                best = FlatDistance(truckBody.position, companion.actor.transform.position);
                target = PickupSnapshot(companion); found = true;
            }
            return found;
        }

        public bool IsPickupTargetAvailable(PickupTarget target, float maximumDistance) =>
            target.Actor != null && IsPickupAvailable(FindPickup(target), maximumDistance);

        private bool IsPickupAvailable(Companion companion, float maximumDistance)
        {
            if (!Ready || IsBusy || !float.IsFinite(maximumDistance) || maximumDistance <= 0) return false;
            if (companion == null || companion.actor == null || !companion.actor.activeInHierarchy ||
                companion.returnPending || companion.impactRoutine != null || Time.time < companion.availableAt ||
                regionAvailable != null && !regionAvailable(companion.actor.transform.position)) return false;
            var actor = companion.actor.GetComponent<TruckTaxiPassengerActor>();
            if (actor == null || actor.IsTumbling || actor.Lifecycle != TruckTaxiActorLifecycle.OutsideWaiting ||
                actor.transform.IsChildOf(host.Player.transform) || FlatDistance(truckBody.position, actor.transform.position) >
                Mathf.Min(maximumDistance, materializationRadius)) return false;
            if (cabRig == null) cabRig = host.Player.GetComponent<TruckTaxiCabOccupantRig>();
            return cabRig != null && cabRig.HasSecondarySeat(CompanionSeatType, actor.transform);
        }

        public bool TryReservePickupTarget(object owner, PickupTarget target, System.Func<bool> canBoard = null)
        {
            if (owner == null || pickupReservationOwner != null && !ReferenceEquals(owner, pickupReservationOwner) ||
                !IsPickupTargetAvailable(target, materializationRadius)) return false;
            pickupReservationOwner = owner; reservedPickup = target; reservedPickupCanBoard = canBoard;
            nearby = FindPickup(target);
            return true;
        }

        public bool ReleasePickupTarget(object owner)
        {
            if (owner == null || !ReferenceEquals(owner, pickupReservationOwner)) return false;
            pickupReservationOwner = null; reservedPickup = default; reservedPickupCanBoard = null;
            nearby = null;
            return true;
        }

        public bool IsPickupTargetBoarding(PickupTarget target) => boarding && MatchesPickup(active, target);
        public bool IsPickupTargetOnboard(PickupTarget target) => HasOnboardCompanion && MatchesPickup(active, target) &&
            target.Actor.IsChildOf(host.Player.transform) &&
            target.Actor.GetComponent<TruckTaxiPassengerActor>()?.Lifecycle == TruckTaxiActorLifecycle.Seated;
        private static PickupTarget PickupSnapshot(Companion companion) => companion?.actor != null && companion.population != null ?
            new PickupTarget(companion.population.Id + ".actor." + companion.actor.GetInstanceID(),
                companion.actor.transform, companion.variant) : default;
        private static bool MatchesPickup(Companion companion, PickupTarget target) => target.Actor != null &&
            companion?.actor != null && companion.actor.transform == target.Actor && PickupSnapshot(companion).Id == target.Id;
        private Companion FindPickup(PickupTarget target)
        {
            foreach (var companion in companions) if (MatchesPickup(companion, target)) return companion;
            return null;
        }
        public static bool AllowsHornPickup(TruckTaxiState state) =>
            state == TruckTaxiState.Available || state == TruckTaxiState.DrivingToPickup ||
            state == TruckTaxiState.DrivingToDestination;

        private bool SafeInteraction => Ready && !host.Paused && host.Session != null &&
            AllowsHornPickup(host.Session.State) &&
            (host.WorldLife == null || host.WorldLife.Mode == TruckTaxiPlayerControlMode.Driving) &&
            host.Roadside?.IsRecovering != true && host.Fuel?.IsRescuing != true &&
            truckBody != null && truckBody.linearVelocity.magnitude <= MaximumSpeed;
        private bool PickupInRange => SafeInteraction && phase == Phase.Waiting && nearby?.actor != null &&
            Time.time >= nearby.availableAt && FlatDistance(truckBody.position, nearby.actor.transform.position) <= InteractionRadius;
        public bool CanInteract => SafeInteraction &&
            truckBody != null && truckBody.linearVelocity.magnitude <= MaximumSpeed &&
            (phase == Phase.Waiting ? PickupInRange && HasRoom :
                phase == Phase.ToPrivateStop && !host.Session.HasPassenger && !boarding && active?.destination != null &&
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
            cabRig = owner.Player.GetComponent<TruckTaxiCabOccupantRig>();
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
            foreach (var service in TruckTaxiServicePoint.Points)
                if (service != null && service.isActiveAndEnabled && service.capabilities != TruckTaxiServiceCapability.None &&
                    !string.IsNullOrWhiteSpace(service.stableId))
                    population.RegisterService(service.stableId, service.Position, service.InteractionRadius, service.radius >= 30);
            foreach (var location in FindObjectsByType<TruckTaxiRideLocation>(FindObjectsSortMode.None))
                if (TruckTaxiCompanionPopulationPolicy.IsServiceLocation(location.locationType) &&
                    !string.IsNullOrWhiteSpace(location.locationId) && location.GetComponent<TruckTaxiServicePoint>() == null)
                    population.RegisterService(location.locationId, location.StopPosition, location.detectionRadius);
            foreach (var depot in FindObjectsByType<TruckTaxiRivalDepot>(FindObjectsSortMode.None))
                if (!string.IsNullOrWhiteSpace(depot.depotId))
                    population.RegisterService(depot.depotId, depot.StagingPosition, 18, true);
            foreach (var area in FindObjectsByType<TruckTaxiPedestrianArea>(FindObjectsSortMode.None))
                if (!string.IsNullOrWhiteSpace(area.areaId))
                    population.RegisterBlock(area.areaId, new Bounds(area.transform.position,
                        new Vector3(area.localSize.x * Mathf.Abs(area.transform.lossyScale.x), 4,
                            area.localSize.y * Mathf.Abs(area.transform.lossyScale.z))));
            foreach (var pickup in FindObjectsByType<TruckTaxiStopObjectivePoint>(FindObjectsSortMode.None))
                if (!string.IsNullOrWhiteSpace(pickup.stableId) &&
                    (pickup.category == TruckTaxiStopCategory.IllicitPickup || pickup.category == TruckTaxiStopCategory.PrivateMeeting))
                    population.RegisterBlock(pickup.stableId, new Bounds(pickup.Position, new Vector3(40, 4, 40)));
            SyncPopulationRecords();
        }

        private void SyncPopulationRecords()
        {
            while (companions.Count < population.Count)
            {
                var record = population.Records[companions.Count];
                companions.Add(new Companion { population = record, variant = record.Variant });
            }
        }

        private void MaterializeNearby()
        {
            if (Time.time < nextMaterialization) return;
            nextMaterialization = Time.time + .5f;
            for (int i = departingCompanions.Count - 1; i >= 0; i--)
            {
                var departure = departingCompanions[i];
                if (departure.actor == null) { departingCompanions.RemoveAt(i); continue; }
                if (Time.time >= departure.returnAt && FlatDistance(truckBody.position, departure.actor.transform.position) > 30 &&
                    (Camera.main == null || Offscreen(Camera.main, departure.actor.transform.position)))
                { RetireActor(departure); departingCompanions.RemoveAt(i); }
            }
            populationOrder.Clear();
            foreach (var companion in companions)
            {
                if (companion == active || companion.impactRoutine != null) continue;
                bool available = regionAvailable == null || regionAvailable(companion.population.Origin);
                float distance = FlatDistance(truckBody.position, companion.population.Origin);
                if (companion.actor != null && (!available || distance > materializationRadius + 60 &&
                    (Camera.main == null || Offscreen(Camera.main, companion.actor.transform.position))))
                    RetireActor(companion);
                if (available && distance <= materializationRadius) populationOrder.Add(companion);
            }
            populationOrder.Sort((a, b) => FlatDistance(truckBody.position, a.population.Origin)
                .CompareTo(FlatDistance(truckBody.position, b.population.Origin)));
            int count = MaterializedCount, attempts = 0;
            foreach (var companion in populationOrder)
            {
                if (companion.actor != null || Time.time < companion.availableAt ||
                    Time.time < companion.nextMaterializationAttempt || count >= MaterializationBudget) continue;
                if (++attempts > 8) break;
                if (!TryPopulationPosition(companion, out var standing))
                { RejectedPlacementCount++; companion.nextMaterializationAttempt = Time.time + 5; continue; }
                if (companion.materializedBefore && (FlatDistance(truckBody.position, standing) < 30 ||
                    Camera.main != null && !Offscreen(Camera.main, standing))) continue;
                if (CreateActor(companion, standing)) count++;
            }
        }

        private bool TryPopulationPosition(Companion companion, out Vector3 standing)
        {
            var record = companion.population;
            for (int attempt = 0; attempt < 32; attempt++)
            {
                float angle = (record.Slot * 360f / record.Capacity + attempt * 137.5f) * Mathf.Deg2Rad;
                float radius = record.IsService ? record.ExclusionRadius + 3 + attempt % 4 * 3 :
                    Mathf.Min(record.Area.extents.x, record.Area.extents.z) * (.2f + attempt % 4 * .2f);
                Vector3 candidate = record.Origin + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
                if (!TruckTaxiActorWorldPlacement.TryGround(candidate, host.Player.transform, out standing) ||
                    !TruckTaxiActorWorldPlacement.ClearOfTrafficLanes(standing, host.pedestrians?.roadLanes, 1.2f) ||
                    !TruckTaxiActorWorldPlacement.Clear(standing, 1.2f, 2.5f, host.Player.transform)) continue;
                bool occupied = false;
                foreach (var service in population.Records)
                    if (service.IsService && FlatDistance(standing, service.Origin) < service.ExclusionRadius + 1.5f) { occupied = true; break; }
                foreach (var other in companions)
                    if (other.actor != null && FlatDistance(standing, other.actor.transform.position) < 3) { occupied = true; break; }
                if (!occupied) return true;
            }
            standing = default;
            return false;
        }

        private void RetireActor(Companion companion)
        {
            if (companion.actor == null) return;
            var actor = companion.actor.GetComponent<TruckTaxiPassengerActor>();
            actor.WaitingTruckHit -= OnCompanionHit;
            actor.DisarmWaitingTruckHit();
            companion.actor.SetActive(false);
            Destroy(companion.actor);
            companion.actor = null;
        }

        private bool CreateActor(Companion companion, Vector3 standing)
        {
                int variant = companion.variant;
                var actor = new GameObject("Roadside companion (21+ consenting adult)");
                actor.transform.SetPositionAndRotation(standing, Quaternion.Euler(0, variant * 71f, 0));
                if (variantMaterials[variant] == null) variantMaterials[variant] = new[] {
                    CreateMaterial(new Color(variant % 2 == 0 ? .82f : .64f, .06f, .11f)),
                    CreateMaterial(new Color(.13f, .10f, .14f)), CreateMaterial(new Color(.96f, .35f, .27f)) };
                var red = variantMaterials[variant][0]; var dark = variantMaterials[variant][1]; var accent = variantMaterials[variant][2];
                var visual=TruckTaxiWobbleVisual.CreateThemed(actor.transform, new TruckTaxiWobbleVisual.Design {
                    silhouette = TruckTaxiWobbleVisual.Silhouette.Human,
                    headwear = variant % 3 == 0 ? TruckTaxiWobbleVisual.Headwear.WideHat :
                        variant % 3 == 1 ? TruckTaxiWobbleVisual.Headwear.Curls : TruckTaxiWobbleVisual.Headwear.Braid,
                    accessory = variant % 2 == 0 ? TruckTaxiWobbleVisual.Accessory.Scarf : TruckTaxiWobbleVisual.Accessory.Flower,
                    height = 1.55f + .04f * (variant % 5), width = 1.1f + .06f * (variant % 4),
                    headScale = 1f, upperBodyScale = 1.35f, clothedAdult = true,
                    body = red, clothes = dark, accent = accent, skin = accent
                });
                visual.AddComponent<TruckTaxiWobbleWalkSway>();
                // CreateThemed schedules primitive collider destruction in play mode.
                foreach (var collider in actor.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
                var passengerActor=actor.AddComponent<TruckTaxiPassengerActor>();
                passengerActor.Bind(null);
                actor.transform.position = standing + Vector3.up * passengerActor.StandingGroundOffset;
                passengerActor.WaitingTruckHit+=OnCompanionHit;
                passengerActor.ArmWaitingTruckHit(truckBody);
                companion.actor = actor;
                if (!TruckTaxiActorWorldPlacement.Clear(standing, passengerActor.StandingRadius,
                    passengerActor.StandingHeight, host.Player.transform, actor.transform))
                {
                    RetireActor(companion);
                    companion.nextMaterializationAttempt = Time.time + 5;
                    RejectedPlacementCount++;
                    return false;
                }
                companion.materializedBefore = true;
                companion.returnPending = false;
                return true;
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
            if (point == null || !point.isActiveAndEnabled || !point.gameObject.scene.isLoaded ||
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
                if(host.GPS.SetPrivateStopDestination(point))
                { privateRouteToken=host.GPS.CompanionLeaseToken; return point; }
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
                if (!TruckTaxiActorWorldPlacement.TryGround(candidate, host.Player.transform, out var ground) ||
                    !TruckTaxiActorWorldPlacement.Clear(ground, 1.2f, 2.5f, host.Player.transform) ||
                    !TruckTaxiActorWorldPlacement.ClearOfTrafficLanes(ground, host.pedestrians?.roadLanes, 1.2f) ||
                    interactionOrigin.HasValue &&
                    FlatDistance(interactionOrigin.Value, ground) > InteractionRadius) continue;
                bool insideService = false;
                foreach (var record in population.Records)
                    if (record.IsService && FlatDistance(ground, record.Origin) < record.ExclusionRadius + 1.5f)
                    { insideService = true; break; }
                if (insideService) continue;
                position = ground;
                return true;
            }
            position = default;
            return false;
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
                companion.actor.GetComponent<TruckTaxiPassengerActor>().ArmWaitingTruckHit(truckBody);
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
            if(!host.GPS.SetPrivateStopDestination(destination)) return false;
            privateRouteToken=host.GPS.CompanionLeaseToken;
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
            cabRig = host.Player.GetComponent<TruckTaxiCabOccupantRig>();
            if (cabRig != null && cabRig.HasSleeperCapability)
            {
                driverPresentation = host.Player.GetComponent<TruckTaxiDriverPresentation>();
                if (driverPresentation == null || !driverPresentation.Ready || !TryAcquireSleeperHold())
                {
                    Feedback = "Sleeper event requires an available exterior camera and parked truck controls.";
                    return false;
                }
                if (!driverPresentation.TryBeginSleeperTransition(cabRig, active.actor.transform,
                    seat != null ? seat.seatType : TruckTaxiSeatType.CompanionPassenger))
                {
                    EndSleeperSequence();
                    Feedback = "Sleeper event requires two compatible private occupant anchors.";
                    return false;
                }
                phase = Phase.MovingToSleeper;
                eventTime = 0;
                Feedback = "";
                host.PickupZone?.SetPrivateStop(null);
                return true;
            }
            // Keep the participant visibly seated in the existing non-graphic presentation.
            phase = Phase.PrivateEvent;
            if (!BeginEventPresentation()) { phase = Phase.ToPrivateStop; return false; }
            eventTime = 0;
            Feedback = "";
            host.PickupZone?.SetPrivateStop(null);
            return true;
        }

        private bool BeginEventPresentation()
        {
            var profile = ActiveInteractionProfile;
            if (profile == null || host.CustomContent == null) return motion.Begin();
            driverPresentation = host.Player.GetComponent<TruckTaxiDriverPresentation>();
            eventVehicleRenderers.Clear();
            foreach (var renderer in host.Player.GetComponentsInChildren<Renderer>(true))
                if (renderer.GetComponentInParent<TruckTaxiPassengerActor>() == null &&
                    (driverPresentation?.Occupant == null || !renderer.transform.IsChildOf(driverPresentation.Occupant)))
                    eventVehicleRenderers.Add(renderer);
            host.CustomContent.Begin(profile, driverPresentation?.Occupant, active.actor.transform, inputHeldByEvent: true);
            ownsCustomContent = true;
            contentCompleting = false;
            nextContentBeat = 1;
            SyncContentMotion();
            return true;
        }

        private void SyncContentMotion()
        {
            if (!ownsCustomContent || host.CustomContent == null) return;
            FadeAlpha = host.CustomContent.FadeAlpha;
            if (!host.CustomContent.RequestsTruckRocking) { motion.StopMotion(); return; }
            if (motion.IsActive) return;
            // Snapshot only the pre-event truck renderers, never participants or the runner's proxies.
            // This is synchronous visibility isolation; no rendered frame or physics state changes.
            var excluded = new Dictionary<Renderer, bool>();
            foreach (var renderer in host.Player.GetComponentsInChildren<Renderer>(true))
                if (!eventVehicleRenderers.Contains(renderer))
                { excluded[renderer] = renderer.enabled; renderer.enabled = false; }
            try { motion.Begin(); }
            finally { foreach (var entry in excluded) if (entry.Key != null) entry.Key.enabled = entry.Value; }
        }

        private bool TryAcquireSleeperHold()
        {
            eventVehicle = host.Player.GetComponent<VehicleController>();
            eventCameraChanger = host.Player.GetComponentInChildren<CameraChanger>(true);
            eventTransmission = host.Player.GetComponent<Lws18SpeedTransmissionController>();
            if (eventVehicle == null || !eventVehicle.isActiveAndEnabled || eventVehicle.input == null || !eventVehicle.input.autoSetInput ||
                controls == null || !controls.enabled || !controls.CurrentState.parkingBrakeOn || truckBody == null ||
                truckBody.isKinematic || eventCameraChanger == null || !eventCameraChanger.isActiveAndEnabled ||
                eventCameraChanger.cameras == null) return false;
            exteriorCamera = null;
            foreach (var candidate in eventCameraChanger.cameras)
                if (candidate != null && candidate.GetComponentInChildren<Camera>(true) != null &&
                    candidate.GetComponent<CameraInsideVehicle>()?.isInsideVehicle != true &&
                    (candidate.transform.parent == null || candidate.transform.parent.gameObject.activeInHierarchy))
                { exteriorCamera = candidate; break; }
            if (exteriorCamera == null) return false;
            int index = eventCameraChanger.currentCameraIndex;
            if (index < 0 || index >= eventCameraChanger.cameras.Count) return false;
            previousCamera = eventCameraChanger.cameras[index];
            if (previousCamera == null || !previousCamera.activeInHierarchy) return false;
            previousCameraChangerEnabled = eventCameraChanger.enabled;
            previousCameraInside = eventVehicle.CameraInsideVehicle;
            previousAutoInput = eventVehicle.input.autoSetInput;
            previousSwapInput = eventVehicle.input.swapInputInReverse;
            previousInputStates = eventVehicle.input.states;
            previousControlsEnabled = controls.enabled;
            previousTransmissionEnabled = eventTransmission != null && eventTransmission.enabled;
            previousConstraints = truckBody.constraints;
            sleeperHold = true;
            controls.enabled = false;
            if (eventTransmission != null) eventTransmission.enabled = false;
            eventCameraChanger.enabled = false;
            truckBody.linearVelocity = Vector3.zero;
            truckBody.angularVelocity = Vector3.zero;
            truckBody.constraints = RigidbodyConstraints.FreezeAll;
            HoldSleeperInputs();
            if (SelectEventCamera(exteriorCamera)) return true;
            EndSleeperSequence();
            return false;
        }

        private void HoldSleeperInputs()
        {
            if (!sleeperHold || eventVehicle == null) return;
            // NWH manual input blocks every provider, including wheel input. Do not replay stale throttle on release.
            eventVehicle.input.autoSetInput = false;
            eventVehicle.input.swapInputInReverse = false;
            var input = previousInputStates;
            input.Reset();
            input.brakes = input.brakesRaw = input.inputSwappedBrakes = input.inputSwappedBrakesRaw = 1;
            input.handbrake = input.handbrakeRaw = 1;
            input.clutch = input.clutchRaw = 1;
            input.lowBeamLights = previousInputStates.lowBeamLights;
            input.highBeamLights = previousInputStates.highBeamLights;
            input.hazardLights = previousInputStates.hazardLights;
            eventVehicle.input.states = input;
        }

        private bool SelectEventCamera(GameObject cameraObject)
        {
            if (eventCameraChanger == null || cameraObject == null || eventCameraChanger.cameras == null ||
                !eventCameraChanger.cameras.Contains(cameraObject)) return false;
            for (int i = 0; i <= eventCameraChanger.cameras.Count; i++)
            {
                int index = eventCameraChanger.currentCameraIndex;
                if (index >= 0 && index < eventCameraChanger.cameras.Count &&
                    eventCameraChanger.cameras[index] == cameraObject && cameraObject.activeInHierarchy)
                {
                    if (eventVehicle != null) eventVehicle.CameraInsideVehicle =
                        cameraObject.GetComponent<CameraInsideVehicle>()?.isInsideVehicle == true;
                    host?.Player?.GetComponentInChildren<LwsNwhCameraPresentationMonitor>(true)?.PublishCurrentCamera(false);
                    return true;
                }
                eventCameraChanger.NextCamera();
            }
            return false;
        }

        private void EndSleeperSequence()
        {
            if (driverPresentation != null) driverPresentation.RestoreDriving();
            if (cabRig != null) cabRig.EndPrivateTransition();
            if (!sleeperHold) return;
            sleeperHold = false;
            if (truckBody != null) truckBody.constraints = previousConstraints;
            if (eventVehicle != null)
            {
                var restored = previousInputStates;
                restored.Reset();
                restored.handbrake = restored.handbrakeRaw = controls != null && controls.CurrentState.parkingBrakeOn ? 1 : 0;
                restored.lowBeamLights = previousInputStates.lowBeamLights;
                restored.highBeamLights = previousInputStates.highBeamLights;
                restored.hazardLights = previousInputStates.hazardLights;
                eventVehicle.input.states = restored;
                eventVehicle.input.swapInputInReverse = previousSwapInput;
                eventVehicle.input.autoSetInput = previousAutoInput;
            }
            if (controls != null) controls.enabled = previousControlsEnabled;
            if (eventTransmission != null) eventTransmission.enabled = previousTransmissionEnabled;
            if (eventCameraChanger != null)
            {
                bool restoredCamera = SelectEventCamera(previousCamera);
                if (restoredCamera && eventVehicle != null) eventVehicle.CameraInsideVehicle = previousCameraInside;
                eventCameraChanger.enabled = previousCameraChangerEnabled;
            }
            previousCamera = exteriorCamera = null;
        }

        public bool TryHornPickup()
        {
            if (pickupReservationOwner != null)
            {
                if (!IsPickupTargetAvailable(reservedPickup, InteractionRadius) ||
                    reservedPickupCanBoard != null && !reservedPickupCanBoard()) return false;
                nearby = FindPickup(reservedPickup);
            }
            if (PickupInRange && !HasRoom) { Feedback = "NO ROOM FOR ANOTHER PASSENGER"; return false; }
            if (phase != Phase.Waiting || !CanInteract || nearby?.actor == null ||
                nearby.actor.GetComponent<TruckTaxiPassengerActor>().IsTumbling || controls==null ||
                !(controls.CurrentState.hornActive || controls.CurrentState.airHornActive)) return false;
            {
                active = nearby; nearby = null;
                int slot = companions.IndexOf(active);
                if (slot >= 0)
                    companions[slot] = new Companion { population = active.population, variant = active.variant,
                        availableAt = Time.time + 30, materializedBefore = true };
                // CompanionTask is a lease: pickups/services cover it without deleting the fare below it.
                var stops=FindObjectsByType<TruckTaxiStopObjectivePoint>(FindObjectsSortMode.None);
                active.destination=ClaimPrivateStop(truckBody.position,active.pickup,stops);
                phase = active.destination != null ? Phase.ToPrivateStop : Phase.AuxiliaryRide;
                active.actor.GetComponent<TruckTaxiPassengerActor>().DisarmWaitingTruckHit();
                if (phase == Phase.ToPrivateStop)
                {
                    host.Session.AcquireOfferSuppression(this);
                    offerLeaseHeld = true;
                }
                boarding=true;
                boardingRoutine=StartCoroutine(BoardCompanion(active));
                if(active==null) { boardingRoutine=null; return false; }
                if (active.destination != null) host.PickupZone?.SetPrivateStop(active.destination);
                Feedback = "Companion is boarding.";
                return true;
            }
        }

        private IEnumerator BoardCompanion(Companion companion)
        {
            var actor = companion.actor.GetComponent<TruckTaxiPassengerActor>();
            actor.SetLifecycle(TruckTaxiActorLifecycle.Boarding);
            if(cabRig==null || !cabRig.TryReserveSecondary(actor.transform,CompanionSeatType,
                TruckTaxiCabOccupantKind.Companion,PickupSnapshot(companion).Id))
            { boardingRoutine=null; Cancel(); Feedback="NO ROOM FOR ANOTHER PASSENGER"; yield break; }
            bool canWalk = TruckTaxiActorWorldPlacement.TryExit(actor, host.Player.transform, out var door) &&
                TruckTaxiActorWorldPlacement.ClearSegment(actor.GroundPosition, door, actor.StandingRadius,
                    actor.StandingHeight, host.Player.transform, actor.transform);
            float deadline=Time.time+4f;
            while(canWalk && companion?.actor!=null && Time.time<deadline)
            {
                if (host.Paused) { deadline += Time.deltaTime; yield return null; continue; }
                if(truckBody.linearVelocity.magnitude>MaximumSpeed*2f) { Cancel(); yield break; }
                Vector3 delta=Vector3.ProjectOnPlane(door-companion.actor.transform.position,Vector3.up);
                if(delta.sqrMagnitude>.02f)
                {
                    Vector3 next = Vector3.MoveTowards(actor.GroundPosition, door, 3f * Time.deltaTime);
                    if (!TruckTaxiActorWorldPlacement.ClearSegment(actor.GroundPosition, next, actor.StandingRadius,
                        actor.StandingHeight, host.Player.transform, actor.transform)) break;
                    companion.actor.transform.rotation=Quaternion.LookRotation(delta.normalized);
                    companion.actor.transform.position=next + Vector3.up * actor.StandingGroundOffset;
                }
                if(delta.sqrMagnitude<.4f) break;
                yield return null;
            }
            if(companion==active && companion.actor!=null)
            {
                actor.PrepareForSeat();
                yield return new WaitForSeconds(.12f);
                if (companion != active || actor == null) yield break;
                if (truckBody.linearVelocity.magnitude > MaximumSpeed * 2 || cabRig == null ||
                    !cabRig.TrySeatSecondary(actor.transform, CompanionSeatType, actor.StandingWorldScale))
                { boardingRoutine = null; Cancel(); Feedback = "NO ROOM FOR ANOTHER PASSENGER"; yield break; }
                actor.CompleteSeating();
                cabRig.SetOccupantIdentity(actor.transform,PickupSnapshot(companion).Id);
                boarding=false;
                boardingRoutine=null;
                Feedback=phase == Phase.AuxiliaryRide ? "Companion onboard. Fare destination unchanged." : "Park at the nearby private stop.";
                CompanionBoarded?.Invoke(PickupSnapshot(companion));
            }
        }

        public bool Eject()
        {
            if (!CanEject) return false;
            var departing=active;
            var actor=departing.actor.GetComponent<TruckTaxiPassengerActor>();
            motion?.StopMotion();
            EndSleeperSequence();
            if (cabRig != null) cabRig.Release(actor.transform);
            departing.actor=null;
            departing.availableAt = Time.time + 30;
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
            if (!Ready) { if (IsBusy || ownsCustomContent || sleeperHold) Cancel(); return; }
            if (IsBusy && active?.actor == null) { Cancel(); return; }
            if (phase != Phase.Waiting && (host.Session.State == TruckTaxiState.Inactive ||
                host.Roadside?.IsRecovering == true || host.Fuel?.IsRescuing == true))
            { Cancel(); return; }
            if (sleeperHold)
            {
                if (active?.actor == null || eventVehicle == null || !eventVehicle.isActiveAndEnabled ||
                    driverPresentation == null || !driverPresentation.isActiveAndEnabled ||
                    !driverPresentation.SleeperTransitionValid || !SelectEventCamera(exteriorCamera))
                { Cancel(); return; }
                HoldSleeperInputs();
            }
            if (host.Paused) return;
            if (Time.time >= nextPopulationRefresh)
            {
                nextPopulationRefresh = Time.time + 3;
                SpawnAtAuthoredStops();
            }
            MaterializeNearby();
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
                        RetireActor(companion);
                        companion.returnAt = 0;
                        companion.returnPending = false;
                        companion.availableAt = Time.time + 1f;
                    }
                    if (pickupReservationOwner != null && !MatchesPickup(companion, reservedPickup)) continue;
                    if (companion.actor == null || !companion.actor.activeSelf || Time.time < companion.availableAt) continue;
                    float distance = FlatDistance(truckBody.position, companion.actor.transform.position);
                    if (distance < best) { nearby = companion; best = distance; }
                }
                if(hornPressed) TryHornPickup();
                if(identifier!=null)
                {
                    identifier.gameObject.SetActive(nearby?.actor!=null && PickupInRange);
                    if(identifier.gameObject.activeSelf)
                    {
                        identifier.text=HasRoom ? "COMPANION  /  HONK TO PICK UP ("+HornBinding+")" : "NO ROOM FOR ANOTHER PASSENGER";
                        identifier.transform.position=nearby.actor.transform.position+Vector3.up*2.7f;
                        if(Camera.main!=null) identifier.transform.rotation=Quaternion.LookRotation(
                            identifier.transform.position-Camera.main.transform.position);
                    }
                }
                return;
            }
            if(identifier!=null) identifier.gameObject.SetActive(false);
            if (phase == Phase.AuxiliaryRide) return;
            if (phase == Phase.CompletingContent)
            {
                SyncContentMotion();
                if (host.CustomContent == null || !host.CustomContent.IsPlaying) Finish(true);
                return;
            }
            if (phase == Phase.ToPrivateStop)
            {
                if (active.destination == null)
                {
                    phase = Phase.AuxiliaryRide;
                    if (offerLeaseHeld) host.Session.ReleaseOfferSuppression(this);
                    offerLeaseHeld = false;
                    return;
                }
                // A higher-priority intent suspends this task; only completion/cancellation releases it.
                return;
            }
            if (truckBody.linearVelocity.magnitude > MaximumSpeed) { Cancel(); return; }
            if (phase == Phase.MovingToSleeper)
            {
                if (!driverPresentation.StepSleeperTransition(deltaTime)) return;
                if (!FrontSeatsVacated || !BeginEventPresentation()) { Cancel(); return; }
                phase = Phase.PrivateEvent;
                eventTime = 0;
                return;
            }
            eventTime += Mathf.Max(0, deltaTime);
            if (ownsCustomContent)
            {
                SyncContentMotion();
                if (eventTime >= nextContentBeat)
                {
                    host.CustomContent?.Beat();
                    nextContentBeat = eventTime + 1;
                }
            }
            else FadeAlpha = .65f * Mathf.Min(Mathf.Clamp01(eventTime / .6f),
                Mathf.Clamp01((EventSeconds - eventTime) / .8f));
            if (eventTime < EventSeconds) return;
            host.DriverNeeds.State.SatisfyThirst();
            Feedback = "THIRST SATISFIED";
            Finish(true);
        }

        public void Cancel()
        {
            if (ownsCustomContent) { host?.CustomContent?.Cancel(); ownsCustomContent = false; contentCompleting = false; }
            if (phase == Phase.Waiting) { EndSleeperSequence(); return; }
            Feedback = "Private stop cancelled.";
            Finish(false);
        }

        private void Finish(bool completed)
        {
            if (completed && ownsCustomContent && !contentCompleting && host?.CustomContent?.IsPlaying == true)
            {
                host.CustomContent.Complete();
                contentCompleting = true;
                phase = Phase.CompletingContent;
                return;
            }
            if (!completed && ownsCustomContent) host?.CustomContent?.Cancel();
            ownsCustomContent = false;
            contentCompleting = false;
            eventVehicleRenderers.Clear();
            if(boardingRoutine!=null) { StopCoroutine(boardingRoutine); boardingRoutine=null; }
            boarding=false;
            motion?.StopMotion();
            EndSleeperSequence();
            FadeAlpha = 0;
            if (active?.destination != null) host?.PickupZone?.SetPrivateStop(null);
            if (host?.GPS != null && active?.destination != null)
                host.GPS.ClearPrivateStopDestination(active.destination.stableId,privateRouteToken);
            if (host?.Session != null && offerLeaseHeld) host.Session.ReleaseOfferSuppression(this);
            offerLeaseHeld = false;
            if (active?.actor != null)
            {
                var actor = active.actor.GetComponent<TruckTaxiPassengerActor>();
                cabRig?.Release(actor.transform);
                actor.PrepareForWorld(host?.Player != null ? host.Player.transform : null);
                bool safeExit = host?.Player != null && TruckTaxiActorWorldPlacement.TryExit(actor, host.Player.transform, out var exit) &&
                    actor.ShowInWorld(exit, host.Player.transform);
                if (!safeExit) RetireActor(active);
                active.availableAt = Time.time + 30f;
                active.returnAt = Time.time + 30f;
                active.returnPending = true;
                if (!companions.Contains(active) && active.actor != null) departingCompanions.Add(active);
            }
            active = null;
            phase = Phase.Waiting;
            nearby = null;
        }

        private void OnEnable()
        {
            if(host==null || phase!=Phase.Waiting) return;
            foreach(var companion in companions)
                if(companion.actor!=null && companion.actor.activeInHierarchy)
                    companion.actor.GetComponent<TruckTaxiPassengerActor>().ArmWaitingTruckHit(truckBody);
        }

        private void OnDisable()
        {
            Cancel();
            pickupReservationOwner = null; reservedPickup = default; reservedPickupCanBoard = null;
            foreach(var companion in companions)
            {
                if(companion.impactRoutine!=null) { StopCoroutine(companion.impactRoutine); companion.impactRoutine=null; }
                if(companion.actor==null) continue;
                var actor=companion.actor.GetComponent<TruckTaxiPassengerActor>();
                if(actor.IsTumbling) actor.EndLightTumble(companion.actor.transform.position);
                actor.DisarmWaitingTruckHit();
            }
            foreach (var companion in departingCompanions)
                if (companion.actor != null) companion.actor.GetComponent<TruckTaxiPassengerActor>().DisarmWaitingTruckHit();
        }

        private void OnCompanionHit(TruckTaxiPassengerActor actor,Vector3 velocity)
        {
            if(actor==null || phase!=Phase.Waiting) return;
            foreach(var companion in companions)
            {
                if(companion.actor!=actor.gameObject || companion.impactRoutine!=null || Time.time<companion.nextImpactAt) continue;
                companion.nextImpactAt=Time.time+1.5f;
                companion.impactRoutine=StartCoroutine(TumbleCompanion(companion,actor,velocity));
                break;
            }
        }

        private IEnumerator TumbleCompanion(Companion companion,TruckTaxiPassengerActor actor,Vector3 velocity)
        {
            Vector3 standing=actor.transform.position;
            actor.BeginLightTumble(velocity);
            yield return new WaitForSeconds(.8f);
            if(actor!=null)
            {
                actor.EndLightTumble(standing);
                if(companion.actor.activeInHierarchy && phase==Phase.Waiting) actor.ArmWaitingTruckHit(truckBody);
            }
            companion.impactRoutine=null;
        }

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
            if (companion.actor == null ||
                FlatDistance(truckBody.position, companion.actor.transform.position) < 150f ||
                FlatDistance(truckBody.position, companion.population.Origin) < 30f) return false;
            var camera = Camera.main;
            return camera != null ? Offscreen(camera, companion.actor.transform.position) &&
                Offscreen(camera, companion.population.Origin) :
                FlatDistance(truckBody.position, companion.actor.transform.position) > 300f &&
                FlatDistance(truckBody.position, companion.population.Origin) > 300f;
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
            foreach (var companion in companions) if (companion.actor != null)
            {
                var actor=companion.actor.GetComponent<TruckTaxiPassengerActor>();
                if(actor!=null) actor.WaitingTruckHit-=OnCompanionHit;
                Destroy(companion.actor);
            }
            foreach (var material in materials) if (material != null) Destroy(material);
            foreach (var companion in departingCompanions) RetireActor(companion);
            departingCompanions.Clear();
            companions.Clear(); materials.Clear();
        }
    }
}
