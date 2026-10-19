using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using LWS.TruckSports;using LWS.TruckSports.Editor;
namespace LWS.TruckMattressRacing.Editor {
public static class MattressGoddessAuthoring {
 public static void Upgrade(){var scene=EditorSceneManager.OpenScene(MattressAuthoring.Scene);Install(UnityEngine.Object.FindAnyObjectByType<MattressMatch>());EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();}
 public static void Install(MattressMatch match){match.course.weather.presets=SportsWeatherPresetsAuthoring.EnsureCatalog();var old=match.menus.optionText.transform.parent.gameObject;old.SetActive(false);var picker=SportsWeatherPickerAuthoring.Install(match,match.course.weather.presets,new Vector2(360,255));foreach(var label in match.menus.pages[0].GetComponentsInChildren<TMPro.TMP_Text>(true)){if(label.name=="Weather selection label")label.rectTransform.anchoredPosition=new Vector2(360,301);if(label.name=="Weather selection explanation")label.gameObject.SetActive(false);}foreach(var text in match.menus.pages[4].GetComponentsInChildren<TMPro.TMP_Text>(true))if(text.name=="Rules and controls"){text.text=match.Help;EditorUtility.SetDirty(text);}EditorUtility.SetDirty(match.course.weather);}
}
}
