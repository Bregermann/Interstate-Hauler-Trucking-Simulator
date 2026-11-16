using System;
using System.Collections.Generic;
using MalbersAnimations;
using MalbersAnimations.Controller;
using UnityEngine;

namespace LWS.TruckDragonKingdom
{
    [Serializable]
    public sealed class DragonMotorActionBinding
    {
        public DragonAnimationAction action;
        public ModeID mode;
        public int abilityIndex;
        [Range(0, 1)] public float contactNormalizedTime = .45f;
        public bool continuous;
    }

    // Sports owns intent/input. Malbers owns all locomotion, Rigidbody movement,
    // pitch/bank, grounded alignment, animation-state movement and mode playback.
    [DefaultExecutionOrder(-5)]
    public sealed class DragonAnimalMotor : MonoBehaviour
    {
        public MAnimal animal;
        public string configuredSpeciesId;
        public Animator animator;
        public Rigidbody body;
        public StateID idleState, groundState, flightState, fallState;
        public DragonMotorActionBinding[] actionBindings = Array.Empty<DragonMotorActionBinding>();
        public DragonSpeciesDefinition Species { get; private set; }
        public bool Prepared { get; private set; }
        public bool Paused { get; private set; }
        public bool StagedAnimationHeld {get;private set;}
        public int StagedAnimationEvaluations {get;private set;}
        public bool IsFlying => Prepared && animal != null && flightState != null &&
            animal.ActiveStateID == flightState && animal.ActiveState != null && animal.ActiveState.InCoreAnimation;
        public bool IsGrounded => Prepared && animal != null && animal.Grounded;
        public float Speed => body != null ? body.linearVelocity.magnitude : 0;
        public Vector3 Velocity => body != null ? body.linearVelocity : Vector3.zero;
        public bool ActionPlaying => observedAction != null;
        public DragonAnimationAction CurrentAction => observedAction != null ? observedAction.action : DragonAnimationAction.GroundIdle;
        public float ActionNormalizedTime { get; private set; }
        public float LastActionNormalizedTime { get; private set; }
        public bool LastActionCompleted { get; private set; }
        // Read-only evidence of the actual public request path. These fields
        // do not decide readiness, activate a mode, retry, or alter cooldowns.
        public string LastActionRequestDiagnosis { get; private set; } = "No request observed";
        public int LastActionRequestFrame { get; private set; } = -1;
        public float LastActionRequestTime { get; private set; }
        public DragonAnimationAction LastRequestedAction { get; private set; }
        public int LastPrepareModeResetFrame { get; private set; } = -1;
        public int LastPreparePlayingModesReset { get; private set; }
        public int LastPrepareStalePlayingModesReset { get; private set; }
        public string LastPrepareModeResetDiagnosis { get; private set; } = "No prepare observed";
        public const string LandingAnimatorTag="DragonLand";
        public bool LandingPlaying=>Prepared&&!Paused&&(landingObserved||landingReceiptCancelled);
        public int CompletedLandings { get; private set; }
        public event Action<DragonAnimationAction> ActionStarted;
        public event Action<DragonAnimationAction> ActionContact;
        public event Action<DragonAnimationAction> ActionFinished;

        readonly float[] nextActionAt = new float[Enum.GetValues(typeof(DragonAnimationAction)).Length];
        readonly List<AnimatorClipInfo> currentClips = new List<AnimatorClipInfo>(8), nextClips = new List<AnimatorClipInfo>(8);
        readonly List<AnimatorOverrideController> overrideChain = new List<AnimatorOverrideController>(4);
        readonly List<KeyValuePair<AnimationClip, AnimationClip>> overrideClips = new List<KeyValuePair<AnimationClip, AnimationClip>>(64);
        DragonMotorActionBinding observedAction;
        bool contactReported, actionCancelled, oldKinematic, oldAnimatorEnabled, oldLockInput, oldLockMovement;
        bool landingObserved,landingNaturalExit,landingInterrupted,landingReceiptCancelled;
        float landingNormalized;
        float oldAnimatorSpeed, requestAt;
        Vector3 pausedVelocity, pausedAngularVelocity;
        AnimatorUpdateMode stagedOldUpdateMode;
        bool stagedOldDisablePosition,stagedOldDisableRotation;

        public void Prepare(DragonSpeciesDefinition species)
        {
            if (species == null) throw new ArgumentNullException(nameof(species));
            if (!string.IsNullOrEmpty(configuredSpeciesId) && configuredSpeciesId != species.id)
                throw new InvalidOperationException("The configured dragon rig belongs to a different discovered species.");
            if (animal == null || animator == null || body == null || !animal.CloneStates || animal.gameObject != animator.gameObject ||
                animal.Anim != animator || animal.states == null || animal.states.Count == 0 || animator.runtimeAnimatorController == null ||
                animator.avatar == null || !animator.avatar.isValid)
                throw new InvalidOperationException("An authored compatible Malbers dragon rig, Avatar, states and controller are required.");
            if (!gameObject.activeInHierarchy)
                throw new InvalidOperationException("Activate the configured dragon before preparing its vendor controller.");
            if (Paused) Pause(false);
            StagedAnimationEvaluations=0;
            Species = species; Prepared = true;
            animal.CloneStates = true; animal.UseCameraInput = false;
            animal.UpdateInputSource(false); // no second global/vendor player input owner
            animal.LockInput = false; animal.LockMovement = false;
            animator.keepAnimatorStateOnDisable = true;
            Array.Clear(nextActionAt, 0, nextActionAt.Length);
            observedAction = null; contactReported = false; ActionNormalizedTime = LastActionNormalizedTime = 0; LastActionCompleted = false;
            landingObserved=landingNaturalExit=landingInterrupted=landingReceiptCancelled=false;landingNormalized=0;CompletedLandings=0;
            LastPrepareModeResetFrame=Time.frameCount;LastPreparePlayingModesReset=LastPrepareStalePlayingModesReset=0;LastPrepareModeResetDiagnosis="";
            // V234 proves a prior match can leave mode.PlayingMode true while
            // the animal itself no longer plays it. Interrupting Animator
            // parameters alone does not reset that vendor-owned mode object.
            // Use its public reset only at this explicit prepare boundary.
            if(animal.modes!=null)foreach(var mode in animal.modes)if(mode!=null&&mode.PlayingMode)
            {
                bool animalPlaying=animal.IsPlayingMode,wasActive=ReferenceEquals(animal.ActiveMode,mode),active=mode.Active,inCooldown=mode.InCoolDown;int temporal=mode.TemporalActivation;var id=mode.ID;var abilities=mode.Abilities;int abilityCount=abilities!=null?abilities.Count:0;
                mode.Reset();
                // The public Reset clears ActiveAbility, but its active native
                // pointer must also finish the ordinary Exit lifecycle before
                // ResetController can query that ability on a state change.
                if(wasActive)mode.Exit();
                LastPreparePlayingModesReset++;if(!animalPlaying)LastPrepareStalePlayingModesReset++;
                LastPrepareModeResetDiagnosis+="mode="+mode.Name+",animalPlayingBefore="+animalPlaying+",modePlayingBefore=True,modePlayingAfter="+mode.PlayingMode+",active="+active+"->"+mode.Active+",temporal="+temporal+"->"+mode.TemporalActivation+",inCooldown="+inCooldown+"->"+mode.InCoolDown+",wasNativeActive="+wasActive+",nativePointerCleared="+(!wasActive||animal.ActiveMode==null)+",sameModeId="+ReferenceEquals(id,mode.ID)+",sameAbilityList="+ReferenceEquals(abilities,mode.Abilities)+",abilityCount="+abilityCount+"->"+(mode.Abilities!=null?mode.Abilities.Count:0)+";";
            }
            animal.Mode_Interrupt_Forced(); animal.ResetController(); animal.Reset_Movement();
            if (!body.isKinematic) body.linearVelocity = body.angularVelocity = Vector3.zero;
        }

        public void MoveWorld(Vector3 desiredDirection, bool sprint, bool wantsFlight)
        {
            if (!Prepared || Paused || !isActiveAndEnabled || !Finite(desiredDirection)) return;
            if(wantsFlight&&landingObserved)ClearLandingObservation();
            bool canFly = Species.Supports(DragonSpeciesRole.Flying) && flightState != null;
            var fly = animal.State_Get<Fly>();
            if (fly != null)
            {
                // While commanded to fly, ground proximity must not immediately
                // undo takeoff. Landing remains the vendor's real surface test.
                bool canLand = !wantsFlight && Species.Supports(DragonSpeciesRole.Ground) && Species.HasAnimation(DragonAnimationAction.Land);
                fly.canLand.Value = canLand;
                // Keep ordinary Fly input until its actual configured surface
                // ray accepts support. Clearing it here permits Fall before a
                // genuine landing, erasing the Fly->ground animation contract.
            }
            // State_TryActivate is only a predicate in this owned vendor version;
            // it neither sets Fly's required input nor activates the state. Use
            // its ordinary public input contract and normal state arbitration.
            if (wantsFlight && canFly && animal.ActiveStateID != flightState)
                animal.State_InputTrue(flightState);
            animal.Sprint_Set(sprint);
            animal.Move(Vector3.ClampMagnitude(desiredDirection, 1));
        }

        public void StopMotion()
        {
            if (animal == null) return;
            animal.Sprint_Set(false); animal.StopMoving();
        }

        // True means the vendor accepted the request. Actual animation playback
        // is observed below; gameplay must wait for that evidence/contact frame.
        public bool TryAction(DragonAnimationAction action, float cooldown = 1)
        {
            // Preserve the original predicate order and short-circuit result;
            // distinguish real rejection branches without a second attempt.
            if (!Prepared) return RecordActualActionRequest(action, false, "NotPrepared");
            if (Paused) return RecordActualActionRequest(action, false, "Paused");
            if (!float.IsFinite(cooldown) || cooldown < 0) return RecordActualActionRequest(action, false, "InvalidCooldown");
            if (!Species.HasAnimation(action)) return RecordActualActionRequest(action, false, "MissingSpeciesAnimation");
            if (animal.IsPreparingMode) return RecordActualActionRequest(action, false, "NativePreparingMode");
            if (animal.IsPlayingMode) return RecordActualActionRequest(action, false, "NativePlayingMode");
            int index = (int)action;
            if (index < 0 || index >= nextActionAt.Length) return RecordActualActionRequest(action, false, "InvalidActionIndex");
            if (Time.time < nextActionAt[index]) return RecordActualActionRequest(action, false, "ExistingMotorCooldown until="+nextActionAt[index].ToString("R"));
            var binding = Find(action);
            if (binding == null || binding.mode == null || binding.abilityIndex <= 0) return RecordActualActionRequest(action, false, "MissingModeBinding");
            string before=DescribeActualActionState();
            if (!animal.Mode_TryActivate(binding.mode.ID, binding.abilityIndex)) return RecordActualActionRequest(action, false, "NativeModeTryActivateRejected mode="+binding.mode.ID+" ability="+binding.abilityIndex, before);
            ClearLandingObservation();
            requestAt = Time.time;
            nextActionAt[index] = Time.time + cooldown;
            return RecordActualActionRequest(action, true, "Accepted mode="+binding.mode.ID+" ability="+binding.abilityIndex, before);
        }
        bool RecordActualActionRequest(DragonAnimationAction action,bool accepted,string reason,string before=null)
        {
            LastRequestedAction=action;LastActionRequestFrame=Time.frameCount;LastActionRequestTime=Time.time;
            LastActionRequestDiagnosis=reason+" | before="+(before??"not invoked")+" | actual="+DescribeActualActionState();return accepted;
        }
        string DescribeActualActionState()
        {
            if(animal==null)return "animal=null";
            var state=animal.ActiveState;string text="prepared="+Prepared+",paused="+Paused+",state="+(state!=null?state.GetType().Name:"none")+",core="+(state!=null&&state.InCoreAnimation)+",stateNoModes="+(state!=null&&state.NoModes)+",preparing="+animal.IsPreparingMode+",playing="+animal.IsPlayingMode;
            if(animal.modes!=null)foreach(var mode in animal.modes)if(mode!=null)text+=";mode=["+mode.Name+",active="+mode.Active+",temporal="+mode.TemporalActivation+",playing="+mode.PlayingMode+",hasCooldown="+mode.HasCoolDown+",inCooldown="+mode.InCoolDown+",activationTime="+mode.ActivationTime.ToString("R")+",ability="+(mode.ActiveAbility!=null?mode.ActiveAbility.Name:"none")+"]";
            return text;
        }

        public void InterruptAction()
        {
            actionCancelled = true;
            if (animal != null) animal.Mode_Interrupt();
        }

        public void CancelLandingReceipt()=>ClearLandingObservation();
        void ClearLandingObservation()
        {
            if(landingObserved)landingReceiptCancelled=true;
            landingObserved=landingNaturalExit=landingInterrupted=false;landingNormalized=0;
        }

        // Explicit pre-race presentation only: the vendor evaluates its real
        // graph, while the existing pause keeps the dynamic root held safely.
        // Ordinary Pause users retain their existing frozen-Animator behavior.
        public bool BeginStagedAnimationHold()
        {
            if(!Prepared||!Paused||animal==null||animator==null||body==null||!body.isKinematic)return false;
            if(StagedAnimationHeld)return true;
            stagedOldUpdateMode=animator.updateMode;stagedOldDisablePosition=animal.DisablePosition;stagedOldDisableRotation=animal.DisableRotation;
            StagedAnimationHeld=true;StagedAnimationEvaluations=0;animal.PostStateMovement+=HoldStagedRoot;
            animal.DisablePosition=animal.DisableRotation=true;
            if(Species.Supports(DragonSpeciesRole.Flying)&&flightState!=null)animal.State_InputTrue(flightState);
            animator.updateMode=AnimatorUpdateMode.Normal;animator.enabled=true;return true;
        }
        public void FreezeStagedAnimation(bool frozen)
        {if(StagedAnimationHeld&&animator!=null)animator.enabled=!frozen;}
        void HoldStagedRoot(MAnimal source)
        {
            if(!StagedAnimationHeld||source!=animal)return;
            // The vendor invokes this after state/tag modifiers and before its
            // position/rotation writes. No root pose or velocity is assigned.
            source.DisablePosition=source.DisableRotation=true;source.LockInput=source.LockMovement=true;StagedAnimationEvaluations++;
        }
        void EndStagedAnimationHold()
        {
            if(!StagedAnimationHeld)return;
            if(animal!=null){animal.PostStateMovement-=HoldStagedRoot;animal.DisablePosition=stagedOldDisablePosition;animal.DisableRotation=stagedOldDisableRotation;}
            if(animator!=null)animator.updateMode=stagedOldUpdateMode;StagedAnimationHeld=false;
        }
        public void Pause(bool paused)
        {
            if(!paused)EndStagedAnimationHold();
            if (Paused == paused || animal == null || animator == null || body == null) return;
            if (paused)
            {
                oldKinematic = body.isKinematic; oldAnimatorEnabled = animator.enabled; oldAnimatorSpeed = animator.speed;
                oldLockInput = animal.LockInput; oldLockMovement = animal.LockMovement;
                pausedVelocity = body.linearVelocity; pausedAngularVelocity = body.angularVelocity;
                animal.LockInput = animal.LockMovement = true;
                animator.keepAnimatorStateOnDisable = true;
                // Disabling only Animator preserves MAnimal's state; toggling
                // MAnimal.enabled or Sleep would reset the controller on resume.
                animator.enabled = false; body.isKinematic = true;
            }
            else
            {
                body.isKinematic = oldKinematic;
                if (!oldKinematic) { body.linearVelocity = pausedVelocity; body.angularVelocity = pausedAngularVelocity; }
                animal.LockInput = oldLockInput; animal.LockMovement = oldLockMovement;
                animator.speed = oldAnimatorSpeed; animator.enabled = oldAnimatorEnabled;
            }
            Paused = paused;
        }

        void LateUpdate()
        {
            if (!Prepared || Paused) return;
            ObserveBaseLanding();
            var mode = animal.ActiveMode;
            DragonMotorActionBinding current = null;
            AnimatorStateInfo playback = default;
            if (mode != null && mode.PlayingMode && mode.ActiveAbility != null)
                foreach (var binding in actionBindings)
                    if (binding != null && binding.mode != null && binding.mode.ID == mode.ID.ID &&
                        binding.abilityIndex == mode.ActiveAbility.Index.Value && TryReadActionPlayback(binding,mode.LayerIndex,out playback)) { current = binding; break; }
            if (current != observedAction)
            {
                var previous = observedAction;
                // An outgoing empty/other state may have an unrelated clock.
                // Only the previously observed mapped action proves completion.
                LastActionNormalizedTime = previous != null ? ActionNormalizedTime : 0;
                LastActionCompleted = previous != null && !previous.continuous && !actionCancelled && LastActionNormalizedTime >= .9f;
                observedAction = current; contactReported = false; ActionNormalizedTime = 0;
                if (previous != null) ActionFinished?.Invoke(previous.action);
                if (current != null) { actionCancelled = false; ActionStarted?.Invoke(current.action); }
            }
            if (current != null)
            {
                ActionNormalizedTime = playback.normalizedTime;
                if (!contactReported && ActionNormalizedTime >= current.contactNormalizedTime)
                { contactReported = true; ActionContact?.Invoke(current.action); }
            }
            // A missing/miswired Animator transition cannot reserve the vendor
            // mode forever. This cancels a failed request; it never reports a hit.
            if (animal.IsPreparingMode && Time.time - requestAt > 3) { actionCancelled = true; animal.Mode_Interrupt_Forced(); }
        }

        // Fixed-mode Animator can evaluate several times between rendered
        // frames. Read the previous evaluation each fixed step too, so genuine
        // short exit transitions remain observable during a declared4x run.
        // This performs no movement, input or animation mutation.
        void FixedUpdate(){if(Prepared&&!Paused)ObserveBaseLanding();}

        bool TryReadActionPlayback(DragonMotorActionBinding binding,int layer,out AnimatorStateInfo state)
            =>TryReadMappedPlayback(binding.action,layer,"DragonAction_"+binding.action,out state);

        bool TryReadMappedPlayback(DragonAnimationAction action,int layer,string tag,out AnimatorStateInfo state)
        {
            state=default;
            if(layer<0||layer>=animator.layerCount||layer>0&&animator.GetLayerWeight(layer)<=0||
                !Species.TryGetAnimation(action,out var mapping))return false;
            var actualClip=ResolveMappedClip(mapping.clip);if(actualClip==null)return false;
            var current=animator.GetCurrentAnimatorStateInfo(layer);currentClips.Clear();animator.GetCurrentAnimatorClipInfo(layer,currentClips);
            if(current.IsTag(tag)&&ContainsActualClip(currentClips,actualClip)){state=current;return true;}
            // ModeBehaviour.OnStateEnter runs when an incoming transition starts,
            // while GetCurrentAnimatorStateInfo still describes No Action. Read
            // the genuinely weighted incoming clip and its own normalized time.
            if(animator.IsInTransition(layer))
            {
                var incoming=animator.GetNextAnimatorStateInfo(layer);nextClips.Clear();animator.GetNextAnimatorClipInfo(layer,nextClips);
                if(incoming.IsTag(tag)&&ContainsActualClip(nextClips,actualClip)){state=incoming;return true;}
            }
            return false;
        }
        void ObserveBaseLanding()
        {
            bool visibleLand=TryReadMappedPlayback(DragonAnimationAction.Land,0,LandingAnimatorTag,out var playback);
            // A cancelled operation cannot re-arm from the remaining weighted
            // tail of that same clip. Preserve truthful LandingPlaying until
            // it actually exits, then allow the next distinct landing.
            if(landingReceiptCancelled){if(visibleLand)return;landingReceiptCancelled=false;}
            if(visibleLand)
            {
                if(!landingObserved){landingObserved=true;landingNaturalExit=landingInterrupted=false;landingNormalized=0;}
                landingNormalized=Mathf.Max(landingNormalized,playback.normalizedTime);
                var current=animator.GetCurrentAnimatorStateInfo(0);
                if(current.IsTag(LandingAnimatorTag)&&animator.IsInTransition(0))
                {
                    var next=animator.GetNextAnimatorStateInfo(0);var transition=animator.GetAnimatorTransitionInfo(0);
                    bool groundTag=idleState!=null&&next.IsTag(idleState.name)||groundState!=null&&next.IsTag(groundState.name);
                    // Transition identity proves the natural exit; the exact
                    // weighted Land clock still must reach .9 before a receipt
                    // can be counted below. Its first blended sample can precede
                    // that threshold and is not itself an interruption.
                    if(!transition.anyState&&groundTag)landingNaturalExit=true;
                    else landingInterrupted=true;
                }
                return;
            }
            if(!landingObserved)return;
            bool realGround=animal.Grounded&&(animal.ActiveStateID==idleState||animal.ActiveStateID==groundState);
            string tag=animal.ActiveStateID==idleState?idleState.name:groundState!=null?groundState.name:string.Empty;
            bool visibleCore=realGround&&(TryReadMappedPlayback(DragonAnimationAction.GroundIdle,0,tag,out _)||TryReadMappedPlayback(DragonAnimationAction.Walk,0,tag,out _)||TryReadMappedPlayback(DragonAnimationAction.Run,0,tag,out _)||TryReadMappedPlayback(DragonAnimationAction.TurnLeft,0,tag,out _)||TryReadMappedPlayback(DragonAnimationAction.TurnRight,0,tag,out _));
            // Clearing the landing lock can advance the vendor's active ground
            // state before its normal Animator transition has caught up. Keep
            // the genuinely witnessed exit pending while supported; only the
            // matching real weighted core clip can finish the receipt.
            if(landingNaturalExit&&!landingInterrupted&&landingNormalized>=.9f&&realGround&&!visibleCore)return;
            if(landingNaturalExit&&!landingInterrupted&&landingNormalized>=.9f&&visibleCore)CompletedLandings++;
            landingObserved=landingNaturalExit=landingInterrupted=false;landingNormalized=0;
        }
        static bool ContainsActualClip(List<AnimatorClipInfo> clips,AnimationClip actual)
        {foreach(var clip in clips)if(clip.clip==actual&&clip.weight>.0001f)return true;return false;}

        // Mode-local authored poses can replace catalog clips through the real
        // Unity override controller. Resolve exact clip references from inner to
        // outer controllers, retaining all weighted/tag/time observation gates.
        // Re-read the public bindings so in-place ApplyOverrides also takes effect;
        // lists are reused and names never establish clip identity.
        AnimationClip ResolveMappedClip(AnimationClip catalogClip)
        {
            if(catalogClip==null||animator==null||animator.runtimeAnimatorController==null)return null;
            overrideChain.Clear();RuntimeAnimatorController controller=animator.runtimeAnimatorController;
            while(controller is AnimatorOverrideController overrides)
            {
                if(overrideChain.Count>=32||overrideChain.Contains(overrides))return null;
                overrideChain.Add(overrides);controller=overrides.runtimeAnimatorController;
            }
            AnimationClip actual=catalogClip;
            for(int layer=overrideChain.Count-1;layer>=0;layer--)
            {
                overrideClips.Clear();overrideChain[layer].GetOverrides(overrideClips);
                bool found=false;
                foreach(var pair in overrideClips)if(pair.Key==actual){if(pair.Value!=null)actual=pair.Value;found=true;break;}
                if(!found)foreach(var pair in overrideClips)if(pair.Key==catalogClip){if(pair.Value!=null)actual=pair.Value;break;}
            }
            return actual;
        }

        DragonMotorActionBinding Find(DragonAnimationAction action)
        {
            foreach (var binding in actionBindings) if (binding != null && binding.action == action) return binding;
            return null;
        }
        void OnDisable() { if (Paused) Pause(false); StopMotion(); observedAction = null;landingObserved=landingNaturalExit=landingInterrupted=landingReceiptCancelled=false;landingNormalized=0; }
        static bool Finite(Vector3 value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
