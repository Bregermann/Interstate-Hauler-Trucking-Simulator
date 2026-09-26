#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // Explicit fixture. Caller owns the live, boarded ride.
    public sealed class TruckTaxiVehicleObjectivesProbe : MonoBehaviour
    {
        private TruckTaxiSession retainedSession;
        private PassengerRequestDefinition retainedRam, retainedCollect;
        private bool released;

        public static IEnumerator Run(TruckTaxiBootstrap host, Action<bool, string> check)
        {
            if (check == null) throw new ArgumentNullException(nameof(check));
            if (host == null || !host.Ready || host.Paused || host.Session?.State != TruckTaxiState.DrivingToDestination ||
                host.VehicleObjectives == null || host.Player == null || host.traffic == null || host.GPS?.MapMarkers == null)
            {
                check(false, "Vehicle fixture requires an unpaused, boarded ride and initialized adapters");
                yield break;
            }
            var session = host.Session;
            if (session.Requests.Count > 6 || session.Requests.Any(r => r.State == TaxiRequestState.Active))
            {
                check(false, "Vehicle fixture requires two free request slots and no active objectives");
                yield break;
            }
            var body = host.Player.GetComponent<Rigidbody>();
            var observer = host.Player.GetComponent<TruckTaxiCollisionObserver>();
            var markers = host.GPS.MapMarkers;
            if (body == null || observer == null || !body.detectCollisions || Time.timeScale <= 0 ||
                !host.Player.GetComponentsInChildren<Collider>().Any(c => c.enabled && !c.isTrigger && c.attachedRigidbody == body) ||
                markers.Registry?.Find(TruckTaxiMapMarkerType.TargetVehicle)?.icon == null ||
                markers.Registry.Find(TruckTaxiMapMarkerType.Collectible)?.icon == null)
            {
                check(false, "Tractor physics, collision observer, or mission POI icons unavailable");
                yield break;
            }
            Vector3 oldPosition = body.position, oldVelocity = body.linearVelocity, oldAngular = body.angularVelocity;
            Quaternion oldRotation = body.rotation;
            bool oldKinematic = body.isKinematic, oldCollisions = body.detectCollisions;
            var ram = ScriptableObject.CreateInstance<PassengerRequestDefinition>();
            var collect = ScriptableObject.CreateInstance<PassengerRequestDefinition>();
            var assets = host.gameObject.AddComponent<TruckTaxiVehicleObjectivesProbe>();
            assets.Initialize(session, ram, collect);
            TaxiRequestProgress active = null;
            int ramEvents = 0;
            Action<TaxiEventType> onEvent = type => { if (type == TaxiEventType.TrafficRam) ramEvents++; };
            session.DrivingEvent += onEvent;
            try
            {
                Configure(ram, TaxiRequestType.RamTargetVehicle, "probe.ram", "Ram assigned traffic");
                Configure(collect, TaxiRequestType.CollectDroppedObjects, "probe.collect", "Collect dropped object");
                if (!TryTarget(host, out string id, out var vehicle))
                {
                    check(false, "No eligible UTS traffic vehicle is active");
                    yield break;
                }
                ram.targetId = id;
                bool assigned = session.GenerateRequest(ram);
                active = assigned ? session.Requests[session.Requests.Count - 1] : null;
                check(active?.State == TaxiRequestState.Active, "Live session assigned exact UTS target");
                if (active == null || active.State != TaxiRequestState.Active) yield break;
                yield return null;
                var marker = markers.Markers.FirstOrDefault(m => m != null &&
                    m.markerType == TruckTaxiMapMarkerType.TargetVehicle && m.source == vehicle.transform &&
                    m.state == TruckTaxiMapMarkerState.Active);
                check(marker != null && markers.RenderedPoi(marker) != null,
                    "Exact target has active rendered Compass POI");
                int hitsBefore = session.TrafficHits;
                for (int attempt = 0; attempt < 3 && active.State == TaxiRequestState.Active; attempt++)
                {
                    yield return new WaitForSeconds(Mathf.Max(0, host.Configuration.collisionCooldown) + .1f);
                    if (!host.traffic.TryResolveVehicle(id, out var current) || current != vehicle) break;
                    StageBehind(body, vehicle);
                    yield return new WaitForFixedUpdate();
                    float deadline = Time.realtimeSinceStartup + 2.5f;
                    while (active.State == TaxiRequestState.Active && Time.realtimeSinceStartup < deadline)
                    {
                        body.linearVelocity = vehicle.transform.forward * Mathf.Max(12, host.Configuration.minimumImpactSpeed + 7);
                        yield return new WaitForFixedUpdate();
                    }
                    Stop(body);
                }
                check(active.State == TaxiRequestState.Succeeded && observer.LastTarget == id &&
                    observer.LastImpactSpeed >= host.Configuration.minimumImpactSpeed &&
                    session.TrafficHits > hitsBefore && ramEvents > 0,
                    "Real tractor contact produced exact-target TrafficRam and completed mission");
                if (active.State == TaxiRequestState.Active) session.FailObjective(active, "Probe ram timed out.");
                active = null;
                yield return null;
                check(marker == null || !markers.Markers.Contains(marker), "Resolved target POI cleaned up");
                if (!TryTarget(host, out id, out vehicle))
                {
                    check(false, "No eligible UTS vehicle remains for collection");
                    yield break;
                }
                // Keep the tractor outside the new trigger until the fixture deliberately drives into it.
                SetPose(body, vehicle.transform.position + vehicle.transform.right * 25 + Vector3.up,
                    Quaternion.LookRotation(-vehicle.transform.right, Vector3.up));
                collect.targetId = id;
                assigned = session.GenerateRequest(collect);
                active = assigned ? session.Requests[session.Requests.Count - 1] : null;
                check(active?.State == TaxiRequestState.Active, "Live session assigned dropped-object mission");
                if (active == null || active.State != TaxiRequestState.Active) yield break;
                yield return null;
                var drop = UnityEngine.Object.FindObjectsByType<TruckTaxiDroppedCollectible>(FindObjectsSortMode.None)
                    .FirstOrDefault(d => d != null && !d.Collected && d.transform.parent != null &&
                        markers.Markers.Any(m => m != null && m.markerType == TruckTaxiMapMarkerType.Collectible &&
                            m.source == d.transform.parent));
                var dropMarker = drop != null ? markers.Markers.FirstOrDefault(m => m != null &&
                    m.markerType == TruckTaxiMapMarkerType.Collectible && m.source == drop.transform.parent) : null;
                check(drop != null && dropMarker != null && markers.RenderedPoi(dropMarker) != null,
                    "Spawned physical drop has rendered Compass POI");
                if (drop == null) yield break;
                for (int attempt = 0; attempt < 3 && active.State == TaxiRequestState.Active && drop != null; attempt++)
                {
                    Vector3 direction = Vector3.ProjectOnPlane(drop.transform.position - body.position, Vector3.up).normalized;
                    if (direction.sqrMagnitude < .1f)
                        direction = Vector3.ProjectOnPlane(vehicle.transform.forward, Vector3.up).normalized;
                    if (direction.sqrMagnitude < .1f) direction = Vector3.forward;
                    SetPose(body, drop.transform.position - direction * 8 + Vector3.up * 1.3f,
                        Quaternion.LookRotation(direction, Vector3.up));
                    yield return new WaitForFixedUpdate();
                    float deadline = Time.realtimeSinceStartup + 2;
                    while (active.State == TaxiRequestState.Active && drop != null && Time.realtimeSinceStartup < deadline)
                    {
                        direction = Vector3.ProjectOnPlane(drop.transform.position - body.position, Vector3.up).normalized;
                        if (direction.sqrMagnitude > .1f) body.linearVelocity = direction * 9;
                        yield return new WaitForFixedUpdate();
                    }
                    Stop(body);
                }
                check(active.State == TaxiRequestState.Succeeded && active.Progress >= active.Target,
                    "Tractor collider entered physical drop trigger and completed collection");
                if (active.State == TaxiRequestState.Active) session.FailObjective(active, "Probe collection timed out.");
                active = null;
                yield return null;
                check(dropMarker == null || !markers.Markers.Contains(dropMarker), "Collected POI cleaned up");
            }
            finally
            {
                session.DrivingEvent -= onEvent;
                if (active != null && active.State == TaxiRequestState.Active)
                    session.FailObjective(active, "Vehicle objective probe interrupted.");
                body.isKinematic = true;
                body.detectCollisions = oldCollisions;
                body.position = oldPosition; body.rotation = oldRotation;
                body.transform.SetPositionAndRotation(oldPosition, oldRotation);
                Physics.SyncTransforms();
                body.isKinematic = oldKinematic;
                if (!oldKinematic) { body.linearVelocity = oldVelocity; body.angularVelocity = oldAngular; }
                session.DiscardTeleportDistance();
                // The session retains definitions until its request list is cleared.
                assets.ReleaseWhenUnused();
                // Input and UTS AI were never modified.
            }
        }

        private static void Configure(PassengerRequestDefinition definition, TaxiRequestType type, string id, string text)
        {
            definition.requestType = type;
            definition.objectiveId = id;
            definition.description = text;
            definition.target = 1;
            definition.timer = 120;
        }

        private static bool TryTarget(TruckTaxiBootstrap host, out string id, out GameObject vehicle)
        {
            id = null; vehicle = null;
            float nearest = float.MaxValue;
            foreach (var pair in host.traffic.ActiveVehicles)
            {
                var candidate = pair.Value;
                if (candidate == null || !candidate.activeInHierarchy ||
                    candidate.GetComponent<TruckTaxiImpactTarget>()?.targetId != pair.Key ||
                    candidate.GetComponent<Rigidbody>() == null ||
                    !candidate.GetComponentsInChildren<Collider>().Any(c => c.enabled && !c.isTrigger) ||
                    !host.traffic.TryGetVehicleAi(pair.Key, out var ai) ||
                    !(ai is Behaviour behaviour) || !behaviour.isActiveAndEnabled) continue;
                float distance = (candidate.transform.position - host.Player.transform.position).sqrMagnitude;
                if (distance >= nearest) continue;
                nearest = distance; id = pair.Key; vehicle = candidate;
            }
            return vehicle != null;
        }

        private static void StageBehind(Rigidbody tractor, GameObject vehicle)
        {
            Vector3 forward = Vector3.ProjectOnPlane(vehicle.transform.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < .1f) forward = Vector3.forward;
            var rotation = Quaternion.LookRotation(forward, Vector3.up);
            SetPose(tractor, vehicle.transform.position - forward * 25 + Vector3.up, rotation);
            float front = 0, rear = 0;
            foreach (var collider in tractor.GetComponentsInChildren<Collider>())
                if (collider.enabled && !collider.isTrigger && collider.attachedRigidbody == tractor)
                    front = Mathf.Max(front, Vector3.Dot(collider.bounds.center - tractor.position, forward) +
                        Vector3.Dot(collider.bounds.extents, Abs(forward)));
            foreach (var collider in vehicle.GetComponentsInChildren<Collider>())
                if (collider.enabled && !collider.isTrigger)
                    rear = Mathf.Min(rear, Vector3.Dot(collider.bounds.center - vehicle.transform.position, forward) -
                        Vector3.Dot(collider.bounds.extents, Abs(forward)));
            SetPose(tractor, vehicle.transform.position + forward * (rear - front - 5) + Vector3.up, rotation);
        }

        private static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
        private static void Stop(Rigidbody body) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
        private static void SetPose(Rigidbody body, Vector3 position, Quaternion rotation)
        {
            body.isKinematic = true;
            body.position = position; body.rotation = rotation;
            body.transform.SetPositionAndRotation(position, rotation);
            Physics.SyncTransforms();
            body.isKinematic = false;
            Stop(body);
        }

        public void Initialize(TruckTaxiSession owner, PassengerRequestDefinition ramDefinition,
            PassengerRequestDefinition collectDefinition)
        {
            retainedSession = owner;
            retainedRam = ramDefinition;
            retainedCollect = collectDefinition;
        }

        public void ReleaseWhenUnused() { released = true; }

        private void Update()
        {
            if (released && (retainedSession == null || !retainedSession.Requests.Any(r =>
                ReferenceEquals(r.Definition, retainedRam) || ReferenceEquals(r.Definition, retainedCollect))))
                Destroy(this);
        }

        private void OnDestroy()
        {
            if (retainedRam != null) Destroy(retainedRam);
            if (retainedCollect != null) Destroy(retainedCollect);
        }
    }
}
#endif
