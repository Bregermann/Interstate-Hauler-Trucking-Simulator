using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace LWS.TruckTaxi
{
    public static class TruckTaxiAnalyticsCsv
    {
        private sealed class Passenger
        {
            public string Id, LastSession;
            public TruckTaxiAnalyticsArchetype Archetype;
            public int Offered, Accepted, Completed, Cancelled, Ejected, Failed, Rated, PatienceSamples, FareSamples;
            public double LastGameMinutes, LastRealSeconds, Rating, Patience, Fare;
        }
        private static string Row(params object[] values) => string.Join(",", values.Select(value =>
            TruckTaxiAnalyticsPrivacy.Csv(Convert.ToString(value, CultureInfo.InvariantCulture))));

        // Called only by the file worker or explicit tests. Exports replace six bounded files.
        public static void Export(string analyticsDirectory)
        {
            string directory = Path.Combine(analyticsDirectory, "Exports");
            Directory.CreateDirectory(directory);
            string[] names = { "Sessions", "Passengers", "Rides", "Objectives", "Performance", "WorldEvents" };
            var writers = new Dictionary<string, StreamWriter>();
            try
            {
                foreach (string name in names) writers[name] = new StreamWriter(Path.Combine(directory, name + ".csv.tmp"), false, new UTF8Encoding(false));
                const string common = "schema_version,game_version,build_id,install_id,session_id,event_id,event,real_seconds,game_minutes,";
                writers["Sessions"].WriteLine(common + "play_seconds,pause_seconds,driving_seconds,stopped_seconds,distance_meters,max_speed_mps,average_speed_mps");
                writers["Rides"].WriteLine(common + "passenger_id,ride_id,outcome,pickup_seconds,ride_seconds,distance_meters,fare_cents,tip_cents,stars,patience,intercity,demand_multiplier,event_bonus_cents,weather_bonus_cents");
                writers["Objectives"].WriteLine(common + "passenger_id,ride_id,request_type,state,elapsed_seconds");
                writers["Performance"].WriteLine(common + "region_id,paused,fps,frame_ms,main_thread_ms,physics_ms,live_cars,live_pedestrians,live_rivals,loaded_regions");
                writers["WorldEvents"].WriteLine(common + "region_id,passenger_id,code,outcome,candidate_count,base_weight,final_weight,unseen,recent_penalty,archetype_modifier,event_modifier,custom_content_id,custom_item_id,custom_event_id");
                writers["Passengers"].WriteLine("passenger_id,archetype,offered,accepted,completed,cancelled,ejected,failed,last_session_id,last_game_minutes,last_real_seconds,average_rating,average_patience,average_fare_cents");
                var passengers = new Dictionary<string, Passenger>(StringComparer.Ordinal);
                var eventIds = new HashSet<string>(StringComparer.Ordinal);
                var files = Directory.GetFiles(analyticsDirectory, "events-*.jsonl").OrderBy(x => x, StringComparer.Ordinal).TakeLast(TruckTaxiAnalyticsStorage.JournalFileLimit);
                foreach (string file in files)
                    foreach (var e in TruckTaxiAnalyticsStorage.ReadEvents(file))
                    {
                        if (!eventIds.Add(e.eventId)) continue;
                        string prefix = Row(e.schemaVersion, e.gameVersion, e.buildId, e.installId, e.sessionId, e.eventId, e.kind, e.realSeconds, e.gameMinutes) + ",";
                        switch (e.kind)
                        {
                            case TruckTaxiAnalyticsKind.SessionStarted:
                            case TruckTaxiAnalyticsKind.SessionSnapshot:
                            case TruckTaxiAnalyticsKind.SessionEnded:
                                writers["Sessions"].WriteLine(prefix + Row(e.playSeconds, e.pauseSeconds, e.drivingSeconds, e.stoppedSeconds, e.distanceMeters, e.maxSpeedMps, e.averageSpeedMps)); break;
                            case TruckTaxiAnalyticsKind.OfferGenerated:
                            case TruckTaxiAnalyticsKind.OfferAccepted:
                            case TruckTaxiAnalyticsKind.OfferClosedUnknown:
                            case TruckTaxiAnalyticsKind.OfferDeclined:
                            case TruckTaxiAnalyticsKind.OfferExpired:
                            case TruckTaxiAnalyticsKind.PickupCompleted:
                            case TruckTaxiAnalyticsKind.RideFinished:
                                writers["Rides"].WriteLine(prefix + Row(e.passengerId, e.rideId, e.kind == TruckTaxiAnalyticsKind.RideFinished ? ((TruckTaxiRideOutcome)e.outcome).ToString() : "", e.pickupSeconds, e.rideSeconds, e.distanceMeters,
                                    e.fareCents, e.tipCents, e.rating, e.patience, e.intercity, e.demandMultiplier, e.eventBonusCents, e.weatherBonusCents)); break;
                            case TruckTaxiAnalyticsKind.ObjectiveCreated:
                            case TruckTaxiAnalyticsKind.ObjectiveResolved:
                            case TruckTaxiAnalyticsKind.DiversionOffered:
                                writers["Objectives"].WriteLine(prefix + Row(e.passengerId, e.rideId, e.code, e.outcome, e.rideSeconds)); break;
                            case TruckTaxiAnalyticsKind.Performance:
                            case TruckTaxiAnalyticsKind.LowPerformance:
                                writers["Performance"].WriteLine(prefix + Row(e.regionId, e.paused, e.fps, e.frameMs, e.mainThreadMs, e.physicsMs, e.liveCars, e.livePedestrians, e.liveRivals, e.loadedRegions)); break;
                            case TruckTaxiAnalyticsKind.RosterMember: break;
                            default:
                                writers["WorldEvents"].WriteLine(prefix + Row(e.regionId, e.passengerId, e.code, e.outcome, e.candidateCount, e.baseWeight, e.finalWeight, e.unseen, e.recentPenalty, e.archetypeModifier, e.eventModifier, e.customContentId, e.customItemId, e.customEventId)); break;
                        }
                        if (e.passengerId == "") continue;
                        if (!passengers.TryGetValue(e.passengerId, out var p))
                        {
                            if (passengers.Count >= 512) continue;
                            passengers[e.passengerId] = p = new Passenger { Id = e.passengerId, Archetype = e.archetype };
                        }
                        if (e.kind == TruckTaxiAnalyticsKind.RosterMember) { p.Archetype = e.archetype; continue; }
                        p.LastSession = e.sessionId; p.LastGameMinutes = e.gameMinutes; p.LastRealSeconds = e.realSeconds;
                        if (e.kind == TruckTaxiAnalyticsKind.OfferGenerated) p.Offered++;
                        if (e.kind == TruckTaxiAnalyticsKind.OfferAccepted) p.Accepted++;
                        if (e.kind != TruckTaxiAnalyticsKind.RideFinished) continue;
                        switch ((TruckTaxiRideOutcome)e.outcome)
                        {
                            case TruckTaxiRideOutcome.Completed: p.Completed++; break;
                            case TruckTaxiRideOutcome.PassengerCancelled:
                            case TruckTaxiRideOutcome.PickupCancelled: p.Cancelled++; break;
                            case TruckTaxiRideOutcome.Ejected: p.Ejected++; break;
                            default: p.Failed++; break;
                        }
                        if (e.rating >= 0) { p.Rating += e.rating; p.Rated++; }
                        if (e.patience >= 0) { p.Patience += e.patience; p.PatienceSamples++; }
                        p.Fare += e.fareCents; p.FareSamples++;
                    }
                foreach (var p in passengers.Values.OrderByDescending(p => p.Offered).ThenBy(p => p.Id, StringComparer.Ordinal))
                    writers["Passengers"].WriteLine(Row(p.Id, p.Archetype, p.Offered, p.Accepted, p.Completed, p.Cancelled, p.Ejected, p.Failed, p.LastSession,
                        p.LastGameMinutes, p.LastRealSeconds, p.Rated > 0 ? p.Rating / p.Rated : -1,
                        p.PatienceSamples > 0 ? p.Patience / p.PatienceSamples : -1, p.FareSamples > 0 ? p.Fare / p.FareSamples : -1));
            }
            finally { foreach (var writer in writers.Values) writer.Dispose(); }
            foreach (string name in names)
            {
                string path = Path.Combine(directory, name + ".csv");
                if (File.Exists(path)) File.Replace(path + ".tmp", path, null); else File.Move(path + ".tmp", path);
            }
        }
    }
}
