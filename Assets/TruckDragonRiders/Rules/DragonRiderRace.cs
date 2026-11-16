using System;
using UnityEngine;

namespace LWS.TruckDragonRiders
{
    [Serializable]
    public sealed class DragonRiderGateDefinition
    {
        public Vector3 center;
        public Quaternion rotation = Quaternion.identity;
        public float clearRadius = 32;
    }

    // Ordered, direction-sensitive physical passages are the only finish source.
    // Penalties modify a genuine finish time; they cannot award gate progress.
    public sealed class DragonRiderRace
    {
        public int Count { get; private set; }
        public int Laps { get; private set; }
        public int GateCount => gates.Length;
        public float Clock { get; private set; }
        public readonly int[] Passed = new int[4];
        public readonly float[] Penalty = new float[4];
        public readonly float[] FinishTime = { -1, -1, -1, -1 };
        public readonly bool[] Retired = new bool[4];
        DragonRiderGateDefinition[] gates = Array.Empty<DragonRiderGateDefinition>();
        readonly bool[] approached = new bool[4];
        public bool Complete
        {
            get { for (int i = 0; i < Count; i++) if (!Finished(i) && !Retired[i]) return false; return Count > 0; }
        }
        public void Begin(int count, int laps, DragonRiderGateDefinition[] authoredGates)
        {
            if (count < 1 || count > 4 || laps < 1 || laps > 5 || authoredGates == null || authoredGates.Length < 8)
                throw new ArgumentException("One to four real crews, bounded laps and at least eight authored aerial gates are required");
            gates = new DragonRiderGateDefinition[authoredGates.Length];
            for (int i = 0; i < gates.Length; i++)
            {
                var source = authoredGates[i];
                if (source == null || !Finite(source.center) || !Finite(source.rotation) || !float.IsFinite(source.clearRadius) || source.clearRadius < 8)
                    throw new ArgumentException("Invalid real aerial gate " + i);
                gates[i] = new DragonRiderGateDefinition { center = source.center, rotation = source.rotation.normalized, clearRadius = source.clearRadius };
            }
            Count = count; Laps = laps; Clock = 0;
            Array.Clear(Passed,0,4); Array.Clear(Penalty,0,4); Array.Clear(Retired,0,4); Array.Clear(approached,0,4);
            for (int i = 0; i < 4; i++) FinishTime[i] = -1;
        }
        public void Tick(float dt)
        {
            if (!float.IsFinite(dt) || dt < 0) throw new ArgumentOutOfRangeException(nameof(dt));
            if (!Complete) Clock += dt;
        }
        public int NextGate(int slot) { Check(slot); return Passed[slot] % gates.Length; }
        public bool NeedsApproach(int slot) { Check(slot); return !approached[slot]; }
        public int CurrentLap(int slot) { Check(slot); return Mathf.Min(Laps, Passed[slot] / gates.Length + 1); }
        public bool Finished(int slot) => slot >= 0 && slot < Count && FinishTime[slot] >= 0;
        public DragonRiderGateDefinition Gate(int index)
        {
            var source = gates[index];
            return new DragonRiderGateDefinition { center = source.center, rotation = source.rotation, clearRadius = source.clearRadius };
        }
        public Vector3 GateCenter(int index) => gates[index].center;
        public Quaternion GateRotation(int index) => gates[index].rotation;
        public float GateRadius(int index) => gates[index].clearRadius;
        public bool ObservePassage(int slot, Vector3 before, Vector3 after, float truckRadius, bool physicallyMounted)
        {
            Check(slot);
            if (Finished(slot) || Retired[slot] || !physicallyMounted || !Finite(before) || !Finite(after)
                || !float.IsFinite(truckRadius) || truckRadius < 0) return false;
            var gate = gates[NextGate(slot)];
            var normal = gate.rotation * Vector3.forward;
            float a = Vector3.Dot(before-gate.center,normal), b = Vector3.Dot(after-gate.center,normal);
            if (a < -Mathf.Max(1,truckRadius)) approached[slot] = true;
            if (!approached[slot] || a > 0 || b < 0 || b-a < .001f) return false;
            float t = -a/(b-a);
            var crossing = Vector3.Lerp(before,after,t)-gate.center;
            if (Vector3.ProjectOnPlane(crossing,normal).magnitude + truckRadius > gate.clearRadius) return false;
            Passed[slot]++; approached[slot] = false;
            if (Passed[slot] == Laps*gates.Length) FinishTime[slot] = Clock + Penalty[slot];
            return true;
        }
        public void AddPenalty(int slot, float seconds)
        {
            Check(slot);
            if (!float.IsFinite(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            if (!Finished(slot) && !Retired[slot]) Penalty[slot] += seconds;
        }
        public void FailedRescuesExhausted(int slot) { Check(slot); if (!Finished(slot)) Retired[slot] = true; }
        public void ClearPassageApproach(int slot) { Check(slot); approached[slot] = false; }
        public int Place(int slot, Vector3[] actualPositions)
        {
            Check(slot); int place = 1;
            for (int i = 0; i < Count; i++)
            {
                if (i == slot) continue;
                if (Finished(i)) { if (!Finished(slot) || FinishTime[i] < FinishTime[slot]-.0001f) place++; continue; }
                if (Finished(slot)) continue;
                if (Retired[slot] && !Retired[i]) { place++; continue; }
                if (!Retired[slot] && Retired[i]) continue;
                if (Passed[i] > Passed[slot]) place++;
                else if (Passed[i] == Passed[slot] && actualPositions != null && actualPositions.Length >= Count
                    && Vector3.SqrMagnitude(actualPositions[i]-gates[NextGate(i)].center) < Vector3.SqrMagnitude(actualPositions[slot]-gates[NextGate(slot)].center)-.01f) place++;
            }
            return place;
        }
        public float Progress(int slot, Vector3 position)
        {
            Check(slot); if (Finished(slot)) return Laps*gates.Length;
            int next = NextGate(slot), previous = (next+gates.Length-1)%gates.Length;
            float length = Vector3.Distance(gates[previous].center,gates[next].center);
            return Passed[slot] + (length > 1 ? Mathf.Clamp01(1-Vector3.Distance(position,gates[next].center)/length) : 0);
        }
        void Check(int slot) { if (slot < 0 || slot >= Count) throw new ArgumentOutOfRangeException(nameof(slot)); }
        static bool Finite(Vector3 p) => float.IsFinite(p.x)&&float.IsFinite(p.y)&&float.IsFinite(p.z);
        static bool Finite(Quaternion q) => float.IsFinite(q.x)&&float.IsFinite(q.y)&&float.IsFinite(q.z)&&float.IsFinite(q.w)&&Quaternion.Dot(q,q)>.01f;
    }

    public sealed class DragonRiderFlightBudget
    {
        public float Stamina { get; private set; }
        public float Capacity { get; private set; }
        public float BrakeCharge { get; private set; }
        public float Stability { get; private set; }
        float quietSeconds;
        public void Reset(float capacity)
        {
            if (!float.IsFinite(capacity) || capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            Capacity = Stamina = capacity; BrakeCharge = 1.5f; Stability = 100; quietSeconds=3;
        }
        public bool Step(float dt, float climb, bool sprintRequested, bool brakeRequested)
        {
            if (!float.IsFinite(dt) || dt < 0 || !float.IsFinite(climb)) throw new ArgumentOutOfRangeException(nameof(dt));
            bool sprint = sprintRequested && Stamina > 8 && !brakeRequested;
            float use = (sprint ? 19 : 0) + Mathf.Max(0,climb)*9;
            Stamina = Mathf.Clamp(Stamina + (use > 0 ? -use : 13)*dt,0,Capacity);
            BrakeCharge = Mathf.Clamp(BrakeCharge + (brakeRequested ? -1 : .35f)*dt,0,1.5f);
            float calmTime=Mathf.Max(0,quietSeconds+dt-3)-Mathf.Max(0,quietSeconds-3);
            quietSeconds+=dt;Stability = Mathf.Min(100,Stability+7*calmTime);
            return sprint;
        }
        public bool ApplyAttack(float damage)
        {
            if (!float.IsFinite(damage) || damage < 0) return false;
            Stability = Mathf.Max(0,Stability-damage);
            quietSeconds=0;
            return Stability <= 0;
        }
    }
}
