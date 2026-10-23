using System;
using System.Text;
using UnityEngine;
using TMPro;
using LWS.TruckSports;
using LWS.TruckBowling;
namespace LWS.TruckWhackAMole
{
    public sealed class WhackMatch : SportsEventSession
    {
 public override void DescribeBroadcast(SportsBroadcastAction a,SportsBroadcastFraming f){int slot=f.Nearest(f.RequestedFocus,true);if(slot<0)slot=f.Nearest(f.RequestedFocus);a.primary=slot;if(slot<0)return;int best=-1;float distance=float.PositiveInfinity;for(int i=0;i<moles.Length;i++)if(rules.Phase(i)==MolePhase.Exposed||rules.Phase(i)==MolePhase.Emerging){float d=(moles[i].transform.position-trucks[slot].body.position).sqrMagnitude;if(d<distance){distance=d;best=i;}}if(best>=0)a.Objective(moles[best].transform.position,1);a.reason="EXPOSED MOLE / LOCAL BUMPER PLAY";}
        public SportsEventTruck[] trucks;
        public SportsFragmentRig[] fragments;
        public WhackMole[] moles;
        public ParticleSystem[] dirt, decoyBlasts;
        public Transform moleField;
        public SportsDrivingViews drivingViews;
        public SportsEventCamera broadcast;
        public SportsCameraFit framing;
        public SportsChaosWeather atmosphere;
        public SportsRecordedReview review;
        public LWS.TruckFieldGoal.FieldGoalCrowd crowd;
        public TMP_Text stadiumScores;
        public WhackRules rules = new WhackRules();
        public float Countdown { get; private set; }
        public int PhysicalHits { get; private set; }
        public int DecoyExplosions { get; private set; }
        public override string Title => "TRUCK WHACK-A-MOLE";
        public override int MinimumPlayers => 1;
        public override int MaximumPlayers => 8;
        public override int OptionCount => 3;
        public override int WeatherCount => 1;
        public override string OptionLabel => new[] { "2 MINUTES", "3 MINUTES", "4 MINUTES" }[option];
        public override int CompactScoreColumns => 4;
        public override bool SupportsSplitAIViews => true;
        public override bool SupportsRaceCountdown => true;
        public override float RaceCountdownSeconds => Live && Countdown > 0 ? Countdown : -1;
        public override float RaceStartSeconds => Countdown <= 0 ? rules.Clock : -1;
        public override bool ReplayControls => review != null && review.Playing;
        public override bool MultiViewHUD => !ReplayControls && drivingViews != null && !drivingViews.NeedsArena;
        public override bool BroadcastStandingsVisible => !ReplayControls;
        public override bool UsesAction => false;
        public override string Header => Title + " · " + Mathf.CeilToInt(Mathf.Max(0, rules.Duration - rules.Clock)) + "s · " + PhysicalHits + " WHACKS";
        public override string Status => Countdown > 0 ? "WATCH THE HOLES!" : "BUMPER CONTACT: +10 NORMAL · +50 GOLDEN STAR · −25 BLACK/RED DECOY";
        public override string Help => "Race around36fairground holes and physically bumper-whack giant animated moles before they dive underground. Ordinary brown moles score10, rare golden star moles score50. Black helmet/red-cross decoys deduct25 and blast your real tractor into a bounded cartoon tumble. Your front bumper must touch an exposed target at0.8m/s or faster. Inactive holes, stationary overlap and repeated contact never score. Every emergence has a new one-hit identity. The number of active moles increases and exposed windows shrink over the match. Highest score wins; equal scores share placing and negative scores are possible. Choose2,3or4minutes, up to4human drivers and8competitors, Easy/Normal/Hard AI or AI-only spectating. AI physically drives to emerged targets and can make difficulty-dependent decoy mistakes. Safe recovery never awards points. Shared arena, independent chase/cab, free look, optional AI views, crew identity, shared weather and horn use collection controls. Watch the actual recorded match at results without changing scores.";
        readonly StringBuilder text = new StringBuilder(800);
        readonly float[] stuck = new float[8], reverseUntil = new float[8], recoverAfter = new float[8], nextDecision = new float[8];
        readonly int[] goals = new int[8];
        public override bool ValidateSources(out string reason)
        {
            if (!base.ValidateSources(out reason)) return false;
            int humans = 0; for (int i = 0; i < setup.count; i++) if (setup.players[i].source != BowlingSource.AI) humans++;
            if (humans <= 4) return true; reason = "Up to four human drivers; assign remaining competitors to AI."; return false;
        }
        public override bool TryReadScoreCard(int slot, out string value, out string detail)
        { value = rules.scores[slot] + "P"; detail = rules.normalHits[slot] + " NORMAL · " + rules.goldenHits[slot] + " GOLD · " + rules.decoyHits[slot] + " DECOYS"; return slot < setup.count; }
        public override string ViewText(int view) { int slot = drivingViews.rally.owners[view]; TryReadScoreCard(slot, out var score, out var detail); return setup.players[slot].name + " · " + score + "\n" + detail + "\n" + Mathf.CeilToInt(Mathf.Max(0, rules.Duration - rules.Clock)) + "s REMAINING"; }
        public override string Standings { get { text.Clear(); for (int i = 0; i < setup.count; i++) text.Append('#').Append(rules.Place(i)).Append(' ').Append(setup.players[i].name).Append(" · ").Append(rules.scores[i]).Append("PTS · ").Append(rules.normalHits[i]).Append(" NORMAL / ").Append(rules.goldenHits[i]).Append(" GOLD / ").Append(rules.decoyHits[i]).Append(" DECOYS\n"); return text.ToString(); } }
        public override string Results => "FAIRGROUND RESULTS · EQUAL SCORES SHARE PLACING\n\n" + Standings;
        public Vector3 Grid(int slot) { float a = slot * Mathf.PI * 2 / Mathf.Max(1, setup.count); return new Vector3(Mathf.Sin(a) * 75, .65f, Mathf.Cos(a) * 75); }
        protected override void BeginGame()
        {
            if (review == null) { review = gameObject.AddComponent<SportsRecordedReview>(); review.match = this; review.trucks = trucks; review.fragments = fragments; review.drivingViews = drivingViews; review.broadcast = broadcast; review.framing = framing; review.atmosphere = atmosphere; review.watchAction = "watchwhack"; review.extras = new[] { moleField }; }
            review.ResetRecord(); rules.Reset(setup.count, moles.Length, Environment.TickCount, new[] { 120f, 180f, 240f }[option]); Countdown = 3; PhysicalHits = DecoyExplosions = 0; crowd?.ResetAttempt();
            foreach (var mole in moles) mole.ResetActor();
            foreach (var effect in dirt) effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            foreach (var effect in decoyBlasts) effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            for (int i = 0; i < trucks.Length; i++)
            {
                fragments[i].ResetBody(); trucks[i].gameObject.SetActive(i < setup.count); goals[i] = -1; stuck[i] = reverseUntil[i] = recoverAfter[i] = nextDecision[i] = 0;
                if (i >= setup.count) continue;
                trucks[i].tractorMotor.Configure(); trucks[i].Prepare(Grid(i), Quaternion.LookRotation(-Vector3.ProjectOnPlane(Grid(i), Vector3.up))); Hold(trucks[i]);
            }
            atmosphere.Configure(Environment.TickCount, 0, 1); drivingViews.Layout();
        }
        static void Hold(SportsEventTruck truck) { var input = truck.tractorMotor.vehicle.input; input.states.throttle = input.states.throttleRaw = input.states.brakes = input.states.brakesRaw = 0; input.states.handbrake = input.states.handbrakeRaw = 1; }
        protected override void Simulate(float dt)
        {
            atmosphere.Tick(rules.Clock);
            if (Countdown > 0) { Countdown = Mathf.Max(0, Countdown - dt); for (int i = 0; i < setup.count; i++) Hold(trucks[i]); return; }
            rules.Step(dt); foreach (var mole in moles) mole.Tick(); review.Capture(rules.Clock + 3);
            if (rules.Complete) { review.focusSlot = Leader(); EndMatch(); return; }
            for (int i = 0; i < setup.count; i++)
            {
                var truck = trucks[i]; truck.tractorMotor.gripScale = atmosphere.Grip;
                truck.TickDrive(setup.players[i].source == BowlingSource.AI ? Bot(i, dt) : commands[i]);
                if (truck.body.position.y < -5 || Vector3.Dot(truck.transform.up, Vector3.up) < .25f || stuck[i] > 9) Recover(i);
            }
        }
        int Leader() { int slot = 0; for (int i = 1; i < setup.count; i++) if (rules.scores[i] > rules.scores[slot]) slot = i; return slot; }
        internal void Contact(int slot, int hole, float speed)
        {
            if (!Live || Countdown > 0 || slot < 0 || slot >= setup.count || hole < 0 || hole >= rules.generations.Length) return;
            int previousScore = rules.scores[slot];
            if (!rules.Hit(slot, hole, rules.generations[hole], speed)) return;
            var kind = rules.kinds[hole];
            ConfirmScore("mole:" + hole + ":" + rules.generations[hole], slot, rules.scores[slot] - previousScore, rules.scores[slot], kind == MoleKind.Decoy ? "DECOY PENALTY" : kind == MoleKind.Golden ? "GOLDEN MOLE" : "MOLE HIT", kind == MoleKind.Golden ? SportsScoreTier.Major : SportsScoreTier.Small);
            PhysicalHits++; moles[hole].Tick(); var position = moles[hole].transform.position + Vector3.up * 1.5f;
            var effect = dirt[hole]; effect.transform.position = position; effect.Play(); feedback.PlayAt(5, position, 0, true); Pulse(slot, .5f, .2f);
            if (rules.kinds[hole] == MoleKind.Decoy)
            {
                DecoyExplosions++; var blast = decoyBlasts[slot]; blast.transform.position = position; blast.Play(); feedback.PlayAt(3, position, 12, true);
                var away = Vector3.ProjectOnPlane(trucks[slot].body.position - position, Vector3.up).normalized; trucks[slot].tractorMotor.AddAssemblyVelocity(away * 5 + Vector3.up * 8); Pulse(slot, .9f, .35f);
            }
            else if (rules.kinds[hole] == MoleKind.Golden) crowd?.React(true, "GOLDEN MOLE", position);
        }
        Vector2 Bot(int slot, float dt)
        {
            var truck = trucks[slot]; var p = truck.body.position; int skill = (int)setup.players[slot].difficulty;
            if (rules.Clock >= nextDecision[slot] || goals[slot] < 0 || rules.Phase(goals[slot]) == MolePhase.Underground || rules.Phase(goals[slot]) == MolePhase.Whacked)
            {
                goals[slot] = -1; float best = float.MaxValue;
                for (int i = 0; i < moles.Length; i++)
                {
                    var phase = rules.Phase(i); if (phase != MolePhase.Exposed && phase != MolePhase.Emerging) continue;
                    if (rules.kinds[i] == MoleKind.Decoy && (skill > 0 || (slot + rules.generations[i]) % 3 != 0)) continue;
                    float utility = (moles[i].transform.position - p).sqrMagnitude / (rules.kinds[i] == MoleKind.Golden ? 2.3f : 1);
                    if (utility < best) { best = utility; goals[slot] = i; }
                }
                nextDecision[slot] = rules.Clock + .5f - skill * .15f;
            }
            var goal = goals[slot] >= 0 ? moles[goals[slot]].transform.position : new Vector3(Mathf.Sin(rules.Clock * .13f + slot) * 38, 0, Mathf.Cos(rules.Clock * .13f + slot) * 38);
            goal.y = p.y;
            if (Vector3.ProjectOnPlane(p, Vector3.up).magnitude > 78) goal = Vector3.zero;
            stuck[slot] = truck.body.linearVelocity.magnitude < .7f ? stuck[slot] + dt : 0;
            if (stuck[slot] > 1.5f && reverseUntil[slot] < rules.Clock) reverseUntil[slot] = rules.Clock + 1.7f;
            bool reverse = rules.Clock < reverseUntil[slot]; float angle = Vector3.SignedAngle(Vector3.ProjectOnPlane(reverse ? -truck.transform.forward : truck.transform.forward, Vector3.up), goal - p, Vector3.up);
            float speed = Mathf.Abs(angle) > 70 ? 4 : Mathf.Abs(angle) > 35 ? 8 : 13 + skill * 2;
            return new Vector2(Mathf.Clamp(angle / 32, -1, 1) * (reverse ? -1 : 1), reverse ? -.8f : truck.body.linearVelocity.magnitude < speed ? .9f : 0);
        }
        void Recover(int slot)
        {
            if (rules.Clock < recoverAfter[slot]) return;
            for (int k = 0; k < 8; k++) { var p = Grid((slot + k) % setup.count); bool free = true; for (int i = 0; i < setup.count; i++) if (i != slot && Vector3.Distance(trucks[i].body.position, p) < 12) free = false; if (!free) continue; trucks[slot].Prepare(p, Quaternion.LookRotation(-Vector3.ProjectOnPlane(p, Vector3.up))); recoverAfter[slot] = rules.Clock + 6; stuck[slot] = 0; return; }
        }
        protected override void Present(float dt)
        {
            if (drivingViews == null) return; if (ReplayControls) { review.Tick(); return; } drivingViews.Tick(dt);
            if (drivingViews.NeedsArena) { var area = new Bounds(new Vector3(0, 4, 0), new Vector3(170, 16, 170)); area.Encapsulate(new Vector3(0, 36, 102)); if(broadcast.LegacyPoseAllowed)broadcast.view.orthographic=true; broadcast.Pose(area.center, new Vector3(0, 100, -150), 60); if(broadcast.LegacyPoseAllowed)broadcast.transform.rotation=Quaternion.LookRotation(new Vector3(0, -100, 150), Vector3.up); framing.arenaBounds = area; framing.Apply(); }
            if (stadiumScores != null && Running) stadiumScores.text = Header + "\n" + Standings;
        }
        public override void ExtraAction(string key) { if (key == "drivingview") drivingViews.Cycle(); else review?.Action(key); }
        protected override void SetPaused(bool pause) => atmosphere?.Pause(pause);
        protected override void StopSounds() { if (trucks != null) foreach (var truck in trucks) truck.StopSound(); }
        protected override void StopGame()
        {
            review?.End(false); StopSounds(); if (trucks == null) return;
            for (int i = 0; i < trucks.Length; i++) { trucks[i].Power(false); trucks[i].body.isKinematic = true; fragments[i].Freeze(); }
            if (moles != null) foreach (var mole in moles) { mole.target.enabled = false; foreach (var animator in mole.animators) animator.enabled = false; }
        }
    }
}
