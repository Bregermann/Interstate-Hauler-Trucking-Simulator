using System.Reflection;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // Temporary changes to UTS's public cruise and following-distance controls only.
    public sealed class TruckTaxiRoadRage : MonoBehaviour
    {
        [Range(0, 1)] public float collisionTriggerChance = .08f;
        [Min(1)] public float cooldownSeconds = 45;
        [Min(1)] public float durationSeconds = 4;
        public static int ActiveCount { get; private set; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetActiveCount() { ActiveCount = 0; }
        private Component ai;
        private PropertyInfo acceleration, followingDistance;
        private float normalAcceleration, normalDistance, endsAt, nextAllowed;
        private bool active;
        private readonly Collider[] nearby = new Collider[24];

        private void Awake()
        {
            foreach (var component in GetComponentsInChildren<MonoBehaviour>(true))
                if (component.GetType().Name == "CarAIController") { ai = component; break; }
            if (ai == null) return;
            acceleration = ai.GetType().GetProperty("INCREASE");
            followingDistance = ai.GetType().GetProperty("TO_CAR");
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.relativeVelocity.sqrMagnitude < 9 || Random.value > collisionTriggerChance) return;
            var target = collision.collider.GetComponentInParent<TruckTaxiImpactTarget>();
            if ((target == null || target.kind != TaxiImpactKind.Traffic) &&
                collision.collider.GetComponentInParent<TruckTaxiCollisionObserver>() == null) return;
            TryTrigger();
        }

        public bool TryTrigger()
        {
            if (active || Time.time < nextAllowed || ActiveCount >= 2 || ai == null || acceleration == null || followingDistance == null) return false;
            normalAcceleration = (float)acceleration.GetValue(ai);
            normalDistance = (float)followingDistance.GetValue(ai);
            acceleration.SetValue(ai, Mathf.Min(normalAcceleration * 1.15f, normalAcceleration + .3f));
            followingDistance.SetValue(ai, Mathf.Max(12, normalDistance * .8f));
            endsAt = Time.time + Mathf.Max(1, durationSeconds);
            nextAllowed = endsAt + Mathf.Max(1, cooldownSeconds);
            active = true;
            ActiveCount++;
            foreach (var source in GetComponentsInChildren<AudioSource>(true))
                if (source.clip != null && source.clip.name.ToLowerInvariant().Contains("horn")) { source.PlayOneShot(source.clip); break; }
            int count = Physics.OverlapSphereNonAlloc(transform.position, 7, nearby, ~0, QueryTriggerInteraction.Ignore);
            for (int i=0;i<count;i++) nearby[i].GetComponentInParent<TruckTaxiPedestrianGesture>()?.ReactToBadDriver();
            return true;
        }

        private void Update()
        {
            if (active && Time.time >= endsAt) End();
        }

        private void OnDisable() { End(); }

        private void End()
        {
            if (!active) return;
            active = false;
            ActiveCount = Mathf.Max(0, ActiveCount - 1);
            if (ai == null) return;
            acceleration?.SetValue(ai, normalAcceleration);
            followingDistance?.SetValue(ai, normalDistance);
        }
    }
}
