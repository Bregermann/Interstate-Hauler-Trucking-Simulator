using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using LWS.TruckSports;
using LWS.TruckBowling;
using LWS.TruckPedestrianPlowing;
namespace LWS.TruckTrailerPlug
{
    public enum TrailerPlugPhase { CountIn, Swing, Flight, AttemptResult, Replay, Complete }
    public sealed class TrailerPlugMatch : SportsEventSession, IPlowCrowdContext
    {
        public TrailerPlugRig rig;
        public SportsEventCamera broadcast;
        public SportsMotionReplay replay;
        public SportsChaosWeather atmosphere;
        public SportsWeatherPresets presets;
        public PlowSettings crowdSettings;
        public Transform crowdRoot, hole;
        public ParticleSystem steam, bubbles;
        public AudioSource voice;
        public float maximumSwingSeconds = 35, maximumFlightSeconds = 40;
        public Vector2 openingRadii = new Vector2(2.4f, 6);
        public int crowdLimit = 144;
        public readonly TrailerPlugRules rules = new TrailerPlugRules();
        public TrailerPlugPhase Phase { get; private set; }
        public float Age { get; private set; }
        public int PhysicalReleases { get; private set; }
        public int PhysicalHits { get; private set; }
        public int SettledAttempts { get; private set; }
        public IReadOnlyList<PlowPedestrian> Crowd => crowd;
        readonly List<PlowPedestrian> crowd = new List<PlowPedestrian>(192);
        readonly List<TrailerPlugEmergence> emergences = new List<TrailerPlugEmergence>(192);
        readonly List<Vector2> coverageSamples = new List<Vector2>(180);
        NavMeshPath route;
        readonly StringBuilder text = new StringBuilder();
        readonly Collider[] noClimbing = Array.Empty<Collider>();
        System.Random random;
        float quiet, nextSpawn, totalAge, lastImpactTime = -100, nextVoice;
        int spawned;
        Vector3 lastImpact;
        string announcement;
        public override string Title => "TRUCK TRAILER PLUG";
        public override int MinimumPlayers => 1;
        public override int MaximumPlayers => 4;
        public override int OptionCount => 2;
        public override string OptionLabel => option == 0 ? "THREE DROPS EACH" : "FIVE DROPS EACH";
        public override bool ReplayControls => Phase == TrailerPlugPhase.Replay;
        public override string Help => "SWING SIX TONS. DROP THE TRAILER. PLUG THE HOLE.\n\n" +
            "Your assigned movement inputs pump the established bungee swing. ACTION releases the real hitch, once. The trailer keeps its actual velocity and spin. The truck stays on its bungee. Release timing determines the landing; there is no homing or midair control.\n\n" +
            "100 points per distinct pedestrian physically struck at meaningful speed. Ragdolls cannot score twice. A supported, quiet trailer earns 500–2,000 points for partial coverage, 5,000 for a seal, and 2,000 extra for a centered upright seal. Coverage is measured through the actual trailer colliders after 1.8 seconds of stable observation. Hovering, brushing the rim or ongoing motion cannot bank a plug.\n\n" +
            "The finite crowd emerges faster as you wait. Release within 35 seconds; the watchdog releases a stalled rig with its existing momentum. A delivery still moving after 40 seconds retains actual pedestrian hits but earns no unproven resting bonus. Spawning stops on a confirmed seal.\n\n" +
            "One to four participants take alternating turns. Three or five drops EACH; highest cumulative score wins and ties share victory. Pause/reconnect preserves the hitch, flight, crowd and scores. Recorded replays use actual transforms, never resimulate the result. AI uses the same pump and release controls. Offline only.";
        public override string Header => Title + " · ROUND " + Mathf.Min(rules.Round, rules.Rounds) + "/" + rules.Rounds + " · " + atmosphere.Description;
        public override string Status => Phase == TrailerPlugPhase.CountIn ? "READY · " + Mathf.Max(1, Mathf.CeilToInt(3 - Age)) :
            Phase == TrailerPlugPhase.Swing ? setup.players[rules.Participant].name + " · PUMP WITH MOVEMENT · ACTION RELEASES · " + Mathf.Max(0, maximumSwingSeconds - Age).ToString("F0") + "s" :
            Phase == TrailerPlugPhase.Flight ? "TRAILER RELEASED · " + rules.DistinctHits + " DISTINCT IMPACTS · LET THE LANDING SETTLE" : announcement;
        public override string Standings
        {
            get { text.Clear(); for (int i = 0; i < setup.count; i++) text.Append(setup.players[i].name).Append("  ").Append(rules.score[i]).Append(" PTS · ").Append(rules.attempts[i]).Append('/').Append(rules.Rounds).Append(" DROPS\n"); return text.ToString(); }
        }
        public override string Results
        {
            get { text.Clear(); for (int i = 0; i < setup.count; i++) if (rules.winners[i]) text.Append(setup.players[i].name).Append("  "); return text.ToString() + "WIN THE QUARRY!\n\n" + Standings; }
        }
        public override bool TryReadScoreCard(int i, out string value, out string detail)
        { value = rules.score[i].ToString(); detail = rules.attempts[i] + "/" + rules.Rounds + " DROPS" + (i == rules.Participant && !rules.Complete ? " · CURRENT" : ""); return true; }
        protected override void BeginGame()
        {
            int seed = UnityEngine.Random.Range(1, int.MaxValue); random = new System.Random(seed);
            atmosphere.ConfigurePreset(presets.Resolve(seed), seed); rules.Start(setup.count, option == 0 ? 3 : 5);
            PhysicalReleases = PhysicalHits = SettledAttempts = 0; BeginAttempt();
        }
        void BeginAttempt()
        {
            foreach (var pedestrian in crowd) { pedestrian.ragdoll.PrepareForPool(); pedestrian.gameObject.SetActive(false); }
            spawned = 0; Age = totalAge = quiet = nextSpawn = 0; Phase = TrailerPlugPhase.CountIn;
            rig.truck.slot=rules.Participant; rig.Prepare(); rig.Paint(setup.players[rules.Participant].color); replay.Colorize(new[]{setup.players[rules.Participant].color,Color.white}); replay.ResetRecord();
            bubbles.Play(); steam.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); Gate();
        }
        Vector2 AIPump(out bool release)
        {
            var body = rig.trailer; float skill = (int)setup.players[rules.Participant].difficulty;
            float time = (body.linearVelocity.y + Mathf.Sqrt(body.linearVelocity.y * body.linearVelocity.y + 2 * 9.81f * Mathf.Max(0, body.position.y))) / 9.81f;
            var predicted = body.position + Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up) * time;
            Vector3 desired = hole.position - predicted;
            float error = Mathf.Sin((rules.Round * 11 + rules.Participant * 7) * .77f) * (2.5f - skill * .85f);
            desired.x += error; desired.z += error * .6f;
            release = Age > 7 && new Vector2(desired.x, desired.z).magnitude < 2.8f + (2 - skill) * .65f && body.position.y < 30;
            if (Age > 28 + skill) release = true;
            return Vector2.ClampMagnitude(new Vector2(desired.x, desired.z) * .12f - new Vector2(body.linearVelocity.x, body.linearVelocity.z) * .04f, 1);
        }
        protected override void Simulate(float dt)
        {
            Age += dt; totalAge += dt; atmosphere.Tick(Time.time);
            if (Phase == TrailerPlugPhase.CountIn)
            { if (Age >= 3) { rig.BeginSwing(); Phase = TrailerPlugPhase.Swing; Age = 0; feedback.Play(5, 0, true); } return; }
            if (Phase == TrailerPlugPhase.Replay)
            { if (replay.Step(dt)) { StopReplay(); NextAttempt(); } return; }
            if (Phase == TrailerPlugPhase.AttemptResult)
            { if (Age >= 3) { if (AllAI) NextAttempt(); else BeginReplay(); } return; }
            if (Phase != TrailerPlugPhase.Swing && Phase != TrailerPlugPhase.Flight) return;
            if (!rules.Sealed && spawned < Mathf.Clamp(crowdLimit, 16, TrailerPlugRules.MaximumCrowd) && totalAge >= nextSpawn)
            { Spawn(); nextSpawn = totalAge + Mathf.Lerp(.8f, .18f, Mathf.Clamp01(totalAge / 30)); }
            for(int i=0;i<crowd.Count;i++)if(crowd[i].gameObject.activeSelf&&(!emergences[i].Active||crowd[i].Claimed))crowd[i].Tick(dt);
            replay.Record(totalAge, 3);
            if (Phase == TrailerPlugPhase.Swing)
            {
                bool release; Vector2 drive;
                int slot = rules.Participant;
                if (setup.players[slot].source == BowlingSource.AI) drive = AIPump(out release);
                else { drive = commands[slot]; release = actions[slot]; }
                rig.Pump(drive);
                if (release || Age >= maximumSwingSeconds) Release();
            }
            else
            {
                bool slow = rig.trailer.linearVelocity.sqrMagnitude < .09f && rig.trailer.angularVelocity.sqrMagnitude < .12f;
                quiet = slow ? quiet + dt : 0;
                if (quiet >= 1.8f || Age >= maximumFlightSeconds || rig.trailer.position.y < -35)
                    Resolve(quiet >= 1.8f);
            }
        }
        public bool Release()
        {
            if (Phase != TrailerPlugPhase.Swing || !rules.Release()) return false;
            if (!rig.Release()) throw new InvalidOperationException("Authoritative trailer release had no physical hitch.");
            PhysicalReleases++; Phase = TrailerPlugPhase.Flight; Age = quiet = 0;
            feedback.Play(2, 0, true); Pulse(rules.Participant, .2f, .12f); return true;
        }
        public float MeasureCoverage()
        {
            int covered = 0;
            foreach (var point in coverageSamples)
            {
                var origin = hole.position + new Vector3(point.x, -.3f, point.y);
                foreach (var collider in rig.trailer.GetComponentsInChildren<Collider>())
                    if (collider.enabled && collider.Raycast(new Ray(origin, Vector3.up), out _, 6)) { covered++; break; }
            }
            return coverageSamples.Count == 0 ? 0 : covered / (float)coverageSamples.Count;
        }
        void Resolve(bool stable)
        {
            float coverage = MeasureCoverage(); float center = Vector3.ProjectOnPlane(rig.trailer.position - hole.position, Vector3.up).magnitude;
            if (!rules.Resolve(coverage, rig.Supported, stable, center, Vector3.Dot(rig.trailer.rotation * Vector3.up, Vector3.up))) return;
            if (stable) SettledAttempts++;
            announcement = rules.Sealed ? rules.Perfect ? "PERFECT SEAL!" : "HOLE PLUGGED!" : rules.Coverage > 0 ? "PARTIAL PLUG · " + Mathf.RoundToInt(rules.Coverage * 100) + "%" : "MISSED THE PLUG";
            announcement += " · " + rules.DistinctHits + " IMPACTS · +" + rules.AttemptPoints;
            if (rules.Sealed) { bubbles.Stop(); steam.Play(); feedback.Play(5, 0, true); }
            else feedback.Play(4, 0, true);
            rig.Hold(); Phase = TrailerPlugPhase.AttemptResult; Age = 0;
        }
        void NextAttempt()
        { if (!rules.Next()) { Phase = TrailerPlugPhase.Complete; EndMatch(); } else BeginAttempt(); }
        void BeginReplay()
        { rig.Hold(); rig.truck.visuals.gameObject.SetActive(false); foreach (var renderer in rig.trailer.GetComponentsInChildren<Renderer>()) renderer.enabled = false; replay.Begin(Mathf.Max(0, replay.Duration - 14)); Phase = TrailerPlugPhase.Replay; }
        void StopReplay()
        { replay.End(); rig.truck.visuals.gameObject.SetActive(true); foreach (var renderer in rig.trailer.GetComponentsInChildren<Renderer>()) renderer.enabled = true; }
        public override void ExtraAction(string key)
        {
            if (Phase != TrailerPlugPhase.Replay) return;
            if (key == "replayskip") { StopReplay(); NextAttempt(); }
            else if (key == "replayangle") replay.Angle = (replay.Angle + 1) % 3;
            else if (key == "replayspeed") replay.Speed = replay.Speed == 1 ? .35f : replay.Speed < 1 ? 2 : 1;
            else if (key == "replayrewind") replay.Seek(0);
        }
        protected override void Present(float dt)
        {
            if (broadcast == null || rig == null) return;
            Vector3 focus = !Running ? new Vector3(0, 22, -5) : Phase == TrailerPlugPhase.Swing || Phase == TrailerPlugPhase.CountIn ?
                Vector3.Lerp(rig.truck.body.position, hole.position, .4f) : Phase == TrailerPlugPhase.Replay ? replay.Position(1) : rig.trailer.position;
            focus.y = Mathf.Max(2, focus.y);
            Vector3 offset = Phase == TrailerPlugPhase.Swing || Phase == TrailerPlugPhase.CountIn ? new Vector3(30, 15, -40) : new Vector3(21, 16, -24);
            if (Phase == TrailerPlugPhase.Replay) offset = replay.Angle == 1 ? new Vector3(-18, 12, 22) : replay.Angle == 2 ? new Vector3(2, 38, -8) : offset;
            broadcast.Pose(focus, offset, 65);
        }
        protected override void SetPaused(bool pause) { atmosphere?.Pause(pause); if (pause) voice?.Pause(); else voice?.UnPause(); }
        protected override void StopGame() { rig?.Hold(); replay?.End(); bubbles?.Stop(); }
        protected override void StopSounds() { rig?.truck.StopSound(); voice?.Stop(); }
        void StartSamples()
        { for (int x = -7; x <= 7; x++) for (int z = -12; z <= 12; z++) { var p = new Vector2(x / 7f, z / 12f); if (p.sqrMagnitude < .9f) coverageSamples.Add(Vector2.Scale(p, openingRadii)); } }
        protected override void Awake() { base.Awake(); StartSamples(); }
        void Spawn()
        {
            float angle = (float)random.NextDouble() * Mathf.PI * 2;
            var desired = hole.position + new Vector3(Mathf.Cos(angle) * (openingRadii.x + 1.3f), 0, Mathf.Sin(angle) * (openingRadii.y + 1.3f));
            if (!NavMesh.SamplePosition(desired, out var hit, 3, NavMesh.AllAreas)) return;
            PlowPedestrian pedestrian;
            if (spawned < crowd.Count) pedestrian = crowd[spawned];
            else { pedestrian = PlowCrowdFactory.Create(PlowCrowdFactory.Choose(crowdSettings.characters, random), crowdRoot, crowdSettings); crowd.Add(pedestrian); emergences.Add(pedestrian.gameObject.AddComponent<TrailerPlugEmergence>()); }
            pedestrian.gameObject.SetActive(true);
            pedestrian.ResetTarget(this, spawned, PlowTargetKind.Standard, hit.position, Quaternion.Euler(0, angle * Mathf.Rad2Deg, 0), pedestrian.characterName, pedestrian.panicLine);
            emergences[spawned].Begin(pedestrian,hit.position); Reaction(pedestrian); spawned++;
        }
        PlowSettings IPlowCrowdContext.CrowdSettings => crowdSettings;
        int IPlowCrowdContext.ThreatCount => 1;
        Rigidbody IPlowCrowdContext.ThreatBody(int index) => rig.Released ? rig.trailer : rig.truck.body;
        Collider[] IPlowCrowdContext.ClimbSurfaces => noClimbing;
        public bool TryRoute(Vector3 from, Vector3 desired, out Vector3 destination)
        { route ??= new NavMeshPath(); destination = from; if (!NavMesh.SamplePosition(from, out var a, 4, NavMesh.AllAreas) || !NavMesh.SamplePosition(desired, out var b, 4, NavMesh.AllAreas)) return false; if (!NavMesh.CalculatePath(a.position, b.position, NavMesh.AllAreas, route) || route.status != NavMeshPathStatus.PathComplete) return false; destination = b.position; return true; }
        public Vector3 RandomDestination(Vector3 from, float distance) => from + new Vector3(((float)random.NextDouble() - .5f) * distance, 0, ((float)random.NextDouble() - .5f) * distance);
        public bool NearImpact(Vector3 p) => Time.time - lastImpactTime < 1 && (p - lastImpact).sqrMagnitude < 100;
        public bool NearView(Vector3 p, float distance) { var v = broadcast.view.WorldToViewportPoint(p); return v.z > 0 && v.x > -.1f && v.x < 1.1f && v.y > -.1f && v.y < 1.1f; }
        public bool SafeToRetire(Vector3 p) => !NearView(p, 100);
        public void Reaction(PlowPedestrian pedestrian)
        { if (!Live || Time.time < nextVoice || pedestrian.reactions.Length == 0) return; nextVoice = Time.time + 2.5f; var line = pedestrian.reactions[random.Next(pedestrian.reactions.Length)]; if (line.generatedAudio != null) { voice.volume = settings.sfx * .22f; voice.PlayOneShot(line.generatedAudio); } }
        public void Contact(PlowPedestrian pedestrian, Collision collision)
        {
            if (!Live || Phase != TrailerPlugPhase.Flight || pedestrian.Claimed || collision.rigidbody != rig.trailer || collision.collider.isTrigger) return;
            for (int i = 0; i < collision.contactCount; i++)
            {
                var point = collision.GetContact(i);
                Vector3 relative = rig.IncomingVelocity + Vector3.Cross(rig.IncomingAngular, point.point - rig.IncomingCenter) - pedestrian.Velocity;
                float closing = Mathf.Max(0, Vector3.Dot(relative, point.normal));
                if (!rules.Hit(pedestrian.id, closing)) continue;
                emergences[pedestrian.id].Cancel(); pedestrian.Strike(relative, point.point); PhysicalHits++; lastImpact = point.point; lastImpactTime = Time.time;
                feedback.Play(0, Mathf.Min(20, closing)); Pulse(rules.Participant, .12f, .08f); break;
            }
        }
    }
}
