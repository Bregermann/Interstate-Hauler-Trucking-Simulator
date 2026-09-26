using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LWS.TruckTaxi
{
    public sealed class TruckTaxiAudioSettingsPanel : MonoBehaviour
    {
        private TruckTaxiBootstrap host;
        private TruckTaxiAudioController audio;
        private RectTransform panel;
        private bool wasPaused;
        private readonly List<System.Action> refresh=new List<System.Action>();
        public bool IsOpen => panel!=null && panel.gameObject.activeSelf;
        public Transform FocusRoot => panel;
        public void Initialize(TruckTaxiBootstrap value,TruckTaxiHud hud)
        {
            if(panel!=null) return;
            host=value;
            InitializeView(value.Audio,hud);
        }
        public void InitializeView(TruckTaxiAudioController value,TruckTaxiHud hud)
        {
            if(panel!=null) return;
            audio=value;
            panel=hud.Panel(hud.Root,"Audio settings",new Vector2(.25f,.15f),new Vector2(.75f,.87f));
            // Configure Heat before Awake can register its own saved-value listeners or invoke prefab callbacks.
            panel.gameObject.SetActive(false);
            hud.Text(panel,"Audio heading",new Vector2(.06f,.87f),new Vector2(.94f,.97f),34).text="AUDIO";
            if(audio==null || !audio.Ready)
                hud.Text(panel,"Audio setup pending",new Vector2(.06f,.22f),new Vector2(.94f,.83f),26).text="Audio mixer setup is not complete.";
            else
            {
                int index=0;
                foreach(var bus in audio.Buses)
                {
                    if(bus?.group==null) continue;
                    var category=bus.category;
                    float y=.74f-index++*.12f;
                    var label=hud.Text(panel,bus.label+" value",new Vector2(.06f,y),new Vector2(.48f,y+.1f),26);
                    var control=Instantiate(hud.heatSliderPrefab,panel,false); control.name=bus.label+" volume";
                    var rect=(RectTransform)control.transform;
                    rect.anchorMin=new Vector2(.51f,y+.025f); rect.anchorMax=new Vector2(.94f,y+.08f); rect.offsetMin=rect.offsetMax=Vector2.zero;
                    foreach(var component in control.GetComponents<Component>())
                    {
                        var type=component.GetType(); if(type.FullName!="Michsky.UI.Heat.SliderManager") continue;
                        type.GetField("saveValue").SetValue(component,false);
                        type.GetField("invokeOnAwake").SetValue(component,false);
                        type.GetField("useSounds").SetValue(component,false);
                    }
                    foreach(var text in control.GetComponentsInChildren<TMP_Text>(true)) text.enabled=false;
                    foreach(var input in control.GetComponentsInChildren<TMP_InputField>(true)) input.gameObject.SetActive(false);
                    var slider=control.GetComponent<Slider>(); slider.minValue=0; slider.maxValue=100; slider.wholeNumbers=true;
                    slider.onValueChanged.AddListener(v=>audio.SetVolume(category,v/100));
                    refresh.Add(()=> { float v=audio.GetVolume(category)*100; slider.SetValueWithoutNotify(v); label.text=bus.label+"  "+v.ToString("0"); });
                }
                hud.Button(panel,"RESET AUDIO TO DEFAULTS",new Vector2(.06f,.035f),new Vector2(.64f,.125f),audio.ResetDefaults);
            }
            hud.Button(panel,"BACK",new Vector2(.69f,.035f),new Vector2(.94f,.125f),Close);
            if(audio!=null) audio.Changed+=Refresh;
            Refresh(); panel.gameObject.SetActive(false);
        }
        private void Refresh() { foreach(var action in refresh) action(); }
        public void Open()
        {
            if(IsOpen) return;
            if(host!=null) { wasPaused=host.Paused; host.SetPaused(true); }
            panel.gameObject.SetActive(true); panel.SetAsLastSibling(); Refresh();
        }
        public void Close()
        {
            if(!IsOpen) return;
            panel.gameObject.SetActive(false); PlayerPrefs.Save();
            if(host!=null) host.SetPaused(wasPaused);
        }
        private void OnDestroy() { if(audio!=null) audio.Changed-=Refresh; }
    }
}
