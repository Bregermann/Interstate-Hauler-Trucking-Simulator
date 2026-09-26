using System;
using UnityEngine;
using UnityEngine.Audio;

namespace LWS.TruckTaxi
{
    public enum TruckTaxiAudioCategory { Master, Voices, Vehicle, World, UI }

    [Serializable]
    public sealed class TruckTaxiAudioBus
    {
        public TruckTaxiAudioCategory category;
        public string label;
        public AudioMixerGroup group;
        public string volumeParameter;
        [Range(-80,0), Tooltip("Authored default attenuation. Reset Defaults restores this mix, not an arbitrary maximum.")]
        public float defaultDecibels;
    }

    [CreateAssetMenu(menuName="Truck Taxi/Audio Routing")]
    public sealed class TruckTaxiAudioConfiguration : ScriptableObject
    {
        public TruckTaxiAudioBus[] buses=Array.Empty<TruckTaxiAudioBus>();
        [Tooltip("Project-owned copy of NWH's mixer; preserves its engine filters and source mix without changing the vendor asset.")]
        public AudioMixer vehicleMixer;
        public TruckTaxiAudioBus Find(TruckTaxiAudioCategory category) => buses==null ? null : Array.Find(buses,b=>b!=null && b.category==category);
    }
}
