using UnityEngine;

namespace LWS.InterstateHauler
{
    [DisallowMultipleComponent]
    public sealed class LwsRoadGraphProvider : MonoBehaviour
    {
        [SerializeField] private bool registerOnStart = true;
        [SerializeField] private float lookupMaxDistanceMeters = 80f;
        [SerializeField] private LwsRoadGraph graph = LwsRoadGraph.CreateEmpty("unassigned-road-graph");

        public LwsRoadGraph Graph => graph;
        public float LookupMaxDistanceMeters => lookupMaxDistanceMeters;

        private void Start()
        {
            if (registerOnStart)
            {
                RegisterGraph();
            }
        }

        public void SetGraph(LwsRoadGraph value, bool registerImmediately)
        {
            graph = value ?? LwsRoadGraph.CreateEmpty("null-road-graph");
            if (registerImmediately)
            {
                RegisterGraph();
            }
        }

        public LwsRoadGraphValidationResult ValidateGraph()
        {
            return graph != null
                ? graph.Validate()
                : new LwsRoadGraphValidationResult(false, new[] { "Road graph provider has no graph." });
        }

        public bool TryFindNearestRoad(Vector3 worldPosition, out LwsRoadLookupResult result)
        {
            return LwsRoadGraphQuery.TryFindNearestRoad(graph, worldPosition, lookupMaxDistanceMeters, out result);
        }

        public LwsRoadGraphValidationResult RegisterGraph()
        {
            LwsRoadGraphValidationResult validation = ValidateGraph();
            if (!validation.IsValid)
            {
                Debug.LogWarning($"Road graph provider validation failed: {validation.Summary}", this);
                return validation;
            }

            LwsApplicationBootstrap bootstrap = LwsApplicationBootstrap.Instance;
            if (bootstrap != null &&
                bootstrap.Registry != null &&
                bootstrap.Registry.TryGet(out ILwsRoadGraphService roadGraphService))
            {
                return roadGraphService.SetActiveGraph(graph);
            }

            return validation;
        }
    }
}
