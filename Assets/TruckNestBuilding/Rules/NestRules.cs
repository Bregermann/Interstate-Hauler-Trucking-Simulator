using System;
namespace LWS.TruckNestBuilding
{
    public enum NestMaterialKind { Stick, Branch, Leaves, Moss, Decoration }
    public enum NestMilestone { None, ReceiveEgg, Hatch }
    public static class NestSupportGraph
    {
        // -1 is the authored platform, -2 is unsupported. Cycles never count.
        public static bool Rooted(int node, int[] parents, bool[] connected)
        {
            if (parents == null || connected == null || parents.Length != connected.Length || node < 0 || node >= parents.Length) return false;
            for (int step = 0; step <= parents.Length; step++)
            {
                if (node < 0 || node >= parents.Length || !connected[node]) return false;
                int parent = parents[node]; if (parent == -1) return true; if (parent < 0) return false; node = parent;
            }
            return false;
        }
    }
    public sealed class NestRules
    {
        public const float SessionLimit = 360;
        public int Sites { get; private set; }
        public bool Cooperative { get; private set; }
        public readonly float[] settled = new float[4], incubation = new float[4];
        public readonly bool[] received = new bool[4], hatched = new bool[4];
        public readonly int[] quality = new int[4];
        public void Begin(int players, bool cooperative)
        {
            if (players < 1 || players > 4) throw new ArgumentOutOfRangeException(nameof(players));
            Sites = cooperative ? 1 : players; Cooperative = cooperative;
            Array.Clear(settled, 0, 4); Array.Clear(incubation, 0, 4); Array.Clear(received, 0, 4); Array.Clear(hatched, 0, 4); Array.Clear(quality, 0, 4);
        }
        public NestMilestone Observe(int site, bool allCoreRooted, bool physicalEggStable, int rootedPieces, int varieties, int ornaments, float dt)
        {
            if (site < 0 || site >= Sites || !float.IsFinite(dt) || dt <= 0 || dt > .25f) return NestMilestone.None;
            quality[site] = Math.Max(0, rootedPieces) * 20 + Math.Max(0, varieties - 1) * 10 + Math.Max(0, ornaments) * 15 + (hatched[site] ? 150 : 0);
            if (hatched[site]) return NestMilestone.None;
            settled[site] = allCoreRooted ? settled[site] + dt : 0;
            if (!allCoreRooted) incubation[site] = 0;
            if (!received[site] && settled[site] >= 3) { received[site] = true; return NestMilestone.ReceiveEgg; }
            incubation[site] = received[site] && allCoreRooted && physicalEggStable ? incubation[site] + dt : 0;
            if (incubation[site] >= 5) { hatched[site] = true; quality[site] += 150; return NestMilestone.Hatch; }
            return NestMilestone.None;
        }
        public void EggLost(int site) { if (site < 0 || site >= Sites || hatched[site]) return; received[site] = false; settled[site] = incubation[site] = 0; }
        public bool Complete { get { if (Sites == 0) return false; for (int i = 0; i < Sites; i++) if (!hatched[i]) return false; return true; } }
        public int Place(int site) { int place = 1; for (int i = 0; i < Sites; i++) if (quality[i] > quality[site]) place++; return place; }
    }
}
