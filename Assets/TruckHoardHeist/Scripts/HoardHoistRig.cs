using LWS.TruckJenga;
using UnityEngine;

namespace LWS.TruckHoardHeist
{
    // The shared crane's original bridge assumes a world-origin warehouse.
    // This is only a truck-mounted visual adapter; JengaCrane owns the load.
    public sealed class HoardHoistRig : MonoBehaviour
    {
        public JengaCrane crane;
        public Transform mastTip, telescopicBoom;
        public void Present()
        {
            if (crane == null || mastTip == null || telescopicBoom == null || crane.spreader == null) return;
            Vector3 a = mastTip.position;
            Vector3 b = crane.spreader.position + Vector3.up * 1.2f;
            b.y = Mathf.Max(a.y, b.y);
            Vector3 delta = b - a;
            telescopicBoom.position = (a + b) * .5f;
            telescopicBoom.rotation = delta.sqrMagnitude > .001f ? Quaternion.FromToRotation(Vector3.up, delta.normalized) : Quaternion.identity;
            telescopicBoom.localScale = new Vector3(.13f, Mathf.Max(.05f, delta.magnitude * .5f), .13f);
            if (crane.hoist != null) { crane.hoist.SetPosition(0,b); crane.hoist.SetPosition(1,crane.spreader.position); }
        }
    }
}
