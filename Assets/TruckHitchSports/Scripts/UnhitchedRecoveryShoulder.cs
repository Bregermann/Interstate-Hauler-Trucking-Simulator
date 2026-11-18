using UnityEngine;
using LWS.TruckSports;

namespace LWS.TruckHitchSports
{
    // Authored recovery-road metadata only. EasyRoads owns the static mesh;
    // NWH/PhysX own all actual driving, jumping and contact outcomes.
    public sealed class UnhitchedRecoveryShoulder : MonoBehaviour
    {
        public Transform[] GuidePoints;
        public Collider[] DrivingSurfaces;
        public float InnerLaunchOffset = -.7f;
        public Vector3 PointAtForwardPosition(float worldZ, bool launchStrip = false)
        {
            if (GuidePoints == null || GuidePoints.Length == 0) return transform.position;
            for (int i = 1; i < GuidePoints.Length; i++)
            {
                Vector3 a = GuidePoints[i - 1].position, b = GuidePoints[i].position;
                if (worldZ > b.z && i < GuidePoints.Length - 1) continue;
                Vector3 point = Vector3.Lerp(a, b, Mathf.InverseLerp(a.z, b.z, worldZ));
                if (launchStrip) point += Vector3.right * InnerLaunchOffset;
                return point;
            }
            return GuidePoints[GuidePoints.Length - 1].position;
        }
        public bool HasNativeWheelContact(SportsEventTruck truck)
        {
            foreach (var wheel in truck.wheels) if (wheel != null && wheel.IsGrounded && Vector3.Dot(wheel.HitNormal, Vector3.up) > .65f)
                foreach (var surface in DrivingSurfaces) if (wheel.HitCollider == surface) return true;
            return false;
        }
    }
}
