#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using LWS.TruckDragonKingdom;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace LWS.TruckDragonRiders.Editor
{
    public static partial class DragonRiderSaddleAuthoring
    {
        public sealed class MountedPoseTrial
        {
            public string name;
            public AnimationClip clip, incoming;
            public float fraction, incomingFraction, incomingWeight;
            public bool actualConfiguredFlight,actualConfiguredBreathLayer,breathLayerEntry,actualConfiguredStartup;
            public float transitionElapsed;
            public string actualNativeLayerProof;
            public float horizontal,vertical;
        }
        static IEnumerable<AnimationClip> MotionClips(Motion motion)
        {
            if(motion is AnimationClip clip){yield return clip;yield break;}
            if(motion is BlendTree tree)foreach(var child in tree.children)foreach(var value in MotionClips(child.motion))yield return value;
        }
        static IEnumerable<AnimatorState> ControllerStates(AnimatorStateMachine machine)
        {
            foreach(var state in machine.states)yield return state.state;
            foreach(var nested in machine.stateMachines)foreach(var state in ControllerStates(nested.stateMachine))yield return state;
        }
        public static List<MountedPoseTrial> MountedAnimationTrials(DragonCharacter actor,DragonSpeciesDefinition species)
        {
            var runtime=actor.motor.animator.runtimeAnimatorController;
            while(runtime is AnimatorOverrideController overlay)runtime=overlay.runtimeAnimatorController;
            if(runtime is not AnimatorController controller||controller.layers.Length<2)throw new InvalidOperationException("Actual compatible mounted base/action controller required for full pose coverage.");
            var states=ControllerStates(controller.layers[0].stateMachine).ToArray();
            var baseClips=states.SelectMany(state=>MotionClips(state.motion)).Distinct().ToArray();
            if(!species.TryGetAnimation(DragonAnimationAction.Breath,out var breath))throw new InvalidOperationException("Mounted Breath attack mapping required for contact-preserving coverage.");
            var selected=baseClips.Concat(new[]{breath.clip}).Distinct().ToArray();
            var trials=new List<MountedPoseTrial>{new MountedPoseTrial{name=species.id=="dragon-fire"?"Authored carrier-hover staged default; no runtime playback claim":"Original configured staged pose"}};
            foreach(var clip in selected)
            {
                string name=string.Join("/",species.animations.Where(value=>value!=null&&value.clip==clip).Select(value=>value.action.ToString()));
                if(string.IsNullOrEmpty(name))throw new InvalidOperationException("Actual mounted controller uses an unaccounted source clip: "+clip.name);
                foreach(float phase in new[]{0f,.25f,.45f,.75f,.98f})trials.Add(new MountedPoseTrial{name=name+" phase="+phase,clip=EffectiveRiderClip(actor,clip),fraction=phase});
            }
            var pairs=new HashSet<(AnimationClip outgoing,AnimationClip incoming)>();
            // Blend-tree children and direct state transitions are the actual
            // configured Unity animation graph, not a guessed Idle/Fly list.
            foreach(var state in states)
            {
                var children=MotionClips(state.motion).Distinct().ToArray();
                for(int a=0;a<children.Length;a++)for(int b=a+1;b<children.Length;b++)pairs.Add((children[a],children[b]));
                foreach(var transition in state.transitions)
                    if(transition.destinationState!=null)foreach(var source in children)foreach(var target in MotionClips(transition.destinationState.motion))if(source!=target)pairs.Add((source,target));
            }
            // Vendor AnyState entries can switch the base state. Cover their
            // real clips in both directions, including launch/land/fall; the
            // mixer below uses Unity's real native blend evaluation.
            var entered=controller.layers[0].stateMachine.anyStateTransitions.Where(value=>value.destinationState!=null).SelectMany(value=>MotionClips(value.destinationState.motion)).Distinct().ToArray();
            foreach(var source in baseClips)foreach(var target in entered)if(source!=target)pairs.Add((source,target));
            // Mode layer is full-body at weight1. At entry/exit it genuinely
            // blends with whatever ordinary mounted base pose is evaluated.
            foreach(var source in baseClips){pairs.Add((source,breath.clip));pairs.Add((breath.clip,source));}
            foreach(var pair in pairs)
                foreach(float weight in new[]{.25f,.5f,.75f})
                    trials.Add(new MountedPoseTrial{name="Actual graph blend "+pair.outgoing.name+" -> "+pair.incoming.name+" weight="+weight,
                        clip=EffectiveRiderClip(actor,pair.outgoing),incoming=EffectiveRiderClip(actor,pair.incoming),fraction=.45f,incomingFraction=.05f,incomingWeight=weight});
            if(species.id=="dragon-fire")
            {
                trials.Add(ActualV203FireFlightTrial());
                foreach(float phase in new[]{0f,.25f,.45f,.75f,.98f}){var measured=ActualV203FireFlightTrial();measured.fraction=phase;measured.name="Actual configured three-leaf Flight weight replay; phase coverage="+phase;trials.Add(measured);}
            }
            return trials;
        }
        static MountedPoseTrial ActualV203FireFlightTrial()=>new MountedPoseTrial
        {
            name="Actual V203 native Fire Flight three-leaf state phase=.535184741 Horizontal=-.0221373457 Vertical=.4991438",
            actualConfiguredFlight=true,fraction=.535184741f,horizontal=-.0221373457f,vertical=.4991438f
        };
        static void SampleActualConfiguredStartup(DragonCharacter actor,MountedPoseTrial trial)
        {
            var animator=actor.motor.animator;var animal=actor.motor.animal;var body=actor.motor.body;bool oldEnabled=animator.enabled,oldPaused=actor.Paused,oldTimeline=animal.InTimeline;
            var root=actor.motor.transform;var beforePosition=root.position;var beforeRotation=root.rotation;var beforeScale=root.localScale;var beforeBodyPosition=body.position;var beforeBodyRotation=body.rotation;
            Transform[] evaluated=null;Vector3[] positions=null,scales=null;Quaternion[] rotations=null;
            try
            {
                // Use the same supported zero-duration configured graph path
                // as actual public Prepare. Direct SampleAnimation omits some
                // original constant-scale curves (actual V351 witness).
                actor.Pause(true);if(!oldTimeline)animal.InTimeline=true;animator.enabled=true;animator.Rebind();animator.Update(0);
                var clips=animator.GetCurrentAnimatorClipInfo(0).Where(info=>info.weight>.00001f).ToArray();if(clips.Length!=1||clips[0].clip!=trial.clip||Mathf.Abs(clips[0].weight-1)>.000001f||animator.GetCurrentAnimatorStateInfo(0).normalizedTime!=0||animator.IsInTransition(0))throw new InvalidOperationException("Actual configured default graph must evaluate the original proven sole source clip at0/weight1; no forced state or substituted pose.");
                for(int layer=1;layer<animator.layerCount;layer++)if(animator.GetCurrentAnimatorClipInfo(layer).Any(info=>info.weight>.00001f))throw new InvalidOperationException("Actual default source may not contain an unobserved action-layer pose.");
                evaluated=root.GetComponentsInChildren<Transform>(true);positions=evaluated.Select(bone=>bone.localPosition).ToArray();rotations=evaluated.Select(bone=>bone.localRotation).ToArray();scales=evaluated.Select(bone=>bone.localScale).ToArray();trial.actualNativeLayerProof="GENUINE_CONFIGURED_DEFAULT_CONTROLLER_ZERO_REBIND_TIMELINE_PUBLIC_PAUSE_SOLE_ORIGINAL_SOURCE_NO_BONE_WRITES";
            }
            finally
            {
                if(!oldTimeline){animal.InTimeline=false;animal.UpdateInputSource(false);}actor.Pause(oldPaused);animator.enabled=oldEnabled;
            }
            if(!root.position.Equals(beforePosition)||!root.rotation.Equals(beforeRotation)||!root.localScale.Equals(beforeScale)||!body.position.Equals(beforeBodyPosition)||!body.rotation.Equals(beforeBodyRotation))throw new InvalidOperationException("Read-only configured default graph changed the original root/body pose or scale; no restoration/teleport waiver.");
            for(int index=0;index<evaluated.Length;index++)if(!evaluated[index].localPosition.Equals(positions[index])||!evaluated[index].localRotation.Equals(rotations[index])||!evaluated[index].localScale.Equals(scales[index]))throw new InvalidOperationException("Configured default graph teardown changed evaluated skeletal/attachment TRS: "+evaluated[index].name+"; no pose reapplication permitted.");
        }
        static void SampleActualConfiguredFlight(DragonCharacter actor,MountedPoseTrial trial)
        {
            var animator=actor.motor.animator;bool enabled=animator.enabled;
            var runtime=animator.runtimeAnimatorController;while(runtime is AnimatorOverrideController overlay)runtime=overlay.runtimeAnimatorController;
            if(runtime is not AnimatorController controller)throw new InvalidOperationException("Actual configured controller required for observed three-way state replay.");
            var state=controller.layers[0].stateMachine.states.Select(value=>value.state).Single(value=>value.name=="Flight");
            if(state.motion is not BlendTree tree||tree.children.Length!=5||tree.children.Any(child=>child.timeScale!=1||child.cycleOffset!=0||child.mirror)||state.cycleOffset!=0||state.speed!=1||state.timeParameterActive)
                throw new InvalidOperationException("Observed V203 replay configuration changed; record actual native leaf times/offsets before substituting a guessed mixer.");
            animator.enabled=true;
            var hover=EffectiveRiderClip(actor,(AnimationClip)tree.children.Single(child=>child.position==Vector2.zero).motion);
            var forward=EffectiveRiderClip(actor,(AnimationClip)tree.children.Single(child=>child.position==Vector2.up).motion);
            var bank=EffectiveRiderClip(actor,(AnimationClip)tree.children.Single(child=>child.position==new Vector2(-1,1)).motion);
            Vector3 Evaluate(float horizontal,float vertical)
            {
                animator.Rebind();animator.SetFloat("Horizontal",horizontal);animator.SetFloat("Vertical",vertical);
                animator.Play(Animator.StringToHash(controller.layers[0].name+"."+state.name),0,trial.fraction);animator.Update(0);
                var evaluated=animator.GetCurrentAnimatorClipInfo(0);
                return new Vector3(evaluated.Where(value=>value.clip==hover).Sum(value=>value.weight),evaluated.Where(value=>value.clip==forward).Sum(value=>value.weight),evaluated.Where(value=>value.clip==bank).Sum(value=>value.weight));
            }
            // V208 proves GetFloat read after motor input writes is not the
            // input consumed by the already-evaluated V203 animation frame.
            // Fit two input coordinates through the REAL configured Mecanim
            // controller, retaining all three measured leaf-weight gates.
            // This is a read-only diagnostic/authoring replay, never game AI.
            float h=trial.horizontal,v=trial.vertical;Vector3 target=new Vector3(.47732f,.48017f,.04251f),weights=default;
            for(int iteration=0;iteration<10;iteration++)
            {
                weights=Evaluate(h,v);if((weights-target).sqrMagnitude<.000000001f)break;
                const float epsilon=.0005f;var dh=(Evaluate(h+epsilon,v)-weights)/epsilon;var dv=(Evaluate(h,v+epsilon)-weights)/epsilon;
                float determinant=dh.x*dv.z-dv.x*dh.z;if(Mathf.Abs(determinant)<.000001f)throw new InvalidOperationException("Actual native three-leaf parameter Jacobian is singular; no guessed mixer fallback.");
                var error=target-weights;h=Mathf.Clamp(h+Mathf.Clamp((error.x*dv.z-dv.x*error.z)/determinant,-.25f,.25f),-1,1);v=Mathf.Clamp(v+Mathf.Clamp((dh.x*error.z-error.x*dh.z)/determinant,-.25f,.25f),0,2);
            }
            weights=Evaluate(h,v);if(Mathf.Abs(weights.x-target.x)>.0001f||Mathf.Abs(weights.y-target.y)>.0001f||Mathf.Abs(weights.z-target.z)>.0001f)throw new InvalidOperationException("Native configured-controller inverse replay cannot reproduce all three actual V203 weights; no tolerance relaxation.");
            var info=animator.GetCurrentAnimatorStateInfo(0);var clips=animator.GetCurrentAnimatorClipInfo(0);
            if(!info.IsName(controller.layers[0].name+"."+state.name)||animator.IsInTransition(0)||Mathf.Abs(info.normalizedTime-trial.fraction)>.0001f||clips.Count(value=>value.weight>.00001f)!=3)
                throw new InvalidOperationException("Actual configured native replay did not evaluate the observed three-leaf Flight state.");
            var transforms=actor.motor.GetComponentsInChildren<Transform>(true);var positions=transforms.Select(value=>value.localPosition).ToArray();var rotations=transforms.Select(value=>value.localRotation).ToArray();var scales=transforms.Select(value=>value.localScale).ToArray();
            animator.enabled=enabled;
            for(int index=0;index<transforms.Length;index++)if(transforms[index].localPosition!=positions[index]||transforms[index].localRotation!=rotations[index]||transforms[index].localScale!=scales[index])throw new InvalidOperationException("Disabling replay Animator changed actual evaluated pose before independent skin measurement.");
        }
        public static void SampleMountedTrial(DragonCharacter actor,MountedPoseTrial trial)
        {
            if(trial.actualConfiguredStartup){SampleActualConfiguredStartup(actor,trial);return;}
            if(trial.actualConfiguredBreathLayer){SampleActualBreathLayer(actor,trial);return;}
            if(trial.actualConfiguredFlight){SampleActualConfiguredFlight(actor,trial);return;}
            if(trial.clip==null)return;
            if(trial.incoming==null){trial.clip.SampleAnimation(actor.motor.gameObject,trial.clip.length*trial.fraction);return;}
            var animator=actor.motor.animator;bool enabled=animator.enabled;
            var graph=PlayableGraph.Create("Read-only Riders native animation-graph footprint sampling");
            Transform[] sampledBones=null;Vector3[] sampledPositions=null,sampledScales=null;Quaternion[] sampledRotations=null;
            try
            {
                graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                var mix=AnimationMixerPlayable.Create(graph,2);
                var outgoing=AnimationClipPlayable.Create(graph,trial.clip);outgoing.SetApplyFootIK(false);outgoing.SetApplyPlayableIK(false);outgoing.SetTime(trial.clip.length*trial.fraction);
                var incoming=AnimationClipPlayable.Create(graph,trial.incoming);incoming.SetApplyFootIK(false);incoming.SetApplyPlayableIK(false);incoming.SetTime(trial.incoming.length*trial.incomingFraction);
                graph.Connect(outgoing,0,mix,0);graph.Connect(incoming,0,mix,1);mix.SetInputWeight(0,1-trial.incomingWeight);mix.SetInputWeight(1,trial.incomingWeight);
                var output=AnimationPlayableOutput.Create(graph,"Actual original rig",animator);output.SetSourcePlayable(mix);
                animator.enabled=true;graph.Play();graph.Evaluate(0);
                sampledBones=actor.motor.GetComponentsInChildren<Transform>(true);
                sampledPositions=sampledBones.Select(bone=>bone.localPosition).ToArray();sampledRotations=sampledBones.Select(bone=>bone.localRotation).ToArray();sampledScales=sampledBones.Select(bone=>bone.localScale).ToArray();
            }
            finally{graph.Destroy();animator.enabled=enabled;}
            // A temporary native graph must not silently restore the controller
            // or bind pose before the caller's actual skin/physics oracle. Do
            // not reconstruct or reapply a sampled pose to hide a reset.
            for(int index=0;index<sampledBones.Length;index++)
                if(!sampledBones[index].localPosition.Equals(sampledPositions[index])||!sampledBones[index].localRotation.Equals(sampledRotations[index])||!sampledBones[index].localScale.Equals(sampledScales[index]))
                    throw new InvalidOperationException("Native mounted animation mixer teardown changed evaluated bone TRS before actual skin measurement: "+trial.name+" / "+sampledBones[index].name+". A scoped live graph measurement is required; no pose reapplication is permitted.");
        }
    }
}
#endif
