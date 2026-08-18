using UnityEngine;

namespace LWS.InterstateHauler
{
    [DisallowMultipleComponent]
    public sealed class LwsFloatingOriginRigidbodyParticipant : LwsFloatingOriginTransformParticipant
    {
        [SerializeField] private bool includeChildRigidbodies = true;

        private Rigidbody[] _rigidbodies = System.Array.Empty<Rigidbody>();
        private Vector3[] _linearVelocities = System.Array.Empty<Vector3>();
        private Vector3[] _angularVelocities = System.Array.Empty<Vector3>();

        protected override void OnEnable()
        {
            RefreshRigidbodies();
            base.OnEnable();
        }

        public override bool ApplyOriginShift(LwsOriginShiftEvent shiftEvent, out string message)
        {
            RefreshRigidbodiesIfNeeded();
            CaptureVelocities();
            transform.position += shiftEvent.LocalTranslationDelta;
            RestoreVelocities();
            message = $"{ParticipantId} shifted with rigidbody velocities preserved.";
            return true;
        }

        public void RefreshRigidbodies()
        {
            _rigidbodies = includeChildRigidbodies
                ? GetComponentsInChildren<Rigidbody>(true)
                : new[] { GetComponent<Rigidbody>() };

            int length = _rigidbodies != null ? _rigidbodies.Length : 0;
            if (_linearVelocities.Length != length)
            {
                _linearVelocities = new Vector3[length];
                _angularVelocities = new Vector3[length];
            }
        }

        private void RefreshRigidbodiesIfNeeded()
        {
            if (_rigidbodies == null || _rigidbodies.Length == 0)
            {
                RefreshRigidbodies();
            }
        }

        private void CaptureVelocities()
        {
            for (int i = 0; i < _rigidbodies.Length; i++)
            {
                Rigidbody body = _rigidbodies[i];
                if (body == null)
                {
                    _linearVelocities[i] = Vector3.zero;
                    _angularVelocities[i] = Vector3.zero;
                    continue;
                }

                _linearVelocities[i] = body.linearVelocity;
                _angularVelocities[i] = body.angularVelocity;
            }
        }

        private void RestoreVelocities()
        {
            for (int i = 0; i < _rigidbodies.Length; i++)
            {
                Rigidbody body = _rigidbodies[i];
                if (body == null)
                {
                    continue;
                }

                body.linearVelocity = _linearVelocities[i];
                body.angularVelocity = _angularVelocities[i];
            }
        }
    }
}
