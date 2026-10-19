using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using LWS.TruckSports;
using LWS.TruckSports.Editor;
namespace LWS.TruckBeerPong.Editor {
 public static class PongPacingAuthoring {
  public static void Upgrade(){var scene=EditorSceneManager.OpenScene(PongAuthoring.Scene);Install(Object.FindAnyObjectByType<PongMatch>());EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();}
  public static void Install(PongMatch m){var playback=m.GetComponent<PongAIPlayback>()??m.gameObject.AddComponent<PongAIPlayback>();playback.match=m;m.aiPlayback=playback;var title=m.menus.pages[0].transform.Find("Title").GetComponent<RectTransform>();title.anchoredPosition=new Vector2(0,420);if(m.menus.pages[0].GetComponentsInChildren<SportsMenuAction>(true).All(a=>a.action!="aipacing"))SportsUIAuthoring.Button(m.menus.pages[0].transform,"AI TURNS: NORMAL","aipacing",new Vector2(0,340),new Vector2(650,44));if(m.menus.pages[3].GetComponentsInChildren<SportsMenuAction>(true).All(a=>a.action!="aipacing"))SportsUIAuthoring.Button(m.menus.pages[3].transform,"AI TURNS: NORMAL","aipacing",new Vector2(0,-285),new Vector2(650,48));EditorUtility.SetDirty(m);EditorUtility.SetDirty(playback);}
 }
}
