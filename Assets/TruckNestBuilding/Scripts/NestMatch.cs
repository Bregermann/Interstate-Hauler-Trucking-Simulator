using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using LWS.TruckBowling;
using LWS.TruckSports;
namespace LWS.TruckNestBuilding
{
    public sealed class NestMatch : SportsEventSession, ISportsWorkshopState
    {
        public NestBuilder[] builders;
        public NestBuildSite[] sites;
        public NestMaterial[] materials;
        public SportsDrivingViews drivingViews;
        public SportsEventCamera broadcast;
        public GameObject finishWorkshop;
        public readonly NestRules rules = new NestRules();
        public float Clock { get; private set; }
        public string SaveStatus { get; private set; } = "";
        public event Action<NestMatch> WorkshopSaveRequested;
        readonly int[] announced = new int[4];
        float finishing;
        bool resumePending;
        Workshop restored;
        public override string Title => "TRUCK NEST BUILDING";
        public override int MinimumPlayers => 1;
        public override int MaximumPlayers => 4;
        public override int OptionCount => 2;
        public override string OptionLabel => option == 0 ? "COOPERATIVE FAMILY NEST" : "NEST QUALITY CHALLENGE";
        public override bool SupportsSplitAIViews => true;
        public override bool MultiViewHUD => drivingViews != null && !drivingViews.NeedsArena;
        public override bool CompactBroadcastStatus => true;
        public override string Help => "BUILD A REAL HOME\n\nDrive gently to marked branch, stick, leaf, moss or decoration piles. Stop and tap ACTION to grasp the nearest loose material. The physical gripper can break under hard impacts. Tap again to enter PLACEMENT, braking the truck. Move left/right and forward/back to position the hand. HOLD ACTION while placing to change height (forward/back) and rotation (left/right). A short tap releases the material.\n\nBuild the crossing timbers from the ground up, then leaves and protective moss. Pieces must actually rest on rooted supports before they interlock. Broken or unsupported connections stop nest stability. Decorations reward creativity.\n\nA stable complete nest receives a real egg. Protect it on the lining until it hatches into wiggling baby trucks. Cooperative players share one untimed nest, ending with a family or FINISH WORKSHOP in Pause. The six-minute quality challenge gives each player a nest. AI physically gathers, carries and places the same materials.\n\nPause to request a workshop save through the shared save adapter. Release held materials before saving. Restart resets the workshop; no pickups count as construction.";
        public override string Header => rules.Cooperative ? "FAMILY NEST · COOPERATIVE WORKSHOP" : "NEST CHALLENGE · " + Mathf.Max(0, NestRules.SessionLimit - Clock).ToString("0") + " SEC";
        public override string Status => SaveStatus.Length > 0 && Paused ? SaveStatus : rules.Complete ? "WELCOME, LITTLE HAULERS!" : "PLACE · SUPPORT · PROTECT · HATCH";
        string SiteText(int index) => sites[index].RootedCount + "/12 ROOTED · " + (rules.hatched[index] ? "FAMILY HATCHED" : rules.received[index] ? "EGG " + rules.incubation[index].ToString("0.0") + "/5s STABLE" : "BUILD & LINE THE NEST");
        public override string Standings { get { var b = new StringBuilder(); for (int i = 0; i < rules.Sites; i++) b.AppendLine((rules.Cooperative ? "OUR NEST" : setup.players[i].name) + " · " + rules.quality[i] + " PTS\n" + SiteText(i)); return b.ToString(); } }
        public override string Results { get { var b = new StringBuilder(rules.Cooperative ? "OUR FAMILY HOME\nCOOPERATIVE WORKSHOP\n\n" : "FOREST NEST QUALITY\n\n"); for (int i = 0; i < rules.Sites; i++) b.AppendLine((rules.Cooperative ? "TOGETHER" : "#" + rules.Place(i) + " " + setup.players[i].name) + " · " + rules.quality[i] + " PTS · " + SiteText(i)); return b.ToString(); } }
        public override string ViewText(int view) { int owner = drivingViews.rally.owners[view], site = rules.Cooperative ? 0 : owner; return setup.players[owner].name + " · " + builders[owner].Mode + "\n" + (builders[owner].Carried != null ? builders[owner].Carried.kind.ToString().ToUpperInvariant() : "DRIVE TO A MATERIAL PILE") + "\n" + SiteText(site); }
        public override bool TryReadScoreCard(int card, out string value, out string detail) { int site = rules.Cooperative ? 0 : card; value = rules.quality[site] + " PTS"; detail = SiteText(site); return true; }
        public override void DescribeBroadcast(SportsBroadcastAction action, SportsBroadcastFraming framing) { action.primary = 0; action.preferHuman = true; action.reason = "PHYSICAL NEST CONSTRUCTION"; action.Objective(sites[0].transform.position + Vector3.up * 1.5f, 4); }
        public bool ClaimAvailableFor(NestBuilder requester)
        {
            if (!rules.Cooperative) return true;
            // Waiting AI companions receive the next supported layer before a
            // more-experienced carrier starts another pickup. Humans place
            // freely; they never block AI claim rotation by not requesting one.
            for (int i = 0; i < setup.count; i++)
                if (setup.players[i].source == BowlingSource.AI && builders[i] != requester && builders[i].GoalSocket < 0 && builders[i].PhysicalDrops < requester.PhysicalDrops) return false;
            return true;
        }
        public bool ShouldRecoverMissingCore(NestBuilder requester,NestBuildSite site)
        {
            if(requester.GoalSocket<0||requester.GoalSocket>=10||site.Prerequisites(requester.GoalSocket)||site.CollectionAncestorsCovered(requester.GoalSocket,builders,setup.count))return false;
            // One deterministic worker is enough. A player or an actively
            // completing prerequisite keeps authority and prevents this branch.
            NestBuilder first=null;
            for(int i=0;i<setup.count;i++)
            {
                var builder=builders[i];
                if(setup.players[i].source!=BowlingSource.AI||builder.GoalSite!=site||builder.GoalSocket<0||builder.Carried==null||site.Prerequisites(builder.GoalSocket))return false;
                if(first==null)first=builder;
            }
            return first==requester;
        }
        protected override void BeginGame()
        {
            if (resumePending) { resumePending = false; SetActivePhysics(); ApplyBodyVelocities(restored); drivingViews.Layout(); return; }
            Clock = finishing = 0; SaveStatus = ""; rules.Begin(setup.count, option == 0); Array.Clear(announced, 0, 4);
            foreach (var builder in builders) builder.ReleaseAll(); foreach (var site in sites) site.Clear(); foreach (var item in materials) item.ResetPiece();
            for (int i = 0; i < sites.Length; i++)
            {
                sites[i].Prepare(i < rules.Sites, rules.Cooperative);
                if (i < rules.Sites) sites[i].family.Prepare(setup.players[rules.Cooperative ? 0 : i].participantId, setup.players[rules.Cooperative ? 0 : i].color);
            }
            for (int i = 0; i < builders.Length; i++) builders[i].Prepare(i < setup.count);
            drivingViews.Layout(); Gate();
        }
        protected override void Simulate(float dt)
        {
            Clock += dt;
            if (finishing <= 0)
                for (int i = 0; i < setup.count; i++) builders[i].Tick(commands[i], setup.players[i].released && ActionHeld(i), setup.players[i].source == BowlingSource.AI, dt);
            foreach (var item in materials) item.Tick(dt);
            for (int i = 0; i < rules.Sites; i++)
            {
                sites[i].Tick(materials, dt); sites[i].family.Tick(dt);
                var milestone = rules.Observe(i, sites[i].CoreReady, sites[i].family.EggStable, sites[i].RootedCount, sites[i].Varieties, sites[i].Ornaments, dt);
                if (rules.quality[i] != announced[i]) { ConfirmScore("quality-" + i + "-" + Time.frameCount, rules.Cooperative ? 0 : i, rules.quality[i] - announced[i], rules.quality[i], "ROOTED NEST QUALITY", SportsScoreTier.Small); announced[i] = rules.quality[i]; }
                if (milestone == NestMilestone.ReceiveEgg) sites[i].family.ReceiveEgg();
                if (milestone == NestMilestone.Hatch) { sites[i].family.Hatch(); ConfirmOutcome("family-" + i, rules.Cooperative ? 0 : i, 2, "BABY TRUCK FAMILY HATCHED", "BABIES", SportsScoreTier.Major); }
            }
            // Session runs at-100, NWH wheel casts at100. Refresh after real
            // support/lock transitions so rooted pieces are eligible this step.
            for (int i = 0; i < setup.count; i++) builders[i].RefreshMaterialGroundExclusions();
            if (rules.Complete || !rules.Cooperative && Clock >= NestRules.SessionLimit)
            {
                if (finishing <= 0) { finishing = .001f; foreach (var builder in builders) builder.Stop(); }
                finishing += dt;
                if (finishing >= 4)
                {
                    if (!rules.Cooperative) for (int i = 0; i < rules.Sites; i++) if (rules.Place(i) == 1 && rules.quality[i] > 0) ConfirmOutcome("nest-winner-" + i, i, rules.quality[i], "FOREST HOME QUALITY", "PTS", SportsScoreTier.MatchChampion, matchEnded: true);
                    EndMatch();
                }
            }
        }
        public override void ExtraAction(string key)
        {
            if (key == "drivingview") drivingViews.Cycle();
            else if (key == "nest-save")
            {
                if (WorkshopSaveRequested == null) SaveStatus = "Shared workshop save adapter is not connected.";
                else { try { CaptureWorkshopJson(); WorkshopSaveRequested(this); SaveStatus = "Workshop save requested."; } catch (InvalidOperationException e) { SaveStatus = e.Message; } }
            }
            else if (key == "nest-new") { resumePending = false; StartMatch(); }
            else if (key == "nest-finish" && Running && rules.Cooperative) { if (Paused) Resume(); EndMatch(); }
        }
        protected override void Present(float dt) { if (finishWorkshop != null) finishWorkshop.SetActive(Running && rules.Cooperative); if (drivingViews == null) return; drivingViews.Tick(dt); if (drivingViews.NeedsArena && sites != null) broadcast.Pose(sites[0].transform.position, new Vector3(27, 25, -42), 60); }
        protected override void StopGame()
        {
            if (builders != null) foreach (var builder in builders) builder.Stop();
            if (materials != null) foreach (var item in materials) { if (!item.body.isKinematic) item.body.linearVelocity = item.body.angularVelocity = Vector3.zero; item.body.isKinematic = true; }
            foreach (var site in sites) { if (!site.family.eggBody.isKinematic) site.family.eggBody.linearVelocity = site.family.eggBody.angularVelocity = Vector3.zero; site.family.eggBody.isKinematic = true; }
        }
        protected override void StopSounds() { if (sites != null) foreach (var site in sites) site.family.StopSound(); }
        void SetActivePhysics()
        {
            foreach (var item in materials) item.body.isKinematic = false;
            for (int i = 0; i < builders.Length; i++) { builders[i].ResumeWorkshop(i < setup.count); if (i >= setup.count) continue; builders[i].truck.Power(true); builders[i].truck.tractorMotor.Configure(); builders[i].truck.body.isKinematic = false; foreach (var body in builders[i].originalAttachedBodies) body.isKinematic = false; }
            foreach (var site in sites) site.family.eggBody.isKinematic = !site.family.EggPresent;
        }

        [Serializable] public sealed class BodyState { public Vector3 position, velocity, angular; public Quaternion rotation; }
        [Serializable] public sealed class PieceState { public string id, parent, owner; public int site = -1, socket = -1; public BodyState pose; }
        [Serializable] public sealed class FamilyState { public bool received, hatched; public float incubation; public BodyState egg; }
        [Serializable] public sealed class TruckState { public BodyState body; public BodyState[] attached; }
        [Serializable] public sealed class Workshop { public int version, count, format; public float clock; public string[] profiles; public PieceState[] pieces; public FamilyState[] families; public TruckState[] trucks; }
        static BodyState Read(Rigidbody b) => new BodyState { position = b.position, rotation = b.rotation, velocity = b.linearVelocity, angular = b.angularVelocity };
        static void Write(Rigidbody b, BodyState p)
        {
            b.isKinematic = true; b.position = p.position; b.rotation = p.rotation;
            // Restore is already validated and stopped/paused. Publish the same
            // saved Transform pose before Lock creates native joint reference
            // frames; Rigidbody setters alone leave an interpolated Transform
            // at its previous depot pose until the next physics step.
            b.transform.SetPositionAndRotation(p.position, p.rotation);
        }
        static bool Finite(Vector3 p) => float.IsFinite(p.x) && float.IsFinite(p.y) && float.IsFinite(p.z) && p.sqrMagnitude < 1000000;
        static bool Valid(BodyState p) => p != null && Finite(p.position) && Finite(p.velocity) && Finite(p.angular) && float.IsFinite(p.rotation.x) && float.IsFinite(p.rotation.y) && float.IsFinite(p.rotation.z) && float.IsFinite(p.rotation.w) && Mathf.Abs(Quaternion.Dot(p.rotation, p.rotation) - 1) < .01f;
        public string CaptureWorkshopJson()
        {
            if (Live) throw new InvalidOperationException("Pause the workshop before saving.");
            if (builders.Any(b => b.Carried != null && b.Carried.Grip != null || b.Selected != null)) throw new InvalidOperationException("Release held materials before saving.");
            return JsonUtility.ToJson(new Workshop
            {
                version = 1, count = setup.count, format = option, clock = Clock, profiles = setup.players.Take(setup.count).Select(p => p.participantId).ToArray(),
                pieces = materials.Select(p => new PieceState { id = p.pieceId, owner = p.LastOwner >= 0 && p.LastOwner < setup.count ? setup.players[p.LastOwner].participantId : "", site = p.Site == null ? -1 : p.Site.index, socket = p.Socket, parent = p.Interlock == null ? "" : p.Interlock.connectedBody == p.Site.body ? "platform:" + p.Site.index : p.Interlock.connectedBody.GetComponent<NestMaterial>().pieceId, pose = Read(p.body) }).ToArray(),
                families = sites.Select(s => new FamilyState { received = s.family.EggPresent, hatched = s.family.Hatched, incubation = rules.incubation[s.index], egg = Read(s.family.eggBody) }).ToArray(),
                trucks = builders.Take(setup.count).Select(b => new TruckState { body = Read(b.truck.body), attached = b.originalAttachedBodies.Select(Read).ToArray() }).ToArray()
            });
        }
        public bool TryRestoreWorkshopJson(string json, out string reason)
        {
            reason = ""; if (Live) { reason = "Pause or stop before restoring a workshop."; return false; }
            Workshop data; try { data = JsonUtility.FromJson<Workshop>(json); } catch (Exception) { reason = "Corrupt workshop JSON."; return false; }
            if (data == null || data.version != 1 || data.count != setup.count || data.format != option || data.profiles == null || !data.profiles.SequenceEqual(setup.players.Take(setup.count).Select(p => p.participantId)) || !float.IsFinite(data.clock) || data.clock < 0 || data.clock > (option == 0 ? 86400 : NestRules.SessionLimit + 5))
            { reason = "Workshop version, participants, format or clock does not match this setup."; return false; }
            if (data.pieces == null || data.pieces.Length != materials.Length || data.families == null || data.families.Length != sites.Length || data.trucks == null || data.trucks.Length != setup.count)
            { reason = "Workshop must contain the exact authored piece/family/truck roster."; return false; }
            var ids = materials.ToDictionary(p => p.pieceId); var entries = new Dictionary<string, PieceState>(); var occupied = new HashSet<string>(); int activeSites = option == 0 ? 1 : setup.count;
            foreach (var p in data.pieces)
            {
                if (p == null || string.IsNullOrEmpty(p.id) || !ids.ContainsKey(p.id) || entries.ContainsKey(p.id) || !Valid(p.pose) || !string.IsNullOrEmpty(p.owner) && !data.profiles.Contains(p.owner)) { reason = "Unknown, duplicate or invalid piece/owner pose."; return false; }
                entries.Add(p.id, p);
                if (p.site < -1 || p.site >= activeSites || p.site < 0 && (p.socket != -1 || !string.IsNullOrEmpty(p.parent))) { reason = "Invalid support binding."; return false; }
                if (p.site >= 0)
                {
                    if (p.socket < 0 || p.socket >= 12 || !occupied.Add(p.site + ":" + p.socket) || ids[p.id].kind != NestBuildSite.Kind(p.socket)) { reason = "Placed piece does not match its authored nest socket."; return false; }
                    var siteHome = option == 0 ? new Vector3(0, sites[p.site].competitiveHome.y, sites[p.site].competitiveHome.z) : sites[p.site].competitiveHome;
                    var target = siteHome + sites[p.site].transform.rotation * NestBuildSite.Position(p.socket);
                    if (Vector3.Distance(target, p.pose.position) > .75f || ids[p.id].kind != NestMaterialKind.Decoration && Mathf.Abs(Vector3.Dot(p.pose.rotation * Vector3.forward, sites[p.site].TargetRotation(p.socket) * Vector3.forward)) <= .91f) { reason = "Saved piece is outside its known physical placement envelope."; return false; }
                }
            }
            foreach (var p in data.pieces) if (p.site >= 0)
            {
                var cursor = p; bool rooted = false;
                for (int step = 0; step <= 12; step++)
                {
                    if (cursor.parent == "platform:" + cursor.site) { rooted = NestBuildSite.SupportAllowed(cursor.socket, -1); break; }
                    if (string.IsNullOrEmpty(cursor.parent) || !entries.TryGetValue(cursor.parent, out var parent) || parent.site != cursor.site || !NestBuildSite.SupportAllowed(cursor.socket, parent.socket)) break;
                    cursor = parent;
                }
                if (!rooted) { reason = "Unsupported, cyclic or incompatible saved structure."; return false; }
            }
            for (int i = 0; i < data.families.Length; i++)
            {
                var f = data.families[i]; if (f == null || !Valid(f.egg) || !float.IsFinite(f.incubation) || f.incubation < 0 || f.incubation > 6) { reason = "Invalid family pose/incubation."; return false; }
                if (f.hatched && (f.received || f.incubation < 5) || !f.hatched && !f.received && f.incubation > 0) { reason = "Family incubation and hatching state is inconsistent."; return false; }
                if ((f.hatched || f.received) && (i >= activeSites || Enumerable.Range(0, 10).Any(socket => !occupied.Contains(i + ":" + socket)))) { reason = "An egg/family requires the complete saved supported lining."; return false; }
            }
            for (int i = 0; i < data.trucks.Length; i++) if (data.trucks[i] == null || !Valid(data.trucks[i].body) || data.trucks[i].attached == null || data.trucks[i].attached.Length != builders[i].originalAttachedBodies.Length || data.trucks[i].attached.Any(p => !Valid(p))) { reason = "Invalid original truck/cab pose roster."; return false; }
            // All validation precedes mutation. The lead's Pixel Crushers adapter
            // owns persistence/scene/profile coordination; this is semantic state.
            foreach (var builder in builders) builder.ReleaseAll(); rules.Begin(setup.count, option == 0);
            for (int i = 0; i < sites.Length; i++) { sites[i].Prepare(i < activeSites, option == 0); if (i < activeSites) sites[i].family.Prepare(data.profiles[option == 0 ? 0 : i], setup.players[option == 0 ? 0 : i].color); }
            foreach (var p in data.pieces) { var item = ids[p.id]; item.Unlock(); Write(item.body, p.pose); item.LastOwner = Array.IndexOf(data.profiles, p.owner); }
            foreach (var p in data.pieces.Where(p => p.site >= 0).OrderBy(p => p.socket)) { ids[p.id].body.isKinematic = false; sites[p.site].RestorePlacement(ids[p.id], p.socket, p.parent.StartsWith("platform:", StringComparison.Ordinal) ? sites[p.site].body : ids[p.parent].body); }
            for (int i = 0; i < sites.Length; i++) { var f = data.families[i]; sites[i].family.RestoreEgg(f.egg.position, f.egg.rotation, f.received, f.hatched); rules.received[i] = f.received || f.hatched; rules.hatched[i] = f.hatched; rules.incubation[i] = f.incubation; }
            for (int i = 0; i < setup.count; i++) { Write(builders[i].truck.body, data.trucks[i].body); for (int j = 0; j < data.trucks[i].attached.Length; j++) Write(builders[i].originalAttachedBodies[j], data.trucks[i].attached[j]); }
            Clock = data.clock; finishing = 0; restored = data; resumePending = !Running;
            for (int i = 0; i < rules.Sites; i++) { rules.quality[i] = sites[i].RootedCount * 20 + Mathf.Max(0, sites[i].Varieties - 1) * 10 + sites[i].Ornaments * 15 + (rules.hatched[i] ? 150 : 0); announced[i] = rules.quality[i]; }
            if (Running) { SetActivePhysics(); ApplyBodyVelocities(data); }
            else { foreach (var item in materials) item.body.isKinematic = true; foreach (var site in sites) site.family.eggBody.isKinematic = true; }
            SaveStatus = "Workshop restored at a safe boundary."; return true;
        }
        void ApplyBodyVelocities(Workshop data)
        {
            foreach (var p in data.pieces) { var item = materials.First(m => m.pieceId == p.id); if (!item.body.isKinematic) { item.body.linearVelocity = p.pose.velocity; item.body.angularVelocity = p.pose.angular; } }
            for (int i = 0; i < setup.count; i++) { var b = builders[i].truck.body; if (!b.isKinematic) { b.linearVelocity = data.trucks[i].body.velocity; b.angularVelocity = data.trucks[i].body.angular; } }
            for (int i = 0; i < setup.count; i++) for (int j = 0; j < builders[i].originalAttachedBodies.Length; j++) { var b = builders[i].originalAttachedBodies[j]; if (!b.isKinematic) { b.linearVelocity = data.trucks[i].attached[j].velocity; b.angularVelocity = data.trucks[i].attached[j].angular; } }
            for (int i = 0; i < sites.Length; i++) if (!sites[i].family.eggBody.isKinematic) { sites[i].family.eggBody.linearVelocity = data.families[i].egg.velocity; sites[i].family.eggBody.angularVelocity = data.families[i].egg.angular; }
        }
    }
}
