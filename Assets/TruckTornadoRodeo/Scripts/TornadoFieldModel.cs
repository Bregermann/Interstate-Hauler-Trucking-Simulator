using System;

namespace LWS.TruckTornadoRodeo
{
    // One mode-local spatial hazard, not a weather service. Weather Maker's
    // current global flow is an input; no global wind/gravity is overwritten.
    public static class TornadoFieldModel
    {
        public const float Radius = 68, Height = 120, MaximumAcceleration = 28;
        public static void Sample(float x, float y, float z, float vx, float vy, float vz, float seconds, float windX, float windZ, out float ax, out float ay, out float az)
        {
            ax = ay = az = 0;
            if (!Finite(x) || !Finite(y) || !Finite(z) || !Finite(vx) || !Finite(vy) || !Finite(vz) || !Finite(seconds) || !Finite(windX) || !Finite(windZ) || y < -5 || y > Height + 12) return;
            float radius = (float)Math.Sqrt(x * x + z * z); if (radius >= Radius) return;
            float nx = radius > .01f ? x / radius : 0, nz = radius > .01f ? z / radius : 0;
            float edge = Math.Clamp((Radius - radius) / 20, 0, 1), top = Math.Clamp((Height + 12 - y) / 24, 0, 1);
            float pulse = 1 + .14f * (float)Math.Sin(seconds * .63f) + .07f * (float)Math.Sin(seconds * 1.71f + .6f);
            float core = Math.Clamp(1 - radius / 60, 0, 1);
            float radialVelocity = nx * vx + nz * vz, tangentVelocity = -nz * vx + nx * vz;
            float suction = (3 + core * 8) * pulse + radialVelocity * .4f;
            float orbit = ((12 + core * 5) * pulse - tangentVelocity) * .9f;
            float lift = (12 + core * 12) * Math.Clamp(1 - Math.Max(0, y - 6) / 112, 0, 1) * pulse;
            // Velocity-relative drag bounds terminal travel through force, never
            // by assigning a Rigidbody velocity or moving an airborne tractor.
            ax = (-nx * suction - nz * orbit - vx * .08f + (float)Math.Sin(seconds * 1.9f + y * .15f) * 1.1f + Math.Clamp(windX, -20, 20) * .12f) * edge * top;
            az = (-nz * suction + nx * orbit - vz * .08f + (float)Math.Cos(seconds * 1.3f + x * .08f) * 1.1f + Math.Clamp(windZ, -20, 20) * .12f) * edge * top;
            ay = (lift - vy * .52f + (float)Math.Sin(seconds * 2.1f + z * .12f) * .7f) * edge * top;
            if (y > 85) ay -= Math.Min(18, (y - 85) * .55f);
            float magnitude = (float)Math.Sqrt(ax * ax + ay * ay + az * az);
            if (magnitude > MaximumAcceleration) { float scale = MaximumAcceleration / magnitude; ax *= scale; ay *= scale; az *= scale; }
        }
        public static float Strength(float radius) => Finite(radius) ? Math.Clamp((Radius - Math.Max(0, radius)) / 20, 0, 1) : 0;
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
