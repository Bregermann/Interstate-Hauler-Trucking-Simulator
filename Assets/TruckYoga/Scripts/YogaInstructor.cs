using System;
using UnityEngine;

namespace LWS.TruckYoga
{
    // Genuine UTS Generic biped skin and explicitly authored bone bindings. A stationary instructor
    // performs seven original bone routines; no frozen character slides around.
    public sealed class YogaInstructor : MonoBehaviour
    {
        public YogaMatch match;
        public Animator animator;
        public Transform[] bones = Array.Empty<Transform>();
        public HumanBodyBones[] roles = Array.Empty<HumanBodyBones>();
        Quaternion[] rest;
        Vector3[] restPositions;
        public int AnimatedFrames { get; private set; }
        public YogaPose PresentedPose { get; private set; }
        void Awake()
        {
            rest = new Quaternion[bones.Length]; restPositions = new Vector3[bones.Length];
            for (int i = 0; i < bones.Length; i++) { rest[i] = bones[i].localRotation; restPositions[i] = bones[i].localPosition; }
            // The procedural bone routine is the sole writer for this actor.
            animator.applyRootMotion = false; animator.enabled = false;
        }
        public static Vector3 BonePose(YogaPose pose, HumanBodyBones role)
        {
            bool left = role.ToString().StartsWith("Left", StringComparison.Ordinal);
            float sign = left ? 1 : -1;
            if (role == HumanBodyBones.Hips) return new Vector3(pose == YogaPose.DownwardFacingTruck ? 32 : pose == YogaPose.TruckBridge ? -30 : 0, 0, 0);
            if (role == HumanBodyBones.Spine || role == HumanBodyBones.Chest)
                return new Vector3(pose == YogaPose.DownwardFacingTruck ? 32 : pose == YogaPose.TruckBridge ? -26 : pose == YogaPose.RestingTruck ? 14 : 0, pose == YogaPose.CabTwist ? 24 : 0, 0);
            if (role == HumanBodyBones.LeftUpperArm || role == HumanBodyBones.RightUpperArm)
                return new Vector3(pose == YogaPose.DownwardFacingTruck ? -55 : pose == YogaPose.RestingTruck ? -25 : -10, 0,
                    sign * (pose == YogaPose.WarriorWheelie || pose == YogaPose.CabTwist ? 0 : pose == YogaPose.TwoWheelTree ? -100 : pose == YogaPose.SuspensionStretch ? -90 : 70));
            if (role == HumanBodyBones.LeftLowerArm || role == HumanBodyBones.RightLowerArm)
                return new Vector3(0, pose == YogaPose.TwoWheelTree ? sign * 65 : 0, pose == YogaPose.CabTwist ? sign * -40 : 0);
            if (role == HumanBodyBones.LeftUpperLeg || role == HumanBodyBones.RightUpperLeg)
                return new Vector3(pose == YogaPose.DownwardFacingTruck ? -45 : pose == YogaPose.TruckBridge ? -70 : pose == YogaPose.RestingTruck ? -65 : pose == YogaPose.WarriorWheelie ? left ? -55 : 20 : pose == YogaPose.TwoWheelTree && left ? -40 : 0, 0,
                    pose == YogaPose.TwoWheelTree && left ? 35 : pose == YogaPose.RestingTruck ? sign * 30 : 0);
            if (role == HumanBodyBones.LeftLowerLeg || role == HumanBodyBones.RightLowerLeg)
                return new Vector3(pose == YogaPose.TruckBridge ? 95 : pose == YogaPose.RestingTruck ? 100 : pose == YogaPose.TwoWheelTree && left ? 110 : pose == YogaPose.WarriorWheelie && left ? 65 : 0, 0, 0);
            return Vector3.zero;
        }
        void LateUpdate()
        {
            if (match == null || !match.Live || rest == null) return;
            PresentedPose = match.rules.Pose; AnimatedFrames++;
            for (int i = 0; i < bones.Length; i++)
            {
                Vector3 pose = BonePose(PresentedPose, roles[i]);
                if (roles[i] == HumanBodyBones.Chest) pose.x += Mathf.Sin(Time.time * 1.4f) * 1.3f;
                // Biped bone axes are not Humanoid muscle axes. Convert the
                // semantic actor-space rotation into the actual parent frame,
                // preserving the installed bind pose and genuine skinning.
                var basis = Quaternion.Inverse(bones[i].parent.rotation) * animator.transform.rotation;
                var target = basis * Quaternion.Euler(pose) * Quaternion.Inverse(basis) * rest[i];
                bones[i].localRotation = Quaternion.RotateTowards(bones[i].localRotation, target, Time.deltaTime * 100);
                if (roles[i] == HumanBodyBones.Hips)
                {
                    float lower = PresentedPose == YogaPose.RestingTruck ? -.55f : PresentedPose == YogaPose.TruckBridge ? -.42f : PresentedPose == YogaPose.DownwardFacingTruck ? -.25f : 0;
                    var offset = bones[i].parent.InverseTransformVector(Vector3.up * lower);
                    bones[i].localPosition = Vector3.MoveTowards(bones[i].localPosition, restPositions[i] + offset, Time.deltaTime * .65f);
                }
            }
        }
        void OnDisable() { if (rest != null) for (int i = 0; i < bones.Length; i++) if (bones[i] != null) { bones[i].localRotation = rest[i]; bones[i].localPosition = restPositions[i]; } }
    }
}
