using System;

namespace LWS.TruckKickball
{
    public enum KickballPhase { PitchReady, Pitch, Live, Walk, HomeRun, FoulReturn, Between, Complete }

    // The match supplies actual contact/crossing observations. This authority never
    // moves a truck, catches a ball, or invents a physical outcome.
    public sealed class KickballRules
    {
        public readonly int[] Scores = new int[2];
        public readonly bool[] Active = new bool[12], TagRequired = new bool[12];
        public readonly int[] Base = new int[12], ForcedTo = new int[12];
        readonly int[] startBase = new int[12], lineup = new int[2];
        readonly bool[] startActive = new bool[12];
        int pendingRuns;
        bool thirdOutWasForce;

        public int TeamSize { get; private set; }
        public int RegulationInnings { get; private set; }
        public int Inning { get; private set; }
        public int BattingTeam { get; private set; }
        public int Outs { get; private set; }
        public int Balls { get; private set; }
        public int Strikes { get; private set; }
        public int Batter { get; private set; }
        public int Pitches { get; private set; }
        public int LegalKicks { get; private set; }
        public int CompletedPlays { get; private set; }
        public int HomeRunRuns { get; private set; }
        public int PendingRuns => pendingRuns;
        public int Count => TeamSize * 2;
        public KickballPhase Phase { get; private set; }
        public bool Complete => Phase == KickballPhase.Complete;
        public int Winner => Complete ? (Scores[0] > Scores[1] ? 0 : 1) : -1;
        public int Team(int slot) => slot / TeamSize;
        public bool Runner(int slot) => slot >= 0 && slot < Count && Active[slot];

        public void Reset(int teamSize, int innings)
        {
            if (teamSize < 3 || teamSize > 6) throw new ArgumentOutOfRangeException(nameof(teamSize));
            if (innings != 1 && innings != 3 && innings != 5 && innings != 7)
                throw new ArgumentOutOfRangeException(nameof(innings));
            TeamSize = teamSize; RegulationInnings = innings; Inning = 1; BattingTeam = 0;
            Outs = Balls = Strikes = Pitches = LegalKicks = CompletedPlays = pendingRuns = HomeRunRuns = 0;
            Array.Clear(Scores, 0, 2); Array.Clear(lineup, 0, 2);
            ClearRunners(); Batter = 0; Phase = KickballPhase.PitchReady;
        }

        public bool Pitch()
        {
            if (Phase != KickballPhase.PitchReady) return false;
            Pitches++; Phase = KickballPhase.Pitch; return true;
        }

        public bool PitchPassed(bool strike)
        {
            if (Phase != KickballPhase.Pitch) return false;
            if (strike && ++Strikes >= 3)
            {
                Outs++; thirdOutWasForce = false; Phase = KickballPhase.Between;
            }
            else if (!strike && ++Balls >= 4)
            {
                Snapshot(); Active[Batter] = true; Base[Batter] = 0;
                SetForces(); Phase = KickballPhase.Walk;
            }
            else Phase = KickballPhase.PitchReady;
            return true;
        }

        public bool Kick()
        {
            if (Phase != KickballPhase.Pitch) return false;
            Snapshot(); Active[Batter] = true; Base[Batter] = 0;
            SetForces(); Phase = KickballPhase.Live; LegalKicks++; return true;
        }

        void Snapshot()
        {
            Array.Copy(Base, startBase, 12); Array.Copy(Active, startActive, 12);
            Array.Clear(TagRequired, 0, 12); Array.Clear(ForcedTo, 0, 12);
            pendingRuns = 0; thirdOutWasForce = false;
            HomeRunRuns = 0;
        }

        void SetForces()
        {
            int required = 1; ForcedTo[Batter] = 1;
            while (required <= 3)
            {
                int runner = Occupant(required);
                if (runner < 0 || runner == Batter) break;
                ForcedTo[runner] = required + 1; required++;
            }
        }

        public int Occupant(int number)
        {
            for (int i = 0; i < Count; i++) if (Active[i] && Base[i] == number) return i;
            return -1;
        }

        public bool TouchBase(int runner, int number)
        {
            if (!Runner(runner) || Team(runner) != BattingTeam) return false;
            if (Phase == KickballPhase.FoulReturn)
            {
                if (number != startBase[runner]) return false;
                TagRequired[runner] = false; return true;
            }
            if (Phase != KickballPhase.Live && Phase != KickballPhase.Walk && Phase != KickballPhase.HomeRun)
                return false;
            if (TagRequired[runner])
            {
                if (number != startBase[runner]) return false;
                TagRequired[runner] = false; return true;
            }
            if (number != Base[runner] + 1 || number > 4) return false;
            if (number < 4 && Occupant(number) >= 0 && Occupant(number) != runner) return false;
            Base[runner] = number;
            if (number >= ForcedTo[runner]) ForcedTo[runner] = 0;
            if (number == 4) { Active[runner] = false; pendingRuns++; }
            return true;
        }

        public bool Foul()
        {
            if (Phase != KickballPhase.Live) return false;
            Strikes = Math.Min(2, Strikes + 1); LegalKicks--;
            Array.Copy(startBase, Base, 12); Array.Copy(startActive, Active, 12);
            Array.Clear(ForcedTo, 0, 12); pendingRuns = 0;
            for (int i = 0; i < Count; i++) TagRequired[i] = Active[i];
            Phase = KickballPhase.FoulReturn; return true;
        }

        public bool FinishFoulReturn()
        {
            if (Phase != KickballPhase.FoulReturn) return false;
            for (int i = 0; i < Count; i++) if (Active[i] && TagRequired[i]) return false;
            Phase = KickballPhase.PitchReady; return true;
        }

        public bool FlyCaught()
        {
            if (Phase != KickballPhase.Live || !Runner(Batter)) return false;
            Remove(Batter, false);
            pendingRuns = 0;
            for (int i = 0; i < Count; i++)
                if (startActive[i])
                {
                    // A runner who crossed home before the catch still has to
                    // return and tag up. No teleport is applied to its body.
                    Active[i] = true; Base[i] = startBase[i];
                    ForcedTo[i] = 0; TagRequired[i] = true;
                }
            return true;
        }

        public int ForceAt(int number)
        {
            if (Phase != KickballPhase.Live) return -1;
            for (int i = 0; i < Count; i++)
                if (Active[i] && (ForcedTo[i] == number || TagRequired[i] && startBase[i] == number))
                { Remove(i, true); return i; }
            return -1;
        }

        public bool Tag(int runner, bool safelyOnBase)
        {
            if (Phase != KickballPhase.Live || !Runner(runner) || safelyOnBase) return false;
            Remove(runner, false); return true;
        }

        void Remove(int runner, bool force)
        {
            Active[runner] = false; ForcedTo[runner] = 0; TagRequired[runner] = false;
            Outs++; if (Outs >= 3) thirdOutWasForce = force || runner == Batter && Base[runner] < 1;
            // Removing a trailing runner breaks the force chain ahead of it.
            for (int i = 0; i < Count; i++)
                if (Active[i] && Base[i] > Base[runner] && !TagRequired[i]) ForcedTo[i] = 0;
        }

        public bool HomeRun()
        {
            if (Phase != KickballPhase.Live || Outs >= 3) return false;
            // A legal fence clearance ends defensive play immediately. Include
            // runners who already crossed home in this play, then seal credit;
            // later collisions/base callbacks cannot create outs or extra runs.
            HomeRunRuns = pendingRuns;
            for (int i = 0; i < Count; i++) if (Active[i] && Team(i) == BattingTeam)
            { HomeRunRuns++; Active[i] = false; ForcedTo[i] = 0; TagRequired[i] = false; }
            Scores[BattingTeam] += HomeRunRuns; pendingRuns = 0;
            Phase = KickballPhase.HomeRun; return true;
        }

        public bool GroundRuleDouble()
        {
            if (Phase != KickballPhase.Live) return false;
            for (int i = 0; i < Count; i++) if (Active[i]) ForcedTo[i] = Math.Min(4, Base[i] + 2);
            Phase = KickballPhase.Walk; return true;
        }

        public bool ForcedAdvanceComplete()
        {
            for (int i = 0; i < Count; i++) if (Active[i] && ForcedTo[i] > Base[i]) return false;
            return true;
        }

        public bool FinishPlay()
        {
            if (Phase != KickballPhase.Live && Phase != KickballPhase.Walk &&
                Phase != KickballPhase.HomeRun && Phase != KickballPhase.Between) return false;
            if ((Phase == KickballPhase.Walk || Phase == KickballPhase.HomeRun) && !ForcedAdvanceComplete())
                return false;
            if (Outs < 3 && FreeBatter() < 0)
            {
                // In 3-a-side play the third runner can occupy the last free
                // truck. Keep the ball live until a runner scores or is put out;
                // never conjure a ghost runner or teleport one into the lineup.
                if (Phase == KickballPhase.Walk) Phase = KickballPhase.Live;
                return false;
            }
            Scores[BattingTeam] += thirdOutWasForce ? 0 : pendingRuns;
            pendingRuns = 0; CompletedPlays++; Balls = Strikes = 0;
            if (BattingTeam == 1 && Inning >= RegulationInnings && Scores[1] > Scores[0])
            { Phase = KickballPhase.Complete; return true; }
            if (Outs >= 3)
            {
                ClearRunners(); Outs = 0;
                if (BattingTeam == 0)
                {
                    if (Inning >= RegulationInnings && Scores[1] > Scores[0])
                    { Phase = KickballPhase.Complete; return true; }
                    BattingTeam = 1;
                }
                else
                {
                    if (Inning >= RegulationInnings && Scores[0] != Scores[1])
                    { Phase = KickballPhase.Complete; return true; }
                    Inning++; BattingTeam = 0;
                }
            }
            lineup[BattingTeam] = (lineup[BattingTeam] + 1) % TeamSize;
            Batter = FreeBatter();
            if (Batter < 0) throw new InvalidOperationException("A live small-team runner must advance before the next pitch.");
            Phase = KickballPhase.PitchReady; return true;
        }

        int FreeBatter()
        {
            for (int n = 0; n < TeamSize; n++)
            {
                int slot = BattingTeam * TeamSize + (lineup[BattingTeam] + n) % TeamSize;
                if (!Active[slot]) return slot;
            }
            return -1;
        }

        void ClearRunners()
        {
            Array.Clear(Active, 0, 12); Array.Clear(TagRequired, 0, 12);
            Array.Clear(ForcedTo, 0, 12); Array.Clear(Base, 0, 12);
            pendingRuns = 0; thirdOutWasForce = false;
        }
    }
}
