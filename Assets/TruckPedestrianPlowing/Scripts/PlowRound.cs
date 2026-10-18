using System;
namespace LWS.TruckPedestrianPlowing
{
    public enum PlowTargetKind { Standard, BonusRunner, PenaltyMarshal }
    // Scoring has no character-appearance inputs. Physical contacts are qualified by the match.
    public sealed class PlowRound
    {
        readonly PlowTargetKind[] kinds;
        readonly int[] owner;
        readonly int[] score, standard, bonus, penalty;
        public int Participants { get; }
        public int TargetCount => owner.Length;
        public int PositiveRemaining { get; private set; }
        public float Duration { get; }
        public float Elapsed { get; private set; }
        public bool Sealed { get; private set; }
        public int AwardCount { get; private set; }
        public bool ShouldEnd => PositiveRemaining == 0 || Elapsed >= Duration;
        public PlowRound(PlowTargetKind[] targets, int participants, float duration = 180)
        {
            if (targets == null || targets.Length == 0 || participants < 1 || participants > 8 ||
                !Finite(duration) || duration <= 0) throw new ArgumentException("Invalid Plowing round.");
            Participants = participants; Duration = duration; kinds = (PlowTargetKind[])targets.Clone();
            owner = new int[kinds.Length]; Array.Fill(owner, -1);
            score = new int[participants]; standard = new int[participants];
            bonus = new int[participants]; penalty = new int[participants];
            foreach (var kind in kinds) { if ((int)kind < 0 || (int)kind > 2) throw new ArgumentException("Invalid role."); if (Positive(kind)) PositiveRemaining++; }
        }
        public static bool Finite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
        public static bool Positive(PlowTargetKind kind) => kind != PlowTargetKind.PenaltyMarshal;
        public static int Points(PlowTargetKind kind) => kind == PlowTargetKind.BonusRunner ? 100 : kind == PlowTargetKind.PenaltyMarshal ? -100 : 10;
        public int Owner(int id) => id >= 0 && id < owner.Length ? owner[id] : -1;
        public int Score(int slot) => score[slot];
        public int StandardHits(int slot) => standard[slot];
        public int BonusHits(int slot) => bonus[slot];
        public int Penalties(int slot) => penalty[slot];
        public void Advance(float dt) { if (!Sealed && Finite(dt) && dt > 0) Elapsed = Math.Min(Duration, Elapsed + dt); }
        // Unity callbacks run on the main thread. This single owner latch is the only score mutation.
        public bool TryClaim(int id, int slot, float closingSpeed, float truckSpeed, float impactTime)
        {
            if (Sealed || PositiveRemaining == 0 || id < 0 || id >= owner.Length || slot < 0 || slot >= Participants || owner[id] >= 0 ||
                !Finite(closingSpeed) || !Finite(truckSpeed) || closingSpeed < 2 || truckSpeed < 2 ||
                !Finite(impactTime) || impactTime < 0 || impactTime >= Duration || impactTime > Elapsed + .001f) return false;
            owner[id] = slot; AwardCount++; score[slot] += Points(kinds[id]);
            switch (kinds[id]) { case PlowTargetKind.Standard: standard[slot]++; break; case PlowTargetKind.BonusRunner: bonus[slot]++; break; default: penalty[slot]++; break; }
            if (Positive(kinds[id])) PositiveRemaining--;
            return true;
        }
        public int Place(int slot) { int place = 1; for (int i = 0; i < Participants; i++) if (score[i] > score[slot]) place++; return place; }
        public void Finish() => Sealed = true;
    }
}
