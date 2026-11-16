using System;
using UnityEngine;
using LWS.TruckSports;

namespace LWS.TruckHoardHeist
{
    [Serializable]
    public sealed class HoardCargoJointBreakSnapshot
    {
        public int carrier, frame, remainingJoints, securedJointId;
        public float clock, fixedTime, force, configuredBreakForce, cargoMass, contactClock, contactFixedTime, contactSpeed;
        public bool matchLive, jointReferenceStillAlive, contactWasOwnTruck;
        public string contactCollider, contactBody, overlaps, remainingJointDetails, captureError;
        public Vector3 cargoPosition, cargoVelocity, cargoAngularVelocity, cargoSize, cargoScale, truckPosition, truckVelocity, truckAngularVelocity, truckScale, rackPosition, rackSize, rackScale;
    }

    public sealed class HoardTreasure : MonoBehaviour
    {
        public HoardHeistMatch match;
        public string stableId;
        public int index, value = 100;
        public Rigidbody body;
        public BoxCollider shape;
        public AudioSource clink;
        public AudioClip clinkClip;
        public Vector3 seedPosition;
        public Quaternion seedRotation = Quaternion.identity;
        public int RackSlot { get; set; } = -1;
        public FixedJoint CargoJoint { get; private set; }
        public int Carrier { get; private set; } = -1;
        public float LastReleasedAt { get; private set; } = -10;
        public float LastJointBreakAt { get; private set; } = -1;
        public float LastJointBreakForce { get; private set; }
        public int LastBrokenCarrier { get; private set; } = -1;
        public string LastImpactCollider { get; private set; }
        public float LastImpactAt { get; private set; } = -1;
        public float LastImpactSpeed { get; private set; }
        public HoardCargoJointBreakSnapshot LastJointBreakSnapshot { get; private set; }
        float lastClink = -10;
        float configuredBreakForce, lastContactFixedTime;
        string lastContactBody;
        bool lastContactWasOwnTruck;
        int securedJointId;
        public float Height => shape.size.y * Mathf.Abs(shape.transform.lossyScale.y);

        public void ResetTreasure()
        {
            Release(); RackSlot = -1; LastReleasedAt = lastClink = -10; if (clink != null) clink.Stop();
            LastJointBreakAt=LastImpactAt=-1;LastJointBreakForce=LastImpactSpeed=0;LastBrokenCarrier=-1;LastImpactCollider=null;
            LastJointBreakSnapshot=null;configuredBreakForce=0;lastContactFixedTime=-1;lastContactBody=null;lastContactWasOwnTruck=false;securedJointId=0;
            gameObject.SetActive(true); body.isKinematic = false; body.detectCollisions = true;
            body.position = seedPosition; body.rotation = seedRotation;
            body.linearVelocity = body.angularVelocity = Vector3.zero;
        }
        public bool Secure(int owner, Rigidbody truck)
        {
            if (CargoJoint != null || truck == null || body == null || body.isKinematic) return false;
            CargoJoint = gameObject.AddComponent<FixedJoint>(); CargoJoint.connectedBody = truck;
            CargoJoint.enableCollision = false;
            // Native cargo inertia remains real. Hard impacts can break the
            // restraint, leaving the same physical object available to rivals.
            CargoJoint.breakForce = Mathf.Max(800, body.mass * 35);
            configuredBreakForce = CargoJoint.breakForce;
            securedJointId = CargoJoint.GetInstanceID();
            CargoJoint.breakTorque = CargoJoint.breakForce * 1.5f;
            Carrier = owner; return true;
        }
        public void Release()
        {
            // Keep the existing body connection coherent until deferred destruction.
            // Disconnecting first turns this FixedJoint into a world restraint.
            if (CargoJoint != null) Destroy(CargoJoint);
            CargoJoint = null; Carrier = -1; RackSlot = -1;
            LastReleasedAt = match != null ? match.Clock : Time.time;
        }
        void OnJointBreak(float force)
        {
            try { CaptureJointBreak(force); }
            catch(Exception error)
            {
                LastJointBreakSnapshot=new HoardCargoJointBreakSnapshot { carrier=Carrier,force=force,captureError=error.GetType().Name+": "+error.Message };
            }
            // Diagnostic failures cannot change the original release authority.
            LastJointBreakAt=match!=null?match.Clock:Time.time;LastJointBreakForce=force;LastBrokenCarrier=Carrier;CargoJoint = null; Carrier = -1; LastReleasedAt = match != null ? match.Clock : Time.time;
        }
        void CaptureJointBreak(float force)
        {
            // Capture the event before ownership changes. Later collisions must
            // not overwrite the contact/vehicle witnesses for this break.
            var truck=match!=null&&Carrier>=0&&Carrier<match.trucks.Length?match.trucks[Carrier]:null;
            var rack=match!=null&&Carrier>=0&&Carrier<match.cargoRacks.Length?match.cargoRacks[Carrier]:null;
            LastJointBreakSnapshot=new HoardCargoJointBreakSnapshot {
                carrier=Carrier,frame=Time.frameCount,clock=match!=null?match.Clock:Time.time,fixedTime=Time.fixedTime,force=force,
                configuredBreakForce=configuredBreakForce,cargoMass=body!=null?body.mass:0,matchLive=match!=null&&match.Live,
                jointReferenceStillAlive=CargoJoint!=null,remainingJoints=GetComponents<Joint>().Length,securedJointId=securedJointId,
                contactCollider=LastImpactCollider,contactBody=lastContactBody,contactClock=LastImpactAt,contactFixedTime=lastContactFixedTime,
                contactSpeed=LastImpactSpeed,contactWasOwnTruck=lastContactWasOwnTruck,
                cargoPosition=body!=null?body.position:transform.position,cargoVelocity=body!=null?body.linearVelocity:Vector3.zero,
                cargoAngularVelocity=body!=null?body.angularVelocity:Vector3.zero,cargoSize=shape!=null?shape.size:Vector3.zero,cargoScale=transform.lossyScale,
                truckPosition=truck!=null?truck.body.position:Vector3.zero,truckVelocity=truck!=null?truck.body.linearVelocity:Vector3.zero,
                truckAngularVelocity=truck!=null?truck.body.angularVelocity:Vector3.zero,truckScale=truck!=null?truck.transform.lossyScale:Vector3.zero,
                rackPosition=rack!=null?rack.transform.position:Vector3.zero,rackSize=rack!=null?rack.size:Vector3.zero,rackScale=rack!=null?rack.transform.lossyScale:Vector3.zero
            };
            var remaining=new System.Collections.Generic.List<string>();
            foreach(var joint in GetComponents<Joint>())if(joint!=null)remaining.Add(joint.GetInstanceID()+"|connected="+(joint.connectedBody!=null?joint.connectedBody.name:"world")+"|limit="+joint.breakForce.ToString("R",System.Globalization.CultureInfo.InvariantCulture));
            LastJointBreakSnapshot.remainingJointDetails=string.Join(";",remaining);
            if(shape!=null)
            {
                var contacts=new Collider[32];int count=Physics.OverlapBoxNonAlloc(shape.bounds.center,shape.bounds.extents,contacts,Quaternion.identity,~0,QueryTriggerInteraction.Ignore);
                var witnesses=new System.Collections.Generic.List<string>();
                for(int i=0;i<count;i++)
                {
                    var other=contacts[i];if(other==null||other.attachedRigidbody==body)continue;
                    bool penetrating=Physics.ComputePenetration(shape,shape.transform.position,shape.transform.rotation,other,other.transform.position,other.transform.rotation,out _,out float depth);
                    witnesses.Add(other.name+"|body="+(other.attachedRigidbody!=null?other.attachedRigidbody.name:"static")+"|penetrating="+penetrating+"|depth="+depth.ToString("R",System.Globalization.CultureInfo.InvariantCulture));
                }
                LastJointBreakSnapshot.overlaps=(count==contacts.Length?"QUERY_SATURATED;":"")+string.Join(";",witnesses);
            }
        }
        void RecordContact(Collision collision)
        {
            LastImpactAt=match.Clock;LastImpactSpeed=collision.impulse.magnitude/Mathf.Max(1,body.mass);LastImpactCollider=collision.collider.name;
            lastContactFixedTime=Time.fixedTime;lastContactBody=collision.rigidbody!=null?collision.rigidbody.name:null;
            lastContactWasOwnTruck=Carrier>=0&&Carrier<match.trucks.Length&&collision.rigidbody==match.trucks[Carrier].body;
        }
        void OnCollisionStay(Collision collision)
        {
            if(match!=null&&match.Live&&body!=null&&CargoJoint!=null)RecordContact(collision);
        }
        void OnCollisionEnter(Collision collision)
        {
            if (match == null || !match.Live || match.Intro > 0 || body == null) return;
            var truck = collision.collider.GetComponentInParent<SportsEventTruck>();
            int owner = truck != null ? truck.slot : match.Rules != null && index >= 0 && index < match.Rules.Owners.Length ? match.Rules.Owners[index] : -1;
            float impulseSpeed = collision.impulse.magnitude / Mathf.Max(1, body.mass);
            RecordContact(collision);
            if (impulseSpeed > .35f) match.ReportCollisionNoise(owner, body.position, impulseSpeed, body.GetInstanceID(), collision.rigidbody != null ? collision.rigidbody.GetInstanceID() : collision.collider.GetInstanceID(), "LOOSE TREASURE");
            if (impulseSpeed > .5f && match.Clock-lastClink>.25f && clink!=null && clinkClip!=null && !clink.isPlaying)
            { lastClink=match.Clock;clink.clip=clinkClip;clink.volume=Mathf.Clamp01(impulseSpeed*.1f)*match.settings.sfx;clink.Play(); }
        }
    }

}
