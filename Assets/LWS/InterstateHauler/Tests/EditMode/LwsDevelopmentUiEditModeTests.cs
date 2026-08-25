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
            Assert.IsTrue(registry.TryGet(out ILwsCameraPresentationService cameraPresentationService));
            Assert.AreEqual("lws.camera.presentation", cameraPresentationService.ServiceId);
            Assert.AreEqual(1, registry.Registrations.Count(r => r.ServiceType == typeof(ILwsCameraPresentationService)));
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
        public void SemanticMapTwoPointPolylineCreatesVisibleRoadTrianglesWithoutRoute()
        {
            var go = new GameObject("semantic-map-two-point-test");
            try
            {
                LwsSemanticGpsMapGraphic graphic = go.AddComponent<LwsSemanticGpsMapGraphic>();
                LwsRoadGraph graph = CreateTwoPointRoadGraph();

                graphic.SetMapData(graph, null, LwsWorldPositionD.FromVector3(Vector3.zero), Vector3.forward, true, 1200f, Vector2.zero, 0.4f);

                Assert.IsTrue(graphic.GraphBound);
                Assert.IsTrue(graphic.HasRoadPresentation);
                Assert.AreEqual("IH_TEST_MINIMAP_GRAPH", graphic.GraphId);
                Assert.AreEqual(1, graphic.RoadCount);
                Assert.AreEqual(1, graphic.EdgeCount);
                Assert.AreEqual(2, graphic.CenterlineSampleCount);
                Assert.AreEqual(0, graphic.RoutePointCount);
                Assert.Greater(graphic.BaseRoadVertexCount, 0);
                Assert.Greater(graphic.BaseRoadTriangleCount, 0);
                Assert.AreEqual(0, graphic.RouteVertexCount);
                Assert.AreEqual(0, graphic.RouteTriangleCount);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void SemanticMapRebuildsWhenGraphLateBindsFromNullToValid()
        {
            var go = new GameObject("semantic-map-late-bind-test");
            try
            {
                LwsSemanticGpsMapGraphic graphic = go.AddComponent<LwsSemanticGpsMapGraphic>();

                graphic.SetMapData(null, null, LwsWorldPositionD.FromVector3(Vector3.zero), Vector3.forward, true, 1200f, Vector2.zero, 0.4f);

                Assert.IsFalse(graphic.GraphBound);
                Assert.AreEqual(0, graphic.BaseRoadTriangleCount);

                graphic.SetMapData(CreateTwoPointRoadGraph(), null, LwsWorldPositionD.FromVector3(Vector3.zero), Vector3.forward, true, 1200f, Vector2.zero, 0.4f);

                Assert.IsTrue(graphic.GraphBound);
                Assert.Greater(graphic.BaseRoadTriangleCount, 0);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void SemanticMapRouteGeometryIsDistinctFromBaseRoadGeometry()
        {
            var go = new GameObject("semantic-map-route-test");
            try
            {
                LwsSemanticGpsMapGraphic graphic = go.AddComponent<LwsSemanticGpsMapGraphic>();

                graphic.SetMapData(
                    CreateTwoPointRoadGraph(),
                    CreateTwoPointRoute(),
                    LwsWorldPositionD.FromVector3(Vector3.zero),
                    Vector3.forward,
                    true,
                    1200f,
                    Vector2.zero,
                    0.4f);

                Assert.IsTrue(graphic.HasRoadPresentation);
                Assert.IsTrue(graphic.HasRoutePresentation);
                Assert.AreEqual(2, graphic.RoutePointCount);
                Assert.Greater(graphic.RouteVertexCount, 0);
                Assert.Greater(graphic.RouteTriangleCount, 0);
                Assert.Greater(graphic.RouteLineWidth, graphic.RoadLineWidth);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void SemanticMapProjectsPlayerToConfiguredViewportCenter()
        {
            var go = new GameObject("semantic-map-projection-test");
            try
            {
                LwsSemanticGpsMapGraphic graphic = go.AddComponent<LwsSemanticGpsMapGraphic>();
                graphic.SetMapData(CreateTwoPointRoadGraph(), null, LwsWorldPositionD.FromVector3(Vector3.zero), Vector3.forward, true, 1200f, Vector2.zero, 0.4f);

                Vector2 playerMap = graphic.ProjectGlobalPointToMap(Vector3.zero);
                Vector2 aheadMap = graphic.ProjectGlobalPointToMap(new Vector3(0f, 0f, 300f));

                Assert.AreEqual(0.5f, playerMap.x, 0.0001f);
                Assert.AreEqual(0.4f, playerMap.y, 0.0001f);
                Assert.AreEqual(0.5f, aheadMap.x, 0.0001f);
                Assert.Greater(aheadMap.y, playerMap.y);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void DevelopmentUiSourceAnchorsMinimapBottomRight()
        {
            string root = File.ReadAllText("Assets/LWS/InterstateHauler/UI/Development/LwsDevelopmentUiRoot.cs");

            StringAssert.Contains("CreateFixedPanel(", root);
            StringAssert.Contains("\"GPS Minimap\"", root);
            StringAssert.Contains("new Vector2(1f, 0f)", root);
            StringAssert.Contains("new Vector2(-MinimapPanelMarginPixels, MinimapPanelMarginPixels)", root);
            StringAssert.Contains("MinimapMetersVisible", root);
        }

        [Test]
        public void CameraPresentationPolicyHidesHudMinimapOnlyInCockpitByDefault()
        {
            var service = new LwsCameraPresentationService();
            service.Initialize(new LwsServiceContext(new LwsServiceRegistry()));

            Assert.AreEqual(LwsGpsPresentationPolicy.Auto, service.GpsPresentationPolicy);

            service.SetCameraMode(LwsVehicleCameraMode.Cockpit, "Cab Camera");
            Assert.IsFalse(service.ShouldShowHudMinimap);

            service.SetCameraMode(LwsVehicleCameraMode.Exterior, "Chase Camera");
            Assert.IsTrue(service.ShouldShowHudMinimap);

            service.SetGpsPresentationPolicy(LwsGpsPresentationPolicy.ForceHudMinimapOff);
            Assert.IsFalse(service.ShouldShowHudMinimap);

            service.SetGpsPresentationPolicy(LwsGpsPresentationPolicy.ForceHudMinimapOn);
            Assert.IsTrue(service.ShouldShowHudMinimap);
        }

        [Test]
        public void CabGpsBindsWorldSpaceCanvasToStableGpsAnchorAndSemanticMap()
        {
            var truck = new GameObject("cab-gps-truck");
            try
            {
                var cab = new GameObject("Cab");
                cab.transform.SetParent(truck.transform, false);
                LwsCabAccessoryAnchorRegistry anchors = truck.AddComponent<LwsCabAccessoryAnchorRegistry>();
                anchors.EnsureInitialized();

                LwsCabGpsController gps = truck.AddComponent<LwsCabGpsController>();
                gps.BindPhysicalScreen();

                Assert.IsTrue(gps.PhysicalGpsBound);
                Assert.IsNotNull(gps.PhysicalCanvas);
                Assert.AreEqual(RenderMode.WorldSpace, gps.PhysicalCanvas.renderMode);
                Assert.IsNotNull(gps.GpsMount);
                Assert.AreEqual(LwsCabGpsController.DefaultGpsAnchorId, gps.GpsMount.name);
                Assert.AreSame(gps.GpsMount, gps.PhysicalCanvas.transform.parent);
                Assert.IsNotNull(gps.SemanticMapGraphic);
                Assert.IsNotNull(gps.SemanticMapGraphic.GetComponent<CanvasRenderer>());
                Assert.AreEqual(0.00042f, gps.ScreenScale, 0.00001f);
                Assert.AreEqual(0.27f, gps.ApproximatePhysicalSizeMeters.x, 0.01f);
                Assert.AreEqual(0.17f, gps.ApproximatePhysicalSizeMeters.y, 0.01f);
                Assert.AreEqual(180f, gps.LocalScreenEulerAngles.y, 0.01f);
            }
            finally
            {
                Object.DestroyImmediate(truck);
            }
        }

        [Test]
        public void DevelopmentUiSourceDefinesVisibleRuntimeHostAndDevButton()
        {
            string root = File.ReadAllText("Assets/LWS/InterstateHauler/UI/Development/LwsDevelopmentUiRoot.cs");

            StringAssert.Contains("\"Development Canvas\"", root);
            StringAssert.Contains("\"Persistent HUD Layer\"", root);
            StringAssert.Contains("\"Modal Overlay Layer\"", root);
            StringAssert.Contains("\"DEV Button\"", root);
            StringAssert.Contains("\"[ DEV ]\"", root);
            StringAssert.Contains("new Vector2(0.05f, 0.05f)", root);
            StringAssert.Contains("new Vector2(0.95f, 0.95f)", root);
            StringAssert.Contains("UpdateDevButtonVisibility", root);
            StringAssert.Contains("LwsDevelopmentUiInputBridge", root);
        }

        [Test]
        public void DevelopmentUiSourceUsesInputSystemBridgeAndConfiguredEventSystem()
        {
            string root = File.ReadAllText("Assets/LWS/InterstateHauler/UI/Development/LwsDevelopmentUiRoot.cs");

            StringAssert.Contains("Keyboard.current", root);
            StringAssert.Contains("f1Key.wasPressedThisFrame", root);
            StringAssert.Contains("InputSystemUIInputModule", root);
            StringAssert.Contains("AssignDefaultActions", root);
            StringAssert.Contains("GraphicRaycaster", root);
            StringAssert.Contains("EventSystem.current", root);
        }

        [Test]
        public void DevelopmentUiSourceUsesUnitySixSafeRuntimeFont()
        {
            string root = File.ReadAllText("Assets/LWS/InterstateHauler/UI/Development/LwsDevelopmentUiRoot.cs");

            StringAssert.Contains("ResolveUiFont", root);
            StringAssert.Contains("LegacyRuntime.ttf", root);
            StringAssert.DoesNotContain("label.font = Resources.GetBuiltinResource<Font>(\"Arial.ttf\")", root);
        }

        [Test]
        public void DevelopmentUiSourceAuditsRaycastableButtonsAndDecorativeText()
        {
            string root = File.ReadAllText("Assets/LWS/InterstateHauler/UI/Development/LwsDevelopmentUiRoot.cs");

            StringAssert.Contains("image.raycastTarget = true", root);
            StringAssert.Contains("label.raycastTarget = false", root);
            StringAssert.Contains("RectMask2D", root);
        }

        [Test]
        public void DevelopmentUiSourceExposesAutomaticTransmissionSelector()
        {
            string root = File.ReadAllText("Assets/LWS/InterstateHauler/UI/Development/LwsDevelopmentUiRoot.cs");

            StringAssert.Contains("AddTransmissionSelectorRow", root);
            StringAssert.Contains("Automatic Selector D", root);
            StringAssert.Contains("Automatic Selector N", root);
            StringAssert.Contains("Automatic Selector R", root);
            StringAssert.Contains("TrySetAutomaticSelector", root);
            StringAssert.Contains("\"Transmission Test Controls\"", root);
            StringAssert.Contains("\"Transmission Shift Down\"", root);
            StringAssert.Contains("\"Transmission Shift Up\"", root);
            StringAssert.Contains("RefreshTransmissionHud", root);
            StringAssert.Contains("KeyCode.Alpha1", root);
            StringAssert.Contains("KeyCode.Alpha2", root);
            StringAssert.Contains("KeyCode.Alpha3", root);
            StringAssert.Contains("SelectedButtonColor", root);
        }

        [Test]
        public void DevelopmentUiSourceUsesOriginAwareNavigationAndCachedSemanticMap()
        {
            string root = File.ReadAllText("Assets/LWS/InterstateHauler/UI/Development/LwsDevelopmentUiRoot.cs");
            string map = File.ReadAllText("Assets/LWS/InterstateHauler/UI/Development/LwsSemanticGpsMapGraphic.cs");
            string cabGps = File.ReadAllText("Assets/LWS/InterstateHauler/Navigation/LwsCabGpsController.cs");

            StringAssert.Contains("LocalToGlobal", root);
            StringAssert.Contains("ILwsNavigationService", root);
            StringAssert.Contains("ILwsRoadGraphService", root);
            StringAssert.Contains("CurrentRoute", root);
            StringAssert.Contains("RebuildRoadCache", map);
            StringAssert.Contains("RebuildRouteCache", map);
            StringAssert.Contains("LwsRoadGraph", map);
            StringAssert.Contains("LwsRouteResult", map);
            StringAssert.Contains("[RequireComponent(typeof(CanvasRenderer))]", map);
            StringAssert.Contains("CreateSemanticMapGraphic", root);
            StringAssert.Contains("AddComponent<CanvasRenderer>()", root);
            StringAssert.Contains("LineIntersectsExpandedViewport", map);
            StringAssert.Contains("RefreshRoadLookupCache", root);
            StringAssert.Contains("LwsSemanticGpsMapGraphic", cabGps);
            StringAssert.Contains("ILwsRoadGraphService", cabGps);
            StringAssert.Contains("SetMapData", cabGps);
            StringAssert.Contains("RenderMode.WorldSpace", cabGps);
            StringAssert.Contains(LwsCabGpsController.DefaultGpsAnchorId, cabGps);
            StringAssert.Contains("ILwsCameraPresentationService", root);
            StringAssert.Contains("ShouldShowHudMinimap", root);
            StringAssert.Contains("GpsPresentationPolicy", root);
        }

        [Test]
        public void DevelopmentUiSourceUsesCompassNavigatorProPresentationWhenAvailable()
        {
            string root = File.ReadAllText("Assets/LWS/InterstateHauler/UI/Development/LwsDevelopmentUiRoot.cs");
            string adapter = File.ReadAllText("Assets/LWS/InterstateHauler/Navigation/Compass/LwsCompassNavigatorProAdapter.cs");
            string spawner = File.ReadAllText("Assets/LWS/InterstateHauler/Vehicles/LwsPlayerTruckSpawner.cs");
            string validator = File.ReadAllText("Assets/LWS/InterstateHauler/Editor/LwsProjectValidator.cs");

            StringAssert.Contains("LwsCompassNavigatorProAdapter", root);
            StringAssert.Contains("SetHudMinimapVisible", root);
            StringAssert.Contains("SetFullMapVisible", root);
            StringAssert.Contains("Compass Pro 4", root);
            Assert.IsFalse(root.Contains("GPS MAP UNAVAILABLE"));
            StringAssert.Contains("CompassNavigatorPro.CompassPro", adapter);
            StringAssert.Contains("CompassNavigatorPro.CompassProPOI", adapter);
            StringAssert.Contains("CompassProNavMeshRoute", adapter);
            StringAssert.Contains("SetRoute", adapter);
            StringAssert.Contains("miniMapFullScreenState", adapter);
            StringAssert.Contains("GlobalToLocal", adapter);
            StringAssert.Contains("AddComponent<LwsCompassNavigatorProAdapter>", spawner);
            StringAssert.Contains("Compass Navigator Pro 4 Vendor", validator);
        }

        [Test]
        public void WeatherTestPanelDefinesLargeReadableSemanticControls()
        {
            Assert.AreEqual(10, LwsWeatherTestPanel.WeatherButtonDefinitions.Count);
            Assert.IsTrue(LwsWeatherTestPanel.WeatherButtonDefinitions.Any(b => b.Label == "CLEAR" && b.PresetId == LwsWeatherPresetCatalog.ClearId));
            Assert.IsTrue(LwsWeatherTestPanel.WeatherButtonDefinitions.Any(b => b.Label == "PARTLY CLOUDY" && b.PresetId == LwsWeatherPresetCatalog.PartlyCloudyId));
            Assert.IsTrue(LwsWeatherTestPanel.WeatherButtonDefinitions.Any(b => b.Label == "HEAVY SNOW" && b.PresetId == LwsWeatherPresetCatalog.HeavySnowId));

            Assert.AreEqual(6, LwsWeatherTestPanel.TimeButtonDefinitions.Count);
            Assert.IsTrue(LwsWeatherTestPanel.TimeButtonDefinitions.Any(b => b.Label == "DAWN" && Mathf.Approximately(b.Hour, 6f)));
            Assert.IsTrue(LwsWeatherTestPanel.TimeButtonDefinitions.Any(b => b.Label == "MIDNIGHT" && Mathf.Approximately(b.Hour, 0f)));

            Assert.AreEqual(5, LwsWeatherTestPanel.RoadButtonDefinitions.Count);
            Assert.IsTrue(LwsWeatherTestPanel.RoadButtonDefinitions.Any(b => b.Label == "DRY ROAD" && b.Mode == LwsRoadConditionOverrideMode.ForceDry));
            Assert.IsTrue(LwsWeatherTestPanel.RoadButtonDefinitions.Any(b => b.Label == "PUDDLED ROAD" && b.Mode == LwsRoadConditionOverrideMode.ForceStandingWater));
            Assert.IsTrue(LwsWeatherTestPanel.RoadButtonDefinitions.Any(b => b.Label == "ICY ROAD" && b.Mode == LwsRoadConditionOverrideMode.ForceIce));

            Assert.AreEqual("F2", LwsWeatherTestPanel.ToggleHotkeyName);
            Assert.AreEqual(60f, LwsWeatherTestPanel.ButtonPreferredHeight, 0.01f);
            Assert.AreEqual(new Vector2(1920f, 1080f), LwsWeatherTestPanel.ReferenceResolution);
        }

        [Test]
        public void WeatherTestPanelSourceUsesLwsServicesAndNoVendorAuthorityOrOnGui()
        {
            string source = File.ReadAllText("Assets/LWS/InterstateHauler/UI/Development/LwsWeatherTestPanel.cs");
            string root = File.ReadAllText("Assets/LWS/InterstateHauler/UI/Development/LwsDevelopmentUiRoot.cs");
            string service = File.ReadAllText("Assets/LWS/InterstateHauler/UI/Development/LwsDevelopmentUiService.cs");

            StringAssert.Contains("f2Key.wasPressedThisFrame", source);
            StringAssert.Contains("Input.GetKeyDown(KeyCode.F2)", source);
            StringAssert.Contains("ILwsWeatherService", source);
            StringAssert.Contains("RequestWeather(presetId, 0f, true)", source);
            StringAssert.Contains("ILwsGameClockService", source);
            StringAssert.Contains("SetTimeOfDayHours", source);
            StringAssert.Contains("ILwsRoadConditionService", source);
            StringAssert.Contains("ForceCondition(mode)", source);
            StringAssert.Contains("LwsWeatheradeAdapter", source);
            StringAssert.Contains("CanvasScaler.ScaleMode.ScaleWithScreenSize", source);
            StringAssert.Contains("ButtonPreferredHeight = 60f", source);
            StringAssert.Contains("RenderMode.ScreenSpaceOverlay", source);
            StringAssert.Contains("AddComponent<LwsWeatherTestPanel>", root);
            StringAssert.Contains("ShowWeatherTestPanel", service);
            StringAssert.Contains("ToggleWeatherTestPanel", service);
            StringAssert.DoesNotContain("private void OnGUI", source);
            StringAssert.DoesNotContain("WeatherMakerScript", source);
            StringAssert.DoesNotContain("NOT_Lonely.Weatherade", source);
        }

        [Test]
        public void WeatherTestPanelFormatsWeatheradeCoverageDiagnostics()
        {
            Assert.AreEqual("ACTIVE", LwsWeatherTestPanel.ResolveCoverageActivity("RainCoverage wetness 1.00, puddles 0.75."));
            Assert.AreEqual("INACTIVE", LwsWeatherTestPanel.ResolveCoverageActivity("SnowCoverage inactive while RainCoverage owns the active Weatherade instance."));
            Assert.AreEqual("UNKNOWN", LwsWeatherTestPanel.ResolveCoverageActivity(string.Empty));
            Assert.AreEqual("SUCCESS", LwsWeatherTestPanel.ResolveCoverageUpdateStatus("Weatherade UpdateCoverageMaterials invoked for Rain."));
            Assert.AreEqual("FAILURE", LwsWeatherTestPanel.ResolveCoverageUpdateStatus("Weatherade UpdateCoverageMaterials API was not found."));
        }
        private static LwsRoadGraph CreateTwoPointRoadGraph()
        {
            var graph = new LwsRoadGraph { graphId = "IH_TEST_MINIMAP_GRAPH" };
            graph.nodes.Add(new LwsRoadNode { nodeId = "MINIMAP_START", position = new Vector3(0f, 0f, -2000f) });
            graph.nodes.Add(new LwsRoadNode { nodeId = "MINIMAP_END", position = new Vector3(0f, 0f, 2000f) });

            var edge = new LwsRoadEdge
            {
                roadId = "IH_TEST_MINIMAP_ROAD",
                segmentId = "IH_TEST_MINIMAP_SEGMENT",
                edgeId = "IH_TEST_MINIMAP_EDGE",
                fromNodeId = "MINIMAP_START",
                toNodeId = "MINIMAP_END",
                roadClass = LwsRoadClass.Interstate,
                direction = LwsRoadDirection.Northbound,
                surfaceType = LwsRoadSurfaceType.AsphaltInterstate,
                oneWay = true,
                distanceMeters = 4000f,
                travelCost = 4000f,
                speedLimitMph = 65f,
                laneCount = 2,
                laneWidthMeters = 3.7f
            };
            edge.samples.Add(CreateSample(edge, 0f, new Vector3(0f, 0f, -2000f)));
            edge.samples.Add(CreateSample(edge, 4000f, new Vector3(0f, 0f, 2000f)));
            graph.edges.Add(edge);
            return graph;
        }

        private static LwsRoadSample CreateSample(LwsRoadEdge edge, float distance, Vector3 position)
        {
            return new LwsRoadSample
            {
                roadId = edge.roadId,
                segmentId = edge.segmentId,
                distanceFromStartMeters = distance,
                position = position,
                forward = Vector3.forward,
                up = Vector3.up,
                direction = edge.direction,
                roadWidthMeters = 11f,
                laneWidthMeters = edge.laneWidthMeters,
                laneCount = edge.laneCount,
                speedLimitMph = edge.speedLimitMph
            };
        }

        private static LwsRouteResult CreateTwoPointRoute()
        {
            return new LwsRouteResult
            {
                routeId = "IH_TEST_MINIMAP_ROUTE",
                succeeded = true,
                distanceMeters = 600f,
                waypoints =
                {
                    new Vector3(0f, 0f, -300f),
                    new Vector3(0f, 0f, 300f)
                }
            };
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

