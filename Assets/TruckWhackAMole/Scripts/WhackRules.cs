using System;
namespace LWS.TruckWhackAMole
{
    public enum MoleKind { Normal, Golden, Decoy }
    public enum MolePhase { Underground, Emerging, Exposed, Retreating, Whacked }
    [Serializable]
    public sealed class WhackRules
    {
        public int Count { get; private set; }
        public int Holes { get; private set; }
        public float Clock { get; private set; }
        public float Duration { get; private set; }
        public bool Complete { get; private set; }
        public int Contacts { get; private set; }
        public readonly int[] scores = new int[8], normalHits = new int[8], goldenHits = new int[8], decoyHits = new int[8];
        public readonly int[] generations = new int[64];
        public readonly MoleKind[] kinds = new MoleKind[64];
        readonly float[] born = new float[64], windows = new float[64], whacked = new float[64];
        readonly bool[] occupied = new bool[64], claimed = new bool[64];
        Random random; int nextHole, stride; float nextSpawn;
        public void Reset(int count, int holes, int seed, float duration = 180)
        {
            if (count < 1 || count > 8 || holes < 12 || holes > 64 || duration < 15 || float.IsNaN(duration)) throw new ArgumentOutOfRangeException();
            Count = count; Holes = holes; Duration = duration; Clock = 0; Complete = false; Contacts = 0; nextSpawn = 0;
            random = new Random(seed); nextHole = random.Next(holes); stride = holes / 2 + 1;
            while (Gcd(stride, holes) != 1) stride++;
            Array.Clear(scores, 0, 8); Array.Clear(normalHits, 0, 8); Array.Clear(goldenHits, 0, 8); Array.Clear(decoyHits, 0, 8); Array.Clear(generations, 0, 64); Array.Clear(occupied, 0, 64); Array.Clear(claimed, 0, 64);
        }
        static int Gcd(int a, int b) { while (b != 0) { int next = a % b; a = b; b = next; } return a; }
        public float Age(int hole) => Clock - born[hole];
        public MolePhase Phase(int hole)
        {
            if (hole < 0 || hole >= Holes || !occupied[hole]) return MolePhase.Underground;
            if (claimed[hole]) return Clock - whacked[hole] < .55f ? MolePhase.Whacked : MolePhase.Underground;
            float age = Age(hole);
            return age < .5f ? MolePhase.Emerging : age < windows[hole] + .5f ? MolePhase.Exposed : age < windows[hole] + 1 ? MolePhase.Retreating : MolePhase.Underground;
        }
        public float Visibility(int hole)
        {
            switch (Phase(hole))
            {
                case MolePhase.Emerging: return Age(hole) / .5f;
                case MolePhase.Exposed: return 1;
                case MolePhase.Retreating: return Math.Max(0, 1 - (Age(hole) - windows[hole] - .5f) / .5f);
                case MolePhase.Whacked: return Math.Max(0, 1 - (Clock - whacked[hole]) / .55f);
                default: return 0;
            }
        }
        public void Step(float dt)
        {
            if (dt <= 0 || float.IsNaN(dt) || float.IsInfinity(dt)) throw new ArgumentOutOfRangeException(nameof(dt));
            if (Complete) return;
            Clock = Math.Min(Duration, Clock + dt);
            if (Clock >= Duration) { Complete = true; return; }
            int active = 0;
            for (int i = 0; i < Holes; i++) { if (Phase(i) == MolePhase.Underground) occupied[i] = false; if (occupied[i]) active++; }
            int capacity = Math.Min(10, 3 + (int)(Clock / 20));
            if (Clock < nextSpawn || active >= capacity) return;
            for (int i = 0; i < Holes; i++)
            {
                int hole = nextHole; nextHole = (nextHole + stride) % Holes;
                if (occupied[hole]) continue;
                occupied[hole] = true; claimed[hole] = false; born[hole] = Clock; generations[hole]++;
                windows[hole] = Math.Max(1.5f, 4.2f - 2.7f * Clock / Duration);
                double choice = random.NextDouble(); kinds[hole] = choice < .12 ? MoleKind.Golden : choice < .23 ? MoleKind.Decoy : MoleKind.Normal;
                nextSpawn = Clock + .3f; break;
            }
        }
        public bool Hit(int slot, int hole, int generation, float relativeSpeed)
        {
            if (Complete || Clock >= Duration || slot < 0 || slot >= Count || hole < 0 || hole >= Holes || relativeSpeed < .8f || float.IsNaN(relativeSpeed) || Phase(hole) != MolePhase.Exposed || generations[hole] != generation) return false;
            claimed[hole] = true; whacked[hole] = Clock; Contacts++;
            switch (kinds[hole])
            {
                case MoleKind.Golden: scores[slot] += 50; goldenHits[slot]++; break;
                case MoleKind.Decoy: scores[slot] -= 25; decoyHits[slot]++; break;
                default: scores[slot] += 10; normalHits[slot]++; break;
            }
            return true;
        }
        public int Place(int slot) { int place = 1; for (int i = 0; i < Count; i++) if (scores[i] > scores[slot]) place++; return place; }
    }
}
