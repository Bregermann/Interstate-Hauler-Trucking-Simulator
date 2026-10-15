using System;
using System.Collections.Generic;
using LWS.InterstateHauler;
using NWH.Common.Vehicles;
using NWH.VehiclePhysics2;
using NWH.VehiclePhysics2.Powertrain;
using NWH.VehiclePhysics2.Powertrain.Wheel;
using UnityEngine;

namespace LWS.TruckTaxi
{
    [Serializable]
    public sealed class TruckTaxiDamageSnapshot
    {
        public int Version = 1;
        public string VehicleId;
        public TruckTaxiDamageEntry[] Parts;
        public float Heat01;
    }

    [Serializable]
    public sealed class TruckTaxiDamageEntry
    {
        public string Id;
        public float Condition;
    }

    public enum TruckTaxiDamageCategory { Body, Engine, Cooling, Transmission, Steering, Brakes, Suspension, Fuel, Light, Tire, Rim }

    public sealed class TruckTaxiDamagePart
    {
        public string Id { get; }
        public string DisplayName { get; }
        public TruckTaxiDamageCategory Category { get; }
        public long FullRepairCostCents { get; }
        public float Condition { get; internal set; } = 1f;
        public string State => Condition <= .22f ? "CRITICAL" : Condition <= .45f ? "DAMAGED" :
            Condition <= .75f ? "WORN" : "GOOD";
        public TruckTaxiDamagePart(string id, string name, TruckTaxiDamageCategory category, long cost)
        { Id = id; DisplayName = name; Category = category; FullRepairCostCents = cost; }
        public long RepairCostCents => Condition >= .999f ? 0 : Math.Max(100,
            (long)Math.Ceiling((1f - Condition) * FullRepairCostCents));
    }

    // Semantic component condition and a thin NWH effects adapter. The vendor remains the physics authority.
    [DisallowMultipleComponent]
    public sealed class TruckTaxiComponentDamage : MonoBehaviour
    {
        [Tooltip("Impact energy below this speed does not create new localized damage.")]
        [Min(0)] public float minimumDamageSpeed = 4f;
        [Tooltip("Condition lost at roughly 10 m/s before region weighting.")]
        [Range(.01f, .6f)] public float impactDamageAtTenMetersPerSecond = .14f;
        [Tooltip("A failed radiator gradually raises an abstract overheating meter.")]
        [Min(.01f)] public float overheatingSeconds = 100f;
        [Tooltip("Fuel lost per second from a critically damaged tank, using the NWH tank.")]
        [Min(0)] public float criticalFuelLeakLitersPerSecond = .025f;
        public string VehicleId => host?.Player?.VehicleId;
        public float Heat01 { get; private set; }
        public string LastWarning { get; private set; } = string.Empty;
        public IReadOnlyDictionary<string, TruckTaxiDamagePart> Parts => parts;
        // Repair-cost weighting gives the same summary before and after restoring the authoritative parts.
        public float SummaryDamage01 => CalculateSummary(parts.Values);
        public static float CalculateSummary(IEnumerable<TruckTaxiDamagePart> values)
        {
            double loss = 0, total = 0;
            foreach (var part in values)
            { total += part.FullRepairCostCents; loss += (1 - part.Condition) * part.FullRepairCostCents; }
            return total > 0 ? Mathf.Clamp01((float)(loss / total)) : 0;
        }
        public long TotalRepairCostCents
        {
            get { long total = 0; foreach (var part in parts.Values) total += part.RepairCostCents; return total; }
        }
        public long RepairCostFor(TruckTaxiServiceCapability capabilities)
        {
            long total = 0;
            foreach (var part in parts.Values)
                if (CanRepair(part, capabilities)) total += part.RepairCostCents;
            return total;
        }
        private static bool CanRepair(TruckTaxiDamagePart part, TruckTaxiServiceCapability capabilities)
        {
            var required = part.Category == TruckTaxiDamageCategory.Tire || part.Category == TruckTaxiDamageCategory.Rim ?
                TruckTaxiServiceCapability.RepairTires : TruckTaxiServiceCapability.RepairGeneralDamage;
            return (capabilities & required) == required;
        }
        private readonly Dictionary<string, TruckTaxiDamagePart> parts = new Dictionary<string, TruckTaxiDamagePart>(StringComparer.Ordinal);
        private readonly List<WheelComponent> wheels = new List<WheelComponent>();
        private readonly List<string> tireIds = new List<string>();
        private readonly List<string> rimIds = new List<string>();
        private readonly List<string> suspensionIds = new List<string>();
        private readonly List<Vector3> suspensionBaseline = new List<Vector3>();
        private readonly List<Light> leftHeadlights = new List<Light>(), rightHeadlights = new List<Light>();
        private readonly Dictionary<Light,NWH.VehiclePhysics2.Effects.LightSource> headlightSources =
            new Dictionary<Light,NWH.VehiclePhysics2.Effects.LightSource>();
        private TruckTaxiBootstrap host;
        private VehicleController vehicle;
        private Rigidbody body;
        private TruckTaxiSnowTraction snow;
        private float originalBrakeTorque, nextTick;
        private float careerBrakeMultiplier = 1;
        private NWH.VehiclePhysics2.Damage.DamageHandler nativeDamage;
        private bool originalVisualOnly;
        public void SetCareerBrakeMultiplier(float multiplier)
        {
            careerBrakeMultiplier = float.IsFinite(multiplier) ? Mathf.Clamp(multiplier,1,1.5f) : 1;
            ApplyMechanical();
        }

        public void Initialize(TruckTaxiBootstrap owner)
        {
            if (host != null) return;
            host = owner; vehicle = owner.Player.GetComponent<VehicleController>(); body = owner.Player.GetComponent<Rigidbody>();
            if (vehicle?.powertrain?.wheels == null) return;
            nativeDamage = vehicle.GetComponent<NWH.VehiclePhysics2.Damage.DamageHandler>();
            if (nativeDamage != null)
            {
                originalVisualOnly = nativeDamage.visualOnly;
                nativeDamage.visualOnly = true;
            }
            Add("body.front.left", "Front Left Body", TruckTaxiDamageCategory.Body, 85000);
            Add("body.front.right", "Front Right Body", TruckTaxiDamageCategory.Body, 85000);
            Add("body.rear", "Rear Body", TruckTaxiDamageCategory.Body, 62000);
            Add("body.cab", "Cab", TruckTaxiDamageCategory.Body, 130000);
            Add("engine", "Engine", TruckTaxiDamageCategory.Engine, 350000);
            Add("radiator", "Radiator", TruckTaxiDamageCategory.Cooling, 120000);
            Add("transmission", "Transmission", TruckTaxiDamageCategory.Transmission, 275000);
            Add("steering", "Steering", TruckTaxiDamageCategory.Steering, 160000);
            Add("brakes.front", "Front Brakes", TruckTaxiDamageCategory.Brakes, 95000);
            Add("brakes.rear", "Rear Brakes", TruckTaxiDamageCategory.Brakes, 115000);
            Add("suspension.front.left", "Front Left Suspension", TruckTaxiDamageCategory.Suspension, 85000);
            Add("suspension.front.right", "Front Right Suspension", TruckTaxiDamageCategory.Suspension, 85000);
            Add("suspension.rear.left", "Rear Left Suspension", TruckTaxiDamageCategory.Suspension, 90000);
            Add("suspension.rear.right", "Rear Right Suspension", TruckTaxiDamageCategory.Suspension, 90000);
            Add("fuel.tank", "Fuel Tank", TruckTaxiDamageCategory.Fuel, 120000);
            Add("light.head.left", "Left Headlight", TruckTaxiDamageCategory.Light, 20000);
            Add("light.head.right", "Right Headlight", TruckTaxiDamageCategory.Light, 20000);
            RegisterWheels();
            originalBrakeTorque = vehicle.brakes.maxTorque;
            RefreshHeadlightBindings();
            snow = owner.Player.GetComponent<TruckTaxiSnowTraction>();
            if (snow != null) { snow.TireGripMultiplier = TireGripMultiplier; snow.TireRollingMultiplier = TireRollingMultiplier; }
            owner.Handling.MechanicalPowerMultiplier = PowerMultiplier;
            ApplyMechanical();
        }
        private void Add(string id, string label, TruckTaxiDamageCategory category, long cost) =>
            parts.Add(id, new TruckTaxiDamagePart(id, label, category, cost));
        private struct WheelPosition { public WheelComponent wheel; public float x, z; }
        private void RegisterWheels()
        {
            var positions = new List<WheelPosition>();
            foreach (WheelComponent wheel in vehicle.powertrain.wheels)
            {
                if (wheel?.wheelUAPI == null) continue;
                Vector3 local = vehicle.transform.InverseTransformPoint(wheel.wheelUAPI.transform.position);
                positions.Add(new WheelPosition { wheel = wheel, x = local.x, z = local.z });
            }
            positions.Sort((a, b) => b.z.CompareTo(a.z));
            var assigned = new Dictionary<WheelComponent, string>();
            int axle = 0;
            for (int first = 0; first < positions.Count;)
            {
                int last = first + 1;
                while (last < positions.Count && Mathf.Abs(positions[last].z - positions[first].z) < .4f) last++;
                var group = positions.GetRange(first, last - first);
                group.Sort((a, b) => a.x.CompareTo(b.x));
                int left = 0, right = 0;
                foreach (var position in group)
                {
                    bool isLeft = position.x < 0;
                    string id = "axle" + (axle + 1) + "." + (isLeft ? "left." + ++left : "right." + ++right);
                    assigned[position.wheel] = id;
                }
                axle++; first = last;
            }
            foreach (WheelComponent wheel in vehicle.powertrain.wheels)
            {
                if (wheel?.wheelUAPI == null || !assigned.TryGetValue(wheel, out string suffix)) continue;
                wheels.Add(wheel);
                string tire = "tire." + suffix, rim = "rim." + suffix;
                tireIds.Add(tire); rimIds.Add(rim);
                suspensionIds.Add("suspension." + (suffix.StartsWith("axle1.", StringComparison.Ordinal) ? "front." : "rear.") +
                    (suffix.Contains(".left.") ? "left" : "right"));
                suspensionBaseline.Add(new Vector3(wheel.wheelUAPI.SpringMaxForce,
                    wheel.wheelUAPI.DamperBumpRate, wheel.wheelUAPI.DamperReboundRate));
                Add(tire, "Tire " + suffix, TruckTaxiDamageCategory.Tire, 65000);
                Add(rim, "Wheel/Rim " + suffix, TruckTaxiDamageCategory.Rim, 120000);
            }
        }
        public static bool ValidSnapshot(TruckTaxiDamageSnapshot snapshot, string vehicleId, IReadOnlyDictionary<string, TruckTaxiDamagePart> available)
        {
            if (snapshot == null) return true; // Existing saves predate component damage.
            if (!ValidSnapshotData(snapshot) || snapshot.VehicleId != vehicleId || available == null) return false;
            foreach (var entry in snapshot.Parts) if (!available.ContainsKey(entry.Id)) return false;
            return true;
        }
        public static bool ValidSnapshotData(TruckTaxiDamageSnapshot snapshot)
        {
            if (snapshot == null) return true;
            if (snapshot.Version != 1 || snapshot.Parts == null || snapshot.Parts.Length > 512 ||
                !float.IsFinite(snapshot.Heat01) || snapshot.Heat01 < 0 || snapshot.Heat01 > 1) return false;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entry in snapshot.Parts)
                if (entry == null || string.IsNullOrWhiteSpace(entry.Id) ||
                    !seen.Add(entry.Id) || !float.IsFinite(entry.Condition) || entry.Condition < 0 || entry.Condition > 1) return false;
            return true;
        }
        public TruckTaxiDamageSnapshot Capture()
        {
            var entries = new List<TruckTaxiDamageEntry>(parts.Count);
            foreach (var pair in parts) entries.Add(new TruckTaxiDamageEntry { Id = pair.Key, Condition = pair.Value.Condition });
            entries.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            return new TruckTaxiDamageSnapshot { VehicleId = VehicleId, Parts = entries.ToArray(), Heat01 = Heat01 };
        }
        public bool Restore(TruckTaxiDamageSnapshot snapshot)
        {
            if (!ValidSnapshot(snapshot, VehicleId, parts)) return false;
            foreach (var part in parts.Values) part.Condition = 1;
            if (snapshot != null) foreach (var entry in snapshot.Parts) parts[entry.Id].Condition = entry.Condition;
            Heat01 = snapshot?.Heat01 ?? 0; ApplyMechanical(); return true;
        }
        public bool Damage(string stablePartId, float amount)
        {
            if (!parts.TryGetValue(stablePartId, out var part) || !float.IsFinite(amount) || amount <= 0) return false;
            part.Condition = Mathf.Clamp01(part.Condition - amount);
            LastWarning = part.DisplayName + " " + part.State;
            ApplyMechanical(); return true;
        }
        public void ReceiveImpact(Vector3 contact, float relativeSpeed, float impulse)
        {
            if (vehicle == null || !float.IsFinite(relativeSpeed) || relativeSpeed < minimumDamageSpeed) return;
            Vector3 local = vehicle.transform.InverseTransformPoint(contact);
            float severity = Mathf.Clamp((relativeSpeed * relativeSpeed / 100f) * impactDamageAtTenMetersPerSecond, 0, .65f);
            if (float.IsFinite(impulse) && impulse > 0) severity *= Mathf.Clamp(impulse / Mathf.Max(1, body.mass * 4), .35f, 1.4f);
            bool front = local.z > .4f, left = local.x < 0;
            string side = left ? "left" : "right";
            string bodyId = front ? "body.front." + side : local.z < -1 ? "body.rear" : "body.cab";
            Damage(bodyId, severity);
            int wheelIndex = NearestWheel(contact, 1.15f);
            if (wheelIndex >= 0)
            {
                Damage(tireIds[wheelIndex], severity * 1.35f);
                Damage(rimIds[wheelIndex], severity * .6f);
                Damage(suspensionIds[wheelIndex], severity * .55f);
            }
            else if (front)
            {
                Damage("radiator", severity * .8f);
                if (severity > .2f) Damage("engine", severity * .22f);
                if (severity > .3f) Damage("steering", severity * .25f);
                Damage("light.head." + side, severity * .7f);
            }
            else if (local.z < -1 && severity > .25f) Damage("fuel.tank", severity * .3f);
        }
        public string InspectPartAt(Vector3 contact)
        {
            if (vehicle == null || !float.IsFinite(contact.x) || !float.IsFinite(contact.y) || !float.IsFinite(contact.z)) return null;
            int wheel = NearestWheel(contact, 1.15f);
            if (wheel >= 0) return tireIds[wheel];
            return BodyPartAtLocalPosition(vehicle.transform.InverseTransformPoint(contact));
        }
        public static string BodyPartAtLocalPosition(Vector3 local) => local.z > .4f ?
            "body.front." + (local.x < 0 ? "left" : "right") : local.z < -1 ? "body.rear" : "body.cab";
        private int NearestWheel(Vector3 contact, float maximumDistance)
        {
            float best = maximumDistance * maximumDistance; int index = -1;
            for (int i = 0; i < wheels.Count; i++)
            {
                float sqr = (wheels[i].wheelUAPI.transform.position - contact).sqrMagnitude;
                if (sqr >= best) continue;
                best = sqr; index = i;
            }
            return index;
        }
        private float Condition(string id) => parts.TryGetValue(id, out var part) ? part.Condition : 1f;
        public float TireGripMultiplier(int index) => index >= 0 && index < tireIds.Count ?
            GripForCondition(Mathf.Min(Condition(tireIds[index]), Condition(rimIds[index]) * 1.2f)) : 1f;
        public static float GripForCondition(float condition) => Mathf.Lerp(.92f, 1f, Mathf.Clamp01(condition));
        public float TireRollingMultiplier(int index) => index >= 0 && index < tireIds.Count ?
            Mathf.Lerp(1.15f, 1f, Condition(tireIds[index])) : 1f;
        private float PowerMultiplier() => Mathf.Clamp(.25f + Condition("engine") * .75f - Heat01 * .32f, .15f, 1f);
        private void ApplyMechanical()
        {
            if (vehicle == null) return;
            vehicle.powertrain.engine.Damage = 1f - Condition("engine");
            vehicle.powertrain.transmission.Damage = 1f - Condition("transmission");
            vehicle.brakes.maxTorque = originalBrakeTorque * careerBrakeMultiplier * Mathf.Lerp(.4f, 1f,
                (Condition("brakes.front") + Condition("brakes.rear")) * .5f);
            host.Handling?.SetDamageSteeringMultiplier(1);
            for (int i = 0; i < wheels.Count; i++)
                if (wheels[i].wheelUAPI != null)
                {
                    // Native wheel damage may alter geometry. Taxi tire wear only uses bounded grip/drag.
                    wheels[i].wheelUAPI.Damage = 0;
                    float suspension = Condition(suspensionIds[i]);
                    wheels[i].wheelUAPI.SpringMaxForce = suspensionBaseline[i].x * Mathf.Lerp(.62f, 1f, suspension);
                    wheels[i].wheelUAPI.DamperBumpRate = suspensionBaseline[i].y * Mathf.Lerp(.48f, 1f, suspension);
                    wheels[i].wheelUAPI.DamperReboundRate = suspensionBaseline[i].z * Mathf.Lerp(.48f, 1f, suspension);
                }
            ApplyHeadlights(leftHeadlights,"light.head.left");
            ApplyHeadlights(rightHeadlights,"light.head.right");
        }
        private void RefreshHeadlightBindings()
        {
            leftHeadlights.Clear();rightHeadlights.Clear();headlightSources.Clear();
            if(vehicle==null || host?.Player==null) return;
            var manager=vehicle.effectsManager.lightsManager;
            foreach(var group in new[]{manager.lowBeamLights,manager.highBeamLights})
                if(group?.lightSources!=null) foreach(var source in group.lightSources)
                    if(source?.light!=null) headlightSources[source.light]=source;
            foreach(var light in host.Player.GetComponentsInChildren<Light>(true))
            {
                string label=light.name.ToLowerInvariant();
                if(!headlightSources.ContainsKey(light) && !label.Contains("head") && !label.Contains("low beam")) continue;
                if(host.Player.transform.InverseTransformPoint(light.transform.position).x<0) leftHeadlights.Add(light);
                else rightHeadlights.Add(light);
            }
        }
        private void ApplyHeadlights(List<Light> lights,string part)
        {
            foreach(var light in lights) if(light!=null)
                light.enabled=Condition(part)>.1f && (!headlightSources.TryGetValue(light,out var source) || source.IsOn);
        }
        public void ReapplyPhysicalState() { RefreshHeadlightBindings();ApplyMechanical(); }
        private void Update()
        {
            if (host?.Ready != true || Time.time < nextTick || host.Paused) return;
            const float interval = .5f;
            nextTick = Time.time + interval;
            float radiator = Condition("radiator");
            Heat01 = Mathf.Clamp01(Heat01 + (vehicle.powertrain.engine.IsRunning && radiator < .7f ?
                (1f - radiator) * interval / overheatingSeconds : -interval / 90f));
            if (Heat01 > .95f) Damage("engine", interval * .0005f);
            if (Condition("fuel.tank") < .4f && host.Fuel?.Fuel != null)
                host.Fuel.Fuel.amount = Mathf.Max(0, host.Fuel.Fuel.amount - criticalFuelLeakLitersPerSecond * interval);
            if (body.linearVelocity.magnitude > 5)
                for (int i = 0; i < tireIds.Count; i++)
                    if (Condition(tireIds[i]) < .2f) Damage(rimIds[i], body.linearVelocity.magnitude * interval * .00002f);
        }
        public bool TryRepair(string id)
        {
            if (RepairBlockReason(id) != null || !parts.TryGetValue(id, out var part)) return false;
            if (!host.Session.TrySpend(part.RepairCostCents)) return false;
            part.Condition = 1f; ApplyMechanical(); LastWarning = part.DisplayName + " REPAIRED"; return true;
        }
        public string RepairBlockReason(string id)
        {
            if (!parts.TryGetValue(id, out var part)) return "Part unavailable";
            if (part.RepairCostCents <= 0) return "No repair needed";
            if (host?.WorldLife?.IsOnFoot != true) return "Exit the truck at a repair service";
            if (body == null || body.linearVelocity.magnitude > TruckTaxiServicePoint.ServiceStopSpeedMetersPerSecond)
                return "Stop the truck";
            var capability = part.Category == TruckTaxiDamageCategory.Tire || part.Category == TruckTaxiDamageCategory.Rim
                ? TruckTaxiServiceCapability.RepairTires : TruckTaxiServiceCapability.RepairGeneralDamage;
            var service = TruckTaxiServicePoint.Nearest(body.position, capability, host.gameObject.scene, true);
            if (service == null || !service.CanUse(body.position, body.linearVelocity.magnitude,
                TruckTaxiServicePoint.ServiceStopSpeedMetersPerSecond))
                return capability == TruckTaxiServiceCapability.RepairTires ? "Tire service required" : "General repair service required";
            return null;
        }
        public void RepairAllAfterAssistance()
        {
            RepairForCapabilities(TruckTaxiServiceCapability.RepairGeneralDamage | TruckTaxiServiceCapability.RepairTires);
        }
        public void RepairForCapabilities(TruckTaxiServiceCapability capabilities)
        {
            foreach (var part in parts.Values)
                if (CanRepair(part, capabilities)) part.Condition = 1f;
            Heat01 = 0; ApplyMechanical();
        }
        private void OnDestroy()
        {
            if (nativeDamage != null) nativeDamage.visualOnly = originalVisualOnly;
            if (snow != null && snow.TireGripMultiplier == TireGripMultiplier)
            { snow.TireGripMultiplier = null; snow.TireRollingMultiplier = null; snow.ClearModifiers(); }
            if (host?.Handling != null)
            { host.Handling.MechanicalPowerMultiplier = null; host.Handling.SetDamageSteeringMultiplier(1); }
            if (vehicle != null && originalBrakeTorque > 0) vehicle.brakes.maxTorque = originalBrakeTorque;
            for (int i = 0; i < wheels.Count; i++)
                if (wheels[i]?.wheelUAPI != null)
                {
                    wheels[i].wheelUAPI.SpringMaxForce = suspensionBaseline[i].x;
                    wheels[i].wheelUAPI.DamperBumpRate = suspensionBaseline[i].y;
                    wheels[i].wheelUAPI.DamperReboundRate = suspensionBaseline[i].z;
                }
        }
    }
}
