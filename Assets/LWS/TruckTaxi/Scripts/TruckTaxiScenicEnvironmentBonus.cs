using LWS.InterstateHauler;
using UnityEngine;

namespace LWS.TruckTaxi
{
    [DisallowMultipleComponent, RequireComponent(typeof(TruckTaxiStopObjectivePoint))]
    public sealed class TruckTaxiScenicEnvironmentBonus : MonoBehaviour
    {
        public bool requirePreferredTime = true;
        public TruckTaxiDayPeriod preferredTime = TruckTaxiDayPeriod.Sunset;
        [Tooltip("Empty means any weather. This is a bonus, never an objective eligibility requirement.")]
        public string preferredWeatherId;
        [Min(0)] public int bonusFareCents = 150;
        [TextArea] public string bonusDialogue = "Now that is a view worth stopping for.";
        public bool Matches(TruckTaxiDayPeriod period, LwsWeatherSnapshot weather) =>
            (!requirePreferredTime || preferredTime == period) && (string.IsNullOrWhiteSpace(preferredWeatherId) ||
            (!weather.transitioning && preferredWeatherId == weather.weatherPresetId));
    }
}
