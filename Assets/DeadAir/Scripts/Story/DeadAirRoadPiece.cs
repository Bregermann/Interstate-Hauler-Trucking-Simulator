using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DeadAir
{
    [DisallowMultipleComponent]
    public sealed class DeadAirRoadPiece : MonoBehaviour
    {
        [SerializeField] private DeadAirRoadPieceKind kind = DeadAirRoadPieceKind.Straight;
        [SerializeField] private Transform start;
        [SerializeField] private Transform end;
        [SerializeField, TextArea] private string notes = "Road construction template.";
        [SerializeField] private Color gizmoColor = new Color(0.2f, 0.9f, 1f, 0.8f);

        public DeadAirRoadPieceKind Kind => kind;
        public Transform Start => start;
        public Transform End => end;

        public void Configure(DeadAirRoadPieceKind roadKind, string noteText)
        {
            kind = roadKind;
            notes = noteText;
            EnsureEndpoints();
        }

        private void Reset()
        {
            EnsureEndpoints();
        }

        private void OnValidate()
        {
            EnsureEndpoints();
        }

        private void EnsureEndpoints()
        {
            start = EnsureChild("START_ANCHOR", new Vector3(0f, 0f, -8f));
            end = EnsureChild("END_ANCHOR", new Vector3(0f, 0f, 8f));
        }

        private Transform EnsureChild(string name, Vector3 localPosition)
        {
            Transform child = transform.Find(name);
            if (child == null)
            {
                child = new GameObject(name).transform;
                child.SetParent(transform, false);
            }

            child.localPosition = localPosition;
            return child;
        }

        private void OnDrawGizmos()
        {
            if (!DeadAirTriggerZone.ShowDeadAirGizmos)
            {
                return;
            }

            EnsureEndpoints();
            Gizmos.color = gizmoColor;
            Gizmos.DrawLine(start.position, end.position);
            Gizmos.DrawWireCube(transform.position, new Vector3(10f, 0.1f, 18f));
#if UNITY_EDITOR
            Handles.color = gizmoColor;
            Handles.Label(transform.position + Vector3.up * 1.5f, $"{kind}\n{notes}");
#endif
        }
    }
}
