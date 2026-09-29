using UnityEngine;

namespace LWS.TruckTaxi
{
    // Optional scene-authored emergence point. Placement is still range and region checked.
    [DisallowMultipleComponent]
    public sealed class TruckTaxiKaijuSpawnAnchor : MonoBehaviour
    {
        public string regionId;
        public bool allowSighting = true;
        public bool allowAttack = true;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(.1f, .9f, .7f, .7f);
            Gizmos.DrawWireSphere(transform.position, 20);
        }
    }
}
