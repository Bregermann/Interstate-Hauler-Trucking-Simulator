using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    public class TruckTaxiGPSDisplayTests
    {
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
