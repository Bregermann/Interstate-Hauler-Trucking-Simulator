using System;
using System.Collections.Generic;
using System.Text;
using LWS.TruckSports;
using MalbersAnimations.Controller;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Mayhem
{
    [DefaultExecutionOrder(80)]
    public sealed class DragonMayhemDirector : MonoBehaviour
    {
        public DragonMayhemSportAdapter adapter;
        public DragonSpeciesCatalog catalog;
        public DragonMayhemConfiguration configuration;
        public GameObject actorPrefab, goldPrefab, sheepPrefab;
        public string speciesId = "dragon-inferno";
        [Range(6,32)] public int goldBudget = 18;
        public bool EnabledForSport { get; private set; }
        public bool Active => runtime != null && adapter.Live;
        public Transform RuntimeRoot => runtime != null ? runtime.transform : null;
        public string Warning { get; private set; } = "";
        public int PhysicalGoldDrops { get; private set; }
        public int PhysicalAttackContacts { get; private set; }
        public int SheepGrabs { get; private set; }
        public int SheepMeals { get; private set; }
        public int ExplosiveReactions { get; private set; }
        public int ConsumedExplosiveMeals { get; private set; }
        public int ConsumedExplosiveReactions { get; private set; }
        public int LastConsumedSheepId { get; private set; }
        public int LastConsumedExplosionSheepId { get; private set; }
        public int CurrentSheepId => meal!=null&&sheepIds.TryGetValue(meal,out int id)?id:0;
        public IReadOnlyList<string> SheepEvents => sheepEvents;
        public int[] SideGold { get; } = new int[8];
        public DragonCharacter Actor => actor;
        public int RuntimeActorCount => actor != null && actor.gameObject.activeSelf ? 1 : 0;
        public string PreferenceKey => "TruckSports.DragonMayhem." + adapter.SportId + ".v1";
        public const string Help = "Optional Dragon Mayhem: visible dragon passes, warned small gusts, physical gold, and sheep snacks. Gold earns a capped sport-specific side bonus; the original possession, balloon, board, or cheese winner remains authoritative. Cornhole gusts occur only on the approach; trigger-only gold may touch an approaching or airborne truck, never a settled truck. Dragons and gold never block the board or hole. Gold expires and cannot be collected twice. Explosive sheep retain their original once-only blast. OFF creates no variant actors, gold, sheep, or in-match overlay.";
        enum Phase { Approach, Attack, DropApproach, Drop, SheepApproach, Grab, Carry, Eat, Depart }
        Phase phase;
        GameObject runtime;
        DragonCharacter actor;
        DragonSpeciesDefinition species;
        MPickUp pickup;
        DragonMayhemSheep meal;
        readonly List<DragonMayhemSheep> sheep = new List<DragonMayhemSheep>();
        DragonMayhemGold[] gold;
        readonly HashSet<int> collected = new HashSet<int>();
        readonly HashSet<int> consumedExplosions = new HashSet<int>();
        readonly List<string> sheepEvents = new List<string>();
        readonly Dictionary<DragonMayhemSheep,int> sheepIds = new Dictionary<DragonMayhemSheep,int>();
        int nextSheepId;
        readonly List<Collider> originalDynamic = new List<Collider>();
        readonly List<Component> addedOriginalComponents = new List<Component>();
        sealed class OriginalSheepState { public DragonMayhemSheep entry; public Transform parent; public Vector3 position,localScale; public Quaternion rotation; }
        readonly List<OriginalSheepState> originalSheep = new List<OriginalSheepState>();
        int nextLease, nextTarget, target, pass, lastRound;
        float phaseAge, clock, nextPass = 6, feedAge;
        float selectedPreySeparation,selectedPreyTravel;
        float dropContactLead;
        Vector3 predictedDropMouth;
        bool dropForecastSupported;
        bool wasRunning, wasPaused, actionAccepted, contactSeen;
        bool passActive;
        bool sheepLandingRequested, sheepLanded, carryStarted;
        Transform mouthHolder;
        string previousMatch;
        Vector3 goal, departure, handoffStart, dropGround;

        void Awake() { adapter.Validate(); EnabledForSport = SportsPrefs.GetInt(PreferenceKey, 0) == 1; }
        public bool SetEnabled(bool value)
        {
            if (adapter.InMatch) return false;
            EnabledForSport = value; SportsPrefs.SetInt(PreferenceKey,value ? 1 : 0); SportsPrefs.Save();
            if (!value) Cleanup(); return true;
        }
        void Update()
        {
            bool running = adapter.InMatch;
            bool restart = running && (!wasRunning || previousMatch != adapter.MatchKey);
            if (restart) { Cleanup(); Array.Clear(SideGold,0,SideGold.Length); collected.Clear(); consumedExplosions.Clear(); sheepEvents.Clear(); nextSheepId = nextLease = pass = nextTarget = 0; clock = 0; nextPass = 6; PhysicalGoldDrops = PhysicalAttackContacts = SheepGrabs = SheepMeals = ExplosiveReactions = ConsumedExplosiveMeals = ConsumedExplosiveReactions = LastConsumedSheepId = LastConsumedExplosionSheepId = 0; }
            else if(runtime!=null && adapter.cheese!=null && adapter.cheese.Round!=lastRound) { Cleanup(); nextPass=clock+6; }
            wasRunning = running; previousMatch = adapter.MatchKey; if (adapter.cheese != null) lastRound = adapter.cheese.Round;
            if (!EnabledForSport || !running || adapter.Finished) { Cleanup(); return; }
            if (!adapter.Live)
            {
                if (actor != null && !wasPaused) { actor.Pause(true); FreezeGold(true); wasPaused = true; }
                return;
            }
            if (wasPaused) { actor?.Pause(false); FreezeGold(false); wasPaused = false; }
            clock += Time.deltaTime;
            if (runtime == null && clock >= nextPass) StartRuntime();
            else if(runtime!=null && !passActive && clock>=nextPass) BeginPass();
        }
        void FixedUpdate()
        {
            if (!Active) return;
            float dt = Time.fixedDeltaTime;
            foreach (var coin in gold) if (coin.gameObject.activeSelf) coin.Advance(dt);
            if(!passActive)return;
            phaseAge += dt; actor.Tick(dt);
            if (meal != null && meal.Detonated) { ReactToExplosion(); return; }
            if (phase == Phase.Depart)
            {
                FlyTo(departure, dt);
                if (Vector3.Distance(actor.Position,departure) < 12 || phaseAge > 18) EndPass();
                return;
            }
            if (phaseAge > 26) { Depart("DRAGON DEPARTING · PASS CANCELLED"); return; }
            if (phase == Phase.Approach || phase == Phase.DropApproach)
            {
                if(phase==Phase.Approach)
                {
                    if(target<0 || !adapter.Eligible(target)) { Depart("DRAGON PASS ENDED · TARGET PROTECTED"); return; }
                    var body=adapter.Body(target);
                    goal=body.position+Vector3.up*configuration.approachHeight-body.transform.forward*14;
                }
                else
                {
                    UpdateDropApproachGoal();
                }
                FlyTo(goal,dt);
                // A moving pass is valid. Timing uses the real clip's contact
                // lead and measured mouth pose; only the eventual genuine
                // action contact can authorize any release.
                predictedDropMouth=actor.motor.body.position+actor.motor.Velocity*dropContactLead+
                    actor.motor.transform.TransformVector(configuration.dropMouthLocalOffset);
                dropForecastSupported=SafeDropGround(predictedDropMouth,out var predictedGround)&&predictedDropMouth.y>predictedGround.y+2;
                bool arrived=Vector3.Distance(actor.Position,goal)<9&&(phase==Phase.Approach||actor.motor.IsFlying&&dropForecastSupported);
                if (arrived)
                {
                    phaseAge = 0; contactSeen = false;
                    if (phase == Phase.Approach) { actor.motor.StopMotion(); phase = Phase.Attack; actionAccepted = actor.RequestAttack(DragonAnimationAction.Breath); }
                    else
                    {
                        phase = Phase.Drop; actionAccepted = actor.motor.TryAction(DragonAnimationAction.Drop,8);
                        Debug.Log("MAYHEM_ACTUAL_FLYBY_DROP_REQUEST sport="+adapter.SportId+" accepted="+actionAccepted+" body="+actor.motor.body.position.ToString("F3")+" velocity="+actor.motor.Velocity.ToString("F3")+" clipLead="+dropContactLead+" predictedMouth="+predictedDropMouth.ToString("F3")+" intendedGround="+dropGround.ToString("F3"));
                    }
                    if (!actionAccepted) Depart("DRAGON PASS ENDED · NO CONTACT");
                }
            }
            else if(phase==Phase.Drop)
            {
                UpdateDropApproachGoal();FlyTo(goal,dt);
                if(contactSeen&&!actor.motor.ActionPlaying)Depart("");
            }
            else if (phase == Phase.SheepApproach)
            {
                if (meal == null || !meal.Available) { Depart(""); return; }
                if(!HasGroundGrabContactGeometry()){Depart("SHEEP PASS ENDED · GROUNDED REACH NOT CONFIGURED");return;}
                // A flying palm is several metres above the dynamic body. Do
                // not command the body below the ground to touch a sheep: use
                // the existing real vendor landing and supported ground move.
                if(!sheepLandingRequested)
                {
                    if(!species.Supports(DragonSpeciesRole.Ground)||!adapter.TryGround(meal.transform.position,out var support)||!actor.RequestLanding(support)) { Depart("SHEEP PASS ENDED · NO SAFE LANDING");return; }
                    sheepLandingRequested=true;
                }
                if(!sheepLanded)
                {
                    if(!actor.motor.IsGrounded||actor.motor.IsFlying||actor.motor.LandingPlaying)return;
                    sheepLanded=true;actor.SetIntent(DragonCharacterIntent.PlayerMounted,actor.Position);
                }
                // The real idle palm is4.7m high; its pose cannot decide
                // whether the selected grounded reach can contact a sheep.
                // Approach using the explicitly measured future clip pose,
                // while the actual animated contact below remains authority.
                Vector3 contactOffset=actor.motor.transform.TransformVector(configuration.groundGrabGripLocalOffset);
                goal=meal.transform.position-Vector3.ProjectOnPlane(contactOffset,Vector3.up);
                actor.motor.MoveWorld(Vector3.ClampMagnitude(Vector3.ProjectOnPlane(goal-actor.Position,Vector3.up)/8,1),false,false);
                if (Vector3.Distance(actor.Position+contactOffset,meal.transform.position) < 2.8f)
                {actor.motor.StopMotion();if(actor.motor.TryAction(DragonAnimationAction.GroundGrab,5)){phase=Phase.Grab;phaseAge=0;actionAccepted=true;}}
            }
            else if(phase==Phase.Grab)
            {
                actor.motor.MoveWorld(Vector3.zero,false,false);
                if(!contactSeen&&!actor.motor.ActionPlaying&&phaseAge>1.5f){phase=Phase.SheepApproach;phaseAge=0;}
            }
            else if (phase == Phase.Carry)
            {
                if (meal == null || !pickup.Has_Item) { Depart(""); return; }
                FlyTo(goal,dt);
                if(!carryStarted)
                {
                    if(actor.motor.ActionPlaying&&actor.motor.CurrentAction==DragonAnimationAction.Carry)carryStarted=true;
                    else {actor.motor.TryAction(DragonAnimationAction.Carry,3);return;}
                }
                feedAge += dt;
                // A visible hand-to-mouth handoff uses the vendor's own holder
                // API; it does not replace Pickable physics or parenting rules.
                pickup.Parent(mouthHolder,Vector3.Lerp(handoffStart,Vector3.down * .4f,Mathf.Clamp01(feedAge/2)),Vector3.zero);
                if (feedAge > 2.1f && Vector3.Distance(meal.transform.position,actor.mouth.position) < 3)
                { phase = Phase.Eat; phaseAge = 0; actor.motor.StopMotion(); actor.motor.InterruptAction(); actionAccepted=false; }
            }
            else if(phase==Phase.Eat&&!actionAccepted)
            {actor.motor.MoveWorld(Vector3.zero,false,true);actionAccepted=actor.motor.TryAction(DragonAnimationAction.Eat,8);if(!actionAccepted&&phaseAge>4)Depart("SHEEP PASS ENDED · EAT UNAVAILABLE");}
            else if (contactSeen && !actor.motor.ActionPlaying && actor.WarningSecondsRemaining <= 0) Depart("");
        }
        void StartRuntime()
        {
            if (configuration==null || configuration.sport!=adapter.sport || actorPrefab == null || goldPrefab == null || sheepPrefab == null || catalog == null || !catalog.TryGet(speciesId,out species) || !species.Supports(DragonSpeciesRole.Flying)) throw new InvalidOperationException("Mayhem requires matching sport configuration and configured owned animated actor, sheep and physical gold assets.");
            if(configuration.dropSpeciesId!=speciesId||!species.TryGetAnimation(DragonAnimationAction.Drop,out var drop)||configuration.dropContactClip!=drop.clip)
                throw new InvalidOperationException("Explicitly upgrade owned Mayhem Drop geometry from the actual configured contact clip before enabling this variant.");
            dropContactLead=drop.clip.length*configuration.dropContactNormalizedTime/Mathf.Max(.01f,drop.playbackSpeed)+Time.fixedDeltaTime;
            originalDynamic.Clear();
            foreach (var root in gameObject.scene.GetRootGameObjects()) foreach (var c in root.GetComponentsInChildren<Collider>(true)) if (c.attachedRigidbody != null) originalDynamic.Add(c);
            runtime = new GameObject("Dragon Mayhem · bounded runtime only"); runtime.transform.SetParent(transform,false);
            var instance = Instantiate(actorPrefab,runtime.transform); actor = instance.GetComponent<DragonCharacter>();
            PlaceActualMotorAtSpawn();
            actor.gameObject.SetActive(true);
            actor.BindAudio(adapter.Settings); actor.Prepare(species,Array.Empty<SportsEventTruck>(),0,7919+pass);
            actor.SetIntent(DragonCharacterIntent.PlayerMounted,adapter.Center);
            actor.motor.ActionContact += OnActionContact;
            IgnoreOriginalDynamics(actor.GetComponentsInChildren<Collider>(true));
            PreparePickup();
            gold = new DragonMayhemGold[Mathf.Clamp(goldBudget,6,32)];
            for (int i=0;i<gold.Length;i++) { gold[i]=Instantiate(goldPrefab,runtime.transform).GetComponent<DragonMayhemGold>(); IgnoreOriginalDynamics(new[]{gold[i].solid}); foreach(var c in actor.physicalColliders) if(c!=null)Physics.IgnoreCollision(gold[i].solid,c,true); gold[i].Retire(); }
            PrepareSheep();
            BeginPass();
        }
        void BeginPass()
        {
            passActive=true;meal=null;sheepLandingRequested=sheepLanded=carryStarted=false;actor.gameObject.SetActive(true);
            PlaceActualMotorAtSpawn();
            actor.BindAudio(adapter.Settings); actor.Prepare(species,Array.Empty<SportsEventTruck>(),0,7919+pass);
            actor.SetIntent(DragonCharacterIntent.PlayerMounted,adapter.Center);
            int temporaryIndex=0;
            foreach(var animal in sheep) if(animal!=null && animal.temporary && !animal.gameObject.activeSelf && adapter.TryGround(adapter.Center+new Vector3(temporaryIndex++==0?-12:12,0,12),out var spawn))animal.AcquireTemporary(this,spawn+Vector3.up*.65f,temporaryIndex);
            target = FindTarget();
            Vector3 point = target >= 0 ? adapter.Body(target).position : adapter.Center;
            int dropSlot=target>=0?target:(pass%adapter.Count);
            bool grounded = TryDropGround(dropSlot,out var ground);
            pass++; phaseAge = 0; actionAccepted = contactSeen = false;
            if (pass % 3 == 0 && (sheep.Count > 0 || adapter.cheese!=null&&adapter.cheese.sheep.Length>0 || adapter.keepaway!=null&&adapter.keepaway.chaosField!=null&&adapter.keepaway.chaosField.sheep.Length>0))
            { meal = SelectPrey(); phase = Phase.SheepApproach; RecordSheepEvent("approach"); Warning = "DRAGON APPROACHING SHEEP · KEEP CLEAR"; }
            // Three-balloon matches can end before a second pass. Show the
            // optional physical drop on their first ordinary safe pass.
            else if ((pass % 2 == 0 || pass == 1 && (adapter.sport == DragonMayhemSport.BalloonBattle||adapter.sport==DragonMayhemSport.CheeseRolling)) && grounded)
            { phase = Phase.DropApproach; dropGround=ground; goal = ground + Vector3.up * 14; Warning = "GOLD PASS · PHYSICAL DROP AHEAD"; }
            else if (target >= 0 && !adapter.Protected(point))
            { phase = Phase.Approach; goal = point + Vector3.up * configuration.approachHeight - adapter.Body(target).transform.forward * 14; Warning = "DRAGON INBOUND · SMALL GUST WARNING"; }
            else Depart("DRAGON FLYOVER · PROTECTED START");
            actor.motor.TryAction(DragonAnimationAction.Roar,8);
        }
        void EndPass()
        {
            if(pickup.Has_Item)pickup.DropItem();
            if(meal!=null && meal.keepaway!=null && !meal.Consumed)meal.keepaway.ExternalCarryHeld=false;
            actor.ResetCharacter();actor.gameObject.SetActive(false);passActive=false;Warning="";
            nextPass=clock+configuration.passCooldown;
        }
        int FindTarget() { for(int i=0;i<adapter.Count;i++) { int candidate=(nextTarget+i)%adapter.Count; if(adapter.Eligible(candidate)) { nextTarget=(candidate+1)%adapter.Count; return candidate; } } return -1; }
        void FlyTo(Vector3 point,float dt)
        {
            var direction=point-actor.Position;
            if(direction.sqrMagnitude<1) { actor.motor.MoveWorld(Vector3.zero,false,true); return; }
            // Vendor flight retains its normal collision and bank authority.
            actor.motor.MoveWorld(Vector3.ClampMagnitude(direction/35,1),false,true);
        }
        void PlaceActualMotorAtSpawn()
        {
            Vector3 anchor=adapter.sport==DragonMayhemSport.CheeseRolling?adapter.DropCandidate(Mathf.Clamp(nextTarget,0,Mathf.Max(0,adapter.Count-1))):adapter.Center;
            Vector3 position=anchor+configuration.spawnOffset;
            if(adapter.sport==DragonMayhemSport.CheeseRolling)
            {
                // Stage this new optional pass beside its actual runout. Do not
                // start300m uphill after a short ordinary race is already over.
                // Keep it above any real static hill/structure at that location.
                float highest=float.NegativeInfinity;
                foreach(var hit in Physics.RaycastAll(position+Vector3.up*1000,Vector3.down,2000,~0,QueryTriggerInteraction.Ignore))
                    if(hit.collider.attachedRigidbody==null&&!hit.transform.IsChildOf(actor.transform))highest=Mathf.Max(highest,hit.point.y);
                if(float.IsFinite(highest))position.y=Mathf.Max(position.y,highest+configuration.spawnOffset.y);
            }
            Vector3 forward=Vector3.ProjectOnPlane(anchor-position,Vector3.up);
            actor.motor.body.position=position;actor.motor.body.rotation=Quaternion.LookRotation(forward.sqrMagnitude>.01f?forward:Vector3.forward,Vector3.up);
        }
        bool TryDropGround(int slot,out Vector3 ground)
        {
            Vector3 candidate=adapter.DropCandidate(slot);if(SafeDropGround(candidate,out ground))return true;
            // A moving Cheese/crew can protect the single nominal point. Find
            // a nearby genuinely supported permitted point, never a guessed Y.
            foreach(var offset in new[]{Vector3.right*12,Vector3.left*12,Vector3.right*24,Vector3.left*24,Vector3.back*12})if(SafeDropGround(candidate+offset,out ground))return true;
            ground=default;return false;
        }
        bool SafeDropGround(Vector3 point,out Vector3 ground)
        {
            if(!adapter.TryGround(point,out ground))return false;
            // Include the actual three release positions and their solid radii.
            return adapter.TryGround(point+Vector3.right,out _)&&adapter.TryGround(point-Vector3.right,out _);
        }
        void UpdateDropApproachGoal()
        {
            Vector3 contactOffset=actor.motor.transform.TransformVector(configuration.dropMouthLocalOffset);
            goal=dropGround+Vector3.up*14-Vector3.ProjectOnPlane(contactOffset,Vector3.up);
        }
        void OnActionContact(DragonAnimationAction action)
        {
            if (!Active) return;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("MAYHEM_ACTUAL_CONTACT sport="+adapter.SportId+" phase="+phase+" action="+action+" time="+actor.motor.ActionNormalizedTime.ToString("F4")+" body="+actor.Position.ToString("F3")+" mouth="+actor.mouth.position.ToString("F3")+" grip="+actor.leftGrip.position.ToString("F3")+" meal="+(meal==null?"none":meal.transform.position.ToString("F3"))+" gripDistance="+(meal==null?-1:Vector3.Distance(actor.leftGrip.position,meal.transform.position))+" mouthDistance="+(meal==null?-1:Vector3.Distance(actor.mouth.position,meal.transform.position)));
#endif
            if (phase == Phase.Attack && action == DragonAnimationAction.Breath)
            {
                contactSeen = true;
                if (target < 0 || !adapter.Eligible(target)) return;
                var body=adapter.Body(target); var delta=body.worldCenterOfMass-actor.mouth.position;
                if(delta.magnitude>32 || Vector3.Angle(actor.mouth.forward,delta)>36 || adapter.Protected(body.position) || !ClearLine(actor.mouth.position,body.worldCenterOfMass,body)) return;
                body.AddForce(Vector3.ProjectOnPlane(delta,Vector3.up).normalized*configuration.gustVelocityChange,ForceMode.VelocityChange);
                PhysicalAttackContacts++;
            }
            else if (phase == Phase.Drop && action == DragonAnimationAction.Drop)
            {
                contactSeen=true;
                if(!SafeDropGround(actor.mouth.position,out var ground) || ground.y>actor.mouth.position.y-2)
                {Debug.Log("MAYHEM_DROP_REJECT sport="+adapter.SportId+" reason=actual-mouth-release-footprint-outside-supported-permitted-area mouth="+actor.mouth.position.ToString("F3")+" body="+actor.Position.ToString("F3")+" intendedGround="+dropGround.ToString("F3"));return;}
                foreach(var c in Physics.OverlapSphere(actor.mouth.position,.7f,~0,QueryTriggerInteraction.Ignore))if(!c.transform.IsChildOf(actor.transform)){Debug.Log("MAYHEM_DROP_REJECT sport="+adapter.SportId+" reason=actual-mouth-overlap collider="+c.name);return;}
                for(int i=0;i<3;i++) foreach(var coin in gold) if(!coin.gameObject.activeSelf)
                { coin.Release(this,++nextLease,actor.mouth.position+Vector3.right*(i-1)*.6f,Vector3.ClampMagnitude(Vector3.ProjectOnPlane(actor.motor.Velocity,Vector3.up)*.1f,.8f)+new Vector3((i-1)*.8f,-1,.2f)); PhysicalGoldDrops++; break; }
            }
            else if (phase == Phase.Grab && action == DragonAnimationAction.GroundGrab)
            {
                if(meal==null||!meal.Available){Debug.Log("MAYHEM_GROUND_GRAB_REJECT reason=prey-unavailable id="+CurrentSheepId);return;}
                float gripDistance=Vector3.Distance(actor.leftGrip.position,meal.transform.position);
                if(gripDistance>3){Debug.Log("MAYHEM_GROUND_GRAB_REJECT reason=actual-palm-range id="+CurrentSheepId+" distance="+gripDistance+" grip="+actor.leftGrip.position.ToString("F3")+" prey="+meal.transform.position.ToString("F3"));return;}
                if(!ClearLine(actor.leftGrip.position,meal.transform.position,meal.item.RigidBody,true))return;
                if(meal.keepaway!=null)meal.keepaway.ExternalCarryHeld=true;
                pickup.FocusItem(meal.gameObject); pickup.PickUpItem();
                if(!pickup.Has_Item || !meal.item.IsPicked) {Debug.Log("MAYHEM_GROUND_GRAB_REJECT reason=vendor-pickup-refused id="+CurrentSheepId+" hasItem="+pickup.Has_Item+" isPicked="+meal.item.IsPicked+" itemActive="+meal.item.Active+" gripDistance="+gripDistance);if(meal.keepaway!=null)meal.keepaway.ExternalCarryHeld=false;return;}
                SheepGrabs++; RecordSheepEvent("grab-contact"); contactSeen=true;
                phase=Phase.Carry; phaseAge=feedAge=0;carryStarted=false; goal=actor.Position+Vector3.up*14+actor.motor.transform.forward*15;
                handoffStart=mouthHolder.InverseTransformPoint(meal.transform.position);
                actor.motor.InterruptAction();
            }
            else if (phase == Phase.Eat && action == DragonAnimationAction.Eat)
            {
                if(meal==null || !pickup.Has_Item || Vector3.Distance(actor.mouth.position,meal.transform.position)>3) return;
                contactSeen=true; bool explosive=meal.Explosive;
                pickup.DropItem(); meal.Consume(); SheepMeals++; LastConsumedSheepId=CurrentSheepId;
                if(explosive)ConsumedExplosiveMeals++;
                RecordSheepEvent("eat-contact-consumed");
                if(explosive) Warning="EXPLOSIVE SHEEP · DRAGON RETREATS";
            }
        }
        bool HasGroundGrabContactGeometry()
        {
            if(configuration==null||species==null||!species.Supports(DragonSpeciesRole.Ground)||configuration.groundGrabSpeciesId!=species.id||
                !species.TryGetAnimation(DragonAnimationAction.GroundGrab,out var mapping)||configuration.groundGrabContactClip!=mapping.clip)return false;
            Vector3 offset=configuration.groundGrabGripLocalOffset;
            if(!float.IsFinite(offset.x)||!float.IsFinite(offset.y)||!float.IsFinite(offset.z)||offset.sqrMagnitude<.01f)return false;
            foreach(var binding in actor.motor.actionBindings)if(binding!=null&&binding.action==DragonAnimationAction.GroundGrab&&Mathf.Approximately(binding.contactNormalizedTime,configuration.groundGrabContactNormalizedTime))return true;
            return false;
        }
        bool ClearLine(Vector3 from,Vector3 to,Rigidbody targetBody,bool reportGroundGrab=false)
        {
            foreach(var hit in Physics.RaycastAll(from,(to-from).normalized,(to-from).magnitude,~0,QueryTriggerInteraction.Ignore))
                if(!hit.collider.transform.IsChildOf(actor.transform) && hit.collider.attachedRigidbody!=targetBody && (meal==null || !hit.collider.transform.IsChildOf(meal.transform)))
                {if(reportGroundGrab)Debug.Log("MAYHEM_GROUND_GRAB_REJECT reason=actual-line-blocked id="+CurrentSheepId+" collider="+hit.collider.name+" root="+hit.collider.transform.root.name+" distance="+hit.distance+" point="+hit.point.ToString("F3"));return false;}
            return true;
        }
        public bool TryCollect(DragonMayhemGold coin,int slot)
        {
            if(!Active || coin==null || coin.Owner!=this || !coin.gameObject.activeSelf || coin.Collected || !adapter.CanCollect(slot) || SideGold[slot]>=configuration.maximumSideGold || !collected.Add(coin.Lease)) return false;
            SideGold[slot]++; return true;
        }
        void PreparePickup()
        {
            var helper=new GameObject("Malbers animated sheep pickup"); helper.SetActive(false); helper.transform.SetParent(actor.leftGrip,false);UnitWorldScale(helper.transform);
            var area=helper.AddComponent<SphereCollider>(); area.radius=3; area.isTrigger=true;
            pickup=helper.AddComponent<MPickUp>(); pickup.PickUpArea=area; pickup.Holder=helper.transform;pickup.extraHolders??=new List<MPickUp.ExtraHolder>(); pickup.IgnoreItemPickDelay.Value=true; pickup.IgnoreItemDropDelay.Value=true; helper.SetActive(true);
            mouthHolder=new GameObject("Mayhem unit-scale animated mouth holder").transform;mouthHolder.SetParent(actor.mouth,false);UnitWorldScale(mouthHolder);
        }
        static void UnitWorldScale(Transform holder){var scale=holder.parent.lossyScale;holder.localScale=new Vector3(1/Mathf.Max(.0001f,Mathf.Abs(scale.x)),1/Mathf.Max(.0001f,Mathf.Abs(scale.y)),1/Mathf.Max(.0001f,Mathf.Abs(scale.z)));}
        void PrepareSheep()
        {
            sheep.Clear();
            // Original prey is chosen lazily from the real current arena.
            // No extra original sheep, immunity, or base movement owner exists.
            for(int i=0;i<configuration.temporarySheep;i++)
            {
                if(!adapter.TryGround(adapter.Center+new Vector3(i==0?-12:12,0,12),out var point)) continue;
                var animal=Instantiate(sheepPrefab,runtime.transform);
                var entry=animal.GetComponent<DragonMayhemSheep>(); entry.AcquireTemporary(this,point+Vector3.up*.65f,i); RegisterSheep(entry);
                IgnoreOriginalDynamics(animal.GetComponentsInChildren<Collider>(true));
            }
        }
        DragonMayhemSheep SelectPrey()
        {
            GameObject best=null;LWS.TruckCheeseRolling.CheeseSheep cheese=null;LWS.TruckKeepaway.KeepawaySheep keepaway=null;float bestScore=float.NegativeInfinity;
            void Consider(GameObject candidate,LWS.TruckCheeseRolling.CheeseSheep source,LWS.TruckKeepaway.KeepawaySheep keep)
            {
                if(candidate==null||!candidate.activeInHierarchy||adapter.Protected(candidate.transform.position)||!adapter.TryGround(candidate.transform.position,out _))return;
                var existing=candidate.GetComponent<DragonMayhemSheep>();
                if(existing!=null&&!existing.Available||existing==null&&candidate.GetComponent<Pickable>()!=null)return;
                float separation=float.PositiveInfinity;
                for(int slot=0;slot<adapter.Count;slot++)
                {
                    var truck=adapter.Body(slot);if(truck==null||!truck.gameObject.activeInHierarchy)continue;
                    separation=Mathf.Min(separation,Vector3.Distance(candidate.transform.position,truck.position),Vector3.Distance(candidate.transform.position,truck.position+Vector3.ClampMagnitude(truck.linearVelocity*.75f,18)));
                }
                if(separation<8)return;
                float travel=Vector3.Distance(actor.Position,candidate.transform.position);
                float score=Mathf.Min(separation,45)*2-travel*.2f;
                if(score<=bestScore)return;
                best=candidate;cheese=source;keepaway=keep;bestScore=score;selectedPreySeparation=separation;selectedPreyTravel=travel;
            }
            // Prefer an isolated real original explosive prey when that sport
            // supplies one; temporary ordinary snacks remain a fallback.
            if(adapter.cheese!=null)foreach(var source in adapter.cheese.sheep)if(source!=null&&!source.Queued&&!source.Detonated)Consider(source.gameObject,source,null);
            if(adapter.keepaway!=null&&adapter.keepaway.chaosField!=null)foreach(var source in adapter.keepaway.chaosField.sheep)if(source!=null&&source.Active&&!source.Queued)Consider(source.gameObject,null,source);
            if(best==null)foreach(var candidate in sheep)if(candidate!=null&&candidate.temporary&&candidate.Available)Consider(candidate.gameObject,null,null);
            if(best==null)return null;
            var entry=best.GetComponent<DragonMayhemSheep>();
            if(entry==null){AddExistingSheep(best,cheese,keepaway);entry=best.GetComponent<DragonMayhemSheep>();}
            return entry;
        }
        void AddExistingSheep(GameObject original,LWS.TruckCheeseRolling.CheeseSheep source,LWS.TruckKeepaway.KeepawaySheep keep)
        {
            if(original.GetComponent<Pickable>()!=null || original.GetComponent<DragonMayhemSheep>()!=null) return;
            if(original.GetComponent<Rigidbody>()==null) { var b=original.AddComponent<Rigidbody>(); b.isKinematic=true; addedOriginalComponents.Add(b); }
            var item=original.AddComponent<Pickable>(); item.ByAnimation=true; addedOriginalComponents.Add(item);
            var entry=original.AddComponent<DragonMayhemSheep>(); entry.item=item; entry.cheese=source; entry.keepaway=keep; addedOriginalComponents.Add(entry); RegisterSheep(entry);
            originalSheep.Add(new OriginalSheepState { entry=entry,parent=original.transform.parent,position=original.transform.position,rotation=original.transform.rotation,localScale=original.transform.localScale });
        }
        void IgnoreOriginalDynamics(Collider[] own)
        {
            foreach(var a in own) if(a!=null && !a.isTrigger) foreach(var b in originalDynamic) if(b!=null && !b.isTrigger && a!=b) Physics.IgnoreCollision(a,b,true);
            // Physical gold and animals can bounce on ordinary ground. They
            // cannot become a new cornhole board/hole/trampoline obstruction.
            if(adapter.cornhole!=null) foreach(var course in adapter.cornhole.courses)
            {
                foreach(var b in course.board.GetComponentsInChildren<Collider>(true)) foreach(var a in own) if(a!=null && !a.isTrigger && b!=null && a!=b) Physics.IgnoreCollision(a,b,true);
                foreach(var b in course.trampoline.GetComponentsInChildren<Collider>(true)) foreach(var a in own) if(a!=null && !a.isTrigger && b!=null && a!=b) Physics.IgnoreCollision(a,b,true);
            }
        }
        readonly Dictionary<DragonMayhemGold,Vector3[]> frozenGold=new Dictionary<DragonMayhemGold,Vector3[]>();
        void FreezeGold(bool freeze)
        {
            if(gold==null)return;
            if(freeze) { frozenGold.Clear(); foreach(var c in gold) if(c.gameObject.activeSelf) { frozenGold[c]=new[]{c.body.linearVelocity,c.body.angularVelocity}; c.body.collisionDetectionMode=CollisionDetectionMode.Discrete;c.body.isKinematic=true; } }
            else { foreach(var pair in frozenGold) if(pair.Key!=null && pair.Key.gameObject.activeSelf) { pair.Key.body.isKinematic=false;pair.Key.body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic; pair.Key.body.linearVelocity=pair.Value[0]; pair.Key.body.angularVelocity=pair.Value[1]; } frozenGold.Clear(); }
        }
        void RegisterSheep(DragonMayhemSheep entry) { sheep.Add(entry);sheepIds.Add(entry,++nextSheepId); }
        void RecordSheepEvent(string kind)
        {
            if(sheepEvents.Count==64)sheepEvents.RemoveAt(0);
            sheepEvents.Add("clock="+clock.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+" pass="+pass+" event="+kind+" id="+CurrentSheepId+" original="+(meal!=null&&meal.Explosive)+" consumed="+(meal!=null&&meal.Consumed)+" detonated="+(meal!=null&&meal.Detonated)+" picked="+(meal!=null&&meal.item.IsPicked)+" selectedTruckSeparation="+selectedPreySeparation+" selectedTravel="+selectedPreyTravel+" position="+(meal==null?"none":meal.transform.position.ToString("F3")));
        }
        void ReactToExplosion()
        {
            ExplosiveReactions++;
            if(meal!=null&&meal.Consumed&&meal.Explosive&&consumedExplosions.Add(CurrentSheepId))
            {ConsumedExplosiveReactions++;LastConsumedExplosionSheepId=CurrentSheepId;}
            RecordSheepEvent("observed-original-detonation");
            pickup?.DropItem(); actor.motor.InterruptAction(); actor.motor.TryAction(DragonAnimationAction.Damage,3); actor.motor.body.AddForce((Vector3.up-actor.motor.transform.forward)*2,ForceMode.VelocityChange); Depart("EXPLOSIVE SHEEP · DRAGON RETREAT"); meal=null;
        }
        void Depart(string notice)
        {
            if(meal!=null)RecordSheepEvent("depart");
            if(pickup!=null && pickup.Has_Item) pickup.DropItem();
            if(meal!=null && meal.keepaway!=null && !meal.Consumed)meal.keepaway.ExternalCarryHeld=false;
            phase=Phase.Depart; phaseAge=0; Warning=notice; departure=actor.Position+configuration.departureOffset;
        }
        public string BonusText()
        {
            var text=new StringBuilder(configuration.variantName+" · "+configuration.sideBonusLabel+"\n");
            for(int i=0;i<adapter.Count;i++) text.Append(adapter.Count>4?"P"+(i+1):adapter.ParticipantName(i)).Append("  ").Append(SideGold[i]).Append(" / ").Append(configuration.maximumSideGold).Append(adapter.Count>4?" · ":"\n");
            return text.ToString();
        }
        public string ResultBonusText()
        {
            var text=new StringBuilder("MAYHEM SIDE GOLD · ");
            for(int i=0;i<adapter.Count;i++) { if(i>0)text.Append("  •  "); text.Append(adapter.Count>4?"P"+(i+1):adapter.ParticipantName(i)).Append(' ').Append(SideGold[i]).Append('/').Append(configuration.maximumSideGold); }
            string victory = adapter.sport == DragonMayhemSport.Keepaway ? "Reach the possession target to win." :
                adapter.sport == DragonMayhemSport.BalloonBattle ? "The last truck with balloons wins." :
                adapter.sport == DragonMayhemSport.Cornhole ? "Board and hole points decide the winner." :
                "First to five cheese catches wins.";
            return text.Append('\n').Append(victory).ToString();
        }
        public void Cleanup()
        {
            if(pickup!=null && pickup.Has_Item) pickup.DropItem();
            if(actor!=null) { actor.motor.ActionContact-=OnActionContact; actor.Pause(false); }
            foreach(var saved in originalSheep) if(saved.entry!=null)
            {
                saved.entry.transform.SetParent(saved.parent,true);
                saved.entry.transform.localScale=saved.localScale;
                if(saved.entry.keepaway!=null)saved.entry.keepaway.ExternalCarryHeld=false;
                if(!saved.entry.Consumed && !saved.entry.Detonated) saved.entry.transform.SetPositionAndRotation(saved.position,saved.rotation);
            }
            originalSheep.Clear();
            // Pickable.Drop deliberately unparents the held object. Temporary
            // animals must therefore be explicitly reclaimed after a cancelled
            // pass, including animals already delivered to the mouth.
            foreach(var animal in sheep) if(animal!=null && animal.temporary) { animal.gameObject.SetActive(false); Destroy(animal.gameObject); }
            foreach(var c in addedOriginalComponents) if(c!=null) Destroy(c);
            addedOriginalComponents.Clear(); sheep.Clear(); sheepIds.Clear(); frozenGold.Clear();
            if(runtime!=null) { runtime.SetActive(false); Destroy(runtime); }
            runtime=null; actor=null; pickup=null; meal=null; gold=null; Warning=""; wasPaused=false;passActive=false;
        }
        void OnDisable()=>Cleanup();
        void OnDestroy()=>Cleanup();
    }
}
