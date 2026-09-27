using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public readonly struct TruckTaxiVenueEventSnapshot
    {
        public readonly string EventId, VenueId, EventName, VenueName, OccurrenceKey;
        public readonly DateTime OccurrenceDate;
        public readonly TruckTaxiVenueEventType Type;
        public readonly TruckTaxiVenueEventStatus Status;
        public readonly DateTime Start, End, ArrivalStart, DepartureEnd;
        public readonly Vector3 Position;
        public readonly float AttendanceScale, FareMultiplier, TrafficMultiplier, CrowdMultiplier, RideFrequencyMultiplier;
        public TruckTaxiVenueEventSnapshot(TruckTaxiVenueEventDefinition item, TruckTaxiVenueDefinition venue,
            TruckTaxiVenueEventStatus status, DateTime occurrenceDate, DateTime start, DateTime end)
        {
            EventId = item.id; VenueId = venue.id; EventName = item.displayName; VenueName = venue.displayName;
            OccurrenceDate = occurrenceDate.Date;
            OccurrenceKey = TruckTaxiVenueRuntime.OccurrenceKey(item.id, occurrenceDate);
            Type = item.type; Status = status; Start = start; End = end;
            ArrivalStart = start.AddMinutes(-item.arrivalLeadMinutes);
            DepartureEnd = end.AddMinutes(item.departureMinutes);
            Position = venue.worldPosition; AttendanceScale = item.attendanceScale;
            FareMultiplier = item.fareMultiplier; TrafficMultiplier = item.trafficMultiplier;
            CrowdMultiplier = item.crowdMultiplier; RideFrequencyMultiplier = item.rideFrequencyMultiplier;
        }
    }

    public readonly struct TruckTaxiVenueDemand
    {
        public readonly float RideFrequencyMultiplier, TrafficMultiplier, CrowdMultiplier;
        public readonly float PickupWeight, DestinationWeight;
        public TruckTaxiVenueDemand(float rides, float traffic, float crowd, float pickup, float destination)
        { RideFrequencyMultiplier = rides; TrafficMultiplier = traffic; CrowdMultiplier = crowd;
          PickupWeight = pickup; DestinationWeight = destination; }
    }

    public readonly struct TruckTaxiVenueFareQuote
    {
        public readonly float EventMultiplier;
        public readonly string EventLabel, EventId, VenueId;
        public TruckTaxiVenueFareQuote(float multiplier, string label, string eventId, string venueId)
        { EventMultiplier = multiplier; EventLabel = label; EventId = eventId; VenueId = venueId; }
    }

    public readonly struct TruckTaxiVenueAlert
    {
        public readonly string EventId, VenueId, Title, Message;
        public readonly DateTime GameTime;
        public TruckTaxiVenueAlert(string eventId, string venueId, string title, string message, DateTime gameTime)
        { EventId = eventId; VenueId = venueId; Title = title; Message = message; GameTime = gameTime; }
    }

    // Calendar and game-specific venue semantics only. The caller supplies the existing game clock.
    [DisallowMultipleComponent]
    public sealed class TruckTaxiVenueRuntime : MonoBehaviour
    {
        private sealed class WeatherDecision
        {
            public TruckTaxiVenueWeatherImpact Impact;
            public DateTime PulseStart, PulseEnd;
        }
        public TruckTaxiVenueCatalog catalog;
        public IReadOnlyList<TruckTaxiVenueDefinition> Venues => catalog != null ? catalog.venues : Array.Empty<TruckTaxiVenueDefinition>();
        public IReadOnlyList<TruckTaxiVenueEventSnapshot> EventsSnapshots { get { Refresh(); return snapshots; } }
        public event Action<TruckTaxiVenueAlert> AlertQueued;
        private readonly List<TruckTaxiVenueEventSnapshot> snapshots = new List<TruckTaxiVenueEventSnapshot>();
        private readonly Queue<TruckTaxiVenueAlert> alerts = new Queue<TruckTaxiVenueAlert>();
        private readonly Dictionary<string, WeatherDecision> weather = new Dictionary<string, WeatherDecision>(StringComparer.Ordinal);
        private readonly Dictionary<string, DateTime> forcedStarts = new Dictionary<string, DateTime>(StringComparer.Ordinal);
        private readonly Dictionary<string, DateTime> forcedEnds = new Dictionary<string, DateTime>(StringComparer.Ordinal);
        private readonly HashSet<string> announced = new HashSet<string>(StringComparer.Ordinal);
        private Func<DateTime> clock;
        private DateTime refreshedMinute;
        private DateTime lastAlertTime = DateTime.MinValue;
        private float nextRealtimeRefresh;
        private bool logicalVenueRegionsRegistered;

        public void Initialize(TruckTaxiBootstrap host, Func<DateTime> gameNow)
        {
            if (host == null || gameNow == null) throw new ArgumentNullException(host == null ? nameof(host) : nameof(gameNow));
            if (catalog == null) catalog = Resources.Load<TruckTaxiVenueCatalog>(TruckTaxiVenueCatalog.ResourcePath);
            if (catalog == null) throw new InvalidOperationException("Truck Taxi venue catalog missing; run Mega World authoring.");
            catalog.Validate();
            if (!logicalVenueRegionsRegistered && host.pedestrians != null)
            {
                var world = host.GetComponent<TruckTaxiRegionalWorld>();
                if (world != null)
                    foreach (var venue in catalog.venues)
                    {
                        if (venue.type != TruckTaxiVenueType.Stadium &&
                            venue.type != TruckTaxiVenueType.ConcertArena) continue;
                        foreach (var region in world.regions)
                            if (region.id == venue.regionId)
                            {
                                var crowdBounds = new Bounds(venue.worldPosition, new Vector3(350, 40, 400));
                                if (region.bounds.Contains(crowdBounds.min) && region.bounds.Contains(crowdBounds.max))
                                    host.pedestrians.RegisterLogicalRegion(venue.id, crowdBounds);
                                break;
                            }
                    }
                logicalVenueRegionsRegistered = true;
            }
            clock = gameNow;
            refreshedMinute = DateTime.MinValue;
            Refresh();
        }

        private void Update()
        {
            if (clock == null || Time.unscaledTime < nextRealtimeRefresh) return;
            nextRealtimeRefresh = Time.unscaledTime + 1;
            Refresh();
        }

        public IReadOnlyList<TruckTaxiVenueEventSnapshot> GetNextSevenDays()
        {
            Refresh();
            return snapshots.ToArray();
        }

        public TruckTaxiVenueDemand GetDemand(Vector3 position)
        {
            if (clock == null) return new TruckTaxiVenueDemand(1, 1, 1, 1, 1);
            Refresh();
            float rides = DayDemand(clock()), traffic = 1, crowd = 1, pickup = 1, destination = 1;
            foreach (var snapshot in snapshots)
            {
                var venue = FindVenue(snapshot.VenueId);
                if (venue == null) continue;
                float reach = Reach(position, venue);
                if (reach <= 0) continue;
                if (snapshot.Status == TruckTaxiVenueEventStatus.ArrivalSurge)
                {
                    destination = Mathf.Max(destination, 1 + (snapshot.RideFrequencyMultiplier - 1) * reach * 2);
                    rides = Mathf.Max(rides, 1 + (snapshot.RideFrequencyMultiplier - 1) * reach);
                }
                if (snapshot.Status == TruckTaxiVenueEventStatus.DepartureSurge ||
                    snapshot.Status == TruckTaxiVenueEventStatus.Cancelled && CancellationPulseActive(snapshot, clock()))
                {
                    pickup = Mathf.Max(pickup, 1 + (snapshot.RideFrequencyMultiplier - 1) * reach * 2);
                    rides = Mathf.Max(rides, 1 + (snapshot.RideFrequencyMultiplier - 1) * reach);
                }
                if (snapshot.Status == TruckTaxiVenueEventStatus.ArrivalSurge ||
                    snapshot.Status == TruckTaxiVenueEventStatus.InProgress ||
                    snapshot.Status == TruckTaxiVenueEventStatus.DepartureSurge)
                {
                    traffic = Mathf.Max(traffic, 1 + (snapshot.TrafficMultiplier - 1) * reach * snapshot.AttendanceScale);
                    crowd = Mathf.Max(crowd, 1 + (snapshot.CrowdMultiplier - 1) * reach * snapshot.AttendanceScale);
                }
            }
            return new TruckTaxiVenueDemand(rides, traffic, crowd, pickup, destination);
        }

        public float GetWeightForPickup(Vector3 position) => GetDemand(position).PickupWeight;
        public float GetWeightForDestination(Vector3 position) => GetDemand(position).DestinationWeight;
        public TruckTaxiVenueFareQuote GetFareQuote(Vector3 pickup, Vector3 destination)
        {
            Refresh();
            var best = new TruckTaxiVenueFareQuote(1, null, null, null);
            foreach (var snapshot in snapshots)
            {
                var venue = FindVenue(snapshot.VenueId);
                if (venue == null) continue;
                bool applies = snapshot.Status == TruckTaxiVenueEventStatus.ArrivalSurge && Reach(destination, venue) > 0 ||
                    snapshot.Status == TruckTaxiVenueEventStatus.DepartureSurge && Reach(pickup, venue) > 0 ||
                    snapshot.Status == TruckTaxiVenueEventStatus.Cancelled &&
                        CancellationPulseActive(snapshot, clock()) && Reach(pickup, venue) > 0;
                if (applies && snapshot.FareMultiplier > best.EventMultiplier)
                    best = new TruckTaxiVenueFareQuote(snapshot.FareMultiplier, snapshot.EventName + " - " + snapshot.VenueName,
                        snapshot.EventId, snapshot.VenueId);
            }
            return best;
        }

        public bool ForceStart(string eventId)
        {
            if (FindEvent(eventId) == null || clock == null) return false;
            forcedEnds.Remove(eventId); forcedStarts[eventId] = clock();
            Invalidate(); return true;
        }
        public bool ForceEnd(string eventId)
        {
            if (FindEvent(eventId) == null || clock == null) return false;
            forcedStarts.Remove(eventId); forcedEnds[eventId] = clock();
            Invalidate(); return true;
        }
        public void SetSevereWeather(TruckTaxiVenueWeatherImpact impact, string eventId = null)
        {
            if (catalog == null || clock == null) return;
            DateTime now = clock();
            bool changed = false;
            foreach (var item in catalog.events)
            {
                if (eventId != null && item.id != eventId) continue;
                DateTime? date = FindEligibleOccurrence(item, now);
                if (!date.HasValue) continue;
                string key = OccurrenceKey(item.id, date.Value);
                if (impact == TruckTaxiVenueWeatherImpact.None)
                {
                    changed |= weather.Remove(key);
                    continue;
                }
                if (weather.TryGetValue(key, out var existing) && existing.Impact == impact) continue;
                var decision = new WeatherDecision { Impact = impact };
                if (impact == TruckTaxiVenueWeatherImpact.Cancel)
                {
                    DateTime arrival = item.StartOn(date.Value).AddMinutes(-item.arrivalLeadMinutes);
                    decision.PulseStart = now > arrival ? now : arrival;
                    decision.PulseEnd = decision.PulseStart.AddMinutes(60);
                }
                weather[key] = decision;
                changed = true;
            }
            if (changed) Invalidate();
        }
        public bool TryDequeueAlert(out TruckTaxiVenueAlert alert)
        {
            if (clock == null) { alert = default; return false; }
            Refresh();
            if (clock() < lastAlertTime) lastAlertTime = DateTime.MinValue;
            if (alerts.Count == 0 || clock() < lastAlertTime.AddMinutes(5)) { alert = default; return false; }
            alert = alerts.Dequeue(); lastAlertTime = clock(); AlertQueued?.Invoke(alert); return true;
        }

        private void Invalidate() { refreshedMinute = DateTime.MinValue; Refresh(); }
        private void Refresh()
        {
            if (clock == null || catalog == null) return;
            DateTime now = clock();
            var minute = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0);
            if (minute == refreshedMinute) return;
            refreshedMinute = minute; snapshots.Clear();
            foreach (var item in catalog.events)
            {
                var venue = FindVenue(item.venueId);
                if (venue == null) continue;
                for (int day = -1; day <= 7; day++)
                {
                    DateTime date = now.Date.AddDays(day);
                    if (!item.OccursOn(date) && !(day == 0 &&
                        (forcedStarts.ContainsKey(item.id) || forcedEnds.ContainsKey(item.id)))) continue;
                    DateTime start = item.StartOn(date), end = item.EndOn(date);
                    var key = OccurrenceKey(item.id, date);
                    var impact = weather.TryGetValue(key, out var decision) ? decision.Impact : TruckTaxiVenueWeatherImpact.None;
                    if (impact != TruckTaxiVenueWeatherImpact.None)
                    {
                        if (impact == TruckTaxiVenueWeatherImpact.Delay) { start = start.AddHours(1); end = end.AddHours(1); }
                        if (impact == TruckTaxiVenueWeatherImpact.Postpone) { start = start.AddDays(1); end = end.AddDays(1); }
                    }
                    if (forcedStarts.TryGetValue(item.id, out var forced) && day == 0)
                    { start = forced; end = forced.Add(item.EndOn(date) - item.StartOn(date)); }
                    if (forcedEnds.TryGetValue(item.id, out var ended) && day == 0)
                    { end = ended; start = ended.Subtract(item.EndOn(date) - item.StartOn(date)); }
                    var status = Status(item, now, start, end, impact);
                    if (day < 0 && status == TruckTaxiVenueEventStatus.Completed) continue;
                    if (day == 7 && start > now.AddDays(7)) continue;
                    var snapshot = new TruckTaxiVenueEventSnapshot(item, venue, status, date, start, end);
                    snapshots.Add(snapshot);
                    Announce(item, snapshot, now);
                }
            }
            snapshots.Sort((a, b) => a.Start.CompareTo(b.Start));
        }

        private static TruckTaxiVenueEventStatus Status(TruckTaxiVenueEventDefinition item, DateTime now, DateTime start,
            DateTime end, TruckTaxiVenueWeatherImpact impact)
        {
            if (impact == TruckTaxiVenueWeatherImpact.Cancel) return TruckTaxiVenueEventStatus.Cancelled;
            if (now < start.AddMinutes(-item.arrivalLeadMinutes))
                return impact == TruckTaxiVenueWeatherImpact.Delay ? TruckTaxiVenueEventStatus.Delayed :
                    impact == TruckTaxiVenueWeatherImpact.Postpone ? TruckTaxiVenueEventStatus.Postponed :
                    TruckTaxiVenueEventStatus.Upcoming;
            if (now < start) return TruckTaxiVenueEventStatus.ArrivalSurge;
            if (now < end) return TruckTaxiVenueEventStatus.InProgress;
            if (now < end.AddMinutes(item.departureMinutes)) return TruckTaxiVenueEventStatus.DepartureSurge;
            return TruckTaxiVenueEventStatus.Completed;
        }
        private void Announce(TruckTaxiVenueEventDefinition item, TruckTaxiVenueEventSnapshot snapshot, DateTime now)
        {
            bool weatherChange = snapshot.Status == TruckTaxiVenueEventStatus.Cancelled ||
                snapshot.Status == TruckTaxiVenueEventStatus.Delayed ||
                snapshot.Status == TruckTaxiVenueEventStatus.Postponed;
            if ((!weatherChange && snapshot.Start > now.AddDays(1)) ||
                snapshot.End < now.AddMinutes(-120)) return;
            if (snapshot.Status == TruckTaxiVenueEventStatus.Cancelled)
                QueueAlert(item, snapshot, now, "cancelled", "EVENT CANCELLED");
            if (snapshot.Status == TruckTaxiVenueEventStatus.Delayed)
                QueueAlert(item, snapshot, now, "delayed", "EVENT DELAYED");
            if (snapshot.Status == TruckTaxiVenueEventStatus.Postponed)
                QueueAlert(item, snapshot, now, "postponed", "EVENT POSTPONED");
            if (snapshot.Status == TruckTaxiVenueEventStatus.ArrivalSurge)
                QueueAlert(item, snapshot, now, "arrival", "EVENT ARRIVAL SURGE");
            if (now >= snapshot.Start.AddMinutes(-item.announcementLeadMinutes) && now < snapshot.Start &&
                snapshot.Status != TruckTaxiVenueEventStatus.Cancelled)
                QueueAlert(item, snapshot, now, "soon", "EVENT STARTING SOON");
            if (snapshot.Status == TruckTaxiVenueEventStatus.InProgress)
                QueueAlert(item, snapshot, now, "started", "EVENT IN PROGRESS");
            if (now >= snapshot.End.AddMinutes(-item.announcementLeadMinutes) && now < snapshot.End &&
                snapshot.Status == TruckTaxiVenueEventStatus.InProgress)
                QueueAlert(item, snapshot, now, "ending", "EVENT ENDING SOON");
            if (snapshot.Status == TruckTaxiVenueEventStatus.DepartureSurge)
                QueueAlert(item, snapshot, now, "departure", "EVENT LETTING OUT");
        }
        private void QueueAlert(TruckTaxiVenueEventDefinition item, TruckTaxiVenueEventSnapshot snapshot,
            DateTime now, string state, string title)
        {
            string key = snapshot.OccurrenceKey + ":" + state;
            if (!announced.Add(key)) return;
            var alert = new TruckTaxiVenueAlert(item.id, item.venueId, title,
                snapshot.EventName + " at " + snapshot.VenueName, now);
            if (alerts.Count >= 16) alerts.Dequeue();
            alerts.Enqueue(alert);
        }
        private TruckTaxiVenueDefinition FindVenue(string id)
        {
            if (catalog == null) return null;
            foreach (var venue in catalog.venues) if (venue.id == id) return venue;
            return null;
        }
        private TruckTaxiVenueEventDefinition FindEvent(string id)
        {
            if (catalog == null) return null;
            foreach (var item in catalog.events) if (item.id == id) return item;
            return null;
        }
        public static string OccurrenceKey(string eventId, DateTime date) =>
            eventId + "@" + date.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
        private bool CancellationPulseActive(TruckTaxiVenueEventSnapshot snapshot, DateTime now)
        {
            return weather.TryGetValue(snapshot.OccurrenceKey, out var decision) &&
                decision.Impact == TruckTaxiVenueWeatherImpact.Cancel &&
                now >= decision.PulseStart && now < decision.PulseEnd;
        }
        private DateTime? FindEligibleOccurrence(TruckTaxiVenueEventDefinition item, DateTime now)
        {
            DateTime? best = null;
            double bestScore = double.PositiveInfinity;
            for (int day = -1; day <= 1; day++)
            {
                DateTime date = now.Date.AddDays(day);
                if (!item.OccursOn(date) && !(day == 0 &&
                    (forcedStarts.ContainsKey(item.id) || forcedEnds.ContainsKey(item.id)))) continue;
                DateTime start = item.StartOn(date), end = item.EndOn(date);
                if (day == 0 && forcedStarts.TryGetValue(item.id, out var forced))
                { start = forced; end = forced.Add(item.EndOn(date) - item.StartOn(date)); }
                if (now < start.AddHours(-24) || now > end.AddMinutes(item.departureMinutes)) continue;
                double score = now >= start.AddMinutes(-item.arrivalLeadMinutes) && now <= end ? 0 :
                    Math.Abs((start - now).TotalMinutes);
                if (score >= bestScore) continue;
                best = date; bestScore = score;
            }
            return best;
        }
        private static float Reach(Vector3 position, TruckTaxiVenueDefinition venue)
        {
            position.y = venue.worldPosition.y;
            float distance = Vector3.Distance(position, venue.worldPosition);
            return distance <= venue.influenceRadiusMeters ? 1 - distance / (venue.influenceRadiusMeters * 1.5f) : 0;
        }
        private static float DayDemand(DateTime now)
        {
            if (now.DayOfWeek == DayOfWeek.Friday && now.Hour >= 18) return 1.2f;
            if (now.DayOfWeek == DayOfWeek.Saturday && now.Hour >= 17) return 1.15f;
            if (now.DayOfWeek >= DayOfWeek.Monday && now.DayOfWeek <= DayOfWeek.Friday &&
                now.Hour >= 7 && now.Hour < 10) return 1.15f;
            return 1;
        }
    }
}
