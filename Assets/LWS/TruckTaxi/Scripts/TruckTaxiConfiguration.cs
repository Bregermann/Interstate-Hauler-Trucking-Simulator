using UnityEngine;

namespace LWS.TruckTaxi
{
    [CreateAssetMenu(menuName = "Truck Taxi/Demo Configuration")]
    public sealed class TruckTaxiConfiguration : ScriptableObject
    {
        public PassengerProfile[] passengers;
        public TruckTaxiPassengerDatabase passengerDatabase;
        public PassengerRequestDefinition[] requests;
        [Header("Ride generation")]
        public float minimumTripDistance = 150;
        public float maximumTripDistance = 950;
        public float rideFrequency = 6;
        public float offerDuration = 30;
        [Header("Regional rides")]
        [Min(1)] public float pickupTargetMinimumSeconds = 20;
        [Min(1)] public float pickupTargetMaximumSeconds = 90;
        [Min(1)] public float pickupHardMaximumSeconds = 180;
        [Range(0, 1)] public float intercityRideChance = 0.1f;
        [Range(0, 10)] public int localOffersAfterIntercity = 3;
        [Min(1), Tooltip("After this many completed local fares, the next routable eligible offer must be intercity. Declines never consume the guarantee.")]
        public int intercityDryStreakThreshold = 5;
        [Header("Dispatch delay (real seconds)")]
        public Vector2 veryHighDemandDelay = new Vector2(5,20);
        public Vector2 highDemandDelay = new Vector2(10,35);
        public Vector2 normalDemandDelay = new Vector2(25,75);
        public Vector2 lowDemandDelay = new Vector2(60,150);
        public Vector2 veryLowDemandDelay = new Vector2(150,300);
        [Min(50)] public float defaultWorkAreaRadius = 500;
        [Min(1)] public float speedwayPassengerWeight = 3;
        [Range(0,1)] public float racingSpeedwayDestinationChance = .6f;
        [Header("Bounded offer generation")]
        [Range(1, 12)] public int offerPickupCandidateLimit = 6;
        [Range(1, 24)] public int offerDestinationCandidateLimit = 12;
        [Range(.5f, 8)] public float offerFrameBudgetMilliseconds = 3;
        [Range(1, 10)] public float offerTimeoutSeconds = 3;
        public bool logOfferTimings;
        [Min(0)] public float intercityMaximumTripDistance = 20000;
        [Min(1)] public float intercityFareMultiplier = 1.2f;
        [Header("Passenger continuity")]
        [Range(1, 20)] public int recentOfferCount = 10;
        [Range(0.01f, 1)] public float recentOfferWeight = 0.2f;
        [Min(0)] public float recentLocationMinutes = 60;
        [Min(0), Tooltip("Maximum walk from a destination-only stop to an existing legal pickup bay. Never relocates a recent passenger across town.")]
        public float repeatPickupAccessRadiusMeters = 40;
        [Min(0)] public float citywideRelocationMinutes = 360;
        [Range(0, 1)] public float moderateSameLocationChance = 0.65f;
        [Header("Passenger rarity multipliers")]
        [Min(0)] public float commonWeight = 1;
        [Min(0)] public float uncommonWeight = 0.65f;
        [Min(0)] public float rareWeight = 0.3f;
        [Min(0)] public float legendaryWeight = 0.1f;
        [Header("Pickup patience")]
        [Min(0)] public float pickupBaseGraceSeconds = 25;
        [Min(1)] public float pickupMinimumSeconds = 45;
        [Min(1)] public float pickupReasonableSpeedMetersPerSecond = 12;
        [Min(0)] public float pickupPatienceMultiplier = 2;
        [Min(0.1f)] public float timeObsessedPickupMultiplier = 0.6f;
        [Min(1)] public float pickupMaximumSeconds = 300;
        [Header("Onboard behavior (not a ride countdown)")]
        [Min(1)] public float patienceProgressSampleSeconds = 4;
        [Min(0)] public float wrongWayGraceSeconds = 20;
        [Min(0)] public float unexplainedStopGraceSeconds = 60;
        [Min(0)] public float patienceRecoveryPerSecond = .001f;
        [Min(0)] public long cancellationFeeCents = 300;
        [Header("Demand multipliers")]
        [Min(0.1f)] public float morningFrequencyMultiplier = 0.75f;
        [Min(0.1f)] public float eveningFrequencyMultiplier = 0.85f;
        [Min(0.1f)] public float lateNightFrequencyMultiplier = 1.5f;
        [Min(0.1f)] public float preDawnFrequencyMultiplier = 2f;
        [Min(0.1f)] public float rainFrequencyMultiplier = 0.8f;
        [Min(0.1f)] public float stormFrequencyMultiplier = 0.7f;
        [Min(0.1f)] public float snowFrequencyMultiplier = 0.7f;
        [Min(0.1f)] public float heavySnowFrequencyMultiplier = 0.65f;
        [Min(0.1f)] public float blizzardFrequencyMultiplier = 0.6f;
        [Min(0)] public float morningCommuterWeight = 1.5f;
        [Min(0)] public float eveningSocialWeight = 1.5f;
        [Min(0)] public float eveningRareWeight = 1.25f;
        [Min(0)] public float lateNightRareWeight = 1.5f;
        [Min(0)] public float lateNightWeirdWeight = 1.5f;
        [Min(0)] public float lateNightNightlifeWeight = 1.5f;
        [Min(0)] public float lateNightChaoticWeight = 1.35f;
        [Min(0)] public float lateNightFlirtatiousWeight = 1.25f;
        [Min(0)] public float preDawnRareWeight = 1.75f;
        [Min(0)] public float preDawnWeirdWeight = 1.75f;
        [Min(0)] public float preDawnNightlifeWeight = 1.25f;
        [Min(0)] public float preDawnChaoticWeight = 1.5f;
        [Min(0)] public float preDawnFlirtatiousWeight = 1.1f;
        [Min(0)] public float morningFareMultiplier = 1.1f;
        [Min(0)] public float eveningFareMultiplier = 1.1f;
        [Min(0)] public float lateNightFareMultiplier = 1.25f;
        [Min(0)] public float preDawnFareMultiplier = 1.35f;
        [Min(0)] public float rainFareMultiplier = 1.08f;
        [Min(0)] public float stormFareMultiplier = 1.15f;
        [Min(0)] public float snowFareMultiplier = 1.2f;
        [Min(0)] public float heavySnowFareMultiplier = 1.25f;
        [Min(0)] public float blizzardFareMultiplier = 1.3f;
        [Min(0)] public float eveningAppreciationMultiplier = 1.2f;
        [Min(0)] public float lateNightAppreciationMultiplier = 1.25f;
        [Min(0)] public float preDawnAppreciationMultiplier = 1.4f;
        [Range(0, 1)] public float appreciationBaseChance = 0.35f;
        [Min(0)] public float badWeatherPickupTimeMultiplier = 1.25f;
        public float stoppedSpeed = 0.5f;
        public float boardingSeconds = 1.5f;
        public float exitingSeconds = 1;
        [Header("Fare (integer cents)")]
        public long baseFareCents = 500;
        public float centsPerMeter = 1.5f;
        public float centsPerSecond = 2;
        public float tipFraction = 0.2f;
        public long impactPenaltyCents = 100;
        public float chaosCentsPerPoint = 0.1f;
        [Header("Scoring")]
        public int trafficRamScore = 100;
        public int pedestrianHitScore = 250;
        public int propertyDamageScore = 50;
        public int shortcutScore = 300;
        public int nearMissScore = 75;
        public int hardLandingScore = 200;
        public float offroadScorePerSecond = 5;
        [Header("Detection")]
        public float minimumImpactSpeed = 3;
        public float collisionCooldown = 3;
        public float nearMissSpeed = 8;
        public float nearMissRadius = 5;
        [Min(0), Tooltip("Seconds before the same traffic car can award another near miss. Even gentle contact disqualifies a pass.")]
        public float nearMissCooldown = 12;
        [Header("Pedestrian impact / UTS ragdoll")]
        public TruckTaxiPedestrianImpactSettings pedestrianImpact = new TruckTaxiPedestrianImpactSettings();
        [Header("Passenger driving reactions")]
        public float fastDrivingSpeed = 18;
        public float speedReactionCooldown = 15;
        public float gentleAcceleration = 2;
        public float smoothRatingPerSecond = 0.003f;
        public float hardLandingSpeed = 6;
        [Header("Optional audio")]
        public TruckTaxiAudioConfiguration audio;
        public AudioClip offerSound, acceptSound, boardingSound, exitSound, requestSound,
            requestSuccessSound, requestFailureSound, collisionSound, fareSound;

        public int Score(TaxiEventType type)
        {
            switch (type)
            {
                case TaxiEventType.TrafficRam: return trafficRamScore;
                case TaxiEventType.PedestrianHit: return pedestrianHitScore;
                case TaxiEventType.PropDamage: return propertyDamageScore;
                case TaxiEventType.Shortcut: return shortcutScore;
                case TaxiEventType.NearMiss: return nearMissScore;
                case TaxiEventType.HardLanding: return hardLandingScore;
                default: return 0;
            }
        }

        public float RarityWeight(TruckTaxiRarity rarity)
        {
            switch (rarity)
            {
                case TruckTaxiRarity.Uncommon: return uncommonWeight;
                case TruckTaxiRarity.Rare: return rareWeight;
                case TruckTaxiRarity.Legendary: return legendaryWeight;
                default: return commonWeight;
            }
        }
    }
}
