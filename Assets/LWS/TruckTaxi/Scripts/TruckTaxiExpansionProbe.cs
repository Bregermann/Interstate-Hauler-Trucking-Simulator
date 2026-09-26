#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace LWS.TruckTaxi
{
    // Explicit integration fixture only. It never runs during a normal launch.
    public sealed class TruckTaxiExpansionProbe : MonoBehaviour
    {
        public bool Complete { get; private set; }
        public int Failures { get; private set; }
        public int Checks { get; private set; }
        private Keyboard keyboard;
        private Gamepad gamepad;
        private InputSettings originalInput;
        private string output;
        private TruckTaxiConfiguration testedConfiguration;
        private float originalRideFrequency;
        private int runtimeErrors;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Launch()
        {
            if(Application.isEditor || !Array.Exists(Environment.GetCommandLineArgs(),a=>a=="-truck-taxi-expansion-smoke")) return;
            new GameObject("Taxi expansion validation").AddComponent<TruckTaxiExpansionProbe>();
        }
        private void Check(bool condition,string message)
        {
            Checks++;
            if(condition) Debug.Log("EXPANSION PASS: "+message);
            else { Failures++; Debug.LogError("EXPANSION FAIL: "+message); }
        }
        private void Capture(string name) => TruckTaxiPlayerSmokeTest.Capture(Path.Combine(output,"Expansion_"+name+".png"));
        private IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject); Application.runInBackground=true;
            Application.logMessageReceived += ObserveLog;
            originalInput=InputSystem.settings; InputSystem.settings=Instantiate(originalInput);
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            keyboard=InputSystem.AddDevice<Keyboard>("Expansion keyboard"); gamepad=InputSystem.AddDevice<Gamepad>("Expansion gamepad");
            output=Application.isEditor ? Path.GetFullPath("Builds/TruckTaxiDemo/Validation") : Path.GetFullPath(Path.Combine(Application.dataPath,"../Validation"));
            Directory.CreateDirectory(output);
            Screen.SetResolution(1920,1080,FullScreenMode.Windowed);
            yield return new WaitForSecondsRealtime(1);
            if(SceneManager.GetActiveScene().name==TruckTaxiMainMenu.SceneName)
            {
                Capture("MainMenu");
                Check(SelectButton("NEW GAME"),"Main menu NEW GAME is selectable"); yield return PressEnter();
                Check(GameObject.Find("Mode select")!=null,"Keyboard opens mode select");
                Check(SelectButton("FREE PLAY"),"FREE PLAY is selectable");
                InputSystem.QueueStateEvent(gamepad,new GamepadState().WithButton(GamepadButton.South)); yield return null;
                InputSystem.QueueStateEvent(gamepad,new GamepadState());
            }
            yield return Until(()=>TruckTaxiBootstrap.Instance?.Ready==true,60);
            var host=TruckTaxiBootstrap.Instance;
            Check(host?.Ready==true,"Free Play scene and runtime truck initialized");
            if(host?.Ready!=true) { Finish(); yield break; }
            yield return new WaitForSecondsRealtime(2);
            Check(host.Audio.Ready && host.Audio.Buses.Count==5,"Five authored mixer buses connected");
            Check(Shader.Find("Hidden/NOT_Lonely/Weatherade/NL_TexturePacking") != null &&
                Shader.Find("Hidden/NOT_Lonely/NL_GaussianBlur") != null &&
                Shader.Find("Hidden/NOT_Lonely/Weatherade/DepthRenderer") != null,
                "Weatherade runtime shaders retained in player");
            Check(host.Fuel?.Fuel!=null,"Existing NWH fuel module connected");
            Check(TruckTaxiGasStationPoint.Points.Count>0,"Authored gas station registered");
            Check(host.DriverNeeds?.Stores.Count>=2,"Store examples registered");
            var roster=host.configuration.passengerDatabase.passengers;
            Check(roster.Count(p=>p.passengerId.StartsWith("glam-",StringComparison.Ordinal))>=24,"24 adult glam profiles registered");
            Check(roster.Count(p=>p.passengerId.StartsWith("racing-",StringComparison.Ordinal))==5,"Five original racing profiles registered");
            foreach(var compass in new[]{host.GPS.CabCompass,host.GPS.HudCompass})
            {
                var sprite=compass?.GetType().GetProperty("miniMapCardinalsSprite")?.GetValue(compass) as Sprite;
                Check(sprite!=null && sprite.name=="NorthOnly","Compass uses north-only sprite without replacing map orientation");
            }
            testedConfiguration=host.configuration; originalRideFrequency=testedConfiguration.rideFrequency;
            host.configuration.rideFrequency=100000; host.StartShift();
            host.Environment.SetAutomaticWeather(false); host.Environment.SetFrozen(true);
            var body=host.Player.GetComponent<Rigidbody>(); body.isKinematic=true;
            Capture("City");
            var source=roster.First(p=>p.passengerId.StartsWith("racing-",StringComparison.Ordinal));
            var passenger=Instantiate(source); passenger.specialAppreciationEligible=false;
            passenger.uniqueMechanics=Array.Empty<TruckTaxiMechanic>(); passenger.basePatience=passenger.requestFrequency=10000;
            passenger.possibleRequests=Array.Empty<PassengerRequestDefinition>(); passenger.districts=Array.Empty<string>();
            Check(host.Session.OfferRide(passenger) && host.Session.AcceptRide(),"Racing passenger accepted through existing flow");
            Check(host.Session.PickupRemaining>0 && host.Session.PickupRemaining<=300,"Post-accept pickup timer bounded");
            string destination=host.Session.Destination.locationId;
            Vector3 dropoffPosition=host.Session.Destination.StopPosition;
            host.TeleportNear(host.Session.Pickup);
            yield return Until(()=>host.Session.HasPassenger,15);
            Check(host.Session.HasPassenger,"Passenger boards at the actual pickup zone (teleport-assisted)");
            if(host.Session.HasPassenger)
            {
                yield return TruckTaxiVehicleObjectivesProbe.Run(host,Check);
                Capture("VehicleObjectives");
                host.TeleportNear(host.Session.Destination);
                yield return Until(()=>host.Session.State==TruckTaxiState.RideComplete,8);
                Check(host.Session.RideHistory.Count==1,"Ride completion records immutable history"); Capture("FareGoals");
                host.Session.ContinueShift();
                bool repeat=host.Session.OfferRide(passenger);
                Check(repeat && (host.Session.Pickup.locationId==destination ||
                    Vector3.Distance(host.Session.Pickup.StopPosition,dropoffPosition)<=host.configuration.repeatPickupAccessRadiusMeters),
                    "Recent repeat originates at previous dropoff or its nearby legal pickup bay");
                if(!repeat || !host.Session.AcceptRide()) { Finish(); yield break; }
                var away=host.Session.Pickup.StopPosition+Vector3.right*100;
                host.Session.Tick(host.Session.PickupDuration+1,away,0,0,true);
                Check(host.Session.State==TruckTaxiState.Available && host.Session.PickupCancellations==1,"Late pickup cancels and returns to Available");
                Check(string.IsNullOrEmpty(host.GPS.TargetId),"Cancellation clears GPS objective");
                host.Session.ContinueShift();
            }
            var needs=host.DriverNeeds;
            if(needs.Stores.Count>0)
            {
                Place(host,needs.Stores[0].Position); needs.RefreshProximity();
                long cash=host.Session.WalletBalanceCents;
                Check(needs.BuyItem(TruckTaxiNeedsItem.WaterBottle),"Stationary store purchase uses session wallet");
                Check(host.Session.WalletBalanceCents<cash,"Store charged actual cents");
                Check(needs.ConsumeItem(TruckTaxiNeedsItem.WaterBottle) && needs.State.Count(TruckTaxiNeedsItem.EmptyBottle)>0,"Drinking creates an empty container");
                host.hud.EnvironmentNeeds.Open(); yield return null; Capture("DriverNeeds"); host.hud.EnvironmentNeeds.Close();
            }
            host.SetPaused(false); needs.State.SetPressure(.9f);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Insert));
            yield return Until(()=>needs.JugActive,1);
            Check(needs.JugActive && needs.LoopAudioActive,"Real Insert input starts QTE and liquid sound");
            yield return TruckTaxiEnvironmentRuntimeProbe.CompleteJug(needs,keyboard,null);
            Check(needs.State.FilledJug && !needs.JugActive && !needs.LoopAudioActive,"Timed Input System cues complete and stop liquid audio");
            Check(needs.ThrowFilledContainer(),"Filled container throw accepted"); yield return new WaitForSecondsRealtime(.3f);
            var thrown=FindFirstObjectByType<TruckTaxiFilledContainerProjectile>();
            Check(thrown!=null && thrown.GetComponent<Rigidbody>()!=null && !thrown.GetComponent<Rigidbody>().isKinematic && host.Session.ContainersThrown>0,"Physical thrown container and session stat exist");
            Capture("Throw");
            var intersections=FindObjectsByType<TruckTaxiIntersection>(FindObjectsSortMode.None);
            Check(intersections.Length>0 && intersections.All(x=>x.IsConfigured),"UTS signals and crosswalks have actual vendor bindings");
            Check(host.pedestrians.pedestrianAreas.Length>3,"Bounded varied pedestrian areas registered");
            var rage=host.traffic.Vehicles.Select(v=>v.GetComponent<TruckTaxiRoadRage>()).FirstOrDefault(r=>r!=null);
            Check(rage!=null && rage.TryTrigger(),"Actual UTS traffic accepts bounded aggression");
            yield return new WaitForSecondsRealtime(5);
            Check(TruckTaxiRoadRage.ActiveCount==0,"Aggression restores normal UTS values after its duration");
            host.Environment.ForceWeather(TruckTaxiSnow.BlizzardId); host.Environment.SetFrozen(false);
            host.Environment.Snow.Region.Accumulate(.6f);
            host.Environment.Snow.GetComponent<TruckTaxiSnowSurface>().MarkDirty();
            yield return new WaitForSecondsRealtime(7);
            var snow=host.Environment.Snow;
            Check(snow.GetComponent<TruckTaxiSnowSurface>().RenderedCells>0,"Snow builds visible physical depth geometry");
            yield return Until(()=>!string.IsNullOrEmpty(snow.Plow.VehicleId),30);
            string plowId=snow.Plow.VehicleId;
            Vector3 before=host.traffic.TryResolveVehicle(plowId,out var plow) ? plow.transform.position : Vector3.zero;
            int cleared=snow.Plow.ClearedCellCount;
            // UTS is allowed to wait at a red light; it still must physically travel and clear new cells.
            yield return Until(()=>plow!=null && Vector3.Distance(before,plow.transform.position)>1 &&
                snow.Plow.ClearedCellCount>cleared,30);
            Debug.Log("EXPANSION PLOW: "+snow.Plow.Diagnostic+" id="+plowId+" resolved="+(plow!=null)+
                " travel="+(plow!=null ? Vector3.Distance(before,plow.transform.position) : 0)+" cleared="+(snow.Plow.ClearedCellCount-cleared));
            Check(plow!=null && Vector3.Distance(before,plow.transform.position)>1,"UTS physically drives the plow");
            Check(snow.Plow.ClearedCellCount>cleared,"Moving blade clears only swept snow cells"); Capture("Blizzard");
            host.Environment.ForceWeather("clear");
            host.Environment.Snow.Region.ClearSweep(new Vector3(-1000,0,0),new Vector3(1000,0,0),3000,0);
            host.Environment.Snow.GetComponent<TruckTaxiSnowSurface>().MarkDirty();
            yield return new WaitForSecondsRealtime(.6f);
            if(TruckTaxiGasStationPoint.Points.Count>0)
            {
                body.isKinematic=false;
                long charges=host.Session.ServiceChargesCents;
                host.Fuel.Fuel.amount=0;
                yield return Until(()=>host.Fuel.IsRescuing,2); Check(host.Fuel.IsRescuing,"Empty NWH tank starts cartoon rescue");
                yield return new WaitForSecondsRealtime(.25f); Capture("FuelLaunch");
                yield return Until(()=>!host.Fuel.IsRescuing,10);
                Check(host.Fuel.Rescues==1 && host.Fuel.Fraction>.98f,"Fuel rescue refills exactly once");
                Check(host.Session.ServiceChargesCents>charges,"Rescue charges double-cost service rather than free reset");
                var station=TruckTaxiGasStationPoint.Nearest(body.position);
                Check(station!=null && Vector3.ProjectOnPlane(station.RecoveryPosition-body.position,Vector3.up).magnitude<5,"Recovery arrives at nearest gas bay");
                Check(Vector3.Dot(body.transform.up,Vector3.up)>.9f,"NWH/LWS recovery returns upright"); Capture("GasRecovery");
            }
            Destroy(passenger);
            host.ReturnToMainMenu();
            yield return Until(()=>SceneManager.GetActiveScene().name==TruckTaxiMainMenu.SceneName,10);
            yield return null;
            Check(TruckTaxiMainMenu.LastSessionStats?.rides==1,"Main menu receives actual completed session stats");
            Check(FindObjectsByType<LWS.InterstateHauler.LwsPlayerTruck>(FindObjectsSortMode.None).Length==0,"Returning to menu removes gameplay tractor");
            Capture("ReturnMenu");
            Debug.Log("EXPANSION NOTE: teleport-assisted integration, not manual driving or human audio listening.");
            Finish();
        }
        private void Finish()
        {
            Check(runtimeErrors==0,"No unexpected runtime errors/exceptions (count="+runtimeErrors+")");
            Complete=true; Debug.Log($"TRUCK TAXI EXPANSION COMPLETE checks={Checks} failures={Failures}");
            if(!Application.isEditor) Application.Quit(Failures==0 ? 0 : 2);
        }
        private static bool SelectButton(string name)
        {
            var button=GameObject.Find(name); if(button==null || EventSystem.current==null) return false;
            EventSystem.current.SetSelectedGameObject(button); return true;
        }
        private IEnumerator PressEnter()
        {
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Enter)); yield return null;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState()); yield return null;
        }
        private static IEnumerator Until(Func<bool> predicate,float seconds)
        { float end=Time.realtimeSinceStartup+seconds; while(!predicate() && Time.realtimeSinceStartup<end) yield return null; }
        private static void Place(TruckTaxiBootstrap host,Vector3 position)
        { var body=host.Player.GetComponent<Rigidbody>(); body.position=position+Vector3.up*1.6f; host.Player.transform.position=body.position; Physics.SyncTransforms(); host.Session.DiscardTeleportDistance(); }
        private void OnDestroy()
        {
            Application.logMessageReceived -= ObserveLog;
            if(testedConfiguration!=null) testedConfiguration.rideFrequency=originalRideFrequency;
            if(keyboard!=null) InputSystem.RemoveDevice(keyboard); if(gamepad!=null) InputSystem.RemoveDevice(gamepad);
            if(originalInput!=null) { var temporary=InputSystem.settings; InputSystem.settings=originalInput; Destroy(temporary); }
        }
        private void ObserveLog(string message,string stack,LogType type)
        {
            if((type==LogType.Exception || type==LogType.Assert || type==LogType.Error) &&
                !message.StartsWith("EXPANSION FAIL:",StringComparison.Ordinal)) runtimeErrors++;
        }
    }
}
#endif
