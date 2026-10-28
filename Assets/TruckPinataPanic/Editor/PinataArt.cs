using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace LWS.TruckPinataPanic.Editor
{
    // Original authored meshes for the missing paper-star/candy models. These
    // are saved art assets; normal startup never regenerates an environment.
    public static class PinataArt
    {
        public const string Root = "Assets/TruckPinataPanic";
        sealed class Surface
        {
            readonly List<Vector3> vertices = new List<Vector3>();
            readonly List<Vector2> uv = new List<Vector2>();
            readonly List<int> triangles = new List<int>();
            public void Triangle(Vector3 a, Vector3 b, Vector3 c)
            {
                int n = vertices.Count; vertices.Add(a); vertices.Add(b); vertices.Add(c);
                uv.Add(new Vector2(a.x, a.y) * .25f); uv.Add(new Vector2(b.x, b.y) * .25f); uv.Add(new Vector2(c.x, c.y) * .25f);
                triangles.Add(n); triangles.Add(n + 1); triangles.Add(n + 2);
            }
            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d) { Triangle(a, b, c); Triangle(a, c, d); }
            public Mesh Finish(string name)
            {
                var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
                mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
            }
        }
        static Mesh Save(string name, Func<Mesh> build)
        {
            string directory = Root + "/Art/Meshes"; Directory.CreateDirectory(directory);
            string path = directory + "/" + name + ".asset"; var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) return existing;
            var mesh = build(); mesh.name = name; AssetDatabase.CreateAsset(mesh, path); return mesh;
        }
        static Vector3 Radial(float angle, float radius, float z) => new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, z);
        public static Mesh PaperWedge(int wedge) => Save("Seven point shell panel " + wedge, () =>
        {
            var s = new Surface(); float a = Mathf.PI / 2 + wedge * Mathf.PI * 2 / 7, half = Mathf.PI / 7;
            var polygon = new[] { Vector3.zero, Radial(a - half, 2.45f, 0), Radial(a, 5.1f, 0), Radial(a + half, 2.45f, 0) };
            for (int side = -1; side <= 1; side += 2)
            {
                var center = new Vector3(0, 0, side * 1.4f);
                for (int k = 1; k < 3; k++)
                {
                    var p = polygon[k] + Vector3.forward * (side * (k == 0 ? 1.4f : .65f));
                    var q = polygon[k + 1] + Vector3.forward * (side * .65f);
                    if (side == 1) s.Triangle(center, p, q); else s.Triangle(center, q, p);
                }
            }
            for (int k = 0; k < polygon.Length; k++)
            {
                int j = (k + 1) % polygon.Length; var p = polygon[k]; var q = polygon[j];
                float depthP = k == 0 ? 1.4f : .65f, depthQ = j == 0 ? 1.4f : .65f;
                s.Quad(p - Vector3.forward * depthP, q - Vector3.forward * depthQ, q + Vector3.forward * depthQ, p + Vector3.forward * depthP);
            }
            return s.Finish("Closed folded paper wedge");
        });
        public static Mesh Fringe(int layer) => Save("Hand cut staggered tissue fringe " + layer, () =>
        {
            var s = new Surface();
            for (int side = -1; side <= 1; side += 2) for (int row = 0; row < 12; row++)
            {
                float y = -2.35f + row * .4f; float radius = Mathf.Sqrt(Mathf.Max(0, 2.7f * 2.7f - y * y));
                for (int k = 0; k < 16; k++)
                {
                    float x = Mathf.Lerp(-radius, radius, (k + .35f * (row % 2)) / 16);
                    if (k % 3 != layer) continue;
                    float width = Mathf.Min(.32f, radius / 7), depth = side * (1.48f + row % 2 * .035f);
                    float drop = .32f + .06f * Mathf.Sin(k * 4.3f + row * .7f);
                    var a = new Vector3(x, y, depth); var b = a + Vector3.right * width;
                    var c = b + new Vector3(-.025f, -drop, side * .1f); var d = a + new Vector3(.025f, -drop, side * .1f);
                    s.Quad(a, b, c, d); s.Quad(d, c, b, a);
                }
            }
            return s.Finish("Layered cut paper fringe");
        });
        public static Mesh Tear(int stage) => Save("Progressive torn kraft paper " + stage, () =>
        {
            var s = new Surface();
            for (int side = -1; side <= 1; side += 2) for (int k = 0; k <= stage; k++)
            {
                var a = new Vector3(-1.2f + k * .7f, .8f - k * .65f, side * 1.55f);
                var b = a + new Vector3(.5f, -.15f, 0); var c = a + new Vector3(.12f, -.7f, 0);
                s.Triangle(a, b, c); s.Triangle(a, c, b);
            }
            return s.Finish("Jagged paper tear");
        });
        // Lathed faceted sweet with crimped wrapper ends, modeled in the mesh.
        public static Mesh WrappedSweet(bool explosive) => Save(explosive ? "Fused liquorice sweet" : "Twist wrapped boiled sweet", () =>
        {
            var s = new Surface(); int slices = 20;
            float[] x = { -.95f, -.66f, -.49f, -.43f, -.25f, .25f, .43f, .49f, .66f, .95f };
            float[] radius = { .42f, .19f, .12f, .4f, .49f, .49f, .4f, .12f, .19f, .42f };
            for (int row = 0; row < x.Length - 1; row++) for (int k = 0; k < slices; k++)
            {
                Vector3 P(int r, int j) { float a = j * Mathf.PI * 2 / slices + x[r] * 2; float crimp = (r == 0 || r == x.Length - 1) ? 1 + .15f * Mathf.Cos(j * Mathf.PI) : 1; return new Vector3(x[r], Mathf.Sin(a) * radius[r] * crimp, Mathf.Cos(a) * radius[r] * crimp); }
                s.Quad(P(row, k), P(row + 1, k), P(row + 1, k + 1), P(row, k + 1));
            }
            for (int k = 0; k < slices; k++) { float a = k * Mathf.PI * 2 / slices, b = (k + 1) * Mathf.PI * 2 / slices; s.Triangle(new Vector3(-.95f, 0, 0), new Vector3(-.95f, Mathf.Sin(a) * .42f, Mathf.Cos(a) * .42f), new Vector3(-.95f, Mathf.Sin(b) * .42f, Mathf.Cos(b) * .42f)); s.Triangle(new Vector3(.95f, 0, 0), new Vector3(.95f, Mathf.Sin(b) * .42f, Mathf.Cos(b) * .42f), new Vector3(.95f, Mathf.Sin(a) * .42f, Mathf.Cos(a) * .42f)); }
            return s.Finish("Sealed twist wrapped sweet");
        });
        public static Mesh GoldenSweet() => Save("Pressed gold star confection", () =>
        {
            var s = new Surface();
            for (int k = 0; k < 10; k++)
            {
                float a = k * Mathf.PI / 5, b = (k + 1) * Mathf.PI / 5;
                var p = Radial(a, k % 2 == 0 ? .78f : .38f, .22f); var q = Radial(b, k % 2 == 0 ? .38f : .78f, .22f);
                s.Triangle(new Vector3(0, 0, .3f), p, q); s.Triangle(new Vector3(0, 0, -.3f), new Vector3(q.x, q.y, -.22f), new Vector3(p.x, p.y, -.22f));
                s.Quad(p, new Vector3(p.x, p.y, -.22f), new Vector3(q.x, q.y, -.22f), q);
            }
            return s.Finish("Embossed golden star");
        });
        public static Mesh Tube(string name, float radius, float length, int sides = 20) => Save(name, () =>
        {
            var s = new Surface();
            for (int k = 0; k < sides; k++)
            {
                float a = k * 2 * Mathf.PI / sides, b = (k + 1) * 2 * Mathf.PI / sides;
                var p = new Vector3(Mathf.Cos(a) * radius, -length / 2, Mathf.Sin(a) * radius); var q = new Vector3(Mathf.Cos(b) * radius, -length / 2, Mathf.Sin(b) * radius);
                s.Quad(p, p + Vector3.up * length, q + Vector3.up * length, q);
                s.Triangle(Vector3.down * length / 2, p, q); s.Triangle(Vector3.up * length / 2, q + Vector3.up * length, p + Vector3.up * length);
            }
            return s.Finish(name);
        });
        public static Mesh Ring(string name, float inner, float outer, float height, int steps = 128) => Save(name, () =>
        {
            var s = new Surface();
            for (int k = 0; k < steps; k++)
            {
                Vector3 P(int j, float r, float y) { float a = j * Mathf.PI * 2 / steps; return new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r); }
                s.Quad(P(k, inner, height), P(k + 1, inner, height), P(k + 1, outer, height), P(k, outer, height));
                s.Quad(P(k, outer, 0), P(k, outer, height), P(k + 1, outer, height), P(k + 1, outer, 0));
                s.Quad(P(k + 1, inner, 0), P(k + 1, inner, height), P(k, inner, height), P(k, inner, 0));
            }
            return s.Finish(name);
        });
        public static Mesh Pennants() => Save("Sewn scalloped carnival bunting", () =>
        {
            var s = new Surface(); for (int i = 0; i < 12; i++) { float x = -6 + i; float y = -.8f * (1 - x * x / 36); var a = new Vector3(x, y, 0); var b = a + Vector3.right * .8f; var c = a + new Vector3(.4f, -1.1f, .03f); s.Triangle(a, c, b); s.Triangle(b, c, a); } return s.Finish("Sewn pennants");
        });
        public static Mesh Canopy() => Save("Stitched scalloped kiosk canopy", () =>
        {
            var s = new Surface(); for (int i = 0; i < 20; i++) { float x = -4 + i * .4f; float droop = .13f + i % 2 * .09f; s.Quad(new Vector3(x, 1.6f, 0), new Vector3(x + .4f, 1.6f, 0), new Vector3(x + .4f, -.2f, -3), new Vector3(x, -.2f, -3)); s.Quad(new Vector3(x, -.2f, -3), new Vector3(x + .4f, -.2f, -3), new Vector3(x + .4f, -.2f - droop, -3), new Vector3(x, -.2f - droop, -3)); s.Quad(new Vector3(x, 1.6f, 0), new Vector3(x, -.2f, 3), new Vector3(x + .4f, -.2f, 3), new Vector3(x + .4f, 1.6f, 0)); } return s.Finish("Scalloped canopy");
        });
        public static GameObject Model(string name, Transform parent, Mesh mesh, Material material, Vector3 localPosition)
        {
            var root = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); root.transform.SetParent(parent, false); root.transform.localPosition = localPosition;
            root.GetComponent<MeshFilter>().sharedMesh = mesh; root.GetComponent<MeshRenderer>().sharedMaterial = material; return root;
        }
        public static Texture2D PaperTexture()
        {
            string path = Root + "/Art/Hand laid paper fibers.asset"; var prior = AssetDatabase.LoadAssetAtPath<Texture2D>(path); if (prior != null) return prior;
            var texture = new Texture2D(128, 128, TextureFormat.RGBA32, true) { name = "Original hand laid tissue paper fibers", wrapMode = TextureWrapMode.Repeat };
            for (int y = 0; y < 128; y++) for (int x = 0; x < 128; x++) { float n = Mathf.PerlinNoise(x * .24f, y * .78f) * .14f + .79f + ((y % 17 == 0 || x % 43 == 0) ? .04f : 0); texture.SetPixel(x, y, new Color(n, n, n, 1)); }
            texture.Apply(); AssetDatabase.CreateAsset(texture, path); return texture;
        }
    }
}
