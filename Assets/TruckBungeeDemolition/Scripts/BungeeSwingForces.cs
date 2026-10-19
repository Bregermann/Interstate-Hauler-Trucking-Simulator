using UnityEngine;
namespace LWS.TruckBungeeDemolition
{
    // The established Bungee Destruction pump, shared without changing its tuning.
    public static class BungeeSwingForces
    {
        public static void Apply(Rigidbody body, Vector2 input, float acceleration)
        {
            input = Vector2.ClampMagnitude(input, 1);
            body.AddForce(new Vector3(input.x, 0, input.y) * acceleration, ForceMode.Acceleration);
            body.AddTorque(new Vector3(input.y, 0, -input.x) * .8f, ForceMode.Acceleration);
        }
    }
}
