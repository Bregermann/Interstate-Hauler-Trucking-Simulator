using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LWS.TruckTaxi
{
    public sealed class TruckTaxiVideoSettingsPanel : MonoBehaviour
    {
        private TruckTaxiHud hud;
        private TruckTaxiBootstrap host;
        private RectTransform panel;
        private TextMeshProUGUI status;
        private TruckTaxiVideoSettings draft,previous;
        private readonly List<Action> refresh=new List<Action>();
        private readonly List<Vector2Int> resolutions=new List<Vector2Int>();
        private bool wasPaused,pending,syncing;
        private float confirmUntil;
        public bool IsOpen => panel!=null && panel.gameObject.activeSelf;
        public Transform FocusRoot => panel;
        public bool AwaitingConfirmation => pending;
        public void Initialize(TruckTaxiHud view,TruckTaxiBootstrap owner=null)
        {
            if(panel!=null) return;
            hud=view;host=owner;
            panel=hud.Panel(hud.Root,"Video display settings",new Vector2(.1f,.08f),new Vector2(.9f,.92f));
            hud.Text(panel,"Heading",new Vector2(.04f,.90f),new Vector2(.96f,.98f),34).text="VIDEO / DISPLAY";
            draft=TruckTaxiVideoSettings.Load();
            foreach(var r in Screen.resolutions)
            { var size=new Vector2Int(r.width,r.height);if(r.width>=800 && r.height>=600 && !resolutions.Contains(size)) resolutions.Add(size); }
            if(resolutions.Count==0) resolutions.Add(new Vector2Int(1920,1080));
            Choice(0,"WINDOW MODE",()=>new[]{"WINDOWED","BORDERLESS","FULLSCREEN"}[draft.mode],n=>draft.mode=(draft.mode+n+3)%3);
            Choice(1,"RESOLUTION",()=>draft.width+" x "+draft.height,ChangeResolution);
            Choice(2,"QUALITY",()=>QualitySettings.names[draft.quality],n=>draft.quality=(draft.quality+n+QualitySettings.names.Length)%QualitySettings.names.Length);
            Toggle(3,"VSYNC",()=>draft.vsync,v=>draft.vsync=v);
            Slider(4,"FRAME CAP",30,240,()=>draft.frameCap,v=>draft.frameCap=Mathf.RoundToInt(v),true);
            Slider(5,"RENDER SCALE",.5f,1.5f,()=>draft.renderScale,v=>draft.renderScale=v,false);
            Choice(6,"ANTI-ALIASING",()=>draft.msaa==1 ? "OFF" : draft.msaa+"x MSAA",n=>
            { int[] values={1,2,4,8};draft.msaa=values[(Array.IndexOf(values,draft.msaa)+n+4)%4]; });
            Toggle(7,"SHADOWS",()=>draft.shadows,v=>draft.shadows=v);
            Slider(8,"SHADOW DISTANCE",0,400,()=>draft.shadowDistance,v=>draft.shadowDistance=v,true);
            Choice(9,"TEXTURES",()=>new[]{"FULL","HALF","QUARTER","EIGHTH"}[draft.textureLimit],n=>draft.textureLimit=(draft.textureLimit+n+4)%4);
            Toggle(10,"REALTIME REFLECTIONS",()=>draft.reflections,v=>draft.reflections=v);
            Toggle(11,"POST-PROCESSING",()=>draft.postProcessing,v=>draft.postProcessing=v);
            Choice(12,"WEATHER EFFECTS",()=>new[]{"ULTRA","HIGH","MEDIUM","LOW"}[draft.weatherQuality],n=>draft.weatherQuality=(draft.weatherQuality+n+4)%4);
            status=hud.Text(panel,"Confirmation status",new Vector2(.04f,.11f),new Vector2(.96f,.18f),23);
            hud.Button(panel,"APPLY",new Vector2(.04f,.02f),new Vector2(.25f,.10f),Apply);
            hud.Button(panel,"CONFIRM",new Vector2(.28f,.02f),new Vector2(.49f,.10f),Confirm);
            hud.Button(panel,"REVERT",new Vector2(.52f,.02f),new Vector2(.73f,.10f),Revert);
            hud.Button(panel,"BACK",new Vector2(.76f,.02f),new Vector2(.96f,.10f),Close);
            panel.gameObject.SetActive(false);
        }
        private RectTransform Row(int index,string label)
        {
            float x=.035f+(index%2)*.49f,y=.80f-(index/2)*.095f;
            var row=TruckTaxiHud.Rect(panel,label,new Vector2(x,y),new Vector2(x+.45f,y+.085f));
            hud.Text(row,"Label",new Vector2(0,.5f),new Vector2(1,1),20).text=label;
            return row;
        }
        private void Choice(int index,string label,Func<string> get,Action<int> change)
        {
            var row=Row(index,label);
            var text=hud.Text(row,"Value",new Vector2(.13f,0),new Vector2(.87f,.52f),22);text.alignment=TextAlignmentOptions.Center;
            hud.Button(row,"<",Vector2.zero,new Vector2(.12f,.52f),()=>{if(!pending) {change(-1);Refresh();}});
            hud.Button(row,">",new Vector2(.88f,0),new Vector2(1,.52f),()=>{if(!pending) {change(1);Refresh();}});
            refresh.Add(()=>text.text=get());
        }
        private void Toggle(int index,string label,Func<bool> get,Action<bool> set)
        {
            var row=Row(index,label);
            var box=TruckTaxiHud.Rect(row,"Toggle",new Vector2(.02f,.05f),new Vector2(.12f,.48f));
            var background=box.gameObject.AddComponent<Image>();background.color=new Color(.2f,.24f,.25f);
            var tick=TruckTaxiHud.Rect(box,"Checked",new Vector2(.2f,.2f),new Vector2(.8f,.8f)).gameObject.AddComponent<Image>();tick.color=new Color(.2f,.9f,.6f);
            var toggle=box.gameObject.AddComponent<UnityEngine.UI.Toggle>();toggle.targetGraphic=background;toggle.graphic=tick;
            var value=hud.Text(row,"State",new Vector2(.17f,0),new Vector2(.9f,.52f),22);
            toggle.onValueChanged.AddListener(v=>{if(!syncing && !pending) {set(v);Refresh();}});
            refresh.Add(()=>{toggle.SetIsOnWithoutNotify(get());toggle.interactable=!pending;value.text=get() ? "ON" : "OFF";});
        }
        private void Slider(int index,string label,float min,float max,Func<float> get,Action<float> set,bool whole)
        {
            var row=Row(index,label);
            var value=hud.Text(row,"Value",new Vector2(.72f,0),new Vector2(1,.52f),22);
            var go=Instantiate(hud.heatSliderPrefab,row,false);go.name=label;
            var rect=(RectTransform)go.transform;rect.anchorMin=new Vector2(.02f,.10f);rect.anchorMax=new Vector2(.67f,.42f);rect.offsetMin=rect.offsetMax=Vector2.zero;
            foreach(var component in go.GetComponents<Component>()) if(component.GetType().FullName=="Michsky.UI.Heat.SliderManager")
            { var t=component.GetType();t.GetField("saveValue").SetValue(component,false);t.GetField("invokeOnAwake").SetValue(component,false);t.GetField("useSounds").SetValue(component,false); }
            foreach(var text in go.GetComponentsInChildren<TMP_Text>(true)) text.enabled=false;
            foreach(var input in go.GetComponentsInChildren<TMP_InputField>(true)) input.gameObject.SetActive(false);
            var slider=go.GetComponent<UnityEngine.UI.Slider>();slider.minValue=min;slider.maxValue=max;slider.wholeNumbers=whole;
            slider.onValueChanged.AddListener(v=>{if(!syncing && !pending) {set(v);Refresh();}});
            refresh.Add(()=>{slider.SetValueWithoutNotify(get());slider.interactable=!pending;value.text=get().ToString(whole ? "0" : "0.00");});
        }
        private void ChangeResolution(int change)
        { int index=resolutions.IndexOf(new Vector2Int(draft.width,draft.height));var size=resolutions[(Mathf.Max(0,index)+change+resolutions.Count)%resolutions.Count];draft.width=size.x;draft.height=size.y; }
        public void Open()
        {
            if(IsOpen) return;
            draft=TruckTaxiVideoSettings.Capture();wasPaused=host?.Paused ?? false;host?.SetPaused(true);
            panel.gameObject.SetActive(true);panel.SetAsLastSibling();status.text="";Refresh();
        }
        public void Apply()
        {
            if(!IsOpen || pending) return;
            previous=TruckTaxiVideoSettings.Capture();draft.Apply(host);pending=true;confirmUntil=Time.unscaledTime+15;Refresh();
        }
        public void Confirm()
        { if(!pending) return;pending=false;draft.Save();status.text="DISPLAY SETTINGS SAVED";Refresh(); }
        public void Revert()
        {
            if(pending) { pending=false;previous.Apply(host);draft=previous.Copy(); }
            else draft=TruckTaxiVideoSettings.Capture();
            status.text="CHANGES REVERTED";Refresh();
        }
        public void Close()
        { if(!IsOpen) return;if(pending) Revert();panel.gameObject.SetActive(false);host?.SetPaused(wasPaused); }
        private void Refresh() { syncing=true;foreach(var action in refresh) action();syncing=false; }
        private void Update()
        { if(!pending) return;if(Time.unscaledTime>=confirmUntil) Revert();else status.text="KEEP THESE SETTINGS?  "+Mathf.CeilToInt(confirmUntil-Time.unscaledTime)+"s"; }
        private void OnDisable() { if(pending) Revert(); }
    }
}
