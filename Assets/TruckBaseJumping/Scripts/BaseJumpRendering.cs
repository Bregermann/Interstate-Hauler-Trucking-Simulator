using UnityEngine;
using UnityEngine.Rendering;
namespace LWS.TruckBaseJumping {
[DefaultExecutionOrder(-200)]public sealed class BaseJumpRendering:MonoBehaviour {
 public RenderPipelineAsset pipeline;RenderPipelineAsset previousDefault,previousQuality;bool bound;
 void Awake(){previousDefault=GraphicsSettings.defaultRenderPipeline;previousQuality=QualitySettings.renderPipeline;GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;bound=true;}
 void OnDisable(){if(!bound)return;bound=false;if(GraphicsSettings.defaultRenderPipeline==pipeline)GraphicsSettings.defaultRenderPipeline=previousDefault;if(QualitySettings.renderPipeline==pipeline)QualitySettings.renderPipeline=previousQuality;}
}
}
