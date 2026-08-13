using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LWS.InterstateHauler
{
    public sealed class LwsGpsMapGraphic : MaskableGraphic
    {
        private readonly List<Vector2> _routePoints = new List<Vector2>();
        private bool _hasRoute;

        public void SetRoute(IReadOnlyList<Vector3> worldPoints, Vector3 playerPosition, Vector3 playerForward)
        {
            _routePoints.Clear();
            _hasRoute = worldPoints != null && worldPoints.Count > 1;
            if (_hasRoute)
            {
                ProjectRoute(worldPoints, playerPosition, playerForward);
            }

            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = rectTransform.rect;
            AddQuad(vh, rect, new Color32(10, 20, 24, 255));
            AddGrid(vh, rect);

            if (_hasRoute)
            {
                for (int i = 1; i < _routePoints.Count; i++)
                {
                    AddLine(vh, rect, _routePoints[i - 1], _routePoints[i], 0.025f, new Color32(0, 180, 255, 255));
                }
            }

            AddPlayerMarker(vh, rect);
        }

        private void ProjectRoute(IReadOnlyList<Vector3> worldPoints, Vector3 playerPosition, Vector3 playerForward)
        {
            Vector3 forward = playerForward.sqrMagnitude > 0.0001f ? playerForward.normalized : Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            const float metersVisible = 520f;

            for (int i = 0; i < worldPoints.Count; i++)
            {
                Vector3 delta = worldPoints[i] - playerPosition;
                float x = Vector3.Dot(delta, right) / metersVisible + 0.5f;
                float y = Vector3.Dot(delta, forward) / metersVisible + 0.35f;
                _routePoints.Add(new Vector2(x, y));
            }
        }

        private static void AddGrid(VertexHelper vh, Rect rect)
        {
            Color32 color = new Color32(35, 54, 60, 180);
            for (int i = 1; i < 4; i++)
            {
                float t = i / 4f;
                AddLine(vh, rect, new Vector2(t, 0f), new Vector2(t, 1f), 0.004f, color);
                AddLine(vh, rect, new Vector2(0f, t), new Vector2(1f, t), 0.004f, color);
            }
        }

        private static void AddPlayerMarker(VertexHelper vh, Rect rect)
        {
            Vector2 center = RectPoint(rect, new Vector2(0.5f, 0.35f));
            float h = rect.height * 0.08f;
            float w = h * 0.65f;
            int start = vh.currentVertCount;
            Color32 color = new Color32(255, 245, 120, 255);
            vh.AddVert(center + new Vector2(0f, h), color, Vector2.zero);
            vh.AddVert(center + new Vector2(w, -h), color, Vector2.zero);
            vh.AddVert(center + new Vector2(0f, -h * 0.45f), color, Vector2.zero);
            vh.AddVert(center + new Vector2(-w, -h), color, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }

        private static void AddQuad(VertexHelper vh, Rect rect, Color32 color)
        {
            int start = vh.currentVertCount;
            vh.AddVert(new Vector2(rect.xMin, rect.yMin), color, Vector2.zero);
            vh.AddVert(new Vector2(rect.xMin, rect.yMax), color, Vector2.zero);
            vh.AddVert(new Vector2(rect.xMax, rect.yMax), color, Vector2.zero);
            vh.AddVert(new Vector2(rect.xMax, rect.yMin), color, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }

        private static void AddLine(VertexHelper vh, Rect rect, Vector2 a, Vector2 b, float thickness01, Color32 color)
        {
            a.x = Mathf.Clamp(a.x, -0.2f, 1.2f);
            a.y = Mathf.Clamp(a.y, -0.2f, 1.2f);
            b.x = Mathf.Clamp(b.x, -0.2f, 1.2f);
            b.y = Mathf.Clamp(b.y, -0.2f, 1.2f);

            Vector2 pa = RectPoint(rect, a);
            Vector2 pb = RectPoint(rect, b);
            Vector2 dir = pb - pa;
            if (dir.sqrMagnitude <= 0.001f)
            {
                return;
            }

            Vector2 normal = new Vector2(-dir.y, dir.x).normalized * rect.height * thickness01;
            int start = vh.currentVertCount;
            vh.AddVert(pa - normal, color, Vector2.zero);
            vh.AddVert(pa + normal, color, Vector2.zero);
            vh.AddVert(pb + normal, color, Vector2.zero);
            vh.AddVert(pb - normal, color, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }

        private static Vector2 RectPoint(Rect rect, Vector2 normalized)
        {
            return new Vector2(
                Mathf.Lerp(rect.xMin, rect.xMax, normalized.x),
                Mathf.Lerp(rect.yMin, rect.yMax, normalized.y));
        }
    }
}
