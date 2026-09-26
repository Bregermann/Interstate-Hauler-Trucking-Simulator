using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    public class TruckTaxiLateDemandTests
    {
        private readonly List<UnityEngine.Object> created = new List<UnityEngine.Object>();
        private TruckTaxiConfiguration config;
        private PassengerProfile ordinary;
        private TruckTaxiRideLocation[] stops;

        [SetUp] public void SetUp()
        {
            config=ScriptableObject.CreateInstance<TruckTaxiConfiguration>(); created.Add(config);
            ordinary=NewPassenger("ordinary");
            config.passengers=new[]{ordinary};
            config.minimumTripDistance=10;
            config.maximumTripDistance=1000;
            config.recentOfferWeight=1;
            stops=new TruckTaxiRideLocation[2];
            for(int i=0;i<stops.Length;i++)
            {
                var go=new GameObject("Demand stop "+i); created.Add(go);
                stops[i]=go.AddComponent<TruckTaxiRideLocation>();
                stops[i].locationId="demand."+i;
                go.transform.position=Vector3.right*(100*i);
            }
        }

        [TearDown] public void TearDown()
        {
            foreach(var item in created) UnityEngine.Object.DestroyImmediate(item);
            created.Clear();
        }

        private PassengerProfile NewPassenger(string id)
        {
            var profile=ScriptableObject.CreateInstance<PassengerProfile>(); created.Add(profile);
            profile.passengerId=id;
            return profile;
        }

        private TruckTaxiSession NewSession(float hour, TruckTaxiDemandWeather weather=TruckTaxiDemandWeather.Neutral)
        {
            var session=new TruckTaxiSession(config,stops,47);
            session.SetWorldConditions(0,hour,weather);
            session.StartShift();
            return session;
        }

        [TestCase(21f,0.85f,1.1f)]
        [TestCase(22f,1.5f,1.25f)]
        [TestCase(0f,1.5f,1.25f)]
        [TestCase(2.99f,1.5f,1.25f)]
        [TestCase(3f,2f,1.35f)]
        [TestCase(5.99f,2f,1.35f)]
        [TestCase(6f,0.75f,1.1f)]
        [TestCase(12f,1f,1f)]
        public void TimeWindowControlsOfferIntervalAndFare(float hour, float frequency, float fare)
        {
            var session=NewSession(hour);
            float interval=config.rideFrequency*frequency;
            session.Tick(interval-.05f,Vector3.zero,0,0,true);
            Assert.AreEqual(TruckTaxiState.Available,session.State);
            session.Tick(.1f,Vector3.zero,0,0,true);
            Assert.AreEqual(TruckTaxiState.RideOffered,session.State);
            Assert.That(session.DemandFareMultiplier,Is.EqualTo(fare).Within(.001f));
            Assert.AreEqual((long)Math.Round(session.Offer.EstimatedFareCents*fare),session.DemandEstimatedFareCents);
            Assert.IsTrue(session.AcceptRide());
            Assert.AreEqual((long)Math.Round(config.baseFareCents*fare),session.EstimateFare().Base);
        }

        [TestCase(23f,true)]
        [TestCase(4f,false)]
        public void WeatherStillMultipliesOvernightDemand(float hour, bool lateNight)
        {
            var session=NewSession(hour,TruckTaxiDemandWeather.Storm);
            float frequency=(lateNight ? config.lateNightFrequencyMultiplier : config.preDawnFrequencyMultiplier)*
                config.stormFrequencyMultiplier;
            session.Tick(config.rideFrequency*frequency+.01f,Vector3.zero,0,0,true);
            Assert.AreEqual(TruckTaxiState.RideOffered,session.State);
            float fare=(lateNight ? config.lateNightFareMultiplier : config.preDawnFareMultiplier)*
                config.stormFareMultiplier;
            Assert.That(session.DemandFareMultiplier,Is.EqualTo(fare).Within(.001f));
        }

        [TestCase(23f,"rare")]
        [TestCase(23f,"weird")]
        [TestCase(23f,"nightlife")]
        [TestCase(23f,"chaotic")]
        [TestCase(23f,"flirtatious")]
        [TestCase(4f,"rare")]
        [TestCase(4f,"weird")]
        [TestCase(4f,"nightlife")]
        [TestCase(4f,"chaotic")]
        [TestCase(4f,"flirtatious")]
        public void OvernightWeightsFavorAuthoredPassengerTypes(float hour, string kind)
        {
            var featured=NewPassenger(kind);
            if(kind=="rare") featured.rarity=TruckTaxiRarity.Rare;
            else if(kind=="flirtatious") featured.flirtatiousPresentation=true;
            else if(kind=="chaotic") featured.chaosAffinity=1;
            else featured.specialTraits=new[]{kind};
            config.passengers=new[]{ordinary,featured};
            bool late=hour>=22;
            int baseline=CountOffers(hour,featured,120);
            int daytime=CountOffers(12,featured,120);
            SetWeight(late,kind,20);
            int boosted=CountOffers(hour,featured,120);
            Assert.Greater(boosted,baseline+20,kind+" should be favored in its overnight window.");
            Assert.AreEqual(daytime,CountOffers(12,featured,120),"Overnight weights must not affect daytime.");
        }

        private int CountOffers(float hour, PassengerProfile target, int count)
        {
            var session=NewSession(hour);
            int selected=0;
            for(int i=0;i<count;i++)
            {
                Assert.IsTrue(session.OfferRide());
                if(session.Passenger==target) selected++;
                session.DeclineRide();
            }
            return selected;
        }

        private void SetWeight(bool late, string kind, float value)
        {
            switch(kind)
            {
                case "rare": if(late) config.lateNightRareWeight=value; else config.preDawnRareWeight=value; break;
                case "weird": if(late) config.lateNightWeirdWeight=value; else config.preDawnWeirdWeight=value; break;
                case "nightlife": if(late) config.lateNightNightlifeWeight=value; else config.preDawnNightlifeWeight=value; break;
                case "chaotic": if(late) config.lateNightChaoticWeight=value; else config.preDawnChaoticWeight=value; break;
                case "flirtatious": if(late) config.lateNightFlirtatiousWeight=value; else config.preDawnFlirtatiousWeight=value; break;
            }
        }

        [TestCase(23f,true)]
        [TestCase(4f,false)]
        public void OvernightAppreciationMultiplierUsesAuthoredAdultGate(float hour, bool lateNight)
        {
            var casting=ScriptableObject.CreateInstance<TruckTaxiCastingProfile>(); created.Add(casting);
            ordinary.casting=casting;
            ordinary.specialAppreciationEligible=true;
            ordinary.explicitlyAdult=true;
            ordinary.minimumAdultAge=21;
            ordinary.adultFemalePresentation=true;
            ordinary.flirtatiousPresentation=true;
            ordinary.baseSatisfaction=5;
            ordinary.appreciationChance=0;
            config.appreciationBaseChance=.5f;
            if(lateNight) config.lateNightAppreciationMultiplier=0;
            else config.preDawnAppreciationMultiplier=0;
            var noOffer=NewSession(hour);
            Assert.IsTrue(noOffer.OfferRide(ordinary));
            Assert.IsTrue(noOffer.AcceptRide());
            Board(noOffer);
            noOffer.DebugComplete();
            Assert.AreEqual(TruckTaxiState.RideComplete,noOffer.State);

            if(lateNight) config.lateNightAppreciationMultiplier=2;
            else config.preDawnAppreciationMultiplier=2;
            var offered=NewSession(hour);
            Assert.IsTrue(offered.OfferRide(ordinary));
            Assert.IsTrue(offered.AcceptRide());
            Board(offered);
            offered.DebugComplete();
            Assert.AreEqual(TruckTaxiState.AppreciationOffer,offered.State);
        }

        private static void Board(TruckTaxiSession session)
        {
            session.Tick(.1f,session.Pickup.StopPosition,0,0,true);
            session.Tick(2,session.Pickup.StopPosition,0,0,true);
            Assert.AreEqual(TruckTaxiState.DrivingToDestination,session.State);
        }
    }
}
