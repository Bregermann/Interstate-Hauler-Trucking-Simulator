using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace LWS.TruckTaxi
{
    // Add to the gameplay URP renderer. Only explicitly registered player cameras are affected.
    public sealed class TruckTaxiMushroomWaveFeature : ScriptableRendererFeature
    {
        [Range(.003f, .008f)] public float peakUvDisplacement = .005f;
        [Range(.2f, .7f)] public float waveHertz = .4f;
        [SerializeField] private Shader waveShader;
        private static readonly Dictionary<Camera, float> strengths = new Dictionary<Camera, float>();
        private Material material;
        private WavePass pass;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistrations() => strengths.Clear();

        public static void SetCameraStrength(Camera camera, float strength)
        {
            if (ReferenceEquals(camera, null)) return;
            if (strength <= 0) strengths.Remove(camera);
            else if (camera != null) strengths[camera] = Mathf.Clamp01(strength);
        }

        public void SetShader(Shader shader) => waveShader = shader;

        public override void Create()
        {
            if (material != null) CoreUtils.Destroy(material);
            var shader = waveShader != null ? waveShader : Shader.Find("Hidden/LWS/TruckTaxi/MushroomWorldWave");
            material = shader != null ? CoreUtils.CreateEngineMaterial(shader) : null;
            pass = new WavePass { renderPassEvent = RenderPassEvent.BeforeRenderingTransparents,
                requiresIntermediateTexture = true };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var cameraData = renderingData.cameraData;
            if (material == null || cameraData.cameraType != CameraType.Game ||
                cameraData.renderType != CameraRenderType.Base ||
                !strengths.TryGetValue(cameraData.camera, out float strength) || strength <= 0) return;
            pass.Setup(material, strength, peakUvDisplacement, waveHertz);
            renderer.EnqueuePass(pass);
        }

        protected override void Dispose(bool disposing)
        {
            if (material != null) CoreUtils.Destroy(material);
            material = null;
        }

        private sealed class WavePass : ScriptableRenderPass
        {
            private static readonly int Strength = Shader.PropertyToID("_Strength");
            private static readonly int Amplitude = Shader.PropertyToID("_UVAmplitude");
            private static readonly int Hertz = Shader.PropertyToID("_WaveHertz");
            private Material material;
            private float strength, amplitude, hertz;

            public void Setup(Material value, float intensity, float peak, float speed)
            { material = value; strength = intensity; amplitude = peak; hertz = speed; }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var resourceData = frameData.Get<UniversalResourceData>();
                if (material == null || resourceData.isActiveTargetBackBuffer ||
                    !resourceData.activeColorTexture.IsValid()) return;
                var source = resourceData.activeColorTexture;
                var desc = renderGraph.GetTextureDesc(source);
                desc.name = "Taxi Mushroom World Wave";
                desc.clearBuffer = false;
                var destination = renderGraph.CreateTexture(desc);
                material.SetFloat(Strength, strength);
                material.SetFloat(Amplitude, Mathf.Clamp(amplitude, .003f, .008f));
                material.SetFloat(Hertz, Mathf.Clamp(hertz, .2f, .7f));
                renderGraph.AddBlitPass(new RenderGraphUtils.BlitMaterialParameters(source, destination, material, 0),
                    "Taxi Mushroom World Wave");
                resourceData.cameraColor = destination;
            }
        }
    }
}
