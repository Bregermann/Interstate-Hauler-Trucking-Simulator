using NWH.VehiclePhysics2;
using NWH.VehiclePhysics2.Modules.Trailer;
using UnityEngine;

namespace LWS.InterstateHauler
{
    [DefaultExecutionOrder(220)]
    [DisallowMultipleComponent]
    public sealed class LwsTruckUprightRecoveryController : MonoBehaviour
    {
        public const float DefaultResetHeightOffset = 1.6f;
        public const float DefaultFlippedDotThreshold = 0.35f;
        public const float DefaultFlippedDetectionDelaySeconds = 2f;

        [Header("References")]
        [SerializeField] private VehicleController vehicleController;
        [SerializeField] private Rigidbody tractorRigidbody;
        [SerializeField] private TrailerHitchModuleWrapper trailerHitch;

        [Header("Reset Upright")]
        [Tooltip("Vertical clearance above the detected ground point when the tractor is reset upright.")]
        [SerializeField] private float resetHeightOffset = DefaultResetHeightOffset;
        [Tooltip("Vertical clearance used while checking whether an attached trailer also needs more lift.")]
        [SerializeField] private float trailerGroundClearanceOffset = 1.2f;
        [Tooltip("If enabled, an attached trailer is reset with the tractor and its hitch points are kept aligned.")]
        [SerializeField] private bool resetAttachedTrailer = true;
        [Tooltip("Fallback distance behind the tractor if trailer attachment points cannot be resolved.")]
        [SerializeField] private float fallbackAttachedTrailerDistance = 8f;
        [Tooltip("Height above the current vehicle position where the ground probe starts.")]
        [SerializeField] private float groundRaycastStartHeight = 8f;
        [Tooltip("Maximum distance searched downward when finding safe reset ground.")]
        [SerializeField] private float groundRaycastDistance = 40f;
        [Tooltip("Layers treated as reset ground. Vehicle and trailer bodies are filtered out even if their layers are included.")]
        [SerializeField] private LayerMask groundMask = Physics.DefaultRaycastLayers;

        [Header("Input")]
        [Tooltip("When enabled, reads the existing LWS resetTruckUpright command from ILwsVehicleInputService.")]
        [SerializeField] private bool listenForInputCommand = true;

        [Header("Flipped Detection")]
        [Tooltip("A truck is considered substantially overturned when Dot(transform.up, world up) is less than or equal to this value.")]
        [SerializeField] private float flippedDotThreshold = DefaultFlippedDotThreshold;
        [Tooltip("How long the truck must remain substantially overturned before HasBeenFlippedLongEnough becomes true.")]
        [SerializeField] private float flippedDetectionDelaySeconds = DefaultFlippedDetectionDelaySeconds;

        [Header("Diagnostics")]
        [SerializeField] private bool logResetDiagnostics = true;

        private ILwsVehicleInputService _inputService;
        private float _flippedTimer;
        private bool _resetInProgress;

        public float ResetHeightOffset => resetHeightOffset;
        public float FlippedDotThreshold => flippedDotThreshold;
        public float FlippedDetectionDelaySeconds => flippedDetectionDelaySeconds;
        public bool IsSubstantiallyOverturned => IsSubstantiallyOverturnedTransform(transform, flippedDotThreshold);
        public bool HasBeenFlippedLongEnough => _flippedTimer >= flippedDetectionDelaySeconds;
        public float CurrentFlippedSeconds => _flippedTimer;

        private void Reset()
        {
            ResolveReferences();
        }

        private void Awake()
        {
            ResolveReferences();
        }

        private void Update()
        {
            UpdateFlippedTimer();
            if (!listenForInputCommand)
            {
                return;
            }

            ResolveInputService();
            if (_inputService != null && LwsVehicleCommandFrameUtility.IsPressed(_inputService.ReadCommandFrame().resetTruckUpright))
            {
                RequestResetUpright("Input command");
            }
        }

        public bool RequestResetUpright(string reason = "Player requested reset upright")
        {
            if (_resetInProgress)
            {
                return false;
            }

            ResolveReferences();
            if (tractorRigidbody == null)
            {
                Debug.LogWarning($"[IH Truck Reset] Cannot reset {name}; tractor Rigidbody was not found.", this);
                return false;
            }

            _resetInProgress = true;
            try
            {
                Quaternion tractorRotation = CreateUprightYawRotation(transform.rotation);
                Vector3 tractorPosition = ResolveRaisedGroundPosition(transform.position, resetHeightOffset, tractorRigidbody, null);

                AttachedTrailerReset trailerReset = resetAttachedTrailer ? CaptureAttachedTrailerReset(tractorRotation, tractorPosition) : default;
                if (trailerReset.valid)
                {
                    ApplyCombinationLift(ref tractorPosition, ref trailerReset);
                    ApplyBodyPose(trailerReset.body, trailerReset.transform, trailerReset.position, trailerReset.rotation);
                }

                ApplyBodyPose(tractorRigidbody, transform, tractorPosition, tractorRotation);
                Physics.SyncTransforms();
                WakeBodies(tractorRigidbody, trailerReset.body);
                _flippedTimer = 0f;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (logResetDiagnostics)
                {
                    string trailerText = trailerReset.valid ? $", trailer {trailerReset.transform.name} reset" : ", no attached trailer reset";
                    Debug.Log($"[IH Truck Reset] Reset upright completed for {name} at {tractorPosition} yaw {tractorRotation.eulerAngles.y:0.0}. Reason: {reason}{trailerText}.", this);
                }
#endif
                return true;
            }
            finally
            {
                _resetInProgress = false;
            }
        }

        public static Quaternion CreateUprightYawRotation(Quaternion currentRotation)
        {
            Vector3 forward = Vector3.ProjectOnPlane(currentRotation * Vector3.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.0001f)
            {
                Vector3 right = Vector3.ProjectOnPlane(currentRotation * Vector3.right, Vector3.up);
                forward = right.sqrMagnitude > 0.0001f ? Vector3.Cross(Vector3.up, right).normalized : Vector3.forward;
            }

            return Quaternion.LookRotation(forward.normalized, Vector3.up);
        }

        public static bool IsSubstantiallyOverturnedTransform(Transform target, float dotThreshold)
        {
            return target != null && Vector3.Dot(target.up, Vector3.up) <= dotThreshold;
        }

        private void ResolveReferences()
        {
            if (vehicleController == null)
            {
                vehicleController = GetComponent<VehicleController>();
            }

            if (tractorRigidbody == null)
            {
                tractorRigidbody = vehicleController != null && vehicleController.vehicleRigidbody != null
                    ? vehicleController.vehicleRigidbody
                    : GetComponent<Rigidbody>();
            }

            if (trailerHitch == null)
            {
                trailerHitch = GetComponentInChildren<TrailerHitchModuleWrapper>(true);
            }
        }

        private void ResolveInputService()
        {
            if (_inputService != null || LwsApplicationBootstrap.Instance == null || LwsApplicationBootstrap.Instance.Registry == null)
            {
                return;
            }

            LwsApplicationBootstrap.Instance.Registry.TryGet(out _inputService);
        }

        private void UpdateFlippedTimer()
        {
            if (IsSubstantiallyOverturned)
            {
                _flippedTimer += Time.deltaTime;
            }
            else
            {
                _flippedTimer = 0f;
            }
        }

        private Vector3 ResolveRaisedGroundPosition(Vector3 currentPosition, float heightOffset, Rigidbody ignoredBodyA, Rigidbody ignoredBodyB)
        {
            if (TryFindGround(currentPosition, ignoredBodyA, ignoredBodyB, out Vector3 groundPoint))
            {
                return new Vector3(currentPosition.x, groundPoint.y + Mathf.Max(0f, heightOffset), currentPosition.z);
            }

            return currentPosition + Vector3.up * Mathf.Max(0f, heightOffset);
        }

        private bool TryFindGround(Vector3 position, Rigidbody ignoredBodyA, Rigidbody ignoredBodyB, out Vector3 groundPoint)
        {
            Vector3 origin = position + Vector3.up * Mathf.Max(0.1f, groundRaycastStartHeight);
            float distance = Mathf.Max(0.1f, groundRaycastStartHeight + groundRaycastDistance);
            RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, distance, groundMask, QueryTriggerInteraction.Ignore);
            float bestDistance = float.PositiveInfinity;
            groundPoint = Vector3.zero;
            bool found = false;

            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];
                if (hit.collider == null || ShouldIgnoreHit(hit.collider, ignoredBodyA, ignoredBodyB))
                {
                    continue;
                }

                if (hit.distance < bestDistance)
                {
                    bestDistance = hit.distance;
                    groundPoint = hit.point;
                    found = true;
                }
            }

            return found;
        }

        private static bool ShouldIgnoreHit(Collider hitCollider, Rigidbody ignoredBodyA, Rigidbody ignoredBodyB)
        {
            Rigidbody attached = hitCollider.attachedRigidbody;
            if (attached != null && (attached == ignoredBodyA || attached == ignoredBodyB))
            {
                return true;
            }

            Transform hitTransform = hitCollider.transform;
            return (ignoredBodyA != null && hitTransform.IsChildOf(ignoredBodyA.transform)) ||
                   (ignoredBodyB != null && hitTransform.IsChildOf(ignoredBodyB.transform));
        }

        private AttachedTrailerReset CaptureAttachedTrailerReset(Quaternion tractorRotation, Vector3 tractorPosition)
        {
            VehicleController trailerController = ResolveAttachedTrailerController(out TrailerModule trailerModule);
            Rigidbody trailerBody = trailerController != null ? trailerController.vehicleRigidbody : null;
            if (trailerController == null || trailerBody == null)
            {
                return default;
            }

            Transform trailerTransform = trailerController.transform;
            Quaternion trailerRotation = CreateUprightYawRotation(trailerTransform.rotation);
            Vector3 trailerPosition;
            bool hasAttachmentPoints = trailerHitch != null &&
                                       trailerHitch.module != null &&
                                       trailerHitch.module.attachmentPoint != null &&
                                       trailerModule != null &&
                                       trailerModule.attachmentPoint != null;

            if (hasAttachmentPoints)
            {
                Vector3 localHitchPoint = transform.InverseTransformPoint(trailerHitch.module.attachmentPoint.position);
                Vector3 localTrailerAttachmentPoint = trailerTransform.InverseTransformPoint(trailerModule.attachmentPoint.position);
                Vector3 hitchWorld = tractorPosition + tractorRotation * localHitchPoint;
                trailerPosition = hitchWorld - trailerRotation * localTrailerAttachmentPoint;
            }
            else
            {
                Vector3 fallbackOffset = Vector3.ProjectOnPlane(trailerTransform.position - transform.position, Vector3.up);
                if (fallbackOffset.sqrMagnitude < 1f)
                {
                    fallbackOffset = -(tractorRotation * Vector3.forward) * Mathf.Max(1f, fallbackAttachedTrailerDistance);
                }

                trailerPosition = tractorPosition + fallbackOffset;
            }

            return new AttachedTrailerReset
            {
                valid = true,
                transform = trailerTransform,
                body = trailerBody,
                position = trailerPosition,
                rotation = trailerRotation
            };
        }

        private VehicleController ResolveAttachedTrailerController(out TrailerModule trailerModule)
        {
            trailerModule = null;
            if (trailerHitch == null || trailerHitch.module == null || !trailerHitch.module.attached || trailerHitch.module.attachedTrailerModule == null)
            {
                return null;
            }

            trailerModule = trailerHitch.module.attachedTrailerModule;
            return trailerModule.vehicleController;
        }

        private void ApplyCombinationLift(ref Vector3 tractorPosition, ref AttachedTrailerReset trailerReset)
        {
            float lift = 0f;
            if (TryFindGround(tractorPosition, tractorRigidbody, trailerReset.body, out Vector3 tractorGround))
            {
                lift = Mathf.Max(lift, tractorGround.y + resetHeightOffset - tractorPosition.y);
            }

            if (TryFindGround(trailerReset.position, tractorRigidbody, trailerReset.body, out Vector3 trailerGround))
            {
                lift = Mathf.Max(lift, trailerGround.y + trailerGroundClearanceOffset - trailerReset.position.y);
            }

            if (lift > 0f)
            {
                Vector3 liftVector = Vector3.up * lift;
                tractorPosition += liftVector;
                trailerReset.position += liftVector;
            }
        }

        private static void ApplyBodyPose(Rigidbody body, Transform targetTransform, Vector3 position, Quaternion rotation)
        {
            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.position = position;
                body.rotation = rotation;
            }

            targetTransform.SetPositionAndRotation(position, rotation);
        }

        private static void WakeBodies(Rigidbody tractorBody, Rigidbody trailerBody)
        {
            tractorBody?.WakeUp();
            trailerBody?.WakeUp();
        }

        private void OnValidate()
        {
            resetHeightOffset = Mathf.Max(0.1f, resetHeightOffset);
            trailerGroundClearanceOffset = Mathf.Max(0.1f, trailerGroundClearanceOffset);
            fallbackAttachedTrailerDistance = Mathf.Max(1f, fallbackAttachedTrailerDistance);
            groundRaycastStartHeight = Mathf.Max(0.1f, groundRaycastStartHeight);
            groundRaycastDistance = Mathf.Max(0.1f, groundRaycastDistance);
            flippedDotThreshold = Mathf.Clamp(flippedDotThreshold, -1f, 1f);
            flippedDetectionDelaySeconds = Mathf.Max(0f, flippedDetectionDelaySeconds);
        }

        [ContextMenu("Reset Truck Upright")]
        private void ResetTruckUprightFromInspector()
        {
            RequestResetUpright("Inspector context menu");
        }

        private struct AttachedTrailerReset
        {
            public bool valid;
            public Transform transform;
            public Rigidbody body;
            public Vector3 position;
            public Quaternion rotation;
        }
    }
}
