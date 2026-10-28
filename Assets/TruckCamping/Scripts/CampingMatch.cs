using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using LWS.TruckBowling;
using LWS.TruckSports;
namespace LWS.TruckCamping
{
    public class CampingMatch : SportsEventSession, ISportsWorkshopState
    {
        public CampingWilderness world;
        public CampExplorer[] explorers;
        public SportsDrivingViews drivingViews;
        public SportsEventCamera broadcast;
        public SportsWorkshopPersistence persistence;
        public GameObject photoExit;
        public float WildernessSeconds { get; private set; }
        public bool PhotoActive { get; private set; }
        public bool AtWorkshopStartBoundary { get; private set; }
        public int PhotoOwner { get; private set; }
        public string WorkshopStatus { get; private set; } = "";
        public bool SurvivalLite => option == 1;
        public override string Title => "TRUCK CAMPING · CAMP HAUL";
        public override int MinimumPlayers => 1;
        public override int MaximumPlayers => 4;
        public override int OptionCount => 2;
        public override string OptionLabel => SurvivalLite ? "GENTLE SURVIVAL-LITE" : "RELAXED COOPERATIVE EXPLORATION";
        public override bool BroadcastStandingsVisible => false;
        public override bool CompactBroadcastStatus => true;
        public override bool CompactViewportHud => true;
        public override Vector2 ViewportCardAnchor(int view, Rect viewport) => new Vector2(0, 1);
        public override bool SupportsSplitAIViews => true;
        public override bool MultiViewHUD => drivingViews != null && !drivingViews.NeedsArena;
        public override bool BroadcastFramingApplicable => !PhotoActive;
        public override string Header => "CAMP HAUL · YOUR SHARED WILDERNESS";
        public override string Status => PhotoActive ? "SCENIC PHOTO · MOVE TO ORBIT · ACTION TO CAPTURE" : "EXPLORE · GATHER · COOK · FISH · REST";
        public override string Help => "A WILDERNESS TO SHARE\n\nNo race timer, score or winner. Drive the mountain trails to discover lakes, caves, cabins and overlooks. Stop, tap ACTION, then use left/right to browse nearby activities and ACTION to choose.\n\nGather visible sticks, firewood and forage. A campfire needs3sticks and2firewood. Fresh fish or forage cooks into2warm meals. At the lake, cast and tap ACTION when the bobber dips; missing a bite has no penalty.\n\nDrive gently up a hammock approach until the actual wheels settle on its spring-supported deck, then choose NAP. Cabins and rest clearings also allow naps. WAKE returns to exploring.\n\nPHOTO opens a shared scenic camera. The selected owner's controls orbit, ACTION captures a real PNG, and RETURN TO WALK ends the photo. Pause/help freeze the session. Save or restore a paused expedition through the existing workshop adapter.\n\nSurvival-lite is optional, forgiving hunger/food. No starvation, collision harvesting or compulsory leaderboard. Companions actually drive, gather, cook, fish and relax.";
        public override string Standings => string.Join("\n", explorers.Take(setup.count).Select((e, i) => setup.players[i].name + " · " + e.Activity));
        public override string Results
        {
            get { var b = new StringBuilder("CAMP HAUL JOURNAL\nA SHARED TRIP · NO WINNER OR POINTS\n\n"); for (int i = 0; i < setup.count; i++) { var j = explorers[i].Journal; b.AppendLine($"{setup.players[i].name} · {CountBits(j.discovered)} PLACES · {j.photos} PHOTOS · {j.mealsCooked} MEALS · {j.fishCaught} FISH"); } return b.ToString(); }
        }
        public override string ViewText(int view)
        {
            int owner = drivingViews.rally.owners[view]; var e = explorers[owner];
            return setup.players[owner].name + " · " + e.Activity.ToString().ToUpperInvariant() + "\n" + (e.ContextOpen ? "< " + ChoiceLabel(e.Choice) + " > · ACTION" : e.Hint);
        }
        // Reuse the collection's owned compact card, rather than allocating a
        // large mostly empty plate in every wilderness viewport. No score is
        // introduced into this cooperative mode.
        protected bool TryViewportExplorer(int view, out int owner)
        {
            owner = -1;
            if (drivingViews == null || drivingViews.rally == null || view < 0 ||
                view >= drivingViews.rally.Humans || view >= drivingViews.rally.owners.Length) return false;
            owner = drivingViews.rally.owners[view];
            return owner >= 0 && owner < setup.count && owner < explorers.Length && explorers[owner] != null;
        }
        public override bool TryReadViewportCard(int view, out int participant, out string score, out string detail)
        {
            score = detail = null;
            if (!TryViewportExplorer(view, out participant)) return false;
            score = explorers[participant].Activity.ToString().ToUpperInvariant();
            return true;
        }
        public override string ViewportContextPrompt(int view)
        {
            if (!TryViewportExplorer(view, out int owner)) return null;
            var explorer = explorers[owner];
            return explorer.ContextOpen ? "< " + ChoiceLabel(explorer.Choice) + " >\n" + Controls(owner) : explorer.Hint;
        }
        public override bool TryReadScoreCard(int card, out string value, out string detail) { value = "EXPLORING"; detail = explorers[card].Activity.ToString(); return false; }
        public override void DescribeBroadcast(SportsBroadcastAction action, SportsBroadcastFraming framing) { action.primary = 0; action.preferHuman = true; action.reason = "COOPERATIVE WILDERNESS EXPLORATION"; var near = world.Nearest(explorers[0].truck.body.position); if (near != null) action.Objective(near.activity.position, 5); }
        static int CountBits(int bits) { int count = 0; while (bits != 0) { count += bits & 1; bits >>= 1; } return count; }
        public virtual int ChoiceCount => 11;
        public virtual Vector2 FilterDrive(int owner, Vector2 requested) => requested;
        public virtual bool TryCompanionDrive(int owner, float dt, out Vector2 requested) { requested = Vector2.zero; return false; }
        public virtual bool CompanionOwnsInteraction(int owner)=>false;
        public virtual string ChoiceLabel(int choice) => new[] { "GATHER NEARBY MATERIAL", "BUILD CAMPFIRE", "ADD FIREWOOD", "COOK FRESH MEAL", "EAT", "CAST AT LAKE", "RELAX", "NAP", "WAKE / EXPLORE", "SCENIC PHOTO", "CLOSE" }[Mathf.Clamp(choice, 0, 10)];
        public virtual string Interact(int owner, int choice)
        {
            var e = explorers[owner]; var j = e.Journal; var p = e.truck.body.position;
            if (choice == 0)
            {
                var part = world.resources.Where(r => r.Available).OrderBy(r => (r.body.position - p).sqrMagnitude).FirstOrDefault();
                return e.BeginGather(part) ? "GATHERING REAL MATERIAL" : "STOP WITHIN10m OF A VISIBLE MATERIAL";
            }
            var site = world.campsites.OrderBy(c => (c.landmark.parking.position - p).sqrMagnitude).First();
            if (choice == 1) return site.Nearby(p) && site.state.Build(j) ? "CAMPFIRE BUILT" : "CAMPFIRE:3STICKS +2FIREWOOD AT THE CAMP";
            if (choice == 2) return site.Nearby(p) && site.state.AddFuel(j) ? "FIRE REPLENISHED" : "BRING FIREWOOD TO THE CAMP";
            if (choice == 3) return e.BeginCooking(site) ? "GENTLE COOKING ·9SECONDS" : "LIT CAMPFIRE +FISH OR FORAGE REQUIRED";
            if (choice == 4) return j.Eat() ? "ENJOY YOUR MEAL" : "GATHER FORAGE OR COOK FIRST";
            if (choice == 5) return e.BeginFishing() ? "CAST · WATCH FOR THE BOBBER DIP" : "PARK AT THE LAKE FISHING SIGN";
            if (choice == 6) { e.Begin(CampActivity.Resting); return "RESTING · CHOOSE WAKE WHEN READY"; }
            if (choice == 7)
            {
                bool supported = world.hammocks.Any(h => h.Supports(e.truck));
                var place = world.Nearest(p); bool clearing = place != null && (place.kind == CampPlaceKind.Cabin || place.kind == CampPlaceKind.RestClearing) && Vector3.Distance(p, place.parking.position) < 15;
                if (!supported && !clearing) return "SETTLE ON A HAMMOCK OR PARK AT A CABIN / REST CLEARING";
                e.Begin(CampActivity.Sleeping); j.naps++; return "SLEEPING · CHOOSE WAKE WHEN READY";
            }
            if (choice == 8) { e.FinishActivity("WELCOME BACK TO THE TRAIL"); return "EXPLORING"; }
            if (choice == 9) { BeginPhoto(owner); return "PHOTO MODE"; }
            return "";
        }
        bool resumePending, hasWorkshop;
        Workshop restored;
        float photoYaw, photoPitch = 20, photoDistance = 32;
        protected override void BeginGame()
        {
            world.session = this;
            if (!resumePending) { WildernessSeconds = 0; restored = null; world.ResetWorkshop(); }
            for (int i = 0; i < explorers.Length; i++) explorers[i].Prepare(i < setup.count, resumePending && i < setup.count ? restored.journals[i] : new CampJournal(setup.players[i].participantId), !resumePending);
            hasWorkshop = true; resumePending = false; if (restored != null) ApplyVelocities(restored);
            drivingViews.Layout(); Gate(); AtWorkshopStartBoundary = true;
            try { BeginActivitySession(); } finally { AtWorkshopStartBoundary = false; }
        }
        protected virtual void BeginActivitySession() { }
        protected override void Simulate(float dt)
        {
            WildernessSeconds += dt; world.Tick(dt);
            if (PhotoActive)
            {
                photoYaw += commands[PhotoOwner].x * dt * 35; photoPitch = Mathf.Clamp(photoPitch + commands[PhotoOwner].y * dt * 20, 8, 65);
                for (int i = 0; i < setup.count; i++) { explorers[i].truck.TickDrive(Vector2.zero); explorers[i].truck.tractorMotor.vehicle.input.states.handbrake = explorers[i].truck.tractorMotor.vehicle.input.states.handbrakeRaw = 1; }
                if (actions[PhotoOwner]) CapturePhoto(); return;
            }
            for (int i = 0; i < setup.count; i++) explorers[i].Tick(commands[i], actions[i], setup.players[i].source == BowlingSource.AI, dt);
            TickActivities(dt);
        }
        protected virtual void TickActivities(float dt) { }
        public virtual Vector3 PhotoFocus(int owner) => explorers[owner].truck.body.worldCenterOfMass;
        public void BeginPhoto(int owner)
        {
            PhotoOwner = Mathf.Clamp(owner, 0, setup.count - 1); PhotoActive = true; photoYaw = explorers[PhotoOwner].truck.transform.eulerAngles.y;
            drivingViews.rally.ReplayView(PhotoFocus(PhotoOwner), 0); Gate(); broadcast.broadcastFraming?.Release();
        }
        public void EndPhoto() { if (!PhotoActive) return; PhotoActive = false; drivingViews.Layout(); broadcast.broadcastFraming?.Release(); Gate(); }
        void CapturePhoto()
        {
            string folder = Path.Combine(Application.persistentDataPath, "TruckSports", "CampPhotos"); Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + ".png"); ScreenCapture.CaptureScreenshot(path); explorers[PhotoOwner].Journal.photos++; WorkshopStatus = "PHOTO CAPTURE REQUESTED · " + Path.GetFileName(path);
        }
        protected override void Present(float dt)
        {
            if (photoExit != null) photoExit.SetActive(PhotoActive && Live);
            if (PhotoActive)
            {
                var offset = Quaternion.Euler(photoPitch, photoYaw, 0) * Vector3.back * photoDistance;
                broadcast.Pose(PhotoFocus(PhotoOwner), offset, 58); return;
            }
            drivingViews.Tick(dt);
            if (drivingViews.NeedsArena) broadcast.Pose(explorers[0].truck.body.worldCenterOfMass + explorers[0].truck.transform.forward * 8, new Vector3(0, 17, -28), 62);
            PresentActivities(dt);
        }
        protected virtual void PresentActivities(float dt) { }
        public override void ExtraAction(string key)
        {
            if (key == "drivingview") drivingViews.Cycle();
            else if (key == "camp-photo-exit") EndPhoto();
            else if (key == "camp-finish" && Running) { EndPhoto(); if (Paused) Resume(); EndMatch(); }
            else if (key == "camp-new") { resumePending = false; restored = null; StartMatch(); }
            else if (key == "camp-save" && persistence != null) { persistence.RequestSave(); WorkshopStatus = persistence.Status; }
            else if (key == "camp-load" && persistence != null) { persistence.RequestLoad(); WorkshopStatus = persistence.Status; }
        }
        protected override void StopGame() { EndPhoto(); if (explorers != null) foreach (var explorer in explorers) explorer.Stop(); StopActivities(); }
        protected virtual void StopActivities() { }
        protected override void StopSounds() { if (world != null && world.campsites != null) foreach (var site in world.campsites) if (site.crackle != null) site.crackle.Stop(); }

        [Serializable] public sealed class Pose { public Vector3 position, velocity, angular; public Quaternion rotation; }
        [Serializable] public sealed class TruckPose { public Pose body; public Pose[] attached; }
        [Serializable] public sealed class ResourceState { public string id; public float regrowth; public Pose pose; }
        [Serializable] public sealed class Workshop { public int version, count, option; public string[] profiles; public float clock; public CampJournal[] journals; public CampfireState[] fires; public ResourceState[] resources; public TruckPose[] trucks; public string activity; }
        static Pose Read(Rigidbody body) => new Pose { position = body.position, rotation = body.rotation, velocity = body.linearVelocity, angular = body.angularVelocity };
        static bool Finite(Vector3 p) => float.IsFinite(p.x) && float.IsFinite(p.y) && float.IsFinite(p.z) && p.sqrMagnitude < 16000000;
        static bool Valid(Pose p) => p != null && Finite(p.position) && Finite(p.velocity) && Finite(p.angular) && float.IsFinite(p.rotation.x) && float.IsFinite(p.rotation.y) && float.IsFinite(p.rotation.z) && float.IsFinite(p.rotation.w) && Mathf.Abs(Quaternion.Dot(p.rotation, p.rotation) - 1) < .01f;
        protected virtual string CaptureActivityJson() => "";
        protected virtual bool ValidateActivityJson(string json, out string reason) { reason = ""; return string.IsNullOrEmpty(json); }
        protected virtual void RestoreActivityJson(string json) { }
        public string CaptureWorkshopJson()
        {
            if (Live || PhotoActive || !hasWorkshop) throw new InvalidOperationException("Pause a started expedition and close photo mode before saving.");
            if (explorers.Take(setup.count).Any(e => e.Activity == CampActivity.Gathering || e.Activity == CampActivity.Cooking || e.Activity == CampActivity.Fishing)) throw new InvalidOperationException("Finish or leave the active gathering/cooking/fishing interaction before saving.");
            return JsonUtility.ToJson(new Workshop { version = 1, count = setup.count, option = option, clock = WildernessSeconds, profiles = setup.players.Take(setup.count).Select(p => p.participantId).ToArray(), journals = explorers.Take(setup.count).Select(e => e.Journal).ToArray(), fires = world.campsites.Select(c => c.state).ToArray(), resources = world.resources.Select(r => new ResourceState { id = r.id, regrowth = r.Regrowth,pose=Read(r.body) }).ToArray(), trucks = explorers.Take(setup.count).Select(e => new TruckPose { body = Read(e.truck.body), attached = e.attachedBodies.Select(Read).ToArray() }).ToArray(), activity = CaptureActivityJson() });
        }
        public bool TryRestoreWorkshopJson(string json, out string reason)
        {
            reason = ""; if (Live || PhotoActive) { reason = "Pause or stop the expedition before restoring."; return false; }
            Workshop data; try { data = JsonUtility.FromJson<Workshop>(json); } catch (Exception) { reason = "Corrupt expedition JSON."; return false; }
            var ids = setup.players.Take(setup.count).Select(p => p.participantId).ToArray();
            if (data == null || data.version != 1 || data.count != setup.count || data.option != option || data.profiles == null || !data.profiles.SequenceEqual(ids) || !float.IsFinite(data.clock) || data.clock < 0 || data.clock > 86400 || data.journals == null || data.journals.Length != setup.count || data.trucks == null || data.trucks.Length != setup.count || data.fires == null || data.fires.Length != world.campsites.Length || data.resources == null || data.resources.Length != world.resources.Length) { reason = "Expedition version/crew/format/roster mismatch."; return false; }
            for (int i = 0; i < setup.count; i++) if (!CampJournal.Valid(data.journals[i]) || data.journals[i].profileId != ids[i] || data.trucks[i] == null || !Valid(data.trucks[i].body) || !world.Contains(data.trucks[i].body.position) || data.trucks[i].attached == null || data.trucks[i].attached.Length != explorers[i].attachedBodies.Length || data.trucks[i].attached.Any(p => !Valid(p))) { reason = "Invalid stable profile or original truck/cab pose."; return false; }
            for (int i = 0; i < data.resources.Length; i++) if (data.resources[i] == null || data.resources[i].id != world.resources[i].id || !float.IsFinite(data.resources[i].regrowth) || data.resources[i].regrowth < 0 || data.resources[i].regrowth > 180 || !Valid(data.resources[i].pose)||!world.Contains(data.resources[i].pose.position)) { reason = "Unknown resource or invalid physical/regrowth state."; return false; }
            for (int i = 0; i < data.fires.Length; i++) if (data.fires[i] == null || data.fires[i].id != world.campsites[i].id || !float.IsFinite(data.fires[i].fuel) || data.fires[i].fuel < 0 || data.fires[i].fuel > 720 || !data.fires[i].built && data.fires[i].fuel > 0 || data.fires[i].meals < 0) { reason = "Unknown campsite or invalid fire state."; return false; }
            if (!ValidateActivityJson(data.activity, out reason)) return false;
            // Every mode/profile/geometry/extension check precedes mutation.
            EndPhoto(); world.session = this;
            for (int i = 0; i < world.resources.Length; i++)
            {
                var resource=world.resources[i];resource.Restore(data.resources[i].regrowth);Write(resource.body,data.resources[i].pose);
                if(data.resources[i].regrowth<=0){resource.body.isKinematic=false;resource.body.linearVelocity=data.resources[i].pose.velocity;resource.body.angularVelocity=data.resources[i].pose.angular;}
            }
            for (int i = 0; i < world.campsites.Length; i++) world.campsites[i].state = data.fires[i];
            for (int i = 0; i < explorers.Length; i++)
            {
                explorers[i].Prepare(i < setup.count, i < setup.count ? data.journals[i] : new CampJournal(setup.players[i].participantId), false); if (i >= setup.count) continue;
                Write(explorers[i].truck.body, data.trucks[i].body);
                for (int j = 0; j < explorers[i].attachedBodies.Length; j++) Write(explorers[i].attachedBodies[j], data.trucks[i].attached[j]);
            }
            WildernessSeconds = data.clock; restored = data; hasWorkshop = true; resumePending = !Running;
            RestoreActivityJson(data.activity);
            if (Running) ApplyVelocities(data); else foreach (var explorer in explorers) explorer.Stop();
            WorkshopStatus = "EXPEDITION RESTORED · RESUME WHEN READY"; return true;
        }
        static void Write(Rigidbody body, Pose pose) { body.isKinematic = true; body.position = pose.position; body.rotation = pose.rotation; }
        void ApplyVelocities(Workshop data)
        {
            for (int i = 0; i < data.count; i++)
            {
                var e = explorers[i]; e.truck.body.isKinematic = false; e.truck.body.linearVelocity = data.trucks[i].body.velocity; e.truck.body.angularVelocity = data.trucks[i].body.angular;
                for (int j = 0; j < e.attachedBodies.Length; j++) { e.attachedBodies[j].isKinematic = false; e.attachedBodies[j].linearVelocity = data.trucks[i].attached[j].velocity; e.attachedBodies[j].angularVelocity = data.trucks[i].attached[j].angular; }
            }
        }
    }
}
