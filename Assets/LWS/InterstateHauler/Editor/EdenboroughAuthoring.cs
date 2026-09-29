using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using LWS.InterstateHauler;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace LWS.InterstateHauler.Editor
{
    public sealed class EdenboroughAuthoring : EditorWindow
    {
        private const string ToolFolder = "Tools/GeoImport/Edenborough";
        private const string GeneratedFolder = "Assets/LWS/InterstateHauler/Edenborough/Generated";
        private string selectedRegion = "R_0_0";
        private string[] regionNames = Array.Empty<string>();

        [MenuItem("Tools/Edenborough/Locate Source")]
        private static void LocateSource() => RunTool("locate");

        [MenuItem("Tools/Edenborough/Download Small Source")]
        private static void DownloadSource()
        {
            if (EditorUtility.DisplayDialog("Download OSM snapshot",
                "Download the configured small Edinburgh bbox to the ignored cache outside Assets?",
                "Download", "Cancel"))
                RunTool("download");
        }

        [MenuItem("Tools/Edenborough/Extract Edinburgh")]
        private static void Extract() => RunTool("extract");

        [MenuItem("Tools/Edenborough/Validate")]
        private static void Validate() => RunTool("validate");

        [MenuItem("Tools/Edenborough/Build Edenborough Graph")]
        private static void BuildGraph() => RunTool("build");

        [MenuItem("Tools/Edenborough/Preview Road Classes")]
        private static void PreviewRoadClasses() => RunTool("preview-classes");

        [MenuItem("Tools/Edenborough/Preview Regions")]
        private static void PreviewRegions() => RunTool("preview-regions");

        [MenuItem("Tools/Edenborough/Rebuild Map")]
        private static void RebuildMap() => RunTool("rebuild-map");

        [MenuItem("Tools/Edenborough/Build Selected Region")]
        private static void OpenBuilder() => GetWindow<EdenboroughAuthoring>("Edenborough Region");

        private void OnEnable() => RefreshRegions();

        private void OnGUI()
        {
            if (GUILayout.Button("Refresh Regions")) RefreshRegions();
            if (regionNames.Length == 0)
            {
                EditorGUILayout.HelpBox("Build the graph first. The source stays outside Assets.", MessageType.Info);
                return;
            }
            int index = Mathf.Max(0, Array.IndexOf(regionNames, selectedRegion));
            selectedRegion = regionNames[EditorGUILayout.Popup("Region", index, regionNames)];
            if (GUILayout.Button("Build Selected Region With EasyRoads") &&
                EditorUtility.DisplayDialog("Build Edenborough region",
                    "Rebuild the selected generated scene and its owned assets using EasyRoads?",
                    "Build", "Cancel"))
                Debug.Log(BuildRegion(selectedRegion));
        }

        private void RefreshRegions()
        {
            Dataset data = ReadDataset();
            regionNames = data?.regions?.Select(r => r.id).OrderBy(s => s).ToArray() ?? Array.Empty<string>();
            if (regionNames.Length > 0 && !regionNames.Contains(selectedRegion)) selectedRegion = regionNames[0];
            Repaint();
        }

        private static string ProjectRoot => Path.GetDirectoryName(Application.dataPath);
        private static string ToolPath => Path.Combine(ProjectRoot, ToolFolder);

        private static void RunTool(string command)
        {
            string script = Path.Combine(ToolPath, "edenborough.py");
            var start = new ProcessStartInfo("python", "\"" + script + "\" " + command)
            {
                WorkingDirectory = ProjectRoot, UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true
            };
            try
            {
                using (var process = Process.Start(start))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    if (!process.WaitForExit(300000))
                    {
                        process.Kill();
                        throw new TimeoutException("OSM editor command timed out.");
                    }
                    if (process.ExitCode != 0) throw new InvalidOperationException(error + output);
                    Debug.Log("Edenborough " + command + ": " + output);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("Edenborough " + command + " failed: " + ex.Message);
            }
        }

        private static Dataset ReadDataset()
        {
            string path = Path.Combine(ToolPath, "prototype.json");
            return File.Exists(path) ? JsonUtility.FromJson<Dataset>(File.ReadAllText(path)) : null;
        }

        [MenuItem("Tools/Edenborough/Build Prototype With EasyRoads")]
        public static void BuildPrototype() => Debug.Log(BuildRegion("R_0_0"));

        public static string BuildRegion(string regionId = "R_0_0")
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Edenborough authoring requires Edit Mode.");
            Dataset data = ReadDataset();
            Region region = data?.regions?.FirstOrDefault(r => r.id == regionId);
            if (region == null) throw new InvalidOperationException("Build the graph and select a valid region.");
            if (!data.left_hand_traffic) throw new InvalidOperationException("Edenborough requires LHT source data.");
            Road[] candidates = data.roads.Where(r => r.segments != null &&
                r.segments.Any(s => s.region == regionId)).ToArray();
            Road[] roads = candidates.Where(IsFlatPrototypeRoad).OrderBy(r => r.id, StringComparer.Ordinal).ToArray();
            if (roads.Any(r => r.tags == null || r.tags.Length == 0))
                throw new InvalidOperationException("Rebuild prototype.json with the tag-preserving importer first.");
            var roadIds = new HashSet<string>(roads.Select(r => r.id), StringComparer.Ordinal);
            Lane[] lanes = data.lanes.Where(l => l.region == regionId && roadIds.Contains(l.road)).ToArray();
            var laneIds = new HashSet<string>(lanes.Select(l => l.id), StringComparer.Ordinal);
            Turn[] turns = data.turns.Where(t => laneIds.Contains(t.@from) && laneIds.Contains(t.to)).ToArray();
            RoadRun[] runs = roads.SelectMany(r => RoadRuns(r, regionId)).ToArray();
            if (lanes.Length == 0 || runs.Length == 0 || runs.Length > 128)
                throw new InvalidOperationException("Select a prototype with 1-128 contiguous EasyRoads runs.");
            string scenePath = GeneratedFolder + "/" + regionId + "/Edenborough_" + regionId + ".unity";
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).path == scenePath)
                    throw new InvalidOperationException("Close the previously generated region scene before rebuilding it.");
            string folder = EnsureFolder(GeneratedFolder + "/" + regionId);
            Scene previousScene = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            bool saved = false;
            SceneManager.SetActiveScene(scene);
            try
            {
            var root = new GameObject("Edenborough " + regionId);
            var metadata = root.AddComponent<EdenboroughSourceMetadata>();
            metadata.districtId = data.district_id;
            metadata.regionId = regionId;
            metadata.coordinateSystem = data.coordinate_system;
            metadata.sourceUrl = data.source.source_url;
            metadata.sourceSha256 = data.source.source_sha256;
            metadata.sourceDownloadedUtc = data.source.downloaded_utc;
            metadata.sourceBoundary = data.source.boundary_json;
            metadata.license = data.source.license;
            metadata.attribution = data.source.attribution;
            metadata.attributionUrl = data.source.attribution_url;
            metadata.pipelineVersion = data.source.pipeline_version;
            metadata.leftHandTraffic = data.left_hand_traffic;
            LwsStreamedChunkSceneRoot chunkRoot = root.AddComponent<LwsStreamedChunkSceneRoot>();
            var serialized = new SerializedObject(chunkRoot);
            serialized.FindProperty("chunkId").stringValue = regionId;
            serialized.FindProperty("sceneName").stringValue = "Edenborough_" + regionId;
            serialized.FindProperty("synchronizeSceneStreamerNeighbors").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            metadata.roadCount = roads.Length;
            metadata.laneCount = lanes.Length;
            metadata.turnCount = turns.Length;
            metadata.easyRoadsObjectCount = runs.Length;
            metadata.excludedGradeSeparatedRoadCount = candidates.Length - roads.Length;
            metadata.roadGeometryAuthority = "EasyRoads3D Pro";

            Material asphalt = AssetDatabase.LoadAssetAtPath<Material>(folder + "/Road.mat");
            if (asphalt == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                asphalt = new Material(shader) { color = new Color(0.19f, 0.21f, 0.23f) };
                AssetDatabase.CreateAsset(asphalt, folder + "/Road.mat");
            }
            var vendor = new EdenboroughEasyRoadsAdapter(scene, root.transform, asphalt);
            foreach (RoadRun run in runs)
            {
                float width = RoadWidth(run.road, data.lane_width_meters);
                vendor.CreateRoad(run.name, ExtendedMarkers(run, width), width,
                    run.road.forward_count == 0 || run.road.backward_count == 0, run.road.@class);
            }
            Physics.SyncTransforms();
            MeshCollider[] colliders = vendor.Colliders;
            var supported = new Dictionary<string, Vector3[]>(StringComparer.Ordinal);
            foreach (Lane lane in lanes) supported.Add(lane.id, SupportedSamples(lane.id, lane.points, colliders));
            foreach (Turn turn in turns) supported.Add(turn.id, SupportedSamples(turn.id, turn.points, colliders));
            CreateGround(root.transform, region);

            LwsRoadGraph graph = BuildRoadGraph(regionId, lanes, turns, roads, data.lane_width_meters, supported);
            var validation = graph.Validate();
            if (!validation.IsValid) throw new InvalidOperationException(validation.Summary);
            var provider = root.AddComponent<LwsRoadGraphProvider>();
            provider.SetGraph(graph, false);
            var providerSerialized = new SerializedObject(provider);
            providerSerialized.FindProperty("registerOnStart").boolValue = false;
            providerSerialized.ApplyModifiedPropertiesWithoutUndo();
            metadata.trafficData = CreateTrafficData(folder, data, regionId, roads, candidates, runs, lanes, turns, graph);
            vendor.PersistMeshes(folder);
            Lane spawnLane = lanes.OrderByDescending(l => Distance(l.points)).First();
            var spawn = new GameObject("PrototypeTruckSpawn");
            spawn.transform.SetParent(root.transform, false);
            Vector3 p0 = supported[spawnLane.id][0] + Vector3.up * .8f;
            Vector3 p1 = supported[spawnLane.id].Last() + Vector3.up * .8f;
            spawn.transform.position = Vector3.Lerp(p0, p1, .35f);
            spawn.transform.rotation = Quaternion.LookRotation((p1 - p0).normalized, Vector3.up);
            Bounds bounds = new Bounds(new Vector3(region.bounds[0] + region.bounds[2] / 2f,
                0f, region.bounds[1] + region.bounds[3] / 2f), new Vector3(region.bounds[2], 100f, region.bounds[3]));
            foreach (MeshCollider collider in vendor.Colliders) bounds.Encapsulate(collider.bounds);
            CreateManifest(folder, region, scenePath, graph, bounds);
            if (!EditorSceneManager.SaveScene(scene, scenePath))
                throw new IOException("Could not save " + scenePath);
            AssetDatabase.SaveAssets();
            saved = true;
            return $"Edenborough {regionId}: {runs.Length} EasyRoads objects, {roads.Length} OSM ways, " +
                $"{lanes.Length} LWS lanes, {turns.Length} turns; vendor-collider coverage passed. Scene: {scenePath}";
            }
            finally
            {
                if (previousScene.IsValid() && previousScene.isLoaded) SceneManager.SetActiveScene(previousScene);
                if (!saved && scene.IsValid()) EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static bool IsFlatPrototypeRoad(Road road) =>
            !HasGradeTag(road.bridge) && !HasGradeTag(road.tunnel) &&
            (string.IsNullOrEmpty(road.layer) || road.layer == "0");

        private static bool HasGradeTag(string value) =>
            !string.IsNullOrEmpty(value) && value != "no" && value != "0" && value != "false";

        private static float RoadWidth(Road road, float laneWidth) =>
            (road.forward_count > 0 && road.backward_count > 0
                ? 2 * Mathf.Max(road.forward_count, road.backward_count)
                : road.forward_count + road.backward_count) * laneWidth + 1f;

        private static IEnumerable<RoadRun> RoadRuns(Road road, string region)
        {
            RoadRun run = null;
            long last = -1;
            foreach (Segment segment in road.segments)
            {
                if (segment.region != region || segment.a != last)
                {
                    if (run != null) { yield return run; run = null; }
                }
                if (segment.region != region) continue;
                if (segment.points == null || segment.points.Length != 2)
                    throw new InvalidOperationException(road.id + " contains an invalid segment.");
                if (run == null)
                {
                    run = new RoadRun { road = road, name = "Edenborough " + road.id + " n" + segment.a };
                    run.points.Add(Position(segment.points[0], .12f));
                }
                run.points.Add(Position(segment.points[1], .12f));
                last = segment.b;
            }
            if (run != null) yield return run;
        }

        private static Vector3[] ExtendedMarkers(RoadRun run, float width)
        {
            Vector3[] points = run.points.ToArray();
            if ((points[0] - points[points.Length - 1]).sqrMagnitude < .001f) return points;
            var markers = new List<Vector3> { points[0] - (points[1] - points[0]).normalized * width * .5f };
            markers.AddRange(points);
            markers.Add(points[points.Length - 1] +
                (points[points.Length - 1] - points[points.Length - 2]).normalized * width * .5f);
            return markers.ToArray();
        }

        private static Vector3[] SupportedSamples(string id, Point[] points, MeshCollider[] colliders)
        {
            Vector3 a = Position(points[0], .12f), b = Position(points[1], .12f);
            int steps = Mathf.Max(2, Mathf.CeilToInt(Vector3.Distance(a, b) / 2f));
            var samples = new Vector3[steps + 1];
            for (int i = 0; i <= steps; i++)
            {
                Vector3 point = Vector3.Lerp(a, b, i / (float)steps);
                var ray = new Ray(point + Vector3.up * 3f, Vector3.down);
                bool found = false;
                foreach (MeshCollider collider in colliders)
                {
                    if (!collider.Raycast(ray, out RaycastHit hit, 6f) || hit.normal.y < .5f) continue;
                    samples[i] = new Vector3(point.x, hit.point.y + .06f, point.z);
                    found = true;
                    break;
                }
                if (!found) throw new InvalidOperationException(
                    id + ": no EasyRoads collision under graph sample " + i +
                    ". Refine this vendor junction before exporting a navigable graph.");
            }
            return samples;
        }

        private static void CreateGround(Transform parent, Region region)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Prototype ground";
            ground.transform.SetParent(parent, false);
            var b = region.bounds;
            ground.transform.position = new Vector3(b[0] + b[2] / 2f, -.35f, b[1] + b[3] / 2f);
            ground.transform.localScale = new Vector3(b[2], .6f, b[3]);
        }

        private static LwsRoadGraph BuildRoadGraph(string regionId, Lane[] lanes, Turn[] turns,
            Road[] roads, float laneWidth, Dictionary<string, Vector3[]> supported)
        {
            var graph = LwsRoadGraph.CreateEmpty("EDENBOROUGH_" + regionId);
            var roadById = roads.ToDictionary(r => r.id, StringComparer.Ordinal);
            foreach (Lane lane in lanes)
            {
                string a = lane.id + ":a", b = lane.id + ":b";
                Vector3[] points = supported[lane.id];
                graph.nodes.Add(new LwsRoadNode { nodeId = a, position = points[0],
                    regionId = regionId, sceneChunkId = regionId });
                graph.nodes.Add(new LwsRoadNode { nodeId = b, position = points.Last(),
                    regionId = regionId, sceneChunkId = regionId });
                Road road = roadById[lane.road];
                graph.edges.Add(MakeEdge(lane.id, a, b, points, regionId, laneWidth,
                    SpeedMph(road.maxspeed), road));
            }
            foreach (Turn turn in turns)
                graph.edges.Add(MakeEdge(turn.id, turn.@from + ":b", turn.to + ":a",
                    supported[turn.id], regionId, laneWidth, 12f, null));
            return graph;
        }

        private static LwsRoadEdge MakeEdge(string id, string from, string to,
            Vector3[] points, string region, float width, float speed, Road road)
        {
            Vector3 a = points[0], b = points.Last();
            Vector3 delta = b - a;
            Vector3 forward = delta.sqrMagnitude > .0001f ? delta.normalized : Vector3.forward;
            var edge = new LwsRoadEdge
            {
                edgeId = id, roadId = id, segmentId = id, fromNodeId = from, toNodeId = to,
                roadClass = road?.@class == "service" ? LwsRoadClass.DepotAccess : LwsRoadClass.LocalRoad,
                direction = Direction(forward),
                surfaceType = road?.surface == "gravel" ? LwsRoadSurfaceType.Gravel :
                    road?.surface == "dirt" || road?.surface == "unpaved"
                        ? LwsRoadSurfaceType.Dirt : LwsRoadSurfaceType.AsphaltInterstate,
                oneWay = true,
                speedLimitMph = speed, laneCount = 1, laneWidthMeters = width,
                stateOrRegionId = region, sceneChunkId = region,
                laneCenterOffsetsMeters = new List<float> { 0f },
                samples = new List<LwsRoadSample>()
            };
            float distance = 0f;
            for (int i = 0; i < points.Length; i++)
            {
                if (i > 0) distance += Vector3.Distance(points[i - 1], points[i]);
                edge.samples.Add(new LwsRoadSample
                {
                    roadId = id, segmentId = id, position = points[i],
                    forward = forward, direction = edge.direction, laneCount = 1, laneWidthMeters = width,
                    roadWidthMeters = width, speedLimitMph = speed, distanceFromStartMeters = distance
                });
            }
            edge.distanceMeters = edge.travelCost = Mathf.Max(.01f, distance);
            return edge;
        }

        private static LwsRoadDirection Direction(Vector3 forward) =>
            Mathf.Abs(forward.x) > Mathf.Abs(forward.z)
                ? forward.x >= 0 ? LwsRoadDirection.Eastbound : LwsRoadDirection.Westbound
                : forward.z >= 0 ? LwsRoadDirection.Northbound : LwsRoadDirection.Southbound;

        private static float SpeedMph(string raw)
        {
            Match match = Regex.Match(raw ?? "", @"^\s*(\d+(?:\.\d+)?)\s*(mph|km/h|kph)?\s*$");
            if (!match.Success) return 20f;
            float speed = float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            return Mathf.Max(1f, match.Groups[2].Value == "mph" ? speed : speed * .621371f);
        }

        private static Vector3 Position(Point point, float y) => new Vector3(point.x, y, point.z);
        private static float Distance(Point[] points) => points?.Length == 2
            ? Vector2.Distance(new Vector2(points[0].x, points[0].z),
                new Vector2(points[1].x, points[1].z)) : 0f;

        private static EdenboroughTrafficData CreateTrafficData(string folder, Dataset data, string regionId,
            Road[] roads, Road[] candidates, RoadRun[] runs, Lane[] lanes, Turn[] turns, LwsRoadGraph graph)
        {
            string path = folder + "/EdenboroughTrafficData.asset";
            var asset = AssetDatabase.LoadAssetAtPath<EdenboroughTrafficData>(path);
            if (asset == null)
            {
                asset = CreateInstance<EdenboroughTrafficData>();
                AssetDatabase.CreateAsset(asset, path);
            }
            asset.regionId = regionId;
            asset.graphId = graph.graphId;
            asset.sourceSha256 = data.source.source_sha256;
            asset.leftHandTraffic = true;
            var edges = graph.edges.ToDictionary(e => e.edgeId, StringComparer.Ordinal);
            asset.ways = roads.Select(road => new EdenboroughWayBinding
            {
                sourceRoadId = road.id, osmWayId = road.osm_way, displayName = road.name, tags = road.tags,
                easyRoadsObjectNames = runs.Where(r => r.road == road).Select(r => r.name).ToArray(),
                laneIds = lanes.Where(l => l.road == road.id).Select(l => l.id).ToArray()
            }).ToList();
            asset.lanes = lanes.Select(lane =>
            {
                LwsRoadEdge edge = edges[lane.id];
                var definition = new LwsTrafficLaneDefinition
                {
                    laneId = lane.id, roadId = edge.roadId, segmentId = edge.segmentId, edgeId = edge.edgeId,
                    laneIndex = lane.index, roadClass = edge.roadClass, direction = edge.direction,
                    speedLimitMph = edge.speedLimitMph, laneWidthMeters = edge.laneWidthMeters,
                    laneCenterOffsetMeters = 0f, lengthMeters = edge.distanceMeters,
                    centerline = edge.samples.Select(s => s.position).ToArray(), spawnEnabled = false
                };
                if (!definition.Validate(out string message)) throw new InvalidOperationException(message);
                return definition;
            }).ToList();
            asset.turns = turns.Select(turn => new EdenboroughTurnBinding
            {
                edgeId = turn.id, fromLaneId = turn.@from, toLaneId = turn.to, viaOsmNode = turn.via,
                centerline = edges[turn.id].samples.Select(s => s.position).ToArray()
            }).ToList();
            var included = new HashSet<string>(roads.Select(r => r.id), StringComparer.Ordinal);
            asset.excludedSourceRoads = candidates.Where(r => !included.Contains(r.id)).Select(r => r.id).ToList();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static void CreateManifest(string folder, Region region, string scenePath,
            LwsRoadGraph graph, Bounds bounds)
        {
            string policyPath = folder + "/StreamingPolicy.asset";
            var policy = AssetDatabase.LoadAssetAtPath<LwsWorldStreamingPolicy>(policyPath);
            if (policy == null)
            {
                policy = CreateInstance<LwsWorldStreamingPolicy>();
                policy.minimumLoadedNeighborCount = 0;
                AssetDatabase.CreateAsset(policy, policyPath);
            }
            string manifestPath = folder + "/StreamingManifest.asset";
            var manifest = AssetDatabase.LoadAssetAtPath<LwsWorldStreamingManifest>(manifestPath);
            if (manifest == null)
            {
                manifest = CreateInstance<LwsWorldStreamingManifest>();
                AssetDatabase.CreateAsset(manifest, manifestPath);
            }
            manifest.worldId = "EDENBOROUGH_PROTOTYPE";
            manifest.policy = policy;
            manifest.chunks = new List<LwsWorldChunkDefinition>
            {
                new LwsWorldChunkDefinition
                {
                    chunkId = region.id, displayName = region.id,
                    sceneName = "Edenborough_" + region.id, scenePath = scenePath,
                    boundsCenter = bounds.center, boundsSize = bounds.size,
                    roadIds = graph.edges.Select(e => e.roadId).Distinct().ToList(),
                    containsTrafficPresentation = false,
                    containsWeatheradePresentation = false
                }
            };
            var validation = manifest.ValidateManifest();
            if (!validation.IsValid) throw new InvalidOperationException(validation.Summary);
            EditorUtility.SetDirty(manifest);
        }

        private static string EnsureFolder(string folder)
        {
            string current = "Assets";
            foreach (string part in folder.Split('/').Skip(1))
            {
                string next = current + "/" + part;
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, part);
                current = next;
            }
            return current;
        }

        [Serializable] private sealed class Point { public float x, z; }
        private sealed class RoadRun
        {
            public Road road;
            public string name;
            public readonly List<Vector3> points = new List<Vector3>();
        }
        [Serializable] private sealed class Segment
        {
            public long a, b;
            public string region;
            public Point[] points;
        }
        [Serializable] private sealed class Road
        {
            public string id, name, @class, maxspeed, surface, bridge, tunnel, layer;
            public long osm_way;
            public int forward_count, backward_count;
            public Segment[] segments;
            public EdenboroughOsmTag[] tags;
        }
        [Serializable] private sealed class Lane
        {
            public string id, road, region;
            public int index;
            public Point[] points;
        }
        [Serializable] private sealed class Turn
        {
            public string id, @from, to;
            public long via;
            public Point[] points;
        }
        [Serializable] private sealed class Region
        {
            public string id;
            public float[] bounds;
            public string[] roads;
        }
        [Serializable] private sealed class Source
        {
            public string source_url, source_sha256, downloaded_utc, license, attribution, attribution_url;
            public string boundary_json;
            public int pipeline_version;
        }
        [Serializable] private sealed class Dataset
        {
            public string district_id, coordinate_system;
            public bool left_hand_traffic;
            public float lane_width_meters;
            public Source source;
            public Road[] roads;
            public Lane[] lanes;
            public Turn[] turns;
            public Region[] regions;
        }
    }
}
