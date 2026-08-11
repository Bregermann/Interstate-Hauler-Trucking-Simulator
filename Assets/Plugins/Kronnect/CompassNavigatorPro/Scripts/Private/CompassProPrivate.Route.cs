using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CompassNavigatorPro {

    public partial class CompassPro : MonoBehaviour {

        #region Route internal state

        readonly List<Vector3> routePoints = new List<Vector3>();
        readonly List<Sprite> routeIcons = new List<Sprite>();
        readonly List<Color> routeIconColors = new List<Color>();
        readonly List<float> routeArcWorld = new List<float>();
        readonly List<Vector3> routeRenderPts = new List<Vector3>();
        readonly List<float> routeRenderArc = new List<float>();

        // authored-waypoints + per-waypoint minimap markers
        readonly List<GameObject> routeMarkerPool = new List<GameObject>();
        readonly List<Image> routeMarkerImages = new List<Image>();
        static Sprite routeDefaultMarkerSprite;
        int lastWaypointSig;
        int lastWaypointCount = -1;
        bool lastRouteUseWaypoints;
        int routeNextWaypoint;
        bool routeStartFollowsPlayer;
        CompassProPOI routeDestinationPOI;
        float routeProgressValue;

        bool needUpdateRoute;

        CompassProRouteGraphic routeGraphic;
        Material routeMat;

        // route cue (auto-managed POI at the next waypoint)
        CompassProPOI routeCuePOI;
        GameObject routeCuePOIGO;

        // reused build scratch (no per-frame allocations after warmup)
        readonly List<Vector3> routeScreenNorm = new List<Vector3>();
        readonly List<Vector2> routeScreenLocal = new List<Vector2>();
        readonly List<Vector2> routeLLocal = new List<Vector2>();
        readonly List<Vector2> routeRLocal = new List<Vector2>();
        readonly List<float> routePixelArc = new List<float>();
        int[] routeVertexBase = new int[8];

        // rebuild gating signature
        Vector3 lastRouteCamPos;
        Quaternion lastRouteCamRot;
        float lastRouteZoom = -1f;
        Vector2 lastRouteShift = new Vector2(float.NaN, float.NaN);
        bool lastRouteFullScreen;
        bool routeCapWarned;
        bool routeTraced;

        // geometry-settings change detection (rebuild even when the camera is still)
        float lastBuiltWidth = -1f;
        int lastBuiltWidthSpace = -1;
        int lastBuiltTilingSpace = -1;
        Color lastBuiltColor;
        Color lastBuiltGradient;
        bool lastBuiltUseGradient;
        bool lastBuiltShowTraveled;
        int lastBuiltSmoothing = -1;
        float lastBuiltCornerRadius = -1f;
        int lastBuiltCurveResolution = -1;

        static Sprite routeCueDefaultSprite;

        #endregion


        #region Route settings invalidation

        // Called from UpdateSettings() so inspector edits update the live route
        void RouteSettingsChanged () {
            needUpdateRoute = true;
        }

        #endregion


        #region Route update

        void RecomputeRouteWorldArc () {
            int n = routePoints.Count;
            routeArcWorld.Clear();
            if (n == 0) return;
            routeArcWorld.Add(0f);
            for (int i = 1; i < n; i++) {
                routeArcWorld.Add(routeArcWorld[i - 1] + (routePoints[i] - routePoints[i - 1]).magnitude);
            }
        }

        // Called by the scripting API so the authored-waypoints list stops driving the route
        void RouteApiTakeover () {
            _routeUseWaypoints = false;
            routeIcons.Clear();
            routeIconColors.Clear();
            lastWaypointCount = -1;
        }

        // Captures the current (API-set) route into the editable waypoints list
        void SeedWaypointsFromCurrentRoute () {
            _routeWaypoints.Clear();
            for (int i = 0; i < routePoints.Count; i++) {
                CompassProRouteWaypoint wp = new CompassProRouteWaypoint();
                wp.position = routePoints[i];
                wp.icon = routeIcons.Count > i ? routeIcons[i] : null;
                wp.iconColor = routeIconColors.Count > i ? routeIconColors[i] : _routeColor;
                _routeWaypoints.Add(wp);
            }
            lastWaypointCount = -1;
            lastWaypointSig = 0;
        }

        void RefreshRouteFromWaypoints () {
            routeStartFollowsPlayer = false;
            routeDestinationPOI = null;
            int n = _routeWaypoints.Count;
            routePoints.Clear();
            routeIcons.Clear();
            routeIconColors.Clear();
            int sig = n;
            for (int i = 0; i < n; i++) {
                CompassProRouteWaypoint wp = _routeWaypoints[i];
                Vector3 pos = wp != null ? wp.GetPosition() : Vector3.zero;
                routePoints.Add(pos);
                routeIcons.Add(wp != null ? wp.icon : null);
                routeIconColors.Add(wp != null ? wp.GetIconColor() : Color.white);
                sig = sig * 31 + pos.GetHashCode();
            }
            if (n != lastWaypointCount) {
                lastWaypointCount = n;
                routeNextWaypoint = n > 1 ? 1 : 0;
            }
            if (sig != lastWaypointSig) {
                lastWaypointSig = sig;
                RecomputeRouteWorldArc();
                needUpdateRoute = true;
            }
        }

        void UpdateRouteWaypointMarkers () {
            if (!_routeShowWaypointMarkers || miniMapMaskUI == null || miniMapCamera == null) {
                HideRouteMarkers();
                return;
            }
            float aspect = GetMiniMapAspectRatio();
            int n = routePoints.Count;
            const float margin = 0.1f;
            int used = 0;
            for (int i = 0; i < n; i++) {
                if (_routeHideTraveledWaypointMarkers && Application.isPlaying && i < routeNextWaypoint) continue;
                Vector3 sp = GetMiniMapScreenPos(routePoints[i], aspect);
                if (sp.z <= 0f) continue;
                if (sp.x < -margin || sp.x > 1f + margin || sp.y < -margin || sp.y > 1f + margin) continue;
                Image img = GetRouteMarker(used);
                used++;
                RectTransform rt = img.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(sp.x, sp.y);
                rt.sizeDelta = new Vector2(_routeWaypointMarkerSize, _routeWaypointMarkerSize);
                Sprite ic = (routeIcons.Count > i && routeIcons[i] != null) ? routeIcons[i] : GetRouteDefaultMarkerSprite();
                if (img.sprite != ic) img.sprite = ic;
                img.color = routeIconColors.Count > i ? routeIconColors[i] : _routeColor;
            }
            for (int k = used; k < routeMarkerPool.Count; k++) {
                if (routeMarkerPool[k] != null && routeMarkerPool[k].activeSelf) routeMarkerPool[k].SetActive(false);
            }
        }

        Image GetRouteMarker (int index) {
            while (routeMarkerPool.Count <= index) {
                GameObject go = new GameObject("Route Marker");
                go.transform.SetParent(miniMapMaskUI, false);
                Image img = go.AddComponent<Image>();
                img.raycastTarget = false;
                RectTransform rt = img.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.localScale = Vector3.one;
                routeMarkerPool.Add(go);
                routeMarkerImages.Add(img);
            }
            GameObject m = routeMarkerPool[index];
            if (!m.activeSelf) m.SetActive(true);
            return routeMarkerImages[index];
        }

        void HideRouteMarkers () {
            for (int k = 0; k < routeMarkerPool.Count; k++) {
                if (routeMarkerPool[k] != null && routeMarkerPool[k].activeSelf) routeMarkerPool[k].SetActive(false);
            }
        }

        void DestroyRouteMarkers () {
            for (int k = 0; k < routeMarkerPool.Count; k++) {
                if (routeMarkerPool[k] != null) Misc.DestroySafe(routeMarkerPool[k]);
            }
            routeMarkerPool.Clear();
            routeMarkerImages.Clear();
        }

        static Sprite GetRouteDefaultMarkerSprite () {
            if (routeDefaultMarkerSprite != null) return routeDefaultMarkerSprite;
            const int sz = 32;
            Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.hideFlags = HideFlags.DontSave;
            Color32[] px = new Color32[sz * sz];
            float r = sz * 0.5f;
            for (int y = 0; y < sz; y++) {
                for (int x = 0; x < sz; x++) {
                    float dx = x + 0.5f - r, dy = y + 0.5f - r;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(r - d);
                    px[y * sz + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            routeDefaultMarkerSprite = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), 100f);
            routeDefaultMarkerSprite.hideFlags = HideFlags.DontSave;
            return routeDefaultMarkerSprite;
        }

        void UpdateRoute () {

            // Authored waypoints drive the route when enabled (runtime only; avoids creating objects in edit mode)
            if (_routeUseWaypoints && Application.isPlaying) {
                // When just enabled with an empty list, capture the current route into the editable list
                if (!lastRouteUseWaypoints && _routeWaypoints.Count == 0 && routePoints.Count > 1) {
                    SeedWaypointsFromCurrentRoute();
                }
                RefreshRouteFromWaypoints();
            }
            lastRouteUseWaypoints = _routeUseWaypoints;

            bool routeActive = _showRoute && routePoints.Count > 1;

            // Keep dynamic endpoints in sync (direct line to player / POI)
            if (routeActive && routeStartFollowsPlayer) {
                routePoints[0] = followPos;
                if (routeDestinationPOI != null) {
                    routePoints[routePoints.Count - 1] = routeDestinationPOI.transform.position;
                }
                RecomputeRouteWorldArc();
                needUpdateRoute = true;
            }

            // Auto-advance waypoints
            if (routeActive && _routeAutoAdvance && Application.isPlaying) {
                RouteAutoAdvance();
                routeActive = _showRoute && routePoints.Count > 1;
            }

            // Travelled progress: computed once, shared by the mini-map polyline and the in-world ribbon
            if (routeActive && _routeShowTraveled && _routeAutoProgress) {
                routeProgressValue = ComputeRouteProgress();
            }

            // Mini-map polyline
            if (routeActive && _routeShowOnMiniMap && _showMiniMap) {
                EnsureRouteGraphic();
                if (routeGraphic != null) {
                    // Apply material params every frame so inspector edits take effect even on prefab instances
                    // (UpdateSettings early-returns for prefabs, so we can't rely on a dirty flag here).
                    // The marching-ants flow is driven by _Time inside the shader, so no per-frame scroll push is needed.
                    ApplyRouteMaterial();
                    bool rebuild = needUpdateRoute || RouteViewChanged() || RouteGeometrySettingsChanged() || routeStartFollowsPlayer;
                    if (rebuild) {
                        BuildMiniMapRoute();
                    }
                }
            } else {
                HideRouteGraphic();
            }

            // Route cue: an auto-managed POI placed at the next waypoint (shows on bar and/or minimap)
            if (routeActive) {
                UpdateRouteCuePOI();
            } else {
                DestroyRouteCuePOI();
            }

            // Per-waypoint markers on the mini-map
            if (routeActive && _showMiniMap) {
                UpdateRouteWaypointMarkers();
            } else {
                HideRouteMarkers();
            }

            // In-world holographic line (independent of the mini-map; previews from waypoints in edit mode)
            UpdateRouteWorld();

            // In-world billboard markers at each waypoint (independent of the in-world line)
            UpdateRouteWorldMarkers();

            needUpdateRoute = false;
        }

        void RouteAutoAdvance () {
            if (routeNextWaypoint >= routePoints.Count) return;
            Vector3 wp = routePoints[routeNextWaypoint];
            float dx = wp.x - followPos.x;
            float dz = wp.z - followPos.z;
            float dsq = dx * dx + dz * dz;
            if (_use3Ddistance) {
                float dy = wp.y - followPos.y;
                dsq += dy * dy;
            }
            if (dsq <= _routeWaypointReachDistance * _routeWaypointReachDistance) {
                int reached = routeNextWaypoint;
                routeNextWaypoint++;
                needUpdateRoute = true;
                OnRouteWaypointReached?.Invoke(reached, wp);
                if (routeNextWaypoint >= routePoints.Count) {
                    OnRouteCompleted?.Invoke();
                    if (_routeClearOnComplete) ClearRoute();
                }
            }
        }

        bool RouteViewChanged () {
            bool changed = false;
            if (miniMapCamera != null) {
                Transform ct = miniMapCamera.transform;
                if (lastRouteCamPos != ct.position) { lastRouteCamPos = ct.position; changed = true; }
                if (lastRouteCamRot != ct.rotation) { lastRouteCamRot = ct.rotation; changed = true; }
            }
            if (lastRouteZoom != currentMiniMapZoomLevel) { lastRouteZoom = currentMiniMapZoomLevel; changed = true; }
            if (lastRouteShift != _miniMapIconPositionShift) { lastRouteShift = _miniMapIconPositionShift; changed = true; }
            if (lastRouteFullScreen != _miniMapFullScreenState) { lastRouteFullScreen = _miniMapFullScreenState; changed = true; }
            return changed;
        }

        bool RouteGeometrySettingsChanged () {
            if (_routeWidth != lastBuiltWidth) return true;
            if ((int)_routeWidthSpace != lastBuiltWidthSpace) return true;
            if ((int)_routeTextureTilingSpace != lastBuiltTilingSpace) return true;
            if (_routeUseGradient != lastBuiltUseGradient) return true;
            if (!RouteColorEquals(_routeColor, lastBuiltColor)) return true;
            if (!RouteColorEquals(_routeGradientColor, lastBuiltGradient)) return true;
            if (_routeShowTraveled != lastBuiltShowTraveled) return true;
            if ((int)_routeSmoothing != lastBuiltSmoothing) return true;
            if (_routeCornerRadius != lastBuiltCornerRadius) return true;
            if (_routeCurveResolution != lastBuiltCurveResolution) return true;
            return false;
        }

        static bool RouteColorEquals (Color a, Color b) {
            return a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a;
        }

        #endregion


        #region Route mini-map mesh builder

        void BuildMiniMapRoute () {

            if (routeGraphic == null || miniMapCamera == null) return;

            RectTransform rt = routeGraphic.rectTransform;
            Vector2 rectSize = rt.rect.size;
            if (rectSize.x <= 0f || rectSize.y <= 0f) return;

            int n = routePoints.Count;
            if (n < 2) { routeGraphic.ClearGeometry(); return; }

            // keep mask in sync (it can change with full screen / style)
            Texture maskTex = GetRouteMaskTexture();
            if (routeGraphic.maskTexture != maskTex) routeGraphic.maskTexture = maskTex;
            if (routeMat != null) routeMat.SetTexture(ShaderParams.MaskTex, maskTex);

            // Smoothed render polyline (same shape as the in-world ribbon); route logic/progress still use the raw routePoints
            List<Vector3> rpts = routePoints;
            List<float> rarc = routeArcWorld;
            if (routePoints.Count >= 3) {
                if (_routeSmoothing == RouteSmoothing.Rounded && _routeCornerRadius > 0.001f) {
                    BuildRoundedSource(routePoints, routeRenderPts, _routeCornerRadius);
                    BuildRouteRenderArc();
                    rpts = routeRenderPts; rarc = routeRenderArc;
                } else if (_routeSmoothing == RouteSmoothing.Curved) {
                    BuildSplineSource(routePoints, routeRenderPts, _routeCurveResolution);
                    BuildRouteRenderArc();
                    rpts = routeRenderPts; rarc = routeRenderArc;
                }
            }
            n = rpts.Count;

            EnsureRouteScratch(n);

            float aspect = GetMiniMapAspectRatio();
            for (int i = 0; i < n; i++) {
                Vector3 sp = GetMiniMapScreenPos(rpts[i], aspect);
                routeScreenNorm[i] = sp;
                routeScreenLocal[i] = new Vector2((sp.x - 0.5f) * rectSize.x, (sp.y - 0.5f) * rectSize.y);
            }

            // pixel arc length (stable origin at route start)
            routePixelArc[0] = 0f;
            for (int i = 1; i < n; i++) {
                routePixelArc[i] = routePixelArc[i - 1] + Vector2.Distance(routeScreenLocal[i], routeScreenLocal[i - 1]);
            }

            if (!routeTraced) {
                routeTraced = true;
                string matInfo = routeMat == null ? "MAT=NULL" : ("mat=" + routeMat.shader.name + " styleId=" + routeMat.GetFloat(ShaderParams.RouteStyleId).ToString("F1"));
                Material rm = routeGraphic.materialForRendering;
                string renderShader = rm != null ? rm.shader.name : "null";
                Debug.Log($"[CNP Route] build pts={n} tex={(_routeTexture != null)} width={_routeWidth} flow={_routeFlowSpeed} tiling={_routeTextureTiling} {matInfo} renderShader={renderShader} renderIsRouteMat={ReferenceEquals(rm, routeMat)}");
            }

            // Uniform-width ribbon, one quad per segment (no miter -> uniform stroke width); travelled coloring is shader-based.
            bool worldWidth = _routeWidthSpace == RouteWidthSpace.World;
            bool worldDash = _routeTextureTilingSpace == RouteTilingSpace.World;
            float half = worldWidth ? Mathf.Max(_routeWidth * 0.5f, 0.001f) : Mathf.Max(_routeWidth * 0.5f, 0.5f);
            float totalWorld = rarc.Count == n ? rarc[n - 1] : 1f;
            if (totalWorld < 1e-5f) totalWorld = 1e-5f;

            var g = routeGraphic;
            g.vertices.Clear();
            g.uv0.Clear();
            g.uv1.Clear();
            g.uv2.Clear();
            g.vertexColors.Clear();
            g.triangles.Clear();

            const float margin = 0.2f;
            int emitted = 0;
            int segCount = n - 1;
            for (int s = 0; s < segCount; s++) {
                if (emitted >= _routeMaxSegments) {
                    if (!routeCapWarned) {
                        routeCapWarned = true;
                        Debug.Log("[CNP Route] segment cap reached (" + _routeMaxSegments + "); long route truncated on minimap.");
                    }
                    break;
                }
                if (!RouteSegmentVisible(s, margin)) continue;

                float arc0 = worldDash ? rarc[s] : routePixelArc[s];
                float arc1 = worldDash ? rarc[s + 1] : routePixelArc[s + 1];
                float wn0 = rarc.Count > s ? rarc[s] / totalWorld : 0f;
                float wn1 = rarc.Count > s + 1 ? rarc[s + 1] / totalWorld : 0f;
                Color32 c0 = RouteColorAt(wn0);
                Color32 c1 = RouteColorAt(wn1);
                Vector2 a = routeScreenLocal[s], b = routeScreenLocal[s + 1];
                Vector3 aw = rpts[s], bw = rpts[s + 1];

                EmitRouteQuad(a, b, aw, bw, arc0, arc1, wn0, wn1, c0, c1, half, worldWidth, aspect, rectSize);
                emitted++;
            }

            lastBuiltWidth = _routeWidth;
            lastBuiltWidthSpace = (int)_routeWidthSpace;
            lastBuiltTilingSpace = (int)_routeTextureTilingSpace;
            lastBuiltColor = _routeColor;
            lastBuiltGradient = _routeGradientColor;
            lastBuiltUseGradient = _routeUseGradient;
            lastBuiltShowTraveled = _routeShowTraveled;
            lastBuiltSmoothing = (int)_routeSmoothing;
            lastBuiltCornerRadius = _routeCornerRadius;
            lastBuiltCurveResolution = _routeCurveResolution;

            g.MarkGeometryDirty();
        }

        Vector2 WorldOffsetToLocal (Vector3 worldPos, float aspect, Vector2 rectSize) {
            Vector3 sp = GetMiniMapScreenPos(worldPos, aspect);
            return new Vector2((sp.x - 0.5f) * rectSize.x, (sp.y - 0.5f) * rectSize.y);
        }

        // Emits one ribbon quad between two points at the given half-width (pixel or world space)
        void EmitRouteQuad (Vector2 a, Vector2 b, Vector3 aw, Vector3 bw, float arcA, float arcB, float wnA, float wnB, Color32 cA, Color32 cB, float half, bool worldWidth, float aspect, Vector2 rectSize) {
            Vector2 L0, R0, L1, R1;
            if (worldWidth) {
                Vector3 wt = bw - aw; wt.y = 0f;
                if (wt.sqrMagnitude < 1e-8f) wt = Vector3.forward; else wt.Normalize();
                Vector3 wPerp = new Vector3(-wt.z, 0f, wt.x);
                L0 = WorldOffsetToLocal(aw + wPerp * half, aspect, rectSize);
                R0 = WorldOffsetToLocal(aw - wPerp * half, aspect, rectSize);
                L1 = WorldOffsetToLocal(bw + wPerp * half, aspect, rectSize);
                R1 = WorldOffsetToLocal(bw - wPerp * half, aspect, rectSize);
            } else {
                Vector2 d = b - a;
                if (d.sqrMagnitude < 1e-6f) return;
                d.Normalize();
                Vector2 off = new Vector2(-d.y, d.x) * half;
                L0 = a + off; R0 = a - off;
                L1 = b + off; R1 = b - off;
            }
            var g = routeGraphic;
            int idx = g.vertices.Count;
            AddRouteVert(L0, arcA, 1f, wnA, cA, rectSize);
            AddRouteVert(R0, arcA, -1f, wnA, cA, rectSize);
            AddRouteVert(L1, arcB, 1f, wnB, cB, rectSize);
            AddRouteVert(R1, arcB, -1f, wnB, cB, rectSize);
            g.triangles.Add(idx); g.triangles.Add(idx + 1); g.triangles.Add(idx + 3);
            g.triangles.Add(idx); g.triangles.Add(idx + 3); g.triangles.Add(idx + 2);
        }

        Color32 RouteColorAt (float tNorm) {
            if (!_routeUseGradient) return _routeColor;
            return Color.Lerp(_routeColor, _routeGradientColor, tNorm);
        }

        // World arc length over the smoothed render polyline (mirrors routeArcWorld but for the displayed line)
        void BuildRouteRenderArc () {
            int m = routeRenderPts.Count;
            routeRenderArc.Clear();
            if (m == 0) return;
            routeRenderArc.Add(0f);
            for (int i = 1; i < m; i++) {
                routeRenderArc.Add(routeRenderArc[i - 1] + (routeRenderPts[i] - routeRenderPts[i - 1]).magnitude);
            }
        }

        void AddRouteVert (Vector2 p, float arc, float cross, float worldNorm, Color32 c, Vector2 rectSize) {
            var g = routeGraphic;
            g.vertices.Add(new Vector3(p.x, p.y, 0f));
            g.uv0.Add(new Vector2(arc, cross));
            g.uv1.Add(new Vector2(p.x / rectSize.x + 0.5f, p.y / rectSize.y + 0.5f));
            g.uv2.Add(new Vector2(worldNorm, 0f));
            g.vertexColors.Add(c);
        }

        bool RouteSegmentVisible (int s, float margin) {
            Vector3 p0 = routeScreenNorm[s];
            Vector3 p1 = routeScreenNorm[s + 1];
            // skip segments behind the minimap camera (near-plane)
            if (p0.z <= 0f || p1.z <= 0f) return false;
            float minx = Mathf.Min(p0.x, p1.x), maxx = Mathf.Max(p0.x, p1.x);
            float miny = Mathf.Min(p0.y, p1.y), maxy = Mathf.Max(p0.y, p1.y);
            if (maxx < -margin || minx > 1f + margin || maxy < -margin || miny > 1f + margin) return false;
            return true;
        }

        void EnsureRouteScratch (int n) {
            while (routeScreenNorm.Count < n) routeScreenNorm.Add(Vector3.zero);
            while (routeScreenLocal.Count < n) routeScreenLocal.Add(Vector2.zero);
            while (routeLLocal.Count < n) routeLLocal.Add(Vector2.zero);
            while (routeRLocal.Count < n) routeRLocal.Add(Vector2.zero);
            while (routePixelArc.Count < n) routePixelArc.Add(0f);
            if (routeVertexBase.Length < n) {
                int sz = routeVertexBase.Length;
                while (sz < n) sz *= 2;
                routeVertexBase = new int[sz];
            }
        }

        #endregion


        #region Route cue (managed POI at the next waypoint)

        Sprite GetRouteCueSprite () {
            if (_routeCompassBarSprite != null) return _routeCompassBarSprite;
            Sprite s = Resources.Load<Sprite>("CNPro/Sprites/route-cue");
            if (s == null) s = GetRouteCueDefaultSprite();
            return s;
        }

        void UpdateRouteCuePOI () {
            bool cueWanted = _routeShowOnCompassBar || _routeCueShowOnMiniMap;
            if (!cueWanted) { DestroyRouteCuePOI(); return; }

            EnsureRouteCuePOI();
            if (routeCuePOI == null) return;

            int idx = Mathf.Min(routeNextWaypoint, routePoints.Count - 1);
            routeCuePOIGO.transform.position = routePoints[idx];

            routeCuePOI.visibility = _routeShowOnCompassBar ? POIVisibility.AlwaysVisible : POIVisibility.AlwaysHidden;
            routeCuePOI.miniMapVisibility = _routeCueShowOnMiniMap ? POIVisibility.AlwaysVisible : POIVisibility.AlwaysHidden;
            routeCuePOI.tintColor = _routeColor;

            Sprite icon = GetRouteCueSprite();
            if (routeCuePOI.iconNonVisited != icon) {
                routeCuePOI.iconNonVisited = icon;
                routeCuePOI.iconVisited = icon;
            }
            string title = _routeWaypointTitle ?? "";
            if (routeCuePOI.title != title) routeCuePOI.title = title;
            routeCuePOI.titleVisibility = string.IsNullOrEmpty(title) ? TitleVisibility.OnlyWhenVisited : TitleVisibility.Always;
        }

        void EnsureRouteCuePOI () {
            if (routeCuePOIGO != null) return;
            routeCuePOIGO = new GameObject("Route Cue");
            if (routePoints.Count > 0) {
                int idx = Mathf.Min(routeNextWaypoint, routePoints.Count - 1);
                routeCuePOIGO.transform.position = routePoints[idx];
            }
            routeCuePOI = routeCuePOIGO.AddComponent<CompassProPOI>();
            routeCuePOI.isRouteWaypoint = true;
            routeCuePOI.canBeVisited = false;
            routeCuePOI.clampPosition = true;
            routeCuePOI.miniMapClampPosition = true;
            routeCuePOI.showOnScreenIndicator = false;
            routeCuePOI.showOffScreenIndicator = false;
            routeCuePOI.showSceneGizmo = false;
            Sprite icon = GetRouteCueSprite();
            routeCuePOI.iconNonVisited = icon;
            routeCuePOI.iconVisited = icon;
            routeCuePOI.tintColor = _routeColor;
        }

        void DestroyRouteCuePOI () {
            if (routeCuePOIGO != null) {
                Misc.DestroySafe(routeCuePOIGO);
                routeCuePOIGO = null;
                routeCuePOI = null;
            }
        }

        #endregion


        #region Route lifecycle

        Texture GetRouteMaskTexture () {
            return miniMapOverlayMat != null ? miniMapOverlayMat.GetTexture(ShaderParams.MaskTex) : null;
        }

        void EnsureRouteMaterial () {
            if (routeMat != null) return;
            Shader sh = Shader.Find("CompassNavigatorPro/RouteOverlay");
            if (sh == null) {
                Debug.LogError("[CNP Route] 'CompassNavigatorPro/RouteOverlay' shader not found.");
                return;
            }
            routeMat = new Material(sh);
            routeMat.hideFlags = HideFlags.DontSave;
        }

        void EnsureRouteGraphic () {
            if (routeGraphic != null) {
                if (!routeGraphic.gameObject.activeSelf) routeGraphic.gameObject.SetActive(true);
                return;
            }
            if (miniMapMaskUI == null) return;
            EnsureRouteMaterial();

            GameObject go = new GameObject("Route");
            go.transform.SetParent(miniMapMaskUI, false);
            routeGraphic = go.AddComponent<CompassProRouteGraphic>();
            routeGraphic.raycastTarget = false;
            routeGraphic.material = routeMat;
            routeGraphic.maskTexture = GetRouteMaskTexture();

            RectTransform rt = routeGraphic.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            go.transform.SetAsFirstSibling();

            // The route mesh carries the mask uv in TexCoord1; make sure the canvas keeps that channel
            if (_canvas != null) {
                _canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.TexCoord2;
            }
            Canvas c = routeGraphic.canvas;
            if (c != null && c != _canvas) {
                c.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.TexCoord2;
            }

            // force a rebuild against the fresh graphic
            lastRouteZoom = -1f;
        }

        void ApplyRouteMaterial () {
            if (routeMat == null) return;
            // Texture when assigned (styleId 4, tiling follows Tiling/density), otherwise a plain solid line (styleId 0)
            bool useTex = _routeTexture != null;
            routeMat.SetFloat(ShaderParams.RouteStyleId, useTex ? 4f : 0f);
            routeMat.SetTexture(ShaderParams.RouteTex, useTex ? _routeTexture : null);
            routeMat.SetVector(ShaderParams.RouteData, new Vector4(_routeTextureTiling, 0f, _routeEdgeFeather, 0f));
            routeMat.SetFloat(ShaderParams.RouteFlowId, _routeFlowSpeed);
            routeMat.SetFloat(ShaderParams.RouteProgress, _routeShowTraveled ? routeProgressValue : -1f);
            routeMat.SetColor(ShaderParams.RouteTraveledColor, _routeTraveledColor);
            routeMat.SetFloat(ShaderParams.RouteTraveledSolid, _routeTraveledSolid ? 1f : 0f);
            routeMat.SetTexture(ShaderParams.MaskTex, GetRouteMaskTexture());
        }

        // Projects the follow target onto the route polyline (XZ) and returns the travelled fraction [0..1]
        float ComputeRouteProgress () {
            int n = routePoints.Count;
            if (n < 2 || routeArcWorld.Count != n) return routeProgressValue;
            float total = routeArcWorld[n - 1];
            if (total < 1e-5f) return 0f;
            Vector3 p = followPos;
            float bestDistSq = float.MaxValue;
            float bestArc = 0f;
            for (int s = 0; s < n - 1; s++) {
                Vector3 a = routePoints[s], b = routePoints[s + 1];
                float abx = b.x - a.x, abz = b.z - a.z;
                float abLen2 = abx * abx + abz * abz;
                float t = abLen2 > 1e-8f ? Mathf.Clamp01(((p.x - a.x) * abx + (p.z - a.z) * abz) / abLen2) : 0f;
                float dx = p.x - (a.x + abx * t), dz = p.z - (a.z + abz * t);
                float dsq = dx * dx + dz * dz;
                if (dsq < bestDistSq) {
                    bestDistSq = dsq;
                    bestArc = routeArcWorld[s] + t * (routeArcWorld[s + 1] - routeArcWorld[s]);
                }
            }
            return Mathf.Clamp01(bestArc / total);
        }

        static Sprite GetRouteCueDefaultSprite () {
            if (routeCueDefaultSprite != null) return routeCueDefaultSprite;
            const int sz = 32;
            Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.hideFlags = HideFlags.DontSave;
            Color32[] px = new Color32[sz * sz];
            const float aa = 1.5f;
            for (int y = 0; y < sz; y++) {
                for (int x = 0; x < sz; x++) {
                    float u = (x + 0.5f) / sz;
                    float v = (y + 0.5f) / sz;           // 0 = bottom (tip), 1 = top (base)
                    float half = 0.5f * v;                // downward-pointing triangle
                    float du = Mathf.Abs(u - 0.5f);
                    float a = Mathf.Clamp01((half - du) * sz / aa);
                    a *= Mathf.Clamp01(v * sz / aa);
                    px[y * sz + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            routeCueDefaultSprite = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), 100f);
            routeCueDefaultSprite.hideFlags = HideFlags.DontSave;
            return routeCueDefaultSprite;
        }

        void HideRouteGraphic () {
            if (routeGraphic != null && routeGraphic.gameObject.activeSelf) {
                routeGraphic.gameObject.SetActive(false);
            }
        }

        void RouteDispose () {
            DestroyRouteCuePOI();
            DestroyRouteMarkers();
            RouteWorldDispose();
            if (routeMat != null) {
                DestroyImmediate(routeMat);
                routeMat = null;
            }
        }

        #endregion

    }
}
