using NUnit.Framework;
using System.Reflection;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    public class TruckTaxiGPSDisplayTests
    {
        [Test]
        public void InstrumentCaptureRestoresCameraWithoutChangingWorldWeather()
        {
            var root=new GameObject("Instrument capture"); var map=new GameObject("Map camera"); var world=new GameObject("World camera");
            try
            {
                var gps=root.AddComponent<TruckTaxiGPSAdapter>();
                typeof(TruckTaxiGPSAdapter).GetField("attemptedTileLoad",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(gps,true);
                var camera=map.AddComponent<Camera>(); var worldCamera=world.AddComponent<Camera>();
                var original=camera.cameraType; var worldType=worldCamera.cameraType;
                bool fog=RenderSettings.fog;
                Invoke(gps,"BeginInstrumentCapture",camera);
                Assert.AreEqual(CameraType.Preview,camera.cameraType);
                Assert.AreEqual(worldType,worldCamera.cameraType);
                Assert.AreEqual(fog,RenderSettings.fog);
                Invoke(gps,"EndMapCapture");
                Assert.AreEqual(original,camera.cameraType);
                Invoke(gps,"BeginInstrumentCapture",camera);
                Invoke(gps,"OnDisable");
                Assert.AreEqual(original,camera.cameraType,"Interrupted capture restores state on disable.");
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(map); Object.DestroyImmediate(world); }
        }

        [Test]
        public void HazardFootprintNeverMovesToViewportEdgeDuringPanOrZoom()
        {
            var root=new GameObject("Hazard projection camera");
            try
            {
                var camera=root.AddComponent<Camera>(); camera.orthographic=true; camera.aspect=1; camera.orthographicSize=50;
                camera.transform.SetPositionAndRotation(new Vector3(0,100,0),Quaternion.Euler(90,0,0));
                Assert.IsTrue(TruckTaxiWorkAreaGraphic.ProjectFootprint(camera,Vector3.zero,10,out var center,out var radii));
                Assert.That(center.x,Is.EqualTo(.5f).Within(.001f));
                camera.transform.position=new Vector3(55,100,0);
                Assert.IsTrue(TruckTaxiWorkAreaGraphic.ProjectFootprint(camera,Vector3.zero,10,out center,out radii));
                Assert.Less(center.x,0,"Partial footprint keeps its offscreen center; mask clips only its pixels.");
                camera.transform.position=new Vector3(100,100,0);
                Assert.IsFalse(TruckTaxiWorkAreaGraphic.ProjectFootprint(camera,Vector3.zero,10,out center,out radii));
                Assert.Less(center.x,0);
                camera.orthographicSize=150;
                Assert.IsTrue(TruckTaxiWorkAreaGraphic.ProjectFootprint(camera,Vector3.zero,10,out center,out radii));
                camera.transform.position=new Vector3(0,100,0);
                Assert.IsTrue(TruckTaxiWorkAreaGraphic.ProjectFootprint(camera,Vector3.zero,10,out center,out radii));
                Assert.That(center.x,Is.EqualTo(.5f).Within(.001f));
            }
            finally { Object.DestroyImmediate(root); }
        }
        private static void Invoke(object target,string method,params object[] args) =>
            target.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,args);
        [Test]
        public void DefaultsKeepHudVisibleAndBothMapsReadable()
        {
            var settings=new TruckTaxiGPSDisplaySettings();
            Assert.IsTrue(settings.showHud);
            Assert.IsTrue(settings.showPois);
            Assert.AreEqual(700,settings.hudRangeMeters);
            Assert.AreEqual(700,settings.cabRangeMeters);
            Assert.AreEqual(12,settings.routeWidth);
        }
        [Test]
        public void InvalidPreferencesCannotCollapseTheDisplay()
        {
            var settings=new TruckTaxiGPSDisplaySettings {
                hudSize=0,cabRangeMeters=float.NaN,hudRangeMeters=float.PositiveInfinity,
                routeWidth=-1,routeColorIndex=999
            };
            settings.Validate();
            Assert.AreEqual(.18f,settings.hudSize);
            Assert.AreEqual(700,settings.cabRangeMeters);
            Assert.AreEqual(700,settings.hudRangeMeters);
            Assert.AreEqual(4,settings.routeWidth);
            Assert.AreEqual(3,settings.routeColorIndex);
        }
        [Test]
        public void DisplayPreferencesRoundTripWithoutCareerOrRouteData()
        {
            string prefix="ih.tests.gps."+System.Guid.NewGuid().ToString("N")+".";
            try
            {
                var settings=new TruckTaxiGPSDisplaySettings {
                    showHud=false,northUp=true,showPois=false,hudSize=.25f,
                    hudRangeMeters=900,cabRangeMeters=450,routeWidth=18,routeColorIndex=2
                };
                settings.Save(prefix);
                var loaded=TruckTaxiGPSDisplaySettings.Load(prefix);
                Assert.IsFalse(loaded.showHud); Assert.IsTrue(loaded.northUp); Assert.IsFalse(loaded.showPois);
                Assert.AreEqual(.25f,loaded.hudSize); Assert.AreEqual(900,loaded.hudRangeMeters);
                Assert.AreEqual(450,loaded.cabRangeMeters); Assert.AreEqual(18,loaded.routeWidth);
                Assert.AreEqual(settings.RouteColor,loaded.RouteColor);
            }
            finally
            {
                foreach(var key in new[]{"hud","north","pois","size","hudRange","cabRange","width","color"}) PlayerPrefs.DeleteKey(prefix+key);
            }
        }
    }
}
