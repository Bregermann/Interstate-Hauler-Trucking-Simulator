using UnityEngine;

namespace LWS.TruckMechanicalBull
{
    // One kinematic physics owner. Contact forces are supplied by PhysX/NWH;
    // neither the player pose nor their velocity is copied from the platform.
    public sealed class MechanicalBullMachine : MonoBehaviour
    {
        public Rigidbody body;
        public BoxCollider deck;
        public Transform piston, yawRing;
        Vector3 home;
        Quaternion upright;
        bool initialized;
        public float Intensity { get; private set; }
        public int Steps { get; private set; }
        public Vector3 TopCenter => deck.transform.TransformPoint(deck.center + Vector3.up * deck.size.y * .5f);
        public Vector3 Normal => body.rotation * Vector3.up;

        public void ResetMachine()
        {
            if (!initialized) { home = body.position; upright = body.rotation; initialized = true; }
            body.isKinematic = true; body.position = home; body.rotation = upright; Intensity = 0; Steps = 0;
        }
        public static void Pattern(float elapsed, out Vector3 offset, out Vector3 euler)
        {
            float t = Mathf.Max(0, elapsed), intensity = Mathf.Clamp01(t / 45);
            float phase = t * Mathf.Lerp(1.4f, 2.5f, intensity);
            // Smooth compound harmonics produce changing pitch/roll, a true
            // vertical buck and yaw reversals without discontinuous velocities.
            offset = Vector3.up * (Mathf.Sin(phase * 1.7f) + .28f * Mathf.Sin(phase * 3.1f)) * Mathf.Lerp(.055f, .38f, intensity);
            euler = new Vector3(Mathf.Sin(phase) * Mathf.Lerp(5, 24, intensity),
                Mathf.Sin(t * .31f) * Mathf.Lerp(12, 65, intensity),
                (Mathf.Sin(phase * .79f + .4f) - Mathf.Sin(.4f)) * Mathf.Lerp(4, 19, intensity));
        }
        public void Step(float elapsed)
        {
            Pattern(elapsed, out var offset, out var euler); Intensity = Mathf.Clamp01(elapsed / 45); Steps++;
            body.MovePosition(home + offset); body.MoveRotation(upright * Quaternion.Euler(euler));
            if (piston != null) piston.localScale = new Vector3(1, 1 + offset.y * .12f, 1);
            if (yawRing != null) yawRing.localRotation = Quaternion.Euler(0, euler.y, 0);
        }
        public bool Outside(Vector3 world, float margin)
        {
            var p = deck.transform.InverseTransformPoint(world) - deck.center;
            return Mathf.Abs(p.x) > deck.size.x * .5f + margin || Mathf.Abs(p.z) > deck.size.z * .5f + margin;
        }
    }
}
