using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // Stable identities and region membership exist without any Unity actor instances.
    public sealed class TruckTaxiPedestrianLogicalPopulation
    {
        public sealed class Record
        {
            public int Id { get; internal set; }
            public string RegionId { get; internal set; }
            public Vector3 Position { get; internal set; }
            public TruckTaxiPedestrian Actor { get; internal set; }
        }

        private readonly Dictionary<string, Bounds> regions = new Dictionary<string, Bounds>(StringComparer.Ordinal);
        private readonly List<string> regionOrder = new List<string>();
        private readonly List<Record> records = new List<Record>();
        public int Count => records.Count;
        public IReadOnlyList<Record> Records => records;

        public void RegisterRegion(string id, Bounds bounds)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A region needs a stable ID.", nameof(id));
            if (!regions.ContainsKey(id)) regionOrder.Add(id);
            regions[id] = bounds;
            AssignRegions();
        }

        public void Resize(int count, Vector3 fallbackPosition)
        {
            count = Mathf.Max(0, count);
            while (records.Count < count) records.Add(new Record { Id = records.Count, Position = fallbackPosition });
            if (records.Count > count) records.RemoveRange(count, records.Count - count);
            AssignRegions();
        }

        public bool Contains(Record record, Vector3 point)
        {
            if (record.RegionId == null) return true;
            return regions.TryGetValue(record.RegionId, out var bounds) &&
                point.x >= bounds.min.x && point.x <= bounds.max.x &&
                point.z >= bounds.min.z && point.z <= bounds.max.z;
        }

        public bool RegionAvailable(Record record, Func<Vector3, bool> availability)
        {
            return availability == null || availability(record.Position);
        }

        public bool RegionNear(Record record, Vector3 observer, float radius)
        {
            if (record.RegionId == null) return true;
            if (!regions.TryGetValue(record.RegionId, out var bounds)) return false;
            float x = Mathf.Clamp(observer.x, bounds.min.x, bounds.max.x) - observer.x;
            float z = Mathf.Clamp(observer.z, bounds.min.z, bounds.max.z) - observer.z;
            return x * x + z * z <= radius * radius;
        }

        private void AssignRegions()
        {
            if (regionOrder.Count == 0) return;
            for (int i = 0; i < records.Count; i++)
            {
                var record = records[i];
                var id = regionOrder[i % regionOrder.Count];
                var bounds = regions[id];
                record.RegionId = id;
                // Deterministic world-space representative; no terrain query while unloaded.
                float x = (i * .61803398875f) % 1f;
                float z = (i * .38196601125f) % 1f;
                record.Position = new Vector3(Mathf.Lerp(bounds.min.x, bounds.max.x, x),
                    bounds.center.y, Mathf.Lerp(bounds.min.z, bounds.max.z, z));
            }
        }
    }
}
