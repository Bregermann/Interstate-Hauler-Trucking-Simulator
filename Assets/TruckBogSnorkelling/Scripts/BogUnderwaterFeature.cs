using UnityEngine;using UnityEngine.Rendering;using UnityEngine.Rendering.RenderGraphModule;using UnityEngine.Rendering.RenderGraphModule.Util;using UnityEngine.Rendering.Universal;
namespace LWS.TruckBogSnorkelling {
// The vendor water surface is retained; this fills its missing independent split-screen depth treatment.
public sealed class BogUnderwaterFeature:ScriptableRendererFeature {
 Material material;BogPass pass;
 public override void Create(){if(material!=null)CoreUtils.Destroy(material);var shader=Resources.Load<Shader>("TruckBog/BogUnderwater");if(shader==null||!shader.isSupported){Debug.LogError("Bog camera-local underwater shader missing or unsupported");return;}material=CoreUtils.CreateEngineMaterial(shader);pass=new BogPass(material);}
 public override void AddRenderPasses(ScriptableRenderer renderer,ref RenderingData data){var camera=data.cameraData.camera;if(!Application.isPlaying||material==null||camera.cameraType!=CameraType.Game||!camera.TryGetComponent<BogCameraEffects>(out var effects)||!effects.Evaluate(camera))return;renderer.EnqueuePass(pass);}
 protected override void Dispose(bool disposing){if(material!=null)CoreUtils.Destroy(material);material=null;pass=null;}
 sealed class BogPass:ScriptableRenderPass {
 readonly Material material;static readonly int Depth=Shader.PropertyToID("_CameraDepthTexture"),Source=Shader.PropertyToID("_BlitTexture");
 public BogPass(Material material){this.material=material;renderPassEvent=RenderPassEvent.AfterRenderingTransparents;requiresIntermediateTexture=true;ConfigureInput(ScriptableRenderPassInput.Depth);}
 public override void RecordRenderGraph(RenderGraph graph,ContextContainer frame){var camera=frame.Get<UniversalCameraData>().camera;if(!camera.TryGetComponent<BogCameraEffects>(out var effects)||!effects.Evaluate(camera))return;var resources=frame.Get<UniversalResourceData>();if(resources.isActiveTargetBackBuffer||!resources.activeColorTexture.IsValid()||!resources.cameraDepthTexture.IsValid())return;var source=resources.activeColorTexture;var descriptor=graph.GetTextureDesc(source);effects.TargetWidth=descriptor.width;effects.TargetHeight=descriptor.height;var rect=descriptor.width>camera.pixelWidth*1.25f||descriptor.height>camera.pixelHeight*1.25f?camera.rect:new Rect(0,0,1,1);effects.Properties.SetVector("_BogViewport",new Vector4(rect.x,rect.y,rect.width,rect.height));descriptor.name="Bog camera-local underwater color";descriptor.depthBufferBits=DepthBits.None;descriptor.clearBuffer=false;var destination=graph.CreateTexture(descriptor);var parameters=new RenderGraphUtils.BlitMaterialParameters(source,destination,material,0,effects.Properties,geometry:RenderGraphUtils.FullScreenGeometryType.ProceduralTriangle,sourceTexturePropertyID:Source);using(var builder=graph.AddBlitPass(parameters,"Bog independent underwater depth fog",returnBuilder:true))builder.UseGlobalTexture(Depth,AccessFlags.Read);resources.cameraColor=destination;effects.AppliedFrames++;}
 }
}
}
