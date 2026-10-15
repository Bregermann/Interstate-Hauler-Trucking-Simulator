using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.InterstateHauler
{
    [DefaultExecutionOrder(140)]
    [DisallowMultipleComponent]
    public sealed class LwsDeliveryArea : MonoBehaviour
    {
        [Header("Destination")]
        [SerializeField] private LwsDestinationDefinition destinationDefinition;
        [SerializeField] private Collider arrivalTrigger;
        [SerializeField] private LwsDeliveryParkingZone parkingZone;

        private readonly HashSet<Collider> _playerTractorColliders = new HashSet<Collider>();
        private ILwsDeliveryService _deliveryService;
        private ILwsPlayerVehicleService _playerVehicleService;
        private LwsPlayerTruck _insideTruck;
        private bool _registered;

        public LwsDestinationDefinition DestinationDefinition => destinationDefinition;
        public string DestinationId => destinationDefinition != null ? destinationDefinition.StableDestinationId : string.Empty;
        public string DestinationDisplayName => destinationDefinition != null ? destinationDefinition.DisplayName : DestinationId;
        public Collider ArrivalTrigger => arrivalTrigger;
        public LwsDeliveryParkingZone ParkingZone => parkingZone;
        public bool PlayerTractorInside => _playerTractorColliders.Count > 0;

        public void Configure(
            LwsDestinationDefinition definition,
            Collider trigger,
            LwsDeliveryParkingZone targetZone)
        {
            destinationDefinition = definition;
            arrivalTrigger = trigger;
            parkingZone = targetZone;
            EnsureTrigger();
            if (isActiveAndEnabled)
            {
                RegisterIfPossible();
            }
        }

        public bool Validate(out string message)
        {
            if (destinationDefinition == null || !destinationDefinition.Enabled)
            {
                message = $"{name} has no enabled destination definition.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(destinationDefinition.StableDestinationId))
            {
                message = $"{name} has no stable destination ID.";
                return false;
            }

            if (parkingZone == null)
            {
                message = $"{name} has no delivery parking zone.";
                return false;
            }

            message = $"{DestinationId} delivery area is valid.";
            return true;
        }

        private void Awake()
        {
            EnsureTrigger();
        }

        private void OnEnable()
        {
            RegisterIfPossible();
        }

        private void Start()
        {
            RegisterIfPossible();
        }

        private void OnDisable()
        {
            if (_insideTruck != null)
            {
                _deliveryService?.NotifyPlayerExitedDeliveryArea(this, _insideTruck);
            }

            _playerTractorColliders.Clear();
            _insideTruck = null;
            if (_registered)
            {
                _deliveryService?.UnregisterDeliveryArea(this);
            }

            _registered = false;
            _deliveryService = null;
            _playerVehicleService = null;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!TryResolveCanonicalPlayerTruck(other, out LwsPlayerTruck truck))
            {
                return;
            }

            bool firstCollider = _playerTractorColliders.Count == 0;
            _playerTractorColliders.Add(other);
            _insideTruck = truck;
            if (firstCollider)
            {
                ResolveServices();
                _deliveryService?.TryEnterDeliveryContext(
                    this,
                    truck,
                    $"Canonical player tractor entered delivery area {DestinationId}.");
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!_playerTractorColliders.Remove(other) || _playerTractorColliders.Count > 0)
            {
                return;
            }

            LwsPlayerTruck truck = _insideTruck;
            _insideTruck = null;
            _deliveryService?.NotifyPlayerExitedDeliveryArea(this, truck);
        }

        private void EnsureTrigger()
        {
            if (arrivalTrigger == null)
            {
                arrivalTrigger = GetComponent<Collider>();
            }

            if (arrivalTrigger == null)
            {
                arrivalTrigger = gameObject.AddComponent<BoxCollider>();
            }

            arrivalTrigger.isTrigger = true;
        }

        private void RegisterIfPossible()
        {
            if (_registered || !Validate(out _))
            {
                return;
            }

            ResolveServices();
            if (_deliveryService == null)
            {
                return;
            }

            LwsServiceResult result = _deliveryService.RegisterDeliveryArea(this);
            if (!result.Succeeded)
            {
                Debug.LogWarning(result.Message, this);
                return;
            }

            _registered = true;
        }

        private bool TryResolveCanonicalPlayerTruck(Collider candidate, out LwsPlayerTruck truck)
        {
            truck = candidate != null ? candidate.GetComponentInParent<LwsPlayerTruck>() : null;
            if (truck == null)
            {
                return false;
            }

            ResolveServices();
            return _playerVehicleService == null ||
                   _playerVehicleService.ActiveTruck == null ||
                   _playerVehicleService.ActiveTruck == truck;
        }

        private void ResolveServices()
        {
            if (LwsApplicationBootstrap.Instance == null || LwsApplicationBootstrap.Instance.Registry == null)
            {
                return;
            }

            if (_deliveryService == null)
            {
                LwsApplicationBootstrap.Instance.Registry.TryGet(out _deliveryService);
            }

            if (_playerVehicleService == null)
            {
                LwsApplicationBootstrap.Instance.Registry.TryGet(out _playerVehicleService);
            }
        }
    }

    [DefaultExecutionOrder(145)]
    [DisallowMultipleComponent]
    public sealed class LwsDeliveryParkingZone : MonoBehaviour
    {
        [Header("Parking Bounds")]
        [Tooltip("Local-space width, height, and length of the accepted trailer parking volume.")]
        [SerializeField] private Vector3 zoneDimensions = new Vector3(5.5f, 5.5f, 18f);
        [Tooltip("Extra meters allowed around the parking volume to absorb small collider inaccuracies.")]
        [SerializeField] private float containmentToleranceMeters = 0.35f;
        [Range(0.1f, 1f)]
        [Tooltip("Fraction of representative trailer collider sample points that must be inside the volume.")]
        [SerializeField] private float minimumContainmentRatio = 0.8f;

        [Header("Heading")]
        [Tooltip("Maximum horizontal angle between the trailer and this target's forward direction.")]
        [SerializeField] private float allowedHeadingErrorDegrees = 12f;
        [Tooltip("Accept a trailer facing 180 degrees opposite the target direction.")]
        [SerializeField] private bool allowOppositeHeading;

        [Header("Settling")]
        [Tooltip("Maximum trailer speed while the valid-delivery timer advances.")]
        [SerializeField] private float maximumLinearSpeedMetersPerSecond = 0.35f;
        [Tooltip("Maximum trailer angular speed while the valid-delivery timer advances.")]
        [SerializeField] private float maximumAngularSpeedDegreesPerSecond = 5f;
        [Tooltip("Continuous seconds all delivery conditions must remain valid.")]
        [SerializeField] private float minimumValidHoldSeconds = 2f;

        [Header("Development Visualization")]
        [SerializeField] private bool showRuntimeVisualization;
        [SerializeField] private float lineWidthMeters = 0.2f;
        [SerializeField] private Color waitingColor = new Color(0.16f, 0.72f, 1f, 0.9f);
        [SerializeField] private Color readyColor = new Color(0.15f, 1f, 0.35f, 0.95f);

        private readonly List<Vector3> _zoneLocalSamplePoints = new List<Vector3>(64);
        private LwsTrailerGameplayIdentity _cachedTrailer;
        private Collider[] _cachedTrailerColliders = Array.Empty<Collider>();
        private Rigidbody _cachedTrailerRigidbody;
        private ILwsDeliveryService _deliveryService;
        private LineRenderer _outlineRenderer;
        private LineRenderer _headingRenderer;
        private Material _runtimeLineMaterial;

        public Vector3 ZoneDimensions => zoneDimensions;
        public float ContainmentToleranceMeters => containmentToleranceMeters;
        public float MinimumContainmentRatio => minimumContainmentRatio;
        public float AllowedHeadingErrorDegrees => allowedHeadingErrorDegrees;
        public bool AllowOppositeHeading => allowOppositeHeading;
        public float MaximumLinearSpeedMetersPerSecond => maximumLinearSpeedMetersPerSecond;
        public float MaximumAngularSpeedDegreesPerSecond => maximumAngularSpeedDegreesPerSecond;
        public float MinimumValidHoldSeconds => minimumValidHoldSeconds;
        public bool ShowRuntimeVisualization => showRuntimeVisualization;

        public void Configure(
            Vector3 dimensions,
            float containmentTolerance,
            float requiredContainment,
            float headingToleranceDegrees,
            bool acceptOppositeHeading,
            float maximumLinearSpeed,
            float maximumAngularSpeed,
            float holdSeconds,
            bool showVisualization)
        {
            zoneDimensions = dimensions;
            containmentToleranceMeters = containmentTolerance;
            minimumContainmentRatio = requiredContainment;
            allowedHeadingErrorDegrees = headingToleranceDegrees;
            allowOppositeHeading = acceptOppositeHeading;
            maximumLinearSpeedMetersPerSecond = maximumLinearSpeed;
            maximumAngularSpeedDegreesPerSecond = maximumAngularSpeed;
            minimumValidHoldSeconds = holdSeconds;
            showRuntimeVisualization = showVisualization;
            ClampConfiguration();
            EnsureRuntimeVisualization();
            SetRuntimeVisualState(false, false);
        }

        public LwsDeliveryParkingEvaluation EvaluateTrailer(LwsTrailerGameplayIdentity trailer)
        {
            if (trailer == null)
            {
                return default;
            }

            RefreshTrailerCache(trailer);
            _zoneLocalSamplePoints.Clear();
            for (int i = 0; i < _cachedTrailerColliders.Length; i++)
            {
                Collider collider = _cachedTrailerColliders[i];
                if (collider == null || !collider.enabled || collider.isTrigger || !collider.gameObject.activeInHierarchy)
                {
                    continue;
                }

                AppendColliderSamplePoints(collider, _zoneLocalSamplePoints);
            }

            if (_zoneLocalSamplePoints.Count == 0)
            {
                _zoneLocalSamplePoints.Add(transform.InverseTransformPoint(trailer.transform.position));
            }

            float containment = LwsDeliveryValidation.CalculateContainmentRatio(
                _zoneLocalSamplePoints,
                zoneDimensions,
                containmentToleranceMeters);
            float headingError = LwsDeliveryValidation.CalculateHorizontalHeadingError(
                trailer.transform.forward,
                transform.forward,
                allowOppositeHeading);
            float linearSpeed = _cachedTrailerRigidbody != null ? _cachedTrailerRigidbody.linearVelocity.magnitude : 0f;
            float angularSpeed = _cachedTrailerRigidbody != null
                ? _cachedTrailerRigidbody.angularVelocity.magnitude * Mathf.Rad2Deg
                : 0f;

            Vector3 average = Vector3.zero;
            for (int i = 0; i < _zoneLocalSamplePoints.Count; i++)
            {
                average += _zoneLocalSamplePoints[i];
            }

            average /= Mathf.Max(1, _zoneLocalSamplePoints.Count);
            return new LwsDeliveryParkingEvaluation
            {
                samplePointCount = _zoneLocalSamplePoints.Count,
                containmentRatio = containment,
                trailerInsideZone = containment >= minimumContainmentRatio,
                headingErrorDegrees = headingError,
                headingValid = LwsDeliveryValidation.IsHeadingValid(headingError, allowedHeadingErrorDegrees),
                trailerLinearSpeedMetersPerSecond = linearSpeed,
                trailerAngularSpeedDegreesPerSecond = angularSpeed,
                stopped = LwsDeliveryValidation.IsStopped(
                    linearSpeed,
                    angularSpeed,
                    maximumLinearSpeedMetersPerSecond,
                    maximumAngularSpeedDegreesPerSecond),
                lateralOffsetMeters = average.x,
                longitudinalOffsetMeters = average.z
            };
        }

        public void SetRuntimeVisualState(bool active, bool ready)
        {
            EnsureRuntimeVisualization();
            bool visible = showRuntimeVisualization && active;
            if (_outlineRenderer != null)
            {
                _outlineRenderer.enabled = visible;
            }

            if (_headingRenderer != null)
            {
                _headingRenderer.enabled = visible;
            }

            Color color = ready ? readyColor : waitingColor;
            ApplyLineColor(_outlineRenderer, color);
            ApplyLineColor(_headingRenderer, color);
        }

        private void FixedUpdate()
        {
            ResolveService();
            _deliveryService?.EvaluateParking(this, Time.fixedDeltaTime);
        }

        private void OnEnable()
        {
            EnsureRuntimeVisualization();
        }

        private void OnDisable()
        {
            if (_outlineRenderer != null)
            {
                _outlineRenderer.enabled = false;
            }

            if (_headingRenderer != null)
            {
                _headingRenderer.enabled = false;
            }
        }

        private void OnDestroy()
        {
            if (_runtimeLineMaterial != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_runtimeLineMaterial);
                }
                else
                {
                    DestroyImmediate(_runtimeLineMaterial);
                }
            }
        }

        private void OnValidate()
        {
            ClampConfiguration();
        }

        private void OnDrawGizmosSelected()
        {
            Matrix4x4 previous = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = waitingColor;
            Gizmos.DrawWireCube(Vector3.zero, zoneDimensions);
            Gizmos.DrawLine(Vector3.zero, Vector3.forward * zoneDimensions.z * 0.45f);
            Gizmos.matrix = previous;
        }

        private void ClampConfiguration()
        {
            zoneDimensions.x = Mathf.Max(0.5f, zoneDimensions.x);
            zoneDimensions.y = Mathf.Max(0.5f, zoneDimensions.y);
            zoneDimensions.z = Mathf.Max(0.5f, zoneDimensions.z);
            containmentToleranceMeters = Mathf.Max(0f, containmentToleranceMeters);
            minimumContainmentRatio = Mathf.Clamp01(minimumContainmentRatio);
            allowedHeadingErrorDegrees = Mathf.Clamp(allowedHeadingErrorDegrees, 0f, 180f);
            maximumLinearSpeedMetersPerSecond = Mathf.Max(0f, maximumLinearSpeedMetersPerSecond);
            maximumAngularSpeedDegreesPerSecond = Mathf.Max(0f, maximumAngularSpeedDegreesPerSecond);
            minimumValidHoldSeconds = Mathf.Max(0.1f, minimumValidHoldSeconds);
            lineWidthMeters = Mathf.Max(0.02f, lineWidthMeters);
        }

        private void RefreshTrailerCache(LwsTrailerGameplayIdentity trailer)
        {
            if (_cachedTrailer == trailer)
            {
                return;
            }

            _cachedTrailer = trailer;
            _cachedTrailerColliders = trailer.GetComponentsInChildren<Collider>(true);
            _cachedTrailerRigidbody = trailer.GetComponent<Rigidbody>() ?? trailer.GetComponentInChildren<Rigidbody>(true);
        }

        private void AppendColliderSamplePoints(Collider collider, List<Vector3> destination)
        {
            if (collider is BoxCollider box)
            {
                Vector3 half = box.size * 0.5f;
                for (int x = -1; x <= 1; x += 2)
                {
                    for (int y = -1; y <= 1; y += 2)
                    {
                        for (int z = -1; z <= 1; z += 2)
                        {
                            Vector3 local = box.center + new Vector3(half.x * x, half.y * y, half.z * z);
                            destination.Add(transform.InverseTransformPoint(box.transform.TransformPoint(local)));
                        }
                    }
                }

                destination.Add(transform.InverseTransformPoint(box.transform.TransformPoint(box.center)));
                return;
            }

            Bounds bounds = collider.bounds;
            Vector3 center = bounds.center;
            Vector3 extents = bounds.extents;
            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        destination.Add(transform.InverseTransformPoint(center + Vector3.Scale(extents, new Vector3(x, y, z))));
                    }
                }
            }

            destination.Add(transform.InverseTransformPoint(center));
        }

        private void EnsureRuntimeVisualization()
        {
            if (!Application.isPlaying || !showRuntimeVisualization)
            {
                return;
            }

            if (_runtimeLineMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader != null)
                {
                    _runtimeLineMaterial = new Material(shader)
                    {
                        name = "IH Delivery Parking Target Runtime Material",
                        hideFlags = HideFlags.DontSave
                    };
                }
            }

            if (_outlineRenderer == null)
            {
                _outlineRenderer = GetComponent<LineRenderer>() ?? gameObject.AddComponent<LineRenderer>();
                ConfigureLineRenderer(_outlineRenderer, 5);
                float halfX = zoneDimensions.x * 0.5f;
                float halfZ = zoneDimensions.z * 0.5f;
                float floorY = -zoneDimensions.y * 0.5f + 0.08f;
                _outlineRenderer.SetPositions(new[]
                {
                    new Vector3(-halfX, floorY, -halfZ),
                    new Vector3(-halfX, floorY, halfZ),
                    new Vector3(halfX, floorY, halfZ),
                    new Vector3(halfX, floorY, -halfZ),
                    new Vector3(-halfX, floorY, -halfZ)
                });
            }

            if (_headingRenderer == null)
            {
                Transform child = transform.Find("IH Delivery Heading Indicator");
                GameObject heading = child != null ? child.gameObject : new GameObject("IH Delivery Heading Indicator");
                heading.transform.SetParent(transform, false);
                _headingRenderer = heading.GetComponent<LineRenderer>() ?? heading.AddComponent<LineRenderer>();
                ConfigureLineRenderer(_headingRenderer, 5);
                float floorY = -zoneDimensions.y * 0.5f + 0.12f;
                float arrowEnd = zoneDimensions.z * 0.38f;
                _headingRenderer.SetPositions(new[]
                {
                    new Vector3(0f, floorY, -zoneDimensions.z * 0.25f),
                    new Vector3(0f, floorY, arrowEnd),
                    new Vector3(-zoneDimensions.x * 0.2f, floorY, arrowEnd - zoneDimensions.z * 0.15f),
                    new Vector3(0f, floorY, arrowEnd),
                    new Vector3(zoneDimensions.x * 0.2f, floorY, arrowEnd - zoneDimensions.z * 0.15f)
                });
            }
        }

        private void ConfigureLineRenderer(LineRenderer renderer, int pointCount)
        {
            renderer.useWorldSpace = false;
            renderer.loop = false;
            renderer.positionCount = pointCount;
            renderer.startWidth = lineWidthMeters;
            renderer.endWidth = lineWidthMeters;
            renderer.alignment = LineAlignment.TransformZ;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            if (_runtimeLineMaterial != null)
            {
                renderer.sharedMaterial = _runtimeLineMaterial;
            }
        }

        private static void ApplyLineColor(LineRenderer renderer, Color color)
        {
            if (renderer == null)
            {
                return;
            }

            renderer.startColor = color;
            renderer.endColor = color;
        }

        private void ResolveService()
        {
            if (_deliveryService == null &&
                LwsApplicationBootstrap.Instance != null &&
                LwsApplicationBootstrap.Instance.Registry != null)
            {
                LwsApplicationBootstrap.Instance.Registry.TryGet(out _deliveryService);
            }
        }
    }
}