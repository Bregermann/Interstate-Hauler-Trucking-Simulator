using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // Logical capacity only: no actors, weather gates, map markers, or scene loading.
    public sealed class TruckTaxiCompanionPopulationPolicy
    {
        public const int VariantCount = 8;
        public sealed class Record
        {
            public string Id { get; internal set; }
            public string AreaId { get; internal set; }
            public Bounds Area { get; internal set; }
            public float ExclusionRadius { get; internal set; }
            public bool IsService { get; internal set; }
            public int Slot { get; internal set; }
            public int Capacity { get; internal set; }
            public int Variant { get; internal set; }
            public Vector3 Origin => Area.center;
        }

        private readonly List<Record> records = new List<Record>();
        private readonly Dictionary<string, List<Record>> areas = new Dictionary<string, List<Record>>(StringComparer.Ordinal);
        public IReadOnlyList<Record> Records => records;
        public int Count => records.Count;

        public void RegisterBlock(string id, Bounds bounds) => Register("block:" + id, bounds, false, 0, 1);
        public void RegisterService(string id, Vector3 position, float radius, bool largeSite = false)
        {
            radius = Mathf.Max(2, radius);
            Register("service:" + id, new Bounds(position, Vector3.one * (radius + 18) * 2), true,
                radius, largeSite ? 6 : 3);
        }

        private void Register(string id, Bounds bounds, bool service, float exclusionRadius, int count)
        {
            if (string.IsNullOrWhiteSpace(id.Substring(id.IndexOf(':') + 1)))
                throw new ArgumentException("Population areas need stable IDs.", nameof(id));
            if (!areas.TryGetValue(id, out var slots))
            {
                slots = new List<Record>();
                areas.Add(id, slots);
            }
            while (slots.Count < count)
            {
                var record = new Record { Id = id + ":" + slots.Count, AreaId = id,
                    Slot = slots.Count, Variant = (StableHash(id) + slots.Count) % VariantCount };
                slots.Add(record); records.Add(record);
            }
            foreach (var record in slots)
            {
                record.Area = bounds; record.IsService = service;
                record.ExclusionRadius = exclusionRadius; record.Capacity = slots.Count;
            }
        }

        public static bool IsServiceLocation(TaxiLocationType type) =>
            type == TaxiLocationType.Store || type == TaxiLocationType.Restaurant ||
            type == TaxiLocationType.Hotel || type == TaxiLocationType.GasStation;

        private static int StableHash(string value)
        {
            uint hash = 2166136261;
            foreach (char c in value) hash = unchecked((hash ^ c) * 16777619);
            return (int)(hash % 100000);
        }
    }
}
