using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DeadAir
{
    [RequireComponent(typeof(BoxCollider))]
    [DisallowMultipleComponent]
    public class DeadAirTriggerZone : MonoBehaviour
    {
        [SerializeField] private string beatId = "DA_000";
        [SerializeField] private string displayName = "Dead Air Beat";
        [SerializeField, TextArea] private string description;
        [SerializeField, TextArea] private string notes = "PLACEMENT STATUS: UNPLACED";
        [SerializeField] private bool triggerEnabled = true;
        [SerializeField] private bool triggerOnce = true;
        [SerializeField] private DeadAirTriggerCategory category = DeadAirTriggerCategory.Story;
        [SerializeField] private float estimatedPlaybackSeconds = 8f;
        [SerializeField] private float routeDistanceMiles;
        [SerializeField] private Color debugColor = new Color(0.2f, 0.85f, 1f, 0.28f);
        [SerializeField] private float cooldownSeconds = 0.25f;
        [SerializeField] private int sequenceIndex;

        private bool _triggered;
        private float _nextAllowedTime;
        private BoxCollider _boxCollider;

        public string BeatId => beatId;
        public string DisplayName => displayName;
        public string Notes => notes;
        public bool TriggerEnabled => triggerEnabled;
        public bool TriggerOnce => triggerOnce;
        public DeadAirTriggerCategory Category => category;
        public float RouteDistanceMiles => routeDistanceMiles;
        public int SequenceIndex => sequenceIndex;
        public bool HasTriggered => _triggered;

        protected virtual DeadAirChoiceOutcome ChoiceOutcome => DeadAirChoiceOutcome.None;

        protected virtual void Reset()
        {
            EnsureCollider();
        }

        protected virtual void Awake()
        {
            EnsureCollider();
        }

        protected virtual void OnTriggerEnter(Collider other)
        {
            if (!CanActivate(other))
            {
                return;
            }

            ActivateTrigger();
        }

        public void Configure(DeadAirBeatDefinition beat, int index)
        {
            if (beat == null)
            {
                return;
            }

            beatId = beat.beatId;
            displayName = beat.displayName;
            description = beat.description;
            notes = string.IsNullOrWhiteSpace(beat.notes) ? "PLACEMENT STATUS: UNPLACED" : beat.notes;
            category = beat.category;
            estimatedPlaybackSeconds = beat.estimatedPlaybackSeconds;
            routeDistanceMiles = beat.routeDistanceMiles;
            debugColor = beat.debugColor;
            sequenceIndex = index;
        }

        public void ResetRuntimeState()
        {
            _triggered = false;
            _nextAllowedTime = 0f;
        }

        protected void ActivateTrigger()
        {
            _triggered = true;
            _nextAllowedTime = Time.time + Mathf.Max(0f, cooldownSeconds);

            DeadAirStoryDirector director = DeadAirGameManager.Instance != null
                ? DeadAirGameManager.Instance.StoryDirector
                : FindFirstObjectByType<DeadAirStoryDirector>();

            var triggerEvent = new DeadAirTriggerEvent
            {
                beatId = beatId,
                displayName = displayName,
                description = description,
                notes = notes,
                category = category,
                estimatedPlaybackSeconds = estimatedPlaybackSeconds,
                routeDistanceMiles = routeDistanceMiles,
                position = transform.position,
                triggerObject = gameObject,
                choiceOutcome = ChoiceOutcome,
                timestamp = Time.time
            };

            director?.HandleTrigger(triggerEvent);
            OnActivated(triggerEvent, director);
        }

        protected virtual void OnActivated(DeadAirTriggerEvent triggerEvent, DeadAirStoryDirector director)
        {
        }

        protected void SetCategory(DeadAirTriggerCategory value)
        {
            category = value;
        }

        private bool CanActivate(Collider other)
        {
            if (!triggerEnabled || triggerOnce && _triggered || Time.time < _nextAllowedTime)
            {
                return false;
            }

            return other != null && other.GetComponentInParent<DeadAirVehicleAdapter>() != null;
        }

        private void EnsureCollider()
        {
            _boxCollider = GetComponent<BoxCollider>();
            if (_boxCollider != null)
            {
                _boxCollider.isTrigger = true;
                if (_boxCollider.size == Vector3.zero)
                {
                    _boxCollider.size = new Vector3(12f, 8f, 12f);
                }
            }
        }

        private void OnDrawGizmos()
        {
            EnsureCollider();
            Gizmos.color = debugColor;
            Matrix4x4 previous = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Vector3 center = _boxCollider != null ? _boxCollider.center : Vector3.zero;
            Vector3 size = _boxCollider != null ? _boxCollider.size : Vector3.one * 8f;
            Gizmos.DrawCube(center, size);
            Gizmos.color = new Color(debugColor.r, debugColor.g, debugColor.b, 0.92f);
            Gizmos.DrawWireCube(center, size);
            Gizmos.matrix = previous;

#if UNITY_EDITOR
            Handles.color = Color.white;
            Handles.Label(transform.position + Vector3.up * 2f, $"{sequenceIndex:00} {beatId}\n{displayName}\n{category}");
#endif
        }
    }
}
