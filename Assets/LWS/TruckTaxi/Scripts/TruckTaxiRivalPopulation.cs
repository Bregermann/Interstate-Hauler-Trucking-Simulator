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

    // Rival fare state is deliberately local presentation state, never a TruckTaxiSession request.
    [DisallowMultipleComponent]
    public sealed class TruckTaxiRivalPopulation : MonoBehaviour
    {
        private enum RiderStage { None, Waiting, Boarding, Riding, Exiting }
        private sealed class Rival
        {
            public TruckTaxiTrafficPopulation.Car Car;
            public PassengerProfile VisualIdentity;
            public GameObject Rider;
            public RiderStage Stage;
            public float StageUntil;
            public float NextPickup;
            public Vector3 RiderFrom;
            public Vector3 RiderTo;
            public bool PickupQueued;
        }

        [Range(8, 16)] public int logicalTarget = 12;
        [Range(2, 5)] public int maximumLive = 3;
        [Min(40)] public float materializeRadius = 290;
        [Min(50)] public float releaseRadius = 410;
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
        public int LogicalCount => rivals.Count;
        public int LiveCount { get { int count = 0; foreach (var rival in rivals) if (rival.Car.Actor != null) count++; return count; } }
        public IReadOnlyList<TruckTaxiRivalDemandZone> DemandZones => zones;
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
                host.trafficPrefabs?.Length ?? 0, 700000);
            rivals.Clear();
            for (int i = 0; i < logical.Count; i++)
            {
                var car = logical.Cars[i];
                car.Id = "taxi.rival." + i;
                car.Personality = i % 8 == 0 ? TruckTaxiDriverPersonality.Reckless :
                    i % 3 == 0 ? TruckTaxiDriverPersonality.Aggressive : TruckTaxiDriverPersonality.Impatient;
                car.DesiredSpeed *= car.Personality == TruckTaxiDriverPersonality.Reckless ? 1.18f : 1.08f;
                rivals.Add(new Rival { Car = car, NextPickup = Time.time + 12 + i * 4 });
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
                else AdvanceRider(rival, dt);
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
            return Vector3.Distance(position, observer) - 100 * DemandAt(position);
        }

        private bool Materialize(Rival rival)
        {
            if (rival.Car.Actor != null || traffic == null || !traffic.TrySpawnDedicatedVehicle(rival.Car.Id,
                logical.Position(rival.Car), out var vehicle)) return false;
            rival.Car.Actor = vehicle;
            rival.Car.FullPhysics = true;
            var visual = vehicle.GetComponent<TruckTaxiRivalVehicleVisual>() ??
                vehicle.AddComponent<TruckTaxiRivalVehicleVisual>();
            visual.Apply(rival.Car.AppearanceSeed);
            rival.NextPickup = Mathf.Max(rival.NextPickup, Time.time + 5);
            return true;
        }

        private void Release(Rival rival)
        {
            var vehicle = rival.Car.Actor;
            if (vehicle == null) return;
            Vector3 position = vehicle.transform.position;
            var body = vehicle.GetComponent<TruckTaxiTrafficPooledActor>()?.Body;
            float speed = body != null ? body.linearVelocity.magnitude : rival.Car.Speed;
            ReturnRider(rival);
            if (traffic == null || !traffic.ReleaseDedicatedVehicle(rival.Car.Id)) return;
            rival.Car.Actor = null; rival.Car.FullPhysics = false;
            logical.RetargetUnmaterialized(rival.Car, position);
            logical.Capture(rival.Car, position, speed);
            rival.Stage = RiderStage.None;
            rival.PickupQueued = false;
            rival.NextPickup = Time.time + 12;
        }

        private void AdvanceRider(Rival rival, float dt)
        {
            var vehicle = rival.Car.Actor;
            if (vehicle == null) return;
            if (rival.Stage == RiderStage.None)
            {
                if (!rival.PickupQueued && Time.time < rival.NextPickup) return;
                var profile = SelectIdentity();
                if (profile == null) { rival.PickupQueued = false; rival.NextPickup = Time.time + 15; return; }
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
                    rival.Rider.SetActive(false);
                }
            }
            else if (rival.Stage == RiderStage.Riding && Time.time >= rival.StageUntil)
            {
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
                candidate == player || candidate == player?.pairPassenger || candidate == player?.companion)
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
            if (rival.Rider != null) { rival.Rider.SetActive(false); riderPool.Push(rival.Rider); rival.Rider = null; }
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
