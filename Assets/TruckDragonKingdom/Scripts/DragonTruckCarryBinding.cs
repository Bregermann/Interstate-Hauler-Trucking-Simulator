using System;
using System.Collections.Generic;
using LWS.TruckSports;
using UnityEngine;

namespace LWS.TruckDragonKingdom
{
    // A Pickable has one Rigidbody; an NWH tractor has several. This adapter
    // preserves the complete authored assembly while a saddle/claw/vessel owns
    // its pose. It never reparents or edits the saved appearance/prefab.
    public sealed class DragonTruckCarryBinding : IDisposable
    {
        sealed class BodyState
        {
            public Rigidbody body;
            public Vector3 offset;
            public Quaternion rotation;
            public bool kinematic, gravity, collisions;
            public RigidbodyConstraints constraints;
            public RigidbodyInterpolation interpolation;
        }
        struct CollisionPair { public Collider truck, carrier; public bool ignored; }
        readonly SportsEventTruck truck;
        readonly bool powered;
        readonly BodyState[] bodies;
        readonly List<CollisionPair> ignoredPairs = new List<CollisionPair>();
        bool released;
        public bool Released => released;
        public SportsEventTruck Truck => truck;

        public DragonTruckCarryBinding(SportsEventTruck truck, Pose carrierPose, Collider[] carrierColliders = null)
        {
            if (truck == null || truck.body == null) throw new ArgumentException("A complete NWH truck assembly is required.", nameof(truck));
            this.truck = truck; powered = truck.Powered;
            var all = truck.GetComponentsInChildren<Rigidbody>(true);
            bodies = new BodyState[all.Length];
            for (int i = 0; i < all.Length; i++)
            {
                var body = all[i];
                bodies[i] = new BodyState {
                    body = body, offset = Quaternion.Inverse(carrierPose.rotation) * (body.position - carrierPose.position),
                    rotation = Quaternion.Inverse(carrierPose.rotation) * body.rotation,
                    kinematic = body.isKinematic, gravity = body.useGravity, collisions = body.detectCollisions,
                    constraints = body.constraints, interpolation = body.interpolation
                };
            }
            if (carrierColliders != null)
                foreach (var own in truck.GetComponentsInChildren<Collider>(true))
                foreach (var other in carrierColliders)
                    if (own != null && other != null && own != other && !other.transform.IsChildOf(truck.transform))
                    {
                        ignoredPairs.Add(new CollisionPair { truck = own, carrier = other, ignored = Physics.GetIgnoreCollision(own, other) });
                        Physics.IgnoreCollision(own, other, true);
                    }
            truck.Power(false); truck.command = Vector2.zero;
            foreach (var b in bodies)
            {
                if (!b.body.isKinematic) b.body.linearVelocity = b.body.angularVelocity = Vector3.zero;
                b.body.isKinematic = true;
            }
        }

        public void Follow(Pose carrierPose)
        {
            if (released) return;
            foreach (var b in bodies)
                if (b.body != null)
                {
                    b.body.MovePosition(carrierPose.position + carrierPose.rotation * b.offset);
                    b.body.MoveRotation(carrierPose.rotation * b.rotation);
                }
        }
        // Final-frame attachment of an already mounted, NWH-disabled, wholly
        // kinematic assembly. Detached/dynamic crews never enter this path.
        public bool TryFollowKinematicAttachment(Pose carrierPose)
        {
            if(released||truck==null||truck.Powered)return false;
            foreach(var b in bodies)if(b.body==null||!b.body.isKinematic)return false;
            foreach(var b in bodies)
            {
                Vector3 position=carrierPose.position+carrierPose.rotation*b.offset;Quaternion rotation=carrierPose.rotation*b.rotation;
                // Final-pose attachment supplies the rendered pose directly;
                // a second per-body interpolation would lag the real skin.
                // Release restores each body's captured original interpolation.
                b.body.interpolation=RigidbodyInterpolation.None;
                b.body.position=position;b.body.rotation=rotation;
                b.body.transform.SetPositionAndRotation(position,rotation);
            }
            return true;
        }
        public bool RemainsBound(Pose carrierPose, float maximumError = .5f)
        {
            if (released || truck == null || truck.body == null) return false;
            foreach (var b in bodies)
                if (b.body == null || !b.body.isKinematic
                    || Vector3.Distance(carrierPose.position + carrierPose.rotation * b.offset, b.body.position) > maximumError) return false;
            return true;
        }
        public void Release(Vector3 inheritedVelocity, Vector3 angularVelocity, bool reset = false)
            => ReleaseCore(inheritedVelocity, angularVelocity, reset, true);
        // Owner shutdown restores the assembly/collision state without waking
        // NWH components while Unity is disabling or unloading the scene.
        public void ReleaseForTeardown(Vector3 inheritedVelocity, Vector3 angularVelocity, bool reset = false)
            => ReleaseCore(inheritedVelocity, angularVelocity, reset, false);
        void ReleaseCore(Vector3 inheritedVelocity, Vector3 angularVelocity, bool reset, bool restorePower)
        {
            if (released) return;
            released = true;
            // Caller supplies actual carrier velocity, never a second launch
            // impulse. Bound values protect the NWH joints during handoff.
            inheritedVelocity = Vector3.ClampMagnitude(inheritedVelocity, 85);
            angularVelocity = Vector3.ClampMagnitude(angularVelocity, 3);
            foreach (var b in bodies)
            {
                if (b.body == null) continue;
                b.body.isKinematic = b.kinematic; b.body.useGravity = b.gravity;
                b.body.detectCollisions = b.collisions; b.body.constraints = b.constraints;
                b.body.interpolation = b.interpolation;
                if (!b.kinematic)
                {
                    b.body.linearVelocity = reset ? Vector3.zero : inheritedVelocity;
                    b.body.angularVelocity = reset ? Vector3.zero : angularVelocity;
                    b.body.WakeUp();
                }
            }
            foreach (var pair in ignoredPairs)
                if (pair.truck != null && pair.carrier != null) Physics.IgnoreCollision(pair.truck, pair.carrier, pair.ignored);
            ignoredPairs.Clear();
            if (restorePower && truck != null) truck.Power(powered);
        }
        public void Dispose() => Release(Vector3.zero, Vector3.zero, true);
    }
}
