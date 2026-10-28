using LWS.TruckSports;
using UnityEngine;

namespace LWS.TruckTornadoRodeo
{
    public sealed class TornadoRing : MonoBehaviour
    {
        public int id;
        public float apertureRadius = 9;
        readonly LWS.TruckSkyTow.SkyTowBodyGate existingGate = new LWS.TruckSkyTow.SkyTowBodyGate();
        public bool WholeTruckPassed(SportsEventTruck truck, Vector3 before, Quaternion rotationBefore, Vector3 after, Quaternion rotationAfter)
        {
            var shape = truck.shape; var scale = truck.transform.lossyScale; var centerOffset = Vector3.Scale(shape.center, scale); var extents = Vector3.Scale(shape.size * .5f, new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
            if (!existingGate.Sweep(before, rotationBefore, after, rotationAfter, centerOffset, extents, transform.position, transform.rotation, apertureRadius - .4f)) return false;
            Vector3 from = before + rotationBefore * centerOffset, to = after + rotationAfter * centerOffset;
            float a = Vector3.Dot(from - transform.position, transform.forward), b = Vector3.Dot(to - transform.position, transform.forward);
            if (a == 0 || a * b > 0 || Mathf.Abs(b - a) < .001f) return false;
            float t = -a / (b - a); if (t < 0 || t > 1 || (to - from).sqrMagnitude < .0025f) return false;
            var center = Vector3.Lerp(from, to, t); var rotation = Quaternion.Slerp(rotationBefore, rotationAfter, t);
            // All original physical truck envelope corners must fit the actual
            // aperture at the swept crossing; grazing a trigger is not a pass.
            float radius = Mathf.Max(0, apertureRadius - .4f);
            for (int i = 0; i < 8; i++)
            {
                var p = center + rotation * Vector3.Scale(extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var local = transform.InverseTransformPoint(p);
                if (new Vector2(local.x, local.y).sqrMagnitude > radius * radius) return false;
            }
            return true;
        }
    }
}
