using System;
using UnityEngine;
using UnityEngine.AI;
using LWS.TruckCamping;
namespace LWS.TruckPetPals
{
    [Serializable] public sealed class PetVisual
    {
        public PetKind kind;
        public GameObject root;
        public CampLivingArt living;
        public PetTruckArt miniature;
        public Transform collar;
        public Transform peeAnchor, kickAnchor;
        public Renderer[] dirtSurfaces;
    }
    public sealed class PetCompanion : MonoBehaviour
    {
        public CampingMatch match;
        public CampExplorer owner;
        public PetProfileRepository repository;
        public NavMeshAgent motor;
        public Rigidbody physicalBody;
        public PetVisual[] visualTypes;
        public LineRenderer leash;
        public Transform ownerTether;
        public ParticleSystem peeStream, grassKicks, bathWater, soap, splash;
        public PetDigPatch[] digging;
        public PetToy[] toys;
        public PetRecord Record { get; private set; }
        public PetMood Mood { get; private set; }
        public PetVisual Visual { get; private set; }
        public string Hint { get; private set; } = "";
        public PetBathRules Bath { get; } = new PetBathRules();
        public bool PeeLegObserved { get; private set; }
        public bool GrassKickObserved { get; private set; }
        public int PerformedTricksMask { get; private set; }
        public int NaturalPees { get; private set; }
        public float LeashDistance => owner == null ? 0 : Vector3.Distance(ownerTether.position, transform.position);
        public bool TrainingCue => Mood == PetMood.Training && elapsed >= .7f && elapsed <= 2.7f;
        public bool InteractionBusy => Mood == PetMood.Training || Mood == PetMood.Bathing;
        public bool Stayed => staying;
        MaterialPropertyBlock tint;
        Color[][] cleanColors;
        bool[][] paintSlots;
        Vector3[] originalVisualScales;
        float nextGrowth;
        Vector3 prior, activityStart, goal;
        float elapsed, nextPee, nextWander, nextDirty, nextArchive, actionPeak, comeTravel;
        bool staying, timed, cueAccepted, peeApproach;
        PetTrick trick;
        PetDigPatch patch;
        PetToy toy;
        Transform bathStation;
        UnityEngine.AI.NavMeshPath safePath;
        void Awake() { EnsureNativeResources(); }
        void EnsureNativeResources()
        {
            if(tint==null)tint=new MaterialPropertyBlock();
            if(safePath==null)safePath=new UnityEngine.AI.NavMeshPath();
        }
        readonly Vector3[] leashPoints=new Vector3[24];
        float nextReplan, stuckAge;
        Vector3 lastProgress;
        public void Begin(PetRecord record, Color color)
        {
            // Pet roots are authored inactive; Begin can precede their Awake.
            EnsureNativeResources();
            Cancel(); Record = record; Visual = Array.Find(visualTypes, v => v.kind == record.kind);
            if (originalVisualScales == null) { originalVisualScales = new Vector3[visualTypes.Length]; for (int i = 0; i < visualTypes.Length; i++) originalVisualScales[i] = visualTypes[i].root.transform.localScale; }
            for (int i = 0; i < visualTypes.Length; i++) visualTypes[i].root.transform.localScale = originalVisualScales[i];
            if (Visual == null || Visual.root == null) throw new InvalidOperationException("Selected pet type has no authored animated visual.");
            foreach (var v in visualTypes) v.root.SetActive(v == Visual);
            if (Visual.miniature != null) Visual.miniature.SelectBody(record.bodyId, color);
            Visual.root.GetComponent<PetAccessory>()?.Select(record.accessoryId); nextGrowth = 0;
            if (Visual.living == null && Visual.miniature == null) throw new InvalidOperationException("A living pet needs actual animation bindings.");
            if (Visual.peeAnchor == null || Visual.kickAnchor == null || peeStream == null || grassKicks == null) throw new InvalidOperationException("Every pet type needs an actual leg/effect and grass-kick binding.");
            CaptureDirtBindings();
            gameObject.SetActive(true); motor.enabled = true;
            var desired = owner.truck.body.position - owner.truck.transform.forward * 5 + owner.truck.transform.right * 4;
            if (!match.world.WalkingPoint(desired, out var safe) || !motor.Warp(safe)) throw new InvalidOperationException("The actual shared wilderness NavMesh cannot place the chosen pet.");
            motor.autoRepath = true; motor.isStopped = false; motor.speed = 7.5f; motor.acceleration = 12; motor.angularSpeed = 160;
            prior = transform.position; nextPee = Time.time + 35 + owner.slot * 7; nextWander = Time.time + 12 + owner.slot * 3; nextArchive = Time.unscaledTime + 60; nextDirty = 0; Mood = PetMood.Following;
            lastProgress=transform.position;stuckAge=0;nextReplan=0;
            PeeLegObserved = GrassKickObserved = false; PerformedTricksMask = NaturalPees = 0;
        }
        GameObject dirtModel;
        void CaptureDirtBindings()
        {
            EnsureNativeResources();
            if(Visual.miniature!=null)
            {
                dirtModel=Visual.miniature.CurrentModel;
                if(dirtModel==null)throw new InvalidOperationException("The selected miniature body has no current render model.");
                // Destroy(oldModel) is deferred. The complete assembly still
                // contains retired inactive renderers until end-of-frame.
                Visual.dirtSurfaces=dirtModel.GetComponentsInChildren<Renderer>(true);
            }
            else if(Visual.dirtSurfaces==null||System.Array.Exists(Visual.dirtSurfaces,r=>r==null))Visual.dirtSurfaces=Visual.root.GetComponentsInChildren<Renderer>(true);
            if(Visual.dirtSurfaces.Length==0)throw new InvalidOperationException("Current pet art has no actual dirt-presentation surfaces.");
            nextDirty=0;
            cleanColors = new Color[Visual.dirtSurfaces.Length][];
            paintSlots = new bool[cleanColors.Length][];
            for (int r = 0; r < cleanColors.Length; r++)
            {
                var materials = Visual.dirtSurfaces[r].sharedMaterials; cleanColors[r] = new Color[materials.Length]; paintSlots[r] = new bool[materials.Length];
                for (int i = 0; i < materials.Length; i++)
                {
                    var material = materials[i]; cleanColors[r][i] = material != null && material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : material != null && material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
                    paintSlots[r][i] = material != null && (Visual.miniature != null ? LWS.TruckSports.SportsTruckAppearanceCompatibility.IsPaintMaterial(Visual.miniature.ResolvedBodyId, material) : material.name.IndexOf("glass", StringComparison.OrdinalIgnoreCase) < 0 && material.name.IndexOf("eye", StringComparison.OrdinalIgnoreCase) < 0 && material.name.IndexOf("chrome", StringComparison.OrdinalIgnoreCase) < 0);
                    if (Visual.miniature != null && paintSlots[r][i]) { Visual.dirtSurfaces[r].GetPropertyBlock(tint, i); if (tint.HasColor("_BaseColor")) cleanColors[r][i] = tint.GetColor("_BaseColor"); }
                }
            }
        }
        void Pose(CampPose pose, float phase = 0, float strength = 1)
        {
            Visual.living?.Request(pose, phase, strength); Visual.miniature?.Request(pose, phase);
        }
        float ContextAngle => Visual.living != null ? Visual.living.ActualContextAngle : Visual.miniature.ActualContextAngle;
        float LegLift => Visual.living != null ? Visual.living.ActualLegLift : Visual.miniature.ActualLegLift * 100;
        float KickAngle => Visual.living != null ? Visual.living.ActualKickAngle : Visual.miniature.ActualKickAngle;
        void StopFx() { foreach (var fx in new[] { peeStream, grassKicks, bathWater, soap }) if (fx != null) fx.Stop(true, ParticleSystemStopBehavior.StopEmitting); }
        static void Effect(ParticleSystem fx, bool play) { if (fx == null) return; if (play && !fx.isPlaying) fx.Play(); else if (!play && fx.isPlaying) fx.Stop(); }
        public void Cancel()
        {
            staying = false; toy?.Release(owner == null ? -1 : owner.slot, false); toy = null;
            Mood = PetMood.Following; elapsed = 0; bathStation = null; patch = null; peeApproach = false; StopFx();
            if (motor != null && motor.enabled && motor.isOnNavMesh) { motor.isStopped = false; motor.ResetPath(); }
        }
        public void CallBack() { Cancel(); Mood = PetMood.Returning; Hint = "COMING!"; }
        public bool ToggleLeash()
        {
            if (Record.leashed)
            {
                var near = match.world.Nearest(transform.position);
                if (near == null || near.kind != CampPlaceKind.Meadow && near.kind != CampPlaceKind.DiggingGlade && near.kind != CampPlaceKind.RestClearing || Vector3.Distance(transform.position, near.activity.position) > 50) { Hint = "UNCLIP IN A MARKED MEADOW OR PLAY GLADE"; return false; }
            }
            Record.leashed = !Record.leashed; repository.Changed(); CallBack(); return true;
        }
        bool Go(Vector3 destination, float stop = 1.5f)
        {
            if (!match.world.WalkingPoint(destination, out var safe)) { Hint = "FINDING A SAFE PATH"; motor.isStopped = true; return false; }
            motor.isStopped = false; motor.stoppingDistance = stop;
            if ((motor.destination - safe).sqrMagnitude > 1 || Time.time>=nextReplan)
            {
                nextReplan=Time.time+2;
                bool complete=UnityEngine.AI.NavMesh.CalculatePath(transform.position,safe,UnityEngine.AI.NavMesh.AllAreas,safePath)&&safePath.status==UnityEngine.AI.NavMeshPathStatus.PathComplete;
                if(!complete)
                {
                    for(int i=0;i<8&&!complete;i++){var nearby=safe+Quaternion.Euler(0,i*45,0)*Vector3.forward*3;if(match.world.WalkingPoint(nearby,out var alternative)&&UnityEngine.AI.NavMesh.CalculatePath(transform.position,alternative,UnityEngine.AI.NavMesh.AllAreas,safePath)&&safePath.status==UnityEngine.AI.NavMeshPathStatus.PathComplete){safe=alternative;complete=true;}}
                }
                if(!complete){motor.isStopped=true;Hint="WAITING FOR A CONNECTED TRAIL · CALL FROM OPEN GROUND";return false;}motor.SetPath(safePath);
            }
            if((transform.position-lastProgress).sqrMagnitude>.2f){lastProgress=transform.position;stuckAge=0;}else if(motor.remainingDistance>stop+1){stuckAge+=Time.fixedDeltaTime;if(stuckAge>4){nextReplan=0;stuckAge=0;motor.ResetPath();Hint="TAKING ANOTHER SAFE PATH";}}
            return Vector3.ProjectOnPlane(transform.position - safe, Vector3.up).magnitude <= stop + .25f;
        }
        public void Tick(float dt, bool bot)
        {
            if (Record == null || !gameObject.activeInHierarchy) return;
            if (!motor.isOnNavMesh) { Hint = "PET NAVIGATION UNAVAILABLE · RETURN TO SETUP"; return; }
            float distance = Vector3.Distance(prior, transform.position); if (distance < 2) comeTravel += distance; prior = transform.position;
            elapsed += dt; bool moving = motor.velocity.magnitude > .15f;
            if(Visual.living!=null)Visual.living.LocomotionSpeed=motor.velocity.magnitude;
            var near = match.world.Nearest(transform.position); bool muddy = near != null && (near.kind == CampPlaceKind.Lake || near.kind == CampPlaceKind.DiggingGlade);
            Record.Tick(dt, moving, muddy, Mood == PetMood.Sleeping);
            if (Time.time >= nextGrowth) { int index = Array.IndexOf(visualTypes, Visual); Visual.root.transform.localScale = originalVisualScales[index] * (Record.kind == PetKind.ClothedHuman ? 1 : 1 + Record.Growth * .12f); nextGrowth = Time.time + 20; }
            if (Time.unscaledTime >= nextArchive) { repository.Changed(); nextArchive = Time.unscaledTime + 60; }
            if (Mood == PetMood.Peeing) TickPee(dt);
            else if (Mood == PetMood.Training) TickTraining(dt, bot);
            else if (Mood == PetMood.Bathing) TickBath(dt, bot);
            else if (Mood == PetMood.Playing) TickToy();
            else if (Mood == PetMood.Sleeping || Mood == PetMood.Resting || staying)
            { motor.isStopped = true; Pose(Mood == PetMood.Sleeping ? CampPose.Sleep : staying ? CampPose.Stay : CampPose.LieDown); }
            else
            {
                if (Time.time >= nextPee && Record.walked > 8 && RequestPee()) return;
                Vector3 follow = owner.truck.body.position - owner.truck.transform.forward * 5 + owner.truck.transform.right * 4;
                if (Mood == PetMood.Investigating || Mood == PetMood.Sniffing || Mood == PetMood.Looking)
                {
                    if (!Go(goal, .7f)) Pose(CampPose.Walk);
                    else { motor.isStopped = true; Pose(Mood == PetMood.Looking ? CampPose.Look : CampPose.Sniff); }
                    if (elapsed > 4 || Vector3.Distance(owner.truck.body.position, transform.position) > (Record.leashed ? 9 : 24)) { Mood = PetMood.Following; elapsed = 0; }
                }
                else
                {
                    bool arrived = Go(follow, 2.4f); Pose(arrived ? CampPose.Idle : Mood == PetMood.Returning ? CampPose.Come : CampPose.Walk);
                    if (Mood == PetMood.Returning && arrived) { Mood = PetMood.Following; Hint = "BACK TOGETHER"; Record.Affection(1); repository.Changed(); }
                    if (Time.time >= nextWander && Vector3.Distance(follow, transform.position) < (Record.leashed ? 5 : 15))
                    {
                        var offset = Quaternion.Euler(0, owner.slot * 71 + Time.time * 7, 0) * Vector3.forward * (Record.leashed ? 3 : 10);
                        if (match.world.WalkingPoint(transform.position + offset, out goal)) { Mood = PetMood.Investigating; elapsed = 0; }
                        nextWander = Time.time + 14 + Record.stubborn * 20;
                    }
                }
            }
            PresentDirt();
        }
        public bool RequestPee()
        {
            if (InteractionBusy || Mood == PetMood.Playing) return false;
            for (int i = 0; i < 12; i++)
            {
                var candidate = transform.position + Quaternion.Euler(0, i * 30, 0) * Vector3.forward * (i < 6 ? 5 : 9);
                if (!match.world.WalkingPoint(candidate, out var safe)) continue;
                if (!Physics.Raycast(safe + Vector3.up * 3, Vector3.down, out var hit, 6, ~0, QueryTriggerInteraction.Ignore) || !(hit.collider is TerrainCollider)) continue;
                goal = safe; Mood = PetMood.Peeing; peeApproach = true; elapsed = 0; PeeLegObserved = GrassKickObserved = false; Hint = "A LITTLE GRASS BREAK"; return true;
            }
            nextPee = Time.time + 10; return false;
        }
        void TickPee(float dt)
        {
            peeStream.transform.position = Visual.peeAnchor.position; grassKicks.transform.position = Visual.kickAnchor.position;
            if (peeApproach)
            {
                if (!Go(goal, .4f)) { Pose(CampPose.Walk); return; }
                peeApproach = false; elapsed = 0; motor.isStopped = true;
            }
            if (elapsed < .8f) Pose(CampPose.Look);
            else if (elapsed < 4.2f)
            {
                float lift = Mathf.Clamp01((elapsed - .8f) / .7f); Pose(CampPose.Pee, lift, lift);
                PeeLegObserved |= LegLift > 15;
                Effect(peeStream, elapsed > 1.7f && PeeLegObserved);
            }
            else if (elapsed < 6.2f)
            {
                Effect(peeStream, false); Pose(CampPose.GrassKick); GrassKickObserved |= KickAngle > 20; Effect(grassKicks, GrassKickObserved);
            }
            else
            {
                StopFx(); if (PeeLegObserved && GrassKickObserved) { Record.pees++; NaturalPees++; Record.Affection(1); repository.Changed(); }
                Pose(CampPose.Happy); Visual.miniature?.Honk(match.settings.sfx); Mood = PetMood.Returning; elapsed = 0; nextPee = Time.time + 90 + owner.slot * 9;
            }
        }
        public bool BeginTraining(PetTrick selected)
        {
            if (InteractionBusy || (int)selected < 0 || (int)selected > 6) return false;
            Cancel(); trick = selected; patch = null;
            if (selected == PetTrick.Dig)
            {
                float closest = 30;
                foreach (var dig in digging) { float d = Vector3.Distance(transform.position, dig.transform.position); if (!dig.Discovered && d < closest) { closest = d; patch = dig; } }
                if (patch == null) { Hint = "TRAIN DIG IN A MARKED SOFT-SOIL PATCH"; return false; }
            }
            Mood = PetMood.Training; elapsed = actionPeak = comeTravel = 0; activityStart = transform.position; cueAccepted = timed = false; owner.Begin(CampActivity.Resting); Hint = "WATCH FOR THE GENTLE TRAINING CUE"; return true;
        }
        void TickTraining(float dt, bool bot)
        {
            if (trick == PetTrick.Dig && patch != null && Vector3.Distance(transform.position, patch.transform.position) > 4)
            { Go(patch.transform.position, 2.5f); Pose(CampPose.Walk); elapsed = 0; return; }
            if (!cueAccepted && elapsed >= .4f && (match.actions[owner.slot] || bot && elapsed >= 1.4f)) { cueAccepted = true; timed = TrainingCue; }
            float phase = Mathf.Max(0, elapsed - 2.8f); bool done = false, performed = false;
            motor.isStopped = true;
            if (phase <= 0) { Pose(CampPose.Look); return; }
            if (trick == PetTrick.Come)
            { done = Go(owner.truck.body.position + owner.truck.transform.right * 4, 2); Pose(CampPose.Come); performed = done && comeTravel > 2; done |= phase >= 12; }
            else
            {
                var pose = trick == PetTrick.Sit ? CampPose.Sit : trick == PetTrick.LieDown ? CampPose.LieDown : trick == PetTrick.RollOver ? CampPose.RollOver : trick == PetTrick.Shake ? CampPose.Shake : trick == PetTrick.Dig ? CampPose.Dig : CampPose.Stay;
                Pose(pose, Mathf.Clamp01(phase / 3)); actionPeak = Mathf.Max(actionPeak, ContextAngle);
                if (trick == PetTrick.Dig && patch != null) patch.Dig(transform.position, Visual.miniature != null ? Visual.miniature.ActualKickAngle > 20 : ContextAngle > 15, dt);
                done = phase >= (trick == PetTrick.Dig ? 8 : 3.2f);
                performed = trick == PetTrick.Stay ? Vector3.Distance(activityStart, transform.position) < .25f : trick == PetTrick.RollOver ? actionPeak > 120 : trick == PetTrick.Dig ? patch != null && patch.Discovered : trick == PetTrick.Shake ? Visual.miniature != null ? LegLift > 15 : actionPeak > 30 : trick == PetTrick.LieDown && Visual.miniature != null ? Visual.miniature.ActualLowering > .18f : actionPeak > 12;
            }
            if (!done) return;
            bool learned = Record.Learn(trick, performed, timed);
            if (learned) { PerformedTricksMask |= 1 << (int)trick; Hint = "GOOD " + Record.name.ToUpperInvariant() + " · " + trick.ToString().ToUpperInvariant(); repository.Changed(); Visual.miniature?.Honk(match.settings.sfx); }
            else Hint = performed ? "NICE TRY · TAP ACTION DURING THE CUE NEXT TIME" : "LET YOUR PET SETTLE AND TRY AGAIN";
            staying = trick == PetTrick.Stay && performed; Mood = staying ? PetMood.Resting : PetMood.Following; elapsed = 0; owner.FinishActivity(Hint);
        }
        public bool BeginBath(Transform station)
        {
            if (station == null || InteractionBusy || Vector3.Distance(owner.truck.body.position, station.position) > 18) return false;
            Cancel(); bathStation = station; Mood = PetMood.Bathing; elapsed = 0; Bath.Begin(); owner.Begin(CampActivity.Resting); Hint = "TO THE WASH STATION"; return true;
        }
        void TickBath(float dt, bool bot)
        {
            if (!Go(bathStation.position, .5f)) { Pose(CampPose.Walk); return; }
            motor.isStopped = true; Pose(elapsed % 6 < 1.2f ? CampPose.BathShake : CampPose.Look);
            bool hold = bot || match.ActionHeld(owner.slot); float stroke = bot ? Mathf.Sin(elapsed * 7) : match.commands[owner.slot].x;
            bool ready = Bath.Stroke(stroke, hold, dt);
            Effect(bathWater, Bath.Step == PetBathStep.Water || Bath.Step == PetBathStep.Rinse); Effect(soap, Bath.Step == PetBathStep.Soap || Bath.Step == PetBathStep.Scrub);
            if (elapsed % 4 < dt && splash != null) splash.Play();
            Hint = Bath.Step + " · " + (ready ? "TAP ACTION TO CONTINUE" : Bath.Step == PetBathStep.Scrub ? "SWEEP LEFT / RIGHT" : "HOLD ACTION");
            if (ready && (match.actions[owner.slot] || bot)) Bath.Confirm(Record);
            if (!Bath.Complete) return;
            StopFx(); Pose(CampPose.Happy); repository.Changed(); Hint = "SPARKLING CLEAN!"; owner.FinishActivity(Hint); Mood = PetMood.Following; elapsed = 0;
        }
        public bool Play(PetToy selected)
        {
            if (selected == null || InteractionBusy || !selected.Throw(owner.truck.body.position, owner.truck.transform.forward)) return false;
            Cancel(); toy = selected; Mood = PetMood.Playing; elapsed = 0; return true;
        }
        void TickToy()
        {
            if (toy == null || elapsed > 30) { Cancel(); return; }
            if (!toy.Carried) { Go(toy.body.position, 1); Pose(CampPose.Walk); toy.Retrieve(physicalBody, owner.slot); }
            else if (Go(owner.truck.body.position + owner.truck.transform.right * 4, 2))
            { toy.Release(owner.slot, true); Record.Affection(5); Record.playful += elapsed; repository.Changed(); Visual.miniature?.Honk(match.settings.sfx); Hint = "A PROPER FETCH!"; toy = null; Mood = PetMood.Following; }
            else Pose(CampPose.Come);
        }
        public void Sleep() { Cancel(); Mood = PetMood.Sleeping; repository.Changed(); }
        public bool RestoreSafePose(Vector3 position, Quaternion rotation, bool asleep, out string reason)
        {
            reason = "";
            if (match.Live && !match.AtWorkshopStartBoundary || !match.world.WalkingPoint(position, out var safe) || Vector3.Distance(position, safe) > 1) { reason = "A stopped/fresh-start, valid shared-terrain pet pose is required."; return false; }
            if (!motor.Warp(safe)) { reason = "The saved pet pose could not be restored on the existing NavMesh."; return false; }
            transform.rotation = rotation; prior = transform.position; if (asleep) Sleep(); return true;
        }
        void PresentDirt()
        {
            if(cleanColors==null||Visual.dirtSurfaces==null||Visual.miniature!=null&&dirtModel!=Visual.miniature.CurrentModel||System.Array.Exists(Visual.dirtSurfaces,r=>r==null))CaptureDirtBindings();
            if (Time.time < nextDirty) return; nextDirty = Time.time + .2f;
            float clean = Record.cleanliness / 100;
            if (Mood == PetMood.Bathing && Bath.Step == PetBathStep.Rinse) clean = Mathf.Lerp(clean, 1, Bath.Progress);
            for (int r = 0; r < Visual.dirtSurfaces.Length; r++)
            {
                var renderer = Visual.dirtSurfaces[r];
                for (int i = 0; i < cleanColors[r].Length; i++)
                {
                    if (!paintSlots[r][i]) continue;
                    renderer.GetPropertyBlock(tint, i); var color = Color.Lerp(new Color(.19f, .11f, .055f), cleanColors[r][i], clean); tint.SetColor("_BaseColor", color); tint.SetColor("_Color", color); renderer.SetPropertyBlock(tint, i);
                }
            }
        }
        void LateUpdate()
        {
            if (leash == null || Record == null) return;
            leash.enabled = Record.leashed && gameObject.activeInHierarchy;
            if (!leash.enabled) return;
            Vector3 a=ownerTether.position,b=Visual.collar!=null?Visual.collar.position:transform.position+Vector3.up*.8f;int count=2;leashPoints[0]=a;leashPoints[1]=b;
            if(Physics.Linecast(a,b,1,QueryTriggerInteraction.Ignore)&&match.world.WalkingPoint(owner.truck.body.position,out var from)&&UnityEngine.AI.NavMesh.CalculatePath(from,transform.position,UnityEngine.AI.NavMesh.AllAreas,safePath)&&safePath.status==UnityEngine.AI.NavMeshPathStatus.PathComplete)
            {
                var corners=safePath.corners;count=0;leashPoints[count++]=a;for(int i=1;i<corners.Length-1&&count<leashPoints.Length-1;i++)leashPoints[count++]=corners[i]+Vector3.up;leashPoints[count++]=b;
            }
            float total=0;for(int i=1;i<count;i++)total+=Vector3.Distance(leashPoints[i-1],leashPoints[i]);
            for(int i=0;i<leash.positionCount;i++)
            {
                float remaining=total*i/(leash.positionCount-1f);int segment=1;while(segment<count-1&&remaining>Vector3.Distance(leashPoints[segment-1],leashPoints[segment])){remaining-=Vector3.Distance(leashPoints[segment-1],leashPoints[segment]);segment++;}
                float t=remaining/Mathf.Max(.001f,Vector3.Distance(leashPoints[segment-1],leashPoints[segment]));var p=Vector3.Lerp(leashPoints[segment-1],leashPoints[segment],t);p.y-=Mathf.Sin(Mathf.PI*t)*Mathf.Min(.7f,total*.05f);p.y=Mathf.Max(p.y,match.world.Height(p)+.12f);leash.SetPosition(i,p);
            }
        }
        public void Stop()
        {
            Cancel(); if (motor != null && motor.enabled && motor.isOnNavMesh) motor.isStopped = true; if (leash != null) leash.enabled = false;
        }
    }
}
