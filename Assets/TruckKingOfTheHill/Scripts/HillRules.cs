using System;
namespace LWS.TruckKingOfTheHill
{
    [Serializable]
    public sealed class HillRules
    {
        public int Count { get; private set; }
        public int ZoneCount { get; private set; }
        public int Zone { get; private set; }
        public int NextZone { get; private set; }
        public int Moves { get; private set; }
        public int Occupants { get; private set; }
        public int Controller { get; private set; }
        public int Winner { get; private set; }
        public float Clock { get; private set; }
        public float Target { get; private set; }
        public float Period { get; private set; }
        public bool Complete { get; private set; }
        public readonly float[] control = new float[8];
        Random random;
        public float UntilMove => Math.Max(0, (Moves + 1) * Period - Clock);
        public bool Warning => !Complete && UntilMove <= 3;
        public void Reset(int count, int zones, int seed, float target = 60, float period = 25)
        {
            if (count < 2 || count > 8 || zones < 2 || target <= 0 || period <= 3 || float.IsNaN(target) || float.IsInfinity(target) || float.IsNaN(period) || float.IsInfinity(period)) throw new ArgumentOutOfRangeException();
            Count = count; ZoneCount = zones; Target = target; Period = period; random = new Random(seed); Zone = 0; NextZone = Pick(); Moves = Occupants = 0; Controller = Winner = -1; Clock = 0; Complete = false; Array.Clear(control, 0, 8);
        }
        int Pick() { int value = random.Next(ZoneCount - 1); return value >= Zone ? value + 1 : value; }
        public void Step(float dt, bool[] present, bool[] eligible)
        {
            if (dt <= 0 || float.IsNaN(dt) || float.IsInfinity(dt) || present == null || eligible == null || present.Length < Count || eligible.Length < Count) throw new ArgumentException("Provide a finite step and one physical occupancy/eligibility sample per truck.");
            if (Complete) return;
            Occupants = 0; Controller = -1;
            for (int i = 0; i < Count; i++) if (present[i]) { Occupants++; Controller = i; }
            if (Occupants != 1 || !eligible[Controller]) Controller = -1;
            float sampledTime = Math.Min(dt, UntilMove);
            if (Controller >= 0)
            {
                float earned = Math.Min(sampledTime, Target - control[Controller]); control[Controller] += earned;
                if (control[Controller] >= Target) { Clock += earned; Winner = Controller; Complete = true; return; }
            }
            Clock += dt;
            // Never credit the new location using a physical sample taken at the
            // previous location. It requires a fresh sample in the next step.
            while (Clock >= (Moves + 1) * Period) { Zone = NextZone; NextZone = Pick(); Moves++; Controller = -1; Occupants = 0; }
        }
        public int Place(int slot) { int place = 1; for (int i = 0; i < Count; i++) if (control[i] > control[slot] + .0001f) place++; return place; }
    }
}
