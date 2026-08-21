using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

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

            foreach (EventSystem eventSystem in Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
            {
                Object.Destroy(eventSystem.gameObject);
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
            LwsDevelopmentUiRoot root = service.RuntimeRoot;
            Assert.IsTrue(root.gameObject.activeInHierarchy);
            Assert.IsNotNull(root.Canvas);
            Assert.IsTrue(root.Canvas.enabled);
            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, root.Canvas.renderMode);
            Assert.IsNotNull(root.CanvasScaler);
            Assert.AreEqual(CanvasScaler.ScaleMode.ScaleWithScreenSize, root.CanvasScaler.uiScaleMode);
            Assert.AreEqual(new Vector2(1920f, 1080f), root.CanvasScaler.referenceResolution);
            Assert.IsNotNull(root.GraphicRaycaster);
            Assert.IsTrue(root.GraphicRaycaster.enabled);
            Assert.IsNotNull(root.InputBridge);
            Assert.IsTrue(root.InputBridge.enabled);
            Assert.IsTrue(root.InputBridge.IsBound);
            Assert.IsNotNull(root.DevButtonObject);
            Assert.IsTrue(root.DevButtonObject.activeInHierarchy);
            Assert.IsNotNull(root.ControlCenterPanel);
            Assert.IsFalse(root.ControlCenterPanel.activeSelf);
            AssertControlCenterUsesCenteredNinetyPercentLayout(root.ControlCenterRect);
            AssertDevButtonAnchoredBottomLeft(root.DevButtonRect);
            AssertSemanticMapHasRequiredComponents(root.MinimapGraphic);
            AssertSemanticMapHasRequiredComponents(root.BigMapGraphic);
            AssertMinimapAnchoredBottomRight(root.MinimapRect);
            AssertValidEventSystem();
            Assert.IsFalse(service.IsVisible);
        }

        [UnityTest]
        public IEnumerator DevelopmentUiServiceTogglesControlCenterAndBigMap()
        {
            var go = new GameObject("development-ui-toggle-bootstrap");
            LwsApplicationBootstrap bootstrap = go.AddComponent<LwsApplicationBootstrap>();

            yield return null;

            Assert.IsTrue(bootstrap.Registry.TryGet(out ILwsDevelopmentUiService service));
            service.EnsureRuntime();
            yield return null;

            Button devButton = service.RuntimeRoot.DevButtonObject.GetComponent<Button>();
            Assert.IsNotNull(devButton);
            devButton.onClick.Invoke();
            yield return null;

            Assert.IsTrue(service.IsVisible);
            Assert.IsFalse(service.RuntimeRoot.DevButtonObject.activeSelf);
            service.Hide();
            yield return null;

            Assert.IsFalse(service.IsVisible);
            Assert.IsTrue(service.RuntimeRoot.DevButtonObject.activeSelf);

            service.Show();
            yield return null;

            Assert.IsTrue(service.IsVisible);
            Assert.IsFalse(service.RuntimeRoot.DevButtonObject.activeSelf);
            service.OpenTab(LwsDevelopmentUiTab.GpsNavigation);
            Assert.AreEqual(LwsDevelopmentUiTab.GpsNavigation, service.ActiveTab);

            service.ShowBigMap();
            yield return null;

            Assert.IsTrue(service.IsBigMapVisible);
            Assert.IsFalse(service.RuntimeRoot.DevButtonObject.activeSelf);
            Assert.AreEqual(0f, Time.timeScale);

            service.HideBigMap();
            yield return null;

            Assert.IsFalse(service.IsBigMapVisible);
            Assert.IsFalse(service.RuntimeRoot.DevButtonObject.activeSelf);
            Assert.AreEqual(1f, Time.timeScale);

            service.Toggle();
            yield return null;
            Assert.IsFalse(service.IsVisible);
            Assert.IsTrue(service.RuntimeRoot.DevButtonObject.activeSelf);

            service.Toggle();
            yield return null;
            Assert.IsTrue(service.IsVisible);

            service.Hide();
            Assert.IsFalse(service.IsVisible);
            Assert.IsTrue(service.RuntimeRoot.DevButtonObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator TransmissionTabCreatesDevelopmentSelectorControlsWithoutController()
        {
            var go = new GameObject("development-ui-transmission-bootstrap");
            LwsApplicationBootstrap bootstrap = go.AddComponent<LwsApplicationBootstrap>();

            yield return null;

            Assert.IsTrue(bootstrap.Registry.TryGet(out ILwsDevelopmentUiService service));
            service.OpenTab(LwsDevelopmentUiTab.Transmission);
            yield return null;

            Button[] buttons = service.RuntimeRoot.GetComponentsInChildren<Button>(true);
            Assert.IsTrue(buttons.Any(button => button.name == "Automatic Selector D"));
            Assert.IsTrue(buttons.Any(button => button.name == "Automatic Selector N"));
            Assert.IsTrue(buttons.Any(button => button.name == "Automatic Selector R"));
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

        private static void AssertDevButtonAnchoredBottomLeft(RectTransform devButton)
        {
            Assert.IsNotNull(devButton);
            Assert.AreEqual(new Vector2(0f, 0f), devButton.anchorMin);
            Assert.AreEqual(new Vector2(0f, 0f), devButton.anchorMax);
            Assert.AreEqual(new Vector2(0f, 0f), devButton.pivot);
            Assert.AreEqual(82f, devButton.sizeDelta.x, 0.01f);
            Assert.AreEqual(42f, devButton.sizeDelta.y, 0.01f);
            Assert.AreEqual(32f, devButton.anchoredPosition.x, 0.01f);
            Assert.AreEqual(32f, devButton.anchoredPosition.y, 0.01f);
            Assert.IsNotNull(devButton.GetComponent<Image>());
            Assert.IsTrue(devButton.GetComponent<Image>().raycastTarget);
        }

        private static void AssertControlCenterUsesCenteredNinetyPercentLayout(RectTransform controlCenter)
        {
            Assert.IsNotNull(controlCenter);
            Assert.AreEqual(new Vector2(0.05f, 0.05f), controlCenter.anchorMin);
            Assert.AreEqual(new Vector2(0.95f, 0.95f), controlCenter.anchorMax);
            Assert.AreEqual(Vector2.zero, controlCenter.offsetMin);
            Assert.AreEqual(Vector2.zero, controlCenter.offsetMax);
            Assert.AreEqual(Vector3.one, controlCenter.localScale);
        }

        private static void AssertValidEventSystem()
        {
            EventSystem[] eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None)
                .Where(eventSystem => eventSystem.gameObject.activeInHierarchy)
                .ToArray();
            Assert.AreEqual(1, eventSystems.Length);
            BaseInputModule[] modules = eventSystems[0].GetComponents<BaseInputModule>();
            Assert.IsTrue(modules.Any(module =>
                module.enabled &&
                module.GetType().FullName == "UnityEngine.InputSystem.UI.InputSystemUIInputModule"));
        }
    }
}
