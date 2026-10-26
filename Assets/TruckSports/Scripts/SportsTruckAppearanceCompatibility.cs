using System;
using UnityEngine;

namespace LWS.TruckSports
{
    // Authored per sport. These poses affect rendering only, never the NWH rig.
    [Serializable]
    public sealed class SportsTruckAppearanceCompatibility
    {
        [Serializable] public struct PaintSlot { public int rendererIndex, materialIndex; }
        [Serializable] public sealed class WheelGeometry
        {
            public int physicalWheel;
            public int[] rendererIndices = Array.Empty<int>();
            public Vector3 center, euler;
            public float diameter, visualDiameter;
        }
        public string bodyId;
        public Vector3 position, euler, scale = Vector3.one;
        public PaintSlot[] paintSlots = Array.Empty<PaintSlot>();
        public WheelGeometry[] wheelGeometry = Array.Empty<WheelGeometry>();
        public string notice;
        // Material names audited from the installed GR3D/Monster content. UTS
        // bodies use combined atlases, which retain their authored colors.
        // Their separate sport-owned livery is the only extra paint target.
        public static bool IsPaintMaterial(string bodyId, Material material)
        {
            if (material == null) return false;
            string name = material.name;
            if (name == "Sports participant livery") return true;
            if (name.StartsWith("010416TRUK-bodycolor", StringComparison.Ordinal)) return true;
            return (bodyId == "truck.monster" || bodyId == "truck.lifted") && name == "Body";
        }
        public bool Valid => !string.IsNullOrEmpty(bodyId) && Finite(position) && Finite(euler) &&
            Finite(scale) && scale.x > 0 && scale.y > 0 && scale.z > 0;
        static bool Finite(Vector3 v) => float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
    }
}
