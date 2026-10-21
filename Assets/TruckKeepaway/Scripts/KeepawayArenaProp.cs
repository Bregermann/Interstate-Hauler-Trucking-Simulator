using UnityEngine;
using LWS.TruckSports;
using LWS.TruckPedestrianPlowing;

namespace LWS.TruckKeepaway
{
    // Core-Collapse and the existing pooled Plowing fragment lifecycle own
    // destruction. This adapter only coordinates Keepaway navigation/rematches.
    public sealed class KeepawayArenaProp : MonoBehaviour
    {
        public PlowBreakable breakable;
        public Collider intactShape;
        public bool anchored;
        bool observedBroken;
        Vector3 navigationPosition, incoming;
        Quaternion navigationRotation;
        public void ResetProp()
        {
            bool active=gameObject.activeSelf;
            if(!active)gameObject.SetActive(true); // initialize pooled vendor/fragment Awake once
            breakable.ResetPiece(); observedBroken=false; incoming=Vector3.zero;
            intactShape.enabled=true; breakable.intactBody.isKinematic=anchored;
            if(!anchored)breakable.intactBody.linearVelocity=breakable.intactBody.angularVelocity=Vector3.zero;
            navigationPosition=transform.position; navigationRotation=transform.rotation;
            if(!active)gameObject.SetActive(false);
        }
        public bool TickProp()
        {
            if(!gameObject.activeInHierarchy||observedBroken||!breakable.HasCollapsed)return false;
            observedBroken=true; intactShape.enabled=false;
            // The native contact supplied this motion; no random launch force.
            for(int i=0;i<breakable.fragments.Length;i++)
            {
                var fragment=breakable.fragments[i];
                fragment.linearVelocity=Vector3.ClampMagnitude(incoming*.55f+Vector3.up*1.2f,12);
                fragment.angularVelocity=new Vector3(0,i%2==0?2:-2,0);
            }
            return true;
        }
        public bool NavigationMoved()
        {
            bool moved=(transform.position-navigationPosition).sqrMagnitude>.16f||Quaternion.Angle(transform.rotation,navigationRotation)>10;
            if(moved){navigationPosition=transform.position;navigationRotation=transform.rotation;}
            return moved;
        }
        void OnCollisionEnter(Collision contact)
        {
            if(contact.rigidbody!=null&&contact.rigidbody.GetComponent<SportsEventTruck>()!=null)
                incoming=contact.rigidbody.linearVelocity-contact.relativeVelocity*.15f;
        }
        void OnDestroy()
        {
            // Plowing detaches live fragments. Own them through scene exit too.
            if(breakable==null||breakable.fragments==null)return;
            foreach(var fragment in breakable.fragments)if(fragment!=null&&!fragment.transform.IsChildOf(transform))Destroy(fragment.gameObject);
        }
    }
}
