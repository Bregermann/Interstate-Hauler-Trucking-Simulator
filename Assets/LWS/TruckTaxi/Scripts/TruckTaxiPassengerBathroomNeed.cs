using System;
using System.Collections.Generic;
using LWS.InterstateHauler;
using UnityEngine;

namespace LWS.TruckTaxi
{
    [DisallowMultipleComponent]
    public sealed class TruckTaxiPassengerBathroomNeed : MonoBehaviour
    {
        [Serializable]
        public sealed class PassengerOverride
        {
            public PassengerProfile passenger;
            public TruckTaxiPassengerBathroomPolicy policy = new TruckTaxiPassengerBathroomPolicy();
        }

        public const string NavigationOwner = "passenger-bathroom";
        public TruckTaxiPassengerBathroomPolicy defaultPolicy = new TruckTaxiPassengerBathroomPolicy();
        public PassengerOverride[] passengerOverrides = Array.Empty<PassengerOverride>();

        // The existing-HUD companion panel supplies this lease after initialization.
        public Func<IDisposable> AcquirePresentationHold { get; set; }
        public event Action RequestOffered;
        public event Action Changed;
        public TruckTaxiPassengerBathroomState Need { get; private set; }
        public TruckTaxiPassengerBathroomPanel HudPanel { get; private set; }
        public bool IsInitialized => clock != null && HudPanel != null;
        public bool CanOfferRequest => HudPanel != null && HudPanel.CanOffer;
        public TruckTaxiServicePoint RestroomService { get; private set; }
        public bool HasPendingRequest => Need?.Phase == TruckTaxiPassengerBathroomPhase.Offered;
        public bool HasAcceptedDiversion => Need?.Phase == TruckTaxiPassengerBathroomPhase.Accepted || IsPresenting;
        public bool IsPresenting => Need?.Phase == TruckTaxiPassengerBathroomPhase.Presenting;
        public bool ReboardRequired { get; private set; }
        public bool IsUrgent => (HasPendingRequest || HasAcceptedDiversion) && Need.Urgent;
        public bool IsLegitimateServiceDiversion => HasAcceptedDiversion && OwnsActiveRoute;
        public string LastDiagnostic { get; private set; } = "Not initialized";
        public string RequestText => (IsUrgent ? "I really need a restroom. " : "Could we stop for a restroom? ") +
            (RestroomService != null ? RestroomService.displayName : "");
        public string StatusText => ReboardRequired ? "Passenger needs help reboarding." :
            IsPresenting ? "Passenger restroom break" : HasAcceptedDiversion ? "Passenger restroom stop" : RequestText;

        private TruckTaxiBootstrap host;
        private ILwsGameClockService clock;
        private Rigidbody body;
        private TruckTaxiServiceRings rings;
        private IDisposable presentationHold;
        private TruckTaxiPassengerActor outsideActor;
        private PassengerProfile passenger;
        private string rideId, targetId;
        private long leaseToken;
        private float nextSearch, nextExitAttempt, presentationAge, nextReboardAttempt;
        private bool skippedTime, abortPresentation;
        private bool OwnsActiveRoute => host != null && host.GPS != null && leaseToken != 0 &&
            host.GPS.ActiveLayer == TruckTaxiNavigationLayer.PassengerDiversion &&
            host.GPS.ActiveLeaseToken == leaseToken && host.GPS.TargetId == targetId;
        private bool CurrentRide => host != null && host.Session != null && host.Session.CurrentRideId == rideId &&
            host.Session.State == TruckTaxiState.DrivingToDestination && !host.Session.SafeDropRequested;
        private bool ValidTarget => IsRestroom(RestroomService) && RestroomService.stableId == targetId &&
            TruckTaxiServicePresentation.Find(targetId) == RestroomService;
        private bool Parked => ValidTarget && body != null &&
            RestroomService.CanUse(body.position, body.linearVelocity.magnitude, TruckTaxiServicePoint.ServiceStopSpeedMetersPerSecond);

        public void Initialize(TruckTaxiBootstrap value)
        {
            Shutdown();
            host = value;
            clock = host != null ? host.Environment?.Clock : null;
            body = host != null && host.Player != null ? host.Player.GetComponent<Rigidbody>() : null;
            rings = host != null ? host.GetComponent<TruckTaxiServiceRings>() : null;
            if (clock == null || host.Session == null || host.GPS == null || body == null)
            { LastDiagnostic = "Waiting for session, player, GPS and game clock initialization."; return; }
            clock.ClockChanged += OnClock;
            host.Session.Changed += OnSessionChanged;
            LastDiagnostic = "Ready";
            SyncRide();
            if (host.hud != null)
            {
                var panel = host.hud.GetComponent<TruckTaxiPassengerBathroomPanel>() ??
                    host.hud.gameObject.AddComponent<TruckTaxiPassengerBathroomPanel>();
                if (panel.Initialize(this, host, host.hud)) HudPanel = panel;
            }
            if (HudPanel == null) LastDiagnostic = "Waiting for the existing HUD root and UI input initialization.";
        }

        // Parent calls with true only when the existing modal arbiter can offer a request.
        public void Tick(bool mayOffer)
        {
            if (!isActiveAndEnabled || clock == null || host == null || body == null) return;
            SyncRide();
            if (!CurrentRide || Need == null) return;
            if (HasPendingRequest || HasAcceptedDiversion)
            {
                if (!ValidTarget || host.Session.ActiveStop != null || host.Session.PendingDiversion != null)
                { Cancel("Restroom diversion withdrawn; service or passenger request changed."); return; }
                // Higher layers suspend this lease. A replacement in our own layer invalidates it.
                if (HasAcceptedDiversion && (host.GPS.ActiveLayer == null ||
                    host.GPS.ActiveLayer <= TruckTaxiNavigationLayer.PassengerDiversion && !OwnsActiveRoute))
                { Cancel("Restroom navigation lease was replaced."); return; }
                if (Need.Phase == TruckTaxiPassengerBathroomPhase.Accepted && OwnsActiveRoute && Parked &&
                    !host.Paused && Time.unscaledTime >= nextExitAttempt) TryBeginPresentation();
                return;
            }
            if (!mayOffer || HudPanel != null && !HudPanel.CanOffer || host.Paused || clock.CurrentSnapshot.paused || !Need.CanOffer ||
                AcquirePresentationHold == null || RequestOffered == null || !PassengerSlotAvailable() ||
                Time.unscaledTime < nextSearch) return;
            nextSearch = Time.unscaledTime + 5;
            RestroomService = FindNearestRestroom(TruckTaxiServicePoint.Points, body.position,
                host.RouteDistances != null ? host.RouteDistances.Measure : (Func<Vector3, Vector3, TruckTaxiRouteLeg>)null);
            if (RestroomService == null) { LastDiagnostic = "No reachable real restroom service is loaded."; return; }
            targetId = RestroomService.stableId;
            if (!Need.Offer()) return;
            LastDiagnostic = "Restroom request offered";
            host.Session.React(RequestText);
            Changed?.Invoke();
            RequestOffered?.Invoke();
        }

        public bool Accept()
        {
            if (!HasPendingRequest || !CurrentRide || !ValidTarget || !PassengerSlotAvailable()) return false;
            if (!host.GPS.TrySetNavigationIntent(TruckTaxiNavigationLayer.PassengerDiversion, NavigationOwner,
                targetId, RestroomService.displayName, RestroomService.Position, TruckTaxiMapMarkerType.Bathroom,
                RestroomService.InteractionRadius, true))
            { LastDiagnostic = "Restroom route unavailable; request remains open."; Changed?.Invoke(); return false; }
            leaseToken = host.GPS.ActiveLeaseToken;
            Need.Accept();
            LastDiagnostic = "Routing to a real restroom";
            host.Session.React("Thanks. A quick restroom stop would help.");
            Changed?.Invoke();
            return true;
        }

        public bool Decline()
        {
            if (!HasPendingRequest || !CurrentRide || !Need.Decline()) return false;
            host.Session.AdjustOnboardPatience(-TruckTaxiPassengerBathroomState.DeclinePatienceLoss(host.Session.OnboardPatience));
            host.Session.React("All right. I'd appreciate a stop when there's time.");
            RestroomService = null; targetId = null;
            LastDiagnostic = "Restroom request declined; ride continues.";
            Changed?.Invoke();
            return true;
        }

        // Call before recovery/teleport. A failed reboard retains the hold instead of stranding a hidden actor.
        public bool Cancel(string reason = "Restroom diversion cancelled.")
        {
            if (!HasPendingRequest && !HasAcceptedDiversion) return true;
            abortPresentation = true;
            LastDiagnostic = reason;
            if (!TryReboard()) { Changed?.Invoke(); return false; }
            Need.Withdraw();
            ReleaseResources();
            Changed?.Invoke();
            return true;
        }

        public bool RetryReboard()
        {
            if (!IsPresenting) return false;
            if (!CurrentRide || outsideActor == null || host.Passengers?.PrimaryActor != outsideActor) abortPresentation = true;
            if (!TryReboard()) return false;
            if (skippedTime && !abortPresentation) Need.CompleteAfterReboard(); else Need.Withdraw();
            LastDiagnostic = skippedTime && !abortPresentation ? "Restroom break complete; previous navigation restored." : "Restroom break cancelled.";
            ReleaseResources();
            Changed?.Invoke();
            return true;
        }

        private void TryBeginPresentation()
        {
            nextExitAttempt = Time.unscaledTime + 3;
            var actor = host.Passengers?.PrimaryActor;
            if (AcquirePresentationHold == null || actor == null || actor.Lifecycle != TruckTaxiActorLifecycle.Seated ||
                body.GetComponent<TruckTaxiCabOccupantRig>() == null ||
                !TruckTaxiActorWorldPlacement.TryExit(actor, body.transform, out var exit))
            { LastDiagnostic = "Waiting for a checked passenger exit and an available presentation hold."; return; }
            var hold = AcquirePresentationHold();
            if (hold == null) return;
            if (!CurrentRide || !OwnsActiveRoute || !Parked || !Need.BeginPresentation()) { hold.Dispose(); return; }
            presentationHold = hold;
            presentationAge = 0; skippedTime = abortPresentation = ReboardRequired = false;
            outsideActor = actor;
            actor.PrepareForWorld(body.transform);
            if (!actor.ShowInWorld(exit, body.transform)) { Cancel("Passenger exit space became obstructed."); return; }
            actor.DisarmWaitingTruckHit();
            actor.SetLifecycle(TruckTaxiActorLifecycle.Exiting);
            actor.Animate(TruckTaxiPassengerAnimation.ExitAbstraction);
            rings?.SetInteracting(targetId, true);
            LastDiagnostic = "Passenger taking a brief restroom break";
            Changed?.Invoke();
        }

        private void Update()
        {
            Tick(false);
            if (!IsPresenting) return;
            if (!CurrentRide || !OwnsActiveRoute || !Parked)
            { Cancel("Restroom break interrupted; passenger must reboard."); return; }
            if (ReboardRequired || abortPresentation)
            { if (Time.unscaledTime >= nextReboardAttempt) RetryReboard(); return; }
            presentationAge += Time.unscaledDeltaTime;
            if (!skippedTime && presentationAge >= .6f)
            {
                skippedTime = true;
                clock.AddHours(Need.ReliefMinutes / 60f);
            }
            if (presentationAge >= 1.2f) RetryReboard();
        }

        private bool TryReboard()
        {
            if (outsideActor == null) { outsideActor = null; ReboardRequired = false; return true; }
            // Terminal ride transitions belong to PassengerRuntime, including ejection/dropoff placement.
            if (host == null || host.Session == null || host.Session.CurrentRideId != rideId || !host.Session.HasPassenger ||
                host.Passengers?.PrimaryActor != outsideActor)
            { outsideActor = null; ReboardRequired = false; return true; }
            if (ReboardRequired && Time.unscaledTime < nextReboardAttempt) return false;
            nextReboardAttempt = Time.unscaledTime + 1;
            var actor = outsideActor;
            Vector3 ground = actor.GroundPosition;
            actor.PrepareForSeat();
            if (body != null && TruckTaxiSeatProfile.TryPlaceActor(actor.transform, body.transform, passenger?.seatProfile,
                false, actor.StandingWorldScale))
            {
                actor.CompleteSeating(); outsideActor = null; ReboardRequired = false; return true;
            }
            actor.PrepareForWorld(body != null ? body.transform : null);
            bool shown = body != null && actor.ShowInWorld(ground, body.transform);
            if (!shown && body != null && TruckTaxiActorWorldPlacement.TryExit(actor, body.transform, out var exit))
                shown = actor.ShowInWorld(exit, body.transform);
            // Preserve visibility at the previous standing pose when streamed support disappears; never invent a new exit.
            if (!shown) actor.gameObject.SetActive(true);
            actor.DisarmWaitingTruckHit();
            actor.SetLifecycle(TruckTaxiActorLifecycle.Returning);
            ReboardRequired = true;
            LastDiagnostic = "No compatible cab seat available. Hold retained; retry reboarding or end the ride before recovery.";
            return false;
        }

        private bool PassengerSlotAvailable() => host.Session.ActiveStop == null && host.Session.PendingDiversion == null &&
            (host.GPS.ActiveLayer == null || host.GPS.ActiveLayer == TruckTaxiNavigationLayer.RideFinal);

        private void OnClock(LwsGameClockSnapshot snapshot) => Need?.ObserveClock(snapshot.totalGameSeconds,
            CurrentRide && !snapshot.paused && !host.Paused && !IsPresenting);
        private void OnSessionChanged() => SyncRide();
        private void SyncRide()
        {
            if (host == null || clock == null || host.Session == null) return;
            var session = host.Session;
            if (session.State != TruckTaxiState.DrivingToDestination || session.SafeDropRequested)
            { Cancel("Ride state changed."); return; }
            if (session.CurrentRideId == rideId && Need != null) return;
            if (!Cancel("Previous ride ended.")) return;
            rideId = session.CurrentRideId;
            passenger = session.Passenger;
            var policy = defaultPolicy;
            if (passengerOverrides != null)
                foreach (var entry in passengerOverrides)
                    if (entry != null && entry.passenger == passenger && entry.policy != null) { policy = entry.policy; break; }
            Need = new TruckTaxiPassengerBathroomState(policy, clock.CurrentSnapshot.totalGameSeconds,
                session.IsIntercityRide, UnityEngine.Random.value);
            nextSearch = nextExitAttempt = 0;
        }

        public static bool IsRestroom(TruckTaxiServicePoint point) => TruckTaxiServicePresentation.IsUsable(point) &&
            point.Supports(TruckTaxiServiceCapability.Restroom) && float.IsFinite(point.verticalTolerance) && point.verticalTolerance >= 0 &&
            float.IsFinite(point.Position.x) && float.IsFinite(point.Position.y) && float.IsFinite(point.Position.z);

        public static TruckTaxiServicePoint FindNearestRestroom(IEnumerable<TruckTaxiServicePoint> points, Vector3 origin,
            Func<Vector3, Vector3, TruckTaxiRouteLeg> measure)
        {
            TruckTaxiServicePoint best = null;
            float distance = float.PositiveInfinity;
            if (points == null) return null;
            foreach (var point in points)
            {
                if (!IsRestroom(point)) continue;
                bool atBay = point.CanUse(origin, 0, TruckTaxiServicePoint.ServiceStopSpeedMetersPerSecond);
                var leg = atBay ? null : measure?.Invoke(origin, point.Position);
                if (!atBay && (leg == null || !leg.Navigable || !float.IsFinite(leg.Meters) || leg.Meters < 0)) continue;
                float candidate = atBay ? 0 : leg.Meters;
                if (candidate < distance || candidate == distance && string.CompareOrdinal(point.stableId, best?.stableId) < 0)
                { best = point; distance = candidate; }
            }
            return best;
        }

        private void ReleaseResources()
        {
            long token = leaseToken; leaseToken = 0;
            if (token != 0 && host != null && host.GPS != null)
                host.GPS.ReleaseNavigationIntent(TruckTaxiNavigationLayer.PassengerDiversion, NavigationOwner, targetId, token);
            rings?.SetInteracting(targetId, false);
            RestroomService = null; targetId = null;
            var hold = presentationHold; presentationHold = null;
            hold?.Dispose();
        }
        private void Shutdown()
        {
            if (clock != null) clock.ClockChanged -= OnClock;
            if (host != null && host.Session != null) host.Session.Changed -= OnSessionChanged;
            if (!Cancel("Restroom controller shutting down."))
            {
                Debug.LogWarning("Passenger restroom controller disabled before reboarding. Actor remains at the checked exit; host must recover the ride.", this);
                Need?.Withdraw(); ReleaseResources();
            }
            HudPanel?.Detach(this); HudPanel = null;
            clock = null; Need = null; rideId = null; outsideActor = null;
        }
        private void OnDisable() => Shutdown();
    }
}
