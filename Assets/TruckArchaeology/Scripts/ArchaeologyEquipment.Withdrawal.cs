using UnityEngine;
using LWS.TruckJenga;
namespace LWS.TruckArchaeology
{
    public sealed partial class ArchaeologyEquipment
    {
        public enum WithdrawalStage { None, Escape, Side, Rise, Ready }
        public WithdrawalStage Withdrawal {get;private set;}
        public int WithdrawalSteps {get;private set;}
        public int CompletedWithdrawals {get;private set;}
        public float InitialWithdrawalDepth {get;private set;}public Vector3 InitialWithdrawalNormal {get;private set;}
        ArchaeologyArtifact withdrawing;
        Vector3 withdrawalGoal;
        public bool HeadOverlaps(BoxCollider load,out Vector3 direction,out float distance)
        {
            return Physics.ComputePenetration(headShape,head.position,head.rotation,
                load,load.transform.position,load.transform.rotation,out direction,out distance);
        }
        Vector3 SphereCenter=>head.position+head.rotation*Vector3.Scale(((SphereCollider)headShape).center,head.transform.lossyScale);
        float Radius=>((SphereCollider)headShape).radius*Mathf.Max(Mathf.Abs(head.transform.lossyScale.x),Mathf.Abs(head.transform.lossyScale.y),Mathf.Abs(head.transform.lossyScale.z));
        float SafeHeight(ArchaeologyArtifact item)=>Mathf.Max(6+item.shape.size.y+2,item.shape.bounds.max.y+Radius*2);
        public bool WithdrawalReady(ArchaeologyArtifact item)=>withdrawing==item&&Withdrawal==WithdrawalStage.Ready&&head.position.y>=SafeHeight(item)-.001f&&!HeadOverlaps(item.shape,out _,out _);
        void ResetWithdrawal(){withdrawing=null;Withdrawal=WithdrawalStage.None;WithdrawalSteps=CompletedWithdrawals=0;InitialWithdrawalDepth=0;InitialWithdrawalNormal=Vector3.zero;}
        // Axis-aligned bounds conservatively enclose the real rotated box. Once
        // outside this expanded footprint, rising cannot cross that box.
        public static Vector3 SideExit(Vector3 center,Bounds bounds,float radius)
        {
            float margin=radius+.08f;
            if(center.x<=bounds.min.x-margin||center.x>=bounds.max.x+margin||center.z<=bounds.min.z-margin||center.z>=bounds.max.z+margin)return center;
            Vector3 best=new Vector3(bounds.min.x-margin,center.y,center.z);
            var candidate=new Vector3(bounds.max.x+margin,center.y,center.z);if((candidate-center).sqrMagnitude<(best-center).sqrMagnitude)best=candidate;
            candidate=new Vector3(center.x,center.y,bounds.min.z-margin);if((candidate-center).sqrMagnitude<(best-center).sqrMagnitude)best=candidate;
            candidate=new Vector3(center.x,center.y,bounds.max.z+margin);if((candidate-center).sqrMagnitude<(best-center).sqrMagnitude)best=candidate;
            return best;
        }
        public void StepWithdrawal(ArchaeologyArtifact item,float dt)
        {
            if(hoist.Stage!=JengaCrane.MotorStage.Idle||!item.Clearance)return;
            if(withdrawing!=item){withdrawing=item;Withdrawal=WithdrawalStage.Escape;HeadOverlaps(item.shape,out Vector3 normal,out float depth);InitialWithdrawalNormal=normal;InitialWithdrawalDepth=depth;}
            if(Withdrawal==WithdrawalStage.Ready){if(WithdrawalReady(item))return;Withdrawal=WithdrawalStage.Escape;}
            WithdrawalSteps++;
            if(Withdrawal==WithdrawalStage.Escape)
            {
                // V8 measured an actual DOWN minimum-translation direction at
                // release. Follow the actual query, never assume UP is an escape.
                if(HeadOverlaps(item.shape,out Vector3 direction,out float depth))
                { MoveWithdrawal(head.position+direction*(depth+.06f),dt);return; }
                Vector3 center=SphereCenter;
                withdrawalGoal=center.y-Radius>item.shape.bounds.max.y+.04f?head.position:head.position+SideExit(center,item.shape.bounds,Radius)-center;
                Withdrawal=WithdrawalStage.Side;
            }
            if(Withdrawal==WithdrawalStage.Side)
            {
                if(!MoveWithdrawal(withdrawalGoal,dt))return;
                withdrawalGoal=new Vector3(head.position.x,SafeHeight(item),head.position.z);Withdrawal=WithdrawalStage.Rise;
            }
            if(Withdrawal==WithdrawalStage.Rise&&MoveWithdrawal(withdrawalGoal,dt))
            {Withdrawal=WithdrawalStage.Ready;CompletedWithdrawals++;}
        }
        bool MoveWithdrawal(Vector3 destination,float dt)
        {
            Aim=destination;
            // Same bounded original boom speed, one fixed-step MovePosition.
            // Test actual Rigidbody pose, so release cannot precede the queued move.
            if(Vector3.Distance(head.position,destination)<.001f)return true;
            head.MovePosition(Vector3.MoveTowards(head.position,destination,9*dt));return false;
        }
    }
}
