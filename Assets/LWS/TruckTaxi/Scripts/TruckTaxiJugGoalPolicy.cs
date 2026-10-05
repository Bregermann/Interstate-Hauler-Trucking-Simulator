using System;

namespace LWS.TruckTaxi
{
    public struct TruckTaxiJugGoalContext
    {
        public bool RuntimeAvailable, SafeCompletionAvailable, JugActive;
        public int UsableContainers;
        public float Pressure, CurrentCycleGameSeconds, MinimumCycleGameSeconds, MaximumCycleGameSeconds;
        public float CrisisGameSeconds, CrisisGraceGameSeconds, JugDurationSeconds;
        public float GameSecondsPerRealSecond, BladderRateMultiplier;
    }

    // Feasibility only. Inventory, bladder progression, QTE and rewards retain their existing owners.
    public static class TruckTaxiJugGoalPolicy
    {
        public const float MinimumUsefulPressure = .25f;
        public const float SetupSeconds = 15;
        public const float CompletionBufferSeconds = 5;
        public const float LongRideSeconds = 300;
        public const float RareTwoChance = .1f;
        public const float RideBudgetMetersPerSecond = 20;

        public static TruckTaxiJugGoalContext Capture(TruckTaxiDriverNeedsState needs,
            TruckTaxiEnvironmentSettings settings, float gameSecondsPerRealSecond,
            bool runtimeAvailable, bool safeCompletionAvailable)
        {
            if (needs == null || settings == null) return default;
            var state = needs.CapturePersistentState();
            float minimumCycle = Math.Max(.1f, settings.bladderCycleGameHours.x) * 3600;
            return new TruckTaxiJugGoalContext
            {
                RuntimeAvailable = runtimeAvailable, SafeCompletionAvailable = safeCompletionAvailable,
                JugActive = needs.JugActive,
                UsableContainers = Math.Min(needs.Count(TruckTaxiNeedsItem.EmptyPissJug), Math.Max(0, 99 - needs.Count(TruckTaxiNeedsItem.FilledJug))) +
                    Math.Min(needs.Count(TruckTaxiNeedsItem.EmptyBottle), Math.Max(0, 99 - needs.Count(TruckTaxiNeedsItem.FilledBottle))),
                Pressure = state.Pressure, CurrentCycleGameSeconds = state.CycleGameSeconds,
                MinimumCycleGameSeconds = minimumCycle,
                MaximumCycleGameSeconds = Math.Max(minimumCycle, settings.bladderCycleGameHours.y * 3600),
                CrisisGameSeconds = state.CrisisGameSeconds,
                CrisisGraceGameSeconds = Math.Max(1, settings.crisisGraceGameMinutes) * 60,
                JugDurationSeconds = Math.Max(3, settings.jugDurationSeconds),
                GameSecondsPerRealSecond = gameSecondsPerRealSecond,
                BladderRateMultiplier = needs.Effects.BladderRateMultiplier
            };
        }

        public static int Capacity(TruckTaxiJugGoalContext context, float remainingRideSeconds,
            float deadlineSeconds, out string reason)
        {
            if (!context.RuntimeAvailable || !context.SafeCompletionAvailable)
                return Reject("Ordinary jug QTE or safe completion opportunity unavailable.", out reason);
            if (context.JugActive) return Reject("A jug QTE is already active.", out reason);
            if (context.UsableContainers <= 0) return Reject("No usable empty container and matching filled-container space.", out reason);
            if (!Valid(context, remainingRideSeconds, deadlineSeconds))
                return Reject("Invalid jug feasibility snapshot or remaining ride budget.", out reason);

            double rate = context.GameSecondsPerRealSecond;
            // Do not rely on a temporary acceleration effect surviving either refill.
            double slowRate = rate * Math.Min(1, context.BladderRateMultiplier);
            double wait = context.Pressure >= MinimumUsefulPressure ? 0 : slowRate > 0
                ? (MinimumUsefulPressure - context.Pressure) * context.CurrentCycleGameSeconds / slowRate : double.PositiveInfinity;
            double completion = SetupSeconds + context.JugDurationSeconds + CompletionBufferSeconds;
            double budget = Math.Min(remainingRideSeconds, deadlineSeconds);
            double first = wait + completion;
            if (first > budget)
                return Reject("Not enough ride time for natural bladder readiness and safe QTE completion.", out reason);
            if (first >= SecondsUntilAccident(context.Pressure, context.CurrentCycleGameSeconds, context.CrisisGameSeconds, context))
                return Reject("Bladder crisis would interrupt safe QTE completion.", out reason);

            reason = "One ordinary jug goal is feasible.";
            if (context.UsableContainers < 2 || remainingRideSeconds < LongRideSeconds || slowRate <= 0) return 1;
            // Successful relief samples a NEW cycle. Budget the longest authored cycle, not the old sample.
            double refill = MinimumUsefulPressure * context.MaximumCycleGameSeconds / slowRate;
            if (first + refill + completion > budget ||
                completion >= SecondsUntilAccident(MinimumUsefulPressure, context.MinimumCycleGameSeconds, 0, context)) return 1;
            reason = "Two ordinary jug goals are feasible on this long ride.";
            return 2;
        }

        public static int SelectCount(int capacity, bool authoredRareTwo, float roll)
        {
            if (capacity <= 0) return 0;
            return capacity >= 2 && authoredRareTwo && float.IsFinite(roll) && roll >= 0 && roll < RareTwoChance ? 2 : 1;
        }

        private static double SecondsUntilAccident(float pressure, float cycle, float crisis, TruckTaxiJugGoalContext context)
        {
            if (context.GameSecondsPerRealSecond == 0) return double.PositiveInfinity;
            double fastestRate = context.GameSecondsPerRealSecond * (double)Math.Max(1, context.BladderRateMultiplier);
            return (1 - pressure) * cycle / fastestRate +
                (context.CrisisGraceGameSeconds - crisis) / context.GameSecondsPerRealSecond;
        }
        private static bool Valid(TruckTaxiJugGoalContext c, float ride, float deadline) =>
            float.IsFinite(ride) && ride > 0 && float.IsFinite(deadline) && deadline > 0 &&
            float.IsFinite(c.Pressure) && c.Pressure >= 0 && c.Pressure <= 1 &&
            float.IsFinite(c.CurrentCycleGameSeconds) && c.CurrentCycleGameSeconds > 0 &&
            float.IsFinite(c.MinimumCycleGameSeconds) && c.MinimumCycleGameSeconds > 0 &&
            float.IsFinite(c.MaximumCycleGameSeconds) && c.MaximumCycleGameSeconds >= c.MinimumCycleGameSeconds &&
            float.IsFinite(c.CrisisGameSeconds) && c.CrisisGameSeconds >= 0 &&
            float.IsFinite(c.CrisisGraceGameSeconds) && c.CrisisGraceGameSeconds > c.CrisisGameSeconds &&
            float.IsFinite(c.JugDurationSeconds) && c.JugDurationSeconds >= 3 &&
            float.IsFinite(c.GameSecondsPerRealSecond) && c.GameSecondsPerRealSecond >= 0 &&
            float.IsFinite(c.BladderRateMultiplier) && c.BladderRateMultiplier > 0;
        private static int Reject(string message, out string reason) { reason = message; return 0; }
    }
}
