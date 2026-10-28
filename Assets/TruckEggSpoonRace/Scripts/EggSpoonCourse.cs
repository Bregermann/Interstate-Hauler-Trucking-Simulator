using UnityEngine;
namespace LWS.TruckEggSpoonRace
{
    public sealed class EggSpoonCourse : MonoBehaviour
    {
        public const float Finish = 180;
        public static readonly float[] Gates = { 45, 90, 135 };
        public static float Center(int slot, float z) => (slot - 1.5f) * 10 + Mathf.Sin(z * .032f) * 1.2f;
        public static float Height(float z) => .35f + Mathf.Sin(Mathf.Clamp(z, 0, Finish) * Mathf.PI / Finish) * 1.8f + Mathf.Sin(z * .23f) * .10f;
        public static Vector3 Point(int slot, float z) => new Vector3(Center(slot, z), Height(z), z);
        public static float StationZ(int secured) => secured == 0 ? 0 : Gates[Mathf.Clamp(secured - 1, 0, Gates.Length - 1)];
        public static bool Crossed(Vector3 before, Vector3 after, float z, int slot)
        {
            if (before.z >= z || after.z < z) return false;
            float x = Mathf.Lerp(before.x, after.x, Mathf.InverseLerp(before.z, after.z, z));
            return Mathf.Abs(x - Center(slot, z)) < 4.2f;
        }
    }
}
