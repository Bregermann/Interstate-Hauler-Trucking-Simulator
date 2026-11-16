using System;
using LWS.TruckJenga;
using LWS.TruckSports;
using UnityEngine;

namespace LWS.TruckDragonEggDelivery
{
    [Serializable]
    public sealed class DragonEggNest
    {
        public string id;
        public Transform center, approach;
        public BoxCollider zone;
        public Collider[] supports = Array.Empty<Collider>();
        [NonSerialized] RaycastHit[] hits;
        public bool ContainsAndSupports(DragonEggCargo cargo)
        {
            if (cargo == null || !cargo.PhysicallyPresent || center == null || zone == null) return false;
            var bounds = cargo.ActiveShape.bounds;
            if (!zone.bounds.Contains(bounds.min) || !zone.bounds.Contains(bounds.max)) return false;
            if (hits == null) hits = new RaycastHit[24];
            int found = Physics.RaycastNonAlloc(bounds.center + Vector3.up * .1f, Vector3.down, hits, bounds.extents.y + .55f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < found; i++)
                if (hits[i].collider != null && hits[i].normal.y > .6f)
                    foreach (var support in supports) if (support != null && hits[i].collider == support) return true;
            return false;
        }
    }

    [Serializable]
    public sealed class DragonEggCarrier
    {
        public const float MaximumHoistReach = 26;
        public SportsEventTruck truck;
        public Transform equipment, socket, boom;
        public JengaCrane crane;
        public LineRenderer cable;
        public DragonEggCargo cargo;
        public bool Busy => job != 0;
        public bool Unloading => job == 2;
        public bool Released => Busy && crane.Released;
        int job; float obstacleTop;
        public float AppliedHoistSpeedLimit {get;private set;}
        const float GripBreakForce=12000,GripBreakTorque=5000;
        public void ResetCarrier()
        {
            crane.StopCrane(); crane.ResetCrane(); job = 0;AppliedHoistSpeedLimit=0;
            crane.spreader.position = socket.position + Vector3.up * 4;
            crane.spreader.rotation = Quaternion.identity;
        }
        public bool BeginJob(bool unloading, DragonEggNest nest)
        {
            if (Busy || cargo == null || !cargo.PhysicallyPresent || cargo.body.isKinematic || truck.body.linearVelocity.magnitude > .8f ||
                Vector3.Distance(cargo.body.position, truck.body.position) > MaximumHoistReach || crane.Stage != JengaCrane.MotorStage.Idle) return false;
            if (unloading && (nest == null || Vector3.Distance(truck.body.position, nest.approach.position) > 11)) return false;
            Vector3 destination = unloading ? nest.center.position : socket.position;
            if(Vector3.Distance(destination,truck.body.position)>MaximumHoistReach) return false;
            if (!crane.Begin(cargo.body, destination, unloading ? Quaternion.identity : truck.body.rotation, cargo.GripHeight)) return false;
            // Keep the real cradle restraint until the existing crane has
            // physically reached and gripped this load. Releasing at Begin left
            // the ungripped load loose throughout Approach/LowerPickup.
            obstacleTop = Mathf.Max(truck.body.position.y + 6, destination.y + cargo.height);
            job = unloading ? 2 : 1; return true;
        }
        public void Tick(float dt)
        {
            if (Busy)
            {
                // Existing Jenga exposes speed, not acceleration. V153 showed
                // its10m/s Traverse step breaking this25kg load's existing
                // finite grip at the actual.85m anchor. Limit only the public
                // speed configuration for this Step, leaving Jenga's complete
                // path/stage/MovePosition authority intact. A worst-case full
                // velocity reversal consumes at most half the smaller linear
                // or anchor-torque force capacity, reserving the other half for
                // gravity, turning, contacts and solver error. No force gate is
                // increased, no competing motion/velocity is applied.
                float lever=Mathf.Max(.1f,cargo.GripHeight*.5f),capacity=Mathf.Min(GripBreakForce,GripBreakTorque/lever);
                float availableAcceleration=Mathf.Max(0,capacity/Mathf.Max(.001f,cargo.body.mass)-Physics.gravity.magnitude);
                AppliedHoistSpeedLimit=availableAcceleration*dt*.25f;
                float travel=crane.travelSpeed,lift=crane.liftSpeed;
                try {crane.travelSpeed=Mathf.Min(travel,AppliedHoistSpeedLimit);crane.liftSpeed=Mathf.Min(lift,AppliedHoistSpeedLimit);crane.Step(dt,obstacleTop);}
                finally {crane.travelSpeed=travel;crane.liftSpeed=lift;}
                var grip=cargo.GetComponent<FixedJoint>();
                if(grip!=null&&grip.connectedBody==crane.spreader)
                {
                    // Both authorities hand over in this same pre-solve step;
                    // no teleport or synthetic crane stage completes the grip.
                    if(cargo.Restraint!=null)cargo.ReleaseRestraint();
                    grip.breakForce=GripBreakForce;grip.breakTorque=GripBreakTorque;
                }
                if(crane.Stage>=JengaCrane.MotorStage.Lift&&crane.Stage<=JengaCrane.MotorStage.LowerStack&&grip==null)
                { crane.StopCrane();job=0; }
                // The reused tower crane's AtRest requires world-vertical blocks.
                // A real truck cradle can be pitched on a graded road: prove the
                // same achieved quiet pose relative to this original NWH body.
                bool seated=job==1&&crane.Released&&grip==null&&Vector3.Distance(cargo.body.position,socket.position)<.5f&&
                    Quaternion.Angle(cargo.body.rotation,truck.body.rotation)<8&&
                    (cargo.body.linearVelocity-truck.body.GetPointVelocity(cargo.body.position)).magnitude<.25f&&
                    (cargo.body.angularVelocity-truck.body.angularVelocity).magnitude<.2f;
                if(seated&&cargo.AttachRestraint(this)){crane.Complete();job=0;}
            }
            if (cable != null) { cable.SetPosition(0, truck.transform.TransformPoint(new Vector3(0, 5, -1))); cable.SetPosition(1, crane.spreader.position); }
            if (boom != null)
            {
                Vector3 a = truck.transform.TransformPoint(new Vector3(0, 3, -1)), b = crane.spreader.position;
                boom.position = (a + b) * .5f; boom.rotation = Quaternion.FromToRotation(Vector3.up, b - a);
                boom.localScale = new Vector3(.14f, Vector3.Distance(a, b) * .5f, .14f);
            }
        }
        public void FinishDelivery() { crane.Complete(); job = 0; }
        public void Stop() { crane.StopCrane(); job = 0; cargo?.ReleaseRestraint(); }
    }

    // Same dynamic cargo identity/body survives cracks, hatch, recovery and
    // delivery. The baby is actual Generic rig animation over that physical body.
    public sealed class DragonEggCargo : MonoBehaviour
    {
        public DragonEggDeliveryMatch match;
        public int slot;
        public Rigidbody body;
        public Collider eggShape, babyShape;
        public Renderer[] shellRenderers = Array.Empty<Renderer>();
        public LineRenderer[] cracks = Array.Empty<LineRenderer>();
        public GameObject baby;
        public Animator babyAnimator;
        public Light hatchLight;
        public Rigidbody[] shellChips = Array.Empty<Rigidbody>();
        public float height = 1.7f;
        public ConfigurableJoint Restraint { get; private set; }
        public int GenuineImpacts { get; private set; }
        public int Hatches { get; private set; }
        public Collider ActiveShape => babyShape != null && babyShape.enabled ? babyShape : eggShape;
        public float GripHeight => ActiveShape is CapsuleCollider capsule ? capsule.height : height;
        public bool PhysicallyPresent => body != null && gameObject.activeInHierarchy && body.detectCollisions && ActiveShape != null && ActiveShape.enabled;
        public bool IsBabyVisible => baby != null && baby.activeInHierarchy && babyAnimator != null && babyAnimator.enabled;
        public bool Settled => PhysicallyPresent && Restraint == null && GetComponent<FixedJoint>() == null &&
            body.linearVelocity.magnitude < .35f && body.angularVelocity.magnitude < .4f && Vector3.Dot(transform.up, Vector3.up) > .7f;
        float crackAge, settleAge, chipLife;
        bool sawActualHatch;
        Vector3 previousVelocity;
        DragonEggStage previousStage;
        public float HandlingAcceleration { get; private set; }

        public void ResetCargo(DragonEggCarrier carrier, bool active)
        {
            ReleaseRestraint(); gameObject.SetActive(active); if (!active) return;
            body.isKinematic = false; body.detectCollisions = true;
            body.linearVelocity = body.angularVelocity = Vector3.zero;
            body.position = carrier.socket.position; body.rotation = carrier.truck.body.rotation;
            transform.SetPositionAndRotation(body.position, body.rotation);
            eggShape.enabled = true; babyShape.enabled = false; baby.SetActive(false);
            foreach (var renderer in shellRenderers) renderer.enabled = true;
            foreach (var crack in cracks) crack.enabled = false;
            foreach (var chip in shellChips) { chip.isKinematic = true; chip.detectCollisions = false; chip.gameObject.SetActive(false); }
            if (hatchLight != null) hatchLight.enabled = false;
            crackAge = settleAge = chipLife = 0; sawActualHatch = false; previousVelocity = Vector3.zero; previousStage = DragonEggStage.Egg;
            GenuineImpacts = Hatches = 0; AttachRestraint(carrier);
        }
        public bool AttachRestraint(DragonEggCarrier carrier)
        {
            if (Restraint != null || carrier == null || !PhysicallyPresent || Vector3.Distance(body.position, carrier.socket.position) > .7f) return false;
            Restraint = gameObject.AddComponent<ConfigurableJoint>(); Restraint.connectedBody = carrier.truck.body;
            Restraint.autoConfigureConnectedAnchor = false; Restraint.anchor = Vector3.zero;
            // Capture the achieved actual pose; neither recovery nor care snaps
            // cargo into an artificial inventory or changes its world position.
            Restraint.connectedAnchor = carrier.truck.transform.InverseTransformPoint(body.position);
            Restraint.xMotion = Restraint.yMotion = Restraint.zMotion = ConfigurableJointMotion.Limited;
            Restraint.angularXMotion = Restraint.angularYMotion = Restraint.angularZMotion = ConfigurableJointMotion.Limited;
            Restraint.linearLimit = new SoftJointLimit { limit = .42f };
            Restraint.lowAngularXLimit = new SoftJointLimit { limit = -28 }; Restraint.highAngularXLimit = new SoftJointLimit { limit = 28 };
            Restraint.angularYLimit = Restraint.angularZLimit = new SoftJointLimit { limit = 28 };
            Restraint.breakForce = 9500; Restraint.breakTorque = 4000; Restraint.enableCollision = true;
            Restraint.projectionMode = JointProjectionMode.None; Restraint.rotationDriveMode = RotationDriveMode.Slerp;
            SetStabilizing(false, .02f); return true;
        }
        public void SetStabilizing(bool held, float dt)
        {
            if (Restraint == null) return;
            var limit = Restraint.linearLimit; limit.limit = Mathf.MoveTowards(limit.limit, held ? .25f : .42f, dt * .25f); Restraint.linearLimit = limit;
            var drive = new JointDrive { positionSpring = held ? 1500 : 800, positionDamper = held ? 220 : 90, maximumForce = 1800 };
            Restraint.xDrive = Restraint.yDrive = Restraint.zDrive = drive;
            Restraint.slerpDrive = new JointDrive { positionSpring = held ? 160 : 70, positionDamper = held ? 45 : 18, maximumForce = 450 };
        }
        public void ReleaseRestraint()
        {
            if (Restraint == null) return;
            var released=Restraint;Restraint=null;
            // Destroy is deferred until the rendered frame ends. V149 captured
            // a real intervening PhysX step with the old drives still active and
            // truck-local connectedAnchor interpreted as a world point1307m away.
            // Make the pending joint inert before disconnecting it; preserve its
            // actual world anchor without changing either rigidbody's pose.
            released.xDrive=released.yDrive=released.zDrive=default;
            released.angularXDrive=released.angularYZDrive=released.slerpDrive=default;
            released.linearLimitSpring=released.angularXLimitSpring=released.angularYZLimitSpring=default;
            released.xMotion=released.yMotion=released.zMotion=ConfigurableJointMotion.Free;
            released.angularXMotion=released.angularYMotion=released.angularZMotion=ConfigurableJointMotion.Free;
            released.projectionMode=JointProjectionMode.None;
            Vector3 worldAnchor=body.transform.TransformPoint(released.anchor);
            released.autoConfigureConnectedAnchor=false;released.connectedBody=null;released.connectedAnchor=worldAnchor;
            Destroy(released);
        }
        public void Tick(float dt, bool caring)
        {
            if (match.Rules == null || slot >= match.Rules.Count) return;
            if (chipLife > 0)
            {
                chipLife -= dt;
                if (chipLife <= 0) foreach (var chip in shellChips) { chip.isKinematic = true; chip.gameObject.SetActive(false); }
            }
            var record = match.Rules.Cargo[slot];
            HandlingAcceleration = (body.linearVelocity - previousVelocity).magnitude / Mathf.Max(.001f, dt); previousVelocity = body.linearVelocity;
            match.Rules.ObserveGentleHandling(slot, dt, HandlingAcceleration < 5 && body.angularVelocity.magnitude < 1);
            SetStabilizing(caring, dt);
            foreach (var crack in cracks) crack.enabled = record.Condition < .8 || record.Stage == DragonEggStage.Cracking;
            if (record.Stage == DragonEggStage.Egg && match.Rules.BeginCracking(slot)) crackAge = 0;
            if (record.Stage == DragonEggStage.Cracking)
            {
                crackAge += dt; body.AddRelativeTorque(new Vector3(Mathf.Sin(crackAge * 17), 0, Mathf.Cos(crackAge * 13)) * .45f, ForceMode.Acceleration);
                if (hatchLight != null) { hatchLight.enabled = true; hatchLight.intensity = 1.5f + Mathf.Sin(crackAge * 8); }
                if (crackAge >= .9f && match.Rules.BeginHatching(slot)) OpenShell();
            }
            if (record.Stage == DragonEggStage.Hatching && babyAnimator != null && IsBabyVisible)
            {
                var state = babyAnimator.GetCurrentAnimatorStateInfo(0);
                if (state.IsName("Hatch")) sawActualHatch = true;
                bool actualHatch = sawActualHatch && (state.IsName("Hatch") && state.normalizedTime >= .9f || state.IsName("Care") && state.normalizedTime > 0);
                if (match.Rules.CompleteHatch(slot, true, actualHatch)) { Hatches++; if (hatchLight != null) hatchLight.enabled = false; }
            }
            if (record.Stage == DragonEggStage.Baby && babyAnimator != null)
            {
                babyAnimator.SetFloat("Activity", caring ? 0 : Mathf.Clamp01(body.angularVelocity.magnitude * .5f + .25f));
                if (!caring) body.AddRelativeTorque(new Vector3(Mathf.Sin(match.Clock * 2), 0, Mathf.Cos(match.Clock * 1.7f)) * .12f, ForceMode.Acceleration);
            }
            if (record.Stage == DragonEggStage.Lost && previousStage != DragonEggStage.Lost) { ReleaseRestraint(); EmitChips(); }
            previousStage = record.Stage;
            if (body.position.y < -40 && !record.Terminal) match.Rules.Lose(slot);
        }
        void OpenShell()
        {
            foreach (var renderer in shellRenderers) renderer.enabled = false;
            foreach (var crack in cracks) crack.enabled = false;
            eggShape.enabled = false; babyShape.enabled = true;
            sawActualHatch = false; baby.SetActive(true); babyAnimator.Rebind(); babyAnimator.Play("Hatch", 0, 0);
            EmitChips(); match.feedback.Play(4, 0, true);
        }
        void EmitChips()
        {
            chipLife = 6;
            for (int i = 0; i < shellChips.Length; i++)
            {
                var chip = shellChips[i]; float angle = i * Mathf.PI * 2 / Mathf.Max(1, shellChips.Length);
                var radial = new Vector3(Mathf.Cos(angle), .3f, Mathf.Sin(angle));
                chip.gameObject.SetActive(true); chip.isKinematic = false; chip.detectCollisions = true;
                chip.position = body.position + radial * .7f; chip.rotation = body.rotation * Quaternion.Euler(i * 31, i * 47, i * 13);
                chip.linearVelocity = body.linearVelocity + radial * 1.5f; chip.angularVelocity = radial * 2;
            }
        }
        public bool ObserveDelivery(DragonEggNest nest, float dt)
        {
            bool valid = nest.ContainsAndSupports(this) && Settled;
            settleAge = valid ? settleAge + dt : 0; return settleAge >= 1.25f;
        }
        public void SecureDelivered()
        { ReleaseRestraint(); body.linearVelocity = body.angularVelocity = Vector3.zero; body.isKinematic = true; if (babyAnimator != null) babyAnimator.SetFloat("Activity", 0); }
        void OnCollisionEnter(Collision collision)
        {
            if (match == null || !match.IsDriving(slot) || match.Rules == null || body.isKinematic) return;
            double delta = Math.Max(collision.relativeVelocity.magnitude, collision.impulse.magnitude / Mathf.Max(1, body.mass));
            if (match.Rules.Impact(slot, delta)) { GenuineImpacts++; match.CargoDisturbed(slot); }
        }
        public void StopCargo()
        {
            ReleaseRestraint(); if (!body.isKinematic) body.linearVelocity = body.angularVelocity = Vector3.zero;
            body.isKinematic = true; body.detectCollisions = false;
            foreach (var chip in shellChips) if (chip != null) { chip.isKinematic = true; chip.gameObject.SetActive(false); }
            if (hatchLight != null) hatchLight.enabled = false;
        }
    }
}
