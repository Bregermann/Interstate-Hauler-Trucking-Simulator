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
                yield return DualNativeDrive("Demo City initial drive");
                Result(host.Persistence.Save(1,true),"dual-world Demo City manual checkpoint");
                yield return DualSnowViews("DemoCity");
                yield return DualTransfer(true);
                yield return DualNativeDrive("Edenborough physical arrival");
                yield return DualSnowViews("Edenborough");
                yield return DualTransfer(true);
                yield return DualNativeDrive("Demo City physical return");
                // Ten transfers in the same real player process; first two use native trigger contacts.
                for (int i = 2; i < 10; i++)
                {
                    host.hud.WorldTravel.Open();
                    Check(host.hud.WorldTravel.IsOpen,"actual pause travel panel opens");
                    host.hud.WorldTravel.Close(); host.SetPaused(false);
                    Check(!host.hud.WorldTravel.IsOpen && !TruckTaxiWorldTravel.IsBusy,"cancel does not change world");
                    yield return DualTransfer(false);
                    yield return DualNativeDrive("menu arrival " + i);
                }
                Check(TruckTaxiWorldTravel.CompletedTransfers == 10,"ten successful transfers in one process");
                // Leave the restart checkpoint in Edenborough; next process returns to Demo City.
                yield return DualTransfer(false);
                Result(host.Persistence.Save(2,true),"dual-world Edenborough manual checkpoint");
                Result(host.Persistence.SaveBeforeLeaving(),"dual-world Edenborough quit checkpoint");
            }
            else
            {
                dualProfile = FindTestProfile(true).stableProfileId;
                continueGame.Invoke(menu,null); yield return WaitForRun(false);
                string expected = request.Mode == "continue" ? TruckTaxiWorldCatalog.Edenborough : TruckTaxiWorldCatalog.Demo;
                Check(host.gameObject.scene.name == expected,"fresh process Continue restored saved world "+expected);
                DualCareerAssertions();
                yield return DualNativeDrive("fresh process " + expected);
                if (request.Mode == "continue")
                {
                    yield return DualTransfer(false);
                    Result(host.Persistence.SaveBeforeLeaving(),"dual-world Demo City quit checkpoint");
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
            host.Session.DiscardTeleportDistance(); Physics.SyncTransforms(); yield return new WaitForFixedUpdate();
            Check(host.GPS.TrySetServiceDestination(exit.GetComponent<TruckTaxiMapMarker>().stableId,
                exit.GetComponent<TruckTaxiMapMarker>().label,exit.transform.position,TruckTaxiMapMarkerType.Place),"local exit accepted by existing GPS");
            float until=Time.realtimeSinceStartup+8;
            while (!host.GPS.RouteReady && Time.realtimeSinceStartup<until) yield return null;
            Check(host.GPS.RouteReady,"local road-graph route to regional exit calculated");
            yield return DualScreenshot(host.gameObject.scene.name+"-exit");
        }

        private IEnumerator DualTransfer(bool physical)
        {
            string source = host.gameObject.scene.name, destination = TruckTaxiWorldCatalog.Other(source);
            double time = host.Environment.Clock.CurrentSnapshot.totalGameSeconds;
            long wallet = host.Session.WalletBalanceCents;
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
                }
                finally { InputSystem.RemoveDevice(keyboard); if(previous!=null && previous.added)previous.MakeCurrent(); }
            }
            else
            {
                host.SetPaused(true); host.hud.WorldTravel.Open();
                var button = host.hud.WorldTravel.FocusRoot.GetComponentsInChildren<Button>().First(b=>b.GetComponentInChildren<TMPro.TextMeshProUGUI>()?.text=="TRAVEL");
                button.onClick.Invoke();
                Check(TruckTaxiWorldTravel.IsBusy,"actual travel panel confirm callback started transaction");
            }
            yield return null; yield return DualScreenshot(source+"-loading");
            float deadline = Time.realtimeSinceStartup+180;
            while (TruckTaxiWorldTravel.IsBusy && Time.realtimeSinceStartup<deadline) yield return null;
            Check(!TruckTaxiWorldTravel.IsBusy,"world transfer completed before timeout: "+TruckTaxiWorldTravel.LastStatus);
            yield return new WaitForEndOfFrame(); yield return null;
            host=TruckTaxiBootstrap.Instance; saves=host.Persistence.SaveService;
            Check(host.gameObject.scene.name==destination,"arrived in intended world "+destination+" | "+TruckTaxiWorldTravel.LastStatus);
            Check(host.Session.WalletBalanceCents==wallet,"wallet preserved through transaction");
            Check(host.Environment.Clock.CurrentSnapshot.totalGameSeconds>=time,"global calendar never moved backward");
            DualCareerAssertions();
            Check(FindObjectsByType<TruckTaxiBootstrap>(FindObjectsSortMode.None).Length==1 &&
                FindObjectsByType<LwsPlayerTruck>(FindObjectsSortMode.None).Length==1 &&
                FindObjectsByType<TruckTaxiHud>(FindObjectsSortMode.None).Length==1,"one host, truck and HUD after transfer");
            Check(FindObjectsByType<TruckTaxiWorldTravel>(FindObjectsSortMode.None).Length==0,"loading transaction retired");
            Check(Vector3.Distance(host.Player.transform.position,DualExit().transform.position)>25,"arrival does not overlap outbound trigger");
            dualEvidence.Add("TRANSFER "+TruckTaxiWorldTravel.CompletedTransfers+" "+source+" -> "+destination+
                " objects="+FindObjectsByType<Transform>(FindObjectsSortMode.None).Length+
                " managed="+GC.GetTotalMemory(false)+" allocated="+UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()+
                " traffic="+host.traffic.LogicalCount+" peds="+host.pedestrians.ActiveCount);
            yield return DualScreenshot(destination+"-arrival-"+TruckTaxiWorldTravel.CompletedTransfers);
        }

        private void DualCareerAssertions()
        {
            Check(host.Persistence.RunProfileId==dualProfile && saves.ActiveProfileId==dualProfile,"same career identity across worlds/restart");
            var career=host.Session.CaptureCareerProfile();
            Check(career.driverName=="Dual World Driver","driver name retained");
            var owned=career.trucks.Find(t=>t.truckId==host.Player.DefinitionId);
            Check(owned!=null && owned.hasCustomPaint && Vector4.Distance(owned.paint.color,EdenboroughTestPaint)<.001f,"paint and truck ownership retained");
            Check(host.DriverNeeds.State.Count(TruckTaxiNeedsItem.Snack)==9,"inventory retained without starter grants");
            Check(host.DriverNeeds.State.AmbientChoiceIds.Contains("astra.dualworld."+request.RunId),"needs/progression marker retained");
            Check(host.Session.CapturePersistentState().statistics.completedRides==2,"completed fares retained");
        }

        private IEnumerator DualNativeDrive(string label)
        {
            host.SetPaused(false);
            var registry=LwsApplicationBootstrap.Instance.Registry; registry.TryGet(out ILwsVehicleInputService input); CaptureDriveInput(input);
            var keyboard=InputSystem.AddDevice<Keyboard>("Dual-world post-arrival keyboard"); var previous=Keyboard.current; driveKeyboard=keyboard; keyboard.MakeCurrent();
            try
            {
                RequireDriveReady(); var body=host.Player.GetComponent<Rigidbody>(); Vector3 start=body.position;
                if(host.Player.TruckControlController.CurrentState.parkingBrakeOn) yield return DrivePulse("ParkingBrake",Key.P);
                float until=Time.realtimeSinceStartup+18;
                while(Vector3.Distance(start,body.position)<3 && Time.realtimeSinceStartup<until) yield return DriveKeys(1,Key.W);
                Check(Vector3.Distance(start,body.position)>=3 && host.Player.NwhAdapter.VehicleController.input.Throttle>.5f,label+" native throttle/forward motion without recovery");
                yield return DriveKeys(2,Key.W,Key.S);
                Check(host.Player.NwhAdapter.VehicleController.input.Brakes>.5f,label+" native brake input");
                until=Time.realtimeSinceStartup+8;
                while(body.linearVelocity.magnitude>.2f && Time.realtimeSinceStartup<until)yield return DriveKeys(1,Key.W,Key.S);
                yield return DriveKeys(3,Key.A); Check(host.Player.NwhAdapter.VehicleController.input.Steering<-.5f,label+" native steering");
                yield return DriveKeys(2);
                if(!host.Player.TruckControlController.CurrentState.parkingBrakeOn)yield return DrivePulse("ParkingBrake",Key.P);
                dualEvidence.Add("DRIVE "+label+" meters="+Vector3.Distance(start,body.position));
            }
            finally { InputSystem.RemoveDevice(keyboard); driveKeyboard=null; if(previous!=null&&previous.added)previous.MakeCurrent(); }
        }

        private IEnumerator DualSnowViews(string label)
        {
            host.SetPaused(true);
            foreach(float depth in new[]{0f,.2f,.85f})
            {
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
                ms.Sort(); dualEvidence.Add("PERF "+label+" snow="+depth+" median="+ms[ms.Count/2]+" p95="+ms[(int)((ms.Count-1)*.95f)]+" p99="+ms[(int)((ms.Count-1)*.99f)]+
                    " materials="+surface.SharedEnvironmentMaterialCount+" surfaces="+surface.GroundSnowSurfaceCount+" terrain="+surface.TerrainSnowSurfaceCount+
                    " screen="+Screen.width+"x"+Screen.height+" GPU="+SystemInfo.graphicsDeviceName+" paused stationary, not driving/human acceptance");
                File.WriteAllLines(Path.Combine(request.Root,"dual-world-"+request.Mode+".txt"),dualEvidence);
            }
            host.SetPaused(false);
        }
        private IEnumerator DualScreenshot(string name)
        {
            yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(request.Root,"dual-"+name+".png"));
            yield return null;
        }
#endif
    }
}
