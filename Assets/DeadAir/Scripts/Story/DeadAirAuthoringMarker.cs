using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DeadAir
{
    [DisallowMultipleComponent]
    public sealed class DeadAirAuthoringMarker : MonoBehaviour
    {
        [SerializeField] private string markerId = "DA_MARKER";
        [SerializeField, TextArea] private string notes = "Construction marker. Duplicate or move this marker into the playable route.";
        [SerializeField] private Color gizmoColor = new Color(1f, 0.85f, 0.2f, 0.8f);
        [SerializeField] private float radius = 2f;

        public string MarkerId => markerId;
        public string Notes => notes;

        public void Configure(string id, string noteText, Color color, float markerRadius)
        {
            markerId = string.IsNullOrWhiteSpace(id) ? markerId : id;
            notes = string.IsNullOrWhiteSpace(noteText) ? notes : noteText;
            gizmoColor = color;
            radius = Mathf.Max(0.1f, markerRadius);
        }

        private void OnDrawGizmos()
        {
            if (!DeadAirTriggerZone.ShowDeadAirGizmos)
            {
                return;
            }

            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.1f, radius));
#if UNITY_EDITOR
            Handles.color = gizmoColor;
            Handles.Label(transform.position + Vector3.up * Mathf.Max(1f, radius), $"{markerId}\n{notes}");
#endif
        }
    }
}
