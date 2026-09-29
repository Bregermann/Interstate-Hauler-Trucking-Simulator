using System;
using System.Collections.Generic;
using System.Reflection;
using NWH.VehiclePhysics2.Input;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiRaceSafetyTests
    {
        private readonly List<Object> objects = new List<Object>();
        [TearDown] public void TearDown()
        {
            for (int i = objects.Count - 1; i >= 0; i--) if (objects[i] != null) Object.DestroyImmediate(objects[i]);
            objects.Clear();
        }

        [Test] public void EightGridSlotsAreDistinctAndUseBothAuthoredLanes()
        {
            var centerline = TruckTaxiSpeedwayLayout.TrackCenterline().ToArray();
            var points = new List<Vector3>();
            for (int slot = 0; slot < 8; slot++)
            {
                int lane = TruckTaxiRaceGrid.Lane(slot);
                Vector3 point = TruckTaxiRaceGrid.LanePoint(centerline, TruckTaxiRaceGrid.Index(centerline.Length, slot), lane);
                Assert.That(lane, Is.EqualTo(slot % 2));
                foreach (var previous in points) Assert.That(Vector3.Distance(previous, point), Is.GreaterThan(8));
                points.Add(point);
            }
            Assert.That(TruckTaxiRaceGrid.Index(centerline.Length, 0), Is.EqualTo(TruckTaxiRaceGrid.Index(centerline.Length, 1)));
            Assert.That(Vector3.Distance(points[0], points[1]), Is.GreaterThanOrEqualTo(9.99f));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void CountdownRestoresConstraintsAndNwhInputExactlyOnce(bool autoInput)
        {
            var go = new GameObject("Start grid body"); objects.Add(go);
            var body = go.AddComponent<Rigidbody>(); body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeRotationX;
            var input = new VehicleInputHandler { autoSetInput = autoInput };
            input.Throttle = .8f; input.Steering = .4f; input.Handbrake = .2f;
            var restraint = new TruckTaxiRaceCountdownLock(body, input);
            try
            {
                Assert.That(body.constraints, Is.EqualTo(RigidbodyConstraints.FreezeAll));
                Assert.That(input.autoSetInput, Is.False);
                input.Throttle = 1; body.position = Vector3.forward * 30;
                restraint.Hold();
                Assert.That(body.position, Is.EqualTo(Vector3.zero));
                Assert.That(input.Throttle, Is.Zero);
                Assert.That(input.Handbrake, Is.EqualTo(1));
            }
            finally { restraint.Dispose(); }
            restraint.Dispose();
            Assert.That(body.constraints, Is.EqualTo(RigidbodyConstraints.FreezeRotationX));
            Assert.That(input.autoSetInput, Is.EqualTo(autoInput));
            Assert.That(input.Throttle, Is.EqualTo(.8f));
            Assert.That(input.Steering, Is.EqualTo(.4f));
            Assert.That(input.Handbrake, Is.EqualTo(.2f));
            input.Throttle = .6f; input.inputModifyCallback.Invoke();
            Assert.That(input.Throttle, Is.EqualTo(.6f));
        }

        private TruckTaxiSession Session()
        {
            var config = ScriptableObject.CreateInstance<TruckTaxiConfiguration>(); objects.Add(config);
            var session = new TruckTaxiSession(config, Array.Empty<TruckTaxiRideLocation>(), 7);
            session.StartShift();
            session.RecordRaceResult(4, 0, 2000);
            return session;
        }

        [Test] public void UnstartedEntryRefundIsExactAndCannotMintCash()
        {
            var session = Session();
            long wallet = session.WalletBalanceCents;
            Assert.That(session.RefundRaceEntry(), Is.False);
            Assert.That(session.TryEnterRace(500), Is.True);
            Assert.That(session.TryEnterRace(500), Is.False);
            Assert.That(session.WalletBalanceCents, Is.EqualTo(wallet - 500));
            Assert.That(session.RefundRaceEntry(), Is.True);
            Assert.That(session.RacesEntered, Is.Zero);
            Assert.That(session.WalletBalanceCents, Is.EqualTo(wallet));
            Assert.That(session.RefundRaceEntry(), Is.False);
            Assert.That(session.WalletBalanceCents, Is.EqualTo(wallet));
        }

        [Test] public void StartedEntryCannotBeRefundedAndInsufficientFundsDoNotCharge()
        {
            var session = Session();
            long wallet = session.WalletBalanceCents;
            Assert.That(session.TryEnterRace(wallet + 1), Is.False);
            Assert.That(session.WalletBalanceCents, Is.EqualTo(wallet));
            Assert.That(session.RacesEntered, Is.Zero);
            Assert.That(session.TryEnterRace(500), Is.True);
            session.CommitRaceEntry();
            Assert.That(session.RefundRaceEntry(), Is.False);
            Assert.That(session.WalletBalanceCents, Is.EqualTo(wallet - 500));
            Assert.That(session.RacesEntered, Is.EqualTo(1));
        }

        [Test] public void BothModalsExposeFocusAndCloseThroughSameApi()
        {
            var go = new GameObject("Race sidecar"); objects.Add(go);
            var race = go.AddComponent<TruckTaxiRaceRuntime>();
            var menu = new GameObject("Registration", typeof(RectTransform)); objects.Add(menu);
            var results = new GameObject("Results", typeof(RectTransform)); objects.Add(results);
            typeof(TruckTaxiRaceRuntime).GetField("menu", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(race, menu.transform);
            typeof(TruckTaxiRaceRuntime).GetField("results", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(race, results.transform);
            results.SetActive(false);
            Assert.That(race.MenuOpen, Is.True);
            Assert.That(race.FocusRoot, Is.SameAs(menu.transform));
            Assert.That(race.BlockTravelReason, Is.Not.Null);
            race.CloseRegistration();
            Assert.That(race.MenuOpen, Is.False);
            results.SetActive(true);
            Assert.That(race.MenuRoot, Is.SameAs(results.transform));
            race.CloseRegistration();
            Assert.That(race.MenuOpen, Is.False);
            Assert.That(race.FocusRoot, Is.Null);
            Assert.That(race.BlockTravelReason, Is.Null);
        }
    }
}
