using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // Optional-stop adapter side effect; normal item/fare rewards remain with Bootstrap/Session.
    public sealed class TruckTaxiScenicCompletion
    {
        private readonly HashSet<TaxiRequestProgress> completed = new HashSet<TaxiRequestProgress>();
        private readonly TruckTaxiPrivateEventVehicleMotion motion;
        private readonly TruckTaxiSession session;
        private Action<float> relieveThirst;

        public TruckTaxiScenicCompletion(TruckTaxiSession owner, TruckTaxiPrivateEventVehicleMotion vehicleMotion)
        { session = owner; motion = vehicleMotion; }

        public void SetThirstRelief(Action<float> relief) => relieveThirst = relief;

        public bool TryComplete(TaxiRequestProgress request)
        {
            if (request == null || request.State != TaxiRequestState.Succeeded ||
                request.Definition?.requestType != TaxiRequestType.ScenicRoute ||
                request.StopPoint == null || !completed.Add(request)) return false;
            float relief = Mathf.Clamp01(request.Definition.scenicThirstRelief);
            relieveThirst?.Invoke(relief);
            motion?.BeginScenic();
            if (relieveThirst != null)
                session?.React("SCENIC STOP COMPLETE  THIRST -" + Mathf.RoundToInt(relief * 100) + "%");
            return true;
        }
    }
}
