using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public sealed class TruckTaxiPlanningPanel : MonoBehaviour
    {
        private static readonly string[] Tabs = { "TODAY", "EVENTS", "CALENDAR", "WEATHER", "SURGE" };
        private TruckTaxiBootstrap host;
        private TruckTaxiHud hud;
        private RectTransform panel;
        private TextMeshProUGUI heading, detail, pageLabel;
        private readonly TextMeshProUGUI[] rows = new TextMeshProUGUI[4];
        private readonly GameObject[] selects = new GameObject[4];
        private GameObject routeButton;
        private readonly List<TruckTaxiVenueEventSnapshot> events = new List<TruckTaxiVenueEventSnapshot>();
        private readonly List<TruckTaxiForecastEntry> weather = new List<TruckTaxiForecastEntry>();
        private int tab, page, selected, day;
        private string feedback = "";
        private bool wasPaused;
        private float nextRefresh;
        public bool IsOpen => panel != null && panel.gameObject.activeSelf;
        public Transform FocusRoot => panel;

        public void Initialize(TruckTaxiBootstrap value, TruckTaxiHud view)
        {
            host = value; hud = view;
            panel = view.Panel(view.Root, "Planning", new Vector2(.10f, .08f), new Vector2(.90f, .94f));
            heading = view.Text(panel, "Planning heading", new Vector2(.035f, .87f), new Vector2(.76f, .98f), 34);
            view.Button(panel, "CLOSE", new Vector2(.80f, .89f), new Vector2(.97f, .97f), Close);
            for (int i = 0; i < Tabs.Length; i++)
            {
                int index = i;
                float left = .03f + i * .19f;
                view.Button(panel, Tabs[i], new Vector2(left, .77f), new Vector2(left + .18f, .85f), () => SelectTab(index));
            }
            for (int i = 0; i < rows.Length; i++)
            {
                int index = i;
                float top = .745f - i * .12f;
                rows[i] = view.Text(panel, "Planning row " + i, new Vector2(.045f, top - .095f), new Vector2(.76f, top), 24);
                selects[i] = view.Button(panel, "VIEW", new Vector2(.79f, top - .09f), new Vector2(.96f, top), () => SelectRow(index));
            }
            detail = view.Text(panel, "Planning detail", new Vector2(.045f, .12f), new Vector2(.96f, .27f), 24);
            pageLabel = view.Text(panel, "Planning page", new Vector2(.46f, .025f), new Vector2(.60f, .105f), 22);
            pageLabel.alignment = TextAlignmentOptions.Center;
            view.Button(panel, "PREVIOUS", new Vector2(.04f, .025f), new Vector2(.23f, .105f), () => ChangePage(-1));
            view.Button(panel, "NEXT", new Vector2(.25f, .025f), new Vector2(.44f, .105f), () => ChangePage(1));
            routeButton = view.Button(panel, "ROUTE TO VENUE", new Vector2(.65f, .025f), new Vector2(.96f, .105f), RouteToVenue);
            panel.gameObject.SetActive(false);
        }
        public void Open()
        {
            if (panel == null || IsOpen || host == null || !host.Ready) return;
            wasPaused = host.Paused;
            host.SetPaused(true);
            panel.gameObject.SetActive(true);
            panel.SetAsLastSibling();
            feedback = "";
            Refresh();
            hud.UIInput?.Focus(panel);
        }
        public void Close()
        {
            if (!IsOpen) return;
            panel.gameObject.SetActive(false);
            if (!wasPaused && host != null) host.SetPaused(false);
            hud.UIInput?.Focus(null);
        }
        private void Update()
        {
            if (!IsOpen || Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 1f;
            Refresh();
        }
        private void SelectTab(int value)
        {
            tab = value; page = 0; selected = 0; day = 0;
            feedback = "";
            Refresh();
            hud.UIInput?.Focus(panel);
        }
        private void SelectRow(int row)
        {
            selected = page * rows.Length + row;
            if (tab == 2) day = selected;
            Refresh();
            hud.UIInput?.Focus(panel);
        }
        private void ChangePage(int delta)
        {
            int pages = Mathf.Max(1, Mathf.CeilToInt(ItemCount() / (float)rows.Length));
            page = (page + delta + pages) % pages;
            selected = page * rows.Length;
            Refresh();
            hud.UIInput?.Focus(panel);
        }
        public void Refresh()
        {
            if (panel == null || host == null) return;
            var calendar = host.CalendarWeather;
            if (calendar == null || !calendar.IsInitialized)
            {
                heading.text = "PLANNING";
                detail.text = "Calendar and weather are initializing.";
                for (int i = 0; i < 4; i++) { rows[i].text = ""; selects[i].SetActive(false); }
                routeButton.SetActive(false);
                return;
            }
            DateTime now = calendar.CurrentDateTime;
            Collect(now, calendar);
            int count = ItemCount();
            int pages = Mathf.Max(1, Mathf.CeilToInt(count / (float)rows.Length));
            page = Mathf.Clamp(page, 0, pages - 1);
            selected = Mathf.Clamp(selected, 0, Mathf.Max(0, count - 1));
            heading.text = "PLANNING  /  " + Tabs[tab] + "     " + now.ToString("ddd, MMM d  h:mm tt");
            pageLabel.text = (page + 1) + " / " + pages;
            for (int i = 0; i < rows.Length; i++)
            {
                int index = page * rows.Length + i;
                bool visible = index < count;
                rows[i].gameObject.SetActive(visible);
                selects[i].SetActive(visible && tab != 0);
                if (visible) rows[i].text = RowText(index, now, calendar);
            }
            detail.text = DetailText(now, calendar) + (string.IsNullOrEmpty(feedback) ? "" : "\n" + feedback);
            routeButton.SetActive(SelectedEvent(out _));
        }
        private void Collect(DateTime now, TruckTaxiCalendarWeatherService calendar)
        {
            events.Clear(); weather.Clear();
            var source = host.Venues?.GetNextSevenDays();
            if (source != null)
                foreach (var item in source)
                    if (item.Start.Date < now.Date.AddDays(7) && item.DepartureEnd >= now) events.Add(item);
            foreach (var item in calendar.Forecast) weather.Add(item);
        }
        private int ItemCount()
        {
            if (tab == 0) return 1;
            if (tab == 2) return 7;
            if (tab == 3) return weather.Count;
            return events.Count;
        }
        private string RowText(int index, DateTime now, TruckTaxiCalendarWeatherService calendar)
        {
            if (tab == 0) return "CURRENT CONDITIONS  " + calendar.CurrentCondition + "  /  " + calendar.CurrentSeverity;
            if (tab == 2)
            {
                DateTime date = now.Date.AddDays(index);
                int count = 0;
                foreach (var item in events) if (item.Start.Date == date) count++;
                return date.ToString("ddd, MMM d") + "     " + count + " EVENTS";
            }
            if (tab == 3)
            {
                var item = weather[index];
                return item.Start.ToString("ddd h:mm tt") + " - " + item.End.ToString("h:mm tt") + "    " + item.Condition;
            }
            var eventItem = events[index];
            string prefix = tab == 4 ? "FARE x" + eventItem.FareMultiplier.ToString("0.00") + "  " : "";
            return prefix + eventItem.Start.ToString("ddd h:mm tt") + "   " + eventItem.EventName + "  /  " + eventItem.VenueName;
        }
        private string DetailText(DateTime now, TruckTaxiCalendarWeatherService calendar)
        {
            if (tab == 0)
            {
                var current = calendar.Current;
                int today = 0;
                foreach (var item in events) if (item.Start.Date == now.Date) today++;
                return "TODAY  " + today + " EVENTS   |   WEATHER " + calendar.CurrentCondition +
                    "   |   NEXT CHANGE " + (calendar.NextChange?.ToString("h:mm tt") ?? "NONE") +
                    (current != null ? "\nHIGH " + current.HighCelsius.ToString("0") + " C   LOW " + current.LowCelsius.ToString("0") +
                    " C   RAIN " + (current.PrecipitationChance01 * 100).ToString("0") + "%" : "") +
                    (string.IsNullOrEmpty(calendar.CurrentWarning) ? "" : "\n" + calendar.CurrentWarning);
            }
            if (tab == 2)
            {
                DateTime date = now.Date.AddDays(day);
                int count = 0;
                foreach (var item in events) if (item.Start.Date == date) count++;
                return date.ToString("dddd, MMMM d, yyyy") + "   |   " + count + " EVENTS";
            }
            if (tab == 3)
            {
                if (weather.Count == 0) return "No forecast entries for this day.";
                var item = weather[Mathf.Clamp(selected, 0, weather.Count - 1)];
                return item.Condition + "  /  " + item.Severity + "   " + item.Start.ToString("ddd h:mm tt") +
                    " - " + item.End.ToString("h:mm tt") + "\nHIGH " + item.HighCelsius.ToString("0") +
                    " C   LOW " + item.LowCelsius.ToString("0") + " C   RAIN " +
                    (item.PrecipitationChance01 * 100).ToString("0") + "%   WIND " +
                    item.WindMetersPerSecond.ToString("0") + " m/s   VIS " + item.VisibilityMeters.ToString("0") + " m" +
                    (string.IsNullOrEmpty(item.Warning) ? "" : "\n" + item.Warning);
            }
            if (!SelectedEvent(out var selectedEvent)) return "No scheduled events in the next seven days.";
            return selectedEvent.EventName + "  /  " + selectedEvent.VenueName + "   " + selectedEvent.Status +
                "\n" + selectedEvent.Start.ToString("ddd, MMM d  h:mm tt") + " - " + selectedEvent.End.ToString("h:mm tt") +
                "   |   ARRIVAL " + selectedEvent.ArrivalStart.ToString("h:mm tt") +
                "   DEPARTURE UNTIL " + selectedEvent.DepartureEnd.ToString("h:mm tt") +
                "\nFARE x" + selectedEvent.FareMultiplier.ToString("0.00") +
                "   RIDES x" + selectedEvent.RideFrequencyMultiplier.ToString("0.00") +
                "   TRAFFIC x" + selectedEvent.TrafficMultiplier.ToString("0.00") +
                "   CROWD x" + selectedEvent.CrowdMultiplier.ToString("0.00");
        }
        private bool SelectedEvent(out TruckTaxiVenueEventSnapshot item)
        {
            item = default;
            if ((tab != 1 && tab != 4) || selected < 0 || selected >= events.Count) return false;
            item = events[selected];
            return true;
        }
        private void RouteToVenue()
        {
            if (!SelectedEvent(out var item) || host?.GPS == null) return;
            bool routed = host.GPS.TrySetMapServiceDestination("venue." + item.VenueId, item.VenueName, item.Position);
            feedback = routed ? "VENUE ROUTE SET" : "ROUTE UNAVAILABLE WHILE A RIDE IS ACTIVE OR GPS IS NOT READY";
            if (routed) Close();
            else Refresh();
        }
    }
}
