using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiKaijuCaptureTests
    {
        private GameObject root;
        private TruckTaxiKaijuRendererLease lease;

        [TearDown]
        public void TearDown()
        {
            lease?.Dispose();
            if (root != null) Object.DestroyImmediate(root);
        }

        [Test]
        public void InterruptedCaptureRestoresVisibilityWithoutChangingPhysics()
        {
            root = new GameObject("kaiju-capture-test");
            var body = root.AddComponent<Rigidbody>();
            body.isKinematic = false;
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeRotationX;
            var visible = new GameObject("visible"); visible.transform.SetParent(root.transform);
            var visibleRenderer = visible.AddComponent<MeshRenderer>();
            var hidden = new GameObject("hidden cab mesh"); hidden.transform.SetParent(root.transform);
            var hiddenRenderer = hidden.AddComponent<MeshRenderer>(); hiddenRenderer.enabled = false;
            lease = new TruckTaxiKaijuRendererLease(new Renderer[] { visibleRenderer, hiddenRenderer });
            Assert.IsFalse(visibleRenderer.enabled);
            visibleRenderer.enabled = true;
            lease.Suppress();
            Assert.IsFalse(visibleRenderer.enabled);
            lease.Dispose();
            lease.Dispose();
            Assert.IsTrue(visibleRenderer.enabled);
            Assert.IsFalse(hiddenRenderer.enabled);
            Assert.IsFalse(body.isKinematic);
            Assert.IsFalse(body.useGravity);
            Assert.AreEqual(RigidbodyConstraints.FreezeRotationX, body.constraints);
        }

        [Test]
        public void CleanupToleratesDestroyedRenderersAndPreservesKinematicBodies()
        {
            root = new GameObject("kaiju-destroyed-renderer-test");
            var body = root.AddComponent<Rigidbody>(); body.isKinematic = true;
            var renderer = root.AddComponent<MeshRenderer>();
            lease = new TruckTaxiKaijuRendererLease(new Renderer[] { renderer, null });
            Object.DestroyImmediate(renderer);
            Assert.DoesNotThrow(() => lease.Dispose());
            Assert.IsTrue(body.isKinematic);
        }

        [Test]
        public void CaptureCannotStartBeforeExplicitInitialization()
        {
            root = new GameObject("kaiju-uninitialized-test");
            var director = root.AddComponent<TruckTaxiKaijuDirector>();
            Assert.IsFalse(director.Initialize(null));
            Assert.IsFalse(director.PlayerCaptureBound);
            Assert.IsFalse(director.ForceGrabPlayer());
            var recovery = root.AddComponent<TruckTaxiKaijuPlayerRecovery>();
            Assert.IsFalse(recovery.Initialize(null));
            Assert.IsFalse(recovery.TryBegin(default));
            Assert.DoesNotThrow(() => recovery.Complete(false));
        }

        [Test]
        public void InvalidTuningCannotRemoveEscapeWarningOrReachAcrossTown()
        {
            var config = ScriptableObject.CreateInstance<TruckTaxiKaijuConfig>();
            try
            {
                config.playerWarningSeconds = 0;
                config.playerCaptureRadiusMeters = 10000;
                Assert.AreEqual(3, config.CaptureWarningSeconds);
                Assert.AreEqual(50, config.CaptureRadiusMeters);
                config.playerWarningSeconds = float.NaN;
                Assert.AreEqual(7, config.CaptureWarningSeconds);
            }
            finally { Object.DestroyImmediate(config); }
        }
    }
}
