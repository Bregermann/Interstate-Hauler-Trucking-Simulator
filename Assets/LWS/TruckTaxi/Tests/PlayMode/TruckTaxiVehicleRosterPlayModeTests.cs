using System.Collections;
using LWS.InterstateHauler;
using NUnit.Framework;
using NWH.VehiclePhysics2;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiVehicleRosterPlayModeTests
    {
        private bool hadPreference;
        private int previousPreference;
        [SetUp] public void CapturePreference()
        {hadPreference=PlayerPrefs.HasKey(TruckTaxiSession.RideRequestsPreferenceKey);previousPreference=PlayerPrefs.GetInt(TruckTaxiSession.RideRequestsPreferenceKey,1);}
        [TearDown] public void RestorePreference()
        {if(hadPreference) PlayerPrefs.SetInt(TruckTaxiSession.RideRequestsPreferenceKey,previousPreference);else PlayerPrefs.DeleteKey(TruckTaxiSession.RideRequestsPreferenceKey);Time.timeScale=1;}
        [UnityTest]
        public IEnumerator AuthoredChassisRetainActualNwhTrailerJoint()
        {
            yield return SceneManager.LoadSceneAsync("TruckTaxi_DemoCity");
            float deadline=Time.realtimeSinceStartup+90;
            while((TruckTaxiBootstrap.Instance==null || !TruckTaxiBootstrap.Instance.Ready) && Time.realtimeSinceStartup<deadline) yield return null;
            var host=TruckTaxiBootstrap.Instance;Assert.IsNotNull(host);
            yield return TruckTaxiPlayModeSaveIsolation.CompleteNewRunIntroduction(host);
            host.Session.SetRideRequestsEnabled(false);
            var state=host.Session.CapturePersistentState();state.shiftEarningsCents=2000000;
            Assert.IsTrue(host.Session.TryRestorePersistentState(state,out var reason),reason);
            var body=host.Player.GetComponent<Rigidbody>();var catalog=host.TruckFactory.Catalog;
            var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LWS/InterstateHauler/Vehicles/Prefabs/IH_TestTrailer_DryVan.prefab");
            Assert.IsNotNull(prefab);
            foreach(var entry in catalog.trucks)
            {
                if(entry.priceCents>0) Assert.IsTrue(host.Session.TryPurchaseTruck(catalog,entry.StableId,out reason),reason);
                deadline=Time.realtimeSinceStartup+20;
                while(!host.TruckFactory.CanSwitch(out reason) && Time.realtimeSinceStartup<deadline)
                {body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;yield return new WaitForFixedUpdate();}
                Assert.IsTrue(host.TruckFactory.TryEquip(entry.StableId,out reason),entry.StableId+": "+reason);
                // Let PhysX commit the factory's kinematic/configuration boundary
                // before creating a joint against the reconfigured dynamic actor.
                yield return new WaitForFixedUpdate();
                yield return null;
                var trailer=Object.Instantiate(prefab,body.position-host.Player.transform.forward*10,body.rotation);
                // This physics fixture has no baked lighting scene. Empty source probes
                // contribute no reflection and can leave stale URP culling entries on teardown.
                foreach(var probe in trailer.GetComponentsInChildren<ReflectionProbe>(true))
                    if(probe.mode==UnityEngine.Rendering.ReflectionProbeMode.Baked && probe.texture==null) probe.enabled=false;
                try
                {
                    var vc=trailer.GetComponent<VehicleController>();
                    var hitch=host.Player.GetComponentInChildren<NWH.VehiclePhysics2.Modules.Trailer.TrailerHitchModuleWrapper>();
                    var module=trailer.GetComponentInChildren<NWH.VehiclePhysics2.Modules.Trailer.TrailerModuleWrapper>();
                    Assert.IsNotNull(hitch);Assert.IsNotNull(module);
                    // A real pickup approaches the kingpin. Do not ask NWH to snap
                    // an unsynchronized body ten metres while creating its joint.
                    trailer.transform.position+=hitch.module.attachmentPoint.position-module.module.attachmentPoint.position;
                    Physics.SyncTransforms();
                    hitch.module.AttachTrailer(module);
                    deadline=Time.realtimeSinceStartup+10;
                    while(!vc.IsInitialized && Time.realtimeSinceStartup<deadline) yield return null;
                    Assert.IsTrue(vc.IsInitialized,"NWH initializes the parked trailer when the hitch activates it.");
                    yield return new WaitForFixedUpdate();
                    Assert.IsTrue(host.Player.CouplingAdapter.IsTrailerAttached,entry.StableId+" attached="+hitch.module.attached+" rootJoints="+host.Player.GetComponents<ConfigurableJoint>().Length+" input="+host.Player.GetComponent<VehicleController>().input.TrailerAttachDetach);
                    Assert.AreSame(trailer.GetComponent<Rigidbody>(),host.Player.GetComponent<ConfigurableJoint>().connectedBody);
                    Assert.IsFalse(host.TruckFactory.CanSwitch(out _),"Attached trailer must block switching.");
                    yield return new WaitForSeconds(3);
                    Assert.IsTrue(host.Player.CouplingAdapter.IsTrailerAttached,entry.StableId+" joint broke");
                    Assert.Less(body.linearVelocity.magnitude,5,entry.StableId+" tractor launched");
                    Assert.Less(vc.vehicleRigidbody.linearVelocity.magnitude,5,entry.StableId+" trailer launched");
                    Debug.Log("ROSTER NWH JOINT STABLE: "+entry.StableId);
                    hitch.module.DetachTrailer(host.Player.GetComponent<VehicleController>());
                    yield return new WaitForFixedUpdate();
                    Assert.IsFalse(host.Player.CouplingAdapter.IsTrailerAttached);
                }
                finally {trailer.SetActive(false);Object.Destroy(trailer);}
                // Unregister source render/probe state before the next fixture is created.
                yield return null;
            }
        }
        [UnityTest]
        public IEnumerator DealershipAndSleepersUseExistingUiClockAndDriver()
        {
            yield return SceneManager.LoadSceneAsync("TruckTaxi_DemoCity");
            float deadline=Time.realtimeSinceStartup+90;
            while((TruckTaxiBootstrap.Instance==null || !TruckTaxiBootstrap.Instance.Ready) && Time.realtimeSinceStartup<deadline) yield return null;
            var host=TruckTaxiBootstrap.Instance;Assert.IsNotNull(host);
            yield return TruckTaxiPlayModeSaveIsolation.CompleteNewRunIntroduction(host);
            host.Session.SetRideRequestsEnabled(false);
            var state=host.Session.CapturePersistentState();state.shiftEarningsCents=2000000;
            Assert.IsTrue(host.Session.TryRestorePersistentState(state,out var reason),reason);
            var body=host.Player.GetComponent<Rigidbody>();var catalog=host.TruckFactory.Catalog;
            foreach(var entry in catalog.trucks)
            {
                var model=entry.truck.PlayerPrefab.GetComponent<TruckTaxiTruckModel>();
                if(!model.hasSleeper) continue;
                Assert.IsTrue(host.Session.TryPurchaseTruck(catalog,entry.StableId,out reason),reason);
                deadline=Time.realtimeSinceStartup+20;
                while(!host.TruckFactory.CanSwitch(out reason) && Time.realtimeSinceStartup<deadline)
                {body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;yield return new WaitForFixedUpdate();}
                Assert.IsTrue(host.TruckFactory.TryEquip(entry.StableId,out reason),reason);
                yield return new WaitForSeconds(.6f);
                var dealer=host.GetComponent<TruckTaxiDealership>();
                Assert.IsTrue(dealer.TryOpen(),"Actual dealership modal must open when safely parked.");
                yield return null;
                foreach(var button in dealer.FocusRoot.GetComponentsInChildren<UnityEngine.UI.Button>())
                    Assert.Greater(((RectTransform)button.transform).rect.height,1,"Collapsed dealer control "+button.name);
                TruckTaxiDemoPlayModeTests.Capture("Dealer_"+entry.StableId,1920,1080);
                dealer.Close();Assert.IsFalse(host.Paused);
                var control=host.Player.TruckControlController;
                if(!control.CurrentState.parkingBrakeOn) control.ApplyCommandFrame(new LwsVehicleCommandFrame{parkingBrakeToggle=LwsMomentaryIntent.Pressed},default);
                var needs=host.DriverNeeds.CapturePersistentState();needs.Fatigue=.8f;
                Assert.IsTrue(host.DriverNeeds.RestorePersistentState(needs,out reason),reason);
                Assert.IsTrue(host.WorldLife.TryExitTruck(),host.WorldLife.ExitBlockReason);
                yield return null;
                Vector3 eye=host.WorldLife.FootCamera.transform.localPosition;
                host.WorldLife.OpenCabSleep();
                Assert.AreEqual(TruckTaxiPlayerControlMode.SeatedInteraction,host.WorldLife.Mode);
                Assert.IsFalse(host.TruckFactory.CanSwitch(out _),"Sleeper interaction blocks vehicle switching.");
                TruckTaxiDemoPlayModeTests.Capture("Sleeper_"+entry.StableId,1920,1080,host.WorldLife.FootCamera);
                double time=host.Environment.Clock.CurrentSnapshot.totalGameSeconds;
                Assert.IsTrue(host.DriverNeeds.TryTruckSleep(2),host.DriverNeeds.Feedback);
                Assert.GreaterOrEqual(host.Environment.Clock.CurrentSnapshot.totalGameSeconds-time,7200);
                Assert.Less(host.DriverNeeds.CapturePersistentState().Fatigue,.8f);
                host.WorldLife.CloseTemporaryInteraction();
                Assert.That(Vector3.Distance(eye,host.WorldLife.FootCamera.transform.localPosition),Is.LessThan(.001f));
                Assert.IsTrue(host.WorldLife.IsOnFoot);
                host.WorldLife.ReturnToTruck();
                Assert.AreEqual(TruckTaxiPlayerControlMode.Driving,host.WorldLife.Mode);
                Assert.IsFalse(host.Player.GetComponent<LwsKeyboardGamepadTruckInputSource>().DrivingInputSuppressed);
                yield return null;
            }
        }
        [UnityTest]
        public IEnumerator SixAuthoredTrucksEquipOnOneCanonicalRigAndRetainCareerPaint()
        {
            yield return SceneManager.LoadSceneAsync("TruckTaxi_DemoCity");
            float deadline=Time.realtimeSinceStartup+90;
            while((TruckTaxiBootstrap.Instance==null || !TruckTaxiBootstrap.Instance.Ready) && Time.realtimeSinceStartup<deadline) yield return null;
            var host=TruckTaxiBootstrap.Instance;Assert.IsNotNull(host);Assert.IsTrue(host.Ready);
            yield return TruckTaxiPlayModeSaveIsolation.CompleteNewRunIntroduction(host);
            host.Session.SetRideRequestsEnabled(false);
            var state=host.Session.CapturePersistentState();state.shiftEarningsCents=2000000;
            Assert.IsTrue(host.Session.TryRestorePersistentState(state,out var reason),reason);
            var actor=host.Player;var body=actor.GetComponent<Rigidbody>();
            var starterScreen=host.GPS.DashboardScreen;
            var starterParent=starterScreen.parent;
            var starterPosition=starterScreen.localPosition;var starterRotation=starterScreen.localRotation;var starterScale=starterScreen.localScale;
            var catalog=host.TruckFactory.Catalog;Assert.AreEqual(6,catalog.trucks.Length);
            foreach(var entry in catalog.trucks)
            {
                Assert.IsTrue(TruckTaxiTruckModel.ValidatePlayerPrefab(entry.truck,out reason),reason);
                if(entry.priceCents>0) Assert.IsTrue(host.Session.TryPurchaseTruck(catalog,entry.StableId,out reason),reason);
                host.SetPaused(false);
                deadline=Time.realtimeSinceStartup+20;
                while(!host.TruckFactory.CanSwitch(out reason) && Time.realtimeSinceStartup<deadline)
                {body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;yield return new WaitForFixedUpdate();}
                Assert.IsTrue(host.TruckFactory.TryEquip(entry.StableId,out reason),entry.StableId+": "+reason);
                yield return new WaitForFixedUpdate();
                Assert.AreSame(actor,host.Player);Assert.IsFalse(body.isKinematic);
                Assert.AreEqual(entry.StableId,host.Player.DefinitionId);Assert.AreEqual(entry.StableId,host.Session.EquippedTruckId);
                Assert.AreEqual(1,Object.FindObjectsByType<LwsPlayerTruck>(FindObjectsSortMode.None).Length);
                Assert.AreEqual(1,actor.GetComponents<LwsNwhVehicleInputProvider>().Length);
                Assert.IsTrue(actor.GetComponent<VehicleController>().IsInitialized);
                var paint=new TruckTaxiGaragePaintData{color=new Color(.2f,.6f,.8f)};
                Assert.IsTrue(host.Session.TryApplyGarage(catalog,entry.StableId,paint,out reason),reason);
                Assert.IsNotEmpty(host.TruckFactory.CurrentPaintMaterials);
                TruckTaxiDemoPlayModeTests.Capture("Roster_"+entry.StableId,1600,900);
                var changer=host.Player.GetComponentInChildren<NWH.Common.Cameras.CameraChanger>(true);
                int prior=changer.currentCameraIndex;
                for(int n=0;n<changer.cameras.Count && !changer.cameras[changer.currentCameraIndex].name.Contains("Driver");n++) changer.NextCamera();
                yield return null;
                TruckTaxiDemoPlayModeTests.Capture("Cockpit_"+entry.StableId,1920,1080);
                var model=entry.truck.PlayerPrefab.GetComponent<TruckTaxiTruckModel>();
                if(!model.retainsOriginalBody)
                {
                    Assert.Less(Vector3.Distance(Camera.main.transform.position,host.Player.transform.TransformPoint(model.driverEye)),.2f,"Native cached camera escaped the authored eye point.");
                    var gps=host.Player.GetComponent<LwsCabGpsController>();Assert.IsNotNull(gps.GpsMount);
                    Assert.AreSame(gps.GpsMount,host.GPS.DashboardScreen.parent,"Actual Compass screen must follow the authored mount, not the legacy interior fit.");
                    Assert.Greater(Vector3.Dot(host.GPS.DashboardScreen.up,host.Player.transform.up),.99f,"New-cab map must stay level rather than inheriting the starter mount tilt.");
                    Vector3 point=Camera.main.WorldToViewportPoint(host.GPS.DashboardScreen.position);
                    Assert.Greater(point.z,0);Assert.That(point.x,Is.InRange(.05f,.95f));Assert.That(point.y,Is.InRange(.05f,.95f));
                }
                for(int n=0;n<changer.cameras.Count && changer.currentCameraIndex!=prior;n++) changer.NextCamera();
            }
            Assert.IsNotNull(Object.FindFirstObjectByType<TruckTaxiDealershipFacility>());
            Assert.IsTrue(host.Persistence.Save(1,true).Succeeded);
            host.GPS.SetVehicleDashboardMount(null);
            Assert.AreSame(starterParent,starterScreen.parent);
            Assert.Less(Vector3.Distance(starterPosition,starterScreen.localPosition),.00001f);
            Assert.Less(Quaternion.Angle(starterRotation,starterScreen.localRotation),.001f);
            Assert.AreEqual(starterScale,starterScreen.localScale);
            host.GPS.SetVehicleDashboardMount(host.Player.GetComponent<LwsCabGpsController>().GpsMount);
            string selected=host.Player.DefinitionId;
            long wallet=host.Session.WalletBalanceCents;
            Assert.IsTrue(TruckTaxiWorldTravel.TryTravel(host,TruckTaxiWorldCatalog.Edenborough,false),TruckTaxiWorldTravel.LastStatus);
            deadline=Time.realtimeSinceStartup+240;
            while(TruckTaxiWorldTravel.IsBusy && Time.realtimeSinceStartup<deadline) yield return null;
            Assert.IsFalse(TruckTaxiWorldTravel.IsBusy,TruckTaxiWorldTravel.LastStatus);
            host=TruckTaxiBootstrap.Instance;Assert.IsNotNull(host);
            Assert.AreEqual(TruckTaxiWorldCatalog.Edenborough,host.gameObject.scene.name);
            Assert.AreEqual(selected,host.Player.DefinitionId);Assert.AreEqual(selected,host.Session.EquippedTruckId);
            Assert.AreEqual(wallet,host.Session.WalletBalanceCents);
            Assert.AreEqual(6,host.Session.CaptureCareerProfile().trucks.Count);
            Assert.IsFalse(host.Player.GetComponent<Rigidbody>().isKinematic);
            Assert.AreEqual(1,Object.FindObjectsByType<LwsPlayerTruck>(FindObjectsSortMode.None).Length);
            Assert.IsNotNull(Object.FindFirstObjectByType<TruckTaxiDealershipFacility>());
            Assert.IsTrue(host.Player.GetComponent<TruckTaxiGaragePaint>().Ready);
            Assert.AreEqual(selected,host.TruckFactory.Catalog.FindTruck(selected).StableId);
        }
    }
}
