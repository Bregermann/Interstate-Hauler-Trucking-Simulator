using System;
using System.Collections.Generic;
using System.Linq;
using LWS.TruckBowling;
using LWS.TruckDragonKingdom;
using LWS.TruckJenga;
using LWS.TruckSports;
using UnityEngine;

namespace LWS.TruckHoardHeist
{
    public sealed partial class HoardHeistMatch : DragonKingdomGroundMatch
    {
        public HoardTreasure[] treasure = Array.Empty<HoardTreasure>();
        public BoxCollider[] cargoRacks = new BoxCollider[4];
        public JengaCrane[] hoists = new JengaCrane[4];
        public HoardHoistRig[] hoistRigs = new HoardHoistRig[4];
        public BoxCollider extractionZone;
        public Transform extractionApproach, guardianHome;
        public Rigidbody[] caveDebris = Array.Empty<Rigidbody>();
        public HoardPottery[] pottery = Array.Empty<HoardPottery>();
        public AudioSource sleepRumble;
        public AudioClip awakeningRoar;
        public bool hasSleepVigil;
        public Transform[] quietCoverGoals = new Transform[4];
        [Range(1, 4)] public int cargoCapacity = 3;
        [Min(1)] public float capacityKg = 350;
        [Min(1)] public float pickupReach = 9;
        public HoardHeistRules Rules { get; private set; }
        public HoardAlertness Alert { get; private set; }
        public HoardAlertStage AlertStage { get; private set; }
        public int PhysicalLoads { get; private set; }
        public int PhysicalDeliveries { get; private set; }
        public int ActualNoiseEvents { get; private set; }
        public int[] Targets { get; } = { -1, -1, -1, -1 };
        public float SecondsRemaining => Mathf.Max(0, RoundSeconds - Clock);
        public float RoundSeconds => option == 0 ? 180 : option == 1 ? 300 : 480;
        readonly int[] processing = { -1, -1, -1, -1 };
        readonly bool[] unloading = new bool[4];
        readonly bool[] returningHoist = new bool[4];
        readonly bool[] hiding = new bool[4];
        readonly float[] settle = new float[4];
        readonly float[] hoistStartedAt = new float[4];
        readonly Vector3[] hoistDestination = new Vector3[4];
        readonly float[,] blockedUntil = new float[4,64];
        readonly Vector3[,] blockedFrom = new Vector3[4,64];
        readonly Dictionary<long, float> collisionNoise = new Dictionary<long, float>();
        readonly Dictionary<Rigidbody, Pose> rubbleStarts = new Dictionary<Rigidbody, Pose>();
        readonly RaycastHit[] hits = new RaycastHit[64];
        DragonCharacter guardian;
        DragonEyelidPresentation guardianEyes;
        public bool GuardianVisionAvailable=>guardianEyes!=null&&guardianEyes.EyesOpenForVision;
        Vector3 lastSensedPosition;
        float lastPerceptionAt, searchStarted, nextStir;
        bool roared, rubbleReleased, stirring, stirRequested, stirObserved;
        bool homeLandingNeeded, homeLandingRequested;
        int homeLandingBaseline;

        public override string Title => "TRUCK DRAGON HOARD HEIST";
        public override string Help => "Steal actual treasure from the Gilded Deep. Feather the throttle, use cover and avoid horns/crashes. Stop near a treasure and press ACTION to operate the physical hoist; ACTION away from treasure releases a carried item. Each rack holds three items and 770 LB. Drive the actual loaded truck into reception and stop to bank its treasure. Only original objects physically received there score. Unbanked cargo earns nothing when the heist closes.";
        public override string OptionLabel => option == 0 ? "THREE MINUTE HEIST" : option == 1 ? "FIVE MINUTE HEIST" : "EIGHT MINUTE HEIST";
        public override int OptionCount => 3;
        public override string ControlCaption(int slot) => Controls(slot) + " · LIFT / RELEASE CARGO";
        public override string Header => "HOARD HEIST · " + Mathf.CeilToInt(SecondsRemaining) + "s · ALERT " + (Alert != null ? Alert.Value.ToString("0") : "0") + "%";
        public override string Status => Announcement;
        public override string Standings => Rules == null ? "PHYSICAL TREASURE ONLY" : string.Join("  ·  ", Enumerable.Range(0, setup.count).Select(i => setup.players[i].name + " " + Rules.Scores[i]));
        public override string Results
        {
            get
            {
                if (Rules == null) return "NO TREASURE RECEIVED";
                int best = Rules.Scores.Max();
                string leaders = string.Join(" & ", Enumerable.Range(0, setup.count).Where(i => Rules.Scores[i] == best).Select(i => setup.players[i].name));
                return leaders + (Enumerable.Range(0, setup.count).Count(i => Rules.Scores[i] == best) > 1 ? " · SHARED TOP VALUE\n" : " · HOARD HEIST WINNER\n") + Standings + "\n" + Rules.DeliveredCount + " original treasures received. Unbanked cargo did not score.";
            }
        }
        public override bool TryReadScoreCard(int card, out string value, out string detail)
        {
            value = detail = null;
            if (Rules == null || card < 0 || card >= setup.count) return false;
            value = Rules.Scores[card] + " VALUE";
            detail = "CARGO " + Rules.CargoValue(card) + " · " + Rules.LoadCount(card) + "/" + Rules.CapacitySlots + " · " + (Rules.LoadKg(card) * 2.204623f).ToString("0") + " LB";
            return true;
        }
        public override float ParticipantProgress(int slot) => Rules == null || slot < 0 || slot >= setup.count ? -1 : Rules.LoadCount(slot) / (float)Rules.CapacitySlots;
        public override void DescribeBroadcast(SportsBroadcastAction action, SportsBroadcastFraming framing)
        {
            if (guardian == null || Rules == null || framing == null) return;
            bool waking = guardian.motor.ActionPlaying && (guardian.motor.CurrentAction == DragonAnimationAction.Wake || guardian.motor.CurrentAction == DragonAnimationAction.Roar);
            if (waking)
            {
                int nearby = framing.Nearest(guardian.Position,!AllAI);
                if (nearby >= 0) { action.primary=nearby;action.Objective(guardian.mouth.position,7);action.reason="THE DRAGON AWAKENS"; }
                return;
            }
            int pursued=guardian.TargetSlot;
            if (guardian.Intent==DragonCharacterIntent.Pursuit && pursued>=0 && pursued<setup.count && (AllAI||setup.players[pursued].source!=BowlingSource.AI))
            { action.primary=pursued;action.Objective(guardian.Position+Vector3.up*5,5);action.reason="ESCAPE WITH THE TREASURE";return; }
            for(int slot=0;slot<setup.count;slot++)
                if(processing[slot]>=0&&hoists[slot].Load!=null&&(AllAI||setup.players[slot].source!=BowlingSource.AI))
                { action.primary=slot;action.Objective(hoists[slot].Load.position,1);action.reason="PHYSICAL TREASURE HOIST";return; }
        }

        protected override void PrepareObjectives()
        {
            startingRegionId = "caverns";
            if (treasure.Length < 8 || treasure.Length > 64 || cargoRacks.Length != 4 || hoists.Length != 4 || quietCoverGoals.Length != 4 || quietCoverGoals.Any(t => t == null) || !hasSleepVigil || extractionZone == null || extractionApproach == null || guardianHome == null || dragons.Length < 1)
                throw new InvalidOperationException("Heist requires an authored original treasure inventory, four real cargo hoists/racks, extraction and sleeping-guardian anchors.");
            guardian = dragons[0];
            guardianEyes=guardian.motor.GetComponent<DragonEyelidPresentation>();
            if(guardianEyes==null)throw new InvalidOperationException("The actual sleeping guardian requires its real eyelid presentation before sensing.");
            if(guardianEyes.sleepingPeekClips==null||guardianEyes.sleepingPeekClips.Length==0)throw new InvalidOperationException("Explicitly bind the existing Heist sleeping-vigilance peek before starting this saved scene.");
            for (int i = 0; i < 4; i++)
            {
                if (cargoRacks[i] == null || hoists[i] == null || hoists[i].spreader == null) throw new InvalidOperationException("Four configured physical hoists/racks required.");
                StopHoist(i); processing[i] = Targets[i] = -1; unloading[i] = returningHoist[i] = hiding[i] = false; settle[i] = 0;
            }
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in treasure)
            {
                if (item == null || item.body == null || item.shape == null || string.IsNullOrWhiteSpace(item.stableId) || !ids.Add(item.stableId)) throw new InvalidOperationException("Treasure needs unique IDs and its actual original body/shape.");
                item.match = this; item.ResetTreasure();
            }
            foreach (var pot in pottery) if (pot != null) pot.ResetPottery();
            Rules = new HoardHeistRules(setup.count, cargoCapacity, capacityKg, treasure.Select(t => t.value).ToArray(), treasure.Select(t => t.body.mass).ToArray(), treasure.Select(t => t.body.GetInstanceID()).ToArray());
            Alert = new HoardAlertness(); AlertStage = HoardAlertStage.Sleeping;
            PhysicalLoads = PhysicalDeliveries = ActualNoiseEvents = 0; collisionNoise.Clear();ResetTreasureAccess();
            Array.Clear(blockedUntil,0,blockedUntil.Length);
            lastSensedPosition = guardianHome.position; lastPerceptionAt = searchStarted = 0; roared = rubbleReleased = false;
            stirring = stirRequested = stirObserved = false; nextStir = 5;
            homeLandingNeeded = homeLandingRequested = false; homeLandingBaseline = 0;
            foreach (var body in caveDebris)
            {
                if (body == null) continue;
                if (!rubbleStarts.TryGetValue(body, out var pose)) { pose = new Pose(body.position, body.rotation); rubbleStarts.Add(body, pose); }
                if (!body.isKinematic) body.linearVelocity = body.angularVelocity = Vector3.zero;
                body.isKinematic = true; body.position = pose.position; body.rotation = pose.rotation;
            }
            Announcement = "THE ANCIENT DRAGON IS SLEEPING · STEAL QUIETLY";
        }

        protected override void BeginDragonIntents()
        {
            if (!species.TryGet(dragonSpeciesIds[0], out var definition) || !definition.Supports(DragonSpeciesRole.HoardGuardian) || !definition.HasAnimation(DragonAnimationAction.Sleep) || !definition.HasAnimation(DragonAnimationAction.Wake))
                throw new InvalidOperationException("Heist requires the real configured sleeping/waking ground guardian.");
            guardian.SleepingWakeEligible = slot => Alert != null && Alert.WakeAllowed;
            guardian.SetIntent(DragonCharacterIntent.SleepingGuardian, guardianHome.position);
            guardian.TruckHit -= GuardianHit; guardian.TruckHit += GuardianHit;
            guardian.motor.ActionContact -= GuardianActionContact; guardian.motor.ActionContact += GuardianActionContact;
            guardian.motor.ActionStarted -= GuardianActionStarted; guardian.motor.ActionStarted += GuardianActionStarted;
            for (int i = 1; i < dragons.Length; i++) dragons[i].SetIntent(DragonCharacterIntent.Passive, dragons[i].Position);
            for (int i = 0; i < setup.count; i++) ParkHoist(i);
            if (sleepRumble != null) { sleepRumble.loop = true; sleepRumble.volume = .18f * settings.sfx; sleepRumble.Play(); }
        }

        protected override void Simulate(float dt)
        {
            base.Simulate(dt);
            if(!Live||Intro>0)return;
            // Intentional finite withdrawal bypasses route.Update/Drive entirely.
            // NWH still receives only the established physical control command.
            for(int slot=0;slot<setup.count;slot++)
                if(IsDriving(slot)&&setup.players[slot].source==BowlingSource.AI&&!conditions[slot].Stunned&&!HoistBusy(slot)&&trafficYieldPhase[slot]>0&&trafficYieldPhase[slot]<3&&!trafficHolding[slot])
                    trucks[slot].TickDrive(FilterDriveCommand(slot,trafficYieldDrive[slot],dt));
        }

        protected override void TickObjectives(float dt)
        {
            TickGuardian(dt);
            for (int i = 0; i < treasure.Length; i++)
            {
                var item = treasure[i];
                if (Rules.States[i] == HoardCargoState.Carried && item.CargoJoint == null)
                {
                    int owner = Rules.Owners[i]; item.Release(); Rules.Drop(i, owner, true);
                    Announcement = "TREASURE SPILLED · ANY CREW CAN RECOVER IT";
                    ReportNoiseEvent(owner, item.body.position, 1.2f, "SPILLED TREASURE");
                }
                if (item.body.position.y < -80 || Mathf.Abs(item.body.position.x) > 10000 || Mathf.Abs(item.body.position.z) > 10000)
                {
                    if (Rules.Lose(i, true))
                    {
                        item.Release(); item.gameObject.SetActive(false);
                        for (int slot = 0; slot < setup.count; slot++) if (processing[slot] == i) { StopHoist(slot); processing[slot] = -1; unloading[slot] = false;returningHoist[slot]=true; }
                    }
                }
            }
            for (int slot = 0; slot < setup.count; slot++)
            {
                if (!IsDriving(slot)) continue;
                TickHoist(slot, dt);
                if (HoistBusy(slot)) continue;
                if (TryBank(slot)) continue;
                bool ai = setup.players[slot].source == BowlingSource.AI;
                if (ai)
                {
                    if (!Alert.Awakened && Alert.Value >= 45 && Rules.CargoValue(slot) == 0) hiding[slot] = true;
                    if (Alert.Awakened || Alert.Value <= 18) hiding[slot] = false;
                }
                Targets[slot] = ChooseTreasure(slot);
                bool pressed = actions[slot]; actions[slot] = false;
                bool extract = ShouldExtract(slot);
                if (ai && extract) Targets[slot] = -1;
                bool collect = ai ? !extract && !hiding[slot] && trafficYieldPhase[slot]==0 && !trafficHolding[slot] : pressed;
                if (collect && Targets[slot] >= 0)
                {
                    if (Vector3.Distance(trucks[slot].body.position, treasure[Targets[slot]].body.position) < pickupReach && trucks[slot].body.linearVelocity.magnitude < .7f) TryLift(slot, Targets[slot]);
                }
                else if (!ai && pressed) ReleaseLast(slot);
            }
            if (Rules.Exhausted || SecondsRemaining <= 0 || Enumerable.Range(0, setup.count).All(i => Retired[i])) EndMatch();
        }

        void TickGuardian(float dt)
        {
            bool visible = false;
            for (int i = 0; GuardianVisionAvailable && i < setup.count; i++)
                if (IsDriving(i) && guardian.CanObserveTarget(i))
                {
                    visible = true; lastPerceptionAt = Clock;
                    // This is an actual LOS witness, not an omniscient route.
                    lastSensedPosition = trucks[i].body.worldCenterOfMass;
                }
            Alert.Step(dt, visible);
            AlertStage = Alert.Stage;
            if (!Alert.Awakened && !Alert.WakeAllowed && !stirring && guardian.Intent == DragonCharacterIntent.SleepingGuardian && Clock >= nextStir)
            {
                stirring = true; stirRequested = stirObserved = false;
                guardian.SetIntent(DragonCharacterIntent.Passive, guardianHome.position);
            }
            if (stirring)
            {
                if (Alert.WakeAllowed)
                {
                    stirring = false; guardian.SetIntent(DragonCharacterIntent.SleepingGuardian, guardianHome.position);
                }
                else
                {
                    if (!stirRequested && !guardian.motor.ActionPlaying) stirRequested = guardian.motor.TryAction(DragonAnimationAction.LookAround,.5f);
                    if (stirRequested && guardianEyes.ObservedPeekPlayback) stirObserved = true;
                    if (stirObserved && !guardian.motor.ActionPlaying && !guardianEyes.ObservedPeekPlayback)
                    {
                        stirring = false; nextStir = Clock + (Alert.Value >= 65 ? 4 : Alert.Value >= 35 ? 7 : 12);
                        guardian.SetIntent(DragonCharacterIntent.SleepingGuardian, guardianHome.position);
                    }
                }
                return;
            }
            if (guardian.Intent == DragonCharacterIntent.SleepingGuardian)
            {
                if (guardian.motor.ActionPlaying && guardian.motor.CurrentAction == DragonAnimationAction.Wake) AlertStage = HoardAlertStage.Awakening;
                return;
            }
            if (!Alert.Awakened)
            {
                Alert.MarkAwake(); AlertStage = HoardAlertStage.Awake;
                if (sleepRumble != null) sleepRumble.Stop();
                Announcement = "THE DRAGON HAS AWAKENED · ESCAPE WITH YOUR TREASURE";
            }
            if (!roared && !guardian.motor.ActionPlaying)
            { roared = guardian.motor.TryAction(DragonAnimationAction.Roar, 1); }
            if (guardian.HasTargetMemory)
            {
                homeLandingNeeded = homeLandingRequested = false;
                AlertStage = guardian.motor.ActionPlaying && guardian.motor.CurrentAction == DragonAnimationAction.Roar ? HoardAlertStage.Awake : HoardAlertStage.Pursuing;
                searchStarted = 0;
                if (guardian.Intent == DragonCharacterIntent.Patrol) guardian.SetIntent(DragonCharacterIntent.Pursuit, lastSensedPosition);
            }
            else if (Clock - lastPerceptionAt > guardian.memorySeconds + 1)
            {
                AlertStage = HoardAlertStage.Investigating;
                if (searchStarted <= 0) searchStarted = Clock;
                if (Clock - searchStarted < 5) guardian.SetIntent(DragonCharacterIntent.Patrol, lastSensedPosition);
                else
                {
                    float homeDistance = Vector3.ProjectOnPlane(guardian.Position - guardianHome.position, Vector3.up).magnitude;
                    if ((guardian.motor.IsFlying || !guardian.motor.IsGrounded) && homeDistance < 35)
                    {
                        if (!homeLandingNeeded)
                        {
                            // Land is a vendor base-state animation, not a
                            // Mode action. Capture its truthful completion
                            // counter before asking the shared actor to land.
                            homeLandingNeeded = true;
                            homeLandingBaseline = guardian.motor.CompletedLandings;
                        }
                        if (!homeLandingRequested) homeLandingRequested = guardian.RequestLanding(guardianHome.position);
                    }
                    // Reapplying SetIntent would cancel the shared actor's
                    // pending physical landing request on the next frame.
                    if (!homeLandingRequested) guardian.SetIntent(DragonCharacterIntent.Patrol, guardianHome.position);
                }
                if (Clock - searchStarted > 12 && Vector3.Distance(guardian.Position, guardianHome.position) < 12 && guardian.motor.IsGrounded && !guardian.motor.IsFlying && !guardian.motor.LandingPlaying && (!homeLandingNeeded || guardian.motor.CompletedLandings > homeLandingBaseline) && !guardian.motor.ActionPlaying)
                {
                    Alert.ReturnToSleep(); roared = false; searchStarted = 0;
                    guardian.SetIntent(DragonCharacterIntent.SleepingGuardian, guardianHome.position);
                    if (sleepRumble != null) sleepRumble.Play();
                }
            }
            else guardian.SetIntent(DragonCharacterIntent.Pursuit, lastSensedPosition);
            if (visible && guardian.Intent == DragonCharacterIntent.Patrol) guardian.SetIntent(DragonCharacterIntent.Pursuit, lastSensedPosition);
        }

        protected override Vector3 DrivingGoal(int slot)
        {
            if (ShouldExtract(slot)) return extractionApproach.position;
            if (hiding[slot]) return quietCoverGoals[slot].position;
            int target = Targets[slot];
            if (target < 0) return extractionApproach.position;
            if(setup.players[slot].source==BowlingSource.AI&&treasureAccessValid[slot,target])return treasureAccessParking[slot,target];
            Vector3 offset = Vector3.ProjectOnPlane(trucks[slot].body.position - treasure[target].body.position, Vector3.up).normalized * 5;
            return treasure[target].body.position + offset;
        }
        protected override string ObjectivePrompt(int slot)
        {
            if (Rules == null) return "PREPARE HEIST";
            string alert = AlertStage.ToString().ToUpperInvariant() + " " + Alert.Value.ToString("0") + "%";
            if(setup.players[slot].source==BowlingSource.AI&&(trafficHolding[slot]||trafficYieldPhase[slot]!=0))return "MAKING ROOM \u00B7 "+alert;
            if(setup.players[slot].source==BowlingSource.AI&&roadOriginHolding[slot])return "NO SUPPORTED ROAD ENTRY · " + alert;
            if (hoists[slot].Stage != JengaCrane.MotorStage.Idle) return "PHYSICAL HOIST · " + hoists[slot].Stage.ToString().ToUpperInvariant() + " · " + alert;
            float feet = Vector3.Distance(trucks[slot].body.position, extractionApproach.position) * 3.28084f;
            return (ShouldExtract(slot) ? "RECEPTION " + feet.ToString("0") + " FT · STOP TO BANK" : hiding[slot] ? "WAIT IN COVER · LET SUSPICION FADE" : "FEATHER THROTTLE · STOP + ACTION TO LIFT") + " · " + alert;
        }
        // Waking changes the urgency of banking actual cargo. An empty crew
        // must still be able to recover spilled treasure, including at the
        // reception, instead of parking there forever with nothing to bank.
        bool ShouldExtract(int slot) => Rules.CargoValue(slot) > 0 && (Alert.Awakened || Rules.LoadCount(slot) >= Rules.CapacitySlots || Alert.Value >= 55 || ChooseTreasure(slot) < 0 || SecondsRemaining < 55);
        protected override bool ShouldHoldPosition(int slot)
        {
            if (HoistBusy(slot)) return true;
            if (setup.players[slot].source != BowlingSource.AI) return false;
            if(trafficYieldPhase[slot]!=0)return trafficHolding[slot];
            if(trafficHolding[slot]||Targets[slot]<0&&!ShouldExtract(slot)&&!hiding[slot])return true;
            if (ShouldExtract(slot)) return Vector3.ProjectOnPlane(trucks[slot].body.position - extractionApproach.position, Vector3.up).magnitude < 4;
            if (hiding[slot]) return Vector3.ProjectOnPlane(trucks[slot].body.position - quietCoverGoals[slot].position,Vector3.up).magnitude < 2.5f;
            int target = Targets[slot];
            return target >= 0 && Vector3.Distance(trucks[slot].body.position, treasure[target].body.position) < pickupReach - 1;
        }
        protected override Vector2 FilterDriveCommand(int slot, Vector2 drive, float dt)
        {
            if (setup.players[slot].source != BowlingSource.AI) return drive;
            drive=FilterRigTraffic(slot,drive);
            float nearGuardian = Vector3.Distance(trucks[slot].body.position, guardian.Position);
            bool quiet = !Alert.Awakened && nearGuardian < guardian.hearingRange * 1.2f;
            float limit = quiet ? 2.1f : Rules.LoadCount(slot) > 0 ? 7 : 10;
            if (trucks[slot].body.linearVelocity.magnitude > limit) drive.y = Mathf.Min(drive.y, -.3f);
            else if (quiet && drive.y > 0) drive.y = Mathf.Min(drive.y, .22f);
            return drive;
        }

        int ChooseTreasure(int slot)
        {
            int best = -1; float distance = float.PositiveInfinity;
            for (int i = 0; i < treasure.Length; i++)
            {
                if (!Rules.Fits(i, slot) || Clock - treasure[i].LastReleasedAt < 1 || treasure[i].body.isKinematic) continue;
                if (Clock<blockedUntil[slot,i]&&Vector3.Distance(trucks[slot].body.position,blockedFrom[slot,i])<2)continue;
                bool claimed = false;
                for (int other = 0; other < slot; other++) if (Targets[other] == i && IsDriving(other)) { claimed = true; break; }
                if (claimed) continue;
                if(setup.players[slot].source==BowlingSource.AI&&(!CanReachTreasure(slot,i)||!HoistWorkspaceClear(slot,i)))continue;
                float d = (treasure[i].body.position - trucks[slot].body.position).sqrMagnitude;
                if (d < distance) { best = i; distance = d; }
            }
            return best;
        }
        bool TryLift(int slot, int itemIndex)
        {
            var item = treasure[itemIndex];
            int rackSlot = FreeRackSlot(slot);
            if (rackSlot < 0 || item.body.linearVelocity.magnitude > .7f) return false;
            var destination=RackPoint(slot,rackSlot,item.Height);
            if(!HoistPathClear(slot,item,destination))
            { blockedUntil[slot,itemIndex]=Clock+8;blockedFrom[slot,itemIndex]=trucks[slot].body.position;Announcement="HOIST PATH BLOCKED · REPOSITION THE TRUCK";return false; }
            if(!Rules.ReserveLift(itemIndex,slot,true))return false;
            item.RackSlot = rackSlot;
            if (!hoists[slot].Begin(item.body, RackPoint(slot, rackSlot, item.Height), trucks[slot].body.rotation, item.Height))
            { Rules.Drop(itemIndex, slot, true); item.RackSlot = -1; return false; }
            processing[slot] = itemIndex; unloading[slot] = false; settle[slot] = 0;
            hoistStartedAt[slot]=Clock;hoistDestination[slot]=destination;
            Announcement = setup.players[slot].name + " · HOISTING " + item.gameObject.name.ToUpperInvariant(); return true;
        }
        void TickHoist(int slot, float dt)
        {
            var hoist = hoists[slot]; int index = processing[slot];
            if (hoist.Stage == JengaCrane.MotorStage.Idle)
            {
                hoist.spreader.MovePosition(Vector3.MoveTowards(hoist.spreader.position, ParkPoint(slot), dt * hoist.travelSpeed));
                if (Vector3.Distance(hoist.spreader.position, ParkPoint(slot)) <= .18f) returningHoist[slot] = false;
                if (hoistRigs[slot] != null) hoistRigs[slot].Present();
                return;
            }
            if (index < 0) return;
            var item = treasure[index];
            bool displaced=!unloading[slot]&&Vector3.Distance(RackPoint(slot,item.RackSlot,item.Height),hoistDestination[slot])>1.2f;
            if(displaced||Clock-hoistStartedAt[slot]>45)
            {
                StopHoist(slot);item.Release();Rules.Drop(index,slot,true);processing[slot]=-1;unloading[slot]=false;returningHoist[slot]=true;
                ReportNoiseEvent(slot,item.body.position,1,"INTERRUPTED CARGO LIFT");Announcement="LIFT INTERRUPTED · RECOVER THE ORIGINAL TREASURE";return;
            }
            // The crane captures source/target clearance in Begin. Feeding the
            // rising load back as towerTop continually raises its lift goal.
            hoist.Step(dt, cargoRacks[slot].bounds.max.y); hoist.Present();
            if (hoistRigs[slot] != null) hoistRigs[slot].Present();
            if (!hoist.Released) return;
            Vector3 destination = unloading[slot] ? DepositPoint(slot, index, item.Height) : RackPoint(slot, item.RackSlot, item.Height);
            bool rest = hoist.AtRest(destination, item.Height);
            settle[slot] = rest ? settle[slot] + dt : 0;
            if (settle[slot] < .3f) return;
            if (!unloading[slot])
            {
                if (!RackContains(slot, item) || !item.Secure(slot, trucks[slot].body)) return;
                if (!Rules.CompleteLoad(index, slot, true)) { item.Release(); throw new InvalidOperationException("Observed rack load disagrees with cargo authority."); }
                PhysicalLoads++;
                ReportNoiseEvent(slot, item.body.position, .22f + item.body.mass / 300, "LOADING CARGO");
            }
            hoist.Complete(); processing[slot] = -1; unloading[slot] = false; returningHoist[slot] = true; settle[slot] = 0;
        }
        bool TryBank(int slot)
        {
            if (trucks[slot].body.linearVelocity.magnitude >= .7f || !Contains(extractionZone, trucks[slot].body.worldCenterOfMass)) return false;
            for (int i = 0; i < treasure.Length; i++)
            {
                if (Rules.States[i] != HoardCargoState.Carried || Rules.Owners[i] != slot) continue;
                var item = treasure[i];
                var witness = new HoardDeliveryEvidence(item.body.GetInstanceID(), item.Carrier, FullyInside(item.shape, extractionZone), !item.body.isKinematic,
                    item.CargoJoint != null && item.CargoJoint.connectedBody == trucks[slot].body, true,
                    item.body.linearVelocity.magnitude < .8f && item.body.angularVelocity.magnitude < .4f);
                int award = Rules.Deliver(i, slot, witness);
                if (award < 0) continue;
                PhysicalDeliveries++;
                if (!ConfirmScore("heist.delivery." + item.stableId, slot, award, Rules.Scores[slot], "ORIGINAL TREASURE PHYSICALLY RECEIVED", SportsScoreTier.Major, "VALUE"))
                    throw new InvalidOperationException("Unique physical treasure award failed the shared score journal.");
                item.Release();
                if (HoistPathClear(slot, item, DepositPoint(slot, i, item.Height)) && hoists[slot].Begin(item.body, DepositPoint(slot, i, item.Height), Quaternion.identity, item.Height))
                { processing[slot] = i; unloading[slot] = true; settle[slot] = 0;hoistStartedAt[slot]=Clock;hoistDestination[slot]=DepositPoint(slot,i,item.Height); }
                Announcement = setup.players[slot].name + " · RECEIVED " + award + " VALUE";
                return true;
            }
            return false;
        }

        int FreeRackSlot(int owner)
        {
            for (int slot = 0; slot < cargoCapacity; slot++)
            {
                bool occupied = false;
                for (int i = 0; i < treasure.Length; i++) if (Rules.Owners[i] == owner && treasure[i].RackSlot == slot) { occupied = true; break; }
                if (!occupied) return slot;
            }
            return -1;
        }
        Vector3 RackPoint(int owner, int slot, float height) => cargoRacks[owner].transform.TransformPoint(cargoRacks[owner].center + new Vector3(0, cargoRacks[owner].size.y * .5f + height * .5f + .06f, (slot - (cargoCapacity - 1) * .5f) * 1.35f));
        Vector3 DepositPoint(int owner, int item, float height) => extractionZone.transform.TransformPoint(new Vector3((owner - 1.5f) * 7, -extractionZone.size.y * .5f + height * .5f + .12f, -8 + item % 4 * 3));
        bool RackContains(int owner, HoardTreasure item)
        {
            Vector3 local = cargoRacks[owner].transform.InverseTransformPoint(item.body.position) - cargoRacks[owner].center;
            return Mathf.Abs(local.x) < cargoRacks[owner].size.x * .5f && Mathf.Abs(local.z) < cargoRacks[owner].size.z * .5f && local.y > 0 && local.y < item.Height + 1;
        }
        bool HoistPathClear(int owner, HoardTreasure item, Vector3 goal)
        {
            float safe = Mathf.Max(goal.y + item.Height * .5f + 3, item.body.position.y + item.Height + 2);
            Vector3[] path = { item.body.position, new Vector3(item.body.position.x, safe, item.body.position.z), new Vector3(goal.x, safe, goal.z), goal };
            for (int leg = 0; leg < 3; leg++)
            {
                var delta = path[leg + 1] - path[leg];
                int count = Physics.BoxCastNonAlloc(path[leg], item.shape.bounds.extents * .9f, delta.normalized, hits, item.body.rotation, delta.magnitude, ~0, QueryTriggerInteraction.Ignore);
                if (count == hits.Length) return false;
                for (int h = 0; h < count; h++)
                {
                    var hit = hits[h];
                    if (hit.collider == null || hit.collider.transform.IsChildOf(item.transform) || hit.collider.attachedRigidbody == hoists[owner].spreader) continue;
                    if (hit.distance > .12f && hit.distance < delta.magnitude - .15f) return false;
                }
            }
            return true;
        }
        void ReleaseLast(int owner)
        {
            for (int i = treasure.Length - 1; i >= 0; i--) if (Rules.Owners[i] == owner && Rules.States[i] == HoardCargoState.Carried)
            { treasure[i].Release(); Rules.Drop(i, owner, true); ReportNoiseEvent(owner, treasure[i].body.position, 1, "DROPPED TREASURE"); return; }
        }
        protected override void ParticipantRecoveryBeginning(int slot) => ReleaseCrew(slot);
        protected override void ParticipantEliminated(int slot) => ReleaseCrew(slot);
        protected override void ParticipantRecovered(int slot) => ParkHoist(slot);
        void ReleaseCrew(int slot)
        {
            ClearRigYield(slot);
            StopHoist(slot); processing[slot] = -1; unloading[slot] = returningHoist[slot] = false;
            for (int i = 0; i < treasure.Length; i++) if (Rules != null && Rules.Owners[i] == slot)
            { treasure[i].Release(); Rules.Drop(i, slot, true); }
        }
        void ParkHoist(int slot)
        {
            var point = ParkPoint(slot);
            hoists[slot].spreader.position = point; hoists[slot].spreader.rotation = trucks[slot].body.rotation;
            hoists[slot].ceiling = point.y + 2;
            returningHoist[slot] = false;
            if (hoistRigs[slot] != null) hoistRigs[slot].Present();
        }
        Vector3 ParkPoint(int slot) => cargoRacks[slot].transform.TransformPoint(new Vector3(0, 5, -1));
        bool HoistBusy(int slot) => hoists[slot].Stage != JengaCrane.MotorStage.Idle || returningHoist[slot];
        void StopHoist(int slot)
        {
            var hoist=hoists[slot];
            // Public StopCrane destroys the grip with its coherent connection intact.
            hoist.StopCrane();
        }

        protected override void TruckNoiseObserved(int slot, Vector3 position, float loudness)
        { ReportNoiseEvent(slot, position, loudness * (1 + Rules.LoadKg(slot) / capacityKg * .6f), loudness > 2 ? "HORN" : "ENGINE"); }
        public void ReportCollisionNoise(int slot, Vector3 position, float impulseSpeed, int firstBody, int secondBody, string reason)
        {
            if (!Live || Intro > 0 || !float.IsFinite(impulseSpeed)) return;
            long key = ((long)Mathf.Min(firstBody, secondBody) << 32) ^ (uint)Mathf.Max(firstBody, secondBody);
            if (collisionNoise.TryGetValue(key, out float last) && Clock - last < .25f) return;
            if (collisionNoise.Count > 2048) collisionNoise.Clear();
            collisionNoise[key] = Clock; ReportNoiseEvent(slot, position, Mathf.Clamp(impulseSpeed * .7f, .1f, 4), reason);
        }
        public void ReportNoiseEvent(int slot, Vector3 position, float loudness, string reason)
        {
            if (!Live || Intro > 0 || Alert == null || guardian?.mouth == null || slot < 0 || slot >= setup.count || Retired[slot] || !float.IsFinite(position.x) || !float.IsFinite(position.y) || !float.IsFinite(position.z) || !float.IsFinite(loudness)) return;
            float distance = Vector3.Distance(position, guardian.mouth.position);
            bool obscured = NoiseObstructed(position, slot);
            if (!Alert.Hear(loudness, distance, guardian.hearingRange, obscured)) return;
            ActualNoiseEvents++; lastPerceptionAt = Clock; lastSensedPosition = position;
            guardian.ReportNoise(slot, position, loudness);
            if (loudness > 1) Announcement = reason + " · THE DRAGON HEARD THAT";
        }
        bool NoiseObstructed(Vector3 position, int source)
        {
            var delta = position - guardian.mouth.position;
            int count = Physics.RaycastNonAlloc(guardian.mouth.position, delta.normalized, hits, delta.magnitude, guardian.obstructionMask, QueryTriggerInteraction.Ignore);
            if (count == hits.Length) return true;
            for (int i = 0; i < count; i++) if (hits[i].collider != null && !hits[i].collider.transform.IsChildOf(guardian.transform) && !hits[i].collider.transform.IsChildOf(trucks[source].transform) && hits[i].distance < delta.magnitude - 1) return true;
            return false;
        }
        void GuardianHit(int slot, DragonElement element, float damage, Vector3 point) => ReleaseRubble();
        void GuardianActionStarted(DragonAnimationAction action)
        { if (action == DragonAnimationAction.Roar && awakeningRoar != null && guardian.voice != null) guardian.voice.PlayOneShot(awakeningRoar,.7f*settings.sfx); }
        void GuardianActionContact(DragonAnimationAction action)
        { if (action == DragonAnimationAction.Roar || action == DragonAnimationAction.Breath || action == DragonAnimationAction.ClawAttack) ReleaseRubble(); }
        void ReleaseRubble()
        {
            if (rubbleReleased) return;
            rubbleReleased = true;
            foreach (var body in caveDebris) if (body != null) { body.isKinematic = false; body.useGravity = true; body.WakeUp(); }
        }
        public static bool Contains(Collider zone, Vector3 point) => zone != null && float.IsFinite(point.x) && float.IsFinite(point.y) && float.IsFinite(point.z) && (zone.ClosestPoint(point) - point).sqrMagnitude < .0001f;
        public static bool FullyInside(BoxCollider item, Collider zone)
        {
            if (item == null || zone == null || !item.enabled || !zone.enabled) return false;
            for (int x = -1; x <= 1; x += 2) for (int y = -1; y <= 1; y += 2) for (int z = -1; z <= 1; z += 2)
                if (!Contains(zone, item.transform.TransformPoint(item.center + Vector3.Scale(item.size * .5f, new Vector3(x, y, z))))) return false;
            return true;
        }
        protected override void Present(float dt) { base.Present(dt); if (sleepRumble != null && settings != null) sleepRumble.volume = .18f * settings.sfx; }
        protected override void StopSounds()
        {
            foreach(var item in treasure)if(item!=null&&item.clink!=null)item.clink.Stop();
            foreach(var pot in pottery)if(pot!=null&&pot.breakSound!=null)pot.breakSound.Stop();
            base.StopSounds();
        }
        protected override void SetPaused(bool paused) { base.SetPaused(paused); if (sleepRumble != null) { if (paused) sleepRumble.Pause(); else if (Alert != null && !Alert.Awakened) sleepRumble.UnPause(); } }
        protected override void StopGame()
        {
            for (int i = 0; i < hoists.Length; i++) if (hoists[i] != null) ReleaseCrew(i);
            if (guardian != null) { guardian.TruckHit -= GuardianHit; guardian.SleepingWakeEligible = null; if (guardian.motor != null) { guardian.motor.ActionContact -= GuardianActionContact; guardian.motor.ActionStarted -= GuardianActionStarted; } }
            if (sleepRumble != null) sleepRumble.Stop();
            base.StopGame();
        }
    }
}
