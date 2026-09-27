using System.Collections;
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
        [Range(0, 1)] public float extremeChance = .04f;
        public AudioClip angryBark;
        public static int ActiveCount { get; private set; }
        public static int ExtremeCount { get; private set; }
        private static float nextGlobalExtreme;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetActiveCount() { ActiveCount = ExtremeCount = 0; nextGlobalExtreme = 0; }
        private Component ai;
        private PropertyInfo acceleration, followingDistance;
        private float normalAcceleration, normalDistance, endsAt, nextAllowed;
        private bool active;
        private bool extremeActive;
        private GameObject driverVisual;
        private FieldInfo stopField;
        private readonly Collider[] nearby = new Collider[24];

        private void Awake()
        {
            foreach (var component in GetComponentsInChildren<MonoBehaviour>(true))
                if (component.GetType().Name == "CarAIController") { ai = component; break; }
            if (ai == null) return;
            acceleration = ai.GetType().GetProperty("INCREASE");
            followingDistance = ai.GetType().GetProperty("TO_CAR");
            stopField = ai.GetType().GetField("tempStop", BindingFlags.Public | BindingFlags.Instance);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.relativeVelocity.sqrMagnitude < 9 || Random.value > collisionTriggerChance) return;
            var target = collision.collider.GetComponentInParent<TruckTaxiImpactTarget>();
            if ((target == null || target.kind != TaxiImpactKind.Traffic) &&
                collision.collider.GetComponentInParent<TruckTaxiCollisionObserver>() == null) return;
            TryTrigger(collision.collider.attachedRigidbody);
        }

        public bool TryTrigger() => TryTrigger(null);

        public bool TryTrigger(Rigidbody offender)
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
            if (offender != null && Random.value < extremeChance && Time.time >= nextGlobalExtreme &&
                ExtremeCount == 0 && GetComponent<TruckTaxiTrafficPooledActor>()?.FullPhysics == true)
                StartCoroutine(ExtremeRetaliation(offender));
            return true;
        }

        private IEnumerator ExtremeRetaliation(Rigidbody offender)
        {
            extremeActive = true; ExtremeCount++; nextGlobalExtreme = Time.time + 90;
            stopField?.SetValue(ai, true);
            yield return new WaitForSeconds(.8f);
            if (offender != null && gameObject.activeInHierarchy)
            {
                driverVisual = new GameObject("Taxi angry Wobble driver");
                driverVisual.transform.position = transform.position + transform.right * 2.5f;
                var sourceRenderer = GetComponentInChildren<Renderer>();
                Material paint = sourceRenderer != null ? sourceRenderer.sharedMaterial : null;
                TruckTaxiWobbleVisual.Create(driverVisual.transform, 1.5f, 1, paint, paint);
                if (angryBark != null)
                {
                    var source = driverVisual.AddComponent<AudioSource>();
                    source.playOnAwake = false; source.spatialBlend = 1;
                    source.rolloffMode = AudioRolloffMode.Linear; source.minDistance = 3; source.maxDistance = 30;
                    TruckTaxiAudioController.Instance?.Route(source, TruckTaxiAudioCategory.World);
                    source.PlayOneShot(angryBark);
                }
                float until = Time.time + .7f;
                while (Time.time < until && offender != null && gameObject.activeInHierarchy)
                {
                    driverVisual.transform.rotation = Quaternion.LookRotation((offender.position - driverVisual.transform.position).normalized);
                    yield return null;
                }
                if (offender != null && gameObject.activeInHierarchy)
                    TruckTaxiNpcProjectileLauncher.LaunchNpcProjectile(driverVisual.transform.position + Vector3.up * 1.2f,
                        offender.worldCenterOfMass, ai.GetComponent<Rigidbody>(), TruckTaxiNpcProjectileStyle.RoadRageGrenade);
            }
            yield return new WaitForSeconds(1.5f);
            EndExtreme();
        }

        private void EndExtreme()
        {
            if (!extremeActive) return;
            extremeActive = false; ExtremeCount = Mathf.Max(0, ExtremeCount - 1);
            if (ai != null) stopField?.SetValue(ai, false);
            if (driverVisual != null) Destroy(driverVisual);
        }

        private void Update()
        {
            if (active && Time.time >= endsAt) End();
        }

        private void OnDisable() { StopAllCoroutines(); EndExtreme(); End(); }

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
