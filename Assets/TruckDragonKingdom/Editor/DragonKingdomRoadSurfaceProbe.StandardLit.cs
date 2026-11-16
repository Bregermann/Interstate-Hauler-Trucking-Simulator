#if UNITY_EDITOR && VISTA
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRoadSurfaceProbe
    {
        [Serializable] public sealed class StandardLitCavernEvidence
        {
            public string scope = "REVERSIBLE_STANDARD_URP_SHADER_DIAGNOSTIC_NOT_PRODUCTION_CAVE_MATERIAL", shader;
            public Frame[] frames = Array.Empty<Frame>();
            public bool performed, stateRestored, temporaryMaterialsDestroyed, meshBindingsExact, meshPayloadExact;
            public bool propertyBlocksRestored, propertyBlocksProvenAbsent, originalMaterialsExact, rendererStateExact, physicalBindingsExact, cameraLightsLodsExact;
            public int rendererSlotsChanged, temporaryMaterials, originalMaterialCount;
            public string[] shaderErrors = Array.Empty<string>();
        }

        public static Material CreateStandardLitCavernDiagnosticMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null || !shader.isSupported) throw new InvalidOperationException("Actual supported standard URP/Lit shader required; no shader fallback.");
            var material = new Material(shader) { name = "Temporary neutral standard URP cavern diagnostic", hideFlags = HideFlags.HideAndDontSave };
            foreach (string property in material.GetTexturePropertyNames()) material.SetTexture(property, null);
            material.shaderKeywords = Array.Empty<string>();
            material.SetColor("_BaseColor", new Color(.55f, .55f, .55f, 1));
            material.SetColor("_EmissionColor", Color.black);
            foreach (string property in new[] { "_Metallic", "_Smoothness", "_Surface", "_Blend", "_AlphaClip", "_SpecularHighlights", "_EnvironmentReflections" })
                material.SetFloat(property, 0);
            material.SetFloat("_ZWrite", 1);
            material.SetFloat("_Cull", (float)CullMode.Back);
            material.SetOverrideTag("RenderType", "Opaque");
            material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            material.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            material.renderQueue = (int)RenderQueue.Geometry;
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            return material;
        }

        public static void RequireCavernStandardLitNoPropertyBlocks(MeshRenderer[] renderers)
        {
            var block = new MaterialPropertyBlock();
            foreach (var renderer in renderers)
            {
                if (renderer.HasPropertyBlock()) throw new InvalidOperationException("Nonempty actual renderer MaterialPropertyBlock would confound the standard-shader diagnostic: " + renderer.name);
                renderer.GetPropertyBlock(block);
                if (!block.isEmpty) throw new InvalidOperationException("Actual global renderer property block must be absent.");
                for (int slot = 0; slot < renderer.sharedMaterials.Length; slot++)
                {
                    block.Clear(); renderer.GetPropertyBlock(block, slot);
                    if (!block.isEmpty) throw new InvalidOperationException("Actual indexed renderer property block must be absent.");
                }
                block.Clear();
            }
        }

        static string StandardLitMeshPayload(Mesh mesh)
        {
            using (var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
            using (var data = Mesh.AcquireReadOnlyMeshData(mesh))
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(mesh.vertexCount); writer.Write(mesh.vertexBufferCount); writer.Write((int)mesh.indexFormat);
                writer.Write(mesh.subMeshCount); writer.Write(mesh.blendShapeCount);
                void Vector(Vector3 p) { writer.Write(p.x); writer.Write(p.y); writer.Write(p.z); }
                Vector(mesh.bounds.center); Vector(mesh.bounds.extents);
                foreach (var attribute in mesh.GetVertexAttributes())
                { writer.Write((int)attribute.attribute); writer.Write((int)attribute.format); writer.Write(attribute.dimension); writer.Write(attribute.stream); }
                for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
                {
                    var descriptor = mesh.GetSubMesh(submesh);
                    writer.Write((int)descriptor.topology); writer.Write(descriptor.indexStart); writer.Write(descriptor.indexCount);
                    writer.Write(descriptor.baseVertex); writer.Write(descriptor.firstVertex); writer.Write(descriptor.vertexCount);
                    Vector(descriptor.bounds.center); Vector(descriptor.bounds.extents);
                }
                writer.Flush(); hash.AppendData(stream.ToArray());
                for (int buffer = 0; buffer < mesh.vertexBufferCount; buffer++) hash.AppendData(data[0].GetVertexData<byte>(buffer).ToArray());
                hash.AppendData(data[0].GetIndexData<byte>().ToArray());
                return BitConverter.ToString(hash.GetHashAndReset()).Replace("-", "").ToLowerInvariant();
            }
        }

        static void CaptureCaveStandardLitDiagnostic(Camera camera, DragonKingdomWorld world, HashSet<Collider> roads, Shader validationShader, string output, MeshRenderer[] renderers, StandardLitCavernEvidence proof)
        {
            proof.performed = true;
            var bindings = renderers.ToDictionary(r => r, r => r.sharedMaterials);
            var originals = bindings.Values.SelectMany(v => v).Distinct().ToArray();
            if (originals.Length == 0 || originals.Any(m => m == null || !m.shader.name.StartsWith("Digger/Mesh/URP/Lit-Pass", StringComparison.Ordinal)))
                throw new InvalidOperationException("Actual complete persisted Digger shader/material slots required for this diagnostic.");
            proof.originalMaterialCount = originals.Length;
            string Pose(Transform t) => EditorJsonUtility.ToJson(t) + "/" + t.gameObject.activeSelf + "/" + t.gameObject.layer + "/" + (int)t.gameObject.hideFlags;
            var materialJson = originals.ToDictionary(m => m, m => EditorJsonUtility.ToJson(m));
            var rendererJson = renderers.ToDictionary(r => r, r => EditorJsonUtility.ToJson(r) + Pose(r.transform));
            var meshes = renderers.ToDictionary(r => r, r => r.GetComponent<MeshFilter>().sharedMesh);
            var meshPayload = meshes.Values.Distinct().ToDictionary(m => m, StandardLitMeshPayload);
            var physical = world.GetComponentsInChildren<MeshCollider>(true).ToDictionary(c => c, c => c.sharedMesh);
            var physicalJson = physical.Keys.ToDictionary(c => c, c => EditorJsonUtility.ToJson(c) + Pose(c.transform));
            var lodJson = world.GetComponentsInChildren<LODGroup>(true).ToDictionary(g => g, g => EditorJsonUtility.ToJson(g) + Pose(g.transform));
            var lights = world.gameObject.scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Light>(true)).ToDictionary(l => l, l => EditorJsonUtility.ToJson(l) + Pose(l.transform));
            string cameraJson = EditorJsonUtility.ToJson(camera) + Pose(camera.transform);
            var temporary = new Dictionary<Material, Material>(); var frames = new List<Frame>();
            bool Restored()
            {
                proof.originalMaterialsExact = materialJson.All(p => p.Key != null && p.Value == EditorJsonUtility.ToJson(p.Key));
                proof.rendererStateExact = rendererJson.All(p => p.Key != null && p.Value == EditorJsonUtility.ToJson(p.Key) + Pose(p.Key.transform) && p.Key.sharedMaterials.SequenceEqual(bindings[p.Key]));
                proof.meshBindingsExact = meshes.All(p => p.Key != null && p.Key.GetComponent<MeshFilter>().sharedMesh == p.Value);
                proof.meshPayloadExact = meshPayload.All(p => p.Key != null && p.Value == StandardLitMeshPayload(p.Key));
                proof.physicalBindingsExact = physical.All(p => p.Key != null && p.Key.sharedMesh == p.Value && physicalJson[p.Key] == EditorJsonUtility.ToJson(p.Key) + Pose(p.Key.transform));
                proof.cameraLightsLodsExact = cameraJson == EditorJsonUtility.ToJson(camera) + Pose(camera.transform) && lights.All(p => p.Key != null && p.Value == EditorJsonUtility.ToJson(p.Key) + Pose(p.Key.transform)) && lodJson.All(p => p.Key != null && p.Value == EditorJsonUtility.ToJson(p.Key) + Pose(p.Key.transform));
                RequireCavernStandardLitNoPropertyBlocks(renderers); proof.propertyBlocksRestored = true;
                proof.temporaryMaterialsDestroyed = temporary.Values.All(m => m == null);
                return proof.originalMaterialsExact && proof.rendererStateExact && proof.meshBindingsExact && proof.meshPayloadExact && proof.physicalBindingsExact && proof.cameraLightsLodsExact && proof.propertyBlocksRestored && proof.temporaryMaterialsDestroyed;
            }
            try
            {
                RequireCavernStandardLitNoPropertyBlocks(renderers); proof.propertyBlocksProvenAbsent = true;
                foreach (var original in originals) temporary.Add(original, CreateStandardLitCavernDiagnosticMaterial());
                proof.temporaryMaterials = temporary.Count; proof.shader = temporary.Values.First().shader.name;
                foreach (var entry in bindings) { entry.Key.sharedMaterials = entry.Value.Select(m => temporary[m]).ToArray(); proof.rendererSlotsChanged += entry.Value.Length; }
                frames.Add(Capture(camera, world, roads, validationShader, output, "rounded-entry-standard-lit-diagnostic", "standard-urp-lit-neutral")); proof.frames = frames.ToArray();
                proof.shaderErrors = ShaderUtil.GetShaderMessages(temporary.Values.First().shader).Where(m => m.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error).Select(m => m.message).ToArray();
                if (proof.shaderErrors.Length != 0 || frames[0].shaderErrors.Length != 0) throw new InvalidOperationException("Actual standard URP shader compilation errors in diagnostic.");
            }
            finally
            {
                foreach (var entry in bindings) if (entry.Key != null) entry.Key.sharedMaterials = entry.Value;
                foreach (var material in temporary.Values) if (material != null) UnityEngine.Object.DestroyImmediate(material);
                proof.stateRestored = Restored();
            }
            if (!proof.stateRestored) throw new InvalidOperationException("Standard URP shader pair did not restore exact original materials/geometry/physics/LOD/light/camera/property-block state.");
            frames.Add(Capture(camera, world, roads, validationShader, output, "rounded-entry-standard-lit-diagnostic", "original-digger-materials-restored")); proof.frames = frames.ToArray();
            proof.stateRestored &= Restored();
            if (!proof.stateRestored || frames[1].shaderErrors.Length != 0) throw new InvalidOperationException("Restored original Digger shader capture changed the saved state.");
        }
    }
}
#endif
