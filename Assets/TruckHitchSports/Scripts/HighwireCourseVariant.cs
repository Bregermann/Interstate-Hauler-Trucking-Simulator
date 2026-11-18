using UnityEngine;

namespace LWS.TruckHitchSports
{
    public sealed class HighwireCourseVariant : MonoBehaviour
    {
        public int Mode;
        public Vector3[] Points;
        public bool[] ReverseNodes, StopNodes;
        public float RecommendedSeconds = 210;
        public HighwireMovingBarrier[] Barriers;
        public bool RequiresReverse(int node) => ReverseNodes != null && node >= 0 && node < ReverseNodes.Length && ReverseNodes[node];
        public bool RequiresStop(int node) => StopNodes != null && node >= 0 && node < StopNodes.Length && StopNodes[node];
        public bool BlockedAhead(Vector3 point)
        {
            if (Barriers != null) foreach (var barrier in Barriers)
                if (barrier != null && barrier.gameObject.activeInHierarchy && barrier.ShouldWait(point)) return true;
            return false;
        }
        public float Deviation(Vector3 point, Vector3 start, float laneOffset)
        {
            float nearest = float.MaxValue; Vector3 a = start;
            foreach (var node in Points)
            {
                Vector3 b = node + Vector3.right * laneOffset, line = Vector3.ProjectOnPlane(b - a, Vector3.up);
                float t = line.sqrMagnitude > .01f ? Mathf.Clamp01(Vector3.Dot(Vector3.ProjectOnPlane(point - a, Vector3.up), line) / line.sqrMagnitude) : 0;
                nearest = Mathf.Min(nearest, Vector3.ProjectOnPlane(point - (a + line * t), Vector3.up).magnitude); a = b;
            }
            return nearest;
        }
    }
}
