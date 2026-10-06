using System;

namespace LWS.TruckTaxi
{
    // Internal physics stays in SI; only player-facing labels use these conversions.
    public static class TruckTaxiUsUnits
    {
        public const double MetersPerMile = 1609.344;
        public const double MetersPerFoot = .3048;
        public const double LitersPerUsGallon = 3.785411784;
        public static float Mph(float metersPerSecond) => metersPerSecond * 2.23693629f;
        public static string Distance(double meters)
        {
            if (double.IsNaN(meters) || double.IsInfinity(meters)) return "--";
            double positive = Math.Max(0, meters);
            return positive < .1 * MetersPerMile ?
                Math.Round(positive / MetersPerFoot, MidpointRounding.AwayFromZero).ToString("0") + " ft" :
                Math.Round(positive / MetersPerMile, 1, MidpointRounding.AwayFromZero).ToString("0.0") + " mi";
        }
        public static string Fuel(float liters) => (liters / (float)LitersPerUsGallon).ToString("0.0") + " gal";
        public static long CentsPerGallon(int centsPerLiter) =>
            (long)Math.Round(Math.Max(0, centsPerLiter) * LitersPerUsGallon, MidpointRounding.AwayFromZero);
    }
}
