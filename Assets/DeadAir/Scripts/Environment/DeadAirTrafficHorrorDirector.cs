using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeadAir
{
    [DefaultExecutionOrder(-170)]
    [DisallowMultipleComponent]
    public sealed class DeadAirTrafficHorrorDirector : MonoBehaviour
    {
        [Serializable]
        public sealed class TrafficEvent
        {
            public string eventId = "DA_TRAFFIC";
            public DeadAirTrafficHorrorEventKind kind = DeadAirTrafficHorrorEventKind.HeadlightsBehind;
            public GameObject prefab;
            public Transform marker;
            public float lifetimeSeconds = 12f;
            public bool attachToMarker;
        }

        private readonly List<GameObject> _spawned = new List<GameObject>();

        public string LastEventId { get; private set; } = string.Empty;
        public IReadOnlyList<GameObject> Spawned => _spawned;

        public void Play(TrafficEvent trafficEvent)
        {
            if (trafficEvent == null)
            {
                return;
            }

            LastEventId = trafficEvent.eventId;
            if (trafficEvent.prefab == null)
            {
                Debug.Log($"[Dead Air] Traffic horror hook: {trafficEvent.kind} ({trafficEvent.eventId}).", this);
                return;
            }

            Transform marker = trafficEvent.marker != null ? trafficEvent.marker : transform;
            GameObject instance = Instantiate(trafficEvent.prefab, marker.position, marker.rotation);
            instance.name = $"Dead Air Traffic Event - {trafficEvent.eventId}";
            if (trafficEvent.attachToMarker)
            {
                instance.transform.SetParent(marker, true);
            }

            _spawned.Add(instance);
            if (trafficEvent.lifetimeSeconds > 0f)
            {
                Destroy(instance, trafficEvent.lifetimeSeconds);
            }
        }

        public void Cleanup()
        {
            for (int i = _spawned.Count - 1; i >= 0; i--)
            {
                if (_spawned[i] != null)
                {
                    Destroy(_spawned[i]);
                }
            }

            _spawned.Clear();
        }
    }
}
