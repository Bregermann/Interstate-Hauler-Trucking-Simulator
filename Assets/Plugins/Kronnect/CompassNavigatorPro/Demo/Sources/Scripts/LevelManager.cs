using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using CompassNavigatorPro;

namespace CompassNavigatorProDemos {
    public class LevelManager : MonoBehaviour {

        public int seed = 0;
        public int initialPoiCount = 1;
        public Material sphereMaterial;
        public Sprite[] icons;
        public AudioClip[] soundClips;

        int poiNumber;
        CompassPro compass;
        CompassProPOI POIUnderMouse;
        readonly List<Vector3> routeWaypoints = new List<Vector3>();

        string SaveFilePath => Path.Combine(Application.persistentDataPath, "cnp3_savestate.dat");

        void OnEnable() {
            InputProxy.SetupEventSystem();
        }

        IEnumerator Start () {
            Random.InitState(seed);
            
            // Get a reference to the Compass Pro Navigator component
            compass = CompassPro.instance;

            // Add a callback when POIs are reached
            compass.OnPOIVisited.AddListener(OnPOIVisited);

            // Subscribe to Compass minimap events
            compass.OnMiniMapMouseClick.AddListener(OnMiniMapClick);
            compass.OnPOIMiniMapIconMouseEnter.AddListener(OnPOIHover);
            compass.OnPOIMiniMapIconMouseExit.AddListener(OnPOIExit);
            compass.OnPOIMiniMapIconMouseClick.AddListener(OnPOIClick);
            compass.OnPOIEnterCircle.AddListener(OnPOIEnterCircle);
            compass.OnPOIExitCircle.AddListener(OnPOIExitCircle);

            // Subscribe to route events
            compass.OnRouteWaypointReached.AddListener(OnRouteWaypointReached);
            compass.OnRouteCompleted.AddListener(OnRouteCompleted);

            // Populate the scene with initial POIs
            WaitForSeconds w = new WaitForSeconds(0.5f);
            for (int k = 1; k <= initialPoiCount; k++) {
                yield return w;
                AddRandomPOI();
            }
        }

        void Update () {

            if (InputProxy.GetKeyDown(KeyCode.B)) {
                compass.POIShowBeacon(5f, 1.1f, 1f, new Color(1, 1, 0.25f));
            }
            if (InputProxy.GetKey(KeyCode.Z)) {
                compass.miniMapZoomLevel -= Time.deltaTime;
            }
            if (InputProxy.GetKey(KeyCode.X)) {
                compass.miniMapZoomLevel += Time.deltaTime;
            }
            if (InputProxy.GetKeyDown(KeyCode.C)) {
                compass.showCompassBar = !compass.showCompassBar;
            }
            if (InputProxy.GetKeyDown(KeyCode.M)) {
                compass.showMiniMap = !compass.showMiniMap;
            }
            if (InputProxy.GetKeyDown(KeyCode.V)) {
                Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f));
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit)) {
                    compass.POIShowBeacon(hit.point, 5f, 0.4f, 1f, new Color(0, 0.5f, 1f));
                }
            }
            if (InputProxy.GetKeyDown(KeyCode.T)) {
                compass.miniMapFullScreenState = !compass.miniMapFullScreenState;
            }
            if (InputProxy.GetKeyDown(KeyCode.H)) {
                foreach (var poi in compass.pois) {
                    poi.StartCircleAnimation();
                }
            }
            if (InputProxy.GetKeyDown(KeyCode.F)) {
                ScanEffect scan = compass.Scan(); // Scan method has many optional parameters
                scan.OnScanHit.AddListener(OnScanHit);
            }
            if (InputProxy.GetKeyDown(KeyCode.R)) {
                ToggleDemoRoute();
            }
            if (InputProxy.GetKeyDown(KeyCode.F5)) {
                SaveCompassState();
            }
            if (InputProxy.GetKeyDown(KeyCode.F9)) {
                LoadCompassState();
            }
        }

        // Save/Load State API demo (F5 saves, F9 reloads); state is written to persistentDataPath so it survives a session restart
        void SaveCompassState () {
            byte[] data = compass.SaveState();
            if (data == null || data.Length == 0) {
                Debug.Log("[CNP SaveLoad] nothing to save");
                return;
            }
            File.WriteAllBytes(SaveFilePath, data);
            Debug.Log($"[CNP SaveLoad] saved {data.Length} bytes to {SaveFilePath}");
        }

        void LoadCompassState () {
            if (!File.Exists(SaveFilePath)) {
                Debug.Log("[CNP SaveLoad] nothing saved yet");
                return;
            }
            CompassLoadResult result = compass.LoadState(File.ReadAllBytes(SaveFilePath));
            if (result.ok) {
                Debug.Log($"[CNP SaveLoad] loaded (restored {result.poisRestored}, recreated {result.poisRecreated}, unmatched {result.poisUnmatched})");
            } else {
                Debug.Log("[CNP SaveLoad] load error: " + result.error);
            }
        }

        // Builds a multi-waypoint route from the player through the active POIs (press R again to clear)
        void ToggleDemoRoute () {
            if (compass.hasRoute) {
                compass.ClearRoute();
                Debug.Log("[CNP Route] route cleared");
                return;
            }
            routeWaypoints.Clear();
            Vector3 start = compass.follow != null ? compass.follow.position : (Camera.main != null ? Camera.main.transform.position : Vector3.zero);

            List<Vector3> targets = new List<Vector3>();
            foreach (var poi in compass.pois) {
                if (poi == null || !poi.isActiveAndEnabled || poi.isRouteWaypoint) continue;
                targets.Add(poi.transform.position);
                if (targets.Count >= 5) break;
            }
            if (targets.Count == 0) {
                Debug.Log("[CNP Route] no POIs available to build a route");
                return;
            }

            // Order waypoints so the route doesn't cross itself on the minimap
            BuildNonCrossingRoute(start, targets, routeWaypoints);

            compass.SetRoute(routeWaypoints);
            Debug.Log($"[CNP Route] route set through {targets.Count} POIs ({routeWaypoints.Count} points)");
        }

        // Nearest-neighbor ordering plus 2-opt; a locally shortest path has no self-intersections
        void BuildNonCrossingRoute (Vector3 start, List<Vector3> targets, List<Vector3> result) {
            result.Clear();
            result.Add(start);

            List<Vector3> remaining = new List<Vector3>(targets);
            Vector3 current = start;
            while (remaining.Count > 0) {
                int nearest = 0;
                float best = float.MaxValue;
                for (int i = 0; i < remaining.Count; i++) {
                    float d = FlatSqrDist(current, remaining[i]);
                    if (d < best) {
                        best = d;
                        nearest = i;
                    }
                }
                current = remaining[nearest];
                result.Add(current);
                remaining.RemoveAt(nearest);
            }

            bool improved = true;
            while (improved) {
                improved = false;
                for (int i = 1; i < result.Count - 1; i++) {
                    for (int j = i + 1; j < result.Count; j++) {
                        bool hasTail = j + 1 < result.Count;
                        float before = FlatDist(result[i - 1], result[i]) + (hasTail ? FlatDist(result[j], result[j + 1]) : 0f);
                        float after = FlatDist(result[i - 1], result[j]) + (hasTail ? FlatDist(result[i], result[j + 1]) : 0f);
                        if (after + 1e-4f < before) {
                            result.Reverse(i, j - i + 1);
                            improved = true;
                        }
                    }
                }
            }
        }

        static float FlatSqrDist (Vector3 a, Vector3 b) {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return dx * dx + dz * dz;
        }

        static float FlatDist (Vector3 a, Vector3 b) => Mathf.Sqrt(FlatSqrDist(a, b));

        void OnRouteWaypointReached (int index, Vector3 position) {
            Debug.Log($"[CNP Route] waypoint {index} reached at {position}");
        }

        void OnRouteCompleted () {
            Debug.Log("[CNP Route] route completed");
        }

        /// <summary>
        /// This event is triggered when the scan effect hits a collider that matches the hit layer mask of the scan effect
        /// </summary>
        void OnScanHit (ScanEffect scan, CompassProPOI poi, Transform hitTransform) {
            if (!poi.enabled) {
                poi.enabled = true;
            }
            // start circle animation in minimap
            poi.StartCircleAnimation();
            // show beacon at POI position
            compass.POIShowBeacon(poi, duration: 3f);
        }

        void OnGUI () {
            // Example of tooltip for POI under mouse
            if (POIUnderMouse != null) {
                Rect rect = POIUnderMouse.GetMiniMapIconScreenRect();
                rect = new Rect(rect.center.x - 100, Screen.height - rect.y - 85, 200, 25);
                GUIStyle style = GUI.skin.GetStyle("Label");
                style.alignment = TextAnchor.UpperCenter;
                style.normal.textColor = Color.yellow;
                GUI.Label(rect, POIUnderMouse.title, style);
            }
        }

        void AddRandomPOI () {
            Vector3 position = new Vector3(Random.Range(-50, 50), 1, Random.Range(-50, 50));
            AddPOI(position);
        }


        void AddPOI (Vector3 position) {
            // Create placeholder
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.transform.position = position;
            obj.GetComponent<Renderer>().material = sphereMaterial;

            // Add POI info
            CompassProPOI poi = obj.AddComponent<CompassProPOI>();

            // Title name and reveal text
            poi.title = "Target " + (++poiNumber).ToString();
            poi.titleVisibility = TitleVisibility.Always;
            poi.visitedText = "Target " + poiNumber + " acquired!";

            // Assign icons
            int j = Random.Range(0, icons.Length / 2);
            poi.iconNonVisited = icons[j * 2];
            poi.iconVisited = icons[j * 2 + 1];

            // Enable indicators
            poi.showOnScreenIndicator = true;
            poi.showOffScreenIndicator = true;

            // Hide poi when visited and random sound
            poi.hideWhenVisited = true;
            j = Random.Range(0, soundClips.Length);
            poi.visitedAudioClipOverride = soundClips[j];

            // Make a circle animation appear
            poi.miniMapCircleAnimationWhenAppears = true;
            poi.miniMapCircleAnimationRepetitions = 1;
        }

        void OnPOIVisited (CompassProPOI poi) {
            Debug.Log($"{poi.title} has been reached.");
            StartCoroutine(PickUpPOI(poi));
        }

        IEnumerator PickUpPOI (CompassProPOI poi) {
            for (int k = 0; k < 10; k++) {
                poi.transform.localScale *= 0.9f;
                yield return null;
            }
            Destroy(poi.gameObject);
            AddRandomPOI();
        }

        void OnPOIClick (CompassProPOI poi, int mouseButtonIndex) {
            Debug.Log(poi.title + " has been clicked on minimap.");
            if (mouseButtonIndex == 1) {
                Debug.Log($"Removing POI...");
                Destroy(poi.gameObject);
            }
        }

        void OnPOIHover (CompassProPOI poi) {
            POIUnderMouse = poi;
        }

        void OnPOIExit (CompassProPOI poi) {
            POIUnderMouse = null;
        }

        void OnMiniMapClick (Vector3 position, int buttonIndex) {
            if (buttonIndex == 0) {
                Debug.Log($"User clicked on mini-map. Creating a POI at world position: {position}");
                position.y = 1f;
                AddPOI(position);
            } else {
                // re-center mini-map with secondary mouse button
                compass.ResetDragOffset();
            }
        }

        void OnPOIEnterCircle (CompassProPOI poi) {
            Debug.Log($"Entering circle of poi {poi.title}");
        }

        void OnPOIExitCircle (CompassProPOI poi) {
            Debug.Log($"Exiting circle of poi {poi.title}");
        }


    }
}