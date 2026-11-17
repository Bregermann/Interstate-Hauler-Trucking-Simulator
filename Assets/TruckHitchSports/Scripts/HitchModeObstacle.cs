using UnityEngine;
namespace LWS.TruckHitchSports
{
    public sealed class HitchModeObstacle : MonoBehaviour
    {
        public int ModeMask;
        public void Apply(int mode) => gameObject.SetActive((ModeMask & (1 << (mode % 4))) != 0);
    }
}
