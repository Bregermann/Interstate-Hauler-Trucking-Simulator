using LWS.TruckSports;
using UnityEngine;

namespace LWS.TruckTornadoRodeo
{
    public sealed class TornadoField : MonoBehaviour
    {
        public TornadoMatch match;
        public Vector3 Center => transform.position;
        public Vector3 Acceleration(Vector3 worldPosition, Vector3 velocity)
        {
            var p = worldPosition - Center; Vector3 wind = match.presentationWeather != null ? match.presentationWeather.Flow : Vector3.zero;
            TornadoFieldModel.Sample(p.x, p.y, p.z, velocity.x, velocity.y, velocity.z, match.rules.Clock, wind.x, wind.z, out float x, out float y, out float z);
            return new Vector3(x, y, z);
        }
        public void Apply(Rigidbody body, float response = 1)
        {
            if (body == null || body.isKinematic || !match.Live || !match.rules.Live) return;
            var acceleration = Acceleration(body.worldCenterOfMass, body.linearVelocity) * Mathf.Clamp01(response);
            body.AddForce(acceleration, ForceMode.Acceleration);
        }
    }
}
