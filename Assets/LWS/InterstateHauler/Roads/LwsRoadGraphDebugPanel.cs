using UnityEngine;

namespace LWS.InterstateHauler
{
    [DisallowMultipleComponent]
    public sealed class LwsRoadGraphDebugPanel : MonoBehaviour
    {
        [SerializeField] private LwsRoadGraphProvider roadGraphProvider;
        [SerializeField] private bool visible = true;
        [SerializeField] private float refreshIntervalSeconds = 0.25f;

        private float _nextRefreshTime;
        private LwsRoadLookupResult _lastResult = LwsRoadLookupResult.None;

        private void Awake()
        {
            if (roadGraphProvider == null)
            {
                roadGraphProvider = GetComponent<LwsRoadGraphProvider>();
            }
        }

        private void Update()
        {
            if (!visible || Time.unscaledTime < _nextRefreshTime)
            {
                return;
            }

            _nextRefreshTime = Time.unscaledTime + Mathf.Max(0.05f, refreshIntervalSeconds);
            Vector3 position = ResolvePlayerPosition();
            if (roadGraphProvider != null)
            {
                roadGraphProvider.TryFindNearestRoad(position, out _lastResult);
            }
            else if (LwsApplicationBootstrap.Instance != null &&
                     LwsApplicationBootstrap.Instance.Registry != null &&
                     LwsApplicationBootstrap.Instance.Registry.TryGet(out ILwsRoadGraphService roadGraphService))
            {
                roadGraphService.TryFindNearestRoad(position, 80f, out _lastResult);
            }
        }

        private void OnGUI()
        {
            if (!visible)
            {
                return;
            }

            const float width = 390f;
            GUILayout.BeginArea(new Rect(12f, 480f, width, 190f), "LWS Road", GUI.skin.window);
            GUILayout.Label($"Road ID: {(_lastResult.Found ? _lastResult.RoadId : "none")}");
            GUILayout.Label($"Segment ID: {(_lastResult.Found ? _lastResult.SegmentId : "none")}");
            GUILayout.Label($"Class: {_lastResult.RoadClass}  Direction: {_lastResult.Direction}");
            GUILayout.Label($"Speed Limit: {_lastResult.SpeedLimitMph:0} MPH");
            GUILayout.Label($"Lanes: {_lastResult.LaneCount} x {_lastResult.LaneWidthMeters:0.0}m");
            GUILayout.Label($"Distance Along: {_lastResult.DistanceAlongSegmentMeters:0}m");
            GUILayout.Label($"Nearest Sample: {_lastResult.NearestPosition}");
            GUILayout.EndArea();
        }

        private void OnDrawGizmosSelected()
        {
            LwsRoadGraph graph = roadGraphProvider != null ? roadGraphProvider.Graph : null;
            if (graph == null || graph.edges == null)
            {
                return;
            }

            foreach (LwsRoadEdge edge in graph.edges)
            {
                if (edge == null || edge.samples == null || edge.samples.Count < 2)
                {
                    continue;
                }

                Gizmos.color = edge.direction == LwsRoadDirection.Southbound ? Color.yellow : Color.cyan;
                for (int i = 0; i < edge.samples.Count - 1; i++)
                {
                    if (edge.samples[i] != null && edge.samples[i + 1] != null)
                    {
                        Gizmos.DrawLine(edge.samples[i].position + Vector3.up * 0.25f, edge.samples[i + 1].position + Vector3.up * 0.25f);
                    }
                }
            }
        }

        private static Vector3 ResolvePlayerPosition()
        {
            LwsPlayerTruck playerTruck = FindFirstObjectByType<LwsPlayerTruck>();
            return playerTruck != null ? playerTruck.transform.position : Vector3.zero;
        }
    }
}
