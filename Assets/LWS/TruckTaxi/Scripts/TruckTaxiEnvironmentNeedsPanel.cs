using System;
using System.Collections.Generic;
using LWS.InterstateHauler;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace LWS.TruckTaxi
{
    // A subpanel on the existing Heat HUD canvas; never owns vehicle input or pause semantics.
    [DisallowMultipleComponent]
    public sealed class TruckTaxiEnvironmentNeedsPanel : MonoBehaviour
    {
        private TruckTaxiBootstrap host;
        private TruckTaxiHud hud;
        private TruckTaxiEnvironmentCoordinator environment;
        private TruckTaxiDriverNeedsCoordinator needs;
        private RectTransform panel, driverPage, environmentPage, storePage, inventoryPage, jugPanel, jugFill, bladderFill;
        private TextMeshProUGUI clockText, meterText, needText, jugText, debugText, environmentText, inventoryText, storeText, storeFeedback;
        private CanvasGroup bandGroup;
        private readonly List<Action> refreshControls = new List<Action>();
        private readonly List<UnityEngine.UI.Selectable> startJugControls = new List<UnityEngine.UI.Selectable>();
        private readonly List<UnityEngine.UI.Selectable> storeControls = new List<UnityEngine.UI.Selectable>();
        private bool wasPaused, syncing;
        private float nextRefresh;
        public bool IsOpen => panel != null && panel.gameObject.activeSelf;
        public Transform FocusRoot => environmentPage != null && environmentPage.gameObject.activeSelf ? environmentPage :
            storePage != null && storePage.gameObject.activeSelf ? storePage :
            inventoryPage != null && inventoryPage.gameObject.activeSelf ? inventoryPage : driverPage;

        public void Initialize(TruckTaxiBootstrap owner, TruckTaxiHud view, TruckTaxiEnvironmentCoordinator weather, TruckTaxiDriverNeedsCoordinator driver)
        {
            if (panel != null || view.Root == null || driver.State == null) return;
            host = owner; hud = view; environment = weather; needs = driver;
            var band = hud.Panel(hud.Root, "Taxi time and driver needs", new Vector2(.36f, .932f), new Vector2(.72f, .994f));
            bandGroup = band.gameObject.AddComponent<CanvasGroup>();
            clockText = hud.Text(band, "Game time", new Vector2(.025f, .12f), new Vector2(.29f, .95f), 20);
            meterText = hud.Text(band, "Driver needs", new Vector2(.30f, .23f), new Vector2(.75f, .98f), 17);
            var bladderTrack = TruckTaxiHud.Rect(band, "Bladder track", new Vector2(.30f, .10f), new Vector2(.74f, .18f));
            bladderTrack.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(.25f, .28f, .29f);
            bladderFill = TruckTaxiHud.Rect(bladderTrack, "Bladder fill", Vector2.zero, new Vector2(.01f, 1));
            bladderFill.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(.33f, .8f, .62f);
            hud.Button(band, "NEEDS", new Vector2(.77f, .12f), new Vector2(.985f, .88f), Open);
            panel = hud.Panel(hud.Root, "Driver needs and environment", new Vector2(.14f, .19f), new Vector2(.72f, .91f));
            driverPage = TruckTaxiHud.Rect(panel, "Driver needs page", Vector2.zero, Vector2.one);
            environmentPage = TruckTaxiHud.Rect(panel, "Development environment page", Vector2.zero, Vector2.one);
            storePage = TruckTaxiHud.Rect(panel, "Truck stop store page", Vector2.zero, Vector2.one);
            inventoryPage = TruckTaxiHud.Rect(panel, "Driver inventory page", Vector2.zero, Vector2.one);
            BuildDriverPage(); BuildEnvironmentPage(); BuildStorePage(); BuildInventoryPage();
            environmentPage.gameObject.SetActive(false); panel.gameObject.SetActive(false);
            storePage.gameObject.SetActive(false); inventoryPage.gameObject.SetActive(false);
            jugPanel = hud.Panel(hud.Root, "Jug timing", new Vector2(.35f, .75f), new Vector2(.71f, .915f));
            jugText = hud.Text(jugPanel, "Jug prompt", new Vector2(.03f, .43f), new Vector2(.97f, .95f), 25);
            var track = TruckTaxiHud.Rect(jugPanel, "Jug progress track", new Vector2(.04f, .14f), new Vector2(.96f, .36f));
            track.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(.22f, .25f, .26f);
            jugFill = TruckTaxiHud.Rect(track, "Jug progress", Vector2.zero, new Vector2(.001f, 1));
            jugFill.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(.25f, .7f, .6f);
            for (int i = 1; i <= 3; i++)
            {
                var cue = TruckTaxiHud.Rect(track, "Cue " + i, new Vector2(i * .25f - .006f, -.12f), new Vector2(i * .25f + .006f, 1.12f));
                cue.gameObject.AddComponent<UnityEngine.UI.Image>().color = Color.white;
            }
            jugPanel.gameObject.SetActive(false);
            Canvas.ForceUpdateCanvases(); UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
            Refresh();
        }
        private void BuildDriverPage()
        {
            hud.Text(driverPage, "Driver heading", new Vector2(.04f, .91f), new Vector2(.96f, .985f), 34).text = "DRIVER NEEDS";
            needText = hud.Text(driverPage, "Driver status", new Vector2(.05f, .68f), new Vector2(.95f, .90f), 24);
            hud.Button(driverPage, "ROUTE TO RESTROOM", new Vector2(.05f, .55f), new Vector2(.485f, .65f), () => { needs.RouteToBathroom(); Refresh(); });
            hud.Button(driverPage, "USE BATHROOM", new Vector2(.515f, .55f), new Vector2(.95f, .65f), () => { needs.UseBathroom(); Refresh(); });
            var jug = hud.Button(driverPage, "USE PISS JUG", new Vector2(.05f, .42f), new Vector2(.485f, .52f), BeginJug);
            startJugControls.AddRange(jug.GetComponentsInChildren<UnityEngine.UI.Selectable>(true));
            hud.Button(driverPage, "DISPOSE OF FILLED JUG", new Vector2(.515f, .42f), new Vector2(.95f, .52f), () => { needs.DisposeJug(); Refresh(); });
            hud.Button(driverPage, "CLEAN CAB", new Vector2(.05f, .29f), new Vector2(.485f, .39f), () => { needs.CleanCab(); Refresh(); });
            hud.Button(driverPage, "RESUME RIDE ROUTE", new Vector2(.515f, .29f), new Vector2(.95f, .39f), () => { needs.RestoreRideRoute(); Refresh(); });
            debugText = hud.Text(driverPage, "Driver detail", new Vector2(.05f, .16f), new Vector2(.95f, .275f), 22);
            hud.Button(driverPage, "STORE", new Vector2(.05f, .035f), new Vector2(.27f, .13f), ShowStore);
            hud.Button(driverPage, "ITEMS", new Vector2(.29f, .035f), new Vector2(.51f, .13f), ShowInventory);
            if (Debug.isDebugBuild) hud.Button(driverPage, "DEBUG", new Vector2(.53f, .035f), new Vector2(.74f, .13f), ShowEnvironment);
            hud.Button(driverPage, "CLOSE", new Vector2(.76f, .035f), new Vector2(.95f, .13f), Close);
        }
        private void BuildStorePage()
        {
            storeText = hud.Text(storePage, "Store heading", new Vector2(.04f, .94f), new Vector2(.96f, .99f), 32);
            storeFeedback = hud.Text(storePage, "Store status", new Vector2(.05f, .86f), new Vector2(.95f, .935f), 21);
            var entries = TruckTaxiNeedsItems.Store;
            for (int i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                int row = i / 2, column = i % 2;
                float top = .85f - row * .1f;
                var button = hud.Button(storePage, entry.Name + "  " + TruckTaxiHud.Money(entry.PriceCents),
                    new Vector2(.05f + column * .46f, top - .088f), new Vector2(.49f + column * .46f, top),
                    () => { needs.BuyItem(entry.Item); Refresh(); });
                storeControls.AddRange(button.GetComponentsInChildren<UnityEngine.UI.Selectable>(true));
            }
            hud.Button(storePage, "ROUTE", new Vector2(.05f, .035f), new Vector2(.34f, .13f),
                () => { needs.RouteToStore(); Refresh(); });
            hud.Button(storePage, "BACK", new Vector2(.36f, .035f), new Vector2(.65f, .13f), ShowDriver);
            hud.Button(storePage, "CLOSE", new Vector2(.67f, .035f), new Vector2(.95f, .13f), Close);
        }
        private void BuildInventoryPage()
        {
            hud.Text(inventoryPage, "Items heading", new Vector2(.04f, .94f), new Vector2(.96f, .99f), 32).text = "CAB INVENTORY";
            inventoryText = hud.Text(inventoryPage, "Item counts", new Vector2(.05f, .86f), new Vector2(.95f, .935f), 18);
            TruckTaxiNeedsItem[] usable = { TruckTaxiNeedsItem.WaterBottle, TruckTaxiNeedsItem.Soda, TruckTaxiNeedsItem.EnergyDrink,
                TruckTaxiNeedsItem.Snack, TruckTaxiNeedsItem.Burger, TruckTaxiNeedsItem.Sandwich, TruckTaxiNeedsItem.Meal,
                TruckTaxiNeedsItem.HighOctaneSuppository, TruckTaxiNeedsItem.MysteryMushrooms };
            for (int i = 0; i < usable.Length; i++)
            {
                var item = usable[i]; int row = i / 2, column = i % 2;
                float top = .85f - row * .1f;
                hud.Button(inventoryPage, "USE " + TruckTaxiNeedsItems.Find(item).Name.ToUpperInvariant(),
                    new Vector2(.05f + column * .46f, top - .088f), new Vector2(.49f + column * .46f, top),
                    () => { needs.ConsumeItem(item); Refresh(); });
            }
            hud.Button(inventoryPage, "FRESHENER / MIRROR", new Vector2(.05f, .27f), new Vector2(.34f, .36f),
                () => { needs.MountDecoration(TruckTaxiNeedsItem.AirFreshener, TruckTaxiCabSlot.Mirror); Refresh(); });
            hud.Button(inventoryPage, "DANCER / LEFT", new Vector2(.36f, .27f), new Vector2(.65f, .36f),
                () => { needs.MountDecoration(TruckTaxiNeedsItem.HulaFigure, TruckTaxiCabSlot.DashboardLeft); Refresh(); });
            hud.Button(inventoryPage, "DANCER / RIGHT", new Vector2(.67f, .27f), new Vector2(.95f, .36f),
                () => { needs.MountDecoration(TruckTaxiNeedsItem.HulaFigure, TruckTaxiCabSlot.DashboardRight); Refresh(); });
            hud.Button(inventoryPage, "DUCK / LEFT", new Vector2(.05f, .17f), new Vector2(.34f, .26f),
                () => { needs.MountDecoration(TruckTaxiNeedsItem.NoveltyDuck, TruckTaxiCabSlot.DashboardLeft); Refresh(); });
            hud.Button(inventoryPage, "DUCK / RIGHT", new Vector2(.36f, .17f), new Vector2(.65f, .26f),
                () => { needs.MountDecoration(TruckTaxiNeedsItem.NoveltyDuck, TruckTaxiCabSlot.DashboardRight); Refresh(); });
            hud.Button(inventoryPage, "THROW FILLED", new Vector2(.67f, .17f), new Vector2(.95f, .26f),
                () => { if (needs.State.FilledJug) { Close(); host.SetPaused(false); needs.ThrowFilledContainer(); } });
            hud.Button(inventoryPage, "BACK", new Vector2(.05f, .035f), new Vector2(.48f, .13f), ShowDriver);
            hud.Button(inventoryPage, "CLOSE", new Vector2(.52f, .035f), new Vector2(.95f, .13f), Close);
        }
        private void BuildEnvironmentPage()
        {
            hud.Text(environmentPage, "Environment heading", new Vector2(.04f, .94f), new Vector2(.96f, .995f), 30).text = "ENVIRONMENT / DRIVER DEBUG";
            environmentText = hud.Text(environmentPage, "Environment state", new Vector2(.04f, .865f), new Vector2(.96f, .935f), 21);
            string[] periods = { "DAWN", "MORNING", "NOON", "AFTERNOON", "SUNSET", "NIGHT" };
            float[] hours = { 6, 9, 12, 15, 19, 22 };
            for (int i = 0; i < periods.Length; i++) { int index = i; GridButton(periods[i], .79f, i, 6, () => environment.SetTime(hours[index])); }
            GridButton("+1 HOUR", .715f, 0, 3, environment.AdvanceHour);
            GridButton("FREEZE / RUN", .715f, 1, 3, () => environment.SetFrozen(!environment.Frozen));
            GridButton("AUTO WEATHER", .715f, 2, 3, () => environment.SetAutomaticWeather(!environment.AutomaticWeather));
            AddHeatSlider("Game time", .655f, 0, 23.99f, () => environment.Clock.CurrentSnapshot.timeOfDayHours, environment.SetTime);
            AddHeatSlider("Time scale", .595f, 0, 180, () => environment.Clock.CurrentSnapshot.timeScale, environment.SetTimeScale);
            string[] weatherNames = { "CLEAR", "CLOUDS", "OVERCAST", "RAIN", "HEAVY RAIN", "STORM", "FOG", "NEXT" };
            string[] weatherIds = { "clear", "partly_cloudy", "overcast", "light_rain", "heavy_rain", "thunderstorm", "fog" };
            for (int i = 0; i < weatherNames.Length; i++)
            {
                int index = i;
                GridButton(weatherNames[i], .50f - (i / 4) * .075f, i % 4, 4, () => { if (index == 7) environment.NextWeather(); else environment.ForceWeather(weatherIds[index]); });
            }
            string[] levels = { "BLADDER 0%", "50%", "90%", "CRISIS" };
            float[] values = { 0, .5f, .9f, 1 };
            for (int i = 0; i < 4; i++) { int index = i; GridButton(levels[i], .325f, i, 4, () => needs.DebugSetBladder(values[index])); }
            GridButton("START JUG", .25f, 0, 3, () => { needs.DebugSetBladder(.9f); BeginJug(); });
            GridButton("JUG SUCCESS", .25f, 1, 3, () => needs.DebugFinishJug(true));
            GridButton("JUG SPILL", .25f, 2, 3, () => needs.DebugFinishJug(false));
            GridButton("CLEAR CAB MESS", .175f, 0, 2, needs.DebugClearCabMess);
            GridButton("DRIVER NEEDS", .175f, 1, 2, ShowDriver);
            hud.Button(environmentPage, "CLOSE", new Vector2(.25f, .04f), new Vector2(.75f, .13f), Close);
        }
        private void GridButton(string label, float y, int column, int count, Action action)
        {
            float width = .92f / count;
            hud.Button(environmentPage, label, new Vector2(.04f + column * width, y), new Vector2(.04f + (column + 1) * width - .012f, y + .062f), () =>
            { if (!Debug.isDebugBuild) return; action(); Refresh(); });
        }
        private void AddHeatSlider(string label, float y, float minimum, float maximum, Func<float> get, Action<float> set)
        {
            if (hud.heatSliderPrefab == null) return;
            var valueText = hud.Text(environmentPage, label + " value", new Vector2(.05f, y), new Vector2(.37f, y + .05f), 22);
            var go = Instantiate(hud.heatSliderPrefab, environmentPage, false); go.name = label;
            var rect = (RectTransform)go.transform; rect.anchorMin = new Vector2(.39f, y); rect.anchorMax = new Vector2(.94f, y + .05f); rect.offsetMin = rect.offsetMax = Vector2.zero;
            foreach (var c in go.GetComponents<Component>()) if (c.GetType().FullName == "Michsky.UI.Heat.SliderManager")
            { var type = c.GetType(); type.GetField("saveValue").SetValue(c, false); type.GetField("invokeOnAwake").SetValue(c, false); type.GetField("useSounds").SetValue(c, false); }
            foreach (var text in go.GetComponentsInChildren<TMP_Text>(true)) text.enabled = false;
            foreach (var field in go.GetComponentsInChildren<TMP_InputField>(true)) field.gameObject.SetActive(false);
            var slider = go.GetComponent<UnityEngine.UI.Slider>(); slider.minValue = minimum; slider.maxValue = maximum; slider.SetValueWithoutNotify(get());
            slider.onValueChanged.AddListener(value => { if (!syncing && Debug.isDebugBuild) { set(value); Refresh(); } });
            refreshControls.Add(() => { slider.SetValueWithoutNotify(get()); valueText.text = label + "  " + get().ToString("0.0"); });
        }
        public void Open()
        {
            if (panel == null || IsOpen) return;
            wasPaused = host.Paused; host.SetPaused(true); panel.gameObject.SetActive(true); panel.SetAsLastSibling(); ShowDriver(); Refresh();
        }
        public void Close() { if (!IsOpen) return; panel.gameObject.SetActive(false); host.SetPaused(wasPaused); }
        public void Toggle() { if (IsOpen) Close(); else Open(); }
        private void ShowDriver()
        { driverPage.gameObject.SetActive(true); environmentPage.gameObject.SetActive(false); storePage.gameObject.SetActive(false); inventoryPage.gameObject.SetActive(false); hud.UIInput.Focus(driverPage); }
        private void ShowStore()
        { driverPage.gameObject.SetActive(false); environmentPage.gameObject.SetActive(false); inventoryPage.gameObject.SetActive(false); storePage.gameObject.SetActive(true); Refresh(); hud.UIInput.Focus(storePage); }
        private void ShowInventory()
        { driverPage.gameObject.SetActive(false); environmentPage.gameObject.SetActive(false); storePage.gameObject.SetActive(false); inventoryPage.gameObject.SetActive(true); Refresh(); hud.UIInput.Focus(inventoryPage); }
        private void ShowEnvironment()
        {
            if (!Debug.isDebugBuild) return;
            driverPage.gameObject.SetActive(false); storePage.gameObject.SetActive(false); inventoryPage.gameObject.SetActive(false);
            environmentPage.gameObject.SetActive(true); hud.UIInput.Focus(environmentPage); Refresh();
        }
        private void BeginJug()
        {
            if (!ValidDrivingPhase() || !needs.State.CanStartJug) return;
            Close(); host.SetPaused(false); needs.StartJug();
        }
        private bool ValidDrivingPhase() => host.Session.State == TruckTaxiState.Available || host.Session.State == TruckTaxiState.DrivingToPickup || host.Session.State == TruckTaxiState.DrivingToDestination;
        private void Update()
        {
            if (panel == null) return;
            bandGroup.interactable = bandGroup.blocksRaycasts = !host.Paused;
            jugPanel.gameObject.SetActive(needs.JugActive && !host.Paused);
            if (needs.JugActive)
            {
                jugFill.anchorMax = new Vector2(Mathf.Max(.001f, needs.State.JugProgress), 1);
                jugText.text = "HOLD " + hud.UIInput.Hint(needs.JugHoldAction) + "  /  TAP " + hud.UIInput.Hint(needs.JugCueAction) +
                    "\n" + (needs.State.CueIsOpen(needs.IsMoving) ? "NOW" : "STEADY") + "    " + needs.State.JugCuesCompleted + "/3";
            }
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + .15f; Refresh();
        }
        private void Refresh()
        {
            if (panel == null) return;
            var state = needs.State; var clock = environment.Clock.CurrentSnapshot;
            clockText.text = clock.ClockText + "\n" + environment.Period.ToString().ToUpperInvariant();
            meterText.color = state.StrangeUiSeconds > 0 ? new Color(.95f, .4f, .85f) :
                state.ArcadeRushSeconds > 0 ? new Color(1f, .86f, .25f) : new Color(.96f, .96f, .91f);
            meterText.text = "BLADDER " + (state.Pressure * 100).ToString("0") + "%\nHUNGER " +
                (state.Hunger * 100).ToString("0") + "%   THIRST " + (state.Thirst * 100).ToString("0") + "%";
            bladderFill.anchorMax = new Vector2(Mathf.Max(.005f, state.Pressure), 1);
            bladderFill.GetComponent<UnityEngine.UI.Image>().color = state.Pressure >= .85f ? new Color(1, .35f, .25f) : new Color(.3f, .8f, .6f);
            if (!IsOpen) return;
            storeText.text = "TRUCK STOP STORE  |  " + TruckTaxiHud.Money(host.Session.WalletBalanceCents);
            storeFeedback.text = string.IsNullOrEmpty(needs.Feedback) ?
                (needs.NearbyStore != null ? needs.NearbyStore.displayName : "Stop inside a store bay to buy.") : needs.Feedback;
            needText.text = "BLADDER " + (state.Pressure * 100).ToString("0") + "%  HUNGER " + (state.Hunger * 100).ToString("0") +
                "%  THIRST " + (state.Thirst * 100).ToString("0") + "%\n" +
                "EMPTY BOTTLES " + state.Count(TruckTaxiNeedsItem.EmptyBottle) + "  JUGS " + state.Count(TruckTaxiNeedsItem.EmptyPissJug) +
                "  FILLED " + (state.Count(TruckTaxiNeedsItem.FilledBottle) + state.Count(TruckTaxiNeedsItem.FilledJug)) + "\n" +
                (state.CabMess ? "CAB NEEDS CLEANUP  |  " : "") +
                (needs.NearbyStore != null ? "STORE: " + needs.NearbyStore.displayName : needs.NearbyBathroom != null ?
                    "RESTROOM: " + needs.NearbyBathroom.displayName : "No service bay nearby.");
            debugText.text = needs.Feedback;
            inventoryText.text = "WATER " + state.Count(TruckTaxiNeedsItem.WaterBottle) + "  SODA " + state.Count(TruckTaxiNeedsItem.Soda) +
                "  ENERGY " + state.Count(TruckTaxiNeedsItem.EnergyDrink) + "  FOOD " +
                (state.Count(TruckTaxiNeedsItem.Snack) + state.Count(TruckTaxiNeedsItem.Burger) +
                 state.Count(TruckTaxiNeedsItem.Sandwich) + state.Count(TruckTaxiNeedsItem.Meal)) +
                "\nFILLED BOTTLES " + state.Count(TruckTaxiNeedsItem.FilledBottle) + "  FILLED JUGS " + state.Count(TruckTaxiNeedsItem.FilledJug) +
                "  EMPTY BOTTLES " + state.Count(TruckTaxiNeedsItem.EmptyBottle) + "  JUGS " + state.Count(TruckTaxiNeedsItem.EmptyPissJug) +
                "\n" + needs.Feedback;
            environmentText.text = clock.ClockText + "  |  " + environment.Period + "  |  " + environment.Weather.CurrentSnapshot.weatherPresetId +
                "\nTIME " + (environment.Frozen ? "FROZEN" : "RUNNING") + "  |  AUTO WEATHER " + (environment.AutomaticWeather ? "ON" : "OFF");
            foreach (var control in startJugControls) control.interactable = ValidDrivingPhase() && state.CanStartJug;
            foreach (var control in storeControls) control.interactable = needs.NearbyStore != null;
            syncing = true; foreach (var refresh in refreshControls) refresh(); syncing = false;
        }
    }
}
