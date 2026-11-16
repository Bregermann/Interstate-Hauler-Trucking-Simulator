using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
namespace LWS.TruckSports
{
    public enum SportsGameNightPhase { Lobby, Selection, Configuration, Playing, Results, Ended }
    public sealed class SportsGameNightSeat
    {
        public string DeviceKey { get; internal set; }
        public string ProfileId { get; internal set; }
        public bool Connected { get; internal set; }
        public bool Ready { get; internal set; }
        public bool Guest { get; internal set; }
        public bool Occupied => DeviceKey != null;
        public bool ProfileConfirmed => !string.IsNullOrWhiteSpace(ProfileId);
    }
    public sealed class SportsGameNightOutcome
    {
        public string ProfileId { get; }
        public bool IsAI { get; }
        public bool Winner { get; }
        public SportsGameNightOutcome(string profileId, bool isAI, bool winner)
        {
            if (string.IsNullOrWhiteSpace(profileId)) throw new ArgumentException("A result requires a stable identity.");
            ProfileId = profileId; IsAI = isAI; Winner = winner;
        }
    }
    // Party semantics only. Devices, UI, profile storage and sport rules retain
    // their existing authorities. No per-sport completion/progression tracker.
    public sealed class SportsGameNightState
    {
        readonly SportsGameNightSeat[] seats = Enumerable.Range(0, 4).Select(_ => new SportsGameNightSeat()).ToArray();
        readonly Dictionary<string, int> points = new Dictionary<string, int>(StringComparer.Ordinal);
        readonly HashSet<string> handledMatches = new HashSet<string>(StringComparer.Ordinal);
        readonly HashSet<string> matchHumans = new HashSet<string>(StringComparer.Ordinal);
        readonly ReadOnlyCollection<SportsGameNightSeat> seatView;
        readonly ReadOnlyDictionary<string, int> pointView;
        string configuration;
        HashSet<string> requiredProfiles;
        public string SessionId { get; }
        public string StartedUtc { get; }
        public string EndedUtc { get; private set; }
        public string SelectedSport { get; private set; }
        public string MatchId { get; private set; }
        public SportsGameNightPhase Phase { get; private set; } = SportsGameNightPhase.Lobby;
        public IReadOnlyList<SportsGameNightSeat> Seats => seatView;
        public IReadOnlyDictionary<string, int> Points => pointView;
        public int HumanCount => seats.Count(s => s.Occupied);
        public bool AllReady => HumanCount > 0 && seats.Where((s,i) => IsRequired(i)).All(s => s.Connected && s.ProfileConfirmed && s.Ready);
        public bool IsRequired(int slot) => slot >= 0 && slot < 4 && seats[slot].Occupied && (Phase != SportsGameNightPhase.Configuration || requiredProfiles == null || (requiredProfiles.Count == 0 ? slot == 0 : requiredProfiles.Contains(seats[slot].ProfileId)));
        public bool MatchInProgress => Phase == SportsGameNightPhase.Playing;
        public event Action Changed;
        public SportsGameNightState()
        {
            SessionId = Guid.NewGuid().ToString("N"); StartedUtc = DateTime.UtcNow.ToString("O");
            seatView = Array.AsReadOnly(seats); pointView = new ReadOnlyDictionary<string, int>(points);
        }
        public int SeatForDevice(string key) => Array.FindIndex(seats, s => s.Occupied && s.DeviceKey == key);
        public bool IsHost(string key) => seats[0].Occupied && seats[0].Connected && seats[0].DeviceKey == key;
        void Notify() => Changed?.Invoke();
        void Invalidate() { foreach (var seat in seats) seat.Ready = false; }
        public int Join(string key)
        {
            if (string.IsNullOrWhiteSpace(key) || Phase == SportsGameNightPhase.Ended || MatchInProgress || SeatForDevice(key) >= 0) return -1;
            int slot = Array.FindIndex(seats, s => !s.Occupied);
            if (slot < 0) return -1;
            seats[slot].DeviceKey = key; seats[slot].Connected = true; Invalidate(); Notify(); return slot;
        }
        public bool SelectProfile(int slot, string profileId, bool guest)
        {
            if (slot < 0 || slot >= 4 || !seats[slot].Occupied || MatchInProgress || Phase == SportsGameNightPhase.Ended || string.IsNullOrWhiteSpace(profileId)) return false;
            if (seats.Where((s, i) => i != slot && s.Occupied).Any(s => s.ProfileId == profileId)) return false;
            seats[slot].ProfileId = profileId; seats[slot].Guest = guest;
            if (!points.ContainsKey(profileId)) points.Add(profileId, 0);
            Invalidate(); Notify(); return true;
        }
        public bool SetReady(int slot, bool ready)
        {
            if (slot < 0 || slot >= 4 || MatchInProgress || Phase == SportsGameNightPhase.Ended) return false;
            var seat = seats[slot];
            if (!seat.Occupied || !seat.Connected || !seat.ProfileConfirmed) return false;
            seat.Ready = ready; Notify(); return true;
        }
        public bool SetConnection(string key, bool connected)
        {
            int slot = SeatForDevice(key); if (slot < 0 || seats[slot].Connected == connected) return false;
            seats[slot].Connected = connected; Invalidate(); Notify(); return true;
        }
        public bool ReplaceController(int slot, string replacement)
        {
            if (slot < 0 || slot >= 4 || !seats[slot].Occupied || seats[slot].Connected || string.IsNullOrWhiteSpace(replacement) || SeatForDevice(replacement) >= 0 || Phase == SportsGameNightPhase.Ended) return false;
            seats[slot].DeviceKey = replacement; seats[slot].Connected = true; Invalidate(); Notify(); return true;
        }
        public bool Leave(int slot)
        {
            if (slot < 0 || slot >= 4 || !seats[slot].Occupied || MatchInProgress || Phase == SportsGameNightPhase.Ended) return false;
            seats[slot] = new SportsGameNightSeat();
            if (slot == 0)
            {
                int replacement = Array.FindIndex(seats, s => s.Occupied);
                if (replacement > 0) { seats[0] = seats[replacement]; seats[replacement] = new SportsGameNightSeat(); }
            }
            Phase = SportsGameNightPhase.Lobby; Invalidate(); Notify(); return true;
        }
        public bool TakeHost(int slot)
        {
            if (slot <= 0 || slot >= 4 || !seats[slot].Occupied || !seats[slot].Connected || seats[0].Connected || MatchInProgress || Phase == SportsGameNightPhase.Ended) return false;
            var previous = seats[0]; seats[0] = seats[slot]; seats[slot] = previous; Invalidate(); Notify(); return true;
        }
        public bool OpenSelection(string key)
        {
            if (!IsHost(key) || !AllReady || MatchInProgress || Phase == SportsGameNightPhase.Ended) return false;
            Phase = SportsGameNightPhase.Selection; Notify(); return true;
        }
        public bool ConfigureSport(string key, string sport, string initialConfiguration)
        {
            if (!IsHost(key) || !AllReady || Phase != SportsGameNightPhase.Selection || string.IsNullOrWhiteSpace(sport)) return false;
            SelectedSport = sport; configuration = initialConfiguration; requiredProfiles = null;
            Phase = SportsGameNightPhase.Configuration; Notify(); return true;
        }
        public bool BindInitialConfiguration(string fingerprint)
        {
            if (Phase != SportsGameNightPhase.Configuration || configuration != null || fingerprint == null) return false;
            configuration = fingerprint; Notify(); return true;
        }
        public bool SetMatchParticipants(IEnumerable<string> profileIds)
        {
            if (Phase != SportsGameNightPhase.Configuration || profileIds == null) return false;
            var ids = profileIds.ToArray();
            if (ids.Distinct(StringComparer.Ordinal).Count() != ids.Length || ids.Any(id => !seats.Any(s => s.Occupied && s.ProfileId == id))) return false;
            bool changed = requiredProfiles != null && !requiredProfiles.SetEquals(ids);
            requiredProfiles = new HashSet<string>(ids, StringComparer.Ordinal);
            if (changed) Invalidate(); Notify(); return true;
        }
        public bool PrepareRematch(string key)
        {
            if (!IsHost(key) || Phase != SportsGameNightPhase.Results) return false;
            Phase = SportsGameNightPhase.Configuration; MatchId = null; Invalidate(); Notify(); return true;
        }
        public bool RestartSetup(string key)
        {
            if (!IsHost(key) || !MatchInProgress) return false;
            AbortMatch("restart-or-change-setup"); Phase = SportsGameNightPhase.Configuration; Notify(); return true;
        }
        public bool ChangeConfiguration(string fingerprint)
        {
            if (Phase != SportsGameNightPhase.Configuration || fingerprint == configuration) return false;
            configuration = fingerprint; Invalidate(); Notify(); return true;
        }
        public bool CanStart(string key) => IsHost(key) && AllReady && Phase == SportsGameNightPhase.Configuration;
        public bool ConfirmMatchStarted(string matchId, IEnumerable<string> humanProfiles)
        {
            if (Phase != SportsGameNightPhase.Configuration || !AllReady || string.IsNullOrWhiteSpace(matchId) || handledMatches.Contains(matchId) || humanProfiles == null) return false;
            var ids = humanProfiles.ToArray();
            if (ids.Distinct(StringComparer.Ordinal).Count() != ids.Length || ids.Any(id => !seats.Any(s => s.Occupied && s.ProfileId == id)) || requiredProfiles != null && !requiredProfiles.SetEquals(ids)) return false;
            matchHumans.Clear(); foreach (var id in ids) matchHumans.Add(id);
            MatchId = matchId; Phase = SportsGameNightPhase.Playing; Invalidate(); Notify(); return true;
        }
        public bool CompleteMatch(string matchId, IEnumerable<SportsGameNightOutcome> outcomes)
        {
            if (!MatchInProgress || MatchId != matchId || handledMatches.Contains(matchId) || outcomes == null) return false;
            var rows = outcomes.ToArray();
            if (rows.Length == 0 || rows.Any(r => r == null) || rows.Select(r => r.ProfileId).Distinct(StringComparer.Ordinal).Count() != rows.Length) return false;
            if (matchHumans.Any(id => !rows.Any(r => r.ProfileId == id && !r.IsAI))) return false;
            foreach (var row in rows)
                if (row.Winner && !row.IsAI && matchHumans.Contains(row.ProfileId)) points[row.ProfileId] = checked(points[row.ProfileId] + 1);
            handledMatches.Add(matchId); Phase = SportsGameNightPhase.Results; Notify(); return true;
        }
        public bool AbortMatch(string reason)
        {
            if (!MatchInProgress || string.IsNullOrWhiteSpace(reason)) return false;
            handledMatches.Add(MatchId); Phase = SportsGameNightPhase.Lobby; MatchId = null;
            matchHumans.Clear(); Invalidate(); Notify(); return true;
        }
        public bool ReturnToLobby()
        {
            if (MatchInProgress || Phase == SportsGameNightPhase.Ended) return false;
            Phase = SportsGameNightPhase.Lobby; MatchId = null; matchHumans.Clear(); requiredProfiles = null; Invalidate(); Notify(); return true;
        }
        public bool EndNight(string key)
        {
            if (!IsHost(key) || MatchInProgress || Phase == SportsGameNightPhase.Ended) return false;
            Phase = SportsGameNightPhase.Ended; EndedUtc = DateTime.UtcNow.ToString("O"); Invalidate(); Notify(); return true;
        }
    }
}
