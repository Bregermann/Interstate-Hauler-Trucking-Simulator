using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;
using LWS.TruckSports;
using LWS.TruckSports.Editor;
namespace LWS.TruckHungryHaulers.Editor
{
 public static class HaulerCargoHudAuthoring
 {
  public static void InstallEssentialClocks()
  {
   EditorSceneManager.OpenScene("Assets/TruckHungryHaulers/Scenes/TruckHungryHaulers.unity");var match=UnityEngine.Object.FindAnyObjectByType<HaulerMatch>();var hud=match.menus.GetComponent<HaulerCargoHud>();
   if(hud==null)throw new InvalidOperationException("Install the saved compact Hauler HUD first.");
   // The installed heading font has a76.17/65 line-height ratio: even
   // the old27pt minimum needed31.64px, exceeding the31px value rect.
   foreach(var card in hud.cards)if(card.points.rectTransform.sizeDelta==new Vector2(58,31))card.points.rectTransform.sizeDelta=new Vector2(58,35);
   if(hud.clocks!=null&&hud.clocks.Length>0){if(hud.clocks.Length!=4)throw new InvalidOperationException("Preserve and inspect partial clock bindings.");return;}
   var theme=Resources.Load<SportsBroadcastTheme>("SportsBroadcastTheme");hud.clocks=new HaulerCargoHud.Clock[4];
   for(int i=0;i<4;i++)
   {
    var panel=Plate(hud.root,"Actual remaining match time and balls "+i,new Vector2(230,32));
    var text=Label(panel,"Actual time and remaining balls",theme.headingFont,new Vector2(10,-1),new Vector2(210,30),22);text.fontSizeMin=18;text.margin=Vector4.zero;text.alignment=TextAlignmentOptions.Center;
    hud.clocks[i]=new HaulerCargoHud.Clock{root=panel,text=text};
   }
   EditorUtility.SetDirty(hud);EditorSceneManager.MarkSceneDirty(match.gameObject.scene);EditorSceneManager.SaveScene(match.gameObject.scene);AssetDatabase.SaveAssets();Debug.Log("HAULER_COMPACT_ESSENTIAL_CLOCKS_SAVED original duration/remaining targets, one shared or per actual viewport; no instructions or score changes.");
  }
  public static void Install()
  {
   EditorSceneManager.OpenScene("Assets/TruckHungryHaulers/Scenes/TruckHungryHaulers.unity");var match=UnityEngine.Object.FindAnyObjectByType<HaulerMatch>();if(match.menus.GetComponent<HaulerCargoHud>()!=null){Debug.Log("HAULER_CARGO_HUD_ALREADY_AUTHORED");return;}
   var theme=Resources.Load<SportsBroadcastTheme>("SportsBroadcastTheme");var portrait=Resources.Load<Sprite>("Broadcast truck portrait");if(theme==null||theme.headingFont==null||theme.bodyFont==null||portrait==null)throw new InvalidOperationException("Reuse installed Heat broadcast fonts and owned NWH portrait.");
   var menus=match.menus;var hud=menus.gameObject.AddComponent<HaulerCargoHud>();hud.match=match;hud.root=SportsUIAuthoring.Rect("Hauler compact participant cargo HUD",menus.hud.transform,Vector2.zero,Vector2.zero).GetComponent<RectTransform>();SportsBroadcastCanvas.Stretch(hud.root);var alpha=hud.root.gameObject.AddComponent<CanvasGroup>();alpha.interactable=alpha.blocksRaycasts=false;hud.cards=new HaulerCargoHud.Card[4];
   for(int index=0;index<4;index++)
   {
    var panel=Plate(hud.root,"Owned hauler cargo card "+index,new Vector2(248,66));var stripe=SportsUIAuthoring.Rect("Participant color",panel,new Vector2(4,50),Vector2.zero).AddComponent<UnityEngine.UI.Image>();Top(stripe.rectTransform,new Vector2(0,-8));stripe.raycastTarget=false;
    var icon=SportsUIAuthoring.Rect("Owned NWH truck portrait",panel,new Vector2(36,36),Vector2.zero).AddComponent<UnityEngine.UI.Image>();Top(icon.rectTransform,new Vector2(10,-28));icon.sprite=portrait;icon.preserveAspect=true;icon.raycastTarget=false;
    var card=new HaulerCargoHud.Card{root=panel,color=stripe,portrait=icon,name=Label(panel,"Actual participant name",theme.headingFont,new Vector2(12,-3),new Vector2(224,24),21),points=Label(panel,"Actual banked points",theme.headingFont,new Vector2(51,-28),new Vector2(58,35),29),cargo=Label(panel,"Actual stored cargo",theme.bodyFont,new Vector2(117,-31),new Vector2(119,28),18)};
    card.promptRoot=Plate(panel,"Contextual dump only",new Vector2(248,28));card.prompt=Label(card.promptRoot,"Assigned dump action",theme.headingFont,new Vector2(9,-1),new Vector2(230,26),19);card.prompt.alignment=TextAlignmentOptions.Center;card.promptRoot.gameObject.SetActive(false);hud.cards[index]=card;
   }
   foreach(var graphic in new[]{menus.header,menus.status,menus.standings}.Concat(menus.viewTexts))graphic.enabled=false;
   foreach(var panel in menus.viewPanels){var backing=panel.Find("View data backing");if(backing!=null)backing.gameObject.SetActive(false);}
   var scores=menus.GetComponent<SportsBroadcastScores>()??menus.gameObject.AddComponent<SportsBroadcastScores>();scores.menus=menus;scores.enabled=false;foreach(var old in menus.GetComponents<SportsCompactViewportHud>())old.enabled=false;foreach(var old in match.GetComponents<SportsExpansionViewHud>())old.enabled=false;
   EditorUtility.SetDirty(hud);EditorSceneManager.MarkSceneDirty(match.gameObject.scene);EditorSceneManager.SaveScene(match.gameObject.scene);AssetDatabase.SaveAssets();Debug.Log("HAULER_CARGO_HUD_AUTHORED existing Heat theme/TMP/plate/portrait; actual name/color/points/cargo only; shared roster or actual split owner, contextual authoritative dump prompt, no static instruction strip");
  }
  static RectTransform Plate(Transform parent,string name,Vector2 size){var rect=SportsUIAuthoring.Rect(name,parent,size,Vector2.zero).GetComponent<RectTransform>();var image=rect.gameObject.AddComponent<UnityEngine.UI.Image>();image.color=SportsBroadcastCanvas.Ink;image.raycastTarget=false;rect.gameObject.AddComponent<SportsBroadcastPlate>().cut=8;return rect;}
  static void Top(RectTransform rect,Vector2 at){rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.up;rect.anchoredPosition=at;}
  static TMP_Text Label(Transform parent,string name,TMP_FontAsset font,Vector2 at,Vector2 size,int height){var text=SportsUIAuthoring.Text(name,parent,"",size,Vector2.zero,height);text.font=font;text.richText=false;text.fontSizeMin=height-2;text.fontSizeMax=height;text.alignment=TextAlignmentOptions.MidlineLeft;text.overflowMode=TextOverflowModes.Ellipsis;Top(text.rectTransform,at);return text;}
 }
}
