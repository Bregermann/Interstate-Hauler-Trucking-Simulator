using System;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // Fare-specific semantics only. GPS, secondary seating, and Session retain their authorities.
    public sealed class TruckTaxiCompanionPickupRequest : MonoBehaviour
    {
        [Min(1)] public float nearbyRadius = 120;
        [Min(0)] public int rewardFareCents = 400;
        private TruckTaxiBootstrap host;
        private TruckTaxiSession session;
        private TruckTaxiRoadsideCompanionLoop companions;
        private TruckTaxiCompanionPickupRequestPolicy policy = new TruckTaxiCompanionPickupRequestPolicy();
        private System.Random random;
        private PassengerProfile farePassenger;
        private TruckTaxiRoadsideCompanionLoop.PickupTarget target;
        private Vector3 routedPosition;
        private long routeToken;
        private string routeOwner;
        private bool subscribed, completing;
        private float nextOfferCheck;
        public Func<bool> CanOfferPresentation { get; set; }
        public event Action RequestOffered;
        public event Action Changed;
        public TruckTaxiCompanionPickupRequestPanel HudPanel { get; private set; }
        public bool CanOfferRequest => CanOfferPresentation?.Invoke() == true;
        public TruckTaxiCompanionPickupRequestState State => policy.State;
        public bool HasOffer => State == TruckTaxiCompanionPickupRequestState.Offered;
        public bool IsActive => State == TruckTaxiCompanionPickupRequestState.Accepted;
        public bool IsLegitimateDiversion => IsActive && OwnRouteActive;
        public bool IsBoarding => IsActive && companions != null && companions.IsPickupTargetBoarding(target);
        public string RideId => policy.RideId;
        public TruckTaxiRoadsideCompanionLoop.PickupTarget Target => target;
        public long RouteLeaseToken => routeToken;
        public string Feedback { get; private set; } = "";
        public string Prompt => HasOffer ? "PICK UP A COMPANION?" : IsBoarding ? "BOARDING" : IsActive ? "HONK TO PICK UP" : "";
        private bool Ready => isActiveAndEnabled && host != null && host.Session == session && session != null &&
            host.Player != null && host.GPS != null && companions != null && companions.Ready;
        private bool SameFare => Ready && session.State == TruckTaxiState.DrivingToDestination &&
            session.CurrentRideId == policy.RideId && session.Passenger == farePassenger;
        private bool OwnRouteActive => host?.GPS != null && routeToken != 0 &&
            host.GPS.ActiveLayer == TruckTaxiNavigationLayer.PassengerDiversion &&
            host.GPS.ActiveLeaseToken == routeToken && host.GPS.TargetId == target.Id;

        public bool Initialize(TruckTaxiBootstrap owner, int seed = 27)
        {
            if (owner?.Session == null || owner.Companions == null || owner.GPS == null) return false;
            if (host == owner && session == owner.Session) { Subscribe(); InitializePanel(); return Ready; }
            Cancel(); Unsubscribe();
            HudPanel?.Detach(this); HudPanel = null;
            host = owner; session = owner.Session; companions = owner.Companions;
            policy = new TruckTaxiCompanionPickupRequestPolicy(); random = new System.Random(seed);
            Subscribe(); InitializePanel(); return Ready;
        }

        private void InitializePanel()
        {
            if (host?.hud == null) return;
            var panel = host.hud.GetComponent<TruckTaxiCompanionPickupRequestPanel>() ??
                host.hud.gameObject.AddComponent<TruckTaxiCompanionPickupRequestPanel>();
            if (panel.Initialize(this, host, host.hud)) HudPanel = panel;
        }

        // Parent calls Tick with the existing gameplay loop; this component does not add a second update authority.
        public void Tick()
        {
            if (completing) return;
            if (!Ready) { Cancel(); return; }
            if (policy.Pending && !SameFare) { Cancel("Fare ended or changed."); return; }
            if (host.Paused) return;
            if (!policy.Pending)
            {
                if (Time.time < nextOfferCheck) return;
                nextOfferCheck = Time.time + .5f;
                TryOffer(); return;
            }
            if (HasOffer && (session.ActiveStop != null || session.PendingDiversion != null))
            { Cancel("Another passenger diversion is already pending."); return; }
            if (IsActive && companions.IsPickupTargetBoarding(target)) return;
            if (!companions.IsPickupTargetAvailable(target, nearbyRadius))
            { Cancel("Companion or safe secondary seat is no longer available."); return; }
            if (IsActive && (target.Actor.position - routedPosition).sqrMagnitude > .0625f)
            { Cancel("Companion moved away from the pickup point."); return; }
            if (IsActive && (!host.GPS.ActiveLayer.HasValue ||
                host.GPS.ActiveLayer <= TruckTaxiNavigationLayer.PassengerDiversion) && !OwnRouteActive)
                Cancel("Pickup navigation was replaced.");
        }

        public bool TryOffer()
        {
            if (!Ready || host.Paused || !CanOfferRequest || session.State != TruckTaxiState.DrivingToDestination ||
                session.Passenger == null || !session.Passenger.companionPickupEligible ||
                !policy.CanEvaluate(session.CurrentRideId) || session.ActiveStop != null || session.PendingDiversion != null ||
                host.GPS.ActiveLayer != TruckTaxiNavigationLayer.RideFinal ||
                session.CurrentDesiredDestination == null || host.GPS.TargetId != session.CurrentDesiredDestination.locationId ||
                !companions.TryFindPickupTarget(nearbyRadius, out var candidate)) return false;
            if (!policy.TryOffer(session.CurrentRideId, candidate.Id, true, session.Passenger.companionPickupChance,
                random.NextDouble(), true)) return false;
            target = candidate; farePassenger = session.Passenger;
            routeOwner = "companion-pickup." + policy.RideId;
            Feedback = "Passenger requests an ordinary companion pickup.";
            Changed?.Invoke(); RequestOffered?.Invoke(); return HasOffer;
        }

        public bool Accept()
        {
            if (!HasOffer || !SameFare || host.Paused && HudPanel?.OwnsOfferHold != true) return false;
            if (session.ActiveStop != null || session.PendingDiversion != null ||
                host.GPS.ActiveLayer != TruckTaxiNavigationLayer.RideFinal || session.CurrentDesiredDestination == null ||
                host.GPS.TargetId != session.CurrentDesiredDestination.locationId)
            { Feedback = "Finish the current diversion first."; Changed?.Invoke(); return false; }
            if (!companions.IsPickupTargetAvailable(target, nearbyRadius) ||
                !companions.TryReservePickupTarget(this, target, CanBoardTarget))
            { Cancel("Companion or safe secondary seat is no longer available."); return false; }
            routedPosition = target.Actor.position;
            if (!host.GPS.TrySetNavigationIntent(TruckTaxiNavigationLayer.PassengerDiversion, routeOwner,
                target.Id, "Companion pickup", routedPosition, TruckTaxiMapMarkerType.PassengerPickup, 12, true))
            { Cancel("No truck route to the available companion."); return false; }
            routeToken = host.GPS.ActiveLeaseToken;
            if (!policy.Accept()) { ReleaseOwnedRoute(); companions.ReleasePickupTarget(this); return false; }
            Feedback = "Honk near the selected companion to board the secondary seat.";
            Changed?.Invoke(); return true;
        }

        private bool CanBoardTarget() => IsActive && SameFare && OwnRouteActive &&
            target.Actor != null && (target.Actor.position - routedPosition).sqrMagnitude <= .0625f;

        public bool Decline()
        {
            if (!policy.Decline()) return false;
            Feedback = "Pickup declined. Continue the fare.";
            Changed?.Invoke(); return true;
        }

        public void Cancel() => Cancel("Companion pickup cancelled.");
        private void Cancel(string reason)
        {
            if (completing) return;
            bool changed = policy.Cancel();
            ReleaseOwnedRoute(); companions?.ReleasePickupTarget(this);
            if (changed) { Feedback = reason; Changed?.Invoke(); }
        }

        private void OnBoarded(TruckTaxiRoadsideCompanionLoop.PickupTarget boarded)
        {
            if (completing || !IsActive || boarded.Id != target.Id || boarded.Actor != target.Actor) return;
            if (!SameFare || !companions.IsPickupTargetOnboard(boarded)) { Cancel(); return; }
            completing = true;
            try
            {
                // Releasing the captured token proves ownership even beneath a covering service/emergency route.
                bool owned = ReleaseOwnedRoute();
                companions.ReleasePickupTarget(this);
                if (owned && SameFare && companions.IsPickupTargetOnboard(boarded) &&
                    policy.Complete(session.CurrentRideId, boarded.Id))
                {
                    session.ApplyMechanicReward(0, Mathf.Clamp(rewardFareCents, 0, 10000));
                    Feedback = "Companion onboard. Pickup complete; continue the fare.";
                }
                else { policy.Cancel(); Feedback = "Pickup navigation was replaced; no request reward granted."; }
            }
            finally { completing = false; }
            Changed?.Invoke();
        }

        private bool ReleaseOwnedRoute()
        {
            long token = routeToken; routeToken = 0;
            return token != 0 && host?.GPS != null && host.GPS.ReleaseNavigationIntent(
                TruckTaxiNavigationLayer.PassengerDiversion, routeOwner, target.Id, token);
        }
        private void OnSessionChanged() { if (policy.Pending && !SameFare) Cancel("Fare ended or changed."); }
        private void Subscribe()
        {
            if (subscribed || !isActiveAndEnabled || companions == null || session == null) return;
            companions.CompanionBoarded += OnBoarded; session.Changed += OnSessionChanged; subscribed = true;
        }
        private void Unsubscribe()
        {
            if (!subscribed) return;
            if (companions != null) companions.CompanionBoarded -= OnBoarded;
            if (session != null) session.Changed -= OnSessionChanged;
            subscribed = false;
        }
        private void OnEnable() => Subscribe();
        private void OnDisable() { Cancel(); Unsubscribe(); }
        private void OnDestroy() { Cancel(); Unsubscribe(); HudPanel?.Detach(this); HudPanel = null; }
    }
}
