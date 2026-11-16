#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using LWS.TruckDragonKingdom;
using UnityEditor.Animations;
using UnityEngine;

namespace LWS.TruckDragonRiders.Editor
{
    public static partial class DragonRiderSaddleAuthoring
    {
        static List<MountedPoseTrial> ActualLayerBreathTrials(DragonCharacter actor,DragonSpeciesDefinition species)=>ActualLayerBreathTrialsCore(actor,species,false);
        static List<MountedPoseTrial> CompleteActualLayerBreathTrials(DragonCharacter actor,DragonSpeciesDefinition species)=>ActualLayerBreathTrialsCore(actor,species,true);
        static List<MountedPoseTrial> ActualLayerBreathTrialsCore(DragonCharacter actor,DragonSpeciesDefinition species,bool everyBreathFrame)
        {
            if(!species.TryGetAnimation(DragonAnimationAction.Breath,out var breath))throw new InvalidOperationException("Actual source Breath required.");
            var clip=EffectiveRiderClip(actor,breath.clip);var result=new List<MountedPoseTrial>();
            int count=Mathf.Max(2,Mathf.CeilToInt(breath.clip.length*breath.clip.frameRate));
            var frames=!everyBreathFrame&&species.id=="dragon-inferno"?new[]{1,2,8}:Enumerable.Range(0,count+1).ToArray();
            foreach(int frame in frames)result.Add(new MountedPoseTrial{name="Mandatory actual pure Breath frame "+frame+"/"+count,clip=clip,fraction=frame/(float)count});
            // These are source Flight inputs, not guessed clip weights. The real
            // controller evaluates its five-leaf tree underneath its action layer.
            foreach(var input in new[]{Vector2.zero,Vector2.up,Vector2.up*2,new Vector2(-1,1),new Vector2(1,1),new Vector2(-.25f,.75f)})
            foreach(float basePhase in new[]{0f,.45f,.98f})foreach(bool entry in new[]{true,false})
            foreach(float elapsed in new[]{0f,.03f,.06f,.09f,.1201f})
                result.Add(new MountedPoseTrial{name="Actual two-layer Flight H="+input.x+" V="+input.y+" basePhase="+basePhase+" Breath "+(entry?"entry":"natural-end exit")+" elapsedSeconds="+elapsed,clip=clip,fraction=basePhase,horizontal=input.x,vertical=input.y,actualConfiguredBreathLayer=true,breathLayerEntry=entry,transitionElapsed=elapsed});
            return result;
        }

        static void SampleActualBreathLayer(DragonCharacter actor,MountedPoseTrial trial)
        {
            var animator=actor.motor.animator;var runtime=animator.runtimeAnimatorController;
            while(runtime is AnimatorOverrideController layer)runtime=layer.runtimeAnimatorController;
            if(runtime is not AnimatorController controller||controller.layers.Length!=2)throw new InvalidOperationException("Exact original two-layer configured controller required.");
            var baseLayer=controller.layers[0];var actionLayer=controller.layers[1];
            var flight=ControllerStates(baseLayer.stateMachine).Single(state=>state.name=="Flight");
            var breath=ControllerStates(actionLayer.stateMachine).Single(state=>state.name=="Breath");
            var empty=ControllerStates(actionLayer.stateMachine).Single(state=>state.name=="No Action");
            var binding=actor.motor.actionBindings.Single(value=>value.action==DragonAnimationAction.Breath);
            int modeCode=Mathf.Abs(binding.mode.ID)*1000+binding.abilityIndex;
            var enter=actionLayer.stateMachine.anyStateTransitions.Single(value=>value.destinationState==breath);
            var exit=actionLayer.stateMachine.anyStateTransitions.Single(value=>value.destinationState==empty&&value.conditions.Any(condition=>condition.parameter=="Mode"&&condition.mode==AnimatorConditionMode.Equals&&condition.threshold==0));
            if(actionLayer.blendingMode!=AnimatorLayerBlendingMode.Override||actionLayer.avatarMask!=null||actionLayer.defaultWeight!=1||!enter.hasFixedDuration||!exit.hasFixedDuration||Mathf.Abs(enter.duration-.12f)>.00001f||Mathf.Abs(exit.duration-.12f)>.00001f||enter.hasExitTime||exit.hasExitTime||enter.canTransitionToSelf||exit.canTransitionToSelf||!enter.conditions.Any(value=>value.parameter=="Mode"&&value.mode==AnimatorConditionMode.Equals&&value.threshold==modeCode)||!enter.conditions.Any(value=>value.parameter=="ModeOn"&&value.mode==AnimatorConditionMode.If)||!exit.conditions.Any(value=>value.parameter=="ModeStatus"&&value.mode==AnimatorConditionMode.Equals&&value.threshold==0))
                throw new InvalidOperationException("Actual action-layer mask/override/parameter/fixed .12-second edges changed; do not substitute a single-layer crossfade.");
            bool enabled=animator.enabled,animalEnabled=actor.motor.animal.enabled;
            Transform[] bones=null;Vector3[] positions=null,scales=null;Quaternion[] rotations=null;
            try
            {
                // This stopped-preview graph measures animation only. No vendor
                // mode/root callback or second physics owner runs on the clone.
                actor.motor.animal.enabled=false;animator.enabled=true;animator.Rebind();
                animator.SetFloat("Horizontal",trial.horizontal);animator.SetFloat("Vertical",trial.vertical);
                animator.SetInteger("Mode",trial.breathLayerEntry?0:modeCode);animator.SetInteger("ModeStatus",trial.breathLayerEntry?0:1);
                animator.ResetTrigger("ModeOn");animator.ResetTrigger("StateOn");
                animator.Play(Animator.StringToHash(baseLayer.name+"."+flight.name),0,trial.fraction);
                animator.Play(Animator.StringToHash(actionLayer.name+"."+(trial.breathLayerEntry?empty.name:breath.name)),1,trial.breathLayerEntry?0:.98f);
                animator.Update(0);
                if(trial.breathLayerEntry){animator.SetInteger("Mode",modeCode);animator.SetInteger("ModeStatus",1);animator.SetTrigger("ModeOn");}
                else{animator.SetInteger("Mode",0);animator.SetInteger("ModeStatus",0);}
                animator.Update(0);
                float remaining=trial.transitionElapsed;while(remaining>0){float step=Mathf.Min(remaining,1f/120);animator.Update(step);remaining-=step;if(remaining<.0000001f)remaining=0;}
                var current=animator.GetCurrentAnimatorStateInfo(1);var next=animator.GetNextAnimatorStateInfo(1);bool transitioning=animator.IsInTransition(1);
                string target=actionLayer.name+"."+(trial.breathLayerEntry?breath.name:empty.name);
                if(!current.IsName(target)&&!(transitioning&&next.IsName(target)))throw new InvalidOperationException("Actual Mode parameter transition did not reach the configured target: "+trial.name);
                if(!animator.GetCurrentAnimatorStateInfo(0).IsName(baseLayer.name+"."+flight.name)||animator.IsInTransition(0))throw new InvalidOperationException("Actual base Flight stopped while the action layer blended.");
                string Clips(int layerIndex,bool incoming)=>string.Join("|",(incoming?animator.GetNextAnimatorClipInfo(layerIndex):animator.GetCurrentAnimatorClipInfo(layerIndex)).Select(value=>value.clip.name+"@"+value.weight.ToString("R",System.Globalization.CultureInfo.InvariantCulture)));
                trial.actualNativeLayerProof="REAL_UNCHANGED_TWO_LAYER_CONTROLLER Mode="+modeCode+" entry="+trial.breathLayerEntry+" elapsed="+trial.transitionElapsed+" fixedDuration="+(trial.breathLayerEntry?enter.duration:exit.duration)+" layerWeight="+animator.GetLayerWeight(1)+" currentHash="+current.fullPathHash+" currentTime="+current.normalizedTime+" inTransition="+transitioning+" nextHash="+next.fullPathHash+" nextTime="+next.normalizedTime+" transitionPhase="+(transitioning?animator.GetAnimatorTransitionInfo(1).normalizedTime:1)+" base="+Clips(0,false)+" actionCurrent="+Clips(1,false)+" actionIncoming="+Clips(1,true)+" EXIT_SEED_IS_EXACT_BREATH_PHASE_.98_NOT_GAMEPLAY_MODE_ACCEPTANCE";
                bones=actor.motor.GetComponentsInChildren<Transform>(true);positions=bones.Select(value=>value.localPosition).ToArray();rotations=bones.Select(value=>value.localRotation).ToArray();scales=bones.Select(value=>value.localScale).ToArray();
            }
            finally{animator.enabled=enabled;actor.motor.animal.enabled=animalEnabled;}
            for(int index=0;index<bones.Length;index++)if(!bones[index].localPosition.Equals(positions[index])||!bones[index].localRotation.Equals(rotations[index])||!bones[index].localScale.Equals(scales[index]))throw new InvalidOperationException("Actual layer sampler teardown changed native evaluated TRS: "+bones[index].name);
        }
    }
}
#endif
