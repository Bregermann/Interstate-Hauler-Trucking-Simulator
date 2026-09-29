using UnityEngine;

namespace LWS.TruckTaxi
{
    [DisallowMultipleComponent]
    public sealed class TruckTaxiRivalDepot : MonoBehaviour
    {
        public string depotId;
        public string preferredTown;
        [Range(0, 1)] public float venueBias = .25f;
        [Range(0, 24)] public float shiftStartHour = 6;
        [Range(0, 24)] public float shiftEndHour = 22;
        public Vector3 StagingPosition => transform.position;
    }
}
