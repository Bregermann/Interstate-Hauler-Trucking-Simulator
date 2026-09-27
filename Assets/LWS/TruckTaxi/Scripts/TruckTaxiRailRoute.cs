using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace LWS.TruckTaxi
{
    // Persistent route data; the physical rail mesh streams independently.
    [DisallowMultipleComponent]
    public sealed class TruckTaxiRailRoute : MonoBehaviour
    {
        public SplineContainer container;
        public float[] stationDistances = new float[2];
        private const int Samples = 512;
        private readonly float[] lengths = new float[Samples + 1];
        public float Length { get; private set; }

        public void Configure(Vector3[] knots)
        {
            if (knots == null || knots.Length < 2) throw new ArgumentException("Rail requires at least two knots.");
            container = GetComponent<SplineContainer>() ?? gameObject.AddComponent<SplineContainer>();
            var spline = new Spline();
            foreach (var world in knots) spline.Add((float3)transform.InverseTransformPoint(world), TangentMode.AutoSmooth);
            container.Spline = spline;
            RebuildCache();
            stationDistances[0] = NearestDistance(new Vector3(0, 0, 355));
            stationDistances[1] = NearestDistance(new Vector3(4000, 0, 355));
        }

        private void Awake() { if (container == null) container = GetComponent<SplineContainer>(); RebuildCache(); }

        public void RebuildCache()
        {
            Length = 0;
            if (container == null || container.Spline == null || container.Spline.Count < 2) return;
            Vector3 previous = container.EvaluatePosition(0);
            lengths[0] = 0;
            for (int i = 1; i <= Samples; i++)
            {
                Vector3 next = container.EvaluatePosition(i / (float)Samples);
                Length += Vector3.Distance(previous, next);
                lengths[i] = Length;
                previous = next;
            }
        }

        public Vector3 Position(float distance)
        {
            if (Length <= 0) return transform.position;
            int index = Array.BinarySearch(lengths, Mathf.Clamp(distance, 0, Length));
            if (index < 0) index = ~index;
            index = Mathf.Clamp(index, 1, Samples);
            float t = Mathf.Lerp((index - 1f) / Samples, index / (float)Samples,
                Mathf.InverseLerp(lengths[index - 1], lengths[index], distance));
            return container.EvaluatePosition(t);
        }

        public float NearestDistance(Vector3 world)
        {
            float best = float.PositiveInfinity, result = 0;
            for (int i = 0; i <= Samples; i++)
            {
                float squared = (world - (Vector3)container.EvaluatePosition(i / (float)Samples)).sqrMagnitude;
                if (squared < best) { best = squared; result = lengths[i]; }
            }
            return result;
        }
    }
}
