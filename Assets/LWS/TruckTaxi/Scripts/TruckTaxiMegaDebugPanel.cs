using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // Development controls only; gameplay state remains owned by the existing services.
    public sealed class TruckTaxiMegaDebugPanel : MonoBehaviour
    {
        private TruckTaxiBootstrap host;
        private TruckTaxiHud hud;
        private RectTransform panel;
        private static readonly string[] Tabs = { "CLOCK", "EVENTS", "WEATHER", "EXTREMES", "WORLD", "KAIJU", "EXTRAS", "ANALYTICS" };
        private readonly RectTransform[] pages = new RectTransform[8];
        private TextMeshProUGUI heading, status, clockText, eventText, zoneText, countsText;
        private TextMeshProUGUI[] eventRows;
        private TextMeshProUGUI kaijuText, extrasText;
        private TextMeshProUGUI analyticsText;
        private GameObject[] eventSelectors;
        private int page, eventPage, selectedEvent, zoneIndex;
        private DateTime editDate;
        private int editHour, editMinute;
        private string lastRivalId, message = "";
        private float nextRefresh;
        public bool IsOpen => panel != null && panel.gameObject.activeSelf;
        public Transform FocusRoot => panel;
        public static bool IsAvailable
        {
            get
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                return true;
#else
                return false;
#endif
            }
        }
        // Parent-owned map and collider tools are deliberately injected, not duplicated here.
        public Action OpenWorkAreaMap { get; set; }
        public Action ToggleSurgeZones { get; set; }
        public Action TogglePassengerImpactColliders { get; set; }
        public Action ToggleBusColliders { get; set; }
        public Action ToggleTrafficColliders { get; set; }
        public Action ToggleObjectiveTimers { get; set; }

        public void Initialize(TruckTaxiBootstrap value, TruckTaxiHud view)
        {
            if (!IsAvailable) return;
            host = value; hud = view;
            panel = view.Panel(view.Root, "Mega debug", new Vector2(.08f, .07f), new Vector2(.92f, .94f));
            heading = view.Text(panel, "Mega debug heading", new Vector2(.035f, .875f), new Vector2(.72f, .975f), 32);
            view.Button(panel, "CLOSE", new Vector2(.78f, .89f), new Vector2(.97f, .97f), Close);
            var tabs = Tabs;
            for (int i = 0; i < tabs.Length; i++)
            {
                int index = i;
                float width = .94f / tabs.Length, left = .03f + i * width;
                var tabButton = view.Button(panel, tabs[i], new Vector2(left, .78f), new Vector2(left + width - .008f, .86f), () => ShowPage(index));
                var tabText = tabButton.GetComponentInChildren<TextMeshProUGUI>();
                if (tabText != null) { tabText.enableAutoSizing = true; tabText.fontSizeMin = 10; tabText.fontSizeMax = 22; }
                pages[i] = TruckTaxiHud.Rect(panel, tabs[i] + " tools", new Vector2(.03f, .17f), new Vector2(.97f, .76f));
                pages[i].gameObject.SetActive(i == 0);
            }
            status = view.Text(panel, "Mega debug status", new Vector2(.035f, .035f), new Vector2(.965f, .155f), 23);
            BuildClock(); BuildEvents(); BuildWeather(); BuildExtremes(); BuildWorld(); BuildKaiju(); BuildExtras(); BuildAnalytics();
            panel.gameObject.SetActive(false);
        }
        public void Open()
        {
            if (!IsAvailable || panel == null || IsOpen || host == null || !host.Ready) return;
            var now = host.CalendarWeather?.CurrentDateTime ?? new DateTime(2026,6,1);
            editDate = now.Date; editHour = now.Hour; editMinute = now.Minute;
            panel.gameObject.SetActive(true); panel.SetAsLastSibling();
            Refresh(); hud.UIInput?.Focus(panel);
        }
        public void Close()
        {
            if (!IsOpen) return;
            panel.gameObject.SetActive(false);
            hud.UIInput?.Focus(null);
        }
        public void Toggle() { if (IsOpen) Close(); else Open(); }
        private void ShowPage(int index)
        {
            page = index;
            for (int i = 0; i < pages.Length; i++) pages[i].gameObject.SetActive(i == page);
            Refresh(); hud.UIInput?.Focus(panel);
        }
        private void Update()
        {
            if (!IsOpen || Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 1f;
            Refresh();
        }
        private void BuildClock()
        {
            var root = pages[0];
            clockText = hud.Text(root, "Clock values", new Vector2(.02f, .80f), new Vector2(.98f, .99f), 26);
            Stepper(root, "YEAR", .66f, n => ChangeDate(() => editDate.AddYears(n)));
            Stepper(root, "MONTH", .52f, n => ChangeDate(() => editDate.AddMonths(n)));
            Stepper(root, "DAY", .38f, n => ChangeDate(() => editDate.AddDays(n)));
            Stepper(root, "HOUR", .24f, n => { editHour = (editHour + n + 24) % 24; Refresh(); });
            Stepper(root, "MINUTE", .10f, n => { editMinute = (editMinute + n * 5 + 60) % 60; Refresh(); });
            hud.Button(root, "SET DATE", new Vector2(.51f, .67f), new Vector2(.96f, .78f), () => Run(() => host.CalendarWeather.SetDate(editDate.Year, editDate.Month, editDate.Day), "Date set"));
            hud.Button(root, "SET TIME", new Vector2(.51f, .53f), new Vector2(.96f, .64f), () =>
            {
                host.CalendarWeather?.SetTime(editHour + editMinute / 60f);
                Message("Time set");
            });
            hud.Button(root, "ADVANCE DAY", new Vector2(.51f, .39f), new Vector2(.96f, .50f), () =>
            {
                host.CalendarWeather?.AdvanceDay(); Message("Advanced one game day");
            });
        }
        private void Stepper(Transform root, string label, float bottom, Action<int> change)
        {
            hud.Text(root, label, new Vector2(.02f, bottom), new Vector2(.22f, bottom + .11f), 23).text = label;
            hud.Button(root, "-", new Vector2(.23f, bottom), new Vector2(.35f, bottom + .11f), () => change(-1));
            hud.Button(root, "+", new Vector2(.37f, bottom), new Vector2(.49f, bottom + .11f), () => change(1));
        }
        private void ChangeDate(Func<DateTime> change)
        {
            try { editDate = change(); Refresh(); }
            catch (ArgumentOutOfRangeException) { Message("Date limit reached"); }
        }
        private void BuildEvents()
        {
            var root = pages[1];
            eventText = hud.Text(root, "Selected event", new Vector2(.02f, .78f), new Vector2(.98f, .99f), 24);
            eventRows = new TextMeshProUGUI[3]; eventSelectors = new GameObject[3];
            for (int i = 0; i < 3; i++)
            {
                int row = i;
                float top = .75f - i * .17f;
                eventRows[i] = hud.Text(root, "Event row " + i, new Vector2(.02f, top - .13f), new Vector2(.76f, top), 23);
                eventSelectors[i] = hud.Button(root, "SELECT", new Vector2(.78f, top - .12f), new Vector2(.98f, top), () =>
                { selectedEvent = eventPage * 3 + row; Refresh(); });
            }
            hud.Button(root, "PREV", new Vector2(.02f, .05f), new Vector2(.18f, .18f), () => ChangeEventPage(-1));
            hud.Button(root, "NEXT", new Vector2(.20f, .05f), new Vector2(.36f, .18f), () => ChangeEventPage(1));
            hud.Button(root, "FORCE NEXT", new Vector2(.38f, .05f), new Vector2(.58f, .18f), ForceNextEvent);
            hud.Button(root, "START SELECTED", new Vector2(.60f, .05f), new Vector2(.79f, .18f), () => EventAction(true));
            hud.Button(root, "END EVENT", new Vector2(.81f, .05f), new Vector2(.98f, .18f), () => EventAction(false));
        }
        private void BuildWeather()
        {
            var root = pages[2];
            hud.Text(root, "Weather heading", new Vector2(.02f, .87f), new Vector2(.98f, .99f), 24).text = "FORCE CURRENT FORECAST CONDITION";
            var kinds = (TruckTaxiWeatherCondition[])Enum.GetValues(typeof(TruckTaxiWeatherCondition));
            for (int i = 0; i < kinds.Length; i++)
            {
                var kind = kinds[i];
                int col = i % 2, row = i / 2;
                float left = .02f + col * .49f, top = .84f - row * .15f;
                hud.Button(root, kind.ToString().ToUpperInvariant(), new Vector2(left, top - .12f), new Vector2(left + .47f, top),
                    () => Run(() => host.CalendarWeather != null && host.CalendarWeather.ForceForecast(kind), kind + " forecast forced"));
            }
        }
        private void BuildExtremes()
        {
            var root = pages[3];
            zoneText = hud.Text(root, "Hazard zone", new Vector2(.02f, .83f), new Vector2(.98f, .99f), 24);
            hud.Button(root, "PREV ZONE", new Vector2(.02f, .69f), new Vector2(.24f, .81f), () => ChangeZone(-1));
            hud.Button(root, "NEXT ZONE", new Vector2(.26f, .69f), new Vector2(.48f, .81f), () => ChangeZone(1));
            hud.Button(root, "CLEAR EXTREME EVENTS", new Vector2(.51f, .69f), new Vector2(.98f, .81f), () =>
            { host.Extremes?.Clear(); Message("Extreme events cleared"); });
            var kinds = (TruckTaxiExtremeKind[])Enum.GetValues(typeof(TruckTaxiExtremeKind));
            for (int i = 0; i < kinds.Length; i++)
            {
                var kind = kinds[i];
                int col = i % 2, row = i / 2;
                float left = .02f + col * .49f, top = .65f - row * .18f;
                hud.Button(root, "FORCE " + kind.ToString().ToUpperInvariant(),
                    new Vector2(left, top - .14f), new Vector2(left + .47f, top), () => ForceExtreme(kind));
            }
        }
        private void BuildWorld()
        {
            var root = pages[4];
            countsText = hud.Text(root, "World counts", new Vector2(.02f, .58f), new Vector2(.98f, .99f), 22);
            string[] labels = { "SHOW SURGE ZONES", "SET WORK AREA", "CLEAR WORK AREA", "SPAWN RIVAL TAXI",
                "FORCE RIVAL PICKUP", "PASSENGER COLLIDERS", "BUS COLLIDERS", "TRAFFIC COLLIDERS", "OBJECTIVE TIMERS" };
            Action[] actions = { () => InvokeHook(ToggleSurgeZones, "Surge overlay unavailable"),
                () => InvokeHook(OpenWorkAreaMap, "Work Area map unavailable"),
                () => { host.Session?.SetWorkAreaPreset(TruckTaxiWorkAreaMode.AnywhereNearMe); Message("Work Area cleared"); },
                SpawnRival, ForceRivalPickup,
                () => InvokeHook(TogglePassengerImpactColliders, "Passenger collider tool unavailable"),
                () => InvokeHook(ToggleBusColliders, "Bus collider tool unavailable"),
                () => InvokeHook(ToggleTrafficColliders, "Traffic collider tool unavailable"),
                () => InvokeHook(ToggleObjectiveTimers, "Objective timer tool unavailable") };
            for (int i = 0; i < labels.Length; i++)
            {
                int index = i, col = i % 3, row = i / 3;
                float left = .02f + col * .326f, top = .55f - row * .17f;
                hud.Button(root, labels[i], new Vector2(left, top - .13f), new Vector2(left + .31f, top), actions[index]);
            }
        }
        private void ChangeEventPage(int delta)
        {
            int count = host.Venues?.GetNextSevenDays()?.Count ?? 0;
            int pagesCount = Mathf.Max(1, Mathf.CeilToInt(count / 3f));
            eventPage = (eventPage + delta + pagesCount) % pagesCount;
            selectedEvent = eventPage * 3;
            Refresh();
        }
        private void EventAction(bool start)
        {
            var events = host.Venues?.GetNextSevenDays();
            if (events == null || selectedEvent >= events.Count) { Message("No selected event"); return; }
            bool ok = start ? host.Venues.ForceStart(events[selectedEvent].EventId) :
                host.Venues.ForceEnd(events[selectedEvent].EventId);
            Message(ok ? (start ? "Event started" : "Event ended") : "Event command rejected");
        }
        private void ForceNextEvent()
        {
            var events = host.Venues?.GetNextSevenDays();
            DateTime now = host.CalendarWeather?.CurrentDateTime ?? DateTime.MinValue;
            if (events != null)
                foreach (var item in events)
                    if (item.Start > now) { Run(() => host.Venues.ForceStart(item.EventId), "Next event started"); return; }
            Message("No upcoming event");
        }
        private IReadOnlyList<TruckTaxiHazardZone> Zones => host?.WorldCoordinator?.HazardZones;
        private void ChangeZone(int delta)
        {
            int count = Zones?.Count ?? 0;
            if (count > 0) zoneIndex = (zoneIndex + delta + count) % count;
            Refresh();
        }
        private void ForceExtreme(TruckTaxiExtremeKind kind)
        {
            Run(() => host.Extremes!=null && host.Extremes.StartEventHere(kind), kind+" started near player");
        }
        private void BuildKaiju()
        {
            var root=pages[5];
            kaijuText=hud.Text(root,"Kaiju state",new Vector2(.02f,.70f),new Vector2(.98f,.99f),23);
            string[] labels={"SIGHTING HERE","ATTACK HERE","GRAB CAR","THROW CAR","FIRE BREATH","GRAB PLAYER","END KAIJU"};
            Action[] commands={()=>Run(()=>host.Kaiju?.ForceSightingHere()==true,"Sighting started"),
                ()=>Run(()=>host.Kaiju?.ForceAttackHere()==true,"Attack started"),
                ()=>Run(()=>host.Kaiju?.ForceGrabCar()==true,"Car grabbed; default action eats after hold"),
                ()=>Run(()=>host.Kaiju?.ForceThrowCar()==true,"Held car thrown"),
                ()=>Run(()=>host.Kaiju?.ForceFire()==true,"Fire warning started"),
                ()=>Run(()=>host.Kaiju?.ForceGrabPlayer()==true,"Player capture warning started"),
                ()=>{host.Kaiju?.EndKaiju(); Message("Kaiju cleared");}};
            ToolGrid(root,labels,commands,.66f);
        }
        private void BuildExtras()
        {
            var root=pages[6];
            extrasText=hud.Text(root,"Addendum state",new Vector2(.02f,.72f),new Vector2(.98f,.99f),22);
            extrasText.enableAutoSizing=true; extrasText.fontSizeMin=14; extrasText.fontSizeMax=22;
            string[] labels={"START ROAD PEP","PERFECT","MESSY","BOTCHED","CLEAR ROAD PEP","WORLD COLLIDERS","RADIO BULLETIN","ALERT STACK TEST","RACE REGISTRATION"};
            Action[] commands={()=>{Close(); host.SetPaused(false); host.DriverNeeds?.DebugStartRoadPep();},
                ()=>host.DriverNeeds?.DebugForceRoadPep(100),()=>host.DriverNeeds?.DebugForceRoadPep(55),
                ()=>host.DriverNeeds?.DebugForceRoadPep(10),()=>host.DriverNeeds?.DebugClearRoadPep(),
                ()=> (host.GetComponent<TruckTaxiColliderDiagnostics>() ?? host.gameObject.AddComponent<TruckTaxiColliderDiagnostics>()).ToggleSolidWorld(),
                ()=>host.Radio?.WeatherBulletin(),
                ()=>{hud.Alerts.Publish("test-critical","EMERGENCY ALERT TEST",TruckTaxiAlertPriority.Critical);
                    hud.Alerts.Publish("test-secondary","Secondary notification test");
                    hud.Alerts.Publish("test-queued","Queued notification test"); Close();},
                ()=>{Close(); host.Races?.OpenRegistration();}};
            ToolGrid(root,labels,commands,.66f);
        }
        private void ToolGrid(Transform root,string[] labels,Action[] actions,float top)
        {
            for(int i=0;i<labels.Length;i++)
            {
                int n=i; float left=.02f+(i%3)*.326f, y=top-(i/3)*.18f;
                hud.Button(root,labels[i],new Vector2(left,y-.14f),new Vector2(left+.31f,y),()=>actions[n]());
            }
        }
        private void BuildAnalytics()
        {
            var root = pages[7];
            analyticsText = hud.Text(root, "Analytics state", new Vector2(.02f, .19f), new Vector2(.98f, .99f), 22);
            analyticsText.enableAutoSizing = true; analyticsText.fontSizeMin = 14; analyticsText.fontSizeMax = 22;
            hud.Button(root, "EXPORT CSV", new Vector2(.02f, .02f), new Vector2(.34f, .15f), () =>
            {
                var analytics = host.GetComponent<TruckTaxiAnalytics>();
                if (analytics == null || !analytics.IsInitialized) { Message("Analytics not initialized"); return; }
                analytics.ExportCsv(); Message("CSV export queued");
            });
        }
        private void RefreshAnalytics()
        {
            if (analyticsText == null || page != 7) return;
            var analytics = host.GetComponent<TruckTaxiAnalytics>();
            if (analytics == null || !analytics.IsInitialized) { analyticsText.text = "ANALYTICS NOT INITIALIZED"; return; }
            var m = analytics.Metrics;
            analyticsText.text = $"PLAYTIME {m.PlaySeconds / 60:0.0} min   PAUSED {m.PauseSeconds / 60:0.0} min" +
                $"\nRIDES {analytics.Rides}   UNIQUE PASSENGERS {analytics.UniquePassengers}" +
                $"\nROSTER COVERAGE {analytics.RosterCoveragePercent:0.0}% ({analytics.RosterCount} authored IDs)" +
                $"\nFPS {(analytics.Fps < 0 ? "N/A" : analytics.Fps.ToString("0.0"))}   AVG FPS {m.AverageFps:0.0}" +
                $"\nDISTANCE {m.DistanceMeters / 1000:0.00} km   CRASHES {analytics.Crashes}   REGIONS {analytics.RegionsVisited}" +
                $"\nPENDING UPLOAD EVENTS {analytics.PendingUploadEvents}   DROPPED {analytics.DroppedEvents}" +
                $"\nUPLOAD {analytics.LastUploadResult}   LOCAL {analytics.LastLocalResult}";
        }
        private void SpawnRival()
        {
            lastRivalId = host.Rivals?.DebugSpawn(host.Player.transform.position);
            Message(string.IsNullOrEmpty(lastRivalId) ? "Rival spawn rejected" : "Spawned rival " + lastRivalId);
        }
        private void ForceRivalPickup()
        {
            Run(() => !string.IsNullOrEmpty(lastRivalId) && host.Rivals != null && host.Rivals.ForcePickup(lastRivalId),
                "Rival pickup forced");
        }
        private void InvokeHook(Action hook, string missing)
        {
            if (hook == null) { Message(missing); return; }
            hook(); Message("Tool toggled");
        }
        private void Run(Func<bool> action, string success) => Message(action != null && action() ? success : "Command rejected");
        private void Message(string text) { message = text; Refresh(); }
        public void Refresh()
        {
            if (panel == null || host == null) return;
            heading.text = "MEGA DEBUG  /  " + Tabs[page];
            status.text = message;
            RefreshAnalytics();
            if(kaijuText!=null) kaijuText.text="KAIJU  "+(host.Kaiju?.Kind.ToString() ?? "UNAVAILABLE")+
                "\nNEXT SIGHTING  "+host.Kaiju?.NextNaturalSighting.ToString("ddd HH:mm")+
                "   ATTACK  "+host.Kaiju?.NextNaturalAttack.ToString("ddd HH:mm");
            if(extrasText!=null) extrasText.text="ROAD PEP  "+(host.DriverNeeds?.RoadPepChallenge?.Score.ToString("0") ?? "IDLE")+
                "\nRADIO  "+(host.Radio?.Display ?? "UNAVAILABLE")+
                "\nFAST TRAVEL  "+(host.FastTravel?.Feedback ?? "IDLE")+
                "\nCUSTOM RUNNER  "+(host.CustomContent?.Status ?? "UNAVAILABLE");
            var calendar = host.CalendarWeather;
            if (calendar != null)
                clockText.text = "GAME CLOCK  " + calendar.CurrentDateTime.ToString("ddd, MMM d yyyy  h:mm tt") +
                    "\nEDIT DATE  " + editDate.ToString("yyyy-MM-dd") + "    EDIT TIME  " + editHour.ToString("00") + ":" + editMinute.ToString("00");
            var events = host.Venues?.GetNextSevenDays();
            int count = events?.Count ?? 0;
            eventPage = Mathf.Clamp(eventPage, 0, Mathf.Max(0, Mathf.CeilToInt(count / 3f) - 1));
            selectedEvent = Mathf.Clamp(selectedEvent, 0, Mathf.Max(0, count - 1));
            eventText.text = count == 0 ? "NO SCHEDULED EVENTS" : "SELECTED  " + events[selectedEvent].EventName +
                "  /  " + events[selectedEvent].VenueName + "  /  " + events[selectedEvent].Status;
            for (int i = 0; i < 3; i++)
            {
                int index = eventPage * 3 + i;
                bool visible = index < count;
                eventRows[i].gameObject.SetActive(visible); eventSelectors[i].SetActive(visible);
                if (visible) eventRows[i].text = events[index].Start.ToString("ddd h:mm tt") + "  " +
                    events[index].EventName + "  /  " + events[index].VenueName;
            }
            var zones = Zones;
            zoneText.text = zones == null || zones.Count == 0 ? "NO AUTHORED HAZARD ZONES" :
                "ZONE " + (zoneIndex + 1) + "/" + zones.Count + "  " + zones[Mathf.Clamp(zoneIndex, 0, zones.Count - 1)]?.displayName +
                "  /  " + zones[Mathf.Clamp(zoneIndex, 0, zones.Count - 1)]?.kind;
            if(host.Extremes!=null && zones!=null && zones.Count>0)
            {
                var schedule=host.Extremes.GetScheduleStatus(zones[Mathf.Clamp(zoneIndex,0,zones.Count-1)].kind);
                zoneText.text+=$"\nELIGIBLE {schedule.Eligible}  CHANCE {schedule.Probability:P0}  DRY DAYS {schedule.EligibleDryDays}  {schedule.RejectionReason}";
            }
            var policy = host.Session?.IntercityPolicy;
            countsText.text = "TRAFFIC  LOGICAL " + (host.traffic?.LogicalCount ?? 0) + "  LIVE " + (host.traffic?.ActiveCount ?? 0) +
                "     PEDESTRIANS  LOGICAL " + (host.pedestrians?.LogicalCount ?? 0) + "  LIVE " + (host.pedestrians?.ActiveCount ?? 0) +
                "\nRIVALS  LOGICAL " + (host.Rivals?.LogicalCount ?? 0) + "  LIVE " + (host.Rivals?.LiveCount ?? 0) +
                "     EXTREMES  " + (host.Extremes?.ActiveSnapshots.Count ?? 0) +
                "\nWORK AREA  " + (host.Session?.WorkArea?.label ?? "NONE") +
                "     LOCAL STREAK  " + (policy?.CompletedLocalStreak ?? 0) +
                "  /  THRESHOLD  " + (policy?.Threshold ?? 0) +
                "     COOLDOWN  " + (policy?.LocalCooldownRemaining ?? 0) +
                "     GUARANTEE  " + (policy?.GuaranteePending ?? false);
        }
    }
}
