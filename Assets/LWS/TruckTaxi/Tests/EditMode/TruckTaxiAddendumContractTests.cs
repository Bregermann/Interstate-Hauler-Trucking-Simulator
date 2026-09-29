using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    [Category("TaxiAddendum")]
    public sealed class TruckTaxiAddendumContractTests
    {
        [TestCase(22,true)] [TestCase(2,true)] [TestCase(7,false)] [TestCase(15,false)]
        public void RadioProgramsSupportOvernightTimeBands(int hour,bool expected)
        { Assert.That(new TruckTaxiRadioTrack { fromHour=22,untilHour=7 }.Available(hour),Is.EqualTo(expected)); }

        [Test]
        public void ColliderReviewDetectsHiddenWallButNeverMutatesIt()
        {
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                var collider=wall.GetComponent<Collider>();
                Assert.That(TruckTaxiWorldColliderReview.Suspicion(collider),Is.Null);
                wall.GetComponent<Renderer>().enabled=false;
                Assert.That(TruckTaxiWorldColliderReview.Suspicion(collider),Does.Contain("no enabled"));
                Assert.That(collider.enabled,Is.True,"Review is report-only");
                collider.isTrigger=true;
                Assert.That(TruckTaxiWorldColliderReview.Suspicion(collider),Is.Null);
                collider.isTrigger=false; collider.enabled=false;
                Assert.That(TruckTaxiWorldColliderReview.Suspicion(collider),Is.Null);
            }
            finally { UnityEngine.Object.DestroyImmediate(wall); }
        }

        [Test]
        public void KaijuEvacuationSurvivesWeatherClearWithoutCancellingNextWeek()
        {
            var go=new GameObject("Evacuation contract");
            var catalog=ScriptableObject.CreateInstance<TruckTaxiVenueCatalog>();
            try
            {
                catalog.venues=new[]{new TruckTaxiVenueDefinition {id="test.venue",displayName="Test venue",influenceRadiusMeters=250}};
                catalog.events=new[]{new TruckTaxiVenueEventDefinition {id="test.event",venueId="test.venue",displayName="Test event",
                    firstDate="2026-09-27",lastDate="2026-10-31",recurrence=TruckTaxiVenueRecurrence.Weekly,
                    startHour=20,endHour=22,arrivalLeadMinutes=90,departureMinutes=90,fareMultiplier=1.5f}};
                var host=go.AddComponent<TruckTaxiBootstrap>();
                var venue=go.AddComponent<TruckTaxiVenueRuntime>(); venue.catalog=catalog;
                DateTime now=new DateTime(2026,9,27,20,30,0);
                venue.Initialize(host,()=>now);
                Assert.That(venue.EvacuateOccurrence("test.event",now.Date),Is.True);
                Assert.That(venue.EvacuateOccurrence("test.event",now.Date),Is.False,"Idempotent evacuation");
                venue.SetSevereWeather(TruckTaxiVenueWeatherImpact.None);
                Assert.That(venue.EventsSnapshots.Single(s=>s.OccurrenceDate==now.Date).Status,Is.EqualTo(TruckTaxiVenueEventStatus.Cancelled));
                Assert.That(venue.EventsSnapshots.Single(s=>s.OccurrenceDate==now.Date.AddDays(7)).Status,Is.EqualTo(TruckTaxiVenueEventStatus.Upcoming));
                Assert.That(venue.GetWeightForPickup(Vector3.zero),Is.GreaterThan(1),"Evacuation departure demand");
                now=now.AddMinutes(61);
                Assert.That(venue.GetWeightForPickup(Vector3.zero),Is.EqualTo(1),"Evacuation pulse expires");
            }
            finally { UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(catalog); }
        }

        [Test]
        public void RadioInputsAreDistinctAndDoNotReplaceDrivingActions()
        {
            var events=new GameObject("Radio input test",typeof(UnityEngine.EventSystems.EventSystem));
            try
            {
                using var input=new TruckTaxiUIInput(events.GetComponent<UnityEngine.EventSystems.EventSystem>());
                var radio=new[]{input.RadioToggle,input.RadioNext,input.RadioPrevious,input.RadioMute,input.RadioVolumeUp,input.RadioVolumeDown};
                Assert.That(radio.Select(a=>a.bindings[0].path).Distinct().Count(),Is.EqualTo(radio.Length));
                Assert.That(input.Pause.bindings[0].path,Is.EqualTo("<Keyboard>/escape"));
                Assert.That(input.FullMap,Is.Not.Null);
            }
            finally { UnityEngine.Object.DestroyImmediate(events); }
        }
    }
}
