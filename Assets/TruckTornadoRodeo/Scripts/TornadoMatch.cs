using System;
using System.Text;
using LWS.TruckBowling;
using LWS.TruckFareFight;
using LWS.TruckSports;
using UnityEngine;

namespace LWS.TruckTornadoRodeo
{
    public sealed class TornadoMatch : SportsEventSession
    {
        public SportsEventTruck[] trucks;
        public TornadoRider[] riders;
        public TornadoDebris[] debris;
        public TornadoRing[] rings;
        public Transform[] starts;
        public TornadoField field;
        public Collider ground;
        public TornadoPresentation presentation;
        public SportsDrivingViews drivingViews;
        public SportsEventCamera broadcast;
        public SportsCameraFit framing;
        public TornadoRules rules = new TornadoRules();
        public int AIPlans { get; private set; }
        public int AIFlares { get; private set; }
        public int LastScorer { get; private set; } = -1;
        public override string Title => "TRUCK TORNADO RODEO";
        public override int MinimumPlayers => 1;
        public override int MaximumPlayers => 4;
        public override int WeatherCount => 1;
        public override bool SupportsSplitAIViews => true;
        public override bool MultiViewHUD => drivingViews != null && !drivingViews.NeedsArena;
        public override bool CompactBroadcastStatus => true;
        public override bool CompactViewportHud => true;
        public override int CompactScoreColumns => 4;
        public override string OptionLabel => option == 0 ? "90 SECOND RODEO" : option == 1 ? "150 SECOND RODEO" : "210 SECOND RODEO";
        public override string Header => Title;
        public override string Status => rules.Countdown > 0 ? "READY · " + Mathf.CeilToInt(rules.Countdown) : Mathf.CeilToInt(rules.Remaining) + "s LEFT · " + Living + " RIDERS";
        public override bool BroadcastParticipantActive(int slot) => slot >= 0 && slot < setup.count && !rules.retired[slot];
        public override string Help => "Drive your tractor into the storm funnel. Suction, orbit, lift and gusts act on the physical truck. Reach each new 10-metre altitude tier for 5 points, complete a full rotation for 12, fit your whole truck through an aerial ring for 30, avoid a close flying object for 3, and land upright under control for 40. Rings and debris score once per real flight. In the air, left/right gives limited correction and roll; forward/back gives limited correction and pitch. Release forward/back once after lift-off to arm that correction. ACTION opens a seven-second flare: reduced lift, outward drag and upright torque help you leave the funnel and land. The flare has an eight-second cooldown. A controlled landing requires wheel support for 0.8 seconds, a mostly upright truck, downward contact speed at most 12 m/s and ground speed at most 16 m/s. An uncontrolled landing earns no bonus. Drive back into the funnel for another flight. On the ground, ACTION can self-right a slow overturned tractor with a physical hop; there are no active-run teleports. Flying sheep are cartoonish and unharmed. AI drives in, attempts real stunts, flares and lands using the same controls. The timed match counts actual actions; tied scores share first place. Profiles, horn, free-look, shared/chase/cab views, pause and rematch use the collection controls.";
        public override string ControlCaption(int slot) => base.ControlCaption(slot).Replace(" ACTION", " FLARE / SELF-RIGHT");
        int Living { get { int n = 0; for (int i = 0; i < setup.count; i++) if (!rules.retired[i]) n++; return n; } }
        public override bool TryReadScoreCard(int slot, out string score, out string detail)
        {
            bool valid = slot >= 0 && slot < setup.count; score = valid ? rules.points[slot] + " PTS" : ""; detail = valid ? rules.bestHeight[slot].ToString("F0") + "m BEST · " + rules.rings[slot] + " RINGS · " + rules.landings[slot] + " LANDED" : ""; return valid;
        }
        public override bool TryReadViewportCard(int view, out int participant, out string score, out string detail) => TryReadDrivingViewportCard(drivingViews, view, out participant, out score, out detail);
        public override string ViewportTelemetry(int view)
        {
            int slot = drivingViews.rally.owners[view]; return rules.retired[slot] ? "RODEO FINISHED" : rules.Countdown > 0 ? Status : (riders[slot].Flaring ? "FLARING · " : rules.airborne[slot] ? "AIRBORNE · " : "GROUND · ") + Mathf.Max(0, trucks[slot].body.worldCenterOfMass.y - field.Center.y).ToString("F0") + "m · " + Mathf.CeilToInt(rules.Remaining) + "s";
        }
        public override string ViewText(int view) { int slot = drivingViews.rally.owners[view]; TryReadScoreCard(slot, out string score, out string detail); return setup.players[slot].name + " · " + score + "\n" + detail + "\n" + ViewportTelemetry(view); }
        readonly StringBuilder table = new StringBuilder(400);
        public override string Standings { get { table.Clear(); for (int place = 1; place <= setup.count; place++) for (int i = 0; i < setup.count; i++) if (rules.Place(i) == place) table.Append('#').Append(place).Append(' ').Append(setup.players[i].name).Append(" · ").Append(rules.points[i]).Append(" PTS · ").Append(rules.bestHeight[i].ToString("F0")).Append("m · ").Append(rules.turns[i]).Append(" TURNS · ").Append(rules.rings[i]).Append(" RINGS · ").Append(rules.landings[i]).Append(" LANDINGS\n"); return table.ToString(); } }
        public override string Results => (rules.Tied ? "SHARED STORM CHAMPIONS!" : rules.Winner >= 0 ? setup.players[rules.Winner].name + " WINS TORNADO RODEO!" : "TORNADO RODEO RESULTS") + "\n\n" + Standings;
        readonly FareFightRoute[] drivers = new FareFightRoute[4];
        readonly float[] nextPlan = new float[4], recoverAt = new float[4];
        readonly Vector3[] goals = new Vector3[4];
        int receipt; float lastAwardAt;
        protected override void BeginGame()
        {
            if (field == null || ground == null || riders == null || riders.Length != 4 || trucks == null || trucks.Length != 4 || starts == null || starts.Length != 4 || debris == null || debris.Length == 0 || rings == null || rings.Length != 6 || drivingViews == null || presentation == null) throw new InvalidOperationException("Author the complete Tornado Rodeo venue before play.");
            rules.Reset(setup.count, option == 0 ? 90 : option == 1 ? 150 : 210); receipt = AIPlans = AIFlares = 0; LastScorer = -1; lastAwardAt = -100;
            for (int i = 0; i < trucks.Length; i++)
            {
                var truck = trucks[i]; truck.gameObject.SetActive(i < setup.count); drivers[i] ??= new FareFightRoute(); drivers[i].Reset(); nextPlan[i] = recoverAt[i] = 0;
                if (i >= setup.count) continue; truck.tractorMotor.Configure(); truck.Prepare(starts[i].position, starts[i].rotation); riders[i].ResetRider(); Hold(truck);
            }
            foreach (var prop in debris) prop.ResetProp(); drivingViews.Layout(); presentation.Begin(); Physics.SyncTransforms();
        }
        static void Hold(SportsEventTruck truck) { var input = truck.tractorMotor.vehicle.input; input.states.throttle = input.states.throttleRaw = input.states.brakes = input.states.brakesRaw = 0; input.states.handbrake = input.states.handbrakeRaw = 1; }
        protected override void Simulate(float dt)
        {
            bool before = rules.Live; rules.Tick(dt);
            if (rules.Complete)
            {
                for (int i = 0; i < setup.count; i++) if (rules.Place(i) == 1) ConfirmOutcome("rodeo-result:" + i, i, rules.points[i], rules.Tied ? "SHARED STORM CHAMPION" : "STORM CHAMPION", "PTS", SportsScoreTier.MatchChampion, matchEnded: true);
                EndMatch(); return;
            }
            if (!rules.Live) { for (int i = 0; i < setup.count; i++) Hold(trucks[i]); return; }
            if (!before) foreach (var prop in debris) prop.body.isKinematic = false;
            foreach (var prop in debris) prop.Step();
            for (int i = 0; i < setup.count; i++)
            {
                if (rules.retired[i]) continue; bool ai = setup.players[i].source == BowlingSource.AI; var command = ai ? Bot(i, dt) : commands[i];
                trucks[i].tractorMotor.gripScale = presentationWeather != null ? presentationWeather.Grip : 1;
                bool action = actions[i];
                if (ai && rules.airborne[i] && !riders[i].Flaring && (riders[i].FlightSeconds > 11 + i || trucks[i].body.position.y > 68)) { if (riders[i].Flare()) AIFlares++; }
                if (!rules.airborne[i] && (action || ai && Vector3.Dot(trucks[i].transform.up, Vector3.up) < .45f)) Recover(i);
                riders[i].Step(command, action, dt);
            }
        }
        Vector2 Bot(int slot, float dt)
        {
            var truck = trucks[slot];
            if (!rules.airborne[slot])
            {
                if (rules.Clock >= nextPlan[slot]) { nextPlan[slot] = rules.Clock + .35f; AIPlans++; float a = slot * Mathf.PI * .5f + .3f; goals[slot] = field.Center + new Vector3(Mathf.Cos(a) * 9, 0, Mathf.Sin(a) * 9); }
                drivers[slot].DirectGoal(truck.body.position, goals[slot]); return drivers[slot].Drive(truck.body, truck.transform, (int)setup.players[slot].difficulty, dt, presentationWeather != null ? presentationWeather.Grip : 1);
            }
            Vector3 target = field.Center + Vector3.up * 28; float nearest = float.MaxValue;
            foreach (var ring in rings) if (!rules.RingPassed(slot, ring.id)) { float d = (ring.transform.position - truck.body.position).sqrMagnitude; if (d < nearest) { nearest = d; target = ring.transform.position; } }
            var direction = Vector3.ProjectOnPlane(target - truck.body.position, Vector3.up).normalized;
            if (riders[slot].Flaring) return Vector2.zero;
            return Vector2.ClampMagnitude(new Vector2(direction.x * .5f + .45f, direction.z * .5f), 1);
        }
        bool Recover(int slot)
        {
            var truck = trucks[slot]; if (rules.Clock < recoverAt[slot] || truck.body.linearVelocity.magnitude > 2 || Vector3.Dot(truck.transform.up, Vector3.up) > .55f) return false;
            recoverAt[slot] = rules.Clock + 5; truck.tractorMotor.AddAssemblyVelocity(Vector3.up * 2.7f); truck.body.AddTorque(Vector3.Cross(truck.transform.up, Vector3.up) * 4, ForceMode.VelocityChange); return true;
        }
        public void Award(int slot, int delta, string reason, SportsScoreTier tier)
        {
            if (!Live || delta <= 0) return;
            LastScorer = slot; lastAwardAt = rules.Clock; ConfirmScore("rodeo:" + (++receipt), slot, delta, rules.points[slot], reason, tier);
            if (tier != SportsScoreTier.Small) feedback.PlayAt(3, trucks[slot].body.position, 0, true);
        }
        public void Retired(int slot) => ConfirmOutcome("rodeo-retired:" + slot, slot, rules.points[slot], "RODEO RUN FINISHED", "PTS", SportsScoreTier.Small);
        SportsBroadcastFraming configuredFraming;
        SportsBroadcastSettings airborneFraming,entryFraming;
        ParticleSystemRenderer entryFunnel;
        TornadoEntryGroundConstraint entryGroundConstraint;
        bool entryShot;
        public bool GroundEntryShot => entryShot && Running && !Finished && !ReplayControls && drivingViews != null && drivingViews.NeedsArena;
        void EnsureEntryGroundConstraint(SportsBroadcastFraming framingSource)
        {
            if(entryGroundConstraint!=null)return;var rig=framingSource.cameraRig.rig;
            // Install in vendor callback order: existing obstacle authority,
            // then the mode's absolute Body floor, then vendor Aim.
            if(rig.GetComponent<Cinemachine.CinemachineCollider>()==null)rig.gameObject.AddComponent<Cinemachine.CinemachineCollider>();
            entryGroundConstraint=rig.gameObject.AddComponent<TornadoEntryGroundConstraint>();entryGroundConstraint.match=this;
        }
        void ReleaseEntryGroundConstraint()
        {
            entryShot=false;if(entryGroundConstraint==null)return;entryGroundConstraint.Detach();Destroy(entryGroundConstraint);entryGroundConstraint=null;
        }
        Bounds EntryFunnelBounds()
        {
            if(entryFunnel==null&&presentation!=null&&presentation.funnel!=null)entryFunnel=presentation.funnel.GetComponent<ParticleSystemRenderer>();
            if(entryFunnel!=null){var b=entryFunnel.bounds;if(float.IsFinite(b.center.x)&&float.IsFinite(b.center.y)&&float.IsFinite(b.center.z)&&b.size.y>10)return b;}
            // Exact authored spiral height2..118 and billboard-size envelope,
            // used only before the original particle system has emitted a bound.
            return new Bounds(field.Center+Vector3.up*60,new Vector3(84,132,84));
        }
        void ConfigureEntryFraming(SportsBroadcastFraming framingSource,bool entering,int selected)
        {
            if(configuredFraming!=framingSource)
            {
                airborneFraming=JsonUtility.FromJson<SportsBroadcastSettings>(JsonUtility.ToJson(framingSource.Configuration));
                entryFraming=JsonUtility.FromJson<SportsBroadcastSettings>(JsonUtility.ToJson(airborneFraming));
                // R64b: the genuine funnel entry point was at viewport y1.106.
                // Ground entry needs both the tractor and approaching storm;
                // actual airborne rider/ring shots keep their authored settings.
                entryFraming.minimumTruckHeight=.084f;entryFraming.preferredTruckHeight=.10f;
                // Documented ground-entry exception: look up at the actual giant
                // funnel from behind the approaching tractor, rather than crop it
                // with the outdoor aerial angle. Same vendor rig and lens owner.
                entryFraming.elevation=-24;entryFraming.fieldOfView=85;
                // V13 clipped two upper particle corners; V14 over-aimed below
                // the tractor. Their measured midpoint preserves both subjects.
                // Explicit Clear/Snow/Rain starts verify the actual composition.
                entryFraming.screenY=.585f;
                // Entry is a two-subject shot. A half-weight objective biased the
                // actual look-at31.5% along their span and clipped its left edge.
                entryFraming.objectiveWeight=airborneFraming.primaryWeight;
                configuredFraming=framingSource;
            }
            if(entering&&selected>=0&&framingSource.Valid(selected))
            {
                var bounds=framingSource.TruckBounds(selected);var funnel=EntryFunnelBounds();var entry=funnel.center;
                float length=Mathf.Max(2,Mathf.Max(bounds.size.x,bounds.size.z));
                entryFraming.objectiveRangeInTruckLengths=Mathf.Max(airborneFraming.objectiveRangeInTruckLengths,Vector3.Distance(bounds.center,entry)/length+1);
                entryFraming.maximumDistanceInTruckLengths=Mathf.Max(airborneFraming.maximumDistanceInTruckLengths,(Vector3.Distance(bounds.center,entry)+funnel.size.y)/length);
                // The low approach-facing entry shot keeps the nearby tractor
                // below the full distant storm, rather than stack two subjects in
                // an aerial view or leave the funnel at the transverse frame edge.
                var approach=Vector3.ProjectOnPlane(field.Center-bounds.center,Vector3.up);
                if(approach.sqrMagnitude>25)entryFraming.azimuth=Mathf.Atan2(approach.x,approach.z)*Mathf.Rad2Deg;
            }
            entryShot=entering;if(entering)EnsureEntryGroundConstraint(framingSource);
            framingSource.settings=entering?entryFraming:airborneFraming;framingSource.useSharedDefaults=false;
        }
        public override void DescribeBroadcast(SportsBroadcastAction action, SportsBroadcastFraming framingSource)
        {
            if (field == null || riders == null) return; int selected = rules.Clock - lastAwardAt < 3 ? LastScorer : -1;
            if (selected < 0 || !BroadcastParticipantActive(selected))
            {
                float highest = -1; for (int i = 0; i < setup.count; i++) if (!rules.retired[i] && rules.airborne[i] && trucks[i].body.position.y > highest) { highest = trucks[i].body.position.y; selected = i; }
            }
            if (selected < 0) selected = framingSource.Nearest(field.Center, true); if (selected < 0) selected = framingSource.Nearest(field.Center);
            ConfigureEntryFraming(framingSource,selected<0||!rules.airborne[selected],selected);
            action.primary = selected; Vector3 point = field.Center + Vector3.up * 15;
            if (selected >= 0 && rules.airborne[selected])
            {
                point = trucks[selected].body.position; float nearest = float.MaxValue;
                if (!riders[selected].Flaring) foreach (var ring in rings) if (!rules.RingPassed(selected, ring.id)) { float d = (point - ring.transform.position).sqrMagnitude; if (d < nearest) { nearest = d; action.Objective(ring.transform.position, Mathf.Min(2.5f, ring.apertureRadius)); } }
                if (riders[selected].Flaring || !action.hasObjective) action.Objective(point + Vector3.down * Mathf.Min(15, point.y), 5);
            }
            else {action.objective=EntryFunnelBounds();action.hasObjective=true;}
            action.reason = selected >= 0 && rules.airborne[selected] ? riders[selected].Flaring ? "ACTUAL FLARE / LANDING" : "AIRBORNE RIDER / ACTUAL RING" : "TORNADO ENTRY";
        }
        protected override void Present(float dt)
        {
            if (drivingViews == null || !Running) return; drivingViews.Tick(dt);
            if (drivingViews.NeedsArena && broadcast.LegacyPoseAllowed)
            {
                int slot = LastScorer >= 0 && LastScorer < setup.count ? LastScorer : 0; var focus = trucks[slot].body.position;
                broadcast.Pose(focus + Vector3.up * 4, new Vector3(28, 22, -42), 62);
            }
        }
        public override void ExtraAction(string key) { if (key == "drivingview") drivingViews.Cycle(); }
        protected override void SetPaused(bool paused) => presentation?.Pause(paused);
        protected override void StopSounds() { if (trucks != null) foreach (var truck in trucks) if (truck != null) truck.StopSound(); if (!Paused) presentation?.StopSound(); }
        protected override void StopGame()
        {
            ReleaseEntryGroundConstraint();
            if (debris != null) foreach (var prop in debris) if (prop != null) prop.Freeze();
            if (trucks != null) foreach (var truck in trucks) if (truck != null) { truck.Power(false); if (!truck.body.isKinematic) truck.body.linearVelocity = truck.body.angularVelocity = Vector3.zero; truck.body.isKinematic = true; }
            presentation?.Stop(); StopSounds();
        }
    }
}
