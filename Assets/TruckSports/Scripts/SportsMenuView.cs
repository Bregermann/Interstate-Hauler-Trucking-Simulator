using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using Michsky.UI.Heat;
namespace LWS.TruckSports {

public class SportsMenuView:MonoBehaviour {
 public GameObject[] pages;public SportsSettings settings;public SportsFeedback feedback;
 public Slider master,sfx,music,steering;public TMP_Text displayText,resolutionText,vibrationText;public int Page{get;private set;}
 protected readonly SportsDeviceInput menuInput=new SportsDeviceInput();
 protected virtual void Start(){foreach(var a in GetComponentsInChildren<SportsMenuAction>(true)){string key=a.action;a.button.onClick.AddListener(()=>Action(key));}master.onValueChanged.AddListener(v=>{settings.master=v/100;settings.ApplyAudio();});sfx.onValueChanged.AddListener(v=>settings.sfx=v/100);if(music!=null)music.onValueChanged.AddListener(v=>settings.music=v/100);steering.onValueChanged.AddListener(v=>settings.sensitivity=v);}
 public virtual void Action(string key){feedback.Play(0,0,true);switch(key){case "master":break;case "fullscreen":settings.fullscreen=!settings.fullscreen;SettingsText();break;case "resolution":settings.resolutionIndex=(settings.resolutionIndex+1)%settings.resolutions.Length;SettingsText();break;case "vibration":settings.vibration=!settings.vibration;settings.Save(false);if(!settings.vibration)feedback.StopMotors();SettingsText();break;case "save":settings.Save();SettingsText();break;}}
 public virtual void Show(int page){Page=page;foreach(var p in pages)if(p!=null)p.SetActive(false);if(page>=0&&page<pages.Length&&pages[page]!=null)pages[page].SetActive(true);Focus();}
 public void Focus(){if(EventSystem.current==null||Page<0||Page>=pages.Length)return;var items=pages[Page].GetComponentsInChildren<Selectable>();var active=new System.Collections.Generic.List<Selectable>();foreach(var i in items)if(i.IsInteractable())active.Add(i);for(int i=0;i<active.Count;i++){var nav=new Navigation{mode=Navigation.Mode.Explicit,selectOnUp=active[(i+active.Count-1)%active.Count],selectOnDown=active[(i+1)%active.Count]};if(!(active[i] is Slider)){nav.selectOnLeft=nav.selectOnUp;nav.selectOnRight=nav.selectOnDown;}var heat=active[i].GetComponent<ButtonManager>();if(heat!=null){heat.navigationMode=nav.mode;heat.selectOnUp=nav.selectOnUp!=null?nav.selectOnUp.gameObject:null;heat.selectOnDown=nav.selectOnDown!=null?nav.selectOnDown.gameObject:null;heat.selectOnLeft=nav.selectOnLeft!=null?nav.selectOnLeft.gameObject:null;heat.selectOnRight=nav.selectOnRight!=null?nav.selectOnRight.gameObject:null;}active[i].navigation=nav;}EventSystem.current.SetSelectedGameObject(null);if(active.Count>0)EventSystem.current.SetSelectedGameObject(active[0].gameObject);}
 static void SetCaption(TMP_Text text,string value){text.text=value;var button=text.GetComponentInParent<ButtonManager>(true);if(button!=null)button.buttonText=value;}
 public void SettingsText(){master.SetValueWithoutNotify(settings.master*100);sfx.SetValueWithoutNotify(settings.sfx*100);steering.SetValueWithoutNotify(settings.sensitivity);if(music!=null)music.SetValueWithoutNotify(settings.music*100);SetCaption(displayText,"Display: "+(settings.fullscreen?"FULLSCREEN":"WINDOWED"));var r=settings.resolutions[settings.resolutionIndex];SetCaption(resolutionText,"Resolution: "+r.x+" × "+r.y);SetCaption(vibrationText,"Vibration: "+(settings.vibration?"ON":"OFF"));}
 protected virtual void Update(){menuInput.Sample();if(SportsDeviceInput.KeyDown(Key.Tab)&&EventSystem.current!=null){var selected=EventSystem.current.currentSelectedGameObject;var item=selected!=null?selected.GetComponent<Selectable>():null;if(item!=null){var next=SportsDeviceInput.KeyHeld(Key.LeftShift)?item.FindSelectableOnUp():item.FindSelectableOnDown();if(next!=null)EventSystem.current.SetSelectedGameObject(next.gameObject);}}}
}
}


