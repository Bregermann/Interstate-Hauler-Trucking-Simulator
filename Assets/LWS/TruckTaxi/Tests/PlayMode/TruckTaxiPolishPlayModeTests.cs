using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LWS.TruckTaxi.Tests
{
    [Category("TaxiPolish")]
    public sealed class TruckTaxiPolishPlayModeTests
    {
        [UnityTest]
        public IEnumerator WaitingTriggerBecomesSolidBoundedEjectionBody()
        {
            var truck=new GameObject("Tractor fixture");
            var person=new GameObject("Passenger fixture");
            try
            {
                var truckBody=truck.AddComponent<Rigidbody>(); truckBody.isKinematic=true;
                var actor=person.AddComponent<TruckTaxiPassengerActor>(); actor.Bind(null);
                actor.ArmWaitingTruckHit(truckBody);
                Assert.IsTrue(person.GetComponent<CapsuleCollider>().isTrigger);
                actor.Eject(Vector3.right*80,truck.transform);
                Assert.IsFalse(person.GetComponent<CapsuleCollider>().isTrigger);
                Assert.IsFalse(person.GetComponent<Rigidbody>().isKinematic);
                Assert.LessOrEqual(person.GetComponent<Rigidbody>().linearVelocity.magnitude,22.01f);
                yield return null;
            }
            finally { Object.Destroy(person); Object.Destroy(truck); }
        }

        [UnityTest]
        public IEnumerator BoundedOffersMapAndCompanionEjectionFixtures()
        {
            yield return SceneManager.LoadSceneAsync("TruckTaxi_DemoCity");
            float deadline=Time.realtimeSinceStartup+60;
            while((TruckTaxiBootstrap.Instance==null || !TruckTaxiBootstrap.Instance.Ready) && Time.realtimeSinceStartup<deadline)
                yield return null;
            var host=TruckTaxiBootstrap.Instance;
            Assert.IsNotNull(host); Assert.IsTrue(host.Ready);
            var failures=new List<string>();
            yield return TruckTaxiPolishRuntimeProbe.Run(host,(ok,message)=> {
                Debug.Log("TAXI POLISH FIXTURE "+(ok ? "PASS: " : "FAIL: ")+message);
                if(!ok) failures.Add(message);
            },null);
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
    }
}
