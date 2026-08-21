using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LWS.InterstateHauler
{
    public sealed class LwsSemanticGpsMapGraphic : MaskableGraphic
    {
        private readonly List<MapSegment> _roadSegments = new List<MapSegment>();
        private readonly List<MapSegment> _routeSegments = new List<MapSegment>();
        private LwsRoadGraph _cachedGraph;
        private LwsRouteResult _cachedRoute;
        private LwsWorldPositionD _playerGlobal;
        private Vector3 _playerForward = Vector3.forward;
        private Vector2 _panMeters;
        private float _metersVisible = 900f;
        private bool _headingUp = true;
        private bool _routeActive;

        public bool HasRoadPresentation => _roadSegments.Count > 0;
        public bool HasRoutePresentation => _routeSegments.Count > 0;
        public bool HeadingUp => _headingUp;
        public float MetersVisible => _metersVisible;

        public void SetMapData(
            LwsRoadGraph graph,
            LwsRouteResult route,
            LwsWorldPositionD playerGlobal,
            Vector3 playerForward,
            bool headingUp,
            float metersVisible,
            Vector2 panMeters)
        {
            if (!ReferenceEquals(_cachedGraph, graph))
            {
                _cachedGraph = graph;
                RebuildRoadCache(graph);
            }

            if (!ReferenceEquals(_cachedRoute, route))
            {
                _cachedRoute = route;
                RebuildRouteCache(route);
            }

            _playerGlobal = playerGlobal;
            _playerForward = playerForward.sqrMagnitude > 0.0001f ? playerForward.normalized : Vector3.forward;
            _headingUp = headingUp;
            _metersVisible = Mathf.Max(80f, metersVisible);
            _panMeters = panMeters;
            _routeActive = route != null && route.succeeded && route.waypoints != null && route.waypoints.Count > 1;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = rectTransform.rect;
            AddQuad(vh, rect, new Color32(8, 15, 18, 245));
            AddGrid(vh, rect, 4, new Color32(28, 46, 50, 125));

            for (int i = 0; i < _roadSegments.Count; i++)
            {
                AddWorldLine(vh, rect, _roadSegments[i].a, _roadSegments[i].b, 2.25f, new Color32(84, 105, 110, 220));
            }

            if (_routeActive)
            {
                for (int i = 0; i < _routeSegments.Count; i++)
                {
                    AddWorldLine(vh, rect, _routeSegments[i].a, _routeSegments[i].b, 5.5f, new Color32(0, 188, 255, 255));
                    AddWorldLine(vh, rect, _routeSegments[i].a, _routeSegments[i].b, 2.25f, new Color32(255, 238, 88, 255));
                }

                if (_cachedRoute.waypoints.Count > 0)
                {
                    AddCircle(vh, rect, WorldToMap(_cachedRoute.waypoints[_cachedRoute.waypoints.Count - 1]), 8f, new Color32(255, 108, 92, 255));
                }
            }

            AddPlayerMarker(vh, rect);
        }

        private void RebuildRoadCache(LwsRoadGraph graph)
        {
            _roadSegments.Clear();
            if (graph?.edges == null)
            {
                return;
            }

            foreach (LwsRoadEdge edge in graph.edges)
            {
                if (edge?.samples == null || edge.samples.Count < 2)
                {
                    continue;
                }

                for (int i = 1; i < edge.samples.Count; i++)
                {
                    LwsRoadSample previous = edge.samples[i - 1];
                    LwsRoadSample current = edge.samples[i];
                    if (previous != null && current != null)
                    {
                        _roadSegments.Add(new MapSegment(previous.position, current.position));
                    }
                }
            }
        }

        private void RebuildRouteCache(LwsRouteResult route)
        {
            _routeSegments.Clear();
            if (route?.waypoints == null || route.waypoints.Count < 2)
            {
                return;
            }

            for (int i = 1; i < route.waypoints.Count; i++)
            {
                _routeSegments.Add(new MapSegment(route.waypoints[i - 1], route.waypoints[i]));
            }
        }

        private Vector2 WorldToMap(Vector3 globalPoint)
        {
            Vector2 delta = new Vector2(
                globalPoint.x - (float)_playerGlobal.x,
                globalPoint.z - (float)_playerGlobal.z);
            delta -= _panMeters;

            if (_headingUp)
            {
                Vector2 forward = new Vector2(_playerForward.x, _playerForward.z);
                if (forward.sqrMagnitude < 0.0001f)
                {
                    forward = Vector2.up;
                }

                forward.Normalize();
                float angle = -Mathf.Atan2(forward.x, forward.y) * Mathf.Rad2Deg;
                delta = Quaternion.Euler(0f, 0f, angle) * delta;
            }

            float half = _metersVisible * 0.5f;
            return new Vector2(delta.x / half * 0.5f + 0.5f, delta.y / half * 0.5f + 0.5f);
        }

        private void AddWorldLine(VertexHelper vh, Rect rect, Vector3 a, Vector3 b, float thicknessPixels, Color32 color)
        {
            Vector2 pa = WorldToMap(a);
            Vector2 pb = WorldToMap(b);
            if (!MaybeVisible(pa) && !MaybeVisible(pb))
            {
                return;
            }

            AddLine(vh, rect, pa, pb, thicknessPixels, color);
        }

        private static bool MaybeVisible(Vector2 point)
        {
            return point.x >= -0.18f && point.x <= 1.18f && point.y >= -0.18f && point.y <= 1.18f;
        }

        private static void AddPlayerMarker(VertexHelper vh, Rect rect)
        {
            Vector2 center = RectPoint(rect, new Vector2(0.5f, 0.5f));
            float h = Mathf.Min(rect.width, rect.height) * 0.055f;
            float w = h * 0.7f;
            int start = vh.currentVertCount;
            Color32 color = new Color32(255, 244, 110, 255);
            vh.AddVert(center + new Vector2(0f, h), color, Vector2.zero);
            vh.AddVert(center + new Vector2(w, -h), color, Vector2.zero);
            vh.AddVert(center, color, Vector2.zero);
            vh.AddVert(center + new Vector2(-w, -h), color, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }

        private static void AddCircle(VertexHelper vh, Rect rect, Vector2 normalized, float radiusPixels, Color32 color)
        {
            Vector2 center = RectPoint(rect, normalized);
            float r = radiusPixels;
            int start = vh.currentVertCount;
            vh.AddVert(center, color, Vector2.zero);
            for (int i = 0; i <= 12; i++)
            {
                float a = i / 12f * Mathf.PI * 2f;
                vh.AddVert(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r, color, Vector2.zero);
            }

            for (int i = 1; i <= 12; i++)
            {
                vh.AddTriangle(start, start + i, start + i + 1);
            }
        }

        private static void AddGrid(VertexHelper vh, Rect rect, int lines, Color32 color)
        {
            for (int i = 1; i < lines; i++)
            {
                float t = i / (float)lines;
                AddLine(vh, rect, new Vector2(t, 0f), new Vector2(t, 1f), 1f, color);
                AddLine(vh, rect, new Vector2(0f, t), new Vector2(1f, t), 1f, color);
            }
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

        private static void AddLine(VertexHelper vh, Rect rect, Vector2 a, Vector2 b, float thicknessPixels, Color32 color)
        {
            a.x = Mathf.Clamp(a.x, -0.25f, 1.25f);
            a.y = Mathf.Clamp(a.y, -0.25f, 1.25f);
            b.x = Mathf.Clamp(b.x, -0.25f, 1.25f);
            b.y = Mathf.Clamp(b.y, -0.25f, 1.25f);

            Vector2 pa = RectPoint(rect, a);
            Vector2 pb = RectPoint(rect, b);
            Vector2 dir = pb - pa;
            if (dir.sqrMagnitude < 0.001f)
            {
                return;
            }

            Vector2 normal = new Vector2(-dir.y, dir.x).normalized * Mathf.Max(0.5f, thicknessPixels);
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

        private readonly struct MapSegment
        {
            public MapSegment(Vector3 a, Vector3 b)
            {
                this.a = a;
                this.b = b;
            }

            public readonly Vector3 a;
            public readonly Vector3 b;
        }
    }
}
