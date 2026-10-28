using System;
using UnityEngine;
using LWS.TruckSports;

namespace LWS.TruckYoga
{
    // Stationary expressive sport, like Dance: NWH power is disabled while this
    // component alone moves the authoritative kinematic body in fixed steps.
    public sealed class YogaTruckRig : MonoBehaviour
    {
        public SportsEventTruck truck;
        public Transform cabArticulation;
        public Rigidbody[] attachedBodies = Array.Empty<Rigidbody>();
        public Vector3 home;
        public int slot;
        public int ControlBank { get; private set; }
        public Vector4 Intent { get; private set; }
        public Vector4 Achieved { get; private set; }
        public Vector4 Previous { get; private set; }
        public int PoseSteps { get; private set; }
        public string BankLabel => ControlBank == 0 ? "PITCH / ROLL" : "LIFT / CAB TWIST";
        Quaternion upright;
        Vector3 restCabPosition;
        Quaternion restCabRotation;
        Vector3[] attachedPositions;
        Quaternion[] attachedRotations;
        bool initialized;
        float lift, pitch, roll, twist;
        int revision = -1;
        public int BoundAppearanceRevision => revision;
        void Awake()
        {
            truck.Power(false); truck.body.isKinematic = true;
            foreach (var body in attachedBodies) { body.isKinematic = true; body.detectCollisions = false; }
        }
        public void Begin(bool active)
        {
            if (!initialized)
            {
                upright = truck.transform.rotation; restCabPosition = cabArticulation.localPosition; restCabRotation = cabArticulation.localRotation;
                attachedPositions = new Vector3[attachedBodies.Length]; attachedRotations = new Quaternion[attachedBodies.Length];
                for (int i = 0; i < attachedBodies.Length; i++) { attachedPositions[i] = truck.transform.InverseTransformPoint(attachedBodies[i].position); attachedRotations[i] = Quaternion.Inverse(truck.transform.rotation) * attachedBodies[i].rotation; }
                initialized = true;
            }
            truck.gameObject.SetActive(active); if (!active) return;
            truck.Prepare(home, upright, false); truck.Power(false); truck.body.isKinematic = true;
            truck.body.detectCollisions = false;
            foreach (var body in attachedBodies) { body.isKinematic = true; body.detectCollisions = false; }
            ControlBank = PoseSteps = 0; lift = pitch = roll = twist = 0; Intent = Achieved = Previous = Vector4.zero;
            cabArticulation.localPosition = restCabPosition; cabArticulation.localRotation = restCabRotation;
            RefreshAppearanceArticulation();
            ApplyCabTwist(0);
        }
        // All catalog art, physical cab offsets and rolling anchors inherit one
        // coherent assembly pose. Inseparable cabs use the explicit whole-truck
        // yaw fallback; no independently turned chassis leaves detached tires.
        public void RefreshAppearanceArticulation()
        {
            if (truck.appearanceHost == null) return;
            int current = truck.appearanceHost.AppearanceRevision;
            if (revision == current) return;
            revision = current;
        }
        public void ChangeBank() { ControlBank = 1 - ControlBank; }
        public Vector2 AI(YogaPose pose)
        {
            Vector4 error = YogaPoses.Target(pose) - Achieved;
            float bodyError = Mathf.Abs(error.x) / 30 + Mathf.Abs(error.y) / 26;
            float cabError = Mathf.Abs(error.z) + Mathf.Abs(error.w) / 40;
            ControlBank = bodyError > cabError ? 0 : 1;
            return ControlBank == 0 ? Vector2.ClampMagnitude(new Vector2(error.y / 8, error.x / 8), 1) : Vector2.ClampMagnitude(new Vector2(error.w / 9, error.z / .23f), 1);
        }
        public void Step(Vector2 axes, bool changeBank, float dt)
        {
            if (!truck.gameObject.activeInHierarchy || !float.IsFinite(dt) || dt <= 0) return;
            RefreshAppearanceArticulation(); if (changeBank) ChangeBank();
            var target = Intent;
            if (ControlBank == 0) { target.x = Mathf.Clamp(target.x + axes.y * 30 * dt, -38, 32); target.y = Mathf.Clamp(target.y + axes.x * 26 * dt, -30, 30); }
            else { target.z = Mathf.Clamp(target.z + axes.y * .85f * dt, -.2f, 1.4f); target.w = Mathf.Clamp(target.w + axes.x * 42 * dt, -40, 40); }
            Intent = target; Previous = Achieved;
            // Read the prior actual fixed-step body pose before commanding next.
            var local = Quaternion.Inverse(upright) * truck.body.rotation;
            var euler = local.eulerAngles;
            float actualTwist = Mathf.DeltaAngle(0, euler.y);
            Achieved = new Vector4(Mathf.DeltaAngle(0, euler.x), Mathf.DeltaAngle(0, euler.z), truck.body.position.y - home.y, actualTwist);
            pitch = Mathf.MoveTowards(pitch, target.x, dt * 24); roll = Mathf.MoveTowards(roll, target.y, dt * 22);
            lift = Mathf.MoveTowards(lift, target.z, dt * .7f); twist = Mathf.MoveTowards(twist, target.w, dt * 35);
            truck.body.MovePosition(home + Vector3.up * lift);
            var bodyRotation = upright * Quaternion.Euler(pitch, twist, roll);
            truck.body.MoveRotation(bodyRotation);
            for (int i = 0; i < attachedBodies.Length; i++)
            {
                attachedBodies[i].MovePosition(home + Vector3.up * lift + bodyRotation * attachedPositions[i]);
                attachedBodies[i].MoveRotation(bodyRotation * attachedRotations[i]);
            }
            ApplyCabTwist(twist);
            PoseSteps++;
        }
        void ApplyCabTwist(float angle)
        {
            var rotation = Quaternion.Euler(0, angle, 0); cabArticulation.localRotation = restCabRotation * rotation;
        }
        public void Stop()
        {
            truck.Power(false); truck.StopSound(); truck.body.isKinematic = true;
            if (!initialized) return;
            truck.body.position = home; truck.body.rotation = upright;
            for (int i = 0; i < attachedBodies.Length; i++)
            {
                attachedBodies[i].isKinematic = true;
                attachedBodies[i].position = home + upright * attachedPositions[i];
                attachedBodies[i].rotation = upright * attachedRotations[i];
            }
            ApplyCabTwist(0); cabArticulation.localPosition = restCabPosition;
        }
    }
}
