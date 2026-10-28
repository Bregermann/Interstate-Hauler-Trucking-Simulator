using System;
using System.Collections.Generic;

namespace LWS.TruckPinataPanic
{
    public enum PinataPhase { Ready, Striking, Candy, Intermission, Complete }
    public enum PinataCandyKind { Normal, Golden, Explosive }

    // Candy/score/round authority only. CoreCollapse owns every point of shell health.
    [Serializable]
    public sealed class PinataRules
    {
        public const int Capacity = 4, CandyCapacity = 48;
        public readonly int[] points = new int[Capacity], collected = new int[Capacity], damagePoints = new int[Capacity];
        readonly bool[] available = new bool[CandyCapacity], released = new bool[CandyCapacity];
        readonly PinataCandyKind[] kinds = new PinataCandyKind[CandyCapacity];
        readonly HashSet<int> hits = new HashSet<int>();
        public PinataPhase Phase { get; private set; }
        public int Count { get; private set; }
        public int Round { get; private set; }
        public int Rounds { get; private set; }
        public float Clock { get; private set; }
        public float PhaseSeconds { get; private set; }
        public int Ruptures { get; private set; }
        public int RemainingCandy { get; private set; }
        public int Winner { get; private set; } = -1;
        public bool Complete => Phase == PinataPhase.Complete;
        public void Reset(int count, int rounds)
        {
            Count = Math.Clamp(count, 1, Capacity); Rounds = Math.Clamp(rounds, 1, 7);
            Array.Clear(points, 0, points.Length); Array.Clear(collected, 0, collected.Length); Array.Clear(damagePoints, 0, damagePoints.Length);
            Round = 0; Clock = 0; Ruptures = 0; Winner = -1; BeginRound();
        }
        void BeginRound() { Phase = PinataPhase.Ready; PhaseSeconds = 3; RemainingCandy = 0; hits.Clear(); Array.Clear(available, 0, available.Length); Array.Clear(released, 0, released.Length); }
        public void Tick(float dt)
        {
            if (Complete || !float.IsFinite(dt) || dt <= 0) return;
            dt = Math.Min(dt, .1f); Clock += dt; PhaseSeconds = Math.Max(0, PhaseSeconds - dt);
            if (PhaseSeconds > 0) return;
            if (Phase == PinataPhase.Ready) { Phase = PinataPhase.Striking; PhaseSeconds = 55; }
            else if (Phase == PinataPhase.Striking || Phase == PinataPhase.Candy) { Phase = PinataPhase.Intermission; PhaseSeconds = 3; Array.Clear(available, 0, available.Length); RemainingCandy = 0; }
            else if (Phase == PinataPhase.Intermission)
            {
                if (++Round < Rounds) BeginRound();
                else { Phase = PinataPhase.Complete; Round = Rounds - 1; Winner = 0; for (int i = 1; i < Count; i++) if (points[i] > points[Winner]) Winner = i; }
            }
        }
        public int BankDamage(int round, int hit, int slot, float actualHealthLost)
        {
            // The final real impact may have just changed Striking -> Candy.
            if ((Phase != PinataPhase.Striking && Phase != PinataPhase.Candy) || round != Round || slot < 0 || slot >= Count || hit < 0 || !float.IsFinite(actualHealthLost) || actualHealthLost < 1 || !hits.Add(hit)) return 0;
            int value = Math.Clamp((int)Math.Ceiling(actualHealthLost / 80), 1, 6);
            points[slot] += value; damagePoints[slot] += value; return value;
        }
        public bool Rupture()
        {
            if (Phase != PinataPhase.Striking) return false;
            Phase = PinataPhase.Candy; PhaseSeconds = 24; Ruptures++; return true;
        }
        public bool Release(int round, int id, PinataCandyKind kind)
        {
            if (Phase != PinataPhase.Candy || round != Round || id < 0 || id >= CandyCapacity || released[id]) return false;
            released[id] = available[id] = true; kinds[id] = kind; RemainingCandy++; return true;
        }
        public bool Available(int round, int id) => Phase == PinataPhase.Candy && round == Round && id >= 0 && id < CandyCapacity && available[id];
        public bool Collect(int round, int id, int slot, out int delta)
        {
            delta = 0;
            if (slot < 0 || slot >= Count || !Available(round, id)) return false;
            available[id] = false; RemainingCandy--;
            int value = kinds[id] == PinataCandyKind.Golden ? 50 : kinds[id] == PinataCandyKind.Explosive ? -15 : 10;
            int previous = points[slot]; points[slot] = Math.Max(0, previous + value); delta = points[slot] - previous;
            if (kinds[id] != PinataCandyKind.Explosive) collected[slot]++;
            return true;
        }
        public int Place(int slot) { int place = 1; for (int i = 0; i < Count; i++) if (points[i] > points[slot]) place++; return place; }
        public bool TiedWinner { get { if (!Complete) return false; for (int i = 0; i < Count; i++) if (i != Winner && points[i] == points[Winner]) return true; return false; } }
    }
}
