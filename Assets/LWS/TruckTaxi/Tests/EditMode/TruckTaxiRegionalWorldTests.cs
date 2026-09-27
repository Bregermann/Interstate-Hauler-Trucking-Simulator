using NUnit.Framework;
using System;
using System.Reflection;
using LWS.InterstateHauler;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    [Category("TaxiRegional")]
    public sealed class TruckTaxiRegionalWorldTests
    {
        [Test]
        public void InstalledSceneStreamerExplicitLoadSignatureIsAvailable()
        {
            var type = Type.GetType("PixelCrushers.SceneStreamer.SceneStreamer, PixelCrushers.SceneStreamer");
            Assert.That(type, Is.Not.Null);
            var handler = type.GetNestedType("InternalLoadedHandler", BindingFlags.NonPublic);
            Assert.That(handler, Is.Not.Null);
            Assert.That(type.GetMethod("Load", BindingFlags.Instance | BindingFlags.NonPublic, null,
                new[] { typeof(string), handler, typeof(int) }, null), Is.Not.Null);
            Assert.That(type.GetProperty("instance", BindingFlags.Static | BindingFlags.NonPublic), Is.Not.Null);
        }
        [Test]
        public void RegionDistanceUsesHorizontalWorldBounds()
        {
            var region = new TruckTaxiRegion { bounds = new Bounds(new Vector3(2000,0,0), new Vector3(1000,100,1000)) };
            Assert.That(region.Distance(new Vector3(2000,800,0)), Is.Zero);
            Assert.That(region.Distance(new Vector3(2750,0,0)), Is.EqualTo(250));
        }
        [Test]
        public void UnloadedRegionRemainsResolvableWithoutBecomingAvailable()
        {
            var go = new GameObject("Regional metadata test");
            try
            {
                var world = go.AddComponent<TruckTaxiRegionalWorld>();
                world.regions = new[] { new TruckTaxiRegion { id="town02",sceneName="never-load-this-fixture",bounds=new Bounds(new Vector3(4000,0,0),new Vector3(700,50,700)) } };
                Assert.That(world.ResolveRegion(new Vector3(4000,0,0)), Is.EqualTo("town02"));
                Assert.That(world.IsPositionAvailable(new Vector3(4000,0,0)), Is.False);
                Assert.That(world.LoadedCount, Is.Zero);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
        [Test]
        public void LogicalTargetsAre150CarsAnd720Pedestrians()
        {
            var profile = ScriptableObject.CreateInstance<TruckTaxiPopulationProfile>();
            try
            {
                Assert.That(profile.TrafficTarget(12), Is.EqualTo(150));
                Assert.That(profile.PedestrianTarget(12), Is.EqualTo(720));
            }
            finally { UnityEngine.Object.DestroyImmediate(profile); }
        }
    }
}
