using System;
using System.Collections.Generic;
using System.Linq;
using LWS.TruckBowling;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LWS.TruckSports
{
    // Adapter for the existing common session. It assigns devices/identities,
    // never touches driving, physics, AI decisions or sport scoring.
    public sealed class SportsPartyMatchBinding
    {
        readonly SportsGameNightRuntime night;
        readonly Dictionary<int, int> humans = new Dictionary<int, int>();
        public SportsEventSession Session { get; }
        public SportsLegacySessionAdapter Legacy { get; }
        public Component Owner => Session != null ? (Component)Session : Legacy.Owner;
        public ISportsParticipantSlot[] Players => Session != null ? Session.setup.players : Legacy.Players;
        public int Count => Session != null ? Session.setup.count : Legacy.Count;
        public bool Running => Session != null ? Session.Running : Legacy.Running;
        public bool Finished => Session != null ? Session.Finished : Legacy.Finished;
        public bool Live => Session != null ? Session.Live : Legacy.Live;
        public string MatchId => Session != null ? Session.MatchId : Legacy.MatchId;
        public string Title => Session != null ? Session.Title : Legacy.Title;
        public string OptionLabel => Session != null ? Session.OptionLabel : Legacy.Variant;
        public string SceneName => Owner.gameObject.scene.name;
        public bool SupportsAiSpectating => !(Legacy?.Owner is LWS.TruckBilliards.BilliardsSession billiards && billiards.IsPractice);
        public bool Applying { get; private set; }
        public bool SpectateAI { get; set; }
        public SportsPartyMatchBinding(SportsGameNightRuntime night, SportsEventSession session) { this.night = night; Session = session; }
        public SportsPartyMatchBinding(SportsGameNightRuntime night, SportsLegacySessionAdapter legacy) { this.night = night; Legacy = legacy; }
        public void Apply()
        {
            if (Applying) return;
            if (Legacy != null) { ApplyLegacy(); return; }
            Applying = true;
            try
            {
                int[] seats = Enumerable.Range(0, 4).Where(i => night.State.Seats[i].Occupied && night.State.Seats[i].ProfileConfirmed).ToArray();
                int count = SpectateAI ? 0 : Math.Min(seats.Length, Session.MaximumPlayers);
                if (Session.TeamSport)
                {
                    Session.teamA = Math.Max(Session.teamA, (count + 1) / 2);
                    Session.teamB = Math.Max(Session.teamB, count / 2);
                }
                Session.setup.count = Math.Max(Session.setup.count, Math.Max(Session.MinimumPlayers, count));
                Session.NormalizeSetup(); humans.Clear();
                for (int n = 0; n < count; n++)
                {
                    int slot = Session.TeamSport ? n % 2 == 0 ? n / 2 : Session.teamA + n / 2 : n;
                    if (slot < Session.setup.count) humans[slot] = seats[n];
                }
                for (int slot = 0; slot < Session.setup.players.Length; slot++)
                {
                    var player = Session.setup.players[slot];
                    if (!humans.TryGetValue(slot, out int seat)) { player.source = BowlingSource.AI; player.pad = null; }
                    else
                    {
                        player.source = night.State.Seats[seat].DeviceKey == "keyboard" ? BowlingSource.Keyboard : BowlingSource.WholeController;
                        player.pad = night.Pad(seat);
                    }
                }
                Session.RefreshParticipants(); Session.Gate(); Session.Save(); Session.menus.Dirty();
                foreach (var field in Session.menus.names) if (field != null) field.readOnly = true;
                foreach (var action in Session.menus.GetComponentsInChildren<SportsMenuAction>(true))
                {
                    if (string.IsNullOrEmpty(action.action)) continue;
                    if (!action.action.StartsWith("source", StringComparison.Ordinal) && !action.action.StartsWith("pad", StringComparison.Ordinal)) continue;
                    action.button.isInteractable = false;
                    var selectable = action.button.GetComponent<UnityEngine.UI.Selectable>(); if (selectable != null) selectable.interactable = false;
                }
                var selected = UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject;
                var selectedField = selected != null ? selected.GetComponent<TMPro.TMP_InputField>() : null;
                if (selected == null || selectedField != null && selectedField.readOnly) Session.menus.Focus();
            }
            finally { Applying = false; }
        }
        void ApplyLegacy()
        {
            Applying = true;
            try
            {
                int[] seats = Enumerable.Range(0, 4).Where(i => night.State.Seats[i].Occupied && night.State.Seats[i].ProfileConfirmed).ToArray();
                Legacy.EnsureHumanCapacity(SpectateAI ? 0 : seats.Length);
                int count = SpectateAI ? 0 : Math.Min(seats.Length, Legacy.Maximum); humans.Clear();
                for (int n = 0; n < count; n++)
                {
                    int slot = Legacy.TeamSplit > 0 ? n % 2 == 0 ? n / 2 : Legacy.TeamSplit + n / 2 : n;
                    if (slot < Legacy.Count) humans[slot] = seats[n];
                }
                for (int slot = 0; slot < Players.Length; slot++)
                {
                    bool human = humans.TryGetValue(slot, out int seat);
                    Legacy.Assign(slot, human ? night.Pad(seat) : null, human && night.State.Seats[seat].DeviceKey == "keyboard", !human);
                }
                Legacy.Refresh(); Legacy.Save(); Legacy.Gate();
                foreach (var field in Legacy.MenuRoot.GetComponentsInChildren<TMPro.TMP_InputField>(true)) field.readOnly = true;
                foreach (var action in Legacy.MenuRoot.GetComponentsInChildren<SportsMenuAction>(true))
                    if (ManagedLegacyAction(action.action)) Disable(action.button);
                foreach (var action in Legacy.MenuRoot.GetComponentsInChildren<LWS.TruckBilliards.BilliardsMenuAction>(true))
                    if (ManagedLegacyAction(action.action)) Disable(action.button);
                var selected = UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject;
                var selectable = selected != null ? selected.GetComponent<UnityEngine.UI.Selectable>() : null;
                if (selectable == null || !selectable.IsInteractable() || selectable is TMPro.TMP_InputField input && input.readOnly) Legacy.Focus();
            }
            finally { Applying = false; }
        }
        bool ManagedLegacyAction(string action) => !string.IsNullOrEmpty(action) && (action.StartsWith("source", StringComparison.Ordinal) || action.StartsWith("pad", StringComparison.Ordinal) || action.StartsWith("control", StringComparison.Ordinal) || Legacy.InputPresetModes && action == "mode");
        static void Disable(Michsky.UI.Heat.ButtonManager button)
        {
            if (button == null) return; button.isInteractable = false;
            var selectable = button.GetComponent<UnityEngine.UI.Selectable>(); if (selectable != null) selectable.interactable = false;
        }
        public SportsParticipant ProfileForSlot(int slot)
        {
            if (humans.TryGetValue(slot, out int seat)) return night.Profile(seat);
            int ai = 0; for (int i = 0; i < slot; i++) if (!humans.ContainsKey(i)) ai++;
            var humanIds = new HashSet<string>(humans.Values.Select(s => night.State.Seats[s].ProfileId), StringComparer.Ordinal);
            for (int index = 4; ; index++)
            {
                var candidate = SportsParticipantRegistry.Default(index);
                if (!humanIds.Contains(candidate.id) && ai-- == 0) return candidate;
            }
        }
        public void ApplyControllers()
        {
            foreach (var pair in humans)
            {
                if (Legacy != null) Legacy.Assign(pair.Key, night.Pad(pair.Value), night.State.Seats[pair.Value].DeviceKey == "keyboard", false);
                else Session.setup.players[pair.Key].pad = night.Pad(pair.Value);
            }
            if (Legacy != null) Legacy.Gate(); else { Session.Gate(); Session.menus.Dirty(); }
        }
        public void SyncAssignedControllers()
        {
            foreach (var pair in humans)
            {
                var pad = Controller(pair.Key);
                if (pad != null && pad.added && pad.enabled && pad != night.Pad(pair.Value) && !night.State.Seats[pair.Value].Connected)
                    night.ReplaceController(pair.Value, pad);
            }
        }
        public Gamepad Controller(int slot) => Session != null ? Session.setup.players[slot].pad : Legacy.Pad(slot);
        public string Source(int slot) => Session != null ? Session.setup.players[slot].source.ToString() : Legacy.Source(slot);
        public string Difficulty(int slot) => Session != null ? Session.setup.players[slot].difficulty.ToString() : Legacy.Difficulty(slot);
        public void Setup() { if (Session != null) Session.SetupScreen(); else Legacy.Setup(); }
        public string[] HumanProfileIds() => humans.Where(p => p.Key < Count).OrderBy(p => p.Key).Select(p => night.State.Seats[p.Value].ProfileId).ToArray();
        public string Fingerprint()
        {
            if (Legacy != null) return SceneName + "/" + Count + "/" + Legacy.Configuration;
            var p = Session.setup.players.Take(Session.setup.count).Select(player => player.participantId + ":" + player.source + ":" + player.difficulty);
            return Session.gameObject.scene.name + "/" + Session.option + "/" + Session.teamA + "/" + Session.teamB + "/" + Session.weather + "/" + Session.setup.count + "/" + string.Join(";", p);
        }
        public string StatisticsVariant => Legacy != null ? Legacy.Variant : Session is LWS.TruckDanceOff.DanceMatch dance ? "option:" + Session.option + "/format:" + dance.Format : "option:" + Session.option;
        public bool TryResults(out SportsResultsSnapshot snapshot)
        {
            if (Legacy != null) { if (!Legacy.TryResults(out snapshot)) return false; }
            else if (!SportsFinishResultsSource.TryCaptureSession(Session, out snapshot)) return false;
            snapshot = SportsStatisticsResults.Prepare(Session, snapshot); return true;
        }
    }
}
