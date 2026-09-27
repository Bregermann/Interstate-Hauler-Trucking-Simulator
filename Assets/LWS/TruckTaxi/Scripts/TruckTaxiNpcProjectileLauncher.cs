using UnityEngine;

namespace LWS.TruckTaxi
{
    public enum TruckTaxiNpcProjectileStyle { FilledJug, RoadRageGrenade }

    // Shared NPC throw entry point. The projectile retains the existing Taxi blast/impact rules.
    public static class TruckTaxiNpcProjectileLauncher
    {
        private const int MaximumLiveProjectiles = 3;
        private static int liveProjectiles;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { liveProjectiles = 0; }

        public static TruckTaxiFilledContainerProjectile LaunchNpcProjectile(Vector3 origin, Vector3 target,
            Rigidbody owner, TruckTaxiNpcProjectileStyle style)
        {
            var player = TruckTaxiBootstrap.Instance?.Player;
            if (liveProjectiles >= MaximumLiveProjectiles || !Finite(origin) || !Finite(target) ||
                (target - origin).sqrMagnitude > 80 * 80 ||
                (player != null && (player.transform.position - origin).sqrMagnitude > 100 * 100)) return null;

            bool grenade = style == TruckTaxiNpcProjectileStyle.RoadRageGrenade;
            var visual = GameObject.CreatePrimitive(grenade ? PrimitiveType.Sphere : PrimitiveType.Cylinder);
            visual.name = grenade ? "Taxi cartoon road rage grenade" : "Taxi NPC filled jug";
            visual.transform.position = origin;
            visual.transform.localScale = grenade ? Vector3.one * .25f : new Vector3(.12f, .2f, .12f);
            var renderer = visual.GetComponent<Renderer>();
            var color = new MaterialPropertyBlock();
            color.SetColor("_BaseColor", grenade ? new Color(1f, .25f, .08f) : new Color(.75f, .85f, .25f));
            color.SetColor("_Color", grenade ? new Color(1f, .25f, .08f) : new Color(.75f, .85f, .25f));
            renderer.SetPropertyBlock(color);
            var body = visual.AddComponent<Rigidbody>();
            body.mass = .35f;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var projectile = visual.AddComponent<TruckTaxiFilledContainerProjectile>();
            projectile.ConfigureNpc(null, owner, grenade);
            projectile.detonateOnImpact = grenade;
            projectile.maximumLifetime = 15;
            visual.AddComponent<LiveProjectile>().OnGone = () => liveProjectiles = Mathf.Max(0, liveProjectiles - 1);
            liveProjectiles++;

            const float flightSeconds = .75f;
            Vector3 velocity = (target - origin) / flightSeconds - Physics.gravity * (.5f * flightSeconds);
            body.linearVelocity = Vector3.ClampMagnitude(velocity, 30);
            return projectile;
        }

        private static bool Finite(Vector3 point) => float.IsFinite(point.x) && float.IsFinite(point.y) && float.IsFinite(point.z);

        private sealed class LiveProjectile : MonoBehaviour
        {
            public System.Action OnGone;
            private void OnDestroy() { OnGone?.Invoke(); }
        }
    }
}
