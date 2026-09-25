using UnityEngine;

namespace LWS.TruckTaxi
{
    public enum TaxiLocationType { Apartment, House, Office, Store, Restaurant, Bar, Hospital, Hotel, GasStation, Industrial, Transit, RandomStreet }
    public sealed class TruckTaxiRideLocation : MonoBehaviour
    {
        public string locationName, locationId, district;
        public TaxiLocationType locationType;
        public bool pickupAllowed = true, dropoffAllowed = true;
        public Transform passengerSpawnPoint, truckStopPoint;
        [Min(3)] public float detectionRadius = 11;
        public string[] tags;
        public bool debugGizmos = true;
        public Vector3 StopPosition => truckStopPoint != null ? truckStopPoint.position : transform.position;
        public bool Contains(Vector3 position) => Vector3.ProjectOnPlane(position - StopPosition, Vector3.up).sqrMagnitude <= detectionRadius * detectionRadius;
        private void OnDrawGizmos()
        {
            if (!debugGizmos) return;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(StopPosition, detectionRadius);
            Gizmos.color=Color.green; Gizmos.DrawRay(StopPosition,Vector3.up*4);
            if(truckStopPoint!=null) Gizmos.DrawRay(StopPosition,truckStopPoint.forward*4);
            if(passengerSpawnPoint!=null)
            { Gizmos.color=Color.cyan; Gizmos.DrawWireCube(passengerSpawnPoint.position+Vector3.up,new Vector3(.7f,2,.7f)); }
        }
    }
}
