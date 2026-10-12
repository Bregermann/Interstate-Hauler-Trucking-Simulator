using System.Collections;
using LWS.InterstateHauler;
using NWH.VehiclePhysics2;
using NWH.VehiclePhysics2.Damage;
using NWH.VehiclePhysics2.Modules.Trailer;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // Taxi service transaction. Existing NWH/LWS recovery still owns the connected combination pose.
    [DisallowMultipleComponent]
    public sealed class TruckTaxiRoadsideAssistance : MonoBehaviour
    {
        [Header("Tow pricing (integer cents; fuel and repairs are separate)")]
        [Min(0)] public int towCalloutCents=1500;
        [Min(0)] public int towCentsPerMile=400;
        [Min(0)] public int overturnedSurchargeCents=1000;
        [Min(0)] public int minimumTowCents=1500;
        [Min(0)] public int maximumTowCents=6000;
        public bool IsRecovering { get; private set; }
        public float FadeAlpha { get; private set; }
        public string Feedback { get; private set; } = "";
        public string Condition { get; private set; } = "VEHICLE OK";
        public string Diagnostics { get; private set; } = "";
        public TruckTaxiServicePoint QuotedDestination { get; private set; }
        public long QuotedCostCents { get; private set; }
        public long RepairCostCents => vehicle == null ? 0 : (long)System.Math.Ceiling(
            (Mathf.Clamp01(host?.ComponentDamage == null && damage != null ? damage.Damage : 0) +
             (host?.ComponentDamage == null ? Mathf.Clamp01(vehicle.powertrain.engine.Damage) +
             Mathf.Clamp01(vehicle.powertrain.transmission.Damage) : 0)) * 5000) +
             (host?.ComponentDamage?.TotalRepairCostCents ?? 0);
        private long RepairCostFor(TruckTaxiServicePoint service)
        {
            if (service == null) return 0;
            long vendorCost = service.Supports(TruckTaxiServiceCapability.RepairGeneralDamage) ?
                RepairCostCents - (host?.ComponentDamage?.TotalRepairCostCents ?? 0) : 0;
            return vendorCost + (host?.ComponentDamage?.RepairCostFor(service.capabilities) ?? 0);
        }
        public bool NeedsRepair => RepairCostCents > 0;
        public bool CanRequest => host != null && host.Ready && !IsRecovering && host.Fuel?.IsRescuing != true &&
            (host.Session.State == TruckTaxiState.Available || host.Session.State == TruckTaxiState.DrivingToPickup ||
             host.Session.State == TruckTaxiState.DrivingToDestination);
        private TruckTaxiBootstrap host;
        private VehicleController vehicle;
        private DamageHandler damage;
        private Rigidbody body;
        private TruckTaxiSnowTraction traction;
        private float nextStatus;

        public void Initialize(TruckTaxiBootstrap owner)
        {
            if (host != null) return;
            host = owner; vehicle = owner.Player.GetComponent<VehicleController>(); body = vehicle.vehicleRigidbody;
            damage = vehicle.GetComponent<DamageHandler>(); traction = vehicle.GetComponent<TruckTaxiSnowTraction>();
            RefreshCondition();
        }
        private void Update()
        { if (host != null && Time.unscaledTime >= nextStatus) { nextStatus = Time.unscaledTime + .2f; RefreshCondition(); } }
        public static string Diagnose(float fuel, bool engineEnabled, bool engineRunning, float engineDamage,
            float transmissionDamage, float generalDamage, float parkingBrake, float brake, int gear, float snowDepth)
        {
            if (fuel <= 0) return "OUT OF FUEL - CALL TOW TRUCK";
            if (engineDamage >= .95f) return "ENGINE DAMAGED - SEVERE / CALL TOW";
            if (transmissionDamage >= .95f) return "TRANSMISSION DAMAGED - CALL TOW";
            if (!engineEnabled) return "ENGINE DISABLED - CALL TOW TRUCK";
            if (!engineRunning) return "ENGINE STOPPED";
            if (parkingBrake > .02f) return "PARKING BRAKE ON";
            if (brake > .02f) return "SERVICE BRAKE ON";
            if (gear == 0) return "TRANSMISSION NEUTRAL";
            if (snowDepth >= .4f) return "DEEP SNOW - HIGH RESISTANCE / TOW AVAILABLE";
            if (generalDamage >= .7f) return "SEVERE DAMAGE - REPAIR ADVISED";
            if (engineDamage >= .1f) return "ENGINE DAMAGE - REDUCED POWER";
            if (transmissionDamage >= .1f) return "TRANSMISSION DAMAGE - REPAIR ADVISED";
            if (fuel < .15f) return "LOW FUEL";
            return "VEHICLE OK";
        }
        public void RefreshCondition()
        {
            if (vehicle == null) return;
            var engine = vehicle.powertrain.engine; var transmission = vehicle.powertrain.transmission;
            float depth = host.Environment?.Snow?.Region?.DepthAt(body.position) ?? 0;
            Condition = Diagnose(host.Fuel?.Fraction ?? 1, engine.state.isEnabled, engine.IsRunning, engine.Damage,
                transmission.Damage, host.ComponentDamage?.SummaryDamage01 ?? (damage != null ? damage.Damage : 0), vehicle.input.Handbrake, vehicle.input.Brakes, transmission.Gear, depth);
            Diagnostics = $"FUEL {(host.Fuel?.Fraction ?? 1):P0} | ENGINE RUNNING {engine.IsRunning} / ENABLED {engine.state.isEnabled}\n" +
                $"DAMAGE {(host.ComponentDamage?.SummaryDamage01 ?? (damage != null ? damage.Damage : 0)):P0} | ENGINE {engine.Damage:P0} / TRANS {transmission.Damage:P0} / GEAR {transmission.GearName}\n" +
                $"PARK {vehicle.input.Handbrake:0.00} / BRAKE {vehicle.input.Brakes:0.00} | SNOW {depth:0.00}m\n" +
                $"GRIP {(traction != null ? traction.CurrentGripMultiplier : 1):0.00} / RESISTANCE {(traction != null ? traction.CurrentRollingMultiplier : 1):0.00}\n" +
                Condition + " | TOW " + (CanRequest ? "AVAILABLE" : "UNAVAILABLE");
        }
        public bool PrepareQuote()
        {
            if (!CanRequest) return false;
            var required = TruckTaxiServiceCapability.RecoverVehicle;
            if (host.Fuel?.Fraction <= 0) required |= TruckTaxiServiceCapability.Refuel;
            if (vehicle.powertrain.engine.Damage >= .1f) required |= TruckTaxiServiceCapability.RepairEngine;
            if (vehicle.powertrain.transmission.Damage >= .1f) required |= TruckTaxiServiceCapability.RepairTransmission;
            QuotedDestination = TruckTaxiServicePoint.Nearest(body.position, required, host.gameObject.scene);
            if (QuotedDestination == null) { Feedback = "No recovery service has been authored in this level."; return false; }
            QuotedCostCents = CalculateTowCost(Vector3.Distance(body.position,QuotedDestination.RecoveryPosition)/1609.344f,
                Vector3.Dot(body.transform.up,Vector3.up)<.4f,towCalloutCents,towCentsPerMile,overturnedSurchargeCents,minimumTowCents,maximumTowCents);
            Feedback = QuotedDestination.displayName + "\n" + TruckTaxiHud.Money(QuotedCostCents) +
                "\nTOW ONLY - FUEL AND REPAIRS PURCHASED SEPARATELY";
            return true;
        }
        public static long CalculateTowCost(float miles,bool overturned,int callout=1500,int perMile=400,int severity=1000,int minimum=1500,int maximum=6000)
        {
            if(!float.IsFinite(miles) || miles<0) return System.Math.Max(0,maximum);
            double quoted=System.Math.Max(0,callout)+(double)miles*System.Math.Max(0,perMile)+(overturned ? System.Math.Max(0,severity) : 0);
            return (long)System.Math.Ceiling(System.Math.Clamp(quoted,System.Math.Max(0,minimum),System.Math.Max(System.Math.Max(0,minimum),maximum)));
        }
        public bool ConfirmTow()
        {
            if (!CanRequest || QuotedDestination == null || !QuotedDestination.Supports(TruckTaxiServiceCapability.RecoverVehicle)) return false;
            StartCoroutine(Recover(QuotedDestination, QuotedCostCents)); return true;
        }
        public bool RepairAt(TruckTaxiServicePoint service)
        {
            if (!CanRequest || service == null || !service.Supports(TruckTaxiServiceCapability.RepairGeneralDamage) ||
                body == null || !service.CanUse(body.position, body.linearVelocity.magnitude,
                    TruckTaxiServicePoint.ServiceStopSpeedMetersPerSecond))
            { Feedback = "Stop inside a repair bay first."; return false; }
            long cost = RepairCostFor(service);
            if (cost <= 0) { Feedback = "Vehicle already repaired."; return false; }
            if (host.Session.WalletBalanceCents < cost || !host.Session.TrySpend(cost))
            { Feedback = "Not Enough Cash for Repair"; return false; }
            if (damage != null) damage.Repair();
            vehicle.powertrain.engine.Damage = 0;
            vehicle.powertrain.transmission.Damage = 0;
            if (service.Supports(TruckTaxiServiceCapability.RepairTires))
                foreach (var wheel in vehicle.powertrain.wheels) if (wheel.wheelUAPI != null) wheel.wheelUAPI.Damage = 0;
            host.ComponentDamage?.RepairForCapabilities(service.capabilities);
            Feedback = "Vehicle repaired for " + TruckTaxiHud.Money(cost) + ".";
            RefreshCondition();
            if (host.Session.HasPassenger && host.Session.Passenger != null)
                host.Passengers?.Dialogue?.Speak(host.Session.Passenger,
                    TruckTaxiDialogueCategory.RepairReaction, host.Session);
            return true;
        }
        private IEnumerator Recover(TruckTaxiServicePoint service, long cost)
        {
            IsRecovering = true;
            host.DriverNeeds?.State?.CancelJug();
            host.Session.CancelTemporaryActionsForRecovery();
            host.SetPaused(true);
            Rigidbody trailer = AttachedTrailerBody();
            Stop(body); Stop(trailer);
            try
            {
                for (float t = 0; t < .35f; t += Time.unscaledDeltaTime)
                { FadeAlpha = Mathf.Clamp01(t / .35f); yield return null; }
                FadeAlpha = 1;
                if (service == null) { Feedback = "Recovery destination is no longer available."; yield break; }
                var regional = host.GetComponent<TruckTaxiRegionalWorld>();
                if (regional != null && !regional.IsPositionAvailable(service.RecoveryPosition))
                {
                    yield return regional.PrepareInitialWorld(service.RecoveryPosition);
                    if (!regional.IsPositionAvailable(service.RecoveryPosition))
                    { Feedback = "Recovery region could not be prepared. No charge."; yield break; }
                }
                Vector3 delta = service.RecoveryPosition + Vector3.up * 1.6f - body.position;
                Vector3 originalPosition = body.position;
                Vector3 trailerPosition = trailer != null ? trailer.position : Vector3.zero;
                Move(body, delta); Move(trailer, delta);
                Physics.SyncTransforms();
                if (!host.Player.UprightRecoveryController.RequestResetUpright("Truck Taxi roadside assistance"))
                {
                    Move(body, originalPosition - body.position);
                    if (trailer != null) Move(trailer, trailerPosition - trailer.position);
                    Physics.SyncTransforms();
                    Feedback = "Upright recovery could not complete. No charge."; yield break;
                }
                // Recovery sells relocation/uprighting, not an unrequested full tank and overhaul.
                var controls = host.Player.GetComponent<LwsTruckControlController>();
                if (controls != null) controls.ApplyCommandFrame(new LwsVehicleCommandFrame {
                    parkingBrakeToggle = controls.CurrentState.parkingBrakeOn ? LwsMomentaryIntent.Pressed : LwsMomentaryIntent.None,
                    cruiseCancel = LwsMomentaryIntent.Pressed }, default);
                vehicle.input.Brakes = vehicle.input.Handbrake = 0;
                vehicle.powertrain.engine.VC_Enable(false); vehicle.powertrain.transmission.VC_Enable(false);
                vehicle.powertrain.engine.StartEngine();
                host.Session.DiscardTeleportDistance(); host.Session.ChargeService(cost);
                host.Session.ApplyRoadsideAssistanceConsequence();
                Feedback = "RECOVERED TO " + service.displayName + "  " + TruckTaxiHud.Money(cost);
                if (host.Session.HasPassenger && host.Session.Passenger != null)
                    host.Passengers?.Dialogue?.Speak(host.Session.Passenger,
                        TruckTaxiDialogueCategory.RepairReaction, host.Session);
                yield return new WaitForSecondsRealtime(.3f);
                for (float t = .35f; t > 0; t -= Time.unscaledDeltaTime) { FadeAlpha = t / .35f; yield return null; }
            }
            finally { FadeAlpha = 0; IsRecovering = false; host.SetPaused(host.Session.State == TruckTaxiState.RideFailed); RefreshCondition(); }
        }
        private Rigidbody AttachedTrailerBody()
        {
            var hitch = vehicle.GetComponent<TrailerHitchModuleWrapper>();
            return hitch != null && hitch.module.attached ? hitch.module.attachedTrailerModule?.vehicleController?.vehicleRigidbody : null;
        }
        private static void Stop(Rigidbody target) { if (target != null) target.linearVelocity = target.angularVelocity = Vector3.zero; }
        private static void Move(Rigidbody target, Vector3 delta)
        { if (target == null) return; Stop(target); target.position += delta; target.transform.position = target.position; }
    }
}
