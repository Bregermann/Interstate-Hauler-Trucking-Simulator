using System;

namespace LWS.TruckHotPotato
{
    public enum HotPotatoAwardKind { Pass, Survival, LastStanding }
    // One ownership/scoring authority. Contacts are reported by real truck
    // colliders; proximity, AI decisions and explosion damage cannot transfer.
    [Serializable]
    public sealed class HotPotatoRules
    {
        public float minimumFuse = 12, maximumFuse = 20, transferCooldown = .7f;
        public int transferPoints = 3;
        public int TransferPoints => Math.Max(1, transferPoints);
        public int Count { get; private set; }
        public int Carrier { get; private set; } = -1;
        public int Winner { get; private set; } = -1;
        public int Cycle { get; private set; }
        public int Transfers { get; private set; }
        public float Clock { get; private set; }
        public float Fuse { get; private set; }
        public float Cooldown => Math.Max(0, nextTransfer - Clock);
        public bool Complete { get; private set; }
        public readonly bool[] alive = new bool[8];
        public readonly int[] scores = new int[8], passes = new int[8], survived = new int[8];
        public event Action<HotPotatoAwardKind, string, int, int, int> Awarded;
        void Award(HotPotatoAwardKind kind, string id, int slot, int amount)
        { scores[slot] += amount; Awarded?.Invoke(kind, id, slot, amount, scores[slot]); }
        readonly float[,] lastContact = new float[8, 8];
        readonly bool[,] pending = new bool[8, 8];
        Random random;
        float nextTransfer;

        public void Reset(int count, int seed)
        {
            if (count < 2 || count > 8) throw new ArgumentOutOfRangeException(nameof(count));
            Count = count; random = new Random(seed); Clock = nextTransfer = 0;
            Winner = Carrier = -1; Cycle = Transfers = 0; Complete = false;
            Array.Clear(alive, 0, alive.Length); Array.Clear(scores, 0, scores.Length);
            Array.Clear(passes, 0, passes.Length); Array.Clear(survived, 0, survived.Length);
            for (int a = 0; a < 8; a++) for (int b = 0; b < 8; b++) { lastContact[a, b] = -100; pending[a, b] = false; }
            for (int i = 0; i < count; i++) alive[i] = true;
            NextCycle();
        }

        public void Contact(int a, int b, float fixedStep)
        {
            if (Complete || a < 0 || b < 0 || a >= Count || b >= Count || a == b || !alive[a] || !alive[b]) return;
            if (a > b) { int swap = a; a = b; b = swap; }
            // Compound colliders and continuous resting contact produce one
            // edge. Same pair must genuinely separate for at least two steps.
            bool separated = Clock - lastContact[a, b] > Math.Max(.06f, fixedStep * 2.5f);
            lastContact[a, b] = Clock;
            if (separated) pending[a, b] = true;
        }

        public int Step(float dt)
        {
            if (dt <= 0 || float.IsNaN(dt)) throw new ArgumentOutOfRangeException(nameof(dt));
            if (Complete) return -1;
            // Contacts from the completed physical step precede the next clock
            // advance. Simultaneous candidates use stable slot order, once.
            if (Clock >= nextTransfer)
                for (int other = 0; other < Count; other++)
                {
                    int a = Math.Min(Carrier, other), b = Math.Max(Carrier, other);
                    if (other == Carrier || !alive[other] || !pending[a, b]) continue;
                    int prior = Carrier; Carrier = other; Transfers++; passes[prior]++;
                    Award(HotPotatoAwardKind.Pass, "cycle:" + Cycle + ":transfer:" + Transfers, prior, TransferPoints);
                    nextTransfer = Clock + transferCooldown; break;
                }
            Array.Clear(pending, 0, pending.Length);
            Clock += dt; Fuse -= dt;
            if (Fuse > 0) return -1;
            int eliminated = Carrier; alive[eliminated] = false; Carrier = -1;
            int remaining = 0, survivor = -1;
            for (int i = 0; i < Count; i++) if (alive[i]) { survived[i]++; Award(HotPotatoAwardKind.Survival, "cycle:" + Cycle + ":survival:" + i, i, 20); remaining++; survivor = i; }
            if (remaining == 1) { Winner = survivor; Award(HotPotatoAwardKind.LastStanding, "cycle:" + Cycle + ":winner:" + survivor, survivor, 100); Complete = true; }
            else NextCycle();
            return eliminated;
        }

        void NextCycle()
        {
            Cycle++; int choice = random.Next(AliveCount());
            for (int i = 0; i < Count; i++) if (alive[i] && choice-- == 0) { Carrier = i; break; }
            Fuse = minimumFuse + (float)random.NextDouble() * Math.Max(0, maximumFuse - minimumFuse);
            nextTransfer = Clock + transferCooldown;
        }
        public int AliveCount() { int count = 0; for (int i = 0; i < Count; i++) if (alive[i]) count++; return count; }
    }
}
