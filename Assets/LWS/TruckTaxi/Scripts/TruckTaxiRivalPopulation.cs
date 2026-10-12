using System;
using System.Collections.Generic;
using LWS.InterstateHauler;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public readonly struct TruckTaxiRivalDemandZone
    {
        public readonly string Id;
        public readonly Vector3 Center;
        public readonly float Radius;
        public readonly float Strength;

        public TruckTaxiRivalDemandZone(string id, Vector3 center, float radius, float strength)
        {
            Id = id; Center = center; Radius = Mathf.Max(1, radius);
            Strength = Mathf.Clamp01(strength);
        }

        public float At(Vector3 point)
        {
            Vector3 offset = point - Center; offset.y = 0;
            return Strength * Mathf.Clamp01(1 - offset.magnitude / Radius);
        }
    }

    // UTS retains driving; Session owns transferable fares, never the primary ride request list.
    [DisallowMultipleComponent]
    public sealed class TruckTaxiRivalPopulation : MonoBehaviour
    {
        private enum RiderStage { None, Waiting, Boarding, Riding, Exiting }
        private sealed class Rival
        {
            public TruckTaxiTrafficPopulation.Car Car;
            public TruckTaxiRivalDepot Home;
            public float ShiftStart, ShiftEnd, PreferredVenueBias;
            public PassengerProfile VisualIdentity;
            public GameObject Rider;
            public RiderStage Stage;
            public float StageUntil;
            public float NextPickup;
            public float NextMaterialize;
            public Vector3 RiderFrom;
            public Vector3 RiderTo;
            public bool PickupQueued;
            public TruckTaxiRivalFare Fare;
            public TruckTaxiSession FareSession;
            public float AngryUntil;
        }

        [Range(8, 16)] public int logicalTarget = 12;
        [Range(2, 5)] public int maximumLive = 3;
        [Min(40)] public float materializeRadius = 290;
        [Min(50)] public float releaseRadius = 410;
        public TruckTaxiRivalDepot[] depots = Array.Empty<TruckTaxiRivalDepot>();
        private readonly List<Rival> rivals = new List<Rival>();
        private readonly List<Rival> candidates = new List<Rival>();
        private readonly List<TruckTaxiRivalDemandZone> zones = new List<TruckTaxiRivalDemandZone>();
        private readonly List<Vector3> demandSamples = new List<Vector3>();
        private readonly HashSet<string> usedVisualIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Stack<GameObject> riderPool = new Stack<GameObject>();
        private TruckTaxiTrafficPopulation logical;
        private TruckTaxiTrafficAdapter traffic;
        private Func<Vector3, float> demandSampler;
        private Material riderBody, riderClothes, riderSkin;
        private float nextMaintenance, nextConverge;
        private int pickupCursor;
        private TruckTaxiSession session;
        private TruckTaxiRivalTheft theft;
        public int LogicalCount => rivals.Count;
        public int LiveCount { get { int count = 0; foreach (var rival in rivals) if (rival.Car.Actor != null) count++; return count; } }
        public IReadOnlyList<TruckTaxiRivalDemandZone> DemandZones => zones;

        public void ConfigureTheft(TruckTaxiSession owner, TruckTaxiRivalTheft receiver)
        {
            if (session != owner)
                foreach (var rival in rivals)
                { ReturnRider(rival); rival.Stage = RiderStage.None; rival.PickupQueued = false; }
            session = owner; theft = receiver;
        }

        public bool TryGetOccupiedFare(string rivalId, int generation, out TruckTaxiRivalFare fare)
        {
            fare = null;
            var rival = rivals.Find(item => item.Car.Id == rivalId);
            if (rival?.Car.Actor == null || !rival.Car.Actor.activeInHierarchy || rival.Stage != RiderStage.Riding ||
                rival.Car.Actor.GetComponent<TruckTaxiTrafficPooledActor>()?.Generation != generation ||
                rival.Fare == null || session?.OwnsRivalFare(rivalId, rival.Fare.Id) != true) return false;
            fare = rival.Fare; return true;
        }

        public bool IsAngry(string rivalId) => rivals.Exists(item => item.Car.Id == rivalId && Time.time < item.AngryUntil);

        internal void OnRivalCollision(string rivalId, int generation, Collision collision)
        {
            if (theft == null || collision == null || collision.rigidbody != theft.PlayerBody ||
                !TryGetOccupiedFare(rivalId, generation, out _)) return;
            float normalSpeed = 0;
            for (int i = 0; i < collision.contactCount; i++)
                normalSpeed = Mathf.Max(normalSpeed, Mathf.Abs(Vector3.Dot(collision.relativeVelocity, collision.GetContact(i).normal)));
            var rival = rivals.Find(item => item.Car.Id == rivalId);
            Vector3 toward = (rival.Car.Actor.transform.position - theft.PlayerBody.position).normalized;
            float approach = Vector3.Dot(theft.PlayerBody.linearVelocity, toward);
            if (TruckTaxiRivalTheft.IsValidImpact(collision.relativeVelocity.magnitude, normalSpeed, approach,
                theft.minimumImpactSpeed)) theft.TrySteal(rivalId, generation);
        }

        internal bool CommitTheft(string rivalId, int generation, TruckTaxiRivalFare fare, Rigidbody offender)
        {
            var rival = rivals.Find(item => item.Car.Id == rivalId);
            if (rival?.Car.Actor == null || rival.Stage != RiderStage.Riding || rival.Fare != fare ||
                rival.Car.Actor.GetComponent<TruckTaxiTrafficPooledActor>()?.Generation != generation ||
                session?.StolenPassengerFare != fare) return false;
            ReturnRider(rival);
            rival.Stage = RiderStage.None; rival.PickupQueued = false;
            rival.NextPickup = Time.time + 35; rival.AngryUntil = Time.time + 12;
            rival.Car.Actor.GetComponent<TruckTaxiRivalVehicleVisual>()?.SetPassengerState(false, true);
            rival.Car.Actor.GetComponent<TruckTaxiTrafficBehaviour>()?.Honk(TruckTaxiHonkPattern.Angry, true);
            rival.Car.Actor.GetComponent<TruckTaxiRoadRage>()?.TryTrigger(offender);
            return true;
        }
        public bool TryGetHome(string rivalId, out string depotId, out string townId,
            out float shiftStart, out float shiftEnd, out float venueBias)
        {
            foreach (var rival in rivals)
                if (rival.Car.Id == rivalId)
                {
                    depotId = rival.Home != null ? rival.Home.depotId : null;
                    townId = rival.Home != null ? rival.Home.preferredTown : null;
                    shiftStart = rival.ShiftStart; shiftEnd = rival.ShiftEnd;
                    venueBias = rival.PreferredVenueBias;
                    return true;
                }
            depotId = townId = null;
            shiftStart = shiftEnd = venueBias = 0;
            return false;
        }
        public static int AllowedLiveCount(int requested, int dedicatedCapacity, int physicsCapacity) =>
            Mathf.Max(0, Mathf.Min(requested, dedicatedCapacity, physicsCapacity));

        public void Initialize(TruckTaxiTrafficAdapter host)
        {
            if (host == null) throw new ArgumentNullException(nameof(host));
            if (traffic == host && logical != null) return;
            if (traffic != null) ReleaseAll();
            traffic = host;
            var lanes = new List<LwsTrafficLaneDefinition>();
            demandSamples.Clear();
            if (host.cityLanes != null)
                foreach (var lane in host.cityLanes)
                    if (lane != null && lane.spawnEnabled && lane.centerline != null && lane.centerline.Length >= 5)
                    {
                        lanes.Add(lane);
                        for (int p = 2; p < lane.centerline.Length - 2; p += 8)
                            if (demandSamples.Count < 256) demandSamples.Add(lane.centerline[p]);
                    }
            logical = new TruckTaxiTrafficPopulation(lanes, Mathf.Clamp(logicalTarget, 8, 16),
                host.trafficPrefabs?.Length ?? 0, 700000, host.Tuning);
            rivals.Clear();
            for (int i = 0; i < logical.Count; i++)
            {
                var car = logical.Cars[i];
                car.Id = "taxi.rival." + i;
                car.Personality = host.Tuning.Choose(TruckTaxiTrafficPopulation.HashId(car.Id), true);
                car.DesiredSpeed *= car.Personality == TruckTaxiDriverPersonality.Reckless ? 1.12f : 1.03f;
                TruckTaxiRivalDepot home = depots != null && depots.Length > 0 ? depots[i % depots.Length] : null;
                rivals.Add(new Rival { Car = car, Home = home, NextPickup = Time.time + 12 + i * 4,
                    ShiftStart = home != null ? home.shiftStartHour : 6,
                    ShiftEnd = home != null ? home.shiftEndHour : 22,
                    PreferredVenueBias = home != null ? home.venueBias : .25f });
                if (home != null && i % 3 == 0)
                    logical.RetargetUnmaterialized(car, home.StagingPosition);
            }
            nextMaintenance = nextConverge = 0;
        }

        public void SetDemandZones(IReadOnlyList<TruckTaxiRivalDemandZone> snapshots)
        {
            zones.Clear();
            if (snapshots == null) return;
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < snapshots.Count; i++)
                if (!string.IsNullOrWhiteSpace(snapshots[i].Id) && ids.Add(snapshots[i].Id) &&
                    snapshots[i].Strength > 0) zones.Add(snapshots[i]);
        }

        public void SetDemandSampler(Func<Vector3, float> sampler) => demandSampler = sampler;

        public string DebugSpawn(Vector3 near)
        {
            if (traffic == null || !traffic.Ready || LiveCount >= LiveBudget) return null;
            Rival best = null;
            float distance = float.PositiveInfinity;
            foreach (var rival in rivals)
            {
                if (rival.Car.Actor != null) continue;
                float sqr = (logical.Position(rival.Car) - near).sqrMagnitude;
                if (sqr >= distance) continue;
                best = rival; distance = sqr;
            }
            if (best == null || !logical.RetargetUnmaterialized(best.Car, near)) return null;
            return Materialize(best) ? best.Car.Id : null;
        }

        public bool ForcePickup(string rivalId)
        {
            Rival rival = rivals.Find(item => item.Car.Id == rivalId);
            if (rival == null || rival.PickupQueued || rival.Stage != RiderStage.None) return false;
            rival.PickupQueued = true;
            return true;
        }

        // A visual-only kaiju proxy already exists; retire the real rival through its usual owners.
        public bool TryReleaseForKaiju(TruckTaxiTrafficAdapter owner, GameObject victim, int expectedGeneration)
            => TryReleaseForRecovery(owner, victim, expectedGeneration);

        public bool TryReleaseForRecovery(TruckTaxiTrafficAdapter owner, GameObject victim, int expectedGeneration)
        {
            if (owner == null || traffic != owner || logical == null || victim == null || !victim.activeInHierarchy ||
                victim.GetComponentInChildren<NWH.VehiclePhysics2.VehicleController>(true) != null) return false;
            var actor = victim.GetComponent<TruckTaxiTrafficPooledActor>();
            var mission = victim.GetComponent<TruckTaxiMissionTarget>();
            if (actor == null || actor.Generation != expectedGeneration || actor.MissionReserved ||
                (mission != null && !string.IsNullOrEmpty(mission.StableId))) return false;
            var rival = rivals.Find(item => item.Car.Actor == victim);
            if (rival == null || rival.Car.MissionReserved ||
                !traffic.TryResolveVehicle(rival.Car.Id, out var live) || live != victim) return false;
            if (!Release(rival)) return false;
            rival.StageUntil = 0;
            rival.NextMaterialize = Time.time + 30;
            rival.NextPickup = rival.NextMaterialize + 5;
            return true;
        }

        private int LiveBudget => traffic == null ? 0 :
            AllowedLiveCount(Mathf.Clamp(maximumLive, 2, 5), traffic.maximumDedicatedVehicles, traffic.maxFullTraffic);

        private float DemandAt(Vector3 position)
        {
            float demand = demandSampler != null ?
                TruckTaxiTrafficAdapter.VenuePressureFromMultiplier(demandSampler(position)) : 0;
            for (int i = 0; i < zones.Count; i++) demand = Mathf.Max(demand, zones[i].At(position));
            return demand;
        }

        private void Update()
        {
            if (traffic == null || logical == null) return;
            float dt = Mathf.Min(.5f, Time.deltaTime);
            foreach (var rival in rivals)
            {
                if (rival.Car.Actor == null) logical.Advance(rival.Car, dt);
                else
                {
                    AdvanceRider(rival, dt);
                    rival.Car.Actor.GetComponent<TruckTaxiRivalVehicleVisual>()?.SetPassengerState(
                        rival.Stage == RiderStage.Riding, Time.time < rival.AngryUntil);
                }
            }
            if (Time.time < nextMaintenance || !traffic.Ready) return;
            nextMaintenance = Time.time + .5f;
            Maintain();
        }

        private void Maintain()
        {
            Vector3 observer = Observer();
            var playerProfile = TruckTaxiBootstrap.Instance?.Session?.Passenger;
            for (int i = 0; i < rivals.Count; i++)
            {
                var rival = rivals[i];
                if (rival.VisualIdentity != null &&
                    !IsIdentityEligible(rival.VisualIdentity, playerProfile, null))
                {
                    ReturnRider(rival);
                    rival.Stage = RiderStage.None;
                    rival.PickupQueued = false;
                    rival.NextPickup = Time.time + 5;
                }
                if (rival.Car.Actor != null && !traffic.TryResolveVehicle(rival.Car.Id, out var live))
                {
                    Vector3 lastPosition = rival.Car.Actor.transform.position;
                    ReturnRider(rival);
                    rival.Car.Actor = null; rival.Stage = RiderStage.None;
                    rival.Car.FullPhysics = false;
                    logical.RetargetUnmaterialized(rival.Car, lastPosition);
                    rival.PickupQueued = false;
                }
            }
            if (Time.time >= nextConverge && (zones.Count > 0 || demandSampler != null))
            {
                nextConverge = Time.time + 8;
                Vector3 target = Vector3.zero;
                float strongest = .2f;
                for (int z = 0; z < zones.Count; z++)
                    if (zones[z].Strength > strongest) { strongest = zones[z].Strength; target = zones[z].Center; }
                if (demandSampler != null)
                    for (int sample = 0; sample < demandSamples.Count; sample++)
                    {
                        float strength = TruckTaxiTrafficAdapter.VenuePressureFromMultiplier(
                            demandSampler(demandSamples[sample]));
                        if (strength > strongest) { strongest = strength; target = demandSamples[sample]; }
                    }
                int moved = 0;
                for (int i = 0; strongest > .2f && i < rivals.Count && moved < 2; i++)
                {
                    var rival = rivals[(pickupCursor + i) % rivals.Count];
                    if (rival.Car.Actor != null || rival.Stage != RiderStage.None) continue;
                    Vector3 position = logical.Position(rival.Car);
                    if ((position - observer).sqrMagnitude < releaseRadius * releaseRadius) continue;
                    if (logical.RetargetUnmaterialized(rival.Car, target +
                        new Vector3((i % 3 - 1) * 22, 0, (i % 4 - 2) * 18))) moved++;
                }
                pickupCursor++;
            }
            candidates.Clear();
            foreach (var rival in rivals)
            {
                Vector3 position = rival.Car.Actor != null ? rival.Car.Actor.transform.position : logical.Position(rival.Car);
                float distance = Vector3.Distance(position, observer);
                if (rival.Car.Actor != null && (distance > releaseRadius || !traffic.RegionAvailable(position)))
                    Release(rival);
                else if (rival.Car.Actor == null && distance < materializeRadius &&
                    distance > 35 && traffic.RegionAvailable(position)) candidates.Add(rival);
            }
            candidates.Sort((a, b) => Score(a, observer).CompareTo(Score(b, observer)));
            for (int i = 0; i < candidates.Count && LiveCount < LiveBudget; i++) Materialize(candidates[i]);
        }

        private float Score(Rival rival, Vector3 observer)
        {
            Vector3 position = logical.Position(rival.Car);
            return Vector3.Distance(position, observer) - (100 + 40 * rival.PreferredVenueBias) * DemandAt(position);
        }

        private bool Materialize(Rival rival)
        {
            if (rival.Car.Actor != null || Time.time < rival.NextMaterialize || traffic == null || !traffic.TrySpawnDedicatedVehicle(rival.Car.Id,
                logical.Position(rival.Car), out var vehicle)) return false;
            rival.Car.Actor = vehicle;
            rival.Car.FullPhysics = true;
            var visual = vehicle.GetComponent<TruckTaxiRivalVehicleVisual>() ??
                vehicle.AddComponent<TruckTaxiRivalVehicleVisual>();
            visual.Apply(rival.Car.AppearanceSeed, int.Parse(rival.Car.Id.Substring("taxi.rival.".Length)) + 1);
            var relay = vehicle.GetComponent<TruckTaxiRivalCollisionRelay>() ?? vehicle.AddComponent<TruckTaxiRivalCollisionRelay>();
            relay.Initialize(this, rival.Car.Id, vehicle.GetComponent<TruckTaxiTrafficPooledActor>().Generation);
            rival.NextPickup = Mathf.Max(rival.NextPickup, Time.time + 5);
            return true;
        }

        private bool Release(Rival rival)
        {
            var vehicle = rival.Car.Actor;
            if (vehicle == null) return false;
            Vector3 position = vehicle.transform.position;
            var body = vehicle.GetComponent<TruckTaxiTrafficPooledActor>()?.Body;
            float speed = body != null ? body.linearVelocity.magnitude : rival.Car.Speed;
            if (traffic == null || !traffic.ReleaseDedicatedVehicle(rival.Car.Id)) return false;
            ReturnRider(rival);
            rival.Car.Actor = null; rival.Car.FullPhysics = false;
            logical.RetargetUnmaterialized(rival.Car, position);
            logical.Capture(rival.Car, position, speed);
            if (rival.Home != null && pickupCursor++ % 4 == 0)
                logical.RetargetUnmaterialized(rival.Car, rival.Home.StagingPosition);
            rival.Stage = RiderStage.None;
            rival.PickupQueued = false;
            rival.NextPickup = Time.time + 12;
            return true;
        }

        private void AdvanceRider(Rival rival, float dt)
        {
            var vehicle = rival.Car.Actor;
            if (vehicle == null) return;
            if (rival.Fare != null && session?.OwnsRivalFare(rival.Car.Id, rival.Fare.Id) != true)
            { ReturnRider(rival); rival.Stage = RiderStage.None; rival.NextPickup = Time.time + 12; }
            if (rival.Stage == RiderStage.None)
            {
                if (!rival.PickupQueued && Time.time < rival.NextPickup) return;
                var profile = SelectIdentity();
                if (profile == null) { rival.PickupQueued = false; rival.NextPickup = Time.time + 15; return; }
                if (session != null && !session.TryCreateRivalFare(rival.Car.Id, profile, vehicle.transform.position, out rival.Fare))
                { usedVisualIds.Remove(profile.passengerId); rival.PickupQueued = false; rival.NextPickup = Time.time + 15; return; }
                rival.FareSession = session;
                rival.VisualIdentity = profile;
                rival.PickupQueued = false;
                rival.Stage = RiderStage.Waiting;
                rival.StageUntil = Time.time + 2;
                rival.Rider = AcquireRider(profile);
                rival.RiderFrom = vehicle.transform.position + vehicle.transform.right * 3 + vehicle.transform.forward * 2;
                rival.RiderTo = vehicle.transform.position + vehicle.transform.right * 1.25f;
                rival.Rider.transform.position = rival.RiderFrom;
                vehicle.GetComponent<TruckTaxiTrafficBehaviour>()?.HoldForPresentation(5);
            }
            else if (rival.Stage == RiderStage.Waiting && Time.time >= rival.StageUntil)
            {
                rival.Stage = RiderStage.Boarding; rival.StageUntil = Time.time + 2;
                rival.RiderFrom = rival.Rider.transform.position;
                rival.RiderTo = vehicle.transform.position + vehicle.transform.right * 1.15f;
                vehicle.GetComponent<TruckTaxiTrafficBehaviour>()?.HoldForPresentation(3);
            }
            else if (rival.Stage == RiderStage.Boarding)
            {
                rival.Rider.transform.position = Vector3.Lerp(rival.RiderFrom, rival.RiderTo,
                    1 - Mathf.Clamp01((rival.StageUntil - Time.time) / 2));
                if (Time.time >= rival.StageUntil)
                {
                    rival.Stage = RiderStage.Riding;
                    rival.StageUntil = Time.time + 25 + rival.Car.AppearanceSeed % 20;
                    var mount = vehicle.GetComponent<TruckTaxiRivalVehicleVisual>()?.PassengerMount;
                    if (mount != null)
                    {
                        rival.Rider.transform.SetParent(mount, false);
                        rival.Rider.transform.localPosition = Vector3.zero;
                        rival.Rider.transform.localRotation = Quaternion.identity;
                        rival.Rider.transform.localScale = Vector3.one * .55f;
                    }
                    else rival.Rider.SetActive(false);
                }
            }
            else if (rival.Stage == RiderStage.Riding && Time.time >= rival.StageUntil &&
                (rival.Fare == null || Vector3.ProjectOnPlane(vehicle.transform.position - rival.Fare.DestinationPosition,
                    Vector3.up).sqrMagnitude <= rival.Fare.DestinationRadius * rival.Fare.DestinationRadius))
            {
                rival.Rider.transform.SetParent(transform, true);
                rival.Rider.transform.localScale = Vector3.one;
                rival.Stage = RiderStage.Exiting; rival.StageUntil = Time.time + 3;
                rival.RiderFrom = vehicle.transform.position + vehicle.transform.right * 1.1f;
                rival.RiderTo = rival.RiderFrom + vehicle.transform.right * 3;
                rival.Rider.transform.position = rival.RiderFrom;
                rival.Rider.SetActive(true);
                vehicle.GetComponent<TruckTaxiTrafficBehaviour>()?.HoldForPresentation(3);
            }
            else if (rival.Stage == RiderStage.Exiting)
            {
                rival.Rider.transform.position = Vector3.Lerp(rival.RiderFrom, rival.RiderTo,
                    1 - Mathf.Clamp01((rival.StageUntil - Time.time) / 3));
                if (Time.time >= rival.StageUntil)
                {
                    ReturnRider(rival);
                    rival.Stage = RiderStage.None;
                    rival.NextPickup = Time.time + 25;
                }
            }
        }

        private PassengerProfile SelectIdentity()
        {
            var configuration = TruckTaxiBootstrap.Instance?.Configuration;
            var roster = configuration?.passengerDatabase?.passengers ?? configuration?.passengers;
            if (roster == null || roster.Length == 0) return null;
            var session = TruckTaxiBootstrap.Instance?.Session;
            int start = pickupCursor++ % roster.Length;
            for (int attempt = 0; attempt < roster.Length; attempt++)
            {
                var profile = roster[(start + attempt) % roster.Length];
                if (IsIdentityEligible(profile, session?.Passenger, usedVisualIds))
                {
                    usedVisualIds.Add(profile.passengerId);
                    return profile;
                }
            }
            return null;
        }

        public static bool IsIdentityEligible(PassengerProfile candidate, PassengerProfile player,
            ISet<string> reserved)
        {
            if (candidate == null || !candidate.available || string.IsNullOrWhiteSpace(candidate.passengerId) ||
                candidate == player || candidate == player?.pairPassenger || candidate == player?.companion ||
                candidate.passengerId == player?.passengerId || candidate.passengerId == player?.pairPassenger?.passengerId ||
                candidate.passengerId == player?.companion?.passengerId)
                return false;
            return reserved == null || !reserved.Contains(candidate.passengerId);
        }

        private GameObject AcquireRider(PassengerProfile profile)
        {
            if (riderBody == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                riderBody = new Material(shader) { color = new Color(.25f, .6f, .64f), hideFlags = HideFlags.DontSave };
                riderClothes = new Material(shader) { color = new Color(.18f, .2f, .24f), hideFlags = HideFlags.DontSave };
                riderSkin = new Material(shader) { color = new Color(.74f, .53f, .38f), hideFlags = HideFlags.DontSave };
            }
            GameObject rider;
            if (riderPool.Count > 0) rider = riderPool.Pop();
            else
            {
                rider = new GameObject("Rival visual rider");
                rider.transform.SetParent(transform, false);
                TruckTaxiWobbleVisual.CreateThemed(rider.transform, new TruckTaxiWobbleVisual.Design {
                    silhouette = TruckTaxiWobbleVisual.Silhouette.Human,
                    headwear = TruckTaxiWobbleVisual.Headwear.Cap,
                    accessory = TruckTaxiWobbleVisual.Accessory.None,
                    height = 1.65f, width = 1, headScale = 1, upperBodyScale = 1,
                    clothedAdult = true, body = riderBody, clothes = riderClothes,
                    accent = riderBody, skin = riderSkin
                });
                foreach (var collider in rider.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            }
            rider.name = "Rival rider visual " + profile.passengerId;
            Color palette = profile.appearance != null ? profile.appearance.fallbackColor :
                new Color(.25f, .6f, .64f);
            var block = new MaterialPropertyBlock();
            foreach (var renderer in rider.GetComponentsInChildren<Renderer>(true))
                if (renderer.name == "Body" || renderer.name == "Clothed chest")
                {
                    renderer.GetPropertyBlock(block);
                    block.SetColor("_BaseColor", palette);
                    block.SetColor("_Color", palette);
                    renderer.SetPropertyBlock(block);
                }
            rider.SetActive(true);
            return rider;
        }

        private void ReturnRider(Rival rival)
        {
            rival.FareSession?.ReleaseRivalFare(rival.Car.Id, rival.Fare?.Id);
            rival.Fare = null; rival.FareSession = null;
            if (rival.Rider != null)
            {
                rival.Rider.SetActive(false); rival.Rider.transform.SetParent(transform, false);
                rival.Rider.transform.localScale = Vector3.one;
                riderPool.Push(rival.Rider); rival.Rider = null;
            }
            if (rival.VisualIdentity != null)
            {
                usedVisualIds.Remove(rival.VisualIdentity.passengerId);
                rival.VisualIdentity = null;
            }
        }

        private Vector3 Observer()
        {
            var player = TruckTaxiBootstrap.Instance?.Player;
            return player != null ? player.transform.position : traffic.transform.position;
        }

        private void ReleaseAll()
        {
            foreach (var rival in rivals) Release(rival);
        }

        private void OnDestroy()
        {
            ReleaseAll();
            if (riderBody != null) Destroy(riderBody);
            if (riderClothes != null) Destroy(riderClothes);
            if (riderSkin != null) Destroy(riderSkin);
        }
    }
}
