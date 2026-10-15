using UnityEngine;using UnityEngine.Rendering;
namespace LWS.TruckSports {
// Resolve pose before culling, so every viewport and child shadow use the same current-camera bounds.
public sealed class SportsPerCameraBillboard:MonoBehaviour {
 public int excludedLayer=-1;public bool distanceSizing;public float referenceDistance=30;
 void OnEnable(){RenderPipelineManager.beginCameraRendering+=BeforeRender;Camera.onPreCull+=BeforeCull;}
 void OnDisable(){RenderPipelineManager.beginCameraRendering-=BeforeRender;Camera.onPreCull-=BeforeCull;}
 void BeforeRender(ScriptableRenderContext context,Camera camera){Apply(camera);}void BeforeCull(Camera camera){Apply(camera);}
 void Apply(Camera camera){if(camera==null)return;transform.rotation=camera.transform.rotation;if(distanceSizing)transform.localScale=Vector3.one*Mathf.Clamp(Vector3.Distance(camera.transform.position,transform.position)/referenceDistance,.15f,3);}
}
}
