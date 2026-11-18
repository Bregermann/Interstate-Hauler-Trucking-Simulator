using System;
using UnityEngine;
using UnityEngine.VFX;

namespace LWS.TruckWaterSports
{
    [Flags]
    public enum RiverWhirlpoolVariants { Race = 1, Slalom = 2, Freestyle = 4, Survival = 8, All = 15 }

    [Serializable]
    public sealed class RiverWhirlpool
    {
        public string name = "River whirlpool";
        public Transform center;
        [Min(.1f)] public float radius = 7.5f, coreRadius = 2;
        [Min(0)] public float swirlSpeed = 3.5f, inwardSpeed = 1.4f, downSpeed = .8f;
        [Min(.1f)] public float affectedDepth = 4;
        public bool clockwise;
    }

    /// <summary>
    /// Authored velocity-field adapter sampled by WaterCourse.Current. It NEVER
    /// applies Rigidbody forces, sets water height, or duplicates Archimeds.
    /// The existing WaterVessel current response remains the sole force authority.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RiverWhirlpoolCurrentField : MonoBehaviour
    {
        public WaterMatch match;
        public WaterCourse course;
        public RiverWhirlpoolVariants variants = RiverWhirlpoolVariants.Slalom | RiverWhirlpoolVariants.Survival;
        public RiverWhirlpool[] whirlpools = Array.Empty<RiverWhirlpool>();
        public VisualEffect[] foamEffects = Array.Empty<VisualEffect>();
        bool visible, presentationInitialized;
        string visibleMatch;

        public bool Active => isActiveAndEnabled && match != null && match.Live && course != null && ((int)variants & 1 << match.option) != 0;

        public Vector3 SampleCurrent(Vector3 position, float clock)
        {
            if (!Active || whirlpools == null) return Vector3.zero;
            float surface = course.SurfacePointXZ(position).y;
            Vector3 current = Vector3.zero;
            foreach (var whirlpool in whirlpools)
                if (whirlpool != null && whirlpool.center != null)
                    current += Sample(whirlpool, whirlpool.center.position, position, surface, clock);
            return current;
        }

        /// <summary>Current velocity in metres/second; finite at the core, zero outside real water depth/radius.</summary>
        public static Vector3 Sample(RiverWhirlpool whirlpool, Vector3 center, Vector3 position, float surfaceHeight, float clock)
        {
            if (whirlpool == null || whirlpool.radius <= .01f || whirlpool.affectedDepth <= .01f || position.y > surfaceHeight + 1 || position.y < surfaceHeight - whirlpool.affectedDepth)
                return Vector3.zero;
            Vector3 radial = Vector3.ProjectOnPlane(position - center, Vector3.up);
            float distance = radial.magnitude;
            if (distance >= whirlpool.radius) return Vector3.zero;
            float core = Mathf.Clamp(whirlpool.coreRadius, .05f, whirlpool.radius);
            float edge = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(whirlpool.radius, core, distance));
            float depth = 1 - Mathf.SmoothStep(0, 1, Mathf.Clamp01((surfaceHeight - position.y) / whirlpool.affectedDepth));
            float coreFalloff = 1 - Mathf.Clamp01(distance / core);
            float pulse = 1 + Mathf.Sin(clock * .65f + center.z * .03f) * .1f;
            Vector3 outward = distance > .001f ? radial / distance : Vector3.zero;
            Vector3 tangent = Vector3.Cross(Vector3.up, outward) * (whirlpool.clockwise ? -1 : 1);
            // The tangential/inward terms approach zero at the exact core, avoiding
            // singular acceleration; the shallow down-current marks the whirlpool.
            float coreRamp = Mathf.Clamp01(distance / core);
            return (tangent * Mathf.Max(0, whirlpool.swirlSpeed) - outward * Mathf.Max(0, whirlpool.inwardSpeed)) * (edge * depth * coreRamp * pulse)
                - Vector3.up * (Mathf.Max(0, whirlpool.downSpeed) * coreFalloff * coreFalloff * depth * pulse);
        }

        void Update()
        {
            bool active = Active;
            if (presentationInitialized && active == visible && (!active || visibleMatch == match.MatchId)) return;
            presentationInitialized = true;
            visible = active; visibleMatch = match != null ? match.MatchId : null;
            foreach (var effect in foamEffects) if (effect != null)
            {
                effect.enabled = active;
                if (active) { effect.Reinit(); effect.Play(); } else effect.Stop();
            }
        }
        void OnDisable()
        {
            visible = false; presentationInitialized = false;
            foreach (var effect in foamEffects) if (effect != null) { effect.Stop(); effect.enabled = false; }
        }
        void OnDrawGizmosSelected()
        {
            if (whirlpools == null) return;
            foreach (var whirlpool in whirlpools) if (whirlpool?.center != null)
            {
                Gizmos.color = new Color(.1f, .8f, 1, .6f); Gizmos.DrawWireSphere(whirlpool.center.position, whirlpool.radius);
                Gizmos.color = new Color(.15f, .35f, .8f, .8f); Gizmos.DrawLine(whirlpool.center.position, whirlpool.center.position - Vector3.up * whirlpool.affectedDepth);
            }
        }
    }
}
