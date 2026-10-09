using UnityEditor;

namespace LWS.TruckTaxi.Editor
{
    public sealed class TruckTaxiNeedsImageImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/LWS/TruckTaxi/Resources/TruckTaxi/Needs/", System.StringComparison.Ordinal) ||
                !(assetImporter is TextureImporter texture)) return;
            texture.textureType = TextureImporterType.Default;
            texture.alphaIsTransparency = true;
            texture.mipmapEnabled = false;
            texture.isReadable = false;
            texture.maxTextureSize = 512;
            texture.wrapMode = UnityEngine.TextureWrapMode.Clamp;
        }
    }
}
