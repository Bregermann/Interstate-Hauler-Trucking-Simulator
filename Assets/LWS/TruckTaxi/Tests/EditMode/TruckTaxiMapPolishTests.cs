using System.Reflection;
using LWS.InterstateHauler;
using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiMapZoomProbe : MonoBehaviour
    {
        public float miniMapFullScreenZoomLevel { get; set; }=1f;
    }

    [Category("TaxiPolish")]
    public sealed class TruckTaxiMapPolishTests
    {
        [Test]
        public void RegionalMapCatalogRetainsItsShaderAsABuildDependency()
        {
            var catalog=Resources.Load<TruckTaxiMapTileCatalog>(TruckTaxiMapTileCatalog.ResourcePath);
            Assert.NotNull(catalog);
            Assert.NotNull(catalog.tileMaterial,"Runtime Shader.Find alone is not a player-build dependency.");
            Assert.NotNull(catalog.tileMaterial.shader);
            Assert.IsNotEmpty(catalog.tiles);
            string catalogPath=UnityEditor.AssetDatabase.GetAssetPath(catalog);
            string shaderPath=UnityEditor.AssetDatabase.GetAssetPath(catalog.tileMaterial.shader);
            Assert.That(UnityEditor.AssetDatabase.GetDependencies(catalogPath,true),Does.Contain(shaderPath));
            foreach(var tile in catalog.tiles) Assert.NotNull(tile.texture,tile.sceneName);
        }

        [Test]
        public void EveryMapInputUsesPositiveStepsForZoomIn()
        {
            var go=new GameObject("Full map zoom probe");
            try
            {
                var gps=go.AddComponent<TruckTaxiGPSAdapter>();
                SetPrivate(gps,"previewCompass",go.AddComponent<TruckTaxiMapZoomProbe>());
                SetPrivate(gps,"fullMapOpen",true);
                gps.ZoomFullMap(1);
                Assert.Greater(gps.FullMapZoomLevel,1f,"Plus, PageUp, right shoulder and positive wheel must zoom in.");
                gps.ZoomFullMap(-1);
                Assert.That(gps.FullMapZoomLevel,Is.EqualTo(1f).Within(.001f));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void StopNavigationKeepsServiceTargetAndOnlyClearsGuidance()
        {
            var go=new GameObject("Map navigation probe");
            try
            {
                var gps=go.AddComponent<TruckTaxiGPSAdapter>();
                var navigation=new LwsNavigationService();
                SetPrivate(navigation,"<CurrentRoute>k__BackingField",new LwsRouteResult { routeId="private.stop",succeeded=true });
                SetPrivate(gps,"navigation",navigation);
                SetPrivate(gps,"player",go.transform);
                SetPrivate(gps,"serviceTargetActive",true);
                SetPrivate(gps,"serviceTargetPosition",new Vector3(100,0,0));
                SetPrivate(gps,"<TargetId>k__BackingField","private.stop");
                gps.StopNavigation();
                Assert.IsTrue(gps.GuidanceSuppressed);
                Assert.IsTrue(gps.IsServiceDestination);
                Assert.AreEqual("private.stop",gps.TargetId);
                Assert.IsNull(navigation.CurrentRoute);
                Assert.IsFalse(gps.RouteReady);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void NewRegionalStopsUseTypedIconFamilies()
        {
            Assert.AreEqual(TruckTaxiMapMarkerType.Racetrack,
                TruckTaxiMapIconRegistry.ForStop(TruckTaxiStopCategory.Racetrack));
            Assert.AreEqual(TruckTaxiMapMarkerType.FoodStop,
                TruckTaxiMapIconRegistry.ForStop(TruckTaxiStopCategory.FoodStop));
        }

        private static void SetPrivate(object target,string name,object value)
        {
            var field=target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic);
            Assert.NotNull(field,name);
            field.SetValue(target,value);
        }
    }
}
