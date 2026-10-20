#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.IO;
using UnityEngine;
namespace LWS.TruckSports {
// Explicit acceptance evidence: players capture the composed framebuffer.
// Editor runs use labeled controlled camera/UI fixtures instead.
public static class SportsWindowCapture {
 public static string EvidenceKind=>Application.isEditor?"controlled Editor camera/UI fixture":"Windows composed framebuffer";
 public static IEnumerator Save(int width,int height,string path){
  if(Application.isEditor){var match=Object.FindAnyObjectByType<SportsEventSession>();if(match!=null)SportsControlledCapture.Save(match,width,height,path);else{var front=Object.FindAnyObjectByType<SportsFrontEnd>();var cameras=Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude);SportsControlledCapture.Save(front.GetComponent<Canvas>(),System.Array.FindAll(cameras,c=>c.enabled&&c.targetTexture==null),width,height,path);}yield return null;yield break;}
  if(!Application.isEditor){Screen.SetResolution(width,height,FullScreenMode.Windowed);for(int i=0;i<5;i++)yield return null;}
  for(int attempt=0;attempt<3;attempt++){
   yield return new WaitForEndOfFrame();
   var texture=ScreenCapture.CaptureScreenshotAsTexture();
   if(texture==null)throw new System.InvalidOperationException("Composed-frame capture unavailable: "+path);
   var pixels=texture.GetPixels32();bool visible=false;for(int i=0;i<pixels.Length;i+=97)if(pixels[i].r>4||pixels[i].g>4||pixels[i].b>4){visible=true;break;}
   if(visible){Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,texture.EncodeToPNG());Object.Destroy(texture);yield break;}
   Object.Destroy(texture);for(int frame=0;frame<5;frame++)yield return null;
  }
  throw new System.InvalidOperationException("Composed framebuffer remained black after resize/readiness retries: "+path);
 }
}
}
#endif
