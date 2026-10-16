using UnityEngine;using TMPro;using LWS.TruckSports;
namespace LWS.TruckBuzkashi {
// Six pooled screen-aligned captions; the broadcast camera never shrinks names into world-text pixels.
[DefaultExecutionOrder(220)]public sealed class BuzNameplates:MonoBehaviour {
 public BuzMatch match;public RectTransform root;public RectTransform[] plates;public TMP_Text[] captions;public int VisibleCount{get;private set;}
 readonly Rect[] placed=new Rect[6];readonly bool[] visible=new bool[6];float refreshAt;
 void LateUpdate(){bool active=match.Running&&match.menus.Screen==SportsEventScreen.Play;root.gameObject.SetActive(active);VisibleCount=0;if(!active)return;var camera=match.broadcast.view;float width=root.rect.width,height=root.rect.height;bool refresh=Time.unscaledTime>=refreshAt;if(refresh)refreshAt=Time.unscaledTime+.1f;for(int i=0;i<6;i++)visible[i]=false;
  for(int i=0;i<6;i++){var point=camera.WorldToViewportPoint(match.trucks[i].body.position+Vector3.up*3);bool show=point.z>camera.nearClipPlane&&point.x>.02f&&point.x<.98f&&point.y>.1f&&point.y<.9f;if(!show){plates[i].gameObject.SetActive(false);continue;}const float w=252,h=32;var pos=new Vector2(Mathf.Clamp(point.x*width,w*.5f+5,width-w*.5f-5),point.y*height+12);Rect rect=default;for(int attempt=0;attempt<7;attempt++){rect=new Rect(pos.x-w*.5f,pos.y-h*.5f,w,h);show=rect.yMax<height*.91f;for(int j=0;j<i&&show;j++)if(visible[j]&&rect.Overlaps(placed[j]))show=false;if(show)break;pos.y+=h+3;}plates[i].gameObject.SetActive(show);if(!show)continue;plates[i].anchoredPosition=pos;visible[i]=true;placed[i]=rect;VisibleCount++;if(refresh){string name=match.setup.players[i].name;captions[i].text=(name.Length>21?name.Substring(0,20)+"…":name)+(match.Owner==i?" ●":"");captions[i].color=match.setup.players[i].color;}}
 }
}
}
