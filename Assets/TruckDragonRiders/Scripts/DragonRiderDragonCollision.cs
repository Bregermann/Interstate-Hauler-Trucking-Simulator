using UnityEngine;

namespace LWS.TruckDragonRiders
{
    // Receives the original dynamic Malbers-body collision, never a synthetic
    // damage notification or a proximity-only detachment.
    [DefaultExecutionOrder(20)] // the real MAnimal.OnAnimatorMove runs at -10
    public sealed class DragonRiderDragonCollision : MonoBehaviour
    {
        public DragonRiderRacer owner;
        void OnCollisionEnter(Collision collision) => owner?.Collision(collision);
        // Observe the normal evaluated animation after the vendor has applied
        // its movement. Do not evaluate, override, or apply root motion here.
        void OnAnimatorMove() => owner?.FollowEvaluatedSaddle();
    }
}
