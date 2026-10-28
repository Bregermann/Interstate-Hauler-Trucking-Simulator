using UnityEngine;
using LWS.TruckSports;
namespace LWS.TruckSnowmanBuilding
{
    public sealed class SnowmanBall:MonoBehaviour
    {
        public string stableId;public int role;public SnowmanMatch match;public Rigidbody body;public SphereCollider sphere;public Transform shell;public SportsSnowBodyContact contact;
        public int LastTruckContactSlot {get;private set;}=-1;public float LastTruckContactClock {get;private set;}=-100;
        public float radius=.75f,maximumRadius=3,density=35;public bool Packed {get;private set;}public float Rolled {get;private set;}public int LastPusher {get;private set;}=-1;
        Vector3 previous;Quaternion previousRotation;FixedJoint bond;float rest;SnowmanSnowPatch gathering;Vector3 gatherPoint;float gatherStamp=-100,packStamp=-100;Collider packSurface;
        public void ResetBall(Vector3 p){Unpack();radius=.75f;Rolled=0;LastPusher=-1;LastTruckContactSlot=-1;LastTruckContactClock=-100;Size();body.isKinematic=false;body.position=p;body.rotation=Quaternion.identity;body.linearVelocity=body.angularVelocity=Vector3.zero;Track();}
        void Size(){sphere.radius=radius;shell.localScale=Vector3.one*(2*radius);body.mass=4*Mathf.PI*radius*radius*radius/3*density;body.ResetInertiaTensor();}
        public void Track(){previous=body.position;previousRotation=body.rotation;rest=0;gathering=null;packSurface=null;gatherStamp=packStamp=-100;}
        public void Step(){if(Packed){Track();return;}float distance=Vector3.ProjectOnPlane(body.position-previous,Vector3.up).magnitude;float radians=Quaternion.Angle(body.rotation,previousRotation)*Mathf.Deg2Rad;var patch=gathering!=null&&Time.fixedTime-gatherStamp<.07f?gathering:null;if(patch!=null){double next=SnowmanRules.Grow(radius,distance,radians,patch.depth,patch.Available(gatherPoint,radius*2),maximumRadius,out double wanted);float used=patch.Consume(gatherPoint,radius*2,(float)wanted);if(used>0){float oldMass=body.mass,oldRadius=radius;double volume=4*System.Math.PI*radius*radius*radius/3;radius=(float)System.Math.Pow((volume+used)*3/(4*System.Math.PI),1d/3);Size();body.linearVelocity*=oldMass/body.mass;body.angularVelocity*=oldMass*oldRadius*oldRadius/(body.mass*radius*radius);Rolled+=distance;}}previous=body.position;previousRotation=body.rotation;}
        bool SupportedPackingPair()
        {
            // PhysX omits Stay callbacks for sleeping bodies. A genuine upward
            // contact remains paired until Exit/reset/disable, never a timer.
            if(packSurface==null)return false;
            if(!isActiveAndEnabled||!sphere.enabled||!body.detectCollisions||!packSurface.enabled||!packSurface.gameObject.activeInHierarchy){ClearPackingPair();return false;}
            Vector3 offset=body.position-packSurface.ClosestPoint(body.position);float distance=offset.magnitude;
            float skin=sphere.contactOffset+packSurface.contactOffset;
            // Also reject real geometric separation even before an Exit callback
            // (for example a deliberately disabled/moved supporting collider).
            if(distance<.0001f||distance>radius+skin||offset.y/distance<.5f){ClearPackingPair();return false;}
            return true;
        }
        void ClearPackingPair(){packSurface=null;packStamp=-100;rest=0;}
        public bool RestOn(SnowmanBall lower,Collider foundation,float dt){if(Packed)return true;bool support=SupportedPackingPair()&&(lower!=null?packSurface.attachedRigidbody==lower.body:packSurface==foundation);float xz=lower!=null?Vector3.ProjectOnPlane(body.position-lower.body.position,Vector3.up).magnitude:Vector3.ProjectOnPlane(body.position-match.buildCenter.position,Vector3.up).magnitude;float expected=lower!=null?lower.body.position.y+lower.radius+radius:foundation.bounds.max.y+radius;bool valid=support&&xz<(lower!=null?lower.radius*.28f:1.2f)&&Mathf.Abs(body.position.y-expected)<.2f&&body.linearVelocity.magnitude<.3f&&body.angularVelocity.magnitude<.3f;rest=valid?rest+dt:0;return rest>.65f;}
        public void CancelRest(){rest=0;}
        public void Pack(SnowmanBall lower){if(Packed)return;bond=gameObject.AddComponent<FixedJoint>();bond.connectedBody=lower!=null?lower.body:match.foundationBody;bond.autoConfigureConnectedAnchor=true;Packed=true;}
        public void Unpack(){if(bond!=null){Destroy(bond);bond=null;}Packed=false;}
        public void Restore(float r,float rolled,int pusher,Vector3 p,Quaternion q,Vector3 v,Vector3 w){Unpack();radius=r;Rolled=rolled;LastPusher=pusher;Size();body.isKinematic=false;body.position=p;body.rotation=q;body.linearVelocity=v;body.angularVelocity=w;Track();}
        // Mode semantics keep snow/foundation contacts distinct from truck bumper contacts.
        // The shared observer remains present for Weatherade's existing track renderer.
        void Observe(Collision collision){var patch=collision.collider.GetComponent<SnowmanSnowPatch>();bool packing=collision.collider==match.foundation||collision.collider.GetComponent<SnowmanBall>()!=null;for(int i=0;i<collision.contactCount;i++){var point=collision.GetContact(i);if(point.normal.y<.5f)continue;if(patch!=null){gathering=patch;gatherPoint=point.point;gatherStamp=Time.fixedTime;}if(packing){packSurface=collision.collider;packStamp=Time.fixedTime;}}
var collector=collision.collider.GetComponentInParent<SnowmanCollectionRack>();if(collector!=null&&collector.match==match)collector.Observe(this,collision);
var truck=collision.collider.GetComponentInParent<SportsEventTruck>();if(truck!=null&&truck.session==match){LastTruckContactSlot=truck.slot;LastTruckContactClock=match.Clock;if(collision.relativeVelocity.magnitude>.05f)LastPusher=truck.slot;}}
        void OnCollisionEnter(Collision c){Observe(c);}void OnCollisionStay(Collision c){Observe(c);}void OnCollisionExit(Collision c){if(gathering!=null&&c.collider==gathering.ground){gathering=null;gatherStamp=-100;}if(c.collider==packSurface)ClearPackingPair();}
        void OnDisable(){if(body!=null)Track();else ClearPackingPair();}
    }
}
