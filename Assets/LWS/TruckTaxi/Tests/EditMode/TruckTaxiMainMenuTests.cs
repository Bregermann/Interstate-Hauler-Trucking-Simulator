using System.Reflection;
using LWS.TruckTaxi.Editor;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiMainMenuTests
    {
        [Test]
        public void MenuVisualStrippingKeepsRenderersButNoGameplayOrAudio()
        {
            var display=GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                display.AddComponent<Rigidbody>();
                display.AddComponent<AudioSource>();
                display.AddComponent<AudioListener>();
                display.AddComponent<TruckTaxiMainMenuIdle>();
                var strip=typeof(EditorTruckTaxiMainMenuSetup).GetMethod("StripToVisual",
                    BindingFlags.NonPublic|BindingFlags.Static);
                Assert.NotNull(strip);
                strip.Invoke(null,new object[]{display,true});
                Assert.NotNull(display.GetComponent<Renderer>());
                Assert.NotNull(display.GetComponent<MeshFilter>());
                Assert.IsNull(display.GetComponent<Collider>());
                Assert.IsNull(display.GetComponent<Rigidbody>());
                Assert.IsNull(display.GetComponent<AudioSource>());
                Assert.IsNull(display.GetComponent<AudioListener>());
                Assert.IsNull(display.GetComponent<TruckTaxiMainMenuIdle>());
            }
            finally { Object.DestroyImmediate(display); }
        }

        [Test]
        public void StatsHandoffIsTransientAndUsesPublishedValues()
        {
            TruckTaxiMainMenu.ClearSessionStats();
            Assert.IsFalse(TruckTaxiMainMenu.LastSessionStats.HasValue);
            try
            {
                var stats=new TruckTaxiMainMenuStats { rides=3, earningsCents=1234, chaos=42 };
                TruckTaxiMainMenu.PublishSessionStats(stats);
                Assert.AreEqual(3,TruckTaxiMainMenu.LastSessionStats.Value.rides);
                Assert.AreEqual(1234,TruckTaxiMainMenu.LastSessionStats.Value.earningsCents);
                Assert.AreEqual(42f,TruckTaxiMainMenu.LastSessionStats.Value.chaos);
            }
            finally { TruckTaxiMainMenu.ClearSessionStats(); }
        }
    }
}
