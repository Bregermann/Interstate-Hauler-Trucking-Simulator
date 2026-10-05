using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using LWS.InterstateHauler;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace LWS.TruckTaxi.Tests
{
    [Category("TaxiCompanionPickupRequest")]
    public sealed class TruckTaxiCompanionPickupRequestPlayModeTests
    {
        private readonly List<Object> owned = new List<Object>();
        private TruckTaxiBootstrap host;
        private TruckTaxiRoadsideCompanionLoop loop;
        private TruckTaxiCompanionPickupRequest request;
        private TruckTaxiPassengerActor actor;
        private PassengerProfile fare;
        private GameObject truck, fareVisual;
        private TruckTaxiCabOccupantRig rig;
        private long fareLease;
        private int boarded;
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        private static void Field(object owner, string name, object value) => owner.GetType().GetField(name, Flags).SetValue(owner, value);
        private static void Property(object owner, string name, object value) => owner.GetType().GetProperty(name, Flags).SetValue(owner, value);
        private T Keep<T>(T value) where T : Object { owned.Add(value); return value; }
        private GameObject Box(string name, Vector3 position, Vector3 scale)
        {
            var result = Keep(GameObject.CreatePrimitive(PrimitiveType.Cube)); result.name = name;
            result.transform.position = position; result.transform.localScale = scale; return result;
        }
        private static TruckTaxiCabOccupantRig.Anchor[] SecondaryAnchors() => new[] {
            new TruckTaxiCabOccupantRig.Anchor { id = TruckTaxiCabAnchorId.SleeperRight,
                mountPath = "", localPosition = new Vector3(0, .5f, -.3f), secondaryOccupantSeat = true,
                allowedOccupants = TruckTaxiCabOccupantKind.Companion } };

        [UnitySetUp]
        public IEnumerator Setup()
        {
            var owner = Keep(new GameObject("pickup request bootstrap")); owner.SetActive(false);
            host = owner.AddComponent<TruckTaxiBootstrap>(); host.enabled = false;
            host.configuration = Keep(ScriptableObject.CreateInstance<TruckTaxiConfiguration>());
            var session = new TruckTaxiSession(host.configuration, Array.Empty<TruckTaxiRideLocation>(), 27);
            Property(host, "Session", session);
            fare = Keep(ScriptableObject.CreateInstance<PassengerProfile>());
            fare.passengerId = "test.fare"; fare.companionPickupEligible = true; fare.companionPickupChance = 1;
            var destination = Keep(new GameObject("fare destination")).AddComponent<TruckTaxiRideLocation>();
            destination.locationId = "test.fare.destination"; destination.transform.position = Vector3.forward * 40;
            Property(session, "Passenger", fare); Property(session, "Destination", destination);
            Property(session, "CurrentRideId", "test.fare.1"); Property(session, "State", TruckTaxiState.DrivingToDestination);
            truck = Box("pickup fixture truck", Vector3.up, new Vector3(3, 2, 7));
            var body = truck.AddComponent<Rigidbody>(); body.isKinematic = true;
            Property(host, "Player", truck.AddComponent<LwsPlayerTruck>());
            foreach (var behaviour in truck.GetComponents<MonoBehaviour>()) behaviour.enabled = false;
            var controls = truck.AddComponent<LwsTruckControlController>(); controls.enabled = false;
            Field(controls, "_state", new LwsTruckControlState { parkingBrakeOn = true, hornActive = true });
            foreach (var behaviour in truck.GetComponents<MonoBehaviour>()) behaviour.enabled = false;
            rig = truck.AddComponent<TruckTaxiCabOccupantRig>(); rig.Configure(false, SecondaryAnchors());
            fareVisual = Box("primary fare", new Vector3(1.2f, 2, 0), Vector3.one * .5f);
            fareVisual.AddComponent<TruckTaxiPassengerActor>().Bind(fare);
            fareVisual.transform.SetParent(truck.transform, true);
            Box("walkable floor", new Vector3(0, -.1f, 0), new Vector3(100, .2f, 100)).AddComponent<TruckTaxiSurface>();
            var gps = owner.AddComponent<TruckTaxiGPSAdapter>(); Property(host, "GPS", gps);
            Field(gps, "navigation", new LwsNavigationService()); Field(gps, "player", truck.transform);
            Field(gps, "playerBody", body);
            // The real target is within the pickup radius. GPS uses its ordinary arrived-target path.
            gps.SetRideDestination(destination); fareLease = gps.ActiveLeaseToken;
            var needs = owner.AddComponent<TruckTaxiDriverNeedsCoordinator>();
            Property(needs, "State", new TruckTaxiDriverNeedsState(Keep(ScriptableObject.CreateInstance<TruckTaxiEnvironmentSettings>()), 27));
            Property(host, "DriverNeeds", needs);
            var loopObject = Keep(new GameObject("pickup companions"));
            loop = loopObject.AddComponent<TruckTaxiRoadsideCompanionLoop>(); loop.enabled = false;
            Property(host, "Companions", loop);
            var motion = loopObject.AddComponent<TruckTaxiPrivateEventVehicleMotion>(); motion.Initialize(truck.transform);
            Field(loop, "host", host); Field(loop, "truckBody", body); Field(loop, "controls", controls);
            Field(loop, "cabRig", rig); Field(loop, "motion", motion);
            Field(loop, "nextPopulationRefresh", float.PositiveInfinity); Field(loop, "nextMaterialization", float.PositiveInfinity);
            loop.RegisterBlock("request.block", new Bounds(Vector3.zero, Vector3.one * 40));
            var companions = (IList)typeof(TruckTaxiRoadsideCompanionLoop).GetField("companions", Flags).GetValue(loop);
            var visual = Box("available companion", Vector3.right * 5 + Vector3.up * .25f, Vector3.one * .5f);
            actor = visual.AddComponent<TruckTaxiPassengerActor>(); actor.Bind(null);
            Field(companions[0], "actor", visual);
            loop.CompanionBoarded += _ => boarded++;
            var requestObject = Keep(new GameObject("pickup request coordinator"));
            request = requestObject.AddComponent<TruckTaxiCompanionPickupRequest>();
            Assert.IsTrue(request.Initialize(host));
            // This routing/boarding fixture has no HUD. Supply an explicit presentation capability.
            request.CanOfferPresentation = () => true;
            Physics.SyncTransforms(); yield return null;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            if (request != null) request.Cancel();
            if (loop != null) loop.Cancel();
            if (host?.DriverNeeds != null) Property(host.DriverNeeds, "State", null);
            for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.Destroy(owned[i]);
            owned.Clear(); boarded = 0; yield return null;
        }

        private IEnumerator AwaitBoarding()
        {
            float deadline = Time.realtimeSinceStartup + 6;
            while (loop.IsBusy && !loop.HasOnboardCompanion && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsTrue(loop.HasOnboardCompanion);
        }

        [UnityTest]
        public IEnumerator MissingOrBusyPresentationDoesNotRollOrOffer()
        {
            request.CanOfferPresentation = null;
            Assert.IsFalse(request.TryOffer());
            request.CanOfferPresentation = () => false;
            Assert.IsFalse(request.TryOffer());
            request.CanOfferPresentation = () => true;
            Assert.IsTrue(request.TryOffer());
            yield return null;
        }

        [UnityTest]
        public IEnumerator PausedWithoutOwnedOfferModalCannotAccept()
        {
            Assert.IsTrue(request.TryOffer());
            Property(host, "Paused", true);
            try
            {
                Assert.IsFalse(request.Accept());
                Assert.IsTrue(request.HasOffer);
                Assert.AreEqual(fareLease, host.GPS.ActiveLeaseToken);
                Assert.IsFalse(loop.IsBusy);
            }
            finally { Property(host, "Paused", false); }
            yield return null;
        }

        [UnityTest]
        public IEnumerator AcceptedRequestRewardsOnlyActualSecondaryBoardingAndRestoresFare()
        {
            Vector3 primaryPosition = fareVisual.transform.position;
            var destination = host.Session.Destination;
            Assert.IsTrue(request.TryOffer()); Assert.IsTrue(request.Accept());
            Assert.AreEqual(request.Target.Id, host.GPS.TargetId);
            Assert.AreEqual(0, host.Session.MechanicFareAdjustment);
            Assert.IsTrue(loop.TryHornPickup());
            Assert.AreEqual(0, boarded); Assert.AreEqual(0, host.Session.MechanicFareAdjustment);
            yield return AwaitBoarding();
            Assert.AreEqual(1, boarded);
            Assert.AreEqual(TruckTaxiCompanionPickupRequestState.Succeeded, request.State);
            Assert.AreEqual(request.rewardFareCents, host.Session.MechanicFareAdjustment);
            Assert.AreEqual(fareLease, host.GPS.ActiveLeaseToken);
            Assert.AreEqual(destination.locationId, host.GPS.TargetId);
            Assert.AreSame(fare, host.Session.Passenger); Assert.AreSame(destination, host.Session.Destination);
            Assert.AreEqual(primaryPosition, fareVisual.transform.position);
            Assert.IsFalse(host.Session.OffersSuppressed);
            Assert.IsNull(loop.ActivePrivateStop); Assert.IsFalse(loop.Interact());
            request.Tick(); request.Cancel(); Assert.IsFalse(request.TryOffer());
            Assert.AreEqual(request.rewardFareCents, host.Session.MechanicFareAdjustment);
        }

        [UnityTest]
        public IEnumerator DeclineConsumesRequestWithoutChangingFareOrRoute()
        {
            Assert.IsTrue(request.TryOffer()); Assert.IsTrue(request.Decline());
            request.Tick();
            Assert.IsFalse(request.TryOffer()); Assert.IsFalse(request.Accept());
            Assert.AreEqual(TruckTaxiCompanionPickupRequestState.Declined, request.State);
            Assert.AreEqual(fareLease, host.GPS.ActiveLeaseToken);
            Assert.AreEqual(0, host.Session.MechanicFareAdjustment); Assert.IsFalse(loop.IsBusy);
            yield return null;
        }

        [UnityTest]
        public IEnumerator EligibilityRequiresAuthoredFlagMaterializedActorAndSafeSpareSeat()
        {
            fare.companionPickupEligible = false; fare.personality = "party chaos";
            Assert.IsFalse(request.TryOffer());
            fare.companionPickupEligible = true;
            rig.Configure(false, Array.Empty<TruckTaxiCabOccupantRig.Anchor>());
            Assert.IsFalse(request.TryOffer());
            rig.Configure(false, SecondaryAnchors()); actor.gameObject.SetActive(false);
            Assert.IsFalse(request.TryOffer());
            actor.gameObject.SetActive(true); Assert.IsTrue(request.TryOffer());
            yield return null;
        }

        [UnityTest]
        public IEnumerator TargetLossPopsOnlyOwnedLeaseUnderCoveringService()
        {
            Assert.IsTrue(request.TryOffer()); Assert.IsTrue(request.Accept());
            Assert.IsTrue(host.GPS.TrySetNavigationIntent(TruckTaxiNavigationLayer.PlayerService, "player-service",
                "test.covering.service", "Service", truck.transform.position, TruckTaxiMapMarkerType.Place));
            long overlay = host.GPS.ActiveLeaseToken;
            actor.gameObject.SetActive(false); request.Tick();
            Assert.AreEqual(TruckTaxiCompanionPickupRequestState.Cancelled, request.State);
            Assert.AreEqual(overlay, host.GPS.ActiveLeaseToken);
            Assert.IsTrue(host.GPS.CompleteServiceDestination("test.covering.service", overlay));
            Assert.AreEqual(fareLease, host.GPS.ActiveLeaseToken);
            Assert.AreEqual(0, host.Session.MechanicFareAdjustment);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ReplacedDiversionIsNotPoppedOrRewardedByLateBoarding()
        {
            Assert.IsTrue(request.TryOffer()); Assert.IsTrue(request.Accept()); Assert.IsTrue(loop.TryHornPickup());
            Assert.IsTrue(host.GPS.TrySetNavigationIntent(TruckTaxiNavigationLayer.PassengerDiversion, "other-request",
                "test.other-stop", "Other stop", truck.transform.position, TruckTaxiMapMarkerType.Place));
            long replacement = host.GPS.ActiveLeaseToken;
            yield return AwaitBoarding();
            Assert.AreEqual(TruckTaxiCompanionPickupRequestState.Cancelled, request.State);
            Assert.AreEqual(replacement, host.GPS.ActiveLeaseToken);
            Assert.AreEqual("test.other-stop", host.GPS.TargetId);
            Assert.AreEqual(0, host.Session.MechanicFareAdjustment);
        }

        [UnityTest]
        public IEnumerator MovedTargetCancelsRatherThanRoutingToInventedCoordinates()
        {
            Assert.IsTrue(request.TryOffer()); Assert.IsTrue(request.Accept());
            actor.transform.position += Vector3.right * 2;
            request.Tick();
            Assert.AreEqual(TruckTaxiCompanionPickupRequestState.Cancelled, request.State);
            Assert.AreEqual(fareLease, host.GPS.ActiveLeaseToken);
            Assert.IsFalse(request.Accept()); Assert.AreEqual(0, host.Session.MechanicFareAdjustment);
            yield return null;
        }

        [UnityTest]
        public IEnumerator FareChangeDuringBoardingCannotRewardTheNextFare()
        {
            Assert.IsTrue(request.TryOffer()); Assert.IsTrue(request.Accept()); Assert.IsTrue(loop.TryHornPickup());
            Property(host.Session, "CurrentRideId", "test.fare.2"); request.Tick();
            Assert.AreEqual(TruckTaxiCompanionPickupRequestState.Cancelled, request.State);
            yield return AwaitBoarding();
            Assert.AreEqual(0, host.Session.MechanicFareAdjustment);
            Assert.AreEqual(fareLease, host.GPS.ActiveLeaseToken);
            Assert.IsNull(loop.ActivePrivateStop);
        }

        [UnityTest]
        public IEnumerator UnrouteableActualTargetDeclinesAcceptanceWithoutLeavingReservation()
        {
            actor.transform.position = Vector3.right * 20 + Vector3.up * .25f;
            Assert.IsTrue(request.TryOffer());
            Assert.IsFalse(request.Accept(), "The fixture has no road graph outside the already-arrived pickup radius.");
            Assert.AreEqual(TruckTaxiCompanionPickupRequestState.Cancelled, request.State);
            Assert.AreEqual(fareLease, host.GPS.ActiveLeaseToken);
            Assert.IsTrue(loop.TryFindPickupTarget(120, out var available));
            Assert.AreEqual(actor.transform, available.Actor);
            Assert.AreEqual(0, host.Session.MechanicFareAdjustment);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CoveringRouteSuspendsHornPickupUntilOwnedRouteResumes()
        {
            Assert.IsTrue(request.TryOffer()); Assert.IsTrue(request.Accept());
            long pickupLease = request.RouteLeaseToken;
            Assert.IsTrue(host.GPS.TrySetNavigationIntent(TruckTaxiNavigationLayer.PlayerService, "player-service",
                "test.covering.service", "Service", truck.transform.position, TruckTaxiMapMarkerType.Place));
            long overlay = host.GPS.ActiveLeaseToken;
            request.Tick();
            Assert.IsTrue(request.IsActive); Assert.IsFalse(request.IsLegitimateDiversion); Assert.IsFalse(loop.TryHornPickup());
            Assert.AreEqual(0, boarded);
            Assert.IsTrue(host.GPS.CompleteServiceDestination("test.covering.service", overlay));
            Assert.AreEqual(pickupLease, host.GPS.ActiveLeaseToken);
            Assert.IsTrue(request.IsLegitimateDiversion);
            Assert.IsTrue(loop.TryHornPickup());
            yield return AwaitBoarding();
            Assert.AreEqual(TruckTaxiCompanionPickupRequestState.Succeeded, request.State);
            Assert.AreEqual(fareLease, host.GPS.ActiveLeaseToken);
        }

        [UnityTest]
        public IEnumerator BoardingCompletesOwnLeaseBeneathServiceOverlayWithoutClearingService()
        {
            Assert.IsTrue(request.TryOffer()); Assert.IsTrue(request.Accept()); Assert.IsTrue(loop.TryHornPickup());
            Assert.IsTrue(host.GPS.TrySetNavigationIntent(TruckTaxiNavigationLayer.PlayerService, "player-service",
                "test.covering.service", "Service", truck.transform.position, TruckTaxiMapMarkerType.Place));
            long overlay = host.GPS.ActiveLeaseToken;
            yield return AwaitBoarding();
            Assert.AreEqual(TruckTaxiCompanionPickupRequestState.Succeeded, request.State);
            Assert.AreEqual(request.rewardFareCents, host.Session.MechanicFareAdjustment);
            Assert.AreEqual(overlay, host.GPS.ActiveLeaseToken);
            Assert.IsTrue(host.GPS.CompleteServiceDestination("test.covering.service", overlay));
            Assert.AreEqual(fareLease, host.GPS.ActiveLeaseToken);
        }

        [UnityTest]
        public IEnumerator ReplacedActorInSameLogicalSlotCannotCompleteOriginalRequest()
        {
            Assert.IsTrue(request.TryOffer()); Assert.IsTrue(request.Accept());
            var original = request.Target;
            actor.gameObject.SetActive(false);
            var replacement = Box("replacement companion", Vector3.right * 5 + Vector3.up * .25f, Vector3.one * .5f);
            replacement.AddComponent<TruckTaxiPassengerActor>().Bind(null);
            var companions = (IList)typeof(TruckTaxiRoadsideCompanionLoop).GetField("companions", Flags).GetValue(loop);
            Field(companions[0], "actor", replacement);
            request.Tick();
            Assert.AreEqual(TruckTaxiCompanionPickupRequestState.Cancelled, request.State);
            Assert.AreEqual(fareLease, host.GPS.ActiveLeaseToken);
            Assert.IsTrue(loop.TryFindPickupTarget(120, out var next));
            Assert.AreNotEqual(original.Id, next.Id,
                $"Original '{original.Id}' ({original.Actor?.GetInstanceID()}), replacement '{next.Id}' ({next.Actor?.GetInstanceID()}).");
            Assert.AreNotSame(original.Actor, next.Actor);
            Assert.AreEqual(0, host.Session.MechanicFareAdjustment);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DisableReleasesReservationAndLeaseWithoutRerollingSameFare()
        {
            Assert.IsTrue(request.TryOffer()); Assert.IsTrue(request.Accept());
            request.enabled = false;
            Assert.AreEqual(fareLease, host.GPS.ActiveLeaseToken);
            Assert.IsTrue(loop.TryFindPickupTarget(120, out _));
            request.enabled = true;
            Assert.IsFalse(request.TryOffer()); Assert.AreEqual(0, host.Session.MechanicFareAdjustment);
            yield return null;
        }
    }
}
