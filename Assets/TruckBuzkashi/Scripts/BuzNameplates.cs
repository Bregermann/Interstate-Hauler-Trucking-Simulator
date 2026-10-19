using UnityEngine;using TMPro;using LWS.TruckSports;
namespace LWS.TruckBuzkashi {
// Six pooled captions retain stable slot identity. Only changed names request
// preferred widths; camera projection and cluster placement remain allocation-free.
[DefaultExecutionOrder(220)]public sealed class BuzNameplates:MonoBehaviour {
 public BuzMatch match;public RectTransform root;public RectTransform[] plates;public TMP_Text[] captions;public GameObject[] carriers;public int VisibleCount{get;private set;}
 readonly Rect[] placed=new Rect[6];readonly bool[] visible=new bool[6];readonly float[] widths=new float[6];readonly string[] names=new string[6];float refreshAt;
 void LateUpdate(){bool active=match.option==0&&match.Running&&match.menus.Screen==SportsEventScreen.Play;root.gameObject.SetActive(active);VisibleCount=0;if(!active)return;var camera=match.broadcast.view;float width=root.rect.width,height=root.rect.height;bool refresh=Time.unscaledTime>=refreshAt;if(refresh)refreshAt=Time.unscaledTime+.1f;for(int i=0;i<6;i++)visible[i]=false;
  for(int i=0;i<6;i++){if(refresh||widths[i]==0){string name=match.setup.players[i].name;if(name!=names[i]){names[i]=name;captions[i].text=name;widths[i]=Mathf.Clamp(captions[i].GetPreferredValues(name).x+12,78,196);captions[i].rectTransform.sizeDelta=new Vector2(widths[i],26);}captions[i].color=match.setup.players[i].color;}
   bool carrier=match.Owner==i;if(carriers!=null&&i<carriers.Length&&carriers[i]!=null)carriers[i].SetActive(carrier);var point=camera.WorldToViewportPoint(match.trucks[i].body.position+Vector3.up*3);bool show=point.z>camera.nearClipPlane&&point.x>.02f&&point.x<.98f&&point.y>.1f&&point.y<.9f;if(!show){plates[i].gameObject.SetActive(false);continue;}float w=widths[i]+(carrier?20:0);const float h=26;var original=new Vector2(Mathf.Clamp(point.x*width,w*.5f+16,width-w*.5f-16),point.y*height+12);Vector2 position=original;Rect rect=default;
   for(int attempt=0;attempt<12;attempt++){int step=(attempt+1)/2;position=original+Vector2.up*(attempt==0?0:(attempt%2==1?step:-step)*(h+4));rect=new Rect(position.x-w*.5f,position.y-h*.5f,w,h);show=rect.yMin>height*.11f&&rect.yMax<height*.91f;for(int j=0;j<i&&show;j++)if(visible[j]&&rect.Overlaps(placed[j]))show=false;if(show)break;}
   plates[i].gameObject.SetActive(show);if(!show)continue;plates[i].sizeDelta=new Vector2(widths[i],h);plates[i].anchoredPosition=position;visible[i]=true;placed[i]=rect;VisibleCount++;
  }
 }
}
}
