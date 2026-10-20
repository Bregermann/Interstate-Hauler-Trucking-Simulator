using System;
namespace LWS.TruckBalloonBattle
{
    // Contact authority only. AI/proximity/vehicle-body bumps cannot pop balloons.
    [Serializable]
    public sealed class BalloonRules
    {
        public float telegraphSeconds = .25f, extendedSeconds = .6f, attackCooldown = 2.5f;
        public int Count { get; private set; }
        public float Clock { get; private set; }
        public bool Complete { get; private set; }
        public int Winner { get; private set; } = -1;
        public int Pops { get; private set; }
        public readonly bool[,] balloons = new bool[8, 3];
        public readonly int[] scores = new int[8], creditedPops = new int[8], eliminations = new int[8];
        readonly int[] attacks = new int[8];
        readonly float[] attackStart = new float[8];
        readonly float[] eliminatedAt = new float[8];
        readonly int[,] consumedAttack = new int[8, 8];
        public void Reset(int count)
        {
            if (count < 2 || count > 8) throw new ArgumentOutOfRangeException(nameof(count));
            Count = count; Clock = 0; Complete = false; Winner = -1; Pops = 0;
            Array.Clear(scores, 0, 8); Array.Clear(creditedPops, 0, 8); Array.Clear(eliminations, 0, 8); Array.Clear(attacks, 0, 8); Array.Clear(consumedAttack, 0, consumedAttack.Length);
            for (int i = 0; i < 8; i++) { attackStart[i] = eliminatedAt[i] = -100; for (int b = 0; b < 3; b++) balloons[i, b] = i < count; }
        }
        public int Remaining(int slot) { int total = 0; for (int b = 0; b < 3; b++) if (balloons[slot, b]) total++; return total; }
        public bool Alive(int slot) => slot >= 0 && slot < Count && Remaining(slot) > 0;
        public float Cooldown(int slot) => Math.Max(0, attackCooldown - (Clock - attackStart[slot]));
        public float AttackAge(int slot) => Clock - attackStart[slot];
        public int AttackSequence(int slot) => attacks[slot];
        public bool Extended(int slot) => slot >= 0 && slot < Count && (Alive(slot) || eliminatedAt[slot] == Clock) && attacks[slot] > 0 && AttackAge(slot) >= telegraphSeconds && AttackAge(slot) < telegraphSeconds + extendedSeconds;
        public bool Attack(int slot)
        {
            if (Complete || !Alive(slot) || Cooldown(slot) > 0) return false;
            attacks[slot]++; attackStart[slot] = Clock; return true;
        }
        public bool Pop(int attacker, int victim, int balloon, int sequence)
        {
            if (Complete || attacker == victim || !Alive(victim) || balloon < 0 || balloon > 2 || !balloons[victim, balloon] || !Extended(attacker) || sequence != attacks[attacker] || consumedAttack[attacker, victim] == sequence) return false;
            balloons[victim, balloon] = false; consumedAttack[attacker, victim] = sequence;
            Pops++; creditedPops[attacker]++; scores[attacker]++;
            if (Remaining(victim) == 0) { eliminatedAt[victim] = Clock; scores[attacker] += 3; eliminations[attacker]++; }
            return true;
        }
        public void Step(float dt)
        {
            if (dt <= 0 || float.IsNaN(dt)) throw new ArgumentOutOfRangeException(nameof(dt));
            if (Complete) return;
            Clock += dt; int alive = 0, last = -1;
            for (int i = 0; i < Count; i++) if (Alive(i)) { alive++; last = i; }
            // Resolve once after all contacts in a physical step. A simultaneous
            // mutual final pop is a draw, rather than a callback-order victory.
            if (alive <= 1) { Complete = true; Winner = last; if (last >= 0) scores[last] += 5; }
        }
    }
}
