using UnityEngine;using UnityEngine.Rendering;
namespace LWS.TruckSports {
// Resolve pose before culling, so every viewport and child shadow use the same current-camera bounds.
public sealed class SportsPerCameraBillboard:MonoBehaviour {
 public int excludedLayer=-1;public bool distanceSizing;public float referenceDistance=30;public float maximumPixelHeight=26,maximumPixelWidth=144,maximumLabelDistance=250;Vector3 originalScale;Renderer labelRenderer;
 void OnEnable(){originalScale=transform.localScale;labelRenderer=GetComponent<Renderer>();RenderPipelineManager.beginCameraRendering+=BeforeRender;Camera.onPreCull+=BeforeCull;}
 void OnDisable(){RenderPipelineManager.beginCameraRendering-=BeforeRender;Camera.onPreCull-=BeforeCull;}
 void BeforeRender(ScriptableRenderContext context,Camera camera){Apply(camera);}void BeforeCull(Camera camera){Apply(camera);}
 void Apply(Camera camera){if(camera==null)return;transform.rotation=camera.transform.rotation;float distance=Vector3.Distance(camera.transform.position,transform.position);transform.localScale=originalScale*(distanceSizing?Mathf.Clamp(distance/referenceDistance,.15f,3):1);if(labelRenderer==null)return;if(distance>maximumLabelDistance){transform.localScale=Vector3.zero;return;}var bounds=labelRenderer.bounds;Vector2 min=Vector2.one*float.MaxValue,max=Vector2.one*float.MinValue;for(int i=0;i<8;i++){var p=bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));var viewport=camera.WorldToViewportPoint(p);if(viewport.z<=0)return;min=Vector2.Min(min,viewport);max=Vector2.Max(max,viewport);}var pixels=Vector2.Scale(max-min,new Vector2(camera.pixelWidth,camera.pixelHeight));float scale=Mathf.Min(1,Mathf.Min(maximumPixelWidth/Mathf.Max(1,pixels.x),maximumPixelHeight/Mathf.Max(1,pixels.y)));transform.localScale*=scale;}
}
}
