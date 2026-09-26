#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace LWS.TruckTaxi
{
    // Opt-in, teleport-assisted integration fixture, shared by PlayMode and the actual Windows player.
    public static class TruckTaxiObjectiveRuntimeProbe
    {
        public static IEnumerator Run(TruckTaxiBootstrap host,Action<bool,string> check,Action<string> capture)
        {
            var original=InputSystem.settings;
            var inputSettings=UnityEngine.Object.Instantiate(original); inputSettings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            inputSettings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings=inputSettings;
            var keyboard=InputSystem.AddDevice<Keyboard>("Taxi objective test keyboard");
            var gamepad=InputSystem.AddDevice<Gamepad>("Taxi objective test gamepad");
            var profile=UnityEngine.Object.Instantiate(host.configuration.passengerDatabase.passengers.First(p=>p.passengerName=="Bree Brightside"));
            var settings=JsonUtility.ToJson(host.GPS.DisplaySettings);
            var body=host.Player.GetComponent<Rigidbody>(); bool wasKinematic=body.isKinematic;
            profile.possibleRequests=Array.Empty<PassengerRequestDefinition>(); profile.uniqueMechanics=Array.Empty<TruckTaxiMechanic>();
            profile.requestDifficultyRange=Vector2.one; profile.requestFrequency=10000; profile.basePatience=10000;
            profile.specialAppreciationEligible=false; profile.baseSatisfaction=5; profile.smoothAffinity=0;
            try
            {
                ValidateEvaluators(host,profile,check);
                host.Session.EndShift(); yield return Wait(.4f);
                yield return KeyPress(keyboard,Key.Enter); check(host.Session.State==TruckTaxiState.Available,"Keyboard starts shift without pointer (state="+host.Session.State+")");
                host.Session.OfferRide(profile); yield return Wait(.3f); capture?.Invoke("Objective_Offer");
                yield return KeyPress(keyboard,Key.Escape); check(host.Session.State==TruckTaxiState.Available,"Escape declines offer without opening pause");
                host.Session.OfferRide(profile); yield return Wait(.3f);
                yield return PadPress(gamepad,GamepadButton.South); check(host.Session.State==TruckTaxiState.DrivingToPickup,"Gamepad South accepts offer once");
                yield return PadPress(gamepad,GamepadButton.Start); check(host.Paused,"Gamepad Start pauses");
                yield return PadPress(gamepad,GamepadButton.DpadDown);
                check(EventSystem.current.currentSelectedGameObject?.name=="GPS SETTINGS","Pause navigation focuses GPS settings");
                yield return PadPress(gamepad,GamepadButton.South); check(host.hud.GPSSettings.IsOpen,"Gamepad opens GPS settings");
                bool shown=host.GPS.HudVisible;
                yield return PadPress(gamepad,GamepadButton.South); check(host.GPS.HudVisible!=shown,"Selected Heat switch changes HUD visibility");
                yield return PadPress(gamepad,GamepadButton.South);
                for(int i=0;i<3;i++) yield return PadPress(gamepad,GamepadButton.DpadDown);
                var slider=EventSystem.current.currentSelectedGameObject?.GetComponent<Slider>();
                check(slider!=null,"Keyboard/gamepad navigation reaches GPS slider");
                for(int i=0;i<4;i++)
                {
                    slider=EventSystem.current.currentSelectedGameObject?.GetComponent<Slider>();
                    check(slider!=null,"GPS slider focus "+i);
                    if(slider!=null)
                    { float before=slider.value; yield return PadPress(gamepad,GamepadButton.DpadRight); check(slider.value>before,"Gamepad adjusts "+slider.name); }
                    yield return PadPress(gamepad,GamepadButton.DpadDown);
                }
                for(int i=0;i<4;i++)
                {
                    yield return PadPress(gamepad,GamepadButton.South);
                    check(host.GPS.DisplaySettings.routeColorIndex==i,"Gamepad selects GPS route color "+i);
                    yield return PadPress(gamepad,GamepadButton.DpadDown);
                }
                check(EventSystem.current.currentSelectedGameObject?.name=="DEFAULTS","GPS defaults reachable");
                yield return PadPress(gamepad,GamepadButton.South);
                yield return PadPress(gamepad,GamepadButton.DpadDown);
                check(EventSystem.current.currentSelectedGameObject?.name=="CLOSE","GPS close reachable");
                capture?.Invoke("Objective_GPS_Settings");
                yield return PadPress(gamepad,GamepadButton.East); check(!host.hud.GPSSettings.IsOpen && host.Paused,"Gamepad Back restores pause menu");
                yield return KeyPress(keyboard,Key.Escape); check(!host.Paused,"Keyboard resumes driving");
                host.TeleportNear(host.Session.Pickup); body.isKinematic=true;
                yield return Until(()=>host.Session.HasPassenger,12); check(host.Session.HasPassenger,"Passenger boards through existing runtime");
                check(host.Session.Capabilities.Count(TruckTaxiObjectiveCapability.ScenicStops)==6,"Six routable scenic stops registered");
                check(host.Session.Capabilities.Count(TruckTaxiObjectiveCapability.IllicitStops)==6,"Six routable sketchy stops registered");
                foreach(var point in host.Session.Capabilities.Stops)
                {
                    check(host.roadGraph.Graph.nodes.Exists(n=>n.nodeId==point.stableId),"Stop has authored route endpoint: "+point.stableId);
                    check(TruckTaxiSurface.TrySample(point.Position+Vector3.up,host.Player.transform,out bool paved) && paved,"Stop has actual paved ground: "+point.stableId);
                    bool clear=true;
                    foreach(var collider in Physics.OverlapBox(point.Position+Vector3.up*2,new Vector3(2,1.4f,5),Quaternion.identity,~0,QueryTriggerInteraction.Ignore))
                        if(collider.attachedRigidbody==null) clear=false;
                    check(clear,"Stop static tractor clearance: "+point.stableId);
                }
                foreach(var type in new[]{TaxiRequestType.ScenicRoute,TaxiRequestType.IllicitStop})
                {
                    // Stops are sequential ride objectives, not simultaneously competing GPS destinations.
                    if(type==TaxiRequestType.IllicitStop)
                    {
                        host.Session.DebugComplete(); yield return Wait(.3f); yield return KeyPress(keyboard,Key.Enter);
                        host.Session.OfferRide(profile); yield return Wait(.3f); yield return KeyPress(keyboard,Key.Enter);
                        host.TeleportNear(host.Session.Pickup); yield return Until(()=>host.Session.HasPassenger,12);
                    }
                    var definition=host.configuration.requests.First(d=>d.requestType==type);
                    check(host.Session.GenerateRequest(definition),type+" assigns only with actual support");
                    var request=host.Session.ActiveStop;
                    if(request==null) yield break;
                    yield return Wait(.3f); check(host.OptionalStops.MarkerVisible && host.GPS.TargetId==request.StopPoint.stableId,"Optional stop marker and GPS detour active");
                    Place(host,request.StopPoint.Position+Vector3.right*(request.StopPoint.radius+3)); yield return Wait(.6f);
                    check(request.Progress==0,"Stop timer does not run outside zone");
                    int lines=host.Passengers.Dialogue.SpokenLines;
                    Place(host,request.StopPoint.Position); yield return Wait(2);
                    check(request.Progress>1 && request.Progress<request.Target,"Stationary stop advances authoritative timer");
                    check(host.Passengers.Dialogue.SpokenLines>lines,"Passenger stop dialogue plays");
                    Place(host,request.StopPoint.Position+Vector3.right*20); yield return Wait(.3f); check(request.Progress==0,"Leaving stop resets continuous progress");
                    Place(host,request.StopPoint.Position); yield return Wait(.5f); capture?.Invoke("Objective_"+type);
                    yield return Until(()=>request.State!=TaxiRequestState.Active,request.Target+3);
                    check(request.State==TaxiRequestState.Succeeded,"Timed "+type+" completes");
                    check(host.GPS.TargetId==host.Session.Destination.locationId && !host.OptionalStops.MarkerVisible,"Final destination restored after stop");
                }
                host.Session.DebugComplete(); yield return Wait(.3f); yield return KeyPress(keyboard,Key.Enter);
                host.Session.OfferRide(profile); yield return Wait(.3f); yield return KeyPress(keyboard,Key.Enter);
                host.TeleportNear(host.Session.Pickup); yield return Until(()=>host.Session.HasPassenger,12);
                var chaos=host.configuration.requests.First(d=>d.requestType==TaxiRequestType.MaximumChaos);
                check(host.Session.GenerateRequest(chaos),"Chaos objective generated");
                var goal=host.Session.Requests.Last(); int initial=host.Session.ChaosScore;
                host.Session.RecordEvent(TaxiEventType.Shortcut,"probe.score",scoreOverride:32);
                check(goal.Target==400 && goal.Description.Contains("400") && goal.ProgressText.EndsWith("/400"),"Chaos text and denominator share 400 target");
                yield return Wait(.3f); capture?.Invoke("Objective_Chaos400");
                host.Session.RecordEvent(TaxiEventType.Shortcut,"probe.finish",scoreOverride:400-initial-32);
                check(goal.State==TaxiRequestState.Succeeded,"Chaos completes at exactly displayed target");
                // Finish the current and two subsequent rides with exact distinct satisfaction fixtures.
                host.Session.DebugComplete(); yield return Wait(.3f);
                check(host.Session.LastFare.Rating==5,"Individual completed ride awards integer five stars");
                yield return KeyPress(keyboard,Key.Enter);
                foreach(int rating in new[]{3,4})
                {
                    profile.baseSatisfaction=rating; host.Session.OfferRide(profile); yield return Wait(.3f); yield return KeyPress(keyboard,Key.Enter);
                    host.TeleportNear(host.Session.Pickup); yield return Until(()=>host.Session.HasPassenger,12);
                    host.TeleportNear(host.Session.Destination); yield return Until(()=>host.Session.State==TruckTaxiState.RideComplete,4);
                    check(host.Session.State==TruckTaxiState.RideComplete && host.Session.LastFare?.Rating==rating,"Ride result integer "+rating+" state="+host.Session.State);
                    yield return Wait(.2f);
                    capture?.Invoke("Objective_Rating"+rating); yield return KeyPress(keyboard,Key.Enter);
                }
                check(Math.Abs(host.Session.DriverAverageRating-(double)host.Session.TotalStarsEarned/host.Session.CompletedRides)<.0001,"Driver average uses cumulative actual star sum");
                foreach(bool accept in new[]{false,true})
                {
                    profile.specialAppreciationEligible=true; profile.appreciationChance=1; profile.baseSatisfaction=5;
                    check(profile.CanOfferAppreciation,"Authored adult eligible passenger");
                    host.Session.OfferRide(profile); yield return Wait(.3f); yield return PadPress(gamepad,GamepadButton.South);
                    host.TeleportNear(host.Session.Pickup); yield return Until(()=>host.Session.HasPassenger,12);
                    host.TeleportNear(host.Session.Destination); yield return Until(()=>host.Session.State==TruckTaxiState.AppreciationOffer,5);
                    check(host.Session.State==TruckTaxiState.AppreciationOffer,"Special appreciation waits for explicit player choice");
                    capture?.Invoke("Objective_AppreciationOffer"); int rides=host.Session.CompletedRides;
                    yield return PadPress(gamepad,accept ? GamepadButton.South : GamepadButton.East);
                    if(accept) { yield return Wait(1); capture?.Invoke("Objective_AppreciationFade"); yield return Wait(4); }
                    check(host.Session.State==TruckTaxiState.RideComplete && host.Session.CompletedRides==rides+1,"Appreciation "+(accept ? "accept fade" : "decline")+" finishes once");
                    check(host.Session.LastFare.Rating==5,"Decline/accept does not penalize rating");
                    yield return KeyPress(keyboard,Key.Enter);
                }
                profile.specialAppreciationEligible=false;
                host.Session.OfferRide(profile); yield return Wait(.3f); yield return KeyPress(keyboard,Key.Enter);
                host.TeleportNear(host.Session.Pickup); yield return Until(()=>host.Session.HasPassenger,12);
                InputSystem.QueueStateEvent(gamepad,new GamepadState().WithButton(GamepadButton.Select)); yield return Wait(1.5f);
                InputSystem.QueueStateEvent(gamepad,new GamepadState()); yield return Wait(.2f);
                check(host.Session.State==TruckTaxiState.PassengerEjected,"Physical gamepad hold still ejects passenger");
                yield return Wait(3); host.Session.OfferRide(profile); yield return Wait(.3f); yield return PadPress(gamepad,GamepadButton.East);
                check(host.Session.State==TruckTaxiState.Available,"Next offer can be declined without mouse");
                check(host.GPS.CabCompass!=null && host.GPS.HudCompass!=null,"Existing physical and HUD Compass instances preserved");
                check(host.pedestrians.People.Any(p=>p!=null && p.HasJointedRagdoll),"Pedestrian ragdoll integration retained");
            }
            finally
            {
                body.isKinematic=wasKinematic; host.Session.EndShift();
                JsonUtility.FromJsonOverwrite(settings,host.GPS.DisplaySettings); host.GPS.ApplyDisplaySettings();
                InputSystem.RemoveDevice(keyboard); InputSystem.RemoveDevice(gamepad); InputSystem.settings=original;
                UnityEngine.Object.Destroy(inputSettings); UnityEngine.Object.Destroy(profile);
            }
        }
        private static void Place(TruckTaxiBootstrap host,Vector3 position)
        { var body=host.Player.GetComponent<Rigidbody>(); body.position=position+Vector3.up*1.6f; host.Player.transform.position=body.position; Physics.SyncTransforms(); host.Session.DiscardTeleportDistance(); }
        private static IEnumerator KeyPress(Keyboard keyboard,Key key)
        { InputSystem.QueueStateEvent(keyboard,new KeyboardState(key)); yield return Wait(.12f); InputSystem.QueueStateEvent(keyboard,new KeyboardState()); yield return Wait(.25f); }
        private static IEnumerator PadPress(Gamepad pad,GamepadButton button)
        { InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(button)); yield return Wait(.12f); InputSystem.QueueStateEvent(pad,new GamepadState()); yield return Wait(.25f); }
        private static WaitForSecondsRealtime Wait(float seconds) => new WaitForSecondsRealtime(seconds);
        private static IEnumerator Until(Func<bool> predicate,float seconds)
        { float deadline=Time.realtimeSinceStartup+seconds; while(!predicate() && Time.realtimeSinceStartup<deadline) yield return null; }
        private static void ValidateEvaluators(TruckTaxiBootstrap host,PassengerProfile source,Action<bool,string> check)
        {
            var locations=UnityEngine.Object.FindObjectsByType<TruckTaxiRideLocation>(FindObjectsSortMode.None);
            var bundlePassenger=UnityEngine.Object.Instantiate(source); bundlePassenger.possibleRequests=host.configuration.requests;
            bool compatible=true;
            for(int seed=0;seed<1000;seed++)
            {
                var session=new TruckTaxiSession(host.configuration,locations,seed,host.RouteDistances,()=>host.Player.transform.position);
                foreach(TruckTaxiObjectiveCapability flag in Enum.GetValues(typeof(TruckTaxiObjectiveCapability)))
                    session.Capabilities.Register(flag,host.Session.Capabilities.Count(flag));
                session.Capabilities.Stops.AddRange(host.Session.Capabilities.Stops);
                session.StartShift(); session.OfferRide(bundlePassenger); session.AcceptRide();
                session.Tick(.1f,session.Pickup.StopPosition,0,0,true); session.Tick(2,session.Pickup.StopPosition,0,0,true);
                for(int attempt=0;attempt<8;attempt++) session.GenerateRequest();
                compatible&=session.Capabilities.ValidateCombination(session.Requests.Select(r=>r.Definition).ToArray(),out _);
            }
            UnityEngine.Object.Destroy(bundlePassenger);
            check(compatible,"1000 runtime generated bundles valid with actual scene capabilities");
            foreach(var definition in host.configuration.requests)
            {
                if(!host.Session.Capabilities.Supports(definition)) continue;
                var passenger=UnityEngine.Object.Instantiate(source); passenger.possibleRequests=new[]{definition};
                var session=new TruckTaxiSession(host.configuration,locations,42,host.RouteDistances,()=>host.Player.transform.position);
                foreach(TruckTaxiObjectiveCapability flag in Enum.GetValues(typeof(TruckTaxiObjectiveCapability)))
                    session.Capabilities.Register(flag,host.Session.Capabilities.Count(flag));
                session.Capabilities.Stops.AddRange(host.Session.Capabilities.Stops);
                session.StartShift(); session.OfferRide(passenger); session.AcceptRide();
                session.Tick(.1f,session.Pickup.StopPosition,0,0,true); session.Tick(2,session.Pickup.StopPosition,0,0,true);
                var request=session.Requests.FirstOrDefault();
                check(request!=null,"Runtime capability-gated evaluator assigned: "+definition.requestType);
                if(request==null) { UnityEngine.Object.Destroy(passenger); continue; }
                if(definition.IsStop) session.Tick(request.Target,request.StopPoint.Position,0,0,true);
                else if(definition.requestType==TaxiRequestType.Offroad) session.Tick(request.Target,session.Pickup.StopPosition,3,0,false);
                else if(definition.requestType==TaxiRequestType.MaximumChaos) session.RecordEvent(TaxiEventType.Shortcut,"test.chaos",scoreOverride:(int)request.Target);
                else
                {
                    TaxiEventType? type=definition.requestType==TaxiRequestType.Shortcut ? TaxiEventType.Shortcut :
                        definition.requestType==TaxiRequestType.RamTraffic ? TaxiEventType.TrafficRam :
                        definition.requestType==TaxiRequestType.HitPedestrian ? TaxiEventType.PedestrianHit :
                        definition.requestType==TaxiRequestType.PropertyDamage ? TaxiEventType.PropDamage :
                        definition.requestType==TaxiRequestType.NearMiss ? TaxiEventType.NearMiss : (TaxiEventType?)null;
                    if(type.HasValue) for(int i=0;i<request.Target;i++) session.RecordEvent(type.Value,"test.target."+i,8);
                    else { session.Tick(.1f,session.Destination.StopPosition,0,0,true); session.Tick(2,session.Destination.StopPosition,0,0,true); }
                }
                check(request.State==TaxiRequestState.Succeeded,"Runtime evaluator completed (simulated event/telemetry): "+definition.requestType);
                UnityEngine.Object.Destroy(passenger);
            }
            check(TruckTaxiSurface.TrySample(new Vector3(320,2,80),host.Player.transform,out bool road) && road,"Actual paved road classified as road");
            check(TruckTaxiSurface.TrySample(new Vector3(370,2,80),host.Player.transform,out bool grass) && !grass,"Actual grass classified as offroad");
        }
    }
}
#endif
