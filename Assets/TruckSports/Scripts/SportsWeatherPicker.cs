using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Michsky.UI.Heat;

namespace LWS.TruckSports {
// Stable shared preference + input navigation over the installed Heat dropdown.
[DefaultExecutionOrder(-20)]
public sealed class SportsWeatherPicker:MonoBehaviour,ISubmitHandler {
 public SportsWeatherPresets catalog;
 public Michsky.UI.Heat.Dropdown dropdown;
 bool initialized,expanded;ScrollRect scroll;GameObject priorSelected;readonly Vector3[] corners=new Vector3[4];UnityEngine.InputSystem.InputAction cancelAction;
 void Awake(){
  catalog.ValidateCatalog();dropdown.initOnEnable=false;dropdown.updateOnEnable=false;dropdown.invokeOnEnable=false;dropdown.saveSelected=false;dropdown.navigationMode=Navigation.Mode.Automatic;
  dropdown.items.Clear();dropdown.items.Add(new Michsky.UI.Heat.Dropdown.Item{itemName="Random · installed presets"});
  foreach(var preset in catalog.entries)dropdown.items.Add(new Michsky.UI.Heat.Dropdown.Item{itemName=preset.displayName});
  dropdown.selectedItemIndex=Mathf.Max(0,catalog.Find(catalog.Selection)+1);
 }
 void Start(){dropdown.Initialize();ApplyStyle();scroll=dropdown.GetComponentInChildren<ScrollRect>(true);initialized=true;Refresh();dropdown.onValueChanged.AddListener(Choose);var module=EventSystem.current!=null?EventSystem.current.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>():null;cancelAction=module!=null&&module.cancel!=null?module.cancel.action:null;if(cancelAction!=null)cancelAction.performed+=Cancel;}
 public void OnSubmit(BaseEventData data){if(isActiveAndEnabled&&dropdown.isInteractable&&EventSystem.current!=null&&EventSystem.current.currentSelectedGameObject==dropdown.gameObject)dropdown.Animate();}
 void Cancel(UnityEngine.InputSystem.InputAction.CallbackContext context){if(!isActiveAndEnabled||!dropdown.isOn)return;dropdown.Animate();EventSystem.current?.SetSelectedGameObject(dropdown.gameObject);}
 void ApplyStyle(){
  var theme=Resources.Load<SportsBroadcastTheme>("SportsBroadcastTheme");
  foreach(var owned in dropdown.GetComponentsInChildren<UIManagerImage>(true))owned.useCustomColor=true;
  foreach(var owned in dropdown.GetComponentsInChildren<UIManagerText>(true)){owned.useCustomFont=true;owned.useCustomColor=true;}
  foreach(var label in dropdown.GetComponentsInChildren<TMPro.TMP_Text>(true)){if(theme!=null)label.font=theme.bodyFont;label.color=SportsBroadcastCanvas.Cream;label.fontSize=23;}
  foreach(var image in dropdown.GetComponentsInChildren<UnityEngine.UI.Image>(true)){
   if(image.name=="Frame"||image.name=="Border"){image.gameObject.SetActive(false);continue;}
   if(image.name!="Background"&&image.name!="Border"&&image.name!="Filler"&&image.name!="Handle")continue;
   image.color=image.name=="Handle"?SportsBroadcastCanvas.Orange:image.transform.parent.name=="Highlighted"?new Color(.65f,.20f,.035f,.99f):image.name=="Filler"?SportsBroadcastCanvas.Raised:SportsBroadcastCanvas.Ink;
   if(image.name=="Background"&&image.transform.parent.name=="List"){var color=image.color;color.a=1;image.color=color;}
   image.sprite=null;image.type=UnityEngine.UI.Image.Type.Simple;var plate=image.GetComponent<SportsBroadcastPlate>()??image.gameObject.AddComponent<SportsBroadcastPlate>();plate.cut=9;
  }
 }
 void OnEnable(){if(initialized)Refresh();expanded=false;}
 void Refresh(){dropdown.SetDropdownIndex(Mathf.Max(0,catalog.Find(catalog.Selection)+1));}
 public void Choose(int index){if(index<0||index>catalog.entries.Length)throw new System.ArgumentOutOfRangeException(nameof(index));catalog.Select(index==0?SportsWeatherPresets.RandomId:catalog.entries[index-1].id);}
 void LateUpdate(){
  if(!initialized)return;var system=EventSystem.current;if(system==null)return;
  if(expanded==dropdown.isOn){if(expanded)KeepSelectionVisible(system.currentSelectedGameObject);return;}expanded=dropdown.isOn;priorSelected=null;
  if(!expanded){system.SetSelectedGameObject(dropdown.gameObject);return;}
  for(int i=0;i<dropdown.items.Count;i++){
   var button=dropdown.items[i].itemButton.GetComponent<UnityEngine.UI.Button>();var up=dropdown.items[(i+dropdown.items.Count-1)%dropdown.items.Count].itemButton.GetComponent<UnityEngine.UI.Button>();var down=dropdown.items[(i+1)%dropdown.items.Count].itemButton.GetComponent<UnityEngine.UI.Button>();
   button.navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnUp=up,selectOnDown=down,selectOnLeft=dropdown.GetComponent<UnityEngine.UI.Button>(),selectOnRight=dropdown.GetComponent<UnityEngine.UI.Button>()};
   var heat=dropdown.items[i].itemButton;heat.useUINavigation=false;
  }
  system.SetSelectedGameObject(dropdown.items[dropdown.selectedItemIndex].itemButton.gameObject);
 }
 void KeepSelectionVisible(GameObject selected){
  if(selected==null||selected==priorSelected||scroll==null||!selected.transform.IsChildOf(dropdown.itemParent))return;
  var viewport=scroll.viewport!=null?scroll.viewport:scroll.GetComponent<RectTransform>();if(viewport.rect.height<60)return;
  Canvas.ForceUpdateCanvases();selected.GetComponent<RectTransform>().GetWorldCorners(corners);float low=float.PositiveInfinity,high=float.NegativeInfinity;foreach(var corner in corners){float y=viewport.InverseTransformPoint(corner).y;low=Mathf.Min(low,y);high=Mathf.Max(high,y);}float delta=high>viewport.rect.yMax?viewport.rect.yMax-high:low<viewport.rect.yMin?viewport.rect.yMin-low:0;
  scroll.StopMovement();float travel=scroll.content.rect.height-viewport.rect.height;
  if(travel>0)scroll.verticalNormalizedPosition=Mathf.Clamp01(scroll.verticalNormalizedPosition-delta/travel);
  priorSelected=selected;
 }
 void OnDestroy(){if(dropdown!=null)dropdown.onValueChanged.RemoveListener(Choose);if(cancelAction!=null)cancelAction.performed-=Cancel;}
}
}
