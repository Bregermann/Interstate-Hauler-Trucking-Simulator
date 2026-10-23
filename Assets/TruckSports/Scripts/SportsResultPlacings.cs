using System;
using System.Collections.Generic;

namespace LWS.TruckSports
{
    // Read-only result ordering for older modes that previously displayed only
    // a winner and an unsorted list. The game's declared tiebreak winner always
    // comes first; other final totals keep shared placings. No score is changed.
    public static class SportsResultPlacings
    {
        public static int FromFinalTotals(int slot, int count, IReadOnlyList<int> totals, int declaredWinner = -1)
        {
            if (totals == null || count < 1 || count > totals.Count || slot < 0 || slot >= count || declaredWinner < -1 || declaredWinner >= count)
                throw new ArgumentOutOfRangeException("Provide a valid completed roster, actual totals and the existing declared winner.");
            if (slot == declaredWinner) return 1;
            int place = declaredWinner >= 0 ? 2 : 1;
            for (int i = 0; i < count; i++) if (i != declaredWinner && totals[i] > totals[slot]) place++;
            return place;
        }
    }
}
