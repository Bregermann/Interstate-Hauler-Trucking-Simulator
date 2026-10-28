using System.Text;
using UnityEngine;
using LWS.TruckBowling;
using LWS.TruckSports;

namespace LWS.TruckMechanicalBull
{
    public enum MechanicalBullPhase { Mounting, Riding, Landed }
    public sealed class MechanicalBullMatch : SportsEventSession
    {
        public MechanicalBullMachine machine;
        public MechanicalBullRider[] riders;
        public SportsEventCamera broadcast;
        public AudioSource machinery;
        public LWS.TruckFieldGoal.FieldGoalCrowd crowd;
        public readonly MechanicalBullRules rules = new MechanicalBullRules();
        public MechanicalBullPhase Phase { get; private set; }
        public float PhaseSeconds { get; private set; }
        public int CurrentOwner => rules.Owner;
        public override string Title => "TRUCK MECHANICAL BULL";
        public override int MinimumPlayers => 1;
        public override int MaximumPlayers => 4;
        public override int OptionCount => 3;
        public override void NormalizeSetup() { base.NormalizeSetup(); setup.mode = BowlingMode.HotSeat; setup.sharedHotSeat = true; }
        public override string SourcePolicy => "ALTERNATING RIDES · one to four named participants.\nA keyboard/controller may be shared between turns; only the current rider receives balance commands. AI rides the same physical machine.";
        public override bool CompactBroadcastStatus => true;
        public override bool BroadcastParticipantActive(int slot) => slot >= 0 && slot < setup.count && (!Running || slot == CurrentOwner);
        public override void DescribeBroadcast(SportsBroadcastAction action, SportsBroadcastFraming framing)
        {
            action.primary = CurrentOwner; action.preferHuman = false; action.reason = "ACTUAL RODEO RIDER · " + Phase;
            if (machine != null) action.Objective(machine.TopCenter, Mathf.Max(2, machine.deck.size.x * .4f));
        }
        public override string OptionLabel => new[] { "ONE RIDE EACH", "THREE RIDES EACH", "FIVE RIDES EACH" }[option];
        public override string Help => "SURVIVE THE IRON LONGHORN\n\nOne truck rides at a time. Your normal forward/back and left/right controls counterlean the chassis; ACTION braces angular motion. The machine pitches, rolls, yaws, bucks and reverses, growing stronger over 60 seconds.\n\nStay physically on the saddle. Earn 10 points per second, 25 for recovering from a genuine dangerous tilt and 15 for each 10-second pattern survived. A real fall ends the ride; do not steer off deliberately. Everyone receives the same number of rides. Highest total wins; best survival breaks a tie.\n\nAI uses the same physical balance forces. Pause/help preserve the ride. Release controls before resuming.";
        public override string Header => Running ? $"IRON LONGHORN · ROUND {rules.Round} · {setup.players[rules.Owner].name}" : "IRON LONGHORN RODEO";
        public override string Status => Phase == MechanicalBullPhase.Mounting ? $"MOUNT UP · {Mathf.CeilToInt(PhaseSeconds)}" : Phase == MechanicalBullPhase.Landed ? (rules.Fell ? "OFF THE SADDLE" : "FULL RIDE") + $" · {rules.RidePoints} PTS" : $"{rules.Survival:0.0} / 60 SEC · {rules.Recoveries} RECOVERIES · {machine.Intensity * 100:0}% BUCK";
        public override string Standings { get { var b = new StringBuilder(); for (int i = 0; i < setup.count; i++) b.AppendLine($"{setup.players[i].name} · {rules.totals[i]} PTS"); return b.ToString(); } }
        public override string Results { get { var b = new StringBuilder("IRON LONGHORN · FINAL RIDES\n\n"); for (int i = 0; i < setup.count; i++) b.AppendLine($"#{rules.Place(i)} {setup.players[i].name} · {rules.totals[i]} PTS · BEST {rules.best[i]:0.0}s"); return b.ToString(); } }
        public override bool TryReadScoreCard(int card, out string value, out string detail) { value = rules.totals[card].ToString(); detail = card == rules.Owner && !Finished ? "RIDING · " + rules.Survival.ToString("0.0") + " SEC" : "BEST " + rules.best[card].ToString("0.0") + " SEC"; return true; }
        protected override void BeginGame() { rules.Begin(setup.count, new[] { 1, 3, 5 }[option]); BeginRide(); }
        void BeginRide()
        {
            machine.ResetMachine(); for (int i = 0; i < riders.Length; i++) riders[i].Prepare(i == rules.Owner);
            Phase = MechanicalBullPhase.Mounting; PhaseSeconds = 3; Gate();
        }
        protected override void Simulate(float dt)
        {
            var rider = riders[rules.Owner]; var player = setup.players[rules.Owner];
            if (Phase == MechanicalBullPhase.Landed)
            {
                PhaseSeconds -= dt; if (PhaseSeconds > 0) return;
                rules.Next();
                if (rules.Complete)
                {
                    for (int i = 0; i < setup.count; i++) if (rules.Place(i) == 1) ConfirmOutcome("bull-champion-" + i, i, rules.totals[i], "IRON LONGHORN CHAMPION", "PTS", SportsScoreTier.MatchChampion, matchEnded: true);
                    EndMatch(); return;
                }
                BeginRide(); return;
            }
            Vector2 lean = player.source == BowlingSource.AI ? rider.BalanceAI(player.difficulty == BowlingDifficulty.Hard ? 1 : player.difficulty == BowlingDifficulty.Easy ? .65f : .82f) : commands[rules.Owner];
            bool brace = player.source == BowlingSource.AI ? Vector3.Dot(rider.transform.up, Vector3.up) < .8f : ActionHeld(rules.Owner);
            rider.Step(lean, brace, dt, Phase == MechanicalBullPhase.Riding);
            if (Phase == MechanicalBullPhase.Mounting) { PhaseSeconds -= dt; if (PhaseSeconds <= 0) { Phase = MechanicalBullPhase.Riding; Gate(); } return; }
            machine.Step(rules.Survival);
            if (!rules.Tick(dt, rider.DeckContact, rider.Fell, Vector3.Dot(rider.transform.up, Vector3.up))) return;
            ConfirmScore("ride-" + rules.Turn, rules.Owner, rules.RidePoints, rules.totals[rules.Owner], rules.Fell ? "PHYSICAL FALL" : "FULL SIXTY SECOND RIDE", SportsScoreTier.Major, roundEnded: true);
            ConfirmOutcome("survival-" + rules.Turn, rules.Owner, rules.Survival, "RODEO SURVIVAL", "SEC", roundEnded: true);
            rider.EndRide(rules.Fell); Phase = MechanicalBullPhase.Landed; PhaseSeconds = 3;
            if (crowd != null) crowd.React(!rules.Fell, rules.Fell ? "FALL" : "FULL RIDE", rider.truck.body.position);
            feedback.Play(rules.Fell ? 3 : 2, rules.Owner, true);
        }
        protected override void Present(float dt)
        {
            if (broadcast != null && machine != null) broadcast.Pose(machine.TopCenter + Vector3.up * 1.5f, new Vector3(20, 15, -27), 54);
            if (machinery != null)
            {
                bool play = Live && Phase == MechanicalBullPhase.Riding && Application.isFocused;
                machinery.volume = play ? settings.sfx * .15f : 0; machinery.pitch = .65f + machine.Intensity * .55f;
                if (play && !machinery.isPlaying) machinery.Play(); else if (!play) machinery.Stop();
            }
        }
        protected override void StopGame() { if (riders != null) foreach (var rider in riders) if (rider != null) rider.Stop(); if (machine != null) machine.ResetMachine(); }
        protected override void StopSounds() { if (machinery != null) machinery.Stop(); }
    }
}
