using System;

namespace LWS.TruckMechanicalBull
{
    // Only measured physical contact, elapsed simulation time and observed tilt
    // enter scoring. Difficulty/AI never manufacture a successful ride.
    public sealed class MechanicalBullRules
    {
        public int Count { get; private set; }
        public int Rounds { get; private set; }
        public int Turn { get; private set; }
        public int Owner => Count > 0 ? Turn % Count : 0;
        public int Round => Count > 0 ? Turn / Count + 1 : 1;
        public bool Complete => Count > 0 && Turn >= Count * Rounds;
        public bool Resolved { get; private set; }
        public float Survival { get; private set; }
        public int Recoveries { get; private set; }
        public int Patterns { get; private set; }
        public bool Fell { get; private set; }
        public readonly int[] totals = new int[4];
        public readonly float[] best = new float[4];
        public const float RideLimit = 60;
        float stable;
        bool threatened;

        public void Begin(int count, int rounds)
        {
            if (count < 1 || count > 4 || rounds < 1 || rounds > 5) throw new ArgumentOutOfRangeException();
            Count = count; Rounds = rounds; Turn = 0;
            Array.Clear(totals, 0, totals.Length); Array.Clear(best, 0, best.Length); ResetRide();
        }
        void ResetRide() { Survival = 0; Recoveries = Patterns = 0; stable = 0; threatened = Fell = Resolved = false; }
        public bool Tick(float dt, bool deckContact, bool physicalFall, float uprightDot)
        {
            if (Count == 0 || Complete || Resolved || !float.IsFinite(dt) || dt <= 0 || dt > .25f || !float.IsFinite(uprightDot)) return false;
            if (physicalFall) { Resolve(true); return true; }
            Survival = Math.Min(RideLimit, Survival + dt);
            if (deckContact && uprightDot < .78f) { threatened = true; stable = 0; }
            if (threatened && deckContact && uprightDot > .94f)
            {
                stable += dt;
                if (stable >= .7f) { Recoveries++; threatened = false; stable = 0; }
            }
            else stable = 0;
            Patterns = (int)(Survival / 10);
            if (Survival >= RideLimit) { Resolve(false); return true; }
            return false;
        }
        void Resolve(bool fell)
        {
            if (Resolved) return;
            Fell = fell; Resolved = true;
            totals[Owner] += RidePoints;
            best[Owner] = Math.Max(best[Owner], Survival);
        }
        public int RidePoints => (int)Math.Floor(Survival * 10) + Recoveries * 25 + Patterns * 15;
        public bool Next()
        {
            if (!Resolved || Complete) return false;
            Turn++; if (!Complete) ResetRide(); return true;
        }
        public int Place(int slot)
        {
            int place = 1;
            for (int i = 0; i < Count; i++) if (totals[i] > totals[slot] || totals[i] == totals[slot] && best[i] > best[slot] + .001f) place++;
            return place;
        }
    }
}
