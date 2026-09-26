using System.Collections.Generic;
using UnityEngine;

namespace LWS.TruckTaxi
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class TruckTaxiCollisionObserver : MonoBehaviour
    {
        private TruckTaxiBootstrap host;
        private Rigidbody body;
        private Vector3 incomingVelocity;
        private readonly Dictionary<int,float> lastImpact = new Dictionary<int,float>();
        private readonly Dictionary<TruckTaxiImpactTarget,float> near = new Dictionary<TruckTaxiImpactTarget,float>();
        private readonly HashSet<TruckTaxiImpactTarget> current = new HashSet<TruckTaxiImpactTarget>();
        private readonly List<TruckTaxiImpactTarget> departed = new List<TruckTaxiImpactTarget>();
        private readonly Collider[] hits = new Collider[96];
        private float nextNearCheck;
        public float LastImpactSpeed { get; private set; }
        public Vector3 LastRelativeVelocity { get; private set; }
        public float LastImpulse { get; private set; }
        public string LastTarget { get; private set; }
        public float LastCollisionTime { get; private set; }
        public void Initialize(TruckTaxiBootstrap value) { host = value; body = GetComponent<Rigidbody>(); }
        private void FixedUpdate()
        {
            if (body != null) incomingVelocity = body.linearVelocity;
            if (host == null || Time.time < nextNearCheck) return;
            nextNearCheck = Time.time + 0.15f;
            current.Clear();
            int count = Physics.OverlapSphereNonAlloc(transform.position, host.Configuration.nearMissRadius, hits, ~0, QueryTriggerInteraction.Ignore);
            for (int i=0;i<count;i++)
            {
                var target = hits[i].GetComponentInParent<TruckTaxiImpactTarget>();
                if (target == null || target.kind != TaxiImpactKind.Traffic) continue;
                current.Add(target);
                if (!near.ContainsKey(target) && incomingVelocity.magnitude >= host.Configuration.nearMissSpeed) near[target] = Time.time;
            }
            departed.Clear();
            foreach (var pair in near)
            {
                if (pair.Key != null && current.Contains(pair.Key)) continue;
                departed.Add(pair.Key);
                if (pair.Key != null && (!lastImpact.TryGetValue(pair.Key.GetInstanceID(),out float at) || at < pair.Value))
                    host.Session.RecordEvent(TaxiEventType.NearMiss,pair.Key.targetId);
            }
            foreach (var target in departed) near.Remove(target);
        }
        private void OnCollisionEnter(Collision collision)
        {
            if (host == null || host.Session == null) return;
            var target = collision.collider.GetComponentInParent<TruckTaxiImpactTarget>();
            if(target!=null && target.kind==TaxiImpactKind.Pedestrian)
            {
                var pedestrian=target.GetComponent<TruckTaxiPedestrian>();
                if(pedestrian==null || pedestrian.IsRagdoll) return;
                Vector3 contact=collision.contactCount>0 ? collision.GetContact(0).point : target.transform.position;
                // Collision relative velocity is pre-solver; the other body's current
                // velocity already includes the contact impulse and understates the hit.
                Vector3 relative=incomingVelocity.normalized*collision.relativeVelocity.magnitude;
                if(Vector3.Dot(incomingVelocity,(contact-body.worldCenterOfMass).normalized)<=.1f) return;
                if(!pedestrian.TryStrike(relative,contact)) return;
                LastImpactSpeed=relative.magnitude; LastRelativeVelocity=relative;
                LastImpulse=pedestrian.LastImpulse.magnitude; LastTarget=target.targetId; LastCollisionTime=Time.time;
                host.Session.RecordPedestrianHit(target.targetId,LastImpactSpeed,pedestrian.LastImpulse,contact);
                pedestrian.MarkHitEventSent();
                return; // Ragdoll bone contacts must never fall through to generic collision scoring.
            }
            int key = target != null ? target.GetInstanceID() : collision.collider.GetInstanceID();
            if (lastImpact.TryGetValue(key,out float at) && Time.time - at < host.Configuration.collisionCooldown) return;
            if (collision.relativeVelocity.magnitude < host.Configuration.minimumImpactSpeed) return;
            // A car striking a stationary/receding tractor is not a player ram.
            Vector3 toward = collision.contactCount > 0 ? collision.GetContact(0).point - body.worldCenterOfMass : collision.transform.position - transform.position;
            bool playerCaused = Vector3.Dot(incomingVelocity, toward.normalized) > 1;
            lastImpact[key] = Time.time;
            LastImpactSpeed = collision.relativeVelocity.magnitude; LastRelativeVelocity = collision.relativeVelocity;
            LastImpulse = collision.impulse.magnitude; LastTarget = target != null ? target.targetId : collision.collider.name; LastCollisionTime = Time.time;
            if (target == null && incomingVelocity.y < -host.Configuration.hardLandingSpeed &&
                collision.contactCount > 0 && collision.GetContact(0).normal.y > .7f)
            {
                host.Session.RecordEvent(TaxiEventType.HardLanding,LastTarget,LastImpactSpeed);
                return;
            }
            if (!playerCaused) return;
            TaxiEventType type = TaxiEventType.Collision;
            if (target != null && playerCaused && target.Hit())
                type = target.kind == TaxiImpactKind.Traffic ? TaxiEventType.TrafficRam :
                    target.kind == TaxiImpactKind.Pedestrian ? TaxiEventType.PedestrianHit : TaxiEventType.PropDamage;
            host.Session.RecordEvent(type, LastTarget, LastImpactSpeed);
        }
        public void ResetTracking() { near.Clear(); lastImpact.Clear(); }
    }
}
