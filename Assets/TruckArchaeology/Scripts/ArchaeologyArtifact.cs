using UnityEngine;
namespace LWS.TruckArchaeology
{
    public sealed class ArchaeologyArtifact : MonoBehaviour
    {
        public ArchaeologyMatch match; public string stableId;public int index,value=100;public Rigidbody body;public BoxCollider shape;
        public Vector3 seedPosition;public Quaternion seedRotation=Quaternion.identity;public Renderer[] actualModels;
        public Vector3 WorkPoint {get;private set;}public bool HasWorkPoint=>float.IsFinite(WorkPoint.x)&&float.IsFinite(WorkPoint.y)&&float.IsFinite(WorkPoint.z);public float Exposure {get;private set;}public bool Clearance {get;private set;}public int RackOwner {get;private set;}=-1;
        public FixedJoint CargoJoint {get;private set;}float lastImpact=-10;
        public void ResetArtifact(){ReleaseCargo();body.gameObject.SetActive(index<match.setup.count*3);if(!body.isKinematic)body.linearVelocity=body.angularVelocity=Vector3.zero;body.isKinematic=true;body.position=seedPosition;body.rotation=seedRotation;WorkPoint=seedPosition;Exposure=0;Clearance=false;RackOwner=-1;lastImpact=-10;}
        public void ObserveExposure(){Exposure=match.earth.Exposure(shape,out bool clear,out Vector3 point);WorkPoint=point;Clearance=clear;if(clear&&match.HeadClearForRelease(this)&&match.Rules.Expose(index,true))body.isKinematic=false;}
        public bool OnRack(int owner)
        {
            return RackOwner==owner&&body.linearVelocity.magnitude<.45f&&body.angularVelocity.magnitude<.4f&&Vector3.Dot(transform.up,Vector3.up)>.75f;
        }
        public void SecureCargo(int owner)
        {
            CargoJoint=gameObject.AddComponent<FixedJoint>();CargoJoint.connectedBody=match.trucks[owner].body;CargoJoint.enableCollision=false;CargoJoint.breakForce=18000;CargoJoint.breakTorque=18000;
        }
        public void ReleaseCargo(){if(CargoJoint!=null){Destroy(CargoJoint);CargoJoint=null;}}
        void OnCollisionStay(Collision collision){var rack=collision.collider.GetComponent<ArchaeologyRack>();if(rack!=null&&rack.match==match)RackOwner=rack.owner;}
        void OnCollisionExit(Collision collision){var rack=collision.collider.GetComponent<ArchaeologyRack>();if(rack!=null&&rack.owner==RackOwner)RackOwner=-1;}
        void OnCollisionEnter(Collision collision)
        {
            if(match==null||!match.Live||match.Clock-lastImpact<.2f)return;float change=collision.impulse.magnitude/Mathf.Max(1,body.mass);
            if(change>2){match.Rules.Damage(index,(change-2)*.015);lastImpact=match.Clock;}
        }
    }
}
