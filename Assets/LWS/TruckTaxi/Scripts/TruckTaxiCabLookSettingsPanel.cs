using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LWS.TruckTaxi
{
    public sealed class TruckTaxiCabLookSettingsPanel : MonoBehaviour
    {
        private TruckTaxiBootstrap host;
        private TruckTaxiCabLookSettings settings;
        private RectTransform panel;
        private readonly List<Action> refresh=new List<Action>();
        private bool syncing,wasPaused;
        public bool IsOpen => panel!=null && panel.gameObject.activeSelf;
        public Transform FocusRoot => panel;
        public void Initialize(TruckTaxiBootstrap owner,TruckTaxiHud hud,TruckTaxiCabLook look)
        {
            if(panel!=null) return;
            host=owner; settings=look.Settings;
            panel=hud.Panel(hud.Root,"Cab look settings",new Vector2(.28f,.22f),new Vector2(.72f,.83f));
            panel.gameObject.SetActive(false);
            hud.Text(panel,"Cab look heading",new Vector2(.05f,.86f),new Vector2(.95f,.97f),34).text="CAB LOOK";
            Slider(hud,"Mouse sensitivity",.7f,.02f,.5f,()=>settings.MouseSensitivity,v=>settings.SetMouseSensitivity(v));
            Slider(hud,"Gamepad sensitivity",.55f,30f,240f,()=>settings.GamepadSensitivity,v=>settings.SetGamepadSensitivity(v));
            Slider(hud,"Smoothing",.4f,0f,.25f,()=>settings.Smoothing,v=>settings.SetSmoothing(v));
            var label=hud.Text(panel,"Invert Y label",new Vector2(.05f,.26f),new Vector2(.62f,.35f),25); label.text="Invert Y";
            var toggle=Instantiate(hud.heatSwitchPrefab,panel,false); toggle.name="Invert Y";
            Place(toggle,new Vector2(.74f,.27f),new Vector2(.95f,.34f));
            var heat=Find(toggle,"Michsky.UI.Heat.SwitchManager"); var type=heat.GetType();
            type.GetField("saveValue").SetValue(heat,false);
            type.GetField("useSounds").SetValue(heat,false);
            type.GetField("invokeOnEnable").SetValue(heat,false);
            type.GetField("useUINavigation").SetValue(heat,true);
            type.GetField("isOn").SetValue(heat,settings.InvertY);
            ((UnityEngine.Events.UnityEvent<bool>)type.GetField("onValueChanged").GetValue(heat)).AddListener(v=> { if(!syncing) settings.SetInvertY(v); });
            refresh.Add(()=> { type.GetField("isOn").SetValue(heat,settings.InvertY); if(toggle.activeInHierarchy) type.GetMethod("UpdateUI").Invoke(heat,null); });
            hud.Button(panel,"DEFAULTS",new Vector2(.05f,.05f),new Vector2(.45f,.15f),()=> { settings.ResetDefaults(); Refresh(); });
            hud.Button(panel,"CLOSE",new Vector2(.55f,.05f),new Vector2(.95f,.15f),Close);
            Refresh(); panel.gameObject.SetActive(false);
        }
        public void Open()
        {
            if(IsOpen || panel==null) return;
            wasPaused=host.Paused; host.SetPaused(true);
            panel.gameObject.SetActive(true); panel.SetAsLastSibling(); Refresh();
        }
        public void Close()
        {
            if(!IsOpen) return;
            panel.gameObject.SetActive(false); PlayerPrefs.Save(); host.SetPaused(wasPaused);
        }
        private void Slider(TruckTaxiHud hud,string name,float y,float min,float max,Func<float> get,Action<float> set)
        {
            var label=hud.Text(panel,name+" value",new Vector2(.05f,y),new Vector2(.5f,y+.1f),23);
            var go=Instantiate(hud.heatSliderPrefab,panel,false); go.name=name;
            Place(go,new Vector2(.53f,y+.02f),new Vector2(.95f,y+.08f));
            var heat=Find(go,"Michsky.UI.Heat.SliderManager"); var type=heat.GetType();
            type.GetField("saveValue").SetValue(heat,false);
            type.GetField("invokeOnAwake").SetValue(heat,false);
            type.GetField("useSounds").SetValue(heat,false);
            foreach(var text in go.GetComponentsInChildren<TMP_Text>(true)) text.enabled=false;
            foreach(var field in go.GetComponentsInChildren<TMP_InputField>(true)) field.gameObject.SetActive(false);
            var slider=go.GetComponent<Slider>(); slider.minValue=min; slider.maxValue=max;
            slider.onValueChanged.AddListener(v=> { if(!syncing) { set(v); label.text=name+"\n"+get().ToString("0.##"); } });
            refresh.Add(()=> { slider.SetValueWithoutNotify(get()); label.text=name+"\n"+get().ToString("0.##"); });
        }
        private void Refresh() { syncing=true; foreach(var action in refresh) action(); syncing=false; }
        private static void Place(GameObject go,Vector2 min,Vector2 max)
        { var rect=(RectTransform)go.transform; rect.anchorMin=min; rect.anchorMax=max; rect.offsetMin=rect.offsetMax=Vector2.zero; }
        private static Component Find(GameObject go,string typeName)
        { foreach(var c in go.GetComponents<Component>()) if(c.GetType().FullName==typeName) return c; throw new InvalidOperationException("Missing Heat control: "+typeName); }
    }
}
