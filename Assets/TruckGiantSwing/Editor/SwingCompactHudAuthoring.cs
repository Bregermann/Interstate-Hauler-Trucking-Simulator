using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using LWS.TruckSports;
namespace LWS.TruckGiantSwing.Editor
{
 // Explicit original-scene UI configuration only. The caller opens/saves.
 public static class SwingCompactHudAuthoring
 {
  [MenuItem("Truck Sports/Swing/Install compact live context in open scene")]
  public static void InstallOpenScene(){var match=UnityEngine.Object.FindAnyObjectByType<SwingMatch>();if(match==null||match.gameObject.scene.path!=SwingAuthoring.Scene)throw new InvalidOperationException("Open the original Swing scene; this entry never opens or saves scenes.");Install(match);EditorSceneManager.MarkSceneDirty(match.gameObject.scene);Debug.Log("Swing_COMPACT_CONTEXT_READY original scores/owners/replay retained; caller saves scene.");}
  public static void Install(SwingMatch match)
  {
   if(match==null||match.gameObject.scene.path!=SwingAuthoring.Scene)throw new ArgumentException("Original Swing only.");var menus=match.menus;if(menus==null||menus.GetComponent<SportsBroadcastCanvas>()==null)throw new InvalidOperationException("Existing authored Heat broadcast canvas required.");
   var rect=menus.status.rectTransform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,0);rect.pivot=new Vector2(.5f,0);rect.anchoredPosition=new Vector2(0,86);rect.sizeDelta=new Vector2(600,48);menus.status.fontSizeMax=25;menus.status.fontSizeMin=21;menus.status.enableAutoSizing=true;
   var backing=menus.status.transform.parent.Find("Broadcast status backplate");if(backing!=null){var plate=backing.GetComponent<RectTransform>();plate.anchorMin=rect.anchorMin;plate.anchorMax=rect.anchorMax;plate.pivot=rect.pivot;plate.anchoredPosition=rect.anchoredPosition;plate.sizeDelta=rect.sizeDelta;}
   if(match.CompactViewportHud){var compact=menus.GetComponent<SportsCompactViewportHud>();if(compact==null)compact=menus.gameObject.AddComponent<SportsCompactViewportHud>();compact.menus=menus;EditorUtility.SetDirty(compact);}
   EditorUtility.SetDirty(menus.status);EditorUtility.SetDirty(menus);
  }
 }
}