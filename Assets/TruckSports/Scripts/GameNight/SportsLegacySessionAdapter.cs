using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using LWS.TruckBowling;
using LWS.TruckCornhole;
using LWS.TruckCheeseRolling;
using LWS.TruckHockey;
using LWS.TruckAirHockey;
using LWS.TruckWallball;
using LWS.TruckBilliards;
namespace LWS.TruckSports
{
    // Typed access to the seven older session owners. No reflection, replacement
    // game rules, vendor modifications or per-frame object searches.
    public sealed class SportsLegacySessionAdapter
    {
        public Component Owner { get; }
        SportsLegacySessionAdapter(Component owner) { Owner = owner; }
        public static SportsLegacySessionAdapter Find()
        {
            Component owner = UnityEngine.Object.FindAnyObjectByType<BowlingMatch>();
            if (owner == null) owner = UnityEngine.Object.FindAnyObjectByType<CornholeMatch>();
            if (owner == null) owner = UnityEngine.Object.FindAnyObjectByType<CheeseMatch>();
            if (owner == null) owner = UnityEngine.Object.FindAnyObjectByType<HockeyMatch>();
            if (owner == null) owner = UnityEngine.Object.FindAnyObjectByType<AirHockeyMatch>();
            if (owner == null) owner = UnityEngine.Object.FindAnyObjectByType<WallballMatch>();
            if (owner == null) owner = UnityEngine.Object.FindAnyObjectByType<BilliardsSession>();
            return owner != null ? new SportsLegacySessionAdapter(owner) : null;
        }
        public ISportsParticipantSlot[] Players => Owner switch
        { BowlingMatch m => m.setup.players, CornholeMatch m => m.setup.players, CheeseMatch m => m.setup.players,
          HockeyMatch m => m.players, AirHockeyMatch m => m.players, WallballMatch m => m.players, BilliardsSession m => m.players, _ => throw new InvalidOperationException() };
        public int Count => Owner switch
        { BowlingMatch m => m.setup.count, CornholeMatch m => m.setup.count, CheeseMatch m => m.setup.count,
          WallballMatch m => m.Count, BilliardsSession m => m.IsPractice ? 1 : 2, _ => Players.Length };
        public int Maximum => Owner switch
        { BowlingMatch m => m.setup.mode == BowlingMode.HotSeat ? 8 : 4, CornholeMatch _ => 4, CheeseMatch _ => 4,
          WallballMatch _ => 4, BilliardsSession m => m.IsPractice ? 1 : 2, _ => Players.Length };
        public int TeamSplit => Owner switch { HockeyMatch _ => 3, WallballMatch m => m.blueSize, _ => 0 };
        public Transform MenuRoot => Owner switch
        { BowlingMatch m => m.menus.transform, CornholeMatch m => m.menus.transform, CheeseMatch m => m.menus.transform,
          HockeyMatch m => m.menus.transform, AirHockeyMatch m => m.menus.transform, WallballMatch m => m.menus.transform, BilliardsSession m => m.menus.transform, _ => null };
        public bool InputPresetModes => Owner is HockeyMatch || Owner is AirHockeyMatch || Owner is WallballMatch || Owner is BilliardsSession;
        public string Title => Owner switch
        { BowlingMatch _ => "TRUCK BOWLING", CornholeMatch _ => "TRUCK CORNHOLE", CheeseMatch _ => "CHEESE ROLLING",
          HockeyMatch _ => "TRUCK HOCKEY", AirHockeyMatch _ => "TRUCK AIR HOCKEY", WallballMatch _ => "TRUCK WALLBALL", _ => "TRUCK BILLIARDS" };
        public bool Running => Owner switch
        { BowlingMatch m => m.InMatch, CornholeMatch m => m.InMatch, CheeseMatch m => m.InMatch, HockeyMatch m => m.InMatch,
          AirHockeyMatch m => m.InMatch, WallballMatch m => m.InMatch, BilliardsSession m => m.InMatch, _ => false };
        public bool Finished => Running && (Owner switch
        { BowlingMatch m => m.Finished, CornholeMatch m => m.Finished, CheeseMatch m => m.Finished,
          HockeyMatch m => m.rules.Phase == HockeyPhase.Finished, AirHockeyMatch m => m.rules.Phase == AirHockeyPhase.Finished,
          WallballMatch m => m.rules.Phase == WallballPhase.Finished, BilliardsSession m => m.series.Finished, _ => false });
        public bool Live => Running && !Finished && (Owner switch
        { BowlingMatch m => !m.Paused, CornholeMatch m => !m.Paused, CheeseMatch m => m.Live, HockeyMatch m => !m.MenuOpen,
          AirHockeyMatch m => !m.MenuOpen, WallballMatch m => !m.MenuOpen, BilliardsSession m => m.GameplayEnabled, _ => false });
        public string MatchId => Owner switch
        { BowlingMatch m => m.MatchId, CornholeMatch m => m.MatchId, CheeseMatch m => m.MatchId, HockeyMatch m => m.MatchId,
          AirHockeyMatch m => m.MatchId, WallballMatch m => m.MatchId, BilliardsSession m => m.MatchId, _ => null };
        public string PreferenceKey => Owner switch
        { BowlingMatch _ => BowlingMatch.SetupKey, CornholeMatch _ => CornholeMatch.SetupKey, CheeseMatch _ => CheeseMatch.SetupKey,
          HockeyMatch _ => HockeyMatch.SetupKey, AirHockeyMatch _ => AirHockeyMatch.SetupKey, WallballMatch _ => WallballMatch.SetupKey, _ => BilliardsSession.SetupKey };
        public string Variant => Owner switch
        { BowlingMatch m => m.setup.mode.ToString(), HockeyMatch m => m.durationMinutes + " MINUTES", BilliardsSession m => m.IsPractice ? "PRACTICE" : m.format.ToString(), _ => "STANDARD MATCH" };
        public string Configuration => SportsPrefs.GetString(PreferenceKey);
        public Gamepad Pad(int slot) => Players[slot] switch { BowlingPlayer p => p.pad, HockeyPlayer p => p.pad, BilliardsPlayerSlot p => p.pad, _ => null };
        public string Source(int slot) => Players[slot] switch { BowlingPlayer p => p.source.ToString(), HockeyPlayer p => p.source.ToString(), BilliardsPlayerSlot p => p.control.ToString(), _ => "" };
        public string Difficulty(int slot) => Players[slot] switch { BowlingPlayer p => p.difficulty.ToString(), HockeyPlayer p => p.difficulty.ToString(), BilliardsPlayerSlot p => p.difficulty.ToString(), _ => "" };
        public void EnsureHumanCapacity(int humans)
        {
            switch (Owner)
            {
                case BowlingMatch m: m.setup.count = Mathf.Clamp(Math.Max(m.setup.count, humans), 1, Maximum); m.setup.sharedHotSeat = false; break;
                case CornholeMatch m: m.setup.count = Mathf.Clamp(Math.Max(m.setup.count, humans), 1, 4); break;
                case CheeseMatch m: m.setup.count = Mathf.Clamp(Math.Max(m.setup.count, humans), 1, 4); break;
                case HockeyMatch m: m.mode = HockeyMode.Custom; break;
                case AirHockeyMatch m: m.mode = AirHockeyMode.Custom; break;
                case WallballMatch m: m.mode = WallballMode.Custom; m.blueSize = Mathf.Clamp(Math.Max(m.blueSize, (humans + 1) / 2), 1, 2); m.orangeSize = Mathf.Clamp(Math.Max(m.orangeSize, humans / 2), 1, 2); break;
                case BilliardsSession m: if (!m.IsPractice) m.mode = BilliardsMode.Custom; m.hotSeatGamepad = m.IsPractice; break;
            }
        }
        public void Assign(int slot, Gamepad pad, bool keyboard, bool ai)
        {
            switch (Players[slot])
            {
                case BowlingPlayer p: p.pad = pad; p.source = ai ? BowlingSource.AI : keyboard ? BowlingSource.Keyboard : BowlingSource.WholeController; break;
                case HockeyPlayer p: p.pad = pad; p.source = ai ? HockeySource.AI : keyboard ? HockeySource.Keyboard : HockeySource.WholeController; break;
                case BilliardsPlayerSlot p: p.pad = pad; p.control = ai ? BilliardsControl.AI : keyboard ? BilliardsControl.KeyboardWASD : BilliardsControl.Gamepad;
                    if (Owner is BilliardsSession m && m.IsPractice) m.hotSeatGamepad = !keyboard; break;
            }
        }
        public void Refresh()
        {
            switch (Owner) { case BowlingMatch m: m.RefreshParticipants(); break; case CornholeMatch m: m.RefreshParticipants(); break; case CheeseMatch m: m.RefreshParticipants(); break;
                case HockeyMatch m: m.RefreshParticipants(); break; case AirHockeyMatch m: m.RefreshParticipants(); break; case WallballMatch m: m.RefreshParticipants(); break; case BilliardsSession m: m.RefreshParticipants(); break; }
        }
        public void Save()
        {
            switch (Owner) { case BowlingMatch m: m.Save(); break; case CornholeMatch m: m.Save(); break; case CheeseMatch m: m.Save(); break;
                case HockeyMatch m: m.SaveSetup(); break; case AirHockeyMatch m: m.Save(); break; case WallballMatch m: m.Save(); break; case BilliardsSession m: m.SaveSetup(); break; }
        }
        public void Gate()
        {
            switch (Owner) { case BowlingMatch m: m.Gate(); break; case CornholeMatch m: m.Gate(); break; case CheeseMatch m: m.Gate(); break;
                case HockeyMatch m: m.Gate(); break; case AirHockeyMatch m: m.Gate(); break; case WallballMatch m: m.Gate(); break; case BilliardsSession m: m.input.GateAll(); break; }
        }
        public void Setup()
        {
            switch (Owner) { case BowlingMatch m: m.ChangeSetup(); break; case CornholeMatch m: m.ChangeSetup(); break; case CheeseMatch m: m.ChangeSetup(); break;
                case HockeyMatch m: m.ChangeSetup(); break; case AirHockeyMatch m: m.ChangeSetup(); break; case WallballMatch m: m.ChangeSetup(); break; case BilliardsSession m: m.ChangeSetup(); break; }
        }
        public void Focus()
        {
            if (Owner is BilliardsSession billiards) billiards.menus.Show(billiards.Screen);
            else MenuRoot.GetComponent<SportsMenuView>()?.Focus();
        }
        public bool TryResults(out SportsResultsSnapshot result) => SportsLegacyFinalResults.TryCapture(Owner, out result);
    }
}
