using System;
using System.Globalization;
using System.Collections.Generic;

namespace LWS.TruckTaxi
{
    // Append enum members only: numeric values are part of the versioned wire contract.
    public enum TruckTaxiAnalyticsKind
    {
        SessionStarted, SessionSnapshot, SessionEnded, RosterMember, OfferGenerated,
        OfferAccepted, OfferClosedUnknown, PickupCompleted, RideFinished, ObjectiveCreated,
        ObjectiveResolved, DiversionOffered, DrivingEvent, DriverNeed, Performance,
        LowPerformance, RegionVisited, WeatherChanged, RadioChanged, Error, Fairness,
        WorldEvent, ItemUsed, ServiceUsed, NavigationUsed, OfferDeclined, OfferExpired,
        CustomContentPhase, CustomItemInteraction, CustomItemStarted, CustomItemCompleted
    }
    public enum TruckTaxiAnalyticsError { UnhandledException, RouteFailure, StreamFailure, InvalidPassenger, InvalidSpawn }
    public enum TruckTaxiAnalyticsWorldEvent { RivalPickup, RivalRecovery, TrafficStuck, TrafficRecovery, TrafficExplosion, DisasterStarted, DisasterEnded, KaijuStarted, KaijuEnded, RaceStarted, RaceCompleted, RaceCancelled }
    public enum TruckTaxiAnalyticsService { Refuel, Repair, Food, Drink, Restroom, Sleep, Recovery, Store }
    public enum TruckTaxiAnalyticsNavigation { MapOpened, DestinationSelected, RouteStarted, RouteCleared, RouteFailed }
    public enum TruckTaxiAnalyticsArchetype { Other, Racing, Business, Chaos, Glamorous, Commuter, Elderly, GameParody, Fantasy }

    [Serializable]
    public sealed class TruckTaxiAnalyticsEvent
    {
        public int schemaVersion = 1;
        public string gameVersion, buildId, installId, sessionId, eventId;
        public TruckTaxiAnalyticsKind kind;
        public string passengerId, rideId, regionId;
        public string customContentId, customItemId, customEventId;
        public TruckTaxiAnalyticsArchetype archetype;
        // code is a typed enum value selected by the record seam, never log text.
        public int code, outcome, count;
        public double realSeconds, gameMinutes, playSeconds, pauseSeconds, drivingSeconds, stoppedSeconds;
        public double distanceMeters, maxSpeedMps, averageSpeedMps, pickupSeconds, rideSeconds;
        public long fareCents, tipCents, eventBonusCents, weatherBonusCents;
        public double rating = -1, patience = -1, demandMultiplier = -1;
        public bool intercity, paused;
        public double fps = -1, frameMs = -1, mainThreadMs = -1, physicsMs = -1;
        public int liveCars = -1, livePedestrians = -1, liveRivals = -1, loadedRegions = -1;
        public int candidateCount = -1;
        public double baseWeight = -1, finalWeight = -1, recentPenalty = -1, archetypeModifier = -1, eventModifier = -1;
        public bool unseen;

        public TruckTaxiAnalyticsEvent Snapshot() => (TruckTaxiAnalyticsEvent)MemberwiseClone();

        public TruckTaxiAnalyticsEvent Sanitize()
        {
            if (schemaVersion != 1 || !Enum.IsDefined(typeof(TruckTaxiAnalyticsKind), kind)) return null;
            var e = (TruckTaxiAnalyticsEvent)MemberwiseClone();
            e.gameVersion = TruckTaxiAnalyticsPrivacy.Token(gameVersion, 48);
            e.buildId = TruckTaxiAnalyticsPrivacy.Token(buildId, 64);
            e.installId = TruckTaxiAnalyticsPrivacy.RandomId(installId);
            e.sessionId = TruckTaxiAnalyticsPrivacy.RandomId(sessionId);
            e.eventId = TruckTaxiAnalyticsPrivacy.RandomId(eventId);
            if (e.installId == "" || e.sessionId == "" || e.eventId == "") return null;
            e.passengerId = TruckTaxiAnalyticsPrivacy.Token(passengerId);
            e.rideId = TruckTaxiAnalyticsPrivacy.RandomId(rideId);
            e.regionId = TruckTaxiAnalyticsPrivacy.Token(regionId);
            e.customContentId = TruckTaxiAnalyticsPrivacy.RandomId(customContentId);
            e.customItemId = TruckTaxiAnalyticsPrivacy.RandomId(customItemId);
            e.customEventId = TruckTaxiAnalyticsPrivacy.RandomId(customEventId);
            if (!Enum.IsDefined(typeof(TruckTaxiAnalyticsArchetype), e.archetype)) e.archetype = TruckTaxiAnalyticsArchetype.Other;
            e.code = Math.Max(0, Math.Min(1024, code)); e.outcome = Math.Max(0, Math.Min(1024, outcome));
            e.count = Math.Max(0, Math.Min(1000000, count));
            e.realSeconds = Number(realSeconds); e.gameMinutes = Number(gameMinutes);
            e.playSeconds = Number(playSeconds); e.pauseSeconds = Number(pauseSeconds);
            e.drivingSeconds = Number(drivingSeconds); e.stoppedSeconds = Number(stoppedSeconds);
            e.distanceMeters = Number(distanceMeters); e.maxSpeedMps = Number(maxSpeedMps, 0, 200);
            e.averageSpeedMps = Number(averageSpeedMps, 0, 200);
            e.pickupSeconds = Number(pickupSeconds); e.rideSeconds = Number(rideSeconds);
            e.fareCents = Money(fareCents); e.tipCents = Money(tipCents);
            e.eventBonusCents = Money(eventBonusCents); e.weatherBonusCents = Money(weatherBonusCents);
            e.rating = Number(rating, -1, 5); e.patience = Number(patience, -1, 1);
            e.demandMultiplier = Number(demandMultiplier, -1, 1000);
            e.fps = Number(fps, -1, 10000); e.frameMs = Number(frameMs, -1, 600000);
            e.mainThreadMs = Number(mainThreadMs, -1, 600000); e.physicsMs = Number(physicsMs, -1, 600000);
            e.liveCars = Counter(liveCars); e.livePedestrians = Counter(livePedestrians);
            e.liveRivals = Counter(liveRivals); e.loadedRegions = Counter(loadedRegions); e.candidateCount = Counter(candidateCount);
            e.baseWeight = Number(baseWeight, -1); e.finalWeight = Number(finalWeight, -1);
            e.recentPenalty = Number(recentPenalty, -1); e.archetypeModifier = Number(archetypeModifier, -1);
            e.eventModifier = Number(eventModifier, -1);
            return e;
        }
        public static double Number(double value, double minimum = 0, double maximum = 1e12) =>
            double.IsNaN(value) || double.IsInfinity(value) ? minimum : Math.Max(minimum, Math.Min(maximum, value));
        private static int Counter(int n) => Math.Max(-1, Math.Min(1000000, n));
        private static long Money(long n) => Math.Max(0, Math.Min(1000000000000L, n));
    }

    public static class TruckTaxiAnalyticsPrivacy
    {
        // Reject the entire value: paths and sentences must not become IDs.
        public static string Token(string value, int limit = 80)
        {
            if (string.IsNullOrEmpty(value) || value.Length > limit || !AsciiLetterOrDigit(value[0]) || value.Contains("..")) return "";
            foreach (char c in value) if (!AsciiLetterOrDigit(c) && c != '_' && c != '-' && c != '.') return "";
            return value;
        }
        private static bool AsciiLetterOrDigit(char c) => c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c >= '0' && c <= '9';
        public static string RandomId(string value) => Guid.TryParseExact(value, "N", out var id) && id != Guid.Empty ? id.ToString("N") : "";
        public static string Csv(string value)
        {
            value = value ?? "";
            if (value.Length > 0 && "=+-@\t\r\n".IndexOf(value[0]) >= 0) value = "'" + value;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
        public static string Invariant(double value) => value.ToString("0.######", CultureInfo.InvariantCulture);
    }

    // Session-local opaque aliases; authored content strings never enter files or uploads.
    public sealed class TruckTaxiAnalyticsContentIds
    {
        private readonly Dictionary<string, string> aliases = new Dictionary<string, string>(StringComparer.Ordinal);
        public string Alias(string stableId)
        {
            string id = TruckTaxiAnalyticsPrivacy.Token(stableId);
            if (id == "") return "";
            if (aliases.TryGetValue(id, out var alias)) return alias;
            if (aliases.Count >= 256) return "";
            alias = Guid.NewGuid().ToString("N"); aliases.Add(id, alias); return alias;
        }
    }

    public sealed class TruckTaxiAnalyticsMetrics
    {
        public double RealSeconds { get; private set; }
        public double PlaySeconds { get; private set; }
        public double PauseSeconds { get; private set; }
        public double DrivingSeconds { get; private set; }
        public double StoppedSeconds { get; private set; }
        public double DistanceMeters { get; private set; }
        public double MaxSpeedMps { get; private set; }
        public double AverageSpeedMps => PlaySeconds > 0 ? speedIntegral / PlaySeconds : 0;
        public double AverageFps => frameSeconds > 0 ? frames / frameSeconds : 0;
        private double speedIntegral, frameSeconds;
        private long frames;
        public void Tick(double unscaledSeconds, double simulationSeconds, double speedMps, bool active, bool paused)
        {
            unscaledSeconds = TruckTaxiAnalyticsEvent.Number(unscaledSeconds, 0, 600);
            if (!active || unscaledSeconds <= 0) return;
            RealSeconds += unscaledSeconds;
            if (paused) { PauseSeconds += unscaledSeconds; return; }
            PlaySeconds += unscaledSeconds; frames++; frameSeconds += unscaledSeconds;
            speedMps = TruckTaxiAnalyticsEvent.Number(Math.Abs(speedMps), 0, 200);
            if (speedMps > .5) DrivingSeconds += unscaledSeconds; else StoppedSeconds += unscaledSeconds;
            speedIntegral += speedMps * unscaledSeconds;
            // Integrate vendor speed in simulation time, excluding teleports and coordinates.
            DistanceMeters += speedMps * TruckTaxiAnalyticsEvent.Number(simulationSeconds, 0, 1);
            MaxSpeedMps = Math.Max(MaxSpeedMps, speedMps);
        }
        public void CopyTo(TruckTaxiAnalyticsEvent e)
        {
            e.realSeconds = RealSeconds; e.playSeconds = PlaySeconds; e.pauseSeconds = PauseSeconds;
            e.drivingSeconds = DrivingSeconds; e.stoppedSeconds = StoppedSeconds;
            e.distanceMeters = DistanceMeters; e.maxSpeedMps = MaxSpeedMps; e.averageSpeedMps = AverageSpeedMps;
        }
    }
}
