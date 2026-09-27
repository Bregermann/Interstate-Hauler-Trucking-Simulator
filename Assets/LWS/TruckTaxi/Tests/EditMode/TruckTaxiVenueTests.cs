using System;
using System.Linq;
using LWS.InterstateHauler;
using LWS.TruckTaxi.Editor;
using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    [Category("TaxiPolish"), Category("TaxiMegaPass")]
    public sealed class TruckTaxiVenueTests
    {
        [Test]
        public void RecurrenceCoversOnceWeeklyWeekendsWeekdaysAndDateRange()
        {
            var item = new TruckTaxiVenueEventDefinition {
                firstDate = "2026-09-27", lastDate = "2026-10-31"
            };
            var sunday = new DateTime(2026, 9, 27);
            item.recurrence = TruckTaxiVenueRecurrence.Once;
            Assert.That(item.OccursOn(sunday), Is.True);
            Assert.That(item.OccursOn(sunday.AddDays(1)), Is.False);
            item.recurrence = TruckTaxiVenueRecurrence.Weekly;
            Assert.That(item.OccursOn(sunday.AddDays(7)), Is.True);
            Assert.That(item.OccursOn(sunday.AddDays(6)), Is.False);
            item.recurrence = TruckTaxiVenueRecurrence.Weekends;
            Assert.That(item.OccursOn(sunday), Is.True);
            Assert.That(item.OccursOn(sunday.AddDays(1)), Is.False);
            item.recurrence = TruckTaxiVenueRecurrence.Weekdays;
            Assert.That(item.OccursOn(sunday), Is.False);
            Assert.That(item.OccursOn(sunday.AddDays(1)), Is.True);
            item.recurrence = TruckTaxiVenueRecurrence.SpecificWeekday;
            item.weekday = DayOfWeek.Tuesday;
            Assert.That(item.OccursOn(sunday.AddDays(2)), Is.True);
            item.recurrence = TruckTaxiVenueRecurrence.DateRange;
            Assert.That(item.OccursOn(sunday.AddDays(3)), Is.True);
            Assert.That(item.OccursOn(sunday.AddDays(-1)), Is.False);
            item.startHour = 23; item.endHour = 1;
            Assert.That(item.EndOn(sunday), Is.EqualTo(sunday.AddDays(1).AddHours(1)));
        }

        [Test]
        public void ArrivalDepartureWeatherAndForcedEventPreserveDirectionalQuotes()
        {
            var go = new GameObject("Venue test");
            var catalog = ScriptableObject.CreateInstance<TruckTaxiVenueCatalog>();
            try
            {
                var venue = new TruckTaxiVenueDefinition {
                    id = "test.venue", displayName = "Test Stadium", worldPosition = new Vector3(100, 0, 100),
                    influenceRadiusMeters = 250
                };
                catalog.venues = new[] { venue };
                catalog.events = new[] { new TruckTaxiVenueEventDefinition {
                    id = "test.event", venueId = venue.id, displayName = "Test Game",
                    firstDate = "2026-09-27", lastDate = "2026-10-31",
                    recurrence = TruckTaxiVenueRecurrence.Weekly, startHour = 20, endHour = 22,
                    arrivalLeadMinutes = 90, departureMinutes = 90,
                    fareMultiplier = 1.75f, rideFrequencyMultiplier = 1.5f
                } };
                var host = go.AddComponent<TruckTaxiBootstrap>();
                var runtime = go.AddComponent<TruckTaxiVenueRuntime>();
                runtime.catalog = catalog;
                DateTime now = new DateTime(2026, 9, 27, 19, 30, 0);
                runtime.Initialize(host, () => now);
                var near = venue.worldPosition;
                var far = new Vector3(1000, 0, 1000);
                Assert.That(runtime.EventsSnapshots.Any(s => s.Status == TruckTaxiVenueEventStatus.ArrivalSurge), Is.True);
                Assert.That(runtime.GetWeightForDestination(near), Is.GreaterThan(1));
                Assert.That(runtime.GetWeightForPickup(near), Is.EqualTo(1));
                Assert.That(runtime.GetFareQuote(far, near).EventMultiplier, Is.EqualTo(1.75f));
                Assert.That(runtime.GetFareQuote(near, far).EventMultiplier, Is.EqualTo(1));
                now = new DateTime(2026, 9, 27, 22, 30, 0);
                Assert.That(runtime.GetWeightForPickup(near), Is.GreaterThan(1));
                Assert.That(runtime.GetWeightForDestination(near), Is.EqualTo(1));
                Assert.That(runtime.GetFareQuote(near, far).EventMultiplier, Is.EqualTo(1.75f));
                runtime.SetSevereWeather(TruckTaxiVenueWeatherImpact.Cancel);
                Assert.That(runtime.EventsSnapshots.Any(s => s.Status == TruckTaxiVenueEventStatus.Cancelled), Is.True);
                Assert.That(runtime.EventsSnapshots.Single(s => s.OccurrenceDate == new DateTime(2026, 10, 4)).Status,
                    Is.EqualTo(TruckTaxiVenueEventStatus.Upcoming), "next week's occurrence must not inherit cancellation");
                Assert.That(runtime.GetFareQuote(near, far).EventMultiplier, Is.EqualTo(1.75f),
                    "cancellation itself should create a bounded departure pickup pulse");
                now = now.AddMinutes(30);
                runtime.SetSevereWeather(TruckTaxiVenueWeatherImpact.Cancel);
                now = now.AddMinutes(31);
                Assert.That(runtime.GetFareQuote(near, far).EventMultiplier, Is.EqualTo(1));
                now = new DateTime(2026, 10, 4, 19, 30, 0);
                Assert.That(runtime.GetFareQuote(far, near).EventMultiplier, Is.EqualTo(1.75f),
                    "the following weekly event must retain its arrival surge");
                now = new DateTime(2026, 10, 4, 10, 0, 0);
                runtime.SetSevereWeather(TruckTaxiVenueWeatherImpact.Delay);
                Assert.That(runtime.EventsSnapshots.Single(s => s.OccurrenceDate == now.Date).Status,
                    Is.EqualTo(TruckTaxiVenueEventStatus.Delayed));
                runtime.SetSevereWeather(TruckTaxiVenueWeatherImpact.Postpone);
                Assert.That(runtime.EventsSnapshots.Single(s => s.OccurrenceDate == now.Date).Status,
                    Is.EqualTo(TruckTaxiVenueEventStatus.Postponed));
                runtime.SetSevereWeather(TruckTaxiVenueWeatherImpact.None);
                Assert.That(runtime.EventsSnapshots.Single(s => s.OccurrenceDate == now.Date).Status,
                    Is.EqualTo(TruckTaxiVenueEventStatus.Upcoming));
                now = new DateTime(2026, 9, 28, 10, 0, 0);
                Assert.That(runtime.ForceStart("test.event"), Is.True);
                Assert.That(runtime.EventsSnapshots.Any(s => s.Status == TruckTaxiVenueEventStatus.InProgress), Is.True);
                Assert.That(runtime.ForceEnd("test.event"), Is.True);
                Assert.That(runtime.EventsSnapshots.Any(s => s.Status == TruckTaxiVenueEventStatus.DepartureSurge), Is.True);
                Assert.That(runtime.GetNextSevenDays(), Is.Not.Empty);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(catalog); }
        }

        [Test]
        public void AllNewStopsRemainInTheirChunksAndRoadGraphReturnsToHighway()
        {
            foreach (var stop in TruckTaxiVenueLayout.StadiumStops)
                Assert.That(TruckTaxiVenueLayout.StadiumBounds.Contains(stop.Position), Is.True, stop.Id);
            foreach (var stop in TruckTaxiVenueLayout.ConcertStops)
                Assert.That(TruckTaxiVenueLayout.ConcertBounds.Contains(stop.Position), Is.True, stop.Id);
            foreach (var stop in TruckTaxiVenueLayout.MountainStops)
                Assert.That(TruckTaxiVenueLayout.MountainBounds.Contains(stop.Position), Is.True, stop.Id);
            var graph = new LwsRoadGraph { graphId = "venue-test" };
            graph.nodes.Add(new LwsRoadNode { nodeId = "taxi.regional.1080", position = new Vector3(1080, .14f, 0) });
            graph.nodes.Add(new LwsRoadNode { nodeId = "taxi.regional.3640", position = new Vector3(3640, .14f, 0) });
            TruckTaxiVenueLayout.AddToGraph(graph);
            int edges = graph.edges.Count;
            TruckTaxiVenueLayout.AddToGraph(graph);
            Assert.That(graph.edges.Count, Is.EqualTo(edges));
            Assert.That(graph.edges.Any(e => e.edgeId == TruckTaxiVenueLayout.MountainHazardRoadEdgeId), Is.True);
            Assert.That(graph.Validate().IsValid, Is.True, graph.Validate().Summary);
            var routes = new TruckTaxiRouteDistanceService(graph);
            foreach (var stop in TruckTaxiVenueLayout.StadiumStops.Concat(TruckTaxiVenueLayout.MountainStops))
            {
                Assert.That(routes.Measure(new Vector3(1080, .14f, 0), stop.Position).Navigable, Is.True, stop.Id);
                Assert.That(routes.Measure(stop.Position, new Vector3(1080, .14f, 0)).Navigable, Is.True, stop.Id);
            }
            foreach (var stop in TruckTaxiVenueLayout.ConcertStops)
                Assert.That(routes.Measure(new Vector3(3640, .14f, 0), stop.Position).Navigable, Is.True, stop.Id);
        }

        [Test]
        public void VenueAccessRoadsProduceValidUtsLaneDefinitions()
        {
            foreach (var specification in new[] {
                ("taxi.stadium.access.loop", TruckTaxiVenueLayout.StadiumRoad),
                ("taxi.concert.access.loop", TruckTaxiVenueLayout.ConcertRoad),
                ("taxi.mountainpass.access.loop", TruckTaxiVenueLayout.MountainRoad)
            })
            {
                var lane = TruckTaxiMegaWorldAuthoring.CreateAccessLane(specification.Item1, specification.Item2);
                Assert.That(lane.Validate(out string message), Is.True, message);
                Assert.That(lane.centerline.Length, Is.GreaterThan(8));
                Assert.That(Vector3.Distance(lane.centerline[0], lane.centerline[lane.centerline.Length - 1]),
                    Is.LessThan(.01f));
            }
        }
    }
}
