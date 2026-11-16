using System;
using System.Collections.Generic;
using LWS.TruckSports;
using MalbersAnimations;
using UnityEngine;

namespace LWS.TruckDragonKingdom
{
    public enum DragonCharacterIntent { Passive, Patrol, SleepingGuardian, Pursuit, ProtectNest, PlayerMounted }

    // Match-owned dragon intent/perception/combat coordination over the vendor
    // motor. Tick is explicit: disabled variants never start an autonomous loop.
    public sealed class DragonCharacter : MonoBehaviour
    {
        public const string MouthSocketName="LWS Dragon Mouth Socket";
        public DragonAnimalMotor motor;
        public Transform mouth, saddle, leftGrip, rightGrip;
        public Collider[] physicalColliders = Array.Empty<Collider>();
        public AudioSource voice;
        public Light warningLight;
        public LayerMask obstructionMask = ~0;
        [Min(1)] public float sightRange = 140, hearingRange = 110, memorySeconds = 6;
        [Range(20, 360)] public float sightAngle = 155;
        [Min(.1f)] public float warningDuration = 1.2f, attackCooldown = 6;
        [Min(1)] public float breathRange = 28, clawRange = 8;
        [Range(1, 70)] public float breathHalfAngle = 26;
        [Range(0, 5)] public float maximumImpulse = 2.4f;
        [Min(0)] public float attackDamage = 12;
        [Min(.1f)] public float clearanceRadius = 3, clearanceDistance = 16;
        public DragonCharacterIntent Intent { get; private set; }
        public Vector3 Goal { get; private set; }
        public int TargetSlot { get; private set; } = -1;
        public bool Prepared { get; private set; }
        public bool Paused { get; private set; }
        public Vector3 Position => motor != null ? motor.transform.position : transform.position;
        public Func<int, bool> TargetEligible { get; set; }
        public Func<int, bool> SleepingWakeEligible { get; set; }
        public float AttackReadyIn => Mathf.Max(0, attackReadyAt - clock);
        public float WarningSecondsRemaining => warningPending ? Mathf.Max(0, warningEnds - clock) : 0;
        public bool HasTargetMemory => ValidTarget(TargetSlot) && memories[TargetSlot].valid && clock - memories[TargetSlot].heardAt <= memorySeconds;
        public event Action<int, DragonElement, float, Vector3> TruckHit;
        public event Action<int, Vector3, float> AttackWarning;

        struct Memory
        {
            public bool valid;
            public Vector3 position, velocity;
            public float heardAt;
        }
        readonly Memory[] memories = new Memory[12];
        readonly RaycastHit[] sightHits = new RaycastHit[32];
        readonly Collider[] attackContacts = new Collider[96];
        SportsEventTruck[] targets = Array.Empty<SportsEventTruck>();
        Rigidbody[][] assemblies;
        DragonSpeciesDefinition species;
        DragonEyelidPresentation eyelids;
        System.Random random;
        int count, protectedSlot = -1, hitMask;
        float clock, senseAt, attackReadyAt, warningEnds;
        bool warningPending, wakingRequested, waitingForWake, landingRequested;
        DragonAnimationAction warnedAction;
        GameObject breathObject;
        FireBreath breath;
        ParticleSystem[] breathParticles = Array.Empty<ParticleSystem>();
        SportsSettings audioSettings;
        AudioSource fireVoice;
        AudioSource[] effectVoices=Array.Empty<AudioSource>();
        float[] effectVoiceGains=Array.Empty<float>();
        readonly List<AudioClip> warningClips=new List<AudioClip>(4);
        float voiceEndsAt,nextVoiceAt;
        bool breathActive;
        const string VoiceEmitterName="Owned dragon mouth voice";

        // The shared settings retain global AudioListener master ownership.
        // Binding is inert before Prepare, including disabled Mayhem variants.
        public void BindAudio(SportsSettings settings)
        {
            audioSettings=settings;ApplyAudioGains();
            foreach(var wings in GetComponentsInChildren<DragonWingAudio>(true))wings.BindAudio(settings);
        }

        public void Prepare(DragonSpeciesDefinition definition, SportsEventTruck[] trucks, int participantCount, int seed)
        {
            if (definition == null || trucks == null || participantCount < 0 || participantCount > 12 || participantCount > trucks.Length)
                throw new ArgumentException("A discovered species and valid bounded original truck roster are required.");
            ResetCharacter();
            species = definition; count = participantCount; targets = (SportsEventTruck[])trucks.Clone(); random = new System.Random(seed);
            if (motor == null) motor = GetComponentInChildren<DragonAnimalMotor>(true);
            if (motor == null) throw new InvalidOperationException("A configured Malbers DragonAnimalMotor is required.");
            motor.Prepare(definition);
            eyelids = motor.GetComponentInChildren<DragonEyelidPresentation>(true);
            var mouthBone=FindBone(definition.mouthBone);mouth=mouthBone!=null?mouthBone.Find(MouthSocketName):null;saddle = FindBone(definition.saddleBone);
            leftGrip = FindBone(definition.leftGripBone); rightGrip = FindBone(definition.rightGripBone);
            if (mouth == null) throw new InvalidOperationException("The actual rig requires its explicitly authored calibrated mouth socket before perception/attacks. Upgrade the owned configured animal prefab.");
            physicalColliders = motor.GetComponentsInChildren<Collider>(true);
            assemblies = new Rigidbody[count][];
            for (int i = 0; i < count; i++) assemblies[i] = targets[i] != null ? targets[i].GetComponentsInChildren<Rigidbody>(true) : Array.Empty<Rigidbody>();
            motor.ActionStarted += ActionStarted; motor.ActionContact += ActionContact; motor.ActionFinished += ActionFinished;
            PrepareMouthAudio();
            if (warningLight == null)
            {
                var marker = new GameObject("Dragon attack warning glow"); marker.transform.SetParent(mouth, false);
                warningLight = marker.AddComponent<Light>(); warningLight.type = LightType.Point;
                warningLight.color = new Color(1, .35f, .06f); warningLight.intensity = 5; warningLight.range = 14;
            }
            warningLight.enabled = false;
            if (definition.breathEffect != null)
            {
                breathObject = Instantiate(definition.breathEffect, mouth);
                breathObject.name = "Owned dragon breath · " + definition.id;
                breathObject.transform.localPosition = Vector3.zero; breathObject.transform.localRotation = Quaternion.identity;
                breath = breathObject.GetComponentInChildren<FireBreath>(true);
                breathParticles = breathObject.GetComponentsInChildren<ParticleSystem>(true);
                effectVoices=breathObject.GetComponentsInChildren<AudioSource>(true);effectVoiceGains=new float[effectVoices.Length];
                for(int i=0;i<effectVoices.Length;i++){effectVoiceGains[i]=effectVoices[i].volume;effectVoices[i].playOnAwake=false;effectVoices[i].Stop();}
                if (breath != null) breath.Activate(false);
                foreach (var particle in breathParticles) particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            clock = senseAt = 0; attackReadyAt = 2; Prepared = true; Intent = DragonCharacterIntent.Passive;
            voiceEndsAt=nextVoiceAt=0;breathActive=false;ApplyAudioGains();
            Goal = motor.transform.position; TargetSlot = -1;
        }

        public void SetIntent(DragonCharacterIntent intent, Vector3 goal, int targetSlot = -1)
        {
            if (!Finite(goal) || targetSlot < -1 || targetSlot >= count) return;
            if (Intent != intent)
            {
                CancelWarning(); motor?.InterruptAction(); wakingRequested = waitingForWake = false;
            }
            landingRequested = false; Intent = intent; Goal = goal; TargetSlot = targetSlot;
            if (intent == DragonCharacterIntent.Passive || intent == DragonCharacterIntent.SleepingGuardian) motor?.StopMotion();
        }

        public void ReportNoise(int slot, Vector3 position, float loudness)
        {
            if (!Prepared || Paused || slot < 0 || slot >= count || !ValidTarget(slot) || !Finite(position) ||
                !float.IsFinite(loudness) || loudness <= 0 || mouth == null) return;
            float range = hearingRange * Mathf.Sqrt(Mathf.Clamp(loudness, 0, 4));
            if ((position - mouth.position).sqrMagnitude > range * range) return;
            memories[slot] = new Memory { valid = true, position = position, velocity = Vector3.zero, heardAt = clock };
        }

        public bool TryReadRememberedTarget(int slot, out Vector3 position, out float age)
        {
            position = default; age = float.PositiveInfinity;
            if (!ValidTarget(slot) || !memories[slot].valid) return false;
            age = clock - memories[slot].heardAt;
            if (age > memorySeconds) return false;
            position = memories[slot].position; return true;
        }

        public bool CanObserveTarget(int slot) => Prepared && !Paused && CanSee(slot);

        public bool RequestLanding(Vector3 physicalLandingGoal)
        {
            if (!Prepared || Paused || !Finite(physicalLandingGoal) || !species.Supports(DragonSpeciesRole.Ground | DragonSpeciesRole.Flying) ||
                !species.HasAnimation(DragonAnimationAction.Land)) return false;
            motor.CancelLandingReceipt();
            SetIntent(DragonCharacterIntent.Patrol, physicalLandingGoal); landingRequested = true; return true;
        }

        public void Tick(float dt)
        {
            if (!Prepared || Paused || !float.IsFinite(dt) || dt <= 0) return;
            clock += dt;
            ApplyAudioGains();if(voiceEndsAt>0&&clock>=voiceEndsAt){voice.Stop();voiceEndsAt=0;}
            if (clock >= senseAt) { SenseTargets(); senseAt = clock + .14f; }
            TickWarning();
            if (Intent == DragonCharacterIntent.PlayerMounted) return; // human host owns its motor command
            if (Intent == DragonCharacterIntent.Passive) { motor.StopMotion(); return; }
            if (Intent == DragonCharacterIntent.SleepingGuardian)
            {
                motor.StopMotion();
                int sensed = SelectRememberedTarget(true);
                if (sensed < 0)
                {
                    if (!motor.ActionPlaying && !wakingRequested) motor.TryAction(DragonAnimationAction.Sleep, 2);
                    return;
                }
                TargetSlot = sensed;
                // Sleeping and waking must each have real clips. A missing wake
                // action remains unsupported instead of an instant fake wake.
                if (!species.HasAnimation(DragonAnimationAction.Wake)) return;
                if (!wakingRequested) { motor.InterruptAction(); wakingRequested = true; }
                if (!waitingForWake && !motor.ActionPlaying && motor.TryAction(DragonAnimationAction.Wake, 2)) waitingForWake = true;
                return;
            }
            Vector3 destination = Goal;
            if (Intent == DragonCharacterIntent.Pursuit || Intent == DragonCharacterIntent.ProtectNest)
            {
                int chosen = SelectRememberedTarget();
                if (chosen >= 0)
                {
                    TargetSlot = chosen;
                    var memory = memories[chosen];
                    // Prediction uses the velocity observed at last sight, never
                    // a hidden truck's current Rigidbody pose or velocity.
                    destination = memory.position + memory.velocity * Mathf.Clamp(clock - memory.heardAt + .4f, 0, 1.1f);
                    if (!warningPending && clock >= attackReadyAt && CanSee(chosen))
                    {
                        float range = Vector3.Distance(mouth.position, targets[chosen].body.worldCenterOfMass);
                        if (range <= clawRange && species.HasAnimation(DragonAnimationAction.ClawAttack)) BeginWarning(DragonAnimationAction.ClawAttack, chosen, -1);
                        else if (range <= breathRange && species.element != DragonElement.None && species.HasAnimation(DragonAnimationAction.Breath)) BeginWarning(DragonAnimationAction.Breath, chosen, -1);
                    }
                }
                else if (Intent == DragonCharacterIntent.Pursuit)
                { TargetSlot = -1; motor.StopMotion(); return; }
            }
            Vector3 difference = destination - motor.transform.position;
            bool flying = species.Supports(DragonSpeciesRole.Flying);
            Vector3 support = default;
            bool landingSupport = landingRequested && Vector3.ProjectOnPlane(difference, Vector3.up).magnitude < 10 && TryLandingSupport(out support);
            if (landingSupport)
            {
                difference = new Vector3(Goal.x, support.y, Goal.z) - motor.transform.position;
                if (motor.IsGrounded && difference.sqrMagnitude < 16)
                { landingRequested = false; Intent = DragonCharacterIntent.Passive; motor.StopMotion(); return; }
                bool closeToSupport = motor.transform.position.y - support.y <= motor.animal.Height + 2;
                // Actual downward support authorizes the vendor landing test.
                // No body pose, grounded flag or result is synthesized here.
                motor.MoveWorld(difference.sqrMagnitude < .01f ? Vector3.down * .2f : difference.normalized, false, !closeToSupport);
                return;
            }
            if (flying && (Intent == DragonCharacterIntent.Pursuit || Intent == DragonCharacterIntent.ProtectNest)) difference.y += Mathf.Clamp(Vector3.Distance(destination, motor.transform.position) * .08f, 2, 14);
            if (!flying) difference.y = 0;
            if (difference.sqrMagnitude < 9) { motor.StopMotion(); return; }
            var direction = AvoidObstacles(difference.normalized, flying);
            motor.MoveWorld(direction, Intent == DragonCharacterIntent.Pursuit, flying);
        }

        bool TryLandingSupport(out Vector3 point)
        {
            point = default; float nearest = float.PositiveInfinity;
            var origin = motor.transform.position + Vector3.up * 2;
            int found = Physics.RaycastNonAlloc(origin, Vector3.down, sightHits, 160, obstructionMask, QueryTriggerInteraction.Ignore);
            if (found == sightHits.Length) return false;
            for (int i = 0; i < found; i++)
            {
                var hit = sightHits[i];
                if (hit.collider == null || hit.collider.transform.IsChildOf(transform) || hit.normal.y < .65f ||
                    hit.collider.GetComponentInParent<SportsEventTruck>() != null || hit.distance >= nearest) continue;
                nearest = hit.distance; point = hit.point;
            }
            return nearest < float.PositiveInfinity;
        }

        // Mounted human attacks share the same telegraph, cooldown and actual
        // contact rules. The mounted truck is explicitly excluded from damage.
        public bool RequestAttack(DragonAnimationAction action, int mountedTruckSlot = -1)
        {
            if (!Prepared || Paused || warningPending || clock < attackReadyAt || motor.ActionPlaying ||
                mountedTruckSlot < -1 || mountedTruckSlot >= count || !IsAttack(action) || !species.HasAnimation(action)) return false;
            if (action == DragonAnimationAction.Breath && species.element == DragonElement.None) return false;
            BeginWarning(action, -1, mountedTruckSlot); return true;
        }

        void BeginWarning(DragonAnimationAction action, int slot, int exclude)
        {
            if (warningPending || motor.ActionPlaying) return;
            warnedAction = action; warningPending = true; warningEnds = clock + Mathf.Max(.35f, warningDuration);
            protectedSlot = exclude; if (slot >= 0) TargetSlot = slot;
            if (warningLight != null) warningLight.enabled = true;
            PlayCue();
            Vector3 warningPoint = slot >= 0 && ValidTarget(slot) ? targets[slot].body.worldCenterOfMass : mouth.position + mouth.forward * breathRange;
            AttackWarning?.Invoke(slot, warningPoint, warningDuration);
        }

        void TickWarning()
        {
            if (!warningPending || clock < warningEnds) return;
            warningPending = false; if (warningLight != null) warningLight.enabled = false;
            if (TargetSlot >= 0 && Intent != DragonCharacterIntent.PlayerMounted && !CanSee(TargetSlot))
            { attackReadyAt = clock + 1; return; }
            bool accepted = motor.TryAction(warnedAction, attackCooldown);
            attackReadyAt = clock + (accepted ? Mathf.Max(1, attackCooldown) : 1);
        }

        void SenseTargets()
        {
            for (int i = 0; i < count; i++)
            {
                if (!CanSee(i)) continue;
                var target = targets[i];
                memories[i] = new Memory { valid = true, position = target.body.worldCenterOfMass,
                    velocity = Vector3.ClampMagnitude(target.body.linearVelocity, 55), heardAt = clock };
            }
        }
        int SelectRememberedTarget(bool waking = false)
        {
            if (TargetSlot >= 0 && ValidTarget(TargetSlot) && (!waking || SleepingWakeEligible == null || SleepingWakeEligible(TargetSlot)) && TryReadRememberedTarget(TargetSlot, out _, out _)) return TargetSlot;
            int selected = -1; float best = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                if (!ValidTarget(i) || waking && SleepingWakeEligible != null && !SleepingWakeEligible(i) || !TryReadRememberedTarget(i, out var position, out _)) continue;
                float distance = (position - motor.transform.position).sqrMagnitude;
                if (distance < best) { best = distance; selected = i; }
            }
            return selected;
        }
        bool CanSee(int slot)
        {
            if (!ValidTarget(slot) || mouth == null || eyelids != null && !eyelids.EyesOpenForVision) return false;
            var delta = targets[slot].body.worldCenterOfMass - mouth.position;
            if (delta.sqrMagnitude > sightRange * sightRange) return false;
            if (delta.sqrMagnitude > .01f && Vector3.Angle(mouth.forward, delta) > sightAngle * .5f) return false;
            return ClearSight(slot, mouth.position, delta);
        }
        bool ClearSight(int slot, Vector3 origin, Vector3 delta)
        {
            float distance = delta.magnitude;
            if (distance < .01f) return true;
            int found = Physics.RaycastNonAlloc(origin, delta / distance, sightHits, distance, obstructionMask, QueryTriggerInteraction.Ignore);
            if (found == sightHits.Length) return false; // saturated evidence cannot prove clear vision
            float nearest = float.PositiveInfinity; Collider first = null;
            for (int i = 0; i < found; i++)
            {
                var hit = sightHits[i]; if (hit.collider == null || hit.collider.transform.IsChildOf(transform)) continue;
                if (hit.distance < nearest) { nearest = hit.distance; first = hit.collider; }
            }
            return first == null || first.transform.IsChildOf(targets[slot].transform);
        }

        Vector3 AvoidObstacles(Vector3 desired, bool flying)
        {
            var origin = motor.transform.position + Vector3.up * Mathf.Max(1, clearanceRadius);
            if (ClearCorridor(origin, desired)) return desired;
            var right = Vector3.Cross(Vector3.up, desired).normalized;
            if (right.sqrMagnitude < .01f) right = motor.transform.right;
            Vector3 a = (desired + right * 1.4f + (flying ? Vector3.up : Vector3.zero)).normalized;
            Vector3 b = (desired - right * 1.4f + (flying ? Vector3.up : Vector3.zero)).normalized;
            if (ClearCorridor(origin, a)) return a;
            if (ClearCorridor(origin, b)) return b;
            if (flying && ClearCorridor(origin, Vector3.up)) return Vector3.up;
            return Vector3.zero; // blocked intent stops; it never moves through scenery
        }
        bool ClearCorridor(Vector3 origin, Vector3 direction)
        {
            if (direction.sqrMagnitude < .01f) return false;
            int found = Physics.SphereCastNonAlloc(origin, clearanceRadius, direction, sightHits, clearanceDistance, obstructionMask, QueryTriggerInteraction.Ignore);
            if (found == sightHits.Length) return false;
            for (int i = 0; i < found; i++)
                if (sightHits[i].collider != null && !sightHits[i].collider.transform.IsChildOf(transform)) return false;
            return true;
        }

        void ActionStarted(DragonAnimationAction action)
        {
            hitMask = 0;
            if (action == DragonAnimationAction.Breath)
            {
                voice?.Stop();voiceEndsAt=0;
                breathActive=true;ApplyAudioGains();
                if (breath != null) breath.Activate(true);
                foreach (var particle in breathParticles) if (!particle.isPlaying) particle.Play();
                if(AudioGain>0)
                {
                    bool authoredAudio=false;foreach(var source in effectVoices)if(source!=null&&source.clip!=null){source.Play();authoredAudio=true;}
                    if(!authoredAudio&&fireVoice!=null&&fireVoice.clip!=null){fireVoice.Stop();fireVoice.Play();}
                }
            }
        }
        void ActionContact(DragonAnimationAction action)
        {
            if (!Prepared || Paused || !IsAttack(action) || !motor.ActionPlaying || motor.CurrentAction != action) return;
            float range = action == DragonAnimationAction.Breath ? breathRange : clawRange;
            int found = Physics.OverlapSphereNonAlloc(mouth.position, range, attackContacts, ~0, QueryTriggerInteraction.Ignore);
            for (int slot = 0; slot < count; slot++)
            {
                if (slot == protectedSlot || !ValidTarget(slot) || (hitMask & (1 << slot)) != 0) continue;
                bool contact = false;
                for (int i = 0; i < found; i++)
                    if (attackContacts[i] != null && attackContacts[i].transform.IsChildOf(targets[slot].transform)) { contact = true; break; }
                if (!contact) continue;
                var delta = targets[slot].body.worldCenterOfMass - mouth.position;
                float angle = action == DragonAnimationAction.TailAttack ? 140 : action == DragonAnimationAction.Breath ? breathHalfAngle : 65;
                Vector3 forward = action == DragonAnimationAction.TailAttack ? -motor.transform.forward : mouth.forward;
                if (Vector3.Angle(forward, delta) > angle || !ClearSight(slot, mouth.position, delta)) continue;
                hitMask |= 1 << slot;
                Vector3 impulse = (delta.normalized + Vector3.up * .25f).normalized * Mathf.Clamp(maximumImpulse, 0, 5);
                foreach (var rigidbody in assemblies[slot]) if (rigidbody != null && !rigidbody.isKinematic) rigidbody.AddForce(impulse, ForceMode.VelocityChange);
                TruckHit?.Invoke(slot, action == DragonAnimationAction.Breath ? species.element : DragonElement.None,
                    Mathf.Max(0, attackDamage), targets[slot].body.worldCenterOfMass);
            }
        }
        void ActionFinished(DragonAnimationAction action)
        {
            if (action == DragonAnimationAction.Breath) StopBreath();
            if (action == DragonAnimationAction.Wake && waitingForWake)
            {
                waitingForWake = false;
                if (motor.LastActionCompleted) { wakingRequested = false; Intent = DragonCharacterIntent.Pursuit; }
            }
        }

        public void Pause(bool paused)
        {
            if (Paused == paused) return;
            Paused = paused; motor?.Pause(paused);
            if (paused)
            {
                voice?.Pause();fireVoice?.Pause();foreach(var source in effectVoices)if(source!=null)source.Pause();
                if(breath!=null)breath.Activate(false);foreach(var particle in breathParticles)if(particle!=null)particle.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                if (warningLight != null) warningLight.enabled = false;
            }
            else
            {
                ApplyAudioGains();if(Prepared){voice?.UnPause();if(breathActive&&motor.ActionPlaying&&motor.CurrentAction==DragonAnimationAction.Breath){if(breath!=null)breath.Activate(true);foreach(var particle in breathParticles)if(particle!=null)particle.Play();fireVoice?.UnPause();foreach(var source in effectVoices)if(source!=null)source.UnPause();}}
                if (warningLight != null) warningLight.enabled = warningPending;
            }
        }
        public void ResetCharacter()
        {
            Prepared = false; if (Paused) Pause(false);
            if (motor != null)
            { motor.ActionStarted -= ActionStarted; motor.ActionContact -= ActionContact; motor.ActionFinished -= ActionFinished; motor.CancelLandingReceipt(); motor.InterruptAction(); motor.StopMotion(); }
            CancelWarning(); StopBreath(); voice?.Stop();
            if (breathObject != null) { breathObject.SetActive(false); Destroy(breathObject); breathObject = null; }
            breath = null; breathParticles = Array.Empty<ParticleSystem>();
            effectVoices=Array.Empty<AudioSource>();effectVoiceGains=Array.Empty<float>();voiceEndsAt=nextVoiceAt=0;
            Array.Clear(memories, 0, memories.Length); count = 0; targets = Array.Empty<SportsEventTruck>();
            eyelids?.ResetPresentation();
            assemblies = null; eyelids = null; TargetSlot = protectedSlot = -1; hitMask = 0; clock = 0; Intent = DragonCharacterIntent.Passive;
            wakingRequested = waitingForWake = landingRequested = false;
        }
        void CancelWarning() { warningPending = false; if (warningLight != null) warningLight.enabled = false; }
        void StopBreath()
        {
            breathActive=false;fireVoice?.Stop();foreach(var source in effectVoices)if(source!=null)source.Stop();
            if (breath != null) breath.Activate(false);
            foreach (var particle in breathParticles) if (particle != null) particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        void PlayCue()
        {
            ApplyAudioGains();if(voice==null||AudioGain<=0||voice.isPlaying||clock<nextVoiceAt||warningClips.Count==0)return;
            var clip=warningClips[random.Next(warningClips.Count)];voice.clip=clip;voice.Play();
            voiceEndsAt=clock+Mathf.Min(2.5f,clip.length);nextVoiceAt=clock+Mathf.Max(3,warningDuration+1);
        }
        float AudioGain=>audioSettings!=null&&audioSettings.master>0?Mathf.Clamp01(audioSettings.sfx):0;
        void PrepareMouthAudio()
        {
            if(voice==null||voice.transform.parent!=mouth||voice.gameObject.name!=VoiceEmitterName)
            {
                if(voice!=null){voice.Stop();voice.playOnAwake=false;voice.loop=false;voice.clip=null;}
                var prior=mouth.Find(VoiceEmitterName);var owner=prior!=null?prior.gameObject:new GameObject(VoiceEmitterName);if(prior==null)owner.transform.SetParent(mouth,false);
                voice=owner.GetComponent<AudioSource>();if(voice==null)voice=owner.AddComponent<AudioSource>();
            }
            ConfigureSpatial(voice);warningClips.Clear();AudioClip fireClip=null;
            foreach(var clip in species.sounds??Array.Empty<AudioClip>())
            {
                if(clip==null)continue;
                if(clip.name.IndexOf("growl",StringComparison.OrdinalIgnoreCase)>=0||clip.name.IndexOf("roar",StringComparison.OrdinalIgnoreCase)>=0)warningClips.Add(clip);
                if(species.element==DragonElement.Fire&&string.Equals(clip.name,"Fire Throw",StringComparison.OrdinalIgnoreCase))fireClip=clip;
            }
            bool authoredAudio=false;if(species.breathEffect!=null)foreach(var source in species.breathEffect.GetComponentsInChildren<AudioSource>(true))if(source.clip!=null){authoredAudio=true;break;}
            if(fireClip!=null&&!authoredAudio&&species.HasAnimation(DragonAnimationAction.Breath))
            {
                if(fireVoice==null){var prior=mouth.Find("FireBreathAudio");var owner=prior!=null?prior.gameObject:new GameObject("FireBreathAudio");if(prior==null)owner.transform.SetParent(mouth,false);fireVoice=owner.GetComponent<AudioSource>();if(fireVoice==null)fireVoice=owner.AddComponent<AudioSource>();}
                ConfigureSpatial(fireVoice);fireVoice.clip=fireClip;
            }
        }
        static void ConfigureSpatial(AudioSource source)
        {source.Stop();source.playOnAwake=false;source.loop=false;source.spatialBlend=1;source.minDistance=8;source.maxDistance=180;source.rolloffMode=AudioRolloffMode.Logarithmic;source.dopplerLevel=0;}
        void ApplyAudioGains()
        {
            float gain=AudioGain;
            if(voice!=null){voice.volume=.65f*gain;voice.mute=gain<=0;}
            if(fireVoice!=null){fireVoice.volume=.5f*gain;fireVoice.mute=gain<=0;}
            for(int i=0;i<effectVoices.Length;i++)if(effectVoices[i]!=null){effectVoices[i].volume=effectVoiceGains[i]*gain;effectVoices[i].mute=gain<=0;}
        }
        Transform FindBone(string name)
        { foreach (var bone in motor.GetComponentsInChildren<Transform>(true)) if (bone.name == name) return bone; return null; }
        bool ValidTarget(int slot) => slot >= 0 && slot < count && targets[slot] != null && targets[slot].gameObject.activeInHierarchy && targets[slot].body != null && targets[slot].body.detectCollisions && (TargetEligible == null || TargetEligible(slot));
        static bool IsAttack(DragonAnimationAction action) => action == DragonAnimationAction.Breath || action == DragonAnimationAction.ClawAttack || action == DragonAnimationAction.TailAttack;
        static bool Finite(Vector3 value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
        void OnDisable() => ResetCharacter();
    }
}
