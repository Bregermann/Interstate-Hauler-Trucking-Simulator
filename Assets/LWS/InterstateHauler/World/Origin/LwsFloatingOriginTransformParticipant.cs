using UnityEngine;

namespace LWS.InterstateHauler
{
    [DisallowMultipleComponent]
    public class LwsFloatingOriginTransformParticipant : MonoBehaviour, ILwsFloatingOriginParticipant
    {
        [SerializeField] private string participantId = "floating-origin.spatial-root";
        [SerializeField] private LwsFloatingOriginParticipantKind participantKind = LwsFloatingOriginParticipantKind.GenericSpatialRoot;
        [SerializeField] private bool alignToCurrentOriginOnRegistration;
        [SerializeField] private bool registerOnEnable = true;

        private ILwsWorldOriginService _originService;
        private bool _registered;

        public string ParticipantId => string.IsNullOrWhiteSpace(participantId) ? name : participantId;
        public LwsFloatingOriginParticipantKind ParticipantKind => participantKind;
        public Transform ParticipantTransform => transform;
        public bool AlignToCurrentOriginOnRegistration => alignToCurrentOriginOnRegistration;

        public void Configure(
            string newParticipantId,
            LwsFloatingOriginParticipantKind newKind,
            bool alignOnRegistration)
        {
            participantId = string.IsNullOrWhiteSpace(newParticipantId) ? participantId : newParticipantId;
            participantKind = newKind;
            alignToCurrentOriginOnRegistration = alignOnRegistration;
            if (isActiveAndEnabled)
            {
                RegisterIfPossible();
            }
        }

        protected virtual void OnEnable()
        {
            if (registerOnEnable)
            {
                RegisterIfPossible();
            }
        }

        protected virtual void Start()
        {
            if (registerOnEnable)
            {
                RegisterIfPossible();
            }
        }

        protected virtual void OnDisable()
        {
            Unregister();
        }

        public virtual bool ApplyOriginShift(LwsOriginShiftEvent shiftEvent, out string message)
        {
            transform.position += shiftEvent.LocalTranslationDelta;
            message = $"{ParticipantId} shifted by {shiftEvent.LocalTranslationDelta}.";
            return true;
        }

        protected void RegisterIfPossible()
        {
            if (_registered)
            {
                return;
            }

            ResolveOriginService();
            if (_originService == null)
            {
                return;
            }

            _originService.RegisterParticipant(this);
            _registered = true;
        }

        protected void Unregister()
        {
            if (!_registered)
            {
                return;
            }

            _originService?.UnregisterParticipant(this);
            _registered = false;
        }

        private void ResolveOriginService()
        {
            if (_originService != null ||
                LwsApplicationBootstrap.Instance == null ||
                LwsApplicationBootstrap.Instance.Registry == null)
            {
                return;
            }

            LwsApplicationBootstrap.Instance.Registry.TryGet(out _originService);
        }
    }
}
