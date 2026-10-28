using System;

namespace LWS.TruckDanceOff
{
    public enum DanceGrade { None, Perfect, Great, Good, Miss }
    [Serializable]
    public struct DanceNote
    {
        public double time, duration;
        public int mask;
        public DanceNote(double at, int lanes, double hold = 0) { time = at; mask = lanes; duration = hold; }
    }

    // Absolute song timestamps supplied by the DSP/input adapter. Neither frame
    // rate nor a truck animation is allowed to decide a rhythm judgment.
    public sealed class DanceRules
    {
        public const double PerfectWindow = .045, GreatWindow = .09, GoodWindow = .16, HoldGrace = .08;
        readonly double greatWindow,goodWindow;
        public double AcceptedGreatWindow=>greatWindow;
        public double AcceptedGoodWindow=>goodWindow;
        readonly DanceNote[] notes;
        readonly byte[] state;
        readonly int[] partial;
        readonly DanceGrade[] grade;
        readonly double[] firstPress, missingSince;
        double lastClock = double.NegativeInfinity;
        long lastFreestyleTick = long.MinValue;
        int previousFreestyleMask, repeatedFreestyle;
        public int Score { get; private set; }
        public int Combo { get; private set; }
        public int MaximumCombo { get; private set; }
        public int Perfect { get; private set; }
        public int Great { get; private set; }
        public int Good { get; private set; }
        public int Miss { get; private set; }
        public int EmptyPresses { get; private set; }
        public int FreestyleHits { get; private set; }
        public int Resolved { get; private set; }
        public int Count => notes.Length;
        public DanceGrade LastGrade { get; private set; }
        public event Action<int, DanceGrade> Judged;
        // Presentation-only head event; hold scoring still resolves at its tail.
        public event Action<int, DanceGrade> Started;
        public event Action<int> Freestyled;

        public DanceRules(DanceNote[] chart) : this(chart,GreatWindow,GoodWindow) { }
        public DanceRules(DanceNote[] chart,double greatTolerance,double goodTolerance)
        {
            if (chart == null) throw new ArgumentNullException(nameof(chart));
            if(double.IsNaN(greatTolerance)||double.IsInfinity(greatTolerance)||double.IsNaN(goodTolerance)||double.IsInfinity(goodTolerance)||greatTolerance<PerfectWindow||goodTolerance<greatTolerance||goodTolerance>.35)throw new ArgumentException("Dance timing windows must be finite, ordered PERFECT <= GREAT <= GOOD, and GOOD <= 350ms.");
            greatWindow=greatTolerance;goodWindow=goodTolerance;
            notes = (DanceNote[])chart.Clone(); state = new byte[notes.Length]; partial = new int[notes.Length];
            grade = new DanceGrade[notes.Length]; firstPress = new double[notes.Length]; missingSince = new double[notes.Length];
            for (int i = 0; i < notes.Length; i++)
            {
                var note = notes[i];
                if (double.IsNaN(note.time) || double.IsInfinity(note.time) || note.time < 0 ||
                    double.IsNaN(note.duration) || double.IsInfinity(note.duration) || note.duration < 0 ||
                    note.mask < 1 || note.mask > 15 || i > 0 && note.time < notes[i - 1].time)
                    throw new ArgumentException("Dance notes require sorted finite song times, nonnegative holds and lanes 1–15.");
                firstPress[i] = missingSince[i] = double.NaN;
            }
        }

        public DanceNote Note(int index) => notes[index];
        public bool Pending(int index) => state[index] < 2;
        public bool Holding(int index) => state[index] == 1;
        public DanceGrade Judgment(int index) => grade[index];

        public void Step(double songTime, int presses, int held)
        {
            if (double.IsNaN(songTime) || double.IsInfinity(songTime)) throw new ArgumentException("A finite DSP song time is required.");
            if (songTime + .000001 < lastClock) throw new InvalidOperationException("Rhythm judgment cannot rewind; replay is presentation only.");
            lastClock = songTime; presses &= 15; held &= 15;
            for (int i = 0; i < notes.Length; i++)
            {
                if (state[i] >= 2) continue;
                var note = notes[i];
                if (state[i] == 0 && songTime - note.time > goodWindow + .0000001) { Resolve(i, DanceGrade.Miss); continue; }
                if (state[i] != 1) continue;
                if (songTime >= note.time + note.duration - .055) { Resolve(i, grade[i]); continue; }
                if ((held & note.mask) == note.mask) missingSince[i] = double.NaN;
                else if (double.IsNaN(missingSince[i])) missingSince[i] = songTime;
                else if (songTime - missingSince[i] > HoldGrace) Resolve(i, DanceGrade.Miss);
            }
            for (int lane = 1; lane <= 8; lane <<= 1)
            {
                if ((presses & lane) == 0) continue;
                int best = -1; double error = goodWindow + .0000001;
                for (int i = 0; i < notes.Length; i++)
                {
                    if (state[i] != 0 || (notes[i].mask & lane) == 0 || (partial[i] & lane) != 0) continue;
                    double difference = Math.Abs(songTime - notes[i].time);
                    if (difference < error) { error = difference; best = i; }
                }
                if (best < 0)
                {
                    bool ownedByHold = false;
                    for (int i = 0; i < notes.Length; i++) if (state[i] == 1 && (notes[i].mask & lane) != 0) ownedByHold = true;
                    if (ownedByHold) continue;
                    EmptyPresses++; Combo = 0; Score = Math.Max(0, Score - 20); continue;
                }
                if (double.IsNaN(firstPress[best])) firstPress[best] = songTime;
                else if (songTime - firstPress[best] > HoldGrace) { Resolve(best, DanceGrade.Miss); continue; }
                DanceGrade value = error <= PerfectWindow + .0000001 ? DanceGrade.Perfect : error <= greatWindow + .0000001 ? DanceGrade.Great : DanceGrade.Good;
                if (value > grade[best]) grade[best] = value;
                partial[best] |= lane;
                if (partial[best] != notes[best].mask) continue;
                Started?.Invoke(best, grade[best]);
                if (notes[best].duration > 0) state[best] = 1;
                else Resolve(best, grade[best]);
            }
        }

        public bool Freestyle(double songTime, double beatSeconds, int mask, double start, double end)
        {
            if (songTime < start || songTime >= end || beatSeconds <= 0 || mask < 1 || mask > 15) return false;
            double grid = songTime / (beatSeconds * .5); long tick = (long)Math.Round(grid);
            if (Math.Abs(grid - tick) * beatSeconds * .5 > goodWindow || tick == lastFreestyleTick) return false;
            lastFreestyleTick = tick;
            repeatedFreestyle = mask == previousFreestyleMask ? repeatedFreestyle + 1 : 0;
            previousFreestyleMask = mask;
            if (repeatedFreestyle >= 2) return false;
            FreestyleHits++; Score += 75 + Bits(mask) * 25; Freestyled?.Invoke(mask); return true;
        }

        void Resolve(int index, DanceGrade result)
        {
            if (state[index] >= 2) return;
            state[index] = (byte)(result == DanceGrade.Miss ? 3 : 2); grade[index] = result; Resolved++; LastGrade = result;
            switch (result)
            {
                case DanceGrade.Perfect: Perfect++; break;
                case DanceGrade.Great: Great++; break;
                case DanceGrade.Good: Good++; break;
                default: Miss++; Combo = 0; Judged?.Invoke(index, result); return;
            }
            Combo++; MaximumCombo = Math.Max(MaximumCombo, Combo);
            int points = result == DanceGrade.Perfect ? 1000 : result == DanceGrade.Great ? 700 : 400;
            Score += points * Bits(notes[index].mask) + (int)Math.Round(notes[index].duration * 100);
            Judged?.Invoke(index, result);
        }

        static int Bits(int value) { int count = 0; while (value != 0) { value &= value - 1; count++; } return count; }
    }
}
