using System;
using UnityEngine;

namespace LWS.TruckRedLightGreenLight
{
    public enum RedLightPhase { Countdown, Green, LaughWarning, Red, Complete }
    public enum RedLightOutcome { Racing, Finished, Eliminated }

    // One rule authority. Rendering, AI and physics report observations here;
    // neither an animation callback nor a difficulty roll can eliminate a truck.
    [Serializable]
    public sealed class RedLightRules
    {
        public float warningSeconds = 1, jitterRadius = .045f, movementSpeed = .12f;
        public float stationaryDriftSpeed = .025f;
        public RedLightPhase Phase { get; private set; }
        public float PhaseTime { get; private set; }
        public float Elapsed { get; private set; }
        public int Winner { get; private set; } = -1;
        public int Count { get; private set; }
        public int Laughs { get; private set; }
        public readonly RedLightOutcome[] outcomes = new RedLightOutcome[12];
        public readonly float[] finishTimes = new float[12];
        public readonly string[] reasons = new string[12];
        readonly Vector2[] redAnchor = new Vector2[12];
        readonly Vector2[] pushedVelocity = new Vector2[12];
        readonly bool[] anchored = new bool[12];
        System.Random random;
        float duration, firstFinish;

        public void Reset(int count, int seed)
        {
            if (count < 1 || count > 12) throw new ArgumentOutOfRangeException(nameof(count));
            Count = count; random = new System.Random(seed); Phase = RedLightPhase.Countdown;
            PhaseTime = Elapsed = 0; duration = 3; Winner = -1; Laughs = 0; firstFinish = -1;
            Array.Clear(outcomes, 0, outcomes.Length); Array.Clear(finishTimes, 0, finishTimes.Length);
            Array.Clear(reasons, 0, reasons.Length); Array.Clear(pushedVelocity, 0, pushedVelocity.Length);
            Array.Clear(anchored, 0, anchored.Length);
        }

        public void Advance(float dt)
        {
            if (dt < 0 || float.IsNaN(dt)) throw new ArgumentOutOfRangeException(nameof(dt));
            if (Phase == RedLightPhase.Complete) return;
            Elapsed += dt; PhaseTime += dt;
            while (PhaseTime >= duration && Phase != RedLightPhase.Complete)
            {
                PhaseTime -= duration;
                if (Phase == RedLightPhase.Countdown || Phase == RedLightPhase.Red)
                { Phase = RedLightPhase.Green; duration = 4 + (float)random.NextDouble() * 4; }
                else if (Phase == RedLightPhase.Green)
                { Phase = RedLightPhase.LaughWarning; duration = warningSeconds; Laughs++; }
                else
                { Phase = RedLightPhase.Red; duration = 2 + (float)random.NextDouble() * 2; Array.Clear(anchored, 0, anchored.Length); }
            }
            bool racing = false;
            for (int i = 0; i < Count; i++) racing |= outcomes[i] == RedLightOutcome.Racing;
            if (!racing || firstFinish >= 0 && Elapsed - firstFinish >= 10)
                Phase = RedLightPhase.Complete;
        }

        public void ExternalPush(int slot, Vector2 receivingVelocityChange)
        {
            if (slot < 0 || slot >= Count || outcomes[slot] != RedLightOutcome.Racing) return;
            // A measured contact impulse is a velocity budget, never blanket
            // collision immunity. It cannot excuse steering/throttle-driven travel.
            pushedVelocity[slot] = Vector2.ClampMagnitude(pushedVelocity[slot] + receivingVelocityChange, 30);
        }

        public bool Observe(int slot, Vector2 position, Vector2 previous, Vector2 velocity,
            float forwardSpeed, float throttle, bool grounded, float dt)
        {
            if (outcomes[slot] != RedLightOutcome.Racing) return false;
            Vector2 delta = position - previous;
            bool driven = throttle > .15f && forwardSpeed > .015f || throttle < -.15f && forwardSpeed < -.015f;
            Vector2 pushed = pushedVelocity[slot];
            bool externallyMoved = !driven && pushed.sqrMagnitude > .01f &&
                Vector2.Dot(delta, pushed.normalized) >= delta.magnitude * .7f &&
                delta.magnitude <= pushed.magnitude * dt * 1.25f + .008f;
            // Remove contact budget as braking/friction remove actual motion.
            pushedVelocity[slot] = velocity.sqrMagnitude < .01f ? Vector2.zero :
                Vector2.ClampMagnitude(pushed * Mathf.Exp(-dt * .65f), velocity.magnitude);
            if (Phase != RedLightPhase.Red) return false;
            if (!anchored[slot] || externallyMoved) { redAnchor[slot] = previous; anchored[slot] = true; }
            if (externallyMoved) { redAnchor[slot] = position; return false; }
            if (!grounded || delta.magnitude < .0001f) return false;
            if ((position - redAnchor[slot]).magnitude <= jitterRadius) return false;
            if (!driven && velocity.magnitude < stationaryDriftSpeed && delta.magnitude / Mathf.Max(.001f, dt) < stationaryDriftSpeed) return false;
            // No second hidden grace period after the audible one-second warning.
            return Eliminate(slot, driven ? "DRIVING ON RED" : "STILL ROLLING ON RED");
        }

        public bool Eliminate(int slot, string reason)
        {
            if (outcomes[slot] != RedLightOutcome.Racing) return false;
            outcomes[slot] = RedLightOutcome.Eliminated; reasons[slot] = reason; return true;
        }

        public bool Finish(int slot, float crossingTime)
        {
            if (Phase == RedLightPhase.Countdown || Phase == RedLightPhase.Complete || outcomes[slot] != RedLightOutcome.Racing) return false;
            outcomes[slot] = RedLightOutcome.Finished; finishTimes[slot] = crossingTime;
            if (Winner < 0 || crossingTime < finishTimes[Winner]) { Winner = slot; firstFinish = Elapsed; }
            return true;
        }

        public static bool WholeBodyCrossing(float previousRear, float currentRear, float finish, out float fraction)
        {
            fraction = 0;
            if (currentRear < finish || previousRear >= finish || currentRear <= previousRear) return false;
            fraction = Mathf.Clamp01((finish - previousRear) / (currentRear - previousRear)); return true;
        }
    }
}
