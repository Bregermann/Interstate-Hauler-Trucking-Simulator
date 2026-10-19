using System;
namespace LWS.TruckTrailerPlug
{
    public sealed class TrailerPlugRules
    {
        public const int MaximumParticipants = 4;
        public const int MaximumCrowd = 192;
        public readonly int[] score = new int[MaximumParticipants];
        public readonly int[] attempts = new int[MaximumParticipants];
        public readonly bool[] winners = new bool[MaximumParticipants];
        readonly bool[] struck = new bool[MaximumCrowd];
        public int Count { get; private set; }
        public int Rounds { get; private set; }
        public int Round { get; private set; }
        public int Participant { get; private set; }
        public int DistinctHits { get; private set; }
        public int AttemptPoints { get; private set; }
        public float Coverage { get; private set; }
        public bool Sealed { get; private set; }
        public bool Perfect { get; private set; }
        public bool Released { get; private set; }
        public bool Resolved { get; private set; }
        public bool Complete { get; private set; }
        public int hitPoints = 100, sealPoints = 5000, perfectPoints = 2000;
        public void Start(int participants, int rounds)
        {
            if (participants < 1 || participants > MaximumParticipants || rounds != 3 && rounds != 5)
                throw new ArgumentOutOfRangeException("Trailer Plug supports 1–4 participants and 3 or 5 deliveries each.");
            Count = participants; Rounds = rounds; Round = 1; Participant = 0; Complete = false;
            Array.Clear(score, 0, score.Length); Array.Clear(attempts, 0, attempts.Length);
            Array.Clear(winners, 0, winners.Length); ResetAttempt();
        }
        void ResetAttempt()
        {
            Array.Clear(struck, 0, struck.Length); DistinctHits = AttemptPoints = 0;
            Coverage = 0; Released = Resolved = Sealed = Perfect = false;
        }
        public bool Release()
        {
            if (Released || Resolved || Complete) return false;
            Released = true; return true;
        }
        public bool Hit(int pedestrian, float normalSpeed)
        {
            if (!Released || Resolved || Complete || pedestrian < 0 || pedestrian >= MaximumCrowd ||
                float.IsNaN(normalSpeed) || float.IsInfinity(normalSpeed) || normalSpeed < 2 || struck[pedestrian]) return false;
            struck[pedestrian] = true; DistinctHits++; AttemptPoints += hitPoints; return true;
        }
        public bool Resolve(float coverage, bool supported, bool stable, float centerError, float uprightDot)
        {
            if (!Released || Resolved || Complete || float.IsNaN(coverage) || float.IsInfinity(coverage)) return false;
            Coverage = supported && stable ? Math.Max(0, Math.Min(1, coverage)) : 0;
            Sealed = Coverage >= .92f;
            Perfect = Sealed && centerError <= .45f && uprightDot >= .94f;
            int landing = Sealed ? sealPoints + (Perfect ? perfectPoints : 0) :
                Coverage >= .12f ? (int)Math.Round(500 + (Coverage - .12f) / .8f * 1500) : 0;
            AttemptPoints += landing; score[Participant] += AttemptPoints; attempts[Participant]++; Resolved = true;
            return true;
        }
        public bool Next()
        {
            if (!Resolved || Complete) return false;
            Participant++;
            if (Participant >= Count) { Participant = 0; Round++; }
            if (Round > Rounds)
            {
                Complete = true; int best = int.MinValue;
                for (int i = 0; i < Count; i++) best = Math.Max(best, score[i]);
                for (int i = 0; i < Count; i++) winners[i] = score[i] == best;
                return false;
            }
            ResetAttempt(); return true;
        }
    }
}
