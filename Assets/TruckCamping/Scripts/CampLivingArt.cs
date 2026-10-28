using System;
using UnityEngine;
namespace LWS.TruckCamping
{
    public enum CampPose { Idle, Walk, Look, Sniff, Sit, LieDown, RollOver, Shake, Dig, Stay, Come, Pee, GrassKick, Sleep, BathShake, Happy }
    public enum CampBone { Pelvis, Spine, Head, FrontLeft, FrontLeftLower, FrontRight, FrontRightLower, RearLeft, RearLeftLower, RearRight, RearRightLower, Tail, LeftEar, RightEar }
    // Explicit author-time bones on genuine skins or finished original rigs.
    // Animator owns locomotion; while a missing contextual routine is active,
    // it is disabled and this one writer owns the recorded bind-pose offsets.
    public sealed class CampLivingArt : MonoBehaviour
    {
        public Animator animator;
        public Transform model;
        public Transform[] bones = Array.Empty<Transform>();
        public CampBone[] roles = Array.Empty<CampBone>();
        public bool biped;
        public string idleState = "Idle", walkState = "Walk";
        public float groundSize = 1;
        public bool previewUnscaled;
        public float LocomotionSpeed { get; set; }
        public CampPose Pose { get; private set; }
        public int AnimatedFrames { get; private set; }
        public float ActualLegLift { get; private set; }
        public float ActualKickAngle { get; private set; }
        public float ActualContextAngle { get; private set; }
        Quaternion[] rest;
        Vector3[] positions;
        Quaternion modelRest;
        Vector3 modelPosition;
        float phase, strength = 1, bodyDrop, gaitPhase;
        string state;
        void Awake() { Initialize(); }
        void Initialize()
        {
            if (rest != null) return;
            if (bones.Length != roles.Length || model == null) throw new InvalidOperationException("Explicit living-art bone roles and model are required.");
            rest = new Quaternion[bones.Length]; positions = new Vector3[bones.Length];
            for (int i = 0; i < bones.Length; i++) { rest[i] = bones[i].localRotation; positions[i] = bones[i].localPosition; }
            modelRest = model.localRotation; modelPosition = model.localPosition;
            if (animator != null) { animator.applyRootMotion = false; animator.fireEvents = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate; }
        }
        public void Request(CampPose pose, float progress = 0, float intensity = 1)
        {
            Initialize(); Pose = pose; phase = progress; strength = Mathf.Clamp01(intensity);
            bool vendor = animator != null && animator.runtimeAnimatorController != null && (pose == CampPose.Walk || pose == CampPose.Come || pose == CampPose.Idle || pose == CampPose.Look);
            if (animator != null)
            {
                if (vendor)
                {
                    string desired = pose == CampPose.Walk || pose == CampPose.Come ? walkState : idleState;
                    if (!animator.enabled) { ResetBones(); animator.enabled = true; state = null; }
                    if (state != desired) { animator.CrossFadeInFixedTime(desired, .2f); state = desired; }
                    animator.speed=desired==walkState?Mathf.Clamp(LocomotionSpeed/Mathf.Max(.5f,groundSize*1.7f),.2f,6):1;
                }
                else animator.enabled = false;
            }
        }
        void ResetBones() { for (int i = 0; i < bones.Length; i++) { bones[i].localRotation = rest[i]; bones[i].localPosition = positions[i]; } model.localRotation = modelRest; model.localPosition = modelPosition; }
        static float Cycle(float t) => Mathf.Sin(t * Mathf.PI * 2);
        public static Vector3 Rotation(CampPose pose, CampBone bone, float t, bool adult)
        {
            bool left = bone == CampBone.FrontLeft || bone == CampBone.FrontLeftLower || bone == CampBone.RearLeft || bone == CampBone.RearLeftLower;
            bool front = bone == CampBone.FrontLeft || bone == CampBone.FrontRight;
            bool rear = bone == CampBone.RearLeft || bone == CampBone.RearRight;
            bool lower = bone == CampBone.FrontLeftLower || bone == CampBone.FrontRightLower || bone == CampBone.RearLeftLower || bone == CampBone.RearRightLower;
            float side = left ? 1 : -1;
            if (pose == CampPose.Walk || pose == CampPose.Come)
            {
                float gait = Cycle(t + (left ? 0 : .5f) + (rear ? .5f : 0));
                if (front || rear) return new Vector3(gait * (adult ? 23 : 28), 0, adult && front ? side * 65 : 0);
                if (lower) return new Vector3(Mathf.Max(0, -gait) * 25, 0, 0);
            }
            if (pose == CampPose.Pee)
            {
                if (bone == CampBone.RearLeft) return new Vector3(-20, -8, 65);
                if (bone == CampBone.RearLeftLower) return new Vector3(70, 0, 0);
                if (bone == CampBone.Head) return new Vector3(-10, 30, 0);
                if (adult && bone == CampBone.Spine) return new Vector3(46, 0, -8);
            }
            if (pose == CampPose.GrassKick)
            {
                if (bone == CampBone.RearLeft || bone == CampBone.RearRight) return new Vector3(-15 - Mathf.Max(0, Cycle(t * 3 + (left ? 0 : .5f))) * 55, 0, 0);
                if (lower) return new Vector3(20, 0, 0);
            }
            if (pose == CampPose.Sit)
            {
                if (rear) return new Vector3(-65, 0, side * 8);
                if (bone == CampBone.RearLeftLower || bone == CampBone.RearRightLower) return new Vector3(100, 0, 0);
                if (bone == CampBone.Spine) return new Vector3(adult ? 0 : -12, 0, 0);
            }
            if (pose == CampPose.LieDown || pose == CampPose.Sleep || pose == CampPose.RollOver)
            {
                if (front || rear) return new Vector3(front ? -65 : -75, 0, side * 12);
                if (lower) return new Vector3(105, 0, 0);
                if (bone == CampBone.Head) return new Vector3(15, 0, pose == CampPose.Sleep ? 16 : 0);
                if (adult && bone == CampBone.Spine) return new Vector3(65, 0, 0);
            }
            if (pose == CampPose.Shake)
            {
                if (bone == CampBone.FrontLeft) return new Vector3(-75, 0, adult ? 65 : -10);
                if (bone == CampBone.FrontLeftLower) return new Vector3(30 + Cycle(t * 2) * 10, 0, 0);
            }
            if (pose == CampPose.Dig)
            {
                if (front) return new Vector3(-35 + Cycle(t * 3 + (left ? 0 : .5f)) * 40, 0, adult ? side * 60 : 0);
                if (bone == CampBone.Head || bone == CampBone.Spine) return new Vector3(25, 0, 0);
            }
            if (pose == CampPose.Sniff && bone == CampBone.Head) return new Vector3(35, Cycle(t) * 12, 0);
            if ((pose == CampPose.Look || pose == CampPose.Idle) && bone == CampBone.Head) return new Vector3(Cycle(t * .2f) * 3, Cycle(t * .3f) * 16, 0);
            if (bone == CampBone.Tail) return new Vector3(0, Cycle(t * (pose == CampPose.Happy ? 4 : 1)) * 25, 0);
            if (adult && front) return new Vector3(0, 0, side * 72);
            return Vector3.zero;
        }
        void LateUpdate()
        {
            float dt=previewUnscaled?Time.unscaledDeltaTime:Time.deltaTime;
            if (rest == null || dt <= 0) return;
            AnimatedFrames++; ActualLegLift = ActualKickAngle = ActualContextAngle = 0;
            bool vendor = animator != null && animator.enabled && animator.runtimeAnimatorController != null;
            if (vendor) return;
            gaitPhase+=dt*LocomotionSpeed/Mathf.Max(.5f,groundSize*1.7f);
            float t = Pose == CampPose.Pee || Pose == CampPose.RollOver ? phase : Pose==CampPose.Walk||Pose==CampPose.Come?gaitPhase:previewUnscaled?Time.unscaledTime:Time.time;
            for (int i = 0; i < bones.Length; i++)
            {
                Vector3 pose = Rotation(Pose, roles[i], t, biped) * strength;
                var basis = Quaternion.Inverse(bones[i].parent.rotation) * transform.rotation;
                var rotation = basis * Quaternion.Euler(pose) * Quaternion.Inverse(basis) * rest[i];
                bones[i].localRotation = Quaternion.RotateTowards(bones[i].localRotation, rotation, dt * 260);
                var neutral = basis * Quaternion.Euler(Rotation(CampPose.Idle, roles[i], t, biped)) * Quaternion.Inverse(basis) * rest[i];
                ActualContextAngle = Mathf.Max(ActualContextAngle, Quaternion.Angle(neutral, bones[i].localRotation));
                if (roles[i] == CampBone.RearLeft) ActualLegLift = Quaternion.Angle(rest[i], bones[i].localRotation);
                if (roles[i] == CampBone.RearLeft || roles[i] == CampBone.RearRight) ActualKickAngle = Mathf.Max(ActualKickAngle, Quaternion.Angle(rest[i], bones[i].localRotation));
            }
            float drop = Pose == CampPose.Sit ? -.2f : Pose == CampPose.LieDown || Pose == CampPose.Sleep || Pose == CampPose.RollOver ? -.35f : 0;
            bodyDrop = Mathf.MoveTowards(bodyDrop, drop * groundSize, dt * groundSize);
            model.localPosition = modelPosition + Vector3.up * bodyDrop;
            var whole = modelRest;
            if (Pose == CampPose.RollOver) whole *= Quaternion.Euler(0, 0, Mathf.SmoothStep(0, 1, Mathf.Clamp01(phase)) * 360);
            else if (Pose == CampPose.BathShake) whole *= Quaternion.Euler(0, Cycle(Time.time * 5) * 13, Cycle(Time.time * 5) * 5);
            else if (Pose == CampPose.Happy) model.localPosition += Vector3.up * Mathf.Abs(Cycle(Time.time * 2)) * .08f * groundSize;
            model.localRotation = whole;
            ActualContextAngle = Mathf.Max(ActualContextAngle, Quaternion.Angle(modelRest, whole));
        }
        void OnDisable() { if (rest != null) ResetBones(); }
    }
}
