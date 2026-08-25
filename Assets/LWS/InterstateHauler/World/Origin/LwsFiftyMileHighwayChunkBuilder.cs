using System.Collections.Generic;
using UnityEngine;

namespace LWS.InterstateHauler
{
    [DisallowMultipleComponent]
    public sealed class LwsFiftyMileHighwayChunkBuilder : MonoBehaviour
    {
        public const string RuntimeRoadRootSuffix = "Proving Ground Interstate Runtime";
        public const string MainRoadSurfaceRootName = "Main Road Surface";
        public const string ShouldersAndMedianRootName = "Shoulders And Median";
        public const string LaneMarkingsRootName = "Lane Markings";
        public const string RoadsideSupportRootName = "Roadside Support";

        [SerializeField] private int chunkIndex;
        [SerializeField] private bool buildOnStart = true;
        [SerializeField] private bool createLaneDebugLines = true;
        [SerializeField] private bool createLaneMarkings = true;
        [SerializeField] private bool createWholeMileMarkers = true;
        [SerializeField] private bool createQuarterMileMarkers;

        private GameObject _generatedRoot;

        public int ChunkIndex => Mathf.Clamp(chunkIndex, 0, LwsFiftyMileHighwayModel.ChunkCount - 1);
        public bool WasBuilt => _generatedRoot != null;
        public Vector3 GeneratedRootLocalPosition => _generatedRoot != null ? _generatedRoot.transform.localPosition : default;
        public float ChunkStartLocalZ => (float)LwsFiftyMileHighwayModel.GetChunkStartMeters(ChunkIndex);

        private void Awake()
        {
            if (Application.isPlaying && buildOnStart)
            {
                BuildChunk();
            }
        }

        private void Start()
        {
            if (buildOnStart)
            {
                BuildChunk();
            }
        }

        public void Configure(int newChunkIndex)
        {
            chunkIndex = Mathf.Clamp(newChunkIndex, 0, LwsFiftyMileHighwayModel.ChunkCount - 1);
        }

        public void BuildChunk()
        {
            if (_generatedRoot != null)
            {
                return;
            }

            int index = ChunkIndex;
            string chunkId = LwsFiftyMileHighwayModel.GetChunkId(index);
            _generatedRoot = new GameObject($"{chunkId} {RuntimeRoadRootSuffix}");
            _generatedRoot.transform.SetParent(transform, false);
            _generatedRoot.transform.localPosition = Vector3.zero;

            Transform roadSurfaceRoot = EnsureChild(_generatedRoot.transform, MainRoadSurfaceRootName);
            Transform shoulderRoot = EnsureChild(_generatedRoot.transform, ShouldersAndMedianRootName);
            Transform laneMarkingsRoot = EnsureChild(_generatedRoot.transform, LaneMarkingsRootName);
            Transform roadsideRoot = EnsureChild(_generatedRoot.transform, RoadsideSupportRootName);

            Material asphalt = CreateRuntimeMaterial("IH 50-Mile Asphalt", new Color(0.06f, 0.06f, 0.055f, 1f));
            Material shoulder = CreateRuntimeMaterial("IH 50-Mile Shoulder", new Color(0.26f, 0.26f, 0.24f, 1f));
            Material grass = CreateRuntimeMaterial("IH 50-Mile Grass", new Color(0.12f, 0.24f, 0.1f, 1f));
            Material whiteMarker = CreateRuntimeMaterial("IH 50-Mile Marker White", new Color(0.95f, 0.95f, 0.86f, 1f));
            Material yellowMarker = CreateRuntimeMaterial("IH 50-Mile Marker Yellow", new Color(1f, 0.82f, 0.08f, 1f));

            float endLocal = (float)(LwsFiftyMileHighwayModel.GetChunkEndMeters(index) - LwsFiftyMileHighwayModel.GetChunkStartMeters(index));
            CreateGroundRibbon(roadsideRoot, "IH_50MI_GROUND", 0f, endLocal, 190f, grass);
            CreateRoadRibbon(roadSurfaceRoot, $"{LwsFiftyMileHighwayModel.EastboundRoadId}_{index:000}", LwsFiftyMileHighwayModel.EastboundRoadId, LwsRoadDirection.Eastbound, LwsFiftyMileHighwayModel.CarriagewayOffsetMeters, 0f, endLocal, asphalt);
            CreateRoadRibbon(roadSurfaceRoot, $"{LwsFiftyMileHighwayModel.WestboundRoadId}_{index:000}", LwsFiftyMileHighwayModel.WestboundRoadId, LwsRoadDirection.Westbound, -LwsFiftyMileHighwayModel.CarriagewayOffsetMeters, 0f, endLocal, asphalt);
            CreateMedianAndShoulderStrips(shoulderRoot, endLocal, shoulder);
            if (createLaneMarkings)
            {
                CreateLaneMarkings(laneMarkingsRoot, 0f, endLocal, whiteMarker, yellowMarker);
            }

            LwsInterstateRoadsideBuilder.BuildStraightPairedInterstate(
                roadsideRoot,
                chunkId,
                0f,
                endLocal);

            if (index == 0)
            {
                CreatePavedRect(roadSurfaceRoot, "Mile 0 Start Pad", new Vector3(LwsFiftyMileHighwayModel.CarriagewayOffsetMeters, LwsFiftyMileHighwayModel.RoadSurfaceY + 0.02f, 70f), new Vector2(95f, 165f), asphalt, "IH_TEST_50MI_START_PAD");
            }

            if (index == LwsFiftyMileHighwayModel.ChunkCount - 1)
            {
                CreatePavedRect(roadSurfaceRoot, "Mile 50 Turnaround Pad", new Vector3(0f, LwsFiftyMileHighwayModel.RoadSurfaceY + 0.03f, endLocal - 70f), new Vector2(150f, 160f), asphalt, "IH_TEST_50MI_TURNAROUND_PAD");
                CreateFinishMarker(endLocal, whiteMarker);
            }

            if (createWholeMileMarkers)
            {
                foreach (int mile in LwsFiftyMileHighwayModel.EnumerateWholeMileMarkersForChunk(index))
                {
                    double globalMeters = LwsFiftyMileHighwayModel.MileToMeters(mile);
                    float localZ = (float)(globalMeters - LwsFiftyMileHighwayModel.GetChunkStartMeters(index));
                    CreateMileMarker(mile, localZ, whiteMarker);
                }
            }

            if (createQuarterMileMarkers)
            {
                CreateQuarterMileMarkers(index, whiteMarker);
            }
        }

        private void CreateRoadRibbon(Transform parent, string segmentId, string roadId, LwsRoadDirection direction, float xOffset, float startZ, float endZ, Material material)
        {
            Vector3[] samples =
            {
                new Vector3(xOffset, LwsFiftyMileHighwayModel.RoadSurfaceY, startZ),
                new Vector3(xOffset, LwsFiftyMileHighwayModel.RoadSurfaceY, endZ)
            };

            Mesh mesh = CreateRibbonMesh(samples, LwsFiftyMileHighwayModel.CarriagewayWidthMeters);
            GameObject go = new GameObject($"{segmentId} Mainline Paved Road Surface");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
            go.AddComponent<LwsRoadSurface>().Configure(roadId, segmentId, LwsRoadSurfaceType.AsphaltInterstate, "50-mile straight validation interstate asphalt");

            if (createLaneDebugLines)
            {
                CreateLaneDebugLines(go.transform, samples, direction);
            }
        }

        private void CreateGroundRibbon(Transform parent, string segmentId, float startZ, float endZ, float width, Material material)
        {
            Vector3[] samples =
            {
                new Vector3(0f, 0f, startZ),
                new Vector3(0f, 0f, endZ)
            };

            Mesh mesh = CreateRibbonMesh(samples, width);
            GameObject go = new GameObject($"{segmentId} Grass Base");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        private void CreateMedianAndShoulderStrips(Transform parent, float endLocal, Material material)
        {
            float carriageway = LwsFiftyMileHighwayModel.CarriagewayOffsetMeters;
            float halfLanes = LwsFiftyMileHighwayModel.LaneWidthMeters;
            float innerShoulderHalf = LwsFiftyMileHighwayModel.LeftShoulderWidthMeters * 0.5f;
            float outerShoulderHalf = LwsFiftyMileHighwayModel.RightShoulderWidthMeters * 0.5f;
            float y = LwsFiftyMileHighwayModel.RoadSurfaceY + 0.018f;
            CreatePavedRect(parent, "50-Mile Median Separation", new Vector3(0f, LwsFiftyMileHighwayModel.RoadSurfaceY - 0.012f, endLocal * 0.5f), new Vector2(LwsFiftyMileHighwayModel.MedianWidthMeters, endLocal), material, "IH_TEST_50MI_MEDIAN", false);
            CreatePavedRect(parent, "50-Mile Eastbound Median Shoulder", new Vector3(carriageway - halfLanes - innerShoulderHalf, y, endLocal * 0.5f), new Vector2(LwsFiftyMileHighwayModel.LeftShoulderWidthMeters, endLocal), material, "IH_TEST_50MI_EB_MEDIAN_SHOULDER", false);
            CreatePavedRect(parent, "50-Mile Eastbound Outer Shoulder", new Vector3(carriageway + halfLanes + outerShoulderHalf, y, endLocal * 0.5f), new Vector2(LwsFiftyMileHighwayModel.RightShoulderWidthMeters, endLocal), material, "IH_TEST_50MI_EB_OUTER_SHOULDER", false);
            CreatePavedRect(parent, "50-Mile Westbound Median Shoulder", new Vector3(-carriageway + halfLanes + innerShoulderHalf, y, endLocal * 0.5f), new Vector2(LwsFiftyMileHighwayModel.LeftShoulderWidthMeters, endLocal), material, "IH_TEST_50MI_WB_MEDIAN_SHOULDER", false);
            CreatePavedRect(parent, "50-Mile Westbound Outer Shoulder", new Vector3(-carriageway - halfLanes - outerShoulderHalf, y, endLocal * 0.5f), new Vector2(LwsFiftyMileHighwayModel.RightShoulderWidthMeters, endLocal), material, "IH_TEST_50MI_WB_OUTER_SHOULDER", false);
        }

        private void CreateLaneMarkings(Transform parent, float startZ, float endZ, Material white, Material yellow)
        {
            float ebCenter = LwsFiftyMileHighwayModel.CarriagewayOffsetMeters;
            float wbCenter = -LwsFiftyMileHighwayModel.CarriagewayOffsetMeters;
            float laneEdgeOffset = LwsFiftyMileHighwayModel.LaneWidthMeters;
            float y = LwsFiftyMileHighwayModel.RoadSurfaceY + 0.045f;

            CreateDashedMarking(parent, "EB Dashed White Lane Divider", ebCenter, startZ, endZ, y, 0.16f, 9f, 15f, white);
            CreateDashedMarking(parent, "WB Dashed White Lane Divider", wbCenter, startZ, endZ, y, 0.16f, 9f, 15f, white);
            CreateSolidMarking(parent, "EB Outer White Edge Line", ebCenter + laneEdgeOffset, startZ, endZ, y, 0.14f, white);
            CreateSolidMarking(parent, "EB Median Yellow Edge Line", ebCenter - laneEdgeOffset, startZ, endZ, y, 0.16f, yellow);
            CreateSolidMarking(parent, "WB Outer White Edge Line", wbCenter - laneEdgeOffset, startZ, endZ, y, 0.14f, white);
            CreateSolidMarking(parent, "WB Median Yellow Edge Line", wbCenter + laneEdgeOffset, startZ, endZ, y, 0.16f, yellow);
        }

        private void CreateSolidMarking(Transform parent, string name, float x, float startZ, float endZ, float y, float width, Material material)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = CreateMarkingMesh(x, startZ, endZ, y, width);
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        private void CreateDashedMarking(Transform parent, string name, float x, float startZ, float endZ, float y, float width, float dashLength, float gapLength, Material material)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = CreateDashedMarkingMesh(x, startZ, endZ, y, width, dashLength, gapLength);
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static Mesh CreateRibbonMesh(IReadOnlyList<Vector3> samples, float width)
        {
            int count = samples.Count;
            var vertices = new Vector3[count * 2];
            var uvs = new Vector2[count * 2];
            var triangles = new int[(count - 1) * 6];
            float distance = 0f;

            for (int i = 0; i < count; i++)
            {
                Vector3 forward = ResolveForward(samples, i);
                Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
                vertices[i * 2] = samples[i] - right * width * 0.5f;
                vertices[i * 2 + 1] = samples[i] + right * width * 0.5f;
                if (i > 0)
                {
                    distance += Vector3.Distance(samples[i - 1], samples[i]);
                }

                uvs[i * 2] = new Vector2(0f, distance / 10f);
                uvs[i * 2 + 1] = new Vector2(1f, distance / 10f);
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

            var mesh = new Mesh { name = "IH 50-Mile Straight Highway Ribbon Mesh" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private void CreateLaneDebugLines(Transform parent, IReadOnlyList<Vector3> samples, LwsRoadDirection direction)
        {
            Material material = CreateRuntimeMaterial(
                direction == LwsRoadDirection.Westbound ? "IH 50-Mile Lane Yellow" : "IH 50-Mile Lane Cyan",
                direction == LwsRoadDirection.Westbound ? new Color(1f, 0.78f, 0.12f, 1f) : new Color(0.15f, 0.75f, 1f, 1f));

            for (int lane = 0; lane < 2; lane++)
            {
                float laneOffset = ((2 - 1) * -0.5f + lane) * LwsFiftyMileHighwayModel.LaneWidthMeters;
                var lineObject = new GameObject($"Lane {lane + 1} Center Debug");
                lineObject.transform.SetParent(parent, false);
                LineRenderer line = lineObject.AddComponent<LineRenderer>();
                line.sharedMaterial = material;
                line.widthMultiplier = 0.14f;
                line.positionCount = samples.Count;
                line.useWorldSpace = false;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                line.receiveShadows = false;

                for (int i = 0; i < samples.Count; i++)
                {
                    Vector3 forward = ResolveForward(samples, i);
                    Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
                    line.SetPosition(i, samples[i] + right * laneOffset + Vector3.up * 0.07f);
                }
            }
        }

        private void CreatePavedRect(Transform parent, string name, Vector3 center, Vector2 size, Material material, string segmentId, bool addCollider = true)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;

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
            if (addCollider)
            {
                go.AddComponent<MeshCollider>().sharedMesh = mesh;
            }

            go.AddComponent<LwsRoadSurface>().Configure("IH_TEST_50MI_SUPPORT", segmentId, LwsRoadSurfaceType.AsphaltInterstate, "50-mile validation support pavement");
        }

        private static Mesh CreateMarkingMesh(float x, float startZ, float endZ, float y, float width)
        {
            var mesh = new Mesh { name = "IH 50-Mile Lane Marking Mesh" };
            float halfWidth = Mathf.Max(0.02f, width) * 0.5f;
            mesh.vertices = new[]
            {
                new Vector3(x - halfWidth, y, startZ),
                new Vector3(x - halfWidth, y, endZ),
                new Vector3(x + halfWidth, y, endZ),
                new Vector3(x + halfWidth, y, startZ)
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
            return mesh;
        }

        private static Mesh CreateDashedMarkingMesh(float x, float startZ, float endZ, float y, float width, float dashLength, float gapLength)
        {
            float length = Mathf.Max(0f, endZ - startZ);
            float step = Mathf.Max(1f, dashLength + gapLength);
            int dashCount = Mathf.Max(1, Mathf.CeilToInt(length / step));
            var vertices = new Vector3[dashCount * 4];
            var uvs = new Vector2[dashCount * 4];
            var triangles = new int[dashCount * 6];
            float halfWidth = Mathf.Max(0.02f, width) * 0.5f;

            for (int dash = 0; dash < dashCount; dash++)
            {
                float z0 = startZ + dash * step;
                float z1 = Mathf.Min(endZ, z0 + Mathf.Max(0.1f, dashLength));
                int vertexIndex = dash * 4;
                vertices[vertexIndex] = new Vector3(x - halfWidth, y, z0);
                vertices[vertexIndex + 1] = new Vector3(x - halfWidth, y, z1);
                vertices[vertexIndex + 2] = new Vector3(x + halfWidth, y, z1);
                vertices[vertexIndex + 3] = new Vector3(x + halfWidth, y, z0);
                uvs[vertexIndex] = new Vector2(0f, 0f);
                uvs[vertexIndex + 1] = new Vector2(0f, 1f);
                uvs[vertexIndex + 2] = new Vector2(1f, 1f);
                uvs[vertexIndex + 3] = new Vector2(1f, 0f);

                int triangleIndex = dash * 6;
                triangles[triangleIndex] = vertexIndex;
                triangles[triangleIndex + 1] = vertexIndex + 1;
                triangles[triangleIndex + 2] = vertexIndex + 2;
                triangles[triangleIndex + 3] = vertexIndex;
                triangles[triangleIndex + 4] = vertexIndex + 2;
                triangles[triangleIndex + 5] = vertexIndex + 3;
            }

            var mesh = new Mesh { name = "IH 50-Mile Dashed Lane Marking Mesh" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private void CreateMileMarker(int mile, float localZ, Material material)
        {
            GameObject root = new GameObject($"MILE {mile}");
            root.transform.SetParent(_generatedRoot.transform, false);
            root.transform.localPosition = new Vector3(58f, LwsFiftyMileHighwayModel.RoadSurfaceY + 3.5f, localZ);

            GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = $"MILE {mile} Board";
            board.transform.SetParent(root.transform, false);
            board.transform.localScale = new Vector3(8.5f, 4.2f, 0.18f);
            board.GetComponent<Renderer>().sharedMaterial = material;

            GameObject textObject = new GameObject($"MILE {mile} Text");
            textObject.transform.SetParent(root.transform, false);
            textObject.transform.localPosition = new Vector3(0f, -0.15f, -0.12f);
            textObject.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            TextMesh text = textObject.AddComponent<TextMesh>();
            text.text = $"MILE {mile}";
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.characterSize = 0.8f;
            text.fontSize = 80;
            text.color = Color.black;
        }

        private void CreateFinishMarker(float endLocal, Material material)
        {
            GameObject root = new GameObject("50 MILE TEST COMPLETE");
            root.transform.SetParent(_generatedRoot.transform, false);
            root.transform.localPosition = new Vector3(0f, LwsFiftyMileHighwayModel.RoadSurfaceY + 8f, endLocal - 20f);

            GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = "50 Mile Test Complete Board";
            board.transform.SetParent(root.transform, false);
            board.transform.localScale = new Vector3(34f, 8f, 0.35f);
            board.GetComponent<Renderer>().sharedMaterial = material;

            GameObject textObject = new GameObject("50 Mile Test Complete Text");
            textObject.transform.SetParent(root.transform, false);
            textObject.transform.localPosition = new Vector3(0f, -0.2f, -0.24f);
            textObject.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            TextMesh text = textObject.AddComponent<TextMesh>();
            text.text = "50 MILE TEST COMPLETE";
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.characterSize = 1.15f;
            text.fontSize = 80;
            text.color = Color.black;
        }

        private void CreateQuarterMileMarkers(int index, Material material)
        {
            double chunkStart = LwsFiftyMileHighwayModel.GetChunkStartMeters(index);
            double chunkEnd = LwsFiftyMileHighwayModel.GetChunkEndMeters(index);
            for (double markerMile = 0.25d; markerMile < LwsFiftyMileHighwayModel.TotalMiles; markerMile += 0.25d)
            {
                if (Mathf.Approximately((float)(markerMile % 1d), 0f))
                {
                    continue;
                }

                double markerMeters = LwsFiftyMileHighwayModel.MileToMeters(markerMile);
                if (markerMeters < chunkStart || markerMeters >= chunkEnd)
                {
                    continue;
                }

                GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
                post.name = $"Quarter Mile {markerMile:0.00}";
                post.transform.SetParent(_generatedRoot.transform, false);
                post.transform.localPosition = new Vector3(51f, LwsFiftyMileHighwayModel.RoadSurfaceY + 1.2f, (float)(markerMeters - chunkStart));
                post.transform.localScale = new Vector3(1.2f, 2.4f, 0.12f);
                post.GetComponent<Renderer>().sharedMaterial = material;
            }
        }

        private static Vector3 ResolveForward(IReadOnlyList<Vector3> samples, int index)
        {
            if (samples.Count < 2)
            {
                return Vector3.forward;
            }

            if (index < samples.Count - 1)
            {
                Vector3 next = samples[index + 1] - samples[index];
                if (next.sqrMagnitude > 0.0001f)
                {
                    return next.normalized;
                }
            }

            Vector3 previous = samples[index] - samples[Mathf.Max(0, index - 1)];
            return previous.sqrMagnitude > 0.0001f ? previous.normalized : Vector3.forward;
        }

        private static Transform EnsureChild(Transform parent, string childName)
        {
            Transform existing = parent.Find(childName);
            if (existing != null)
            {
                return existing;
            }

            GameObject childObject = new GameObject(childName);
            Transform child = childObject.transform;
            child.SetParent(parent, false);
            return child;
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
    }
}
