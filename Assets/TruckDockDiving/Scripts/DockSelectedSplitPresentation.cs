using System;
using System.Collections.Generic;
using UnityEngine;
using LWS.TruckSports;
using LWS.TruckBowling;
namespace LWS.TruckDockDiving
{
    // Mode-owned rendering only. Original DockSplitVisual supplies the amount;
    // existing NWH/Host wheel pivots remain the sole wheel-pose authority.
    public sealed class DockSelectedSplitPresentation : MonoBehaviour
    {
        public DockMatch match;
        public SportsTruckAppearanceHost host;
        public DockAppearanceSplitRecipe[] recipes = Array.Empty<DockAppearanceSplitRecipe>();
        public bool Applied { get; private set; }
        public Transform UpperRoot { get; private set; }
        public string Notice { get; private set; } = "";
        readonly List<GameObject> roots = new List<GameObject>();
        readonly Dictionary<Renderer, bool> hidden = new Dictionary<Renderer, bool>();
        public void PrepareOwner(BowlingPlayer owner, int slot)
        {
            Clear(); Notice = ""; host.participantSlot = slot; host.Prepare(owner.participantId, owner.color);
        }
        public void CompleteOwner()
        {
            if (!host.Applied) return;
            try
            {
                var body = host.catalog.FindBody(host.EffectiveBodyId); var recipe = Array.Find(recipes, item => item != null && item.bodyId == host.EffectiveBodyId);
                if (recipe == null || !recipe.ValidFor(body)) throw new InvalidOperationException("Selected body has no validated Dock upper/lower rendering.");
                var fit = Array.Find(host.compatibleBodies, item => item.bodyId == body.id); var matrix = Matrix4x4.TRS(fit.position, Quaternion.Euler(fit.euler), fit.scale) * Matrix4x4.Rotate(Quaternion.Euler(body.presentationEuler));
                for (int row = 0; row < 4; row++) for (int col = 0; col < 4; col++) if (Mathf.Abs(matrix[row, col] - recipe.canonicalToVehicle[row, col]) > 2e-4f) throw new InvalidOperationException("Dock fit changed after its private plane recipe was authored.");
                var upper = new GameObject("Dock selected upper body").transform; upper.SetParent(match.truck.visuals, false); roots.Add(upper.gameObject); UpperRoot = upper;
                var lower = new GameObject("Dock selected lower body").transform; lower.SetParent(match.truck.visuals, false); roots.Add(lower.gameObject);
                foreach (var part in recipe.parts)
                {
                    var source = host.ComposedBodyRenderers[part.rendererIndex]; if (!source.enabled && !hidden.ContainsKey(source)) continue;
                    Add(source, part.mesh, part.upper ? upper : lower, part.materialIndex, recipe.capMaterial);
                    if (!hidden.ContainsKey(source)) hidden.Add(source, source.enabled);
                }
                foreach (var renderer in host.AppearanceRoot.GetComponentsInChildren<Renderer>(true))
                {
                    if (!renderer.enabled || hidden.ContainsKey(renderer)) continue; var tag = renderer.GetComponentInParent<SportsTruckAppearanceMountTag>();
                    if (tag == null || tag.wheelIndex >= 0 || tag.slotId == SportsTruckAppearanceSlots.Wheels || tag.slotId == SportsTruckAppearanceSlots.RearOrnament) continue;
                    var filter = renderer.GetComponent<MeshFilter>(); if (filter == null || filter.sharedMesh == null) throw new InvalidOperationException("Unsupported upper cosmetic rendering.");
                    Add(renderer, filter.sharedMesh, upper, -1, null); hidden.Add(renderer, renderer.enabled);
                }
                foreach (var pair in hidden) pair.Key.enabled = false;
                match.split.BindPresentation(upper); Applied = true; Notice = "";
                foreach (var framing in match.appearanceFraming) if (framing != null) framing.RefreshAppearanceSubjects();
            }
            catch (Exception exception)
            {
                Clear(); host.RestoreOriginal(); match.truck.ApplyColor(match.setup.players[host.participantSlot].color); Notice = "Original Dock truck retained; selected split rendering is unavailable.";
                Debug.LogWarning(Notice + " " + exception.Message, this);
            }
        }
        void Add(Renderer source, Mesh mesh, Transform parent, int material, Material cap)
        {
            var item = new GameObject("Dock actual selected surface · " + source.name, typeof(MeshFilter), typeof(MeshRenderer)); item.layer = source.gameObject.layer; item.transform.SetParent(parent, false);
            SetLocal(item.transform, match.truck.visuals.worldToLocalMatrix * source.localToWorldMatrix); item.GetComponent<MeshFilter>().sharedMesh = mesh;
            var target = item.GetComponent<MeshRenderer>(); target.shadowCastingMode = source.shadowCastingMode; target.receiveShadows = source.receiveShadows;
            if (material < 0) target.sharedMaterials = source.sharedMaterials;
            else { var materials = new Material[mesh.subMeshCount]; materials[0] = source.sharedMaterials[material]; for (int i = 1; i < materials.Length; i++) materials[i] = cap; target.sharedMaterials = materials; }
            var block = new MaterialPropertyBlock(); source.GetPropertyBlock(block); target.SetPropertyBlock(block);
            for (int i = 0; i < target.sharedMaterials.Length; i++) { block.Clear(); if (material < 0) source.GetPropertyBlock(block, i); else if (i == 0) source.GetPropertyBlock(block, material); target.SetPropertyBlock(block.isEmpty ? null : block, i); }
        }
        public static void SetLocal(Transform target, Matrix4x4 matrix)
        {
            Vector3 position = matrix.GetColumn(3), right = matrix.GetColumn(0), up = matrix.GetColumn(1), forward = matrix.GetColumn(2); var scale = new Vector3(right.magnitude, up.magnitude, forward.magnitude);
            if (scale.x < 1e-7f || scale.y < 1e-7f || scale.z < 1e-7f) throw new InvalidOperationException("Degenerate Dock graphic transform.");
            if (matrix.determinant < 0) scale.x = -scale.x; var rotation = Quaternion.LookRotation(forward / scale.z, up / scale.y); var rebuilt = Matrix4x4.TRS(position, rotation, scale);
            for (int row = 0; row < 4; row++) for (int col = 0; col < 4; col++) if (!float.IsFinite(matrix[row, col]) || Mathf.Abs(matrix[row, col] - rebuilt[row, col]) > 2e-4f) throw new InvalidOperationException("Unsupported sheared Dock presentation transform.");
            target.localPosition = position; target.localRotation = rotation; target.localScale = scale;
        }
        void Clear()
        {
            if (match != null && match.split != null) match.split.BindPresentation(null);
            foreach (var pair in hidden) if (pair.Key != null) pair.Key.enabled = pair.Value; hidden.Clear();
            foreach (var root in roots) if (root != null) { root.SetActive(false); root.transform.SetParent(null, true); Destroy(root); } roots.Clear(); UpperRoot = null; Applied = false;
        }
        void OnDestroy() { Clear(); }
    }
}
