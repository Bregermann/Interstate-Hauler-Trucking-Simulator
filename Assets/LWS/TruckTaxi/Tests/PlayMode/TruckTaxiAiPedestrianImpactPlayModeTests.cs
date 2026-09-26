using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiAiPedestrianImpactPlayModeTests
    {
        [UnityTest]
        public IEnumerator GenuineAiBodyCollisionEmitsOneWorldEventWithoutPlayerObserver()
        {
            var pedestrianObject = new GameObject("AI impact pedestrian");
            var vehicle = new GameObject("AI impact vehicle");
            int impacts = 0;
            System.Action<TruckTaxiAiPedestrianImpact.Impact> listener = hit => impacts++;
            TruckTaxiAiPedestrianImpact.WorldImpact += listener;
            try
            {
                pedestrianObject.transform.position = Vector3.zero;
                var capsule = pedestrianObject.AddComponent<CapsuleCollider>();
                capsule.center = Vector3.up; capsule.height = 2;
                pedestrianObject.AddComponent<Rigidbody>();
                var pedestrian = pedestrianObject.AddComponent<TruckTaxiPedestrian>();
                pedestrianObject.AddComponent<TruckTaxiAiPedestrianImpact>();
                pedestrianObject.GetComponent<Rigidbody>().useGravity = false;
                pedestrianObject.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezePosition;
                vehicle.transform.position = new Vector3(0, 0, -4);
                var box = vehicle.AddComponent<BoxCollider>(); box.center = Vector3.up; box.size = new Vector3(2, 2, 2);
                var body = vehicle.AddComponent<Rigidbody>(); body.useGravity = false; body.mass = 1500;
                var target = vehicle.AddComponent<TruckTaxiImpactTarget>();
                target.kind = TaxiImpactKind.Traffic; target.targetId = "taxi.traffic.test";
                body.linearVelocity = Vector3.forward * 8;
                float deadline = Time.realtimeSinceStartup + 2;
                while (impacts == 0 && Time.realtimeSinceStartup < deadline) yield return new WaitForFixedUpdate();
                Assert.AreEqual(1, impacts);
                Assert.IsTrue(pedestrian.IsRagdoll);
                Assert.IsTrue(pedestrian.HitEventSent);
                yield return new WaitForFixedUpdate();
                Assert.AreEqual(1, impacts, "Residual contacts cannot emit another world hit.");
            }
            finally
            {
                TruckTaxiAiPedestrianImpact.WorldImpact -= listener;
                Object.Destroy(pedestrianObject); Object.Destroy(vehicle);
            }
        }
    }
}
