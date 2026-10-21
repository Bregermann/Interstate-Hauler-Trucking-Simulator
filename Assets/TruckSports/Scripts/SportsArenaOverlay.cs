using System;
using UnityEngine;
namespace LWS.TruckSports {
// Screen-aligned information follows arena edges; it never owns input or gameplay state.
[DefaultExecutionOrder(200)]public sealed class SportsArenaOverlay:MonoBehaviour {
 [Serializable]public sealed class Dock {public RectTransform target,plate;public Vector2 relative,offset;public float width=.9f,height=40;}
 public Camera arenaCamera;public Bounds surface;public Dock[] docks;Canvas canvas;SportsBroadcastFraming broadcast;
 void Awake(){canvas=GetComponentInParent<Canvas>();if(arenaCamera!=null)broadcast=arenaCamera.GetComponent<SportsBroadcastFraming>();}
 void LateUpdate(){if(canvas==null||arenaCamera==null||docks==null)return;
 // The older fixed-rink HUD followed projected world edges. Those edges leave
 // the screen in an action close-up. Keep the same existing plates and content
 // inside this camera's viewport while the broadcast director owns its shot.
 var viewport=arenaCamera.rect;
 var rect=broadcast!=null&&broadcast.OwnsShot?new Rect(viewport.x+.02f*viewport.width,viewport.y+.13f*viewport.height,.96f*viewport.width,.70f*viewport.height):SportsCameraFit.Projected(arenaCamera,surface);
 float scale=Mathf.Max(.001f,canvas.scaleFactor);foreach(var dock in docks){if(dock.target==null||dock.plate==null)continue;dock.plate.gameObject.SetActive(dock.target.gameObject.activeSelf);var screen=new Vector2((rect.xMin+rect.width*dock.relative.x)*Screen.width,(rect.yMin+rect.height*dock.relative.y)*Screen.height);RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,screen,null,out var local);dock.plate.anchoredPosition=local+dock.offset;dock.plate.sizeDelta=new Vector2(Mathf.Min(1850,rect.width*Screen.width/scale*dock.width),dock.height);}}
 }
}
