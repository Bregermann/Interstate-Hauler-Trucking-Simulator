using System;
using System.Collections.Generic;

namespace LWS.TruckTaxi
{
    public enum TruckTaxiCompanionPickupRequestState { None, NotSelected, Offered, Accepted, Declined, Succeeded, Cancelled }

    public sealed class TruckTaxiCompanionPickupRequestPolicy
    {
        private readonly HashSet<string> evaluatedFares = new HashSet<string>(StringComparer.Ordinal);
        public TruckTaxiCompanionPickupRequestState State { get; private set; }
        public string RideId { get; private set; }
        public string TargetId { get; private set; }
        public bool Pending => State == TruckTaxiCompanionPickupRequestState.Offered || State == TruckTaxiCompanionPickupRequestState.Accepted;
        public bool CanEvaluate(string rideId) => !string.IsNullOrWhiteSpace(rideId) && !Pending && !evaluatedFares.Contains(rideId);

        public bool TryOffer(string rideId, string targetId, bool explicitlyEligible, float chance,
            double roll, bool safeSeatAndMaterializedTarget)
        {
            if (!CanEvaluate(rideId) || !explicitlyEligible || !safeSeatAndMaterializedTarget ||
                string.IsNullOrWhiteSpace(targetId) || !float.IsFinite(chance) ||
                double.IsNaN(roll) || double.IsInfinity(roll) || roll < 0 || roll >= 1) return false;
            evaluatedFares.Add(rideId);
            RideId = rideId; TargetId = targetId;
            State = roll < Math.Max(0, Math.Min(1, chance)) ?
                TruckTaxiCompanionPickupRequestState.Offered : TruckTaxiCompanionPickupRequestState.NotSelected;
            return State == TruckTaxiCompanionPickupRequestState.Offered;
        }

        public bool Accept()
        {
            if (State != TruckTaxiCompanionPickupRequestState.Offered) return false;
            State = TruckTaxiCompanionPickupRequestState.Accepted; return true;
        }
        public bool Decline()
        {
            if (State != TruckTaxiCompanionPickupRequestState.Offered) return false;
            State = TruckTaxiCompanionPickupRequestState.Declined; return true;
        }
        public bool Complete(string rideId, string targetId)
        {
            if (State != TruckTaxiCompanionPickupRequestState.Accepted || rideId != RideId || targetId != TargetId) return false;
            State = TruckTaxiCompanionPickupRequestState.Succeeded; return true;
        }
        public bool Cancel()
        {
            if (!Pending) return false;
            State = TruckTaxiCompanionPickupRequestState.Cancelled; return true;
        }
    }
}
