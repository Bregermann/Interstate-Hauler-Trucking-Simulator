#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using LWS.TruckMoonMadness;

// Test-only observation of ORIGINAL natural physics. No state/force/contact writes.
[DefaultExecutionOrder(32000)]
public sealed class MoonFlagEnergyDiagnosticObserver : MonoBehaviour
{
    [Serializable] public sealed class JointSample
    {
        public int id, connectedId;
        public string connectedName;
        public Vector3 anchor, connectedAnchor, worldAnchor, worldConnectedAnchor, currentForce, currentTorque;
        public float spring, damper, minDistance, maxDistance, tolerance, breakForce, breakTorque, massScale, connectedMassScale, connectedMass;
    }
    [Serializable] public sealed class Sample
    {
        public string phase, otherCollider, otherBody;
        public float clock, fixedTime, speed, mass, heldSeconds, breakCallbackForce, minContactSeparation, otherMass;
        public int frame, owner, jointId, jointCount, collections, contactCount, otherColliderId, otherBodyId;
        public bool ownCollisionIgnored;
        public Vector3 position, velocity, angularVelocity, rotationEuler, collisionImpulse, relativeVelocity, otherVelocity, ownerPosition, ownerVelocity, rackPoint, accumulatedExternalForce;
        public JointSample[] joints;
        public WheelSample[] originalWheelsHittingFlag;
    }
    [Serializable] public sealed class WheelSample
    {
        public int owner, wheel, instanceId;
        public string name;
        public float otherBodyForceScale;
        public Vector3 hitPoint, hitNormal, suspensionForce, frictionForce, vendorReactionForce, predictedUnconstrainedDeltaV;
    }
    sealed class OriginalWheel
    {
        public object source;
        public int owner,index;
        public PropertyInfo grounded,hitCollider,hitPoint,hitNormal;
        public FieldInfo suspension,friction,scale;
    }
    [Serializable] public sealed class Report
    {
        public int flagId;
        public string unityVersion, semantics="CONTROLLED NATIVE diagnostic: untouched natural eight-AI gameplay; pre-native FixedUpdate, native callbacks and post-native LateUpdate. Not full-match acceptance.";
        public float originalMass, originalLinearDamping, originalAngularDamping, originalFixedDeltaTime, originalTimeScale, firstSpeedBreachClock=-1, maximumSpeed, maximumStepDeltaV;
        public int entriesDropped, originalWheelBindings;
        public List<Sample> samples=new List<Sample>();
    }
    public Report report;
    Component flag;
    Rigidbody body;
    Collider shape;
    object match;
    MoonRules rules;
    FieldInfo clockField;
    PropertyInfo clockProperty, jointProperty, heldProperty, collectionsProperty;
    Array trucks;
    readonly List<OriginalWheel> wheels=new List<OriginalWheel>();
    Vector3 previousVelocity;
    bool hasPrevious;
    float lastLateFixedTime=-1;
    public void Configure(Component originalFlag, object originalMatch, MoonRules originalRules, Array originalTrucks)
    {
        flag=originalFlag;match=originalMatch;rules=originalRules;trucks=originalTrucks;
        var type=flag.GetType();body=(Rigidbody)type.GetField("body").GetValue(flag);shape=(Collider)type.GetField("shape").GetValue(flag);
        jointProperty=type.GetProperty("Joint");heldProperty=type.GetProperty("HeldSeconds");collectionsProperty=type.GetProperty("PhysicalCollections");
        clockProperty=match.GetType().GetProperty("Clock");clockField=match.GetType().GetField("Clock");
        for(int owner=0;owner<trucks.Length;owner++)
        {
            var truck=trucks.GetValue(owner);var originals=(Array)truck.GetType().GetField("wheels").GetValue(truck);
            for(int index=0;index<originals.Length;index++)
            {
                var wheel=originals.GetValue(index);var wt=wheel.GetType();const BindingFlags fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
                var binding=new OriginalWheel{source=wheel,owner=owner,index=index,grounded=wt.GetProperty("IsGrounded"),hitCollider=wt.GetProperty("HitCollider"),hitPoint=wt.GetProperty("HitPoint"),hitNormal=wt.GetProperty("HitNormal"),suspension=wt.GetField("_suspensionForce",fields),friction=wt.GetField("_frictionForce",fields),scale=wt.GetField("otherBodyForceScale")};
                if(binding.grounded==null||binding.hitCollider==null||binding.hitPoint==null||binding.hitNormal==null||binding.suspension==null||binding.friction==null||binding.scale==null)throw new InvalidOperationException("Original installed NWH wheel force observation contract changed; preserve vendor source.");
                wheels.Add(binding);
            }
        }
        if(wheels.Count!=32)throw new InvalidOperationException("All32 original NWH wheels required for the eight-AI energy diagnostic.");
        report=new Report{flagId=(int)type.GetField("id").GetValue(flag),unityVersion=Application.unityVersion,originalMass=body.mass,originalLinearDamping=body.linearDamping,originalAngularDamping=body.angularDamping,originalFixedDeltaTime=Time.fixedDeltaTime,originalTimeScale=Time.timeScale,originalWheelBindings=wheels.Count};
        Observe("CONFIGURED");
    }
    float Clock=>(float)(clockProperty!=null?clockProperty.GetValue(match):clockField.GetValue(match));
    void FixedUpdate(){if(report!=null)Observe("FIXED_AFTER_ORIGINAL_MODE_BEFORE_NATIVE_STEP");}
    void LateUpdate(){if(report!=null&&lastLateFixedTime!=Time.fixedTime){lastLateFixedTime=Time.fixedTime;Observe("LATE_AFTER_NATIVE_STEP");}}
    void OnCollisionEnter(Collision collision){if(report!=null)Observe("NATIVE_COLLISION_ENTER",collision);}
    void OnCollisionStay(Collision collision){if(report!=null)Observe("NATIVE_COLLISION_STAY",collision);}
    void OnJointBreak(float force){if(report!=null)Observe("NATIVE_JOINT_BREAK",null,force);}
    void Observe(string phase,Collision contact=null,float broken=0)
    {
        float clock=Clock,speed=body.linearVelocity.magnitude;
        report.maximumSpeed=Mathf.Max(report.maximumSpeed,speed);
        if(phase=="FIXED_AFTER_ORIGINAL_MODE_BEFORE_NATIVE_STEP")
        {
            if(hasPrevious)report.maximumStepDeltaV=Mathf.Max(report.maximumStepDeltaV,(body.linearVelocity-previousVelocity).magnitude);
            previousVelocity=body.linearVelocity;hasPrevious=true;
        }
        if(report.firstSpeedBreachClock<0&&speed>30)report.firstSpeedBreachClock=clock;
        // Preserve complete first transition plus two seconds; later ballistic
        // flight cannot diagnose its origin. Bound test-only telemetry memory.
        if(report.samples.Count>=12000||(report.firstSpeedBreachClock>=0&&clock>report.firstSpeedBreachClock+2)){report.entriesDropped++;return;}
        int owner=rules.Owners[report.flagId];var joint=(SpringJoint)jointProperty.GetValue(flag);
        var s=new Sample{phase=phase,clock=clock,fixedTime=Time.fixedTime,frame=Time.frameCount,owner=owner,jointId=joint!=null?joint.GetInstanceID():0,collections=(int)collectionsProperty.GetValue(flag),position=body.position,velocity=body.linearVelocity,angularVelocity=body.angularVelocity,rotationEuler=body.rotation.eulerAngles,speed=speed,mass=body.mass,heldSeconds=(float)heldProperty.GetValue(flag),breakCallbackForce=broken,accumulatedExternalForce=body.GetAccumulatedForce(Time.fixedDeltaTime)};
        var hitting=new List<WheelSample>();
        foreach(var wheel in wheels)
        {
            if(!(bool)wheel.grounded.GetValue(wheel.source))continue;
            var collider=(Collider)wheel.hitCollider.GetValue(wheel.source);if(collider==null||collider.attachedRigidbody!=body)continue;
            var component=(Component)wheel.source;var suspension=(Vector3)wheel.suspension.GetValue(wheel.source);var friction=(Vector3)wheel.friction.GetValue(wheel.source);float scale=(float)wheel.scale.GetValue(wheel.source);Vector3 reaction=-(suspension+friction)*scale;
            hitting.Add(new WheelSample{owner=wheel.owner,wheel=wheel.index,instanceId=component.GetInstanceID(),name=component.name,otherBodyForceScale=scale,hitPoint=(Vector3)wheel.hitPoint.GetValue(wheel.source),hitNormal=(Vector3)wheel.hitNormal.GetValue(wheel.source),suspensionForce=suspension,frictionForce=friction,vendorReactionForce=reaction,predictedUnconstrainedDeltaV=reaction*Time.fixedDeltaTime/body.mass});
        }
        s.originalWheelsHittingFlag=hitting.ToArray();
        var actual=body.GetComponents<SpringJoint>();s.jointCount=actual.Length;s.joints=new JointSample[actual.Length];
        for(int i=0;i<actual.Length;i++)
        {
            var j=actual[i];var connected=j.connectedBody;
            s.joints[i]=new JointSample{id=j.GetInstanceID(),connectedId=connected!=null?connected.GetInstanceID():0,connectedName=connected!=null?connected.name:"WORLD",connectedMass=connected!=null?connected.mass:0,anchor=j.anchor,connectedAnchor=j.connectedAnchor,worldAnchor=j.transform.TransformPoint(j.anchor),worldConnectedAnchor=connected!=null?connected.transform.TransformPoint(j.connectedAnchor):j.connectedAnchor,currentForce=j.currentForce,currentTorque=j.currentTorque,spring=j.spring,damper=j.damper,minDistance=j.minDistance,maxDistance=j.maxDistance,tolerance=j.tolerance,breakForce=j.breakForce,breakTorque=j.breakTorque,massScale=j.massScale,connectedMassScale=j.connectedMassScale};
        }
        if(owner>=0){var truck=(Component)trucks.GetValue(owner);var ownerBody=(Rigidbody)truck.GetType().GetField("body").GetValue(truck);s.ownerPosition=ownerBody.position;s.ownerVelocity=ownerBody.linearVelocity;s.rackPoint=truck.transform.TransformPoint(new Vector3(0,2.5f,.3f));}
        if(contact!=null)
        {
            s.collisionImpulse=contact.impulse;s.relativeVelocity=contact.relativeVelocity;s.contactCount=contact.contactCount;s.minContactSeparation=float.MaxValue;
            for(int i=0;i<contact.contactCount;i++)s.minContactSeparation=Mathf.Min(s.minContactSeparation,contact.GetContact(i).separation);
            var c=contact.collider;var other=c.attachedRigidbody;s.otherCollider=c.name;s.otherColliderId=c.GetInstanceID();s.otherBody=other!=null?other.name:"STATIC";s.otherBodyId=other!=null?other.GetInstanceID():0;s.otherMass=other!=null?other.mass:0;s.otherVelocity=other!=null?other.linearVelocity:Vector3.zero;s.ownCollisionIgnored=Physics.GetIgnoreCollision(shape,c);
        }
        report.samples.Add(s);
    }
}
#endif
