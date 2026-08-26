using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace LWS.InterstateHauler
{
    [DisallowMultipleComponent]
    public sealed class LwsInterstateCorridorRuntimeBuilder : MonoBehaviour
    {
        private const string GraphId = "IH_TEST_INTERSTATE_CORRIDOR_009";
        private const string CorridorId = "IH_TEST_I000";
        private const float MetersPerMile = 1609.344f;

        [Header("Generation")]
        [SerializeField] private bool generateOnStart = true;
        [SerializeField] private bool useEasyRoadsRuntimeApi = true;
        [SerializeField] private bool buildFallbackMeshesIfEasyRoadsUnavailable = true;
        [SerializeField] private bool restoreEasyRoadsOnDestroy = true;
        [SerializeField] private LwsRoadGraphProvider roadGraphProvider;

        [Header("Highway Dimensions")]
        [SerializeField] private float laneWidthMeters = 3.7f;
        [SerializeField] private float rightShoulderWidthMeters = 3.0f;
        [SerializeField] private float leftShoulderWidthMeters = 1.2f;
        [SerializeField] private float medianWidthMeters = 14.0f;
        [SerializeField] private float roadSurfaceY = 0.55f;
        [SerializeField] private float sampleSpacingMeters = 50f;

        [Header("Validation")]
        [SerializeField] private bool createServiceArea = true;
        [SerializeField] private bool createDepotJobBoardValidation = true;
        [SerializeField] private bool createLaneDebugLines = true;
        [SerializeField] private bool showDebugPanel = true;
        [SerializeField] private bool createUtsTrafficValidation = true;
        [SerializeField] private bool createNavigationValidation = true;
        [SerializeField] private bool createWeatherValidation = true;
        [SerializeField] private bool createRoadConditionValidation = true;
        [SerializeField] private GameObject weatherMakerPrefab;
        [SerializeField] private LwsUtsTrafficProfile validationTrafficProfile;
        [SerializeField] private GameObject[] validationTrafficPrefabs;

        private GameObject _generatedRoot;
        private object _easyRoadsNetwork;

        public LwsRoadGraph LastGraph { get; private set; }
        public bool LastEasyRoadsBuildSucceeded { get; private set; }
        public string LastBuildMessage { get; private set; } = "Not built.";
        public float CarriagewayWidthMeters => laneWidthMeters * 2f + rightShoulderWidthMeters + leftShoulderWidthMeters;
        public float CorridorLengthMeters => LastGraph != null && LastGraph.edges.Count > 0 ? LastGraph.edges[0].distanceMeters : 0f;
        public float CorridorLengthMiles => CorridorLengthMeters / MetersPerMile;
        public bool CreateWeatherValidationEnabled => createWeatherValidation;
        public bool WeatherMakerPrefabConfigured => weatherMakerPrefab != null;

        private void Start()
        {
            if (generateOnStart)
            {
                BuildCorridor();
            }
        }

        public void BuildCorridor()
        {
            if (_generatedRoot != null)
            {
                return;
            }

            _generatedRoot = new GameObject("IH_TEST_I000 Generated Interstate Corridor");
            _generatedRoot.transform.SetParent(transform, false);

            LastGraph = CreateDefaultGraph();
            EnsureRoadGraphProvider();
            roadGraphProvider.SetGraph(LastGraph, true);

            if (createServiceArea)
            {
                BuildServiceArea(_generatedRoot.transform);
            }

            if (createDepotJobBoardValidation)
            {
                BuildDepotJobBoardValidation(_generatedRoot.transform);
            }

            string easyRoadsMessage = "EasyRoads runtime generation was disabled.";
            LastEasyRoadsBuildSucceeded = useEasyRoadsRuntimeApi && TryBuildEasyRoadsCorridor(_generatedRoot.transform, out easyRoadsMessage);
            LastBuildMessage = easyRoadsMessage;

            if (!LastEasyRoadsBuildSucceeded && buildFallbackMeshesIfEasyRoadsUnavailable)
            {
                BuildFallbackRoadMeshes(_generatedRoot.transform, LastGraph);
                LastBuildMessage += " Fallback LWS ribbon meshes were generated so the validation scene remains drivable.";
            }

            if (createLaneDebugLines)
            {
                BuildLaneDebugLines(_generatedRoot.transform, LastGraph);
            }

            LwsInterstateRoadsideBuilder.BuildFromRoadGraph(
                _generatedRoot.transform,
                GraphId,
                LastGraph,
                LwsInterstateCrossSectionProfile.CreateValidationDefault());

            if (showDebugPanel && roadGraphProvider.GetComponent<LwsRoadGraphDebugPanel>() == null)
            {
                roadGraphProvider.gameObject.AddComponent<LwsRoadGraphDebugPanel>();
            }

            if (createUtsTrafficValidation)
            {
                BuildUtsTrafficValidation();
            }

            if (createNavigationValidation)
            {
                BuildNavigationValidation();
            }

            if (createWeatherValidation)
            {
                BuildWeatherValidation();
            }

            if (createRoadConditionValidation)
            {
                BuildRoadConditionValidation();
            }

            Debug.Log($"Interstate corridor generated. EasyRoads: {LastEasyRoadsBuildSucceeded}. {LastBuildMessage}", this);
        }

        private void OnDestroy()
        {
            if (!restoreEasyRoadsOnDestroy || _easyRoadsNetwork == null)
            {
                return;
            }

            InvokeOptional(_easyRoadsNetwork, "RestoreRoadNetwork");
            _easyRoadsNetwork = null;
        }

        private void EnsureRoadGraphProvider()
        {
            if (roadGraphProvider != null)
            {
                return;
            }

            roadGraphProvider = GetComponent<LwsRoadGraphProvider>();
            if (roadGraphProvider == null)
            {
                roadGraphProvider = gameObject.AddComponent<LwsRoadGraphProvider>();
            }
        }

        private void BuildUtsTrafficValidation()
        {
            if (roadGraphProvider == null || LastGraph == null)
            {
                return;
            }

            LwsUtsHighwayTrafficController traffic = roadGraphProvider.GetComponent<LwsUtsHighwayTrafficController>();
            if (traffic == null)
            {
                traffic = roadGraphProvider.gameObject.AddComponent<LwsUtsHighwayTrafficController>();
            }

            traffic.ConfigureValidationProfile(validationTrafficProfile, validationTrafficPrefabs);
            traffic.InitializeFromGraph(roadGraphProvider, LastGraph);
        }

        private void BuildNavigationValidation()
        {
            if (roadGraphProvider == null)
            {
                return;
            }

            if (roadGraphProvider.GetComponent<LwsNavigationDebugPanel>() == null)
            {
                roadGraphProvider.gameObject.AddComponent<LwsNavigationDebugPanel>();
            }

            if (roadGraphProvider.GetComponent<LwsGpsSettingsPanel>() == null)
            {
                roadGraphProvider.gameObject.AddComponent<LwsGpsSettingsPanel>();
            }
        }

        private void BuildWeatherValidation()
        {
            if (roadGraphProvider == null)
            {
                return;
            }

            if (roadGraphProvider.GetComponent<LwsWeatherMakerAdapter>() == null)
            {
                LwsWeatherMakerAdapter adapter = roadGraphProvider.gameObject.AddComponent<LwsWeatherMakerAdapter>();
                adapter.ConfigureWeatherMakerPrefab(weatherMakerPrefab);
            }
            else
            {
                roadGraphProvider.GetComponent<LwsWeatherMakerAdapter>().ConfigureWeatherMakerPrefab(weatherMakerPrefab);
            }

            if (roadGraphProvider.GetComponent<LwsWeatherDebugPanel>() == null)
            {
                roadGraphProvider.gameObject.AddComponent<LwsWeatherDebugPanel>();
            }
        }

        private void BuildRoadConditionValidation()
        {
            if (roadGraphProvider == null)
            {
                return;
            }

            if (roadGraphProvider.GetComponent<LwsRoadConditionRuntimeController>() == null)
            {
                roadGraphProvider.gameObject.AddComponent<LwsRoadConditionRuntimeController>();
            }

            if (roadGraphProvider.GetComponent<LwsWeatheradeAdapter>() == null)
            {
                roadGraphProvider.gameObject.AddComponent<LwsWeatheradeAdapter>();
            }

            if (roadGraphProvider.GetComponent<LwsNwhRoadConditionAdapter>() == null)
            {
                roadGraphProvider.gameObject.AddComponent<LwsNwhRoadConditionAdapter>();
            }

            if (roadGraphProvider.GetComponent<LwsRoadConditionDebugPanel>() == null)
            {
                roadGraphProvider.gameObject.AddComponent<LwsRoadConditionDebugPanel>();
            }
        }

        private bool TryBuildEasyRoadsCorridor(Transform parent, out string message)
        {
            Type roadNetworkType = ResolveType("EasyRoads3Dv3.ERRoadNetwork");
            Type roadTypeType = ResolveType("EasyRoads3Dv3.ERRoadType");
            if (roadNetworkType == null || roadTypeType == null)
            {
                message = "EasyRoads runtime API types were not found.";
                return false;
            }

            try
            {
                _easyRoadsNetwork = Activator.CreateInstance(roadNetworkType);
                Material roadMaterial = LwsWeatheradeMaterialFactory.CreateRoadSurfaceMaterial("IH Runtime Asphalt Weatherade", new Color(0.07f, 0.07f, 0.065f, 1f));
                object mainRoadType = CreateEasyRoadsRoadType(roadTypeType, "IH Validation Interstate", CarriagewayWidthMeters, roadMaterial);
                object rampRoadType = CreateEasyRoadsRoadType(roadTypeType, "IH Validation Ramp", 7.5f, roadMaterial);

                Vector3[] centerline = CreateReferenceCenterline();
                float carriagewayOffset = medianWidthMeters * 0.5f + CarriagewayWidthMeters * 0.5f;

                CreateEasyRoad(_easyRoadsNetwork, mainRoadType, "IH_TEST_I000_NB_MAIN", OffsetPolyline(centerline, carriagewayOffset));
                CreateEasyRoad(_easyRoadsNetwork, mainRoadType, "IH_TEST_I000_SB_MAIN", Reverse(OffsetPolyline(centerline, -carriagewayOffset)));
                CreateEasyRoad(
                    _easyRoadsNetwork,
                    rampRoadType,
                    "IH_TEST_I000_NB_ENTRY_RAMP",
                    new[]
                    {
                        new Vector3(-35f, roadSurfaceY, -110f),
                        new Vector3(-18f, roadSurfaceY, -40f),
                        new Vector3(3f, roadSurfaceY, 30f),
                        new Vector3(carriagewayOffset, roadSurfaceY, 135f)
                    });
                CreateEasyRoad(
                    _easyRoadsNetwork,
                    rampRoadType,
                    "IH_TEST_I000_TURNAROUND_CROSSOVER",
                    new[]
                    {
                        OffsetPolyline(centerline, carriagewayOffset)[centerline.Length - 1],
                        new Vector3(0f, centerline[centerline.Length - 1].y, centerline[centerline.Length - 1].z + 55f),
                        OffsetPolyline(centerline, -carriagewayOffset)[centerline.Length - 1]
                    });

                InvokeOptional(_easyRoadsNetwork, "HideWhiteSurfaces", true);
                if (!InvokeOptional(_easyRoadsNetwork, "BuildRoadNetwork", false, false, false))
                {
                    InvokeOptional(_easyRoadsNetwork, "BuildRoadNetwork");
                }

                AttachRoadSurfaceToGeneratedRoadObjects(parent);
                message = "EasyRoads runtime API generated paired carriageways, entry ramp, crossover, and mesh colliders.";
                return true;
            }
            catch (Exception ex)
            {
                message = $"EasyRoads runtime build failed: {ex.GetType().Name}: {ex.Message}";
                return false;
            }
        }

        private object CreateEasyRoadsRoadType(Type roadTypeType, string typeName, float roadWidth, Material material)
        {
            object roadType = Activator.CreateInstance(roadTypeType);
            SetMember(roadType, "roadTypeName", typeName);
            SetMember(roadType, "roadWidth", roadWidth);
            SetMember(roadType, "roadMaterial", material);
            SetMember(roadType, "layer", 0);
            SetMember(roadType, "tag", "Untagged");
            SetMember(roadType, "hasMeshCollider", true);
            SetMember(roadType, "isSideObject", false);
            return roadType;
        }

        private object CreateEasyRoad(object roadNetwork, object roadType, string roadName, Vector3[] markers)
        {
            Type networkType = roadNetwork.GetType();
            MethodInfo createRoad = networkType.GetMethod(
                "CreateRoad",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new[] { typeof(string), roadType.GetType(), typeof(Vector3[]) },
                null);

            if (createRoad == null)
            {
                throw new MissingMethodException(networkType.FullName, "CreateRoad(string, ERRoadType, Vector3[])");
            }

            object road = createRoad.Invoke(roadNetwork, new object[] { roadName, roadType, markers });
            InvokeOptional(road, "SetResolution", 8f);
            InvokeOptional(road, "SetMeshCollider", true);
            InvokeOptional(road, "SetTerrainDeformation", false);
            InvokeOptional(road, "SnapToTerrain", false);
            return road;
        }

        private void AttachRoadSurfaceToGeneratedRoadObjects(Transform parent)
        {
            MeshCollider[] colliders = UnityEngine.Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None);
            for (int i = 0; i < colliders.Length; i++)
            {
                GameObject roadObject = colliders[i].gameObject;
                if (!roadObject.name.StartsWith("IH_TEST_I000", StringComparison.Ordinal))
                {
                    continue;
                }

                if (roadObject.GetComponent<LwsRoadSurface>() == null)
                {
                    LwsRoadSurface surface = roadObject.AddComponent<LwsRoadSurface>();
                    surface.Configure(CorridorId, roadObject.name, LwsRoadSurfaceType.AsphaltInterstate, "Dry interstate asphalt");
                }

                if (roadObject.transform.parent == null)
                {
                    roadObject.transform.SetParent(parent, true);
                }
            }
        }

        private void BuildServiceArea(Transform parent)
        {
            Material asphalt = LwsWeatheradeMaterialFactory.CreateRoadSurfaceMaterial("IH Service Asphalt Weatherade", new Color(0.09f, 0.09f, 0.085f, 1f));
            Material shoulder = LwsWeatheradeMaterialFactory.CreateRoadSurfaceMaterial("IH Shoulder Concrete Weatherade", new Color(0.31f, 0.31f, 0.29f, 1f));
            CreatePavedRect(parent, "IH_TEST_I000 Service And Trailer Pickup Area", new Vector3(-38f, roadSurfaceY, -95f), new Vector2(120f, 120f), asphalt, "IH_TEST_I000_SERVICE");
            CreatePavedRect(parent, "IH_TEST_I000 Turnaround Apron", new Vector3(0f, roadSurfaceY, 3420f), new Vector2(110f, 95f), asphalt, "IH_TEST_I000_TURNAROUND");
            CreatePavedRect(parent, "IH_TEST_I000 Start Shoulder Pad", new Vector3(34f, roadSurfaceY, 45f), new Vector2(26f, 170f), shoulder, "IH_TEST_I000_SHOULDER_START");
        }

        private void BuildDepotJobBoardValidation(Transform parent)
        {
            LwsJobCatalog catalog = Resources.Load<LwsJobCatalog>(LwsJobCatalogService.ValidationCatalogResourcePath);
            LwsDepotDefinition depotDefinition = null;
            if (catalog != null)
            {
                foreach (LwsDepotDefinition candidate in catalog.DepotDefinitions)
                {
                    if (candidate != null && string.Equals(candidate.StableDepotId, "depot.validation.interstate-corridor", StringComparison.Ordinal))
                    {
                        depotDefinition = candidate;
                        break;
                    }
                }
            }

            if (depotDefinition == null)
            {
                Debug.LogWarning("Prompt 020 validation depot definition was not found in the authored job catalog.", this);
                return;
            }

            GameObject depotRoot = new GameObject("IH Validation Depot Runtime");
            depotRoot.transform.SetParent(parent, false);
            depotRoot.transform.position = new Vector3(-38f, roadSurfaceY, -95f);

            BoxCollider zone = depotRoot.AddComponent<BoxCollider>();
            zone.isTrigger = true;
            zone.center = new Vector3(0f, 4f, 0f);
            zone.size = new Vector3(128f, 8f, 128f);

            GameObject terminal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            terminal.name = "IH Validation Job Board Terminal";
            terminal.transform.SetParent(depotRoot.transform, false);
            terminal.transform.localPosition = new Vector3(-34f, 1.2f, -33f);
            terminal.transform.localRotation = Quaternion.Euler(0f, 24f, 0f);
            terminal.transform.localScale = new Vector3(3.2f, 2.4f, 1.0f);
            Renderer terminalRenderer = terminal.GetComponent<Renderer>();
            if (terminalRenderer != null)
            {
                terminalRenderer.sharedMaterial = CreateRuntimeMaterial("IH Validation Job Board Terminal", new Color(0.11f, 0.15f, 0.16f, 1f));
            }

            GameObject sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sign.name = "IH Validation Job Board Sign";
            sign.transform.SetParent(depotRoot.transform, false);
            sign.transform.localPosition = new Vector3(-34f, 3.0f, -32.45f);
            sign.transform.localRotation = terminal.transform.localRotation;
            sign.transform.localScale = new Vector3(4.4f, 1.0f, 0.18f);
            Renderer signRenderer = sign.GetComponent<Renderer>();
            if (signRenderer != null)
            {
                signRenderer.sharedMaterial = CreateRuntimeMaterial("IH Validation Job Board Sign", new Color(0.82f, 0.70f, 0.32f, 1f));
            }

            GameObject label = new GameObject("IH Validation Job Board Label");
            label.transform.SetParent(sign.transform, false);
            label.transform.localPosition = new Vector3(0f, 0f, -0.62f);
            label.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            label.transform.localScale = new Vector3(0.22f, 0.22f, 0.22f);
            TextMesh text = label.AddComponent<TextMesh>();
            text.text = "JOB BOARD";
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.characterSize = 1f;
            text.fontSize = 42;
            text.color = Color.black;

            LwsDepotRuntime runtime = depotRoot.AddComponent<LwsDepotRuntime>();
            runtime.Configure(depotDefinition, zone, terminal.transform, 12f);

            LwsJobBoardTerminal boardTerminal = terminal.AddComponent<LwsJobBoardTerminal>();
            boardTerminal.Configure(runtime, terminal.transform, 12f, 1f);
            LwsJobBoardPresenter.EnsureScenePresenter();
        }
        private void CreatePavedRect(Transform parent, string name, Vector3 center, Vector2 size, Material material, string segmentId)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = center;

            Mesh mesh = new Mesh { name = $"{name} Mesh" };
            float x = size.x * 0.5f;
            float z = size.y * 0.5f;
            mesh.vertices = new[]
            {
                new Vector3(-x, 0f, -z),
                new Vector3(-x, 0f, z),
                new Vector3(x, 0f, z),
                new Vector3(x, 0f, -z)
            };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 0f)
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
            go.AddComponent<LwsRoadSurface>().Configure(CorridorId, segmentId, LwsRoadSurfaceType.AsphaltInterstate, "Dry interstate asphalt");
        }

        private void BuildFallbackRoadMeshes(Transform parent, LwsRoadGraph graph)
        {
            if (graph == null || graph.edges == null)
            {
                return;
            }

            Material asphalt = LwsWeatheradeMaterialFactory.CreateRoadSurfaceMaterial("IH Fallback Asphalt Weatherade", new Color(0.065f, 0.065f, 0.06f, 1f));
            foreach (LwsRoadEdge edge in graph.edges)
            {
                if (edge == null || edge.samples == null || edge.samples.Count < 2)
                {
                    continue;
                }

                CreateRoadRibbon(parent, edge, asphalt);
            }
        }

        private void CreateRoadRibbon(Transform parent, LwsRoadEdge edge, Material material)
        {
            int count = edge.samples.Count;
            var vertices = new Vector3[count * 2];
            var uvs = new Vector2[count * 2];
            var triangles = new int[(count - 1) * 6];
            float width = Mathf.Max(4f, edge.laneWidthMeters * edge.laneCount + edge.leftShoulderWidthMeters + edge.rightShoulderWidthMeters);

            for (int i = 0; i < count; i++)
            {
                LwsRoadSample sample = edge.samples[i];
                Vector3 forward = sample.forward.sqrMagnitude > 0.0001f ? sample.forward.normalized : Vector3.forward;
                Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
                vertices[i * 2] = sample.position - right * width * 0.5f;
                vertices[i * 2 + 1] = sample.position + right * width * 0.5f;
                uvs[i * 2] = new Vector2(0f, sample.distanceFromStartMeters / 10f);
                uvs[i * 2 + 1] = new Vector2(1f, sample.distanceFromStartMeters / 10f);
            }

            for (int i = 0; i < count - 1; i++)
            {
                int vi = i * 2;
                int ti = i * 6;
                triangles[ti] = vi;
                triangles[ti + 1] = vi + 2;
                triangles[ti + 2] = vi + 1;
                triangles[ti + 3] = vi + 1;
                triangles[ti + 4] = vi + 2;
                triangles[ti + 5] = vi + 3;
            }

            Mesh mesh = new Mesh { name = $"{edge.segmentId} Fallback Mesh" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            GameObject go = new GameObject($"{edge.segmentId} Fallback Road");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
            go.AddComponent<LwsRoadSurface>().Configure(edge.roadId, edge.segmentId, edge.surfaceType, "Dry interstate asphalt");
        }

        private void BuildLaneDebugLines(Transform parent, LwsRoadGraph graph)
        {
            if (graph == null || graph.edges == null)
            {
                return;
            }

            Material cyan = CreateRuntimeMaterial("IH Lane Center Cyan", new Color(0.15f, 0.75f, 1f, 1f));
            Material yellow = CreateRuntimeMaterial("IH Lane Center Yellow", new Color(1f, 0.78f, 0.12f, 1f));
            foreach (LwsRoadEdge edge in graph.edges)
            {
                if (edge == null || edge.samples == null || edge.samples.Count < 2 || edge.laneCenterOffsetsMeters == null)
                {
                    continue;
                }

                for (int lane = 0; lane < edge.laneCenterOffsetsMeters.Count; lane++)
                {
                    var lineObject = new GameObject($"{edge.segmentId} Lane {lane + 1} Center Debug");
                    lineObject.transform.SetParent(parent, false);
                    LineRenderer line = lineObject.AddComponent<LineRenderer>();
                    line.sharedMaterial = edge.direction == LwsRoadDirection.Southbound ? yellow : cyan;
                    line.widthMultiplier = 0.14f;
                    line.positionCount = edge.samples.Count;
                    line.useWorldSpace = true;
                    line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    line.receiveShadows = false;

                    float laneOffset = edge.laneCenterOffsetsMeters[lane];
                    for (int i = 0; i < edge.samples.Count; i++)
                    {
                        LwsRoadSample sample = edge.samples[i];
                        Vector3 right = Vector3.Cross(Vector3.up, sample.forward.sqrMagnitude > 0.0001f ? sample.forward.normalized : Vector3.forward).normalized;
                        line.SetPosition(i, sample.position + right * laneOffset + Vector3.up * 0.07f);
                    }
                }
            }
        }

        private LwsRoadGraph CreateDefaultGraph()
        {
            Vector3[] centerline = CreateReferenceCenterline();
            float carriagewayOffset = medianWidthMeters * 0.5f + CarriagewayWidthMeters * 0.5f;
            Vector3[] northbound = OffsetPolyline(centerline, carriagewayOffset);
            Vector3[] southbound = Reverse(OffsetPolyline(centerline, -carriagewayOffset));
            Vector3[] ramp =
            {
                new Vector3(-35f, roadSurfaceY, -110f),
                new Vector3(-18f, roadSurfaceY, -40f),
                new Vector3(3f, roadSurfaceY, 30f),
                new Vector3(carriagewayOffset, roadSurfaceY, 135f)
            };
            Vector3[] crossover =
            {
                northbound[northbound.Length - 1],
                new Vector3(0f, centerline[centerline.Length - 1].y, centerline[centerline.Length - 1].z + 55f),
                OffsetPolyline(centerline, -carriagewayOffset)[centerline.Length - 1]
            };

            var graph = new LwsRoadGraph { graphId = GraphId };
            AddEdge(graph, "IH_TEST_I000_NB", "IH_TEST_I000_NB_MAIN", "IH_TEST_I000_NB_MAIN_EDGE", LwsRoadDirection.Northbound, LwsRoadClass.Interstate, 2, 65f, northbound, CarriagewayWidthMeters);
            AddEdge(graph, "IH_TEST_I000_SB", "IH_TEST_I000_SB_MAIN", "IH_TEST_I000_SB_MAIN_EDGE", LwsRoadDirection.Southbound, LwsRoadClass.Interstate, 2, 65f, southbound, CarriagewayWidthMeters);
            AddEdge(graph, "IH_TEST_I000_RAMP", "IH_TEST_I000_NB_ENTRY_RAMP", "IH_TEST_I000_NB_ENTRY_RAMP_EDGE", LwsRoadDirection.Northbound, LwsRoadClass.Ramp, 1, 35f, ramp, 7.5f);
            AddEdge(graph, "IH_TEST_I000_TURN", "IH_TEST_I000_TURNAROUND_CROSSOVER", "IH_TEST_I000_TURNAROUND_CROSSOVER_EDGE", LwsRoadDirection.Bidirectional, LwsRoadClass.Ramp, 1, 25f, crossover, 10f);
            return graph;
        }

        private void AddEdge(
            LwsRoadGraph graph,
            string roadId,
            string segmentId,
            string edgeId,
            LwsRoadDirection direction,
            LwsRoadClass roadClass,
            int laneCount,
            float speedLimitMph,
            Vector3[] polyline,
            float roadWidth)
        {
            string startNodeId = $"{segmentId}_START";
            string endNodeId = $"{segmentId}_END";
            graph.nodes.Add(new LwsRoadNode { nodeId = startNodeId, position = polyline[0], regionId = "validation", sceneChunkId = "local" });
            graph.nodes.Add(new LwsRoadNode { nodeId = endNodeId, position = polyline[polyline.Length - 1], regionId = "validation", sceneChunkId = "local" });

            var edge = new LwsRoadEdge
            {
                roadId = roadId,
                segmentId = segmentId,
                edgeId = edgeId,
                fromNodeId = startNodeId,
                toNodeId = endNodeId,
                roadClass = roadClass,
                direction = direction,
                surfaceType = LwsRoadSurfaceType.AsphaltInterstate,
                oneWay = direction != LwsRoadDirection.Bidirectional,
                distanceMeters = ComputePolylineLength(polyline),
                travelCost = ComputePolylineLength(polyline),
                speedLimitMph = speedLimitMph,
                laneCount = laneCount,
                laneWidthMeters = laneWidthMeters,
                leftShoulderWidthMeters = roadClass == LwsRoadClass.Interstate ? leftShoulderWidthMeters : 0.5f,
                rightShoulderWidthMeters = roadClass == LwsRoadClass.Interstate ? rightShoulderWidthMeters : 1.0f,
                medianWidthMeters = roadClass == LwsRoadClass.Interstate ? medianWidthMeters : 0f,
                stateOrRegionId = "validation",
                sceneChunkId = "local"
            };

            for (int i = 0; i < laneCount; i++)
            {
                float laneOffset = ((laneCount - 1) * -0.5f + i) * laneWidthMeters;
                edge.laneCenterOffsetsMeters.Add(laneOffset);
            }

            PopulateSamples(edge, polyline, roadWidth);
            graph.edges.Add(edge);
        }

        private void PopulateSamples(LwsRoadEdge edge, Vector3[] polyline, float roadWidth)
        {
            float distance = 0f;
            for (int i = 0; i < polyline.Length - 1; i++)
            {
                Vector3 start = polyline[i];
                Vector3 end = polyline[i + 1];
                Vector3 segment = end - start;
                float length = segment.magnitude;
                if (length <= 0.01f)
                {
                    continue;
                }

                int steps = Mathf.Max(1, Mathf.CeilToInt(length / Mathf.Max(5f, sampleSpacingMeters)));
                for (int step = 0; step < steps; step++)
                {
                    float t = step / (float)steps;
                    edge.samples.Add(CreateSample(edge, Vector3.Lerp(start, end, t), segment.normalized, distance + length * t, roadWidth));
                }

                distance += length;
            }

            Vector3 finalForward = (polyline[polyline.Length - 1] - polyline[polyline.Length - 2]).normalized;
            edge.samples.Add(CreateSample(edge, polyline[polyline.Length - 1], finalForward, distance, roadWidth));
        }

        private static LwsRoadSample CreateSample(LwsRoadEdge edge, Vector3 position, Vector3 forward, float distance, float roadWidth)
        {
            return new LwsRoadSample
            {
                roadId = edge.roadId,
                segmentId = edge.segmentId,
                distanceFromStartMeters = distance,
                position = position,
                forward = forward,
                up = Vector3.up,
                direction = edge.direction,
                roadWidthMeters = roadWidth,
                laneWidthMeters = edge.laneWidthMeters,
                laneCount = edge.laneCount,
                speedLimitMph = edge.speedLimitMph
            };
        }

        private Vector3[] CreateReferenceCenterline()
        {
            return new[]
            {
                new Vector3(0f, roadSurfaceY, 0f),
                new Vector3(0f, roadSurfaceY, 520f),
                new Vector3(105f, roadSurfaceY + 2f, 1080f),
                new Vector3(255f, roadSurfaceY + 10f, 1740f),
                new Vector3(170f, roadSurfaceY + 17f, 2460f),
                new Vector3(0f, roadSurfaceY + 8f, 3350f)
            };
        }

        private static Vector3[] OffsetPolyline(Vector3[] source, float xOffset)
        {
            var result = new Vector3[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                result[i] = source[i] + Vector3.right * xOffset;
            }

            return result;
        }

        private static Vector3[] Reverse(Vector3[] source)
        {
            var result = new Vector3[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                result[i] = source[source.Length - 1 - i];
            }

            return result;
        }

        private static float ComputePolylineLength(Vector3[] polyline)
        {
            float total = 0f;
            for (int i = 0; i < polyline.Length - 1; i++)
            {
                total += Vector3.Distance(polyline[i], polyline[i + 1]);
            }

            return total;
        }

        private static Material CreateRuntimeMaterial(string name, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
            var material = new Material(shader) { name = name, color = color };
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            return material;
        }

        private static Type ResolveType(string typeName)
        {
            Type type = Type.GetType(typeName) ?? Type.GetType($"{typeName}, Assembly-CSharp");
            if (type != null)
            {
                return type;
            }

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                type = assemblies[i].GetType(typeName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static bool SetMember(object target, string memberName, object value)
        {
            if (target == null)
            {
                return false;
            }

            Type type = target.GetType();
            FieldInfo field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public);
            if (field != null)
            {
                field.SetValue(target, value);
                return true;
            }

            PropertyInfo property = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public);
            if (property != null && property.CanWrite)
            {
                property.SetValue(target, value);
                return true;
            }

            return false;
        }

        private static bool InvokeOptional(object target, string methodName, params object[] args)
        {
            if (target == null)
            {
                return false;
            }

            MethodInfo method = FindMethod(target.GetType(), methodName, args);
            if (method == null)
            {
                return false;
            }

            method.Invoke(target, args);
            return true;
        }

        private static MethodInfo FindMethod(Type type, string methodName, object[] args)
        {
            MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method.Name != methodName)
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length != args.Length)
                {
                    continue;
                }

                bool match = true;
                for (int p = 0; p < parameters.Length; p++)
                {
                    if (args[p] != null && !parameters[p].ParameterType.IsInstanceOfType(args[p]))
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                {
                    return method;
                }
            }

            return null;
        }
    }
}
