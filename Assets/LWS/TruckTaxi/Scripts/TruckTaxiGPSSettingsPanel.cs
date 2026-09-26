using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace LWS.TruckTaxi
{
    // Presentation on the existing HUD canvas; Compass remains the map/route renderer.
    public sealed class TruckTaxiGPSSettingsPanel : MonoBehaviour
    {
        private TruckTaxiBootstrap host;
        private TruckTaxiHud hud;
        private RectTransform panel;
        private readonly List<Action> refreshControls=new List<Action>();
        private bool syncing, wasPaused;
        public bool IsOpen => panel!=null && panel.gameObject.activeSelf;

        public void Initialize(TruckTaxiBootstrap value,TruckTaxiHud view)
        {
            host=value; hud=view;
            panel=hud.Panel(hud.Root,"GPS display settings",new Vector2(.69f,.29f),new Vector2(.99f,.96f));
            panel.gameObject.SetActive(false);
            hud.Text(panel,"GPS settings heading",new Vector2(.05f,.9f),new Vector2(.95f,.98f),32).text="GPS DISPLAY";
            Switch("On-screen GPS",.81f,()=>Settings.showHud,v=>Settings.showHud=v);
            Switch("North up",.73f,()=>Settings.northUp,v=>Settings.northUp=v);
            Switch("Place markers",.65f,()=>Settings.showPois,v=>Settings.showPois=v);
            Slider("On-screen size",.55f,.18f,.36f,()=>Settings.hudSize,v=>Settings.hudSize=v,v=>(v*100).ToString("0")+"%");
            Slider("On-screen range",.45f,150,1600,()=>Settings.hudRangeMeters,v=>Settings.hudRangeMeters=v,v=>v.ToString("0")+" m");
            Slider("Cab map range",.35f,150,1600,()=>Settings.cabRangeMeters,v=>Settings.cabRangeMeters=v,v=>v.ToString("0")+" m");
            Slider("Route width",.25f,4,24,()=>Settings.routeWidth,v=>Settings.routeWidth=v,v=>v.ToString("0")+" px");
            hud.Text(panel,"Route color label",new Vector2(.05f,.16f),new Vector2(.49f,.23f),24).text="Route color";
            var colors=TruckTaxiHud.Rect(panel,"Route colors",new Vector2(.53f,.16f),new Vector2(.95f,.23f));
            var group=colors.gameObject.AddComponent<ToggleGroup>();
            string[] names={"Cyan","Amber","Pink","Green"};
            for(int i=0;i<TruckTaxiGPSDisplaySettings.RouteColors.Length;i++)
            {
                int index=i;
                var swatch=TruckTaxiHud.Rect(colors,names[i],new Vector2(i*.25f,0),new Vector2(i*.25f+.21f,1));
                var background=swatch.gameObject.AddComponent<Image>(); background.color=TruckTaxiGPSDisplaySettings.RouteColors[i];
                var toggle=swatch.gameObject.AddComponent<Toggle>(); toggle.targetGraphic=background; toggle.group=group;
                var mark=TruckTaxiHud.Rect(swatch,"Selected",new Vector2(.12f,.1f),new Vector2(.88f,.2f)).gameObject.AddComponent<Image>();
                mark.color=Color.white; mark.raycastTarget=false; toggle.graphic=mark;
                toggle.SetIsOnWithoutNotify(i==Settings.routeColorIndex);
                toggle.onValueChanged.AddListener(on=> { if(on && !syncing) { Settings.routeColorIndex=index; Changed(); } });
                refreshControls.Add(()=>toggle.SetIsOnWithoutNotify(Settings.routeColorIndex==index));
            }
            hud.Button(panel,"DEFAULTS",new Vector2(.05f,.035f),new Vector2(.48f,.12f),()=>host.GPS.ResetDisplaySettings());
            hud.Button(panel,"CLOSE",new Vector2(.53f,.035f),new Vector2(.95f,.12f),Close);
            host.GPS.DisplaySettingsChanged+=RefreshControls;
            RefreshControls();
        }
        private TruckTaxiGPSDisplaySettings Settings => host.GPS.DisplaySettings;
        public void Toggle() { if(IsOpen) Close(); else Open(); }
        public void Open()
        {
            if(IsOpen) return;
            wasPaused=host.Paused;
            host.SetPaused(true);
            panel.gameObject.SetActive(true);
            panel.SetAsLastSibling();
            RefreshControls();
        }
        public void Close()
        {
            if(!IsOpen) return;
            panel.gameObject.SetActive(false);
            PlayerPrefs.Save();
            host.SetPaused(wasPaused);
        }
        private void Changed() { if(!syncing) host.GPS.ApplyDisplaySettings(); }
        private void RefreshControls()
        {
            syncing=true;
            foreach(var refresh in refreshControls) refresh();
            syncing=false;
        }
        private void Switch(string label,float y,Func<bool> get,Action<bool> set)
        {
            hud.Text(panel,label+" label",new Vector2(.05f,y),new Vector2(.75f,y+.07f),24).text=label;
            var go=Instantiate(hud.heatSwitchPrefab,panel,false); go.name=label;
            var rect=(RectTransform)go.transform; rect.anchorMin=new Vector2(.79f,y); rect.anchorMax=new Vector2(.95f,y+.07f); rect.offsetMin=rect.offsetMax=Vector2.zero;
            var heat=Find(go,"Michsky.UI.Heat.SwitchManager"); var type=heat.GetType();
            type.GetField("saveValue").SetValue(heat,false);
            type.GetField("useSounds").SetValue(heat,false);
            type.GetField("invokeOnEnable").SetValue(heat,false);
            type.GetField("useUINavigation").SetValue(heat,true);
            type.GetField("isOn").SetValue(heat,get());
            ((UnityEvent<bool>)type.GetField("onValueChanged").GetValue(heat)).AddListener(v=> { if(!syncing) { set(v); Changed(); } });
            refreshControls.Add(()=> { type.GetField("isOn").SetValue(heat,get()); if(go.activeInHierarchy) type.GetMethod("UpdateUI").Invoke(heat,null); });
        }
        private void Slider(string label,float y,float min,float max,Func<float> get,Action<float> set,Func<float,string> format)
        {
            var text=hud.Text(panel,label+" value",new Vector2(.05f,y),new Vector2(.5f,y+.085f),23);
            var go=Instantiate(hud.heatSliderPrefab,panel,false); go.name=label;
            var rect=(RectTransform)go.transform; rect.anchorMin=new Vector2(.55f,y+.015f); rect.anchorMax=new Vector2(.95f,y+.075f); rect.offsetMin=rect.offsetMax=Vector2.zero;
            var heat=Find(go,"Michsky.UI.Heat.SliderManager"); var type=heat.GetType();
            type.GetField("saveValue").SetValue(heat,false);
            type.GetField("invokeOnAwake").SetValue(heat,false);
            type.GetField("useSounds").SetValue(heat,false);
            foreach(var t in go.GetComponentsInChildren<TMP_Text>(true)) t.enabled=false;
            var slider=go.GetComponent<Slider>(); slider.minValue=min; slider.maxValue=max; slider.SetValueWithoutNotify(get());
            slider.onValueChanged.AddListener(v=> { if(!syncing) { set(v); Changed(); } });
            refreshControls.Add(()=> { slider.SetValueWithoutNotify(get()); text.text=label+"\n"+format(get()); });
        }
        private static Component Find(GameObject go,string typeName)
        {
            foreach(var c in go.GetComponents<Component>()) if(c.GetType().FullName==typeName) return c;
            throw new InvalidOperationException("Missing Heat control: "+typeName);
        }
        private void OnDestroy()
        {
            if(host!=null && host.GPS!=null) host.GPS.DisplaySettingsChanged-=RefreshControls;
        }
    }
}

