using System;
using NWH.VehiclePhysics2;
using NWH.VehiclePhysics2.Modules.Trailer;
using UnityEngine;

namespace LWS.InterstateHauler
{
    public interface ILwsTrailerCoupling
    {
        bool IsTrailerAttached { get; }
        string CurrentTrailerId { get; }
        LwsTrailerAttachmentState CurrentState { get; }
        event Action<LwsTrailerAttachmentState> TrailerAttached;
        event Action<LwsTrailerAttachmentState> TrailerDetached;
        void RequestAttachDetach();
    }

    [DisallowMultipleComponent]
    public sealed class LwsNwhTrailerCouplingAdapter : MonoBehaviour, ILwsTrailerCoupling
    {
        [SerializeField] private VehicleController vehicleController;
        [SerializeField] private TrailerHitchModuleWrapper trailerHitch;
        [SerializeField] private LwsVehicleIdentity towingIdentity;

        private bool _lastAttached;
        private string _lastTrailerId = string.Empty;

        public bool IsTrailerAttached => trailerHitch != null && trailerHitch.module != null && trailerHitch.module.attached;
        public string CurrentTrailerId => ResolveAttachedTrailerId();
        public LwsTrailerAttachmentState CurrentState => BuildState();

        public event Action<LwsTrailerAttachmentState> TrailerAttached;
        public event Action<LwsTrailerAttachmentState> TrailerDetached;

        private void Reset()
        {
            ResolveReferences();
        }

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            if (trailerHitch != null && trailerHitch.module != null)
            {
                trailerHitch.module.onTrailerAttach.AddListener(OnNwhTrailerAttached);
                trailerHitch.module.onTrailerDetach.AddListener(OnNwhTrailerDetached);
            }

            _lastAttached = IsTrailerAttached;
            _lastTrailerId = CurrentTrailerId;
        }

        private void OnDisable()
        {
            if (trailerHitch != null && trailerHitch.module != null)
            {
                trailerHitch.module.onTrailerAttach.RemoveListener(OnNwhTrailerAttached);
                trailerHitch.module.onTrailerDetach.RemoveListener(OnNwhTrailerDetached);
            }
        }

        private void Update()
        {
            MonitorState();
        }

        public void RequestAttachDetach()
        {
            if (vehicleController != null && vehicleController.input != null)
            {
                vehicleController.input.TrailerAttachDetach = true;
            }
        }

        private void ResolveReferences()
        {
            if (vehicleController == null)
            {
                vehicleController = GetComponent<VehicleController>();
            }

            if (trailerHitch == null)
            {
                trailerHitch = GetComponentInChildren<TrailerHitchModuleWrapper>();
            }

            if (towingIdentity == null)
            {
                towingIdentity = GetComponent<LwsVehicleIdentity>();
            }
        }

        private void OnNwhTrailerAttached()
        {
            PublishState(true);
        }

        private void OnNwhTrailerDetached()
        {
            PublishState(false);
        }

        private void MonitorState()
        {
            bool attached = IsTrailerAttached;
            string trailerId = CurrentTrailerId;
            if (attached == _lastAttached && string.Equals(trailerId, _lastTrailerId, StringComparison.Ordinal))
            {
                return;
            }

            PublishState(attached);
        }

        private void PublishState(bool attached)
        {
            LwsTrailerAttachmentState state = BuildState();
            _lastAttached = attached;
            _lastTrailerId = state.trailerId ?? string.Empty;

            if (attached)
            {
                TrailerAttached?.Invoke(state);
            }
            else
            {
                TrailerDetached?.Invoke(state);
            }
        }

        private LwsTrailerAttachmentState BuildState()
        {
            VehicleController trailerController = ResolveAttachedTrailerController();
            LwsVehicleIdentity trailerIdentity = trailerController != null
                ? trailerController.GetComponent<LwsVehicleIdentity>()
                : null;

            return new LwsTrailerAttachmentState
            {
                attached = IsTrailerAttached,
                towingVehicleId = towingIdentity != null ? towingIdentity.VehicleId : string.Empty,
                trailerId = trailerIdentity != null
                    ? trailerIdentity.VehicleId
                    : trailerController != null ? trailerController.name : string.Empty,
                trailerPose = LwsSerializablePose.FromTransform(trailerController != null ? trailerController.transform : null)
            };
        }

        private string ResolveAttachedTrailerId()
        {
            return BuildState().trailerId;
        }

        private VehicleController ResolveAttachedTrailerController()
        {
            if (trailerHitch == null || trailerHitch.module == null || trailerHitch.module.attachedTrailerModule == null)
            {
                return null;
            }

            return trailerHitch.module.attachedTrailerModule.vehicleController;
        }
    }
}
