using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using LWS.TruckSports;
using NWH.WheelController3D;

namespace LWS.TruckMoonMadness
{
    // Mode abilities act on the original NWH assembly; no alternate drivetrain.
    public sealed class MoonTruck : MonoBehaviour
    {
        public MoonMatch match; public SportsEventTruck truck;
        public ParticleSystem[] jets;
        public float Fuel { get; private set; } = 1;
        public int Hops { get; private set; }
        public float ThrustSeconds { get; private set; }
        public bool Applied { get; private set; }
        Rigidbody[] assembly; float[] spring, bump, rebound, longitudinal, lateral;
        float grip, nextHop;
        NWH.VehiclePhysics2.Modules.Aerodynamics.AerodynamicsModuleWrapper aero;
        NWH.VehiclePhysics2.Modules.ArcadeModule.ArcadeModuleWrapper arcade;
        bool originalDrag,originalDownforce;float originalSteerAssist,originalDriftForce;
        sealed class FlagGroundExclusion
        {
            public HashSet<Collider> originalSet;
            public readonly List<Collider> added=new List<Collider>(5);
            public void Restore(){foreach(var shape in added)originalSet.Remove(shape);added.Clear();}
        }
        readonly Dictionary<WheelController,FlagGroundExclusion> flagGroundExclusions=new Dictionary<WheelController,FlagGroundExclusion>();
        public Rigidbody[] Assembly => assembly;
        public void Apply()
        {
            if (Applied) return;
            assembly = truck.GetComponentsInChildren<Rigidbody>(true);
            spring = truck.wheels.Select(w => w.spring.maxForce).ToArray();
            bump = truck.wheels.Select(w => w.damper.bumpRate).ToArray();
            rebound = truck.wheels.Select(w => w.damper.reboundRate).ToArray();
            longitudinal = truck.wheels.Select(w => w.LongitudinalFrictionGrip).ToArray();
            lateral = truck.wheels.Select(w => w.LateralFrictionGrip).ToArray();
            grip = truck.tractorMotor.gripScale;
            aero=truck.GetComponent<NWH.VehiclePhysics2.Modules.Aerodynamics.AerodynamicsModuleWrapper>();
            if(aero!=null){originalDrag=aero.module.simulateDrag;originalDownforce=aero.module.simulateDownforce;aero.module.simulateDrag=false;aero.module.simulateDownforce=false;}
            arcade=truck.GetComponent<NWH.VehiclePhysics2.Modules.ArcadeModule.ArcadeModuleWrapper>();
            if(arcade!=null){originalSteerAssist=arcade.module.artificialSteerStrength;originalDriftForce=arcade.module.maxDriftAssistForce;arcade.module.artificialSteerStrength=originalSteerAssist/6;arcade.module.maxDriftAssistForce=originalDriftForce/6;}
            for (int i = 0; i < spring.Length; i++)
            {
                truck.wheels[i].spring.maxForce = spring[i] / 6;
                truck.wheels[i].damper.bumpRate = bump[i] / Mathf.Sqrt(6);
                truck.wheels[i].damper.reboundRate = rebound[i] / Mathf.Sqrt(6);
            }
            truck.tractorMotor.gripScale = grip * 1.8f; Applied = true; ExcludeFlagGround();
        }
        public void ResetFlight() { Fuel = 1; Hops = 0; ThrustSeconds = 0; nextHop = 0; StopJets(); }
        public void Restore()
        {
            StopJets();RestoreFlagGround(); if (!Applied) return; Applied = false;
            truck.tractorMotor.gripScale = grip;
            if(aero!=null){aero.module.simulateDrag=originalDrag;aero.module.simulateDownforce=originalDownforce;}
            if(arcade!=null){arcade.module.artificialSteerStrength=originalSteerAssist;arcade.module.maxDriftAssistForce=originalDriftForce;}
            for (int i = 0; i < spring.Length; i++)
            {
                var w = truck.wheels[i]; w.spring.maxForce = spring[i]; w.damper.bumpRate = bump[i]; w.damper.reboundRate = rebound[i];
                w.LongitudinalFrictionGrip = longitudinal[i]; w.LateralFrictionGrip = lateral[i];
            }
        }
        public void GravityStep()
        {
            if (!Applied) return;
            ExcludeFlagGround();
            foreach (var body in assembly) if (!body.isKinematic && body.useGravity) body.AddForce(Physics.gravity * (1f / 6 - 1), ForceMode.Acceleration);
        }
        void ExcludeFlagGround()
        {
            // NWH's public cast-filter set is explicitly extensible. Flags are
            // native collision cargo, never suspension-supporting terrain. Pair
            // IgnoreCollision does not filter the vendor's wheel ground casts.
            foreach(var wheel in truck.wheels)
            {
                var current=wheel.vehicleColliders;
                if(current==null)continue; // NWH initializes this in Start.
                if(flagGroundExclusions.TryGetValue(wheel,out var existing)&&ReferenceEquals(existing.originalSet,current))continue;
                existing?.Restore();
                var exclusion=new FlagGroundExclusion{originalSet=current};
                foreach(var flag in match.flags)if(flag!=null&&flag.shape!=null&&current.Add(flag.shape))exclusion.added.Add(flag.shape);
                flagGroundExclusions[wheel]=exclusion;
            }
        }
        void RestoreFlagGround()
        {
            foreach(var exclusion in flagGroundExclusions.Values)exclusion.Restore();
            flagGroundExclusions.Clear();
        }
        public void Step(Vector2 input, bool fire, bool hop, float dt, float now)
        {
            if (!Applied) return;

            bool grounded = truck.tractorMotor.Grounded;
            if (grounded)
            {
                Fuel = Mathf.Min(1, Fuel + dt * .22f);
                if (hop && now >= nextHop && Fuel >= .25f)
                {
                    float impulse = Mathf.Sqrt(2 * MoonMatch.LunarGravity * 3.5f);
                    truck.tractorMotor.AddAssemblyVelocity(Vector3.up * Mathf.Clamp(impulse - truck.body.linearVelocity.y, 0, impulse));
                    Fuel -= .25f; nextHop = now + 1.2f; Hops++;
                }
                StopJets(); return;
            }
            if (!fire || Fuel <= 0) { StopJets(); return; }
            float consume = Mathf.Min(Fuel, dt * .24f), fraction = consume / (dt * .24f);
            Fuel -= consume; ThrustSeconds += dt * fraction;
            Vector3 torque = truck.transform.right * (input.y * .8f) - truck.transform.forward * (input.x * .6f) + Vector3.up * (input.x * .45f);
            torque += Vector3.Cross(truck.transform.up, Vector3.up) * .75f - truck.body.angularVelocity * .4f;
            truck.body.AddTorque(Vector3.ClampMagnitude(torque, 1.8f) * fraction, ForceMode.Acceleration);
            // Less than lunar weight: correction can soften a landing, never hover.
            if (truck.body.linearVelocity.y < 0)
                foreach (var body in assembly) if (!body.isKinematic) body.AddForce(Vector3.up * MoonMatch.LunarGravity * .55f * fraction, ForceMode.Acceleration);
            foreach (var jet in jets) if (!jet.isPlaying) jet.Play(true);
        }
        public void StopJets() { if (jets != null) foreach (var jet in jets) if (jet != null) jet.Stop(true, ParticleSystemStopBehavior.StopEmitting); }
        void OnCollisionEnter(Collision collision)
        {
            ObserveCollision(collision,truck.body);
        }
        public void ObserveCollision(Collision collision,Rigidbody contactBody)
        {
            if (match != null && match.Live && !collision.collider.transform.IsChildOf(truck.transform) && collision.impulse.magnitude / Mathf.Max(1,contactBody.mass) > 2.4f) match.Disrupt(truck.slot);
        }
        void OnDisable() { if (truck != null) Restore(); }
    }
}
