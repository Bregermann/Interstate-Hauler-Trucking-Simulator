using System;
using System.Linq;
using System.Text;
using UnityEngine;
using LWS.TruckBowling;
using LWS.TruckCamping;
using LWS.TruckSports;
namespace LWS.TruckPetPals
{
    public sealed class PetPalsMatch : CampingMatch
    {
        public PetCompanion[] companions;
        public PetProfileRepository petProfiles;
        public Transform[] washStations;
        public PetToy[] petToys;
        public PetDigPatch[] digPatches;
        public PetDigGround[] digGrounds;
        public event Action<int> AdoptionRequested;
        readonly float[] nextAiCare = new float[4];
        readonly int[] aiCare = new int[4];
        readonly bool[] petView = new bool[4];
        PetWorkshop pendingPets;
        public override string Title => "TRUCK PET PALS";
        public override string Header => "YOUR WILDERNESS · YOUR LITTLE COMPANION";
        public override string Status => PhotoActive ? base.Status : "WALK · CARE · PLAY · LEARN · RELAX";
        public override string Help => "LITTLE FRIENDS. BIG TRUCKS.\n\nAdopt a miniature truck, fully clothed adult character or animated dog in Setup/Pause. Names are editable; pet and owner IDs stay stable. Each owner has a separate companion and saved care/trick record.\n\nDrive gently. Your pet follows a soft visual leash and occasionally investigates, sniffs or takes a funny grass break. Unclip in a marked play glade; CALL brings it back. A held STAY ends with CALL.\n\nACTION opens care choices. Feed from your real camp supplies, throw a nearby physical toy, nap or select a trick. Sit, lie down, roll over, shake, dig, stay and come all perform visible routines. Tap ACTION during the gentle cue; actual execution improves familiarity. Dig requires a marked soft-soil patch.\n\nAt a wash station: hold ACTION for water/soap/rinse/dry, alternate left/right to scrub, then tap ACTION for the next stage. Cleanliness changes visibly. Pets remain forgiving and never starve.\n\nPet VIEW follows your companion in chase view; private cab stays yours. PHOTO is shared and captures real scenic images. Camping's actual forest, trails, lake, wildlife and camp activities are reused.\n\nNo points or winner. Companion profiles autosave through the existing mode-local Pixel Crushers storer; Pause SAVE also preserves the strict expedition/terrain/pose snapshot. Finish care interactions before saving a workshop.";
        public override int ChoiceCount => 29;
        public override string ChoiceLabel(int choice)
        {
            if (choice < 11) return base.ChoiceLabel(choice);
            return new[] { "CALL COMPANION", "CLIP / RELEASE LEASH", "FEED COMPANION", "WASH COMPANION", "TRAIN SIT", "TRAIN LIE DOWN", "TRAIN ROLL OVER", "TRAIN SHAKE", "TRAIN DIG", "TRAIN STAY", "TRAIN COME", "PLAY FETCH", "PET NAP", "WAKE / RELEASE STAY", "PET PHOTO", "OWNER / PET CHASE VIEW", "AFFECTION", "GRASS BREAK" }[choice - 11];
        }
        protected override void BeginActivitySession()
        {
            if (!petProfiles.Loaded) petProfiles.LoadProfiles();
            if (pendingPets == null) { foreach (var ground in digGrounds) ground.ResetGround(); foreach (var patch in digPatches) patch.Restore(0); foreach (var toy in petToys) toy.ResetToy(); }
            for (int i = 0; i < companions.Length; i++)
            {
                companions[i].gameObject.SetActive(i < setup.count); if (i >= setup.count) continue;
                bool firstCompanion = !petProfiles.Archive.profiles.Any(p => p.profileId == setup.players[i].participantId);
                var profile = petProfiles.ForProfile(setup.players[i].participantId, SportsParticipantRegistry.GetAppearance(setup.players[i].participantId).bodyId);
                if (firstCompanion && i % 3 != 0) petProfiles.SelectOrAdopt(profile.profileId, (PetKind)(i % 3), SportsParticipantRegistry.GetAppearance(profile.profileId).bodyId);
                companions[i].Begin(profile.pets.Single(p => p.id == profile.selectedPetId), setup.players[i].color); nextAiCare[i] = Time.time + 12 + i * 4; aiCare[i] = i;
            }
            if (pendingPets != null) { ApplyPetWorkshop(pendingPets); pendingPets = null; }
        }
        protected override void TickActivities(float dt)
        {
            for (int i = 0; i < setup.count; i++) companions[i].Tick(dt, setup.players[i].source == BowlingSource.AI);
        }
        public override Vector2 FilterDrive(int owner, Vector2 requested)
        {
            var pet = companions[owner]; if (pet.Record == null) return requested;
            float speed = Vector3.Dot(explorers[owner].truck.body.linearVelocity, explorers[owner].truck.transform.forward);
            if (requested.y > 0 && speed > 6.5f) requested.y = speed > 7 ? -.25f : 0;
            if (pet.Record.leashed && pet.LeashDistance > 10 && Vector3.Dot(explorers[owner].truck.transform.forward, pet.transform.position - explorers[owner].truck.body.position) < 0) requested.y = speed > .6f ? -.5f : 0;
            var separation=pet.transform.position-explorers[owner].truck.body.position;
            if(separation.sqrMagnitude<36&&Vector3.Dot(explorers[owner].truck.transform.forward,separation)>1&&requested.y>0)requested.y=speed>.6f?-.5f:0;
            return requested;
        }
        public override bool CompanionOwnsInteraction(int owner)=>companions[owner].InteractionBusy;
        public override bool TryCompanionDrive(int owner, float dt, out Vector2 requested)
        {
            requested = Vector2.zero; var pet = companions[owner]; if (pet.Record == null) return false;
            if (pet.InteractionBusy) return true;
            if (Time.time < nextAiCare[owner]) return false;
            var explorer = explorers[owner];
            if (pet.Record.cleanliness < 70 || aiCare[owner] % 6 == 0)
            {
                var station = washStations.OrderBy(t => (t.position - explorer.truck.body.position).sqrMagnitude).First();
                if (Vector3.Distance(station.position, explorer.truck.body.position) > 15 || explorer.truck.body.linearVelocity.magnitude > 1)
                { explorer.FinishActivity("VISITING THE PET WASH"); requested = explorer.Navigate(station.position - Vector3.forward * 8); return true; }
                pet.BeginBath(station);
            }
            else if (aiCare[owner] % 6 == 1) { Interact(owner, 13); }
            else if (aiCare[owner] % 6 == 2) { pet.CallBack(); }
            else if (aiCare[owner] % 6 == 3)
            {
                var toy = petToys.OrderBy(t => (t.body.position - explorer.truck.body.position).sqrMagnitude).First();
                if (Vector3.Distance(toy.body.position, explorer.truck.body.position) > 10) { explorer.FinishActivity("VISITING THE PLAY GLADE"); requested = explorer.Navigate(toy.home - Vector3.forward * 6); return true; }
                pet.Play(toy);
            }
            else pet.BeginTraining((PetTrick)(aiCare[owner] / 2 % 7));
            aiCare[owner]++; nextAiCare[owner] = Time.time + 30; return pet.InteractionBusy;
        }
        public override string Interact(int owner, int choice)
        {
            var pet = companions[owner];
            if (choice < 11) { if (choice == 8) pet.CallBack(); return base.Interact(owner, choice); }
            if (choice == 11) { pet.CallBack(); return "CALLING YOUR COMPANION"; }
            if (choice == 12) return pet.ToggleLeash() ? pet.Record.leashed ? "LEASH CLIPPED" : "FREE ROAM · CALL ANYTIME" : pet.Hint;
            if (choice == 13)
            {
                var journal = explorers[owner].Journal;
                if (!journal.Consume(CampSupply.Meals, 1) && !journal.Consume(CampSupply.Forage, 1)) return "GATHER FORAGE OR COOK A MEAL FIRST";
                pet.Record.Feed(); petProfiles.Changed(); return "A TASTY TREAT";
            }
            if (choice == 14) return pet.BeginBath(washStations.OrderBy(t => (t.position - explorers[owner].truck.body.position).sqrMagnitude).First()) ? "OFF TO THE WASH STATION" : "PARK AT A PET WASH STATION";
            if (choice >= 15 && choice <= 21) return pet.BeginTraining((PetTrick)(choice - 15)) ? "TRAINING · WATCH THE CUE" : pet.Hint;
            if (choice == 22) return pet.Play(petToys.OrderBy(t => (t.body.position - explorers[owner].truck.body.position).sqrMagnitude).First()) ? "FETCH!" : "STOP CLOSE TO A PHYSICAL TOY";
            if (choice == 23) { var near = world.Nearest(pet.transform.position); if (near == null || near.kind != CampPlaceKind.Campsite && near.kind != CampPlaceKind.Cabin && near.kind != CampPlaceKind.RestClearing && near.kind != CampPlaceKind.Meadow || Vector3.Distance(near.activity.position, pet.transform.position) > 35) return "VISIT A COMFORTABLE CAMP OR CLEARING"; pet.Sleep(); return "SLEEP WELL, LITTLE FRIEND"; }
            if (choice == 24) { pet.CallBack(); return "BACK TO THE WALK"; }
            if (choice == 25) { BeginPhoto(owner); return "A COMPANION PORTRAIT"; }
            if (choice == 26) { petView[owner] = !petView[owner]; return petView[owner] ? "PET CHASE VIEW · CAB REMAINS YOURS" : "OWNER CHASE VIEW"; }
            if (choice == 27) { if (Vector3.Distance(pet.transform.position, explorers[owner].truck.body.position) > 12) return "CALL YOUR PET CLOSER"; pet.Record.Affection(3); petProfiles.Changed(); return "A LITTLE KINDNESS"; }
            if (choice == 28) return pet.RequestPee() ? "FINDING A SUITABLE GRASS PATCH" : "LET YOUR PET FINISH THIS ACTIVITY FIRST";
            return "";
        }
        public override Vector3 PhotoFocus(int owner) => companions[owner].Record == null ? base.PhotoFocus(owner) : companions[owner].transform.position + Vector3.up * .9f;
        public override void DescribeBroadcast(SportsBroadcastAction action,SportsBroadcastFraming framing)
        {
            base.DescribeBroadcast(action,framing);
            for(int i=0;i<setup.count;i++)if(companions[i].Record!=null&&companions[i].InteractionBusy){action.primary=i;action.preferHuman=true;action.Objective(companions[i].transform.position+Vector3.up*.7f,2);action.reason="ACTUAL COMPANION CARE AND TRICK PERFORMANCE";break;}
        }
        protected override void PresentActivities(float dt)
        {
            if (drivingViews.NeedsArena) return;
            for (int v = 0; v < drivingViews.rally.Humans; v++)
            {
                int owner = drivingViews.rally.owners[v]; if ((!petView[owner]&&!companions[owner].InteractionBusy) || drivingViews.ViewMode(v) == 2) continue;
                var pet = companions[owner]; var forward = Vector3.ProjectOnPlane(explorers[owner].truck.transform.forward, Vector3.up).normalized;
                drivingViews.rally.views[v].Pose(pet.transform.position + Vector3.up, Vector3.up * 5 - forward * 9, 60);
            }
        }
        public override string ViewText(int view)
        {
            int owner = drivingViews.rally.owners[view]; var pet = companions[owner]; if (pet.Record == null) return base.ViewText(view);
            if (explorers[owner].ContextOpen) return setup.players[owner].name + " + " + pet.Record.name + "\n< " + ChoiceLabel(explorers[owner].Choice) + " > · ACTION";
            return pet.Record.name + " · " + pet.Mood.ToString().ToUpperInvariant() + "\n" + pet.Hint;
        }
        public override bool TryReadViewportCard(int view, out int participant, out string score, out string detail)
        {
            if (!base.TryReadViewportCard(view, out participant, out score, out detail)) return false;
            var pet = companions[participant];
            if (pet.Record != null) { score = pet.Record.name; detail = pet.Mood.ToString().ToUpperInvariant(); }
            return true;
        }
        public override string ViewportContextPrompt(int view)
        {
            if (!TryViewportExplorer(view, out int owner)) return null;
            if (explorers[owner].ContextOpen || companions[owner].Record == null) return base.ViewportContextPrompt(view);
            return companions[owner].Hint;
        }
        public override string Results
        {
            get { var b = new StringBuilder("GOOD FRIENDS. GOOD WALK.\nNO WINNER OR POINTS\n\n"); for (int i = 0; i < setup.count; i++) { var pet = companions[i].Record; if (pet != null) b.AppendLine(setup.players[i].name + " + " + pet.name + " · BOND " + pet.BondLevel + " · " + pet.baths + " BATHS"); } return b.ToString(); }
        }
        public override void ExtraAction(string key)
        {
            if (key.StartsWith("pet-adopt", StringComparison.Ordinal) && !Live && int.TryParse(key.Substring(9), out int owner) && owner >= 0 && owner < setup.count) AdoptionRequested?.Invoke(owner);
            else base.ExtraAction(key);
        }
        public PetRecord Adopt(int owner, PetKind kind, string name, string bodyId = null, string accessoryId = null)
        {
            if (Live || owner < 0 || owner >= setup.count) throw new InvalidOperationException("Choose companions in Setup or Pause.");
            string id = setup.players[owner].participantId; var record = petProfiles.SelectOrAdopt(id, kind, SportsParticipantRegistry.GetAppearance(id).bodyId);
            if (!string.IsNullOrWhiteSpace(name)) petProfiles.Rename(id, record.id, name);
            if (!string.IsNullOrWhiteSpace(bodyId)) record.bodyId = bodyId;
            if (!string.IsNullOrWhiteSpace(accessoryId)) record.accessoryId = accessoryId;
            petProfiles.Changed();
            if (Running) companions[owner].Begin(record, setup.players[owner].color); return record;
        }
        protected override void StopActivities()
        {
            if (companions != null) foreach (var pet in companions) if (pet != null) pet.Stop();
            if (petProfiles != null && petProfiles.Loaded && !petProfiles.ReadOnly) { petProfiles.Changed(); petProfiles.SaveProfiles(); }
        }
        [Serializable] public sealed class PetPose { public string profileId, petId; public Vector3 position; public Quaternion rotation; public bool sleeping; }
        [Serializable] public sealed class DigState { public string id; public float progress; }
        [Serializable] public sealed class ToyState { public string id; public Pose pose; }
        [Serializable] public sealed class PetWorkshop { public int version; public PetArchive profiles; public PetPose[] poses; public DigState[] holes; public ToyState[] toys; }
        protected override string CaptureActivityJson()
        {
            if (petProfiles.ReadOnly) throw new InvalidOperationException("Unsupported original pet profiles are preserved read-only.");
            if (companions.Take(setup.count).Any(p => p.Record == null || p.InteractionBusy || p.Mood == PetMood.Peeing || p.Mood == PetMood.Playing)) throw new InvalidOperationException("Finish the pet's current care, grass break or toy retrieval before saving the expedition.");
            var ids = setup.players.Take(setup.count).Select(p => p.participantId).ToArray();
            return JsonUtility.ToJson(new PetWorkshop { version = 1, profiles = new PetArchive { version = 1, profiles = ids.Select(id => petProfiles.Archive.profiles.Single(p => p.profileId == id)).ToArray() }, poses = companions.Take(setup.count).Select(p => new PetPose { profileId = p.Record.profileId, petId = p.Record.id, position = p.transform.position, rotation = p.transform.rotation, sleeping = p.Mood == PetMood.Sleeping }).ToArray(), holes = digPatches.Select(p => new DigState { id = p.id, progress = p.Progress }).ToArray(), toys = petToys.Select(t => new ToyState { id = t.id, pose = new Pose { position = t.body.position, rotation = t.body.rotation, velocity = t.body.linearVelocity, angular = t.body.angularVelocity } }).ToArray() });
        }
        protected override bool ValidateActivityJson(string json, out string reason)
        {
            reason = ""; PetWorkshop data;
            try { data = JsonUtility.FromJson<PetWorkshop>(json); } catch (Exception) { reason = "Corrupt pet expedition state."; return false; }
            if (!petProfiles.Loaded) petProfiles.LoadProfiles();
            if (petProfiles.ReadOnly) { reason = "Unsupported original pet profiles are preserved read-only."; return false; }
            if (data == null || data.version != 1 || !PetArchive.Valid(data.profiles, out reason) || data.profiles.profiles.Length != setup.count || data.poses == null || data.poses.Length != setup.count || data.holes == null || data.holes.Length != digPatches.Length || data.toys == null || data.toys.Length != petToys.Length) { reason = "Pet expedition version/roster mismatch."; return false; }
            for (int i = 0; i < setup.count; i++)
            {
                var pose = data.poses[i]; if (data.profiles.profiles[i].profileId != setup.players[i].participantId || pose == null || pose.profileId != setup.players[i].participantId || pose.petId != data.profiles.profiles[i].selectedPetId || !ValidPose(pose.position, pose.rotation) || !world.WalkingPoint(pose.position, out var safe) || Vector3.Distance(pose.position, safe) > 1) { reason = "Invalid stable pet owner or terrain pose."; return false; }
            }
            for (int i = 0; i < digPatches.Length; i++) if (data.holes[i] == null || data.holes[i].id != digPatches[i].id || !float.IsFinite(data.holes[i].progress) || data.holes[i].progress < 0 || data.holes[i].progress > 1) { reason = "Unknown/invalid pet digging patch."; return false; }
            for (int i = 0; i < petToys.Length; i++) if (data.toys[i] == null || data.toys[i].id != petToys[i].id || data.toys[i].pose == null || !ValidPose(data.toys[i].pose.position, data.toys[i].pose.rotation) || !Finite(data.toys[i].pose.velocity) || !Finite(data.toys[i].pose.angular)) { reason = "Unknown/invalid physical toy."; return false; }
            if (!petProfiles.CanMergeProfiles(data.profiles.profiles, out reason)) return false;
            return true;
        }
        static bool Finite(Vector3 p) => float.IsFinite(p.x) && float.IsFinite(p.y) && float.IsFinite(p.z) && p.sqrMagnitude < 16000000;
        static bool ValidPose(Vector3 p, Quaternion q) => Finite(p) && float.IsFinite(q.x) && float.IsFinite(q.y) && float.IsFinite(q.z) && float.IsFinite(q.w) && Mathf.Abs(Quaternion.Dot(q, q) - 1) < .01f;
        protected override void RestoreActivityJson(string json)
        {
            pendingPets = JsonUtility.FromJson<PetWorkshop>(json);
            if (!petProfiles.MergeProfiles(pendingPets.profiles.profiles, out string reason)) throw new InvalidOperationException(reason);
            if (Running) { for (int i = 0; i < setup.count; i++) { var p = pendingPets.profiles.profiles[i]; companions[i].Begin(p.pets.Single(r => r.id == p.selectedPetId), setup.players[i].color); } ApplyPetWorkshop(pendingPets); pendingPets = null; }
        }
        void ApplyPetWorkshop(PetWorkshop data)
        {
            foreach (var ground in digGrounds) ground.ResetGround(); for (int i = 0; i < digPatches.Length; i++) digPatches[i].Restore(data.holes[i].progress);
            for (int i = 0; i < data.poses.Length; i++) if (!companions[i].RestoreSafePose(data.poses[i].position, data.poses[i].rotation, data.poses[i].sleeping, out string reason)) throw new InvalidOperationException(reason);
            for (int i = 0; i < petToys.Length; i++) { var body = petToys[i].body; body.isKinematic = true; body.position = data.toys[i].pose.position; body.rotation = data.toys[i].pose.rotation; body.isKinematic = false; body.linearVelocity = data.toys[i].pose.velocity; body.angularVelocity = data.toys[i].pose.angular; }
        }
    }
}
