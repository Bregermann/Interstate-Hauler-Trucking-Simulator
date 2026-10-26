using System;
using UnityEngine;
namespace LWS.TruckDockDiving
{
    public sealed class DockAppearanceSplitSettings : ScriptableObject
    {
        [Serializable] public sealed class Plane
        {
            public string bodyId;
            [Tooltip("Original vehicle-local metres. Seeded from Dock's0.65m high-body reference; visual cut only.")] public float vehiclePlaneY = .65f;
            [HideInInspector] public Matrix4x4 canonicalToVehicle = Matrix4x4.identity;
        }
        [Tooltip("Original visual-root scale converts the existing LOCAL split amount to world separation; never reinterpret it as metres.")] public float legacyVisualYScale = 1;
        public string previewBodyId = "semi.standard";
        public Plane[] planes = Array.Empty<Plane>();
    }
}
