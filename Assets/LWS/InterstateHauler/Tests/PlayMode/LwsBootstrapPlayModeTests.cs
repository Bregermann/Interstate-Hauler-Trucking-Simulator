using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LWS.InterstateHauler.Tests.PlayMode
{
    public sealed class LwsBootstrapPlayModeTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            LwsApplicationBootstrap.ResetForTests();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (LwsApplicationBootstrap.Instance != null)
            {
                Object.Destroy(LwsApplicationBootstrap.Instance.gameObject);
            }

            LwsApplicationBootstrap.ResetForTests();
            yield return null;
        }

        [UnityTest]
        public IEnumerator BootstrapInitializesOnce()
        {
            var go = new GameObject("test-lws-bootstrap");
            LwsApplicationBootstrap bootstrap = go.AddComponent<LwsApplicationBootstrap>();

            yield return null;

            Assert.AreSame(bootstrap, LwsApplicationBootstrap.Instance);
            Assert.IsTrue(bootstrap.IsReady);
        }

        [UnityTest]
        public IEnumerator DuplicateBootstrapIsRejected()
        {
            var first = new GameObject("first-lws-bootstrap");
            LwsApplicationBootstrap firstBootstrap = first.AddComponent<LwsApplicationBootstrap>();
            var second = new GameObject("second-lws-bootstrap");
            LwsApplicationBootstrap secondBootstrap = second.AddComponent<LwsApplicationBootstrap>();

            yield return null;

            Assert.AreSame(firstBootstrap, LwsApplicationBootstrap.Instance);
            Assert.IsTrue(secondBootstrap == null || secondBootstrap.WasDuplicateRejected);
        }

        [UnityTest]
        public IEnumerator RegisteredServicesReachReadyState()
        {
            var go = new GameObject("ready-lws-bootstrap");
            LwsApplicationBootstrap bootstrap = go.AddComponent<LwsApplicationBootstrap>();

            yield return null;

            Assert.IsNotNull(bootstrap.Registry);
            Assert.IsTrue(bootstrap.Registry.AreAllReady());
        }

        [UnityTest]
        public IEnumerator RenderingCoordinatorInitializes()
        {
            var go = new GameObject("rendering-lws-bootstrap");
            LwsApplicationBootstrap bootstrap = go.AddComponent<LwsApplicationBootstrap>();

            yield return null;

            Assert.IsTrue(bootstrap.Registry.TryGet(out ILwsRenderingService renderingService));
            Assert.AreEqual(LwsServiceState.Ready, FindRenderingRegistrationState(bootstrap.Registry));
            Assert.AreEqual(LwsRenderQualityTier.High, renderingService.ActiveTier);
        }

        [UnityTest]
        public IEnumerator ShutdownDoesNotThrow()
        {
            var go = new GameObject("shutdown-lws-bootstrap");
            LwsApplicationBootstrap bootstrap = go.AddComponent<LwsApplicationBootstrap>();

            yield return null;

            Assert.DoesNotThrow(() => bootstrap.Shutdown());
        }

        private static LwsServiceState FindRenderingRegistrationState(LwsServiceRegistry registry)
        {
            foreach (LwsServiceRegistration registration in registry.Registrations)
            {
                if (registration.Service is ILwsRenderingService)
                {
                    return registration.State;
                }
            }

            return LwsServiceState.Failed;
        }
    }
}
