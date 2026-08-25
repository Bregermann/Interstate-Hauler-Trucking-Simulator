using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DeadAir
{
    [RequireComponent(typeof(BoxCollider))]
    [DisallowMultipleComponent]
    public sealed class DeadAirValidRoadZone : MonoBehaviour
    {
        private static readonly List<DeadAirValidRoadZone> ActiveZonesInternal = new List<DeadAirValidRoadZone>();

        [SerializeField] private string zoneId = "DA_VALID_ROAD_ZONE";
        [SerializeField] private Color gizmoColor = new Color(0.15f, 0.95f, 0.45f, 0.22f);
        [SerializeField] private bool validForDepot = true;
        [SerializeField] private bool validForHighway = true;
        [SerializeField] private bool validForChoiceBranches = true;

        private BoxCollider _boxCollider;

        public static bool ShowRoadBoundaryGizmos { get; set; } = true;
        public static IReadOnlyList<DeadAirValidRoadZone> ActiveZones => ActiveZonesInternal;
        public string ZoneId => zoneId;
        public bool ValidForDepot => validForDepot;
        public bool ValidForHighway => validForHighway;
        public bool ValidForChoiceBranches => validForChoiceBranches;
        public bool RuntimeCandidate => !IsUnderConstructionKit();

        private void Reset()
        {
            EnsureCollider();
        }

        private void Awake()
        {
            EnsureCollider();
        }

        private void OnEnable()
        {
            if (!ActiveZonesInternal.Contains(this))
            {
                ActiveZonesInternal.Add(this);
            }
        }

        private void OnDisable()
        {
            ActiveZonesInternal.Remove(this);
        }

        public void Configure(string id, Vector3 size)
        {
            zoneId = string.IsNullOrWhiteSpace(id) ? zoneId : id;
            EnsureCollider();
            if (_boxCollider != null)
            {
                _boxCollider.size = size;
            }
        }

        public bool ContainsWorldPoint(Vector3 worldPoint)
        {
            EnsureCollider();
            if (_boxCollider == null)
            {
                return false;
            }

            Vector3 local = transform.InverseTransformPoint(worldPoint);
            Vector3 center = _boxCollider.center;
            Vector3 half = _boxCollider.size * 0.5f;
            Vector3 delta = local - center;
            return Mathf.Abs(delta.x) <= half.x &&
                   Mathf.Abs(delta.y) <= half.y &&
                   Mathf.Abs(delta.z) <= half.z;
        }

        private void EnsureCollider()
        {
            if (_boxCollider == null)
            {
                _boxCollider = GetComponent<BoxCollider>();
            }

            if (_boxCollider != null)
            {
                _boxCollider.isTrigger = true;
                if (_boxCollider.size == Vector3.zero)
                {
                    _boxCollider.size = new Vector3(22f, 10f, 80f);
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (!ShowRoadBoundaryGizmos || !DeadAirTriggerZone.ShowDeadAirGizmos)
            {
                return;
            }

            EnsureCollider();
            if (_boxCollider == null)
            {
                return;
            }

            Matrix4x4 previous = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = gizmoColor;
            Gizmos.DrawCube(_boxCollider.center, _boxCollider.size);
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.92f);
            Gizmos.DrawWireCube(_boxCollider.center, _boxCollider.size);
            Gizmos.matrix = previous;

#if UNITY_EDITOR
            Handles.color = new Color(0.7f, 1f, 0.75f, 1f);
            Handles.Label(transform.position + Vector3.up * 2.5f, $"{zoneId}\n{(RuntimeCandidate ? "VALID DEAD AIR ROAD" : "TEMPLATE - DUPLICATE TO USE")}");
#endif
        }

        private bool IsUnderConstructionKit()
        {
            Transform current = transform;
            while (current != null)
            {
                if (current.name == DeadAirBeatLayoutUtility.ConstructionKitRootName)
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }
    }
}
