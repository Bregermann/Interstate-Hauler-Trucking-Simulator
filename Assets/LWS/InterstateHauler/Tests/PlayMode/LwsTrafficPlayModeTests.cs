using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LWS.InterstateHauler.Tests.PlayMode
{
    public sealed class LwsTrafficPlayModeTests
    {
        [UnityTest]
        public IEnumerator TrafficIdentityConfiguresAiVehicleIdentity()
        {
            var go = new GameObject("traffic-identity-test");
            LwsTrafficIdentity trafficIdentity = go.AddComponent<LwsTrafficIdentity>();

            trafficIdentity.Configure("ih.traffic.play.001", "lane-a", "Car_1", LwsTrafficVehicleKind.PassengerCar);

            yield return null;

            LwsVehicleIdentity vehicleIdentity = go.GetComponent<LwsVehicleIdentity>();
            Assert.IsNotNull(vehicleIdentity);
            Assert.AreEqual(LwsVehicleRole.AiVehicle, vehicleIdentity.Role);
            Assert.AreEqual("ih.traffic.play.001", vehicleIdentity.VehicleId);
            Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator TrafficServiceUnregistersDestroyedIdentity()
        {
            var registry = new LwsServiceRegistry();
            var service = new LwsTrafficService();
            registry.Register<ILwsTrafficService>(service);
            registry.InitializeAll();

            var go = new GameObject("traffic-register-test");
            LwsTrafficIdentity identity = go.AddComponent<LwsTrafficIdentity>();
            identity.Configure("ih.traffic.play.002", "lane-a", "Car_1", LwsTrafficVehicleKind.PassengerCar);

            LwsServiceResult result = service.RegisterTrafficVehicle(identity);
            Assert.IsTrue(result.Succeeded, result.Message);
            Assert.AreEqual(1, service.ActiveTrafficVehicles.Count);

            service.UnregisterTrafficVehicle(identity);
            yield return null;

            Assert.AreEqual(0, service.ActiveTrafficVehicles.Count);
            Object.Destroy(go);
            registry.ShutdownAll();
        }
    }
}
