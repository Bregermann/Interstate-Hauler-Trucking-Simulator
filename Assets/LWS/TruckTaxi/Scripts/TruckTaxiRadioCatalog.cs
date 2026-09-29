using System;
using UnityEngine;

namespace LWS.TruckTaxi
{
    [Serializable]
    public sealed class TruckTaxiRadioTrack
    {
        public string title;
        public AudioClip clip;
        [TextArea] public string transcript;
        [Range(0,23)] public int fromHour;
        [Range(1,24)] public int untilHour = 24;
        public bool Available(int hour) => fromHour < untilHour ? hour >= fromHour && hour < untilHour : hour >= fromHour || hour < untilHour;
    }
    [Serializable]
    public sealed class TruckTaxiRadioStation
    {
        public string stableId, displayName, frequency, format;
        public bool shuffle, allowEmergencyInterrupt = true;
        public AudioClip jingle;
        public TruckTaxiRadioTrack[] playlist = Array.Empty<TruckTaxiRadioTrack>();
    }
    [CreateAssetMenu(menuName="Truck Taxi/Radio Catalog")]
    public sealed class TruckTaxiRadioCatalog : ScriptableObject
    {
        public TruckTaxiRadioStation[] stations = Array.Empty<TruckTaxiRadioStation>();
        public AudioClip alertTone;
        [Tooltip("Original/licensed content provenance; no live radio streams.")] public string attribution;
    }
}
