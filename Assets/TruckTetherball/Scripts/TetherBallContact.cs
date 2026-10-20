using UnityEngine;
using LWS.TruckSports;
namespace LWS.TruckTetherball
{
    public sealed class TetherBallContact : MonoBehaviour
    {
        public TetherMatch match;
        void OnCollisionEnter(Collision collision)
        {
            var truck=collision.rigidbody!=null?collision.rigidbody.GetComponentInParent<SportsEventTruck>():null;
            if(truck!=null)match.BallContact(truck,collision.relativeVelocity.magnitude);
        }
    }
}
