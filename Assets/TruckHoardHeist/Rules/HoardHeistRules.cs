using System;

namespace LWS.TruckHoardHeist
{
    public enum HoardCargoState { Loose, Lifting, Carried, Delivered, Lost }
    public enum HoardAlertStage { Sleeping, Restless, Suspicious, Investigating, Awakening, Awake, Pursuing }

    public readonly struct HoardDeliveryEvidence
    {
        public readonly int originalBodyId, rackOwner;
        public readonly bool fullyInsideExtraction, dynamicBody, securedJoint, truckStopped, cargoSettled;
        public HoardDeliveryEvidence(int body, int owner, bool inside, bool dynamic, bool joint, bool stopped, bool settled)
        { originalBodyId = body; rackOwner = owner; fullyInsideExtraction = inside; dynamicBody = dynamic; securedJoint = joint; truckStopped = stopped; cargoSettled = settled; }
        public bool Complete => fullyInsideExtraction && dynamicBody && securedJoint && truckStopped && cargoSettled;
    }

    // Only the mode's observed physical lift/rack/extraction witnesses can move
    // these original finite objects through the award state machine.
    public sealed class HoardHeistRules
    {
        public int Participants { get; }
        public int CapacitySlots { get; }
        public float CapacityKg { get; }
        public int[] Scores { get; }
        public HoardCargoState[] States { get; }
        public int[] Owners { get; }
        public int DeliveredCount { get; private set; }
        public int LostCount { get; private set; }
        public bool Exhausted => DeliveredCount + LostCount == States.Length;
        public int Place(int slot)
        {
            if (slot < 0 || slot >= Participants) throw new ArgumentOutOfRangeException(nameof(slot));
            int rank = 1; for (int i = 0; i < Participants; i++) if (Scores[i] > Scores[slot]) rank++; return rank;
        }
        readonly int[] values, bodyIds;
        readonly float[] masses;

        public HoardHeistRules(int participants, int slots, float kilograms, int[] values, float[] masses, int[] bodyIds)
        {
            if (participants < 1 || participants > 4 || slots < 1 || slots > 8 || !Finite(kilograms) || kilograms <= 0 ||
                values == null || masses == null || bodyIds == null || values.Length == 0 || values.Length != masses.Length || values.Length != bodyIds.Length)
                throw new ArgumentException("A bounded crew, capacity and exact original physical cargo inventory are required.");
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] <= 0 || !Finite(masses[i]) || masses[i] <= 0 || masses[i] > kilograms || bodyIds[i] == 0) throw new ArgumentException("Invalid cargo identity, mass or value.");
                for (int j = 0; j < i; j++) if (bodyIds[i] == bodyIds[j]) throw new ArgumentException("Cargo bodies must have unique identities.");
            }
            Participants = participants; CapacitySlots = slots; CapacityKg = kilograms;
            this.values = (int[])values.Clone(); this.masses = (float[])masses.Clone(); this.bodyIds = (int[])bodyIds.Clone();
            Scores = new int[participants]; States = new HoardCargoState[values.Length]; Owners = new int[values.Length];
            for (int i = 0; i < Owners.Length; i++) Owners[i] = -1;
        }

        public int LoadCount(int owner)
        {
            int count = 0;
            for (int i = 0; i < States.Length; i++) if (Owners[i] == owner && (States[i] == HoardCargoState.Carried || States[i] == HoardCargoState.Lifting)) count++;
            return count;
        }
        public float LoadKg(int owner)
        {
            float mass = 0;
            for (int i = 0; i < States.Length; i++) if (Owners[i] == owner && (States[i] == HoardCargoState.Carried || States[i] == HoardCargoState.Lifting)) mass += masses[i];
            return mass;
        }
        public int CargoValue(int owner)
        {
            int value = 0;
            for (int i = 0; i < States.Length; i++) if (Owners[i] == owner && States[i] == HoardCargoState.Carried) value += values[i];
            return value;
        }
        public bool Fits(int item, int owner) => Valid(item, owner) && States[item] == HoardCargoState.Loose && LoadCount(owner) < CapacitySlots && LoadKg(owner) + masses[item] <= CapacityKg + .001f;
        public bool ReserveLift(int item, int owner, bool actualReachAndClearance)
        {
            if (!actualReachAndClearance || !Fits(item, owner)) return false;
            for (int i = 0; i < States.Length; i++) if (Owners[i] == owner && States[i] == HoardCargoState.Lifting) return false;
            States[item] = HoardCargoState.Lifting; Owners[item] = owner; return true;
        }
        public bool CompleteLoad(int item, int owner, bool settledInsideRackWithActualJoint)
        {
            if (!Valid(item, owner) || !settledInsideRackWithActualJoint || States[item] != HoardCargoState.Lifting || Owners[item] != owner) return false;
            States[item] = HoardCargoState.Carried; return true;
        }
        public bool Drop(int item, int owner, bool actualJointReleased)
        {
            if (!Valid(item, owner) || !actualJointReleased || Owners[item] != owner || (States[item] != HoardCargoState.Lifting && States[item] != HoardCargoState.Carried)) return false;
            States[item] = HoardCargoState.Loose; Owners[item] = -1; return true;
        }
        public int Deliver(int item, int owner, HoardDeliveryEvidence evidence)
        {
            if (!Valid(item, owner) || States[item] != HoardCargoState.Carried || Owners[item] != owner || !evidence.Complete || evidence.rackOwner != owner || evidence.originalBodyId != bodyIds[item]) return -1;
            States[item] = HoardCargoState.Delivered; Owners[item] = -1; DeliveredCount++; Scores[owner] += values[item]; return values[item];
        }
        public bool Lose(int item, bool physicallyOutsideWorld)
        {
            if (item < 0 || item >= States.Length || !physicallyOutsideWorld || States[item] == HoardCargoState.Delivered || States[item] == HoardCargoState.Lost) return false;
            States[item] = HoardCargoState.Lost; Owners[item] = -1; LostCount++; return true;
        }
        bool Valid(int item, int owner) => item >= 0 && item < States.Length && owner >= 0 && owner < Participants;
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }

    public sealed class HoardAlertness
    {
        public float Value { get; private set; }
        public bool Awakened { get; private set; }
        bool wakeRequested;
        public bool WakeAllowed => wakeRequested;
        public HoardAlertStage Stage => Awakened ? HoardAlertStage.Awake : wakeRequested ? HoardAlertStage.Awakening : Value < 15 ? HoardAlertStage.Sleeping : Value < 35 ? HoardAlertStage.Restless : Value < 65 ? HoardAlertStage.Suspicious : HoardAlertStage.Investigating;
        public bool Hear(float loudness, float distance, float hearingRange, bool obstructed)
        {
            if (!Finite(loudness) || !Finite(distance) || !Finite(hearingRange) || loudness <= 0 || distance < 0 || hearingRange <= 0) return false;
            float effectiveRange = hearingRange * (float)Math.Sqrt(Math.Min(4, loudness));
            if (distance > effectiveRange) return false;
            float gain = loudness * 10 * Math.Max(.08f, 1 - distance / Math.Max(1, effectiveRange)) * (obstructed ? .3f : 1);
            Value = Math.Min(100, Value + gain); if (Value >= 99.9f) wakeRequested = true; return true;
        }
        public void Step(float dt, bool actuallyVisible)
        {
            if (!Finite(dt) || dt <= 0) return;
            Value = Math.Max(0, Math.Min(100, Value + dt * (actuallyVisible ? 5 : -3)));
            if (Value >= 99.9f) wakeRequested = true;
        }
        public void MarkAwake() => Awakened = true;
        public void ReturnToSleep() { Awakened = false; wakeRequested = false; Value = 10; }
        static bool Finite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
    }
}
