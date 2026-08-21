using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace LWS.InterstateHauler.Tests.EditMode
{
    public sealed class LwsDevelopmentUiEditModeTests
    {
        [Test]
        public void DevelopmentControlCenterDefinesRequiredUniqueTabs()
        {
            Assert.IsTrue(LwsDevelopmentUiCatalog.ValidateTabs(out string message), message);
            Assert.AreEqual(13, LwsDevelopmentUiCatalog.Tabs.Count);
            Assert.AreEqual(LwsDevelopmentUiCatalog.Tabs.Count, LwsDevelopmentUiCatalog.Tabs.Select(t => t.Id).Distinct().Count());
            Assert.IsTrue(LwsDevelopmentUiCatalog.Tabs.Any(t => t.Tab == LwsDevelopmentUiTab.GpsNavigation));
            Assert.IsTrue(LwsDevelopmentUiCatalog.Tabs.Any(t => t.Tab == LwsDevelopmentUiTab.FiftyMileTest));
        }

        [Test]
        public void DefaultRegistryContainsDevelopmentUiService()
        {
            LwsServiceRegistry registry = LwsApplicationBootstrap.CreateDefaultRegistry();

            Assert.IsTrue(registry.TryGet(out ILwsDevelopmentUiService service));
            Assert.AreEqual("lws.development.ui", service.ServiceId);
            Assert.AreEqual(1, registry.Registrations.Count(r => r.ServiceType == typeof(ILwsDevelopmentUiService)));
        }

        [Test]
        public void SemanticMapConsumesRoadGraphAndRoutePresentation()
        {
            var go = new GameObject("semantic-map-test");
            try
            {
                LwsSemanticGpsMapGraphic graphic = go.AddComponent<LwsSemanticGpsMapGraphic>();
                LwsRoadGraph graph = LwsFiftyMileHighwayModel.CreateRoadGraph();
                var planner = new LwsRoutePlanner(LwsNavigationTuning.Default());
                LwsRouteResult route = planner.PlanRoute(
                    new LwsRouteRequest
                    {
                        originNodeId = $"{LwsFiftyMileHighwayModel.EastboundSegmentId}_START",
                        destinationNodeId = $"{LwsFiftyMileHighwayModel.EastboundSegmentId}_END",
                        truckRouteRequired = true
                    },
                    graph);

                graphic.SetMapData(
                    graph,
                    route,
                    LwsWorldPositionD.FromVector3(LwsFiftyMileHighwayModel.EastboundStartPosition),
                    Vector3.forward,
                    true,
                    1200f,
                    Vector2.zero);

                Assert.IsTrue(graphic.HasRoadPresentation);
                Assert.IsTrue(graphic.HasRoutePresentation);
                Assert.IsTrue(graphic.HeadingUp);
                Assert.AreEqual(1200f, graphic.MetersVisible);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void DevelopmentUiSourceUsesOriginAwareNavigationAndCachedSemanticMap()
        {
            string root = File.ReadAllText("Assets/LWS/InterstateHauler/UI/Development/LwsDevelopmentUiRoot.cs");
            string map = File.ReadAllText("Assets/LWS/InterstateHauler/UI/Development/LwsSemanticGpsMapGraphic.cs");

            StringAssert.Contains("LocalToGlobal", root);
            StringAssert.Contains("ILwsNavigationService", root);
            StringAssert.Contains("ILwsRoadGraphService", root);
            StringAssert.Contains("CurrentRoute", root);
            StringAssert.Contains("RebuildRoadCache", map);
            StringAssert.Contains("RebuildRouteCache", map);
            StringAssert.Contains("LwsRoadGraph", map);
            StringAssert.Contains("LwsRouteResult", map);
        }

        [Test]
        public void LegacyDebugPanelsDefaultHidden()
        {
            string[] panelPaths =
            {
                "Assets/LWS/InterstateHauler/World/Streaming/LwsStreamingDebugPanel.cs",
                "Assets/LWS/InterstateHauler/Navigation/LwsNavigationDebugPanel.cs",
                "Assets/LWS/InterstateHauler/Navigation/LwsGpsSettingsPanel.cs",
                "Assets/LWS/InterstateHauler/Vehicles/Dashboard/LwsTruckDashboardDebugPanel.cs",
                "Assets/LWS/InterstateHauler/Vehicles/Transmission/Debug/Lws18SpeedTransmissionDebugPanel.cs",
                "Assets/LWS/InterstateHauler/Vehicles/LwsPlayerTruckDebugPanel.cs",
                "Assets/LWS/InterstateHauler/World/Origin/LwsFiftyMileHighwayDebugPanel.cs",
                "Assets/LWS/InterstateHauler/World/Origin/LwsFloatingOriginDebugPanel.cs",
                "Assets/LWS/InterstateHauler/Vehicles/Controls/LwsTruckControlDebugPanel.cs",
                "Assets/LWS/InterstateHauler/Input/Validation/LwsWheelCalibrationPanel.cs",
                "Assets/LWS/InterstateHauler/Input/Validation/LwsWheelDeviceDiagnosticsPanel.cs",
                "Assets/LWS/InterstateHauler/Roads/LwsRoadGraphDebugPanel.cs",
                "Assets/LWS/InterstateHauler/Input/Validation/LwsWheelInputDebugPanel.cs",
                "Assets/LWS/InterstateHauler/Weather/LwsWeatherDebugPanel.cs",
                "Assets/LWS/InterstateHauler/Roads/Conditions/LwsRoadConditionDebugPanel.cs",
                "Assets/LWS/InterstateHauler/Traffic/UTS/LwsUtsTrafficDebugPanel.cs"
            };

            foreach (string path in panelPaths)
            {
                string source = File.ReadAllText(path);
                Assert.IsFalse(source.Contains("visible = true"), path);
                Assert.IsFalse(source.Contains("showPanel = true"), path);
            }
        }
    }
}
