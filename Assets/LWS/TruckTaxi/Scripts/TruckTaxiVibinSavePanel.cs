using System;
using LWS.InterstateHauler;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LWS.TruckTaxi
{
    // Presentation only: the scoped Vibin facade retains all save/load authority.
    public sealed class TruckTaxiVibinSavePanel : MonoBehaviour
    {
        private TruckTaxiBootstrap host;
        private TruckTaxiHud hud;
        private TruckTaxiVibinPersistence persistence;
        private Func<TruckTaxiVibinPersistence> resolve;
        private Action keepPaused;
        private RectTransform panel, content, confirmRoot;
        private TextMeshProUGUI profileText, statusText, promptText;
        private readonly TextMeshProUGUI[] slotText = new TextMeshProUGUI[4];
        private readonly ViewButton[] saves = new ViewButton[3], loads = new ViewButton[4];
        private ViewButton previous, next, latest, recovery, back, cancel, confirm;
        private string viewedProfile, feedback;
        private Confirmation pending;
        private bool executing, failed;
        private float refreshAt;
        public event Action VisibilityChanged;
        public bool IsOpen => panel != null && panel.gameObject.activeSelf;
        public bool IsBusy => executing || persistence != null && (persistence.IsBusy || persistence.SaveService?.IsSaving == true ||
            persistence.SaveService?.IsLoading == true || persistence.SaveService?.LoadCoordinator?.IsLoadActive == true);
        public Transform FocusRoot => !IsOpen || IsBusy ? null : pending != null ? confirmRoot : content;

        public void Initialize(TruckTaxiBootstrap value, TruckTaxiHud view)
        {
            host = value;
            InitializeView(view, () => host != null ? host.GetComponent<TruckTaxiVibinPersistence>() : null,
                () => { if (host != null) host.SetPaused(true); });
        }

        // Also usable by an existing menu/view root without constructing a second HUD or Canvas.
        public void InitializeView(TruckTaxiHud view, Func<TruckTaxiVibinPersistence> source, Action pause = null)
        {
            if (panel != null) return;
            hud = view; resolve = source; keepPaused = pause;
            panel = hud.Panel(hud.Root, "Vibin save overlay", Vector2.zero, Vector2.one);
            panel.gameObject.SetActive(false);
            var frame = TruckTaxiHud.Rect(panel, "Save load frame", new Vector2(.16f, .1f), new Vector2(.84f, .9f));
            hud.Text(frame, "Save load heading", new Vector2(.03f, .925f), new Vector2(.97f, 1), 34).text = "VIBIN' / SAVE & LOAD";
            content = TruckTaxiHud.Rect(frame, "Save slots", Vector2.zero, new Vector2(1, .92f));
            previous = Button(content, "<", new Vector2(.03f, .86f), new Vector2(.1f, .97f), () => Browse(-1));
            profileText = hud.Text(content, "Save profile", new Vector2(.13f, .85f), new Vector2(.87f, .98f), 24);
            profileText.richText = false;
            next = Button(content, ">", new Vector2(.9f, .86f), new Vector2(.97f, .97f), () => Browse(1));
            for (int i = 0; i < slotText.Length; i++)
            {
                int row = i; float y = .71f - i * .14f;
                slotText[i] = hud.Text(content, "Save slot " + i, new Vector2(.03f, y), new Vector2(.57f, y + .12f), 22);
                slotText[i].richText = false; slotText[i].fontSizeMin = 16;
                if (i < 3) saves[i] = Button(content, "SAVE", new Vector2(.59f, y + .015f), new Vector2(.76f, y + .11f), () => RequestSave(row + 1));
                loads[i] = Button(content, "LOAD", new Vector2(.79f, y + .015f), new Vector2(.97f, y + .11f), () => RequestLoad(Slot(row)));
            }
            latest = Button(content, "LOAD LATEST", new Vector2(.03f, .01f), new Vector2(.32f, .105f), () => RequestLoad(persistence?.Latest));
            recovery = Button(content, "REVIEW BACKUP", new Vector2(.35f, .01f), new Vector2(.68f, .105f), RequestRecovery);
            back = Button(content, "BACK", new Vector2(.73f, .01f), new Vector2(.97f, .105f), Back);
            confirmRoot = TruckTaxiHud.Rect(frame, "Save confirmation", new Vector2(0, .27f), new Vector2(1, .88f));
            promptText = hud.Text(confirmRoot, "Save confirmation text", new Vector2(.03f, .28f), new Vector2(.97f, .97f), 28);
            promptText.richText = false; promptText.fontSizeMin = 18;
            // Cancel is first in focus order, so opening a confirmation never selects its destructive action.
            cancel = Button(confirmRoot, "CANCEL", new Vector2(.03f, .015f), new Vector2(.47f, .19f), Back);
            confirm = Button(confirmRoot, "CONFIRM", new Vector2(.53f, .015f), new Vector2(.97f, .19f), Confirm);
            statusText = hud.Text(frame, "Save operation status", new Vector2(.03f, .11f), new Vector2(.97f, .235f), 22);
            statusText.richText = false; statusText.fontSizeMin = 16;
            confirmRoot.gameObject.SetActive(false);
        }

        public void Open()
        {
            if (panel == null || IsOpen || host != null && (host.CustomContent?.IsPlaying == true ||
                host.CustomContent?.OwnsInputHold == true || host.Companions?.IsBusy == true)) return;
            Bind(); keepPaused?.Invoke(); pending = null; feedback = null; failed = false;
            viewedProfile = persistence?.SaveService?.ActiveProfileId;
            panel.gameObject.SetActive(true); panel.SetAsLastSibling(); Refresh(); VisibilityChanged?.Invoke();
        }

        public void Back()
        {
            if (!IsOpen || IsBusy) return;
            if (pending != null) { pending = null; Refresh(); return; }
            panel.gameObject.SetActive(false); keepPaused?.Invoke(); hud.UIInput?.Focus(null); VisibilityChanged?.Invoke();
        }

        private void Bind()
        {
            var value = resolve?.Invoke();
            if (value == persistence) return;
            if (persistence != null)
            {
                persistence.OperationCompleted -= Completed;
                persistence.LoadCompleted -= Loaded;
            }
            persistence = value;
            if (persistence != null)
            {
                persistence.OperationCompleted += Completed;
                persistence.LoadCompleted += Loaded;
            }
        }
        private void Loaded(LwsSaveOperationResult result)
        {
            if (!result.Succeeded || !IsOpen) return;
            pending = null;
            panel.gameObject.SetActive(false);
            hud.UIInput?.Focus(null);
            VisibilityChanged?.Invoke();
        }
        private void Completed(LwsSaveOperationResult result)
        {
            feedback = result.Message; failed = !result.Succeeded;
            if (result.Succeeded && Ready && !persistence.IsBusy) viewedProfile = persistence.SaveService.ActiveProfileId;
            if (IsOpen) Refresh();
        }
        private void Update()
        {
            if (!IsOpen || Time.unscaledTime < refreshAt) return;
            refreshAt = Time.unscaledTime + .2f; Bind(); Refresh();
        }
        private bool Ready => persistence != null && persistence.Initialized && persistence.SaveService != null;
        private void Browse(int direction)
        {
            if (!Ready || IsBusy || pending != null) return;
            var profiles = persistence.Profiles;
            if (profiles.Count == 0) return;
            int index = 0;
            for (int i = 0; i < profiles.Count; i++) if (profiles[i].stableProfileId == viewedProfile) index = i;
            viewedProfile = profiles[(index + direction + profiles.Count) % profiles.Count].stableProfileId;
            Refresh();
        }
        private LwsManualSaveSlotMetadata Slot(int row) => !Ready || string.IsNullOrEmpty(viewedProfile) ? null : row == 3
            ? persistence.GetAutosaveSlot(viewedProfile) : persistence.SaveService.GetSlotMetadata(viewedProfile, row + 1);
        private bool Occupied(LwsManualSaveSlotMetadata slot) => slot != null && (slot.occupied ||
            (slot.slotType == LwsSaveSlotType.Autosave ? persistence.SaveService.HasAutosave(slot.stableProfileId) :
                persistence.SaveService.HasSave(slot.stableProfileId, slot.slotNumber)));
        private string ProfileName(string id)
        {
            if (persistence != null) foreach (var profile in persistence.Profiles)
                if (profile.stableProfileId == id) return profile.DisplayNameOrFallback;
            return id ?? "NO PROFILE";
        }
        private void RequestSave(int slot)
        {
            if (!Ready || IsBusy || pending != null || viewedProfile != persistence.SaveService.ActiveProfileId) return;
            var metadata = Slot(slot - 1);
            if (metadata == null) { Message("Manual slot is unavailable."); return; }
            if (!Occupied(metadata)) { Run(() => persistence.Save(slot, false)); return; }
            pending = Confirmation.ForSlot(metadata, true);
            promptText.text = "OVERWRITE MANUAL " + slot + "?\n\n" + ProfileName(pending.ProfileId) + "\nSaved " + Timestamp(pending.SavedTicks) +
                "\n\nThe existing save will be backed up, then replaced.";
            Refresh();
        }
        private void RequestLoad(LwsManualSaveSlotMetadata metadata)
        {
            if (!Ready || IsBusy || pending != null || !Occupied(metadata)) return;
            pending = Confirmation.ForSlot(metadata, false);
            promptText.text = "LOAD " + metadata.SlotLabel + "?\n\n" + ProfileName(pending.ProfileId) + "\nSaved " + Timestamp(pending.SavedTicks) +
                "\n\nUnsaved progress will be replaced.";
            Refresh();
        }
        private void RequestRecovery()
        {
            if (!Ready || IsBusy || pending != null || persistence.RecoveryOffer?.available != true) return;
            var offer = persistence.RecoveryOffer;
            pending = Confirmation.ForRecovery(offer);
            promptText.text = "LOAD RECOVERY BACKUP?\n\n" + ProfileName(offer.profileId) + " / " +
                (offer.primarySlotKind == LwsSaveLoadSlotKind.AutosavePrimary ? "AUTOSAVE" : "MANUAL " + offer.primarySlotNumber) +
                "\nBackup " + Timestamp(offer.backupTimestampUtcTicks) + "\n\n" + offer.failureMessage +
                "\n\nProgress since the backup will be lost.";
            Refresh();
        }
        private void Confirm()
        {
            if (!Ready || IsBusy || pending == null) return;
            var request = pending; pending = null;
            if (request.IsRecovery)
            {
                if (!request.Matches(persistence.RecoveryOffer)) { Message("Recovery offer changed. Review the backup again."); return; }
                Run(() => persistence.LoadRecoveryBackup()); return;
            }
            var current = request.SlotType == LwsSaveSlotType.Autosave ? persistence.GetAutosaveSlot(request.ProfileId) :
                persistence.SaveService.GetSlotMetadata(request.ProfileId, request.SlotNumber);
            if (!Occupied(current) || !request.Matches(current, persistence.SaveService.ActiveProfileId)) { Message("Save slot changed. Select it again."); return; }
            Run(() => request.Overwrite ? persistence.Save(request.SlotNumber, true) : request.SlotType == LwsSaveSlotType.Autosave
                ? persistence.LoadAutosave(request.ProfileId) : persistence.Load(request.ProfileId, request.SlotNumber));
        }
        private void Run(Func<LwsSaveOperationResult> operation)
        {
            if (IsBusy) return;
            executing = true; keepPaused?.Invoke(); Refresh();
            try { Completed(operation()); }
            catch (Exception exception) { Completed(LwsSaveOperationResult.Failure("Save/load failed: " + exception.Message)); Debug.LogException(exception, this); }
            finally { executing = false; if (IsOpen) keepPaused?.Invoke(); Refresh(); }
        }
        private void Message(string value) { feedback = value; failed = true; Refresh(); }
        private void Refresh()
        {
            if (!IsOpen) return;
            bool busy = IsBusy, ready = Ready, enabled = ready && !busy && pending == null;
            content.gameObject.SetActive(pending == null); confirmRoot.gameObject.SetActive(pending != null);
            if (ready && string.IsNullOrEmpty(viewedProfile)) viewedProfile = persistence.SaveService.ActiveProfileId;
            bool active = ready && viewedProfile == persistence.SaveService.ActiveProfileId;
            profileText.text = ProfileName(viewedProfile) + (active ? " / ACTIVE PROFILE" : " / VIEWING PROFILE");
            for (int i = 0; i < 4; i++)
            {
                var slot = Slot(i);
                bool occupied = Occupied(slot);
                slotText[i].text = (i == 3 ? "AUTOSAVE" : "MANUAL " + (i + 1)) + "\n" +
                    (occupied ? Timestamp(slot.savedUtcTicks) + "\n" + (slot.worldLabel ?? slot.sceneName ?? "") : "EMPTY");
                loads[i].SetEnabled(enabled && occupied);
                if (i < 3) saves[i].SetEnabled(enabled && active && slot != null);
            }
            previous.SetEnabled(enabled && persistence.Profiles.Count > 1); next.SetEnabled(previous.Enabled);
            latest.SetEnabled(enabled && persistence.HasSave);
            recovery.Root.SetActive(persistence?.RecoveryOffer?.available == true);
            recovery.SetEnabled(enabled && persistence?.RecoveryOffer?.available == true);
            back.SetEnabled(!busy); cancel.SetEnabled(!busy); confirm.SetEnabled(ready && !busy);
            statusText.text = busy ? (persistence?.IsBusy == true ? persistence.Status : "Save operation in progress...") :
                !ready ? persistence?.Status ?? "Vibin persistence is unavailable." : feedback ?? persistence.Status;
            statusText.color = failed && !busy ? new Color(1, .65f, .45f) : new Color(.96f, .96f, .91f);
            hud.UIInput?.Focus(FocusRoot);
        }
        public static string Timestamp(long ticks) => ticks > 0 && ticks <= DateTime.MaxValue.Ticks
            ? new DateTime(ticks, DateTimeKind.Utc).ToString("yyyy-MM-dd HH:mm 'UTC'") : "UNKNOWN DATE";
        private ViewButton Button(Transform parent, string label, Vector2 min, Vector2 max, Action action)
        {
            ViewButton button = null;
            var go = hud.Button(parent, label, min, max, () => { if (button.Enabled) action(); });
            button = new ViewButton(go); return button;
        }
        private sealed class ViewButton
        {
            public readonly GameObject Root;
            private readonly CanvasGroup group;
            private readonly Selectable[] controls;
            public bool Enabled { get; private set; } = true;
            public ViewButton(GameObject root)
            {
                Root = root;
                // Unity's missing-component wrapper is not necessarily a CLR null in the Editor.
                group = root.GetComponent<CanvasGroup>();
                if (group == null) group = root.AddComponent<CanvasGroup>();
                controls = root.GetComponentsInChildren<Selectable>(true);
            }
            public void SetEnabled(bool value)
            {
                if (Enabled == value) return;
                Enabled = value; group.alpha = value ? 1 : .4f; group.interactable = value; group.blocksRaycasts = value;
                foreach (var control in controls) control.interactable = value;
            }
        }
        private void OnDestroy()
        {
            if (persistence == null) return;
            persistence.OperationCompleted -= Completed;
            persistence.LoadCompleted -= Loaded;
        }

        // Immutable identities prevent a confirmation from approving a different slot or backup.
        public sealed class Confirmation
        {
            public string ProfileId { get; private set; }
            public int SlotNumber { get; private set; }
            public LwsSaveSlotType SlotType { get; private set; }
            public long SavedTicks { get; private set; }
            public bool Overwrite { get; private set; }
            public bool IsRecovery { get; private set; }
            private int vendorSlot, primaryVendorSlot;
            private bool occupied;
            private LwsSaveLoadSlotKind primaryKind, backupKind;
            private Confirmation() { }
            public static Confirmation ForSlot(LwsManualSaveSlotMetadata slot, bool overwrite) => new Confirmation
            { ProfileId = slot.stableProfileId, SlotNumber = slot.slotNumber, SlotType = slot.slotType, SavedTicks = slot.savedUtcTicks, vendorSlot = slot.vendorSlotNumber, occupied = slot.occupied, Overwrite = overwrite };
            public static Confirmation ForRecovery(LwsSaveRecoveryOffer offer) => new Confirmation
            { IsRecovery = true, ProfileId = offer.profileId, SlotNumber = offer.primarySlotNumber, SavedTicks = offer.backupTimestampUtcTicks,
                vendorSlot = offer.backupVendorSlotNumber, primaryVendorSlot = offer.primaryVendorSlotNumber, primaryKind = offer.primarySlotKind, backupKind = offer.backupSlotKind };
            public bool Matches(LwsManualSaveSlotMetadata slot, string activeProfileId) => !IsRecovery && slot != null && slot.occupied == occupied &&
                slot.stableProfileId == ProfileId && slot.slotNumber == SlotNumber && slot.slotType == SlotType &&
                slot.savedUtcTicks == SavedTicks && slot.vendorSlotNumber == vendorSlot &&
                (!Overwrite || SlotType == LwsSaveSlotType.Manual && activeProfileId == ProfileId);
            public bool Matches(LwsSaveRecoveryOffer offer) => IsRecovery && offer != null && offer.available &&
                offer.profileId == ProfileId && offer.primarySlotNumber == SlotNumber && offer.backupTimestampUtcTicks == SavedTicks &&
                offer.backupVendorSlotNumber == vendorSlot && offer.primaryVendorSlotNumber == primaryVendorSlot &&
                offer.primarySlotKind == primaryKind && offer.backupSlotKind == backupKind;
        }
    }
}
