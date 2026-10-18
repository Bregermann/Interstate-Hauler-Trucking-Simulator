using UnityEditor;
using UnityEngine;

namespace LWS.TruckSports.Editor {
public static class SportsWeatherPickerAuthoring {
 public static SportsWeatherPicker Install(SportsEventSession match,SportsWeatherPresets catalog,Vector2 position){
  var parent=match.menus.pages[0].transform;var existing=parent.GetComponentInChildren<SportsWeatherPicker>(true);if(existing!=null){existing.catalog=catalog;existing.dropdown.navigationMode=UnityEngine.UI.Navigation.Mode.Automatic;EditorUtility.SetDirty(existing.dropdown);EditorUtility.SetDirty(existing);return existing;}
  var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Heat - Complete Modern UI/Prefabs/UI Elements/Dropdown/Dropdown.prefab");
  if(prefab==null)throw new System.InvalidOperationException("Installed Heat production dropdown is required.");
  var root=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);PrefabUtility.UnpackPrefabInstance(root,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);root.name="Weather · installed profile selector";
  var rect=root.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=position;rect.sizeDelta=new Vector2(630,54);
  var dropdown=root.GetComponent<Michsky.UI.Heat.Dropdown>();dropdown.saveSelected=false;dropdown.initOnEnable=false;dropdown.updateOnEnable=false;dropdown.invokeOnEnable=false;dropdown.enableIcon=false;dropdown.useSounds=false;dropdown.useUINavigation=true;dropdown.navigationMode=UnityEngine.UI.Navigation.Mode.Explicit;dropdown.enableScrollbar=true;dropdown.panelSize=330;dropdown.items.Clear();dropdown.items.Add(new Michsky.UI.Heat.Dropdown.Item{itemName="Random · installed presets"});foreach(var entry in catalog.entries)dropdown.items.Add(new Michsky.UI.Heat.Dropdown.Item{itemName=entry.displayName});dropdown.selectedItemIndex=0;
  // SportsMenuView wires explicit neighbours after showing its page. Avoid Heat's
  // delayed explicit-navigation coroutine while the saved setup page is inactive.
  dropdown.navigationMode=UnityEngine.UI.Navigation.Mode.Automatic;
  var picker=root.AddComponent<SportsWeatherPicker>();picker.catalog=catalog;picker.dropdown=dropdown;
  SportsUIAuthoring.Text("Weather selection label",parent,"WEATHER",new Vector2(620,30),position+new Vector2(0,44),22).color=new Color(.58f,.88f,.91f);
  SportsUIAuthoring.Text("Weather selection explanation",parent,"One forecast for the whole match.\nWind affects every kick. Random rolls again on rematch.",new Vector2(610,86),position+new Vector2(0,-85),22);
  EditorUtility.SetDirty(dropdown);EditorUtility.SetDirty(picker);return picker;
 }
}
}
