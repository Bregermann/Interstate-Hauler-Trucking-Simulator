using System;
using System.Collections;
using System.IO;
using UnityEngine;
using LWS.InterstateHauler;

namespace LWS.TruckTaxi
{
    public sealed partial class TruckTaxiRestartHarness
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private IEnumerator RunVehicleRoster()
        {
            int index=request.RosterIndex;Check(index>=0 && index<6,"roster index in authored range");
            if(request.Mode=="seed")
            {
                Check(FindTestProfile(false)==null,"fresh isolated roster profile");
                menu.NewVibinInWorld(TruckTaxiWorldCatalog.Demo);
                yield return WaitForRun();yield return AcknowledgeFirstDay();
                Result(saves.RenameProfile(host.Persistence.RunProfileId,TruckTaxiRestartTestArguments.ProfileName),"roster profile named");
                var state=host.Session.CapturePersistentState();state.shiftEarningsCents=2000000;
                Check(host.Session.TryRestorePersistentState(state,out var reason),"isolated affordability fixture "+reason);
                host.Session.SetRideRequestsEnabled(false);
                var catalog=host.TruckFactory.Catalog;Check(catalog.trucks.Length==6,"six actual authored Career entries");
                var entry=catalog.trucks[index];
                yield return PrepareDriveSeedEngine();host.SetPaused(false);
                float until=Time.realtimeSinceStartup+20;
                while(!host.TruckFactory.CanSwitch(out reason) && Time.realtimeSinceStartup<until) yield return null;
                Check(host.TruckFactory.CanSwitch(out reason),"safe native grounded switch "+reason);
                if(index>0)
                {
                    long cash=host.Session.WalletBalanceCents;
                    Check(host.Session.TryPurchaseTruck(catalog,entry.StableId,out reason),"authored truck purchase "+reason);
                    Check(host.Session.WalletBalanceCents==cash-entry.priceCents,"purchase charged exact authored price");
                    Check(!host.Session.TryPurchaseTruck(catalog,entry.StableId,out _),"repeat purchase rejected");
                    Check(host.Session.WalletBalanceCents==cash-entry.priceCents,"repeat purchase did not charge");
                }
                Check(host.TruckFactory.TryEquip(entry.StableId,out reason),"canonical model equipped "+reason);
                Check(host.Session.TryApplyGarage(catalog,entry.StableId,new TruckTaxiGaragePaintData{color=RosterPaint},out reason),"authored paint applied "+reason);
                yield return RosterAssertions(index);
                yield return RosterPresentation(index);
                yield return DualNativeDrive("roster "+entry.StableId+" before save");
                Result(host.Persistence.Save(1,true),"roster manual save");
                Result(host.Persistence.SaveBeforeLeaving(),"roster quit autosave");
            }
            else
            {
                FindTestProfile(true);continueGame.Invoke(menu,null);
                yield return WaitForRun(false);
                yield return RosterAssertions(index);
                // No engine/rebind/recovery/menu intervention on the loaded actor.
                yield return DualNativeDrive("roster fresh process "+host.Player.DefinitionId);
            }
            File.WriteAllText(Path.Combine(request.Root,"roster-"+index+"-"+request.Mode+".txt"),
                "PASS real Windows "+host.Player.DefinitionId+" selected, paint retained, native throttle/brake/steering; "+DateTime.UtcNow.ToString("O"));
        }
        private static readonly Color RosterPaint=new Color(.17f,.61f,.83f,1);
        private IEnumerator RosterPresentation(int index)
        {
            var dealer=host.GetComponent<TruckTaxiDealership>();
            Check(dealer!=null && dealer.TryOpen(),"actual dealer modal and real-model preview open");
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(request.Root,"dealer-"+index+".png"));yield return null;
            Check(host.hud.NormalHudSuppressed,"normal HUD suppressed behind dealership");
            dealer.Close();Check(!host.Paused && !dealer.IsOpen,"dealer releases pause/modal");
            var exit=DualExit();var marker=exit.GetComponent<TruckTaxiMapMarker>();
            Check(host.GPS.TrySetServiceDestination(marker.stableId,marker.label,exit.transform.position,TruckTaxiMapMarkerType.Place),"existing regional exit navigation requested");
            float routeDeadline=Time.realtimeSinceStartup+8;
            while(!host.GPS.RouteReady && Time.realtimeSinceStartup<routeDeadline) yield return null;
            var compass=host.Player.GetComponent<LwsCompassNavigatorProAdapter>();
            Check(host.GPS.RouteReady,"actual road-graph route calculated for selected truck");
            host.GPS.RequestPresentationRefresh();yield return new WaitForSecondsRealtime(.6f);
            Check(compass.VendorRoutePresentationActive && compass.AppliedRoutePointCount>=2,"existing Compass route presented");
            var changer=host.Player.GetComponentInChildren<NWH.Common.Cameras.CameraChanger>(true);int prior=changer.currentCameraIndex;
            for(int n=0;n<changer.cameras.Count && !changer.cameras[changer.currentCameraIndex].name.Contains("Driver");n++) changer.NextCamera();
            Check(changer.cameras[changer.currentCameraIndex].name.Contains("Driver"),"existing cockpit camera selected");
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(request.Root,"cockpit-"+index+".png"));yield return null;
            for(int n=0;n<changer.cameras.Count && changer.currentCameraIndex!=prior;n++) changer.NextCamera();
            Check(host.GPS.ClearPlayerServiceDestination(),"test navigation lease released");
            Check(host.Player.GetComponent<LwsCompassNavigatorProAdapter>().VendorRuntimeReady,"existing Compass runtime ready");
            var control=host.Player.TruckControlController;bool lights=control.CurrentState.headlightsOn;
            control.ApplyCommandFrame(new LwsVehicleCommandFrame{lowBeamLights=LwsMomentaryIntent.Pressed,horn=LwsMomentaryIntent.Pressed},default);
            Check(control.CurrentState.headlightsOn!=lights && control.CurrentState.hornActive,"existing headlight and horn semantic commands accepted");
            yield return new WaitForFixedUpdate();yield return null;
            control.ApplyCommandFrame(new LwsVehicleCommandFrame{lowBeamLights=LwsMomentaryIntent.Pressed},default);
            yield return new WaitForFixedUpdate();yield return null;
            var samples=new System.Collections.Generic.List<float>();float until=Time.realtimeSinceStartup+5;
            while(Time.realtimeSinceStartup<until){samples.Add(Time.unscaledDeltaTime*1000);yield return null;}
            samples.Sort();Check(samples.Count>10,"bounded live-scene frame sample collected");
            File.WriteAllText(Path.Combine(request.Root,"performance-"+index+".txt"),
                "Stationary live Demo City; NPCs/weather enabled; shared GPU, NOT isolated or driving-comparison proof.\nGPU="+SystemInfo.graphicsDeviceName+
                "\nSamples="+samples.Count+" medianMs="+samples[samples.Count/2]+" p95Ms="+samples[(int)((samples.Count-1)*.95f)]);
        }
        private IEnumerator RosterAssertions(int index)
        {
            var entry=host.TruckFactory.Catalog.trucks[index];
            Check(host.Player.DefinitionId==entry.StableId && host.Session.EquippedTruckId==entry.StableId,"selected physical model matches Career");
            Check(FindObjectsByType<LwsPlayerTruck>(FindObjectsSortMode.None).Length==1,"one canonical player truck");
            Check(host.Player.GetComponents<LwsNwhVehicleInputProvider>().Length==1,"one native player input provider");
            Check(!host.Player.GetComponent<Rigidbody>().isKinematic,"selected tractor dynamic");
            var owned=host.Session.CaptureCareerProfile().trucks.Find(t=>t.truckId==entry.StableId);
            Check(owned!=null && owned.hasCustomPaint && Vector4.Distance(owned.paint.color,RosterPaint)<.001f,"per-truck paint persisted");
            Check(host.Player.GetComponent<TruckTaxiGaragePaint>().Ready,"paint presentation bound");
            Check(FindFirstObjectByType<TruckTaxiDealershipFacility>()!=null,"regional dealership facility exists");
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(request.Root,"roster-"+index+"-"+request.Mode+".png"));
            yield return null;
        }
#endif
    }
}
