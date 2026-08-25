using System;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DeadAir
{
    [DisallowMultipleComponent]
    public sealed class DeadAirRouteFlowGizmo : MonoBehaviour
    {
        [SerializeField] private bool showRouteFlow = true;
        [SerializeField] private bool showChoiceBranches = true;

        public bool ShowRouteFlow => showRouteFlow;

        private void OnDrawGizmos()
        {
            if (!showRouteFlow || !DeadAirTriggerZone.ShowDeadAirGizmos)
            {
                return;
            }

            DeadAirTriggerZone[] triggers = FindObjectsByType<DeadAirTriggerZone>(FindObjectsSortMode.None)
                .Where(trigger => trigger != null && !string.IsNullOrWhiteSpace(trigger.BeatId))
                .OrderBy(trigger => trigger.SequenceIndex)
                .ThenBy(trigger => trigger.BeatId, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            for (int i = 1; i < triggers.Length; i++)
            {
                DeadAirTriggerZone previous = triggers[i - 1];
                DeadAirTriggerZone current = triggers[i];
                bool branch = previous.Category == DeadAirTriggerCategory.ChoiceStart ||
                              current.Category == DeadAirTriggerCategory.ChoiceCommit;
                if (branch && !showChoiceBranches)
                {
                    continue;
                }

                Gizmos.color = branch ? new Color(1f, 0.3f, 0.9f, 0.85f) : new Color(0.85f, 0.95f, 1f, 0.6f);
                Gizmos.DrawLine(previous.transform.position + Vector3.up, current.transform.position + Vector3.up);
#if UNITY_EDITOR
                Vector3 midpoint = Vector3.Lerp(previous.transform.position, current.transform.position, 0.5f) + Vector3.up * 1.25f;
                Handles.color = Gizmos.color;
                Handles.Label(midpoint, branch ? "CHOICE BRANCH" : "NEXT BEAT");
#endif
            }
        }
    }
}
