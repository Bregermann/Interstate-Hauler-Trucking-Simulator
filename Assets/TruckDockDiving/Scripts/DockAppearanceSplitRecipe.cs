using System;
using UnityEngine;
using LWS.TruckSports;
namespace LWS.TruckDockDiving
{
    // Private derived rendering, never physics or a replacement pose recorder.
    public sealed class DockAppearanceSplitRecipe : ScriptableObject
    {
        [Serializable] public sealed class Part { public int rendererIndex, materialIndex, sourceMaterialCount; public bool upper; public Mesh sourceMesh, mesh; }
        public int version = 1;
        public string bodyId, sourceDependencyHash;
        public GameObject sourcePrefab;
        [Tooltip("Artist-authored cut height in the original vehicle-local art frame; no physics effect.")] public float vehiclePlaneY = .65f;
        public Matrix4x4 canonicalToVehicle = Matrix4x4.identity;
        public Material capMaterial;
        public Part[] parts = Array.Empty<Part>();
        public bool ValidFor(SportsTruckAppearanceCatalog.Body body)
        {
            if (version != 1 || body == null || body.id != bodyId || body.visualPrefab != sourcePrefab || capMaterial == null || parts == null || parts.Length == 0) return false;
            var renderers = sourcePrefab.GetComponentsInChildren<Renderer>(true); bool top = false, bottom = false;
            foreach (var part in parts)
            {
                if (part == null || part.rendererIndex < 0 || part.rendererIndex >= renderers.Length || part.mesh == null || !part.mesh.isReadable || part.mesh.vertexCount < 3 || part.mesh.subMeshCount > 2 || part.materialIndex < 0 || part.materialIndex >= part.sourceMaterialCount) return false;
                var source = renderers[part.rendererIndex].GetComponent<MeshFilter>();
                if (source == null || source.sharedMesh != part.sourceMesh || renderers[part.rendererIndex].sharedMaterials.Length != part.sourceMaterialCount) return false;
                top |= part.upper; bottom |= !part.upper;
            }
            return top && bottom;
        }
    }
}
