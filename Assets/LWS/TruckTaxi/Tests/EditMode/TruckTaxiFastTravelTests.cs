using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiFastTravelTests
    {
        private readonly List<Object> objects = new List<Object>();
        private readonly Vector3 origin = new Vector3(40000, 10000, 40000);
        private float timeScale;
        private bool cursorVisible;
        private CursorLockMode cursorLock;

        [SetUp] public void SetUp()
        { timeScale = Time.timeScale; cursorVisible = Cursor.visible; cursorLock = Cursor.lockState; }

        [TearDown] public void TearDown()
        {
            for (int i = objects.Count - 1; i >= 0; i--) if (objects[i] != null) Object.DestroyImmediate(objects[i]);
            objects.Clear();
            Time.timeScale = timeScale; Cursor.visible = cursorVisible; Cursor.lockState = cursorLock;
        }

        private GameObject NewObject(string name)
        { var go = new GameObject(name); objects.Add(go); return go; }

        private Rigidbody Body(Vector3 size, Vector3 offset)
        {
            var go = NewObject("Recovery test body"); go.transform.position = origin + offset;
            go.AddComponent<BoxCollider>().size = size;
            var body = go.AddComponent<Rigidbody>(); body.useGravity = false;
            return body;
        }

        private GameObject Floor(Vector3 size)
        {
            var floor = NewObject("Authored ground");
            floor.transform.position = origin - Vector3.up * .5f;
            floor.AddComponent<BoxCollider>().size = size;
            return floor;
        }

        [Test] public void OnlyAvailableStateAllowsTravelWithoutFareCredit()
        {
            foreach (TruckTaxiState state in Enum.GetValues(typeof(TruckTaxiState)))
                Assert.That(TruckTaxiFastTravel.CanTravelSession(state), Is.EqualTo(state == TruckTaxiState.Available), state.ToString());
        }

        [Test] public void TrailerCandidateTurnsWithWholeCombination()
        {
            var rotation = Quaternion.Euler(0, 90, 0);
            TruckTaxiFastTravel.CandidateTrailerPose(Vector3.zero, Quaternion.identity, Vector3.back * 10,
                Quaternion.identity, new Vector3(30, 2, 20), rotation, out var point, out var facing);
            Assert.That(Vector3.Distance(point, new Vector3(20, 2, 20)), Is.LessThan(.001f));
            Assert.That(Quaternion.Angle(facing, rotation), Is.LessThan(.001f));
        }

        [Test] public void GroundUnderSameHostIsNotIgnored()
        {
            var host = NewObject("Bootstrap hierarchy");
            var floor = Floor(new Vector3(40, 1, 40)); floor.transform.SetParent(host.transform, true);
            var body = Body(new Vector3(3, 2, 7), Vector3.left * 25); body.transform.SetParent(host.transform, true);
            Physics.SyncTransforms();
            Assert.That(TruckTaxiRecoverySpace.TryGroundedPose(body, origin, Quaternion.identity, 1.6f,
                body, null, out var target), Is.True);
            Assert.That(target.y, Is.GreaterThan(origin.y + 1));
        }

        [Test] public void FullTrailerFootprintRequiresGround()
        {
            Floor(new Vector3(10, 1, 8));
            var trailer = Body(new Vector3(3, 3, 14), Vector3.left * 25);
            Physics.SyncTransforms();
            Assert.That(TruckTaxiRecoverySpace.TryGroundedPose(trailer, origin, Quaternion.identity, 1.2f,
                trailer, null, out _), Is.False);
        }

        [Test] public void LowObstacleInLandingVolumeRejectsPlacement()
        {
            Floor(new Vector3(40, 1, 40));
            var body = Body(new Vector3(3, 2, 7), Vector3.left * 25);
            var obstacle = NewObject("Low blocker"); obstacle.transform.position = origin + new Vector3(0, .45f, 2);
            obstacle.AddComponent<BoxCollider>().size = new Vector3(.5f, .9f, .5f);
            Physics.SyncTransforms();
            Assert.That(TruckTaxiRecoverySpace.TryGroundedPose(body, origin, Quaternion.identity, 3,
                body, null, out _), Is.False);
        }

        [Test] public void OccupiedGridSlotRejectsOtherBodyButNotSelf()
        {
            Floor(new Vector3(40, 1, 40));
            var body = Body(new Vector3(3, 2, 7), Vector3.left * 25);
            Body(new Vector3(3, 2, 7), Vector3.up * 2);
            Physics.SyncTransforms();
            Assert.That(TruckTaxiRecoverySpace.TryGroundedPose(body, origin, Quaternion.identity, 1.6f,
                body, null, out _), Is.False);
        }

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public void CancelOrDisableRestoresBothBodiesPauseAndOwnedSuppression(bool previouslyPaused, bool disable)
        {
            var hostObject = NewObject("Inactive host"); hostObject.SetActive(false);
            var host = hostObject.AddComponent<TruckTaxiBootstrap>();
            var config = ScriptableObject.CreateInstance<TruckTaxiConfiguration>(); objects.Add(config);
            var session = new TruckTaxiSession(config, Array.Empty<TruckTaxiRideLocation>(), 17);
            session.StartShift();
            var travel = NewObject("Travel").AddComponent<TruckTaxiFastTravel>(); travel.Initialize(host);
            var tractor = Body(new Vector3(3, 2, 7), Vector3.zero);
            var trailer = Body(new Vector3(3, 3, 14), Vector3.back * 12);
            tractor.linearVelocity = Vector3.right * 2; trailer.angularVelocity = Vector3.up;
            Set(travel, "tractorBefore", new TruckTaxiRecoverySpace.BodySnapshot(tractor));
            Set(travel, "trailerBefore", new TruckTaxiRecoverySpace.BodySnapshot(trailer));
            Set(travel, "travelSession", session); Set(travel, "wasPaused", previouslyPaused);
            Set(travel, "<IsTravelling>k__BackingField", true);
            session.AcquireOfferSuppression(travel);
            var otherOwner = new object(); session.AcquireOfferSuppression(otherOwner);
            long wallet = session.WalletBalanceCents;
            bool requests = session.RideRequestsEnabled;
            host.SetPaused(true);
            TruckTaxiRecoverySpace.Move(tractor, origin + Vector3.forward * 40, Quaternion.Euler(0, 90, 0));
            TruckTaxiRecoverySpace.Move(trailer, origin + Vector3.forward * 28, Quaternion.Euler(0, 90, 0));
            if (disable)
                typeof(TruckTaxiFastTravel).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(travel, null);
            else travel.CancelTravel();
            travel.CancelTravel();
            Assert.That(travel.IsTravelling, Is.False);
            Assert.That(host.Paused, Is.EqualTo(previouslyPaused));
            Assert.That(Vector3.Distance(tractor.position, origin), Is.LessThan(.001f));
            Assert.That(Vector3.Distance(trailer.position, origin + Vector3.back * 12), Is.LessThan(.001f));
            Assert.That(tractor.linearVelocity, Is.EqualTo(Vector3.right * 2));
            Assert.That(trailer.angularVelocity, Is.EqualTo(Vector3.up));
            Assert.That(session.WalletBalanceCents, Is.EqualTo(wallet));
            Assert.That(session.RideRequestsEnabled, Is.EqualTo(requests));
            Assert.That(session.OffersSuppressed, Is.True);
            session.ReleaseOfferSuppression(otherOwner);
            Assert.That(session.OffersSuppressed, Is.False);
        }

        private static void Set(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
