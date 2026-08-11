using System.Collections.Generic;
using UnityEngine;

namespace CompassNavigatorPro {

    public partial class CompassPro : MonoBehaviour {

        #region In-world route state

        Mesh routeWorldMesh;
        Material routeWorldMat;

        readonly List<Vector3> routeWorldSrc = new List<Vector3>();
        readonly List<Vector3> routeWorldSmoothed = new List<Vector3>();
        readonly List<Vector3> routeWorldPts = new List<Vector3>();
        readonly List<float> routeWorldArc = new List<float>();
        readonly List<Vector3> rwVerts = new List<Vector3>();
        readonly List<Vector2> rwUv0 = new List<Vector2>();
        readonly List<Vector2> rwUv1 = new List<Vector2>();
        readonly List<int> rwTris = new List<int>();

        int routeWorldSig;
        bool routeWorldMatDirty;
        const float RouteWorldRayUp = 200f;

        Mesh routeMarkerQuad;
        Material routeWorldMarkerMat;
        MaterialPropertyBlock routeMarkerMPB;

        #endregion


        #region In-world route update

        // Called every frame from UpdateRoute (runs in edit mode too via ExecuteAlways)
        void UpdateRouteWorld () {

            if (!_showRoute || !_routeShowInWorld) return;
            if (!Application.isPlaying && !_routeWorldEditPreview) return;

            // Source points: live route at runtime, authored waypoints for the edit-mode preview
            routeWorldSrc.Clear();
            if (Application.isPlaying) {
                for (int i = 0; i < routePoints.Count; i++) routeWorldSrc.Add(routePoints[i]);
            } else {
                for (int i = 0; i < _routeWaypoints.Count; i++) {
                    CompassProRouteWaypoint wp = _routeWaypoints[i];
                    if (wp != null) routeWorldSrc.Add(wp.GetPosition());
                }
            }
            if (routeWorldSrc.Count < 2) return;

            EnsureRouteWorldMaterial();
            if (routeWorldMat == null) return;

            int sig = ComputeRouteWorldSig();
            if (routeWorldMesh == null || sig != routeWorldSig) {
                routeWorldSig = sig;
                BuildRouteWorldMesh();
            }

            // General material settings: re-applied only when something changed (route edit, inspector, or material recreated)
            if (needUpdateRoute || routeWorldMatDirty) {
                routeWorldMatDirty = false;
                ApplyRouteWorldMaterialStatic();
            }
            // Per-frame state: travelled progress + follow-relative distance fade
            ApplyRouteWorldMaterialDynamic();

            if (routeWorldMesh != null) {
                Graphics.DrawMesh(routeWorldMesh, Matrix4x4.identity, routeWorldMat, gameObject.layer);
            }
        }

        // Billboard waypoint markers in the 3D world (independent of the ribbon; both gated by showRoute)
        void UpdateRouteWorldMarkers () {

            if (!_showRoute || !_routeWorldShowWaypointMarkers) return;
            if (!Application.isPlaying && !_routeWorldEditPreview) return;

            EnsureRouteWorldMarkerMaterial();
            if (routeWorldMarkerMat == null) return;
            EnsureRouteMarkerQuad();
            if (routeMarkerMPB == null) routeMarkerMPB = new MaterialPropertyBlock();

            // Shared state: same depth/fade behaviour as the ribbon; tint matches the mini-map markers
            routeWorldMarkerMat.SetFloat(ShaderParams.RouteZTest, _routeWorldAlwaysVisible ? 8f : 4f);
            bool fade = _routeWorldMaxDistance > 0f || _routeWorldMinDistance > 0f;
            routeWorldMarkerMat.SetVector(ShaderParams.RouteFade, fade
                ? new Vector4(followPos.x, followPos.z, _routeWorldMaxDistance, _routeWorldMinDistance)
                : Vector4.zero);
            routeWorldMarkerMat.SetFloat(ShaderParams.RouteFadeBand, _routeWorldFadeDistance);

            float size = Mathf.Max(_routeWorldWaypointMarkerSize, 0.0001f);
            Vector3 scale = new Vector3(size, size, size);
            int layer = gameObject.layer;
            int count = Application.isPlaying ? routePoints.Count : _routeWaypoints.Count;
            Sprite defSprite = null;

            for (int i = 0; i < count; i++) {
                if (_routeHideTraveledWaypointMarkers && Application.isPlaying && i < routeNextWaypoint) continue;
                Vector3 src;
                Sprite icon;
                Color tint;
                if (Application.isPlaying) {
                    src = routePoints[i];
                    icon = routeIcons.Count > i ? routeIcons[i] : null;
                    tint = routeIconColors.Count > i ? routeIconColors[i] : _routeColor;
                } else {
                    CompassProRouteWaypoint wp = _routeWaypoints[i];
                    if (wp == null) continue;
                    src = wp.GetPosition();
                    icon = wp.icon;
                    tint = wp.GetIconColor();
                }
                if (icon == null) {
                    if (defSprite == null) defSprite = GetRouteDefaultMarkerSprite();
                    icon = defSprite;
                }
                Texture tex = icon != null ? icon.texture : null;
                if (tex == null) continue;

                // Sit on the ground at the waypoint, lift by half the size so the billboard stands on it, plus the user Y offset
                Vector3 pos = DrapeRoutePoint(src) + Vector3.up * (size * 0.5f + _routeWorldWaypointMarkerYOffset);
                routeMarkerMPB.SetTexture(ShaderParams.MainTex, tex);
                routeMarkerMPB.SetColor(ShaderParams.Color, tint);
                Graphics.DrawMesh(routeMarkerQuad, Matrix4x4.TRS(pos, Quaternion.identity, scale), routeWorldMarkerMat, layer, null, 0, routeMarkerMPB);
            }
        }

        void EnsureRouteWorldMarkerMaterial () {
            if (routeWorldMarkerMat != null) return;
            Shader sh = Shader.Find("CompassNavigatorPro/RouteWorldMarker");
            if (sh == null) {
                Debug.LogError("[CNP Route] 'CompassNavigatorPro/RouteWorldMarker' shader not found.");
                return;
            }
            routeWorldMarkerMat = new Material(sh) { hideFlags = HideFlags.DontSave };
        }

        void EnsureRouteMarkerQuad () {
            if (routeMarkerQuad != null) return;
            routeMarkerQuad = new Mesh { hideFlags = HideFlags.DontSave };
            routeMarkerQuad.SetVertices(new List<Vector3> {
                new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f)
            });
            routeMarkerQuad.SetUVs(0, new List<Vector2> {
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f)
            });
            routeMarkerQuad.SetTriangles(new[] { 0, 1, 2, 0, 2, 3 }, 0);
            routeMarkerQuad.RecalculateBounds();
        }

        int ComputeRouteWorldSig () {
            int sig = routeWorldSrc.Count;
            for (int i = 0; i < routeWorldSrc.Count; i++) sig = sig * 31 + routeWorldSrc[i].GetHashCode();
            sig = sig * 31 + _routeWorldWidth.GetHashCode();
            sig = sig * 31 + _routeWorldYOffset.GetHashCode();
            sig = sig * 31 + _routeWorldSampleStep.GetHashCode();
            sig = sig * 31 + _routeWorldGroundMask.value;
            sig = sig * 31 + (int)_routeWorldGroundMode;
            sig = sig * 31 + _routeWorldFixedHeight.GetHashCode();
            sig = sig * 31 + (int)_routeSmoothing;
            sig = sig * 31 + _routeCornerRadius.GetHashCode();
            sig = sig * 31 + _routeCurveResolution;
            return sig;
        }

        void BuildRouteWorldMesh () {

            // Smoothing on the source polyline: fillet corners or a spline through every point (Straight uses the raw points)
            List<Vector3> baseLine = routeWorldSrc;
            if (routeWorldSrc.Count >= 3) {
                if (_routeSmoothing == RouteSmoothing.Rounded && _routeCornerRadius > 0.001f) {
                    BuildRoundedSource(routeWorldSrc, routeWorldSmoothed, _routeCornerRadius);
                    baseLine = routeWorldSmoothed;
                } else if (_routeSmoothing == RouteSmoothing.Curved) {
                    BuildSplineSource(routeWorldSrc, routeWorldSmoothed, _routeCurveResolution);
                    baseLine = routeWorldSmoothed;
                }
            }

            // Draped centerline (subdivide each segment so it follows the ground; no subdivision needed at a fixed height)
            routeWorldPts.Clear();
            routeWorldPts.Add(DrapeRoutePoint(baseLine[0]));
            float step = _routeWorldGroundMode == RouteWorldGroundMode.FixedHeight ? 0f : _routeWorldSampleStep;
            for (int s = 0; s < baseLine.Count - 1; s++) {
                Vector3 a = baseLine[s], b = baseLine[s + 1];
                if (step > 0.01f) {
                    float segLen = Vector3.Distance(a, b);
                    int subdiv = Mathf.Max(0, Mathf.CeilToInt(segLen / step) - 1);
                    for (int k = 1; k <= subdiv; k++) {
                        float t = (k * step) / segLen;
                        if (t < 1f) routeWorldPts.Add(DrapeRoutePoint(Vector3.Lerp(a, b, t)));
                    }
                }
                routeWorldPts.Add(DrapeRoutePoint(b));
            }

            int n = routeWorldPts.Count;
            if (n < 2) return;

            // Arc length (world meters) for dash/progress
            routeWorldArc.Clear();
            routeWorldArc.Add(0f);
            for (int i = 1; i < n; i++) {
                routeWorldArc.Add(routeWorldArc[i - 1] + Vector3.Distance(routeWorldPts[i], routeWorldPts[i - 1]));
            }
            float total = routeWorldArc[n - 1];
            if (total < 1e-5f) total = 1e-5f;

            float half = Mathf.Max(_routeWorldWidth * 0.5f, 0.0001f);

            rwVerts.Clear(); rwUv0.Clear(); rwUv1.Clear(); rwTris.Clear();
            // One independent rectangular quad per segment (own perpendicular -> uniform UVs, no shear); joints closed with a bevel fan
            int prevTop = -1, prevBot = -1;
            Vector3 prevDir = Vector3.zero;
            for (int s = 0; s < n - 1; s++) {
                Vector3 a = routeWorldPts[s], b = routeWorldPts[s + 1];
                Vector3 dir = b - a; dir.y = 0f;
                if (dir.sqrMagnitude < 1e-8f) continue;
                dir.Normalize();
                Vector3 perp = new Vector3(dir.z, 0f, -dir.x) * half;
                float arcA = routeWorldArc[s], arcB = routeWorldArc[s + 1];
                float normA = arcA / total, normB = arcB / total;
                int bi = rwVerts.Count;
                rwVerts.Add(a + perp); rwUv0.Add(new Vector2(arcA, 1f)); rwUv1.Add(new Vector2(normA, 0f));
                rwVerts.Add(a - perp); rwUv0.Add(new Vector2(arcA, -1f)); rwUv1.Add(new Vector2(normA, 0f));
                rwVerts.Add(b + perp); rwUv0.Add(new Vector2(arcB, 1f)); rwUv1.Add(new Vector2(normB, 0f));
                rwVerts.Add(b - perp); rwUv0.Add(new Vector2(arcB, -1f)); rwUv1.Add(new Vector2(normB, 0f));
                rwTris.Add(bi); rwTris.Add(bi + 1); rwTris.Add(bi + 3);
                rwTris.Add(bi); rwTris.Add(bi + 3); rwTris.Add(bi + 2);

                // Bevel fill: close the outer wedge at the joint with the previous segment (Cull Off -> winding free)
                if (prevTop >= 0) {
                    float cross = prevDir.x * dir.z - prevDir.z * dir.x;
                    if (cross * cross > 1e-10f) {
                        int pivot = rwVerts.Count;
                        rwVerts.Add(a); rwUv0.Add(new Vector2(arcA, 0f)); rwUv1.Add(new Vector2(normA, 0f));
                        int outPrev = cross > 0f ? prevTop : prevBot;
                        int outCur = cross > 0f ? bi : bi + 1;
                        rwTris.Add(pivot); rwTris.Add(outPrev); rwTris.Add(outCur);
                    }
                }
                prevTop = bi + 2; prevBot = bi + 3; prevDir = dir;
            }

            if (routeWorldMesh == null) {
                routeWorldMesh = new Mesh { hideFlags = HideFlags.DontSave };
                routeWorldMesh.MarkDynamic();
            }
            routeWorldMesh.Clear();
            routeWorldMesh.SetVertices(rwVerts);
            routeWorldMesh.SetUVs(0, rwUv0);
            routeWorldMesh.SetUVs(1, rwUv1);
            routeWorldMesh.SetTriangles(rwTris, 0);
            routeWorldMesh.RecalculateBounds();
        }

        // Replaces each interior corner with a tangent fillet arc so the ribbon (and its texture) flows around bends
        void BuildRoundedSource (List<Vector3> src, List<Vector3> dst, float radius) {
            dst.Clear();
            int m = src.Count;
            dst.Add(src[0]);
            for (int i = 1; i < m - 1; i++) {
                Vector3 p = src[i];
                Vector3 toPrev = src[i - 1] - p, toNext = src[i + 1] - p;
                float lenPrev = toPrev.magnitude, lenNext = toNext.magnitude;
                if (lenPrev < 1e-4f || lenNext < 1e-4f) { dst.Add(p); continue; }
                Vector3 d0 = toPrev / lenPrev, d1 = toNext / lenNext;
                float angle = Mathf.Acos(Mathf.Clamp(Vector3.Dot(d0, d1), -1f, 1f));
                if (angle > 3.0f || angle < 0.05f) { dst.Add(p); continue; }
                float halfA = angle * 0.5f;
                float t = Mathf.Min(radius / Mathf.Max(Mathf.Tan(halfA), 1e-3f), lenPrev * 0.5f, lenNext * 0.5f);
                float rEff = t * Mathf.Tan(halfA);
                Vector3 bis = d0 + d1;
                if (bis.sqrMagnitude < 1e-8f) { dst.Add(p); continue; }
                bis.Normalize();
                Vector3 a = p + d0 * t, b = p + d1 * t;
                Vector3 center = p + bis * (rEff / Mathf.Max(Mathf.Sin(halfA), 1e-3f));
                Vector3 ca = a - center, cb = b - center;
                int steps = Mathf.Clamp(Mathf.CeilToInt((Mathf.PI - angle) / 0.2618f), 1, 24);
                dst.Add(a);
                for (int k = 1; k < steps; k++) dst.Add(center + Vector3.Slerp(ca, cb, (float)k / steps));
                dst.Add(b);
            }
            dst.Add(src[m - 1]);
        }

        // Centripetal Catmull-Rom resample so the ribbon (and its texture) curves smoothly through every point
        void BuildSplineSource (List<Vector3> src, List<Vector3> dst, int subdivisions) {
            dst.Clear();
            int m = src.Count;
            int sub = Mathf.Clamp(subdivisions, 1, 64);
            dst.Add(src[0]);
            for (int i = 0; i < m - 1; i++) {
                Vector3 p0 = src[Mathf.Max(i - 1, 0)];
                Vector3 p1 = src[i];
                Vector3 p2 = src[i + 1];
                Vector3 p3 = src[Mathf.Min(i + 2, m - 1)];
                float t1 = CRStep(p0, p1);
                float t2 = t1 + CRStep(p1, p2);
                float t3 = t2 + CRStep(p2, p3);
                for (int k = 1; k <= sub; k++) {
                    float u = Mathf.Lerp(t1, t2, (float)k / sub);
                    dst.Add(CatmullRomPoint(p0, p1, p2, p3, 0f, t1, t2, t3, u));
                }
            }
        }

        static float CRStep (Vector3 a, Vector3 b) {
            return Mathf.Sqrt(Mathf.Max((b - a).magnitude, 1e-4f));
        }

        static Vector3 CRLerp (Vector3 a, Vector3 b, float ta, float tb, float u) {
            float d = tb - ta;
            return d < 1e-6f ? a : a + (b - a) * ((u - ta) / d);
        }

        static Vector3 CatmullRomPoint (Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t0, float t1, float t2, float t3, float u) {
            Vector3 a1 = CRLerp(p0, p1, t0, t1, u);
            Vector3 a2 = CRLerp(p1, p2, t1, t2, u);
            Vector3 a3 = CRLerp(p2, p3, t2, t3, u);
            Vector3 b1 = CRLerp(a1, a2, t0, t2, u);
            Vector3 b2 = CRLerp(a2, a3, t1, t3, u);
            return CRLerp(b1, b2, t1, t2, u);
        }

        Vector3 DrapeRoutePoint (Vector3 p) {
            // Fixed height: constant world Y, no raycast
            if (_routeWorldGroundMode == RouteWorldGroundMode.FixedHeight) {
                return new Vector3(p.x, _routeWorldFixedHeight, p.z);
            }
            if (_routeWorldGroundMask.value != 0) {
                Ray ray = new Ray(p + Vector3.up * RouteWorldRayUp, Vector3.down);
                if (Physics.Raycast(ray, out RaycastHit hit, RouteWorldRayUp * 2f, _routeWorldGroundMask, QueryTriggerInteraction.Ignore)) {
                    return hit.point + Vector3.up * _routeWorldYOffset;
                }
            }
            return p + Vector3.up * _routeWorldYOffset;
        }

        void EnsureRouteWorldMaterial () {
            if (routeWorldMat != null) return;
            Shader sh = Shader.Find("CompassNavigatorPro/RouteWorld");
            if (sh == null) {
                Debug.LogError("[CNP Route] 'CompassNavigatorPro/RouteWorld' shader not found.");
                return;
            }
            routeWorldMat = new Material(sh) { hideFlags = HideFlags.DontSave };
            routeWorldMatDirty = true;
        }

        // General appearance: only re-applied when a setting changes (color is a tint here, not baked into the mesh)
        void ApplyRouteWorldMaterialStatic () {
            // Texture when assigned (styleId 4), otherwise a plain solid line (styleId 0)
            bool useTex = _routeWorldTexture != null;
            // Color, gradient and travelled coloring are shared with the mini-map (gradient color = base color when disabled)
            routeWorldMat.SetColor(ShaderParams.Color, _routeColor);
            routeWorldMat.SetColor(ShaderParams.RouteGradientColor, _routeUseGradient ? _routeGradientColor : _routeColor);
            routeWorldMat.SetFloat(ShaderParams.RouteStyleId, useTex ? 4f : 0f);
            routeWorldMat.SetTexture(ShaderParams.RouteTex, useTex ? _routeWorldTexture : null);
            routeWorldMat.SetFloat(ShaderParams.RouteTexLen, _routeWorldTextureLength);
            routeWorldMat.SetVector(ShaderParams.RouteData, new Vector4(0f, 0f, _routeWorldEdgeFeather, 0f));
            routeWorldMat.SetFloat(ShaderParams.RouteFlowId, _routeWorldFlowSpeed);
            routeWorldMat.SetColor(ShaderParams.RouteTraveledColor, _routeWorldTraveledColor);
            routeWorldMat.SetFloat(ShaderParams.RouteTraveledSolid, _routeTraveledSolid ? 1f : 0f);
            routeWorldMat.SetFloat(ShaderParams.RouteZTest, _routeWorldAlwaysVisible ? 8f : 4f);
        }

        // Per-frame state: fade is centered on the follow target, plus travelled progress (shared with the mini-map; -1 disables)
        void ApplyRouteWorldMaterialDynamic () {
            bool fade = _routeWorldMaxDistance > 0f || _routeWorldMinDistance > 0f;
            routeWorldMat.SetVector(ShaderParams.RouteFade, fade
                ? new Vector4(followPos.x, followPos.z, _routeWorldMaxDistance, _routeWorldMinDistance)
                : Vector4.zero);
            routeWorldMat.SetFloat(ShaderParams.RouteFadeBand, _routeWorldFadeDistance);
            // Travelled coloring in-world: needs the global Color Travelled Part on AND the in-world travelled toggle (-1 disables)
            routeWorldMat.SetFloat(ShaderParams.RouteProgress, (_routeShowTraveled && _routeWorldShowTraveled) ? routeProgressValue : -1f);
        }

        void RouteWorldDispose () {
            if (routeWorldMat != null) {
                DestroyImmediate(routeWorldMat);
                routeWorldMat = null;
            }
            if (routeWorldMesh != null) {
                DestroyImmediate(routeWorldMesh);
                routeWorldMesh = null;
            }
            if (routeWorldMarkerMat != null) {
                DestroyImmediate(routeWorldMarkerMat);
                routeWorldMarkerMat = null;
            }
            if (routeMarkerQuad != null) {
                DestroyImmediate(routeMarkerQuad);
                routeMarkerQuad = null;
            }
        }

        #endregion

    }
}
