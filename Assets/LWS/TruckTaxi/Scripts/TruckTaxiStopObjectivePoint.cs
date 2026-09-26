using UnityEngine;

namespace LWS.TruckTaxi
{
    public enum TruckTaxiStopCategory { Scenic, IllicitPickup, PrivateMeeting, FoodStop, PhotoStop, CollectionStop, SpecialEvent }
    public sealed class TruckTaxiStopObjectivePoint : MonoBehaviour
    {
        public string stableId, displayName, district;
        public TruckTaxiStopCategory category;
        [Min(6)] public float radius=9;
        [Min(1)] public int durationSeconds=12;
        [Tooltip("Metres/second; 0.447 is approximately one MPH.")] public float maximumSpeed=.447f;
        public Vector3 viewDirection=Vector3.forward;
        public string[] passengerTags=System.Array.Empty<string>();
        [TextArea] public string arrivalDialogue, completionDialogue;
        public int chaosReward;
        public Vector3 Position => transform.position;
        public bool Allows(PassengerProfile passenger)
        {
            if(string.IsNullOrWhiteSpace(stableId) || radius<6 || durationSeconds<1 || passenger==null) return false;
            if(passengerTags.Length==0) return true;
            foreach(string tag in passengerTags)
                if(System.Array.IndexOf(passenger.specialTraits??System.Array.Empty<string>(),tag)>=0) return true;
            return false;
        }
        public bool IsValidStop(Vector3 position,float speed) => speed<=maximumSpeed &&
            Mathf.Abs(position.y-Position.y)<5 && Vector2.Distance(new Vector2(position.x,position.z),new Vector2(Position.x,Position.z))<=radius;
        private void OnDrawGizmosSelected()
        { Gizmos.color=category==TruckTaxiStopCategory.Scenic ? Color.cyan : Color.magenta; Gizmos.DrawWireSphere(Position,radius); Gizmos.DrawRay(Position,viewDirection*10); }
    }
}
