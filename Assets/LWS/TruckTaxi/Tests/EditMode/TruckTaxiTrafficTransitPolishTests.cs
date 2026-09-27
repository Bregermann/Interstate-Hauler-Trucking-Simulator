using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    [Category("TaxiPolish"), Category("TaxiMegaPass")]
    public sealed class TruckTaxiTrafficTransitPolishTests
    {
        [Test]
        public void PedestrianGraceProtectsOnlyUntilExpiry()
        {
            Assert.IsTrue(TruckTaxiPedestrianPopulation.KeepLive(40000, 170, 20, 1, 9, false));
            Assert.IsFalse(TruckTaxiPedestrianPopulation.KeepLive(40000, 170, 20, 9, 9, false));
            Assert.IsTrue(TruckTaxiPedestrianPopulation.KeepLive(40000, 170, 20, 9, 0, true));
            Assert.IsTrue(TruckTaxiPedestrianPopulation.KeepLive(36100, 170, 20, 9, 0, false));
        }

        [Test]
        public void NpcLauncherRejectsInvalidOrUnboundedThrows()
        {
            Assert.IsNull(TruckTaxiNpcProjectileLauncher.LaunchNpcProjectile(
                new Vector3(float.NaN, 0, 0), Vector3.zero, null, TruckTaxiNpcProjectileStyle.FilledJug));
            Assert.IsNull(TruckTaxiNpcProjectileLauncher.LaunchNpcProjectile(
                Vector3.zero, Vector3.right * 100, null, TruckTaxiNpcProjectileStyle.RoadRageGrenade));
        }

        [Test]
        public void BusRouteProjectionPreservesMidRoutePosition()
        {
            var points = new[] { Vector3.zero, Vector3.right * 10, new Vector3(10, 0, 10), Vector3.forward * 10 };
            float progress = TruckTaxiBusService.ProjectDistance(points, new Vector3(10, 0, 6));
            Assert.AreEqual(16, progress, .001f);
            Assert.AreEqual(new Vector3(10, 0, 6), TruckTaxiBusService.Sample(points, progress));
        }

        [Test]
        public void BusPhysicalBoxFitsVisibleBodyAndUsesTrafficIdentity()
        {
            var bus=new GameObject("Bus");
            var visual=GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                var body=bus.AddComponent<Rigidbody>();
                visual.name="Big_Bus_LOD0";
                visual.transform.SetParent(bus.transform,false);
                visual.transform.localPosition=Vector3.up*2f;
                visual.transform.localScale=new Vector3(2.5f,3f,12f);
                bus.transform.rotation=Quaternion.Euler(0,37,0);
                typeof(TruckTaxiBusService).GetMethod("FitPhysicalBody",BindingFlags.NonPublic | BindingFlags.Static)
                    .Invoke(null,new object[]{body});
                var fitted=bus.GetComponent<BoxCollider>();
                Assert.IsNotNull(fitted);
                Assert.Greater(fitted.bounds.size.z,11f);
                Assert.Greater(fitted.bounds.size.x,2.3f);
                Assert.Less(fitted.size.x,3f);
                Assert.Less(fitted.size.z,13f);
                Assert.IsFalse(fitted.isTrigger);
                var target=bus.AddComponent<TruckTaxiImpactTarget>();
                target.kind=TaxiImpactKind.Traffic;
                Assert.IsTrue(TruckTaxiImpactTarget.FromCollider(fitted).IsTrafficVehicle);
            }
            finally { UnityEngine.Object.DestroyImmediate(bus); }
        }

        [Test]
        public void UtsPathInitializationDoesNotMoveBusOffContinuousProgress()
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(candidate => candidate.GetType("MovePath") != null);
            Assert.IsNotNull(assembly, "Installed UTS MovePath must be loaded.");
            var pathType = assembly.GetType("WalkPath");
            var moverType = assembly.GetType("MovePath");
            Assert.IsNotNull(pathType);
            var actor = new GameObject("Bus progress probe");
            try
            {
                var path = actor.AddComponent(pathType);
                var mover = actor.AddComponent(moverType);
                var points = new Vector3[1, 5];
                for (int i = 0; i < 5; i++) points[0, i] = new Vector3(i * 10, 0, 0);
                pathType.GetField("points").SetValue(path, points);
                pathType.GetField("pointLength").SetValue(path, new[] { 5, 0 });
                moverType.GetField("walkPath").SetValue(mover, path);
                var continuousPosition = new Vector3(14, 0, 2);
                actor.transform.position = continuousPosition;

                moverType.GetMethod("InitStartPosition").Invoke(mover, new object[] { 0, 1, true, true });
                moverType.GetMethod("SetLookPosition").Invoke(mover, null);

                Assert.AreEqual(continuousPosition, actor.transform.position);
                Assert.Greater(Vector3.Dot(actor.transform.forward,
                    (points[0, 2] - continuousPosition).normalized), .999f);
            }
            finally { UnityEngine.Object.DestroyImmediate(actor); }
        }
    }
}
