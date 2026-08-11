using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace CompassNavigatorPro {

    /// <summary>
    /// Optional helper that computes a path to a target using Unity's NavMesh and feeds it to the compass route.
    /// Keeps the compass core decoupled from NavMesh: the compass draws, this component (or your own pathfinder) computes.
    /// </summary>
    [AddComponentMenu("Compass Navigator Pro/Compass Pro NavMesh Route")]
    public class CompassProNavMeshRoute : MonoBehaviour {

        [Tooltip("Compass to feed the route to. If null, uses CompassPro.instance.")]
        public CompassPro compass;

        [Tooltip("Destination transform. Ignored if a Destination POI is set.")]
        public Transform target;

        [Tooltip("Destination POI. Takes precedence over Destination transform.")]
        public CompassProPOI targetPOI;

        [Tooltip("Recompute the path when the follow target or destination moves more than this distance (meters).")]
        public float recomputeDistance = 1.5f;

        [Tooltip("Minimum seconds between path recomputations.")]
        public float recomputeInterval = 0.25f;

        [Tooltip("NavMesh area mask used for the path query.")]
        public int areaMask = NavMesh.AllAreas;

        [Tooltip("Clear the route automatically when this component is disabled.")]
        public bool clearRouteWhenDisabled = true;

        readonly List<Vector3> corners = new List<Vector3>();
        NavMeshPath path;
        Vector3 lastStart, lastEnd;
        float lastTime;
        bool hasComputed;

        void Reset () {
            compass = FindObjectOfType<CompassPro>();
        }

        void OnEnable () {
            if (compass == null) compass = CompassPro.instance;
            path = new NavMeshPath();
            hasComputed = false;
        }

        void OnDisable () {
            if (clearRouteWhenDisabled && compass != null) compass.ClearRoute();
        }

        Vector3 GetDestination () {
            if (targetPOI != null) return targetPOI.transform.position;
            if (target != null) return target.position;
            return transform.position;
        }

        void Update () {
            if (compass == null || compass.follow == null) return;
            if (target == null && targetPOI == null) return;

            Vector3 start = compass.follow.position;
            Vector3 end = GetDestination();

            float now = Time.time;
            bool due = !hasComputed ||
                (now - lastTime >= recomputeInterval &&
                    ((start - lastStart).sqrMagnitude >= recomputeDistance * recomputeDistance ||
                     (end - lastEnd).sqrMagnitude >= recomputeDistance * recomputeDistance));
            if (!due) return;

            lastTime = now;
            lastStart = start;
            lastEnd = end;
            hasComputed = true;

            if (NavMesh.CalculatePath(start, end, areaMask, path) && path.corners.Length > 1) {
                corners.Clear();
                for (int i = 0; i < path.corners.Length; i++) {
                    corners.Add(path.corners[i]);
                }
                compass.SetRoute(corners);
            }
        }
    }
}
