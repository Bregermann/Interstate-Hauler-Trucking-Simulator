using System;
using UnityEngine;

namespace LWS.TruckYoga
{
    public enum YogaPose { DownwardFacingTruck, WarriorWheelie, TwoWheelTree, SuspensionStretch, CabTwist, TruckBridge, RestingTruck }
    public static class YogaPoses
    {
        public const int Count = 7;
        public static readonly string[] Names = { "DOWNWARD-FACING TRUCK", "WARRIOR WHEELIE", "TWO-WHEEL TREE", "SUSPENSION STRETCH", "CAB TWIST", "TRUCK BRIDGE", "RESTING TRUCK" };
        // pitch degrees, roll degrees, suspension/body height metres, cab yaw degrees.
        public static Vector4 Target(YogaPose pose)
        {
            switch (pose)
            {
                case YogaPose.DownwardFacingTruck: return new Vector4(24, 0, .15f, 0);
                case YogaPose.WarriorWheelie: return new Vector4(-32, 0, .55f, 0);
                case YogaPose.TwoWheelTree: return new Vector4(0, 24, .5f, 0);
                case YogaPose.SuspensionStretch: return new Vector4(0, 0, 1.2f, 0);
                case YogaPose.CabTwist: return new Vector4(0, 0, .25f, 32);
                case YogaPose.TruckBridge: return new Vector4(-14, 0, .95f, 0);
                default: return new Vector4(0, 0, -.12f, 0);
            }
        }
        public static float Accuracy(Vector4 actual, YogaPose pose)
        {
            var delta = actual - Target(pose);
            if (!float.IsFinite(delta.x) || !float.IsFinite(delta.y) || !float.IsFinite(delta.z) || !float.IsFinite(delta.w)) return 0;
            float error = Mathf.Max(Mathf.Abs(delta.x) / 30, Mathf.Abs(delta.y) / 26, Mathf.Abs(delta.z) / .95f, Mathf.Abs(delta.w) / 40);
            return Mathf.Clamp01(1 - error);
        }
        public static float Stability(Vector4 current, Vector4 previous, float dt)
        {
            if (!float.IsFinite(dt) || dt <= 0) return 0;
            var delta = (current - previous) / dt;
            return Mathf.Clamp01(1 - Mathf.Max(Mathf.Abs(delta.x) / 35, Mathf.Abs(delta.y) / 30, Mathf.Abs(delta.z) / .9f, Mathf.Abs(delta.w) / 55));
        }
    }
    public sealed class YogaRules
    {
        public readonly float[] score = new float[4], hold = new float[4], accuracy = new float[4], stability = new float[4];
        public readonly int[] completed = new int[4];
        readonly bool[] awarded = new bool[4];
        public int Count { get; private set; }
        public YogaPose Pose { get; private set; }
        public float PoseSeconds { get; private set; }
        public bool Free { get; private set; }
        public bool Finished { get; private set; }
        public const float HoldRequired = 3, PoseLimit = 22;
        public void Begin(int count, bool free)
        {
            if (count < 1 || count > 4) throw new ArgumentOutOfRangeException(nameof(count));
            Count = count; Free = free; Pose = YogaPose.DownwardFacingTruck; Finished = false; PoseSeconds = 0;
            Array.Clear(score, 0, 4); Array.Clear(completed, 0, 4); ResetHold();
        }
        void ResetHold() { PoseSeconds = 0; Array.Clear(hold, 0, 4); Array.Clear(accuracy, 0, 4); Array.Clear(stability, 0, 4); Array.Clear(awarded, 0, 4); }
        public bool Observe(int slot, Vector4 actual, Vector4 previous, float dt)
        {
            if (Finished || slot < 0 || slot >= Count || !float.IsFinite(dt) || dt <= 0 || dt > .25f) return false;
            accuracy[slot] = YogaPoses.Accuracy(actual, Pose); stability[slot] = YogaPoses.Stability(actual, previous, dt);
            bool holding = accuracy[slot] >= .86f && stability[slot] >= .65f;
            hold[slot] = holding ? hold[slot] + dt : 0;
            // Free yoga has no points, compulsory winner or competitive bonus.
            if (!Free && accuracy[slot] >= .72f) score[slot] += dt * 10 * accuracy[slot] * stability[slot];
            if (!awarded[slot] && hold[slot] >= HoldRequired)
            {
                awarded[slot] = true; completed[slot]++; if (!Free) score[slot] += 25; return true;
            }
            return false;
        }
        public bool Tick(float dt)
        {
            if (Finished || !float.IsFinite(dt) || dt <= 0 || dt > .25f) return false;
            PoseSeconds += dt; if (Free) return false;
            bool all = true; for (int i = 0; i < Count; i++) all &= awarded[i];
            if (!all && PoseSeconds < PoseLimit) return false;
            return NextPose();
        }
        public bool NextPose()
        {
            if (Finished) return false;
            if ((int)Pose == YogaPoses.Count - 1)
            {
                if (Free) Pose = YogaPose.DownwardFacingTruck;
                else { Finished = true; return true; }
            }
            else Pose++;
            ResetHold(); return true;
        }
        public int Place(int slot) { if (Free) return 0; int rank = 1; for (int i = 0; i < Count; i++) if (score[i] > score[slot] + .01f) rank++; return rank; }
    }
}
