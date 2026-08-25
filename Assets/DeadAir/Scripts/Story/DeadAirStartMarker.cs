using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DeadAir
{
    [DisallowMultipleComponent]
    public sealed class DeadAirStartMarker : MonoBehaviour
    {
        public const string TruckStartReferenceName = "TRUCK_START_REFERENCE";
        public const string TrailerStartReferenceName = "TRAILER_START_REFERENCE";
        public const string ForwardDirectionName = "FORWARD_DIRECTION";

        [SerializeField] private float referenceSpeedMph = 55f;
        [SerializeField] private Transform truckStartReference;
        [SerializeField] private Transform trailerStartReference;
        [SerializeField] private Vector3 truckLocalPosition = Vector3.zero;
        [SerializeField] private Vector3 truckLocalEulerAngles = Vector3.zero;
        [SerializeField] private Vector3 trailerLocalPosition = new Vector3(0f, 0f, -13.5f);
        [SerializeField] private Vector3 trailerLocalEulerAngles = Vector3.zero;

        public float ReferenceSpeedMph => Mathf.Max(1f, referenceSpeedMph);
        public Transform TruckStartReference => truckStartReference;
        public Transform TrailerStartReference => trailerStartReference;
        public Vector3 TruckLocalPosition => truckStartReference != null ? transform.InverseTransformPoint(truckStartReference.position) : truckLocalPosition;
        public Vector3 TrailerLocalPosition => trailerStartReference != null ? transform.InverseTransformPoint(trailerStartReference.position) : trailerLocalPosition;

        public void ConfigureRigReferences(Transform truckReference, Transform trailerReference)
        {
            truckStartReference = truckReference;
            trailerStartReference = trailerReference;
        }

        public void ConfigureRigOffsets(Vector3 truckLocal, Vector3 trailerLocal)
        {
            truckLocalPosition = truckLocal;
            trailerLocalPosition = trailerLocal;
        }

        public void GetTruckPose(out Vector3 position, out Quaternion rotation)
        {
            if (truckStartReference != null)
            {
                position = truckStartReference.position;
                rotation = truckStartReference.rotation;
                return;
            }

            position = transform.TransformPoint(truckLocalPosition);
            rotation = transform.rotation * Quaternion.Euler(truckLocalEulerAngles);
        }

        public void GetTrailerPose(out Vector3 position, out Quaternion rotation)
        {
            if (trailerStartReference != null)
            {
                position = trailerStartReference.position;
                rotation = trailerStartReference.rotation;
                return;
            }

            position = transform.TransformPoint(trailerLocalPosition);
            rotation = transform.rotation * Quaternion.Euler(trailerLocalEulerAngles);
        }

        private void OnDrawGizmos()
        {
            if (!DeadAirTriggerZone.ShowDeadAirGizmos)
            {
                return;
            }

            Gizmos.color = new Color(0.2f, 1f, 0.5f, 0.95f);
            Gizmos.DrawWireSphere(transform.position, 3f);
            Gizmos.DrawRay(transform.position, transform.forward * 8f);
            DrawRigPreview();
#if UNITY_EDITOR
            Handles.color = Color.green;
            Handles.Label(transform.position + Vector3.up * 3f, $"DeadAirStartMarker\n[TRUCK]---[TRAILER]\nReference speed: {ReferenceSpeedMph:0} MPH");
#endif
        }

        private void DrawRigPreview()
        {
            GetTruckPose(out Vector3 truckPosition, out Quaternion truckRotation);
            GetTrailerPose(out Vector3 trailerPosition, out Quaternion trailerRotation);

            Matrix4x4 previousMatrix = Gizmos.matrix;
            Gizmos.color = new Color(0.1f, 0.8f, 1f, 0.75f);
            Gizmos.matrix = Matrix4x4.TRS(truckPosition + truckRotation * new Vector3(0f, 1.5f, 0f), truckRotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(3f, 3f, 6f));

            Gizmos.color = new Color(1f, 0.85f, 0.15f, 0.75f);
            Gizmos.matrix = Matrix4x4.TRS(trailerPosition + trailerRotation * new Vector3(0f, 1.8f, -2.5f), trailerRotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(3.2f, 3.6f, 12f));
            Gizmos.matrix = previousMatrix;

            Gizmos.color = new Color(0.2f, 1f, 0.5f, 0.85f);
            Gizmos.DrawLine(truckPosition, trailerPosition);
            Gizmos.DrawRay(truckPosition, truckRotation * Vector3.forward * 6f);
        }
    }
}
