using UnityEngine;

namespace LWS.TruckTaxi
{
    [CreateAssetMenu(menuName="Truck Taxi/Career/NWH Upgrade Tuning")]
    public sealed class TruckTaxiCareerUpgradeTuning : ScriptableObject
    {
        public const string AdapterId = "taxi.nwh.tuning";
        [Tooltip("Multiplier of the existing NWH value. One compatible upgrade per category; never stacks on itself.")]
        [Range(1, 1.5f)] public float multiplier = 1.1f;
        public bool IsValid => float.IsFinite(multiplier) && multiplier >= 1 && multiplier <= 1.5f;
    }
}
