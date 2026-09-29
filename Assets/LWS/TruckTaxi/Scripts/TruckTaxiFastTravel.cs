using System;
using System.Collections;
using LWS.InterstateHauler;
using NWH.VehiclePhysics2.Modules.Trailer;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // Only coordinates destination readiness and the existing connected-rig recovery operation.
    [DisallowMultipleComponent]
    public sealed class TruckTaxiFastTravel : MonoBehaviour
    {
        private TruckTaxiBootstrap host;
        private TruckTaxiRecoverySpace.BodySnapshot tractorBefore, trailerBefore;
        private Coroutine travelRoutine;
        private Preparation preparation;
        private TruckTaxiSession travelSession;
        private bool wasPaused, committed;
        private sealed class Preparation { public bool Done; }
        public Func<string> AdditionalBlockReason;
        public bool IsTravelling { get; private set; }
        public string Feedback { get; private set; } = "";
        public int CompletedTravels { get; private set; }
        public void Initialize(TruckTaxiBootstrap owner) => host = owner;
        public string BlockReason => host?.Ready != true ? "World is not ready." :
            IsTravelling || preparation != null && !preparation.Done ||
            host.Roadside?.IsRecovering == true || host.Fuel?.IsRescuing == true ? "Recovery already active." :
            host.Companions?.IsBusy == true ? "Finish or end the companion visit before fast travel." :
            !CanTravelSession(host.Session.State) ? "Finish the current ride before fast travel." : AdditionalBlockReason?.Invoke();
        public static bool CanTravelSession(TruckTaxiState state) => state == TruckTaxiState.Available;
        public bool TryTravel(Vector3 mapPosition, string label)
        {
            string blocked = BlockReason;
            if (!string.IsNullOrEmpty(blocked)) return Reject(blocked);
            if (!isActiveAndEnabled || host.Player == null || host.Player.UprightRecoveryController == null ||
                host.Player.GetComponent<Rigidbody>() == null) return Reject("Truck recovery is unavailable.");
            var hitch = host.Player.GetComponentInChildren<TrailerHitchModuleWrapper>(true);
            if (hitch != null && hitch.module.attached &&
                hitch.module.attachedTrailerModule?.vehicleController?.vehicleRigidbody == null)
                return Reject("The attached trailer could not be resolved for recovery.");
            if (hitch != null && hitch.module.attached &&
                (hitch.module.attachmentPoint == null || hitch.module.attachedTrailerModule.attachmentPoint == null))
                return Reject("The attached trailer recovery anchors are unavailable.");
            if (!LwsRoadGraphQuery.TryFindNearestRoad(host.roadGraph.Graph, mapPosition, 1200, out var road))
                return Reject("No authored drivable road near that point.");
            travelRoutine = StartCoroutine(Travel(road, label)); return true;
        }
        private bool Reject(string reason)
        { Feedback = reason; host?.hud?.Alerts?.Publish("travel-rejected", reason, TruckTaxiAlertPriority.High); return false; }
        private IEnumerator Travel(LwsRoadLookupResult road, string label)
        {
            var tractor = host.Player.GetComponent<Rigidbody>();
            var hitch = host.Player.GetComponentInChildren<TrailerHitchModuleWrapper>(true);
            var trailer = hitch != null && hitch.module.attached ? hitch.module.attachedTrailerModule?.vehicleController?.vehicleRigidbody : null;
            tractorBefore = new TruckTaxiRecoverySpace.BodySnapshot(tractor);
            trailerBefore = new TruckTaxiRecoverySpace.BodySnapshot(trailer);
            wasPaused = host.Paused; committed = false; IsTravelling = true;
            travelSession = host.Session;
            travelSession.AcquireOfferSuppression(this);
            try
            {
                host.SetPaused(true);
                Feedback = "PREPARING " + label;
                host.hud.Alerts.Publish("travel-start", Feedback);
                var regional = host.GetComponent<TruckTaxiRegionalWorld>();
                if (regional != null)
                {
                    // Finish on the streaming owner even if this component is disabled.
                    // PrepareInitialWorld retains the source player region while loading.
                    preparation = new Preparation();
                    regional.StartCoroutine(Prepare(regional, road.NearestPosition, preparation));
                    while (!preparation.Done) yield return null;
                    if (!regional.IsPositionAvailable(road.NearestPosition)) { Reject("Destination region did not load. Truck was not moved."); yield break; }
                }
                Physics.SyncTransforms();
                Vector3 forward = Vector3.ProjectOnPlane(road.Forward, Vector3.up).normalized;
                if (forward.sqrMagnitude < .1f) forward = Vector3.ProjectOnPlane(tractor.transform.forward, Vector3.up).normalized;
                if (forward.sqrMagnitude < .1f) forward = Vector3.forward;
                Quaternion facing = Quaternion.LookRotation(forward, Vector3.up);
                if (!CanTravelSession(host.Session.State))
                { Reject("Taxi state changed while loading. Truck was not moved."); yield break; }
                var currentTrailer = hitch != null && hitch.module.attached ?
                    hitch.module.attachedTrailerModule?.vehicleController?.vehicleRigidbody : null;
                if (currentTrailer != trailer)
                { Reject("Trailer coupling changed while loading. Truck was not moved."); yield break; }
                if (!FindClearGround(road.NearestPosition, forward, facing, tractor, trailer,
                    out var destination, out var trailerDestination, out var trailerFacing))
                { Reject("No clear grounded recovery space. Choose another road point."); yield break; }
                TruckTaxiRecoverySpace.Move(trailer, trailerDestination, trailerFacing);
                TruckTaxiRecoverySpace.Move(tractor, destination, facing);
                Physics.SyncTransforms();
                if (!host.Player.UprightRecoveryController.RequestResetUpright("Sandbox fast travel"))
                { Reject("Existing upright recovery rejected the move."); yield break; }
                Physics.SyncTransforms();
                if (!TruckTaxiRecoverySpace.HasClearance(tractor, tractor.position, tractor.rotation, tractor, trailer) ||
                    trailer != null && !TruckTaxiRecoverySpace.HasClearance(trailer, trailer.position, trailer.rotation, tractor, trailer))
                { Reject("Upright recovery did not leave clear space for the whole rig. Truck restored."); yield break; }
                committed = true; CompletedTravels++;
                host.DriverNeeds?.State?.CancelJug();
                host.Session.CancelTemporaryActionsForRecovery();
                host.Session.DiscardTeleportDistance();
                // FailRide still pays a cancellation fare for an onboard passenger; fare-bearing states are blocked.
                Feedback = "FAST TRAVEL: " + label;
                host.hud.Alerts.Publish("travel-complete:" + CompletedTravels, Feedback);
                yield return null;
            }
            finally { FinishTravel(); }
        }
        private static IEnumerator Prepare(TruckTaxiRegionalWorld regional, Vector3 point, Preparation state)
        {
            try { yield return regional.PrepareInitialWorld(point); }
            finally { state.Done = true; }
        }
        public void CancelTravel()
        {
            if (!IsTravelling) return;
            var running = travelRoutine;
            travelRoutine = null;
            if (running != null) StopCoroutine(running);
            FinishTravel();
        }
        private void FinishTravel()
        {
            if (!IsTravelling) return;
            if (!committed) { tractorBefore.Restore(); trailerBefore.Restore(); }
            Physics.SyncTransforms();
            travelSession?.DiscardTeleportDistance();
            travelSession?.ReleaseOfferSuppression(this);
            travelSession = null;
            IsTravelling = false; travelRoutine = null;
            if (host != null) host.SetPaused(wasPaused);
        }
        private void OnDisable() => CancelTravel();
        private void OnDestroy() => CancelTravel();

        private bool FindClearGround(Vector3 point, Vector3 forward, Quaternion rotation, Rigidbody tractor, Rigidbody trailer,
            out Vector3 position, out Vector3 trailerPosition, out Quaternion trailerRotation)
        {
            position = trailerPosition = default; trailerRotation = Quaternion.identity;
            foreach (float along in new[] { 0f, 15f, -15f, 30f, -30f, 50f, -50f })
            {
                Vector3 probe = point + forward * along;
                if (!TruckTaxiRecoverySpace.TryGroundedPose(tractor, probe, rotation,
                    host.Player.UprightRecoveryController.ResetHeightOffset, tractor, trailer, out position)) continue;
                if (trailer != null)
                {
                    CandidateTrailerPose(tractor.position, tractor.rotation, trailer.position, trailer.rotation,
                        position, rotation, out trailerPosition, out trailerRotation);
                    var hitch = host.Player.GetComponentInChildren<TrailerHitchModuleWrapper>(true);
                    var trailerModule = hitch != null ? hitch.module.attachedTrailerModule : null;
                    if (hitch != null && hitch.module.attachmentPoint != null && trailerModule?.attachmentPoint != null)
                    {
                        trailerRotation = rotation;
                        trailerPosition = position + rotation * Quaternion.Inverse(tractor.rotation) *
                            (hitch.module.attachmentPoint.position - tractor.position) -
                            trailerRotation * Quaternion.Inverse(trailer.rotation) *
                            (trailerModule.attachmentPoint.position - trailer.position);
                    }
                    if (!TruckTaxiRecoverySpace.TryGroundedPose(trailer,
                        trailerPosition - Vector3.up * host.Player.UprightRecoveryController.ResetHeightOffset,
                        trailerRotation, 1.2f, tractor, trailer, out var supportedTrailer)) continue;
                    float lift = Mathf.Max(0, supportedTrailer.y - trailerPosition.y);
                    if (lift > 4) continue;
                    position += Vector3.up * lift; trailerPosition += Vector3.up * lift;
                    if (!TruckTaxiRecoverySpace.HasClearance(trailer, trailerPosition, trailerRotation, tractor, trailer) ||
                        !TruckTaxiRecoverySpace.HasClearance(tractor, position, rotation, tractor, trailer)) continue;
                }
                return true;
            }
            return false;
        }
        public static void CandidateTrailerPose(Vector3 tractorFrom, Quaternion tractorRotationFrom,
            Vector3 trailerFrom, Quaternion trailerRotationFrom, Vector3 tractorTo, Quaternion tractorRotationTo,
            out Vector3 trailerTo, out Quaternion trailerRotationTo)
        {
            Quaternion turn = tractorRotationTo * Quaternion.Inverse(tractorRotationFrom);
            trailerTo = tractorTo + turn * (trailerFrom - tractorFrom);
            trailerRotationTo = LwsTruckUprightRecoveryController.CreateUprightYawRotation(turn * trailerRotationFrom);
        }
    }

    // Placement queries only; vehicle motion remains owned by NWH/UTS.
    public static class TruckTaxiRecoverySpace
    {
        public readonly struct BodySnapshot
        {
            private readonly Rigidbody body;
            private readonly Vector3 position, velocity, angularVelocity;
            private readonly Quaternion rotation;
            public BodySnapshot(Rigidbody body)
            {
                this.body = body;
                position = body != null ? body.position : default;
                rotation = body != null ? body.rotation : Quaternion.identity;
                velocity = body != null ? body.linearVelocity : default;
                angularVelocity = body != null ? body.angularVelocity : default;
            }
            public void Restore()
            {
                if (body == null) return;
                Move(body, position, rotation);
                if (!body.isKinematic) { body.linearVelocity = velocity; body.angularVelocity = angularVelocity; }
            }
        }

        public static void Move(Rigidbody body, Vector3 position, Quaternion rotation)
        {
            if (body == null) return;
            if (!body.isKinematic) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
            body.position = position; body.rotation = rotation;
            body.transform.SetPositionAndRotation(position, rotation);
        }

        public static bool TryGroundedPose(Rigidbody body, Vector3 surface, Quaternion rotation, float minimumLift,
            Rigidbody ignoreA, Rigidbody ignoreB, out Vector3 position)
        {
            position = surface;
            if (!LocalBounds(body, out var bounds)) return false;
            float low = float.PositiveInfinity, high = float.NegativeInfinity;
            for (int i = 0; i < 5; i++)
            {
                Vector3 offset = i == 4 ? bounds.center : new Vector3(
                    (i & 1) == 0 ? bounds.min.x : bounds.max.x, 0,
                    (i & 2) == 0 ? bounds.min.z : bounds.max.z);
                offset.y = 0;
                if (!Ground(surface + rotation * offset, body, ignoreA, ignoreB, out float height)) return false;
                low = Mathf.Min(low, height); high = Mathf.Max(high, height);
            }
            if (high - low > 4f) return false;
            position.y = high + Mathf.Max(minimumLift, .3f - bounds.min.y);
            float settlingDrop = Mathf.Max(0, position.y + bounds.min.y - high - .3f);
            return HasClearance(body, position, rotation, ignoreA, ignoreB, settlingDrop);
        }

        private static bool Ground(Vector3 point, Rigidbody body, Rigidbody ignoreA, Rigidbody ignoreB, out float height)
        {
            height = 0;
            float nearest = float.PositiveInfinity;
            foreach (var hit in Physics.RaycastAll(point + Vector3.up * 20, Vector3.down, 40, ~0, QueryTriggerInteraction.Ignore))
            {
                if (Owned(hit.collider, body) || Owned(hit.collider, ignoreA) || Owned(hit.collider, ignoreB) ||
                    hit.collider.attachedRigidbody != null || hit.normal.y < .7f) continue;
                float delta = Mathf.Abs(hit.point.y - point.y);
                if (delta > 8 || delta >= nearest) continue;
                nearest = delta; height = hit.point.y;
            }
            return !float.IsPositiveInfinity(nearest);
        }

        public static bool HasClearance(Rigidbody body, Vector3 position, Quaternion rotation, Rigidbody ignoreA, Rigidbody ignoreB,
            float settlingDrop = 0)
        {
            if (!LocalBounds(body, out var bounds)) return false;
            Vector3 settling = Vector3.up * Mathf.Max(0, settlingDrop) * .5f;
            foreach (var hit in Physics.OverlapBox(position + rotation * bounds.center - settling,
                bounds.extents + Vector3.one * .1f + settling, rotation, ~0, QueryTriggerInteraction.Ignore))
                if (!Owned(hit, body) && !Owned(hit, ignoreA) && !Owned(hit, ignoreB)) return false;
            return true;
        }

        private static bool Owned(Collider collider, Rigidbody body) => body != null &&
            (collider.attachedRigidbody == body || collider.transform.IsChildOf(body.transform));

        private static bool LocalBounds(Rigidbody body, out Bounds bounds)
        {
            bounds = default;
            if (body == null) return false;
            bool found = false;
            Quaternion inverse = Quaternion.Inverse(body.rotation);
            foreach (var collider in body.GetComponentsInChildren<Collider>())
            {
                if (!collider.enabled || !collider.gameObject.activeInHierarchy || collider.isTrigger ||
                    collider is WheelCollider || collider.attachedRigidbody != body) continue;
                Bounds source;
                bool local = true;
                if (collider is BoxCollider box) source = new Bounds(box.center, box.size);
                else if (collider is MeshCollider mesh && mesh.sharedMesh != null) source = mesh.sharedMesh.bounds;
                else { source = collider.bounds; local = false; }
                for (int i = 0; i < 8; i++)
                {
                    Vector3 point = new Vector3((i & 1) == 0 ? source.min.x : source.max.x,
                        (i & 2) == 0 ? source.min.y : source.max.y, (i & 4) == 0 ? source.min.z : source.max.z);
                    if (local) point = collider.transform.TransformPoint(point);
                    point = inverse * (point - body.position);
                    if (!found) { bounds = new Bounds(point, Vector3.zero); found = true; }
                    else bounds.Encapsulate(point);
                }
            }
            return found && bounds.size.sqrMagnitude > .01f;
        }
    }
}
