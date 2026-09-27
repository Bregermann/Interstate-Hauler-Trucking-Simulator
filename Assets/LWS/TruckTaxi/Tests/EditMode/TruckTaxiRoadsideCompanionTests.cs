using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    [Category("TaxiIntegrated")]
    public sealed class TruckTaxiRoadsideCompanionTests
    {
        [Test]
        public void VehicleMotionProxiesCollidableBodyAndRestoresRendererState()
        {
            var truck = new GameObject("Truck");
            var controller = new GameObject("Motion");
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                var rigidbody = truck.AddComponent<Rigidbody>();
                body.transform.SetParent(truck.transform, false);
                body.transform.localPosition = new Vector3(.2f, 1f, -.3f);
                var originalPosition = body.transform.localPosition;
                var rigidbodyPosition = rigidbody.position;
                var collider = body.GetComponent<Collider>();
                var renderer = body.GetComponent<MeshRenderer>();
                var motion = controller.AddComponent<TruckTaxiPrivateEventVehicleMotion>();

                Assert.IsTrue(motion.Initialize(truck.transform));
                Assert.IsTrue(motion.Begin());
                Assert.IsFalse(renderer.enabled);
                Assert.IsTrue(collider.enabled);
                Assert.AreEqual(2, truck.transform.childCount);
                motion.Step(.35f);
                Assert.AreNotEqual(Vector3.zero, truck.transform.GetChild(1).localPosition);
                Assert.AreEqual(originalPosition, body.transform.localPosition);
                Assert.AreEqual(rigidbodyPosition, rigidbody.position);
                motion.StopMotion();

                Assert.IsTrue(renderer.enabled);
                Assert.IsTrue(collider.enabled);
                Assert.AreEqual(1, truck.transform.childCount);
                Assert.AreEqual(originalPosition, body.transform.localPosition);
            }
            finally
            {
                Object.DestroyImmediate(controller);
                Object.DestroyImmediate(truck);
            }
        }

        [Test]
        public void VehicleMotionDoesNotEnableAnOriginallyDisabledRenderer()
        {
            var truck = new GameObject("Truck");
            var controller = new GameObject("Motion");
            var visible = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var disabled = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                truck.AddComponent<Rigidbody>();
                visible.transform.SetParent(truck.transform, false);
                disabled.transform.SetParent(truck.transform, false);
                var disabledRenderer = disabled.GetComponent<MeshRenderer>();
                disabledRenderer.enabled = false;
                var motion = controller.AddComponent<TruckTaxiPrivateEventVehicleMotion>();

                Assert.IsTrue(motion.Initialize(truck.transform));
                Assert.IsTrue(motion.Begin());
                motion.StopMotion();

                Assert.IsTrue(visible.GetComponent<MeshRenderer>().enabled);
                Assert.IsFalse(disabledRenderer.enabled);
            }
            finally
            {
                Object.DestroyImmediate(controller);
                Object.DestroyImmediate(truck);
            }
        }
    }
}
