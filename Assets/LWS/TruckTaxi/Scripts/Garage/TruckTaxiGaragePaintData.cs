using System;
using UnityEngine;

namespace LWS.TruckTaxi
{
    [Serializable]
    public sealed class TruckTaxiGaragePaintData
    {
        public int version = 1;
        public Color color = Color.white;
        public TruckTaxiGaragePaintData Copy() => (TruckTaxiGaragePaintData)MemberwiseClone();
        public bool Validate() => version == 1 && Channel(color.r) && Channel(color.g) &&
            Channel(color.b) && color.a == 1;
        private static bool Channel(float value) => float.IsFinite(value) && value >= 0 && value <= 1;
    }
}
