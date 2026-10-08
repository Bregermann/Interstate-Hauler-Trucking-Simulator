using System;
using System.Linq;
using System.Reflection;
using LWS.InterstateHauler;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiCompassRecoveryProbe : MonoBehaviour
    {
        public bool showMiniMap { get; set; }=true;
        public bool hasRoute { get; set; }=true;
        public bool showRoute { get; set; }=true;
        public Camera miniMapCamera;
        public float miniMapDragMaxDistance { get; set; }=1000;
        public float miniMapCaptureSize { get; set; }
        private Vector3 offset;
        public Vector3 miniMapFollowOffset { get => offset; set => offset=Vector3.ClampMagnitude(value,miniMapDragMaxDistance); }
        public int Refreshes,RequestedFrames;
        public void UpdateMiniMapContents(int frames) { Refreshes++; RequestedFrames=frames; }
    }

    public sealed class TruckTaxiGpsRecoveryTests
    {
        private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        private sealed class Presenter : ILwsNavigationRoutePresenter
        {
            private readonly TruckTaxiCompassRecoveryProbe compass;
            public int Presents;
            public string PresenterId => "taxi.test.recovery";
            public Presenter(TruckTaxiCompassRecoveryProbe value) { compass=value; }
            public bool PresentRoute(LwsRouteResult route) { Presents++; compass.hasRoute=true; return true; }
            public void ClearRoute() { compass.hasRoute=false; }
        }
        private static void Set(object target,string field,object value) => target.GetType().GetField(field,Private).SetValue(target,value);
        private static object Call(object target,string method,params object[] args) => target.GetType().GetMethod(method,Private).Invoke(target,args);
        private static void Bind(TruckTaxiGPSAdapter gps,TruckTaxiCompassRecoveryProbe compass)
        {
            var type=typeof(TruckTaxiGPSAdapter).GetNestedType("CompassPresentation",BindingFlags.NonPublic);
            Set(gps,"hudPresentation",Activator.CreateInstance(type,new object[] { compass }));
        }
        private static LwsNavigationService Navigation(TruckTaxiGPSAdapter gps,Presenter presenter)
        {
            var navigation=new LwsNavigationService();
            Set(navigation,"<CurrentRoute>k__BackingField",new LwsRouteResult {
                succeeded=true,destinationId="ride",waypoints={Vector3.zero,Vector3.forward*100}
            });
            navigation.SetPresenter(presenter); Set(gps,"navigation",navigation);
            return navigation;
        }

        [Test]
        public void IntercityOfferIncludesActualStopsAndTemporarilyExpandsVendorDragLimit()
        {
            var root=new GameObject("Intercity framing");
            var configuration=ScriptableObject.CreateInstance<TruckTaxiConfiguration>();
            try
            {
                var pickup=new GameObject("Pickup").AddComponent<TruckTaxiRideLocation>();
                var destination=new GameObject("Destination").AddComponent<TruckTaxiRideLocation>();
                pickup.transform.SetParent(root.transform); destination.transform.SetParent(root.transform);
                pickup.transform.position=new Vector3(100,0,0); destination.transform.position=new Vector3(6000,0,100);
                var gps=root.AddComponent<TruckTaxiGPSAdapter>();
                var compass=root.AddComponent<TruckTaxiCompassRecoveryProbe>();
                Set(gps,"player",root.transform); Set(gps,"previewCompass",compass);
                Set(gps,"followOffsetProperty",typeof(TruckTaxiCompassRecoveryProbe).GetProperty("miniMapFollowOffset"));
                var leg=new TruckTaxiRouteLeg(200,"fixture",new[]{Vector3.zero,Vector3.right*200},true);
                gps.FrameOffer(new TruckTaxiRideOffer(null,pickup,destination,Vector3.zero,leg,leg,configuration));
                Assert.That(compass.miniMapFollowOffset.x,Is.EqualTo(3000).Within(.01f));
                Assert.That(compass.miniMapFollowOffset.z,Is.EqualTo(50).Within(.01f));
                Assert.GreaterOrEqual(compass.miniMapCaptureSize,6000*1.3f);
                gps.FrameOffer(null);
                Assert.AreEqual(1000,compass.miniMapDragMaxDistance);
                Assert.AreEqual(Vector3.zero,compass.miniMapFollowOffset);
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(configuration); }
        }

        [Test]
        public void PausedPresentationRefreshIsOneShotAndCoalescesRequests()
        {
            var root=new GameObject("GPS recovery"); float timeScale=Time.timeScale;
            try
            {
                Time.timeScale=0;
                var gps=root.AddComponent<TruckTaxiGPSAdapter>();
                var compass=root.AddComponent<TruckTaxiCompassRecoveryProbe>(); Bind(gps,compass);
                var presenter=new Presenter(compass); Navigation(gps,presenter);
                gps.RequestPresentationRefresh(); gps.RequestPresentationRefresh();
                Call(gps,"RefreshPresentationIfNeeded");
                int presents=presenter.Presents;
                Assert.AreEqual(1,compass.Refreshes); Assert.AreEqual(1,compass.RequestedFrames);
                Assert.IsTrue(compass.showRoute);
                Call(gps,"RefreshPresentationIfNeeded");
                Assert.AreEqual(1,compass.Refreshes,"Ordinary frames must not force camera captures.");
                Assert.AreEqual(presents,presenter.Presents);
                Time.timeScale=timeScale==0 ? 1 : timeScale;
                Call(gps,"RefreshPresentationIfNeeded");
                Assert.AreEqual(2,compass.Refreshes,"Resume requires one refresh.");
            }
            finally { Time.timeScale=timeScale; Object.DestroyImmediate(root); }
        }

        [Test]
        public void HiddenThenVisibleAndLostVendorRouteReuseTheRegisteredPresenter()
        {
            var root=new GameObject("GPS visibility");
            try
            {
                var gps=root.AddComponent<TruckTaxiGPSAdapter>();
                var compass=root.AddComponent<TruckTaxiCompassRecoveryProbe>(); Bind(gps,compass);
                var presenter=new Presenter(compass); var navigation=Navigation(gps,presenter);
                var route=navigation.CurrentRoute;
                Call(gps,"RefreshPresentationIfNeeded");
                compass.showMiniMap=false; Call(gps,"RefreshPresentationIfNeeded");
                compass.showMiniMap=true; Call(gps,"RefreshPresentationIfNeeded");
                Assert.AreEqual(2,compass.Refreshes);
                compass.hasRoute=false; Set(gps,"nextPresentationCheck",float.NegativeInfinity);
                Call(gps,"RefreshPresentationIfNeeded");
                Assert.AreEqual(3,compass.Refreshes); Assert.IsTrue(compass.hasRoute);
                Assert.AreSame(route,navigation.CurrentRoute,"Recovery must not compute or own another route.");
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void PresentationRecoveryDoesNotResumeStoppedNavigation()
        {
            var root=new GameObject("Stopped GPS");
            try
            {
                var gps=root.AddComponent<TruckTaxiGPSAdapter>();
                var compass=root.AddComponent<TruckTaxiCompassRecoveryProbe>(); Bind(gps,compass);
                var presenter=new Presenter(compass); Navigation(gps,presenter);
                Set(gps,"guidanceSuppressed",true);
                int before=presenter.Presents;
                gps.RequestPresentationRefresh(); Call(gps,"RefreshPresentationIfNeeded");
                Assert.AreEqual(before,presenter.Presents); Assert.IsTrue(gps.GuidanceSuppressed);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void RestoreActiveUnsuppressedIntentRepairsPresentationWithoutReplacingLease()
        {
            var root=new GameObject("Intent recovery");
            try
            {
                var gps=root.AddComponent<TruckTaxiGPSAdapter>(); Set(gps,"player",root.transform);
                var navigation=new LwsNavigationService(); Set(gps,"navigation",navigation);
                Assert.IsTrue(gps.TrySetNavigationIntent(TruckTaxiNavigationLayer.PlayerService,"player-service",
                    "arrival","Arrival",Vector3.zero,TruckTaxiMapMarkerType.Place));
                long token=gps.ActiveLeaseToken;
                Set(gps,"presentationRefreshPending",false);
                Assert.IsTrue(gps.RestoreServiceRoute(),"Not suppressed does not mean the presentation is current.");
                Assert.AreEqual(token,gps.ActiveLeaseToken); Assert.IsFalse(gps.GuidanceSuppressed);
                Assert.IsTrue((bool)typeof(TruckTaxiGPSAdapter).GetField("presentationRefreshPending",Private).GetValue(gps));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void ExplicitServiceLocationLinkProducesOnePlaceAndPreservesFareSelection()
        {
            var root=new GameObject("Map ownership"); var hotel=new GameObject("Hotel");
            try
            {
                var location=hotel.AddComponent<TruckTaxiRideLocation>(); location.locationId="taxi.stop.08";
                var service=hotel.AddComponent<TruckTaxiServicePoint>(); service.stableId="TT_SLEEP_taxi.stop.08";
                service.displayName="The Long Stay Hotel"; service.capabilities=TruckTaxiServiceCapability.Sleep; service.location=location;
                Call(service,"OnEnable");
                var markers=root.AddComponent<TruckTaxiMapMarkers>();
                for(int i=0;i<4;i++)
                {
                    markers.RegisterRegionalPoint(location.locationId,service.displayName,"Hotel",Vector3.zero,TruckTaxiMapMarkerType.Place,true);
                    markers.RegisterRegionalPoint(service.stableId,service.displayName,"Sleep",Vector3.zero,TruckTaxiMapMarkerType.Sleep,true);
                    Assert.AreEqual(1,markers.RegionalPoints.Count);
                    Assert.AreEqual(service.stableId,markers.RegionalPoints.Single().Id);
                }
                // Same name and position alone are NOT identity evidence.
                markers.RegisterRegionalPoint("unrelated",service.displayName,"Hotel",Vector3.zero,TruckTaxiMapMarkerType.Place,true);
                Assert.AreEqual(2,markers.RegionalPoints.Count);
                var gps=root.AddComponent<TruckTaxiGPSAdapter>(); Set(gps,"<MapMarkers>k__BackingField",markers);
                Set(gps,"player",root.transform); Set(gps,"navigation",new LwsNavigationService());
                gps.SetRideDestination(location);
                Set(markers,"gps",gps);
                var rideMarker=hotel.AddComponent<TruckTaxiMapMarker>(); rideMarker.stableId=location.locationId;
                rideMarker.source=location.transform; rideMarker.state=TruckTaxiMapMarkerState.Active; markers.Register(rideMarker);
                var serviceMarker=root.AddComponent<TruckTaxiMapMarker>(); serviceMarker.stableId=service.stableId;
                serviceMarker.service=service; serviceMarker.serviceBound=true; serviceMarker.state=TruckTaxiMapMarkerState.Known;
                markers.Register(serviceMarker);
                Assert.IsTrue((bool)Call(markers,"IsPrimaryPlaceMarker",rideMarker));
                Assert.IsFalse((bool)Call(markers,"IsPrimaryPlaceMarker",serviceMarker),"The active fare must not acquire a second service icon.");
                var map=root.AddComponent<TruckTaxiFullMapPresenter>(); Set(map,"gps",gps);
                Call(map,"RebuildPlaces"); map.RestoreActiveSelection();
                Assert.AreEqual(service.stableId,map.FocusedPlaceId); Assert.AreEqual(location.locationId,gps.TargetId);
                Call(service,"OnDisable"); Object.DestroyImmediate(hotel); hotel=null;
                Assert.AreEqual(2,markers.RegionalPoints.Count,"Unloading must retain the metadata alias, not duplicate the place.");
            }
            finally { if(hotel!=null) Object.DestroyImmediate(hotel); Object.DestroyImmediate(root); }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void StreamedSourceCleanupRemovesOrphansOrRestoresMetadata(bool persistent)
        {
            var root=new GameObject("Stream markers"); var source=new GameObject("Stream source");
            try
            {
                var markers=root.AddComponent<TruckTaxiMapMarkers>();
                Set(markers,"markerRoot",root.transform);
                if(persistent) markers.RegisterRegionalPoint("stream","Stream","Place",Vector3.right*25,TruckTaxiMapMarkerType.Place,true);
                var marker=(TruckTaxiMapMarker)Call(markers,"Ensure","stream","Stream",TruckTaxiMapMarkerType.Place,source.transform,Vector3.zero);
                Object.DestroyImmediate(source); source=null;
                Call(markers,"PruneStreamedMarkers");
                Assert.AreEqual(persistent ? 1 : 0,markers.Markers.Count);
                if(persistent)
                {
                    Assert.IsNull(marker.source); Assert.AreEqual(Vector3.right*25,marker.Position);
                    var replacement=new GameObject("Replacement"); replacement.transform.SetParent(root.transform);
                    Assert.AreSame(marker,Call(markers,"Ensure","stream","Stream",TruckTaxiMapMarkerType.Place,replacement.transform,Vector3.one));
                    Assert.AreEqual(1,markers.Markers.Count);
                }
            }
            finally
            {
                Set(root.GetComponent<TruckTaxiMapMarkers>(),"markerRoot",null);
                if(source!=null) Object.DestroyImmediate(source); Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RegionalCaptureIsExclusiveRestoresCameraAndSurvivesRepeatedSetup()
        {
            var root=new GameObject("Tile capture"); var cameraObject=new GameObject("Compass camera");
            var catalog=Resources.Load<TruckTaxiMapTileCatalog>(TruckTaxiMapTileCatalog.ResourcePath);
            try
            {
                var layer=root.AddComponent<TruckTaxiMapTileLayer>(); Assert.IsTrue(layer.Initialize(catalog));
                int count=root.transform.childCount; Assert.IsTrue(layer.Initialize(catalog)); Assert.AreEqual(count,root.transform.childCount);
                var camera=cameraObject.AddComponent<Camera>(); camera.enabled=false;
                camera.transform.SetPositionAndRotation(Vector3.up*400,Quaternion.Euler(90,0,0));
                layer.ShowOnlyFor(camera);
                var renderers=root.GetComponentsInChildren<MeshRenderer>();
                for(int i=0;i<4;i++)
                {
                    camera.cullingMask=1<<i; camera.farClipPlane=50; camera.useOcclusionCulling=true;
                    camera.backgroundColor=Color.black; camera.clearFlags=CameraClearFlags.Skybox;
                    layer.BeginCapture();
                    Assert.AreEqual(1<<TruckTaxiMapTileLayer.MapLayer,camera.cullingMask);
                    Assert.Greater(camera.farClipPlane,435); Assert.IsFalse(camera.useOcclusionCulling);
                    Assert.IsTrue(renderers.All(renderer=>renderer.enabled));
                    layer.EndCapture();
                    Assert.IsTrue(renderers.All(renderer=>!renderer.enabled));
                    Assert.AreEqual(1<<i,camera.cullingMask); Assert.AreEqual(50,camera.farClipPlane);
                    Assert.IsTrue(camera.useOcclusionCulling); Assert.AreEqual(Color.black,camera.backgroundColor);
                    Assert.AreEqual(CameraClearFlags.Skybox,camera.clearFlags);
                }
                layer.BeginCapture(); layer.ShowOnlyFor(null);
                Assert.IsTrue(renderers.All(renderer=>!renderer.enabled)); Assert.AreEqual(1<<3,camera.cullingMask);
                Assert.IsFalse(layer.IsAssignedTo(camera));
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(cameraObject); }
        }
    }
}
