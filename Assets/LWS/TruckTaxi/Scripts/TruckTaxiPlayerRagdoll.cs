using System.Reflection;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // Owns only the player's lease on the existing Taxi/UTS ragdoll, never NPC population or scoring.
    public sealed class TruckTaxiPlayerRagdoll : MonoBehaviour
    {
        private TruckTaxiPedestrian body;
        public bool Ready => body != null && body.HasJointedRagdoll && body.CanReuse;
        public bool IsActive => body != null && body.IsRagdoll;
        public string Status => !Ready ? "No compatible UTS jointed ragdoll configured." : IsActive ? "KNOCKED DOWN" : "READY";
        public Vector3 Position => body != null && body.RagdollBodies.Count > 0 && body.RagdollBodies[0] != null
            ? body.RagdollBodies[0].position : transform.position;
        public Vector3 LastImpulse => body != null ? body.LastImpulse : Vector3.zero;
        public bool Contains(Transform candidate) => body != null && candidate != null && candidate.IsChildOf(body.transform);

        public void Initialize(GameObject prefab, Component[] paths)
        {
            if (body != null) return;
            if (!Compatible(prefab) && paths != null)
                foreach (var path in paths)
                {
                    if (path == null) continue;
                    var prefabs = path.GetType().GetField("walkingPrefabs", BindingFlags.Public | BindingFlags.Instance)?.GetValue(path) as GameObject[];
                    if (prefabs == null) continue;
                    foreach (var candidate in prefabs)
                        if (Compatible(candidate)) { prefab = candidate; break; }
                    if (Compatible(prefab)) break;
                }
            if (!Compatible(prefab)) return;
            var instance = Instantiate(prefab, transform);
            instance.name = "Player UTS recovery ragdoll";
            body = instance.GetComponent<TruckTaxiPedestrian>() ?? instance.AddComponent<TruckTaxiPedestrian>();
            // Player recovery owns lifetime; never let the population retirement timer run.
            body.enabled = false;
            body.settings = new TruckTaxiPedestrianImpactSettings {
                minimumRagdollImpactSpeed = 1, maximumRagdollImpulse = 180,
                impactForceMultiplier = 12, upwardImpulse = 32, angularImpulse = 1
            };
            body.PrepareForPool();
        }
        private static bool Compatible(GameObject prefab)
        {
            if (prefab == null || prefab.GetComponentsInChildren<Rigidbody>(true).Length < 3 ||
                prefab.GetComponentsInChildren<Joint>(true).Length < 2) return false;
            foreach (var component in prefab.GetComponents<MonoBehaviour>())
                if (component is ITruckTaxiPedestrianRagdoll) return true;
            return false;
        }
        public bool Begin(Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 contact)
        {
            if (!Ready || IsActive || !TruckTaxiPedestrianImpactSettings.Finite(velocity) ||
                !TruckTaxiPedestrianImpactSettings.Finite(position) || !TruckTaxiPedestrianImpactSettings.Finite(contact)) return false;
            body.Reactivate(position, rotation, "taxi.player.on-foot", false);
            if (body.TryStrike(Vector3.ClampMagnitude(velocity, 30), contact)) return true;
            body.PrepareForPool(); return false;
        }
        public void ResetBody() { if (body != null) body.PrepareForPool(); }
        private void OnDisable() => ResetBody();
        private void OnDestroy() { if (body != null) Destroy(body.gameObject); }
    }

    public sealed class TruckTaxiPlayerTrafficContact : MonoBehaviour
    {
        private TruckTaxiWorldLifeController owner;
        public void Initialize(TruckTaxiWorldLifeController value) => owner = value;
        private void OnTriggerEnter(Collider other) => owner?.TryTrafficImpact(other);
        private void OnTriggerStay(Collider other) => owner?.TryTrafficImpact(other);
    }
}
