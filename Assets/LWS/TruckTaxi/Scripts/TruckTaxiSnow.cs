using System;
using System.Collections.Generic;
using LWS.InterstateHauler;
using NWH.Common.Vehicles;
using NWH.VehiclePhysics2;
using NWH.VehiclePhysics2.Powertrain;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public enum TruckTaxiPlowState { Unplowed, PartiallyPlowed, Plowed }

    // Taxi owns local, plowable depth. LWS road condition remains the general weather authority.
    public sealed class TruckTaxiSnowRegion
    {
        public struct Cell
        {
            public Vector3 a, b;
            public float width, depth;
            public TruckTaxiPlowState State(float maximum)
            {
                if (depth <= .08f) return TruckTaxiPlowState.Plowed;
                return depth < maximum * .7f ? TruckTaxiPlowState.PartiallyPlowed : TruckTaxiPlowState.Unplowed;
            }
        }

        private readonly List<Cell> cells = new List<Cell>();
        public IReadOnlyList<Cell> Cells => cells;
        public float MaximumDepth { get; }
        public int Count => cells.Count;
        public TruckTaxiSnowRegion(float maximumDepth) { MaximumDepth = Mathf.Max(.1f, maximumDepth); }

        public void AddLane(LwsTrafficLaneDefinition lane, int maximumCells)
        {
            if (lane?.centerline == null || lane.centerline.Length < 2) return;
            float half = Mathf.Max(1, lane.laneWidthMeters * .5f);
            for (int i = 1; i < lane.centerline.Length && cells.Count + 2 <= maximumCells; i++)
            {
                Vector3 from = lane.centerline[i - 1], to = lane.centerline[i];
                float length = Vector2.Distance(new Vector2(from.x, from.z), new Vector2(to.x, to.z));
                if (length < .1f) continue;
                int steps = Mathf.CeilToInt(length / 3f);
                Vector3 right = Vector3.Cross(Vector3.up, (to - from).normalized) * (half * .5f);
                for (int step = 0; step < steps && cells.Count + 2 <= maximumCells; step++)
                {
                    Vector3 a = Vector3.Lerp(from, to, (float)step / steps);
                    Vector3 b = Vector3.Lerp(from, to, (float)(step + 1) / steps);
                    cells.Add(new Cell { a = a - right, b = b - right, width = half });
                    cells.Add(new Cell { a = a + right, b = b + right, width = half });
                }
            }
        }

        public bool Accumulate(float metres)
        {
            if (!float.IsFinite(metres) || metres <= 0) return false;
            bool changed = false;
            for (int i = 0; i < cells.Count; i++)
            {
                Cell cell = cells[i];
                float next = Mathf.Min(MaximumDepth, cell.depth + metres);
                if (next <= cell.depth) continue;
                cell.depth = next; cells[i] = cell; changed = true;
            }
            return changed;
        }

        public float DepthAt(Vector3 position)
        {
            float closest = float.MaxValue, depth = 0;
            for (int i = 0; i < cells.Count; i++)
            {
                Cell cell = cells[i];
                float distance = DistanceToSegmentXZ(position, cell.a, cell.b);
                if (distance > cell.width * .5f || distance >= closest) continue;
                closest = distance; depth = cell.depth;
            }
            return depth;
        }

        public int ClearSweep(Vector3 from, Vector3 to, float bladeWidth, float residual)
        {
            if (!float.IsFinite(from.x) || !float.IsFinite(to.x) || bladeWidth <= 0) return 0;
            int cleared = 0;
            for (int i = 0; i < cells.Count; i++)
            {
                Cell cell = cells[i];
                Vector3 center = (cell.a + cell.b) * .5f;
                if (DistanceToSegmentXZ(center, from, to) > bladeWidth * .5f + cell.width * .35f) continue;
                float next = Mathf.Min(cell.depth, Mathf.Clamp(residual, 0, MaximumDepth));
                if (next >= cell.depth) continue;
                cell.depth = next; cells[i] = cell; cleared++;
            }
            return cleared;
        }

        private static float DistanceToSegmentXZ(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector2 start = new Vector2(a.x, a.z), end = new Vector2(b.x, b.z), point = new Vector2(p.x, p.z);
            Vector2 delta = end - start;
            float t = delta.sqrMagnitude > .0001f ? Mathf.Clamp01(Vector2.Dot(point - start, delta) / delta.sqrMagnitude) : 0;
            return Vector2.Distance(point, start + delta * t);
        }
    }

    [DisallowMultipleComponent]
    public sealed class TruckTaxiSnow : MonoBehaviour
    {
        [SerializeField, HideInInspector] private Shader[] runtimeCoverageShaders;
        public const string BlizzardId = "blizzard";
        public TruckTaxiSnowRegion Region { get; private set; }
        public TruckTaxiSnowplow Plow { get; private set; }
        public string Diagnostic { get; private set; } = "Not initialized";
        public int VisibleCellCount => Region?.Count ?? 0;
        public float PlayerSnowDepth => owner?.Player != null ? Region?.DepthAt(owner.Player.transform.position) ?? 0 : 0;
        private TruckTaxiBootstrap owner;
        private TruckTaxiEnvironmentSettings settings;
        private TruckTaxiSnowSurface surface;
        private TruckTaxiSnowTraction traction;
        private bool initialized;
        private float nextVisualUpdate;

        public void Initialize(TruckTaxiBootstrap taxi, TruckTaxiEnvironmentCoordinator environment, TruckTaxiEnvironmentSettings configuration)
        {
            if (initialized || taxi == null || taxi.Player == null || taxi.traffic == null || configuration == null) return;
            owner = taxi; settings = configuration;
            Region = new TruckTaxiSnowRegion(settings.maximumSnowDepthMeters);
            foreach (LwsTrafficLaneDefinition lane in taxi.traffic.cityLanes ?? Array.Empty<LwsTrafficLaneDefinition>())
                Region.AddLane(lane, settings.maximumSnowCells);
            if (Region.Count == 0) { Diagnostic = "No authored Taxi traffic lanes for snow geometry."; return; }
            surface = gameObject.AddComponent<TruckTaxiSnowSurface>();
            surface.Initialize(Region);
            traction = taxi.Player.gameObject.AddComponent<TruckTaxiSnowTraction>();
            traction.Initialize(taxi.Player.GetComponent<VehicleController>(), Region);
            Plow = gameObject.AddComponent<TruckTaxiSnowplow>();
            Plow.Initialize(taxi, Region, settings, surface);
            initialized = true;
            Diagnostic = $"Taxi snow active on {Region.Count} bounded cells; maximum {Region.MaximumDepth:0.00} m.";
        }

        public void Tick(float deltaSeconds, string weatherId)
        {
            if (!initialized || !isActiveAndEnabled || deltaSeconds <= 0) return;
            float rate = weatherId == BlizzardId ? settings.blizzardMetersPerSecond :
                weatherId == LwsWeatherPresetCatalog.HeavySnowId ? settings.heavySnowMetersPerSecond :
                weatherId == LwsWeatherPresetCatalog.LightSnowId ? settings.lightSnowMetersPerSecond : 0;
            if (Region.Accumulate(Mathf.Min(deltaSeconds, .25f) * rate)) surface.MarkDirty();
            Plow.Tick(weatherId);
            traction.Tick();
            if (Time.time >= nextVisualUpdate)
            {
                surface.Refresh();
                nextVisualUpdate = Time.time + .5f;
            }
        }
        private void OnEnable()
        {
            if (initialized && traction != null) traction.enabled = true;
            if (initialized && Plow != null) Plow.enabled = true;
        }
        private void OnDisable()
        {
            if (traction != null) traction.enabled = false;
            if (Plow != null) Plow.enabled = false;
        }
    }

    // Applies local depth through the same public NWH wheel properties as the LWS road-condition adapter.
    public sealed class TruckTaxiSnowTraction : MonoBehaviour
    {
        private readonly List<Wheel> wheels = new List<Wheel>();
        private TruckTaxiSnowRegion region;
        private float nextApply;
        public float CurrentGripMultiplier { get; private set; } = 1;
        public float CurrentRollingMultiplier { get; private set; } = 1;
        private struct Wheel
        {
            public WheelUAPI api;
            public float longitudinal, lateral, rolling;
        }
        public void Initialize(VehicleController vehicle, TruckTaxiSnowRegion snow)
        {
            region = snow;
            if (vehicle?.powertrain?.wheels == null) return;
            foreach (WheelComponent component in vehicle.powertrain.wheels)
            {
                WheelUAPI api = component?.wheelUAPI;
                if (api == null) continue;
                wheels.Add(new Wheel { api = api, longitudinal = api.LongitudinalFrictionGrip,
                    lateral = api.LateralFrictionGrip, rolling = api.RollingResistanceTorque });
            }
        }
        public void Tick()
        {
            if (region == null || Time.time < nextApply) return;
            nextApply = Time.time + .2f;
            float depth = region.DepthAt(transform.position);
            CurrentGripMultiplier = GripForDepth(depth, region.MaximumDepth);
            CurrentRollingMultiplier = RollingForDepth(depth, region.MaximumDepth);
            foreach (Wheel wheel in wheels)
            {
                if (wheel.api == null) continue;
                wheel.api.LongitudinalFrictionGrip = wheel.longitudinal * CurrentGripMultiplier;
                wheel.api.LateralFrictionGrip = wheel.lateral * CurrentGripMultiplier;
                wheel.api.RollingResistanceTorque = wheel.rolling * CurrentRollingMultiplier;
            }
        }
        public static float GripForDepth(float depth, float maximumDepth) =>
            Mathf.Lerp(1, .42f, Mathf.Clamp01(depth / Mathf.Max(.1f, maximumDepth)));
        public static float RollingForDepth(float depth, float maximumDepth) =>
            Mathf.Lerp(1, 2.5f, Mathf.Clamp01(depth / Mathf.Max(.1f, maximumDepth)));
        private void OnDisable()
        {
            foreach (Wheel wheel in wheels)
            {
                if (wheel.api == null) continue;
                wheel.api.LongitudinalFrictionGrip = wheel.longitudinal;
                wheel.api.LateralFrictionGrip = wheel.lateral;
                wheel.api.RollingResistanceTorque = wheel.rolling;
            }
        }
    }
}
