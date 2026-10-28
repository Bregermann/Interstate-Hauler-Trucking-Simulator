using System.Text;
using UnityEngine;
using LWS.TruckBowling;
using LWS.TruckSports;
namespace LWS.TruckEggSpoonRace
{
    public sealed class EggSpoonMatch : SportsEventSession
    {
        public EggSpoonRacer[] racers;
        public SportsEventCamera broadcast;
        public SportsDrivingViews drivingViews;
        public readonly EggSpoonRules rules = new EggSpoonRules();
        public float Clock { get; private set; }
        public float Countdown { get; private set; }
        public bool Racing => Countdown <= 0 && Running && !Finished;
        public override string Title => "TRUCK EGG & SPOON RACE";
        public override int MinimumPlayers => 1;
        public override int MaximumPlayers => 4;
        public override int OptionCount => 3;
        public override bool SupportsRaceCountdown => true;
        public override float RaceCountdownSeconds => Countdown > 0 ? Countdown : -1;
        public override float RaceStartSeconds => Racing ? Clock : -1;
        public override bool SupportsSplitAIViews => true;
        public override string OptionLabel => new[] { "TIMED COUNTRY RACE", "BEST CLEAN RUN", "FEWEST BROKEN EGGS" }[option];
        public override string Help => "GENTLE HANDS. GIANT EGG.\n\nDrive using your normal controls. The egg rolls physically in the spoon: acceleration, braking, steering, bumps and impacts matter. HOLD ACTION to brake and trim the spoon with the same axes: forward/back pitches, left/right rolls. Release to drive.\n\nCarry your egg through all three marked checkpoints and across the finish. A spill/break adds 8 seconds. Physically return to your last secured checkpoint (or START), stop, and HOLD ACTION for 3 seconds to replace it. No replacement away from a station; no checkpoint/finish credit without an intact egg.\n\nTIMED ranks adjusted time; CLEAN prioritizes a clean finish; FEWEST prioritizes fewer lost/broken eggs. Everyone has the same physical constraints. The race ends after 180 seconds; stranded/capsized trucks may DNF. Pause/help preserve physics. Use chase view to see the spoon.";
        public override string Header => Countdown > 0 ? "COUNTRY EGG RUN · READY " + Mathf.CeilToInt(Countdown) : "COUNTRY EGG RUN · " + Clock.ToString("0.0") + " SEC";
        public override string Status => Countdown > 0 ? "BALANCE YOUR BREAKFAST" : "3 CHECKPOINTS · " + Mathf.Max(0, EggSpoonRules.RaceLimit - Clock).ToString("0") + " SEC LEFT";
        public override bool CompactBroadcastStatus => true;
        public override bool MultiViewHUD => drivingViews != null && !drivingViews.NeedsArena;
        public override string Standings { get { var b = new StringBuilder(); for (int i = 0; i < setup.count; i++) b.AppendLine(setup.players[i].name + " · " + Caption(i)); return b.ToString(); } }
        string Caption(int i) => rules.finished[i] ? rules.time[i].ToString("0.0") + "s · " + rules.spills[i] + " SPILLS" : rules.dnf[i] ? "DNF" : rules.carrying[i] ? rules.checkpoint[i] + "/3 · EGG SAFE" : "RETURN TO " + (rules.checkpoint[i] == 0 ? "START" : "CHECKPOINT " + rules.checkpoint[i]);
        public override string Results { get { var b = new StringBuilder(OptionLabel + "\n\n"); for (int i = 0; i < setup.count; i++) b.AppendLine($"#{rules.Place(i)} {setup.players[i].name} · {Caption(i)}" + (rules.finished[i] && rules.spills[i] == 0 ? " · CLEAN RUN" : "")); return b.ToString(); } }
        public override string ViewText(int view) { int i = drivingViews.rally.owners[view]; return setup.players[i].name + " · " + Clock.ToString("0.0") + "s\n" + Caption(i) + "\n" + (racers[i].ServiceSeconds > 0 ? "REPLACING " + racers[i].ServiceSeconds.ToString("0.0") + "/3s" : "HOLD ACTION · SPOON TRIM"); }
        public override bool TryReadScoreCard(int card, out string value, out string detail) { value = rules.finished[card] ? rules.time[card].ToString("0.0") + " SEC" : rules.spills[card] + " SPILLS"; detail = Caption(card); return true; }
        public override bool BroadcastParticipantActive(int slot) => slot >= 0 && slot < setup.count && !rules.finished[slot] && !rules.dnf[slot];
        int Leader() { int leader = 0; for (int i = 1; i < setup.count; i++) if (BroadcastParticipantActive(i) && (!BroadcastParticipantActive(leader) || racers[i].truck.body.position.z > racers[leader].truck.body.position.z)) leader = i; return leader; }
        public override void DescribeBroadcast(SportsBroadcastAction action, SportsBroadcastFraming framing)
        {
            int leader = Leader(); action.primary = leader; action.preferHuman = false; action.reason = rules.carrying[leader] ? "LIVE EGG BALANCE" : "RETURN FOR A FRESH EGG";
            action.Objective(rules.carrying[leader] ? racers[leader].egg.body.position : EggSpoonCourse.Point(leader, EggSpoonCourse.StationZ(rules.checkpoint[leader])), 1.7f);
        }
        protected override void BeginGame() { rules.Begin(setup.count, option); Clock = 0; Countdown = 3; for (int i = 0; i < racers.Length; i++) racers[i].Prepare(i < setup.count); drivingViews.Layout(); Gate(); }
        protected override void Simulate(float dt)
        {
            if (Countdown > 0) { Countdown -= dt; if (Countdown <= 0) for (int i = 0; i < setup.count; i++) racers[i].Release(); return; }
            Clock += dt;
            for (int i = 0; i < setup.count; i++)
            {
                bool trim; Vector2 axes;
                if (setup.players[i].source == BowlingSource.AI) axes = racers[i].AI(out trim); else { axes = commands[i]; trim = setup.players[i].released && ActionHeld(i); }
                racers[i].Step(axes, trim, dt);
                if (Clock >= EggSpoonRules.RaceLimit && !rules.finished[i]) { rules.Fail(i); racers[i].Stop(); }
            }
            if (rules.Complete)
            {
                for (int i = 0; i < setup.count; i++) if (rules.finished[i] && rules.Place(i) == 1) ConfirmOutcome("egg-champion-" + i, i, rules.time[i], "COUNTRY EGG CHAMPION", "SEC", SportsScoreTier.MatchChampion, matchEnded: true);
                EndMatch();
            }
        }
        public void Spilled(int slot) { ConfirmOutcome("spill-" + slot + "-" + rules.spills[slot], slot, 8, "EGG SPILLED · RETURN TO CHECKPOINT", "PENALTY SEC"); feedback.Play(3, slot, true); }
        public void Replaced(int slot) { ConfirmOutcome("replace-" + slot + "-" + rules.replacements[slot], slot, rules.replacements[slot], "FRESH EGG AT CHECKPOINT", "EGGS"); }
        public void Secured(int slot, int gate) { ConfirmOutcome("gate-" + slot + "-" + gate, slot, gate, "EGG CHECKPOINT SECURED", "GATES"); }
        public void Arrived(int slot) { ConfirmOutcome("finish-" + slot, slot, rules.time[slot], "EGG DELIVERED · ADJUSTED TIME", "SEC", SportsScoreTier.RoundWinner, roundEnded: true); feedback.Play(5, slot, true); }
        public override void ExtraAction(string key) { if (key == "drivingview") drivingViews.Cycle(); }
        protected override void Present(float dt) { if (drivingViews == null) return; drivingViews.Tick(dt); if (drivingViews.NeedsArena && racers != null) { int i = Leader(); broadcast.Pose(rules.carrying[i] ? racers[i].egg.body.position - Vector3.up * 2 : racers[i].truck.body.position + Vector3.forward * 4, new Vector3(16, 17, -28), 60); } }
        protected override void StopGame() { if (racers != null) foreach (var racer in racers) if (racer != null) racer.Stop(); }
    }
}
