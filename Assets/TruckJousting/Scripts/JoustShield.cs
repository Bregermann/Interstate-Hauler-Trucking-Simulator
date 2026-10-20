using UnityEngine;
namespace LWS.TruckJousting
{
    public sealed class JoustShield:MonoBehaviour
    {
        public JoustMatch match;public int owner;public Vector2 halfSize=Vector2.one;
        public bool Central(Vector3 world){var p=transform.InverseTransformPoint(world);return Mathf.Abs(p.x)<halfSize.x*.42f&&Mathf.Abs(p.y)<halfSize.y*.42f;}
    }
}
