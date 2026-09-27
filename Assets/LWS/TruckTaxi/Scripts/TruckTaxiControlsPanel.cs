using System.Collections.Generic;
using System.Text;
using TMPro;
using LWS.InterstateHauler;
using UnityEngine;
using UnityEngine.UI;

namespace LWS.TruckTaxi
{
    [DefaultExecutionOrder(1500)]
    public sealed class TruckTaxiControlsPanel : MonoBehaviour
    {
        private TruckTaxiBootstrap host;
        private TruckTaxiUIInput input;
        private RectTransform page,overlay,content,viewport;
        private ScrollRect scroll;
        private TextMeshProUGUI fullText,quickText;
        private TextMeshProUGUI requestStatus;
        private float requestStatusUntil;
        private List<TruckTaxiControlsCatalog.Entry> entries;
        private bool wasPaused,lastGamepad;
        private bool wheelActive,lastWheel;
        private LwsWheelInputSource wheel;
        public bool IsOpen => page!=null && page.gameObject.activeSelf;
        public Transform FocusRoot => page;

        public void Initialize(TruckTaxiBootstrap owner,TruckTaxiHud hud)
        {
            if(page!=null) return;
            host=owner; input=hud.UIInput;
            wheel=owner.Player?.GetComponentInChildren<LwsWheelInputSource>(true);
            entries=TruckTaxiControlsCatalog.Build(input,owner.DriverNeeds,owner.Passengers);
            page=hud.Panel(hud.Root,"Controls",new Vector2(.16f,.07f),new Vector2(.84f,.94f));
            page.gameObject.SetActive(false);
            hud.Text(page,"Controls heading",new Vector2(.04f,.91f),new Vector2(.96f,.98f),36).text="CONTROLS";
            viewport=TruckTaxiHud.Rect(page,"Controls viewport",new Vector2(.04f,.15f),new Vector2(.96f,.9f));
            viewport.gameObject.AddComponent<RectMask2D>();
            content=TruckTaxiHud.Rect(viewport,"Controls content",new Vector2(0,1),new Vector2(1,1));
            content.pivot=new Vector2(.5f,1f);
            fullText=hud.Text(content,"All bindings",Vector2.zero,Vector2.one,22);
            fullText.enableAutoSizing=false; fullText.overflowMode=TextOverflowModes.Overflow;
            scroll=page.gameObject.AddComponent<ScrollRect>();
            scroll.viewport=viewport; scroll.content=content; scroll.horizontal=false; scroll.vertical=true; scroll.scrollSensitivity=40;
            hud.Button(page,"UP",new Vector2(.04f,.035f),new Vector2(.21f,.13f),()=>ScrollBy(.3f));
            hud.Button(page,"DOWN",new Vector2(.24f,.035f),new Vector2(.41f,.13f),()=>ScrollBy(-.3f));
            hud.Button(page,"CLOSE",new Vector2(.73f,.035f),new Vector2(.96f,.13f),Close);
            overlay=hud.Panel(hud.Root,"Quick controls",new Vector2(.02f,.19f),new Vector2(.49f,.86f));
            quickText=hud.Text(overlay,"Quick bindings",new Vector2(.035f,.03f),new Vector2(.965f,.97f),18);
            quickText.enableAutoSizing=false; quickText.overflowMode=TextOverflowModes.Ellipsis;
            overlay.gameObject.SetActive(false);
            requestStatus=hud.Text(hud.Root,"Ride request toggle status",new Vector2(.3f,.87f),new Vector2(.7f,.94f),27);
            requestStatus.alignment=TextAlignmentOptions.Center;
            requestStatus.gameObject.SetActive(false);
            Rebuild();
        }
        public void Open()
        {
            if(page==null || IsOpen) return;
            wasPaused=host.Paused; host.SetPaused(true);
            page.gameObject.SetActive(true); page.SetAsLastSibling();
            Rebuild(); scroll.verticalNormalizedPosition=1; input.Focus(page);
        }
        public void Close()
        {
            if(!IsOpen) return;
            page.gameObject.SetActive(false); input.Focus(null); host.SetPaused(wasPaused);
        }
        private void Update()
        {
            if(input==null || page==null) return;
            input.RefreshDeviceFromHardware();
            if(wheel?.SelectedDevice!=null && wheel.SelectedDevice.wasUpdatedThisFrame) wheelActive=true;
            else if(UnityEngine.InputSystem.Keyboard.current?.wasUpdatedThisFrame==true ||
                UnityEngine.InputSystem.Mouse.current?.wasUpdatedThisFrame==true ||
                UnityEngine.InputSystem.Gamepad.current?.wasUpdatedThisFrame==true) wheelActive=false;
            if(!host.Paused && host.Session!=null && input.ToggleRideRequests.WasPressedThisFrame())
            {
                input.ToggleRideRequestsFor(host.Session);
                requestStatus.text="RIDE REQUESTS: "+(host.Session.RideRequestsEnabled?"ON":"OFF");
                requestStatusUntil=Time.unscaledTime+2f;
                requestStatus.gameObject.SetActive(true);
                requestStatus.transform.SetAsLastSibling();
            }
            if(requestStatus.gameObject.activeSelf && Time.unscaledTime>=requestStatusUntil) requestStatus.gameObject.SetActive(false);
            if(input.UsingGamepad!=lastGamepad || wheelActive!=lastWheel) Rebuild();
            if(IsOpen)
            {
                overlay.gameObject.SetActive(false);
                input.Focus(page);
                return;
            }
            bool show=!host.Paused && input.ShowControls.IsPressed();
            if(overlay.gameObject.activeSelf!=show) overlay.gameObject.SetActive(show);
            if(show) overlay.SetAsLastSibling();
        }
        private void ScrollBy(float amount)
        { if(scroll!=null) scroll.verticalNormalizedPosition=Mathf.Clamp01(scroll.verticalNormalizedPosition+amount); }
        private void Rebuild()
        {
            lastGamepad=input.UsingGamepad;
            lastWheel=wheelActive;
            string device=wheelActive?"WHEEL":lastGamepad?"GAMEPAD":"KEYBOARD / MOUSE";
            string source=wheelActive?"Calibrated wheel profile; UNBOUND where unmapped":"Live actions; FIXED DIRECT READ legacy; MENU BUTTON has no shortcut";
            var all=new StringBuilder("CURRENT DEVICE: ").Append(device).Append('\n').Append(source).Append('\n');
            var quick=new StringBuilder("CONTROLS - ").Append(device).Append('\n').Append(wheelActive?"WHEEL PROFILE":"LEGACY ROWS FIXED / ACTIONS LIVE").Append('\n');
            string category=null;
            foreach(var row in entries)
            {
                if(row.Category!=category) { category=row.Category; all.Append('\n').Append(category).Append('\n'); }
                string binding=wheelActive?row.WheelBinding(wheel?.CalibrationProfile):row.Binding(lastGamepad);
                string line=row.Name+"  -  "+binding;
                all.Append(line);
                if(!wheelActive && row.IsLegacyDirectRead) all.Append("  [FIXED DIRECT READ]");
                if(!string.IsNullOrEmpty(row.Note)) all.Append("  [").Append(row.Note).Append(']');
                all.Append('\n');
                if(row.Quick) quick.Append(row.Name).Append("  ").Append(binding).Append('\n');
            }
            fullText.text=all.ToString(); quickText.text=quick.ToString();
            Canvas.ForceUpdateCanvases();
            float width=page.gameObject.activeInHierarchy?Mathf.Max(1f,viewport.rect.width-12f):300f;
            float height=fullText.GetPreferredValues(fullText.text,width,float.PositiveInfinity).y+24f;
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,height);
        }
    }
}
