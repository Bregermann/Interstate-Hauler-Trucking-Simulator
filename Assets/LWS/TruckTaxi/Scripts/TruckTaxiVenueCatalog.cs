using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace LWS.TruckTaxi
{
    [CreateAssetMenu(menuName = "Truck Taxi/Venue Event Catalog")]
    public sealed class TruckTaxiVenueCatalog : ScriptableObject
    {
        public const string ResourcePath = "TruckTaxi/TruckTaxiVenueCatalog";
        public TruckTaxiVenueDefinition[] venues = Array.Empty<TruckTaxiVenueDefinition>();
        public TruckTaxiVenueEventDefinition[] events = Array.Empty<TruckTaxiVenueEventDefinition>();

        public void Validate()
        {
            var venueIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var venue in venues)
                if (venue == null || string.IsNullOrWhiteSpace(venue.id) || !venueIds.Add(venue.id) ||
                    venue.influenceRadiusMeters <= 0)
                    throw new InvalidOperationException("Invalid or duplicate venue ID/radius.");
            var eventIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in events)
                if (item == null || string.IsNullOrWhiteSpace(item.id) || !eventIds.Add(item.id) ||
                    !venueIds.Contains(item.venueId) ||
                    !DateTime.TryParseExact(item.firstDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out var first) ||
                    !DateTime.TryParseExact(item.lastDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out var last) ||
                    last < first || item.fareMultiplier < 1 || item.trafficMultiplier < 1 ||
                    item.crowdMultiplier < 1 || item.rideFrequencyMultiplier < 1)
                    throw new InvalidOperationException("Invalid event definition: " + item?.id);
        }
    }
}
