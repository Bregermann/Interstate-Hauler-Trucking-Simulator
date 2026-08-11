using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CompassNavigatorPro {

    /// <summary>
    /// Renders the route polyline on the minimap as a single masked UI mesh.
    /// Geometry is built by the compass route updater and uploaded here; the mask
    /// texture provides the same circular/box clipping used by the rest of the minimap.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class CompassProRouteGraphic : Graphic {

        public Texture maskTexture;

        // Reused buffers filled by the route updater (no per-frame allocations)
        public readonly List<Vector3> vertices = new List<Vector3>();
        public readonly List<Vector2> uv0 = new List<Vector2>();    // x = arc length, y = cross axis [-1..1]
        public readonly List<Vector2> uv1 = new List<Vector2>();    // normalized minimap position [0..1] (mask uv)
        public readonly List<Vector2> uv2 = new List<Vector2>();    // x = normalized world arc along route [0..1]
        public readonly List<Color32> vertexColors = new List<Color32>();
        public readonly List<int> triangles = new List<int>();

        public override Texture mainTexture => maskTexture != null ? maskTexture : Texture2D.whiteTexture;

        public void MarkGeometryDirty () {
            SetVerticesDirty();
        }

        public void ClearGeometry () {
            vertices.Clear();
            uv0.Clear();
            uv1.Clear();
            uv2.Clear();
            vertexColors.Clear();
            triangles.Clear();
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh (VertexHelper vh) {
            vh.Clear();
            int count = vertices.Count;
            if (count < 3 || triangles.Count < 3) return;

            UIVertex vert = UIVertex.simpleVert;
            for (int i = 0; i < count; i++) {
                vert.position = vertices[i];
                vert.color = vertexColors[i];
                vert.uv0 = uv0[i];
                vert.uv1 = uv1[i];
                vert.uv2 = uv2[i];
                vh.AddVert(vert);
            }
            for (int i = 0; i < triangles.Count; i += 3) {
                vh.AddTriangle(triangles[i], triangles[i + 1], triangles[i + 2]);
            }
        }
    }
}
