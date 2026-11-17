using System;
namespace LWS.TruckHorrorSports
{
    public enum HorrorSport { GraveyardShift, ScarecrowSeason, DeadEnd, CursedCargo }
    public enum HorrorOutcome { Active, Escaped, Lost, NightComplete, Contained }
    /// <summary>Actual objective receipts. Interactions are committed only by the physical match owner.</summary>
    [Serializable]
    public sealed class HorrorObjectives
    {
        public const int Capacity = 12;
        readonly bool[,] completed = new bool[Capacity, 32];
        readonly bool[,] convoyArrivals = new bool[Capacity, 32];
        public readonly int[] tasks = new int[Capacity];
        public readonly int[] convoyStops = new int[Capacity], containmentStops = new int[Capacity], roadGates = new int[Capacity], wrongSignals = new int[Capacity], rescues = new int[Capacity];
        public readonly int[] taskCycles = new int[Capacity];
        public readonly float[] distance = new float[Capacity];
        public readonly float[] health = new float[Capacity], survival = new float[Capacity], exposure = new float[Capacity];
        public readonly HorrorOutcome[] outcome = new HorrorOutcome[Capacity];
        public int Count { get; private set; }
        public bool Cooperative { get; private set; }
        public void Begin(int count, bool cooperative)
        {
            Count = Math.Clamp(count, 1, Capacity); Cooperative = cooperative;
            Array.Clear(completed, 0, completed.Length); Array.Clear(tasks, 0, tasks.Length);
            Array.Clear(convoyArrivals, 0, convoyArrivals.Length); Array.Clear(convoyStops, 0, convoyStops.Length);
            Array.Clear(containmentStops, 0, containmentStops.Length); Array.Clear(roadGates, 0, roadGates.Length);
            Array.Clear(wrongSignals, 0, wrongSignals.Length); Array.Clear(rescues, 0, rescues.Length); Array.Clear(distance, 0, distance.Length);
            Array.Clear(taskCycles, 0, taskCycles.Length);
            Array.Clear(survival, 0, survival.Length); Array.Clear(exposure, 0, exposure.Length);
            for (int i = 0; i < Capacity; i++) { health[i] = 100; outcome[i] = HorrorOutcome.Active; }
        }
        public bool Done(int slot, int task) => slot >= 0 && slot < Count && task >= 0 && task < 32 && completed[Cooperative ? 0 : slot, task];
        public bool Commit(int slot, int task)
        {
            if (slot < 0 || slot >= Count || task < 0 || task >= 32 || outcome[slot] != HorrorOutcome.Active) return false;
            int owner = Cooperative ? 0 : slot;
            if (completed[owner, task]) return false;
            completed[owner, task] = true; tasks[slot]++; return true;
        }
        public void Damage(int slot, float amount)
        {
            if (slot < 0 || slot >= Count || float.IsNaN(amount) || float.IsInfinity(amount)) return;
            if (outcome[slot] != HorrorOutcome.Active) return;
            health[slot] = Math.Max(0, health[slot] - Math.Max(0, amount));
            if (health[slot] == 0) outcome[slot] = HorrorOutcome.Lost;
        }
        public bool ConvoyArrived(int slot, int task) => slot >= 0 && slot < Count && task >= 0 && task < 32 && convoyArrivals[slot, task];
        public bool RegisterConvoyArrival(int slot, int task)
        {
            if (slot < 0 || slot >= Count || task < 0 || task >= 32 || outcome[slot] != HorrorOutcome.Active || convoyArrivals[slot, task]) return false;
            convoyArrivals[slot, task] = true; convoyStops[slot]++; return true;
        }
        public bool AllConvoyArrived(int task)
        {
            if (task < 0 || task >= 32 || Count == 0) return false;
            for (int i = 0; i < Count; i++) if (outcome[i] == HorrorOutcome.Lost || !convoyArrivals[i, task]) return false;
            return true;
        }
        public bool RegisterRescue(int helper, int rescued)
        {
            if (!Cooperative || helper < 0 || helper >= Count || rescued < 0 || rescued >= Count || helper == rescued || outcome[helper] != HorrorOutcome.Active || outcome[rescued] != HorrorOutcome.Lost) return false;
            outcome[rescued] = HorrorOutcome.Active; health[rescued] = 40; exposure[rescued] = 0; rescues[helper]++; return true;
        }
        public bool RegisterRoadGate(int slot, int gate, int gateCount)
        {
            if (slot < 0 || slot >= Count || gateCount <= 0 || gate < 0 || gate >= gateCount || outcome[slot] != HorrorOutcome.Active || roadGates[slot] % gateCount != gate) return false;
            roadGates[slot]++; return true;
        }
        public bool BeginNextTaskCycle(int slot)
        {
            if (Cooperative || slot < 0 || slot >= Count || outcome[slot] != HorrorOutcome.Active) return false;
            for (int task = 0; task < 32; task++) completed[slot, task] = false;
            taskCycles[slot]++; return true;
        }
        public int TotalTasks { get { int n = 0; for (int i = 0; i < Count; i++) n += tasks[i]; return n; } }
        public double Score(int slot)
        {
            bool escaped = outcome[slot] == HorrorOutcome.Escaped || outcome[slot] == HorrorOutcome.NightComplete || outcome[slot] == HorrorOutcome.Contained;
            return tasks[slot] * 100 + (escaped ? 1000 - survival[slot] * .1 : survival[slot] * .1);
        }
        /// <summary>
        /// Competitive qualification from genuine receipts, independent of display score.
        /// Nonqualifying participants are still reported by the match adapter as losses/attempts.
        /// An unfinished escape night cannot become a victory through partial job points.
        /// </summary>
        public bool QualifiesCompetitiveResult(HorrorSport sport, int mode, int slot, bool finished,
            bool physicalExit, int objectiveCount, float roundSeconds, bool intactLoad)
        {
            if (!finished || Cooperative || slot < 0 || slot >= Count || mode < 0 || mode > 3) return false;
            bool alive = outcome[slot] != HorrorOutcome.Lost;
            bool fullNight = float.IsFinite(roundSeconds) && roundSeconds > 0 && survival[slot] + .1f >= roundSeconds;
            if (sport == HorrorSport.GraveyardShift && mode == 1) return tasks[slot] > 0;
            bool lastStanding = sport == HorrorSport.DeadEnd && mode == 3
                || (sport == HorrorSport.GraveyardShift || sport == HorrorSport.ScarecrowSeason) && mode == 2;
            if (lastStanding) return alive && (fullNight || survival[slot] > 20);
            if (sport == HorrorSport.DeadEnd && mode == 1)
                return alive && fullNight && distance[slot] > 0 && roadGates[slot] > 0;
            if (sport == HorrorSport.CursedCargo && mode == 3)
                return alive && fullNight && intactLoad && containmentStops[slot] > 0;
            if (sport == HorrorSport.CursedCargo && mode == 2)
                return outcome[slot] == HorrorOutcome.Contained && intactLoad && objectiveCount > 0 && tasks[slot] >= objectiveCount;
            if (!physicalExit) return false;
            if (sport == HorrorSport.GraveyardShift) return outcome[slot] == HorrorOutcome.NightComplete;
            // A delivery receipt was emitted only with the original attached, intact load in
            // the real receiving zone. Do not invalidate it because post-result physics drifts.
            return outcome[slot] == HorrorOutcome.Escaped;
        }
        public int Place(int slot)
        {
            if (Cooperative) return 0;
            int rank = 1; for (int i = 0; i < Count; i++) if (Score(i) > Score(slot) + .01) rank++;
            return rank;
        }
    }
}
