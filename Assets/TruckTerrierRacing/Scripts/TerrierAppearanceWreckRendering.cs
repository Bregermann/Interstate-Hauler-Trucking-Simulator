using System;
using System.Collections.Generic;
using UnityEngine;
using LWS.TruckSports;
namespace LWS.TruckTerrierRacing
{
    public sealed partial class TerrierAppearanceBridge
    {
        sealed class Graphic
        {
            public Renderer source, target, ghost;
            public TerrierWreckPool.Piece piece;
            public Mesh mesh, input;
            public Matrix4x4 bind;
            public int material;
            public Material cap;
        }
        void BuildWrecks(int slot)
        {
            ClearWrecks(slot); var state = Owner(slot); var host = owners[slot].host; var body = host.catalog.FindBody(host.EffectiveBodyId);
            var recipe = Array.Find(recipes, item => item != null && item.bodyId == body.id); string reason = "";
            if (recipe == null || !recipe.ValidFor(body, out reason)) throw new InvalidOperationException(recipe == null ? "Missing private selected-body recipe." : reason);
            var parents = new Dictionary<TerrierWreckPool.Piece, Transform>(); var ghosts = new Dictionary<TerrierWreckPool.Piece, Transform>();
            foreach (var piece in match.wrecks.pieces) if (piece.owner == slot)
            {
                var live = new GameObject("Terrier selected actual wreck rendering"); live.transform.SetParent(piece.body.transform, false); state.roots.Add(live); parents.Add(piece, live.transform);
                var ghost = new GameObject("Terrier selected recorded wreck rendering"); ghost.transform.SetParent(piece.ghost, false); state.roots.Add(ghost); ghosts.Add(piece, ghost.transform);
            }
            if (parents.Count != 12) throw new InvalidOperationException("Exactly twelve original physical pieces per owner required.");
            var used = new HashSet<Renderer>();
            foreach (var part in recipe.parts)
            {
                var source = host.ComposedBodyRenderers[part.rendererIndex]; if (!source.enabled) continue; used.Add(source);
                var piece = Piece(slot, SportsTruckFragmentRecipe.BodyRoles[part.region]); Add(state, source, part.mesh, piece, parents[piece], ghosts[piece], part.sourceMaterialIndex, recipe.capMaterial);
            }
            foreach (var source in host.AppearanceRoot.GetComponentsInChildren<Renderer>(true))
            {
                if (!source.enabled || used.Contains(source)) continue; var tag = source.GetComponentInParent<SportsTruckAppearanceMountTag>(); var filter = source.GetComponent<MeshFilter>();
                if (tag == null || filter == null || filter.sharedMesh == null) throw new InvalidOperationException("Visible selected surface lacks an explicit semantic role.");
                string role = tag.wheelIndex >= 0 ? SportsTruckFragmentRecipe.WheelRoles[tag.wheelIndex] : tag.slotId == SportsTruckAppearanceSlots.Mirrors ? "mirror lft.001" : tag.slotId == SportsTruckAppearanceSlots.RearOrnament ? "Chassis" : "Cab";
                var piece = Piece(slot, role); Add(state, source, filter.sharedMesh, piece, parents[piece], ghosts[piece], -1, null);
            }
            if (state.graphics.Count == 0) throw new InvalidOperationException("Selected body has no actual wreck surfaces."); state.revision = host.AppearanceRevision; PoolBuilds++;
        }
        TerrierWreckPool.Piece Piece(int slot, string role)
        {
            TerrierWreckPool.Piece found = null;
            foreach (var piece in match.wrecks.pieces) if (piece.owner == slot && piece.source.name == role) { if (found != null) throw new InvalidOperationException("Duplicate original physical role."); found = piece; }
            return found != null ? found : throw new InvalidOperationException("Missing original physical role: " + role);
        }
        static Matrix4x4 Matrix(Graphic graphic)
        {
            var source = graphic.piece.source; return Matrix4x4.TRS(source.position, source.rotation, source.lossyScale).inverse * graphic.source.localToWorldMatrix;
        }
        void Add(State state, Renderer source, Mesh input, TerrierWreckPool.Piece piece, Transform parent, Transform ghostParent, int material, Material cap)
        {
            if (!(source is MeshRenderer) || input == null || !input.isReadable) throw new InvalidOperationException("Readable actual selected geometry required.");
            var live = new GameObject("Terrier actual selected surface " + state.graphics.Count); live.layer = piece.renderer.gameObject.layer; live.transform.SetParent(parent, false);
            var ghost = new GameObject("Terrier recorded selected surface " + state.graphics.Count); ghost.layer = piece.ghostRenderer.gameObject.layer; ghost.transform.SetParent(ghostParent, false);
            var graphic = new Graphic { source = source, target = live.AddComponent<MeshRenderer>(), ghost = ghost.AddComponent<MeshRenderer>(), piece = piece, input = input, mesh = Instantiate(input), material = material, cap = cap };
            state.graphics.Add(graphic); graphic.bind = Matrix(graphic); Bake(graphic.mesh, graphic.bind); live.AddComponent<MeshFilter>().sharedMesh = graphic.mesh; ghost.AddComponent<MeshFilter>().sharedMesh = graphic.mesh;
            graphic.target.shadowCastingMode = graphic.ghost.shadowCastingMode = source.shadowCastingMode; graphic.target.receiveShadows = graphic.ghost.receiveShadows = source.receiveShadows; CopyGraphicMetadata(graphic);
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
        static void CopyGraphicMetadata(Graphic graphic)
        {
            foreach (var target in new[] { graphic.target, graphic.ghost })
            {
                if (target == null || graphic.source == null) continue;
                var materials = graphic.source.sharedMaterials;
                if (graphic.material < 0) target.sharedMaterials = materials;
                else { var assigned = new Material[graphic.mesh.subMeshCount]; assigned[0] = materials[graphic.material]; for (int index = 1; index < assigned.Length; index++) assigned[index] = graphic.cap; target.sharedMaterials = assigned; }
                var block = new MaterialPropertyBlock(); graphic.source.GetPropertyBlock(block); target.SetPropertyBlock(block);
                for (int material = 0; material < target.sharedMaterials.Length; material++) { block.Clear(); if (graphic.material < 0) graphic.source.GetPropertyBlock(block, material); else if (material == 0) graphic.source.GetPropertyBlock(block, graphic.material); target.SetPropertyBlock(block.isEmpty ? null : block, material); }
            }
        }
        public bool BeforeEmit(int slot)
        {
            try
            {
                var state = Owner(slot); var host = owners[slot].host;
                if (!host.Applied || state.revision != host.AppearanceRevision || state.graphics.Count == 0) return false;
                foreach (var graphic in state.graphics)
                {
                    Pose(graphic); CopyGraphicMetadata(graphic);
                    // One original emission per owner/heat: the existing debris
                    // recorder retains root TRS; this fixed render bind belongs
                    // to that one event, not another time-series backend.
                    graphic.ghost.transform.localPosition = graphic.target.transform.localPosition; graphic.ghost.transform.localRotation = graphic.target.transform.localRotation; graphic.ghost.transform.localScale = graphic.target.transform.localScale;
                }
                return true;
            }
            catch (Exception exception) { LastFallback = exception.Message; Debug.LogWarning("Terrier original wreck physics continues with original rendering: " + exception.Message, this); return false; }
        }
        public void AfterEmit(int slot, bool selected)
        {
            foreach (var piece in match.wrecks.pieces) if (piece.owner == slot) { piece.renderer.enabled = !selected; piece.ghostRenderer.enabled = !selected; }
            foreach (var root in Owner(slot).roots) root.SetActive(selected);
            if (selected) ActualEmissions++;
        }
        void ClearWrecks(int slot)
        {
            var state = Owner(slot); foreach (var graphic in state.graphics) if (graphic.mesh != null) Dispose(graphic.mesh); state.graphics.Clear();
            foreach (var root in state.roots) if (root != null) { root.SetActive(false); root.transform.SetParent(null, true); Dispose(root); } state.roots.Clear(); state.revision = -1;
            if (match != null && match.wrecks != null) foreach (var piece in match.wrecks.pieces) if (piece.owner == slot) { if (piece.renderer != null) piece.renderer.enabled = true; if (piece.ghostRenderer != null) piece.ghostRenderer.enabled = true; }
        }
    }
}
