using System;
using UnityEngine;

namespace LWS.InterstateHauler
{
    [DisallowMultipleComponent]
    public sealed class LwsPlayerGestureController : MonoBehaviour
    {
        [SerializeField] private LwsVehicleIdentity sourceIdentity;
        [SerializeField] private Transform attentionOrigin;
        [SerializeField] private float targetRadiusMeters = 40f;
        [SerializeField, Range(-1f, 1f)] private float minimumForwardDot = -0.15f;
        [SerializeField] private float gestureDurationSeconds = 1.2f;
        [SerializeField] private float cooldownSeconds = 2.0f;

        private readonly Collider[] _targetBuffer = new Collider[32];
        private LwsTruckGestureType _activeGesture;
        private float _activeUntil;
        private float _cooldownUntil;
        private LwsDriverGestureEvent _lastEvent;

        public LwsTruckGestureType ActiveGesture => _activeGesture;
        public LwsDriverGestureEvent LastEvent => _lastEvent;
        public float CooldownRemaining => Mathf.Max(0f, _cooldownUntil - Time.time);
        public float GestureDurationSeconds => gestureDurationSeconds;
        public float CooldownSeconds => cooldownSeconds;
        public bool IsGestureActive => _activeGesture != LwsTruckGestureType.None && Time.time < _activeUntil;

        public event Action<LwsDriverGestureEvent> GestureStarted;
        public event Action<LwsDriverGestureEvent> GestureEnded;

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
            if (_activeGesture != LwsTruckGestureType.None && Time.time >= _activeUntil)
            {
                _activeGesture = LwsTruckGestureType.None;
                GestureEnded?.Invoke(_lastEvent);
            }
        }

        public bool RequestFlipOff(out LwsDriverGestureEvent gestureEvent)
        {
            return RequestGesture(LwsTruckGestureType.FlipOffDriver, out gestureEvent);
        }

        public bool RequestGesture(LwsTruckGestureType gestureType, out LwsDriverGestureEvent gestureEvent)
        {
            ResolveReferences();
            if (gestureType == LwsTruckGestureType.None || Time.time < _cooldownUntil)
            {
                gestureEvent = _lastEvent;
                return false;
            }

            Transform origin = attentionOrigin != null ? attentionOrigin : transform;
            LwsVehicleIdentity target = FindGestureTarget(origin);
            gestureEvent = new LwsDriverGestureEvent
            {
                gestureType = gestureType,
                sourceVehicleId = sourceIdentity != null ? sourceIdentity.VehicleId : name,
                targetVehicleId = target != null ? target.VehicleId : string.Empty,
                hasValidTarget = target != null,
                targetDistanceMeters = target != null ? Vector3.Distance(origin.position, target.transform.position) : 0f,
                sourcePosition = origin.position,
                targetPosition = target != null ? target.transform.position : Vector3.zero,
                sourceForward = origin.forward,
                timestamp = Time.time
            };

            _lastEvent = gestureEvent;
            _activeGesture = gestureType;
            _activeUntil = Time.time + Mathf.Max(0.1f, gestureDurationSeconds);
            _cooldownUntil = Time.time + Mathf.Max(0.1f, cooldownSeconds);
            GestureStarted?.Invoke(gestureEvent);
            return true;
        }

        private LwsVehicleIdentity FindGestureTarget(Transform origin)
        {
            int count = Physics.OverlapSphereNonAlloc(
                origin.position,
                targetRadiusMeters,
                _targetBuffer,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            LwsVehicleIdentity best = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                Collider candidateCollider = _targetBuffer[i];
                if (candidateCollider == null)
                {
                    continue;
                }

                LwsVehicleIdentity candidate = candidateCollider.GetComponentInParent<LwsVehicleIdentity>();
                if (candidate == null || candidate == sourceIdentity || candidate.Role == LwsVehicleRole.PlayerTractor)
                {
                    continue;
                }

                Vector3 toTarget = candidate.transform.position - origin.position;
                float distance = toTarget.magnitude;
                if (distance <= 0.01f || distance >= bestDistance)
                {
                    continue;
                }

                float dot = Vector3.Dot(origin.forward, toTarget.normalized);
                if (dot < minimumForwardDot)
                {
                    continue;
                }

                best = candidate;
                bestDistance = distance;
            }

            return best;
        }

        private void ResolveReferences()
        {
            if (sourceIdentity == null)
            {
                sourceIdentity = GetComponent<LwsVehicleIdentity>();
            }

            if (attentionOrigin == null)
            {
                attentionOrigin = transform;
            }
        }
    }
}
