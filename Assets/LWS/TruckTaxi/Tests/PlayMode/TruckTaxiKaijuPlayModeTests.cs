using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiKaijuPlayModeTests
    {
        [UnityTest]
        public IEnumerator AuthoredVehicleGrabThrowAndRestoreNeverMutatesTrafficPool()
        {
            var root = new GameObject("kaiju-test");
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.transform.SetParent(root.transform, false);
            var damaged = GameObject.CreatePrimitive(PrimitiveType.Cube);
            damaged.transform.SetParent(root.transform, false);
            damaged.SetActive(false);
            var prop = root.AddComponent<TruckTaxiKaijuProp>();
            prop.intactRoot = visual; prop.damagedRoot = damaged;
            var hand = new GameObject("hand").transform;
            Assert.IsTrue(prop.IsEligible);
            var proxy = prop.Grab(hand);
            Assert.IsNotNull(proxy);
            Assert.IsFalse(visual.activeSelf);
            Assert.IsTrue(damaged.activeSelf);
            var thrown = prop.Throw(Vector3.forward * 100);
            Assert.IsNotNull(thrown);
            Assert.LessOrEqual(thrown.GetComponent<Rigidbody>().linearVelocity.magnitude, 35);
            prop.Restore();
            Assert.IsTrue(visual.activeSelf);
            Assert.IsFalse(damaged.activeSelf);
            Assert.IsTrue(prop.IsEligible);
            Assert.IsTrue(prop.Crush());
            Assert.IsFalse(visual.activeSelf);
            prop.Restore();
            Assert.IsTrue(visual.activeSelf);
            root.AddComponent<TruckTaxiTrafficPooledActor>();
            Assert.IsFalse(prop.IsEligible);
            Object.Destroy(thrown); Object.Destroy(hand.gameObject); Object.Destroy(root);
            yield return null;
            Assert.IsTrue(thrown == null);
        }

        [UnityTest]
        public IEnumerator ProceduralNearAndFarLodsStayVisibleWithoutPhysicalWalls()
        {
            var config = ScriptableObject.CreateInstance<TruckTaxiKaijuConfig>();
            var root = new GameObject("kaiju-actor-test");
            var camera = new GameObject("kaiju-camera-test").transform;
            var actor = root.AddComponent<TruckTaxiKaijuActor>();
            actor.Initialize(config);
            camera.position = root.transform.position + Vector3.forward * 1000;
            actor.Tick(.016f, Vector3.zero, camera);
            Assert.IsTrue(root.transform.Find("Distant silhouette").gameObject.activeSelf);
            Assert.IsFalse(root.transform.Find("Near silhouette").gameObject.activeSelf);
            foreach (var collider in root.GetComponentsInChildren<Collider>(true)) Assert.IsFalse(collider.enabled);
            camera.position = root.transform.position + Vector3.forward * 10;
            actor.Tick(.016f, Vector3.zero, camera);
            Assert.IsTrue(root.transform.Find("Near silhouette").gameObject.activeSelf);
            Object.Destroy(root); Object.Destroy(camera.gameObject); Object.Destroy(config);
            yield return null;
            Assert.IsTrue(root == null);
        }
    }
}
