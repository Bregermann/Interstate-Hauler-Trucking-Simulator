using System;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace LWS.TruckTaxi
{
    public sealed class TruckTaxiHud : MonoBehaviour
    {
        public GameObject heatButtonPrefab;
        public GameObject heatSliderPrefab;
        public GameObject heatSwitchPrefab;
        public TMP_FontAsset font;
        private TruckTaxiBootstrap host;
        private RectTransform root, modal, ridePanel, pausePanel;
        private TextMeshProUGUI title, details, status, requestText, reaction, speed;
        private UnityEngine.UI.Image portrait;
        private GameObject start, accept, decline, next, resume;
        private GameObject eject;
        private TextMeshProUGUI ejectProgress;
        private TruckTaxiDebugPanel debug;
        private TruckTaxiOfferMap offerMap;
        public TruckTaxiGPSSettingsPanel GPSSettings { get; private set; }
        private UnityEngine.UI.Image offerPortrait;
        private bool showOfferMap=true;
        public TruckTaxiUIInput UIInput { get; private set; }
        private UnityEngine.UI.Image appreciationFade;
        private TextMeshProUGUI appreciationCaption;
        private bool appreciationRunning;
        private readonly System.Collections.Generic.List<CanvasGroup> drivingControls=new System.Collections.Generic.List<CanvasGroup>();
        private float refreshAt;
        public RectTransform Root => root;
        public void Initialize(TruckTaxiBootstrap value)
        {
            host = value;
            var canvasObject = new GameObject("Truck Taxi Canvas",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster));
            canvasObject.transform.SetParent(transform,false);
            root = (RectTransform)canvasObject.transform;
            var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 900;
            var scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920,1080); scaler.matchWidthOrHeight = 0.5f;
            if(EventSystem.current == null) new GameObject("Truck Taxi Event System",typeof(EventSystem),typeof(InputSystemUIInputModule));
            var telemetryBand=Panel(root,"Telemetry background",new Vector2(0,0),new Vector2(.72f,.105f));
            speed = Text(telemetryBand,"Truck telemetry",new Vector2(.025f,.05f),new Vector2(.33f,.91f),30);
            status = Text(telemetryBand,"Shift",new Vector2(.39f,.05f),new Vector2(.99f,.91f),28);
            ridePanel = Panel(root,"Passenger and requests",new Vector2(0.73f,0.33f),new Vector2(0.99f,0.9f));
            portrait = Rect(ridePanel,"Passenger portrait",new Vector2(.05f,.85f),new Vector2(.22f,.98f)).gameObject.AddComponent<UnityEngine.UI.Image>();
            portrait.preserveAspect=true;
            requestText = Text(ridePanel,"Requests",new Vector2(0.05f,0.28f),new Vector2(0.95f,0.82f),24);
            reaction = Text(root,"Passenger reaction",new Vector2(0.025f,0.112f),new Vector2(0.71f,0.185f),28);
            modal = Panel(root,"Ride dispatch",new Vector2(.12f,.17f),new Vector2(.88f,.92f));
            title = Text(modal,"Title",new Vector2(.04f,.86f),new Vector2(.96f,.98f),38);
            details = Text(modal,"Ride details",new Vector2(0.06f,0.23f),new Vector2(0.94f,0.80f),27);
            var mapFrame=Rect(modal,"Offer map frame",new Vector2(.51f,.22f),new Vector2(.96f,.83f));
            var mapRect=Rect(mapFrame,"Compass offer preview",Vector2.zero,Vector2.one);
            var aspect=mapRect.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();
            aspect.aspectMode=UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent; aspect.aspectRatio=1;
            offerMap=mapRect.gameObject.AddComponent<TruckTaxiOfferMap>(); offerMap.Initialize(this,host.GPS);
            offerPortrait=Rect(modal,"Offer portrait",new Vector2(.37f,.86f),new Vector2(.47f,.98f)).gameObject.AddComponent<UnityEngine.UI.Image>();
            offerPortrait.preserveAspect=true; offerPortrait.raycastTarget=false;
            start = Button(modal,"START SHIFT",new Vector2(0.18f,0.06f),new Vector2(0.82f,0.19f),()=>host.StartShift());
            accept = Button(modal,"ACCEPT",new Vector2(0.06f,0.06f),new Vector2(0.48f,0.19f),Accept);
            decline = Button(modal,"DECLINE",new Vector2(0.52f,0.06f),new Vector2(0.94f,0.19f),Decline);
            next = Button(modal,"NEXT FARE",new Vector2(0.18f,0.06f),new Vector2(0.82f,0.19f),()=>host.Session.ContinueShift());
            Button(root,"RESET UPRIGHT",new Vector2(0.73f,0.03f),new Vector2(0.88f,0.09f),()=>host.Player.UprightRecoveryController.RequestResetUpright("Truck Taxi HUD"));
            Button(root,"PAUSE",new Vector2(0.89f,0.03f),new Vector2(0.99f,0.09f),HandlePause);
            Button(root,"DEBUG",new Vector2(0.89f,0.11f),new Vector2(0.99f,0.17f),()=>debug.Toggle());
            Button(root,"GPS ON / OFF",new Vector2(.73f,.11f),new Vector2(.88f,.17f),()=>host.GPS.ToggleHud());
            Button(root,"GPS SETTINGS",new Vector2(.89f,.19f),new Vector2(.99f,.25f),()=>GPSSettings?.Toggle());
            eject=Button(ridePanel,"HOLD TO EJECT",new Vector2(.05f,.08f),new Vector2(.95f,.21f),()=>{});
            var ejectEvents=eject.AddComponent<EventTrigger>();
            var down=new EventTrigger.Entry { eventID=EventTriggerType.PointerDown };
            down.callback.AddListener(_=>host.Passengers.uiEjectHeld=true); ejectEvents.triggers.Add(down);
            foreach(var eventId in new[]{EventTriggerType.PointerUp,EventTriggerType.PointerExit})
            { var up=new EventTrigger.Entry { eventID=eventId }; up.callback.AddListener(_=>host.Passengers.uiEjectHeld=false); ejectEvents.triggers.Add(up); }
            ejectProgress=Text(ridePanel,"Ejection hold progress",new Vector2(.05f,.01f),new Vector2(.95f,.075f),22);
            pausePanel = Panel(root,"Paused",new Vector2(0.31f,0.25f),new Vector2(0.68f,0.76f));
            Text(pausePanel,"PAUSED",new Vector2(0.08f,0.76f),new Vector2(0.92f,0.94f),38).text="SHIFT PAUSED";
            resume = Button(pausePanel,"RESUME",new Vector2(.1f,.64f),new Vector2(.9f,.75f),()=>host.SetPaused(false));
            Button(pausePanel,"GPS SETTINGS",new Vector2(.1f,.52f),new Vector2(.9f,.63f),()=>GPSSettings?.Open());
            Button(pausePanel,"GPS ON / OFF",new Vector2(.1f,.40f),new Vector2(.9f,.51f),()=>host.GPS.ToggleHud());
            Button(pausePanel,"RESET UPRIGHT",new Vector2(.1f,.28f),new Vector2(.9f,.39f),()=>host.Player.UprightRecoveryController.RequestResetUpright("Truck Taxi pause"));
            Button(pausePanel,"END SHIFT",new Vector2(.1f,.16f),new Vector2(.9f,.27f),()=>host.Session.EndShift());
            Button(pausePanel,"QUIT DEMO",new Vector2(.1f,.04f),new Vector2(.9f,.15f),()=>Application.Quit());
            debug = gameObject.AddComponent<TruckTaxiDebugPanel>(); debug.Initialize(host,this);
            if(heatSliderPrefab!=null && heatSwitchPrefab!=null)
            {
                GPSSettings=gameObject.AddComponent<TruckTaxiGPSSettingsPanel>();
                GPSSettings.Initialize(host,this);
            }
            else Debug.LogError("Truck Taxi GPS settings need the Heat slider/switch prefab references on TruckTaxiHud.",this);
            appreciationFade=Rect(root,"Special appreciation fade",Vector2.zero,Vector2.one).gameObject.AddComponent<UnityEngine.UI.Image>();
            appreciationFade.color=Color.clear;
            appreciationCaption=Text(appreciationFade.transform,"Time skip",new Vector2(.15f,.4f),new Vector2(.85f,.6f),36);
            appreciationCaption.alignment=TextAlignmentOptions.Center;
            appreciationFade.gameObject.SetActive(false);
            UIInput=new TruckTaxiUIInput();
            host.Session.Changed+=Refresh;
            Canvas.ForceUpdateCanvases();
            Refresh();
        }
        private void Update()
        {
            if(host == null || host.Session == null) return;
            if(UIInput.Debug.WasPressedThisFrame()) debug.Toggle();
            if(!appreciationRunning)
            {
                if(UIInput.Cancel.WasPressedThisFrame() || UIInput.Pause.WasPressedThisFrame()) HandlePause();
                else if(UIInput.Submit.WasPressedThisFrame())
                {
                    UIInput.SubmitSelected();
                }
            }
            if(host.Session.State==TruckTaxiState.AppreciationSequence && !appreciationRunning) StartCoroutine(Appreciation());
            if(Time.unscaledTime >= refreshAt) { refreshAt = Time.unscaledTime + 0.15f; Refresh(); }
            RefreshFocus();
        }
        private void RefreshFocus() => UIInput?.Focus(appreciationRunning ? null : GPSSettings?.IsOpen==true ? GPSSettings.FocusRoot :
            modal.gameObject.activeSelf ? modal : pausePanel.gameObject.activeSelf ? pausePanel : null);
        private void Accept()
        { if(host.Session.State==TruckTaxiState.AppreciationOffer) host.Session.ChooseAppreciation(true); else host.Session.AcceptRide(); Refresh(); }
        private void Decline()
        { if(host.Session.State==TruckTaxiState.AppreciationOffer) host.Session.ChooseAppreciation(false); else host.Session.DeclineRide(); Refresh(); }
        private System.Collections.IEnumerator Appreciation()
        {
            appreciationRunning=true; appreciationFade.gameObject.SetActive(true); appreciationFade.transform.SetAsLastSibling();
            appreciationCaption.text="";
            for(float t=0;t<.7f;t+=Time.unscaledDeltaTime) { appreciationFade.color=new Color(0,0,0,t/.7f); yield return null; }
            appreciationFade.color=Color.black;
            appreciationCaption.text="A little later...\nThe meter politely minds its own business.";
            yield return new WaitForSecondsRealtime(2.5f);
            appreciationCaption.text="";
            host.Session.CompleteAppreciation();
            for(float t=0;t<.7f;t+=Time.unscaledDeltaTime) { appreciationFade.color=new Color(0,0,0,1-t/.7f); yield return null; }
            appreciationFade.gameObject.SetActive(false); appreciationRunning=false; Refresh();
        }
        private void HandlePause()
        {
            if(GPSSettings!=null && GPSSettings.IsOpen) GPSSettings.Close();
            else if(host.Session.State==TruckTaxiState.RideOffered || host.Session.State==TruckTaxiState.AppreciationOffer) Decline();
            else if(host.Session.State==TruckTaxiState.RideComplete || host.Session.State==TruckTaxiState.RideFailed) host.Session.ContinueShift();
            else if(host.Session.State!=TruckTaxiState.Inactive && host.Session.State!=TruckTaxiState.AppreciationSequence) host.SetPaused(!host.Paused);
            Refresh();
        }
        private void Refresh()
        {
            var s = host.Session;
            bool canEject=s.HasPassenger && s.Passenger!=null && s.Passenger.canBeEjected && !host.Paused;
            eject.SetActive(canEject); ejectProgress.gameObject.SetActive(canEject);
            ejectProgress.text=host.Passengers.EjectionHoldProgress>0 ? "EJECTING  "+(host.Passengers.EjectionHoldProgress*100).ToString("0")+"%" : "HOLD F / VIEW TO EJECT";
            bool offered=s.State==TruckTaxiState.RideOffered, inactive=s.State==TruckTaxiState.Inactive;
            bool appreciation=s.State==TruckTaxiState.AppreciationOffer;
            bool ended=s.State==TruckTaxiState.RideComplete || s.State==TruckTaxiState.RideFailed;
            modal.gameObject.SetActive(offered||inactive||ended||appreciation);
            start.SetActive(inactive); accept.SetActive(offered||appreciation); decline.SetActive(offered||appreciation); next.SetActive(ended);
            bool gpsSettings=GPSSettings!=null && GPSSettings.IsOpen;
            pausePanel.gameObject.SetActive(host.Paused && !inactive && !ended && !offered && !appreciation && !appreciationRunning && !gpsSettings);
            if(pausePanel.gameObject.activeSelf) modal.gameObject.SetActive(false);
            if(gpsSettings) modal.gameObject.SetActive(false);
            offerMap.transform.parent.gameObject.SetActive(offered && showOfferMap);
            offerMap.Show(offered ? s.Offer : null);
            offerPortrait.sprite=offered ? s.Passenger?.portrait : null;
            offerPortrait.gameObject.SetActive(offerPortrait.sprite!=null);
            details.rectTransform.anchorMin=new Vector2(.04f,.24f);
            details.rectTransform.anchorMax=new Vector2(offered ? .47f : .94f,.83f);
            status.text = "TRUCK TAXI   |   " + s.State.ToString().ToUpperInvariant() + "\nSHIFT " + Money(s.ShiftEarnings) + "   /   " + s.CompletedRides+" RIDES";
            float mph=host.Player!=null ? host.Player.GetComponent<Rigidbody>().linearVelocity.magnitude*2.236936f : 0;
            speed.text=$"{mph:0} MPH\nTRACTOR TAXI";
            title.text=inactive ? "TRUCK TAXI" : offered ? "RIDE REQUEST" : s.State==TruckTaxiState.RideComplete ? "FARE COMPLETE" : "RIDE ENDED";
            if(appreciation) title.text="SPECIAL APPRECIATION";
            if(inactive) details.text="SHIFT EARNINGS\n"+Money(s.ShiftEarnings)+"\n\nDOWNTOWN / RESIDENTIAL / INDUSTRIAL";
            else if(appreciation) details.text=s.Passenger.passengerName+" offers a special thank-you for an excellent ride.\n\nAccept a private moment, or politely decline.\n\n["+UIInput.Hint(UIInput.Submit)+"] ACCEPT     ["+UIInput.Hint(UIInput.Cancel)+"] DECLINE";
            else if(offered)
            {
                var offer=s.Offer;
                details.text=$"{offer.Passenger.passengerName}\nPASSENGER RATING: {offer.Passenger.passengerRating:0.0}\n{offer.Passenger.personality}\n\nA  PICKUP\n{offer.Pickup.locationName}\nDISTANCE TO PICKUP  {offer.ToPickup.Meters/1609.344f:0.00} mi\n\nB  DESTINATION\n{offer.Destination.locationName}\nTRIP DISTANCE  {offer.Trip.Meters/1609.344f:0.00} mi\n\nESTIMATED FARE  {Money(offer.EstimatedFareCents)}\n[{UIInput.Hint(UIInput.Submit)}] ACCEPT   [{UIInput.Hint(UIInput.Cancel)}] DECLINE";
                if(!offer.ToPickup.Navigable || !offer.Trip.Navigable) details.text+="\nSTRAIGHT-LINE FALLBACK";
            }
            else if(s.LastFare!=null)
            {
                var f=s.LastFare;
                details.text=$"THIS RIDE: {f.Rating} STARS\nDRIVER AVERAGE: {s.DriverAverageText} STARS\n\nBASE {Money(f.Base)}  /  DISTANCE {Money(f.Distance)}\nTIME {Money(f.Time)}  /  REQUESTS {Money(f.Requests)}\nCHAOS +{f.ChaosScore}  /  TIP {Money(f.Tip)}\nPENALTIES -{Money(f.Penalties)}\nFARE {Money(f.Total)}"+(s.SpecialAppreciationAccepted ? "\nSPECIAL APPRECIATION" : "");
            }
            else if(ended) details.text=s.Reaction;
            bool active = s.State==TruckTaxiState.DrivingToPickup || s.State==TruckTaxiState.PassengerBoarding || s.HasPassenger;
            ridePanel.gameObject.SetActive(active && !gpsSettings);
            if(active)
            {
                portrait.sprite=s.Passenger.portrait;
                portrait.gameObject.SetActive(portrait.sprite!=null);
                var target=s.HasPassenger ? s.Destination : s.Pickup;
                float distance=Vector3.Distance(host.Player.transform.position,target.StopPosition);
                var b=new StringBuilder();
                b.AppendLine(s.Passenger.passengerName).AppendLine(target.locationName+"  /  "+distance.ToString("0")+" m");
                b.AppendLine(s.HasPassenger ? "ON BOARD" : host.PickupZone.Feedback);
                b.AppendLine("FARE "+Money(s.EstimateFare().Total)+" | PROJECTED "+TruckTaxiSession.StarsForSatisfaction(s.Satisfaction)+" STARS");
                b.AppendLine("CHAOS  "+s.ChaosScore+"\n");
                for(int i=Mathf.Max(0,s.Requests.Count-3);i<s.Requests.Count;i++)
                {
                    var r=s.Requests[i];
                    b.AppendLine(r.Description);
                    b.AppendLine(r.State==TaxiRequestState.Active ? $"{r.ProgressText}  |  {r.Remaining:0}s" : r.State.ToString().ToUpperInvariant());
                }
                requestText.text=b.ToString();
            }
            reaction.text=!string.IsNullOrEmpty(host.Passengers.Dialogue.Subtitle) ? host.Passengers.Dialogue.Subtitle : s.ReactionAge<5 && (active || s.State==TruckTaxiState.PassengerEjected) ? s.Reaction : "";
            bool blocked=modal.gameObject.activeSelf || pausePanel.gameObject.activeSelf || gpsSettings || appreciationRunning;
            foreach(var group in drivingControls) { group.interactable=!blocked; group.blocksRaycasts=!blocked; }
            RefreshFocus();
        }
        public static string Money(long cents) => "$"+(cents/100m).ToString("0.00");
        public void SetOfferMapVisible(bool visible) { showOfferMap=visible; Refresh(); }
        public RectTransform Panel(Transform parent,string name,Vector2 min,Vector2 max)
        {
            var rect=Rect(parent,name,min,max);
            rect.gameObject.AddComponent<UnityEngine.UI.Image>().color=new Color(0.045f,0.055f,0.06f,0.94f);
            return rect;
        }
        public static RectTransform Rect(Transform parent,string name,Vector2 min,Vector2 max)
        {
            var go=new GameObject(name,typeof(RectTransform)); var rect=(RectTransform)go.transform;
            rect.SetParent(parent,false); rect.anchorMin=min; rect.anchorMax=max; rect.offsetMin=rect.offsetMax=Vector2.zero; return rect;
        }
        public TextMeshProUGUI Text(Transform parent,string name,Vector2 min,Vector2 max,float size)
        {
            var t=Rect(parent,name,min,max).gameObject.AddComponent<TextMeshProUGUI>();
            if(font!=null) t.font=font;
            t.fontSize=size; t.enableAutoSizing=true; t.fontSizeMin=Mathf.Min(22,size); t.fontSizeMax=size;
            t.color=new Color(0.96f,0.96f,0.91f); t.raycastTarget=false;
            t.overflowMode=TextOverflowModes.Ellipsis; return t;
        }
        public GameObject Button(Transform parent,string text,Vector2 min,Vector2 max,Action click)
        {
            GameObject go;
            if(heatButtonPrefab!=null)
            {
                go=Instantiate(heatButtonPrefab,parent,false); go.name=text;
                if(parent==root) drivingControls.Add(go.AddComponent<CanvasGroup>());
                var rect=go.GetComponent<RectTransform>(); rect.anchorMin=min; rect.anchorMax=max; rect.offsetMin=rect.offsetMax=Vector2.zero;
                Component heat=null;
                foreach(var c in go.GetComponents<Component>()) if(c.GetType().FullName=="Michsky.UI.Heat.ButtonManager") heat=c;
                if(heat!=null)
                {
                    var type=heat.GetType();
                    type.GetField("buttonText").SetValue(heat,text);
                    type.GetField("autoFitContent").SetValue(heat,false);
                    type.GetField("useLocalization").SetValue(heat,false);
                    type.GetField("useSounds").SetValue(heat,false);
                    type.GetField("checkForDoubleClick").SetValue(heat,false);
                    type.GetField("useUINavigation").SetValue(heat,true);
                    type.GetMethod("AddUINavigation").Invoke(heat,null);
                    ((UnityEvent)type.GetField("onClick").GetValue(heat)).AddListener(()=>click());
                    type.GetMethod("UpdateUI").Invoke(heat,null);
                    foreach(var fitter in go.GetComponentsInChildren<UnityEngine.UI.ContentSizeFitter>(true)) fitter.enabled=false;
                    foreach(var label in go.GetComponentsInChildren<TextMeshProUGUI>(true))
                    { label.enableAutoSizing=true; label.fontSizeMin=18; label.fontSizeMax=26; }
                    return go;
                }
            }
            else go=Rect(parent,text,min,max).gameObject;
            var background=go.GetComponent<UnityEngine.UI.Image>()??go.AddComponent<UnityEngine.UI.Image>();
            background.color=new Color(0.17f,0.34f,0.31f);
            var button=go.GetComponent<UnityEngine.UI.Button>()??go.AddComponent<UnityEngine.UI.Button>();
            button.onClick.AddListener(()=>click());
            Text(go.transform,text,new Vector2(.05f,.1f),new Vector2(.95f,.9f),26).text=text;
            return go;
        }
        private void OnDestroy() { if(host?.Session!=null) host.Session.Changed-=Refresh; UIInput?.Dispose(); }
    }
}
