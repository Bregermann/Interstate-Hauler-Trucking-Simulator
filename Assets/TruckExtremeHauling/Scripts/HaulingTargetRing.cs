using System;
using UnityEngine;

namespace LWS.TruckExtremeHauling
{
    public sealed class HaulingTargetRing : MonoBehaviour
    {
        public float radius = 10;
        public bool TryPass(Vector3 previous, Vector3 current, float bodyRadius)
        {
            if (bodyRadius >= radius || bodyRadius < 0) return false;
            Vector3 a = transform.InverseTransformPoint(previous), b = transform.InverseTransformPoint(current);
            if (a.z <= 0 && b.z <= 0 || a.z >= 0 && b.z >= 0) return false;
            float denominator = a.z - b.z;
            if (Mathf.Abs(denominator) < .00001f) return false;
            Vector3 crossing = Vector3.Lerp(a, b, a.z / denominator);
            float available = Mathf.Max(0, radius - bodyRadius);
            return crossing.x * crossing.x + crossing.y * crossing.y <= available * available;
        }
    }
}
