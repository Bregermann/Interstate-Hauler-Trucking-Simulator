using System;

namespace LWS.TruckTaxi
{
    [Serializable]
    public sealed class TruckTaxiPassengerBathroomPolicy
    {
        public bool enabled = true;
        public float rideChance = .5f;
        public float initialBladder = .1f;
        public float gainPerGameHour = .35f;
        public float intercityMultiplier = 1.35f;
        public float requestThreshold = .75f;
        public float urgentThreshold = .95f;
        public float minimumRideMinutes = 45;
        public float repeatCooldownMinutes = 60;
        public int maximumRequestsPerRide = 1;
        public float reliefMinutes = 3;

        public TruckTaxiPassengerBathroomPolicy Normalized() => new TruckTaxiPassengerBathroomPolicy {
            enabled = enabled, rideChance = Limit(rideChance, 0, 1, .5f),
            initialBladder = Limit(initialBladder, 0, 1, .1f),
            gainPerGameHour = Limit(gainPerGameHour, 0, 4, .35f),
            intercityMultiplier = Limit(intercityMultiplier, 1, 4, 1.35f),
            requestThreshold = Limit(requestThreshold, .1f, 1, .75f),
            urgentThreshold = Math.Max(Limit(requestThreshold, .1f, 1, .75f), Limit(urgentThreshold, .1f, 1, .95f)),
            minimumRideMinutes = Limit(minimumRideMinutes, 1, 1440, 45),
            repeatCooldownMinutes = Limit(repeatCooldownMinutes, 15, 1440, 60),
            maximumRequestsPerRide = Math.Max(1, Math.Min(3, maximumRequestsPerRide)),
            reliefMinutes = Limit(reliefMinutes, 1, 10, 3)
        };

        private static float Limit(float value, float min, float max, float fallback) =>
            float.IsNaN(value) || float.IsInfinity(value) ? fallback : Math.Max(min, Math.Min(max, value));
    }

    public enum TruckTaxiPassengerBathroomPhase { Monitoring, Offered, Accepted, Presenting, Declined, Completed }

    // Game-clock state only. The host owns modal/pause arbitration, services, actors and navigation.
    public sealed class TruckTaxiPassengerBathroomState
    {
        private readonly TruckTaxiPassengerBathroomPolicy policy;
        private readonly bool enabled;
        private readonly double gainPerMinute;
        private double lastSeconds, nextOfferMinute;
        public double Bladder { get; private set; }
        public double RideMinutes { get; private set; }
        public int ResolvedRequests { get; private set; }
        public TruckTaxiPassengerBathroomPhase Phase { get; private set; }
        public float ReliefMinutes => policy.reliefMinutes;
        public bool Urgent => Bladder >= policy.urgentThreshold;
        public bool CanOffer => enabled &&
            (Phase == TruckTaxiPassengerBathroomPhase.Monitoring || Phase == TruckTaxiPassengerBathroomPhase.Declined ||
             Phase == TruckTaxiPassengerBathroomPhase.Completed) &&
            RideMinutes >= policy.minimumRideMinutes && RideMinutes >= nextOfferMinute &&
            ResolvedRequests < policy.maximumRequestsPerRide && Bladder >= policy.requestThreshold;

        public TruckTaxiPassengerBathroomState(TruckTaxiPassengerBathroomPolicy authored, double gameSeconds,
            bool intercity, double chanceSample)
        {
            policy = (authored ?? new TruckTaxiPassengerBathroomPolicy()).Normalized();
            enabled = policy.enabled && Finite(chanceSample) && chanceSample >= 0 && chanceSample <= 1 &&
                (policy.rideChance >= 1 || chanceSample < policy.rideChance);
            gainPerMinute = policy.gainPerGameHour * (intercity ? policy.intercityMultiplier : 1) / 60d;
            lastSeconds = Finite(gameSeconds) ? gameSeconds : 0;
            Bladder = policy.initialBladder;
        }

        public void ObserveClock(double totalGameSeconds, bool advance)
        {
            if (!Finite(totalGameSeconds)) return;
            double minutes = Math.Max(0, totalGameSeconds - lastSeconds) / 60d;
            lastSeconds = totalGameSeconds;
            // Baseline still advances during pause, recovery and the bathroom's own time skip.
            if (!advance || !enabled || !Finite(minutes) || Phase == TruckTaxiPassengerBathroomPhase.Presenting) return;
            RideMinutes = Math.Min(double.MaxValue, RideMinutes + minutes);
            Bladder = Math.Min(1, Bladder + minutes * gainPerMinute);
        }

        public bool Offer()
        {
            if (!CanOffer) return false;
            Phase = TruckTaxiPassengerBathroomPhase.Offered;
            return true;
        }
        public bool Accept()
        {
            if (Phase != TruckTaxiPassengerBathroomPhase.Offered) return false;
            Phase = TruckTaxiPassengerBathroomPhase.Accepted;
            return true;
        }
        public bool Decline()
        {
            if (Phase != TruckTaxiPassengerBathroomPhase.Offered) return false;
            ResolvedRequests++;
            nextOfferMinute = RideMinutes + policy.repeatCooldownMinutes;
            Phase = TruckTaxiPassengerBathroomPhase.Declined;
            return true;
        }
        public bool BeginPresentation()
        {
            if (Phase != TruckTaxiPassengerBathroomPhase.Accepted) return false;
            Phase = TruckTaxiPassengerBathroomPhase.Presenting;
            return true;
        }
        public bool CompleteAfterReboard()
        {
            if (Phase != TruckTaxiPassengerBathroomPhase.Presenting) return false;
            Bladder = 0;
            ResolvedRequests++;
            nextOfferMinute = RideMinutes + policy.repeatCooldownMinutes;
            Phase = TruckTaxiPassengerBathroomPhase.Completed;
            return true;
        }
        public void Withdraw()
        {
            if (Phase != TruckTaxiPassengerBathroomPhase.Offered && Phase != TruckTaxiPassengerBathroomPhase.Accepted &&
                Phase != TruckTaxiPassengerBathroomPhase.Presenting) return;
            Phase = TruckTaxiPassengerBathroomPhase.Monitoring;
            nextOfferMinute = RideMinutes + 5;
        }
        public static float DeclinePatienceLoss(float currentPatience) =>
            float.IsNaN(currentPatience) || float.IsInfinity(currentPatience) ? 0 :
                Math.Max(0, Math.Min(.08f, currentPatience - .15f));
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
