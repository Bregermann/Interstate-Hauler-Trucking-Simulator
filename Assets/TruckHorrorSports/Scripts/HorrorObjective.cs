using UnityEngine;
namespace LWS.TruckHorrorSports
{
    public sealed class HorrorObjective : MonoBehaviour
    {
        public string caption;
        public int requires = -1;
        public float seconds = 2, passageRadius = 7;
        public bool restoresPower;
        public bool requiresFoot;
        public Vector3 truckApproach;
        public HorrorSecurityMonitor securityMonitor;
        public Light beacon;
        public void ResetObjective() { if (beacon != null) { beacon.enabled = true; beacon.color = new Color(.85f, .55f, .14f); } }
        public void CompleteVisual(bool shared) { if (beacon != null && shared) beacon.color = new Color(.2f, .7f, .5f); }
    }
}
