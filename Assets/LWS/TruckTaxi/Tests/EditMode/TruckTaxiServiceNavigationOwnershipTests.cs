using System.Reflection;
using LWS.InterstateHauler;
using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiServiceNavigationOwnershipTests
    {
        [Test]
        public void OfferClosureRetainsExactServiceRouteAndTargetUntilServiceCompletes()
        {
            var gameObject=new GameObject("Service GPS ownership");
            try
            {
                var gps=gameObject.AddComponent<TruckTaxiGPSAdapter>();
                var navigation=new LwsNavigationService();
                var route=new LwsRouteResult { routeId="existing.service.route", succeeded=true };
                SetPrivate(navigation,"<CurrentRoute>k__BackingField",route);
                SetPrivate(gps,"navigation",navigation);
                SetPrivate(gps,"player",gameObject.transform);
                Assert.IsTrue(gps.TrySetNavigationIntent(TruckTaxiNavigationLayer.PlayerService,"player-service",
                    "service.restroom","Service",Vector3.zero,TruckTaxiMapMarkerType.Place));
                SetPrivate(navigation,"<CurrentRoute>k__BackingField",route);

                gps.FrameOffer(null);
                gps.ClearRideDestination();
                Assert.IsTrue(gps.IsServiceDestination);
                Assert.AreEqual("service.restroom",gps.TargetId);
                Assert.AreSame(route,navigation.CurrentRoute);
                Assert.IsFalse(gps.CompleteServiceDestination("different.service"));
                Assert.AreSame(route,navigation.CurrentRoute);

                Assert.IsTrue(gps.CompleteServiceDestination("service.restroom"));
                Assert.IsFalse(gps.IsServiceDestination);
                Assert.IsNull(navigation.CurrentRoute);
            }
            finally { Object.DestroyImmediate(gameObject); }
        }

        private static void SetPrivate(object target,string name,object value)
        {
            var field=target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic);
            Assert.IsNotNull(field,name);
            field.SetValue(target,value);
        }
    }
}
