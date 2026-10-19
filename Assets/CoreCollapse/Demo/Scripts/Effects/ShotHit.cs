using UnityEngine;

namespace Assets.CoreCollapse.Demo.Scripts.Effects
{
    public class ShotHit : MonoBehaviour
    {
        [SerializeField] GameObject impactVfxPrefab;

        public void Initialize(GameObject vfx) => impactVfxPrefab = vfx;

        public void ReceiveShot(Vector3 hitPosition, Vector3 hitNormal)
        {
            var vfx = Instantiate(impactVfxPrefab, hitPosition, Quaternion.LookRotation(hitNormal));
            vfx.transform.SetParent(transform);
        }
    }
}
