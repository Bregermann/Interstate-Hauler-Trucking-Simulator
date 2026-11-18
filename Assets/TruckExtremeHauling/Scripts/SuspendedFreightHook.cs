using UnityEngine;
namespace LWS.TruckExtremeHauling
{
    /// <summary>A real suspended winch hook. Native joints carry the same original load.</summary>
    public sealed class SuspendedFreightHook : MonoBehaviour
    {
        public Rigidbody body;
        public SpringJoint rope;
        public PhysicalCableCar carrier;
        public LineRenderer line;
        public Transform winchVisual;
        public Transform resetPose;
        public float initialLength=18,minimumLength=4,maximumLength=25,winchSpeed=2.5f;
        ConfigurableJoint latch;
        public Rigidbody Captured {get;private set;}
        public bool Holding => latch!=null&&Captured!=null&&latch.connectedBody==Captured;
        public int Captures {get;private set;}
        public void ResetHook()
        {
            Release();Captures=0;body.isKinematic=false;body.position=resetPose.position;body.rotation=resetPose.rotation;body.linearVelocity=body.angularVelocity=Vector3.zero;
            rope.connectedBody=carrier.trolley;rope.maxDistance=initialLength;
        }
        public bool TryCapture(Rigidbody load,Vector3 localEye)
        {
            if(Holding||load==null||load.isKinematic)return false;
            Vector3 eye=load.transform.TransformPoint(localEye);
            if(Vector3.Distance(body.position,eye)>.4f||(body.GetPointVelocity(body.position)-load.GetPointVelocity(eye)).magnitude>.9f)return false;
            latch=body.gameObject.AddComponent<ConfigurableJoint>();latch.autoConfigureConnectedAnchor=false;latch.connectedBody=load;
            latch.anchor=Vector3.zero;latch.connectedAnchor=load.transform.InverseTransformPoint(body.position);
            latch.xMotion=latch.yMotion=latch.zMotion=ConfigurableJointMotion.Locked;
            latch.angularXMotion=latch.angularYMotion=latch.angularZMotion=ConfigurableJointMotion.Free;
            latch.enableCollision=false;latch.breakForce=1500000;latch.breakTorque=1500000;
            Captured=load;Captures++;return true;
        }
        public void Release(){if(latch!=null)Destroy(latch);latch=null;Captured=null;}
        public void Apply(float sway,float hoist,float dt)
        {
            if(body.isKinematic)return;
            rope.maxDistance=Mathf.Clamp(rope.maxDistance-Mathf.Clamp(hoist,-1,1)*winchSpeed*dt,minimumLength,maximumLength);
            body.AddForce(Vector3.right*Mathf.Clamp(sway,-1,1)*5,ForceMode.Acceleration);
        }
        public void Freeze(){Release();body.isKinematic=true;}
        void LateUpdate()
        {if(carrier==null)return;if(winchVisual!=null)winchVisual.SetPositionAndRotation(carrier.trolley.position,carrier.trolley.rotation);if(line==null)return;line.positionCount=2;line.SetPosition(0,carrier.trolley.position);line.SetPosition(1,body.position);}
    }
}
