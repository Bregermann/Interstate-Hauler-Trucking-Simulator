using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LWS.TruckTaxi
{
    // Controls Compass's existing overlay minimap. No map camera, route, or world chunk is created here.
    public sealed class TruckTaxiFullMapPresenter : MonoBehaviour
    {
        private readonly struct Place
        {
            public readonly string Id,Label,Kind;
            public readonly Vector3 Position;
            public readonly bool Routeable;
            public Place(string id,string label,string kind,Vector3 position,bool routeable)
            { Id=id; Label=label; Kind=kind; Position=position; Routeable=routeable; }
        }
        private readonly List<Place> places=new List<Place>();
        private readonly List<GameObject> rows=new List<GameObject>();
        private TruckTaxiBootstrap host;
        private TruckTaxiGPSAdapter gps;
        private TruckTaxiHud hud;
        private TruckTaxiUIInput input;
        private RectTransform root,placeList;
        private TextMeshProUGUI pageText,selectionText;
        private GameObject routeButton;
        private bool wasPaused;
        private int page,selected=-1;
        private const int PageSize=8;
        public bool IsOpen => root!=null && root.gameObject.activeSelf;
        public Transform FocusRoot => IsOpen ? root : null;

        // Call once after Hud.Initialize, GPS.ConfigureDemoPresentation and UIInput creation.
        public void Initialize(TruckTaxiBootstrap taxi,TruckTaxiHud view)
        {
            host=taxi; hud=view; gps=taxi.GPS; input=view.UIInput;
            var canvasObject=new GameObject("Truck Taxi full map controls",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform,false);
            var canvas=canvasObject.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.sortingOrder=920;
            var scaler=canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1920,1080); scaler.matchWidthOrHeight=.5f;
            root=TruckTaxiHud.Rect(canvasObject.transform,"Full map",Vector2.zero,Vector2.one);
            var viewport=TruckTaxiHud.Rect(root,"Compass map viewport",new Vector2(.32f,.06f),new Vector2(.98f,.90f));
            TruckTaxiGPSAdapter.Set(gps.HudCompass,"miniMapFullScreenPlaceholder",viewport);
            var header=hud.Panel(root,"Map header",new Vector2(.02f,.92f),new Vector2(.98f,.985f));
            hud.Text(header,"REGIONAL MAP",new Vector2(.02f,.08f),new Vector2(.33f,.92f),30).text="REGIONAL MAP";
            hud.Button(header,"CENTER ON PLAYER",new Vector2(.38f,.08f),new Vector2(.63f,.92f),()=>gps.CenterFullMapOnPlayer());
            hud.Button(header,"-",new Vector2(.69f,.08f),new Vector2(.75f,.92f),()=>gps.ZoomFullMap(-1));
            hud.Button(header,"+",new Vector2(.76f,.08f),new Vector2(.82f,.92f),()=>gps.ZoomFullMap(1));
            hud.Button(header,"CLOSE",new Vector2(.85f,.08f),new Vector2(.98f,.92f),Close);
            var sidebar=hud.Panel(root,"Map places",new Vector2(.02f,.12f),new Vector2(.30f,.90f));
            hud.Text(sidebar,"PLACES",new Vector2(.04f,.93f),new Vector2(.96f,.99f),25).text="PLACES";
            placeList=TruckTaxiHud.Rect(sidebar,"Place rows",new Vector2(.04f,.18f),new Vector2(.96f,.91f));
            pageText=hud.Text(sidebar,"Page",new Vector2(.31f,.11f),new Vector2(.69f,.18f),20);
            pageText.alignment=TextAlignmentOptions.Center;
            hud.Button(sidebar,"<",new Vector2(.04f,.1f),new Vector2(.29f,.17f),()=>ShowPage(page-1));
            hud.Button(sidebar,">",new Vector2(.71f,.1f),new Vector2(.96f,.17f),()=>ShowPage(page+1));
            selectionText=hud.Text(sidebar,"Selected place",new Vector2(.04f,.055f),new Vector2(.96f,.105f),18);
            routeButton=hud.Button(sidebar,"SET GPS DESTINATION",new Vector2(.04f,.005f),new Vector2(.96f,.055f),RouteSelected);
            root.gameObject.SetActive(false);
            var miniRoot=gps.HudCompass?.transform.Find("MiniMap Root") as RectTransform;
            if(miniRoot!=null)
            {
                var expand=TruckTaxiHud.Rect(miniRoot,"Expand regional map",new Vector2(.80f,.80f),new Vector2(.98f,.98f));
                var icon=expand.gameObject.AddComponent<Image>();
                icon.sprite=Resources.Load<Sprite>("CNPro/Sprites/zoomToggle");
                icon.color=new Color(.9f,.95f,.9f,.95f);
                icon.preserveAspect=true;
                expand.gameObject.AddComponent<Button>().onClick.AddListener(()=>Toggle());
                expand.SetAsLastSibling();
            }
        }
        public bool Open()
        {
            if(IsOpen || host==null || gps==null || gps.HudCompass==null || hud?.CanOpenFullMap!=true) return false;
            wasPaused=host.Paused;
            RebuildPlaces();
            var bounds=new Bounds(host.Player.transform.position,Vector3.zero);
            foreach(var place in places) bounds.Encapsulate(place.Position);
            root.gameObject.SetActive(true);
            Canvas.ForceUpdateCanvases();
            if(!gps.SetFullMapOpen(true,Mathf.Max(1500,bounds.size.x+600,bounds.size.z+600),bounds.center))
            { root.gameObject.SetActive(false); return false; }
            host.SetPaused(true);
            page=0; selected=-1; ShowPage(0);
            root.gameObject.SetActive(true); input.Focus(root); hud.OnFullMapVisibilityChanged();
            return true;
        }
        public void Close()
        {
            if(!IsOpen) return;
            root.gameObject.SetActive(false);
            gps.SetFullMapOpen(false);
            if(!wasPaused) host.SetPaused(false);
            hud.OnFullMapVisibilityChanged();
        }
        public bool Toggle() { if(IsOpen) { Close(); return true; } return Open(); }
        // HUD calls this before its normal pause/submit handling. True consumes this frame's menu command.
        public bool HandleUiActions()
        {
            if(input==null) return false;
            if(input.FullMap.WasPressedThisFrame()) return Toggle();
            if(!IsOpen) return false;
            if(input.Cancel.WasPressedThisFrame() || input.Pause.WasPressedThisFrame()) { Close(); return true; }
            if(input.Submit.WasPressedThisFrame()) { input.SubmitSelected(); return true; }
            var keyboard=Keyboard.current;
            Vector2 direction=Vector2.zero;
            if(keyboard!=null)
            {
                if(keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) direction.x--;
                if(keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) direction.x++;
                if(keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) direction.y--;
                if(keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) direction.y++;
                if(keyboard.pageUpKey.wasPressedThisFrame) gps.ZoomFullMap(1);
                if(keyboard.pageDownKey.wasPressedThisFrame) gps.ZoomFullMap(-1);
                if(keyboard.homeKey.wasPressedThisFrame) gps.CenterFullMapOnPlayer();
            }
            if(Gamepad.current!=null)
            {
                direction+=Gamepad.current.rightStick.ReadValue();
                if(Gamepad.current.rightShoulder.wasPressedThisFrame) gps.ZoomFullMap(1);
                if(Gamepad.current.leftShoulder.wasPressedThisFrame) gps.ZoomFullMap(-1);
            }
            if(direction.sqrMagnitude>.01f) gps.PanFullMap(Vector2.ClampMagnitude(direction,1),Time.unscaledDeltaTime);
            if(Mouse.current!=null)
            {
                float wheel=Mouse.current.scroll.ReadValue().y;
                if(Mathf.Abs(wheel)>1) gps.ZoomFullMap(Mathf.Sign(wheel));
            }
            return true;
        }
        private void RebuildPlaces()
        {
            places.Clear();
            var markers=gps.MapMarkers;
            if(markers==null) return;
            var metadata=new HashSet<string>(StringComparer.Ordinal);
            foreach(var point in markers.RegionalPoints)
            { places.Add(new Place(point.Id,point.Label,point.Kind,point.Position,point.Routeable)); metadata.Add(point.Id); }
            foreach(var marker in markers.Markers)
            {
                if(marker==null || metadata.Contains(marker.stableId) || marker.state==TruckTaxiMapMarkerState.Hidden ||
                    marker.markerType==TruckTaxiMapMarkerType.Debug) continue;
                places.Add(new Place(marker.stableId,marker.label,marker.markerType.ToString(),marker.Position,IsRouteable(marker.markerType)));
            }
            places.Sort((a,b)=>string.Compare(a.Label,b.Label,StringComparison.OrdinalIgnoreCase));
        }
        public static bool IsRouteable(TruckTaxiMapMarkerType type) =>
            type==TruckTaxiMapMarkerType.Bathroom || type==TruckTaxiMapMarkerType.FoodStop ||
            type==TruckTaxiMapMarkerType.ScenicStop || type==TruckTaxiMapMarkerType.PhotoStop ||
            type==TruckTaxiMapMarkerType.SpecialEvent || type==TruckTaxiMapMarkerType.PrivateEventStop;
        private void ShowPage(int next)
        {
            foreach(var row in rows) { row.SetActive(false); Destroy(row); }
            rows.Clear();
            page=Mathf.Clamp(next,0,Mathf.Max(0,(places.Count-1)/PageSize));
            pageText.text=$"{page+1} / {Mathf.Max(1,(places.Count+PageSize-1)/PageSize)}";
            for(int i=page*PageSize;i<Mathf.Min(places.Count,(page+1)*PageSize);i++)
            {
                int index=i,slot=i-page*PageSize;
                var row=hud.Button(placeList,places[i].Label,new Vector2(0,1-(slot+1)/8f),new Vector2(1,1-slot/8f),()=>Select(index));
                rows.Add(row);
            }
            UpdateSelection();
            input?.Focus(root);
        }
        private void Select(int index) { selected=index; UpdateSelection(); }
        private void UpdateSelection()
        {
            bool valid=selected>=0 && selected<places.Count;
            selectionText.text=valid ? places[selected].Kind+": "+places[selected].Label : "SELECT A PLACE";
            routeButton.SetActive(valid && places[selected].Routeable && gps.CanSetMapServiceDestination);
        }
        private void RouteSelected()
        {
            if(selected<0 || selected>=places.Count) return;
            var place=places[selected];
            if(place.Routeable && gps.TrySetMapServiceDestination(place.Id,place.Label,place.Position)) Close();
            else UpdateSelection();
        }
        private void OnDestroy() { if(IsOpen) Close(); }
    }
}
