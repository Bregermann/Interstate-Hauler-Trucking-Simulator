using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    [Category("TaxiMegaPass")]
    public sealed class TruckTaxiObjectiveMegaPassTests
    {
        private PassengerRequestDefinition definition;
        private PassengerProfile passenger;
        private bool hadPreference;
        private int previousPreference;

        [SetUp] public void SetUp()
        {
            hadPreference=PlayerPrefs.HasKey(TruckTaxiSession.RideRequestsPreferenceKey);
            previousPreference=PlayerPrefs.GetInt(TruckTaxiSession.RideRequestsPreferenceKey,1);
            PlayerPrefs.SetInt(TruckTaxiSession.RideRequestsPreferenceKey,1);
            definition = ScriptableObject.CreateInstance<PassengerRequestDefinition>();
            passenger = ScriptableObject.CreateInstance<PassengerProfile>();
            definition.requestType = TaxiRequestType.RamTraffic;
        }

        [TearDown] public void TearDown()
        {
            Object.DestroyImmediate(definition);
            Object.DestroyImmediate(passenger);
            if(hadPreference) PlayerPrefs.SetInt(TruckTaxiSession.RideRequestsPreferenceKey,previousPreference);
            else PlayerPrefs.DeleteKey(TruckTaxiSession.RideRequestsPreferenceKey);
        }

        [Test] public void CountBandsRespectRideOpportunityAndAvailableDistinctTargets()
        {
            Assert.AreEqual(1, TruckTaxiRequestPolicy.Count(definition, passenger, 250, 20, 1));
            Assert.AreEqual(2, TruckTaxiRequestPolicy.Count(definition, passenger, 900, 20, 1));
            definition.countBand = TaxiRequestCountBand.Medium;
            Assert.AreEqual(3, TruckTaxiRequestPolicy.Count(definition, passenger, 1200, 20, 1));
            definition.countBand = TaxiRequestCountBand.Chaotic;
            Assert.AreEqual(5, TruckTaxiRequestPolicy.Count(definition, passenger, 4000, 20, 1));
            Assert.AreEqual(2, TruckTaxiRequestPolicy.Count(definition, passenger, 4000, 2, 1));
        }

        [Test] public void ExplicitCountBoundsAndPersonalityAffectRequirements()
        {
            definition.minimumCount = 2;
            definition.maximumCount = 4;
            passenger.chaosAffinity = -1;
            int cautious = TruckTaxiRequestPolicy.Count(definition, passenger, 4000, 20, .35f);
            passenger.chaosAffinity = 1;
            int chaotic = TruckTaxiRequestPolicy.Count(definition, passenger, 4000, 20, .35f);
            Assert.Less(cautious, chaotic);
            Assert.AreEqual(1, TruckTaxiRequestPolicy.SelectionWeight(definition, null));
            Assert.AreEqual(4, TruckTaxiRequestPolicy.Count(definition, passenger, 4000, 20, 1));
        }

        [Test] public void OnlyAuthoredDeadlinesOrFastDeliveryAreTimed()
        {
            definition.timer = 1;
            Assert.IsFalse(TruckTaxiRequestPolicy.IsTimed(definition));
            definition.hasDeadline = true;
            Assert.IsTrue(TruckTaxiRequestPolicy.IsTimed(definition));
            definition.hasDeadline = false;
            definition.requestType = TaxiRequestType.FastDelivery;
            Assert.IsTrue(TruckTaxiRequestPolicy.IsTimed(definition));
        }

        [Test] public void LargerDistinctRequirementsEarnLargerRewards()
        {
            var one = new TaxiRequestProgress(definition, 1);
            var three = new TaxiRequestProgress(definition, 3);
            Assert.AreEqual(1, TruckTaxiRequestPolicy.RewardMultiplier(one));
            Assert.Greater(TruckTaxiRequestPolicy.RewardMultiplier(three),
                TruckTaxiRequestPolicy.RewardMultiplier(one));
        }

        [Test] public void ScenicCompletionGrantsPartialReliefOnlyOnceAfterSuccess()
        {
            var owned = new List<Object>();
            try
            {
                var config = ScriptableObject.CreateInstance<TruckTaxiConfiguration>(); owned.Add(config);
                passenger.possibleRequests = System.Array.Empty<PassengerRequestDefinition>();
                passenger.basePatience = 1000;
                config.passengers = new[] { passenger };
                config.minimumTripDistance = 10;
                config.maximumTripDistance = 1000;
                var places = new List<TruckTaxiRideLocation>();
                for (int i = 0; i < 2; i++)
                {
                    var go = new GameObject("Ride stop"); owned.Add(go);
                    var point = go.AddComponent<TruckTaxiRideLocation>();
                    point.locationId = "stop." + i;
                    go.transform.position = Vector3.right * (100 * i);
                    places.Add(point);
                }
                var scenicGo = new GameObject("Scenic stop"); owned.Add(scenicGo);
                var scenic = scenicGo.AddComponent<TruckTaxiStopObjectivePoint>();
                scenic.stableId = "scenic.test";
                scenic.category = TruckTaxiStopCategory.Scenic;
                definition.requestType = TaxiRequestType.ScenicRoute;
                definition.scenicThirstRelief = .25f;
                var session = new TruckTaxiSession(config, places, 31);
                session.Capabilities.Stops.Add(scenic);
                session.Capabilities.Register(TruckTaxiObjectiveCapability.ScenicStops, 1);
                session.StartShift(); Assert.IsTrue(session.OfferRide()); Assert.IsTrue(session.AcceptRide());
                session.Tick(.1f, session.Pickup.StopPosition, 0, 0, true);
                session.Tick(2, session.Pickup.StopPosition, 0, 0, true);
                Assert.IsTrue(session.GenerateRequest(definition));
                Assert.IsTrue(session.AcceptDiversion());
                var request = session.ActiveStop;
                var completion = new TruckTaxiScenicCompletion(session, null);
                int grants = 0; float amount = 0;
                completion.SetThirstRelief(value => { grants++; amount = value; });
                Assert.IsFalse(completion.TryComplete(request));
                Assert.IsTrue(session.RecordObjectiveProgress(request, request.Target, true));
                Assert.IsTrue(completion.TryComplete(request));
                Assert.IsFalse(completion.TryComplete(request));
                Assert.AreEqual(1, grants);
                Assert.AreEqual(.25f, amount);
            }
            finally { foreach (var item in owned) Object.DestroyImmediate(item); }
        }
    }
}
