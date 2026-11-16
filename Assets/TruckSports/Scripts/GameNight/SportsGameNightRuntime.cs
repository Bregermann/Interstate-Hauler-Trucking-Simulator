using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace LWS.TruckSports
{
    // One game-night owner. NWH, individual sport rules, profile preferences and
    // the Input System keep their existing responsibilities.
    [DefaultExecutionOrder(-550)]
    public sealed class SportsGameNightRuntime : MonoBehaviour
    {
        public static SportsGameNightRuntime Instance { get; private set; }
        public static bool Active => Instance != null && Instance.State != null && Instance.State.Phase != SportsGameNightPhase.Ended;
        public SportsGameNightState State { get; private set; }
        public SportsGameNightReport Report { get; private set; }
        readonly Dictionary<string, Gamepad> pads = new Dictionary<string, Gamepad>();
        readonly Dictionary<string, SportsParticipant> guests = new Dictionary<string, SportsParticipant>();
        SportsGameNightView view;
        SportsPartyMatchBinding binding;
        SportsFrontEnd front;
        InputSystemUIInputModule ui;
        InputActionAsset originalUiActions, scopedUiActions;
        bool originalNavigation;
        bool uiLease;
        int gateFrame;
        string observedMatch;
        double matchPlayedSeconds;
        public bool LobbyVisible => view != null && view.Visible;
        public bool InSport => binding != null;
        public static bool BlockSharedMenus => SportsControllerKeyboard.InputBlocked || Instance != null && (Instance.LobbyVisible || Time.frameCount <= Instance.gateFrame);
        public string HostKey => State != null ? State.Seats[0].DeviceKey : null;
        public static string DeviceKey(Gamepad pad) => "pad:" + pad.deviceId;
        public Gamepad Pad(int seat) => seat >= 0 && seat < 4 && State != null && State.Seats[seat].DeviceKey != null && pads.TryGetValue(State.Seats[seat].DeviceKey, out var pad) ? pad : null;
        public SportsParticipant Profile(int seat) => State != null && seat >= 0 && seat < 4 ? Profile(State.Seats[seat].ProfileId) : null;
        public SportsParticipant Profile(string id) => id != null && guests.TryGetValue(id, out var guest) ? guest : SportsParticipantRegistry.Find(id);
        public static bool IsGuest(string id) => id != null && id.StartsWith("guest:", StringComparison.Ordinal);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { Instance = null; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (Instance != null || FindAnyObjectByType<SportsFrontEnd>() == null) return;
            var owner = new GameObject("Truck Sports game night"); DontDestroyOnLoad(owner); owner.AddComponent<SportsGameNightRuntime>();
        }
        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this; DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += Loaded; InputSystem.onDeviceChange += DeviceChanged;
            SportsControllerKeyboard.MenuPadAllowed = IsMenuPadAllowed;
            StartCoroutine(BindScene(SceneManager.GetActiveScene()));
        }
        void OnDestroy()
        {
            if (Instance != this) return;
            SceneManager.sceneLoaded -= Loaded; InputSystem.onDeviceChange -= DeviceChanged;
            Report?.Save(true, State != null && State.Phase != SportsGameNightPhase.Ended); Report?.Dispose();
            SportsControllerKeyboard.MenuPadAllowed = null; RestoreUi(); Instance = null;
        }
        void Loaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            // The old MonoBehaviour is already destroyed here. Never poll its
            // controller roster while waiting one frame for the new scene UI.
            binding = null; observedMatch = null;
            StartCoroutine(BindScene(scene));
        }
        IEnumerator BindScene(Scene scene)
        {
            yield return null; if (!scene.isLoaded || scene != SceneManager.GetActiveScene()) yield break;
            RestoreUi(); ui = FindAnyObjectByType<InputSystemUIInputModule>();
            if (ui != null && ui.actionsAsset != null)
            {
                originalUiActions = ui.actionsAsset; scopedUiActions = Instantiate(originalUiActions); ui.actionsAsset = scopedUiActions;
                originalNavigation = EventSystem.current == null || EventSystem.current.sendNavigationEvents;
                uiLease = true;
            }
            front = FindAnyObjectByType<SportsFrontEnd>(); binding = null; observedMatch = null;
            if (front != null)
            {
                if (view == null) view = SportsGameNightView.Create(this, front);
                if (Active) ShowLobby();
            }
            else if (Active)
            {
                var session = FindAnyObjectByType<SportsEventSession>();
                if (session != null)
                {
                    binding = new SportsPartyMatchBinding(this, session);
                }
                else
                {
                    var legacy = SportsLegacySessionAdapter.Find();
                    if (legacy != null) binding = new SportsPartyMatchBinding(this, legacy);
                }
                if (binding != null)
                {
                    binding.Apply();
                    State.BindInitialConfiguration(binding.Fingerprint());
                    State.SetMatchParticipants(binding.HumanProfileIds());
                }
            }
            ApplyUiAuthority();
        }
        void RestoreUi()
        {
            if (ui != null && originalUiActions != null) ui.actionsAsset = originalUiActions;
            if (scopedUiActions != null) Destroy(scopedUiActions);
            scopedUiActions = originalUiActions = null; ui = null;
            if (uiLease && EventSystem.current != null) EventSystem.current.sendNavigationEvents = originalNavigation;
            uiLease = false;
        }
        void ApplyUiAuthority()
        {
            // The saved title canvas has its own sorting order. Hide it while
            // our separate lobby/profile canvas owns the front-end screen.
            if (front != null)
            {
                var frontCanvas = front.GetComponentInParent<Canvas>();
                if (frontCanvas != null) frontCanvas.enabled = !LobbyVisible;
            }
            if (scopedUiActions == null) return;
            if (!Active) { scopedUiActions.devices = null; if (EventSystem.current != null) EventSystem.current.sendNavigationEvents = !LobbyVisible && originalNavigation; return; }
            var devices = new List<InputDevice>();
            if (Keyboard.current != null) devices.Add(Keyboard.current);
            if (Mouse.current != null) devices.Add(Mouse.current);
            if (!LobbyVisible && Pad(0) != null && State.Seats[0].Connected) devices.Add(Pad(0));
            scopedUiActions.devices = devices.ToArray();
            if (EventSystem.current != null) EventSystem.current.sendNavigationEvents = !LobbyVisible;
        }
        public static bool IsMenuPadAllowed(Gamepad pad) => !BlockSharedMenus && (!Active || pad != null && Instance.State.IsHost(DeviceKey(pad)));
        public void RefreshUiAuthority() { gateFrame = Time.frameCount + 1; ApplyUiAuthority(); }
        public static bool InterceptFrontAction(SportsFrontEnd owner, string action)
        {
            if (Instance == null) return false;
            if (BlockSharedMenus) return true;
            if (action == "back" && Active && owner.Page == 1 && Instance.State.Phase == SportsGameNightPhase.Selection) { Instance.ShowLobby(); return true; }
            if (action == "play") { Instance.Open(owner, SportsControllerKeyboard.SubmittingPad()); return true; }
            return false;
        }
        public void Open(SportsFrontEnd owner, Gamepad firstPad = null)
        {
            front = owner;
            if (!Active)
            {
                Report?.Dispose(); State = new SportsGameNightState(); guests.Clear(); pads.Clear();
                Report = new SportsGameNightReport(State); SportsSessionTelemetry.BeginGameNight(State.SessionId);
            }
            if (view == null) view = SportsGameNightView.Create(this, owner);
            ShowLobby(); if (firstPad != null && CanJoin(firstPad)) Join(firstPad);
        }
        public void ShowLobby()
        {
            if (State == null || State.Phase == SportsGameNightPhase.Ended) return;
            if (State.MatchInProgress) { Report?.Interrupt("return-to-lobby"); State.AbortMatch("return-to-lobby"); }
            State.ReturnToLobby(); view.Show(); gateFrame = Time.frameCount + 1; ApplyUiAuthority();
        }
        public bool Join(Gamepad pad)
        {
            if (!CanJoin(pad)) return false;
            int slot = State.Join(DeviceKey(pad)); if (slot < 0) return false;
            pads[DeviceKey(pad)] = pad; view.ProfileSelection(slot); gateFrame = Time.frameCount + 1; return true;
        }
        public bool JoinKeyboard()
        {
            if (Keyboard.current == null || State == null) return false;
            int slot = State.Join("keyboard"); if (slot < 0) return false;
            view.ProfileSelection(slot); gateFrame = Time.frameCount + 1; return true;
        }
        public static bool CanJoin(Gamepad pad)
        {
            if (pad == null || !pad.added || !pad.enabled) return false;
            string product = ((pad.description.product ?? "") + " " + pad.layout).ToLowerInvariant();
            return !new[] { "steering", "wheel", "pedal", "shifter" }.Any(product.Contains);
        }
        public bool SelectProfile(int slot, string id)
        {
            var profile = Profile(id); if (profile == null || !State.SelectProfile(slot, id, IsGuest(id))) return false;
            if (!IsGuest(id)) SportsParticipantRegistry.UseInSeat(slot, id);
            if (!IsGuest(id)) SportsParticipantRegistry.RecordGameNightVisit(id, State.SessionId);
            Report?.Remember(profile);
            view.Refresh(); return true;
        }
        public void SelectGuest(int slot)
        {
            string id = "guest:" + State.SessionId + ":" + slot;
            if (!guests.ContainsKey(id)) guests[id] = new SportsParticipant { id = id, displayName = "Guest " + (slot + 1), color = Color.HSVToRGB(slot * .22f, .65f, 1) };
            SelectProfile(slot, id);
        }
        public void ProfileChanged(int slot) { var profile = Profile(slot); if (profile != null) { State.SelectProfile(slot, profile.id, IsGuest(profile.id)); Report?.Remember(profile); } view.Refresh(); }
        public string DriverName(string id) => Profile(id)?.displayName ?? Report?.NameFor(id) ?? "Driver";
        public void SelectSport()
        {
            if (front == null || !State.OpenSelection(HostKey)) { view.Notice("Every participating player must choose a profile and ready up."); return; }
            view.Hide(); ApplyUiAuthority(); front.Show(1);
        }
        public static bool BeforeLoadSport(string scene)
        {
            if (!Active) return true;
            if (!Instance.State.ConfigureSport(Instance.HostKey, scene, null)) { Instance.ShowLobby(); return false; }
            return true;
        }
        public static void BeforeReturnToCollection()
        {
            if (!Active) return;
            if (Instance.State.MatchInProgress) { Instance.Report?.Interrupt("return-to-menu"); Instance.State.AbortMatch("return-to-menu"); }
            Instance.State.ReturnToLobby();
        }
        public static SportsParticipant ProfileForSlot(ISportsParticipantSlot[] slots, int index)
            => Active && Instance.binding != null && ReferenceEquals(Instance.binding.Players, slots) ? Instance.binding.ProfileForSlot(index) : null;
        public static void ConfigurationEdited(SportsEventSession session)
            => ConfigurationEditedOwner(session);
        public static void ConfigurationEditedOwner(Component owner)
        {
            if (!Active || Instance.binding == null || Instance.binding.Owner != owner || Instance.binding.Applying || Instance.binding.Running && !Instance.binding.Finished) return;
            Instance.binding.Apply(); Instance.State.SetMatchParticipants(Instance.binding.HumanProfileIds());
            Instance.State.ChangeConfiguration(Instance.binding.Fingerprint());
        }
        public static bool TryStart(SportsEventSession session)
            => TryStartOwner(session);
        public static bool TryStartOwner(Component session)
        {
            if (!Active) return true;
            var owner = Instance;
            if (owner.binding == null || owner.binding.Owner != session) return false;
            if (owner.State.Phase == SportsGameNightPhase.Results || owner.State.MatchInProgress)
            {
                PrepareSetupOwner(session); owner.binding.Setup();
            }
            ConfigurationEditedOwner(session);
            if (owner.State.CanStart(owner.HostKey)) return true;
            owner.view.Notice("Ready check: each participating player presses START / MENU, then Player 1 starts the match."); return false;
        }
        public static void PrepareSetup(SportsEventSession session)
            => PrepareSetupOwner(session);
        public static void PrepareSetupOwner(Component session)
        {
            if (!Active || Instance.binding == null || Instance.binding.Owner != session) return;
            if (Instance.State.MatchInProgress) { Instance.Report?.Interrupt("restart-or-change-setup"); Instance.State.RestartSetup(Instance.HostKey); }
            else if (Instance.State.Phase == SportsGameNightPhase.Results) Instance.State.PrepareRematch(Instance.HostKey);
        }
        public static bool InterceptSetupAction(SportsEventSession session, string action)
        {
            if (!Active || Instance.binding == null || Instance.binding.Session != session) return false;
            if (BlockSharedMenus) return true;
            if (action == "allai") { Instance.binding.SpectateAI = true; return false; }
            if (session.menus.Screen == SportsEventScreen.Setup && (action.StartsWith("source", StringComparison.Ordinal) || action.StartsWith("pad", StringComparison.Ordinal)))
            { Instance.view.Notice("Controllers and human profiles come from the game-night lobby."); return true; }
            return false;
        }
        public static bool InterceptOwnerAction(Component owner, string action)
        {
            if (!Active || Instance.binding == null || Instance.binding.Owner != owner) return false;
            if (BlockSharedMenus) return true;
            if (Instance.State.Phase != SportsGameNightPhase.Configuration) return false;
            if (action == "allai")
            {
                if (Instance.binding.SupportsAiSpectating) { Instance.binding.SpectateAI = true; ConfigurationEditedOwner(owner); }
                return true;
            }
            if (!string.IsNullOrEmpty(action) && (action.StartsWith("source", StringComparison.Ordinal) || action.StartsWith("pad", StringComparison.Ordinal) ||
                action.StartsWith("control", StringComparison.Ordinal) || Instance.binding.Legacy != null && Instance.binding.Legacy.InputPresetModes && action == "mode"))
            { Instance.view.Notice("Choose profiles and controllers in the game-night lobby. Y switches between crew and AI spectating."); return true; }
            return false;
        }
        public static bool MenuConfirmPressed()
        {
            if (!Active || SportsControllerKeyboard.InputBlocked) return false;
            if (Time.frameCount <= Instance.gateFrame) return false;
            var pad = Instance.Pad(0);
            return pad != null && pad.added && pad.buttonSouth.wasPressedThisFrame || Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame) || Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
        }
        void Update()
        {
            if (State != null && State.Phase == SportsGameNightPhase.Ended && view != null && view.Visible && !SportsControllerKeyboard.InputBlocked) { view.TickEnded(); return; }
            if (!Active || SportsControllerKeyboard.InputBlocked || Time.frameCount <= gateFrame) return;
            if (LobbyVisible) { view.Tick(); return; }
            if (binding == null) return;
            foreach (var pad in Gamepad.all)
                if (CanJoin(pad) && State.SeatForDevice(DeviceKey(pad)) < 0 && pad.startButton.wasPressedThisFrame && State.Seats.Any(s => s.Occupied && !s.Connected))
                { view.OfferReplacement(pad); return; }
            binding.SyncAssignedControllers();
            if (binding.Running && !binding.Finished)
            {
                if (observedMatch != binding.MatchId)
                {
                    observedMatch = binding.MatchId;
                    matchPlayedSeconds = 0;
                    if (State.ConfirmMatchStarted(observedMatch, binding.HumanProfileIds())) Report?.BeginMatch(binding);
                }
                if (binding.Live) matchPlayedSeconds += Time.unscaledDeltaTime;
            }
            else if (binding.Finished && State.MatchInProgress && binding.TryResults(out var snapshot))
            {
                if (State.CompleteMatch(binding.MatchId, snapshot.Rows.Select(r => new SportsGameNightOutcome(r.ParticipantId, r.IsAI, r.Winner))))
                {
                    SportsParticipantRegistry.RecordFinalStatistics(State.SessionId, binding.MatchId, binding.SceneName,
                        binding.StatisticsVariant, snapshot, binding.HumanProfileIds(), matchPlayedSeconds, binding.Title, binding.OptionLabel);
                    SportsSessionTelemetry.MatchCompleted(binding.MatchId, binding.SceneName, snapshot);
                    Report?.Complete(snapshot, binding.HumanProfileIds(), matchPlayedSeconds);
                }
            }
            if (State.Phase == SportsGameNightPhase.Configuration)
            {
                if (Pad(0) != null && Pad(0).buttonNorth.wasPressedThisFrame || Keyboard.current != null && Keyboard.current.f6Key.wasPressedThisFrame)
                {
                    if (binding.SupportsAiSpectating) { binding.SpectateAI = !binding.SpectateAI; ConfigurationEditedOwner(binding.Owner); }
                }
                for (int slot = 0; slot < 4; slot++)
                {
                    var pad = Pad(slot);
                    if (pad != null && pad.added && pad.startButton.wasPressedThisFrame) State.SetReady(slot, !State.Seats[slot].Ready);
                    if (State.Seats[slot].DeviceKey == "keyboard" && Keyboard.current != null && Keyboard.current.f5Key.wasPressedThisFrame) State.SetReady(slot, !State.Seats[slot].Ready);
                }
            }
            view.ReadyBar(!binding.Live && !binding.Finished, State, binding.SpectateAI, binding.SupportsAiSpectating);
        }
        void DeviceChanged(InputDevice device, InputDeviceChange change)
        {
            if (!Active || !(device is Gamepad pad)) return;
            string key = DeviceKey(pad);
            if (change == InputDeviceChange.Disconnected || change == InputDeviceChange.Removed || change == InputDeviceChange.Disabled) State.SetConnection(key, false);
            else if (change == InputDeviceChange.Reconnected || change == InputDeviceChange.Added || change == InputDeviceChange.Enabled)
            {
                if (State.SeatForDevice(key) >= 0) { pads[key] = pad; State.SetConnection(key, true); }
                else if (!string.IsNullOrEmpty(pad.description.serial))
                    for (int i = 0; i < 4; i++)
                    {
                        var previous = Pad(i);
                        if (State.Seats[i].Occupied && !State.Seats[i].Connected && previous != null && previous.description.serial == pad.description.serial && previous.description.product == pad.description.product && State.ReplaceController(i, key)) { pads[key] = pad; break; }
                    }
                binding?.ApplyControllers();
            }
            ApplyUiAuthority(); view?.Refresh();
        }
        public bool ReplaceController(int slot, Gamepad pad)
        {
            if (!CanJoin(pad) || !State.ReplaceController(slot, DeviceKey(pad))) return false;
            pads[DeviceKey(pad)] = pad; binding?.ApplyControllers(); gateFrame = Time.frameCount + 1; ApplyUiAuthority(); view.Refresh(); return true;
        }
        public void EndNight()
        {
            if (!State.EndNight(HostKey)) return;
            SportsSessionTelemetry.EndGameNight(State.SessionId); Report?.Save(true);
            view.ShowFinalStandings(); ApplyUiAuthority();
        }
        public bool ExportReport() => Report != null && Report.Save(State.Phase == SportsGameNightPhase.Ended);
    }
}
