using System;
using LWS.TruckSports;
using NWH.VehiclePhysics2.Damage;
using UnityEngine;

namespace LWS.TruckDragonKingdom
{
    // Elemental semantics over the existing NWH damage/powertrain. No second
    // vehicle-health or wheel/traction simulation competes with the vendor.
    public sealed class DragonTruckCondition : MonoBehaviour
    {
        public SportsEventTruck truck;
        public float Health => truck?.tractorMotor?.vehicle == null ? 0 : 1 - Mathf.Max(
            truck.tractorMotor.vehicle.powertrain.engine.Damage, truck.tractorMotor.vehicle.powertrain.transmission.Damage);
        public bool Destroyed => Health <= .001f;
        public bool Stunned => Time.time < stunnedUntil;
        public float GripMultiplier => Time.time < icedUntil ? .6f : 1;
        public float LastHitAt { get; private set; } = float.NegativeInfinity;
        public DragonElement LastElement { get; private set; }
        public event Action<DragonElement, Vector3> Hit;
        float stunnedUntil, icedUntil;

        public bool ApplyDragonHit(DragonElement element, float damage, Vector3 point)
        {
            if (truck?.tractorMotor?.vehicle == null || Destroyed || !float.IsFinite(damage) || damage <= 0
                || Time.time - LastHitAt < .35f) return false;
            LastHitAt = Time.time; LastElement = element;
            var powertrain = truck.tractorMotor.vehicle.powertrain;
            float bounded = Mathf.Clamp(damage, .01f, .35f);
            powertrain.engine.Damage += bounded;
            powertrain.transmission.Damage += bounded * .4f;
            if (element == DragonElement.Ice) icedUntil = Mathf.Max(icedUntil, Time.time + 3);
            if (element == DragonElement.Lightning) stunnedUntil = Mathf.Max(stunnedUntil, Time.time + .65f);
            Hit?.Invoke(element, point); return true;
        }
        public void RepairForNewAttempt()
        {
            if (truck == null) throw new InvalidOperationException("Dragon condition must bind its actual Sports/NWH tractor.");
            truck.GetComponent<DamageHandler>()?.Repair();
            if (truck.tractorMotor?.vehicle != null)
            {
                ClearInterruptedRevLimiter();
                truck.tractorMotor.vehicle.powertrain.engine.Damage = 0;
                truck.tractorMotor.vehicle.powertrain.transmission.Damage = 0;
            }
            stunnedUntil = icedUntil = 0; LastHitAt = float.NegativeInfinity;
        }
        public void ClearInterruptedRevLimiter()
        {
            // Call only after the old vehicle has been disabled. NWH cancels
            // its cutoff coroutine there without clearing this public transient,
            // which otherwise suppresses power after a restart. In-place
            // recovery uses this without repairing the existing damage.
            // Normal live limiter timing and torque remain vendor-owned.
            if (truck?.tractorMotor?.vehicle != null)
                truck.tractorMotor.vehicle.powertrain.engine.revLimiterActive = false;
        }
    }
}
