using UnityEngine;
using UnityEngine.AI;
using LWS.TruckSports;
namespace LWS.TruckCamping
{
    public sealed partial class CampExplorer : MonoBehaviour
    {
        public CampingMatch match;
        public SportsEventTruck truck;
        public Rigidbody[] attachedBodies;
        public int slot;
        public Vector3 spawn;
        public Transform rod, bobber;
        public LineRenderer fishingLine;
        public ParticleSystem catchSplash;
        public CampActivity Activity { get; private set; }
        public CampJournal Journal { get; private set; }
        public string Hint { get; private set; } = "";
        public bool ContextOpen { get; private set; }
        public int Choice { get; private set; }
        public bool Sleeping => Activity == CampActivity.Sleeping;
        public float ActivitySeconds { get; private set; }
        public bool Bite => Activity == CampActivity.Fishing && ActivitySeconds % 10 >= 4 && ActivitySeconds % 10 < 6;
        public int ActualCollections { get; private set; }
        public int ActualFishingCasts { get; private set; }
        NavMeshPath path;
        void Awake() { EnsureNavigationPath(); }
        void EnsureNavigationPath() { if(path==null)path=new NavMeshPath(); }
        Vector3 target, previous;
        float nextPath, contextAge;
        bool menuAxis;
        CampSite activeSite;
        CampResource resource;
        int routeIndex, aiTask;
        float taskAge;
        public void Prepare(bool active, CampJournal journal, bool resetPose)
        {
            RestorePickupGroundExclusions();ResetNavigationRecovery();
            Journal = journal; truck.gameObject.SetActive(active); if (!active) return;
            if (resetPose) truck.Prepare(spawn, Quaternion.identity, true);
            truck.Power(true); truck.tractorMotor.Configure(); truck.body.isKinematic = false;
            foreach (var attached in attachedBodies) attached.isKinematic = false;
            Activity = CampActivity.Exploring; ContextOpen = false; Choice = 0; ActivitySeconds = taskAge = 0; routeIndex = slot; aiTask = slot % 3;
            ActualCollections = ActualFishingCasts = 0; previous = truck.body.position; HideRod();RefreshPickupGroundExclusions();
        }
        public void Stop()
        {
            RestorePickupGroundExclusions();ResetNavigationRecovery();
            truck.Power(false); truck.StopSound(); ContextOpen = false; HideRod();
            foreach (var body in attachedBodies) { if (!body.isKinematic) body.linearVelocity = body.angularVelocity = Vector3.zero; body.isKinematic = true; }
            if (!truck.body.isKinematic) truck.body.linearVelocity = truck.body.angularVelocity = Vector3.zero; truck.body.isKinematic = true;
        }
        public void Tick(Vector2 axes, bool action, bool bot, float dt)
        {
            RefreshPickupGroundExclusions();
            float distance = Vector3.Distance(previous, truck.body.position); if (distance < 3) Journal.walked += distance; previous = truck.body.position;
            Journal.Tick(dt, match.SurvivalLite, truck.body.linearVelocity.magnitude > .5f, Sleeping);
            var place = match.world.Nearest(truck.body.position); if (place != null && Vector3.Distance(truck.body.position, place.transform.position) < place.discoveryRadius) Journal.Discover(place.index);
            ActivitySeconds += dt; taskAge += dt;
            Vector2 drive = axes;
            if(match.CompanionOwnsInteraction(slot)){ContextOpen=false;drive=Vector2.zero;}
            else if (bot && !match.TryCompanionDrive(slot, dt, out drive)) drive = Think(dt);
            else if (ContextOpen)
            {
                contextAge += dt;
                if (Mathf.Abs(axes.x) > .6f && !menuAxis) { Choice = (Choice + (axes.x > 0 ? 1 : match.ChoiceCount - 1)) % match.ChoiceCount; menuAxis = true; }
                if (Mathf.Abs(axes.x) < .2f) menuAxis = false;
                if (action && contextAge > .15f) { ContextOpen = false; Hint = match.Interact(slot, Choice); }
                drive = Vector2.zero;
            }
            else if (action)
            {
                if (Activity == CampActivity.Fishing)
                {
                    if (Bite) { Journal.Add(CampSupply.Fish, 1); Journal.fishCaught++; if (catchSplash != null) catchSplash.Play(); Hint = "FRESH FISH · TRY COOKING AT A LIT CAMPFIRE"; ActivitySeconds = 0; }
                    else { ActivitySeconds = 0; Hint = "CAST AGAIN · WATCH FOR THE BOBBER DIP"; ActualFishingCasts++; }
                }
                else { ContextOpen = true; contextAge = 0; menuAxis = false; }
            }
            if (Activity == CampActivity.Gathering)
            {
                if (resource == null || !resource.Available) FinishActivity("RESOURCE UNAVAILABLE");
                else if (ActivitySeconds >= 1.25f)
                {
                    bool picked = resource.Gather(Journal, truck.body.position, truck.body.linearVelocity.magnitude);
                    if (picked) ActualCollections++; FinishActivity(picked ? "COLLECTED " + resource.kind.ToString().ToUpperInvariant() : "STOP CLOSE TO THE REAL MATERIAL");
                }
            }
            else if (Activity == CampActivity.Cooking && ActivitySeconds >= 9)
            {
                bool cooked = activeSite != null && activeSite.Nearby(truck.body.position) && activeSite.state.Cook(Journal);
                FinishActivity(cooked ? "TWO WARM MEALS PREPARED" : "COOKING NEEDS A LIT FIRE AND FRESH INGREDIENTS");
            }
            else if (Activity == CampActivity.Fishing) PresentRod();
            else if (Sleeping && truck.body.linearVelocity.magnitude > 1.5f) FinishActivity("AWAKE · BACK TO THE TRAIL");
            bool parked = ContextOpen || Activity != CampActivity.Exploring;
            drive = match.FilterDrive(slot, drive);
            truck.TickDrive(parked ? Vector2.zero : new Vector2(drive.x, Mathf.Clamp(drive.y, -.65f, .7f)));
            if (parked) truck.tractorMotor.vehicle.input.states.handbrake = truck.tractorMotor.vehicle.input.states.handbrakeRaw = 1;
        }
        public bool BeginGather(CampResource part)
        {
            if (part == null || !part.Available || Vector3.Distance(truck.body.position, part.body.position) > 10 || truck.body.linearVelocity.magnitude > 1.5f) return false;
            resource = part; Begin(CampActivity.Gathering); return true;
        }
        public bool BeginCooking(CampSite site)
        {
            if (site == null || !site.Nearby(truck.body.position) || !site.state.Burning || Journal.supplies[(int)CampSupply.Fish] + Journal.supplies[(int)CampSupply.Forage] <= 0) return false;
            activeSite = site; Begin(CampActivity.Cooking); return true;
        }
        public bool BeginFishing()
        {
            var near = match.world.Nearest(truck.body.position);
            if (near == null || near.kind != CampPlaceKind.Lake || Vector3.Distance(truck.body.position, near.parking.position) > 18 || truck.body.linearVelocity.magnitude > 1.5f) return false;
            Begin(CampActivity.Fishing); ActualFishingCasts++; return true;
        }
        public void Begin(CampActivity activity) { Activity = activity; ActivitySeconds = 0; ContextOpen = false; if (activity != CampActivity.Fishing) HideRod(); }
        public void FinishActivity(string hint) { Begin(CampActivity.Exploring); Hint = hint; }
        void HideRod() { if (rod != null) rod.gameObject.SetActive(false); if (bobber != null) bobber.gameObject.SetActive(false); if (fishingLine != null) fishingLine.enabled = false; }
        void PresentRod()
        {
            if (rod == null || bobber == null || fishingLine == null) return;
            var lake = match.world.Nearest(truck.body.position); var water = lake.activity.position;
            rod.gameObject.SetActive(true); bobber.gameObject.SetActive(true); fishingLine.enabled = true;
            rod.position = truck.transform.TransformPoint(new Vector3(0, 3, 6)); rod.rotation = Quaternion.LookRotation(water - rod.position);
            bobber.position = water + Vector3.right * (slot - 1.5f) * 1.5f + Vector3.up * (Mathf.Sin(Time.time * 4) * .025f - (Bite ? .2f : 0));
            fishingLine.SetPosition(0, rod.position + rod.forward * 5); fishingLine.SetPosition(1, bobber.position);
        }
        Vector2 Think(float dt)
        {
            if (Activity == CampActivity.Fishing)
            {
                if (Bite) { Journal.Add(CampSupply.Fish, 1); Journal.fishCaught++; if (catchSplash != null) catchSplash.Play(); FinishActivity("AI CAUGHT A REAL BOBBER BITE"); taskAge = 0; aiTask = 2; }
                return Vector2.zero;
            }
            if (Activity == CampActivity.Sleeping || Activity == CampActivity.Resting)
            { if (ActivitySeconds >= 12) { FinishActivity("RESTED"); taskAge = 0; aiTask = (aiTask + 1) % 5; } return Vector2.zero; }
            if (Activity != CampActivity.Exploring) return Vector2.zero;
            if (Journal.hunger < 65) Journal.Eat();
            if (aiTask == 0)
            {
                CampSupply needed = Journal.supplies[(int)CampSupply.Sticks] < 3 ? CampSupply.Sticks : Journal.supplies[(int)CampSupply.Firewood] < 2 ? CampSupply.Firewood : CampSupply.Forage;
                CampResource nearest = null; float best = float.PositiveInfinity;
                foreach (var part in match.world.resources) if (part.Available && part.kind == needed) { float d = (part.body.position - truck.body.position).sqrMagnitude; if (d < best) { nearest = part; best = d; } }
                if (nearest != null)
                {
                    if (best < 85 && truck.body.linearVelocity.magnitude < 1.3f) { BeginGather(nearest); if (Journal.supplies[(int)CampSupply.Forage] > 0) aiTask = 2; return Vector2.zero; }
                    return NavigateTo(CollectionApproachTarget(nearest.body.position),2.5f);
                }
                aiTask = 4;
            }
            if (aiTask == 1)
            {
                CampLandmark lake = null; foreach (var place in match.world.landmarks) if (place.kind == CampPlaceKind.Lake) { lake = place; break; }
                if (lake != null) { if (Vector3.Distance(truck.body.position, lake.parking.position) < 14 && truck.body.linearVelocity.magnitude < 1) { BeginFishing(); return Vector2.zero; } return Navigate(lake.parking.position); }
                aiTask = 4;
            }
            if (aiTask == 2)
            {
                var site = match.world.campsites[slot % match.world.campsites.Length];
                if (!site.Nearby(truck.body.position) || truck.body.linearVelocity.magnitude > 1) return Navigate(site.landmark.parking.position);
                if (!site.state.built && !site.state.Build(Journal)) { aiTask = 0; return Vector2.zero; }
                if (!site.state.Burning) site.state.AddFuel(Journal);
                if (BeginCooking(site)) { aiTask = 3; return Vector2.zero; }
                Begin(CampActivity.Resting); aiTask = 3; return Vector2.zero;
            }
            if (aiTask == 3)
            {
                var bed = match.world.hammocks[slot % match.world.hammocks.Length];
                if (bed.Supports(truck)) { Begin(CampActivity.Sleeping); Journal.naps++; return Vector2.zero; }
                if (taskAge > 75) { taskAge = 0; aiTask = 4; }
                else return Navigate(bed.approach.position);
            }
            if (match.world.route.Length > 0)
            {
                for(int i=0;i<match.setup.count;i++)
                {
                    if(match.setup.players[i].source==LWS.TruckBowling.BowlingSource.AI)continue;
                    var leader=match.explorers[i].truck;float separation=Vector3.Distance(leader.body.position,truck.body.position);
                    if(separation>28&&separation<300){Hint="FOLLOWING OUR CAMPING PARTY";return Navigate(leader.body.position-leader.transform.forward*20+leader.transform.right*(slot%2==0?-7:7));}
                    break;
                }
                if(taskAge>8&&NearbyWildlife())
                {Hint="WATCHING THE ANIMATED WOODLAND WILDLIFE";Begin(CampActivity.Resting);taskAge=0;return Vector2.zero;}
                var point = match.world.route[routeIndex % match.world.route.Length].position;
                if (Vector3.Distance(truck.body.position, point) < 18) { routeIndex++; taskAge = 0; aiTask = routeIndex % 4; }
                return Navigate(point);
            }
            return Vector2.zero;
        }
        bool NearbyWildlife()
        {if(match.world.animals==null)return false;foreach(var animal in match.world.animals)if(animal!=null&&Vector3.Distance(animal.transform.position,truck.body.position)<14)return true;return false;}
        public static Vector3 CollectionApproachTarget(Vector3 actualPickup)=>actualPickup-Vector3.forward*6;
        public static float NavigationDesiredSpeed(float distance,float absoluteAngle,float stoppingDistance)
            =>distance<stoppingDistance?0:distance<18?1.8f:absoluteAngle>35?2.8f:7;
        public Vector2 Navigate(Vector3 destination)=>NavigateTo(destination,7);
        Vector2 NavigateTo(Vector3 destination,float stoppingDistance)
        {
            EnsureNavigationPath();
            destination=HammockNavigationLeg(destination,ref stoppingDistance,out bool directDeckApproach);
            if (!directDeckApproach&&(Time.time >= nextPath || (target - destination).sqrMagnitude > 9))
            {
                nextPath = Time.time + 2; target = destination;
                if (match.world.WalkingPoint(truck.body.position, out var from) && match.world.WalkingPoint(destination, out var to)) NavMesh.CalculatePath(from, to, NavMesh.AllAreas, path);
            }
            Vector3 aim = destination;
            if (!directDeckApproach&&path.status == NavMeshPathStatus.PathComplete && path.corners.Length > 1)
                aim=HullClearNavigationAim(path.corners,destination);
            var local = Quaternion.Inverse(truck.body.rotation) * (aim - truck.body.position); float angle = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
            float distance = Vector3.ProjectOnPlane(destination - truck.body.position, Vector3.up).magnitude;
            float desired = NavigationDesiredSpeed(distance,Mathf.Abs(angle),stoppingDistance);
            float speed = Vector3.Dot(truck.body.linearVelocity, truck.transform.forward);
            var requested=new Vector2(Mathf.Clamp(angle / 28, -1, 1), Mathf.Clamp((desired - speed) * .35f, -.6f, .7f));
            return RecoverActualBlockedNavigation(requested,angle,distance,stoppingDistance);
        }
    }
}
