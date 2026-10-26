using UnityEngine;
namespace LWS.TruckSports
{
    // Added only to owned composition anchors, never to purchased prefabs.
    public sealed class SportsTruckAppearanceMountTag : MonoBehaviour
    {
        public string slotId;
        public int wheelIndex=-1;
    }
}
