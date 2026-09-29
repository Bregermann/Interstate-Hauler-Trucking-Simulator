using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    [TestFixture, Category("TaxiSleeper")]
    public sealed class TruckTaxiCabOccupantRigTests
    {
        private GameObject truck, driver, companion;
        private TruckTaxiCabOccupantRig rig;

        [SetUp]
        public void SetUp()
        {
            truck = new GameObject("sleeper fixture");
            rig = truck.AddComponent<TruckTaxiCabOccupantRig>();
            driver = new GameObject("driver");
            companion = new GameObject("companion");
            driver.transform.SetParent(truck.transform, false);
            companion.transform.SetParent(truck.transform, false);
            driver.transform.localPosition = new Vector3(-.5f, 1, 1);
            companion.transform.localPosition = new Vector3(.5f, 1, 1);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(truck);

        private static TruckTaxiCabOccupantRig.Anchor Rear(TruckTaxiCabAnchorId id,
            TruckTaxiCabOccupantKind allowed, Vector3 position)
        {
            return new TruckTaxiCabOccupantRig.Anchor { id = id, mountPath = "",
                allowedOccupants = allowed, localPosition = position, privateEventEligible = true };
        }

        private void ConfigurePair()
        {
            rig.Configure(true, new[] {
                Rear(TruckTaxiCabAnchorId.SleeperLeft, TruckTaxiCabOccupantKind.Driver, new Vector3(-1, 1, -2)),
                Rear(TruckTaxiCabAnchorId.SleeperRight, TruckTaxiCabOccupantKind.Companion, new Vector3(1, 1, -2))
            });
        }

        [Test]
        public void DisabledSleeperCapabilityPreservesLegacySeatPlacement()
        {
            rig.Configure(false, new[] { Rear(TruckTaxiCabAnchorId.SleeperLeft,
                TruckTaxiCabOccupantKind.Passenger, Vector3.back * 3) });
            var profile = ScriptableObject.CreateInstance<TruckTaxiSeatProfile>();
            try
            {
                profile.mountPath = "";
                Assert.IsTrue(TruckTaxiSeatProfile.TryPlaceActor(companion.transform, truck.transform,
                    profile, true, Vector3.one));
                Assert.AreEqual(profile.localPosition + profile.secondarySeatOffset, companion.transform.localPosition);
                Assert.AreEqual(.84f, companion.transform.lossyScale.x, .001f);
                Assert.IsFalse(rig.TryBeginPrivateTransition(driver.transform, companion.transform,
                    TruckTaxiSeatType.CompanionPassenger));
            }
            finally { Object.DestroyImmediate(profile); }
        }

        [Test]
        public void PrivateTransitionNeedsDistinctCompatibleSeatsBeforeMovingAnyone()
        {
            var shared = Rear(TruckTaxiCabAnchorId.SleeperBed,
                TruckTaxiCabOccupantKind.Driver | TruckTaxiCabOccupantKind.Companion, Vector3.back * 3);
            rig.Configure(true, new[] { shared });
            Vector3 driverBefore = driver.transform.localPosition, companionBefore = companion.transform.localPosition;
            Assert.IsFalse(rig.TryBeginPrivateTransition(driver.transform, companion.transform, TruckTaxiSeatType.CompanionPassenger));
            Assert.AreEqual(driverBefore, driver.transform.localPosition);
            Assert.AreEqual(companionBefore, companion.transform.localPosition);
            Assert.IsFalse(rig.PrivateTransitionActive);
        }

        [Test]
        public void CompletedTransitionVacatesFrontSeatsAndRestoresBothOccupantsAndDivider()
        {
            ConfigurePair();
            var divider = new GameObject("divider");
            divider.transform.SetParent(truck.transform, false);
            divider.SetActive(false);
            rig.privacyDivider = divider;
            Vector3 driverBefore = driver.transform.localPosition, companionBefore = companion.transform.localPosition;
            companion.transform.localScale = Vector3.one * .7f;
            Assert.IsTrue(rig.TryBeginPrivateTransition(driver.transform, companion.transform, TruckTaxiSeatType.CompanionPassenger));
            Assert.AreEqual(driverBefore, driver.transform.localPosition);
            Assert.IsFalse(rig.StepPrivateTransition(rig.privateTransitionSeconds * .5f));
            Assert.IsFalse(divider.activeSelf);
            Assert.IsTrue(rig.StepPrivateTransition(rig.privateTransitionSeconds));
            Assert.AreEqual(new Vector3(-1, 1, -2), driver.transform.localPosition);
            Assert.AreEqual(new Vector3(1, 1, -2), companion.transform.localPosition);
            Assert.IsTrue(rig.PrivateTransitionComplete);
            Assert.IsTrue(divider.activeSelf);
            rig.EndPrivateTransition();
            rig.EndPrivateTransition();
            Assert.AreEqual(driverBefore, driver.transform.localPosition);
            Assert.AreEqual(companionBefore, companion.transform.localPosition);
            Assert.AreEqual(Vector3.one * .7f, companion.transform.localScale);
            Assert.IsFalse(divider.activeSelf);
            Assert.IsFalse(rig.PrivateTransitionActive);
        }

        [Test]
        public void DisableCallbackDuringTransitionRestoresTheFrontPose()
        {
            ConfigurePair();
            Vector3 before = driver.transform.localPosition;
            Assert.IsTrue(rig.TryBeginPrivateTransition(driver.transform, companion.transform, TruckTaxiSeatType.CompanionPassenger));
            rig.StepPrivateTransition(.3f);
            // EditMode does not dispatch MonoBehaviour lifecycle callbacks for this runtime component.
            typeof(TruckTaxiCabOccupantRig).GetMethod("OnDisable",System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic).Invoke(rig,null);
            Assert.AreEqual(before, driver.transform.localPosition);
            Assert.IsFalse(rig.PrivateTransitionActive);
        }

        [Test]
        public void RearScaleDoesNotCompoundFrontScaleAndFrontReservationSurvivesReturn()
        {
            var front = Rear(TruckTaxiCabAnchorId.FrontPassengerSeat, TruckTaxiCabOccupantKind.Companion, Vector3.forward);
            front.localScale = Vector3.one * .5f;
            var rear = Rear(TruckTaxiCabAnchorId.SleeperRight, TruckTaxiCabOccupantKind.Companion, Vector3.back);
            rear.localScale = Vector3.one * .75f;
            rig.Configure(true, new[] { front, rear,
                Rear(TruckTaxiCabAnchorId.SleeperLeft, TruckTaxiCabOccupantKind.Driver, Vector3.back * 2) });
            Assert.IsTrue(rig.TrySeat(companion.transform, TruckTaxiCabOccupantKind.Companion,
                TruckTaxiCabAnchorId.FrontPassengerSeat, TruckTaxiSeatType.CompanionPassenger, Vector3.one));
            Assert.AreEqual(.5f, companion.transform.lossyScale.x, .001f);
            Assert.IsTrue(rig.TryBeginPrivateTransition(driver.transform, companion.transform, TruckTaxiSeatType.CompanionPassenger));
            Assert.IsTrue(rig.StepPrivateTransition(1));
            Assert.AreEqual(.75f, companion.transform.lossyScale.x, .001f);
            rig.EndPrivateTransition();
            Assert.AreEqual(.5f, companion.transform.lossyScale.x, .001f);
            var other = new GameObject("other occupant");
            other.transform.SetParent(truck.transform, false);
            Assert.IsTrue(rig.TrySeat(other.transform, TruckTaxiCabOccupantKind.Companion,
                TruckTaxiCabAnchorId.FrontPassengerSeat, TruckTaxiSeatType.CompanionPassenger, Vector3.one));
            Assert.AreEqual(rear.localPosition, other.transform.localPosition);
        }

        [Test]
        public void InactiveOccupantInvalidatesTransitionAndCleanupStillRestoresDriver()
        {
            ConfigurePair();
            Vector3 before = driver.transform.localPosition;
            Assert.IsTrue(rig.TryBeginPrivateTransition(driver.transform, companion.transform, TruckTaxiSeatType.CompanionPassenger));
            rig.StepPrivateTransition(.3f);
            companion.SetActive(false);
            Assert.IsFalse(rig.PrivateTransitionValid);
            Assert.IsFalse(rig.StepPrivateTransition(1));
            rig.EndPrivateTransition();
            Assert.AreEqual(before, driver.transform.localPosition);
            Assert.IsFalse(rig.PrivateTransitionActive);
        }

        [Test]
        public void AnchorRestrictionsPriorityVisibilityAndLightweightPhysicsAreApplied()
        {
            var denied = Rear(TruckTaxiCabAnchorId.FrontPassengerSeat, TruckTaxiCabOccupantKind.Passenger, Vector3.forward);
            denied.allowedSeatTypes = new[] { TruckTaxiSeatType.RobotPassenger };
            var allowed = Rear(TruckTaxiCabAnchorId.SleeperRear, TruckTaxiCabOccupantKind.Passenger, Vector3.back * 2);
            allowed.firstPersonVisible = false;
            allowed.priority = 10;
            var fallback = Rear(TruckTaxiCabAnchorId.FloorPerch, TruckTaxiCabOccupantKind.Passenger, Vector3.back);
            var part = new GameObject("head");
            part.transform.SetParent(companion.transform, false);
            Vector3 oldPose = part.transform.localPosition;
            allowed.pose = new[] { new TruckTaxiCabOccupantRig.PosePart { path = "head", localPosition = Vector3.up } };
            var body = companion.AddComponent<Rigidbody>();
            var collider = companion.AddComponent<BoxCollider>();
            rig.Configure(true, new[] { denied, fallback, allowed });
            Assert.IsTrue(rig.TrySeat(companion.transform, TruckTaxiCabOccupantKind.Passenger,
                TruckTaxiCabAnchorId.FrontPassengerSeat, TruckTaxiSeatType.StandardPassenger, Vector3.one));
            Assert.AreEqual(allowed.localPosition, companion.transform.localPosition);
            Assert.AreEqual(29, companion.layer);
            Assert.AreEqual(Vector3.up, part.transform.localPosition);
            Assert.IsTrue(body.isKinematic);
            Assert.IsFalse(collider.enabled);
            rig.Release(companion.transform);
            Assert.AreEqual(0, companion.layer);
            Assert.AreEqual(oldPose, part.transform.localPosition);
        }

        [Test]
        public void SecondaryPassengerCannotFallBackIntoOccupiedFrontSeats()
        {
            var front = Rear(TruckTaxiCabAnchorId.FrontPassengerSeat, TruckTaxiCabOccupantKind.Passenger, Vector3.forward);
            rig.Configure(true, new[] { front });
            Vector3 before = companion.transform.position;
            Assert.IsFalse(TruckTaxiSeatProfile.TryPlaceActor(companion.transform, truck.transform, null, true, Vector3.one));
            Assert.AreEqual(before, companion.transform.position);
        }
    }
}
