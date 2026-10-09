using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    [TestFixture, Category("TaxiOccupants")]
    public sealed class TruckTaxiOccupantFoundationTests
    {
        private readonly List<Object> created = new List<Object>();
        private GameObject Make(string name)
        {
            var result = new GameObject(name);
            created.Add(result);
            return result;
        }
        private TruckTaxiCabOccupantRig Rig(params TruckTaxiCabOccupantRig.Anchor[] slots)
        {
            var rig = Make("truck").AddComponent<TruckTaxiCabOccupantRig>();
            rig.Configure(true, slots);
            return rig;
        }
        private static TruckTaxiCabOccupantRig.Anchor Slot(string id, float x,
            TruckTaxiCabOccupantKind role = TruckTaxiCabOccupantKind.Passenger) =>
            new TruckTaxiCabOccupantRig.Anchor {
                id = TruckTaxiCabAnchorId.SleeperLeft, slotId = id, mountPath = "",
                localPosition = Vector3.right * x, allowedOccupants = role, secondaryOccupantSeat = true
            };

        [TearDown]
        public void Cleanup()
        {
            for (int i = created.Count - 1; i >= 0; i--)
                if (created[i] != null) Object.DestroyImmediate(created[i]);
            created.Clear();
        }

        [Test]
        public void CapacityComesFromValidAuthoredSlotsAndNeverExceedsEight()
        {
            var rig = Rig();
            Assert.AreEqual(0, rig.AuthoredCapacity);
            var slots = new TruckTaxiCabOccupantRig.Anchor[9];
            for (int i = 0; i < slots.Length; i++) slots[i] = Slot("seat-" + i, i * 3);
            rig.Configure(true, slots);
            Assert.AreEqual(8, rig.AuthoredCapacity);
            for (int i = 0; i < 8; i++)
                Assert.IsTrue(rig.TrySeatSecondary(Make("rider").transform,
                    TruckTaxiSeatType.StandardPassenger, Vector3.one, TruckTaxiCabOccupantKind.Passenger));
            Assert.AreEqual(8, rig.OccupiedCount);
            Assert.IsFalse(rig.HasSecondarySeat(TruckTaxiSeatType.StandardPassenger, null, TruckTaxiCabOccupantKind.Passenger));
        }

        [Test]
        public void DuplicateIdentitiesAndMissingMountsDoNotAdvertiseCapacity()
        {
            var missing = Slot("missing", 6);
            missing.mountPath = "NotAuthored";
            var rig = Rig(Slot("same", 0), Slot("same", 3), missing);
            Assert.AreEqual(0, rig.AuthoredCapacity);
            Assert.IsFalse(rig.HasSecondarySeat(TruckTaxiSeatType.StandardPassenger, null, TruckTaxiCabOccupantKind.Passenger));
        }

        [Test]
        public void CompanionDoesNotGloballyBlockOtherCompatibleSlots()
        {
            var rig = Rig(Slot("companion", 0, TruckTaxiCabOccupantKind.Companion), Slot("fare", 3),
                Slot("stolen", 6, TruckTaxiCabOccupantKind.StolenPassenger));
            Assert.IsTrue(rig.TrySeatSecondary(Make("companion").transform, TruckTaxiSeatType.CompanionPassenger, Vector3.one));
            var fare = Make("fare");
            Assert.IsTrue(TruckTaxiSeatProfile.TryPlaceActor(fare.transform, rig.transform, null, false, Vector3.one));
            Assert.IsTrue(rig.TryGetSlot(fare.transform, out var id));
            Assert.AreEqual("fare", id);
            Assert.IsTrue(rig.HasSecondarySeat(TruckTaxiSeatType.StandardPassenger, null, TruckTaxiCabOccupantKind.StolenPassenger));
            Assert.IsFalse(rig.HasSecondarySeat(TruckTaxiSeatType.StandardPassenger, null, TruckTaxiCabOccupantKind.Passenger));
            rig.Release(fare.transform);
            // Exit owner detaches the released visual before the next boarding query.
            fare.transform.SetParent(null);
            Assert.IsFalse(rig.IsOccupied("fare"));
            Assert.IsTrue(rig.HasSecondarySeat(TruckTaxiSeatType.StandardPassenger, null, TruckTaxiCabOccupantKind.Passenger));
        }

        [Test]
        public void PairRequiresTwoDistinctCompatibleSlotsBeforeMovingEitherActor()
        {
            var rig = Rig(Slot("only", 0));
            var first = Make("first").AddComponent<TruckTaxiPassengerActor>();
            var second = Make("second").AddComponent<TruckTaxiPassengerActor>();
            first.Bind(null);
            second.Bind(null);
            Assert.IsFalse(rig.CanSeatPassengerPair(first.transform, null, second.transform, null));
            Assert.IsFalse(rig.TrySeatPassengerPair(first, null, second, null));
            Assert.IsNull(first.transform.parent);
            Assert.IsNull(second.transform.parent);
            rig.Configure(true, new[] { Slot("a", 0), Slot("b", 3) });
            Assert.IsTrue(rig.CanSeatPassengerPair(first.transform, null, second.transform, null));
            Assert.IsTrue(rig.TrySeatPassengerPair(first, null, second, null));
            Assert.AreEqual(2, rig.OccupiedCount);
        }

        [Test]
        public void SeatTypeAndRoleRestrictionsRemainAuthoritative()
        {
            var slot = Slot("robot", 0, TruckTaxiCabOccupantKind.ExtraPassenger);
            slot.allowedSeatTypes = new[] { TruckTaxiSeatType.RobotPassenger };
            var rig = Rig(slot);
            Assert.IsFalse(rig.HasSecondarySeat(TruckTaxiSeatType.StandardPassenger, null, TruckTaxiCabOccupantKind.ExtraPassenger));
            Assert.IsFalse(rig.HasSecondarySeat(TruckTaxiSeatType.RobotPassenger, null, TruckTaxiCabOccupantKind.Companion));
            Assert.IsTrue(rig.HasSecondarySeat(TruckTaxiSeatType.RobotPassenger, null, TruckTaxiCabOccupantKind.ExtraPassenger));
        }

        [Test]
        public void BlockedLegacyFrontSeatFallsBackToAuthoredCompatibleExtraSeat()
        {
            var extra = Slot("extra", 5);
            var rig = Rig(extra);
            rig.Configure(false, new[] { extra });
            var legacy = Make("legacy companion").AddComponent<TruckTaxiPassengerActor>();
            legacy.Bind(null);
            legacy.transform.SetParent(rig.transform);
            legacy.transform.localPosition = new Vector3(.5f, 1, 0);
            var incoming = Make("fare").transform;
            Assert.IsTrue(TruckTaxiSeatProfile.CanPlaceActor(incoming, rig.transform, null, false));
            Assert.IsTrue(TruckTaxiSeatProfile.TryPlaceActor(incoming, rig.transform, null, false, Vector3.one));
            Assert.IsTrue(rig.TryGetSlot(incoming, out var id));
            Assert.AreEqual("extra", id);
        }

        [Test]
        public void OverlappingExtraSlotCannotBypassClearanceThroughPrimarySeatingApi()
        {
            var rig = Rig(Slot("companion", 0, TruckTaxiCabOccupantKind.Companion), Slot("fare", .1f));
            Assert.IsTrue(rig.TrySeatSecondary(Make("companion").transform, TruckTaxiSeatType.CompanionPassenger, Vector3.one));
            Assert.IsFalse(rig.TrySeat(Make("fare").transform, TruckTaxiCabOccupantKind.Passenger,
                TruckTaxiCabAnchorId.SleeperLeft, TruckTaxiSeatType.StandardPassenger, Vector3.one));
        }

        [Test]
        public void LegacyEnumSlotIdentityRemainsStableWhenStringIdIsAbsent()
        {
            var slot = Slot(null, 0);
            Assert.AreEqual("SleeperLeft", slot.StableSlotId);
            var rig = Rig(slot);
            var actor = Make("fare").transform;
            Assert.IsTrue(rig.TrySeatSecondary(actor, TruckTaxiSeatType.StandardPassenger, Vector3.one,
                TruckTaxiCabOccupantKind.Passenger));
            Assert.IsTrue(rig.IsOccupied("SleeperLeft"));
        }

        [TestCase(TruckTaxiState.Available, true)]
        [TestCase(TruckTaxiState.DrivingToPickup, true)]
        [TestCase(TruckTaxiState.DrivingToDestination, true)]
        [TestCase(TruckTaxiState.PassengerBoarding, false)]
        [TestCase(TruckTaxiState.PassengerExiting, false)]
        [TestCase(TruckTaxiState.Inactive, false)]
        public void HornEligibilityAllowsTravelButNotTransitions(TruckTaxiState state, bool expected) =>
            Assert.AreEqual(expected, TruckTaxiRoadsideCompanionLoop.AllowsHornPickup(state));

        [Test]
        public void DriverVisualHidesInAllCamerasWithoutDeactivatingLogicalActorAndRestoresPriorFlags()
        {
            var visual = Make("driver");
            var visible = visual.AddComponent<MeshRenderer>();
            var child = Make("already hidden");
            child.transform.SetParent(visual.transform);
            var hidden = child.AddComponent<MeshRenderer>();
            hidden.forceRenderingOff = true;
            using (var visibility = new TruckTaxiDriverVisualVisibility())
            {
                visibility.SetOutsideCab(visual.transform, true);
                visibility.SetOutsideCab(visual.transform, true);
                Assert.IsTrue(visual.activeSelf);
                Assert.IsTrue(visible.forceRenderingOff);
                visibility.SetOutsideCab(visual.transform, false);
                Assert.IsFalse(visible.forceRenderingOff);
                Assert.IsTrue(hidden.forceRenderingOff);
                visibility.SetOutsideCab(visual.transform, true);
            }
            Assert.IsFalse(visible.forceRenderingOff);
            Assert.IsTrue(hidden.forceRenderingOff);
        }

        [Test]
        public void ReactionRequiresAuthoredTextAndExplicitAdults()
        {
            var profile = ScriptableObject.CreateInstance<PassengerProfile>();
            created.Add(profile);
            var reaction = new TruckTaxiOccupantReaction { passenger = profile, subtitle = "More company? Fine by me." };
            Assert.IsFalse(reaction.Eligible(profile, true));
            profile.explicitlyAdult = true;
            profile.minimumAdultAge = 17;
            Assert.IsFalse(reaction.Eligible(profile, true));
            profile.minimumAdultAge = 21;
            Assert.IsFalse(reaction.Eligible(profile, false));
            Assert.IsTrue(reaction.Eligible(profile, true));
            reaction.subtitle = " ";
            Assert.IsFalse(reaction.Eligible(profile, true));
        }
    }
}
