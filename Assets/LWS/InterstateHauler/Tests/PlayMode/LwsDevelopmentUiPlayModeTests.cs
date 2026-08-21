using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LWS.InterstateHauler.Tests.PlayMode
{
    public sealed class LwsDevelopmentUiPlayModeTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            LwsApplicationBootstrap.ResetForTests();
            Time.timeScale = 1f;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (LwsDevelopmentUiRoot root in Object.FindObjectsByType<LwsDevelopmentUiRoot>(FindObjectsSortMode.None))
            {
                Object.Destroy(root.gameObject);
            }

            if (LwsApplicationBootstrap.Instance != null)
            {
                Object.Destroy(LwsApplicationBootstrap.Instance.gameObject);
            }

            LwsApplicationBootstrap.ResetForTests();
            Time.timeScale = 1f;
            yield return null;
        }

        [UnityTest]
        public IEnumerator BootstrapCreatesSingleDevelopmentUiRuntime()
        {
            var go = new GameObject("development-ui-bootstrap");
            LwsApplicationBootstrap bootstrap = go.AddComponent<LwsApplicationBootstrap>();

            yield return null;

            Assert.IsTrue(bootstrap.Registry.TryGet(out ILwsDevelopmentUiService service));
            service.EnsureRuntime();
            yield return null;

            Assert.AreEqual(1, Object.FindObjectsByType<LwsDevelopmentUiRoot>(FindObjectsSortMode.None).Length);
            Assert.IsNotNull(service.RuntimeRoot.Canvas);
            Assert.IsNotNull(service.RuntimeRoot.CanvasScaler);
            Assert.IsNotNull(service.RuntimeRoot.ScrollRect);
            AssertSemanticMapHasRequiredComponents(service.RuntimeRoot.MinimapGraphic);
            AssertSemanticMapHasRequiredComponents(service.RuntimeRoot.BigMapGraphic);
            Assert.IsFalse(service.IsVisible);
        }

        [UnityTest]
        public IEnumerator DevelopmentUiServiceTogglesControlCenterAndBigMap()
        {
            var go = new GameObject("development-ui-toggle-bootstrap");
            LwsApplicationBootstrap bootstrap = go.AddComponent<LwsApplicationBootstrap>();

            yield return null;

            Assert.IsTrue(bootstrap.Registry.TryGet(out ILwsDevelopmentUiService service));
            service.Show();
            yield return null;

            Assert.IsTrue(service.IsVisible);
            service.OpenTab(LwsDevelopmentUiTab.GpsNavigation);
            Assert.AreEqual(LwsDevelopmentUiTab.GpsNavigation, service.ActiveTab);

            service.ShowBigMap();
            yield return null;

            Assert.IsTrue(service.IsBigMapVisible);
            Assert.AreEqual(0f, Time.timeScale);

            service.HideBigMap();
            yield return null;

            Assert.IsFalse(service.IsBigMapVisible);
            Assert.AreEqual(1f, Time.timeScale);

            service.Hide();
            Assert.IsFalse(service.IsVisible);
        }

        [UnityTest]
        public IEnumerator SemanticMapKeepsRoutePresentationStableAcrossOriginShift()
        {
            var go = new GameObject("origin-map-bootstrap");
            LwsApplicationBootstrap bootstrap = go.AddComponent<LwsApplicationBootstrap>();
            yield return null;

            LwsRoadGraph graph = LwsFiftyMileHighwayModel.CreateRoadGraph();
            Assert.IsTrue(bootstrap.Registry.TryGet(out ILwsRoadGraphService graphService));
            Assert.IsTrue(bootstrap.Registry.TryGet(out ILwsNavigationService navigationService));
            Assert.IsTrue(bootstrap.Registry.TryGet(out ILwsWorldOriginService originService));
            graphService.SetActiveGraph(graph);
            navigationService.SetDestination(
                LwsFiftyMileHighwayModel.EastboundDestinationPosition,
                LwsFiftyMileHighwayModel.EastboundStartPosition,
                graph);

            Assert.IsTrue(bootstrap.Registry.TryGet(out ILwsDevelopmentUiService uiService));
            uiService.EnsureRuntime();
            yield return null;

            LwsDevelopmentUiRoot root = uiService.RuntimeRoot;
            originService.UpdatePlayerLocalPosition(LwsFiftyMileHighwayModel.EastboundStartPosition);
            yield return null;
            Assert.IsTrue(root.MinimapGraphic.HasRoutePresentation);

            Assert.IsTrue(originService.SetOriginOffsetForValidation(new LwsWorldPositionD(0d, 0d, 5000d), "map test", out _));
            originService.UpdatePlayerLocalPosition(originService.GlobalToLocal(LwsWorldPositionD.FromVector3(LwsFiftyMileHighwayModel.EastboundStartPosition)));
            yield return null;

            Assert.IsTrue(root.MinimapGraphic.HasRoutePresentation);
            Assert.AreEqual(1, Object.FindObjectsByType<LwsDevelopmentUiRoot>(FindObjectsSortMode.None).Length);
        }

        private static void AssertSemanticMapHasRequiredComponents(LwsSemanticGpsMapGraphic mapGraphic)
        {
            Assert.IsNotNull(mapGraphic);
            Assert.IsNotNull(mapGraphic.GetComponent<RectTransform>());
            Assert.IsNotNull(mapGraphic.GetComponent<CanvasRenderer>());
        }
    }
}
