using NWH.VehiclePhysics2;

namespace LWS.TruckTaxi
{
    // Shared copy for the existing WorldLife inspection screen; no second panel or input owner.
    public static class TruckTaxiConditionPresentation
    {
        public static string Summary(TruckTaxiBootstrap host)
        {
            if (host?.ComponentDamage == null) return "TRUCK CONDITION UNAVAILABLE";
            var vehicle = host.Player != null ? host.Player.GetComponent<VehicleController>() : null;
            return $"DAMAGE {host.ComponentDamage.SummaryDamage01:P0}  |  HEAT {host.ComponentDamage.Heat01:P0}\n" +
                $"Engine upgrade x{host.Handling?.CareerPowerMultiplier ?? 1:0.00}  |  Brakes {vehicle?.brakes.maxTorque ?? 0:0} Nm  |  Tank {host.Fuel?.Fuel?.capacity ?? 0:0} L";
        }
        public static string Part(TruckTaxiComponentDamage damage, string id)
        {
            if (damage == null || !damage.Parts.TryGetValue(id ?? "", out var part)) return "PART UNAVAILABLE";
            return $"{part.DisplayName}  |  {part.Condition:P0}  {part.State}\n{Consequence(part.Category)}\n" +
                $"Repair ${(part.RepairCostCents / 100m):0.00}  |  {damage.RepairBlockReason(id) ?? "Service available; payment required"}";
        }
        public static string Consequence(TruckTaxiDamageCategory category)
        {
            switch (category)
            {
                case TruckTaxiDamageCategory.Engine: return "Wear reduces available engine power.";
                case TruckTaxiDamageCategory.Cooling: return "Radiator wear raises heat while running; severe heat wears the engine.";
                case TruckTaxiDamageCategory.Transmission: return "Drivetrain wear is applied through NWH transmission damage.";
                case TruckTaxiDamageCategory.Steering: return "Repairable wear only. Steering authority is always retained.";
                case TruckTaxiDamageCategory.Brakes: return "Wear reduces available brake torque.";
                case TruckTaxiDamageCategory.Suspension: return "Wear reduces spring and damper strength.";
                case TruckTaxiDamageCategory.Fuel: return "A critically damaged tank leaks fuel.";
                case TruckTaxiDamageCategory.Light: return "Severe damage disables this headlight.";
                case TruckTaxiDamageCategory.Tire:
                case TruckTaxiDamageCategory.Rim: return "Small grip/rolling-resistance penalty. Steering lock is retained.";
                default: return "Body condition contributes to repair cost; no steering penalty.";
            }
        }
    }
}
