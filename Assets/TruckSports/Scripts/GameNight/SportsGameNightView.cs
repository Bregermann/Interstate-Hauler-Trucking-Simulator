using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Michsky.UI.Heat;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace LWS.TruckSports
{
    // Lobby presentation reuses the collection's saved Heat button, fonts,
    // broadcast plates, portrait and DOTween. One EventSystem remains in charge.
    public sealed class SportsGameNightView : MonoBehaviour
    {
        sealed class Card { public TMP_Text title, name, device, ready, help, points; public UnityEngine.UI.Image avatar, accent; }
        SportsGameNightRuntime night;
        GameObject body, editor, profilePage, finalPage, readyRoot, buttonTemplate;
        Card[] cards = new Card[4];
        readonly int[] choices = new int[4];
        readonly float[] nextChoice = new float[4];
        TMP_FontAsset headingFont, bodyFont;
        TMP_Text notice, editorTitle, profileText, finalText, banner, reportStatus;
        TMP_InputField nameField;
        ButtonManager selectSport, profileBack, profileConfirm;
        int editing = -1, profileOwner = -1, deleteChoice, finalPageIndex, profileStatsPage;
        bool creating, deleting;
        float noticeUntil;
        string previousBanner;
        Gamepad replacementPad;
        int[] disconnectedSeats;
        int replacementChoice;
        public bool Visible => body != null && (body.activeSelf || editor.activeSelf || profilePage.activeSelf || finalPage.activeSelf);
        public static SportsGameNightView Create(SportsGameNightRuntime night, SportsFrontEnd front)
        {
            var root = new GameObject("Game night lobby", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            root.transform.SetParent(night.transform, false);
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 500;
            var scaler = root.GetComponent<UnityEngine.UI.CanvasScaler>(); scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.Expand;
            var view = root.AddComponent<SportsGameNightView>(); view.night = night;
            var theme = Resources.Load<SportsBroadcastTheme>("SportsBroadcastTheme"); view.headingFont = theme != null ? theme.headingFont : TMP_Settings.defaultFontAsset; view.bodyFont = theme != null ? theme.bodyFont : TMP_Settings.defaultFontAsset;
            var template = front.pages[0].GetComponentsInChildren<ButtonManager>(true).First();
            view.buttonTemplate = Instantiate(template.gameObject, root.transform); view.buttonTemplate.SetActive(false);
            var marker = view.buttonTemplate.GetComponent<SportsMenuAction>(); if (marker != null) Destroy(marker);
            view.Build(front); SportsControllerKeyboard.Closed += view.KeyboardClosed; return view;
        }
        void OnDestroy() { SportsControllerKeyboard.Closed -= KeyboardClosed; }
        RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 at)
        {
            var root = new GameObject(name, typeof(RectTransform)); root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f); rect.sizeDelta = size; rect.anchoredPosition = at; return rect;
        }
        TMP_Text Text(string name, Transform parent, Vector2 size, Vector2 at, int fontSize, bool heading = false)
        {
            var text = Rect(name, parent, size, at).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = heading ? headingFont : bodyFont; text.fontSize = fontSize; text.color = SportsBroadcastCanvas.Cream;
            text.alignment = TextAlignmentOptions.Center; text.richText = false; text.raycastTarget = false;
            text.overflowMode = TextOverflowModes.Ellipsis; return text;
        }
        GameObject Page(string name, string title)
        {
            var page = Rect(name, transform, new Vector2(1840, 1000), Vector2.zero).gameObject;
            var image = page.AddComponent<UnityEngine.UI.Image>(); image.color = SportsBroadcastCanvas.Ink; page.AddComponent<SportsBroadcastPlate>().cut = 24;
            Text("Heading", page.transform, new Vector2(1680, 72), new Vector2(0, 425), 48, true).text = title;
            page.SetActive(false); return page;
        }
        ButtonManager Button(string caption, Transform parent, Vector2 at, Vector2 size, Action action)
        {
            var go = Instantiate(buttonTemplate, parent); go.name = caption;
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f); rect.anchoredPosition = at; rect.sizeDelta = size;
            var button = go.GetComponent<ButtonManager>(); button.buttonText = caption; button.onClick = new UnityEngine.Events.UnityEvent(); button.onClick.AddListener(() => action()); button.UpdateUI();
            foreach (var text in go.GetComponentsInChildren<TMP_Text>(true)) { text.fontSize = 24; text.fontSizeMax = 24; text.fontSizeMin = 20; text.richText = false; }
            go.SetActive(true); return button;
        }
        void Build(SportsFrontEnd front)
        {
            body = Page("Party lobby", "TRUCK SPORTS: PARTY!  /  GAME NIGHT");
            Text("Subtitle", body.transform, new Vector2(1600, 48), new Vector2(0, 355), 25).text = "Your controller. Your driver. Ready when you are.";
            for (int i = 0; i < 4; i++)
            {
                int slot = i; var card = Rect("Player " + (i + 1), body.transform, new Vector2(800, 250), new Vector2(i % 2 == 0 ? -420 : 420, i < 2 ? 175 : -105));
                card.gameObject.AddComponent<UnityEngine.UI.Image>().color = SportsBroadcastCanvas.Raised; card.gameObject.AddComponent<SportsBroadcastPlate>().cut = 18;
                var avatar = Rect("Driver icon", card, new Vector2(88, 88), new Vector2(-320, 10)).gameObject.AddComponent<UnityEngine.UI.Image>(); avatar.sprite = Resources.Load<Sprite>("Broadcast truck portrait"); avatar.preserveAspect = true;
                var accent = Rect("Player color", card, new Vector2(5, 220), new Vector2(-385, 0)).gameObject.AddComponent<UnityEngine.UI.Image>();
                cards[i] = new Card { avatar = avatar, accent = accent,
                    title = Text("Player", card, new Vector2(600, 34), new Vector2(-30, 89), 24, true),
                    name = Text("Driver", card, new Vector2(580, 46), new Vector2(55, 38), 32, true),
                    device = Text("Controller", card, new Vector2(580, 32), new Vector2(55, -4), 21),
                    ready = Text("Ready state", card, new Vector2(580, 34), new Vector2(55, -43), 26, true),
                    help = Text("Controls", card, new Vector2(740, 54), new Vector2(0, -94), 20),
                    points = Text("Winner points", card, new Vector2(190, 34), new Vector2(285, 90), 23, true) };
                var readyButton = Button("CHOOSE / READY", card, new Vector2(55, -43), new Vector2(460, 40), () =>
                {
                    var seat = night.State.Seats[slot];
                    if (seat.DeviceKey == "keyboard" || !seat.Occupied && slot == 0) Activate(slot);
                    else Notice("Each player readies with their own assigned controller.");
                });
                var readyGroup = readyButton.GetComponent<CanvasGroup>();
                if (readyGroup == null) readyGroup = readyButton.gameObject.AddComponent<CanvasGroup>();
                readyGroup.alpha = 0;
            }
            notice = Text("Party notice", body.transform, new Vector2(1650, 80), new Vector2(0, -300), 24);
            selectSport = Button("START / MENU  ·  CHOOSE SPORT", body.transform, new Vector2(0, -390), new Vector2(620, 64), night.SelectSport);
            Button("KEYBOARD JOIN", body.transform, new Vector2(-650, -390), new Vector2(340, 64), () => night.JoinKeyboard());
            Button("END GAME NIGHT", body.transform, new Vector2(650, -390), new Vector2(340, 64), OpenEndConfirmation);
            Text("Host controls", body.transform, new Vector2(1600, 40), new Vector2(0, -455), 22).text = "Player 1 chooses the next sport.  A / Cross: select or ready  ·  X: new / rename  ·  Y: profile  ·  B: leave";
            editor = Page("Name editor", "YOUR DRIVER");
            editorTitle = Text("Editor instruction", editor.transform, new Vector2(1400, 72), new Vector2(0, 240), 32, true);
            var templateField = front.GetComponent<SportsParticipantsMenu>().names[0];
            nameField = Instantiate(templateField, editor.transform); nameField.name = "Game night driver name";
            var fieldRect = nameField.GetComponent<RectTransform>(); fieldRect.anchorMin = fieldRect.anchorMax = fieldRect.pivot = new Vector2(.5f, .5f); fieldRect.anchoredPosition = new Vector2(0, 80); fieldRect.sizeDelta = new Vector2(1050, 72);
            nameField.characterLimit = 40; nameField.onEndEdit = new TMP_InputField.SubmitEvent(); nameField.onValueChanged = new TMP_InputField.OnChangeEvent();
            SportsControllerTextField.Bind(nameField);
            Button("SAVE DRIVER", editor.transform, new Vector2(-270, -120), new Vector2(460, 64), () => CommitName(nameField.text));
            Button("CANCEL", editor.transform, new Vector2(270, -120), new Vector2(460, 64), CancelEditor);
            Text("Name help", editor.transform, new Vector2(1500, 80), new Vector2(0, -270), 24).text = "Choose a name up to 40 characters. The on-screen keyboard opens for your controller.";
            profilePage = Page("Driver profile", "YOUR DRIVER");
            profileText = Text("Profile information", profilePage.transform, new Vector2(1500, 500), new Vector2(0, 60), 28);
            profileBack = Button("BACK", profilePage.transform, new Vector2(-300, -350), new Vector2(420, 64), CloseProfile);
            profileConfirm = Button("DELETE PROFILE", profilePage.transform, new Vector2(300, -350), new Vector2(420, 64), ProfileConfirm);
            finalPage = Page("Night standings", "GAME NIGHT COMPLETE"); finalText = Text("Final standings", finalPage.transform, new Vector2(1550, 620), new Vector2(0, 20), 34, true);
            reportStatus = Text("Report status", finalPage.transform, new Vector2(1650, 65), new Vector2(0, -305), 20);
            Button("BACK TO TITLE", finalPage.transform, new Vector2(-300, -390), new Vector2(520, 64), CloseFinal);
            Button("X  ·  SAVE SESSION REPORT", finalPage.transform, new Vector2(300, -390), new Vector2(520, 64), ExportReport);
            readyRoot = Rect("Game-night ready check", transform, new Vector2(1540, 68), Vector2.zero).gameObject;
            var readyRect = readyRoot.GetComponent<RectTransform>(); readyRect.anchorMin = readyRect.anchorMax = new Vector2(.5f, 0); readyRect.pivot = new Vector2(.5f, 0); readyRect.anchoredPosition = new Vector2(0, 10);
            readyRoot.AddComponent<UnityEngine.UI.Image>().color = SportsBroadcastCanvas.Ink; readyRoot.AddComponent<SportsBroadcastPlate>().cut = 12;
            banner = Text("Ready participants", readyRoot.transform, new Vector2(1500, 60), Vector2.zero, 24); readyRoot.SetActive(false);
        }
        void ShowOnly(GameObject page)
        {
            foreach (var item in new[] { body, editor, profilePage, finalPage }) item.SetActive(item == page);
            readyRoot.SetActive(false);
            if (page != null)
            {
                var group = page.GetComponent<CanvasGroup>(); if (group == null) group = page.AddComponent<CanvasGroup>(); group.DOKill(); group.alpha = 0; group.DOFade(1, .18f).SetUpdate(true).SetLink(page);
            }
        }
        public void Show() { ShowOnly(body); Refresh(); }
        public void Hide() { ShowOnly(null); }
        public void Notice(string text) { notice.text = text; noticeUntil = Time.unscaledTime + 6; }
        public void ProfileSelection(int slot) { choices[slot] = 0; Refresh(); }
        List<SportsParticipant> Profiles(int slot) => SportsParticipantRegistry.Current.participants.Where(p => !p.preferredAI && !Enumerable.Range(0, 4).Any(i => i != slot && night.State.Seats[i].Occupied && night.State.Seats[i].ProfileId == p.id)).ToList();
        public void Refresh()
        {
            if (night.State == null) return;
            for (int i = 0; i < 4; i++)
            {
                var saved = Profiles(i);
                var seat = night.State.Seats[i]; var card = cards[i]; var profile = night.Profile(i);
                card.title.text = "PLAYER " + (i + 1) + (i == 0 && seat.Occupied ? "  ·  HOST" : "");
                card.accent.color = profile != null ? profile.color : new Color(.22f, .27f, .34f);
                card.avatar.color = profile != null ? Color.Lerp(profile.color, Color.white, .35f) : Color.gray;
                card.points.text = profile != null && night.State.Points.TryGetValue(profile.id, out int points) ? points + " POINTS" : "";
                if (!seat.Occupied)
                {
                    card.name.text = "PRESS A / CROSS / START TO JOIN"; card.device.text = ""; card.ready.text = "OPEN SEAT"; card.help.text = "Bring a controller. Pick your driver."; continue;
                }
                if (!seat.ProfileConfirmed)
                {
                    choices[i] = saved.Count == 0 ? 0 : Mathf.Clamp(choices[i], 0, saved.Count - 1);
                    card.name.text = saved.Count == 0 ? "CREATE YOUR DRIVER" : saved[choices[i]].displayName;
                    card.ready.text = "CHOOSE A PROFILE"; card.ready.color = new Color(1, .77f, .24f);
                    card.help.text = "← / →: choose  ·  A: select  ·  X: new driver  ·  Y: guest";
                }
                else
                {
                    card.name.text = profile != null ? profile.displayName : "Driver unavailable";
                    card.ready.text = !seat.Connected ? "CONTROLLER DISCONNECTED" : seat.Ready ? "READY!" : "NOT READY";
                    card.ready.color = seat.Ready ? new Color(.45f, 1, .65f) : new Color(1, .77f, .24f);
                    card.help.text = "A: ready / unready  ·  X: rename  ·  Y: profile  ·  ↑ / ↓: color / icon";
                    card.avatar.sprite = Avatar(profile != null ? profile.avatarKey : "truck");
                }
                card.device.text = seat.DeviceKey == "keyboard" ? "KEYBOARD / MOUSE" : night.Pad(i) != null ? night.Pad(i).displayName : "Reconnect your controller";
            }
            selectSport.buttonText = night.State.AllReady ? "START / MENU  ·  CHOOSE SPORT" : "WAITING FOR PLAYERS TO READY"; selectSport.UpdateUI();
            if (Time.unscaledTime >= noticeUntil) notice.text = night.State.AllReady ? "Ready to roll. Player 1 chooses the sport." : "Each player chooses a driver, then presses A / Cross to ready up.";
        }
        Sprite Avatar(string key)
        {
            var theme = Resources.Load<SportsBroadcastTheme>("SportsBroadcastTheme");
            if (theme != null) { if (key == "controller") return theme.controllerGlyph; if (key == "left-stick") return theme.leftStickGlyph; if (key == "right-stick") return theme.rightStickGlyph; }
            return Resources.Load<Sprite>("Broadcast truck portrait");
        }
        void Activate(int slot)
        {
            var seat = night.State.Seats[slot]; if (!seat.Occupied) { if (slot == 0) night.JoinKeyboard(); return; }
            if (!seat.ProfileConfirmed) { var profiles = Profiles(slot); if (profiles.Count > 0) night.SelectProfile(slot, profiles[Mathf.Clamp(choices[slot], 0, profiles.Count - 1)].id); else BeginName(slot, true); }
            else night.State.SetReady(slot, !seat.Ready);
            Refresh();
        }
        static bool Left(Gamepad pad) => pad.dpad.left.wasPressedThisFrame || pad.leftStick.left.wasPressedThisFrame;
        static bool Right(Gamepad pad) => pad.dpad.right.wasPressedThisFrame || pad.leftStick.right.wasPressedThisFrame;
        public void Tick()
        {
            if (editor.activeSelf) { if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame) CommitName(nameField.text); if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) CancelEditor(); return; }
            if (profilePage.activeSelf) { TickProfile(); return; }
            if (finalPage.activeSelf) { TickEnded(); return; }
            foreach (var pad in Gamepad.all)
            {
                if (!SportsGameNightRuntime.CanJoin(pad)) continue;
                int slot = night.State.SeatForDevice(SportsGameNightRuntime.DeviceKey(pad));
                if (slot < 0)
                {
                    if (pad.startButton.wasPressedThisFrame && night.State.Seats.Any(s => s.Occupied && !s.Connected))
                    {
                        OfferReplacement(pad); return;
                    }
                    if (pad.buttonSouth.wasPressedThisFrame || pad.startButton.wasPressedThisFrame) night.Join(pad);
                    continue;
                }
                var seat = night.State.Seats[slot];
                if (slot > 0 && !night.State.Seats[0].Connected && pad.startButton.wasPressedThisFrame)
                { night.State.TakeHost(slot); night.RefreshUiAuthority(); Refresh(); return; }
                if (pad.buttonEast.wasPressedThisFrame) { night.State.Leave(slot); Refresh(); continue; }
                if (pad.buttonSouth.wasPressedThisFrame) { Activate(slot); if (editor.activeSelf || SportsControllerKeyboard.InputBlocked) return; }
                if (pad.buttonWest.wasPressedThisFrame) { BeginName(slot, !seat.ProfileConfirmed); return; }
                if (pad.buttonNorth.wasPressedThisFrame) { if (!seat.ProfileConfirmed) night.SelectGuest(slot); else { OpenProfile(slot); return; } }
                Vector2 direction = pad.dpad.ReadValue(); if (direction.sqrMagnitude < .25f) direction = pad.leftStick.ReadValue();
                if (!seat.ProfileConfirmed && Mathf.Abs(direction.x) > .5f && Time.unscaledTime >= nextChoice[slot])
                {
                    int count = Profiles(slot).Count; if (count > 0) choices[slot] = (choices[slot] + (direction.x > 0 ? 1 : count - 1)) % count;
                    nextChoice[slot] = Time.unscaledTime + .22f; Refresh();
                }
                else if (seat.ProfileConfirmed && Mathf.Abs(direction.y) > .5f && Time.unscaledTime >= nextChoice[slot])
                {
                    var profile = night.Profile(slot); if (profile != null)
                    {
                        string avatar = profile.avatarKey; Color color = profile.color;
                        if (direction.y > 0) { Color.RGBToHSV(color, out float hue, out _, out _); color = Color.HSVToRGB(Mathf.Repeat(hue + .137f, 1), .7f, 1); }
                        else { string[] icons = { "truck", "controller", "left-stick", "right-stick" }; avatar = icons[(Array.IndexOf(icons, avatar) + 1) % icons.Length]; }
                        if (SportsGameNightRuntime.IsGuest(profile.id)) { profile.color = color; profile.avatarKey = avatar; }
                        else SportsParticipantRegistry.UpdateProfile(profile.id, profile.displayName, color, avatar);
                        night.ProfileChanged(slot);
                    }
                    nextChoice[slot] = Time.unscaledTime + .3f;
                }
                if (slot == 0 && pad.startButton.wasPressedThisFrame) night.SelectSport();
                if (slot == 0 && pad.selectButton.wasPressedThisFrame) OpenEndConfirmation();
            }
            if (Keyboard.current != null)
            {
                int slot = night.State.SeatForDevice("keyboard");
                if (slot < 0 && Keyboard.current.enterKey.wasPressedThisFrame) night.JoinKeyboard();
                else if (slot >= 0)
                {
                    if (Keyboard.current.enterKey.wasPressedThisFrame) Activate(slot);
                    if (Keyboard.current.f2Key.wasPressedThisFrame) BeginName(slot, !night.State.Seats[slot].ProfileConfirmed);
                    if (Keyboard.current.f5Key.wasPressedThisFrame && slot == 0) night.SelectSport();
                }
            }
        }
        public void OfferReplacement(Gamepad pad)
        {
            disconnectedSeats = Enumerable.Range(0, 4).Where(i => night.State.Seats[i].Occupied && !night.State.Seats[i].Connected).ToArray();
            if (disconnectedSeats.Length == 0) return;
            if (disconnectedSeats.Length == 1) { night.ReplaceController(disconnectedSeats[0], pad); return; }
            replacementPad = pad; replacementChoice = 0; ShowOnly(profilePage); RenderReplacement(); night.RefreshUiAuthority();
        }
        void BeginName(int slot, bool isNew)
        {
            editing = slot; creating = isNew; editorTitle.text = isNew ? "CREATE YOUR DRIVER" : "RENAME YOUR DRIVER";
            nameField.SetTextWithoutNotify(isNew ? "" : night.Profile(slot)?.displayName ?? ""); ShowOnly(editor);
            var pad = night.Pad(slot);
            if (pad != null) SportsControllerKeyboard.Open(nameField, pad, nameField.text, CommitName, editorTitle.text);
            else { EventSystem.current.SetSelectedGameObject(nameField.gameObject); nameField.ActivateInputField(); }
        }
        void CommitName(string text)
        {
            if (editing < 0 || string.IsNullOrWhiteSpace(text)) return;
            int slot = editing; text = text.Trim(); if (text.Length > 40 || text.Any(char.IsControl)) return;
            var profile = creating ? SportsParticipantRegistry.Create(text) : night.Profile(slot);
            if (profile == null) return;
            if (SportsGameNightRuntime.IsGuest(profile.id)) profile.displayName = text;
            else if (!SportsParticipantRegistry.UpdateProfile(profile.id, text, profile.color, profile.avatarKey)) return;
            editing = -1; night.SelectProfile(slot, profile.id); Show();
        }
        void CancelEditor() { editing = -1; SportsControllerKeyboard.CancelFor(nameField); Show(); }
        void KeyboardClosed(TMP_InputField field, bool confirmed) { if (field == nameField && !confirmed && editing >= 0) { editing = -1; Show(); } }
        void OpenProfile(int slot)
        {
            profileOwner = slot; deleting = false; var profile = night.Profile(slot); if (profile == null) return;
            ProfileButtons("BACK", SportsGameNightRuntime.IsGuest(profile.id) ? "LEAVE AS GUEST" : "DELETE PROFILE");
            profileStatsPage = 0; RenderProfileStatistics();
            ShowOnly(profilePage);
        }
        static string ProfileDate(string value) => DateTime.TryParse(value, out var date) ? date.ToString("yyyy-MM-dd") : "Not recorded";
        static int ProfilePages(SportsProfileStatistics stats) => 1 + (stats?.sports?.Sum(s => Math.Max(1, (s.records.Count + 3) / 4)) ?? 0);
        void RenderProfileStatistics()
        {
            var profile = night.Profile(profileOwner); if (profile == null) return;
            if (SportsGameNightRuntime.IsGuest(profile.id))
            { profileText.text = profile.displayName + "\n\nGUEST · THIS GAME NIGHT ONLY\n\nSession winner points remain in the lobby.\nChoose or create a saved driver for lifetime statistics.\n\nB: back · X: leave guest"; return; }
            var stats = profile.statistics ?? new SportsProfileStatistics(); stats.Normalize();
            profileStatsPage = Mathf.Clamp(profileStatsPage, 0, ProfilePages(stats) - 1);
            if (profileStatsPage == 0)
            {
                profileText.text = profile.displayName + "\nLIFETIME DRIVER STATISTICS\n\n" +
                    stats.matches + " MATCHES   ·   " + stats.sessionIds.Count + " GAME NIGHTS\n" +
                    stats.wins + " WINS   ·   " + stats.losses + " LOSSES   ·   " + stats.draws + " DRAWS\n" +
                    stats.WinPercentage.ToString("0.#") + "% COMPETITIVE WIN RATE   ·   " + stats.winnerPoints + " LIFETIME WINNER POINTS\n" +
                    stats.practice + " PRACTICE   ·   " + stats.cooperative + " COOPERATIVE MATCHES\n" +
                    TimeSpan.FromSeconds(stats.playedSeconds).TotalMinutes.ToString("0.#") + " MINUTES PLAYED\n\n" +
                    "CREATED " + ProfileDate(profile.createdUtc) + "   ·   LAST PLAYED " + ProfileDate(profile.lastPlayedUtc) +
                    "\n\n← / →: sport records · B: back · X: delete profile";
            }
            else
            {
                int recordPage = profileStatsPage - 1; SportsSportStatistic sport = null;
                foreach (var candidate in stats.sports.OrderBy(s => s.title, StringComparer.Ordinal))
                {
                    int count = Math.Max(1, (candidate.records.Count + 3) / 4);
                    if (recordPage < count) { sport = candidate; break; } recordPage -= count;
                }
                string records = sport.records.Count == 0 ? "NO COMPARABLE PERSONAL RECORD YET" : string.Join("\n", sport.records.Skip(recordPage * 4).Take(4).Select(r => (string.IsNullOrEmpty(r.variantLabel) ? "STANDARD" : r.variantLabel) + " · " + r.value.ToString("0.###") + " " + r.unit + (r.preference == SportsRecordPreference.Lower ? " · LOWEST" : " · HIGHEST")));
                string places = sport.placements.Count == 0 ? "NO COMPETITIVE PLACEMENTS" : string.Join("   ·   ", sport.placements.OrderBy(p => p.place).Take(6).Select(p => "#" + p.place + " × " + p.count));
                profileText.text = profile.displayName + "\n" + (string.IsNullOrEmpty(sport.title) ? "SPORT RESULTS" : sport.title) + "   ·   RECORD PAGE " + (recordPage + 1) + "\n\n" +
                    sport.matches + " MATCHES   ·   " + sport.wins + " WINS   ·   " + sport.losses + " LOSSES   ·   " + sport.draws + " DRAWS\n" +
                    places + "\n\nPERSONAL RECORDS\n" + records + "\n\n← / →: pages · B: back · X: delete profile";
            }
        }
        void CloseProfile() { replacementPad = null; disconnectedSeats = null; profileOwner = -1; deleting = false; if (night.InSport) Hide(); else Show(); night.RefreshUiAuthority(); }
        void BeginDelete() { deleting = true; deleteChoice = 0; RenderDeleteChoice(); }
        void ProfileButtons(string back, string confirm)
        {
            profileBack.SetText(back); profileConfirm.SetText(confirm);
        }
        void ProfileConfirm()
        {
            if (replacementPad != null) { night.ReplaceController(disconnectedSeats[replacementChoice], replacementPad); CloseProfile(); return; }
            if (!deleting) { BeginDelete(); return; }
            if (profileOwner < 0) { night.EndNight(); return; }
            var profile = night.Profile(profileOwner);
            if (profile != null && !SportsGameNightRuntime.IsGuest(profile.id) && !SportsParticipantRegistry.DeleteProfile(profile.id))
            { profileText.text = SportsParticipantRegistry.Notice; return; }
            night.State.Leave(profileOwner); CloseProfile();
        }
        void RenderDeleteChoice()
        {
            bool ending = profileOwner < 0;
            ProfileButtons(ending ? "CONTINUE PLAYING" : "KEEP DRIVER", ending ? "END GAME NIGHT" : "DELETE DRIVER");
            profileText.text = (ending ? "END GAME NIGHT?" : "DELETE THIS SAVED DRIVER?\nPast match history remains preserved.") + "\n\n" + (deleteChoice == 0 ? "▶ " : "  ") + (ending ? "CONTINUE PLAYING" : "KEEP DRIVER") + "\n\n" + (deleteChoice == 1 ? "▶ " : "  ") + (ending ? "END GAME NIGHT" : "DELETE DRIVER") + "\n\n← / → choose · A confirms · B cancels";
        }
        void TickProfile()
        {
            if (replacementPad != null)
            {
                if (!replacementPad.added || !replacementPad.enabled || replacementPad.buttonEast.wasPressedThisFrame) { CloseProfile(); return; }
                if (Left(replacementPad)) replacementChoice = (replacementChoice + disconnectedSeats.Length - 1) % disconnectedSeats.Length;
                if (Right(replacementPad)) replacementChoice = (replacementChoice + 1) % disconnectedSeats.Length;
                RenderReplacement();
                if (replacementPad.buttonSouth.wasPressedThisFrame)
                {
                    bool replaced = night.ReplaceController(disconnectedSeats[replacementChoice], replacementPad);
                    CloseProfile(); if (!replaced) Notice("That driver has already reconnected. Try again.");
                }
                return;
            }
            var pad = profileOwner >= 0 ? night.Pad(profileOwner) : night.Pad(0);
            if (pad == null) return;
            if (pad.buttonEast.wasPressedThisFrame) { CloseProfile(); return; }
            if (!deleting)
            {
                if (pad.buttonWest.wasPressedThisFrame) BeginDelete();
                else if (Left(pad) || Right(pad))
                {
                    int pages = ProfilePages(night.Profile(profileOwner)?.statistics);
                    profileStatsPage = (profileStatsPage + (Right(pad) ? 1 : pages - 1)) % pages;
                    RenderProfileStatistics();
                }
                return;
            }
            if (Left(pad) || Right(pad)) { deleteChoice = 1 - deleteChoice; RenderDeleteChoice(); }
            if (!pad.buttonSouth.wasPressedThisFrame) return;
            if (deleteChoice == 1) ProfileConfirm(); else CloseProfile();
        }
        void RenderReplacement()
        {
            ProfileButtons("CANCEL", "RECONNECT DRIVER");
            profileText.text = "RECONNECT YOUR DRIVER\n\n" + string.Join("\n\n", disconnectedSeats.Select((s, i) => (i == replacementChoice ? "▶ " : "  ") + "PLAYER " + (s + 1) + " · " + (night.Profile(s)?.displayName ?? "Driver"))) + "\n\n← / → choose · A reconnects · B cancels\n\nYour profile and winner points stay with your driver.";
        }
        void OpenEndConfirmation()
        {
            profileOwner = -1; deleting = true; deleteChoice = 0; ShowOnly(profilePage);
            RenderDeleteChoice();
        }
        public void ShowFinalStandings()
        {
            ShowOnly(finalPage); var scores = night.State.Points.OrderByDescending(p => p.Value).ThenBy(p => night.DriverName(p.Key)).ToArray();
            finalPageIndex = Mathf.Clamp(finalPageIndex, 0, Math.Max(0, (scores.Length - 1) / 6));
            finalText.text = (scores.Length == 0 || scores[0].Value == 0 ? "NO WINNER POINTS EARNED" : "THE GAME-NIGHT CHAMPIONS") + "\n\n" + string.Join("\n\n", scores.Skip(finalPageIndex * 6).Take(6).Select(p => (1 + scores.Count(other => other.Value > p.Value)) + ".  " + night.DriverName(p.Key) + "   " + p.Value + " POINTS"));
            reportStatus.text = night.Report != null && night.Report.SaveError == null ? "SESSION REPORT READY  ·  X TO SAVE / EXPORT  ·  ← / → STANDINGS" : "REPORT SAVE INCOMPLETE · X TO RETRY";
        }
        void ExportReport() { reportStatus.text = night.ExportReport() ? "REPORT SAVED\n" + night.Report.JsonPath : "REPORT COULD NOT BE SAVED · CHECK AVAILABLE DISK SPACE AND RETRY"; }
        public void TickEnded()
        {
            var pad = night.Pad(0);
            if (pad != null && pad.buttonWest.wasPressedThisFrame) ExportReport();
            if (pad != null && (pad.buttonSouth.wasPressedThisFrame || pad.buttonEast.wasPressedThisFrame) || Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame) CloseFinal();
            if (pad != null && Right(pad)) { finalPageIndex++; ShowFinalStandings(); }
            if (pad != null && Left(pad)) { finalPageIndex = Math.Max(0, finalPageIndex - 1); ShowFinalStandings(); }
        }
        void CloseFinal() { Hide(); night.RefreshUiAuthority(); var front = FindAnyObjectByType<SportsFrontEnd>(); front?.Show(0); }
        public void ReadyBar(SportsEventSession session, SportsGameNightState state)
            => ReadyBar(!session.Live && !session.Finished, state);
        public void ReadyBar(bool setupVisible, SportsGameNightState state, bool spectateAI = false, bool supportsAiSpectating = true)
        {
            bool show = !Visible && state.Phase == SportsGameNightPhase.Configuration && setupVisible; readyRoot.SetActive(show); if (!show) return;
            string text = string.Join("   ·   ", Enumerable.Range(0, 4).Where(i => state.Seats[i].Occupied).Select(i => "P" + (i + 1) + " " + (state.IsRequired(i) ? state.Seats[i].Ready ? "READY" : "PRESS START / MENU" : "SPECTATING")));
            if (supportsAiSpectating) text += "   ·   Y / F6: " + (spectateAI ? "PLAY WITH CREW" : "WATCH AI");
            if (text != previousBanner) { previousBanner = text; banner.text = text; }
        }
    }
}
