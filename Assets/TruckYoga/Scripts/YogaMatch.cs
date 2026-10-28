using System.Text;
using UnityEngine;
using LWS.TruckBowling;
using LWS.TruckSports;

namespace LWS.TruckYoga
{
    public sealed class YogaMatch : SportsEventSession
    {
        public YogaTruckRig[] rigs;
        public YogaInstructor instructor;
        public SportsEventCamera broadcast;
        public GameObject freeNext, freeFinish, pauseFreeFinish;
        public AudioSource ambience;
        public readonly YogaRules rules = new YogaRules();
        readonly float[] announced = new float[4];
        public float ArrivalSeconds { get; private set; }
        public override string Title => "TRUCK YOGA";
        public override int MinimumPlayers => 1;
        public override int MaximumPlayers => 4;
        public override int OptionCount => 2;
        public override string OptionLabel => option == 0 ? "SEVEN-POSE CHALLENGE" : "RELAXING FREE YOGA";
        public override bool CompactBroadcastStatus => true;
        public override bool BroadcastStandingsVisible => false;
        public override bool BroadcastParticipantActive(int slot) => slot >= 0 && slot < setup.count && rigs != null && slot < rigs.Length && rigs[slot].gameObject.activeInHierarchy;
        public override void DescribeBroadcast(SportsBroadcastAction action, SportsBroadcastFraming framing)
        {
            action.primary = 0; action.preferHuman = false; action.reason = "SHARED MEADOW POSE CLASS";
            if (instructor != null) action.Objective(instructor.animator.transform.position + Vector3.up, 1.5f);
        }
        public override string Help => "BREATHE. BALANCE. BE A TRUCK.\n\nFollow seven poses. PITCH/ROLL uses forward/back for pitch and left/right for lean. Tap ACTION to change banks. LIFT/TWIST uses forward/back to raise/lower and left/right to twist.\n\nFor these truck bodies, CAB TWIST turns the complete truck and its wheels together. Your saved customization stays selected.\n\nSettle accurately and hold 3 seconds. Accuracy, stability and actual hold duration earn points; movement lowers stability. Each challenge pose has a 22-second attempt window. Highest total wins. AI uses the same controls.\n\nFREE YOGA has no score, winner or time limit. Use NEXT POSE to explore and FINISH CLASS when ready, also available in Pause. Pause/help never advances a pose.";
        public override string Header => "MOUNTAIN MEADOW · " + YogaPoses.Names[(int)rules.Pose];
        public override string Status => ArrivalSeconds > 0 ? "FIND YOUR BALANCE · " + Mathf.CeilToInt(ArrivalSeconds) : rules.Free ? "FREE YOGA · YOUR OWN PACE" : $"POSE {(int)rules.Pose + 1} / 7 · {Mathf.Max(0, YogaRules.PoseLimit - rules.PoseSeconds):0}s";
        public override string Standings { get { var b = new StringBuilder(); for (int i = 0; i < setup.count; i++) b.AppendLine($"{setup.players[i].name} · {rules.score[i]:0} · {rules.accuracy[i] * 100:0}% · {rules.hold[i]:0.0}s"); return b.ToString(); } }
        public override string Results { get { var b = new StringBuilder(rules.Free ? "A MOMENT OF STILLNESS\nCLASS COMPLETE · NO WINNER OR POINTS\n\n" : "SEVEN-POSE CHALLENGE\n\n"); for (int i = 0; i < setup.count; i++) b.AppendLine(rules.Free ? $"{setup.players[i].name} · {rules.completed[i]} HELD POSES" : $"#{rules.Place(i)} {setup.players[i].name} · {rules.score[i]:0} PTS · {rules.completed[i]} / 7 HELD"); return b.ToString(); } }
        public override bool TryReadScoreCard(int card, out string value, out string detail)
        {
            value = rules.Free ? $"{rules.accuracy[card] * 100:0}% ALIGNMENT" : $"{rules.score[card]:0} PTS";
            detail = $"{rules.accuracy[card] * 100:0}% · HOLD {Mathf.Min(3, rules.hold[card]):0.0}/3s · {rigs[card].BankLabel}"; return true;
        }
        protected override void BeginGame()
        {
            rules.Begin(setup.count, option == 1); ArrivalSeconds = 3;
            System.Array.Clear(announced, 0, announced.Length);
            for (int i = 0; i < rigs.Length; i++) rigs[i].Begin(i < setup.count); Gate();
        }
        protected override void Simulate(float dt)
        {
            if (ArrivalSeconds > 0) { ArrivalSeconds -= dt; return; }
            for (int i = 0; i < setup.count; i++)
            {
                bool bot = setup.players[i].source == BowlingSource.AI;
                var axes = bot ? rigs[i].AI(rules.Pose) : commands[i];
                rigs[i].Step(axes, !bot && actions[i], dt);
                bool held = rules.Observe(i, rigs[i].Achieved, rigs[i].Previous, dt);
                if (held) feedback.Play(2, i, true);
                // Continuous measurements are aggregated at each pose boundary;
                // no feedback queue flood and no points in free yoga.
            }
            var oldPose = rules.Pose;
            if (rules.Tick(dt))
            {
                if (!rules.Free) for (int i = 0; i < setup.count; i++)
                {
                    float delta = rules.score[i] - announced[i];
                    if (delta > .001f) ConfirmScore("pose-points-" + (int)oldPose + "-" + i, i, delta, rules.score[i], YogaPoses.Names[(int)oldPose] + " · ACCURACY & HOLD", SportsScoreTier.Major, roundEnded: true);
                    announced[i] = rules.score[i];
                    ConfirmOutcome("pose-" + (int)oldPose + "-" + i, i, rules.completed[i], YogaPoses.Names[(int)oldPose], "POSES", roundEnded: true);
                }
                if (rules.Finished)
                {
                    for (int i = 0; i < setup.count; i++) if (rules.Place(i) == 1) ConfirmOutcome("yoga-champion-" + i, i, rules.score[i], "MEADOW POSE CHAMPION", "PTS", SportsScoreTier.MatchChampion, matchEnded: true);
                    EndMatch();
                }
                else Gate();
            }
        }
        public override void ExtraAction(string key)
        {
            if (!Running || Finished || !rules.Free) return;
            if (key == "yoga-next" && Live && menus.Screen == SportsEventScreen.Play) { rules.NextPose(); Gate(); }
            else if (key == "yoga-finish") { if (Paused) Resume(); EndMatch(); }
        }
        protected override void Present(float dt)
        {
            if (freeNext != null) freeNext.SetActive(Live && rules.Free);
            if (freeFinish != null) freeFinish.SetActive(Live && rules.Free);
            if (pauseFreeFinish != null) pauseFreeFinish.SetActive(Running && rules.Free);
            if (broadcast != null) broadcast.Pose(new Vector3(0, 2, 2), new Vector3(0, 24, -31), 57);
            if (ambience != null)
            {
                bool play = Live && Application.isFocused; ambience.volume = play ? settings.sfx * .13f : 0;
                if (play && !ambience.isPlaying) ambience.Play(); else if (!play) ambience.Stop();
            }
        }
        protected override void StopGame() { if (rigs != null) foreach (var rig in rigs) if (rig != null) rig.Stop(); }
        protected override void StopSounds() { if (ambience != null) ambience.Stop(); }
    }
}
