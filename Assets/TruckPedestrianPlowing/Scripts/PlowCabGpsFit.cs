using UnityEngine;
namespace LWS.TruckPedestrianPlowing {
 // Same post-setup physical-screen fit used by Taxi; CNP may rebuild its Canvas on enable/resize.
 [DefaultExecutionOrder(400)]public sealed class PlowCabGpsFit:MonoBehaviour {
  public RectTransform screen;public RectTransform[] mapRects;
  public Vector3 position=new Vector3(-.10599f,.71074f,.38486f),normal=new Vector3(-.365503f,.168422f,-.915446f);
  public void Apply(){if(screen==null)return;screen.anchorMin=screen.anchorMax=screen.pivot=new Vector2(.5f,.5f);screen.sizeDelta=new Vector2(640,340);screen.localPosition=position+normal.normalized*.012f;screen.localRotation=Quaternion.LookRotation(-normal,Vector3.up);screen.localScale=Vector3.one*.00023f;foreach(var rect in mapRects){rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.sizeDelta=new Vector2(640,340);rect.anchoredPosition3D=Vector3.zero;rect.localScale=Vector3.one;}}
  void OnEnable(){Apply();}
  void LateUpdate(){if(screen==null)return;bool changed=screen.sizeDelta!=new Vector2(640,340)||(screen.localPosition-position-normal.normalized*.012f).sqrMagnitude>1e-8f;foreach(var rect in mapRects)if(rect!=null&&rect.sizeDelta!=new Vector2(640,340))changed=true;if(changed)Apply();}
 }
}
