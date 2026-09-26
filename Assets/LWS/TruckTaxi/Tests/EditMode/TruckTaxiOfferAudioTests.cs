using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiOfferAudioTests
    {
        private readonly List<Object> owned=new List<Object>();
        private TruckTaxiConfiguration config;
        private TruckTaxiSession session;
        [SetUp] public void Setup()
        {
            config=ScriptableObject.CreateInstance<TruckTaxiConfiguration>(); owned.Add(config);
            var passenger=ScriptableObject.CreateInstance<PassengerProfile>(); owned.Add(passenger);
            config.passengers=new[]{passenger}; config.minimumTripDistance=10; config.maximumTripDistance=1000; config.offerDuration=10;
            var locations=new List<TruckTaxiRideLocation>();
            for(int i=0;i<2;i++)
            {
                var go=new GameObject("Offer test stop"); owned.Add(go);
                var stop=go.AddComponent<TruckTaxiRideLocation>(); stop.locationId="offer.test."+i; stop.transform.position=Vector3.right*i*100;
                locations.Add(stop);
            }
            session=new TruckTaxiSession(config,locations,31); session.StartShift(); Assert.IsTrue(session.OfferRide());
        }
        [TearDown] public void Cleanup() { foreach(var o in owned) Object.DestroyImmediate(o); owned.Clear(); }
        [Test] public void OneNotificationPerOfferAndNewOfferAfterExpiry()
        {
            var first=session.Offer; Assert.IsTrue(first.TryClaimNotification());
            session.DebugSetOfferDuration(3); Assert.IsFalse(first.TryClaimNotification());
            session.Tick(3,Vector3.zero,0,0,true); Assert.IsFalse(session.CanRespondToOffer);
            Assert.IsTrue(session.OfferRide()); Assert.AreNotEqual(first.Id,session.Offer.Id); Assert.IsTrue(session.Offer.TryClaimNotification());
            Assert.AreEqual(1,session.OfferRemainingNormalized);
        }
        [Test] public void SnapshotDurationDrivesCountdownAndExpiry()
        {
            config.offerDuration=50;
            for(int i=0;i<10;i++)
            {
                Assert.That(session.OfferRemainingNormalized,Is.EqualTo((10-i)/10f).Within(.0001));
                Assert.That(session.OfferRemaining,Is.EqualTo(10-i).Within(.0001));
                session.Tick(1,Vector3.zero,0,0,true);
            }
            Assert.AreEqual(TruckTaxiState.Available,session.State);
            Assert.AreEqual(0,session.OfferRemaining); Assert.AreEqual(0,session.OfferRemainingNormalized); Assert.IsFalse(session.AcceptRide());
        }
        [Test] public void DebugPauseFreezesWholeOfferTimer()
        {
            session.OfferTimerPaused=true; session.Tick(99,Vector3.zero,0,0,true);
            Assert.AreEqual(10,session.OfferRemaining); Assert.AreEqual(1,session.OfferRemainingNormalized);
            session.OfferTimerPaused=false; session.Tick(2,Vector3.zero,0,0,true); Assert.AreEqual(8,session.OfferRemaining);
        }
        [TestCase(true)] [TestCase(false)] public void ResponseClosesTimer(bool accept)
        {
            if(accept) Assert.IsTrue(session.AcceptRide()); else session.DeclineRide();
            Assert.AreEqual(0,session.OfferRemainingNormalized); Assert.IsFalse(session.CanRespondToOffer);
        }
        [TestCase(0,-80)] [TestCase(1,0)] [TestCase(.5f,-6.0206f)]
        public void MixerVolumeIsLogarithmic(float volume,float db) => Assert.That(TruckTaxiAudioController.ToDecibels(volume),Is.EqualTo(db).Within(.0001));
        [Test] public void AuthoredDefaultRoundTrips() => Assert.That(TruckTaxiAudioController.ToDecibels(TruckTaxiAudioController.FromDecibels(-9)),Is.EqualTo(-9).Within(.0001));
    }
}
