using System.Collections.Generic;
using UnityEngine;

namespace CompassNavigatorPro {

    public enum RouteWidthSpace {
        Pixels = 0,
        World = 1
    }

    public enum RouteTilingSpace {
        Screen = 0,
        World = 1
    }

    public enum RouteWorldGroundMode {
        Raycast = 0,
        FixedHeight = 1
    }

    public enum RouteSmoothing {
        Straight = 0,
        Rounded = 1,
        Curved = 2
    }

    [System.Serializable]
    public class CompassProRouteWaypoint {
        [Tooltip("Optional scene Transform. If set, its position is used (and tracked) instead of the Position field.")]
        public Transform anchor;
        [Tooltip("World position of this waypoint (used when no Anchor is assigned).")]
        public Vector3 position;
        [Tooltip("Optional icon shown for this waypoint on the mini-map. If null, the default circle is used.")]
        public Sprite icon;
        [Tooltip("Tint applied to this waypoint's icon marker (mini-map and in-world), independent of the route line Color. White = the icon's natural colors.")]
        public Color iconColor = Color.white;

        public Vector3 GetPosition () {
            return anchor != null ? anchor.position : position;
        }

        public Color GetIconColor () {
            if (iconColor.r == 0f && iconColor.g == 0f && iconColor.b == 0f && iconColor.a == 0f) return Color.white;
            return iconColor;
        }
    }

    public partial class CompassPro : MonoBehaviour {

        #region Route public properties

        [Tooltip("Master switch for the route overlay (minimap polyline + compass bar cue).")]
        [SerializeField]
        bool _showRoute = true;
        public bool showRoute {
            get { return _showRoute; }
            set { if (value != _showRoute) { _showRoute = value; needUpdateRoute = true; } }
        }

        [Tooltip("Drive the route from the authored Waypoints list below instead of the scripting API.")]
        [SerializeField]
        bool _routeUseWaypoints;
        public bool routeUseWaypoints {
            get { return _routeUseWaypoints; }
            set { if (value != _routeUseWaypoints) { _routeUseWaypoints = value; needUpdateRoute = true; } }
        }

        [Tooltip("Authored route waypoints (position/anchor + optional icon). Used when 'Use Waypoints' is enabled.")]
        [SerializeField]
        List<CompassProRouteWaypoint> _routeWaypoints = new List<CompassProRouteWaypoint>();
        public List<CompassProRouteWaypoint> routeWaypoints {
            get { return _routeWaypoints; }
        }

        [Tooltip("Shows an icon marker on the mini-map at every waypoint (per-waypoint icon, or a default circle).")]
        [SerializeField]
        bool _routeShowWaypointMarkers;
        public bool routeShowWaypointMarkers {
            get { return _routeShowWaypointMarkers; }
            set { if (value != _routeShowWaypointMarkers) { _routeShowWaypointMarkers = value; needUpdateRoute = true; } }
        }

        [Tooltip("Pixel size of the per-waypoint markers on the mini-map.")]
        [SerializeField]
        float _routeWaypointMarkerSize = 16f;
        public float routeWaypointMarkerSize {
            get { return _routeWaypointMarkerSize; }
            set { if (value != _routeWaypointMarkerSize) { _routeWaypointMarkerSize = value; needUpdateRoute = true; } }
        }

        [Tooltip("Draws the route polyline on the minimap.")]
        [SerializeField]
        bool _routeShowOnMiniMap = true;
        public bool routeShowOnMiniMap {
            get { return _routeShowOnMiniMap; }
            set { if (value != _routeShowOnMiniMap) { _routeShowOnMiniMap = value; needUpdateRoute = true; } }
        }

        [Tooltip("Shows a directional cue on the compass bar pointing to the next route waypoint.")]
        [SerializeField]
        bool _routeShowOnCompassBar = true;
        public bool routeShowOnCompassBar {
            get { return _routeShowOnCompassBar; }
            set { if (value != _routeShowOnCompassBar) { _routeShowOnCompassBar = value; needUpdateRoute = true; } }
        }

        [Tooltip("Shows the next-waypoint cue as a marker on the mini-map (uses the managed route POI).")]
        [SerializeField]
        bool _routeCueShowOnMiniMap;
        public bool routeCueShowOnMiniMap {
            get { return _routeCueShowOnMiniMap; }
            set { if (value != _routeCueShowOnMiniMap) { _routeCueShowOnMiniMap = value; needUpdateRoute = true; } }
        }

        [Tooltip("Route line color (start color when gradient is enabled).")]
        [SerializeField]
        Color _routeColor = new Color(0.25f, 0.85f, 1f, 1f);
        public Color routeColor {
            get { return _routeColor; }
            set { if (value != _routeColor) { _routeColor = value; needUpdateRoute = true; } }
        }

        [Tooltip("Optional texture tiled along the mini-map line (replaces the procedural Style when assigned). Its alpha is the shape; it's tinted by Color. Tiling follows Pattern Density. Set the texture wrap mode to Repeat.")]
        [SerializeField]
        Texture2D _routeTexture;
        public Texture2D routeTexture {
            get { return _routeTexture; }
            set { if (value != _routeTexture) { _routeTexture = value; needUpdateRoute = true; } }
        }

        [Tooltip("Blend the line color from start to destination.")]
        [SerializeField]
        bool _routeUseGradient;
        public bool routeUseGradient {
            get { return _routeUseGradient; }
            set { if (value != _routeUseGradient) { _routeUseGradient = value; needUpdateRoute = true; } }
        }

        [Tooltip("Route line color near the destination (used when gradient is enabled).")]
        [SerializeField]
        Color _routeGradientColor = new Color(1f, 1f, 1f, 1f);
        public Color routeGradientColor {
            get { return _routeGradientColor; }
            set { if (value != _routeGradientColor) { _routeGradientColor = value; needUpdateRoute = true; } }
        }

        [Tooltip("Colors the portion of the route already travelled with a different color.")]
        [SerializeField]
        bool _routeShowTraveled = true;
        public bool routeShowTraveled {
            get { return _routeShowTraveled; }
            set { if (value != _routeShowTraveled) { _routeShowTraveled = value; needUpdateRoute = true; } }
        }

        [Tooltip("Color (and alpha) applied to the already-travelled part of the route.")]
        [SerializeField]
        Color _routeTraveledColor = new Color(0.55f, 0.55f, 0.55f, 0.35f);
        public Color routeTraveledColor {
            get { return _routeTraveledColor; }
            set { if (value != _routeTraveledColor) { _routeTraveledColor = value; needUpdateRoute = true; } }
        }

        [Tooltip("Draws the already-travelled part as a solid line (no marching ants / pattern). The remaining part keeps the style.")]
        [SerializeField]
        bool _routeTraveledSolid = true;
        public bool routeTraveledSolid {
            get { return _routeTraveledSolid; }
            set { if (value != _routeTraveledSolid) { _routeTraveledSolid = value; needUpdateRoute = true; } }
        }

        [Tooltip("Automatically computes travelled progress by projecting the follow target onto the route. Disable to set routeProgress yourself.")]
        [SerializeField]
        bool _routeAutoProgress = true;
        public bool routeAutoProgress {
            get { return _routeAutoProgress; }
            set { _routeAutoProgress = value; }
        }

        /// <summary>
        /// Travelled fraction of the route (0..1). Auto-computed when routeAutoProgress is on; otherwise set it yourself.
        /// </summary>
        public float routeProgress {
            get { return routeProgressValue; }
            set { routeProgressValue = Mathf.Clamp01(value); }
        }

        [Tooltip("Line thickness. Interpreted in screen pixels or world meters depending on Route Width Space.")]
        [SerializeField]
        float _routeWidth = 3f;
        public float routeWidth {
            get { return _routeWidth; }
            set { value = Mathf.Max(0f, value); if (value != _routeWidth) { _routeWidth = value; needUpdateRoute = true; } }
        }

        [Tooltip("Pixels = constant on-screen thickness. World = thickness in meters that foreshortens under perspective/tilt.")]
        [SerializeField]
        RouteWidthSpace _routeWidthSpace = RouteWidthSpace.Pixels;
        public RouteWidthSpace routeWidthSpace {
            get { return _routeWidthSpace; }
            set { if (value != _routeWidthSpace) { _routeWidthSpace = value; needUpdateRoute = true; } }
        }

        [Tooltip("Texture tiling space: Screen = tile spacing constant on screen; World = tile length in meters.")]
        [SerializeField]
        RouteTilingSpace _routeTextureTilingSpace = RouteTilingSpace.Screen;
        public RouteTilingSpace routeTextureTilingSpace {
            get { return _routeTextureTilingSpace; }
            set { if (value != _routeTextureTilingSpace) { _routeTextureTilingSpace = value; needUpdateRoute = true; } }
        }

        [Tooltip("Texture repetitions per arc unit (per pixel in Screen mode, per meter in World mode).")]
        [SerializeField]
        float _routeTextureTiling = 0.05f;
        public float routeTextureTiling {
            get { return _routeTextureTiling; }
            set { if (value != _routeTextureTiling) { _routeTextureTiling = value; } }
        }

        [Tooltip("Antialiasing softness of the line edges.")]
        [SerializeField, Range(0.1f, 4f)]
        float _routeEdgeFeather = 1.5f;
        public float routeEdgeFeather {
            get { return _routeEdgeFeather; }
            set { if (value != _routeEdgeFeather) { _routeEdgeFeather = value; } }
        }

        [Tooltip("Animated flow speed of the texture towards the destination. 0 = static.")]
        [SerializeField]
        float _routeFlowSpeed = 1f;
        public float routeFlowSpeed {
            get { return _routeFlowSpeed; }
            set { _routeFlowSpeed = value; }
        }

        [Tooltip("Sprite for the route cue (the icon of the auto-managed POI at the next waypoint).")]
        [SerializeField]
        Sprite _routeCompassBarSprite;
        public Sprite routeCompassBarSprite {
            get { return _routeCompassBarSprite; }
            set { if (value != _routeCompassBarSprite) { _routeCompassBarSprite = value; needUpdateRoute = true; } }
        }

        [Tooltip("Optional title shown on the route cue POI (e.g. \"Destination\"). Empty = no title.")]
        [SerializeField]
        string _routeWaypointTitle = "";
        public string routeWaypointTitle {
            get { return _routeWaypointTitle; }
            set { if (value != _routeWaypointTitle) { _routeWaypointTitle = value; needUpdateRoute = true; } }
        }

        [Tooltip("Automatically consume waypoints as the follow target reaches them.")]
        [SerializeField]
        bool _routeAutoAdvance = true;
        public bool routeAutoAdvance {
            get { return _routeAutoAdvance; }
            set { _routeAutoAdvance = value; }
        }

        [Tooltip("Distance at which a waypoint is considered reached (auto-advance).")]
        [SerializeField]
        float _routeWaypointReachDistance = 3f;
        public float routeWaypointReachDistance {
            get { return _routeWaypointReachDistance; }
            set { _routeWaypointReachDistance = value; }
        }

        [Tooltip("Automatically clears the route when the last waypoint is reached (fires OnRouteCompleted then OnRouteCleared).")]
        [SerializeField]
        bool _routeClearOnComplete;
        public bool routeClearOnComplete {
            get { return _routeClearOnComplete; }
            set { _routeClearOnComplete = value; }
        }

        [Tooltip("As Auto Advance progresses, hides the markers of waypoints already reached (the travelled part), on both the mini-map and in-world.")]
        [SerializeField]
        bool _routeHideTraveledWaypointMarkers;
        public bool routeHideTraveledWaypointMarkers {
            get { return _routeHideTraveledWaypointMarkers; }
            set { _routeHideTraveledWaypointMarkers = value; }
        }

        [Tooltip("Maximum number of route segments tessellated on the minimap per frame.")]
        [SerializeField]
        int _routeMaxSegments = 512;
        public int routeMaxSegments {
            get { return _routeMaxSegments; }
            set { if (value != _routeMaxSegments) { _routeMaxSegments = value; needUpdateRoute = true; } }
        }

        [Tooltip("Draws the route as a holographic line in the world, projected on the ground (seen through the main camera). Uses the same color/style/travelled settings as the mini-map route.")]
        [SerializeField]
        bool _routeShowInWorld;
        public bool routeShowInWorld {
            get { return _routeShowInWorld; }
            set { if (value != _routeShowInWorld) { _routeShowInWorld = value; needUpdateRoute = true; } }
        }

        [Tooltip("Renders the in-world route line in the Scene view while editing, without entering Play mode, as a live preview of the authored waypoints. Uses immediate-mode rendering (no scene objects created). Has no effect in Play mode.")]
        [SerializeField]
        bool _routeWorldEditPreview = true;
        public bool routeWorldEditPreview {
            get { return _routeWorldEditPreview; }
            set { if (value != _routeWorldEditPreview) { _routeWorldEditPreview = value; needUpdateRoute = true; } }
        }

        [Tooltip("Shows a billboard marker (the waypoint icon) at each waypoint in the 3D world.")]
        [SerializeField]
        bool _routeWorldShowWaypointMarkers;
        public bool routeWorldShowWaypointMarkers {
            get { return _routeWorldShowWaypointMarkers; }
            set { if (value != _routeWorldShowWaypointMarkers) { _routeWorldShowWaypointMarkers = value; needUpdateRoute = true; } }
        }

        [Tooltip("World-space size (meters) of the in-world waypoint markers.")]
        [SerializeField]
        float _routeWorldWaypointMarkerSize = 1f;
        public float routeWorldWaypointMarkerSize {
            get { return _routeWorldWaypointMarkerSize; }
            set { value = Mathf.Max(0f, value); if (value != _routeWorldWaypointMarkerSize) { _routeWorldWaypointMarkerSize = value; needUpdateRoute = true; } }
        }

        [Tooltip("Additional world-space height (meters) to raise or lower the in-world waypoint markers above the ground.")]
        [SerializeField]
        float _routeWorldWaypointMarkerYOffset;
        public float routeWorldWaypointMarkerYOffset {
            get { return _routeWorldWaypointMarkerYOffset; }
            set { if (value != _routeWorldWaypointMarkerYOffset) { _routeWorldWaypointMarkerYOffset = value; needUpdateRoute = true; } }
        }

        [Tooltip("World-space width (meters) of the in-world route line.")]
        [SerializeField]
        float _routeWorldWidth = 1f;
        public float routeWorldWidth {
            get { return _routeWorldWidth; }
            set { value = Mathf.Max(0f, value); if (value != _routeWorldWidth) { _routeWorldWidth = value; needUpdateRoute = true; } }
        }

        [Tooltip("Draws the in-world line on top of everything (ignores depth). Off = the line is occluded by world geometry.")]
        [SerializeField]
        bool _routeWorldAlwaysVisible = true;
        public bool routeWorldAlwaysVisible {
            get { return _routeWorldAlwaysVisible; }
            set { _routeWorldAlwaysVisible = value; routeWorldMatDirty = true; }
        }

        [Tooltip("Fades the in-world line out beyond this distance (meters) from the follow target so it doesn't reach the horizon. 0 = no limit.")]
        [SerializeField]
        float _routeWorldMaxDistance;
        public float routeWorldMaxDistance {
            get { return _routeWorldMaxDistance; }
            set { value = Mathf.Max(0f, value); if (value != _routeWorldMaxDistance) { _routeWorldMaxDistance = value; needUpdateRoute = true; } }
        }

        [Tooltip("The in-world line and markers are hidden when closer than this distance (meters) to the follow target, so they don't clutter the view around the player. 0 = visible right up to the player.")]
        [SerializeField]
        float _routeWorldMinDistance;
        public float routeWorldMinDistance {
            get { return _routeWorldMinDistance; }
            set { value = Mathf.Max(0f, value); if (value != _routeWorldMinDistance) { _routeWorldMinDistance = value; needUpdateRoute = true; } }
        }

        [Tooltip("Width (meters) of the fade-in band that starts at Min Distance. 0 = hard edge (the route appears abruptly at Min Distance).")]
        [SerializeField]
        float _routeWorldFadeDistance;
        public float routeWorldFadeDistance {
            get { return _routeWorldFadeDistance; }
            set { value = Mathf.Max(0f, value); if (value != _routeWorldFadeDistance) { _routeWorldFadeDistance = value; needUpdateRoute = true; } }
        }

        [Tooltip("Shows the travelled-part coloring on the in-world line. The mini-map has its own Color Travelled Part toggle, so you can show the travelled part on the mini-map but not in-world.")]
        [SerializeField]
        bool _routeWorldShowTraveled = false;
        public bool routeWorldShowTraveled {
            get { return _routeWorldShowTraveled; }
            set { if (value != _routeWorldShowTraveled) { _routeWorldShowTraveled = value; needUpdateRoute = true; } }
        }

        [Tooltip("Color (and alpha) of the already-travelled part of the in-world line. Set alpha to 0 to hide the travelled part in-world only (the mini-map keeps its own Travelled Color).")]
        [SerializeField]
        Color _routeWorldTraveledColor = new Color(0.55f, 0.55f, 0.55f, 0.35f);
        public Color routeWorldTraveledColor {
            get { return _routeWorldTraveledColor; }
            set { if (value != _routeWorldTraveledColor) { _routeWorldTraveledColor = value; needUpdateRoute = true; } }
        }

        [Tooltip("How the in-world line is placed vertically: Raycast drapes it onto the ground; Fixed Height draws it at a constant world Y with no raycast.")]
        [SerializeField]
        RouteWorldGroundMode _routeWorldGroundMode = RouteWorldGroundMode.FixedHeight;
        public RouteWorldGroundMode routeWorldGroundMode {
            get { return _routeWorldGroundMode; }
            set { if (value != _routeWorldGroundMode) { _routeWorldGroundMode = value; needUpdateRoute = true; } }
        }

        [Tooltip("World Y the in-world line is drawn at when Ground Projection = Fixed Height (no raycast).")]
        [SerializeField]
        float _routeWorldFixedHeight;
        public float routeWorldFixedHeight {
            get { return _routeWorldFixedHeight; }
            set { if (value != _routeWorldFixedHeight) { _routeWorldFixedHeight = value; needUpdateRoute = true; } }
        }

        [Tooltip("Layer mask of the ground/surfaces the in-world route is draped onto via raycast (Raycast mode). None = use the raw waypoint heights.")]
        [SerializeField]
        LayerMask _routeWorldGroundMask;
        public LayerMask routeWorldGroundMask {
            get { return _routeWorldGroundMask; }
            set { _routeWorldGroundMask = value; needUpdateRoute = true; }
        }

        [Tooltip("Height (meters) the in-world line is lifted above the ground to avoid z-fighting.")]
        [SerializeField]
        float _routeWorldYOffset = 0.05f;
        public float routeWorldYOffset {
            get { return _routeWorldYOffset; }
            set { value = Mathf.Max(0f, value); if (value != _routeWorldYOffset) { _routeWorldYOffset = value; needUpdateRoute = true; } }
        }

        [Tooltip("Distance (meters) between ground samples along the line. Smaller follows terrain better. 0 = sample only at waypoints.")]
        [SerializeField]
        float _routeWorldSampleStep = 2f;
        public float routeWorldSampleStep {
            get { return _routeWorldSampleStep; }
            set { value = Mathf.Max(0f, value); if (value != _routeWorldSampleStep) { _routeWorldSampleStep = value; needUpdateRoute = true; } }
        }

        [Tooltip("Optional texture tiled along the in-world line (replaces the procedural Style when assigned). Its alpha is the shape; it's tinted by Color. Set the texture wrap mode to Repeat.")]
        [SerializeField]
        Texture2D _routeWorldTexture;
        public Texture2D routeWorldTexture {
            get { return _routeWorldTexture; }
            set { if (value != _routeWorldTexture) { _routeWorldTexture = value; needUpdateRoute = true; } }
        }

        [Tooltip("Length in meters of one texture repetition along the route (tiling).")]
        [SerializeField]
        float _routeWorldTextureLength = 4f;
        public float routeWorldTextureLength {
            get { return _routeWorldTextureLength; }
            set { value = Mathf.Max(0.01f, value); if (value != _routeWorldTextureLength) { _routeWorldTextureLength = value; needUpdateRoute = true; } }
        }

        [Tooltip("Animated flow speed of the in-world line pattern. 0 = static.")]
        [SerializeField]
        float _routeWorldFlowSpeed = 1f;
        public float routeWorldFlowSpeed {
            get { return _routeWorldFlowSpeed; }
            set { if (value != _routeWorldFlowSpeed) { _routeWorldFlowSpeed = value; needUpdateRoute = true; } }
        }

        [Tooltip("Antialiasing softness of the in-world line edges.")]
        [SerializeField, Range(0.1f, 4f)]
        float _routeWorldEdgeFeather = 1.5f;
        public float routeWorldEdgeFeather {
            get { return _routeWorldEdgeFeather; }
            set { if (value != _routeWorldEdgeFeather) { _routeWorldEdgeFeather = value; needUpdateRoute = true; } }
        }

        [Tooltip("How the in-world ribbon follows the route points: Straight segments, Rounded corners (fillet), or Curved (a spline that bends through every point).")]
        [SerializeField]
        RouteSmoothing _routeSmoothing = RouteSmoothing.Rounded;
        public RouteSmoothing routeSmoothing {
            get { return _routeSmoothing; }
            set { if (value != _routeSmoothing) { _routeSmoothing = value; needUpdateRoute = true; } }
        }

        [Tooltip("Rounds route corners by inserting a fillet arc of this radius (m) at each bend so textures flow around turns. 0 = sharp corners (filled with a bevel).")]
        [SerializeField]
        float _routeCornerRadius = 1f;
        public float routeCornerRadius {
            get { return _routeCornerRadius; }
            set { value = Mathf.Max(0f, value); if (value != _routeCornerRadius) { _routeCornerRadius = value; needUpdateRoute = true; } }
        }

        [Tooltip("Curve detail: subdivisions per segment when Smoothing is set to Curved. Higher = smoother spline, more triangles.")]
        [SerializeField, Range(1, 32)]
        int _routeCurveResolution = 8;
        public int routeCurveResolution {
            get { return _routeCurveResolution; }
            set { value = Mathf.Clamp(value, 1, 64); if (value != _routeCurveResolution) { _routeCurveResolution = value; needUpdateRoute = true; } }
        }

        #endregion

        #region Route public API

        /// <summary>
        /// Sets the active route as a list of world space points. The compass draws it; you (or a pathfinder) provide the points.
        /// </summary>
        public void SetRoute (IList<Vector3> worldPoints) {
            RouteApiTakeover();
            routePoints.Clear();
            if (worldPoints != null) {
                for (int k = 0; k < worldPoints.Count; k++) {
                    routePoints.Add(worldPoints[k]);
                }
            }
            routeStartFollowsPlayer = false;
            routeDestinationPOI = null;
            routeNextWaypoint = routePoints.Count > 1 ? 1 : 0;
            RecomputeRouteWorldArc();
            needUpdateRoute = true;
            OnRouteUpdated?.Invoke();
        }

        /// <summary>
        /// Sets a direct guided line from the follow target to a destination POI. The start tracks the player and the end tracks the POI.
        /// </summary>
        public void SetRouteToPOI (CompassProPOI poi) {
            if (poi == null) { ClearRoute(); return; }
            RouteApiTakeover();
            routePoints.Clear();
            routePoints.Add(followPos);
            routePoints.Add(poi.transform.position);
            routeStartFollowsPlayer = true;
            routeDestinationPOI = poi;
            routeNextWaypoint = 1;
            RecomputeRouteWorldArc();
            needUpdateRoute = true;
            OnRouteUpdated?.Invoke();
        }

        /// <summary>
        /// Sets a direct guided line from the follow target to a world position. The start tracks the player.
        /// </summary>
        public void SetRouteToDestination (Vector3 worldPos) {
            RouteApiTakeover();
            routePoints.Clear();
            routePoints.Add(followPos);
            routePoints.Add(worldPos);
            routeStartFollowsPlayer = true;
            routeDestinationPOI = null;
            routeNextWaypoint = 1;
            RecomputeRouteWorldArc();
            needUpdateRoute = true;
            OnRouteUpdated?.Invoke();
        }

        /// <summary>
        /// Clears the active route.
        /// </summary>
        public void ClearRoute () {
            bool had = routePoints.Count > 0;
            routePoints.Clear();
            routeStartFollowsPlayer = false;
            routeDestinationPOI = null;
            routeNextWaypoint = 0;
            needUpdateRoute = true;
            if (had) OnRouteCleared?.Invoke();
        }

        /// <summary>
        /// Current route points (world space). Read-only view; use SetRoute to change.
        /// </summary>
        public IReadOnlyList<Vector3> route => routePoints;

        /// <summary>
        /// True if there is an active route with at least 2 points.
        /// </summary>
        public bool hasRoute => routePoints.Count > 1;

        /// <summary>
        /// Index of the next un-reached waypoint.
        /// </summary>
        public int routeNextWaypointIndex => routeNextWaypoint;

        /// <summary>
        /// The auto-managed POI placed at the next waypoint (the route cue). Null if the cue is not active.
        /// Tagged with isRouteWaypoint = true so you can filter it from your own POI logic.
        /// </summary>
        public CompassProPOI routeWaypointPOI => routeCuePOI;

        #endregion

    }
}
