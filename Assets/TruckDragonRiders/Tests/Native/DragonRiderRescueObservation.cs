#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

// Native test-only read-only witness. It never supplies input, poses, velocities,
// animation callbacks, gate progress, physics outcomes or gameplay clock values.
public sealed class DragonRiderRescueObservation : IDisposable
{
    const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    readonly Component match,racer,dragon,motor;
    readonly Rigidbody dragonBody,truckBody;
    readonly Collider shape;
    readonly Transform grip;
    readonly Animator animator;
    readonly string output;
    readonly float started=Time.realtimeSinceStartup;
    readonly List<Frame> frames=new List<Frame>();
    readonly List<EventBinding> bindings=new List<EventBinding>();
    readonly HashSet<string> capturedPhases=new HashSet<string>();
    float nextSample,nextCapture;
    bool disposed;
    [Serializable] sealed class Report
    {public string scope="READ_ONLY_ACTUAL_SAVED_RIDERS_DYNAMIC_IMPACT_RESCUE_NATIVE_WITNESS_NOT_WINDOWS_HARDWARE_OR_HUMAN_ACCEPTANCE";public List<Frame> frames;}
    [Serializable] sealed class Frame
    {
        public string stage,phase,action,activeState,mode,moveInput,evidencePng,captureError;
        public float realSeconds,clock,actionTime,lastActionTime,detachedSeconds,rescueTravel,blockedRescueTime,gripDistance,relativeSpeed,bodyGoalDistance,aboveSupport,dragonHorizontalSpeed,truckHorizontalSpeed,stamina,canopyOpening;
        public bool actionPlaying,lastActionCompleted,flying,grounded,mounted,grabRequested,grabContact,carryObserved,clearGripPath,paused;
        public int successfulRescues,failedRescues;
        public Vector3 dragonPosition,truckPosition,truckCenter,gripPosition,dragonVelocity,truckVelocity,desiredBody,gripOffset,physicalContactTarget;
        public Hit[] gripRayHits;public Layer[] layers;
    }
    [Serializable] sealed class Hit {public string path;public float distance;public bool actor,truck;}
    [Serializable] sealed class Layer {public int layer;public string currentTag,currentState,nextTag,nextState,currentClips,nextClips;public bool transitioning;public float weight,currentTime,nextTime;}
    sealed class EventBinding
    {
        public EventInfo info;public Delegate handler;public object source;
        readonly DragonRiderRescueObservation owner;readonly string name;
        public EventBinding(DragonRiderRescueObservation owner,object source,string name)
        {
            this.owner=owner;this.name=name;this.source=source;info=source.GetType().GetEvent(name);var type=info.EventHandlerType.GenericTypeArguments.Single();
            handler=Delegate.CreateDelegate(info.EventHandlerType,this,GetType().GetMethod(nameof(Receive),Any).MakeGenericMethod(type));info.AddEventHandler(source,handler);
        }
        void Receive<T>(T action)=>owner.Sample(name+":"+action,true,name=="ActionContact"&&action.ToString()=="Grab");
        public void Stop()=>info.RemoveEventHandler(source,handler);
    }
    public DragonRiderRescueObservation(Component match,object racer)
    {
        this.match=match;this.racer=(Component)racer;dragon=(Component)Get(racer,"Dragon");motor=(Component)Get(dragon,"motor");dragonBody=(Rigidbody)Get(motor,"body");
        var truck=(Component)Get(racer,"truck");truckBody=(Rigidbody)Get(truck,"body");shape=(Collider)Get(truck,"shape");grip=(Transform)Get(dragon,"leftGrip");animator=(Animator)Get(motor,"animator");
        var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");Assert.That(at>=0&&at+1<args.Length,Is.True,"Explicit output required for actual rescue diagnosis.");
        output=Path.Combine(args[at+1],"RidersActualRescue",DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff"));Directory.CreateDirectory(output);
        foreach(string name in new[]{"ActionStarted","ActionContact","ActionFinished"})bindings.Add(new EventBinding(this,motor,name));
    }
    public void Sample(string stage,bool force=false,bool capture=false)
    {
        if(disposed||!force&&Time.realtimeSinceStartup<nextSample)return;nextSample=Time.realtimeSinceStartup+1;
        Vector3 delta=truckBody.worldCenterOfMass-grip.position;var rays=Physics.RaycastAll(grip.position,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore).OrderBy(value=>value.distance).ToArray();
        var animal=Get(motor,"animal");var state=Get(animal,"ActiveState");var activeMode=Get(animal,"ActiveMode");
        var frame=new Frame{stage=stage,phase=Get(racer,"Phase").ToString(),realSeconds=Time.realtimeSinceStartup-started,clock=(float)Get(match,"Clock"),
            action=Get(motor,"CurrentAction").ToString(),actionPlaying=(bool)Get(motor,"ActionPlaying"),actionTime=(float)Get(motor,"ActionNormalizedTime"),lastActionTime=(float)Get(motor,"LastActionNormalizedTime"),lastActionCompleted=(bool)Get(motor,"LastActionCompleted"),
            flying=(bool)Get(motor,"IsFlying"),grounded=(bool)Get(motor,"IsGrounded"),paused=(bool)Get(motor,"Paused"),mounted=(bool)Get(racer,"PhysicallyMounted"),activeState=state==null?"none":state.GetType().Name,mode=activeMode==null?"none":Text(activeMode,"PlayingMode")+"/"+Text(activeMode,"ModeTime"),moveInput=Text(animal,"MovementAxis")+" smooth="+Text(animal,"MovementAxisSmoothed"),
            detachedSeconds=(float)Get(match,"Clock")-(float)Get(racer,"detachedAt"),rescueTravel=(float)Get(racer,"rescueTravel"),blockedRescueTime=(float)Get(racer,"blockedRescueTime"),grabRequested=(bool)Get(racer,"grabRequested"),grabContact=(bool)Get(racer,"grabContact"),carryObserved=(bool)Get(racer,"carryObserved"),
            successfulRescues=(int)Get(racer,"SuccessfulRescues"),failedRescues=(int)Get(racer,"FailedRescues"),dragonPosition=dragonBody.position,truckPosition=truckBody.position,truckCenter=truckBody.worldCenterOfMass,gripPosition=grip.position,dragonVelocity=dragonBody.linearVelocity,truckVelocity=truckBody.linearVelocity,
            gripDistance=Vector3.Distance(grip.position,shape.ClosestPoint(grip.position)),relativeSpeed=(dragonBody.linearVelocity-truckBody.linearVelocity).magnitude,
            gripOffset=grip.position-dragonBody.position,clearGripPath=(bool)racer.GetType().GetMethod("ClearGripPath",Any).Invoke(racer,new object[]{grip.position}),
            gripRayHits=rays.Take(12).Select(value=>new Hit{path=PathOf(value.transform),distance=value.distance,actor=value.transform.IsChildOf(dragon.transform),truck=value.transform.IsChildOf(truckBody.transform)}).ToArray(),layers=ReadLayers()};
        frame.physicalContactTarget=shape.ClosestPoint(grip.position);if((frame.physicalContactTarget-grip.position).sqrMagnitude>.01f)frame.physicalContactTarget+=Vector3.ClampMagnitude(truckBody.worldCenterOfMass-frame.physicalContactTarget,.8f);
        frame.desiredBody=frame.physicalContactTarget-frame.gripOffset;frame.bodyGoalDistance=Vector3.Distance(frame.desiredBody,dragonBody.position);
        frame.dragonHorizontalSpeed=Vector3.ProjectOnPlane(dragonBody.linearVelocity,Vector3.up).magnitude;frame.truckHorizontalSpeed=Vector3.ProjectOnPlane(truckBody.linearVelocity,Vector3.up).magnitude;frame.stamina=(float)Get(Get(racer,"Budget"),"Stamina");
        frame.canopyOpening=(bool)Get(racer,"ChuteOpen")?Mathf.SmoothStep(0,1,Mathf.Clamp01((frame.clock-(float)Get(racer,"chuteOpenedAt"))/1.15f)):0;
        frame.aboveSupport=-1;foreach(var hit in Physics.RaycastAll(truckBody.worldCenterOfMass+Vector3.up*2,Vector3.down,4000,~0,QueryTriggerInteraction.Ignore).OrderBy(value=>value.distance))if(!hit.transform.IsChildOf(dragon.transform)&&!hit.transform.IsChildOf(truckBody.transform)){frame.aboveSupport=hit.distance-2;break;}
        if(frame.phase!="Mounted"&&frame.phase!="Staged"&&capturedPhases.Add(frame.phase))capture=true;
        if(Time.realtimeSinceStartup>=nextCapture&&frame.phase!="Mounted"){capture=true;nextCapture=Time.realtimeSinceStartup+10;}
        if(capture)
        {
            frame.evidencePng=frames.Count.ToString("D4")+"-"+frame.phase+".png";
            try{var type=Type.GetType("LWS.TruckSports.SportsControlledCapture, Assembly-CSharp",true);type.GetMethods().Single(value=>value.Name=="Save"&&value.GetParameters().Length==5).Invoke(null,new object[]{match,1280,720,Path.Combine(output,frame.evidencePng),null});}
            catch(Exception error){frame.captureError=error.ToString();}
        }
        frames.Add(frame);File.WriteAllText(Path.Combine(output,"observations.json"),JsonUtility.ToJson(new Report{frames=frames},true));
        if(force||capture)TestContext.WriteLine("ACTUAL_RESCUE "+stage+" phase="+frame.phase+" clock="+frame.clock.ToString("F2")+" gripDistance="+frame.gripDistance.ToString("F3")+" relativeSpeed="+frame.relativeSpeed.ToString("F3")+" goal="+frame.bodyGoalDistance.ToString("F2")+" action="+frame.action+"/"+frame.actionTime.ToString("F3")+" grab="+frame.grabRequested+" clear="+frame.clearGripPath+" output="+output);
    }
    Layer[] ReadLayers()
    {
        var result=new Layer[animator.layerCount];for(int layer=0;layer<result.Length;layer++)
        {var current=animator.GetCurrentAnimatorStateInfo(layer);var next=animator.GetNextAnimatorStateInfo(layer);result[layer]=new Layer{layer=layer,weight=animator.GetLayerWeight(layer),transitioning=animator.IsInTransition(layer),currentTime=current.normalizedTime,nextTime=next.normalizedTime,currentState=current.fullPathHash.ToString(),nextState=next.fullPathHash.ToString(),currentTag=current.tagHash.ToString(),nextTag=next.tagHash.ToString(),currentClips=Clips(animator.GetCurrentAnimatorClipInfo(layer)),nextClips=Clips(animator.GetNextAnimatorClipInfo(layer))};}return result;
    }
    static string Clips(AnimatorClipInfo[] clips)=>string.Join(";",clips.Select(value=>(value.clip==null?"null":value.clip.name)+"@"+value.weight.ToString("F5")));
    static object Get(object source,string name){var field=source.GetType().GetField(name,Any);return field!=null?field.GetValue(source):source.GetType().GetProperty(name,Any).GetValue(source);}
    static string Text(object source,string name){var field=source.GetType().GetField(name,Any);if(field!=null)return field.GetValue(source)?.ToString()??"null";var property=source.GetType().GetProperty(name,Any);return property!=null?property.GetValue(source)?.ToString()??"null":"unavailable";}
    static string PathOf(Transform value)=>value.parent==null?value.name:PathOf(value.parent)+"/"+value.name;
    public void Dispose(){if(disposed)return;Sample("OBSERVATION_END",true);disposed=true;foreach(var binding in bindings)binding.Stop();bindings.Clear();}
}
#endif
