using System;
namespace LWS.TruckEggSpoonRace
{
    public sealed class EggSpoonRules
    {
        public const int Gates = 3;
        public const float SpillPenalty = 8, RaceLimit = 180;
        public int Count { get; private set; }
        public int Format { get; private set; }
        public readonly int[] checkpoint = new int[4], spills = new int[4], replacements = new int[4];
        public readonly bool[] carrying = new bool[4], finished = new bool[4], dnf = new bool[4];
        public readonly float[] time = new float[4], bestClean = new float[4];
        public bool Complete { get { if (Count == 0) return false; for (int i = 0; i < Count; i++) if (!finished[i] && !dnf[i]) return false; return true; } }
        public void Begin(int count, int format)
        {
            if (count < 1 || count > 4 || format < 0 || format > 2) throw new ArgumentOutOfRangeException();
            Count = count; Format = format;
            Array.Clear(checkpoint, 0, 4); Array.Clear(spills, 0, 4); Array.Clear(replacements, 0, 4); Array.Clear(finished, 0, 4); Array.Clear(dnf, 0, 4);
            for (int i = 0; i < 4; i++) { carrying[i] = i < count; time[i] = bestClean[i] = float.PositiveInfinity; }
        }
        bool Active(int slot) => slot >= 0 && slot < Count && !finished[slot] && !dnf[slot];
        public bool Spill(int slot) { if (!Active(slot) || !carrying[slot]) return false; carrying[slot] = false; spills[slot]++; return true; }
        public bool Replace(int slot, bool atSecuredStation, bool stopped, float serviceSeconds)
        {
            if (!Active(slot) || carrying[slot] || !atSecuredStation || !stopped || !float.IsFinite(serviceSeconds) || serviceSeconds < 3) return false;
            carrying[slot] = true; replacements[slot]++; return true;
        }
        public bool Cross(int slot, int gate)
        {
            if (!Active(slot) || !carrying[slot] || checkpoint[slot] != gate || gate < 0 || gate >= Gates) return false;
            checkpoint[slot]++; return true;
        }
        public bool Finish(int slot, float elapsed)
        {
            if (!Active(slot) || !carrying[slot] || checkpoint[slot] != Gates || !float.IsFinite(elapsed) || elapsed < 0 || elapsed > RaceLimit) return false;
            finished[slot] = true; time[slot] = elapsed + spills[slot] * SpillPenalty;
            if (spills[slot] == 0) bestClean[slot] = elapsed;
            return true;
        }
        public void Fail(int slot) { if (Active(slot)) dnf[slot] = true; }
        public int Place(int slot)
        {
            int rank = 1; for (int i = 0; i < Count; i++) if (i != slot && Better(i, slot)) rank++; return rank;
        }
        bool Better(int a, int b)
        {
            if (finished[a] != finished[b]) return finished[a];
            if (!finished[a]) { if (checkpoint[a] != checkpoint[b]) return checkpoint[a] > checkpoint[b]; return spills[a] < spills[b]; }
            if (Format == 1 && (spills[a] == 0) != (spills[b] == 0)) return spills[a] == 0;
            if (Format == 2 && spills[a] != spills[b]) return spills[a] < spills[b];
            if (Math.Abs(time[a] - time[b]) > .001f) return time[a] < time[b];
            return spills[a] < spills[b];
        }
    }
}
