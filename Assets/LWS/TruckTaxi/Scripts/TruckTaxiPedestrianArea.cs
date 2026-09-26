using LWS.InterstateHauler;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public sealed class TruckTaxiPedestrianArea : MonoBehaviour
    {
        public string areaId;
        [Min(2)] public Vector2 localSize = new Vector2(28, 18);
        [Min(0)] public float roadSetback = 5;
        public Component[] walkingPaths;

        public void Configure(string id, Vector2 size, float setback)
        {
            areaId = id;
            localSize = new Vector2(Mathf.Max(2, size.x), Mathf.Max(2, size.y));
            roadSetback = Mathf.Max(0, setback);
        }

        public bool Allows(Vector3 point, LwsTrafficLaneDefinition[] lanes)
        {
            Vector3 local = transform.InverseTransformPoint(point);
            if (Mathf.Abs(local.x) > localSize.x * .5f || Mathf.Abs(local.z) > localSize.y * .5f) return false;
            if (lanes == null || roadSetback <= 0) return true;
            float minimum = roadSetback * roadSetback;
            foreach (var lane in lanes)
            {
                if (lane == null || lane.centerline == null) continue;
                for (int i = 1; i < lane.centerline.Length; i++)
                {
                    Vector2 a = new Vector2(lane.centerline[i-1].x, lane.centerline[i-1].z);
                    Vector2 b = new Vector2(lane.centerline[i].x, lane.centerline[i].z);
                    Vector2 p = new Vector2(point.x, point.z);
                    Vector2 ab = b-a;
                    float t = ab.sqrMagnitude > .001f ? Mathf.Clamp01(Vector2.Dot(p-a,ab)/ab.sqrMagnitude) : 0;
                    if ((p-(a+t*ab)).sqrMagnitude < minimum) return false;
                }
            }
            return true;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(.1f, .85f, .4f, .7f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(localSize.x, .2f, localSize.y));
        }
    }
}
