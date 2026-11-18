using System;
using UnityEngine;

namespace LWS.TruckExtremeHauling
{
    // Course semantics only. NWH/PhysX remain the only movement and collision authorities.
    public sealed class RoadTrainObstacleCourse : MonoBehaviour
    {
        [Serializable] public struct Gate
        {
            public Transform Marker;
            [Min(1)] public float HalfWidth;
        }
        [Serializable] public struct Lane { public Gate[] Gates; }
        public GameObject Content;
        public Lane[] Lanes = Array.Empty<Lane>();
        public Rigidbody[] MovableRoadblocks = Array.Empty<Rigidbody>();
        readonly int[] passed = new int[4], missed = new int[4];
        readonly Vector3[] previous = new Vector3[4];
        readonly bool[] sampled = new bool[4];
        Vector3[] blockPositions;
        Quaternion[] blockRotations;

        void Awake() => CaptureAuthoredBlocks();
        void CaptureAuthoredBlocks()
        {
            if (blockPositions != null) return;
            blockPositions = new Vector3[MovableRoadblocks.Length];
            blockRotations = new Quaternion[MovableRoadblocks.Length];
            for (int i = 0; i < MovableRoadblocks.Length; i++)
                if (MovableRoadblocks[i] != null)
                { blockPositions[i] = MovableRoadblocks[i].transform.position; blockRotations[i] = MovableRoadblocks[i].transform.rotation; }
        }
        public int GateCount(int slot) => slot >= 0 && slot < Lanes.Length && Lanes[slot].Gates != null ? Lanes[slot].Gates.Length : 0;
        public int GatesPassed(int slot) => passed[slot];
        public int GatesMissed(int slot) => missed[slot];
        public bool Complete(int slot) => GateCount(slot) > 0 && passed[slot] == GateCount(slot);
        public Vector3 NextGate(int slot) => Lanes[slot].Gates[Mathf.Min(passed[slot], GateCount(slot) - 1)].Marker.position;
        public void BeginRound(bool enabled)
        {
            CaptureAuthoredBlocks();
            Array.Clear(passed, 0, passed.Length); Array.Clear(missed, 0, missed.Length); Array.Clear(sampled, 0, sampled.Length);
            if (Content != null) Content.SetActive(enabled);
            // Only an ordinary round boundary restores props. A collision in a
            // live attempt permanently moves the real obstacle until that boundary.
            for (int i = 0; i < MovableRoadblocks.Length; i++)
            {
                var body = MovableRoadblocks[i]; if (body == null) continue;
                body.isKinematic = true;
                body.transform.SetPositionAndRotation(blockPositions[i], blockRotations[i]);
                body.position = blockPositions[i]; body.rotation = blockRotations[i];
                if (!enabled) continue;
                body.isKinematic = false; body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero;
            }
        }
        public bool ObserveTail(int slot, Rigidbody tail, bool fullConnectedTrain, bool wheelContact, float dt)
        {
            Vector3 current = tail.worldCenterOfMass;
            if (!sampled[slot]) { previous[slot] = current; sampled[slot] = true; return false; }
            Vector3 before = previous[slot]; previous[slot] = current;
            if (Complete(slot) || GateCount(slot) == 0 || !fullConnectedTrain || !wheelContact || Vector3.Distance(before, current) > Mathf.Max(1, 35 * dt)) return false;
            var gate = Lanes[slot].Gates[passed[slot]];
            Vector3 a = gate.Marker.InverseTransformPoint(before), b = gate.Marker.InverseTransformPoint(current);
            if (a.z > 0 || b.z <= 0) return false;
            Vector3 crossing = Vector3.Lerp(a, b, -a.z / (b.z - a.z));
            if (Mathf.Abs(crossing.x) > gate.HalfWidth || Mathf.Abs(crossing.y) > 6) { missed[slot]++; return false; }
            passed[slot]++;
            return true;
        }
        public string Diagnostic(int slot) => "gates=" + GatesPassed(slot) + "/" + GateCount(slot) + " missed=" + GatesMissed(slot) + (Complete(slot) || GateCount(slot) == 0 ? "" : " next=" + NextGate(slot).ToString("F1"));
    }
}
