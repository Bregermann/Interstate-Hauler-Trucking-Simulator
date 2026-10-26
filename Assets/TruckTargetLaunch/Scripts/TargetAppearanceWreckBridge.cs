using System;
using System.Collections.Generic;
using UnityEngine;
using LWS.TruckSports;
using LWS.TruckTerrierRacing;

namespace LWS.TruckTargetLaunch
{
    // Selected-body rendering on the ORIGINAL Terrier pieces. No physics pool,
    // force, lifetime, scoring, contact or replay clock is owned here.
    public sealed class TargetAppearanceWreckBridge : MonoBehaviour
    {
        public TargetMatch match;
        public SportsTruckFragmentRecipe[] recipes = Array.Empty<SportsTruckFragmentRecipe>();
        public int ActualEmissions { get; private set; }
        public int PoolBuilds { get; private set; }
        public string LastFallback { get; private set; } = "";
        public string LastEmittedBodyId { get; private set; } = "";
        readonly Owner[] owners = { new Owner(), new Owner(), new Owner(), new Owner() };
        readonly int[] replaySources = { 0, 1, 2, 3, 5, 6, 7, 8 };
        sealed class Owner
        {
            public int revision = -1;
            public readonly List<Graphic> graphics = new List<Graphic>();
            public readonly List<GameObject> roots = new List<GameObject>();
        }
        sealed class Graphic
        {
            public Renderer source, target;
            public TerrierWreckPool.Piece piece;
            public Mesh mesh, input;
            public Matrix4x4 bind;
            public int material;
            public Material cap;
        }
        TerrierWreckPool Pool => match.volcano.wrecks;
        public void PrepareRound()
        {
            ActualEmissions = 0; LastFallback = LastEmittedBodyId = "";
            for (int slot = 0; slot < 4; slot++)
            {
                var host = match.racers[slot].truck.appearanceHost;
                if (slot >= match.setup.count || !match.racers[slot].gameObject.activeSelf || host == null || !host.Applied) { Clear(slot); continue; }
                try
                {
                    if (owners[slot].revision != host.AppearanceRevision) Build(slot, host);
                    foreach (var graphic in owners[slot].graphics) { graphic.target.gameObject.SetActive(true); Pose(graphic); CopyColor(graphic); }
                    foreach (var piece in Pool.pieces) if (piece.owner == slot) piece.renderer.enabled = false;
                }
                catch (Exception exception)
                {
                    // At this stopped fresh-record boundary only. Removing a
                    // body hierarchy during recording would invalidate its poses.
                    Clear(slot); host.UseOriginalForUnavailableFragments(); LastFallback = exception.Message;
                    Debug.LogWarning("Target selected-body wreck rendering unavailable; original compatible truck retained: " + exception.Message, this);
                }
            }
            if (!match.replay.ResetFreshAppearanceRecord()) throw new InvalidOperationException("Stop Target review before preparing a fresh appearance record.");
            if (!match.replay.RefreshAppearanceSources(replaySources, false) && !match.replay.RefreshAppearanceSources(replaySources, true)) throw new InvalidOperationException("Original Target nine-source render hierarchy could not be refreshed.");
            foreach (var framing in match.appearanceFraming) if (framing != null) framing.RefreshAppearanceSubjects();
        }
        void Build(int slot, SportsTruckAppearanceHost host)
        {
            Clear(slot); var body = host.catalog.FindBody(host.EffectiveBodyId);
            var recipe = Array.Find(recipes, item => item != null && item.bodyId == host.EffectiveBodyId);
            string reason = "";
            if (recipe == null || !recipe.ValidFor(body, out reason)) throw new InvalidOperationException(recipe == null ? "Missing private body recipe." : reason);
            var state = owners[slot]; var parents = new Dictionary<TerrierWreckPool.Piece, Transform>();
            foreach (var piece in Pool.pieces) if (piece.owner == slot)
            {
                var root = new GameObject("Target selected-body graphics"); root.transform.SetParent(piece.body.transform, false); state.roots.Add(root); parents.Add(piece, root.transform);
            }
            if (parents.Count != 12) throw new InvalidOperationException("Exactly twelve original physical pieces per owner required.");
            var used = new HashSet<Renderer>();
            foreach (var part in recipe.parts)
            {
                var renderer = host.ComposedBodyRenderers[part.rendererIndex]; if (!renderer.enabled) continue; used.Add(renderer);
                var piece = Piece(slot, SportsTruckFragmentRecipe.BodyRoles[part.region]); Add(state, renderer, part.mesh, piece, parents[piece], part.sourceMaterialIndex, recipe.capMaterial);
            }
            foreach (var renderer in host.AppearanceRoot.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.enabled || used.Contains(renderer)) continue;
                var tag = renderer.GetComponentInParent<SportsTruckAppearanceMountTag>(); var filter = renderer.GetComponent<MeshFilter>();
                if (tag == null || filter == null || filter.sharedMesh == null) throw new InvalidOperationException("Visible selected-body surface lacks an explicit semantic mapping.");
                string role = tag.wheelIndex >= 0 ? SportsTruckFragmentRecipe.WheelRoles[tag.wheelIndex] : tag.slotId == SportsTruckAppearanceSlots.Mirrors ? "mirror lft.001" : tag.slotId == SportsTruckAppearanceSlots.RearOrnament ? "Chassis" : "Cab";
                var piece = Piece(slot, role); Add(state, renderer, filter.sharedMesh, piece, parents[piece], -1, null);
            }
            if (state.graphics.Count == 0) throw new InvalidOperationException("Selected body has no fragment rendering.");
            state.revision = host.AppearanceRevision; PoolBuilds++;
        }
        TerrierWreckPool.Piece Piece(int slot, string role)
        {
            TerrierWreckPool.Piece found = null;
            foreach (var piece in Pool.pieces) if (piece.owner == slot && piece.source.name == role) { if (found != null) throw new InvalidOperationException("Duplicate original physical role."); found = piece; }
            return found ?? throw new InvalidOperationException("Missing original physical role: " + role);
        }
        static Matrix4x4 Matrix(Graphic graphic)
        {
            // Exact world TRS assigned by unchanged TerrierWreckPool.Emit, not
            // the inactive piece's old pose/scale from a previous round.
            var source = graphic.piece.source;
            return Matrix4x4.TRS(source.position, source.rotation, source.lossyScale).inverse * graphic.source.localToWorldMatrix;
        }
        void Add(Owner state, Renderer source, Mesh input, TerrierWreckPool.Piece piece, Transform parent, int material, Material cap)
        {
            if (!(source is MeshRenderer) || input == null || !input.isReadable) throw new InvalidOperationException("Readable existing appearance geometry required.");
            var item = new GameObject("Target actual appearance surface · " + state.graphics.Count); item.layer = piece.renderer.gameObject.layer; item.transform.SetParent(parent, false);
            var graphic = new Graphic { source = source, target = item.AddComponent<MeshRenderer>(), piece = piece, material = material, cap = cap, input = input, mesh = Instantiate(input) };
            state.graphics.Add(graphic); graphic.bind = Matrix(graphic); Bake(graphic.mesh, graphic.bind); item.AddComponent<MeshFilter>().sharedMesh = graphic.mesh;
            graphic.target.shadowCastingMode = source.shadowCastingMode; graphic.target.receiveShadows = source.receiveShadows; CopyColor(graphic);
        }
        static void Bake(Mesh mesh, Matrix4x4 matrix)
        {
            float determinant = matrix.determinant;
            if (!float.IsFinite(determinant) || Mathf.Abs(determinant) < 1e-10f) throw new InvalidOperationException("Degenerate appearance-to-original-piece transform.");
            var vertices = mesh.vertices; for (int i = 0; i < vertices.Length; i++) vertices[i] = matrix.MultiplyPoint3x4(vertices[i]); mesh.vertices = vertices;
            var normals = mesh.normals; var normalMatrix = matrix.inverse.transpose;
            if (normals.Length == vertices.Length) { for (int i = 0; i < normals.Length; i++) normals[i] = normalMatrix.MultiplyVector(normals[i]).normalized; mesh.normals = normals; } else mesh.RecalculateNormals();
            var tangents = mesh.tangents;
            if (tangents.Length == vertices.Length) { for (int i = 0; i < tangents.Length; i++) { var old = tangents[i]; var direction = matrix.MultiplyVector(new Vector3(old.x, old.y, old.z)).normalized; tangents[i] = new Vector4(direction.x, direction.y, direction.z, old.w * (determinant < 0 ? -1 : 1)); } mesh.tangents = tangents; }
            if (determinant < 0) for (int sub = 0; sub < mesh.subMeshCount; sub++) { var indices = mesh.GetTriangles(sub); for (int i = 0; i < indices.Length; i += 3) { int swap = indices[i]; indices[i] = indices[i + 1]; indices[i + 1] = swap; } mesh.SetTriangles(indices, sub, false); }
            // The cloned recipe retains UV/color channels without resampling.
            mesh.RecalculateBounds();
        }
        static void Pose(Graphic graphic)
        {
            var delta = Matrix(graphic) * graphic.bind.inverse;
            Vector3 position = delta.GetColumn(3), right = delta.GetColumn(0), up = delta.GetColumn(1), forward = delta.GetColumn(2);
            var scale = new Vector3(right.magnitude, up.magnitude, forward.magnitude);
            if (scale.x < 1e-7f || scale.y < 1e-7f || scale.z < 1e-7f) throw new InvalidOperationException("Degenerate fragment render pose.");
            if (delta.determinant < 0) scale.x = -scale.x; var rotation = Quaternion.LookRotation(forward / scale.z, up / scale.y); var rebuilt = Matrix4x4.TRS(position, rotation, scale);
            for (int row = 0; row < 4; row++) for (int col = 0; col < 4; col++) if (!float.IsFinite(delta[row, col]) || Mathf.Abs(delta[row, col] - rebuilt[row, col]) > 2e-4f) throw new InvalidOperationException("Unsupported sheared cosmetic motion.");
            graphic.target.transform.localPosition = position; graphic.target.transform.localRotation = rotation; graphic.target.transform.localScale = scale;
        }
        static void CopyColor(Graphic graphic)
        {
            var materials = graphic.source.sharedMaterials;
            if (graphic.material < 0) graphic.target.sharedMaterials = materials;
            else { var assigned = new Material[graphic.mesh.subMeshCount]; assigned[0] = materials[graphic.material]; for (int i = 1; i < assigned.Length; i++) assigned[i] = graphic.cap; graphic.target.sharedMaterials = assigned; }
            var block = new MaterialPropertyBlock(); graphic.source.GetPropertyBlock(block); graphic.target.SetPropertyBlock(block);
            for (int i = 0; i < graphic.target.sharedMaterials.Length; i++) { block.Clear(); if (graphic.material < 0) graphic.source.GetPropertyBlock(block, i); else if (i == 0) graphic.source.GetPropertyBlock(block, graphic.material); graphic.target.SetPropertyBlock(block.isEmpty ? null : block, i); }
        }
        public bool BeforeEmit(int slot)
        {
            try
            {
                var host = match.racers[slot].truck.appearanceHost; var state = owners[slot];
                if (host == null || !host.Applied || state.revision != host.AppearanceRevision || state.graphics.Count == 0) return false;
                foreach (var graphic in state.graphics) { Pose(graphic); CopyColor(graphic); }
                return true;
            }
            catch (Exception exception) { LastFallback = exception.Message; Debug.LogWarning("Target original wreck physics continues with original rendering: " + exception.Message, this); return false; }
        }
        public void AfterEmit(int slot, bool selected)
        {
            foreach (var piece in Pool.pieces) if (piece.owner == slot) piece.renderer.enabled = !selected;
            foreach (var root in owners[slot].roots) root.SetActive(selected);
            // Rendering metadata only; this does not invalidate existing poses.
            if (!match.replay.RefreshAppearanceSources(new[] { 5 + slot }, false)) { LastFallback = "Target debris topology changed during its existing record."; Debug.LogWarning(LastFallback, this); }
            if (selected) { ActualEmissions++; LastEmittedBodyId = match.racers[slot].truck.appearanceHost.EffectiveBodyId; }
        }
        void Clear(int slot)
        {
            var state = owners[slot]; foreach (var graphic in state.graphics) if (graphic.mesh != null) Destroy(graphic.mesh); state.graphics.Clear();
            foreach (var root in state.roots) if (root != null) { root.SetActive(false); root.transform.SetParent(null, true); Destroy(root); } state.roots.Clear(); state.revision = -1;
            if (match != null && match.volcano != null && match.volcano.wrecks != null)
                foreach (var piece in Pool.pieces) if (piece.owner == slot && piece.renderer != null) piece.renderer.enabled = true;
        }
        void OnDestroy() { for (int slot = 0; slot < 4; slot++) Clear(slot); }
    }
}
