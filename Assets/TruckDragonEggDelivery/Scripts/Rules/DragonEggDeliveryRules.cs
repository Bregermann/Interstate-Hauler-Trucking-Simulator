using System;
using System.Collections.Generic;

namespace LWS.TruckDragonEggDelivery
{
    public enum DragonEggStage { Egg, Cracking, Hatching, Baby, Lost, Delivered }

    public sealed class DragonEggRecord
    {
        public string Id { get; internal set; }
        public int Owner { get; internal set; }
        public string NestId { get; internal set; }
        public DragonEggStage Stage { get; internal set; }
        public double Condition { get; internal set; }
        public double HatchAt { get; internal set; }
        public double LastImpactAt { get; internal set; }
        public int RoughImpacts { get; internal set; }
        public double GentleSeconds { get; internal set; }
        public double DeliverySeconds { get; internal set; }
        public bool DeliveredBaby { get; internal set; }
        public int Score { get; internal set; }
        public bool Terminal => Stage == DragonEggStage.Lost || Stage == DragonEggStage.Delivered;
    }

    // Egg-specific identity, condition and awards. Unity contact/animation proof
    // is supplied by the physical owner; clocks alone never deliver or hatch.
    public sealed class DragonEggDeliveryRules
    {
        readonly DragonEggRecord[] records;
        public IReadOnlyList<DragonEggRecord> Cargo { get; }
        public double Clock { get; private set; }
        public double TimeLimit { get; }
        public int Count => records.Length;
        public DragonEggDeliveryRules(int count, int seed, int option, string[] nestIds)
        {
            if (count < 1 || count > 4 || option < 0 || option > 2 || nestIds == null || nestIds.Length < count)
                throw new ArgumentException("One to four actual crews and valid nest assignments are required.");
            var random = new Random(seed); records = new DragonEggRecord[count]; TimeLimit = 900;
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < count; i++)
            {
                if (string.IsNullOrWhiteSpace(nestIds[i]) || !ids.Add(nestIds[i])) throw new ArgumentException("Each crew requires a distinct correct physical nest.");
                bool early = random.NextDouble() < (option == 0 ? .35 : option == 1 ? .7 : 1);
                records[i] = new DragonEggRecord { Id = "brood:" + seed + ":" + i, Owner = i, NestId = nestIds[i], Condition = 1,
                    Stage = DragonEggStage.Egg, HatchAt = early ? (option == 2 ? 35 : 75) + random.NextDouble() * (option == 2 ? 75 : 180) : double.PositiveInfinity,
                    LastImpactAt = double.NegativeInfinity };
            }
            Cargo = Array.AsReadOnly(records);
        }
        public void Advance(double dt)
        { if (double.IsNaN(dt) || double.IsInfinity(dt) || dt <= 0) return; Clock += dt; }
        public bool Impact(int slot, double deltaVelocity)
        {
            var item = Item(slot);
            if (item.Terminal || !Finite(deltaVelocity) || deltaVelocity <= 3 || Clock < 1.5 || Clock - item.LastImpactAt < .18) return false;
            item.LastImpactAt = Clock; item.RoughImpacts++;
            double loss = deltaVelocity >= 25 ? 1 : Math.Min(.4, (deltaVelocity - 3) * .025);
            item.Condition = Math.Max(0, item.Condition - loss);
            if (item.Condition <= 0) item.Stage = DragonEggStage.Lost;
            return true;
        }
        public void ObserveGentleHandling(int slot, double dt, bool gentle)
        { var item = Item(slot); if (!item.Terminal && gentle && Finite(dt) && dt > 0) item.GentleSeconds += dt; }
        public bool ShouldHatch(int slot)
        {
            var item = Item(slot);
            return item.Stage == DragonEggStage.Egg && item.Condition > 0 && (Clock >= item.HatchAt || item.Condition < .5 && item.RoughImpacts >= 3);
        }
        public bool BeginCracking(int slot)
        { if (!ShouldHatch(slot)) return false; Item(slot).Stage = DragonEggStage.Cracking; return true; }
        public bool BeginHatching(int slot)
        { var item = Item(slot); if (item.Stage != DragonEggStage.Cracking) return false; item.Stage = DragonEggStage.Hatching; return true; }
        public bool CompleteHatch(int slot, bool babyActuallyVisible, bool hatchAnimationCompleted)
        {
            var item = Item(slot);
            if (item.Stage != DragonEggStage.Hatching || !babyActuallyVisible || !hatchAnimationCompleted) return false;
            item.Stage = DragonEggStage.Baby; return true;
        }
        public bool Deliver(int slot, string nestId, bool cargoPhysicallyPresent, bool supportedInsideNest, bool settled, out int points)
        {
            points = 0; var item = Item(slot);
            if (item.Terminal || item.Stage == DragonEggStage.Hatching || item.Condition <= 0 || !cargoPhysicallyPresent ||
                !supportedInsideNest || !settled || item.NestId != nestId) return false;
            item.DeliveredBaby = item.Stage == DragonEggStage.Baby;
            double handling = Math.Min(1, item.GentleSeconds / Math.Max(1, Clock));
            points = (int)Math.Round(600 * Math.Pow(item.Condition, 1.2) + 180 * Math.Max(0, 1 - Clock / TimeLimit) +
                180 * handling / (1 + item.RoughImpacts * .2) + (item.DeliveredBaby ? 120 : 0));
            item.Score = points; item.DeliverySeconds = Clock; item.Stage = DragonEggStage.Delivered;
            return true;
        }
        public bool Lose(int slot)
        { var item = Item(slot); if (item.Terminal) return false; item.Stage = DragonEggStage.Lost; return true; }
        public void ExpireUnfinished()
        { if (Clock < TimeLimit) return; for (int i = 0; i < records.Length; i++) Lose(i); }
        public bool Complete { get { foreach (var item in records) if (!item.Terminal) return false; return true; } }
        public int[] Winners()
        {
            int best = 0; foreach (var item in records) best = Math.Max(best, item.Score);
            if (best == 0) return Array.Empty<int>();
            var winners = new List<int>(); for (int i = 0; i < records.Length; i++) if (records[i].Score == best) winners.Add(i);
            return winners.ToArray();
        }
        DragonEggRecord Item(int slot)
        { if (slot < 0 || slot >= records.Length) throw new ArgumentOutOfRangeException(nameof(slot)); return records[slot]; }
        static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
