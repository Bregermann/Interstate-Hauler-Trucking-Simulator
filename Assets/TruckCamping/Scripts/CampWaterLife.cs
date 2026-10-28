using UnityEngine;
namespace LWS.TruckCamping
{
    public sealed class CampWaterLife : MonoBehaviour
    {
        public CampingWilderness world;
        public Animator animator;
        public Vector3 center;
        public Vector2 radii = new Vector2(7, 12);
        public float depth = 1.4f, pace = .2f, phase;
        public int AnimatedFrames { get; private set; }
        void Update()
        {
            if (Time.deltaTime <= 0 || world.session == null || !world.session.Live) return;
            phase += Time.deltaTime * pace;
            var p = center + new Vector3(Mathf.Cos(phase) * radii.x, -depth + Mathf.Sin(phase * 2) * .15f, Mathf.Sin(phase) * radii.y);
            var tangent = new Vector3(-Mathf.Sin(phase) * radii.x, 0, Mathf.Cos(phase) * radii.y);
            transform.SetPositionAndRotation(p, Quaternion.LookRotation(tangent)); AnimatedFrames++;
        }
    }
}
