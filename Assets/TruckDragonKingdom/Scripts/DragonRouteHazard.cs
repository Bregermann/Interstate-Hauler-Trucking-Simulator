using System;
using System.Collections.Generic;
using Assets.CoreCollapse.Scripts.Structural;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonKingdom
{
    // A rare, explicitly requested vendor collapse transaction. The graph host
    // validates all detours BEFORE calling Trigger. CoreCollapse creates actual
    // fragments; this owner captures those synchronous outputs for full reset.
    public sealed class DragonRouteHazard : MonoBehaviour, IDragonWorldResettable
    {
        public string id;
        public string[] blockedEdges = Array.Empty<string>();
        public GameObject intactPrefab;
        [Tooltip("Real initial collapse impulse in this hazard's local frame; bounded to12m/s.")]
        public Vector3 collapseVelocityChange;
        public Light warning;
        public bool Collapsed { get; private set; }
        public Bounds PhysicalBounds { get; private set; }
        GameObject intact;
        Collapser core;
        readonly HashSet<GameObject> priorRoots = new HashSet<GameObject>();
        readonly List<GameObject> fragments = new List<GameObject>();
        public void SetWarning(bool visible) { if (warning != null) warning.enabled = visible && !Collapsed; }
        public bool Trigger()
        {
            if (Collapsed || core == null || !core.gameObject.activeInHierarchy || core.HasCollapsed) return false;
            var scene = gameObject.scene;
            priorRoots.Clear(); foreach (var root in scene.GetRootGameObjects()) priorRoots.Add(root);
            core.ReceiveAllDamage();
            if (!core.HasCollapsed) return false;
            foreach (var root in scene.GetRootGameObjects())
                if (!priorRoots.Contains(root))
                {
                    // CollapserPrefrag has no spawned-object return value. Its
                    // synchronous new scene roots are owned by this transaction.
                    fragments.Add(root); root.transform.SetParent(transform, true);
                    foreach (var body in root.GetComponentsInChildren<Rigidbody>(true))
                    {
                        body.maxAngularVelocity = Mathf.Min(body.maxAngularVelocity, 12); body.maxLinearVelocity = 55;
                        if (!body.isKinematic) body.AddForce(transform.rotation * Vector3.ClampMagnitude(collapseVelocityChange, 12), ForceMode.VelocityChange);
                    }
                }
            Collapsed = true; SetWarning(false); return true;
        }
        public void ResetDragonMatch(int seed)
        {
            if (intactPrefab == null || intactPrefab.GetComponentInChildren<Collapser>(true) == null)
                throw new InvalidOperationException("Route hazard requires its actual authored CoreCollapse structure and fragment prefab: " + id);
            foreach (var debris in fragments) if (debris != null) { debris.SetActive(false); Destroy(debris); }
            fragments.Clear(); priorRoots.Clear();
            if (intact != null) { intact.SetActive(false); Destroy(intact); }
            intact = Instantiate(intactPrefab, transform); intact.transform.localPosition = Vector3.zero; intact.transform.localRotation = Quaternion.identity;
            core = intact.GetComponentInChildren<Collapser>(true); Collapsed = false; SetWarning(false);
            var renderers = intact.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException("Physical route hazard needs visible owned geometry: " + id);
            var bounds = renderers[0].bounds; foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            PhysicalBounds = bounds;
        }
    }
}
