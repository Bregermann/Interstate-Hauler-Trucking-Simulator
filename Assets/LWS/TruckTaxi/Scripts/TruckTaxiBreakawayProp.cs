using System.Collections;
using LWS.InterstateHauler;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public enum TruckTaxiPropPhysicsCategory { Lightweight, Breakaway, Destructible, Structural }

    // Author on small scenery. Structural objects keep their original colliders and physics.
    [DisallowMultipleComponent]
    public sealed class TruckTaxiBreakawayProp : MonoBehaviour
    {
        public TruckTaxiPropPhysicsCategory category = TruckTaxiPropPhysicsCategory.Breakaway;
        [Min(1)] public float activationSpeed = 2.5f;
        [Min(.1f)] public float massKg = 35f;
        [Min(.1f)] public float durability = 12f;
        [Min(0)] public int debrisPieces = 2;
        [Min(1)] public float debrisLifetimeSeconds = 8f;
        public bool Activated { get; private set; }
        public bool Destroyed { get; private set; }
        public float RemainingDurability { get; private set; }
        private Rigidbody body;
        private Collider[] colliders;
        private Renderer[] renderers;
        private Vector3 home;
        private Quaternion homeRotation;

        public void Configure(TruckTaxiPropPhysicsCategory nextCategory, float nextMassKg)
        {
            category = nextCategory; massKg = Mathf.Max(.1f, nextMassKg);
            if (body != null) body.mass = massKg;
        }

        private void Awake()
        {
            home = transform.position; homeRotation = transform.rotation;
            RemainingDurability = durability;
            if (category == TruckTaxiPropPhysicsCategory.Structural) return;
            body = GetComponent<Rigidbody>();
            if (body == null) body = gameObject.AddComponent<Rigidbody>();
            body.mass = massKg; body.isKinematic = true;
            colliders = GetComponentsInChildren<Collider>(true);
            renderers = GetComponentsInChildren<Renderer>(true);
            foreach (var collider in colliders) collider.isTrigger = true;
        }
        private void OnTriggerEnter(Collider other)
        {
            if (Activated || Destroyed || category == TruckTaxiPropPhysicsCategory.Structural || other == null) return;
            var host = TruckTaxiBootstrap.Instance;
            var truck = host?.Player?.GetComponent<Rigidbody>();
            if (truck == null || other.attachedRigidbody != truck) return;
            float speed = truck.linearVelocity.magnitude;
            if (speed < activationSpeed) return;
            Activate(truck, other, speed);
        }
        private void Activate(Rigidbody truck, Collider truckCollider, float speed)
        {
            Activated = true;
            var impact = GetComponent<TruckTaxiImpactTarget>();
            impact?.Hit();
            Vector3 direction = Vector3.ProjectOnPlane(transform.position - truck.worldCenterOfMass, Vector3.up).normalized;
            if (direction.sqrMagnitude < .01f) direction = truck.transform.forward;
            body.isKinematic = false;
            body.AddForce((direction + Vector3.up * .22f) * Mathf.Min(18f, speed * .8f), ForceMode.VelocityChange);
            body.AddTorque(Vector3.Cross(direction, Vector3.up) * Mathf.Min(9f, speed * .4f), ForceMode.VelocityChange);
            TruckTaxiBootstrap.Instance?.Session?.RecordEvent(TaxiEventType.PropDamage,
                impact != null ? impact.targetId : name, speed);
            if (speed > 8f)
                TruckTaxiBootstrap.Instance?.ComponentDamage?.ReceiveImpact(truckCollider.ClosestPoint(transform.position),
                    speed, massKg * speed);
            if (category == TruckTaxiPropPhysicsCategory.Destructible)
            {
                RemainingDurability -= speed * Mathf.Max(1, truck.mass / 2000f);
                if (RemainingDurability <= 0) { Shatter(); return; }
            }
            StartCoroutine(BecomeSolidAfterPassing(truckCollider));
        }
        private IEnumerator BecomeSolidAfterPassing(Collider truckCollider)
        {
            yield return new WaitForSeconds(.35f);
            if (Destroyed) yield break;
            foreach (var collider in colliders)
                if (collider != null)
                {
                    collider.isTrigger = false;
                    if (truckCollider != null) Physics.IgnoreCollision(collider, truckCollider, true);
                }
            yield return new WaitForSeconds(1f);
            foreach (var collider in colliders)
                if (collider != null && truckCollider != null) Physics.IgnoreCollision(collider, truckCollider, false);
        }
        private void Shatter()
        {
            Destroyed = true;
            foreach (var collider in colliders) if (collider != null) collider.enabled = false;
            foreach (var renderer in renderers) if (renderer != null) renderer.enabled = false;
            for (int i = 0; i < Mathf.Min(4, debrisPieces); i++)
            {
                var chunk = GameObject.CreatePrimitive(PrimitiveType.Cube);
                chunk.name = "Temporary prop debris";
                chunk.transform.position = transform.position + Vector3.up * .25f;
                chunk.transform.localScale = Vector3.one * .22f;
                if (renderers.Length > 0 && renderers[0] != null)
                    chunk.GetComponent<Renderer>().sharedMaterial = renderers[0].sharedMaterial;
                var piece = chunk.AddComponent<Rigidbody>(); piece.mass = Mathf.Max(.1f, massKg / Mathf.Max(1, debrisPieces));
                piece.AddForce((Vector3.up + transform.right * (i % 2 == 0 ? 1 : -1)) * 2f, ForceMode.VelocityChange);
                Destroy(chunk, debrisLifetimeSeconds);
            }
        }
        public void ResetProp()
        {
            if (category == TruckTaxiPropPhysicsCategory.Structural || body == null) return;
            StopAllCoroutines();
            body.isKinematic = true; body.linearVelocity = body.angularVelocity = Vector3.zero;
            transform.SetPositionAndRotation(home, homeRotation);
            foreach (var collider in colliders) if (collider != null) { collider.enabled = true; collider.isTrigger = true; }
            foreach (var renderer in renderers) if (renderer != null) renderer.enabled = true;
            RemainingDurability = durability; Destroyed = Activated = false;
        }
    }
}
