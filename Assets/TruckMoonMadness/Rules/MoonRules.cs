using System;
using System.Linq;

namespace LWS.TruckMoonMadness
{
    // Physical observations are supplied by the mode's sole runtime authority.
    public sealed class MoonRules
    {
        public readonly int[] Scores = new int[8], Owners;
        public readonly int Target;
        public bool Complete { get; private set; }
        public int Deliveries { get; private set; }
        public MoonRules(int flags, int target)
        {
            if (flags < 1 || flags > 8 || target < 1) throw new ArgumentOutOfRangeException();
            Target = target; Owners = Enumerable.Repeat(-1, flags).ToArray();
        }
        public int Carried(int driver) => Array.IndexOf(Owners, driver);
        public bool Collect(int flag, int driver, bool physicalContact)
        {
            if (Complete || !physicalContact || flag < 0 || flag >= Owners.Length || driver < 0 || driver >= 8 || Owners[flag] != -1 || Carried(driver) >= 0) return false;
            Owners[flag] = driver; return true;
        }
        public bool Drop(int flag)
        {
            if (Complete || flag < 0 || flag >= Owners.Length || Owners[flag] < 0) return false;
            Owners[flag] = -1; return true;
        }
        public bool Deliver(int flag, int driver, bool actualOwnStationRest)
        {
            if (Complete || !actualOwnStationRest || driver < 0 || driver >= 8 || flag < 0 || flag >= Owners.Length || Owners[flag] != driver) return false;
            Owners[flag] = -2; Scores[driver]++; Deliveries++;
            Complete = Scores[driver] >= Target; return true;
        }
        public bool Respawn(int flag)
        {
            if (Complete || flag < 0 || flag >= Owners.Length || Owners[flag] != -2) return false;
            Owners[flag] = -1; return true;
        }
        public void FinishAtTimeLimit() => Complete = true;
        public int[] Leaders() { int best = Scores.Max(); return Enumerable.Range(0, 8).Where(i => Scores[i] == best).ToArray(); }
    }
}
