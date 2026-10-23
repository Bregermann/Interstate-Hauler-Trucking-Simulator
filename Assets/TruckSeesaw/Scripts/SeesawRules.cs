using System;
namespace LWS.TruckSeesaw
{
    public enum SeesawAwardKind { Survival, Knockoff, Balance, HeatWinner }
    // Only actual physics observations reach this authority. Presentation and
    // AI cannot eliminate opponents or manufacture a balancing recovery.
    [Serializable]
    public sealed class SeesawRules
    {
        public int Count { get; private set; }
        public int Heat { get; private set; }
        public int HeatCount { get; private set; }
        public int HeatWinner { get; private set; }
        public float Clock { get; private set; }
        public float HeatSeconds { get; private set; }
        public bool HeatComplete { get; private set; }
        public bool Complete { get; private set; }
        public readonly bool[] alive = new bool[4];
        public readonly int[] scores = new int[4], knockoffs = new int[4], recoveries = new int[4], heatWins = new int[4];
        public readonly float[] survival = new float[4];
        public event Action<SeesawAwardKind, string, int, int, int> Awarded;
        void Award(SeesawAwardKind kind, string id, int slot, int amount)
        { if (amount == 0) return; scores[slot] += amount; Awarded?.Invoke(kind, id, slot, amount, scores[slot]); }
        readonly int[] hitter = new int[4], survivalAwards = new int[4];
        readonly float[] hitTime = new float[4];
        readonly bool[] recoveryLatched = new bool[4];
        public void Reset(int count, int heats, float heatSeconds = 90)
        {
            if (count < 1 || count > 4 || heats < 1 || heatSeconds < 15 || float.IsNaN(heatSeconds) || float.IsInfinity(heatSeconds)) throw new ArgumentOutOfRangeException();
            Count = count; HeatCount = heats; HeatSeconds = heatSeconds; Heat = 0; Complete = false;
            Array.Clear(scores,0,4); Array.Clear(knockoffs,0,4); Array.Clear(recoveries,0,4); Array.Clear(heatWins,0,4); StartHeat();
        }
        void StartHeat()
        {
            Clock = 0; HeatComplete = false; HeatWinner = -1;
            for (int i = 0; i < 4; i++) { alive[i] = i < Count; hitter[i] = -1; hitTime[i] = -100; survival[i] = 0; survivalAwards[i] = 0; recoveryLatched[i] = false; }
        }
        public bool NextHeat()
        { if (!HeatComplete || Complete) return false; Heat++; StartHeat(); return true; }
        public void RecordPush(int attacker, int victim, float incomingNormalSpeed)
        {
            if (HeatComplete || attacker < 0 || victim < 0 || attacker >= Count || victim >= Count || attacker == victim || !alive[attacker] || !alive[victim] || incomingNormalSpeed < 2 || float.IsNaN(incomingNormalSpeed) || float.IsInfinity(incomingNormalSpeed)) return;
            hitter[victim] = attacker; hitTime[victim] = Clock;
        }
        public void Step(float dt, int actualFallsMask, bool[] physicalBalanceRecoveries)
        {
            if (dt <= 0 || float.IsNaN(dt) || float.IsInfinity(dt) || physicalBalanceRecoveries == null || physicalBalanceRecoveries.Length < Count) throw new ArgumentException("A finite step and actual per-truck recovery observations are required.");
            if (HeatComplete) return; float elapsed = Math.Min(dt, HeatSeconds - Clock); Clock += elapsed;
            for (int i = 0; i < Count; i++) if (alive[i])
            {
                survival[i] += elapsed; int awards = (int)(survival[i] / 15); if (awards != survivalAwards[i]) Award(SeesawAwardKind.Survival, "heat:" + Heat + ":survival:" + i + ":" + awards, i, awards - survivalAwards[i]); survivalAwards[i] = awards;
                if ((actualFallsMask & (1 << i)) != 0)
                {
                    alive[i] = false; int credit = hitter[i];
                    if (credit >= 0 && Clock - hitTime[i] <= 5) { knockoffs[credit]++; Award(SeesawAwardKind.Knockoff, "heat:" + Heat + ":knockoff:" + i, credit, 3); }
                }
                else if (physicalBalanceRecoveries[i] && !recoveryLatched[i]) { recoveries[i]++; Award(SeesawAwardKind.Balance, "heat:" + Heat + ":balance:" + i + ":" + recoveries[i], i, 2); }
                recoveryLatched[i] = physicalBalanceRecoveries[i];
            }
            // All falls in the same physical sample resolve before sole-survivor
            // rewards, so simultaneous final falls cannot create a false winner.
            int remaining = 0, survivor = -1; for (int i = 0; i < Count; i++) if (alive[i]) { remaining++; survivor = i; }
            if (remaining == 0 || Count > 1 && remaining == 1 || Clock >= HeatSeconds)
            {
                HeatComplete = true; Complete = Heat + 1 >= HeatCount;
                if (remaining == 1) { HeatWinner = survivor; heatWins[survivor]++; Award(SeesawAwardKind.HeatWinner, "heat:" + Heat + ":winner:" + survivor, survivor, 5); }
            }
        }
        public int Place(int slot) { int rank=1; for (int i=0;i<Count;i++) if (scores[i]>scores[slot]) rank++; return rank; }
    }
}
