using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LWS.InterstateHauler;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace LWS.TruckTaxi
{
    public sealed partial class TruckTaxiRestartHarness
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private string dualProfile;
        private readonly List<string> dualEvidence = new List<string>();
        private void RecordDualEvidence(string evidence)
        {
            dualEvidence.Add(evidence);
            Debug.Log(Prefix + evidence);
            File.AppendAllText(Path.Combine(request.Root, "dual-world-progress-" + request.Mode + ".txt"), evidence + Environment.NewLine);
        }
        private IEnumerator RunDualWorld()
        {
            if (request.Mode == "seed")
            {
                Check(FindTestProfile(false) == null, "dual-world fresh disposable vendor directory");
                menu.NewVibinInWorld(TruckTaxiWorldCatalog.Demo);
                yield return WaitForRun(); yield return AcknowledgeFirstDay();
                Result(saves.RenameProfile(host.Persistence.RunProfileId, TruckTaxiRestartTestArguments.ProfileName), "dual-world isolated profile named");
                dualProfile = host.Persistence.RunProfileId;
                Check(host.Session.TryRestorePersistentState(CreateSessionFixture(0,host.Session.CapturePersistentState()),out var reason), "dual-world career fixture: "+reason);
                Check(host.Session.TrySetDriverName("Dual World Driver",out reason), "dual-world driver identity: "+reason);
                var needs = host.DriverNeeds.CapturePersistentState();
                needs.AmbientChoiceIds = new[] { "astra.dualworld." + request.RunId };
                needs.Hunger = needs.Thirst = needs.Fatigue = needs.Pressure = needs.BowelPressure = .2f;
                foreach (var item in needs.Inventory) if (item.ItemId == "snack") item.Count = 9;
                Check(host.DriverNeeds.RestorePersistentState(needs,out reason),"dual-world inventory fixture: "+reason);
                Check(host.Session.TryApplyGarage(host.CareerEquipment.Catalog,host.Player.DefinitionId,
                    new TruckTaxiGaragePaintData { color = EdenboroughTestPaint },out reason),"dual-world authored paint: "+reason);
                host.Fuel.Fuel.amount = 40;
                Check(host.Player.TransmissionController.TrySetAutomaticMode(out reason),"dual-world seed automatic mode: "+reason);
                yield return DualPlaceAtExit(false);
                yield return PrepareDriveSeedEngine(); host.SetPaused(false);
                Check(host.CareerEquipment.PurchaseOrEquip("taxi.upgrade.brake-hardware"), "purchase existing authored brake upgrade");
                Check(host.CareerEquipment.PurchaseOrEquip("taxi.upgrade.brake-hardware"), "equip existing purchased brake upgrade");
                var damage = host.ComponentDamage.Capture();
                string bodyPart = SelectBodyPart(host.ComponentDamage.Parts);
                foreach (var part in damage.Parts) if (part.Id == bodyPart) part.Condition = .87f;
                Check(host.ComponentDamage.Restore(damage), "nonzero body-only damage fixture through existing semantic restore");
                yield return DualNativeDrive("Demo City initial drive");
                Result(host.Persistence.Save(1,true),"dual-world Demo City manual checkpoint");
                yield return DualTransfer(true);
                yield return DualNativeDrive("Edenborough physical arrival");
                yield return DualTransfer(true);
                yield return DualNativeDrive("Demo City physical return");
                yield return DualTravelCancelInputs();
                yield return DualTransfer(false, true);
                yield return DualNativeDrive("Edenborough menu arrival");
                // Ten transfers in the same real player process; first two use native trigger contacts.
                for (int i = 3; i < 10; i++)
                {
                    host.hud.WorldTravel.Open();
                    Check(host.hud.WorldTravel.IsOpen,"actual pause travel panel opens");
                    host.hud.WorldTravel.Close(); host.SetPaused(false);
                    Check(!host.hud.WorldTravel.IsOpen && !TruckTaxiWorldTravel.IsBusy,"cancel does not change world");
                    yield return DualTransfer(false);
                    yield return DualNativeDrive("menu arrival " + i);
                }
                Check(TruckTaxiWorldTravel.CompletedTransfers == 10,"ten successful transfers in one process");
                yield return DualSnowViews("DemoCity");
                yield return DualOffroadTracks("DemoCity");
                // Leave the restart checkpoint in Edenborough; next process returns to Demo City.
                yield return DualTransfer(false);
                yield return DualSnowViews("Edenborough");
                yield return DualOffroadTracks("Edenborough");
                Result(host.Persistence.Save(2,true),"dual-world Edenborough manual checkpoint");
                Result(host.Persistence.SaveBeforeLeaving(),"dual-world Edenborough quit checkpoint");
            }
            else
            {
                bool returningContinue = request.Mode == "continue" && File.Exists(Path.Combine(request.Root, "dual-world-continue.txt"));
                dualProfile = FindTestProfile(true).stableProfileId;
                if (request.Mode == "manual") { openLoad.Invoke(menu,null); loadSlot.Invoke(menu,new object[] { 0 }); }
                else continueGame.Invoke(menu,null);
                yield return WaitForRun(false);
                string expected = request.Mode == "continue" && !returningContinue ? TruckTaxiWorldCatalog.Edenborough : TruckTaxiWorldCatalog.Demo;
                Check(host.gameObject.scene.name == expected,"fresh process " + request.Mode + " restored saved world "+expected);
                DualCareerAssertions();
                if (request.Mode == "continue")
                {
                    string weather = returningContinue ? LwsWeatherPresetCatalog.LightRainId : LwsWeatherPresetCatalog.HeavySnowId;
                    float depth = host.Environment.Snow.Capture().backgroundDepth;
                    Check(host.Environment.Weather.CurrentSnapshot.weatherPresetId == weather && float.IsFinite(depth) &&
                        (returningContinue ? depth >= 0 : depth > .1f),
                        "saved region retains its own weather and snow/melt state after process restart");
                }
                yield return DualNativeDrive("fresh process " + expected);
                if (request.Mode == "continue" && !returningContinue)
                {
                    var visits = TruckTaxiVibinPersistence.ReadTravelPayload(saves.CaptureSnapshot(dualProfile)).worldVisits;
                    var demo = visits.Single(v => v.scene == TruckTaxiWorldCatalog.Demo);
                    yield return DualTransfer(false);
                    double now = TruckTaxiCalendarWeatherService.SnapshotDateTime(host.Environment.Clock.CurrentSnapshot).Ticks / (double)TimeSpan.TicksPerSecond;
                    var weather = JsonUtility.FromJson<LwsWeatherSavePayload>(demo.weather.payloadJson);
                    TruckTaxiWorldVisit.AdvanceSnow(demo.environment, now-demo.gameSeconds, demo.clockRate, host.Environment.settings,
                        TruckTaxiCalendarWeatherService.ConditionFor(weather.weatherPresetId), weather.ambientTemperatureC);
                    Check(host.Environment.Weather.CurrentSnapshot.weatherPresetId == LwsWeatherPresetCatalog.LightRainId &&
                        Mathf.Abs(host.Environment.Snow.Capture().backgroundDepth-demo.environment.snow.backgroundDepth)<.02f,
                        "returning Demo City keeps its own rainy snow state with elapsed-time melting, not Edenborough weather");
                    Result(host.Persistence.SaveBeforeLeaving(),"dual-world Demo City quit checkpoint");
                }
                else if (request.Mode == "manual")
                {
                    yield return DualRollbackFixture();
                    yield return DualNativeDrive("failed-transfer rollback");
                    yield return DualSnowViews("DemoCity");
                    yield return DualOffroadTracks("DemoCity");
                    yield return DualTransfer(false);
                    yield return DualTownSnowInspection();
                    host.Environment.SetFrozen(false);
                    yield return DualPlaceAtExit(false);
                    yield return DualOffroadTracks("Edenborough");
                    Result(host.Persistence.Save(2,true), "manual regression leaves a snowy Edenborough restart checkpoint");
                    Result(host.Persistence.SaveBeforeLeaving(), "manual regression quit checkpoint");
                }
            }
            File.WriteAllLines(Path.Combine(request.Root,"dual-world-"+request.Mode+".txt"),dualEvidence);
        }

        private TruckTaxiWorldExit DualExit() => FindObjectsByType<TruckTaxiWorldExit>(FindObjectsSortMode.None)
            .Single(e=>e.gameObject.scene == host.gameObject.scene);

        private IEnumerator DualPlaceAtExit(bool outbound)
        {
            var exit = DualExit(); Vector3 forward = exit.transform.forward;
            Vector3 point = outbound ? exit.transform.position - forward * 28 : exit.arrival.position;
            var world = host.GetComponent<TruckTaxiRegionalWorld>();
            yield return world.PrepareInitialWorld(point);
            Quaternion rotation = outbound ? exit.transform.rotation : exit.arrival.rotation;
            var body = host.Player.GetComponent<Rigidbody>(); Physics.SyncTransforms();
            Check(TruckTaxiRecoverySpace.TryGroundedPose(body,point,rotation,.35f,body,null,out var grounded),"test-only approach fixture has valid drivable collision");
            TruckTaxiRecoverySpace.Move(body,grounded,rotation); body.linearVelocity=body.angularVelocity=Vector3.zero;
            host.Session.DiscardTeleportDistance(); Physics.SyncTransforms(); yield return null;
            Check(host.GPS.TrySetServiceDestination(exit.GetComponent<TruckTaxiMapMarker>().stableId,
                exit.GetComponent<TruckTaxiMapMarker>().label,exit.transform.position,TruckTaxiMapMarkerType.Place),"local exit accepted by existing GPS");
            float until=Time.realtimeSinceStartup+8;
            while (!host.GPS.RouteReady && Time.realtimeSinceStartup<until) yield return null;
            Check(host.GPS.RouteReady,"local road-graph route to regional exit calculated");
            yield return DualScreenshot(host.gameObject.scene.name+"-exit");
        }

        private IEnumerator DualTransfer(bool physical, bool gamepadConfirm = false)
        {
            string source = host.gameObject.scene.name, destination = TruckTaxiWorldCatalog.Other(source);
            DateTime time = TruckTaxiCalendarWeatherService.SnapshotDateTime(host.Environment.Clock.CurrentSnapshot);
            long wallet = host.Session.WalletBalanceCents;
            float fuel = host.Fuel.Fuel.amount;
            if (physical)
            {
                host.SetPaused(false); yield return DualPlaceAtExit(true);
                var keyboard = InputSystem.AddDevice<Keyboard>("Dual-world physical exit keyboard"); var previous=Keyboard.current; keyboard.MakeCurrent();
                try
                {
                    if (host.Player.TruckControlController.CurrentState.parkingBrakeOn)
                    { InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.P)); yield return null; InputSystem.QueueStateEvent(keyboard,new KeyboardState()); yield return null; }
                    float until=Time.realtimeSinceStartup+30;
                    while (!TruckTaxiWorldTravel.IsBusy && Time.realtimeSinceStartup<until)
                    { InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W)); yield return null; }
                    Check(TruckTaxiWorldTravel.IsBusy,"actual NWH driven tractor contact activated directional exit");
                    // Compare the departure checkpoint, not fuel consumed while driving to the trigger.
                    fuel = host.Fuel.Fuel.amount;
                }
                finally { InputSystem.RemoveDevice(keyboard); if(previous!=null && previous.added)previous.MakeCurrent(); }
            }
            else
            {
                host.SetPaused(true); host.hud.WorldTravel.Open();
                yield return null; // A real player cannot confirm before the opened UI receives its first frame.
                if (gamepadConfirm)
                {
                    var pad = InputSystem.AddDevice<Gamepad>("Dual-world travel UI gamepad");
                    try
                    {
                        pad.MakeCurrent();
                        InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(GamepadButton.South));
                        yield return null; yield return null;
                        InputSystem.QueueStateEvent(pad, new GamepadState());
                        yield return null;
                    }
                    finally { InputSystem.RemoveDevice(pad); }
                }
                else
                {
                    var button = host.hud.WorldTravel.FocusRoot.GetComponentsInChildren<Button>().First(b=>b.GetComponentInChildren<TMPro.TextMeshProUGUI>()?.text=="TRAVEL");
                    // Heat owns its click event; the co-located Unity Button can have no listeners.
                    UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(button.gameObject);
                    host.hud.UIInput.SubmitSelected();
                }
                Check(TruckTaxiWorldTravel.IsBusy,"actual travel panel confirm callback started transaction | status="+
                    TruckTaxiWorldTravel.LastStatus+" safety="+host.Persistence.WorldTransferBlockReason);
            }
            yield return null; yield return DualScreenshot(source+"-loading");
            float deadline = Time.realtimeSinceStartup+180;
            while (TruckTaxiWorldTravel.IsBusy && Time.realtimeSinceStartup<deadline) yield return null;
            Check(!TruckTaxiWorldTravel.IsBusy,"world transfer completed before timeout: "+TruckTaxiWorldTravel.LastStatus);
            yield return new WaitForEndOfFrame(); yield return null;
            host=TruckTaxiBootstrap.Instance; saves=host.Persistence.SaveService;
            Check(host.gameObject.scene.name==destination,"arrived in intended world "+destination+" | "+TruckTaxiWorldTravel.LastStatus);
            Check(host.Session.WalletBalanceCents==wallet,"wallet preserved through transaction");
            Check(Mathf.Abs(host.Fuel.Fuel.amount-fuel)<.1f, "fuel quantity retained across regional arrival: "+fuel+" -> "+host.Fuel.Fuel.amount);
            DateTime arrivalTime = TruckTaxiCalendarWeatherService.SnapshotDateTime(host.Environment.Clock.CurrentSnapshot);
            Check(arrivalTime>=time,"global calendar never moved backward: "+time.ToString("O")+" -> "+arrivalTime.ToString("O"));
            DualCareerAssertions();
            Check(FindObjectsByType<TruckTaxiBootstrap>(FindObjectsSortMode.None).Length==1 &&
                FindObjectsByType<LwsPlayerTruck>(FindObjectsSortMode.None).Length==1 &&
                FindObjectsByType<TruckTaxiHud>(FindObjectsSortMode.None).Length==1,"one host, truck and HUD after transfer");
            Check(FindObjectsByType<TruckTaxiWorldTravel>(FindObjectsSortMode.None).Length==0,"loading transaction retired");
            Check(Vector3.Distance(host.Player.transform.position,DualExit().transform.position)>25,"arrival does not overlap outbound trigger");
            var localExit = DualExit(); var marker = localExit.GetComponent<TruckTaxiMapMarker>();
            Check(host.GPS.TrySetServiceDestination(marker.stableId, marker.label, localExit.transform.position, TruckTaxiMapMarkerType.Place),
                "arrived region accepts its own GPS exit objective");
            float routeDeadline = Time.realtimeSinceStartup + 8;
            while (!host.GPS.RouteReady && Time.realtimeSinceStartup < routeDeadline) yield return null;
            Check(host.GPS.RouteReady, "current region graph and Compass route rebound after transfer");
            RecordDualEvidence("TRANSFER "+TruckTaxiWorldTravel.CompletedTransfers+" "+source+" -> "+destination+
                " objects="+FindObjectsByType<Transform>(FindObjectsSortMode.None).Length+
                " managed="+GC.GetTotalMemory(false)+" allocated="+UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()+
                " traffic="+host.traffic.LogicalCount+" peds="+host.pedestrians.ActiveCount+
                " weather="+host.Environment.Weather.CurrentSnapshot.weatherPresetId+" snow="+host.Environment.Snow.Capture().backgroundDepth);
            yield return DualScreenshot(destination+"-arrival-"+TruckTaxiWorldTravel.CompletedTransfers);
        }

        private IEnumerator DualTravelCancelInputs()
        {
            host.SetPaused(false); host.hud.WorldTravel.Open();
            var keyboard = InputSystem.AddDevice<Keyboard>("Dual-world travel cancel keyboard");
            try
            {
                keyboard.MakeCurrent(); InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
                yield return null; yield return null;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
                Check(!host.hud.WorldTravel.IsOpen && !host.Paused && !TruckTaxiWorldTravel.IsBusy,
                    "Escape cancels travel and restores previous driving state without also opening Pause");
            }
            finally { InputSystem.RemoveDevice(keyboard); }
            host.hud.WorldTravel.Open();
            var pad = InputSystem.AddDevice<Gamepad>("Dual-world travel cancel gamepad");
            try
            {
                pad.MakeCurrent(); InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(GamepadButton.East));
                yield return null; yield return null;
                InputSystem.QueueStateEvent(pad, new GamepadState()); yield return null;
                Check(!host.hud.WorldTravel.IsOpen && !host.Paused && !TruckTaxiWorldTravel.IsBusy,
                    "gamepad East cancels travel through the existing UI actions");
            }
            finally { InputSystem.RemoveDevice(pad); }
        }

        private IEnumerator DualRollbackFixture()
        {
            string source = host.gameObject.scene.name, target = TruckTaxiWorldCatalog.Other(source);
            int completed = TruckTaxiWorldTravel.CompletedTransfers;
            long wallet = host.Session.WalletBalanceCents;
            Check(host.WorldLife.TryExitTruck(), "safe on-foot world travel fixture: " + host.WorldLife.ExitBlockReason);
            // Disposable-player fixture only: reject this arrival without editing a scene asset or save.
            void RejectArrival(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
            {
                if (scene.name != target) return;
                foreach (var exit in FindObjectsByType<TruckTaxiWorldExit>(FindObjectsSortMode.None))
                    if (exit.gameObject.scene == scene) exit.arrival = null;
            }
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += RejectArrival;
            try
            {
                Check(TruckTaxiWorldTravel.TryTravel(host, target, false), "safe on-foot transfer enters existing transaction");
                float until = Time.realtimeSinceStartup + 240;
                while (TruckTaxiWorldTravel.IsBusy && Time.realtimeSinceStartup < until) yield return null;
                Check(!TruckTaxiWorldTravel.IsBusy, "controlled rejected arrival resolves without a permanent loading lock");
                yield return new WaitForEndOfFrame(); yield return null;
                host = TruckTaxiBootstrap.Instance; saves = host.Persistence.SaveService;
                Check(host.gameObject.scene.name == source && TruckTaxiWorldTravel.CompletedTransfers == completed,
                    "rejected destination rolls back to the source rather than counting a successful transfer");
                Check(host.Session.WalletBalanceCents == wallet && !host.WorldLife.IsOnFoot,
                    "rollback preserves wallet and safely returns the on-foot driver to the truck");
                DualCareerAssertions();
                Check(FindObjectsByType<TruckTaxiWorldTravel>(FindObjectsSortMode.None).Length == 0,
                    "failed-transfer loading overlay and transaction retired");
                RecordDualEvidence("ROLLBACK " + source + " | " + TruckTaxiWorldTravel.LastStatus);
                yield return DualScreenshot(source + "-rollback");
            }
            finally { UnityEngine.SceneManagement.SceneManager.sceneLoaded -= RejectArrival; }
        }

        private void DualCareerAssertions()
        {
            Check(host.Persistence.RunProfileId==dualProfile && saves.ActiveProfileId==dualProfile,"same career identity across worlds/restart");
            var career=host.Session.CaptureCareerProfile();
            Check(career.driverName=="Dual World Driver","driver name retained");
            var owned=career.trucks.Find(t=>t.truckId==host.Player.DefinitionId);
            Check(owned!=null && owned.hasCustomPaint && Vector4.Distance(owned.paint.color,EdenboroughTestPaint)<.001f,"paint and truck ownership retained");
            Check(host.Player.GetComponent<TruckTaxiGaragePaint>()?.Ready == true,"existing paint presentation bound in current world");
            Check(host.DriverNeeds.State.Count(TruckTaxiNeedsItem.Snack)==9,"inventory retained without starter grants");
            Check(host.DriverNeeds.State.AmbientChoiceIds.Contains("astra.dualworld."+request.RunId),"needs/progression marker retained");
            Check(host.Session.CapturePersistentState().statistics.completedRides==2,"completed fares retained");
            Check(host.CareerEquipment.IsOwned("taxi.upgrade.brake-hardware") && host.CareerEquipment.IsEquipped("taxi.upgrade.brake-hardware"),
                "purchased and equipped authored upgrade retained");
            Check(Mathf.Abs(host.ComponentDamage.Parts[SelectBodyPart(host.ComponentDamage.Parts)].Condition-.87f)<.005f,
                "nonzero component damage retained");
        }

        private IEnumerator DualNativeDrive(string label, bool curve = false)
        {
            var registry=LwsApplicationBootstrap.Instance.Registry; registry.TryGet(out ILwsVehicleInputService input); CaptureDriveInput(input);
            var keyboard=InputSystem.AddDevice<Keyboard>("Dual-world post-arrival keyboard"); var previous=Keyboard.current; driveKeyboard=keyboard; keyboard.MakeCurrent();
            try
            {
                RequireDriveReady(); var body=host.Player.GetComponent<Rigidbody>(); Vector3 start=body.position;
                if(host.Player.TruckControlController.CurrentState.parkingBrakeOn) yield return DrivePulse("ParkingBrake",Key.P);
                float until=Time.realtimeSinceStartup+18;
                while(Vector3.Distance(start,body.position)<3 && Time.realtimeSinceStartup<until)
                    yield return curve ? DriveKeys(1,Key.W,Key.D) : DriveKeys(1,Key.W);
                Check(Vector3.Distance(start,body.position)>=3 && host.Player.NwhAdapter.VehicleController.input.Throttle>.5f,label+" native throttle/forward motion without recovery");
                yield return DriveKeys(2,Key.W,Key.S);
                Check(host.Player.NwhAdapter.VehicleController.input.Brakes>.5f,label+" native brake input");
                until=Time.realtimeSinceStartup+8;
                while(body.linearVelocity.magnitude>.2f && Time.realtimeSinceStartup<until)yield return DriveKeys(1,Key.W,Key.S);
                yield return DriveKeys(3,Key.A); Check(host.Player.NwhAdapter.VehicleController.input.Steering<-.5f,label+" native steering");
                yield return DriveKeys(2);
                if(!host.Player.TruckControlController.CurrentState.parkingBrakeOn)yield return DrivePulse("ParkingBrake",Key.P);
                RecordDualEvidence("DRIVE "+label+" meters="+Vector3.Distance(start,body.position));
            }
            finally { InputSystem.RemoveDevice(keyboard); driveKeyboard=null; if(previous!=null&&previous.added)previous.MakeCurrent(); }
        }

        private IEnumerator DualSnowViews(string label)
        {
            bool frozen = host.Environment.Frozen;
            host.Environment.SetFrozen(true); host.SetPaused(false);
            foreach(float depth in new[]{0f,.2f,.85f})
            {
                string preset=depth==0 ? LwsWeatherPresetCatalog.ClearId : depth<.5f ? LwsWeatherPresetCatalog.LightSnowId : LwsWeatherPresetCatalog.HeavySnowId;
                Check(host.Environment.ForceWeather(preset),"existing weather controls apply actual snow atmosphere "+preset);
                yield return new WaitForSecondsRealtime(6);
                var snow=host.Environment.Snow.Capture(); snow.backgroundDepth=snow.maximumDepth*depth;
                for(int i=0;i<snow.depths.Length;i++)snow.depths[i]=snow.backgroundDepth;
                Check(host.Environment.Snow.Restore(snow),"controlled actual snow presentation fixture "+depth);
                var surface=host.GetComponent<TruckTaxiSnowSurface>();
                float until=Time.realtimeSinceStartup+30;
                while(!surface.EnvironmentBindingsReady && Time.realtimeSinceStartup<until){surface.Refresh();yield return null;}
                Check(surface.EnvironmentBindingsReady,"bounded outdoor material binding completed");
                yield return DualScreenshot(label+"-snow-"+depth);
                var ms=new List<float>(); until=Time.realtimeSinceStartup+10;
                while(Time.realtimeSinceStartup<until){ms.Add(Time.unscaledDeltaTime*1000);yield return null;}
                ms.Sort(); RecordDualEvidence("PERF "+label+" snow="+depth+" median="+ms[ms.Count/2]+" p95="+ms[(int)((ms.Count-1)*.95f)]+" p99="+ms[(int)((ms.Count-1)*.99f)]+
                    " materials="+surface.SharedEnvironmentMaterialCount+" surfaces="+surface.GroundSnowSurfaceCount+" terrain="+surface.TerrainSnowSurfaceCount+
                    " screen="+Screen.width+"x"+Screen.height+" GPU="+SystemInfo.graphicsDeviceName+" stationary truck; live NPCs; clock frozen for controlled snow; NOT isolated/driving/human acceptance");
                File.WriteAllLines(Path.Combine(request.Root,"dual-world-"+request.Mode+".txt"),dualEvidence);
            }
            host.Environment.SetFrozen(frozen);
            float before=host.Environment.Snow.Capture().backgroundDepth;
            yield return new WaitForSecondsRealtime(3);
            float accumulated=host.Environment.Snow.Capture().backgroundDepth;
            Check(accumulated>before,"live snowfall gradually increases depth");
            Check(host.Environment.ForceWeather(LwsWeatherPresetCatalog.LightRainId),"rain requested through existing weather control");
            yield return new WaitForSecondsRealtime(6);
            float melted=host.Environment.Snow.Capture().backgroundDepth;
            Check(melted>0 && melted<accumulated,"rain reduces snow gradually rather than instantly clearing it");
            RecordDualEvidence("SNOW_EVOLUTION "+label+" before="+before+" snowfall="+accumulated+" rain="+melted);
            if(label=="Edenborough")
            {
                Check(host.Environment.ForceWeather(LwsWeatherPresetCatalog.HeavySnowId),"Edenborough keeps its own snowy weather");
                yield return new WaitForSecondsRealtime(6);
            }
        }

        private IEnumerator DualTownSnowInspection()
        {
            // Disposable observation fixture: use the existing real spawn/garage frontage, not a new scene.
            var world = host.GetComponent<TruckTaxiRegionalWorld>();
            Vector3 point = host.playerSpawn.position;
            yield return world.PrepareInitialWorld(point);
            Vector3 look = TruckTaxiEdenboroughLayout.Garage - point; look.y = 0;
            Quaternion rotation = Quaternion.LookRotation(look.normalized, Vector3.up);
            var body = host.Player.GetComponent<Rigidbody>();
            Check(TruckTaxiRecoverySpace.TryGroundedPose(body, point, rotation, .35f, body, null, out var grounded),
                "existing town/garage frontage has safe observation ground");
            TruckTaxiRecoverySpace.Move(body, grounded, rotation); body.linearVelocity = body.angularVelocity = Vector3.zero;
            host.Session.DiscardTeleportDistance(); Physics.SyncTransforms();
            host.Environment.SetFrozen(true); host.SetPaused(false);
            foreach (float depth in new[] { 0f, .85f })
            {
                Check(host.Environment.ForceWeather(depth == 0 ? LwsWeatherPresetCatalog.ClearId : LwsWeatherPresetCatalog.HeavySnowId),
                    "town observation uses existing weather controls");
                yield return new WaitForSecondsRealtime(6);
                var snow = host.Environment.Snow.Capture(); snow.backgroundDepth = snow.maximumDepth * depth;
                for (int i = 0; i < snow.depths.Length; i++) snow.depths[i] = snow.backgroundDepth;
                Check(host.Environment.Snow.Restore(snow), "town snow observation fixture restored");
                var surface = host.GetComponent<TruckTaxiSnowSurface>(); float until = Time.realtimeSinceStartup + 30;
                while (!surface.EnvironmentBindingsReady && Time.realtimeSinceStartup < until) { surface.Refresh(); yield return null; }
                Check(surface.EnvironmentBindingsReady, "town imported surfaces and terrain finished binding");
                yield return new WaitForSecondsRealtime(2);
                RecordDualEvidence("TOWN_SNOW " + depth + " ground=" + surface.GroundSnowSurfaceCount + " terrain=" + surface.TerrainSnowSurfaceCount +
                    " sharedMaterials=" + surface.SharedEnvironmentMaterialCount);
                yield return DualScreenshot("Edenborough-garage-foliage-snow-" + depth);
                Vector3 roof = TruckTaxiEdenboroughLayout.Garage + Vector3.up * 3;
                yield return DualObservationScreenshot("Edenborough-rooftop-snow-" + depth, roof,
                    roof + new Vector3(18, 16, -22));
                var foliage = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
                    .Where(r => r.gameObject.scene == host.gameObject.scene && r.enabled && r.bounds.size.y > 3 &&
                        r.sharedMaterials.Any(m => m != null && m.shader != null &&
                            (m.shader.name.Contains("EnvironmentTree") || m.shader.name.Contains("EnvironmentFoliage"))))
                    .OrderBy(r => (r.bounds.center - point).sqrMagnitude).FirstOrDefault();
                if (foliage != null)
                {
                    Vector3 center = foliage.bounds.center;
                    float span = Mathf.Clamp(foliage.bounds.size.magnitude, 12, 35);
                    RecordDualEvidence("FOLIAGE_OBSERVATION " + foliage.name + " shader=" + foliage.sharedMaterial.shader.name);
                    yield return DualObservationScreenshot("Edenborough-imported-foliage-snow-" + depth, center,
                        center + new Vector3(span * .7f, span * .35f, -span));
                }
            }
        }

        private IEnumerator DualOffroadTracks(string label)
        {
            var body=host.Player.GetComponent<Rigidbody>(); Vector3 roadPoint=body.position; Quaternion roadRotation=body.rotation;
            var surface=host.GetComponent<TruckTaxiSnowSurface>();
            var snow=host.Environment.Snow.Capture();
            Check(host.Environment.Snow.Restore(snow),"off-road trace fixture resets only transient trace history");
            float bindDeadline=Time.realtimeSinceStartup+30;
            while(!surface.EnvironmentBindingsReady && Time.realtimeSinceStartup<bindDeadline){surface.Refresh();yield return null;}
            Check(surface.EnvironmentBindingsReady,"off-road fixture waits for restored environment bindings");
            bool placed=false;
            foreach(float offset in new[]{14f,-14f,25f,-25f})
            {
                Vector3 candidate=roadPoint+body.transform.right*offset;
                if(!TruckTaxiRecoverySpace.TryGroundedPose(body,candidate,roadRotation,.35f,body,null,out var grounded))
                {Debug.Log(Prefix+"OFFROAD candidate lacks clearance offset="+offset);continue;}
                if(!Physics.Raycast(grounded+Vector3.up*4,Vector3.down,out var hit,12,~0,QueryTriggerInteraction.Ignore) ||
                    hit.collider.GetComponentInParent<Rigidbody>()!=null || !surface.IsSnowCoveredOffRoad(hit.collider,hit.point,body.transform))
                {Debug.Log(Prefix+"OFFROAD candidate unsupported offset="+offset+" hit="+hit.collider?.name+" bound="+surface.GroundSnowSurfaceCount+" terrain="+surface.TerrainSnowSurfaceCount);continue;}
                TruckTaxiRecoverySpace.Move(body,grounded,roadRotation); body.linearVelocity=body.angularVelocity=Vector3.zero;
                host.Session.DiscardTeleportDistance(); Physics.SyncTransforms(); placed=true;break;
            }
            Check(placed,"supported snow-covered off-road fixture exists beside the real connector");
            yield return new WaitForFixedUpdate();
            int stamps=surface.WheelTraceStampCount; Quaternion before=body.rotation;
            yield return DualNativeDrive(label+" curved off-road snow",true);
            int contacts=0;
            foreach(var wheel in host.Player.NwhAdapter.VehicleController.powertrain.wheels)
                if(wheel.wheelUAPI.IsGrounded && surface.IsSnowCoveredOffRoad(wheel.wheelUAPI.HitCollider,
                    wheel.wheelUAPI.HitPoint,body.transform))contacts++;
            Check(contacts>=2 && surface.WheelTraceStampCount>stamps,"actual off-road wheel contacts generated Weatherade trace stamps");
            Check(Quaternion.Angle(before,body.rotation)>2,"native steering produced a curved snow path");
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var generator = typeof(TruckTaxiSnowSurface).GetField("generator", flags).GetValue(surface) as Component;
            var inputs = generator.GetType().GetField("traceMaskCalcMtl", flags).GetValue(generator) as Material;
            foreach (string name in new[] { "traceSurfCam", "traceObjsCam" })
            {
                var camera = generator.GetType().GetField(name, flags).GetValue(generator) as Camera;
                var data = camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                var pipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
                Debug.Log(Prefix + "TRACE_INPUT " + name + " mask=" + camera.cullingMask + " expectedRenderer=" +
                    ReferenceEquals(data.scriptableRenderer, pipeline.GetRenderer(surface.WeatheradeDepthRendererIndex)) +
                    " target=" + camera.targetTexture.GetInstanceID() + " input=" + inputs.GetTexture(name == "traceSurfCam" ? "_SRS_SurfaceDepth" : "_SRS_TraceObjsDepth")?.GetInstanceID() +
                    " area=" + camera.orthographicSize + " near/far=" + camera.nearClipPlane + "/" + camera.farClipPlane + " volume=" + inputs.GetFloat("_VolumeDepth"));
            }
            surface.RequestTracePixelDiagnostic(); float until=Time.realtimeSinceStartup+6;
            while(surface.TracePixelDiagnostic.Contains("pending") && Time.realtimeSinceStartup<until)yield return null;
            Check(surface.TraceActivePixelCount>0,"actual GPU Weatherade off-road trace mask contains tire marks: "+surface.TracePixelDiagnostic);
            RecordDualEvidence("OFFROAD "+label+" wheelContacts="+contacts+" newStamps="+(surface.WheelTraceStampCount-stamps)+" "+surface.TracePixelDiagnostic);
            yield return DualScreenshot(label+"-offroad-snow-tracks");
            Vector3 trail = body.position - body.transform.forward * 3;
            yield return DualObservationScreenshot(label + "-offroad-track-detail", trail,
                trail + Vector3.up * 12 - body.transform.forward * 7);
            TruckTaxiRecoverySpace.Move(body,roadPoint,roadRotation); body.linearVelocity=body.angularVelocity=Vector3.zero;
            host.Session.DiscardTeleportDistance(); Physics.SyncTransforms(); yield return new WaitForFixedUpdate();
        }
        private IEnumerator DualScreenshot(string name)
        {
            yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(request.Root,"dual-"+name+".png"));
            yield return null;
        }

        private IEnumerator DualObservationScreenshot(string name, Vector3 target, Vector3 position)
        {
            Camera camera = Camera.main;
            if (camera == null) yield break;
            var followers = host.Player.GetComponentsInChildren<NWH.Common.Cameras.CameraMouseDrag>(true)
                .Where(c => c.enabled).ToArray();
            Vector3 originalPosition = camera.transform.position;
            Quaternion originalRotation = camera.transform.rotation;
            try
            {
                foreach (var follower in followers) follower.enabled = false;
                camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(target - position, Vector3.up));
                yield return null;
                yield return null;
                yield return DualScreenshot(name);
            }
            finally
            {
                if (camera != null) camera.transform.SetPositionAndRotation(originalPosition, originalRotation);
                foreach (var follower in followers) if (follower != null) follower.enabled = true;
            }
        }
#endif
    }
}
