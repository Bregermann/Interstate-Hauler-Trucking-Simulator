using System;

namespace LWS.TruckTaxi
{
    public enum TruckTaxiKaijuKind { None, Sighting, Attack }

    // Game-time cadence is independent of frame rate and Unity's global RNG.
    public sealed class TruckTaxiKaijuSchedule
    {
        private readonly Random random;
        private readonly TruckTaxiKaijuConfig config;
        private DateTime lastObserved;
        public DateTime LastSighting { get; private set; }
        public DateTime NextSighting { get; private set; }
        public DateTime NextAttack { get; private set; }

        public TruckTaxiKaijuSchedule(TruckTaxiKaijuConfig settings, int seed)
        {
            config = settings ?? throw new ArgumentNullException(nameof(settings));
            random = new Random(seed);
        }

        public void Start(DateTime now)
        {
            lastObserved = LastSighting = now;
            NextSighting = SightAfter(now);
            NextAttack = After(now, config.attackMinDays, config.attackMaxDays);
        }

        public TruckTaxiKaijuKind Advance(DateTime now)
        {
            if (now < lastObserved) { Start(now); return TruckTaxiKaijuKind.None; }
            lastObserved = now;
            if (now >= NextAttack)
            {
                Record(TruckTaxiKaijuKind.Attack, now);
                return TruckTaxiKaijuKind.Attack;
            }
            if (now >= NextSighting || now >= DryLimit())
            {
                Record(TruckTaxiKaijuKind.Sighting, now);
                return TruckTaxiKaijuKind.Sighting;
            }
            return TruckTaxiKaijuKind.None;
        }

        public void Record(TruckTaxiKaijuKind kind, DateTime now)
        {
            if (kind == TruckTaxiKaijuKind.None) return;
            LastSighting = now;
            NextSighting = SightAfter(now);
            if (kind == TruckTaxiKaijuKind.Attack)
                NextAttack = After(now, config.attackMinDays, config.attackMaxDays);
        }

        private DateTime SightAfter(DateTime now)
        {
            DateTime candidate = After(now, config.sightingMinDays, config.sightingMaxDays);
            DateTime limit = DryLimit();
            return candidate <= limit ? candidate : limit;
        }

        private DateTime DryLimit() => LastSighting.AddDays(Math.Max(.1, config.maximumSightingDryDays));

        private DateTime After(DateTime now, float minimum, float maximum)
        {
            double low = Math.Max(.1, minimum);
            double high = Math.Max(low, maximum);
            return now.AddDays(low + random.NextDouble() * (high - low));
        }
    }
}
