using System;
using TMPro;
using UnityEngine;
namespace LWS.TruckVolleyball
{
 // Saved Heat/TMP broadcast view over the original two authoritative scores.
 [DefaultExecutionOrder(450)]public sealed class VolleyScorebug:MonoBehaviour
 {
  [Serializable]public sealed class TeamCell{public RectTransform root;public TMP_Text name,points;public UnityEngine.UI.Image color;[NonSerialized]public int previousPoints=-1;}
  [Serializable]public sealed class View{public RectTransform root;public TeamCell[] teams;}
  public VolleyMatch match;public RectTransform root;public View[] views;
  public int VisibleBugs{get;private set;}
  void LateUpdate()
  {
   bool playing=match!=null&&match.menus.Screen==LWS.TruckSports.SportsEventScreen.Play&&!match.ReplayControls;root.gameObject.SetActive(playing);VisibleBugs=0;if(!playing)return;
   for(int i=0;i<views.Length;i++)
   {
    var view=views[i];bool active=i<match.cameras.Length&&match.cameras[i]!=null&&match.cameras[i].enabled;view.root.gameObject.SetActive(active);if(!active)continue;VisibleBugs++;
    var viewport=match.cameras[i].rect;float width=viewport.width<.99f?360:420;view.root.anchorMin=view.root.anchorMax=new Vector2(viewport.xMin,viewport.yMax);view.root.pivot=new Vector2(0,1);view.root.anchoredPosition=new Vector2(12,-12);view.root.sizeDelta=new Vector2(width,48);
    for(int team=0;team<2;team++)
    {
     var cell=view.teams[team];cell.root.anchoredPosition=new Vector2(team*width*.5f,0);cell.root.sizeDelta=new Vector2(width*.5f,48);cell.name.text=match.TeamName(team);cell.name.color=cell.color.color=match.TeamColor(team);if(cell.points.text.Length==0||cell.previousPoints!=match.rules.points[team]){cell.previousPoints=match.rules.points[team];cell.points.text=cell.previousPoints.ToString();}cell.name.rectTransform.sizeDelta=new Vector2(width*.5f-82,36);cell.points.rectTransform.anchoredPosition=new Vector2(width*.5f-52,-4);
    }
   }
  }
 }
}
