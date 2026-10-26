#if UNITY_EDITOR
using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class SkeePerceptualPaletteGpuTests
{
    const string ShaderPath = "Assets/TruckSkeeBall/Rendering/SkeeArcadePixels.shader";
    const int Magnification = 4, Rows = 4;
    static readonly Color32[] Swatches = {
        new Color32(0,0,0,255), new Color32(8,8,8,255), new Color32(16,16,16,255),
        new Color32(24,24,24,255), new Color32(32,32,32,255),
        new Color32(24,18,12,255), // dark wood
        new Color32(16,20,24,255), // dark road
        new Color32(36,12,18,255), // dark truck paint
        new Color32(128,128,128,255), new Color32(255,255,255,255)
    };

    sealed class GpuPalette : IDisposable
    {
        public readonly Texture2D readback;
        readonly Texture2D source;
        readonly RenderTexture target;
        readonly Material material;
        readonly Mesh quad;
        readonly RenderTexture priorTarget;
        readonly bool priorSrgbWrite;
        public GpuPalette()
        {
            Assert.That(SystemInfo.graphicsDeviceType, Is.Not.EqualTo(GraphicsDeviceType.Null), "Actual GPU sampling is required; a null-graphics run cannot verify color-space output.");
            Assert.That(SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat), Is.True);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            Assert.That(shader, Is.Not.Null); Assert.That(shader.isSupported, Is.True);
            Assert.That(shader.name, Is.EqualTo("LWS/TruckSkeeBall/Arcade Pixels"));
            Assert.That(shader, Is.Not.SameAs(Shader.Find("UI/Default")), "Only the Skee-owned material is exercised.");
            priorTarget = RenderTexture.active; priorSrgbWrite = GL.sRGBWrite;
            source = new Texture2D(Swatches.Length, Rows, TextureFormat.RGBA32, false, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var texels = new Color32[Swatches.Length * Rows];
            for (int y = 0; y < Rows; y++) Array.Copy(Swatches, 0, texels, y * Swatches.Length, Swatches.Length);
            source.SetPixels32(texels); source.Apply(false, false);
            // Same default ARGB32/sRGB feed contract as SkeeArcadePixels. The
            // source data above is display-encoded, so native sampling must decode
            // it in a Linear project. Float-linear output avoids readback encoding.
            target = new RenderTexture(Swatches.Length * Magnification, Rows * Magnification, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear) { filterMode = FilterMode.Point, antiAliasing = 1 };
            target.Create(); material = new Material(shader); material.SetTexture("_MainTex", source); material.SetColor("_Color", Color.white);
            material.SetInt("unity_GUIZTestMode", (int)CompareFunction.Always);
            quad = new Mesh { vertices = new[] { new Vector3(-1,-1,0), new Vector3(1,-1,0), new Vector3(1,1,0), new Vector3(-1,1,0) },
                uv = new[] { new Vector2(0,0), new Vector2(1,0), new Vector2(1,1), new Vector2(0,1) },
                colors = new[] { Color.white, Color.white, Color.white, Color.white }, triangles = new[] { 0,1,2,0,2,3 } };
            var commands = new CommandBuffer { name = "Synthetic Skee native color-space palette regression" };
            try
            {
                commands.SetRenderTarget(target); commands.ClearRenderTarget(false, true, Color.clear);
                commands.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
                commands.DrawMesh(quad, Matrix4x4.identity, material, 0, 0);
                commands.SetViewProjectionMatrices(Shader.GetGlobalMatrix("unity_MatrixV"), Shader.GetGlobalMatrix("unity_MatrixP"));
                GL.sRGBWrite = false; Graphics.ExecuteCommandBuffer(commands);
                RenderTexture.active = target;
                readback = new Texture2D(target.width, target.height, TextureFormat.RGBAFloat, false, true);
                readback.ReadPixels(new Rect(0,0,target.width,target.height), 0, 0, false); readback.Apply(false, false);
                Assert.That(ShaderUtil.ShaderHasError(shader), Is.False, "The real owned shader must compile on the native graphics backend.");
            }
            catch { Dispose(); throw; }
            finally { commands.Release(); RenderTexture.active = priorTarget; GL.sRGBWrite = priorSrgbWrite; }
        }
        public Color At(int stripe, int row, int dx = 2, int dy = 2) => readback.GetPixel(stripe * Magnification + dx, row * Magnification + dy);
        public void Dispose()
        {
            RenderTexture.active = priorTarget; GL.sRGBWrite = priorSrgbWrite;
            if (target != null) { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
            if (readback != null) UnityEngine.Object.DestroyImmediate(readback);
            if (source != null) UnityEngine.Object.DestroyImmediate(source);
            if (material != null) UnityEngine.Object.DestroyImmediate(material);
            if (quad != null) UnityEngine.Object.DestroyImmediate(quad);
        }
    }

    static float DisplayToLinear(float value) => value <= .04045f ? value / 12.92f : Mathf.Pow((value + .055f) / 1.055f, 2.4f);
    static float Expected(byte value)
    {
        float displayLevel = Mathf.Floor(value / 255f * 31 + .5f) / 31;
        return QualitySettings.activeColorSpace == ColorSpace.Linear ? DisplayToLinear(displayLevel) : displayLevel;
    }
    static void Close(float actual, float expected, string context) => Assert.That(actual, Is.EqualTo(expected).Within(.0003f), context);
    static float Scanline(GpuPalette gpu, int row)
    {
        float white = gpu.At(Swatches.Length - 1, row).r;
        float factor = white < .97f ? .94f : 1;
        Close(white, factor, "Only the original .94/1 scanline modulation may affect white; no global brightness correction.");
        return factor;
    }

    [Test]
    public void ActualGpuKeepsDarkWoodRoadAndTruckPaletteDistinctInActiveColorSpace()
    {
        using (var gpu = new GpuPalette())
        {
            for (int row = 0; row < Rows; row++)
            {
                float scan = Scanline(gpu, row), previous = 0;
                for (int stripe = 1; stripe <= 4; stripe++)
                {
                    float actual = gpu.At(stripe, row).r;
                    Assert.That(actual, Is.GreaterThan(previous + .0005f), "Dark display swatches8/16/24/32 must remain distinct instead of collapsing into black."); previous = actual;
                }
                for (int stripe = 0; stripe < Swatches.Length; stripe++)
                {
                    var actual = gpu.At(stripe, row); var input = Swatches[stripe];
                    Close(actual.r, Expected(input.r) * scan, "Native red output; rejects missing or doubled sRGB encoding.");
                    Close(actual.g, Expected(input.g) * scan, "Native green output."); Close(actual.b, Expected(input.b) * scan, "Native blue output."); Close(actual.a, 1, "Original opaque feed.");
                }
                var wood = gpu.At(5, row); var road = gpu.At(6, row); var truck = gpu.At(7, row);
                Assert.That(Vector3.Distance(new Vector3(wood.r,wood.g,wood.b), new Vector3(road.r,road.g,road.b)), Is.GreaterThan(.001f));
                Assert.That(Vector3.Distance(new Vector3(wood.r,wood.g,wood.b), new Vector3(truck.r,truck.g,truck.b)), Is.GreaterThan(.001f));
            }
            if (QualitySettings.activeColorSpace == ColorSpace.Linear)
                for (int stripe = 1; stripe <= 4; stripe++) Assert.That(Mathf.Floor(DisplayToLinear(Swatches[stripe].r / 255f) * 31 + .5f), Is.Zero, "These real low-luminance inputs explicitly reproduce the former linear-quantization black crush.");
            TestContext.WriteLine("Synthetic GPU palette: sRGB texture sampling → actual Skee shader → float-linear readback, activeColorSpace=" + QualitySettings.activeColorSpace + "; no scene, camera ownership, physics, UI settings or pipeline changes.");
        }
    }

    [Test]
    public void ActualGpuPreservesPointBlocksBlackWhiteAndOriginalAlternatingScanlines()
    {
        using (var gpu = new GpuPalette())
        {
            float previous = Scanline(gpu, 0);
            for (int row = 0; row < Rows; row++)
            {
                float scan = Scanline(gpu, row); if (row > 0) Assert.That(Mathf.Abs(scan - previous), Is.EqualTo(.06f).Within(.0003f)); previous = scan;
                var black = gpu.At(0, row); Close(black.r, 0, "Black remains black."); Close(black.g, 0, "Black remains black."); Close(black.b, 0, "Black remains black.");
                for (int stripe = 0; stripe < Swatches.Length; stripe++)
                    for (int dx = 0; dx < Magnification; dx++) for (int dy = 0; dy < Magnification; dy++)
                    {
                        var center = gpu.At(stripe, row); var pixel = gpu.At(stripe, row, dx, dy);
                        Close(pixel.r, center.r, "Point pixel block must not blend adjacent palette stripes."); Close(pixel.g, center.g, "Point pixel block green."); Close(pixel.b, center.b, "Point pixel block blue.");
                    }
            }
        }
    }
}
#endif
