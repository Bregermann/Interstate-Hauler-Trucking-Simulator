using System;
using System.Collections.Generic;

namespace LWS.TruckTornadoRodeo
{
    [Serializable]
    public sealed class TornadoRules
    {
        public readonly int[] points = new int[4], flights = new int[4], landings = new int[4], rings = new int[4], turns = new int[4], dodges = new int[4];
        public readonly float[] bestHeight = new float[4];
        public readonly bool[] airborne = new bool[4], retired = new bool[4];
        readonly HashSet<string> awards = new HashSet<string>(StringComparer.Ordinal);
        readonly int[] heightTier = new int[4];
        readonly float[] pitch = new float[4], roll = new float[4], yaw = new float[4];
        public int Count { get; private set; }
        public float Clock { get; private set; }
        public float Remaining { get; private set; }
        public float Countdown { get; private set; }
        public bool Complete { get; private set; }
        public bool Live => !Complete && Countdown <= 0;
        public int Winner { get; private set; } = -1;
        public int Revision { get; private set; }
        public void Reset(int count, float duration)
        {
            Count = Math.Clamp(count, 1, 4); Remaining = float.IsFinite(duration) ? Math.Clamp(duration, 30, 300) : 90; Clock = 0; Countdown = 3; Complete = false; Winner = -1; Revision = 0; awards.Clear();
            Array.Clear(points, 0, 4); Array.Clear(flights, 0, 4); Array.Clear(landings, 0, 4); Array.Clear(rings, 0, 4); Array.Clear(turns, 0, 4); Array.Clear(dodges, 0, 4); Array.Clear(bestHeight, 0, 4); Array.Clear(airborne, 0, 4); Array.Clear(retired, 0, 4); Array.Clear(heightTier, 0, 4); Array.Clear(pitch, 0, 4); Array.Clear(roll, 0, 4); Array.Clear(yaw, 0, 4);
        }
        public void Tick(float dt)
        {
            if (Complete || !float.IsFinite(dt) || dt <= 0) return; dt = Math.Min(.1f, dt);
            if (Countdown > 0) { Countdown = Math.Max(0, Countdown - dt); return; }
            Clock += dt; Remaining = Math.Max(0, Remaining - dt); bool all = true; for (int i = 0; i < Count; i++) all &= retired[i];
            if (Remaining <= 0 || all) { Complete = true; Winner = 0; for (int i = 1; i < Count; i++) if (points[i] > points[Winner]) Winner = i; }
        }
        public bool BeginFlight(int slot, float height, bool nativeSupported)
        {
            if (!Valid(slot) || airborne[slot] || nativeSupported || !float.IsFinite(height) || height < 6) return false;
            airborne[slot] = true; flights[slot]++; heightTier[slot] = 0; pitch[slot] = roll[slot] = yaw[slot] = 0; return true;
        }
        public int ObserveHeight(int slot, float height)
        {
            if (!Valid(slot) || !airborne[slot] || !float.IsFinite(height) || height < 0 || height > 140) return 0;
            bestHeight[slot] = Math.Max(bestHeight[slot], height); int tier = Math.Min(10, (int)(height / 10));
            if (tier <= heightTier[slot]) return 0; int delta = (tier - heightTier[slot]) * 5; heightTier[slot] = tier; return Bank(slot, delta);
        }
        public int ObserveRotation(int slot, float dp, float dr, float dy)
        {
            if (!Valid(slot) || !airborne[slot] || !float.IsFinite(dp) || !float.IsFinite(dr) || !float.IsFinite(dy) || Math.Abs(dp) > 20 || Math.Abs(dr) > 20 || Math.Abs(dy) > 20) return 0;
            pitch[slot] += dp; roll[slot] += dr; yaw[slot] += dy; int count = Consume(ref pitch[slot]) + Consume(ref roll[slot]) + Consume(ref yaw[slot]);
            turns[slot] += count; return count > 0 ? Bank(slot, count * 12) : 0;
        }
        static int Consume(ref float degrees) { int count = (int)(Math.Abs(degrees) / 360); if (count > 0) degrees -= Math.Sign(degrees) * count * 360; return count; }
        public int PassRing(int slot, int flight, int ring)
        {
            if (!Valid(slot) || !airborne[slot] || flight != flights[slot] || ring < 0 || ring >= 8 || !awards.Add(slot + ":" + flight + ":ring:" + ring)) return 0;
            rings[slot]++; return Bank(slot, 30);
        }
        public bool RingPassed(int slot, int ring) => slot >= 0 && slot < Count && awards.Contains(slot + ":" + flights[slot] + ":ring:" + ring);
        public int Dodge(int slot, int flight, int debris)
        {
            if (!Valid(slot) || !airborne[slot] || flight != flights[slot] || debris < 0 || debris >= 32 || !awards.Add(slot + ":" + flight + ":dodge:" + debris)) return 0;
            dodges[slot]++; return Bank(slot, 3);
        }
        public int Land(int slot, int flight, bool nativeSupported, float upright, float impactDownSpeed, float planarSpeed, float stableSeconds)
        {
            if (!Valid(slot) || !airborne[slot] || flight != flights[slot] || !nativeSupported || !float.IsFinite(upright) || !float.IsFinite(impactDownSpeed) || !float.IsFinite(planarSpeed) || !float.IsFinite(stableSeconds) || stableSeconds < .8f) return 0;
            airborne[slot] = false;
            if (upright < .7f || impactDownSpeed > 12 || impactDownSpeed < 0 || planarSpeed > 16 || planarSpeed < 0) return 0;
            landings[slot]++; return Bank(slot, 40);
        }
        public void Retire(int slot) { if (slot < 0 || slot >= Count || Complete) return; retired[slot] = true; airborne[slot] = false; }
        bool Valid(int slot) => Live && slot >= 0 && slot < Count && !retired[slot];
        int Bank(int slot, int delta) { points[slot] += delta; Revision++; return delta; }
        public int Place(int slot) { int place = 1; for (int i = 0; i < Count; i++) if (points[i] > points[slot]) place++; return place; }
        public bool Tied { get { if (!Complete) return false; for (int i = 0; i < Count; i++) if (i != Winner && points[i] == points[Winner]) return true; return false; } }
    }
}
