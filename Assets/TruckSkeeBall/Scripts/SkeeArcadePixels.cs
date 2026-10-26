using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace LWS.TruckSkeeBall
{
 // Original participant cameras still own every world pose, cab, listener and
 // weather pass. A zero-culling output camera only presents their point-filtered
 // feeds behind the existing crisp HUD; no render-pipeline asset is modified.
 [DefaultExecutionOrder(1500)]
 public sealed class SkeeArcadePixels:MonoBehaviour
 {
  sealed class Feed
  {
   public Camera camera;public RenderTexture original,target;public RawImage image;
   public Rect viewport;public float originalAspect;public bool viewportDerivedAspect,fullRect;
  }
  public SkeeMatch match;public Shader arcadeShader;
  [Range(320,960)]public int virtualWidth=640;
  [Range(180,540)]public int virtualHeight=360;
  readonly Feed[] feeds=new Feed[4];Camera output;RectTransform images;Material material;
  public bool Active{get;private set;}
  public Camera OutputCamera=>output;
  public int FeedCount{get;private set;}
  public RenderTexture TextureFor(int view)=>view>=0&&view<FeedCount?feeds[view]?.target:null;
  public Rect ViewportFor(int view)=>feeds[view].viewport;
  void OnEnable(){RenderPipelineManager.beginContextRendering+=BeginContext;RenderPipelineManager.endCameraRendering+=EndCamera;RenderPipelineManager.endContextRendering+=EndContext;}
  void CreatePresentation()
  {
   if(images!=null)return;
   var canvas=match.menus.GetComponent<Canvas>();var root=new GameObject("Skee arcade world pixels behind crisp HUD",typeof(RectTransform));images=root.GetComponent<RectTransform>();images.SetParent(canvas.transform,false);images.SetAsFirstSibling();images.anchorMin=Vector2.zero;images.anchorMax=Vector2.one;images.offsetMin=images.offsetMax=Vector2.zero;
   if(arcadeShader!=null)material=new Material(arcadeShader){name="Skee-only arcade pixel presentation"};
   for(int i=0;i<4;i++){var go=new GameObject("Original lane pixel feed "+i,typeof(RectTransform),typeof(CanvasRenderer),typeof(RawImage));var image=go.GetComponent<RawImage>();image.transform.SetParent(images,false);image.raycastTarget=false;image.material=material;image.color=Color.white;feeds[i]=new Feed{image=image};go.SetActive(false);}
   // This camera presents the game backbuffer, not an Editor preview texture.
   // URP's preview-camera path requires a targetTexture and fails in the player.
   var cameraObject=new GameObject("Skee arcade OUTPUT ONLY - no world or weather",typeof(Camera));cameraObject.transform.SetParent(transform,false);output=cameraObject.GetComponent<Camera>();output.enabled=false;output.cameraType=CameraType.Game;output.depth=100;output.cullingMask=0;output.clearFlags=CameraClearFlags.SolidColor;output.backgroundColor=Color.black;output.allowHDR=false;output.allowMSAA=false;output.nearClipPlane=.1f;output.farClipPlane=1;
   var data=output.GetUniversalAdditionalCameraData();data.renderPostProcessing=false;data.renderShadows=false;data.volumeLayerMask=0;data.antialiasing=AntialiasingMode.None;data.requiresColorOption=CameraOverrideOption.Off;data.requiresDepthOption=CameraOverrideOption.Off;
  }
  void LateUpdate()
  {
   if(match==null||!match.Running||match.Finished){Restore();return;}
   CreatePresentation();Active=true;images.gameObject.SetActive(true);output.enabled=true;FeedCount=Mathf.Min(4,match.cameras.Length);
   for(int i=0;i<4;i++)
   {
    var feed=feeds[i];var camera=i<FeedCount?match.cameras[i]:null;
    if(camera==null||!camera.enabled){Release(feed);continue;}
    if(feed.camera!=camera)
    {
     Release(feed);feed.camera=camera;feed.original=camera.targetTexture;feed.originalAspect=camera.aspect;
     float pixels=camera.pixelRect.width/Mathf.Max(1,camera.pixelRect.height);
     feed.viewportDerivedAspect=Mathf.Abs(feed.originalAspect-pixels)<=Mathf.Max(1,pixels)*.0001f;
    }
    if(!feed.fullRect)feed.viewport=camera.rect;
    float reduction=Mathf.Min(Mathf.Clamp(virtualWidth,320,960)/(float)Mathf.Max(1,Screen.width),Mathf.Clamp(virtualHeight,180,540)/(float)Mathf.Max(1,Screen.height));
    int width=Mathf.Max(1,Mathf.RoundToInt(Screen.width*reduction*feed.viewport.width)),height=Mathf.Max(1,Mathf.RoundToInt(Screen.height*reduction*feed.viewport.height));
    if(feed.target==null||feed.target.width!=width||feed.target.height!=height)
    {
     if(feed.target!=null){camera.targetTexture=feed.original;feed.image.texture=null;feed.target.Release();Destroy(feed.target);}
     feed.target=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32){name="Skee lane "+i+" point-filtered world "+width+"x"+height,filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp,antiAliasing=1};feed.target.Create();feed.image.texture=feed.target;
    }
    camera.targetTexture=feed.target;RestoreOutsideAspect(feed);var rect=feed.image.rectTransform;rect.anchorMin=feed.viewport.min;rect.anchorMax=feed.viewport.max;rect.offsetMin=rect.offsetMax=Vector2.zero;feed.image.gameObject.SetActive(true);
   }
  }
  // URP snapshots Camera.rect before beginCameraRendering. The context callback
  // precedes that snapshot, so every small RT receives a whole image. Restore
  // the original split metadata before the next input/HUD/presentation update.
  void BeginContext(ScriptableRenderContext context,List<Camera> cameras)
  {
   if(!Active)return;foreach(var feed in feeds)if(feed?.camera!=null&&feed.camera.enabled&&cameras.Contains(feed.camera)&&feed.camera.targetTexture==feed.target){feed.camera.rect=new Rect(0,0,1,1);feed.camera.aspect=feed.viewportDerivedAspect?feed.target.width/(float)feed.target.height:feed.originalAspect;feed.fullRect=true;}
  }
  void EndContext(ScriptableRenderContext context,List<Camera> cameras){foreach(var feed in feeds)RestoreRect(feed);}
  void EndCamera(ScriptableRenderContext context,Camera camera){foreach(var feed in feeds)if(feed?.camera==camera)RestoreRect(feed);}
  static void RestoreOutsideAspect(Feed feed)
  {
   if(!feed.viewportDerivedAspect){feed.camera.aspect=feed.originalAspect;return;}
   // The original automatic projection belongs to the original screen/target viewport.
   // Resetting against the small feed RT can apply the split rectangle twice, so
   // keep this derived value while bound and restore true automatic mode on release.
   float width=feed.original!=null?feed.original.width:Screen.width,height=feed.original!=null?feed.original.height:Screen.height;
   feed.camera.aspect=Mathf.Max(1,width)*feed.viewport.width/Mathf.Max(.001f,Mathf.Max(1,height)*feed.viewport.height);
  }
  static void RestoreRect(Feed feed){if(feed?.camera!=null&&feed.fullRect){feed.camera.rect=feed.viewport;RestoreOutsideAspect(feed);feed.fullRect=false;}}
  void Release(Feed feed)
  {
   if(feed==null)return;RestoreRect(feed);if(feed.camera!=null){if(feed.camera.targetTexture==feed.target)feed.camera.targetTexture=feed.original;if(feed.viewportDerivedAspect)feed.camera.ResetAspect();else feed.camera.aspect=feed.originalAspect;}feed.camera=null;feed.original=null;if(feed.image!=null){feed.image.gameObject.SetActive(false);feed.image.texture=null;}if(feed.target!=null){feed.target.Release();Destroy(feed.target);feed.target=null;}
  }
  public void Restore(){foreach(var feed in feeds)Release(feed);Active=false;FeedCount=0;if(output!=null)output.enabled=false;if(images!=null)images.gameObject.SetActive(false);}
  void OnDisable(){RenderPipelineManager.beginContextRendering-=BeginContext;RenderPipelineManager.endCameraRendering-=EndCamera;RenderPipelineManager.endContextRendering-=EndContext;Restore();}
  void OnDestroy(){Restore();if(material!=null)Destroy(material);if(images!=null)Destroy(images.gameObject);if(output!=null)Destroy(output.gameObject);}
 }
}
