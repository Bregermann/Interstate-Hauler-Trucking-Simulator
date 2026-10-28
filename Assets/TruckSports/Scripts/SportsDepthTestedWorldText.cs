using System.Collections.Generic;
using UnityEngine;

namespace LWS.TruckSports
{
    // Keep the authored legacy TextMesh geometry, glyphs and color authority.
    // The built-in GUI font ignores depth; these explicit NEW-scene instances
    // use a depth-tested atlas material without changing the shared font asset.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TextMesh), typeof(MeshRenderer))]
    public sealed class SportsDepthTestedWorldText : MonoBehaviour
    {
        public Shader worldTextShader;
        sealed class Binding { public Font font; public Material material; public int users; }
        static readonly Dictionary<Material, Binding> bindings = new Dictionary<Material, Binding>();
        MeshRenderer target;
        Material original, key;
        Binding binding;
        TextMesh text;
        MaterialPropertyBlock authoredBlock, tintedBlock;
        bool hadPropertyBlock;
        Color lastTextColor, lastFontColor;
        public static int SharedMaterialCount => bindings.Count;

        void OnEnable()
        {
            if (!Application.isPlaying || worldTextShader == null) return;
            text = GetComponent<TextMesh>(); target = GetComponent<MeshRenderer>();
            original = target.sharedMaterial;
            if (text.font == null || original == null || original.shader == null) return;
            // A cloned render diagnostic can already hold the same live atlas.
            // Custom non-GUI font materials remain designer-owned and untouched.
            if (original.shader.name != "GUI/Text Shader" && original.shader != worldTextShader) return;
            key = original.shader == worldTextShader ? text.font.material : original;
            if (!bindings.TryGetValue(key, out binding))
            {
                var material = new Material(worldTextShader) { name = key.name + " (depth tested world text)", hideFlags = HideFlags.DontSave };
                material.mainTexture = key.mainTexture;
                if (key.HasProperty("_Color")) material.SetColor("_Color", key.GetColor("_Color"));
                binding = new Binding { font = text.font, material = material };
                if (bindings.Count == 0) Font.textureRebuilt += TextureRebuilt;
                bindings.Add(key, binding);
            }
            binding.users++; target.sharedMaterial = binding.material;
            // The installed fixed-function URP font sample takes its primary
            // tint from _Color rather than TextMesh's vertex colors. Preserve
            // the authored per-label color through a renderer override while
            // retaining the one-material-per-atlas lifetime contract.
            hadPropertyBlock = target.HasPropertyBlock();
            authoredBlock = new MaterialPropertyBlock(); target.GetPropertyBlock(authoredBlock);
            tintedBlock = new MaterialPropertyBlock(); target.GetPropertyBlock(tintedBlock);
            ApplyTint(true);
        }
        void LateUpdate() { ApplyTint(false); }
        void ApplyTint(bool force)
        {
            if (binding == null || text == null || target == null || key == null || tintedBlock == null) return;
            var fontColor = key.HasProperty("_Color") ? key.GetColor("_Color") : Color.white;
            if (!force && text.color == lastTextColor && fontColor == lastFontColor) return;
            lastTextColor = text.color; lastFontColor = fontColor;
            tintedBlock.SetColor("_Color", lastTextColor * lastFontColor);
            target.SetPropertyBlock(tintedBlock);
        }
        static void TextureRebuilt(Font font)
        {
            foreach (var entry in bindings.Values)
                if (entry.font == font && entry.material != null) entry.material.mainTexture = font.material.mainTexture;
        }
        void OnDisable()
        {
            if (binding == null) return;
            if (target != null && target.sharedMaterial == binding.material)
            {
                target.sharedMaterial = original;
                target.SetPropertyBlock(hadPropertyBlock ? authoredBlock : null);
            }
            if (--binding.users == 0)
            {
                bindings.Remove(key);
                if (bindings.Count == 0) Font.textureRebuilt -= TextureRebuilt;
                Destroy(binding.material);
            }
            binding = null; original = key = null; target = null; text = null;
            authoredBlock = tintedBlock = null;
        }
    }
}
