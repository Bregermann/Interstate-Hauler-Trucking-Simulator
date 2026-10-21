using UnityEngine;

namespace LWS.TruckSports
{
    [CreateAssetMenu(menuName="Truck Sports/Broadcast framing profile")]
    public sealed class SportsBroadcastProfile:ScriptableObject
    {
        public SportsBroadcastSettings settings=new SportsBroadcastSettings();
    }
}
