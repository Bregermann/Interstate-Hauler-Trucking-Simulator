using System;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using LWS.TruckSports;
using LWS.TruckBowling;

namespace LWS.TruckMoonMadness
{
    public sealed partial class MoonMatch : SportsEventSession
    {
        public const float LunarGravity = 9.81f / 6;
        public SportsEventTruck[] trucks; public MoonTruck[] pilots; public MoonFlag[] flags;
        public MoonStation[] stations; public Transform[] flagSeeds; public MoonMeteor[] meteors;
        public SportsEventCamera broadcast; public SportsDrivingViews drivingViews;
        public MoonRules Rules { get; private set; }
        public float Clock { get; private set; }
        public int TotalCollections { get; private set; }
        public int CollisionDrops { get; private set; }
        public int PhysicalMeteorImpacts { get; private set; }
        public int Recoveries { get; private set; }
        public int seed = 73003;
        public float limitSeconds = 300;
        readonly float[] rest = new float[8], respawn = new float[8], stuck = new float[8];
        readonly Vector3[] safe = new Vector3[8];
        readonly NavMeshPath[] paths = new NavMeshPath[8];
        protected override void Awake()
        {
            // NavMeshPath owns a native handle; MonoBehaviour construction cannot create it.
            base.Awake();
            for(int i=0;i<paths.Length;i++)paths[i]=new NavMeshPath();
        }
        System.Random random; float nextShower, finishAge; bool outcomeLocked;
        public override string Title => "TRUCK MOON MADNESS";
        public override int MinimumPlayers => 8; public override int MaximumPlayers => 8;
        public override string OptionLabel => new[] { "FIRST TO 3 FLAGS", "FIRST TO 5 FLAGS", "FIRST TO 8 FLAGS" }[option];
        public override string Help => "LUNAR CAPTURE\nEight full tractors compete under one-sixth gravity. Drive to an actual loose flag and press ACTION while touching it to connect your cargo spring. Return slowly to YOUR numbered glowing station; the real flag must enter its volume, settle near the truck rack and remain there for half a second. Every valid delivery scores +1; repeated contact cannot score twice. Strong physical collisions can detach your cargo for anyone to recover. ACTION on bare ground performs a bounded hop. Hold ACTION in the air: driving fore/aft controls pitch, left/right controls roll/yaw, and limited jets soften descent. Fuel is finite and recharges only on wheel contact; jets cannot hover. NWH still owns the wheels, motor and suspension. Marked crater ramps offer shortcuts; navigate the rim if you lack hop fuel. Orange circles warn of actual meteors for three seconds: clear the area before the rock arrives. First to the chosen captures wins; at five minutes the actual highest score wins, including ties. A tractor stranded upside down or outside the field for ten seconds returns to its last safe ground position and drops its flag without score. Use standard pause, reconnect, views and rematch menus.";
        public override bool SupportsSplitAIViews => true;
        public override bool SupportsRaceCountdown => true; public override float RaceCountdownSeconds => Clock<3?3-Clock:-1; public override float RaceStartSeconds => Clock>=3?Clock-3:-1;
        public override bool CompactBroadcastStatus => true; public override bool CompactViewportHud => true;
        public override int CompactScoreColumns => 8;
        public override bool MultiViewHUD => drivingViews != null && !drivingViews.NeedsArena;
        public override string Header => "LUNAR CAPTURE | " + Mathf.Max(0, limitSeconds - Clock).ToString("0") + "s";
        public override string Status => meteors != null && meteors.Any(m => m.WarningSeconds > 0) ? "METEOR WARNING | CLEAR THE ORANGE CIRCLES" : Rules == null ? "LUNAR FIELD READY" : "FLAGS " + Rules.Owners.Count(o=>o==-1) + " AVAILABLE | " + Rules.Owners.Count(o=>o>=0) + " CARRIERS | " + Rules.Deliveries + " DELIVERED";
        public override string Standings => Rules == null ? "" : string.Join("\n", Enumerable.Range(0, 8).OrderByDescending(i => Rules.Scores[i]).Select(i => setup.players[i].name + " | " + Rules.Scores[i]));
        public override string Results => Rules == null ? "" : "LUNAR CAPTURE CHAMPION" + (Rules.Leaders().Length > 1 ? "S" : "") + "\n" + string.Join(" / ", Rules.Leaders().Select(i => setup.players[i].name)) + "\n" + Standings;
        public override bool TryReadScoreCard(int card, out string value, out string detail)
        {
            if (card < 0 || card >= 8) { value = detail = null; return false; }
            value = Rules == null ? "0" : Rules.Scores[card].ToString(); detail = Rules != null && Rules.Carried(card) >= 0 ? "FLAG CARRIER" : "CAPTURE"; return true;
        }
        public override Vector2 ViewportCardAnchor(int view, Rect viewport) => new Vector2(0, 1);
        public override bool TryReadViewportCard(int view, out int participant, out string score, out string detail) => TryReadDrivingViewportCard(drivingViews, view, out participant, out score, out detail);
        public override string ViewportContextPrompt(int view) => Status;
        public override string ViewportTelemetry(int view)
        {
            int owner = drivingViews != null && view >= 0 && view < drivingViews.rally.owners.Length ? drivingViews.rally.owners[view] : -1;
            return owner >= 0 && owner < 8 ? "FUEL " + (pilots[owner].Fuel * 100).ToString("0") + "% | BASE " + (owner + 1) + " | 1/6 g" : "1/6 g";
        }
        public override bool BroadcastParticipantActive(int slot) => slot >= 0 && slot < 8;
        public override void DescribeBroadcast(SportsBroadcastAction action, SportsBroadcastFraming framing)
        {
            if (Rules == null) return;
            int carrier = Enumerable.Range(0, 8).Where(i => Rules.Carried(i) >= 0).OrderByDescending(i => Rules.Scores[i]).DefaultIfEmpty(-1).First();
            action.primary = carrier >= 0 ? carrier : Rules.Scores.ToList().IndexOf(Rules.Scores.Max()); action.preferHuman = false;
            action.reason = carrier >= 0 ? "ACTUAL LUNAR FLAG CARRIER" : "LUNAR CAPTURE FIELD";
            if (carrier >= 0) action.Objective(stations[carrier].transform.position, 10); else action.Objective(Vector3.zero, 25);
        }
        public override bool ValidateSources(out string reason)
        {
            if (!base.ValidateSources(out reason)) return false;
            if (setup.players.Take(8).Count(p => p.source != BowlingSource.AI) > 4) { reason = "At most four local drivers; the other lunar trucks use AI."; return false; }
            return true;
        }
        public override void ExtraAction(string key) { if (key == "drivingview" && drivingViews != null) drivingViews.Cycle(); }
        public Vector3 Base(int owner) { float a = owner * Mathf.PI / 4; return new Vector3(Mathf.Sin(a) * 78, .7f, Mathf.Cos(a) * 78); }
        protected override void BeginGame()
        {
            if (trucks.Length != 8 || pilots.Length != 8 || stations.Length != 8 || flags.Length != 5 || flagSeeds.Length != 5 || meteors.Length != 4)
                throw new InvalidOperationException("Complete original eight-tractor lunar field required.");
            Rules = new MoonRules(flags.Length, new[] { 3, 5, 8 }[option]); random = new System.Random(seed);
            Clock = finishAge = 0; outcomeLocked=false; nextShower = 12; TotalCollections = CollisionDrops = PhysicalMeteorImpacts = Recoveries = 0;
            Array.Clear(rest, 0, 8); Array.Clear(respawn, 0, 8); Array.Clear(stuck, 0, 8);
            for (int i = 0; i < 8; i++)
            {
                pilots[i].Restore(); trucks[i].tractorMotor.Configure(); pilots[i].Apply(); pilots[i].ResetFlight();
                trucks[i].Prepare(Base(i), Quaternion.LookRotation(-Vector3.ProjectOnPlane(Base(i), Vector3.up)));
                safe[i] = trucks[i].body.position; stations[i].ResetStation();
            }
            ResetPhysicalDiagnostics();
            for (int i = 0; i < flags.Length; i++) flags[i].ResetFlag(flagSeeds[i].position);
            foreach (var meteor in meteors) meteor.ResetMeteor();
            if (drivingViews != null) drivingViews.Layout(); ConsumeActions();
        }
        protected override void Simulate(float dt)
        {
            Clock += dt; foreach(var pilot in pilots) pilot.GravityStep(); foreach(var flag in flags) flag.Step(dt); foreach(var meteor in meteors) meteor.Step(dt);
            if (Rules.Complete) { finishAge += dt; if (finishAge > 2.5f) EndMatch(); return; }
            if(Clock<3){foreach(var truck in trucks)truck.TickDrive(new Vector2(0,-1));return;}
            for (int i = 0; i < 8; i++)
            {
                var truck = trucks[i]; bool ai = setup.players[i].source == BowlingSource.AI; Vector2 drive = commands[i];
                bool action = actions[i], fire = ActionHeld(i), hop = action;
                if (ai) drive = AiDrive(i, out fire, out hop);
                bool picked = false;
                if (action || ai)
                    for (int f = 0; f < flags.Length; f++) if (flags[f].Touching(i) && flags[f].Collect(i)) { TotalCollections++; rest[f] = 0; picked = true; feedback.PlayAt(2,flags[f].body.position,0,true); break; }
                if(ai&&drive.x==0&&drive.y<=-.99f)ParkAi(truck);else truck.TickDrive(drive); pilots[i].Step(drive, fire, hop && !picked, dt, Clock);
                if (truck.tractorMotor.Grounded && Vector3.Dot(truck.transform.up, Vector3.up) > .7f && truck.body.position.y > -10) safe[i] = truck.body.position;
                bool stranded = Vector3.Dot(truck.transform.up, Vector3.up) < .15f || truck.body.position.y < -18 || Vector3.ProjectOnPlane(truck.body.position, Vector3.up).magnitude > 118;
                stuck[i] = stranded ? stuck[i] + dt : 0;
                if (stuck[i] > 10) { Disrupt(i); truck.Prepare(safe[i] + Vector3.up * .5f, Quaternion.LookRotation(-Vector3.ProjectOnPlane(safe[i], Vector3.up))); pilots[i].ResetFlight(); stuck[i] = 0; Recoveries++; }
            }
            TracePhysicalState();
            for (int f = 0; f < flags.Length; f++)
            {
                var flag = flags[f]; int owner = Rules.Owners[f];
                if (owner >= 0)
                {
                    bool genuine = flag.Joint != null && flag.HeldSeconds >= 1 && stations[owner].Contains(f) &&
                        Vector3.Distance(trucks[owner].body.position, stations[owner].transform.position) < 9 && trucks[owner].body.linearVelocity.magnitude < 3 &&
                        Vector3.Distance(flag.body.position, trucks[owner].transform.TransformPoint(new Vector3(0, 2.5f, .3f))) < 1.3f;
                    rest[f] = genuine ? rest[f] + dt : 0;
                    if (rest[f] > .5f && Rules.Deliver(f, owner, true))
                    {
                        flag.Release(); flag.RestoreContacts(); flag.body.isKinematic = true; flag.gameObject.SetActive(false); respawn[f] = 2.5f;
                        ConfirmScore("moon.flag." + Rules.Deliveries, owner, 1, Rules.Scores[owner], "PHYSICAL LUNAR FLAG DELIVERY", SportsScoreTier.Major, roundEnded: true, matchEnded: Rules.Complete);
                        feedback.PlayAt(5,stations[owner].transform.position,0,true);
                    }
                }
                else if (owner == -2)
                {
                    respawn[f] -= dt;
                    if (respawn[f] <= 0 && Rules.Respawn(f)) flag.ResetFlag(flagSeeds[f].position);
                }
            }
            if (Clock >= nextShower) { ScheduleShower(); nextShower = Clock + 18 + (float)random.NextDouble() * 10; }
            if (Clock >= limitSeconds) Rules.FinishAtTimeLimit();
            if (Rules.Complete) { if(!outcomeLocked){outcomeLocked=true;foreach(int winner in Rules.Leaders())ConfirmOutcome("moon.champion."+winner,winner,Rules.Scores[winner],"ACTUAL LUNAR FLAG CHAMPION","FLAGS",SportsScoreTier.MatchChampion,matchEnded:true);} foreach (var truck in trucks) truck.TickDrive(new Vector2(0, -1)); foreach (var pilot in pilots) pilot.StopJets(); }
        }
        Vector2 AiDrive(int owner, out bool fire, out bool hop)
        {
            var truck = trucks[owner]; int cargo = Rules.Carried(owner);
            int loose = Enumerable.Range(0, flags.Length).Where(f => Rules.Owners[f] == -1).OrderBy(f => Vector3.SqrMagnitude(flags[f].body.position - truck.body.position) + (f != owner % flags.Length ? 70 : 0)).DefaultIfEmpty(-1).First();
            Vector3 destination = cargo >= 0 ? stations[owner].transform.position : loose >= 0 ? flags[loose].body.position : Base(owner);
            var threat = meteors.FirstOrDefault(m => m.WarningSeconds > 0 && Vector3.Distance(truck.body.position, m.ImpactPoint) < 14);
            if (threat != null) destination = truck.body.position + Vector3.ProjectOnPlane(truck.body.position - threat.ImpactPoint, Vector3.up).normalized * 20;
            Vector3 target = destination;
            if (NavMesh.SamplePosition(truck.body.position, out NavMeshHit start, 15, NavMesh.AllAreas) && NavMesh.SamplePosition(destination, out NavMeshHit end, 12, NavMesh.AllAreas) &&
                NavMesh.CalculatePath(start.position, end.position, NavMesh.AllAreas, paths[owner]) && paths[owner].corners.Length > 1)
            {
                var corners = paths[owner].corners; int next = 1; while (next < corners.Length - 1 && Vector3.Distance(truck.body.position, corners[next]) < 8) next++; target = corners[next];
            }
            ObserveAiGoal(owner,destination,target);
            Vector3 delta = Vector3.ProjectOnPlane(target - truck.body.position, Vector3.up);
            float angle = Vector3.SignedAngle(Vector3.ProjectOnPlane(truck.transform.forward, Vector3.up), delta, Vector3.up);
            float distance = Vector3.ProjectOnPlane(destination - truck.body.position, Vector3.up).magnitude;
            float desired = distance < 12 ? Mathf.Min(cargo >= 0 ? 2.3f : 3.5f, distance * .5f) : Mathf.Abs(angle) > 55 ? 3 : 9;
            bool arrived=distance<(cargo>=0?4:1.7f);
            bool grounded = truck.tractorMotor.Grounded;
            bool obstruction=Physics.Raycast(truck.body.worldCenterOfMass + truck.transform.forward * 4, truck.transform.forward, out RaycastHit obstacle, 8, ~0, QueryTriggerInteraction.Ignore) && !obstacle.collider.transform.IsChildOf(truck.transform) && obstacle.collider.attachedRigidbody == null;
            bool craterDrop=Physics.Raycast(truck.body.position+truck.transform.forward*8+Vector3.up*5,Vector3.down,out RaycastHit ahead,16,~0,QueryTriggerInteraction.Ignore)&&ahead.collider.attachedRigidbody==null&&ahead.point.y<truck.body.position.y-1.5f;
            hop = grounded && pilots[owner].Fuel > .45f && truck.body.linearVelocity.magnitude > 3 && (obstruction||craterDrop);
            fire = !grounded && pilots[owner].Fuel > .05f;
            if(arrived)return new Vector2(0,-1); // Supported NWH handbrake, never AutoDNR reverse-as-stop.
            return SignedDrive(angle,Vector3.Dot(truck.body.linearVelocity,truck.transform.forward),desired);
        }
        static Vector2 SignedDrive(float angle,float forwardSpeed,float desired)
        {
            bool reverse=Mathf.Abs(angle)>115;float error=reverse?Mathf.DeltaAngle(180,angle):angle;
            float speed=forwardSpeed*(reverse?-1:1),effort=Mathf.Clamp((Mathf.Min(desired,reverse?3:desired)-speed)*.6f,-.8f,.8f);
            return new Vector2(Mathf.Clamp(error/(reverse?35:32),-1,1)*(reverse?-1:1),effort*(reverse?-1:1));
        }
        static void ParkAi(SportsEventTruck truck)
        {
            truck.TickDrive(Vector2.zero);var state=truck.tractorMotor.vehicle.input.states;state.handbrake=state.handbrakeRaw=1;truck.tractorMotor.vehicle.input.states=state;
        }
        public void DropFlag(int flag, string reason)
        {
            if (!Live || Rules == null || !Rules.Drop(flag)) return;
            flags[flag].Release(); rest[flag] = 0; CollisionDrops++;
        }
        public void Disrupt(int owner) { if (Rules == null) return; int flag = Rules.Carried(owner); if (flag >= 0) DropFlag(flag, "STRONG PHYSICAL COLLISION"); }
        void ScheduleShower()
        {
            foreach (var meteor in meteors)
            {
                if (meteor.Busy) continue;
                float angle = (float)random.NextDouble() * Mathf.PI * 2, radius = 18 + (float)random.NextDouble() * 48;
                Vector3 p = new Vector3(Mathf.Sin(angle) * radius, 0, Mathf.Cos(angle) * radius);
                var ground=Physics.RaycastAll(p+Vector3.up*25,Vector3.down,60,1<<0,QueryTriggerInteraction.Ignore).Where(h=>h.collider.attachedRigidbody==null).OrderBy(h=>h.distance).ToArray();if(ground.Length>0)p.y=ground[0].point.y;
                meteor.Warn(p);
            }
            feedback.Play(4, 0, true);
        }
        public void MeteorImpact(Vector3 position)
        {
            if (!Live) return; PhysicalMeteorImpacts++;
            foreach (var pilot in pilots) foreach (var body in pilot.Assembly)
                if (!body.isKinematic && Vector3.Distance(body.worldCenterOfMass, position) < 10) body.AddExplosionForce(body.mass * 3, position, 10, .4f, ForceMode.Impulse);
            foreach (var flag in flags) if (!flag.body.isKinematic) flag.body.AddExplosionForce(flag.body.mass * 3, position, 10, .4f, ForceMode.Impulse);
            feedback.PlayAt(3,position,8);
        }
        protected override void Present(float dt)
        {
            if (drivingViews != null) drivingViews.Tick(dt);
            if (broadcast != null && (drivingViews == null || drivingViews.NeedsArena)) broadcast.Pose(Vector3.up * 3, new Vector3(0, 115, -115), 60);
            if (flags != null) foreach (var flag in flags) if (flag.banner != null) flag.banner.localRotation = Quaternion.Euler(0, Live ? Mathf.Sin(Clock * 2 + flag.id) * 7 : 0, 0);
        }
        protected override void StopGame()
        {
            if (flags != null) foreach (var flag in flags) if (flag != null) { flag.Release(); flag.RestoreContacts(); }
            if (meteors != null) foreach (var meteor in meteors) if(meteor!=null) meteor.ResetMeteor();
            if (pilots != null) foreach (var pilot in pilots) if (pilot != null) pilot.Restore();
            if (trucks != null) foreach (var truck in trucks) if (truck != null) truck.Power(false);
        }
        protected override void StopSounds() { if (trucks != null) foreach (var truck in trucks) if (truck != null) truck.StopSound(); }
    }
}
