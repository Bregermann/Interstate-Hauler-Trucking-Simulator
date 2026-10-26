using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;
using LWS.TruckSports;
using LWS.TruckSports.Editor;
namespace LWS.TruckVolleyball.Editor
{
 public static class VolleyScorebugAuthoring
 {
  public static void Install()
  {
   EditorSceneManager.OpenScene("Assets/TruckVolleyball/Scenes/TruckVolleyball.unity");var match=UnityEngine.Object.FindAnyObjectByType<VolleyMatch>();if(match.menus.GetComponent<VolleyScorebug>()!=null){Debug.Log("VOLLEY_SCOREBUG_ALREADY_AUTHORED");return;}
   var theme=Resources.Load<SportsBroadcastTheme>("SportsBroadcastTheme");if(theme==null||theme.headingFont==null||theme.bodyFont==null)throw new InvalidOperationException("Reuse the installed Heat broadcast theme.");
   var menus=match.menus;var hud=menus.gameObject.AddComponent<VolleyScorebug>();hud.match=match;hud.root=SportsUIAuthoring.Rect("Volley compact paired-team scorebug",menus.hud.transform,Vector2.zero,Vector2.zero).GetComponent<RectTransform>();SportsBroadcastCanvas.Stretch(hud.root);var alpha=hud.root.gameObject.AddComponent<CanvasGroup>();alpha.interactable=alpha.blocksRaycasts=false;hud.views=new VolleyScorebug.View[4];
   for(int view=0;view<4;view++)
   {
    var panel=Plate(hud.root,"Viewport "+view+" actual teams and points",new Vector2(420,48));var data=new VolleyScorebug.View{root=panel,teams=new VolleyScorebug.TeamCell[2]};hud.views[view]=data;
    for(int team=0;team<2;team++)
    {
     var cell=SportsUIAuthoring.Rect("Authoritative team "+team,panel,new Vector2(210,48),Vector2.zero).GetComponent<RectTransform>();Top(cell,Vector2.zero);var color=SportsUIAuthoring.Rect("Team color",cell,new Vector2(4,32),Vector2.zero).AddComponent<UnityEngine.UI.Image>();Top(color.rectTransform,new Vector2(8,-8));color.raycastTarget=false;
     data.teams[team]=new VolleyScorebug.TeamCell{root=cell,color=color,name=Label(cell,"Team identity",theme.headingFont,new Vector2(22,-6),new Vector2(128,36),23),points=Label(cell,"Actual points",theme.headingFont,new Vector2(158,-4),new Vector2(44,40),30)};data.teams[team].points.alignment=TextAlignmentOptions.Center;
    }
   }
   foreach(var graphic in new[]{menus.header,menus.status,menus.standings}.Concat(menus.viewTexts))graphic.enabled=false;
   foreach(var panel in menus.viewPanels){var backing=panel.Find("View data backing");if(backing!=null)backing.gameObject.SetActive(false);}
   var scores=menus.GetComponent<SportsBroadcastScores>()??menus.gameObject.AddComponent<SportsBroadcastScores>();scores.menus=menus;scores.enabled=false;foreach(var old in menus.GetComponents<SportsCompactViewportHud>())old.enabled=false;foreach(var old in match.GetComponents<SportsExpansionViewHud>())old.enabled=false;
   EditorUtility.SetDirty(hud);EditorSceneManager.MarkSceneDirty(match.gameObject.scene);EditorSceneManager.SaveScene(match.gameObject.scene);AssetDatabase.SaveAssets();Debug.Log("VOLLEY_SCOREBUG_AUTHORED existing Heat theme/TMP/plate; two actual team names/colors/points only;1shared or viewport-owned compact bugs; menu/score notification/results unchanged");
  }
  static RectTransform Plate(Transform parent,string name,Vector2 size){var rect=SportsUIAuthoring.Rect(name,parent,size,Vector2.zero).GetComponent<RectTransform>();var image=rect.gameObject.AddComponent<UnityEngine.UI.Image>();image.color=SportsBroadcastCanvas.Ink;image.raycastTarget=false;rect.gameObject.AddComponent<SportsBroadcastPlate>().cut=6;return rect;}
  static void Top(RectTransform rect,Vector2 at){rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.up;rect.anchoredPosition=at;}
  static TMP_Text Label(Transform parent,string name,TMP_FontAsset font,Vector2 at,Vector2 size,int height){var text=SportsUIAuthoring.Text(name,parent,"",size,Vector2.zero,height);text.font=font;text.richText=false;text.fontSizeMin=height-2;text.fontSizeMax=height;text.alignment=TextAlignmentOptions.MidlineLeft;text.overflowMode=TextOverflowModes.Ellipsis;Top(text.rectTransform,at);return text;}
 }
}
