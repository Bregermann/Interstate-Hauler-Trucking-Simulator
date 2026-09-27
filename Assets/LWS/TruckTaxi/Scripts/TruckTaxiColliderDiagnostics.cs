using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // Development Scene-view diagnostics; no gameplay collider is mutated.
    public sealed class TruckTaxiColliderDiagnostics : MonoBehaviour
    {
        private TruckTaxiBootstrap host;
        private bool passengers,buses,traffic;
        private readonly List<Collider> colliders=new List<Collider>();
        public void Initialize(TruckTaxiBootstrap value) => host=value;
        public void TogglePassengers() {passengers=!passengers; Rebuild();}
        public void ToggleBuses() {buses=!buses; Rebuild();}
        public void ToggleTraffic() {traffic=!traffic; Rebuild();}
        private void Rebuild()
        {
            colliders.Clear();
            if(host==null) return;
            if(passengers)
            {
                if(host.Passengers.PrimaryActor!=null) colliders.AddRange(host.Passengers.PrimaryActor.GetComponentsInChildren<Collider>());
                if(host.Passengers.SecondaryActor!=null) colliders.AddRange(host.Passengers.SecondaryActor.GetComponentsInChildren<Collider>());
            }
            if(traffic) foreach(var car in host.traffic.Vehicles) if(car!=null) colliders.AddRange(car.GetComponentsInChildren<Collider>());
            if(buses) foreach(var service in FindObjectsByType<TruckTaxiBusService>(FindObjectsSortMode.None))
                for(int i=0;i<service.RouteCount;i++) {var bus=service.GetLiveBus(i); if(bus!=null) colliders.AddRange(bus.GetComponentsInChildren<Collider>());}
            Debug.Log($"Taxi collider gizmos: {colliders.Count} cached colliders. Scene view Gizmos must be enabled.");
        }
        private void OnDrawGizmos()
        {
            foreach(var collider in colliders) if(collider!=null && collider.enabled)
            {Gizmos.color=collider.isTrigger ? Color.yellow : Color.cyan; Gizmos.DrawWireCube(collider.bounds.center,collider.bounds.size);}
        }
        public static string ObjectiveTimers(TruckTaxiSession session)
        {
            var text=new StringBuilder("TAXI OBJECTIVE TIMERS\n");
            foreach(var request in session.Requests) text.AppendLine($"{request.Description}: {request.State} / "+
                (TruckTaxiRequestPolicy.IsTimed(request.Definition) ? $"{request.Remaining:0.0}s" : "UNTIMED"));
            return text.ToString();
        }
    }
}
