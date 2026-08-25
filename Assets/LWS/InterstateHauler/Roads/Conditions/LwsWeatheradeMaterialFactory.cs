using UnityEngine;

namespace LWS.InterstateHauler
{
    public enum LwsWeatheradeSurfaceMaterialMode
    {
        Rain,
        Snow
    }

    public static class LwsWeatheradeMaterialFactory
    {
        public const string RainCoverageShaderName = "NOT_Lonely/Weatherade/Rain Coverage";
        public const string SnowCoverageShaderName = "NOT_Lonely/Weatherade/Snow Coverage";

        public static bool RainCoverageShaderAvailable => Shader.Find(RainCoverageShaderName) != null;
        public static bool SnowCoverageShaderAvailable => Shader.Find(SnowCoverageShaderName) != null;

        public static Material CreateRoadSurfaceMaterial(string name, Color baseColor, LwsWeatheradeSurfaceMaterialMode mode = LwsWeatheradeSurfaceMaterialMode.Rain)
        {
            return TryCreateWeatheradeRoadSurfaceMaterial(name, baseColor, mode, out Material material)
                ? material
                : CreateFallbackLitMaterial(name, baseColor);
        }

        public static bool TryCreateWeatheradeRoadSurfaceMaterial(string name, Color baseColor, LwsWeatheradeSurfaceMaterialMode mode, out Material material)
        {
            Shader shader = ResolveWeatheradeShader(mode);
            if (shader == null)
            {
                material = null;
                return false;
            }

            material = new Material(shader) { name = name, color = baseColor };
            ConfigureSharedSurfaceProperties(material, baseColor);
            if (mode == LwsWeatheradeSurfaceMaterialMode.Snow)
            {
                ConfigureSnowCoverageDefaults(material);
            }
            else
            {
                ConfigureRainCoverageDefaults(material);
            }

            return true;
        }

        public static bool TryCreateWeatheradeRoadSurfaceMaterial(string name, Material sourceMaterial, Color fallbackColor, LwsWeatheradeSurfaceMaterialMode mode, out Material material)
        {
            Color baseColor = ResolveBaseColor(sourceMaterial, fallbackColor);
            if (!TryCreateWeatheradeRoadSurfaceMaterial(name, baseColor, mode, out material))
            {
                return false;
            }

            CopyTexture(sourceMaterial, material, "_BaseMap", "_BaseMap");
            CopyTexture(sourceMaterial, material, "_MainTex", "_BaseMap");
            return true;
        }

        public static bool IsWeatheradeCompatibleMaterial(Material material)
        {
            return IsWeatheradeMaterialForMode(material, LwsWeatheradeSurfaceMaterialMode.Rain) ||
                   IsWeatheradeMaterialForMode(material, LwsWeatheradeSurfaceMaterialMode.Snow);
        }

        public static bool IsWeatheradeMaterialForMode(Material material, LwsWeatheradeSurfaceMaterialMode mode)
        {
            return material != null && material.shader != null && material.shader.name == ResolveShaderName(mode);
        }

        public static Color ResolveBaseColor(Material sourceMaterial, Color fallbackColor)
        {
            if (sourceMaterial == null)
            {
                return fallbackColor;
            }

            if (sourceMaterial.HasProperty("_BaseColor"))
            {
                return sourceMaterial.GetColor("_BaseColor");
            }

            if (sourceMaterial.HasProperty("_Color"))
            {
                return sourceMaterial.GetColor("_Color");
            }

            return fallbackColor;
        }

        public static Material CreateFallbackLitMaterial(string name, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
            var material = new Material(shader) { name = name, color = color };
            ConfigureSharedSurfaceProperties(material, color);
            return material;
        }

        private static Shader ResolveWeatheradeShader(LwsWeatheradeSurfaceMaterialMode mode)
        {
            return Shader.Find(ResolveShaderName(mode));
        }

        private static string ResolveShaderName(LwsWeatheradeSurfaceMaterialMode mode)
        {
            return mode == LwsWeatheradeSurfaceMaterialMode.Snow ? SnowCoverageShaderName : RainCoverageShaderName;
        }

        private static void ConfigureSharedSurfaceProperties(Material material, Color baseColor)
        {
            if (material == null)
            {
                return;
            }

            SetColorIfPresent(material, "_BaseColor", baseColor);
            SetColorIfPresent(material, "_Color", baseColor);
            SetFloatIfPresent(material, "_Metallic", 0f);
            SetFloatIfPresent(material, "_Smoothness", 0.72f);
            SetFloatIfPresent(material, "_Coverage", 1f);
            SetFloatIfPresent(material, "_CoverageAreaMaskRange", 0.55f);
            SetFloatIfPresent(material, "_CoverageAreaBias", 0f);
            SetFloatIfPresent(material, "_CoverageLeakReduction", 0.25f);
            SetFloatIfPresent(material, "_CoverageAreaFalloffHardness", 0.75f);
        }

        private static void ConfigureRainCoverageDefaults(Material material)
        {
            material.EnableKeyword("_COVERAGE_ON");
            material.EnableKeyword("_RIPPLES_ON");
            material.EnableKeyword("_DRIPS_ON");
            SetFloatIfPresent(material, "_WetnessAmount", 0f);
            SetFloatIfPresent(material, "_PuddlesAmount", 0f);
            SetFloatIfPresent(material, "_RipplesAmount", 8f);
            SetFloatIfPresent(material, "_RipplesIntensity", 0f);
            SetFloatIfPresent(material, "_SpotsIntensity", 0f);
            SetFloatIfPresent(material, "_DripsIntensity", 0f);
        }

        private static void ConfigureSnowCoverageDefaults(Material material)
        {
            material.EnableKeyword("_COVERAGE_ON");
            SetFloatIfPresent(material, "_CoverageAmount", 0f);
            SetFloatIfPresent(material, "_CoverageHeight", 0.08f);
            SetFloatIfPresent(material, "_CoverageMaskRange", 0.55f);
            SetColorIfPresent(material, "_CoverageColor", new Color(0.88f, 0.92f, 0.95f, 1f));
        }

        private static void SetColorIfPresent(Material material, string propertyName, Color value)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetColor(propertyName, value);
            }
        }

        private static void SetFloatIfPresent(Material material, string propertyName, float value)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetFloat(propertyName, value);
            }
        }

        private static void CopyTexture(Material sourceMaterial, Material destinationMaterial, string sourceProperty, string destinationProperty)
        {
            if (sourceMaterial == null || destinationMaterial == null || !sourceMaterial.HasProperty(sourceProperty) || !destinationMaterial.HasProperty(destinationProperty))
            {
                return;
            }

            Texture texture = sourceMaterial.GetTexture(sourceProperty);
            if (texture != null)
            {
                destinationMaterial.SetTexture(destinationProperty, texture);
            }
        }
    }
}