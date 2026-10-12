using UnityEngine;

namespace LWS.TruckTaxi
{
    // Coordinates existing seat, Session, GPS and rival owners. It never calculates or pays a fare.
    [DisallowMultipleComponent]
    public sealed class TruckTaxiRivalTheft : MonoBehaviour
    {
        [Min(1)] public float minimumImpactSpeed = 6;
        private TruckTaxiBootstrap host;
        private TruckTaxiSession session;
        private TruckTaxiRivalPopulation population;
        private TruckTaxiCabOccupantRig rig;
        private TruckTaxiRivalFare fare;
        private TruckTaxiPassengerActor actor;
        private long routeToken;
        private float stoppedSeconds;
        private bool subscribed, delivering;
        public Rigidbody PlayerBody { get; private set; }
        public string Feedback { get; private set; } = "";
        public TruckTaxiPassengerActor StolenActor => actor;
        public bool IsActive => fare != null && session?.StolenPassengerFare == fare;
        public bool IsLegitimateDiversion => IsActive && OwnRouteActive;
        public string RouteOwnerId => fare == null ? null : "rival-theft." + fare.Id;
        public long RouteLeaseToken => routeToken;
        private bool OwnRouteActive => host?.GPS != null && fare != null && routeToken != 0 &&
            host.GPS.ActiveLayer == TruckTaxiNavigationLayer.RivalPassenger &&
            host.GPS.TargetId == fare.DestinationId &&
            host.GPS.ActiveLeaseToken == routeToken;

        public bool Initialize(TruckTaxiBootstrap owner, TruckTaxiRivalPopulation rivals)
        {
            if (owner?.Session == null || owner.Player == null || owner.GPS == null || rivals == null) return false;
            var body = owner.Player.GetComponent<Rigidbody>();
            if (body == null) return false;
            if (host == owner && session == owner.Session && population == rivals) { Subscribe(); return true; }
            Cancel(); Unsubscribe();
            host = owner; session = owner.Session; population = rivals;
            PlayerBody = body;
            rig = owner.Player.GetComponent<TruckTaxiCabOccupantRig>();
            population.ConfigureTheft(session, this);
            Subscribe();
            return true;
        }

        public static bool IsValidImpact(float relativeSpeed, float normalSpeed, float playerApproachSpeed, float minimum = 6) =>
            float.IsFinite(relativeSpeed) && float.IsFinite(normalSpeed) && float.IsFinite(playerApproachSpeed) &&
            float.IsFinite(minimum) && minimum >= 1 && relativeSpeed >= minimum && normalSpeed >= minimum &&
            playerApproachSpeed >= minimum * .5f;

        internal bool TrySteal(string rivalId, int generation)
        {
            if (!isActiveAndEnabled || host == null || host.Paused || session == null || host.Session != session || PlayerBody == null ||
                population == null || fare != null || host.GPS == null ||
                host.GPS.ActiveLayer >= TruckTaxiNavigationLayer.RivalPassenger ||
                !population.TryGetOccupiedFare(rivalId, generation, out var candidate)) return false;
            if (rig == null ||
                !rig.HasSecondarySeat(candidate.Passenger.seatProfile != null ?
                    candidate.Passenger.seatProfile.seatType : TruckTaxiSeatType.StandardPassenger,
                    kind: TruckTaxiCabOccupantKind.StolenPassenger))
                return Reject("NO ROOM FOR ANOTHER PASSENGER");
            if (!session.CanStealRivalPassenger(rivalId, candidate.Id, true)) return false;

            // Reserve actual geometry before changing either fare. All failures unwind only these owned resources.
            fare = candidate;
            actor = TruckTaxiPassengerRuntime.Spawn(candidate.Passenger);
            actor.PrepareForSeat();
            var type = candidate.Passenger.seatProfile != null ? candidate.Passenger.seatProfile.seatType :
                TruckTaxiSeatType.StandardPassenger;
            if (!rig.TrySeatSecondary(actor.transform, type, actor.StandingWorldScale, TruckTaxiCabOccupantKind.StolenPassenger))
            { ReleasePresentation(false); return Reject("NO ROOM FOR ANOTHER PASSENGER"); }
            if (!host.GPS.TrySetNavigationIntent(TruckTaxiNavigationLayer.RivalPassenger, RouteOwnerId,
                candidate.DestinationId, candidate.DestinationName, candidate.DestinationPosition,
                TruckTaxiMapMarkerType.Destination, candidate.DestinationRadius, true))
            { ReleasePresentation(false); return Reject("PASSENGER TRANSFER BLOCKED: NO ROUTE"); }
            routeToken = host.GPS.ActiveLeaseToken;
            if (!OwnRouteActive || !session.TryStealRivalPassenger(rivalId, candidate.Id, true) ||
                !population.CommitTheft(rivalId, generation, candidate, PlayerBody))
            { Cancel(); return false; }
            actor.CompleteSeating();
            rig.SetOccupantIdentity(actor.transform,candidate.Id);
            Feedback = "PASSENGER STOLEN\nNEW STOP ADDED";
            stoppedSeconds = 0;
            return true;
        }

        private bool Reject(string reason)
        {
            Feedback = reason; session?.React(reason); return false;
        }

        // Parent calls once per gameplay frame, before Session.Tick; pause never advances delivery.
        public void Tick(float seconds)
        {
            if (!IsActive) { if (fare != null) ReleasePresentation(false); return; }
            if (host == null || host.Session != session || host.Player == null || PlayerBody == null || actor == null || rig == null ||
                !actor.transform.IsChildOf(host.Player.transform) || actor.Lifecycle != TruckTaxiActorLifecycle.Seated ||
                session.State == TruckTaxiState.Inactive)
            { Cancel(); return; }
            if (host.Paused || !float.IsFinite(seconds) || seconds <= 0) return;
            if (!OwnRouteActive)
            {
                stoppedSeconds = 0;
                if (!host.GPS.ActiveLayer.HasValue || host.GPS.ActiveLayer <= TruckTaxiNavigationLayer.RivalPassenger) Cancel();
                return;
            }
            float speed = PlayerBody.linearVelocity.magnitude;
            bool atStop = Vector3.ProjectOnPlane(PlayerBody.position - fare.DestinationPosition, Vector3.up).sqrMagnitude <=
                fare.DestinationRadius * fare.DestinationRadius && speed <= session.BoardingMaximumSpeed;
            stoppedSeconds = atStop ? stoppedSeconds + seconds : 0;
            if (!atStop || stoppedSeconds < Mathf.Max(.1f, host.Configuration.exitingSeconds)) return;
            delivering = true;
            bool paid;
            try { paid = session.TryDeliverStolenPassenger(fare.Id, fare.DestinationId, PlayerBody.position, speed); }
            finally { delivering = false; }
            if (paid) Feedback = "STOLEN PASSENGER DELIVERED\nPRIOR ROUTE RESTORED";
        }

        public void Cancel()
        {
            string id = fare?.Id;
            if (id != null) session?.CancelStolenPassenger(id);
            ReleasePresentation(false);
        }

        private void ReleasePresentation(bool exitAtStop)
        {
            var oldFare = fare;
            long oldToken = routeToken;
            var oldActor = actor;
            fare = null; actor = null; routeToken = 0; stoppedSeconds = 0;
            // Navigation ownership must end even if an actor's exit presentation fails.
            if (oldFare != null && oldToken != 0)
                host?.GPS?.ReleaseNavigationIntent(TruckTaxiNavigationLayer.RivalPassenger,
                    "rival-theft." + oldFare.Id, oldFare.DestinationId, oldToken);
            if (oldActor != null)
            {
                rig?.Release(oldActor.transform);
                bool visibleExit = false;
                if (exitAtStop && host?.Player != null)
                {
                    oldActor.PrepareForWorld(host.Player.transform);
                    visibleExit = TruckTaxiActorWorldPlacement.TryExit(oldActor, host.Player.transform, out var exit) &&
                        oldActor.ShowInWorld(exit, host.Player.transform);
                    oldActor.DisarmWaitingTruckHit();
                    if (visibleExit) oldActor.Animate(TruckTaxiPassengerAnimation.ExitAbstraction);
                }
                if (!visibleExit) { oldActor.gameObject.SetActive(false); oldActor.transform.SetParent(null, true); }
                Destroy(oldActor.gameObject, visibleExit ? 8 : 0);
            }
        }

        private void OnFareChanged()
        {
            if (fare != null && session.StolenPassengerFare != fare) ReleasePresentation(delivering);
        }
        private void Subscribe()
        {
            if (subscribed || session == null) return;
            session.RivalPassengerChanged += OnFareChanged; subscribed = true;
        }
        private void Unsubscribe()
        {
            if (subscribed && session != null) session.RivalPassengerChanged -= OnFareChanged;
            subscribed = false;
        }
        private void OnEnable() => Subscribe();
        private void OnDisable() { Cancel(); Unsubscribe(); }
        private void OnDestroy() { Cancel(); Unsubscribe(); }
    }
}
