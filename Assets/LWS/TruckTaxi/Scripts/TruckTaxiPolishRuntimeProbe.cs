#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace LWS.TruckTaxi
{
    // Opt-in integration fixtures. Teleport/kinematic setup is reported, never presented as a real driving test.
    public static class TruckTaxiPolishRuntimeProbe
    {
        public static IEnumerator Run(TruckTaxiBootstrap host,Action<bool,string> check,Action<string> capture)
        {
            if(host?.Ready!=true || host.Companions==null) { check(false,"Polish dependencies ready"); yield break; }
            bool preference=host.Session.RideRequestsEnabled;
            bool hadPreference=PlayerPrefs.HasKey(TruckTaxiSession.RideRequestsPreferenceKey);
            var input=InputSystem.settings;
            var temporary=UnityEngine.Object.Instantiate(input);
            var keyboard=InputSystem.AddDevice<Keyboard>("Taxi polish integration keyboard");
            var body=host.Player.GetComponent<Rigidbody>();
            bool kinematic=body.isKinematic;
            Vector3 start=body.position; Quaternion rotation=body.rotation;
            try
            {
                InputSystem.settings=temporary;
                temporary.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
                temporary.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
                InputSystem.EnableDevice(keyboard);
                host.StartShift(); host.Session.SetRideRequestsEnabled(true);
                host.Configuration.logOfferTimings=true;
                var world=host.GetComponent<TruckTaxiRegionalWorld>();
                yield return new WaitForSecondsRealtime(2);
                for(int offer=0;offer<3;offer++)
                {
                    int loaded=world.LoadedCount;
                    host.Session.RequestRideOffer();
                    float deadline=Time.realtimeSinceStartup+8; int frames=0;
                    while(host.Session.IsGeneratingOffer && Time.realtimeSinceStartup<deadline) { frames++; yield return null; }
                    check(host.Session.State==TruckTaxiState.RideOffered && host.Session.LastOfferRouteQueries<=30,
                        $"Bounded offer {offer+1}: frames={frames}; {host.Session.LastOfferDiagnostics}");
                    check(world.LoadedCount==loaded,"Offer does not load scenery");
                    check(host.Session.LastOfferMaximumStepMs<1000,"Offer has no one-second blocking stage");
                    if(offer==0) capture?.Invoke("Polish_Offer");
                    host.Session.DeclineRide(); yield return null;
                }
                host.Session.SetRideRequestsEnabled(false);
                var map=host.hud.FullMap;
                check(map.Open(),"Existing full map opens"); yield return new WaitForSecondsRealtime(.5f);
                float zoom=host.GPS.FullMapZoomLevel;
                host.GPS.ZoomFullMap(1); float closer=host.GPS.FullMapZoomLevel;
                host.GPS.ZoomFullMap(-1);
                check(closer>zoom && host.GPS.FullMapZoomLevel<closer,"Map +/- use Compass zoom convention");
                var tiles=host.GetComponentInChildren<TruckTaxiMapTileLayer>(true);
                check(tiles!=null && tiles.GetComponentsInChildren<MeshRenderer>().Length>=world.regions.Length,
                    "Persistent baked geography exists independently of loaded chunks");
                capture?.Invoke("Polish_FullMap"); map.Close(); yield return null;
                float sampleEnd=Time.realtimeSinceStartup+15,frameMs=0; double mainMs=0,physicsMs=0; int samples=0;
                while(Time.realtimeSinceStartup<sampleEnd)
                {
                    yield return null; samples++; frameMs+=Time.unscaledDeltaTime*1000;
                    mainMs+=world.MainThreadMs; physicsMs+=world.PhysicsMs;
                }
                Debug.Log($"TAXI POLISH PERF: fps={1000/(frameMs/Mathf.Max(1,samples)):F1} mainMs={mainMs/Math.Max(1,samples):F2} physicsMs={physicsMs/Math.Max(1,samples):F2} cars={host.traffic.ActiveCount}/{host.traffic.LogicalCount} peds={host.pedestrians.ActiveCount}/{host.pedestrians.LogicalCount}");
                check(samples>0 && frameMs/samples<50,"Stationary-town sample remains above 20 FPS (not whole-route benchmark)");

                // Isolate the new companion state/input checks from route driving, already covered separately.
                body.linearVelocity=body.angularVelocity=Vector3.zero; body.isKinematic=true;
                for(int attempt=0;attempt<2;attempt++)
                {
                    host.Session.SetRideRequestsEnabled(false);
                    body.position=start; body.rotation=rotation; Physics.SyncTransforms();
                    host.DriverNeeds.State.DebugSetThirst(1);
                    bool spawned=host.Companions.DebugSpawnNearPlayer();
                    yield return new WaitForSecondsRealtime(.3f);
                    bool eligible=host.Companions.CanInteract;
                    Press(keyboard,Key.H); yield return new WaitForSecondsRealtime(.3f);
                    bool horn=host.Player.TruckControlController.CurrentState.hornActive;
                    LWS.InterstateHauler.LwsApplicationBootstrap.Instance.Registry.TryGet(out LWS.InterstateHauler.ILwsGameplayStateService macro);
                    var source=host.Player.GetComponent<LWS.InterstateHauler.LwsKeyboardGamepadTruckInputSource>();
                    Debug.Log($"TAXI POLISH HORN: key={keyboard.hKey.isPressed} current={Keyboard.current==keyboard} enabled={keyboard.enabled} macro={macro?.CurrentState} suppressed={source?.DrivingInputSuppressed} controls={host.Player.TruckControlController.enabled}");
                    Release(keyboard);
                    float boardingDeadline=Time.realtimeSinceStartup+3;
                    while(!host.Companions.HasOnboardCompanion && Time.realtimeSinceStartup<boardingDeadline) yield return null;
                    check(spawned && host.Companions.HasOnboardCompanion,$"Companion boards through normal H horn input (positioning fixture): spawned={spawned} eligible={eligible} horn={horn} busy={host.Companions.IsBusy} paused={host.Paused} state={host.Session.State} gps={host.GPS.TargetId} feedback={host.Companions.Feedback}");
                    if(!host.Companions.HasOnboardCompanion) break;
                    host.Session.SetRideRequestsEnabled(attempt==1);
                    var stop=host.Companions.ActivePrivateStop;
                    float until=Time.realtimeSinceStartup+3;
                    while(!host.GPS.RouteReady && Time.realtimeSinceStartup<until) yield return null;
                    var circle=FindLine(host,"Private stop parking radius");
                    check(stop!=null && host.GPS.TargetId==stop.stableId && host.GPS.RouteReady && circle!=null,
                        "Private-stop GPS and world circle have one target");
                    if(stop==null) break;
                    host.GPS.StopNavigation();
                    check(host.GPS.GuidanceSuppressed && host.Companions.HasOnboardCompanion && host.Session.OffersSuppressed,
                        "Stop Navigation clears only guidance, not companion state");
                    check(host.GPS.RestoreServiceRoute(),"Restore private-stop navigation");
                    check(circle!=null && Mathf.Abs(Vector3.ProjectOnPlane(circle.GetPosition(0)-stop.Position,Vector3.up).magnitude-stop.radius)<.2f,
                        $"Private circle matches gameplay radius {stop.radius:F1}m");
                    body.position=stop.Position+Vector3.up*1.5f; Physics.SyncTransforms();
                    if(host.Player.TruckControlController.CurrentState.parkingBrakeOn)
                    { Press(keyboard,Key.P); yield return null; Release(keyboard); yield return null; }
                    yield return new WaitForSecondsRealtime(.3f);
                    var caption=FindText(host,"Private stop label");
                    check(circle!=null && circle.gameObject.activeInHierarchy && circle.enabled && caption!=null && caption.text.Contains("BRAKE"),
                        "Inside private circle shows parking-brake prompt");
                    GameObject eject=null;
                    foreach(var trigger in host.hud.Root.GetComponentsInChildren<EventTrigger>(true))
                        if(trigger.gameObject.activeInHierarchy && trigger.GetComponentInChildren<TMP_Text>()?.text.Contains("EJECT")==true) { eject=trigger.gameObject; break; }
                    check(eject!=null,"Normal companion Eject Passenger HUD button visible");
                    var actor=host.Player.GetComponentInChildren<TruckTaxiPassengerActor>();
                    capture?.Invoke("Polish_PrivateStop_"+attempt);
                    float thirst=host.DriverNeeds.State.Thirst;
                    if(attempt==0 && eject!=null)
                        ExecuteEvents.Execute<IPointerDownHandler>(eject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerDownHandler);
                    else Press(keyboard,Key.F);
                    yield return new WaitForSecondsRealtime(1.45f); Release(keyboard);
                    if(eject!=null) ExecuteEvents.Execute<IPointerUpHandler>(eject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerUpHandler);
                    yield return null;
                    check(!host.Companions.HasOnboardCompanion,attempt==0 ? "HUD hold ejects companion" : "Existing F input ejects companion");
                    var ejectedBody=actor!=null ? actor.GetComponent<Rigidbody>() : null;
                    check(actor!=null && !actor.transform.IsChildOf(host.Player.transform) && ejectedBody!=null && !ejectedBody.isKinematic,
                        "Companion ejection uses a live detached physics body");
                    check(string.IsNullOrEmpty(host.GPS.TargetId) && (circle==null || !circle.gameObject.activeInHierarchy),
                        "Private route and circle clean up after eject");
                    bool activePrivateMarker=false;
                    foreach(var marker in host.GPS.MapMarkers.Markers)
                        if(marker!=null && marker.stableId==stop.stableId && marker.state==TruckTaxiMapMarkerState.Active) activePrivateMarker=true;
                    check(!activePrivateMarker,"Private active GPS marker cleans up after eject");
                    check(!host.Session.OffersSuppressed && host.Session.RideRequestsEnabled==(attempt==1),
                        "Eject releases offer suppression, preserves ON/OFF preference");
                    check(host.DriverNeeds.State.Thirst>=thirst-.01f,"Eject does not satisfy thirst");
                    host.Session.SetRideRequestsEnabled(false);
                    yield return new WaitForSecondsRealtime(.5f);
                }
                check(host.traffic.LogicalCount==150 && host.pedestrians.LogicalCount==720,"Regional logical population budgets retained");
            }
            finally
            {
                Release(keyboard); InputSystem.RemoveDevice(keyboard); InputSystem.settings=input; UnityEngine.Object.Destroy(temporary);
                host.Companions.Cancel(); host.Session.SetRideRequestsEnabled(preference);
                if(!hadPreference) { PlayerPrefs.DeleteKey(TruckTaxiSession.RideRequestsPreferenceKey); PlayerPrefs.Save(); }
                body.position=start; body.rotation=rotation; body.isKinematic=kinematic; Physics.SyncTransforms();
            }
        }
        private static LineRenderer FindLine(TruckTaxiBootstrap host,string name)
        { foreach(var line in host.GetComponentsInChildren<LineRenderer>(true)) if(line.name==name) return line; return null; }
        private static TMP_Text FindText(TruckTaxiBootstrap host,string name)
        { foreach(var text in host.GetComponentsInChildren<TMP_Text>(true)) if(text.name==name) return text; return null; }
        private static void Press(Keyboard keyboard,Key key)
        {
            if(!keyboard.enabled) InputSystem.EnableDevice(keyboard);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));
        }
        private static void Release(Keyboard keyboard) => InputSystem.QueueStateEvent(keyboard,new KeyboardState());
    }
}
#endif
