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
        public float pedestrianRespawnSeconds = 12;
        [Header("Passenger driving reactions")]
        public float fastDrivingSpeed = 18;
        public float speedReactionCooldown = 15;
        public float gentleAcceleration = 2;
        public float smoothRatingPerSecond = 0.003f;
        public float hardLandingSpeed = 6;
        [Header("Optional audio")]
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
    }
}
