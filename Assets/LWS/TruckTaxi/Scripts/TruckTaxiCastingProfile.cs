using UnityEngine;

namespace LWS.TruckTaxi
{
    [CreateAssetMenu(menuName = "Truck Taxi/Passengers/Casting")]
    public sealed class TruckTaxiCastingProfile : ScriptableObject
    {
        public string species = "Human";
        public bool human = true;
        public string raceEthnicity, skinTone, sexPresentation, approximateAge;
        [Min(.1f)] public float heightMeters = 1.75f;
        public string build, hair, regionalBackground, accentMetadata;
        [TextArea] public string distinctiveFeatures, clothingStyle;
    }
}
