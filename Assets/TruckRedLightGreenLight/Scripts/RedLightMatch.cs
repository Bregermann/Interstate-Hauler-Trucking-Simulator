using System;
using System.Text;
using UnityEngine;
using TMPro;
using DG.Tweening;
using LWS.TruckSports;
using LWS.TruckBowling;

namespace LWS.TruckRedLightGreenLight
{
    public sealed class RedLightMatch : SportsEventSession
    {
        public SportsEventTruck[] trucks;
        public SportsFragmentRig[] fragments;
        public ParticleSystem[] explosions;
        public SportsDrivingViews drivingViews;
        public SportsEventCamera broadcast;
        public SportsCameraFit framing;
        public SportsRecordedReview review;
        public LWS.TruckFieldGoal.FieldGoalCrowd crowd;
        public SportsChaosWeather atmosphere;
        public Transform master;
        public Animator masterAnimator;
        public AudioSource laugh;
        public AudioClip[] laughs;
        public Renderer[] signals;
        public TMP_Text signalCaption;
        public float finishZ = 300, trackHalfWidth = 45, startY = .65f;
        public float explosionRadius = 20, explosionVelocity = 12;
        [Min(1000)] public float competitionBrakeTorque = 26000;
        public RedLightRules rules = new RedLightRules();
        public int Explosions { get; private set; }
        public int LegalFinishes { get; private set; }
        public int Seed { get; private set; }
        public override string Title => "TRUCK RED LIGHT, GREEN LIGHT";
        public override int MinimumPlayers => 1;
        public override int MaximumPlayers => 12;
        public override bool ValidateSources(out string reason)
        {
            if (!base.ValidateSources(out reason)) return false;
            int humans = 0; for (int i = 0; i < setup.count; i++) if (setup.players[i].source != BowlingSource.AI) humans++;
            if (humans <= 4) return true;
            reason = "Up to four local human drivers. Set additional competitors to AI."; return false;
        }
        public override int OptionCount => 1;
        public override int WeatherCount => 1;
        public override string OptionLabel => "LAUGH WARNING: ONE SECOND";
        public override bool UsesAction => true;
        public override string ControlCaption(int slot) => base.ControlCaption(slot).Replace(" ACTION", " H.BRAKE");
        public override bool SupportsSplitAIViews => true;
        public override int CompactScoreColumns => 6;
        public override bool SupportsRaceCountdown => true;
        public override float RaceCountdownSeconds => Live && rules.Phase == RedLightPhase.Countdown ? 3 - rules.PhaseTime : -1;
        public override float RaceStartSeconds => rules.Phase != RedLightPhase.Countdown ? Mathf.Max(0, rules.Elapsed - 3) : -1;
        public override bool MultiViewHUD => (review == null || !review.Playing) && drivingViews != null && !drivingViews.NeedsArena;
        public override bool ReplayControls => review != null && review.Playing;
        public override bool BroadcastStandingsVisible => !ReplayControls;
        public override string Help => "Race to the checkered line. GREEN means drive. The giant master's laugh is your ONE-SECOND warning to brake before RED: there is no yellow light. Deliberate forward/backward movement, creeping or continued rolling on red causes one huge truck-shattering explosion. Small suspension jitter and motion caused entirely by another truck's measured impact do not count. A collision gives no blanket immunity to throttle-driven travel. Your whole truck must clear the finish inside the track. First legal finisher wins; remaining racers have ten seconds to place. If nobody legally finishes, the results say so. Twelve competitors, up to four local humans, real AI braking and difficulty, shared/chase/cab views and optional AI cameras. Use the existing throttle/brake/steer bindings; reverse requires braking first. Horn remains independent. Camera/free-look settings and actual device prompts are in pause/help. Selected weather persists across individual eliminations. Rematch restores all trucks, debris, signals and countdown.";
        public override string Header => ReplayControls ? Title + " · RECORDED REPLAY" : Title + " · " + PhaseLabel + " · " + Mathf.Max(0, 183 - rules.Elapsed).ToString("F0") + "s";
        public override string Status => noticeTime > 0 ? notice : rules.Phase == RedLightPhase.Complete ? "RACE COMPLETE" : rules.Phase == RedLightPhase.LaughWarning ? "THE LAUGH! BRAKE NOW — RED IN ONE SECOND" : rules.Phase == RedLightPhase.Red ? "RED LIGHT · STOP" : "GREEN LIGHT · GET TO THE FINISH";
        string PhaseLabel => rules.Phase == RedLightPhase.Complete ? "RACE COMPLETE" : rules.Phase == RedLightPhase.Countdown ? Mathf.CeilToInt(3 - rules.PhaseTime).ToString() : rules.Phase == RedLightPhase.Red ? "RED LIGHT" : "GREEN LIGHT";
        public override string Standings { get { text.Clear(); for (int i = 0; i < setup.count; i++) text.Append(setup.players[i].name).Append(" · ").Append(State(i)).Append('\n'); return text.ToString(); } }
        public override string Results => (rules.Winner >= 0 ? setup.players[rules.Winner].name + " WINS!" : "NO LEGAL FINISHER") + "\n\n" + Standings + "\n" + Explosions + " ELIMINATIONS · " + LegalFinishes + " LEGAL FINISHES";
        public override bool ParticipantLocked(int slot) => rules.outcomes[slot] != RedLightOutcome.Racing;
        public override float ParticipantProgress(int slot) => Mathf.Clamp01(trucks[slot].body.position.z / finishZ);
        public override bool TryReadScoreCard(int slot, out string value, out string detail)
        { value = rules.outcomes[slot] == RedLightOutcome.Finished ? Points(slot) + "P" : Mathf.Max(0, finishZ - Rear(trucks[slot])).ToString("F0") + "m"; detail = State(slot); return slot < setup.count; }
        public override string ViewText(int view)
        { int slot = drivingViews.rally.owners[view]; if (ParticipantLocked(slot)) return setup.players[slot].name + " · " + State(slot) + "\nSPECTATING THE REMAINING RACE"; return setup.players[slot].name + " · " + State(slot) + "\n" + PhaseLabel + (rules.Phase == RedLightPhase.LaughWarning ? " · BRAKE!" : "") + "\n" + Mathf.Max(0, finishZ - Rear(trucks[slot])).ToString("F0") + "m TO FINISH"; }

        readonly Vector3[] previous = new Vector3[12];
        readonly float[] lastThrottle = new float[12], reactionAt = new float[12], previousRear = new float[12];
        MaterialPropertyBlock tint;
        readonly StringBuilder text = new StringBuilder(600);
        RedLightPhase presentedPhase = (RedLightPhase)(-1);
        string notice = ""; float noticeTime, completionAge, explosionViewTime; int serial;
        Bounds recentExplosions;
        Sequence masterShake;

        protected override void Awake() { base.Awake(); tint = new MaterialPropertyBlock(); }

        public Vector3 Grid(int slot) => new Vector3((slot % 6 - 2.5f) * 11, startY, -slot / 6 * 14);
        static float Rear(SportsEventTruck truck) => truck.shape.bounds.min.z;
        int Place(int slot) { int place = 1; for (int i = 0; i < setup.count; i++) if (rules.outcomes[i] == RedLightOutcome.Finished && rules.finishTimes[i] < rules.finishTimes[slot] - .0001f) place++; return place; }
        int Points(int slot) => rules.outcomes[slot] == RedLightOutcome.Finished ? Mathf.Max(10, 100 - (Place(slot) - 1) * 10) : 0;
        string State(int slot) => rules.outcomes[slot] == RedLightOutcome.Finished ? "#" + Place(slot) + " · " + Points(slot) + "PTS · " + rules.finishTimes[slot].ToString("F2") + "s" : rules.outcomes[slot] == RedLightOutcome.Eliminated ? "OUT · " + rules.reasons[slot] : rules.Phase == RedLightPhase.Complete ? "DID NOT FINISH" : "RACING";

        protected override void BeginGame()
        {
            StopSounds(); masterShake?.Kill(); master.localRotation = Quaternion.Euler(0, 180, 0);
            if (review == null) { review = gameObject.AddComponent<SportsRecordedReview>(); review.match = this; review.trucks = trucks; review.fragments = fragments; review.drivingViews = drivingViews; review.broadcast = broadcast; review.framing = framing; review.atmosphere = atmosphere; review.watchAction = "watchredlight"; }
            review.ResetRecord();
            crowd?.ResetAttempt();
            Seed = Environment.TickCount ^ ++serial * 7919; rules.Reset(setup.count, Seed);
            Explosions = LegalFinishes = 0; presentedPhase = (RedLightPhase)(-1); noticeTime = completionAge = explosionViewTime = 0;
            for (int i = 0; i < trucks.Length; i++)
            {
                explosions[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                fragments[i].ResetBody(); trucks[i].gameObject.SetActive(i < setup.count);
                if (i >= setup.count) continue;
                trucks[i].tractorMotor.Configure();
                trucks[i].tractorMotor.vehicle.brakes.maxTorque = competitionBrakeTorque;
                trucks[i].Prepare(Grid(i), Quaternion.identity);
                previous[i] = trucks[i].body.position; previousRear[i] = Rear(trucks[i]); lastThrottle[i] = 0;
                reactionAt[i] = .12f + .11f * (2 - (int)setup.players[i].difficulty) + (i % 3) * .035f;
                Brake(trucks[i]);
            }
            drivingViews.Layout(); atmosphere.Configure(Seed, 0, 1);
        }

        protected override void Simulate(float dt)
        {
            noticeTime = Mathf.Max(0, noticeTime - dt); atmosphere.Tick(rules.Elapsed);
            explosionViewTime = Mathf.Max(0, explosionViewTime - dt);
            if (rules.Phase == RedLightPhase.Complete)
            {
                completionAge += dt; review.Capture(rules.Elapsed + completionAge);
                for (int i = 0; i < setup.count; i++) if (trucks[i].Powered) Brake(trucks[i]);
                if (completionAge >= 2) { review.focusSlot = Mathf.Max(0, rules.Winner); EndMatch(); }
                return;
            }
            // Observe the displacement caused by the PREVIOUS physics command,
            // before writing current input or changing the signal state.
            for (int i = 0; i < setup.count; i++)
            {
                if (rules.outcomes[i] != RedLightOutcome.Racing) continue;
                var t = trucks[i]; var p = t.body.position; var velocity = t.body.linearVelocity;
                Vector2 now = new Vector2(p.x, p.z), before = new Vector2(previous[i].x, previous[i].z);
                bool violation = rules.Observe(i, now, before, new Vector2(velocity.x, velocity.z), Vector3.Dot(velocity, t.transform.forward), lastThrottle[i], t.tractorMotor.Grounded, dt);
                if (violation)
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.Log("RLGL_VIOLATION slot=" + i + " phase=" + rules.Phase + " clock=" + rules.Elapsed.ToString("F3") + " phaseTime=" + rules.PhaseTime.ToString("F3") + " position=" + p.ToString("F4") + " previous=" + previous[i].ToString("F4") + " velocity=" + velocity.ToString("F4") + " command=" + lastThrottle[i] + " brake=" + t.tractorMotor.vehicle.input.states.brakes + " handbrake=" + t.tractorMotor.vehicle.input.states.handbrake + " grounded=" + t.tractorMotor.Grounded + " reason=" + rules.reasons[i]);
#endif
                    Explode(i);
                }
                else if (rules.Phase != RedLightPhase.Countdown)
                {
                    float rear = Rear(t), priorRear = previousRear[i];
                    if (Mathf.Abs(p.x) + t.shape.bounds.extents.x <= trackHalfWidth &&
                        RedLightRules.WholeBodyCrossing(priorRear, rear, finishZ, out float fraction) && rules.Finish(i, Mathf.Max(0, rules.Elapsed - 3 - dt + dt * fraction)))
                    { LegalFinishes++; t.TickDrive(Vector2.zero); Brake(t); Announce(setup.players[i].name + " FINISHED!", 5); }
                    else if ((Mathf.Abs(p.x) > trackHalfWidth + 5 || p.y < -8) && rules.Eliminate(i, "LEFT THE COURSE")) Explode(i);
                }
                previous[i] = p; previousRear[i] = Rear(t);
            }
            review?.Capture(rules.Elapsed);
            rules.Advance(dt);
            UpdateSignal();
            if (rules.Phase == RedLightPhase.Complete) return;
            for (int i = 0; i < setup.count; i++)
            {
                if (rules.outcomes[i] != RedLightOutcome.Racing)
                { if (rules.outcomes[i] == RedLightOutcome.Finished && Rear(trucks[i]) < finishZ + 22) trucks[i].TickDrive(new Vector2(0, .35f)); else Brake(trucks[i]); continue; }
                var command = setup.players[i].source == BowlingSource.AI ? Bot(i) : commands[i];
                if (rules.Phase == RedLightPhase.Countdown) command = new Vector2(0, -1);
                trucks[i].tractorMotor.gripScale = atmosphere.Grip;
                trucks[i].TickDrive(command); lastThrottle[i] = command.y;
                // At rest a brake input must hold, rather than ask an automatic
                // transmission to select reverse and create a false violation.
                bool botBraking = setup.players[i].source == BowlingSource.AI && command.y < -.1f;
                if (rules.Phase == RedLightPhase.Countdown || botBraking || setup.players[i].source != BowlingSource.AI && ActionHeld(i))
                { Brake(trucks[i]); lastThrottle[i] = 0; }
            }
        }

        Vector2 Bot(int slot)
        {
            var t = trucks[slot]; float error = Grid(slot).x - t.body.position.x;
            bool stop = rules.Phase == RedLightPhase.Countdown || rules.Phase == RedLightPhase.Red || rules.Phase == RedLightPhase.LaughWarning && rules.PhaseTime >= reactionAt[slot];
            float steer = Mathf.Clamp(error * .07f - Vector3.SignedAngle(Vector3.forward, t.transform.forward, Vector3.up) * .03f, -.6f, .6f);
            // The AI receives the same audible warning, physical braking and
            // weather grip as humans. Difficulty changes response, not outcomes.
            float targetSpeed = (5.1f + .25f * (int)setup.players[slot].difficulty) * atmosphere.Grip;
            float speed = Vector3.Dot(t.body.linearVelocity, t.transform.forward);
            return new Vector2(steer, stop ? -1 : speed < targetSpeed - .2f ? 1 : speed > targetSpeed + .35f ? -.25f : .2f);
        }
        void Brake(SportsEventTruck truck)
        {
            if (!truck.Powered || truck.tractorMotor == null) return;
            var input = truck.tractorMotor.vehicle.input;
            input.states.throttle = input.states.throttleRaw = 0;
            // NWH Auto D/N/R interprets a held service brake at rest as reverse.
            // Handbrake holds the settled truck without feeding that reverse
            // request. Keep ordinary service braking while actually slowing.
            bool slowing = truck.body.linearVelocity.sqrMagnitude > .25f;
            input.states.brakes = input.states.brakesRaw = slowing ? 1 : 0;
            input.states.handbrake = input.states.handbrakeRaw = 1;
            var transmission = truck.tractorMotor.vehicle.powertrain.transmission;
            if (!slowing && transmission.Gear < 0) transmission.ShiftInto(0, true);
        }
        void Announce(string message, int cue)
        { notice = message; noticeTime = 2.5f; feedback.Play(cue, 0, true); }
        void Explode(int slot)
        {
            if (fragments[slot].Bursting) return;
            var t = trucks[slot]; var center = t.body.worldCenterOfMass;
            if (explosionViewTime <= 0) recentExplosions = new Bounds(center, Vector3.one * 20); else recentExplosions.Encapsulate(new Bounds(center, Vector3.one * 20));
            explosionViewTime = 2;
            t.Power(false); fragments[slot].Burst(); explosions[slot].transform.position = center;
            crowd?.React(true, "RED LIGHT VIOLATION", center);
            explosions[slot].Play(true); Explosions++;
            for (int i = 0; i < setup.count; i++) if (i != slot && rules.outcomes[i] == RedLightOutcome.Racing)
            {
                var away = trucks[i].body.worldCenterOfMass - center; float distance = away.magnitude;
                if (distance > explosionRadius || distance < .01f) continue;
                var delta = away.normalized * explosionVelocity * (1 - distance / explosionRadius);
                trucks[i].tractorMotor.AddAssemblyVelocity(delta);
                // The measured shockwave is also external movement, not an
                // automatic chain of unfair red-light eliminations.
                rules.ExternalPush(i, new Vector2(delta.x, delta.z));
                Pulse(i, Mathf.Clamp01(1 - distance / explosionRadius), .25f);
            }
            Announce(setup.players[slot].name + " · " + rules.reasons[slot], 3); Pulse(slot, 1, .5f);
        }

        protected override void Present(float dt)
        {
            if (drivingViews == null) return;
            if (review != null && review.Playing) { review.Tick(); return; }
            UpdateSignal();
            drivingViews.Tick(dt);
            if (drivingViews.NeedsArena)
            {
                bool first = true; Bounds b = default;
                for (int i = 0; i < setup.count; i++) if (rules.outcomes[i] == RedLightOutcome.Racing)
                { if (first) { b = trucks[i].shape.bounds; first = false; } else b.Encapsulate(trucks[i].shape.bounds); }
                if (first) b = explosionViewTime > 0 ? recentExplosions : new Bounds(new Vector3(0, 2, finishZ), new Vector3(60, 6, 60));
                if (explosionViewTime > 0) b.Encapsulate(recentExplosions);
                b.Expand(new Vector3(18, 8, 24)); framing.arenaBounds = b;
                broadcast.view.orthographic = true;
                broadcast.Pose(b.center, new Vector3(0, 70, -100), 60);
                broadcast.transform.rotation = Quaternion.LookRotation(new Vector3(0, -70, 100), Vector3.up);
                framing.Apply();
            }
        }
        void UpdateSignal()
        {
            // The cue belongs to the authoritative phase transition, not a
            // rendered-frame sample that could skip the entire warning at low FPS.
            if (presentedPhase != rules.Phase)
            {
                presentedPhase = rules.Phase;
                bool red = rules.Phase == RedLightPhase.Red;
                tint.SetColor("_BaseColor", red ? Color.red : Color.green);
                tint.SetColor("_EmissionColor", (red ? Color.red : Color.green) * 4);
                foreach (var signal in signals) signal.SetPropertyBlock(tint);
                signalCaption.text = red ? "RED LIGHT" : rules.Phase == RedLightPhase.Countdown ? "GET READY" : "GREEN LIGHT";
                if (rules.Phase == RedLightPhase.LaughWarning)
                {
                    if (laughs.Length > 0) { laugh.Stop(); laugh.clip = laughs[rules.Laughs % laughs.Length]; laugh.volume = settings.sfx * .8f; laugh.Play(); }
                    masterAnimator.Play("Laugh", 0, 0);
                    masterShake?.Kill(); masterShake = DOTween.Sequence().Append(master.DOLocalRotate(new Vector3(0, 180, 7), .12f).SetLoops(6, LoopType.Yoyo)).Append(master.DOLocalRotate(new Vector3(0, 180, 0), .15f));
                }
                else if (rules.Phase == RedLightPhase.Red) { masterAnimator.Play("Watch", 0, 0); feedback.Play(2, 0, true); }
            }
        }
        public override void ExtraAction(string key) { if (key == "drivingview") drivingViews.Cycle(); else review?.Action(key); }
        protected override void SetPaused(bool pause) { atmosphere?.Pause(pause); if (pause) { laugh?.Pause(); masterShake?.Pause(); } else { laugh?.UnPause(); masterShake?.Play(); } }
        protected override void StopSounds() { if (laugh != null) laugh.Stop(); if (trucks != null) foreach (var t in trucks) t.StopSound(); }
        protected override void StopGame()
        {
            masterShake?.Kill(); StopSounds();
            review?.End(false);
            if (trucks == null) return;
            for (int i = 0; i < trucks.Length; i++) { trucks[i].Power(false); trucks[i].body.isKinematic = true; fragments[i].Freeze(); }
        }
    }
}
