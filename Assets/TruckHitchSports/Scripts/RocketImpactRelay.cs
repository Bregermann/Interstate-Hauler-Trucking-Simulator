using UnityEngine;

namespace LWS.TruckHitchSports
{
    [DisallowMultipleComponent]
    public sealed class RocketImpactRelay : MonoBehaviour
    {
        public RocketFlightPresentation Presentation;
        void OnCollisionEnter(Collision collision) => Presentation?.ObserveCollision(collision);
    }
}
