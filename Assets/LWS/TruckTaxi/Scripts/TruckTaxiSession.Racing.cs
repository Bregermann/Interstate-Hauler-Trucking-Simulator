using System;

namespace LWS.TruckTaxi
{
    public sealed partial class TruckTaxiSession
    {
        public int RacesEntered { get; private set; }
        public int RaceWins { get; private set; }
        public int RacePodiums { get; private set; }
        public float ThunderBowlBestLapSeconds { get; private set; }
        public long RacePrizeEarningsCents { get; private set; }
        public int RaceChaosEarned { get; private set; }
        private long pendingRaceEntryCents = -1;

        public bool TryEnterRace(long entryFeeCents)
        {
            if (pendingRaceEntryCents >= 0 || State != TruckTaxiState.Available || entryFeeCents < 0 ||
                (entryFeeCents > 0 && !TrySpend(entryFeeCents))) return false;
            pendingRaceEntryCents = entryFeeCents;
            RacesEntered++;
            Changed?.Invoke();
            return true;
        }

        public void CommitRaceEntry() => pendingRaceEntryCents = -1;

        public bool RefundRaceEntry()
        {
            if (pendingRaceEntryCents < 0) return false;
            purchaseCharges -= pendingRaceEntryCents;
            pendingRaceEntryCents = -1;
            RacesEntered--;
            Changed?.Invoke();
            return true;
        }

        public void RecordRaceResult(int position, float bestLapSeconds, long prizeCents, int chaosEarned = 0)
        {
            if (position < 1 || prizeCents < 0 || chaosEarned < 0) throw new ArgumentOutOfRangeException();
            if (position == 1) RaceWins++;
            if (position <= 3) RacePodiums++;
            if (bestLapSeconds > 0 && (ThunderBowlBestLapSeconds <= 0 || bestLapSeconds < ThunderBowlBestLapSeconds))
                ThunderBowlBestLapSeconds = bestLapSeconds;
            ShiftEarnings = checked(ShiftEarnings + prizeCents);
            RacePrizeEarningsCents = checked(RacePrizeEarningsCents + prizeCents);
            RaceChaosEarned = checked(RaceChaosEarned + chaosEarned);
            Changed?.Invoke();
        }
    }
}
