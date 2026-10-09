using UnityEngine;

namespace LWS.TruckTaxi
{
    [CreateAssetMenu(menuName="Truck Taxi/Morality/Town")]
    public sealed class TruckTaxiTownDefinition : ScriptableObject
    {
        [Tooltip("Stable authored town ID and bounded initial values. Existing saved values are never reseeded.")]
        public TruckTaxiTownState initialState=new TruckTaxiTownState();
    }
}
