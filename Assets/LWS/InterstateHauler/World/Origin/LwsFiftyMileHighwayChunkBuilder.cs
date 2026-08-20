using System.Collections.Generic;
using UnityEngine;

namespace LWS.InterstateHauler
{
    [DisallowMultipleComponent]
    public sealed class LwsFiftyMileHighwayChunkBuilder : MonoBehaviour
    {
        [SerializeField] private int chunkIndex;
        [SerializeField] private bool buildOnStart = true;
        [SerializeField] private bool createLaneDebugLines = true;
        [SerializeField] private bool createWholeMileMarkers = true;
        [SerializeField] private bool createQuarterMileMarkers;

        private GameObject _generatedRoot;

        public int ChunkIndex => Mathf.Clamp(chunkIndex, 0, LwsFiftyMileHighwayModel.ChunkCount - 1);
        public bool WasBuilt => _generatedRoot != null;

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
            _generatedRoot = new GameObject($"{chunkId} 50-Mile Runtime Presentation");
            _generatedRoot.transform.SetParent(transform, false);

            Material asphalt = CreateRuntimeMaterial("IH 50-Mile Asphalt", new Color(0.06f, 0.06f, 0.055f, 1f));
            Material shoulder = CreateRuntimeMaterial("IH 50-Mile Shoulder", new Color(0.26f, 0.26f, 0.24f, 1f));
            Material grass = CreateRuntimeMaterial("IH 50-Mile Grass", new Color(0.12f, 0.24f, 0.1f, 1f));
            Material marker = CreateRuntimeMaterial("IH 50-Mile Marker White", new Color(0.95f, 0.95f, 0.86f, 1f));

            float endLocal = (float)(LwsFiftyMileHighwayModel.GetChunkEndMeters(index) - LwsFiftyMileHighwayModel.GetChunkStartMeters(index));
            CreateGroundRibbon("IH_50MI_GROUND", 0f, endLocal, 190f, grass);
            CreateRoadRibbon($"{LwsFiftyMileHighwayModel.EastboundRoadId}_{index:000}", LwsFiftyMileHighwayModel.EastboundRoadId, LwsRoadDirection.Eastbound, LwsFiftyMileHighwayModel.CarriagewayOffsetMeters, 0f, endLocal, asphalt);
            CreateRoadRibbon($"{LwsFiftyMileHighwayModel.WestboundRoadId}_{index:000}", LwsFiftyMileHighwayModel.WestboundRoadId, LwsRoadDirection.Westbound, -LwsFiftyMileHighwayModel.CarriagewayOffsetMeters, 0f, endLocal, asphalt);
            CreateMedianAndShoulderStrips(endLocal, shoulder);

            if (index == 0)
            {
                CreatePavedRect("Mile 0 Start Pad", new Vector3(LwsFiftyMileHighwayModel.CarriagewayOffsetMeters, LwsFiftyMileHighwayModel.RoadSurfaceY + 0.02f, 70f), new Vector2(95f, 165f), asphalt, "IH_TEST_50MI_START_PAD");
            }

            if (index == LwsFiftyMileHighwayModel.ChunkCount - 1)
            {
                CreatePavedRect("Mile 50 Turnaround Pad", new Vector3(0f, LwsFiftyMileHighwayModel.RoadSurfaceY + 0.03f, endLocal - 70f), new Vector2(150f, 160f), asphalt, "IH_TEST_50MI_TURNAROUND_PAD");
                CreateFinishMarker(endLocal, marker);
            }

            if (createWholeMileMarkers)
            {
                foreach (int mile in LwsFiftyMileHighwayModel.EnumerateWholeMileMarkersForChunk(index))
                {
                    double globalMeters = LwsFiftyMileHighwayModel.MileToMeters(mile);
                    float localZ = (float)(globalMeters - LwsFiftyMileHighwayModel.GetChunkStartMeters(index));
                    CreateMileMarker(mile, localZ, marker);
                }
            }

            if (createQuarterMileMarkers)
            {
                CreateQuarterMileMarkers(index, marker);
            }
        }

        private void CreateRoadRibbon(string segmentId, string roadId, LwsRoadDirection direction, float xOffset, float startZ, float endZ, Material material)
        {
            Vector3[] samples =
            {
                new Vector3(xOffset, LwsFiftyMileHighwayModel.RoadSurfaceY, startZ),
                new Vector3(xOffset, LwsFiftyMileHighwayModel.RoadSurfaceY, endZ)
            };

            Mesh mesh = CreateRibbonMesh(samples, LwsFiftyMileHighwayModel.CarriagewayWidthMeters);
            GameObject go = new GameObject($"{segmentId} Streamed Road");
            go.transform.SetParent(_generatedRoot.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
            go.AddComponent<LwsRoadSurface>().Configure(roadId, segmentId, LwsRoadSurfaceType.AsphaltInterstate, "50-mile straight validation interstate asphalt");

            if (createLaneDebugLines)
            {
                CreateLaneDebugLines(go.transform, samples, direction);
            }
        }

        private void CreateGroundRibbon(string segmentId, float startZ, float endZ, float width, Material material)
        {
            Vector3[] samples =
            {
                new Vector3(0f, 0f, startZ),
                new Vector3(0f, 0f, endZ)
            };

            Mesh mesh = CreateRibbonMesh(samples, width);
            GameObject go = new GameObject($"{segmentId} Grass Base");
            go.transform.SetParent(_generatedRoot.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        private void CreateMedianAndShoulderStrips(float endLocal, Material material)
        {
            CreatePavedRect("50-Mile Median Strip", new Vector3(0f, LwsFiftyMileHighwayModel.RoadSurfaceY - 0.01f, endLocal * 0.5f), new Vector2(LwsFiftyMileHighwayModel.MedianWidthMeters, endLocal), material, "IH_TEST_50MI_MEDIAN");
            CreatePavedRect("50-Mile EB Right Shoulder Visual", new Vector3(42f, LwsFiftyMileHighwayModel.RoadSurfaceY - 0.005f, endLocal * 0.5f), new Vector2(3f, endLocal), material, "IH_TEST_50MI_EB_SHOULDER");
            CreatePavedRect("50-Mile WB Right Shoulder Visual", new Vector3(-42f, LwsFiftyMileHighwayModel.RoadSurfaceY - 0.005f, endLocal * 0.5f), new Vector2(3f, endLocal), material, "IH_TEST_50MI_WB_SHOULDER");
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

        private void CreatePavedRect(string name, Vector3 center, Vector2 size, Material material, string segmentId)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(_generatedRoot.transform, false);
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
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
            go.AddComponent<LwsRoadSurface>().Configure("IH_TEST_50MI_SUPPORT", segmentId, LwsRoadSurfaceType.AsphaltInterstate, "50-mile validation support pavement");
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
