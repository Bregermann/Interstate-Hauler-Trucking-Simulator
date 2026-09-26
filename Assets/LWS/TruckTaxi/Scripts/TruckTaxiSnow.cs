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

        public bool Melt(float metres)
        {
            if (!float.IsFinite(metres) || metres <= 0) return false;
            bool changed = false;
            for (int i = 0; i < cells.Count; i++)
            {
                Cell cell = cells[i];
                float next = Mathf.Max(0, cell.depth - metres);
                if (next >= cell.depth) continue;
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

        public int CompressSweep(Vector3 from, Vector3 to, float tireWidth, float metres)
        {
            if (!float.IsFinite(from.x) || !float.IsFinite(to.x) || tireWidth <= 0 || !float.IsFinite(metres) || metres <= 0) return 0;
            int compressed = 0;
            for (int i = 0; i < cells.Count; i++)
            {
                Cell cell = cells[i];
                if (DistanceToSegmentXZ((cell.a + cell.b) * .5f, from, to) > tireWidth * .5f + cell.width * .25f) continue;
                float next = Mathf.Max(0, cell.depth - metres);
                if (next >= cell.depth) continue;
                cell.depth = next; cells[i] = cell; compressed++;
            }
            return compressed;
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
        [SerializeField, HideInInspector] private Texture2D weatheradeSnowTexture;
        [SerializeField, HideInInspector] private Texture2D weatheradeSnowDetailTexture;
        [SerializeField, HideInInspector] private Texture2D weatheradeSnowSparkleTexture;
        [SerializeField, HideInInspector] private int weatheradeDepthRendererIndex = -1;
        public const string BlizzardId = "blizzard";
        public TruckTaxiSnowRegion Region { get; private set; }
        public TruckTaxiSnowplow Plow { get; private set; }
        public string Diagnostic { get; private set; } = "Not initialized";
        // Compatibility with existing probes: this is the hidden depth-cell budget, not rendered geometry.
        public int VisibleCellCount => Region?.Count ?? 0;
        public float PlayerSnowDepth => owner?.Player != null ? Region?.DepthAt(owner.Player.transform.position) ?? 0 : 0;
        private TruckTaxiBootstrap owner;
        private TruckTaxiEnvironmentSettings settings;
        private TruckTaxiSnowSurface surface;
        private TruckTaxiSnowTraction traction;
        private bool initialized;
        private float nextVisualUpdate;
        private float backgroundDepth;

        public void Initialize(TruckTaxiBootstrap taxi, TruckTaxiEnvironmentCoordinator environment, TruckTaxiEnvironmentSettings configuration)
        {
            if (initialized || taxi == null || taxi.Player == null || taxi.traffic == null || configuration == null) return;
            owner = taxi; settings = configuration;
            Region = new TruckTaxiSnowRegion(settings.maximumSnowDepthMeters);
            foreach (LwsTrafficLaneDefinition lane in taxi.traffic.cityLanes ?? Array.Empty<LwsTrafficLaneDefinition>())
                Region.AddLane(lane, settings.maximumSnowCells);
            if (Region.Count == 0) { Diagnostic = "No authored Taxi traffic lanes for gameplay snow depth."; return; }
            surface = gameObject.AddComponent<TruckTaxiSnowSurface>();
            surface.Initialize(Region, GetComponent<LwsWeatheradeAdapter>());
            surface.ConfigureVendorTextures(weatheradeSnowTexture, weatheradeSnowDetailTexture, weatheradeSnowSparkleTexture);
            surface.ConfigureDepthRenderer(weatheradeDepthRendererIndex);
            traction = taxi.Player.gameObject.AddComponent<TruckTaxiSnowTraction>();
            traction.Initialize(taxi.Player.GetComponent<VehicleController>(), Region);
            Plow = gameObject.AddComponent<TruckTaxiSnowplow>();
            Plow.Initialize(taxi, Region, settings, surface);
            initialized = true;
            Diagnostic = $"Taxi snow depth active on {Region.Count} hidden cells; Weatherade owns visuals; maximum {Region.MaximumDepth:0.00} m.";
        }

        public void Tick(float deltaSeconds, string weatherId)
        {
            if (!initialized || !isActiveAndEnabled || deltaSeconds <= 0) return;
            float rate = weatherId == BlizzardId ? settings.blizzardMetersPerSecond :
                weatherId == LwsWeatherPresetCatalog.HeavySnowId ? settings.heavySnowMetersPerSecond :
                weatherId == LwsWeatherPresetCatalog.LightSnowId ? settings.lightSnowMetersPerSecond : 0;
            float step = Mathf.Min(deltaSeconds, .25f);
            if (rate > 0)
            {
                float amount = step * rate;
                backgroundDepth = Mathf.Min(Region.MaximumDepth, backgroundDepth + amount);
                Region.Accumulate(amount);
            }
            else
            {
                float amount = step * settings.snowMeltMetersPerSecond;
                backgroundDepth = Mathf.Max(0, backgroundDepth - amount);
                Region.Melt(amount);
            }
            Plow.Tick(weatherId);
            traction.Tick(surface, settings.tireCompressionMetersPerMeter);
            if (Time.time >= nextVisualUpdate)
            {
                surface.SetCoverage(backgroundDepth / Region.MaximumDepth);
                surface.Refresh();
                nextVisualUpdate = Time.time + .2f;
            }
        }
        public void ClearSnow()
        {
            if (!initialized) return;
            Region.Melt(Region.MaximumDepth);
            backgroundDepth = 0;
            traction.ClearModifiers();
            surface.SetCoverage(0);
            surface.Refresh();
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
            public float appliedLongitudinal, appliedLateral, appliedRolling;
            public Vector3 lastContact;
            public bool hasContact, modified;
        }
        public void Initialize(VehicleController vehicle, TruckTaxiSnowRegion snow)
        {
            region = snow;
            if (vehicle?.powertrain?.wheels == null) return;
            foreach (WheelComponent component in vehicle.powertrain.wheels)
            {
                WheelUAPI api = component?.wheelUAPI;
                if (api == null) continue;
                wheels.Add(new Wheel { api = api });
            }
        }
        public void Tick() { Tick(null, 0); }
        public void Tick(TruckTaxiSnowSurface surface, float compressionMetersPerMeter)
        {
            if (region == null) return;
            bool apply = Time.time >= nextApply;
            if (apply)
            {
                nextApply = Time.time + .2f;
                CurrentGripMultiplier = 1;
                CurrentRollingMultiplier = 1;
            }
            for (int i = 0; i < wheels.Count; i++)
            {
                Wheel wheel = wheels[i];
                WheelUAPI api = wheel.api;
                if (api == null) continue;
                bool grounded = api.IsGrounded;
                Vector3 contact = grounded ? api.HitPoint : Vector3.zero;
                float depth = grounded ? region.DepthAt(contact) : 0;
                if (grounded && (depth > .001f || surface != null && surface.Coverage01 > .001f) && wheel.hasContact)
                {
                    float distance = Vector2.Distance(new Vector2(contact.x, contact.z), new Vector2(wheel.lastContact.x, wheel.lastContact.z));
                    if (distance >= .05f && distance <= 5f)
                    {
                        float width = Mathf.Max(.15f, api.Width);
                        if (depth > .001f) region.CompressSweep(wheel.lastContact, contact, width, distance * Mathf.Max(0, compressionMetersPerMeter));
                        surface?.TraceWheel(i, wheel.lastContact, contact, width);
                    }
                    else surface?.StopWheelTrace(i);
                }
                else surface?.StopWheelTrace(i);
                wheel.hasContact = grounded;
                if (grounded) wheel.lastContact = contact;
                if (apply)
                {
                    float grip = GripForDepth(depth, region.MaximumDepth);
                    float rolling = RollingForDepth(depth, region.MaximumDepth);
                    CurrentGripMultiplier = Mathf.Min(CurrentGripMultiplier, grip);
                    CurrentRollingMultiplier = Mathf.Max(CurrentRollingMultiplier, rolling);
                    if (depth <= .001f) Restore(ref wheel);
                    else Apply(ref wheel, grip, rolling);
                }
                wheels[i] = wheel;
            }
        }
        public static float GripForDepth(float depth, float maximumDepth) =>
            Mathf.Lerp(1, .42f, Mathf.Clamp01(depth / Mathf.Max(.1f, maximumDepth)));
        public static float RollingForDepth(float depth, float maximumDepth) =>
            Mathf.Lerp(1, 2.5f, Mathf.Clamp01(depth / Mathf.Max(.1f, maximumDepth)));
        private static void Apply(ref Wheel wheel, float grip, float rolling)
        {
            WheelUAPI api = wheel.api;
            if (!wheel.modified || !Mathf.Approximately(api.LongitudinalFrictionGrip, wheel.appliedLongitudinal))
                wheel.longitudinal = api.LongitudinalFrictionGrip;
            if (!wheel.modified || !Mathf.Approximately(api.LateralFrictionGrip, wheel.appliedLateral))
                wheel.lateral = api.LateralFrictionGrip;
            if (!wheel.modified || !Mathf.Approximately(api.RollingResistanceTorque, wheel.appliedRolling))
                wheel.rolling = api.RollingResistanceTorque;
            wheel.appliedLongitudinal = wheel.longitudinal * grip;
            wheel.appliedLateral = wheel.lateral * grip;
            wheel.appliedRolling = wheel.rolling * rolling;
            api.LongitudinalFrictionGrip = wheel.appliedLongitudinal;
            api.LateralFrictionGrip = wheel.appliedLateral;
            api.RollingResistanceTorque = wheel.appliedRolling;
            wheel.modified = true;
        }
        private static void Restore(ref Wheel wheel)
        {
            if (!wheel.modified || wheel.api == null) return;
            WheelUAPI api = wheel.api;
            if (Mathf.Approximately(api.LongitudinalFrictionGrip, wheel.appliedLongitudinal)) api.LongitudinalFrictionGrip = wheel.longitudinal;
            if (Mathf.Approximately(api.LateralFrictionGrip, wheel.appliedLateral)) api.LateralFrictionGrip = wheel.lateral;
            if (Mathf.Approximately(api.RollingResistanceTorque, wheel.appliedRolling)) api.RollingResistanceTorque = wheel.rolling;
            wheel.modified = false;
        }
        private void OnDisable()
        {
            ClearModifiers();
        }
        public void ClearModifiers()
        {
            for (int i = 0; i < wheels.Count; i++)
            {
                Wheel wheel = wheels[i];
                Restore(ref wheel);
                wheels[i] = wheel;
            }
            CurrentGripMultiplier = 1;
            CurrentRollingMultiplier = 1;
        }
    }
}
