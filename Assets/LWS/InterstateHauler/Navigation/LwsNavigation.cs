using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace LWS.InterstateHauler
{
    public interface ILwsNavigationRoutePresenter
    {
        string PresenterId { get; }
        bool PresentRoute(LwsRouteResult route);
        void ClearRoute();
    }

    public interface ILwsNavigationService : ILwsService
    {
        LwsRouteResult CurrentRoute { get; }
        LwsNavigationRuntimeState RuntimeState { get; }
        event Action<LwsRouteResult> RouteStarted;
        event Action<LwsRouteStep> StepChanged;
        event Action<LwsNavigationManeuverType> NavigationEvent;
        LwsRouteResult RequestRoute(LwsRouteRequest request, LwsRoadGraph graph);
        LwsRouteResult SetDestination(Vector3 destinationWorldPosition, Vector3 originWorldPosition, LwsRoadGraph graph);
        LwsRouteResult RecalculateRoute(Vector3 originWorldPosition);
        void UpdateVehiclePose(Vector3 playerPosition, Vector3 playerForward, float deltaTime);
        void SetPresenter(ILwsNavigationRoutePresenter presenter);
        bool PresentCurrentRoute();
        void ClearRoute();
    }

    public sealed class LwsNavigationService : ILwsNavigationService
    {
        private readonly LwsNavigationTuning _tuning = LwsNavigationTuning.Default();
        private readonly HashSet<int> _announcedSteps = new HashSet<int>();
        private LwsRoutePlanner _planner;
        private ILwsNavigationRoutePresenter _presenter;
        private ILwsRoadGraphService _roadGraphService;
        private ILwsGpsVoiceGuidanceService _voiceGuidanceService;
        private LwsRoadGraph _activeGraph;
        private LwsRouteRequest _lastRequest;
        private float _lastRerouteTime = -999f;
        private float _offRouteSeconds;
        private float _distanceAlongRouteMeters;
        private int _currentStepIndex;

        public string ServiceId => "lws.navigation";
        public LwsRouteResult CurrentRoute { get; private set; }
        public LwsNavigationRuntimeState RuntimeState { get; private set; } = new LwsNavigationRuntimeState();

        public event Action<LwsRouteResult> RouteStarted;
        public event Action<LwsRouteStep> StepChanged;
        public event Action<LwsNavigationManeuverType> NavigationEvent;

        public LwsServiceResult Initialize(LwsServiceContext context)
        {
            _planner = new LwsRoutePlanner(_tuning);
            context.Registry.TryGet(out _roadGraphService);
            context.Registry.TryGet(out _voiceGuidanceService);
            ClearRouteStateOnly();
            return LwsServiceResult.Success("LWS navigation service initialized.");
        }

        public LwsServiceResult Shutdown(LwsServiceContext context)
        {
            ClearRoute();
            _presenter = null;
            _roadGraphService = null;
            _voiceGuidanceService = null;
            return LwsServiceResult.Success("LWS navigation service shut down.");
        }

        public void SetPresenter(ILwsNavigationRoutePresenter presenter)
        {
            _presenter = presenter;
        }

        public LwsRouteResult SetDestination(Vector3 destinationWorldPosition, Vector3 originWorldPosition, LwsRoadGraph graph)
        {
            return RequestRoute(
                new LwsRouteRequest
                {
                    requestId = $"route.{DateTime.UtcNow:yyyyMMddHHmmss}",
                    destinationId = "validation.destination",
                    useOriginWorldPosition = true,
                    originWorldPosition = originWorldPosition,
                    useDestinationWorldPosition = true,
                    destinationWorldPosition = destinationWorldPosition,
                    truckRouteRequired = true
                },
                graph);
        }

        public LwsRouteResult RequestRoute(LwsRouteRequest request, LwsRoadGraph graph)
        {
            _activeGraph = graph ?? _roadGraphService?.ActiveGraph;
            _lastRequest = request;
            CurrentRoute = _planner.PlanRoute(request, _activeGraph);
            _announcedSteps.Clear();
            _currentStepIndex = 0;
            _distanceAlongRouteMeters = 0f;
            _offRouteSeconds = 0f;

            if (!CurrentRoute.succeeded)
            {
                RuntimeState.status = LwsNavigationRouteStatus.Failed;
                RuntimeState.routeActive = false;
                RuntimeState.nextInstructionText = CurrentRoute.message;
                return CurrentRoute;
            }

            RuntimeState.routeActive = true;
            RuntimeState.status = LwsNavigationRouteStatus.Active;
            RuntimeState.routeId = CurrentRoute.routeId;
            RuntimeState.destinationId = CurrentRoute.destinationId;
            RuntimeState.currentStepIndex = 0;
            RuntimeState.totalSteps = CurrentRoute.steps.Count;
            RuntimeState.distanceRemainingMeters = CurrentRoute.distanceMeters;
            ApplyStepToState(GetCurrentStep());
            PresentCurrentRoute();
            RouteStarted?.Invoke(CurrentRoute);
            NavigationEvent?.Invoke(LwsNavigationManeuverType.StartRoute);
            _voiceGuidanceService?.Announce(LwsNavigationManeuverType.StartRoute, -1, true);
            return CurrentRoute;
        }

        public LwsRouteResult RecalculateRoute(Vector3 originWorldPosition)
        {
            if (_lastRequest == null || _activeGraph == null)
            {
                return new LwsRouteResult { succeeded = false, message = "Cannot recalculate without an active route request and graph." };
            }

            var request = new LwsRouteRequest
            {
                requestId = $"{_lastRequest.requestId}.recalc",
                destinationId = _lastRequest.destinationId,
                destinationNodeId = _lastRequest.destinationNodeId,
                useDestinationWorldPosition = _lastRequest.useDestinationWorldPosition,
                destinationWorldPosition = _lastRequest.destinationWorldPosition,
                useOriginWorldPosition = true,
                originWorldPosition = originWorldPosition,
                truckRouteRequired = _lastRequest.truckRouteRequired
            };

            RuntimeState.recalculating = true;
            RuntimeState.status = LwsNavigationRouteStatus.Recalculating;
            NavigationEvent?.Invoke(LwsNavigationManeuverType.RouteRecalculating);
            _voiceGuidanceService?.Announce(LwsNavigationManeuverType.RouteRecalculating, -1, true);

            LwsRouteResult result = RequestRoute(request, _activeGraph);
            RuntimeState.recalculating = false;
            if (result.succeeded)
            {
                RuntimeState.status = LwsNavigationRouteStatus.Active;
                NavigationEvent?.Invoke(LwsNavigationManeuverType.RouteRecalculated);
                _voiceGuidanceService?.Announce(LwsNavigationManeuverType.RouteRecalculated, -1, true);
            }

            return result;
        }

        public void UpdateVehiclePose(Vector3 playerPosition, Vector3 playerForward, float deltaTime)
        {
            RuntimeState.playerPosition = playerPosition;
            RuntimeState.playerForward = playerForward.sqrMagnitude > 0.0001f ? playerForward.normalized : Vector3.forward;

            if (CurrentRoute == null || !CurrentRoute.succeeded || _activeGraph == null)
            {
                return;
            }

            if (!TryMatchRoute(playerPosition, playerForward, out RouteMatch match))
            {
                HandleOffRoute(playerPosition, deltaTime);
                return;
            }

            _offRouteSeconds = 0f;
            RuntimeState.offRoute = false;
            RuntimeState.status = RuntimeState.destinationReached ? LwsNavigationRouteStatus.Arrived : LwsNavigationRouteStatus.Active;
            RuntimeState.currentEdgeId = match.edgeId;
            RuntimeState.currentRoadId = match.roadId;
            RuntimeState.currentRoadDisplayName = match.roadDisplayName;
            RuntimeState.matchedPosition = match.nearestPosition;
            RuntimeState.distanceRemainingMeters = Mathf.Max(0f, CurrentRoute.distanceMeters - match.distanceAlongRouteMeters);
            RuntimeState.estimatedTimeRemainingSeconds = EstimateRemainingSeconds(match.edgeId, RuntimeState.distanceRemainingMeters);
            _distanceAlongRouteMeters = match.distanceAlongRouteMeters;
            AdvanceStepIfNeeded();
            MaybeAnnounceCurrentStep();
            CheckArrival();
        }

        public bool PresentCurrentRoute()
        {
            return CurrentRoute != null && _presenter != null && _presenter.PresentRoute(CurrentRoute);
        }

        public void ClearRoute()
        {
            ClearRouteStateOnly();
            _presenter?.ClearRoute();
        }

        private void ClearRouteStateOnly()
        {
            CurrentRoute = null;
            _lastRequest = null;
            _announcedSteps.Clear();
            _distanceAlongRouteMeters = 0f;
            _currentStepIndex = 0;
            RuntimeState = new LwsNavigationRuntimeState { status = LwsNavigationRouteStatus.Inactive };
        }

        private void HandleOffRoute(Vector3 playerPosition, float deltaTime)
        {
            _offRouteSeconds += Mathf.Max(0f, deltaTime);
            RuntimeState.offRoute = true;
            RuntimeState.status = LwsNavigationRouteStatus.OffRoute;
            RuntimeState.nextManeuver = LwsNavigationManeuverType.OffRoute;
            RuntimeState.nextInstructionText = "Off route";
            NavigationEvent?.Invoke(LwsNavigationManeuverType.OffRoute);

            if (_offRouteSeconds < 1f || Time.time - _lastRerouteTime < _tuning.rerouteCooldownSeconds)
            {
                return;
            }

            _lastRerouteTime = Time.time;
            RecalculateRoute(playerPosition);
        }

        private void AdvanceStepIfNeeded()
        {
            if (CurrentRoute.steps == null || CurrentRoute.steps.Count == 0)
            {
                return;
            }

            int nextIndex = Mathf.Clamp(_currentStepIndex, 0, CurrentRoute.steps.Count - 1);
            while (nextIndex < CurrentRoute.steps.Count - 1 &&
                   _distanceAlongRouteMeters >= CurrentRoute.steps[nextIndex + 1].distanceFromRouteStartMeters - _tuning.maneuverAdvanceDistanceMeters)
            {
                nextIndex++;
            }

            if (nextIndex == _currentStepIndex)
            {
                ApplyStepToState(GetCurrentStep());
                return;
            }

            _currentStepIndex = nextIndex;
            LwsRouteStep step = GetCurrentStep();
            ApplyStepToState(step);
            StepChanged?.Invoke(step);
        }

        private void MaybeAnnounceCurrentStep()
        {
            LwsRouteStep step = GetCurrentStep();
            if (step == null || _announcedSteps.Contains(step.stepIndex))
            {
                return;
            }

            float threshold = IsHighway(step) ? _tuning.highwayVoiceApproachMeters : _tuning.localVoiceApproachMeters;
            float distanceToStep = Mathf.Max(0f, step.distanceFromRouteStartMeters - _distanceAlongRouteMeters);
            if (step.stepIndex == 0 || distanceToStep <= threshold)
            {
                _announcedSteps.Add(step.stepIndex);
                _voiceGuidanceService?.Announce(step.maneuver, step.stepIndex, false);
            }
        }

        private void CheckArrival()
        {
            if (RuntimeState.destinationReached || RuntimeState.distanceRemainingMeters > _tuning.arrivalDistanceMeters)
            {
                return;
            }

            RuntimeState.destinationReached = true;
            RuntimeState.status = LwsNavigationRouteStatus.Arrived;
            RuntimeState.nextManeuver = LwsNavigationManeuverType.Arrive;
            RuntimeState.nextInstructionText = "Arrive at destination";
            NavigationEvent?.Invoke(LwsNavigationManeuverType.Arrive);
            _voiceGuidanceService?.Announce(LwsNavigationManeuverType.Arrive, CurrentRoute.steps.Count - 1, true);
        }

        private void ApplyStepToState(LwsRouteStep step)
        {
            if (step == null)
            {
                return;
            }

            RuntimeState.currentStepIndex = step.stepIndex;
            RuntimeState.totalSteps = CurrentRoute != null && CurrentRoute.steps != null ? CurrentRoute.steps.Count : 0;
            RuntimeState.nextManeuver = step.maneuver;
            RuntimeState.nextInstructionText = step.instructionText;
            RuntimeState.nextRoadId = step.roadId;
            RuntimeState.nextRoadDisplayName = step.roadDisplayName;
            RuntimeState.distanceToNextManeuverMeters = Mathf.Max(0f, step.distanceFromRouteStartMeters - _distanceAlongRouteMeters);
        }

        private LwsRouteStep GetCurrentStep()
        {
            if (CurrentRoute == null || CurrentRoute.steps == null || CurrentRoute.steps.Count == 0)
            {
                return null;
            }

            return CurrentRoute.steps[Mathf.Clamp(_currentStepIndex, 0, CurrentRoute.steps.Count - 1)];
        }

        private bool TryMatchRoute(Vector3 playerPosition, Vector3 playerForward, out RouteMatch match)
        {
            match = default;
            if (CurrentRoute.edgeIds == null || CurrentRoute.edgeIds.Count == 0)
            {
                return false;
            }

            float bestScore = float.PositiveInfinity;
            float routeDistanceBefore = 0f;
            for (int routeEdgeIndex = 0; routeEdgeIndex < CurrentRoute.edgeIds.Count; routeEdgeIndex++)
            {
                LwsRoadEdge edge = LwsRoutePlanner.FindEdge(_activeGraph, CurrentRoute.edgeIds[routeEdgeIndex]);
                if (edge == null)
                {
                    continue;
                }

                if (TryMatchEdge(edge, playerPosition, playerForward, out LwsRoadLookupResult lookup))
                {
                    float headingPenalty = Mathf.Clamp01(1f - Mathf.Abs(Vector3.Dot(playerForward.normalized, lookup.Forward.normalized))) * 15f;
                    float score = lookup.LateralDistanceMeters + headingPenalty;
                    if (score < bestScore)
                    {
                        bestScore = score;
                        match = new RouteMatch(
                            true,
                            edge.edgeId,
                            edge.roadId,
                            LwsRoadDisplayNames.GetRoadDisplayName(edge.roadId, edge.segmentId),
                            lookup.NearestPosition,
                            routeDistanceBefore + lookup.DistanceAlongSegmentMeters,
                            lookup.LateralDistanceMeters);
                    }
                }

                routeDistanceBefore += Mathf.Max(1f, edge.distanceMeters);
            }

            return match.found && match.lateralDistanceMeters <= _tuning.offRouteToleranceMeters;
        }

        private static bool TryMatchEdge(LwsRoadEdge edge, Vector3 playerPosition, Vector3 playerForward, out LwsRoadLookupResult lookup)
        {
            if (edge == null || edge.samples == null || edge.samples.Count == 0)
            {
                lookup = default;
                return false;
            }

            var graph = new LwsRoadGraph { graphId = "single-edge-route-match" };
            graph.nodes.Add(new LwsRoadNode { nodeId = edge.fromNodeId, position = edge.samples[0].position });
            graph.nodes.Add(new LwsRoadNode { nodeId = edge.toNodeId, position = edge.samples[edge.samples.Count - 1].position });
            graph.edges.Add(edge);
            return LwsRoadGraphQuery.TryFindNearestRoad(graph, playerPosition, 1000f, out lookup);
        }

        private float EstimateRemainingSeconds(string edgeId, float distanceRemainingMeters)
        {
            LwsRoadEdge edge = LwsRoutePlanner.FindEdge(_activeGraph, edgeId);
            float mph = edge != null && edge.speedLimitMph > 1f ? edge.speedLimitMph : 55f;
            float metersPerSecond = Mathf.Max(1f, mph * 0.44704f);
            return distanceRemainingMeters / metersPerSecond;
        }

        private static bool IsHighway(LwsRouteStep step)
        {
            return step != null &&
                   (string.Equals(step.roadId, "IH_TEST_I000_NB", StringComparison.Ordinal) ||
                    string.Equals(step.roadId, "IH_TEST_I000_SB", StringComparison.Ordinal));
        }

        private readonly struct RouteMatch
        {
            public RouteMatch(bool found, string edgeId, string roadId, string roadDisplayName, Vector3 nearestPosition, float distanceAlongRouteMeters, float lateralDistanceMeters)
            {
                this.found = found;
                this.edgeId = edgeId;
                this.roadId = roadId;
                this.roadDisplayName = roadDisplayName;
                this.nearestPosition = nearestPosition;
                this.distanceAlongRouteMeters = distanceAlongRouteMeters;
                this.lateralDistanceMeters = lateralDistanceMeters;
            }

            public readonly bool found;
            public readonly string edgeId;
            public readonly string roadId;
            public readonly string roadDisplayName;
            public readonly Vector3 nearestPosition;
            public readonly float distanceAlongRouteMeters;
            public readonly float lateralDistanceMeters;
        }
    }

    [DisallowMultipleComponent]
    public sealed class LwsCompassRoutePresenter : MonoBehaviour, ILwsNavigationRoutePresenter
    {
        [SerializeField] private MonoBehaviour compassBehaviour;
        [SerializeField] private string presenterId = "compass.navigator";

        public string PresenterId => presenterId;

        public bool PresentRoute(LwsRouteResult route)
        {
            if (route == null || route.waypoints == null || route.waypoints.Count == 0)
            {
                return false;
            }

            object compass = compassBehaviour != null ? compassBehaviour : ResolveCompassInstance();
            if (compass == null)
            {
                return false;
            }

            MethodInfo setRoute = compass.GetType().GetMethod("SetRoute", BindingFlags.Instance | BindingFlags.Public);
            if (setRoute == null)
            {
                return false;
            }

            setRoute.Invoke(compass, new object[] { route.waypoints });
            return true;
        }

        public void ClearRoute()
        {
            object compass = compassBehaviour != null ? compassBehaviour : ResolveCompassInstance();
            MethodInfo clearRoute = compass?.GetType().GetMethod("ClearRoute", BindingFlags.Instance | BindingFlags.Public);
            clearRoute?.Invoke(compass, null);
        }

        private static object ResolveCompassInstance()
        {
            Type compassType = Type.GetType("CompassNavigatorPro.CompassPro, Assembly-CSharp-firstpass")
                ?? Type.GetType("CompassNavigatorPro.CompassPro, Assembly-CSharp");
            PropertyInfo instanceProperty = compassType?.GetProperty("instance", BindingFlags.Static | BindingFlags.Public);
            return instanceProperty?.GetValue(null);
        }
    }
}
