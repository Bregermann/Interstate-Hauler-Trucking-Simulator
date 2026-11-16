#if UNITY_EDITOR
using System;
using System.IO;
using LWS.TruckSports;
using LWS.TruckSports.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LWS.TruckDragonKingdom.Editor
{
    public static class DragonKingdomArtworkAuthoring
    {
        public static Sprite RequireLogo(string mode)
        {
            string path = SportsRetroMenuAuthoring.LogoFolder + mode + ".png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Original generated sport PNG is required: " + path);
            if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single
                || importer.mipmapEnabled || !importer.alphaIsTransparency || importer.wrapMode != TextureWrapMode.Clamp)
            {
                importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false; importer.alphaIsTransparency = true; importer.wrapMode = TextureWrapMode.Clamp;
                importer.maxTextureSize = 2048; importer.filterMode = FilterMode.Bilinear;
                importer.spritePixelsPerUnit = 100;
                importer.textureCompression = TextureImporterCompression.CompressedHQ; importer.SaveAndReimport();
            }
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) throw new InvalidOperationException("Actual imported logo sprite is missing: " + mode);
            return sprite;
        }
        public static void Install(SportsEventSession match, DragonKingdomWorld world, string mode)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || match == null || match.menus == null || world == null)
                throw new InvalidOperationException("Explicit stopped new-mode presentation authoring required.");
            var logo = RequireLogo(mode);
            var setup = match.menus.pages[(int)SportsEventScreen.Setup].transform;
            Picture(setup, "Original fantasy sport logo", logo, new Vector2(600, 120), new Vector2(0, 435));
            var title = setup.Find("Title"); if (title != null) title.gameObject.SetActive(false);
            foreach (var picker in setup.GetComponentsInChildren<SportsWeatherPicker>(true))
            {
                var rect = picker.GetComponent<RectTransform>(); rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, 307);
            }
            foreach (string name in new[] { "Weather selection label", "Time of day selection label" })
            {
                var label = setup.Find(name)?.GetComponent<RectTransform>();
                if (label != null) label.anchoredPosition = new Vector2(label.anchoredPosition.x, 351);
            }
            foreach (var page in new[] { SportsEventScreen.Help, SportsEventScreen.Result })
            {
                var parent = match.menus.pages[(int)page].transform;
                Picture(parent, "Original sport corner logo", logo, new Vector2(300, 120), new Vector2(530, 385));
                var label = parent.Find("Title")?.GetComponent<RectTransform>();
                if (label != null) { label.anchoredPosition = new Vector2(-210, 380); label.sizeDelta = new Vector2(850, 80); }
            }
            string directory = "Assets/" + mode + "/Materials"; Directory.CreateDirectory(directory);
            string materialPath = directory + "/Original kingdom venue branding.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) throw new InvalidOperationException("Installed URP unlit banner shader required.");
                material = new Material(shader) { name = mode + " painted venue banner", mainTexture = logo.texture, renderQueue = 3000 };
                material.SetFloat("_Surface", 1); material.SetFloat("_ZWrite", 0);
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); AssetDatabase.CreateAsset(material, materialPath);
            }
            foreach (string extraction in new[] { "fortress", "mountain", "boat" })
            {
                var site = world.Extraction(extraction);
                var sign = GameObject.CreatePrimitive(PrimitiveType.Quad); sign.name = mode + " original venue banner · " + extraction;
                UnityEngine.Object.DestroyImmediate(sign.GetComponent<Collider>());
                sign.transform.SetParent(site.approach, false); sign.transform.localPosition = new Vector3(0, 17, 3);
                sign.transform.localRotation = Quaternion.identity;
                sign.transform.localScale = new Vector3(24, 24 * logo.rect.height / logo.rect.width, 1);
                sign.GetComponent<Renderer>().sharedMaterial = material;
                sign.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }
        static void Picture(Transform parent, string name, Sprite sprite, Vector2 size, Vector2 position)
        {
            if (parent.Find(name) != null) throw new InvalidOperationException("Preserve previously authored logo presentation: " + name);
            var go = SportsUIAuthoring.Rect(name, parent, size, position); var image = go.AddComponent<Image>();
            image.sprite = sprite; image.preserveAspect = true; image.raycastTarget = false;
        }
    }
}
#endif
