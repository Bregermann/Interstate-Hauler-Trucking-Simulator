using System;
using System.Collections.Generic;
using System.Linq;
using LWS.TruckDragonKingdom;
using MalbersAnimations.Controller;
using MalbersAnimations.Scriptables;
using UnityEngine;
namespace LWS.TruckDragonRiders
{
    // A parent Awake can precede its child's Awake during hierarchy activation.
    // Observe Awake, then configure only in ordinary Start, after every native
    // Awake/OnEnable has completed. The vendor owns state, sprint and cadence.
    [DefaultExecutionOrder(-9)]
    public sealed class DragonRiderOwnedMotorScaleSpeed : MonoBehaviour
    {
        public DragonAnimalMotor motor;public DragonSpeciesCatalog catalog;public string speciesId="dragon-inferno";public float authoredMotorScale=5,configuredMotorScale=17;public State[] authoredStateSources=Array.Empty<State>();
        [Serializable]public sealed class SpeedProof{public string set,speed;public float originalPosition,ownedPosition,worldTarget,animationSpeed;public bool otherFieldsExact;}
        [Serializable]public sealed class StateIdentityProof{public int index,instanceId,authoredInstanceId;public string name,authoredName;public bool sameAuthoredReference,sameId;}
        public bool AwakeObserved {get;private set;}public bool NativeInitialized {get;private set;}public bool UnitAncestorsRetained {get;private set;}public bool RuntimeStateOwnership {get;private set;}public bool AuthoredStateSetsUntouched {get;private set;}public int AwakeFrame {get;private set;}public string Failure {get;private set;}
        public bool StartObserved {get;private set;}public int StartFrame {get;private set;}public StateIdentityProof[] AwakeStates {get;private set;}=Array.Empty<StateIdentityProof>();public StateIdentityProof[] StartStates {get;private set;}=Array.Empty<StateIdentityProof>();
        public SpeedProof[] Speeds {get;private set;}=Array.Empty<SpeedProof>();
        void Awake()
        {
            AwakeObserved=true;AwakeFrame=Time.frameCount;
            AwakeStates=CaptureStates();
        }
        StateIdentityProof[] CaptureStates()
        {
            if(motor==null||motor.animal==null||motor.animal.states==null)return Array.Empty<StateIdentityProof>();
            return motor.animal.states.Select((state,index)=>{var source=index<authoredStateSources.Length?authoredStateSources[index]:null;return new StateIdentityProof{index=index,instanceId=state==null?0:state.GetInstanceID(),authoredInstanceId=source==null?0:source.GetInstanceID(),name=state==null?null:state.name,authoredName=source==null?null:source.name,sameAuthoredReference=ReferenceEquals(state,source),sameId=state!=null&&source!=null&&state.ID==source.ID};}).ToArray();
        }
        void Start()
        {
            StartObserved=true;StartFrame=Time.frameCount;StartStates=CaptureStates();
            try
            {
                if(motor==null||catalog==null||!catalog.TryGet(speciesId,out var species)||motor.animal==null||Mathf.Abs(motor.animal.ScaleFactor-configuredMotorScale)>.00001f||authoredMotorScale<=0)throw new InvalidOperationException("Exact owned mounted motor/catalog/scale configuration required.");var animal=motor.animal;
                NativeInitialized=animal.Anim==motor.animator&&animal.CloneStates&&animal.states!=null&&animal.states.Count==authoredStateSources.Length&&animal.states.Count>0;
                UnitAncestorsRetained=true;for(var parent=motor.transform.parent;parent!=null;parent=parent.parent)UnitAncestorsRetained&=parent.localScale.Equals(Vector3.one);
                RuntimeStateOwnership=NativeInitialized&&animal.states.Select((state,index)=>state!=null&&authoredStateSources[index]!=null&&!ReferenceEquals(state,authoredStateSources[index])&&state.ID==authoredStateSources[index].ID).All(value=>value);
                if(!NativeInitialized||!UnitAncestorsRetained||!RuntimeStateOwnership)throw new InvalidOperationException("Actual vendor Awake/OnEnable must initialize its Animator, retain UNIT ancestors and clone all captured authored State sources BEFORE ordinary Start scale-speed adaptation.");
                var authoredSets=authoredStateSources.SelectMany(state=>state.SpeedSets).Distinct().ToArray();var authoredSnapshots=authoredSets.Select(JsonUtility.ToJson).ToArray();var sets=animal.speedSets.Concat(animal.states.SelectMany(state=>state.SpeedSets)).Distinct().Where(set=>set.name=="Dragon Ground"||set.name=="Dragon Fly").ToArray();if(!sets.Any(set=>set.name=="Dragon Ground")||!sets.Any(set=>set.name=="Dragon Fly")||sets.Any(set=>authoredSets.Any(source=>ReferenceEquals(source,set))))throw new InvalidOperationException("Native per-animal Ground/Fly sets must be independently owned, never persistent authored State sets.");var proof=new List<SpeedProof>();
                foreach(var set in sets)
                {
                    if(set.Speeds.Count!=2)throw new InvalidOperationException("Preserve unknown designer speed configurations; the qualified native two-speed set is required.");float world=set.name=="Dragon Fly"?species.flightSpeed:species.groundSpeed,ratio=set.name=="Dragon Fly"?1.28f:1.45f;
                    for(int i=0;i<set.Speeds.Count;i++)
                    {
                        var original=set.Speeds[i];float target=world*(i==0?1:ratio);if(original.position==null||!original.position.UseConstant||Mathf.Abs(original.position.Value*authoredMotorScale-target)>.0001f)throw new InvalidOperationException("Unknown authored native translation profile; never override a designer variable or nonqualified speed.");var owned=original;owned.position=new FloatReference(target/configuredMotorScale);var comparison=owned;comparison.position=original.position;bool unchanged=JsonUtility.ToJson(comparison)==JsonUtility.ToJson(original);if(!unchanged)throw new InvalidOperationException("Only the owned position reference may change; native cadence/rotation/sprint configuration must remain exact.");set.Speeds[i]=owned;proof.Add(new SpeedProof{set=set.name,speed=owned.name,originalPosition=original.position.Value,ownedPosition=owned.position.Value,worldTarget=target,animationSpeed=owned.animator.Value,otherFieldsExact=unchanged});
                    }
                }
                AuthoredStateSetsUntouched=authoredSets.Select(JsonUtility.ToJson).SequenceEqual(authoredSnapshots);if(!AuthoredStateSetsUntouched)throw new InvalidOperationException("Authored shared State speed settings changed.");Speeds=proof.ToArray();animal.Speed_Update_Current();
            }
            catch(Exception error){Failure=error.ToString();throw;}
        }
    }
}
