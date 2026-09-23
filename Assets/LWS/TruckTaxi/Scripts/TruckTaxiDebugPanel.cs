using System;
using TMPro;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public sealed class TruckTaxiDebugPanel : MonoBehaviour
    {
        private TruckTaxiBootstrap host;
        private RectTransform panel;
        private TextMeshProUGUI diagnostics;
        public bool IsOpen => panel!=null && panel.gameObject.activeSelf;
        public void Initialize(TruckTaxiBootstrap value,TruckTaxiHud ui)
        {
            host=value;
            panel=ui.Panel(ui.Root,"Truck Taxi Debug",new Vector2(.02f,.17f),new Vector2(.72f,.89f));
            diagnostics=ui.Text(panel,"Session diagnostics",new Vector2(.03f,.70f),new Vector2(.97f,.98f),23);
            string[] actions={"Start Shift","End Shift","Force Ride Offer","Auto Accept","Teleport Near Pickup","Force Passenger Boarding",
                "Teleport Near Destination","Complete Ride","Fail Ride","Generate Request","Complete Current Request","Fail Current Request",
                "Add Chaos Score","Spawn Traffic","Spawn Pedestrian","Reset Demo City"};
            for(int i=0;i<actions.Length;i++)
            {
                string action=actions[i]; int row=i/2,col=i%2;
                float top=.67f-row*.078f, left=.03f+col*.49f;
                ui.Button(panel,action.ToUpperInvariant(),new Vector2(left,top-.065f),new Vector2(left+.46f,top),()=>Execute(action));
            }
            panel.gameObject.SetActive(false);
        }
        public void Toggle() { if(panel!=null) panel.gameObject.SetActive(!panel.gameObject.activeSelf); }
        public void Execute(string action)
        {
            if(host==null || !host.Ready) return;
            var s=host.Session;
            switch(action)
            {
                case "Start Shift": host.StartShift(); break;
                case "End Shift": s.EndShift(); break;
                case "Force Ride Offer": s.OfferRide(); break;
                case "Auto Accept": s.AcceptRide(); break;
                case "Teleport Near Pickup": host.TeleportNear(s.Pickup); break;
                case "Force Passenger Boarding": host.TeleportNear(s.Pickup); s.DebugBoard(); host.SetPaused(false); break;
                case "Teleport Near Destination": host.TeleportNear(s.Destination); break;
                case "Complete Ride": s.DebugComplete(); break;
                case "Fail Ride": s.FailRide("Debug ride failure."); break;
                case "Generate Request": s.GenerateRequest(); break;
                case "Complete Current Request": s.DebugResolveRequest(true); break;
                case "Fail Current Request": s.DebugResolveRequest(false); break;
                case "Add Chaos Score": s.DebugChaos(); break;
                case "Spawn Traffic": host.traffic.SpawnTraffic(); break;
                case "Spawn Pedestrian": host.SpawnPedestrian(); break;
                case "Reset Demo City": host.ResetCity(); break;
            }
        }
        private void Update()
        {
            if(!IsOpen || host?.Session==null) return;
            var s=host.Session;
            diagnostics.text=$"DEVELOPMENT / TAXI SESSION\n{s.State} | {s.Passenger?.passengerName ?? "NO PASSENGER"}\n{s.Pickup?.locationName} -> {s.Destination?.locationName}\nREQUESTS {s.Requests.Count} | FARE {TruckTaxiHud.Money(s.EstimateFare().Total)} | {s.ElapsedRide:0}s\nCHAOS {s.ChaosScore} | SATISFACTION {s.Satisfaction:0.0} | COLLISIONS {s.TrackedCollisions} | TRAFFIC {host.traffic.ActiveCount}";
        }
    }
}
