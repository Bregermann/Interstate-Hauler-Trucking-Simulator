using LWS.InterstateHauler;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // One bounded runtime mesh, rebuilt at most twice a second; authored road geometry is untouched.
    public sealed class TruckTaxiSnowSurface : MonoBehaviour
    {
        public int RenderedCells { get; private set; }
        public Collider DepthCollider => snowCollider;
        private TruckTaxiSnowRegion region;
        private Mesh mesh;
        private MeshCollider snowCollider;
        private Material material;
        private GameObject root;
        private bool dirty;

        public void Initialize(TruckTaxiSnowRegion snow)
        {
            region = snow;
            root = new GameObject("Taxi Snow Road Strips");
            root.transform.SetParent(transform, false);
            var filter = root.AddComponent<MeshFilter>();
            var renderer = root.AddComponent<MeshRenderer>();
            snowCollider = root.AddComponent<MeshCollider>();
            root.AddComponent<TruckTaxiSurface>().isRoad = true;
            mesh = new Mesh { name = "Taxi dynamic snow depth" };
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.MarkDynamic();
            filter.sharedMesh = mesh;
            material = LwsWeatheradeMaterialFactory.CreateRoadSurfaceMaterial(
                "Taxi snow depth (Weatherade)", new Color(.89f, .93f, .95f), LwsWeatheradeSurfaceMaterialMode.Snow);
            renderer.sharedMaterial = material;
            dirty = true;
            Refresh();
        }

        public void MarkDirty() { dirty = true; }

        public void Refresh()
        {
            if (!dirty || region == null || mesh == null) return;
            int active = 0;
            foreach (TruckTaxiSnowRegion.Cell cell in region.Cells) if (cell.depth >= .02f) active++;
            var vertices = new Vector3[active * 8];
            var triangles = new int[active * 30];
            int v = 0, t = 0;
            foreach (TruckTaxiSnowRegion.Cell cell in region.Cells)
            {
                if (cell.depth < .02f) continue;
                Vector3 forward = cell.b - cell.a;
                Vector3 right = Vector3.Cross(Vector3.up, forward.normalized) * (cell.width * .5f);
                Vector3 a = root.transform.InverseTransformPoint(cell.a);
                Vector3 b = root.transform.InverseTransformPoint(cell.b);
                Vector3 localRight = root.transform.InverseTransformVector(right);
                Vector3 up = root.transform.InverseTransformVector(Vector3.up);
                float bottom = .015f, top = cell.depth + .015f;
                vertices[v] = a - localRight + up * bottom;
                vertices[v + 1] = a + localRight + up * bottom;
                vertices[v + 2] = b + localRight + up * bottom;
                vertices[v + 3] = b - localRight + up * bottom;
                for (int corner = 0; corner < 4; corner++) vertices[v + 4 + corner] = vertices[v + corner] + up * (top - bottom);
                AddQuad(triangles, ref t, v + 7, v + 6, v + 5, v + 4);
                AddQuad(triangles, ref t, v, v + 1, v + 5, v + 4);
                AddQuad(triangles, ref t, v + 1, v + 2, v + 6, v + 5);
                AddQuad(triangles, ref t, v + 2, v + 3, v + 7, v + 6);
                AddQuad(triangles, ref t, v + 3, v, v + 4, v + 7);
                v += 8;
            }
            snowCollider.sharedMesh = null;
            mesh.Clear();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            // PhysX cannot cook an empty mesh after the final snow cell melts/clears.
            if (active > 0) snowCollider.sharedMesh = mesh;
            RenderedCells = active;
            dirty = false;
        }

        private static void AddQuad(int[] indices, ref int offset, int a, int b, int c, int d)
        {
            indices[offset++] = a; indices[offset++] = b; indices[offset++] = c;
            indices[offset++] = a; indices[offset++] = c; indices[offset++] = d;
        }

        private void OnDestroy()
        {
            if (material != null) Destroy(material);
            if (mesh != null) Destroy(mesh);
            if (root != null) Destroy(root);
        }
    }
}
