using UnityEngine;

namespace LWS.InterstateHauler
{
    [DisallowMultipleComponent]
    public sealed class LwsCabAccessoryBobble : MonoBehaviour
    {
        [SerializeField] private bool animate = true;
        [SerializeField] private float motionScale = 4f;
        [SerializeField] private float damping = 8f;
        [SerializeField] private float maxTiltDegrees = 8f;
        [SerializeField] private LwsNwhVehicleAdapter vehicleAdapter;

        private Vector3 _previousPosition;
        private Vector2 _tilt;
        private Quaternion _baseRotation;
        private bool _initialized;

        public bool Animate
        {
            get => animate;
            set => animate = value;
        }

        private void Awake()
        {
            _baseRotation = transform.localRotation;
            ResolveVehicleAdapter();
        }

        private void OnEnable()
        {
            _initialized = false;
        }

        private void Update()
        {
            if (!animate)
            {
                transform.localRotation = _baseRotation;
                return;
            }

            ResolveVehicleAdapter();
            Vector3 velocity = Vector3.zero;
            if (vehicleAdapter != null)
            {
                LwsVehicleTelemetry telemetry = vehicleAdapter.ReadTelemetry();
                float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
                velocity = transform.root.InverseTransformDirection((telemetry.worldPosition - _previousPosition) / deltaTime);
                _previousPosition = telemetry.worldPosition;
            }

            if (!_initialized)
            {
                _initialized = true;
                return;
            }

            Vector2 target = new Vector2(-velocity.z, velocity.x) * motionScale;
            _tilt = Vector2.Lerp(_tilt, target, Time.deltaTime * damping);
            _tilt.x = Mathf.Clamp(_tilt.x, -maxTiltDegrees, maxTiltDegrees);
            _tilt.y = Mathf.Clamp(_tilt.y, -maxTiltDegrees, maxTiltDegrees);
            transform.localRotation = _baseRotation * Quaternion.Euler(_tilt.x, 0f, _tilt.y);
        }

        private void ResolveVehicleAdapter()
        {
            if (vehicleAdapter != null)
            {
                return;
            }

            vehicleAdapter = GetComponentInParent<LwsNwhVehicleAdapter>();
        }
    }
}
