using System.Collections.Generic;
using UnityEngine;

namespace LWS.InterstateHauler
{
    [DisallowMultipleComponent]
    public sealed class LwsStreamingHighwayChunkBuilder : MonoBehaviour
    {
        private const string CorridorId = "IH_TEST_I000";
        private const float LaneWidthMeters = 3.7f;
        private const float RightShoulderWidthMeters = 3.0f;
        private const float LeftShoulderWidthMeters = 1.2f;
        private const float MedianWidthMeters = 14.0f;
        private const float RoadSurfaceY = 0.55f;
        private const float SampleSpacingMeters = 50f;

        [SerializeField] private string chunkId = "IH_TEST_CHUNK_UNASSIGNED";
        [SerializeField] private float minZ;
        [SerializeField] private float maxZ = 650f;
        [SerializeField] private bool buildOnStart = true;
        [SerializeField] private bool includeServiceArea;
        [SerializeField] private bool includeTurnaround;
        [SerializeField] private bool createLaneDebugLines = true;

        private GameObject _generatedRoot;

        public string ChunkId => chunkId;
        public bool WasBuilt => _generatedRoot != null;

        private static float CarriagewayWidthMeters => LaneWidthMeters * 2f + RightShoulderWidthMeters + LeftShoulderWidthMeters;
        private static float CarriagewayOffsetMeters => MedianWidthMeters * 0.5f + CarriagewayWidthMeters * 0.5f;

        private void Start()
        {
            if (buildOnStart)
            {
                BuildChunk();
            }
        }

        public void BuildChunk()
        {
            if (_generatedRoot != null)
            {
                return;
            }

            _generatedRoot = new GameObject($"{chunkId} Runtime Road Presentation");
            _generatedRoot.transform.SetParent(transform, false);

            Material asphalt = CreateRuntimeMaterial("IH Streamed Asphalt", new Color(0.065f, 0.065f, 0.06f, 1f));
            Material shoulder = CreateRuntimeMaterial("IH Streamed Shoulder", new Color(0.3f, 0.3f, 0.28f, 1f));

            CreateRoadRibbon("IH_TEST_I000_NB_MAIN", "IH_TEST_I000_NB", LwsRoadDirection.Northbound, CarriagewayOffsetMeters, minZ, maxZ, asphalt);
            CreateRoadRibbon("IH_TEST_I000_SB_MAIN", "IH_TEST_I000_SB", LwsRoadDirection.Southbound, -CarriagewayOffsetMeters, minZ, maxZ, asphalt);

            if (includeServiceArea)
            {
                CreatePavedRect("IH_TEST_I000 Service Area Chunk Pad", new Vector3(-38f, RoadSurfaceY, -95f), new Vector2(120f, 120f), asphalt, "IH_TEST_I000_SERVICE");
                CreatePavedRect("IH_TEST_I000 Start Shoulder Chunk Pad", new Vector3(34f, RoadSurfaceY, 45f), new Vector2(26f, 170f), shoulder, "IH_TEST_I000_SHOULDER_START");
                CreateRamp(asphalt);
            }

            if (includeTurnaround)
            {
                CreatePavedRect("IH_TEST_I000 Turnaround Chunk Apron", new Vector3(0f, RoadSurfaceY + 8f, 3420f), new Vector2(110f, 95f), asphalt, "IH_TEST_I000_TURNAROUND");
                CreateTurnaround(asphalt);
            }
        }

        private void CreateRoadRibbon(string segmentId, string roadId, LwsRoadDirection direction, float xOffset, float startZ, float endZ, Material material)
        {
            Vector3[] samples = SampleCenterlineByZ(startZ, endZ, xOffset);
            if (samples.Length < 2)
            {
                return;
            }

            Mesh mesh = CreateRibbonMesh(samples, CarriagewayWidthMeters);
            GameObject go = new GameObject($"{segmentId} {chunkId} Streamed Road");
            go.transform.SetParent(_generatedRoot.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
            go.AddComponent<LwsRoadSurface>().Configure(roadId, $"{segmentId}_{chunkId}", LwsRoadSurfaceType.AsphaltInterstate, "Streamed dry interstate asphalt");

            if (createLaneDebugLines)
            {
                CreateLaneDebugLines(go.transform, samples, direction);
            }
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

            var mesh = new Mesh { name = "IH Streamed Highway Ribbon Mesh" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private void CreateRamp(Material material)
        {
            Vector3[] ramp =
            {
                new Vector3(-35f, RoadSurfaceY, -110f),
                new Vector3(-18f, RoadSurfaceY, -40f),
                new Vector3(3f, RoadSurfaceY, 30f),
                new Vector3(CarriagewayOffsetMeters, RoadSurfaceY, 135f)
            };

            Mesh mesh = CreateRibbonMesh(ramp, 7.5f);
            GameObject go = new GameObject("IH_TEST_I000_NB_ENTRY_RAMP Streamed Road");
            go.transform.SetParent(_generatedRoot.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
            go.AddComponent<LwsRoadSurface>().Configure("IH_TEST_I000_RAMP", "IH_TEST_I000_NB_ENTRY_RAMP", LwsRoadSurfaceType.AsphaltInterstate, "Streamed ramp asphalt");
        }

        private void CreateTurnaround(Material material)
        {
            Vector3 end = EvaluateCenterline(3350f);
            Vector3[] crossover =
            {
                end + Vector3.right * CarriagewayOffsetMeters,
                new Vector3(0f, end.y, end.z + 55f),
                end - Vector3.right * CarriagewayOffsetMeters
            };

            Mesh mesh = CreateRibbonMesh(crossover, 10f);
            GameObject go = new GameObject("IH_TEST_I000_TURNAROUND_CROSSOVER Streamed Road");
            go.transform.SetParent(_generatedRoot.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
            go.AddComponent<LwsRoadSurface>().Configure("IH_TEST_I000_TURN", "IH_TEST_I000_TURNAROUND_CROSSOVER", LwsRoadSurfaceType.AsphaltInterstate, "Streamed turnaround asphalt");
        }

        private void CreatePavedRect(string name, Vector3 center, Vector2 size, Material material, string segmentId)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(_generatedRoot.transform, false);
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
            go.AddComponent<LwsRoadSurface>().Configure(CorridorId, segmentId, LwsRoadSurfaceType.AsphaltInterstate, "Streamed paved validation surface");
        }

        private void CreateLaneDebugLines(Transform parent, IReadOnlyList<Vector3> samples, LwsRoadDirection direction)
        {
            Material material = CreateRuntimeMaterial(
                direction == LwsRoadDirection.Southbound ? "IH Streamed Lane Yellow" : "IH Streamed Lane Cyan",
                direction == LwsRoadDirection.Southbound ? new Color(1f, 0.78f, 0.12f, 1f) : new Color(0.15f, 0.75f, 1f, 1f));

            for (int lane = 0; lane < 2; lane++)
            {
                float laneOffset = ((2 - 1) * -0.5f + lane) * LaneWidthMeters;
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

        private static Vector3[] SampleCenterlineByZ(float startZ, float endZ, float xOffset)
        {
            int steps = Mathf.Max(2, Mathf.CeilToInt(Mathf.Abs(endZ - startZ) / SampleSpacingMeters) + 1);
            var samples = new Vector3[steps];
            for (int i = 0; i < steps; i++)
            {
                float t = i / (float)(steps - 1);
                float z = Mathf.Lerp(startZ, endZ, t);
                samples[i] = EvaluateCenterline(z) + Vector3.right * xOffset;
            }

            return samples;
        }

        private static Vector3 EvaluateCenterline(float z)
        {
            Vector3[] polyline = CreateReferenceCenterline();
            if (z <= polyline[0].z)
            {
                return polyline[0] + Vector3.forward * (z - polyline[0].z);
            }

            for (int i = 0; i < polyline.Length - 1; i++)
            {
                Vector3 a = polyline[i];
                Vector3 b = polyline[i + 1];
                if (z >= a.z && z <= b.z)
                {
                    float t = Mathf.InverseLerp(a.z, b.z, z);
                    return Vector3.Lerp(a, b, t);
                }
            }

            Vector3 last = polyline[polyline.Length - 1];
            return last + Vector3.forward * (z - last.z);
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

        private static Vector3[] CreateReferenceCenterline()
        {
            return new[]
            {
                new Vector3(0f, RoadSurfaceY, 0f),
                new Vector3(0f, RoadSurfaceY, 520f),
                new Vector3(105f, RoadSurfaceY + 2f, 1080f),
                new Vector3(255f, RoadSurfaceY + 10f, 1740f),
                new Vector3(170f, RoadSurfaceY + 17f, 2460f),
                new Vector3(0f, RoadSurfaceY + 8f, 3350f)
            };
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
