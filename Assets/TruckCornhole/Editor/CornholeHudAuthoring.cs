using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using LWS.TruckSports;
namespace LWS.TruckCornhole.Editor
{
 public static class CornholeHudAuthoring
 {
  [MenuItem("Truck Sports/Cornhole/Install compact owned HUD in open scene")]
  public static void InstallOpenScene(){var match=UnityEngine.Object.FindAnyObjectByType<CornholeMatch>();if(match==null||match.gameObject.scene.path!=CornholeAuthoring.Scene)throw new InvalidOperationException("Open the existing Cornhole scene; this tool never opens or saves scenes.");Install(match);EditorSceneManager.MarkSceneDirty(match.gameObject.scene);Debug.Log("CORNHOLE_COMPACT_HUD_READY original owner/score/phase/replay/wind authorities retained; caller saves scene.");}
  static RectTransform Rect(Transform parent,string name,Vector2 size,Vector2 position)
  {
   var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.sizeDelta=size;rect.anchoredPosition=position;return rect;
  }
  static UnityEngine.UI.Image Image(Transform parent,string name,Vector2 size,Vector2 position){var rect=Rect(parent,name,size,position);var image=rect.gameObject.AddComponent<UnityEngine.UI.Image>();image.raycastTarget=false;return image;}
  static TMP_Text Text(Transform parent,string name,TMP_FontAsset font,Vector2 size,Vector2 position,int pixels)
  {
   var rect=Rect(parent,name,size,position);var text=rect.gameObject.AddComponent<TextMeshProUGUI>();text.font=font;text.fontSizeMax=pixels;text.fontSizeMin=pixels-1;text.enableAutoSizing=true;text.color=SportsBroadcastCanvas.Cream;text.alignment=TextAlignmentOptions.MidlineLeft;text.overflowMode=TextOverflowModes.Ellipsis;text.textWrappingMode=TextWrappingModes.NoWrap;text.raycastTarget=false;text.richText=false;return text;
  }
  static CornholeCompactHud.Card Card(Transform owner,int slot,SportsBroadcastTheme theme,Sprite portrait,bool small)
  {
   var plate=Image(owner,small?"Shared-view actual participant "+slot:"Compact Cornhole owner "+slot,new Vector2(small?310:318,small?43:94),Vector2.zero);plate.color=SportsBroadcastCanvas.Ink;plate.gameObject.AddComponent<SportsBroadcastPlate>().cut=9;var card=new CornholeCompactHud.Card{root=plate.rectTransform};
   card.stripe=Image(card.root,"Stable owner color",new Vector2(4,small?33:76),new Vector2(0,-8));card.portrait=Image(card.root,"Actual NWH truck portrait",new Vector2(small?28:39,small?28:39),new Vector2(10,small?-7:-34));card.portrait.sprite=portrait;card.portrait.preserveAspect=true;
   card.name=Text(card.root,"Actual participant identity",theme.headingFont,new Vector2(small?175:294,25),new Vector2(small?45:12,-3),small?19:22);card.name.fontStyle=FontStyles.Bold;
   card.score=Text(card.root,"Original banked score",theme.headingFont,new Vector2(small?79:112,29),new Vector2(small?223:59,small?-2:-31),small?21:25);card.score.fontStyle=FontStyles.Bold;
   card.context=Text(card.root,"Original round attempt and clock",theme.bodyFont,new Vector2(small?254:294,small?16:25),new Vector2(small?45:12,small?-25:-65),small?14:17);
   if(!small)card.delta=Text(card.root,"Separate original provisional or final round delta",theme.bodyFont,new Vector2(128,26),new Vector2(179,-33),18);
   return card;
  }
  public static CornholeCompactHud Install(CornholeMatch match)
  {
   if(match==null||match.gameObject.scene.path!=CornholeAuthoring.Scene)throw new ArgumentException("Original Cornhole only.");var existing=match.menus.GetComponent<CornholeCompactHud>();if(existing!=null){if(existing.cards?.Length!=4||existing.rosterRows?.Length!=4)throw new InvalidOperationException("Incomplete owned HUD requires review; existing custom hierarchy was preserved.");return existing;}
   var theme=Resources.Load<SportsBroadcastTheme>("SportsBroadcastTheme");var portrait=Resources.Load<Sprite>("Broadcast truck portrait");if(theme==null||theme.bodyFont==null||theme.headingFont==null||portrait==null)throw new InvalidOperationException("Existing Heat broadcast fonts and installed NWH portrait are required.");
   var cards=new CornholeCompactHud.Card[4];var rows=new CornholeCompactHud.Card[4];var created=new List<GameObject>();
   try
   {
    for(int i=0;i<4;i++){cards[i]=Card(match.menus.views[i],i,theme,portrait,false);created.Add(cards[i].root.gameObject);cards[i].root.gameObject.SetActive(false);}
    var roster=Rect(match.menus.transform,"Single-view original participant scores",new Vector2(310,172),new Vector2(-10,-10));created.Add(roster.gameObject);roster.anchorMin=roster.anchorMax=roster.pivot=Vector2.one;
    for(int i=0;i<4;i++){rows[i]=Card(roster,i,theme,portrait,true);rows[i].root.anchoredPosition=new Vector2(0,-i*43);}roster.gameObject.SetActive(false);
    var legacy=match.menus.headers.Concat(match.menus.footers).SelectMany(text=>new UnityEngine.UI.Graphic[]{text,text.transform.parent.GetComponent<UnityEngine.UI.Image>()}).Where(graphic=>graphic!=null).Distinct().ToArray();var enabled=legacy.Select(graphic=>graphic.enabled).ToArray();
    var wind=match.menus.GetComponent<CornholeWindDisplay>();if(wind==null||wind.arrows.Length!=4||wind.speeds.Length!=4)throw new InvalidOperationException("Existing original bottom-right wind display required; this tool does not replace its authority.");
    for(int i=0;i<4;i++)
    {
     var panel=wind.speeds[i].transform.parent.GetComponent<RectTransform>();panel.anchorMin=panel.anchorMax=panel.pivot=new Vector2(1,0);panel.anchoredPosition=new Vector2(-10,10);panel.sizeDelta=new Vector2(143,59);
     wind.arrows[i].anchorMin=wind.arrows[i].anchorMax=wind.arrows[i].pivot=new Vector2(.5f,.5f);wind.arrows[i].anchoredPosition=new Vector2(-50,0);wind.arrows[i].localScale=Vector3.one*.75f;
     var speed=wind.speeds[i];speed.font=theme.bodyFont;speed.fontSizeMax=19;speed.fontSizeMin=18;speed.enableAutoSizing=true;speed.rectTransform.anchorMin=speed.rectTransform.anchorMax=speed.rectTransform.pivot=new Vector2(.5f,.5f);speed.rectTransform.anchoredPosition=new Vector2(19,0);speed.rectTransform.sizeDelta=new Vector2(98,49);speed.raycastTarget=false;
     for(int b=0;b<5;b++){var button=match.menus.replayButtons[i*5+b].GetComponent<RectTransform>();button.anchorMin=button.anchorMax=button.pivot=Vector2.zero;button.anchoredPosition=new Vector2(10+b*135,10);button.sizeDelta=new Vector2(128,38);foreach(var text in button.GetComponentsInChildren<TMP_Text>(true)){text.fontSizeMax=20;text.fontSizeMin=18;text.enableAutoSizing=true;text.rectTransform.sizeDelta=new Vector2(118,34);}}
    }
    var hud=match.menus.gameObject.AddComponent<CornholeCompactHud>();hud.match=match;hud.cards=cards;hud.roster=roster;hud.rosterRows=rows;hud.headingFont=theme.headingFont;hud.bodyFont=theme.bodyFont;hud.legacyGraphics=legacy;hud.originalGraphicEnabled=enabled;foreach(var graphic in legacy)graphic.enabled=false;EditorUtility.SetDirty(hud);return hud;
   }
   catch{foreach(var go in created)if(go!=null)UnityEngine.Object.DestroyImmediate(go);throw;}
  }
 }
}
