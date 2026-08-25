using System;
using UnityEngine;

namespace DeadAir
{
    [DefaultExecutionOrder(-175)]
    [DisallowMultipleComponent]
    public sealed class DeadAirDashboardMisinformationDirector : MonoBehaviour
    {
        [Serializable]
        public struct DashboardState
        {
            public DeadAirDashboardEventKind eventKind;
            public bool active;
            public string overrideSpeedText;
            public string overrideGearText;
            public string overrideFuelText;
            public string overrideClockText;
            public string warningLampId;
            public float durationSeconds;
        }

        [SerializeField] private DashboardState state;

        private float _expiresAt;

        public event Action<DashboardState> DashboardStateChanged;
        public DashboardState CurrentState => state;

        private void Update()
        {
            if (state.active && state.durationSeconds > 0f && Time.time >= _expiresAt)
            {
                Clear();
            }
        }

        public void Apply(DashboardState nextState)
        {
            state = nextState;
            state.active = nextState.eventKind != DeadAirDashboardEventKind.None;
            _expiresAt = state.durationSeconds > 0f ? Time.time + state.durationSeconds : float.PositiveInfinity;
            DashboardStateChanged?.Invoke(state);
        }

        public void Clear()
        {
            state = default;
            DashboardStateChanged?.Invoke(state);
        }
    }
}
