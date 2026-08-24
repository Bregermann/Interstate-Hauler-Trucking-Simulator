using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.InterstateHauler
{
    [Serializable]
    public sealed class LwsInterstateCrossSectionProfile
    {
        public int lanesPerDirection = 2;
        public float laneWidthMeters = 3.7f;
        public float leftShoulderWidthMeters = 1.2f;
        public float rightShoulderWidthMeters = 3.0f;
        public float medianWidthMeters = 14.0f;
        public float roadSurfaceY = 0.55f;
        public float groundTotalWidthMeters = 170f;
        public float outerSlopeWidthMeters = 7.5f;
        public float ditchWidthMeters = 5.0f;
        public float ditchDepthMeters = 0.85f;
        public float guardrailOffsetFromRoadEdgeMeters = 1.05f;
        public float guardrailHeightMeters = 0.8f;
        public float guardrailWidthMeters = 0.28f;
        public float guardrailColliderHeightMeters = 1.2f;
        public float guardrailMaxSegmentLengthMeters = 160f;

        public float LanePavementWidthMeters => Mathf.Max(1, lanesPerDirection) * Mathf.Max(1f, laneWidthMeters);
        public float CarriagewayWidthMeters => LanePavementWidthMeters + Mathf.Max(0f, leftShoulderWidthMeters) + Mathf.Max(0f, rightShoulderWidthMeters);
        public float CarriagewayOffsetMeters => Mathf.Max(0f, medianWidthMeters) * 0.5f + CarriagewayWidthMeters * 0.5f;

        public static LwsInterstateCrossSectionProfile CreateValidationDefault()
        {
            return new LwsInterstateCrossSectionProfile();
        }

        public bool Validate(out string message)
        {
            if (lanesPerDirection <= 0)
            {
                message = "Interstate cross-section profile requires at least one lane per direction.";
                return false;
            }

            if (laneWidthMeters <= 0f ||
                leftShoulderWidthMeters < 0f ||
                rightShoulderWidthMeters < 0f ||
                medianWidthMeters < 0f ||
                groundTotalWidthMeters <= CarriagewayWidthMeters * 2f + medianWidthMeters ||
                outerSlopeWidthMeters < 0f ||
                ditchWidthMeters < 0f ||
                ditchDepthMeters < 0f ||
                guardrailHeightMeters <= 0f ||
                guardrailWidthMeters <= 0f ||
                guardrailMaxSegmentLengthMeters <= 0f)
            {
                message = "Interstate cross-section profile has invalid dimensions.";
                return false;
            }

            message = "Interstate cross-section profile is valid.";
            return true;
        }
    }

    [DisallowMultipleComponent]
    public sealed class LwsRoadsideRuntimeRoot : MonoBehaviour
    {
        [SerializeField] private string buildId = "IH_ROADSIDE";
        [SerializeField] private int shoulderStripCount;
        [SerializeField] private int ditchStripCount;
        [SerializeField] private int guardrailSegmentCount;
        [SerializeField] private int groundMeshCount;
        [SerializeField] private int medianMeshCount;
        [SerializeField] private bool built;

        public string BuildId => buildId;
        public int ShoulderStripCount => shoulderStripCount;
        public int DitchStripCount => ditchStripCount;
        public int GuardrailSegmentCount => guardrailSegmentCount;
        public int GroundMeshCount => groundMeshCount;
        public int MedianMeshCount => medianMeshCount;
        public bool Built => built;

        public void Configure(
            string newBuildId,
            int shoulders,
            int ditches,
            int guardrails,
            int ground,
            int median)
        {
            buildId = string.IsNullOrWhiteSpace(newBuildId) ? buildId : newBuildId;
            shoulderStripCount = Mathf.Max(0, shoulders);
            ditchStripCount = Mathf.Max(0, ditches);
            guardrailSegmentCount = Mathf.Max(0, guardrails);
            groundMeshCount = Mathf.Max(0, ground);
            medianMeshCount = Mathf.Max(0, median);
            built = true;
        }
    }

    public static class LwsInterstateRoadsideBuilder
    {
        private const string RootSuffix = "Roadside Cross Section";

        public static LwsRoadsideRuntimeRoot BuildStraightPairedInterstate(
            Transform parent,
            string buildId,
            float startZ,
            float endZ,
            LwsInterstateCrossSectionProfile profile = null)
        {
            profile ??= LwsInterstateCrossSectionProfile.CreateValidationDefault();
            float end = Mathf.Max(startZ + 1f, endZ);
            float offset = profile.CarriagewayOffsetMeters;
            var positive = new[]
            {
                new Vector3(offset, profile.roadSurfaceY, startZ),
                new Vector3(offset, profile.roadSurfaceY, end)
            };
            var negative = new[]
            {
                new Vector3(-offset, profile.roadSurfaceY, startZ),
                new Vector3(-offset, profile.roadSurfaceY, end)
            };

            return BuildPairedInterstate(parent, buildId, positive, negative, profile);
        }

        public static LwsRoadsideRuntimeRoot BuildPairedInterstate(
            Transform parent,
            string buildId,
            IReadOnlyList<Vector3> positiveCarriageway,
            IReadOnlyList<Vector3> negativeCarriageway,
            LwsInterstateCrossSectionProfile profile = null)
        {
            profile ??= LwsInterstateCrossSectionProfile.CreateValidationDefault();
            if (parent == null || !profile.Validate(out _))
            {
                return null;
            }

            string rootName = $"{Sanitize(buildId)} {RootSuffix}";
            Transform existing = parent.Find(rootName);
            if (existing != null)
            {
                return existing.GetComponent<LwsRoadsideRuntimeRoot>() ?? existing.gameObject.AddComponent<LwsRoadsideRuntimeRoot>();
            }

            var rootObject = new GameObject(rootName);
            rootObject.transform.SetParent(parent, false);
            LwsRoadsideRuntimeRoot runtime = rootObject.AddComponent<LwsRoadsideRuntimeRoot>();

            Material ground = CreateRuntimeMaterial("IH Cross Section Ground", new Color(0.13f, 0.23f, 0.12f, 1f));
            Material shoulder = CreateRuntimeMaterial("IH Cross Section Shoulder", new Color(0.34f, 0.34f, 0.31f, 1f));
            Material ditch = CreateRuntimeMaterial("IH Cross Section Ditch", new Color(0.10f, 0.18f, 0.09f, 1f));
            Material median = CreateRuntimeMaterial("IH Cross Section Median", new Color(0.19f, 0.24f, 0.14f, 1f));
            Material guardrail = CreateRuntimeMaterial("IH Cross Section Guardrail", new Color(0.58f, 0.60f, 0.57f, 1f));

            Bounds bounds = CalculateBounds(positiveCarriageway, negativeCarriageway, profile);
            int groundCount = CreateGround(rootObject.transform, bounds, profile, ground);
            int medianCount = CreateMedian(rootObject.transform, bounds, profile, median);
            int shoulders = 0;
            int ditches = 0;
            int guardrails = 0;

            BuildCarriageway(rootObject.transform, "Positive", positiveCarriageway, profile, shoulder, ditch, guardrail, ref shoulders, ref ditches, ref guardrails);
            BuildCarriageway(rootObject.transform, "Negative", negativeCarriageway, profile, shoulder, ditch, guardrail, ref shoulders, ref ditches, ref guardrails);

            runtime.Configure(buildId, shoulders, ditches, guardrails, groundCount, medianCount);
            return runtime;
        }

        public static LwsRoadsideRuntimeRoot BuildFromRoadGraph(
            Transform parent,
            string buildId,
            LwsRoadGraph graph,
            LwsInterstateCrossSectionProfile profile = null)
        {
            if (graph == null || graph.edges == null || graph.edges.Count == 0)
            {
                return null;
            }

            List<Vector3> first = null;
            List<Vector3> second = null;
            for (int i = 0; i < graph.edges.Count; i++)
            {
                LwsRoadEdge edge = graph.edges[i];
                if (edge == null || edge.roadClass != LwsRoadClass.Interstate || edge.samples == null || edge.samples.Count < 2)
                {
                    continue;
                }

                var samples = new List<Vector3>(edge.samples.Count);
                for (int sampleIndex = 0; sampleIndex < edge.samples.Count; sampleIndex++)
                {
                    samples.Add(edge.samples[sampleIndex].position);
                }

                if (first == null)
                {
                    first = samples;
                }
                else
                {
                    second = samples;
                    break;
                }
            }

            return first != null && second != null
                ? BuildPairedInterstate(parent, buildId, first, second, profile)
                : null;
        }

        private static void BuildCarriageway(
            Transform parent,
            string label,
            IReadOnlyList<Vector3> samples,
            LwsInterstateCrossSectionProfile profile,
            Material shoulder,
            Material ditch,
            Material guardrail,
            ref int shoulders,
            ref int ditches,
            ref int guardrails)
        {
            if (samples == null || samples.Count < 2)
            {
                return;
            }

            float halfRoad = profile.CarriagewayWidthMeters * 0.5f;
            float halfPavement = profile.LanePavementWidthMeters * 0.5f;
            shoulders += CreateRibbon(parent, $"{label} Left Shoulder", samples, -halfRoad, -halfPavement, 0.025f, 0.025f, shoulder, true) ? 1 : 0;
            shoulders += CreateRibbon(parent, $"{label} Right Shoulder", samples, halfPavement, halfRoad, 0.025f, 0.025f, shoulder, true) ? 1 : 0;

            float slopeOuter = halfRoad + profile.outerSlopeWidthMeters;
            float ditchOuter = slopeOuter + profile.ditchWidthMeters;
            ditches += CreateRibbon(parent, $"{label} Left Slope Ditch", samples, -halfRoad, -slopeOuter, -0.06f, -profile.ditchDepthMeters, ditch, true) ? 1 : 0;
            ditches += CreateRibbon(parent, $"{label} Right Slope Ditch", samples, halfRoad, slopeOuter, -0.06f, -profile.ditchDepthMeters, ditch, true) ? 1 : 0;
            ditches += CreateRibbon(parent, $"{label} Left Outer Ground", samples, -slopeOuter, -ditchOuter, -profile.ditchDepthMeters, -profile.ditchDepthMeters * 0.55f, ditch, true) ? 1 : 0;
            ditches += CreateRibbon(parent, $"{label} Right Outer Ground", samples, slopeOuter, ditchOuter, -profile.ditchDepthMeters, -profile.ditchDepthMeters * 0.55f, ditch, true) ? 1 : 0;

            guardrails += CreateGuardrails(parent, $"{label} Left Guardrail", samples, -halfRoad - profile.guardrailOffsetFromRoadEdgeMeters, profile, guardrail);
            guardrails += CreateGuardrails(parent, $"{label} Right Guardrail", samples, halfRoad + profile.guardrailOffsetFromRoadEdgeMeters, profile, guardrail);
        }

        private static int CreateGround(Transform parent, Bounds bounds, LwsInterstateCrossSectionProfile profile, Material material)
        {
            float width = Mathf.Max(profile.groundTotalWidthMeters, bounds.size.x + profile.outerSlopeWidthMeters * 4f);
            float length = Mathf.Max(1f, bounds.size.z + 10f);
            var go = new GameObject("IH Roadside Ground Base");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(bounds.center.x, profile.roadSurfaceY - profile.ditchDepthMeters * 0.55f, bounds.center.z);
            Mesh mesh = CreateRectMesh(width, length);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
            return 1;
        }

        private static int CreateMedian(Transform parent, Bounds bounds, LwsInterstateCrossSectionProfile profile, Material material)
        {
            var go = new GameObject("IH Roadside Median Ground");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, profile.roadSurfaceY - 0.025f, bounds.center.z);
            Mesh mesh = CreateRectMesh(Mathf.Max(1f, profile.medianWidthMeters), Mathf.Max(1f, bounds.size.z + 6f));
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
            return 1;
        }

        private static bool CreateRibbon(
            Transform parent,
            string name,
            IReadOnlyList<Vector3> samples,
            float innerOffset,
            float outerOffset,
            float innerYOffset,
            float outerYOffset,
            Material material,
            bool addCollider)
        {
            if (samples == null || samples.Count < 2)
            {
                return false;
            }

            Mesh mesh = CreateOffsetRibbonMesh(samples, innerOffset, outerOffset, innerYOffset, outerYOffset);
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            if (addCollider)
            {
                go.AddComponent<MeshCollider>().sharedMesh = mesh;
            }

            return true;
        }

        private static int CreateGuardrails(
            Transform parent,
            string name,
            IReadOnlyList<Vector3> samples,
            float offset,
            LwsInterstateCrossSectionProfile profile,
            Material material)
        {
            int created = 0;
            float maxSegment = Mathf.Max(8f, profile.guardrailMaxSegmentLengthMeters);
            for (int i = 0; i < samples.Count - 1; i++)
            {
                Vector3 start = OffsetPoint(samples, i, offset) + Vector3.up * (profile.guardrailHeightMeters * 0.5f + 0.04f);
                Vector3 end = OffsetPoint(samples, i + 1, offset) + Vector3.up * (profile.guardrailHeightMeters * 0.5f + 0.04f);
                float distance = Vector3.Distance(start, end);
                int pieces = Mathf.Max(1, Mathf.CeilToInt(distance / maxSegment));
                for (int piece = 0; piece < pieces; piece++)
                {
                    float t0 = piece / (float)pieces;
                    float t1 = (piece + 1) / (float)pieces;
                    Vector3 a = Vector3.Lerp(start, end, t0);
                    Vector3 b = Vector3.Lerp(start, end, t1);
                    Vector3 forward = b - a;
                    if (forward.sqrMagnitude <= 0.0001f)
                    {
                        continue;
                    }

                    var segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    segment.name = $"{name} {i:000}_{piece:00}";
                    segment.transform.SetParent(parent, false);
                    segment.transform.localPosition = (a + b) * 0.5f;
                    segment.transform.localRotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
                    segment.transform.localScale = new Vector3(
                        Mathf.Max(0.05f, profile.guardrailWidthMeters),
                        Mathf.Max(profile.guardrailHeightMeters, profile.guardrailColliderHeightMeters),
                        forward.magnitude);
                    Renderer renderer = segment.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.sharedMaterial = material;
                    }

                    created++;
                }
            }

            return created;
        }

        private static Mesh CreateOffsetRibbonMesh(
            IReadOnlyList<Vector3> samples,
            float innerOffset,
            float outerOffset,
            float innerYOffset,
            float outerYOffset)
        {
            int count = samples.Count;
            var vertices = new Vector3[count * 2];
            var uvs = new Vector2[count * 2];
            var triangles = new int[(count - 1) * 6];
            float distance = 0f;

            for (int i = 0; i < count; i++)
            {
                Vector3 right = ResolveRight(samples, i);
                vertices[i * 2] = samples[i] + right * innerOffset + Vector3.up * innerYOffset;
                vertices[i * 2 + 1] = samples[i] + right * outerOffset + Vector3.up * outerYOffset;
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

            var mesh = new Mesh { name = "IH Roadside Cross Section Ribbon" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateRectMesh(float width, float length)
        {
            float x = width * 0.5f;
            float z = length * 0.5f;
            var mesh = new Mesh { name = "IH Roadside Rect Mesh" };
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
            return mesh;
        }

        private static Bounds CalculateBounds(
            IReadOnlyList<Vector3> first,
            IReadOnlyList<Vector3> second,
            LwsInterstateCrossSectionProfile profile)
        {
            bool initialized = false;
            Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
            Encapsulate(first, ref initialized, ref bounds);
            Encapsulate(second, ref initialized, ref bounds);
            if (!initialized)
            {
                bounds = new Bounds(Vector3.zero, Vector3.one);
            }

            bounds.Expand(new Vector3(profile.groundTotalWidthMeters, 0f, 12f));
            return bounds;
        }

        private static void Encapsulate(IReadOnlyList<Vector3> samples, ref bool initialized, ref Bounds bounds)
        {
            if (samples == null)
            {
                return;
            }

            for (int i = 0; i < samples.Count; i++)
            {
                if (!initialized)
                {
                    bounds = new Bounds(samples[i], Vector3.zero);
                    initialized = true;
                }
                else
                {
                    bounds.Encapsulate(samples[i]);
                }
            }
        }

        private static Vector3 OffsetPoint(IReadOnlyList<Vector3> samples, int index, float offset)
        {
            return samples[index] + ResolveRight(samples, index) * offset;
        }

        private static Vector3 ResolveRight(IReadOnlyList<Vector3> samples, int index)
        {
            Vector3 forward = ResolveForward(samples, index);
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            return right.sqrMagnitude > 0.0001f ? right.normalized : Vector3.right;
        }

        private static Vector3 ResolveForward(IReadOnlyList<Vector3> samples, int index)
        {
            if (samples == null || samples.Count < 2)
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

        private static string Sanitize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "IH_ROADSIDE" : value.Trim();
        }
    }
}
