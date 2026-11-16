using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.TruckDragonKingdom
{
    // New, real project-owned lid geometry has this genuine mesh morph. No
    // original eye bone, Animator parameter or vendor channel is fabricated.
    [DefaultExecutionOrder(20)]
    public sealed class DragonEyelidPresentation : MonoBehaviour
    {
        public const string ClosureShape="LWS Visible Eyelid Closure";
        public DragonAnimalMotor motor;
        public Transform actualHead;
        public SkinnedMeshRenderer[] lids=Array.Empty<SkinnedMeshRenderer>();
        public SkinnedMeshRenderer[] creases=Array.Empty<SkinnedMeshRenderer>();
        public AnimationClip[] sleepingClips=Array.Empty<AnimationClip>();
        public AnimationClip[] sleepingPeekClips=Array.Empty<AnimationClip>();
        public float Closure { get; private set; }
        public bool EyesOpenForVision=>Closure<=.15f;
        public bool ObservedPeekPlayback { get; private set; }
        public float PeekNormalizedTime { get; private set; }
        public AnimationClip ObservedPeekClip { get; private set; }
        public bool ObservedSleepPlayback { get; private set; }
        public int ClosedFrames { get; private set; }
        readonly List<AnimatorClipInfo> clips=new List<AnimatorClipInfo>(8);
        bool restingEyes;
        DragonCharacter character;
        void Awake()=>character=GetComponentInParent<DragonCharacter>(true);
        public void RegisterSleepingClip(AnimationClip clip)
        {
            if(clip==null||Array.IndexOf(sleepingClips,clip)>=0)return;
            var replacement=new AnimationClip[sleepingClips.Length+1];Array.Copy(sleepingClips,replacement,sleepingClips.Length);replacement[replacement.Length-1]=clip;sleepingClips=replacement;
        }
        // Optional mode-local derived motion only. Ordinary catalog LookAround
        // and configured animals keep their original behavior by default.
        public void RegisterSleepingPeekClip(AnimationClip clip)
        {
            if(clip==null||Array.IndexOf(sleepingPeekClips,clip)>=0)return;
            RegisterSleepingClip(clip);var replacement=new AnimationClip[sleepingPeekClips.Length+1];Array.Copy(sleepingPeekClips,replacement,sleepingPeekClips.Length);replacement[replacement.Length-1]=clip;sleepingPeekClips=replacement;
        }
        // Character reset/Prepare may occur synchronously without a presentation
        // frame between them. Clear per-session observations and real morphs now;
        // retain the explicitly authored clip/geometry bindings for reuse.
        public void ResetPresentation()
        {
            Closure=0;ObservedSleepPlayback=ObservedPeekPlayback=false;ObservedPeekClip=null;PeekNormalizedTime=0;ClosedFrames=0;restingEyes=false;clips.Clear();Apply();
        }
        public void ValidateBindings()
        {
            if(motor==null||actualHead==null||actualHead.name!="RigHead"||lids.Length!=2||creases.Length!=2||sleepingClips.Length==0)throw new InvalidOperationException("Actual head, paired surface lids/creases and real sleeping clip references required.");
            foreach(var renderer in lids)ValidateMesh(renderer);
            foreach(var renderer in creases)ValidateMesh(renderer);
            foreach(var clip in sleepingPeekClips)if(clip==null||Array.IndexOf(sleepingClips,clip)<0)throw new InvalidOperationException("A real mode-local peek clip must also retain its sleeping posture binding.");
        }
        void ValidateMesh(SkinnedMeshRenderer renderer)
        {
            if(renderer==null||renderer.sharedMesh==null||renderer.sharedMesh.GetBlendShapeIndex(ClosureShape)<0||renderer.bones.Length!=1||renderer.bones[0]!=actualHead||renderer.sharedMaterial==null)throw new InvalidOperationException("Real authored head-attached closure geometry required.");
        }
        void LateUpdate()
        {
            // Actor reset does not disable the motor. Its outgoing Animator
            // blend must not revive a prior session's resting-eye latch. A
            // standalone configured Animal still uses its own motor lifecycle.
            if(character!=null&&!character.Prepared||motor==null||!motor.Prepared){ResetPresentation();return;}
            if(motor.Paused)return;
            float target=0;ObservedSleepPlayback=ObservedPeekPlayback=false;ObservedPeekClip=null;PeekNormalizedTime=0;
            if(motor.ActionPlaying&&motor.CurrentAction==DragonAnimationAction.Wake)
            {restingEyes=false;target=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.02f,.20f,motor.ActionNormalizedTime));}
            else if(motor.ActionPlaying&&motor.CurrentAction==DragonAnimationAction.Sleep)
            {
                restingEyes=true;ObservedSleepPlayback=true;
                target=Mathf.SmoothStep(0,1,Mathf.Clamp01(motor.ActionNormalizedTime/.08f));
            }
            else if(TryReadActualPeek(out var peek,out float phase))
            {
                restingEyes=true;ObservedSleepPlayback=ObservedPeekPlayback=true;ObservedPeekClip=peek;PeekNormalizedTime=phase;
                float opening=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.28f,.38f,phase));
                float closing=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.58f,.70f,phase));
                target=1-opening*(1-closing);
            }
            else if(HasWeightedSleepingClip())
            {
                // Includes the explicitly registered mode-local sleeping
                // vigilance override and the genuine outgoing sleep blend.
                restingEyes=true;ObservedSleepPlayback=true;target=1;
            }
            else if(sleepingPeekClips.Length>0&&restingEyes)
            {
                if(motor.ActionPlaying&&motor.CurrentAction!=DragonAnimationAction.LookAround)restingEyes=false;
                else target=1; // no open-eye flash between genuine rest modes
            }
            Closure=Mathf.MoveTowards(Closure,target,Time.deltaTime*8);
            if(Closure>.99f)ClosedFrames++;
            Apply();
        }
        bool HasWeightedSleepingClip()
        {
            if(motor.animator==null)return false;
            for(int layer=0;layer<motor.animator.layerCount;layer++)
            {
                if(layer>0&&motor.animator.GetLayerWeight(layer)<=.01f)continue;
                clips.Clear();motor.animator.GetCurrentAnimatorClipInfo(layer,clips);
                if(ContainsSleep(clips))return true;
                if(motor.animator.IsInTransition(layer)){clips.Clear();motor.animator.GetNextAnimatorClipInfo(layer,clips);if(ContainsSleep(clips))return true;}
            }
            return false;
        }
        bool ContainsSleep(List<AnimatorClipInfo> values)
        {foreach(var clip in values)if(clip.weight>.15f&&Array.IndexOf(sleepingClips,clip.clip)>=0)return true;return false;}
        bool TryReadActualPeek(out AnimationClip selected,out float phase)
        {
            selected=null;phase=0;if(sleepingPeekClips.Length==0||motor.animator==null)return false;
            for(int layer=0;layer<motor.animator.layerCount;layer++)
            {
                if(layer>0&&motor.animator.GetLayerWeight(layer)<=.15f)continue;
                var state=motor.animator.GetCurrentAnimatorStateInfo(layer);clips.Clear();motor.animator.GetCurrentAnimatorClipInfo(layer,clips);
                if(PeekInState(clips,state,out selected,out phase))return true;
                if(!motor.animator.IsInTransition(layer))continue;
                state=motor.animator.GetNextAnimatorStateInfo(layer);clips.Clear();motor.animator.GetNextAnimatorClipInfo(layer,clips);
                if(PeekInState(clips,state,out selected,out phase))return true;
            }
            return false;
        }
        bool PeekInState(List<AnimatorClipInfo> values,AnimatorStateInfo state,out AnimationClip selected,out float phase)
        {
            selected=null;phase=0;if(!state.IsTag("DragonAction_LookAround"))return false;
            foreach(var value in values)if(value.weight>.15f&&Array.IndexOf(sleepingPeekClips,value.clip)>=0){selected=value.clip;phase=Mathf.Clamp01(state.normalizedTime);return true;}
            return false;
        }
        void Apply()
        {
            foreach(var renderer in lids)if(renderer!=null){renderer.SetBlendShapeWeight(0,Closure*100);renderer.enabled=Closure>.001f;}
            float crease=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.85f,.99f,Closure));
            foreach(var renderer in creases)if(renderer!=null){renderer.SetBlendShapeWeight(0,crease*100);renderer.enabled=crease>.001f;}
        }
        void OnDisable()=>ResetPresentation();
    }
}
