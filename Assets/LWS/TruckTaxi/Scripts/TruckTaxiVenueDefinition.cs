using System;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public enum TruckTaxiVenueType { Stadium, ConcertArena, Racetrack, ConventionCenter, Fairgrounds }
    public enum TruckTaxiVenueEventType { Football, ArenaSports, Concert, StockCarRace, Festival, Convention, MonsterTrucks, CountyFair }
    public enum TruckTaxiVenueRecurrence { Once, Weekly, Weekends, Weekdays, SpecificWeekday, DateRange }
    public enum TruckTaxiVenueEventStatus { Upcoming, ArrivalSurge, InProgress, DepartureSurge, Completed, Delayed, Postponed, Cancelled }
    public enum TruckTaxiVenueWeatherImpact { None, Delay, Postpone, Cancel }

    [Serializable]
    public sealed class TruckTaxiVenueDefinition
    {
        public string id, displayName, town, district, regionId, sceneName;
        public TruckTaxiVenueType type;
        public Vector3 worldPosition, mapPosition;
        [Min(0)] public int capacity;
        public float influenceRadiusMeters = 260;
        public Vector3[] parkingZones = Array.Empty<Vector3>();
        public Vector3[] crowdZones = Array.Empty<Vector3>();
        public Vector3[] taxiPickupZones = Array.Empty<Vector3>();
        public Vector3[] taxiDropoffZones = Array.Empty<Vector3>();
        public string[] eventIds = Array.Empty<string>();
    }

    [Serializable]
    public sealed class TruckTaxiVenueEventDefinition
    {
        public string id, venueId, displayName;
        public TruckTaxiVenueEventType type;
        public TruckTaxiVenueRecurrence recurrence;
        // In-game calendar dates; no wall-clock or timezone conversion.
        public string firstDate = "2026-01-01", lastDate = "2036-12-31";
        public DayOfWeek weekday = DayOfWeek.Saturday;
        [Range(0, 23)] public int startHour = 19, endHour = 22;
        [Range(0, 59)] public int startMinute, endMinute;
        [Min(0)] public int arrivalLeadMinutes = 90, departureMinutes = 90, announcementLeadMinutes = 30;
        [Range(0, 1)] public float attendanceScale = 1;
        [Min(1)] public float trafficMultiplier = 1.5f, crowdMultiplier = 1.8f,
            fareMultiplier = 1.5f, rideFrequencyMultiplier = 1.4f;
        public bool showOnMap = true;

        public bool OccursOn(DateTime date)
        {
            if (!DateTime.TryParseExact(firstDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var first) ||
                !DateTime.TryParseExact(lastDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var last)) return false;
            date = date.Date;
            if (date < first.Date || date > last.Date) return false;
            switch (recurrence)
            {
                case TruckTaxiVenueRecurrence.Once: return date == first.Date;
                case TruckTaxiVenueRecurrence.Weekly: return (date - first.Date).Days % 7 == 0;
                case TruckTaxiVenueRecurrence.Weekends: return date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;
                case TruckTaxiVenueRecurrence.Weekdays: return date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday;
                case TruckTaxiVenueRecurrence.SpecificWeekday: return date.DayOfWeek == weekday;
                case TruckTaxiVenueRecurrence.DateRange: return true;
                default: return false;
            }
        }

        public DateTime StartOn(DateTime date) => date.Date.AddHours(startHour).AddMinutes(startMinute);
        public DateTime EndOn(DateTime date)
        {
            var end = date.Date.AddHours(endHour).AddMinutes(endMinute);
            return end <= StartOn(date) ? end.AddDays(1) : end;
        }
    }

}
