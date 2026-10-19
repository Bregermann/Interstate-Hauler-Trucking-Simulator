using UnityEngine;using UnityEngine.UI;using UnityEditor;using UnityEditor.SceneManagement;using LWS.TruckSports;using LWS.TruckSports.Editor;
namespace LWS.TruckDockDiving.Editor {
public static class DockSlangAuthoring {
 public static void Upgrade(){var scene=EditorSceneManager.OpenScene(DockAuthoring.Scene);Install(Object.FindAnyObjectByType<DockMatch>());EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();}
 public static void Install(DockMatch match){if(match.GetComponent<DockSlang>()!=null)return;var slang=match.gameObject.AddComponent<DockSlang>();slang.match=match;var card=SportsUIAuthoring.Rect("Occasional Canadian flight bubble",match.menus.hud.transform,new Vector2(230,82),Vector2.zero);slang.bubble=card.GetComponent<RectTransform>();var image=card.AddComponent<Image>();image.color=new Color(.98f,.94f,.79f,.97f);image.raycastTarget=false;var plate=card.AddComponent<SportsBroadcastPlate>();plate.cut=28;var shadow=card.AddComponent<Shadow>();shadow.effectDistance=new Vector2(3,-5);shadow.effectColor=new Color(.02f,.04f,.07f,.55f);slang.group=card.AddComponent<CanvasGroup>();slang.group.alpha=0;slang.group.blocksRaycasts=slang.group.interactable=false;
  var tail=SportsUIAuthoring.Rect("Comic tail",card.transform,new Vector2(18,18),new Vector2(-45,-40));tail.transform.localRotation=Quaternion.Euler(0,0,45);var ti=tail.AddComponent<Image>();ti.color=image.color;ti.raycastTarget=false;slang.caption=SportsUIAuthoring.Text("Canadian expression",card.transform,"Beauty!",new Vector2(210,68),Vector2.zero,36);slang.caption.color=new Color(.025f,.08f,.12f);slang.caption.fontStyle=TMPro.FontStyles.Bold;slang.caption.enableAutoSizing=false;EditorUtility.SetDirty(slang);
 }
}
}
