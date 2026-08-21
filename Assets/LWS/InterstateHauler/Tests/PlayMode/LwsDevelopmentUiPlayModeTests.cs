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
            AssertMinimapAnchoredBottomRight(service.RuntimeRoot.MinimapRect);
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
            yield return new WaitForSecondsRealtime(0.12f);
            Assert.IsTrue(root.MinimapGraphic.GraphBound);
            Assert.Greater(root.MinimapGraphic.RoadCount, 0);
            Assert.Greater(root.MinimapGraphic.EdgeCount, 0);
            Assert.Greater(root.MinimapGraphic.CenterlineSampleCount, 0);
            Assert.Greater(root.MinimapGraphic.BaseRoadVertexCount, 0);
            Assert.Greater(root.MinimapGraphic.BaseRoadTriangleCount, 0);
            Assert.Greater(root.BigMapGraphic.BaseRoadVertexCount, 0);
            Assert.Greater(root.BigMapGraphic.BaseRoadTriangleCount, 0);
            Assert.IsTrue(root.MinimapGraphic.HasRoutePresentation);
            Assert.Greater(root.MinimapGraphic.RouteTriangleCount, 0);

            navigationService.ClearRoute();
            yield return new WaitForSecondsRealtime(0.12f);
            Assert.Greater(root.MinimapGraphic.BaseRoadTriangleCount, 0);
            Assert.AreEqual(0, root.MinimapGraphic.RouteTriangleCount);

            Assert.IsTrue(originService.SetOriginOffsetForValidation(new LwsWorldPositionD(0d, 0d, 5000d), "map test", out _));
            originService.UpdatePlayerLocalPosition(originService.GlobalToLocal(LwsWorldPositionD.FromVector3(LwsFiftyMileHighwayModel.EastboundStartPosition)));
            navigationService.SetDestination(
                LwsFiftyMileHighwayModel.EastboundDestinationPosition,
                LwsFiftyMileHighwayModel.EastboundStartPosition,
                graph);
            yield return new WaitForSecondsRealtime(0.12f);

            Assert.IsTrue(root.MinimapGraphic.HasRoutePresentation);
            Vector2 playerMap = root.MinimapGraphic.ProjectGlobalPointToMap(LwsFiftyMileHighwayModel.EastboundStartPosition);
            Assert.AreEqual(0.5f, playerMap.x, 0.0001f);
            Assert.AreEqual(0.4f, playerMap.y, 0.0001f);
            Assert.AreEqual(1, Object.FindObjectsByType<LwsDevelopmentUiRoot>(FindObjectsSortMode.None).Length);
        }

        private static void AssertSemanticMapHasRequiredComponents(LwsSemanticGpsMapGraphic mapGraphic)
        {
            Assert.IsNotNull(mapGraphic);
            Assert.IsNotNull(mapGraphic.GetComponent<RectTransform>());
            Assert.IsNotNull(mapGraphic.GetComponent<CanvasRenderer>());
        }

        private static void AssertMinimapAnchoredBottomRight(RectTransform minimap)
        {
            Assert.IsNotNull(minimap);
            Assert.AreEqual(new Vector2(1f, 0f), minimap.anchorMin);
            Assert.AreEqual(new Vector2(1f, 0f), minimap.anchorMax);
            Assert.AreEqual(new Vector2(1f, 0f), minimap.pivot);
            Assert.AreEqual(304f, minimap.sizeDelta.x, 0.01f);
            Assert.AreEqual(304f, minimap.sizeDelta.y, 0.01f);
            Assert.AreEqual(-28f, minimap.anchoredPosition.x, 0.01f);
            Assert.AreEqual(28f, minimap.anchoredPosition.y, 0.01f);
        }
    }
}
