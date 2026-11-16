using System;
using LWS.TruckSports;
using UnityEngine;

namespace LWS.TruckDragonEggDelivery
{
    public sealed partial class DragonEggDeliveryMatch
    {
        readonly int[] bayBlockingCrew=new int[4],yieldTo=new int[4],yieldPhase=new int[4],yieldStarts=new int[4],yieldSeparatingSteps=new int[4];
        readonly float[] bayBlockedSince=new float[4],yieldUntil=new float[4],yieldRetryAt=new float[4];
        readonly Vector3[] yieldOrigin=new Vector3[4],yieldRear=new Vector3[4],yieldGoal=new Vector3[4];
        readonly Collider[][] yieldShapes=new Collider[4][];
        public int BayYieldStarts(int slot)=>slot>=0&&slot<4?yieldStarts[slot]:0;
        public int BayYieldSeparatingSteps(int slot)=>slot>=0&&slot<4?yieldSeparatingSteps[slot]:0;
        public bool BayYieldActive(int slot)=>slot>=0&&slot<4&&yieldPhase[slot]!=0;
        void ResetBayYields()
        {
            Array.Clear(yieldPhase,0,4);Array.Clear(yieldStarts,0,4);Array.Clear(yieldSeparatingSteps,0,4);Array.Clear(bayBlockedSince,0,4);Array.Clear(yieldUntil,0,4);Array.Clear(yieldRetryAt,0,4);
            for(int i=0;i<4;i++){bayBlockingCrew[i]=yieldTo[i]=-1;yieldShapes[i]=null;}
        }
        int BayPriority(int slot)=>(slot+(int)((uint)MatchSeed%4))%4;
        int BayBodyOwner(Collider shape)
        {
            var truck=shape.GetComponentInParent<SportsEventTruck>();if(truck!=null)return Array.IndexOf(trucks,truck);
            for(int i=0;i<4;i++)if(shape.transform.IsChildOf(carriers[i].cargo.transform)&&carriers[i].cargo.Restraint!=null&&carriers[i].cargo.Restraint.connectedBody==trucks[i].body)return i;
            return -1;
        }
        bool TryBeginBayYield(int slot)
        {
            int other=bayBlockingCrew[slot];
            // Stable right of way rotates with the ordinary match seed; a pair
            // cannot both reverse at once. Four bounded attempts, no timer award.
            if(other<0||other==slot||BayPriority(slot)<BayPriority(other)||Clock<yieldRetryAt[slot]||yieldStarts[slot]>=4)return false;
            var truck=trucks[slot];Vector3 forward=Vector3.ProjectOnPlane(truck.transform.forward,Vector3.up).normalized,right=Vector3.Cross(Vector3.up,forward),origin=truck.body.position;
            yieldShapes[slot]=truck.GetComponentsInChildren<Collider>(true);if(yieldShapes[slot].Length>128)return false;
            // Existing Musical Parking's finite reverse/side escape, with every
            // proposed leg additionally subject to this mode's real full-wheel,
            // height and body support queries before NWH receives any command.
            Vector3 rear=origin-forward*3;
            for(int side=0;side<2;side++)
            {
                float sign=((slot+yieldStarts[slot])&1)==0?1:-1;if(side==1)sign=-sign;
                Vector3 target=origin-forward*5+right*(sign*5.5f);
                if(!YieldSegmentClear(slot,origin,rear,truck.body.rotation)||!YieldSegmentClear(slot,rear,target,Quaternion.LookRotation(Vector3.ProjectOnPlane(rear-target,Vector3.up))))continue;
                yieldOrigin[slot]=origin;yieldRear[slot]=rear;yieldGoal[slot]=target;yieldTo[slot]=other;yieldPhase[slot]=1;yieldUntil[slot]=Clock+20;yieldStarts[slot]++;bayParking[slot]=true;return true;
            }
            yieldRetryAt[slot]=Clock+2;return false;
        }
        bool YieldSegmentClear(int slot,Vector3 from,Vector3 to,Quaternion rotation)
        {
            int steps=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(from,to)));if(steps>10)return false;Pose previous=default;
            for(int i=0;i<=steps;i++)
            {
                if(!TryBayPose(slot,Vector3.Lerp(from,to,i/(float)steps),rotation,out var pose)||i>0&&Mathf.Abs(pose.position.y-previous.position.y)>.65f)return false;previous=pose;
            }
            return true;
        }
        bool DriveBayYield(int slot,out Vector2 drive)
        {
            drive=Vector2.zero;var truck=trucks[slot];bayParking[slot]=true;
            if(Clock>=yieldUntil[slot]){yieldPhase[slot]=0;yieldRetryAt[slot]=Clock+2;bayNextCheck[slot]=0;bayMoveBlocked[slot]=true;bayStatus[slot]="BAY YIELD · BOUNDED RETRY";return true;}
            if(yieldPhase[slot]==1&&Vector3.ProjectOnPlane(truck.body.position-yieldOrigin[slot],Vector3.up).magnitude>=2.8f)yieldPhase[slot]=2;
            if(yieldPhase[slot]==2&&Vector3.ProjectOnPlane(truck.body.position-yieldGoal[slot],Vector3.up).magnitude<1.6f)yieldPhase[slot]=3;
            if(yieldPhase[slot]==3)
            {
                bayMoveBlocked[slot]=true;bayStatus[slot]="BAY YIELD · LET THE OTHER CREW PASS";
                if(Vector3.ProjectOnPlane(truck.body.position-trucks[yieldTo[slot]].body.position,Vector3.up).magnitude>7&&BayMovementClear(slot,false))
                {yieldPhase[slot]=0;yieldRetryAt[slot]=Clock+1;bayNextCheck[slot]=0;bayBlockedSince[slot]=0;}
                return true;
            }
            // Straight withdrawal first: steering cannot rotate an already
            // contacting cab into its neighbour while we separate their bodies.
            Vector3 target=yieldPhase[slot]==1?yieldRear[slot]:yieldGoal[slot],delta=Vector3.ProjectOnPlane(target-truck.body.position,Vector3.up);
            float angle=Vector3.SignedAngle(Vector3.ProjectOnPlane(truck.transform.forward,Vector3.up),delta,Vector3.up);
            if(yieldPhase[slot]==1||Mathf.Abs(angle)>128)bayReverse[slot]=true;else if(Mathf.Abs(angle)<82)bayReverse[slot]=false;
            bool reverse=bayReverse[slot];bayReverseObserved[slot]|=reverse;
            bayMoveBlocked[slot]=!BayMovementClear(slot,reverse,true);if(bayMoveBlocked[slot])return true;
            float steer=yieldPhase[slot]==1?0:Mathf.Clamp((reverse?Mathf.DeltaAngle(angle,180):angle)/28,-1,1)*(reverse?-1:1);
            float desired=Mathf.Min(1.5f,Mathf.Max(.55f,delta.magnitude*.6f)),along=Vector3.Dot(truck.body.linearVelocity,truck.transform.forward);
            drive=new Vector2(steer,Mathf.Clamp(((reverse?-desired:desired)-along)*.65f,-1,1));routes[slot].DirectGoal(truck.body.position,target);
            bayStatus[slot]="BAY YIELD · "+(yieldPhase[slot]==1?"SUPPORTED STRAIGHT REVERSE":"SUPPORTED SIDE WITHDRAWAL");return true;
        }
        bool SeparatingFromBody(int slot,Collider other,Vector3 direction,float horizon)
        {
            var shapes=yieldShapes[slot];if(shapes==null||shapes.Length>128)return false;
            Vector3 displacement=direction*Mathf.Min(.25f,horizon),away=Vector3.ProjectOnPlane(trucks[slot].shape.bounds.center-other.bounds.center,Vector3.up);
            if(Vector3.Dot(direction,away.normalized)<.15f)return false;bool tested=false;
            foreach(var own in shapes)
            {
                if(own==null||!own.enabled||own.isTrigger||!own.gameObject.activeInHierarchy)continue;
                tested=true;bool current=Physics.ComputePenetration(own,own.transform.position,own.transform.rotation,other,other.transform.position,other.transform.rotation,out var normal,out float depth);
                bool future=Physics.ComputePenetration(own,own.transform.position+displacement,own.transform.rotation,other,other.transform.position,other.transform.rotation,out _,out float futureDepth);
                // Only a separating displacement may leave an existing actual
                // contact. Never permit a new overlap or increasing penetration.
                if(current&&(Vector3.Dot(direction,normal)<.15f||future&&futureDepth>=depth-.001f)||!current&&future)return false;
            }
            return tested;
        }
    }
}
