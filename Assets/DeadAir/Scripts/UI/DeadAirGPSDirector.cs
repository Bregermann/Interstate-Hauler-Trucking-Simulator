using System;
using LWS.InterstateHauler;
using UnityEngine;

namespace DeadAir
{
    [DefaultExecutionOrder(-205)]
    [DisallowMultipleComponent]
    public sealed class DeadAirGPSDirector : MonoBehaviour
    {
        [SerializeField] private DeadAirGpsState state = DeadAirGpsState.Default();
        [SerializeField] private bool consumeLwsNavigationState = true;
        [SerializeField] private bool allowIntentionalMisinformation = true;

        private ILwsNavigationService _navigationService;

        public event Action<DeadAirGpsState> GpsStateChanged;
        public DeadAirGpsState CurrentState => state;

        private void Update()
        {
            ResolveServices();
            if (consumeLwsNavigationState && _navigationService != null && !state.intentionallyWrong)
            {
                LwsNavigationRuntimeState runtime = _navigationService.RuntimeState;
                state.currentRoad = string.IsNullOrWhiteSpace(runtime.currentRoadDisplayName) ? state.currentRoad : runtime.currentRoadDisplayName;
                state.instructionText = string.IsNullOrWhiteSpace(runtime.nextInstructionText) ? state.instructionText : runtime.nextInstructionText;
                state.distanceToInstructionMeters = runtime.distanceToNextManeuverMeters;
                state.remainingRouteMeters = runtime.distanceRemainingMeters;
                state.routeVisible = runtime.routeActive;
                state.presentationMode = runtime.recalculating ? DeadAirGpsPresentationMode.Recalculating : state.presentationMode;
            }

            GpsStateChanged?.Invoke(state);
        }

        public void ResetGps()
        {
            state = DeadAirGpsState.Default();
            GpsStateChanged?.Invoke(state);
        }

        public void SetEnabled(bool enabled)
        {
            state.enabled = enabled;
            state.presentationMode = enabled ? DeadAirGpsPresentationMode.Normal : DeadAirGpsPresentationMode.Hidden;
            GpsStateChanged?.Invoke(state);
        }

        public void SetDestination(string destination)
        {
            state.destination = destination;
            GpsStateChanged?.Invoke(state);
        }

        public void SetRouteVisible(bool visible)
        {
            state.routeVisible = visible;
            GpsStateChanged?.Invoke(state);
        }

        public void SetRecalculating(bool recalculating)
        {
            state.presentationMode = recalculating ? DeadAirGpsPresentationMode.Recalculating : DeadAirGpsPresentationMode.Normal;
            GpsStateChanged?.Invoke(state);
        }

        public void SetGpsInstruction(string instruction, DeadAirGpsArrow arrow, float distanceMeters, bool intentionallyWrong)
        {
            state.enabled = true;
            state.presentationMode = DeadAirGpsPresentationMode.Normal;
            state.instructionText = instruction;
            state.arrow = arrow;
            state.distanceToInstructionMeters = Mathf.Max(0f, distanceMeters);
            state.intentionallyWrong = allowIntentionalMisinformation && intentionallyWrong;
            GpsStateChanged?.Invoke(state);
        }

        public void SetSignalLost(bool lost)
        {
            state.presentationMode = lost ? DeadAirGpsPresentationMode.SignalLost : DeadAirGpsPresentationMode.Normal;
            state.enabled = !lost;
            GpsStateChanged?.Invoke(state);
        }

        public void SetCorrupted(bool corrupted)
        {
            state.presentationMode = corrupted ? DeadAirGpsPresentationMode.Corrupted : DeadAirGpsPresentationMode.Normal;
            state.intentionallyWrong = corrupted;
            GpsStateChanged?.Invoke(state);
        }

        private void ResolveServices()
        {
            if (_navigationService != null ||
                LwsApplicationBootstrap.Instance == null ||
                LwsApplicationBootstrap.Instance.Registry == null)
            {
                return;
            }

            LwsApplicationBootstrap.Instance.Registry.TryGet(out _navigationService);
        }
    }
}
