using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LWS.TruckTaxi
{
    // Presentation only. Session owns identity/statistics; the existing facade owns persistence.
    public sealed class TruckTaxiCareerPanel : MonoBehaviour
    {
        private TruckTaxiBootstrap host;
        private TruckTaxiHud hud;
        private RectTransform panel;
        private TMP_InputField nameField;
        private TextMeshProUGUI heading, statistics, feedback;
        private bool wasPaused, firstDay;
        private RectTransform equipment;
        private readonly GameObject[] upgradeButtons = new GameObject[3];
        private readonly TextMeshProUGUI[] upgradeLabels = new TextMeshProUGUI[3];
        private readonly string[] upgradeIds = new string[3];
        private TextMeshProUGUI categoryHeading, pageLabel, historyNote;
        private int page, pageCount=1;
        private string category="DRIVER";
        private bool shopping;
        public const int RowsPerPage=6;
        private static readonly string[] Categories={ "DRIVER","FINANCES","RIDES","RATING","TRUCK",
            "UPGRADES","COSMETICS","RACES","GOALS","EVENTS","COLLECTIONS","LAST SESSION" };
        public bool IsOpen => panel != null && panel.gameObject.activeSelf;
        public Transform FocusRoot => panel;
        public event Action VisibilityChanged;

        public void Initialize(TruckTaxiBootstrap owner, TruckTaxiHud view)
        {
            if (panel != null) return;
            host = owner; hud = view;
            panel = hud.Panel(hud.Root, "Truck Taxi Career", new Vector2(.1f, .06f), new Vector2(.9f, .94f));
            heading = hud.Text(panel, "Career heading", new Vector2(.05f, .87f), new Vector2(.95f, .98f), 34);
            hud.Text(panel, "Driver name label", new Vector2(.05f, .76f), new Vector2(.29f, .85f), 26).text = "DRIVER NAME";
            var input = hud.Panel(panel, "Driver name", new Vector2(.31f, .76f), new Vector2(.72f, .85f));
            nameField = input.gameObject.AddComponent<TMP_InputField>();
            var text = hud.Text(input, "Value", new Vector2(.035f, .08f), new Vector2(.965f, .92f), 26);
            text.richText = false;
            nameField.textViewport = input; nameField.textComponent = text;
            nameField.characterLimit = 40; nameField.lineType = TMP_InputField.LineType.SingleLine;
            nameField.targetGraphic = input.GetComponent<Image>();
            hud.Button(panel, "APPLY", new Vector2(.75f, .76f), new Vector2(.95f, .85f), ApplyName);
            for(int i=0;i<Categories.Length;i++)
            {
                string selected=Categories[i]; float top=.72f-i*.052f;
                hud.Button(panel,selected,new Vector2(.025f,top-.045f),new Vector2(.245f,top),()=>SelectCategory(selected));
            }
            categoryHeading=hud.Text(panel,"Category",new Vector2(.28f,.66f),new Vector2(.95f,.73f),28);
            statistics = hud.Text(panel, "Lifetime statistics", new Vector2(.28f, .32f), new Vector2(.95f, .65f), 25);
            statistics.richText = false;
            statistics.textWrappingMode=TextWrappingModes.NoWrap;
            historyNote=hud.Text(panel,"Historical detail",new Vector2(.28f,.26f),new Vector2(.95f,.31f),20);
            hud.Button(panel,"<",new Vector2(.28f,.19f),new Vector2(.39f,.25f),()=>ChangePage(-1));
            pageLabel=hud.Text(panel,"Page",new Vector2(.41f,.19f),new Vector2(.81f,.25f),22);
            pageLabel.alignment=TextAlignmentOptions.Center;
            hud.Button(panel,">",new Vector2(.84f,.19f),new Vector2(.95f,.25f),()=>ChangePage(1));
            equipment=TruckTaxiHud.Rect(panel,"Equipment",new Vector2(.28f,.32f),new Vector2(.95f,.65f));
            for(int i=0;i<3;i++)
            {
                int index=i; float top=1-i*.33f;
                upgradeLabels[i]=hud.Text(equipment,"Upgrade "+i,new Vector2(0,top-.3f),new Vector2(.67f,top),20);
                upgradeLabels[i].richText=false;
                upgradeButtons[i]=hud.Button(equipment,"PURCHASE / EQUIP",new Vector2(.66f,top-.26f),new Vector2(1,top-.04f),()=>
                { host.CareerEquipment.PurchaseOrEquip(upgradeIds[index]); feedback.text=host.CareerEquipment.Feedback; RefreshEquipment(); hud.UIInput?.Focus(panel); });
            }
            hud.Button(panel,"EQUIPMENT",new Vector2(.28f,.025f),new Vector2(.49f,.1f),()=>ShowEquipment(true));
            hud.Button(panel,"APPEARANCE",new Vector2(.505f,.025f),new Vector2(.73f,.1f),()=>hud.DriverAppearance?.Open());
            feedback = hud.Text(panel, "Career feedback", new Vector2(.28f, .105f), new Vector2(.95f, .18f), 22);
            feedback.richText = false;
            hud.Button(panel, "CONTINUE", new Vector2(.745f, .025f), new Vector2(.975f, .1f), Close);
            panel.gameObject.SetActive(false);
        }

        public void OpenFirstDay()
        {
            var profile = host?.Session?.CaptureCareerProfile();
            if (profile?.firstGuideEligible == true && !profile.firstGuideClaimed) Open();
        }
        public void Open()
        {
            if (IsOpen || panel == null || host.Persistence?.HasActiveRun != true || host.Persistence.ResumeBlocked) return;
            var profile = host.Session.CaptureCareerProfile();
            if (profile == null) return;
            firstDay = profile.firstGuideEligible && !profile.firstGuideClaimed;
            wasPaused = host.Paused; host.SetPaused(true);
            heading.text = firstDay ? TruckTaxiSession.FirstDayGuidePreview.Subtitle : "TRUCK TAXI CAREER";
            nameField.SetTextWithoutNotify(profile.driverName);
            feedback.text = string.Empty;
            SelectCategory("DRIVER");
            panel.gameObject.SetActive(true); panel.SetAsLastSibling(); hud.UIInput?.Focus(panel);
            VisibilityChanged?.Invoke();
        }
        private void RefreshStatistics()
        {
            var state = host.Session.CapturePersistentState();
            var rows=BuildCategoryRows(category,state,host.CareerEquipment?.Catalog);
            pageCount=Math.Max(1,(rows.Count+RowsPerPage-1)/RowsPerPage);
            page=Mathf.Clamp(page,0,pageCount-1);
            statistics.text=string.Join("\n",PageRows(rows,page));
            categoryHeading.text=category;
            historyNote.text=state.career.historicalDetailsIncomplete ? "Earlier detailed history is incomplete." : "";
            UpdatePageLabel();
        }
        private void ApplyName()
        {
            if (!host.Session.TrySetDriverName(nameField.text, out string reason)) { feedback.text = reason; return; }
            nameField.SetTextWithoutNotify(host.Session.DriverName);
            host.Persistence.RequestAutosave("Career driver name");
            feedback.text = "DRIVER NAME UPDATED";
            if(!shopping) RefreshStatistics();
        }
        private void SelectCategory(string value)
        { category=value; ShowEquipment(false); }
        private void ChangePage(int direction)
        {
            page=Mathf.Clamp(page+direction,0,pageCount-1);
            if(shopping) RefreshEquipment(); else RefreshStatistics();
        }
        private void UpdatePageLabel() => pageLabel.text=(page+1)+" / "+pageCount;
        public static List<string> PageRows(List<string> rows,int requestedPage)
        {
            if(rows==null || rows.Count==0) return new List<string>();
            int boundedPage=Mathf.Clamp(requestedPage,0,(rows.Count-1)/RowsPerPage);
            int start=boundedPage*RowsPerPage;
            return rows.GetRange(start,Math.Min(RowsPerPage,rows.Count-start));
        }
        private void ShowEquipment(bool show)
        {
            shopping=show; page=0;
            statistics.gameObject.SetActive(!show); equipment.gameObject.SetActive(show);
            if(show) RefreshEquipment(); else RefreshStatistics();
        }
        public void OpenEquipment() { Open(); if(IsOpen) ShowEquipment(true); }
        private void RefreshEquipment()
        {
            var owner=host.CareerEquipment;
            var catalog=owner?.Catalog;
            var items=new List<TruckTaxiCareerUpgradeDefinition>();
            if(catalog!=null && catalog.Validate(out _)) foreach(var item in catalog.upgrades)
            {
                if(!item.availableForPurchase || !TruckTaxiCareerEquipment.Supports(item) ||
                    !TruckTaxiCareerCatalog.Compatible(item.compatibleTruckIds,host.Player.DefinitionId)) continue;
                items.Add(item);
            }
            pageCount=Math.Max(1,(items.Count+upgradeButtons.Length-1)/upgradeButtons.Length);
            page=Mathf.Clamp(page,0,pageCount-1);
            categoryHeading.text="EQUIPMENT";
            historyNote.text=items.Count==0 ? "No compatible upgrades available." : "CASH  "+TruckTaxiHud.Money(host.Session.WalletBalanceCents);
            UpdatePageLabel();
            int row=0;
            for(int i=page*upgradeButtons.Length;i<items.Count && row<upgradeButtons.Length;i++)
            {
                var item=items[i];
                upgradeIds[row]=item.stableId;
                var tuning=item.adapterConfiguration as TruckTaxiCareerUpgradeTuning;
                string effect=tuning!=null ? " +"+((tuning.multiplier-1)*100).ToString("0")+"% "+item.category.ToString().ToUpperInvariant() : "";
                bool equipped=owner.IsEquipped(item.stableId),owned=owner.IsOwned(item.stableId);
                upgradeLabels[row].text=item.displayName+effect+"\n"+(equipped ? "LEVEL 1 / MAX - EQUIPPED" :
                    owned ? "LEVEL 1 OWNED - EQUIP" : "LEVEL 0 > 1  "+TruckTaxiHud.Money(item.priceCents)+
                    (host.Session.WalletBalanceCents<item.priceCents ? " - NEED CASH" : ""));
                upgradeLabels[row].gameObject.SetActive(true); upgradeButtons[row].SetActive(!owner.IsEquipped(item.stableId));
                row++;
            }
            for(;row<upgradeButtons.Length;row++)
            { upgradeLabels[row].gameObject.SetActive(false); upgradeButtons[row].SetActive(false); }
        }
        // Pure presentation projection: no recording, migration, catalog mutation or wallet operations.
        public static List<string> BuildCategoryRows(string selected,TruckTaxiPersistentState state,TruckTaxiCareerCatalog catalog)
        {
            var rows=new List<string>();
            if(state?.statistics==null || state.career==null) { rows.Add("No recorded statistics."); return rows; }
            var s=state.statistics; var c=state.career; var profile=state.ownedProfile;
            switch(selected)
            {
                case "DRIVER":
                    rows.Add("Driver: "+Label(profile?.driverName));
                    rows.Add("Playtime: "+(c.playtimeSeconds/3600).ToString("0.0")+" hr");
                    break;
                case "FINANCES":
                    long wallet=state.shiftEarningsCents-state.purchaseChargesCents-state.serviceChargesCents;
                    rows.Add("Current cash: "+TruckTaxiHud.Money(wallet));
                    rows.Add("Lifetime earnings: "+TruckTaxiHud.Money(state.shiftEarningsCents));
                    rows.Add("Lifetime spending: $"+(((decimal)state.purchaseChargesCents+state.serviceChargesCents)/100m).ToString("0.00"));
                    rows.Add("Purchases: "+TruckTaxiHud.Money(state.purchaseChargesCents));
                    rows.Add("Service charges: "+TruckTaxiHud.Money(state.serviceChargesCents));
                    rows.Add("Debt: "+TruckTaxiHud.Money(wallet<0 ? -wallet : 0));
                    break;
                case "RIDES":
                    long accepted=0;
                    if(state.passengerSelection?.passengers!=null)
                        foreach(var exposure in state.passengerSelection.passengers) accepted+=exposure.timesAccepted;
                    rows.Add("Offered: "+(state.passengerSelection?.offerIndex ?? 0)); rows.Add("Accepted: "+accepted);
                    rows.Add("Declined: "+s.offersDeclined); rows.Add("Expired: "+s.offersExpired);
                    rows.Add("Completed: "+s.completedRides); rows.Add("Cancelled: "+c.cancelledRides);
                    rows.Add("Pickup cancellations: "+s.pickupCancellations); rows.Add("Failed: "+s.failedRides);
                    rows.Add("Local: "+c.localRides); rows.Add("Intercity: "+c.intercityRides);
                    rows.Add("Stolen fares completed: "+c.stolenFaresCompleted); rows.Add("Passenger ejections: "+s.ejectedRides);
                    break;
                case "RATING":
                    rows.Add("Average stars: "+(s.completedRides==0 ? "Not yet rated" : ((double)s.totalStarsEarned/s.completedRides).ToString("0.00")));
                    rows.Add("1-star: "+c.oneStar); rows.Add("2-star: "+c.twoStars); rows.Add("3-star: "+c.threeStars);
                    rows.Add("4-star: "+c.fourStars); rows.Add("5-star: "+c.fiveStars);
                    break;
                case "TRUCK":
                    if(profile?.trucks!=null) foreach(var truck in profile.trucks)
                        rows.Add(TruckLabel(truck.truckId,catalog)+(truck.truckId==profile.equippedTruckId ? " [EQUIPPED]" : " [OWNED]"));
                    break;
                case "UPGRADES":
                case "COSMETICS":
                    bool upgrades=selected=="UPGRADES";
                    if(profile?.trucks!=null) foreach(var truck in profile.trucks)
                    {
                        var ids=upgrades ? truck.ownedUpgradeIds : truck.ownedCosmeticIds;
                        if(ids==null || ids.Count==0) continue;
                        rows.Add(TruckLabel(truck.truckId,catalog));
                        foreach(var id in ids)
                        {
                            var selections=upgrades ? truck.upgrades : truck.cosmetics;
                            bool equipped=selections!=null && selections.Exists(item=>item.itemId==id);
                            string name=upgrades ? catalog?.FindUpgrade(id)?.displayName : catalog?.FindCosmetic(id)?.displayName;
                            rows.Add(Label(string.IsNullOrWhiteSpace(name) ? id : name)+(equipped ? " [EQUIPPED]" : " [OWNED]"));
                        }
                    }
                    if(!upgrades && profile?.ownedHeadwearIds!=null) foreach(var id in profile.ownedHeadwearIds)
                    {
                        string name=catalog?.FindCosmetic(id)?.displayName;
                        rows.Add("Headwear: "+Label(string.IsNullOrWhiteSpace(name) ? id : name)+
                            (id==profile.equippedHeadwearId ? " [EQUIPPED]" : " [OWNED]"));
                    }
                    break;
                case "RACES":
                    rows.Add("Entered: "+s.racesEntered); rows.Add("Completed: "+c.racesCompleted);
                    rows.Add("Won: "+s.raceWins); rows.Add("Podiums: "+s.racePodiums);
                    rows.Add("Prize earnings: "+TruckTaxiHud.Money(s.racePrizeEarningsCents));
                    rows.Add("Race chaos: "+s.raceChaosEarned);
                    break;
                case "GOALS":
                    long offered=0,attempted=0,completed=0,failed=0;
                    foreach(var goal in c.goals)
                    { offered+=goal.offered; attempted+=goal.attempted; completed+=goal.completed; failed+=goal.failed; }
                    rows.Add("Offered: "+offered); rows.Add("Attempted: "+attempted);
                    rows.Add("Completed: "+completed); rows.Add("Failed: "+failed);
                    foreach(var goal in c.goals)
                    {
                        rows.Add(Label(goal.id));
                        rows.Add("Offered "+goal.offered+" / Attempted "+goal.attempted);
                        rows.Add("Completed "+goal.completed+" / Failed "+goal.failed);
                    }
                    break;
                case "EVENTS":
                    rows.Add("Chaos score: "+((decimal)c.chaosScore+s.raceChaosEarned).ToString("0"));
                    rows.Add("Chaos events: "+c.chaosEvents); rows.Add("Traffic collisions: "+s.trafficHits);
                    rows.Add("Pedestrians hit: "+s.pedestriansHit); rows.Add("Bottles thrown: "+s.containersThrown);
                    rows.Add("Passenger ejections: "+s.ejectedRides);
                    foreach(var entry in c.events) rows.Add(Label(entry.id)+": "+entry.count);
                    break;
                case "COLLECTIONS":
                    rows.Add("Toilets discovered: "+c.discoveredToiletIds.Count);
                    foreach(var id in c.discoveredToiletIds) rows.Add(Label(id));
                    break;
                case "LAST SESSION":
                    var last=state.lastSession;
                    if(last==null) { rows.Add("No recorded last session."); break; }
                    rows.Add("Rides completed: "+last.rides); rows.Add("Earnings: "+TruckTaxiHud.Money(last.earningsCents));
                    rows.Add("Average stars: "+(last.rides==0 ? "Not yet rated" : ((double)last.totalStars/last.rides).ToString("0.00")));
                    rows.Add("Playtime: "+(last.playtimeSeconds/3600).ToString("0.0")+" hr");
                    rows.Add("Chaos: "+last.chaos); rows.Add("Goals completed: "+last.objectivesCompleted);
                    rows.Add("Traffic collisions: "+last.trafficCollisions); rows.Add("Pedestrians hit: "+last.pedestriansHit);
                    rows.Add("Passenger ejections: "+last.passengersEjected); rows.Add("Bottles thrown: "+last.bottlesThrown);
                    rows.Add("Jug events: "+last.jugEvents); rows.Add("Special events: "+last.specialEvents);
                    break;
            }
            if(rows.Count==0) rows.Add("None recorded.");
            return rows;
        }
        private static string TruckLabel(string id,TruckTaxiCareerCatalog catalog)
        {
            var entry=catalog?.FindTruck(id);
            return Label(string.IsNullOrWhiteSpace(entry?.DisplayName) ? id : entry.DisplayName);
        }
        private static string Label(string value)
        {
            if(string.IsNullOrWhiteSpace(value)) return "Not recorded";
            value=value.Replace('\r',' ').Replace('\n',' ').Replace('\t',' ');
            return value.Length>48 ? value.Substring(0,45)+"..." : value;
        }

        public void Close()
        {
            if (!IsOpen) return;
            if (firstDay)
            {
                if (!host.Session.TrySetDriverName(nameField.text, out string reason)) { feedback.text = reason; return; }
                if (host.Session.TryClaimFirstDayGuide(out _)) host.Persistence.RequestAutosave("Career first-day introduction");
            }
            nameField.DeactivateInputField(); panel.gameObject.SetActive(false);
            host.SetPaused(wasPaused); hud.UIInput?.Focus(null);
            VisibilityChanged?.Invoke();
        }
        public bool HandleInput(TruckTaxiUIInput input)
        {
            if (!IsOpen) return false;
            input.Focus(panel);
            if (nameField.isFocused)
            {
                // Backspace and WASD belong to text editing, not global menu shortcuts.
                if (Keyboard.current?.escapeKey.wasPressedThisFrame == true) nameField.DeactivateInputField();
                return true;
            }
            if (input.Cancel.WasPressedThisFrame() || input.Pause.WasPressedThisFrame()) Close();
            else if (input.Submit.WasPressedThisFrame()) input.SubmitSelected();
            return true;
        }
    }
}
