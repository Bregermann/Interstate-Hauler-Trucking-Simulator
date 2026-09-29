using UnityEngine;

namespace LWS.TruckTaxi
{
    public enum TruckTaxiRoadPepRating { Perfect, Good, Messy, Botched }

    // Abstract cursor-and-timing challenge. No vehicle input or physics authority lives here.
    public sealed class TruckTaxiRoadPepChallenge
    {
        public const float SteadySeconds = 2.4f;
        public const float TimingSeconds = 1.2f;
        public const float TotalSeconds = SteadySeconds + TimingSeconds;
        public float Elapsed { get; private set; }
        public float Cursor01 { get; private set; } = .5f;
        public float Target01 { get; private set; } = .5f;
        public float Marker01 => Mathf.Clamp01((Elapsed - SteadySeconds) / TimingSeconds);
        public bool TimingPhase => Elapsed >= SteadySeconds;
        public bool Finished { get; private set; }
        public int Score { get; private set; }
        public TruckTaxiRoadPepRating Rating => Score >= 90 ? TruckTaxiRoadPepRating.Perfect :
            Score >= 70 ? TruckTaxiRoadPepRating.Good : Score >= 40 ? TruckTaxiRoadPepRating.Messy : TruckTaxiRoadPepRating.Botched;
        private float steadyTime;
        private float timingQuality;
        private bool timingPressed;
        private bool majorCollision;

        public void Tick(float seconds, float cursorDelta, float vehicleDisturbance, bool timingTap)
        {
            if (Finished || !float.IsFinite(seconds) || seconds <= 0) return;
            float previous = Elapsed;
            Elapsed = Mathf.Min(TotalSeconds, Elapsed + seconds);
            Target01 = Mathf.Clamp(.5f + Mathf.Clamp(vehicleDisturbance, -1, 1) * .24f, .2f, .8f);
            Cursor01 = Mathf.Clamp01(Cursor01 + (float.IsFinite(cursorDelta) ? cursorDelta : 0));
            if (previous < SteadySeconds)
            {
                float steadyStep = Mathf.Min(Elapsed, SteadySeconds) - previous;
                float error = Mathf.Abs(Cursor01 - Target01);
                steadyTime += steadyStep * Mathf.Clamp01((.23f - error) / .16f);
            }
            if (Elapsed >= SteadySeconds && timingTap && !timingPressed)
            {
                timingPressed = true;
                timingQuality = Mathf.Clamp01(1 - Mathf.Abs(Marker01 - .62f) / .3f);
                Complete();
            }
            else if (Elapsed >= TotalSeconds) Complete();
        }

        public void ReportCollision(float relativeSpeed)
        {
            if (!Finished && float.IsFinite(relativeSpeed) && relativeSpeed >= 6f) majorCollision = true;
        }

        public void ForceFinish(int score)
        {
            if (Finished) return;
            Score = Mathf.Clamp(score, 0, 100);
            Finished = true;
        }

        private void Complete()
        {
            Score = Mathf.Clamp(Mathf.RoundToInt(65 * steadyTime / SteadySeconds + 35 * timingQuality), 0, 100);
            if (majorCollision) Score = Mathf.Min(Score, 25);
            Finished = true;
        }
    }
}
