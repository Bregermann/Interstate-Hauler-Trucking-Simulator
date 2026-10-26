using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LWS.TruckDockDiving.Editor
{
    // EzySlice remains the cutter. This restores only additional source-surface
    // data absent from that vendor's UV0/normal/tangent interpolation contract.
    public static class DockSplitSurfaceAttributes
    {
        sealed class Triangle { public int a, b, c; public Bounds bounds; }
        static VertexAttribute Uv(int channel) => (VertexAttribute)((int)VertexAttribute.TexCoord0 + channel);
        public static Mesh OwnMaterialSurface(Mesh source, int material)
        {
            if (source.blendShapeCount != 0 || source.HasVertexAttribute(VertexAttribute.BlendWeight) || source.HasVertexAttribute(VertexAttribute.BlendIndices)) throw new InvalidOperationException("Dock refuses deformable/weighted source surfaces.");
            var indices = source.GetTriangles(material); var map = new Dictionary<int, int>(); var original = new List<int>();
            for (int i = 0; i < indices.Length; i++) { int old = indices[i]; if (!map.TryGetValue(old, out int next)) { next = original.Count; map.Add(old, next); original.Add(old); } indices[i] = next; }
            var result = new Mesh { name = "Owned Dock material surface", indexFormat = original.Count > 65535 ? IndexFormat.UInt32 : source.indexFormat };
            try
            {
                var vertices = source.vertices; var normals = source.normals; var tangents = source.tangents; var colors = source.colors;
                result.SetVertices(original.ConvertAll(index => vertices[index]));
                if (normals.Length == vertices.Length) result.SetNormals(original.ConvertAll(index => normals[index]));
                if (tangents.Length == vertices.Length) result.SetTangents(original.ConvertAll(index => tangents[index]));
                if (colors.Length == vertices.Length) result.SetColors(original.ConvertAll(index => colors[index]));
                for (int channel = 0; channel < 8; channel++)
                {
                    var values = new List<Vector4>(); source.GetUVs(channel, values); if (values.Count == 0) continue;
                    if (values.Count != vertices.Length) throw new InvalidOperationException("Incomplete source UV channel.");
                    int dimension = source.GetVertexAttributeDimension(Uv(channel)); var selected = original.ConvertAll(index => values[index]);
                    if (dimension == 2) result.SetUVs(channel, selected.ConvertAll(value => new Vector2(value.x, value.y)));
                    else if (dimension == 3) result.SetUVs(channel, selected.ConvertAll(value => new Vector3(value.x, value.y, value.z)));
                    else if (dimension == 4) result.SetUVs(channel, selected);
                    else throw new InvalidOperationException("Unsupported one-component source UV channel.");
                }
                result.subMeshCount = 1; result.SetTriangles(indices, 0); result.RecalculateBounds(); return result;
            }
            catch { UnityEngine.Object.DestroyImmediate(result); throw; }
        }
        public static bool HasExtra(Mesh mesh)
        {
            if (mesh.HasVertexAttribute(VertexAttribute.Color)) return true;
            for (int channel = 1; channel < 8; channel++) if (mesh.HasVertexAttribute(Uv(channel))) return true;
            return false;
        }
        public static void Restore(Mesh output, Mesh source, int material, Matrix4x4 toBody)
        {
            if (!HasExtra(source)) return;
            if (source.blendShapeCount != 0 || source.HasVertexAttribute(VertexAttribute.BlendWeight) || source.HasVertexAttribute(VertexAttribute.BlendIndices)) throw new InvalidOperationException("Dock refuses cut deformable/weighted content rather than discarding attributes.");
            var vertices = source.vertices; var uv = source.uv; var normals = source.normals; var colors = source.colors;
            bool hasColors = colors.Length == vertices.Length; var extra = new List<Vector4>[8]; var dimensions = new int[8];
            for (int channel = 1; channel < 8; channel++)
            {
                extra[channel] = new List<Vector4>(); source.GetUVs(channel, extra[channel]);
                if (extra[channel].Count == 0) continue;
                dimensions[channel] = source.GetVertexAttributeDimension(Uv(channel));
                if (extra[channel].Count != vertices.Length || dimensions[channel] < 2 || dimensions[channel] > 4) throw new InvalidOperationException("Unsupported incomplete/one-component extra UV channel.");
            }
            if (source.HasVertexAttribute(VertexAttribute.Color) && !hasColors) throw new InvalidOperationException("Incomplete source color channel.");
            float epsilon = Mathf.Max(1e-5f, source.bounds.size.magnitude * 1e-6f);
            var triangles = new List<Triangle>(); var indices = source.GetTriangles(material);
            for (int i = 0; i < indices.Length; i += 3)
            {
                var triangle = new Triangle { a = indices[i], b = indices[i + 1], c = indices[i + 2] };
                triangle.bounds = new Bounds(vertices[triangle.a], Vector3.zero); triangle.bounds.Encapsulate(vertices[triangle.b]); triangle.bounds.Encapsulate(vertices[triangle.c]); triangle.bounds.Expand(epsilon * 4); triangles.Add(triangle);
            }
            var points = output.vertices; var outUv = output.uv; var outNormals = output.normals; var outTangents = output.tangents;
            var result = new List<Vector3>(); var resultUv = new List<Vector2>(); var resultNormals = new List<Vector3>(); var resultTangents = new List<Vector4>(); var resultColors = new List<Color>();
            var resultExtra = new List<Vector4>[8]; for (int channel = 1; channel < 8; channel++) resultExtra[channel] = new List<Vector4>();
            var resultTriangles = new int[output.subMeshCount][];
            for (int sub = 0; sub < output.subMeshCount; sub++)
            {
                var faces = output.GetTriangles(sub); resultTriangles[sub] = new int[faces.Length];
                for (int face = 0; face < faces.Length; face += 3)
                {
                    Triangle selected = null; Vector3[] selectedWeights = null;
                    if (sub == 0)
                    {
                        var centroid = (points[faces[face]] + points[faces[face + 1]] + points[faces[face + 2]]) / 3;
                        foreach (var candidate in triangles)
                        {
                            if (!candidate.bounds.Contains(centroid)) continue; var weights = new Vector3[3]; bool match = true;
                            for (int corner = 0; corner < 3 && match; corner++)
                            {
                                int index = faces[face + corner]; match = Weights(points[index], vertices[candidate.a], vertices[candidate.b], vertices[candidate.c], epsilon, out weights[corner]);
                                if (match && uv.Length == vertices.Length && outUv.Length == points.Length) match = Vector2.Distance(uv[candidate.a] * weights[corner].x + uv[candidate.b] * weights[corner].y + uv[candidate.c] * weights[corner].z, outUv[index]) <= 2e-4f;
                                if (match && normals.Length == vertices.Length && outNormals.Length == points.Length)
                                { var expected = (normals[candidate.a] * weights[corner].x + normals[candidate.b] * weights[corner].y + normals[candidate.c] * weights[corner].z).normalized; match = expected.sqrMagnitude < .01f || outNormals[index].sqrMagnitude < .01f || Vector3.Dot(expected, outNormals[index].normalized) >= .9999f; }
                            }
                            if (!match) continue;
                            if (selected != null && !Equivalent(selected, selectedWeights, candidate, weights, extra, colors, hasColors)) throw new InvalidOperationException("Ambiguous conflicting extra source attributes at a UV0/normal seam.");
                            selected = candidate; selectedWeights = weights;
                        }
                        if (selected == null) throw new InvalidOperationException("A cut exterior triangle could not be matched to its original material surface.");
                    }
                    for (int corner = 0; corner < 3; corner++)
                    {
                        int index = faces[face + corner]; resultTriangles[sub][face + corner] = result.Count; result.Add(points[index]);
                        if (outUv.Length == points.Length) resultUv.Add(outUv[index]); if (outNormals.Length == points.Length) resultNormals.Add(outNormals[index]); if (outTangents.Length == points.Length) resultTangents.Add(outTangents[index]);
                        var cap = toBody.MultiplyPoint3x4(points[index]);
                        for (int channel = 1; channel < 8; channel++) if (extra[channel].Count > 0) resultExtra[channel].Add(sub == 0 ? Interpolate(extra[channel], selected, selectedWeights[corner]) : new Vector4(cap.x, cap.z, 0, 0));
                        if (hasColors) resultColors.Add(sub == 0 ? Interpolate(colors, selected, selectedWeights[corner]) : Color.white);
                    }
                }
            }
            output.Clear(); output.indexFormat = result.Count > 65535 ? IndexFormat.UInt32 : source.indexFormat; output.SetVertices(result);
            if (resultUv.Count > 0) output.SetUVs(0, resultUv); if (resultNormals.Count > 0) output.SetNormals(resultNormals); if (resultTangents.Count > 0) output.SetTangents(resultTangents); if (hasColors) output.SetColors(resultColors);
            for (int channel = 1; channel < 8; channel++)
                if (dimensions[channel] == 2) output.SetUVs(channel, resultExtra[channel].ConvertAll(value => new Vector2(value.x, value.y)));
                else if (dimensions[channel] == 3) output.SetUVs(channel, resultExtra[channel].ConvertAll(value => new Vector3(value.x, value.y, value.z)));
                else if (dimensions[channel] == 4) output.SetUVs(channel, resultExtra[channel]);
            output.subMeshCount = resultTriangles.Length; for (int sub = 0; sub < resultTriangles.Length; sub++) output.SetTriangles(resultTriangles[sub], sub, false); output.RecalculateBounds();
        }
        static Vector4 Interpolate(List<Vector4> channel, Triangle triangle, Vector3 weights) => channel[triangle.a] * weights.x + channel[triangle.b] * weights.y + channel[triangle.c] * weights.z;
        static Color Interpolate(Color[] channel, Triangle triangle, Vector3 weights) => channel[triangle.a] * weights.x + channel[triangle.b] * weights.y + channel[triangle.c] * weights.z;
        static bool Equivalent(Triangle a, Vector3[] wa, Triangle b, Vector3[] wb, List<Vector4>[] extra, Color[] colors, bool hasColors)
        {
            for (int corner = 0; corner < 3; corner++)
            {
                for (int channel = 1; channel < 8; channel++) if (extra[channel].Count > 0 && Vector4.Distance(Interpolate(extra[channel], a, wa[corner]), Interpolate(extra[channel], b, wb[corner])) > 1e-5f) return false;
                if (hasColors) { Color ca = Interpolate(colors, a, wa[corner]), cb = Interpolate(colors, b, wb[corner]); if (Mathf.Max(Mathf.Abs(ca.r - cb.r), Mathf.Abs(ca.g - cb.g), Mathf.Abs(ca.b - cb.b), Mathf.Abs(ca.a - cb.a)) > 1e-5f) return false; }
            }
            return true;
        }
        static bool Weights(Vector3 point, Vector3 a, Vector3 b, Vector3 c, float epsilon, out Vector3 weights)
        {
            weights = default; var normal = Vector3.Cross(b - a, c - a); if (normal.sqrMagnitude < 1e-20f || Mathf.Abs(Vector3.Dot(point - a, normal.normalized)) > epsilon) return false;
            double x0 = b.x - a.x, y0 = b.y - a.y, z0 = b.z - a.z, x1 = c.x - a.x, y1 = c.y - a.y, z1 = c.z - a.z, x2 = point.x - a.x, y2 = point.y - a.y, z2 = point.z - a.z;
            double d00 = x0 * x0 + y0 * y0 + z0 * z0, d01 = x0 * x1 + y0 * y1 + z0 * z1, d11 = x1 * x1 + y1 * y1 + z1 * z1, d20 = x2 * x0 + y2 * y0 + z2 * z0, d21 = x2 * x1 + y2 * y1 + z2 * z1, denominator = d00 * d11 - d01 * d01;
            if (Math.Abs(denominator) < 1e-24) return false; float v = (float)((d11 * d20 - d01 * d21) / denominator), w = (float)((d00 * d21 - d01 * d20) / denominator); weights = new Vector3(1 - v - w, v, w);
            return weights.x >= -2e-4f && weights.y >= -2e-4f && weights.z >= -2e-4f && weights.x <= 1.0002f && weights.y <= 1.0002f && weights.z <= 1.0002f;
        }
    }
}
