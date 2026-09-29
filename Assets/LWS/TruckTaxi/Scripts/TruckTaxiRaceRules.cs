using System;
using NWH.VehiclePhysics2.Input;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public enum TruckTaxiRaceMode { Practice, TimeTrial, TruckRace, Elimination, ChaosRace, EventRace }
    public enum TruckTaxiRacePersonality { Cautious, Normal, Aggressive, Reckless }

    public static class TruckTaxiRaceGrid
    {
        // Slot zero belongs to the player; opponents use slots one through seven.
        public static int Lane(int slot) => slot % 2;
        public static int Index(int pointCount, int slot) => Mathf.Clamp(pointCount - 3 - slot / 2 * 3, 1, pointCount - 2);
        public static Vector3 LanePoint(Vector3[] centerline, int index, int lane)
        {
            Vector3 tangent = centerline[(index + 1) % centerline.Length] -
                centerline[(index + centerline.Length - 1) % centerline.Length];
            tangent.y = 0; tangent.Normalize();
            Vector3 point = centerline[index] + new Vector3(-tangent.z, 0, tangent.x) * (lane == 0 ? -5f : 5f);
            point.y += TruckTaxiSpeedwayLayout.BankOffset(point, centerline);
            return point;
        }
    }

    // Temporary start-line restraint, not a vehicle controller.
    public sealed class TruckTaxiRaceCountdownLock : IDisposable
    {
        private Rigidbody body;
        private readonly RigidbodyConstraints constraints;
        private readonly VehicleInputHandler input;
        private readonly VehicleInputStates states;
        private readonly bool autoSetInput;
        private readonly Vector3 position;
        private readonly Quaternion rotation;
        private bool disposed;

        public TruckTaxiRaceCountdownLock(Rigidbody body, VehicleInputHandler input = null)
        {
            this.body = body;
            this.input = input;
            constraints = body.constraints;
            position = body.position; rotation = body.rotation;
            if (input != null)
            {
                states = input.states; autoSetInput = input.autoSetInput;
                input.autoSetInput = false;
                input.inputModifyCallback.AddListener(HoldInput);
            }
            body.constraints = RigidbodyConstraints.FreezeAll;
            Hold();
        }

        private void HoldInput()
        {
            if (input == null) return;
            input.states.Reset();
            input.Brakes = input.Handbrake = 1;
        }

        public void Hold()
        {
            if (body == null) return;
            TruckTaxiRecoverySpace.Move(body, position, rotation);
            HoldInput();
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (body != null) body.constraints = constraints;
            body = null;
            if (input == null) return;
            input.inputModifyCallback.RemoveListener(HoldInput);
            input.autoSetInput = autoSetInput;
            input.states = states;
        }
    }

    [Serializable]
    public sealed class TruckTaxiRaceSettings
    {
        [Range(3, 7)] public int normalOpponents = 5;
        [Range(3, 7)] public int eventOpponents = 7;
        [Min(0)] public long entryFeeCents = 500;
        public long[] normalPrizesCents = { 5000, 2500, 1000 };
        public long[] eventPrizesCents = { 10000, 5000, 2500 };
        [Min(10)] public float goldLapSeconds = 64;
        [Min(10)] public float silverLapSeconds = 72;
        [Min(10)] public float bronzeLapSeconds = 82;
        [Min(1)] public float resetPenaltySeconds = 5;
        [Min(1)] public float checkpointHalfWidthMeters = 18;
        [Min(1)] public float maximumStepMeters = 35;

        public int Laps(TruckTaxiRaceMode mode, int timeTrialLaps) =>
            mode == TruckTaxiRaceMode.Practice ? 0 : mode == TruckTaxiRaceMode.TimeTrial ?
            Mathf.Clamp(timeTrialLaps, 1, 5) : mode == TruckTaxiRaceMode.EventRace ? 5 : 3;

        public long Prize(TruckTaxiRaceMode mode, int place, float bestLap)
        {
            if (mode == TruckTaxiRaceMode.Practice) return 0;
            if (mode == TruckTaxiRaceMode.TimeTrial)
                return bestLap <= 0 ? 0 : bestLap <= goldLapSeconds ? 3000 : bestLap <= silverLapSeconds ? 1800 :
                    bestLap <= bronzeLapSeconds ? 800 : 0;
            var prizes = mode == TruckTaxiRaceMode.EventRace ? eventPrizesCents : normalPrizesCents;
            return place > 0 && prizes != null && place <= prizes.Length ? Math.Max(0, prizes[place - 1]) : 0;
        }

        public string Medal(float bestLap) => bestLap <= 0 ? "--" : bestLap <= goldLapSeconds ? "GOLD" :
            bestLap <= silverLapSeconds ? "SILVER" : bestLap <= bronzeLapSeconds ? "BRONZE" : "--";

        public static TruckTaxiRacePersonality Personality(int gridIndex) =>
            gridIndex % 7 == 0 ? TruckTaxiRacePersonality.Cautious :
            gridIndex % 7 == 4 ? TruckTaxiRacePersonality.Reckless :
            gridIndex % 7 == 3 || gridIndex % 7 == 6 ? TruckTaxiRacePersonality.Aggressive :
            TruckTaxiRacePersonality.Normal;

        public static float SpeedScale(TruckTaxiRacePersonality personality) =>
            personality == TruckTaxiRacePersonality.Cautious ? .78f :
            personality == TruckTaxiRacePersonality.Aggressive ? 1f :
            personality == TruckTaxiRacePersonality.Reckless ? 1.08f : .9f;
    }

    // Directed gates reject reverse travel, skips, off-track cuts, and single-frame teleports.
    public sealed class TruckTaxiRaceProgress
    {
        private readonly Vector3[] gates;
        private readonly Vector3[] forwards;
        private readonly TruckTaxiRaceSettings settings;
        private int next;
        private bool hasPrevious, lapArmed;
        private Vector3 previous;
        private float lapStart, lastGateTime;
        public int CompletedLaps { get; private set; }
        public int NextCheckpoint => next;
        public int PassedCheckpoints { get; private set; }
        public float BestLapSeconds { get; private set; }
        public float LastLapSeconds { get; private set; }
        public float CurrentLapSeconds(float now) => hasPrevious ? Mathf.Max(0, now - lapStart) : 0;

        public TruckTaxiRaceProgress(Vector3[] checkpoints, TruckTaxiRaceSettings settings)
        {
            if (checkpoints == null || checkpoints.Length < 3) throw new ArgumentException("At least three checkpoints are required.");
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            gates = checkpoints;
            forwards = new Vector3[gates.Length];
            for (int i = 0; i < gates.Length; i++)
            {
                Vector3 direction = gates[(i + 1) % gates.Length] - gates[(i + gates.Length - 1) % gates.Length];
                direction.y = 0;
                forwards[i] = direction.normalized;
            }
        }

        public void Begin(Vector3 position, float now)
        {
            next = 0; lapArmed = false; CompletedLaps = 0; PassedCheckpoints = 0; BestLapSeconds = LastLapSeconds = 0;
            previous = position; hasPrevious = true; lapStart = lastGateTime = now;
        }

        public void Invalidate(Vector3 position, float now)
        {
            next = 0; lapArmed = false; PassedCheckpoints = CompletedLaps * gates.Length;
            previous = position; hasPrevious = true; lapStart = lastGateTime = now;
        }

        public bool Tick(Vector3 position, float now)
        {
            if (!hasPrevious) { previous = position; hasPrevious = true; lapStart = now; return false; }
            Vector3 step = position - previous;
            if (step.magnitude > settings.maximumStepMeters) { Invalidate(position, now); return false; }
            Vector3 gate = gates[next];
            float before = Vector3.Dot(previous - gate, forwards[next]);
            float after = Vector3.Dot(position - gate, forwards[next]);
            previous = position;
            if (before >= 0 || after < 0 || now - lastGateTime < .5f) return false;
            float portion = -before / Mathf.Max(.001f, after - before);
            Vector3 crossing = position - step * (1 - portion);
            Vector3 lateral = crossing - gate;
            lateral.y = 0;
            lateral -= forwards[next] * Vector3.Dot(lateral, forwards[next]);
            if (lateral.magnitude > settings.checkpointHalfWidthMeters || Mathf.Abs(crossing.y - gate.y) > 8) return false;
            lastGateTime = now;
            PassedCheckpoints++;
            if (next == 0 && lapArmed)
            {
                LastLapSeconds = now - lapStart;
                if (LastLapSeconds > 0 && (BestLapSeconds == 0 || LastLapSeconds < BestLapSeconds)) BestLapSeconds = LastLapSeconds;
                CompletedLaps++;
                lapStart = now; lapArmed = false; next = 1;
                return true;
            }
            if (next == gates.Length - 1) { next = 0; lapArmed = true; }
            else next++;
            return false;
        }
    }
}
