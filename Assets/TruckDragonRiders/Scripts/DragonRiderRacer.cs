using System;
using System.Collections.Generic;
using LWS.TruckDragonKingdom;
using LWS.TruckSports;
using UnityEngine;

namespace LWS.TruckDragonRiders
{
    public enum DragonRiderPhase { Staged, Mounted, Falling, RescueApproach, ClawCarry, ReturningToSaddle, FailedRescueNotice, Finished, Retired }
    public struct DragonRiderCommand
    {
        public float yaw, climb, throttle;
        public bool boost, brake, dive, attack, parachute;
    }

    [DefaultExecutionOrder(50)]
    public sealed class DragonRiderRacer : MonoBehaviour
    {
#if UNITY_EDITOR
        public static event Action<DragonRiderRacer,string> PrepareGraphObservation;
        public int PrepareGraphNativeCallbacks {get;private set;}
#endif
        public DragonRidersMatch match;
        public SportsEventTruck truck;
        public DragonCharacter[] choices = Array.Empty<DragonCharacter>();
        public string[] choiceIds = Array.Empty<string>();
        public Transform canopy;
        public LineRenderer[] chuteLines = Array.Empty<LineRenderer>();
        public int slot;
        public DragonCharacter Dragon { get; private set; }
        public DragonSpeciesDefinition Species { get; private set; }
        public DragonRiderPhase Phase { get; private set; }
        public DragonRiderFlightBudget Budget { get; } = new DragonRiderFlightBudget();
        public float AttackReadyIn => Mathf.Max(Mathf.Max(0,attackReadyAt-match.Clock),Dragon!=null&&Dragon.Prepared?Dragon.AttackReadyIn:0);
        public int SuccessfulRescues { get; private set; }
        public int FailedRescues { get; private set; }
        public bool ChuteOpen { get; private set; }
        public bool FinishClearanceActive { get; private set; }
        Vector3 finishExitOrigin, finishExitHeading;
        public string Notice { get; private set; }
        public string LastDislodgementReason { get; private set; }
        public string LastCollisionCollider { get; private set; }
        public float LastDislodgementAt { get; private set; }
        public float LastCollisionNormalSpeed { get; private set; }
        public int CarrierAnimationCallbacks { get; private set; }
        public float CarrierAnimationFixedTime { get; private set; }
        public Vector3 CarrierRootBeforeIntegration { get; private set; }
        public Vector3 CarrierTransformBeforeIntegration { get; private set; }
        public Vector3 CarrierVelocityBeforeIntegration { get; private set; }
        public Vector3 CarrierPendingTranslation { get; private set; }
        public Quaternion CarrierBodyRotationBeforeIntegration { get; private set; }
        public Quaternion CarrierTransformRotationBeforeIntegration { get; private set; }
        public Quaternion CarrierFrameRotationCorrection { get; private set; }
        public Pose CarrierEvaluatedSocket { get; private set; }
        public Pose CarrierCommandedPose { get; private set; }
        public int FinalAttachmentFrame {get;private set;}
        public Vector3 FinalAttachmentSkinPoint {get;private set;}
        public bool PhysicallyMounted => (Phase == DragonRiderPhase.Mounted || Phase == DragonRiderPhase.Staged) && carry != null && hasCarrierPose
            && (carry.RemainsBound(lastCarrierPose,.35f)||carry.RemainsBound(previousCarrierPose,.35f));
        public Vector3 Position => truck != null && truck.body != null ? truck.body.position : transform.position;
        // This is the direction supplied to the vendor movement controller,
        // whose physical orientation legitimately follows with its own lag.
        public Vector3 FlightHeading => heading;

        DragonTruckCarryBinding carry;
        Rigidbody[] assembly = Array.Empty<Rigidbody>();
        float[] angularDamping = Array.Empty<float>();
        Vector3 heading, previousCentre, previousDragon, clawLocalOffset, previousRescueGripOffset;
        Quaternion clawLocalRotation;
        DragonRiderSaddle saddleFrame;
        Pose initialPose;
        float detachedAt, rescueTravel, blockedRescueTime, transfer, attackReadyAt, immuneUntil, failedNoticeAt, grabRequestedAt, saddleSupport, chuteOpenedAt;
        bool grabRequested, grabContact, carryObserved, started;
        readonly RaycastHit[] hits = new RaycastHit[48];
        readonly Collider[] space = new Collider[64];
        struct CollisionPair { public Collider own, other; public bool original; }
        readonly List<CollisionPair> collisionPairs = new List<CollisionPair>();
        bool temporaryCollisionIgnore;
        bool hasCarrierPose;
        Pose lastCarrierPose,previousCarrierPose;
        bool[] resultKinematics;

        public void Prepare(string speciesId, Pose start)
        {
            Stop();
            if (truck == null || truck.tractorMotor == null || choices.Length != choiceIds.Length)
                throw new InvalidOperationException("A full NWH crew and matching configured dragon choices are required");
            int selected = Array.IndexOf(choiceIds,speciesId);
            if (selected < 0 || !match.catalog.TryGet(speciesId,out var definition) || !definition.Supports(DragonSpeciesRole.RiderMount))
                throw new InvalidOperationException("Select an actual configured rideable flying species");
            Species = definition; Dragon = choices[selected];
            initialPose=start;
            foreach (var actor in choices) actor.gameObject.SetActive(actor == Dragon);
            Dragon.ResetCharacter();
            // This is the authorized match spawn, before flight begins. The
            // vendor reads its Transform for Position/LastPosition and for the
            // animated bone hierarchy; an interpolated Rigidbody-only reset
            // leaves the previous race's saddle pose visible during remount.
            Dragon.motor.animal.Rotation = start.rotation;
            Dragon.motor.animal.Teleport(start.position);
            Dragon.motor.body.position = start.position; Dragon.motor.body.rotation = start.rotation;
            Dragon.BindAudio(match.settings);
            Dragon.Prepare(Species,match.trucks,match.setup.count,match.MatchSeed+slot*7919);
            Dragon.SetIntent(DragonCharacterIntent.PlayerMounted,start.position);
            // ResetController selects the supported native default and writes
            // its parameters, but does not evaluate the Animator. V328 proves
            // that capturing/pausing here otherwise freezes imported bind pose
            // on the first start and the previous run's pose on rematches.
            // Evaluate the genuine configured default once BEFORE attachment;
            // never sample a bone/clip or advance gameplay/physics time.
#if UNITY_EDITOR
            PrepareGraphNativeCallbacks=0;PrepareGraphObservation?.Invoke(this,"before-configured-default-evaluation");
#endif
            var preparedAnimator=Dragon.motor.animator;var preparedAnimal=Dragon.motor.animal;
            bool preparedAnimatorEnabled=preparedAnimator.enabled,preparedPaused=Dragon.Paused;
            bool preparedTimeline=preparedAnimal.InTimeline;
            // OnAnimalMove uses Time.deltaTime even for Animator.Update(0),
            // and ground alignment precedes its DisablePosition/Rotation guard.
            // The supported external-animation lease bypasses that gameplay
            // integration while evaluating the actual configured graph at zero
            // duration. Public pause holds the original body; no pose is set.
            Action<MalbersAnimations.Controller.MAnimal> holdPreparedRoot=animal=>
            {
                // Diagnostic only: this callback must not be reached while
                // native InTimeline bypasses gameplay integration.
#if UNITY_EDITOR
                PrepareGraphNativeCallbacks++;PrepareGraphObservation?.Invoke(this,"during-native-poststate-root-hold");
#endif
            };
            try
            {
                Dragon.Pause(true);if(!preparedTimeline)preparedAnimal.InTimeline=true;
                preparedAnimal.PostStateMovement+=holdPreparedRoot;
                preparedAnimator.enabled=true;
#if UNITY_EDITOR
                PrepareGraphObservation?.Invoke(this,"before-Rebind-held");
#endif
                preparedAnimator.Rebind();
#if UNITY_EDITOR
                PrepareGraphObservation?.Invoke(this,"after-Rebind-before-Update-held");
#endif
                preparedAnimator.Update(0);
#if UNITY_EDITOR
                PrepareGraphObservation?.Invoke(this,"after-Update-before-restore-held");
#endif
            }
            finally
            {
                preparedAnimal.PostStateMovement-=holdPreparedRoot;
                if(!preparedTimeline)
                {
                    preparedAnimal.InTimeline=false;
                    // The vendor exit reconnects its input by default. Prepare
                    // deliberately disconnected it: Sports owns this rider's
                    // assigned controls, so retain that existing adapter policy.
                    preparedAnimal.UpdateInputSource(false);
                }
                Dragon.Pause(preparedPaused);preparedAnimator.enabled=preparedAnimatorEnabled;
            }
#if UNITY_EDITOR
            PrepareGraphObservation?.Invoke(this,"after-configured-default-evaluation");
#endif
            saddleFrame=Dragon.GetComponent<DragonRiderSaddle>();
            if(saddleFrame==null)throw new InvalidOperationException("Run the explicit saved Riders semantic-saddle upgrade before mounting.");
            saddleFrame.Validate(Dragon);
            saddleFrame.CaptureEvaluatedAnimationPose();
            Dragon.TargetEligible = candidate => match.CanAttack(slot,candidate);
            Dragon.TruckHit += Hit;
            Dragon.motor.ActionContact += ActionContact;
            Dragon.motor.ActionStarted += ActionStarted;
            Dragon.motor.ActionFinished += ActionFinished;
            var collision = Dragon.motor.gameObject.GetComponent<DragonRiderDragonCollision>() ?? Dragon.motor.gameObject.AddComponent<DragonRiderDragonCollision>();
            collision.owner = this;
            assembly = truck.GetComponentsInChildren<Rigidbody>(true); angularDamping = new float[assembly.Length];
            for (int i=0;i<assembly.Length;i++) angularDamping[i]=assembly[i].angularDamping;
            CaptureCollisionBaselines();
            Budget.Reset(Species.maximumStamina); SuccessfulRescues=FailedRescues=0; attackReadyAt=2; immuneUntil=0;
            started=false; FinishClearanceActive=false; Notice="MOUNTED · READY"; Phase=DragonRiderPhase.Staged;
            LastDislodgementReason=LastCollisionCollider="";LastDislodgementAt=LastCollisionNormalSpeed=0;
            CarrierAnimationCallbacks=0;CarrierAnimationFixedTime=0;CarrierRootBeforeIntegration=CarrierTransformBeforeIntegration=CarrierVelocityBeforeIntegration=CarrierPendingTranslation=Vector3.zero;
            heading=start.rotation*Vector3.forward;
            truck.tractorMotor.Configure();
            float lower=truck.shape.center.y-truck.shape.size.y*.5f;
            foreach(var wheel in truck.wheels)if(wheel!=null)lower=Mathf.Min(lower,truck.transform.InverseTransformPoint(wheel.transform.position).y-wheel.wheel.radius-wheel.spring.maxLength);
            saddleSupport=-lower+.05f;
            var seat = SaddlePose(); truck.Prepare(seat.position,seat.rotation,false);
            carry = new DragonTruckCarryBinding(truck,new Pose(truck.body.position,truck.body.rotation),Dragon.physicalColliders);
            lastCarrierPose=previousCarrierPose=new Pose(truck.body.position,truck.body.rotation);hasCarrierPose=true;
            FinalizeCarryPose(); previousCentre=truck.shape.bounds.center; previousDragon=Dragon.motor.body.position;
            Dragon.Pause(true); SetCanopy(false);
        }

        public void ReleaseStart()
        {
            if (Phase != DragonRiderPhase.Staged) return;
            Dragon.Pause(false); Phase=DragonRiderPhase.Mounted; started=true;
            previousCentre=truck.shape.bounds.center;
        }
        public void Step(DragonRiderCommand command,float dt)
        {
            if (Dragon == null) return;
            if(Phase==DragonRiderPhase.Finished)
            {
                // Finished crews retain their result but must vacate the actual
                // gate. Pausing here used to leave three collidable mounts in
                // the opening and strand the last racer above their bodies.
                if(FinishClearanceActive)
                {
                    if(Vector3.Dot(Dragon.motor.body.position-finishExitOrigin,finishExitHeading)>=180)
                    {FinishClearanceActive=false;Dragon.Pause(true);}
                    else Dragon.motor.MoveWorld(AvoidCollision(finishExitHeading*.65f+Vector3.up*.08f),false,true);
                }
                return;
            }
            if(match.rules.Finished(slot)||match.rules.Retired[slot])return;
            Dragon.Tick(dt); // shared warning/animation-contact attack authority
            if (!started)return;
            if (Phase == DragonRiderPhase.Mounted)
            {
                bool sprint=Budget.Step(dt,command.climb,command.boost,command.brake);
                heading=Quaternion.AngleAxis(command.yaw*Species.turnRate*dt,Vector3.up)*heading;
                heading=Vector3.ProjectOnPlane(heading,Vector3.up).normalized;
                float climb=command.climb*(Budget.Stamina>0?.65f:.2f);
                if(command.dive)climb=Mathf.Min(climb,-.75f);
                float throttle=command.brake&&Budget.BrakeCharge>0?.18f:Mathf.Clamp(command.throttle,.2f,1);
                var desired=(heading+Vector3.up*climb).normalized*throttle;
                desired=AvoidCollision(desired);
                Dragon.motor.MoveWorld(desired,sprint,true);
                // The real Fixed Animator evaluates after this input step.
                // FollowEvaluatedSaddle issues the carrier pose after MAnimal's
                // normal OnAnimatorMove, using that same freshly evaluated rig.
                var centre=truck.shape.bounds.center;
                float radius=Mathf.Max(truck.shape.bounds.extents.x,truck.shape.bounds.extents.y);
                if(match.rules.ObservePassage(slot,previousCentre,centre,radius,PhysicallyMounted))match.GatePassed(slot);
                previousCentre=centre;
                if(command.attack&&match.Clock>=attackReadyAt&&Dragon.RequestAttack(DragonAnimationAction.Breath,slot))
                    attackReadyAt=match.Clock+Mathf.Max(6,Dragon.attackCooldown);
                if(!PhysicallyMounted)Dislodge("SADDLE CONNECTION LOST",Vector3.zero);
            }
            else if(Phase==DragonRiderPhase.Falling||Phase==DragonRiderPhase.RescueApproach)
            {
                Phase=DragonRiderPhase.RescueApproach;
                if(command.parachute)SetCanopy(true);
                if(ChuteOpen)ApplyChute(dt);
                Vector3 velocity=truck.body.linearVelocity;
                var grip=Dragon.leftGrip;
                if(grip==null)throw new InvalidOperationException("Actual configured rescue grip bone is required");
                // Match the actual moving collider and animated grip rather
                // than chase its future centre at a fixed normal flight speed.
                // V115's boosted truck retained42m/s while that pursuit slowed
                // to32m/s, losing an initially reachable Grab contact forever.
                Vector3 gripOffset=grip.position-Dragon.motor.body.position;
                Vector3 gripMotion=dt>0?Vector3.ClampMagnitude((gripOffset-previousRescueGripOffset)/dt,18):Vector3.zero;
                previousRescueGripOffset=gripOffset;
                Vector3 contact=truck.shape.ClosestPoint(grip.position);
                if((contact-grip.position).sqrMagnitude>.01f)contact+=Vector3.ClampMagnitude(truck.body.worldCenterOfMass-contact,.8f);
                Vector3 error=contact-grip.position;
                Vector3 desiredVelocity=velocity-gripMotion+Vector3.ClampMagnitude(error*2.4f,18);
                bool sprint=Budget.Step(dt,Mathf.Clamp(desiredVelocity.y/Mathf.Max(1,Species.flightSpeed),-1,1),
                    error.magnitude>12&&desiredVelocity.magnitude>Species.flightSpeed*.9f,false);
                float requestedSpeed=Mathf.Max(1,Species.flightSpeed*(sprint?1.28f:1));
                var direction=AvoidCollision(Vector3.ClampMagnitude(desiredVelocity/requestedSpeed,1));
                float actualTravel=Vector3.Distance(previousDragon,Dragon.motor.body.position);
                if(direction.sqrMagnitude<.05f||actualTravel<.025f&&error.magnitude>6)blockedRescueTime+=dt;
                else blockedRescueTime=Mathf.Max(0,blockedRescueTime-dt*2);
                Dragon.motor.MoveWorld(direction,sprint,true);
                rescueTravel+=actualTravel; previousDragon=Dragon.motor.body.position;
                if(grabRequested&&!Dragon.motor.ActionPlaying&&match.Clock-grabRequestedAt>3)grabRequested=false;
                if(!grabRequested&&Vector3.Distance(grip.position,truck.shape.ClosestPoint(grip.position))<11)
                {grabRequested=Dragon.motor.TryAction(DragonAnimationAction.Grab,2);if(grabRequested)grabRequestedAt=match.Clock;}
                bool failedFlight=match.Clock-detachedAt>18&&(rescueTravel>8||blockedRescueTime>5);
                if(failedFlight||match.Clock-detachedAt>10&&truck.body.position.y<-30&&rescueTravel>8)FailRescue();
            }
            else if(Phase==DragonRiderPhase.ClawCarry)
            {
                FollowCarrier(GripPose());
                if(!Dragon.motor.ActionPlaying&&!carryObserved)Dragon.motor.TryAction(DragonAnimationAction.Carry,.5f);
                Dragon.motor.MoveWorld(heading*.45f+Vector3.up*.2f,false,true);
            }
            else if(Phase==DragonRiderPhase.ReturningToSaddle)
            {
                transfer=Mathf.Min(1,transfer+dt/1.6f);
                var from=GripPose();var to=SaddlePose();
                FollowCarrier(new Pose(Vector3.Lerp(from.position,to.position,Mathf.SmoothStep(0,1,transfer)),Quaternion.Slerp(from.rotation,to.rotation,transfer)));
                Dragon.motor.MoveWorld(heading*.5f,false,true);
                if(transfer>=1)
                {
                    Dragon.motor.InterruptAction(); Phase=DragonRiderPhase.Mounted; SuccessfulRescues++;
                    match.rules.AddPenalty(slot,4); immuneUntil=match.Clock+5;
                    Notice="ANIMATED RESCUE · +4 SEC";match.Announce(slot,Notice);
                    SetCanopy(false); previousCentre=truck.shape.bounds.center;
                }
            }
            else if(Phase==DragonRiderPhase.FailedRescueNotice)
            {
                if(ChuteOpen)ApplyChute(dt);
                Dragon.motor.MoveWorld(Vector3.up*.25f,false,true);
                if(match.Clock-failedNoticeAt>=2)CheckpointRecovery();
            }
            if(temporaryCollisionIgnore&&carry==null&&match.Clock-detachedAt>.8f&&Vector3.Distance(truck.body.position,Dragon.motor.body.position)>18)RestoreCollisionBaselines();
        }

        public void Collision(Collision collision)
        {
            if(!match.Live||Phase!=DragonRiderPhase.Mounted||match.Clock<immuneUntil||collision.collider==null||collision.collider.transform.IsChildOf(truck.transform))return;
            float normal=0;
            for(int i=0;i<collision.contactCount;i++)normal=Mathf.Max(normal,Mathf.Abs(Vector3.Dot(collision.relativeVelocity,collision.GetContact(i).normal)));
            if(normal>13)
            {
                LastCollisionCollider=collision.collider.name+" / "+collision.collider.transform.root.name;
                LastCollisionNormalSpeed=normal;
                Dislodge("HEAVY AERIAL COLLISION",collision.relativeVelocity*.12f);
            }
        }
        public void ReceiveAttack(DragonElement element,float damage,Vector3 point)
        {
            if(!match.Live||Phase!=DragonRiderPhase.Mounted||match.Clock<immuneUntil)return;
            immuneUntil=match.Clock+2.5f; // same protection and authority for humans and AI
            Notice=element.ToString().ToUpperInvariant()+" HIT";
            if(Budget.ApplyAttack(Mathf.Clamp(damage*2.5f,15,60)))Dislodge("SADDLE DISLODGED BY ATTACK",(truck.body.worldCenterOfMass-point).normalized*2);
            match.Announce(slot,Notice);
        }
        void Hit(int target,DragonElement element,float damage,Vector3 point)
        { if(match.CanAttack(slot,target))match.racers[target].ReceiveAttack(element,damage,point); }

        public void Dislodge(string reason,Vector3 impulse)
        {
            if(Phase!=DragonRiderPhase.Mounted||carry==null)return;
            LastDislodgementReason=reason;LastDislodgementAt=match.Clock;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("RIDERS_ACTUAL_DISLODGEMENT slot="+slot+" clock="+match.Clock+" reason="+reason+" collision="+LastCollisionCollider+" normalSpeed="+LastCollisionNormalSpeed+" dragonBody="+Dragon.motor.body.position.ToString("F3")+" dragonTransform="+Dragon.motor.transform.position.ToString("F3")+" truck="+truck.body.position.ToString("F3")+" lastCarrier="+lastCarrierPose.position.ToString("F3")+" previousCarrier="+previousCarrierPose.position.ToString("F3")+" lastBound="+carry.RemainsBound(lastCarrierPose,.35f)+" previousBound="+carry.RemainsBound(previousCarrierPose,.35f));
#endif
            var velocity=Dragon.motor.Velocity;
            carry.Release(velocity+Vector3.up*2+Dragon.motor.transform.right*3+Vector3.ClampMagnitude(impulse,4),Vector3.zero);
            carry=null; foreach(var pair in collisionPairs)if(pair.own!=null&&pair.other!=null)Physics.IgnoreCollision(pair.own,pair.other,true);
            temporaryCollisionIgnore=true; Phase=DragonRiderPhase.Falling; Notice=reason+" · DEPLOY CHUTE";
            if(saddleFrame.rigging!=null)saddleFrame.rigging.SetAttached(false);
            detachedAt=match.Clock;rescueTravel=blockedRescueTime=transfer=0;grabRequested=grabContact=carryObserved=false;
            previousDragon=Dragon.motor.body.position; Dragon.motor.InterruptAction();
            previousRescueGripOffset=Dragon.leftGrip.position-Dragon.motor.body.position;
            Dragon.SetIntent(DragonCharacterIntent.Passive,Dragon.Position);
            Dragon.SetIntent(DragonCharacterIntent.PlayerMounted,Dragon.Position);
            match.rules.ClearPassageApproach(slot);match.Announce(slot,Notice);
        }
        void ActionContact(DragonAnimationAction action)
        {
            if(action!=DragonAnimationAction.Grab||Phase!=DragonRiderPhase.RescueApproach||!grabRequested||!Dragon.motor.ActionPlaying)return;
            Vector3 grip=Dragon.leftGrip.position;
            if(Vector3.Distance(grip,truck.shape.ClosestPoint(grip))>3||(Dragon.motor.Velocity-truck.body.linearVelocity).magnitude>20||!ClearGripPath(grip))return;
            var pose=new Pose(truck.body.position,truck.body.rotation);
            clawLocalOffset=Quaternion.Inverse(Dragon.leftGrip.rotation)*(pose.position-grip);
            clawLocalRotation=Quaternion.Inverse(Dragon.leftGrip.rotation)*pose.rotation;
            carry=new DragonTruckCarryBinding(truck,pose,Dragon.physicalColliders);grabContact=true;Phase=DragonRiderPhase.ClawCarry;
            lastCarrierPose=previousCarrierPose=pose;hasCarrierPose=true;
            SetCanopy(false);Notice="CLAW CAPTURE · HOLDING THE ACTUAL TRUCK";
        }
        void ActionStarted(DragonAnimationAction action)
        {
            if(action==DragonAnimationAction.Carry&&Phase==DragonRiderPhase.ClawCarry&&grabContact)
            {carryObserved=true;transfer=0;Phase=DragonRiderPhase.ReturningToSaddle;}
        }
        void ActionFinished(DragonAnimationAction action)
        { if(action==DragonAnimationAction.Grab&&Phase==DragonRiderPhase.RescueApproach)grabRequested=false; }
        void FailRescue()
        {
            if(Phase==DragonRiderPhase.FailedRescueNotice)return;
            FailedRescues++;Phase=DragonRiderPhase.FailedRescueNotice;failedNoticeAt=match.Clock;
            Dragon.motor.InterruptAction();
            Notice="RESCUE FAILED · CHECKPOINT RECOVERY +12 SEC";match.Announce(slot,Notice);
        }
        void CheckpointRecovery()
        {
            if(FailedRescues>=3)
            {match.rules.FailedRescuesExhausted(slot);Phase=DragonRiderPhase.Retired;Dragon.motor.StopMotion();Dragon.Pause(true);Notice="THREE FAILED RESCUES · RETIRED";return;}
            int next=match.rules.NextGate(slot), previous=(next+match.rules.GateCount-1)%match.rules.GateCount;
            Vector3 position=match.rules.Passed[slot]==0?initialPose.position:match.rules.GateCenter(previous)-match.rules.GateRotation(previous)*Vector3.forward*12;
            Quaternion rotation=Quaternion.LookRotation((match.rules.GateCenter(next)-position).normalized,Vector3.up);
            int found=Physics.OverlapSphereNonAlloc(position,8,space,~0,QueryTriggerInteraction.Ignore);
            if(found==space.Length)return;
            for(int i=0;i<found;i++)if(!space[i].transform.IsChildOf(Dragon.transform)&&!space[i].transform.IsChildOf(truck.transform))return;
            RestoreCollisionBaselines();SetCanopy(false);
            Dragon.motor.animal.Rotation=rotation;Dragon.motor.animal.Teleport(position);
            Dragon.motor.body.position=position;Dragon.motor.body.rotation=rotation;Dragon.motor.body.linearVelocity=Vector3.zero;
            heading=Vector3.ProjectOnPlane(rotation*Vector3.forward,Vector3.up).normalized;
            saddleFrame.CaptureEvaluatedAnimationPose();
            var seat=SaddlePose();truck.Prepare(seat.position,seat.rotation,false);
            carry=new DragonTruckCarryBinding(truck,new Pose(truck.body.position,truck.body.rotation),Dragon.physicalColliders);
            lastCarrierPose=previousCarrierPose=new Pose(truck.body.position,truck.body.rotation);hasCarrierPose=true;
            match.rules.AddPenalty(slot,12);match.rules.ClearPassageApproach(slot);
            Phase=DragonRiderPhase.Mounted;immuneUntil=match.Clock+6;Notice="CHECKPOINT RECOVERY · +12 SEC";previousCentre=truck.shape.bounds.center;
        }

        Pose SaddlePose()
        {
            var saddle=saddleFrame.ReadPose();
            return new Pose(saddle.position+saddle.rotation*Vector3.up*saddleSupport,saddle.rotation);
        }
        public void FollowEvaluatedSaddle()
        {
            if(carry==null||Dragon==null||!Dragon.Prepared||Dragon.Paused||!Time.inFixedTimeStep||
                Dragon.motor.animator.updateMode!=AnimatorUpdateMode.Fixed||
                Phase!=DragonRiderPhase.Mounted)return;
            var root=Dragon.motor.body;
            // MAnimal's Fixed OnAnimatorMove assigns the actual dynamic root
            // velocity before this physics integration. The kinematic original
            // assembly must arrive at the same end-of-step root, rather than
            // remain one evaluated bone pose behind it. No vendor movement,
            // velocity, root-motion or animation state is changed here.
            CarrierRootBeforeIntegration=root.position;
            CarrierTransformBeforeIntegration=Dragon.motor.transform.position;
            CarrierVelocityBeforeIntegration=root.linearVelocity;
            CarrierBodyRotationBeforeIntegration=root.rotation;
            CarrierTransformRotationBeforeIntegration=Dragon.motor.transform.rotation;
            CarrierFrameRotationCorrection=Quaternion.identity;
            CarrierPendingTranslation=root.isKinematic?Vector3.zero:
                root.position-Dragon.motor.transform.position+root.linearVelocity*Time.fixedDeltaTime;
            // Telemetry only. Final attachment happens after the Fixed
            // animation/physics write in the ordinary pre-camera Present path.
            CarrierEvaluatedSocket=new Pose(saddleFrame.socket.position,saddleFrame.socket.rotation);
            CarrierAnimationFixedTime=Time.fixedTime;CarrierAnimationCallbacks++;
        }
        void FinalizeCarryPose()
        {
            // Public pause has already frozen the vendor animal and the real
            // carried assembly. Do not issue another skin-driven pose while
            // its interpolated root settles at the pause boundary.
            if(match!=null&&match.Paused)return;
            if(carry==null||Dragon==null||!Dragon.Prepared)return;
            Pose target;
            if(Phase==DragonRiderPhase.ClawCarry)target=GripPose();
            else
            {
                try{saddleFrame.CaptureEvaluatedAnimationPose();}
                catch(InvalidOperationException error)
                {
                    throw new InvalidOperationException("Actual Riders saddle evaluation failed: slot="+slot+" phase="+Phase+" clock="+match.Clock+" detachedAt="+detachedAt+" lastDislodgement="+LastDislodgementReason+" lastCollision="+LastCollisionCollider+" collisionNormalSpeed="+LastCollisionNormalSpeed+" dragonVelocity="+Dragon.motor.body.linearVelocity.ToString("F4")+"; "+error.Message,error);
                }
                FinalAttachmentSkinPoint=saddleFrame.ActualSurfacePoint;target=SaddlePose();
                if(Phase==DragonRiderPhase.ReturningToSaddle){var from=GripPose();float blend=Mathf.SmoothStep(0,1,transfer);target=new Pose(Vector3.Lerp(from.position,target.position,blend),Quaternion.Slerp(from.rotation,target.rotation,blend));}
            }
            if(!carry.TryFollowKinematicAttachment(target)){if(Phase==DragonRiderPhase.Mounted)Dislodge("SADDLE CONNECTION LOST",Vector3.zero);return;}
            previousCarrierPose=lastCarrierPose;lastCarrierPose=target;hasCarrierPose=true;CarrierCommandedPose=target;FinalAttachmentFrame=Time.frameCount;
            if(saddleFrame.rigging!=null)saddleFrame.rigging.SetAttached(Phase==DragonRiderPhase.Mounted||Phase==DragonRiderPhase.Staged||Phase==DragonRiderPhase.Finished);
        }
        Pose GripPose()=>new Pose(Dragon.leftGrip.position+Dragon.leftGrip.rotation*clawLocalOffset,Dragon.leftGrip.rotation*clawLocalRotation);
        void FollowCarrier(Pose target)
        {
            // MovePosition commits during physics integration. Verify against
            // the last two actually issued poses, rather than falsely treating
            // the normal one-step carrier movement as a broken saddle at speed.
            previousCarrierPose=lastCarrierPose;lastCarrierPose=target;hasCarrierPose=true;carry.Follow(target);
        }
        Vector3 AvoidCollision(Vector3 desired)
        {
            if(desired.sqrMagnitude<.001f)return Vector3.zero;
            int count=Physics.SphereCastNonAlloc(Dragon.motor.body.position+Vector3.up*2,4,desired.normalized,hits,Mathf.Max(18,Dragon.motor.Speed*1.5f),~0,QueryTriggerInteraction.Ignore);
            if(count==hits.Length)return Vector3.zero;
            for(int i=0;i<count;i++)if(hits[i].collider!=null&&!hits[i].collider.transform.IsChildOf(Dragon.transform)&&!hits[i].collider.transform.IsChildOf(truck.transform))
                return (desired+Vector3.up*1.8f).normalized*Mathf.Min(.55f,desired.magnitude);
            return desired;
        }
        bool ClearGripPath(Vector3 grip)
        {
            Vector3 delta=truck.body.worldCenterOfMass-grip;
            int count=Physics.RaycastNonAlloc(grip,delta.normalized,hits,delta.magnitude,~0,QueryTriggerInteraction.Ignore);
            if(count==hits.Length)return false;
            for(int i=0;i<count;i++)if(hits[i].collider!=null&&!hits[i].collider.transform.IsChildOf(Dragon.transform)&&!hits[i].collider.transform.IsChildOf(truck.transform))return false;
            return true;
        }
        void ApplyChute(float dt)
        {
            // Reuse the established BaseJumper canopy opening/air-drag pattern
            // on the already detached original assembly. Uniform bounded
            // acceleration preserves its NWH joints and conserved body masses.
            float opening=Mathf.SmoothStep(0,1,Mathf.Clamp01((match.Clock-chuteOpenedAt)/1.15f));
            foreach(var body in assembly)if(body!=null&&!body.isKinematic)
            {
                var horizontal=Vector3.ProjectOnPlane(body.linearVelocity,Vector3.up);
                body.AddForce(-Vector3.ClampMagnitude(horizontal*.65f,14)*opening,ForceMode.Acceleration);
                body.AddForce(Vector3.up*Mathf.Clamp((-body.linearVelocity.y-7)*2,0,18)*opening,ForceMode.Acceleration);
                body.AddTorque(-body.angularVelocity*2*opening,ForceMode.Acceleration);
            }
        }
        void SetCanopy(bool open)
        {if(open&&!ChuteOpen)chuteOpenedAt=match.Clock;ChuteOpen=open;if(canopy!=null)canopy.gameObject.SetActive(open);foreach(var line in chuteLines)if(line!=null)line.enabled=open;}
        public void Present()
        {
            if(!ChuteOpen||canopy==null)return;
            canopy.SetPositionAndRotation(truck.body.position+Vector3.up*9,Quaternion.identity);
            for(int i=0;i<chuteLines.Length;i++)if(chuteLines[i]!=null)
            {var side=i%2==0?-1:1;var fore=i<2?-1:1;chuteLines[i].SetPosition(0,truck.transform.TransformPoint(new Vector3(side*.8f,1,fore)));chuteLines[i].SetPosition(1,canopy.position+new Vector3(side*3,0,fore*1.7f));}
        }
        // Fixed Animator bone writes and Rigidbody interpolation are complete.
        // This is the final kinematic attachment before ordinary camera render.
        void LateUpdate()=>FinalizeCarryPose();
        void CaptureCollisionBaselines()
        {
            collisionPairs.Clear();
            foreach(var own in truck.GetComponentsInChildren<Collider>(true))foreach(var other in Dragon.physicalColliders)
                if(own!=null&&other!=null)collisionPairs.Add(new CollisionPair{own=own,other=other,original=Physics.GetIgnoreCollision(own,other)});
        }
        void RestoreCollisionBaselines()
        {foreach(var pair in collisionPairs)if(pair.own!=null&&pair.other!=null)Physics.IgnoreCollision(pair.own,pair.other,pair.original);temporaryCollisionIgnore=false;}
        public void Pause(bool paused)=>Dragon?.Pause(paused||Phase==DragonRiderPhase.Finished&&!FinishClearanceActive||Phase==DragonRiderPhase.Retired);
        public void Finish()
        {
            Phase=DragonRiderPhase.Finished;FinishClearanceActive=true;
            finishExitOrigin=Dragon.motor.body.position;
            finishExitHeading=Vector3.ProjectOnPlane(match.rules.GateRotation(match.rules.GateCount-1)*Vector3.forward,Vector3.up).normalized;
            Dragon.motor.InterruptAction();Dragon.Pause(false);Notice="PHYSICAL RACE FINISH";
        }
        public void SuspendResult()
        {
            if(resultKinematics!=null)return;
            Dragon?.Pause(true); resultKinematics=new bool[assembly.Length];
            for(int i=0;i<assembly.Length;i++)if(assembly[i]!=null){resultKinematics[i]=assembly[i].isKinematic;assembly[i].isKinematic=true;}
        }
        public void Stop()
        {
            if(resultKinematics!=null){for(int i=0;i<assembly.Length&&i<resultKinematics.Length;i++)if(assembly[i]!=null)assembly[i].isKinematic=resultKinematics[i];resultKinematics=null;}
            carry?.Dispose();carry=null;RestoreCollisionBaselines();
            if(saddleFrame!=null&&saddleFrame.rigging!=null)saddleFrame.rigging.SetAttached(false);
            hasCarrierPose=false;
            if(Dragon!=null)
            {Dragon.TruckHit-=Hit;Dragon.motor.ActionContact-=ActionContact;Dragon.motor.ActionStarted-=ActionStarted;Dragon.motor.ActionFinished-=ActionFinished;Dragon.ResetCharacter();}
            for(int i=0;i<assembly.Length&&i<angularDamping.Length;i++)if(assembly[i]!=null)assembly[i].angularDamping=angularDamping[i];
            SetCanopy(false);started=false;FinishClearanceActive=false;
        }
        void OnDisable()=>Stop();
    }
}
