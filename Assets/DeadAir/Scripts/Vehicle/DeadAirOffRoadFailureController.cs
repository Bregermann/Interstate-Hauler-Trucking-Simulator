using System.Collections.Generic;
using UnityEngine;

namespace DeadAir
{
    [DefaultExecutionOrder(-60)]
    [DisallowMultipleComponent]
    public sealed class DeadAirOffRoadFailureController : MonoBehaviour
    {
        [SerializeField] private DeadAirVehicleAdapter vehicleAdapter;
        [SerializeField] private DeadAirStartRigController startRigController;
        [SerializeField, InspectorName("Enable Off-Road Void Failure")]
        private bool enableOffRoadVoidFailure;
        [SerializeField] private float graceSeconds = 1.25f;
        [SerializeField] private bool monitorOnStart = true;
        [SerializeField] private bool showRuntimeDebug = true;
        [SerializeField] private Transform tractorFrontReference;
        [SerializeField] private Transform tractorRearReference;
        [SerializeField] private Transform trailerFrontReference;
        [SerializeField] private Transform trailerRearReference;
        [SerializeField] private float tractorHalfLengthMeters = 3.75f;
        [SerializeField] private float trailerHalfLengthMeters = 6.75f;
        [SerializeField] private float referencePointHeightMeters = 0.8f;

        private float _graceTimer;
        private bool _failureTriggered;
        private bool _monitoring;
        private DeadAirRoadBoundaryEvaluation _lastEvaluation;

        public float GraceSeconds => Mathf.Max(0.05f, graceSeconds);
        public bool EnableOffRoadVoidFailure => enableOffRoadVoidFailure;
        public bool Monitoring => enableOffRoadVoidFailure && _monitoring;
        public bool FailureTriggered => _failureTriggered;
        public bool ShowRuntimeDebug => showRuntimeDebug;
        public DeadAirRoadBoundaryEvaluation LastEvaluation => _lastEvaluation;

        private void Awake()
        {
            ResolveReferences();
            ResetBoundaryState();
        }

        private void Update()
        {
            if (!enableOffRoadVoidFailure || !_monitoring || _failureTriggered)
            {
                return;
            }

            DeadAirGameManager manager = DeadAirGameManager.Instance;
            if (manager != null && manager.State != DeadAirGameState.Playing)
            {
                return;
            }

            ResolveReferences();
            if (vehicleAdapter == null)
            {
                return;
            }

            DeadAirRoadBoundaryEvaluation evaluation = EvaluateCurrentRig();
            _graceTimer = UpdateGraceTimer(evaluation.entireRigOffRoad, _graceTimer, Time.deltaTime, GraceSeconds, out bool triggered);
            evaluation.graceTimerSeconds = _graceTimer;
            evaluation.graceDurationSeconds = GraceSeconds;
            _lastEvaluation = evaluation;

            if (triggered)
            {
                TriggerVoidFailure();
            }
        }

        public void ResetBoundaryState()
        {
            _graceTimer = 0f;
            _failureTriggered = false;
            _monitoring = enableOffRoadVoidFailure && monitorOnStart;
            _lastEvaluation = default;
            vehicleAdapter?.SetDeadAirDrivingInputLocked(false);
        }

        public void SetOffRoadVoidFailureEnabled(bool enabled)
        {
            enableOffRoadVoidFailure = enabled;
            ResetBoundaryState();
        }

        public void SetMonitoring(bool monitoring)
        {
            _monitoring = enableOffRoadVoidFailure && monitoring;
            if (!enableOffRoadVoidFailure || !monitoring)
            {
                _graceTimer = 0f;
            }
        }

        public DeadAirRoadBoundaryEvaluation EvaluateCurrentRig()
        {
            ResolveReferences();
            Vector3 tractorFront = ResolvePoint(tractorFrontReference, vehicleAdapter != null ? vehicleAdapter.transform : null, tractorHalfLengthMeters);
            Vector3 tractorRear = ResolvePoint(tractorRearReference, vehicleAdapter != null ? vehicleAdapter.transform : null, -tractorHalfLengthMeters);
            GameObject trailer = startRigController != null ? startRigController.DeliveryTrailer : null;
            Transform trailerTransform = trailer != null ? trailer.transform : null;
            Vector3 trailerFront = ResolvePoint(trailerFrontReference, trailerTransform, trailerHalfLengthMeters);
            Vector3 trailerRear = ResolvePoint(trailerRearReference, trailerTransform, -trailerHalfLengthMeters);

            return EvaluateRigRoadState(
                tractorFront,
                tractorRear,
                trailerTransform != null || trailerFrontReference != null,
                trailerFront,
                trailerRear,
                DeadAirValidRoadZone.ActiveZones);
        }

        public static DeadAirRoadBoundaryEvaluation EvaluateRigRoadState(
            Vector3 tractorFront,
            Vector3 tractorRear,
            bool includeTrailer,
            Vector3 trailerFront,
            Vector3 trailerRear,
            IReadOnlyList<DeadAirValidRoadZone> zones)
        {
            bool tractorFrontValid = IsPointInAnyZone(tractorFront, zones);
            bool tractorRearValid = IsPointInAnyZone(tractorRear, zones);
            bool trailerFrontValid = includeTrailer && IsPointInAnyZone(trailerFront, zones);
            bool trailerRearValid = includeTrailer && IsPointInAnyZone(trailerRear, zones);
            bool tractorValid = tractorFrontValid || tractorRearValid;
            bool trailerValid = includeTrailer && (trailerFrontValid || trailerRearValid);
            bool anyValid = tractorValid || trailerValid;
            int zoneCount = CountRuntimeZones(zones);

            return new DeadAirRoadBoundaryEvaluation
            {
                tractorFrontValid = tractorFrontValid,
                tractorRearValid = tractorRearValid,
                trailerFrontValid = trailerFrontValid,
                trailerRearValid = trailerRearValid,
                tractorValid = tractorValid,
                trailerValid = !includeTrailer || trailerValid,
                anyRigPointValid = anyValid,
                entireRigOffRoad = zoneCount > 0 && !anyValid,
                validZoneCount = zoneCount
            };
        }

        public static float UpdateGraceTimer(bool entireRigOffRoad, float currentTimer, float deltaSeconds, float graceDurationSeconds, out bool triggered)
        {
            triggered = false;
            if (!entireRigOffRoad)
            {
                return 0f;
            }

            float next = Mathf.Max(0f, currentTimer) + Mathf.Max(0f, deltaSeconds);
            triggered = next >= Mathf.Max(0.01f, graceDurationSeconds);
            return next;
        }

        private void TriggerVoidFailure()
        {
            if (!enableOffRoadVoidFailure)
            {
                ResetBoundaryState();
                return;
            }

            _failureTriggered = true;
            _monitoring = false;
            vehicleAdapter?.SetDeadAirDrivingInputLocked(true);
            DeadAirGameManager.Instance?.RequestVoidFailure();
        }

        private static bool IsPointInAnyZone(Vector3 point, IReadOnlyList<DeadAirValidRoadZone> zones)
        {
            if (zones == null)
            {
                return false;
            }

            for (int i = 0; i < zones.Count; i++)
            {
                DeadAirValidRoadZone zone = zones[i];
                if (zone != null && zone.isActiveAndEnabled && zone.RuntimeCandidate && zone.ContainsWorldPoint(point))
                {
                    return true;
                }
            }

            return false;
        }

        private static int CountRuntimeZones(IReadOnlyList<DeadAirValidRoadZone> zones)
        {
            if (zones == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < zones.Count; i++)
            {
                DeadAirValidRoadZone zone = zones[i];
                if (zone != null && zone.isActiveAndEnabled && zone.RuntimeCandidate)
                {
                    count++;
                }
            }

            return count;
        }

        private Vector3 ResolvePoint(Transform explicitReference, Transform fallbackRoot, float forwardOffset)
        {
            if (explicitReference != null)
            {
                return explicitReference.position;
            }

            if (fallbackRoot == null)
            {
                return Vector3.zero;
            }

            return fallbackRoot.position +
                   fallbackRoot.forward * forwardOffset +
                   Vector3.up * referencePointHeightMeters;
        }

        private void ResolveReferences()
        {
            if (vehicleAdapter == null)
            {
                vehicleAdapter = DeadAirGameManager.Instance != null
                    ? DeadAirGameManager.Instance.VehicleAdapter
                    : FindFirstObjectByType<DeadAirVehicleAdapter>();
            }

            if (startRigController == null)
            {
                startRigController = DeadAirGameManager.Instance != null
                    ? DeadAirGameManager.Instance.StartRigController
                    : FindFirstObjectByType<DeadAirStartRigController>();
            }
        }
    }
}
