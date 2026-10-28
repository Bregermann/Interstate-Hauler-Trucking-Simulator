using UnityEngine;
namespace LWS.TruckJenga
{
    public sealed class JengaCrane : MonoBehaviour
    {
        public enum MotorStage { Idle, Approach, LowerPickup, Lift, Traverse, LowerStack, Release, Rest }
        public Rigidbody spreader;public Transform bridge,trolley;public LineRenderer hoist;
        public float travelSpeed=4,liftSpeed=2.4f,turnSpeed=45,ceiling=48,parkHeight=22;
        public MotorStage Stage { get; private set; }
        public Rigidbody Load { get; private set; }
        public int Pickups { get; private set; }public int Releases { get; private set; }
        public bool Released=>Stage==MotorStage.Rest;
        FixedJoint grip;Vector3 destination;Quaternion destinationRotation;float pieceHeight,clearanceHeight;
        public bool Begin(Rigidbody piece,Vector3 target,Quaternion rotation,float height)
        {
            if(Stage!=MotorStage.Idle||piece==null||piece.isKinematic)return false;Load=piece;destination=target;destinationRotation=rotation;pieceHeight=height;clearanceHeight=Mathf.Max(target.y+height*.5f+3,piece.position.y+height+2);Stage=MotorStage.Approach;return true;
        }
        public void Step(float dt,float towerTop)
        {
            if(Stage==MotorStage.Idle||Stage==MotorStage.Rest||Load==null)return;Vector3 desired=spreader.position;Quaternion rotation=spreader.rotation;float safe=Mathf.Max(towerTop+pieceHeight+2,clearanceHeight);
            switch(Stage){
                case MotorStage.Approach:desired=new Vector3(Load.position.x,safe,Load.position.z);rotation=Load.rotation;if(Move(desired,rotation,travelSpeed,dt))Stage=MotorStage.LowerPickup;break;
                case MotorStage.LowerPickup:desired=Load.transform.TransformPoint(Vector3.up*(pieceHeight*.5f));rotation=Load.rotation;if(Move(desired,rotation,liftSpeed,dt)&&Load.linearVelocity.magnitude<.7f){grip=Load.gameObject.AddComponent<FixedJoint>();grip.autoConfigureConnectedAnchor=false;grip.anchor=Vector3.up*(pieceHeight*.5f);grip.connectedBody=spreader;grip.connectedAnchor=Vector3.zero;grip.enableCollision=false;grip.breakForce=Mathf.Infinity;grip.massScale=grip.connectedMassScale=1;Pickups++;Stage=MotorStage.Lift;}break;
                case MotorStage.Lift:desired=new Vector3(spreader.position.x,safe,spreader.position.z);if(Move(desired,rotation,liftSpeed,dt))Stage=MotorStage.Traverse;break;
                case MotorStage.Traverse:desired=new Vector3(destination.x,safe,destination.z);rotation=destinationRotation;if(Move(desired,rotation,travelSpeed,dt))Stage=MotorStage.LowerStack;break;
                case MotorStage.LowerStack:desired=destination+Vector3.up*(pieceHeight*.5f+.06f);rotation=destinationRotation;if(Move(desired,rotation,liftSpeed*.45f,dt))Stage=MotorStage.Release;break;
                case MotorStage.Release:if(grip!=null){Destroy(grip);grip=null;}Releases++;Stage=MotorStage.Rest;break;
            }
        }
        bool Move(Vector3 target,Quaternion rotation,float speed,float dt){spreader.MovePosition(Vector3.MoveTowards(spreader.position,target,speed*dt));spreader.MoveRotation(Quaternion.RotateTowards(spreader.rotation,rotation,turnSpeed*dt));return Vector3.Distance(spreader.position,target)<.06f&&Quaternion.Angle(spreader.rotation,rotation)<1;}
        public bool AtRest(Vector3 center,float height)=>Released&&Load!=null&&!Load.isKinematic&&Vector3.Distance(Load.position,center)<.5f&&Vector3.Dot(Load.transform.up,Vector3.up)>.98f&&Load.linearVelocity.magnitude<.25f&&Load.angularVelocity.magnitude<.2f&&Quaternion.Angle(Load.rotation,destinationRotation)<8&&Load.GetComponent<FixedJoint>()==null;
        public void Complete(){if(Stage==MotorStage.Rest){Load=null;Stage=MotorStage.Idle;}}
        public void ResetCrane(){if(grip!=null){Destroy(grip);grip=null;}Load=null;Stage=MotorStage.Idle;Pickups=Releases=0;if(spreader!=null){spreader.position=new Vector3(16,Mathf.Min(ceiling,parkHeight),0);spreader.rotation=Quaternion.identity;}}
        public void Present(){if(spreader==null)return;if(bridge!=null)bridge.position=new Vector3(spreader.position.x,ceiling,0);if(trolley!=null)trolley.position=new Vector3(spreader.position.x,ceiling,spreader.position.z);if(hoist!=null){hoist.SetPosition(0,new Vector3(spreader.position.x,ceiling,spreader.position.z));hoist.SetPosition(1,spreader.position);}}
        public void StopCrane(){if(grip!=null)Destroy(grip);grip=null;Load=null;Stage=MotorStage.Idle;}
        void OnDisable(){StopCrane();}
    }
}
