using System;
using System.Collections.Generic;
using System.Linq;
using LWS.TruckBowling;

namespace LWS.TruckSports
{
    // Adapter for the existing common session. It assigns devices/identities,
    // never touches driving, physics, AI decisions or sport scoring.
    public sealed class SportsPartyMatchBinding
    {
        readonly SportsGameNightRuntime night;
        readonly Dictionary<int, int> humans = new Dictionary<int, int>();
        public SportsEventSession Session { get; }
        public bool Applying { get; private set; }
        public bool SpectateAI { get; set; }
        public SportsPartyMatchBinding(SportsGameNightRuntime night, SportsEventSession session) { this.night = night; Session = session; }
        public void Apply()
        {
            if (Applying) return;
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
            }
            finally { Applying = false; }
        }
        public SportsParticipant ProfileForSlot(int slot)
        {
            if (humans.TryGetValue(slot, out int seat)) return night.Profile(seat);
            int ai = 0; for (int i = 0; i < slot; i++) if (!humans.ContainsKey(i)) ai++;
            return SportsParticipantRegistry.Default(4 + ai);
        }
        public void ApplyControllers()
        {
            foreach (var pair in humans)
            {
                var player = Session.setup.players[pair.Key]; player.pad = night.Pad(pair.Value);
            }
            Session.Gate(); Session.menus.Dirty();
        }
        public void SyncAssignedControllers()
        {
            foreach (var pair in humans)
            {
                var pad = Session.setup.players[pair.Key].pad;
                if (pad != null && pad.added && pad.enabled && pad != night.Pad(pair.Value) && !night.State.Seats[pair.Value].Connected)
                    night.ReplaceController(pair.Value, pad);
            }
        }
        public string[] HumanProfileIds() => humans.Where(p => p.Key < Session.setup.count).OrderBy(p => p.Key).Select(p => night.State.Seats[p.Value].ProfileId).ToArray();
        public string Fingerprint()
        {
            var p = Session.setup.players.Take(Session.setup.count).Select(player => player.participantId + ":" + player.source + ":" + player.difficulty);
            return Session.gameObject.scene.name + "/" + Session.option + "/" + Session.teamA + "/" + Session.teamB + "/" + Session.weather + "/" + Session.setup.count + "/" + string.Join(";", p);
        }
        public bool TryResults(out SportsResultsSnapshot snapshot) => SportsFinishResultsSource.TryCaptureSession(Session, out snapshot);
    }
}
