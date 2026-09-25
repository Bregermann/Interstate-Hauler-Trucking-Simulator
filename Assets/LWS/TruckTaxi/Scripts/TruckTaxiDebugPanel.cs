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
        private RectTransform[] pages;
        private int page;
        private int passengerIndex;
        public bool IsOpen => panel!=null && panel.gameObject.activeSelf;
        public void Initialize(TruckTaxiBootstrap value,TruckTaxiHud ui)
        {
            host=value;
            panel=ui.Panel(ui.Root,"Truck Taxi Debug",new Vector2(.02f,.17f),new Vector2(.72f,.89f));
            diagnostics=ui.Text(panel,"Session diagnostics",new Vector2(.03f,.70f),new Vector2(.97f,.98f),23);
            pages=new[]{TruckTaxiHud.Rect(panel,"Ride tools",new Vector2(0,.07f),new Vector2(1,.70f)),
                TruckTaxiHud.Rect(panel,"Presentation tools",new Vector2(0,.07f),new Vector2(1,.70f)),
                TruckTaxiHud.Rect(panel,"Passenger tools",new Vector2(0,.07f),new Vector2(1,.70f))};
            string[] actions={"Start Shift","End Shift","Force Ride Offer","Auto Accept","Teleport Near Pickup","Force Passenger Boarding",
                "Teleport Near Destination","Complete Ride","Fail Ride","Generate Request","Complete Current Request","Fail Current Request",
                "Add Chaos Score","Spawn Traffic","Spawn Pedestrian","Reset Demo City"};
            for(int i=0;i<actions.Length;i++)
            {
                string action=actions[i]; int row=i/2,col=i%2;
                float top=.99f-row*.123f, left=.03f+col*.49f;
                ui.Button(pages[0],action.ToUpperInvariant(),new Vector2(left,top-.10f),new Vector2(left+.46f,top),()=>Execute(action));
            }
            string[] presentation={"Regenerate Ride Offer","Show Offer Map","Hide Offer Map","Log Route Distance","Debug Route Path","Handling On/Off","Reset Handling Defaults"};
            for(int i=0;i<presentation.Length;i++)
            {
                string action=presentation[i]; float top=.99f-(i/2)*.19f,left=.03f+(i%2)*.49f;
                ui.Button(pages[1],action.ToUpperInvariant(),new Vector2(left,top-.15f),new Vector2(left+.46f,top),()=>Execute(action));
            }
            pages[1].gameObject.SetActive(false);
            string[] passengerActions={"Next Passenger","Offer Selected Passenger","Eject Passenger","Test Chatter","Test Arrival","Test Animation",
                "Toggle Pickup VFX","Toggle Pickup Guides","Cycle Pickup State","Normal Pickup State","Test Mechanic","Validate Database"};
            for(int i=0;i<passengerActions.Length;i++)
            {
                string action=passengerActions[i]; float top=.99f-(i/2)*.16f,left=.03f+(i%2)*.49f;
                ui.Button(pages[2],action.ToUpperInvariant(),new Vector2(left,top-.13f),new Vector2(left+.46f,top),()=>Execute(action));
            }
            pages[2].gameObject.SetActive(false);
            ui.Button(panel,"NEXT TOOL PAGE",new Vector2(.25f,.01f),new Vector2(.75f,.065f),()=>{
                pages[page].gameObject.SetActive(false); page=(page+1)%pages.Length; pages[page].gameObject.SetActive(true);
            });
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
                case "Regenerate Ride Offer": s.DeclineRide(); s.OfferRide(); break;
                case "Show Offer Map": host.hud.SetOfferMapVisible(true); Toggle(); break;
                case "Hide Offer Map": host.hud.SetOfferMapVisible(false); break;
                case "Handling On/Off": host.Handling.Toggle(); break;
                case "Reset Handling Defaults": host.Handling.ResetDefaults(); break;
                case "Next Passenger": passengerIndex++; break;
                case "Offer Selected Passenger": s.DeclineRide(); s.OfferRide(SelectedPassenger()); break;
                case "Eject Passenger": host.Passengers.RequestEjection(); break;
                case "Test Chatter": host.Passengers.Dialogue.Speak(s.Passenger,TruckTaxiDialogueCategory.GeneralChatter,s,"Passenger voice preview.",90); break;
                case "Test Arrival": host.Passengers.Dialogue.Speak(s.Passenger,TruckTaxiDialogueCategory.Arrival,s,"We have arrived.",90); break;
                case "Test Animation": if(host.Passengers.PrimaryActor!=null) host.Passengers.PrimaryActor.Animate(TruckTaxiPassengerAnimation.Cheer); break;
                case "Test Mechanic": s.RecordEvent(TaxiEventType.Shortcut,"debug.mechanic"); break;
                case "Toggle Pickup VFX": host.PickupZone.show=!host.PickupZone.show; break;
                case "Toggle Pickup Guides":
                    bool guides=!host.PickupZone.showRadius;
                    host.PickupZone.showRadius=host.PickupZone.showCollider=host.PickupZone.showPassengerSpawn=host.PickupZone.showPreferredStop=guides; break;
                case "Cycle Pickup State": host.PickupZone.forcedState=(TruckTaxiPickupVisualState)(1+((int)(host.PickupZone.forcedState ?? TruckTaxiPickupVisualState.Hidden)%4)); break;
                case "Normal Pickup State": host.PickupZone.forcedState=null; break;
                case "Validate Database": Debug.Log("Taxi passenger database: "+(host.configuration.passengerDatabase?.passengers.Length ?? 0)+" registered profiles."); break;
                case "Log Route Distance":
                    if(s.Offer!=null) Debug.Log($"TAXI OFFER: pickup {s.Offer.ToPickup.Meters:0.0}m ({s.Offer.ToPickup.Source}); trip {s.Offer.Trip.Meters:0.0}m ({s.Offer.Trip.Source})");
                    break;
                case "Debug Route Path":
                    if(s.Offer!=null) { Draw(s.Offer.ToPickup,Color.cyan); Draw(s.Offer.Trip,Color.yellow); }
                    break;
            }
        }
        private void Update()
        {
            if(!IsOpen || host?.Session==null) return;
            var s=host.Session;
            diagnostics.text=$"DEVELOPMENT / TAXI SESSION\n{s.State} | {s.Passenger?.passengerName ?? "NO PASSENGER"}\n{s.Pickup?.locationName} -> {s.Destination?.locationName}\nREQUESTS {s.Requests.Count} | FARE {TruckTaxiHud.Money(s.EstimateFare().Total)} | {s.ElapsedRide:0}s\nCHAOS {s.ChaosScore} | SATISFACTION {s.Satisfaction:0.0} | COLLISIONS {s.TrackedCollisions} | TRAFFIC {host.traffic.ActiveCount}";
            if(page==1) diagnostics.text=$"PRESENTATION / HANDLING\nPICKUP {s.Offer?.ToPickup.Meters:0.0}m [{s.Offer?.ToPickup.Source}]  TRIP {s.Offer?.Trip.Meters:0.0}m [{s.Offer?.Trip.Source}]\nTAXI OVERRIDE {host.Handling.Applied} | {host.Handling.SpeedMph:0.0} MPH\nSTEER INPUT {host.Handling.SteeringInput:0.00} | ANGLE {host.Handling.SteeringAngle:0.0} | YAW ASSIST {host.Handling.YawAssist:0.0}";
            if(page==2) diagnostics.text=$"PASSENGER / PICKUP\nSELECTED {SelectedPassenger()?.passengerName}\nACTIVE {s.Passenger?.passengerName} | PAIR {s.Passenger?.pairPassenger?.passengerName}\nRADIUS {host.PickupZone.Radius:0.0}m | DISTANCE {host.PickupZone.Distance:0.0}m | {host.PickupZone.State}\nVOICE {host.Passengers.Dialogue.IsPlaying} | EJECTED {host.Passengers.EjectedBodies}";
        }
        private PassengerProfile SelectedPassenger()
        {
            var all=host.configuration.passengerDatabase!=null ? host.configuration.passengerDatabase.passengers : host.configuration.passengers;
            return all!=null && all.Length>0 ? all[passengerIndex%all.Length] : null;
        }
        private static void Draw(TruckTaxiRouteLeg leg,Color color)
        { for(int i=1;i<leg.Points.Count;i++) Debug.DrawLine(leg.Points[i-1]+Vector3.up*2,leg.Points[i]+Vector3.up*2,color,15); }
    }
}
