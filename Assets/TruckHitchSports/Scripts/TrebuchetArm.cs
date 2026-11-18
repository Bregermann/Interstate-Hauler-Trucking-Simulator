using UnityEngine;
namespace LWS.TruckHitchSports
{
    public sealed class TrebuchetArm : MonoBehaviour
    {
        public Rigidbody Arm;
        public HingeJoint Pivot;
        public BoxCollider LoadingCradle;
        [Tooltip("The original physical approach/exit roads, fitted with kinematic hydraulic actuators by explicit scene authoring.")]
        public Rigidbody[] LoadingBridges = System.Array.Empty<Rigidbody>();
        [Tooltip("World-space mechanical travel, normally approach -forward6m and exit +forward6m.")]
        public Vector3[] LoadingBridgeRetractionOffsets = System.Array.Empty<Vector3>();
        public float BridgeRetractionMetres = 6, BridgeRetractionSpeed = 2;
        public bool PreparingToSwing { get; private set; }
        public bool LoadingBridgesReady { get; private set; }
        bool swingWhenBridgesClear;
        Vector3[] bridgeStarts;
        float preparedPower;
        public TrailerBody Load { get; private set; }
        public bool Swinging { get; private set; }
        public bool Released { get; private set; }
        ConfigurableJoint sling;
        struct ClearancePair { public Collider arm, load; }
        readonly System.Collections.Generic.List<ClearancePair> releaseClearance = new System.Collections.Generic.List<ClearancePair>(12);
        float releasedAt;
        public Vector3 ReleaseWorldVelocity { get; private set; }
        public Vector3 ReleaseWorldPosition { get; private set; }
        public int InitiallyPenetratingPairs { get; private set; }
        public int FilteredReleasePairs { get; private set; }
        public float DeepestReleasePenetration { get; private set; }
        public bool ClearanceTimedOut { get; private set; }
        public int ActiveClearancePairs => releaseClearance.Count;
        public string ReleasePairNames { get; private set; } = "";
        public bool SlingIntact => sling != null && Load != null && sling.connectedBody == Load.Body;
        // A negative gap marks an absent constraint, never a measured distance.
        public float SlingAnchorGapMetres => SlingIntact ? Vector3.Distance(Arm.transform.TransformPoint(sling.anchor), Load.Body.transform.TransformPoint(sling.connectedAnchor)) : -1;
        public Vector3 SlingConstraintForce => SlingIntact ? sling.currentForce : Vector3.zero;
        public Vector3 SlingConstraintTorque => SlingIntact ? sling.currentTorque : Vector3.zero;
        public string SlingDiagnostic() => SlingIntact ? "sling=cradle harness gap=" + SlingAnchorGapMetres.ToString("F4") + "m force=" + sling.currentForce.ToString("F1") + "N torque=" + sling.currentTorque.ToString("F1") + "Nm armAnchor=" + Arm.transform.TransformPoint(sling.anchor).ToString("F2") + " loadAnchor=" + Load.Body.transform.TransformPoint(sling.connectedAnchor).ToString("F2") + " angular=" + sling.angularXMotion + "/" + sling.angularYMotion + "/" + sling.angularZMotion : "sling=absent released=" + Released + " " + ReleaseDiagnostic();
        Vector3 start;
        Quaternion rotation;
        void Awake()
        {
            start = Arm.position; rotation = Arm.rotation;
            bridgeStarts = new Vector3[LoadingBridges.Length];
            for (int i = 0; i < LoadingBridges.Length; i++) bridgeStarts[i] = LoadingBridges[i].position;
            StopMechanism();
        }
        public bool CanLoad(TrailerBody trailer)
        {
            if (trailer == null || trailer.Body == null || LoadingCradle == null || Arm == null) return false;
            Vector3 point = trailer.Body.worldCenterOfMass;
            Bounds volume = LoadingCradle.bounds;
            if (!volume.Contains(point)) return false;
            Vector3 downstream = Vector3.ProjectOnPlane(Arm.transform.forward, Vector3.up);
            // First bounds entry is still behind the pivot and leaves the rear
            // of the trailer on the approach ramp. Haul it onto the actual
            // receiving center before closing the sling, so swing lifts it.
            return downstream.sqrMagnitude > .001f && Vector3.Dot(point - volume.center, downstream.normalized) >= -1;
        }
        public bool TractorClearOfLoadingBridges(Collider[] tractorShapes)
        {
            foreach (var bridge in LoadingBridges)
            {
                var bridgeShape = bridge.GetComponent<Collider>();
                if (bridgeShape == null) continue;
                Bounds receiving = bridgeShape.bounds; receiving.Expand(.5f);
                foreach (var shape in tractorShapes)
                    if (shape != null && shape.enabled && !shape.isTrigger && shape.gameObject.activeInHierarchy && receiving.Intersects(shape.bounds)) return false;
            }
            return true;
        }
        public string BridgeDiagnostic()
        {
            string state = "preparing=" + PreparingToSwing + " bridgesReady=" + LoadingBridgesReady + " bridges=" + LoadingBridges.Length;
            if (bridgeStarts != null) for (int i = 0; i < LoadingBridges.Length; i++) state += " deck" + i + "=" + Vector3.Distance(LoadingBridges[i].position, bridgeStarts[i]).ToString("F2") + "m";
            return state;
        }
        public bool TryLoad(TrailerBody trailer)
        {
            if (Load != null || trailer == null || trailer.Module.module.attached || !CanLoad(trailer) || trailer.Body.linearVelocity.magnitude > 2) return false;
            // The loading cradle/sling supports this load; native parking legs
            // lowered by OnDetach must not spear that same physical platform.
            if(trailer.Module.module.trailerStand!=null)trailer.Module.module.trailerStand.SetActive(false);
            Load = trailer; sling = Arm.gameObject.AddComponent<ConfigurableJoint>();
            // The cradle harness retains the physically loaded relative pose.
            // The real loading bridges retract before this assembly rotates;
            // an unconstrained pitch would rotate its solid deck through an
            // upright trailer. Destroying this joint releases genuine solver
            // momentum and permits free trailer tumbling.
            var point=trailer.Body.worldCenterOfMass;sling.autoConfigureConnectedAnchor=false;
            sling.anchor=Arm.transform.InverseTransformPoint(point);sling.connectedBody=trailer.Body;
            sling.connectedAnchor=trailer.Body.transform.InverseTransformPoint(point);
            sling.xMotion = sling.yMotion = sling.zMotion = ConfigurableJointMotion.Locked;
            sling.angularXMotion = sling.angularYMotion = sling.angularZMotion = ConfigurableJointMotion.Locked;
            // Let the actual solver carry the load; no corrective projection
            // or motor on the sling may manufacture its trajectory.
            sling.projectionMode = JointProjectionMode.None;
            sling.breakForce=sling.breakTorque=float.PositiveInfinity;
            trailer.Body.solverIterations=32;trailer.Body.solverVelocityIterations=12;
            sling.enableCollision = false; sling.enablePreprocessing = false; trailer.SetBrakes(0); return true;
        }
        public void Aim(float offsetDegrees)
        {
            if (Swinging || !Arm.isKinematic || Load != null && LoadingBridges.Length > 0 && !LoadingBridgesReady && Mathf.Abs(offsetDegrees) > .001f) return;
            // A position-driven loading winch moves the actual locked cradle.
            // The attached dynamic load responds through the real sling joint.
            Arm.MoveRotation(rotation * Quaternion.AngleAxis(Mathf.Clamp(offsetDegrees, -15, 10), Vector3.right));
        }
        public void PrepareLoadingBridgesForAim()
        {
            if (Load == null || Swinging || PreparingToSwing || LoadingBridgesReady) return;
            // Human setup ends with a safely retracted, still locked apparatus.
            // A separate assigned ACTION press must then begin the real swing.
            swingWhenBridgesClear = false;
            if (LoadingBridges.Length == 0) LoadingBridgesReady = true;
            else PreparingToSwing = true;
        }
        public void BeginSwing(float power)
        {
            if (Load == null || Swinging || PreparingToSwing) return;
            preparedPower = power;
            if (LoadingBridges.Length > 0 && !LoadingBridgesReady)
            { swingWhenBridgesClear = true; PreparingToSwing = true; return; }
            LoadingBridgesReady = true;
            UnlockSwing(power);
        }
        void UnlockSwing(float power)
        {
            PreparingToSwing = false;
            Swinging = true; Pivot.useSpring = false;
            Arm.isKinematic = false; Arm.useGravity = true; Arm.WakeUp();
            var motor = Pivot.motor; motor.force = Arm.mass * Mathf.Lerp(50, 120, Mathf.Clamp01(power)); motor.targetVelocity = -65; motor.freeSpin = true;
            Pivot.motor = motor; Pivot.useMotor = true;
        }
        public bool Release()
        {
            if (!Swinging || Released || sling == null) return false;
            ReleaseWorldVelocity = Load.Body.linearVelocity; ReleaseWorldPosition = Load.Body.worldCenterOfMass;
            PrepareReleaseClearance();
            Object.Destroy(sling); sling = null; Released = true; releasedAt = Time.fixedTime; return true;
        }
        void PrepareReleaseClearance()
        {
            RestoreReleaseClearance();
            InitiallyPenetratingPairs = FilteredReleasePairs = 0; DeepestReleasePenetration = 0; ClearanceTimedOut = false;
            var names = new System.Text.StringBuilder();
            var arms = Arm.GetComponentsInChildren<Collider>(true); var loads = Load.GetComponentsInChildren<Collider>(true);
            foreach (var armShape in arms)
            {
                if (!armShape.enabled || !armShape.gameObject.activeInHierarchy || armShape.isTrigger || armShape.attachedRigidbody != Arm) continue;
                foreach (var loadShape in loads)
                {
                    if (!loadShape.enabled || !loadShape.gameObject.activeInHierarchy || loadShape.isTrigger || loadShape.attachedRigidbody != Load.Body) continue;
                    if (!Penetrates(armShape, loadShape, out float depth) || depth <= .001f) continue;
                    InitiallyPenetratingPairs++; DeepestReleasePenetration = Mathf.Max(DeepestReleasePenetration, depth);
                    if (InitiallyPenetratingPairs <= 4) names.Append(armShape.name).Append(" -> ").Append(loadShape.name).Append(" depth=").Append(depth.ToString("F3")).Append("; ");
                    // Preserve any independent pre-existing pair filter. Only
                    // this release's new filters are subsequently restored.
                    if (Physics.GetIgnoreCollision(armShape, loadShape)) continue;
                    Physics.IgnoreCollision(armShape, loadShape, true);
                    releaseClearance.Add(new ClearancePair { arm = armShape, load = loadShape }); FilteredReleasePairs++;
                }
            }
            ReleasePairNames = names.ToString();
        }
        static bool Penetrates(Collider a, Collider b, out float depth)
        {
            Pose(a, out var ap, out var ar); Pose(b, out var bp, out var br);
            return Physics.ComputePenetration(a, ap, ar, b, bp, br, out _, out depth);
        }
        static void Pose(Collider shape, out Vector3 position, out Quaternion rotation)
        {
            var body = shape.attachedRigidbody;
            if (body == null) { position = shape.transform.position; rotation = shape.transform.rotation; return; }
            // Collider local frames stay authored; use current physics poses,
            // rather than the previous interpolated rendering frame.
            position = body.position + body.rotation * body.transform.InverseTransformPoint(shape.transform.position);
            rotation = body.rotation * Quaternion.Inverse(body.transform.rotation) * shape.transform.rotation;
        }
        void FixedUpdate()
        {
            if (PreparingToSwing)
            {
                bool clear = true;
                for (int i = 0; i < LoadingBridges.Length; i++)
                {
                    var bridge = LoadingBridges[i];
                    Vector3 travel = i < LoadingBridgeRetractionOffsets.Length ? LoadingBridgeRetractionOffsets[i] : (i == 0 ? Vector3.back : Vector3.forward) * BridgeRetractionMetres;
                    Vector3 target = bridgeStarts[i] + travel;
                    clear &= Vector3.Distance(bridge.position, target) < .01f;
                    // These are the real visible deck bodies/colliders, moved
                    // axially by their kinematic hydraulic mechanism. The load remains
                    // dynamic on its supported sling; no actor is repositioned.
                    bridge.MovePosition(Vector3.MoveTowards(bridge.position, target, BridgeRetractionSpeed * Time.fixedDeltaTime));
                }
                if (clear)
                {
                    LoadingBridgesReady = true; PreparingToSwing = false;
                    if (swingWhenBridgesClear) UnlockSwing(preparedPower);
                }
            }
            for (int i = releaseClearance.Count - 1; i >= 0; i--)
            {
                var pair = releaseClearance[i];
                bool valid = pair.arm != null && pair.load != null && pair.arm.enabled && pair.load.enabled && pair.arm.gameObject.activeInHierarchy && pair.load.gameObject.activeInHierarchy;
                bool penetrating = valid && Penetrates(pair.arm, pair.load, out float depth) && depth > .001f;
                bool timeout = Time.fixedTime - releasedAt >= 1;
                if (penetrating && !timeout) continue;
                if (penetrating && timeout) ClearanceTimedOut = true;
                if (valid) Physics.IgnoreCollision(pair.arm, pair.load, false);
                releaseClearance.RemoveAt(i);
            }
        }
        void RestoreReleaseClearance()
        {
            foreach (var pair in releaseClearance) if (pair.arm != null && pair.load != null && pair.arm.gameObject.activeInHierarchy && pair.load.gameObject.activeInHierarchy) Physics.IgnoreCollision(pair.arm, pair.load, false);
            releaseClearance.Clear();
        }
        public string ReleaseDiagnostic() => "releaseCOM=" + ReleaseWorldPosition.ToString("F2") + " velocity=" + ReleaseWorldVelocity.ToString("F2") + " initialPenetrations=" + InitiallyPenetratingPairs + " filtered=" + FilteredReleasePairs + " activeFilters=" + ActiveClearancePairs + " maxDepth=" + DeepestReleasePenetration.ToString("F3") + " timeout=" + ClearanceTimedOut + " pairs=" + ReleasePairNames;
        void OnDisable() => RestoreReleaseClearance();
        public void StopMechanism()
        {
            PreparingToSwing = false; swingWhenBridgesClear = false;
            RestoreReleaseClearance();
            Pivot.useMotor = Pivot.useSpring = Pivot.useLimits = false; Pivot.enablePreprocessing = false;
            if (!Arm.isKinematic) Arm.linearVelocity = Arm.angularVelocity = Vector3.zero;
            Arm.isKinematic = true;
        }
        public void ResetForRound()
        {
            RestoreReleaseClearance();
            LoadingBridgesReady = false; swingWhenBridgesClear = false;
            if (sling != null) Object.Destroy(sling); sling = null; Load = null; Swinging = Released = false;
            ReleaseWorldVelocity = ReleaseWorldPosition = Vector3.zero; InitiallyPenetratingPairs = FilteredReleasePairs = 0; DeepestReleasePenetration = 0; ClearanceTimedOut = false; ReleasePairNames = "";
            StopMechanism();
            if (bridgeStarts != null)
                for (int i = 0; i < LoadingBridges.Length; i++)
                {
                    LoadingBridges[i].position = bridgeStarts[i];
                    LoadingBridges[i].transform.position = bridgeStarts[i];
                }
            Arm.interpolation = RigidbodyInterpolation.None;
            Arm.position = start; Arm.rotation = rotation;
            Arm.transform.SetPositionAndRotation(start, rotation);
        }
    }
}
