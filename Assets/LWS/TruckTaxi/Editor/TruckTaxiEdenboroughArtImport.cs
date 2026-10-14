using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckTaxi.Editor
{
    // Import-time copies only; never changes vendor materials or existing scene art.
    public static class TruckTaxiEdenboroughArtImport
    {
        private const string Folder = TruckTaxiEdenboroughAuthoring.Folder + "/ReviewedArt";
        public static GameObject ReviewPrefab(GameObject source)
        {
            if (source == null || !EditorUtility.IsPersistent(source)) throw new ArgumentException("A persistent imported prefab is required.");
            var renderers = source.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException("Imported prefab has no geometry: " + AssetDatabase.GetAssetPath(source));
            if (renderers.Any(r => r.sharedMaterials.Any(m => m == null || m.shader == null || m.shader.name == "Hidden/InternalErrorShader")))
                throw new InvalidOperationException("Imported prefab has unresolved materials: " + AssetDatabase.GetAssetPath(source));
            if (!renderers.SelectMany(r => r.sharedMaterials).Any(IsLegacy)) return source;
            Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source));
            string path = Folder + "/" + source.name + "_" + guid.Substring(0, 8) + ".prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            var content = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(source));
            try
            {
                foreach (var renderer in content.GetComponentsInChildren<Renderer>(true))
                    renderer.sharedMaterials = renderer.sharedMaterials.Select(m => IsLegacy(m) ? CopyMaterial(m) : m).ToArray();
                return PrefabUtility.SaveAsPrefabAsset(content, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(content); }
        }
        private static bool IsLegacy(Material m) => m.shader.name.StartsWith("Standard", StringComparison.Ordinal) ||
            m.shader.name.StartsWith("Legacy Shaders/", StringComparison.Ordinal) || m.shader.name == "NOT_Lonely/MaskedPBR";
        private static Material CopyMaterial(Material source)
        {
            string path = Folder + "/" + source.name + "_" + AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source)).Substring(0, 8) + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = source.name + " Edenborough URP", enableInstancing = true };
            material.SetColor("_BaseColor", source.HasProperty("_Color") ? source.GetColor("_Color") : Color.white);
            CopyTexture(source, material, "_MainTex", "_BaseMap");
            CopyTexture(source, material, "_BumpMap", "_BumpMap");
            CopyTexture(source, material, "_MetallicGlossMap", "_MetallicGlossMap");
            CopyTexture(source, material, "_EmissionMap", "_EmissionMap");
            CopyTexture(source, material, "_Occlusion", "_OcclusionMap");
            if (material.GetTexture("_OcclusionMap") != null) material.EnableKeyword("_OCCLUSIONMAP");
            if (material.GetTexture("_BumpMap") != null) material.EnableKeyword("_NORMALMAP");
            if (material.GetTexture("_MetallicGlossMap") != null) material.EnableKeyword("_METALLICSPECGLOSSMAP");
            material.SetFloat("_Smoothness", source.HasProperty("_Glossiness") ? source.GetFloat("_Glossiness") : .3f);
            material.SetFloat("_Metallic", source.HasProperty("_Metallic") ? source.GetFloat("_Metallic") : 0);
            if (source.HasProperty("_EmissionColor"))
            {
                var emission = source.GetColor("_EmissionColor"); material.SetColor("_EmissionColor", emission);
                if (emission.maxColorComponent > 0) material.EnableKeyword("_EMISSION");
            }
            if (source.IsKeywordEnabled("_ALPHATEST_ON"))
            {
                material.SetFloat("_AlphaClip", 1); material.EnableKeyword("_ALPHATEST_ON");
                material.SetFloat("_Cutoff", source.HasProperty("_Cutoff") ? source.GetFloat("_Cutoff") : .5f);
                material.renderQueue = 2450;
            }
            else if (source.renderQueue >= 3000)
            {
                material.SetFloat("_Surface", 1); material.SetFloat("_ZWrite", 0);
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.SetOverrideTag("RenderType", "Transparent");
                material.renderQueue = 3000;
            }
            AssetDatabase.CreateAsset(material, path); return material;
        }
        private static void CopyTexture(Material source, Material target, string oldProperty, string newProperty)
        {
            if (!source.HasProperty(oldProperty)) return;
            target.SetTexture(newProperty, source.GetTexture(oldProperty));
            target.SetTextureScale(newProperty, source.GetTextureScale(oldProperty));
            target.SetTextureOffset(newProperty, source.GetTextureOffset(oldProperty));
        }
    }
}
